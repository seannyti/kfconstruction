using System.Security.Cryptography;
using System.Text;
using KfConstructionWeb.Services.Interfaces;

namespace KfConstructionWeb.Services;

/// <summary>
/// File encryption service using AES-256-GCM
/// Provides at-rest encryption for receipt files
/// OWASP ASVS L2 compliant
/// </summary>
public class FileEncryptionService : IFileEncryptionService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<FileEncryptionService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _receiptStoragePath;
    private byte[] _encryptionKey;

    public FileEncryptionService(
        IWebHostEnvironment environment,
        ILogger<FileEncryptionService> logger,
        IConfiguration configuration)
    {
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        _receiptStoragePath = Path.Combine(_environment.WebRootPath, "uploads", "receipts");
        Directory.CreateDirectory(_receiptStoragePath);

        // Load encryption key from configuration
        // SECURITY: In production, use Azure Key Vault, AWS Secrets Manager, or HashiCorp Vault
        // Try both configuration paths for backwards compatibility
        var keyBase64 = _configuration["ReceiptEncryption:EncryptionKey"] 
                        ?? _configuration["ReceiptSettings:EncryptionKey"];
        if (string.IsNullOrEmpty(keyBase64))
        {
            // CRITICAL: Fail fast in production if encryption key not configured
            // Temporary keys would make encrypted files permanently inaccessible after restart
            if (_environment.IsProduction())
            {
                _logger.LogCritical(
                    "SECURITY VIOLATION: Encryption key not configured in production environment. " +
                    "Configure 'ReceiptEncryption:EncryptionKey' or 'ReceiptSettings:EncryptionKey' in Azure Key Vault, user-secrets, or environment variables.");
                throw new InvalidOperationException(
                    "Encryption key must be configured in production. " +
                    "Use Azure Key Vault, AWS Secrets Manager, or configure via user-secrets/environment variables.");
            }
            
            _logger.LogWarning(
                "Encryption key not configured in {Environment} environment. " +
                "Generating temporary development key. Files encrypted with this key " +
                "will become inaccessible after application restart.",
                _environment.EnvironmentName);
            _encryptionKey = GenerateKey();
        }
        else
        {
            try
            {
                _encryptionKey = Convert.FromBase64String(keyBase64);
                if (_encryptionKey.Length != 32)
                {
                    throw new InvalidOperationException("Encryption key must be 256 bits (32 bytes)");
                }
                
                _logger.LogInformation(
                    "File encryption service initialized successfully (Algorithm: AES-256-GCM, Key Length: 256 bits)");
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "Encryption key is not valid Base64. Key format: Base64-encoded 32 bytes.");
                throw new InvalidOperationException("Invalid encryption key format. Must be Base64-encoded 32 bytes.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load encryption key from configuration.");
                throw;
            }
        }
    }

    /// <summary>
    /// Encrypts a file using AES-256-GCM
    /// </summary>
    public async Task<(string EncryptedPath, string Algorithm)> EncryptFileAsync(Stream inputStream, string originalFileName)
    {
        try
        {
            var extension = Path.GetExtension(originalFileName);
            var secureFileName = GenerateSecureFileName(extension);
            var encryptedPath = Path.Combine(_receiptStoragePath, secureFileName);

            _logger.LogInformation("Encrypting file: {OriginalName} -> {SecureName}", originalFileName, secureFileName);

            // Use explicit tag size to align with .NET 9 guidance and avoid obsolete constructor warnings
            using var aes = new AesGcm(_encryptionKey, AesGcm.TagByteSizes.MaxSize);
            
            // Generate random nonce (12 bytes for GCM)
            var nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
            RandomNumberGenerator.Fill(nonce);

            // Read input stream
            using var memoryStream = new MemoryStream();
            await inputStream.CopyToAsync(memoryStream);
            var plaintext = memoryStream.ToArray();

            // Prepare buffers
            var ciphertext = new byte[plaintext.Length];
            var tag = new byte[AesGcm.TagByteSizes.MaxSize]; // 16 bytes authentication tag

            // Encrypt with authentication
            aes.Encrypt(nonce, plaintext, ciphertext, tag);

            // Write encrypted file format: [nonce(12)][tag(16)][ciphertext]
            await using var fileStream = new FileStream(encryptedPath, FileMode.Create, FileAccess.Write);
            await fileStream.WriteAsync(nonce);
            await fileStream.WriteAsync(tag);
            await fileStream.WriteAsync(ciphertext);

            _logger.LogInformation("File encrypted successfully: {Path} ({Size} bytes)", 
                encryptedPath, fileStream.Length);

            return (encryptedPath, "AES-256-GCM");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error encrypting file: {FileName}", originalFileName);
            throw new InvalidOperationException($"Failed to encrypt file: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Decrypts a file using AES-256-GCM
    /// </summary>
    public async Task<Stream> DecryptFileAsync(string encryptedPath)
    {
        try
        {
            if (!File.Exists(encryptedPath))
            {
                throw new FileNotFoundException("Encrypted file not found", encryptedPath);
            }

            _logger.LogDebug("Decrypting file: {Path}", encryptedPath);

            await using var fileStream = new FileStream(encryptedPath, FileMode.Open, FileAccess.Read, FileShare.Read);

            // Sanity-check file length before allocations
            if (fileStream.Length < (AesGcm.NonceByteSizes.MaxSize + AesGcm.TagByteSizes.MaxSize))
            {
                throw new InvalidDataException("Encrypted file is too small to contain required headers (nonce + tag)");
            }

            // Read nonce (12 bytes) exactly
            var nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
            await fileStream.ReadExactlyAsync(nonce.AsMemory(0, AesGcm.NonceByteSizes.MaxSize));

            // Read authentication tag (16 bytes) exactly
            var tag = new byte[AesGcm.TagByteSizes.MaxSize];
            await fileStream.ReadExactlyAsync(tag.AsMemory(0, AesGcm.TagByteSizes.MaxSize));

            // Read ciphertext (rest of file) exactly
            var remaining = checked((int)(fileStream.Length - AesGcm.NonceByteSizes.MaxSize - AesGcm.TagByteSizes.MaxSize));
            if (remaining < 0)
            {
                throw new InvalidDataException("Encrypted file header lengths are invalid");
            }

            var ciphertext = new byte[remaining];
            if (remaining > 0)
            {
                await fileStream.ReadExactlyAsync(ciphertext.AsMemory());
            }

            // Decrypt and authenticate
            using var aes = new AesGcm(_encryptionKey, AesGcm.TagByteSizes.MaxSize);
            var plaintext = new byte[ciphertext.Length];
            
            aes.Decrypt(nonce, ciphertext, tag, plaintext);

            _logger.LogDebug("File decrypted successfully: {Path} ({Size} bytes)", encryptedPath, plaintext.Length);

            // Return decrypted content as memory stream
            return new MemoryStream(plaintext);
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "Decryption failed (authentication tag verification failed): {Path}", encryptedPath);
            throw new InvalidOperationException("File decryption failed - file may be corrupted or tampered with", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrypting file: {Path}", encryptedPath);
            throw new InvalidOperationException($"Failed to decrypt file: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Securely deletes a file by overwriting with random data before deletion
    /// Defense against forensic recovery
    /// </summary>
    public async Task<bool> SecureDeleteFileAsync(string encryptedPath)
    {
        try
        {
            if (!File.Exists(encryptedPath))
            {
                _logger.LogWarning("File not found for secure deletion: {Path}", encryptedPath);
                return false;
            }

            var fileInfo = new FileInfo(encryptedPath);
            var fileSize = fileInfo.Length;

            // Overwrite file with random data 3 times (DOD 5220.22-M standard)
            for (int pass = 0; pass < 3; pass++)
            {
                await using var stream = new FileStream(encryptedPath, FileMode.Open, FileAccess.Write);
                var randomData = new byte[4096];
                
                for (long written = 0; written < fileSize; written += randomData.Length)
                {
                    RandomNumberGenerator.Fill(randomData);
                    var bytesToWrite = (int)Math.Min(randomData.Length, fileSize - written);
                    await stream.WriteAsync(randomData.AsMemory(0, bytesToWrite));
                }
                
                await stream.FlushAsync();
            }

            // Delete the file
            File.Delete(encryptedPath);

            _logger.LogInformation("File securely deleted: {Path}", encryptedPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during secure file deletion: {Path}", encryptedPath);
            return false;
        }
    }

    /// <summary>
    /// Generates a cryptographically secure random filename
    /// </summary>
    public string GenerateSecureFileName(string originalExtension)
    {
        var guidBytes = Guid.NewGuid().ToByteArray();
        var randomBytes = new byte[16];
        RandomNumberGenerator.Fill(randomBytes);

        var combined = guidBytes.Concat(randomBytes).ToArray();
        var hash = SHA256.HashData(combined);
        
        var fileName = Convert.ToBase64String(hash)
            .Replace("+", "")
            .Replace("/", "")
            .Replace("=", "")
            .Substring(0, 32);

        return $"{fileName}{originalExtension}";
    }

    /// <summary>
    /// Generates a 256-bit encryption key
    /// </summary>
    private static byte[] GenerateKey()
    {
        var key = new byte[32]; // 256 bits
        RandomNumberGenerator.Fill(key);
        return key;
    }
}

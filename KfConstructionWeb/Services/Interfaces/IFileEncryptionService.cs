namespace KfConstructionWeb.Services.Interfaces;

public interface IFileEncryptionService
{
    /// <summary>
    /// Encrypts a file and returns the encrypted file path
    /// Uses AES-256-GCM for authenticated encryption
    /// </summary>
    Task<(string EncryptedPath, string Algorithm)> EncryptFileAsync(Stream inputStream, string originalFileName);

    /// <summary>
    /// Decrypts a file and returns the decrypted stream
    /// </summary>
    Task<Stream> DecryptFileAsync(string encryptedPath);

    /// <summary>
    /// Securely deletes an encrypted file
    /// </summary>
    Task<bool> SecureDeleteFileAsync(string encryptedPath);

    /// <summary>
    /// Generates a secure random filename
    /// </summary>
    string GenerateSecureFileName(string originalExtension);
}

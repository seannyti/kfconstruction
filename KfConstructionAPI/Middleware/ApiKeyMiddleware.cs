using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace KfConstructionAPI.Middleware
{
    public class ApiKeyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ApiKeyMiddleware> _logger;

        public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<ApiKeyMiddleware> logger)
        {
            _next = next;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, KfConstructionAPI.Services.Interfaces.IApiKeyService apiKeyService)
        {
            // Skip API key check for Swagger endpoints in development
            if (context.Request.Path.StartsWithSegments("/swagger") && 
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                await _next(context);
                return;
            }

            // Skip API key check for health checks
            if (context.Request.Path.StartsWithSegments("/health"))
            {
                await _next(context);
                return;
            }

            // Check for API key in header
            if (!context.Request.Headers.TryGetValue("X-API-Key", out var providedApiKey))
            {
                _logger.LogWarning("API request without API key from {RemoteIpAddress}", context.Connection.RemoteIpAddress);
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("API Key is required");
                return;
            }

            // Validate against DB first; fallback to appsettings for legacy single key
            var isValid = await apiKeyService.ValidateAsync(providedApiKey!);
            if (!isValid)
            {
                var legacyKey = _configuration["ApiSettings:ApiKey"];
                isValid = !string.IsNullOrEmpty(legacyKey) && providedApiKey.Equals(legacyKey);
            }

            if (!isValid)
            {
                _logger.LogWarning("Invalid API key attempt from {RemoteIpAddress}", context.Connection.RemoteIpAddress);
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Invalid API Key");
                return;
            }

            _logger.LogInformation("Valid API request from {RemoteIpAddress}", context.Connection.RemoteIpAddress);
            await _next(context);
        }
    }
}
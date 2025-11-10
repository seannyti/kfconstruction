namespace KfConstructionWeb.Middleware;

public static class MiddlewareHelpers
{
    /// <summary>
    /// Check if the current request path should skip middleware processing
    /// </summary>
    public static bool ShouldSkipPath(string? requestPath, string[] skipPaths)
    {
        if (string.IsNullOrEmpty(requestPath))
            return false;

        var path = requestPath.ToLower();
        return skipPaths.Any(skipPath => path.StartsWith(skipPath.ToLower(), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Common paths that should be skipped for most middleware
    /// </summary>
    public static readonly string[] CommonSkipPaths = 
    {
        "/css/",
        "/js/",
        "/lib/",
        "/images/",
        "/favicon.ico",
        "/_vs/browserLink" // Visual Studio browser link
    };

    /// <summary>
    /// Authentication related paths
    /// </summary>
    public static readonly string[] AuthPaths = 
    {
        "/identity/account/login",
        "/identity/account/logout",
        "/identity/account/register",
        "/identity/account/accessdenied"
    };

    /// <summary>
    /// Admin related paths
    /// </summary>
    public static readonly string[] AdminPaths = 
    {
        "/admin"
    };

    /// <summary>
    /// API related paths
    /// </summary>
    public static readonly string[] ApiPaths = 
    {
        "/api/"
    };
}
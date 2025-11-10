using System.Net;

namespace KfConstructionAPI.Models;

/// <summary>
/// Standard API response wrapper for consistent response formatting across all endpoints
/// </summary>
public class APIResponse
{
    /// <summary>
    /// The HTTP status code for the response
    /// </summary>
    public HttpStatusCode StatusCode { get; set; }

    /// <summary>
    /// Indicates whether the operation was successful (defaults to true)
    /// </summary>
    public bool IsSuccess { get; set; } = true;

    /// <summary>
    /// The result data to be returned to the client (null for operations that don't return data)
    /// </summary>
    public object? Result { get; set; }

    /// <summary>
    /// A collection of error messages (empty when IsSuccess is true)
    /// </summary>
    public List<string> ErrorMessages { get; set; } = new();
}
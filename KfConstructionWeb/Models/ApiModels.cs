namespace KfConstructionWeb.Models;

/// <summary>
/// Generic API response wrapper for consistency across API calls
/// </summary>
public class APIResponse
{
    public int StatusCode { get; set; }
    public bool IsSuccess { get; set; } = true;
    public object? Result { get; set; }
    public List<string> ErrorMessages { get; set; } = new();
}


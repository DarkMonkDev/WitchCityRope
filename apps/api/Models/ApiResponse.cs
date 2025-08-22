namespace WitchCityRope.Api.Models;

/// <summary>
/// Standard API response wrapper for consistent response format
/// Shared across all controllers and endpoints for uniform API responses
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }
    public string? Details { get; set; }
    public string? Message { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
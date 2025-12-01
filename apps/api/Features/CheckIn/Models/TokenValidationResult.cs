namespace WitchCityRope.Api.Features.CheckIn.Models;

/// <summary>
/// Result of session token validation
/// Contains event ID, session ID, and staff ID from the validated token
/// </summary>
public class TokenValidationResult
{
    /// <summary>
    /// Event this token grants access to
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Session this token grants check-in access to
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// Admin user who generated this token (becomes RecordedByStaffId for operations)
    /// </summary>
    public Guid CreatedByStaffId { get; set; }
}

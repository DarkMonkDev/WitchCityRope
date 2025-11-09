namespace WitchCityRope.Api.Features.Participation.Entities;

/// <summary>
/// Status of event attendance
/// </summary>
public enum AttendanceStatus
{
    /// <summary>
    /// Active attendance
    /// </summary>
    Active = 1,

    /// <summary>
    /// Cancelled attendance
    /// </summary>
    Cancelled = 2,

    /// <summary>
    /// Refunded attendance (for tickets)
    /// </summary>
    Refunded = 3,

    /// <summary>
    /// Waitlisted attendance
    /// </summary>
    Waitlisted = 4
}

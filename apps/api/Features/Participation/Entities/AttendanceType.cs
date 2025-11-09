namespace WitchCityRope.Api.Features.Participation.Entities;

/// <summary>
/// Type of event attendance
/// </summary>
public enum AttendanceType
{
    /// <summary>
    /// RSVP for social events (no payment required) - free attendance
    /// </summary>
    RSVP = 1,

    /// <summary>
    /// Ticket purchase for classes (payment required) - paid attendance
    /// </summary>
    Ticket = 2
}

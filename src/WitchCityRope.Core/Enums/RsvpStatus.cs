namespace WitchCityRope.Core.Enums;

/// <summary>
/// Represents the status of an RSVP for a social event
/// </summary>
public enum RsvpStatus
{
    /// <summary>
    /// RSVP is confirmed and active
    /// </summary>
    Confirmed = 0,
    
    /// <summary>
    /// RSVP has been cancelled by the user
    /// </summary>
    Cancelled = 1,
    
    /// <summary>
    /// User has checked in at the event
    /// </summary>
    CheckedIn = 2,
    
    /// <summary>
    /// On waitlist due to capacity limits
    /// </summary>
    Waitlisted = 3
}
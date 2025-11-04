namespace WitchCityRope.Api.Features.CheckIn.Models;

/// <summary>
/// Registration status for event attendees
/// </summary>
public enum RegistrationStatus
{
    /// <summary>
    /// Attendee is confirmed/registered but hasn't checked in yet
    /// </summary>
    Confirmed,

    /// <summary>
    /// Attendee is on the waitlist
    /// </summary>
    Waitlist,

    /// <summary>
    /// Attendee has checked in to the event
    /// </summary>
    CheckedIn,

    /// <summary>
    /// Attendee was registered but didn't show up
    /// </summary>
    NoShow
}

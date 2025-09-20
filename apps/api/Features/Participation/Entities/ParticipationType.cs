namespace WitchCityRope.Api.Features.Participation.Entities;

/// <summary>
/// Type of event participation
/// </summary>
public enum ParticipationType
{
    /// <summary>
    /// RSVP for social events (no payment required)
    /// </summary>
    RSVP = 1,

    /// <summary>
    /// Ticket purchase for classes (payment required)
    /// </summary>
    Ticket = 2
}
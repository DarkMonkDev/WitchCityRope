namespace WitchCityRope.Api.Features.Participation.Entities;

/// <summary>
/// Status of event participation
/// </summary>
public enum ParticipationStatus
{
    /// <summary>
    /// Active participation
    /// </summary>
    Active = 1,

    /// <summary>
    /// Cancelled participation
    /// </summary>
    Cancelled = 2,

    /// <summary>
    /// Refunded participation (for tickets)
    /// </summary>
    Refunded = 3,

    /// <summary>
    /// Waitlisted participation
    /// </summary>
    Waitlisted = 4
}
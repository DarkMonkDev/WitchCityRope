using WitchCityRope.Api.Features.Participation.Entities;

namespace WitchCityRope.Api.Features.Participation.Models;

/// <summary>
/// DTO for user's participation list
/// Auto-generated as TypeScript interface by NSwag
/// </summary>
public class UserParticipationDto
{
    /// <summary>
    /// Participation ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Event ID
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Event title
    /// </summary>
    public string EventTitle { get; set; } = string.Empty;

    /// <summary>
    /// Event start date
    /// </summary>
    public DateTime EventStartDate { get; set; }

    /// <summary>
    /// Event end date
    /// </summary>
    public DateTime EventEndDate { get; set; }

    /// <summary>
    /// Event location
    /// </summary>
    public string EventLocation { get; set; } = string.Empty;

    /// <summary>
    /// Type of participation (RSVP or Ticket)
    /// </summary>
    public ParticipationType ParticipationType { get; set; }

    /// <summary>
    /// Current status of participation
    /// </summary>
    public ParticipationStatus Status { get; set; }

    /// <summary>
    /// When user registered for the event
    /// </summary>
    public DateTime ParticipationDate { get; set; }

    /// <summary>
    /// Optional notes from participant
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Whether this participation can be cancelled
    /// </summary>
    public bool CanCancel { get; set; }
}
using WitchCityRope.Api.Features.Participation.Entities;

namespace WitchCityRope.Api.Features.Participation.Models;

/// <summary>
/// DTO for admin view of event participations
/// Auto-generated as TypeScript interface by NSwag
/// </summary>
public class EventParticipationDto
{
    /// <summary>
    /// Participation ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// User's scene name
    /// </summary>
    public string UserSceneName { get; set; } = string.Empty;

    /// <summary>
    /// User's email address
    /// </summary>
    public string UserEmail { get; set; } = string.Empty;

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

    /// <summary>
    /// Flexible metadata for additional information (e.g., purchase amount)
    /// Stored as JSONB in PostgreSQL
    /// </summary>
    public string? Metadata { get; set; }
}
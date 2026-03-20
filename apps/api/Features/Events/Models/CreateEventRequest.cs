using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.Events.Models;

/// <summary>
/// Request model for creating a new event
/// Follows vertical slice architecture pattern
/// </summary>
public class CreateEventRequest
{
    [Required]
    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? ShortDescription { get; set; }

    [Required]
    public string Description { get; set; } = string.Empty;

    public string? Policies { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Required]
    public int VenueId { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int Capacity { get; set; }

    /// <summary>
    /// Whether to publish the event immediately (default: false for draft)
    /// </summary>
    public bool IsPublished { get; set; } = false;

    /// <summary>
    /// Whether free RSVPs are enabled for this event
    /// </summary>
    [Required]
    public bool AllowRsvps { get; set; }

    /// <summary>
    /// Whether ticket purchase is mandatory to attend
    /// </summary>
    [Required]
    public bool RequireTicketPurchase { get; set; }

    /// <summary>
    /// Whether only vetted members can attend this event
    /// </summary>
    [Required]
    public bool VettedMembersOnly { get; set; }

    /// <summary>
    /// List of sessions within this event (optional)
    /// </summary>
    public List<SessionDto>? Sessions { get; set; }

    /// <summary>
    /// List of ticket types available for this event (optional)
    /// </summary>
    public List<TicketTypeDto>? TicketTypes { get; set; }

    /// <summary>
    /// List of volunteer positions for this event (optional)
    /// </summary>
    public List<EventVolunteerPositionDto>? VolunteerPositions { get; set; }

    /// <summary>
    /// List of teacher/organizer user IDs (optional)
    /// </summary>
    public List<string>? TeacherIds { get; set; }

    // Granular timing controls (all optional)
    public decimal? RegistrationOpenHours { get; set; }
    public decimal? RegistrationCloseHours { get; set; }
    public decimal? CancellationCloseHours { get; set; }
    public decimal? VolunteerRegistrationCloseHours { get; set; }
    public decimal? VolunteerCancellationCloseHours { get; set; }

    /// <summary>
    /// Default maximum tickets/RSVPs per person for this event (optional).
    /// NULL = no per-person limit.
    /// </summary>
    [Range(1, 1000)]
    public int? DefaultMaxTicketOrRsvpPerPerson { get; set; }
}
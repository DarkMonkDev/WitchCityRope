using System.ComponentModel.DataAnnotations;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.CheckIn.Entities;

/// <summary>
/// EventAttendee entity - Links users to events with registration and attendee details
/// Supports both pre-registered attendees and walk-in manual entries
/// </summary>
public class EventAttendee
{
    /// <summary>
    /// Unique identifier for the attendee registration
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Reference to the event
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Reference to the registered user
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Current registration status
    /// Values: confirmed, waitlist, checked-in, no-show, cancelled
    /// </summary>
    [Required]
    public string RegistrationStatus { get; set; } = "confirmed";

    /// <summary>
    /// Unique ticket/confirmation number for identification
    /// </summary>
    public string? TicketNumber { get; set; }

    /// <summary>
    /// Position on waitlist (null if not waitlisted)
    /// </summary>
    public int? WaitlistPosition { get; set; }

    /// <summary>
    /// Flag indicating if this is the attendee's first event
    /// </summary>
    public bool IsFirstTime { get; set; } = false;

    /// <summary>
    /// Dietary restrictions and allergies (displayed during check-in)
    /// </summary>
    public string? DietaryRestrictions { get; set; }

    /// <summary>
    /// Accessibility needs and accommodations
    /// </summary>
    public string? AccessibilityNeeds { get; set; }

    /// <summary>
    /// Whether the attendee has completed required waivers
    /// </summary>
    public bool HasCompletedWaiver { get; set; } = false;

    /// <summary>
    /// Flexible metadata for additional attendee information
    /// </summary>
    public string Metadata { get; set; } = "{}";

    /// <summary>
    /// When the registration was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the registration was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// User who created this registration
    /// </summary>
    public Guid? CreatedBy { get; set; }

    /// <summary>
    /// User who last updated this registration
    /// </summary>
    public Guid? UpdatedBy { get; set; }

    // Navigation properties
    public Event Event { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
    public ApplicationUser? CreatedByUser { get; set; }
    public ApplicationUser? UpdatedByUser { get; set; }
    public ICollection<CheckIn> CheckIns { get; set; } = new List<CheckIn>();
    public ICollection<CheckInAuditLog> AuditLogs { get; set; } = new List<CheckInAuditLog>();

    /// <summary>
    /// Constructor initializes required fields
    /// </summary>
    public EventAttendee()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Constructor for new registration
    /// </summary>
    public EventAttendee(Guid eventId, Guid userId, string registrationStatus = "confirmed") : this()
    {
        EventId = eventId;
        UserId = userId;
        RegistrationStatus = registrationStatus;
    }
}
using System.ComponentModel.DataAnnotations;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.CheckIn.Entities;

/// <summary>
/// CheckIn entity - Records actual check-in events for attendees
/// Supports both regular attendees and manual walk-in entries
/// </summary>
public class CheckIn
{
    /// <summary>
    /// Unique identifier for the check-in record
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Reference to the attendee being checked in
    /// </summary>
    public Guid EventAttendeeId { get; set; }

    /// <summary>
    /// Reference to the event (denormalized for quick queries)
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Exact timestamp when check-in occurred (in event local time)
    /// </summary>
    public DateTime CheckInTime { get; set; }

    /// <summary>
    /// Staff member who performed the check-in
    /// </summary>
    public Guid StaffMemberId { get; set; }

    /// <summary>
    /// Optional notes added during check-in
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Flag indicating if this was a manual entry (walk-in)
    /// </summary>
    public bool IsManualEntry { get; set; } = false;

    /// <summary>
    /// Flag indicating if capacity was overridden for this check-in
    /// </summary>
    public bool OverrideCapacity { get; set; } = false;

    /// <summary>
    /// Manual entry data for walk-in attendees (JSON)
    /// Required when IsManualEntry = true
    /// </summary>
    public string? ManualEntryData { get; set; }

    /// <summary>
    /// When the check-in record was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// User who created this check-in record
    /// </summary>
    public Guid CreatedBy { get; set; }

    // Navigation properties
    public EventAttendee EventAttendee { get; set; } = null!;
    public Event Event { get; set; } = null!;
    public ApplicationUser StaffMember { get; set; } = null!;
    public ApplicationUser CreatedByUser { get; set; } = null!;

    /// <summary>
    /// Constructor initializes required fields
    /// </summary>
    public CheckIn()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        CheckInTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Constructor for new check-in
    /// </summary>
    public CheckIn(Guid eventAttendeeId, Guid eventId, Guid staffMemberId) : this()
    {
        EventAttendeeId = eventAttendeeId;
        EventId = eventId;
        StaffMemberId = staffMemberId;
        CreatedBy = staffMemberId;
    }
}
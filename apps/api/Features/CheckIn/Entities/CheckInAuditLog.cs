using System.ComponentModel.DataAnnotations;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.CheckIn.Entities;

/// <summary>
/// CheckInAuditLog entity - Complete audit trail for check-in operations
/// Tracks all changes and actions for compliance and troubleshooting
/// </summary>
public class CheckInAuditLog
{
    /// <summary>
    /// Unique identifier for the audit log entry
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Reference to the attendee (null for event-level actions)
    /// </summary>
    public Guid? EventAttendeeId { get; set; }

    /// <summary>
    /// Reference to the event
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Type of action performed
    /// Values: check-in, manual-entry, capacity-override, status-change, data-update
    /// </summary>
    [Required]
    public string ActionType { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable description of the action
    /// </summary>
    [Required]
    public string ActionDescription { get; set; } = string.Empty;

    /// <summary>
    /// JSON representation of old values (before change)
    /// </summary>
    public string? OldValues { get; set; }

    /// <summary>
    /// JSON representation of new values (after change)
    /// </summary>
    public string? NewValues { get; set; }

    /// <summary>
    /// IP address of the user performing the action
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string for the device/browser
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// When the action occurred
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// User who performed the action
    /// </summary>
    public Guid CreatedBy { get; set; }

    // Navigation properties
    public EventAttendee? EventAttendee { get; set; }
    public Event Event { get; set; } = null!;
    public ApplicationUser CreatedByUser { get; set; } = null!;

    /// <summary>
    /// Constructor initializes required fields
    /// </summary>
    public CheckInAuditLog()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Constructor for new audit log entry
    /// </summary>
    public CheckInAuditLog(Guid eventId, string actionType, string description, Guid createdBy) : this()
    {
        EventId = eventId;
        ActionType = actionType;
        ActionDescription = description;
        CreatedBy = createdBy;
    }
}
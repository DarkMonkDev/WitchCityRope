using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WitchCityRope.Api.Features.Participation.Entities;

namespace WitchCityRope.Api.Models;

/// <summary>
/// Session entity representing individual sessions within an event
/// Supports both single-session and multi-session events
/// </summary>
public class Session
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Reference to the parent event
    /// </summary>
    [Required]
    public Guid EventId { get; set; }

    /// <summary>
    /// Session identifier code (e.g., "S1", "S2", "Day1", "Day2")
    /// </summary>
    [Required]
    [MaxLength(10)]
    public string SessionCode { get; set; } = string.Empty;

    /// <summary>
    /// Name of the session (e.g., "Morning Session", "Day 1", "Afternoon Workshop")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Session start time in UTC
    /// </summary>
    [Required]
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Session end time in UTC
    /// </summary>
    [Required]
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Maximum capacity for this specific session
    /// </summary>
    [Required]
    public int Capacity { get; set; }

    /// <summary>
    /// Current number of attendees registered for this session.
    ///
    /// BUSINESS LOGIC:
    /// - Counts active ticket attendances for this session
    /// - Handles single-session tickets (SessionId = this session)
    /// - Handles multi-session tickets (IsMultiSession flag)
    ///
    /// DESIGN DECISION:
    /// Calculated property to ensure accuracy. Previous approach calculated
    /// in EventService.cs but never persisted, leading to confusion.
    ///
    /// CAPACITY CALCULATION:
    /// For workshops: Capacity based on ticket count (this property)
    /// For social events: Capacity based on RSVP count (different calculation)
    /// </summary>
    [NotMapped]
    public int CurrentAttendees
    {
        get
        {
            if (Event?.EventAttendances == null) return 0;

            return Event.EventAttendances.Count(ea =>
                ea.Status == AttendanceStatus.Active &&
                ea.AttendanceType == AttendanceType.Ticket &&
                ea.TicketPurchase != null &&
                // Ticket is for this session
                (ea.TicketPurchase.TicketType.SessionId == Id ||
                 // OR it's a multi-session ticket for this event
                 (ea.TicketPurchase.TicketType.SessionId == null &&
                  ea.TicketPurchase.TicketType.EventId == EventId)));
        }
    }

    /// <summary>
    /// Navigation property to parent event
    /// </summary>
    public Event? Event { get; set; }

    /// <summary>
    /// Navigation property to ticket types that include this session
    /// </summary>
    public ICollection<TicketType> TicketTypes { get; set; } = new List<TicketType>();

    /// <summary>
    /// When record was created
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When record was last updated
    /// </summary>
    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

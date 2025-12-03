using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WitchCityRope.Api.Features.Participation.Entities;

namespace WitchCityRope.Api.Models;

// =============================================================================
// DATETIME HANDLING DOCUMENTATION
// =============================================================================
//
// This entity stores times as TRUE UTC in the database.
//
// STORAGE:
// - StartTime and EndTime are stored as actual UTC datetimes
// - Example: User enters 10:25 PM EST → stored as 3:25 AM UTC next day
//
// THERE ARE NO SEPARATE DATE COLUMNS:
// - The calendar date must be derived from StartTime/EndTime
// - CRITICAL: Convert to local timezone FIRST, then extract date
//
// EXTRACTING LOCAL DATE (correct pattern):
//   var easternZone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
//   var utcTime = DateTime.SpecifyKind(session.StartTime, DateTimeKind.Utc);
//   var localTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, easternZone);
//   var localDate = localTime.Date;  // Correct: Dec 4
//
// WRONG (do not do):
//   var localDate = session.StartTime.Date;  // Wrong: Dec 5 (UTC date)
//
// BUSINESS LOGIC:
// - All timing calculations use UTC-to-UTC comparisons
// - Example: (session.StartTime - DateTime.UtcNow).TotalHours
// - NO timezone conversion needed for timing logic
//
// See: /docs/guides-setup/datetime-handling-guide.md
// =============================================================================

/// <summary>
/// Session entity representing individual sessions within an event.
/// Supports both single-session and multi-session events.
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
    /// Session start time stored as TRUE UTC.
    ///
    /// STORAGE: This is the actual UTC time, not "naive UTC".
    /// Example: User enters 10:25 PM EST on Dec 4 → stored as 3:25 AM UTC Dec 5.
    ///
    /// BUSINESS LOGIC: Compare directly with DateTime.UtcNow for timing calculations.
    /// Example: var hoursUntil = (StartTime - DateTime.UtcNow).TotalHours;
    ///
    /// DISPLAY: Frontend converts to local using utcToLocal() from eventUtils.ts.
    ///
    /// EXTRACTING DATE: Do NOT use StartTime.Date (gives UTC date).
    /// Use TimeZoneInfo.ConvertTimeFromUtc(StartTime, easternZone).Date instead.
    /// </summary>
    [Required]
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Session end time stored as TRUE UTC.
    /// Same handling rules as StartTime - see StartTime documentation.
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
    /// - Handles single-session tickets (Sessions contains this session)
    /// - Handles multi-session tickets (Sessions contains this session)
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
                // Ticket includes this session (many-to-many relationship)
                ea.TicketPurchase.TicketType.Sessions.Any(s => s.Id == Id));
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

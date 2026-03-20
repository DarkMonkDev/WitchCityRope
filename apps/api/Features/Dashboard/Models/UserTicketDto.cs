namespace WitchCityRope.Api.Features.Dashboard.Models;

/// <summary>
/// Lightweight ticket info for user dashboard display.
/// Shows what ticket type the user purchased and for which session (if multi-session event).
/// Includes assignment info so the dashboard can show "From: [Name]" badges
/// for tickets that were assigned to the user by another person.
/// </summary>
public class UserTicketDto
{
    /// <summary>
    /// Name of the ticket type purchased (e.g., "General Admission", "VIP", "Early Bird")
    /// </summary>
    public string TicketTypeName { get; set; } = string.Empty;

    /// <summary>
    /// Session name the ticket is for (null for single-session events or event-level tickets).
    /// Helps users identify which day/session their ticket covers in multi-session events.
    /// </summary>
    public string? SessionName { get; set; }

    /// <summary>
    /// Session start time (UTC). Used to display session date/time below ticket name
    /// on the dashboard card, matching the format used on the event detail page.
    /// Null when no session is associated with this ticket.
    /// </summary>
    public DateTime? SessionStartTime { get; set; }

    /// <summary>
    /// Session end time (UTC). Used with SessionStartTime to display time range.
    /// Null when no session is associated with this ticket.
    /// </summary>
    public DateTime? SessionEndTime { get; set; }

    /// <summary>
    /// Scene name of the person who assigned this ticket to the current user.
    /// Null for tickets the user purchased themselves (self-bought).
    /// When set, the dashboard shows a "From: [Name]" badge next to the ticket name.
    /// </summary>
    public string? AssignedBySceneName { get; set; }
}

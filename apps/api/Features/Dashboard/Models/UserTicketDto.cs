namespace WitchCityRope.Api.Features.Dashboard.Models;

/// <summary>
/// Lightweight ticket info for user dashboard display.
/// Shows what ticket type the user purchased and for which session (if multi-session event).
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
}

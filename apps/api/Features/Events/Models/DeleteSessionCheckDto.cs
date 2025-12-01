using System.Collections.Generic;

namespace WitchCityRope.Api.Features.Events.Models;

/// <summary>
/// DTO for checking if a session can be deleted
/// Returns information about what would be affected by the deletion
/// </summary>
public class DeleteSessionCheckDto
{
    /// <summary>
    /// Whether the session can be deleted
    /// </summary>
    public bool CanDelete { get; set; }

    /// <summary>
    /// Reason why deletion is blocked
    /// Possible values: "ticketsSold", "onlySession", "cascadeBlocking"
    /// </summary>
    public string? BlockReason { get; set; }

    /// <summary>
    /// Number of active RSVPs for this session
    /// </summary>
    public int RsvpCount { get; set; }

    /// <summary>
    /// Number of tickets sold for this session
    /// </summary>
    public int TicketsSoldCount { get; set; }

    /// <summary>
    /// List of volunteer shifts that would be affected
    /// </summary>
    public List<string>? VolunteerShifts { get; set; }

    /// <summary>
    /// Ticket types that would be affected by deletion
    /// </summary>
    public List<AffectedTicketTypeDto>? AffectedTicketTypes { get; set; }
}

/// <summary>
/// Information about a ticket type that would be affected by session deletion
/// </summary>
public class AffectedTicketTypeDto
{
    /// <summary>
    /// Ticket type ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Ticket type name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Number of tickets sold for this ticket type
    /// </summary>
    public int TicketsSold { get; set; }

    /// <summary>
    /// Whether this ticket type will be deleted (single-session ticket)
    /// </summary>
    public bool WillBeDeleted { get; set; }
}

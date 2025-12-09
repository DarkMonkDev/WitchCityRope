using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.Participation.Models;

/// <summary>
/// Request model for selective ticket cancellation
/// Allows cancelling specific ticket purchases (not just all tickets)
/// </summary>
public class CancelTicketRequest
{
    /// <summary>
    /// List of TicketPurchase IDs to cancel
    /// Each TicketPurchase may have multiple EventAttendance records (one per session)
    /// ALL attendances for each ticket purchase will be cancelled
    /// </summary>
    public List<Guid>? TicketPurchaseIds { get; set; }

    /// <summary>
    /// Optional reason for cancellation
    /// Passed through to refund service and stored in attendance history
    /// </summary>
    public string? Reason { get; set; }
}

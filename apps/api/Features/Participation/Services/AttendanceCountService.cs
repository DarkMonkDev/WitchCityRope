using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Participation.Entities;

namespace WitchCityRope.Api.Features.Participation.Services;

/// <summary>
/// Single source of truth for attendance counting.
/// See <see cref="IAttendanceCountService"/> for full documentation on display vs reserved counts.
/// </summary>
public class AttendanceCountService : IAttendanceCountService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AttendanceCountService> _logger;

    public AttendanceCountService(
        ApplicationDbContext context,
        ILogger<AttendanceCountService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<int> GetDisplayCountAsync(Guid eventId, CancellationToken ct = default)
    {
        // Single query: fetch event flags and count in one round-trip.
        // Uses conditional counting — social events count Active RSVPs,
        // class events count Active Tickets.
        // This matches Event.GetCurrentRSVPCount()/GetCurrentTicketCount() in-memory logic.
        var result = await _context.Events
            .AsNoTracking()
            .Where(e => e.Id == eventId)
            .Select(e => new
            {
                Count = (e.AllowRsvps && !e.RequireTicketPurchase)
                    ? e.EventAttendances.Count(ea =>
                        ea.Status == AttendanceStatus.Active &&
                        ea.AttendanceType == AttendanceType.RSVP)
                    : e.EventAttendances.Count(ea =>
                        ea.Status == AttendanceStatus.Active &&
                        ea.AttendanceType == AttendanceType.Ticket)
            })
            .FirstOrDefaultAsync(ct);

        if (result == null)
        {
            _logger.LogWarning("Event {EventId} not found when getting display count", eventId);
            return 0;
        }

        return result.Count;
    }

    /// <inheritdoc />
    public async Task<int> GetReservedCountAsync(Guid eventId, CancellationToken ct = default)
    {
        // Counts DISTINCT USERS with reserved attendance, not total records.
        //
        // WHY DISTINCT: A single user can have multiple EventAttendance records:
        //   - RSVP record + Ticket record(s) for social events (auto-RSVP on ticket purchase)
        //   - Multiple Ticket records for multi-session tickets (one per session)
        // Without DISTINCT, a user with 1 RSVP + 1 Ticket = 2 capacity slots consumed
        // instead of 1. This caused production incident 2026-03-19 where 25 unique
        // attendees inflated to 37 records, blocking ticket purchases at capacity 35.
        //
        // Includes PendingPayment to reserve capacity during payment windows.
        // Includes PendingAcceptance because assigned tickets reserve a spot even before
        // the assignee accepts (BR-013, BR-054 - purchased tickets hold capacity).
        return await _context.EventAttendances
            .Where(ea => ea.EventId == eventId
                && (ea.Status == AttendanceStatus.Active
                    || ea.Status == AttendanceStatus.PendingPayment
                    || ea.Status == AttendanceStatus.PendingAcceptance))
            .Select(ea => ea.UserId)
            .Distinct()
            .CountAsync(ct);
    }

    /// <inheritdoc />
    public async Task<int> GetTicketsSoldForTicketTypeAsync(Guid eventId, Guid ticketTypeId, CancellationToken ct = default)
    {
        // Count active ticket attendances for a specific ticket type.
        // Uses EF navigation property in Where clause (generates SQL JOIN).
        // Used by EventService delete validation to check if tickets are sold
        // before allowing deletion of sessions or ticket types.
        return await _context.EventAttendances
            .CountAsync(ea => ea.EventId == eventId
                && ea.Status == AttendanceStatus.Active
                && ea.AttendanceType == AttendanceType.Ticket
                && ea.TicketPurchase != null
                && ea.TicketPurchase.TicketTypeId == ticketTypeId, ct);
    }

    /// <inheritdoc />
    public async Task<int> GetTicketsSoldForSessionAsync(Guid eventId, Guid sessionId, CancellationToken ct = default)
    {
        // Count active ticket attendances where the ticket type includes the given session.
        // Uses EF navigation chain: EventAttendance → TicketPurchase → TicketType → Sessions.
        // This generates a SQL JOIN + EXISTS subquery to check session membership.
        // Used by EventService.CheckSessionDeletionAsync to block deletion of sessions
        // that have tickets sold against them.
        return await _context.EventAttendances
            .CountAsync(ea => ea.EventId == eventId
                && ea.Status == AttendanceStatus.Active
                && ea.AttendanceType == AttendanceType.Ticket
                && ea.TicketPurchase != null
                && ea.TicketPurchase.TicketType.Sessions.Any(s => s.Id == sessionId), ct);
    }
}

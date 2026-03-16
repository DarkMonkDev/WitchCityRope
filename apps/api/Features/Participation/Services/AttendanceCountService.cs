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
        // Includes PendingPayment to reserve capacity during payment windows.
        // Counts ALL attendance types (RSVP + Ticket) because any reservation
        // occupies capacity regardless of type. This prevents overselling when
        // a user has started checkout but hasn't completed payment yet.
        return await _context.EventAttendances
            .CountAsync(ea => ea.EventId == eventId
                && (ea.Status == AttendanceStatus.Active || ea.Status == AttendanceStatus.PendingPayment), ct);
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

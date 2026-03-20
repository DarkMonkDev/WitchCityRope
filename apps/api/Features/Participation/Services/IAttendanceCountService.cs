namespace WitchCityRope.Api.Features.Participation.Services;

/// <summary>
/// SINGLE SOURCE OF TRUTH for all attendance counting logic.
///
/// There are TWO distinct counting concerns in this application:
///
/// 1. DISPLAY COUNT (what users see on event cards and detail pages):
///    - Active status only (no PendingPayment)
///    - Type-appropriate: RSVP count for social events, Ticket count for class events
///    - Use <see cref="GetDisplayCountAsync"/> for this
///
/// 2. RESERVED COUNT (prevents overselling during checkout):
///    - Active + PendingPayment + PendingAcceptance statuses
///    - Counts DISTINCT USERS (not total records) to prevent double-counting
///      users who have both RSVP and Ticket records for the same event
///    - Use <see cref="GetReservedCountAsync"/> for this
///
/// RELATED IN-MEMORY METHODS:
/// - <c>Event.GetCurrentAttendeeCount()</c>, <c>Event.GetCurrentRSVPCount()</c>,
///   and <c>Event.GetCurrentTicketCount()</c> follow the same display-count rules
///   for in-memory scenarios (when EventAttendances navigation property is loaded).
///
/// SQL PROJECTION NOTE:
/// - <c>EventService.GetEventListAsync</c> contains a SQL projection that mirrors
///   display-count rules but runs inside a LINQ Select expression, so it cannot
///   call this service. If counting logic changes, update that projection too.
/// </summary>
public interface IAttendanceCountService
{
    /// <summary>
    /// Gets the display count for an event (Active status only, type-appropriate).
    /// Social events (AllowRsvps=true, RequireTicketPurchase=false): counts Active RSVPs.
    /// Class events: counts Active Tickets.
    /// This is what users see as "X RSVPs" or "X sold" on event cards and detail pages.
    /// </summary>
    Task<int> GetDisplayCountAsync(Guid eventId, CancellationToken ct = default);

    /// <summary>
    /// Gets the reserved count for an event as DISTINCT USERS with Active, PendingPayment,
    /// or PendingAcceptance status. Counts unique people, not total records, to prevent
    /// double-counting users who have multiple records (RSVP + Ticket, multi-session tickets).
    /// Used for capacity business logic checks (CanRSVP, CanPurchaseTicket, ProxyRSVP).
    /// </summary>
    Task<int> GetReservedCountAsync(Guid eventId, CancellationToken ct = default);

    /// <summary>
    /// Gets the count of active ticket attendances for a specific ticket type.
    /// Used by delete validation to check if tickets have been sold before allowing deletion.
    /// </summary>
    Task<int> GetTicketsSoldForTicketTypeAsync(Guid eventId, Guid ticketTypeId, CancellationToken ct = default);

    /// <summary>
    /// Gets the count of active ticket attendances for a specific session.
    /// Counts tickets whose TicketType includes the given session.
    /// Used by delete validation to check if tickets are sold for a session before allowing deletion.
    /// </summary>
    Task<int> GetTicketsSoldForSessionAsync(Guid eventId, Guid sessionId, CancellationToken ct = default);
}

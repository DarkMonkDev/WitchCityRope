using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Features.Participation.Models;
using WitchCityRope.Api.Features.Shared.Models;

namespace WitchCityRope.Api.Features.Participation.Services;

/// <summary>
/// Service for managing event attendance (RSVPs and tickets)
/// </summary>
public interface IAttendanceService
{
    /// <summary>
    /// Get user's attendance status for a specific event
    /// Returns enhanced DTO with hasRSVP/hasTicket flags, nested details, and capacity info
    /// </summary>
    Task<Result<EnhancedParticipationStatusDto?>> GetParticipationStatusAsync(
        Guid eventId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create an RSVP for a social event (vetted members only)
    /// </summary>
    Task<Result<ParticipationStatusDto>> CreateRSVPAsync(
        CreateRSVPRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Purchase a ticket for a class event (any authenticated user)
    /// </summary>
    Task<Result<ParticipationStatusDto>> CreateTicketPurchaseAsync(
        CreateTicketPurchaseRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel user's attendance in an event (legacy mode - cancels one attendance)
    /// </summary>
    Task<Result> CancelParticipationAsync(
        Guid eventId,
        Guid userId,
        AttendanceType? attendanceType = null,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel specific ticket purchases (selective mode - cancels all sessions for each ticket)
    /// </summary>
    /// <param name="eventId">Event identifier</param>
    /// <param name="userId">User identifier (for security verification)</param>
    /// <param name="ticketPurchaseIds">List of TicketPurchase IDs to cancel</param>
    /// <param name="reason">Optional cancellation reason</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result or error</returns>
    Task<Result> CancelTicketPurchasesAsync(
        Guid eventId,
        Guid userId,
        List<Guid> ticketPurchaseIds,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all of user's current attendances
    /// </summary>
    Task<Result<List<UserParticipationDto>>> GetUserParticipationsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all attendances for a specific event (admin only)
    /// </summary>
    Task<Result<List<EventParticipationDto>>> GetEventParticipationsAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);
}

using WitchCityRope.Api.Features.Participation.Models;
using WitchCityRope.Api.Features.Shared.Models;

namespace WitchCityRope.Api.Features.Participation.Services;

/// <summary>
/// Service for managing event participation (RSVPs and tickets)
/// </summary>
public interface IParticipationService
{
    /// <summary>
    /// Get user's participation status for a specific event
    /// </summary>
    Task<Result<ParticipationStatusDto?>> GetParticipationStatusAsync(
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
    /// Cancel user's participation in an event
    /// </summary>
    Task<Result> CancelParticipationAsync(
        Guid eventId,
        Guid userId,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all of user's current participations
    /// </summary>
    Task<Result<List<UserParticipationDto>>> GetUserParticipationsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all participations for a specific event (admin only)
    /// </summary>
    Task<Result<List<EventParticipationDto>>> GetEventParticipationsAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);
}
using WitchCityRope.Api.Features.Vetting.Models;
using WitchCityRope.Api.Features.Shared.Models;

namespace WitchCityRope.Api.Features.Vetting.Services;

/// <summary>
/// Interface for vetting access control service
/// Defines methods for checking RSVP and ticket purchase eligibility based on vetting status
/// </summary>
public interface IVettingAccessControlService
{
    /// <summary>
    /// Checks if user can RSVP to an event based on vetting status
    /// Blocks OnHold (6), Denied (8), and Withdrawn (9) statuses
    /// </summary>
    /// <param name="userId">User ID to check</param>
    /// <param name="eventId">Event ID for access check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>AccessControlResult with IsAllowed flag and user-friendly messaging</returns>
    Task<Result<AccessControlResult>> CanUserRsvpAsync(
        Guid userId,
        Guid eventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if user can purchase tickets based on vetting status
    /// Blocks OnHold (6), Denied (8), and Withdrawn (9) statuses
    /// </summary>
    /// <param name="userId">User ID to check</param>
    /// <param name="eventId">Event ID for access check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>AccessControlResult with IsAllowed flag and user-friendly messaging</returns>
    Task<Result<AccessControlResult>> CanUserPurchaseTicketAsync(
        Guid userId,
        Guid eventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets user's current vetting status information
    /// Returns status with application details if exists, or default status if no application
    /// </summary>
    /// <param name="userId">User ID to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>VettingStatusInfo with current status and application details</returns>
    Task<Result<VettingStatusInfo>> GetUserVettingStatusAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}

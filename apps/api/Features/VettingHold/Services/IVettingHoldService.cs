using WitchCityRope.Api.Features.Shared.Models;
using WitchCityRope.Api.Features.VettingHold.Models;

namespace WitchCityRope.Api.Features.VettingHold.Services;

/// <summary>
/// Service interface for membership hold and reinstatement operations
/// Allows users to voluntarily place their membership on hold and request reinstatement
/// </summary>
public interface IVettingHoldService
{
    /// <summary>
    /// Place user's membership on hold
    /// User must be currently Approved (VettingStatus == 3)
    /// Transitions to OnHold (VettingStatus == 5)
    /// Cancels all future social event RSVPs
    /// </summary>
    /// <param name="userId">ID of the user placing hold (must match authenticated user)</param>
    /// <param name="reason">Reason for placing on hold</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing hold response or error</returns>
    Task<Result<MembershipHoldResponse>> PlaceMembershipOnHoldAsync(
        Guid userId,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Request reinstatement of membership (moves to Final Review)
    /// User must be currently OnHold (VettingStatus == 5)
    /// Transitions to FinalReview (VettingStatus == 2)
    /// </summary>
    /// <param name="userId">ID of the user requesting reinstatement (must match authenticated user)</param>
    /// <param name="reason">Reason for reinstatement request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing reinstatement response or error</returns>
    Task<Result<MembershipHoldResponse>> RequestReinstatementAsync(
        Guid userId,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current hold/reinstatement status for user
    /// Shows what actions are available based on current vetting status
    /// </summary>
    /// <param name="userId">ID of the user to check status for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing status response or error</returns>
    Task<Result<VettingHoldStatusResponse>> GetHoldStatusAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}

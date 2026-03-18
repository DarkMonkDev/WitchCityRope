using WitchCityRope.Api.Features.AuthorizedContacts.Models;
using WitchCityRope.Api.Features.Shared.Models;

namespace WitchCityRope.Api.Features.AuthorizedContacts.Services;

/// <summary>
/// Service for managing authorized contact relationships between users.
/// Handles the delegation authorization system where a Principal grants a Delegate
/// the ability to purchase tickets and create RSVPs on their behalf.
/// </summary>
public interface IAuthorizedContactService
{
    /// <summary>
    /// Gets all active authorized contact relationships for a user, in both directions.
    /// Returns contacts where the user is the Principal (delegates they authorized)
    /// and where the user is the Delegate (principals who authorized them).
    /// </summary>
    /// <param name="userId">The authenticated user's ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>AuthorizedContactsListDto with Delegates and Principals lists</returns>
    Task<Result<AuthorizedContactsListDto>> GetContactsAsync(Guid userId, CancellationToken ct);

    /// <summary>
    /// Creates a new authorized contact relationship where the caller (principal)
    /// authorizes another user (delegate) to act on their behalf.
    /// Validates: no self-authorization (BR-003), delegate exists, no duplicate active relationship.
    /// </summary>
    /// <param name="principalId">The user granting authorization (caller)</param>
    /// <param name="delegateUserId">The user being authorized to act on behalf</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The created AuthorizedContactDto on success, or error</returns>
    Task<Result<AuthorizedContactDto>> AddContactAsync(Guid principalId, Guid delegateUserId, CancellationToken ct);

    /// <summary>
    /// Revokes an existing authorized contact relationship (soft delete).
    /// Only the Principal who created the relationship can revoke it.
    /// Revocation does NOT affect existing tickets/RSVPs (BR-005).
    /// </summary>
    /// <param name="principalId">The caller's user ID (must be the Principal)</param>
    /// <param name="contactId">The AuthorizedContact record ID to revoke</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Success or error (403 if not the principal, 404 if not found)</returns>
    Task<Result> RevokeContactAsync(Guid principalId, Guid contactId, CancellationToken ct);

    /// <summary>
    /// Searches for users by scene name for the "add authorized contact" typeahead.
    /// Excludes the current user (can't add yourself) and users already authorized
    /// by the current user. Returns only UserId and SceneName per AD-009 privacy.
    /// Limited to 10 results, ordered by SceneName.
    /// </summary>
    /// <param name="currentUserId">The authenticated user's ID (excluded from results)</param>
    /// <param name="query">Scene name search query (minimum 2 characters)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of matching users (max 10)</returns>
    Task<Result<List<UserSearchResultDto>>> SearchUsersAsync(Guid currentUserId, string query, CancellationToken ct);

    /// <summary>
    /// Gets the list of principals who have authorized the current user as their delegate.
    /// Used in checkout/RSVP dropdowns to show "who can I act for?"
    /// When eventId is provided, filters by vetting requirements and excludes principals
    /// who already have active or pending attendance for that event.
    /// </summary>
    /// <param name="delegateId">The current user's ID (the delegate)</param>
    /// <param name="eventId">Optional event ID for context-aware filtering (BR-035, BR-012)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of principals with their vetting status</returns>
    Task<Result<List<PrincipalContactDto>>> GetPrincipalsAsync(Guid delegateId, Guid? eventId, CancellationToken ct);

    /// <summary>
    /// Checks whether a delegate is authorized to act on behalf of a principal.
    /// Used internally by other services (checkout, proxy RSVP) for authorization checks.
    /// </summary>
    /// <param name="principalId">The user being represented</param>
    /// <param name="delegateId">The user acting on behalf</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if an active authorization exists</returns>
    Task<bool> IsAuthorizedDelegateAsync(Guid principalId, Guid delegateId, CancellationToken ct);
}

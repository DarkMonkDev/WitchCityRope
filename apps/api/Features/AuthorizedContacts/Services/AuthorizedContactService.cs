using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.AuthorizedContacts.Models;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Features.Shared.Models;

namespace WitchCityRope.Api.Features.AuthorizedContacts.Services;

/// <summary>
/// Implementation of IAuthorizedContactService for managing delegation authorization
/// relationships between users. Uses ApplicationDbContext for data access and follows
/// the Result pattern for consistent error handling.
/// </summary>
public class AuthorizedContactService : IAuthorizedContactService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuthorizedContactService> _logger;

    public AuthorizedContactService(
        ApplicationDbContext context,
        ILogger<AuthorizedContactService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<AuthorizedContactsListDto>> GetContactsAsync(Guid userId, CancellationToken ct)
    {
        try
        {
            // Query all active (non-revoked) AuthorizedContact records involving this user
            var contacts = await _context.AuthorizedContacts
                .AsNoTracking()
                .Include(ac => ac.Principal)
                .Include(ac => ac.Delegate)
                .Where(ac => (ac.PrincipalId == userId || ac.DelegateId == userId)
                             && ac.RevokedAt == null)
                .ToListAsync(ct);

            var result = new AuthorizedContactsListDto
            {
                // Delegates: people I authorized (I am the Principal)
                Delegates = contacts
                    .Where(ac => ac.PrincipalId == userId)
                    .Select(ac => new AuthorizedContactDto
                    {
                        Id = ac.Id,
                        UserId = ac.DelegateId,
                        SceneName = ac.Delegate.SceneName ?? string.Empty,
                        Direction = "delegate",
                        CreatedAt = ac.CreatedAt
                    })
                    .OrderBy(d => d.SceneName)
                    .ToList(),

                // Principals: people who authorized me (I am the Delegate)
                Principals = contacts
                    .Where(ac => ac.DelegateId == userId)
                    .Select(ac => new AuthorizedContactDto
                    {
                        Id = ac.Id,
                        UserId = ac.PrincipalId,
                        SceneName = ac.Principal.SceneName ?? string.Empty,
                        Direction = "principal",
                        CreatedAt = ac.CreatedAt
                    })
                    .OrderBy(p => p.SceneName)
                    .ToList()
            };

            _logger.LogDebug(
                "Retrieved authorized contacts for user {UserId}: {DelegateCount} delegates, {PrincipalCount} principals",
                userId, result.Delegates.Count, result.Principals.Count);

            return Result<AuthorizedContactsListDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving authorized contacts for user {UserId}", userId);
            return Result<AuthorizedContactsListDto>.Failure(
                "Failed to retrieve authorized contacts",
                ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result<AuthorizedContactDto>> AddContactAsync(Guid principalId, Guid delegateUserId, CancellationToken ct)
    {
        try
        {
            // BR-003: Cannot authorize yourself
            if (principalId == delegateUserId)
            {
                _logger.LogWarning(
                    "User {UserId} attempted self-authorization (BR-003)", principalId);
                return Result<AuthorizedContactDto>.Failure(
                    "Cannot authorize yourself as a contact",
                    "You cannot add yourself as an authorized contact (BR-003)");
            }

            // BR-006: Delegate must have a registered account
            var delegateUser = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == delegateUserId)
                .Select(u => new { u.Id, u.SceneName })
                .FirstOrDefaultAsync(ct);

            if (delegateUser == null)
            {
                _logger.LogWarning(
                    "User {PrincipalId} attempted to authorize non-existent user {DelegateUserId}",
                    principalId, delegateUserId);
                return Result<AuthorizedContactDto>.Failure(
                    "User not found",
                    "The specified user does not exist");
            }

            // Check for existing active authorization (prevent duplicates)
            var existingActive = await _context.AuthorizedContacts
                .AsNoTracking()
                .AnyAsync(ac =>
                    ac.PrincipalId == principalId
                    && ac.DelegateId == delegateUserId
                    && ac.RevokedAt == null, ct);

            if (existingActive)
            {
                _logger.LogWarning(
                    "User {PrincipalId} attempted duplicate authorization for delegate {DelegateUserId}",
                    principalId, delegateUserId);
                return Result<AuthorizedContactDto>.Failure(
                    "Authorization already exists",
                    "This user is already authorized as your contact");
            }

            // Create new authorization using entity constructor (validates BR-003 at entity level too)
            var contact = new AuthorizedContact(principalId, delegateUserId);
            _context.AuthorizedContacts.Add(contact);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Authorized contact created: Principal {PrincipalId} authorized Delegate {DelegateUserId}, ContactId {ContactId}",
                principalId, delegateUserId, contact.Id);

            var dto = new AuthorizedContactDto
            {
                Id = contact.Id,
                UserId = delegateUserId,
                SceneName = delegateUser.SceneName ?? string.Empty,
                Direction = "delegate",
                CreatedAt = contact.CreatedAt
            };

            return Result<AuthorizedContactDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error creating authorized contact: Principal {PrincipalId}, Delegate {DelegateUserId}",
                principalId, delegateUserId);
            return Result<AuthorizedContactDto>.Failure(
                "Failed to create authorized contact",
                ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result> RevokeContactAsync(Guid principalId, Guid contactId, CancellationToken ct)
    {
        try
        {
            // Find the active authorization record
            var contact = await _context.AuthorizedContacts
                .Where(ac => ac.Id == contactId && ac.RevokedAt == null)
                .FirstOrDefaultAsync(ct);

            if (contact == null)
            {
                _logger.LogWarning(
                    "Revoke attempt for non-existent or already revoked contact {ContactId} by user {UserId}",
                    contactId, principalId);
                return Result.Failure(
                    "Authorization not found",
                    "The specified authorization does not exist or has already been revoked");
            }

            // Only the Principal can revoke the authorization
            if (contact.PrincipalId != principalId)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to revoke contact {ContactId} owned by Principal {PrincipalId}",
                    principalId, contactId, contact.PrincipalId);
                return Result.Failure(
                    "Not authorized to revoke",
                    "Only the principal who created this authorization can revoke it");
            }

            // Soft-delete via entity method (BR-005: revocation does not affect existing tickets)
            contact.Revoke("Removed by principal");
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Authorized contact revoked: ContactId {ContactId}, Principal {PrincipalId}, Delegate {DelegateId}",
                contactId, principalId, contact.DelegateId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error revoking authorized contact {ContactId} by user {UserId}",
                contactId, principalId);
            return Result.Failure(
                "Failed to revoke authorization",
                ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result<List<ContactSearchResultDto>>> SearchUsersAsync(Guid currentUserId, string query, CancellationToken ct)
    {
        try
        {
            // Validate minimum query length
            if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
            {
                return Result<List<ContactSearchResultDto>>.Failure(
                    "Query too short",
                    "Search query must be at least 2 characters");
            }

            var trimmedQuery = query.Trim();

            // Get IDs of users already authorized by the current user (active contacts only)
            var alreadyAuthorizedDelegateIds = await _context.AuthorizedContacts
                .AsNoTracking()
                .Where(ac => ac.PrincipalId == currentUserId && ac.RevokedAt == null)
                .Select(ac => ac.DelegateId)
                .ToListAsync(ct);

            // Search users across multiple fields for discoverability, but only
            // return scene name in results for privacy (AD-009).
            // Users can search by: scene name, email, FetLife name, or Discord name.
            // This allows finding someone even if you only know their email or FetLife handle.
            var searchPattern = $"%{trimmedQuery}%";
            var results = await _context.Users
                .AsNoTracking()
                .Where(u =>
                    u.Id != currentUserId
                    && !alreadyAuthorizedDelegateIds.Contains(u.Id)
                    && u.SceneName != null
                    && (EF.Functions.ILike(u.SceneName, searchPattern)
                        || (u.Email != null && EF.Functions.ILike(u.Email, searchPattern))
                        || (u.FetLifeName != null && EF.Functions.ILike(u.FetLifeName, searchPattern))
                        || (u.DiscordName != null && EF.Functions.ILike(u.DiscordName, searchPattern))))
                // Prioritize scene name matches first, then matches from other fields.
                // Users most likely search by scene name, so those should appear at the top.
                // Within each group, sort alphabetically by scene name.
                .OrderBy(u => EF.Functions.ILike(u.SceneName!, searchPattern) ? 0 : 1)
                .ThenBy(u => u.SceneName)
                .Take(10)
                // AD-009: Only return scene name - do NOT expose email, FetLife, or Discord
                // in the results. The search fields are for matching only.
                .Select(u => new ContactSearchResultDto
                {
                    UserId = u.Id,
                    SceneName = u.SceneName ?? string.Empty
                })
                .ToListAsync(ct);

            _logger.LogDebug(
                "User search for '{Query}' by user {UserId}: {ResultCount} results",
                trimmedQuery, currentUserId, results.Count);

            return Result<List<ContactSearchResultDto>>.Success(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error searching users with query '{Query}' for user {UserId}",
                query, currentUserId);
            return Result<List<ContactSearchResultDto>>.Failure(
                "Search failed",
                ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result<List<PrincipalContactDto>>> GetPrincipalsAsync(Guid delegateId, Guid? eventId, CancellationToken ct)
    {
        try
        {
            // Base query: active authorizations where the current user is the Delegate
            var principalsQuery = _context.AuthorizedContacts
                .AsNoTracking()
                .Include(ac => ac.Principal)
                .Where(ac => ac.DelegateId == delegateId && ac.RevokedAt == null);

            // If eventId is provided, apply event-specific filtering
            if (eventId.HasValue)
            {
                var eventEntity = await _context.Events
                    .AsNoTracking()
                    .Where(e => e.Id == eventId.Value)
                    .Select(e => new { e.Id, e.VettedMembersOnly })
                    .FirstOrDefaultAsync(ct);

                if (eventEntity == null)
                {
                    _logger.LogWarning(
                        "GetPrincipals called with non-existent eventId {EventId} by delegate {DelegateId}",
                        eventId.Value, delegateId);
                    return Result<List<PrincipalContactDto>>.Failure(
                        "Event not found",
                        "The specified event does not exist");
                }

                // Get IDs of principals who already have Active or PendingAcceptance attendance
                // for this event (BR-012: one per person per event)
                var principalsWithExistingAttendance = await _context.EventAttendances
                    .AsNoTracking()
                    .Where(ea =>
                        ea.EventId == eventId.Value
                        && (ea.Status == AttendanceStatus.Active
                            || ea.Status == AttendanceStatus.PendingAcceptance))
                    .Select(ea => ea.UserId)
                    .Distinct()
                    .ToListAsync(ct);

                var contacts = await principalsQuery.ToListAsync(ct);

                var filteredResults = contacts
                    .Where(ac =>
                    {
                        // BR-012: Exclude principals who already have attendance for this event
                        if (principalsWithExistingAttendance.Contains(ac.PrincipalId))
                            return false;

                        // BR-035: If VettedMembersOnly, only include vetted principals
                        if (eventEntity.VettedMembersOnly && !ac.Principal.IsVetted)
                            return false;

                        return true;
                    })
                    .Select(ac => new PrincipalContactDto
                    {
                        UserId = ac.PrincipalId,
                        SceneName = ac.Principal.SceneName ?? string.Empty,
                        IsVetted = ac.Principal.IsVetted
                    })
                    .OrderBy(p => p.SceneName)
                    .ToList();

                _logger.LogDebug(
                    "GetPrincipals for delegate {DelegateId}, event {EventId}: {Count} eligible principals (VettedOnly={VettedOnly})",
                    delegateId, eventId.Value, filteredResults.Count, eventEntity.VettedMembersOnly);

                return Result<List<PrincipalContactDto>>.Success(filteredResults);
            }

            // No eventId: return all active principals without event-specific filtering
            // NOTE: IsVetted is [NotMapped] (computed from VettingStatus == 3),
            // so we project VettingStatus and compute IsVetted in the Select expression
            var allPrincipals = await principalsQuery
                .Select(ac => new PrincipalContactDto
                {
                    UserId = ac.PrincipalId,
                    SceneName = ac.Principal.SceneName ?? string.Empty,
                    IsVetted = ac.Principal.VettingStatus == 3
                })
                .OrderBy(p => p.SceneName)
                .ToListAsync(ct);

            _logger.LogDebug(
                "GetPrincipals for delegate {DelegateId} (no event filter): {Count} principals",
                delegateId, allPrincipals.Count);

            return Result<List<PrincipalContactDto>>.Success(allPrincipals);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error getting principals for delegate {DelegateId}, eventId {EventId}",
                delegateId, eventId);
            return Result<List<PrincipalContactDto>>.Failure(
                "Failed to retrieve principals",
                ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsAuthorizedDelegateAsync(Guid principalId, Guid delegateId, CancellationToken ct)
    {
        try
        {
            return await _context.AuthorizedContacts
                .AsNoTracking()
                .AnyAsync(ac =>
                    ac.PrincipalId == principalId
                    && ac.DelegateId == delegateId
                    && ac.RevokedAt == null, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error checking authorization: Principal {PrincipalId}, Delegate {DelegateId}",
                principalId, delegateId);
            return false;
        }
    }
}

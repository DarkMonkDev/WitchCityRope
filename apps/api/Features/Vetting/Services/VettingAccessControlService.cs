using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Vetting.Entities;
using WitchCityRope.Api.Features.Vetting.Models;
using WitchCityRope.Api.Features.Shared.Models;

namespace WitchCityRope.Api.Features.Vetting.Services;

/// <summary>
/// Access control service for vetting workflow system
/// Enforces access restrictions based on vetting status for RSVP and ticket purchases
/// Implements caching and audit logging for performance and compliance
/// </summary>
public class VettingAccessControlService : IVettingAccessControlService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<VettingAccessControlService> _logger;
    private readonly IMemoryCache _cache;

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private const string CacheKeyPrefix = "vetting_access_";

    public VettingAccessControlService(
        ApplicationDbContext context,
        ILogger<VettingAccessControlService> logger,
        IMemoryCache cache)
    {
        _context = context;
        _logger = logger;
        _cache = cache;
    }

    /// <summary>
    /// Checks if user can RSVP to an event based on vetting status
    /// Blocks OnHold (6), Denied (8), and Withdrawn (9) statuses
    /// Returns detailed result with user-friendly messaging
    /// </summary>
    public async Task<Result<AccessControlResult>> CanUserRsvpAsync(
        Guid userId,
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check cache first for performance
            var cacheKey = $"{CacheKeyPrefix}rsvp_{userId}";
            if (_cache.TryGetValue<VettingStatusInfo>(cacheKey, out var cachedStatus))
            {
                var cachedResult = EvaluateRsvpAccess(cachedStatus, userId, eventId);
                _logger.LogDebug("RSVP access check for user {UserId} served from cache: {IsAllowed}",
                    userId, cachedResult.IsAllowed);
                return Result<AccessControlResult>.Success(cachedResult);
            }

            // Get vetting status from database
            var statusInfo = await GetVettingStatusInternalAsync(userId, cancellationToken);

            // Cache the status for 5 minutes
            _cache.Set(cacheKey, statusInfo, CacheDuration);

            // Evaluate access based on vetting status
            var result = EvaluateRsvpAccess(statusInfo, userId, eventId);

            // Log denial events for audit trail
            if (!result.IsAllowed)
            {
                await LogAccessDenialAsync(userId, eventId, "RSVP", result.VettingStatus,
                    result.DenialReason ?? "Unknown reason", cancellationToken);
            }

            return Result<AccessControlResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking RSVP access for user {UserId} and event {EventId}",
                userId, eventId);
            return Result<AccessControlResult>.Failure(
                "Access check failed",
                "Unable to verify RSVP eligibility at this time");
        }
    }

    /// <summary>
    /// Checks if user can purchase tickets based on vetting status
    /// Blocks OnHold (6), Denied (8), and Withdrawn (9) statuses
    /// Returns detailed result with user-friendly messaging
    /// </summary>
    public async Task<Result<AccessControlResult>> CanUserPurchaseTicketAsync(
        Guid userId,
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check cache first for performance
            var cacheKey = $"{CacheKeyPrefix}ticket_{userId}";
            if (_cache.TryGetValue<VettingStatusInfo>(cacheKey, out var cachedStatus))
            {
                var cachedResult = EvaluateTicketPurchaseAccess(cachedStatus, userId, eventId);
                _logger.LogDebug("Ticket purchase access check for user {UserId} served from cache: {IsAllowed}",
                    userId, cachedResult.IsAllowed);
                return Result<AccessControlResult>.Success(cachedResult);
            }

            // Get vetting status from database
            var statusInfo = await GetVettingStatusInternalAsync(userId, cancellationToken);

            // Cache the status for 5 minutes
            _cache.Set(cacheKey, statusInfo, CacheDuration);

            // Evaluate access based on vetting status
            var result = EvaluateTicketPurchaseAccess(statusInfo, userId, eventId);

            // Log denial events for audit trail
            if (!result.IsAllowed)
            {
                await LogAccessDenialAsync(userId, eventId, "TicketPurchase", result.VettingStatus,
                    result.DenialReason ?? "Unknown reason", cancellationToken);
            }

            return Result<AccessControlResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking ticket purchase access for user {UserId} and event {EventId}",
                userId, eventId);
            return Result<AccessControlResult>.Failure(
                "Access check failed",
                "Unable to verify ticket purchase eligibility at this time");
        }
    }

    /// <summary>
    /// Gets user's current vetting status information
    /// Returns status with application details if exists, or default status if no application
    /// </summary>
    public async Task<Result<VettingStatusInfo>> GetUserVettingStatusAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var statusInfo = await GetVettingStatusInternalAsync(userId, cancellationToken);
            return Result<VettingStatusInfo>.Success(statusInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting vetting status for user {UserId}", userId);
            return Result<VettingStatusInfo>.Failure(
                "Failed to retrieve vetting status",
                ex.Message);
        }
    }

    /// <summary>
    /// Internal helper to get vetting status from database
    /// Returns default "no application" status if user has no vetting application
    /// </summary>
    private async Task<VettingStatusInfo> GetVettingStatusInternalAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        // Query database for user's vetting application
        var application = await _context.VettingApplications
            .AsNoTracking()
            .Where(v => v.UserId == userId)
            .Select(v => new
            {
                v.Id,
                v.WorkflowStatus,
                v.SubmittedAt,
                v.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        // If no application exists, default to allowing access (general members can RSVP for public events)
        if (application == null)
        {
            return new VettingStatusInfo
            {
                HasApplication = false,
                Status = null,
                ApplicationId = null,
                SubmittedAt = null,
                LastUpdated = null
            };
        }

        // Return application status information
        return new VettingStatusInfo
        {
            HasApplication = true,
            Status = application.WorkflowStatus,
            ApplicationId = application.Id,
            SubmittedAt = application.SubmittedAt,
            LastUpdated = application.UpdatedAt
        };
    }

    /// <summary>
    /// Evaluates RSVP access based on vetting status
    /// Blocks OnHold (6), Denied (8), and Withdrawn (9)
    /// Allows all other statuses including no application (general members)
    /// </summary>
    private AccessControlResult EvaluateRsvpAccess(
        VettingStatusInfo statusInfo,
        Guid userId,
        Guid eventId)
    {
        // If no application exists, allow RSVP (general members can RSVP for public events)
        if (!statusInfo.HasApplication || !statusInfo.Status.HasValue)
        {
            return new AccessControlResult(
                IsAllowed: true,
                DenialReason: null,
                VettingStatus: null,
                UserMessage: null
            );
        }

        var status = statusInfo.Status.Value;

        // Block access for restricted statuses
        return status switch
        {
            VettingStatus.OnHold => new AccessControlResult(
                IsAllowed: false,
                DenialReason: "Vetting application on hold",
                VettingStatus: status,
                UserMessage: "Your vetting application is on hold. Please contact support@witchcityrope.com to provide additional information and reactivate your application."
            ),
            VettingStatus.Denied => new AccessControlResult(
                IsAllowed: false,
                DenialReason: "Vetting application denied",
                VettingStatus: status,
                UserMessage: "Your vetting application was denied. You cannot RSVP for events at this time."
            ),
            VettingStatus.Withdrawn => new AccessControlResult(
                IsAllowed: false,
                DenialReason: "Vetting application withdrawn",
                VettingStatus: status,
                UserMessage: "You withdrew your vetting application. You may submit a new application to gain access to events."
            ),
            // Allow all other statuses
            _ => new AccessControlResult(
                IsAllowed: true,
                DenialReason: null,
                VettingStatus: status,
                UserMessage: null
            )
        };
    }

    /// <summary>
    /// Evaluates ticket purchase access based on vetting status
    /// Blocks OnHold (6), Denied (8), and Withdrawn (9)
    /// Allows all other statuses including no application (general members)
    /// </summary>
    private AccessControlResult EvaluateTicketPurchaseAccess(
        VettingStatusInfo statusInfo,
        Guid userId,
        Guid eventId)
    {
        // If no application exists, allow ticket purchase (general members can purchase for public events)
        if (!statusInfo.HasApplication || !statusInfo.Status.HasValue)
        {
            return new AccessControlResult(
                IsAllowed: true,
                DenialReason: null,
                VettingStatus: null,
                UserMessage: null
            );
        }

        var status = statusInfo.Status.Value;

        // Block access for restricted statuses (same logic as RSVP)
        return status switch
        {
            VettingStatus.OnHold => new AccessControlResult(
                IsAllowed: false,
                DenialReason: "Vetting application on hold",
                VettingStatus: status,
                UserMessage: "Your application is on hold. Please contact support@witchcityrope.com to reactivate your application."
            ),
            VettingStatus.Denied => new AccessControlResult(
                IsAllowed: false,
                DenialReason: "Vetting application denied",
                VettingStatus: status,
                UserMessage: "Your vetting application was not approved. You cannot purchase tickets at this time."
            ),
            VettingStatus.Withdrawn => new AccessControlResult(
                IsAllowed: false,
                DenialReason: "Vetting application withdrawn",
                VettingStatus: status,
                UserMessage: "You withdrew your vetting application. Please submit a new application if you would like to join the community."
            ),
            // Allow all other statuses
            _ => new AccessControlResult(
                IsAllowed: true,
                DenialReason: null,
                VettingStatus: status,
                UserMessage: null
            )
        };
    }

    /// <summary>
    /// Logs access denial events to audit log for compliance and troubleshooting
    /// Creates VettingAuditLog entry with user ID, event ID, vetting status, and timestamp
    /// </summary>
    private async Task LogAccessDenialAsync(
        Guid userId,
        Guid eventId,
        string accessType,
        VettingStatus? vettingStatus,
        string reason,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get user's vetting application ID if exists
            var applicationId = await _context.VettingApplications
                .AsNoTracking()
                .Where(v => v.UserId == userId)
                .Select(v => v.Id)
                .FirstOrDefaultAsync(cancellationToken);

            // If no application exists, don't log (general member access denial not vetting-related)
            if (applicationId == Guid.Empty)
            {
                _logger.LogWarning("Access denied for user {UserId} to {AccessType} event {EventId} but no vetting application found",
                    userId, accessType, eventId);
                return;
            }

            // Create audit log entry
            var auditLog = new VettingAuditLog
            {
                Id = Guid.NewGuid(),
                ApplicationId = applicationId,
                Action = accessType, // Use accessType directly for consistency with tests (e.g., "RSVP", "TicketPurchase")
                PerformedBy = userId,
                PerformedAt = DateTime.UtcNow,
                OldValue = null,
                NewValue = null,
                Notes = $"Access denied. Vetting status: {vettingStatus}. Reason: {reason}"
            };

            _context.VettingAuditLogs.Add(auditLog);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Access denial logged: User {UserId}, Event {EventId}, Type {AccessType}, Status {VettingStatus}",
                userId, eventId, accessType, vettingStatus);
        }
        catch (Exception ex)
        {
            // Log failure but don't throw - audit logging failure shouldn't block access checks
            _logger.LogError(ex, "Failed to log access denial for user {UserId}, event {EventId}, type {AccessType}",
                userId, eventId, accessType);
        }
    }
}

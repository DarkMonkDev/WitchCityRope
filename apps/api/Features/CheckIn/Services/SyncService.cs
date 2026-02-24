using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.CheckIn.Entities;
using WitchCityRope.Api.Features.CheckIn.Models;
using WitchCityRope.Api.Features.Shared.Models;

namespace WitchCityRope.Api.Features.CheckIn.Services;

/// <summary>
/// Offline sync service implementation
/// Handles network interruptions and data conflicts
/// </summary>
public class SyncService : ISyncService
{
    private readonly ApplicationDbContext _context;
    private readonly ICheckInService _checkInService;
    private readonly ILogger<SyncService> _logger;

    public SyncService(
        ApplicationDbContext context,
        ICheckInService checkInService,
        ILogger<SyncService> logger)
    {
        _context = context;
        _checkInService = checkInService;
        _logger = logger;
    }

    /// <summary>
    /// Process pending offline check-ins with conflict detection
    /// </summary>
    public async Task<Result<SyncResponse>> ProcessOfflineSyncAsync(
        SyncRequest request,
        string sessionToken,
        CancellationToken cancellationToken = default)
    {
        var conflicts = new List<SyncConflict>();
        var processedCount = 0;
        var updatedAttendees = new List<AttendeeResponse>();

        try
        {
            foreach (var pendingCheckIn in request.PendingCheckIns)
            {
                var result = await ProcessPendingCheckInAsync(pendingCheckIn, sessionToken, cancellationToken);

                if (result.IsSuccess)
                {
                    processedCount++;

                    // Get updated attendee information for successful check-ins
                    // Fetch raw data first (SQL translatable)
                    var rawAttendee = await _context.EventAttendees
                        .Include(ea => ea.User)
                        .Include(ea => ea.CheckIns)
                        .Where(ea => ea.Id == Guid.Parse(pendingCheckIn.AttendeeId))
                        .Select(ea => new
                        {
                            ea.Id,
                            ea.UserId,
                            SceneName = ea.User.SceneName ?? string.Empty,
                            Email = ea.User.Email ?? string.Empty,
                            RegistrationStatus = ea.RegistrationStatus, // String from database
                            ea.TicketNumber,
                            CheckInTime = ea.CheckIns.OrderByDescending(c => c.CheckInTime)
                                                     .Select(c => c.CheckInTime.ToString("O"))
                                                     .FirstOrDefault(),
                            ea.IsFirstTime,
                            ea.DietaryRestrictions,
                            ea.AccessibilityNeeds,
                            Pronouns = ea.User.Pronouns,
                            ea.HasCompletedWaiver,
                            ea.WaitlistPosition
                        })
                        .AsNoTracking()
                        .FirstOrDefaultAsync(cancellationToken);

                    // Convert to DTO with enum parsing
                    var attendeeInfo = rawAttendee == null ? null : new AttendeeResponse
                    {
                        AttendeeId = rawAttendee.Id.ToString(),
                        UserId = rawAttendee.UserId.ToString(),
                        SceneName = rawAttendee.SceneName,
                        Email = rawAttendee.Email,
                        RegistrationStatus = ParseRegistrationStatus(rawAttendee.RegistrationStatus),
                        TicketNumber = rawAttendee.TicketNumber,
                        CheckInTime = rawAttendee.CheckInTime,
                        IsFirstTime = rawAttendee.IsFirstTime,
                        DietaryRestrictions = rawAttendee.DietaryRestrictions,
                        AccessibilityNeeds = rawAttendee.AccessibilityNeeds,
                        Pronouns = rawAttendee.Pronouns,
                        HasCompletedWaiver = rawAttendee.HasCompletedWaiver,
                        WaitlistPosition = rawAttendee.WaitlistPosition
                    };

                    if (attendeeInfo != null)
                    {
                        updatedAttendees.Add(attendeeInfo);
                    }
                }
                else
                {
                    // Check for conflicts
                    var conflict = await AnalyzeConflictAsync(pendingCheckIn, result.Error);
                    if (conflict != null)
                    {
                        conflicts.Add(conflict);
                    }
                }
            }

            return Result<SyncResponse>.Success(new SyncResponse
            {
                Success = true,
                ProcessedCount = processedCount,
                Conflicts = conflicts,
                UpdatedAttendees = updatedAttendees,
                NewSyncTimestamp = DateTime.UtcNow.ToString("O")
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during sync processing");
            return Result<SyncResponse>.Failure("Sync processing failed");
        }
    }

    /// <summary>
    /// Queue action for offline processing
    /// </summary>
    public async Task<Result<string>> QueueOfflineActionAsync(
        Guid eventId,
        Guid userId,
        string actionType,
        object actionData,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var queueEntry = new OfflineSyncQueue(
                eventId,
                userId,
                actionType,
                JsonSerializer.Serialize(actionData));

            _context.OfflineSyncQueues.Add(queueEntry);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Queued offline action {ActionType} for user {UserId} on event {EventId}",
                actionType, userId, eventId);

            return Result<string>.Success(queueEntry.Id.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error queuing offline action");
            return Result<string>.Failure("Failed to queue offline action");
        }
    }

    /// <summary>
    /// Get pending sync count for user
    /// </summary>
    public async Task<Result<int>> GetPendingSyncCountAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var count = await _context.OfflineSyncQueues
                // Case-insensitive sync status check for robustness
                .CountAsync(q => q.UserId == userId && q.SyncStatus.ToLower() == "pending", cancellationToken);

            return Result<int>.Success(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending sync count for user {UserId}", userId);
            return Result<int>.Failure("Failed to get pending sync count");
        }
    }

    /// <summary>
    /// Process a single pending check-in
    /// </summary>
    private async Task<Result<CheckInResponse>> ProcessPendingCheckInAsync(
        PendingCheckIn pendingCheckIn,
        string sessionToken,
        CancellationToken cancellationToken)
    {
        try
        {
            var checkInRequest = new CheckInRequest
            {
                AttendeeId = pendingCheckIn.AttendeeId,
                CheckInTime = pendingCheckIn.CheckInTime,
                StaffMemberId = pendingCheckIn.StaffMemberId,
                Notes = pendingCheckIn.Notes,
                IsManualEntry = pendingCheckIn.IsManualEntry,
                ManualEntryData = pendingCheckIn.ManualEntryData
            };

            return await _checkInService.CheckInAttendeeAsync(checkInRequest, sessionToken, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing pending check-in {LocalId}", pendingCheckIn.LocalId);
            return Result<CheckInResponse>.Failure("Failed to process pending check-in");
        }
    }

    /// <summary>
    /// Analyze conflicts when sync fails
    /// </summary>
    private async Task<SyncConflict?> AnalyzeConflictAsync(PendingCheckIn pending, string error)
    {
        try
        {
            // Check for duplicate check-in
            var existingCheckIn = await _context.CheckIns
                .Include(ci => ci.EventAttendee)
                .FirstOrDefaultAsync(ci => ci.EventAttendeeId == Guid.Parse(pending.AttendeeId));

            if (existingCheckIn != null)
            {
                return new SyncConflict
                {
                    LocalId = pending.LocalId,
                    AttendeeId = pending.AttendeeId,
                    ConflictType = "duplicate_checkin",
                    ServerData = new
                    {
                        checkInTime = existingCheckIn.CheckInTime,
                        staffMember = existingCheckIn.StaffMemberId
                    },
                    LocalData = new
                    {
                        checkInTime = pending.CheckInTime,
                        staffMember = pending.StaffMemberId
                    },
                    Resolution = DateTime.Parse(pending.CheckInTime) < existingCheckIn.CheckInTime ?
                        "auto_resolved" : "manual_required",
                    Message = "Attendee was already checked in online"
                };
            }

            // Check for capacity conflicts
            if (error.Contains("capacity"))
            {
                return new SyncConflict
                {
                    LocalId = pending.LocalId,
                    AttendeeId = pending.AttendeeId,
                    ConflictType = "capacity_exceeded",
                    ServerData = null,
                    LocalData = new { checkInTime = pending.CheckInTime },
                    Resolution = "manual_required",
                    Message = "Event capacity was exceeded while offline"
                };
            }

            // Check for attendee not found
            if (error.Contains("not found"))
            {
                return new SyncConflict
                {
                    LocalId = pending.LocalId,
                    AttendeeId = pending.AttendeeId,
                    ConflictType = "attendee_not_found",
                    ServerData = null,
                    LocalData = new
                    {
                        attendeeId = pending.AttendeeId,
                        isManualEntry = pending.IsManualEntry
                    },
                    Resolution = "manual_required",
                    Message = "Attendee was not found in the system"
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing conflict for {LocalId}", pending.LocalId);
            return null;
        }
    }

    /// <summary>
    /// Helper method to convert string registration status to enum
    /// Handles case-insensitive mapping of database string values to RegistrationStatus enum
    /// </summary>
    /// <param name="status">String status from database (confirmed, waitlist, checked-in, no-show)</param>
    /// <returns>Corresponding RegistrationStatus enum value, defaults to Confirmed if unknown</returns>
    private static RegistrationStatus ParseRegistrationStatus(string status)
    {
        return status?.ToLowerInvariant() switch
        {
            "confirmed" => RegistrationStatus.Confirmed,
            "waitlist" => RegistrationStatus.Waitlist,
            "checked-in" or "checkedin" => RegistrationStatus.CheckedIn,
            "no-show" or "noshow" => RegistrationStatus.NoShow,
            _ => RegistrationStatus.Confirmed // Default to Confirmed for unknown values
        };
    }
}
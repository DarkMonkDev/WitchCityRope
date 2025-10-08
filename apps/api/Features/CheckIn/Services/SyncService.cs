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
        CancellationToken cancellationToken = default)
    {
        var conflicts = new List<SyncConflict>();
        var processedCount = 0;
        var updatedAttendees = new List<AttendeeResponse>();

        try
        {
            foreach (var pendingCheckIn in request.PendingCheckIns)
            {
                var result = await ProcessPendingCheckInAsync(pendingCheckIn, cancellationToken);

                if (result.IsSuccess)
                {
                    processedCount++;
                    // TODO: Add to updated attendees list
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
                .CountAsync(q => q.UserId == userId && q.SyncStatus == "pending", cancellationToken);

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

            return await _checkInService.CheckInAttendeeAsync(checkInRequest, cancellationToken);
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
}
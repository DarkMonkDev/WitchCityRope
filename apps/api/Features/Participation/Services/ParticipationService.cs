using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Enums;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Features.Participation.Models;
using WitchCityRope.Api.Features.Shared.Models;
using WitchCityRope.Api.Features.Volunteers.Services;
using WitchCityRope.Api.Features.Events.Interfaces;

namespace WitchCityRope.Api.Features.Participation.Services;

/// <summary>
/// Service for managing event participation (RSVPs and tickets)
/// Follows vertical slice architecture with direct EF access
/// Enforces registration/cancellation cutoff times based on event start time and configured buffer
/// </summary>
public class ParticipationService : IParticipationService
{
    private readonly ApplicationDbContext _context;
    private readonly VolunteerAssignmentService _volunteerAssignmentService;
    private readonly ITimeZoneService _timeZoneService;
    private readonly ILogger<ParticipationService> _logger;

    public ParticipationService(
        ApplicationDbContext context,
        VolunteerAssignmentService volunteerAssignmentService,
        ITimeZoneService timeZoneService,
        ILogger<ParticipationService> logger)
    {
        _context = context;
        _volunteerAssignmentService = volunteerAssignmentService;
        _timeZoneService = timeZoneService;
        _logger = logger;
    }

    /// <summary>
    /// Get user's participation status for a specific event
    /// Returns enhanced DTO with hasRSVP/hasTicket flags and nested details
    /// Matches frontend ParticipationCard component expectations
    /// </summary>
    public async Task<Result<EnhancedParticipationStatusDto?>> GetParticipationStatusAsync(
        Guid eventId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting enhanced participation status for user {UserId} in event {EventId}", userId, eventId);

            // Get event details for capacity calculation
            var eventEntity = await _context.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

            if (eventEntity == null)
            {
                _logger.LogWarning("Event {EventId} not found when fetching participation status", eventId);
                return Result<EnhancedParticipationStatusDto?>.Failure("Event not found");
            }

            // Get all ACTIVE participations for this event (for capacity calculation)
            var activeParticipationsCount = await _context.EventParticipations
                .Where(ep => ep.EventId == eventId && ep.Status == ParticipationStatus.Active)
                .CountAsync(cancellationToken);

            // BUSINESS RULE: Users can have BOTH RSVP and Ticket for social events
            // Query for both participation types separately
            var rsvpParticipation = await _context.EventParticipations
                .AsNoTracking()
                .Where(ep => ep.EventId == eventId &&
                            ep.UserId == userId &&
                            ep.Status == ParticipationStatus.Active &&
                            ep.ParticipationType == ParticipationType.RSVP)
                .FirstOrDefaultAsync(cancellationToken);

            var ticketParticipation = await _context.EventParticipations
                .AsNoTracking()
                .Where(ep => ep.EventId == eventId &&
                            ep.UserId == userId &&
                            ep.Status == ParticipationStatus.Active &&
                            ep.ParticipationType == ParticipationType.Ticket)
                .FirstOrDefaultAsync(cancellationToken);

            // Build enhanced DTO with nested structure
            var dto = new EnhancedParticipationStatusDto
            {
                HasRSVP = rsvpParticipation != null,
                HasTicket = ticketParticipation != null,
                CanRSVP = rsvpParticipation == null && activeParticipationsCount < eventEntity.Capacity,
                CanPurchaseTicket = ticketParticipation == null && activeParticipationsCount < eventEntity.Capacity,
                Capacity = new CapacityInfoDto
                {
                    Current = activeParticipationsCount,
                    Total = eventEntity.Capacity,
                    Available = Math.Max(0, eventEntity.Capacity - activeParticipationsCount)
                }
            };

            // Populate RSVP details if exists
            if (rsvpParticipation != null)
            {
                dto.Rsvp = new RsvpDetailsDto
                {
                    Id = rsvpParticipation.Id,
                    Status = rsvpParticipation.Status.ToString(),
                    CreatedAt = rsvpParticipation.CreatedAt,
                    CanceledAt = rsvpParticipation.CancelledAt,
                    CancelReason = rsvpParticipation.CancellationReason,
                    Notes = rsvpParticipation.Notes
                };
            }

            // Populate Ticket details if exists
            if (ticketParticipation != null)
            {
                // Extract purchase amount from notes JSON (payment details stored in Notes field)
                decimal? amount = null;
                if (!string.IsNullOrWhiteSpace(ticketParticipation.Notes))
                {
                    try
                    {
                        var notesData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(ticketParticipation.Notes);
                        if (notesData != null && notesData.TryGetValue("purchaseAmount", out var amountElement))
                        {
                            amount = amountElement.GetDecimal();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse notes for participation {ParticipationId}", ticketParticipation.Id);
                    }
                }

                dto.Ticket = new TicketDetailsDto
                {
                    Id = ticketParticipation.Id,
                    Status = ticketParticipation.Status.ToString(),
                    Amount = amount,
                    PaymentStatus = ticketParticipation.Status == ParticipationStatus.Active ? "Completed" :
                                   ticketParticipation.Status == ParticipationStatus.Refunded ? "Refunded" : "Unknown",
                    CreatedAt = ticketParticipation.CreatedAt,
                    CanceledAt = ticketParticipation.CancelledAt,
                    CancelReason = ticketParticipation.CancellationReason,
                    Notes = ticketParticipation.Notes
                };
            }

            _logger.LogInformation(
                "Participation status for user {UserId} in event {EventId}: HasRSVP={HasRSVP}, HasTicket={HasTicket}, CanRSVP={CanRSVP}, Capacity={Current}/{Total}",
                userId, eventId, dto.HasRSVP, dto.HasTicket, dto.CanRSVP, dto.Capacity.Current, dto.Capacity.Total);

            return Result<EnhancedParticipationStatusDto?>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting participation status for user {UserId} in event {EventId}", userId, eventId);
            return Result<EnhancedParticipationStatusDto?>.Failure("Failed to get participation status", ex.Message);
        }
    }

    /// <summary>
    /// Create an RSVP for a social event (any authenticated user allowed)
    /// Business Rule: Social events are open to all authenticated users, regardless of vetting status
    /// </summary>
    public async Task<Result<ParticipationStatusDto>> CreateRSVPAsync(
        CreateRSVPRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating RSVP for user {UserId} in event {EventId}", userId, request.EventId);

            // Check if user exists (authentication verified by endpoint authorization)
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user == null)
            {
                return Result<ParticipationStatusDto>.Failure("User not found");
            }

            // REMOVED: Vetting requirement - Social events are open to all authenticated users
            // Previous restrictive validation: if (!user.IsVetted) return Failure("Only vetted members...")
            // New business rule: Allow any authenticated user to RSVP for social events

            // Check if event exists and is a social event
            var eventEntity = await _context.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == request.EventId, cancellationToken);

            if (eventEntity == null)
            {
                return Result<ParticipationStatusDto>.Failure("Event not found");
            }

            if (eventEntity.EventType != EventType.Social)
            {
                return Result<ParticipationStatusDto>.Failure("RSVPs are only allowed for social events");
            }

            // Check if registration is still open based on event start time and buffer
            var isOpen = await _timeZoneService.IsRegistrationOpenAsync(eventEntity.StartDate, cancellationToken);
            if (!isOpen)
            {
                _logger.LogWarning("RSVP attempt for event {EventId} after registration cutoff", request.EventId);
                return Result<ParticipationStatusDto>.Failure("RSVP period has closed for this event");
            }

            // Check if user already has an ACTIVE participation for this event
            // Cancelled RSVPs should not prevent new RSVPs - this allows re-RSVPing
            var existingParticipation = await _context.EventParticipations
                .FirstOrDefaultAsync(ep => ep.EventId == request.EventId && ep.UserId == userId && ep.Status == ParticipationStatus.Active, cancellationToken);

            if (existingParticipation != null)
            {
                return Result<ParticipationStatusDto>.Failure("User already has an active participation for this event");
            }

            // Check event capacity
            var currentParticipationCount = await _context.EventParticipations
                .CountAsync(ep => ep.EventId == request.EventId && ep.Status == ParticipationStatus.Active, cancellationToken);

            if (currentParticipationCount >= eventEntity.Capacity)
            {
                return Result<ParticipationStatusDto>.Failure("Event is at full capacity");
            }

            // Create the RSVP
            var participation = new EventParticipation(request.EventId, userId, ParticipationType.RSVP)
            {
                Notes = request.Notes,
                CreatedBy = userId
            };

            _context.EventParticipations.Add(participation);

            // Create audit history
            var history = new ParticipationHistory(participation.Id, "Created")
            {
                NewValues = System.Text.Json.JsonSerializer.Serialize(new
                {
                    EventId = participation.EventId,
                    UserId = participation.UserId,
                    ParticipationType = participation.ParticipationType,
                    Notes = participation.Notes
                }),
                ChangedBy = userId,
                ChangeReason = "RSVP created by user"
            };

            _context.ParticipationHistory.Add(history);

            // CRITICAL: Save changes to persist RSVP to database
            await _context.SaveChangesAsync(cancellationToken);

            // Verify persistence (defensive check)
            var savedParticipation = await _context.EventParticipations
                .AsNoTracking()
                .FirstOrDefaultAsync(ep => ep.Id == participation.Id, cancellationToken);

            if (savedParticipation == null)
            {
                _logger.LogError("CRITICAL: RSVP {ParticipationId} for user {UserId} in event {EventId} failed to persist to database",
                    participation.Id, userId, request.EventId);
                return Result<ParticipationStatusDto>.Failure("Failed to save RSVP to database");
            }

            _logger.LogInformation("Successfully created and verified RSVP {ParticipationId} for user {UserId} in event {EventId} (Status: {Status})",
                savedParticipation.Id, userId, request.EventId, savedParticipation.Status);

            var dto = new ParticipationStatusDto
            {
                EventId = participation.EventId,
                UserId = participation.UserId,
                ParticipationType = participation.ParticipationType,
                Status = participation.Status,
                ParticipationDate = participation.CreatedAt,
                Notes = participation.Notes,
                CanCancel = participation.CanBeCancelled(),
                Metadata = participation.Metadata
            };

            return Result<ParticipationStatusDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating RSVP for user {UserId} in event {EventId}", userId, request.EventId);
            return Result<ParticipationStatusDto>.Failure("Failed to create RSVP", ex.Message);
        }
    }

    /// <summary>
    /// Purchase a ticket for a class event (any authenticated user)
    /// </summary>
    public async Task<Result<ParticipationStatusDto>> CreateTicketPurchaseAsync(
        CreateTicketPurchaseRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating ticket purchase for user {UserId} in event {EventId}", userId, request.EventId);

            // Check if user exists
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user == null)
            {
                return Result<ParticipationStatusDto>.Failure("User not found");
            }

            // Check if event exists and is a class event
            var eventEntity = await _context.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == request.EventId, cancellationToken);

            if (eventEntity == null)
            {
                return Result<ParticipationStatusDto>.Failure("Event not found");
            }

            // Social events support optional ticket purchases in addition to free RSVPs
            // Class events require ticket purchases
            // Both event types can have tickets

            // Check if registration is still open based on event start time and buffer
            var isOpen = await _timeZoneService.IsRegistrationOpenAsync(eventEntity.StartDate, cancellationToken);
            if (!isOpen)
            {
                _logger.LogWarning("Ticket purchase attempt for event {EventId} after registration cutoff", request.EventId);
                return Result<ParticipationStatusDto>.Failure("Registration has closed for this event");
            }

            // Check if user already has a TICKET for this event
            // Allow ticket purchase even if user has RSVP'd (social events support both)
            var existingTicket = await _context.EventParticipations
                .FirstOrDefaultAsync(ep =>
                    ep.EventId == request.EventId &&
                    ep.UserId == userId &&
                    ep.Status == ParticipationStatus.Active &&
                    ep.ParticipationType == ParticipationType.Ticket,
                    cancellationToken);

            if (existingTicket != null)
            {
                return Result<ParticipationStatusDto>.Failure("User already has a ticket for this event");
            }

            // Check event capacity
            var currentParticipationCount = await _context.EventParticipations
                .CountAsync(ep => ep.EventId == request.EventId && ep.Status == ParticipationStatus.Active, cancellationToken);

            if (currentParticipationCount >= eventEntity.Capacity)
            {
                return Result<ParticipationStatusDto>.Failure("Event is at full capacity");
            }

            // Create the ticket purchase
            var participation = new EventParticipation(request.EventId, userId, ParticipationType.Ticket)
            {
                Notes = request.Notes,
                CreatedBy = userId
            };

            _context.EventParticipations.Add(participation);

            // Create audit history for ticket
            var history = new ParticipationHistory(participation.Id, "Created")
            {
                NewValues = System.Text.Json.JsonSerializer.Serialize(new
                {
                    EventId = participation.EventId,
                    UserId = participation.UserId,
                    ParticipationType = participation.ParticipationType,
                    Notes = participation.Notes,
                    PaymentMethodId = request.PaymentMethodId
                }),
                ChangedBy = userId,
                ChangeReason = "Ticket purchased by user"
            };

            _context.ParticipationHistory.Add(history);

            // BUSINESS RULE: Auto-RSVP for social events when purchasing a ticket
            // If this is a social event and user doesn't already have an RSVP, create one automatically
            if (eventEntity.EventType == EventType.Social)
            {
                var existingRsvp = await _context.EventParticipations
                    .FirstOrDefaultAsync(ep =>
                        ep.EventId == request.EventId &&
                        ep.UserId == userId &&
                        ep.Status == ParticipationStatus.Active &&
                        ep.ParticipationType == ParticipationType.RSVP,
                        cancellationToken);

                if (existingRsvp == null)
                {
                    _logger.LogInformation("Auto-creating RSVP for user {UserId} in social event {EventId} (ticket purchase)", userId, request.EventId);

                    var autoRsvp = new EventParticipation(request.EventId, userId, ParticipationType.RSVP)
                    {
                        Notes = "Auto-created RSVP from ticket purchase",
                        CreatedBy = userId
                    };

                    _context.EventParticipations.Add(autoRsvp);

                    // Create audit history for auto-RSVP
                    var rsvpHistory = new ParticipationHistory(autoRsvp.Id, "Created")
                    {
                        NewValues = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            EventId = autoRsvp.EventId,
                            UserId = autoRsvp.UserId,
                            ParticipationType = autoRsvp.ParticipationType,
                            Notes = autoRsvp.Notes,
                            AutoCreated = true
                        }),
                        ChangedBy = userId,
                        ChangeReason = "Auto-created RSVP from ticket purchase"
                    };

                    _context.ParticipationHistory.Add(rsvpHistory);
                }
            }

            // CRITICAL: Save changes to persist ticket purchase to database
            await _context.SaveChangesAsync(cancellationToken);

            // Verify persistence (defensive check)
            var savedParticipation = await _context.EventParticipations
                .AsNoTracking()
                .FirstOrDefaultAsync(ep => ep.Id == participation.Id, cancellationToken);

            if (savedParticipation == null)
            {
                _logger.LogError("CRITICAL: Ticket purchase {ParticipationId} for user {UserId} in event {EventId} failed to persist to database",
                    participation.Id, userId, request.EventId);
                return Result<ParticipationStatusDto>.Failure("Failed to save ticket purchase to database");
            }

            _logger.LogInformation("Successfully created and verified ticket purchase {ParticipationId} for user {UserId} in event {EventId} (Status: {Status})",
                savedParticipation.Id, userId, request.EventId, savedParticipation.Status);

            var dto = new ParticipationStatusDto
            {
                EventId = participation.EventId,
                UserId = participation.UserId,
                ParticipationType = participation.ParticipationType,
                Status = participation.Status,
                ParticipationDate = participation.CreatedAt,
                Notes = participation.Notes,
                CanCancel = participation.CanBeCancelled(),
                Metadata = participation.Metadata
            };

            return Result<ParticipationStatusDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating ticket purchase for user {UserId} in event {EventId}", userId, request.EventId);
            return Result<ParticipationStatusDto>.Failure("Failed to create ticket purchase", ex.Message);
        }
    }

    /// <summary>
    /// Cancel user's participation in an event
    /// </summary>
    public async Task<Result> CancelParticipationAsync(
        Guid eventId,
        Guid userId,
        ParticipationType? participationType = null,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Cancelling participation for user {UserId} in event {EventId}, Type: {Type}",
                userId, eventId, participationType?.ToString() ?? "Most Recent");

            // Find the ACTIVE participation for cancellation
            // If participationType is specified, filter by that type; otherwise get most recent
            var query = _context.EventParticipations
                .Where(ep => ep.EventId == eventId && ep.UserId == userId && ep.Status == ParticipationStatus.Active);

            if (participationType.HasValue)
            {
                query = query.Where(ep => ep.ParticipationType == participationType.Value);
            }

            var participation = await query
                .OrderByDescending(ep => ep.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (participation == null)
            {
                return Result.Failure("No active participation found for this event");
            }

            if (!participation.CanBeCancelled())
            {
                return Result.Failure("Participation cannot be cancelled in its current status");
            }

            // Check if cancellation is still allowed based on event start time and buffer
            var eventEntity = await _context.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

            if (eventEntity == null)
            {
                return Result.Failure("Event not found");
            }

            var isOpen = await _timeZoneService.IsRegistrationOpenAsync(eventEntity.StartDate, cancellationToken);
            if (!isOpen)
            {
                _logger.LogWarning("Cancellation attempt for event {EventId} after cancellation cutoff", eventId);
                return Result.Failure("Cancellation period has ended for this event");
            }

            // BUSINESS RULE: If cancelling a ticket, also cancel any associated RSVP
            EventParticipation? associatedRsvp = null;
            if (participation.ParticipationType == ParticipationType.Ticket)
            {
                associatedRsvp = await _context.EventParticipations
                    .Where(ep => ep.EventId == eventId &&
                                ep.UserId == userId &&
                                ep.Status == ParticipationStatus.Active &&
                                ep.ParticipationType == ParticipationType.RSVP)
                    .FirstOrDefaultAsync(cancellationToken);

                if (associatedRsvp != null)
                {
                    _logger.LogInformation("Found associated RSVP {RsvpId} - will also cancel when cancelling ticket {TicketId}",
                        associatedRsvp.Id, participation.Id);
                }
            }

            // Store old values for audit
            var oldValues = System.Text.Json.JsonSerializer.Serialize(new
            {
                Status = participation.Status,
                CancelledAt = participation.CancelledAt,
                CancellationReason = participation.CancellationReason
            });

            // Cancel the participation
            participation.Cancel(reason);
            participation.UpdatedBy = userId;

            // Explicitly mark entity as modified to ensure EF Core tracks the change
            _context.EventParticipations.Update(participation);

            // Create audit history
            var history = new ParticipationHistory(participation.Id, "Cancelled")
            {
                OldValues = oldValues,
                NewValues = System.Text.Json.JsonSerializer.Serialize(new
                {
                    Status = participation.Status,
                    CancelledAt = participation.CancelledAt,
                    CancellationReason = participation.CancellationReason
                }),
                ChangedBy = userId,
                ChangeReason = reason ?? "Cancelled by user"
            };

            _context.ParticipationHistory.Add(history);

            // Cancel associated RSVP if exists
            if (associatedRsvp != null)
            {
                var rsvpOldValues = System.Text.Json.JsonSerializer.Serialize(new
                {
                    Status = associatedRsvp.Status,
                    CancelledAt = associatedRsvp.CancelledAt,
                    CancellationReason = associatedRsvp.CancellationReason
                });

                associatedRsvp.Cancel("Auto-cancelled when ticket was cancelled");
                associatedRsvp.UpdatedBy = userId;
                _context.EventParticipations.Update(associatedRsvp);

                var rsvpHistory = new ParticipationHistory(associatedRsvp.Id, "Cancelled")
                {
                    OldValues = rsvpOldValues,
                    NewValues = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        Status = associatedRsvp.Status,
                        CancelledAt = associatedRsvp.CancelledAt,
                        CancellationReason = associatedRsvp.CancellationReason
                    }),
                    ChangedBy = userId,
                    ChangeReason = "Auto-cancelled when ticket was cancelled"
                };

                _context.ParticipationHistory.Add(rsvpHistory);
            }

            // CRITICAL: Save changes to persist cancellation to database
            await _context.SaveChangesAsync(cancellationToken);

            // Verify persistence (defensive check)
            var cancelledParticipation = await _context.EventParticipations
                .AsNoTracking()
                .FirstOrDefaultAsync(ep => ep.Id == participation.Id, cancellationToken);

            if (cancelledParticipation == null)
            {
                _logger.LogError("CRITICAL: Participation {ParticipationId} disappeared after cancellation for user {UserId} in event {EventId}",
                    participation.Id, userId, eventId);
                return Result.Failure("Failed to verify cancellation in database");
            }

            if (cancelledParticipation.Status != ParticipationStatus.Cancelled)
            {
                _logger.LogError("CRITICAL: Participation {ParticipationId} cancellation not persisted - Status is {Status} instead of Cancelled",
                    participation.Id, cancelledParticipation.Status);
                return Result.Failure("Cancellation did not persist to database");
            }

            _logger.LogInformation("Successfully cancelled and verified participation {ParticipationId} for user {UserId} in event {EventId} (Status: {Status}, CancelledAt: {CancelledAt})",
                cancelledParticipation.Id, userId, eventId, cancelledParticipation.Status, cancelledParticipation.CancelledAt);

            // Verify associated RSVP cancellation if it existed
            if (associatedRsvp != null)
            {
                var cancelledRsvp = await _context.EventParticipations
                    .AsNoTracking()
                    .FirstOrDefaultAsync(ep => ep.Id == associatedRsvp.Id, cancellationToken);

                if (cancelledRsvp == null || cancelledRsvp.Status != ParticipationStatus.Cancelled)
                {
                    _logger.LogError("CRITICAL: Associated RSVP {RsvpId} cancellation not persisted properly",
                        associatedRsvp.Id);
                    return Result.Failure("Failed to cancel associated RSVP");
                }

                _logger.LogInformation("Successfully cancelled and verified associated RSVP {RsvpId} (Status: {Status}, CancelledAt: {CancelledAt})",
                    cancelledRsvp.Id, cancelledRsvp.Status, cancelledRsvp.CancelledAt);
            }

            // Auto-cancel volunteer signups when participation is cancelled
            try
            {
                var cancellationResult = await _volunteerAssignmentService.CancelAllVolunteerSignupsForUserEventAsync(
                    userId,
                    eventId,
                    "Refunded Ticket, so automatically canceled volunteer spot",
                    cancellationToken);

                if (cancellationResult.success && cancellationResult.cancelledCount > 0)
                {
                    _logger.LogInformation(
                        "Auto-cancelled {Count} volunteer signups for user {UserId} at event {EventId} due to participation cancellation",
                        cancellationResult.cancelledCount, userId, eventId);
                }
                else if (!cancellationResult.success)
                {
                    _logger.LogWarning(
                        "Failed to auto-cancel volunteer signups for user {UserId} at event {EventId}: {Error}",
                        userId, eventId, cancellationResult.error);
                }
            }
            catch (Exception ex)
            {
                // Log but don't fail the cancellation if volunteer cancellation fails
                _logger.LogError(ex,
                    "Error auto-cancelling volunteer signups for user {UserId} at event {EventId}",
                    userId, eventId);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling participation for user {UserId} in event {EventId}", userId, eventId);
            return Result.Failure("Failed to cancel participation", ex.Message);
        }
    }

    /// <summary>
    /// Get all of user's current participations
    /// </summary>
    public async Task<Result<List<UserParticipationDto>>> GetUserParticipationsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting participations for user {UserId}", userId);

            var participations = await _context.EventParticipations
                .AsNoTracking()
                .Include(ep => ep.Event)
                .Where(ep => ep.UserId == userId)
                .OrderByDescending(ep => ep.CreatedAt)
                .Select(ep => new UserParticipationDto
                {
                    Id = ep.Id,
                    EventId = ep.EventId,
                    EventTitle = ep.Event.Title,
                    EventStartDate = ep.Event.StartDate,
                    EventEndDate = ep.Event.EndDate,
                    EventLocation = ep.Event.Location,
                    ParticipationType = ep.ParticipationType,
                    Status = ep.Status,
                    ParticipationDate = ep.CreatedAt,
                    Notes = ep.Notes,
                    CanCancel = ep.Status == ParticipationStatus.Active
                })
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Found {Count} participations for user {UserId}", participations.Count, userId);

            return Result<List<UserParticipationDto>>.Success(participations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting participations for user {UserId}", userId);
            return Result<List<UserParticipationDto>>.Failure("Failed to get user participations", ex.Message);
        }
    }

    /// <summary>
    /// Get all participations for a specific event (admin only)
    /// </summary>
    public async Task<Result<List<EventParticipationDto>>> GetEventParticipationsAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting participations for event {EventId}", eventId);

            var participations = await _context.EventParticipations
                .AsNoTracking()
                .Include(ep => ep.User)
                .Where(ep => ep.EventId == eventId)
                .OrderByDescending(ep => ep.CreatedAt)
                .Select(ep => new EventParticipationDto
                {
                    Id = ep.Id,
                    UserId = ep.UserId,
                    UserSceneName = ep.User.SceneName ?? ep.User.Email ?? "Unknown",
                    UserEmail = ep.User.Email ?? "",
                    ParticipationType = ep.ParticipationType,
                    Status = ep.Status,
                    ParticipationDate = ep.CreatedAt,
                    Notes = ep.Notes,
                    CanCancel = ep.Status == ParticipationStatus.Active,
                    Metadata = ep.Metadata
                })
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Found {Count} participations for event {EventId}", participations.Count, eventId);

            return Result<List<EventParticipationDto>>.Success(participations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting participations for event {EventId}", eventId);
            return Result<List<EventParticipationDto>>.Failure("Failed to get event participations", ex.Message);
        }
    }
}
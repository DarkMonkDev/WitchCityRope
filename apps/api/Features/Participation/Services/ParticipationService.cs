using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Features.Participation.Models;
using WitchCityRope.Api.Features.Shared.Models;

namespace WitchCityRope.Api.Features.Participation.Services;

/// <summary>
/// Service for managing event participation (RSVPs and tickets)
/// Follows vertical slice architecture with direct EF access
/// </summary>
public class ParticipationService : IParticipationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ParticipationService> _logger;

    public ParticipationService(
        ApplicationDbContext context,
        ILogger<ParticipationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get user's participation status for a specific event
    /// </summary>
    public async Task<Result<ParticipationStatusDto?>> GetParticipationStatusAsync(
        Guid eventId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting participation status for user {UserId} in event {EventId}", userId, eventId);

            var participation = await _context.EventParticipations
                .AsNoTracking()
                .FirstOrDefaultAsync(ep => ep.EventId == eventId && ep.UserId == userId, cancellationToken);

            if (participation == null)
            {
                _logger.LogInformation("No participation found for user {UserId} in event {EventId}", userId, eventId);
                return Result<ParticipationStatusDto?>.Success(null);
            }

            var dto = new ParticipationStatusDto
            {
                EventId = participation.EventId,
                UserId = participation.UserId,
                ParticipationType = participation.ParticipationType,
                Status = participation.Status,
                ParticipationDate = participation.CreatedAt,
                Notes = participation.Notes,
                CanCancel = participation.CanBeCancelled()
            };

            return Result<ParticipationStatusDto?>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting participation status for user {UserId} in event {EventId}", userId, eventId);
            return Result<ParticipationStatusDto?>.Failure("Failed to get participation status", ex.Message);
        }
    }

    /// <summary>
    /// Create an RSVP for a social event (vetted members only)
    /// </summary>
    public async Task<Result<ParticipationStatusDto>> CreateRSVPAsync(
        CreateRSVPRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating RSVP for user {UserId} in event {EventId}", userId, request.EventId);

            // Check if user is vetted
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user == null)
            {
                return Result<ParticipationStatusDto>.Failure("User not found");
            }

            if (!user.IsVetted)
            {
                return Result<ParticipationStatusDto>.Failure("Only vetted members can RSVP for events");
            }

            // Check if event exists and is a social event
            var eventEntity = await _context.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == request.EventId, cancellationToken);

            if (eventEntity == null)
            {
                return Result<ParticipationStatusDto>.Failure("Event not found");
            }

            if (eventEntity.EventType != "Social")
            {
                return Result<ParticipationStatusDto>.Failure("RSVPs are only allowed for social events");
            }

            // Check if user already has a participation for this event
            var existingParticipation = await _context.EventParticipations
                .FirstOrDefaultAsync(ep => ep.EventId == request.EventId && ep.UserId == userId, cancellationToken);

            if (existingParticipation != null)
            {
                return Result<ParticipationStatusDto>.Failure("User already has a participation for this event");
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
                NewValues = System.Text.Json.JsonSerializer.Serialize(new {
                    EventId = participation.EventId,
                    UserId = participation.UserId,
                    ParticipationType = participation.ParticipationType,
                    Notes = participation.Notes
                }),
                ChangedBy = userId,
                ChangeReason = "RSVP created by user"
            };

            _context.ParticipationHistory.Add(history);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully created RSVP for user {UserId} in event {EventId}", userId, request.EventId);

            var dto = new ParticipationStatusDto
            {
                EventId = participation.EventId,
                UserId = participation.UserId,
                ParticipationType = participation.ParticipationType,
                Status = participation.Status,
                ParticipationDate = participation.CreatedAt,
                Notes = participation.Notes,
                CanCancel = participation.CanBeCancelled()
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

            if (eventEntity.EventType != "Class")
            {
                return Result<ParticipationStatusDto>.Failure("Ticket purchases are only allowed for class events");
            }

            // Check if user already has a participation for this event
            var existingParticipation = await _context.EventParticipations
                .FirstOrDefaultAsync(ep => ep.EventId == request.EventId && ep.UserId == userId, cancellationToken);

            if (existingParticipation != null)
            {
                return Result<ParticipationStatusDto>.Failure("User already has a participation for this event");
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

            // Create audit history
            var history = new ParticipationHistory(participation.Id, "Created")
            {
                NewValues = System.Text.Json.JsonSerializer.Serialize(new {
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

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully created ticket purchase for user {UserId} in event {EventId}", userId, request.EventId);

            var dto = new ParticipationStatusDto
            {
                EventId = participation.EventId,
                UserId = participation.UserId,
                ParticipationType = participation.ParticipationType,
                Status = participation.Status,
                ParticipationDate = participation.CreatedAt,
                Notes = participation.Notes,
                CanCancel = participation.CanBeCancelled()
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
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Cancelling participation for user {UserId} in event {EventId}", userId, eventId);

            var participation = await _context.EventParticipations
                .FirstOrDefaultAsync(ep => ep.EventId == eventId && ep.UserId == userId, cancellationToken);

            if (participation == null)
            {
                return Result.Failure("No participation found for this event");
            }

            if (!participation.CanBeCancelled())
            {
                return Result.Failure("Participation cannot be cancelled in its current status");
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

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully cancelled participation for user {UserId} in event {EventId}", userId, eventId);

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
                    CanCancel = ep.Status == ParticipationStatus.Active
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
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Dashboard.Models;
using WitchCityRope.Api.Features.Shared.Models;

namespace WitchCityRope.Api.Features.Dashboard.Services;

/// <summary>
/// Service for user dashboard functionality
/// Extracts user-specific dashboard data from the database
/// Follows vertical slice architecture pattern with direct Entity Framework access
/// </summary>
public class UserDashboardService : IUserDashboardService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserDashboardService> _logger;

    public UserDashboardService(
        ApplicationDbContext context,
        ILogger<UserDashboardService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get basic dashboard data for a user
    /// </summary>
    public async Task<Result<UserDashboardResponse>> GetUserDashboardAsync(
        Guid userId, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting dashboard data for user: {UserId}", userId);

            // Get user with basic info
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("User not found for dashboard request: {UserId}", userId);
                return Result<UserDashboardResponse>.Failure("User not found");
            }

            // Get the user's latest vetting application status
            var latestVettingApp = await _context.VettingApplications
                .AsNoTracking()
                .Where(v => v.ApplicantId == userId)
                .OrderByDescending(v => v.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            var dashboard = new UserDashboardResponse
            {
                SceneName = user.SceneName,
                Role = user.Role,
                VettingStatus = (int)(latestVettingApp?.Status ?? 0), // Default to Draft
                IsVetted = user.IsVetted,
                Email = user.Email ?? string.Empty,
                JoinDate = user.CreatedAt,
                Pronouns = user.Pronouns
            };

            _logger.LogDebug("Dashboard data retrieved successfully for user: {UserId} ({SceneName})", 
                userId, user.SceneName);
            
            return Result<UserDashboardResponse>.Success(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get dashboard data for user: {UserId}", userId);
            return Result<UserDashboardResponse>.Failure("Failed to retrieve dashboard data");
        }
    }

    /// <summary>
    /// Get user's upcoming events (next 3 by default)
    /// </summary>
    public async Task<Result<UserEventsResponse>> GetUserEventsAsync(
        Guid userId, 
        int count = 3, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting upcoming events for user: {UserId}, count: {Count}", userId, count);

            // Get user's upcoming events via TicketPurchases
            var upcomingEvents = await _context.TicketPurchases
                .AsNoTracking()
                .Include(tp => tp.TicketType)
                .ThenInclude(tt => tt!.Event)
                .ThenInclude(e => e.Organizers)
                .Where(tp => tp.UserId == userId &&
                            tp.TicketType!.Event.StartDate > DateTime.UtcNow &&
                            tp.IsPaymentCompleted) // Only include completed purchases
                .OrderBy(tp => tp.TicketType!.Event.StartDate)
                .Take(count)
                .Select(tp => new DashboardEventDto
                {
                    Id = tp.TicketType!.Event.Id,
                    Title = tp.TicketType.Event.Title,
                    StartDate = tp.TicketType.Event.StartDate,
                    EndDate = tp.TicketType.Event.EndDate,
                    Location = tp.TicketType.Event.Location,
                    EventType = tp.TicketType.Event.EventType,
                    InstructorName = tp.TicketType.Event.Organizers.FirstOrDefault() != null 
                        ? tp.TicketType.Event.Organizers.First().SceneName 
                        : "TBD",
                    RegistrationStatus = MapPaymentStatus(tp.PaymentStatus),
                    TicketId = tp.Id,
                    ConfirmationCode = $"WCR-{tp.Id.ToString().Substring(0, 8).ToUpper()}" // Generate confirmation code
                })
                .ToListAsync(cancellationToken);

            var response = new UserEventsResponse
            {
                UpcomingEvents = upcomingEvents
            };

            _logger.LogDebug("Retrieved {EventCount} upcoming events for user: {UserId}", 
                upcomingEvents.Count, userId);
            
            return Result<UserEventsResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get upcoming events for user: {UserId}", userId);
            return Result<UserEventsResponse>.Failure("Failed to retrieve upcoming events");
        }
    }

    /// <summary>
    /// Get user's membership statistics
    /// </summary>
    public async Task<Result<UserStatisticsResponse>> GetUserStatisticsAsync(
        Guid userId, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting membership statistics for user: {UserId}", userId);

            // Get user basic info
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("User not found for statistics request: {UserId}", userId);
                return Result<UserStatisticsResponse>.Failure("User not found");
            }

            // Get events attended (past events with completed payments)
            var eventsAttended = await _context.TicketPurchases
                .AsNoTracking()
                .Include(tp => tp.TicketType)
                .ThenInclude(tt => tt!.Event)
                .Where(tp => tp.UserId == userId &&
                            tp.TicketType!.Event.EndDate < DateTime.UtcNow &&
                            tp.IsPaymentCompleted)
                .CountAsync(cancellationToken);

            // Calculate months as member
            var monthsAsMember = (int)Math.Ceiling((DateTime.UtcNow - user.CreatedAt).TotalDays / 30);

            // Count recent events (last 6 months)
            var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
            var recentEvents = await _context.TicketPurchases
                .AsNoTracking()
                .Include(tp => tp.TicketType)
                .ThenInclude(tt => tt!.Event)
                .Where(tp => tp.UserId == userId &&
                            tp.TicketType!.Event.StartDate > sixMonthsAgo &&
                            tp.IsPaymentCompleted)
                .CountAsync(cancellationToken);

            // Get upcoming registrations
            var upcomingRegistrations = await _context.TicketPurchases
                .AsNoTracking()
                .Include(tp => tp.TicketType)
                .ThenInclude(tt => tt!.Event)
                .Where(tp => tp.UserId == userId &&
                            tp.TicketType!.Event.StartDate > DateTime.UtcNow &&
                            tp.IsPaymentCompleted)
                .CountAsync(cancellationToken);

            // Get cancelled registrations (failed payments or refunded)
            var cancelledRegistrations = await _context.TicketPurchases
                .AsNoTracking()
                .Where(tp => tp.UserId == userId &&
                            (tp.PaymentStatus == "Failed" || 
                             tp.PaymentStatus == "Cancelled" || 
                             tp.PaymentStatus == "Refunded"))
                .CountAsync(cancellationToken);

            // Get vetting info
            var vettingApp = await _context.VettingApplications
                .AsNoTracking()
                .Where(v => v.ApplicantId == userId)
                .OrderByDescending(v => v.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            var stats = new UserStatisticsResponse
            {
                IsVerified = user.IsVetted,
                EventsAttended = eventsAttended,
                MonthsAsMember = monthsAsMember,
                RecentEvents = recentEvents,
                JoinDate = user.CreatedAt,
                VettingStatus = (int)(vettingApp?.Status ?? 0),
                NextInterviewDate = vettingApp?.InterviewScheduledFor, // Will be null if not scheduled
                UpcomingRegistrations = upcomingRegistrations,
                CancelledRegistrations = cancelledRegistrations
            };

            _logger.LogDebug("Membership statistics retrieved for user: {UserId} - Events attended: {EventsAttended}, Months: {MonthsAsMember}", 
                userId, eventsAttended, monthsAsMember);
            
            return Result<UserStatisticsResponse>.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get membership statistics for user: {UserId}", userId);
            return Result<UserStatisticsResponse>.Failure("Failed to retrieve membership statistics");
        }
    }

    /// <summary>
    /// Maps payment status to user-friendly registration status
    /// </summary>
    private static string MapPaymentStatus(string paymentStatus)
    {
        return paymentStatus switch
        {
            "Completed" => "Registered",
            "Confirmed" => "Registered", 
            "Pending" => "Payment Pending",
            "Failed" => "Payment Failed",
            "Cancelled" => "Cancelled",
            "Refunded" => "Refunded",
            _ => "Unknown"
        };
    }
}
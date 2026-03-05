using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.EmailTemplates.Entities;
using WitchCityRope.Api.Features.Shared.Services;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.EmailTemplates.Services;

public class EventEmailService : IEventEmailService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<EventEmailService> _logger;

    private static readonly TimeZoneInfo EasternTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    public EventEmailService(
        ApplicationDbContext context,
        IEmailService emailService,
        ILogger<EventEmailService> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task SendPostPurchaseEmailsAsync(
        Guid userId, List<Guid> ticketPurchaseIds, CancellationToken ct)
    {
        try
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId, ct);

            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found for post-purchase emails", userId);
                return;
            }

            var purchases = await _context.TicketPurchases
                .AsNoTracking()
                .Include(tp => tp.TicketType)
                    .ThenInclude(tt => tt!.Sessions)
                .Include(tp => tp.TicketType)
                    .ThenInclude(tt => tt!.Event)
                        .ThenInclude(e => e!.Venue)
                .Where(tp => ticketPurchaseIds.Contains(tp.Id))
                .ToListAsync(ct);

            if (purchases.Count == 0)
            {
                _logger.LogWarning("No ticket purchases found for IDs [{Ids}]",
                    string.Join(", ", ticketPurchaseIds));
                return;
            }

            // Send confirmation email
            await SendConfirmationAsync(user.Email!, user.UserName ?? user.Email!, purchases, ct);

            // Send catch-up reminders for any already-sent batch reminders
            await SendCatchUpRemindersAsync(user.Email!, user.UserName ?? user.Email!, purchases, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Post-purchase email processing failed (non-fatal). UserId={UserId}, Purchases=[{Ids}]",
                userId, string.Join(", ", ticketPurchaseIds));
        }
    }

    private async Task SendConfirmationAsync(
        string email, string displayName,
        List<TicketPurchase> purchases, CancellationToken ct)
    {
        try
        {
            // Use first purchase for event details (all purchases should be for same event)
            var firstPurchase = purchases[0];
            var ticketType = firstPurchase.TicketType;
            var evt = ticketType?.Event;
            var venue = evt?.Venue;

            // Get first session for date/time
            var firstSession = ticketType?.Sessions.OrderBy(s => s.StartTime).FirstOrDefault();
            var (dateStr, timeStr) = FormatSessionDateTime(firstSession?.StartTime);

            var variables = new Dictionary<string, string>
            {
                ["attendee_name"] = displayName,
                ["event_title"] = evt?.Title ?? "Event",
                ["event_date"] = dateStr,
                ["event_time"] = timeStr,
                ["venue_name"] = venue?.Name ?? "",
                ["venue_address"] = venue?.Location ?? "",
                ["ticket_type"] = ticketType?.Name ?? "",
                ["total_paid"] = purchases.Sum(p => p.TotalPrice).ToString("C"),
                ["confirmation_number"] = firstPurchase.PaymentReference ?? ""
            };

            var result = await _emailService.SendTemplatedEmailAsync(
                email, displayName, EmailCategory.Events, "Confirmation", variables, ct);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Confirmation email sent to {Email} for purchases [{Ids}]",
                    email, string.Join(", ", purchases.Select(p => p.Id)));
            }
            else
            {
                _logger.LogWarning(
                    "Failed to send confirmation email to {Email}: {Error}",
                    email, result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending confirmation email to {Email}", email);
        }
    }

    private async Task SendCatchUpRemindersAsync(
        string email, string displayName,
        List<TicketPurchase> purchases, CancellationToken ct)
    {
        try
        {
            // Collect all sessions covered by the purchased tickets
            var sessionIds = purchases
                .Where(p => p.TicketType?.Sessions != null)
                .SelectMany(p => p.TicketType!.Sessions)
                .Select(s => s.Id)
                .Distinct()
                .ToList();

            if (sessionIds.Count == 0)
                return;

            // Find already-sent batch reminders for these sessions
            var sentLogs = await _context.Set<EmailTriggerLog>()
                .AsNoTracking()
                .Where(log => sessionIds.Contains(log.SessionId!.Value)
                    && log.TriggerType == "TimeBased"
                    && log.Status == "Sent")
                .ToListAsync(ct);

            if (sentLogs.Count == 0)
                return;

            // Get event details for template variables
            var firstPurchase = purchases[0];
            var evt = firstPurchase.TicketType?.Event;
            var venue = evt?.Venue;

            foreach (var log in sentLogs)
            {
                try
                {
                    // Get session start time for this log entry
                    var session = purchases
                        .SelectMany(p => p.TicketType!.Sessions)
                        .FirstOrDefault(s => s.Id == log.SessionId);

                    var (dateStr, timeStr) = FormatSessionDateTime(session?.StartTime);

                    var variables = new Dictionary<string, string>
                    {
                        ["attendee_name"] = displayName,
                        ["event_title"] = evt?.Title ?? "Event",
                        ["event_date"] = dateStr,
                        ["event_time"] = timeStr,
                        ["venue_name"] = venue?.Name ?? "",
                        ["venue_address"] = venue?.Location ?? ""
                    };

                    var result = await _emailService.SendTemplatedEmailAsync(
                        email, displayName, EmailCategory.Events, log.TemplateType, variables, ct);

                    if (result.IsSuccess)
                    {
                        _logger.LogInformation(
                            "Catch-up {TemplateType} sent to {Email} for session {SessionId}",
                            log.TemplateType, email, log.SessionId);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Failed to send catch-up {TemplateType} to {Email}: {Error}",
                            log.TemplateType, email, result.Error);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error sending catch-up {TemplateType} to {Email}",
                        log.TemplateType, email);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing catch-up reminders for {Email}", email);
        }
    }

    private static (string Date, string Time) FormatSessionDateTime(DateTime? startTimeUtc)
    {
        if (!startTimeUtc.HasValue)
            return ("TBD", "TBD");

        var utcTime = DateTime.SpecifyKind(startTimeUtc.Value, DateTimeKind.Utc);
        var localTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, EasternTimeZone);

        return (
            localTime.ToString("dddd, MMMM d, yyyy"),
            localTime.ToString("h:mm tt") + " ET"
        );
    }
}

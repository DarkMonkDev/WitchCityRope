using System.Text;
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

    /// <summary>
    /// Sends a single cancellation email listing all cancelled sessions grouped by ticket type.
    /// Called after CancelTicketPurchasesAsync completes — fire-and-forget pattern,
    /// failures are logged but never propagated to the caller.
    /// </summary>
    public async Task SendCancellationEmailAsync(
        Guid userId, Guid eventId, List<Guid> ticketPurchaseIds, CancellationToken ct)
    {
        try
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId, ct);

            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found for cancellation email", userId);
                return;
            }

            // Load ticket purchases with their ticket types, sessions, and event/venue
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
                _logger.LogWarning("No ticket purchases found for cancellation email. IDs=[{Ids}]",
                    string.Join(", ", ticketPurchaseIds));
                return;
            }

            var displayName = user.UserName ?? user.Email!;
            var evt = purchases[0].TicketType?.Event;
            var venue = evt?.Venue;

            // Build session lists grouped by ticket type (same format as confirmation)
            var (htmlList, textList) = BuildTicketSessionLists(purchases);

            // Use first session for session_date variable
            var firstSession = purchases
                .Where(p => p.TicketType?.Sessions != null)
                .SelectMany(p => p.TicketType!.Sessions)
                .OrderBy(s => s.StartTime)
                .FirstOrDefault();
            var (dateStr, _) = FormatSessionDateTime(firstSession?.StartTime);

            var variables = new Dictionary<string, string>
            {
                ["attendee_name"] = displayName,
                ["event_title"] = evt?.Title ?? "Event",
                ["session_date"] = dateStr,
                ["venue_name"] = venue?.Name ?? "",
                ["venue_address"] = venue?.Location ?? "",
                ["cancelled_sessions_list"] = htmlList,
                ["cancelled_sessions_list_text"] = textList,
                // custom_message left empty for user-initiated cancellations;
                // admin-initiated cancellations can populate this via event template overrides
                ["custom_message"] = ""
            };

            var result = await _emailService.SendTemplatedEmailAsync(
                user.Email!, displayName, EmailCategory.Events, "Cancellation", variables, ct);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Cancellation email sent to {Email} for {Count} ticket purchase(s) in event {EventId}",
                    user.Email, ticketPurchaseIds.Count, eventId);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to send cancellation email to {Email}: {Error}",
                    user.Email, result.Error);
            }
        }
        catch (Exception ex)
        {
            // Fire-and-forget: cancellation email failure must never block the cancellation flow
            _logger.LogError(ex,
                "Error sending cancellation email for user {UserId} event {EventId} (non-fatal)",
                userId, eventId);
        }
    }

    /// <summary>
    /// Sends an RSVP confirmation email for social/free events.
    /// Called only for manual RSVPs — NOT for auto-RSVPs created during ticket purchase
    /// (those users already receive a ticket confirmation email).
    /// </summary>
    public async Task SendRsvpConfirmationEmailAsync(
        Guid userId, Guid eventId, CancellationToken ct)
    {
        try
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId, ct);

            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found for RSVP confirmation email", userId);
                return;
            }

            // Load event with venue for template variables
            var evt = await _context.Events
                .AsNoTracking()
                .Include(e => e.Venue)
                .Include(e => e.Sessions)
                .FirstOrDefaultAsync(e => e.Id == eventId, ct);

            if (evt == null)
            {
                _logger.LogWarning("Event {EventId} not found for RSVP confirmation email", eventId);
                return;
            }

            var displayName = user.UserName ?? user.Email!;

            // Use first session for session_date (social events typically have one session)
            var firstSession = evt.Sessions.OrderBy(s => s.StartTime).FirstOrDefault();
            var (dateStr, timeStr) = FormatSessionDateTime(firstSession?.StartTime);

            var variables = new Dictionary<string, string>
            {
                ["attendee_name"] = displayName,
                ["event_title"] = evt.Title,
                ["session_date"] = dateStr,
                ["session_time"] = timeStr,
                ["venue_name"] = evt.Venue?.Name ?? "",
                ["venue_address"] = evt.Venue?.Location ?? ""
            };

            var result = await _emailService.SendTemplatedEmailAsync(
                user.Email!, displayName, EmailCategory.Events, "RSVPConfirmation", variables, ct);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "RSVP confirmation email sent to {Email} for event {EventId}",
                    user.Email, eventId);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to send RSVP confirmation email to {Email}: {Error}",
                    user.Email, result.Error);
            }
        }
        catch (Exception ex)
        {
            // Fire-and-forget: email failure must never block the RSVP flow
            _logger.LogError(ex,
                "Error sending RSVP confirmation email for user {UserId} event {EventId} (non-fatal)",
                userId, eventId);
        }
    }

    /// <summary>
    /// Sends an RSVP cancellation confirmation email for social/free events.
    /// RSVPs don't have ticket types or session lists, so this is a simpler email
    /// that just confirms the RSVP was cancelled for the event.
    /// </summary>
    public async Task SendRsvpCancellationEmailAsync(
        Guid userId, Guid eventId, CancellationToken ct)
    {
        try
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId, ct);

            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found for RSVP cancellation email", userId);
                return;
            }

            // Load event with venue for template variables
            var evt = await _context.Events
                .AsNoTracking()
                .Include(e => e.Venue)
                .Include(e => e.Sessions)
                .FirstOrDefaultAsync(e => e.Id == eventId, ct);

            if (evt == null)
            {
                _logger.LogWarning("Event {EventId} not found for RSVP cancellation email", eventId);
                return;
            }

            var displayName = user.UserName ?? user.Email!;

            // Use first session for session_date (social events typically have one session)
            var firstSession = evt.Sessions.OrderBy(s => s.StartTime).FirstOrDefault();
            var (dateStr, timeStr) = FormatSessionDateTime(firstSession?.StartTime);

            var variables = new Dictionary<string, string>
            {
                ["attendee_name"] = displayName,
                ["event_title"] = evt.Title,
                ["session_date"] = dateStr,
                ["session_time"] = timeStr,
                ["venue_name"] = evt.Venue?.Name ?? "",
                ["venue_address"] = evt.Venue?.Location ?? "",
                ["custom_message"] = ""
            };

            var result = await _emailService.SendTemplatedEmailAsync(
                user.Email!, displayName, EmailCategory.Events, "RSVPCancellation", variables, ct);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "RSVP cancellation email sent to {Email} for event {EventId}",
                    user.Email, eventId);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to send RSVP cancellation email to {Email}: {Error}",
                    user.Email, result.Error);
            }
        }
        catch (Exception ex)
        {
            // Fire-and-forget: email failure must never block the cancellation flow
            _logger.LogError(ex,
                "Error sending RSVP cancellation email for user {UserId} event {EventId} (non-fatal)",
                userId, eventId);
        }
    }

    private async Task SendConfirmationAsync(
        string email, string displayName,
        List<TicketPurchase> purchases, CancellationToken ct)
    {
        try
        {
            // Use first purchase for event-level details (all purchases should be for same event)
            var firstPurchase = purchases[0];
            var ticketType = firstPurchase.TicketType;
            var evt = ticketType?.Event;
            var venue = evt?.Venue;

            // Get first session for session_date/session_time variables.
            var firstSession = ticketType?.Sessions.OrderBy(s => s.StartTime).FirstOrDefault();
            var (dateStr, timeStr) = FormatSessionDateTime(firstSession?.StartTime);

            // Build the session list grouped by ticket type for multi-session support.
            // Shows each ticket purchased and which sessions it covers.
            var (htmlSessionList, textSessionList) = BuildTicketSessionLists(purchases);

            var variables = new Dictionary<string, string>
            {
                ["attendee_name"] = displayName,
                ["event_title"] = evt?.Title ?? "Event",
                ["session_date"] = dateStr,
                ["session_time"] = timeStr,
                ["venue_name"] = venue?.Name ?? "",
                ["venue_address"] = venue?.Location ?? "",
                ["ticket_type"] = ticketType?.Name ?? "",
                ["total_paid"] = purchases.Sum(p => p.TotalPrice).ToString("C"),
                ["confirmation_number"] = firstPurchase.PaymentReference ?? "",
                ["ticket_sessions_list"] = htmlSessionList,
                ["ticket_sessions_list_text"] = textSessionList
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
                        ["session_date"] = dateStr,
                        ["session_time"] = timeStr,
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

    // ============================================================================
    // SESSION LIST BUILDERS
    // ============================================================================
    // These methods build formatted session lists grouped by ticket type.
    // Used by both confirmation and cancellation emails to show exactly which
    // sessions are covered by each ticket purchase.
    // ============================================================================

    /// <summary>
    /// Builds HTML and plain text session lists grouped by ticket type.
    /// Each ticket type is listed with the sessions it covers underneath.
    /// Example HTML output:
    ///   <strong>Full Weekend Pass</strong>
    ///   <ul>
    ///     <li>Day 1 - Saturday, March 15, 2026 at 2:00 PM ET</li>
    ///     <li>Day 2 - Sunday, March 16, 2026 at 2:00 PM ET</li>
    ///   </ul>
    /// </summary>
    private static (string Html, string PlainText) BuildTicketSessionLists(List<TicketPurchase> purchases)
    {
        var html = new StringBuilder();
        var text = new StringBuilder();

        // Group by ticket type to avoid repeating the same ticket type name
        // when multiple purchases of the same type exist
        var groupedByTicketType = purchases
            .Where(p => p.TicketType != null)
            .GroupBy(p => p.TicketType!.Id)
            .ToList();

        foreach (var group in groupedByTicketType)
        {
            var ticketTypeName = group.First().TicketType!.Name;

            // Collect all unique sessions across all purchases of this ticket type
            var sessions = group
                .SelectMany(p => p.TicketType!.Sessions)
                .DistinctBy(s => s.Id)
                .OrderBy(s => s.StartTime)
                .ToList();

            // HTML format
            html.Append($"<p><strong>{System.Net.WebUtility.HtmlEncode(ticketTypeName)}</strong></p>");
            html.Append("<ul>");
            foreach (var session in sessions)
            {
                var (dateStr, timeStr) = FormatSessionDateTime(session.StartTime);
                var sessionLabel = !string.IsNullOrEmpty(session.Name)
                    ? $"{System.Net.WebUtility.HtmlEncode(session.Name)} - {dateStr} at {timeStr}"
                    : $"{dateStr} at {timeStr}";
                html.Append($"<li>{sessionLabel}</li>");
            }
            html.Append("</ul>");

            // Plain text format
            text.AppendLine(ticketTypeName);
            foreach (var session in sessions)
            {
                var (dateStr, timeStr) = FormatSessionDateTime(session.StartTime);
                var sessionLabel = !string.IsNullOrEmpty(session.Name)
                    ? $"  - {session.Name} - {dateStr} at {timeStr}"
                    : $"  - {dateStr} at {timeStr}";
                text.AppendLine(sessionLabel);
            }
            text.AppendLine();
        }

        return (html.ToString(), text.ToString().TrimEnd());
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

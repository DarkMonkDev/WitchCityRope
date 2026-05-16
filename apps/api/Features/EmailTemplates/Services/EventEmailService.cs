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
    private readonly IConfiguration _configuration;
    private readonly ILogger<EventEmailService> _logger;

    private static readonly TimeZoneInfo EasternTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    /// <summary>
    /// Template types eligible for the post-purchase "catch-up" send.
    ///
    /// When someone buys a ticket late, <see cref="SendCatchUpRemindersAsync"/> replays the
    /// time-based reminders that already went out for their session(s) so a late registrant
    /// is not left out. This allowlist is deliberately restrictive — ONLY attendee-facing
    /// reminders belong here.
    ///
    /// Volunteer templates (VolunteerReminder, VolunteerThankYou) must NEVER be caught up:
    /// they are addressed exclusively to confirmed volunteers and carry volunteer-only merge
    /// fields ({{volunteer_name}}, {{volunteer_tasks_list}}, {{session_name}}) that the
    /// catch-up variable set does not populate. ThankYou / cancellation / session-change
    /// templates are likewise excluded — they are not "reminders you missed".
    ///
    /// History: on 2026-05-16 a plain ticket buyer received a VolunteerReminder email with
    /// unrendered {{...}} placeholders because the catch-up query previously replayed EVERY
    /// time-based "Sent" log regardless of template type. Do NOT widen this set without
    /// confirming the template is attendee-facing AND every merge field it uses is supplied
    /// by the variables dictionary built in SendCatchUpRemindersAsync.
    /// </summary>
    private static readonly HashSet<string> CatchUpEligibleTemplateTypes = new()
    {
        "Reminder1Week",
        "Reminder1Day",
        "Reminder2Hours",
        "RsvpAcceptanceReminder",
        "TicketAcceptanceReminder"
    };

    public EventEmailService(
        ApplicationDbContext context,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<EventEmailService> logger)
    {
        _context = context;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    // ============================================================================
    // EVENT DETAILS URL/BUTTON HELPERS
    // ============================================================================
    // These helpers build a link back to the event details page on the frontend.
    // Templates can use {{event_details_url}} for a plain URL or
    // {{event_details_button}} for a styled HTML button.
    // ============================================================================

    private string GetEventDetailsUrl(Guid eventId)
    {
        var frontendUrl = _configuration["Frontend:Url"]?.TrimEnd('/') ?? "https://witchcityrope.com";
        return $"{frontendUrl}/events/{eventId}";
    }

    private static string GetEventDetailsButton(string url)
    {
        return $"<a href=\"{url}\" style=\"display: inline-block; padding: 12px 24px; background-color: #880124; color: #ffffff; text-decoration: none; border-radius: 6px; font-weight: 600;\">View Event Details</a>";
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
            await SendConfirmationAsync(user.Email!, !string.IsNullOrEmpty(user.SceneName) ? user.SceneName : user.Email!, purchases, ct);

            // Send catch-up reminders for any already-sent batch reminders
            await SendCatchUpRemindersAsync(user.Email!, !string.IsNullOrEmpty(user.SceneName) ? user.SceneName : user.Email!, purchases, ct);
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

            var displayName = !string.IsNullOrEmpty(user.SceneName) ? user.SceneName : user.Email!;
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

            var eventDetailsUrl = GetEventDetailsUrl(eventId);
            var eventDetailsButton = GetEventDetailsButton(eventDetailsUrl);

            var variables = new Dictionary<string, string>
            {
                ["attendee_name"] = displayName,
                ["event_title"] = evt?.Title ?? "Event",
                ["event_title_link"] = !string.IsNullOrEmpty(evt?.Title)
                    ? $"<a href=\"{eventDetailsUrl}\" style=\"color: #880124; text-decoration: underline;\">{System.Net.WebUtility.HtmlEncode(evt.Title)}</a>"
                    : "",
                ["session_date"] = dateStr,
                ["event_date"] = dateStr,
                ["venue_name"] = venue?.Name ?? "",
                ["venue_address"] = venue?.Location ?? "",
                ["cancelled_sessions_list"] = htmlList,
                ["cancelled_sessions_list_text"] = textList,
                // custom_message left empty for user-initiated cancellations;
                // admin-initiated cancellations can populate this via event template overrides
                ["custom_message"] = "",
                ["event_details_url"] = eventDetailsUrl,
                ["event_details_button"] = eventDetailsButton
            };

            // Pass eventId to use event-specific template overrides if configured
            var result = await _emailService.SendTemplatedEmailAsync(
                user.Email!, displayName, EmailCategory.Events, "Cancellation", variables, eventId, ct);

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

            var displayName = !string.IsNullOrEmpty(user.SceneName) ? user.SceneName : user.Email!;

            // Use first session for session_date (social events typically have one session)
            var firstSession = evt.Sessions.OrderBy(s => s.StartTime).FirstOrDefault();
            var (dateStr, timeStr) = FormatSessionDateTime(firstSession?.StartTime);

            // Build session list for multi-session RSVP events
            var sessions = evt.Sessions.OrderBy(s => s.StartTime).ToList();
            var (htmlSessionList, textSessionList) = BuildRsvpSessionList(sessions);

            var eventDetailsUrl = GetEventDetailsUrl(eventId);
            var eventDetailsButton = GetEventDetailsButton(eventDetailsUrl);

            var variables = new Dictionary<string, string>
            {
                ["attendee_name"] = displayName,
                ["event_title"] = evt.Title,
                ["event_title_link"] = $"<a href=\"{eventDetailsUrl}\" style=\"color: #880124; text-decoration: underline;\">{System.Net.WebUtility.HtmlEncode(evt.Title)}</a>",
                ["session_date"] = dateStr,
                ["session_time"] = timeStr,
                ["event_date"] = dateStr,
                ["event_time"] = timeStr,
                ["venue_name"] = evt.Venue?.Name ?? "",
                ["venue_address"] = evt.Venue?.Location ?? "",
                ["event_details_url"] = eventDetailsUrl,
                ["event_details_button"] = eventDetailsButton,
                ["rsvp_sessions_list"] = htmlSessionList,
                ["rsvp_sessions_list_text"] = textSessionList
            };

            // Pass eventId to use event-specific template overrides if configured
            var result = await _emailService.SendTemplatedEmailAsync(
                user.Email!, displayName, EmailCategory.Events, "RSVPConfirmation", variables, eventId, ct);

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

            var displayName = !string.IsNullOrEmpty(user.SceneName) ? user.SceneName : user.Email!;

            // Use first session for session_date (social events typically have one session)
            var firstSession = evt.Sessions.OrderBy(s => s.StartTime).FirstOrDefault();
            var (dateStr, timeStr) = FormatSessionDateTime(firstSession?.StartTime);

            var eventDetailsUrl = GetEventDetailsUrl(eventId);
            var eventDetailsButton = GetEventDetailsButton(eventDetailsUrl);

            var variables = new Dictionary<string, string>
            {
                ["attendee_name"] = displayName,
                ["event_title"] = evt.Title,
                ["event_title_link"] = $"<a href=\"{eventDetailsUrl}\" style=\"color: #880124; text-decoration: underline;\">{System.Net.WebUtility.HtmlEncode(evt.Title)}</a>",
                ["session_date"] = dateStr,
                ["session_time"] = timeStr,
                ["event_date"] = dateStr,
                ["event_time"] = timeStr,
                ["venue_name"] = evt.Venue?.Name ?? "",
                ["venue_address"] = evt.Venue?.Location ?? "",
                ["custom_message"] = "",
                ["event_details_url"] = eventDetailsUrl,
                ["event_details_button"] = eventDetailsButton
            };

            // Pass eventId to use event-specific template overrides if configured
            var result = await _emailService.SendTemplatedEmailAsync(
                user.Email!, displayName, EmailCategory.Events, "RSVPCancellation", variables, eventId, ct);

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

            // Build event details link for the template
            var eventDetailsUrl = evt != null ? GetEventDetailsUrl(evt.Id) : "";
            var eventDetailsButton = !string.IsNullOrEmpty(eventDetailsUrl)
                ? GetEventDetailsButton(eventDetailsUrl) : "";

            // Populate both session_* and event_* aliases for backward compatibility.
            // Production templates may use either variable name depending on when they were
            // seeded or last edited by an admin.
            var variables = new Dictionary<string, string>
            {
                ["attendee_name"] = displayName,
                ["event_title"] = evt?.Title ?? "Event",
                // event_title_link wraps the event title in an anchor tag linking to the
                // event details page. Use in HTML bodies where event_title should be clickable.
                ["event_title_link"] = !string.IsNullOrEmpty(evt?.Title)
                    ? $"<a href=\"{eventDetailsUrl}\" style=\"color: #880124; text-decoration: underline;\">{System.Net.WebUtility.HtmlEncode(evt.Title)}</a>"
                    : "",
                ["session_date"] = dateStr,
                ["session_time"] = timeStr,
                ["event_date"] = dateStr,
                ["event_time"] = timeStr,
                ["venue_name"] = venue?.Name ?? "",
                ["venue_address"] = venue?.Location ?? "",
                ["ticket_type"] = ticketType?.Name ?? "",
                ["total_paid"] = purchases.Sum(p => p.TotalPrice).ToString("C"),
                ["confirmation_number"] = firstPurchase.PaymentReference ?? "",
                ["ticket_sessions_list"] = htmlSessionList,
                ["ticket_sessions_list_text"] = textSessionList,
                ["event_details_url"] = eventDetailsUrl,
                ["event_details_button"] = eventDetailsButton
            };

            // Pass evt?.Id to use event-specific template overrides if configured
            var result = await _emailService.SendTemplatedEmailAsync(
                email, displayName, EmailCategory.Events, "Confirmation", variables, evt?.Id, ct);

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

            // Find already-sent batch reminders for these sessions.
            // CatchUpEligibleTemplateTypes filters to attendee-facing reminders only —
            // a ticket buyer must never be "caught up" on a volunteer-only template
            // (see CatchUpEligibleTemplateTypes remarks for the 2026-05-16 incident).
            var sentLogs = await _context.Set<EmailTriggerLog>()
                .AsNoTracking()
                .Where(log => sessionIds.Contains(log.SessionId!.Value)
                    && log.TriggerType == "TimeBased"
                    && log.Status == "Sent"
                    && CatchUpEligibleTemplateTypes.Contains(log.TemplateType))
                .ToListAsync(ct);

            if (sentLogs.Count == 0)
                return;

            // Get event details for template variables
            var firstPurchase = purchases[0];
            var evt = firstPurchase.TicketType?.Event;
            var venue = evt?.Venue;

            // Build event details link for catch-up reminder templates
            var eventDetailsUrl = evt != null ? GetEventDetailsUrl(evt.Id) : "";
            var eventDetailsButton = !string.IsNullOrEmpty(eventDetailsUrl)
                ? GetEventDetailsButton(eventDetailsUrl) : "";

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
                        ["event_title_link"] = !string.IsNullOrEmpty(evt?.Title)
                            ? $"<a href=\"{eventDetailsUrl}\" style=\"color: #880124; text-decoration: underline;\">{System.Net.WebUtility.HtmlEncode(evt.Title)}</a>"
                            : "",
                        ["session_date"] = dateStr,
                        ["session_time"] = timeStr,
                        ["event_date"] = dateStr,
                        ["event_time"] = timeStr,
                        ["venue_name"] = venue?.Name ?? "",
                        ["venue_address"] = venue?.Location ?? "",
                        ["event_details_url"] = eventDetailsUrl,
                        ["event_details_button"] = eventDetailsButton
                    };

                    // Pass evt?.Id to use event-specific template overrides if configured
                    var result = await _emailService.SendTemplatedEmailAsync(
                        email, displayName, EmailCategory.Events, log.TemplateType, variables, evt?.Id, ct);

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
    /// Builds HTML and plain text session lists for RSVP (social/free) events.
    /// Unlike ticket-based events, RSVP events don't have ticket types, so sessions
    /// are listed directly without grouping.
    /// Example HTML output:
    ///   <ul>
    ///     <li>Session Name - Saturday, March 15, 2026 at 7:00 PM ET</li>
    ///     <li>Sunday, March 16, 2026 at 2:00 PM ET</li>
    ///   </ul>
    /// </summary>
    private static (string Html, string PlainText) BuildRsvpSessionList(IEnumerable<Session> sessions)
    {
        var html = new StringBuilder();
        var text = new StringBuilder();

        html.Append("<ul>");
        foreach (var session in sessions)
        {
            var (dateStr, timeStr) = FormatSessionDateTime(session.StartTime);
            var sessionLabel = !string.IsNullOrEmpty(session.Name)
                ? $"{System.Net.WebUtility.HtmlEncode(session.Name)} - {dateStr} at {timeStr}"
                : $"{dateStr} at {timeStr}";
            html.Append($"<li>{sessionLabel}</li>");

            var textLabel = !string.IsNullOrEmpty(session.Name)
                ? $"  - {session.Name} - {dateStr} at {timeStr}"
                : $"  - {dateStr} at {timeStr}";
            text.AppendLine(textLabel);
        }
        html.Append("</ul>");

        return (html.ToString(), text.ToString().TrimEnd());
    }

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

    // ============================================================================
    // ASSIGNMENT NOTIFICATION HELPERS
    // ============================================================================
    // These helpers build accept URLs and buttons for ticket/RSVP assignment emails.
    // The accept_url links to the event detail page where users can see the event
    // and navigate to their dashboard to accept via PendingTicketsCard.
    // ============================================================================

    /// <summary>
    /// Builds an HTML accept button with customizable text, using the same
    /// WitchCityRope brand styling as GetEventDetailsButton.
    /// </summary>
    private static string GetAcceptButton(string url, string buttonText)
    {
        return $"<a href=\"{url}\" style=\"display: inline-block; padding: 12px 24px; background-color: #880124; color: #ffffff; text-decoration: none; border-radius: 6px; font-weight: 600;\">{buttonText}</a>";
    }

    /// <summary>
    /// Combines venue name and address into a single string for the {{event_venue}} variable.
    /// Returns "Name, Address" if both are present, or whichever is available, or empty string.
    /// </summary>
    private static string FormatVenue(string? name, string? address)
    {
        if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(address))
            return "";
        if (string.IsNullOrEmpty(address))
            return name!;
        if (string.IsNullOrEmpty(name))
            return address;
        return $"{name}, {address}";
    }

    // ============================================================================
    // TICKET & RSVP ASSIGNMENT NOTIFICATION METHODS
    // ============================================================================
    // These are FixedEvent-triggered emails sent immediately when a ticket is
    // assigned to another user or a proxy RSVP is created on someone's behalf.
    // Both follow the fire-and-forget pattern: internal try/catch ensures
    // failures are logged but never propagate to the caller.
    // ============================================================================

    /// <summary>
    /// Sends a ticket assignment notification email to the assignee.
    /// Called after a ticket is successfully assigned to another user via
    /// AssignTicketAsync, ReassignTicketAsync, AssignUnassignedTicketAsync, or AdminAssignTicketAsync.
    /// Loads the attendance record to get ticket type name for the template.
    /// </summary>
    public async Task SendTicketAssignmentNotificationAsync(
        Guid assigneeUserId, Guid assignerUserId, Guid eventId,
        Guid attendanceId, CancellationToken ct)
    {
        try
        {
            // Load assignee (recipient) and assigner (delegate) users
            var assignee = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == assigneeUserId, ct);

            if (assignee?.Email == null)
            {
                _logger.LogWarning("Assignee {UserId} not found for ticket assignment notification", assigneeUserId);
                return;
            }

            var assigner = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == assignerUserId, ct);

            // Load event with venue and sessions for template variables
            var evt = await _context.Events
                .AsNoTracking()
                .Include(e => e.Venue)
                .Include(e => e.Sessions)
                .FirstOrDefaultAsync(e => e.Id == eventId, ct);

            if (evt == null)
            {
                _logger.LogWarning("Event {EventId} not found for ticket assignment notification", eventId);
                return;
            }

            // Load attendance to get ticket purchase and ticket type name
            var attendance = await _context.EventAttendances
                .AsNoTracking()
                .Include(ea => ea.TicketPurchase)
                    .ThenInclude(tp => tp!.TicketType)
                .FirstOrDefaultAsync(ea => ea.Id == attendanceId, ct);

            var ticketTypeName = attendance?.TicketPurchase?.TicketType?.Name ?? "";

            var assigneeDisplayName = !string.IsNullOrEmpty(assignee.SceneName) ? assignee.SceneName : assignee.Email;
            var assignerDisplayName = !string.IsNullOrEmpty(assigner?.SceneName) ? assigner.SceneName : "Someone";

            var firstSession = evt.Sessions.OrderBy(s => s.StartTime).FirstOrDefault();
            var (dateStr, timeStr) = FormatSessionDateTime(firstSession?.StartTime);

            var acceptUrl = GetEventDetailsUrl(eventId);
            var acceptButton = GetAcceptButton(acceptUrl, "Accept Your Ticket");

            var variables = new Dictionary<string, string>
            {
                ["recipient_scene_name"] = assigneeDisplayName,
                ["delegate_scene_name"] = assignerDisplayName,
                ["event_title"] = evt.Title,
                ["event_title_link"] = $"<a href=\"{acceptUrl}\" style=\"color: #880124; text-decoration: underline;\">{System.Net.WebUtility.HtmlEncode(evt.Title)}</a>",
                ["event_date"] = dateStr,
                ["event_time"] = timeStr,
                ["event_venue"] = FormatVenue(evt.Venue?.Name, evt.Venue?.Location),
                ["ticket_type_name"] = ticketTypeName,
                ["accept_url"] = acceptUrl,
                ["accept_button"] = acceptButton
            };

            var result = await _emailService.SendTemplatedEmailAsync(
                assignee.Email, assigneeDisplayName, EmailCategory.Events,
                "TicketAssignmentNotification", variables, eventId, ct);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Ticket assignment notification sent to {Email} for event {EventId} (assigned by {AssignerUserId})",
                    assignee.Email, eventId, assignerUserId);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to send ticket assignment notification to {Email}: {Error}",
                    assignee.Email, result.Error);
            }
        }
        catch (Exception ex)
        {
            // Fire-and-forget: notification failure must never block the assignment flow
            _logger.LogError(ex,
                "Error sending ticket assignment notification for assignee {AssigneeUserId} event {EventId} (non-fatal)",
                assigneeUserId, eventId);
        }
    }

    /// <summary>
    /// Sends an RSVP assignment notification email to the principal (the person being RSVP'd for).
    /// Called after a proxy RSVP is successfully created by a delegate.
    /// </summary>
    public async Task SendRsvpAssignmentNotificationAsync(
        Guid principalUserId, Guid delegateUserId, Guid eventId,
        CancellationToken ct)
    {
        try
        {
            // Load principal (recipient) and delegate (assigner) users
            var principal = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == principalUserId, ct);

            if (principal?.Email == null)
            {
                _logger.LogWarning("Principal {UserId} not found for RSVP assignment notification", principalUserId);
                return;
            }

            var delegateUser = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == delegateUserId, ct);

            // Load event with venue and sessions for template variables
            var evt = await _context.Events
                .AsNoTracking()
                .Include(e => e.Venue)
                .Include(e => e.Sessions)
                .FirstOrDefaultAsync(e => e.Id == eventId, ct);

            if (evt == null)
            {
                _logger.LogWarning("Event {EventId} not found for RSVP assignment notification", eventId);
                return;
            }

            var principalDisplayName = !string.IsNullOrEmpty(principal.SceneName) ? principal.SceneName : principal.Email;
            var delegateDisplayName = !string.IsNullOrEmpty(delegateUser?.SceneName) ? delegateUser.SceneName : "Someone";

            var firstSession = evt.Sessions.OrderBy(s => s.StartTime).FirstOrDefault();
            var (dateStr, timeStr) = FormatSessionDateTime(firstSession?.StartTime);

            var acceptUrl = GetEventDetailsUrl(eventId);
            var acceptButton = GetAcceptButton(acceptUrl, "Accept Your RSVP");

            var variables = new Dictionary<string, string>
            {
                ["recipient_scene_name"] = principalDisplayName,
                ["delegate_scene_name"] = delegateDisplayName,
                ["event_title"] = evt.Title,
                ["event_title_link"] = $"<a href=\"{acceptUrl}\" style=\"color: #880124; text-decoration: underline;\">{System.Net.WebUtility.HtmlEncode(evt.Title)}</a>",
                ["event_date"] = dateStr,
                ["event_time"] = timeStr,
                ["event_venue"] = FormatVenue(evt.Venue?.Name, evt.Venue?.Location),
                ["accept_url"] = acceptUrl,
                ["accept_button"] = acceptButton
            };

            var result = await _emailService.SendTemplatedEmailAsync(
                principal.Email, principalDisplayName, EmailCategory.Events,
                "RsvpAssignmentNotification", variables, eventId, ct);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "RSVP assignment notification sent to {Email} for event {EventId} (created by {DelegateUserId})",
                    principal.Email, eventId, delegateUserId);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to send RSVP assignment notification to {Email}: {Error}",
                    principal.Email, result.Error);
            }
        }
        catch (Exception ex)
        {
            // Fire-and-forget: notification failure must never block the proxy RSVP flow
            _logger.LogError(ex,
                "Error sending RSVP assignment notification for principal {PrincipalUserId} event {EventId} (non-fatal)",
                principalUserId, eventId);
        }
    }
}

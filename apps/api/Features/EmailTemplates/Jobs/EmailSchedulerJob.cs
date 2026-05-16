using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.EmailTemplates.Entities;
using WitchCityRope.Api.Features.EmailTemplates.Services;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Features.Shared.Services;

namespace WitchCityRope.Api.Features.EmailTemplates.Jobs;

/// <summary>
/// Hangfire recurring job that processes time-based email templates.
/// Runs hourly to support 2-hour reminder precision.
/// Uses EmailTriggerLog for idempotency to prevent duplicate sends.
///
/// Handles both attendee templates (Reminder1Week, Reminder1Day, etc.) and
/// volunteer templates (VolunteerReminder, VolunteerThankYou).
/// Volunteer templates receive additional per-recipient variables:
/// volunteer_role, shift_start, shift_end from their VolunteerPosition.
/// </summary>
public class EmailSchedulerJob
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IEventRecipientService _eventRecipientService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailSchedulerJob> _logger;

    // Must match the Hangfire cron interval (hourly = 1 hour)
    private const int SchedulerIntervalHours = 1;

    private static readonly TimeZoneInfo EasternTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    public EmailSchedulerJob(
        ApplicationDbContext context,
        IEmailTemplateService emailTemplateService,
        IEventRecipientService eventRecipientService,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<EmailSchedulerJob> logger)
    {
        _context = context;
        _emailTemplateService = emailTemplateService;
        _eventRecipientService = eventRecipientService;
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

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Email scheduler job started");

        try
        {
            var templatesResult = await _emailTemplateService.GetTimeBasedTemplatesAsync(cancellationToken);
            if (!templatesResult.IsSuccess || templatesResult.Value == null || templatesResult.Value.Count == 0)
            {
                _logger.LogInformation("No time-based templates found. Job complete.");
                return;
            }

            var now = DateTime.UtcNow;
            var templates = templatesResult.Value;

            foreach (var template in templates)
            {
                try
                {
                    await ProcessTemplateAsync(template, now, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error processing template {TemplateType} (ID={TemplateId})",
                        template.TemplateType, template.Id);
                }
            }

            _logger.LogInformation("Email scheduler job completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email scheduler job failed");
            throw; // Let Hangfire mark as failed
        }
    }

    private async Task ProcessTemplateAsync(
        Models.GlobalEmailTemplateDto template, DateTime now, CancellationToken ct)
    {
        var totalOffsetHours = (template.TimingOffsetDays ?? 0) * 24 + (template.TimingOffsetHours ?? 0);

        // Find qualifying sessions based on offset direction.
        // Includes Event.Venue for venue_name/venue_address variables and session Name
        // for session_name variable (used by volunteer templates).
        List<SessionInfo> qualifyingSessions;

        if (totalOffsetHours >= 0)
        {
            // Pre-event: sessions starting within the offset window from now
            var windowStart = now;
            var windowEnd = now.AddHours(totalOffsetHours + SchedulerIntervalHours);

            qualifyingSessions = await _context.Sessions
                .AsNoTracking()
                .Include(s => s.Event)
                    .ThenInclude(e => e!.Venue)
                .Where(s => s.StartTime > windowStart
                    && s.StartTime <= windowEnd
                    && s.Event!.IsPublished)
                .Select(s => new SessionInfo(
                    s.Id, s.EventId, s.StartTime,
                    s.Event!.Title, s.Name,
                    s.Event.Venue != null ? s.Event.Venue.Name : null,
                    s.Event.Venue != null ? s.Event.Venue.Location : null))
                .ToListAsync(ct);
        }
        else
        {
            // Post-event: sessions that ended within the offset window
            var windowStart = now.AddHours(totalOffsetHours - SchedulerIntervalHours);
            var windowEnd = now;

            qualifyingSessions = await _context.Sessions
                .AsNoTracking()
                .Include(s => s.Event)
                    .ThenInclude(e => e!.Venue)
                .Where(s => s.StartTime >= windowStart
                    && s.StartTime < windowEnd
                    && s.Event!.IsPublished)
                .Select(s => new SessionInfo(
                    s.Id, s.EventId, s.StartTime,
                    s.Event!.Title, s.Name,
                    s.Event.Venue != null ? s.Event.Venue.Name : null,
                    s.Event.Venue != null ? s.Event.Venue.Location : null))
                .ToListAsync(ct);
        }

        if (qualifyingSessions.Count == 0)
            return;

        _logger.LogInformation(
            "Template {TemplateType}: Found {Count} qualifying session(s) with offset {Offset}h",
            template.TemplateType, qualifyingSessions.Count, totalOffsetHours);

        foreach (var session in qualifyingSessions)
        {
            await ProcessSessionAsync(template, session, ct);
        }
    }

    private async Task ProcessSessionAsync(
        Models.GlobalEmailTemplateDto template, SessionInfo session, CancellationToken ct)
    {
        // Idempotency + bounded-retry check.
        //
        // This job runs hourly. A reminder is considered "handled" — and therefore skipped —
        // when either:
        //   1. it has already sent successfully ("Sent"), OR
        //   2. it has failed MaxSendAttempts times.
        //
        // Case 2 is the bounded-retry guard. Without it, a template that fails to send keeps
        // re-attempting on every hourly run for the entire remaining send window, because a
        // "Failed" log row never satisfied the old "Status == Sent" check. This produced ~25
        // hourly "Failed" rows for a single April reminder before the guard existed
        // (see tech-debt BE-16). Three attempts gives transient SendGrid hiccups room to
        // recover while stopping a persistent failure from looping.
        const int MaxSendAttempts = 3;

        var priorStatuses = await _context.Set<EmailTriggerLog>()
            .AsNoTracking()
            .Where(log =>
                log.TemplateId == template.Id
                && log.SessionId == session.Id
                && log.TemplateType == template.TemplateType)
            .Select(log => log.Status)
            .ToListAsync(ct);

        if (priorStatuses.Contains("Sent"))
        {
            _logger.LogDebug(
                "Template {TemplateType} already sent for session {SessionId}, skipping",
                template.TemplateType, session.Id);
            return;
        }

        var failedAttempts = priorStatuses.Count(status => status == "Failed");
        if (failedAttempts >= MaxSendAttempts)
        {
            _logger.LogWarning(
                "Template {TemplateType} for session {SessionId} has failed {FailedAttempts} time(s) " +
                "(>= max {MaxSendAttempts}); giving up — no further retries this send window",
                template.TemplateType, session.Id, failedAttempts, MaxSendAttempts);
            return;
        }

        // Resolve recipients
        if (!template.RecipientGroup.HasValue)
        {
            _logger.LogWarning(
                "Template {TemplateType} has no recipient group configured, skipping",
                template.TemplateType);
            return;
        }

        var recipients = await _eventRecipientService.GetRecipientsAsync(
            template.RecipientGroup.Value, session.Id, ct);

        if (recipients.Count == 0)
        {
            _logger.LogInformation(
                "No recipients for {TemplateType} session {SessionId}, logging as Skipped",
                template.TemplateType, session.Id);

            _context.Set<EmailTriggerLog>().Add(new EmailTriggerLog
            {
                TemplateId = template.Id,
                EventId = session.EventId,
                SessionId = session.Id,
                TemplateType = template.TemplateType,
                TriggerType = "TimeBased",
                RecipientGroup = template.RecipientGroup.Value.ToString(),
                RecipientCount = 0,
                TriggeredAt = DateTime.UtcNow,
                Status = "Skipped"
            });
            await _context.SaveChangesAsync(ct);
            return;
        }

        // Build base template variables (shared across all recipient groups)
        var utcTime = DateTime.SpecifyKind(session.StartTime, DateTimeKind.Utc);
        var localTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, EasternTimeZone);

        var isVolunteerTemplate = template.RecipientGroup.Value == EventRecipientGroup.SessionVolunteers;
        var isPendingAssignment = template.RecipientGroup.Value == EventRecipientGroup.PendingAssignmentHolders;

        var successCount = 0;
        var failCount = 0;

        foreach (var recipient in recipients)
        {
            try
            {
                var formattedDate = localTime.ToString("dddd, MMMM d, yyyy");
                var formattedTime = localTime.ToString("h:mm tt") + " ET";

                // PendingAssignmentHolders branch: builds assignment-specific variables
                // for TicketAcceptanceReminder and RsvpAcceptanceReminder templates.
                // These templates use different variable names (recipient_scene_name,
                // delegate_scene_name, accept_button, etc.) than the generic templates.
                if (isPendingAssignment && recipient.Assignment != null)
                {
                    // Filter by attendance type: TicketAcceptanceReminder only sends to
                    // Ticket holders, RsvpAcceptanceReminder only to RSVP holders.
                    // Both templates share the PendingAssignmentHolders recipient group,
                    // so we filter here to ensure each user gets the correct template.
                    var expectedAttendanceType = template.TemplateType == "TicketAcceptanceReminder"
                        ? AttendanceType.Ticket
                        : AttendanceType.RSVP;

                    if (recipient.Assignment.AttendanceType != expectedAttendanceType)
                        continue;

                    var acceptUrl = GetEventDetailsUrl(session.EventId);
                    var buttonLabel = recipient.Assignment.AttendanceType == AttendanceType.Ticket
                        ? "Accept Your Ticket Now"
                        : "Accept Your RSVP Now";
                    var acceptButton = $"<a href=\"{acceptUrl}\" style=\"display: inline-block; padding: 12px 24px; background-color: #880124; color: #ffffff; text-decoration: none; border-radius: 6px; font-weight: 600;\">{buttonLabel}</a>";

                    // Combine venue name + address into single event_venue variable
                    var eventVenue = !string.IsNullOrEmpty(session.VenueName) && !string.IsNullOrEmpty(session.VenueAddress)
                        ? $"{session.VenueName}, {session.VenueAddress}"
                        : session.VenueName ?? session.VenueAddress ?? "";

                    var assignmentVariables = new Dictionary<string, string>
                    {
                        ["recipient_scene_name"] = recipient.DisplayName,
                        ["delegate_scene_name"] = recipient.Assignment.DelegateSceneName,
                        ["event_title"] = session.EventTitle,
                        // event_title_link wraps the event title in an anchor tag linking to
                        // the event details page for clickable event names in HTML bodies.
                        ["event_title_link"] = !string.IsNullOrEmpty(session.EventTitle)
                            ? $"<a href=\"{acceptUrl}\" style=\"color: #880124; text-decoration: underline;\">{System.Net.WebUtility.HtmlEncode(session.EventTitle)}</a>"
                            : "",
                        ["event_date"] = formattedDate,
                        ["event_time"] = formattedTime,
                        ["event_start_time"] = formattedTime,
                        ["event_venue"] = eventVenue,
                        ["ticket_type_name"] = recipient.Assignment.TicketTypeName ?? "",
                        ["accept_url"] = acceptUrl,
                        ["accept_button"] = acceptButton
                    };

                    var assignResult = await _emailService.SendTemplatedEmailAsync(
                        recipient.Email, recipient.DisplayName,
                        EmailCategory.Events, template.TemplateType, assignmentVariables, session.EventId, ct);

                    if (assignResult.IsSuccess)
                        successCount++;
                    else
                        failCount++;

                    continue; // Skip generic variable building below
                }

                // Generic template variable building for all other recipient groups
                // (RSVPTicketHolders, SessionAttendees, SessionVolunteers, Teachers).
                // Populate both session_* and event_* variable names.
                // Production templates were originally seeded with event_date/event_time,
                // while the seeder was later updated to use session_date/session_time.
                // Both aliases are needed so templates work regardless of which variable
                // name the admin used when editing.
                var eventDetailsUrl = GetEventDetailsUrl(session.EventId);
                var eventDetailsButton = GetEventDetailsButton(eventDetailsUrl);

                var variables = new Dictionary<string, string>
                {
                    ["attendee_name"] = recipient.DisplayName,
                    ["event_title"] = session.EventTitle,
                    ["session_date"] = formattedDate,
                    ["session_time"] = formattedTime,
                    ["event_date"] = formattedDate,
                    ["event_time"] = formattedTime,
                    ["venue_name"] = session.VenueName ?? "",
                    ["venue_address"] = session.VenueAddress ?? "",
                    ["session_name"] = session.SessionName ?? "",
                    // session_name_link wraps the session name in an anchor tag linking to the
                    // event details page. Use in HTML bodies where session_name should be clickable.
                    // Plain text bodies and subjects should continue using {{session_name}}.
                    ["session_name_link"] = !string.IsNullOrEmpty(session.SessionName)
                        ? $"<a href=\"{eventDetailsUrl}\" style=\"color: #880124; text-decoration: underline;\">{System.Net.WebUtility.HtmlEncode(session.SessionName)}</a>"
                        : "",
                    // event_title_link wraps the event title in an anchor tag linking to the
                    // event details page. Use in HTML bodies where event_title should be clickable.
                    // Plain text bodies and subjects should continue using {{event_title}}.
                    ["event_title_link"] = !string.IsNullOrEmpty(session.EventTitle)
                        ? $"<a href=\"{eventDetailsUrl}\" style=\"color: #880124; text-decoration: underline;\">{System.Net.WebUtility.HtmlEncode(session.EventTitle)}</a>"
                        : "",
                    ["event_details_url"] = eventDetailsUrl,
                    ["event_details_button"] = eventDetailsButton
                };

                if (isVolunteerTemplate && recipient.VolunteerAssignments is { Count: > 0 })
                {
                    variables["volunteer_name"] = recipient.DisplayName;

                    // Build a bulleted HTML list of all volunteer assignments for this session.
                    // Each assignment shows the role title and shift times (if set).
                    // This ensures the volunteer sees ALL their tasks in a single email.
                    var htmlItems = recipient.VolunteerAssignments.Select(a =>
                    {
                        var shift = a.ShiftStart != null && a.ShiftEnd != null
                            ? $": {a.ShiftStart} - {a.ShiftEnd}"
                            : "";
                        return $"<li>{a.Role}{shift}</li>";
                    });
                    variables["volunteer_tasks_list"] = $"<ul>{string.Join("", htmlItems)}</ul>";

                    // Plain text version for plain text email body
                    var textItems = recipient.VolunteerAssignments.Select(a =>
                    {
                        var shift = a.ShiftStart != null && a.ShiftEnd != null
                            ? $": {a.ShiftStart} - {a.ShiftEnd}"
                            : "";
                        return $"  • {a.Role}{shift}";
                    });
                    variables["volunteer_tasks_list_text"] = string.Join("\n", textItems);
                }

                // Pass session.EventId to use event-specific template overrides if configured
                var result = await _emailService.SendTemplatedEmailAsync(
                    recipient.Email, recipient.DisplayName,
                    EmailCategory.Events, template.TemplateType, variables, session.EventId, ct);

                if (result.IsSuccess)
                    successCount++;
                else
                    failCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to send {TemplateType} to {Email} for session {SessionId}",
                    template.TemplateType, recipient.Email, session.Id);
                failCount++;
            }
        }

        // Log the trigger
        var status = successCount > 0 ? "Sent" : "Failed";
        var triggerLog = new EmailTriggerLog
        {
            TemplateId = template.Id,
            EventId = session.EventId,
            SessionId = session.Id,
            TemplateType = template.TemplateType,
            TriggerType = "TimeBased",
            RecipientGroup = template.RecipientGroup.Value.ToString(),
            RecipientCount = recipients.Count,
            TriggeredAt = DateTime.UtcNow,
            SentAt = successCount > 0 ? DateTime.UtcNow : null,
            Status = status,
            ErrorMessage = failCount > 0 ? $"{failCount} of {recipients.Count} sends failed" : null
        };
        _context.Set<EmailTriggerLog>().Add(triggerLog);

        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is Npgsql.PostgresException { SqlState: "23505" })
        {
            // Idempotency race: a concurrent scheduler run already wrote a "Sent" row for
            // this (TemplateId, SessionId, TemplateType) tuple, and the unique filtered index
            // UQ_EmailTriggerLogs_Idempotency (filter: SessionId IS NOT NULL AND Status = 'Sent')
            // rejected ours. Swallow it: the reminder is already accounted for, and without
            // this catch the exception would abort ProcessTemplateAsync's loop over the
            // remaining sessions. Detach the rejected entity so the next session's
            // SaveChangesAsync on this shared DbContext doesn't retry the failed INSERT.
            //
            // NOTE: this does NOT prevent a duplicate *send* — if two runs both passed the
            // idempotency check above, both already emailed before reaching this point. True
            // prevention requires single-instance job execution (see tech-debt BE-16).
            _context.Entry(triggerLog).State = EntityState.Detached;
            _logger.LogWarning(
                "Idempotency race: {TemplateType} for session {SessionId} was already logged "
                + "as Sent by a concurrent run; duplicate trigger log discarded",
                template.TemplateType, session.Id);
        }

        _logger.LogInformation(
            "{TemplateType} for session {SessionId}: {Success} sent, {Failed} failed out of {Total} recipients",
            template.TemplateType, session.Id, successCount, failCount, recipients.Count);
    }

    /// <summary>
    /// Session info record including venue and session name for template variable population.
    /// VenueName and VenueAddress are nullable because events may not have a venue assigned.
    /// SessionName is nullable because single-session events may not have a named session.
    /// </summary>
    private record SessionInfo(
        Guid Id,
        Guid EventId,
        DateTime StartTime,
        string EventTitle,
        string? SessionName,
        string? VenueName,
        string? VenueAddress);
}

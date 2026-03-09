using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.EmailTemplates.Entities;
using WitchCityRope.Api.Features.EmailTemplates.Services;
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
        ILogger<EmailSchedulerJob> logger)
    {
        _context = context;
        _emailTemplateService = emailTemplateService;
        _eventRecipientService = eventRecipientService;
        _emailService = emailService;
        _logger = logger;
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
        // Idempotency check
        var alreadySent = await _context.Set<EmailTriggerLog>()
            .AnyAsync(log =>
                log.TemplateId == template.Id
                && log.SessionId == session.Id
                && log.TemplateType == template.TemplateType
                && log.Status == "Sent",
                ct);

        if (alreadySent)
        {
            _logger.LogDebug(
                "Template {TemplateType} already sent for session {SessionId}, skipping",
                template.TemplateType, session.Id);
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

        var successCount = 0;
        var failCount = 0;

        foreach (var recipient in recipients)
        {
            try
            {
                // Build variables dictionary for this recipient.
                // All templates get the base event/venue variables.
                // Volunteer templates additionally get role and shift details
                // from the RecipientInfo (populated by EventRecipientService).
                var variables = new Dictionary<string, string>
                {
                    ["attendee_name"] = recipient.DisplayName,
                    ["event_title"] = session.EventTitle,
                    ["session_date"] = localTime.ToString("dddd, MMMM d, yyyy"),
                    ["session_time"] = localTime.ToString("h:mm tt") + " ET",
                    // venue_name and venue_address were previously missing from scheduler-sent
                    // emails despite being declared in reminder template Variables JSON.
                    // Now included for all templates.
                    ["venue_name"] = session.VenueName ?? "",
                    ["venue_address"] = session.VenueAddress ?? "",
                    ["session_name"] = session.SessionName ?? ""
                };

                if (isVolunteerTemplate)
                {
                    // Volunteer-specific variables from the recipient's VolunteerPosition
                    variables["volunteer_name"] = recipient.DisplayName;
                    variables["volunteer_role"] = recipient.VolunteerRole ?? "";
                    variables["shift_start"] = recipient.ShiftStart ?? "TBD";
                    variables["shift_end"] = recipient.ShiftEnd ?? "TBD";
                }

                var result = await _emailService.SendTemplatedEmailAsync(
                    recipient.Email, recipient.DisplayName,
                    EmailCategory.Events, template.TemplateType, variables, ct);

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
        _context.Set<EmailTriggerLog>().Add(new EmailTriggerLog
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
        });
        await _context.SaveChangesAsync(ct);

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

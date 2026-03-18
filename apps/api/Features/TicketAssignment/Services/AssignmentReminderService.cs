using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.EmailTemplates.Entities;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Features.Shared.Services;

namespace WitchCityRope.Api.Features.TicketAssignment.Services;

/// <summary>
/// Implementation of IAssignmentReminderService that queries for unaccepted
/// ticket/RSVP assignments approaching their event date and sends reminder emails.
///
/// Uses the partial index IX_EventAttendances_Status_ReminderSentAt for efficient
/// queries against PendingAcceptance records without reminders sent.
///
/// Email sending is fire-and-forget per individual recipient; failures are logged
/// but do not block other reminders from being sent. ReminderSentAt is only set
/// after a successful email send attempt to allow retry on next job run.
///
/// Dependencies:
/// - ApplicationDbContext: Data access for EventAttendance, Sessions, Users
/// - IEmailService: SendGrid-based email sending via GlobalEmailTemplates
/// - IConfiguration: Frontend URL for accept links in emails
/// - ILogger: Structured logging with contextual properties
/// </summary>
public class AssignmentReminderService : IAssignmentReminderService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AssignmentReminderService> _logger;

    private static readonly TimeZoneInfo EasternTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    public AssignmentReminderService(
        ApplicationDbContext context,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<AssignmentReminderService> logger)
    {
        _context = context;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SendPendingAssignmentRemindersAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var cutoff = now.AddHours(24);

        _logger.LogInformation(
            "Assignment reminder job started. Checking for PendingAcceptance assignments with events between {Now} and {Cutoff}",
            now, cutoff);

        // Query PendingAcceptance attendances where:
        // - ReminderSentAt IS NULL (BR-062: one reminder per assignment)
        // - The event has at least one session starting within 24 hours
        // Uses the partial index IX_EventAttendances_Status_ReminderSentAt
        var pendingAttendances = await _context.EventAttendances
            .Include(ea => ea.Event)
                .ThenInclude(e => e.Sessions)
            .Include(ea => ea.Event)
                .ThenInclude(e => e.Venue)
            .Include(ea => ea.User)
            .Include(ea => ea.AssignedByUser)
            .Include(ea => ea.TicketPurchase)
                .ThenInclude(tp => tp!.TicketType)
            .Where(ea =>
                ea.Status == AttendanceStatus.PendingAcceptance
                && ea.ReminderSentAt == null
                && ea.Event.Sessions.Any(s => s.StartTime > now && s.StartTime <= cutoff))
            .ToListAsync(ct);

        if (pendingAttendances.Count == 0)
        {
            _logger.LogInformation("No pending assignment reminders to send");
            return;
        }

        _logger.LogInformation(
            "Found {Count} pending assignments needing reminders",
            pendingAttendances.Count);

        var successCount = 0;
        var failCount = 0;

        foreach (var attendance in pendingAttendances)
        {
            try
            {
                var sent = await SendReminderForAttendanceAsync(attendance, ct);

                if (sent)
                {
                    // Mark reminder as sent (BR-062: prevents duplicate sends)
                    attendance.ReminderSentAt = DateTime.UtcNow;
                    attendance.UpdatedAt = DateTime.UtcNow;

                    // Create AttendanceHistory "ReminderSent" record for audit trail
                    var history = new AttendanceHistory(attendance.Id, "ReminderSent")
                    {
                        ChangeReason = "24-hour reminder email sent for pending assignment",
                        NewValues = JsonSerializer.Serialize(new
                        {
                            ReminderSentAt = attendance.ReminderSentAt,
                            UserId = attendance.UserId,
                            EventId = attendance.EventId,
                            AttendanceType = attendance.AttendanceType.ToString()
                        })
                    };
                    _context.AttendanceHistory.Add(history);

                    successCount++;
                }
                else
                {
                    failCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error sending reminder for attendance {AttendanceId} to user {UserId} for event {EventTitle}",
                    attendance.Id, attendance.UserId, attendance.Event?.Title);
                failCount++;
            }
        }

        // Batch save all ReminderSentAt updates and history records
        if (successCount > 0)
        {
            await _context.SaveChangesAsync(ct);
        }

        _logger.LogInformation(
            "Assignment reminder job completed. Sent {SuccessCount} reminders, {FailCount} failed",
            successCount, failCount);
    }

    /// <summary>
    /// Sends a reminder email for a single pending assignment.
    /// Determines the correct template type based on AttendanceType (Ticket vs RSVP).
    /// Returns true if the email was sent successfully.
    /// </summary>
    private async Task<bool> SendReminderForAttendanceAsync(
        EventAttendance attendance, CancellationToken ct)
    {
        var user = attendance.User;
        var assignedByUser = attendance.AssignedByUser;
        var eventEntity = attendance.Event;

        if (user == null || eventEntity == null)
        {
            _logger.LogWarning(
                "Skipping reminder for attendance {AttendanceId}: missing user or event data",
                attendance.Id);
            return false;
        }

        // Determine template type based on attendance type
        var templateType = attendance.AttendanceType == AttendanceType.Ticket
            ? AssignmentEmailTemplates.TicketAcceptanceReminder
            : AssignmentEmailTemplates.RsvpAcceptanceReminder;

        // Find the earliest session start time for the event (for urgency display)
        var earliestSession = eventEntity.Sessions
            .OrderBy(s => s.StartTime)
            .FirstOrDefault();

        if (earliestSession == null)
        {
            _logger.LogWarning(
                "Skipping reminder for attendance {AttendanceId}: event {EventId} has no sessions",
                attendance.Id, eventEntity.Id);
            return false;
        }

        // Build email template variables
        var utcTime = DateTime.SpecifyKind(earliestSession.StartTime, DateTimeKind.Utc);
        var localTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, EasternTimeZone);
        var frontendUrl = _configuration["Frontend:Url"]?.TrimEnd('/') ?? "https://witchcityrope.com";
        var acceptUrl = $"{frontendUrl}/dashboard/pending-assignments";

        var variables = new Dictionary<string, string>
        {
            ["recipient_scene_name"] = user.SceneName ?? user.Email ?? "Member",
            ["delegate_scene_name"] = assignedByUser?.SceneName ?? "Someone",
            ["event_title"] = eventEntity.Title ?? string.Empty,
            ["event_date"] = localTime.ToString("dddd, MMMM d, yyyy"),
            ["event_time"] = localTime.ToString("h:mm tt") + " ET",
            ["event_start_time"] = localTime.ToString("h:mm tt") + " ET",
            ["event_venue"] = eventEntity.Venue?.Name ?? string.Empty,
            ["ticket_type_name"] = attendance.TicketPurchase?.TicketType?.Name ?? string.Empty,
            ["accept_url"] = acceptUrl,
            ["accept_button"] = BuildAcceptButton(acceptUrl)
        };

        _logger.LogInformation(
            "Sending reminder for attendance {AttendanceId} to user {UserId} for event {EventTitle}",
            attendance.Id, attendance.UserId, eventEntity.Title);

        var result = await _emailService.SendTemplatedEmailAsync(
            user.Email ?? string.Empty,
            user.SceneName ?? user.Email ?? "Member",
            EmailCategory.Events,
            templateType,
            variables,
            eventEntity.Id,
            ct);

        if (!result.IsSuccess)
        {
            _logger.LogWarning(
                "Failed to send reminder email for attendance {AttendanceId} to {Email}: {Error}",
                attendance.Id, user.Email, result.Error);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Builds an HTML accept button for the reminder email.
    /// Uses the same styling pattern as EmailSchedulerJob.
    /// </summary>
    private static string BuildAcceptButton(string url)
    {
        return $"<a href=\"{url}\" style=\"display: inline-block; padding: 12px 24px; " +
               $"background-color: #880124; color: #ffffff; text-decoration: none; " +
               $"border-radius: 6px; font-weight: 600;\">Accept Now</a>";
    }
}

/// <summary>
/// Template type constants for assignment-related emails.
/// These correspond to GlobalEmailTemplate.TemplateType values
/// that must be seeded in the database for emails to actually send.
///
/// NOTE: The actual email template content (HTML, subject lines, variable
/// substitution) is created separately as seed data. These constants
/// define the TemplateType identifier used to look up the template.
/// </summary>
public static class AssignmentEmailTemplates
{
    /// <summary>
    /// Sent immediately when a ticket is assigned to a user (BR-060)
    /// </summary>
    public const string TicketAssignmentNotification = "TicketAssignmentNotification";

    /// <summary>
    /// Sent immediately when a proxy RSVP is created for a user (BR-060)
    /// </summary>
    public const string RsvpAssignmentNotification = "RsvpAssignmentNotification";

    /// <summary>
    /// Sent 24 hours before event for unaccepted ticket assignments (BR-061)
    /// </summary>
    public const string TicketAcceptanceReminder = "TicketAcceptanceReminder";

    /// <summary>
    /// Sent 24 hours before event for unaccepted RSVP assignments (BR-061)
    /// </summary>
    public const string RsvpAcceptanceReminder = "RsvpAcceptanceReminder";
}

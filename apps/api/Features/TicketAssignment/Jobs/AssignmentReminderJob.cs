using WitchCityRope.Api.Features.TicketAssignment.Services;

namespace WitchCityRope.Api.Features.TicketAssignment.Jobs;

/// <summary>
/// Hangfire recurring job that sends reminder emails for unaccepted ticket/RSVP
/// assignments approaching their event date.
///
/// Runs hourly to check for events starting within 24 hours that have
/// PendingAcceptance attendances without a reminder sent.
///
/// Business rules:
/// - BR-061: Reminder sent 1 day before event's first session
/// - BR-062: One reminder per assignment (tracked via ReminderSentAt)
/// - BR-063: New recipient group for unaccepted assignments
///
/// Registered as: RecurringJob.AddOrUpdate in Program.cs
/// Schedule: Hourly ("0 * * * *") for timely reminder delivery
/// </summary>
public class AssignmentReminderJob
{
    private readonly IAssignmentReminderService _reminderService;
    private readonly ILogger<AssignmentReminderJob> _logger;

    public AssignmentReminderJob(
        IAssignmentReminderService reminderService,
        ILogger<AssignmentReminderJob> logger)
    {
        _reminderService = reminderService;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Assignment reminder job started");

        try
        {
            await _reminderService.SendPendingAssignmentRemindersAsync(cancellationToken);
            _logger.LogInformation("Assignment reminder job completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Assignment reminder job failed");
            throw; // Let Hangfire handle retry
        }
    }
}

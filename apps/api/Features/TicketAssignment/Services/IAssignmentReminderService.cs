namespace WitchCityRope.Api.Features.TicketAssignment.Services;

/// <summary>
/// Service for sending reminder emails for unaccepted ticket/RSVP assignments
/// approaching their event date. Called by the Hangfire recurring job.
///
/// Business rules implemented:
/// - BR-061: Reminder sent 1 day before event's first session
/// - BR-062: One reminder per assignment (tracked via ReminderSentAt)
/// - BR-063: Targets PendingAcceptance status for both Ticket and RSVP types
/// </summary>
public interface IAssignmentReminderService
{
    /// <summary>
    /// Finds all PendingAcceptance assignments for events starting within 24 hours
    /// where ReminderSentAt is null, and sends reminder emails.
    /// Updates ReminderSentAt to prevent duplicate sends (BR-062).
    /// Creates AttendanceHistory "ReminderSent" records for audit trail.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    Task SendPendingAssignmentRemindersAsync(CancellationToken ct);
}

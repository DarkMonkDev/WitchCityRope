namespace WitchCityRope.Api.Features.EmailTemplates.Services;

public interface IEventEmailService
{
    Task SendPostPurchaseEmailsAsync(
        Guid userId, List<Guid> ticketPurchaseIds, CancellationToken ct);

    /// <summary>
    /// Sends a cancellation email listing all cancelled sessions.
    /// Called after ticket purchases are cancelled — one consolidated email
    /// listing every session that was cancelled, grouped by ticket type.
    /// Non-fatal: failures are logged but do not propagate.
    /// </summary>
    Task SendCancellationEmailAsync(
        Guid userId, Guid eventId, List<Guid> ticketPurchaseIds, CancellationToken ct);

    /// <summary>
    /// Sends an RSVP confirmation email after a user manually RSVPs to a social/free event.
    /// NOT called for auto-RSVPs created during ticket purchase (user already gets ticket confirmation).
    /// Non-fatal: failures are logged but do not propagate.
    /// </summary>
    Task SendRsvpConfirmationEmailAsync(
        Guid userId, Guid eventId, CancellationToken ct);

    /// <summary>
    /// Sends an RSVP cancellation confirmation email.
    /// Called after a user cancels their RSVP for a social/free event.
    /// Simpler than ticket cancellation — no sessions or ticket types to list.
    /// Non-fatal: failures are logged but do not propagate.
    /// </summary>
    Task SendRsvpCancellationEmailAsync(
        Guid userId, Guid eventId, CancellationToken ct);

    /// <summary>
    /// Sends a ticket assignment notification email to the assignee.
    /// Called after a ticket is successfully assigned to another user
    /// (AssignTicketAsync, ReassignTicketAsync, AssignUnassignedTicketAsync, AdminAssignTicketAsync).
    /// Non-fatal: failures are logged but do not propagate.
    /// </summary>
    Task SendTicketAssignmentNotificationAsync(
        Guid assigneeUserId, Guid assignerUserId, Guid eventId,
        Guid attendanceId, CancellationToken ct);

    /// <summary>
    /// Sends an RSVP assignment notification email to the principal.
    /// Called after a proxy RSVP is successfully created on someone's behalf.
    /// Non-fatal: failures are logged but do not propagate.
    /// </summary>
    Task SendRsvpAssignmentNotificationAsync(
        Guid principalUserId, Guid delegateUserId, Guid eventId,
        CancellationToken ct);
}

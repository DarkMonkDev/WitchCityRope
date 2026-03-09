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
}

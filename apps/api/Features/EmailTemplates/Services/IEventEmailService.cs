namespace WitchCityRope.Api.Features.EmailTemplates.Services;

public interface IEventEmailService
{
    Task SendPostPurchaseEmailsAsync(
        Guid userId, List<Guid> ticketPurchaseIds, CancellationToken ct);
}

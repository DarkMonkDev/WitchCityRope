using WitchCityRope.Api.Features.EmailTemplates.Entities;

namespace WitchCityRope.Api.Features.EmailTemplates.Services;

public interface IEventRecipientService
{
    Task<List<RecipientInfo>> GetRecipientsAsync(
        EventRecipientGroup group, Guid sessionId, CancellationToken ct);
}

public record RecipientInfo(Guid UserId, string Email, string DisplayName);

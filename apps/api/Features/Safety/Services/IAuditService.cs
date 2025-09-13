using WitchCityRope.Api.Features.Safety.Models;

namespace WitchCityRope.Api.Features.Safety.Services;

/// <summary>
/// Audit logging service for safety incident actions
/// </summary>
public interface IAuditService
{
    Task LogActionAsync(Guid incidentId, Guid? userId, string actionType, 
        string description, object? oldValues = null, object? newValues = null,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<AuditLogDto>> GetAuditTrailAsync(Guid incidentId, 
        CancellationToken cancellationToken = default);
}
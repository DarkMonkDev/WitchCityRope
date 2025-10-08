using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Safety.Entities;
using WitchCityRope.Api.Features.Safety.Models;

namespace WitchCityRope.Api.Features.Safety.Services;

/// <summary>
/// Audit logging service for safety incident actions
/// </summary>
public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuditService> _logger;

    public AuditService(
        ApplicationDbContext context,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuditService> logger)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task LogActionAsync(Guid incidentId, Guid? userId, string actionType,
        string description, object? oldValues = null, object? newValues = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;

            // Get incident to check if anonymous - don't log IP for anonymous reports
            var incident = await _context.SafetyIncidents
                .AsNoTracking()
                .Where(i => i.Id == incidentId)
                .Select(i => new { i.IsAnonymous })
                .FirstOrDefaultAsync(cancellationToken);

            var auditLog = new IncidentAuditLog
            {
                IncidentId = incidentId,
                UserId = userId,
                ActionType = actionType,
                ActionDescription = description,
                OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
                NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
                // CRITICAL: Don't log IP for anonymous reports
                IpAddress = incident?.IsAnonymous == true ? null : httpContext?.Connection?.RemoteIpAddress?.ToString(),
                UserAgent = httpContext?.Request?.Headers["User-Agent"].ToString()
            };

            _context.IncidentAuditLogs.Add(auditLog);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Audit log created: {ActionType} for incident {IncidentId} by user {UserId}",
                actionType, incidentId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create audit log for incident {IncidentId}", incidentId);
            // Don't throw - audit logging failure shouldn't break main operations
        }
    }

    public async Task<IEnumerable<AuditLogDto>> GetAuditTrailAsync(Guid incidentId,
        CancellationToken cancellationToken = default)
    {
        return await _context.IncidentAuditLogs
            .AsNoTracking()
            .Where(a => a.IncidentId == incidentId)
            .Include(a => a.User)
            .Select(a => new AuditLogDto
            {
                Id = a.Id,
                ActionType = a.ActionType,
                ActionDescription = a.ActionDescription,
                UserId = a.UserId,
                UserName = a.User != null ? a.User.SceneName : "System",
                CreatedAt = a.CreatedAt
            })
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
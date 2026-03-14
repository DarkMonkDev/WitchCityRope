using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.EmailTemplates.Entities;
using WitchCityRope.Api.Features.Safety.Entities;
using WitchCityRope.Api.Features.Shared.Services;

namespace WitchCityRope.Api.Features.Safety.Services;

/// <summary>
/// Incident email service using GlobalEmailTemplates system and SendGrid integration.
/// Sends notifications at key incident lifecycle events: submission, assignment, status change, resolution.
/// All sends are fire-and-forget: exceptions are caught and logged, never thrown.
/// Respects anonymity: anonymous reporters only receive emails if they requested follow-up
/// and provided a contact email.
/// </summary>
public class IncidentEmailService : IIncidentEmailService
{
    private readonly IEmailService _emailService;
    private readonly ApplicationDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<IncidentEmailService> _logger;

    public IncidentEmailService(
        IEmailService emailService,
        ApplicationDbContext context,
        IEncryptionService encryptionService,
        ILogger<IncidentEmailService> logger)
    {
        _emailService = emailService;
        _context = context;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    /// <summary>
    /// Send confirmation to reporter that their incident report was received.
    /// Skips if reporter is anonymous without follow-up contact info.
    /// </summary>
    public async Task SendReportReceivedAsync(SafetyIncident incident, CancellationToken ct = default)
    {
        try
        {
            var (email, name) = await ResolveReporterContactAsync(incident, ct);
            if (email == null)
            {
                _logger.LogInformation(
                    "Skipping ReportReceived email for incident {ReferenceNumber}: no reporter email available (anonymous without follow-up)",
                    incident.ReferenceNumber);
                return;
            }

            // Resolve coordinator name if one is already assigned
            var coordinatorName = await ResolveCoordinatorNameAsync(incident.CoordinatorId, ct);

            var variables = new Dictionary<string, string>
            {
                { "reporter_name", name },
                { "incident_number", incident.ReferenceNumber },
                { "incident_date", incident.IncidentDate.ToString("MMMM dd, yyyy") },
                { "coordinator_name", coordinatorName },
                { "next_steps", string.Empty }
            };

            var result = await _emailService.SendTemplatedEmailAsync(
                email, name, EmailCategory.Incident, "ReportReceived", variables, ct);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "ReportReceived email sent for incident {ReferenceNumber} to {Email}",
                    incident.ReferenceNumber, email);
            }
            else
            {
                _logger.LogError(
                    "Failed to send ReportReceived email for incident {ReferenceNumber}: {Error}",
                    incident.ReferenceNumber, result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error sending ReportReceived email for incident {ReferenceNumber}",
                incident.ReferenceNumber);
        }
    }

    /// <summary>
    /// Notify coordinator they've been assigned to an incident.
    /// </summary>
    public async Task SendAssignmentNotificationAsync(SafetyIncident incident, Guid coordinatorId, CancellationToken ct = default)
    {
        try
        {
            var coordinator = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == coordinatorId, ct);

            if (coordinator?.Email == null)
            {
                _logger.LogWarning(
                    "Cannot send AssignmentNotification for incident {ReferenceNumber}: coordinator {CoordinatorId} not found or has no email",
                    incident.ReferenceNumber, coordinatorId);
                return;
            }

            var variables = new Dictionary<string, string>
            {
                { "coordinator_name", coordinator.SceneName },
                { "incident_number", incident.ReferenceNumber },
                { "incident_date", incident.IncidentDate.ToString("MMMM dd, yyyy") },
                { "next_steps", string.Empty }
            };

            var result = await _emailService.SendTemplatedEmailAsync(
                coordinator.Email, coordinator.SceneName, EmailCategory.Incident, "AssignmentNotification", variables, ct);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "AssignmentNotification email sent for incident {ReferenceNumber} to coordinator {CoordinatorName}",
                    incident.ReferenceNumber, coordinator.SceneName);
            }
            else
            {
                _logger.LogError(
                    "Failed to send AssignmentNotification email for incident {ReferenceNumber}: {Error}",
                    incident.ReferenceNumber, result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error sending AssignmentNotification email for incident {ReferenceNumber}",
                incident.ReferenceNumber);
        }
    }

    /// <summary>
    /// Notify reporter of a status change on their incident.
    /// Skips if reporter is anonymous without follow-up contact info.
    /// </summary>
    public async Task SendStatusUpdateAsync(SafetyIncident incident, string newStatus, CancellationToken ct = default)
    {
        try
        {
            var (email, name) = await ResolveReporterContactAsync(incident, ct);
            if (email == null)
            {
                _logger.LogInformation(
                    "Skipping StatusUpdate email for incident {ReferenceNumber}: no reporter email available",
                    incident.ReferenceNumber);
                return;
            }

            var variables = new Dictionary<string, string>
            {
                { "reporter_name", name },
                { "incident_number", incident.ReferenceNumber },
                { "status", newStatus },
                { "next_steps", string.Empty }
            };

            var result = await _emailService.SendTemplatedEmailAsync(
                email, name, EmailCategory.Incident, "StatusUpdate", variables, ct);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "StatusUpdate email sent for incident {ReferenceNumber} with status {Status}",
                    incident.ReferenceNumber, newStatus);
            }
            else
            {
                _logger.LogError(
                    "Failed to send StatusUpdate email for incident {ReferenceNumber}: {Error}",
                    incident.ReferenceNumber, result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error sending StatusUpdate email for incident {ReferenceNumber}",
                incident.ReferenceNumber);
        }
    }

    /// <summary>
    /// Notify reporter that their incident has been resolved/closed.
    /// Skips if reporter is anonymous without follow-up contact info.
    /// </summary>
    public async Task SendResolvedAsync(SafetyIncident incident, CancellationToken ct = default)
    {
        try
        {
            var (email, name) = await ResolveReporterContactAsync(incident, ct);
            if (email == null)
            {
                _logger.LogInformation(
                    "Skipping Resolved email for incident {ReferenceNumber}: no reporter email available",
                    incident.ReferenceNumber);
                return;
            }

            var variables = new Dictionary<string, string>
            {
                { "reporter_name", name },
                { "incident_number", incident.ReferenceNumber },
                { "incident_date", incident.IncidentDate.ToString("MMMM dd, yyyy") },
                { "next_steps", string.Empty }
            };

            var result = await _emailService.SendTemplatedEmailAsync(
                email, name, EmailCategory.Incident, "Resolved", variables, ct);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Resolved email sent for incident {ReferenceNumber}",
                    incident.ReferenceNumber);
            }
            else
            {
                _logger.LogError(
                    "Failed to send Resolved email for incident {ReferenceNumber}: {Error}",
                    incident.ReferenceNumber, result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error sending Resolved email for incident {ReferenceNumber}",
                incident.ReferenceNumber);
        }
    }

    #region Private Helpers

    /// <summary>
    /// Resolve reporter contact information for email sending.
    /// For non-anonymous reporters: loads user from database by ReporterId.
    /// For anonymous reporters: checks RequestFollowUp flag and decrypts contact email/name.
    /// Returns (null, _) if no email is available (anonymous without follow-up contact).
    /// </summary>
    private async Task<(string? email, string name)> ResolveReporterContactAsync(
        SafetyIncident incident, CancellationToken ct)
    {
        if (!incident.IsAnonymous && incident.ReporterId.HasValue)
        {
            // Non-anonymous reporter: load user from database
            var reporter = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == incident.ReporterId.Value, ct);

            if (reporter?.Email != null)
            {
                return (reporter.Email, reporter.SceneName);
            }

            _logger.LogWarning(
                "Reporter {ReporterId} for incident {ReferenceNumber} not found or has no email",
                incident.ReporterId, incident.ReferenceNumber);
            return (null, "Reporter");
        }

        // Anonymous reporter: only send if they requested follow-up and provided contact email
        if (incident.IsAnonymous && incident.RequestFollowUp && !string.IsNullOrEmpty(incident.EncryptedContactEmail))
        {
            var decryptedEmail = await _encryptionService.DecryptAsync(incident.EncryptedContactEmail);
            var decryptedName = !string.IsNullOrEmpty(incident.EncryptedContactName)
                ? await _encryptionService.DecryptAsync(incident.EncryptedContactName)
                : "Anonymous Reporter";

            return (decryptedEmail, decryptedName);
        }

        // Anonymous without follow-up or without contact email
        return (null, "Anonymous Reporter");
    }

    /// <summary>
    /// Resolve coordinator display name from user ID.
    /// Returns empty string if coordinator is not assigned or not found.
    /// </summary>
    private async Task<string> ResolveCoordinatorNameAsync(Guid? coordinatorId, CancellationToken ct)
    {
        if (!coordinatorId.HasValue)
            return string.Empty;

        var coordinator = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == coordinatorId.Value)
            .Select(u => u.SceneName)
            .FirstOrDefaultAsync(ct);

        return coordinator ?? string.Empty;
    }

    #endregion
}

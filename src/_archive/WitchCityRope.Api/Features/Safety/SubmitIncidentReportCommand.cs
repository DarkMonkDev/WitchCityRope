using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WitchCityRope.Api.Features.Safety;

/// <summary>
/// Command to submit a safety incident report
/// Allows members to report safety concerns, consent violations, or other incidents
/// </summary>
public record SubmitIncidentReportCommand(
    Guid? ReporterId, // Optional for anonymous reports
    IncidentType Type,
    IncidentSeverity Severity,
    DateTime IncidentDate,
    string Location,
    string Description,
    List<string> InvolvedParties,
    List<string> Witnesses,
    bool RequestFollowUp,
    string? PreferredContactMethod,
    bool IsAnonymous,
    List<string>? SupportingDocuments // File paths/URLs
) : IRequest<SubmitIncidentReportResult>;

public enum IncidentType
{
    SafetyViolation,
    ConsentViolation,
    EquipmentFailure,
    Injury,
    HarassmentOrBullying,
    PolicyViolation,
    Other
}

public enum IncidentSeverity
{
    Low,      // Minor issue, no harm
    Medium,   // Moderate issue, potential for harm
    High,     // Serious issue, harm occurred
    Critical  // Emergency, immediate action required
}

public record SubmitIncidentReportResult(
    Guid ReportId,
    string ReferenceNumber,
    ReportStatus Status,
    DateTime SubmittedAt,
    bool NotificationsSent
);

public enum ReportStatus
{
    Submitted,
    UnderReview,
    InvestigationInProgress,
    AwaitingMoreInfo,
    Resolved,
    Closed
}

/// <summary>
/// Handles incident report submission
/// Stores report securely and notifies appropriate safety team members
/// </summary>
public class SubmitIncidentReportCommandHandler
{
    private readonly IIncidentRepository _incidentRepository;
    private readonly IUserRepository _userRepository;
    private readonly ISafetyTeamService _safetyTeamService;
    private readonly INotificationService _notificationService;
    private readonly IEncryptionService _encryptionService;

    public SubmitIncidentReportCommandHandler(
        IIncidentRepository incidentRepository,
        IUserRepository userRepository,
        ISafetyTeamService safetyTeamService,
        INotificationService notificationService,
        IEncryptionService encryptionService)
    {
        _incidentRepository = incidentRepository;
        _userRepository = userRepository;
        _safetyTeamService = safetyTeamService;
        _notificationService = notificationService;
        _encryptionService = encryptionService;
    }

    public async Task<SubmitIncidentReportResult> Execute(SubmitIncidentReportCommand command)
    {
        // Validate incident date
        if (command.IncidentDate > DateTime.UtcNow)
        {
            throw new ValidationException("Incident date cannot be in the future");
        }

        // Validate reporter if not anonymous
        User? reporter = null;
        if (!command.IsAnonymous)
        {
            if (!command.ReporterId.HasValue)
            {
                throw new ValidationException("Reporter ID is required for non-anonymous reports");
            }

            reporter = await _userRepository.GetByIdAsync(command.ReporterId.Value);
            if (reporter == null)
            {
                throw new NotFoundException("Reporter not found");
            }
        }

        // Generate reference number
        var referenceNumber = GenerateReferenceNumber(command.Type);

        // Encrypt sensitive information
        var encryptedDescription = await _encryptionService.EncryptAsync(command.Description);
        var encryptedParties = await EncryptListAsync(command.InvolvedParties);
        var encryptedWitnesses = await EncryptListAsync(command.Witnesses);

        // Create incident report
        var reportId = Guid.NewGuid();
        var report = new IncidentReport
        {
            Id = reportId,
            ReferenceNumber = referenceNumber,
            ReporterId = command.IsAnonymous ? null : command.ReporterId,
            IsAnonymous = command.IsAnonymous,
            Type = command.Type,
            Severity = command.Severity,
            IncidentDate = command.IncidentDate,
            Location = command.Location,
            EncryptedDescription = encryptedDescription,
            EncryptedInvolvedParties = encryptedParties,
            EncryptedWitnesses = encryptedWitnesses,
            RequestFollowUp = command.RequestFollowUp,
            PreferredContactMethod = command.PreferredContactMethod,
            Status = ReportStatus.Submitted,
            SubmittedAt = DateTime.UtcNow,
            SupportingDocuments = command.SupportingDocuments ?? new List<string>()
        };

        await _incidentRepository.CreateAsync(report);

        // Determine urgency and notify appropriate team members
        var urgency = DetermineUrgency(command.Severity, command.Type);
        var notificationsSent = false;

        try
        {
            if (urgency == NotificationUrgency.Immediate)
            {
                // Notify all on-call safety team members immediately
                var onCallMembers = await _safetyTeamService.GetOnCallMembersAsync();
                foreach (var member in onCallMembers)
                {
                    await _notificationService.SendUrgentIncidentAlertAsync(
                        member.Email,
                        member.Phone,
                        referenceNumber,
                        command.Type.ToString(),
                        command.Severity.ToString()
                    );
                }
                notificationsSent = true;
            }
            else
            {
                // Send standard notification to safety team
                await _notificationService.NotifySafetyTeamOfNewReportAsync(
                    referenceNumber,
                    command.Type,
                    command.Severity,
                    urgency
                );
                notificationsSent = true;
            }

            // Send confirmation to reporter if not anonymous and requested
            if (!command.IsAnonymous && reporter != null && command.RequestFollowUp)
            {
                await _notificationService.SendIncidentReportConfirmationAsync(
                    reporter.Email,
                    reporter.DisplayName,
                    referenceNumber
                );
            }
        }
        catch (Exception ex)
        {
            // Log notification failure but don't fail the report submission
            // The report is already saved and can be followed up on
            Console.WriteLine($"Failed to send notifications for report {referenceNumber}: {ex.Message}");
        }

        // Create audit log entry
        await _incidentRepository.CreateAuditLogAsync(new IncidentAuditLog
        {
            Id = Guid.NewGuid(),
            IncidentId = reportId,
            Action = "Report Submitted",
            PerformedBy = command.ReporterId,
            PerformedAt = DateTime.UtcNow,
            Details = $"Incident report {referenceNumber} submitted"
        });

        return new SubmitIncidentReportResult(
            ReportId: reportId,
            ReferenceNumber: referenceNumber,
            Status: report.Status,
            SubmittedAt: report.SubmittedAt,
            NotificationsSent: notificationsSent
        );
    }

    private string GenerateReferenceNumber(IncidentType type)
    {
        var typePrefix = type switch
        {
            IncidentType.SafetyViolation => "SAF",
            IncidentType.ConsentViolation => "CON",
            IncidentType.EquipmentFailure => "EQP",
            IncidentType.Injury => "INJ",
            IncidentType.HarassmentOrBullying => "HAR",
            IncidentType.PolicyViolation => "POL",
            _ => "OTH"
        };

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd");
        var random = new Random().Next(1000, 9999);
        return $"{typePrefix}-{timestamp}-{random}";
    }

    private NotificationUrgency DetermineUrgency(IncidentSeverity severity, IncidentType type)
    {
        // Critical severity always immediate
        if (severity == IncidentSeverity.Critical)
            return NotificationUrgency.Immediate;

        // High severity consent violations are immediate
        if (severity == IncidentSeverity.High && type == IncidentType.ConsentViolation)
            return NotificationUrgency.Immediate;

        // High severity injuries are immediate
        if (severity == IncidentSeverity.High && type == IncidentType.Injury)
            return NotificationUrgency.Immediate;

        // High severity is urgent
        if (severity == IncidentSeverity.High)
            return NotificationUrgency.Urgent;

        // Everything else is standard
        return NotificationUrgency.Standard;
    }

    private async Task<List<string>> EncryptListAsync(List<string> items)
    {
        var encrypted = new List<string>();
        foreach (var item in items)
        {
            encrypted.Add(await _encryptionService.EncryptAsync(item));
        }
        return encrypted;
    }
}

// Marker interface for future MediatR implementation
public interface IRequest<TResponse> { }

// Custom exceptions
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

// Domain models
public class IncidentReport
{
    public Guid Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public Guid? ReporterId { get; set; }
    public bool IsAnonymous { get; set; }
    public IncidentType Type { get; set; }
    public IncidentSeverity Severity { get; set; }
    public DateTime IncidentDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string EncryptedDescription { get; set; } = string.Empty;
    public List<string> EncryptedInvolvedParties { get; set; } = new();
    public List<string> EncryptedWitnesses { get; set; } = new();
    public bool RequestFollowUp { get; set; }
    public string? PreferredContactMethod { get; set; }
    public ReportStatus Status { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public Guid? AssignedTo { get; set; }
    public List<string> SupportingDocuments { get; set; } = new();
}

public class IncidentAuditLog
{
    public Guid Id { get; set; }
    public Guid IncidentId { get; set; }
    public string Action { get; set; } = string.Empty;
    public Guid? PerformedBy { get; set; }
    public DateTime PerformedAt { get; set; }
    public string Details { get; set; } = string.Empty;
}

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}

public class SafetyTeamMember
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public bool IsOnCall { get; set; }
}

public enum NotificationUrgency
{
    Standard,
    Urgent,
    Immediate
}

// Repository and service interfaces
public interface IIncidentRepository
{
    Task CreateAsync(IncidentReport report);
    Task CreateAuditLogAsync(IncidentAuditLog log);
}

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid userId);
}

public interface ISafetyTeamService
{
    Task<List<SafetyTeamMember>> GetOnCallMembersAsync();
}

public interface INotificationService
{
    Task SendUrgentIncidentAlertAsync(
        string email, 
        string phone, 
        string referenceNumber, 
        string incidentType, 
        string severity);
    
    Task NotifySafetyTeamOfNewReportAsync(
        string referenceNumber, 
        IncidentType type, 
        IncidentSeverity severity, 
        NotificationUrgency urgency);
    
    Task SendIncidentReportConfirmationAsync(
        string email, 
        string name, 
        string referenceNumber);
}

public interface IEncryptionService
{
    Task<string> EncryptAsync(string plainText);
    Task<string> DecryptAsync(string encryptedText);
}
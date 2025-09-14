using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WitchCityRope.Api.Features.Vetting.SubmitApplication;

/// <summary>
/// Command to submit a vetting application
/// Collects information about experience, references, and safety knowledge
/// </summary>
public record SubmitApplicationCommand(
    Guid UserId,
    string ExperienceLevel,
    string ExperienceDescription,
    List<string> SkillsAndInterests,
    string SafetyKnowledge,
    string ConsentUnderstanding,
    List<Reference> References,
    string WhyJoin,
    bool AgreesToCodeOfConduct,
    bool AgreesToSafetyGuidelines,
    bool UnderstandsVettingProcess
) : IRequest<SubmitApplicationResult>;

public record Reference(
    string Name,
    string Email,
    string Phone,
    string Relationship,
    int YearsKnown
);

public record SubmitApplicationResult(
    Guid ApplicationId,
    ApplicationStatus Status,
    DateTime SubmittedAt,
    string ReferenceNumber
);

public enum ApplicationStatus
{
    Submitted,
    UnderReview,
    PendingReferences,
    PendingInterview,
    Approved,
    Rejected,
    Withdrawn
}

/// <summary>
/// Handles vetting application submission
/// Validates application, stores it, and initiates the review process
/// </summary>
public class SubmitApplicationCommandHandler
{
    private readonly IApplicationRepository _applicationRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationService _notificationService;
    private readonly IValidator<SubmitApplicationCommand> _validator;

    public SubmitApplicationCommandHandler(
        IApplicationRepository applicationRepository,
        IUserRepository userRepository,
        INotificationService notificationService,
        IValidator<SubmitApplicationCommand> validator)
    {
        _applicationRepository = applicationRepository;
        _userRepository = userRepository;
        _notificationService = notificationService;
        _validator = validator;
    }

    public async Task<SubmitApplicationResult> Execute(SubmitApplicationCommand command)
    {
        // Validate command
        var validationResult = await _validator.ValidateAsync(command);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // Verify user exists
        var user = await _userRepository.GetByIdAsync(command.UserId);
        if (user == null)
        {
            throw new NotFoundException("User not found");
        }

        // Check if user is already vetted
        if (user.IsVetted)
        {
            throw new ConflictException("User is already vetted");
        }

        // Check for existing pending application
        var existingApplication = await _applicationRepository.GetPendingByUserIdAsync(command.UserId);
        if (existingApplication != null)
        {
            throw new ConflictException("User already has a pending vetting application");
        }

        // Validate all agreements are accepted
        if (!command.AgreesToCodeOfConduct || !command.AgreesToSafetyGuidelines || !command.UnderstandsVettingProcess)
        {
            throw new ValidationException("All agreements must be accepted");
        }

        // Validate references (minimum 2 required)
        if (command.References.Count < 2)
        {
            throw new ValidationException("At least 2 references are required");
        }

        // Create application
        var applicationId = Guid.NewGuid();
        var referenceNumber = GenerateReferenceNumber();

        var application = new VettingApplication
        {
            Id = applicationId,
            UserId = command.UserId,
            ReferenceNumber = referenceNumber,
            ExperienceLevel = command.ExperienceLevel,
            ExperienceDescription = command.ExperienceDescription,
            SkillsAndInterests = command.SkillsAndInterests,
            SafetyKnowledge = command.SafetyKnowledge,
            ConsentUnderstanding = command.ConsentUnderstanding,
            WhyJoin = command.WhyJoin,
            Status = ApplicationStatus.Submitted,
            SubmittedAt = DateTime.UtcNow,
            References = command.References.Select(r => new ApplicationReference
            {
                Id = Guid.NewGuid(),
                ApplicationId = applicationId,
                Name = r.Name,
                Email = r.Email,
                Phone = r.Phone,
                Relationship = r.Relationship,
                YearsKnown = r.YearsKnown,
                Status = ReferenceStatus.Pending,
                TokenSentAt = null
            }).ToList()
        };

        await _applicationRepository.CreateAsync(application);

        // Send confirmation email to applicant
        await _notificationService.SendApplicationReceivedEmailAsync(
            user.Email,
            user.DisplayName,
            referenceNumber
        );

        // Send reference request emails
        foreach (var reference in application.References)
        {
            var token = GenerateReferenceToken();
            reference.VerificationToken = token;
            reference.TokenSentAt = DateTime.UtcNow;

            await _notificationService.SendReferenceRequestEmailAsync(
                reference.Email,
                reference.Name,
                user.DisplayName,
                token
            );
        }

        // Update reference tokens in database
        await _applicationRepository.UpdateReferencesAsync(application.References);

        // Notify vetting team
        await _notificationService.NotifyVettingTeamOfNewApplicationAsync(
            applicationId,
            user.DisplayName,
            referenceNumber
        );

        return new SubmitApplicationResult(
            ApplicationId: applicationId,
            Status: ApplicationStatus.Submitted,
            SubmittedAt: application.SubmittedAt,
            ReferenceNumber: referenceNumber
        );
    }

    private string GenerateReferenceNumber()
    {
        // Generate a human-readable reference number
        var prefix = "VET";
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd");
        var random = new Random().Next(1000, 9999);
        return $"{prefix}-{timestamp}-{random}";
    }

    private string GenerateReferenceToken()
    {
        return Guid.NewGuid().ToString("N");
    }
}

// Marker interface for future MediatR implementation
public interface IRequest<TResponse> { }

// Custom exceptions
public class ValidationException : Exception
{
    public List<string> Errors { get; }
    
    public ValidationException(string message) : base(message) 
    {
        Errors = new List<string> { message };
    }
    
    public ValidationException(List<ValidationError> errors) : base("Validation failed")
    {
        Errors = errors.Select(e => e.Message).ToList();
    }
}

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}

// Domain models
public class VettingApplication
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public string ExperienceLevel { get; set; } = string.Empty;
    public string ExperienceDescription { get; set; } = string.Empty;
    public List<string> SkillsAndInterests { get; set; } = new();
    public string SafetyKnowledge { get; set; } = string.Empty;
    public string ConsentUnderstanding { get; set; } = string.Empty;
    public string WhyJoin { get; set; } = string.Empty;
    public ApplicationStatus Status { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedBy { get; set; }
    public string? ReviewNotes { get; set; }
    public string? RejectionReason { get; set; }
    public List<ApplicationReference> References { get; set; } = new();
}

public class ApplicationReference
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public int YearsKnown { get; set; }
    public ReferenceStatus Status { get; set; }
    public string? VerificationToken { get; set; }
    public DateTime? TokenSentAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Comments { get; set; }
    public int? SafetyRating { get; set; } // 1-5
    public int? CommunityRating { get; set; } // 1-5
    public bool? WouldRecommend { get; set; }
}

public enum ReferenceStatus
{
    Pending,
    Completed,
    Declined,
    Invalid
}

// Repository and service interfaces
public interface IApplicationRepository
{
    Task<VettingApplication?> GetPendingByUserIdAsync(Guid userId);
    Task CreateAsync(VettingApplication application);
    Task UpdateReferencesAsync(List<ApplicationReference> references);
}

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid userId);
}

public interface INotificationService
{
    Task SendApplicationReceivedEmailAsync(string email, string name, string referenceNumber);
    Task SendReferenceRequestEmailAsync(string email, string referenceName, string applicantName, string token);
    Task NotifyVettingTeamOfNewApplicationAsync(Guid applicationId, string applicantName, string referenceNumber);
}

public interface IValidator<T>
{
    Task<ValidationResult> ValidateAsync(T instance);
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<ValidationError> Errors { get; set; } = new();
}

public class ValidationError
{
    public string PropertyName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsVetted { get; set; }
}
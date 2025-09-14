using System;
using System.Threading.Tasks;

namespace WitchCityRope.Api.Features.Vetting.ReviewApplication;

/// <summary>
/// Command to review a vetting application
/// Allows vetting team members to approve, reject, or request more information
/// </summary>
public record ReviewApplicationCommand(
    Guid ApplicationId,
    Guid ReviewerId,
    ReviewDecision Decision,
    string ReviewNotes,
    string? RejectionReason,
    int? SafetyScore, // 1-10
    int? CommunityFitScore, // 1-10
    bool? RequiresInterview,
    string[]? Concerns
) : IRequest<ReviewApplicationResult>;

public enum ReviewDecision
{
    Approve,
    Reject,
    RequestMoreInfo,
    ScheduleInterview
}

public record ReviewApplicationResult(
    Guid ApplicationId,
    ApplicationStatus NewStatus,
    DateTime ReviewedAt,
    bool NotificationSent
);

/// <summary>
/// Handles vetting application review
/// Updates application status and notifies the applicant
/// </summary>
public class ReviewApplicationCommandHandler
{
    private readonly IApplicationRepository _applicationRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationService _notificationService;
    private readonly IVettingService _vettingService;

    public ReviewApplicationCommandHandler(
        IApplicationRepository applicationRepository,
        IUserRepository userRepository,
        INotificationService notificationService,
        IVettingService vettingService)
    {
        _applicationRepository = applicationRepository;
        _userRepository = userRepository;
        _notificationService = notificationService;
        _vettingService = vettingService;
    }

    public async Task<ReviewApplicationResult> Execute(ReviewApplicationCommand command)
    {
        // Get application
        var application = await _applicationRepository.GetByIdAsync(command.ApplicationId);
        if (application == null)
        {
            throw new NotFoundException("Application not found");
        }

        // Verify reviewer has permission
        var reviewer = await _userRepository.GetByIdAsync(command.ReviewerId);
        if (reviewer == null)
        {
            throw new NotFoundException("Reviewer not found");
        }

        if (!reviewer.Roles.Contains("VettingTeam") && !reviewer.Roles.Contains("Admin"))
        {
            throw new ForbiddenException("User does not have permission to review applications");
        }

        // Validate application is in reviewable state
        if (application.Status != ApplicationStatus.Submitted && 
            application.Status != ApplicationStatus.UnderReview &&
            application.Status != ApplicationStatus.PendingInterview)
        {
            throw new ConflictException($"Application cannot be reviewed in status: {application.Status}");
        }

        // Check if all references are complete (for approval)
        if (command.Decision == ReviewDecision.Approve)
        {
            var incompleteReferences = application.References
                .Count(r => r.Status != ReferenceStatus.Completed);
            
            if (incompleteReferences > 0)
            {
                throw new ValidationException($"Cannot approve application with {incompleteReferences} incomplete references");
            }

            // Validate safety and community scores are provided
            if (!command.SafetyScore.HasValue || !command.CommunityFitScore.HasValue)
            {
                throw new ValidationException("Safety and community fit scores are required for approval");
            }
        }

        // Update application based on decision
        var newStatus = command.Decision switch
        {
            ReviewDecision.Approve => ApplicationStatus.Approved,
            ReviewDecision.Reject => ApplicationStatus.Rejected,
            ReviewDecision.RequestMoreInfo => ApplicationStatus.UnderReview,
            ReviewDecision.ScheduleInterview => ApplicationStatus.PendingInterview,
            _ => throw new InvalidOperationException("Invalid review decision")
        };

        application.Status = newStatus;
        application.ReviewedAt = DateTime.UtcNow;
        application.ReviewedBy = command.ReviewerId;
        application.ReviewNotes = command.ReviewNotes;

        if (command.Decision == ReviewDecision.Reject)
        {
            application.RejectionReason = command.RejectionReason 
                ?? "Application did not meet community standards";
        }

        // Create review record
        var review = new ApplicationReview
        {
            Id = Guid.NewGuid(),
            ApplicationId = application.Id,
            ReviewerId = command.ReviewerId,
            Decision = command.Decision,
            ReviewNotes = command.ReviewNotes,
            SafetyScore = command.SafetyScore,
            CommunityFitScore = command.CommunityFitScore,
            RequiresInterview = command.RequiresInterview,
            Concerns = command.Concerns?.ToList() ?? new List<string>(),
            CreatedAt = DateTime.UtcNow
        };

        await _applicationRepository.AddReviewAsync(review);
        await _applicationRepository.UpdateAsync(application);

        // Get applicant info
        var applicant = await _userRepository.GetByIdAsync(application.UserId);
        if (applicant == null)
        {
            throw new NotFoundException("Applicant not found");
        }

        // Handle post-decision actions
        bool notificationSent = false;

        switch (command.Decision)
        {
            case ReviewDecision.Approve:
                // Update user vetting status
                await _vettingService.MarkUserAsVettedAsync(application.UserId);
                
                // Send approval email
                notificationSent = await _notificationService.SendApplicationApprovedEmailAsync(
                    applicant.Email,
                    applicant.DisplayName
                );

                // Add to vetted members group
                await _vettingService.AddToVettedMembersGroupAsync(application.UserId);
                break;

            case ReviewDecision.Reject:
                // Send rejection email
                notificationSent = await _notificationService.SendApplicationRejectedEmailAsync(
                    applicant.Email,
                    applicant.DisplayName,
                    application.RejectionReason!
                );
                break;

            case ReviewDecision.RequestMoreInfo:
                // Send request for more information
                notificationSent = await _notificationService.SendMoreInfoRequestedEmailAsync(
                    applicant.Email,
                    applicant.DisplayName,
                    command.ReviewNotes
                );
                break;

            case ReviewDecision.ScheduleInterview:
                // Send interview scheduling email
                notificationSent = await _notificationService.SendInterviewRequestEmailAsync(
                    applicant.Email,
                    applicant.DisplayName
                );
                break;
        }

        return new ReviewApplicationResult(
            ApplicationId: application.Id,
            NewStatus: newStatus,
            ReviewedAt: application.ReviewedAt.Value,
            NotificationSent: notificationSent
        );
    }
}

// Marker interface for future MediatR implementation
public interface IRequest<TResponse> { }

// Custom exceptions
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }
}

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}

public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}

// Domain models
public class ApplicationReview
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public Guid ReviewerId { get; set; }
    public ReviewDecision Decision { get; set; }
    public string ReviewNotes { get; set; } = string.Empty;
    public int? SafetyScore { get; set; }
    public int? CommunityFitScore { get; set; }
    public bool? RequiresInterview { get; set; }
    public List<string> Concerns { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

// Extended domain models
public partial class VettingApplication
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public ApplicationStatus Status { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedBy { get; set; }
    public string? ReviewNotes { get; set; }
    public string? RejectionReason { get; set; }
    public List<ApplicationReference> References { get; set; } = new();
}

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

public class ApplicationReference
{
    public ReferenceStatus Status { get; set; }
}

public enum ReferenceStatus
{
    Pending,
    Completed,
    Declined,
    Invalid
}

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}

// Repository and service interfaces
public interface IApplicationRepository
{
    Task<VettingApplication?> GetByIdAsync(Guid applicationId);
    Task UpdateAsync(VettingApplication application);
    Task AddReviewAsync(ApplicationReview review);
}

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid userId);
}

public interface INotificationService
{
    Task<bool> SendApplicationApprovedEmailAsync(string email, string name);
    Task<bool> SendApplicationRejectedEmailAsync(string email, string name, string reason);
    Task<bool> SendMoreInfoRequestedEmailAsync(string email, string name, string details);
    Task<bool> SendInterviewRequestEmailAsync(string email, string name);
}

public interface IVettingService
{
    Task MarkUserAsVettedAsync(Guid userId);
    Task AddToVettedMembersGroupAsync(Guid userId);
}
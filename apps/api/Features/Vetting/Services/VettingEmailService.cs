using WitchCityRope.Api.Features.EmailTemplates.Entities;
using WitchCityRope.Api.Features.Shared.Models;
using WitchCityRope.Api.Features.Shared.Services;
using WitchCityRope.Api.Features.Vetting.Entities;

namespace WitchCityRope.Api.Features.Vetting.Services;

/// <summary>
/// Vetting email service using GlobalEmailTemplates system and SendGrid integration
/// Provides vetting-specific email sending with proper template variable mapping
/// </summary>
public class VettingEmailService : IVettingEmailService
{
    private readonly IEmailService _emailService;
    private readonly ILogger<VettingEmailService> _logger;
    private readonly IConfiguration _configuration;

    public VettingEmailService(
        IEmailService emailService,
        ILogger<VettingEmailService> logger,
        IConfiguration configuration)
    {
        _emailService = emailService;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Send application confirmation email after successful submission
    /// Uses ApplicationReceived email template
    /// </summary>
    public async Task<Result<bool>> SendApplicationConfirmationAsync(
        VettingApplication application,
        string applicantEmail,
        string applicantName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var variables = new Dictionary<string, string>
            {
                { "scene_name", applicantName },
                { "application_number", application.ApplicationNumber },
                { "submission_date", application.SubmittedAt.ToString("MMMM dd, yyyy") },
                { "application_date", application.CreatedAt.ToString("MMMM dd, yyyy") },
                { "status_change_date", DateTime.UtcNow.ToString("MMMM dd, yyyy") },
                { "current_status", application.WorkflowStatus.ToString() }
            };

            var result = await _emailService.SendTemplatedEmailAsync(
                applicantEmail,
                applicantName,
                EmailCategory.Vetting,
                "ApplicationReceived",
                variables,
                cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Application confirmation email sent: ApplicationNumber={ApplicationNumber}, Email={Email}",
                    application.ApplicationNumber, applicantEmail);
                return Result<bool>.Success(true);
            }
            else
            {
                _logger.LogError(
                    "Failed to send application confirmation email: ApplicationNumber={ApplicationNumber}, Error={Error}",
                    application.ApplicationNumber, result.Error);
                return Result<bool>.Failure(result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error sending application confirmation email: ApplicationNumber={ApplicationNumber}",
                application.ApplicationNumber);
            return Result<bool>.Failure($"Error sending email: {ex.Message}");
        }
    }

    /// <summary>
    /// Send status update notification email when application status changes
    /// Maps status to appropriate template: InterviewApproved, VettingApproved, ApplicationOnHold, ApplicationStatusUpdate
    /// </summary>
    public async Task<Result<bool>> SendStatusUpdateAsync(
        VettingApplication application,
        string applicantEmail,
        string applicantName,
        VettingStatus newStatus,
        string? customMessage = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Map status to template type
            var templateType = newStatus switch
            {
                VettingStatus.InterviewApproved => "InterviewApproved",
                VettingStatus.Approved => "VettingApproved",
                VettingStatus.OnHold => "ApplicationOnHold",
                VettingStatus.Denied => "ApplicationStatusUpdate",
                _ => null
            };

            if (templateType == null)
            {
                _logger.LogWarning(
                    "No email template for status {Status} - skipping email send",
                    newStatus);
                return Result<bool>.Success(true); // Not an error, just no template for this status
            }

            var variables = new Dictionary<string, string>
            {
                { "scene_name", applicantName },
                { "application_number", application.ApplicationNumber },
                { "submission_date", application.SubmittedAt.ToString("MMMM dd, yyyy") },
                { "application_date", application.CreatedAt.ToString("MMMM dd, yyyy") },
                { "status_change_date", DateTime.UtcNow.ToString("MMMM dd, yyyy") },
                { "current_status", newStatus.ToString() },
                { "approval_date", DateTime.UtcNow.ToString("MMMM dd, yyyy") },
                { "review_date", DateTime.UtcNow.ToString("MMMM dd, yyyy") },
                { "interview_link", GetInterviewSchedulingLink(application) },
                { "custom_message", customMessage ?? string.Empty }
            };

            var result = await _emailService.SendTemplatedEmailAsync(
                applicantEmail,
                applicantName,
                EmailCategory.Vetting,
                templateType,
                variables,
                cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Status update email sent: ApplicationNumber={ApplicationNumber}, Status={Status}, Template={Template}",
                    application.ApplicationNumber, newStatus, templateType);
                return Result<bool>.Success(true);
            }
            else
            {
                _logger.LogError(
                    "Failed to send status update email: ApplicationNumber={ApplicationNumber}, Status={Status}, Error={Error}",
                    application.ApplicationNumber, newStatus, result.Error);
                return Result<bool>.Failure(result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error sending status update email: ApplicationNumber={ApplicationNumber}, Status={Status}",
                application.ApplicationNumber, newStatus);
            return Result<bool>.Failure($"Error sending email: {ex.Message}");
        }
    }

    /// <summary>
    /// Send reminder email to applicant (e.g., interview reminder)
    /// Uses InterviewReminder email template
    /// </summary>
    public async Task<Result<bool>> SendReminderAsync(
        VettingApplication application,
        string applicantEmail,
        string applicantName,
        string? customMessage,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var variables = new Dictionary<string, string>
            {
                { "scene_name", applicantName },
                { "application_number", application.ApplicationNumber },
                { "submission_date", application.SubmittedAt.ToString("MMMM dd, yyyy") },
                { "application_date", application.CreatedAt.ToString("MMMM dd, yyyy") },
                { "status_change_date", DateTime.UtcNow.ToString("MMMM dd, yyyy") },
                { "current_status", application.WorkflowStatus.ToString() },
                { "custom_message", customMessage ?? string.Empty }
            };

            var result = await _emailService.SendTemplatedEmailAsync(
                applicantEmail,
                applicantName,
                EmailCategory.Vetting,
                "InterviewReminder",
                variables,
                cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Reminder email sent: ApplicationNumber={ApplicationNumber}, Email={Email}",
                    application.ApplicationNumber, applicantEmail);
                return Result<bool>.Success(true);
            }
            else
            {
                _logger.LogError(
                    "Failed to send reminder email: ApplicationNumber={ApplicationNumber}, Error={Error}",
                    application.ApplicationNumber, result.Error);
                return Result<bool>.Failure(result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error sending reminder email: ApplicationNumber={ApplicationNumber}",
                application.ApplicationNumber);
            return Result<bool>.Failure($"Error sending email: {ex.Message}");
        }
    }

    /// <summary>
    /// Get interview scheduling link (placeholder - replace with actual URL generation)
    /// </summary>
    private string GetInterviewSchedulingLink(VettingApplication application)
    {
        // TODO: Generate actual interview scheduling link
        return $"https://witchcityrope.com/vetting/interview/{application.Id}";
    }

}

using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.EmailTemplates.Entities;
using WitchCityRope.Api.Features.Shared.Models;
using WitchCityRope.Api.Features.Shared.Services;
using WitchCityRope.Api.Features.Users.Extensions;
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
    private readonly ApplicationDbContext _context;

    public VettingEmailService(
        IEmailService emailService,
        ILogger<VettingEmailService> logger,
        IConfiguration configuration,
        ApplicationDbContext context)
    {
        _emailService = emailService;
        _logger = logger;
        _configuration = configuration;
        _context = context;
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
                // interview_link powers the {{interview_link}} token in the InterviewReminder
                // template so reminder recipients can click through to the scheduling page.
                // Must match the variable name used by the InterviewApproved template in
                // SendStatusUpdateAsync above so admins can reuse the same token in both.
                // Omitting this caused a staging bug where reminder emails rendered a broken
                // (or empty) link — see conversation 2026-04-10.
                { "interview_link", GetInterviewSchedulingLink(application) },
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
    /// Send notification to all VettingTeam members that a new application has been submitted.
    /// No applicant PII is included — just a link to the vetting admin page for review.
    /// Uses WhereHasRole to query specifically for VettingTeam (not Administrators).
    /// </summary>
    public async Task<Result<bool>> SendNewApplicationTeamNotificationAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Query all users with VettingTeam role (not Administrators — per business requirement)
            var vettingTeamMembers = await _context.Users
                .AsNoTracking()
                .WhereHasRole("VettingTeam")
                .Where(u => !string.IsNullOrEmpty(u.Email))
                .ToListAsync(cancellationToken);

            if (vettingTeamMembers.Count == 0)
            {
                _logger.LogWarning("No VettingTeam members found to notify about new application");
                return Result<bool>.Success(true);
            }

            // Build environment-specific link to the vetting admin page
            var frontendUrl = _configuration["Frontend:Url"]?.TrimEnd('/') ?? "https://witchcityrope.com";
            var vettingAdminLink = $"{frontendUrl}/admin/vetting";

            var variables = new Dictionary<string, string>
            {
                { "vetting_admin_link", vettingAdminLink }
            };

            var successCount = 0;
            var failureCount = 0;

            foreach (var member in vettingTeamMembers)
            {
                try
                {
                    var result = await _emailService.SendTemplatedEmailAsync(
                        member.Email!,
                        member.SceneName ?? member.FirstName ?? "Vetting Team Member",
                        EmailCategory.Vetting,
                        "VettingTeamNewApplication",
                        variables,
                        cancellationToken);

                    if (result.IsSuccess)
                    {
                        successCount++;
                    }
                    else
                    {
                        failureCount++;
                        _logger.LogWarning(
                            "Failed to send new application notification to VettingTeam member {Email}: {Error}",
                            member.Email, result.Error);
                    }
                }
                catch (Exception ex)
                {
                    failureCount++;
                    _logger.LogError(ex,
                        "Exception sending new application notification to VettingTeam member {Email}",
                        member.Email);
                }
            }

            _logger.LogInformation(
                "New application team notification sent: {SuccessCount} succeeded, {FailureCount} failed out of {TotalCount} VettingTeam members",
                successCount, failureCount, vettingTeamMembers.Count);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending new application team notification to VettingTeam members");
            return Result<bool>.Failure($"Error sending team notification: {ex.Message}");
        }
    }

    /// <summary>
    /// Build interview scheduling link using the frontend URL from configuration.
    /// Links to the internal /vetting-interview-scheduling page with the application ID
    /// so the scheduling page can load the correct application context.
    /// </summary>
    private string GetInterviewSchedulingLink(VettingApplication application)
    {
        var frontendUrl = _configuration["Frontend:Url"]?.TrimEnd('/') ?? "https://witchcityrope.com";
        return $"{frontendUrl}/vetting-interview-scheduling?applicationId={application.Id}";
    }

}

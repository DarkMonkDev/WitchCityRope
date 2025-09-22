using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Shared.Models;
using WitchCityRope.Api.Features.Vetting.Entities;

namespace WitchCityRope.Api.Features.Vetting.Services;

/// <summary>
/// Email service implementation for vetting system
/// Currently implements basic logging for development - can be extended with SendGrid integration
/// </summary>
public class VettingEmailService : IVettingEmailService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<VettingEmailService> _logger;
    private readonly IConfiguration _configuration;

    public VettingEmailService(
        ApplicationDbContext context,
        ILogger<VettingEmailService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Send application confirmation email after successful submission
    /// </summary>
    public async Task<Result<bool>> SendApplicationConfirmationAsync(
        VettingApplication application,
        string applicantEmail,
        string applicantName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending application confirmation email for application {ApplicationNumber} to {Email}",
                application.ApplicationNumber, applicantEmail);

            // Get email template from database
            var template = await _context.VettingEmailTemplates
                .Where(t => t.TemplateType == EmailTemplateType.ApplicationReceived && t.IsActive)
                .OrderByDescending(t => t.Version)
                .FirstOrDefaultAsync(cancellationToken);

            if (template == null)
            {
                _logger.LogWarning("No active email template found for ApplicationReceived");
                return await SendDefaultConfirmationEmailAsync(application, applicantEmail, applicantName, cancellationToken);
            }

            // Render template with variables
            var subject = RenderTemplate(template.Subject, application, applicantName);
            var body = RenderTemplate(template.Body, application, applicantName);

            // For development: Log email content instead of sending
            var emailLog = new
            {
                To = applicantEmail,
                Subject = subject,
                Body = body,
                ApplicationId = application.Id,
                ApplicationNumber = application.ApplicationNumber,
                TemplateType = "ApplicationReceived",
                SentAt = DateTime.UtcNow
            };

            _logger.LogInformation("Email Content: {EmailLog}", JsonSerializer.Serialize(emailLog, new JsonSerializerOptions { WriteIndented = true }));

            // TODO: Implement actual SendGrid integration here
            // await SendViaEmailProviderAsync(applicantEmail, subject, body, cancellationToken);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send application confirmation email for application {ApplicationNumber}",
                application.ApplicationNumber);
            return Result<bool>.Failure("Email send failed", "Failed to send confirmation email");
        }
    }

    /// <summary>
    /// Send status change notification email
    /// </summary>
    public async Task<Result<bool>> SendStatusChangeNotificationAsync(
        VettingApplication application,
        string applicantEmail,
        string applicantName,
        ApplicationStatus newStatus,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending status change notification for application {ApplicationNumber} to {Email}, new status: {Status}",
                application.ApplicationNumber, applicantEmail, newStatus);

            // Map status to template type
            var templateType = GetTemplateTypeForStatus(newStatus);
            if (templateType == null)
            {
                _logger.LogInformation("No email template needed for status {Status}", newStatus);
                return Result<bool>.Success(true);
            }

            // Get email template from database
            var template = await _context.VettingEmailTemplates
                .Where(t => t.TemplateType == templateType && t.IsActive)
                .OrderByDescending(t => t.Version)
                .FirstOrDefaultAsync(cancellationToken);

            if (template == null)
            {
                _logger.LogWarning("No active email template found for {TemplateType}", templateType);
                return await SendDefaultStatusChangeEmailAsync(application, applicantEmail, applicantName, newStatus, cancellationToken);
            }

            // Render template with variables
            var subject = RenderTemplate(template.Subject, application, applicantName);
            var body = RenderTemplate(template.Body, application, applicantName);

            // For development: Log email content instead of sending
            var emailLog = new
            {
                To = applicantEmail,
                Subject = subject,
                Body = body,
                ApplicationId = application.Id,
                ApplicationNumber = application.ApplicationNumber,
                TemplateType = templateType.ToString(),
                NewStatus = newStatus.ToString(),
                SentAt = DateTime.UtcNow
            };

            _logger.LogInformation("Email Content: {EmailLog}", JsonSerializer.Serialize(emailLog, new JsonSerializerOptions { WriteIndented = true }));

            // TODO: Implement actual SendGrid integration here
            // await SendViaEmailProviderAsync(applicantEmail, subject, body, cancellationToken);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send status change notification for application {ApplicationNumber}",
                application.ApplicationNumber);
            return Result<bool>.Failure("Email send failed", "Failed to send status change notification");
        }
    }

    /// <summary>
    /// Test email service connectivity
    /// </summary>
    public Task<Result<bool>> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Testing email service connection");
            // For development mode, always return success
            // TODO: Implement actual SendGrid connection test
            return Task.FromResult(Result<bool>.Success(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email service connection test failed");
            return Task.FromResult(Result<bool>.Failure("Connection failed", "Email service is not available"));
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Render template by replacing variables with actual values
    /// </summary>
    private static string RenderTemplate(string template, VettingApplication application, string applicantName)
    {
        return template
            .Replace("{{applicant_name}}", applicantName)
            .Replace("{{application_number}}", application.ApplicationNumber)
            .Replace("{{application_date}}", application.CreatedAt.ToString("MMMM dd, yyyy"))
            .Replace("{{status_change_date}}", DateTime.UtcNow.ToString("MMMM dd, yyyy"))
            .Replace("{{contact_email}}", "support@witchcityrope.com");
    }

    /// <summary>
    /// Map application status to email template type
    /// </summary>
    private static EmailTemplateType? GetTemplateTypeForStatus(ApplicationStatus status)
    {
        return status switch
        {
            ApplicationStatus.InterviewApproved => EmailTemplateType.InterviewApproved,
            ApplicationStatus.Approved => EmailTemplateType.ApplicationApproved,
            ApplicationStatus.OnHold => EmailTemplateType.ApplicationOnHold,
            ApplicationStatus.Denied => EmailTemplateType.ApplicationDenied,
            _ => null // No email needed for other statuses
        };
    }

    /// <summary>
    /// Send default confirmation email when template is not available
    /// </summary>
    private async Task<Result<bool>> SendDefaultConfirmationEmailAsync(
        VettingApplication application,
        string applicantEmail,
        string applicantName,
        CancellationToken cancellationToken)
    {
        var subject = "WitchCityRope Vetting Application Received";
        var body = $@"
Dear {applicantName},

Thank you for submitting your vetting application to WitchCityRope.

Application Details:
- Application Number: {application.ApplicationNumber}
- Submitted: {application.CreatedAt:MMMM dd, yyyy}
- Status: Under Review

Our vetting team will review your application and contact you within the next few business days.

If you have any questions, please contact us at support@witchcityrope.com.

Best regards,
The WitchCityRope Team
";

        _logger.LogInformation("Sending default confirmation email to {Email}", applicantEmail);
        _logger.LogInformation("Email Content - Subject: {Subject}, Body: {Body}", subject, body);

        // TODO: Implement actual email sending
        return Result<bool>.Success(true);
    }

    /// <summary>
    /// Send default status change email when template is not available
    /// </summary>
    private async Task<Result<bool>> SendDefaultStatusChangeEmailAsync(
        VettingApplication application,
        string applicantEmail,
        string applicantName,
        ApplicationStatus newStatus,
        CancellationToken cancellationToken)
    {
        var subject = $"WitchCityRope Application Update - {application.ApplicationNumber}";
        var body = $@"
Dear {applicantName},

Your vetting application status has been updated.

Application Details:
- Application Number: {application.ApplicationNumber}
- New Status: {GetStatusDescription(newStatus)}
- Updated: {DateTime.UtcNow:MMMM dd, yyyy}

{GetNextStepsForStatus(newStatus)}

If you have any questions, please contact us at support@witchcityrope.com.

Best regards,
The WitchCityRope Team
";

        _logger.LogInformation("Sending default status change email to {Email} for status {Status}", applicantEmail, newStatus);
        _logger.LogInformation("Email Content - Subject: {Subject}, Body: {Body}", subject, body);

        // TODO: Implement actual email sending
        return Result<bool>.Success(true);
    }

    /// <summary>
    /// Get user-friendly status description
    /// </summary>
    private static string GetStatusDescription(ApplicationStatus status)
    {
        return status switch
        {
            ApplicationStatus.UnderReview => "Under Review",
            ApplicationStatus.InterviewApproved => "Interview Approved",
            ApplicationStatus.Approved => "Application Approved",
            ApplicationStatus.OnHold => "Application On Hold",
            ApplicationStatus.Denied => "Application Declined",
            _ => status.ToString()
        };
    }

    /// <summary>
    /// Get next steps message for status
    /// </summary>
    private static string GetNextStepsForStatus(ApplicationStatus status)
    {
        return status switch
        {
            ApplicationStatus.InterviewApproved => "We will contact you soon to schedule your interview.",
            ApplicationStatus.Approved => "Welcome to WitchCityRope! You now have access to our community events and resources.",
            ApplicationStatus.OnHold => "We need additional information from you. Please check your email for details.",
            ApplicationStatus.Denied => "While your application was not approved at this time, you are welcome to reapply in the future.",
            _ => "We will contact you if any further action is needed."
        };
    }

    #endregion
}
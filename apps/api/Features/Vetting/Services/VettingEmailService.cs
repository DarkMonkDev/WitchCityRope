using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Shared.Models;
using WitchCityRope.Api.Features.Vetting.Entities;
using WitchCityRope.Api.Features.Vetting.Models;

namespace WitchCityRope.Api.Features.Vetting.Services;

/// <summary>
/// Email service implementation for vetting system with SendGrid integration
/// Supports both mock mode (logs to console) and production mode (sends via SendGrid)
/// All email attempts are logged to VettingEmailLog database table for auditing
/// </summary>
public class VettingEmailService : IVettingEmailService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<VettingEmailService> _logger;
    private readonly VettingEmailConfiguration _emailConfig;
    private readonly ISendGridClient? _sendGridClient;

    public VettingEmailService(
        ApplicationDbContext context,
        ILogger<VettingEmailService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;

        // Load email configuration from appsettings
        _emailConfig = new VettingEmailConfiguration
        {
            EmailEnabled = configuration.GetValue<bool>("Vetting:EmailEnabled"),
            SendGridApiKey = configuration["Vetting:SendGridApiKey"] ?? string.Empty,
            FromEmail = configuration["Vetting:FromEmail"] ?? "noreply@witchcityrope.com",
            FromName = configuration["Vetting:FromName"] ?? "WitchCityRope"
        };

        // Initialize SendGrid client only if email is enabled and API key is configured
        if (_emailConfig.EmailEnabled && !string.IsNullOrEmpty(_emailConfig.SendGridApiKey))
        {
            _sendGridClient = new SendGridClient(_emailConfig.SendGridApiKey);
            _logger.LogInformation("VettingEmailService initialized with SendGrid integration enabled");
        }
        else
        {
            _logger.LogInformation("VettingEmailService initialized in MOCK mode - emails will be logged only");
        }
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
            _logger.LogInformation(
                "Sending application confirmation email for application {ApplicationNumber} to {Email}",
                application.ApplicationNumber, applicantEmail);

            // Get email template from database
            var template = await _context.VettingEmailTemplates
                .Where(t => t.TemplateType == EmailTemplateType.ApplicationReceived && t.IsActive)
                .OrderByDescending(t => t.Version)
                .FirstOrDefaultAsync(cancellationToken);

            string subject, htmlBody, plainTextBody;
            if (template != null)
            {
                // Render template with variables
                subject = RenderTemplate(template.Subject, application, applicantName);
                htmlBody = RenderTemplate(template.HtmlBody, application, applicantName);
                plainTextBody = RenderTemplate(template.PlainTextBody, application, applicantName);
            }
            else
            {
                // Use default template
                subject = "WitchCityRope Vetting Application Received";
                htmlBody = BuildDefaultConfirmationHtml(application, applicantName);
                plainTextBody = BuildDefaultConfirmationPlainText(application, applicantName);
            }

            // Send email (or log in mock mode)
            var sendResult = await SendEmailAsync(
                applicantEmail,
                subject,
                htmlBody,
                plainTextBody,
                application.Id,
                EmailTemplateType.ApplicationReceived,
                cancellationToken);

            if (sendResult.IsSuccess)
            {
                _logger.LogInformation(
                    "Successfully sent application confirmation email for application {ApplicationNumber}",
                    application.ApplicationNumber);
            }

            return sendResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send application confirmation email for application {ApplicationNumber}",
                application.ApplicationNumber);
            return Result<bool>.Failure("Email send failed", "Failed to send confirmation email");
        }
    }

    /// <summary>
    /// Send status update notification email when application status changes
    /// Maps status to appropriate template: InterviewApproved, Approved, OnHold, Denied
    /// </summary>
    public async Task<Result<bool>> SendStatusUpdateAsync(
        VettingApplication application,
        string applicantEmail,
        string applicantName,
        VettingStatus newStatus,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Sending status update notification for application {ApplicationNumber} to {Email}, new status: {Status}",
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

            string subject, htmlBody, plainTextBody;
            if (template != null)
            {
                // Render template with variables
                subject = RenderTemplate(template.Subject, application, applicantName);
                htmlBody = RenderTemplate(template.HtmlBody, application, applicantName);
                plainTextBody = RenderTemplate(template.PlainTextBody, application, applicantName);
            }
            else
            {
                // Use default template
                subject = $"WitchCityRope Application Update - {application.ApplicationNumber}";
                htmlBody = BuildDefaultStatusUpdateHtml(application, applicantName, newStatus);
                plainTextBody = BuildDefaultStatusUpdatePlainText(application, applicantName, newStatus);
            }

            // Send email (or log in mock mode)
            var sendResult = await SendEmailAsync(
                applicantEmail,
                subject,
                htmlBody,
                plainTextBody,
                application.Id,
                templateType.Value,
                cancellationToken);

            if (sendResult.IsSuccess)
            {
                _logger.LogInformation(
                    "Successfully sent status update notification for application {ApplicationNumber}",
                    application.ApplicationNumber);
            }

            return sendResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send status update notification for application {ApplicationNumber}",
                application.ApplicationNumber);
            return Result<bool>.Failure("Email send failed", "Failed to send status update notification");
        }
    }

    /// <summary>
    /// Send reminder email to applicant (e.g., interview reminder)
    /// Uses InterviewReminder email template with optional custom message
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
            _logger.LogInformation(
                "Sending reminder email for application {ApplicationNumber} to {Email}",
                application.ApplicationNumber, applicantEmail);

            // Get reminder template from database
            var template = await _context.VettingEmailTemplates
                .Where(t => t.TemplateType == EmailTemplateType.InterviewReminder && t.IsActive)
                .OrderByDescending(t => t.Version)
                .FirstOrDefaultAsync(cancellationToken);

            string subject, htmlBody, plainTextBody;
            if (template != null)
            {
                // Render template with variables
                subject = RenderTemplate(template.Subject, application, applicantName);
                htmlBody = RenderTemplate(template.HtmlBody, application, applicantName);
                plainTextBody = RenderTemplate(template.PlainTextBody, application, applicantName);

                // Add custom message if provided
                if (!string.IsNullOrEmpty(customMessage))
                {
                    htmlBody = htmlBody.Replace("{{custom_message}}", customMessage);
                    plainTextBody = plainTextBody.Replace("{{custom_message}}", customMessage);
                }
            }
            else
            {
                // Use default reminder template
                subject = $"WitchCityRope Application Reminder - {application.ApplicationNumber}";
                htmlBody = BuildDefaultReminderHtml(application, applicantName, customMessage);
                plainTextBody = BuildDefaultReminderPlainText(application, applicantName, customMessage);
            }

            // Send email (or log in mock mode)
            var sendResult = await SendEmailAsync(
                applicantEmail,
                subject,
                htmlBody,
                plainTextBody,
                application.Id,
                EmailTemplateType.InterviewReminder,
                cancellationToken);

            if (sendResult.IsSuccess)
            {
                _logger.LogInformation(
                    "Successfully sent reminder email for application {ApplicationNumber}",
                    application.ApplicationNumber);
            }

            return sendResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send reminder email for application {ApplicationNumber}",
                application.ApplicationNumber);
            return Result<bool>.Failure("Email send failed", "Failed to send reminder email");
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Core email sending logic with SendGrid integration or mock logging
    /// Creates VettingEmailLog for all attempts regardless of mode
    /// </summary>
    private async Task<Result<bool>> SendEmailAsync(
        string recipientEmail,
        string subject,
        string htmlBody,
        string plainTextBody,
        Guid applicationId,
        EmailTemplateType templateType,
        CancellationToken cancellationToken = default)
    {
        var emailLog = new VettingEmailLog
        {
            Id = Guid.NewGuid(),
            ApplicationId = applicationId,
            TemplateType = templateType,
            RecipientEmail = recipientEmail,
            Subject = subject,
            SentAt = DateTime.UtcNow,
            DeliveryStatus = EmailDeliveryStatus.Pending,
            RetryCount = 0
        };

        try
        {
            if (_emailConfig.EmailEnabled && _sendGridClient != null)
            {
                // Production mode - Send via SendGrid
                var from = new EmailAddress(_emailConfig.FromEmail, _emailConfig.FromName);
                var to = new EmailAddress(recipientEmail);
                var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextBody, htmlBody);

                var response = await _sendGridClient.SendEmailAsync(msg, cancellationToken);

                // Extract SendGrid message ID from headers
                if (response.Headers.TryGetValues("X-Message-Id", out var messageIds))
                {
                    emailLog.SendGridMessageId = messageIds.FirstOrDefault();
                }

                // Check response status
                if (response.IsSuccessStatusCode)
                {
                    emailLog.DeliveryStatus = EmailDeliveryStatus.Sent;
                    _logger.LogInformation(
                        "SendGrid email sent successfully. MessageId: {MessageId}, Status: {StatusCode}",
                        emailLog.SendGridMessageId, response.StatusCode);
                }
                else
                {
                    emailLog.DeliveryStatus = EmailDeliveryStatus.Failed;
                    var errorBody = await response.Body.ReadAsStringAsync(cancellationToken);
                    emailLog.ErrorMessage = $"SendGrid returned {response.StatusCode}: {errorBody}";
                    _logger.LogError(
                        "SendGrid email send failed. Status: {StatusCode}, Error: {Error}",
                        response.StatusCode, errorBody);
                }
            }
            else
            {
                // Mock mode - Log email content to console
                emailLog.DeliveryStatus = EmailDeliveryStatus.Sent;
                emailLog.SendGridMessageId = null; // No actual send

                _logger.LogInformation(
                    "MOCK EMAIL - Would send to: {Recipient}\n" +
                    "Subject: {Subject}\n" +
                    "HTML Body:\n{HtmlBody}\n" +
                    "Plain Text Body:\n{PlainTextBody}",
                    recipientEmail, subject, htmlBody, plainTextBody);
            }

            // Save email log to database
            _context.VettingEmailLogs.Add(emailLog);
            await _context.SaveChangesAsync(cancellationToken);

            return emailLog.DeliveryStatus == EmailDeliveryStatus.Sent
                ? Result<bool>.Success(true)
                : Result<bool>.Failure("Email send failed", emailLog.ErrorMessage ?? "Unknown error");
        }
        catch (Exception ex)
        {
            emailLog.DeliveryStatus = EmailDeliveryStatus.Failed;
            emailLog.ErrorMessage = ex.Message;

            try
            {
                _context.VettingEmailLogs.Add(emailLog);
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception dbEx)
            {
                _logger.LogError(dbEx, "Failed to save email log to database");
            }

            _logger.LogError(ex, "Exception occurred while sending email to {Recipient}", recipientEmail);
            return Result<bool>.Failure("Email send exception", ex.Message);
        }
    }

    /// <summary>
    /// Render template by replacing variables with actual values
    /// Supports multiple variable formats for compatibility
    /// </summary>
    private static string RenderTemplate(string template, VettingApplication application, string applicantName)
    {
        return template
            .Replace("{{applicant_name}}", applicantName)
            .Replace("{{application_number}}", application.ApplicationNumber)
            .Replace("{{application_date}}", application.SubmittedAt.ToString("MMMM dd, yyyy"))
            .Replace("{{submission_date}}", application.SubmittedAt.ToString("MMMM dd, yyyy"))
            .Replace("{{status_change_date}}", DateTime.UtcNow.ToString("MMMM dd, yyyy"))
            .Replace("{{contact_email}}", "support@witchcityrope.com")
            .Replace("{{current_status}}", application.WorkflowStatus.ToString());
    }

    /// <summary>
    /// Map application status to email template type
    /// Only certain status changes trigger email notifications
    /// Updated for Calendly external interview scheduling workflow
    /// </summary>
    private static EmailTemplateType? GetTemplateTypeForStatus(VettingStatus status)
    {
        return status switch
        {
            VettingStatus.InterviewApproved => EmailTemplateType.InterviewApproved,
            VettingStatus.FinalReview => null, // No email for FinalReview (internal step)
            VettingStatus.OnHold => EmailTemplateType.OnHold,
            VettingStatus.Approved => EmailTemplateType.Approved,
            VettingStatus.Denied => EmailTemplateType.Denied,
            VettingStatus.Withdrawn => null, // No email for withdrawn
            _ => null // No email needed for other statuses
        };
    }

    #endregion

    #region Default Email Templates

    /// <summary>
    /// Default HTML confirmation email when no template exists in database
    /// </summary>
    private static string BuildDefaultConfirmationHtml(VettingApplication application, string applicantName)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>Application Confirmation</title>
</head>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333;"">
    <div style=""max-width: 600px; margin: 0 auto; padding: 20px;"">
        <h2 style=""color: #4a5568;"">Application Received</h2>

        <p>Dear {applicantName},</p>

        <p>Thank you for submitting your vetting application to WitchCityRope.</p>

        <div style=""background-color: #f7fafc; padding: 15px; border-radius: 5px; margin: 20px 0;"">
            <h3 style=""margin-top: 0; color: #2d3748;"">Application Details</h3>
            <ul style=""list-style: none; padding: 0;"">
                <li><strong>Application Number:</strong> {application.ApplicationNumber}</li>
                <li><strong>Submitted:</strong> {application.SubmittedAt:MMMM dd, yyyy}</li>
                <li><strong>Status:</strong> {application.WorkflowStatus}</li>
            </ul>
        </div>

        <p>Our vetting team will review your application and contact you within the next few business days.</p>

        <p>If you have any questions, please contact us at <a href=""mailto:support@witchcityrope.com"">support@witchcityrope.com</a>.</p>

        <p style=""margin-top: 30px;"">
            Best regards,<br>
            <strong>The WitchCityRope Team</strong>
        </p>
    </div>
</body>
</html>";
    }

    /// <summary>
    /// Default plain text confirmation email when no template exists in database
    /// </summary>
    private static string BuildDefaultConfirmationPlainText(VettingApplication application, string applicantName)
    {
        return $@"Dear {applicantName},

Thank you for submitting your vetting application to WitchCityRope.

Application Details:
- Application Number: {application.ApplicationNumber}
- Submitted: {application.SubmittedAt:MMMM dd, yyyy}
- Status: {application.WorkflowStatus}

Our vetting team will review your application and contact you within the next few business days.

If you have any questions, please contact us at support@witchcityrope.com.

Best regards,
The WitchCityRope Team";
    }

    /// <summary>
    /// Default HTML status update email when no template exists in database
    /// </summary>
    private static string BuildDefaultStatusUpdateHtml(
        VettingApplication application,
        string applicantName,
        VettingStatus newStatus)
    {
        var statusDescription = GetStatusDescription(newStatus);
        var nextSteps = GetNextStepsForStatus(newStatus);

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>Application Status Update</title>
</head>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333;"">
    <div style=""max-width: 600px; margin: 0 auto; padding: 20px;"">
        <h2 style=""color: #4a5568;"">Application Status Update</h2>

        <p>Dear {applicantName},</p>

        <p>Your vetting application status has been updated.</p>

        <div style=""background-color: #f7fafc; padding: 15px; border-radius: 5px; margin: 20px 0;"">
            <h3 style=""margin-top: 0; color: #2d3748;"">Application Details</h3>
            <ul style=""list-style: none; padding: 0;"">
                <li><strong>Application Number:</strong> {application.ApplicationNumber}</li>
                <li><strong>New Status:</strong> <span style=""color: #2b6cb0;"">{statusDescription}</span></li>
                <li><strong>Updated:</strong> {DateTime.UtcNow:MMMM dd, yyyy}</li>
            </ul>
        </div>

        <div style=""background-color: #edf2f7; padding: 15px; border-radius: 5px; margin: 20px 0;"">
            <h3 style=""margin-top: 0; color: #2d3748;"">Next Steps</h3>
            <p>{nextSteps}</p>
        </div>

        <p>If you have any questions, please contact us at <a href=""mailto:support@witchcityrope.com"">support@witchcityrope.com</a>.</p>

        <p style=""margin-top: 30px;"">
            Best regards,<br>
            <strong>The WitchCityRope Team</strong>
        </p>
    </div>
</body>
</html>";
    }

    /// <summary>
    /// Default plain text status update email when no template exists in database
    /// </summary>
    private static string BuildDefaultStatusUpdatePlainText(
        VettingApplication application,
        string applicantName,
        VettingStatus newStatus)
    {
        var statusDescription = GetStatusDescription(newStatus);
        var nextSteps = GetNextStepsForStatus(newStatus);

        return $@"Dear {applicantName},

Your vetting application status has been updated.

Application Details:
- Application Number: {application.ApplicationNumber}
- New Status: {statusDescription}
- Updated: {DateTime.UtcNow:MMMM dd, yyyy}

Next Steps:
{nextSteps}

If you have any questions, please contact us at support@witchcityrope.com.

Best regards,
The WitchCityRope Team";
    }

    /// <summary>
    /// Default HTML reminder email when no template exists in database
    /// </summary>
    private static string BuildDefaultReminderHtml(
        VettingApplication application,
        string applicantName,
        string? customMessage)
    {
        var customMessageHtml = string.IsNullOrEmpty(customMessage)
            ? ""
            : $@"
        <div style=""background-color: #fef5e7; padding: 15px; border-radius: 5px; margin: 20px 0;"">
            <h3 style=""margin-top: 0; color: #2d3748;"">Additional Information</h3>
            <p>{customMessage}</p>
        </div>";

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>Application Reminder</title>
</head>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333;"">
    <div style=""max-width: 600px; margin: 0 auto; padding: 20px;"">
        <h2 style=""color: #4a5568;"">Application Reminder</h2>

        <p>Dear {applicantName},</p>

        <p>This is a reminder regarding your vetting application with WitchCityRope.</p>

        <div style=""background-color: #f7fafc; padding: 15px; border-radius: 5px; margin: 20px 0;"">
            <h3 style=""margin-top: 0; color: #2d3748;"">Application Details</h3>
            <ul style=""list-style: none; padding: 0;"">
                <li><strong>Application Number:</strong> {application.ApplicationNumber}</li>
                <li><strong>Current Status:</strong> {application.WorkflowStatus}</li>
                <li><strong>Submitted:</strong> {application.SubmittedAt:MMMM dd, yyyy}</li>
            </ul>
        </div>
        {customMessageHtml}
        <p>If you have any questions, please contact us at <a href=""mailto:support@witchcityrope.com"">support@witchcityrope.com</a>.</p>

        <p style=""margin-top: 30px;"">
            Best regards,<br>
            <strong>The WitchCityRope Team</strong>
        </p>
    </div>
</body>
</html>";
    }

    /// <summary>
    /// Default plain text reminder email when no template exists in database
    /// </summary>
    private static string BuildDefaultReminderPlainText(
        VettingApplication application,
        string applicantName,
        string? customMessage)
    {
        var customMessageText = string.IsNullOrEmpty(customMessage)
            ? ""
            : $@"

Additional Information:
{customMessage}";

        return $@"Dear {applicantName},

This is a reminder regarding your vetting application with WitchCityRope.

Application Details:
- Application Number: {application.ApplicationNumber}
- Current Status: {application.WorkflowStatus}
- Submitted: {application.SubmittedAt:MMMM dd, yyyy}
{customMessageText}

If you have any questions, please contact us at support@witchcityrope.com.

Best regards,
The WitchCityRope Team";
    }

    /// <summary>
    /// Get user-friendly status description for display in emails
    /// Updated for Calendly external interview scheduling workflow
    /// </summary>
    private static string GetStatusDescription(VettingStatus status)
    {
        return status switch
        {
            VettingStatus.UnderReview => "Under Review",
            VettingStatus.InterviewApproved => "Interview Approved",
            VettingStatus.FinalReview => "Final Review",
            VettingStatus.OnHold => "On Hold",
            VettingStatus.Approved => "Approved",
            VettingStatus.Denied => "Denied",
            VettingStatus.Withdrawn => "Withdrawn",
            _ => status.ToString()
        };
    }

    /// <summary>
    /// Get next steps message based on application status
    /// Provides clear guidance to applicants on what to expect
    /// Updated for Calendly external interview scheduling workflow
    /// </summary>
    private static string GetNextStepsForStatus(VettingStatus status)
    {
        return status switch
        {
            VettingStatus.UnderReview =>
                "Your application is being reviewed by our team. We will contact you with updates.",
            VettingStatus.InterviewApproved =>
                "Please check your email for the Calendly link to schedule your interview.",
            VettingStatus.FinalReview =>
                "Your interview has been completed and your application is under final review.",
            VettingStatus.OnHold =>
                "We need additional information from you. Please check your email for details.",
            VettingStatus.Approved =>
                "Welcome to WitchCityRope! You now have access to our community events and resources.",
            VettingStatus.Denied =>
                "While your application was not approved at this time, you are welcome to reapply in the future.",
            VettingStatus.Withdrawn =>
                "Your application has been withdrawn.",
            _ => "We will contact you if any further action is needed."
        };
    }

    #endregion
}

/// <summary>
/// Configuration settings for vetting email service
/// Loaded from appsettings.json Vetting section
/// </summary>
internal class VettingEmailConfiguration
{
    public bool EmailEnabled { get; set; }
    public string SendGridApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
}

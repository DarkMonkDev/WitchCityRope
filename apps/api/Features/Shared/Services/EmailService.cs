using Microsoft.EntityFrameworkCore;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Text.RegularExpressions;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.EmailTemplates.Entities;
using WitchCityRope.Api.Features.Shared.Models;

namespace WitchCityRope.Api.Features.Shared.Services;

/// <summary>
/// Production-ready email service using SendGrid with GlobalEmailTemplates integration
/// Supports variable substitution, null-safe development mode, and comprehensive logging
/// </summary>
public class EmailService : IEmailService
{
    private readonly ISendGridClient? _sendGridClient;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EmailService> _logger;
    private readonly string _fromEmail;
    private readonly string _fromName;
    private readonly bool _isDevelopmentMode;

    public EmailService(
        ISendGridClient? sendGridClient,
        ApplicationDbContext context,
        ILogger<EmailService> logger,
        IConfiguration configuration)
    {
        _sendGridClient = sendGridClient;
        _context = context;
        _logger = logger;
        _fromEmail = configuration["Vetting:FromEmail"] ?? "noreply@witchcityrope.com";
        _fromName = configuration["Vetting:FromName"] ?? "Witch City Rope";
        _isDevelopmentMode = sendGridClient == null;

        if (_isDevelopmentMode)
        {
            _logger.LogWarning("EmailService running in DEVELOPMENT MODE - emails will be logged to console only (SendGrid not configured)");
        }
    }

    /// <summary>
    /// Send email using GlobalEmailTemplate fetched from database by category and template type.
    /// Does not check for event-specific overrides. Use the overload with eventId for event emails.
    /// </summary>
    public Task<Result> SendTemplatedEmailAsync(
        string toEmail,
        string toName,
        EmailCategory category,
        string templateType,
        Dictionary<string, string> variables,
        CancellationToken cancellationToken = default)
    {
        // Delegate to the event-aware overload with no event override
        return SendTemplatedEmailAsync(toEmail, toName, category, templateType, variables, eventId: null, cancellationToken);
    }

    /// <summary>
    /// Send email using template with optional event-specific override.
    /// When eventId is provided, checks for an EventEmailTemplate override first.
    /// Falls back to GlobalEmailTemplate if no event override exists.
    /// </summary>
    public async Task<Result> SendTemplatedEmailAsync(
        string toEmail,
        string toName,
        EmailCategory category,
        string templateType,
        Dictionary<string, string> variables,
        Guid? eventId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                return Result.Failure("Recipient email is required");
            }

            // Fetch the global template from database (always needed as fallback)
            var globalTemplate = await _context.GlobalEmailTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    t => t.Category == category && t.TemplateType == templateType && t.IsActive,
                    cancellationToken);

            if (globalTemplate == null)
            {
                _logger.LogError(
                    "Email template not found: Category={Category}, TemplateType={TemplateType}",
                    category, templateType);
                return Result.Failure($"Email template '{templateType}' not found in category '{category}'");
            }

            // Check for event-specific template override when an eventId is provided.
            // EventEmailTemplate overrides subject, htmlBody, and plainTextBody.
            // The OverrideSendingEnabled field controls whether sending is enabled at the event level;
            // when null, the global template's SendingEnabled is used.
            EventEmailTemplate? eventOverride = null;
            if (eventId.HasValue)
            {
                eventOverride = await _context.EventEmailTemplates
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        t => t.EventId == eventId.Value && t.TemplateType == templateType,
                        cancellationToken);

                if (eventOverride != null)
                {
                    _logger.LogDebug(
                        "Using event-specific template override: EventId={EventId}, TemplateType={TemplateType}",
                        eventId.Value, templateType);
                }
            }

            // Determine if sending is enabled.
            // Event override's OverrideSendingEnabled takes precedence when non-null;
            // otherwise fall back to the global template's SendingEnabled.
            var sendingEnabled = eventOverride?.OverrideSendingEnabled ?? globalTemplate.SendingEnabled;

            if (!sendingEnabled)
            {
                _logger.LogInformation(
                    "Email suppressed (SendingEnabled=false): Category={Category}, TemplateType={TemplateType}, To={ToEmail}, EventId={EventId}",
                    category, templateType, toEmail, eventId);
                return Result.Success();
            }

            // Use event override content if available, otherwise use global template
            var rawSubject = eventOverride?.Subject ?? globalTemplate.Subject;
            var rawHtmlBody = eventOverride?.HtmlBody ?? globalTemplate.HtmlBody;
            var rawPlainTextBody = eventOverride?.PlainTextBody ?? globalTemplate.PlainTextBody;

            // Perform variable substitution
            var subject = SubstituteVariables(rawSubject, variables);
            var htmlBody = SubstituteVariables(rawHtmlBody, variables);
            var plainTextBody = SubstituteVariables(rawPlainTextBody, variables);

            // Development mode: Log to console instead of sending
            if (_isDevelopmentMode)
            {
                _logger.LogInformation(
                    "EMAIL (Development Mode - NOT SENT):\n" +
                    "To: {ToEmail} ({ToName})\n" +
                    "From: {FromEmail} ({FromName})\n" +
                    "Subject: {Subject}\n" +
                    "Template: {Category}/{TemplateType}\n" +
                    "EventOverride: {HasOverride}\n" +
                    "HTML Body:\n{HtmlBody}\n" +
                    "Plain Text Body:\n{PlainTextBody}",
                    toEmail, toName, _fromEmail, _fromName, subject, category, templateType,
                    eventOverride != null, htmlBody, plainTextBody);

                return Result.Success(); // Return success to prevent blocking flows
            }

            // Production mode: Send via SendGrid
            var from = new EmailAddress(_fromEmail, _fromName);
            var to = new EmailAddress(toEmail, toName);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextBody, htmlBody);

            var response = await _sendGridClient!.SendEmailAsync(msg, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Email sent successfully: To={ToEmail}, Template={Category}/{TemplateType}, Subject={Subject}, EventOverride={HasOverride}",
                    toEmail, category, templateType, subject, eventOverride != null);
                return Result.Success();
            }
            else
            {
                var responseBody = await response.Body.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "SendGrid API error: StatusCode={StatusCode}, Response={Response}, To={ToEmail}, Template={Category}/{TemplateType}",
                    response.StatusCode, responseBody, toEmail, category, templateType);
                return Result.Failure($"Failed to send email: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error sending templated email: Category={Category}, TemplateType={TemplateType}, ToEmail={ToEmail}, EventId={EventId}",
                category, templateType, toEmail, eventId);
            return Result.Failure($"Error sending email: {ex.Message}");
        }
    }

    /// <summary>
    /// Send simple email without using a template (for ad-hoc scenarios)
    /// </summary>
    public async Task<Result> SendEmailAsync(
        string toEmail,
        string subject,
        string htmlBody,
        string? plainTextBody = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                return Result.Failure("Recipient email is required");
            }

            if (string.IsNullOrWhiteSpace(subject))
            {
                return Result.Failure("Email subject is required");
            }

            if (string.IsNullOrWhiteSpace(htmlBody))
            {
                return Result.Failure("Email body is required");
            }

            // Use HTML body as plain text fallback if not provided
            plainTextBody ??= StripHtmlTags(htmlBody);

            // Development mode: Log to console instead of sending
            if (_isDevelopmentMode)
            {
                _logger.LogInformation(
                    "EMAIL (Development Mode - NOT SENT):\n" +
                    "To: {ToEmail}\n" +
                    "From: {FromEmail} ({FromName})\n" +
                    "Subject: {Subject}\n" +
                    "HTML Body:\n{HtmlBody}\n" +
                    "Plain Text Body:\n{PlainTextBody}",
                    toEmail, _fromEmail, _fromName, subject, htmlBody, plainTextBody);

                return Result.Success(); // Return success to prevent blocking flows
            }

            // Production mode: Send via SendGrid
            var from = new EmailAddress(_fromEmail, _fromName);
            var to = new EmailAddress(toEmail);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextBody, htmlBody);

            var response = await _sendGridClient!.SendEmailAsync(msg, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Email sent successfully: To={ToEmail}, Subject={Subject}",
                    toEmail, subject);
                return Result.Success();
            }
            else
            {
                var responseBody = await response.Body.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "SendGrid API error: StatusCode={StatusCode}, Response={Response}, To={ToEmail}, Subject={Subject}",
                    response.StatusCode, responseBody, toEmail, subject);
                return Result.Failure($"Failed to send email: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error sending email: ToEmail={ToEmail}, Subject={Subject}",
                toEmail, subject);
            return Result.Failure($"Error sending email: {ex.Message}");
        }
    }

    /// <summary>
    /// Replace {{variable_name}} placeholders with actual values from dictionary
    /// </summary>
    /// <param name="template">Template string with {{variable}} placeholders</param>
    /// <param name="variables">Dictionary of variable names (with or without braces) to values</param>
    /// <returns>String with variables substituted</returns>
    internal static string SubstituteVariables(string template, Dictionary<string, string> variables)
    {
        if (string.IsNullOrWhiteSpace(template) || variables == null || variables.Count == 0)
        {
            return template;
        }

        var result = template;

        foreach (var variable in variables)
        {
            // Support both "user_name" and "{{user_name}}" as dictionary keys
            var key = variable.Key;
            var variableName = key.StartsWith("{{") && key.EndsWith("}}")
                ? key
                : $"{{{{{key}}}}}";

            // Replace all occurrences of {{variable_name}}
            result = result.Replace(variableName, variable.Value ?? string.Empty);
        }

        return result;
    }

    /// <summary>
    /// Strip HTML tags from string to create plain text version
    /// </summary>
    private static string StripHtmlTags(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        // Remove HTML tags
        var stripped = Regex.Replace(html, @"<[^>]+>", string.Empty);

        // Decode HTML entities
        stripped = System.Net.WebUtility.HtmlDecode(stripped);

        // Normalize whitespace
        stripped = Regex.Replace(stripped, @"\s+", " ").Trim();

        return stripped;
    }
}

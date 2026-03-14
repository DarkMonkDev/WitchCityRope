using WitchCityRope.Api.Features.EmailTemplates.Entities;
using WitchCityRope.Api.Features.Shared.Models;

namespace WitchCityRope.Api.Features.Shared.Services;

/// <summary>
/// Service for sending emails via SendGrid using templated and plain email functionality
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Send email using GlobalEmailTemplate fetched from database by category and template type.
    /// Does not check for event-specific overrides.
    /// </summary>
    /// <param name="toEmail">Recipient email address</param>
    /// <param name="toName">Recipient display name</param>
    /// <param name="category">Email category (Vetting, Events, Admin, Incident, AdHoc)</param>
    /// <param name="templateType">Template type within category (e.g., "ApplicationReceived", "PasswordReset")</param>
    /// <param name="variables">Dictionary of variables to substitute in template (e.g., {"user_name": "John"})</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure with error message</returns>
    Task<Result> SendTemplatedEmailAsync(
        string toEmail,
        string toName,
        EmailCategory category,
        string templateType,
        Dictionary<string, string> variables,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send email using template with optional event-specific override.
    /// When eventId is provided, checks for an EventEmailTemplate override first.
    /// Falls back to GlobalEmailTemplate if no event override exists.
    /// </summary>
    /// <param name="toEmail">Recipient email address</param>
    /// <param name="toName">Recipient display name</param>
    /// <param name="category">Email category (Vetting, Events, Admin, Incident, AdHoc)</param>
    /// <param name="templateType">Template type within category (e.g., "Confirmation", "Reminder1Day")</param>
    /// <param name="variables">Dictionary of variables to substitute in template</param>
    /// <param name="eventId">Optional event ID to check for event-specific template overrides</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure with error message</returns>
    Task<Result> SendTemplatedEmailAsync(
        string toEmail,
        string toName,
        EmailCategory category,
        string templateType,
        Dictionary<string, string> variables,
        Guid? eventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send simple email without using a template (for ad-hoc scenarios)
    /// </summary>
    /// <param name="toEmail">Recipient email address</param>
    /// <param name="subject">Email subject line</param>
    /// <param name="htmlBody">HTML email body</param>
    /// <param name="plainTextBody">Plain text email body (optional, falls back to HTML)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure with error message</returns>
    Task<Result> SendEmailAsync(
        string toEmail,
        string subject,
        string htmlBody,
        string? plainTextBody = null,
        CancellationToken cancellationToken = default);
}

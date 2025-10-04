using WitchCityRope.Api.Features.Shared.Models;
using WitchCityRope.Api.Features.Vetting.Entities;
using WitchCityRope.Api.Features.Vetting.Models;

namespace WitchCityRope.Api.Features.Vetting.Services;

/// <summary>
/// Service for sending vetting-related emails using SendGrid integration
/// Supports both mock mode (EmailEnabled: false) and production mode (EmailEnabled: true)
/// All email sends are logged to VettingEmailLog database table for tracking
/// </summary>
public interface IVettingEmailService
{
    /// <summary>
    /// Send application confirmation email after successful submission
    /// Uses ApplicationReceived email template
    /// </summary>
    /// <param name="application">The vetting application that was submitted</param>
    /// <param name="applicantEmail">Email address to send confirmation to</param>
    /// <param name="applicantName">Display name of the applicant for personalization</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Result indicating success or failure with error details</returns>
    Task<Result<bool>> SendApplicationConfirmationAsync(
        VettingApplication application,
        string applicantEmail,
        string applicantName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send status update notification email when application status changes
    /// Maps status to appropriate template: InterviewApproved, Approved, OnHold, Denied
    /// </summary>
    /// <param name="application">The vetting application with updated status</param>
    /// <param name="applicantEmail">Email address to send notification to</param>
    /// <param name="applicantName">Display name of the applicant for personalization</param>
    /// <param name="newStatus">The new status that triggered this notification</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Result indicating success or failure with error details</returns>
    Task<Result<bool>> SendStatusUpdateAsync(
        VettingApplication application,
        string applicantEmail,
        string applicantName,
        VettingStatus newStatus,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send reminder email to applicant (e.g., interview reminder)
    /// Uses InterviewReminder email template with optional custom message
    /// </summary>
    /// <param name="application">The vetting application to send reminder for</param>
    /// <param name="applicantEmail">Email address to send reminder to</param>
    /// <param name="applicantName">Display name of the applicant for personalization</param>
    /// <param name="customMessage">Optional custom message to include in email body</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Result indicating success or failure with error details</returns>
    Task<Result<bool>> SendReminderAsync(
        VettingApplication application,
        string applicantEmail,
        string applicantName,
        string? customMessage,
        CancellationToken cancellationToken = default);
}

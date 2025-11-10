using WitchCityRope.Api.Features.Shared.Models;
using WitchCityRope.Api.Features.Vetting.Entities;

namespace WitchCityRope.Api.Features.Vetting.Services;

/// <summary>
/// TEMPORARY STUB: Email service for vetting feature
/// This is a placeholder implementation that allows VettingService to compile and run
/// without actual email sending functionality.
///
/// TODO: Replace with GlobalEmailTemplates system integration
/// - Remove this stub implementation
/// - Update VettingService to use the new email template system
/// - Implement proper email sending via SendGrid
///
/// Created: 2025-11-09
/// Reason: VettingEmailService implementation was removed but VettingService still depends on IVettingEmailService
/// Impact: Email notifications for vetting status changes are currently disabled
/// </summary>
public class VettingEmailService : IVettingEmailService
{
    private readonly ILogger<VettingEmailService> _logger;

    public VettingEmailService(ILogger<VettingEmailService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Send application confirmation email after successful submission
    /// STUB: Currently returns success without sending email
    /// </summary>
    public Task<Result<bool>> SendApplicationConfirmationAsync(
        VettingApplication application,
        string applicantEmail,
        string applicantName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "VettingEmailService STUB: SendApplicationConfirmationAsync called for application {ApplicationNumber} " +
            "to {Email}. Email NOT sent (stub implementation). TODO: Integrate with GlobalEmailTemplates system.",
            application.ApplicationNumber, applicantEmail);

        // Return success to prevent blocking the application flow
        return Task.FromResult(Result<bool>.Success(true));
    }

    /// <summary>
    /// Send status update notification email when application status changes
    /// STUB: Currently returns success without sending email
    /// </summary>
    public Task<Result<bool>> SendStatusUpdateAsync(
        VettingApplication application,
        string applicantEmail,
        string applicantName,
        VettingStatus newStatus,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "VettingEmailService STUB: SendStatusUpdateAsync called for application {ApplicationNumber} " +
            "to {Email} with status {Status}. Email NOT sent (stub implementation). " +
            "TODO: Integrate with GlobalEmailTemplates system.",
            application.ApplicationNumber, applicantEmail, newStatus);

        // Return success to prevent blocking the application flow
        return Task.FromResult(Result<bool>.Success(true));
    }

    /// <summary>
    /// Send reminder email to applicant (e.g., interview reminder)
    /// STUB: Currently returns success without sending email
    /// </summary>
    public Task<Result<bool>> SendReminderAsync(
        VettingApplication application,
        string applicantEmail,
        string applicantName,
        string? customMessage,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "VettingEmailService STUB: SendReminderAsync called for application {ApplicationNumber} " +
            "to {Email}. Email NOT sent (stub implementation). TODO: Integrate with GlobalEmailTemplates system.",
            application.ApplicationNumber, applicantEmail);

        // Return success to prevent blocking the application flow
        return Task.FromResult(Result<bool>.Success(true));
    }
}

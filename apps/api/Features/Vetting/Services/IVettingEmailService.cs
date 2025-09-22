using WitchCityRope.Api.Features.Shared.Models;
using WitchCityRope.Api.Features.Vetting.Entities;

namespace WitchCityRope.Api.Features.Vetting.Services;

/// <summary>
/// Service for sending vetting-related emails
/// Handles template rendering and email delivery for vetting workflow
/// </summary>
public interface IVettingEmailService
{
    /// <summary>
    /// Send application confirmation email after successful submission
    /// </summary>
    Task<Result<bool>> SendApplicationConfirmationAsync(
        VettingApplication application,
        string applicantEmail,
        string applicantName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send status change notification email
    /// </summary>
    Task<Result<bool>> SendStatusChangeNotificationAsync(
        VettingApplication application,
        string applicantEmail,
        string applicantName,
        ApplicationStatus newStatus,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Test email service connectivity
    /// </summary>
    Task<Result<bool>> TestConnectionAsync(CancellationToken cancellationToken = default);
}
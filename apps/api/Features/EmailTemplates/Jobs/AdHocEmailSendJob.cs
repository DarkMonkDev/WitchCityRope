using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.EmailTemplates.Entities;
using WitchCityRope.Api.Features.Shared.Services;
using WitchCityRope.Api.Features.Vetting.Entities;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.EmailTemplates.Jobs;

/// <summary>
/// Hangfire background job that processes ad-hoc email sends.
/// Decouples the email sending loop from the HTTP request lifecycle so bulk sends
/// (hundreds of recipients) don't time out. The HTTP endpoint creates a SentAdHocEmail
/// record with status "Processing" and enqueues this job to do the actual sending.
/// </summary>
public class AdHocEmailSendJob
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdHocEmailSendJob> _logger;

    public AdHocEmailSendJob(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<AdHocEmailSendJob> logger)
    {
        _context = context;
        _userManager = userManager;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Processes the ad-hoc email send for a given SentAdHocEmail record.
    /// Called by Hangfire as a fire-and-forget job. The CancellationToken comes from
    /// Hangfire (fires on server shutdown), NOT from the HTTP request.
    /// </summary>
    public async Task ExecuteAsync(Guid sentAdHocEmailId, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "AdHocEmailSendJob starting: EmailId={EmailId}",
            sentAdHocEmailId);

        // Load the audit record (needs tracking since we'll update it)
        var sentEmail = await _context.SentAdHocEmails
            .FirstOrDefaultAsync(e => e.Id == sentAdHocEmailId, cancellationToken);

        if (sentEmail == null)
        {
            _logger.LogError(
                "AdHocEmailSendJob ABORTED: EmailId={EmailId} not found in database",
                sentAdHocEmailId);
            return;
        }

        // Guard against duplicate execution (Hangfire may retry failed jobs)
        if (sentEmail.DeliveryStatus != "Processing")
        {
            _logger.LogWarning(
                "AdHocEmailSendJob SKIPPED: EmailId={EmailId}, Status={Status} (expected Processing)",
                sentAdHocEmailId, sentEmail.DeliveryStatus);
            return;
        }

        var segmentLabel = sentEmail.Segment?.ToString() ?? "ManualList";

        try
        {
            // Resolve full user objects for per-user variable replacement.
            // For segment sends: re-resolve from segment query (authoritative, matches what was counted).
            // For manual email lists: look up users by stored email addresses.
            List<ApplicationUser> recipientUsers;
            if (sentEmail.Segment.HasValue)
            {
                recipientUsers = await GetUsersForSegmentAsync(sentEmail.Segment.Value, cancellationToken);
            }
            else
            {
                recipientUsers = await _context.Users
                    .Where(u => u.Email != null && sentEmail.RecipientEmails.Contains(u.Email))
                    .ToListAsync(cancellationToken);
            }

            // Check if template needs per-user variable replacement
            var needsPerUserReplacement = sentEmail.HtmlBody.Contains("{{reset_url}}")
                || sentEmail.HtmlBody.Contains("{{verification_url}}")
                || sentEmail.HtmlBody.Contains("{{user_name}}");

            var successCount = 0;
            var failureCount = 0;

            _logger.LogInformation(
                "AdHocEmailSendJob sending: EmailId={EmailId}, Subject={Subject}, Segment={Segment}, " +
                "Recipients={Count}, Mode={Mode}",
                sentAdHocEmailId, sentEmail.Subject, segmentLabel,
                recipientUsers.Count, needsPerUserReplacement ? "PerUser" : "Bulk");

            if (needsPerUserReplacement)
            {
                // Send individual emails with unique per-user tokens
                foreach (var user in recipientUsers)
                {
                    if (string.IsNullOrEmpty(user.Email)) continue;

                    try
                    {
                        var (personalizedHtml, personalizedPlainText) = await ReplaceVariablesForUserAsync(
                            sentEmail.HtmlBody, sentEmail.PlainTextBody, user, cancellationToken);

                        var result = await _emailService.SendEmailAsync(
                            user.Email,
                            sentEmail.Subject,
                            personalizedHtml,
                            personalizedPlainText,
                            cancellationToken);

                        if (result.IsSuccess)
                        {
                            successCount++;
                        }
                        else
                        {
                            failureCount++;
                            _logger.LogWarning(
                                "AdHocEmailSendJob FAILED: EmailId={EmailId}, Subject={Subject}, " +
                                "Segment={Segment}, Email={Email}, Error={Error}",
                                sentAdHocEmailId, sentEmail.Subject, segmentLabel,
                                user.Email, result.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        failureCount++;
                        _logger.LogError(ex,
                            "AdHocEmailSendJob EXCEPTION: EmailId={EmailId}, Subject={Subject}, " +
                            "Segment={Segment}, Email={Email}",
                            sentAdHocEmailId, sentEmail.Subject, segmentLabel, user.Email);
                    }
                }
            }
            else
            {
                // Send bulk email (same content to all recipients)
                foreach (var email in sentEmail.RecipientEmails)
                {
                    if (string.IsNullOrEmpty(email)) continue;

                    try
                    {
                        var result = await _emailService.SendEmailAsync(
                            email,
                            sentEmail.Subject,
                            sentEmail.HtmlBody,
                            sentEmail.PlainTextBody,
                            cancellationToken);

                        if (result.IsSuccess)
                        {
                            successCount++;
                        }
                        else
                        {
                            failureCount++;
                            _logger.LogWarning(
                                "AdHocEmailSendJob FAILED: EmailId={EmailId}, Subject={Subject}, " +
                                "Segment={Segment}, Email={Email}, Error={Error}",
                                sentAdHocEmailId, sentEmail.Subject, segmentLabel,
                                email, result.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        failureCount++;
                        _logger.LogError(ex,
                            "AdHocEmailSendJob EXCEPTION: EmailId={EmailId}, Subject={Subject}, " +
                            "Segment={Segment}, Email={Email}",
                            sentAdHocEmailId, sentEmail.Subject, segmentLabel, email);
                    }
                }
            }

            // Determine final delivery status
            var deliveryStatus = failureCount == 0
                ? "Sent"
                : successCount == 0
                    ? "Failed"
                    : "PartialFailure";

            // Update the audit record with results
            sentEmail.SuccessCount = successCount;
            sentEmail.FailureCount = failureCount;
            sentEmail.DeliveryStatus = deliveryStatus;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "AdHocEmailSendJob COMPLETE: EmailId={EmailId}, Subject={Subject}, Segment={Segment}, " +
                "Success={SuccessCount}, Failed={FailureCount}, Total={TotalCount}, Status={DeliveryStatus}",
                sentAdHocEmailId, sentEmail.Subject, segmentLabel,
                successCount, failureCount, sentEmail.RecipientCount, deliveryStatus);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "AdHocEmailSendJob CANCELLED (server shutdown): EmailId={EmailId}, Subject={Subject}",
                sentAdHocEmailId, sentEmail.Subject);

            // Mark as failed so admin knows it didn't complete
            sentEmail.DeliveryStatus = "Failed";
            await _context.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "AdHocEmailSendJob ERROR: EmailId={EmailId}, Subject={Subject}, Segment={Segment}",
                sentAdHocEmailId, sentEmail.Subject, segmentLabel);

            sentEmail.DeliveryStatus = "Failed";
            await _context.SaveChangesAsync(CancellationToken.None);

            throw; // Re-throw so Hangfire marks the job as failed
        }
    }

    /// <summary>
    /// Replace template variables with user-specific values.
    /// Generates unique tokens (password reset, email verification) per user.
    /// Mirrors the logic from EmailTemplateService.ReplaceVariablesForUserAsync.
    /// </summary>
    private async Task<(string html, string plainText)> ReplaceVariablesForUserAsync(
        string htmlBody,
        string plainTextBody,
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        var html = htmlBody;
        var plainText = plainTextBody;

        // Replace {{user_name}} - fall back to email if SceneName is null or empty
        var userName = !string.IsNullOrEmpty(user.SceneName) ? user.SceneName : (user.Email ?? "Member");
        html = html.Replace("{{user_name}}", userName);
        plainText = plainText.Replace("{{user_name}}", userName);

        // Replace {{reset_url}} if present - generates unique password reset token per user
        if (html.Contains("{{reset_url}}") || plainText.Contains("{{reset_url}}"))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var frontendUrl = _configuration["Frontend:Url"] ?? "https://staging.witchcityrope.com";
            var resetUrl = $"{frontendUrl}/reset-password?userId={user.Id}&token={System.Web.HttpUtility.UrlEncode(token)}";

            html = html.Replace("{{reset_url}}", resetUrl);
            plainText = plainText.Replace("{{reset_url}}", resetUrl);
        }

        // Replace {{verification_url}} if present - generates unique email confirmation token per user
        if (html.Contains("{{verification_url}}") || plainText.Contains("{{verification_url}}"))
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var frontendUrl = _configuration["Frontend:Url"] ?? "https://staging.witchcityrope.com";
            var verifyUrl = $"{frontendUrl}/verify-email?userId={user.Id}&token={System.Web.HttpUtility.UrlEncode(token)}";

            html = html.Replace("{{verification_url}}", verifyUrl);
            plainText = plainText.Replace("{{verification_url}}", verifyUrl);
        }

        return (html, plainText);
    }

    /// <summary>
    /// Resolve users for a given segment. Mirrors BuildSegmentQuery from EmailTemplateService.
    /// Duplicated here because the job runs in its own DI scope and needs direct DB access.
    /// </summary>
    private async Task<List<ApplicationUser>> GetUsersForSegmentAsync(
        UserSegment segment,
        CancellationToken cancellationToken)
    {
        var query = _context.Users.AsNoTracking();

        var filtered = segment switch
        {
            UserSegment.AllVettedMembers =>
                query.Where(u => u.VettingStatus == VettingStatus.Approved && u.IsActive),

            UserSegment.AllPreVettedMembers =>
                query.Where(u => u.VettingStatus != VettingStatus.Denied
                    && u.VettingStatus != VettingStatus.OnHold
                    && u.IsActive),

            UserSegment.AllTeachers =>
                query.Where(u => u.Role != null && u.Role.ToLower().Contains("teacher") && u.IsActive),

            UserSegment.AllDMs =>
                query.Where(u => u.Role != null && u.Role.ToLower().Contains("dungeonmonitor") && u.IsActive),

            UserSegment.AllSafetyTeam =>
                query.Where(u => u.Role != null && u.Role.ToLower().Contains("safetyteam") && u.IsActive),

            UserSegment.AllAdmins =>
                query.Where(u => u.Role != null && u.Role.ToLower().Contains("administrator") && u.IsActive),

            UserSegment.EmailNotVerified =>
                query.Where(u => !u.EmailConfirmed && u.IsActive),

            UserSegment.VettingPending =>
                query.Where(u => u.VettingStatus == VettingStatus.UnderReview && u.IsActive),

            UserSegment.NewImportedUsers =>
                query.Where(u => u.VettingStatus == VettingStatus.Approved
                    && !u.EmailConfirmed
                    && u.IsActive),

            _ => throw new ArgumentException($"Unknown segment: {segment}")
        };

        return await filtered.ToListAsync(cancellationToken);
    }
}

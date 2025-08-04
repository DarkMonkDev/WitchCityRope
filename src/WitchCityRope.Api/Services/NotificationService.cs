using Microsoft.Extensions.Logging;
using WitchCityRope.Core.Interfaces;
using WitchCityRope.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace WitchCityRope.Api.Services;

/// <summary>
/// Implementation of notification service that uses the email service
/// to send event-related notifications
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IEmailService _emailService;
    private readonly WitchCityRopeIdentityDbContext _dbContext;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IEmailService emailService,
        WitchCityRopeIdentityDbContext dbContext,
        ILogger<NotificationService> logger)
    {
        _emailService = emailService;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task SendEventRegistrationConfirmationAsync(Guid userId, Guid eventId)
    {
        try
        {
            // Get user and event details
            var user = await _dbContext.Users.FindAsync(userId);
            var @event = await _dbContext.Events
                .Include(e => e.Tickets)
                .FirstOrDefaultAsync(e => e.Id == eventId);
                
            if (user == null || @event == null)
            {
                _logger.LogWarning("User or event not found for ticket confirmation");
                return;
            }
            
            var ticket = @event.Tickets.FirstOrDefault(t => t.UserId == userId);
            if (ticket == null)
            {
                _logger.LogWarning("Registration not found for confirmation email");
                return;
            }

            var emailBody = $@"
<h2>Registration Confirmed!</h2>
<p>Hello {user.SceneName?.Value ?? "Member"},</p>
<p>Your ticket for <strong>{@event.Title}</strong> has been confirmed.</p>
<p><strong>Event Details:</strong></p>
<ul>
    <li>Date: {@event.StartDate:MMMM dd, yyyy}</li>
    <li>Time: {@event.StartDate:h:mm tt} - {@event.EndDate:h:mm tt}</li>
    <li>Location: {@event.Location}</li>
    <li>Confirmation Code: {ticket.Id.ToString().Substring(0, 8).ToUpper()}</li>
</ul>
<p>Please save this confirmation code for check-in.</p>
<p>If you need to cancel your ticket, please do so at least 48 hours before the event.</p>
<p>See you there!</p>
<p>- The WitchCity Rope Team</p>
";

            await _emailService.SendEmailAsync(
                to: Core.ValueObjects.EmailAddress.Create(user.Email ?? string.Empty),
                subject: $"Registration Confirmed - {@event.Title}",
                body: emailBody,
                isHtml: true);
                
            _logger.LogInformation("Sent ticket confirmation email to user {UserId} for event {EventId}", userId, eventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send ticket confirmation email");
        }
    }

    public async Task SendEventCancellationNotificationAsync(Guid eventId)
    {
        try
        {
            var @event = await _dbContext.Events
                .Include(e => e.Tickets)
                .FirstOrDefaultAsync(e => e.Id == eventId);
                
            if (@event == null)
            {
                _logger.LogWarning("Event not found for cancellation notification");
                return;
            }

            // Get all confirmed tickets
            var confirmedTickets = @event.Tickets
                .Where(t => t.Status == Core.Enums.TicketStatus.Confirmed)
                .ToList();

            // Get user IDs and load users separately
            var userIds = confirmedTickets.Select(t => t.UserId).Distinct().ToList();
            var users = await _dbContext.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);

            foreach (var ticket in confirmedTickets)
            {
                if (!users.TryGetValue(ticket.UserId, out var user) || user.Email == null) 
                    continue;

                var emailBody = $@"
<h2>Event Cancelled</h2>
<p>Hello {user.SceneName?.Value ?? "Member"},</p>
<p>We regret to inform you that <strong>{@event.Title}</strong> has been cancelled.</p>
<p>If you paid for this event, a full refund will be processed within 3-5 business days.</p>
<p>We apologize for any inconvenience this may cause.</p>
<p>- The WitchCity Rope Team</p>
";

                await _emailService.SendEmailAsync(
                    to: Core.ValueObjects.EmailAddress.Create(user.Email),
                    subject: $"Event Cancelled - {@event.Title}",
                    body: emailBody,
                    isHtml: true);
            }
            
            _logger.LogInformation("Sent cancellation notifications for event {EventId} to {Count} attendees", 
                eventId, confirmedTickets.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send event cancellation notifications");
        }
    }

    public async Task SendEventReminderAsync(Guid eventId)
    {
        try
        {
            var @event = await _dbContext.Events
                .Include(e => e.Tickets)
                .FirstOrDefaultAsync(e => e.Id == eventId);
                
            if (@event == null)
            {
                _logger.LogWarning("Event not found for reminder notification");
                return;
            }

            // Get all confirmed tickets
            var confirmedTickets = @event.Tickets
                .Where(t => t.Status == Core.Enums.TicketStatus.Confirmed)
                .ToList();

            // Get user IDs and load users separately
            var userIds = confirmedTickets.Select(t => t.UserId).Distinct().ToList();
            var users = await _dbContext.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);

            foreach (var ticket in confirmedTickets)
            {
                if (!users.TryGetValue(ticket.UserId, out var user) || user.Email == null) 
                    continue;

                var emailBody = $@"
<h2>Event Reminder</h2>
<p>Hello {user.SceneName?.Value ?? "Member"},</p>
<p>This is a reminder that you're registered for <strong>{@event.Title}</strong> tomorrow!</p>
<p><strong>Event Details:</strong></p>
<ul>
    <li>Date: {@event.StartDate:MMMM dd, yyyy}</li>
    <li>Time: {@event.StartDate:h:mm tt} - {@event.EndDate:h:mm tt}</li>
    <li>Location: {@event.Location}</li>
    <li>Confirmation Code: {ticket.Id.ToString().Substring(0, 8).ToUpper()}</li>
</ul>
<p>Please arrive 10-15 minutes early for check-in.</p>
<p>See you there!</p>
<p>- The WitchCity Rope Team</p>
";

                await _emailService.SendEmailAsync(
                    to: Core.ValueObjects.EmailAddress.Create(user.Email),
                    subject: $"Reminder: {@event.Title} is Tomorrow!",
                    body: emailBody,
                    isHtml: true);
            }
            
            _logger.LogInformation("Sent reminder notifications for event {EventId} to {Count} attendees", 
                eventId, confirmedTickets.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send event reminder notifications");
        }
    }

    public async Task SendVettingStatusUpdateAsync(Guid userId, bool approved)
    {
        try
        {
            var user = await _dbContext.Users.FindAsync(userId);
            if (user?.Email == null)
            {
                _logger.LogWarning("User not found for vetting status update");
                return;
            }

            var subject = approved 
                ? "Your Vetting Application Has Been Approved!" 
                : "Update on Your Vetting Application";
                
            var emailBody = approved
                ? $@"
<h2>Welcome to the Community!</h2>
<p>Hello {user.SceneName?.Value ?? "Member"},</p>
<p>We're excited to inform you that your vetting application has been approved!</p>
<p>You now have access to:</p>
<ul>
    <li>Social events (rope jams and labs)</li>
    <li>Member-only content and discussions</li>
    <li>Advanced workshops and skill shares</li>
</ul>
<p>Please review our community guidelines and consent policies before attending your first social event.</p>
<p>Welcome to the WitchCity Rope community!</p>
<p>- The WitchCity Rope Team</p>
"
                : $@"
<h2>Vetting Application Update</h2>
<p>Hello {user.SceneName?.Value ?? "Member"},</p>
<p>Thank you for your interest in joining our vetted community.</p>
<p>After careful review, we've decided not to approve your application at this time.</p>
<p>This decision may be based on various factors including:</p>
<ul>
    <li>Incomplete application information</li>
    <li>Concerns raised during the review process</li>
    <li>Timing or capacity constraints</li>
</ul>
<p>You're welcome to continue attending our public classes and workshops. You may reapply for vetting after 6 months.</p>
<p>If you have questions about this decision, please contact us.</p>
<p>- The WitchCity Rope Team</p>
";

            await _emailService.SendEmailAsync(
                to: Core.ValueObjects.EmailAddress.Create(user.Email),
                subject: subject,
                body: emailBody,
                isHtml: true);
                
            _logger.LogInformation("Sent vetting status update to user {UserId} (approved: {Approved})", userId, approved);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send vetting status update");
        }
    }

    public async Task SendPasswordResetAsync(string email, string resetToken)
    {
        try
        {
            var resetUrl = $"https://witchcityrope.com/auth/reset-password?token={resetToken}&email={email}";
            
            var emailBody = $@"
<h2>Password Reset Request</h2>
<p>Hello,</p>
<p>We received a request to reset your password. Click the link below to create a new password:</p>
<p><a href=""{resetUrl}"" style=""display: inline-block; padding: 10px 20px; background-color: #8B4513; color: white; text-decoration: none; border-radius: 5px;"">Reset Password</a></p>
<p>Or copy and paste this link into your browser:</p>
<p>{resetUrl}</p>
<p>This link will expire in 24 hours.</p>
<p>If you didn't request a password reset, you can safely ignore this email.</p>
<p>- The WitchCity Rope Team</p>
";

            await _emailService.SendEmailAsync(
                to: Core.ValueObjects.EmailAddress.Create(email),
                subject: "Password Reset Request - WitchCity Rope",
                body: emailBody,
                isHtml: true);
                
            _logger.LogInformation("Sent password reset email to {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email");
        }
    }

    public async Task SendEmailVerificationAsync(string email, string verificationToken)
    {
        try
        {
            var verifyUrl = $"https://witchcityrope.com/auth/verify-email?token={verificationToken}&email={email}";
            
            var emailBody = $@"
<h2>Verify Your Email Address</h2>
<p>Welcome to WitchCity Rope!</p>
<p>Please verify your email address by clicking the link below:</p>
<p><a href=""{verifyUrl}"" style=""display: inline-block; padding: 10px 20px; background-color: #8B4513; color: white; text-decoration: none; border-radius: 5px;"">Verify Email</a></p>
<p>Or copy and paste this link into your browser:</p>
<p>{verifyUrl}</p>
<p>This link will expire in 48 hours.</p>
<p>Once verified, you'll have full access to your account.</p>
<p>- The WitchCity Rope Team</p>
";

            await _emailService.SendEmailAsync(
                to: Core.ValueObjects.EmailAddress.Create(email),
                subject: "Verify Your Email - WitchCity Rope",
                body: emailBody,
                isHtml: true);
                
            _logger.LogInformation("Sent email verification to {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email verification");
        }
    }
}
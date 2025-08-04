using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace WitchCityRope.Infrastructure.Services
{
    /// <summary>
    /// Adapter class that implements ASP.NET Core Identity's IEmailSender interface
    /// using SendGrid for email delivery
    /// </summary>
    public class EmailSenderAdapter : IEmailSender
    {
        private readonly ISendGridClient _sendGridClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailSenderAdapter> _logger;

        public EmailSenderAdapter(
            ISendGridClient sendGridClient,
            IConfiguration configuration,
            ILogger<EmailSenderAdapter> logger)
        {
            _sendGridClient = sendGridClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                // Get sender email from configuration
                var fromEmail = _configuration["Email:SendGrid:FromEmail"] ?? "noreply@witchcityrope.com";
                var fromName = _configuration["Email:SendGrid:FromName"] ?? "Witch City Rope";

                // If SendGrid client is null (not configured), log and return
                if (_sendGridClient == null)
                {
                    _logger.LogWarning("SendGrid client is not configured. Email will not be sent to {Email}", email);
                    return;
                }

                var msg = new SendGridMessage
                {
                    From = new EmailAddress(fromEmail, fromName),
                    Subject = subject,
                    PlainTextContent = htmlMessage.Replace("<br>", "\n").Replace("<br/>", "\n").Replace("<br />", "\n"),
                    HtmlContent = htmlMessage
                };

                msg.AddTo(new EmailAddress(email));

                // Disable click tracking for privacy
                msg.SetClickTracking(false, false);

                var response = await _sendGridClient.SendEmailAsync(msg);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Email sent successfully to {Email}", email);
                }
                else
                {
                    var body = await response.Body.ReadAsStringAsync();
                    _logger.LogError("Failed to send email to {Email}. Status: {Status}, Body: {Body}", 
                        email, response.StatusCode, body);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {Email}", email);
                throw;
            }
        }
    }
}
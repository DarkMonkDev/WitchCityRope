using System.Threading.Tasks;
using WitchCityRope.Core.Interfaces;
using WitchCityRope.Core.ValueObjects;

namespace WitchCityRope.Api.Features.Auth.Services
{
    /// <summary>
    /// Adapter to make Core.Interfaces.IEmailService work with Auth.Services.IEmailService interface
    /// </summary>
    public class EmailServiceAdapter : IEmailService
    {
        private readonly Core.Interfaces.IEmailService _emailService;

        public EmailServiceAdapter(Core.Interfaces.IEmailService emailService)
        {
            _emailService = emailService;
        }

        public async Task<bool> SendVerificationEmailAsync(string email, string displayName, string token)
        {
            // The Core email service has a SendEmailAsync method, we need to adapt it
            var subject = "Verify your WitchCity Rope account";
            var body = $@"
                <h2>Welcome to WitchCity Rope, {displayName}!</h2>
                <p>Please verify your email address by clicking the link below:</p>
                <p><a href='https://witchcityrope.com/verify-email?token={token}'>Verify Email</a></p>
                <p>Or copy this link: https://witchcityrope.com/verify-email?token={token}</p>
                <p>This link will expire in 24 hours.</p>
            ";

            var emailAddress = EmailAddress.Create(email);
            await _emailService.SendEmailAsync(emailAddress, subject, body);
            return true;
        }

        public async Task<bool> SendAsync(EmailMessage message)
        {
            var emailAddress = EmailAddress.Create(message.To);
            await _emailService.SendEmailAsync(emailAddress, message.Subject, message.Body);
            return true;
        }
    }
}
using System.Collections.Concurrent;
using WitchCityRope.Tests.Common.Interfaces;

namespace WitchCityRope.Tests.Common.TestDoubles
{
    /// <summary>
    /// Test implementation of IEmailService that stores emails in memory for verification
    /// </summary>
    public class TestEmailService : IEmailService
    {
        private readonly ConcurrentBag<EmailMessage> _sentEmails = new();
        private readonly ConcurrentBag<TemplateEmail> _sentTemplateEmails = new();
        
        public bool ThrowOnSend { get; set; }
        public bool SimulateFailure { get; set; }

        public IReadOnlyCollection<EmailMessage> SentEmails => _sentEmails.ToList();
        public IReadOnlyCollection<TemplateEmail> SentTemplateEmails => _sentTemplateEmails.ToList();

        public Task<bool> SendAsync(EmailMessage message)
        {
            if (ThrowOnSend)
                throw new InvalidOperationException("Email service is configured to throw");

            if (SimulateFailure)
                return Task.FromResult(false);

            _sentEmails.Add(message);
            return Task.FromResult(true);
        }

        public Task<bool> SendTemplateAsync(string to, string templateId, object templateData)
        {
            if (ThrowOnSend)
                throw new InvalidOperationException("Email service is configured to throw");

            if (SimulateFailure)
                return Task.FromResult(false);

            _sentTemplateEmails.Add(new TemplateEmail
            {
                To = to,
                TemplateId = templateId,
                TemplateData = templateData
            });

            return Task.FromResult(true);
        }

        public Task<bool> SendWelcomeEmailAsync(string to, string name)
        {
            return SendTemplateAsync(to, "welcome", new { Name = name });
        }

        public Task<bool> SendPasswordResetEmailAsync(string to, string resetToken)
        {
            return SendTemplateAsync(to, "password-reset", new { ResetToken = resetToken });
        }

        public Task<bool> SendEmailVerificationAsync(string to, string verificationToken)
        {
            return SendTemplateAsync(to, "email-verification", new { VerificationToken = verificationToken });
        }

        public void Clear()
        {
            _sentEmails.Clear();
            _sentTemplateEmails.Clear();
        }

        public bool HasEmailTo(string email)
        {
            return _sentEmails.Any(e => e.To.Equals(email, StringComparison.OrdinalIgnoreCase)) ||
                   _sentTemplateEmails.Any(e => e.To.Equals(email, StringComparison.OrdinalIgnoreCase));
        }

        public int GetEmailCount(string? to = null)
        {
            if (string.IsNullOrEmpty(to))
                return _sentEmails.Count + _sentTemplateEmails.Count;

            return _sentEmails.Count(e => e.To.Equals(to, StringComparison.OrdinalIgnoreCase)) +
                   _sentTemplateEmails.Count(e => e.To.Equals(to, StringComparison.OrdinalIgnoreCase));
        }
    }

    public class TemplateEmail
    {
        public string To { get; set; } = string.Empty;
        public string TemplateId { get; set; } = string.Empty;
        public object? TemplateData { get; set; }
    }
}
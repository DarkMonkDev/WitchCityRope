using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WitchCityRope.Core.Interfaces;
using WitchCityRope.Core.ValueObjects;

namespace WitchCityRope.Infrastructure.Services
{
    /// <summary>
    /// Mock email service for development/testing when SendGrid is not configured
    /// </summary>
    public class MockEmailService : IEmailService
    {
        private readonly ILogger<MockEmailService> _logger;

        public MockEmailService(ILogger<MockEmailService> logger)
        {
            _logger = logger;
        }

        public Task<bool> SendEmailAsync(EmailAddress to, string subject, string body, bool isHtml = true)
        {
            _logger.LogInformation("Mock email sent:");
            _logger.LogInformation($"  To: {to.Value}");
            _logger.LogInformation($"  Subject: {subject}");
            _logger.LogInformation($"  IsHtml: {isHtml}");
            _logger.LogInformation($"  Body preview: {body.Substring(0, Math.Min(body.Length, 100))}...");
            
            return Task.FromResult(true);
        }

        public Task<bool> SendBulkEmailAsync(IEnumerable<EmailAddress> to, string subject, string body, bool isHtml = true)
        {
            var recipientList = to.ToList();
            _logger.LogInformation($"Mock bulk email sent to {recipientList.Count} recipients:");
            _logger.LogInformation($"  Recipients: {string.Join(", ", recipientList.Select(r => r.Value))}");
            _logger.LogInformation($"  Subject: {subject}");
            _logger.LogInformation($"  IsHtml: {isHtml}");
            
            return Task.FromResult(true);
        }

        public Task<bool> SendTemplateEmailAsync(EmailAddress to, string templateName, object templateData)
        {
            _logger.LogInformation($"Mock template email sent:");
            _logger.LogInformation($"  To: {to.Value}");
            _logger.LogInformation($"  Template: {templateName}");
            _logger.LogInformation($"  Data: {System.Text.Json.JsonSerializer.Serialize(templateData)}");
            
            return Task.FromResult(true);
        }

        public Task<bool> SendRegistrationConfirmationAsync(EmailAddress to, string sceneName, string eventTitle, DateTime eventDate)
        {
            _logger.LogInformation($"Mock registration confirmation sent:");
            _logger.LogInformation($"  To: {to.Value}");
            _logger.LogInformation($"  Scene Name: {sceneName}");
            _logger.LogInformation($"  Event: {eventTitle} on {eventDate:yyyy-MM-dd HH:mm}");
            
            return Task.FromResult(true);
        }

        public Task<bool> SendCancellationConfirmationAsync(EmailAddress to, string sceneName, string eventTitle, Money? refundAmount = null)
        {
            _logger.LogInformation($"Mock cancellation confirmation sent:");
            _logger.LogInformation($"  To: {to.Value}");
            _logger.LogInformation($"  Scene Name: {sceneName}");
            _logger.LogInformation($"  Event: {eventTitle}");
            if (refundAmount != null)
            {
                _logger.LogInformation($"  Refund: {refundAmount.Currency} {refundAmount.Amount}");
            }
            
            return Task.FromResult(true);
        }

        public Task<bool> SendVettingStatusUpdateAsync(EmailAddress to, string sceneName, string status, string? notes = null)
        {
            _logger.LogInformation($"Mock vetting status update sent:");
            _logger.LogInformation($"  To: {to.Value}");
            _logger.LogInformation($"  Scene Name: {sceneName}");
            _logger.LogInformation($"  Status: {status}");
            if (!string.IsNullOrEmpty(notes))
            {
                _logger.LogInformation($"  Notes: {notes}");
            }
            
            return Task.FromResult(true);
        }
    }
}
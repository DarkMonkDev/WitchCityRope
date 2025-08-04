using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WitchCityRope.Core.Entities;

namespace WitchCityRope.Core.Interfaces
{
    public interface IEventEmailService
    {
        // Template Management
        Task<List<EventEmailTemplate>> GetEventEmailTemplatesAsync(Guid eventId);
        Task<EventEmailTemplate> GetEmailTemplateByIdAsync(Guid templateId);
        Task<EventEmailTemplate> CreateEmailTemplateAsync(EventEmailTemplate template);
        Task<EventEmailTemplate> UpdateEmailTemplateAsync(EventEmailTemplate template);
        Task DeleteEmailTemplateAsync(Guid templateId);
        
        // Email Sending
        Task<bool> SendEventEmailAsync(Guid eventId, Guid templateId);
        Task<bool> SendCustomEventEmailAsync(Guid eventId, string subject, string body, List<string> recipientEmails = null);
        Task<bool> SendTestEmailAsync(Guid templateId, string testEmail);
        
        // Template Operations
        Task<string> PreviewEmailTemplateAsync(Guid templateId, Guid? sampleUserId = null);
        Task<EventEmailTemplate> CloneTemplateAsync(Guid templateId, Guid targetEventId);
        Task<List<EventEmailTemplate>> GetSystemTemplatesAsync();
        
        // Recipient Management
        Task<List<string>> GetEventEmailRecipientsAsync(Guid eventId, EmailTemplateType templateType);
        Task<int> GetRecipientCountAsync(Guid eventId, EmailTemplateType templateType);
        
        // Email History
        Task<List<EmailSendHistory>> GetEmailHistoryAsync(Guid eventId);
        Task<bool> ResendEmailAsync(Guid emailHistoryId);
        
        // Bulk Operations
        Task<bool> SendBulkEventEmailsAsync(List<Guid> eventIds, Guid templateId);
        Task<bool> ScheduleEmailAsync(Guid templateId, DateTime scheduledTime);
        Task<bool> CancelScheduledEmailAsync(Guid scheduledEmailId);
    }
    
    // Supporting types
    public class EmailSendHistory
    {
        public Guid Id { get; set; }
        public Guid EventId { get; set; }
        public Guid? TemplateId { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public List<string> Recipients { get; set; }
        public DateTime SentAt { get; set; }
        public bool IsSuccessful { get; set; }
        public string ErrorMessage { get; set; }
    }
}
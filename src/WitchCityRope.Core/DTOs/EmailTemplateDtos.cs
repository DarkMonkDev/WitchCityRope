using System;
using WitchCityRope.Core.Entities;

namespace WitchCityRope.Core.DTOs
{
    /// <summary>
    /// DTO for displaying email template information
    /// </summary>
    public class EventEmailTemplateDto
    {
        public Guid Id { get; set; }
        public Guid EventId { get; set; }
        public EmailTemplateType Type { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Request to create or update an email template
    /// </summary>
    public class SaveEmailTemplateRequest
    {
        public EmailTemplateType Type { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Request to send a custom email to event attendees
    /// </summary>
    public class SendEventEmailRequest
    {
        public EmailRecipientType Recipients { get; set; }
        public string? SpecificUserIds { get; set; } // Comma-separated list of user IDs
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool SaveAsTemplate { get; set; }
        public string? TemplateName { get; set; }
    }

    /// <summary>
    /// Types of email recipients
    /// </summary>
    public enum EmailRecipientType
    {
        AllRegistered,
        ConfirmedOnly,
        Waitlisted,
        Volunteers,
        Specific
    }

    /// <summary>
    /// Response after sending an email
    /// </summary>
    public class SendEventEmailResponse
    {
        public bool Success { get; set; }
        public int RecipientCount { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime SentAt { get; set; }
    }

    /// <summary>
    /// Available email template variables for display
    /// </summary>
    public class EmailTemplateVariablesDto
    {
        public static readonly string[] AvailableVariables = new[]
        {
            "{attendee_name}",
            "{event_name}",
            "{event_date}",
            "{event_time}",
            "{venue_name}",
            "{venue_address}"
        };

        public static readonly string[] VariableDescriptions = new[]
        {
            "The attendee's scene name",
            "The name of the event",
            "The date of the event (e.g., January 15, 2025)",
            "The start time of the event (e.g., 7:00 PM)",
            "The name of the venue",
            "The full address of the venue"
        };
    }
}
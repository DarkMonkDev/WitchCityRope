using System;

namespace WitchCityRope.Core.Entities
{
    /// <summary>
    /// Represents an email template for event communications
    /// </summary>
    public class EventEmailTemplate
    {
        // Private constructor for EF Core
        private EventEmailTemplate()
        {
            Subject = null!;
            Body = null!;
        }

        public EventEmailTemplate(
            Guid eventId,
            EmailTemplateType type,
            string subject,
            string body)
        {
            if (eventId == Guid.Empty)
                throw new ArgumentException("Event ID cannot be empty", nameof(eventId));
            
            if (string.IsNullOrWhiteSpace(subject))
                throw new ArgumentException("Email subject is required", nameof(subject));
            
            if (string.IsNullOrWhiteSpace(body))
                throw new ArgumentException("Email body is required", nameof(body));

            Id = Guid.NewGuid();
            EventId = eventId;
            Type = type;
            Subject = subject;
            Body = body;
            IsActive = true;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public Guid Id { get; private set; }
        
        public Guid EventId { get; private set; }
        
        public EmailTemplateType Type { get; private set; }
        
        public string Subject { get; private set; }
        
        public string Body { get; private set; }
        
        public bool IsActive { get; private set; }
        
        public DateTime CreatedAt { get; private set; }
        
        public DateTime UpdatedAt { get; private set; }
        
        // Navigation property
        public Event Event { get; private set; } = null!;

        /// <summary>
        /// Updates the email template content
        /// </summary>
        public void UpdateContent(string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(subject))
                throw new ArgumentException("Email subject is required", nameof(subject));
            
            if (string.IsNullOrWhiteSpace(body))
                throw new ArgumentException("Email body is required", nameof(body));

            Subject = subject;
            Body = body;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Activates the email template
        /// </summary>
        public void Activate()
        {
            if (IsActive)
                throw new DomainException("Email template is already active");
            
            IsActive = true;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Deactivates the email template
        /// </summary>
        public void Deactivate()
        {
            if (!IsActive)
                throw new DomainException("Email template is already inactive");
            
            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Replaces variables in the template with actual values
        /// </summary>
        public (string subject, string body) RenderTemplate(EventEmailVariables variables)
        {
            var renderedSubject = ReplaceVariables(Subject, variables);
            var renderedBody = ReplaceVariables(Body, variables);
            
            return (renderedSubject, renderedBody);
        }

        private string ReplaceVariables(string template, EventEmailVariables variables)
        {
            return template
                .Replace("{attendee_name}", variables.AttendeeName ?? string.Empty)
                .Replace("{event_name}", variables.EventName ?? string.Empty)
                .Replace("{event_date}", variables.EventDate?.ToString("MMMM d, yyyy") ?? string.Empty)
                .Replace("{event_time}", variables.EventTime?.ToString("h:mm tt") ?? string.Empty)
                .Replace("{venue_name}", variables.VenueName ?? string.Empty)
                .Replace("{venue_address}", variables.VenueAddress ?? string.Empty);
        }
    }

    /// <summary>
    /// Types of email templates available for events
    /// </summary>
    public enum EmailTemplateType
    {
        /// <summary>
        /// Sent when someone registers for the event
        /// </summary>
        RegistrationConfirmation,

        /// <summary>
        /// Sent before the event as a reminder
        /// </summary>
        EventReminder,

        /// <summary>
        /// Sent when a spot opens up for waitlisted attendees
        /// </summary>
        WaitlistNotification,

        /// <summary>
        /// Sent if the event is cancelled
        /// </summary>
        CancellationNotice,

        /// <summary>
        /// Custom email template
        /// </summary>
        Custom
    }

    /// <summary>
    /// Variables available for email template rendering
    /// </summary>
    public class EventEmailVariables
    {
        public string? AttendeeName { get; set; }
        public string? EventName { get; set; }
        public DateTime? EventDate { get; set; }
        public TimeOnly? EventTime { get; set; }
        public string? VenueName { get; set; }
        public string? VenueAddress { get; set; }
    }
}
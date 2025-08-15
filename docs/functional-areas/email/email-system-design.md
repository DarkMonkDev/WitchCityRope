# Comprehensive Email System Design

## Overview
This document outlines a flexible email system architecture that supports all of WitchCityRope's email needs: transactional emails, bulk communications, event-specific templates, and user preferences.

## Email Categories

### 1. Transactional Emails (Immediate)
- **Authentication**
  - Registration confirmation ✅ (implemented)
  - Email verification ✅ (implemented)
  - Password reset 🔄 (needs implementation)
  - 2FA notifications (login from new device, 2FA enabled/disabled)
  
- **Event Management**
  - Registration confirmation
  - RSVP confirmation
  - Cancellation notification
  - Event reminder (24 hours before)
  - Waitlist promotion
  - Check-in confirmation
  
- **Vetting**
  - Application received
  - Status update (approved/rejected) ✅ (method exists)
  - Interview scheduled
  
- **Safety & Incidents**
  - Incident report confirmation
  - Status update notifications

### 2. Bulk Communications (Scheduled)
- Monthly newsletter to vetted members
- Event announcements
- Community updates
- Safety notices

### 3. Event-Specific Templates
Each event can configure custom emails for:
- Registration confirmation
- Reminder (timing configurable)
- Post-event follow-up
- Cancellation notice
- Waitlist notifications

## Technical Architecture

### Email Template System

```csharp
public class EmailTemplate
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public EmailTemplateType Type { get; set; }
    public string Subject { get; set; }
    public string HtmlContent { get; set; }
    public string PlainTextContent { get; set; }
    public Dictionary<string, string> RequiredTokens { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public enum EmailTemplateType
{
    // System templates
    RegistrationConfirmation,
    EmailVerification,
    PasswordReset,
    TwoFactorNotification,
    
    // Event templates
    EventRegistration,
    EventReminder,
    EventCancellation,
    EventWaitlist,
    EventCheckIn,
    
    // Vetting templates
    VettingReceived,
    VettingApproved,
    VettingRejected,
    VettingInterview,
    
    // Bulk templates
    MonthlyNewsletter,
    CommunityAnnouncement,
    SafetyNotice
}
```

### Event Email Configuration

```csharp
public class EventEmailConfiguration
{
    public Guid EventId { get; set; }
    
    // Template overrides
    public Guid? RegistrationTemplateId { get; set; }
    public Guid? ReminderTemplateId { get; set; }
    public Guid? CancellationTemplateId { get; set; }
    
    // Timing settings
    public int? ReminderHoursBefore { get; set; } = 24;
    public bool SendPostEventSurvey { get; set; }
    public int? SurveyDaysAfter { get; set; } = 1;
    
    // Custom tokens for templates
    public Dictionary<string, string> CustomTokens { get; set; }
}
```

### User Email Preferences

```csharp
public class UserEmailPreferences
{
    public Guid UserId { get; set; }
    
    // Transactional (always sent)
    public bool SecurityNotifications { get; set; } = true;
    
    // Optional communications
    public bool EventReminders { get; set; } = true;
    public bool MonthlyNewsletter { get; set; } = true;
    public bool CommunityAnnouncements { get; set; } = true;
    public bool EventAnnouncements { get; set; } = true;
    
    // Delivery preferences
    public EmailFormat PreferredFormat { get; set; } = EmailFormat.Html;
    public string? AlternativeEmail { get; set; }
}

public enum EmailFormat
{
    Html,
    PlainText,
    Both
}
```

### Enhanced Email Service

```csharp
public interface IEmailTemplateService
{
    Task<EmailTemplate> GetTemplateAsync(EmailTemplateType type);
    Task<EmailTemplate> GetTemplateByIdAsync(Guid templateId);
    Task<string> RenderTemplateAsync(EmailTemplate template, object tokens);
    Task<EmailTemplate> CreateTemplateAsync(EmailTemplate template);
    Task UpdateTemplateAsync(EmailTemplate template);
}

public interface IEmailQueueService
{
    Task QueueEmailAsync(QueuedEmail email);
    Task ProcessQueueAsync(CancellationToken cancellationToken);
    Task<List<QueuedEmail>> GetPendingEmailsAsync(int batch = 100);
}

public class QueuedEmail
{
    public Guid Id { get; set; }
    public string To { get; set; }
    public string Subject { get; set; }
    public string HtmlContent { get; set; }
    public string PlainTextContent { get; set; }
    public EmailPriority Priority { get; set; }
    public DateTime ScheduledFor { get; set; }
    public int RetryCount { get; set; }
    public EmailStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
}
```

## Implementation Plan

### Phase 1: Core Infrastructure
1. Create email template entities and migrations
2. Implement EmailTemplateService
3. Add email queue for reliable delivery
4. Create background service for queue processing

### Phase 2: Event Integration
1. Add email configuration UI to event edit page
2. Implement event-specific email triggers
3. Create default email templates
4. Add template preview functionality

### Phase 3: User Preferences
1. Add email preferences to user profile
2. Implement preference checking in email service
3. Create unsubscribe links
4. Add preference management UI

### Phase 4: Bulk Email System
1. Create newsletter composition UI
2. Implement recipient filtering (vetted members only)
3. Add scheduling functionality
4. Create analytics tracking

## Email Triggers

### Event Registration Flow
1. User registers for event
2. Payment processed (or stubbed)
3. Registration confirmed in database
4. Email queued with high priority
5. Confirmation email sent within 1 minute
6. Reminder scheduled for 24 hours before event

### Monthly Newsletter Flow
1. Scheduled job runs on 1st of month
2. Query all vetted members with newsletter preference
3. Render newsletter template with current month's events
4. Queue emails in batches of 100
5. Process over 1 hour to avoid rate limits

### 2FA Email Flow (New)
1. User enables/disables 2FA
2. Security notification sent immediately
3. New device login detected
4. Verification code sent (optional, in addition to authenticator)

## Benefits of This Design

1. **Flexibility**: Events can have custom email templates
2. **Reliability**: Queue system ensures delivery even if service is down
3. **Scalability**: Batch processing for bulk emails
4. **User Control**: Granular preferences for different email types
5. **Compliance**: Easy unsubscribe and preference management
6. **Maintainability**: Centralized template management
7. **Performance**: Background processing doesn't block user actions

## Security Considerations

1. **Rate Limiting**: Prevent email bombing
2. **Template Injection**: Sanitize all user-provided content
3. **Unsubscribe Tokens**: Secure, expiring tokens
4. **Audit Trail**: Log all email sends
5. **PII Protection**: Don't log email content with PII

## Migration Strategy

1. Keep existing IEmailService interface
2. Add new services alongside
3. Gradually migrate existing email calls
4. Backfill templates from SendGrid
5. Import user preferences (default to opted-in)
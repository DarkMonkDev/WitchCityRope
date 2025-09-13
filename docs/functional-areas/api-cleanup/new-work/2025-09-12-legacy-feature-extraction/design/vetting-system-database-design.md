# Vetting System Database Design
<!-- Created: 2025-09-13 -->
<!-- Version: 1.0 -->
<!-- Owner: Database Designer Agent -->
<!-- Status: Complete Database Schema -->

## Executive Summary

This document provides the complete database design for the WitchCityRope Vetting System, a privacy-focused member approval workflow. The design emphasizes data security, audit trails, and workflow state management while following established PostgreSQL and Entity Framework Core patterns.

## Design Principles

### Data Integrity & Security
- **Encrypted PII**: All personally identifiable information stored encrypted
- **Audit Trails**: Complete change tracking for all sensitive operations
- **Role-Based Access**: Database constraints support UI access controls
- **Privacy First**: Anonymous applications completely isolated from identified submissions

### Performance & Scalability
- **Strategic Indexing**: Optimized for reviewer dashboard queries and status lookups
- **Efficient Relationships**: Proper foreign keys with appropriate cascade behaviors
- **Query Optimization**: Composite indexes for common filter combinations
- **Soft Deletes**: Data retention while supporting cleanup workflows

### WitchCityRope Patterns
- **UTC DateTime Handling**: All timestamps use PostgreSQL's timestamptz
- **GUID Primary Keys**: Consistent with existing entity patterns
- **Standard Audit Fields**: CreatedAt, UpdatedAt, CreatedBy, UpdatedBy
- **Entity Framework Configuration**: Separate configuration classes for maintainability

## Entity Relationship Diagram

```
VettingApplication
├── VettingReviewer (assignedReviewerId FK)
├── ApplicationUser (applicantId FK, createdBy FK, updatedBy FK)
├── VettingApplicationAuditLog (1:N)
├── VettingApplicationNote (1:N)
├── VettingDecision (1:N)
├── VettingReference (1:N)
└── VettingNotification (1:N)

VettingReference
├── VettingApplication (applicationId FK)
├── VettingReferenceResponse (1:1)
└── VettingReferenceAuditLog (1:N)

VettingReviewer
├── ApplicationUser (userId FK)
├── VettingApplication (N:1 via assignedReviewerId)
├── VettingDecision (1:N)
└── VettingApplicationNote (1:N)

VettingApplicationNote
├── VettingApplication (applicationId FK)
├── VettingReviewer (reviewerId FK)
└── VettingNoteAttachment (1:N)

VettingDecision
├── VettingApplication (applicationId FK)
├── VettingReviewer (reviewerId FK)
└── VettingDecisionAuditLog (1:N)
```

## Entity Definitions

### 1. VettingApplication

Primary entity for member vetting applications with comprehensive audit trail support.

```csharp
public class VettingApplication
{
    public VettingApplication()
    {
        Id = Guid.NewGuid();
        Status = ApplicationStatus.Draft;
        IsAnonymous = false;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        StatusToken = GenerateSecureToken();
        
        References = new List<VettingReference>();
        Notes = new List<VettingApplicationNote>();
        Decisions = new List<VettingDecision>();
        AuditLogs = new List<VettingApplicationAuditLog>();
        Notifications = new List<VettingNotification>();
    }

    // Primary Key
    public Guid Id { get; set; }

    // Application Identification
    public string ApplicationNumber { get; set; } = string.Empty; // VET-YYYYMMDD-NNNN
    public ApplicationStatus Status { get; set; }
    public string StatusToken { get; set; } = string.Empty; // Secure token for public status checks

    // Applicant Information (Encrypted)
    public string EncryptedFullName { get; set; } = string.Empty;
    public string EncryptedSceneName { get; set; } = string.Empty;
    public string? EncryptedPronouns { get; set; }
    public string EncryptedEmail { get; set; } = string.Empty;
    public string? EncryptedPhone { get; set; }

    // Experience Information (Encrypted)
    public ExperienceLevel ExperienceLevel { get; set; }
    public int YearsExperience { get; set; }
    public string EncryptedExperienceDescription { get; set; } = string.Empty;
    public string EncryptedSafetyKnowledge { get; set; } = string.Empty;
    public string EncryptedConsentUnderstanding { get; set; } = string.Empty;

    // Community Information
    public string EncryptedWhyJoinCommunity { get; set; } = string.Empty;
    public string SkillsInterests { get; set; } = string.Empty; // JSON array of tags
    public string EncryptedExpectationsGoals { get; set; } = string.Empty;
    public bool AgreesToGuidelines { get; set; }

    // Privacy Settings
    public bool IsAnonymous { get; set; }
    public bool AgreesToTerms { get; set; }
    public bool ConsentToContact { get; set; }

    // Workflow Management
    public Guid? AssignedReviewerId { get; set; }
    public ApplicationPriority Priority { get; set; } = ApplicationPriority.Standard;
    public DateTime? ReviewStartedAt { get; set; }
    public DateTime? DecisionMadeAt { get; set; }
    public DateTime? InterviewScheduledFor { get; set; }

    // Data Retention
    public DateTime? DeletedAt { get; set; } // Soft delete
    public DateTime? ExpiresAt { get; set; } // For draft cleanup

    // Standard Audit Fields
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    // Foreign Keys
    public Guid? ApplicantId { get; set; } // Links to ApplicationUser if registered

    // Navigation Properties
    public ApplicationUser? Applicant { get; set; }
    public VettingReviewer? AssignedReviewer { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }
    public ApplicationUser? UpdatedByUser { get; set; }
    
    public ICollection<VettingReference> References { get; set; }
    public ICollection<VettingApplicationNote> Notes { get; set; }
    public ICollection<VettingDecision> Decisions { get; set; }
    public ICollection<VettingApplicationAuditLog> AuditLogs { get; set; }
    public ICollection<VettingNotification> Notifications { get; set; }

    // Helper Methods
    private static string GenerateSecureToken() => 
        Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
}

public enum ApplicationStatus
{
    Draft = 1,
    Submitted = 2,
    UnderReview = 3,
    PendingReferences = 4,
    PendingInterview = 5,
    PendingAdditionalInfo = 6,
    Approved = 7,
    Denied = 8,
    Withdrawn = 9,
    Expired = 10
}

public enum ExperienceLevel
{
    Beginner = 1,
    Intermediate = 2,
    Advanced = 3,
    Expert = 4
}

public enum ApplicationPriority
{
    Standard = 1,
    High = 2,
    Urgent = 3
}
```

### 2. VettingReference

Manages reference collection and verification process.

```csharp
public class VettingReference
{
    public VettingReference()
    {
        Id = Guid.NewGuid();
        Status = ReferenceStatus.NotContacted;
        ResponseToken = GenerateSecureToken();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        
        AuditLogs = new List<VettingReferenceAuditLog>();
    }

    // Primary Key
    public Guid Id { get; set; }

    // Application Reference
    public Guid ApplicationId { get; set; }
    public int ReferenceOrder { get; set; } // 1, 2, 3 for display order

    // Reference Information (Encrypted)
    public string EncryptedName { get; set; } = string.Empty;
    public string EncryptedEmail { get; set; } = string.Empty;
    public string EncryptedRelationship { get; set; } = string.Empty;

    // Reference Process
    public ReferenceStatus Status { get; set; }
    public string ResponseToken { get; set; } = string.Empty; // Secure token for reference form
    public DateTime? ContactedAt { get; set; }
    public DateTime? FirstReminderSentAt { get; set; }
    public DateTime? SecondReminderSentAt { get; set; }
    public DateTime? FinalReminderSentAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public DateTime? FormExpiresAt { get; set; }

    // Manual Follow-up Tracking
    public bool RequiresManualContact { get; set; }
    public string? ManualContactNotes { get; set; }
    public DateTime? ManualContactAttemptAt { get; set; }

    // Standard Audit Fields
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation Properties
    public VettingApplication Application { get; set; } = null!;
    public VettingReferenceResponse? Response { get; set; }
    public ICollection<VettingReferenceAuditLog> AuditLogs { get; set; }

    private static string GenerateSecureToken() => 
        Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
}

public enum ReferenceStatus
{
    NotContacted = 1,
    Contacted = 2,
    ReminderSent = 3,
    Responded = 4,
    Expired = 5,
    ManualFollowupRequired = 6
}
```

### 3. VettingReferenceResponse

Stores reference form responses with assessment data.

```csharp
public class VettingReferenceResponse
{
    public VettingReferenceResponse()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    // Primary Key
    public Guid Id { get; set; }

    // Reference Link
    public Guid ReferenceId { get; set; }

    // Assessment Questions (Encrypted)
    public string EncryptedRelationshipDuration { get; set; } = string.Empty;
    public string EncryptedExperienceAssessment { get; set; } = string.Empty;
    public string? EncryptedSafetyConcerns { get; set; }
    public string EncryptedCommunityReadiness { get; set; } = string.Empty;
    public RecommendationLevel Recommendation { get; set; }
    public string? EncryptedAdditionalComments { get; set; }

    // Response Metadata
    public string ResponseIpAddress { get; set; } = string.Empty;
    public string? ResponseUserAgent { get; set; }
    public bool IsCompleted { get; set; }

    // Standard Audit Fields
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation Properties
    public VettingReference Reference { get; set; } = null!;
}

public enum RecommendationLevel
{
    DoNotSupport = 1,
    Neutral = 2,
    Support = 3,
    StronglySupport = 4
}
```

### 4. VettingReviewer

Manages vetting team members and workload distribution.

```csharp
public class VettingReviewer
{
    public VettingReviewer()
    {
        Id = Guid.NewGuid();
        IsActive = true;
        MaxWorkload = 10; // Default maximum concurrent reviews
        CurrentWorkload = 0;
        TotalReviewsCompleted = 0;
        AverageReviewTimeHours = 0;
        ApprovalRate = 0;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        
        Specializations = new List<string>();
        AssignedApplications = new List<VettingApplication>();
        Decisions = new List<VettingDecision>();
        Notes = new List<VettingApplicationNote>();
    }

    // Primary Key
    public Guid Id { get; set; }

    // User Link
    public Guid UserId { get; set; }

    // Reviewer Settings
    public bool IsActive { get; set; }
    public int MaxWorkload { get; set; }
    public int CurrentWorkload { get; set; }

    // Specializations (JSON array)
    public string SpecializationsJson { get; set; } = "[]";
    [NotMapped]
    public List<string> Specializations 
    { 
        get => JsonSerializer.Deserialize<List<string>>(SpecializationsJson) ?? new List<string>();
        set => SpecializationsJson = JsonSerializer.Serialize(value);
    }

    // Performance Metrics
    public int TotalReviewsCompleted { get; set; }
    public decimal AverageReviewTimeHours { get; set; }
    public decimal ApprovalRate { get; set; } // Percentage
    public DateTime? LastActivityAt { get; set; }

    // Availability Settings
    public bool IsAvailable { get; set; } = true;
    public DateTime? UnavailableUntil { get; set; }
    public string TimeZone { get; set; } = "UTC";

    // Working Hours (JSON object)
    public string? WorkingHoursJson { get; set; }

    // Standard Audit Fields
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation Properties
    public ApplicationUser User { get; set; } = null!;
    public ICollection<VettingApplication> AssignedApplications { get; set; }
    public ICollection<VettingDecision> Decisions { get; set; }
    public ICollection<VettingApplicationNote> Notes { get; set; }
}
```

### 5. VettingApplicationNote

Internal notes and comments for reviewer collaboration.

```csharp
public class VettingApplicationNote
{
    public VettingApplicationNote()
    {
        Id = Guid.NewGuid();
        IsPrivate = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        
        Tags = new List<string>();
        Attachments = new List<VettingNoteAttachment>();
    }

    // Primary Key
    public Guid Id { get; set; }

    // Application Reference
    public Guid ApplicationId { get; set; }
    public Guid ReviewerId { get; set; }

    // Note Content
    public string Content { get; set; } = string.Empty;
    public NoteType Type { get; set; } = NoteType.General;
    public bool IsPrivate { get; set; } // True = internal only, False = visible to applicant

    // Categorization
    public string TagsJson { get; set; } = "[]";
    [NotMapped]
    public List<string> Tags 
    { 
        get => JsonSerializer.Deserialize<List<string>>(TagsJson) ?? new List<string>();
        set => TagsJson = JsonSerializer.Serialize(value);
    }

    // Standard Audit Fields
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation Properties
    public VettingApplication Application { get; set; } = null!;
    public VettingReviewer Reviewer { get; set; } = null!;
    public ICollection<VettingNoteAttachment> Attachments { get; set; }
}

public enum NoteType
{
    General = 1,
    Concern = 2,
    Clarification = 3,
    ReferenceNote = 4,
    InterviewNote = 5,
    DecisionRationale = 6
}
```

### 6. VettingDecision

Tracks all review decisions and their rationale.

```csharp
public class VettingDecision
{
    public VettingDecision()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        
        AuditLogs = new List<VettingDecisionAuditLog>();
    }

    // Primary Key
    public Guid Id { get; set; }

    // Application Reference
    public Guid ApplicationId { get; set; }
    public Guid ReviewerId { get; set; }

    // Decision Details
    public DecisionType DecisionType { get; set; }
    public string Reasoning { get; set; } = string.Empty;
    public int? Score { get; set; } // 1-10 scale if using scoring rubric
    public bool IsFinalDecision { get; set; }

    // Additional Info Request Details
    public string? AdditionalInfoRequested { get; set; }
    public DateTime? AdditionalInfoDeadline { get; set; }

    // Interview Details
    public DateTime? ProposedInterviewTime { get; set; }
    public string? InterviewNotes { get; set; }

    // Decision Metadata
    public DateTime CreatedAt { get; set; }
    public string? DecisionIpAddress { get; set; }
    public string? DecisionUserAgent { get; set; }

    // Navigation Properties
    public VettingApplication Application { get; set; } = null!;
    public VettingReviewer Reviewer { get; set; } = null!;
    public ICollection<VettingDecisionAuditLog> AuditLogs { get; set; }
}

public enum DecisionType
{
    Approve = 1,
    Deny = 2,
    RequestAdditionalInfo = 3,
    ScheduleInterview = 4,
    Reassign = 5,
    Escalate = 6
}
```

## Audit Trail Entities

### 7. VettingApplicationAuditLog

Comprehensive audit trail for all application changes.

```csharp
public class VettingApplicationAuditLog
{
    public VettingApplicationAuditLog()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    // Primary Key
    public Guid Id { get; set; }

    // Application Reference
    public Guid ApplicationId { get; set; }

    // Audit Details
    public string ActionType { get; set; } = string.Empty; // "StatusChange", "AssignmentChange", etc.
    public string ActionDescription { get; set; } = string.Empty;
    public string? OldValues { get; set; } // JSON
    public string? NewValues { get; set; } // JSON

    // User Context
    public Guid? UserId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    // Timestamp
    public DateTime CreatedAt { get; set; }

    // Navigation Properties
    public VettingApplication Application { get; set; } = null!;
    public ApplicationUser? User { get; set; }
}
```

### 8. VettingReferenceAuditLog

Audit trail for reference management actions.

```csharp
public class VettingReferenceAuditLog
{
    public VettingReferenceAuditLog()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    // Primary Key
    public Guid Id { get; set; }

    // Reference Context
    public Guid ReferenceId { get; set; }
    public Guid ApplicationId { get; set; }

    // Audit Details
    public string ActionType { get; set; } = string.Empty; // "EmailSent", "ReminderSent", "ResponseReceived"
    public string ActionDescription { get; set; } = string.Empty;
    public string? EmailMessageId { get; set; } // For tracking email delivery
    public string? DeliveryStatus { get; set; }

    // Timestamp
    public DateTime CreatedAt { get; set; }

    // Navigation Properties
    public VettingReference Reference { get; set; } = null!;
}
```

### 9. VettingDecisionAuditLog

Audit trail for decision changes and approvals.

```csharp
public class VettingDecisionAuditLog
{
    public VettingDecisionAuditLog()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    // Primary Key
    public Guid Id { get; set; }

    // Decision Reference
    public Guid DecisionId { get; set; }
    public Guid ApplicationId { get; set; }

    // Audit Details
    public string ActionType { get; set; } = string.Empty;
    public string ActionDescription { get; set; } = string.Empty;
    public string? PreviousDecision { get; set; }
    public string? NewDecision { get; set; }

    // User Context
    public Guid? UserId { get; set; }

    // Timestamp
    public DateTime CreatedAt { get; set; }

    // Navigation Properties
    public VettingDecision Decision { get; set; } = null!;
    public ApplicationUser? User { get; set; }
}
```

## Supporting Entities

### 10. VettingNotification

Email notification tracking and delivery management.

```csharp
public class VettingNotification
{
    public VettingNotification()
    {
        Id = Guid.NewGuid();
        Status = NotificationStatus.Pending;
        RetryCount = 0;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    // Primary Key
    public Guid Id { get; set; }

    // Application Reference
    public Guid ApplicationId { get; set; }

    // Notification Details
    public NotificationType NotificationType { get; set; }
    public string RecipientEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string MessageBody { get; set; } = string.Empty;
    public NotificationStatus Status { get; set; }

    // Delivery Tracking
    public DateTime? SentAt { get; set; }
    public int RetryCount { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? MessageId { get; set; } // Email provider message ID
    public string? DeliveryStatus { get; set; } // Delivered, Bounced, etc.

    // Template Information
    public string TemplateName { get; set; } = string.Empty;
    public string? TemplateData { get; set; } // JSON data for template merge

    // Standard Audit Fields
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation Properties
    public VettingApplication Application { get; set; } = null!;
}

public enum NotificationType
{
    ApplicationConfirmation = 1,
    ReferenceRequest = 2,
    ReferenceReminder = 3,
    StatusUpdate = 4,
    AdditionalInfoRequest = 5,
    InterviewScheduled = 6,
    DecisionNotification = 7,
    WelcomePackage = 8
}

public enum NotificationStatus
{
    Pending = 1,
    Sent = 2,
    Delivered = 3,
    Failed = 4,
    Bounced = 5,
    Unsubscribed = 6
}
```

### 11. VettingNoteAttachment

File attachments for reviewer notes.

```csharp
public class VettingNoteAttachment
{
    public VettingNoteAttachment()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    // Primary Key
    public Guid Id { get; set; }

    // Note Reference
    public Guid NoteId { get; set; }

    // File Information
    public string FileName { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string MimeType { get; set; } = string.Empty;

    // Storage Information
    public string StoragePath { get; set; } = string.Empty; // Path to encrypted file
    public string FileHash { get; set; } = string.Empty; // SHA-256 hash for integrity

    // Access Control
    public bool IsConfidential { get; set; } = true;

    // Timestamp
    public DateTime CreatedAt { get; set; }

    // Navigation Properties
    public VettingApplicationNote Note { get; set; } = null!;
}
```

## Entity Framework Configuration

### VettingApplicationConfiguration

```csharp
public class VettingApplicationConfiguration : IEntityTypeConfiguration<VettingApplication>
{
    public void Configure(EntityTypeBuilder<VettingApplication> builder)
    {
        // Table mapping
        builder.ToTable("VettingApplications", "public");
        builder.HasKey(e => e.Id);

        // Properties
        builder.Property(e => e.ApplicationNumber)
               .IsRequired()
               .HasMaxLength(20);

        builder.Property(e => e.StatusToken)
               .IsRequired()
               .HasMaxLength(100);

        // Encrypted fields
        builder.Property(e => e.EncryptedFullName)
               .IsRequired()
               .HasMaxLength(500);

        builder.Property(e => e.EncryptedSceneName)
               .IsRequired()
               .HasMaxLength(500);

        builder.Property(e => e.EncryptedEmail)
               .IsRequired()
               .HasMaxLength(500);

        builder.Property(e => e.EncryptedExperienceDescription)
               .IsRequired()
               .HasColumnType("text");

        builder.Property(e => e.EncryptedSafetyKnowledge)
               .IsRequired()
               .HasColumnType("text");

        builder.Property(e => e.EncryptedConsentUnderstanding)
               .IsRequired()
               .HasColumnType("text");

        builder.Property(e => e.EncryptedWhyJoinCommunity)
               .IsRequired()
               .HasColumnType("text");

        builder.Property(e => e.EncryptedExpectationsGoals)
               .IsRequired()
               .HasColumnType("text");

        // JSON field for skills/interests
        builder.Property(e => e.SkillsInterests)
               .IsRequired()
               .HasColumnType("jsonb")
               .HasDefaultValue("[]");

        // Enum configurations
        builder.Property(e => e.Status)
               .IsRequired()
               .HasConversion<int>();

        builder.Property(e => e.ExperienceLevel)
               .IsRequired()
               .HasConversion<int>();

        builder.Property(e => e.Priority)
               .IsRequired()
               .HasConversion<int>();

        // DateTime properties - CRITICAL: Use timestamptz for PostgreSQL
        builder.Property(e => e.CreatedAt)
               .IsRequired()
               .HasColumnType("timestamptz");

        builder.Property(e => e.UpdatedAt)
               .IsRequired()
               .HasColumnType("timestamptz");

        builder.Property(e => e.ReviewStartedAt)
               .HasColumnType("timestamptz");

        builder.Property(e => e.DecisionMadeAt)
               .HasColumnType("timestamptz");

        builder.Property(e => e.InterviewScheduledFor)
               .HasColumnType("timestamptz");

        builder.Property(e => e.DeletedAt)
               .HasColumnType("timestamptz");

        builder.Property(e => e.ExpiresAt)
               .HasColumnType("timestamptz");

        // Indexes for performance
        builder.HasIndex(e => e.ApplicationNumber)
               .IsUnique()
               .HasDatabaseName("IX_VettingApplications_ApplicationNumber");

        builder.HasIndex(e => e.StatusToken)
               .IsUnique()
               .HasDatabaseName("IX_VettingApplications_StatusToken");

        builder.HasIndex(e => e.Status)
               .HasDatabaseName("IX_VettingApplications_Status");

        builder.HasIndex(e => e.AssignedReviewerId)
               .HasDatabaseName("IX_VettingApplications_AssignedReviewerId")
               .HasFilter("\"AssignedReviewerId\" IS NOT NULL");

        builder.HasIndex(e => new { e.Status, e.Priority, e.CreatedAt })
               .HasDatabaseName("IX_VettingApplications_Status_Priority_CreatedAt");

        builder.HasIndex(e => e.DeletedAt)
               .HasDatabaseName("IX_VettingApplications_DeletedAt")
               .HasFilter("\"DeletedAt\" IS NOT NULL");

        // Partial index for active applications
        builder.HasIndex(e => new { e.Status, e.CreatedAt })
               .HasDatabaseName("IX_VettingApplications_Active_Status_CreatedAt")
               .HasFilter("\"DeletedAt\" IS NULL");

        // Relationships
        builder.HasOne(e => e.Applicant)
               .WithMany()
               .HasForeignKey(e => e.ApplicantId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.AssignedReviewer)
               .WithMany(r => r.AssignedApplications)
               .HasForeignKey(e => e.AssignedReviewerId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.CreatedByUser)
               .WithMany()
               .HasForeignKey(e => e.CreatedBy)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.UpdatedByUser)
               .WithMany()
               .HasForeignKey(e => e.UpdatedBy)
               .OnDelete(DeleteBehavior.SetNull);

        // One-to-many relationships
        builder.HasMany(e => e.References)
               .WithOne(r => r.Application)
               .HasForeignKey(r => r.ApplicationId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Notes)
               .WithOne(n => n.Application)
               .HasForeignKey(n => n.ApplicationId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Decisions)
               .WithOne(d => d.Application)
               .HasForeignKey(d => d.ApplicationId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.AuditLogs)
               .WithOne(a => a.Application)
               .HasForeignKey(a => a.ApplicationId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Notifications)
               .WithOne(n => n.Application)
               .HasForeignKey(n => n.ApplicationId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
```

### VettingReferenceConfiguration

```csharp
public class VettingReferenceConfiguration : IEntityTypeConfiguration<VettingReference>
{
    public void Configure(EntityTypeBuilder<VettingReference> builder)
    {
        // Table mapping
        builder.ToTable("VettingReferences", "public");
        builder.HasKey(e => e.Id);

        // Properties
        builder.Property(e => e.EncryptedName)
               .IsRequired()
               .HasMaxLength(500);

        builder.Property(e => e.EncryptedEmail)
               .IsRequired()
               .HasMaxLength(500);

        builder.Property(e => e.EncryptedRelationship)
               .IsRequired()
               .HasMaxLength(500);

        builder.Property(e => e.ResponseToken)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(e => e.Status)
               .IsRequired()
               .HasConversion<int>();

        builder.Property(e => e.ManualContactNotes)
               .HasMaxLength(1000);

        // DateTime properties
        builder.Property(e => e.CreatedAt)
               .IsRequired()
               .HasColumnType("timestamptz");

        builder.Property(e => e.UpdatedAt)
               .IsRequired()
               .HasColumnType("timestamptz");

        builder.Property(e => e.ContactedAt)
               .HasColumnType("timestamptz");

        builder.Property(e => e.FirstReminderSentAt)
               .HasColumnType("timestamptz");

        builder.Property(e => e.SecondReminderSentAt)
               .HasColumnType("timestamptz");

        builder.Property(e => e.FinalReminderSentAt)
               .HasColumnType("timestamptz");

        builder.Property(e => e.RespondedAt)
               .HasColumnType("timestamptz");

        builder.Property(e => e.FormExpiresAt)
               .HasColumnType("timestamptz");

        builder.Property(e => e.ManualContactAttemptAt)
               .HasColumnType("timestamptz");

        // Indexes
        builder.HasIndex(e => e.ApplicationId)
               .HasDatabaseName("IX_VettingReferences_ApplicationId");

        builder.HasIndex(e => e.ResponseToken)
               .IsUnique()
               .HasDatabaseName("IX_VettingReferences_ResponseToken");

        builder.HasIndex(e => e.Status)
               .HasDatabaseName("IX_VettingReferences_Status");

        builder.HasIndex(e => new { e.ApplicationId, e.ReferenceOrder })
               .IsUnique()
               .HasDatabaseName("IX_VettingReferences_ApplicationId_ReferenceOrder");

        // Relationships
        builder.HasOne(e => e.Application)
               .WithMany(a => a.References)
               .HasForeignKey(e => e.ApplicationId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Response)
               .WithOne(r => r.Reference)
               .HasForeignKey<VettingReferenceResponse>(r => r.ReferenceId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.AuditLogs)
               .WithOne(a => a.Reference)
               .HasForeignKey(a => a.ReferenceId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
```

## Strategic Indexes

### Performance Optimization Indexes

```sql
-- Primary application search and filtering
CREATE INDEX "IX_VettingApplications_ReviewerDashboard" 
ON "VettingApplications" ("Status", "Priority", "AssignedReviewerId", "CreatedAt" DESC)
WHERE "DeletedAt" IS NULL;

-- Public status tracking
CREATE INDEX "IX_VettingApplications_StatusToken_Lookup" 
ON "VettingApplications" ("StatusToken")
WHERE "DeletedAt" IS NULL;

-- Reference management queries
CREATE INDEX "IX_VettingReferences_StatusProcessing" 
ON "VettingReferences" ("Status", "ContactedAt", "FormExpiresAt")
WHERE "Status" IN (1, 2, 3); -- NotContacted, Contacted, ReminderSent

-- Notification delivery queues
CREATE INDEX "IX_VettingNotifications_DeliveryQueue" 
ON "VettingNotifications" ("Status", "NextRetryAt", "CreatedAt")
WHERE "Status" IN (1, 4); -- Pending, Failed

-- Audit trail queries
CREATE INDEX "IX_VettingApplicationAuditLog_Timeline" 
ON "VettingApplicationAuditLog" ("ApplicationId", "CreatedAt" DESC);

-- JSONB search for skills/interests
CREATE INDEX "IX_VettingApplications_SkillsInterests" 
ON "VettingApplications" USING GIN ("SkillsInterests");

-- Analytics and reporting
CREATE INDEX "IX_VettingApplications_Analytics" 
ON "VettingApplications" ("Status", "ExperienceLevel", "IsAnonymous", "CreatedAt")
WHERE "DeletedAt" IS NULL;

-- Reviewer workload management
CREATE INDEX "IX_VettingReviewers_Workload" 
ON "VettingReviewers" ("IsActive", "IsAvailable", "CurrentWorkload", "MaxWorkload");
```

## Migration Strategy

### Phase 1: Core Schema Creation

1. **Create Base Tables**: VettingApplication, VettingReviewer, VettingReference
2. **Add Basic Constraints**: Primary keys, required fields, enum constraints
3. **Create Initial Indexes**: Primary performance indexes

### Phase 2: Relationship Setup

1. **Add Foreign Keys**: Link applications to reviewers and references
2. **Configure Cascades**: Proper deletion behavior
3. **Add Navigation Indexes**: Foreign key performance indexes

### Phase 3: Audit and Supporting Tables

1. **Create Audit Tables**: VettingApplicationAuditLog, VettingReferenceAuditLog
2. **Add Notification System**: VettingNotification table
3. **Implement Note System**: VettingApplicationNote, VettingNoteAttachment

### Phase 4: Advanced Features

1. **Add Decision Tracking**: VettingDecision, VettingDecisionAuditLog
2. **Implement JSONB Indexes**: Skills/interests search optimization
3. **Add Composite Indexes**: Complex query optimization

### Migration Commands

```bash
# Generate initial migration
dotnet ef migrations add AddVettingSystemCore --project apps/api

# Generate relationship migration
dotnet ef migrations add AddVettingRelationships --project apps/api

# Generate audit trail migration
dotnet ef migrations add AddVettingAuditTrails --project apps/api

# Generate performance optimization migration
dotnet ef migrations add AddVettingPerformanceIndexes --project apps/api

# Apply migrations
dotnet ef database update --project apps/api
```

## Data Privacy & Security

### Encryption Requirements

All personally identifiable information is encrypted at rest using AES-256:

- **Personal Information**: Full name, scene name, email, phone
- **Application Content**: Experience descriptions, safety knowledge, community understanding
- **Reference Information**: Reference names, emails, relationships, responses

### Access Control

- **Vetting Reviewers**: Access to assigned applications only
- **Vetting Admins**: Full system access including analytics
- **System Users**: No direct access to vetting data
- **Public API**: Status checking via secure tokens only

### Data Retention

- **Active Applications**: Retained until final decision + 90 days
- **Approved Applications**: Personal data retained for 2 years
- **Denied Applications**: Personal data purged after 1 year
- **Audit Logs**: Retained for 7 years for compliance
- **Draft Applications**: Auto-purged after 30 days of inactivity

## Sample Queries

### Common Dashboard Queries

```sql
-- Reviewer dashboard - pending applications
SELECT a."Id", a."ApplicationNumber", a."Status", a."Priority", 
       a."CreatedAt", a."AssignedReviewerId",
       COUNT(r."Id") as "TotalReferences",
       COUNT(CASE WHEN r."Status" = 4 THEN 1 END) as "CompletedReferences"
FROM "VettingApplications" a
LEFT JOIN "VettingReferences" r ON a."Id" = r."ApplicationId"
WHERE a."DeletedAt" IS NULL 
  AND a."Status" IN (2, 3, 4) -- Submitted, UnderReview, PendingReferences
  AND (a."AssignedReviewerId" = @reviewerId OR a."AssignedReviewerId" IS NULL)
GROUP BY a."Id", a."ApplicationNumber", a."Status", a."Priority", 
         a."CreatedAt", a."AssignedReviewerId"
ORDER BY a."Priority" DESC, a."CreatedAt" ASC;

-- Public status check
SELECT a."Status", a."CreatedAt", a."ReviewStartedAt", a."DecisionMadeAt"
FROM "VettingApplications" a
WHERE a."StatusToken" = @statusToken 
  AND a."DeletedAt" IS NULL;

-- Reference reminder queue
SELECT r."Id", r."ApplicationId", r."EncryptedEmail", r."ContactedAt"
FROM "VettingReferences" r
INNER JOIN "VettingApplications" a ON r."ApplicationId" = a."Id"
WHERE r."Status" IN (1, 2) -- NotContacted, Contacted
  AND a."Status" IN (2, 3, 4) -- Active application statuses
  AND a."DeletedAt" IS NULL
  AND (r."ContactedAt" IS NULL OR r."ContactedAt" < NOW() - INTERVAL '3 days')
  AND (r."FirstReminderSentAt" IS NULL OR r."FirstReminderSentAt" < NOW() - INTERVAL '7 days');

-- Analytics - application volume by month
SELECT DATE_TRUNC('month', "CreatedAt") as "Month",
       COUNT(*) as "TotalApplications",
       COUNT(CASE WHEN "Status" = 7 THEN 1 END) as "ApprovedApplications",
       COUNT(CASE WHEN "Status" = 8 THEN 1 END) as "DeniedApplications"
FROM "VettingApplications"
WHERE "DeletedAt" IS NULL
  AND "CreatedAt" >= NOW() - INTERVAL '12 months'
GROUP BY DATE_TRUNC('month', "CreatedAt")
ORDER BY "Month" DESC;
```

### Performance Validation Queries

```sql
-- Verify index usage for dashboard query
EXPLAIN (ANALYZE, BUFFERS) 
SELECT a."Id", a."Status", a."CreatedAt"
FROM "VettingApplications" a
WHERE a."Status" IN (2, 3, 4) 
  AND a."DeletedAt" IS NULL
  AND a."AssignedReviewerId" = 'reviewer-guid'
ORDER BY a."Priority" DESC, a."CreatedAt" ASC;

-- Check JSONB index performance
EXPLAIN (ANALYZE, BUFFERS)
SELECT a."Id", a."SkillsInterests"
FROM "VettingApplications" a
WHERE a."SkillsInterests" @> '["rope-bondage"]'
  AND a."DeletedAt" IS NULL;
```

## Database Initialization

The vetting system tables will be created and populated using the existing DatabaseInitializationService pattern.

### Seed Data Requirements

```csharp
// VettingReviewer seed data
var seedReviewers = new[]
{
    new VettingReviewer 
    { 
        UserId = safetyOfficerUser.Id, 
        MaxWorkload = 15,
        Specializations = new List<string> { "safety", "advanced-techniques" }
    },
    new VettingReviewer 
    { 
        UserId = adminUser.Id, 
        MaxWorkload = 10,
        Specializations = new List<string> { "community-guidelines", "leadership" }
    }
};

// Sample VettingApplication for testing
var sampleApplication = new VettingApplication
{
    ApplicationNumber = "VET-20250913-0001",
    Status = ApplicationStatus.Submitted,
    EncryptedFullName = await encryptionService.EncryptAsync("Test Applicant"),
    EncryptedSceneName = await encryptionService.EncryptAsync("TestRope"),
    EncryptedEmail = await encryptionService.EncryptAsync("test@example.com"),
    ExperienceLevel = ExperienceLevel.Intermediate,
    YearsExperience = 3,
    EncryptedExperienceDescription = await encryptionService.EncryptAsync("Sample experience description"),
    // ... other required fields
};
```

## Quality Assurance Checklist

### Database Design Validation
- [ ] All entities follow WitchCityRope naming conventions
- [ ] UTC DateTime handling implemented for all timestamp fields
- [ ] Proper encryption configuration for sensitive data
- [ ] Strategic indexing for performance optimization
- [ ] Audit trails configured for all sensitive operations
- [ ] Soft delete patterns implemented where appropriate

### Security Validation  
- [ ] PII encryption requirements documented and implemented
- [ ] Role-based access control supported at database level
- [ ] Audit logging captures all required information
- [ ] Data retention policies clearly defined
- [ ] Privacy-first design principles validated

### Performance Validation
- [ ] Composite indexes support common query patterns
- [ ] JSONB indexes configured for flexible search
- [ ] Partial indexes optimize filtered queries
- [ ] Query performance validated with EXPLAIN ANALYZE
- [ ] Connection pooling considerations documented

### Migration Safety
- [ ] Incremental migration strategy defined
- [ ] Rollback procedures documented
- [ ] Data preservation during schema changes
- [ ] Performance impact assessment completed
- [ ] Production deployment plan reviewed

## Implementation Notes

### Integration with Existing Systems

The vetting system integrates with existing WitchCityRope components:

- **ApplicationUser**: Links approved applicants to user accounts
- **DatabaseInitializationService**: Automatic table creation and seeding
- **Encryption Services**: Consistent PII protection patterns
- **Notification System**: Email delivery and tracking
- **Audit Framework**: Standardized change tracking

### Future Enhancements

The database design supports planned enhancements:

- **Document Attachments**: VettingNoteAttachment table ready for file storage
- **Interview Scheduling**: Calendar integration fields included
- **Advanced Analytics**: JSONB fields support flexible reporting
- **Batch Operations**: Bulk assignment and notification capabilities
- **API Rate Limiting**: Audit logs track API usage patterns

This comprehensive database design provides a solid foundation for the WitchCityRope Vetting System while maintaining consistency with existing architectural patterns and supporting future growth requirements.
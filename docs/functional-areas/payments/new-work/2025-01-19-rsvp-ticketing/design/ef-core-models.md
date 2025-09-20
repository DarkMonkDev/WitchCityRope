# Entity Framework Core Models: RSVP and Ticketing System
<!-- Date: 2025-01-19 -->
<!-- Version: 1.0 -->
<!-- Owner: Database Designer Agent -->
<!-- Status: Design Complete -->

## Overview

This document defines the complete Entity Framework Core model implementation for the RSVP and Ticketing System, following WitchCityRope's established patterns for PostgreSQL integration, UTC DateTime handling, and Entity Framework configurations.

## Critical Entity Model Patterns

### ⚠️ MANDATORY: Entity ID Initialization Pattern
**CRITICAL**: Following the lessons learned from Events admin persistence bugs, ALL entity models MUST have simple `public Guid Id { get; set; }` without initializers.

```csharp
// ✅ CORRECT - Simple property, EF handles generation
public class EventParticipation
{
    public Guid Id { get; set; }  // Let Entity Framework manage ID lifecycle
}

// ❌ NEVER DO THIS - Breaks EF Core ID generation
public class EventParticipation
{
    public Guid Id { get; set; } = Guid.NewGuid();  // THIS CAUSES UPDATE instead of INSERT!
}
```

## Entity Models

### 1. EventParticipation Entity

```csharp
using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.Participation.Entities;

/// <summary>
/// EventParticipation entity - Central tracking for both RSVPs and ticket purchases
/// Supports both social event RSVPs and class event ticket purchases
/// </summary>
public class EventParticipation
{
    /// <summary>
    /// Unique identifier for the participation record
    /// CRITICAL: Simple property without initializer per EF Core best practices
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Reference to the event
    /// </summary>
    [Required]
    public Guid EventId { get; set; }

    /// <summary>
    /// Reference to the participating user
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Type of participation: RSVP or Ticket
    /// </summary>
    [Required]
    public ParticipationType ParticipationType { get; set; }

    /// <summary>
    /// Current status of the participation
    /// </summary>
    [Required]
    public ParticipationStatus Status { get; set; } = ParticipationStatus.Active;

    /// <summary>
    /// When the participation was created (UTC)
    /// CRITICAL: UTC for PostgreSQL timestamptz compatibility
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the participation was cancelled (UTC)
    /// </summary>
    public DateTime? CancelledAt { get; set; }

    /// <summary>
    /// Reason for cancellation
    /// </summary>
    public string? CancellationReason { get; set; }

    /// <summary>
    /// Optional notes from the participant
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Flexible metadata for additional information
    /// Stored as JSONB in PostgreSQL
    /// </summary>
    [Required]
    public string Metadata { get; set; } = "{}";

    /// <summary>
    /// User who created this participation
    /// </summary>
    public Guid? CreatedBy { get; set; }

    /// <summary>
    /// User who last updated this participation
    /// </summary>
    public Guid? UpdatedBy { get; set; }

    /// <summary>
    /// When the participation was last updated (UTC)
    /// </summary>
    [Required]
    public DateTime UpdatedAt { get; set; }

    // Navigation Properties
    public Event Event { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
    public ApplicationUser? CreatedByUser { get; set; }
    public ApplicationUser? UpdatedByUser { get; set; }
    public TicketPurchase? TicketPurchase { get; set; }
    public ICollection<ParticipationHistory> History { get; set; } = new List<ParticipationHistory>();

    /// <summary>
    /// Constructor initializes required fields with proper UTC handling
    /// </summary>
    public EventParticipation()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Constructor for new participation
    /// </summary>
    public EventParticipation(Guid eventId, Guid userId, ParticipationType type) : this()
    {
        EventId = eventId;
        UserId = userId;
        ParticipationType = type;
    }

    /// <summary>
    /// Determines if this participation can be cancelled
    /// </summary>
    public bool CanBeCancelled()
    {
        return Status == ParticipationStatus.Active;
    }

    /// <summary>
    /// Cancels the participation with reason
    /// </summary>
    public void Cancel(string? reason = null)
    {
        if (!CanBeCancelled())
            throw new InvalidOperationException("Participation cannot be cancelled in current status");

        Status = ParticipationStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        CancellationReason = reason;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

### 2. TicketPurchase Entity

```csharp
using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.Participation.Entities;

/// <summary>
/// TicketPurchase entity - Payment and ticket details for ticket-based participations
/// Links to PayPal payment processing and maintains PCI compliance
/// </summary>
public class TicketPurchase
{
    /// <summary>
    /// Unique identifier for the ticket purchase
    /// CRITICAL: Simple property without initializer per EF Core best practices
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Reference to the participation record
    /// </summary>
    [Required]
    public Guid ParticipationId { get; set; }

    /// <summary>
    /// Reference to the ticket type purchased
    /// </summary>
    [Required]
    public Guid TicketTypeId { get; set; }

    /// <summary>
    /// Amount paid (stored as separate decimal property per lessons learned)
    /// Avoids nullable owned entity issues with Money value objects
    /// </summary>
    [Required]
    public decimal AmountValue { get; set; }

    /// <summary>
    /// Currency code (defaults to USD)
    /// </summary>
    [Required]
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// PayPal order ID for payment tracking
    /// CRITICAL: Never store credit card numbers for PCI compliance
    /// </summary>
    [Required]
    public string PaymentId { get; set; } = string.Empty;

    /// <summary>
    /// Current payment status
    /// </summary>
    [Required]
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

    /// <summary>
    /// When payment was completed (UTC)
    /// </summary>
    public DateTime? PaymentDate { get; set; }

    /// <summary>
    /// Amount refunded (if any)
    /// </summary>
    public decimal? RefundAmountValue { get; set; }

    /// <summary>
    /// When refund was processed (UTC)
    /// </summary>
    public DateTime? RefundDate { get; set; }

    /// <summary>
    /// Reason for refund
    /// </summary>
    public string? RefundReason { get; set; }

    /// <summary>
    /// Payment processing metadata (PayPal responses, etc.)
    /// Stored as JSONB in PostgreSQL
    /// </summary>
    [Required]
    public string PaymentMetadata { get; set; } = "{}";

    /// <summary>
    /// When the ticket purchase was created (UTC)
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the ticket purchase was last updated (UTC)
    /// </summary>
    [Required]
    public DateTime UpdatedAt { get; set; }

    // Navigation Properties
    public EventParticipation Participation { get; set; } = null!;
    public TicketType TicketType { get; set; } = null!;
    public ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();

    /// <summary>
    /// Constructor initializes required fields with proper UTC handling
    /// </summary>
    public TicketPurchase()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Constructor for new ticket purchase
    /// </summary>
    public TicketPurchase(Guid participationId, Guid ticketTypeId, decimal amount, string paymentId) : this()
    {
        ParticipationId = participationId;
        TicketTypeId = ticketTypeId;
        AmountValue = amount;
        PaymentId = paymentId;
    }

    /// <summary>
    /// Determines if this purchase can be refunded
    /// </summary>
    public bool CanBeRefunded()
    {
        return PaymentStatus == PaymentStatus.Completed && RefundAmountValue == null;
    }

    /// <summary>
    /// Processes a refund for this ticket purchase
    /// </summary>
    public void ProcessRefund(decimal refundAmount, string? reason = null)
    {
        if (!CanBeRefunded())
            throw new InvalidOperationException("Ticket purchase cannot be refunded");

        if (refundAmount > AmountValue)
            throw new ArgumentException("Refund amount cannot exceed paid amount");

        RefundAmountValue = refundAmount;
        RefundDate = DateTime.UtcNow;
        RefundReason = reason;
        PaymentStatus = refundAmount >= AmountValue ? PaymentStatus.Refunded : PaymentStatus.PartiallyRefunded;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Completes the payment processing
    /// </summary>
    public void CompletePayment()
    {
        PaymentStatus = PaymentStatus.Completed;
        PaymentDate = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks payment as failed
    /// </summary>
    public void MarkPaymentFailed()
    {
        PaymentStatus = PaymentStatus.Failed;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

### 3. ParticipationHistory Entity

```csharp
using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.Participation.Entities;

/// <summary>
/// ParticipationHistory entity - Comprehensive audit trail for participation changes
/// Maintains complete change tracking for compliance and debugging
/// </summary>
public class ParticipationHistory
{
    /// <summary>
    /// Unique identifier for the history record
    /// CRITICAL: Simple property without initializer per EF Core best practices
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Reference to the participation record
    /// </summary>
    [Required]
    public Guid ParticipationId { get; set; }

    /// <summary>
    /// Type of action performed
    /// </summary>
    [Required]
    public string ActionType { get; set; } = string.Empty;

    /// <summary>
    /// Previous values before change (JSONB)
    /// </summary>
    public string? OldValues { get; set; }

    /// <summary>
    /// New values after change (JSONB)
    /// </summary>
    public string? NewValues { get; set; }

    /// <summary>
    /// User who made the change
    /// </summary>
    public Guid? ChangedBy { get; set; }

    /// <summary>
    /// Reason for the change
    /// </summary>
    public string? ChangeReason { get; set; }

    /// <summary>
    /// IP address of the user making the change
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string for browser identification
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// When the change was made (UTC)
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; }

    // Navigation Properties
    public EventParticipation Participation { get; set; } = null!;
    public ApplicationUser? ChangedByUser { get; set; }

    /// <summary>
    /// Constructor initializes required fields with proper UTC handling
    /// </summary>
    public ParticipationHistory()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Constructor for new history record
    /// </summary>
    public ParticipationHistory(Guid participationId, string actionType) : this()
    {
        ParticipationId = participationId;
        ActionType = actionType;
    }
}
```

### 4. PaymentTransaction Entity

```csharp
using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.Participation.Entities;

/// <summary>
/// PaymentTransaction entity - Links to PayPal payment processing with audit trail
/// Maintains PCI-compliant payment tracking with comprehensive audit trail
/// </summary>
public class PaymentTransaction
{
    /// <summary>
    /// Unique identifier for the transaction
    /// CRITICAL: Simple property without initializer per EF Core best practices
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Reference to the ticket purchase
    /// </summary>
    [Required]
    public Guid TicketPurchaseId { get; set; }

    /// <summary>
    /// PayPal order identifier
    /// </summary>
    [Required]
    public string PayPalOrderId { get; set; } = string.Empty;

    /// <summary>
    /// PayPal payment identifier (for completed payments)
    /// </summary>
    public string? PayPalPaymentId { get; set; }

    /// <summary>
    /// Type of transaction
    /// </summary>
    [Required]
    public string TransactionType { get; set; } = string.Empty;

    /// <summary>
    /// Transaction amount
    /// </summary>
    [Required]
    public decimal Amount { get; set; }

    /// <summary>
    /// Currency code
    /// </summary>
    [Required]
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Current transaction status
    /// </summary>
    [Required]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// PayPal response data (JSONB)
    /// </summary>
    [Required]
    public string PayPalResponse { get; set; } = "{}";

    /// <summary>
    /// When the transaction was processed (UTC)
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// When the transaction record was created (UTC)
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; }

    // Navigation Properties
    public TicketPurchase TicketPurchase { get; set; } = null!;

    /// <summary>
    /// Constructor initializes required fields with proper UTC handling
    /// </summary>
    public PaymentTransaction()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Constructor for new transaction
    /// </summary>
    public PaymentTransaction(Guid ticketPurchaseId, string paypalOrderId, string transactionType, decimal amount) : this()
    {
        TicketPurchaseId = ticketPurchaseId;
        PayPalOrderId = paypalOrderId;
        TransactionType = transactionType;
        Amount = amount;
    }
}
```

## Enumeration Definitions

```csharp
namespace WitchCityRope.Api.Features.Participation.Entities;

/// <summary>
/// Type of event participation
/// </summary>
public enum ParticipationType
{
    /// <summary>
    /// RSVP for social events (no payment required)
    /// </summary>
    RSVP = 1,

    /// <summary>
    /// Ticket purchase for classes (payment required)
    /// </summary>
    Ticket = 2
}

/// <summary>
/// Status of event participation
/// </summary>
public enum ParticipationStatus
{
    /// <summary>
    /// Active participation
    /// </summary>
    Active = 1,

    /// <summary>
    /// Cancelled participation
    /// </summary>
    Cancelled = 2,

    /// <summary>
    /// Refunded participation (for tickets)
    /// </summary>
    Refunded = 3,

    /// <summary>
    /// Waitlisted participation
    /// </summary>
    Waitlisted = 4
}

/// <summary>
/// Payment status for ticket purchases
/// </summary>
public enum PaymentStatus
{
    /// <summary>
    /// Payment pending
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Payment completed successfully
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Payment failed
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Payment fully refunded
    /// </summary>
    Refunded = 4,

    /// <summary>
    /// Payment partially refunded
    /// </summary>
    PartiallyRefunded = 5
}
```

## Entity Framework Configurations

### 1. EventParticipationConfiguration

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WitchCityRope.Api.Features.Participation.Entities;

namespace WitchCityRope.Api.Features.Participation.Data;

/// <summary>
/// Entity Framework configuration for EventParticipation entity
/// Implements PostgreSQL-specific patterns and constraints
/// </summary>
public class EventParticipationConfiguration : IEntityTypeConfiguration<EventParticipation>
{
    public void Configure(EntityTypeBuilder<EventParticipation> builder)
    {
        // Table mapping
        builder.ToTable("EventParticipations", "public");
        builder.HasKey(e => e.Id);

        // Property configurations with PostgreSQL patterns
        builder.Property(e => e.Id)
               .ValueGeneratedOnAdd(); // Let PostgreSQL generate UUIDs

        builder.Property(e => e.EventId)
               .IsRequired();

        builder.Property(e => e.UserId)
               .IsRequired();

        builder.Property(e => e.ParticipationType)
               .IsRequired()
               .HasConversion<int>(); // Store as INTEGER

        builder.Property(e => e.Status)
               .IsRequired()
               .HasConversion<int>(); // Store as INTEGER

        // CRITICAL: UTC DateTime handling for PostgreSQL
        builder.Property(e => e.CreatedAt)
               .IsRequired()
               .HasColumnType("timestamptz");

        builder.Property(e => e.CancelledAt)
               .HasColumnType("timestamptz");

        builder.Property(e => e.UpdatedAt)
               .IsRequired()
               .HasColumnType("timestamptz");

        builder.Property(e => e.CancellationReason)
               .HasMaxLength(1000);

        builder.Property(e => e.Notes)
               .HasMaxLength(2000);

        // JSONB configuration for PostgreSQL
        builder.Property(e => e.Metadata)
               .IsRequired()
               .HasColumnType("jsonb")
               .HasDefaultValue("{}");

        // Foreign key relationships
        builder.HasOne(e => e.Event)
               .WithMany()
               .HasForeignKey(e => e.EventId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.User)
               .WithMany()
               .HasForeignKey(e => e.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.CreatedByUser)
               .WithMany()
               .HasForeignKey(e => e.CreatedBy)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.UpdatedByUser)
               .WithMany()
               .HasForeignKey(e => e.UpdatedBy)
               .OnDelete(DeleteBehavior.SetNull);

        // One-to-one relationship with TicketPurchase
        builder.HasOne(e => e.TicketPurchase)
               .WithOne(tp => tp.Participation)
               .HasForeignKey<TicketPurchase>(tp => tp.ParticipationId)
               .OnDelete(DeleteBehavior.Cascade);

        // One-to-many relationship with ParticipationHistory
        builder.HasMany(e => e.History)
               .WithOne(h => h.Participation)
               .HasForeignKey(h => h.ParticipationId)
               .OnDelete(DeleteBehavior.Cascade);

        // Indexes for performance
        builder.HasIndex(e => new { e.EventId, e.Status })
               .HasDatabaseName("IX_EventParticipations_EventId_Status");

        builder.HasIndex(e => new { e.UserId, e.Status })
               .HasDatabaseName("IX_EventParticipations_UserId_Status");

        builder.HasIndex(e => e.CreatedAt)
               .HasDatabaseName("IX_EventParticipations_CreatedAt");

        // GIN index for JSONB metadata
        builder.HasIndex(e => e.Metadata)
               .HasDatabaseName("IX_EventParticipations_Metadata_Gin")
               .HasMethod("gin");

        // Business rule constraints
        builder.ToTable(t => t.HasCheckConstraint(
            "CHK_EventParticipations_ParticipationType",
            "\"ParticipationType\" IN (1, 2)"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CHK_EventParticipations_Status",
            "\"Status\" IN (1, 2, 3, 4)"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CHK_EventParticipations_CancelledAt_Logic",
            "(\"Status\" IN (2, 3) AND \"CancelledAt\" IS NOT NULL) OR (\"Status\" NOT IN (2, 3) AND \"CancelledAt\" IS NULL)"));

        // Unique constraint: one participation per user per event
        builder.HasIndex(e => new { e.UserId, e.EventId })
               .IsUnique()
               .HasDatabaseName("UQ_EventParticipations_User_Event_Active");
    }
}
```

### 2. TicketPurchaseConfiguration

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WitchCityRope.Api.Features.Participation.Entities;

namespace WitchCityRope.Api.Features.Participation.Data;

/// <summary>
/// Entity Framework configuration for TicketPurchase entity
/// Implements PCI-compliant payment tracking patterns
/// </summary>
public class TicketPurchaseConfiguration : IEntityTypeConfiguration<TicketPurchase>
{
    public void Configure(EntityTypeBuilder<TicketPurchase> builder)
    {
        // Table mapping
        builder.ToTable("TicketPurchases", "public");
        builder.HasKey(tp => tp.Id);

        // Property configurations
        builder.Property(tp => tp.Id)
               .ValueGeneratedOnAdd();

        builder.Property(tp => tp.ParticipationId)
               .IsRequired();

        builder.Property(tp => tp.TicketTypeId)
               .IsRequired();

        // Money value object pattern (separate properties)
        builder.Property(tp => tp.AmountValue)
               .IsRequired()
               .HasColumnType("decimal(10,2)")
               .HasColumnName("AmountValue");

        builder.Property(tp => tp.Currency)
               .IsRequired()
               .HasMaxLength(3)
               .HasDefaultValue("USD");

        builder.Property(tp => tp.PaymentId)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(tp => tp.PaymentStatus)
               .IsRequired()
               .HasConversion<int>();

        // UTC DateTime handling
        builder.Property(tp => tp.PaymentDate)
               .HasColumnType("timestamptz");

        builder.Property(tp => tp.CreatedAt)
               .IsRequired()
               .HasColumnType("timestamptz");

        builder.Property(tp => tp.UpdatedAt)
               .IsRequired()
               .HasColumnType("timestamptz");

        builder.Property(tp => tp.RefundDate)
               .HasColumnType("timestamptz");

        // Refund amount handling
        builder.Property(tp => tp.RefundAmountValue)
               .HasColumnType("decimal(10,2)")
               .HasColumnName("RefundAmountValue");

        builder.Property(tp => tp.RefundReason)
               .HasMaxLength(1000);

        // JSONB for payment metadata
        builder.Property(tp => tp.PaymentMetadata)
               .IsRequired()
               .HasColumnType("jsonb")
               .HasDefaultValue("{}");

        // Foreign key relationships
        builder.HasOne(tp => tp.Participation)
               .WithOne(e => e.TicketPurchase)
               .HasForeignKey<TicketPurchase>(tp => tp.ParticipationId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(tp => tp.TicketType)
               .WithMany()
               .HasForeignKey(tp => tp.TicketTypeId)
               .OnDelete(DeleteBehavior.Restrict);

        // One-to-many relationship with PaymentTransaction
        builder.HasMany(tp => tp.PaymentTransactions)
               .WithOne(pt => pt.TicketPurchase)
               .HasForeignKey(pt => pt.TicketPurchaseId)
               .OnDelete(DeleteBehavior.Cascade);

        // Indexes for performance
        builder.HasIndex(tp => tp.PaymentId)
               .IsUnique()
               .HasDatabaseName("IX_TicketPurchases_PaymentId");

        builder.HasIndex(tp => new { tp.PaymentStatus, tp.PaymentDate })
               .HasDatabaseName("IX_TicketPurchases_PaymentStatus_PaymentDate");

        // Partial indexes for specific use cases
        builder.HasIndex(tp => tp.CreatedAt)
               .HasDatabaseName("IX_TicketPurchases_PendingPayments")
               .HasFilter("\"PaymentStatus\" = 1"); // Pending only

        builder.HasIndex(tp => tp.CreatedAt)
               .HasDatabaseName("IX_TicketPurchases_FailedPayments")
               .HasFilter("\"PaymentStatus\" = 3"); // Failed only

        // Business rule constraints
        builder.ToTable(t => t.HasCheckConstraint(
            "CHK_TicketPurchases_AmountValue",
            "\"AmountValue\" >= 0"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CHK_TicketPurchases_PaymentStatus",
            "\"PaymentStatus\" IN (1, 2, 3, 4, 5)"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CHK_TicketPurchases_RefundLogic",
            "(\"PaymentStatus\" IN (4, 5) AND \"RefundAmountValue\" IS NOT NULL AND \"RefundDate\" IS NOT NULL) OR (\"PaymentStatus\" NOT IN (4, 5) AND \"RefundAmountValue\" IS NULL AND \"RefundDate\" IS NULL)"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CHK_TicketPurchases_RefundAmount",
            "\"RefundAmountValue\" IS NULL OR \"RefundAmountValue\" <= \"AmountValue\""));

        // Unique constraint: one ticket purchase per participation
        builder.HasIndex(tp => tp.ParticipationId)
               .IsUnique()
               .HasDatabaseName("UQ_TicketPurchases_Participation");
    }
}
```

### 3. ParticipationHistoryConfiguration

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WitchCityRope.Api.Features.Participation.Entities;

namespace WitchCityRope.Api.Features.Participation.Data;

/// <summary>
/// Entity Framework configuration for ParticipationHistory entity
/// Implements comprehensive audit trail patterns
/// </summary>
public class ParticipationHistoryConfiguration : IEntityTypeConfiguration<ParticipationHistory>
{
    public void Configure(EntityTypeBuilder<ParticipationHistory> builder)
    {
        // Table mapping
        builder.ToTable("ParticipationHistory", "public");
        builder.HasKey(h => h.Id);

        // Property configurations
        builder.Property(h => h.Id)
               .ValueGeneratedOnAdd();

        builder.Property(h => h.ParticipationId)
               .IsRequired();

        builder.Property(h => h.ActionType)
               .IsRequired()
               .HasMaxLength(50);

        // JSONB for old/new values
        builder.Property(h => h.OldValues)
               .HasColumnType("jsonb");

        builder.Property(h => h.NewValues)
               .HasColumnType("jsonb");

        builder.Property(h => h.ChangeReason)
               .HasMaxLength(1000);

        builder.Property(h => h.IpAddress)
               .HasMaxLength(45); // IPv6 compatible

        builder.Property(h => h.UserAgent)
               .HasMaxLength(500);

        // UTC DateTime handling
        builder.Property(h => h.CreatedAt)
               .IsRequired()
               .HasColumnType("timestamptz");

        // Foreign key relationships
        builder.HasOne(h => h.Participation)
               .WithMany(e => e.History)
               .HasForeignKey(h => h.ParticipationId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(h => h.ChangedByUser)
               .WithMany()
               .HasForeignKey(h => h.ChangedBy)
               .OnDelete(DeleteBehavior.SetNull);

        // Indexes for performance
        builder.HasIndex(h => new { h.ParticipationId, h.CreatedAt })
               .HasDatabaseName("IX_ParticipationHistory_ParticipationId_CreatedAt");

        builder.HasIndex(h => h.ActionType)
               .HasDatabaseName("IX_ParticipationHistory_ActionType");

        // GIN indexes for JSONB queries
        builder.HasIndex(h => h.OldValues)
               .HasDatabaseName("IX_ParticipationHistory_OldValues_Gin")
               .HasMethod("gin");

        builder.HasIndex(h => h.NewValues)
               .HasDatabaseName("IX_ParticipationHistory_NewValues_Gin")
               .HasMethod("gin");

        // Business rule constraints
        builder.ToTable(t => t.HasCheckConstraint(
            "CHK_ParticipationHistory_ActionType",
            "\"ActionType\" IN ('Created', 'Updated', 'Cancelled', 'Refunded', 'StatusChanged', 'PaymentUpdated')"));
    }
}
```

### 4. PaymentTransactionConfiguration

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WitchCityRope.Api.Features.Participation.Entities;

namespace WitchCityRope.Api.Features.Participation.Data;

/// <summary>
/// Entity Framework configuration for PaymentTransaction entity
/// Implements PCI-compliant payment audit trail
/// </summary>
public class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        // Table mapping
        builder.ToTable("PaymentTransactions", "public");
        builder.HasKey(pt => pt.Id);

        // Property configurations
        builder.Property(pt => pt.Id)
               .ValueGeneratedOnAdd();

        builder.Property(pt => pt.TicketPurchaseId)
               .IsRequired();

        builder.Property(pt => pt.PayPalOrderId)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(pt => pt.PayPalPaymentId)
               .HasMaxLength(100);

        builder.Property(pt => pt.TransactionType)
               .IsRequired()
               .HasMaxLength(20);

        builder.Property(pt => pt.Amount)
               .IsRequired()
               .HasColumnType("decimal(10,2)");

        builder.Property(pt => pt.Currency)
               .IsRequired()
               .HasMaxLength(3)
               .HasDefaultValue("USD");

        builder.Property(pt => pt.Status)
               .IsRequired()
               .HasMaxLength(20);

        // JSONB for PayPal response data
        builder.Property(pt => pt.PayPalResponse)
               .IsRequired()
               .HasColumnType("jsonb")
               .HasDefaultValue("{}");

        // UTC DateTime handling
        builder.Property(pt => pt.ProcessedAt)
               .HasColumnType("timestamptz");

        builder.Property(pt => pt.CreatedAt)
               .IsRequired()
               .HasColumnType("timestamptz");

        // Foreign key relationships
        builder.HasOne(pt => pt.TicketPurchase)
               .WithMany(tp => tp.PaymentTransactions)
               .HasForeignKey(pt => pt.TicketPurchaseId)
               .OnDelete(DeleteBehavior.Cascade);

        // Indexes for performance
        builder.HasIndex(pt => pt.PayPalOrderId)
               .HasDatabaseName("IX_PaymentTransactions_PayPalOrderId");

        builder.HasIndex(pt => pt.ProcessedAt)
               .HasDatabaseName("IX_PaymentTransactions_ProcessedAt");

        builder.HasIndex(pt => new { pt.TicketPurchaseId, pt.CreatedAt })
               .HasDatabaseName("IX_PaymentTransactions_TicketPurchase_CreatedAt");

        // Business rule constraints
        builder.ToTable(t => t.HasCheckConstraint(
            "CHK_PaymentTransactions_TransactionType",
            "\"TransactionType\" IN ('Payment', 'Refund', 'Chargeback')"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CHK_PaymentTransactions_Status",
            "\"Status\" IN ('Pending', 'Completed', 'Failed', 'Cancelled')"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CHK_PaymentTransactions_Amount",
            "\"Amount\" > 0"));
    }
}
```

## DbContext Integration

### ApplicationDbContext Updates

```csharp
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Features.Participation.Data;

namespace WitchCityRope.Api.Data;

public partial class ApplicationDbContext
{
    // DbSets for RSVP and Ticketing entities
    public DbSet<EventParticipation> EventParticipations { get; set; } = null!;
    public DbSet<TicketPurchase> TicketPurchases { get; set; } = null!;
    public DbSet<ParticipationHistory> ParticipationHistory { get; set; } = null!;
    public DbSet<PaymentTransaction> PaymentTransactions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply RSVP and Ticketing configurations
        modelBuilder.ApplyConfiguration(new EventParticipationConfiguration());
        modelBuilder.ApplyConfiguration(new TicketPurchaseConfiguration());
        modelBuilder.ApplyConfiguration(new ParticipationHistoryConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentTransactionConfiguration());

        // Apply other existing configurations...
    }

    /// <summary>
    /// Override SaveChangesAsync to implement audit field updates
    /// CRITICAL: Maintains UTC DateTime handling for PostgreSQL
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateAuditFields()
    {
        var entries = ChangeTracker.Entries<EventParticipation>()
            .Concat(ChangeTracker.Entries<TicketPurchase>().Cast<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry>())
            .Concat(ChangeTracker.Entries<ParticipationHistory>().Cast<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry>())
            .Concat(ChangeTracker.Entries<PaymentTransaction>().Cast<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry>());

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity is EventParticipation participation)
                {
                    participation.CreatedAt = DateTime.UtcNow;
                    participation.UpdatedAt = DateTime.UtcNow;
                }
                else if (entry.Entity is TicketPurchase ticketPurchase)
                {
                    ticketPurchase.CreatedAt = DateTime.UtcNow;
                    ticketPurchase.UpdatedAt = DateTime.UtcNow;
                }
                else if (entry.Entity is ParticipationHistory history)
                {
                    history.CreatedAt = DateTime.UtcNow;
                }
                else if (entry.Entity is PaymentTransaction transaction)
                {
                    transaction.CreatedAt = DateTime.UtcNow;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                if (entry.Entity is EventParticipation participation)
                {
                    participation.UpdatedAt = DateTime.UtcNow;
                }
                else if (entry.Entity is TicketPurchase ticketPurchase)
                {
                    ticketPurchase.UpdatedAt = DateTime.UtcNow;
                }
            }
        }
    }
}
```

## Migration Commands

### Creating the Migration

```bash
# Generate migration for RSVP and Ticketing system
dotnet ef migrations add AddRsvpTicketingSystem --project apps/api --startup-project apps/api

# Review the generated migration file
code apps/api/Migrations/[timestamp]_AddRsvpTicketingSystem.cs

# Apply migration to development database
dotnet ef database update --project apps/api --startup-project apps/api

# Generate SQL script for production deployment
dotnet ef migrations script --project apps/api --startup-project apps/api --idempotent --output migration-scripts/rsvp-ticketing-system.sql
```

### Migration Verification

```bash
# Verify tables were created
dotnet ef dbcontext info --project apps/api --startup-project apps/api

# Check for pending migrations
dotnet ef migrations list --project apps/api --startup-project apps/api

# Validate database schema
dotnet test tests/WitchCityRope.IntegrationTests/ --filter "Category=HealthCheck"
```

## Testing Patterns

### Unit Testing Entity Behavior

```csharp
using WitchCityRope.Api.Features.Participation.Entities;
using Xunit;

namespace WitchCityRope.Tests.Features.Participation.Entities;

public class EventParticipationTests
{
    [Fact]
    public void Constructor_SetsRequiredProperties()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var type = ParticipationType.RSVP;

        // Act
        var participation = new EventParticipation(eventId, userId, type);

        // Assert
        Assert.NotEqual(Guid.Empty, participation.Id);
        Assert.Equal(eventId, participation.EventId);
        Assert.Equal(userId, participation.UserId);
        Assert.Equal(type, participation.ParticipationType);
        Assert.Equal(ParticipationStatus.Active, participation.Status);
        Assert.True(participation.CreatedAt > DateTime.UtcNow.AddMinutes(-1));
        Assert.True(participation.UpdatedAt > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public void Cancel_SetsStatusAndTimestamp()
    {
        // Arrange
        var participation = new EventParticipation(Guid.NewGuid(), Guid.NewGuid(), ParticipationType.RSVP);
        var reason = "User requested cancellation";

        // Act
        participation.Cancel(reason);

        // Assert
        Assert.Equal(ParticipationStatus.Cancelled, participation.Status);
        Assert.NotNull(participation.CancelledAt);
        Assert.Equal(reason, participation.CancellationReason);
        Assert.True(participation.UpdatedAt > participation.CreatedAt);
    }

    [Fact]
    public void Cancel_ThrowsException_WhenAlreadyCancelled()
    {
        // Arrange
        var participation = new EventParticipation(Guid.NewGuid(), Guid.NewGuid(), ParticipationType.RSVP);
        participation.Cancel();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => participation.Cancel());
    }
}
```

### Integration Testing with PostgreSQL

```csharp
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Participation.Entities;
using Xunit;

namespace WitchCityRope.Tests.Features.Participation.Integration;

[Collection("PostgreSQL Integration Tests")]
public class EventParticipationIntegrationTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;

    public EventParticipationIntegrationTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SaveParticipation_WithProperUTC_Success()
    {
        // Arrange
        using var scope = _fixture.ServiceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var participation = new EventParticipation(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ParticipationType.RSVP);

        // Act
        context.EventParticipations.Add(participation);
        await context.SaveChangesAsync();

        // Assert
        var saved = await context.EventParticipations
            .FirstOrDefaultAsync(p => p.Id == participation.Id);

        Assert.NotNull(saved);
        Assert.Equal(participation.EventId, saved.EventId);
        Assert.Equal(participation.UserId, saved.UserId);
        Assert.Equal(ParticipationType.RSVP, saved.ParticipationType);
    }

    [Fact]
    public async Task UniqueConstraint_PreventsDuplicateParticipation()
    {
        // Arrange
        using var scope = _fixture.ServiceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var first = new EventParticipation(eventId, userId, ParticipationType.RSVP);
        var second = new EventParticipation(eventId, userId, ParticipationType.Ticket);

        // Act
        context.EventParticipations.Add(first);
        await context.SaveChangesAsync();

        context.EventParticipations.Add(second);

        // Assert
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }
}
```

## Performance Considerations

### Query Optimization Patterns

```csharp
// ✅ CORRECT - Efficient participation status check
var participationStatus = await _context.EventParticipations
    .AsNoTracking()
    .Select(ep => new ParticipationStatusDto
    {
        EventId = ep.EventId,
        UserId = ep.UserId,
        Type = ep.ParticipationType,
        Status = ep.Status,
        ParticipationDate = ep.CreatedAt,
        PaidAmount = ep.TicketPurchase != null ? ep.TicketPurchase.AmountValue : null,
        PaymentId = ep.TicketPurchase != null ? ep.TicketPurchase.PaymentId : null
    })
    .FirstOrDefaultAsync(ep => ep.EventId == eventId && ep.UserId == userId && ep.Status == ParticipationStatus.Active);

// ✅ CORRECT - Efficient event participant list for admin
var participants = await _context.EventParticipations
    .AsNoTracking()
    .Where(ep => ep.EventId == eventId && ep.Status == ParticipationStatus.Active)
    .Select(ep => new ParticipantDetailDto
    {
        UserId = ep.UserId,
        SceneName = ep.User.SceneName,
        Email = ep.User.Email,
        Type = ep.ParticipationType,
        Status = ep.Status,
        RegisteredAt = ep.CreatedAt,
        AmountPaid = ep.TicketPurchase != null ? ep.TicketPurchase.AmountValue : null
    })
    .OrderBy(p => p.RegisteredAt)
    .ToListAsync();
```

## Conclusion

This Entity Framework Core model implementation provides a robust, type-safe foundation for the RSVP and Ticketing System. The design follows WitchCityRope's established patterns while implementing comprehensive audit trails, PCI-compliant payment handling, and optimal PostgreSQL performance patterns.

### Key Implementation Features
- **Entity Framework Best Practices**: Proper ID handling and UTC DateTime patterns
- **PostgreSQL Optimization**: Strategic indexing and JSONB utilization
- **PCI Compliance**: Secure payment data handling with audit trails
- **Performance Focus**: Efficient query patterns and database constraints
- **Maintainability**: Clear separation of concerns and comprehensive testing patterns
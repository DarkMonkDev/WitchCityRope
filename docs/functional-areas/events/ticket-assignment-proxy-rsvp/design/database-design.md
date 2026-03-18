# Database Design: Ticket Assignment & Proxy RSVP

**Date**: 2026-03-18
**Author**: Database Designer Agent
**Status**: Design Complete - Pending Review
**Feature**: Ticket Assignment & Proxy RSVP

**References**:
- [Architectural Decisions](/docs/functional-areas/events/ticket-assignment-proxy-rsvp/requirements/architectural-decisions.md) - 14 confirmed decisions
- [Business Rules](/docs/functional-areas/events/ticket-assignment-proxy-rsvp/requirements/business-rules.md) - 63 business rules + edge cases
- [Use Cases](/docs/functional-areas/events/ticket-assignment-proxy-rsvp/requirements/use-cases.md) - 11 use cases
- [Codebase Analysis](/docs/functional-areas/events/ticket-assignment-proxy-rsvp/research/codebase-analysis.md) - current system gaps
- [Entity Framework Patterns](/docs/standards-processes/development-standards/entity-framework-patterns.md) - EF Core standards
- [Database Migrations Guide](/docs/standards-processes/backend/database-migrations-guide.md) - Migration standards

---

## 1. Entity Relationship Diagram

```
                     ┌─────────────────────┐
                     │   ApplicationUser    │
                     │   (Users table)      │
                     │─────────────────────│
                     │ Id (PK, Guid)        │
                     │ SceneName            │
                     │ VettingStatus        │
                     │ IsActive             │
                     │ TermsOfServiceAccepted│
                     └──────┬──────┬───────┘
                            │      │
          ┌─────────────────┤      ├──────────────────┐
          │ PrincipalId     │      │ DelegateId       │
          │                 │      │                  │
          ▼                 │      ▼                  │
  ┌───────────────────┐     │                         │
  │ AuthorizedContact  │     │                         │
  │ (NEW TABLE)        │     │                         │
  │───────────────────│     │                         │
  │ Id (PK, Guid)      │     │                         │
  │ PrincipalId (FK)   │◄────┘                         │
  │ DelegateId (FK)    │◄─────────────────────────────┘
  │ CreatedAt          │
  │ UpdatedAt          │
  │ RevokedAt (null=active)│
  │ RevokedReason      │
  └───────────────────┘

  ┌─────────────────────────┐       ┌──────────────────────┐
  │      TicketType          │       │    TicketPurchase     │
  │   (MODIFIED TABLE)       │       │   (MODIFIED TABLE)    │
  │─────────────────────────│       │──────────────────────│
  │ Id (PK, Guid)            │       │ Id (PK, Guid)         │
  │ EventId (FK)             │       │ UserId (FK) [buyer]   │
  │ MaxQuantityPerPurchase   │◄──┐   │ PurchasedForUserId FK │
  │   (NEW, int, default 3)  │   │   │   (NEW, nullable)     │
  │ ...existing fields...    │   │   │ ...existing fields... │
  └─────────────────────────┘   │   └──────────────────────┘
                                │
                                │
  ┌────────────────────────────────────────────────────────────┐
  │                   EventAttendance                           │
  │                   (MODIFIED TABLE)                          │
  │────────────────────────────────────────────────────────────│
  │ Id (PK, Guid)                                              │
  │ EventId (FK)                                               │
  │ UserId (FK)           ← the person attending               │
  │ AttendanceType (1=RSVP, 2=Ticket)                         │
  │ Status (1-6, now includes PendingAcceptance=6)            │
  │ TicketPurchaseId (FK, nullable)                            │
  │ SessionId (FK, nullable)                                   │
  │ AssignedByUserId (FK, nullable)  ← NEW: delegate who assigned │
  │ AssignedAt (timestamptz, nullable) ← NEW: when assigned   │
  │ AcceptedAt (timestamptz, nullable) ← NEW: when accepted   │
  │ DeclinedAt (timestamptz, nullable) ← NEW: when declined   │
  │ DeclinedReason (text, nullable)   ← NEW: decline reason   │
  │ ReminderSentAt (timestamptz, null) ← NEW: reminder tracking │
  │ EventWaiverAccepted (bool)                                 │
  │ EventWaiverAcceptedAt (timestamptz, nullable)              │
  │ ...existing fields...                                      │
  └────────────────────────────────────────────────────────────┘

  ┌───────────────────────────┐
  │    AttendanceStatus       │
  │    (MODIFIED ENUM)        │
  │───────────────────────────│
  │ Active = 1                │
  │ Cancelled = 2             │
  │ Refunded = 3              │
  │ Waitlisted = 4            │
  │ PendingPayment = 5        │
  │ PendingAcceptance = 6 NEW │
  └───────────────────────────┘

  ┌────────────────────────────────────┐
  │       AttendanceHistory            │
  │       (EXISTING - new ActionTypes) │
  │────────────────────────────────────│
  │ New ActionTypes to support:        │
  │  'TicketAssigned'                  │
  │  'TicketReassigned'                │
  │  'TicketAccepted'                  │
  │  'TicketDeclined'                  │
  │  'ProxyRSVPCreated'               │
  │  'ProxyRSVPAccepted'              │
  │  'ProxyRSVPDeclined'              │
  │  'ReminderSent'                   │
  └────────────────────────────────────┘
```

### Relationship Summary

| Relationship | Cardinality | Notes |
|---|---|---|
| ApplicationUser -> AuthorizedContact (as Principal) | 1:N | A user can authorize many delegates |
| ApplicationUser -> AuthorizedContact (as Delegate) | 1:N | A user can be delegate for many principals |
| AuthorizedContact: PrincipalId + DelegateId | Unique (when active) | Partial unique index where RevokedAt IS NULL |
| EventAttendance -> ApplicationUser (AssignedByUserId) | N:1 | Tracks who performed the assignment |
| TicketPurchase -> ApplicationUser (PurchasedForUserId) | N:1 | Tracks intended recipient at purchase time |

---

## 2. New Entity: AuthorizedContact

### 2.1 Entity Definition

**File**: `apps/api/Features/Participation/Entities/AuthorizedContact.cs`

```csharp
using System.ComponentModel.DataAnnotations;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Participation.Entities;

/// <summary>
/// Represents a delegation authorization between two users.
///
/// BUSINESS PURPOSE:
/// The Principal (person being represented) grants the Delegate (person acting
/// on their behalf) the ability to purchase tickets and create RSVPs for them.
///
/// AUTHORIZATION DIRECTION:
/// Principal → Delegate: "I authorize this person to act on my behalf"
///
/// LIFECYCLE:
/// - Created when Principal adds Delegate via Profile Settings > Authorized Contacts
/// - Active while RevokedAt is NULL
/// - Soft-deleted (RevokedAt set) when Principal removes the contact
/// - Revocation does NOT affect existing tickets/RSVPs (BR-005)
///
/// CONSTRAINTS:
/// - PrincipalId != DelegateId (self-authorization blocked, BR-003)
/// - Unique active relationship per PrincipalId + DelegateId pair
/// - Mutual authorization allowed (BR-004): A→B and B→A are separate records
/// </summary>
public class AuthorizedContact
{
    /// <summary>
    /// Unique identifier for the authorization record
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The user who grants authorization (the person being represented)
    /// </summary>
    [Required]
    public Guid PrincipalId { get; set; }

    /// <summary>
    /// The user who receives authorization (the person who can act on behalf)
    /// </summary>
    [Required]
    public Guid DelegateId { get; set; }

    /// <summary>
    /// When the authorization was created (UTC)
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the authorization was last updated (UTC)
    /// </summary>
    [Required]
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// When the authorization was revoked (UTC). NULL = active.
    /// Soft delete for audit trail preservation (UC-002).
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// Optional reason for revocation (e.g., "Removed by principal")
    /// </summary>
    public string? RevokedReason { get; set; }

    // Navigation Properties

    /// <summary>
    /// Navigation property to the Principal (person being represented)
    /// </summary>
    public ApplicationUser Principal { get; set; } = null!;

    /// <summary>
    /// Navigation property to the Delegate (person who can act on behalf)
    /// </summary>
    public ApplicationUser Delegate { get; set; } = null!;

    /// <summary>
    /// Whether this authorization is currently active
    /// </summary>
    public bool IsActive => RevokedAt == null;

    /// <summary>
    /// Constructor initializes required fields with proper UTC handling
    /// </summary>
    public AuthorizedContact()
    {
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Constructor for creating a new authorization
    /// </summary>
    public AuthorizedContact(Guid principalId, Guid delegateId) : this()
    {
        if (principalId == delegateId)
            throw new InvalidOperationException("Cannot authorize yourself as a contact (BR-003)");

        PrincipalId = principalId;
        DelegateId = delegateId;
    }

    /// <summary>
    /// Revokes this authorization
    /// </summary>
    public void Revoke(string? reason = null)
    {
        if (RevokedAt != null)
            throw new InvalidOperationException("Authorization is already revoked");

        RevokedAt = DateTime.UtcNow;
        RevokedReason = reason;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

### 2.2 EF Core Configuration

**File**: `apps/api/Features/Participation/Configuration/AuthorizedContactConfiguration.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WitchCityRope.Api.Features.Participation.Entities;

namespace WitchCityRope.Api.Features.Participation.Configuration;

/// <summary>
/// Entity Framework configuration for AuthorizedContact entity.
/// Implements soft-delete pattern via RevokedAt and partial unique index.
/// </summary>
public class AuthorizedContactConfiguration : IEntityTypeConfiguration<AuthorizedContact>
{
    public void Configure(EntityTypeBuilder<AuthorizedContact> builder)
    {
        // Table mapping
        builder.ToTable("AuthorizedContacts", "public");
        builder.HasKey(ac => ac.Id);

        // Property configurations
        builder.Property(ac => ac.Id)
               .ValueGeneratedOnAdd();

        builder.Property(ac => ac.PrincipalId)
               .IsRequired();

        builder.Property(ac => ac.DelegateId)
               .IsRequired();

        // UTC DateTime handling for PostgreSQL
        builder.Property(ac => ac.CreatedAt)
               .IsRequired()
               .HasColumnType("timestamptz");

        builder.Property(ac => ac.UpdatedAt)
               .IsRequired()
               .HasColumnType("timestamptz");

        builder.Property(ac => ac.RevokedAt)
               .HasColumnType("timestamptz");

        builder.Property(ac => ac.RevokedReason)
               .HasMaxLength(500);

        // Ignore computed property
        builder.Ignore(ac => ac.IsActive);

        // Foreign key relationships
        builder.HasOne(ac => ac.Principal)
               .WithMany()
               .HasForeignKey(ac => ac.PrincipalId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ac => ac.Delegate)
               .WithMany()
               .HasForeignKey(ac => ac.DelegateId)
               .OnDelete(DeleteBehavior.Cascade);

        // Partial unique constraint: one ACTIVE authorization per Principal+Delegate pair
        // Allows re-authorization after revocation (revoked records are not constrained)
        // BR-003: Self-authorization blocked by CHECK constraint below
        // BR-004: Mutual authorization allowed (A→B and B→A are separate records)
        builder.HasIndex(ac => new { ac.PrincipalId, ac.DelegateId })
               .IsUnique()
               .HasDatabaseName("UQ_AuthorizedContacts_Principal_Delegate_Active")
               .HasFilter("\"RevokedAt\" IS NULL");

        // Indexes for querying
        // "Who can act on my behalf?" (Principal's view)
        builder.HasIndex(ac => ac.PrincipalId)
               .HasDatabaseName("IX_AuthorizedContacts_PrincipalId");

        // "Who have I been authorized to act for?" (Delegate's view)
        builder.HasIndex(ac => ac.DelegateId)
               .HasDatabaseName("IX_AuthorizedContacts_DelegateId");

        // Composite index for common query: active authorizations for a delegate
        // Used when delegate is buying tickets/RSVPing - need to find their principals
        builder.HasIndex(ac => new { ac.DelegateId, ac.RevokedAt })
               .HasDatabaseName("IX_AuthorizedContacts_DelegateId_RevokedAt");

        // Business rule constraints
        // BR-003: Self-authorization blocked
        builder.ToTable(t => t.HasCheckConstraint(
            "CHK_AuthorizedContacts_NoSelfAuthorization",
            "\"PrincipalId\" != \"DelegateId\""));
    }
}
```

### 2.3 Design Decisions for AuthorizedContact

**Why no "accepted" state for the delegate?**
Per the requirements discussion, the notification to the delegate is informational, not a consent requirement. The Principal is granting permission for the Delegate to act; the Delegate does not need to consent to receiving this ability. This simplifies the model and UX. The Delegate simply sees the authorization appear on their profile.

**Why soft delete (RevokedAt) instead of hard delete?**
- Preserves audit trail for accountability
- Allows querying historical authorization relationships
- Supports re-authorization after revocation (new record, or re-activate existing)
- Consistent with project-wide soft delete patterns

**Why not a separate AuthorizationHistory table?**
The AuthorizedContact record itself serves as the audit trail with CreatedAt/RevokedAt timestamps. A separate history table would be overengineering for this simple lifecycle (created -> optionally revoked). If more complex state transitions are needed in the future, a history table can be added.

---

## 3. Modifications to Existing Entities

### 3.1 AttendanceStatus Enum - Add PendingAcceptance

**File**: `apps/api/Features/Participation/Entities/AttendanceStatus.cs`

**Change**: Add `PendingAcceptance = 6`

```csharp
namespace WitchCityRope.Api.Features.Participation.Entities;

public enum AttendanceStatus
{
    Active = 1,
    Cancelled = 2,
    Refunded = 3,
    Waitlisted = 4,
    PendingPayment = 5,

    /// <summary>
    /// Ticket or RSVP has been assigned to a user but they have not yet
    /// accepted (signed waiver + ToS). Transitions to Active on acceptance
    /// or returns to assignable state on decline.
    /// Created by: Ticket assignment or proxy RSVP
    /// Transitions to: Active (on acceptance), Cancelled (on decline/expiry)
    /// </summary>
    PendingAcceptance = 6
}
```

**Impact**: The check constraint `CHK_EventAttendances_Status` currently validates `"Status" IN (1, 2, 3, 4, 5)`. This MUST be updated to include 6.

### 3.2 TicketType - Add MaxQuantityPerPurchase

**File**: `apps/api/Models/TicketType.cs`

**Change**: Add `MaxQuantityPerPurchase` property

```csharp
/// <summary>
/// Maximum number of tickets of this type that can be purchased in a single
/// transaction. Configurable per ticket type per event (AD-006).
/// Default: 3 (covers "me + partner + friend" scenario)
/// Checkout quantity selector ranges from 1 to this value.
/// </summary>
[Required]
public int MaxQuantityPerPurchase { get; set; } = 3;
```

**EF Configuration addition** (in ApplicationDbContext.cs, TicketType section):

```csharp
entity.Property(t => t.MaxQuantityPerPurchase)
      .IsRequired()
      .HasDefaultValue(3);

// Check constraint: must be at least 1
entity.ToTable(t => t.HasCheckConstraint(
    "CHK_TicketTypes_MaxQuantityPerPurchase",
    "\"MaxQuantityPerPurchase\" >= 1 AND \"MaxQuantityPerPurchase\" <= 10"));
```

**Upper bound rationale**: 10 is a reasonable maximum for any community event. This prevents accidental or malicious bulk purchases while being generous enough for any realistic scenario.

### 3.3 EventAttendance - Add Assignment Fields

**File**: `apps/api/Features/Participation/Entities/EventAttendance.cs`

**New Properties**:

```csharp
/// <summary>
/// User who assigned this ticket/RSVP to the attendee.
/// NULL for self-purchased tickets and self-RSVPs.
/// Set when a delegate assigns a ticket or creates a proxy RSVP.
/// Also set for admin assignments (BR-040, BR-042).
/// </summary>
public Guid? AssignedByUserId { get; set; }

/// <summary>
/// When the ticket/RSVP was assigned to the current user (UTC).
/// NULL for self-purchased tickets and self-RSVPs.
/// </summary>
public DateTime? AssignedAt { get; set; }

/// <summary>
/// When the assigned user accepted the ticket/RSVP (UTC).
/// NULL if not yet accepted or not an assigned ticket.
/// Set when Status transitions PendingAcceptance -> Active via acceptance flow.
/// </summary>
public DateTime? AcceptedAt { get; set; }

/// <summary>
/// When the assigned user declined the ticket/RSVP (UTC).
/// NULL if not declined.
/// Set when assignee explicitly declines.
/// </summary>
public DateTime? DeclinedAt { get; set; }

/// <summary>
/// Reason the assignee declined the ticket/RSVP.
/// Optional free-text field.
/// </summary>
public string? DeclinedReason { get; set; }

/// <summary>
/// When the reminder email was sent for this pending assignment (UTC).
/// NULL if no reminder sent yet.
/// Used to prevent duplicate reminder emails (BR-062: one reminder per assignment).
/// </summary>
public DateTime? ReminderSentAt { get; set; }

// Navigation property for the user who assigned this ticket/RSVP
/// <summary>
/// Navigation property to the user who performed the assignment.
/// NULL for self-purchased tickets.
/// </summary>
public ApplicationUser? AssignedByUser { get; set; }
```

**Why no `OriginalUserId` field?**
The `AttendanceHistory` table already captures the full reassignment history with `OldValues`/`NewValues` JSONB fields. Adding `OriginalUserId` to EventAttendance would duplicate information that is already tracked in the audit trail. The `AssignedByUserId` field tracks the current/most recent assigner, which is sufficient for display purposes. Historical data is available via `AttendanceHistory`.

### 3.4 TicketPurchase - Add PurchasedForUserId

**File**: `apps/api/Models/TicketPurchase.cs`

**New Property**:

```csharp
/// <summary>
/// User the ticket was intended for when purchased, if different from the purchaser.
/// NULL if the purchaser bought the ticket for themselves.
/// This is an informational field for the checkout flow - the actual attendee
/// is tracked via EventAttendance.UserId.
///
/// NOTE: The sliding scale percentage on the TicketPurchase applies uniformly
/// to all tickets in the purchase (AD-012). All EventAttendance records
/// linked to this TicketPurchase share the same pricing.
/// </summary>
public Guid? PurchasedForUserId { get; set; }

/// <summary>
/// Navigation property to the intended recipient
/// </summary>
public ApplicationUser? PurchasedForUser { get; set; }
```

**Design Note on Sliding Scale (AD-012)**: The `SlidingScalePercentage` field already exists on `TicketPurchase` and applies to the total purchase. When a purchaser buys 3 tickets at 25% sliding scale, the `TotalPrice` reflects all 3 tickets at that discount. Each `EventAttendance` record linked to this `TicketPurchase` inherits the same pricing. No additional fields are needed for sliding scale support.

**Design Note on Multi-Ticket Purchases**: The existing `TicketPurchase.Quantity` field already supports quantities > 1. Currently it is always 1, but the field and column exist. When quantity > 1, multiple `EventAttendance` records will be created pointing to the same `TicketPurchaseId`. This is already documented in the existing code comments ("FUTURE: One TicketPurchase can create multiple EventAttendance records when quantity > 1").

---

## 4. EF Core Configuration Changes

### 4.1 EventAttendanceConfiguration Updates

The following changes apply to the existing `EventAttendanceConfiguration.cs`:

```csharp
// === NEW PROPERTY CONFIGURATIONS ===

// Assignment tracking fields
builder.Property(e => e.AssignedByUserId)
       .IsRequired(false);

builder.Property(e => e.AssignedAt)
       .HasColumnType("timestamptz");

builder.Property(e => e.AcceptedAt)
       .HasColumnType("timestamptz");

builder.Property(e => e.DeclinedAt)
       .HasColumnType("timestamptz");

builder.Property(e => e.DeclinedReason)
       .HasMaxLength(1000);

builder.Property(e => e.ReminderSentAt)
       .HasColumnType("timestamptz");

// === NEW RELATIONSHIP ===

builder.HasOne(e => e.AssignedByUser)
       .WithMany()
       .HasForeignKey(e => e.AssignedByUserId)
       .OnDelete(DeleteBehavior.SetNull);

// === UPDATED CHECK CONSTRAINT ===
// Old: "\"Status\" IN (1, 2, 3, 4, 5)"
// New: Include PendingAcceptance = 6
builder.ToTable(t => t.HasCheckConstraint(
    "CHK_EventAttendances_Status",
    "\"Status\" IN (1, 2, 3, 4, 5, 6)"));

// === UPDATED CancelledAt LOGIC CONSTRAINT ===
// Old: Only checked Status IN (2, 3) for CancelledAt
// No change needed - PendingAcceptance (6) correctly has CancelledAt IS NULL

// === UPDATED UNIQUE CONSTRAINT ===
// Old: Filter was "\"Status\" = 1" (Active only)
// New: Include PendingAcceptance = 6 to prevent double-assignment
// EC-007: Race condition prevention - two delegates assigning to same principal
builder.HasIndex(e => new { e.UserId, e.EventId, e.AttendanceType, e.SessionId })
       .IsUnique()
       .HasDatabaseName("UQ_EventAttendances_User_Event_Type_Session_Active")
       .HasFilter("\"Status\" IN (1, 6)");  // Active OR PendingAcceptance

// === NEW INDEXES ===

// Index for querying a user's pending acceptances (dashboard view)
builder.HasIndex(e => new { e.UserId, e.Status })
       .HasDatabaseName("IX_EventAttendances_UserId_Status");
// NOTE: This index already exists, so no migration needed for it

// Index for the reminder email job: find PendingAcceptance records
// that haven't received a reminder yet
builder.HasIndex(e => new { e.Status, e.ReminderSentAt })
       .HasDatabaseName("IX_EventAttendances_Status_ReminderSentAt")
       .HasFilter("\"Status\" = 6 AND \"ReminderSentAt\" IS NULL");

// Index for assigned tickets lookup (delegate's view of assignments they've made)
builder.HasIndex(e => e.AssignedByUserId)
       .HasDatabaseName("IX_EventAttendances_AssignedByUserId")
       .HasFilter("\"AssignedByUserId\" IS NOT NULL");
```

### 4.2 TicketPurchase Configuration Updates

Add to the existing TicketPurchase configuration in `ApplicationDbContext.cs`:

```csharp
// PurchasedForUserId configuration
entity.Property(p => p.PurchasedForUserId)
      .IsRequired(false);

entity.HasOne(p => p.PurchasedForUser)
      .WithMany()
      .HasForeignKey(p => p.PurchasedForUserId)
      .OnDelete(DeleteBehavior.SetNull);

// Index for finding purchases made for a specific user
entity.HasIndex(p => p.PurchasedForUserId)
      .HasDatabaseName("IX_TicketPurchases_PurchasedForUserId")
      .HasFilter("\"PurchasedForUserId\" IS NOT NULL");
```

### 4.3 AttendanceHistoryConfiguration Updates

The existing check constraint on `ActionType` must be expanded:

```csharp
// Old:
// "\"ActionType\" IN ('Created', 'Updated', 'Cancelled', 'Refunded',
//  'StatusChanged', 'PaymentUpdated')"

// New: Add assignment-related action types
builder.ToTable(t => t.HasCheckConstraint(
    "CHK_AttendanceHistory_ActionType",
    "\"ActionType\" IN ('Created', 'Updated', 'Cancelled', 'Refunded', " +
    "'StatusChanged', 'PaymentUpdated', " +
    "'TicketAssigned', 'TicketReassigned', 'TicketAccepted', 'TicketDeclined', " +
    "'ProxyRSVPCreated', 'ProxyRSVPAccepted', 'ProxyRSVPDeclined', 'ReminderSent')"));
```

### 4.4 DbContext Registration

Add to `ApplicationDbContext.cs`:

```csharp
// New DbSet
public DbSet<AuthorizedContact> AuthorizedContacts { get; set; }

// In OnModelCreating, add:
modelBuilder.ApplyConfiguration(new AuthorizedContactConfiguration());
```

---

## 5. TicketType.Sold Property Update

The existing computed `Sold` property on `TicketType` counts only `Status == Active` attendances. With `PendingAcceptance`, these tickets have been purchased and reserved a spot - they MUST count toward sold/capacity.

**Current** (only counts Active):
```csharp
return Event.EventAttendances.Count(ea =>
    ea.Status == AttendanceStatus.Active &&
    ea.AttendanceType == AttendanceType.Ticket &&
    ea.TicketPurchase != null &&
    ea.TicketPurchase.TicketTypeId == Id);
```

**Updated** (counts Active + PendingAcceptance):
```csharp
return Event.EventAttendances.Count(ea =>
    (ea.Status == AttendanceStatus.Active ||
     ea.Status == AttendanceStatus.PendingAcceptance) &&
    ea.AttendanceType == AttendanceType.Ticket &&
    ea.TicketPurchase != null &&
    ea.TicketPurchase.TicketTypeId == Id);
```

This is critical for BR-013 (capacity check includes pending assignments) and BR-054 (proxy RSVPs count against capacity). Without this change, assigning a ticket would not decrement available capacity.

Similarly, any capacity check queries in `AttendanceService.cs` that count "reserved" spots must include `PendingAcceptance` alongside `Active` and `PendingPayment`.

---

## 6. Index Strategy

### 6.1 New Indexes Summary

| Index Name | Table | Columns | Type | Filter | Purpose |
|---|---|---|---|---|---|
| `UQ_AuthorizedContacts_Principal_Delegate_Active` | AuthorizedContacts | PrincipalId, DelegateId | Unique, Partial | `RevokedAt IS NULL` | Prevent duplicate active authorizations |
| `IX_AuthorizedContacts_PrincipalId` | AuthorizedContacts | PrincipalId | B-tree | None | Query "who can act on my behalf" |
| `IX_AuthorizedContacts_DelegateId` | AuthorizedContacts | DelegateId | B-tree | None | Query "who can I act for" |
| `IX_AuthorizedContacts_DelegateId_RevokedAt` | AuthorizedContacts | DelegateId, RevokedAt | B-tree | None | Find active authorizations for delegate |
| `IX_EventAttendances_Status_ReminderSentAt` | EventAttendances | Status, ReminderSentAt | Partial | `Status = 6 AND ReminderSentAt IS NULL` | Reminder email job efficiency |
| `IX_EventAttendances_AssignedByUserId` | EventAttendances | AssignedByUserId | Partial | `AssignedByUserId IS NOT NULL` | Delegate's assignment dashboard |
| `IX_TicketPurchases_PurchasedForUserId` | TicketPurchases | PurchasedForUserId | Partial | `PurchasedForUserId IS NOT NULL` | Query tickets purchased for others |

### 6.2 Modified Indexes

| Index Name | Table | Change | Reason |
|---|---|---|---|
| `UQ_EventAttendances_User_Event_Type_Session_Active` | EventAttendances | Filter `"Status" = 1` changed to `"Status" IN (1, 6)` | Prevent double-assignment (EC-007) |

### 6.3 Index Design Rationale

**Partial indexes** are used extensively because:
- Most EventAttendance records will NOT have assignment fields populated (existing self-purchased tickets)
- The reminder job index only cares about PendingAcceptance records without reminders
- Partial indexes save storage and improve query performance for the subset that matters

**The reminder job index** (`IX_EventAttendances_Status_ReminderSentAt`) is specifically designed for the daily scheduled job (UC-011) that queries:
```sql
SELECT * FROM "EventAttendances" ea
JOIN "Events" e ON ea."EventId" = e."Id"
WHERE ea."Status" = 6
  AND ea."ReminderSentAt" IS NULL
  AND e."StartDate" BETWEEN NOW() AND NOW() + INTERVAL '24 hours';
```
The partial index ensures this query uses an index scan over a very small subset of rows.

---

## 7. Constraint Summary

### 7.1 New Constraints

| Constraint | Table | Type | Expression | Business Rule |
|---|---|---|---|---|
| `CHK_AuthorizedContacts_NoSelfAuthorization` | AuthorizedContacts | CHECK | `PrincipalId != DelegateId` | BR-003 |
| `CHK_TicketTypes_MaxQuantityPerPurchase` | TicketTypes | CHECK | `MaxQuantityPerPurchase >= 1 AND MaxQuantityPerPurchase <= 10` | AD-006 |

### 7.2 Modified Constraints

| Constraint | Table | Old Value | New Value | Reason |
|---|---|---|---|---|
| `CHK_EventAttendances_Status` | EventAttendances | `IN (1,2,3,4,5)` | `IN (1,2,3,4,5,6)` | New PendingAcceptance status |
| `CHK_AttendanceHistory_ActionType` | AttendanceHistory | 6 action types | 14 action types | New assignment action types |

### 7.3 Constraint Design Notes

- The `CancelledAt` logic constraint does NOT need updating. PendingAcceptance (6) is correctly not in the set (2, 3) that requires `CancelledAt IS NOT NULL`, and status 6 correctly requires `CancelledAt IS NULL`.
- The unique constraint update from `Status = 1` to `Status IN (1, 6)` is critical for preventing the race condition described in EC-007 (two delegates assigning to the same principal simultaneously).

---

## 8. Migration Strategy

### 8.1 Migration Order

The changes should be implemented as **two migrations** to keep concerns separated and allow easier rollback:

**Migration 1: `AddAuthorizedContactsAndAssignmentFields`**
This is the primary schema migration containing all structural changes.

1. Create `AuthorizedContacts` table with all columns, constraints, and indexes
2. Add `MaxQuantityPerPurchase` column to `TicketTypes` (with default value 3)
3. Add assignment columns to `EventAttendances` (AssignedByUserId, AssignedAt, AcceptedAt, DeclinedAt, DeclinedReason, ReminderSentAt)
4. Add `PurchasedForUserId` column to `TicketPurchases`
5. Add new foreign key relationships
6. Add new indexes
7. Update `PendingAcceptance = 6` to status check constraint
8. Update unique constraint filter on EventAttendances
9. Update AttendanceHistory ActionType check constraint

**Migration 2: `AddTicketTypeMaxQuantityCheckConstraint`**
Only needed if the check constraint on MaxQuantityPerPurchase causes issues when combined with the default value in migration 1. In practice, these can likely be combined into Migration 1.

### 8.2 Data Compatibility

**Zero-impact on existing data**: All new columns are nullable (except `MaxQuantityPerPurchase` which has a default value of 3). Existing records are fully compatible:

| Change | Impact on Existing Data |
|---|---|
| New `AuthorizedContacts` table | None - new table, no existing data |
| `MaxQuantityPerPurchase` on TicketTypes | DEFAULT 3 applied to all existing ticket types |
| Assignment fields on EventAttendance | All NULL for existing records (self-purchased) |
| `PurchasedForUserId` on TicketPurchase | NULL for all existing records (self-purchased) |
| Updated status check constraint | Expanding the allowed set - no existing violations |
| Updated unique constraint filter | Expanding from `= 1` to `IN (1, 6)` - no existing violations since no records have status 6 |
| Updated ActionType check constraint | Expanding the allowed set - no existing violations |

### 8.3 Rollback Strategy

Both migrations are additive (adding columns, adding a table, expanding constraints). The `Down()` methods should:

1. Drop the `AuthorizedContacts` table
2. Remove new columns from `EventAttendances`
3. Remove new column from `TicketPurchases`
4. Remove new column from `TicketTypes`
5. Revert check constraints to original values
6. Revert unique constraint filter to original value
7. Drop new indexes

### 8.4 Migration Generation Command

```bash
cd /home/chad/repos/witchcityrope/apps/api
dotnet ef migrations add AddAuthorizedContactsAndAssignmentFields
```

---

## 9. Impact Analysis on Existing Queries/Services

### 9.1 AttendanceService.cs (2011 lines)

This is the largest impact area. The following service methods need review:

| Method Area | Impact | Required Change |
|---|---|---|
| Capacity calculation queries | HIGH | Include `PendingAcceptance` in reserved count alongside `Active` and `PendingPayment` |
| Duplicate attendance check | HIGH | Existing queries check `Status == Active` for duplicate prevention. Must also check `PendingAcceptance` |
| RSVP creation | MEDIUM | Add proxy RSVP path with `PendingAcceptance` status |
| Ticket creation (checkout flow) | HIGH | Support quantity > 1, assignment at checkout |
| Cancellation logic | MEDIUM | `CanBeCancelled()` method on EventAttendance should also return true for `PendingAcceptance` |
| Attendance history logging | LOW | Add new ActionType values when creating history records |

### 9.2 Checkout Endpoints

| Endpoint | Impact | Required Change |
|---|---|---|
| `CheckoutEndpoints.cs` (Credit Card) | HIGH | Support multi-ticket purchases, per-ticket assignment |
| `PayPalCheckoutController.cs` | HIGH | Same changes as CC checkout |
| `KioskPaymentEndpoints.cs` | LOW | Door purchases typically single-ticket, but should support new status |

### 9.3 TicketType.Sold Computed Property

**Critical**: As described in Section 5, the `Sold` getter must include `PendingAcceptance` in its count. This is not a database change but a code change that must ship with the migration.

### 9.4 Vetting Access Control

| Method | Impact | Required Change |
|---|---|---|
| `CanUserRsvpAsync()` | LOW | No change needed - called per-user |
| `CanUserPurchaseTicketAsync()` | LOW | No change needed - called per-user |
| New: Check assignee's vetting | MEDIUM | Need new method or parameter to check assignee vetting for VettedMembersOnly events at assignment time AND acceptance time (AD-014) |

### 9.5 Email System

New email templates required (no database changes needed for templates themselves, but the trigger logic references the new `ReminderSentAt` field):

| Template | Trigger |
|---|---|
| Ticket Assignment Notification | On ticket assignment (PendingAcceptance) |
| RSVP Proxy Notification | On proxy RSVP creation (PendingAcceptance) |
| Reminder (shared template) | Scheduled job, 24h before event, where `ReminderSentAt IS NULL` |
| Ticket Declined Notification | On assignee decline (to delegate) |

---

## 10. SQL DDL Reference

For reference, the raw SQL that the migration should generate:

```sql
-- 1. Create AuthorizedContacts table
CREATE TABLE "public"."AuthorizedContacts" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "PrincipalId" uuid NOT NULL,
    "DelegateId" uuid NOT NULL,
    "CreatedAt" timestamptz NOT NULL,
    "UpdatedAt" timestamptz NOT NULL,
    "RevokedAt" timestamptz NULL,
    "RevokedReason" varchar(500) NULL,
    CONSTRAINT "PK_AuthorizedContacts" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AuthorizedContacts_Users_PrincipalId"
        FOREIGN KEY ("PrincipalId") REFERENCES "public"."Users"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_AuthorizedContacts_Users_DelegateId"
        FOREIGN KEY ("DelegateId") REFERENCES "public"."Users"("Id") ON DELETE CASCADE,
    CONSTRAINT "CHK_AuthorizedContacts_NoSelfAuthorization"
        CHECK ("PrincipalId" != "DelegateId")
);

-- AuthorizedContacts indexes
CREATE UNIQUE INDEX "UQ_AuthorizedContacts_Principal_Delegate_Active"
    ON "public"."AuthorizedContacts" ("PrincipalId", "DelegateId")
    WHERE "RevokedAt" IS NULL;

CREATE INDEX "IX_AuthorizedContacts_PrincipalId"
    ON "public"."AuthorizedContacts" ("PrincipalId");

CREATE INDEX "IX_AuthorizedContacts_DelegateId"
    ON "public"."AuthorizedContacts" ("DelegateId");

CREATE INDEX "IX_AuthorizedContacts_DelegateId_RevokedAt"
    ON "public"."AuthorizedContacts" ("DelegateId", "RevokedAt");

-- 2. Add MaxQuantityPerPurchase to TicketTypes
ALTER TABLE "public"."TicketTypes"
    ADD COLUMN "MaxQuantityPerPurchase" integer NOT NULL DEFAULT 3;

ALTER TABLE "public"."TicketTypes"
    ADD CONSTRAINT "CHK_TicketTypes_MaxQuantityPerPurchase"
    CHECK ("MaxQuantityPerPurchase" >= 1 AND "MaxQuantityPerPurchase" <= 10);

-- 3. Add assignment fields to EventAttendances
ALTER TABLE "public"."EventAttendances"
    ADD COLUMN "AssignedByUserId" uuid NULL,
    ADD COLUMN "AssignedAt" timestamptz NULL,
    ADD COLUMN "AcceptedAt" timestamptz NULL,
    ADD COLUMN "DeclinedAt" timestamptz NULL,
    ADD COLUMN "DeclinedReason" varchar(1000) NULL,
    ADD COLUMN "ReminderSentAt" timestamptz NULL;

ALTER TABLE "public"."EventAttendances"
    ADD CONSTRAINT "FK_EventAttendances_Users_AssignedByUserId"
    FOREIGN KEY ("AssignedByUserId") REFERENCES "public"."Users"("Id")
    ON DELETE SET NULL;

-- 4. Add PurchasedForUserId to TicketPurchases
ALTER TABLE "public"."TicketPurchases"
    ADD COLUMN "PurchasedForUserId" uuid NULL;

ALTER TABLE "public"."TicketPurchases"
    ADD CONSTRAINT "FK_TicketPurchases_Users_PurchasedForUserId"
    FOREIGN KEY ("PurchasedForUserId") REFERENCES "public"."Users"("Id")
    ON DELETE SET NULL;

-- 5. Update check constraints
ALTER TABLE "public"."EventAttendances"
    DROP CONSTRAINT "CHK_EventAttendances_Status";

ALTER TABLE "public"."EventAttendances"
    ADD CONSTRAINT "CHK_EventAttendances_Status"
    CHECK ("Status" IN (1, 2, 3, 4, 5, 6));

ALTER TABLE "public"."AttendanceHistory"
    DROP CONSTRAINT "CHK_AttendanceHistory_ActionType";

ALTER TABLE "public"."AttendanceHistory"
    ADD CONSTRAINT "CHK_AttendanceHistory_ActionType"
    CHECK ("ActionType" IN (
        'Created', 'Updated', 'Cancelled', 'Refunded',
        'StatusChanged', 'PaymentUpdated',
        'TicketAssigned', 'TicketReassigned', 'TicketAccepted', 'TicketDeclined',
        'ProxyRSVPCreated', 'ProxyRSVPAccepted', 'ProxyRSVPDeclined', 'ReminderSent'));

-- 6. Update unique constraint on EventAttendances
DROP INDEX "public"."UQ_EventAttendances_User_Event_Type_Session_Active";

CREATE UNIQUE INDEX "UQ_EventAttendances_User_Event_Type_Session_Active"
    ON "public"."EventAttendances" ("UserId", "EventId", "AttendanceType", "SessionId")
    WHERE "Status" IN (1, 6);

-- 7. New indexes
CREATE INDEX "IX_EventAttendances_Status_ReminderSentAt"
    ON "public"."EventAttendances" ("Status", "ReminderSentAt")
    WHERE "Status" = 6 AND "ReminderSentAt" IS NULL;

CREATE INDEX "IX_EventAttendances_AssignedByUserId"
    ON "public"."EventAttendances" ("AssignedByUserId")
    WHERE "AssignedByUserId" IS NOT NULL;

CREATE INDEX "IX_TicketPurchases_PurchasedForUserId"
    ON "public"."TicketPurchases" ("PurchasedForUserId")
    WHERE "PurchasedForUserId" IS NOT NULL;
```

---

## 11. Business Rule to Schema Mapping

This section maps each business rule to the schema element that enforces it.

| Business Rule | Enforcement Layer | Schema Element |
|---|---|---|
| BR-001: Authorization direction | Application | AuthorizedContact entity (PrincipalId = granter, DelegateId = receiver) |
| BR-002: Single authorization level | Design | One AuthorizedContact covers both tickets and RSVPs (no permission column) |
| BR-003: Self-authorization blocked | Database | `CHK_AuthorizedContacts_NoSelfAuthorization` |
| BR-004: Mutual authorization | Design | PrincipalId+DelegateId unique index allows A→B and B→A as separate rows |
| BR-005: Revocation preserves existing | Application | `RevokedAt` soft delete; revocation does not cascade to EventAttendance |
| BR-006: Account required | Database | Foreign keys to Users table (non-nullable PrincipalId, DelegateId) |
| BR-010: Configurable max quantity | Database | `TicketType.MaxQuantityPerPurchase` with CHECK constraint |
| BR-012: One ticket per assignee per event | Database | `UQ_EventAttendances_User_Event_Type_Session_Active` with filter `IN (1, 6)` |
| BR-022: Assigned enters PendingAcceptance | Application | `AttendanceStatus.PendingAcceptance = 6` |
| BR-024: Irrevocable once accepted | Application | Status=Active + waiver accepted = no reclaim path |
| BR-027: Reassignment audit trail | Database | AttendanceHistory with 'TicketAssigned'/'TicketReassigned' ActionTypes |
| BR-040: Admin bypasses contacts | Application | Admin assignment endpoint does not check AuthorizedContacts |
| BR-042: Admin audit trail | Database | EventAttendance.AssignedByUserId set to admin's user ID |
| BR-062: One reminder per assignment | Database | `EventAttendance.ReminderSentAt` field + partial index |
| EC-007: Race condition prevention | Database | Unique constraint includes PendingAcceptance status |

---

## 12. Quality Checklist

- [x] Normalized appropriately (AuthorizedContacts is its own table, assignment data lives on EventAttendance)
- [x] Constraints enforced at database level (CHECK, UNIQUE, FK)
- [x] Indexes optimized (partial indexes for sparse data, composite indexes for common queries)
- [x] Migration strategy defined (single additive migration, zero data impact)
- [x] EF Core configurations specified (complete property + relationship + index configs)
- [x] Impact analysis on existing queries documented
- [x] Business rules mapped to schema elements
- [x] UTC timestamps used throughout (timestamptz)
- [x] Soft delete pattern for AuthorizedContacts (RevokedAt)
- [x] Audit trail via existing AttendanceHistory (expanded ActionTypes)
- [x] Race condition prevention (unique constraint includes PendingAcceptance)
- [x] Backward compatible (all new columns nullable or with defaults)

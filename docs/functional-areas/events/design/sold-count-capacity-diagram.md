# Sold Count & Capacity System - Visual Diagrams

**Date**: 2025-11-08
**Related**: sold-count-capacity-redesign-2025-11-08.md

---

## Entity Relationship Diagram (Before vs After)

### BEFORE (Current - Broken)

```
┌─────────────────────────┐
│   Event                 │
│─────────────────────────│
│ Id                      │
│ Capacity                │
└─────────┬───────────────┘
          │
          │ 1:N
          ▼
┌─────────────────────────┐         ┌─────────────────────────┐
│   EventParticipation    │         │   TicketPurchase        │
│─────────────────────────│         │─────────────────────────│
│ Id                      │    ❌   │ Id                      │
│ EventId                 │  No FK  │ TicketTypeId            │
│ UserId                  │ ◀─────▶ │ UserId                  │
│ ParticipationType       │         │ Quantity                │
│ Status (Active/Cancel)  │         │ PaymentStatus           │
│ TicketPurchaseId ❌NULL│         │ TotalPrice              │
└─────────────────────────┘         └─────────────────────────┘
                                              ▲
                                              │ N:1
                                              │
                                    ┌─────────┴───────────────┐
                                    │   TicketType            │
                                    │─────────────────────────│
                                    │ Id                      │
                                    │ EventId                 │
                                    │ Available               │
                                    │ Sold ❌ (STALE!)        │
                                    └─────────────────────────┘

❌ PROBLEMS:
- No foreign key link between EventParticipation and TicketPurchase
- TicketType.Sold stored but never updated
- Matching by UserId is implicit, fragile
- No unique constraint on active participations
```

### AFTER (Proposed - Self-Healing)

```
┌─────────────────────────┐
│   Event                 │
│─────────────────────────│
│ Id                      │
│ Capacity                │
└─────────┬───────────────┘
          │
          │ 1:N
          ▼
┌─────────────────────────┐         ┌─────────────────────────┐
│   EventParticipation    │         │   TicketPurchase        │
│─────────────────────────│         │─────────────────────────│
│ Id                      │    ✅   │ Id                      │
│ EventId                 │   FK    │ TicketTypeId            │
│ UserId                  │ ◀───────│ UserId                  │
│ ParticipationType       │         │ Quantity                │
│ Status (Active/Cancel)  │         │ PaymentStatus           │
│ TicketPurchaseId ✅     │────┐    │ TotalPrice              │
└─────────┬───────────────┘    │    └─────────────────────────┘
          │                     │              ▲
          │ Unique Index:       └──────────────┘
          │ (EventId, UserId,                  │ N:1
          │  ParticipationType)                │
          │ WHERE Status=Active    ┌───────────┴───────────────┐
          │                        │   TicketType              │
          └────────────────────────│───────────────────────────│
                                   │ Id                        │
                                   │ EventId                   │
                                   │ Available                 │
                                   │ Sold ✅ [NotMapped]       │
                                   │   get { COUNT(Partic...) }│
                                   │ IsMultiSession ✅         │
                                   └───────────────────────────┘

✅ SOLUTIONS:
- Explicit foreign key: EventParticipation.TicketPurchaseId → TicketPurchase.Id
- TicketType.Sold is calculated property (not stored)
- Unique constraint prevents duplicate active participations
- Self-healing: always accurate from actual participation records
```

---

## Data Flow: Purchase → Sold Count

### Current Flow (Broken)

```
User Purchases Ticket
         │
         ▼
┌────────────────────────────────────────────────┐
│ ParticipationService.CreateTicketPurchaseAsync │
└────────┬───────────────────────────────────────┘
         │
         ├─► Create TicketPurchase (PaymentStatus=Completed)
         │
         ├─► Create EventParticipation (Status=Active)
         │   ❌ TicketPurchaseId NOT SET!
         │
         ├─► ❌ TicketType.Sold NOT UPDATED!
         │
         └─► SaveChangesAsync()
                  │
                  ▼
         Database has stale Sold count
                  │
                  ▼
         Frontend API call triggers calculation
                  │
                  ▼
         TicketTypeDto constructor:
         - Matches Participations by UserId ❌ Fragile!
         - Counts unique users
                  │
                  ▼
         Frontend displays correct count ✅
         But database is WRONG ❌
```

### Proposed Flow (Self-Healing)

```
User Purchases Ticket
         │
         ▼
┌────────────────────────────────────────────────┐
│ ParticipationService.CreateTicketPurchaseAsync │
└────────┬───────────────────────────────────────┘
         │
         ├─► Create TicketPurchase (PaymentStatus=Completed)
         │   purchaseId = purchase.Id
         │
         ├─► Create EventParticipation (Status=Active)
         │   ✅ participation.TicketPurchaseId = purchaseId
         │
         ├─► ✅ NO manual Sold update needed!
         │
         └─► SaveChangesAsync()
                  │
                  ▼
         Database has correct foreign key link
                  │
                  ▼
         Frontend API call triggers calculation
                  │
                  ▼
         TicketTypeDto constructor:
         - Uses TicketPurchaseId foreign key ✅ Reliable!
         - Counts active participations
                  │
                  ▼
         Frontend displays correct count ✅
         Database relationship is CORRECT ✅

         ┌──────────────────────────────────┐
         │ BONUS: TicketType.Sold property  │
         │ calculates on-demand:             │
         │ COUNT(EventParticipations         │
         │   WHERE Status=Active             │
         │   AND TicketPurchaseId IS NOT NULL│
         │   AND TicketPurchase.TicketTypeId │
         │       = this.Id)                  │
         └──────────────────────────────────┘
```

---

## Cancellation Flow: Self-Healing Recalculation

### Before (Manual Decrement - Broken)

```
User Cancels Participation
         │
         ▼
┌────────────────────────────────────────┐
│ ParticipationService.CancelAsync       │
└────────┬───────────────────────────────┘
         │
         ├─► Find EventParticipation (Status=Active)
         │
         ├─► participation.Cancel(reason)
         │   Sets Status = Cancelled
         │
         ├─► ❌ FORGOT to decrement TicketType.Sold!
         │
         └─► SaveChangesAsync()
                  │
                  ▼
         Database: Participation.Status = Cancelled
         Database: TicketType.Sold UNCHANGED ❌ BUG!
                  │
                  ▼
         Frontend API call
                  │
                  ▼
         TicketTypeDto constructor:
         - Filters to Active participations ✅
         - Excludes cancelled automatically ✅
                  │
                  ▼
         Frontend shows correct count ✅
         But database.Sold is WRONG ❌
```

### After (Automatic Recalculation - Self-Healing)

```
User Cancels Participation
         │
         ▼
┌────────────────────────────────────────┐
│ ParticipationService.CancelAsync       │
└────────┬───────────────────────────────┘
         │
         ├─► Find EventParticipation (Status=Active)
         │
         ├─► participation.Cancel(reason)
         │   Sets Status = Cancelled
         │
         ├─► ✅ NO manual decrement needed!
         │
         └─► SaveChangesAsync()
                  │
                  ▼
         Database: Participation.Status = Cancelled
         Database: No Sold column to become stale ✅
                  │
                  ▼
         Frontend API call
                  │
                  ▼
         TicketType.Sold property getter:
         - COUNT(EventParticipations WHERE Status=Active)
         - Automatically excludes cancelled ✅
                  │
                  ▼
         TicketTypeDto constructor:
         - Uses TicketPurchaseId foreign key
         - Filters to Active participations
                  │
                  ▼
         Frontend shows correct count ✅
         Database relationship is CORRECT ✅

         ┌──────────────────────────────────┐
         │ SELF-HEALING:                     │
         │ Even if cancellation logic changes│
         │ or new cancel method added,       │
         │ sold count ALWAYS recalculates    │
         │ correctly on next API call!       │
         └──────────────────────────────────┘
```

---

## Future: Multi-Ticket Purchase Support

### Phase 2: One Purchase → Multiple Participations

```
User Buys 3 Tickets
         │
         ▼
┌────────────────────────────────────────────────┐
│ ParticipationService.CreateMultiTicketPurchase │
└────────┬───────────────────────────────────────┘
         │
         ├─► Create 1 TicketPurchase (Quantity=3)
         │   purchaseId = purchase.Id
         │
         ├─► Create 3 EventParticipations:
         │   ┌─────────────────────────────────┐
         │   │ Participation 1 (Status=Active) │
         │   │ TicketPurchaseId = purchaseId   │
         │   │ UserId = buyer's friend 1       │
         │   └─────────────────────────────────┘
         │   ┌─────────────────────────────────┐
         │   │ Participation 2 (Status=Active) │
         │   │ TicketPurchaseId = purchaseId   │
         │   │ UserId = buyer's friend 2       │
         │   └─────────────────────────────────┘
         │   ┌─────────────────────────────────┐
         │   │ Participation 3 (Status=Active) │
         │   │ TicketPurchaseId = purchaseId   │
         │   │ UserId = buyer (self)           │
         │   └─────────────────────────────────┘
         │
         └─► SaveChangesAsync()
                  │
                  ▼
         Database: 1 TicketPurchase, 3 EventParticipations
                  │
                  ▼
         TicketType.Sold calculation:
         COUNT(EventParticipations WHERE Status=Active
               AND TicketPurchase.TicketTypeId = this.Id)
         = 3 ✅ CORRECT!
                  │
                  ▼
         Capacity consumed: 3 seats ✅
         No code changes needed! ✅

┌──────────────────────────────────────────────────┐
│ FUTURE-PROOF DESIGN:                             │
│ Current design ALREADY supports multi-ticket!    │
│ Just need to:                                     │
│ 1. Allow Quantity > 1 in purchase request        │
│ 2. Create multiple EventParticipations           │
│ 3. Link all to same TicketPurchaseId             │
│ 4. Sold count auto-calculates correctly!         │
└──────────────────────────────────────────────────┘
```

---

## Unique Constraint: Prevents Duplicates

### How Partial Unique Index Works

```sql
CREATE UNIQUE INDEX "IX_EventParticipations_OneActivePerUserPerEvent"
ON "EventParticipations" ("EventId", "UserId", "ParticipationType")
WHERE "Status" = 1;  -- Only Active status
```

### Allowed Scenarios

```
User tries to buy ticket twice for SAME event:

Attempt 1:
┌─────────────────────────────────────────┐
│ EventParticipation                      │
│─────────────────────────────────────────│
│ EventId: Workshop-123                   │
│ UserId: Alice                           │
│ ParticipationType: Ticket               │
│ Status: Active ← INDEX APPLIES          │
└─────────────────────────────────────────┘
✅ INSERTED (first purchase)

Attempt 2:
┌─────────────────────────────────────────┐
│ EventParticipation                      │
│─────────────────────────────────────────│
│ EventId: Workshop-123                   │
│ UserId: Alice                           │
│ ParticipationType: Ticket               │
│ Status: Active ← INDEX APPLIES          │
└─────────────────────────────────────────┘
❌ REJECTED! Unique constraint violation
Index already has (Workshop-123, Alice, Ticket) WHERE Status=Active

User cancels, then re-purchases:

First Purchase (Cancelled):
┌─────────────────────────────────────────┐
│ EventParticipation                      │
│─────────────────────────────────────────│
│ EventId: Workshop-123                   │
│ UserId: Alice                           │
│ ParticipationType: Ticket               │
│ Status: Cancelled ← INDEX DOES NOT APPLY│
└─────────────────────────────────────────┘
Kept for history

Second Purchase (Active):
┌─────────────────────────────────────────┐
│ EventParticipation                      │
│─────────────────────────────────────────│
│ EventId: Workshop-123                   │
│ UserId: Alice                           │
│ ParticipationType: Ticket               │
│ Status: Active ← INDEX APPLIES          │
└─────────────────────────────────────────┘
✅ ALLOWED! Index only checks Active status

Database contains:
- 1 Active participation (counts toward sold)
- 1 Cancelled participation (history only)
- No conflict!
```

---

## Schema Changes Summary

### Removed Columns (Stale Data)

```diff
- TicketTypes.Sold (integer)
-   ❌ Never updated, always stale
-   ✅ Replaced with calculated property

- Sessions.CurrentAttendees (integer)
-   ❌ Calculated in-memory but not persisted
-   ✅ Replaced with calculated property
```

### Added Columns (Explicit Relationships)

```diff
+ EventParticipations.TicketPurchaseId (uuid, nullable)
+   ✅ Foreign key to TicketPurchases
+   ✅ Links attendance record to payment transaction
+   ✅ Enables future multi-ticket support

+ TicketTypes.IsMultiSession (boolean)
+   ✅ Supports tickets valid for multiple sessions
+   ✅ Defaults to false for single-session tickets
+   ✅ Future-ready for session selection UI
```

### Added Indexes (Performance & Integrity)

```diff
+ EventParticipations: Foreign key index on TicketPurchaseId
+   ✅ Improves join performance

+ EventParticipations: Partial unique index
+   WHERE Status = 1 (Active only)
+   ✅ Enforces "one active participation per user per event"
+   ✅ Allows multiple cancelled/refunded for history
+   ✅ Prevents race conditions
```

---

## Performance Comparison

### Query: Get Event with Sold Counts

#### Before (Implicit Relationship)

```csharp
// TicketTypeDto constructor - Lines 97-130
var participationLookup = eventParticipations
    .Where(ep => ep.Status == Active)
    .Select(ep => ep.UserId)
    .ToHashSet();  // ← In-memory HashSet

QuantitySold = ticketType.Purchases
    .Where(p => p.IsPaymentCompleted &&
                participationLookup.Contains(p.UserId))  // ← HashSet lookup
    .Select(p => p.UserId)
    .Distinct()
    .Count();

// Execution:
// 1. Load all participations
// 2. Filter to Active
// 3. Build HashSet of UserIds (memory allocation)
// 4. Load all purchases
// 5. Filter by payment status
// 6. Filter by UserId in HashSet
// 7. Distinct users
// 8. Count
// = 8 operations, requires HashSet
```

#### After (Explicit Foreign Key)

```csharp
// TicketTypeDto constructor - Simplified
QuantitySold = eventParticipations
    .Count(ep =>
        ep.Status == Active &&
        ep.TicketPurchase != null &&
        ep.TicketPurchase.TicketTypeId == ticketType.Id);

// Execution:
// 1. Filter participations to Active
// 2. Filter where TicketPurchase is not null
// 3. Filter by TicketTypeId
// 4. Count
// = 4 operations, NO HashSet needed

// Database indexes used:
// - IX_EventParticipations_EventId_Status (Status=Active)
// - IX_EventParticipations_TicketPurchaseId (join to TicketPurchases)
// - IX_TicketPurchases_TicketTypeId (filter by ticket type)

// Result: 50% fewer operations, better index utilization
```

---

## Migration Sequence

```
Step 1: Remove Stale Columns
┌─────────────────────────────┐
│ Migration:                  │
│ RemoveStaleSoldColumns      │
│─────────────────────────────│
│ DROP COLUMN Sold            │
│ DROP COLUMN CurrentAttendees│
└─────────────────────────────┘
         ↓
┌─────────────────────────────┐
│ Result:                     │
│ No more stale data in DB    │
│ Calculated properties used  │
└─────────────────────────────┘

Step 2: Add Foreign Key
┌─────────────────────────────┐
│ Migration:                  │
│ AddTicketPurchaseLink       │
│─────────────────────────────│
│ ADD COLUMN TicketPurchaseId │
│ POPULATE existing data      │
│ ADD FOREIGN KEY constraint  │
└─────────────────────────────┘
         ↓
┌─────────────────────────────┐
│ Result:                     │
│ Explicit relationship exists│
│ Existing data migrated      │
└─────────────────────────────┘

Step 3: Add Unique Constraint
┌─────────────────────────────┐
│ Migration:                  │
│ AddUniqueConstraint         │
│─────────────────────────────│
│ CLEAN duplicate Active rows │
│ CREATE partial unique index │
└─────────────────────────────┘
         ↓
┌─────────────────────────────┐
│ Result:                     │
│ Business rule enforced by DB│
│ No duplicate active records │
└─────────────────────────────┘

Step 4: Add Multi-Session Support
┌─────────────────────────────┐
│ Migration:                  │
│ AddMultiSessionSupport      │
│─────────────────────────────│
│ ADD COLUMN IsMultiSession   │
│ SET flag for existing       │
│ multi-session tickets       │
└─────────────────────────────┘
         ↓
┌─────────────────────────────┐
│ Result:                     │
│ Future-ready for UI feature │
│ Existing data tagged        │
└─────────────────────────────┘
```

---

**END OF DIAGRAMS**

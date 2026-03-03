# Payment Entity Consolidation - Implementation Plan

**Date**: 2026-03-02
**Last Updated**: 2026-03-03
**Status**: Complete (All 7 Phases + Follow-Up Tech Debt — Deployed to Dev & Staging 2026-03-03)
**Based On**: deep-dive-research.md, dead-code-liveness-analysis.md

---

## Overview

This plan addresses all issues identified in the research phase:
- 2 critical bugs (broken admin refund paths)
- 1 architectural problem (kiosk cash payments invisible in admin list)
- Massive dead code across backend and frontend
- Misleading field naming (`ProcessRefundRequest.PaymentId`)
- Orphaned database tables and relationships

The work is organized into 7 phases, ordered by dependency and risk. Each phase is independently testable and committable.

---

## Phase 1: Fix Critical Bugs - ParticipationEndpoints Admin Refunds

**Risk**: HIGH (active bugs in production)
**Scope**: 1 file, 2 code paths
**Dependencies**: None

### Problem
Two endpoints in `ParticipationEndpoints.cs` query `context.Payments` and pass `Payment.Id` to `RefundService.ProcessRefundAsync()`, which queries `TicketPurchases` by that ID. Since Payment and TicketPurchase have different IDs, refunds always fail with "Ticket purchase not found."

### Changes

**File: `apps/api/Features/Participation/Endpoints/ParticipationEndpoints.cs`**

#### Fix 1: Admin Remove RSVP (lines ~954-971)

**Current (broken):**
```csharp
var payment = await context.Payments
    .FirstOrDefaultAsync(p =>
        p.EventRegistrationId == ticketParticipation.Id &&
        p.Status == PaymentStatus.Completed, cancellationToken);

if (payment != null)
{
    var refundRequest = new ProcessRefundRequest
    {
        PaymentId = payment.Id,
        RefundAmount = Money.Create(payment.AmountValue, payment.Currency),
        ...
    };
}
```

**Fixed:**
```csharp
var ticketPurchase = await context.TicketPurchases
    .FirstOrDefaultAsync(tp =>
        tp.UserId == userId &&
        tp.TicketType.EventId == eventId &&
        tp.PaymentStatus == "Completed", cancellationToken);

if (ticketPurchase != null)
{
    var refundRequest = new ProcessRefundRequest
    {
        PaymentId = ticketPurchase.Id,
        RefundAmount = Money.Create(ticketPurchase.TotalPrice, "USD"),
        ...
    };
}
```

**Notes:**
- The lookup changes from `EventRegistrationId == ticketParticipation.Id` (Payment field) to querying TicketPurchase by UserId + EventId (via TicketType navigation)
- Need to verify the exact query to find the right TicketPurchase — may need to join through EventAttendance or TicketType→Event
- Amount source changes from `payment.AmountValue`/`payment.Currency` to `ticketPurchase.TotalPrice`/`"USD"`

#### Fix 2: Admin Refund Ticket (lines ~1125-1147)

Same pattern as Fix 1. Change from querying `context.Payments` to `context.TicketPurchases`.

**Additional context**: This endpoint also updates EventAttendance status and handles volunteer shift cancellation. Those parts don't need to change — only the Payment→TicketPurchase lookup and the refund request creation.

### Testing
- Verify admin can refund a ticket via participation management UI
- Verify admin can remove RSVP with associated paid ticket (cascading refund)
- Verify refund amount is correct
- Verify EventAttendance and volunteer shift cleanup still works

---

## Phase 2: Migrate Kiosk Cash Payments to TicketPurchase

**Risk**: MEDIUM (active in production for door sales)
**Scope**: 1 backend file + SSE notification updates
**Dependencies**: None (independent of Phase 1)

### Problem
`KioskPaymentEndpoints.RecordCashPayment` creates `Payment` entities for cash door sales. These records are invisible in the admin payment list (which queries `TicketPurchases`).

### Changes

**File: `apps/api/Features/Payments/Endpoints/KioskPaymentEndpoints.cs`**

#### Change: Replace Payment creation with TicketPurchase creation (lines ~281-313)

**Current:**
```csharp
var payment = new Payment
{
    Id = Guid.NewGuid(),
    EventRegistrationId = attendee.Id,
    UserId = request.AttendeeId,
    AmountValue = request.Amount,
    Currency = "USD",
    Status = PaymentStatus.Completed,
    PaymentMethodType = PaymentMethodType.Cash,
    ProcessedAt = DateTime.UtcNow,
    ...
};
payment.Metadata["recordedBy"] = sessionTokenEntity.CreatedByUserId.ToString();
_dbContext.Set<Payment>().Add(payment);
```

**New approach:**
```csharp
var ticketPurchase = new TicketPurchase
{
    Id = Guid.NewGuid(),
    TicketTypeId = /* need to determine — cash door sales may need a default ticket type */,
    UserId = request.AttendeeId,
    TotalPrice = request.Amount,
    PaymentStatus = "Completed",
    PaymentMethod = "cash",
    PaymentReference = $"CASH-{Guid.NewGuid():N}",
    ProcessedAt = DateTime.UtcNow,
    PurchaseDate = DateTime.UtcNow,
    Quantity = 1,
    RecordedByStaffId = sessionTokenEntity.CreatedByUserId,
    SlidingScalePercentage = 0,
    Notes = $"Cash payment recorded at door. Session: {sessionToken}",
    CreatedAt = DateTime.UtcNow,
    UpdatedAt = DateTime.UtcNow,
};
_dbContext.TicketPurchases.Add(ticketPurchase);
```

**Open questions** (need to research before implementation):
1. **TicketTypeId**: TicketPurchase requires a TicketTypeId FK. Cash door sales may not have a pre-existing ticket type. Options:
   - Create a "Door Sale" ticket type per event
   - Use the event's general admission ticket type
   - Need to examine how the kiosk UI selects what the attendee is paying for
2. **EventAttendance**: The current Payment flow creates a Payment but does NOT create an EventAttendance record. TicketPurchase-based flows typically create EventAttendance via AttendanceService. Need to decide: should cash payments also create EventAttendance records? (They probably should, for consistency with the admin payment list and attendance tracking.)
3. **SSE notification**: The current SSE notification sends `payment.Id`. If we change to TicketPurchase, the notification payload changes. Need to verify the kiosk frontend handles this correctly.

### Testing
- Verify cash door sale creates TicketPurchase record
- Verify cash sales now appear in admin payment list
- Verify SSE kiosk notification still works
- Verify kiosk check-in flow end-to-end

---

## Phase 3: Rename ProcessRefundRequest.PaymentId to TicketPurchaseId

**Risk**: LOW (internal rename, no API surface change)
**Scope**: 1 interface file + all callers
**Dependencies**: Phase 1 must be complete (both phases touch the same callers)

### Problem
`ProcessRefundRequest.PaymentId` was originally a Payment entity ID. After the consolidation migration, it became a TicketPurchase ID, but the field was never renamed. This caused the Phase 1 bug and will continue to confuse developers.

### Changes

**File: `apps/api/Features/Payments/Services/IRefundService.cs` (line ~70)**
```csharp
// BEFORE:
public Guid PaymentId { get; set; }

// AFTER:
public Guid TicketPurchaseId { get; set; }
```

**Files that reference `ProcessRefundRequest.PaymentId` (update all):**
1. `Features/Payments/Services/RefundService.cs` — `request.PaymentId` → `request.TicketPurchaseId`
2. `Features/Payments/Commands/RefundTicket.cs` — `PaymentId = ticketPurchase.Id` → `TicketPurchaseId = ticketPurchase.Id`
3. `Features/Payments/Commands/ProcessVariableRefund.cs` — `PaymentId = ticketPurchase.Id` → `TicketPurchaseId = ticketPurchase.Id`
4. `Features/Participation/Endpoints/ParticipationEndpoints.cs` — (already fixed in Phase 1, update field name)

### Testing
- Build succeeds (compile-time verification that all references updated)
- Existing refund tests pass

---

## Phase 4: Remove Dead Backend Code

**Risk**: LOW (dead code removal — nothing calls this code)
**Scope**: ~15 files deleted, 3 files modified
**Dependencies**: Phase 2 must be complete (KioskPaymentEndpoints must no longer create Payment entities)

### Files to Delete (entirely dead)

| # | File | Why Dead |
|---|------|----------|
| 1 | `Features/Payments/Entities/PaymentFailure.cs` | FK to Payment; zero records ever created; Serilog replaces this |
| 2 | `Features/Payments/Configuration/PaymentFailureConfiguration.cs` | Config for dead entity |
| 3 | `Features/Payments/Entities/PaymentMethod.cs` | Saved card feature never implemented; references Stripe |
| 4 | `Features/Payments/Configuration/PaymentMethodConfiguration.cs` | Config for dead entity |
| 5 | `Features/Payments/Entities/PaymentAuditLog.cs` | Only written by dead PaymentService; Serilog replaces this |
| 6 | `Features/Payments/Configuration/PaymentAuditLogConfiguration.cs` | Config for dead entity |
| 7 | `Features/Payments/Services/PaymentService.cs` | Only consumed by dead PaymentEndpoints; KioskPaymentEndpoints creates Payment directly |
| 8 | `Features/Payments/Services/IPaymentService.cs` | Interface for dead service |
| 9 | `Features/Payments/Endpoints/PaymentEndpoints.cs` | Frontend never calls any routes; replaced by CheckoutEndpoints + PayPalCheckoutEndpoints |
| 10 | `Features/Payments/Endpoints/CreditCardEndpoints.cs` | Frontend never calls `POST /api/payments/credit-card`; broken design (charges before persisting) |
| 11 | `Features/Payments/Validators/ProcessPaymentApiRequestValidator.cs` | Validates request for dead PaymentEndpoints |
| 12 | `Features/Payments/Models/Requests/ProcessPaymentApiRequest.cs` | Request model for dead PaymentEndpoints (verify not shared) |

### Files to Modify

**File: `apps/api/Data/ApplicationDbContext.cs`**
- Remove `DbSet<PaymentFailure> PaymentFailures` declaration
- Remove `DbSet<PaymentAuditLog> PaymentAuditLog` declaration
- Remove `DbSet<PaymentMethod>` declaration (if exists as explicit DbSet)
- Remove `modelBuilder.ApplyConfiguration(new PaymentFailureConfiguration())`
- Remove `modelBuilder.ApplyConfiguration(new PaymentAuditLogConfiguration())`
- Remove `modelBuilder.ApplyConfiguration(new PaymentMethodConfiguration())`
- **Keep** `DbSet<Payment> Payments` for now (needed for Phase 6 migration)
- **Keep** `DbSet<PaymentRefund> PaymentRefunds` (active, correctly references TicketPurchase)

**File: `apps/api/Features/Shared/Extensions/ServiceCollectionExtensions.cs`**
- Remove `services.AddScoped<IPaymentService, PaymentService>()`
- **Keep** other payment service registrations (RefundService, PaymentListService, etc.)

**File: `apps/api/Features/Payments/Entities/Payment.cs`**
- Remove `Refunds` navigation property (broken — PaymentRefund FK now points to TicketPurchase)
- Remove `Failures` navigation property (PaymentFailure being deleted)
- Remove `AuditLogs` navigation property (PaymentAuditLog being deleted)
- **Keep** the Payment entity itself for now (Phase 6 handles the DB migration to drop the table)

**File: `apps/api/Features/Payments/Configuration/PaymentConfiguration.cs`**
- Remove relationship configurations for Refunds, Failures, AuditLogs
- **Keep** basic table configuration (needed until Phase 6 drops the table)

### What About the Payment Entity Itself?

The Payment entity and `Payments` table stay in Phase 4. We don't delete them yet because:
1. The database table has existing data that needs a migration to handle
2. PaymentRefunds has an optional FK to PaymentId (needs cleanup in Phase 7)
3. Removing the entity + table requires a coordinated EF migration

The Payment entity will be cleaned down to a minimal stub in this phase (no nav properties, no business logic) and fully removed in Phase 6/7.

### Testing
- Build succeeds
- All active payment flows still work (checkout, PayPal, refunds, admin list)
- No runtime DI errors from removed services

---

## Phase 5: Remove Dead Frontend Code

**Risk**: LOW (dead code removal)
**Scope**: ~5 files deleted, ~4 files modified
**Dependencies**: None (independent of backend phases, but logically after Phase 4)

### Files to Delete

| # | File | Lines | Why Dead |
|---|------|-------|----------|
| 1 | `features/payments/api/paymentApi.ts` | 226 | Every method is dead; `paymentUtils` must be extracted first |
| 2 | `features/payments/hooks/usePayment.ts` | 234 | `processPayment()` never called; `paymentData` reads dead endpoint |
| 3 | `pages/payments/PaymentSuccessPage.tsx` | 265 | Only reachable via dead PayPal flow |
| 4 | `pages/payments/PaymentCancelPage.tsx` | 169 | Only reachable via dead PayPal cancel flow |

### Files to Modify

**File: `apps/web/src/features/payments/pages/EventPaymentPage.tsx`**
- **Line 30**: Remove `import { usePayment } from '../hooks/usePayment'`
- **Lines 80-83**: Remove `const { paymentData, resetProcessingState } = usePayment(registrationId)`
- **Line 341**: Remove `resetProcessingState()` call
- **Line 868**: Change `(completedPayment || paymentData)` → `completedPayment`
- **Line 870**: Change `payment={completedPayment || paymentData || {} as any}` → `payment={completedPayment}`

**File: `apps/web/src/routes/router.tsx`**
- **Lines 180-186**: Remove routes for `/payment/success` and `/payment/cancel`
- Remove imports for `PaymentSuccessPage` and `PaymentCancelPage`

**File: `apps/web/src/features/payments/index.ts`**
- Remove exports for `paymentApi`, `usePayment`, `usePaymentHistory`, `useEventPayments`
- Remove exports for dead types
- Keep exports for active components and types (`PaymentEventInfo`, `SlidingScaleCalculation`, `NonceData`, etc.)

**File: `apps/web/src/features/payments/types/payment.types.ts`**
- Remove dead types: `ProcessPaymentRequest`, `RefundResponse`, `PaymentStatusResponse`, `PaymentError`, `PayPalOrderRequest`, `SavedPaymentMethod`, `CreatePayPalOrderRequest`, `CapturePayPalOrderResponse`, `CreditCardPaymentRequest`, `CreditCardPaymentResponse`, `PaymentFormData`, `PaymentProcessingState`
- Remove dead enums: `PaymentStatus`, `PaymentMethodType`, `RefundStatus`
- Keep active types: `PaymentEventInfo`, `SlidingScaleCalculation`, `NonceData`
- Consider moving remaining active types to a more appropriate location

### paymentUtils Extraction

Before deleting `paymentApi.ts`, extract `paymentUtils` (used by `PaymentConfirmation.tsx` and `useSlidingScale.ts`):
- Create `features/payments/utils/paymentUtils.ts` with the utility functions
- Update imports in `PaymentConfirmation.tsx` (line 22) and `useSlidingScale.ts` (line 5)

### Testing
- Frontend builds without errors
- TypeScript compilation succeeds
- Credit card checkout flow works
- PayPal checkout flow works
- Payment confirmation page displays correctly
- Sliding scale calculation works

---

## Phase 6: Database Migration - Drop Dead Tables

**Risk**: MEDIUM (schema change, requires migration)
**Scope**: 1 EF migration
**Dependencies**: Phase 4 complete (code no longer references dead entities)

### Migration: DropDeadPaymentTables

```csharp
// Drop tables that are entirely dead
migrationBuilder.DropTable(name: "PaymentFailures");
migrationBuilder.DropTable(name: "PaymentMethods");
migrationBuilder.DropTable(name: "PaymentAuditLog");
```

### What About the Payments Table?

The `Payments` table is NOT dropped in this phase because:
1. `PaymentRefunds` has an optional FK (`PaymentId`) pointing to it
2. There may be existing Payment records in production (cash door sales)
3. Dropping requires Phase 7 to clean up the FK first

### After Migration
- Remove the entity classes and configurations deleted in Phase 4 from the model snapshot
- The EF migration will handle the table drops

### Testing
- Migration applies cleanly to dev database
- Migration applies cleanly to staging database
- All active payment flows still work
- No EF model validation errors

---

## Phase 7: Remove Payment Entity and Table (Final Cleanup)

**Risk**: MEDIUM (depends on production data state)
**Scope**: EF migration + entity cleanup
**Dependencies**: Phases 2, 4, 6 all complete

### Step 1: Clean Up PaymentRefund's Optional Payment FK

**ALREADY DONE** in Phase 6 migration (`20260303051802_DropDeadPaymentTables`). The migration drops the `PaymentId` column, FK, and index from `PaymentRefunds`. The entity and configuration already don't reference `PaymentId`.

### Step 2: Migration to Drop Payments Table

```csharp
// Only the Payments table remains to be dropped
migrationBuilder.DropTable(name: "Payments");
```

### Step 3: Remove Payment Entity from Codebase

- Delete `Features/Payments/Entities/Payment.cs`
- Delete `Features/Payments/Configuration/PaymentConfiguration.cs`
- Remove `DbSet<Payment> Payments` from ApplicationDbContext
- Remove `modelBuilder.ApplyConfiguration(new PaymentConfiguration())` from OnModelCreating
- Remove `Payment` model from `PaymentRefund.cs` navigation (if not already done in Step 1)
- Remove any remaining `using` statements referencing Payment entity
- Delete `Features/Payments/ValueObjects/Money.cs` if no longer used (verify RefundService still needs it)
- Delete `Features/Payments/Models/PaymentStatus.cs` enum (verify no remaining references)
- Delete `Features/Payments/Models/PaymentMethodType.cs` enum (verify no remaining references)

### Step 4: Clean Up Remaining Dead Models

Check these files and delete if no longer referenced by active code:
- `Features/Payments/Models/Responses/PaymentResponse.cs`
- `Features/Payments/Models/Responses/PaymentTransactionDto.cs`
- Any other models only used by deleted endpoints

### Production Data Consideration

**Decision**: Accept data loss. The data is already orphaned — never updated after creation, not visible in admin UI. No archive needed.

### Testing
- Migration applies cleanly
- All payment flows work end-to-end
- PaymentRefund records still queryable without Payment FK
- Admin payment list shows all payments including former cash door sales

---

## Phase Summary

| Phase | Description | Risk | Status | Files Changed | Depends On |
|-------|-------------|------|--------|--------------|------------|
| 1 | Fix ParticipationEndpoints admin refund bugs | HIGH | **COMPLETE** | 1 modified | None |
| 2 | Migrate kiosk cash payments to TicketPurchase | MEDIUM | **COMPLETE** | 1 modified | None |
| 3 | Rename ProcessRefundRequest.PaymentId | LOW | **COMPLETE** | 7 modified | Phase 1 |
| 4 | Remove dead backend code | LOW | **COMPLETE** | 12 deleted, 4 modified | Phase 2 |
| 5 | Remove dead frontend code | LOW | **COMPLETE** | 4 deleted, 5 modified, 1 created | None |
| 6 | Database migration: drop dead tables | MEDIUM | **COMPLETE** | 1 migration generated | Phase 4 |
| 7 | Remove Payment entity and table | MEDIUM | **COMPLETE** | 5 deleted, 1 modified, 1 migration | Phases 2, 4, 6 |

### Completion Notes (2026-03-03)

**Phase 1**: Both broken refund paths in `ParticipationEndpoints.cs` (lines ~955 and ~1125) now query `context.TicketPurchases` via `ticketParticipation.TicketPurchaseId` instead of `context.Payments`.

**Phase 3**: `ProcessRefundRequest.PaymentId` renamed to `TicketPurchaseId` in: IRefundService.cs, RefundService.cs (16 occurrences), RefundTicket.cs, ProcessVariableRefund.cs, ParticipationEndpoints.cs (2 places), AttendanceService.cs, PaymentEndpoints.cs.

**Phase 4**: Deleted 12 files (PaymentFailure entity+config, PaymentMethod entity+config, PaymentAuditLog entity+config, PaymentService+IPaymentService, PaymentEndpoints, CreditCardEndpoints, ProcessPaymentApiRequestValidator, ProcessPaymentApiRequest). Modified ApplicationDbContext (removed 3 DbSets, 3 ApplyConfiguration calls, 3 ChangeTracker blocks), ServiceCollectionExtensions (removed DI registration), Payment.cs (removed 3 nav properties), PaymentConfiguration.cs (removed 3 relationship configs).

**Phase 5**: Deleted paymentApi.ts, usePayment.ts, PaymentSuccessPage.tsx, PaymentCancelPage.tsx. Extracted paymentUtils to new file. Cleaned EventPaymentPage.tsx, router.tsx, index.ts, payment.types.ts.

**Phase 2**: KioskPaymentEndpoints.RecordCashPayment now creates TicketPurchase + EventAttendance instead of Payment entity. Auto-creates "Door Sale" TicketType per event. Removed all Payment/PaymentStatus/PaymentMethodType imports. CashPaymentResponse.PaymentId renamed to TicketPurchaseId.

**Phase 6**: Generated migration `20260303051802_DropDeadPaymentTables` which drops PaymentAuditLog, PaymentFailures, PaymentMethods tables and the orphaned PaymentId column/FK from PaymentRefunds. Applied to dev and staging databases on 2026-03-03.

**Phase 7**: Deleted Payment.cs, PaymentConfiguration.cs, PaymentStatus.cs enum, PaymentMethodType.cs enum, PaymentResponse.cs (dead model). Removed DbSet<Payment>, ApplyConfiguration(PaymentConfiguration), and ChangeTracker.Entries<Payment> block from ApplicationDbContext.cs. Generated migration `20260303053611_DropPaymentsTable` to drop the Payments table. Applied to dev and staging databases on 2026-03-03.

### Decisions Made

1. **Kiosk cash TicketTypeId**: Auto-create a "Door Sale" ticket type per event when recording cash payments
2. **Kiosk cash EventAttendance**: Yes, create EventAttendance records for cash door sales
3. **Production Payment data**: Accept data loss when dropping Payments table (records are orphaned, never updated, invisible in admin UI)
4. **CreditCardEndpoints**: Removed in Phase 4 (confirmed dead — frontend never calls it)

### Follow-Up Tech Debt (Completed 2026-03-03)

All 9 remaining issues from the deep-dive research were resolved in a follow-up session:

**Phase 1 (Quick Fixes):**
- Removed duplicate `/purchase-ticket` route, updated frontend and integration tests to use `/tickets`
- Added missing `CancellationToken` to `RefundService.LogRefundRetryAsync`
- Created `PaymentConstants.Currency` constant, replaced all hardcoded "USD" strings

**Phase 2 (PaymentStatus Enum):**
- Created `TicketPurchasePaymentStatus` enum with string-backed EF value converter (no migration needed)
- Updated ~15 files from string literals to enum values

**Phase 3 (Metadata JSONB Column):**
- Added `Dictionary<string, object> Metadata` to TicketPurchase with JSONB storage
- Migration: `20260303063750_AddTicketPurchaseMetadata`
- Added `EnableDynamicJson()` to `NpgsqlDataSourceBuilder` in Program.cs

**Phase 4 (Refund Architecture):**
- RefundTicket now supports multiple refunds (remaining balance pattern)
- Removed Flow 1 refund endpoint from ParticipationEndpoints (no frontend caller), moved EventAttendee status update into RefundService
- Wrapped RefundService.ProcessRefundAsync in `IDbContextTransaction`
- Deleted `AdminRefundTicketResponse.cs`, cleaned up test files

**New files**: `PaymentConstants.cs`, `TicketPurchasePaymentStatus.cs`, migration
**Modified**: ~30 files total (4,500 additions, 1,600 deletions)

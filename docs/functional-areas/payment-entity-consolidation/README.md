# Payment Entity Consolidation Project

**Status**: In Progress - Phases 1, 3, 4, 5, 6 Complete. Phase 2 (kiosk cash migration) next.
**Priority**: High (Technical Debt - causes data integrity confusion)
**Created**: 2026-03-02
**Last Updated**: 2026-03-03
**Business Domain**: Payments

## Progress Summary

| What | Status |
|------|--------|
| Research (deep-dive-research.md, dead-code-liveness-analysis.md) | Complete |
| Implementation plan (implementation-plan.md) | Complete |
| Phase 1: Fix ParticipationEndpoints admin refund bugs | **Complete** |
| Phase 2: Migrate kiosk cash payments to TicketPurchase | **In Progress** |
| Phase 3: Rename ProcessRefundRequest.PaymentId → TicketPurchaseId | **Complete** |
| Phase 4: Remove dead backend code (12 files deleted) | **Complete** |
| Phase 5: Remove dead frontend code (4 files deleted) | **Complete** |
| Phase 6: DB migration to drop dead tables (generated, not applied) | **Complete** |
| Phase 7: Remove Payment entity and Payments table | Not Started |

**Total dead code removed**: ~4,800 lines across 16 files deleted and 12 files modified.

## Problem Statement

The codebase has a **dual-entity payment architecture** where two separate entities track payment data: `TicketPurchase` and `Payment`. Through organic evolution, `TicketPurchase` became the de facto single source of truth for all payment operations, while the `Payment` entity became orphaned - it gets created but is never updated by webhooks, refunds, or admin interfaces. Additionally, `PaymentFailure` (designed to track payment errors) has **zero active code paths** and is completely dead code.

This creates confusion for developers, risks data integrity issues, and makes it impossible to build reliable payment reporting.

## Current State Audit (2026-03-02)

### Entity Overview

#### TicketPurchase - THE Single Source of Truth
**File**: `apps/api/Models/TicketPurchase.cs` (229 lines)

This is the entity that ALL active code paths use for payment tracking.

**Core Fields:**
- `Id` (Guid) - Primary key
- `TicketTypeId` (Guid) - FK to ticket type
- `UserId` (Guid) - FK to purchaser
- `PurchaseDate` (DateTime)
- `Quantity` (int)
- `TotalPrice` (decimal)

**Payment Status Fields:**
- `PaymentStatus` (string) - "Pending", "Completed", "Confirmed", "Failed", "PartiallyRefunded"
- `PaymentMethod` (string) - "authorize-net", "authnet-pending", "paypal", "cash", etc.
- `PaymentReference` (string) - Confirmation number
- `ProcessedAt` (DateTime?) - When payment was confirmed

**Credit Card (Authorize.net) Fields:**
- `EncryptedAuthNetTransactionId` (string?) - Encrypted Authorize.net transaction ID
- `CreditCardLastFour` (string?) - Last 4 digits
- `CreditCardType` (string?) - Visa, Mastercard, etc.

**PayPal Fields (Encrypted):**
- `EncryptedPayPalOrderId` (string?)
- `EncryptedPayPalPayerId` (string?)
- `EncryptedPayPalCaptureId` (string?)
- `PayPalOrderIdHash` (string?) - SHA256 hash for indexed webhook lookups

**Additional Fields:**
- `SlidingScalePercentage` (decimal) - 0-75%
- `IdempotencyKey` (string) - Format: WCR-{guid}
- `Notes` (string) - Accessibility, dietary, cash tracking notes
- `RecordedByStaffId` (Guid?) - Staff who processed door sales
- `EventWaiverAccepted` (bool), `EventWaiverAcceptedAt` (DateTime?)
- `CreatedAt`, `UpdatedAt` (DateTime)

**Helper Methods:**
- `IsPaymentCompleted` - Checks PaymentStatus
- `IsRSVP` - Checks if free RSVP
- `IsDoorPurchase` - Checks if staff-recorded

**Navigation Properties:**
- `TicketType` (many-to-one)
- `User` (purchaser)
- `RecordedByStaff` (staff member for door purchases)
- `EventAttendances` (collection - one ticket creates EventAttendance records)

---

#### Payment - ORPHANED Entity
**File**: `apps/api/Features/Payments/Entities/Payment.cs` (256 lines)

This entity is created by `PaymentService` but **never updated by any downstream process** (webhooks, refunds, admin operations all bypass it).

**Core Fields:**
- `Id` (Guid) - Primary key
- `EventRegistrationId` (Guid) - FK to event registration (NOTE: EventRegistration is not a real entity - this is a legacy concept replaced by EventAttendance)
- `UserId` (Guid) - FK to user

**Amount (Money Value Object pattern):**
- `AmountValue` (decimal) - Payment amount
- `Currency` (varchar(3)) - ISO currency code, default "USD"
- Helper methods: `GetAmount()`, `SetAmount()` using Money value object

**Status:**
- `Status` (PaymentStatus enum) - Pending, Completed, PartiallyRefunded, Refunded, Failed
- `PaymentMethodType` (PaymentMethodType enum) - Enum-based (not string like TicketPurchase)

**PayPal Fields (Encrypted) - DUPLICATED from TicketPurchase:**
- `EncryptedPayPalOrderId` (string?)
- `EncryptedPayPalPayerId` (string?)
- `EncryptedPayPalCaptureId` (string?)

**Authorize.net Fields:**
- `EncryptedAuthNetTransactionId` (string?) - Present but never populated by checkout

**Refund Fields:**
- `RefundAmountValue` (decimal?), `RefundCurrency` (varchar(3)?)
- `RefundedAt` (DateTime?), `EncryptedPayPalRefundId` (string?)
- `RefundReason` (string?), `RefundedByUserId` (Guid?)

**Additional:**
- `IdempotencyKey` (string?)
- `SlidingScalePercentage` (decimal)
- `VenmoUsername` (varchar(20)?)
- `Metadata` (Dictionary<string, object>) - JSONB column
- `ProcessedAt`, `CreatedAt`, `UpdatedAt` (DateTime)

**Navigation Properties:**
- `User` (who made payment)
- `RefundedByUser` (who processed refund)
- `AuditLogs` (collection of PaymentAuditLog)
- `Refunds` (collection of PaymentRefund) - **BROKEN**: PaymentRefund now points to TicketPurchase, not Payment
- `Failures` (collection of PaymentFailure) - **DEAD**: PaymentFailure is never created

---

#### PaymentFailure - DEAD CODE (Zero Active Code Paths)
**File**: `apps/api/Features/Payments/Entities/PaymentFailure.cs` (220 lines)

**Fields:**
- `Id` (Guid)
- `PaymentId` (Guid) - FK to Payment (NOT TicketPurchase)
- `FailureCode` (varchar(50)) - Error code from payment processor
- `FailureMessage` (text) - Human-readable message
- `EncryptedStripeErrorDetails` (text?) - Named for Stripe but intended for any processor
- `RetryCount` (int) - Default 0
- `FailedAt` (DateTime), `CreatedAt` (DateTime)

**Helper Methods (well-designed but unused):**
- `CanRetry()` - Checks retryable codes: "processing_error", "temporary_failure", "rate_limit", "network_error", "timeout"
- `IsPermanentFailure()` - Checks permanent codes: "card_declined", "insufficient_funds", "expired_card", "invalid_cvc", "invalid_number", "incorrect_number"
- `GetUserFriendlyMessage()` - Maps codes to user messages
- `GetSuggestedActions()` - Maps codes to recovery steps

**Factory Methods (never called):**
- `FromStripeError()` - Creates failure from Stripe error
- `FromSystemError()` - Creates failure from system error
- `FromValidationError()` - Creates failure from validation

**Database Configuration:** `PaymentFailureConfiguration.cs`
- Indexes: `IX_PaymentFailures_PaymentId`, `IX_PaymentFailures_FailureCode`, `IX_PaymentFailures_FailedAt`
- Partial index: `IX_PaymentFailures_RetryCount` WHERE RetryCount > 0

**Why it's dead:** PaymentFailure requires a Payment FK, but the checkout flow (which handles CC payments) creates TicketPurchase records, not Payment records. There is literally no code path that creates a PaymentFailure record.

---

#### PaymentRefund - MIGRATED to TicketPurchase
**File**: `apps/api/Features/Payments/Entities/PaymentRefund.cs`

**Key Change:**
- Line 18-21: `TicketPurchaseId` (FK to TicketPurchase)
- Comment (line 19): "RENAMED FROM: OriginalPaymentId (migration: ConsolidatePaymentTrackingToTicketPurchases)"
- Navigation (line 112): `TicketPurchase` (NOT `Payment`)
- Comment (line 110): "RENAMED FROM: OriginalPayment (migration: ConsolidatePaymentTrackingToTicketPurchases)"

**Status:** PaymentRefund has been successfully migrated to point to TicketPurchase as its source of truth. However, the Payment entity still declares a `Refunds` navigation property pointing to PaymentRefund, which is now a broken/orphaned relationship.

---

#### PaymentAuditLog - Attached to Payment (partially orphaned)
**File**: `apps/api/Features/Payments/Entities/PaymentAuditLog.cs` (268 lines)

**Fields:**
- `Id` (Guid)
- `PaymentId` (Guid) - FK to Payment
- `UserId` (Guid?) - User who performed action
- `ActionType` (varchar(50)) - Constrained: 'PaymentInitiated', 'PaymentProcessed', 'PaymentCompleted', 'PaymentFailed', 'PaymentRetried', 'RefundInitiated', 'RefundCompleted', 'RefundFailed', 'StatusChanged', 'MetadataUpdated', 'SystemAction'
- `ActionDescription` (text)
- `OldValues` (JSONB) - Previous state
- `NewValues` (JSONB) - New state
- `IpAddress` (varchar(45))
- `UserAgent` (varchar(1000))
- `CreatedAt` (DateTime)

**Factory Methods:**
- `PaymentInitiated()`, `PaymentCompleted()`, `PaymentFailed()`, `RefundInitiated()`, `RefundCompleted()`, `StatusChanged()`, `MetadataUpdated()`

**Status:** Only written to by `PaymentService` when creating Payment records. Since Payment is orphaned, these audit logs only capture the initial creation, not the full lifecycle.

---

### DbContext Registration

**File**: `apps/api/Data/ApplicationDbContext.cs`

```csharp
public DbSet<TicketPurchase> TicketPurchases { get; set; }      // Line 119
public DbSet<Payment> Payments { get; set; }                     // Line 224
public DbSet<PaymentRefund> PaymentRefunds { get; set; }         // Line 234
public DbSet<PaymentFailure> PaymentFailures { get; set; }       // Line 244
public DbSet<PaymentAuditLog> PaymentAuditLog { get; set; }      // Registered
```

All entities are registered and have corresponding database tables.

---

## Code Path Analysis: Who Uses What?

### TicketPurchase Usage (12+ active code paths)

| Endpoint/Service | File | Action | Line(s) |
|------------------|------|--------|---------|
| CheckoutEndpoints.CreditCardCheckout | `Payments/Endpoints/CheckoutEndpoints.cs` | Creates pending, finalizes on CC success, rolls back on failure | 56-395 |
| CreditCardEndpoints.ProcessCreditCardPayment | `Payments/Endpoints/CreditCardEndpoints.cs` | Updates after CC payment | 48-133 |
| ParticipationEndpoints | `Participation/Endpoints/ParticipationEndpoints.cs` | Creates via AttendanceService | 294, 407 |
| AttendanceService.CreateTicketPurchaseAsync | `Participation/Services/AttendanceService.cs` | Primary creator | 515+ |
| AttendanceService.CancelTicketPurchasesAsync | `Participation/Services/AttendanceService.cs` | Cancellation | N/A |
| RefundService.ProcessRefundAsync | `Payments/Services/RefundService.cs` | Queries TP for refund processing | 51-115 |
| PaymentListService.GetPaymentListAsync | `Payments/Services/PaymentListService.cs` | Admin payment list queries TP | 42-100 |
| PayPalWebhookProcessingService | `Webhooks/Services/PayPalWebhookProcessingService.cs` | Updates TP on PayPal webhook | 86-197 |
| KioskPaymentEndpoints | `Payments/Endpoints/KioskPaymentEndpoints.cs` | Door/cash sales create TP (via Payment entity for cash - see note) | N/A |
| AdminPaymentEndpoints | `Payments/Endpoints/AdminPaymentEndpoints.cs` | Lists via PaymentListService (which queries TP) | 20-44 |

**Note on Kiosk:** KioskPaymentEndpoints creates a `Payment` entity for cash payments (not TicketPurchase). This is one of the few remaining active uses of Payment.

### Payment Usage (2-3 active code paths, mostly creation-only)

| Endpoint/Service | File | Action | Line(s) |
|------------------|------|--------|---------|
| PaymentService.ProcessPaymentAsync | `Payments/Services/PaymentService.cs` | Creates Payment for EventRegistration | 32-135 |
| PaymentEndpoints (POST /api/payments/process) | `Payments/Endpoints/PaymentEndpoints.cs` | Calls PaymentService | 87 |
| KioskPaymentEndpoints.RecordCashPayment | `Payments/Endpoints/KioskPaymentEndpoints.cs` | Creates Payment for cash | 281-294 |
| PaymentService.GetPaymentByIdAsync | `Payments/Services/PaymentService.cs` | Reads (includes Failures, AuditLogs) | 137-161 |
| PaymentService.UpdatePaymentStatusAsync | `Payments/Services/PaymentService.cs` | Status update (unclear if ever called) | 226-273 |

### PaymentFailure Usage (0 active code paths)

| Endpoint/Service | File | Action |
|------------------|------|--------|
| PaymentService.LogPaymentFailureAsync | `Payments/Services/PaymentService.cs` | Private method - only called for PayPal order creation failure, NOT for CC failures |

**The only call site** for `LogPaymentFailureAsync` is line 114 in PaymentService - called when PayPal order creation fails. This creates a PaymentFailure record for the Payment entity. However, CC payment failures in CheckoutEndpoints and CreditCardEndpoints do NOT call this method because those endpoints don't create Payment records.

### PaymentRefund Usage (1 active code path)

| Endpoint/Service | File | Action |
|------------------|------|--------|
| RefundService.ProcessRefundAsync | `Payments/Services/RefundService.cs` | Creates PaymentRefund with TicketPurchaseId FK |

---

## Data Flow Diagrams

### Credit Card Payment (Current)
```
User → POST /api/checkout/credit-card
         │
         ├── Stage 1: Validate request
         ├── Stage 2: Create PENDING TicketPurchase ──── AttendanceService
         ├── Stage 3: Charge card via AuthorizeNetService
         │     ├── SUCCESS → Stage 4: Update TicketPurchase → "Completed"
         │     └── FAILURE → Rollback TicketPurchase → "Failed"
         │                   ⚠️ Error code/message LOST (only in ILogger)
         │                   ⚠️ NO PaymentFailure record created
         │                   ⚠️ NO Payment record exists to attach failure to
         └── Return CheckoutResponse

NOTE: Payment entity is NEVER created or touched in this flow.
```

### PayPal Payment (Current)
```
User → POST /api/payments/process (PaymentEndpoints)
         │
         ├── PaymentService creates Payment record (Pending)
         ├── PaymentService creates PayPal order
         ├── Returns PayPal order ID to frontend
         │
User → Redirected to PayPal → Completes payment
         │
PayPal → POST /api/webhooks/paypal
         │
         ├── PayPalWebhookProcessingService looks up TicketPurchase by PayPalOrderIdHash
         ├── Updates TicketPurchase.PaymentStatus → "Completed"
         └── ⚠️ Payment record is NEVER updated (stuck at Pending forever)
```

### Refund (Current)
```
Admin → POST /api/refunds/{ticketPurchaseId}
         │
         ├── RefundService queries TicketPurchase (NOT Payment)
         ├── Gets encrypted PayPal/AuthNet IDs from TicketPurchase
         ├── Calls PayPal or AuthorizeNet for refund
         ├── Creates PaymentRefund with TicketPurchaseId FK
         └── ⚠️ Payment record (if exists) is NEVER updated
```

### Admin Payment List (Current)
```
Admin → GET /api/admin/payments
         │
         ├── PaymentListService queries TicketPurchases table
         ├── Joins with PaymentRefunds (via TicketPurchaseId)
         └── Returns PaymentListResponse

NOTE: Payment table is completely bypassed.
Comment in code: "REWRITTEN: Now queries TicketPurchases table (single source of truth)"
```

---

## Migration History

### InitialCreate (20251127064731)
- Created `TicketPurchases` table with PaymentStatus, PaymentMethod, PaymentReference
- Created `Payments` table with EventRegistrationId, Amount, Status, Metadata
- Created `PaymentRefunds` table with `OriginalPaymentId` FK to Payments
- Created `PaymentFailures` table with `PaymentId` FK to Payments

### AddCreditCardAndPayPalHashFields (20260223030627)
- Added `CreditCardLastFour`, `CreditCardType`, `EncryptedAuthNetTransactionId` to TicketPurchase
- Added `PayPalOrderIdHash` to TicketPurchase for webhook lookups

### ConsolidatePaymentTrackingToTicketPurchases (referenced in comments, exact migration file TBD)
- Renamed `PaymentRefund.OriginalPaymentId` → `TicketPurchaseId`
- Changed FK from Payments → TicketPurchases
- This migration formalized TicketPurchase as the single source of truth for refunds

---

## Reference Counts

| Entity | Total File References | Active Code Paths | Status |
|--------|----------------------|-------------------|--------|
| TicketPurchase | 81 files | 12+ | PRODUCTION - Single source of truth |
| Payment | 103 files | 2-3 (creation only) | ORPHANED/TRANSITIONAL |
| PaymentRefund | 10+ files | 1 (RefundService) | PRODUCTION (points to TicketPurchase) |
| PaymentFailure | 18 files | 0 | DEAD CODE |
| PaymentAuditLog | ~15 files | 1 (PaymentService creation) | PARTIALLY ORPHANED |

---

## Broken Navigation Properties

### Payment.Refunds Collection
**Problem:** Payment entity declares `ICollection<PaymentRefund> Refunds` navigation property, but PaymentRefund's FK now points to `TicketPurchaseId`, not `PaymentId`. This navigation will always return empty or throw.

### Payment.Failures Collection
**Problem:** Payment entity declares `ICollection<PaymentFailure> Failures` navigation property, but PaymentFailure is never created. This navigation always returns empty.

---

## Decisions Needed

### 1. Deprecate or Remove Payment Entity?
**Evidence for removal:**
- TicketPurchase is already the single source of truth for CC, PayPal, and cash payments
- Payment records that DO get created are never updated downstream
- The ConsolidatePaymentTrackingToTicketPurchases migration already signaled this direction
- PaymentListService was explicitly "REWRITTEN" to use TicketPurchase
- RefundService comment: "ARCHITECTURE FIX (2025-11-18): Now uses TicketPurchase"

**Caution:**
- 103 files reference Payment - need careful cleanup
- KioskPaymentEndpoints.RecordCashPayment creates Payment records for cash - needs migration to TicketPurchase
- PaymentService.ProcessPaymentAsync creates Payment for PayPal flow - need to verify if this can be removed
- Payment has some nice features (Money value object, Metadata JSONB) that TicketPurchase lacks

### 2. What to Do with PaymentFailure?
**Options:**
- **Remove entirely** (it's dead code) - simplest
- **Migrate FK to TicketPurchase** and actually use it for CC failure logging
- **Replace with site-wide logging** (see logging-observability project) - most forward-thinking

**Recommendation:** If the logging-observability project implements Serilog with a queryable sink, PaymentFailure becomes redundant. CC failures would be structured log events queryable by date, error code, etc. However, if we want a dedicated business-level failure table (separate from operational logs), we could migrate it to TicketPurchase FK.

### 3. What to Do with PaymentAuditLog?
**Options:**
- Keep as-is (it still serves a purpose for Payment records that exist)
- Migrate to reference TicketPurchase instead
- Replace with site-wide audit logging from the logging-observability project
- Keep for compliance/business audit, supplement with operational logging

### 4. Valuable Features to Preserve from Payment Entity
Some features on Payment are genuinely better designed than TicketPurchase equivalents:

| Feature | Payment | TicketPurchase | Recommendation |
|---------|---------|----------------|----------------|
| Status type | Enum (PaymentStatus) | String ("Completed", "Failed") | Consider migrating TP to enum |
| Amount | Money value object | Raw decimal (TotalPrice) | Consider adding value object to TP |
| Payment method | Enum (PaymentMethodType) | String ("authorize-net") | Consider migrating TP to enum |
| Metadata | JSONB Dictionary | Notes (string) | Consider adding metadata JSONB to TP |
| Refund tracking | RefundAmountValue, RefundReason, etc. | Via PaymentRefund join | Current join approach is fine |

### 5. Migration Strategy
**Options:**
- **Big bang**: Remove Payment/PaymentFailure in one migration, update all code
- **Gradual deprecation**: Mark Payment as [Obsolete], redirect new code to TicketPurchase, remove later
- **Feature flag**: Support both during transition

---

## Scope of Work (Estimated)

### Phase 1: Audit & Plan (this document)
- Map all code paths (DONE)
- Identify all references (DONE)
- Document data flow (DONE)
- Get decisions on approach

### Phase 2: Code Cleanup (if removing Payment entity)
- Remove PaymentFailure entity, configuration, DbSet, migrations
- Remove or archive Payment entity (or mark [Obsolete])
- Fix PaymentRefund → TicketPurchase navigation (clean up broken references)
- Update KioskPaymentEndpoints to use TicketPurchase for cash
- Update PaymentService or remove if no longer needed
- Fix Payment.Refunds and Payment.Failures broken navigation properties
- Update PaymentEndpoints (POST /api/payments/process) or redirect to checkout flow
- Remove unused DTOs and response models

### Phase 3: TicketPurchase Enhancements (if consolidating)
- Consider adding PaymentStatus enum (replace string)
- Consider adding Metadata JSONB column
- Consider adding Money value object pattern
- Create migration for any schema changes

### Phase 4: Testing
- Verify all payment flows work (CC, PayPal, cash, refund)
- Verify admin payment list still works
- Verify webhook processing still works
- Verify no orphaned data in production

### Phase 5: Data Migration (if needed)
- Migrate any valuable Payment records to TicketPurchase equivalents
- Clean up orphaned Payment records in production
- Verify PaymentRefund records are consistent

---

## Key Files Reference

### Entities
- `apps/api/Models/TicketPurchase.cs` - Primary payment entity (229 lines)
- `apps/api/Features/Payments/Entities/Payment.cs` - Orphaned entity (256 lines)
- `apps/api/Features/Payments/Entities/PaymentFailure.cs` - Dead code (220 lines)
- `apps/api/Features/Payments/Entities/PaymentRefund.cs` - Migrated to TicketPurchase FK
- `apps/api/Features/Payments/Entities/PaymentAuditLog.cs` - Attached to Payment (268 lines)
- `apps/api/Features/Payments/Entities/PaymentMethod.cs` - Payment method entity

### Configurations (EF)
- `apps/api/Features/Payments/Configuration/PaymentConfiguration.cs`
- `apps/api/Features/Payments/Configuration/PaymentFailureConfiguration.cs`
- `apps/api/Features/Payments/Configuration/PaymentAuditLogConfiguration.cs`
- `apps/api/Features/Payments/Configuration/PaymentRefundConfiguration.cs`
- `apps/api/Features/Payments/Configuration/PaymentMethodConfiguration.cs`

### Services
- `apps/api/Features/Payments/Services/PaymentService.cs` - Creates Payment, has LogPaymentFailureAsync (331 lines)
- `apps/api/Features/Payments/Services/IPaymentService.cs` - Interface
- `apps/api/Features/Payments/Services/AuthorizeNetService.cs` - CC processing (474 lines)
- `apps/api/Features/Payments/Services/IAuthorizeNetService.cs` - Interface
- `apps/api/Features/Payments/Services/RefundService.cs` - Uses TicketPurchase
- `apps/api/Features/Payments/Services/PaymentListService.cs` - Uses TicketPurchase
- `apps/api/Features/Payments/Services/PayPalService.cs` - PayPal integration
- `apps/api/Features/Participation/Services/AttendanceService.cs` - Creates TicketPurchase

### Endpoints
- `apps/api/Features/Payments/Endpoints/CheckoutEndpoints.cs` - CC checkout, uses TicketPurchase (465 lines)
- `apps/api/Features/Payments/Endpoints/CreditCardEndpoints.cs` - Standalone CC, uses TicketPurchase (154 lines)
- `apps/api/Features/Payments/Endpoints/PaymentEndpoints.cs` - Uses PaymentService (Payment entity)
- `apps/api/Features/Payments/Endpoints/KioskPaymentEndpoints.cs` - Cash payments, uses Payment entity
- `apps/api/Features/Payments/Endpoints/AdminPaymentEndpoints.cs` - Uses PaymentListService (TicketPurchase)
- `apps/api/Features/Payments/Endpoints/RefundEndpoints.cs` - Uses RefundService (TicketPurchase)
- `apps/api/Features/Payments/Endpoints/WebhookEndpoints.cs` - Routes to webhook service
- `apps/api/Features/Participation/Endpoints/ParticipationEndpoints.cs` - Uses AttendanceService (TicketPurchase)

### Webhooks
- `apps/api/Features/Webhooks/Services/PayPalWebhookProcessingService.cs` - Updates TicketPurchase (not Payment)

### Models/DTOs
- `apps/api/Features/Payments/Models/Requests/ProcessPaymentApiRequest.cs`
- `apps/api/Features/Payments/Models/Requests/PaymentListQueryParameters.cs`
- `apps/api/Features/Payments/Models/Requests/VariableRefundRequest.cs`
- `apps/api/Features/Payments/Models/Responses/PaymentListResponse.cs`
- `apps/api/Features/Payments/Models/Responses/PaymentResponse.cs`
- `apps/api/Features/Payments/Models/Responses/PaymentTransactionDto.cs`
- `apps/api/Features/Payments/Models/PaymentStatus.cs` - Enum
- `apps/api/Features/Payments/Models/PaymentMethodType.cs` - Enum
- `apps/api/Features/Payments/Models/RefundStatus.cs` - Enum

### Database Context
- `apps/api/Data/ApplicationDbContext.cs` - Lines 119, 224, 234, 244

### Migrations
- `apps/api/Migrations/20251127064731_InitialCreate.cs` - Created both tables
- `apps/api/Migrations/20260223030627_AddCreditCardAndPayPalHashFields.cs` - Added CC fields to TicketPurchase
- ConsolidatePaymentTrackingToTicketPurchases migration (referenced in comments)

## Related Issues
- Logging & Observability project (see `/docs/functional-areas/logging-observability/`) - CC failure reporting depends on either this consolidation OR the logging project
- Original trigger: Authorize.net CC failure details not being persisted

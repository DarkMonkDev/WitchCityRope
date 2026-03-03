# Payment System Deep Dive Research

**Date**: 2026-03-02
**Status**: Research Complete — All identified issues resolved or documented as future work
**Supersedes**: Initial README.md research (still valid but incomplete)
**Implementation**: See [implementation-plan.md](./implementation-plan.md) for what was done

## Executive Summary

The payment system has evolved organically into **two parallel payment architectures** that don't communicate. Through five parallel research tracks covering entities, services, endpoints, webhooks/frontend, and DTOs, we've identified **1 critical bug**, **3 major architectural problems**, and **10+ inconsistencies** that need resolution.

The most important finding beyond the initial research: **a critical bug in ParticipationEndpoints admin refund** that passes a `Payment.Id` to `RefundService`, which expects a `TicketPurchaseId`. This means admin refunds via the participation flow will always fail.

---

## Table of Contents

1. [Critical Bug: ParticipationEndpoints Admin Refund](#1-critical-bug-participationendpoints-admin-refund)
2. [Complete Entity Relationship Map](#2-complete-entity-relationship-map)
3. [Complete Endpoint Inventory](#3-complete-endpoint-inventory)
4. [Payment Flow Analysis (End-to-End)](#4-payment-flow-analysis-end-to-end)
5. [The Three Refund Flows Problem](#5-the-three-refund-flows-problem)
6. [Dead & Orphaned Code Inventory](#6-dead--orphaned-code-inventory)
7. [Frontend-Backend Mismatches](#7-frontend-backend-mismatches)
8. [Serilog Impact on Original Research](#8-serilog-impact-on-original-research)
9. [Transaction Safety Issues](#9-transaction-safety-issues)
10. [Type Inconsistencies](#10-type-inconsistencies)
11. [Findings Not in Original Research](#11-findings-not-in-original-research)
12. [Recommended Fix Priority](#12-recommended-fix-priority)

---

## 1. Critical Bug: ParticipationEndpoints Admin Refund

**Severity**: CRITICAL - Refunds via this path will ALWAYS FAIL
**File**: `apps/api/Features/Participation/Endpoints/ParticipationEndpoints.cs` lines 1063-1235

### The Bug

ParticipationEndpoints admin refund (`POST /api/admin/events/{eventId}/tickets/{userId}/refund`) does this:

```csharp
// Line 1125-1129: Looks up Payment entity
var payment = await context.Payments
    .FirstOrDefaultAsync(p =>
        p.EventRegistrationId == ticketParticipation.Id &&
        p.Status == PaymentStatus.Completed);

// Line 1140-1147: Passes Payment.Id as "PaymentId"
var refundRequest = new ProcessRefundRequest
{
    PaymentId = payment.Id,  // <-- This is a PAYMENT entity ID
    ...
};

var refundResult = await refundService.ProcessRefundAsync(refundRequest, cancellationToken);
```

But `RefundService.ProcessRefundAsync` (line 63-65) does this:

```csharp
// ARCHITECTURE FIX comment says "Now queries TicketPurchases instead of Payments"
var ticketPurchase = await _context.TicketPurchases
    .FirstOrDefaultAsync(tp => tp.Id == request.PaymentId, cancellationToken);
    //                              ^^ Looking for PaymentId in TicketPurchases table!
```

**Result**: RefundService looks for `Payment.Id` in the `TicketPurchases` table. Since Payment and TicketPurchase are separate entities with separate IDs, this will always return null and fail with "Ticket purchase not found."

### Why It Wasn't Caught

This code path is only triggered by admins refunding via the participation management UI (not the admin payment list). The other two refund paths (RefundTicket and ProcessVariableRefund) correctly pass TicketPurchase IDs.

### The Deeper Problem

This bug exists because `ProcessRefundRequest.PaymentId` is semantically ambiguous - it was originally a Payment ID, but after the consolidation migration it became a TicketPurchase ID. The field was never renamed, and this code path was never updated.

---

## 2. Complete Entity Relationship Map

### Primary Entities and Their Relationships

```
TicketPurchase (SINGLE SOURCE OF TRUTH for payment data)
├── Id (PK)
├── TicketTypeId (FK → TicketType)
├── UserId (FK → ApplicationUser)
├── RecordedByStaffId (FK → ApplicationUser, nullable)
├── PaymentStatus: string ("Pending", "Completed", "Failed", "PartiallyRefunded", "Refunded", "Confirmed")
├── PaymentMethod: string ("authorize-net", "PayPal", "cash", "rsvp", "Venmo", "authnet-pending")
├── TotalPrice: decimal
├── CC Fields: EncryptedAuthNetTransactionId, CreditCardLastFour, CreditCardType
├── PayPal Fields: EncryptedPayPalOrderId, EncryptedPayPalPayerId, EncryptedPayPalCaptureId, PayPalOrderIdHash
├── EventAttendances[] (navigation)
└── PaymentRefunds[] (via TicketPurchaseId FK on PaymentRefund)

Payment (ORPHANED - created but never updated downstream)
├── Id (PK)
├── EventRegistrationId (FK → ??? - no real entity, maps to EventAttendance.Id)
├── UserId (FK → ApplicationUser)
├── AmountValue + Currency (Money value object pattern)
├── Status: PaymentStatus enum (int storage)
├── PaymentMethodType: PaymentMethodType enum (int storage)
├── PayPal Fields: EncryptedPayPalOrderId, EncryptedPayPalPayerId, EncryptedPayPalCaptureId
├── Refund Fields: RefundAmountValue, RefundCurrency, RefundedAt, RefundedByUserId
├── Metadata: Dictionary<string, object> (JSONB)
├── AuditLogs[] (navigation - PaymentAuditLog)
├── Refunds[] (navigation - BROKEN, FK was moved to TicketPurchase)
└── Failures[] (navigation - PaymentFailure, never created)

PaymentRefund (PRODUCTION - correctly migrated)
├── Id (PK)
├── TicketPurchaseId (FK → TicketPurchase) [RENAMED FROM OriginalPaymentId]
├── RefundAmountValue + RefundCurrency
├── RefundStatus: enum (Processing, Completed, Failed, Cancelled)
├── ProcessedByUserId (FK → ApplicationUser)
└── Metadata: Dictionary<string, object> (JSONB)

PaymentFailure (DEAD CODE - zero active code paths)
├── Id (PK)
├── PaymentId (FK → Payment) [NOT TicketPurchase]
└── Never created by any code path

PaymentAuditLog (PARTIALLY ORPHANED)
├── Id (PK)
├── PaymentId (FK → Payment) [NOT TicketPurchase]
└── Only written during Payment creation, never updated

PaymentMethod (DEAD CODE - saved card feature not implemented)
├── Id (PK)
├── UserId (FK → ApplicationUser)
├── EncryptedStripePaymentMethodId (references Stripe, not AuthNet)
└── Never used by any endpoint
```

### EventRegistrationId Mystery

`Payment.EventRegistrationId` does NOT reference an "EventRegistration" table (no such table exists). Based on the ParticipationEndpoints code (line 1127), it's used to store `EventAttendance.Id`:

```csharp
p.EventRegistrationId == ticketParticipation.Id  // ticketParticipation is EventAttendance
```

And in KioskPaymentEndpoints (line 284):

```csharp
EventRegistrationId = attendee.Id  // attendee is EventAttendee
```

So `EventRegistrationId` is used inconsistently - sometimes it's an EventAttendance ID, sometimes an EventAttendee ID. There's no FK constraint enforcing either relationship.

---

## 3. Complete Endpoint Inventory

### Payment Processing Endpoints

| Route | Method | File | Primary Entity | Status |
|-------|--------|------|---------------|--------|
| `/api/checkout/credit-card` | POST | CheckoutEndpoints.cs | TicketPurchase | PRODUCTION - Atomic 4-stage CC checkout |
| `/api/payments/credit-card` | POST | CreditCardEndpoints.cs | TicketPurchase | PRODUCTION - Standalone CC (broken design) |
| `/api/paypal/create-order` | POST | PayPalCheckoutEndpoints.cs | TicketPurchase | PRODUCTION - PayPal order creation |
| `/api/paypal/capture-order` | POST | PayPalCheckoutEndpoints.cs | TicketPurchase | PRODUCTION - PayPal capture |
| `/api/payments/process` | POST | PaymentEndpoints.cs | **Payment** | ORPHANED - Creates Payment entity |
| `/api/kiosk/events/{id}/payments/cash` | POST | KioskPaymentEndpoints.cs | **Payment** | PRODUCTION - Cash door sales (uses Payment) |

### Payment Query Endpoints

| Route | Method | File | Primary Entity | Status |
|-------|--------|------|---------------|--------|
| `/api/admin/payments` | GET | AdminPaymentEndpoints.cs | TicketPurchase | PRODUCTION - Via PaymentListService |
| `/api/payments/{id}` | GET | PaymentEndpoints.cs | **Payment** | ORPHANED - Reads Payment entity |
| `/api/payments/registration/{id}/status` | GET | PaymentEndpoints.cs | **Payment** | ORPHANED - Reads Payment status |

### Refund Endpoints

| Route | Method | File | Primary Entity | Status |
|-------|--------|------|---------------|--------|
| `/api/admin/refunds/{ticketId}` | POST | RefundEndpoints.cs | TicketPurchase | PRODUCTION - Full refund via RefundTicket |
| `/api/payments/transactions/{id}/refund` | POST | RefundEndpoints.cs | TicketPurchase | PRODUCTION - Variable refund via ProcessVariableRefund |
| `/api/payments/{id}/refund` | POST | PaymentEndpoints.cs | **Payment** | ORPHANED - Uses Payment entity |
| `/api/admin/events/{id}/tickets/{userId}/refund` | POST | ParticipationEndpoints.cs | **Payment** → RefundService(TicketPurchase) | **BROKEN** - Entity mismatch bug |

### Ticket/RSVP Endpoints

| Route | Method | File | Primary Entity | Status |
|-------|--------|------|---------------|--------|
| `/api/events/{id}/tickets` | POST | ParticipationEndpoints.cs | TicketPurchase | PRODUCTION |
| `/api/events/{id}/purchase-ticket` | POST | ParticipationEndpoints.cs | TicketPurchase | DUPLICATE of above |
| `/api/events/{id}/rsvp` | POST | ParticipationEndpoints.cs | EventAttendance | PRODUCTION |

### Webhook Endpoints

| Route | Method | File | Primary Entity | Status |
|-------|--------|------|---------------|--------|
| `/api/webhooks/paypal` | POST | WebhookEndpoints.cs | TicketPurchase | PRODUCTION |
| `/api/webhooks/paypal/health` | GET | WebhookEndpoints.cs | N/A | PRODUCTION |

### SSE/Real-time

| Route | Method | File | Status |
|-------|--------|------|--------|
| `/api/kiosk/payment-stream/{token}` | GET | KioskPaymentEndpoints.cs | PRODUCTION |
| `/api/kiosk/payments/health` | GET | KioskPaymentEndpoints.cs | PRODUCTION |

---

## 4. Payment Flow Analysis (End-to-End)

### Credit Card Payment (Working Correctly)

```
Frontend (CreditCardForm.tsx)
  │ Accept.js tokenizes card → nonce + dataDescriptor
  │ Card data NEVER touches WCR server (PCI compliance)
  ▼
POST /api/checkout/credit-card (CheckoutEndpoints.cs)
  │
  ├── STAGE 1: Validate (CSRF, amount, nonce, idempotency key)
  │     └── Idempotency: Check existing TicketPurchase by key
  │
  ├── STAGE 2: Create PENDING TicketPurchase
  │     └── AttendanceService.CreateTicketPurchaseAsync()
  │     └── PaymentMethod = "authnet-pending"
  │
  ├── STAGE 3: Charge card
  │     └── AuthorizeNetService.ProcessPaymentWithNonceAsync()
  │     ├── SUCCESS → Continue to Stage 4
  │     └── FAILURE → RollbackPendingPurchasesAsync()
  │           └── TicketPurchase.PaymentStatus = "Failed"
  │           └── EventAttendance.Status = Cancelled
  │           ⚠️ CC failure details logged via Serilog only
  │           ⚠️ NO PaymentFailure record created
  │
  └── STAGE 4: Finalize
        └── TicketPurchase.EncryptedAuthNetTransactionId = encrypted
        └── TicketPurchase.CreditCardLastFour = last4
        └── TicketPurchase.PaymentStatus = "Completed"
        └── TicketPurchase.PaymentMethod = "authorize-net"
        ⚠️ If finalization fails AFTER charge:
            └── Attempts auto-void via AuthorizeNetService.RefundAsync()
            └── Logs as CRITICAL

Entity touched: TicketPurchase ONLY
Payment entity: NEVER created or touched
```

### PayPal Payment (Working Correctly)

```
Frontend (PayPalButton.tsx)
  │
  ├── createOrder callback:
  │     POST /api/paypal/create-order (PayPalCheckoutEndpoints.cs)
  │       └── Creates PayPal order via PayPalService
  │       └── Stores EncryptedPayPalOrderId + PayPalOrderIdHash on TicketPurchase
  │       └── Sets PaymentMethod = "PayPal"
  │       └── Returns OrderId to frontend
  │
  └── onApprove callback (user completes PayPal):
        POST /api/paypal/capture-order (PayPalCheckoutEndpoints.cs)
          └── Captures via PayPalService
          └── Finds TicketPurchase by PayPalOrderIdHash
          └── Stores EncryptedPayPalCaptureId, EncryptedPayPalPayerId
          └── TicketPurchase.PaymentStatus = "Completed"
          └── TicketPurchase.ProcessedAt = now

        THEN (async):
        POST /api/webhooks/paypal (WebhookEndpoints.cs)
          └── Confirms status on TicketPurchase (idempotent)
          └── If TicketPurchase already updated → no-op
          └── If TicketPurchase not found → graceful warning

Entity touched: TicketPurchase ONLY
Payment entity: NEVER created or touched
```

**IMPORTANT FINDING**: The initial research said PayPal used `POST /api/payments/process` (PaymentEndpoints.cs) which creates a Payment entity. This is WRONG for the current flow. The PayPalCheckoutEndpoints.cs replaced that flow and works directly with TicketPurchase. The old PaymentEndpoints.cs PayPal flow is orphaned.

### Cash Payment (Still Uses Payment Entity)

```
Staff at door:
  POST /api/kiosk/events/{eventId}/payments/cash (KioskPaymentEndpoints.cs)
    └── Validates session token (X-CheckIn-Token header)
    └── Creates Payment entity (NOT TicketPurchase!)
        └── EventRegistrationId = attendee.Id (EventAttendee)
        └── Status = PaymentStatus.Completed (immediate)
        └── PaymentMethodType = Cash
    └── Sends SSE notification to kiosk

Entity touched: Payment ONLY
TicketPurchase: NEVER created
⚠️ This means cash door sales DON'T appear in PaymentListService
    (which queries TicketPurchases table)
```

### Standalone Credit Card (CreditCardEndpoints - Broken Design)

```
POST /api/payments/credit-card (CreditCardEndpoints.cs)
  │
  ├── Charges card FIRST (no pending ticket)
  │     └── AuthorizeNetService.ProcessPaymentWithNonceAsync()
  │
  └── Updates TicketPurchase SECOND (if TicketPurchaseId provided)
        └── If TicketPurchaseId is null → payment charged but not linked
        └── If TicketPurchase not found → logs warning, returns 200 OK anyway
        └── No rollback if update fails
        └── No idempotency support

⚠️ BROKEN: Charges money first, persists second
⚠️ BROKEN: Returns success even when persistence fails
⚠️ No idempotency protection
```

---

## 5. The Three Refund Flows Problem

The system has **three separate refund code paths** that handle the same operation differently:

### Flow 1: RefundTicket (Full Refund + Optional RSVP Cancel)

**Route**: `POST /api/admin/refunds/{ticketId}`
**File**: `apps/api/Features/Payments/Commands/RefundTicket.cs`
**Entity**: TicketPurchase (CORRECT)

- Looks up TicketPurchase by ticketId
- Validates IsPaymentCompleted
- Checks for existing refund (blocks if already refunded - no partial support!)
- Passes TicketPurchase.Id as PaymentId to RefundService
- Updates TicketPurchase.PaymentStatus = "Refunded"
- Optionally cancels EventAttendance

**Authorization**: Administrator OR Teacher

### Flow 2: ProcessVariableRefund (Partial/Full Refund, NO RSVP Cancel)

**Route**: `POST /api/payments/transactions/{transactionId}/refund`
**File**: `apps/api/Features/Payments/Commands/ProcessVariableRefund.cs`
**Entity**: TicketPurchase (CORRECT)

- Looks up TicketPurchase by transactionId
- Validates IsPaymentCompleted
- Calculates remaining refundable amount (supports multiple partial refunds)
- Passes TicketPurchase.Id as PaymentId to RefundService
- Sets status to "PartiallyRefunded" or "Refunded" based on total
- Does NOT cancel RSVP/ticket (by design - sliding scale adjustment)

**Authorization**: Administrator OR Teacher

### Flow 3: ParticipationEndpoints Admin Refund (BROKEN)

**Route**: `POST /api/admin/events/{eventId}/tickets/{userId}/refund`
**File**: `apps/api/Features/Participation/Endpoints/ParticipationEndpoints.cs` lines 1063-1235
**Entity**: Payment → RefundService(TicketPurchase) = BUG

- Looks up EventAttendance for user+event
- Looks up Payment by EventRegistrationId == EventAttendance.Id
- Passes **Payment.Id** as PaymentId to RefundService
- RefundService tries to find this ID in **TicketPurchases** table → FAILS
- Would also update EventAttendance status and EventAttendee status
- Handles volunteer shift cancellation

**Authorization**: Administrator ONLY (NOT Teacher)

### Comparison Table

| Feature | RefundTicket | ProcessVariableRefund | ParticipationEndpoints |
|---------|-------------|----------------------|----------------------|
| Works? | YES | YES | **NO (BUG)** |
| Partial refunds | NO (blocks on existing) | YES | N/A (broken) |
| Cancels RSVP | Optional | Never | Optional |
| Cancels attendance | Optional | Never | Yes |
| Updates volunteers | No (RefundService does) | No (RefundService does) | Pre-fetches names |
| Auth | Admin+Teacher | Admin+Teacher | Admin ONLY |
| Entity used | TicketPurchase | TicketPurchase | Payment (wrong) |

---

## 6. Dead & Orphaned Code Inventory

### Completely Dead (Zero Active Code Paths)

| Item | File | Lines | Why Dead |
|------|------|-------|----------|
| PaymentFailure entity | `Payments/Entities/PaymentFailure.cs` | 220 | FK to Payment, but no code creates PaymentFailure records. The only potential call site (PaymentService.LogPaymentFailureAsync) is itself dead because PaymentService is orphaned. |
| PaymentFailureConfiguration | `Payments/Configuration/PaymentFailureConfiguration.cs` | ~100 | Configuration for dead entity |
| PaymentMethod entity | `Payments/Entities/PaymentMethod.cs` | ~150 | Saved card feature never implemented. References "Stripe" but system uses Authorize.net. |
| PaymentMethodConfiguration | `Payments/Configuration/PaymentMethodConfiguration.cs` | ~100 | Configuration for dead entity |
| `/api/events/{id}/purchase-ticket` | `ParticipationEndpoints.cs` | 342-437 | Exact duplicate of `/api/events/{id}/tickets`, created "for compatibility" |

### Orphaned (Created But Never Updated Downstream)

| Item | File | Status |
|------|------|--------|
| Payment entity records | `Payments/Entities/Payment.cs` | Created by PaymentService and KioskPaymentEndpoints. Never updated by webhooks, refunds, or admin. Status stuck at creation value forever. |
| PaymentAuditLog records | `Payments/Entities/PaymentAuditLog.cs` | Only written during Payment creation. Should track full lifecycle but Payment is never updated. |
| Payment.Refunds navigation | `Payment.cs` line ~112 | PaymentRefund FK was moved to TicketPurchaseId. Payment.Refunds collection is always empty. |
| Payment.Failures navigation | `Payment.cs` line ~113 | PaymentFailure is never created. Payment.Failures is always empty. |

### Partially Orphaned (Some Use Remaining)

| Item | Active Use | Orphaned Use |
|------|-----------|--------------|
| PaymentService | KioskPaymentEndpoints (cash) | PaymentEndpoints (PayPal - replaced by PayPalCheckoutEndpoints) |
| `/api/payments/process` | Possibly still called by some frontend code | PayPalCheckoutEndpoints has replaced this flow |
| `/api/payments/{id}` | Could be used for reading | Data is stale (never updated) |
| `/api/payments/{id}/refund` | Unknown | Uses Payment entity (likely broken) |

---

## 7. Frontend-Backend Mismatches

### Frontend Calls That Work

| Frontend File | Endpoint Called | Backend Status |
|--------------|----------------|---------------|
| `payments.ts` | `POST /api/checkout/credit-card` | WORKS - CheckoutEndpoints |
| `PayPalButton.tsx` | `POST /api/paypal/create-order` | WORKS - PayPalCheckoutEndpoints |
| `PayPalButton.tsx` | `POST /api/paypal/capture-order` | WORKS - PayPalCheckoutEndpoints |
| `mutations.ts` | `POST /api/events/{id}/purchase-ticket` | WORKS - ParticipationEndpoints (duplicate route) |

### Frontend Calls That May Not Work

| Frontend File | Endpoint Called | Backend Status |
|--------------|----------------|---------------|
| `paymentApi.ts` | `GET /api/payments/{id}` | Returns stale Payment data |
| `paymentApi.ts` | `GET /api/payments/{id}/status` | Returns stale Payment status |
| `paymentApi.ts` | `POST /api/payments/{id}/refund` | Uses Payment entity, may not work correctly |
| `paymentApi.ts` | `POST /api/payments/process` | Creates orphaned Payment record |
| `paymentApi.ts` | `POST /api/payments/credit-card` | Broken design (charge before persist) |

### Frontend Calls to Non-Existent Endpoints

| Frontend File | Endpoint Called | Status |
|--------------|----------------|--------|
| `paymentApi.ts` | `GET /api/payments/user` | NOT IMPLEMENTED on backend |
| `paymentApi.ts` | `GET /api/payments/event/{eventId}` | NOT IMPLEMENTED on backend |

### Auto-Generated Type Alignment

The frontend types in `packages/shared-types` are auto-generated from the OpenAPI spec and match the backend DTOs. No manual DTO mismatches found. However, the frontend also has manual types in `features/payments/types/payment.types.ts` (373 lines) that may overlap with generated types - needs verification.

---

## 8. Serilog Impact on Original Research

### What Changed Since Initial Research

Serilog has been implemented with a PostgreSQL sink (`logging.application_logs` table). This impacts several decisions from the original research:

1. **PaymentFailure entity is now even more redundant**: CC failure details are captured as structured log events in the Serilog PostgreSQL table. The `logging.application_logs` table has:
   - `source_context` (class name filtering)
   - `properties` (JSONB - contains all structured parameters)
   - `level` (numeric severity)
   - `user_id`, `correlation_id` (for tracing)

2. **PaymentAuditLog entity overlaps with Serilog**: All payment operations are already logged to Serilog with structured properties. PaymentAuditLog provides a business-level audit trail but since it only captures Payment creation (not lifecycle), Serilog is actually more complete.

3. **CC failure details ARE being persisted**: The initial research said CC failure details were "lost (only in ILogger)". With Serilog's PostgreSQL sink, they ARE queryable:
   ```sql
   SELECT * FROM logging.application_logs
   WHERE source_context LIKE '%AuthorizeNet%'
   AND level >= 3
   ```
   However, they're in operational logs, not a dedicated business table.

4. **Sensitive data masking is active**: Serilog is configured to mask: password, token, secret, key, authorization, cookie, nonce, creditcard. This means CC failure details in logs won't contain raw card data.

### Recommendation Update

Given Serilog implementation:
- **PaymentFailure**: Remove entirely. Serilog captures all failure details with more context.
- **PaymentAuditLog**: Consider whether business audit needs are met by Serilog or if a dedicated audit table for TicketPurchase lifecycle is needed (compliance/regulatory consideration).

---

## 9. Transaction Safety Issues

### Multiple SaveChangesAsync Calls in Single Operations

| Service/Endpoint | Saves | Risk |
|-----------------|-------|------|
| PaymentService.ProcessPaymentAsync | 2 (line 85, 122) | If PayPal API succeeds but second save fails, Payment exists without OrderId |
| RefundService.ProcessRefundAsync | 2+ (line 115, 281, + audit helpers) | Multiple saves in refund flow without explicit transaction |
| CheckoutEndpoints | 1 main + rollback | Good design - but critical failure path after charge is complex |
| RefundService.LogRefundRetryAsync | 1 (line 598) | Missing cancellationToken parameter |
| PayPalCheckoutEndpoints.CreateOrder | 1 (line 119) | Good - single save after PayPal call |
| PayPalCheckoutEndpoints.CaptureOrder | 1 (line 190) | Good - single save after capture |

### No Explicit Transaction Scopes

None of the payment services use `IDbContextTransaction` or `TransactionScope`. All rely on EF Core's implicit transaction per `SaveChangesAsync()`. For multi-save operations, this means partial state can be persisted.

### Retry Logic Inconsistency

| Payment Method | Retry on Failure | Max Retries | Backoff |
|---------------|-----------------|-------------|---------|
| PayPal refund | YES | 5 | Exponential (2^n seconds) |
| Authorize.net refund | NO | 0 | N/A |
| PayPal capture | NO | 0 | N/A |
| CC charge | NO | 0 | N/A |

---

## 10. Type Inconsistencies

### Payment Status: String vs Enum

| Entity/Context | Type | Values |
|---------------|------|--------|
| TicketPurchase.PaymentStatus | string | "Pending", "Completed", "Confirmed", "Failed", "PartiallyRefunded", "Refunded" |
| Payment.Status | PaymentStatus enum (int) | Pending=0, Completed=1, Failed=2, Refunded=3, PartiallyRefunded=4 |
| PaymentRefund.RefundStatus | RefundStatus enum (int) | Processing=0, Completed=1, Failed=2, Cancelled=3 |

**TicketPurchase has "Confirmed" status**: This is a legacy PayPal response status. It's treated as equivalent to "Completed" via `IsPaymentCompleted`:
```csharp
string.Equals(PaymentStatus, "Confirmed", StringComparison.OrdinalIgnoreCase)
```
The PaymentStatus enum does NOT have a "Confirmed" value.

### Payment Method: String vs Enum

| Entity/Context | Type | Values |
|---------------|------|--------|
| TicketPurchase.PaymentMethod | string | "authorize-net", "authnet-pending", "PayPal", "cash", "rsvp", "Venmo" |
| Payment.PaymentMethodType | PaymentMethodType enum (int) | SavedCard=0, NewCard=1, BankTransfer=2, PayPal=3, Venmo=4, Cash=5 |

**Mismatch**: TicketPurchase uses "authorize-net" (string), Payment uses SavedCard/NewCard (enum). There's no enum value for "authorize-net" or "rsvp".

### Amount Storage

| Entity | Field | Type | Notes |
|--------|-------|------|-------|
| TicketPurchase | TotalPrice | decimal | Raw decimal, no currency |
| Payment | AmountValue + Currency | decimal + varchar(3) | Money value object pattern |
| PaymentRefund | RefundAmountValue + RefundCurrency | decimal + varchar(3) | Money value object pattern |

TicketPurchase assumes USD (hardcoded in PaymentListService and other places).

---

## 11. Findings Not in Original Research

### 1. PayPalCheckoutEndpoints.cs Was Completely Missing

The initial research didn't mention `PayPalCheckoutEndpoints.cs` which provides:
- `POST /api/paypal/create-order` - Creates PayPal order, stores on TicketPurchase
- `POST /api/paypal/capture-order` - Captures payment, updates TicketPurchase

This is the ACTIVE PayPal payment flow. The `PaymentEndpoints.cs POST /api/payments/process` flow (which creates Payment entities) is the OLD flow that's been replaced.

### 2. CreditCardEndpoints Has Critical Design Flaws

Beyond being a parallel path to CheckoutEndpoints:
- Charges card BEFORE creating/updating ticket record
- Returns 200 OK even if TicketPurchase update fails
- No idempotency support
- No rollback if persistence fails
- If TicketPurchaseId is null, payment is charged but never linked to anything

### 3. Kiosk Cash Payments Don't Appear in Admin List

KioskPaymentEndpoints creates Payment entities (not TicketPurchase), but PaymentListService queries TicketPurchases. This means **cash door sales are invisible in the admin payment list**.

### 4. Three Separate Refund Endpoint Patterns

- RefundTicket (vertical slice command via RefundEndpoints)
- ProcessVariableRefund (vertical slice command via RefundEndpoints)
- ParticipationEndpoints inline lambda (broken)

### 5. ProcessRefundRequest.PaymentId Is Semantically Wrong

The field `ProcessRefundRequest.PaymentId` is used to pass TicketPurchase IDs (after the consolidation). The field name is misleading and caused the ParticipationEndpoints bug.

### 6. RefundTicket Blocks Multiple Refunds

RefundTicket.cs (line 112-122) checks for ANY existing refund and blocks:
```csharp
if (existingRefund != null)
{
    return Results.Problem(title: "Already Refunded", ...);
}
```
This means you can't do a partial refund via RefundTicket and then do another partial refund later. Only ProcessVariableRefund supports multiple partial refunds.

### 7. EventRegistrationId References Two Different Entities

- ParticipationEndpoints: `EventRegistrationId = EventAttendance.Id`
- KioskPaymentEndpoints: `EventRegistrationId = EventAttendee.Id`

These are different entities with different IDs.

### 8. Money Value Object Exists But Only Used by Payment Entity

The `Money` value object (`Features/Payments/ValueObjects/Money.cs`) is well-designed with:
- Currency validation (USD, EUR, GBP, CAD)
- Arithmetic operators
- Sliding scale calculation
- PayPal formatting

But TicketPurchase uses raw `decimal TotalPrice` with no currency field. The Money VO is only used in service layer calculations, not persisted on TicketPurchase.

### 9. PaymentMethod Entity References Stripe

`PaymentMethod.cs` has `EncryptedStripePaymentMethodId` - but the system uses Authorize.net, not Stripe. This entity was designed for a saved-card feature that was never implemented with the wrong payment processor.

### 10. Duplicate Ticket Purchase Route

`/api/events/{id}/purchase-ticket` (ParticipationEndpoints line 342) is an exact duplicate of `/api/events/{id}/tickets` (line 227). Comment says "for compatibility with tests."

---

## 12. Recommended Fix Priority

> **Update (2026-03-03)**: Items 1-6 and 10 have been resolved by the 7-phase implementation plan.
> See [implementation-plan.md](./implementation-plan.md) for details.
> Items 3 (duplicate route), 7, 8, 9 remain as future work — documented in [README.md](./README.md).

### Priority 1: Critical Bug Fix (Must Fix)

1. **~~Fix ParticipationEndpoints admin refund~~** ✅ RESOLVED (Phase 1)
   - Fixed: Now queries TicketPurchase via `ticketParticipation.TicketPurchaseId`

### Priority 2: High Impact Cleanup (Should Fix)

2. **~~Migrate cash payments to TicketPurchase~~** ✅ RESOLVED (Phase 2)
   - KioskPaymentEndpoints now creates TicketPurchase + EventAttendance

3. **~~Remove or archive dead code~~** ✅ RESOLVED (Phases 4, 5, 6, 7)
   - All dead entities, services, endpoints, and frontend code removed
   - Database tables dropped via migrations
   - **Remaining**: Duplicate `/api/events/{id}/purchase-ticket` route NOT removed (deferred)

4. **~~Fix CreditCardEndpoints design flaws~~** ✅ RESOLVED (Phase 4)
   - CreditCardEndpoints.cs deleted entirely (confirmed dead — frontend never calls it)

### Priority 3: Architecture Consolidation (Plan Carefully)

5. **~~Deprecate/remove Payment entity~~** ✅ RESOLVED (Phase 7)
   - Payment entity, configuration, and Payments table fully removed

6. **~~Rename ProcessRefundRequest.PaymentId to TicketPurchaseId~~** ✅ RESOLVED (Phase 3)
   - Renamed across all callers

7. **Consolidate refund flows** — NOT ADDRESSED (future work)
   - Three refund endpoints is confusing
   - Consider: One flexible endpoint with parameters for full/partial/with-rsvp-cancel

### Priority 4: Nice-to-Have Improvements (Future)

8. **Migrate TicketPurchase to enum-based PaymentStatus** — NOT ADDRESSED (future work)
   - Replace string comparisons with type-safe enum
   - Preserve "Confirmed" as alias for Completed during migration

9. **Add currency field to TicketPurchase** — NOT ADDRESSED (future work)
   - Currently assumes USD everywhere
   - Or formalize USD-only as a business rule

10. **~~Clean up frontend payment types~~** ✅ PARTIALLY RESOLVED (Phase 5)
    - Dead types, hooks, pages, and API calls removed
    - Auto-generated types remain as source of truth

---

## Files Referenced in This Research

### Entities
- `apps/api/Models/TicketPurchase.cs`
- `apps/api/Features/Payments/Entities/Payment.cs`
- `apps/api/Features/Payments/Entities/PaymentFailure.cs`
- `apps/api/Features/Payments/Entities/PaymentRefund.cs`
- `apps/api/Features/Payments/Entities/PaymentAuditLog.cs`
- `apps/api/Features/Payments/Entities/PaymentMethod.cs`
- `apps/api/Features/Payments/ValueObjects/Money.cs`

### Services
- `apps/api/Features/Payments/Services/PaymentService.cs`
- `apps/api/Features/Payments/Services/AuthorizeNetService.cs`
- `apps/api/Features/Payments/Services/PayPalService.cs`
- `apps/api/Features/Payments/Services/RefundService.cs`
- `apps/api/Features/Payments/Services/PaymentListService.cs`
- `apps/api/Features/Participation/Services/AttendanceService.cs`

### Endpoints
- `apps/api/Features/Payments/Endpoints/CheckoutEndpoints.cs`
- `apps/api/Features/Payments/Endpoints/CreditCardEndpoints.cs`
- `apps/api/Features/Payments/Endpoints/PayPalCheckoutEndpoints.cs`
- `apps/api/Features/Payments/Endpoints/PaymentEndpoints.cs`
- `apps/api/Features/Payments/Endpoints/KioskPaymentEndpoints.cs`
- `apps/api/Features/Payments/Endpoints/AdminPaymentEndpoints.cs`
- `apps/api/Features/Payments/Endpoints/RefundEndpoints.cs`
- `apps/api/Features/Payments/Endpoints/WebhookEndpoints.cs`
- `apps/api/Features/Participation/Endpoints/ParticipationEndpoints.cs`

### Commands
- `apps/api/Features/Payments/Commands/RefundTicket.cs`
- `apps/api/Features/Payments/Commands/ProcessVariableRefund.cs`

### Webhooks
- `apps/api/Features/Webhooks/Services/PayPalWebhookProcessingService.cs`
- `apps/api/Features/Webhooks/Services/PayPalWebhookVerificationService.cs`

### Frontend
- `apps/web/src/features/payments/api/paymentApi.ts`
- `apps/web/src/features/payments/components/checkout/CreditCardForm.tsx`
- `apps/web/src/features/payments/components/PayPalButton.tsx`
- `apps/web/src/features/payments/pages/EventPaymentPage.tsx`
- `apps/web/src/features/payments/types/payment.types.ts`
- `apps/web/src/lib/api/services/payments.ts`

### Configuration
- `apps/api/Features/Payments/Configuration/PaymentConfiguration.cs`
- `apps/api/Features/Payments/Configuration/PaymentFailureConfiguration.cs`
- `apps/api/Features/Payments/Configuration/PaymentAuditLogConfiguration.cs`
- `apps/api/Features/Payments/Configuration/PaymentRefundConfiguration.cs`
- `apps/api/Features/Payments/Configuration/PaymentMethodConfiguration.cs`
- `apps/api/Data/ApplicationDbContext.cs`
- `apps/api/Features/Shared/Extensions/ServiceCollectionExtensions.cs`

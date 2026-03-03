# Payment System: Dead Code & Liveness Analysis

**Date**: 2026-03-02
**Purpose**: Trace every call chain from frontend UI → backend endpoint → entity to determine what code is actually reachable in production vs dead code.

## Methodology

Instead of documenting code structure (which can make dead code look alive), this analysis traces **backwards from entry points**: what does the frontend actually call? What does the user actually trigger?

---

## 1. ACTIVE Payment Flows (What Actually Runs in Production)

### Flow A: Credit Card Checkout (ACTIVE - TicketPurchase)

```
User clicks "Pay" on EventPaymentPage
  → CreditCardForm.tsx tokenizes via Accept.js
  → EventPaymentPage.tsx calls checkout.mutateAsync()
    → useCheckout() hook (lib/api/hooks/usePayments.ts:76)
      → payments.ts:104 → POST /api/checkout/credit-card
        → CheckoutEndpoints.cs → creates/updates TicketPurchase
          → AuthorizeNetService processes nonce
```

**Frontend files (ACTIVE)**:
- `apps/web/src/features/payments/pages/EventPaymentPage.tsx` - page component
- `apps/web/src/features/payments/components/checkout/CreditCardForm.tsx` - card form
- `apps/web/src/lib/api/hooks/usePayments.ts` - `useCheckout()` hook
- `apps/web/src/lib/api/services/payments.ts` - `checkout()` function

**Backend files (ACTIVE)**:
- `apps/api/Features/Payments/Endpoints/CheckoutEndpoints.cs`
- `apps/api/Features/Payments/Services/AuthorizeNetService.cs`
- `apps/api/Features/Participation/Services/AttendanceService.cs`

**Entity**: TicketPurchase ONLY

---

### Flow B: PayPal Checkout (ACTIVE - TicketPurchase)

```
User clicks PayPal button on EventPaymentPage
  → PayPalButton.tsx createOrder callback
    → POST /api/paypal/create-order (PayPalCheckoutEndpoints.cs)
      → Creates PayPal order, stores OrderId hash on TicketPurchase
  → User approves in PayPal popup
  → PayPalButton.tsx onApprove callback
    → POST /api/paypal/capture-order (PayPalCheckoutEndpoints.cs)
      → Captures payment, updates TicketPurchase to "Completed"
  → Async: PayPal webhook confirms (idempotent)
    → POST /api/webhooks/paypal → updates TicketPurchase (no-op if already done)
```

**Frontend files (ACTIVE)**:
- `apps/web/src/features/payments/components/PayPalButton.tsx` - calls `/api/paypal/*` DIRECTLY (not through paymentApi.ts)

**Backend files (ACTIVE)**:
- `apps/api/Features/Payments/Endpoints/PayPalCheckoutEndpoints.cs`
- `apps/api/Features/Payments/Services/PayPalService.cs`
- `apps/api/Features/Webhooks/Services/PayPalWebhookProcessingService.cs`
- `apps/api/Features/Webhooks/Services/PayPalWebhookVerificationService.cs`

**Entity**: TicketPurchase ONLY

---

### Flow C: Admin Full Refund (ACTIVE - TicketPurchase)

```
Admin clicks refund on admin payment list
  → useRefundTicket hook (admin/payments/hooks/useRefundTicket.ts)
    → POST /api/admin/refunds/{ticketId}
      → RefundTicket.cs command → queries TicketPurchase
        → RefundService.ProcessRefundAsync() → queries TicketPurchase
```

**Frontend files (ACTIVE)**:
- `apps/web/src/features/admin/payments/hooks/useRefundTicket.ts`

**Backend files (ACTIVE)**:
- `apps/api/Features/Payments/Endpoints/RefundEndpoints.cs`
- `apps/api/Features/Payments/Commands/RefundTicket.cs`
- `apps/api/Features/Payments/Services/RefundService.cs`

**Entity**: TicketPurchase via RefundService

---

### Flow D: Admin Variable/Partial Refund (ACTIVE - TicketPurchase)

```
Admin clicks partial refund on payment list OR event form
  → useVariableRefund hook OR EventForm.tsx direct call
    → POST /api/payments/transactions/{transactionId}/refund
      → ProcessVariableRefund.cs command → queries TicketPurchase
        → RefundService.ProcessRefundAsync() → queries TicketPurchase
```

**Frontend files (ACTIVE)**:
- `apps/web/src/features/admin/payments/hooks/useVariableRefund.ts`
- `apps/web/src/components/events/EventForm.tsx` (line 1235, direct apiClient call)

**Backend files (ACTIVE)**:
- `apps/api/Features/Payments/Endpoints/RefundEndpoints.cs`
- `apps/api/Features/Payments/Commands/ProcessVariableRefund.cs`
- `apps/api/Features/Payments/Services/RefundService.cs`

**Entity**: TicketPurchase via ProcessVariableRefund + RefundService

---

### Flow E: Admin Payment List (ACTIVE - TicketPurchase)

```
Admin visits payment management page
  → usePayments hook (admin/payments/hooks/usePayments.ts)
    → GET /api/admin/payments
      → AdminPaymentEndpoints.cs → PaymentListService
        → Queries TicketPurchases table (NOT Payments)
```

**Backend files (ACTIVE)**:
- `apps/api/Features/Payments/Endpoints/AdminPaymentEndpoints.cs`
- `apps/api/Features/Payments/Services/PaymentListService.cs`

**Entity**: TicketPurchase via PaymentListService

---

### Flow F: Kiosk Cash Payment (ACTIVE - Payment entity!)

```
Staff records cash payment at door
  → POST /api/kiosk/events/{eventId}/payments/cash
    → KioskPaymentEndpoints.cs → creates Payment entity (NOT TicketPurchase)
    → Sends SSE notification to kiosk stream
```

**Frontend files (ACTIVE)**:
- `apps/web/src/features/checkin/hooks/useKioskPaymentStream.ts` (SSE stream only)
- Cash payment UI (TODO: verify which component posts to cash endpoint)

**Backend files (ACTIVE)**:
- `apps/api/Features/Payments/Endpoints/KioskPaymentEndpoints.cs`

**Entity**: **Payment** (NOT TicketPurchase)
**PROBLEM**: These records are invisible in admin payment list (PaymentListService queries TicketPurchases)

---

### Flow G: Ticket Purchase/RSVP (ACTIVE - TicketPurchase)

```
User purchases ticket or RSVPs
  → POST /api/events/{eventId}/tickets (or /purchase-ticket)
    → ParticipationEndpoints → AttendanceService.CreateTicketPurchaseAsync()
      → Creates TicketPurchase + EventAttendance records
```

**Entity**: TicketPurchase via AttendanceService

---

### Flow H: Admin Remove RSVP with Cascading Refund (ACTIVE but BROKEN)

```
Admin removes RSVP and there's an associated ticket
  → DELETE /api/admin/events/{eventId}/participations/{userId}/remove
    → ParticipationEndpoints.cs line 882
      → Reads context.Payments (line 955) by EventRegistrationId
      → Passes payment.Id to RefundService.ProcessRefundAsync()
        → RefundService queries TicketPurchases by that ID → NOT FOUND → FAILS
```

**Entity**: Payment (read) → RefundService expects TicketPurchase → **BUG**
**File**: `ParticipationEndpoints.cs` lines 940-989

---

### Flow I: Admin Refund Ticket via Participation (ACTIVE but BROKEN)

```
Admin refunds ticket from event management
  → POST /api/admin/events/{eventId}/tickets/{userId}/refund
    → ParticipationEndpoints.cs line 1063
      → Reads context.Payments (line 1125) by EventRegistrationId
      → Passes payment.Id to RefundService.ProcessRefundAsync()
        → RefundService queries TicketPurchases by that ID → NOT FOUND → FAILS
```

**Entity**: Payment (read) → RefundService expects TicketPurchase → **BUG**
**File**: `ParticipationEndpoints.cs` lines 1063-1235

---

## 2. DEAD Code: Backend

### Entirely Dead Backend Files

| File | Why Dead | Evidence |
|------|----------|---------|
| `Features/Payments/Endpoints/PaymentEndpoints.cs` | Frontend never calls any of its routes | `paymentApi.processPayment()` is defined but never invoked from any component. `usePayment.processPayment` is exported but never called. |
| `Features/Payments/Endpoints/CreditCardEndpoints.cs` | Frontend never calls `POST /api/payments/credit-card` | `paymentApi.processCreditCardPayment()` is defined in `paymentApi.ts` but never imported or called by any component. `useCheckout()` calls `/api/checkout/credit-card` instead. |
| `Features/Payments/Services/PaymentService.cs` | Only consumer is PaymentEndpoints (dead) and KioskPaymentEndpoints (creates Payment directly, doesn't use PaymentService for cash) | Wait - need to verify kiosk. |
| `Features/Payments/Services/IPaymentService.cs` | Interface for dead PaymentService | Only injected into PaymentEndpoints |
| `Features/Payments/Entities/PaymentFailure.cs` | FK points to Payment; only created by PaymentService.LogPaymentFailureAsync which is only called from ProcessPaymentAsync (dead flow) | Zero records ever created |
| `Features/Payments/Configuration/PaymentFailureConfiguration.cs` | Configuration for dead entity | |
| `Features/Payments/Entities/PaymentMethod.cs` | Saved card feature never implemented; references Stripe (system uses Authorize.net) | Zero references outside entity/config |
| `Features/Payments/Configuration/PaymentMethodConfiguration.cs` | Configuration for dead entity | |
| `Features/Payments/Entities/PaymentAuditLog.cs` | Only created by PaymentService (dead flow) and KioskPaymentEndpoints doesn't create audit logs | Only records would be from PaymentService.ProcessPaymentAsync which is dead |
| `Features/Payments/Configuration/PaymentAuditLogConfiguration.cs` | Configuration for mostly-dead entity | |
| `Features/Payments/Validators/ProcessPaymentApiRequestValidator.cs` | Validates ProcessPaymentApiRequest used only by PaymentEndpoints (dead) | |

### Partially Dead Backend Files

| File | Active Parts | Dead Parts |
|------|-------------|------------|
| `Features/Payments/Entities/Payment.cs` | Created by KioskPaymentEndpoints (cash). Read by ParticipationEndpoints (broken). | All other methods, Refunds/Failures navigations |
| `Features/Payments/Configuration/PaymentConfiguration.cs` | Configures Payment table (still in DB) | Many indexes/constraints for unused features |

### Kiosk PaymentService Usage Verification

KioskPaymentEndpoints creates Payment entities DIRECTLY (line 281: `new Payment { ... }`). It does NOT use `IPaymentService`. So `PaymentService` is completely dead - its only consumer was `PaymentEndpoints`.

---

## 3. DEAD Code: Frontend

### Entirely Dead Frontend Files

| File | Why Dead | Evidence |
|------|----------|---------|
| `features/payments/api/paymentApi.ts` | EVERY method is dead | See method-by-method analysis below |
| `features/payments/hooks/usePayment.ts` | `processPayment()` never called; `paymentData` reads from dead endpoint | Only component importing it (`EventPaymentPage.tsx`) doesn't call `processPayment` |
| `features/payments/types/payment.types.ts` | Types for dead paymentApi | Manual types that duplicate auto-generated ones |
| `pages/payments/PaymentSuccessPage.tsx` | Handles return from PayPal flow through PaymentEndpoints (dead) | Only reachable via `/payment/success` redirect from `usePayment.returnUrl` which is never triggered |
| `pages/payments/PaymentCancelPage.tsx` | Same as above - handles PayPal cancel from dead flow | Only reachable via `/payment/cancel` redirect |

### paymentApi.ts Method-by-Method Liveness

| Method | Route | Called By | Actually Invoked? | Verdict |
|--------|-------|-----------|-------------------|---------|
| `processPayment()` | `POST /api/payments/process` | `usePayment.ts:36` | NO - `usePayment.processPayment` exported but never called from any .tsx | **DEAD** |
| `getPayment()` | `GET /api/payments/{id}` | `usePayment.ts:82`, `PaymentSuccessPage.tsx:45` | `usePayment.paymentData` used by EventPaymentPage but reads from dead Payment entity; PaymentSuccessPage only reachable from dead PayPal flow | **DEAD** |
| `getPaymentStatus()` | `GET /api/payments/{id}/status` | Not called | Not referenced anywhere | **DEAD** |
| `processRefund()` | `POST /api/payments/{id}/refund` | Not called | Not referenced anywhere | **DEAD** |
| `getUserPayments()` | `GET /api/payments/user` | `usePaymentHistory` hook | `usePaymentHistory` exported but never imported by any component; **backend endpoint doesn't exist** | **DEAD** |
| `getEventPayments()` | `GET /api/payments/event/{id}` | `useEventPayments` hook | Exported but never imported by any component; **backend endpoint doesn't exist** | **DEAD** |
| `processCreditCardPayment()` | `POST /api/payments/credit-card` | Not called | Defined in paymentApi.ts but never imported or called | **DEAD** |
| `createPayPalOrder()` | `POST /api/paypal/create-order` | `usePayment.ts:122` | The `usePayment` version is a STUB returning hardcoded data. Real PayPal goes through `PayPalButton.tsx` directly. | **DEAD** (the paymentApi version) |
| `capturePayPalOrder()` | `POST /api/paypal/capture-order` | Not via paymentApi | `PayPalButton.tsx` calls the endpoint directly via apiClient, not through paymentApi | **DEAD** (the paymentApi version) |

### Partially Dead Frontend

| File | Active Parts | Dead Parts |
|------|-------------|------------|
| `features/payments/pages/EventPaymentPage.tsx` | `useCheckout()`, `CreditCardForm`, `PayPalButton`, `useSlidingScale` | `usePayment(registrationId)` - only uses `paymentData` (reads dead Payment entity) and `resetProcessingState` |
| `features/payments/index.ts` | Exports active components | Exports dead `paymentApi`, `usePayment`, types |

---

## 4. ACTIVE Code That Uses TicketPurchase (The Real Payment System)

### Backend (12 files)

| File | How It Uses TicketPurchase |
|------|--------------------------|
| `Endpoints/CheckoutEndpoints.cs` | Creates pending, finalizes on CC success |
| `Endpoints/PayPalCheckoutEndpoints.cs` | Stores PayPal OrderId/CaptureId on TicketPurchase |
| `Endpoints/AdminPaymentEndpoints.cs` | Lists via PaymentListService |
| `Endpoints/RefundEndpoints.cs` | Routes to RefundTicket/ProcessVariableRefund |
| `Commands/RefundTicket.cs` | Queries TicketPurchase by ID for full refund |
| `Commands/ProcessVariableRefund.cs` | Queries TicketPurchase by ID for partial refund |
| `Services/RefundService.cs` | Queries TicketPurchase by ID (expects TicketPurchase.Id) |
| `Services/PaymentListService.cs` | Queries TicketPurchases table for admin list |
| `Services/AuthorizeNetService.cs` | Processes CC (returns response, caller persists to TicketPurchase) |
| `Services/PayPalService.cs` | Creates/captures PayPal orders (caller persists to TicketPurchase) |
| `Webhooks/PayPalWebhookProcessingService.cs` | Updates TicketPurchase on webhook |
| `Participation/Services/AttendanceService.cs` | Creates TicketPurchase records |

### Frontend (active payment files)

| File | Purpose |
|------|---------|
| `lib/api/hooks/usePayments.ts` | `useCheckout()`, `usePurchaseTicket()`, `useCreatePayPalOrder()`, `useConfirmPayPalPayment()` |
| `lib/api/services/payments.ts` | `checkout()` function → `POST /api/checkout/credit-card` |
| `features/payments/pages/EventPaymentPage.tsx` | Main payment page |
| `features/payments/components/checkout/CreditCardForm.tsx` | Accept.js integration |
| `features/payments/components/PayPalButton.tsx` | PayPal JS SDK integration |
| `features/payments/components/PaymentSummary.tsx` | Price display |
| `features/payments/components/PaymentConfirmation.tsx` | Success display |
| `features/admin/payments/hooks/useRefundTicket.ts` | Full refund |
| `features/admin/payments/hooks/useVariableRefund.ts` | Partial refund |
| `features/admin/payments/components/PaymentTableView.tsx` | Admin payment list |
| `pages/admin/AdminPaymentsPage.tsx` | Admin payment list page |

---

## 5. ACTIVE Code That Uses Payment Entity (Minimal)

Only **2 active code paths** touch the Payment entity:

### 1. KioskPaymentEndpoints.RecordCashPayment (Line 281)
- Creates `new Payment { ... }` directly (no PaymentService)
- Sets `Status = PaymentStatus.Completed`, `PaymentMethodType = Cash`
- These records are **invisible** in admin payment list

### 2. ParticipationEndpoints (2 places, both BROKEN)
- Line 955: `DELETE /api/admin/events/{eventId}/participations/{userId}/remove` - reads Payment, passes to RefundService (bug)
- Line 1125: `POST /api/admin/events/{eventId}/tickets/{userId}/refund` - reads Payment, passes to RefundService (bug)

---

## 6. Summary: What Needs to Happen

### The Payment entity is 95% dead code.

- It was replaced by TicketPurchase for all CC and PayPal flows
- The only active creator is KioskPaymentEndpoints (cash door sales)
- The only active reader is ParticipationEndpoints (2 places, both bugged)
- PaymentService, PaymentEndpoints, PaymentFailure, PaymentAuditLog, PaymentMethod are all completely dead
- The entire `paymentApi.ts` frontend service is dead
- `usePayment` hook is dead
- `PaymentSuccessPage` and `PaymentCancelPage` are dead

### The real payment system is TicketPurchase-based:

- CheckoutEndpoints (CC) → TicketPurchase
- PayPalCheckoutEndpoints (PayPal) → TicketPurchase
- RefundTicket/ProcessVariableRefund → TicketPurchase
- PaymentListService → TicketPurchase
- PayPalWebhookProcessingService → TicketPurchase

### Problems to fix:

1. **BUG**: ParticipationEndpoints passes Payment.Id to RefundService which expects TicketPurchase.Id (2 places)
2. **Kiosk cash payments** create Payment entities, making them invisible in admin payment list
3. **Massive dead code** needs cleanup (entities, services, endpoints, frontend files)
4. **`ProcessRefundRequest.PaymentId`** is misleadingly named (stores TicketPurchase IDs now)
5. **Duplicate route** `/api/events/{id}/purchase-ticket` (compatibility alias)

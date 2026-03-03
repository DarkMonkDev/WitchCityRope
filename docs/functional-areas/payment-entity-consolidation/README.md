# Payment Entity Consolidation Project

**Status**: Complete — All Work Done, Deployed to Dev & Staging (2026-03-03)
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
| Phase 2: Migrate kiosk cash payments to TicketPurchase | **Complete** |
| Phase 3: Rename ProcessRefundRequest.PaymentId → TicketPurchaseId | **Complete** |
| Phase 4: Remove dead backend code (12 files deleted) | **Complete** |
| Phase 5: Remove dead frontend code (4 files deleted) | **Complete** |
| Phase 6: DB migration to drop dead tables | **Complete** (applied to dev & staging) |
| Phase 7: Remove Payment entity and Payments table | **Complete** (applied to dev & staging) |
| Follow-up: Remaining tech debt (9 items) | **Complete** (deployed 2026-03-03) |

**Total dead code removed**: ~5,300 lines across 21 files deleted and 14 files modified.
**Follow-up tech debt**: ~4,500 additions, ~1,600 deletions across 30 files.

## What Was Done

### Problem
The codebase had a **dual-entity payment architecture** where `TicketPurchase` (the actual source of truth) and `Payment` (an orphaned entity) both tracked payment data. Payment records were created but never updated by webhooks, refunds, or admin interfaces. This caused admin refund bugs, invisible cash door sales, and developer confusion.

### Resolution
- **Phase 1**: Fixed 2 critical bugs in `ParticipationEndpoints.cs` where admin refunds queried the wrong table
- **Phase 2**: Migrated kiosk cash payments from `Payment` entity to `TicketPurchase` + `EventAttendance`, making cash door sales visible in admin payment list
- **Phase 3**: Renamed `ProcessRefundRequest.PaymentId` → `TicketPurchaseId` across all callers to prevent future naming confusion
- **Phase 4**: Deleted 12 dead backend files (PaymentService, PaymentEndpoints, CreditCardEndpoints, PaymentFailure, PaymentMethod, PaymentAuditLog entities/configs/validators)
- **Phase 5**: Deleted 4 dead frontend files (paymentApi.ts, usePayment.ts, PaymentSuccessPage, PaymentCancelPage), cleaned dead types and routes
- **Phase 6**: Generated and applied migration `DropDeadPaymentTables` (drops PaymentAuditLog, PaymentFailures, PaymentMethods tables + orphaned PaymentRefunds.PaymentId column)
- **Phase 7**: Deleted Payment entity, configuration, enums, and dead models. Generated and applied migration `DropPaymentsTable`

### Database Tables Dropped
| Table | Rows at Drop Time | Why |
|-------|-------------------|-----|
| Payments | 0 (staging), 0 (dev) | Orphaned — never updated by webhooks/refunds/admin |
| PaymentFailures | 0 | Dead code — no code path ever created records |
| PaymentMethods | 0 | Dead code — saved card feature never implemented (referenced Stripe, not Authorize.net) |
| PaymentAuditLog | 0 | Orphaned — only written during Payment creation, replaced by Serilog |

### Remaining Active Payment Entities
| Entity | File | Status |
|--------|------|--------|
| TicketPurchase | `apps/api/Models/TicketPurchase.cs` | Single source of truth for all payments |
| PaymentRefund | `apps/api/Features/Payments/Entities/PaymentRefund.cs` | Active — FK points to TicketPurchaseId |
| Money (value object) | `apps/api/Features/Payments/ValueObjects/Money.cs` | Active — used by RefundService, PayPalService |

---

## Follow-Up Tech Debt (All Resolved 2026-03-03)

These 9 items were identified during deep-dive research and addressed in a follow-up session.

### Resolved — Should Fix (Medium Priority)

1. **~~Duplicate ticket purchase route~~** — Removed `/purchase-ticket` route, updated frontend and tests to use `/tickets`
2. **~~Three separate refund endpoints~~** — Reduced to 2: removed Flow 1 (ParticipationEndpoints refund, had no frontend caller), preserved its EventAttendee status update logic in RefundService
3. **~~RefundTicket blocks multiple refunds~~** — RefundTicket now calculates remaining refundable amount (same pattern as ProcessVariableRefund)
4. **~~Transaction safety in RefundService~~** — Wrapped ProcessRefundAsync in `IDbContextTransaction` with commit/rollback
5. **~~Missing cancellationToken in LogRefundRetryAsync~~** — Added parameter and passed through to FindAsync and SaveChangesAsync

### Resolved — Nice-to-Have (Low Priority)

6. **~~PaymentStatus string → enum~~** — Created `TicketPurchasePaymentStatus` enum with string-backed EF value converter (no migration needed)
7. **~~Hardcoded USD currency~~** — Created `PaymentConstants.Currency` constant, replaced all hardcoded "USD" strings
8. *Money value object on TicketPurchase* — Deferred (not worth schema change for current usage)
9. **~~JSONB metadata column~~** — Added `Dictionary<string, object> Metadata` to TicketPurchase with JSONB storage, migration applied

---

## Key Files Reference (Post-Consolidation)

### Active Entities
- `apps/api/Models/TicketPurchase.cs` — Single source of truth for payment data (PaymentStatus enum, Metadata JSONB)
- `apps/api/Models/TicketPurchasePaymentStatus.cs` — PaymentStatus enum (Pending, Completed, Confirmed, Failed, PartiallyRefunded, Refunded)
- `apps/api/Features/Payments/Entities/PaymentRefund.cs` — Refund records (FK → TicketPurchase)
- `apps/api/Features/Payments/ValueObjects/Money.cs` — Value object for currency amounts
- `apps/api/Features/Payments/PaymentConstants.cs` — Currency constant (USD-only business rule)

### Active Services
- `apps/api/Features/Payments/Services/AuthorizeNetService.cs` — CC processing
- `apps/api/Features/Payments/Services/PayPalService.cs` — PayPal integration
- `apps/api/Features/Payments/Services/RefundService.cs` — Refund processing (queries TicketPurchase)
- `apps/api/Features/Payments/Services/PaymentListService.cs` — Admin payment list (queries TicketPurchase)
- `apps/api/Features/Payments/Services/PaymentNotificationService.cs` — SSE notifications
- `apps/api/Features/Participation/Services/AttendanceService.cs` — Creates TicketPurchase records

### Active Endpoints
- `apps/api/Features/Payments/Endpoints/CheckoutEndpoints.cs` — CC checkout (4-stage atomic)
- `apps/api/Features/Payments/Endpoints/PayPalCheckoutEndpoints.cs` — PayPal checkout
- `apps/api/Features/Payments/Endpoints/KioskPaymentEndpoints.cs` — Cash door sales (now uses TicketPurchase)
- `apps/api/Features/Payments/Endpoints/AdminPaymentEndpoints.cs` — Admin payment list
- `apps/api/Features/Payments/Endpoints/RefundEndpoints.cs` — Refund processing
- `apps/api/Features/Payments/Endpoints/WebhookEndpoints.cs` — PayPal webhooks
- `apps/api/Features/Participation/Endpoints/ParticipationEndpoints.cs` — Ticket/RSVP management (refund endpoint removed)

### Refund Endpoints (Post-Consolidation)
Only 2 refund flows remain:
- `POST /api/admin/refunds/{ticketId}` — Full refund via `RefundTicket.cs` (supports multiple refunds, cancels RSVP if requested)
- `POST /api/payments/transactions/{transactionId}/refund` — Variable amount refund via `ProcessVariableRefund.cs` (does NOT cancel RSVP)

### Migrations (This Project)
- `apps/api/Migrations/20260303051802_DropDeadPaymentTables.cs` — Drops PaymentAuditLog, PaymentFailures, PaymentMethods + PaymentRefunds.PaymentId
- `apps/api/Migrations/20260303053611_DropPaymentsTable.cs` — Drops Payments table
- `apps/api/Migrations/20260303063750_AddTicketPurchaseMetadata.cs` — Adds Metadata JSONB column to TicketPurchases

---

## Related Documentation
- [Implementation Plan](./implementation-plan.md) — Detailed phase-by-phase plan with completion notes
- [Deep Dive Research](./deep-dive-research.md) — Full research findings (all items resolved)

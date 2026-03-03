# Payment Entity Consolidation Project

**Status**: Complete — All 7 Phases Done, Deployed to Dev & Staging (2026-03-03)
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

**Total dead code removed**: ~5,300 lines across 21 files deleted and 14 files modified.

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

## Known Remaining Issues (Not Addressed in This Project)

These items were identified during deep-dive research but are **out of scope** for this consolidation project. They should be tracked as separate work items.

### Should Fix (Medium Priority)

1. **Duplicate ticket purchase route**: `/api/events/{id}/purchase-ticket` is an exact duplicate of `/api/events/{id}/tickets` (comment says "for compatibility with tests"). Should be removed.

2. **Three separate refund endpoint patterns**: RefundTicket, ProcessVariableRefund, and ParticipationEndpoints each handle refunds differently. Could be consolidated into one flexible endpoint.

3. **RefundTicket blocks multiple refunds**: `RefundTicket.cs` checks for ANY existing refund and blocks. Only `ProcessVariableRefund` supports multiple partial refunds. This is a functional limitation.

4. **Transaction safety in RefundService**: Multiple `SaveChangesAsync` calls without explicit `IDbContextTransaction`. Partial state can be persisted on failure.

5. **RefundService.LogRefundRetryAsync missing cancellationToken**: Line 598, parameter not passed through.

### Nice-to-Have (Low Priority)

6. **Migrate TicketPurchase.PaymentStatus from string to enum**: Currently uses magic strings ("Completed", "Failed", etc.). The deleted Payment entity had a proper enum — could adopt that pattern.

7. **Add currency field to TicketPurchase**: Currently assumes USD everywhere (hardcoded in PaymentListService and other places). Either add the field or formalize USD-only as a documented business rule.

8. **Add Money value object pattern to TicketPurchase**: TicketPurchase uses raw `decimal TotalPrice`. The Money value object exists but is only used in service-layer calculations.

9. **Add JSONB metadata column to TicketPurchase**: The deleted Payment entity had a useful `Metadata` JSONB column. TicketPurchase only has a `Notes` string field.

---

## Key Files Reference (Post-Consolidation)

### Active Entities
- `apps/api/Models/TicketPurchase.cs` — Single source of truth for payment data
- `apps/api/Features/Payments/Entities/PaymentRefund.cs` — Refund records (FK → TicketPurchase)

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
- `apps/api/Features/Participation/Endpoints/ParticipationEndpoints.cs` — Ticket/RSVP/admin refunds

### Migrations (This Project)
- `apps/api/Migrations/20260303051802_DropDeadPaymentTables.cs` — Drops PaymentAuditLog, PaymentFailures, PaymentMethods + PaymentRefunds.PaymentId
- `apps/api/Migrations/20260303053611_DropPaymentsTable.cs` — Drops Payments table

---

## Related Documentation
- [Implementation Plan](./implementation-plan.md) — Detailed phase-by-phase plan with completion notes
- [Deep Dive Research](./deep-dive-research.md) — Full research findings (some items still relevant for future work)

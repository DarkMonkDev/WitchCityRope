# Business Requirements Handoff: Streamlined Check-In Workflow
<!-- Handoff Date: 2025-11-03 -->
<!-- From: Business Requirements Agent -->
<!-- To: UI Designer, Backend Developer, React Developer -->
<!-- Status: Complete -->

## Purpose
This handoff document provides critical business context and requirements for implementing the streamlined check-in workflow. This is a significant UX improvement that reduces workshop check-ins from 4 clicks to 2 clicks by eliminating unnecessary modal confirmations.

## Executive Summary

### What's Changing
**Current System**: Check-in button → Modal popup → Confirm in modal → Checked in (4 clicks)
**New System**: Covid Test Complete → Check In → Checked in (2 clicks)

### Why It Matters
- **40% faster check-ins**: From 5 seconds to 3 seconds per attendee
- **Better staff experience**: Eliminates repetitive confirmations
- **Higher throughput**: 20 attendees/minute (up from 12)
- **Maintained complexity for social events**: Door payment workflows preserved

## Critical Business Rules

### Rule 1: Modal Usage - ONLY When Necessary
**NO Modal Required**:
- Workshop check-ins (already paid + waived)
- Social event check-ins with tickets (already paid + waived)
- Covid test completion (UI state only)
- Standard check-in action (final step)

**Modal REQUIRED**:
- Cash payment amount entry
- QR code display for digital payments
- Walk-in attendee creation
- Error messages requiring acknowledgment

### Rule 2: Button Progression Logic
**Workshops & Pre-Paid Social Events**:
```
Covid Test Complete → Check In → ✓ Checked In
```

**Social Events - RSVP Only**:
```
Paid at Door → [Payment Modal/QR] → Covid Test Complete → Check In → ✓ Checked In
```

### Rule 3: Data Storage (Critical!)
**DO Store in Database**:
- Check-in records (timestamp, attendeeId, staffId)
- Payment records (amount, method, timestamp, staffId)
- Payment method (Cash or PayPal)
- Optional payment notes

**DO NOT Store in Database**:
- Covid test completion status (React state only)
- Button workflow states (React state only)
- QR code scan events

### Rule 4: Payment Security
- **ZERO credit card storage** in WitchCityRope database
- All digital payments via PayPal integration
- Cash payments = transaction records only
- All payment actions include staff attribution (audit trail)

## User Stories - Implementation Priorities

### Priority 1: Workshop Check-In (Must Have)
**Story**: 2-click check-in for workshop attendees
**Acceptance**: No modal, just "Covid Test Complete" → "Check In"
**Why Critical**: Workshops are 70% of events, highest volume

### Priority 2: Social Event - Pre-Paid (Must Have)
**Story**: Same workflow as workshops for pre-paid social attendees
**Acceptance**: Identical to workshop workflow
**Why Critical**: Consistency reduces staff confusion

### Priority 3: Cash Payment at Door (Must Have)
**Story**: Modal for entering cash amount
**Acceptance**: Amount input, "Record Payment" button, creates payment record
**Why Critical**: Financial tracking requirement

### Priority 4: QR Code Payment (Should Have)
**Story**: Display QR code for attendee self-service payment
**Acceptance**: QR code generation, real-time update detection
**Why Critical**: Contactless payment option, reduces staff workload

### Priority 5: Real-Time Payment Detection (Should Have)
**Story**: Kiosk auto-updates when QR payment completes
**Acceptance**: Update within 5 seconds, no manual refresh
**Why Critical**: Seamless workflow, prevents staff confusion

## Technical Requirements for Next Phases

### For UI Designer
**Must Design**:
1. Button states visual progression (4 states per attendee)
2. Payment status badges (green "Ticket Purchased" vs yellow "RSVP Only")
3. Cash payment modal layout
4. QR code display modal/overlay
5. Capacity warning indicators
6. Error states for blocked payments

**Design Constraints**:
- Must follow Design System v7 CSS classes
- NO inline styles or page-specific CSS
- Burgundy theme for headers/primary actions
- Mobile-friendly QR code display

### For Backend Developer
**Must Implement**:
1. Payment record API endpoint (POST /api/checkin/events/{eventId}/payments)
2. Payment validation (capacity check, duplicate prevention)
3. Real-time payment notification system (WebSocket, SSE, or polling)
4. QR code URL generation with security tokens
5. Payment-to-attendee linking logic

**Data Models**:
- PaymentRecord (amount, method, timestamp, staffId, attendeeId, eventId, notes)
- CheckInRecord updates (add paymentId foreign key)

**Security Requirements**:
- Session token validation for all endpoints
- Payment amount validation (prevent negative/zero)
- Capacity enforcement before payment
- Staff attribution on all actions

### For React Developer
**Must Implement**:
1. Button state management (React state, not database)
2. Covid test completion UI state (per attendee)
3. Cash payment modal component
4. QR code display component
5. Real-time payment listener (WebSocket/SSE/polling)
6. Payment status badge component
7. Button click handlers (no modal for simple cases)

**Component Structure**:
```
CheckInInterface
├── CheckInRow (per attendee)
│   ├── PaymentStatusBadge
│   ├── CheckInButton (dynamic state)
│   └── (conditional modals)
├── CashPaymentModal
└── QrCodePaymentModal
```

**State Management**:
- Per-attendee button state (PaidAtDoor | CovidTestComplete | CheckIn | CheckedIn)
- Per-attendee covid test completion (boolean, UI only)
- Modal visibility states
- Real-time payment update handlers

## Data Flow Diagrams

### Workshop Check-In Flow
```
1. Attendee arrives
2. Staff sees: [Covid Test Complete] button
3. Staff clicks → Button becomes [Check In]
4. Staff clicks → API call → Check-in recorded
5. Row updates to: ✓ Checked In
```

### Social Event - Cash Payment Flow
```
1. RSVP attendee arrives (no ticket)
2. Staff sees: [Paid at Door] button
3. Staff clicks → Modal opens
4. Staff enters $20.00 → "Record Payment"
5. API call → Payment recorded
6. Modal closes → Button becomes [Covid Test Complete]
7. Staff clicks → Button becomes [Check In]
8. Staff clicks → API call → Check-in recorded
9. Row updates to: ✓ Checked In
```

### Social Event - QR Code Payment Flow
```
1. RSVP attendee arrives (no ticket)
2. Staff sees: [Paid at Door] button
3. Staff clicks → "Digital Payment" option
4. QR code displays on screen
5. Attendee scans with phone
6. Attendee completes PayPal payment
7. Backend receives webhook
8. Kiosk receives real-time notification
9. Button auto-updates to [Covid Test Complete]
10. Staff continues with normal workflow
```

## Open Questions (MUST ANSWER BEFORE DESIGN)

### Critical Questions for Product Manager
1. **Real-Time Technology**: WebSocket, SSE, or polling for payment updates?
   - Affects architecture significantly
   - Must work with session token auth

2. **Covid Test Button**: Permanent or configurable feature?
   - May be removed post-pandemic
   - Should it be event-specific setting?

3. **Walk-In Button**: Keep or remove?
   - User notes mentioned "hide walk-in button"
   - Affects feature scope

4. **Payment Timeout**: How long should QR code payment wait?
   - 2 minutes? 5 minutes?
   - Manual refresh button needed?

5. **Capacity Override**: Can staff override capacity for VIPs?
   - Affects validation logic
   - Security implications

### Technical Questions for Implementation Team
6. **QR Code Library**: Which React QR library to use?
   - Must generate clear, scannable codes
   - Must support URL encoding

7. **Payment Integration**: Same PayPal flow as regular tickets?
   - How to link door payment to attendee?
   - How to handle payment failures?

8. **Error Recovery**: What if real-time update fails?
   - Manual refresh button?
   - Automatic retry logic?

9. **State Persistence**: Should button states persist on refresh?
   - Current spec: NO (resets to initial state)
   - Could cause confusion if staff loses progress

10. **Multiple Sessions**: How do door payments work for multi-session events?
    - Pay for all sessions or partial?
    - QR code specify session details?

## Success Criteria

### Must Achieve
- ✅ Workshop check-ins: 2 clicks, NO modal
- ✅ Payment records: 100% accurate, linked to attendees
- ✅ Real-time updates: < 5 seconds from payment completion
- ✅ Staff training: < 5 minutes (down from 15)
- ✅ Check-in speed: 3 seconds per attendee (down from 5)

### Should Achieve
- ✅ QR code scan success: > 90%
- ✅ Door payment adoption: > 50% of RSVP attendees
- ✅ Zero payment tracking errors
- ✅ Staff satisfaction: +30% improvement
- ✅ Capacity compliance: 100% (zero overages)

## Next Phase Deliverables

### UI Designer Deliverables
1. Button state wireframes (4 states)
2. Cash payment modal mockup
3. QR code display mockup
4. Payment status badge designs
5. Capacity warning styles
6. Error state mockups

### Backend Developer Deliverables
1. Payment record API endpoint
2. Real-time notification system
3. QR code URL generator
4. Payment validation logic
5. Database schema updates (migration)
6. API documentation

### React Developer Deliverables
1. Refactored CheckInRow component
2. CashPaymentModal component
3. QrCodePaymentModal component
4. PaymentStatusBadge component
5. Real-time payment listener hook
6. Button state management logic

## Risk Assessment

### High Risk Items
1. **Real-Time Payment Detection**: Complex, no clear solution yet
   - Mitigation: Research phase required, fallback to manual refresh

2. **QR Code Payment Abandonment**: Attendee starts payment, doesn't complete
   - Mitigation: Timeout logic, clear status indicators

3. **Button State Confusion**: Staff unsure which button to click
   - Mitigation: Clear visual indicators, comprehensive training

### Medium Risk Items
4. **Capacity Race Conditions**: Two staff members process last spot simultaneously
   - Mitigation: Database-level locking, clear error messages

5. **Payment Reconciliation Errors**: Cash amounts don't match records
   - Mitigation: Audit logging, staff attribution, clear input validation

### Low Risk Items
6. **Browser Refresh**: Staff loses progress
   - Mitigation: Expected behavior (state resets), documented clearly

7. **QR Code Scan Failures**: Poor lighting, bad camera
   - Mitigation: Fallback to cash payment, manual entry option

## Testing Requirements

### Unit Tests Required
- Button state transitions
- Payment validation logic
- Capacity enforcement
- QR code generation
- Real-time listener hooks

### Integration Tests Required
- Cash payment recording
- QR code payment flow
- Real-time update detection
- Database transaction integrity
- Session token validation

### E2E Tests Required
- Workshop check-in workflow
- Social event pre-paid workflow
- Cash payment workflow
- QR code payment workflow
- Capacity limit enforcement
- Page refresh behavior

## Documentation Requirements

### User Documentation
- Staff training guide (5-minute version)
- Quick reference card (button progression)
- Troubleshooting guide (common issues)

### Technical Documentation
- API endpoint specifications
- Real-time notification architecture
- Payment security documentation
- Database schema changes

### Business Documentation
- Payment reconciliation procedures
- Refund processing updates
- Audit trail access procedures

## Appendix: Related Documents

### Source Requirements
- `/docs/functional-areas/events/new-work/2025-11-03-streamlined-checkin-workflow/requirements/business-requirements.md`

### Related Features
- `/docs/functional-areas/events/new-work/2025-08-24-events-management/requirements/business-requirements.md`
- Check-in system implementation (current)
- PayPal integration documentation

### Standards & Patterns
- `/docs/standards-processes/backend/` - API standards
- Design System v7 CSS classes
- Session token authentication pattern

---

## Handoff Checklist

- [x] Business rules documented and clear
- [x] User stories prioritized
- [x] Technical requirements specified
- [x] Data models defined
- [x] Security requirements documented
- [x] Open questions identified (10 critical questions)
- [x] Success criteria defined
- [x] Risk assessment completed
- [x] Testing requirements outlined
- [x] Next phase deliverables specified

**Ready for Next Phase**: ✅ YES (pending answer to open questions)

**Next Agent**: UI Designer (after Product Manager answers questions)

**Estimated Effort**:
- Design Phase: 4-6 hours
- Backend Development: 8-12 hours
- Frontend Development: 12-16 hours
- Testing: 8-10 hours
- **Total: 32-44 hours** (2-3 week sprint)

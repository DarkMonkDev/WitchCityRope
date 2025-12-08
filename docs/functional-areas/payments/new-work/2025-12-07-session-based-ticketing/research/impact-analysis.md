# Session-Based Ticketing - Comprehensive Impact Analysis

<!-- Last Updated: 2025-12-07 -->
<!-- Version: 1.0 -->
<!-- Owner: Orchestrator/Librarian -->
<!-- Status: Active - Complete Research -->

## Executive Summary

**Current State**: ONE ticket per EVENT per user
**Target State**: ONE ticket per SESSION per user
**Critical Finding**: Database schema already has many-to-many TicketType↔Session (migration applied 2025-12-02)
**Main Gap**: Business logic validation still checks EVENT-level, not SESSION-level

### Key Insights
- **Database Migration**: Already completed (2025-12-02) - TicketTypeSessions join table exists
- **DTO Updates**: Already completed - SessionIdentifiers array in place
- **Seeder Updates**: Already completed - TicketTypes properly linked to Sessions
- **Primary Work**: Refactor business logic from EVENT-level to SESSION-level validation
- **Estimated Timeline**: 2-3 weeks across all phases

---

## Database Status

### Already Implemented ✅

**Join Table**:
- `TicketTypeSessions` exists (created 2025-12-02)
- Many-to-many relationship configured between TicketType and Session
- Foreign key constraints in place

**DTOs Updated**:
- `TicketTypeDto` has `SessionIdentifiers` array (Guid[])
- Frontend components already receiving session data
- API responses include session information

**Seeders Updated**:
- `EventSeeder.cs` populates TicketTypeSessions join table
- Test data includes proper ticket-to-session relationships
- Development environment ready for testing

### Still Needed ⚠️

**EventAttendance Schema Enhancement**:
- **Option 1**: Add `SessionId` field to EventAttendance table
  - Pros: Direct session tracking, simple queries
  - Cons: Requires migration, data backfill for existing records
- **Option 2**: Use join through TicketType.Sessions
  - Pros: No schema changes needed
  - Cons: More complex queries, potential performance impact

**Validation Logic**:
- Change "already has ticket for EVENT" to "already has ticket for SESSION(S)"
- Capacity checks must be per-session, not per-event
- Duplicate ticket prevention needs session awareness

---

## Backend Files Requiring Changes

### CRITICAL (Complex - 3-5 days)

| File | Lines | What Needs to Change | Why | Complexity |
|------|-------|---------------------|-----|-----------|
| **AttendanceService.cs** | 562-575 | Change "already has ticket for EVENT" to "already has ticket for SESSION(S)" check | Core validation logic prevents duplicate purchases | **HIGH** - Core business logic |
| **AttendanceService.cs** | 106-107, 333-346, 578-584 | Capacity checks must be per-session | Capacity management for multi-session events | **HIGH** - Critical capacity logic |
| **AttendanceService.cs** | 235-249 | Update GetExistingAttendance to check session-level duplicates | Prevent same user buying tickets for same session multiple times | **MEDIUM** - Query logic change |

**AttendanceService.cs - Detailed Impact**:
```csharp
// CURRENT (Line 562-575):
// Checks: "User already has a ticket for this event"
//
// NEEDED:
// Checks: "User already has a ticket for these specific sessions"
// Example: Event has 3 sessions (A, B, C)
//   - User bought ticket for Session A → Can still buy for B or C
//   - User bought ticket for Session A → CANNOT buy another ticket for A
```

### MEDIUM (1-2 days)

| File | Lines | What Needs to Change | Why | Complexity |
|------|-------|---------------------|-----|-----------|
| **TicketType.cs** | 84-97 | Update Sold calculation for session awareness | Display accurate sold counts per session | **MEDIUM** - Calculated property |
| **Session.cs** | 116-129 | Verify/update CurrentAttendees calculation | Ensure session attendee counts are accurate | **MEDIUM** - Calculated property |
| **EventService.cs** | 204-226 | Refactor capacity calculations | Support session-level capacity queries | **MEDIUM** - Service logic |
| **CheckInService.cs** | 61-77, 254-288 | Align with session-based validation | Check-in logic must validate correct session | **MEDIUM** - Service logic |
| **Event.cs** | 189-200 | Consider session-level capacity methods | Add helper methods for session capacity | **LOW-MEDIUM** - Helper methods |

### SIMPLE (< 1 day)

**DTOs** (add session display fields):
- `EventDto.cs` - Add session-specific availability fields
- `SessionDto.cs` - Add ticketing-related fields (sold, available, capacity)
- `TicketTypeDto.cs` - Already has SessionIdentifiers, verify completeness
- `ParticipationDto.cs` - Add session details for user's tickets

**Seeders** (populate SessionId):
- `EventAttendanceSeeder.cs` - Add SessionId to test data
- Verify all test data includes proper session relationships

**Configuration Files**:
- `EventAttendanceConfiguration.cs` - Add FK relationship for SessionId (if Option 1 chosen)
- Verify all existing configurations remain valid

**Time Zone Service**:
- `TimeZoneService.cs` - Already session-aware, minor adjustments for display

**API Endpoints**:
- Add query parameters for session-specific filtering
- Return session-specific capacity data in responses

---

## Frontend Files Requiring Changes

### CRITICAL (Complex - 2-3 days)

| File | Component | What Needs to Change | Why | Complexity |
|------|-----------|---------------------|-----|-----------|
| **EventForm.tsx** | Admin event editor | Attendees tracking needs session awareness | Admins need to see which sessions have tickets sold | **HIGH** - Complex UI state |
| **EventPaymentPage.tsx** | Checkout flow | Session-based availability checks before payment | Prevent purchasing sold-out sessions | **HIGH** - Critical purchase flow |
| **TicketTypeFormModal.tsx** | Ticket config | Per-session capacity UI configuration | Admins configure capacity per session | **HIGH** - Complex form logic |
| **ParticipationCard.tsx** | Purchase component | Session-specific availability display | Show "Session A: 5/10 available" | **MEDIUM-HIGH** - Display logic |

**EventPaymentPage.tsx - Critical Changes**:
```typescript
// CURRENT:
// Checks event-level capacity: "Event is sold out"
//
// NEEDED:
// Checks session-level capacity:
//   - "Session A (Saturday 2pm): 2 spots left"
//   - "Session B (Sunday 10am): Sold out"
//   - User selects which sessions when purchasing ticket
```

### MEDIUM (1-2 days)

| File | Component | What Needs to Change | Why | Complexity |
|------|-----------|---------------------|-----|-----------|
| **EventDetailPage.tsx** | Public event view | Session-specific availability messages | Users see availability per session | **MEDIUM** - Display logic |
| **EventTicketPurchaseModal.tsx** | Purchase modal | Session availability display | Purchase modal shows session options | **MEDIUM** - Modal logic |
| **PublicEventCard.tsx** | Event cards | Aggregate session availability | Card shows "3 sessions, 2 available" | **MEDIUM** - Aggregation logic |
| **EventTicketTypesGrid.tsx** | Admin grid | Per-session sold counts | Admin sees "Session A: 5 sold, Session B: 3 sold" | **MEDIUM** - Grid display |
| **MyEventsPage.tsx** | User dashboard | Session-specific ticket info | User sees which sessions they have tickets for | **MEDIUM** - Dashboard logic |
| **UserParticipations.tsx** | Dashboard widget | Session badges display | Display session info on participation cards | **MEDIUM** - Widget logic |
| **useEvents.ts** | Data hooks | Session-based transformations | Transform API data for session display | **MEDIUM** - Hook logic |
| **events.types.ts** | TypeScript types | Per-session availability fields | Type safety for session data | **LOW-MEDIUM** - Type definitions |
| **EventRSVPModal.tsx** | RSVP modal | Session selection UI | RSVP may need session selection too | **MEDIUM** - Modal logic |

### SIMPLE (Display updates - 36 files total)

**Event Components** (14 files):
- Event list displays, event cards, event filters
- Update to show session-level status messages
- Type definition updates

**Payment Components** (8 files):
- Payment success/failure pages
- Refund components (session-specific refunds)
- Payment history displays

**Admin Components** (7 files):
- Admin dashboards, admin event lists
- Admin analytics (per-session metrics)

**User Dashboard Components** (7 files):
- User event lists, participation history
- Session-specific ticket displays

**API Hooks** (all hooks using events/tickets):
- Update query parameters for session filtering
- Transform responses to include session data

---

## Implementation Strategy

### Phase 1: Backend Business Logic (3-5 days)
**Objective**: Core validation and capacity logic

1. **Database Decision**: Choose SessionId field approach (Option 1 recommended)
   - Create migration if needed
   - Backfill existing EventAttendance records

2. **AttendanceService Validation**:
   - Update GetExistingAttendance to check session-level duplicates
   - Modify duplicate ticket check (lines 562-575)
   - Refactor capacity calculations (lines 106-107, 333-346, 578-584)

3. **Verify Session Tracking**:
   - Add/verify EventAttendance.SessionId tracking
   - Test: Purchase ticket for Session A, verify can purchase for Session B
   - Test: Purchase ticket for Session A, verify CANNOT purchase another for Session A

**Deliverables**:
- ✅ Backend prevents session-level duplicate purchases
- ✅ Capacity checks work per-session
- ✅ Comprehensive unit tests

---

### Phase 2: Backend DTOs & APIs (2-3 days)
**Objective**: API responses include session data

4. **Service Layer Updates**:
   - Ensure all services include `.Include(tt => tt.Sessions)`
   - EventService: Add session-specific capacity queries
   - CheckInService: Align with session-based validation

5. **DTO Enhancements**:
   - SessionDto: Add sold count, available count, capacity
   - EventDto: Add session-specific availability summary
   - ParticipationDto: Add session details for user tickets

6. **Regenerate Frontend Types**:
   - Run NSwag type generation
   - Verify TypeScript interfaces match new DTOs
   - Commit generated types to shared-types package

**Deliverables**:
- ✅ API responses include session-level data
- ✅ Frontend types updated and published
- ✅ Integration tests passing

---

### Phase 3: Admin UI (2-3 days)
**Objective**: Admin tools for session-based ticketing

7. **EventForm Attendees Tab**:
   - Display attendee counts per session
   - Show "Session A: 5 attendees, Session B: 3 attendees"

8. **TicketTypeFormModal Capacity Config**:
   - UI to configure capacity per session
   - Validation: Ensure capacity doesn't exceed session max

9. **EventTicketTypesGrid Sold Display**:
   - Grid columns for each session's sold count
   - Aggregate totals across sessions

**Deliverables**:
- ✅ Admins can configure per-session capacity
- ✅ Admins see accurate sold counts per session
- ✅ E2E tests for admin workflows

---

### Phase 4: Public UI (2-3 days)
**Objective**: User-facing session availability

10. **EventDetailPage Availability**:
    - Display "Session A: 5 spots left" for each session
    - Show sold-out badge for full sessions

11. **ParticipationCard Purchase Buttons**:
    - Enable purchase only for available sessions
    - Disable button for sold-out sessions with clear messaging

12. **PublicEventCard Status**:
    - Aggregate status: "3 sessions, 2 available, 1 sold out"
    - Click for details shows per-session breakdown

**Deliverables**:
- ✅ Users see accurate session availability
- ✅ Clear messaging for sold-out sessions
- ✅ E2E tests for public user flows

---

### Phase 5: Purchase Flow (2-3 days)
**Objective**: Session selection during purchase

13. **EventPaymentPage Ticket Selection**:
    - Multi-step purchase flow
    - Step 1: Select sessions to attend
    - Step 2: Choose ticket type
    - Step 3: Payment information

14. **Payment Components**:
    - Update PaymentSuccess to show selected sessions
    - Update receipts to include session details
    - Update refund logic for per-session refunds

**Deliverables**:
- ✅ Users can select specific sessions when purchasing
- ✅ Payment confirmation shows sessions
- ✅ E2E tests for complete purchase flow

---

### Phase 6: User Dashboard (1-2 days)
**Objective**: User ticket management

15. **MyEventsPage Session Details**:
    - User sees "You have tickets for: Session A, Session C"
    - Clear indication of which sessions they can attend

16. **UserParticipations Badges**:
    - Display session badges on participation cards
    - "Saturday 2pm" badge next to event name

**Deliverables**:
- ✅ Users see their session-specific tickets
- ✅ Dashboard accurately reflects session participation
- ✅ E2E tests for user dashboard

---

## Risk Assessment

### High Risk Areas

**1. AttendanceService Changes (CRITICAL)**
- **Risk**: Changes affect ALL ticket purchases (100% of revenue flow)
- **Impact**: Breaking changes could prevent all ticket sales
- **Mitigation**:
  - Comprehensive unit tests before deployment
  - Feature flag for gradual rollout
  - Manual QA testing on staging environment
  - Rollback plan ready

**2. Capacity Calculation Changes (HIGH)**
- **Risk**: Incorrect capacity logic allows overselling or blocks valid purchases
- **Impact**: Customer dissatisfaction, revenue loss, operational issues
- **Mitigation**:
  - Extensive edge case testing
  - Parallel run with old logic (log differences)
  - Monitor capacity calculations on staging

**3. Breaking Existing Ticket Purchases (HIGH)**
- **Risk**: Migration affects historical purchase records
- **Impact**: Users lose access to purchased tickets
- **Mitigation**:
  - Database backup before migration
  - Backfill script to assign SessionId to existing EventAttendance
  - Verify all existing tickets remain valid post-migration

**4. Session Selection UX Complexity (MEDIUM-HIGH)**
- **Risk**: Purchase flow becomes too complex, users abandon checkout
- **Impact**: Reduced conversion rate, lost revenue
- **Mitigation**:
  - Clear UI/UX design with user testing
  - Smart defaults (auto-select all sessions)
  - Progressive disclosure (simple for single-session events)

### Medium Risk Areas

**5. Performance Impact (MEDIUM)**
- **Risk**: Session-level queries slower than event-level
- **Impact**: Slower page loads, poor user experience
- **Mitigation**:
  - Database indexing on SessionId
  - Caching for capacity calculations
  - Performance testing before production

**6. Admin Confusion (MEDIUM)**
- **Risk**: Admins don't understand per-session configuration
- **Impact**: Misconfigured events, customer service issues
- **Mitigation**:
  - Clear admin documentation
  - Tooltips and help text in UI
  - Admin training before rollout

### Low Risk Areas

**7. Display Logic Changes (LOW)**
- **Risk**: UI shows incorrect session data
- **Impact**: User confusion but no data corruption
- **Mitigation**: Visual QA, E2E tests

---

## Estimated Timeline

### By Phase

| Phase | Work Type | Estimated Time | Agents Involved |
|-------|-----------|----------------|-----------------|
| **Phase 1** | Backend Core Logic | 3-5 days | Backend Developer, Database Designer |
| **Phase 2** | Backend DTOs & APIs | 2-3 days | Backend Developer, Test Developer |
| **Phase 3** | Admin UI | 2-3 days | React Developer, UI Designer |
| **Phase 4** | Public UI | 2-3 days | React Developer, UI Designer |
| **Phase 5** | Purchase Flow | 2-3 days | React Developer, Backend Developer |
| **Phase 6** | User Dashboard | 1-2 days | React Developer |
| **Testing** | Comprehensive E2E | 3-5 days | Test Developer, Test Executor |
| **TOTAL** | **All Phases** | **16-24 days** | **2-3 weeks calendar time** |

### By Work Type

- **Backend Development**: 5-8 days
- **Frontend Development**: 7-11 days
- **Testing**: 3-5 days
- **Design/Planning**: 1-2 days (already partially complete)

### Assumptions

- **Parallel Work**: Frontend and backend can proceed in parallel after Phase 1
- **Human Reviews**: Quality gate reviews at phase boundaries (add 1-2 days)
- **Rework Buffer**: 20% contingency for unexpected issues (add 3-5 days)
- **Final Timeline**: **2-3 weeks** with parallel work, **4-5 weeks** if sequential

---

## Files Summary

### Backend Files to Modify

**Total**: ~25 files

**By Category**:
- **Critical Services**: 3 files (AttendanceService, EventService, CheckInService)
- **Domain Models**: 3 files (Event.cs, Session.cs, TicketType.cs)
- **DTOs**: 4 files (EventDto, SessionDto, TicketTypeDto, ParticipationDto)
- **Database**: 2 files (EventAttendanceConfiguration, new migration)
- **Seeders**: 2 files (EventAttendanceSeeder, EventSeeder verification)
- **Supporting Services**: 5+ files (TimeZoneService, other affected services)
- **Tests**: 6+ files (unit + integration tests)

### Frontend Files to Modify

**Total**: ~36 files

**By Category**:
- **Critical Components**: 4 files (EventForm, EventPaymentPage, TicketTypeFormModal, ParticipationCard)
- **Medium Components**: 9 files (EventDetailPage, PublicEventCard, etc.)
- **Simple Components**: 23 files (displays, lists, cards, filters)
- **Types**: 3 files (events.types.ts, ticket.types.ts, participation.types.ts)
- **Hooks**: 5+ files (useEvents, useTickets, useParticipations, etc.)
- **Tests**: 8+ files (E2E tests for all workflows)

### Database Migrations

**Total**: 1 new migration

- **EventAttendance.SessionId**: Add SessionId field (if Option 1 chosen)
- **Data Backfill**: Script to assign SessionId to existing records

### Tests to Update/Create

**Total**: ~30-40 tests

**By Type**:
- **Backend Unit Tests**: 10-15 tests (AttendanceService, capacity logic)
- **Backend Integration Tests**: 5-8 tests (API endpoints, database queries)
- **Frontend Unit Tests**: 8-10 tests (components, hooks)
- **E2E Tests**: 7-10 tests (purchase flows, admin workflows, user dashboard)

---

## Dependencies & Prerequisites

### Must Complete Before Starting

1. **Database Migration Decision**: Choose SessionId field approach (Option 1 vs Option 2)
2. **Feature Flag Setup**: Implement feature flag system for gradual rollout
3. **Staging Environment Ready**: Ensure staging has production-like data for testing
4. **Rollback Plan**: Document rollback procedure if issues arise

### External Dependencies

- **PayPal Integration**: Verify session data included in webhook events
- **Email Templates**: Update confirmation emails to show sessions
- **Refund System**: Align refund logic with session-based tickets

### Nice-to-Have (Not Blocking)

- **Analytics Dashboard**: Add session-level metrics to admin analytics
- **Reports**: Update event reports to show per-session attendance
- **Mobile App**: If exists, update mobile ticket display

---

## Quality Gates

### Phase 1 Gate: Backend Logic ✅
- [ ] Unit tests for session-level duplicate prevention (100% pass)
- [ ] Integration tests for capacity calculations (100% pass)
- [ ] Manual QA: Purchase flow works for multi-session events
- [ ] Code review: AttendanceService changes approved

### Phase 2 Gate: DTOs & APIs ✅
- [ ] NSwag types regenerated and published
- [ ] API responses include session data (verified via Postman)
- [ ] Integration tests for all endpoints (100% pass)
- [ ] TypeScript compilation errors: 0

### Phase 3 Gate: Admin UI ✅
- [ ] Admin can configure per-session capacity
- [ ] Admin sees accurate sold counts per session
- [ ] E2E tests for admin workflows (100% pass)
- [ ] UI/UX review: Admin interface clear and intuitive

### Phase 4 Gate: Public UI ✅
- [ ] Users see accurate session availability
- [ ] Sold-out sessions clearly indicated
- [ ] E2E tests for public flows (100% pass)
- [ ] Accessibility review: WCAG 2.1 AA compliance

### Phase 5 Gate: Purchase Flow ✅
- [ ] Users can select specific sessions when purchasing
- [ ] Payment confirmation shows sessions
- [ ] E2E tests for purchase flow (100% pass)
- [ ] Payment provider integration verified (sandbox)

### Phase 6 Gate: User Dashboard ✅
- [ ] Users see their session-specific tickets
- [ ] Dashboard accurately reflects participation
- [ ] E2E tests for user dashboard (100% pass)
- [ ] Cross-browser testing: Chrome, Firefox, Safari, Edge

### Final Release Gate ✅
- [ ] All phase gates passed
- [ ] Staging deployment successful
- [ ] Manual QA: All critical workflows verified
- [ ] Performance testing: Page load times within targets
- [ ] Security review: No new vulnerabilities introduced
- [ ] Documentation complete: Admin guide, user guide, developer guide
- [ ] Rollback plan tested and ready
- [ ] Stakeholder approval for production deployment

---

## Success Metrics

### Technical Metrics
- **Test Coverage**: >90% for new/modified code
- **Zero TypeScript Errors**: 100% compilation success
- **E2E Test Pass Rate**: 100% (all critical workflows)
- **API Response Time**: <200ms for session queries (p95)
- **Database Query Performance**: <50ms for capacity checks (p95)

### Business Metrics
- **Purchase Conversion Rate**: No decrease from baseline
- **Admin Efficiency**: Time to configure events reduced by 20%
- **User Satisfaction**: Clear session selection reduces support tickets by 30%
- **Revenue Protection**: Zero oversold sessions after deployment

### Risk Mitigation Metrics
- **Rollback Time**: <15 minutes if critical issue detected
- **Data Integrity**: 100% of existing tickets remain valid
- **Zero Downtime**: Deployment causes <1 minute unavailability

---

## Next Steps

### Immediate (Before Phase 1)
1. **Decision**: Choose SessionId field approach (Option 1 recommended)
2. **Documentation**: Create detailed technical specification for Phase 1
3. **Setup**: Configure feature flag system
4. **Environment**: Prepare staging with production-like test data

### Phase 1 Kickoff
1. **Database Designer**: Create migration for EventAttendance.SessionId
2. **Backend Developer**: Refactor AttendanceService validation logic
3. **Test Developer**: Create comprehensive unit tests for new logic
4. **QA**: Manual testing on staging environment

### After Phase 1 Complete
1. **Human Review**: Quality gate review and approval
2. **Phase 2 Kickoff**: Backend DTOs & APIs work begins
3. **Parallel Work**: Frontend design work can start in parallel

---

## Related Documentation

### Project Documentation
- **Main README**: `/docs/functional-areas/payments/new-work/2025-12-07-session-based-ticketing/README.md`
- **Backend Analysis**: `/docs/functional-areas/payments/new-work/2025-12-07-session-based-ticketing/research/backend-code-analysis.md`
- **Frontend Analysis**: `/docs/functional-areas/payments/new-work/2025-12-07-session-based-ticketing/research/frontend-code-analysis.md`
- **Database Research**: `/docs/functional-areas/payments/new-work/2025-12-07-session-based-ticketing/research/database-research.md`

### Standards & Patterns
- **DTO Alignment Strategy**: `/docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md`
- **API Design Patterns**: `/docs/standards-processes/backend/api-design-patterns.md`
- **React Patterns**: `/docs/standards-processes/frontend/react-patterns.md`
- **Testing Standards**: `/docs/standards-processes/testing/TEST-CREATION-GUIDE.md`

### Historical Context
- **Database Migration**: Applied 2025-12-02 (TicketTypeSessions join table)
- **Similar Work**: Multi-session event creation (already supports multiple sessions)

---

## Conclusion

This comprehensive impact analysis reveals that **significant groundwork is already complete**:
- ✅ Database schema supports many-to-many TicketType↔Session
- ✅ DTOs include SessionIdentifiers
- ✅ Seeders populate session relationships

The **primary work remaining** is refactoring business logic from event-level to session-level validation, with corresponding frontend updates to support session selection during purchase.

**Recommendation**: Proceed with Phase 1 (Backend Business Logic) using the SessionId field approach for EventAttendance. Estimated timeline of **2-3 weeks** is achievable with parallel frontend/backend development.

**Risk Level**: MEDIUM - Core business logic changes require careful testing, but database foundation is solid and clear rollback path exists.

---

**Document Version**: 1.0
**Created**: 2025-12-07
**Last Updated**: 2025-12-07
**Status**: Complete - Ready for Phase Planning

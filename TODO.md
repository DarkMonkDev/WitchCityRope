# WitchCityRope - Active Development Priorities

**Last Updated**: 2025-10-26

## Immediate Priorities

### 1. E2E Test Stabilization (HIGH PRIORITY)
**Status**: In Progress
**Current**: 72.5% pass rate (194/268 tests)
**Target**: >90% pass rate
**Estimated Time**: 20-30 hours
**Blockers**: None

**Tasks**:
- [ ] Investigate and fix remaining E2E test failures
- [ ] Stabilize flaky tests
- [ ] Add missing test coverage for critical workflows
- [ ] Update test documentation

### 2. Incident Reporting Backend API (CRITICAL PATH)
**Status**: Frontend Complete (100%), Backend Pending
**Estimated Time**: 40-60 hours
**Blockers**: None - Ready to implement

**Tasks**:
- [ ] Implement 12 API endpoints (GET/POST/PUT for incidents, notes, assignments)
- [ ] Apply database migrations to development environment
- [ ] Configure authentication/authorization middleware
- [ ] Integration testing with frontend
- [ ] Security review (authentication, authorization, XSS prevention)

### 3. Pre-Existing TypeScript Errors (HIGH)
**Status**: 10 errors remaining
**Estimated Time**: 4-6 hours
**Impact**: Build succeeds but with warnings

**Tasks**:
- [ ] Audit all TypeScript errors
- [ ] Fix type mismatches
- [ ] Update type definitions
- [ ] Verify build passes cleanly

### 4. Modal Test Infrastructure (HIGH)
**Status**: 68 modal tests failing
**Estimated Time**: 6-8 hours
**Root Cause**: Mantine Modal rendering pattern

**Tasks**:
- [ ] Update modal test utilities
- [ ] Fix Mantine Modal async rendering issues
- [ ] Update all failing modal tests
- [ ] Document correct testing pattern

## Secondary Priorities

### 5. WebSocket HMR Warnings (LOW)
**Status**: Development-only warnings
**Estimated Time**: 2-3 hours
**Impact**: None (non-blocking)

### 6. CMS Phase 2 Features (DEFERRED)
**Status**: Phase 1 Complete, Phase 2 Not Scheduled
**Features**: Image uploads, video embeds, advanced formatting

## Completed Recent Work ✅

- ✅ **Oct 26**: Participation system fixes (auto-cancel RSVP, duplicate cards, cache invalidation)
- ✅ **Oct 24**: ProfilePage production bug fix, test suite stabilization (404/449 passing)
- ✅ **Oct 20**: Volunteer position signup system with auto-RSVP
- ✅ **Oct 18**: Incident reporting frontend (19 components, 239+ tests)
- ✅ **Oct 17**: CMS implementation complete (Phase 1)

## Notes

- **Test-Driven Development**: All new features require comprehensive tests
- **Documentation**: All major features documented in `/docs/functional-areas/`
- **Session Handoffs**: Available in `/docs/standards-processes/session-handoffs/`

---

For detailed development history and milestones, see [PROGRESS.md](./PROGRESS.md)

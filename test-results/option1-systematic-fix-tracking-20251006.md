# Option 1: Systematic Fix to 80% Pass Rate - Tracking Document

**Date**: 2025-10-06
**Status**: NOT STARTED
**Starting Pass Rate**: 158/277 (57.0%)
**Target Pass Rate**: 221/277 (80.0%)
**Gap**: +63 tests

## Executive Summary

Comprehensive systematic fix addressing root cause: three-way field name mismatches across backend DTOs, frontend types, and test mocks.

## Root Cause

**Three-way field name mismatch**:
- Backend API: `capacity`, `currentAttendees`, `startDateTime`, `endDateTime`
- Frontend DTOs: `capacity`, `registrationCount`, `startDate`, `endDate`
- Test Mocks: `maxAttendees`, `currentAttendees`, `startDateTime`, `endDateTime`

**Impact**: ~60 tests failing due to data contract misalignment

## Systematic Fix Strategy

### Phase 1: Backend DTO Standardization
**Agent**: backend-developer
**Status**: NOT STARTED
**Files**: Event DTOs in packages/contracts/
**Changes**:
- MaxAttendees → Capacity
- CurrentAttendees → RegistrationCount
- StartDateTime → StartDate
- EndDateTime → EndDate
- Added Location property

**Completion**: [DATE TIME]
**Summary**: [Link to backend-dto-standardization-20251006.md]

### Phase 2: Frontend Type Regeneration
**Agent**: react-developer
**Status**: BLOCKED - Pending Phase 1 completion
**Command**: `npm run generate:types`
**Files**: packages/shared-types/src/generated/api-client.ts
**Expected**: New types match backend DTO changes

**Completion**: [DATE TIME]
**Verification**: TypeScript compilation succeeds

### Phase 3: MSW Handler Updates (Parallel)
**Agent**: test-developer
**Status**: BLOCKED - Pending Phase 2 completion
**Files**: apps/web/src/test/mocks/handlers.ts
**Changes**: Update all event mocks to use new field names
**Expected Impact**: +40-50 tests

**Completion**: [DATE TIME]

### Phase 4: Component Type Usage Updates (Parallel)
**Agent**: react-developer
**Status**: BLOCKED - Pending Phase 2 completion
**Files**: Component files using Event types
**Changes**: Update property access to new field names
**Expected Impact**: Fix TypeScript compilation errors

**Completion**: [DATE TIME]

### Phase 5: Test Execution and Verification
**Agent**: test-executor
**Status**: BLOCKED - Pending Phases 3 & 4 completion
**Command**: `npm run test -- --run`
**Target**: 221+ tests passing (80%)

**Completion**: [DATE TIME]
**Final Pass Rate**: [X/277 (Y%)]

### Phase 6: Remaining Quick Wins
**Agent**: test-developer
**Status**: BLOCKED - Pending Phase 5 assessment
**Work**: Form labels, useVettingStatus, auth store fixes
**Expected Impact**: +10-18 tests

**Completion**: [DATE TIME]

## Progress Tracking

| Phase | Status | Pass Rate | Tests Fixed | Time Spent |
|-------|--------|-----------|-------------|------------|
| Start | ✅ | 158/277 (57.0%) | - | - |
| Phase 1 | ⏸️ | - | - | - |
| Phase 2 | ⏸️ | - | - | - |
| Phase 3 | ⏸️ | - | - | - |
| Phase 4 | ⏸️ | - | - | - |
| Phase 5 | ⏸️ | - | - | - |
| Phase 6 | ⏸️ | - | - | - |
| **Target** | - | **221/277 (80.0%)** | **+63** | **~9-13 hrs** |

## Files Modified

### Backend (Phase 1)
- [List files when phase completes]

### Frontend Types (Phase 2)
- packages/shared-types/src/generated/api-client.ts (auto-generated)

### MSW Handlers (Phase 3)
- apps/web/src/test/mocks/handlers.ts

### Components (Phase 4)
- [List files using Event types]

### Tests (Phase 6)
- [List test files with quick wins]

## Parallel Execution Strategy

**Phases 3 & 4 run in parallel after Phase 2 completes**:
- test-developer updates MSW handlers
- react-developer updates component usage
- Both complete before Phase 5 test execution

## Blocking Dependencies

- Phase 2 blocks Phases 3 & 4 (need generated types first)
- Phases 3 & 4 block Phase 5 (need code updates before testing)
- Phase 5 blocks Phase 6 (assess remaining failures)

## Estimated Timeline

- **Phase 1**: 1-2 hours (backend DTO changes)
- **Phase 2**: 15 minutes (type regeneration)
- **Phases 3 & 4 (parallel)**: 2-3 hours (MSW + components)
- **Phase 5**: 30 minutes (test execution)
- **Phase 6**: 2-3 hours (remaining quick wins)

**Total**: 6-9 hours (with parallel execution)

## Success Criteria

✅ **Minimum**: 221/277 tests passing (80.0%)
✅ **Stretch**: 230/277 tests passing (83.0%)
✅ **All**: No TypeScript compilation errors
✅ **All**: MSW handlers match backend API exactly
✅ **All**: Component usage matches generated types

## Risk Mitigation

**Risk**: Breaking changes to frontend components
**Mitigation**: Systematic TypeScript compilation checks at each phase

**Risk**: New test failures introduced
**Mitigation**: Run tests after each phase, track regressions

**Risk**: Time overrun
**Mitigation**: Stop at 80% if reached earlier than Phase 6

## Communication

**Stakeholder Updates**:
- After Phase 1: Backend changes complete
- After Phase 2: Types regenerated
- After Phase 5: Initial test results
- Final: 80% achieved or honest assessment

## Lessons Learned (Post-Completion)

[To be filled after completion]

---

**Last Updated**: 2025-10-06 (Initial Creation)
**Current Phase**: NOT STARTED
**Current Pass Rate**: 158/277 (57.0%)
**Status**: AWAITING ORCHESTRATOR DELEGATION

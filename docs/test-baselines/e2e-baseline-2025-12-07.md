# E2E Test Baseline - December 7, 2025

## Test Run Summary

| Metric | Count |
|--------|-------|
| **Passed** | 643 |
| **Failed** | 92 |
| **Skipped** | 72 |
| **Total Runnable** | 807 |
| **Pass Rate** | **87.4%** |
| **Run Time** | 10.1 minutes |

## Environment
- **Container Type**: Test Container (witchcityrope-test)
- **Test Framework**: Playwright
- **Workers**: 6
- **Date**: 2025-12-07

## Comparison to Previous Baseline (December 2, 2025)

| Metric | Dec 2 | Dec 7 | Change |
|--------|-------|-------|--------|
| Passed | 622 | 643 | **+21** |
| Failed | 111 | 92 | **-19** |
| Skipped | 74 | 72 | -2 |
| Pass Rate | 84.9% | 87.4% | **+2.5%** |

## Failing Tests by Category (92 total)

### CHECK-IN MODULE (17 tests)
| File | Test Name | Line |
|------|-----------|------|
| admin-checkin-sessions.spec.ts | should show session selector in token generation modal for multi-session events | 59 |
| admin-checkin-sessions.spec.ts | should require session selection before generating token (multi-session event) | 128 |
| admin-checkin-sessions.spec.ts | should display session name in generated token list | 310 |
| checkin-attendee-workflow.spec.ts | Check in a registered attendee | 75 |
| checkin-attendee-workflow.spec.ts | Cannot check in same attendee twice | 118 |
| checkin-attendee-workflow.spec.ts | Two-step check-in workflow (Covid Test → Check In) | 155 |
| checkin-attendee-workflow.spec.ts | Token validation fails for expired token during check-in | 207 |
| checkin-dashboard.spec.ts | Dashboard displays correct statistics | 62 |
| checkin-dashboard.spec.ts | Dashboard shows event information | 92 |
| checkin-dashboard.spec.ts | Recent check-ins section displays | 110 |
| checkin-dashboard.spec.ts | Sync status displays | 127 |
| checkin-dashboard.spec.ts | Dashboard navigation from check-in interface | 145 |
| checkin-staff-authentication.spec.ts | Valid token allows access to check-in interface | 57 |
| checkin-staff-authentication.spec.ts | Token for wrong event returns error | 143 |
| checkin-staff-authentication.spec.ts | Revoked token cannot be used | 174 |
| checkin-staff-authentication.spec.ts | No authentication required for valid token | 217 |
| checkin-staff-authentication.spec.ts | Expired token shows error message | 253 |

### VETTING MODULE (13 tests)
| File | Test Name | Line |
|------|-----------|------|
| vetting-admin-dashboard.spec.ts | admin can view vetting applications grid | 30 |
| vetting-application-detail.spec.ts | admin can put application on hold with reasoning | 172 |
| vetting-application-workflow.spec.ts | incomplete form shows validation errors and does not submit | 314 |
| vetting-complete-flow.spec.ts | Complete vetting application with registration and login | 11 |
| vetting-notes-direct.spec.ts | Verify notes appear after stage advancement - Direct navigation | 5 |
| vetting-notes-display.spec.ts | Verify notes appear after stage advancement | 5 |
| vetting-profile-update.spec.ts | user submits application with all fields - profile fully updated | 40 |
| vetting-profile-update.spec.ts | profile updates are visible in user dashboard after submission | 178 |
| vetting-success-screen-verification.spec.ts | Complete vetting application flow with success screen verification | 10 |
| vetting-system-basic.spec.ts | Basic vetting discovery and authentication workflow | 24 |
| vetting-system-complete-workflows.spec.ts | Navigation to Application Detail | 115 |
| vetting-system-complete-workflows.spec.ts | Put on Hold Modal Flow | 168 |
| vetting-system-complete-workflows.spec.ts | Send Reminder Modal Flow | 236 |
| vetting-system.spec.ts | Complete vetting workflow from discovery to approval | 23 |
| vetting-workflow.spec.ts | user can submit vetting application successfully | 40 |

### EVENT MANAGEMENT (15 tests)
| File | Test Name | Line |
|------|-----------|------|
| admin-events-volunteers.spec.ts | should add volunteer position via inline form | 102 |
| admin-events-volunteers.spec.ts | should validate volunteer position form fields | 252 |
| admin-events-volunteers.spec.ts | should display sessions in day format in position assignments | 304 |
| event-update-e2e-test.spec.ts | should access AdminEventDetailsPage via admin/events route | 23 |
| event-update-e2e-test.spec.ts | should show EventForm components and attempt event update | 64 |
| event-update-e2e-test.spec.ts | should test partial update behavior | 285 |
| event-update-e2e-test.spec.ts | should handle authentication and authorization | 345 |
| event-update-e2e-test.spec.ts | should test publish/draft status toggle | 177 |
| event-update-e2e-test.spec.ts | should validate API endpoint responses | 370 |
| events-complete-workflow.spec.ts | Step 2: Admin Event Editing - Login as admin and update event details | 189 |
| events-comprehensive.spec.ts | should handle empty events state | 112 |
| events-management-e2e.spec.ts | should load Event Session Matrix demo page | 246 |
| events-management-e2e.spec.ts | should display event form tabs | 270 |
| events-management-e2e.spec.ts | should verify form fields are present | 338 |
| events-policies-field-comprehensive.spec.ts | should display policies field in event form | 39 |
| events-policies-field-comprehensive.spec.ts | should validate policies field as REQUIRED | 87 |
| events-policies-field-comprehensive.spec.ts | should save policies field and persist after page refresh | 132 |

### ADMIN DASHBOARD (2 tests)
| File | Test Name | Line |
|------|-----------|------|
| admin-dashboard-workflow.spec.ts | should update Google Drive links for incident | 205 |
| admin-dashboard-workflow.spec.ts | should add investigation note to incident | 260 |

### ADMIN SESSION/REFUND (1 test)
| File | Test Name | Line |
|------|-----------|------|
| admin-session-deletion.spec.ts | cannot delete session with paid tickets - shows blocked modal with disabled button | 182 |

### CMS (4 tests)
| File | Test Name | Line |
|------|-----------|------|
| cms-workflow.spec.ts | Mobile viewport: Navigation works on mobile | 241 |
| cms.spec.ts | Happy Path: Admin can edit and save page content | 45 |
| cms.spec.ts | XSS Prevention: Backend sanitizes malicious HTML | 157 |
| cms.spec.ts | Performance: Save response time < 1 second | 393 |

### REPORTS (4 tests)
| File | Test Name | Line |
|------|-----------|------|
| anonymous-report-submission.spec.ts | should submit anonymous incident report and receive reference number | 39 |
| anonymous-report-submission.spec.ts | should validate required fields before submission | 131 |
| identified-report-submission.spec.ts | should toggle between anonymous and identified modes | 39 |
| identified-report-submission.spec.ts | should show empty state when user has no reports | 84 |

### REGISTRATION (3 tests)
| File | Test Name | Line |
|------|-----------|------|
| registration-tos.spec.ts | Positive: User can register when Terms of Service checkbox is checked | 40 |
| registration-tos.spec.ts | Positive: Database shows TermsOfServiceAccepted=true and timestamp after registration | 107 |
| registration-tos.spec.ts | Positive: Newly registered user can successfully log in | 171 |

### RSVP (3 tests)
| File | Test Name | Line |
|------|-----------|------|
| comprehensive-rsvp-verification.spec.ts | 3. Admin Event Details - RSVP Tab Content | 232 |
| rsvp-lifecycle-persistence.spec.ts | should persist RSVP to database | 70 |
| rsvp-lifecycle-persistence.spec.ts | should handle rapid RSVP/cancel cycles | 293 |

### PROFILE (2 tests)
| File | Test Name | Line |
|------|-----------|------|
| profile-page.spec.ts | should handle user loading error | 16 |
| profile-update-persistence.spec.ts | should persist profile changes after save and page refresh | 30 |

### CHECKOUT/TICKETS (3 tests)
| File | Test Name | Line |
|------|-----------|------|
| checkout-workflow.spec.ts | Payment form displays required fields | 491 |
| test-checkout.spec.js | Free RSVP ticket - fixed price display | 12 |
| ticket-lifecycle-persistence.spec.ts | should persist cancellation reason to database | 102 |

### VENUE (1 test)
| File | Test Name | Line |
|------|-----------|------|
| venue-editing.spec.ts | should update venue notes (admin-only field) | 109 |

### PHASE TESTING (4 tests)
| File | Test Name | Line |
|------|-----------|------|
| phase3-sessions-tickets.spec.ts | Session CRUD - Add, edit, and delete sessions | 23 |
| phase3-sessions-tickets.spec.ts | Ticket Types - Create and manage ticket types | 106 |
| phase4-events-testing.spec.ts | should display event filters correctly | 23 |
| phase4-events-testing.spec.ts | should be responsive on mobile viewport | 69 |

### MOBILE/NAVIGATION (6 tests)
| File | Test Name | Line |
|------|-----------|------|
| navigation-workflow.spec.ts | Mobile hamburger menu - opens and displays navigation items | 183 |
| navigation-workflow.spec.ts | Mobile menu - authenticated user sees dashboard and logout | 249 |
| navigation-workflow.spec.ts | Mobile menu - admin user sees admin link | 293 |
| navigation-workflow.spec.ts | Mobile menu logout - logs out user and closes menu | 393 |
| scroll-restoration.spec.ts | scrolls to top when navigating from events to homepage - MOBILE | 284 |
| scroll-restoration.spec.ts | hamburger menu opens and resets body overflow on navigation - MOBILE | 396 |

### CSRF/AUTH (1 test)
| File | Test Name | Line |
|------|-----------|------|
| csrf-token-validation.spec.ts | should complete full login/logout flow with CSRF token | 20 |

### TIPTAP EDITORS (2 tests)
| File | Test Name | Line |
|------|-----------|------|
| tiptap-editors.spec.ts | should render Email Content Tiptap editor on Emails tab | 89 |
| tiptap-editors.spec.ts | comprehensive: all three editors render on their respective tabs | 167 |

### SESSION TIMING (1 test)
| File | Test Name | Line |
|------|-----------|------|
| session-based-timing.spec.ts | admin can view session-based timing settings | 265 |

### INFRASTRUCTURE/OTHER (5 tests)
| File | Test Name | Line |
|------|-----------|------|
| compare-wireframe.spec.ts | capture original wireframe | 13 |
| docker-services-test.spec.ts | should have correct baseURL configuration | 78 |
| e2e-events-full-journey.spec.ts | Environment Health Check | 420 |
| notification-system-test.spec.ts | Notifications container appears when notification is shown | 13 |
| user-dashboard-vetting-status.spec.ts | dashboard API returns correct VettingStatus enum values | 84 |
| manual-vetting-submission-test.spec.ts | should submit vetting application without 400 error | 5 |

## Key Observations

### Systemic Issues
1. **Check-In Module (17 tests)** - Largest failure group, likely infrastructure/token validation issues
2. **Vetting Module (13+ tests)** - Form validation and workflow issues, possible timeout problems (33+ second test durations)
3. **Event Management (15 tests)** - Various event form and update issues
4. **Mobile Tests (6 tests)** - Mobile navigation and hamburger menu issues

### Test Durations
Several tests have very long durations (30-45 seconds) suggesting:
- Network/API timeout issues
- Element wait failures
- Possible test infrastructure problems

## Recommended Fix Priorities

1. **HIGH**: Check-In Module - Core functionality, affects event day operations
2. **HIGH**: Vetting Module - Core member onboarding workflow
3. **MEDIUM**: Event Management - Admin functionality
4. **MEDIUM**: Mobile Navigation - User experience
5. **LOW**: Infrastructure tests - Test framework issues, not app bugs

## Conclusion

**Progress from December 2nd Baseline** ✅
- 21 more tests passing
- 19 fewer failures
- 2.5% improvement in pass rate (84.9% → 87.4%)

Focus areas for next iteration:
1. Check-In infrastructure and token validation
2. Vetting form submission and workflow
3. Event form persistence and validation
4. Mobile navigation menu behavior

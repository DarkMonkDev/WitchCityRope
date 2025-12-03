# E2E Failing Tests Tracker

## Purpose

This file tracks all E2E tests currently failing, their failure reasons, and fix status. Use this file to:
1. Track which tests need fixing without re-running the full test suite
2. Mark tests as "fixed but not tested" when code changes are made
3. Remove entries once tests are verified passing

## Workflow

1. **When you fix a test**: Change status from `FAILING` to `FIXED_NOT_TESTED`
2. **When you verify a fix**: Remove the entry entirely from this file
3. **When re-running tests**: Update failure reasons if they changed

## Test Run Info
- **Date**: December 2, 2025
- **Total Tests**: 733
- **Passed**: ~613
- **Failed**: 120
- **Pass Rate**: ~84%

---

## CHECK-IN MODULE (17 failures)

### admin-checkin-sessions (3 tests)
| Test | Status | Failure Reason |
|------|--------|----------------|
| Session select is optional for multi-session events | FAILING | TBD |
| Session name in generated token list | FAILING | TBD |
| Token multi-session event | FAILING | TBD |

### checkin-attendee-workflow (4 tests)
| Test | Status | Failure Reason |
|------|--------|----------------|
| Covid Test -> Check-In flow | FAILING | TBD |
| Check in a registered attendee | FAILING | TBD |
| Cannot check in same attendee twice | FAILING | TBD |
| Expired token during check-in | FAILING | TBD |

### checkin-dashboard (5 tests)
| Test | Status | Failure Reason |
|------|--------|----------------|
| Displays correct statistics | FAILING | TBD |
| Check-ins section displays | FAILING | TBD |
| Dashboard shows event information | FAILING | TBD |
| Navigation from check-in interface | FAILING | TBD |
| Sync status displays | FAILING | TBD |

### checkin-staff-authentication (5 tests)
| Test | Status | Failure Reason |
|------|--------|----------------|
| Wrong event returns error | FAILING | TBD |
| Revoked token cannot be used | FAILING | TBD |
| Access to check-in interface | FAILING | TBD |
| Invalid token shows error message | FAILING | TBD |
| Authentication required for valid token | FAILING | TBD |

---

## ADMIN DASHBOARD (2 failures)

### admin-dashboard-workflow (2 tests)
| Test | Status | Failure Reason |
|------|--------|----------------|
| Google Drive links for incident | FAILING | TBD |
| Investigation note to incident | FAILING | TBD |

---

## ADMIN EVENTS (4 failures)

### admin-events-sessions (1 test)
| Test | Status | Failure Reason |
|------|--------|----------------|
| Edit existing session via modal | FAILING | TBD |

### admin-events-volunteers (3 tests)
| Test | Status | Failure Reason |
|------|--------|----------------|
| Volunteer position form fields | FAILING | TBD |
| Session format in position assignments | FAILING | TBD |
| Add volunteer position via inline form | FAILING | TBD |

---

## ADMIN REFUND (1 failure - 2 FIXED 2025-12-03)

### admin-refund-eligibility (0 tests - 2 FIXED)
| Test | Status | Failure Reason |
|------|--------|----------------|
| ~~Updated after successful refund~~ | ~~FIXED~~ | Fixed: `.or()` locator syntax, partial refund status |
| ~~Can be processed in sequence~~ | ~~FIXED~~ | Fixed: `.or()` locator syntax, status guards |

### admin-session-deletion (1 test)
| Test | Status | Failure Reason |
|------|--------|----------------|
| Modal with disabled button | FAILING | TBD |

---

## REFUND WORKFLOW (6 failures)

### refund-validations (4 tests)
| Test | Status | Failure Reason |
|------|--------|----------------|
| Counter updates in real-time | FAILING | TBD |
| Counter displays correctly | FAILING | TBD |
| Without confirmation checkbox | FAILING | TBD |
| Invalid refund reason | FAILING | TBD |

### refund-workflow (2 tests)
| Test | Status | Failure Reason |
|------|--------|----------------|
| Refund without refund reason | FAILING | TBD |
| Without confirmation checkbox | FAILING | TBD |

---

## EVENTS (22 failures)

### e2e-events-full-journey (2 tests)
| Test | Status | Failure Reason |
|------|--------|----------------|
| API Integration Verification | FAILING | TBD |
| Environment Health Check | FAILING | TBD |

### events-complete-workflow (2 tests)
| Test | Status | Failure Reason |
|------|--------|----------------|
| Login as member and RSVP to event | FAILING | TBD |
| Login and update event details | FAILING | TBD |

### events-comprehensive (3 tests)
| Test | Status | Failure Reason |
|------|--------|----------------|
| Number of events efficiently | FAILING | TBD |
| Handle empty events state | FAILING | TBD |
| Navigation for authenticated users | FAILING | TBD |

### events-management-e2e (3 tests)
| Test | Status | Failure Reason |
|------|--------|----------------|
| Event Session Matrix demo page | FAILING | TBD |
| Should display event form tabs | FAILING | TBD |
| Verify form fields are present | FAILING | TBD |

### events-policies-field-comprehensive (3 tests)
| Test | Status | Failure Reason |
|------|--------|----------------|
| Persist after page refresh | FAILING | TBD |
| Policies field as REQUIRED | FAILING | TBD |
| Policies field in event form | FAILING | TBD |

### event-update-e2e-test (6 tests)
| Test | Status | Failure Reason |
|------|--------|----------------|
| Test partial update behavior | FAILING | TBD |
| Authentication and authorization | FAILING | TBD |
| Attempt event update | FAILING | TBD |
| Page via admin events route | FAILING | TBD |
| Update API endpoint responses | FAILING | TBD |
| Publish/draft status toggle | FAILING | TBD |

### phase3-sessions-tickets (2 tests)
| Test | Status | Failure Reason |
|------|--------|----------------|
| Create and manage ticket types | FAILING | TBD |
| Add, edit and delete sessions | FAILING | TBD |

### phase4-events-testing (2 tests)
| Test | Status | Failure Reason |
|------|--------|----------------|
| Display event filters correctly | FAILING | TBD |
| Responsive on mobile viewport | FAILING | TBD |

### session-based-timing (1 test)
| Test | Status | Failure Reason |
|------|--------|----------------|
| Session-based timing settings | FAILING | TBD |

---

## PUBLIC EVENTS (4 failures)

### public-events-anonymous (4 tests)
| Test | Status | Failure Reason |
|------|--------|----------------|
| View events without authentication | FAILING | TBD |
| Events returned to anonymous users | FAILING | TBD |
| Matches EventDto structure | FAILING | TBD |
| Authentication returns 401 | FAILING | TBD |

---

## CHECKOUT/TICKETS (4 failures)

### checkout-workflow (1 test)
| Test | Status | Failure Reason |
|------|--------|----------------|
| Form displays required fields | FAILING | TBD |

### test-checkout (1 test)
| Test | Status | Failure Reason |
|------|--------|----------------|
| Fixed price ticket display | FAILING | TBD |

### ticket-lifecycle-persistence (1 test)
| Test | Status | Failure Reason |
|------|--------|----------------|
| Cancellation reason to database | FAILING | TBD |

### ticket-refund-workflow (0 tests - 1 FIXED 2025-12-03)
| Test | Status | Failure Reason |
|------|--------|----------------|
| ~~Admin can complete refund workflow~~ | ~~FIXED~~ | Fixed: `.or()` locator syntax |

---

## CMS (3 failures)

### cms-CMS-Feature (3 tests)
| Test | Status | Failure Reason |
|------|--------|----------------|
| Save response time 1 second | FAILING | TBD |
| Edit and save page content | FAILING | TBD |
| Sanitizes malicious HTML | FAILING | TBD |

---

## DASHBOARD (5 failures)

### dashboard-comprehensive (5 tests)
| Test | Status | Failure Reason |
|------|--------|----------------|
| Display correctly on Tablet | FAILING | TBD |
| Correct layout and navigation | FAILING | TBD |
| Validate password change form | FAILING | TBD |
| Display correctly on Mobile | FAILING | TBD |
| Validate profile form fields | FAILING | TBD |

---

## HOME PAGE (3 failures)

### home-page-Home-Page (3 tests)
| Test | Status | Failure Reason |
|------|--------|----------------|
| API-PostgreSQL stack works | FAILING | TBD |
| Different screen sizes | FAILING | TBD |
| Events display from API | FAILING | TBD |

---

## VETTING (18 failures)

### vetting-admin-dashboard (1 test)
| Test | Status | Failure Reason |
|------|--------|----------------|
| Vetting applications grid | FAILING | TBD |

### vetting-application-detail (2 tests)
| Test | Status | Failure Reason |
|------|--------|----------------|
| View application details | FAILING | TBD |
| Application on hold with reasoning | FAILING | TBD |

### vetting-application-workflow (2 tests)
| Test | Status | Failure Reason |
|------|--------|----------------|
| Errors and does not submit | FAILING | TBD |
| Application successfully | FAILING | TBD |

### vetting-complete-flow (1 test)
| Test | Status | Failure Reason |
|------|--------|----------------|
| Registration and login | FAILING | TBD |

### vetting-menu-visibility (3 tests)
| Test | Status | Failure Reason |
|------|--------|----------------|
| Without vetting application | FAILING | TBD |
| Show How to Join menu item | FAILING | TBD |
| Page when How to Join clicked | FAILING | TBD |

### vetting-notes-direct (1 test)
| Test | Status | Failure Reason |
|------|--------|----------------|
| Direct navigation | FAILING | TBD |

### vetting-notes-display (1 test)
| Test | Status | Failure Reason |
|------|--------|----------------|
| Clear after stage advancement | FAILING | TBD |

### vetting-profile-update (2 tests)
| Test | Status | Failure Reason |
|------|--------|----------------|
| Profile fully updated | FAILING | TBD |
| Dashboard after submission | FAILING | TBD |

### vetting-success-screen (1 test)
| Test | Status | Failure Reason |
|------|--------|----------------|
| Success screen verification | FAILING | TBD |

### vetting-system-basic (1 test)
| Test | Status | Failure Reason |
|------|--------|----------------|
| Authentication workflow | FAILING | TBD |

### vetting-system-complete-workflows (2 tests)
| Test | Status | Failure Reason |
|------|--------|----------------|
| Send Reminder Modal Flow | FAILING | TBD |
| Put on Hold Modal Flow | FAILING | TBD |

### vetting-system (1 test)
| Test | Status | Failure Reason |
|------|--------|----------------|
| From discovery to approval | FAILING | TBD |

---

## REPORTS (4 failures)

### anonymous-report-submission (2 tests)
| Test | Status | Failure Reason |
|------|--------|----------------|
| Required fields before submission | FAILING | TBD |
| Receive reference number | FAILING | TBD |

### identified-report-submission (2 tests)
| Test | Status | Failure Reason |
|------|--------|----------------|
| State when user has no reports | FAILING | TBD |
| Anonymous and identified modes | FAILING | TBD |

---

## RSVP (3 failures)

### comprehensive-rsvp-verification (1 test)
| Test | Status | Failure Reason |
|------|--------|----------------|
| RSVP Tab Content | FAILING | TBD |

### rsvp-lifecycle-persistence (2 tests)
| Test | Status | Failure Reason |
|------|--------|----------------|
| Rapid RSVP cancel cycles | FAILING | TBD |
| Persist RSVP to database | FAILING | TBD |

---

## PROFILE (3 failures)

### profile-page (2 tests)
| Test | Status | Failure Reason |
|------|--------|----------------|
| Account information correctly | FAILING | TBD |
| Handle user loading error | FAILING | TBD |

### profile-update-persistence (1 test)
| Test | Status | Failure Reason |
|------|--------|----------------|
| After save and page refresh | FAILING | TBD |

---

## REGISTRATION (3 failures)

### registration-tos (3 tests)
| Test | Status | Failure Reason |
|------|--------|----------------|
| Timestamp after registration | FAILING | TBD |
| Terms of Service checkbox is checked | FAILING | TBD |
| User can successfully log in | FAILING | TBD |

---

## LOGIN/AUTH (3 failures)

### login-with-scene-name (2 tests)
| Test | Status | Failure Reason |
|------|--------|----------------|
| Explaining both login options | FAILING | TBD |
| Case sensitive for scene name | FAILING | TBD |

### csrf-token-validation (1 test)
| Test | Status | Failure Reason |
|------|--------|----------------|
| Logout flow with CSRF token | FAILING | TBD |

---

## VENUE (3 failures)

### venue-editing (3 tests)
| Test | Status | Failure Reason |
|------|--------|----------------|
| Venue notes admin only field | FAILING | TBD |
| Venues in admin dropdown | FAILING | TBD |
| Venue active/inactive status | FAILING | TBD |

---

## TIPTAP EDITORS (2 failures)

### tiptap-editors (2 tests)
| Test | Status | Failure Reason |
|------|--------|----------------|
| Tiptap editor on Emails tab | FAILING | TBD |
| Editor on their respective tabs | FAILING | TBD |

---

## DOCKER/INFRASTRUCTURE (2 failures)

### docker-services-test (2 tests)
| Test | Status | Failure Reason |
|------|--------|----------------|
| Correct baseURL configuration | FAILING | TBD |
| Connect to existing web service | FAILING | TBD |

---

## OTHER (6 failures)

### compare-wireframe (1 test)
| Test | Status | Failure Reason |
|------|--------|----------------|
| Capture original wireframe | FAILING | TBD |

### manual-vetting-submission (1 test)
| Test | Status | Failure Reason |
|------|--------|----------------|
| Application without 400 error | FAILING | TBD |

### notification-system-test (1 test)
| Test | Status | Failure Reason |
|------|--------|----------------|
| When notification is shown | FAILING | TBD |

### user-dashboard-vetting-status (1 test)
| Test | Status | Failure Reason |
|------|--------|----------------|
| VettingStatus enum values | FAILING | TBD |

---

## Status Legend

- **FAILING**: Test is currently failing
- **FIXED_NOT_TESTED**: Code fix applied, needs verification
- **(Remove entry)**: Test verified passing, remove from this file

## Notes

- Tests are grouped by functional area for easier navigation
- "TBD" in Failure Reason means the specific error needs investigation
- Focus on systemic issues first (check-in infrastructure, refund workflow)
- Check-in module has the most failures (17) - likely infrastructure issue
- Vetting module also has many failures (18) - possible systemic cause

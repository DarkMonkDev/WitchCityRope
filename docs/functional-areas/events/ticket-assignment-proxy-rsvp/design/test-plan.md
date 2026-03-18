# Test Plan: Ticket Assignment & Proxy RSVP

<!-- Last Updated: 2026-03-18 -->
<!-- Purpose: Comprehensive test cases for test-developer agents -->

## Test Scope

19 API endpoints across 5 feature groups, 4 services, 1 background job.

---

## 1. Unit Tests: AuthorizedContactService

### Happy Path
| ID | Test Case | Expected |
|----|-----------|----------|
| AC-U01 | GetContacts returns delegates and principals for user | Both lists populated correctly with scene names |
| AC-U02 | AddContact creates relationship between two users | Returns DTO with correct direction, CreatedAt set |
| AC-U03 | RevokeContact soft-deletes active relationship | RevokedAt set, IsActive false |
| AC-U04 | SearchUsers finds matching scene names | Returns max 10, scene name only, ordered |
| AC-U05 | SearchUsers excludes self and already-authorized | Current user and existing contacts not in results |
| AC-U06 | GetPrincipals returns people who authorized current user | Correct direction, includes IsVetted |
| AC-U07 | GetPrincipals with eventId filters by vetting for VettedMembersOnly | Non-vetted principals excluded |
| AC-U08 | GetPrincipals with eventId excludes users with existing attendance | Users with Active/PendingAcceptance excluded |
| AC-U09 | IsAuthorizedDelegate returns true for active relationship | Boolean check |
| AC-U10 | IsAuthorizedDelegate returns false for revoked relationship | Boolean check |

### Edge Cases
| ID | Test Case | Expected |
|----|-----------|----------|
| AC-E01 | AddContact with self (BR-003) | Error: "Cannot authorize yourself" |
| AC-E02 | AddContact duplicate active relationship | Error: 409 conflict |
| AC-E03 | AddContact after previous was revoked | Success: new record created |
| AC-E04 | RevokeContact when caller is not principal | Error: 403 forbidden |
| AC-E05 | RevokeContact on already-revoked contact | Error: "not found" (filtered by active) |
| AC-E06 | SearchUsers with query < 2 characters | Error: 400 bad request |
| AC-E07 | SearchUsers with no matches | Empty list, not error |
| AC-E08 | GetPrincipals for user with no authorizations | Empty list |
| AC-E09 | AddContact with non-existent user | Error: 404 |
| AC-E10 | Mutual authorization (A->B and B->A) | Both succeed independently (BR-004) |

---

## 2. Unit Tests: TicketAssignmentService

### Happy Path
| ID | Test Case | Expected |
|----|-----------|----------|
| TA-U01 | AssignTicket to authorized contact | Status -> PendingAcceptance, AssignedByUserId set |
| TA-U02 | AcceptAssignment with waiver accepted | Status -> Active, waiver fields set, AcceptedAt set |
| TA-U03 | AcceptAssignment creates auto-RSVP for social events | RSVP EventAttendance created |
| TA-U04 | AcceptAssignment updates ToS on user when needed | ApplicationUser.TermsOfServiceAccepted = true |
| TA-U05 | AcceptAssignment creates EventAttendee for check-in | EventAttendee record exists |
| TA-U06 | DeclineAssignment reverts ticket to purchaser | UserId = original purchaser, Status = Active, DeclinedAt set |
| TA-U07 | ReassignTicket to new authorized contact | New PendingAcceptance, DeclinedAt cleared |
| TA-U08 | GetPendingAssignments returns user's pending tickets/RSVPs | Ordered by event date |
| TA-U09 | GetAssignedTickets returns purchaser's assigned tickets | Shows CanReassign for declined |
| TA-U10 | AttendanceHistory created for each operation | Correct ActionType per operation |

### Edge Cases
| ID | Test Case | Expected |
|----|-----------|----------|
| TA-E01 | AssignTicket without authorization (BR-020) | Error: 403 |
| TA-E02 | AssignTicket to user who already has ticket (BR-012) | Error: 409 |
| TA-E03 | AssignTicket to non-vetted user for VettedMembersOnly event (BR-035) | Error: 403 |
| TA-E04 | AssignTicket when caller doesn't own the ticket | Error: 403 |
| TA-E05 | AcceptAssignment when not the assigned user | Error: 403 |
| TA-E06 | AcceptAssignment without waiver (AD-003) | Error: 400 |
| TA-E07 | AcceptAssignment when vetting revoked (BR-036) | Error: 403 |
| TA-E08 | AcceptAssignment for non-PendingAcceptance ticket | Error: 404 |
| TA-E09 | DeclineAssignment when not the assigned user | Error: 403 |
| TA-E10 | ReassignTicket when not the original purchaser | Error: 403 |
| TA-E11 | ReassignTicket on ticket without DeclinedAt | Error: 400 |
| TA-E12 | Ticket is irrevocable once accepted (AD-008) | No reclaim endpoint/path exists |
| TA-E13 | AssignTicket on non-existent attendance | Error: 404 |
| TA-E14 | AssignTicket on RSVP (not ticket) | Error: 400 |
| TA-E15 | AcceptAssignment also checks session overlap | Handles multi-session tickets |

---

## 3. Unit Tests: ProxyRsvpService

### Happy Path
| ID | Test Case | Expected |
|----|-----------|----------|
| PR-U01 | CreateProxyRsvp creates PendingAcceptance RSVP | Status = PendingAcceptance, AssignedByUserId set |
| PR-U02 | AcceptProxyRsvp activates RSVP with waiver | Status -> Active, waiver fields set |
| PR-U03 | AcceptProxyRsvp creates EventAttendee | Check-in record exists |
| PR-U04 | DeclineProxyRsvp cancels the RSVP | Status -> Cancelled, DeclinedAt set |

### Edge Cases
| ID | Test Case | Expected |
|----|-----------|----------|
| PR-E01 | CreateProxyRsvp on event without AllowRsvps | Error: 400 |
| PR-E02 | CreateProxyRsvp without authorization (BR-050) | Error: 403 |
| PR-E03 | CreateProxyRsvp when target already has RSVP | Error: 409 |
| PR-E04 | CreateProxyRsvp at capacity (BR-054) | Error: 400 |
| PR-E05 | CreateProxyRsvp for non-vetted user on VettedMembersOnly (BR-035) | Error: 403 |
| PR-E06 | AcceptProxyRsvp when vetting revoked (BR-036) | Error: 403 |
| PR-E07 | AcceptProxyRsvp without waiver | Error: 400 |
| PR-E08 | DeclineProxyRsvp when not the assigned user | Error: 403 |

---

## 4. Unit Tests: AssignmentReminderService

### Happy Path
| ID | Test Case | Expected |
|----|-----------|----------|
| AR-U01 | Sends reminders for events starting within 24 hours | Emails sent, ReminderSentAt set |
| AR-U02 | Skips assignments where ReminderSentAt already set (BR-062) | No duplicate sends |
| AR-U03 | Handles both Ticket and RSVP attendance types | Both types get reminders |

### Edge Cases
| ID | Test Case | Expected |
|----|-----------|----------|
| AR-E01 | No pending assignments | Completes without error, logs "0 reminders" |
| AR-E02 | Event more than 24 hours away | No reminders sent |
| AR-E03 | Event already passed | No reminders sent |
| AR-E04 | Email send failure | ReminderSentAt NOT set (retry on next run), other reminders still processed |

---

## 5. Unit Tests: Multi-Ticket Checkout (AttendanceService modifications)

### Happy Path
| ID | Test Case | Expected |
|----|-----------|----------|
| MT-U01 | Single ticket purchase (backward compatibility) | Works exactly as before with TicketTypeIds |
| MT-U02 | Multi-ticket with TicketSelections quantity 2 | 2 TicketPurchases, 2+ EventAttendances |
| MT-U03 | Multi-ticket with assignee specified | Assignee's ticket = PendingAcceptance, purchaser's = PendingPayment |
| MT-U04 | Multi-ticket with "assign later" (null assignee) | Extra tickets owned by purchaser |
| MT-U05 | Sliding scale applies uniformly to all tickets (AD-012) | All TicketPurchases have same per-ticket price |
| MT-U06 | ActivateAttendances: assigned tickets -> PendingAcceptance | Not Active, AssignedByUserId preserved |
| MT-U07 | ActivateAttendances: purchaser's tickets -> Active | Normal activation |
| MT-U08 | Auto-RSVP only for purchaser, not assignees | Only purchaser gets auto-RSVP |

### Edge Cases
| ID | Test Case | Expected |
|----|-----------|----------|
| MT-E01 | Quantity exceeds MaxQuantityPerPurchase (BR-010) | Error: validation failure |
| MT-E02 | Total tickets exceed event capacity (BR-013) | Error: at capacity |
| MT-E03 | Assignee already has ticket for overlapping session (BR-012) | Error: conflict |
| MT-E04 | Assignee not authorized (BR-020) | Error: 403 |
| MT-E05 | Assignee not vetted for VettedMembersOnly (BR-035) | Error: 403 |
| MT-E06 | Payment failure rolls back ALL tickets | All pending records cleaned up |
| MT-E07 | TicketSelections null falls back to TicketTypeIds | Backward compat |
| MT-E08 | Assignees list longer than Quantity - 1 | Error: validation |

---

## 6. Unit Tests: Admin Assignment

### Happy Path
| ID | Test Case | Expected |
|----|-----------|----------|
| AA-U01 | Admin assigns comp ticket to user | TicketPurchase (TotalPrice=0, admin-comp), PendingAcceptance |
| AA-U02 | GetEventAssignments returns all assignments | All statuses included |

### Edge Cases
| ID | Test Case | Expected |
|----|-----------|----------|
| AA-E01 | Admin assigns to non-vetted user on VettedMembersOnly | Error: not vetted |
| AA-E02 | Admin assigns to user who already has ticket (BR-012) | Error: 409 |
| AA-E03 | Admin assigns with invalid ticket type for event | Error: 404 |
| AA-E04 | Non-admin attempts admin assign | Error: 403 (role check) |

---

## 7. Integration Tests: API Endpoints

Test each endpoint with real HTTP calls against the test API.

### Authorized Contacts (5 endpoints)
| ID | Test Case | Endpoint |
|----|-----------|----------|
| AC-I01 | GET returns empty lists for new user | GET /api/authorized-contacts |
| AC-I02 | POST + GET roundtrip creates and lists contact | POST then GET |
| AC-I03 | DELETE soft-deletes, no longer in GET | DELETE then GET |
| AC-I04 | Search returns matching scene names | GET /api/authorized-contacts/search?q= |
| AC-I05 | Principals filtered by event vetting | GET /api/authorized-contacts/principals?eventId= |
| AC-I06 | POST without CSRF token fails | 400 CSRF error |
| AC-I07 | All endpoints require auth | 401 without token |

### Ticket Assignment (6 endpoints)
| ID | Test Case | Endpoint |
|----|-----------|----------|
| TA-I01 | Full assign->accept flow | POST assign, POST accept |
| TA-I02 | Full assign->decline->reassign flow | POST assign, POST decline, POST reassign |
| TA-I03 | Pending assignments appear in dashboard | GET /api/user/pending-assignments |
| TA-I04 | Assigned tickets appear in purchaser view | GET /api/user/assigned-tickets |
| TA-I05 | Unauthorized assign returns 403 | POST assign without authorized contact |
| TA-I06 | CSRF required on all mutation endpoints | POST without CSRF token |

### Proxy RSVP (3 endpoints)
| ID | Test Case | Endpoint |
|----|-----------|----------|
| PR-I01 | Full proxy RSVP create->accept flow | POST proxy-rsvp, POST accept |
| PR-I02 | Proxy RSVP create->decline flow | POST proxy-rsvp, POST decline |
| PR-I03 | Capacity enforcement | POST proxy-rsvp at capacity |
| PR-I04 | Vetting enforcement | POST proxy-rsvp for non-vetted on VettedMembersOnly |

### Admin (2 endpoints)
| ID | Test Case | Endpoint |
|----|-----------|----------|
| AA-I01 | Admin assigns comp ticket | POST /api/admin/events/{id}/assign-ticket |
| AA-I02 | Admin views assignments | GET /api/admin/events/{id}/assignments |
| AA-I03 | Non-admin gets 403 | POST assign-ticket as regular user |

### Modified Checkout (2 endpoints)
| ID | Test Case | Endpoint |
|----|-----------|----------|
| CO-I01 | Single ticket checkout unchanged | POST /api/checkout/credit-card (existing flow) |
| CO-I02 | Multi-ticket with TicketSelections | POST /api/checkout/credit-card with quantity > 1 |

---

## 8. E2E Tests: User Flows

### Flow 1: Authorized Contacts Management
| Step | Action | Verify |
|------|--------|--------|
| 1 | User A logs in, goes to Profile Settings | Authorized Contacts tab visible |
| 2 | Clicks "Add Contact", searches for User B | Scene names appear in dropdown |
| 3 | Selects User B | Contact appears in "People who can act on your behalf" |
| 4 | User B logs in, checks profile | User A appears in "People you can act for" |
| 5 | User A removes User B | Contact removed from list |

### Flow 2: Multi-Ticket Purchase with Assignment
| Step | Action | Verify |
|------|--------|--------|
| 1 | User B authorizes User A (setup) | Authorization created |
| 2 | User A goes to event, starts ticket checkout | Quantity selector visible |
| 3 | Selects quantity 2 | "Assign to" dropdown appears for ticket 2 |
| 4 | Assigns ticket 2 to User B | User B shown in dropdown |
| 5 | Completes payment | Confirmation shows 2 tickets, User B's is "Pending Acceptance" |
| 6 | User B logs in, sees pending ticket on dashboard | Pending ticket card visible |
| 7 | User B accepts with waiver | Ticket becomes Active, appears in their events |

### Flow 3: Proxy RSVP
| Step | Action | Verify |
|------|--------|--------|
| 1 | User B authorizes User A (setup) | Authorization created |
| 2 | User A goes to free event | "RSVP for someone else" section visible |
| 3 | Selects User B, creates proxy RSVP | Confirmation shown |
| 4 | User B logs in, sees pending RSVP | Pending RSVP card visible |
| 5 | User B accepts with waiver | RSVP becomes Active |

### Flow 4: Decline and Reassign
| Step | Action | Verify |
|------|--------|--------|
| 1 | Setup: A buys ticket, assigns to B | B has pending ticket |
| 2 | B declines the ticket | Ticket returns to A's dashboard |
| 3 | A sees "Declined" status, clicks "Reassign" | Dropdown of other contacts shown |
| 4 | A assigns to C (who also authorized A) | C has pending ticket |

---

## Coverage Requirements

- **Service methods**: 100% of public methods have at least 1 happy path + 1 error case test
- **Business rules**: Every BR-xxx and AD-xxx referenced in implementation has at least 1 test
- **Endpoints**: Every endpoint has at least 1 success + 1 auth failure + 1 business rule failure test
- **Backward compatibility**: Existing single-ticket checkout flow verified unchanged

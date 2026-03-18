# API Design: Ticket Assignment & Proxy RSVP

**Date**: 2026-03-18
**Author**: API Designer Agent (Plan)
**Status**: Design Complete - Pending Review
**Feature**: Ticket Assignment & Proxy RSVP

**References**:
- [Architectural Decisions](../requirements/architectural-decisions.md) - 14 confirmed decisions
- [Business Rules](../requirements/business-rules.md) - 63 business rules + edge cases
- [Use Cases](../requirements/use-cases.md) - 11 use cases
- [Database Design](./database-design.md) - Entity schema
- [Codebase Analysis](../research/codebase-analysis.md) - current system gaps

---

## 1. DTO Definitions

### 1A. Authorized Contacts DTOs

```csharp
// Response DTO for a single authorized contact relationship
public class AuthorizedContactDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }        // The other party's ID
    public string SceneName { get; set; }    // The other party's scene name
    public string Direction { get; set; }    // "delegate" or "principal"
    public DateTime CreatedAt { get; set; }
}

// Response for the list endpoint
public class AuthorizedContactsListDto
{
    // People who can act on my behalf (I authorized them)
    public List<AuthorizedContactDto> Delegates { get; set; } = new();
    // People I can act for (they authorized me)
    public List<AuthorizedContactDto> Principals { get; set; } = new();
}

// Request to add a new authorized contact
public class AddAuthorizedContactRequest
{
    [Required]
    public Guid DelegateUserId { get; set; }  // The person I'm authorizing
}

// Search result for user lookup
public class UserSearchResultDto
{
    public Guid UserId { get; set; }
    public string SceneName { get; set; } = string.Empty;
}

// Simple list for dropdown usage (people I can act for)
public class PrincipalContactDto
{
    public Guid UserId { get; set; }
    public string SceneName { get; set; } = string.Empty;
    public bool IsVetted { get; set; }
}
```

### 1B. Checkout DTOs (Modified)

The existing `CheckoutRequest` is extended with backward-compatible optional fields. If `TicketSelections` is present, it takes precedence over `TicketTypeIds`.

```csharp
public class TicketSelectionItem
{
    [Required]
    public Guid TicketTypeId { get; set; }

    [Required]
    [Range(1, 10)]
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// Optional per-ticket assignments. Length must be <= Quantity - 1.
    /// First ticket is always for the purchaser. Remaining can be assigned.
    /// Null/empty = all unassigned (purchaser owns all).
    /// </summary>
    public List<Guid?>? Assignees { get; set; }
}

// Added to existing CheckoutRequest:
// public List<TicketSelectionItem>? TicketSelections { get; set; }

// Added to existing CheckoutResponse:
// public List<TicketAssignmentResultDto>? Assignments { get; set; }

public class TicketAssignmentResultDto
{
    public Guid AttendanceId { get; set; }
    public Guid TicketPurchaseId { get; set; }
    public string TicketTypeName { get; set; } = string.Empty;
    public Guid? AssignedToUserId { get; set; }
    public string? AssignedToSceneName { get; set; }
    public string Status { get; set; } = string.Empty; // "Active" or "PendingAcceptance"
}
```

### 1C. Ticket Assignment DTOs

```csharp
// Assign a ticket to an authorized contact
public class AssignTicketRequest
{
    [Required]
    public Guid AssignToUserId { get; set; }
}

// Accept an assigned ticket or proxy RSVP
public class AcceptAssignmentRequest
{
    [Required]
    public bool EventWaiverAccepted { get; set; }

    public bool TermsOfServiceAccepted { get; set; }
}

// Response for assignment operations
public class TicketAssignmentDto
{
    public Guid AttendanceId { get; set; }
    public Guid EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public string TicketTypeName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? AssignedToSceneName { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public string? AssignedBySceneName { get; set; }
    public Guid? AssignedByUserId { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### 1D. Proxy RSVP DTOs

```csharp
public class CreateProxyRsvpRequest
{
    [Required]
    public Guid RsvpForUserId { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }
}
```

### 1E. Dashboard DTOs

```csharp
// Pending assignments for the current user's dashboard
public class PendingAssignmentDto
{
    public Guid AttendanceId { get; set; }
    public Guid EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string AssignedBySceneName { get; set; } = string.Empty;
    public string AttendanceType { get; set; } = string.Empty; // "Ticket" or "RSVP"
    public string TicketTypeName { get; set; } = string.Empty;
    public List<string> SessionNames { get; set; } = new();
    public DateTime AssignedAt { get; set; }
}

// Tickets I purchased for others (purchaser's dashboard view)
public class AssignedTicketStatusDto
{
    public Guid AttendanceId { get; set; }
    public Guid TicketPurchaseId { get; set; }
    public Guid EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string TicketTypeName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid? AssignedToUserId { get; set; }
    public string? AssignedToSceneName { get; set; }
    public bool CanReassign { get; set; }
    public bool IsUnassigned { get; set; }
}

// Admin assign ticket request
public class AdminAssignTicketRequest
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid TicketTypeId { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

// Admin view of event assignments
public class AdminEventAssignmentDto
{
    public Guid AttendanceId { get; set; }
    public string AttendeeName { get; set; } = string.Empty;
    public string AssignedByName { get; set; } = string.Empty;
    public string TicketTypeName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? DeclinedAt { get; set; }
}
```

---

## 2. Endpoint Specifications

### 2A. Authorized Contacts Endpoints

**New file:** `apps/api/Features/AuthorizedContacts/Endpoints/AuthorizedContactEndpoints.cs`

---

#### EP-01: GET `/api/authorized-contacts`

| Aspect | Details |
|--------|---------|
| **Auth** | `[Authorize]` (any authenticated user) |
| **CSRF** | Not required (GET) |
| **Request** | None (userId from JWT) |
| **Response** | `AuthorizedContactsListDto` |
| **Success** | 200 OK |
| **Errors** | 401 Unauthorized |
| **Business rules** | Returns all active relationships in both directions. `Delegates` = people I authorized. `Principals` = people who authorized me. |
| **Service method** | NEW: `IAuthorizedContactService.GetContactsAsync(userId)` |

---

#### EP-02: POST `/api/authorized-contacts`

| Aspect | Details |
|--------|---------|
| **Auth** | `[Authorize]` |
| **CSRF** | Required (state-changing) |
| **Request body** | `AddAuthorizedContactRequest { DelegateUserId: Guid }` |
| **Response** | `AuthorizedContactDto` |
| **Success** | 201 Created |
| **Errors** | 400 (self-authorization, BR-003), 404 (user not found), 409 (already authorized) |
| **Business rules** | BR-001: caller is Principal, `DelegateUserId` is the Delegate. BR-003: cannot authorize self. BR-006: both must have registered accounts. |
| **Service method** | NEW: `IAuthorizedContactService.AddContactAsync(principalId, delegateUserId)` |

---

#### EP-03: DELETE `/api/authorized-contacts/{contactId:guid}`

| Aspect | Details |
|--------|---------|
| **Auth** | `[Authorize]` |
| **CSRF** | Required |
| **Request** | `contactId` from route |
| **Response** | None |
| **Success** | 204 No Content |
| **Errors** | 404 (not found), 403 (not the principal of this relationship) |
| **Business rules** | BR-005: revocation does not affect existing tickets. Soft-delete (`RevokedAt` set). Only the Principal can revoke. |
| **Service method** | NEW: `IAuthorizedContactService.RevokeContactAsync(principalId, contactId)` |

---

#### EP-04: GET `/api/authorized-contacts/search?q={sceneName}`

| Aspect | Details |
|--------|---------|
| **Auth** | `[Authorize]` |
| **CSRF** | Not required (GET) |
| **Request** | Query param `q` (min 2 chars) |
| **Response** | `List<UserSearchResultDto>` (max 10 results) |
| **Success** | 200 OK |
| **Errors** | 400 (query too short), 401 |
| **Business rules** | AD-009: scene name only. Excludes self. Excludes already-authorized contacts. `ILIKE` search on SceneName. |
| **Service method** | NEW: `IAuthorizedContactService.SearchUsersAsync(currentUserId, query)` |

---

#### EP-05: GET `/api/authorized-contacts/principals`

| Aspect | Details |
|--------|---------|
| **Auth** | `[Authorize]` |
| **CSRF** | Not required (GET) |
| **Query params** | `eventId` (optional Guid) - filters by vetting for VettedMembersOnly events and excludes principals who already have tickets/RSVPs |
| **Response** | `List<PrincipalContactDto>` |
| **Success** | 200 OK |
| **Business rules** | Returns people who authorized current user (current user is Delegate). Used in checkout/RSVP dropdowns. When `eventId` supplied: BR-035 (vetting filter), BR-012 (exclude with existing attendance). |
| **Service method** | NEW: `IAuthorizedContactService.GetPrincipalsAsync(delegateId, eventId?)` |

---

### 2B. Modified Checkout Endpoints

**Strategy:** Extend existing DTOs with optional backward-compatible fields rather than new endpoint versions. If `TicketSelections` is present, it takes precedence over `TicketTypeIds`.

---

#### EP-06: POST `/api/checkout/credit-card` (MODIFIED)

| Aspect | Details |
|--------|---------|
| **Auth** | `[Authorize]` |
| **CSRF** | Required (existing) |
| **New request field** | `TicketSelections: List<TicketSelectionItem>?` (optional, backward compat) |
| **New response field** | `Assignments: List<TicketAssignmentResultDto>?` |
| **New validation** | Per-ticket-type quantity vs `MaxQuantityPerPurchase` (BR-010). Total capacity across all tickets (BR-013). Per-assignee duplicate check (BR-012). Assignee authorization check (BR-020). Vetting check for assignees on VettedMembersOnly events (BR-035). |
| **Business logic** | Stage 2 creates multiple `TicketPurchase` records. `EventAttendance`: Active for purchaser, PendingAcceptance for assigned. Sliding scale uniform (AD-012). Sends assignment notification emails. |

---

#### EP-07: POST `/api/checkout/paypal/create-order` (MODIFIED)

Same modification pattern as credit card. `PayPalCheckoutCreateOrderRequest` gets optional `TicketSelections`. `CaptureOrder` and `CancelOrder` need no change.

---

### 2C. Ticket Assignment Endpoints

**New file:** `apps/api/Features/Participation/Endpoints/TicketAssignmentEndpoints.cs`

---

#### EP-08: POST `/api/events/{eventId:guid}/tickets/{attendanceId:guid}/assign`

| Aspect | Details |
|--------|---------|
| **Auth** | `[Authorize]` |
| **CSRF** | Required |
| **Request body** | `AssignTicketRequest { AssignToUserId: Guid }` |
| **Response** | `TicketAssignmentDto` |
| **Success** | 200 OK |
| **Errors** | 400 (not unassigned, self-assignment), 403 (doesn't own ticket, assignee hasn't authorized caller per BR-020), 404 (not found), 409 (assignee already has ticket per BR-012) |
| **Business rules** | BR-020 (authorization required). BR-012 (one per person per event/session). BR-035 (vetting check). AD-007 (post-purchase assignment). Changes `UserId` to assignee, status to PendingAcceptance, clears waiver fields. Creates AttendanceHistory. Sends email. |
| **Service method** | NEW: `ITicketAssignmentService.AssignTicketAsync(attendanceId, callerUserId, assignToUserId)` |

---

#### EP-09: POST `/api/events/{eventId:guid}/tickets/{attendanceId:guid}/accept`

| Aspect | Details |
|--------|---------|
| **Auth** | `[Authorize]` |
| **CSRF** | Required |
| **Request body** | `AcceptAssignmentRequest { EventWaiverAccepted: bool, TermsOfServiceAccepted: bool }` |
| **Response** | `TicketAssignmentDto` |
| **Success** | 200 OK |
| **Errors** | 400 (waiver not accepted, event passed), 403 (not the assigned user, vetting revoked per BR-036), 404 (not found or not PendingAcceptance) |
| **Business rules** | AD-003 (waiver personally accepted). BR-031 (ToS). BR-032 (waiver activates). BR-036 (re-check vetting). Transitions PendingAcceptance -> Active. Sets waiver fields. Updates ToS on ApplicationUser if needed. Auto-creates RSVP for social events. Creates EventAttendee for check-in. Creates AttendanceHistory. |
| **Service method** | NEW: `ITicketAssignmentService.AcceptAssignmentAsync(attendanceId, callerUserId, request)` |

---

#### EP-10: POST `/api/events/{eventId:guid}/tickets/{attendanceId:guid}/decline`

| Aspect | Details |
|--------|---------|
| **Auth** | `[Authorize]` |
| **CSRF** | Required |
| **Request body** | Optional `{ reason?: string }` |
| **Response** | `TicketAssignmentDto` |
| **Success** | 200 OK |
| **Errors** | 403 (not assigned user), 404 (not PendingAcceptance) |
| **Business rules** | BR-025 (declined can be reassigned). Sets `DeclinedAt` on EventAttendance. Changes `UserId` back to original purchaser (from `TicketPurchase.UserId`). Sets status to Active for purchaser. Creates AttendanceHistory. Notifies purchaser. |
| **Design note** | On decline, the ticket reverts to the purchaser as Active (they own it again). The `DeclinedAt` field tracks the decline for display ("Declined by [SceneName]"). The purchaser can then reassign via EP-11. |
| **Service method** | NEW: `ITicketAssignmentService.DeclineAssignmentAsync(attendanceId, callerUserId, reason?)` |

---

#### EP-11: POST `/api/events/{eventId:guid}/tickets/{attendanceId:guid}/reassign`

| Aspect | Details |
|--------|---------|
| **Auth** | `[Authorize]` |
| **CSRF** | Required |
| **Request body** | `AssignTicketRequest { AssignToUserId: Guid }` |
| **Response** | `TicketAssignmentDto` |
| **Success** | 200 OK |
| **Errors** | 400 (ticket not eligible for reassignment - must be Active with `DeclinedAt` set, meaning it was returned to purchaser after decline), 403 (not original purchaser), 404, 409 (new assignee already has ticket) |
| **Business rules** | UC-008. Works on tickets that reverted to purchaser after decline (`Status=Active` AND `DeclinedAt IS NOT NULL`). Re-checks vetting (BR-035). Clears `DeclinedAt`, sets new assignment fields. Creates AttendanceHistory with 'TicketReassigned'. Sends notification email. |
| **Service method** | NEW: `ITicketAssignmentService.ReassignTicketAsync(attendanceId, callerUserId, newAssigneeUserId)` |

---

### 2D. Proxy RSVP Endpoints

**New file:** `apps/api/Features/Participation/Endpoints/ProxyRsvpEndpoints.cs`

---

#### EP-12: POST `/api/events/{eventId:guid}/proxy-rsvp`

| Aspect | Details |
|--------|---------|
| **Auth** | `[Authorize]` |
| **CSRF** | Required |
| **Request body** | `CreateProxyRsvpRequest { RsvpForUserId: Guid, Notes?: string }` |
| **Response** | `TicketAssignmentDto` |
| **Success** | 201 Created |
| **Errors** | 400 (event doesn't allow RSVPs, capacity full per BR-054), 403 (not authorized per BR-050), 404 (event not found), 409 (target already has RSVP) |
| **Business rules** | BR-050 (same auth). BR-051 (PendingAcceptance). BR-054 (capacity). BR-035/BR-036 (vetting). Creates EventAttendance (RSVP, PendingAcceptance, UserId=Principal, AssignedByUserId=Delegate). Creates AttendanceHistory 'ProxyRSVPCreated'. Sends RSVP notification email. |
| **Service method** | NEW: `IProxyRsvpService.CreateProxyRsvpAsync(eventId, delegateUserId, principalUserId, notes?)` |

---

#### EP-13: POST `/api/events/{eventId:guid}/rsvp/{attendanceId:guid}/accept`

| Aspect | Details |
|--------|---------|
| **Auth** | `[Authorize]` |
| **CSRF** | Required |
| **Request body** | `AcceptAssignmentRequest` |
| **Response** | `TicketAssignmentDto` |
| **Success** | 200 OK |
| **Business rules** | Same PendingAcceptance -> Active flow as ticket acceptance. Reuses accept logic since both are EventAttendance records. |
| **Service method** | Reuse: `ITicketAssignmentService.AcceptAssignmentAsync(attendanceId, callerUserId, request)` |

---

#### EP-14: POST `/api/events/{eventId:guid}/rsvp/{attendanceId:guid}/decline`

| Aspect | Details |
|--------|---------|
| **Auth** | `[Authorize]` |
| **CSRF** | Required |
| **Response** | 200 OK |
| **Business rules** | Unlike tickets, declined RSVPs are simply cancelled (no reassignment needed - delegate can create a new proxy RSVP). |
| **Service method** | Reuse decline logic or cancel the RSVP directly. |

---

### 2E. Modified Participation Status Endpoint

#### EP-15: GET `/api/events/{eventId:guid}/participation` (MODIFIED)

Add to existing `EnhancedParticipationStatusDto`:

```csharp
public List<PendingAssignmentSummaryDto> PendingAssignments { get; set; } = new();
public bool HasPendingAssignment { get; set; }
```

```csharp
public class PendingAssignmentSummaryDto
{
    public Guid AttendanceId { get; set; }
    public string AttendanceType { get; set; } = string.Empty;
    public string AssignedBySceneName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
}
```

**Change to `GetParticipationStatusAsync`:** Also query PendingAcceptance records where `UserId = currentUser`.

---

### 2F. Dashboard Endpoints

**New file:** `apps/api/Features/Participation/Endpoints/DashboardAssignmentEndpoints.cs`

---

#### EP-16: GET `/api/user/pending-assignments`

| Aspect | Details |
|--------|---------|
| **Auth** | `[Authorize]` |
| **Response** | `List<PendingAssignmentDto>` |
| **Business rules** | All EventAttendance where `UserId = currentUser` AND `Status = PendingAcceptance`. Both Ticket and RSVP types. Joined to Event for title/date, to AssignedByUser for scene name. Ordered by event date ascending. |
| **Service method** | NEW: `ITicketAssignmentService.GetPendingAssignmentsAsync(userId)` |

---

#### EP-17: GET `/api/user/assigned-tickets`

| Aspect | Details |
|--------|---------|
| **Auth** | `[Authorize]` |
| **Response** | `List<AssignedTicketStatusDto>` |
| **Business rules** | Tickets the current user purchased that are assigned to others or unassigned extras. Shows assignment status and whether each can be reassigned. |
| **Service method** | NEW: `ITicketAssignmentService.GetAssignedTicketsAsync(userId)` |

---

### 2G. Admin Endpoints

---

#### EP-18: POST `/api/admin/events/{eventId:guid}/assign-ticket`

| Aspect | Details |
|--------|---------|
| **Auth** | `[Authorize]` + `RequireRole(Administrator)` |
| **CSRF** | Required |
| **Request body** | `AdminAssignTicketRequest { UserId, TicketTypeId, Notes? }` |
| **Response** | `TicketAssignmentDto` |
| **Success** | 201 Created |
| **Errors** | 400 (invalid ticket type), 404 (event/user/ticket not found), 409 (user already has ticket) |
| **Business rules** | BR-040 (bypasses authorized contacts). BR-041 (recipient must still accept waiver). BR-042 (admin audit trail). Creates "comp" TicketPurchase (TotalPrice=0, PaymentMethod="admin-comp"). Creates EventAttendance PendingAcceptance. Sends notification email. |
| **Service method** | NEW: `ITicketAssignmentService.AdminAssignTicketAsync(eventId, adminUserId, request)` |

---

#### EP-19: GET `/api/admin/events/{eventId:guid}/assignments`

| Aspect | Details |
|--------|---------|
| **Auth** | `[Authorize]` + `RequireRole(Administrator)` |
| **Response** | `List<AdminEventAssignmentDto>` |
| **Business rules** | All EventAttendance where `AssignedByUserId IS NOT NULL`. All statuses (PendingAcceptance, Active, Declined). Full audit info. |
| **Service method** | NEW: `ITicketAssignmentService.GetEventAssignmentsAsync(eventId)` |

---

## 3. New Service Layer

### 3A. IAuthorizedContactService (NEW)

**File:** `apps/api/Features/AuthorizedContacts/Services/IAuthorizedContactService.cs`

```csharp
public interface IAuthorizedContactService
{
    Task<AuthorizedContactsListDto> GetContactsAsync(Guid userId, CancellationToken ct);
    Task<AuthorizedContactDto> AddContactAsync(Guid principalId, Guid delegateUserId, CancellationToken ct);
    Task<Result> RevokeContactAsync(Guid principalId, Guid contactId, CancellationToken ct);
    Task<List<UserSearchResultDto>> SearchUsersAsync(Guid currentUserId, string query, CancellationToken ct);
    Task<List<PrincipalContactDto>> GetPrincipalsAsync(Guid delegateId, Guid? eventId, CancellationToken ct);
    Task<bool> IsAuthorizedDelegateAsync(Guid principalId, Guid delegateId, CancellationToken ct);
}
```

### 3B. ITicketAssignmentService (NEW)

**File:** `apps/api/Features/Participation/Services/ITicketAssignmentService.cs`

```csharp
public interface ITicketAssignmentService
{
    Task<TicketAssignmentDto> AssignTicketAsync(Guid attendanceId, Guid callerUserId, Guid assignToUserId, CancellationToken ct);
    Task<TicketAssignmentDto> AcceptAssignmentAsync(Guid attendanceId, Guid callerUserId, AcceptAssignmentRequest request, CancellationToken ct);
    Task<TicketAssignmentDto> DeclineAssignmentAsync(Guid attendanceId, Guid callerUserId, string? reason, CancellationToken ct);
    Task<TicketAssignmentDto> ReassignTicketAsync(Guid attendanceId, Guid callerUserId, Guid newAssigneeUserId, CancellationToken ct);
    Task<List<PendingAssignmentDto>> GetPendingAssignmentsAsync(Guid userId, CancellationToken ct);
    Task<List<AssignedTicketStatusDto>> GetAssignedTicketsAsync(Guid userId, CancellationToken ct);
    Task<TicketAssignmentDto> AdminAssignTicketAsync(Guid eventId, Guid adminUserId, AdminAssignTicketRequest request, CancellationToken ct);
    Task<List<AdminEventAssignmentDto>> GetEventAssignmentsAsync(Guid eventId, CancellationToken ct);
}
```

### 3C. IProxyRsvpService (NEW)

**File:** `apps/api/Features/Participation/Services/IProxyRsvpService.cs`

```csharp
public interface IProxyRsvpService
{
    Task<TicketAssignmentDto> CreateProxyRsvpAsync(Guid eventId, Guid delegateUserId, Guid principalUserId, string? notes, CancellationToken ct);
    // Accept/decline reuse ITicketAssignmentService since both are EventAttendance records
}
```

---

## 4. Impact Analysis on Existing Code

### 4A. CheckoutEndpoints.cs (Credit Card)

**File:** `apps/api/Features/Payments/Endpoints/CheckoutEndpoints.cs`

1. Add optional `TicketSelections` field to `CheckoutRequest`
2. If `TicketSelections` present, use it instead of `TicketTypeIds`
3. After Stage 4 (finalize), send assignment notification emails
4. Return `Assignments` in response
5. **Backward compat:** If `TicketSelections` is null/empty, fall back to current `TicketTypeIds` behavior

### 4B. PayPalCheckoutController.cs

Same changes as credit card. `CaptureOrder` and `CancelOrder` need no change.

### 4C. AttendanceService.cs (2011 lines - HIGHEST IMPACT)

1. `CreateTicketPurchaseAsync` needs quantity + assignment support
2. Capacity check must account for total quantity across all ticket selections (BR-013)
3. Assigned ticket EventAttendances get `Status = PendingAcceptance`, `AssignedByUserId`
4. Do NOT accept waiver for assigned tickets at purchase (BR-033)
5. Do NOT auto-create RSVP for assigned tickets (created at acceptance)
6. `ActivateAttendanceForPurchasesAsync` should skip PendingAcceptance records

### 4D. TicketType.Sold Computed Property

Must include `PendingAcceptance` in count (in addition to `Active`). Critical for capacity enforcement.

### 4E. VettingAccessControlService

Need new method or parameter to check assignee vetting for VettedMembersOnly events at both assignment and acceptance time (AD-014).

### 4F. EnhancedParticipationStatusDto

Add `PendingAssignments` and `HasPendingAssignment` fields. `GetParticipationStatusAsync` must query PendingAcceptance records.

### 4G. TicketTypeDto

Add `MaxQuantityPerPurchase` field to DTO and mapping.

### 4H. WebApplicationExtensions.cs

Register new endpoint groups:
```csharp
app.MapAuthorizedContactEndpoints();
app.MapTicketAssignmentEndpoints();
app.MapProxyRsvpEndpoints();
app.MapDashboardAssignmentEndpoints();
```

---

## 5. Suggested Implementation Order

### Phase 1: Foundation (backward compatible, no UI changes needed)
1. Database migration (new entity, new columns, new enum values)
2. `AuthorizedContact` entity + EF configuration
3. `IAuthorizedContactService` + implementation
4. `AuthorizedContactEndpoints` (all 5 endpoints)
5. Add `MaxQuantityPerPurchase` to `TicketType` + `TicketTypeDto`

### Phase 2: Assignment Core (enables the feature)
6. `ITicketAssignmentService` + implementation (assign, accept, decline, reassign)
7. `TicketAssignmentEndpoints` (EP-08 through EP-11)
8. `IProxyRsvpService` + implementation
9. `ProxyRsvpEndpoints` (EP-12 through EP-14)
10. Email templates for assignment + RSVP notifications

### Phase 3: Checkout Modification (highest risk, most complex)
11. Extend `CheckoutRequest` and PayPal request with `TicketSelections`
12. Modify `AttendanceService.CreateTicketPurchaseAsync` for multi-ticket + assignment
13. Update `ActivateAttendanceForPurchasesAsync` to handle PendingAcceptance
14. Update capacity calculations to include PendingAcceptance

### Phase 4: Dashboard and Admin
15. `DashboardAssignmentEndpoints` (EP-16, EP-17)
16. Modify `GetParticipationStatusAsync` to include pending assignments
17. Admin assign ticket endpoint (EP-18)
18. Admin assignments view endpoint (EP-19)

### Phase 5: Background Jobs
19. Reminder email scheduled job (1 day before event)
20. Reminder tracking (`ReminderSentAt` field)

---

## 6. Endpoint Summary Table

| # | Method | Path | Auth | Purpose | Phase |
|---|--------|------|------|---------|-------|
| EP-01 | GET | `/api/authorized-contacts` | User | List all contacts (both directions) | 1 |
| EP-02 | POST | `/api/authorized-contacts` | User+CSRF | Add authorized contact | 1 |
| EP-03 | DELETE | `/api/authorized-contacts/{contactId}` | User+CSRF | Revoke authorization | 1 |
| EP-04 | GET | `/api/authorized-contacts/search?q=` | User | Search users by scene name | 1 |
| EP-05 | GET | `/api/authorized-contacts/principals` | User | Get people I can act for (dropdown) | 1 |
| EP-06 | POST | `/api/checkout/credit-card` | User+CSRF | **Modified** - multi-ticket support | 3 |
| EP-07 | POST | `/api/checkout/paypal/create-order` | User+CSRF | **Modified** - multi-ticket support | 3 |
| EP-08 | POST | `/api/events/{eid}/tickets/{aid}/assign` | User+CSRF | Post-purchase assignment | 2 |
| EP-09 | POST | `/api/events/{eid}/tickets/{aid}/accept` | User+CSRF | Accept assigned ticket | 2 |
| EP-10 | POST | `/api/events/{eid}/tickets/{aid}/decline` | User+CSRF | Decline assigned ticket | 2 |
| EP-11 | POST | `/api/events/{eid}/tickets/{aid}/reassign` | User+CSRF | Reassign declined ticket | 2 |
| EP-12 | POST | `/api/events/{eid}/proxy-rsvp` | User+CSRF | Create proxy RSVP | 2 |
| EP-13 | POST | `/api/events/{eid}/rsvp/{aid}/accept` | User+CSRF | Accept proxy RSVP | 2 |
| EP-14 | POST | `/api/events/{eid}/rsvp/{aid}/decline` | User+CSRF | Decline proxy RSVP | 2 |
| EP-15 | GET | `/api/events/{eid}/participation` | User | **Modified** - include pending | 4 |
| EP-16 | GET | `/api/user/pending-assignments` | User | Dashboard pending items | 4 |
| EP-17 | GET | `/api/user/assigned-tickets` | User | Dashboard assigned tickets | 4 |
| EP-18 | POST | `/api/admin/events/{eid}/assign-ticket` | Admin+CSRF | Admin ticket assignment | 4 |
| EP-19 | GET | `/api/admin/events/{eid}/assignments` | Admin | Admin view assignments | 4 |

**Total: 19 endpoints (5 new groups + 2 modified existing)**

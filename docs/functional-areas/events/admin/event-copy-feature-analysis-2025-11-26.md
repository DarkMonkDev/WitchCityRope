# Event Copy Feature - Comprehensive Analysis & Planning

**Date**: November 26, 2025
**Feature Status**: Partially Implemented (Frontend UI exists, Backend Missing)
**Functional Area**: Events Management → Admin
**Work Type**: Feature Completion

---

## 1. Executive Summary

### Feature Status
- **Frontend**: Copy button exists in admin events table, but fails due to missing backend
- **Backend**: Endpoint `POST /api/events/{id}/copy` NOT IMPLEMENTED
- **Critical Finding**: useCopyEvent mutation references non-existent API endpoint
- **User Experience**: Copy button visible but returns 404 error when clicked

### Critical Findings
1. **BACKEND BLOCKER**: Copy endpoint must be created before frontend modal can function
2. **UI EXISTS**: Copy button already in admin events table
3. **MUTATION EXISTS**: TanStack Query mutation configured but calls missing endpoint
4. **MODAL NEEDED**: User requested modal with date/title inputs (not yet implemented)

### Current User Experience
When an admin clicks the "Copy" button:
1. Frontend calls `POST /api/events/{id}/copy`
2. Backend returns 404 (endpoint doesn't exist)
3. User sees error notification: "Unable to copy event"
4. No copy occurs, workflow broken

---

## 2. User Requirements (Original Request)

### Modal Dialog Specification
- ✅ **Modal Display**: Show modal when "Copy" button clicked
- ✅ **New Event Start Date**: Date input field, required, cannot be in past
- ✅ **New Event Title**: Text input field, pre-filled with original event title
- ✅ **Form Validation**: Date (required, future), Title (required, 3-200 chars)

### Data Copying Requirements

#### COPY These Items:
- ✅ **Venue**: Reference to same venue
- ✅ **Text Content**: Title, Description, ShortDescription, Policies
- ✅ **Event Configuration**: EventType, Capacity, all timing controls (6 granular timing fields)
- ✅ **Sessions**: Deep copy with date offset calculations
- ✅ **Ticket Types**: Deep copy with session ID remapping
- ✅ **Volunteer Positions**: Deep copy with session ID remapping, reset SlotsFilled to 0
- ✅ **Custom Email Templates**: Deep copy with event association remapping (CLARIFIED)

#### EXCLUDE These Items:
- ❌ **RSVPs**: No event attendances copied
- ❌ **Ticket Purchases**: No financial transaction history
- ❌ **Volunteer Sign-ups**: No volunteer assignments copied
- ❌ **Attendee Data**: No check-in records or attendance tracking

### Email Template Handling (CLARIFIED)
**User Requirement**: "Every event CAN create a custom email template by starting with a global email template, making changes to it. The changed custom template is associated with THAT EVENT ONLY. When copying an event with custom email templates, each custom template should be copied and the copy associated with the NEW event being created."

**System Architecture**:
- **Global Templates**: `GlobalEmailTemplate` table - Shared across ALL events as defaults
- **Custom Templates**: `EventEmailTemplate` table - Event-specific overrides
  - One-to-many relationship with Event (FK: EventId)
  - Reference to GlobalTemplateId (tracks which global template was customized)
  - Custom Subject, HtmlBody, PlainTextBody for each event
  - TargetSessions field for multi-session event targeting

**Copy Behavior**:
- **If event has custom email templates**: Deep copy each `EventEmailTemplate` with:
  - New ID (Guid.NewGuid())
  - New EventId (reference copied event)
  - Preserve GlobalTemplateId reference
  - Copy Subject, HtmlBody, PlainTextBody
  - Copy TargetSessions array
  - Reset UpdatedBy and timestamps
- **If event uses only global templates**: No action needed (global templates auto-apply)

---

## 3. Current Implementation Status

### 3.1 Frontend (React/TypeScript)

#### What EXISTS and WORKS:
✅ **Copy Button in Table**
- Location: `/home/chad/repos/witchcityrope/apps/web/src/components/events/EventsTableView.tsx`
- Button styling: Subtle variant, wcr.7 color, 14px font, 600 weight
- Event propagation handled: `event.stopPropagation()` prevents row click
- Test ID: `button-copy-event`

✅ **TanStack Query Mutation**
- Location: `/home/chad/repos/witchcityrope/apps/web/src/features/events/api/mutations.ts`
- Function: `useCopyEvent()`
- Query invalidation on success
- Error handling with console logging

✅ **Admin Events Page Integration**
- Location: `/home/chad/repos/witchcityrope/apps/web/src/pages/admin/AdminEventsPage.tsx`
- Handler: `handleCopyEvent(eventId: string)`
- Success notification: "Event copied successfully"
- Navigation to edit page on success

#### What's IMPLEMENTED but BROKEN:
❌ **API Endpoint Call**
- Calls: `POST /api/events/${eventId}/copy`
- Status: **404 - Endpoint doesn't exist**
- Impact: Copy button fails every time

#### What's MISSING (Frontend):
❌ **Copy Event Modal Component**
- Needed: `/home/chad/repos/witchcityrope/apps/web/src/components/events/CopyEventModal.tsx`
- Must include: Date input, title input, validation, cancel/submit buttons

❌ **Modal Integration**
- AdminEventsPage needs modal state management
- Modal opened/closed state
- Event to copy state
- Success handler navigation

❌ **Mutation Parameter Updates**
- Current: `useCopyEvent()` takes only `eventId`
- Needed: Accept `{ eventId, newDate, newTitle }` parameters

❌ **Route Correction**
- Current navigation: `navigate(\`/admin/events/edit/${copiedEventId}\`)`
- Check if route exists and is correct

### 3.2 Backend (C# API)

#### What's MISSING (Backend - ALL OF IT):
❌ **API Endpoint**
- File: `/home/chad/repos/witchcityrope/apps/api/Features/Events/Endpoints/EventEndpoints.cs`
- Needed: `MapPost("/api/events/{id}/copy", CopyEventAsync)`
- CSRF protection required
- Admin authorization required

❌ **Service Interface Method**
- File: `/home/chad/repos/witchcityrope/apps/api/Features/Events/Services/IEventService.cs`
- Needed: `Task<Result<EventDto>> CopyEventAsync(Guid eventId, string newTitle, DateTime newStartDate, CancellationToken ct)`

❌ **Service Implementation**
- Deep copy logic for Event + related entities
- Transaction management (all-or-nothing)
- ID mapping for Sessions → TicketTypes → VolunteerPositions
- Date offset calculations for sessions
- **Email template copying** (NEW)

❌ **Request DTO**
- File: `/home/chad/repos/witchcityrope/apps/api/Features/Events/Models/CopyEventRequest.cs` (NEW)
- Properties: `string NewTitle`, `DateTime NewStartDate`

❌ **Response DTO**
- Use existing `EventDto` with new event ID

### 3.3 Data Model

#### Event Entity Properties (Copy vs Reset):

**COPY AS-IS**:
- ShortDescription, Description, Policies (text content)
- Capacity (event size)
- EventType (Workshop/Social/Performance)
- VenueId (same venue reference)
- RegistrationOpenHours, RegistrationCloseHours (6 timing fields total)
- CancellationOpenHours, CancellationCloseHours
- VolunteerRegistrationCloseHours, VolunteerCancellationCloseHours

**COPY WITH MODIFICATIONS**:
- Title → Add "(Copy)" suffix or use user input
- StartDate → Use new date from modal
- EndDate → Calculate based on new StartDate

**RESET TO NEW VALUES**:
- Id → Generate new GUID
- IsPublished → Set to FALSE (draft mode)
- CreatedAt → DateTime.UtcNow
- UpdatedAt → DateTime.UtcNow

#### Related Entities Requiring Deep Copy:

**1. Sessions** (Deep Copy)
- File: `/home/chad/repos/witchcityrope/apps/api/Models/Session.cs`
- New IDs: Generate new GUIDs
- EventId: Reference new Event
- Date Adjustment: **CRITICAL** - Offset StartTime/EndTime based on new event date
- Copy: SessionCode, Name, Capacity
- Exclude: CurrentAttendees (computed property)

**2. TicketTypes** (Deep Copy)
- File: `/home/chad/repos/witchcityrope/apps/api/Models/TicketType.cs`
- New IDs: Generate new GUIDs
- EventId: Reference new Event
- SessionId: **Map to new Session IDs** (critical for session-specific tickets)
- Copy: Name, Description, PricingType, Price, MinPrice, MaxPrice, DefaultPrice, Available
- Reset: Sold (computed), Purchases (empty collection)

**3. VolunteerPositions** (Deep Copy)
- File: `/home/chad/repos/witchcityrope/apps/api/Models/VolunteerPosition.cs`
- New IDs: Generate new GUIDs
- EventId: Reference new Event
- SessionId: **Map to new Session IDs** (if session-specific)
- Copy: Title, Description, SlotsNeeded, IsPublicFacing
- Reset: SlotsFilled = 0 (no volunteers in copy)

**4. Organizers** (Reference Copy - Many-to-Many)
- Copy organizer relationship (same teachers)
- Implementation: `copiedEvent.Organizers = originalEvent.Organizers.ToList();`

**5. EventEmailTemplates** (Deep Copy - NEW)
- File: `/home/chad/repos/witchcityrope/apps/api/Features/EmailTemplates/Entities/EventEmailTemplate.cs`
- New IDs: Generate new GUIDs
- EventId: Reference new Event (critical FK update)
- GlobalTemplateId: **Preserve reference** (tracks which global template was customized)
- TemplateType: Copy as-is
- Copy: Subject, HtmlBody, PlainTextBody, TargetSessions array, RecipientGroup
- Reset: CreatedAt, UpdatedAt to DateTime.UtcNow
- Reset: UpdatedBy to current admin user ID

#### Entities to EXCLUDE from Copy:

**1. EventAttendances**
- File: `/home/chad/repos/witchcityrope/apps/api/Features/Participation/Entities/EventAttendance.cs`
- Reason: Attendance records specific to original event
- Impact: New event starts with 0 attendees

**2. TicketPurchases**
- File: `/home/chad/repos/witchcityrope/apps/api/Models/TicketPurchase.cs`
- Reason: Financial transactions tied to original event
- Impact: No purchase history in copy

**3. Computed Properties**
- All `[NotMapped]` properties auto-calculate from data
- Examples: CurrentAttendees, Sold, Remaining, IsSoldOut

---

## 4. Gap Analysis

### 4.1 Backend Gaps (BLOCKING - Must Build First)

#### Gap 1: API Endpoint Missing
**Impact**: CRITICAL - Copy functionality completely broken
**Location**: `/home/chad/repos/witchcityrope/apps/api/Features/Events/Endpoints/EventEndpoints.cs`
**Required**:
```csharp
group.MapPost("/{id}/copy", async (
    Guid id,
    CopyEventRequest request,
    IEventService eventService,
    CancellationToken ct) =>
{
    var result = await eventService.CopyEventAsync(id, request.NewTitle, request.NewStartDate, ct);
    return result.IsSuccess
        ? Results.Ok(result.Value)
        : Results.BadRequest(result.Errors);
})
.RequireAuthorization("Admin")
.ValidateAntiforgeryToken();
```

#### Gap 2: Service Interface Method Missing
**Impact**: HIGH - No contract for copy operation
**Location**: `/home/chad/repos/witchcityrope/apps/api/Features/Events/Services/IEventService.cs`
**Required**:
```csharp
Task<Result<EventDto>> CopyEventAsync(
    Guid originalEventId,
    string newTitle,
    DateTime newStartDate,
    CancellationToken cancellationToken = default);
```

#### Gap 3: Service Implementation Missing
**Impact**: CRITICAL - Core business logic missing
**Complexity**: HIGH - Deep copy with ID remapping and transactions
**Required Operations**:
1. Load original event with all related entities (Include chains)
2. Create new Event with reset properties
3. Copy Sessions with new IDs, track old ID → new ID mapping
4. Copy TicketTypes with SessionId remapping
5. Copy VolunteerPositions with SessionId remapping
6. Copy Organizers (many-to-many relationship)
7. **Copy EventEmailTemplates with new EventId** (NEW)
8. Save all in transaction (rollback on failure)
9. Return new EventDto

#### Gap 4: Request DTO Missing
**Impact**: MEDIUM - Input validation and serialization
**Location**: `/home/chad/repos/witchcityrope/apps/api/Features/Events/Models/CopyEventRequest.cs` (NEW FILE)
**Required**:
```csharp
public record CopyEventRequest
{
    public required string NewTitle { get; init; }
    public required DateTime NewStartDate { get; init; }
}
```

#### Gap 5: Type Generation Required
**Impact**: MEDIUM - Frontend types out of sync
**Action**: After backend complete, run `cd packages/shared-types && npm run generate`

### 4.2 Frontend Gaps (Can Build After Backend)

#### Gap 1: Modal Component Missing
**Impact**: HIGH - No UI for date/title input
**Location**: `/home/chad/repos/witchcityrope/apps/web/src/components/events/CopyEventModal.tsx` (NEW FILE)
**Required Components**:
- Modal from `@mantine/core`
- DateInput from `@mantine/dates` (minDate = today)
- TextInput for title (pre-filled with original + " (Copy)")
- Form validation with `@mantine/form`
- Cancel + Copy Event buttons
- Loading states during API call
- Success/error notifications

#### Gap 2: Modal Integration Missing
**Impact**: MEDIUM - Modal not connected to page
**Location**: `/home/chad/repos/witchcityrope/apps/web/src/pages/admin/AdminEventsPage.tsx`
**Required**:
- State: `copyModalOpened`, `eventToCopy`
- Handler: `handleCopyButtonClick(event)`
- JSX: `<CopyEventModal ... />`

#### Gap 3: Mutation Parameters Wrong
**Impact**: MEDIUM - Mutation can't pass date/title
**Location**: `/home/chad/repos/witchcityrope/apps/web/src/features/events/api/mutations.ts`
**Current**: `mutationFn: async (eventId: string)`
**Needed**: `mutationFn: async ({ eventId, newDate, newTitle })`

#### Gap 4: Route Verification Needed
**Impact**: LOW - May have wrong edit route
**Check**: Does `/admin/events/${id}` exist and support editing?

---

## 5. Functional Requirements

### 5.1 Backend Requirements

#### FR-B1: Copy Event with New Date/Title
- **Input**: Original event ID, new title, new start date
- **Output**: New event DTO with unique ID
- **Validation**:
  - Original event exists
  - User is Admin
  - New start date is in future
  - New title is 3-200 characters

#### FR-B2: Deep Copy Sessions
- **Input**: Original event sessions
- **Output**: New sessions with adjusted dates
- **Logic**:
  - Calculate date offset: `newStartDate - originalStartDate`
  - Apply offset to all session StartTime/EndTime
  - Generate new Session IDs
  - Track ID mappings for relationships

#### FR-B3: Deep Copy Ticket Types
- **Input**: Original ticket types
- **Output**: New ticket types with remapped SessionIds
- **Logic**:
  - Generate new TicketType IDs
  - Map SessionId using Session ID mapping dictionary
  - Reset Sold count to 0
  - Copy pricing and availability settings

#### FR-B4: Deep Copy Volunteer Positions
- **Input**: Original volunteer positions
- **Output**: New positions with remapped SessionIds
- **Logic**:
  - Generate new VolunteerPosition IDs
  - Map SessionId using Session ID mapping dictionary
  - Reset SlotsFilled to 0
  - Copy title, description, slots needed

#### FR-B5: Copy Organizer Relationships
- **Input**: Original event organizers
- **Output**: Same organizers linked to new event
- **Logic**: Copy many-to-many relationship (reference same users)

#### FR-B6: Copy Venue Reference
- **Input**: Original VenueId
- **Output**: Same VenueId in copied event
- **Logic**: Reference copy (same venue)

#### FR-B7: Copy Custom Email Templates (NEW)
- **Input**: Original event's custom email templates (if any)
- **Output**: New custom templates linked to copied event
- **Logic**:
  - Load original event with `.Include(e => e.EventEmailTemplates)` or similar
  - For each EventEmailTemplate associated with source event:
    - Create new EventEmailTemplate entity with new ID
    - Set EventId to new event's ID
    - Preserve GlobalTemplateId reference
    - Copy Subject, HtmlBody, PlainTextBody
    - Copy TargetSessions array
    - Copy TemplateType
    - Reset CreatedAt, UpdatedAt timestamps
    - Set UpdatedBy to current admin user
  - Include in transaction

#### FR-B8: Exclude Attendance/Transaction Data
- **Input**: N/A
- **Output**: New event with 0 attendees, 0 purchases
- **Logic**: Do NOT copy EventAttendances or TicketPurchases

#### FR-B9: Create as Draft
- **Input**: N/A
- **Output**: IsPublished = false
- **Logic**: All copies start as unpublished drafts

#### FR-B10: Return New Event DTO
- **Input**: Saved event entity
- **Output**: EventDto with all properties
- **Logic**: Map entity to DTO, return to frontend

### 5.2 Frontend Requirements

#### FR-F1: Modal with Date/Title Inputs
- **Display**: Modal opens when "Copy" button clicked
- **Inputs**:
  - DateInput (required, minDate = today, label "New Event Date")
  - TextInput (required, 3-200 chars, pre-filled with original title)
- **Buttons**: Cancel (close modal), Copy Event (submit with loading)

#### FR-F2: Date Validation
- **Rule**: Date cannot be in the past
- **Error**: "Event date must be in the future"
- **Implementation**: `minDate={new Date()}` on DateInput

#### FR-F3: Title Validation
- **Rules**:
  - Required: "Title is required"
  - Min 3 chars: "Title must be at least 3 characters"
  - Max 200 chars: "Title cannot exceed 200 characters"
- **Pre-fill**: `event.title + " (Copy)"`

#### FR-F4: Submit Mutation
- **Action**: Call `useCopyEvent({ eventId, newDate, newTitle })`
- **Loading**: Show loading spinner on button
- **Disable**: Disable cancel button while submitting

#### FR-F5: Success Notification and Navigation
- **Notification**: "Event copied successfully. Redirecting to edit page." (green)
- **Navigation**: `navigate(\`/admin/events/${copiedEventId}\`)`
- **Modal**: Close modal on success

#### FR-F6: Error Handling
- **Notification**: Display error message (red)
- **Message**: API error message or "Unable to copy event. Please try again."
- **Modal**: Keep modal open on error

---

## 6. Implementation Approach

### 6.1 Phase 1: Backend Implementation (REQUIRED FIRST)

**Duration**: 4-6 hours
**Blocking**: Frontend modal cannot function until this is complete

#### Step 1.1: Create Request DTO (30 min)
- Create `CopyEventRequest.cs` with NewTitle and NewStartDate
- Add validation attributes (`[Required]`, `[MaxLength(200)]`)

#### Step 1.2: Add Service Interface Method (15 min)
- Update `IEventService.cs` with CopyEventAsync signature
- Document return type and parameters

#### Step 1.3: Implement Service Logic (3-4 hours)
- Load original event with Include chains (INCLUDING EventEmailTemplates)
- Create new Event with reset properties
- Copy Sessions with date offset, track ID mappings
- Copy TicketTypes with SessionId remapping
- Copy VolunteerPositions with SessionId remapping
- Copy Organizers (many-to-many)
- **Copy EventEmailTemplates with new EventId** (NEW)
- Wrap in transaction
- Map to EventDto and return

#### Step 1.4: Add API Endpoint (30 min)
- Add MapPost in EventEndpoints.cs
- Add CSRF protection (ValidateAntiforgeryToken)
- Add Admin authorization
- Wire up request/service/response

#### Step 1.5: Regenerate TypeScript Types (5 min)
- Run `cd packages/shared-types && npm run generate`
- Verify EventDto and CopyEventRequest types generated

### 6.2 Phase 2: Frontend Modal Implementation

**Duration**: 2-3 hours
**Dependencies**: Backend must be complete and deployed

#### Step 2.1: Create Modal Component (1.5 hours)
- Create CopyEventModal.tsx
- Add DateInput and TextInput with validation
- Implement form submission with useCopyEvent mutation
- Add success/error notifications
- Add cancel/submit buttons with loading states

#### Step 2.2: Integrate with Admin Page (30 min)
- Add modal state to AdminEventsPage
- Update handleCopyEvent to open modal
- Pass event data to modal
- Wire up success handler for navigation

#### Step 2.3: Update Mutation Hook (30 min)
- Modify useCopyEvent to accept `{ eventId, newDate, newTitle }`
- Update mutationFn to send request body
- Verify query invalidation still works

#### Step 2.4: Verify Routes (15 min)
- Check that navigation to `/admin/events/${id}` works
- Verify edit page loads with copied event

### 6.3 Phase 3: Testing

**Duration**: 3-4 hours

#### Step 3.1: Unit Tests (1.5 hours)
- Test CopyEventAsync with various scenarios
- Test modal component rendering
- Test form validation
- Test mutation hook

#### Step 3.2: Integration Tests (1 hour)
- Test copy endpoint with database
- Verify all relationships copied correctly
- Verify exclusions (attendances, purchases)
- Test transaction rollback on errors

#### Step 3.3: E2E Tests (1.5 hours)
- Test complete copy workflow
- Test date validation (past dates rejected)
- Test title validation
- Test success notification and navigation
- Test error handling

---

## 7. Technical Considerations

### 7.1 Transaction Requirements
**CRITICAL**: Copy operation must be atomic (all-or-nothing)

```csharp
using var transaction = await _context.Database.BeginTransactionAsync(ct);
try
{
    // 1. Copy Event
    // 2. Copy Sessions (track ID mappings)
    // 3. Copy TicketTypes (use mappings)
    // 4. Copy VolunteerPositions (use mappings)
    // 5. Copy Organizers
    // 6. Copy EventEmailTemplates (NEW)

    await _context.SaveChangesAsync(ct);
    await transaction.CommitAsync(ct);
}
catch
{
    await transaction.RollbackAsync(ct);
    throw;
}
```

**Rationale**: Partial copy leaves orphaned data and broken relationships

### 7.2 ID Remapping for Related Entities
**CRITICAL**: Sessions create new IDs, TicketTypes/VolunteerPositions must reference new Session IDs

```csharp
Dictionary<Guid, Guid> sessionIdMap = new();

// Copy sessions first, track mappings
foreach (var originalSession in originalEvent.Sessions)
{
    var newSessionId = Guid.NewGuid();
    sessionIdMap[originalSession.Id] = newSessionId;
    // ... create new session
}

// Copy ticket types, remap SessionIds
foreach (var originalTicket in originalEvent.TicketTypes)
{
    var newSessionId = originalTicket.SessionId.HasValue
        ? sessionIdMap[originalTicket.SessionId.Value]
        : null;
    // ... create new ticket type with remapped SessionId
}
```

### 7.3 Date Offset Calculations for Sessions
**CRITICAL**: Session times must adjust to new event date

```csharp
var dateOffset = newStartDate - originalEvent.StartDate;

foreach (var originalSession in originalEvent.Sessions)
{
    var newSession = new Session
    {
        StartTime = originalSession.StartTime.Add(dateOffset),
        EndTime = originalSession.EndTime.Add(dateOffset),
        // ... other properties
    };
}
```

### 7.4 Email Template Cloning (NEW)
**CRITICAL**: Custom email templates must be duplicated with new event association

```csharp
// Load original event with email templates
var originalEvent = await _context.Events
    .Include(e => e.EventEmailTemplates)  // CRITICAL INCLUDE
    .Include(e => e.Sessions)
    // ... other includes
    .FirstOrDefaultAsync(e => e.Id == eventId, ct);

// Copy custom email templates
foreach (var originalTemplate in originalEvent.EventEmailTemplates ?? Enumerable.Empty<EventEmailTemplate>())
{
    var copiedTemplate = new EventEmailTemplate
    {
        Id = Guid.NewGuid(),
        EventId = copiedEvent.Id,  // NEW EVENT ID
        GlobalTemplateId = originalTemplate.GlobalTemplateId,  // PRESERVE REFERENCE
        TemplateType = originalTemplate.TemplateType,
        Subject = originalTemplate.Subject,
        HtmlBody = originalTemplate.HtmlBody,
        PlainTextBody = originalTemplate.PlainTextBody,
        TargetSessions = originalTemplate.TargetSessions,
        RecipientGroup = originalTemplate.RecipientGroup,
        IsCustomized = true,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow,
        UpdatedBy = currentAdminUserId
    };

    _context.EventEmailTemplates.Add(copiedTemplate);
}
```

### 7.5 Validation Rules

#### Pre-Copy Validation:
- ✅ Original event exists (404 if not found)
- ✅ User is Admin (401 if not authorized)
- ✅ New start date >= today (400 if past date)
- ✅ New title is valid (400 if empty or too long)

#### Post-Copy Validation:
- ✅ All new IDs are unique
- ✅ All SessionId references are valid
- ✅ IsPublished = false
- ✅ No EventAttendances or TicketPurchases exist
- ✅ Organizers collection is not empty (if original had organizers)
- ✅ Email templates have new IDs and correct EventId (NEW)

### 7.6 CSRF Protection on Endpoint
**REQUIREMENT**: Copy endpoint must validate antiforgery token

```csharp
.MapPost("/{id}/copy", handler)
.ValidateAntiforgeryToken();
```

**Frontend**: Axios interceptor automatically includes CSRF token

### 7.7 Authorization Requirements
**REQUIREMENT**: Only Admins can copy events

```csharp
.RequireAuthorization("Admin");
```

**Alternative**: Use `Roles.Admin` constant if available

---

## 8. File References

### 8.1 Frontend Files

#### Existing Files to Modify:
- `/home/chad/repos/witchcityrope/apps/web/src/pages/admin/AdminEventsPage.tsx`
  **Changes**: Add modal state, integrate CopyEventModal component

- `/home/chad/repos/witchcityrope/apps/web/src/features/events/api/mutations.ts`
  **Changes**: Update `useCopyEvent` to accept `{ eventId, newDate, newTitle }`

#### New Files to Create:
- `/home/chad/repos/witchcityrope/apps/web/src/components/events/CopyEventModal.tsx`
  **Purpose**: Modal component with date/title form

#### Reference Files (Read-Only):
- `/home/chad/repos/witchcityrope/apps/web/src/types/api.types.ts`
  **Purpose**: Auto-generated EventDto type (regenerate after backend changes)

- `/home/chad/repos/witchcityrope/apps/web/src/components/events/SessionFormModal.tsx`
  **Purpose**: Example modal pattern with DateInput and form validation

- `/home/chad/repos/witchcityrope/apps/web/src/features/admin/vetting/components/DenyApplicationModal.tsx`
  **Purpose**: Example simple modal with form submission

### 8.2 Backend Files

#### Existing Files to Modify:
- `/home/chad/repos/witchcityrope/apps/api/Features/Events/Endpoints/EventEndpoints.cs`
  **Changes**: Add `MapPost("/{id}/copy", CopyEventAsync)` endpoint

- `/home/chad/repos/witchcityrope/apps/api/Features/Events/Services/IEventService.cs`
  **Changes**: Add `CopyEventAsync` method signature

- `/home/chad/repos/witchcityrope/apps/api/Features/Events/Services/EventService.cs`
  **Changes**: Implement `CopyEventAsync` with deep copy logic

#### New Files to Create:
- `/home/chad/repos/witchcityrope/apps/api/Features/Events/Models/CopyEventRequest.cs`
  **Purpose**: Request DTO with NewTitle and NewStartDate properties

#### Reference Files (Read-Only):
- `/home/chad/repos/witchcityrope/apps/api/Models/Event.cs`
  **Purpose**: Event entity definition

- `/home/chad/repos/witchcityrope/apps/api/Models/Session.cs`
  **Purpose**: Session entity definition

- `/home/chad/repos/witchcityrope/apps/api/Models/TicketType.cs`
  **Purpose**: TicketType entity definition

- `/home/chad/repos/witchcityrope/apps/api/Models/VolunteerPosition.cs`
  **Purpose**: VolunteerPosition entity definition

- `/home/chad/repos/witchcityrope/apps/api/Features/Participation/Entities/EventAttendance.cs`
  **Purpose**: Attendance entity (for exclusion verification)

- `/home/chad/repos/witchcityrope/apps/api/Models/TicketPurchase.cs`
  **Purpose**: Purchase entity (for exclusion verification)

- `/home/chad/repos/witchcityrope/apps/api/Features/EmailTemplates/Entities/EventEmailTemplate.cs`
  **Purpose**: Email template entity (for copying custom templates)

### 8.3 Test Files

#### New Test Files to Create:
- `/home/chad/repos/witchcityrope/tests/unit/api/Events/CopyEventTests.cs`
  **Purpose**: Unit tests for CopyEventAsync service method

- `/home/chad/repos/witchcityrope/tests/integration/Events/CopyEventIntegrationTests.cs`
  **Purpose**: Integration tests for copy endpoint

- `/home/chad/repos/witchcityrope/tests/unit/web/components/events/CopyEventModal.test.tsx`
  **Purpose**: React component tests for modal

- `/home/chad/repos/witchcityrope/tests/e2e/admin/copy-event.spec.ts`
  **Purpose**: E2E test for complete copy workflow

#### Mock Handlers to Update:
- `/home/chad/repos/witchcityrope/apps/web/tests/mocks/handlers/events.ts`
  **Changes**: Add mock handler for `POST /api/events/:id/copy`

---

## 9. Next Steps

### Recommended Sequence for Implementation

#### Step 1: Backend Foundation (BLOCKING)
**Assigned To**: backend-developer agent
**Duration**: 4-6 hours
**Deliverables**:
- CopyEventRequest.cs
- IEventService.CopyEventAsync method
- EventService.CopyEventAsync implementation
- EventEndpoints copy route
- Regenerated TypeScript types

**Success Criteria**:
- Endpoint responds 200 with EventDto
- New event created with all relationships
- Original event unchanged
- No attendances or purchases copied
- Custom email templates copied correctly (NEW)
- Transaction rollback on errors

#### Step 2: Frontend Modal (DEPENDENT)
**Assigned To**: react-developer agent
**Duration**: 2-3 hours
**Dependencies**: Backend complete and deployed
**Deliverables**:
- CopyEventModal.tsx component
- AdminEventsPage integration
- Updated useCopyEvent mutation
- Route verification

**Success Criteria**:
- Modal opens when Copy button clicked
- Form validates date (future only) and title (3-200 chars)
- Success notification shows and navigates to edit page
- Error notification shows on failure
- Modal closes on success/cancel

#### Step 3: Comprehensive Testing (PARALLEL)
**Assigned To**: test-developer agent
**Duration**: 3-4 hours
**Dependencies**: Backend and frontend complete
**Deliverables**:
- Unit tests (backend service + frontend component)
- Integration tests (API endpoint + database)
- E2E tests (complete workflow)
- Mock handlers updated

**Success Criteria**:
- All tests passing
- Edge cases covered (multi-session, sliding scale, volunteers, email templates)
- Error scenarios tested (404, 401, 400)
- Transaction rollback verified

---

## 10. Success Criteria

### Backend Success Criteria:
- ✅ Copy endpoint returns 200 with EventDto
- ✅ New event has unique ID
- ✅ Event properties copied correctly (title, description, capacity, etc.)
- ✅ IsPublished set to false
- ✅ StartDate/EndDate use new values
- ✅ Sessions copied with adjusted dates
- ✅ TicketTypes copied with remapped SessionIds
- ✅ VolunteerPositions copied with remapped SessionIds, SlotsFilled = 0
- ✅ Organizers copied (same users)
- ✅ Venue reference copied
- ✅ Custom email templates copied with new EventId (NEW)
- ✅ No EventAttendances copied
- ✅ No TicketPurchases copied
- ✅ Transaction rollback on any error

### Frontend Success Criteria:
- ✅ Modal opens when Copy button clicked
- ✅ Date input validates (no past dates)
- ✅ Title input validates (3-200 chars)
- ✅ Title pre-filled with original + " (Copy)"
- ✅ Cancel closes modal without action
- ✅ Copy Event button shows loading state
- ✅ Success notification displays
- ✅ Navigation to edit page works
- ✅ Error notification shows on failure
- ✅ Modal closes on success

### Testing Success Criteria:
- ✅ Unit tests cover service logic
- ✅ Integration tests verify database operations
- ✅ E2E tests validate complete workflow
- ✅ Edge cases tested (multi-session, complex tickets, email templates)
- ✅ Error scenarios tested (404, 401, validation errors)
- ✅ Mock handlers updated for tests

---

## 11. Appendices

### Appendix A: Entity Relationship Diagram

```
Event (Copied)
├── Id: NEW GUID
├── Title: User input or original + "(Copy)"
├── StartDate: User input (new date)
├── EndDate: Calculated from new StartDate
├── IsPublished: FALSE (draft)
├── Content Properties: COPIED AS-IS
│   ├── ShortDescription
│   ├── Description
│   └── Policies
├── Configuration Properties: COPIED AS-IS
│   ├── Capacity
│   ├── EventType
│   ├── VenueId
│   └── 6 Timing Controls
│
├── Sessions (DEEP COPY)
│   ├── Id: NEW GUID
│   ├── EventId: New Event ID
│   ├── StartTime: Original + date offset
│   ├── EndTime: Original + date offset
│   ├── SessionCode: COPIED
│   ├── Name: COPIED
│   └── Capacity: COPIED
│
├── TicketTypes (DEEP COPY)
│   ├── Id: NEW GUID
│   ├── EventId: New Event ID
│   ├── SessionId: REMAPPED to new Session ID
│   ├── Name: COPIED
│   ├── PricingType: COPIED
│   ├── Price/MinPrice/MaxPrice: COPIED
│   ├── Available: COPIED
│   └── Sold: RESET to 0
│
├── VolunteerPositions (DEEP COPY)
│   ├── Id: NEW GUID
│   ├── EventId: New Event ID
│   ├── SessionId: REMAPPED to new Session ID
│   ├── Title: COPIED
│   ├── Description: COPIED
│   ├── SlotsNeeded: COPIED
│   └── SlotsFilled: RESET to 0
│
├── EventEmailTemplates (DEEP COPY - NEW)
│   ├── Id: NEW GUID
│   ├── EventId: New Event ID (CRITICAL FK UPDATE)
│   ├── GlobalTemplateId: PRESERVED
│   ├── TemplateType: COPIED
│   ├── Subject: COPIED
│   ├── HtmlBody: COPIED
│   ├── PlainTextBody: COPIED
│   ├── TargetSessions: COPIED
│   ├── RecipientGroup: COPIED
│   ├── CreatedAt: RESET to UtcNow
│   ├── UpdatedAt: RESET to UtcNow
│   └── UpdatedBy: RESET to current admin user
│
├── Organizers (REFERENCE COPY)
│   └── Same ApplicationUser references
│
└── EXCLUDED
    ├── EventAttendances (NOT COPIED)
    └── TicketPurchases (NOT COPIED)
```

### Appendix B: Code Examples

#### Backend Service Method (Skeleton):
```csharp
public async Task<Result<EventDto>> CopyEventAsync(
    Guid originalEventId,
    string newTitle,
    DateTime newStartDate,
    CancellationToken ct = default)
{
    // 1. Load original event
    var originalEvent = await _context.Events
        .Include(e => e.Sessions)
        .Include(e => e.TicketTypes)
        .Include(e => e.VolunteerPositions)
        .Include(e => e.Organizers)
        .Include(e => e.EventEmailTemplates)  // NEW: Include custom templates
        .AsNoTracking()
        .FirstOrDefaultAsync(e => e.Id == originalEventId, ct);

    if (originalEvent == null)
        return Result<EventDto>.Failure("Event not found");

    using var transaction = await _context.Database.BeginTransactionAsync(ct);
    try
    {
        // 2. Create new event
        var copiedEvent = new Event
        {
            Id = Guid.NewGuid(),
            Title = newTitle,
            StartDate = newStartDate,
            EndDate = newStartDate.Add(originalEvent.EndDate - originalEvent.StartDate),
            IsPublished = false,
            // ... copy other properties
        };
        _context.Events.Add(copiedEvent);

        // 3. Copy sessions, track ID mappings
        var sessionIdMap = new Dictionary<Guid, Guid>();
        foreach (var originalSession in originalEvent.Sessions)
        {
            var newSessionId = Guid.NewGuid();
            sessionIdMap[originalSession.Id] = newSessionId;

            var dateOffset = newStartDate - originalEvent.StartDate;
            var copiedSession = new Session
            {
                Id = newSessionId,
                EventId = copiedEvent.Id,
                SessionCode = originalSession.SessionCode,
                StartTime = originalSession.StartTime.Add(dateOffset),
                EndTime = originalSession.EndTime.Add(dateOffset),
                // ... copy other properties
            };
            _context.Sessions.Add(copiedSession);
        }

        // 4. Copy ticket types with SessionId remapping
        // 5. Copy volunteer positions with SessionId remapping
        // 6. Copy organizers

        // 7. Copy custom email templates (NEW)
        foreach (var originalTemplate in originalEvent.EventEmailTemplates ?? Enumerable.Empty<EventEmailTemplate>())
        {
            var copiedTemplate = new EventEmailTemplate
            {
                Id = Guid.NewGuid(),
                EventId = copiedEvent.Id,  // NEW EVENT ID
                GlobalTemplateId = originalTemplate.GlobalTemplateId,
                TemplateType = originalTemplate.TemplateType,
                Subject = originalTemplate.Subject,
                HtmlBody = originalTemplate.HtmlBody,
                PlainTextBody = originalTemplate.PlainTextBody,
                TargetSessions = originalTemplate.TargetSessions,
                RecipientGroup = originalTemplate.RecipientGroup,
                IsCustomized = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                UpdatedBy = _currentUserService.UserId  // Assuming service to get current user
            };
            _context.EventEmailTemplates.Add(copiedTemplate);
        }

        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return Result<EventDto>.Success(_mapper.Map<EventDto>(copiedEvent));
    }
    catch
    {
        await transaction.RollbackAsync(ct);
        throw;
    }
}
```

#### Frontend Modal Component (Skeleton):
```typescript
import React from 'react';
import { Modal, Stack, TextInput, Button, Group, Title } from '@mantine/core';
import { DateInput } from '@mantine/dates';
import { useForm } from '@mantine/form';
import { notifications } from '@mantine/notifications';
import type { EventDto } from '@witchcityrope/shared-types';
import { useCopyEvent } from '../features/events/api/mutations';

interface CopyEventModalProps {
  opened: boolean;
  onClose: () => void;
  event: EventDto | null;
  onSuccess: (copiedEventId: string) => void;
}

export const CopyEventModal: React.FC<CopyEventModalProps> = ({
  opened,
  onClose,
  event,
  onSuccess
}) => {
  const copyEventMutation = useCopyEvent();

  const form = useForm({
    initialValues: {
      newDate: new Date(),
      newTitle: event ? `${event.title} (Copy)` : '',
    },
    validate: {
      newDate: (value) => (!value ? 'Date is required' : null),
      newTitle: (value) => {
        if (!value) return 'Title is required';
        if (value.length < 3) return 'Title must be at least 3 characters';
        if (value.length > 200) return 'Title cannot exceed 200 characters';
        return null;
      },
    },
  });

  const handleSubmit = form.onSubmit(async (values) => {
    if (!event) return;

    try {
      const copiedEvent = await copyEventMutation.mutateAsync({
        eventId: event.id,
        newDate: values.newDate,
        newTitle: values.newTitle,
      });

      notifications.show({
        title: 'Event Copied',
        message: 'Event copied successfully. Redirecting to edit page.',
        color: 'green'
      });

      onSuccess(copiedEvent.id);
      onClose();
    } catch (error) {
      notifications.show({
        title: 'Copy Failed',
        message: 'Unable to copy event. Please try again.',
        color: 'red'
      });
    }
  });

  return (
    <Modal
      opened={opened}
      onClose={onClose}
      title={<Title order={3} style={{ color: '#880124' }}>Copy Event</Title>}
      centered
      size="md"
    >
      <form onSubmit={handleSubmit}>
        <Stack gap="md">
          <DateInput
            label="New Event Date"
            placeholder="Select date"
            required
            minDate={new Date()}
            data-testid="input-event-date"
            {...form.getInputProps('newDate')}
          />

          <TextInput
            label="New Event Title"
            placeholder="e.g., Spring Workshop 2025"
            required
            data-testid="input-event-title"
            {...form.getInputProps('newTitle')}
          />

          <Group justify="flex-end" gap="md" mt="md">
            <Button
              variant="outline"
              onClick={onClose}
              disabled={copyEventMutation.isPending}
            >
              Cancel
            </Button>

            <Button
              type="submit"
              loading={copyEventMutation.isPending}
            >
              Copy Event
            </Button>
          </Group>
        </Stack>
      </form>
    </Modal>
  );
};
```

### Appendix C: Related Documentation

- **Backend Data Model Analysis**: `/home/chad/repos/witchcityrope/test-results/event-copy-data-model-analysis-2025-11-26.md`
- **Frontend UI Analysis**: `/home/chad/repos/witchcityrope/session-work/2025-11-26/admin-events-ui-analysis-report.md`
- **Events Functional Area**: `/home/chad/repos/witchcityrope/docs/functional-areas/events/`
- **Functional Area Master Index**: `/home/chad/repos/witchcityrope/docs/architecture/functional-area-master-index.md`

---

**End of Analysis**

**Next Action**: Assign backend-developer agent to implement Phase 1 (Backend) before proceeding with frontend modal.

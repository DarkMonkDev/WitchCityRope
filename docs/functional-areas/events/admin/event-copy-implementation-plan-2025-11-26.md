# Event Copy Feature - Implementation Plan

**Date**: November 26, 2025
**Feature**: Event Copy with Modal Dialog
**Functional Area**: Events Management → Admin
**Work Type**: Feature Completion
**Source Analysis**: [event-copy-feature-analysis-2025-11-26.md](/home/chad/repos/witchcityrope/docs/functional-areas/events/admin/event-copy-feature-analysis-2025-11-26.md)

---

## 1. Overview

### Feature Description
Implement complete event copy functionality allowing administrators to duplicate existing events with a new date and title. The feature requires:
- Backend API endpoint to perform deep copy of event and all related entities
- Frontend modal dialog for user input (date and title)
- Comprehensive test coverage for all scenarios

### Current Status
- **Frontend**: Copy button exists in admin events table but fails with 404 error
- **Backend**: Endpoint `POST /api/events/{id}/copy` does NOT exist
- **Blocker**: Backend must be implemented FIRST before frontend modal can function

### Implementation Phases
1. **Phase 1**: Backend Implementation (BLOCKING - 5-7 hours)
2. **Phase 2**: Frontend Implementation (DEPENDENT - 2-3 hours)
3. **Phase 3**: Testing (PARALLEL - 3-4 hours)

### Agent Assignments
- **backend-developer**: Phase 1 - Backend implementation
- **react-developer**: Phase 2 - Frontend modal and integration
- **test-developer**: Phase 3 - Test suite creation

### Estimated Effort
- **Backend**: 5-7 hours (increased from 4-6 due to email template logic)
- **Frontend**: 2-3 hours
- **Testing**: 3-4 hours
- **Total**: 10-14 hours

---

## 2. Phase 1: Backend Implementation (backend-developer agent)

**Duration**: 5-7 hours
**Status**: BLOCKING - Must complete before frontend work
**Dependencies**: None

### Task 1: Create Request/Response DTOs

**File**: `/home/chad/repos/witchcityrope/apps/api/Features/Events/Models/CopyEventRequest.cs`

**Action**: Create new file with request DTO

**Required Properties**:
```csharp
public record CopyEventRequest
{
    [Required(ErrorMessage = "New start date is required")]
    public required DateTimeOffset NewStartDate { get; init; }

    [Required(ErrorMessage = "Title is required")]
    [MinLength(3, ErrorMessage = "Title must be at least 3 characters")]
    [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
    public required string NewTitle { get; init; }
}
```

**Validation Attributes**:
- `[Required]` on both properties
- `[MinLength(3)]` on NewTitle
- `[MaxLength(200)]` on NewTitle
- Error messages for user feedback

**Response**: Use existing `EventDto` from auto-generated types

**Duration**: 30 minutes

---

### Task 2: Add Service Interface Method

**File**: `/home/chad/repos/witchcityrope/apps/api/Features/Events/Services/IEventService.cs`

**Action**: Add method signature to interface

**Required Method**:
```csharp
/// <summary>
/// Creates a copy of an existing event with a new date and title.
/// Deep copies all related entities (sessions, ticket types, volunteer positions, organizers, email templates).
/// Excludes attendance and transaction data.
/// </summary>
/// <param name="eventId">ID of the event to copy</param>
/// <param name="request">Copy parameters (new date and title)</param>
/// <param name="cancellationToken">Cancellation token</param>
/// <returns>Result containing the copied event DTO or error</returns>
Task<(bool Success, EventDto? Response, string? Error)> CopyEventAsync(
    string eventId,
    CopyEventRequest request,
    CancellationToken cancellationToken = default);
```

**Documentation**: XML comments explaining:
- What the method does (deep copy with new date/title)
- What is copied (sessions, tickets, volunteers, organizers, email templates)
- What is excluded (attendance, purchases)
- Return type structure

**Duration**: 15 minutes

---

### Task 3: Implement Service Method

**File**: `/home/chad/repos/witchcityrope/apps/api/Features/Events/Services/EventService.cs`

**Action**: Implement full copy logic with transaction management

**Implementation Requirements**:

#### 3.1. Load Source Event
```csharp
// Load with ALL related entities using Include chains
var originalEvent = await _context.Events
    .Include(e => e.Sessions)
    .Include(e => e.TicketTypes)
    .Include(e => e.VolunteerPositions)
    .Include(e => e.Organizers)
    .Include(e => e.EventEmailTemplates)
    .Include(e => e.Venue)
    .AsNoTracking()  // CRITICAL: prevent tracking for copy
    .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

if (originalEvent == null)
    return (false, null, "Event not found");
```

#### 3.2. Create New Event with Copied Properties
```csharp
var copiedEvent = new Event
{
    Id = Guid.NewGuid(),  // NEW ID
    Title = request.NewTitle,  // USER INPUT
    StartDate = request.NewStartDate,  // USER INPUT
    EndDate = request.NewStartDate.Add(originalEvent.EndDate - originalEvent.StartDate),  // CALCULATED
    IsPublished = false,  // ALWAYS DRAFT

    // COPY AS-IS
    ShortDescription = originalEvent.ShortDescription,
    Description = originalEvent.Description,
    Policies = originalEvent.Policies,
    Capacity = originalEvent.Capacity,
    EventType = originalEvent.EventType,
    VenueId = originalEvent.VenueId,

    // COPY TIMING CONTROLS (6 fields)
    RegistrationOpenHours = originalEvent.RegistrationOpenHours,
    RegistrationCloseHours = originalEvent.RegistrationCloseHours,
    CancellationOpenHours = originalEvent.CancellationOpenHours,
    CancellationCloseHours = originalEvent.CancellationCloseHours,
    VolunteerRegistrationCloseHours = originalEvent.VolunteerRegistrationCloseHours,
    VolunteerCancellationCloseHours = originalEvent.VolunteerCancellationCloseHours,

    // RESET TIMESTAMPS
    CreatedAt = DateTimeOffset.UtcNow,
    UpdatedAt = DateTimeOffset.UtcNow
};
```

#### 3.3. Calculate Date Offset for Sessions
```csharp
var dateOffset = request.NewStartDate - originalEvent.StartDate;
```

#### 3.4. Deep Copy Sessions (with Date Offset)
```csharp
var sessionIdMap = new Dictionary<Guid, Guid>();

foreach (var originalSession in originalEvent.Sessions)
{
    var newSessionId = Guid.NewGuid();
    sessionIdMap[originalSession.Id] = newSessionId;  // TRACK MAPPING

    var copiedSession = new Session
    {
        Id = newSessionId,
        EventId = copiedEvent.Id,
        SessionCode = originalSession.SessionCode,
        Name = originalSession.Name,
        StartTime = originalSession.StartTime.Add(dateOffset),  // APPLY OFFSET
        EndTime = originalSession.EndTime.Add(dateOffset),      // APPLY OFFSET
        Capacity = originalSession.Capacity
    };

    copiedEvent.Sessions.Add(copiedSession);
}
```

#### 3.5. Deep Copy TicketTypes (with Session ID Remapping)
```csharp
foreach (var originalTicket in originalEvent.TicketTypes)
{
    var copiedTicket = new TicketType
    {
        Id = Guid.NewGuid(),
        EventId = copiedEvent.Id,

        // REMAP SESSION ID (if session-specific ticket)
        SessionId = originalTicket.SessionId.HasValue
            ? sessionIdMap[originalTicket.SessionId.Value]
            : null,

        // COPY PROPERTIES
        Name = originalTicket.Name,
        Description = originalTicket.Description,
        PricingType = originalTicket.PricingType,
        Price = originalTicket.Price,
        MinPrice = originalTicket.MinPrice,
        MaxPrice = originalTicket.MaxPrice,
        DefaultPrice = originalTicket.DefaultPrice,
        Available = originalTicket.Available

        // Sold and Purchases NOT copied (computed/excluded)
    };

    copiedEvent.TicketTypes.Add(copiedTicket);
}
```

#### 3.6. Deep Copy VolunteerPositions (with Session ID Remapping, Reset SlotsFilled)
```csharp
foreach (var originalPosition in originalEvent.VolunteerPositions)
{
    var copiedPosition = new VolunteerPosition
    {
        Id = Guid.NewGuid(),
        EventId = copiedEvent.Id,

        // REMAP SESSION ID (if session-specific position)
        SessionId = originalPosition.SessionId.HasValue
            ? sessionIdMap[originalPosition.SessionId.Value]
            : null,

        // COPY PROPERTIES
        Title = originalPosition.Title,
        Description = originalPosition.Description,
        SlotsNeeded = originalPosition.SlotsNeeded,
        IsPublicFacing = originalPosition.IsPublicFacing,

        // RESET FILLED COUNT
        SlotsFilled = 0
    };

    copiedEvent.VolunteerPositions.Add(copiedPosition);
}
```

#### 3.7. Copy Organizer References
```csharp
// Copy many-to-many relationship (same users)
copiedEvent.Organizers = originalEvent.Organizers.ToList();
```

#### 3.8. Copy EventEmailTemplates (custom templates only)
```csharp
// Copy custom email templates associated with source event
foreach (var originalTemplate in originalEvent.EventEmailTemplates)
{
    var copiedTemplate = new EventEmailTemplate
    {
        Id = Guid.NewGuid(),  // NEW ID
        EventId = copiedEvent.Id,  // Associate with new event

        // COPY PROPERTIES
        Subject = originalTemplate.Subject,
        HtmlBody = originalTemplate.HtmlBody,
        PlainTextBody = originalTemplate.PlainTextBody,
        TargetSessions = originalTemplate.TargetSessions,
        RecipientGroup = originalTemplate.RecipientGroup,
        GlobalTemplateId = originalTemplate.GlobalTemplateId  // Preserve reference to global template
    };

    copiedEvent.EventEmailTemplates.Add(copiedTemplate);
}
```

#### 3.9. Use Database Transaction for Atomicity
```csharp
using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
try
{
    _context.Events.Add(copiedEvent);
    await _context.SaveChangesAsync(cancellationToken);
    await transaction.CommitAsync(cancellationToken);

    // Map to DTO and return
    var eventDto = _mapper.Map<EventDto>(copiedEvent);
    return (true, eventDto, null);
}
catch (Exception ex)
{
    await transaction.RollbackAsync(cancellationToken);
    _logger.LogError(ex, "Failed to copy event {EventId}", eventId);
    return (false, null, "Failed to copy event. Please try again.");
}
```

#### 3.10. Error Handling and Logging
- Log all exceptions with event ID
- Return user-friendly error messages
- Ensure transaction rollback on any failure
- Validate that all SessionId references are valid

#### 3.11. Critical Logic Notes
- **Date Offset**: `NewStartDate - OriginalStartDate` must be applied to ALL session times
- **ID Mapping**: Track `oldSessionId → newSessionId` in dictionary for remapping
- **Remapping Order**: Sessions FIRST, then TicketTypes/VolunteerPositions (they need new session IDs)
- **Transaction**: ALL operations must succeed or ALL rollback (atomicity)
- **AsNoTracking**: Use on original query to prevent EF tracking issues during copy
- **EventEmailTemplate copying**: Clone all custom templates associated with source event
- **GlobalTemplateId preserves reference** to original global template source
- **New templates get new IDs** but maintain same content/settings

**Duration**: 3-4 hours

---

### Task 4: Create API Endpoint

**File**: `/home/chad/repos/witchcityrope/apps/api/Features/Events/Endpoints/EventEndpoints.cs`

**Action**: Add POST endpoint with CSRF validation and Admin authorization

**Required Endpoint**:
```csharp
group.MapPost("/{id}/copy", async (
    [FromRoute] string id,
    [FromBody] CopyEventRequest request,
    IEventService eventService,
    CancellationToken cancellationToken) =>
{
    var (success, response, error) = await eventService.CopyEventAsync(
        id,
        request,
        cancellationToken);

    if (!success)
    {
        return error == "Event not found"
            ? Results.NotFound(new { Error = error })
            : Results.BadRequest(new { Error = error });
    }

    return Results.Ok(response);
})
.RequireAuthorization("Admin")
.ValidateAntiforgeryToken()
.WithName("CopyEvent")
.WithTags("Events")
.WithOpenApi(operation => new(operation)
{
    Summary = "Copy an existing event",
    Description = "Creates a copy of an event with a new date and title. " +
                  "Deep copies all related entities (sessions, ticket types, volunteers, email templates). " +
                  "Excludes attendance and transaction data. New event created as draft."
})
.Produces<EventDto>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status404NotFound);
```

**Requirements**:
- **CSRF Validation**: `.ValidateAntiforgeryToken()` - prevents CSRF attacks
- **Authorization**: `.RequireAuthorization("Admin")` - only admins can copy events
- **Request Body**: Read `CopyEventRequest` from body
- **Response Codes**:
  - `200 OK`: Successfully copied, return `EventDto`
  - `400 Bad Request`: Validation errors or copy failure
  - `401 Unauthorized`: User not authenticated or not admin
  - `404 Not Found`: Original event doesn't exist
- **OpenAPI Documentation**: Summary, description, tags for API docs

**Duration**: 30 minutes

---

### Task 5: Regenerate Frontend Types

**Command**:
```bash
cd /home/chad/repos/witchcityrope/packages/shared-types
npm run generate
```

**Action**: Regenerate TypeScript types from OpenAPI spec

**Verification**:
- Verify `CopyEventRequest` type exists in generated types
- Verify `EventDto` includes all necessary properties
- Check for any TypeScript compilation errors

**Frontend Import Pattern**:
```typescript
import type { components } from '@witchcityrope/shared-types';
export type CopyEventRequest = components['schemas']['CopyEventRequest'];
export type EventDto = components['schemas']['EventDto'];
```

**Duration**: 5 minutes

---

### Phase 1 Success Criteria

**Backend Complete Checklist**:
- [ ] `CopyEventRequest.cs` created with validation attributes
- [ ] `IEventService.CopyEventAsync` method signature added
- [ ] `EventService.CopyEventAsync` implemented with:
  - [ ] Load original event with includes
  - [ ] Create new event with copied properties
  - [ ] Apply new title and start date
  - [ ] Calculate date offset for sessions
  - [ ] Deep copy Sessions (with date offset)
  - [ ] Deep copy TicketTypes (with session ID remapping)
  - [ ] Deep copy VolunteerPositions (with session ID remapping, reset SlotsFilled)
  - [ ] Copy Organizer references
  - [ ] Copy EventEmailTemplates (custom templates with new IDs)
  - [ ] Set IsPublished = false
  - [ ] Use database transaction for atomicity
  - [ ] Error handling and logging
- [ ] EventEmailTemplates copied and associated with new event
- [ ] API endpoint added with CSRF + Admin authorization
- [ ] Types regenerated successfully
- [ ] Backend compiles without errors
- [ ] Endpoint responds with 200/404/400 as appropriate

---

## 3. Phase 2: Frontend Implementation (react-developer agent)

**Duration**: 2-3 hours
**Status**: DEPENDENT - Requires Phase 1 backend complete
**Dependencies**: Backend endpoint operational, types regenerated

### Task 1: Create Copy Event Modal Component

**File**: `/home/chad/repos/witchcityrope/apps/web/src/components/events/CopyEventModal.tsx`

**Action**: Create new modal component with date/title form

**Component Requirements**:

#### 1.1. Imports and Type Definitions
```typescript
import React from 'react';
import { Modal, Stack, TextInput, Button, Group, Title } from '@mantine/core';
import { DateInput } from '@mantine/dates';
import { useForm } from '@mantine/form';
import { notifications } from '@mantine/notifications';
import type { components } from '@witchcityrope/shared-types';
import { useCopyEvent } from '../../features/events/api/mutations';

type EventDto = components['schemas']['EventDto'];

interface CopyEventModalProps {
  opened: boolean;
  onClose: () => void;
  eventToCopy: { id: string; title: string } | null;
}
```

#### 1.2. Form Setup with Validation
```typescript
const form = useForm({
  initialValues: {
    newDate: new Date(),
    newTitle: eventToCopy ? `${eventToCopy.title} (Copy)` : '',
  },
  validate: {
    newDate: (value) => {
      if (!value) return 'Date is required';
      if (value < new Date()) return 'Event date must be in the future';
      return null;
    },
    newTitle: (value) => {
      if (!value) return 'Title is required';
      if (value.length < 3) return 'Title must be at least 3 characters';
      if (value.length > 200) return 'Title cannot exceed 200 characters';
      return null;
    },
  },
});
```

#### 1.3. Submit Handler with useCopyEvent Mutation
```typescript
const copyEventMutation = useCopyEvent();

const handleSubmit = form.onSubmit(async (values) => {
  if (!eventToCopy) return;

  try {
    const copiedEvent = await copyEventMutation.mutateAsync({
      eventId: eventToCopy.id,
      newStartDate: values.newDate.toISOString(),
      newTitle: values.newTitle,
    });

    notifications.show({
      title: 'Success',
      message: 'Event copied successfully. Redirecting to edit page.',
      color: 'green',
    });

    // Navigate to edit page
    window.location.href = `/admin/events/${copiedEvent.id}`;
    onClose();
  } catch (error) {
    notifications.show({
      title: 'Error',
      message: 'Unable to copy event. Please try again.',
      color: 'red',
    });
  }
});
```

#### 1.4. Modal JSX Structure
```typescript
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
          maxLength={200}
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
```

**Component Features**:
1. **Mantine Modal** wrapper with centered positioning
2. **Form with @mantine/form** useForm hook
3. **DateInput** for new event date (minDate = today)
4. **TextInput** for new event title (pre-filled with "{original.title} (Copy)")
5. **Form Validation** (date not in past, title 3-200 chars)
6. **Submit Handler** calling useCopyEvent mutation
7. **Loading State** during mutation
8. **Error Display** if mutation fails
9. **Close Modal** on success (mutation handles navigation)

**Props Needed**:
- `opened: boolean` - Modal visibility state
- `onClose: () => void` - Close handler
- `eventToCopy: { id: string; title: string } | null` - Event to copy

**Duration**: 1.5 hours

---

### Task 2: Update Copy Event Mutation

**File**: `/home/chad/repos/witchcityrope/apps/web/src/features/events/api/mutations.ts`

**Action**: Update useCopyEvent to accept parameters

**Current Implementation** (BROKEN):
```typescript
export const useCopyEvent = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (eventId: string) => {
      const response = await api.post(`/api/events/${eventId}/copy`);
      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['events'] });
    },
  });
};
```

**New Implementation** (CORRECT):
```typescript
import type { components } from '@witchcityrope/shared-types';

type CopyEventRequest = components['schemas']['CopyEventRequest'];
type EventDto = components['schemas']['EventDto'];

export const useCopyEvent = () => {
  const queryClient = useQueryClient();

  return useMutation<EventDto, Error, {
    eventId: string;
    newStartDate: string;
    newTitle: string;
  }>({
    mutationFn: async ({ eventId, newStartDate, newTitle }) => {
      const response = await api.post<EventDto>(
        `/api/events/${eventId}/copy`,
        {
          newStartDate,
          newTitle,
        } as CopyEventRequest
      );
      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['events'] });
    },
    onError: (error) => {
      console.error('Failed to copy event:', error);
    },
  });
};
```

**Changes**:
- Accept parameters object: `{ eventId, newStartDate, newTitle }`
- Send request body with `CopyEventRequest` shape
- Type mutation properly with `EventDto` return type
- Use auto-generated types from `@witchcityrope/shared-types`

**Duration**: 30 minutes

---

### Task 3: Integrate Modal with Admin Events Page

**File**: `/home/chad/repos/witchcityrope/apps/web/src/pages/admin/AdminEventsPage.tsx`

**Action**: Add modal state and integrate CopyEventModal

**Changes Required**:

#### 3.1. Import Modal Component
```typescript
import { CopyEventModal } from '../../components/events/CopyEventModal';
```

#### 3.2. Add State for Modal
```typescript
const [copyModalOpened, setCopyModalOpened] = useState(false);
const [eventToCopy, setEventToCopy] = useState<{id: string; title: string} | null>(null);
```

#### 3.3. Update handleCopyEvent to Open Modal
```typescript
const handleCopyEvent = (event: EventDto) => {
  setEventToCopy({ id: event.id, title: event.title });
  setCopyModalOpened(true);
};
```

#### 3.4. Add CopyEventModal to JSX
```typescript
return (
  <>
    {/* Existing page content */}
    <EventsTableView
      events={events}
      onEdit={handleEditEvent}
      onCopy={handleCopyEvent}  // Already wired
      onDelete={handleDeleteEvent}
    />

    {/* Add modal AFTER EventsTableView */}
    <CopyEventModal
      opened={copyModalOpened}
      onClose={() => setCopyModalOpened(false)}
      eventToCopy={eventToCopy}
    />
  </>
);
```

**Current Behavior**: Clicking "Copy" button calls API directly (fails with 404)
**New Behavior**: Clicking "Copy" button opens modal for date/title input

**Duration**: 30 minutes

---

### Task 4: Update Test Mock Handlers

**File**: `/home/chad/repos/witchcityrope/apps/web/src/test/mocks/handlers.ts`

**Action**: Add mock handler for copy endpoint

**Required Mock Handler**:
```typescript
import { http, HttpResponse } from 'msw';

export const handlers = [
  // ... existing handlers

  http.post('/api/events/:id/copy', async ({ params, request }) => {
    const { id } = params;
    const body = await request.json() as { newStartDate: string; newTitle: string };

    // Mock successful copy
    return HttpResponse.json({
      id: `copied-${id}`,
      title: body.newTitle,
      startDate: body.newStartDate,
      endDate: new Date(new Date(body.newStartDate).getTime() + 2 * 60 * 60 * 1000).toISOString(),
      shortDescription: 'Copied event description',
      description: 'Full copied event description',
      eventType: 'Workshop',
      capacity: 20,
      isPublished: false,
      venueId: 'venue-1',
      // ... other event properties
    } as EventDto);
  }),
];
```

**Purpose**: Allow frontend tests to run without backend

**Duration**: 15 minutes

---

### Task 5: Fix Navigation Route (if needed)

**Current Code**:
```typescript
// In CopyEventModal or AdminEventsPage
navigate(`/admin/events/${copiedEventId}`);
```

**Action**: Verify route exists and supports editing

**Check**:
1. Does `/admin/events/:id` route exist in React Router config?
2. Does it load the event edit form?
3. Does it handle the new event ID correctly?

**If Route Wrong**: Update to correct route (e.g., `/admin/events/edit/${copiedEventId}`)

**Duration**: 15 minutes

---

### Phase 2 Success Criteria

**Frontend Complete Checklist**:
- [ ] CopyEventModal component created
- [ ] Modal integrated with AdminEventsPage
- [ ] Mutation updated with parameters
- [ ] Test mocks added
- [ ] Navigation route verified
- [ ] Frontend compiles without errors
- [ ] No TypeScript errors

**User Experience Checklist**:
- [ ] Clicking "Copy" button opens modal
- [ ] Modal pre-fills title with original + " (Copy)"
- [ ] Date input validates (no past dates)
- [ ] Title input validates (3-200 chars)
- [ ] Cancel closes modal without action
- [ ] Copy Event button shows loading state
- [ ] Success notification displays
- [ ] Navigation to edit page works
- [ ] Error notification shows on failure

---

## 4. Phase 3: Testing (test-developer agent)

**Duration**: 3-4 hours
**Status**: PARALLEL - Can start after Phase 1 complete
**Dependencies**: Backend implementation, frontend types regenerated

**Note**: Detailed testing plan available in separate document:
→ [event-copy-testing-plan-2025-11-26.md](/home/chad/repos/witchcityrope/docs/functional-areas/events/admin/event-copy-testing-plan-2025-11-26.md)

### Test Categories

#### 1. Unit Tests (Backend)
**File**: `/home/chad/repos/witchcityrope/tests/unit/api/Features/Events/Services/EventServiceCopyTests.cs`
- Test all service logic scenarios (valid event, invalid ID, database errors)
- Verify deep copy operations (sessions, tickets, volunteers, organizers, email templates)
- Test date offset calculations
- Test ID remapping logic

#### 2. Unit Tests (Frontend)
**File**: `/home/chad/repos/witchcityrope/tests/unit/web/components/events/CopyEventModal.test.tsx`
- Test modal rendering
- Test form validation (date and title)
- Test mutation calls
- Test loading states
- Test success/error handling

#### 3. Integration Tests
**File**: `/home/chad/repos/witchcityrope/tests/integration/Events/EventCopyIntegrationTests.cs`
- Test end-to-end copy operation with database
- Verify all related entities copied correctly
- Test CSRF protection
- Test authorization (Admin only)
- Test transaction rollback on errors

#### 4. E2E Tests
**File**: `/home/chad/repos/witchcityrope/tests/e2e/admin/event-copy.spec.ts`
- Test complete user workflow (login → navigate → copy → verify)
- Test modal interactions
- Test validation errors
- Test success notification
- Test navigation to edit page

**Duration**: 3-4 hours total across all test categories

---

## 5. Implementation Sequence

### Sequence Overview

```
Phase 1: Backend (BLOCKING)
    ↓
Phase 1 Complete + Types Regenerated
    ↓
Phase 2: Frontend (DEPENDENT)  +  Phase 3: Testing (PARALLEL)
    ↓
All Phases Complete
    ↓
Feature Ready for Deployment
```

### Detailed Sequence

#### Step 1: Backend-Developer Completes Phase 1 (5-7 hours)
1. Create `CopyEventRequest.cs`
2. Update `IEventService.cs`
3. Implement `EventService.CopyEventAsync`
4. Add API endpoint to `EventEndpoints.cs`
5. Regenerate TypeScript types
6. Verify backend compiles

**Deliverables**:
- [ ] Backend compiles without errors
- [ ] Endpoint responds 200 with EventDto
- [ ] Types regenerated successfully

---

#### Step 2: React-Developer Completes Phase 2 (2-3 hours)
**Prerequisites**: Phase 1 complete, types regenerated

1. Create `CopyEventModal.tsx` component
2. Update `useCopyEvent` mutation in `mutations.ts`
3. Integrate modal with `AdminEventsPage.tsx`
4. Add test mock handlers
5. Verify navigation route

**Deliverables**:
- [ ] Frontend compiles without errors
- [ ] No TypeScript errors
- [ ] Modal opens when Copy clicked
- [ ] Form validates correctly

---

#### Step 3: Test-Developer Completes Phase 3 (3-4 hours)
**Prerequisites**: Phase 1 complete (can start in parallel with Phase 2)

1. Create backend unit tests
2. Create frontend unit tests
3. Create integration tests
4. Create E2E tests
5. Update test catalog

**Deliverables**:
- [ ] All tests pass
- [ ] Test coverage meets quality gates
- [ ] Test catalog updated

---

#### Step 4: Manual Testing and Verification
**Prerequisites**: All phases complete

1. Test copy with basic event
2. Test copy with complex event (sessions, tickets, volunteers)
3. Test validation errors
4. Test success flow
5. Verify copied event can be edited/published

**Deliverables**:
- [ ] All manual tests pass
- [ ] No regressions found

---

## 6. Handoff Requirements

### Handoff Document Template
**Location**: `/home/chad/repos/witchcityrope/docs/standards-processes/agent-handoff-template.md`

### Handoff Document Location
**Path**: `/home/chad/repos/witchcityrope/docs/functional-areas/events/admin/handoffs/`

### Naming Convention
**Pattern**: `[agent-name]-event-copy-YYYY-MM-DD-handoff.md`

**Examples**:
- `backend-developer-event-copy-2025-11-26-handoff.md`
- `react-developer-event-copy-2025-11-26-handoff.md`
- `test-developer-event-copy-2025-11-26-handoff.md`

### Required Handoff Content

Each agent must document:
1. **Work Completed**: What was implemented
2. **Files Created/Modified**: Complete list with purposes
3. **Key Decisions**: Important implementation choices
4. **Technical Challenges**: Issues encountered and solutions
5. **Testing Performed**: What was tested and results
6. **Known Issues**: Any remaining problems or limitations
7. **Next Steps**: What the next agent needs to know
8. **Documentation Updated**: File registry, test catalog, etc.

---

## 7. Success Criteria

### Backend Complete (Phase 1)
- [ ] `CopyEventRequest.cs` created with validation
- [ ] `IEventService` interface updated
- [ ] `EventService` method implemented with:
  - [ ] Load original event with includes
  - [ ] Create new event with copied properties
  - [ ] Deep copy Sessions (with date offset)
  - [ ] Deep copy TicketTypes (with session ID remapping)
  - [ ] Deep copy VolunteerPositions (with session ID remapping)
  - [ ] Copy Organizer references
  - [ ] Copy EventEmailTemplates (custom templates)
  - [ ] Transaction management
  - [ ] Error handling and logging
- [ ] API endpoint added with CSRF + auth
- [ ] Types regenerated successfully
- [ ] Backend compiles without errors

### Frontend Complete (Phase 2)
- [ ] `CopyEventModal` component created
- [ ] Modal integrated with `AdminEventsPage`
- [ ] Mutation updated with parameters
- [ ] Test mocks added
- [ ] Frontend compiles without errors
- [ ] No TypeScript errors

### Ready for Testing (Phase 3)
- [ ] Backend implementation complete
- [ ] Frontend implementation complete
- [ ] All code compiles successfully
- [ ] Test plan reviewed and approved

### Feature Complete (All Phases)
- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] All E2E tests pass
- [ ] Manual testing complete
- [ ] Documentation updated
- [ ] Handoff documents created
- [ ] File registry updated
- [ ] No regressions found

---

## 8. Risk Assessment

### High Risks

#### Risk 1: Transaction Rollback Failure
**Impact**: Partial copy leaves orphaned data
**Mitigation**: Comprehensive transaction testing, error handling
**Likelihood**: Low

#### Risk 2: Session ID Remapping Errors
**Impact**: Ticket types/volunteer positions reference wrong sessions
**Mitigation**: Unit tests for ID mapping, integration tests verify relationships
**Likelihood**: Medium

### Medium Risks

#### Risk 3: Date Offset Calculation Errors
**Impact**: Sessions scheduled at wrong times
**Mitigation**: Unit tests for date arithmetic, manual verification
**Likelihood**: Low

#### Risk 4: Missing Validation
**Impact**: Invalid data causes runtime errors
**Mitigation**: DTO validation attributes, comprehensive validation tests
**Likelihood**: Low

### Low Risks

#### Risk 5: Navigation Route Incorrect
**Impact**: User can't edit copied event
**Mitigation**: Route verification task in Phase 2
**Likelihood**: Very Low

---

## 9. Dependencies and Constraints

### Technical Dependencies
- **.NET 9 API**: Backend must be .NET 9 compatible
- **Entity Framework Core**: For database operations
- **PostgreSQL**: Database backend
- **React 18**: Frontend framework
- **TanStack Query**: For mutations
- **Mantine v7**: UI components
- **Auto-Generated Types**: NSwag type generation

### Architectural Constraints
- **CSRF Protection**: All POST endpoints must validate tokens
- **Admin Authorization**: Only admins can copy events
- **Transaction Management**: Copy must be atomic
- **DTO Alignment**: Frontend types generated from backend

### Business Constraints
- **Draft Mode**: All copies start unpublished
- **No Attendance Data**: Attendance/purchases never copied
- **Same Venue**: Venue reference preserved (not copied)
- **Organizers Preserved**: Same teachers for copied event

---

## 10. Related Documentation

### Source Documents
- **Analysis Document**: [event-copy-feature-analysis-2025-11-26.md](/home/chad/repos/witchcityrope/docs/functional-areas/events/admin/event-copy-feature-analysis-2025-11-26.md)
- **Testing Plan**: [event-copy-testing-plan-2025-11-26.md](/home/chad/repos/witchcityrope/docs/functional-areas/events/admin/event-copy-testing-plan-2025-11-26.md)
- **Backend Data Model Analysis**: `/home/chad/repos/witchcityrope/test-results/event-copy-data-model-analysis-2025-11-26.md`
- **Frontend UI Analysis**: `/home/chad/repos/witchcityrope/session-work/2025-11-26/admin-events-ui-analysis-report.md`

### Reference Documents
- **Events Functional Area**: `/home/chad/repos/witchcityrope/docs/functional-areas/events/`
- **Functional Area Master Index**: `/home/chad/repos/witchcityrope/docs/architecture/functional-area-master-index.md`
- **CSRF Protection Guide**: `/home/chad/repos/witchcityrope/docs/functional-areas/security/csrf-protection/`
- **DTO Alignment Strategy**: `/home/chad/repos/witchcityrope/docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md`

### Agent Resources
- **Backend Developer Lessons**: `/home/chad/repos/witchcityrope/docs/lessons-learned/backend-developer-lessons-learned.md`
- **React Developer Lessons**: `/home/chad/repos/witchcityrope/docs/lessons-learned/react-developer-lessons-learned.md`
- **Test Developer Lessons**: `/home/chad/repos/witchcityrope/docs/lessons-learned/test-developer-lessons-learned.md`

---

**End of Implementation Plan**

**Next Action**: Assign backend-developer agent to implement Phase 1 (Backend) before proceeding with frontend modal.

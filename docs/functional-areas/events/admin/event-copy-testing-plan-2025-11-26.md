# Event Copy Feature - Testing Plan

**Date**: November 26, 2025
**Feature**: Event Copy with Modal Dialog
**Functional Area**: Events Management → Admin
**Work Type**: Feature Completion - Testing
**Source Analysis**: [event-copy-feature-analysis-2025-11-26.md](/home/chad/repos/witchcityrope/docs/functional-areas/events/admin/event-copy-feature-analysis-2025-11-26.md)
**Implementation Plan**: [event-copy-implementation-plan-2025-11-26.md](/home/chad/repos/witchcityrope/docs/functional-areas/events/admin/event-copy-implementation-plan-2025-11-26.md)

---

## 1. Testing Overview

### Testing Scope
This testing plan covers comprehensive test coverage for the event copy feature across all test levels:
- **Unit Tests** (Backend service logic)
- **Unit Tests** (Frontend React components)
- **Integration Tests** (API endpoint + database)
- **E2E Tests** (Complete user workflows)

### Test Types Covered
1. **Backend Unit Tests**: Service method logic, business rules, edge cases
2. **Frontend Unit Tests**: React component behavior, form validation, mutation handling
3. **Integration Tests**: End-to-end API testing with database operations
4. **E2E Tests**: Complete user workflows with browser automation

### Agent Assignment
- **test-developer**: Create all test files and test infrastructure
- **test-executor**: Execute tests and report results

### Testing Objectives
1. Verify all deep copy operations work correctly
2. Validate form input and business rules
3. Ensure transaction atomicity (all-or-nothing)
4. Confirm security (CSRF + authorization)
5. Test edge cases and error scenarios
6. Validate user experience workflows

---

## 2. Unit Tests - Backend

### Test File Location
**File**: `/home/chad/repos/witchcityrope/tests/unit/api/Features/Events/Services/EventServiceCopyTests.cs`

### Test Class Structure
```csharp
namespace WitchCityRope.Tests.Unit.Api.Features.Events.Services;

public class EventServiceCopyTests : IClassFixture<DatabaseFixture>
{
    private readonly ApplicationDbContext _context;
    private readonly IEventService _eventService;
    private readonly IMapper _mapper;

    public EventServiceCopyTests(DatabaseFixture fixture)
    {
        _context = fixture.Context;
        _mapper = fixture.Mapper;
        _eventService = new EventService(_context, _mapper, Mock.Of<ILogger<EventService>>());
    }

    // Tests go here
}
```

---

### Backend Test Cases

#### Test 1: CopyEventAsync_WithValidEvent_CopiesAllProperties
**Purpose**: Verify all event properties are copied correctly

**Arrange**:
```csharp
var originalEvent = new Event
{
    Id = Guid.NewGuid(),
    Title = "Original Event",
    StartDate = DateTimeOffset.UtcNow.AddDays(30),
    EndDate = DateTimeOffset.UtcNow.AddDays(30).AddHours(2),
    ShortDescription = "Short description",
    Description = "Full description",
    Policies = "Event policies",
    Capacity = 50,
    EventType = EventType.Workshop,
    VenueId = Guid.NewGuid(),
    IsPublished = true,
    RegistrationOpenHours = 72,
    RegistrationCloseHours = 1,
    CancellationOpenHours = 24,
    CancellationCloseHours = 1,
    VolunteerRegistrationCloseHours = 2,
    VolunteerCancellationCloseHours = 1
};
_context.Events.Add(originalEvent);
await _context.SaveChangesAsync();

var request = new CopyEventRequest
{
    NewTitle = "Copied Event",
    NewStartDate = DateTimeOffset.UtcNow.AddDays(60)
};
```

**Act**:
```csharp
var (success, response, error) = await _eventService.CopyEventAsync(
    originalEvent.Id.ToString(),
    request,
    CancellationToken.None);
```

**Assert**:
```csharp
Assert.True(success);
Assert.NotNull(response);
Assert.NotEqual(originalEvent.Id, response.Id);
Assert.Equal(request.NewTitle, response.Title);
Assert.Equal(request.NewStartDate, response.StartDate);
Assert.Equal(originalEvent.ShortDescription, response.ShortDescription);
Assert.Equal(originalEvent.Description, response.Description);
Assert.Equal(originalEvent.Policies, response.Policies);
Assert.Equal(originalEvent.Capacity, response.Capacity);
Assert.Equal(originalEvent.EventType, response.EventType);
Assert.Equal(originalEvent.VenueId, response.VenueId);
Assert.Equal(originalEvent.RegistrationOpenHours, response.RegistrationOpenHours);
Assert.Equal(originalEvent.RegistrationCloseHours, response.RegistrationCloseHours);
// ... verify all 6 timing fields
```

---

#### Test 2: CopyEventAsync_WithValidEvent_CreatesDraftEvent
**Purpose**: Verify copied event is unpublished (draft)

**Arrange**: Same as Test 1

**Act**: Same as Test 1

**Assert**:
```csharp
Assert.True(success);
Assert.False(response.IsPublished);
```

---

#### Test 3: CopyEventAsync_WithValidEvent_CopiesSessions
**Purpose**: Verify sessions are deep copied with date offset

**Arrange**:
```csharp
var originalEvent = CreateTestEvent();
var session1 = new Session
{
    Id = Guid.NewGuid(),
    EventId = originalEvent.Id,
    SessionCode = "Session A",
    Name = "Morning Session",
    StartTime = originalEvent.StartDate.AddHours(1),
    EndTime = originalEvent.StartDate.AddHours(3),
    Capacity = 20
};
var session2 = new Session
{
    Id = Guid.NewGuid(),
    EventId = originalEvent.Id,
    SessionCode = "Session B",
    Name = "Afternoon Session",
    StartTime = originalEvent.StartDate.AddHours(4),
    EndTime = originalEvent.StartDate.AddHours(6),
    Capacity = 20
};
originalEvent.Sessions.Add(session1);
originalEvent.Sessions.Add(session2);
_context.Events.Add(originalEvent);
await _context.SaveChangesAsync();

var request = new CopyEventRequest
{
    NewTitle = "Copied Event",
    NewStartDate = originalEvent.StartDate.AddDays(30)
};
```

**Act**: Call CopyEventAsync

**Assert**:
```csharp
Assert.True(success);
Assert.Equal(2, response.Sessions.Count);

var copiedSession1 = response.Sessions.First(s => s.SessionCode == "Session A");
Assert.NotEqual(session1.Id, copiedSession1.Id);
Assert.Equal(session1.Name, copiedSession1.Name);
Assert.Equal(session1.Capacity, copiedSession1.Capacity);

// Verify date offset applied
var expectedStartTime = session1.StartTime.AddDays(30);
Assert.Equal(expectedStartTime, copiedSession1.StartTime);
```

---

#### Test 4: CopyEventAsync_WithValidEvent_CopiesTicketTypes
**Purpose**: Verify ticket types are deep copied with session ID remapping

**Arrange**:
```csharp
var originalEvent = CreateTestEventWithSessions();
var session = originalEvent.Sessions.First();

var ticketType1 = new TicketType
{
    Id = Guid.NewGuid(),
    EventId = originalEvent.Id,
    SessionId = session.Id,  // Session-specific ticket
    Name = "Early Bird",
    Description = "Early registration discount",
    PricingType = PricingType.Fixed,
    Price = 25.00m,
    Available = 50
};
var ticketType2 = new TicketType
{
    Id = Guid.NewGuid(),
    EventId = originalEvent.Id,
    SessionId = null,  // General admission ticket
    Name = "General Admission",
    PricingType = PricingType.Fixed,
    Price = 30.00m,
    Available = 100
};
originalEvent.TicketTypes.Add(ticketType1);
originalEvent.TicketTypes.Add(ticketType2);
await _context.SaveChangesAsync();
```

**Act**: Call CopyEventAsync

**Assert**:
```csharp
Assert.True(success);
Assert.Equal(2, response.TicketTypes.Count);

var copiedTicket1 = response.TicketTypes.First(t => t.Name == "Early Bird");
Assert.NotEqual(ticketType1.Id, copiedTicket1.Id);
Assert.NotEqual(session.Id, copiedTicket1.SessionId);  // REMAPPED
Assert.NotNull(copiedTicket1.SessionId);
Assert.Equal(ticketType1.Name, copiedTicket1.Name);
Assert.Equal(ticketType1.Price, copiedTicket1.Price);
Assert.Equal(ticketType1.Available, copiedTicket1.Available);

var copiedTicket2 = response.TicketTypes.First(t => t.Name == "General Admission");
Assert.Null(copiedTicket2.SessionId);  // Remains null
```

---

#### Test 5: CopyEventAsync_WithValidEvent_CopiesVolunteerPositions
**Purpose**: Verify volunteer positions copied with session remapping and SlotsFilled reset

**Arrange**:
```csharp
var originalEvent = CreateTestEventWithSessions();
var session = originalEvent.Sessions.First();

var position1 = new VolunteerPosition
{
    Id = Guid.NewGuid(),
    EventId = originalEvent.Id,
    SessionId = session.Id,
    Title = "Setup Crew",
    Description = "Help with setup",
    SlotsNeeded = 5,
    SlotsFilled = 3,  // Already partially filled
    IsPublicFacing = true
};
var position2 = new VolunteerPosition
{
    Id = Guid.NewGuid(),
    EventId = originalEvent.Id,
    SessionId = null,  // Event-level volunteer
    Title = "Registration Desk",
    SlotsNeeded = 2,
    SlotsFilled = 2,  // Fully filled
    IsPublicFacing = true
};
originalEvent.VolunteerPositions.Add(position1);
originalEvent.VolunteerPositions.Add(position2);
await _context.SaveChangesAsync();
```

**Act**: Call CopyEventAsync

**Assert**:
```csharp
Assert.True(success);
Assert.Equal(2, response.VolunteerPositions.Count);

var copiedPosition1 = response.VolunteerPositions.First(p => p.Title == "Setup Crew");
Assert.NotEqual(position1.Id, copiedPosition1.Id);
Assert.NotEqual(session.Id, copiedPosition1.SessionId);  // REMAPPED
Assert.NotNull(copiedPosition1.SessionId);
Assert.Equal(position1.SlotsNeeded, copiedPosition1.SlotsNeeded);
Assert.Equal(0, copiedPosition1.SlotsFilled);  // RESET

var copiedPosition2 = response.VolunteerPositions.First(p => p.Title == "Registration Desk");
Assert.Null(copiedPosition2.SessionId);
Assert.Equal(0, copiedPosition2.SlotsFilled);  // RESET
```

---

#### Test 6: CopyEventAsync_WithValidEvent_CopiesOrganizers
**Purpose**: Verify organizer references are copied (same users)

**Arrange**:
```csharp
var originalEvent = CreateTestEvent();
var organizer1 = new ApplicationUser { Id = Guid.NewGuid(), UserName = "teacher1@example.com" };
var organizer2 = new ApplicationUser { Id = Guid.NewGuid(), UserName = "teacher2@example.com" };
originalEvent.Organizers.Add(organizer1);
originalEvent.Organizers.Add(organizer2);
_context.Users.AddRange(organizer1, organizer2);
_context.Events.Add(originalEvent);
await _context.SaveChangesAsync();
```

**Act**: Call CopyEventAsync

**Assert**:
```csharp
Assert.True(success);
Assert.Equal(2, response.Organizers.Count);
Assert.Contains(response.Organizers, o => o.Id == organizer1.Id);
Assert.Contains(response.Organizers, o => o.Id == organizer2.Id);
```

---

#### Test 7: CopyEventAsync_WithValidEvent_ExcludesAttendanceData
**Purpose**: Verify attendance and transaction data NOT copied

**Arrange**:
```csharp
var originalEvent = CreateTestEventWithSessions();
var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = "user@example.com" };

// Add attendance records
var attendance = new EventAttendance
{
    EventId = originalEvent.Id,
    UserId = user.Id,
    Status = AttendanceStatus.Confirmed
};

// Add ticket purchases
var ticketPurchase = new TicketPurchase
{
    EventId = originalEvent.Id,
    UserId = user.Id,
    TicketTypeId = originalEvent.TicketTypes.First().Id,
    Quantity = 1,
    TotalAmount = 25.00m
};

_context.EventAttendances.Add(attendance);
_context.TicketPurchases.Add(ticketPurchase);
await _context.SaveChangesAsync();
```

**Act**: Call CopyEventAsync

**Assert**:
```csharp
Assert.True(success);

// Verify NO attendance records copied
var copiedEventAttendances = await _context.EventAttendances
    .Where(a => a.EventId == response.Id)
    .ToListAsync();
Assert.Empty(copiedEventAttendances);

// Verify NO ticket purchases copied
var copiedPurchases = await _context.TicketPurchases
    .Where(p => p.EventId == response.Id)
    .ToListAsync();
Assert.Empty(copiedPurchases);
```

---

#### Test 8: CopyEventAsync_WithCustomEmailTemplates_CopiesTemplates
**Purpose**: Verify custom email templates are copied with new IDs

**Arrange**:
```csharp
var originalEvent = CreateTestEvent();

// Create 2 custom email templates for the event
var template1 = new EventEmailTemplate
{
    Id = Guid.NewGuid(),
    EventId = originalEvent.Id,
    Subject = "Welcome to Event",
    HtmlBody = "<p>Welcome!</p>",
    PlainTextBody = "Welcome!",
    TargetSessions = "Session A",
    RecipientGroup = "Attendees",
    GlobalTemplateId = Guid.NewGuid()
};

var template2 = new EventEmailTemplate
{
    Id = Guid.NewGuid(),
    EventId = originalEvent.Id,
    Subject = "Event Reminder",
    HtmlBody = "<p>Reminder!</p>",
    PlainTextBody = "Reminder!",
    TargetSessions = null,
    RecipientGroup = "All",
    GlobalTemplateId = Guid.NewGuid()
};

originalEvent.EventEmailTemplates.Add(template1);
originalEvent.EventEmailTemplates.Add(template2);
_context.Events.Add(originalEvent);
await _context.SaveChangesAsync();

var request = new CopyEventRequest
{
    NewTitle = "Copied Event",
    NewStartDate = originalEvent.StartDate.AddDays(30)
};
```

**Act**: Call CopyEventAsync with new date/title

**Assert**:
```csharp
Assert.True(success);
Assert.NotNull(response);
Assert.Equal(2, response.EventEmailTemplates.Count);

// Verify template 1 copied correctly
var copiedTemplate1 = response.EventEmailTemplates
    .First(t => t.Subject == "Welcome to Event");
Assert.NotEqual(template1.Id, copiedTemplate1.Id);  // New ID
Assert.Equal(template1.Subject, copiedTemplate1.Subject);
Assert.Equal(template1.HtmlBody, copiedTemplate1.HtmlBody);
Assert.Equal(template1.PlainTextBody, copiedTemplate1.PlainTextBody);
Assert.Equal(template1.TargetSessions, copiedTemplate1.TargetSessions);
Assert.Equal(template1.RecipientGroup, copiedTemplate1.RecipientGroup);
Assert.Equal(template1.GlobalTemplateId, copiedTemplate1.GlobalTemplateId);  // Preserved
Assert.Equal(response.Id, copiedTemplate1.EventId);  // Points to new event

// Verify template 2 copied correctly
var copiedTemplate2 = response.EventEmailTemplates
    .First(t => t.Subject == "Event Reminder");
Assert.NotEqual(template2.Id, copiedTemplate2.Id);  // New ID
Assert.Equal(template2.Subject, copiedTemplate2.Subject);
Assert.Equal(response.Id, copiedTemplate2.EventId);  // Points to new event
```

---

#### Test 9: CopyEventAsync_WithoutCustomEmailTemplates_CopiesSuccessfully
**Purpose**: Verify copy succeeds when event has no custom templates

**Arrange**:
```csharp
var originalEvent = CreateTestEvent();  // No custom templates
_context.Events.Add(originalEvent);
await _context.SaveChangesAsync();

var request = new CopyEventRequest
{
    NewTitle = "Copied Event",
    NewStartDate = originalEvent.StartDate.AddDays(30)
};
```

**Act**: Call CopyEventAsync

**Assert**:
```csharp
Assert.True(success);
Assert.NotNull(response);

// Verify copy succeeds
Assert.Equal(request.NewTitle, response.Title);

// Verify no templates (or empty collection)
Assert.True(response.EventEmailTemplates == null ||
            response.EventEmailTemplates.Count == 0);
```

---

#### Test 10: CopyEventAsync_WithInvalidEventId_ReturnsError
**Purpose**: Verify proper error handling for non-existent event

**Arrange**:
```csharp
var nonExistentId = Guid.NewGuid().ToString();
var request = new CopyEventRequest
{
    NewTitle = "Test",
    NewStartDate = DateTimeOffset.UtcNow.AddDays(30)
};
```

**Act**:
```csharp
var (success, response, error) = await _eventService.CopyEventAsync(
    nonExistentId,
    request,
    CancellationToken.None);
```

**Assert**:
```csharp
Assert.False(success);
Assert.Null(response);
Assert.Equal("Event not found", error);
```

---

#### Test 11: CopyEventAsync_WithDatabaseError_HandlesGracefully
**Purpose**: Verify transaction rollback on errors

**Arrange**:
```csharp
// Create event with invalid foreign key reference that will fail on save
var originalEvent = CreateTestEvent();
_context.Events.Add(originalEvent);
await _context.SaveChangesAsync();

// Mock a database constraint violation
// (Implementation depends on testing framework - may need to use TestContainers)
```

**Act**: Call CopyEventAsync (expect exception)

**Assert**:
```csharp
Assert.False(success);
Assert.NotNull(error);

// Verify transaction rolled back - NO partial data in database
var copiedEvents = await _context.Events
    .Where(e => e.Title.StartsWith("Copied"))
    .ToListAsync();
Assert.Empty(copiedEvents);
```

---

## 3. Unit Tests - Frontend

### Test File Location
**File**: `/home/chad/repos/witchcityrope/tests/unit/web/components/events/CopyEventModal.test.tsx`

### Test Setup
```typescript
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MantineProvider } from '@mantine/core';
import { CopyEventModal } from '@/components/events/CopyEventModal';
import { vi } from 'vitest';

const createWrapper = () => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

  return ({ children }: { children: React.ReactNode }) => (
    <QueryClientProvider client={queryClient}>
      <MantineProvider>
        {children}
      </MantineProvider>
    </QueryClientProvider>
  );
};
```

---

### Frontend Test Cases

#### Test 1: renders modal when opened
**Purpose**: Verify modal displays when opened prop is true

**Test**:
```typescript
test('renders modal when opened', () => {
  render(
    <CopyEventModal
      opened={true}
      onClose={vi.fn()}
      eventToCopy={{ id: 'event-1', title: 'Test Event' }}
    />,
    { wrapper: createWrapper() }
  );

  expect(screen.getByText('Copy Event')).toBeInTheDocument();
  expect(screen.getByLabelText('New Event Date')).toBeInTheDocument();
  expect(screen.getByLabelText('New Event Title')).toBeInTheDocument();
  expect(screen.getByRole('button', { name: /cancel/i })).toBeInTheDocument();
  expect(screen.getByRole('button', { name: /copy event/i })).toBeInTheDocument();
});
```

---

#### Test 2: pre-fills title with original title plus (Copy)
**Purpose**: Verify title input is pre-filled correctly

**Test**:
```typescript
test('pre-fills title with original title plus (Copy)', () => {
  render(
    <CopyEventModal
      opened={true}
      onClose={vi.fn()}
      eventToCopy={{ id: 'event-1', title: 'Spring Workshop' }}
    />,
    { wrapper: createWrapper() }
  );

  const titleInput = screen.getByLabelText('New Event Title') as HTMLInputElement;
  expect(titleInput.value).toBe('Spring Workshop (Copy)');
});
```

---

#### Test 3: validates date is not in past
**Purpose**: Verify past date validation

**Test**:
```typescript
test('validates date is not in past', async () => {
  render(
    <CopyEventModal
      opened={true}
      onClose={vi.fn()}
      eventToCopy={{ id: 'event-1', title: 'Test Event' }}
    />,
    { wrapper: createWrapper() }
  );

  const dateInput = screen.getByLabelText('New Event Date');
  const pastDate = new Date('2020-01-01');

  fireEvent.change(dateInput, { target: { value: pastDate.toISOString() } });

  const submitButton = screen.getByRole('button', { name: /copy event/i });
  fireEvent.click(submitButton);

  await waitFor(() => {
    expect(screen.getByText('Event date must be in the future')).toBeInTheDocument();
  });
});
```

---

#### Test 4: validates title is required
**Purpose**: Verify required title validation

**Test**:
```typescript
test('validates title is required', async () => {
  render(
    <CopyEventModal
      opened={true}
      onClose={vi.fn()}
      eventToCopy={{ id: 'event-1', title: 'Test Event' }}
    />,
    { wrapper: createWrapper() }
  );

  const titleInput = screen.getByLabelText('New Event Title');
  fireEvent.change(titleInput, { target: { value: '' } });

  const submitButton = screen.getByRole('button', { name: /copy event/i });
  fireEvent.click(submitButton);

  await waitFor(() => {
    expect(screen.getByText('Title is required')).toBeInTheDocument();
  });
});
```

---

#### Test 5: calls mutation on valid submit
**Purpose**: Verify mutation is called with correct parameters

**Test**:
```typescript
test('calls mutation on valid submit', async () => {
  const mockMutate = vi.fn();
  vi.mock('../../features/events/api/mutations', () => ({
    useCopyEvent: () => ({
      mutateAsync: mockMutate,
      isPending: false,
    }),
  }));

  render(
    <CopyEventModal
      opened={true}
      onClose={vi.fn()}
      eventToCopy={{ id: 'event-1', title: 'Test Event' }}
    />,
    { wrapper: createWrapper() }
  );

  const dateInput = screen.getByLabelText('New Event Date');
  const titleInput = screen.getByLabelText('New Event Title');

  const futureDate = new Date();
  futureDate.setDate(futureDate.getDate() + 30);

  fireEvent.change(dateInput, { target: { value: futureDate.toISOString() } });
  fireEvent.change(titleInput, { target: { value: 'Copied Event' } });

  const submitButton = screen.getByRole('button', { name: /copy event/i });
  fireEvent.click(submitButton);

  await waitFor(() => {
    expect(mockMutate).toHaveBeenCalledWith({
      eventId: 'event-1',
      newStartDate: expect.any(String),
      newTitle: 'Copied Event',
    });
  });
});
```

---

#### Test 6: shows loading state during mutation
**Purpose**: Verify loading indicators display

**Test**:
```typescript
test('shows loading state during mutation', async () => {
  const mockMutate = vi.fn(() => new Promise(() => {})); // Never resolves

  vi.mock('../../features/events/api/mutations', () => ({
    useCopyEvent: () => ({
      mutateAsync: mockMutate,
      isPending: true,
    }),
  }));

  render(
    <CopyEventModal
      opened={true}
      onClose={vi.fn()}
      eventToCopy={{ id: 'event-1', title: 'Test Event' }}
    />,
    { wrapper: createWrapper() }
  );

  const submitButton = screen.getByRole('button', { name: /copy event/i });
  expect(submitButton).toHaveAttribute('data-loading', 'true');

  const cancelButton = screen.getByRole('button', { name: /cancel/i });
  expect(cancelButton).toBeDisabled();
});
```

---

#### Test 7: closes modal on successful copy
**Purpose**: Verify modal closes after successful mutation

**Test**:
```typescript
test('closes modal on successful copy', async () => {
  const mockOnClose = vi.fn();
  const mockMutate = vi.fn(() =>
    Promise.resolve({ id: 'copied-event-1', title: 'Copied Event' })
  );

  vi.mock('../../features/events/api/mutations', () => ({
    useCopyEvent: () => ({
      mutateAsync: mockMutate,
      isPending: false,
    }),
  }));

  render(
    <CopyEventModal
      opened={true}
      onClose={mockOnClose}
      eventToCopy={{ id: 'event-1', title: 'Test Event' }}
    />,
    { wrapper: createWrapper() }
  );

  const submitButton = screen.getByRole('button', { name: /copy event/i });
  fireEvent.click(submitButton);

  await waitFor(() => {
    expect(mockOnClose).toHaveBeenCalled();
  });
});
```

---

#### Test 8: shows error message on mutation failure
**Purpose**: Verify error notification displays on failure

**Test**:
```typescript
test('shows error message on mutation failure', async () => {
  const mockMutate = vi.fn(() => Promise.reject(new Error('API Error')));

  vi.mock('../../features/events/api/mutations', () => ({
    useCopyEvent: () => ({
      mutateAsync: mockMutate,
      isPending: false,
    }),
  }));

  render(
    <CopyEventModal
      opened={true}
      onClose={vi.fn()}
      eventToCopy={{ id: 'event-1', title: 'Test Event' }}
    />,
    { wrapper: createWrapper() }
  );

  const submitButton = screen.getByRole('button', { name: /copy event/i });
  fireEvent.click(submitButton);

  await waitFor(() => {
    expect(screen.getByText(/unable to copy event/i)).toBeInTheDocument();
  });
});
```

---

## 4. Integration Tests

### Test File Location
**File**: `/home/chad/repos/witchcityrope/tests/integration/Events/EventCopyIntegrationTests.cs`

### Test Setup
```csharp
public class EventCopyIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly ApplicationDbContext _context;

    public EventCopyIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
        _context = factory.Services.GetRequiredService<ApplicationDbContext>();
    }
}
```

---

### Integration Test Cases

#### Test 1: CopyEvent_EndToEnd_CreatesNewEvent
**Purpose**: Verify complete copy operation with database

**Test**:
```csharp
[Fact]
public async Task CopyEvent_EndToEnd_CreatesNewEvent()
{
    // Arrange
    var adminToken = await GetAdminAuthToken();
    var originalEvent = CreateTestEventWithAllRelations();
    _context.Events.Add(originalEvent);
    await _context.SaveChangesAsync();

    var request = new CopyEventRequest
    {
        NewTitle = "Copied Event",
        NewStartDate = DateTimeOffset.UtcNow.AddDays(60)
    };

    _client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", adminToken);

    // Act
    var response = await _client.PostAsJsonAsync(
        $"/api/events/{originalEvent.Id}/copy",
        request);

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var copiedEvent = await response.Content.ReadFromJsonAsync<EventDto>();
    Assert.NotNull(copiedEvent);
    Assert.NotEqual(originalEvent.Id, copiedEvent.Id);
    Assert.Equal(request.NewTitle, copiedEvent.Title);
    Assert.Equal(request.NewStartDate, copiedEvent.StartDate);
    Assert.False(copiedEvent.IsPublished);

    // Verify in database
    var dbEvent = await _context.Events
        .Include(e => e.Sessions)
        .Include(e => e.TicketTypes)
        .Include(e => e.VolunteerPositions)
        .FirstAsync(e => e.Id == copiedEvent.Id);

    Assert.NotNull(dbEvent);
    Assert.Equal(originalEvent.Sessions.Count, dbEvent.Sessions.Count);
    Assert.Equal(originalEvent.TicketTypes.Count, dbEvent.TicketTypes.Count);
    Assert.Equal(originalEvent.VolunteerPositions.Count, dbEvent.VolunteerPositions.Count);
}
```

---

#### Test 2: CopyEvent_WithSessionRemapping_MapsTicketTypesCorrectly
**Purpose**: Verify session ID remapping works for ticket types

**Test**:
```csharp
[Fact]
public async Task CopyEvent_WithSessionRemapping_MapsTicketTypesCorrectly()
{
    // Arrange
    var adminToken = await GetAdminAuthToken();
    var originalEvent = CreateTestEvent();

    var session1 = new Session
    {
        Id = Guid.NewGuid(),
        EventId = originalEvent.Id,
        SessionCode = "A",
        StartTime = originalEvent.StartDate.AddHours(1),
        EndTime = originalEvent.StartDate.AddHours(3)
    };

    var ticketType1 = new TicketType
    {
        Id = Guid.NewGuid(),
        EventId = originalEvent.Id,
        SessionId = session1.Id,  // Session-specific
        Name = "Session A Ticket",
        PricingType = PricingType.Fixed,
        Price = 25.00m
    };

    originalEvent.Sessions.Add(session1);
    originalEvent.TicketTypes.Add(ticketType1);
    _context.Events.Add(originalEvent);
    await _context.SaveChangesAsync();

    var request = new CopyEventRequest
    {
        NewTitle = "Copied Event",
        NewStartDate = originalEvent.StartDate.AddDays(30)
    };

    _client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", adminToken);

    // Act
    var response = await _client.PostAsJsonAsync(
        $"/api/events/{originalEvent.Id}/copy",
        request);

    // Assert
    var copiedEvent = await response.Content.ReadFromJsonAsync<EventDto>();
    var dbEvent = await _context.Events
        .Include(e => e.Sessions)
        .Include(e => e.TicketTypes)
        .FirstAsync(e => e.Id == copiedEvent.Id);

    var copiedSession = dbEvent.Sessions.First(s => s.SessionCode == "A");
    var copiedTicket = dbEvent.TicketTypes.First(t => t.Name == "Session A Ticket");

    Assert.NotEqual(session1.Id, copiedSession.Id);
    Assert.NotEqual(ticketType1.Id, copiedTicket.Id);
    Assert.Equal(copiedSession.Id, copiedTicket.SessionId);  // REMAPPED CORRECTLY
}
```

---

#### Test 3: CopyEvent_WithVolunteerPositions_RemapsSessionsAndResetsFilled
**Purpose**: Verify volunteer positions copied correctly

**Test**:
```csharp
[Fact]
public async Task CopyEvent_WithVolunteerPositions_RemapsSessionsAndResetsFilled()
{
    // Arrange
    var adminToken = await GetAdminAuthToken();
    var originalEvent = CreateTestEventWithSessions();
    var session = originalEvent.Sessions.First();

    var position = new VolunteerPosition
    {
        Id = Guid.NewGuid(),
        EventId = originalEvent.Id,
        SessionId = session.Id,
        Title = "Setup Crew",
        SlotsNeeded = 5,
        SlotsFilled = 3  // Partially filled
    };

    originalEvent.VolunteerPositions.Add(position);
    await _context.SaveChangesAsync();

    var request = new CopyEventRequest
    {
        NewTitle = "Copied Event",
        NewStartDate = originalEvent.StartDate.AddDays(30)
    };

    _client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", adminToken);

    // Act
    var response = await _client.PostAsJsonAsync(
        $"/api/events/{originalEvent.Id}/copy",
        request);

    // Assert
    var copiedEvent = await response.Content.ReadFromJsonAsync<EventDto>();
    var dbEvent = await _context.Events
        .Include(e => e.Sessions)
        .Include(e => e.VolunteerPositions)
        .FirstAsync(e => e.Id == copiedEvent.Id);

    var copiedSession = dbEvent.Sessions.First(s => s.SessionCode == session.SessionCode);
    var copiedPosition = dbEvent.VolunteerPositions.First(p => p.Title == "Setup Crew");

    Assert.NotEqual(session.Id, copiedSession.Id);
    Assert.NotEqual(position.Id, copiedPosition.Id);
    Assert.Equal(copiedSession.Id, copiedPosition.SessionId);  // REMAPPED
    Assert.Equal(0, copiedPosition.SlotsFilled);  // RESET
}
```

---

#### Test 4: CopyEvent_WithoutCsrfToken_Returns400
**Purpose**: Verify CSRF protection

**Test**:
```csharp
[Fact]
public async Task CopyEvent_WithoutCsrfToken_Returns400()
{
    // Arrange
    var adminToken = await GetAdminAuthToken();
    var originalEvent = CreateTestEvent();
    _context.Events.Add(originalEvent);
    await _context.SaveChangesAsync();

    var request = new CopyEventRequest
    {
        NewTitle = "Copied Event",
        NewStartDate = DateTimeOffset.UtcNow.AddDays(30)
    };

    _client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", adminToken);

    // DO NOT add CSRF token

    // Act
    var response = await _client.PostAsJsonAsync(
        $"/api/events/{originalEvent.Id}/copy",
        request);

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
}
```

---

#### Test 5: CopyEvent_WithoutAuthorization_Returns401
**Purpose**: Verify authorization requirement

**Test**:
```csharp
[Fact]
public async Task CopyEvent_WithoutAuthorization_Returns401()
{
    // Arrange
    var originalEvent = CreateTestEvent();
    _context.Events.Add(originalEvent);
    await _context.SaveChangesAsync();

    var request = new CopyEventRequest
    {
        NewTitle = "Copied Event",
        NewStartDate = DateTimeOffset.UtcNow.AddDays(30)
    };

    // DO NOT add authorization header

    // Act
    var response = await _client.PostAsJsonAsync(
        $"/api/events/{originalEvent.Id}/copy",
        request);

    // Assert
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
}
```

---

#### Test 6: CopyEvent_WithInvalidEventId_Returns404
**Purpose**: Verify 404 for non-existent event

**Test**:
```csharp
[Fact]
public async Task CopyEvent_WithInvalidEventId_Returns404()
{
    // Arrange
    var adminToken = await GetAdminAuthToken();
    var nonExistentId = Guid.NewGuid();

    var request = new CopyEventRequest
    {
        NewTitle = "Copied Event",
        NewStartDate = DateTimeOffset.UtcNow.AddDays(30)
    };

    _client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", adminToken);

    // Act
    var response = await _client.PostAsJsonAsync(
        $"/api/events/{nonExistentId}/copy",
        request);

    // Assert
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
}
```

---

#### Test 7: CopyEvent_WithDatabaseTransaction_RollsBackOnError
**Purpose**: Verify transaction atomicity

**Test**:
```csharp
[Fact]
public async Task CopyEvent_WithDatabaseTransaction_RollsBackOnError()
{
    // Arrange
    var adminToken = await GetAdminAuthToken();

    // Create event with constraint that will violate on copy
    // (Implementation depends on ability to simulate database errors)
    var originalEvent = CreateTestEvent();
    _context.Events.Add(originalEvent);
    await _context.SaveChangesAsync();

    // Simulate database error during copy operation
    // (May require mocking or test-specific configuration)

    // Act
    // Attempt copy that will fail

    // Assert
    // Verify NO partial data exists in database
    var copiedEvents = await _context.Events
        .Where(e => e.Title.StartsWith("Copied"))
        .ToListAsync();
    Assert.Empty(copiedEvents);
}
```

---

#### Test 8: CopyEvent_WithCustomEmailTemplates_CreatesNewTemplateRecords
**Purpose**: Verify email templates are copied with new IDs

**Arrange**:
```csharp
// Create event via API with custom email templates
var adminToken = await GetAdminAuthToken();
var originalEvent = CreateTestEvent();

var template1 = new EventEmailTemplate
{
    Id = Guid.NewGuid(),
    EventId = originalEvent.Id,
    Subject = "Welcome Email",
    HtmlBody = "<p>Welcome</p>",
    PlainTextBody = "Welcome",
    TargetSessions = "Session A",
    RecipientGroup = "Attendees",
    GlobalTemplateId = Guid.NewGuid()
};

originalEvent.EventEmailTemplates.Add(template1);
_context.Events.Add(originalEvent);
await _context.SaveChangesAsync();

var request = new CopyEventRequest
{
    NewTitle = "Copied Event",
    NewStartDate = originalEvent.StartDate.AddDays(30)
};

_client.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", adminToken);
```

**Act**: POST /api/events/{id}/copy with valid request

**Assert**:
```csharp
// 200 OK with copied event
Assert.Equal(HttpStatusCode.OK, response.StatusCode);

var copiedEvent = await response.Content.ReadFromJsonAsync<EventDto>();

// Database query shows new EventEmailTemplate records
var dbTemplates = await _context.EventEmailTemplates
    .Where(t => t.EventId == copiedEvent.Id)
    .ToListAsync();

Assert.Single(dbTemplates);

var copiedTemplate = dbTemplates.First();

// New templates have different IDs from source
Assert.NotEqual(template1.Id, copiedTemplate.Id);

// New templates.EventId == copiedEvent.Id
Assert.Equal(copiedEvent.Id, copiedTemplate.EventId);

// Original templates unchanged
var originalTemplate = await _context.EventEmailTemplates
    .FirstAsync(t => t.Id == template1.Id);
Assert.Equal(originalEvent.Id, originalTemplate.EventId);

// Template content (Subject, Body) matches source
Assert.Equal(template1.Subject, copiedTemplate.Subject);
Assert.Equal(template1.HtmlBody, copiedTemplate.HtmlBody);
Assert.Equal(template1.GlobalTemplateId, copiedTemplate.GlobalTemplateId);
```

---

## 5. End-to-End (E2E) Tests

### Test File Location
**File**: `/home/chad/repos/witchcityrope/tests/e2e/admin/event-copy.spec.ts`

### Test Setup
```typescript
import { test, expect } from '@playwright/test';

test.beforeEach(async ({ page }) => {
  // Login as admin
  await page.goto('/login');
  await page.fill('input[name="email"]', 'admin@witchcityrope.com');
  await page.fill('input[name="password"]', 'Test123!');
  await page.click('button[type="submit"]');
  await expect(page).toHaveURL('/dashboard');

  // Navigate to admin events
  await page.goto('/admin/events');
});
```

---

### E2E Test Cases

#### Test 1: Admin can copy event with new date and title
**Purpose**: Complete workflow test

**Test**:
```typescript
test('Admin can copy event with new date and title', async ({ page }) => {
  // Find first event in table
  const firstEventRow = page.locator('tbody tr').first();
  const eventTitle = await firstEventRow.locator('td').nth(2).textContent();

  // Click Copy button
  await firstEventRow.locator('button[data-testid="button-copy-event"]').click();

  // Verify modal opens
  await expect(page.locator('h3:text("Copy Event")')).toBeVisible();

  // Verify title pre-filled
  const titleInput = page.locator('input[data-testid="input-event-title"]');
  await expect(titleInput).toHaveValue(`${eventTitle} (Copy)`);

  // Enter new date (30 days from now)
  const futureDate = new Date();
  futureDate.setDate(futureDate.getDate() + 30);
  const dateInput = page.locator('input[data-testid="input-event-date"]');
  await dateInput.fill(futureDate.toISOString().split('T')[0]);

  // Edit title if desired
  await titleInput.fill('Spring Workshop 2025');

  // Click Copy Event button
  await page.click('button:text("Copy Event")');

  // Verify success notification
  await expect(page.locator('text=Event copied successfully')).toBeVisible();

  // Verify navigation to edit page
  await expect(page).toHaveURL(/\/admin\/events\/[a-f0-9-]+/);

  // Verify event details loaded
  const titleField = page.locator('input[name="title"]');
  await expect(titleField).toHaveValue('Spring Workshop 2025');
});
```

---

#### Test 2: Copy modal validates past dates
**Purpose**: Verify date validation

**Test**:
```typescript
test('Copy modal validates past dates', async ({ page }) => {
  // Click Copy on first event
  await page.locator('tbody tr').first()
    .locator('button[data-testid="button-copy-event"]').click();

  // Enter past date
  const pastDate = new Date('2020-01-01');
  const dateInput = page.locator('input[data-testid="input-event-date"]');
  await dateInput.fill(pastDate.toISOString().split('T')[0]);

  // Try to submit
  await page.click('button:text("Copy Event")');

  // Verify validation error
  await expect(page.locator('text=Event date must be in the future')).toBeVisible();

  // Verify modal remains open
  await expect(page.locator('h3:text("Copy Event")')).toBeVisible();
});
```

---

#### Test 3: Copy modal validates required title
**Purpose**: Verify title validation

**Test**:
```typescript
test('Copy modal validates required title', async ({ page }) => {
  // Click Copy on first event
  await page.locator('tbody tr').first()
    .locator('button[data-testid="button-copy-event"]').click();

  // Clear title field
  const titleInput = page.locator('input[data-testid="input-event-title"]');
  await titleInput.clear();

  // Try to submit
  await page.click('button:text("Copy Event")');

  // Verify validation error
  await expect(page.locator('text=Title is required')).toBeVisible();

  // Verify modal remains open
  await expect(page.locator('h3:text("Copy Event")')).toBeVisible();
});
```

---

#### Test 4: Copied event has correct sessions
**Purpose**: Verify sessions copied with offset dates

**Test**:
```typescript
test('Copied event has correct sessions', async ({ page }) => {
  // Find event with sessions
  await page.goto('/admin/events');
  const eventWithSessions = page.locator('tbody tr')
    .filter({ hasText: 'Workshop' }).first();

  // Copy event
  await eventWithSessions.locator('button[data-testid="button-copy-event"]').click();

  const futureDate = new Date();
  futureDate.setDate(futureDate.getDate() + 30);
  await page.locator('input[data-testid="input-event-date"]')
    .fill(futureDate.toISOString().split('T')[0]);

  await page.click('button:text("Copy Event")');

  // Wait for navigation to edit page
  await page.waitForURL(/\/admin\/events\/[a-f0-9-]+/);

  // Navigate to sessions tab/section
  await page.click('text=Sessions');

  // Verify sessions present
  const sessionRows = page.locator('[data-testid="session-row"]');
  await expect(sessionRows).not.toHaveCount(0);

  // Verify session dates are offset
  // (Implementation depends on UI structure for session dates)
});
```

---

#### Test 5: Copied event has correct ticket types
**Purpose**: Verify ticket types copied with session mappings

**Test**:
```typescript
test('Copied event has correct ticket types', async ({ page }) => {
  // Copy event with tickets
  await page.locator('tbody tr').first()
    .locator('button[data-testid="button-copy-event"]').click();

  const futureDate = new Date();
  futureDate.setDate(futureDate.getDate() + 30);
  await page.locator('input[data-testid="input-event-date"]')
    .fill(futureDate.toISOString().split('T')[0]);

  await page.click('button:text("Copy Event")');
  await page.waitForURL(/\/admin\/events\/[a-f0-9-]+/);

  // Navigate to tickets section
  await page.click('text=Tickets');

  // Verify ticket types present
  const ticketRows = page.locator('[data-testid="ticket-row"]');
  await expect(ticketRows).not.toHaveCount(0);

  // Verify correct session mappings
  // (Verify session-specific tickets point to new session IDs)
});
```

---

#### Test 6: Copied event excludes attendance data
**Purpose**: Verify attendance/purchases NOT copied

**Test**:
```typescript
test('Copied event excludes attendance data', async ({ page }) => {
  // Create event with RSVPs and purchases
  // (Requires test data setup or existing event with attendance)

  // Copy event
  await page.locator('tbody tr')
    .filter({ hasText: 'has attendees' }).first()
    .locator('button[data-testid="button-copy-event"]').click();

  const futureDate = new Date();
  futureDate.setDate(futureDate.getDate() + 30);
  await page.locator('input[data-testid="input-event-date"]')
    .fill(futureDate.toISOString().split('T')[0]);

  await page.click('button:text("Copy Event")');
  await page.waitForURL(/\/admin\/events\/[a-f0-9-]+/);

  // Navigate to attendees section
  await page.click('text=Attendees');

  // Verify 0 attendees
  await expect(page.locator('text=0 attendees')).toBeVisible();

  // Navigate to purchases section
  await page.click('text=Tickets Sold');

  // Verify 0 tickets sold
  await expect(page.locator('text=0 tickets sold')).toBeVisible();
});
```

---

#### Test 7: Copied event has custom email templates
**Purpose**: Verify email templates are copied

**Steps**:
```typescript
test('Copied event has custom email templates', async ({ page }) => {
  // Login as admin
  await page.goto('/login');
  await page.fill('input[name="email"]', 'admin@witchcityrope.com');
  await page.fill('input[name="password"]', 'Test123!');
  await page.click('button[type="submit"]');

  // Create event with custom email template
  // (Requires implementation or test setup)

  // Copy event via admin panel
  await page.goto('/admin/events');
  await page.locator('tbody tr').first()
    .locator('button[data-testid="button-copy-event"]').click();

  const futureDate = new Date();
  futureDate.setDate(futureDate.getDate() + 30);
  await page.locator('input[data-testid="input-event-date"]')
    .fill(futureDate.toISOString().split('T')[0]);

  await page.click('button:text("Copy Event")');
  await page.waitForURL(/\/admin\/events\/[a-f0-9-]+/);

  // Navigate to copied event email settings
  await page.click('text=Email Templates');  // or appropriate navigation
}
```

**Assert**:
```typescript
// Custom template visible in settings
await expect(page.locator('[data-testid="custom-template"]')).toBeVisible();

// Template subject matches original
const subject = await page.locator('[data-testid="template-subject"]').textContent();
// Verify matches original (requires knowing original subject)

// Template body content matches original
const body = await page.locator('[data-testid="template-body"]').textContent();
// Verify matches original

// Can edit copied template without affecting original
await page.click('[data-testid="edit-template"]');
await page.fill('[data-testid="template-subject"]', 'Modified Subject');
await page.click('[data-testid="save-template"]');

// Navigate back to original event and verify template unchanged
```

---

#### Test 8: Copied event without custom templates works correctly
**Purpose**: Verify copy works when no custom templates exist

**Steps**:
```typescript
test('Copied event without custom templates works correctly', async ({ page }) => {
  // Copy event that uses only global templates (no customizations)
  await page.goto('/admin/events');

  // Find event without custom templates
  const eventRow = page.locator('tbody tr')
    .filter({ hasText: 'no custom templates' }).first();

  await eventRow.locator('button[data-testid="button-copy-event"]').click();

  const futureDate = new Date();
  futureDate.setDate(futureDate.getDate() + 30);
  await page.locator('input[data-testid="input-event-date"]')
    .fill(futureDate.toISOString().split('T')[0]);

  await page.click('button:text("Copy Event")');
  await page.waitForURL(/\/admin\/events\/[a-f0-9-]+/);

  // Navigate to copied event email settings
  await page.click('text=Email Templates');
}
```

**Assert**:
```typescript
// Global templates available
await expect(page.locator('[data-testid="global-templates"]')).toBeVisible();

// No custom templates present
await expect(page.locator('[data-testid="custom-template"]')).not.toBeVisible();

// Can create new custom template for copied event
await page.click('[data-testid="create-custom-template"]');
await page.fill('[data-testid="template-subject"]', 'New Custom Template');
await page.click('[data-testid="save-template"]');

await expect(page.locator('[data-testid="custom-template"]')).toBeVisible();
```

---

#### Test 9: Copy modal can be cancelled
**Purpose**: Verify cancel functionality

**Test**:
```typescript
test('Copy modal can be cancelled', async ({ page }) => {
  // Click Copy
  await page.locator('tbody tr').first()
    .locator('button[data-testid="button-copy-event"]').click();

  // Verify modal opened
  await expect(page.locator('h3:text("Copy Event")')).toBeVisible();

  // Click Cancel
  await page.click('button:text("Cancel")');

  // Verify modal closed
  await expect(page.locator('h3:text("Copy Event")')).not.toBeVisible();

  // Verify still on admin events page
  await expect(page).toHaveURL('/admin/events');
});
```

---

#### Test 10: Copy handles API errors gracefully
**Purpose**: Verify error handling

**Test**:
```typescript
test('Copy handles API errors gracefully', async ({ page }) => {
  // Mock API error
  await page.route('**/api/events/*/copy', route => {
    route.fulfill({
      status: 500,
      body: JSON.stringify({ error: 'Internal Server Error' })
    });
  });

  // Click Copy
  await page.locator('tbody tr').first()
    .locator('button[data-testid="button-copy-event"]').click();

  // Fill form
  const futureDate = new Date();
  futureDate.setDate(futureDate.getDate() + 30);
  await page.locator('input[data-testid="input-event-date"]')
    .fill(futureDate.toISOString().split('T')[0]);

  // Submit
  await page.click('button:text("Copy Event")');

  // Verify error notification
  await expect(page.locator('text=Unable to copy event')).toBeVisible();

  // Verify modal remains open
  await expect(page.locator('h3:text("Copy Event")')).toBeVisible();
});
```

---

## 6. Test Execution Strategy

### Execution Order
1. **Unit Tests First**: Run backend and frontend unit tests (fastest feedback)
2. **Integration Tests Second**: Run after unit tests pass
3. **E2E Tests Last**: Run after integration tests pass (most comprehensive)

### Test Commands

#### Backend Unit Tests
```bash
cd /home/chad/repos/witchcityrope
dotnet test tests/unit/api/Features/Events/Services/EventServiceCopyTests.cs
```

#### Frontend Unit Tests
```bash
cd /home/chad/repos/witchcityrope/apps/web
npm test -- tests/unit/web/components/events/CopyEventModal.test.tsx
```

#### Integration Tests
```bash
cd /home/chad/repos/witchcityrope
dotnet test tests/integration/Events/EventCopyIntegrationTests.cs
```

#### E2E Tests
```bash
cd /home/chad/repos/witchcityrope
npx playwright test tests/e2e/admin/event-copy.spec.ts
```

### All Tests Must Pass
**Quality Gate**: All tests must pass before feature considered complete

---

## 7. Test Data Requirements

### Sample Event with Complex Structure

**Event Properties**:
- Title: "Advanced Rope Workshop"
- StartDate: 30 days from now
- EndDate: 32 days from now
- EventType: Workshop
- Capacity: 50
- 3 Sessions (Morning, Afternoon, Evening)
- 6 Timing control fields configured

**Sessions**:
1. **Session A** - Saturday 10am-12pm (Capacity: 20)
2. **Session B** - Saturday 2pm-4pm (Capacity: 20)
3. **Session C** - Saturday 7pm-9pm (Capacity: 15)

**Ticket Types**:
1. **Early Bird** - Fixed $25, General admission (no SessionId)
2. **Session A Ticket** - Fixed $30, Session-specific (SessionId = Session A)
3. **Session B Ticket** - Fixed $30, Session-specific (SessionId = Session B)
4. **Full Weekend** - Fixed $75, All sessions (no SessionId)
5. **Sliding Scale** - Sliding scale $20-$50, General admission

**Volunteer Positions**:
1. **Setup Crew** - 5 slots (Session A), 3 filled
2. **Registration Desk** - 2 slots (no SessionId), 2 filled
3. **Cleanup Crew** - 4 slots (Session C), 0 filled

**Organizers**:
- Teacher 1 (teacher1@witchcityrope.com)
- Teacher 2 (teacher2@witchcityrope.com)

**Attendance Data** (to verify exclusion):
- 10 RSVPs (EventAttendances)
- 5 Ticket Purchases

---

## 8. Manual Testing Checklist

**After all automated tests pass**:

### Basic Copy Operations
- [ ] Copy event with only basic data (no sessions/tickets)
- [ ] Copy event with complex session structure (3+ sessions)
- [ ] Copy event far in the future (verify date offset)
- [ ] Edit title in modal before copying
- [ ] Cancel modal without copying

### Multiple Copy Operations
- [ ] Copy multiple events in succession
- [ ] Copy same event twice (verify both copies independent)

### Draft Mode Verification
- [ ] Verify copied event is draft (unpublished)
- [ ] Verify original event unchanged
- [ ] Verify new event can be edited normally
- [ ] Verify new event can be published

### Data Integrity
- [ ] Verify sessions have correct dates
- [ ] Verify ticket types have correct session references
- [ ] Verify volunteer positions have correct session references
- [ ] Verify volunteer SlotsFilled reset to 0
- [ ] Verify organizers preserved
- [ ] Verify venue reference preserved

### Edge Cases
- [ ] Copy event with no sessions
- [ ] Copy event with no ticket types
- [ ] Copy event with no volunteer positions
- [ ] Copy event with all sessions in past (offset makes them future)
- [ ] Copy event with very long title (near 200 char limit)
- [ ] Copy event on same date (title differentiates)
- [ ] Copy event with multiple custom email templates (verify all copied)
- [ ] Copy event with no custom email templates (verify global templates used)
- [ ] Copy event with partially customized templates (some custom, some global)
- [ ] Verify editing copied template doesn't affect original template

---

## 9. Performance Considerations

### Performance Tests

#### Test 1: Copy Event with 50+ Sessions
**Purpose**: Stress test with large session count

**Setup**: Create event with 50+ sessions
**Action**: Copy event
**Acceptance**: Complete in <2 seconds

#### Test 2: Copy Event with 20+ Ticket Types
**Purpose**: Stress test with many ticket types

**Setup**: Create event with 20+ ticket types
**Action**: Copy event
**Acceptance**: Complete in <2 seconds

#### Test 3: Verify Transaction Performance
**Purpose**: Ensure transaction doesn't timeout

**Setup**: Create event with all possible related entities
**Action**: Copy event
**Acceptance**: Transaction commits in <2 seconds

---

## 10. Test Coverage Goals

### Coverage Targets

**Backend Service Method**: 90%+ coverage (including email template logic)
- All code paths tested
- All error scenarios covered
- All edge cases validated

**Frontend Modal Component**: 80%+ coverage
- All user interactions tested
- All validation scenarios covered
- All loading states verified

**Integration Tests**: All critical paths covered
- Happy path (successful copy)
- Error paths (404, 401, 400)
- Security paths (CSRF, authorization)

**E2E Tests**: All user workflows covered
- Complete copy workflow
- Validation error workflows
- Cancel workflow
- Error handling workflow

---

## 11. Test Catalog Updates

**After test execution**: Use test-catalog-updater skill

**Update Required**:
- Test execution results
- Pass/fail status
- Coverage data
- Execution time

**Location**: `/home/chad/repos/witchcityrope/docs/standards-processes/testing/TEST_CATALOG.md`

---

## 12. Edge Cases to Test

### Edge Case Checklist

#### Event Structure
- [ ] Copy event with no sessions
- [ ] Copy event with no ticket types
- [ ] Copy event with no volunteer positions
- [ ] Copy event with no organizers
- [ ] Copy event with all optional fields null

#### Date and Time
- [ ] Copy event with all sessions in past (offset makes them future)
- [ ] Copy event on same date as original
- [ ] Copy event with sessions spanning multiple days
- [ ] Copy event with sessions at midnight boundaries

#### Data Limits
- [ ] Copy event with very long title (near 200 char limit)
- [ ] Copy event with very long description (test large text fields)
- [ ] Copy event with maximum capacity value
- [ ] Copy event with 50+ sessions (stress test)

#### Related Entities
- [ ] Copy event with session-specific tickets
- [ ] Copy event with session-specific volunteers
- [ ] Copy event with sliding scale tickets
- [ ] Copy event with sold-out tickets (verify Available copied, Sold reset)

#### Business Rules
- [ ] Copy published event (verify copy is draft)
- [ ] Copy event with filled volunteer positions (verify SlotsFilled reset)
- [ ] Copy event with attendance data (verify excluded)
- [ ] Copy event with ticket purchases (verify excluded)

---

## 13. Documentation Requirements

### Test Documentation Checklist

After testing complete:
- [ ] Update TEST_CATALOG.md with all new tests
- [ ] Document test execution results
- [ ] Update test coverage metrics
- [ ] Create test handoff document
- [ ] Update file registry with test files

### Test Handoff Document

**Location**: `/home/chad/repos/witchcityrope/docs/functional-areas/events/admin/handoffs/test-developer-event-copy-YYYY-MM-DD-handoff.md`

**Required Content**:
1. Test files created (4 files)
2. Test coverage achieved (percentage per file)
3. Test execution results (pass/fail counts)
4. Issues found during testing
5. Known limitations or gaps
6. Recommendations for future tests

---

## 14. Success Metrics

### Test Quality Metrics

**Unit Test Quality**:
- [ ] All arrange/act/assert patterns followed
- [ ] Test names clearly describe what is tested
- [ ] Each test tests one thing
- [ ] Tests are independent (no shared state)
- [ ] Tests are repeatable

**Integration Test Quality**:
- [ ] Tests use real database
- [ ] Tests verify database state after operations
- [ ] Tests clean up data after execution
- [ ] Tests verify HTTP response codes
- [ ] Tests verify response body structure

**E2E Test Quality**:
- [ ] Tests follow user workflows
- [ ] Tests use data-testid selectors
- [ ] Tests verify UI state changes
- [ ] Tests verify notifications
- [ ] Tests verify navigation

### Coverage Quality

**Backend Coverage**: 90%+
- Service method: 100% line coverage
- All branches tested
- All error paths tested

**Frontend Coverage**: 80%+
- Component rendering: 100%
- Form validation: 100%
- Mutation handling: 100%

---

## 15. Related Documentation

### Source Documents
- **Analysis Document**: [event-copy-feature-analysis-2025-11-26.md](/home/chad/repos/witchcityrope/docs/functional-areas/events/admin/event-copy-feature-analysis-2025-11-26.md)
- **Implementation Plan**: [event-copy-implementation-plan-2025-11-26.md](/home/chad/repos/witchcityrope/docs/functional-areas/events/admin/event-copy-implementation-plan-2025-11-26.md)

### Testing Standards
- **Testing Guide**: `/home/chad/repos/witchcityrope/docs/standards-processes/testing/TESTING_GUIDE.md`
- **Test Catalog**: `/home/chad/repos/witchcityrope/docs/standards-processes/testing/TEST_CATALOG.md`
- **Integration Test Patterns**: `/home/chad/repos/witchcityrope/docs/standards-processes/testing/integration-test-patterns.md`

### Agent Resources
- **Test Developer Lessons**: `/home/chad/repos/witchcityrope/docs/lessons-learned/test-developer-lessons-learned.md`
- **Test Executor Lessons**: `/home/chad/repos/witchcityrope/docs/lessons-learned/test-executor-lessons-learned.md`

---

**End of Testing Plan**

**Next Action**: Assign test-developer agent to create test suite after backend implementation complete.

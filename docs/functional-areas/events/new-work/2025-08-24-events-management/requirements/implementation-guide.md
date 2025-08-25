# Events Management System - Comprehensive Implementation Guide

<!-- Last Updated: 2025-08-25 -->
<!-- Version: 1.0 -->
<!-- Owner: Backend Developer Agent -->
<!-- Status: Ready for Implementation -->

## 🚨 CRITICAL: SOURCE OF TRUTH

**WIREFRAMES ARE THE SOURCE OF TRUTH**: This implementation MUST follow the approved wireframes exactly. Any deviations must be explicitly approved and documented.

**Key Documents**:
- Business Requirements: `/docs/functional-areas/events/new-work/2025-08-24-events-management/requirements/business-requirements.md`
- Functional Specifications: `/docs/functional-areas/events/new-work/2025-08-24-events-management/requirements/functional-specifications.md`
- UI Component Mapping: `/docs/functional-areas/events/new-work/2025-08-24-events-management/design/ui-component-mapping.md`

## 1. Implementation Overview

### 1.1 Architecture Foundation
**CRITICAL**: This is a Web+API microservices architecture:
- **React Web Service**: UI/Auth at http://localhost:5173
- **API Service** (Minimal API): Business logic at http://localhost:5653
- **Database** (PostgreSQL): localhost:5433
- **Pattern**: React → HTTP → API → Database (NEVER React → Database directly)

### 1.2 Event Session Matrix Implementation
The system implements an **Event Session Matrix architecture** where:
- Events contain multiple Sessions (S1, S2, S3)
- Tickets can include specific Sessions
- Capacity is tracked per Session, not per Ticket Type
- System prevents overselling by tracking session-specific capacity

### 1.3 Implementation Phases
1. **Phase 1**: Database schema and API endpoints
2. **Phase 2**: React components and API integration
3. **Phase 3**: Admin dashboard and event management
4. **Phase 4**: Public event display and registration
5. **Phase 5**: Check-in interface and kiosk mode

## 2. CSS Standardization Requirements

### 🚨 CRITICAL: CSS STANDARDS ENFORCEMENT

**MANDATORY RULES**:
- ✅ **ONLY Design System v7 standardized CSS classes**
- ❌ **NO inline styles** (style="...")
- ❌ **NO page-specific CSS files**
- ✅ **Use Mantine v7 components with theme customization**
- ✅ **Follow established patterns from wireframes**

### 2.1 Mantine v7 Component Usage
```tsx
// ✅ CORRECT - Mantine components with WCR theme
<DataTable
  columns={columns}
  records={events}
  withTableBorder
  withColumnBorders
  highlightOnHover
  striped
/>

// ❌ WRONG - Inline styles
<div style={{ backgroundColor: '#9b4a75', padding: '16px' }}>
  Content
</div>

// ✅ CORRECT - Theme-based styling
<Box bg="wcr.6" p="md">
  Content
</Box>
```

### 2.2 WitchCityRope Theme Colors
```tsx
const wcrTheme = createTheme({
  colors: {
    wcr: [
      '#f8f4e6', // ivory (lightest)
      '#e8ddd4',
      '#d4a5a5', // dustyRose  
      '#c48b8b',
      '#b47171',
      '#a45757',
      '#9b4a75', // plum
      '#880124', // burgundy
      '#6b0119', // darker
      '#2c2c2c'  // charcoal (darkest)
    ]
  },
  primaryColor: 'wcr',
});
```

## 3. Component Architecture

### 3.1 Directory Structure
```
/apps/web/src/
├── pages/events/
│   ├── AdminDashboard.tsx
│   ├── EventForm.tsx
│   ├── PublicEventsList.tsx
│   ├── EventDetails.tsx
│   └── CheckInInterface.tsx
├── components/events/
│   ├── EventCard.tsx
│   ├── SessionEditor.tsx
│   ├── TicketTypeEditor.tsx
│   ├── VolunteerPositionEditor.tsx
│   ├── EmailTemplateEditor.tsx
│   └── CheckInModal.tsx
└── types/
    └── events.ts

/src/WitchCityRope.Api/Features/Events/
├── Services/
│   ├── IEventService.cs
│   ├── EventService.cs
│   ├── ISessionService.cs
│   ├── SessionService.cs
│   ├── ITicketService.cs
│   ├── TicketService.cs
│   ├── ICheckInService.cs
│   └── CheckInService.cs
├── Endpoints/
│   ├── EventEndpoints.cs
│   ├── SessionEndpoints.cs
│   ├── TicketEndpoints.cs
│   └── CheckInEndpoints.cs
├── Models/
│   ├── EventDto.cs
│   ├── SessionDto.cs
│   ├── TicketTypeDto.cs
│   ├── OrderDto.cs
│   ├── RSVPDto.cs
│   ├── VolunteerPositionDto.cs
│   ├── EmailTemplateDto.cs
│   └── CheckInDto.cs
└── Validators/
    ├── EventValidator.cs
    ├── SessionValidator.cs
    └── TicketValidator.cs
```

### 3.2 Shared Components to Build

#### DataGrid Standardization
**ALL data tables MUST follow this pattern**:
- **First Column**: Edit button/action
- **Last Column**: Delete button/action
- **Middle Columns**: Data specific to entity
- **Table Styling**: Burgundy header with standardized CSS

```tsx
const StandardDataTable = () => (
  <DataTable
    columns={[
      {
        accessor: 'actions',
        title: 'Actions',
        render: (record) => (
          <ActionIcon 
            variant="light" 
            color="wcr.6"
            onClick={() => openEditModal(record)}
          >
            <IconEdit size="1rem" />
          </ActionIcon>
        ),
      },
      // ... data columns
      {
        accessor: 'delete',
        title: 'Delete',
        textAlign: 'center',
        render: (record) => (
          <ActionIcon 
            variant="light" 
            color="red"
            onClick={() => openDeleteModal(record)}
          >
            <IconTrash size="1rem" />
          </ActionIcon>
        ),
      },
    ]}
    records={data}
    withTableBorder
    withColumnBorders
    highlightOnHover
  />
);
```

#### Modal Patterns for Editing
**ALL edit operations MUST use modal dialogs**:

```tsx
const EditModal = ({ opened, onClose, onSave, data }) => (
  <Modal 
    opened={opened} 
    onClose={onClose}
    title="Edit Session"
    size="lg"
  >
    <Stack>
      <TextInput
        label="Session Name"
        placeholder="e.g., Day 1 - Introduction"
        {...form.getInputProps('name')}
      />
      <Group justify="flex-end" mt="md">
        <Button variant="outline" onClick={onClose}>
          Cancel
        </Button>
        <Button onClick={onSave} color="wcr.6">
          Save Changes
        </Button>
      </Group>
    </Stack>
  </Modal>
);
```

#### Form Patterns with Floating Labels
```tsx
const EventForm = () => (
  <form onSubmit={form.onSubmit(handleSubmit)}>
    <Stack spacing="md">
      <TextInput
        label="Event Title"
        placeholder="Enter event title"
        required
        {...form.getInputProps('title')}
      />
      <Textarea
        label="Short Description"
        placeholder="Brief event description (160 chars max)"
        maxLength={160}
        {...form.getInputProps('shortDescription')}
      />
      <RichTextEditor editor={editor}>
        <RichTextEditor.Toolbar>
          <RichTextEditor.ControlsGroup>
            <RichTextEditor.Bold />
            <RichTextEditor.Italic />
            <RichTextEditor.Underline />
          </RichTextEditor.ControlsGroup>
        </RichTextEditor.Toolbar>
        <RichTextEditor.Content />
      </RichTextEditor>
    </Stack>
  </form>
);
```

## 4. Session-Based Architecture

### 4.1 Session ID Format (S1, S2, S3)
**CRITICAL**: ALL session references MUST use the S# format consistently:

```tsx
// ✅ CORRECT - Display format
const SessionReference = ({ sessionNumber, name }) => (
  <Group gap="sm">
    <Badge color="wcr.6" size="sm">
      S{sessionNumber}
    </Badge>
    <Text>{name}</Text>
  </Group>
);

// Session inclusion display
const TicketTypeCard = ({ ticket }) => (
  <Card>
    <Text fw={500}>{ticket.name}</Text>
    <Text size="sm" c="dimmed">
      Sessions: {ticket.sessionsIncluded} {/* "S1, S2, S3" */}
    </Text>
  </Card>
);
```

### 4.2 Session Management Components

#### Event Sessions Table
```tsx
const EventSessionsTable = () => (
  <Stack>
    <Group justify="space-between">
      <Title order={3}>Event Sessions</Title>
      <Button 
        leftSection={<IconPlus size="1rem" />}
        onClick={openAddSessionModal}
        color="wcr.6"
      >
        Add Session
      </Button>
    </Group>
    
    <DataTable
      columns={[
        {
          accessor: 'actions',
          title: 'Actions',
          render: (session) => (
            <ActionIcon 
              variant="light" 
              color="wcr.6"
              onClick={() => openEditModal(session)}
            >
              <IconEdit size="1rem" />
            </ActionIcon>
          ),
        },
        {
          accessor: 'sessionId',
          title: 'S#',
          render: ({ sessionNumber }) => (
            <Badge color="wcr.6" size="sm">
              S{sessionNumber}
            </Badge>
          ),
        },
        { accessor: 'name', title: 'Name' },
        { 
          accessor: 'startDateTime', 
          title: 'Date',
          render: ({ startDateTime }) => 
            new Date(startDateTime).toLocaleDateString()
        },
        {
          accessor: 'startTime',
          title: 'Start Time',
          render: ({ startDateTime }) =>
            new Date(startDateTime).toLocaleTimeString('en-US', { 
              hour: 'numeric', 
              minute: '2-digit',
              hour12: true 
            })
        },
        {
          accessor: 'endTime',
          title: 'End Time',
          render: ({ endDateTime }) =>
            new Date(endDateTime).toLocaleTimeString('en-US', { 
              hour: 'numeric', 
              minute: '2-digit',
              hour12: true 
            })
        },
        { accessor: 'capacity', title: 'Capacity' },
        { accessor: 'soldTickets', title: 'Sold' },
        {
          accessor: 'delete',
          title: 'Delete',
          textAlign: 'center',
          render: (session) => (
            <ActionIcon 
              variant="light" 
              color="red"
              onClick={() => openDeleteModal(session)}
            >
              <IconTrash size="1rem" />
            </ActionIcon>
          ),
        },
      ]}
      records={sessions}
      withTableBorder
      highlightOnHover
    />
  </Stack>
);
```

### 4.3 Capacity Calculation Services

#### Real-Time Capacity Checks
```tsx
const useSessionCapacity = (sessionId: string) => {
  return useQuery({
    queryKey: ['session-capacity', sessionId],
    queryFn: () => 
      fetch(`/api/sessions/${sessionId}/capacity`).then(res => res.json()),
    refetchInterval: 30000, // Refresh every 30 seconds
  });
};

const CapacityIndicator = ({ sessionId }) => {
  const { data: capacity } = useSessionCapacity(sessionId);
  
  const getColor = (available: number, total: number) => {
    const percentage = available / total;
    if (percentage > 0.3) return 'green';
    if (percentage > 0.1) return 'yellow';
    return 'red';
  };
  
  return (
    <Group gap="xs">
      <Text>{capacity?.available}/{capacity?.total}</Text>
      <Badge 
        color={getColor(capacity?.available, capacity?.total)} 
        size="sm"
      >
        {capacity?.available > 0 ? 'Available' : 'Full'}
      </Badge>
    </Group>
  );
};
```

### 4.4 Real-Time Updates
```tsx
// Use polling for real-time capacity updates
const useRealTimeCapacity = (eventId: string) => {
  const queryClient = useQueryClient();
  
  useEffect(() => {
    const interval = setInterval(() => {
      queryClient.invalidateQueries({ 
        queryKey: ['event-capacity', eventId] 
      });
    }, 5000); // Update every 5 seconds
    
    return () => clearInterval(interval);
  }, [eventId, queryClient]);
};
```

## 5. Key Implementation Patterns

### 5.1 Unified Email Template/Ad-hoc Interface

#### Email Templates Tab Layout
```tsx
const EmailsTab = () => (
  <Stack>
    <Title order={3}>Email Templates & Communications</Title>
    
    {/* Template Cards */}
    <SimpleGrid cols={{ base: 1, md: 2, lg: 3 }} spacing="md">
      {/* Email Template Cards */}
      {templates.map(template => (
        <TemplateCard 
          key={template.id}
          template={template}
          onEdit={handleEditTemplate}
          onRemove={handleRemoveTemplate}
        />
      ))}
      
      {/* Ad-hoc Email Card - Always Present */}
      <Card 
        withBorder 
        shadow="sm" 
        style={{ borderStyle: 'dashed' }}
        onClick={handleSendAdHocEmail}
      >
        <Stack align="center" justify="center" mih={120}>
          <ThemeIcon size="xl" variant="light" color="wcr.6">
            <IconMail size="1.5rem" />
          </ThemeIcon>
          <Text fw={500} c="wcr.6">Send Ad-Hoc Email</Text>
          <Text size="sm" c="dimmed" ta="center">
            Send one-time email to event attendees
          </Text>
        </Stack>
      </Card>
      
      {/* Add Template Dropdown */}
      <Card 
        withBorder 
        shadow="sm" 
        style={{ borderStyle: 'dashed' }}
      >
        <Stack align="center" justify="center" mih={120}>
          <ThemeIcon size="xl" variant="light" color="blue">
            <IconPlus size="1.5rem" />
          </ThemeIcon>
          <Select
            placeholder="Add Template"
            data={availableTemplates}
            onChange={handleAddTemplate}
          />
        </Stack>
      </Card>
    </SimpleGrid>
    
    {/* Unified Editor - Opens below when any card is clicked */}
    {selectedTemplate && (
      <EmailEditor 
        template={selectedTemplate}
        isAdHoc={selectedTemplate.type === 'adhoc'}
        onSave={handleSaveTemplate}
        onCancel={() => setSelectedTemplate(null)}
      />
    )}
  </Stack>
);
```

#### Template Card Component
```tsx
const TemplateCard = ({ template, onEdit, onRemove }) => (
  <Card 
    withBorder 
    shadow="sm" 
    onClick={() => onEdit(template)}
    style={{ cursor: 'pointer' }}
  >
    <Stack>
      <Group justify="space-between">
        <Text fw={500}>{template.name}</Text>
        <ActionIcon
          variant="light"
          color="red"
          size="sm"
          onClick={(e) => {
            e.stopPropagation();
            onRemove(template.id);
          }}
        >
          <IconX size="0.8rem" />
        </ActionIcon>
      </Group>
      
      <Text size="sm" c="dimmed">
        Target: {template.targetSessions}
      </Text>
      
      <Badge 
        color={template.triggerType === 'Manual' ? 'blue' : 'green'} 
        size="sm"
      >
        {template.triggerType}
      </Badge>
    </Stack>
  </Card>
);
```

### 5.2 RSVP vs Tickets Distinction

#### Social Events with RSVP System
```tsx
const SocialEventTables = ({ eventId }) => (
  <Stack>
    <Tabs defaultValue="rsvps">
      <Tabs.List>
        <Tabs.Tab value="rsvps">RSVPs</Tabs.Tab>
        <Tabs.Tab value="tickets">Tickets Sold</Tabs.Tab>
      </Tabs.List>
      
      <Tabs.Panel value="rsvps">
        <DataTable
          columns={[
            {
              accessor: 'actions',
              title: 'Actions',
              render: (rsvp) => (
                <ActionIcon variant="light" color="wcr.6">
                  <IconEye size="1rem" />
                </ActionIcon>
              ),
            },
            { accessor: 'name', title: 'Name' },
            { accessor: 'email', title: 'Email' },
            { 
              accessor: 'rsvpDate', 
              title: 'RSVP Date',
              render: ({ rsvpDate }) => 
                new Date(rsvpDate).toLocaleDateString()
            },
            {
              accessor: 'hasPurchasedTicket',
              title: 'Ticket Purchased',
              render: ({ hasPurchasedTicket }) => (
                <Badge 
                  color={hasPurchasedTicket ? 'green' : 'gray'}
                  size="sm"
                >
                  {hasPurchasedTicket ? 'Yes' : 'No'}
                </Badge>
              ),
            },
          ]}
          records={rsvps}
          withTableBorder
          highlightOnHover
        />
      </Tabs.Panel>
      
      <Tabs.Panel value="tickets">
        <TicketsSoldTable eventId={eventId} />
      </Tabs.Panel>
    </Tabs>
  </Stack>
);
```

### 5.3 Kiosk Mode Security

#### Secure Kiosk Session Generation
```tsx
const KioskMode = ({ eventId }) => {
  const [kioskSession, setKioskSession] = useState(null);
  
  const generateKioskSession = async () => {
    const response = await fetch('/api/check-in/kiosk-session', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        eventId,
        stationName: 'Front Desk 1',
        durationHours: 4,
      }),
    });
    
    const session = await response.json();
    setKioskSession(session);
    
    // Open kiosk window with secure URL
    const kioskUrl = `/events/${eventId}/check-in?session=${session.token}`;
    window.open(kioskUrl, '_blank', 'fullscreen=yes,toolbar=no,menubar=no');
  };
  
  return (
    <Card withBorder p="xl">
      <Stack align="center">
        <Title order={3}>Launch Kiosk Mode</Title>
        <Text c="dimmed" ta="center">
          Generate secure check-in session for volunteers
        </Text>
        
        <Button 
          size="lg"
          gradient={{ from: 'wcr.6', to: 'wcr.8' }}
          onClick={generateKioskSession}
          leftSection={<IconDeviceTablet size="1.2rem" />}
        >
          Generate Kiosk Session
        </Button>
        
        {kioskSession && (
          <Alert color="green" mt="md">
            <Stack gap="xs">
              <Text fw={500}>Kiosk Session Active</Text>
              <Text size="sm">
                Station: {kioskSession.stationName}
              </Text>
              <Text size="sm">
                Expires: {new Date(kioskSession.expiresAt).toLocaleString()}
              </Text>
            </Stack>
          </Alert>
        )}
      </Stack>
    </Card>
  );
};
```

#### Kiosk Interface with Security Indicators
```tsx
const KioskInterface = ({ sessionToken }) => {
  const { data: session } = useQuery({
    queryKey: ['kiosk-session', sessionToken],
    queryFn: () => 
      fetch(`/api/check-in/kiosk-session/${sessionToken}`).then(r => r.json()),
  });
  
  return (
    <AppShell
      header={{ height: 80 }}
      styles={{
        header: { 
          backgroundColor: '#880124', // burgundy
          color: 'white' 
        }
      }}
    >
      <AppShell.Header p="md">
        <Group justify="space-between" h="100%">
          <Group>
            <ThemeIcon size="lg" variant="light" color="white">
              <IconShieldCheck size="1.5rem" />
            </ThemeIcon>
            <Stack gap={0}>
              <Text c="white" fw={500}>KIOSK MODE ACTIVE</Text>
              <Text c="gray.3" size="sm">
                Station: {session?.stationName}
              </Text>
            </Stack>
          </Group>
          
          <Group>
            <Stack gap={0} align="end">
              <Text c="white" size="sm" ff="monospace">
                Event: {session?.eventCode}
              </Text>
              <Text c="white" size="sm" ff="monospace">
                Session: {session?.sessionId}
              </Text>
            </Stack>
            
            <Paper p="sm" bg="rgba(255,255,255,0.1)">
              <Stack align="center" gap={0}>
                <Text c="white" size="xs">Time Remaining</Text>
                <CountdownTimer expiresAt={session?.expiresAt} />
              </Stack>
            </Paper>
          </Group>
        </Group>
      </AppShell.Header>
      
      <AppShell.Main>
        <CheckInTable eventId={session?.eventId} />
      </AppShell.Main>
    </AppShell>
  );
};
```

### 5.4 Volunteer Position with Session Assignments

#### Volunteer Positions Table
```tsx
const VolunteerPositionsTable = () => (
  <DataTable
    columns={[
      {
        accessor: 'actions',
        title: 'Edit',
        render: (position) => (
          <ActionIcon 
            variant="light" 
            color="wcr.6"
            onClick={() => openEditModal(position)}
          >
            <IconEdit size="1rem" />
          </ActionIcon>
        ),
      },
      { accessor: 'positionName', title: 'Position' },
      {
        accessor: 'assignedSessions',
        title: 'Sessions',
        render: ({ assignedSessions }) => (
          <Group gap="xs">
            {assignedSessions.map(session => (
              <Badge key={session} color="wcr.6" size="sm">
                {session}
              </Badge>
            ))}
          </Group>
        ),
      },
      {
        accessor: 'timeRange',
        title: 'Time',
        render: ({ startTime, endTime }) => 
          `${startTime} - ${endTime}`
      },
      { 
        accessor: 'description', 
        title: 'Description',
        render: ({ description }) => (
          <Text truncate maw={200}>
            {description}
          </Text>
        ),
      },
      { accessor: 'volunteersNeeded', title: 'Needed' },
      {
        accessor: 'assignmentStatus',
        title: 'Assigned',
        render: ({ volunteersAssigned, volunteersNeeded }) => (
          <Badge 
            color={volunteersAssigned >= volunteersNeeded ? 'green' : 'yellow'}
            size="sm"
          >
            {volunteersAssigned}/{volunteersNeeded}
          </Badge>
        ),
      },
      {
        accessor: 'delete',
        title: 'Delete',
        textAlign: 'center',
        render: (position) => (
          <ActionIcon 
            variant="light" 
            color="red"
            onClick={() => openDeleteModal(position)}
          >
            <IconTrash size="1rem" />
          </ActionIcon>
        ),
      },
    ]}
    records={volunteerPositions}
    withTableBorder
    highlightOnHover
  />
);
```

## 6. API Implementation

### 6.1 Service Layer Following Backend Standards

#### Event Service Implementation
```csharp
public class EventService : IEventService
{
    private readonly WitchCityRopeIdentityDbContext _context; // ✅ CORRECT DbContext name
    private readonly ILogger<EventService> _logger;
    private readonly IValidator<CreateEventRequest> _validator;

    public EventService(
        WitchCityRopeIdentityDbContext context,
        ILogger<EventService> logger,
        IValidator<CreateEventRequest> validator)
    {
        _context = context;
        _logger = logger;
        _validator = validator;
    }

    public async Task<Result<EventDto>> CreateEventAsync(
        CreateEventRequest request, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate input
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result<EventDto>.Failure(validationResult.Errors.First().ErrorMessage);
            }

            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            
            // Create event with proper UTC timestamps
            var eventEntity = new Event
            {
                Id = Guid.NewGuid(), // ✅ CRITICAL: Initialize Id
                Title = request.Title,
                ShortDescription = request.ShortDescription,
                FullDescription = request.FullDescription,
                EventType = request.EventType,
                VenueId = request.VenueId,
                CreatedAt = DateTime.UtcNow, // ✅ CRITICAL: Always UTC
                UpdatedAt = DateTime.UtcNow,
                IsPublished = false
            };

            _context.Events.Add(eventEntity);
            await _context.SaveChangesAsync(cancellationToken);

            // Create initial session (S1)
            var initialSession = new EventSession
            {
                Id = Guid.NewGuid(),
                EventId = eventEntity.Id,
                SessionNumber = 1, // S1
                Name = "Main Session",
                StartDateTime = request.StartDateTime,
                EndDateTime = request.EndDateTime,
                Capacity = request.Capacity,
                CreatedAt = DateTime.UtcNow
            };

            _context.EventSessions.Add(initialSession);
            await _context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Event created successfully: {EventId}", eventEntity.Id);

            return Result<EventDto>.Success(new EventDto(eventEntity, [initialSession]));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating event");
            return Result<EventDto>.Failure("Failed to create event");
        }
    }

    public async Task<Result<List<EventDto>>> GetEventsAsync(
        EventFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Events
                .Include(e => e.Sessions)
                .Include(e => e.TicketTypes)
                .AsNoTracking(); // ✅ CRITICAL: AsNoTracking for read-only

            // Apply filters
            if (!string.IsNullOrEmpty(filter.EventType))
            {
                query = query.Where(e => e.EventType == filter.EventType);
            }

            if (filter.StartDate.HasValue)
            {
                query = query.Where(e => e.Sessions.Any(s => s.StartDateTime >= filter.StartDate.Value));
            }

            if (!filter.IncludePastEvents)
            {
                query = query.Where(e => e.Sessions.Any(s => s.StartDateTime > DateTime.UtcNow));
            }

            // Pagination
            var events = await query
                .OrderBy(e => e.Sessions.Min(s => s.StartDateTime))
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(e => new EventDto
                {
                    Id = e.Id,
                    Title = e.Title,
                    ShortDescription = e.ShortDescription,
                    EventType = e.EventType,
                    IsPublished = e.IsPublished,
                    Sessions = e.Sessions.Select(s => new SessionDto
                    {
                        Id = s.Id,
                        SessionNumber = s.SessionNumber,
                        Name = s.Name,
                        StartDateTime = s.StartDateTime,
                        EndDateTime = s.EndDateTime,
                        Capacity = s.Capacity,
                        SoldTickets = s.SoldTickets
                    }).ToList()
                })
                .ToListAsync(cancellationToken);

            return Result<List<EventDto>>.Success(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching events");
            return Result<List<EventDto>>.Failure("Failed to fetch events");
        }
    }
}
```

#### Session Service with S# Format
```csharp
public class SessionService : ISessionService
{
    private readonly WitchCityRopeIdentityDbContext _context;
    private readonly ILogger<SessionService> _logger;

    public async Task<Result<SessionDto>> CreateSessionAsync(
        CreateSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get next session number for the event
            var lastSessionNumber = await _context.EventSessions
                .Where(s => s.EventId == request.EventId)
                .MaxAsync(s => (int?)s.SessionNumber, cancellationToken) ?? 0;

            var newSession = new EventSession
            {
                Id = Guid.NewGuid(),
                EventId = request.EventId,
                SessionNumber = lastSessionNumber + 1, // Auto-increment S#
                Name = request.Name,
                StartDateTime = request.StartDateTime,
                EndDateTime = request.EndDateTime,
                Capacity = request.Capacity,
                CreatedAt = DateTime.UtcNow
            };

            _context.EventSessions.Add(newSession);
            await _context.SaveChangesAsync(cancellationToken);

            var sessionDto = new SessionDto
            {
                Id = newSession.Id,
                EventId = newSession.EventId,
                SessionNumber = newSession.SessionNumber,
                Name = newSession.Name,
                StartDateTime = newSession.StartDateTime,
                EndDateTime = newSession.EndDateTime,
                Capacity = newSession.Capacity,
                SoldTickets = 0
            };

            _logger.LogInformation("Session S{SessionNumber} created for event {EventId}", 
                newSession.SessionNumber, request.EventId);

            return Result<SessionDto>.Success(sessionDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating session");
            return Result<SessionDto>.Failure("Failed to create session");
        }
    }

    public async Task<Result<SessionCapacityDto>> GetSessionCapacityAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var session = await _context.EventSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

            if (session == null)
            {
                return Result<SessionCapacityDto>.Failure("Session not found");
            }

            // Calculate real-time capacity from ticket sales
            var soldTickets = await _context.TicketSessionInclusions
                .Where(tsi => tsi.SessionId == sessionId)
                .Join(_context.Orders, tsi => tsi.TicketTypeId, o => o.TicketTypeId, (tsi, o) => o)
                .Where(o => o.Status == "Confirmed")
                .CountAsync(cancellationToken);

            var capacity = new SessionCapacityDto
            {
                SessionId = sessionId,
                SessionNumber = session.SessionNumber,
                Name = session.Name,
                Capacity = session.Capacity,
                Sold = soldTickets,
                Available = session.Capacity - soldTickets
            };

            return Result<SessionCapacityDto>.Success(capacity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session capacity");
            return Result<SessionCapacityDto>.Failure("Failed to get session capacity");
        }
    }
}
```

### 6.2 DTOs Matching Wireframe Data

#### Core Event DTOs
```csharp
public class EventDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string FullDescription { get; set; } = string.Empty;
    public string PoliciesAndProcedures { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty; // 'Class' | 'Social'
    public Guid VenueId { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public List<SessionDto> Sessions { get; set; } = new();
    public List<TicketTypeDto> TicketTypes { get; set; } = new();
    public List<Guid> TeacherIds { get; set; } = new();
    public List<VolunteerPositionDto> VolunteerPositions { get; set; } = new();
    public List<EmailTemplateDto> EmailTemplates { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class SessionDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public int SessionNumber { get; set; } // For S1, S2, S3 display
    public string Name { get; set; } = string.Empty;
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public int Capacity { get; set; }
    public int SoldTickets { get; set; }
    public int AvailableSpots => Capacity - SoldTickets;
    
    // For display purposes
    public string SessionId => $"S{SessionNumber}";
}

public class TicketTypeDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // 'Single' | 'Couples'
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public int Quantity { get; set; }
    public DateTime SalesEndDate { get; set; }
    public List<Guid> SessionIds { get; set; } = new();
    public string SessionsIncluded { get; set; } = string.Empty; // "S1, S2, S3"
    public bool IsActive { get; set; }
    public int SoldCount { get; set; }
    public int Available => Quantity - SoldCount;
}

public class VolunteerPositionDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string PositionName { get; set; } = string.Empty;
    public List<string> AssignedSessions { get; set; } = new(); // ["S1", "S2", "All"]
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string Description { get; set; } = string.Empty;
    public int VolunteersNeeded { get; set; }
    public int VolunteersAssigned { get; set; }
}
```

### 6.3 Session-Based Endpoints

#### Event Endpoints
```csharp
public static class EventEndpoints
{
    public static void MapEventEndpoints(this IEndpointRouteBuilder app)
    {
        var events = app.MapGroup("/api/events").WithTags("Events");

        // Admin endpoints
        events.MapGet("/", GetEventsAsync)
            .WithName("GetEvents")
            .WithOpenApi();

        events.MapGet("/{id}", GetEventByIdAsync)
            .WithName("GetEventById")
            .WithOpenApi();

        events.MapPost("/", CreateEventAsync)
            .WithName("CreateEvent")
            .RequireAuthorization("Admin")
            .WithOpenApi();

        events.MapPut("/{id}", UpdateEventAsync)
            .WithName("UpdateEvent")
            .RequireAuthorization("Admin")
            .WithOpenApi();

        events.MapDelete("/{id}", DeleteEventAsync)
            .WithName("DeleteEvent")
            .RequireAuthorization("Admin")
            .WithOpenApi();

        // Session management
        events.MapGet("/{eventId}/sessions", GetEventSessionsAsync)
            .WithName("GetEventSessions")
            .WithOpenApi();

        events.MapPost("/{eventId}/sessions", CreateSessionAsync)
            .WithName("CreateSession")
            .RequireAuthorization("Admin")
            .WithOpenApi();

        // Ticket types
        events.MapGet("/{eventId}/ticket-types", GetTicketTypesAsync)
            .WithName("GetTicketTypes")
            .WithOpenApi();

        events.MapPost("/{eventId}/ticket-types", CreateTicketTypeAsync)
            .WithName("CreateTicketType")
            .RequireAuthorization("Admin")
            .WithOpenApi();
    }

    private static async Task<IResult> GetEventsAsync(
        [AsParameters] EventFilterRequest filter,
        IEventService eventService,
        CancellationToken cancellationToken)
    {
        var result = await eventService.GetEventsAsync(filter, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
    }

    private static async Task<IResult> CreateEventAsync(
        CreateEventRequest request,
        IEventService eventService,
        CancellationToken cancellationToken)
    {
        var result = await eventService.CreateEventAsync(request, cancellationToken);
        return result.IsSuccess 
            ? Results.CreatedAtRoute("GetEventById", new { id = result.Value.Id }, result.Value)
            : Results.BadRequest(result.Error);
    }
}
```

### 6.4 Kiosk Mode Endpoints

#### Check-In Service
```csharp
public class CheckInService : ICheckInService
{
    private readonly WitchCityRopeIdentityDbContext _context;
    private readonly ILogger<CheckInService> _logger;
    private readonly IMemoryCache _cache;

    public async Task<Result<KioskSessionDto>> CreateKioskSessionAsync(
        CreateKioskSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sessionToken = GenerateSecureToken();
            var expiresAt = DateTime.UtcNow.AddHours(request.DurationHours);
            
            var kioskSession = new KioskSessionDto
            {
                Token = sessionToken,
                EventId = request.EventId,
                StationName = request.StationName,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt,
                EventCode = await GenerateEventCodeAsync(request.EventId),
                SessionId = Guid.NewGuid().ToString("N")[..8].ToUpper()
            };

            // Store in cache with expiration
            _cache.Set($"kiosk_session_{sessionToken}", kioskSession, expiresAt);

            _logger.LogInformation("Kiosk session created: {Token} for event {EventId}", 
                sessionToken, request.EventId);

            return Result<KioskSessionDto>.Success(kioskSession);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating kiosk session");
            return Result<KioskSessionDto>.Failure("Failed to create kiosk session");
        }
    }

    public async Task<Result<CheckInDto>> CheckInAttendeeAsync(
        CheckInRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            var checkInRecord = new CheckInRecord
            {
                Id = Guid.NewGuid(),
                OrderId = request.OrderId,
                SessionId = request.SessionId,
                CheckedInAt = DateTime.UtcNow,
                CheckedInBy = GetCurrentUserId(),
                PaymentStatus = request.PaymentStatus,
                WaiverSigned = request.WaiverSigned,
                Notes = request.Notes,
                KioskStationId = request.KioskStationId
            };

            _context.CheckInRecords.Add(checkInRecord);
            await _context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Attendee checked in: Order {OrderId} at station {StationId}",
                request.OrderId, request.KioskStationId);

            return Result<CheckInDto>.Success(new CheckInDto(checkInRecord));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking in attendee");
            return Result<CheckInDto>.Failure("Failed to check in attendee");
        }
    }

    private string GenerateSecureToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var tokenBytes = new byte[32];
        rng.GetBytes(tokenBytes);
        return Convert.ToBase64String(tokenBytes).Replace('+', '-').Replace('/', '_')[..43];
    }
}
```

## 7. Database Schema

### 7.1 Core Event Tables
```sql
-- Events table
CREATE TABLE Events (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    Title VARCHAR(200) NOT NULL,
    ShortDescription VARCHAR(160) NOT NULL,
    FullDescription TEXT NOT NULL,
    PoliciesAndProcedures TEXT,
    EventType VARCHAR(20) NOT NULL CHECK (EventType IN ('Class', 'Social')),
    VenueId UUID NOT NULL REFERENCES Venues(Id),
    IsPublished BOOLEAN NOT NULL DEFAULT FALSE,
    CreatedAt TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UpdatedAt TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CreatedBy UUID NOT NULL REFERENCES Users(Id)
);

-- Event Sessions (S1, S2, S3, etc.)
CREATE TABLE EventSessions (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    EventId UUID NOT NULL REFERENCES Events(Id) ON DELETE CASCADE,
    SessionNumber INTEGER NOT NULL,
    Name VARCHAR(100) NOT NULL,
    StartDateTime TIMESTAMPTZ NOT NULL,
    EndDateTime TIMESTAMPTZ NOT NULL,
    Capacity INTEGER NOT NULL,
    SoldTickets INTEGER NOT NULL DEFAULT 0,
    CreatedAt TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE(EventId, SessionNumber)
);

-- Ticket Types
CREATE TABLE TicketTypes (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    EventId UUID NOT NULL REFERENCES Events(Id) ON DELETE CASCADE,
    Name VARCHAR(100) NOT NULL,
    Type VARCHAR(20) NOT NULL CHECK (Type IN ('Single', 'Couples')),
    MinPrice DECIMAL(10,2) NOT NULL,
    MaxPrice DECIMAL(10,2) NOT NULL,
    Quantity INTEGER NOT NULL,
    SalesEndDate DATE NOT NULL,
    IsActive BOOLEAN NOT NULL DEFAULT TRUE,
    CreatedAt TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Many-to-many: Ticket Types to Sessions
CREATE TABLE TicketSessionInclusions (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    TicketTypeId UUID NOT NULL REFERENCES TicketTypes(Id) ON DELETE CASCADE,
    SessionId UUID NOT NULL REFERENCES EventSessions(Id) ON DELETE CASCADE,
    UNIQUE(TicketTypeId, SessionId)
);

-- Orders (Tickets Sold)
CREATE TABLE Orders (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    OrderNumber VARCHAR(20) UNIQUE NOT NULL,
    CustomerId UUID NOT NULL REFERENCES Users(Id),
    TicketTypeId UUID NOT NULL REFERENCES TicketTypes(Id),
    Status VARCHAR(20) NOT NULL CHECK (Status IN ('Confirmed', 'Pending', 'Cancelled')),
    Amount DECIMAL(10,2) NOT NULL,
    PaymentMethod VARCHAR(50),
    PaymentStatus VARCHAR(20) NOT NULL DEFAULT 'Pending',
    CreatedAt TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- RSVPs (for Social Events)
CREATE TABLE RSVPs (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    EventId UUID NOT NULL REFERENCES Events(Id) ON DELETE CASCADE,
    UserId UUID NOT NULL REFERENCES Users(Id),
    RSVPDate TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    HasPurchasedTicket BOOLEAN NOT NULL DEFAULT FALSE,
    UNIQUE(EventId, UserId)
);
```

### 7.2 Supporting Tables
```sql
-- Venues
CREATE TABLE Venues (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    Name VARCHAR(100) NOT NULL,
    Address TEXT,
    Capacity INTEGER NOT NULL DEFAULT 20,
    IsActive BOOLEAN NOT NULL DEFAULT TRUE
);

-- Event Teachers/Instructors
CREATE TABLE EventTeachers (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    EventId UUID NOT NULL REFERENCES Events(Id) ON DELETE CASCADE,
    TeacherId UUID NOT NULL REFERENCES Users(Id),
    UNIQUE(EventId, TeacherId)
);

-- Volunteer Positions
CREATE TABLE VolunteerPositions (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    EventId UUID NOT NULL REFERENCES Events(Id) ON DELETE CASCADE,
    PositionName VARCHAR(100) NOT NULL,
    SessionsIncluded VARCHAR(50) NOT NULL, -- 'S1,S2,S3' or 'All'
    StartTime TIME NOT NULL,
    EndTime TIME NOT NULL,
    Description TEXT,
    NumberNeeded INTEGER NOT NULL,
    NumberAssigned INTEGER NOT NULL DEFAULT 0
);

-- Email Templates
CREATE TABLE EmailTemplates (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    EventId UUID NOT NULL REFERENCES Events(Id) ON DELETE CASCADE,
    TemplateName VARCHAR(50) NOT NULL,
    TriggerType VARCHAR(30) NOT NULL,
    TargetSessions VARCHAR(50), -- 'All' or 'S1,S2,S3'
    SubjectLine VARCHAR(200) NOT NULL,
    EmailContent TEXT NOT NULL,
    IsActive BOOLEAN NOT NULL DEFAULT TRUE
);

-- Check-in Records
CREATE TABLE CheckInRecords (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    OrderId UUID NOT NULL REFERENCES Orders(Id),
    SessionId UUID NOT NULL REFERENCES EventSessions(Id),
    CheckedInAt TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CheckedInBy UUID NOT NULL REFERENCES Users(Id),
    PaymentStatus VARCHAR(20) NOT NULL,
    WaiverSigned BOOLEAN NOT NULL,
    Notes TEXT,
    KioskStationId VARCHAR(50)
);
```

### 7.3 Entity Configurations
```csharp
public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("Events", "public");
        builder.HasKey(e => e.Id);
        
        // CRITICAL: Initialize Id in constructor
        builder.Property(e => e.Id)
            .ValueGeneratedNever(); // We set this manually
        
        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(200);
            
        builder.Property(e => e.ShortDescription)
            .IsRequired()
            .HasMaxLength(160);
            
        builder.Property(e => e.EventType)
            .IsRequired()
            .HasMaxLength(20);
        
        // UTC DateTime handling
        builder.Property(e => e.CreatedAt)
            .HasConversion(
                v => v.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : v,
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
            
        builder.Property(e => e.UpdatedAt)
            .HasConversion(
                v => v.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : v,
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        // Relationships
        builder.HasMany(e => e.Sessions)
            .WithOne()
            .HasForeignKey(s => s.EventId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasMany(e => e.TicketTypes)
            .WithOne()
            .HasForeignKey(t => t.EventId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class EventSessionConfiguration : IEntityTypeConfiguration<EventSession>
{
    public void Configure(EntityTypeBuilder<EventSession> builder)
    {
        builder.ToTable("EventSessions", "public");
        builder.HasKey(s => s.Id);
        
        builder.Property(s => s.Id)
            .ValueGeneratedNever();
            
        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(100);
            
        // Unique constraint for session numbering
        builder.HasIndex(s => new { s.EventId, s.SessionNumber })
            .IsUnique();
            
        // UTC DateTime conversions
        builder.Property(s => s.StartDateTime)
            .HasConversion(
                v => v.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : v,
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
            
        builder.Property(s => s.EndDateTime)
            .HasConversion(
                v => v.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : v,
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
    }
}
```

## 8. Testing Requirements

### 8.1 Component Tests for Each Wireframe

#### Event Form Component Tests
```tsx
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { EventForm } from '../EventForm';
import { MantineProvider } from '@mantine/core';

describe('EventForm', () => {
  const renderComponent = (props = {}) => {
    return render(
      <MantineProvider>
        <EventForm {...props} />
      </MantineProvider>
    );
  };

  test('renders all tabs as specified in wireframes', () => {
    renderComponent();
    
    expect(screen.getByText('Basic Info')).toBeInTheDocument();
    expect(screen.getByText('Tickets/Orders')).toBeInTheDocument();
    expect(screen.getByText('Emails')).toBeInTheDocument();
    expect(screen.getByText('Volunteers/Staff')).toBeInTheDocument();
  });

  test('Basic Info tab matches wireframe fields', () => {
    renderComponent();
    
    expect(screen.getByLabelText('Event Type')).toBeInTheDocument();
    expect(screen.getByLabelText('Event Title')).toBeInTheDocument();
    expect(screen.getByLabelText('Short Description')).toBeInTheDocument();
    expect(screen.getByLabelText('Full Event Description')).toBeInTheDocument();
    expect(screen.getByLabelText('Policies & Procedures')).toBeInTheDocument();
    expect(screen.getByLabelText('Venue')).toBeInTheDocument();
    expect(screen.getByText('Teachers/Instructors')).toBeInTheDocument();
  });

  test('Tickets/Orders tab shows sessions table with S# format', async () => {
    renderComponent();
    
    fireEvent.click(screen.getByText('Tickets/Orders'));
    
    await waitFor(() => {
      expect(screen.getByText('Event Sessions')).toBeInTheDocument();
      expect(screen.getByText('S#')).toBeInTheDocument();
      expect(screen.getByText('Name')).toBeInTheDocument();
      expect(screen.getByText('Date')).toBeInTheDocument();
      expect(screen.getByText('Start Time')).toBeInTheDocument();
      expect(screen.getByText('End Time')).toBeInTheDocument();
      expect(screen.getByText('Capacity')).toBeInTheDocument();
      expect(screen.getByText('Sold')).toBeInTheDocument();
    });
  });

  test('session creation generates S# format', async () => {
    renderComponent({ eventId: 'test-event' });
    
    fireEvent.click(screen.getByText('Tickets/Orders'));
    fireEvent.click(screen.getByText('Add Session'));
    
    // Mock session creation
    const sessionData = {
      name: 'Day 1 Introduction',
      startDateTime: '2024-03-15T14:00:00Z',
      endDateTime: '2024-03-15T17:00:00Z',
      capacity: 20
    };
    
    fireEvent.change(screen.getByLabelText('Session Name'), {
      target: { value: sessionData.name }
    });
    
    fireEvent.click(screen.getByText('Save Session'));
    
    await waitFor(() => {
      expect(screen.getByText('S1')).toBeInTheDocument();
      expect(screen.getByText('Day 1 Introduction')).toBeInTheDocument();
    });
  });
});
```

#### Admin Dashboard Tests
```tsx
describe('AdminDashboard', () => {
  test('renders events table with correct columns from wireframe', () => {
    renderComponent();
    
    expect(screen.getByText('Date')).toBeInTheDocument();
    expect(screen.getByText('Time')).toBeInTheDocument();
    expect(screen.getByText('Event Title')).toBeInTheDocument();
    expect(screen.getByText('Capacity')).toBeInTheDocument();
    expect(screen.getByText('Tickets')).toBeInTheDocument();
    expect(screen.getByText('Actions')).toBeInTheDocument();
  });

  test('Copy and Edit buttons appear in Actions column', () => {
    const mockEvents = [
      {
        id: '1',
        title: 'Rope Basics',
        date: '2024-03-15',
        time: '2:00 PM - 5:00 PM',
        capacity: 20,
        tickets: 15
      }
    ];
    
    renderComponent({ events: mockEvents });
    
    expect(screen.getByLabelText('Copy event')).toBeInTheDocument();
    expect(screen.getByLabelText('Edit event')).toBeInTheDocument();
  });

  test('Create Event button uses amber gradient styling', () => {
    renderComponent();
    
    const createButton = screen.getByText('Create Event');
    expect(createButton).toHaveClass('gradient'); // Mantine gradient class
  });
});
```

### 8.2 Integration Tests for Workflows

#### Event Creation Workflow Test
```csharp
[Collection("PostgreSQL Integration Tests")]
public class EventCreationWorkflowTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;
    private readonly IEventService _eventService;
    private readonly ISessionService _sessionService;

    public EventCreationWorkflowTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
        _eventService = _fixture.GetService<IEventService>();
        _sessionService = _fixture.GetService<ISessionService>();
    }

    [Fact]
    public async Task CreateEvent_WithMultipleSessions_GeneratesCorrectS#Format()
    {
        // Arrange
        var eventRequest = new CreateEventRequest
        {
            Title = $"Test Event {Guid.NewGuid():N}",
            ShortDescription = "Test description",
            FullDescription = "Full test description",
            EventType = "Class",
            VenueId = _fixture.TestVenueId,
            StartDateTime = DateTime.UtcNow.AddDays(30),
            EndDateTime = DateTime.UtcNow.AddDays(30).AddHours(3),
            Capacity = 20
        };

        // Act - Create event (creates S1 automatically)
        var eventResult = await _eventService.CreateEventAsync(eventRequest);
        Assert.True(eventResult.IsSuccess);
        
        var eventId = eventResult.Value.Id;
        
        // Act - Add S2
        var session2Request = new CreateSessionRequest
        {
            EventId = eventId,
            Name = "Day 2 - Advanced Techniques",
            StartDateTime = DateTime.UtcNow.AddDays(31),
            EndDateTime = DateTime.UtcNow.AddDays(31).AddHours(3),
            Capacity = 20
        };
        
        var session2Result = await _sessionService.CreateSessionAsync(session2Request);
        
        // Act - Add S3
        var session3Request = new CreateSessionRequest
        {
            EventId = eventId,
            Name = "Day 3 - Practice Session",
            StartDateTime = DateTime.UtcNow.AddDays(32),
            EndDateTime = DateTime.UtcNow.AddDays(32).AddHours(3),
            Capacity = 20
        };
        
        var session3Result = await _sessionService.CreateSessionAsync(session3Request);

        // Assert
        Assert.True(session2Result.IsSuccess);
        Assert.True(session3Result.IsSuccess);
        
        // Verify S# numbering
        Assert.Equal(1, eventResult.Value.Sessions.First().SessionNumber);
        Assert.Equal(2, session2Result.Value.SessionNumber);
        Assert.Equal(3, session3Result.Value.SessionNumber);
        
        // Verify display format
        Assert.Equal("S1", eventResult.Value.Sessions.First().SessionId);
        Assert.Equal("S2", session2Result.Value.SessionId);
        Assert.Equal("S3", session3Result.Value.SessionId);
    }
    
    [Fact]
    public async Task CreateTicketType_WithMultipleSessions_ShowsCorrectSessionInclusion()
    {
        // Arrange - Create event with 3 sessions
        var eventResult = await CreateEventWithThreeSessions();
        var eventId = eventResult.Value.Id;
        var sessions = eventResult.Value.Sessions;
        
        // Act - Create ticket type that includes S1 and S3
        var ticketRequest = new CreateTicketTypeRequest
        {
            EventId = eventId,
            Name = "Partial Workshop Pass",
            Type = "Single",
            MinPrice = 60,
            MaxPrice = 80,
            Quantity = 10,
            SalesEndDate = DateTime.UtcNow.AddDays(25),
            SessionIds = [sessions[0].Id, sessions[2].Id] // S1 and S3
        };
        
        var ticketResult = await _ticketService.CreateTicketTypeAsync(ticketRequest);
        
        // Assert
        Assert.True(ticketResult.IsSuccess);
        Assert.Equal("S1, S3", ticketResult.Value.SessionsIncluded);
        Assert.Equal(2, ticketResult.Value.SessionIds.Count);
    }
}
```

### 8.3 End-to-End Tests Matching User Flows

#### Event Management E2E Test
```typescript
import { test, expect } from '@playwright/test';

test.describe('Event Management System', () => {
  test('Complete event creation workflow matches wireframes', async ({ page }) => {
    // Login as admin
    await page.goto('/login');
    await page.fill('[data-testid="email"]', 'admin@witchcityrope.com');
    await page.fill('[data-testid="password"]', 'Test123!');
    await page.click('[data-testid="login-button"]');
    
    // Navigate to admin dashboard
    await page.goto('/admin/events');
    await expect(page.locator('h1')).toContainText('Events Management');
    
    // Verify dashboard table columns match wireframe
    await expect(page.locator('th:has-text("Date")')).toBeVisible();
    await expect(page.locator('th:has-text("Time")')).toBeVisible();
    await expect(page.locator('th:has-text("Event Title")')).toBeVisible();
    await expect(page.locator('th:has-text("Capacity")')).toBeVisible();
    await expect(page.locator('th:has-text("Tickets")')).toBeVisible();
    await expect(page.locator('th:has-text("Actions")')).toBeVisible();
    
    // Start event creation
    await page.click('[data-testid="create-event-button"]');
    await expect(page).toHaveURL(/.*\/admin\/events\/create/);
    
    // Verify all tabs present
    await expect(page.locator('[role="tab"]:has-text("Basic Info")')).toBeVisible();
    await expect(page.locator('[role="tab"]:has-text("Tickets/Orders")')).toBeVisible();
    await expect(page.locator('[role="tab"]:has-text("Emails")')).toBeVisible();
    await expect(page.locator('[role="tab"]:has-text("Volunteers/Staff")')).toBeVisible();
    
    // Fill Basic Info tab
    await page.fill('[data-testid="event-title"]', 'E2E Test Event');
    await page.fill('[data-testid="short-description"]', 'Test description for E2E testing');
    await page.selectOption('[data-testid="event-type"]', 'Class');
    await page.selectOption('[data-testid="venue"]', { index: 0 });
    
    // Switch to Tickets/Orders tab
    await page.click('[role="tab"]:has-text("Tickets/Orders")');
    
    // Verify sessions table
    await expect(page.locator('th:has-text("S#")')).toBeVisible();
    await expect(page.locator('th:has-text("Name")')).toBeVisible();
    await expect(page.locator('td:has-text("S1")')).toBeVisible();
    
    // Add second session
    await page.click('[data-testid="add-session-button"]');
    await page.fill('[data-testid="session-name"]', 'Day 2 - Advanced');
    await page.fill('[data-testid="session-capacity"]', '15');
    await page.click('[data-testid="save-session"]');
    
    // Verify S2 appears
    await expect(page.locator('td:has-text("S2")')).toBeVisible();
    await expect(page.locator('td:has-text("Day 2 - Advanced")')).toBeVisible();
    
    // Add ticket type
    await page.click('[data-testid="add-ticket-type-button"]');
    await page.fill('[data-testid="ticket-name"]', 'Full Workshop Pass');
    await page.selectOption('[data-testid="ticket-type"]', 'Single');
    await page.check('[data-testid="session-s1"]');
    await page.check('[data-testid="session-s2"]');
    await page.click('[data-testid="save-ticket-type"]');
    
    // Verify ticket appears with correct session inclusion
    await expect(page.locator('td:has-text("Full Workshop Pass")')).toBeVisible();
    await expect(page.locator('td:has-text("S1, S2")')).toBeVisible();
    
    // Save event
    await page.click('[data-testid="save-event"]');
    
    // Verify redirect to dashboard and event appears
    await expect(page).toHaveURL(/.*\/admin\/events/);
    await expect(page.locator('td:has-text("E2E Test Event")')).toBeVisible();
  });
  
  test('Check-in interface kiosk mode security', async ({ page }) => {
    // Create event and launch kiosk mode
    await loginAsAdmin(page);
    await page.goto('/admin/events/test-event-id');
    
    // Launch kiosk mode
    await page.click('[data-testid="launch-kiosk-button"]');
    
    // New window should open with kiosk interface
    const [kioskPage] = await Promise.all([
      page.waitForEvent('popup'),
      page.click('[data-testid="generate-kiosk-session"]')
    ]);
    
    // Verify kiosk security indicators
    await expect(kioskPage.locator('[data-testid="kiosk-header"]')).toContainText('KIOSK MODE ACTIVE');
    await expect(kioskPage.locator('[data-testid="station-name"]')).toBeVisible();
    await expect(kioskPage.locator('[data-testid="session-timer"]')).toBeVisible();
    await expect(kioskPage.locator('[data-testid="event-code"]')).toBeVisible();
    
    // Verify check-in functionality
    await kioskPage.fill('[data-testid="search-attendees"]', 'Test User');
    await expect(kioskPage.locator('[data-testid="attendee-row"]')).toBeVisible();
    
    // Check in attendee
    await kioskPage.click('[data-testid="check-in-button"]');
    
    // Verify modal opens
    await expect(kioskPage.locator('[data-testid="check-in-modal"]')).toBeVisible();
    await expect(kioskPage.locator('[data-testid="payment-status"]')).toBeVisible();
    await expect(kioskPage.locator('[data-testid="waiver-checkbox"]')).toBeVisible();
    
    // Complete check-in
    await kioskPage.selectOption('[data-testid="payment-status"]', 'Paid');
    await kioskPage.check('[data-testid="waiver-checkbox"]');
    await kioskPage.click('[data-testid="confirm-check-in"]');
    
    // Verify status update
    await expect(kioskPage.locator('[data-testid="checked-in-status"]')).toContainText('Checked In');
  });
});
```

## 9. Migration Strategy

### 9.1 Integration with Existing System

#### Phase 1: Database Schema Migration
```sql
-- Migration script: AddEventsManagementTables_20250825_001.sql

-- Add new tables while preserving existing event data
BEGIN;

-- Create new Events table structure
CREATE TABLE Events_New (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    Title VARCHAR(200) NOT NULL,
    ShortDescription VARCHAR(160) NOT NULL,
    FullDescription TEXT NOT NULL,
    PoliciesAndProcedures TEXT,
    EventType VARCHAR(20) NOT NULL CHECK (EventType IN ('Class', 'Social')),
    VenueId UUID NOT NULL,
    IsPublished BOOLEAN NOT NULL DEFAULT FALSE,
    CreatedAt TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UpdatedAt TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CreatedBy UUID NOT NULL
);

-- Migrate existing events data
INSERT INTO Events_New (
    Id, Title, ShortDescription, FullDescription, EventType, VenueId, 
    IsPublished, CreatedAt, UpdatedAt, CreatedBy
)
SELECT 
    Id,
    Name AS Title,
    COALESCE(Description, '') AS ShortDescription,
    COALESCE(LongDescription, Description, '') AS FullDescription,
    CASE 
        WHEN Type = 'workshop' THEN 'Class'
        WHEN Type = 'meetup' THEN 'Social'
        ELSE 'Class'
    END AS EventType,
    VenueId,
    IsActive AS IsPublished,
    CreatedAt,
    COALESCE(UpdatedAt, CreatedAt) AS UpdatedAt,
    COALESCE(CreatedBy, '00000000-0000-0000-0000-000000000000'::UUID) AS CreatedBy
FROM Events_Legacy 
WHERE DeletedAt IS NULL;

-- Create EventSessions from existing events
INSERT INTO EventSessions (Id, EventId, SessionNumber, Name, StartDateTime, EndDateTime, Capacity, CreatedAt)
SELECT 
    gen_random_uuid() AS Id,
    e.Id AS EventId,
    1 AS SessionNumber,
    'Main Session' AS Name,
    e.StartTime AS StartDateTime,
    e.EndTime AS EndDateTime,
    COALESCE(e.MaxCapacity, 20) AS Capacity,
    NOW() AS CreatedAt
FROM Events_New e;

-- Rename tables
ALTER TABLE Events RENAME TO Events_Legacy_Backup;
ALTER TABLE Events_New RENAME TO Events;

-- Add foreign key constraints
ALTER TABLE Events ADD CONSTRAINT FK_Events_Venues 
    FOREIGN KEY (VenueId) REFERENCES Venues(Id);

COMMIT;
```

#### Phase 2: API Compatibility Layer
```csharp
// Legacy API compatibility during transition
[ApiController]
[Route("api/legacy/events")]
public class LegacyEventsController : ControllerBase
{
    private readonly IEventService _eventService;

    public LegacyEventsController(IEventService eventService)
    {
        _eventService = eventService;
    }

    [HttpGet]
    public async Task<IActionResult> GetEvents()
    {
        var result = await _eventService.GetEventsAsync(new EventFilterRequest());
        
        if (!result.IsSuccess)
            return BadRequest(result.Error);

        // Map to legacy format for backward compatibility
        var legacyEvents = result.Value.Select(e => new LegacyEventDto
        {
            Id = e.Id,
            Name = e.Title, // Map Title to Name
            Description = e.ShortDescription,
            LongDescription = e.FullDescription,
            Type = e.EventType.ToLowerInvariant(), // Map to legacy types
            StartTime = e.Sessions.MinBy(s => s.StartDateTime)?.StartDateTime,
            EndTime = e.Sessions.MaxBy(s => s.EndDateTime)?.EndDateTime,
            MaxCapacity = e.Sessions.Sum(s => s.Capacity),
            RegisteredCount = e.Sessions.Sum(s => s.SoldTickets),
            IsActive = e.IsPublished
        });

        return Ok(legacyEvents);
    }
}

// Keep legacy DTOs for compatibility
public class LegacyEventDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string LongDescription { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int MaxCapacity { get; set; }
    public int RegisteredCount { get; set; }
    public bool IsActive { get; set; }
}
```

### 9.2 Data Transformation Scripts
```csharp
public class EventsMigrationService
{
    private readonly WitchCityRopeIdentityDbContext _context;
    private readonly ILogger<EventsMigrationService> _logger;

    public async Task<MigrationResult> MigrateExistingEventsAsync()
    {
        var migrationLog = new List<string>();
        
        try
        {
            // 1. Migrate Events
            var legacyEvents = await _context.Database
                .SqlQuery<LegacyEventRecord>(
                    "SELECT * FROM Events_Legacy WHERE DeletedAt IS NULL")
                .ToListAsync();

            foreach (var legacy in legacyEvents)
            {
                var newEvent = new Event
                {
                    Id = legacy.Id,
                    Title = legacy.Name,
                    ShortDescription = TruncateToLength(legacy.Description, 160),
                    FullDescription = legacy.LongDescription ?? legacy.Description,
                    EventType = MapEventType(legacy.Type),
                    VenueId = legacy.VenueId,
                    IsPublished = legacy.IsActive,
                    CreatedAt = legacy.CreatedAt,
                    UpdatedAt = legacy.UpdatedAt ?? legacy.CreatedAt,
                    CreatedBy = legacy.CreatedBy ?? Guid.Empty
                };

                _context.Events.Add(newEvent);
                migrationLog.Add($"Migrated event: {newEvent.Title}");

                // Create default session (S1)
                var session = new EventSession
                {
                    Id = Guid.NewGuid(),
                    EventId = newEvent.Id,
                    SessionNumber = 1,
                    Name = "Main Session",
                    StartDateTime = legacy.StartTime,
                    EndDateTime = legacy.EndTime,
                    Capacity = legacy.MaxCapacity ?? 20,
                    CreatedAt = DateTime.UtcNow
                };

                _context.EventSessions.Add(session);
                migrationLog.Add($"Created session S1 for: {newEvent.Title}");
            }

            // 2. Migrate existing registrations to Orders
            await MigrateRegistrationsToOrders(migrationLog);

            // 3. Create default venues if missing
            await EnsureDefaultVenuesExist(migrationLog);

            await _context.SaveChangesAsync();

            return new MigrationResult
            {
                Success = true,
                EventsMigrated = legacyEvents.Count,
                Log = migrationLog
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during events migration");
            return new MigrationResult
            {
                Success = false,
                Error = ex.Message,
                Log = migrationLog
            };
        }
    }

    private string MapEventType(string legacyType) => legacyType.ToLowerInvariant() switch
    {
        "workshop" => "Class",
        "class" => "Class",
        "meetup" => "Social",
        "social" => "Social",
        _ => "Class"
    };

    private string TruncateToLength(string? text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return text.Length <= maxLength ? text : text.Substring(0, maxLength - 3) + "...";
    }
}
```

### 9.3 Backward Compatibility Maintenance
```csharp
// Feature flag for gradual rollout
public class EventsFeatureFlags
{
    public bool UseNewEventsSystem { get; set; } = false;
    public bool EnableSessionManagement { get; set; } = false;
    public bool EnableKioskMode { get; set; } = false;
    public bool ShowLegacyEventFields { get; set; } = true;
}

// Service that can work with both old and new systems
public class HybridEventService : IEventService
{
    private readonly IEventService _newEventService;
    private readonly ILegacyEventService _legacyEventService;
    private readonly EventsFeatureFlags _featureFlags;

    public async Task<Result<List<EventDto>>> GetEventsAsync(
        EventFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        if (_featureFlags.UseNewEventsSystem)
        {
            return await _newEventService.GetEventsAsync(filter, cancellationToken);
        }

        // Use legacy service and map to new DTOs
        var legacyResult = await _legacyEventService.GetEventsAsync(filter, cancellationToken);
        
        if (!legacyResult.IsSuccess)
            return Result<List<EventDto>>.Failure(legacyResult.Error);

        var mappedEvents = legacyResult.Value.Select(MapLegacyToNew).ToList();
        return Result<List<EventDto>>.Success(mappedEvents);
    }

    private EventDto MapLegacyToNew(LegacyEventDto legacy)
    {
        return new EventDto
        {
            Id = legacy.Id,
            Title = legacy.Name,
            ShortDescription = legacy.Description,
            FullDescription = legacy.LongDescription,
            EventType = MapEventType(legacy.Type),
            IsPublished = legacy.IsActive,
            Sessions = new List<SessionDto>
            {
                new SessionDto
                {
                    Id = Guid.NewGuid(),
                    EventId = legacy.Id,
                    SessionNumber = 1,
                    Name = "Main Session",
                    StartDateTime = legacy.StartTime ?? DateTime.UtcNow,
                    EndDateTime = legacy.EndTime ?? DateTime.UtcNow.AddHours(3),
                    Capacity = legacy.MaxCapacity,
                    SoldTickets = legacy.RegisteredCount
                }
            }
        };
    }
}
```

## 10. Development Checklist

### 10.1 Backend Implementation Order

#### Phase 1: Database and Core Services (Week 1)
- [ ] Create database migration scripts
- [ ] Implement Event entity with proper Id initialization
- [ ] Implement EventSession entity with S# numbering
- [ ] Implement TicketType entity with session relationships
- [ ] Create EventService following backend standards
- [ ] Create SessionService with capacity calculations
- [ ] Implement Result pattern error handling
- [ ] Add structured logging with context
- [ ] Create FluentValidation validators
- [ ] Write integration tests with TestContainers

#### Phase 2: API Endpoints (Week 1)
- [ ] Implement EventEndpoints with minimal API pattern
- [ ] Implement SessionEndpoints with capacity endpoints
- [ ] Implement TicketEndpoints with session inclusion logic
- [ ] Add OpenAPI documentation annotations
- [ ] Implement JWT authorization for admin endpoints
- [ ] Create DTOs with proper NSwag compatibility
- [ ] Add cancellation token support throughout
- [ ] Implement caching strategies

#### Phase 3: Advanced Features (Week 2)
- [ ] Implement VolunteerPositionService
- [ ] Implement EmailTemplateService
- [ ] Implement CheckInService with kiosk mode
- [ ] Create secure kiosk session generation
- [ ] Implement real-time capacity updates
- [ ] Add audit logging for check-in actions
- [ ] Create RSVP system for social events
- [ ] Implement venue management endpoints

### 10.2 Frontend Implementation Order

#### Phase 1: Component Foundation (Week 2)
- [ ] Set up WCR theme with Mantine v7
- [ ] Create standardized DataTable component
- [ ] Implement modal patterns for editing
- [ ] Create form patterns with floating labels
- [ ] Set up React Query for API state management
- [ ] Create TypeScript interfaces from generated DTOs
- [ ] Implement error handling patterns
- [ ] Add loading states with Skeleton components

#### Phase 2: Admin Dashboard (Week 2)
- [ ] Create AdminDashboard component matching wireframe
- [ ] Implement events table with sorting/filtering
- [ ] Create Create Event button with amber gradient
- [ ] Add Copy and Edit actions
- [ ] Implement event status badges
- [ ] Add real-time capacity indicators
- [ ] Create responsive breakpoints for mobile
- [ ] Add search and filter functionality

#### Phase 3: Event Management Form (Week 3)
- [ ] Create tabbed interface with all 4 tabs
- [ ] Implement Basic Info tab with all wireframe fields
- [ ] Create Tickets/Orders tab with sessions table
- [ ] Implement Sessions table with S# format display
- [ ] Create Ticket Types table with session inclusion
- [ ] Add Orders/RSVPs tables with proper distinction
- [ ] Implement modal editing for all entities
- [ ] Add form validation with real-time feedback

#### Phase 4: Email and Volunteer Management (Week 3)
- [ ] Create Emails tab with template cards
- [ ] Implement unified email editor
- [ ] Ensure ad-hoc email card always present
- [ ] Add template dropdown for adding templates
- [ ] Create Volunteers/Staff tab with positions table
- [ ] Implement session assignment for positions
- [ ] Add position creation form
- [ ] Connect email targeting to sessions

#### Phase 5: Public Interface (Week 4)
- [ ] Create PublicEventsList component
- [ ] Implement card and list view toggle
- [ ] Add search and filtering for public events
- [ ] Create EventDetails component
- [ ] Implement registration interface
- [ ] Show session constraints and availability
- [ ] Add ticket selection with session inclusion
- [ ] Create mobile-responsive design

#### Phase 6: Check-in Interface (Week 4)
- [ ] Create CheckInInterface component
- [ ] Implement kiosk mode security
- [ ] Add session timer and security indicators
- [ ] Create attendee search and filtering
- [ ] Implement check-in modal with all fields
- [ ] Add real-time stats updates
- [ ] Create mobile-optimized check-in interface
- [ ] Implement offline indication

### 10.3 Testing Implementation

#### Unit Tests (Throughout Development)
- [ ] Test all service methods with mocked dependencies
- [ ] Test all validation rules
- [ ] Test session capacity calculations
- [ ] Test S# format generation and display
- [ ] Test ticket session inclusion logic
- [ ] Test kiosk session security
- [ ] Test RSVP vs ticket distinction
- [ ] Test volunteer position session assignment

#### Integration Tests (Week 5)
- [ ] Test complete event creation workflow
- [ ] Test session management with capacity limits
- [ ] Test ticket type creation with multiple sessions
- [ ] Test check-in process end-to-end
- [ ] Test kiosk mode session generation
- [ ] Test email template management
- [ ] Test volunteer position assignment
- [ ] Test migration from legacy system

#### E2E Tests (Week 5)
- [ ] Test admin dashboard functionality
- [ ] Test complete event creation in browser
- [ ] Test public event browsing and registration
- [ ] Test check-in interface workflows
- [ ] Test kiosk mode security measures
- [ ] Test mobile responsiveness
- [ ] Test real-time updates
- [ ] Test error handling scenarios

### 10.4 Quality Gates

#### Code Review Checklist
- [ ] All CSS uses Design System v7 classes only
- [ ] No inline styles or page-specific CSS
- [ ] All tables follow Edit-first, Delete-last pattern
- [ ] Session references use S# format consistently
- [ ] All API calls use proper error handling
- [ ] All forms use proper validation
- [ ] All components have proper TypeScript types
- [ ] All backend services follow standards template
- [ ] All database operations use UTC timestamps
- [ ] All entity constructors initialize Id property

#### Security Review Checklist
- [ ] Kiosk mode prevents navigation tampering
- [ ] JWT tokens properly validated on all admin endpoints
- [ ] No sensitive data exposed in client code
- [ ] All user inputs validated and sanitized
- [ ] Audit logging implemented for admin actions
- [ ] Session timeouts properly enforced
- [ ] CORS configured correctly for API calls
- [ ] Authentication patterns follow established standards

#### Performance Review Checklist
- [ ] Database queries use AsNoTracking for read-only operations
- [ ] Pagination implemented for large datasets
- [ ] Real-time updates don't cause excessive API calls
- [ ] Components use React.memo where appropriate
- [ ] Images optimized for web delivery
- [ ] Bundle size meets targets (<200KB for events module)
- [ ] API responses meet timing targets
- [ ] Mobile performance acceptable on slower devices

This comprehensive implementation guide ensures that the Events Management System is built exactly to the wireframe specifications while following all established architectural patterns and standards. The guide provides clear direction for both backend and frontend developers, ensuring consistent implementation across all components.

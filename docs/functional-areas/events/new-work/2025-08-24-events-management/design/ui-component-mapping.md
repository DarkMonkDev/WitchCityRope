# UI Component Mapping: Events Management to React/Mantine v7

<!-- Last Updated: 2025-08-24 -->
<!-- Version: 1.0 -->
<!-- Owner: UI Designer Agent -->
<!-- Status: Ready for Development -->

## Overview

This document maps the existing Events Management wireframes to React components using Mantine v7. The wireframes are approximately 90% complete and provide excellent patterns for implementation.

**Source Wireframes:**
- Admin Events Management: `/docs/functional-areas/events/admin-events-management/admin-events-visual.html`
- Event Creation: `/docs/functional-areas/events/admin-events-management/event-creation.html`
- Check-in Interface: `/docs/functional-areas/events/events-checkin/event-checkin-visual.html`
- Public Events: `/docs/functional-areas/events/public-events/event-list-visual.html`

## Core Mantine Components Mapping

### 1. Admin Events Management Dashboard

#### Layout Structure
```tsx
// Main Layout - Mantine AppShell
<AppShell
  header={{ height: 70 }}
  navbar={{ width: 260, breakpoint: 'sm' }}
  padding="md"
>
  <AppShell.Header>
    {/* Admin Header Component */}
  </AppShell.Header>
  
  <AppShell.Navbar>
    {/* Sidebar Navigation Component */}
  </AppShell.Navbar>
  
  <AppShell.Main>
    {/* Main Content Area */}
  </AppShell.Main>
</AppShell>
```

#### Header Component Mapping
**HTML Elements → Mantine Components:**
- `.admin-header` → `Flex` with `justify="space-between"`
- `.admin-logo` → `Group` with `Anchor` and `Badge`
- `.admin-user` → `Group` with `Text` and `Avatar`

```tsx
const AdminHeader = () => (
  <Flex justify="space-between" align="center" p="md">
    <Group gap="sm">
      <Anchor component={Link} to="/" c="white" td="none">
        <Text size="xl" fw={700}>Witch City Rope</Text>
      </Anchor>
      <Badge color="wcr.6" size="sm">Admin</Badge>
    </Group>
    
    <Group gap="md">
      <Text c="white">Welcome, Alex</Text>
      <Avatar color="wcr.6" radius="xl">AR</Avatar>
    </Group>
  </Flex>
);
```

#### Sidebar Navigation Mapping
**HTML Elements → Mantine Components:**
- `.sidebar-nav` → `NavLink` components
- `.sidebar-section` → `Stack` with section titles
- `.sidebar-title` → `Text` with appropriate styling

```tsx
const AdminSidebar = () => (
  <Stack gap="lg" p="md">
    <div>
      <Text size="xs" tt="uppercase" fw={700} c="dimmed" mb="sm">
        Main
      </Text>
      <Stack gap="xs">
        <NavLink
          component={Link}
          to="/admin/dashboard"
          label="Dashboard"
          leftSection={<IconDashboard size="1rem" />}
        />
        <NavLink
          component={Link}
          to="/admin/events"
          label="Events"
          leftSection={<IconCalendar size="1rem" />}
          active
        />
        <NavLink
          component={Link}
          to="/admin/members"
          label="Members"
          leftSection={<IconUsers size="1rem" />}
        />
      </Stack>
    </div>
  </Stack>
);
```

### 2. Events Data Table

#### Recommended Component: DataTable from @mantine/datatable
**HTML Table → Mantine DataTable:**

```tsx
import { DataTable } from 'mantine-datatable';

const EventsDataTable = () => (
  <DataTable
    columns={[
      {
        accessor: 'title',
        title: 'Event Name',
        render: ({ title, type }) => (
          <Group gap="sm">
            <Text fw={500}>{title}</Text>
            <Badge size="xs" color={type === 'class' ? 'wcr.6' : 'blue'}>
              {type}
            </Badge>
          </Group>
        ),
      },
      { accessor: 'date', title: 'Date' },
      { accessor: 'instructor', title: 'Instructor' },
      {
        accessor: 'capacity',
        title: 'Capacity',
        render: ({ registered, total }) => `${registered}/${total}`,
      },
      {
        accessor: 'status',
        title: 'Status',
        render: ({ status }) => (
          <Badge 
            color={
              status === 'published' ? 'green' : 
              status === 'draft' ? 'yellow' : 'red'
            }
            size="sm"
          >
            {status}
          </Badge>
        ),
      },
      {
        accessor: 'actions',
        title: 'Actions',
        textAlign: 'center',
        render: (event) => (
          <Group gap="xs" justify="center">
            <ActionIcon variant="light" color="blue">
              <IconEdit size="1rem" />
            </ActionIcon>
            <ActionIcon variant="light" color="green">
              <IconClipboard size="1rem" />
            </ActionIcon>
            <ActionIcon variant="light" color="gray">
              <IconDots size="1rem" />
            </ActionIcon>
          </Group>
        ),
      },
    ]}
    records={events}
    withTableBorder
    withColumnBorders
    highlightOnHover
    striped
    minHeight={400}
    noRecordsText="No events found"
  />
);
```

#### Alternative: Native Mantine Table
For simpler requirements:
```tsx
const EventsTable = () => (
  <Table striped highlightOnHover withTableBorder>
    <Table.Thead>
      <Table.Tr>
        <Table.Th>Event Name</Table.Th>
        <Table.Th>Date</Table.Th>
        <Table.Th>Instructor</Table.Th>
        <Table.Th>Capacity</Table.Th>
        <Table.Th>Status</Table.Th>
        <Table.Th>Actions</Table.Th>
      </Table.Tr>
    </Table.Thead>
    <Table.Tbody>{/* table rows */}</Table.Tbody>
  </Table>
);
```

### 3. Stats Cards Component

**HTML Stats → Mantine Grid with Cards:**
```tsx
const StatsGrid = () => (
  <SimpleGrid cols={{ base: 1, sm: 2, lg: 4 }} spacing="lg" mb="xl">
    <Card withBorder shadow="sm">
      <Group justify="space-between">
        <div>
          <Text c="dimmed" size="sm" tt="uppercase" fw={500}>
            Active Events
          </Text>
          <Text fw={700} size="xl">
            8
          </Text>
        </div>
        <ThemeIcon color="wcr.6" variant="light" size="lg" radius="md">
          <IconCalendar size="1.4rem" />
        </ThemeIcon>
      </Group>
    </Card>
    
    <Card withBorder shadow="sm">
      <Group justify="space-between">
        <div>
          <Text c="dimmed" size="sm" tt="uppercase" fw={500}>
            Total Registrations
          </Text>
          <Text fw={700} size="xl">
            142
          </Text>
        </div>
        <ThemeIcon color="blue" variant="light" size="lg" radius="md">
          <IconUsers size="1.4rem" />
        </ThemeIcon>
      </Group>
    </Card>
    
    {/* Additional stat cards... */}
  </SimpleGrid>
);
```

### 4. Filter Bar Component

**HTML Filter Bar → Mantine Flex with Pill Buttons:**
```tsx
const FilterBar = () => (
  <Paper withBorder shadow="sm" p="md" mb="lg">
    <Flex justify="space-between" align="center" wrap="wrap" gap="md">
      <Group gap="sm">
        <Pill.Group>
          <Pill>All Events</Pill>
          <Pill>Classes</Pill>
          <Pill>Meetups</Pill>
          <Pill>Past</Pill>
          <Pill>Draft</Pill>
        </Pill.Group>
      </Group>
      
      <Group gap="md">
        <TextInput
          placeholder="Search events..."
          leftSection={<IconSearch size="1rem" />}
          w={250}
        />
      </Group>
    </Flex>
  </Paper>
);
```

### 5. Event Creation Form (Tabbed Interface)

**HTML Tabs → Mantine Tabs:**
```tsx
const EventCreationForm = () => (
  <Container size="lg">
    <Title order={1} mb="lg">Create New Event</Title>
    
    <Tabs defaultValue="basic-info" variant="outline">
      <Tabs.List grow>
        <Tabs.Tab value="basic-info">Basic Info</Tabs.Tab>
        <Tabs.Tab value="tickets-orders">Tickets/Orders</Tabs.Tab>
        <Tabs.Tab value="emails">Emails</Tabs.Tab>
        <Tabs.Tab value="volunteers-staff">Volunteers/Staff</Tabs.Tab>
      </Tabs.List>

      <Tabs.Panel value="basic-info">
        <Paper withBorder p="xl" mt="md">
          <BasicInfoForm />
        </Paper>
      </Tabs.Panel>

      <Tabs.Panel value="tickets-orders">
        <Paper withBorder p="xl" mt="md">
          <TicketsOrdersForm />
        </Paper>
      </Tabs.Panel>

      {/* Additional tab panels... */}
    </Tabs>
  </Container>
);
```

#### Form Components within Tabs

**Event Type Toggle → Mantine SegmentedControl:**
```tsx
const EventTypeToggle = () => (
  <SegmentedControl
    data={[
      { label: 'Class', value: 'class' },
      { label: 'Meetup', value: 'meetup' },
    ]}
    mb="md"
  />
);
```

**Rich Text Editor → Mantine RichTextEditor:**
```tsx
import { RichTextEditor } from '@mantine/tiptap';

const DescriptionEditor = () => (
  <RichTextEditor editor={editor}>
    <RichTextEditor.Toolbar sticky stickyOffset={60}>
      <RichTextEditor.ControlsGroup>
        <RichTextEditor.Bold />
        <RichTextEditor.Italic />
        <RichTextEditor.Underline />
      </RichTextEditor.ControlsGroup>
    </RichTextEditor.Toolbar>

    <RichTextEditor.Content />
  </RichTextEditor>
);
```

**Image Upload → Mantine Dropzone:**
```tsx
import { Dropzone } from '@mantine/dropzone';

const ImageUpload = () => (
  <Dropzone
    onDrop={(files) => console.log('accepted files', files)}
    maxSize={10 * 1024 ** 2}
    accept={['image/png', 'image/jpeg']}
  >
    <Group justify="center" gap="xl" mih={220} style={{ pointerEvents: 'none' }}>
      <Dropzone.Accept>
        <IconUpload size="3.2rem" stroke={1.5} />
      </Dropzone.Accept>
      <Dropzone.Reject>
        <IconX size="3.2rem" stroke={1.5} />
      </Dropzone.Reject>
      <Dropzone.Idle>
        <IconPhoto size="3.2rem" stroke={1.5} />
      </Dropzone.Idle>

      <div>
        <Text size="xl" inline>
          Click to upload image or drag and drop
        </Text>
        <Text size="sm" c="dimmed" inline mt={7}>
          PNG, JPG up to 10MB
        </Text>
      </div>
    </Group>
  </Dropzone>
);
```

### 6. Check-in Interface

**HTML Check-in Layout → Mantine Layout with Real-time Components:**
```tsx
const CheckinInterface = () => (
  <Stack>
    {/* Header with Event Info and Countdown */}
    <Paper withBorder p="xl" bg="gradient-to-r from-wcr.6 to-wcr.8">
      <Group justify="space-between">
        <div>
          <Title order={1} c="white">
            Rope Basics Workshop - Check-in
          </Title>
          <Text c="gray.1">Saturday, March 15, 2024 • 2:00 PM - 5:00 PM</Text>
        </div>
        
        <Group gap="lg">
          <Paper p="md" radius="md" bg="rgba(255,255,255,0.1)">
            <Stack align="center" gap="xs">
              <Text size="xs" c="white" tt="uppercase">Time Until Start</Text>
              <Text size="xl" fw={700} c="white" ff="monospace">
                1:42:15
              </Text>
            </Stack>
          </Paper>
          
          <Button variant="outline" color="white">
            Exit Check-in
          </Button>
        </Group>
      </Group>
    </Paper>

    {/* Stats Row */}
    <SimpleGrid cols={4} spacing="lg">
      <Card withBorder>
        <Stack align="center">
          <Text size="3rem" fw={700} c="wcr.6">8</Text>
          <Text size="sm" c="dimmed" tt="uppercase">Not Arrived</Text>
        </Stack>
      </Card>
      {/* Additional stat cards... */}
    </SimpleGrid>

    {/* Search and Filters */}
    <Paper withBorder p="md">
      <Group justify="space-between">
        <TextInput
          placeholder="Search by name..."
          leftSection={<IconSearch size="1rem" />}
          w={300}
        />
        <Group gap="sm">
          <Button size="sm" variant={activeFilter === 'all' ? 'filled' : 'outline'}>
            All (12)
          </Button>
          <Button size="sm" variant={activeFilter === 'not-arrived' ? 'filled' : 'outline'}>
            Not Arrived (8)
          </Button>
          <Button size="sm" variant={activeFilter === 'need-waiver' ? 'filled' : 'outline'}>
            Need Waiver (2)
          </Button>
        </Group>
      </Group>
    </Paper>

    {/* Check-in Table */}
    <DataTable
      columns={[
        {
          accessor: 'name',
          title: 'Name',
          render: ({ name, pronouns, memberType }) => (
            <div>
              <Group gap="sm">
                <Text fw={500}>{name}</Text>
                {memberType && (
                  <Badge size="xs" color="gold">
                    {memberType}
                  </Badge>
                )}
              </Group>
              <Text size="sm" c="dimmed" fs="italic">
                {pronouns}
              </Text>
            </div>
          ),
        },
        {
          accessor: 'payment',
          title: 'Payment',
          render: ({ paymentStatus }) => (
            <Badge
              color={
                paymentStatus === 'paid' ? 'green' :
                paymentStatus === 'comp' ? 'blue' : 'red'
              }
              size="sm"
            >
              {paymentStatus}
            </Badge>
          ),
        },
        {
          accessor: 'waiver',
          title: 'Waiver',
          render: ({ waiverSigned }) => (
            <Badge
              color={waiverSigned ? 'green' : 'yellow'}
              size="sm"
            >
              {waiverSigned ? 'Signed' : 'Not Signed'}
            </Badge>
          ),
        },
        {
          accessor: 'status',
          title: 'Status',
          render: ({ checkedIn, onCheckIn }) => (
            checkedIn ? (
              <Group gap="xs">
                <IconCheck size="1rem" color="green" />
                <Text c="green" fw={500}>Checked In</Text>
              </Group>
            ) : (
              <Button
                size="sm"
                onClick={onCheckIn}
                gradient={{ from: 'yellow', to: 'orange' }}
                variant="gradient"
              >
                Check In
              </Button>
            )
          ),
        },
      ]}
      records={attendees}
      withTableBorder
      highlightOnHover
    />
  </Stack>
);
```

### 7. Public Events List

**HTML Event Cards → Mantine Grid with Cards:**
```tsx
const PublicEventsList = () => (
  <>
    {/* Hero Section */}
    <Box bg="gradient-to-r from-wcr.6 to-wcr.8" py="5rem">
      <Container>
        <Stack align="center" ta="center">
          <Title order={1} c="white" size="3rem">
            Explore Classes & Meetups
          </Title>
          <Text size="xl" c="gray.1">
            Learn rope bondage in a safe, inclusive environment
          </Text>
        </Stack>
      </Container>
    </Box>

    {/* Filter Bar */}
    <Paper withBorder p="md" pos="sticky" top={60} style={{ zIndex: 100 }}>
      <Container>
        <Group justify="space-between">
          <Button.Group>
            <Button variant="outline">Show Past Classes</Button>
          </Button.Group>
          
          <Group gap="md">
            <TextInput
              placeholder="Search events..."
              leftSection={<IconSearch size="1rem" />}
              w={250}
            />
            <Select
              placeholder="Sort by Date"
              data={[
                { value: 'date', label: 'Sort by Date' },
                { value: 'price', label: 'Sort by Price' },
                { value: 'availability', label: 'Sort by Availability' },
              ]}
            />
          </Group>
        </Group>
      </Container>
    </Paper>

    {/* Events Grid */}
    <Container py="xl">
      <SimpleGrid cols={{ base: 1, md: 2, lg: 3 }} spacing="lg">
        {events.map(event => (
          <EventCard key={event.id} event={event} />
        ))}
      </SimpleGrid>
    </Container>
  </>
);

const EventCard = ({ event }) => (
  <Card
    shadow="sm"
    withBorder
    radius="md"
    h="100%"
    style={{ 
      transition: 'all 0.3s ease',
      cursor: 'pointer',
    }}
    sx={{
      '&:hover': {
        transform: 'translateY(-4px)',
        boxShadow: theme.shadows.lg,
      }
    }}
  >
    {/* Event Header with Title */}
    <Card.Section
      bg="gradient-to-r from-wcr.6 to-wcr.8"
      p="md"
      pos="relative"
    >
      {event.memberOnly && (
        <Badge
          pos="absolute"
          top="sm"
          right="sm"
          color="purple"
          size="sm"
        >
          Members Only
        </Badge>
      )}
      <Title order={3} c="white" ta="center">
        {event.title}
      </Title>
    </Card.Section>

    <Card.Section p="md" style={{ flex: 1 }}>
      <Stack gap="md">
        <Text fw={700} c="wcr.6">
          {event.date} • {event.time}
        </Text>
        
        <Text c="dimmed">
          {event.description}
        </Text>
        
        <Group gap="md" c="dimmed" size="sm">
          <Group gap="xs">
            <IconClock size="1rem" />
            <Text size="sm">{event.duration}</Text>
          </Group>
          <Group gap="xs">
            <IconUser size="1rem" />
            <Text size="sm">{event.level}</Text>
          </Group>
        </Group>
      </Stack>
    </Card.Section>

    <Card.Section p="md" pt={0}>
      <Group justify="space-between" mb="md">
        <Text fw={700} c="wcr.6" size="lg">
          {event.price}
        </Text>
        <Text
          c={
            event.availability === 'available' ? 'green' :
            event.availability === 'limited' ? 'yellow' : 'red'
          }
          fw={500}
        >
          {event.availabilityText}
        </Text>
      </Group>
      
      <Group grow>
        <Button 
          component={Link} 
          to={`/events/${event.id}`}
          variant="gradient"
          gradient={{ from: 'yellow', to: 'orange' }}
        >
          Learn More
        </Button>
      </Group>
    </Card.Section>
  </Card>
);
```

## State Management Patterns

### Form State with Mantine Form
```tsx
import { useForm } from '@mantine/form';

const EventForm = () => {
  const form = useForm({
    initialValues: {
      title: '',
      description: '',
      date: new Date(),
      capacity: 20,
      price: 35,
      eventType: 'class',
    },
    
    validate: {
      title: (value) => value.length < 2 ? 'Title must have at least 2 letters' : null,
      capacity: (value) => value < 1 ? 'Capacity must be at least 1' : null,
    },
  });

  return (
    <form onSubmit={form.onSubmit((values) => console.log(values))}>
      <Stack>
        <TextInput
          required
          label="Event Title"
          placeholder="e.g., Rope Basics Workshop"
          {...form.getInputProps('title')}
        />
        
        <DateInput
          required
          label="Event Date"
          {...form.getInputProps('date')}
        />
        
        <NumberInput
          required
          label="Maximum Capacity"
          min={1}
          {...form.getInputProps('capacity')}
        />
        
        <Group justify="flex-end" mt="md">
          <Button type="submit">Create Event</Button>
        </Group>
      </Stack>
    </form>
  );
};
```

### Data Fetching with React Query
```tsx
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';

const useEvents = () => {
  return useQuery({
    queryKey: ['events'],
    queryFn: () => fetch('/api/events').then(res => res.json()),
  });
};

const useCheckInAttendee = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: (attendeeId: string) => 
      fetch(`/api/attendees/${attendeeId}/checkin`, { method: 'POST' }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['attendees'] });
    },
  });
};
```

## Responsive Design Patterns

### Mantine Responsive Breakpoints
```tsx
const ResponsiveLayout = () => (
  <SimpleGrid
    cols={{ base: 1, sm: 2, lg: 3 }}
    spacing={{ base: 'sm', lg: 'lg' }}
  >
    {/* Content */}
  </SimpleGrid>
);

// Hidden on mobile
<Box hiddenFrom="sm">Mobile only content</Box>

// Visible only on desktop
<Box visibleFrom="lg">Desktop only content</Box>

// Responsive text sizes
<Title order={{ base: 3, sm: 2, lg: 1 }}>
  Responsive Title
</Title>
```

## Color System Integration

### WitchCityRope Theme Colors
```tsx
// In your MantineProvider theme
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

// Usage in components
<Button color="wcr">Primary Action</Button>
<Badge color="wcr.6">Member Badge</Badge>
<Title c="wcr.7">Burgundy Title</Title>
```

## Icon System

### Tabler Icons (Mantine Standard)
```tsx
import {
  IconCalendar,
  IconUsers,
  IconEdit,
  IconTrash,
  IconPlus,
  IconSearch,
  IconCheck,
  IconX,
  IconClock,
  IconMapPin,
  IconMail,
} from '@tabler/icons-react';

// Usage
<ActionIcon>
  <IconEdit size="1rem" />
</ActionIcon>

<Button leftSection={<IconPlus size="1rem" />}>
  Create Event
</Button>
```

## Performance Optimization

### Code Splitting by Route
```tsx
import { lazy, Suspense } from 'react';
import { LoadingOverlay } from '@mantine/core';

const AdminDashboard = lazy(() => import('./AdminDashboard'));
const EventsList = lazy(() => import('./EventsList'));
const CheckinInterface = lazy(() => import('./CheckinInterface'));

const App = () => (
  <Suspense fallback={<LoadingOverlay visible />}>
    {/* Route components */}
  </Suspense>
);
```

### Virtual Scrolling for Large Lists
```tsx
import { DataTable } from 'mantine-datatable';

const LargeEventsTable = () => (
  <DataTable
    records={events}
    // Enable virtualization for large datasets
    withTableBorder
    highlightOnHover
    minHeight={600}
    // Pagination instead of virtualization for smaller datasets
    page={page}
    recordsPerPage={20}
    totalRecords={totalEvents}
    onPageChange={setPage}
  />
);
```

## Key Implementation Notes

1. **Use DataTable for Complex Tables**: The mantine-datatable package provides excellent data grid functionality that matches the complexity shown in wireframes.

2. **Leverage Mantine Form**: Built-in validation, error handling, and form state management.

3. **AppShell for Admin Layout**: Perfect match for the admin interface layout pattern.

4. **Responsive Design**: Mantine's responsive props system matches the mobile-first approach in wireframes.

5. **Theme Integration**: Custom WCR color scheme integrates seamlessly with Mantine's theming system.

6. **Component Composition**: Build complex interfaces by composing simple Mantine components rather than creating custom components.

7. **Accessibility Built-in**: Mantine components include WCAG 2.1 AA compliance by default.

8. **TypeScript First**: All components should use proper TypeScript interfaces that align with the API DTOs.

The existing wireframes provide excellent guidance for component structure and user flows - this mapping ensures a 1:1 translation to modern React/Mantine patterns while maintaining the design intent.
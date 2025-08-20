# Design Variation 4: Advanced Mantine (Dramatic Change)
<!-- Last Updated: 2025-08-20 -->
<!-- Version: 1.0 -->
<!-- Owner: UI Designer Agent -->
<!-- Status: Complete -->

## Design Overview

This variation fully leverages Mantine v7's advanced component ecosystem to create a feature-rich, highly interactive community platform. The design emphasizes sophisticated data visualization, advanced search capabilities, and rich user interactions while maintaining the alternative aesthetic through careful theming and animation choices.

**Edginess Level**: 4/5 (Feature-rich modern with sophisticated edge)
**Animation Level**: Moderate-to-High with smooth transitions and advanced interactions
**Implementation Complexity**: High - Full utilization of Mantine's advanced component library

## Advanced Component Integration

### Core Advanced Components Used
- **Spotlight**: Global search with command palette (Ctrl+K)
- **DataTable**: Sortable, filterable event and member listings
- **Timeline**: Event schedules and community milestones
- **Carousel**: Image galleries and featured content
- **Dropzone**: File uploads for profiles and event materials
- **Charts**: Community analytics and event statistics
- **DatePicker**: Advanced date selection for events
- **RichTextEditor**: Enhanced content creation
- **Notifications**: Advanced toast system with actions

### Enhanced Color Palette
```typescript
const advancedMantineTheme = createTheme({
  colors: {
    // Enhanced WitchCity palette with data visualization colors
    witchcity: [
      '#f8f4e6', // ivory
      '#f0e9dc', // warm cream
      '#e2d0c4', // light rose
      '#d4a5a5', // dusty rose
      '#c48b8b', // medium rose
      '#b76d75', // rose gold
      '#9b4a75', // plum
      '#880124', // burgundy (primary)
      '#660118', // dark burgundy
      '#2c1a1e'  // deep burgundy
    ],
    // Data visualization palette
    dataViz: [
      '#E3F2FD', // lightest blue
      '#BBDEFB', // light blue
      '#90CAF9', // medium blue
      '#64B5F6', // blue
      '#42A5F5', // primary blue
      '#2196F3', // strong blue
      '#1E88E5', // darker blue
      '#1976D2', // deep blue
      '#1565C0', // deeper blue
      '#0D47A1'  // darkest blue
    ],
    // Status colors for advanced features
    status: [
      '#F1F8E9', // success light
      '#DCEDC8', // success lighter
      '#C5E1A5', // success light
      '#AED581', // success
      '#9CCC65', // success medium
      '#8BC34A', // success primary
      '#7CB342', // success dark
      '#689F38', // success darker
      '#558B2F', // success darkest
      '#33691E'  // success deepest
    ]
  },
  primaryColor: 'witchcity',
  components: {
    Spotlight: {
      styles: {
        search: {
          backgroundColor: 'var(--mantine-color-dark-7)',
          border: '1px solid var(--mantine-color-witchcity-6)',
          '&:focus': {
            borderColor: 'var(--mantine-color-witchcity-4)'
          }
        }
      }
    },
    DataTable: {
      styles: {
        root: {
          backgroundColor: 'var(--mantine-color-dark-8)',
        },
        header: {
          backgroundColor: 'var(--mantine-color-witchcity-6)',
          color: 'white'
        }
      }
    }
  }
});
```

## Advanced Navigation System

### Spotlight Integration
- **Global Search**: Ctrl+K activated command palette
- **Quick Actions**: Event creation, member lookup, admin shortcuts
- **Smart Suggestions**: AI-powered search recommendations
- **Keyboard Navigation**: Full keyboard accessibility

#### Implementation
```typescript
import { Spotlight, spotlight } from '@mantine/spotlight';
import { IconSearch, IconHome, IconCalendar, IconUsers, IconSettings } from '@tabler/icons-react';

const AdvancedNavigation = () => {
  const spotlightActions = [
    {
      id: 'events',
      label: 'Browse Events',
      description: 'View upcoming rope classes and workshops',
      onClick: () => navigate('/events'),
      leftSection: <IconCalendar size={18} />
    },
    {
      id: 'members',
      label: 'Member Directory',
      description: 'Connect with community members',
      onClick: () => navigate('/members'),
      leftSection: <IconUsers size={18} />
    },
    {
      id: 'create-event',
      label: 'Create Event',
      description: 'Schedule a new class or workshop',
      onClick: () => navigate('/events/create'),
      leftSection: <IconCalendar size={18} />
    },
    {
      id: 'admin',
      label: 'Admin Dashboard',
      description: 'Access administrative functions',
      onClick: () => navigate('/admin'),
      leftSection: <IconSettings size={18} />
    }
  ];

  return (
    <Spotlight
      actions={spotlightActions}
      searchProps={{
        leftSection: <IconSearch size={20} />,
        placeholder: 'Search WitchCityRope...'
      }}
      highlightQuery
      scrollable
      maxHeight={300}
      radius="md"
      styles={{
        root: {
          backgroundColor: 'var(--mantine-color-dark-7)'
        },
        action: {
          '&[data-hovered]': {
            backgroundColor: 'var(--mantine-color-witchcity-6)'
          }
        }
      }}
    />
  );
};
```

### Mega Menu Navigation
- **Hierarchical Menus**: Multi-level navigation for complex features
- **Visual Previews**: Event thumbnails and member avatars in dropdowns
- **Quick Stats**: Member counts, upcoming events in navigation
- **Role-Based Sections**: Dynamic menu based on user permissions

#### Implementation
```typescript
import { AppShell, NavLink, Menu, Group, Avatar, Badge, Text, SimpleGrid } from '@mantine/core';

const MegaMenuNavigation = () => {
  const { user, hasRole } = useAuth();
  
  return (
    <AppShell.Navbar p="md" w={300}>
      <Group justify="space-between" mb="md">
        <Avatar.Group spacing="sm">
          <Avatar src={user?.avatar} size="lg" />
          <div>
            <Text fw={500}>{user?.sceneName}</Text>
            <Badge size="xs" color="witchcity">{user?.role}</Badge>
          </div>
        </Avatar.Group>
      </Group>
      
      <NavLink
        label="Dashboard"
        leftSection={<IconDashboard size={18} />}
        rightSection={
          <Badge size="xs" color="status.5">
            {user?.notifications?.length || 0}
          </Badge>
        }
      />
      
      <Menu width={400} position="right-start">
        <Menu.Target>
          <NavLink
            label="Events & Classes"
            leftSection={<IconCalendar size={18} />}
            rightSection={<IconChevronRight size={14} />}
          />
        </Menu.Target>
        <Menu.Dropdown>
          <Menu.Label>Upcoming This Week</Menu.Label>
          <SimpleGrid cols={2} spacing="xs" p="sm">
            {upcomingEvents.slice(0, 4).map(event => (
              <Card key={event.id} p="xs" radius="sm" withBorder>
                <Text size="xs" fw={500}>{event.title}</Text>
                <Text size="xs" c="dimmed">{event.date}</Text>
              </Card>
            ))}
          </SimpleGrid>
          <Menu.Divider />
          <Menu.Item leftSection={<IconCalendar size={16} />}>
            View All Events
          </Menu.Item>
          <Menu.Item leftSection={<IconPlus size={16} />}>
            Create Event
          </Menu.Item>
        </Menu.Dropdown>
      </Menu>
      
      {hasRole('admin') && (
        <Menu width={350} position="right-start">
          <Menu.Target>
            <NavLink
              label="Admin Tools"
              leftSection={<IconSettings size={18} />}
              rightSection={<IconChevronRight size={14} />}
            />
          </Menu.Target>
          <Menu.Dropdown>
            <Menu.Item 
              leftSection={<IconUsers size={16} />}
              rightSection={<Badge size="xs">{pendingVetting}</Badge>}
            >
              Vetting Queue
            </Menu.Item>
            <Menu.Item leftSection={<IconChartBar size={16} />}>
              Analytics Dashboard
            </Menu.Item>
            <Menu.Item leftSection={<IconFlag size={16} />}>
              Incident Reports
            </Menu.Item>
          </Menu.Dropdown>
        </Menu>
      )}
    </AppShell.Navbar>
  );
};
```

## Advanced Event Management

### DataTable Implementation
- **Sortable Columns**: Multi-column sorting with clear indicators
- **Advanced Filtering**: Date ranges, member levels, event types
- **Bulk Actions**: Multi-select for batch operations
- **Export Functionality**: CSV/PDF export capabilities

#### Implementation
```typescript
import { DataTable } from 'mantine-datatable';
import { useState } from 'react';

const AdvancedEventTable = () => {
  const [selectedRecords, setSelectedRecords] = useState([]);
  const [sortStatus, setSortStatus] = useState({
    columnAccessor: 'date',
    direction: 'asc'
  });

  const columns = [
    {
      accessor: 'title',
      title: 'Event Title',
      sortable: true,
      render: ({ title, image }) => (
        <Group gap="sm">
          <Avatar src={image} size="sm" radius="md" />
          <Text fw={500}>{title}</Text>
        </Group>
      )
    },
    {
      accessor: 'date',
      title: 'Date & Time',
      sortable: true,
      render: ({ date, startTime }) => (
        <Stack gap={0}>
          <Text size="sm" fw={500}>{formatDate(date)}</Text>
          <Text size="xs" c="dimmed">{startTime}</Text>
        </Stack>
      )
    },
    {
      accessor: 'instructor',
      title: 'Instructor',
      render: ({ instructor }) => (
        <Group gap="xs">
          <Avatar src={instructor.avatar} size="xs" />
          <Text size="sm">{instructor.name}</Text>
        </Group>
      )
    },
    {
      accessor: 'capacity',
      title: 'Capacity',
      render: ({ registrations, maxCapacity }) => (
        <Group gap="xs">
          <Progress
            value={(registrations / maxCapacity) * 100}
            size="sm"
            color={
              registrations / maxCapacity > 0.8 ? 'red' :
              registrations / maxCapacity > 0.6 ? 'yellow' : 'green'
            }
            w={60}
          />
          <Text size="sm">{registrations}/{maxCapacity}</Text>
        </Group>
      )
    },
    {
      accessor: 'status',
      title: 'Status',
      render: ({ status }) => (
        <Badge
          color={
            status === 'confirmed' ? 'green' :
            status === 'cancelled' ? 'red' : 'yellow'
          }
          variant="light"
        >
          {status}
        </Badge>
      )
    },
    {
      accessor: 'actions',
      title: 'Actions',
      textAlign: 'center',
      render: (event) => (
        <Group gap="xs" justify="center">
          <ActionIcon variant="light" color="blue" size="sm">
            <IconEdit size={16} />
          </ActionIcon>
          <ActionIcon variant="light" color="red" size="sm">
            <IconTrash size={16} />
          </ActionIcon>
        </Group>
      )
    }
  ];

  return (
    <DataTable
      records={events}
      columns={columns}
      selectedRecords={selectedRecords}
      onSelectedRecordsChange={setSelectedRecords}
      sortStatus={sortStatus}
      onSortStatusChange={setSortStatus}
      totalRecords={events.length}
      recordsPerPage={20}
      page={page}
      onPageChange={setPage}
      highlightOnHover
      withBorder
      borderRadius="md"
      shadow="sm"
      styles={{
        root: {
          backgroundColor: 'var(--mantine-color-dark-8)'
        },
        header: {
          backgroundColor: 'var(--mantine-color-witchcity-6)',
          color: 'white'
        },
        pagination: {
          backgroundColor: 'var(--mantine-color-dark-7)'
        }
      }}
    />
  );
};
```

### Advanced Event Creation
- **Multi-Step Wizard**: Progressive event creation process
- **Rich Text Editing**: Enhanced description editing
- **File Upload**: Image and document management
- **Calendar Integration**: Visual date/time selection

#### Implementation
```typescript
import { Stepper, RichTextEditor, Dropzone, DateTimePicker } from '@mantine/core';
import { useState } from 'react';

const AdvancedEventCreation = () => {
  const [active, setActive] = useState(0);
  const [eventData, setEventData] = useState({});

  return (
    <Container size="md">
      <Stepper 
        active={active} 
        onStepClick={setActive}
        breakpoint="sm"
        styles={{
          step: {
            '&[data-completed]': {
              backgroundColor: 'var(--mantine-color-witchcity-6)'
            }
          }
        }}
      >
        <Stepper.Step label="Basic Info" description="Event details">
          <Stack gap="md">
            <TextInput
              label="Event Title"
              placeholder="Rope Fundamentals Workshop"
              required
              size="md"
            />
            
            <Group grow>
              <DateTimePicker
                label="Start Date & Time"
                placeholder="Select date and time"
                required
                size="md"
              />
              <DateTimePicker
                label="End Date & Time"
                placeholder="Select end time"
                required
                size="md"
              />
            </Group>
            
            <Select
              label="Event Type"
              placeholder="Select event type"
              data={[
                { value: 'workshop', label: 'Workshop' },
                { value: 'jam', label: 'Rope Jam' },
                { value: 'performance', label: 'Performance' },
                { value: 'social', label: 'Social Event' }
              ]}
              required
              size="md"
            />
          </Stack>
        </Stepper.Step>

        <Stepper.Step label="Description" description="Event content">
          <Stack gap="md">
            <RichTextEditor>
              <RichTextEditor.Toolbar>
                <RichTextEditor.ControlsGroup>
                  <RichTextEditor.Bold />
                  <RichTextEditor.Italic />
                  <RichTextEditor.Underline />
                  <RichTextEditor.Strikethrough />
                  <RichTextEditor.ClearFormatting />
                  <RichTextEditor.Code />
                </RichTextEditor.ControlsGroup>

                <RichTextEditor.ControlsGroup>
                  <RichTextEditor.H1 />
                  <RichTextEditor.H2 />
                  <RichTextEditor.H3 />
                  <RichTextEditor.H4 />
                </RichTextEditor.ControlsGroup>

                <RichTextEditor.ControlsGroup>
                  <RichTextEditor.Blockquote />
                  <RichTextEditor.Hr />
                  <RichTextEditor.BulletList />
                  <RichTextEditor.OrderedList />
                </RichTextEditor.ControlsGroup>
              </RichTextEditor.Toolbar>

              <RichTextEditor.Content 
                style={{ minHeight: 200 }}
                placeholder="Describe your event in detail..."
              />
            </RichTextEditor>
          </Stack>
        </Stepper.Step>

        <Stepper.Step label="Media & Files" description="Images and documents">
          <Stack gap="md">
            <Dropzone
              onDrop={(files) => console.log('Event images:', files)}
              accept={IMAGE_MIME_TYPE}
              multiple
            >
              <Group justify="center" gap="xl" mih={220} style={{ pointerEvents: 'none' }}>
                <Dropzone.Accept>
                  <IconUpload size={52} color="var(--mantine-color-blue-6)" />
                </Dropzone.Accept>
                <Dropzone.Reject>
                  <IconX size={52} color="var(--mantine-color-red-6)" />
                </Dropzone.Reject>
                <Dropzone.Idle>
                  <IconPhoto size={52} color="var(--mantine-color-dimmed)" />
                </Dropzone.Idle>

                <div>
                  <Text size="xl" inline>
                    Drag event images here or click to select files
                  </Text>
                  <Text size="sm" c="dimmed" inline mt={7}>
                    Attach up to 5 images, each file should not exceed 5mb
                  </Text>
                </div>
              </Group>
            </Dropzone>
          </Stack>
        </Stepper.Step>

        <Stepper.Completed>
          <Paper p="xl" radius="md" withBorder>
            <Stack align="center" gap="md">
              <IconCheck size={64} color="var(--mantine-color-green-6)" />
              <Title order={2}>Event Created Successfully!</Title>
              <Text ta="center" c="dimmed">
                Your event has been created and is now visible to community members.
              </Text>
              <Group>
                <Button variant="outline">View Event</Button>
                <Button color="witchcity">Create Another Event</Button>
              </Group>
            </Stack>
          </Paper>
        </Stepper.Completed>
      </Stepper>

      <Group justify="center" mt="xl">
        <Button variant="default" onClick={prevStep} disabled={active === 0}>
          Back
        </Button>
        <Button color="witchcity" onClick={nextStep} disabled={active === 3}>
          Next
        </Button>
      </Group>
    </Container>
  );
};
```

## Community Analytics Dashboard

### Interactive Charts
- **Event Attendance Trends**: Line charts with hover details
- **Member Growth**: Area charts with time-based filtering
- **Event Type Distribution**: Pie charts with drill-down capability
- **Geographic Distribution**: Heatmap of member locations

#### Implementation
```typescript
import { AreaChart, LineChart, PieChart, BarChart } from '@mantine/charts';
import { SimpleGrid, Paper, Title, Text, Group, Badge } from '@mantine/core';

const CommunityAnalytics = () => {
  const attendanceData = [
    { month: 'Jan', events: 12, attendance: 156 },
    { month: 'Feb', events: 15, attendance: 203 },
    { month: 'Mar', events: 18, attendance: 245 },
    { month: 'Apr', events: 22, attendance: 289 },
    { month: 'May', events: 20, attendance: 267 },
    { month: 'Jun', events: 25, attendance: 334 }
  ];

  const memberGrowthData = [
    { month: 'Jan', total: 487, new: 23 },
    { month: 'Feb', total: 512, new: 25 },
    { month: 'Mar', total: 543, new: 31 },
    { month: 'Apr', total: 578, new: 35 },
    { month: 'May', total: 601, new: 23 },
    { month: 'Jun', total: 628, new: 27 }
  ];

  return (
    <SimpleGrid cols={{ base: 1, md: 2 }} spacing="lg">
      <Paper p="md" radius="md" withBorder>
        <Group justify="space-between" mb="md">
          <Title order={3}>Event Attendance</Title>
          <Badge color="status.5" variant="light">+12% vs last month</Badge>
        </Group>
        <LineChart
          h={300}
          data={attendanceData}
          dataKey="month"
          series={[
            { name: 'attendance', color: 'witchcity.6' },
            { name: 'events', color: 'dataViz.5' }
          ]}
          curveType="natural"
          withLegend
          legendProps={{ verticalAlign: 'bottom', height: 50 }}
          gridAxis="xy"
          withTooltip
        />
      </Paper>

      <Paper p="md" radius="md" withBorder>
        <Group justify="space-between" mb="md">
          <Title order={3}>Member Growth</Title>
          <Badge color="status.5" variant="light">628 total members</Badge>
        </Group>
        <AreaChart
          h={300}
          data={memberGrowthData}
          dataKey="month"
          series={[
            { name: 'total', color: 'witchcity.6' },
            { name: 'new', color: 'status.5' }
          ]}
          withLegend
          legendProps={{ verticalAlign: 'bottom', height: 50 }}
          withTooltip
          fillOpacity={0.7}
        />
      </Paper>

      <Paper p="md" radius="md" withBorder>
        <Title order={3} mb="md">Event Types</Title>
        <PieChart
          h={300}
          data={[
            { name: 'Workshops', value: 45, color: 'witchcity.6' },
            { name: 'Rope Jams', value: 30, color: 'dataViz.5' },
            { name: 'Performances', value: 15, color: 'status.5' },
            { name: 'Social Events', value: 10, color: 'accent.4' }
          ]}
          withTooltip
          withLabelsLine
          labelsPosition="outside"
          labelsType="percent"
          withLabels
        />
      </Paper>

      <Paper p="md" radius="md" withBorder>
        <Title order={3} mb="md">Monthly Revenue</Title>
        <BarChart
          h={300}
          data={attendanceData.map(item => ({
            month: item.month,
            revenue: item.attendance * 45 // Average event price
          }))}
          dataKey="month"
          series={[{ name: 'revenue', color: 'status.5' }]}
          withTooltip
          valueFormatter={(value) => `$${value.toLocaleString()}`}
        />
      </Paper>
    </SimpleGrid>
  );
};
```

## Advanced User Experience Features

### Timeline Component
- **Community Milestones**: Major events and achievements
- **Personal Journey**: Individual member progress tracking
- **Event History**: Chronological event participation

#### Implementation
```typescript
import { Timeline, Avatar, Text, Paper, Badge } from '@mantine/core';

const CommunityTimeline = () => (
  <Timeline active={1} bulletSize={24} lineWidth={2}>
    <Timeline.Item
      bullet={<IconGift size={12} />}
      title="Community Founded"
      color="witchcity"
    >
      <Text c="dimmed" size="sm">
        WitchCityRope was established in Salem, creating a safe space for rope education
      </Text>
      <Text size="xs" mt={4} c="dimmed">
        January 2020
      </Text>
    </Timeline.Item>

    <Timeline.Item
      bullet={<IconUsers size={12} />}
      title="100 Members Milestone"
      color="status"
    >
      <Text c="dimmed" size="sm">
        Our community reached 100 active members committed to education and safety
      </Text>
      <Text size="xs" mt={4} c="dimmed">
        August 2021
      </Text>
    </Timeline.Item>

    <Timeline.Item
      title="First Annual Rope Convention"
      bullet={<IconCalendar size={12} />}
      lineVariant="dashed"
      color="dataViz"
    >
      <Text c="dimmed" size="sm">
        Hosted our first multi-day convention featuring national instructors
      </Text>
      <Text size="xs" mt={4} c="dimmed">
        October 2022
      </Text>
    </Timeline.Item>

    <Timeline.Item
      title="Platform Modernization"
      bullet={<IconCode size={12} />}
      color="accent"
    >
      <Text c="dimmed" size="sm">
        Launched new React-based platform with enhanced member features
      </Text>
      <Text size="xs" mt={4} c="dimmed">
        Present
      </Text>
    </Timeline.Item>
  </Timeline>
);
```

### Advanced Notifications
- **Rich Notifications**: Multi-action toast notifications
- **Persistent Alerts**: Important community announcements
- **Interactive Confirmations**: In-context decision making

#### Implementation
```typescript
import { notifications } from '@mantine/notifications';
import { Button, Group } from '@mantine/core';

const advancedNotifications = {
  eventReminder: () => {
    notifications.show({
      id: 'event-reminder',
      title: 'Rope Fundamentals Workshop',
      message: 'Your workshop starts in 30 minutes. Don\'t forget your rope!',
      color: 'witchcity',
      icon: <IconCalendar size={18} />,
      autoClose: 10000,
      withCloseButton: true,
      action: (
        <Group gap="xs">
          <Button 
            size="xs" 
            variant="outline" 
            onClick={() => navigate('/events/123')}
          >
            View Details
          </Button>
          <Button 
            size="xs" 
            color="witchcity"
            onClick={() => notifications.hide('event-reminder')}
          >
            Got it
          </Button>
        </Group>
      )
    });
  },

  membershipApproval: () => {
    notifications.show({
      title: 'New Membership Application',
      message: 'Sarah M. has applied for vetted membership status',
      color: 'status',
      icon: <IconUserCheck size={18} />,
      autoClose: false,
      action: (
        <Group gap="xs">
          <Button size="xs" color="red" variant="outline">
            Decline
          </Button>
          <Button size="xs" color="green">
            Approve
          </Button>
        </Group>
      )
    });
  }
};
```

## Performance Optimization

### Component Virtualization
```typescript
import { FixedSizeList as List } from 'react-window';

const VirtualizedMemberList = ({ members }) => {
  const Row = ({ index, style }) => (
    <div style={style}>
      <MemberCard member={members[index]} />
    </div>
  );

  return (
    <List
      height={600}
      itemCount={members.length}
      itemSize={100}
      width="100%"
    >
      {Row}
    </List>
  );
};
```

### Advanced Caching
```typescript
import { QueryClient, useQuery } from '@tanstack/react-query';

const useEvents = (filters) => {
  return useQuery({
    queryKey: ['events', filters],
    queryFn: () => fetchEvents(filters),
    staleTime: 5 * 60 * 1000, // 5 minutes
    cacheTime: 10 * 60 * 1000, // 10 minutes
    select: (data) => data.map(transformEventData)
  });
};
```

## Accessibility Excellence

### Advanced A11y Features
```typescript
const AccessibleDataTable = () => (
  <DataTable
    aria-label="Community events table"
    columns={columns.map(col => ({
      ...col,
      titleProps: { 'aria-sort': getSortDirection(col.accessor) }
    }))}
    records={events}
    onRowClick={(event) => {
      // Announce row selection
      announceToScreenReader(`Selected ${event.title}`);
    }}
    highlightOnHover
    rowClassName={({ selected }) => selected ? 'selected-row' : ''}
  />
);

const announceToScreenReader = (message) => {
  const announcement = document.createElement('div');
  announcement.setAttribute('aria-live', 'polite');
  announcement.setAttribute('aria-atomic', 'true');
  announcement.setAttribute('class', 'sr-only');
  announcement.textContent = message;
  document.body.appendChild(announcement);
  setTimeout(() => document.body.removeChild(announcement), 1000);
};
```

This advanced Mantine variation showcases the full potential of Mantine v7's component ecosystem while maintaining the alternative aesthetic through careful theming and sophisticated interactions. The implementation demonstrates how advanced UI components can enhance community platform functionality without compromising the authentic rope bondage community culture.
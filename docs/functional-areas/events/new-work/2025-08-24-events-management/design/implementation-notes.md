# Implementation Notes: Events Management React/Mantine Integration

<!-- Last Updated: 2025-08-24 -->
<!-- Version: 1.0 -->
<!-- Owner: UI Designer Agent -->
<!-- Status: Ready for Development -->

## Overview

Implementation notes and minor changes from the original HTML wireframes to React/Mantine v7 components. The wireframes are 90% complete and provide excellent guidance - these notes cover the 10% of adaptation needed for modern React patterns.

## Key Changes from Original Wireframes

### 1. Layout System Migration

**Original**: Custom CSS Grid and Flexbox
**New**: Mantine AppShell + Layout Components

**Why**: Mantine AppShell provides better responsive behavior and built-in navigation patterns.

```tsx
// Original wireframe had custom header/sidebar CSS
// New: Mantine AppShell pattern
<AppShell
  header={{ height: 70 }}
  navbar={{ width: 260, breakpoint: 'sm', collapsed: { mobile: !opened } }}
  padding="md"
>
  <AppShell.Header>
    <AdminHeader />
  </AppShell.Header>
  
  <AppShell.Navbar>
    <AdminNavigation />
  </AppShell.Navbar>
  
  <AppShell.Main>
    <EventsDashboard />
  </AppShell.Main>
</AppShell>
```

### 2. Data Tables Enhancement

**Original**: HTML tables with custom styling
**New**: Mantine DataTable with advanced features

**Enhancements**:
- Built-in sorting indicators (arrows)
- Pagination controls
- Loading states
- Empty states
- Row selection for bulk operations
- Better mobile responsive behavior

**No Visual Changes**: Maintains exact same layout and styling from wireframes.

```tsx
// Maintains wireframe design but adds functionality
<DataTable
  // Visual matches wireframe exactly
  withTableBorder
  withColumnBorders
  striped
  highlightOnHover
  
  // Enhanced functionality not in wireframe
  sortStatus={sortStatus}
  onSortStatusChange={setSortStatus}
  selectedRecords={selectedRecords}
  onSelectedRecordsChange={setSelectedRecords}
  page={page}
  recordsPerPage={20}
  onPageChange={setPage}
  
  // Same visual content from wireframe
  columns={eventsColumns}
  records={events}
/>
```

### 3. Form Improvements

**Original**: Basic HTML forms
**New**: Mantine Form with validation

**Enhancements**:
- Real-time validation
- Better error display
- Improved accessibility
- TypeScript integration

**Visual Consistency**: All form styling matches wireframe designs.

```tsx
// Matches wireframe design with enhanced functionality
const EventForm = () => {
  const form = useForm({
    initialValues: { /* ... */ },
    validate: {
      title: (value) => value.length < 2 ? 'Title must have at least 2 letters' : null,
      capacity: (value) => value < 1 ? 'Capacity must be at least 1' : null,
    },
  });

  return (
    <form onSubmit={form.onSubmit(handleSubmit)}>
      {/* Same visual layout as wireframe */}
      <TextInput
        label="Event Title"
        placeholder="e.g., Rope Basics Workshop"
        {...form.getInputProps('title')}
        // Mantine adds better error handling and accessibility
      />
    </form>
  );
};
```

### 4. Rich Text Editor Integration

**Original**: Custom toolbar with contenteditable div
**New**: Mantine TipTap integration

**Why**: Better cross-browser compatibility, accessibility, and functionality.

```tsx
// Replaces custom editor from wireframe
import { RichTextEditor } from '@mantine/tiptap';

<RichTextEditor editor={editor}>
  <RichTextEditor.Toolbar sticky>
    {/* Same toolbar buttons as wireframe */}
    <RichTextEditor.ControlsGroup>
      <RichTextEditor.Bold />
      <RichTextEditor.Italic />
      <RichTextEditor.Underline />
      <RichTextEditor.Link />
      <RichTextEditor.BulletList />
      <RichTextEditor.OrderedList />
    </RichTextEditor.ControlsGroup>
  </RichTextEditor.Toolbar>

  <RichTextEditor.Content />
</RichTextEditor>
```

### 5. File Upload Enhancement

**Original**: Custom dropzone styling
**New**: Mantine Dropzone component

**Benefits**: Better file type validation, progress indicators, error handling.

```tsx
// Maintains wireframe visual design
<Dropzone
  onDrop={handleImageUpload}
  maxSize={10 * 1024 ** 2}
  accept={IMAGE_MIME_TYPE}
  styles={{
    root: {
      // Matches wireframe styling
      border: '2px dashed var(--mantine-color-gray-3)',
      borderRadius: 'var(--mantine-radius-md)',
      padding: 'var(--mantine-spacing-xl)',
      textAlign: 'center',
      backgroundColor: 'var(--mantine-color-gray-0)',
    }
  }}
>
  {/* Same content as wireframe */}
  <Group justify="center" gap="xl" mih={220}>
    <IconPhoto size="3.2rem" stroke={1.5} />
    <div>
      <Text size="xl">Click to upload image or drag and drop</Text>
      <Text size="sm" c="dimmed">PNG, JPG up to 10MB</Text>
    </div>
  </Group>
</Dropzone>
```

## Mobile Responsiveness Enhancements

### Responsive Grid System

**Original**: CSS media queries
**New**: Mantine responsive props

```tsx
// Original wireframe mobile patterns enhanced
<SimpleGrid
  cols={{ base: 1, sm: 2, lg: 4 }} // Stats cards
  spacing={{ base: 'sm', lg: 'lg' }}
>
  {statsCards}
</SimpleGrid>

<SimpleGrid
  cols={{ base: 1, md: 2, lg: 3 }} // Event cards
  spacing="lg"
>
  {eventCards}
</SimpleGrid>
```

### Mobile Navigation

**Original**: Custom mobile menu
**New**: Mantine AppShell with built-in mobile support

```tsx
// Enhanced mobile behavior
<AppShell
  navbar={{
    width: 260,
    breakpoint: 'sm',
    collapsed: { mobile: !mobileOpened }, // Built-in mobile collapse
  }}
>
  {/* Navigation automatically adapts */}
</AppShell>
```

### Table Mobile Optimization

**Original**: Horizontal scroll
**New**: Responsive column hiding + mobile-optimized view

```tsx
const columns = [
  { 
    accessor: 'title', 
    title: 'Event',
    // Always visible
  },
  { 
    accessor: 'instructor', 
    title: 'Instructor',
    // Hidden on mobile, visible on tablet+
    visibleFrom: 'sm',
  },
  { 
    accessor: 'capacity', 
    title: 'Capacity',
    // Hidden on mobile/tablet, visible on desktop
    visibleFrom: 'md',
  },
];
```

## State Management Integration

### Form State

**Original**: Basic form handling
**New**: Mantine Form + React Hook Form integration

```tsx
// Enhanced form state management
const useEventForm = (initialData?) => {
  const form = useForm({
    initialValues: initialData || {
      title: '',
      description: '',
      date: new Date(),
      eventType: 'class',
    },
    validate: zodResolver(eventSchema), // TypeScript validation
  });

  return form;
};
```

### Data Fetching

**Original**: Static data display
**New**: React Query integration

```tsx
// Real-time data with proper loading states
const useEventsData = () => {
  return useQuery({
    queryKey: ['events', filters],
    queryFn: () => eventsApi.getEvents(filters),
    refetchInterval: 30000, // Real-time updates for check-in
  });
};

// Optimistic updates for check-in
const useCheckIn = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: eventsApi.checkInAttendee,
    onMutate: async (attendeeId) => {
      // Optimistic update matches wireframe behavior
      await queryClient.cancelQueries(['attendees']);
      const previousAttendees = queryClient.getQueryData(['attendees']);
      
      queryClient.setQueryData(['attendees'], (old) =>
        old.map(attendee =>
          attendee.id === attendeeId
            ? { ...attendee, checkedIn: true }
            : attendee
        )
      );
      
      return { previousAttendees };
    },
    onError: (err, attendeeId, context) => {
      // Rollback on error
      queryClient.setQueryData(['attendees'], context.previousAttendees);
    },
  });
};
```

## Animation and Interaction Enhancements

### Button Interactions

**Original**: CSS hover effects
**New**: Mantine built-in animations + custom enhancements

```tsx
// Maintains wireframe styling with enhanced interactions
<Button
  variant="gradient"
  gradient={{ from: 'yellow', to: 'orange' }}
  size="sm"
  leftSection={<IconPlus size="1rem" />}
  styles={{
    root: {
      transition: 'all 0.3s ease',
      '&:hover': {
        transform: 'translateY(-2px)',
        boxShadow: '0 6px 25px rgba(255, 191, 0, 0.5)',
      }
    }
  }}
>
  Check In
</Button>
```

### Card Hover Effects

**Original**: Basic hover effects
**New**: Enhanced Mantine Card interactions

```tsx
<Card
  shadow="sm"
  withBorder
  radius="md"
  styles={{
    root: {
      transition: 'all 0.3s ease',
      cursor: 'pointer',
      '&:hover': {
        transform: 'translateY(-4px)',
        boxShadow: 'var(--mantine-shadow-lg)',
        borderColor: 'var(--mantine-color-wcr-6)',
      }
    }
  }}
>
  {/* Same content as wireframe */}
</Card>
```

## Accessibility Improvements

### Enhanced ARIA Labels

**Original**: Basic accessibility
**New**: Comprehensive ARIA integration

```tsx
// Example: Enhanced action buttons
<ActionIcon
  variant="light"
  color="blue"
  aria-label={`Edit event: ${event.title}`}
  onClick={() => handleEdit(event.id)}
>
  <IconEdit size="1rem" />
</ActionIcon>

// Enhanced table
<DataTable
  // Built-in accessibility features
  withTableBorder
  highlightOnHover
  records={events}
  columns={columns}
  // Screen reader support
  aria-label="Events management table"
/>
```

### Keyboard Navigation

**Original**: Basic tab navigation
**New**: Full keyboard support

```tsx
// Enhanced keyboard handling
const EventCard = ({ event, onSelect }) => {
  return (
    <Card
      tabIndex={0}
      onKeyDown={(e) => {
        if (e.key === 'Enter' || e.key === ' ') {
          e.preventDefault();
          onSelect(event.id);
        }
      }}
      // Visual focus indicators
      styles={{
        root: {
          '&:focus': {
            outline: '2px solid var(--mantine-color-wcr-6)',
            outlineOffset: '2px',
          }
        }
      }}
    >
      {/* Card content */}
    </Card>
  );
};
```

## Performance Optimizations

### Code Splitting

```tsx
// Route-based code splitting
const AdminDashboard = lazy(() => import('./AdminDashboard'));
const EventCreation = lazy(() => import('./EventCreation'));
const CheckinInterface = lazy(() => import('./CheckinInterface'));

// Component-based code splitting for large tables
const EventsDataTable = lazy(() => import('./EventsDataTable'));
```

### Virtualization for Large Lists

```tsx
// For large event lists (future enhancement)
import { Virtuoso } from 'react-virtuoso';

const LargeEventsList = () => (
  <Virtuoso
    data={events}
    itemContent={(index, event) => <EventCard key={event.id} event={event} />}
    style={{ height: '600px' }}
  />
);
```

### Memoization

```tsx
// Optimize expensive calculations
const EventCard = memo(({ event }) => {
  const formattedDate = useMemo(
    () => dayjs(event.date).format('MMM D, YYYY'),
    [event.date]
  );

  const statusColor = useMemo(() => {
    return event.status === 'published' ? 'green' : 
           event.status === 'draft' ? 'yellow' : 'red';
  }, [event.status]);

  return (
    // Card content using memoized values
  );
});
```

## TypeScript Integration

### Strict Type Safety

```tsx
// Generated types from API (NSwag integration)
import { Event, EventCreateRequest, EventUpdateRequest } from '@/api/types';

// Component props with strict typing
interface EventsTableProps {
  events: Event[];
  loading: boolean;
  onEdit: (eventId: string) => void;
  onDelete: (eventId: string) => void;
  onCheckIn: (eventId: string) => void;
}

// Form handling with type safety
const useEventForm = (event?: Event) => {
  const form = useForm<EventCreateRequest>({
    initialValues: {
      title: event?.title ?? '',
      description: event?.description ?? '',
      date: event?.date ?? new Date(),
      capacity: event?.capacity ?? 20,
      eventType: event?.eventType ?? 'class',
    },
  });

  return form;
};
```

## Testing Considerations

### Component Testing

```tsx
// Example test structure
describe('EventsDataTable', () => {
  it('renders events correctly', () => {
    render(
      <EventsDataTable 
        events={mockEvents} 
        loading={false}
        onEdit={mockOnEdit}
        onDelete={mockOnDelete}
        onCheckIn={mockOnCheckIn}
      />
    );

    expect(screen.getByText('Introduction to Rope Bondage')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /edit event/i })).toBeInTheDocument();
  });

  it('handles check-in functionality', async () => {
    const user = userEvent.setup();
    render(/* component */);
    
    await user.click(screen.getByRole('button', { name: /check in/i }));
    
    expect(mockOnCheckIn).toHaveBeenCalledWith('event-id');
  });
});
```

## Migration Strategy

### Phase 1: Core Tables
1. Admin events management table
2. Basic filtering and search
3. CRUD operations

### Phase 2: Advanced Features
1. Check-in interface with real-time updates
2. Event creation forms
3. File uploads

### Phase 3: Enhancements
1. Advanced filtering
2. Bulk operations
3. Performance optimizations

## Summary

The existing wireframes provide excellent guidance and require minimal changes for React/Mantine implementation. The key improvements are:

1. **Enhanced functionality** without visual changes
2. **Better accessibility** and keyboard navigation
3. **Improved mobile experience** with responsive patterns
4. **Type safety** with TypeScript integration
5. **Performance optimizations** for large datasets
6. **Real-time updates** for check-in interface

All visual designs and user flows remain exactly as specified in the wireframes - these notes only cover technical implementation details that enhance the user experience while maintaining design fidelity.
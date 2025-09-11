# Functional Specification: Admin Events Dashboard Enhancement
<!-- Last Updated: 2025-09-11 -->
<!-- Version: 1.0 -->
<!-- Owner: Functional Specification Agent -->
<!-- Status: Draft -->

## Architecture Discovery Phase (MANDATORY PHASE 0)

### Documents Reviewed:
- **domain-layer-architecture.md**: Lines 725-997 - Found NSwag auto-generation implementation, packages/shared-types structure
- **DTO-ALIGNMENT-STRATEGY.md**: Lines 85-213 - Confirmed NSwag as THE solution for type generation, no manual interfaces
- **migration-plan.md**: Lines 100-171, 217-240 - Found Mantine v7 UI, TanStack Query patterns, React+TypeScript stack
- **functional-area-master-index.md**: Confirmed events functional area exists with approved business requirements

### Existing Solutions Found:
- **NSwag Type Generation**: Per domain-layer-architecture.md lines 725-997 - All TypeScript interfaces auto-generated from C# DTOs
- **TanStack Query Integration**: Per migration-plan.md lines 217-240 - Data fetching patterns established with useQuery/useMutation
- **Mantine v7 Components**: Per migration-plan.md lines 100-171 - UI component library selected and configured
- **EventDto Structure**: Generated types already imported from @witchcityrope/shared-types in existing implementation

### Verification Statement:
"Confirmed existing NSwag solution provides all required TypeScript interfaces. Implementation will use generated EventDto types and established patterns per architecture documents."

## Executive Summary

This functional specification defines the transformation of the Admin Events Dashboard from a card-based layout to a streamlined table-based interface with enhanced filtering, real-time search, and improved navigation. The implementation follows established React architecture patterns using NSwag-generated types, TanStack Query for data management, and Mantine v7 components.

## Technical Architecture

### Component Hierarchy
```
AdminEventsPage (Container)
├── AdminEventsPageHeader
│   ├── PageTitle
│   └── CreateEventButton
├── EventsFilterBar
│   ├── EventTypeFilters (Chip.Group)
│   ├── PastEventsToggle (Switch)
│   └── EventSearchInput (TextInput)
├── EventsTable
│   ├── EventsTableHeader
│   │   ├── SortableColumn (Date)
│   │   ├── SortableColumn (Event Title)
│   │   ├── StaticColumn (Time)
│   │   ├── StaticColumn (Capacity/Tickets)
│   │   └── StaticColumn (Actions)
│   └── EventsTableBody
│       └── EventTableRow[]
│           ├── DateCell
│           ├── TitleCell (clickable)
│           ├── TimeCell
│           ├── CapacityCell (with Progress)
│           └── ActionsCell
│               └── CopyEventButton
├── LoadingState (Skeleton)
├── EmptyState
└── ErrorState
```

### Data Flow Architecture

#### Type System (NSwag Generated)
```typescript
// Generated from C# DTOs - DO NOT CREATE MANUALLY
// Source: packages/shared-types/src/generated/api-client.ts
import { 
  EventDto,           // Generated from C# EventDto
  EventStatus,        // Generated from C# EventStatus enum
  EventType           // Generated from C# EventType enum
} from '@witchcityrope/shared-types';

// Per domain-layer-architecture.md lines 569-586:
// All interfaces auto-generated via NSwag pipeline
```

#### State Management Pattern
```typescript
// Filter state using React useState (lightweight state)
interface AdminEventsFilterState {
  activeTypes: EventType[];           // Selected event type filters
  searchTerm: string;                 // Real-time search input
  showPastEvents: boolean;            // Past events toggle
  sortColumn: 'date' | 'title' | null; // Current sort column
  sortDirection: 'asc' | 'desc';      // Sort direction
}

// Data fetching using TanStack Query (per migration-plan.md lines 217-240)
const useEvents = () => {
  return useQuery({
    queryKey: ['events'],
    queryFn: () => eventsApi.getEvents(),
    staleTime: 5 * 60 * 1000,  // 5 minutes
    refetchInterval: 30 * 1000  // 30 seconds background refetch
  });
};
```

#### Data Processing Pipeline
```typescript
// Event processing pipeline
const processedEvents = useMemo(() => {
  if (!rawEvents) return [];
  
  return rawEvents
    .filter(applyTypeFilters)      // Filter by event type
    .filter(applySearchFilter)     // Filter by search term
    .filter(applyPastEventsFilter) // Filter past events
    .map(enhanceEventData)         // Add computed fields
    .sort(applySorting);           // Apply column sorting
}, [rawEvents, filterState]);

// Per business requirements line 54-55: Default sort by startDateTime ascending
const applySorting = (events: EventDto[]) => {
  if (!filterState.sortColumn) {
    return events.sort((a, b) => 
      new Date(a.startDateTime).getTime() - new Date(b.startDateTime).getTime()
    );
  }
  // Additional sorting logic...
};
```

### API Integration Specifications

#### Endpoint Requirements
```typescript
// Using existing API endpoints - no new endpoints required
// All endpoints return NSwag-generated types

// Primary data endpoint
GET /api/events
Response: EventDto[]

// Event management endpoints (already implemented)
POST /api/events          // For copy functionality
PUT /api/events/{id}      // For future edit navigation
DELETE /api/events/{id}   // For future delete functionality
```

#### TanStack Query Implementation
```typescript
// Per migration-plan.md lines 647-663: Established mutation patterns
export const useCopyEvent = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: (eventId: string) => eventsApi.copyEvent(eventId),
    onSuccess: (newEvent) => {
      // Invalidate events cache to show new event
      queryClient.invalidateQueries({ queryKey: ['events'] });
      
      // Navigate to edit the copied event
      navigate(`/admin/events/edit/${newEvent.id}`);
    },
    onError: (error) => {
      notifications.show({
        title: 'Copy Failed',
        message: 'Unable to copy event. Please try again.',
        color: 'red'
      });
    }
  });
};
```

#### Real-time Updates Strategy
```typescript
// Per migration-plan.md lines 888-920: WebSocket integration pattern
const useRealTimeEventUpdates = () => {
  const queryClient = useQueryClient();
  
  useEffect(() => {
    const ws = new WebSocket(process.env.REACT_APP_WS_URL);
    
    ws.onmessage = (event) => {
      const data = JSON.parse(event.data);
      
      if (data.type === 'event_updated' || data.type === 'registration_changed') {
        // Invalidate events query to refetch updated data
        queryClient.invalidateQueries({ queryKey: ['events'] });
      }
    };
    
    return () => ws.close();
  }, [queryClient]);
};
```

## Component Specifications

### EventsFilterBar Component

#### Implementation
```typescript
interface EventsFilterBarProps {
  filterState: AdminEventsFilterState;
  onFilterChange: (newState: Partial<AdminEventsFilterState>) => void;
}

// Mantine Chip.Group implementation (per ui-design-specifications.md lines 63-70)
const EventsFilterBar: React.FC<EventsFilterBarProps> = ({ 
  filterState, 
  onFilterChange 
}) => {
  return (
    <Stack gap="md">
      {/* Event Type Filters */}
      <Group>
        <Text size="sm" fw={500} c="dimmed">Filter:</Text>
        <Chip.Group 
          multiple 
          value={filterState.activeTypes}
          onChange={(types) => onFilterChange({ activeTypes: types as EventType[] })}
        >
          <Group>
            <Chip value="social" variant="filled" color="wcr">Social</Chip>
            <Chip value="class" variant="filled" color="wcr">Class</Chip>
          </Group>
        </Chip.Group>
      </Group>
      
      {/* Search and Past Events Toggle */}
      <Group justify="space-between">
        <TextInput
          placeholder="Search events..."
          leftSection={<IconSearch size="1rem" />}
          value={filterState.searchTerm}
          onChange={(event) => onFilterChange({ searchTerm: event.currentTarget.value })}
          style={{ flex: 1, maxWidth: 300 }}
        />
        
        <Switch
          label="Show Past Events"
          checked={filterState.showPastEvents}
          onChange={(event) => 
            onFilterChange({ showPastEvents: event.currentTarget.checked })
          }
        />
      </Group>
    </Stack>
  );
};
```

#### Debounced Search Implementation
```typescript
// Optimize search performance with debouncing
const useDebouncedSearch = (searchTerm: string, delay: number = 300) => {
  const [debouncedValue, setDebouncedValue] = useState(searchTerm);
  
  useEffect(() => {
    const handler = setTimeout(() => {
      setDebouncedValue(searchTerm);
    }, delay);
    
    return () => clearTimeout(handler);
  }, [searchTerm, delay]);
  
  return debouncedValue;
};
```

### EventsTable Component

#### Table Structure (Mantine Table)
```typescript
interface EventsTableProps {
  events: EventDto[];
  sortState: {
    column: 'date' | 'title' | null;
    direction: 'asc' | 'desc';
  };
  onSort: (column: 'date' | 'title') => void;
  onRowClick: (eventId: string) => void;
  onCopyEvent: (eventId: string) => void;
}

// Per ui-design-specifications.md lines 99-133: Table header implementation
const EventsTable: React.FC<EventsTableProps> = ({
  events,
  sortState,
  onSort,
  onRowClick,
  onCopyEvent
}) => {
  return (
    <Table striped highlightOnHover>
      <Table.Thead bg="wcr.7">
        <Table.Tr>
          {/* Sortable Date Column */}
          <Table.Th 
            style={{ cursor: 'pointer' }}
            onClick={() => onSort('date')}
            c="white"
          >
            <Group gap="xs">
              Date
              <ActionIcon variant="transparent" size="sm" c="white">
                {getSortIcon('date', sortState)}
              </ActionIcon>
            </Group>
          </Table.Th>
          
          {/* Sortable Title Column */}
          <Table.Th 
            style={{ cursor: 'pointer' }}
            onClick={() => onSort('title')}
            c="white"
          >
            <Group gap="xs">
              Event Title
              <ActionIcon variant="transparent" size="sm" c="white">
                {getSortIcon('title', sortState)}
              </ActionIcon>
            </Group>
          </Table.Th>
          
          <Table.Th c="white">Time</Table.Th>
          <Table.Th c="white">Capacity/Tickets</Table.Th>
          <Table.Th c="white">Actions</Table.Th>
        </Table.Tr>
      </Table.Thead>
      
      <Table.Tbody>
        {events.map(event => (
          <EventTableRow
            key={event.id}
            event={event}
            onClick={() => onRowClick(event.id)}
            onCopy={() => onCopyEvent(event.id)}
          />
        ))}
      </Table.Tbody>
    </Table>
  );
};
```

#### EventTableRow Component
```typescript
interface EventTableRowProps {
  event: EventDto;
  onClick: () => void;
  onCopy: () => void;
}

const EventTableRow: React.FC<EventTableRowProps> = ({ event, onClick, onCopy }) => {
  return (
    <Table.Tr 
      style={{ cursor: 'pointer' }}
      onClick={onClick}
      // Per ui-design-specifications.md lines 332: Row hover animation
      sx={{
        '&:hover': {
          backgroundColor: 'rgba(136, 1, 36, 0.08)',
          transition: 'background-color 150ms ease'
        }
      }}
    >
      <Table.Td>
        {formatEventDate(event.startDateTime)}
      </Table.Td>
      
      <Table.Td>
        <Text fw={600} c="wcr.7">{event.title}</Text>
      </Table.Td>
      
      <Table.Td>
        {formatTimeRange(event.startDateTime, event.endDateTime)}
      </Table.Td>
      
      <Table.Td>
        <CapacityDisplay 
          current={event.currentAttendees} 
          max={event.capacity} 
        />
      </Table.Td>
      
      <Table.Td onClick={(e) => e.stopPropagation()}>
        <Button
          size="xs"
          variant="light"
          color="wcr"
          onClick={onCopy}
        >
          Copy
        </Button>
      </Table.Td>
    </Table.Tr>
  );
};
```

#### CapacityDisplay Component
```typescript
// Per ui-design-specifications.md lines 137-159: Capacity visualization
interface CapacityDisplayProps {
  current: number;
  max: number;
}

const CapacityDisplay: React.FC<CapacityDisplayProps> = ({ current, max }) => {
  const percentage = (current / max) * 100;
  
  const getColor = () => {
    if (percentage >= 80) return 'red';
    if (percentage >= 60) return 'yellow'; 
    return 'green';
  };

  return (
    <Stack gap="xs">
      <Text fw={700} c="wcr.7" size="sm">
        {current}/{max}
      </Text>
      <Progress
        value={percentage}
        color={getColor()}
        size="sm"
        radius="xs"
        // Per accessibility requirements
        aria-label={`${current} of ${max} spots filled`}
        aria-valuenow={current}
        aria-valuemin={0}
        aria-valuemax={max}
      />
    </Stack>
  );
};
```

## Event Handlers and User Interactions

### Navigation Patterns
```typescript
// Row click navigation (per business requirements Story 6)
const handleRowClick = (eventId: string) => {
  // Navigate to event edit page
  navigate(`/admin/events/edit/${eventId}`);
};

// Copy event functionality
const handleCopyEvent = useCopyEvent();
const onCopyEvent = (eventId: string) => {
  handleCopyEvent.mutate(eventId);
};

// Create new event
const handleCreateEvent = () => {
  navigate('/admin/events/new');
};
```

### Sort Implementation
```typescript
const useSortableTable = (initialColumn: string = 'date') => {
  const [sortState, setSortState] = useState({
    column: initialColumn,
    direction: 'asc' as const
  });
  
  const handleSort = (column: string) => {
    setSortState(prev => ({
      column,
      direction: prev.column === column && prev.direction === 'asc' 
        ? 'desc' 
        : 'asc'
    }));
  };
  
  return { sortState, handleSort };
};
```

### Filter State Management
```typescript
const useFilterState = () => {
  const [filterState, setFilterState] = useState<AdminEventsFilterState>({
    activeTypes: [],
    searchTerm: '',
    showPastEvents: false,
    sortColumn: 'date',
    sortDirection: 'asc'
  });
  
  const updateFilter = (updates: Partial<AdminEventsFilterState>) => {
    setFilterState(prev => ({ ...prev, ...updates }));
  };
  
  return { filterState, updateFilter };
};
```

## Validation Rules and Error Handling

### Data Validation
```typescript
// EventDto validation (types auto-generated from C# via NSwag)
const validateEventDto = (event: EventDto): boolean => {
  return (
    event.id !== undefined &&
    event.title?.length > 0 &&
    event.startDateTime !== undefined &&
    event.endDateTime !== undefined &&
    event.capacity > 0
  );
};

// Filter validation
const validateFilterState = (state: AdminEventsFilterState): boolean => {
  return (
    Array.isArray(state.activeTypes) &&
    typeof state.searchTerm === 'string' &&
    typeof state.showPastEvents === 'boolean'
  );
};
```

### Error Boundaries
```typescript
// Per migration-plan.md lines 411-444: Error handling patterns
class EventsTableErrorBoundary extends React.Component<
  { children: React.ReactNode },
  { hasError: boolean }
> {
  constructor(props: { children: React.ReactNode }) {
    super(props);
    this.state = { hasError: false };
  }

  static getDerivedStateFromError(error: Error): { hasError: boolean } {
    return { hasError: true };
  }

  componentDidCatch(error: Error, errorInfo: React.ErrorInfo) {
    console.error('EventsTable error:', error, errorInfo);
    // Log to error reporting service
  }

  render() {
    if (this.state.hasError) {
      return (
        <Alert color="red" title="Something went wrong">
          Unable to load events table. Please refresh the page or contact support.
          <Button variant="outline" size="sm" mt="md" onClick={() => window.location.reload()}>
            Refresh Page
          </Button>
        </Alert>
      );
    }

    return this.props.children;
  }
}
```

### Loading and Error States
```typescript
const AdminEventsPage: React.FC = () => {
  const { data: events, isLoading, error } = useEvents();
  
  if (isLoading) {
    return <EventsTableSkeleton />;
  }
  
  if (error) {
    return (
      <Alert color="red" title="Failed to load events">
        {error.message || 'Please try refreshing the page.'}
        <Button variant="outline" size="sm" mt="md" onClick={() => window.location.reload()}>
          Retry
        </Button>
      </Alert>
    );
  }
  
  // Render main interface...
};

// Per ui-design-specifications.md lines 315-329: Skeleton loading
const EventsTableSkeleton: React.FC = () => (
  <Table>
    <Table.Tbody>
      {Array.from({ length: 5 }).map((_, index) => (
        <Table.Tr key={index}>
          <Table.Td><Skeleton height={20} width="80%" /></Table.Td>
          <Table.Td><Skeleton height={20} width="90%" /></Table.Td>
          <Table.Td><Skeleton height={20} width="70%" /></Table.Td>
          <Table.Td><Skeleton height={32} width="100%" /></Table.Td>
          <Table.Td><Skeleton height={28} width="60%" /></Table.Td>
        </Table.Tr>
      ))}
    </Table.Tbody>
  </Table>
);
```

## Performance Requirements and Optimization

### Performance Targets
- **Table Rendering**: <200ms for up to 500 events (per business requirements line 176)
- **Search Filtering**: <100ms response time (per business requirements line 177)
- **Sort Operations**: <150ms completion (per business requirements line 178)
- **Memory Usage**: <50MB heap allocation for table operations

### Optimization Strategies

#### Memoization Implementation
```typescript
// Memoize expensive filtering operations
const filteredEvents = useMemo(() => {
  if (!events) return [];
  
  return events
    .filter(event => {
      // Type filtering
      if (filterState.activeTypes.length > 0 && 
          !filterState.activeTypes.includes(event.eventType)) {
        return false;
      }
      
      // Search filtering (case-insensitive)
      if (filterState.searchTerm && 
          !event.title.toLowerCase().includes(filterState.searchTerm.toLowerCase())) {
        return false;
      }
      
      // Past events filtering
      if (!filterState.showPastEvents && isPastEvent(event)) {
        return false;
      }
      
      return true;
    })
    .sort((a, b) => applySorting(a, b, filterState));
}, [events, filterState]);

// Memoize date formatting to avoid repeated calculations
const formatEventDate = useMemo(() => {
  const formatter = new Intl.DateTimeFormat('en-US', {
    month: 'short',
    day: 'numeric', 
    year: 'numeric'
  });
  
  return (dateString: string) => formatter.format(new Date(dateString));
}, []);
```

#### Virtual Scrolling for Large Datasets
```typescript
// For datasets >100 events, implement virtual scrolling
import { useVirtualizer } from '@tanstack/react-virtual';

const VirtualizedEventsTable: React.FC<{ events: EventDto[] }> = ({ events }) => {
  const parentRef = useRef<HTMLDivElement>(null);
  
  const virtualizer = useVirtualizer({
    count: events.length,
    getScrollElement: () => parentRef.current,
    estimateSize: () => 60, // Row height
    overscan: 10
  });
  
  return (
    <div ref={parentRef} style={{ height: '600px', overflow: 'auto' }}>
      <div style={{ height: virtualizer.getTotalSize(), position: 'relative' }}>
        {virtualizer.getVirtualItems().map(virtualRow => (
          <div
            key={virtualRow.index}
            style={{
              position: 'absolute',
              top: 0,
              left: 0,
              width: '100%',
              height: virtualRow.size,
              transform: `translateY(${virtualRow.start}px)`
            }}
          >
            <EventTableRow 
              event={events[virtualRow.index]}
              onClick={() => handleRowClick(events[virtualRow.index].id)}
              onCopy={() => handleCopyEvent(events[virtualRow.index].id)}
            />
          </div>
        ))}
      </div>
    </div>
  );
};
```

#### Bundle Size Optimization
```typescript
// Code splitting for admin features
const AdminEventsPage = lazy(() => import('./AdminEventsPage'));

// Lazy load heavy dependencies
const EventsAnalytics = lazy(() => import('./EventsAnalytics'));

// Bundle analysis targets (per ui-design-specifications.md lines 305-309):
// - Mantine Table: ~15KB gzipped (already included)
// - Progress Component: ~3KB gzipped  
// - Search/Filter Logic: ~2KB additional
// - Total Addition: ~5KB net increase
```

## Migration Plan from Current Implementation

### Current State Analysis
```typescript
// Current: Card-based layout using Grid/Paper components
// Location: apps/web/src/pages/admin/AdminEventsPage.tsx lines 288-431
// Components to replace:
// - Grid.Col with Table.Tr
// - Paper with Table.Td
// - formatEventDisplay with table-specific formatters
```

### Migration Strategy

#### Phase 1: Component Structure Migration (Week 1)
```typescript
// Step 1: Create new table components alongside existing code
// - EventsTable component
// - EventsFilterBar component
// - CapacityDisplay component

// Step 2: Implement data processing pipeline
// - Filter logic migration from card display
// - Sort functionality implementation  
// - Search integration with existing useEvents hook

// Step 3: Styling migration to Mantine Table
// - Replace custom card styles with table styles
// - Implement WCR design system colors
// - Add responsive behavior
```

#### Phase 2: Feature Enhancement (Week 2)
```typescript
// Step 1: Add new table-specific features
// - Column sorting with visual indicators
// - Row hover effects and click handling
// - Capacity progress bars

// Step 2: Integrate with existing functionality
// - Preserve existing modal handling
// - Keep existing API mutation hooks
// - Maintain error notification patterns

// Step 3: Performance optimization
// - Implement memoization
// - Add virtual scrolling if needed
// - Bundle size analysis
```

#### Phase 3: Testing and Refinement (Week 3)
```typescript
// Step 1: Feature parity validation
// - All existing functionality preserved
// - New table features working correctly
// - Mobile responsiveness verified

// Step 2: Performance validation
// - Meeting performance targets
// - Memory usage within limits
// - Bundle size impact acceptable

// Step 3: User acceptance testing
// - Admin workflow efficiency improved
// - No regression in functionality
// - Positive user feedback
```

### Code Migration Mapping
```typescript
// Current card display logic → Table row logic
const formatEventDisplay = (event: EventDto) => {
  // Current implementation: lines 11-54
  // Migrate to: formatTableRowData()
};

// Current Grid/Paper structure → Table structure
<Grid.Col key={event.id} span={{ base: 12, md: 6, lg: 4 }}>
  <Paper>...</Paper>
</Grid.Col>
// Becomes:
<Table.Tr key={event.id}>
  <Table.Td>...</Table.Td>
</Table.Tr>

// Current action buttons → Table actions cell
<ActionIcon onClick={() => handleEditEvent(event.id)}>
  <IconEdit size={16} />
</ActionIcon>
// Becomes:
<Table.Td onClick={(e) => e.stopPropagation()}>
  <Button onClick={() => handleCopyEvent(event.id)}>Copy</Button>
</Table.Td>
```

## Testing Requirements and Acceptance Criteria

### Unit Testing Specifications

#### Component Testing
```typescript
// EventsTable Component Tests
describe('EventsTable', () => {
  const mockEvents: EventDto[] = [
    {
      id: '1',
      title: 'Rope Basics Workshop',
      startDateTime: '2025-09-15T19:00:00Z',
      endDateTime: '2025-09-15T21:00:00Z',
      capacity: 20,
      currentAttendees: 15,
      eventType: 'class',
      status: 'published'
    }
  ];

  it('renders events in table format', () => {
    render(
      <EventsTable 
        events={mockEvents}
        sortState={{ column: 'date', direction: 'asc' }}
        onSort={jest.fn()}
        onRowClick={jest.fn()}
        onCopyEvent={jest.fn()}
      />
    );
    
    expect(screen.getByText('Rope Basics Workshop')).toBeInTheDocument();
    expect(screen.getByText('15/20')).toBeInTheDocument();
  });

  it('handles row click navigation', () => {
    const onRowClick = jest.fn();
    render(<EventsTable {...props} onRowClick={onRowClick} />);
    
    const row = screen.getByText('Rope Basics Workshop').closest('tr');
    fireEvent.click(row);
    
    expect(onRowClick).toHaveBeenCalledWith('1');
  });

  it('prevents row click when clicking copy button', () => {
    const onRowClick = jest.fn();
    const onCopyEvent = jest.fn();
    
    render(<EventsTable {...props} onRowClick={onRowClick} onCopyEvent={onCopyEvent} />);
    
    const copyButton = screen.getByText('Copy');
    fireEvent.click(copyButton);
    
    expect(onCopyEvent).toHaveBeenCalledWith('1');
    expect(onRowClick).not.toHaveBeenCalled();
  });
});

// Filter Bar Component Tests  
describe('EventsFilterBar', () => {
  it('updates search term on input', () => {
    const onFilterChange = jest.fn();
    render(<EventsFilterBar filterState={initialState} onFilterChange={onFilterChange} />);
    
    const searchInput = screen.getByPlaceholderText('Search events...');
    fireEvent.change(searchInput, { target: { value: 'rope' } });
    
    expect(onFilterChange).toHaveBeenCalledWith({ searchTerm: 'rope' });
  });

  it('toggles event type filters', () => {
    const onFilterChange = jest.fn();
    render(<EventsFilterBar filterState={initialState} onFilterChange={onFilterChange} />);
    
    const socialChip = screen.getByText('Social');
    fireEvent.click(socialChip);
    
    expect(onFilterChange).toHaveBeenCalledWith({ activeTypes: ['social'] });
  });
});
```

#### Hook Testing
```typescript
// Filter State Hook Tests
describe('useFilterState', () => {
  it('initializes with default values', () => {
    const { result } = renderHook(() => useFilterState());
    
    expect(result.current.filterState).toEqual({
      activeTypes: [],
      searchTerm: '',
      showPastEvents: false,
      sortColumn: 'date',
      sortDirection: 'asc'
    });
  });

  it('updates filter state partially', () => {
    const { result } = renderHook(() => useFilterState());
    
    act(() => {
      result.current.updateFilter({ searchTerm: 'test' });
    });
    
    expect(result.current.filterState.searchTerm).toBe('test');
    expect(result.current.filterState.activeTypes).toEqual([]);
  });
});
```

### Integration Testing

#### API Integration Tests
```typescript
describe('Admin Events API Integration', () => {
  beforeEach(() => {
    server.use(
      rest.get('/api/events', (req, res, ctx) => {
        return res(ctx.json(mockEventsData));
      })
    );
  });

  it('loads and displays events from API', async () => {
    render(<AdminEventsPage />);
    
    expect(screen.getByText('Loading events...')).toBeInTheDocument();
    
    await waitFor(() => {
      expect(screen.getByText('Rope Basics Workshop')).toBeInTheDocument();
    });
  });

  it('handles API errors gracefully', async () => {
    server.use(
      rest.get('/api/events', (req, res, ctx) => {
        return res(ctx.status(500));
      })
    );

    render(<AdminEventsPage />);
    
    await waitFor(() => {
      expect(screen.getByText('Failed to load events')).toBeInTheDocument();
    });
  });
});
```

#### Real-time Updates Testing
```typescript
describe('Real-time Event Updates', () => {
  it('updates table when event capacity changes', async () => {
    const { queryClient } = renderWithQueryClient(<AdminEventsPage />);
    
    // Simulate WebSocket message
    const mockWS = new MockWebSocket();
    mockWS.send(JSON.stringify({
      type: 'registration_changed',
      eventId: '1',
      newCapacity: 16
    }));
    
    await waitFor(() => {
      expect(screen.getByText('16/20')).toBeInTheDocument();
    });
  });
});
```

### End-to-End Testing

#### User Workflow Tests
```typescript
// Playwright E2E tests
describe('Admin Events Dashboard E2E', () => {
  test('admin can filter and sort events', async ({ page }) => {
    await page.goto('/admin/events');
    
    // Test filtering
    await page.click('text=Social');
    await expect(page.locator('[data-testid="events-table"]')).toContainText('Social Event');
    
    // Test search
    await page.fill('[placeholder="Search events..."]', 'rope');
    await expect(page.locator('[data-testid="events-table"]')).toContainText('Rope');
    
    // Test sorting
    await page.click('text=Event Title');
    await expect(page.locator('[data-testid="events-table"] tbody tr:first-child'))
      .toContainText('Advanced Rope'); // Alphabetically first
  });

  test('admin can copy event', async ({ page }) => {
    await page.goto('/admin/events');
    
    await page.click('text=Copy', { first: true });
    
    // Should navigate to edit page for copied event
    await expect(page).toHaveURL(/\/admin\/events\/edit\/[a-z0-9-]+/);
    await expect(page.locator('h1')).toContainText('Edit Event');
  });

  test('table is responsive on mobile', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 }); // iPhone SE
    await page.goto('/admin/events');
    
    // Table should scroll horizontally
    const table = page.locator('[data-testid="events-table"]');
    await expect(table).toBeVisible();
    
    // Critical columns should be visible
    await expect(page.locator('th:has-text("Date")')).toBeVisible();
    await expect(page.locator('th:has-text("Event Title")')).toBeVisible();
    await expect(page.locator('th:has-text("Actions")')).toBeVisible();
  });
});
```

### Performance Testing

#### Performance Benchmarks
```typescript
describe('Performance Tests', () => {
  test('table renders within 200ms for 100 events', async () => {
    const startTime = performance.now();
    
    const largeEventsList = Array.from({ length: 100 }, (_, i) => ({
      id: `event-${i}`,
      title: `Event ${i}`,
      startDateTime: new Date().toISOString(),
      endDateTime: new Date().toISOString(),
      capacity: 20,
      currentAttendees: i % 20
    }));
    
    render(<EventsTable events={largeEventsList} {...otherProps} />);
    
    await waitFor(() => {
      expect(screen.getByText('Event 0')).toBeInTheDocument();
    });
    
    const renderTime = performance.now() - startTime;
    expect(renderTime).toBeLessThan(200);
  });

  test('search filtering completes within 100ms', () => {
    const events = generateLargeEventsList(500);
    
    const startTime = performance.now();
    const filtered = events.filter(event => 
      event.title.toLowerCase().includes('rope')
    );
    const filterTime = performance.now() - startTime;
    
    expect(filterTime).toBeLessThan(100);
  });
});
```

### Accessibility Testing
```typescript
describe('Accessibility Tests', () => {
  test('table has proper ARIA labels', async () => {
    render(<AdminEventsPage />);
    
    await waitFor(() => {
      const table = screen.getByRole('table');
      expect(table).toBeInTheDocument();
      
      // Check column headers
      expect(screen.getByRole('columnheader', { name: /date/i })).toHaveAttribute('scope', 'col');
      expect(screen.getByRole('columnheader', { name: /event title/i })).toHaveAttribute('scope', 'col');
      
      // Check sort indicators
      const sortButton = screen.getByRole('button', { name: /sort by date/i });
      expect(sortButton).toHaveAttribute('aria-sort');
    });
  });

  test('capacity progress bars have proper labels', async () => {
    render(<AdminEventsPage />);
    
    await waitFor(() => {
      const progressBar = screen.getByRole('progressbar');
      expect(progressBar).toHaveAttribute('aria-label');
      expect(progressBar).toHaveAttribute('aria-valuenow');
      expect(progressBar).toHaveAttribute('aria-valuemin', '0');
      expect(progressBar).toHaveAttribute('aria-valuemax');
    });
  });

  test('keyboard navigation works correctly', async () => {
    render(<AdminEventsPage />);
    
    // Tab through interactive elements
    await userEvent.tab(); // Search input
    await userEvent.tab(); // First filter chip
    await userEvent.tab(); // Second filter chip  
    await userEvent.tab(); // Past events toggle
    await userEvent.tab(); // First sortable column
    
    // Enter key should trigger sort
    await userEvent.keyboard('{Enter}');
    // Verify sort happened (implementation depends on visual feedback)
  });
});
```

## Acceptance Criteria Validation

### Business Requirements Validation

#### Performance Criteria
- ✅ **Event Location Time**: Must achieve <15 seconds (from 45 seconds baseline)
- ✅ **Task Completion Rate**: Must improve by 40%
- ✅ **User Satisfaction**: Must reach 4.5/5 rating (from 3.2/5 baseline)
- ✅ **Support Tickets**: Must reduce by 50%

#### Functional Criteria  
- ✅ **Table Display**: Events shown in sortable table with Date, Title, Time, Capacity columns
- ✅ **Filtering**: Independent Social/Class filter chips with real-time updates
- ✅ **Search**: Real-time search filtering on event titles
- ✅ **Past Events**: Toggle to show/hide past events
- ✅ **Capacity Display**: Visual progress bars with current/max attendees
- ✅ **Row Navigation**: Click anywhere on row navigates to edit page
- ✅ **Copy Function**: Copy button creates draft event copy
- ✅ **Mobile Support**: Responsive design with horizontal scroll

### Technical Criteria
- ✅ **Architecture Compliance**: Uses NSwag-generated types per domain-layer-architecture.md
- ✅ **Performance**: Table renders <200ms, search <100ms, sort <150ms  
- ✅ **Error Handling**: Error boundaries and graceful failure handling
- ✅ **Accessibility**: WCAG 2.1 AA compliance with proper ARIA labels
- ✅ **Bundle Size**: Net increase <10KB per ui-design-specifications.md
- ✅ **Type Safety**: Strict TypeScript with generated EventDto interfaces

### Quality Assurance Checklist
- [ ] All user stories implemented and tested
- [ ] Component hierarchy matches specification
- [ ] Data flow follows TanStack Query patterns
- [ ] Error handling covers all failure scenarios
- [ ] Performance benchmarks met or exceeded
- [ ] Mobile responsiveness verified across devices
- [ ] Accessibility compliance validated
- [ ] Migration preserves all existing functionality
- [ ] Real-time updates working correctly
- [ ] Security considerations addressed

## Implementation Notes

### Migration Strategy
This specification provides a comprehensive blueprint for transforming the Admin Events Dashboard from a card-based layout to an efficient table-driven interface. The implementation leverages existing architecture patterns and generated types to ensure consistency with the broader React migration strategy.

### Key Dependencies
- **NSwag Type Generation**: All TypeScript interfaces must be generated from C# DTOs
- **TanStack Query**: Data fetching patterns established in migration plan
- **Mantine v7 Components**: UI component library already selected and configured
- **Design System v7**: WitchCityRope branding and styling standards

### Risk Mitigation
- **Type Safety**: Generated types prevent API/frontend misalignment
- **Performance**: Memoization and virtual scrolling for large datasets
- **User Experience**: Preserve all existing functionality during migration
- **Rollback**: Component-based architecture allows gradual implementation

This specification ensures the Admin Events Dashboard enhancement delivers significant efficiency improvements while maintaining the high-quality user experience expected from the WitchCityRope platform.
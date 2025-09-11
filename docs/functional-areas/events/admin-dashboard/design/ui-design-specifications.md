# UI Design Specifications: Admin Events Dashboard v2
<!-- Last Updated: 2025-09-11 -->
<!-- Version: 2.0 -->
<!-- Owner: UI Designer Agent -->
<!-- Status: Complete -->

## Design Overview

This document specifies the redesigned Admin Events Dashboard that transforms the current card-based layout into a streamlined table view with enhanced filtering, search, and navigation capabilities. The design maintains the WitchCityRope Design System v7 aesthetic while prioritizing admin workflow efficiency.

## Design Principles Applied

### User Experience Focus
- **Efficiency First**: Reduce time to locate and manage events from 45 seconds to 15 seconds
- **Minimal Clicks**: Single-click navigation to event editing, Copy functionality for rapid event creation
- **Visual Clarity**: Table format enables quick comparison of multiple events
- **Real-time Feedback**: Instant filtering and search results

### Design System v7 Compliance
- **Color Palette**: Burgundy primary (#880124), Rose Gold accents (#B76D75), Amber CTAs (#FFBF00)
- **Typography**: Bodoni Moda for display, Montserrat for headings, Source Sans 3 for body
- **Signature Elements**: Asymmetric button corner morphing, center-outward navigation underlines
- **Layout Patterns**: Clean spacing with CSS variables, consistent visual hierarchy

## Component Specifications

### Page Header Enhancement
**Previous Design**: Title and Create button separated  
**New Design**: Title and Create button aligned horizontally

```jsx
// Mantine implementation
<Group justify="space-between" align="center" mb="md">
  <Title order={1} c="wcr.7" ff="var(--font-display)" size="2.5rem">
    Events Dashboard
  </Title>
  <Button
    variant="gradient"
    gradient={{ from: "wcr.6", to: "yellow.6" }}
    size="md"
    styles={{
      root: {
        borderRadius: '12px 6px 12px 6px',
        '&:hover': {
          borderRadius: '6px 12px 6px 12px'
        }
      }
    }}
    onClick={handleCreateEvent}
  >
    + Create New Event
  </Button>
</Group>
```

### Filter Controls Transformation
**Previous Design**: Dropdown for event types  
**New Design**: Clickable filter words with independent selection

#### Event Type Filters
```jsx
// Mantine Chip.Group implementation
<Chip.Group multiple value={activeFilters} onChange={setActiveFilters}>
  <Group>
    <Text size="sm" fw={500} c="dimmed">Filter:</Text>
    <Chip value="social" variant="filled" color="wcr">Social</Chip>
    <Chip value="class" variant="filled" color="wcr">Class</Chip>
  </Group>
</Chip.Group>
```

#### Search Integration
```jsx
// Mantine TextInput with real-time filtering
<TextInput
  placeholder="Search events..."
  leftSection={<IconSearch size="1rem" />}
  value={searchTerm}
  onChange={(event) => setSearchTerm(event.currentTarget.value)}
  styles={{
    input: {
      backgroundColor: 'var(--color-cream)',
      borderColor: 'var(--color-rose-gold)',
      '&:focus': {
        borderColor: 'var(--color-burgundy)',
        boxShadow: '0 0 0 3px rgba(136, 1, 36, 0.15)'
      }
    }
  }}
/>
```

### Events Table Structure
**Previous Design**: Card grid layout  
**New Design**: Sortable table with capacity visualization

#### Table Header
```jsx
<Table striped highlightOnHover>
  <Table.Thead bg="wcr.7">
    <Table.Tr>
      <Table.Th 
        style={{ cursor: 'pointer' }} 
        onClick={() => handleSort('date')}
        c="white"
      >
        <Group gap="xs">
          Date
          <ActionIcon variant="transparent" size="sm" c="white">
            {getSortIcon('date')}
          </ActionIcon>
        </Group>
      </Table.Th>
      <Table.Th 
        style={{ cursor: 'pointer' }} 
        onClick={() => handleSort('title')}
        c="white"
      >
        <Group gap="xs">
          Event Title
          <ActionIcon variant="transparent" size="sm" c="white">
            {getSortIcon('title')}
          </ActionIcon>
        </Group>
      </Table.Th>
      <Table.Th c="white">Time</Table.Th>
      <Table.Th c="white">Capacity/Tickets</Table.Th>
      <Table.Th c="white">Actions</Table.Th>
    </Table.Tr>
  </Table.Thead>
  {/* Table body... */}
</Table>
```

#### Capacity Visualization Component
```jsx
// Custom capacity display with progress bar
const CapacityDisplay = ({ current, max }) => {
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
      />
    </Stack>
  );
};
```

### Row Interaction Patterns
**Previous Design**: Individual card action buttons  
**New Design**: Row click navigation + dedicated Copy button

```jsx
// Table row with click handling
<Table.Tr 
  style={{ cursor: 'pointer' }}
  onClick={() => navigate(`/admin/events/edit/${event.id}`)}
>
  <Table.Td>{formatDate(event.startDateTime)}</Table.Td>
  <Table.Td>
    <Text fw={600} c="wcr.7">{event.title}</Text>
  </Table.Td>
  <Table.Td>{formatTimeRange(event.startDateTime, event.endDateTime)}</Table.Td>
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
      onClick={() => handleCopyEvent(event.id)}
    >
      Copy
    </Button>
  </Table.Td>
</Table.Tr>
```

## Responsive Design Specifications

### Desktop Layout (>768px)
- **Table**: Full column visibility with optimal column widths
- **Filters**: Horizontal layout with search bar expansion
- **Header**: Side-by-side title and create button
- **Sidebar**: Fixed 250px width with hover animations

### Mobile Layout (≤768px)
- **Navigation**: Collapsible drawer with hamburger menu
- **Table**: Horizontal scroll with sticky header
- **Filters**: Stacked layout with full-width elements
- **Create Button**: Full-width for touch accessibility
- **Critical Columns**: Date, Title, Actions remain visible in viewport

### Touch Optimizations
- **Minimum Touch Targets**: 44px for all interactive elements
- **Filter Buttons**: Enlarged padding on mobile (12px vs 8px)
- **Table Cells**: Increased vertical padding for easier row selection
- **Action Buttons**: Enhanced size and spacing for thumb accessibility

## State Management Patterns

### Filter State
```typescript
interface FilterState {
  activeTypes: ('social' | 'class')[];
  searchTerm: string;
  showPastEvents: boolean;
  sortColumn: 'date' | 'title' | null;
  sortDirection: 'asc' | 'desc';
}
```

### Event Display Logic
```typescript
const filteredEvents = useMemo(() => {
  return events
    .filter(event => {
      // Type filtering
      if (filterState.activeTypes.length > 0 && 
          !filterState.activeTypes.includes(event.eventType)) {
        return false;
      }
      
      // Search filtering
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
    .sort((a, b) => {
      if (!filterState.sortColumn) return 0;
      // Sorting logic
    });
}, [events, filterState]);
```

## Accessibility Features

### Keyboard Navigation
- **Table Sorting**: Space/Enter keys activate sort controls
- **Row Selection**: Tab navigation through table rows
- **Filter Controls**: Arrow key navigation for chip groups
- **Search**: Standard input navigation with clear focus indicators

### Screen Reader Support
```jsx
// Table headers with proper labeling
<Table.Th 
  scope="col"
  aria-sort={getSortAttribute('date')}
  role="columnheader"
>
  <VisuallyHidden>Sort by</VisuallyHidden>
  Date
</Table.Th>

// Capacity progress bars with labels
<Progress
  value={percentage}
  aria-label={`${current} of ${max} spots filled`}
  aria-valuenow={current}
  aria-valuemin={0}
  aria-valuemax={max}
/>
```

### Color Contrast Compliance
- **Text on Burgundy**: White text maintains 7:1 contrast ratio
- **Progress Bars**: High contrast color scheme (green/yellow/red with sufficient luminance)
- **Interactive Elements**: Focus indicators with 3:1 contrast minimum
- **Alternative Indicators**: Progress bars supplement color with visual fill level

## Performance Considerations

### Table Optimization
- **Virtual Scrolling**: For datasets >100 events using `@tanstack/react-virtual`
- **Memoization**: Event filtering and sorting operations memoized
- **Debounced Search**: 300ms debounce on search input to reduce filtering calls
- **Pagination**: Server-side pagination for large datasets

### Bundle Size Impact
- **Mantine Table**: ~15KB gzipped (already included in project)
- **Progress Component**: ~3KB gzipped
- **Search/Filter Logic**: ~2KB additional JavaScript
- **Total Addition**: ~5KB net increase (leverages existing Mantine components)

## Animation Specifications

### Loading States
```jsx
// Table skeleton during data fetch
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
```

### Interactive Feedback
- **Row Hover**: 150ms transition to burgundy background (rgba(136, 1, 36, 0.08))
- **Button Morphing**: Corner radius animation matching Design System v7 patterns
- **Progress Bars**: 300ms width transitions for capacity changes
- **Sort Indicators**: 200ms rotation animations for arrow direction changes

## Implementation Priority

### Phase 1: Core Table Structure
1. **Table Layout**: Replace Paper/Grid with Mantine Table component
2. **Basic Sorting**: Date and Title column sorting functionality
3. **Row Click Navigation**: Implement navigation to event edit pages
4. **Capacity Display**: Basic fraction display without progress bars

### Phase 2: Enhanced Filtering
1. **Clickable Filters**: Replace dropdown with Chip.Group implementation
2. **Real-time Search**: TextInput with debounced filtering
3. **Progress Bars**: Visual capacity indicators with color coding
4. **Past Events Toggle**: Checkbox implementation with state management

### Phase 3: Polish & Optimization
1. **Mobile Responsiveness**: Horizontal scroll and drawer navigation
2. **Loading States**: Skeleton components and transition animations
3. **Accessibility**: ARIA labels, keyboard navigation, screen reader support
4. **Performance**: Virtual scrolling and pagination for large datasets

## Success Metrics

### User Experience Improvements
- **Event Location Time**: Target <15 seconds (from 45 seconds baseline)
- **Admin Task Completion**: 40% improvement in completion rate
- **User Satisfaction**: Target 4.5/5 rating (from 3.2/5 baseline)
- **Support Tickets**: 50% reduction in event management related issues

### Technical Performance
- **Initial Load Time**: <200ms for table rendering
- **Search Response**: <100ms filter application
- **Sort Operations**: <150ms completion time
- **Mobile Performance**: Smooth scrolling at 60fps

## Quality Assurance Checklist

### Visual Design Compliance
- [ ] Design System v7 colors applied consistently
- [ ] Typography hierarchy matches specifications
- [ ] Signature animations implemented (button morphing, nav underlines)
- [ ] Progress bars use approved color scheme
- [ ] Mobile responsive breakpoints function correctly

### Functional Requirements
- [ ] Table sorting works for Date and Title columns
- [ ] Clickable filter words toggle independently
- [ ] Search filters events in real-time
- [ ] Row click navigates to event edit page
- [ ] Copy button functions without triggering row click
- [ ] Past events toggle affects display correctly

### Accessibility Standards
- [ ] Keyboard navigation works throughout interface
- [ ] Screen reader announcements for interactive elements
- [ ] Color contrast meets WCAG 2.1 AA standards
- [ ] Focus indicators visible and consistent
- [ ] Table properly structured with headers and scope attributes

### Performance Benchmarks
- [ ] Table renders within 200ms performance budget
- [ ] Search filtering completes within 100ms
- [ ] No layout shift during loading states
- [ ] Mobile scroll performance maintains 60fps
- [ ] Bundle size impact under 10KB net increase

This specification provides comprehensive guidance for implementing the Admin Events Dashboard enhancement while maintaining consistency with the established WitchCityRope design system and ensuring optimal user experience across all device types.
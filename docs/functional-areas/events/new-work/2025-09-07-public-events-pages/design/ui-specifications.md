# UI Design Specifications: Phase 4 - Public Events Pages
<!-- Last Updated: 2025-09-07 -->
<!-- Version: 1.0 -->
<!-- Owner: UI Designer Agent -->
<!-- Status: Ready for Development -->

## Executive Summary
This document provides comprehensive UI design specifications for Phase 4 Public Events Pages, transforming existing wireframes into detailed Mantine v7 component specifications. The design maintains consistency with the established admin UI while creating an engaging public-facing experience for event discovery and registration.

## Design System Foundation

### Theme Configuration (WitchCityRope Branding)
```typescript
import { MantineProvider, createTheme } from '@mantine/core';

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
      '#880124', // burgundy (primary)
      '#6b0119', // darker
      '#2c2c2c'  // charcoal (darkest)
    ]
  },
  primaryColor: 'wcr',
  fontFamily: 'Source Sans 3, sans-serif',
  headings: {
    fontFamily: 'Bodoni Moda, serif'
  },
  other: {
    brandBurgundy: '#8B4513', // Existing brand burgundy from wireframes
    capacityWarning: '#d32f2f',
    successGreen: '#2e7d32'
  }
});
```

### Typography System
- **Primary Font**: Source Sans 3 (system font fallback)
- **Heading Font**: Bodoni Moda (serif for elegance) 
- **Font Sizes**: Mantine scale (xs=12px, sm=14px, md=16px, lg=18px, xl=20px)
- **Font Weights**: 400 (normal), 500 (medium), 600 (semi-bold), 700 (bold)

### Color Palette
- **Primary Brand**: `#8B4513` (burgundy) - matches wireframes
- **Success**: `#2e7d32` (green for pricing/confirmation)
- **Warning**: `#d32f2f` (red for capacity warnings)
- **Background**: `#f5f5f5` (light gray)
- **Surface**: `#ffffff` (white cards/surfaces)
- **Text Primary**: `#1a1a1a` (near black)
- **Text Secondary**: `#666666` (medium gray)

### Spacing System
- **Base Unit**: 8px
- **Component Padding**: 16px, 20px, 24px
- **Section Gaps**: 30px, 40px
- **Card Gaps**: 12px, 16px, 20px

## Page Layout Specifications

### Events List Page (`/events`)

#### Overall Structure
```
┌─────────────────────────────────────┐
│  Utility Bar (dark)                 │
├─────────────────────────────────────┤
│  Header (logo + navigation)         │
├─────────────────────────────────────┤
│  Page Hero (gradient burgundy)      │
├─────────────────────────────────────┤
│  Container (max-width: 1200px)      │
│  ┌─────────────────────────────────┐ │
│  │ Filters Section (white card)    │ │
│  └─────────────────────────────────┘ │
│  ┌─────────────────────────────────┐ │
│  │ Events Grid (by date)           │ │
│  │ ┌─────────────────────────────┐ │ │
│  │ │ Date Header                 │ │ │
│  │ │ Event Card 1                │ │ │
│  │ │ Event Card 2                │ │ │
│  │ └─────────────────────────────┘ │ │
│  └─────────────────────────────────┘ │
└─────────────────────────────────────┘
```

#### Mantine Components Mapping
- **Container**: `Container` with `size="xl"` (1200px)
- **Page Hero**: `Box` with gradient background
- **Filters**: `Paper` with `withBorder` and `shadow="sm"`
- **Event Cards**: `Paper` with `withBorder`, `shadow="sm"`, hover effects
- **Filter Buttons**: `Chip.Group` with `Chip` components
- **Date Headers**: `Title` with `order={2}` and bottom border

### Event Detail Page (`/events/:id`)

#### Overall Structure (Desktop)
```
┌─────────────────────────────────────┐
│  Utility Bar + Header + Breadcrumb  │
├─────────────────┬───────────────────┤
│  Event Content  │  Registration     │
│  (flexible)     │  Sidebar (400px)  │
│                 │                   │
│  Title & Meta   │  Price Card       │
│  Description    │  Capacity Info    │
│  What You'll    │  Registration     │
│  Learn          │  Form            │
│  What to Bring  │                   │
│  Instructor     │  Venue Hidden     │
│  Prerequisites  │  Policies         │
│  Policies       │                   │
└─────────────────┴───────────────────┘
```

#### Mobile Layout (Responsive)
```
┌─────────────────────────────────────┐
│  Header + Breadcrumb                 │
├─────────────────────────────────────┤
│  Registration Card (sticky top)     │
├─────────────────────────────────────┤
│  Event Content (full width)         │
│  Title & Meta                        │
│  Description                         │
│  What You'll Learn                   │
│  Instructor                          │
│  Prerequisites                       │
└─────────────────────────────────────┘
```

#### Mantine Components Mapping
- **Layout**: `Grid` with `Grid.Col` for desktop two-column
- **Content**: `Paper` with rich content sections
- **Sidebar**: `Stack` with sticky positioning
- **Forms**: `@mantine/form` with comprehensive validation
- **Breadcrumbs**: `Breadcrumbs` component

## Component Design Specifications

### 1. EventCard Component

#### Purpose
Display event summary information in list view with capacity indicators and action buttons.

#### Visual Design
- **Container**: White background, border radius 12px, subtle shadow
- **Hover Effect**: Slight elevation increase, transform translateY(-2px)
- **Member Events**: 2px burgundy border for visual distinction

#### Props Interface
```typescript
interface EventCardProps {
  event: {
    id: string;
    title: string;
    type: 'CLASS' | 'SOCIAL' | 'MEMBER';
    date: string;
    startTime: string;
    endTime: string;
    instructor?: string;
    description: string;
    price: {
      type: 'sliding' | 'fixed';
      min?: number;
      max?: number;
      amount?: number;
    };
    capacity: {
      total: number;
      taken: number;
    };
    isMemberOnly: boolean;
    requiresVetting: boolean;
  };
  userRole: 'anonymous' | 'member' | 'vetted' | 'admin';
  onRegister: (eventId: string) => void;
  onRSVP: (eventId: string) => void;
}
```

#### Mantine Component Structure
```typescript
import { Paper, Badge, Title, Text, Group, Stack, Progress, Button, Box } from '@mantine/core';

<Paper
  withBorder
  shadow="sm"
  p="md"
  radius="md"
  style={{
    transition: 'all 0.2s ease',
    '&:hover': {
      transform: 'translateY(-2px)',
      boxShadow: '0 4px 20px rgba(0,0,0,0.1)'
    }
  }}
>
  <Stack gap="sm">
    {/* Event Header */}
    <Group justify="space-between" align="flex-start">
      <Group gap="xs">
        <Badge
          variant="light"
          color={event.type === 'CLASS' ? 'green' : 'orange'}
          size="sm"
        >
          {event.type}
        </Badge>
        <Title order={3} size="md">
          {event.title}
        </Title>
      </Group>
    </Group>

    {/* Event Meta */}
    <Group gap="md">
      <Text size="sm" c="dimmed">
        🕐 {event.startTime} - {event.endTime}
      </Text>
      {event.instructor && (
        <Text size="sm" c="dimmed">
          👤 {event.instructor}
        </Text>
      )}
    </Group>

    {/* Description */}
    <Text size="sm" c="dimmed" lineClamp={2}>
      {event.description}
    </Text>

    {/* Footer */}
    <Group justify="space-between" align="center">
      <Group gap="md">
        {/* Pricing */}
        <Text fw={600} c="green">
          {formatPrice(event.price)}
        </Text>
        
        {/* Capacity */}
        <Group gap="xs">
          <Text size="sm" c="dimmed">
            {event.capacity.total - event.capacity.taken} of {event.capacity.total} available
          </Text>
          <Progress
            value={(event.capacity.taken / event.capacity.total) * 100}
            color={event.capacity.taken / event.capacity.total > 0.8 ? 'red' : 'wcr'}
            size="sm"
            w={80}
          />
        </Group>
      </Group>

      {/* Actions */}
      <Group gap="xs">
        {renderActionButtons(event, userRole)}
      </Group>
    </Group>
  </Stack>
</Paper>
```

#### State Variations
1. **Default**: Normal display with actions
2. **Member-Only (Anonymous)**: Limited description, login prompt
3. **Full Capacity**: Waitlist button instead of register
4. **Member-Only Preview**: Border highlight for demonstration

### 2. EventFilters Component

#### Purpose
Filter and search interface for event discovery with real-time results.

#### Visual Design
- **Container**: White background, border radius 12px, subtle shadow
- **Filter Pills**: Chip components with burgundy active state
- **Layout**: Horizontal on desktop, vertical stack on mobile

#### Props Interface
```typescript
interface EventFiltersProps {
  filters: {
    eventType: 'all' | 'classes' | 'member-only';
    dateRange: 'week' | 'month' | 'custom';
    instructor?: string;
  };
  instructors: string[];
  onFiltersChange: (filters: EventFiltersProps['filters']) => void;
  resultCount: number;
}
```

#### Mantine Component Structure
```typescript
import { Paper, Group, Chip, Select, Text, Stack } from '@mantine/core';

<Paper withBorder shadow="sm" p="md" radius="md">
  <Stack gap="md">
    <Group justify="space-between" align="center">
      <Text fw={600} size="lg">Filter Events</Text>
      <Text size="sm" c="dimmed">
        {resultCount} events found
      </Text>
    </Group>

    <Group gap="md" wrap="wrap">
      {/* Event Type Filter */}
      <Chip.Group
        value={filters.eventType}
        onChange={(value) => onFiltersChange({ ...filters, eventType: value })}
      >
        <Group gap="xs">
          <Chip value="all">All Events</Chip>
          <Chip value="classes">Classes Only</Chip>
          <Chip value="member-only">Member Events Only</Chip>
        </Group>
      </Chip.Group>

      {/* Instructor Filter */}
      <Select
        placeholder="Filter by instructor"
        data={instructors}
        value={filters.instructor}
        onChange={(value) => onFiltersChange({ ...filters, instructor: value })}
        clearable
        w={200}
      />

      {/* Past Events Link */}
      <Text size="sm" component="a" href="/events/past" c="wcr.7">
        View Past Events →
      </Text>
    </Group>
  </Stack>
</Paper>
```

#### Responsive Behavior
- **Desktop**: Horizontal layout with space-between alignment
- **Mobile**: Vertical stack, full-width filter components
- **Tablet**: Wrap-friendly layout with appropriate gaps

### 3. EventDetailHero Component

#### Purpose
Display comprehensive event information in detail page header with key details.

#### Visual Design
- **Layout**: Clean typography hierarchy with meta information
- **Badges**: Event type with appropriate colors
- **Meta Info**: Icons with structured information display

#### Props Interface
```typescript
interface EventDetailHeroProps {
  event: {
    title: string;
    type: 'CLASS' | 'SOCIAL' | 'MEMBER';
    date: string;
    startTime: string;
    endTime: string;
    duration: string;
    maxCapacity: number;
    description: string;
  };
}
```

#### Mantine Component Structure
```typescript
import { Badge, Title, Group, Text, Stack } from '@mantine/core';

<Stack gap="md">
  <Badge
    variant="light"
    color={event.type === 'CLASS' ? 'green' : 'orange'}
    size="lg"
  >
    {event.type}
  </Badge>

  <Title order={1} size="2.25rem" fw={700}>
    {event.title}
  </Title>

  <Group gap="xl" wrap="wrap">
    <Group gap="xs">
      <Text span>📅</Text>
      <Text fw={600}>{event.date}</Text>
    </Group>
    <Group gap="xs">
      <Text span>🕐</Text>
      <Text fw={600}>{event.startTime} - {event.endTime}</Text>
      <Text c="dimmed">({event.duration})</Text>
    </Group>
    <Group gap="xs">
      <Text span>👥</Text>
      <Text fw={600}>{event.maxCapacity} person</Text>
      <Text c="dimmed">maximum</Text>
    </Group>
  </Group>
</Stack>
```

### 4. RegistrationSidebar Component

#### Purpose
Sticky sidebar containing pricing, capacity, and registration form with real-time updates.

#### Visual Design
- **Sticky Positioning**: Follows scroll on desktop
- **Card Design**: Clean white background with subtle borders
- **Progressive Disclosure**: Price selection, form fields, policies

#### Props Interface
```typescript
interface RegistrationSidebarProps {
  event: {
    id: string;
    price: PriceStructure;
    capacity: CapacityInfo;
    isRegistrationOpen: boolean;
    requiresVetting: boolean;
  };
  userRole: UserRole;
  onRegister: (registrationData: RegistrationData) => void;
  onRSVP: (rsvpData: RSVPData) => void;
}
```

#### Mantine Component Structure
```typescript
import { Paper, Text, Progress, Button, Stack, Group } from '@mantine/core';

<Box pos="sticky" top={20}>
  <Stack gap="md">
    {/* Price Display Card */}
    <Paper withBorder p="md" radius="md">
      <Stack gap="sm">
        <Text size="xl" fw={600} c="green">
          {formatPriceDisplay(event.price)}
        </Text>
        <Text size="sm" c="dimmed">
          {event.price.type === 'sliding' ? 'Sliding scale pricing' : 'Fixed price'}
        </Text>

        {/* Capacity Display */}
        <Group gap="xs" align="center">
          <Text size="sm" c="dimmed">
            {event.capacity.available} of {event.capacity.total} spots available
          </Text>
          <Progress
            value={(event.capacity.taken / event.capacity.total) * 100}
            color={getCapacityColor(event.capacity)}
            size="sm"
            flex={1}
          />
        </Group>

        {/* Registration Form or Actions */}
        {renderRegistrationInterface(event, userRole)}
      </Stack>
    </Paper>

    {/* Venue Information Card */}
    <Paper withBorder p="md" radius="md">
      <Stack gap="sm">
        <Text fw={600} size="lg">Venue Information</Text>
        <VenueHiddenDisplay />
      </Stack>
    </Paper>

    {/* Policy Warning */}
    <Alert color="yellow" title="Refund Policy">
      You can receive a full refund up to 48 hours before the event starts.
    </Alert>
  </Stack>
</Box>
```

### 5. SlidingScalePriceSelector Component

#### Purpose
Interactive price selection for sliding scale events with accessibility and visual feedback.

#### Visual Design
- **Slider Design**: Custom-styled range input with brand colors
- **Real-time Updates**: Live price display as user drags
- **Accessibility**: Keyboard navigation and screen reader support

#### Props Interface
```typescript
interface SlidingScalePriceSelectorProps {
  priceRange: {
    min: number;
    max: number;
  };
  selectedPrice: number;
  onPriceChange: (price: number) => void;
  ticketType: 'individual' | 'couple';
}
```

#### Mantine Component Structure
```typescript
import { Stack, Group, Text, Slider, Box } from '@mantine/core';

<Stack gap="sm">
  <Group justify="space-between" align="baseline">
    <Text fw={600}>Choose Your Price</Text>
    <Text size="xl" fw={600} c="wcr.7">
      ${selectedPrice}
    </Text>
  </Group>

  <Slider
    min={priceRange.min}
    max={priceRange.max}
    value={selectedPrice}
    onChange={onPriceChange}
    color="wcr"
    size="md"
    marks={[
      { value: priceRange.min, label: `$${priceRange.min}` },
      { value: priceRange.max, label: `$${priceRange.max}` }
    ]}
    styles={{
      track: { height: 6 },
      thumb: { 
        width: 24, 
        height: 24,
        boxShadow: '0 2px 4px rgba(0,0,0,0.2)'
      }
    }}
  />

  <Box bg="yellow.0" p="sm" style={{ borderRadius: 4 }}>
    <Text size="xs" c="yellow.9">
      We use sliding scale pricing to make our classes accessible. 
      Pay what you can afford within this range - no questions asked.
    </Text>
  </Box>
</Stack>
```

### 6. CapacityIndicator Component

#### Purpose
Visual representation of event capacity with color-coded warnings and accessibility.

#### Visual Design
- **Progress Bar**: Linear indicator with contextual colors
- **Text Display**: Clear numerical capacity information
- **Warning States**: Color changes for low availability

#### Props Interface
```typescript
interface CapacityIndicatorProps {
  capacity: {
    total: number;
    taken: number;
    available: number;
  };
  size?: 'sm' | 'md' | 'lg';
  showText?: boolean;
  warningThreshold?: number; // Default 0.8 (80%)
}
```

#### Mantine Component Structure
```typescript
import { Group, Text, Progress } from '@mantine/core';

const getCapacityColor = (percentage: number, threshold: number) => {
  if (percentage >= threshold) return 'red';
  if (percentage >= 0.6) return 'yellow';
  return 'wcr';
};

const getCapacityText = (capacity: CapacityProps['capacity'], percentage: number, threshold: number) => {
  if (capacity.available === 0) return 'Full - Join Waitlist';
  if (percentage >= threshold) return `Only ${capacity.available} spots left!`;
  return `${capacity.available} of ${capacity.total} spots available`;
};

<Group gap="xs" align="center">
  <Text 
    size={size === 'lg' ? 'md' : 'sm'}
    c={percentage >= warningThreshold ? 'red' : 'dimmed'}
  >
    {getCapacityText(capacity, percentage, warningThreshold)}
  </Text>
  
  <Progress
    value={percentage}
    color={getCapacityColor(percentage, warningThreshold)}
    size={size}
    w={size === 'lg' ? 120 : 80}
  />
</Group>
```

## Responsive Design Specifications

### Breakpoints (Mantine Standard)
- **xs**: 0px - 575px (Mobile)
- **sm**: 576px - 767px (Large Mobile)  
- **md**: 768px - 991px (Tablet)
- **lg**: 992px - 1199px (Desktop)
- **xl**: 1200px+ (Large Desktop)

### Event List Page Responsive Behavior

#### Mobile (xs, sm)
```typescript
// Filter section stacks vertically
<Stack gap="md">
  <Text fw={600} size="lg">Filter Events</Text>
  <Stack gap="sm">
    <Chip.Group>
      <Stack gap="xs">
        <Chip value="all">All Events</Chip>
        <Chip value="classes">Classes Only</Chip>
        <Chip value="member-only">Member Events Only</Chip>
      </Stack>
    </Chip.Group>
    <Select placeholder="Filter by instructor" />
  </Stack>
</Stack>

// Event cards optimize for mobile
<Paper p="sm"> {/* Reduced padding */}
  <Stack gap="xs">
    {/* Content optimized for narrow width */}
    <Group justify="space-between" wrap="wrap">
      {/* Price and capacity stack on mobile */}
    </Group>
    <Stack gap="xs">
      {/* Action buttons full width */}
      <Button fullWidth>Register Now</Button>
    </Stack>
  </Stack>
</Paper>
```

#### Desktop (md, lg, xl)
```typescript
// Two-column layout maintained
<Grid>
  <Grid.Col span={8}>
    {/* Event content */}
  </Grid.Col>
  <Grid.Col span={4}>
    {/* Registration sidebar */}
  </Grid.Col>
</Grid>
```

### Event Detail Page Responsive Behavior

#### Mobile Layout
```typescript
<Stack gap="md">
  {/* Registration card moves to top, sticky */}
  <Box pos="sticky" top={0} style={{ zIndex: 100 }}>
    <RegistrationSidebar />
  </Box>
  
  {/* Content flows below */}
  <Paper p="md">
    <EventContent />
  </Paper>
</Stack>
```

#### Desktop Layout
```typescript
<Grid>
  <Grid.Col span={8}>
    <EventContent />
  </Grid.Col>
  <Grid.Col span={4}>
    <Box pos="sticky" top={20}>
      <RegistrationSidebar />
    </Box>
  </Grid.Col>
</Grid>
```

## Accessibility Requirements

### WCAG 2.1 AA Compliance

#### Color Contrast
- **Text on Background**: Minimum 4.5:1 contrast ratio
- **Large Text**: Minimum 3:1 contrast ratio  
- **Interactive Elements**: Minimum 4.5:1 contrast ratio
- **Focus Indicators**: Minimum 3:1 contrast ratio

#### Keyboard Navigation
```typescript
// All interactive elements keyboard accessible
<Button
  onKeyDown={(event) => {
    if (event.key === 'Enter' || event.key === ' ') {
      handleAction();
    }
  }}
>
  Register Now
</Button>

// Slider accessibility
<Slider
  aria-label="Select your price within sliding scale range"
  aria-valuetext={`${selectedPrice} dollars`}
  aria-describedby="price-help-text"
/>
```

#### Screen Reader Support
```typescript
// Proper ARIA labels
<Progress
  value={capacityPercentage}
  aria-label={`Event capacity: ${taken} of ${total} spots taken`}
/>

// Live regions for dynamic updates
<div aria-live="polite" aria-atomic="true">
  <Text>Capacity updated: {availableSpots} spots remaining</Text>
</div>

// Semantic HTML structure
<main role="main">
  <section aria-labelledby="events-heading">
    <h1 id="events-heading">Events & Classes</h1>
    <div role="list">
      {events.map(event => (
        <article role="listitem" key={event.id}>
          <EventCard event={event} />
        </article>
      ))}
    </div>
  </section>
</main>
```

### Focus Management
```typescript
// Focus trapping in modals
import { useFocusTrap } from '@mantine/hooks';

const RSVPModal = () => {
  const focusTrapRef = useFocusTrap();
  
  return (
    <Modal opened={opened} onClose={onClose}>
      <div ref={focusTrapRef}>
        {/* Modal content */}
      </div>
    </Modal>
  );
};

// Skip links for keyboard users
<a 
  href="#main-content" 
  className="skip-link"
  style={{
    position: 'absolute',
    top: '-40px',
    left: '6px',
    background: '#000',
    color: '#fff',
    padding: '8px',
    textDecoration: 'none',
    '&:focus': { top: '6px' }
  }}
>
  Skip to main content
</a>
```

## Animation and Interaction Specifications

### Hover Effects
```typescript
// Event card hover animation
<Paper
  style={{
    transition: 'all 200ms ease',
    '&:hover': {
      transform: 'translateY(-2px)',
      boxShadow: '0 4px 20px rgba(0,0,0,0.1)'
    }
  }}
>
```

### Loading States
```typescript
// Skeleton loading for event cards
import { Skeleton } from '@mantine/core';

<Skeleton height={200} radius="md" />
<Skeleton height={20} mt="sm" radius="md" />
<Skeleton height={20} mt="sm" width="70%" radius="md" />

// Spinner for form submissions
<Button loading={isSubmitting}>
  Continue to Payment
</Button>
```

### Form Validation Feedback
```typescript
// Real-time validation with smooth transitions
<TextInput
  error={form.errors.email}
  style={{
    '.mantine-TextInput-error': {
      opacity: form.errors.email ? 1 : 0,
      transition: 'opacity 200ms ease'
    }
  }}
/>
```

### Success States
```typescript
// Registration success animation
<Paper
  bg="green.0"
  p="xl"
  style={{
    animation: 'slideIn 300ms ease-out',
    '@keyframes slideIn': {
      '0%': { opacity: 0, transform: 'translateY(20px)' },
      '100%': { opacity: 1, transform: 'translateY(0)' }
    }
  }}
>
  <Stack align="center">
    <Text size={48}>✓</Text>
    <Title order={2} c="green">Registration Complete!</Title>
    <Text c="dimmed">Check your email for confirmation and venue details.</Text>
  </Stack>
</Paper>
```

## Error Handling and Edge Cases

### Error States
1. **Network Failures**: Retry mechanism with user feedback
2. **Capacity Changes**: Real-time updates during registration
3. **Authentication Expiry**: Graceful re-authentication prompt
4. **Payment Failures**: Clear error messages and retry options

### Form Validation Patterns
```typescript
// Comprehensive form validation
const registrationSchema = yup.object({
  email: yup.string().email('Invalid email').required('Email is required'),
  fullName: yup.string().min(2, 'Name too short').required('Name is required'),
  selectedPrice: yup.number()
    .min(priceRange.min, `Minimum price is $${priceRange.min}`)
    .max(priceRange.max, `Maximum price is $${priceRange.max}`)
    .required('Please select a price')
});

// Real-time validation feedback
<TextInput
  {...form.getInputProps('email')}
  error={form.errors.email}
  rightSection={form.errors.email ? '⚠️' : form.values.email ? '✓' : null}
/>
```

### Edge Case Handling
1. **Rapid Capacity Changes**: Optimistic UI with rollback capability
2. **Concurrent Registrations**: Server-side validation with user notification
3. **Session Expiry**: Auto-save form data and graceful re-authentication
4. **Network Interruptions**: Offline capability and sync when restored

## Performance Considerations

### Image Optimization
```typescript
// Lazy loading for instructor photos and event images
<Image
  src={instructor.photo}
  alt={`Photo of ${instructor.name}`}
  loading="lazy"
  placeholder="blur"
  width={80}
  height={80}
  radius="xl"
/>
```

### Bundle Optimization
- **Code Splitting**: Lazy load registration components
- **Tree Shaking**: Import only used Mantine components
- **Asset Optimization**: Optimize images and fonts

### Caching Strategy
```typescript
// Smart caching for event data
import { useQuery } from '@tanstack/react-query';

const useEvents = (filters: EventFilters) => {
  return useQuery({
    queryKey: ['events', filters],
    queryFn: () => fetchEvents(filters),
    staleTime: 5 * 60 * 1000, // 5 minutes
    refetchInterval: 30 * 1000, // Refetch every 30s for capacity updates
  });
};
```

## Success Metrics

### Performance Targets
- **Page Load Time**: <2 seconds for events list
- **Time to Interactive**: <3 seconds
- **Lighthouse Score**: >90 for Performance, Accessibility, Best Practices

### User Experience Metrics
- **Registration Conversion**: >85% completion rate
- **Mobile Experience**: >90% mobile usability score
- **Accessibility**: WCAG 2.1 AA compliance (95%+ automated tests)

### Development Efficiency
- **Component Reusability**: 80% of UI components reused across pages
- **Type Safety**: Zero TypeScript errors in production builds
- **Test Coverage**: >80% component test coverage

---

**Next Steps**: 
1. Review and approve design specifications
2. Create Mantine component mapping document  
3. Begin component development with design system integration
4. Implement accessibility testing and validation

**Dependencies**:
- Phase 3 Backend APIs operational
- Mantine v7 theme configuration complete
- Authentication system integration ready

**Deliverables Ready For**:
- React Developer implementation
- Design-to-development handoff
- Component library integration
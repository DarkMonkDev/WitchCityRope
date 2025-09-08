# Mantine v7 Component Mapping: Public Events Pages
<!-- Last Updated: 2025-09-07 -->
<!-- Version: 1.0 -->
<!-- Owner: UI Designer Agent -->
<!-- Status: Ready for Development -->

## Overview
This document provides a comprehensive mapping of UI elements from the existing wireframes to specific Mantine v7 components, ensuring consistent implementation and optimal use of the component library's capabilities.

## Installation Requirements

### Core Mantine Packages
```bash
npm install @mantine/core@7.x \
             @mantine/hooks@7.x \
             @mantine/form@7.x \
             @mantine/notifications@7.x \
             @mantine/spotlight@7.x \
             @mantine/dates@7.x
```

### Additional Dependencies
```bash
npm install @tabler/icons-react \
             dayjs \
             yup \
             @tanstack/react-query
```

## Global Theme Configuration

### WitchCityRope Theme Setup
```typescript
// theme/wcr-theme.ts
import { MantineProvider, createTheme, MantineColorsTuple } from '@mantine/core';

const wcrColors: MantineColorsTuple = [
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
];

export const wcrTheme = createTheme({
  colors: {
    wcr: wcrColors,
    // Additional color for existing brand compatibility
    brand: ['#f8f4e6', '#e8ddd4', '#d4a5a5', '#c48b8b', '#b47171', '#a45757', '#9b4a75', '#8B4513', '#6b0119', '#2c2c2c']
  },
  primaryColor: 'wcr',
  fontFamily: 'Source Sans 3, -apple-system, BlinkMacSystemFont, Segoe UI, sans-serif',
  headings: {
    fontFamily: 'Bodoni Moda, serif',
    fontWeight: '700'
  },
  components: {
    Button: {
      defaultProps: {
        radius: 'md'
      },
      styles: {
        root: {
          fontWeight: 600,
          transition: 'all 200ms ease'
        }
      }
    },
    Paper: {
      defaultProps: {
        radius: 'md',
        withBorder: true
      },
      styles: {
        root: {
          transition: 'all 200ms ease'
        }
      }
    },
    Container: {
      defaultProps: {
        size: 'xl' // 1200px to match wireframes
      }
    }
  },
  other: {
    // Custom theme values for WCR-specific styling
    capacityWarning: '#d32f2f',
    successGreen: '#2e7d32',
    backgroundGray: '#f5f5f5'
  }
});
```

### App Provider Setup
```typescript
// App.tsx
import { MantineProvider } from '@mantine/core';
import { Notifications } from '@mantine/notifications';
import { wcrTheme } from './theme/wcr-theme';

function App() {
  return (
    <MantineProvider theme={wcrTheme}>
      <Notifications />
      {/* Your app content */}
    </MantineProvider>
  );
}
```

## Page-Level Component Mappings

### Events List Page (`/events`)

#### Page Structure Components
| Wireframe Element | Mantine Component | Props/Configuration |
|-------------------|-------------------|-------------------|
| **Page Container** | `Container` | `size="xl"` (1200px max-width) |
| **Utility Bar** | `Group` + `Box` | Dark background, `justify="flex-end"` |
| **Header** | `Header` + `Group` | White background, `justify="space-between"` |
| **Page Hero** | `Box` | Gradient background, centered content |
| **Filters Section** | `Paper` | `withBorder`, `shadow="sm"`, padding="md" |
| **Events Grid** | `Stack` | `gap="xl"` for date sections |
| **Date Sections** | `Stack` | `gap="md"` with `Title` headers |

#### Filter Components
```typescript
// EventFilters.tsx
import { Paper, Group, Chip, Select, Text, Stack } from '@mantine/core';

const EventFilters = ({ filters, onFiltersChange, instructors, resultCount }) => (
  <Paper withBorder shadow="sm" p="md">
    <Stack gap="md">
      <Group justify="space-between" align="center">
        <Text fw={600} size="lg">Filter Events</Text>
        <Text size="sm" c="dimmed">{resultCount} events found</Text>
      </Group>
      
      <Group gap="md">
        {/* Event Type Filter */}
        <Chip.Group value={filters.eventType} onChange={handleEventTypeChange}>
          <Group gap="xs">
            <Chip value="all" color="wcr">All Events</Chip>
            <Chip value="classes" color="wcr">Classes Only</Chip>
            <Chip value="member-only" color="wcr">Member Events Only</Chip>
          </Group>
        </Chip.Group>
        
        {/* Instructor Filter */}
        <Select
          placeholder="Filter by instructor"
          data={instructors}
          value={filters.instructor}
          onChange={handleInstructorChange}
          clearable
          w={200}
        />
        
        <Text size="sm" component="a" href="/events/past" c="wcr.7">
          View Past Events →
        </Text>
      </Group>
    </Stack>
  </Paper>
);
```

#### Event Card Components
```typescript
// EventCard.tsx
import { 
  Paper, Badge, Title, Text, Group, Stack, Progress, 
  Button, Box, ActionIcon 
} from '@mantine/core';

const EventCard = ({ event, userRole, onRegister, onRSVP }) => (
  <Paper
    withBorder
    shadow="sm" 
    p="md"
    style={{
      transition: 'all 200ms ease',
      '&:hover': {
        transform: 'translateY(-2px)',
        boxShadow: '0 4px 20px rgba(0,0,0,0.1)'
      },
      ...(event.isMemberOnly && { 
        borderColor: 'var(--mantine-color-wcr-7)',
        borderWidth: '2px'
      })
    }}
  >
    <Stack gap="sm">
      {/* Event Header */}
      <Group justify="space-between" align="flex-start">
        <Group gap="xs" align="center">
          <Badge
            variant="light"
            color={event.type === 'CLASS' ? 'green' : 'orange'}
            size="sm"
          >
            {event.type}
          </Badge>
          <Title order={3} size="lg" fw={600}>
            {event.title}
          </Title>
        </Group>
      </Group>

      {/* Event Meta */}
      <Group gap="md">
        <Group gap={4}>
          <Text span>🕐</Text>
          <Text size="sm" c="dimmed">
            {event.startTime} - {event.endTime}
          </Text>
        </Group>
        {event.instructor && (
          <Group gap={4}>
            <Text span>👤</Text>
            <Text size="sm" c="dimmed">
              {event.instructor}
            </Text>
          </Group>
        )}
      </Group>

      {/* Description */}
      <Text size="sm" c="dimmed" lineClamp={2}>
        {event.description}
      </Text>

      {/* Footer */}
      <Group justify="space-between" align="center" mt="xs">
        <Group gap="md">
          <Text fw={600} c="green" size="md">
            {formatPrice(event.price)}
          </Text>
          
          <Group gap="xs" align="center">
            <Text size="sm" c="dimmed">
              {event.capacity.available} of {event.capacity.total} available
            </Text>
            <Progress
              value={(event.capacity.taken / event.capacity.total) * 100}
              color={getCapacityColor(event.capacity)}
              size="sm"
              w={80}
            />
          </Group>
        </Group>

        <Group gap="xs">
          {renderActionButtons(event, userRole)}
        </Group>
      </Group>
    </Stack>
  </Paper>
);
```

### Event Detail Page (`/events/:id`)

#### Layout Structure
```typescript
// EventDetailPage.tsx
import { Container, Grid, Stack, Breadcrumbs, Anchor } from '@mantine/core';

const EventDetailPage = ({ event }) => (
  <Container size="xl">
    {/* Breadcrumbs */}
    <Breadcrumbs separator="›" mt="md" mb="lg">
      <Anchor href="/">Home</Anchor>
      <Anchor href="/events">Events & Classes</Anchor>
      <Text>{event.title}</Text>
    </Breadcrumbs>

    {/* Main Layout */}
    <Grid>
      <Grid.Col span={{ base: 12, md: 8 }}>
        <EventContent event={event} />
      </Grid.Col>
      
      <Grid.Col span={{ base: 12, md: 4 }}>
        <Box pos={{ base: 'static', md: 'sticky' }} top={20}>
          <RegistrationSidebar event={event} />
        </Box>
      </Grid.Col>
    </Grid>
  </Container>
);
```

#### Event Content Components
```typescript
// EventContent.tsx
import { Paper, Title, Badge, Text, Group, Stack, List } from '@mantine/core';

const EventContent = ({ event }) => (
  <Paper withBorder p="xl" shadow="sm">
    <Stack gap="xl">
      {/* Event Header */}
      <Stack gap="md">
        <Badge variant="light" color="green" size="lg">
          {event.type}
        </Badge>
        
        <Title order={1} size="2.25rem" fw={700}>
          {event.title}
        </Title>
        
        <Group gap="xl">
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

      {/* Content Sections */}
      <Stack gap="xl">
        <ContentSection title="About This Class">
          <Text>{event.description}</Text>
        </ContentSection>

        <ContentSection title="What You'll Learn">
          <List spacing="xs" size="sm">
            {event.learningOutcomes.map((outcome, index) => (
              <List.Item key={index}>{outcome}</List.Item>
            ))}
          </List>
        </ContentSection>

        <ContentSection title="Your Instructor">
          <InstructorCard instructor={event.instructor} />
        </ContentSection>
      </Stack>
    </Stack>
  </Paper>
);

const ContentSection = ({ title, children }) => (
  <Stack gap="md">
    <Title order={2} size="1.5rem">
      {title}
    </Title>
    {children}
  </Stack>
);
```

#### Registration Sidebar Components
```typescript
// RegistrationSidebar.tsx
import { 
  Paper, Text, Progress, Button, Stack, Group, 
  TextInput, Slider, Alert, Box 
} from '@mantine/core';

const RegistrationSidebar = ({ event, userRole }) => (
  <Stack gap="md">
    {/* Price Display Card */}
    <Paper withBorder p="md">
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

        {/* Registration Form */}
        {event.isRegistrationOpen && (
          <RegistrationForm event={event} />
        )}
      </Stack>
    </Paper>

    {/* Venue Information */}
    <Paper withBorder p="md">
      <Stack gap="sm">
        <Text fw={600} size="lg">Venue Information</Text>
        <VenueHiddenDisplay />
      </Stack>
    </Paper>

    {/* Policy Alert */}
    <Alert color="yellow" title="Refund Policy">
      You can receive a full refund up to 48 hours before the event starts.
    </Alert>
  </Stack>
);
```

## Specialized Component Mappings

### Sliding Scale Price Selector
```typescript
// SlidingScalePriceSelector.tsx
import { Stack, Group, Text, Slider, Box } from '@mantine/core';

const SlidingScalePriceSelector = ({ 
  priceRange, 
  selectedPrice, 
  onPriceChange,
  ticketType 
}) => (
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
      aria-label="Select your price within sliding scale range"
    />

    <Box bg="yellow.0" p="sm" style={{ borderRadius: 4 }}>
      <Text size="xs" c="yellow.9" ta="center">
        We use sliding scale pricing to make our classes accessible. 
        Pay what you can afford within this range - no questions asked.
      </Text>
    </Box>
  </Stack>
);
```

### Capacity Indicator Component
```typescript
// CapacityIndicator.tsx
import { Group, Text, Progress } from '@mantine/core';

interface CapacityIndicatorProps {
  capacity: {
    total: number;
    taken: number;
    available: number;
  };
  size?: 'sm' | 'md' | 'lg';
  showText?: boolean;
  warningThreshold?: number;
}

const CapacityIndicator = ({ 
  capacity, 
  size = 'md', 
  showText = true,
  warningThreshold = 0.8 
}) => {
  const percentage = capacity.taken / capacity.total;
  
  const getCapacityColor = () => {
    if (percentage >= warningThreshold) return 'red';
    if (percentage >= 0.6) return 'yellow';
    return 'wcr';
  };

  const getCapacityText = () => {
    if (capacity.available === 0) return 'Full - Join Waitlist';
    if (percentage >= warningThreshold) return `Only ${capacity.available} spots left!`;
    return `${capacity.available} of ${capacity.total} spots available`;
  };

  return (
    <Group gap="xs" align="center">
      {showText && (
        <Text 
          size={size === 'lg' ? 'md' : 'sm'}
          c={percentage >= warningThreshold ? 'red' : 'dimmed'}
        >
          {getCapacityText()}
        </Text>
      )}
      
      <Progress
        value={percentage * 100}
        color={getCapacityColor()}
        size={size}
        w={size === 'lg' ? 120 : 80}
        aria-label={`Event capacity: ${capacity.taken} of ${capacity.total} spots taken`}
      />
    </Group>
  );
};
```

### Instructor Profile Card
```typescript
// InstructorCard.tsx
import { Paper, Group, Avatar, Text, Stack, Anchor } from '@mantine/core';

const InstructorCard = ({ instructor }) => (
  <Paper bg="gray.0" p="md" radius="md">
    <Group gap="md" align="flex-start">
      <Avatar
        src={instructor.photo}
        alt={`Photo of ${instructor.name}`}
        size="lg"
        radius="xl"
      >
        {instructor.name.split(' ').map(n => n[0]).join('')}
      </Avatar>
      
      <Stack gap="xs" flex={1}>
        <Text fw={600} size="lg">
          {instructor.name}
        </Text>
        
        <Text size="sm" c="dimmed">
          {instructor.bio}
        </Text>
        
        <Anchor href={`/instructors/${instructor.id}`} size="sm" c="wcr.7">
          View full instructor profile →
        </Anchor>
      </Stack>
    </Group>
  </Paper>
);
```

## Form Components and Validation

### Registration Form
```typescript
// RegistrationForm.tsx
import { useForm, yupResolver } from '@mantine/form';
import { 
  TextInput, Radio, Stack, Button, 
  Group, Paper, Alert 
} from '@mantine/core';
import * as yup from 'yup';

const registrationSchema = yup.object({
  email: yup.string().email('Invalid email').required('Email is required'),
  fullName: yup.string().min(2, 'Name too short').required('Name is required'),
  ticketType: yup.string().required('Please select a ticket type'),
  selectedPrice: yup.number()
    .min(35, 'Minimum price is $35')
    .max(55, 'Maximum price is $55')
    .required('Please select a price')
});

const RegistrationForm = ({ event, onSubmit }) => {
  const form = useForm({
    initialValues: {
      email: '',
      fullName: '',
      ticketType: 'individual',
      selectedPrice: 45
    },
    validate: yupResolver(registrationSchema)
  });

  return (
    <Stack gap="md" component="form" onSubmit={form.onSubmit(onSubmit)}>
      {/* Ticket Type Selection */}
      <Stack gap="xs">
        <Text fw={600} size="sm">Select Ticket Type</Text>
        <Radio.Group {...form.getInputProps('ticketType')}>
          <Paper withBorder p="sm">
            <Radio
              value="individual"
              label={
                <Group justify="space-between" w="100%">
                  <Text>Individual Ticket</Text>
                  <Text c="green" fw={600}>$35 - $55 sliding scale</Text>
                </Group>
              }
            />
          </Paper>
          
          <Paper withBorder p="sm">
            <Radio
              value="couple"
              label={
                <Group justify="space-between" w="100%">
                  <Text>Couple Ticket (2 people)</Text>
                  <Text c="green" fw={600}>$60 - $100 sliding scale</Text>
                </Group>
              }
            />
          </Paper>
        </Radio.Group>
      </Stack>

      {/* Price Selection */}
      <SlidingScalePriceSelector
        priceRange={{ min: 35, max: 55 }}
        selectedPrice={form.values.selectedPrice}
        onPriceChange={(price) => form.setFieldValue('selectedPrice', price)}
        ticketType={form.values.ticketType}
      />

      {/* Contact Information */}
      <TextInput
        label="Email for confirmation"
        placeholder="your@email.com"
        required
        {...form.getInputProps('email')}
      />

      <TextInput
        label="Name for attendance list"
        placeholder="How you'd like to be listed"
        required
        {...form.getInputProps('fullName')}
      />

      {/* Submit Button */}
      <Button 
        type="submit" 
        color="wcr" 
        size="md"
        loading={form.isSubmitting}
        fullWidth
      >
        Continue to Payment
      </Button>
    </Stack>
  );
};
```

### RSVP Modal
```typescript
// RSVPModal.tsx
import { Modal, TextInput, Textarea, Button, Stack, Text } from '@mantine/core';
import { useForm } from '@mantine/form';

const RSVPModal = ({ opened, onClose, event, onSubmit }) => {
  const form = useForm({
    initialValues: {
      email: '',
      fullName: '',
      dietaryRestrictions: '',
      emergencyContact: ''
    }
  });

  return (
    <Modal
      opened={opened}
      onClose={onClose}
      title={`RSVP for ${event.title}`}
      size="md"
      centered
    >
      <Stack gap="md" component="form" onSubmit={form.onSubmit(onSubmit)}>
        <Text size="sm" c="dimmed">
          Reserve your spot for this free social event. You can still purchase a 
          support ticket later to help cover venue costs.
        </Text>

        <TextInput
          label="Email address"
          placeholder="your@email.com"
          required
          {...form.getInputProps('email')}
        />

        <TextInput
          label="Name for attendance list"
          placeholder="How you'd like to be listed"
          required
          {...form.getInputProps('fullName')}
        />

        <TextInput
          label="Emergency contact"
          placeholder="Name and phone number"
          {...form.getInputProps('emergencyContact')}
        />

        <Textarea
          label="Dietary restrictions or accommodations"
          placeholder="Any dietary needs or accessibility requirements"
          minRows={2}
          {...form.getInputProps('dietaryRestrictions')}
        />

        <Group gap="sm" justify="flex-end" mt="md">
          <Button variant="outline" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" color="wcr" loading={form.isSubmitting}>
            Confirm RSVP
          </Button>
        </Group>
      </Stack>
    </Modal>
  );
};
```

## Loading and Error States

### Skeleton Loading Components
```typescript
// EventCardSkeleton.tsx
import { Paper, Skeleton, Stack, Group } from '@mantine/core';

const EventCardSkeleton = () => (
  <Paper withBorder p="md">
    <Stack gap="sm">
      <Group justify="space-between">
        <Group gap="xs">
          <Skeleton height={20} width={60} radius="xl" />
          <Skeleton height={24} width={200} />
        </Group>
      </Group>
      
      <Group gap="md">
        <Skeleton height={16} width={120} />
        <Skeleton height={16} width={100} />
      </Group>
      
      <Skeleton height={40} />
      
      <Group justify="space-between">
        <Group gap="md">
          <Skeleton height={20} width={80} />
          <Skeleton height={16} width={120} />
        </Group>
        <Skeleton height={36} width={120} />
      </Group>
    </Stack>
  </Paper>
);

const EventListSkeleton = () => (
  <Stack gap="xl">
    {Array.from({ length: 3 }, (_, index) => (
      <Stack key={index} gap="md">
        <Skeleton height={32} width={200} />
        <Stack gap="sm">
          <EventCardSkeleton />
          <EventCardSkeleton />
        </Stack>
      </Stack>
    ))}
  </Stack>
);
```

### Error State Components
```typescript
// ErrorStates.tsx
import { Alert, Button, Stack, Text, Title } from '@mantine/core';

const NetworkErrorAlert = ({ onRetry }) => (
  <Alert color="red" title="Connection Error" mb="md">
    <Stack gap="sm">
      <Text size="sm">
        Unable to load events. Please check your internet connection and try again.
      </Text>
      <Button size="xs" variant="outline" onClick={onRetry}>
        Retry
      </Button>
    </Stack>
  </Alert>
);

const EmptyEventsState = ({ filters }) => (
  <Stack align="center" gap="md" py="xl">
    <Title order={3} c="dimmed">No Events Found</Title>
    <Text c="dimmed" ta="center">
      {filters.eventType === 'all' 
        ? "There are no upcoming events scheduled."
        : `No events found matching your current filters.`
      }
    </Text>
    {filters.eventType !== 'all' && (
      <Button variant="outline" onClick={() => clearFilters()}>
        Clear Filters
      </Button>
    )}
  </Stack>
);
```

## Responsive Design Utilities

### Responsive Helper Components
```typescript
// ResponsiveUtils.tsx
import { useMediaQuery } from '@mantine/hooks';

export const useResponsiveLayout = () => {
  const isMobile = useMediaQuery('(max-width: 767px)');
  const isTablet = useMediaQuery('(max-width: 991px)');
  const isDesktop = useMediaQuery('(min-width: 992px)');
  
  return {
    isMobile,
    isTablet, 
    isDesktop,
    eventCardCols: isMobile ? 1 : isTablet ? 2 : 3,
    sidebarPosition: isMobile ? 'static' : 'sticky',
    filterDirection: isMobile ? 'column' : 'row'
  };
};

// Responsive Container
const ResponsiveEventLayout = ({ children }) => {
  const { isMobile, sidebarPosition } = useResponsiveLayout();
  
  return (
    <Grid>
      <Grid.Col span={{ base: 12, md: 8 }}>
        {children.content}
      </Grid.Col>
      <Grid.Col span={{ base: 12, md: 4 }}>
        <Box pos={sidebarPosition} top={isMobile ? 0 : 20}>
          {children.sidebar}
        </Box>
      </Grid.Col>
    </Grid>
  );
};
```

## Performance Optimization Patterns

### Lazy Loading and Code Splitting
```typescript
// LazyComponents.tsx
import { lazy, Suspense } from 'react';
import { Skeleton, Stack } from '@mantine/core';

// Lazy load heavy components
const RegistrationForm = lazy(() => import('./RegistrationForm'));
const RSVPModal = lazy(() => import('./RSVPModal'));

// Loading fallbacks
const FormSkeleton = () => (
  <Stack gap="md">
    <Skeleton height={40} />
    <Skeleton height={40} />
    <Skeleton height={100} />
    <Skeleton height={40} />
  </Stack>
);

// Usage with Suspense
const EventRegistration = ({ event }) => (
  <Suspense fallback={<FormSkeleton />}>
    <RegistrationForm event={event} />
  </Suspense>
);
```

### Memoization Patterns
```typescript
// OptimizedComponents.tsx
import { memo, useMemo } from 'react';

// Memoize expensive calculations
const EventCard = memo(({ event, userRole }) => {
  const formattedPrice = useMemo(() => 
    formatPrice(event.price), [event.price]
  );
  
  const capacityColor = useMemo(() => 
    getCapacityColor(event.capacity), [event.capacity]
  );
  
  const actionButtons = useMemo(() => 
    renderActionButtons(event, userRole), [event, userRole]
  );

  return (
    <Paper>
      {/* Component JSX */}
    </Paper>
  );
});

EventCard.displayName = 'EventCard';
```

## Testing Utilities

### Test Helpers
```typescript
// TestUtils.tsx
import { render } from '@testing-library/react';
import { MantineProvider } from '@mantine/core';
import { wcrTheme } from '../theme/wcr-theme';

export const renderWithTheme = (component: React.ReactNode) => {
  return render(
    <MantineProvider theme={wcrTheme}>
      {component}
    </MantineProvider>
  );
};

export const mockEvent = {
  id: '1',
  title: 'Test Event',
  type: 'CLASS',
  date: '2024-03-15',
  startTime: '2:00 PM',
  endTime: '5:00 PM',
  instructor: 'Test Instructor',
  description: 'Test description',
  price: { type: 'sliding', min: 35, max: 55 },
  capacity: { total: 12, taken: 4, available: 8 }
};
```

## Implementation Checklist

### Phase 1: Foundation (Week 1)
- [ ] Install and configure Mantine v7 packages
- [ ] Set up WitchCityRope theme configuration
- [ ] Create base layout components (Container, Header, Footer)
- [ ] Implement responsive utilities and hooks

### Phase 2: Core Components (Week 2)
- [ ] Build EventCard component with all states
- [ ] Create EventFilters with real-time filtering
- [ ] Implement CapacityIndicator component
- [ ] Build basic page layouts (Events List, Event Detail)

### Phase 3: Advanced Features (Week 3)
- [ ] Create RegistrationForm with validation
- [ ] Build RSVP modal component
- [ ] Implement SlidingScalePriceSelector
- [ ] Add loading states and error handling

### Phase 4: Polish and Testing (Week 4)
- [ ] Add animations and micro-interactions
- [ ] Implement comprehensive error states
- [ ] Create test utilities and write tests
- [ ] Performance optimization and accessibility audit

---

**Dependencies**: 
- Mantine v7 packages installed
- WitchCityRope theme configuration
- Backend API endpoints available

**Integration Points**:
- Authentication context integration
- API service layer integration  
- Form validation with backend validation
- Real-time capacity updates via WebSocket or polling

**Next Steps**:
1. Review component specifications with development team
2. Begin implementation with foundation components
3. Integrate with existing authentication system
4. Connect to Phase 3 backend APIs
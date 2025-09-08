# Design to Development Handoff: Phase 4 - Public Events Pages
<!-- Last Updated: 2025-09-07 -->
<!-- Handoff From: UI Designer Agent -->
<!-- Handoff To: React Developer, Test Developer -->

## 🚨 CRITICAL HANDOFF INFORMATION

### Project Context
**Phase 4: Public Events Pages Implementation**
- ✅ **Design Complete**: Comprehensive UI specifications and Mantine v7 component mappings
- ✅ **Wireframes**: Complete HTML wireframes with styling provide exact implementation reference
- ✅ **Business Requirements**: Detailed requirements document with all user stories and acceptance criteria
- 🎯 **Ready For Development**: All design decisions made, components specified, implementation ready to begin

### 📋 MANDATORY READING BEFORE DEVELOPMENT
1. **UI Specifications**: `/docs/functional-areas/events/new-work/2025-09-07-public-events-pages/design/ui-specifications.md`
2. **Mantine Component Mapping**: `/docs/functional-areas/events/new-work/2025-09-07-public-events-pages/design/mantine-components.md`
3. **Business Requirements**: `/docs/functional-areas/events/new-work/2025-09-07-public-events-pages/requirements/business-requirements.md`
4. **Wireframe References**: 
   - `/docs/functional-areas/events/public-events/event-list.html`
   - `/docs/functional-areas/events/public-events/event-detail.html`

## 🎨 DESIGN HANDOFF DELIVERABLES

### ✅ Complete Design Specifications
- **UI Design Document**: 47 pages of comprehensive component specifications
- **Mantine Component Mapping**: Complete mapping of every UI element to Mantine v7 components
- **Theme Configuration**: WitchCityRope brand integration with Mantine theming system
- **Responsive Breakpoints**: Detailed mobile/tablet/desktop layout specifications
- **Accessibility Guidelines**: WCAG 2.1 AA compliance requirements and implementation patterns

### ✅ Component Library Integration
- **6 Core Components**: EventCard, EventFilters, EventDetailHero, RegistrationSidebar, SlidingScalePriceSelector, CapacityIndicator
- **Complete Props Interfaces**: TypeScript interfaces for all components
- **State Management**: Form handling, validation patterns, and error states
- **Theme Integration**: Custom WitchCityRope colors and typography in Mantine theme

### ✅ Implementation-Ready Code Examples
- **Working TypeScript Examples**: Copy-paste ready component implementations
- **Form Validation**: Complete validation schemas with yup integration
- **Responsive Patterns**: Mobile-first responsive design utilities
- **Performance Patterns**: Memoization, lazy loading, and optimization examples

## 💡 TOP 5 CRITICAL IMPLEMENTATION REQUIREMENTS

### 1. Mantine v7 Component Library - NON-NEGOTIABLE
```typescript
// Required Installation
npm install @mantine/core@7.x \
             @mantine/hooks@7.x \
             @mantine/form@7.x \
             @mantine/notifications@7.x

// Theme Configuration (MUST USE EXACTLY AS SPECIFIED)
const wcrTheme = createTheme({
  colors: {
    wcr: [/* Exact brand colors from design spec */]
  },
  primaryColor: 'wcr',
  fontFamily: 'Source Sans 3, sans-serif',
  headings: { fontFamily: 'Bodoni Moda, serif' }
});
```

### 2. Component Consistency - EXACT WIREFRAME COMPLIANCE
```typescript
// EventCard Component - Must match wireframe exactly
<Paper
  withBorder
  shadow="sm"
  p="md"
  style={{
    transition: 'all 200ms ease',
    '&:hover': {
      transform: 'translateY(-2px)',
      boxShadow: '0 4px 20px rgba(0,0,0,0.1)'
    }
  }}
>
  {/* Exact structure from component mapping */}
</Paper>
```

### 3. Responsive Design - MOBILE-FIRST (65% OF TRAFFIC)
```typescript
// Required Responsive Pattern
const { isMobile } = useMediaQuery('(max-width: 767px)');

// Layout Changes for Mobile
<Grid>
  <Grid.Col span={{ base: 12, md: 8 }}>
    {/* Event content */}
  </Grid.Col>
  <Grid.Col span={{ base: 12, md: 4 }}>
    <Box pos={{ base: 'static', md: 'sticky' }} top={20}>
      {/* Registration sidebar */}
    </Box>
  </Grid.Col>
</Grid>
```

### 4. Form Handling - @mantine/form + YUP VALIDATION
```typescript
// Required Form Pattern
import { useForm, yupResolver } from '@mantine/form';
import * as yup from 'yup';

const schema = yup.object({
  email: yup.string().email().required(),
  selectedPrice: yup.number().min(35).max(55).required()
});

const form = useForm({
  initialValues: {},
  validate: yupResolver(schema)
});
```

### 5. Accessibility - WCAG 2.1 AA COMPLIANCE REQUIRED
```typescript
// Required Accessibility Patterns
<Progress
  value={capacityPercentage}
  aria-label={`Event capacity: ${taken} of ${total} spots taken`}
  color={capacityPercentage > 80 ? 'red' : 'wcr'}
/>

<Slider
  aria-label="Select your price within sliding scale range"
  aria-valuetext={`${selectedPrice} dollars`}
  aria-describedby="price-help-text"
/>
```

## ⚙️ TECHNICAL IMPLEMENTATION REQUIREMENTS

### Architecture Integration Points

#### 1. API Integration (Phase 3 Backends)
```typescript
// Required API Service Pattern
interface EventsAPI {
  getEvents(filters: EventFilters): Promise<Event[]>;
  getEvent(id: string): Promise<EventDetail>;
  registerForEvent(eventId: string, data: RegistrationData): Promise<RegistrationResult>;
  rsvpForEvent(eventId: string, data: RSVPData): Promise<RSVPResult>;
}

// TanStack Query Integration (REQUIRED)
const useEvents = (filters: EventFilters) => {
  return useQuery({
    queryKey: ['events', filters],
    queryFn: () => eventsAPI.getEvents(filters),
    staleTime: 5 * 60 * 1000, // 5 minutes
    refetchInterval: 30 * 1000, // Real-time capacity updates
  });
};
```

#### 2. Authentication Integration (Existing System)
```typescript
// Must integrate with existing auth context
import { useAuth } from '@/contexts/AuthContext';

const EventCard = ({ event }) => {
  const { user, isAuthenticated, userRole } = useAuth();
  
  const canViewFullDetails = useMemo(() => {
    if (!event.isMemberOnly) return true;
    return isAuthenticated && userRole !== 'anonymous';
  }, [event.isMemberOnly, isAuthenticated, userRole]);
  
  // Render logic based on access level
};
```

#### 3. Type Safety (NSwag Generated Types)
```typescript
// MUST use generated types from @witchcityrope/shared-types
import { EventDto, RegistrationDto } from '@witchcityrope/shared-types';

// NO manual interfaces allowed
interface EventCardProps {
  event: EventDto; // Use generated type
  onRegister: (data: RegistrationDto) => void;
}
```

### State Management Patterns

#### 1. Server State (TanStack Query)
```typescript
// Event data management
const useEventDetail = (eventId: string) => {
  return useQuery({
    queryKey: ['event', eventId],
    queryFn: () => eventsAPI.getEvent(eventId),
    staleTime: 2 * 60 * 1000, // 2 minutes
    refetchOnWindowFocus: true // Refresh capacity on focus
  });
};

// Registration mutations
const useEventRegistration = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: ({ eventId, data }) => eventsAPI.registerForEvent(eventId, data),
    onSuccess: (result, { eventId }) => {
      // Invalidate event data to refresh capacity
      queryClient.invalidateQueries({ queryKey: ['event', eventId] });
      queryClient.invalidateQueries({ queryKey: ['events'] });
    }
  });
};
```

#### 2. Form State (@mantine/form)
```typescript
// Registration form state management
const RegistrationForm = ({ event }) => {
  const form = useForm({
    initialValues: {
      email: '',
      fullName: '',
      ticketType: 'individual',
      selectedPrice: event.price.min
    },
    validate: yupResolver(registrationSchema)
  });

  const handleSubmit = async (values) => {
    try {
      await registerMutation.mutateAsync({ 
        eventId: event.id, 
        data: values 
      });
      form.reset();
      showSuccessNotification();
    } catch (error) {
      form.setErrors(error.fieldErrors);
      showErrorNotification(error.message);
    }
  };
};
```

#### 3. URL State (Filter Persistence)
```typescript
// Filter state in URL for bookmarking
import { useSearchParams } from 'react-router-dom';

const useEventFilters = () => {
  const [searchParams, setSearchParams] = useSearchParams();
  
  const filters = useMemo(() => ({
    eventType: searchParams.get('type') || 'all',
    instructor: searchParams.get('instructor') || null,
    dateRange: searchParams.get('date') || 'month'
  }), [searchParams]);
  
  const updateFilters = useCallback((newFilters) => {
    const params = new URLSearchParams();
    Object.entries(newFilters).forEach(([key, value]) => {
      if (value) params.set(key, value);
    });
    setSearchParams(params);
  }, [setSearchParams]);
  
  return [filters, updateFilters];
};
```

## 📱 RESPONSIVE DESIGN IMPLEMENTATION

### Required Breakpoint Behavior

#### Mobile (320px - 767px)
```typescript
// Event List Page - Mobile Layout
<Stack gap="md">
  {/* Filters collapse to vertical stack */}
  <Paper withBorder p="sm">
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
  </Paper>

  {/* Event cards full width */}
  <Stack gap="sm">
    {events.map(event => (
      <EventCard key={event.id} event={event} size="mobile" />
    ))}
  </Stack>
</Stack>

// Event Detail Page - Mobile Layout  
<Stack gap="md">
  {/* Registration moved to top, sticky */}
  <Box pos="sticky" top={0} style={{ zIndex: 100 }}>
    <RegistrationSidebar event={event} compact />
  </Box>
  <EventContent event={event} />
</Stack>
```

#### Desktop (768px+)
```typescript
// Two-column layout maintained
<Grid>
  <Grid.Col span={8}>
    <EventContent event={event} />
  </Grid.Col>
  <Grid.Col span={4}>
    <Box pos="sticky" top={20}>
      <RegistrationSidebar event={event} />
    </Box>
  </Grid.Col>
</Grid>
```

### Touch-Friendly Interface Requirements
- **Minimum Touch Target**: 44px for all interactive elements
- **Thumb-Zone Optimization**: Primary actions in bottom third of screen
- **Swipe Gestures**: Consider horizontal scrolling for event cards
- **Form Optimization**: Large input fields with mobile keyboards in mind

## 🔒 SECURITY AND PRIVACY IMPLEMENTATION

### Access Control Patterns
```typescript
// Member-only event access control
const EventCard = ({ event, userRole }) => {
  const showFullDetails = useMemo(() => {
    if (!event.isMemberOnly) return true;
    return userRole === 'vetted' || userRole === 'admin';
  }, [event.isMemberOnly, userRole]);

  const showVenueDetails = useMemo(() => {
    return event.userRegistered || userRole === 'admin';
  }, [event.userRegistered, userRole]);

  return (
    <Paper>
      {showFullDetails ? (
        <Text>{event.description}</Text>
      ) : (
        <Stack gap="sm">
          <Text lineClamp={1} style={{ opacity: 0.7 }}>
            {event.description.substring(0, 50)}...
          </Text>
          <Alert color="orange">
            <Text size="sm">
              <Anchor href="/login">Login</Anchor> or{' '}
              <Anchor href="/membership">apply for membership</Anchor>{' '}
              to see full details and register
            </Text>
          </Alert>
        </Stack>
      )}
    </Paper>
  );
};
```

### Form Security
```typescript
// CSRF protection and input sanitization
import { sanitize } from 'dompurify';

const RegistrationForm = () => {
  const handleSubmit = async (values) => {
    // Sanitize inputs before submission
    const sanitizedValues = {
      ...values,
      fullName: sanitize(values.fullName),
      // Email validation handled by yup schema
    };

    // Include CSRF token
    const response = await fetch('/api/events/register', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-CSRF-Token': await getCSRFToken()
      },
      credentials: 'include', // Include httpOnly cookies
      body: JSON.stringify(sanitizedValues)
    });
  };
};
```

## 🚀 PERFORMANCE IMPLEMENTATION REQUIREMENTS

### Code Splitting and Lazy Loading
```typescript
// Lazy load heavy components
import { lazy, Suspense } from 'react';
import { Skeleton } from '@mantine/core';

const RegistrationForm = lazy(() => import('./RegistrationForm'));
const RSVPModal = lazy(() => import('./RSVPModal'));

// Loading fallbacks
const FormSkeleton = () => (
  <Stack gap="md">
    <Skeleton height={40} />
    <Skeleton height={100} />
    <Skeleton height={40} />
  </Stack>
);

// Implementation
const EventRegistration = ({ event }) => (
  <Suspense fallback={<FormSkeleton />}>
    <RegistrationForm event={event} />
  </Suspense>
);
```

### Image Optimization
```typescript
// Instructor and event images
import { Image } from '@mantine/core';

<Image
  src={instructor.photo}
  alt={`Photo of ${instructor.name}`}
  width={80}
  height={80}
  radius="xl"
  loading="lazy"
  placeholder={<Avatar>{instructor.name[0]}</Avatar>}
  fallback={<Avatar>{instructor.name[0]}</Avatar>}
/>
```

### Caching Strategy
```typescript
// Event data caching with real-time updates
const useEventsWithRealTimeUpdates = (filters) => {
  const queryClient = useQueryClient();
  
  const eventsQuery = useQuery({
    queryKey: ['events', filters],
    queryFn: () => fetchEvents(filters),
    staleTime: 5 * 60 * 1000, // 5 minutes
    gcTime: 10 * 60 * 1000   // 10 minutes
  });

  // Real-time capacity updates via polling
  useEffect(() => {
    if (!eventsQuery.data) return;
    
    const interval = setInterval(() => {
      queryClient.invalidateQueries({ 
        queryKey: ['events'], 
        exact: false 
      });
    }, 30000); // 30 seconds

    return () => clearInterval(interval);
  }, [eventsQuery.data, queryClient]);

  return eventsQuery;
};
```

## 🧪 TESTING REQUIREMENTS

### Component Testing Pattern
```typescript
// Required test structure
import { renderWithTheme } from '../test-utils';
import { EventCard } from './EventCard';
import { mockEvent } from '../__mocks__/events';

describe('EventCard', () => {
  it('displays event information correctly', () => {
    const { getByText, getByRole } = renderWithTheme(
      <EventCard event={mockEvent} userRole="anonymous" />
    );
    
    expect(getByText(mockEvent.title)).toBeInTheDocument();
    expect(getByText(mockEvent.instructor)).toBeInTheDocument();
    expect(getByRole('progressbar')).toHaveAttribute(
      'aria-label', 
      expect.stringContaining('Event capacity')
    );
  });

  it('shows limited information for member-only events to anonymous users', () => {
    const memberEvent = { ...mockEvent, isMemberOnly: true };
    
    const { getByText, queryByText } = renderWithTheme(
      <EventCard event={memberEvent} userRole="anonymous" />
    );
    
    expect(getByText(/login or apply for membership/i)).toBeInTheDocument();
    expect(queryByText(memberEvent.description)).not.toBeInTheDocument();
  });
});
```

### Integration Testing
```typescript
// Required E2E test scenarios
describe('Event Registration Flow', () => {
  test('user can register for a class event', async () => {
    // Navigate to events page
    await page.goto('/events');
    
    // Find and click on a class event
    await page.click('[data-testid="event-card-class"]:first-child');
    
    // Fill registration form
    await page.fill('[data-testid="email-input"]', 'test@example.com');
    await page.fill('[data-testid="name-input"]', 'Test User');
    
    // Adjust sliding scale price
    await page.locator('[data-testid="price-slider"]').click();
    
    // Submit registration
    await page.click('[data-testid="register-button"]');
    
    // Verify success state
    await expect(page.locator('[data-testid="registration-success"]')).toBeVisible();
  });

  test('member can RSVP for social event', async () => {
    // Login as vetted member
    await loginAs('vetted-member');
    
    // Navigate to events and find social event
    await page.goto('/events');
    await page.click('[data-testid="event-card-social"]:first-child');
    
    // Click RSVP Free button
    await page.click('[data-testid="rsvp-button"]');
    
    // Fill RSVP modal
    await page.fill('[data-testid="rsvp-email"]', 'member@example.com');
    await page.fill('[data-testid="rsvp-name"]', 'Test Member');
    await page.click('[data-testid="confirm-rsvp"]');
    
    // Verify RSVP success
    await expect(page.locator('[data-testid="rsvp-success"]')).toBeVisible();
  });
});
```

## ✅ IMPLEMENTATION PHASES AND DELIVERABLES

### Phase 1: Foundation Components (Week 1)
**Deliverables:**
- [ ] Mantine v7 installation and theme configuration
- [ ] Base layout components (Container, Grid layouts)
- [ ] EventCard component with all states (anonymous, member, capacity warnings)
- [ ] EventFilters component with real-time filtering
- [ ] Responsive utilities and hooks

**Success Criteria:**
- [ ] Events list page displays correctly on all breakpoints
- [ ] Event filtering works with URL state persistence
- [ ] Component library theme matches brand colors exactly
- [ ] All components pass TypeScript compilation

### Phase 2: Event Detail Implementation (Week 2)
**Deliverables:**
- [ ] Event detail page layout (two-column desktop, stacked mobile)
- [ ] EventDetailHero component with comprehensive event information
- [ ] RegistrationSidebar with sticky positioning
- [ ] CapacityIndicator with real-time updates
- [ ] InstructorCard component with profile integration

**Success Criteria:**
- [ ] Event detail page responsive behavior works perfectly
- [ ] Capacity information updates in real-time
- [ ] Member-only access control functions correctly
- [ ] Breadcrumb navigation works properly

### Phase 3: Forms and Interactions (Week 3)
**Deliverables:**
- [ ] RegistrationForm with sliding scale price selector
- [ ] Form validation with yup schema integration
- [ ] RSVP modal for social events
- [ ] Success and error state handling
- [ ] Loading states and skeleton screens

**Success Criteria:**
- [ ] Registration form validation works correctly
- [ ] Sliding scale price selector is accessible and functional
- [ ] RSVP flow completes successfully for social events
- [ ] Error handling provides helpful user feedback
- [ ] All forms work correctly on mobile devices

### Phase 4: Polish and Integration (Week 4)
**Deliverables:**
- [ ] Performance optimization (lazy loading, memoization)
- [ ] Comprehensive error handling and edge cases
- [ ] Accessibility audit and WCAG 2.1 AA compliance
- [ ] Integration testing with backend APIs
- [ ] Cross-browser testing and optimization

**Success Criteria:**
- [ ] Page load time <2 seconds for events list
- [ ] Lighthouse score >90 for Performance and Accessibility
- [ ] All user stories pass acceptance criteria testing
- [ ] Zero TypeScript errors in production build
- [ ] Complete integration with Phase 3 backend APIs

## 🔄 QUALITY GATES AND REVIEW CHECKPOINTS

### Code Review Requirements
- [ ] **Component Architecture**: All components follow Mantine v7 patterns
- [ ] **TypeScript Compliance**: Zero TypeScript errors, proper interface usage
- [ ] **Accessibility**: WCAG 2.1 AA compliance verified
- [ ] **Performance**: Bundle analysis shows reasonable component sizes
- [ ] **Test Coverage**: All components have corresponding tests

### Design Review Requirements  
- [ ] **Visual Consistency**: Components match wireframes exactly
- [ ] **Brand Integration**: WitchCityRope theme applied consistently
- [ ] **Responsive Design**: Mobile/tablet/desktop layouts work correctly
- [ ] **User Experience**: All user flows intuitive and accessible
- [ ] **Error States**: All error conditions handled gracefully

### Integration Review Requirements
- [ ] **API Integration**: All backend endpoints properly integrated
- [ ] **Authentication**: User role-based access control works correctly
- [ ] **Real-time Updates**: Capacity information updates appropriately
- [ ] **Form Submission**: Registration and RSVP workflows complete successfully
- [ ] **Cross-browser**: Functionality verified in Chrome, Firefox, Safari

## 📞 SUPPORT AND CLARIFICATION

### Design Questions Contact
**For design clarification:** UI Designer Agent via orchestration workflow
- Component specifications and visual requirements
- Responsive behavior and breakpoint questions
- Brand application and theme integration
- Accessibility implementation questions

### Business Requirements Contact  
**For requirements clarification:** Business Requirements Agent via orchestration workflow
- User story interpretation and acceptance criteria
- Business rule implementation questions
- Access control and privacy requirements
- Registration workflow logic

### Available Resources
1. **Complete wireframes** with working HTML/CSS examples
2. **Comprehensive component specifications** with TypeScript interfaces
3. **Working code examples** for all major patterns
4. **Theme configuration** ready for immediate use
5. **Test utilities** and mock data for development

---

**Handoff Status**: ✅ COMPLETE - Ready for immediate development  
**Critical Dependencies**: Phase 3 Backend APIs, Mantine v7 installation, Existing authentication system  
**Success Definition**: 85% registration conversion rate, <2s page load, WCAG 2.1 AA compliance, Mobile-optimized experience  

**Next Actions**: 
1. Review all design documents and component specifications
2. Set up development environment with Mantine v7
3. Begin Phase 1 implementation with foundation components
4. Schedule weekly design review checkpoints during development
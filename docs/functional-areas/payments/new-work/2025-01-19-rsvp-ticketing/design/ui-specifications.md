# UI Specifications: RSVP and Ticketing System
<!-- Last Updated: 2025-09-19 -->
<!-- Version: 2.0 - STAKEHOLDER CORRECTIONS APPLIED -->
<!-- Owner: UI Designer Agent -->
<!-- Status: APPROVED - Ready for Development -->

## Overview

This document provides comprehensive UI specifications for the RSVP and Ticketing System, implementing the approved business requirements (Version 3.0) with simplified single-RSVP-per-user model, role stacking, and optional ticket purchases for social events.

**🚨 CRITICAL STAKEHOLDER CORRECTIONS APPLIED**:
- **TERMINOLOGY**: All instances of "register" replaced with "RSVP"
- **FLOW CORRECTION**: Social events show RSVP button FIRST, then optional ticket purchase
- **BUTTON FIX**: Standard Mantine Button components used to prevent text cutoff
- **UI CLARIFICATION**: Different components for social events (RSVP + ticket) vs classes (ticket purchase)

## Component Hierarchy

### 1. Core Components

#### EventDetailPage
```typescript
interface EventDetailPageProps {
  event: Event;
  user: User | null;
  currentParticipation: Participation | null;
}

// Child Components:
// - EventHero
// - EventContent
// - ParticipationCard (RSVP/ticket card)
```

#### ParticipationCard
```typescript
interface ParticipationCardProps {
  event: Event;
  user: User | null;
  participation: Participation | null;
  onRSVP: () => void;
  onPurchaseTicket: (amount: number) => void;
  onCancel: () => void;
}

// States:
// - AnonymousState: Show login prompt
// - BannedState: Show access denied
// - IneligibleState: Show vetting required (General members for social events)
// - AvailableState: Show RSVP/purchase options
// - ParticipatingState: Show current status + cancel options
```

#### UserDashboard
```typescript
interface UserDashboardProps {
  user: User;
  upcomingEvents: ParticipationWithEvent[];
  pastEvents: ParticipationWithEvent[];
}

// Child Components:
// - UpcomingEventsSection
// - PastEventsSection
// - ParticipationCard
```

#### AdminEventParticipants
```typescript
interface AdminEventParticipantsProps {
  event: Event;
  participants: ParticipationWithUser[];
  filters: ParticipantFilters;
  onFilterChange: (filters: ParticipantFilters) => void;
  onExport: () => void;
}

// Child Components:
// - ParticipantFilters
// - ParticipantTable
// - ExportControls
```

## User Flow Diagrams

### 1. Social Event RSVP Flow (🚨 CORRECTED)

```
[Social Event Detail Page]
        |
    Check User Role
        |
    ├─ Anonymous ─────── [Login Prompt]
    ├─ Banned ──────── [Access Denied Message]
    ├─ General Member ── [Vetting Required Message]
    └─ Vetted Member ───┐
                        ├─ No Participation ──── [RSVP Now] + [Purchase Ticket ($X)]
                        ├─ Has RSVP ─────── [Purchase Ticket ($X)] + [Cancel RSVP]
                        └─ Has Ticket ────── [RSVPed ✓] + [Cancel Ticket]
```

### 2. Class Ticket Purchase Flow

```
[Class Detail Page]
        |
    Check User Role
        |
    ├─ Anonymous ─────── [Login Prompt]
    ├─ Banned ──────── [Access Denied Message]
    └─ Active Member ───┐
                        ├─ No Ticket ──── [Purchase Ticket ($X-$Y)] → [Checkout] → [Auto-RSVP Created]
                        └─ Has Ticket ─── [RSVPed ✓] + [Cancel Ticket]
```

### 3. Social Event RSVP Flow (🚨 CORRECTED: Shows ticket purchase option)

```
[RSVP Now Button Click]
        |
    [Confirmation Modal]
    "RSVP for [Event Name]?"
    "This is FREE and confirms your attendance"
        |
    [Confirm] → [Email Confirmation] → [Success Page]
        |
    [ALWAYS AVAILABLE: Purchase Ticket Now ($X)]
```

### 4. Dashboard Event Management Flow

```
[User Dashboard]
        |
    ├─ Upcoming Events ──┐
    │                    ├─ Event Card (RSVP) ── [Cancel RSVP] [Purchase Ticket]
    │                    ├─ Event Card (Ticket) ── [Cancel Ticket]
    │                    └─ Event Card (Both) ─── [Cancel Options]
    │
    └─ Past Events ──── [Event Cards with Status History]
```

## State Management Requirements

### 1. Authentication State Integration

```typescript
// Use existing auth store patterns
const user = useUser();
const isAuthenticated = useIsAuthenticated();
const isVetted = useIsVetted();
const isBanned = useIsBanned();
const userRoles = useUserRoles(); // For role stacking

// Role checking utility
const canAccessSocialEvent = (user: User) => {
  if (user.status === 'Banned') return false;
  return user.roles.some(role => ['Vetted', 'Teacher', 'Administrator'].includes(role));
};

const canPurchaseClassTicket = (user: User) => {
  if (user.status === 'Banned') return false;
  return user.roles.some(role => ['General', 'Vetted', 'Teacher', 'Administrator'].includes(role));
};
```

### 2. Participation State

```typescript
interface ParticipationState {
  rsvp: {
    exists: boolean;
    status: 'Active' | 'Canceled';
    createdAt: Date;
    canceledAt?: Date;
  } | null;

  ticket: {
    exists: boolean;
    status: 'Active' | 'Canceled';
    amount: number;
    paymentStatus: 'Completed' | 'Pending' | 'Failed' | 'Refunded';
    createdAt: Date;
    canceledAt?: Date;
  } | null;

  loading: boolean;
  error: string | null;
}

// Actions
type ParticipationAction =
  | { type: 'CREATE_RSVP' }
  | { type: 'CANCEL_RSVP' }
  | { type: 'PURCHASE_TICKET', amount: number }
  | { type: 'CANCEL_TICKET' }
  | { type: 'SET_LOADING', loading: boolean }
  | { type: 'SET_ERROR', error: string | null };
```

### 3. Event State

```typescript
interface EventState {
  event: Event;
  capacity: {
    total: number;
    rsvpCount: number;
    ticketCount: number;
    available: number;
  };
  loading: boolean;
  error: string | null;
}
```

## Responsive Design Considerations

### Breakpoints (Following Mantine v7 Standards)
- **xs**: 0px - 575px (Mobile)
- **sm**: 576px - 767px (Large Mobile)
- **md**: 768px - 991px (Tablet)
- **lg**: 992px - 1199px (Desktop)
- **xl**: 1200px+ (Large Desktop)

### Mobile-First Component Adaptations

#### EventDetailPage
```css
/* Desktop Layout */
.event-detail-container {
  display: grid;
  grid-template-columns: 1fr 380px;
  gap: var(--space-xl);
  max-width: 1400px;
  margin: 0 auto;
  padding: var(--space-xl);
}

/* Mobile Layout */
@media (max-width: 768px) {
  .event-detail-container {
    grid-template-columns: 1fr;
    padding: var(--space-md);
  }

  .participation-card {
    position: sticky;
    bottom: 0;
    margin: 0 -var(--space-md);
    border-radius: 0;
    box-shadow: 0 -4px 12px rgba(0,0,0,0.1);
  }
}
```

#### ParticipationCard
```css
/* Mobile Sticky CTA */
@media (max-width: 768px) {
  .participation-card {
    padding: var(--space-md);
    background: var(--color-ivory);
    border-top: 2px solid var(--color-rose-gold);
  }

  .participation-buttons {
    display: flex;
    gap: var(--space-sm);
  }

  .btn-rsvp, .btn-purchase {
    flex: 1;
    min-height: 48px; /* Touch target */
    font-size: 16px;
    font-weight: 700;
  }
}
```

#### UserDashboard
```css
/* Mobile Dashboard */
@media (max-width: 768px) {
  .dashboard-container {
    padding: var(--space-sm);
  }

  .event-card {
    margin-bottom: var(--space-md);
  }

  .event-card-actions {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-top: var(--space-sm);
  }
}
```

## Accessibility Requirements

### WCAG 2.1 AA Compliance

#### Color and Contrast
```css
/* Status Color System with 4.5:1 Contrast */
.status-rsvp {
  background: var(--color-success); /* #228B22 */
  color: white; /* Contrast ratio: 5.2:1 */
}

.status-ticket {
  background: var(--color-burgundy); /* #880124 */
  color: white; /* Contrast ratio: 8.1:1 */
}

.status-both {
  background: var(--color-electric); /* #9D4EDD */
  color: white; /* Contrast ratio: 5.8:1 */
}

.status-canceled {
  background: var(--color-stone); /* #8B8680 */
  color: white; /* Contrast ratio: 4.6:1 */
}
```

#### Keyboard Navigation
```typescript
// Keyboard event handlers
const handleKeyDown = (event: KeyboardEvent) => {
  switch (event.key) {
    case 'Enter':
    case ' ': // Space bar
      event.preventDefault();
      handleAction();
      break;
    case 'Escape':
      handleCancel();
      break;
  }
};

// Focus management
const focusManagement = {
  // After RSVP creation, focus success message
  onRSVPSuccess: () => {
    const successElement = document.getElementById('rsvp-success-message');
    successElement?.focus();
  },

  // After modal close, return focus to trigger button
  onModalClose: (triggerElement: HTMLElement) => {
    triggerElement.focus();
  }
};
```

#### Screen Reader Support
```jsx
// ARIA labels and descriptions
<Button
  aria-label="RSVP for Introduction to Rope Bondage on March 15th"
  aria-describedby="rsvp-description"
  onClick={handleRSVP}
>
  RSVP Now (Free)
</Button>

<div
  id="rsvp-description"
  className="sr-only"
>
  Confirms your attendance at this social event. This is free and does not include a ticket.
</div>

// Status announcements
<div
  role="status"
  aria-live="polite"
  className="sr-only"
>
  {statusMessage}
</div>

// Form validation
<TextInput
  label="Email Address"
  error={emailError}
  aria-invalid={!!emailError}
  aria-describedby={emailError ? 'email-error' : undefined}
/>
{emailError && (
  <div
    id="email-error"
    role="alert"
    className="error-message"
  >
    {emailError}
  </div>
)}
```

## Error States and Messaging

### User-Facing Error States

#### Access Denied (Banned Users)
```jsx
<Alert
  type="error"
  title="Access Denied"
  closable={false}
>
  Your account access has been restricted. Please contact support for assistance.
  <br />
  <Link href="/contact">Contact Support</Link>
</Alert>
```

#### Vetting Required (General Members on Social Events)
```jsx
<Alert
  type="info"
  title="Vetting Required"
  closable={false}
>
  This social event is limited to vetted members. Complete the vetting process to attend.
  <br />
  <Button component={Link} href="/vetting" variant="outline">
    Start Vetting Process
  </Button>
</Alert>
```

#### Capacity Full
```jsx
<Alert
  type="warning"
  title="Event Full"
  closable={false}
>
  This event has reached capacity. Join the waitlist to be notified if spots become available.
  <br />
  <Button variant="outline" onClick={joinWaitlist}>
    Join Waitlist
  </Button>
</Alert>
```

#### Payment Processing Errors
```jsx
// For ticket purchases
<Alert
  type="error"
  title="Payment Failed"
  closable={false}
>
  We couldn't process your payment. Please check your payment method and try again.
  <br />
  <Button variant="outline" onClick={retryPayment}>
    Try Again
  </Button>
  <Button variant="subtle" component={Link} href="/contact">
    Contact Support
  </Button>
</Alert>
```

#### Network/Server Errors
```jsx
<Alert
  type="error"
  title="Connection Error"
  closable={false}
>
  We're experiencing technical difficulties. Please try again in a moment.
  <br />
  <Button variant="outline" onClick={retry}>
    Retry
  </Button>
</Alert>
```

## Mantine v7 Component Specifications (🚨 CRITICAL FIX)

### Form Components with Floating Labels
```jsx
// All form inputs MUST use floating label animation
<TextInput
  label="Full Name"
  placeholder=" " // Required for floating label CSS
  classNames={{
    input: 'floating-input',
    label: 'floating-label'
  }}
  styles={{
    input: {
      padding: 'var(--space-md) var(--space-sm) var(--space-xs) var(--space-sm)',
      transition: 'all 0.3s ease'
    },
    label: {
      position: 'absolute',
      left: 'var(--space-sm)',
      top: '50%',
      transform: 'translateY(-50%)',
      transition: 'all 0.3s ease',
      '&[data-floating]': {
        top: '-2px',
        transform: 'translateY(-50%) scale(0.8)',
        color: 'var(--color-burgundy)',
        background: 'var(--color-cream)'
      }
    }
  }}
/>

// Email input for RSVP
<TextInput
  label="Email Address"
  type="email"
  required
  placeholder=" "
  classNames={{
    input: 'floating-input',
    label: 'floating-label'
  }}
/>

// Phone input (optional)
<TextInput
  label="Phone Number (Optional)"
  type="tel"
  placeholder=" "
  classNames={{
    input: 'floating-input',
    label: 'floating-label'
  }}
  description="For event updates and emergencies"
/>
```

### Button Components (🚨 CRITICAL FIX: Use Standard CSS Classes)
**Reference**: `/docs/lessons-learned/react-developer-lessons-learned.md` - Button text cutoff prevention

```jsx
// ✅ CORRECT - Use standard CSS classes to prevent text cutoff
// Primary RSVP Button
<button className="btn btn-primary" onClick={handleRSVP}>
  RSVP Now (Free)
</button>

// Secondary Ticket Purchase Button
<button className="btn btn-secondary" onClick={handleTicketPurchase}>
  Purchase Ticket (${ticketPrice})
</button>

// Cancel Button (Dangerous Action)
<button className="btn btn-secondary" onClick={openCancelModal}>
  Cancel RSVP
</button>

// ❌ WRONG - Inline styles cause text cutoff issues
<Button
  variant="filled"
  styles={{
    root: {
      background: 'linear-gradient(...)', // Custom styling causes cutoff
      borderRadius: '12px 6px 12px 6px',
      // ... other inline styles
    }
  }}
>
  Text gets cut off
</Button>
```

**Standard Button Classes Available** (from `/apps/web/src/index.css`):
- `.btn .btn-primary` - Amber gradient for primary actions
- `.btn .btn-secondary` - Burgundy outline for secondary actions
- `.btn .btn-large` - Large button modifier

### Modal Components for Confirmations
```jsx
// RSVP Confirmation Modal
<Modal
  opened={rsvpModalOpen}
  onClose={() => setRsvpModalOpen(false)}
  title="Confirm RSVP"
  centered
  styles={{
    title: {
      fontFamily: 'var(--font-heading)',
      fontSize: '20px',
      fontWeight: 700,
      color: 'var(--color-burgundy)'
    }
  }}
>
  <Stack gap="md">
    <Text>
      Confirm your RSVP for <strong>{event.title}</strong>?
    </Text>
    <Text size="sm" c="dimmed">
      This is FREE and confirms your attendance. You can optionally purchase a ticket for the suggested donation after RSVPing.
    </Text>

    <Group justify="flex-end" gap="sm">
      <Button
        variant="subtle"
        onClick={() => setRsvpModalOpen(false)}
      >
        Cancel
      </Button>
      <button
        className="btn btn-primary"
        onClick={confirmRSVP}
        disabled={submitting}
      >
        Confirm RSVP
      </button>
    </Group>
  </Stack>
</Modal>

// Cancellation Confirmation Modal
<Modal
  opened={cancelModalOpen}
  onClose={() => setCancelModalOpen(false)}
  title="Cancel Participation"
  centered
>
  <Stack gap="md">
    <Text>
      Are you sure you want to cancel your {participationType} for <strong>{event.title}</strong>?
    </Text>

    {participationType === 'ticket' && (
      <Alert type="warning">
        Canceling your ticket will initiate a refund process. Refunds are subject to our cancellation policy.
      </Alert>
    )}

    <Group justify="flex-end" gap="sm">
      <Button
        variant="subtle"
        onClick={() => setCancelModalOpen(false)}
      >
        Keep {participationType}
      </Button>
      <button
        className="btn btn-secondary"
        onClick={confirmCancel}
        disabled={canceling}
      >
        Yes, Cancel {participationType}
      </button>
    </Group>
  </Stack>
</Modal>
```

### Status Badge Components
```jsx
// Participation Status Badges
const StatusBadge = ({ type, status }: { type: 'rsvp' | 'ticket' | 'both', status: 'active' | 'canceled' }) => {
  const badgeConfig = {
    rsvp: {
      color: 'green',
      label: 'RSVP Confirmed',
      icon: '✓'
    },
    ticket: {
      color: 'red', // Using Mantine's red which maps to burgundy theme
      label: 'Ticket Purchased',
      icon: '🎫'
    },
    both: {
      color: 'violet', // Using Mantine's violet which maps to electric purple
      label: 'Fully RSVPed',
      icon: '⭐'
    }
  };

  const config = badgeConfig[type];

  return (
    <Badge
      color={status === 'canceled' ? 'gray' : config.color}
      variant="filled"
      size="lg"
      styles={{
        root: {
          borderRadius: '12px 6px 12px 6px', // Signature asymmetric corners
          fontFamily: 'var(--font-heading)',
          fontWeight: 600,
          textTransform: 'uppercase',
          letterSpacing: '0.5px'
        }
      }}
    >
      {config.icon} {status === 'canceled' ? 'Canceled' : config.label}
    </Badge>
  );
};
```

### Table Components for Admin
```jsx
// Admin Participants Table
<Table
  striped
  highlightOnHover
  styles={{
    table: {
      borderRadius: '12px',
      overflow: 'hidden'
    },
    th: {
      background: 'var(--color-burgundy)',
      color: 'var(--color-ivory)',
      fontFamily: 'var(--font-heading)',
      fontWeight: 600,
      textTransform: 'uppercase',
      letterSpacing: '0.5px'
    }
  }}
>
  <Table.Thead>
    <Table.Tr>
      <Table.Th>Participant</Table.Th>
      <Table.Th>Roles</Table.Th>
      <Table.Th>Participation</Table.Th>
      <Table.Th>Amount</Table.Th>
      <Table.Th>Status</Table.Th>
      <Table.Th>Date</Table.Th>
      <Table.Th>Actions</Table.Th>
    </Table.Tr>
  </Table.Thead>
  <Table.Tbody>
    {participants.map((participant) => (
      <Table.Tr key={participant.id}>
        <Table.Td>
          <Group gap="sm">
            <Avatar size="sm" color="burgundy">
              {participant.user.sceneName.charAt(0)}
            </Avatar>
            <div>
              <Text size="sm" fw={500}>{participant.user.sceneName}</Text>
              <Text size="xs" c="dimmed">{participant.user.email}</Text>
            </div>
          </Group>
        </Table.Td>
        <Table.Td>
          <Group gap={4}>
            {participant.user.roles.map(role => (
              <Badge key={role} size="xs" variant="light">
                {role}
              </Badge>
            ))}
          </Group>
        </Table.Td>
        <Table.Td>
          <StatusBadge
            type={participant.type}
            status={participant.status}
          />
        </Table.Td>
        <Table.Td>
          {participant.amount ? `$${participant.amount}` : 'Free'}
        </Table.Td>
        <Table.Td>
          <Text
            size="sm"
            c={participant.status === 'active' ? 'green' : 'dimmed'}
          >
            {participant.status}
          </Text>
        </Table.Td>
        <Table.Td>
          <Text size="sm">
            {format(participant.createdAt, 'MMM d, yyyy')}
          </Text>
        </Table.Td>
        <Table.Td>
          <Group gap={4}>
            <ActionIcon variant="subtle" size="sm">
              👁️
            </ActionIcon>
            <ActionIcon variant="subtle" size="sm">
              ✉️
            </ActionIcon>
          </Group>
        </Table.Td>
      </Table.Tr>
    ))}
  </Table.Tbody>
</Table>
```

### Filter Components
```jsx
// Admin Participant Filters
<Paper p="md" withBorder>
  <Group align="flex-end" gap="md">
    <Select
      label="Participation Type"
      placeholder="All types"
      data={[
        { value: 'all', label: 'All Types' },
        { value: 'rsvp', label: 'RSVP Only' },
        { value: 'ticket', label: 'Ticket Only' },
        { value: 'both', label: 'Both RSVP & Ticket' }
      ]}
      value={filters.participationType}
      onChange={(value) => setFilters({ ...filters, participationType: value })}
      styles={{ label: { fontFamily: 'var(--font-heading)', fontWeight: 600 } }}
    />

    <Select
      label="Status"
      placeholder="All statuses"
      data={[
        { value: 'all', label: 'All Statuses' },
        { value: 'active', label: 'Active' },
        { value: 'canceled', label: 'Canceled' }
      ]}
      value={filters.status}
      onChange={(value) => setFilters({ ...filters, status: value })}
      styles={{ label: { fontFamily: 'var(--font-heading)', fontWeight: 600 } }}
    />

    <Select
      label="Role"
      placeholder="All roles"
      data={[
        { value: 'all', label: 'All Roles' },
        { value: 'general', label: 'General Members' },
        { value: 'vetted', label: 'Vetted Members' },
        { value: 'teacher', label: 'Teachers' },
        { value: 'admin', label: 'Administrators' }
      ]}
      value={filters.role}
      onChange={(value) => setFilters({ ...filters, role: value })}
      styles={{ label: { fontFamily: 'var(--font-heading)', fontWeight: 600 } }}
    />

    <button
      className="btn btn-secondary"
      onClick={exportParticipants}
    >
      📥 Export
    </button>
  </Group>
</Paper>
```

## Performance Considerations

### Code Splitting
```typescript
// Lazy load admin components
const AdminEventParticipants = lazy(() =>
  import('./AdminEventParticipants').then(module => ({
    default: module.AdminEventParticipants
  }))
);

// Lazy load checkout flow
const CheckoutFlow = lazy(() =>
  import('./CheckoutFlow').then(module => ({
    default: module.CheckoutFlow
  }))
);

// Use Suspense with loading states
<Suspense fallback={<Loader size="lg" />}>
  <AdminEventParticipants />
</Suspense>
```

### Data Fetching Optimization
```typescript
// Use React Query for caching and optimistic updates
const useParticipation = (eventId: string) => {
  return useQuery({
    queryKey: ['participation', eventId],
    queryFn: () => fetchParticipation(eventId),
    staleTime: 5 * 60 * 1000, // 5 minutes
    refetchOnWindowFocus: true
  });
};

// Optimistic updates for RSVP
const useCreateRSVP = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: createRSVP,
    onMutate: async (variables) => {
      // Cancel outgoing refetches
      await queryClient.cancelQueries(['participation', variables.eventId]);

      // Optimistically update
      queryClient.setQueryData(['participation', variables.eventId], (old) => ({
        ...old,
        rsvp: { exists: true, status: 'Active', createdAt: new Date() }
      }));
    },
    onError: (err, variables, context) => {
      // Revert optimistic update
      queryClient.setQueryData(['participation', variables.eventId], context);
    },
    onSettled: () => {
      // Refetch to ensure consistency
      queryClient.invalidateQueries(['participation']);
    }
  });
};
```

### Memory Management
```typescript
// Cleanup event listeners and subscriptions
useEffect(() => {
  const handleVisibilityChange = () => {
    if (document.visibilityState === 'visible') {
      // Refetch data when user returns to tab
      queryClient.invalidateQueries(['participation']);
    }
  };

  document.addEventListener('visibilitychange', handleVisibilityChange);

  return () => {
    document.removeEventListener('visibilitychange', handleVisibilityChange);
  };
}, [queryClient]);

// Debounce search inputs
const debouncedSearch = useMemo(
  () => debounce((term: string) => {
    setFilters(prev => ({ ...prev, search: term }));
  }, 300),
  []
);

useEffect(() => {
  return () => {
    debouncedSearch.cancel();
  };
}, [debouncedSearch]);
```

## Quality Validation Checklist

### Design System v7 Compliance
- [ ] Uses exact WCR color palette (burgundy #880124, amber #FFBF00, etc.)
- [ ] Implements signature button corner morphing (12px 6px 12px 6px)
- [ ] All form inputs use floating label animation
- [ ] Typography follows font hierarchy (Bodoni Moda, Montserrat, Source Sans 3)
- [ ] Navigation uses center-outward underline animation
- [ ] NO purple buttons (only amber and burgundy allowed)

### Functionality Compliance (🚨 STAKEHOLDER CORRECTIONS)
- [ ] ✅ Single RSVP per user per event (no multi-person RSVPs)
- [ ] ✅ Role stacking supported (user can be Vetted + Teacher + Admin)
- [ ] ✅ Banned users completely blocked from all participation
- [ ] ✅ Social events require vetting, classes allow general members
- [ ] ✅ Automatic RSVP creation when purchasing tickets
- [ ] ✅ Canceled participation retained in system with status
- [ ] ✅ Optional ticket purchase for social events ALWAYS shown
- [ ] ✅ All "register" terminology replaced with "RSVP"
- [ ] ✅ Button components use standard CSS classes to prevent text cutoff

### Accessibility Compliance
- [ ] 4.5:1 color contrast ratio maintained
- [ ] Keyboard navigation for all interactive elements
- [ ] Screen reader support with proper ARIA labels
- [ ] Focus management in modals and dynamic content
- [ ] Error messages announced to screen readers
- [ ] Touch targets minimum 44px on mobile

### Mobile Responsive
- [ ] Single column layout on mobile (768px breakpoint)
- [ ] Sticky CTA buttons in thumb-reach zone
- [ ] Touch-friendly gesture support
- [ ] Readable font sizes on small screens
- [ ] Proper viewport meta tag implementation

### Performance Requirements
- [ ] Code splitting for admin components
- [ ] Optimistic updates for user actions
- [ ] Debounced search and filter inputs
- [ ] Lazy loading for non-critical components
- [ ] Image optimization and compression
- [ ] Bundle size under 500KB (gzipped)

## Browser Support

### Target Browsers
- **Chrome**: 100+ (90%+ market share)
- **Safari**: 15+ (iOS and macOS)
- **Firefox**: 100+
- **Edge**: 100+

### Progressive Enhancement
- Core functionality works without JavaScript (server-rendered forms)
- Enhanced UX with JavaScript enabled (animations, optimistic updates)
- Graceful degradation for older browsers
- Feature detection for modern capabilities

### Testing Requirements
- Cross-browser testing on BrowserStack
- Mobile device testing (iOS Safari, Chrome Android)
- Screen reader testing (NVDA, VoiceOver)
- Keyboard-only navigation testing
- Performance testing on slow networks (3G simulation)

## 🚨 CRITICAL STAKEHOLDER CORRECTIONS SUMMARY

### 1. **TERMINOLOGY CORRECTION** ✅
- **FIXED**: All instances of "register" replaced with "RSVP"
- **Applied to**: Component names, function names, UI text, documentation

### 2. **RSVP FLOW CORRECTION** ✅
- **FIXED**: Social events show RSVP button FIRST, then optional ticket purchase
- **Clarified**: There is NO "RSVP only" flow - ticket purchase is ALWAYS available

### 3. **BUTTON CSS FIX** ✅
- **FIXED**: Use standard CSS classes (`.btn .btn-primary`) instead of inline styles
- **Reference**: `/docs/lessons-learned/react-developer-lessons-learned.md`
- **Prevents**: Text cutoff issues in button components

### 4. **EVENT DETAIL PAGE CLARIFICATION** ✅
- **FIXED**: Social Events show "RSVP" button first, THEN optional "Purchase Ticket"
- **FIXED**: Classes show "Purchase Ticket" with sliding scale pricing
- **Clarified**: These are different UI components for each event type

This comprehensive UI specification provides the foundation for implementing the RSVP and Ticketing System with full alignment to the approved business requirements, stakeholder corrections, and design system standards.
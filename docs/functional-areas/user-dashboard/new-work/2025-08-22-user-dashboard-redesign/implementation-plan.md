# Implementation Plan: User Dashboard Redesign (CONSOLIDATED)
<!-- Last Updated: 2025-10-08 -->
<!-- Version: 1.0 - Initial Implementation Plan -->
<!-- Owner: React Developer Agent -->
<!-- Status: Ready for Development -->

## Overview

This implementation plan breaks down the consolidated User Dashboard redesign into deployable vertical slices. The dashboard consists of **two primary pages** (Dashboard Landing + Events Page) that integrate with the existing EventDetailPage. All work uses the established React + TypeScript + Mantine v7 tech stack with Design System v7.

### What We're Building

**Two-Page Dashboard Structure:**
1. **Dashboard Landing** (`/dashboard`) - Welcome message, upcoming events preview (3-5 cards), quick shortcuts
2. **Events Page** (`/dashboard/events`) - Full event history with tab-based filtering (Upcoming/Past/Cancelled)
3. **Integration** - Both pages use existing EventDetailPage for event actions
4. **Additional Pages** - Profile, Security, and Membership settings pages (simplified)

**Design Philosophy:**
- Simple functionality over complex design
- Edge-to-edge layout with left navigation only
- No right sidebar or floating elements
- Mobile-responsive with proven Mantine patterns

---

## Technical Foundation

### Existing Infrastructure (Already Available)

✅ **React + TypeScript + Vite**
- Functional components with hooks pattern
- Strict TypeScript configuration
- Vite dev server with HMR
- Location: `/apps/web/`

✅ **Mantine v7 UI Framework** (ADR-004)
- Component library: Card, NavLink, Tabs, Button, etc.
- Theme customization with Design System v7 colors
- Responsive utilities and hooks
- Location: Installed in `/apps/web/package.json`

✅ **State Management**
- TanStack Query v5 for server state
- Zustand for global state (auth)
- React Context for theme/preferences
- Pattern: `/apps/web/src/hooks/useAuth.tsx` (reference)

✅ **React Router v7**
- Route definitions in `/apps/web/src/App.tsx`
- Protected route patterns established
- Navigation with type safety

✅ **Design System v7**
- CSS variables in `/apps/web/src/styles/design-system.css`
- Colors: burgundy, rose-gold, amber, plum, cream, ivory
- Typography: Montserrat (headings), Source Sans 3 (body)
- Animations: Corner morphing, center-outward underlines

✅ **API Integration**
- Axios HTTP client
- Base URL configuration
- httpOnly cookie authentication
- Location: `/apps/web/src/lib/api/`

✅ **Existing EventDetailPage**
- Fully functional event detail view
- Action buttons: RSVP, Cancel, Purchase Ticket
- Location: `/apps/web/src/pages/events/EventDetailPage.tsx`
- Integration pattern proven

### What Needs Creation

❌ **Dashboard Layout Component**
- DashboardLayout with left nav
- Sticky header integration
- Responsive sidebar collapse
- Page content area

❌ **Dashboard Landing Page**
- Welcome section
- Upcoming events preview (max 5)
- Quick actions shortcuts
- Empty states

❌ **Events Page**
- Tab navigation (Upcoming/Past/Cancelled)
- Event cards list
- Tab-based filtering
- Empty states per tab

❌ **API Endpoints & DTOs**
- `/api/dashboard/upcoming-events` - Dashboard preview
- `/api/dashboard/events` - Full event history
- DTOs: DashboardPreviewDto, UserEventHistoryDto, EventSummaryDto
- Backend C# implementation

❌ **Supporting Pages**
- Profile settings page (simplified)
- Security settings page (simplified)
- Membership status page (simplified)

---

## Vertical Slice Breakdown

### Vertical Slice Architecture Principles

Each slice is:
- **Independently deployable** - Can go to production on its own
- **Fully testable** - Unit tests + E2E tests included
- **Demonstrable** - Shows visible progress to stakeholders
- **Incremental** - Builds on previous slices

### Slice Dependencies

```
Slice 1: Foundation (DashboardLayout + Routes)
    ↓
Slice 2: Dashboard Landing (Welcome + Events Preview)
    ↓
Slice 3: Events Page (Tabs + Full History)
    ↓
Slice 4: EventDetailPage Integration
    ↓
Slice 5: Additional Pages (Profile/Security/Membership)
    ↓
Slice 6: Mobile Responsive Polish
    ↓
Slice 7: Testing, Accessibility, Performance
```

---

## Slice 1: Dashboard Foundation - Layout & Navigation

**Duration**: 2-3 days (16-24 hours)

**Goal**: Create the DashboardLayout component with left navigation and basic routing.

### Components to Create

**1. DashboardLayout Component**
```typescript
// /apps/web/src/layouts/DashboardLayout.tsx
interface DashboardLayoutProps {
  children: React.ReactNode;
}

// Features:
// - Left navigation (280px wide)
// - 5 nav items: Dashboard, Events, Profile, Security, Membership
// - Active state highlighting with gradient background
// - Sticky header integration
// - Main content area (edge-to-edge)
// - Mobile hamburger menu
```

**2. DashboardNav Component**
```typescript
// /apps/web/src/components/dashboard/DashboardNav.tsx
interface NavItem {
  icon: string;
  label: string;
  href: string;
}

// Features:
// - Mantine NavLink components
// - Active state from React Router
// - Hover effects (translateX + background)
// - Emoji icons for simplicity
// - Design System v7 colors
```

**3. Route Configuration**
```typescript
// Update /apps/web/src/App.tsx
{
  path: '/dashboard',
  element: <ProtectedRoute><DashboardLayout /></ProtectedRoute>,
  children: [
    { index: true, element: <DashboardLandingPage /> },
    { path: 'events', element: <EventsPage /> },
    { path: 'profile', element: <ProfilePage /> },
    { path: 'security', element: <SecurityPage /> },
    { path: 'membership', element: <MembershipPage /> }
  ]
}
```

### API Endpoints Needed

**None** - This slice is UI-only

### Test Coverage

**Unit Tests:**
- DashboardLayout renders children correctly
- DashboardNav highlights active route
- Nav items navigate correctly
- Mobile menu toggles properly

**E2E Tests:**
```typescript
// /apps/web/tests/playwright/dashboard-layout.spec.ts
test('dashboard layout displays navigation', async ({ page }) => {
  await page.goto('/dashboard');
  await expect(page.locator('[data-testid="dashboard-nav"]')).toBeVisible();
  await expect(page.locator('text=Dashboard')).toBeVisible();
  await expect(page.locator('text=Events')).toBeVisible();
  await expect(page.locator('text=Profile')).toBeVisible();
});

test('clicking nav items navigates correctly', async ({ page }) => {
  await page.goto('/dashboard');
  await page.click('text=Events');
  await expect(page.url()).toContain('/dashboard/events');
});
```

### Acceptance Criteria

- [ ] DashboardLayout component renders with left nav
- [ ] 5 navigation items display with correct labels
- [ ] Active route is highlighted with gradient background
- [ ] Routes navigate correctly (`/dashboard`, `/dashboard/events`, etc.)
- [ ] Responsive: Mobile menu collapses to hamburger
- [ ] Design System v7 colors applied (burgundy, plum gradient)
- [ ] Unit tests pass (90%+ coverage)
- [ ] E2E tests pass
- [ ] No TypeScript errors
- [ ] No console errors

---

## Slice 2: Dashboard Landing Page - Welcome & Events Preview

**Duration**: 3-4 days (24-32 hours)

**Goal**: Implement the Dashboard Landing page with welcome message, upcoming events preview (max 5 cards), and quick shortcuts.

### Components to Create

**1. DashboardLandingPage Component**
```typescript
// /apps/web/src/pages/dashboard/DashboardLandingPage.tsx
export const DashboardLandingPage: React.FC = () => {
  const { data: preview, isLoading } = useDashboardPreview();
  const { data: currentUser } = useCurrentUser();

  return (
    <Stack>
      <WelcomeSection sceneName={currentUser?.sceneName} />
      <UpcomingEventsPreview events={preview?.upcomingEvents} />
      <QuickActionsSection />
    </Stack>
  );
};
```

**2. WelcomeSection Component**
```typescript
// /apps/web/src/components/dashboard/WelcomeSection.tsx
interface WelcomeSectionProps {
  sceneName: string;
}

// Features:
// - "Welcome back, [Scene Name]" title
// - Montserrat 800, 28px
// - Full-width underline (gradient: burgundy → rose-gold)
// - Edge-to-edge layout
```

**3. UpcomingEventsPreview Component**
```typescript
// /apps/web/src/components/dashboard/UpcomingEventsPreview.tsx
interface UpcomingEventsPreviewProps {
  events: EventSummaryDto[];
  hasMore: boolean;
}

// Features:
// - Section title: "My Upcoming Events"
// - Display max 5 event cards
// - "View All Events" link if hasMore
// - Empty state: "No upcoming events scheduled"
// - EventCard component (reusable)
```

**4. EventCard Component**
```typescript
// /apps/web/src/components/dashboard/EventCard.tsx
interface EventCardProps {
  id: string;
  title: string;
  date: Date;
  time: string;
  location: string;
  participationStatus: string;
}

// Features:
// - Mantine Card with border
// - Date badge (gradient background)
// - Event title, time, location
// - Participation status badge (color-coded)
// - "View Details" button
// - Hover effect: translateY(-4px) + shadow
```

**5. QuickActionsSection Component**
```typescript
// /apps/web/src/components/dashboard/QuickActionsSection.tsx
// Features:
// - Section title: "Quick Actions"
// - 3 shortcut buttons (Edit Profile, Security Settings, Membership Status)
// - Mantine Button with outline variant
// - Corner morphing animation on hover
// - Emoji icons
```

### API Endpoints Needed

**Backend (C#):**
```csharp
// /apps/api/Controllers/DashboardController.cs
[HttpGet("preview")]
public async Task<ActionResult<DashboardPreviewDto>> GetDashboardPreview()
{
  // Returns upcoming events preview for current user
  // Max 5 events
  // Ordered by startDate ascending
  // Includes hasMoreEvents flag
}
```

**DTOs:**
```csharp
// /apps/api/Models/Dashboard/DashboardPreviewDto.cs
public class DashboardPreviewDto
{
  public Guid UserId { get; set; }
  public List<EventSummaryDto> UpcomingEvents { get; set; } = new();
  public bool HasMoreEvents { get; set; }
  public int TotalUpcomingCount { get; set; }
}

public class EventSummaryDto
{
  public Guid EventId { get; set; }
  public string EventName { get; set; } = string.Empty;
  public DateTime EventDate { get; set; }
  public string EventTime { get; set; } = string.Empty;
  public string Location { get; set; } = string.Empty;
  public string ParticipationStatus { get; set; } = string.Empty;
}
```

**Frontend (React Query Hook):**
```typescript
// /apps/web/src/hooks/useDashboard.ts
export const useDashboardPreview = () => {
  return useQuery({
    queryKey: ['dashboard', 'preview'],
    queryFn: () => apiClient.get<DashboardPreviewDto>('/api/dashboard/preview'),
    staleTime: 5 * 60 * 1000, // 5 minutes
    refetchOnWindowFocus: false
  });
};
```

### Test Coverage

**Unit Tests:**
- WelcomeSection displays correct scene name
- UpcomingEventsPreview renders events correctly
- "View All Events" link appears when hasMoreEvents is true
- Empty state displays when no events
- EventCard displays all information
- QuickActionsSection has 3 buttons with correct links
- Loading state renders skeleton

**E2E Tests:**
```typescript
// /apps/web/tests/playwright/dashboard-landing.spec.ts
test('dashboard landing shows welcome message', async ({ page }) => {
  await page.goto('/dashboard');
  await expect(page.locator('text=Welcome back,')).toBeVisible();
});

test('shows upcoming events preview with max 5 cards', async ({ page }) => {
  await page.goto('/dashboard');
  const eventCards = page.locator('[data-testid="event-card"]');
  const count = await eventCards.count();
  expect(count).toBeLessThanOrEqual(5);
});

test('shows "View All Events" link when more than 5 events', async ({ page }) => {
  // Setup: User with 10 upcoming events
  await page.goto('/dashboard');
  await expect(page.locator('text=View All Events')).toBeVisible();
});

test('quick actions navigate correctly', async ({ page }) => {
  await page.goto('/dashboard');
  await page.click('text=Edit Profile');
  await expect(page.url()).toContain('/dashboard/profile');
});
```

### Acceptance Criteria

- [ ] Welcome message displays "Welcome back, [Scene Name]"
- [ ] Upcoming events section shows max 5 events
- [ ] Event cards display: title, date, time, location, status
- [ ] "View Details" button on each card works
- [ ] "View All Events" link appears when >5 events
- [ ] Empty state shows when no upcoming events
- [ ] Quick Actions section has 3 shortcuts
- [ ] All shortcuts navigate to correct pages
- [ ] Loading skeleton displays while fetching data
- [ ] Design System v7 styling applied
- [ ] Edge-to-edge layout (no floating boxes)
- [ ] API endpoint returns correct data
- [ ] NSwag types generated for DTOs
- [ ] Unit tests pass (90%+ coverage)
- [ ] E2E tests pass
- [ ] Performance: Page loads in <1.5 seconds

---

## Slice 3: Events Page - Tab Navigation & Full History

**Duration**: 3-4 days (24-32 hours)

**Goal**: Implement the Events Page with tab-based filtering (Upcoming/Past/Cancelled) and full event history display.

### Components to Create

**1. EventsPage Component**
```typescript
// /apps/web/src/pages/dashboard/EventsPage.tsx
export const EventsPage: React.FC = () => {
  const [activeTab, setActiveTab] = useState<'upcoming' | 'past' | 'cancelled'>('upcoming');
  const { data: eventsData, isLoading } = useUserEvents();

  const filteredEvents = useMemo(() => {
    if (!eventsData) return [];

    switch (activeTab) {
      case 'upcoming': return eventsData.upcomingEvents;
      case 'past': return eventsData.pastEvents;
      case 'cancelled': return eventsData.cancelledEvents;
      default: return [];
    }
  }, [activeTab, eventsData]);

  return (
    <Stack>
      <Title>My Events</Title>
      <EventsTabNavigation
        activeTab={activeTab}
        onTabChange={setActiveTab}
        upcomingCount={eventsData?.upcomingCount}
        pastCount={eventsData?.pastCount}
        cancelledCount={eventsData?.cancelledCount}
      />
      <EventsList events={filteredEvents} emptyMessage={getEmptyMessage(activeTab)} />
    </Stack>
  );
};
```

**2. EventsTabNavigation Component**
```typescript
// /apps/web/src/components/dashboard/EventsTabNavigation.tsx
interface EventsTabNavigationProps {
  activeTab: 'upcoming' | 'past' | 'cancelled';
  onTabChange: (tab: 'upcoming' | 'past' | 'cancelled') => void;
  upcomingCount: number;
  pastCount: number;
  cancelledCount: number;
}

// Features:
// - Mantine Tabs component
// - 3 tabs: Upcoming (default), Past, Cancelled
// - Show event counts in tab labels
// - Active tab: 3px burgundy bottom border
// - Client-side tab switching (no page reload)
// - Montserrat font, uppercase, letter-spacing
```

**3. EventsList Component**
```typescript
// /apps/web/src/components/dashboard/EventsList.tsx
interface EventsListProps {
  events: EventParticipationDto[];
  emptyMessage: string;
}

// Features:
// - Displays list of EventCard components
// - Empty state with custom message per tab
// - Vertical stack layout (edge-to-edge)
// - Reuses EventCard from Slice 2
```

### API Endpoints Needed

**Backend (C#):**
```csharp
// /apps/api/Controllers/DashboardController.cs
[HttpGet("events")]
public async Task<ActionResult<UserEventHistoryDto>> GetUserEvents()
{
  // Returns ALL user event participations
  // Grouped by status: upcoming, past, cancelled
  // Sorted: upcoming asc, past desc, cancelled desc
}
```

**DTOs:**
```csharp
// /apps/api/Models/Dashboard/UserEventHistoryDto.cs
public class UserEventHistoryDto
{
  public Guid UserId { get; set; }
  public List<EventParticipationDto> UpcomingEvents { get; set; } = new();
  public List<EventParticipationDto> PastEvents { get; set; } = new();
  public List<EventParticipationDto> CancelledEvents { get; set; } = new();
  public int UpcomingCount { get; set; }
  public int PastCount { get; set; }
  public int CancelledCount { get; set; }
}

public class EventParticipationDto
{
  public Guid EventId { get; set; }
  public string EventName { get; set; } = string.Empty;
  public DateTime EventDate { get; set; }
  public string EventTime { get; set; } = string.Empty;
  public string Location { get; set; } = string.Empty;
  public string ParticipationStatus { get; set; } = string.Empty;
  public string EventStatus { get; set; } = string.Empty; // "Upcoming", "Past", "Cancelled"
  public DateTime RegistrationDate { get; set; }
}
```

**Frontend (React Query Hook):**
```typescript
// /apps/web/src/hooks/useDashboard.ts
export const useUserEvents = () => {
  return useQuery({
    queryKey: ['dashboard', 'events'],
    queryFn: () => apiClient.get<UserEventHistoryDto>('/api/dashboard/events'),
    staleTime: 5 * 60 * 1000,
    refetchOnWindowFocus: false
  });
};
```

### Test Coverage

**Unit Tests:**
- EventsPage renders with correct tab structure
- EventsTabNavigation displays 3 tabs with counts
- Active tab is highlighted correctly
- Tab switching updates filtered events
- EventsList renders event cards
- Empty states show correct messages per tab
- Loading skeleton displays correctly

**E2E Tests:**
```typescript
// /apps/web/tests/playwright/events-page.spec.ts
test('events page displays with upcoming tab active', async ({ page }) => {
  await page.goto('/dashboard/events');
  await expect(page.locator('[data-value="upcoming"][data-active]')).toBeVisible();
});

test('tab switching works without page reload', async ({ page }) => {
  await page.goto('/dashboard/events');

  // Click Past tab
  await page.click('text=Past');
  await expect(page.locator('[data-value="past"][data-active]')).toBeVisible();

  // Verify no page reload (check for specific content)
  await expect(page.locator('text=My Events')).toBeVisible();
});

test('shows correct empty states for each tab', async ({ page }) => {
  await page.goto('/dashboard/events');

  // If no upcoming events
  await expect(page.locator('text=No upcoming events')).toBeVisible();

  // Switch to Past
  await page.click('text=Past');
  await expect(page.locator('text=No past events')).toBeVisible();
});

test('displays event counts in tabs', async ({ page }) => {
  await page.goto('/dashboard/events');
  await expect(page.locator('text=/Upcoming \\(\\d+\\)/')).toBeVisible();
});
```

### Acceptance Criteria

- [ ] Events Page displays "My Events" title
- [ ] 3 tabs visible: Upcoming, Past, Cancelled
- [ ] Upcoming tab is active by default
- [ ] Event counts display in tab labels
- [ ] Tab switching works without page reload
- [ ] Filtered events display correctly per tab
- [ ] Upcoming events sorted by date ascending
- [ ] Past events sorted by date descending
- [ ] Empty states show correct messages
- [ ] Event cards match Dashboard Landing design
- [ ] "View Details" button on each card works
- [ ] Edge-to-edge layout maintained
- [ ] API endpoint returns all event history
- [ ] Client-side filtering performs well (no lag)
- [ ] NSwag types generated for DTOs
- [ ] Unit tests pass (90%+ coverage)
- [ ] E2E tests pass
- [ ] Performance: Page loads in <2 seconds

---

## Slice 4: EventDetailPage Integration

**Duration**: 1-2 days (8-16 hours)

**Goal**: Integrate "View Details" buttons from Dashboard and Events pages to navigate to the existing EventDetailPage, ensuring context preservation and proper navigation flow.

### Components to Update

**1. EventCard Component Enhancement**
```typescript
// /apps/web/src/components/dashboard/EventCard.tsx
// Update to use React Router Link for navigation
import { useNavigate } from 'react-router-dom';

const handleViewDetails = () => {
  navigate(`/events/${id}`, {
    state: { from: window.location.pathname }
  });
};
```

**2. EventDetailPage Back Navigation**
```typescript
// /apps/web/src/pages/events/EventDetailPage.tsx
// Add back navigation that preserves context
import { useLocation, useNavigate } from 'react-router-dom';

const location = useLocation();
const navigate = useNavigate();

const handleBack = () => {
  const from = location.state?.from || '/dashboard';
  navigate(from);
};
```

**3. Breadcrumb Update**
```typescript
// Update EventDetailPage breadcrumbs to show origin
<Breadcrumbs>
  <Anchor href="/">Home</Anchor>
  {from === '/dashboard/events' ? (
    <Anchor href="/dashboard/events">My Events</Anchor>
  ) : (
    <Anchor href="/dashboard">Dashboard</Anchor>
  )}
  <Text>{event?.title}</Text>
</Breadcrumbs>
```

### API Endpoints Needed

**None** - EventDetailPage already has API integration

### Test Coverage

**E2E Tests:**
```typescript
// /apps/web/tests/playwright/event-detail-integration.spec.ts
test('view details from dashboard landing navigates correctly', async ({ page }) => {
  await page.goto('/dashboard');
  const firstEventCard = page.locator('[data-testid="event-card"]').first();
  await firstEventCard.locator('text=View Details').click();

  await expect(page.url()).toMatch(/\/events\/[a-f0-9-]+/);
  await expect(page.locator('text=About This Event')).toBeVisible();
});

test('browser back from event detail returns to dashboard', async ({ page }) => {
  await page.goto('/dashboard');
  await page.click('text=View Details');

  await page.goBack();
  await expect(page.url()).toContain('/dashboard');
  await expect(page.locator('text=Welcome back,')).toBeVisible();
});

test('view details from events page preserves tab context', async ({ page }) => {
  await page.goto('/dashboard/events');
  await page.click('text=Past'); // Switch to Past tab
  await page.click('text=View Details'); // Click on past event

  await page.goBack();
  await expect(page.url()).toContain('/dashboard/events');
  // Verify Past tab is still active
  await expect(page.locator('[data-value="past"][data-active]')).toBeVisible();
});

test('event actions on detail page update dashboard', async ({ page }) => {
  await page.goto('/dashboard');
  await page.click('text=View Details');

  // Cancel RSVP on detail page
  await page.click('text=Cancel RSVP');
  await page.fill('[placeholder="Reason for cancellation"]', 'Schedule conflict');
  await page.click('text=Confirm Cancellation');

  await page.goBack();
  // Event should be removed from upcoming events on dashboard
  await expect(page.locator('[data-testid="event-card"]').filter({ hasText: /EventName/ })).not.toBeVisible();
});
```

### Acceptance Criteria

- [ ] "View Details" buttons navigate to EventDetailPage
- [ ] Event ID passed correctly in URL
- [ ] EventDetailPage displays full event information
- [ ] Browser back button returns to origin (dashboard or events page)
- [ ] Origin context preserved (active tab on events page)
- [ ] Breadcrumbs reflect navigation origin
- [ ] Event actions (RSVP, Cancel) trigger data refresh
- [ ] Dashboard/Events data updates after actions
- [ ] No duplicate event action logic created
- [ ] Navigation state management works correctly
- [ ] E2E tests pass
- [ ] No console errors or warnings

---

## Slice 5: Additional Pages - Profile, Security, Membership

**Duration**: 3-4 days (24-32 hours)

**Goal**: Create simplified versions of Profile, Security, and Membership settings pages to complete the 5-section navigation structure.

### Components to Create

**1. ProfilePage Component**
```typescript
// /apps/web/src/pages/dashboard/ProfilePage.tsx
// Features:
// - Page title: "Profile Settings"
// - User profile form (scene name, email, bio)
// - React Hook Form + Zod validation
// - Save button with API integration
// - Success/error notifications
```

**2. SecurityPage Component**
```typescript
// /apps/web/src/pages/dashboard/SecurityPage.tsx
// Features:
// - Page title: "Security Settings"
// - Change password form
// - Two-factor authentication toggle (future)
// - Session management section
// - Logout button
```

**3. MembershipPage Component**
```typescript
// /apps/web/src/pages/dashboard/MembershipPage.tsx
// Features:
// - Page title: "Membership Status"
// - Membership type display (Guest/Member/Vetted/Admin)
// - Vetting status (if applicable)
// - Membership expiration date
// - Renewal/upgrade options (future)
```

### API Endpoints Needed

**Backend (C#):**
```csharp
// /apps/api/Controllers/UserController.cs
[HttpGet("profile")]
public async Task<ActionResult<UserProfileDto>> GetProfile()

[HttpPut("profile")]
public async Task<ActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)

[HttpPost("change-password")]
public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordDto dto)

[HttpGet("membership")]
public async Task<ActionResult<MembershipStatusDto>> GetMembershipStatus()
```

**DTOs:**
```csharp
// /apps/api/Models/User/UserProfileDto.cs
public class UserProfileDto
{
  public Guid UserId { get; set; }
  public string SceneName { get; set; } = string.Empty;
  public string Email { get; set; } = string.Empty;
  public string? Bio { get; set; }
  public string? ProfileImageUrl { get; set; }
}

public class UpdateProfileDto
{
  public string SceneName { get; set; } = string.Empty;
  public string? Bio { get; set; }
}

public class ChangePasswordDto
{
  public string CurrentPassword { get; set; } = string.Empty;
  public string NewPassword { get; set; } = string.Empty;
}

public class MembershipStatusDto
{
  public Guid UserId { get; set; }
  public string MembershipType { get; set; } = string.Empty;
  public string VettingStatus { get; set; } = string.Empty;
  public DateTime? MemberSince { get; set; }
  public DateTime? ExpirationDate { get; set; }
}
```

### Test Coverage

**Unit Tests:**
- ProfilePage form validation works correctly
- SecurityPage password change validation
- MembershipPage displays status correctly
- API hooks fetch/update data properly

**E2E Tests:**
```typescript
// /apps/web/tests/playwright/dashboard-settings.spec.ts
test('profile page updates user information', async ({ page }) => {
  await page.goto('/dashboard/profile');
  await page.fill('[name="sceneName"]', 'Updated Name');
  await page.click('text=Save Changes');
  await expect(page.locator('text=Profile updated successfully')).toBeVisible();
});

test('security page changes password', async ({ page }) => {
  await page.goto('/dashboard/security');
  await page.fill('[name="currentPassword"]', 'OldPassword123!');
  await page.fill('[name="newPassword"]', 'NewPassword123!');
  await page.click('text=Change Password');
  await expect(page.locator('text=Password changed successfully')).toBeVisible();
});

test('membership page displays status', async ({ page }) => {
  await page.goto('/dashboard/membership');
  await expect(page.locator('text=Membership Type:')).toBeVisible();
  await expect(page.locator('text=Vetted Member')).toBeVisible();
});
```

### Acceptance Criteria

- [ ] Profile page displays user information correctly
- [ ] Profile form has validation (scene name required, bio max length)
- [ ] Profile updates save to API
- [ ] Security page has password change form
- [ ] Password validation enforces requirements
- [ ] Membership page shows user's membership type
- [ ] Vetting status displays if applicable
- [ ] All pages use Design System v7 styling
- [ ] Edge-to-edge layout consistent with other pages
- [ ] Left navigation highlights active page
- [ ] Success/error notifications display
- [ ] API endpoints return correct data
- [ ] NSwag types generated for all DTOs
- [ ] Unit tests pass (90%+ coverage)
- [ ] E2E tests pass

---

## Slice 6: Mobile Responsive Implementation

**Duration**: 2-3 days (16-24 hours)

**Goal**: Ensure all dashboard pages work perfectly on mobile devices with responsive layout transformations.

### Components to Update

**1. DashboardLayout Mobile Adaptations**
```typescript
// Add mobile menu toggle
// Collapse left nav to hamburger on <768px
// Content area becomes full-width
// Proper touch targets (44px min)
```

**2. EventCard Mobile Optimizations**
```typescript
// Single column layout on mobile
// Event info stacks vertically
// Larger touch targets for buttons
// Readable font sizes
```

**3. EventsTabNavigation Mobile**
```typescript
// Tabs scroll horizontally if needed
// Tab labels remain readable
// Active tab clearly visible
// Touch-friendly tab switching
```

### Responsive Design Requirements

**Breakpoints:**
- Mobile: max-width: 768px
- Tablet: 769px - 1024px
- Desktop: min-width: 1025px

**Mobile Transformations:**
```css
@media (max-width: 768px) {
  .dashboard-layout {
    grid-template-columns: 1fr; /* Single column */
  }

  .left-nav {
    position: fixed;
    left: -280px; /* Hidden by default */
    transition: left 0.3s ease;
  }

  .left-nav.open {
    left: 0;
  }

  .event-card {
    padding: var(--space-md); /* Smaller padding */
  }

  .quick-actions {
    flex-direction: column; /* Vertical stack */
    gap: var(--space-sm);
  }
}
```

### Test Coverage

**E2E Tests (Mobile Viewports):**
```typescript
// /apps/web/tests/playwright/dashboard-mobile.spec.ts
test.use({ viewport: { width: 375, height: 667 } }); // iPhone SE

test('mobile menu toggles correctly', async ({ page }) => {
  await page.goto('/dashboard');
  await page.click('[data-testid="mobile-menu-toggle"]');
  await expect(page.locator('[data-testid="dashboard-nav"]')).toBeVisible();
});

test('event cards stack vertically on mobile', async ({ page }) => {
  await page.goto('/dashboard');
  const cards = page.locator('[data-testid="event-card"]');
  const firstCard = cards.first();
  const secondCard = cards.nth(1);

  const firstBox = await firstCard.boundingBox();
  const secondBox = await secondCard.boundingBox();

  // Second card should be below first (vertical stacking)
  expect(secondBox!.y).toBeGreaterThan(firstBox!.y + firstBox!.height);
});

test('tabs are scrollable on mobile', async ({ page }) => {
  await page.goto('/dashboard/events');
  const tabsList = page.locator('[role="tablist"]');
  const isScrollable = await tabsList.evaluate(el => el.scrollWidth > el.clientWidth);
  expect(isScrollable).toBe(true);
});
```

### Acceptance Criteria

- [ ] Mobile menu toggles smoothly (hamburger icon)
- [ ] Left nav slides in/out on mobile
- [ ] Event cards stack vertically on mobile
- [ ] Quick actions stack vertically on mobile
- [ ] Touch targets minimum 44px × 44px
- [ ] Tabs scroll horizontally if needed
- [ ] All text remains readable (min 16px body)
- [ ] Forms are usable on mobile
- [ ] No horizontal scroll on any page
- [ ] Images/logos scale appropriately
- [ ] Performance maintained on mobile devices
- [ ] E2E tests pass on mobile viewports (iPhone, Android)
- [ ] Manual testing on real devices completed

---

## Slice 7: Testing, Accessibility, Performance

**Duration**: 2-3 days (16-24 hours)

**Goal**: Comprehensive testing, accessibility compliance (WCAG 2.1 AA), and performance optimization.

### Testing Tasks

**1. Unit Test Coverage Goal: 90%+**
- All components have unit tests
- All hooks have unit tests
- Edge cases covered (empty states, errors, loading)
- Mocking strategies for API calls

**2. E2E Test Coverage**
- Complete user journeys tested
- Cross-browser testing (Chrome, Firefox, Safari)
- Mobile viewport testing
- Error scenario testing

**3. Integration Tests**
- Dashboard → Events → EventDetail flow
- Event actions update dashboard state
- Navigation context preservation
- API error handling

### Accessibility Tasks

**1. WCAG 2.1 AA Compliance**
```typescript
// Add ARIA labels
<nav aria-label="Dashboard navigation">
<main role="main" aria-label="User dashboard">
<article aria-labelledby="event-title-1">

// Tab navigation
<Tabs aria-label="Event filtering tabs">
  <Tabs.Tab value="upcoming" aria-controls="upcoming-panel">

// Skip links
<a href="#main-content" className="skip-link">Skip to main content</a>
```

**2. Keyboard Navigation**
- All interactive elements keyboard accessible
- Focus states visible (3px outline)
- Tab order logical
- Escape key closes modals/menus
- Enter/Space activate buttons

**3. Screen Reader Support**
- Semantic HTML throughout
- ARIA roles for complex components
- Alternative text for icons
- Status messages announced

**4. Color Contrast**
- Primary text: 7:1 ratio (charcoal on cream)
- Secondary text: 4.5:1 ratio (stone on ivory)
- Interactive elements: 4.5:1 minimum
- Status indicators use color + text/icon

**5. Reduced Motion Support**
```css
@media (prefers-reduced-motion: reduce) {
  *, *::before, *::after {
    animation-duration: 0.01ms !important;
    transition-duration: 0.01ms !important;
  }
}
```

### Performance Tasks

**1. Performance Targets**
- Dashboard Landing: <1.5 seconds load time
- Events Page: <2 seconds load time
- Time to Interactive (TTI): <3 seconds
- First Contentful Paint (FCP): <1.2 seconds

**2. Optimization Strategies**
```typescript
// Code splitting for larger components
const EventDetailPage = lazy(() => import('./pages/events/EventDetailPage'));

// Memoization for expensive computations
const filteredEvents = useMemo(() =>
  filterEventsByTab(events, activeTab),
  [events, activeTab]
);

// React.memo for pure components
export const EventCard = memo<EventCardProps>(({ ... }) => { ... });

// TanStack Query caching
queryClient.setQueryDefaults(['dashboard', 'preview'], {
  staleTime: 5 * 60 * 1000,
  cacheTime: 10 * 60 * 1000
});
```

**3. Bundle Size Optimization**
- Tree-shaking unused code
- Lazy loading routes
- Image optimization
- CSS purging

**4. Lighthouse Audits**
- Performance score: 90+
- Accessibility score: 100
- Best Practices score: 90+
- SEO score: 90+ (for public pages)

### Test Coverage

**Accessibility Tests:**
```typescript
// /apps/web/tests/playwright/accessibility.spec.ts
import { injectAxe, checkA11y } from 'axe-playwright';

test('dashboard landing has no accessibility violations', async ({ page }) => {
  await page.goto('/dashboard');
  await injectAxe(page);
  await checkA11y(page);
});

test('keyboard navigation works', async ({ page }) => {
  await page.goto('/dashboard');

  // Tab through navigation
  await page.keyboard.press('Tab');
  await expect(page.locator(':focus')).toHaveText('Dashboard');

  await page.keyboard.press('Tab');
  await expect(page.locator(':focus')).toHaveText('Events');
});
```

**Performance Tests:**
```typescript
// /apps/web/tests/playwright/performance.spec.ts
test('dashboard loads within performance budget', async ({ page }) => {
  const startTime = Date.now();
  await page.goto('/dashboard');
  await page.waitForLoadState('networkidle');
  const loadTime = Date.now() - startTime;

  expect(loadTime).toBeLessThan(1500); // <1.5 seconds
});
```

### Acceptance Criteria

- [ ] Unit test coverage ≥90%
- [ ] E2E tests pass on Chrome, Firefox, Safari
- [ ] Mobile E2E tests pass (iOS, Android viewports)
- [ ] WCAG 2.1 AA compliance verified (axe-core)
- [ ] Keyboard navigation functional throughout
- [ ] Screen reader testing completed (NVDA/JAWS)
- [ ] Color contrast ratios verified (4.5:1 minimum)
- [ ] Focus states visible on all interactive elements
- [ ] Reduced motion support implemented
- [ ] Dashboard load time <1.5 seconds
- [ ] Events page load time <2 seconds
- [ ] Lighthouse Performance score ≥90
- [ ] Lighthouse Accessibility score 100
- [ ] Bundle size optimized (<200KB gzipped)
- [ ] No console errors or warnings
- [ ] Cross-browser testing completed

---

## API Requirements Summary

### Endpoints to Create

| Endpoint | Method | Purpose | DTO Response |
|----------|--------|---------|--------------|
| `/api/dashboard/preview` | GET | Dashboard landing preview (5 events max) | DashboardPreviewDto |
| `/api/dashboard/events` | GET | Full event history with status grouping | UserEventHistoryDto |
| `/api/user/profile` | GET | User profile information | UserProfileDto |
| `/api/user/profile` | PUT | Update user profile | - |
| `/api/user/change-password` | POST | Change user password | - |
| `/api/user/membership` | GET | Membership status information | MembershipStatusDto |

### DTOs to Create

**Dashboard DTOs:**
```csharp
// /apps/api/Models/Dashboard/DashboardPreviewDto.cs
public class DashboardPreviewDto
{
  public Guid UserId { get; set; }
  public List<EventSummaryDto> UpcomingEvents { get; set; }
  public bool HasMoreEvents { get; set; }
  public int TotalUpcomingCount { get; set; }
}

// /apps/api/Models/Dashboard/EventSummaryDto.cs
public class EventSummaryDto
{
  public Guid EventId { get; set; }
  public string EventName { get; set; }
  public DateTime EventDate { get; set; }
  public string EventTime { get; set; }
  public string Location { get; set; }
  public string ParticipationStatus { get; set; }
}

// /apps/api/Models/Dashboard/UserEventHistoryDto.cs
public class UserEventHistoryDto
{
  public Guid UserId { get; set; }
  public List<EventParticipationDto> UpcomingEvents { get; set; }
  public List<EventParticipationDto> PastEvents { get; set; }
  public List<EventParticipationDto> CancelledEvents { get; set; }
  public int UpcomingCount { get; set; }
  public int PastCount { get; set; }
  public int CancelledCount { get; set; }
}

// /apps/api/Models/Dashboard/EventParticipationDto.cs
public class EventParticipationDto
{
  public Guid EventId { get; set; }
  public string EventName { get; set; }
  public DateTime EventDate { get; set; }
  public string EventTime { get; set; }
  public string Location { get; set; }
  public string ParticipationStatus { get; set; }
  public string EventStatus { get; set; } // "Upcoming", "Past", "Cancelled"
  public DateTime RegistrationDate { get; set; }
}
```

**User DTOs:**
```csharp
// /apps/api/Models/User/UserProfileDto.cs
public class UserProfileDto
{
  public Guid UserId { get; set; }
  public string SceneName { get; set; }
  public string Email { get; set; }
  public string? Bio { get; set; }
  public string? ProfileImageUrl { get; set; }
}

// /apps/api/Models/User/MembershipStatusDto.cs
public class MembershipStatusDto
{
  public Guid UserId { get; set; }
  public string MembershipType { get; set; }
  public string VettingStatus { get; set; }
  public DateTime? MemberSince { get; set; }
  public DateTime? ExpirationDate { get; set; }
}
```

### NSwag Type Generation

After creating DTOs, regenerate TypeScript types:

```bash
# From /apps/web/ directory
npm run generate:types

# This runs NSwag to generate TypeScript interfaces from C# DTOs
# Generated types go to /apps/web/src/lib/api/types/
```

**Critical**: Always use NSwag-generated types. Never create manual TypeScript interfaces for API data.

---

## Testing Strategy

### Test Pyramid

```
         E2E Tests (10%)
        ----------------
      Integration Tests (20%)
    ------------------------
   Unit Tests (70%)
  --------------------------
```

### Unit Testing (React Testing Library + Vitest)

**Example:**
```typescript
// /apps/web/src/components/dashboard/EventCard.test.tsx
import { render, screen } from '@testing-library/react';
import { EventCard } from './EventCard';

describe('EventCard', () => {
  it('renders event information correctly', () => {
    const event = {
      id: '123',
      title: 'Rope Basics',
      date: new Date('2025-10-15'),
      time: '7:00 PM',
      location: 'WitchCity Studio',
      participationStatus: 'Registered'
    };

    render(<EventCard {...event} />);

    expect(screen.getByText('Rope Basics')).toBeInTheDocument();
    expect(screen.getByText('7:00 PM')).toBeInTheDocument();
    expect(screen.getByText('WitchCity Studio')).toBeInTheDocument();
    expect(screen.getByText('View Details')).toBeInTheDocument();
  });
});
```

### E2E Testing (Playwright)

**Example:**
```typescript
// /apps/web/tests/playwright/dashboard-flow.spec.ts
import { test, expect } from '@playwright/test';

test.describe('Dashboard User Flow', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/auth/login');
    await page.fill('[name="email"]', 'vetted@witchcityrope.com');
    await page.fill('[name="password"]', 'Test123!');
    await page.click('button[type="submit"]');
    await page.waitForURL('/dashboard');
  });

  test('complete dashboard navigation flow', async ({ page }) => {
    // Dashboard landing
    await expect(page.locator('text=Welcome back,')).toBeVisible();

    // Navigate to Events
    await page.click('text=Events');
    await expect(page.url()).toContain('/dashboard/events');

    // Switch to Past tab
    await page.click('text=Past');
    await expect(page.locator('[data-value="past"][data-active]')).toBeVisible();

    // View event details
    await page.click('text=View Details');
    await expect(page.locator('text=About This Event')).toBeVisible();

    // Back to Events page
    await page.goBack();
    await expect(page.url()).toContain('/dashboard/events');
  });
});
```

---

## Dependencies & Risks

### External Dependencies

| Dependency | Status | Risk | Mitigation |
|------------|--------|------|------------|
| Mantine v7 | ✅ Installed | Low | Stable library, well-documented |
| TanStack Query v5 | ✅ Installed | Low | Proven in authentication feature |
| React Router v7 | ✅ Installed | Low | Industry standard |
| NSwag | ✅ Configured | Low | Already generating auth types |
| EventDetailPage | ✅ Exists | Low | Integration tested |

### Technical Risks

**1. API Performance with Large Event Histories**
- **Risk**: Slow queries if user has 100+ events
- **Mitigation**: Add pagination or virtual scrolling if needed
- **Monitoring**: Track query performance in Slice 3

**2. Mobile Performance on Older Devices**
- **Risk**: Animations lag on low-end Android
- **Mitigation**: Reduce motion support, test on real devices
- **Monitoring**: Lighthouse audits in Slice 6

**3. Browser Compatibility**
- **Risk**: CSS Grid/Flexbox issues in older browsers
- **Mitigation**: Use Mantine responsive utilities, test Safari
- **Monitoring**: Cross-browser E2E tests in Slice 7

**4. State Management Complexity**
- **Risk**: Tab state not preserved correctly
- **Mitigation**: Use React Router state, thorough testing
- **Monitoring**: E2E tests for navigation flows

### Integration Risks

**1. EventDetailPage State Updates**
- **Risk**: Dashboard doesn't refresh after event actions
- **Mitigation**: TanStack Query cache invalidation
- **Testing**: Integration tests in Slice 4

**2. Authentication Context**
- **Risk**: User data not available in dashboard
- **Mitigation**: Reuse existing auth context
- **Testing**: Protected route tests

---

## Success Criteria

### Functionality Checklist

**Dashboard Landing:**
- [ ] Welcome message displays user's scene name
- [ ] Upcoming events preview shows max 5 events
- [ ] "View All Events" link appears when >5 events
- [ ] Quick actions navigate to correct pages
- [ ] Empty state displays when no events
- [ ] Loading states work correctly

**Events Page:**
- [ ] Three tabs: Upcoming, Past, Cancelled
- [ ] Event counts display in tabs
- [ ] Tab filtering works without page reload
- [ ] Events sorted correctly per tab
- [ ] Empty states show per tab
- [ ] All event history accessible

**Navigation:**
- [ ] 5-section left nav works on all pages
- [ ] Active page highlighted correctly
- [ ] Mobile menu toggles smoothly
- [ ] Breadcrumbs reflect navigation context

**Integration:**
- [ ] "View Details" navigates to EventDetailPage
- [ ] Browser back preserves context
- [ ] Event actions update dashboard data
- [ ] No duplicate functionality

**Additional Pages:**
- [ ] Profile page updates user info
- [ ] Security page changes password
- [ ] Membership page shows status
- [ ] All pages styled consistently

**Mobile:**
- [ ] All pages work on mobile viewports
- [ ] Touch targets minimum 44px
- [ ] No horizontal scroll
- [ ] Performance maintained

### Performance Targets

| Metric | Target | Measurement |
|--------|--------|-------------|
| Dashboard Load Time | <1.5s | Playwright performance test |
| Events Page Load Time | <2s | Playwright performance test |
| First Contentful Paint | <1.2s | Lighthouse audit |
| Time to Interactive | <3s | Lighthouse audit |
| Lighthouse Performance | ≥90 | Lighthouse audit |
| Bundle Size (gzipped) | <200KB | Vite build analysis |

### Design System v7 Compliance

- [ ] All colors from Design System v7 palette
- [ ] Typography: Montserrat (headings), Source Sans 3 (body)
- [ ] Button corner morphing animation implemented
- [ ] Navigation underline (center outward) working
- [ ] Edge-to-edge layout (no floating boxes)
- [ ] Hover effects consistent throughout
- [ ] Responsive design follows Mantine patterns

### Accessibility Compliance (WCAG 2.1 AA)

- [ ] Color contrast ratios ≥4.5:1
- [ ] All interactive elements keyboard accessible
- [ ] Focus states visible (3px outline)
- [ ] ARIA labels for screen readers
- [ ] Semantic HTML throughout
- [ ] Reduced motion support
- [ ] Axe-core accessibility scan passes (0 violations)

---

## Implementation Timeline

### Week-by-Week Breakdown

**Week 1: Foundation & Dashboard Landing**
- Days 1-2: Slice 1 (Layout & Navigation)
- Days 3-5: Slice 2 (Dashboard Landing)

**Week 2: Events Page & Integration**
- Days 1-3: Slice 3 (Events Page)
- Days 4-5: Slice 4 (EventDetailPage Integration)

**Week 3: Additional Pages & Mobile**
- Days 1-3: Slice 5 (Profile/Security/Membership)
- Days 4-5: Slice 6 (Mobile Responsive)

**Week 4: Testing & Polish**
- Days 1-3: Slice 7 (Testing, Accessibility, Performance)
- Days 4-5: Bug fixes, documentation, final review

**Total Duration**: 4 weeks (20 working days)

### Critical Path

```
Slice 1 (Layout)
  → Slice 2 (Dashboard Landing)
    → Slice 3 (Events Page)
      → Slice 4 (Integration)
        → Slice 5 (Additional Pages)
          → Slice 6 (Mobile)
            → Slice 7 (Testing)
```

### Parallel Work Opportunities

- **Slice 2 & API Development**: Backend team can work on dashboard DTOs while frontend builds components
- **Slice 5 Pages**: Profile, Security, Membership can be developed in parallel
- **Testing**: Unit tests written during each slice, E2E tests consolidated in Slice 7

---

## Maintenance & Future Enhancements

### Post-Launch Maintenance

**Monthly Reviews:**
- Performance monitoring (Lighthouse scores)
- Accessibility audits (axe-core scans)
- User feedback analysis
- API performance metrics

**Quarterly Updates:**
- Design System v7 updates
- Mantine v7 version updates
- Security patches
- Browser compatibility checks

### Future Enhancement Opportunities

**Phase 2 (Post-Launch):**
- Event calendar view on dashboard
- Notification preferences management
- Profile image upload
- Event reminders/calendar sync
- Social sharing features
- Event ratings/reviews

**Phase 3 (Advanced Features):**
- Dashboard customization (widget placement)
- Event recommendations
- Activity timeline
- Membership renewal automation
- Advanced analytics for users

---

## File Registry Updates

All files created/modified during implementation must be logged in `/docs/architecture/file-registry.md`:

| Date | File Path | Action | Purpose | Session/Task | Status |
|------|-----------|--------|---------|--------------|--------|
| 2025-10-08 | /apps/web/src/layouts/DashboardLayout.tsx | CREATED | Dashboard layout component | Slice 1 | ACTIVE |
| 2025-10-08 | /apps/web/src/components/dashboard/DashboardNav.tsx | CREATED | Dashboard navigation | Slice 1 | ACTIVE |
| 2025-10-08 | /apps/web/src/pages/dashboard/DashboardLandingPage.tsx | CREATED | Dashboard landing page | Slice 2 | ACTIVE |
| 2025-10-08 | /apps/web/src/components/dashboard/EventCard.tsx | CREATED | Event card component | Slice 2 | ACTIVE |
| 2025-10-08 | /apps/api/Models/Dashboard/DashboardPreviewDto.cs | CREATED | Dashboard API DTO | Slice 2 | ACTIVE |

---

## Conclusion

This implementation plan provides a comprehensive roadmap for building the consolidated User Dashboard redesign. By breaking the work into 7 vertical slices, we ensure:

1. **Incremental Progress**: Each slice delivers visible value
2. **Early Testing**: Issues caught and fixed quickly
3. **Stakeholder Demos**: Regular progress demonstrations
4. **Risk Mitigation**: Problems identified before full implementation
5. **Quality Assurance**: Testing integrated throughout

**Next Steps:**
1. Review this plan with stakeholders
2. Confirm API endpoint specifications with backend team
3. Set up development environment (already complete)
4. Begin Slice 1: Dashboard Foundation
5. Schedule weekly progress reviews

**Key Success Factors:**
- Follow vertical slice discipline (don't skip ahead)
- Maintain Design System v7 compliance throughout
- Use NSwag-generated types exclusively
- Write tests during development, not after
- Get feedback after each slice completion

---

**Document Version**: 1.0
**Created**: 2025-10-08
**Last Updated**: 2025-10-08
**Status**: Ready for Development
**Approved By**: Pending stakeholder review

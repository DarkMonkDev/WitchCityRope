# UI Design Specifications: User Dashboard v7 (CONSOLIDATED)
<!-- Last Updated: 2025-10-09 -->
<!-- Version: 2.0 - CONSOLIDATION UPDATE -->
<!-- Owner: UI Designer Agent -->
<!-- Status: Ready for Implementation -->

## 🚨 CONSOLIDATION UPDATE 🚨

**Major simplification based on October 2025 stakeholder feedback:**
- **REMOVED**: Right-side menu/status area entirely
- **TWO PRIMARY PAGES**: Dashboard Landing (`/dashboard`) + Events Page (`/dashboard/events`)
- **Dashboard Landing**: Welcome message, upcoming events preview (3-5 cards), quick shortcuts
- **Events Page**: Full event history with tabs (Upcoming/Past/Cancelled)
- **Integration**: Uses existing EventDetailPage for all event actions
- **Layout**: Edge-to-edge, left nav, full-width content area
- **Design System v7**: ALL colors, typography, and animations maintained

## Design Overview

This document defines the complete UI design specifications for the WitchCityRope User Dashboard redesign using Design System v7. The consolidated design focuses on **two primary pages** with clear purpose separation: a welcoming dashboard landing for quick overview, and a comprehensive events page for full history management. All designs maintain the sophisticated Design System v7 aesthetic while simplifying the layout structure.

## Design Authority & Sources

### Primary Authority
- **Design System v7**: Single source of truth for all visual patterns
- **Mantine v7 Framework**: Component library and responsive patterns
- **Business Requirements v4.0**: Consolidated two-page structure

### Reference Materials
- **Button Style Guide**: Complete button implementation patterns
- **Homepage Template v7**: Header/footer reference patterns
- **Stakeholder Feedback**: Simplified functionality over complex design

## Design Philosophy

### Core Principles
- **Simple Functionality**: Two focused pages, no over-engineering
- **Event-Centric UX**: Dashboard preview + comprehensive events page
- **Warm Sophistication**: Burgundy, rose-gold, and amber create premium feel
- **Signature Animations**: v7 corner morphing and center-outward underlines
- **Accessibility First**: WCAG 2.1 AA compliance throughout
- **Mobile Excellence**: Responsive design with proven React/Mantine patterns

### Visual Language
- **Color Psychology**: Warm metallics convey luxury and community trust
- **Typography Hierarchy**: Clear information architecture with consistent scales
- **Interactive Feedback**: Subtle hover states and morphing animations
- **Spatial Design**: Edge-to-edge layout with intentional information density

## Page Specifications

### 1. Dashboard Landing Page
**Path**: `/dashboard`
**Purpose**: Welcome message + upcoming events preview + quick shortcuts

#### Layout Architecture
```
┌─────────────────────────────────────────────────────────────┐
│  Header (Sticky) - 80px height                             │
├─────────────────────────────────────────────────────────────┤
│  ┌──────────┐  ┌──────────────────────────────────────────┐ │
│  │ Left Nav │  │  Main Content Area (FULL WIDTH)          │ │
│  │ 280px    │  │  - Welcome Section                        │ │
│  │          │  │  - My Upcoming Events (3-5 cards max)    │ │
│  │ Dashboard│  │  - Quick Actions (shortcuts)             │ │
│  │ Events   │  │                                          │ │
│  │ Profile  │  │  NO RIGHT SIDEBAR                        │ │
│  │ Security │  │  NO STATUS AREA                          │ │
│  │ Membersh │  │                                          │ │
│  └──────────┘  └──────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

#### Key Components

**Welcome Section**
- Simple text: "Welcome back, [Scene Name]"
- Font: Montserrat 800, 28px
- Color: var(--color-burgundy)
- Full-width underline below (gradient: burgundy → rose-gold)

**Upcoming Events Preview**
- Section title: "My Upcoming Events"
- Display: 3-5 most recent upcoming events only
- Show "View All Events" link if more than 5 events
- Empty state: "No upcoming events scheduled"
- Each event card:
  - Date badge: Gradient background (burgundy → plum)
  - Event title, time, location
  - Participation status badge (color-coded)
  - "View Details" button → navigates to EventDetailPage
  - Hover: translateY(-4px) with shadow

**Quick Actions Section**
- Section title: "Quick Actions"
- Three shortcut buttons:
  - "Edit Profile" → `/dashboard/profile`
  - "Security Settings" → `/dashboard/security`
  - "Membership Status" → `/dashboard/membership`
- Layout: Simple row or grid (edge-to-edge)
- Button style: Secondary button pattern (burgundy outline)
- Icons: Simple emoji-style for clarity

**Left Navigation**
- 5 sections exactly: Dashboard, Events, Profile, Security, Membership
- Active state: Gradient background (burgundy → plum) with shadow
- Hover effect: Background change + translateX(4px)
- Icons: Simple emoji-style
- Active page clearly highlighted

### 2. Events Page
**Path**: `/dashboard/events`
**Purpose**: Full event history with tab-based filtering

#### Layout Architecture
```
┌─────────────────────────────────────────────────────────────┐
│  Header (Sticky) - 80px height                             │
├─────────────────────────────────────────────────────────────┤
│  ┌──────────┐  ┌──────────────────────────────────────────┐ │
│  │ Left Nav │  │  Main Content Area (FULL WIDTH)          │ │
│  │ 280px    │  │  - Page Title: "My Events"               │ │
│  │          │  │  - Tab Navigation (Upcoming|Past|Cancel) │ │
│  │ Dashboard│  │  - Event Cards (all history)             │ │
│  │ Events   │  │                                          │ │
│  │ Profile  │  │  NO RIGHT SIDEBAR                        │ │
│  │ Security │  │  NO STATUS AREA                          │ │
│  │ Membersh │  │                                          │ │
│  └──────────┘  └──────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

#### Key Components

**Tab Navigation (Mantine Tabs)**
- Three tabs: "Upcoming" | "Past" | "Cancelled"
- Default active: "Upcoming"
- Active tab styling:
  - Border bottom: 3px solid var(--color-burgundy)
  - Font weight: 700
  - Color: var(--color-burgundy)
- Inactive tab: Color var(--color-charcoal)
- Tab switching: No full page reload (client-side filtering)

**Event Cards (Same as Dashboard)**
- Display ALL events matching tab filter
- Sort order:
  - Upcoming: Soonest first (ascending date)
  - Past: Most recent first (descending date)
  - Cancelled: Most recent first (descending date)
- Each event card identical to dashboard preview cards
- "View Details" button → EventDetailPage
- Empty states:
  - "No upcoming events"
  - "No past events"
  - "No cancelled events"

**EventDetailPage Integration**
- All "View Details" buttons navigate to existing EventDetailPage
- Pass event ID in URL: `/events/{eventId}`
- EventDetailPage handles all event actions:
  - Cancel RSVP
  - View Ticket
  - Add to Calendar
  - Contact Teacher
  - etc.
- No duplication of event action logic
- Browser back returns to context (dashboard or events page)

### 3. Profile Settings Page
**Path**: `/dashboard/profile`
**Purpose**: User profile editing (simplified)

#### Layout
Same left nav structure, main content area for profile form.

### 4. Security Settings Page
**Path**: `/dashboard/security`
**Purpose**: Security management (simplified)

#### Layout
Same left nav structure, main content area for security settings.

### 5. Membership Status Page
**Path**: `/dashboard/membership`
**Purpose**: Membership information and management

#### Layout
Same left nav structure, main content area for membership details.

## Design System v7 Implementation

### Color Application

#### Primary Colors
```css
--color-burgundy: #880124        /* Navigation active, titles */
--color-rose-gold: #B76D75       /* Accents, borders, underlines */
--color-amber: #FFBF00           /* Primary action buttons */
--color-electric: #9D4EDD        /* Secondary action buttons */
--color-plum: #614B79            /* Supporting elements */
```

#### Background Hierarchy
```css
--color-cream: #FAF6F2           /* Page background */
--color-ivory: #FFF8F0           /* Card backgrounds */
--color-charcoal: #2B2B2B        /* Primary text */
--color-stone: #8B8680           /* Secondary text */
```

### Typography Implementation

#### Font Usage
```css
/* Page titles - dramatic impact */
font-family: 'Montserrat', sans-serif;
font-weight: 800;
font-size: 28px;
text-transform: uppercase;
letter-spacing: -0.5px;
color: var(--color-burgundy);

/* Section titles */
font-family: 'Montserrat', sans-serif;
font-weight: 700;
font-size: 20px;
text-transform: uppercase;
letter-spacing: 0.5px;

/* Navigation items - clean readability */
font-family: 'Montserrat', sans-serif;
font-weight: 500;
font-size: 14px;
text-transform: uppercase;
letter-spacing: 0.5px;

/* Body text - comfortable reading */
font-family: 'Source Sans 3', sans-serif;
font-weight: 400;
font-size: 16px;
line-height: 1.6;

/* Event card titles */
font-family: 'Montserrat', sans-serif;
font-weight: 700;
font-size: 16px;
```

### Signature Animations

#### 1. Button Corner Morphing (CRITICAL)
```css
.btn {
    border-radius: 12px 6px 12px 6px;  /* Default asymmetric */
    transition: all 0.3s ease;
}

.btn:hover {
    border-radius: 6px 12px 6px 12px;  /* Morphed corners */
    /* NO translateY - no vertical movement */
}

.btn:disabled {
    /* NO corner morphing for disabled state */
    opacity: 0.6;
    cursor: not-allowed;
}
```

#### 2. Navigation Underline (Center Outward)
```css
.nav-item::after {
    content: '';
    position: absolute;
    bottom: -4px;
    left: 50%;
    transform: translateX(-50%);
    width: 0;
    height: 2px;
    background: linear-gradient(90deg, transparent, var(--color-burgundy), transparent);
    transition: width 0.3s ease;
}

.nav-item:hover::after {
    width: 100%;
}
```

#### 3. Card Hover Effects
```css
.event-card:hover {
    transform: translateY(-4px);        /* Subtle elevation */
    box-shadow: 0 8px 30px rgba(0,0,0,0.12);
}
```

## Responsive Design Specifications

### Breakpoint Strategy
- **Mobile**: max-width: 768px
- **Desktop**: min-width: 769px
- **Framework**: Use Mantine v7 responsive patterns

### Mobile Transformations

#### Layout Changes
```css
@media (max-width: 768px) {
    .main-layout {
        grid-template-columns: 1fr;    /* Single column */
        gap: var(--space-lg);
    }

    .left-nav {
        order: 2;                      /* Below main content */
        border-right: none;
        border-top: 2px solid rgba(183, 109, 117, 0.1);
    }

    .main-content {
        order: 1;                      /* Above navigation */
        padding: var(--space-lg) 20px;
    }

    .events-list {
        gap: var(--space-md);
    }
}
```

#### Component Adaptations
- **Event Cards**: Grid → Single column stack
- **Quick Actions**: Row → Vertical stack
- **Tab Navigation**: Scrollable on mobile if needed
- **Button Groups**: Horizontal → Vertical stack
- **Touch Targets**: Minimum 44px × 44px

### Touch Optimization
- **Minimum Touch Targets**: 44px × 44px
- **Button Padding**: Increased on mobile
- **Scroll Areas**: Native momentum scrolling

## Component Specifications

### Event Cards (Mantine Card)
```typescript
interface EventCardProps {
  id: string;
  title: string;
  date: Date;
  location: string;
  status: 'confirmed' | 'pending' | 'waitlisted';
  time: string;
  onViewDetails: (id: string) => void;
}

// Mantine v7 implementation approach
<Card
  radius="md"
  withBorder
  shadow="sm"
  style={{
    borderLeft: '3px solid var(--color-burgundy)',
    background: 'var(--color-ivory)',
    transition: 'all 0.3s ease',
    '&:hover': {
      transform: 'translateY(-4px)',
      boxShadow: '0 8px 30px rgba(0,0,0,0.12)',
    }
  }}
>
  <Group align="flex-start" spacing="lg">
    <Box /* Date badge */>
      <Text size="xl" weight={800}>{day}</Text>
      <Text size="xs" transform="uppercase">{month}</Text>
    </Box>
    <Box style={{ flex: 1 }}>
      <Text size="lg" weight={700}>{title}</Text>
      <Group spacing="md">
        <Text size="sm">{location}</Text>
        <Text size="sm">{time}</Text>
        <Badge color={statusColor}>{status}</Badge>
      </Group>
    </Box>
    <Button
      variant="outline"
      color="burgundy"
      onClick={() => onViewDetails(id)}
    >
      View Details
    </Button>
  </Group>
</Card>
```

### Left Navigation (Mantine NavLink)
```typescript
interface NavItem {
  icon: string;
  label: string;
  href: string;
  active?: boolean;
}

const navItems: NavItem[] = [
  { icon: '📊', label: 'Dashboard', href: '/dashboard', active: true },
  { icon: '📅', label: 'Events', href: '/dashboard/events' },
  { icon: '👤', label: 'Profile', href: '/dashboard/profile' },
  { icon: '🔒', label: 'Security', href: '/dashboard/security' },
  { icon: '🎯', label: 'Membership', href: '/dashboard/membership' }
];

// Mantine v7 implementation approach
<Stack spacing="xs">
  {navItems.map(item => (
    <NavLink
      key={item.href}
      label={item.label}
      icon={<Text>{item.icon}</Text>}
      active={item.active}
      onClick={() => navigate(item.href)}
      styles={{
        root: {
          borderRadius: 0,
          '&[data-active]': {
            background: 'linear-gradient(135deg, var(--color-burgundy), var(--color-plum))',
            color: 'var(--color-ivory)',
          },
          '&:hover:not([data-active])': {
            background: 'rgba(183, 109, 117, 0.1)',
            transform: 'translateX(4px)',
          }
        }
      }}
    />
  ))}
</Stack>
```

### Tab Navigation (Mantine Tabs)
```typescript
interface EventsTabsProps {
  activeTab: 'upcoming' | 'past' | 'cancelled';
  onTabChange: (tab: string) => void;
  upcomingCount: number;
  pastCount: number;
  cancelledCount: number;
}

// Mantine v7 implementation
<Tabs
  value={activeTab}
  onTabChange={onTabChange}
  styles={{
    tab: {
      fontFamily: 'Montserrat, sans-serif',
      fontWeight: 500,
      fontSize: '14px',
      textTransform: 'uppercase',
      letterSpacing: '0.5px',
      '&[data-active]': {
        borderBottomColor: 'var(--color-burgundy)',
        borderBottomWidth: '3px',
        color: 'var(--color-burgundy)',
        fontWeight: 700,
      }
    }
  }}
>
  <Tabs.List>
    <Tabs.Tab value="upcoming">
      Upcoming ({upcomingCount})
    </Tabs.Tab>
    <Tabs.Tab value="past">
      Past ({pastCount})
    </Tabs.Tab>
    <Tabs.Tab value="cancelled">
      Cancelled ({cancelledCount})
    </Tabs.Tab>
  </Tabs.List>

  <Tabs.Panel value="upcoming">
    <EventsList events={upcomingEvents} />
  </Tabs.Panel>

  <Tabs.Panel value="past">
    <EventsList events={pastEvents} />
  </Tabs.Panel>

  <Tabs.Panel value="cancelled">
    <EventsList events={cancelledEvents} />
  </Tabs.Panel>
</Tabs>
```

### Quick Actions Section
```typescript
interface QuickAction {
  icon: string;
  label: string;
  href: string;
}

const quickActions: QuickAction[] = [
  { icon: '👤', label: 'Edit Profile', href: '/dashboard/profile' },
  { icon: '🔒', label: 'Security Settings', href: '/dashboard/security' },
  { icon: '🎯', label: 'Membership Status', href: '/dashboard/membership' }
];

// Mantine v7 implementation
<Box className="quick-actions" mt="xl">
  <Title order={3}>Quick Actions</Title>
  <Group spacing="md" mt="md">
    {quickActions.map(action => (
      <Button
        key={action.href}
        variant="outline"
        color="burgundy"
        leftIcon={<Text>{action.icon}</Text>}
        onClick={() => navigate(action.href)}
        styles={{
          root: {
            borderRadius: '12px 6px 12px 6px',
            transition: 'all 0.3s ease',
            '&:hover': {
              borderRadius: '6px 12px 6px 12px',
            }
          }
        }}
      >
        {action.label}
      </Button>
    ))}
  </Group>
</Box>
```

## Accessibility Implementation

### WCAG 2.1 AA Compliance

#### Color Contrast
- **Primary text**: 7:1 ratio (charcoal on cream)
- **Secondary text**: 4.5:1 ratio (stone on ivory)
- **Interactive elements**: 4.5:1 minimum
- **Status indicators**: Color + text/icon combinations

#### Keyboard Navigation
```css
/* Focus states for all interactive elements */
.nav-link:focus,
.btn:focus,
.tab:focus {
    outline: 3px solid rgba(136, 1, 36, 0.5);
    outline-offset: 2px;
}

/* Skip links for screen readers */
.skip-link {
    position: absolute;
    top: -40px;
    left: 6px;
    background: var(--color-burgundy);
    color: var(--color-ivory);
    padding: 8px;
    text-decoration: none;
    transition: top 0.3s;
}

.skip-link:focus {
    top: 6px;
}
```

#### Screen Reader Support
```html
<!-- Semantic landmarks -->
<main role="main" aria-label="User Dashboard">
<nav aria-label="Dashboard navigation">
<article aria-labelledby="event-title-1">

<!-- Tab navigation -->
<Tabs aria-label="Event filtering tabs">
  <Tabs.Tab value="upcoming" aria-controls="upcoming-panel">
    Upcoming Events
  </Tabs.Tab>
</Tabs>

<!-- Event cards with proper headings -->
<article aria-labelledby="event-title-1">
  <h3 id="event-title-1">Rope Fundamentals Workshop</h3>
  <!-- Event details -->
</article>
```

#### Reduced Motion Support
```css
@media (prefers-reduced-motion: reduce) {
    *, *::before, *::after {
        animation-duration: 0.01ms !important;
        animation-iteration-count: 1 !important;
        transition-duration: 0.01ms !important;
    }
}
```

## Performance Optimization

### CSS Performance
- **Hardware Acceleration**: Use `transform3d()` for smooth animations
- **Paint Optimization**: Avoid layout-triggering properties
- **Memory Management**: Limit concurrent animations

### Loading States
```typescript
// Skeleton loading for event cards
<Skeleton height={120} radius="md" mb="md" />
<Skeleton height={120} radius="md" mb="md" />
<Skeleton height={120} radius="md" />

// Progressive enhancement
{loading ? (
  <EventCardSkeleton count={3} />
) : events.length > 0 ? (
  <EventsList events={events} />
) : (
  <EmptyState message="No upcoming events" />
)}
```

### Data Fetching Strategy
- **Dashboard Landing**: Fetch only 5 most recent upcoming events
- **Events Page**: Fetch all events, filter client-side by tab
- **TanStack Query**: Cache events data, invalidate on mutations
- **Optimistic Updates**: Update UI immediately, rollback on error

## Implementation Roadmap

### Phase 1: Dashboard Landing Page (Week 1)
- [ ] Setup Design System v7 CSS variables
- [ ] Implement basic layout with left navigation
- [ ] Create welcome section with underline
- [ ] Build event card component
- [ ] Implement upcoming events preview (max 5)
- [ ] Add quick actions section
- [ ] Setup responsive grid system

### Phase 2: Events Page (Week 2)
- [ ] Create tab navigation component
- [ ] Implement event filtering logic
- [ ] Build full event history display
- [ ] Add empty states for each tab
- [ ] Connect "View All Events" link from dashboard
- [ ] Test tab switching performance

### Phase 3: EventDetailPage Integration (Week 3)
- [ ] Implement navigation to EventDetailPage
- [ ] Pass event ID correctly in URL
- [ ] Test browser back button behavior
- [ ] Verify state preservation (active tab)
- [ ] Ensure no duplicate event actions

### Phase 4: Additional Pages (Week 4)
- [ ] Profile settings page (simplified)
- [ ] Security settings page (simplified)
- [ ] Membership status page
- [ ] Left nav state management across pages

### Phase 5: Polish & Testing (Week 5)
- [ ] Animation refinements
- [ ] Accessibility testing with screen readers
- [ ] Cross-browser testing
- [ ] Performance optimization
- [ ] Mobile device testing
- [ ] Load testing with many events

## Quality Assurance Checklist

### Visual Design
- [ ] Design System v7 colors used consistently
- [ ] Typography hierarchy clear and readable
- [ ] Signature animations implemented correctly (corner morphing, underlines)
- [ ] Responsive design works on all target devices
- [ ] Brand consistency maintained throughout
- [ ] No right sidebar or status area
- [ ] Edge-to-edge layout implemented

### User Experience
- [ ] Dashboard shows welcome + preview (3-5 events max)
- [ ] Events page shows full history with tabs
- [ ] Tab filtering works without page reload
- [ ] "View Details" navigates to EventDetailPage correctly
- [ ] Quick actions shortcuts work
- [ ] Empty states clear and helpful
- [ ] Loading states and error handling implemented
- [ ] Browser back button works correctly

### Technical Implementation
- [ ] Mantine v7 components used appropriately
- [ ] TypeScript interfaces defined for all data
- [ ] CSS-in-JS integration with Mantine styling
- [ ] Performance targets met (<1.5s dashboard, <2s events page)
- [ ] Cross-browser compatibility verified
- [ ] TanStack Query setup for data fetching
- [ ] NSwag types used for API data
- [ ] No manual DTO creation

### Accessibility
- [ ] WCAG 2.1 AA compliance verified
- [ ] Keyboard navigation functional throughout
- [ ] Screen reader testing completed
- [ ] Color contrast ratios verified
- [ ] Focus management implemented
- [ ] Skip links available
- [ ] Reduced motion support

### Integration
- [ ] EventDetailPage integration tested
- [ ] Event ID passing verified
- [ ] Navigation context preserved
- [ ] No duplicate event action logic
- [ ] API endpoints correct
- [ ] Error handling complete

## Maintenance Guidelines

### Design System Updates
- Always reference Design System v7 for any visual changes
- Never introduce new colors without updating the design system
- Maintain signature animation consistency across all components
- Update documentation when patterns evolve

### Component Evolution
- Extend Mantine v7 components rather than creating custom ones
- Maintain TypeScript interfaces for all component props
- Document any custom styling additions
- Test accessibility impact of any changes

### Performance Monitoring
- Monitor Core Web Vitals regularly
- Test on low-end devices periodically
- Optimize images and assets as content grows
- Review animation performance on older browsers

---

**Implementation Notes**: These consolidated designs provide a complete foundation for the WitchCityRope User Dashboard using Design System v7. The focus on two primary pages (Dashboard Landing + Events Page) with clear purpose separation creates a simple, functional experience. The removal of the right sidebar and integration with existing EventDetailPage maintains simplicity while leveraging proven patterns. All Design System v7 standards are maintained for color, typography, and animations.

**Next Steps**: Begin Phase 1 implementation with Mantine v7 component setup and Dashboard Landing page. All designs are ready for developer handoff with complete specifications and implementation guidance.

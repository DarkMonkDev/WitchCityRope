# UI Design Specifications: User Dashboard v7 (PROFILE CONSOLIDATION & HORIZONTAL NAV)
<!-- Last Updated: 2025-10-09 -->
<!-- Version: 3.0 - PROFILE CONSOLIDATION & HORIZONTAL NAV -->
<!-- Owner: UI Designer Agent -->
<!-- Status: Ready for Implementation -->

## 🚨 VERSION 3.0 UPDATES 🚨

**Major navigation and profile simplification based on October 2025 research:**

### 1. **HORIZONTAL TAB NAVIGATION** (replaces left sidebar)
- **Space Savings**: 280px sidebar → 60px tabs = 78% more screen real estate
- **Three Tabs Only**: Dashboard | Events | Profile Settings
- **Mobile-First**: Tabs naturally scroll on mobile (Material Design pattern)
- **Modern 2024-2025**: Aligns with Stripe, Airbnb, Google Admin patterns
- **Layout**: Top of page, full-width, ~60px height
- **Active Indicator**: 3px bottom border (burgundy)

### 2. **PROFILE SETTINGS CONSOLIDATION** (single page)
- **Merged Pages**: Profile + Security + Membership → ONE page
- **Three Sections**: Profile Information, Change Password, Vetting Status
- **Single Page**: All sections visible without navigation
- **Section Buttons**: Each section has its own action button (Save Profile, Change Password, Put On Hold)
- **Vertical Layout**: Sections stack vertically on scrolling page

### 3. **SOCIAL EVENT TICKET INDICATORS** (PROMINENT)
- **Ticket Purchased Badge**: LARGE green indicator (larger than other badges)
- **Purchase Ticket Button**: LARGE amber/gold gradient button (larger than View Details)
- **Direct Checkout**: Purchase button navigates to checkout (skips EventDetailPage)
- **Visual Hierarchy**: Ticket status is PRIMARY element on social event cards

### 4. **WAITLIST REMOVAL**
- **No Waitlist Status**: Removed from participation status enum
- **No Waitlist Tab**: Events page has only Upcoming/Past/Cancelled
- **No Waitlist Badges**: All waitlist indicators deleted
- **Simplified Status**: Only "Registered" and "Ticketed" statuses

### 5. **MEMBERSHIP HOLD MODAL**
- **Trigger**: "Put Membership On Hold" button in Profile Settings
- **Modal Title**: "Put Membership On Hold"
- **Warning**: "You can apply to take it off hold later, but while on hold you can't attend any social events."
- **Required Field**: "Reason for hold" textarea (minimum 10 characters)
- **Buttons**: "Confirm" (primary) + "Cancel" (secondary)

## Design Overview

This document defines the complete UI design specifications for the WitchCityRope User Dashboard redesign using Design System v7. Version 3.0 introduces **horizontal tab navigation** for dramatically improved screen space efficiency, **consolidated Profile Settings** merging three pages into one, and **prominent social event ticket indicators** with direct checkout flow. All designs maintain the sophisticated Design System v7 aesthetic while implementing modern 2024-2025 navigation patterns.

## Design Authority & Sources

### Primary Authority
- **Design System v7.1**: Single source of truth for all visual patterns
- **Mantine v7 Framework**: Component library and responsive patterns
- **Business Requirements v5.0**: Profile consolidation and navigation simplification
- **Navigation Research**: `/docs/functional-areas/user-management/research/2025-10-09-simple-dashboard-navigation-patterns.md`

### Reference Materials
- **Button Style Guide**: Complete button implementation patterns
- **Homepage Template v7**: Header/footer reference patterns
- **Material Design Tabs**: Industry standard tab navigation patterns
- **Stakeholder Feedback**: Simple functionality over complex design

## Design Philosophy

### Core Principles
- **Maximum Screen Space**: Horizontal tabs reclaim 78% of sidebar space
- **Simple Functionality**: Three tabs, three profile sections, clear purpose
- **Event-Centric UX**: Dashboard preview + comprehensive events page
- **Social Event Focus**: Ticket purchase is CRITICAL user journey
- **Warm Sophistication**: Burgundy, rose-gold, and amber create premium feel
- **Signature Animations**: v7 corner morphing and center-outward underlines
- **Accessibility First**: WCAG 2.1 AA compliance throughout
- **Mobile Excellence**: Responsive design with proven React/Mantine patterns

### Visual Language
- **Color Psychology**: Warm metallics convey luxury and community trust
- **Typography Hierarchy**: Clear information architecture with consistent scales
- **Interactive Feedback**: Subtle hover states and morphing animations
- **Spatial Design**: Edge-to-edge layout with intentional information density
- **Prominent CTAs**: Social event ticket actions are LARGEST elements

## Navigation Architecture (NEW)

### Horizontal Tab Navigation Pattern

**Layout Structure**:
```
┌─────────────────────────────────────────────────────────────┐
│  Header (Sticky) - 80px height                             │
├─────────────────────────────────────────────────────────────┤
│  ┌─ Dashboard ─┬─ Events ─┬─ Profile Settings ─┐          │
│  │  (active)   │           │                     │  ~60px  │
│  └─────────────┴───────────┴─────────────────────┘          │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Main Content Area (FULL WIDTH edge-to-edge)                │
│  - No left sidebar                                          │
│  - No right sidebar                                         │
│  - Content spans entire page width                          │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

**Visual Design**:
- **Tab Container**: Full-width bar below header
- **Tab Height**: ~60px (desktop), ~56px (mobile)
- **Tab Spacing**: 32px gap between tabs
- **Active Tab**:
  - Border bottom: 3px solid var(--color-burgundy)
  - Font weight: 700
  - Color: var(--color-burgundy)
  - Background: transparent
- **Inactive Tab**:
  - Border bottom: none
  - Font weight: 500
  - Color: var(--color-charcoal)
  - Background: transparent
  - Hover: Color changes to var(--color-burgundy) with 0.3s transition
- **Tab Typography**:
  - Font: Montserrat, sans-serif
  - Size: 14px
  - Transform: uppercase
  - Letter spacing: 0.5px

**Mantine Implementation**:
```typescript
import { Tabs } from '@mantine/core';
import { useNavigate, useLocation } from 'react-router-dom';

function DashboardNavigation() {
  const navigate = useNavigate();
  const location = useLocation();

  // Determine active tab from current route
  const getActiveTab = () => {
    if (location.pathname === '/dashboard') return 'dashboard';
    if (location.pathname.startsWith('/dashboard/events')) return 'events';
    if (location.pathname === '/dashboard/profile-settings') return 'settings';
    return 'dashboard';
  };

  const handleTabChange = (value: string) => {
    switch(value) {
      case 'dashboard':
        navigate('/dashboard');
        break;
      case 'events':
        navigate('/dashboard/events');
        break;
      case 'settings':
        navigate('/dashboard/profile-settings');
        break;
    }
  };

  return (
    <Tabs
      value={getActiveTab()}
      onChange={handleTabChange}
      styles={{
        root: {
          borderBottom: '1px solid rgba(183, 109, 117, 0.2)',
        },
        tab: {
          fontFamily: 'Montserrat, sans-serif',
          fontWeight: 500,
          fontSize: '14px',
          textTransform: 'uppercase',
          letterSpacing: '0.5px',
          padding: '20px 32px',
          transition: 'all 0.3s ease',
          '&:hover': {
            color: 'var(--color-burgundy)',
            backgroundColor: 'transparent',
          },
          '&[data-active]': {
            borderBottomWidth: '3px',
            borderBottomColor: 'var(--color-burgundy)',
            color: 'var(--color-burgundy)',
            fontWeight: 700,
            backgroundColor: 'transparent',
          }
        }
      }}
    >
      <Tabs.List>
        <Tabs.Tab value="dashboard">Dashboard</Tabs.Tab>
        <Tabs.Tab value="events">Events</Tabs.Tab>
        <Tabs.Tab value="settings">Profile Settings</Tabs.Tab>
      </Tabs.List>
    </Tabs>
  );
}
```

**Mobile Responsive Behavior**:
- **Desktop (>768px)**: Horizontal tabs with equal spacing
- **Tablet (768px)**: Tabs remain horizontal, may reduce padding
- **Mobile (<768px)**:
  - Tabs remain horizontal with scrollable overflow
  - Partial visibility hint (last tab partially visible) indicates scrollability
  - Native horizontal scroll with momentum
  - Same visual styling maintained

**Benefits**:
1. **Space Efficiency**: 280px (sidebar) → 60px (tabs) = 220px reclaimed
2. **Familiarity**: Universal pattern users recognize immediately
3. **Mobile-First**: Tabs are native mobile pattern (Airbnb, Revolut proven)
4. **Visual Hierarchy**: Navigation doesn't dominate interface
5. **Accessibility**: Keyboard navigable, ARIA-compliant

**Research Validation**:
- Material Design: "Fixed tabs work best with a small number of options (ideally four or fewer)"
- Nielsen Norman Group: "Tabs are used right when they simplify navigation in a complex application"
- Real-world examples: Stripe, Airbnb, Google Admin all use tabs for 3-5 item navigation

## Page Specifications

### 1. Dashboard Landing Page
**Path**: `/dashboard`
**Purpose**: Welcome message + upcoming events preview + quick shortcuts

#### Layout Architecture (NEW - No Sidebar)
```
┌─────────────────────────────────────────────────────────────┐
│  Header (Sticky) - 80px height                             │
├─────────────────────────────────────────────────────────────┤
│  ┌─ Dashboard ─┬─ Events ─┬─ Profile Settings ─┐          │
│  │  (active)   │           │                     │  ~60px  │
│  └─────────────┴───────────┴─────────────────────┘          │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Main Content Area (FULL WIDTH edge-to-edge)                │
│                                                             │
│  - Welcome Section: "Welcome back, [Scene Name]"            │
│  - My Upcoming Events (3-5 cards max)                       │
│    ✓ Social events show PROMINENT ticket indicators         │
│    ✓ "Purchase Ticket" button LARGER than other elements    │
│  - Quick Actions (shortcuts)                                │
│                                                             │
│  NO LEFT SIDEBAR                                           │
│  NO RIGHT SIDEBAR                                          │
│                                                             │
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
  - **NEW: Social Event Ticket Indicators** (see component spec below)
  - Participation status badge (color-coded) - SMALLER than ticket indicators
  - "View Details" button → navigates to EventDetailPage
  - Hover: translateY(-4px) with shadow

**Quick Actions Section**
- Section title: "Quick Actions"
- ONE shortcut button:
  - "Profile Settings" → `/dashboard/profile-settings`
- Layout: Simple centered button
- Button style: Secondary button pattern (burgundy outline)
- Icon: Simple emoji-style for clarity

### 2. Events Page
**Path**: `/dashboard/events`
**Purpose**: Full event history with tab-based filtering (NO WAITLIST)

#### Layout Architecture (NEW - No Sidebar)
```
┌─────────────────────────────────────────────────────────────┐
│  Header (Sticky) - 80px height                             │
├─────────────────────────────────────────────────────────────┤
│  ┌─ Dashboard ─┬─ Events ─┬─ Profile Settings ─┐          │
│  │             │ (active) │                     │  ~60px  │
│  └─────────────┴───────────┴─────────────────────┘          │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Main Content Area (FULL WIDTH edge-to-edge)                │
│                                                             │
│  - Page Title: "My Events"                                  │
│  - Tab Navigation (Upcoming | Past | Cancelled)             │
│    NOTE: NO WAITLIST TAB                                    │
│  - Event Cards (all history)                                │
│    ✓ Social events show PROMINENT ticket indicators         │
│    ✓ NO "Waitlisted" status badges                          │
│                                                             │
│  NO LEFT SIDEBAR                                           │
│  NO RIGHT SIDEBAR                                          │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

#### Key Components

**Tab Navigation (Mantine Tabs) - NO WAITLIST**
- **Three tabs only**: "Upcoming" | "Past" | "Cancelled"
- Default active: "Upcoming"
- Active tab styling:
  - Border bottom: 3px solid var(--color-burgundy)
  - Font weight: 700
  - Color: var(--color-burgundy)
- Inactive tab: Color var(--color-charcoal)
- Tab switching: No full page reload (client-side filtering)
- **REMOVED**: "Waitlisted" tab completely deleted

**Event Cards (Same as Dashboard with Social Ticket Indicators)**
- Display ALL events matching tab filter
- Sort order:
  - Upcoming: Soonest first (ascending date)
  - Past: Most recent first (descending date)
  - Cancelled: Most recent first (descending date)
- Each event card identical to dashboard preview cards
- **Social Event Ticket Indicators**: See component spec below
- "View Details" button → EventDetailPage
- Empty states:
  - "No upcoming events"
  - "No past events"
  - "No cancelled events"
- **REMOVED**: All "Waitlisted" status badges

**Participation Status Values (SIMPLIFIED)**:
- "Registered" - User has RSVP'd
- "Ticketed" - User has purchased ticket
- **REMOVED**: "Waitlisted" status completely eliminated

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

### 3. Profile Settings Page (NEW - CONSOLIDATED)
**Path**: `/dashboard/profile-settings`
**Purpose**: ALL user settings on single page (Profile + Security + Vetting)

#### Layout Architecture (NEW - Single Page with Three Sections)
```
┌─────────────────────────────────────────────────────────────┐
│  Header (Sticky) - 80px height                             │
├─────────────────────────────────────────────────────────────┤
│  ┌─ Dashboard ─┬─ Events ─┬─ Profile Settings ─┐          │
│  │             │           │      (active)       │  ~60px  │
│  └─────────────┴───────────┴─────────────────────┘          │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Main Content Area (FULL WIDTH edge-to-edge)                │
│                                                             │
│  Page Title: "Profile Settings"                             │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ SECTION 1: Profile Information                      │   │
│  │ - Scene Name (editable)                             │   │
│  │ - Email (editable)                                  │   │
│  │ - Pronouns (editable)                               │   │
│  │ - Bio (editable textarea)                           │   │
│  │ [Save Profile] button                               │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ SECTION 2: Change Password                          │   │
│  │ - Current Password (password input)                 │   │
│  │ - New Password (password input)                     │   │
│  │ - Confirm Password (password input)                 │   │
│  │ [Change Password] button                            │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ SECTION 3: Vetting Status                           │   │
│  │ - Current Status: "Vetted" (read-only)              │   │
│  │ - Last Updated: "January 15, 2025" (read-only)      │   │
│  │ [Put Membership On Hold] button (if not on hold)    │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  NO LEFT SIDEBAR                                           │
│  NO RIGHT SIDEBAR                                          │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

#### Section 1: Profile Information

**Fields**:
- **Scene Name**: TextInput (required, 3-50 characters)
- **Email**: TextInput (required, valid email format)
- **Pronouns**: TextInput or Select (optional, max 50 characters)
- **Bio**: Textarea (optional, max 500 characters, 4-5 rows)

**Action Button**:
- "Save Profile" button
- Style: Primary CTA button (gold/amber gradient)
- Position: Below bio field
- Validation: Client-side validation before submit

**Mantine Implementation**:
```typescript
<Box className="profile-section" p="xl" mb="xl" bg="var(--color-ivory)" style={{ borderRadius: '12px' }}>
  <Title order={3} mb="lg" color="var(--color-burgundy)">Profile Information</Title>
  <Stack spacing="md">
    <TextInput
      label="Scene Name"
      placeholder="Enter your scene name"
      required
      minLength={3}
      maxLength={50}
    />
    <TextInput
      label="Email"
      placeholder="your.email@example.com"
      type="email"
      required
    />
    <TextInput
      label="Pronouns"
      placeholder="they/them, she/her, etc."
      maxLength={50}
    />
    <Textarea
      label="Bio"
      placeholder="Tell us about yourself..."
      maxLength={500}
      rows={5}
    />
    <Button
      className="btn btn-primary"
      size="lg"
      style={{
        borderRadius: '12px 6px 12px 6px',
        transition: 'all 0.3s ease',
      }}
      styles={{
        root: {
          '&:hover': {
            borderRadius: '6px 12px 6px 12px',
          }
        }
      }}
    >
      Save Profile
    </Button>
  </Stack>
</Box>
```

#### Section 2: Change Password

**Fields**:
- **Current Password**: PasswordInput (required)
- **New Password**: PasswordInput (required, min 8 characters, complexity rules)
- **Confirm Password**: PasswordInput (required, must match new password)

**Action Button**:
- "Change Password" button
- Style: Primary Alt CTA button (electric purple gradient)
- Position: Below confirm password field
- Validation: Password complexity + match validation

**Password Requirements** (displayed below New Password field):
- Minimum 8 characters
- At least 1 uppercase letter
- At least 1 lowercase letter
- At least 1 number

**Mantine Implementation**:
```typescript
<Box className="password-section" p="xl" mb="xl" bg="var(--color-ivory)" style={{ borderRadius: '12px' }}>
  <Title order={3} mb="lg" color="var(--color-burgundy)">Change Password</Title>
  <Stack spacing="md">
    <PasswordInput
      label="Current Password"
      placeholder="Enter current password"
      required
    />
    <PasswordInput
      label="New Password"
      placeholder="Enter new password"
      required
      minLength={8}
      description="Minimum 8 characters with uppercase, lowercase, and number"
    />
    <PasswordInput
      label="Confirm Password"
      placeholder="Confirm new password"
      required
    />
    <Button
      className="btn btn-primary-alt"
      size="lg"
      style={{
        borderRadius: '12px 6px 12px 6px',
        transition: 'all 0.3s ease',
        background: 'linear-gradient(135deg, #9D4EDD 0%, #7B2CBF 100%)',
      }}
      styles={{
        root: {
          '&:hover': {
            borderRadius: '6px 12px 6px 12px',
          }
        }
      }}
    >
      Change Password
    </Button>
  </Stack>
</Box>
```

#### Section 3: Vetting Status

**Display Fields** (read-only):
- **Current Status**: Text display showing vetting status enum value
  - "Pending" | "Vetted" | "On Hold" | "Denied"
  - Color-coded: Success (green) for Vetted, Warning (amber) for Pending/On Hold, Error (red) for Denied
- **Last Updated**: Formatted date display (e.g., "January 15, 2025")

**Action Button** (conditional):
- "Put Membership On Hold" button
- **Visibility**: Only shown if current status is NOT "On Hold"
- Style: Secondary button (burgundy outline)
- Position: Below status display
- Action: Opens Membership Hold Modal (see component spec below)

**Mantine Implementation**:
```typescript
<Box className="vetting-section" p="xl" mb="xl" bg="var(--color-ivory)" style={{ borderRadius: '12px' }}>
  <Title order={3} mb="lg" color="var(--color-burgundy)">Vetting Status</Title>
  <Stack spacing="md">
    <Group spacing="sm">
      <Text weight={600}>Current Status:</Text>
      <Badge
        color={vettingStatus === 'Vetted' ? 'green' : vettingStatus === 'Denied' ? 'red' : 'yellow'}
        size="lg"
      >
        {vettingStatus}
      </Badge>
    </Group>
    <Group spacing="sm">
      <Text weight={600}>Last Updated:</Text>
      <Text>{new Date(statusUpdatedAt).toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'long',
        day: 'numeric'
      })}</Text>
    </Group>
    {vettingStatus !== 'On Hold' && (
      <Button
        className="btn btn-secondary"
        variant="outline"
        color="var(--color-burgundy)"
        onClick={openMembershipHoldModal}
        style={{
          borderRadius: '12px 6px 12px 6px',
          transition: 'all 0.3s ease',
        }}
        styles={{
          root: {
            '&:hover': {
              borderRadius: '6px 12px 6px 12px',
              backgroundColor: 'var(--color-burgundy)',
              color: 'var(--color-ivory)',
            }
          }
        }}
      >
        Put Membership On Hold
      </Button>
    )}
  </Stack>
</Box>
```

**Layout Notes**:
- All three sections visible on same page (no navigation required)
- Sections stack vertically with spacing between
- User can scroll smoothly between sections
- Each section is visually distinct with ivory background and rounded corners
- No multi-page navigation complexity

## Component Specifications (NEW & UPDATED)

### Social Event Ticket Indicators (NEW - PROMINENT)

**Purpose**: Make social event ticketing the PRIMARY call-to-action

**Visual Hierarchy Rule**: Ticket status indicators must be **LARGER and MORE PROMINENT** than all other event card elements

#### Ticket Purchased State (Social Event with Ticket)

**Visual Design**:
- **Badge Size**: LARGE (height: 48px, padding: 12px 24px)
- **Background**: Success gradient (linear-gradient(135deg, #228B22 0%, #2F8B2F 100%))
- **Text**: "✓ Ticket Purchased" in white
- **Font**: Montserrat 700, 16px (LARGER than other text)
- **Border Radius**: 12px 6px 12px 6px (signature asymmetric)
- **Position**: Prominent position on event card (top right or center)
- **Shadow**: 0 4px 15px rgba(34, 139, 34, 0.4)

**Mantine Implementation**:
```typescript
<Badge
  size="xl"
  style={{
    height: '48px',
    padding: '12px 24px',
    background: 'linear-gradient(135deg, #228B22 0%, #2F8B2F 100%)',
    color: '#FFF8F0',
    fontFamily: 'Montserrat, sans-serif',
    fontWeight: 700,
    fontSize: '16px',
    borderRadius: '12px 6px 12px 6px',
    boxShadow: '0 4px 15px rgba(34, 139, 34, 0.4)',
    textTransform: 'uppercase',
    letterSpacing: '1px',
  }}
>
  ✓ Ticket Purchased
</Badge>
```

#### Purchase Ticket State (Social Event without Ticket)

**Visual Design**:
- **Button Size**: LARGE (height: 56px, padding: 18px 40px)
- **Background**: Primary CTA gradient (linear-gradient(135deg, #FFBF00 0%, #FF8C00 100%))
- **Text**: "Purchase Ticket" in midnight (#1A1A2E)
- **Font**: Montserrat 700, 16px
- **Border Radius**: 12px 6px 12px 6px (default), morphs on hover
- **Position**: Prominent position on event card (center or bottom)
- **Shadow**: 0 4px 15px rgba(255, 191, 0, 0.4)
- **Hover**: Corner morph to 6px 12px 6px 12px + shimmer effect
- **Action**: Navigate directly to checkout with 1 ticket pre-selected (skip EventDetailPage)

**Mantine Implementation**:
```typescript
<Button
  size="xl"
  onClick={() => navigate(`/checkout?eventId=${eventId}&quantity=1`)}
  style={{
    height: '56px',
    padding: '18px 40px',
    background: 'linear-gradient(135deg, #FFBF00 0%, #FF8C00 100%)',
    color: '#1A1A2E',
    fontFamily: 'Montserrat, sans-serif',
    fontWeight: 700,
    fontSize: '16px',
    borderRadius: '12px 6px 12px 6px',
    boxShadow: '0 4px 15px rgba(255, 191, 0, 0.4)',
    border: 'none',
    textTransform: 'uppercase',
    letterSpacing: '1.5px',
    transition: 'all 0.3s ease',
    position: 'relative',
    overflow: 'hidden',
  }}
  styles={{
    root: {
      '&:hover': {
        borderRadius: '6px 12px 6px 12px',
        '&::before': {
          content: '""',
          position: 'absolute',
          top: 0,
          left: '-100%',
          width: '100%',
          height: '100%',
          background: 'linear-gradient(90deg, transparent, rgba(255,255,255,0.3), transparent)',
          animation: 'shimmer 0.6s',
        }
      }
    }
  }}
>
  Purchase Ticket
</Button>
```

**Shimmer Animation CSS**:
```css
@keyframes shimmer {
  0% { left: -100%; }
  100% { left: 100%; }
}
```

#### Non-Social Event Cards (Standard)

**Visual Design**:
- No special ticket indicators
- Standard "View Details" button (secondary style)
- Participation status badge (Registered/Ticketed) - standard size
- No direct checkout option

**Conditional Rendering Logic**:
```typescript
interface EventCardProps {
  event: {
    id: string;
    title: string;
    date: Date;
    location: string;
    isSocialEvent: boolean;
    hasTicket: boolean;
    participationStatus: 'Registered' | 'Ticketed';
  };
}

function EventCard({ event }: EventCardProps) {
  return (
    <Card>
      {/* Event details */}

      {event.isSocialEvent ? (
        // SOCIAL EVENT - Prominent ticket indicators
        <>
          {event.hasTicket ? (
            <Badge /* Ticket Purchased - LARGE green badge */ />
          ) : (
            <Button /* Purchase Ticket - LARGE amber button */ />
          )}
          <Button variant="outline" size="sm">View Details</Button>
        </>
      ) : (
        // NON-SOCIAL EVENT - Standard button
        <Button variant="outline">View Details</Button>
      )}
    </Card>
  );
}
```

**Design Rationale**:
- Social event ticket purchase is CRITICAL business goal
- Visual hierarchy makes ticket status impossible to miss
- Direct checkout reduces friction and increases conversion
- Prominent CTAs align with business requirements for ticket sales

### Membership Hold Modal (NEW)

**Purpose**: Capture reason for membership hold with clear warning message

**Trigger**: User clicks "Put Membership On Hold" button in Profile Settings Section 3

**Modal Specifications**:

**Visual Design**:
- **Modal Width**: 500px (desktop), 90% viewport width (mobile)
- **Title**: "Put Membership On Hold"
  - Font: Montserrat 800, 24px
  - Color: var(--color-burgundy)
- **Warning Message**:
  - Text: "You can apply to take it off hold later, but while on hold you can't attend any social events."
  - Font: Source Sans 3 400, 16px
  - Color: var(--color-charcoal)
  - Background: Warning color with low opacity (rgba(218, 165, 32, 0.1))
  - Padding: 16px
  - Border-left: 4px solid var(--color-warning)
  - Margin: 16px 0
- **Required Field**:
  - Label: "Reason for hold"
  - Component: Textarea
  - Placeholder: "Please provide a brief reason for putting your membership on hold..."
  - Validation: Minimum 10 characters, maximum 1000 characters
  - Required indicator: Red asterisk
  - Rows: 5
- **Buttons**:
  - "Confirm" - Primary CTA (gold/amber gradient)
  - "Cancel" - Secondary (burgundy outline)
  - Spacing: 16px gap between buttons
  - Alignment: Right-aligned in modal footer

**Mantine Implementation**:
```typescript
import { Modal, Textarea, Button, Alert, Group } from '@mantine/core';
import { useForm } from '@mantine/form';

function MembershipHoldModal({ opened, onClose, onConfirm }) {
  const form = useForm({
    initialValues: {
      holdReason: '',
    },
    validate: {
      holdReason: (value) =>
        value.length < 10
          ? 'Reason must be at least 10 characters'
          : value.length > 1000
          ? 'Reason must be less than 1000 characters'
          : null,
    },
  });

  const handleSubmit = form.onSubmit((values) => {
    onConfirm(values.holdReason);
    onClose();
  });

  return (
    <Modal
      opened={opened}
      onClose={onClose}
      title="Put Membership On Hold"
      size="lg"
      centered
      styles={{
        title: {
          fontFamily: 'Montserrat, sans-serif',
          fontWeight: 800,
          fontSize: '24px',
          color: 'var(--color-burgundy)',
        }
      }}
    >
      <form onSubmit={handleSubmit}>
        <Alert
          color="yellow"
          icon={<span>⚠️</span>}
          mb="md"
          styles={{
            root: {
              backgroundColor: 'rgba(218, 165, 32, 0.1)',
              borderLeft: '4px solid var(--color-warning)',
            }
          }}
        >
          You can apply to take it off hold later, but while on hold you can't attend any social events.
        </Alert>

        <Textarea
          label="Reason for hold"
          placeholder="Please provide a brief reason for putting your membership on hold..."
          required
          minLength={10}
          maxLength={1000}
          rows={5}
          {...form.getInputProps('holdReason')}
          styles={{
            label: {
              fontFamily: 'Montserrat, sans-serif',
              fontWeight: 600,
              marginBottom: '8px',
            }
          }}
        />

        <Group position="right" mt="xl" spacing="md">
          <Button
            variant="outline"
            color="burgundy"
            onClick={onClose}
            style={{
              borderRadius: '12px 6px 12px 6px',
              transition: 'all 0.3s ease',
            }}
            styles={{
              root: {
                '&:hover': {
                  borderRadius: '6px 12px 6px 12px',
                }
              }
            }}
          >
            Cancel
          </Button>
          <Button
            type="submit"
            style={{
              borderRadius: '12px 6px 12px 6px',
              background: 'linear-gradient(135deg, #FFBF00 0%, #FF8C00 100%)',
              color: '#1A1A2E',
              transition: 'all 0.3s ease',
            }}
            styles={{
              root: {
                '&:hover': {
                  borderRadius: '6px 12px 6px 12px',
                }
              }
            }}
          >
            Confirm
          </Button>
        </Group>
      </form>
    </Modal>
  );
}
```

**Workflow**:
1. User clicks "Put Membership On Hold" button → Modal opens
2. User sees warning message prominently displayed
3. User enters reason in textarea (minimum 10 characters required)
4. User clicks "Confirm" → Form validates → API call to update vetting status
5. API appends reason to vetting notes with timestamp
6. Modal closes → Section 3 refreshes to show "On Hold" status
7. "Put Membership On Hold" button is hidden (status already on hold)

**Alternative Flow**:
- User clicks "Cancel" → Modal closes → No changes saved

### Event Cards (Mantine Card) - UPDATED

**Updated to include social event ticket indicators and NO waitlist**

```typescript
interface EventCardProps {
  id: string;
  title: string;
  date: Date;
  location: string;
  status: 'confirmed' | 'pending';  // REMOVED 'waitlisted'
  isSocialEvent: boolean;  // NEW
  hasTicket: boolean;  // NEW
  time: string;
  onViewDetails: (id: string) => void;
  onPurchaseTicket?: (id: string) => void;  // NEW
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
        {!isSocialEvent && (
          <Badge color={statusColor} size="sm">{status}</Badge>
        )}
      </Group>
    </Box>

    {/* NEW: Social Event Ticket Indicators */}
    {isSocialEvent ? (
      <Stack spacing="sm" align="flex-end">
        {hasTicket ? (
          <Badge /* LARGE Ticket Purchased badge */
            size="xl"
            style={{
              height: '48px',
              padding: '12px 24px',
              background: 'linear-gradient(135deg, #228B22 0%, #2F8B2F 100%)',
              // ... (see Social Event Ticket Indicators spec)
            }}
          >
            ✓ Ticket Purchased
          </Badge>
        ) : (
          <Button /* LARGE Purchase Ticket button */
            size="xl"
            onClick={() => onPurchaseTicket(id)}
            style={{
              height: '56px',
              padding: '18px 40px',
              background: 'linear-gradient(135deg, #FFBF00 0%, #FF8C00 100%)',
              // ... (see Social Event Ticket Indicators spec)
            }}
          >
            Purchase Ticket
          </Button>
        )}
        <Button
          variant="outline"
          color="burgundy"
          size="sm"
          onClick={() => onViewDetails(id)}
        >
          View Details
        </Button>
      </Stack>
    ) : (
      <Button
        variant="outline"
        color="burgundy"
        onClick={() => onViewDetails(id)}
      >
        View Details
      </Button>
    )}
  </Group>
</Card>
```

### Tab Navigation (Mantine Tabs) - UPDATED (NO WAITLIST)

```typescript
interface EventsTabsProps {
  activeTab: 'upcoming' | 'past' | 'cancelled';  // REMOVED 'waitlisted'
  onTabChange: (tab: string) => void;
  upcomingCount: number;
  pastCount: number;
  cancelledCount: number;
  // REMOVED: waitlistedCount
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
    {/* REMOVED: Waitlisted tab */}
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

  {/* REMOVED: Waitlisted panel */}
</Tabs>
```

### Quick Actions Section - UPDATED

```typescript
interface QuickAction {
  icon: string;
  label: string;
  href: string;
}

const quickActions: QuickAction[] = [
  { icon: '⚙️', label: 'Profile Settings', href: '/dashboard/profile-settings' }
  // REMOVED: Security Settings, Membership Status (now consolidated into Profile Settings)
];

// Mantine v7 implementation
<Box className="quick-actions" mt="xl">
  <Title order={3}>Quick Actions</Title>
  <Group spacing="md" mt="md" position="center">
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

/* Tab navigation - clean readability */
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

#### 2. Tab Active Indicator
```css
.tab[data-active] {
    border-bottom: 3px solid var(--color-burgundy);
    color: var(--color-burgundy);
    font-weight: 700;
    transition: all 0.3s ease;
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

    .horizontal-tabs {
        overflow-x: auto;              /* Scrollable tabs */
        scrollbar-width: none;         /* Hide scrollbar */
        -webkit-overflow-scrolling: touch;
    }

    .horizontal-tabs::-webkit-scrollbar {
        display: none;                 /* Hide scrollbar */
    }

    .tab {
        flex-shrink: 0;                /* Prevent tab compression */
        min-width: fit-content;
    }

    .main-content {
        padding: var(--space-lg) 20px;
    }

    .events-list {
        gap: var(--space-md);
    }

    .profile-sections {
        padding: var(--space-md);
    }
}
```

#### Component Adaptations
- **Horizontal Tabs**: Scrollable on mobile with partial visibility hint
- **Event Cards**: Grid → Single column stack
- **Social Ticket Buttons**: Full-width on mobile
- **Profile Settings Sections**: Increased padding, full-width fields
- **Quick Actions**: Row → Vertical stack
- **Button Groups**: Horizontal → Vertical stack
- **Touch Targets**: Minimum 44px × 44px

### Touch Optimization
- **Minimum Touch Targets**: 44px × 44px
- **Button Padding**: Increased on mobile
- **Scroll Areas**: Native momentum scrolling
- **Tab Scrolling**: Horizontal scroll with partial visibility hint

## Accessibility Implementation

### WCAG 2.1 AA Compliance

#### Color Contrast
- **Primary text**: 7:1 ratio (charcoal on cream)
- **Secondary text**: 4.5:1 ratio (stone on ivory)
- **Interactive elements**: 4.5:1 minimum
- **Status indicators**: Color + text/icon combinations
- **Tab active state**: 3px bottom border ensures visibility

#### Keyboard Navigation
```css
/* Focus states for all interactive elements */
.tab:focus,
.btn:focus {
    outline: 3px solid rgba(136, 1, 36, 0.5);
    outline-offset: 2px;
}

/* Tab keyboard navigation */
.tab-list[role="tablist"] {
    /* Arrow keys for tab navigation */
    /* Tab key to exit tab list */
    /* Enter/Space to select tab */
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

<!-- Horizontal tabs with ARIA -->
<Tabs aria-label="Dashboard sections" role="tablist">
  <Tabs.Tab value="dashboard" aria-controls="dashboard-panel" role="tab">
    Dashboard
  </Tabs.Tab>
</Tabs>

<!-- Profile Settings sections with clear headings -->
<section aria-labelledby="profile-info-heading">
  <h2 id="profile-info-heading">Profile Information</h2>
  <!-- Fields -->
</section>

<!-- Event filtering tabs -->
<Tabs aria-label="Event filtering tabs" role="tablist">
  <Tabs.Tab value="upcoming" aria-controls="upcoming-panel" role="tab">
    Upcoming Events
  </Tabs.Tab>
</Tabs>

<!-- Event cards with proper headings -->
<article aria-labelledby="event-title-1">
  <h3 id="event-title-1">Rope Fundamentals Workshop</h3>
  <!-- Event details -->
</article>

<!-- Membership hold modal with ARIA -->
<Modal
  aria-labelledby="hold-modal-title"
  aria-describedby="hold-modal-description"
>
  <h2 id="hold-modal-title">Put Membership On Hold</h2>
  <p id="hold-modal-description">Warning text...</p>
</Modal>
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
- **Profile Settings**: Single fetch for all user data
- **TanStack Query**: Cache events data, invalidate on mutations
- **Optimistic Updates**: Update UI immediately, rollback on error

## Implementation Roadmap

### Phase 1: Horizontal Tab Navigation (Week 1)
- [ ] Replace left sidebar with horizontal tabs component
- [ ] Implement tab navigation with React Router integration
- [ ] Setup active tab state management
- [ ] Create mobile scrollable tabs behavior
- [ ] Test keyboard navigation and ARIA compliance
- [ ] Verify accessibility with screen readers

### Phase 2: Dashboard Landing Page (Week 1-2)
- [ ] Setup Design System v7 CSS variables
- [ ] Create welcome section with underline
- [ ] Build event card component with social ticket indicators
- [ ] Implement upcoming events preview (max 5)
- [ ] Add prominent "Purchase Ticket" buttons for social events
- [ ] Update quick actions section (single button)
- [ ] Setup responsive grid system

### Phase 3: Events Page (Week 2)
- [ ] Create tab filtering component (3 tabs only - NO waitlist)
- [ ] Implement event filtering logic
- [ ] Build full event history display
- [ ] Add prominent social event ticket indicators
- [ ] Add empty states for each tab
- [ ] Connect "View All Events" link from dashboard
- [ ] Test tab switching performance

### Phase 4: Profile Settings Consolidation (Week 3)
- [ ] Create single Profile Settings page layout
- [ ] Implement Section 1: Profile Information with Save button
- [ ] Implement Section 2: Change Password with validation
- [ ] Implement Section 3: Vetting Status (read-only display)
- [ ] Add "Put Membership On Hold" button (conditional visibility)
- [ ] Create Membership Hold Modal component
- [ ] Implement hold workflow with API integration
- [ ] Test section scrolling and form submissions

### Phase 5: EventDetailPage Integration (Week 3)
- [ ] Implement navigation to EventDetailPage
- [ ] Setup direct checkout navigation for social events
- [ ] Pass event ID correctly in URL
- [ ] Test browser back button behavior
- [ ] Verify state preservation (active tab)
- [ ] Ensure no duplicate event actions

### Phase 6: Polish & Testing (Week 4)
- [ ] Animation refinements
- [ ] Accessibility testing with screen readers
- [ ] Cross-browser testing
- [ ] Performance optimization
- [ ] Mobile device testing
- [ ] Load testing with many events
- [ ] Social ticket indicator prominence verification
- [ ] Hold modal UX testing

## Quality Assurance Checklist

### Navigation Design
- [ ] Horizontal tabs implemented correctly (3 tabs only)
- [ ] 280px sidebar removed completely
- [ ] Tab navigation uses React Router
- [ ] Active tab clearly indicated with 3px bottom border
- [ ] Mobile scrollable tabs working smoothly
- [ ] Keyboard navigation functional
- [ ] ARIA attributes correct for tabs

### Visual Design
- [ ] Design System v7 colors used consistently
- [ ] Typography hierarchy clear and readable
- [ ] Signature animations implemented correctly (corner morphing)
- [ ] Responsive design works on all target devices
- [ ] Brand consistency maintained throughout
- [ ] No left sidebar or right sidebar
- [ ] Edge-to-edge layout implemented

### Social Event Ticket Indicators
- [ ] "Ticket Purchased" badge LARGER than other badges
- [ ] "Purchase Ticket" button LARGER than "View Details"
- [ ] Ticket indicators more prominent than participation status
- [ ] Direct checkout flow working (skips EventDetailPage)
- [ ] Checkout opens with 1 ticket pre-selected
- [ ] Visual hierarchy clearly prioritizes ticket actions
- [ ] High-contrast colors for ticket CTAs

### Profile Settings Consolidation
- [ ] All three sections visible on single page
- [ ] Section 1 (Profile Information) with Save button
- [ ] Section 2 (Change Password) with validation
- [ ] Section 3 (Vetting Status) read-only display
- [ ] "Put Membership On Hold" button conditional visibility
- [ ] Each section has distinct visual separation
- [ ] No multi-page navigation required

### Membership Hold Workflow
- [ ] Modal opens on button click
- [ ] Warning message prominently displayed
- [ ] Reason textarea required (min 10 chars)
- [ ] Form validation working correctly
- [ ] API saves reason to vetting notes
- [ ] Status updates to "On Hold" on confirmation
- [ ] Button disappears after hold confirmed
- [ ] Cancel button closes modal without changes

### Waitlist Removal
- [ ] "Waitlisted" status removed from all event cards
- [ ] Waitlist tab removed from Events page (3 tabs only)
- [ ] No waitlist badges anywhere in UI
- [ ] Participation status only "Registered" or "Ticketed"
- [ ] Empty states updated (no waitlist references)

### User Experience
- [ ] Dashboard shows welcome + preview (3-5 events max)
- [ ] Events page shows full history with 3 tabs
- [ ] Tab filtering works without page reload
- [ ] "View Details" navigates to EventDetailPage correctly
- [ ] Social events skip detail page for ticket purchase
- [ ] Profile settings accessible without navigation
- [ ] Membership hold workflow clear and intuitive
- [ ] Quick actions shortcut works
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
- [ ] Tab navigation ARIA-compliant
- [ ] Screen reader testing completed
- [ ] Color contrast ratios verified
- [ ] Focus management implemented
- [ ] Skip links available
- [ ] Reduced motion support
- [ ] Modal accessibility correct

### Integration
- [ ] EventDetailPage integration tested
- [ ] Direct checkout navigation working
- [ ] Event ID passing verified
- [ ] Navigation context preserved
- [ ] No duplicate event action logic
- [ ] API endpoints correct
- [ ] Error handling complete
- [ ] Hold reason saves to vetting notes

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

**Implementation Notes**: These Version 3.0 designs provide a complete foundation for the WitchCityRope User Dashboard using Design System v7 with modern horizontal tab navigation. The horizontal tabs reclaim 78% of screen space previously used by the left sidebar, aligning with industry best practices for simple 3-item navigation. The consolidated Profile Settings page merges three separate pages into one, eliminating unnecessary navigation complexity. Prominent social event ticket indicators with direct checkout flow prioritize the critical business goal of ticket sales. All Design System v7 standards are maintained for color, typography, and animations.

**Navigation Research**: Horizontal tab pattern validated by Material Design, Nielsen Norman Group research, and real-world examples from Stripe, Airbnb, and Google Admin. Tabs are the recommended pattern for 2-5 navigation items with excellent mobile responsiveness.

**Profile Consolidation**: Single-page settings approach reduces cognitive load and eliminates multi-page navigation friction. All user settings accessible with simple scrolling instead of multiple clicks.

**Social Event Focus**: Ticket indicators designed as PRIMARY visual elements, significantly larger than other badges and buttons, with direct checkout reducing conversion friction.

**Waitlist Removal**: Complete elimination of waitlist complexity simplifies event participation to two clear states: Registered and Ticketed.

**Next Steps**: Begin Phase 1 implementation with horizontal tab navigation and Mantine v7 component setup. All designs are ready for developer handoff with complete specifications and implementation guidance.

# UI Design Specifications: Conditional "How to Join" Menu Visibility
<!-- Last Updated: 2025-10-04 -->
<!-- Version: 1.0 -->
<!-- Owner: UI Designer Agent -->
<!-- Status: Draft -->

## Design Overview

This feature implements intelligent, context-aware navigation for the "How to Join" menu item with status-specific information displays. The design prioritizes clarity, accessibility, and community-appropriate aesthetics while maintaining the WitchCityRope Design System v7 standards.

**Core Principles**:
- **Contextual Clarity**: Users see only relevant navigation options
- **Status Transparency**: Clear, friendly status communication
- **Mobile-First**: Optimized for all device sizes
- **Accessibility**: WCAG 2.1 AA compliant throughout
- **Brand Consistency**: Maintains Design System v7 aesthetic

## User Personas

Based on vetting status, users fall into distinct personas:

- **Guest/Visitor**: Unauthenticated users exploring the community
- **New Member**: Authenticated users without vetting application
- **Pending Applicant**: Users with applications in progress (Submitted, UnderReview, Interview stages)
- **On-Hold User**: Applications paused for additional information
- **Vetted Member**: Approved community members
- **Denied Applicant**: Applications not approved
- **Withdrawn Applicant**: Users who withdrew their applications

## Design System Integration

### Color Palette (Design System v7)

**Primary Colors**:
```css
--color-burgundy: #880124;        /* Primary brand, borders */
--color-rose-gold: #B76D75;       /* Accents, highlights */
--color-amber: #FFBF00;           /* CTA buttons */
--color-electric: #9D4EDD;        /* Primary CTA */
```

**Status Colors**:
```css
--color-success: #228B22;         /* Approved, positive states */
--color-warning: #DAA520;         /* Pending, needs attention */
--color-error: #DC143C;           /* Denied, blocked states */
--color-info: #4285F4;            /* Informational states */
```

**Neutral Colors**:
```css
--color-charcoal: #2B2B2B;        /* Primary text */
--color-stone: #8B8680;           /* Secondary text */
--color-cream: #FAF6F2;           /* Background */
--color-ivory: #FFF8F0;           /* Card backgrounds */
```

### Typography

```css
--font-heading: 'Montserrat', sans-serif;  /* Titles, labels */
--font-body: 'Source Sans 3', sans-serif;   /* Body text */
```

**Usage**:
- Status box titles: Montserrat 700, 24px, uppercase, 1px letter-spacing
- Status descriptions: Source Sans 3, 16px, 1.7 line-height
- Application numbers: Montserrat 600, 14px, uppercase
- Next steps: Source Sans 3, 16px, 1.7 line-height

### Spacing (Design System v7)

```css
--space-sm: 16px;
--space-md: 24px;
--space-lg: 32px;
--space-xl: 40px;
```

## Wireframes

### 1. Navigation Menu - Conditional Visibility

#### Desktop Navigation (1200px+)

**WITH "How to Join" (Guest, New Member, Pending Applicant, Withdrawn)**:
```
+------------------------------------------------------------------+
| WitchCityRope Logo    Events & Classes    How to Join    Login  |
+------------------------------------------------------------------+
```

**WITHOUT "How to Join" (Vetted, OnHold, Denied)**:
```
+------------------------------------------------------------------+
| WitchCityRope Logo    Events & Classes    Dashboard    [Avatar] |
+------------------------------------------------------------------+
```

**Component Specifications**:
- **Container**: Mantine `Group` with `justify="space-between"`
- **Logo**: Mantine `Box` with Link, nav-underline-animation class
- **Navigation Links**: Mantine `Box` with Link, conditional rendering
- **CTA Button**: HTML button with `.btn .btn-primary` classes

#### Mobile Navigation (< 768px)

**Hamburger Menu - WITH "How to Join"**:
```
+----------------------+
| ☰  WitchCityRope    |
+----------------------+
| [Menu Expanded]      |
|                      |
| Events & Classes     |
| How to Join         ← Shows for applicable users
| Login                |
+----------------------+
```

**Hamburger Menu - WITHOUT "How to Join"**:
```
+----------------------+
| ☰  WitchCityRope    |
+----------------------+
| [Menu Expanded]      |
|                      |
| Events & Classes     |
| Dashboard            |
| Profile Settings     |
| Logout               |
+----------------------+
```

**Component Specifications**:
- **Mobile Menu**: Mantine `Drawer` component
- **Menu Items**: Mantine `Stack` with conditional rendering
- **Burger Icon**: Mantine `Burger` component
- **Menu Links**: Mantine `NavLink` components

### 2. HowToJoin Page - Status Box Variants

#### Guest/Unauthenticated View

```
+------------------------------------------------------------------+
|                         How to Join                               |
|                    Witch City Rope Community                      |
+------------------------------------------------------------------+
|                                                                   |
|  Welcome to Witch City Rope! We're a vetted community focused     |
|  on rope bondage education, practice, and performance.            |
|                                                                   |
|  [Login to Apply]  [Learn More About Vetting]                    |
|                                                                   |
|  ┌────────────────────────────────────────────────────────────┐  |
|  │ Why We Vet Members                                          │  |
|  │                                                              │  |
|  │ Our vetting process ensures a safe, respectful community... │  |
|  └────────────────────────────────────────────────────────────┘  |
|                                                                   |
+------------------------------------------------------------------+
```

**Component Specifications**:
- **Title**: Mantine `Title` order={1}, Montserrat 800
- **Description**: Mantine `Text` size="lg", Source Sans 3
- **Buttons**: HTML buttons with `.btn .btn-primary` classes
- **Info Card**: Mantine `Paper` with border, ivory background

#### New Member View (No Application)

```
+------------------------------------------------------------------+
|                    Vetting Application                            |
+------------------------------------------------------------------+
|                                                                   |
|  [Application Form Component - Existing]                          |
|                                                                   |
+------------------------------------------------------------------+
```

**Component Specifications**:
- Uses existing `VettingApplicationForm` component
- No status box displayed
- Full application form visible

### 3. Status Box Designs - Submitted Status

```
+------------------------------------------------------------------+
| ┌──────────────────────────────────────────────────────────────┐ |
| │ 📋 APPLICATION STATUS: SUBMITTED                              │ |
| ├──────────────────────────────────────────────────────────────┤ |
| │                                                                │ |
| │ Application #: a1b2c3d4  |  Submitted: Oct 4, 2025            │ |
| │ Last Updated: Oct 4, 2025                                      │ |
| │                                                                │ |
| │ Your application is in queue for review. Our vetting          │ |
| │ committee will begin reviewing applications within 3-5        │ |
| │ business days.                                                │ |
| │                                                                │ |
| │ ┌────────────────────────────────────────────────────────┐   │ |
| │ │ Next Steps:                                             │   │ |
| │ │ No action needed - we'll contact you via email when    │   │ |
| │ │ your application enters review.                        │   │ |
| │ └────────────────────────────────────────────────────────┘   │ |
| │                                                                │ |
| │ Estimated review time: 14 days                                │ |
| │                                                                │ |
| └──────────────────────────────────────────────────────────────┘ |
+------------------------------------------------------------------+
```

**Visual Specifications**:
- **Container**: Mantine `Paper` with shadow="md", radius="md", withBorder
- **Border Left**: 6px solid `#4285F4` (info blue)
- **Background**: `rgba(66, 133, 244, 0.05)` (light blue tint)
- **Icon**: `IconClipboard` from `@tabler/icons-react`, size 24, color #4285F4
- **Badge**: Mantine `Badge` variant="filled", color="blue", size="lg"
- **Title**: Montserrat 700, 24px, uppercase, charcoal
- **Application Details**: Montserrat 600, 14px, uppercase, stone (dimmed)
- **Description**: Source Sans 3, 16px, charcoal
- **Next Steps Box**: Mantine `Paper` with border, white background
- **Estimated Time**: Source Sans 3, 14px, italic, stone (dimmed)

**Layout**:
- **Container Padding**: `var(--space-xl)` (40px)
- **Stack Gap**: `var(--space-md)` (24px)
- **Group Spacing**: `var(--space-xl)` (40px) between details
- **Margin Bottom**: `var(--space-xl)` (40px) below status box

### 4. Status Box Designs - UnderReview Status

```
+------------------------------------------------------------------+
| ┌──────────────────────────────────────────────────────────────┐ |
| │ 🔍 APPLICATION STATUS: UNDER REVIEW                           │ |
| ├──────────────────────────────────────────────────────────────┤ |
| │                                                                │ |
| │ Application #: a1b2c3d4  |  Last Updated: Oct 6, 2025         │ |
| │ Submitted: Oct 4, 2025                                         │ |
| │                                                                │ |
| │ Your application is currently being reviewed by our vetting   │ |
| │ committee. References may be contacted soon.                  │ |
| │                                                                │ |
| │ ┌────────────────────────────────────────────────────────┐   │ |
| │ │ Next Steps:                                             │   │ |
| │ │ Watch your email for reference contact notifications   │   │ |
| │ │ or additional information requests.                    │   │ |
| │ └────────────────────────────────────────────────────────┘   │ |
| │                                                                │ |
| │ Estimated time remaining: 7-10 days                           │ |
| │                                                                │ |
| └──────────────────────────────────────────────────────────────┘ |
+------------------------------------------------------------------+
```

**Visual Specifications**:
- **Border Left**: 6px solid `#DAA520` (warning gold)
- **Background**: `rgba(218, 165, 32, 0.05)` (light gold tint)
- **Icon**: `IconSearch` from `@tabler/icons-react`, size 24, color #DAA520
- **Badge**: Mantine `Badge` variant="filled", color="yellow", size="lg"
- **Other specs**: Same as Submitted status

### 5. Status Box Designs - InterviewApproved Status

```
+------------------------------------------------------------------+
| ┌──────────────────────────────────────────────────────────────┐ |
| │ ✅ APPLICATION STATUS: INTERVIEW APPROVED                     │ |
| ├──────────────────────────────────────────────────────────────┤ |
| │                                                                │ |
| │ Application #: a1b2c3d4  |  Last Updated: Oct 10, 2025        │ |
| │ Submitted: Oct 4, 2025                                         │ |
| │                                                                │ |
| │ Congratulations! Your application has been approved for the   │ |
| │ interview phase. Someone will contact you soon to schedule    │ |
| │ your interview.                                               │ |
| │                                                                │ |
| │ ┌────────────────────────────────────────────────────────┐   │ |
| │ │ Next Steps:                                             │   │ |
| │ │ Wait for email with interview scheduling options.       │   │ |
| │ │ Respond promptly to schedule your interview.           │   │ |
| │ └────────────────────────────────────────────────────────┘   │ |
| │                                                                │ |
| └──────────────────────────────────────────────────────────────┘ |
+------------------------------------------------------------------+
```

**Visual Specifications**:
- **Border Left**: 6px solid `#228B22` (success green)
- **Background**: `rgba(34, 139, 34, 0.05)` (light green tint)
- **Icon**: `IconCheck` from `@tabler/icons-react`, size 24, color #228B22
- **Badge**: Mantine `Badge` variant="filled", color="green", size="lg"

### 6. Status Box Designs - OnHold Status

```
+------------------------------------------------------------------+
| ┌──────────────────────────────────────────────────────────────┐ |
| │ ⏸️ APPLICATION STATUS: ON HOLD                                │ |
| ├──────────────────────────────────────────────────────────────┤ |
| │                                                                │ |
| │ Application #: a1b2c3d4  |  Last Updated: Oct 8, 2025         │ |
| │ Submitted: Oct 4, 2025                                         │ |
| │                                                                │ |
| │ Your application is currently on hold. This typically means   │ |
| │ we need additional information or there's a temporary pause   │ |
| │ in processing.                                                │ |
| │                                                                │ |
| │ ┌────────────────────────────────────────────────────────┐   │ |
| │ │ Next Steps:                                             │   │ |
| │ │ Please check your email for any requests from our       │   │ |
| │ │ vetting committee. If you have questions, contact:      │   │ |
| │ │ vetting@witchcityrope.com                              │   │ |
| │ └────────────────────────────────────────────────────────┘   │ |
| │                                                                │ |
| │ You cannot submit a new application while your current        │ |
| │ application is on hold.                                       │ |
| │                                                                │ |
| └──────────────────────────────────────────────────────────────┘ |
+------------------------------------------------------------------+
```

**Visual Specifications**:
- **Border Left**: 6px solid `#DC143C` (error red)
- **Background**: `rgba(220, 20, 60, 0.05)` (light red tint)
- **Icon**: `IconPause` from `@tabler/icons-react`, size 24, color #DC143C
- **Badge**: Mantine `Badge` variant="filled", color="red", size="lg"
- **Warning Text**: Montserrat 600, 14px, color error red

### 7. Status Box Designs - Approved Status

```
+------------------------------------------------------------------+
| ┌──────────────────────────────────────────────────────────────┐ |
| │ ✅ APPLICATION STATUS: APPROVED                               │ |
| ├──────────────────────────────────────────────────────────────┤ |
| │                                                                │ |
| │ Application #: a1b2c3d4  |  Approved: Oct 15, 2025            │ |
| │ Submitted: Sep 20, 2025                                        │ |
| │                                                                │ |
| │ Congratulations! You are now a vetted member of Witch City    │ |
| │ Rope. Welcome to the community!                               │ |
| │                                                                │ |
| │ ┌────────────────────────────────────────────────────────┐   │ |
| │ │ Next Steps:                                             │   │ |
| │ │ Access member-only resources via your dashboard.       │   │ |
| │ │ Explore upcoming events and classes.                   │   │ |
| │ └────────────────────────────────────────────────────────┘   │ |
| │                                                                │ |
| │ [Go to Dashboard]                                             │ |
| │                                                                │ |
| └──────────────────────────────────────────────────────────────┘ |
+------------------------------------------------------------------+
```

**Visual Specifications**:
- **Border Left**: 6px solid `#228B22` (success green)
- **Background**: `rgba(34, 139, 34, 0.05)` (light green tint)
- **Icon**: `IconCheck` from `@tabler/icons-react`, size 24, color #228B22
- **Badge**: Mantine `Badge` variant="filled", color="green", size="lg"
- **CTA Button**: HTML button with `.btn .btn-primary` classes

### 8. Status Box Designs - Denied Status

```
+------------------------------------------------------------------+
| ┌──────────────────────────────────────────────────────────────┐ |
| │ ❌ APPLICATION STATUS: NOT APPROVED                           │ |
| ├──────────────────────────────────────────────────────────────┤ |
| │                                                                │ |
| │ Application #: a1b2c3d4  |  Decision: Oct 15, 2025            │ |
| │ Submitted: Oct 4, 2025                                         │ |
| │                                                                │ |
| │ Your vetting application was not approved at this time.       │ |
| │                                                                │ |
| │ ┌────────────────────────────────────────────────────────┐   │ |
| │ │ Reapplication Policy:                                   │   │ |
| │ │ You may reapply after 6 months from the decision date   │   │ |
| │ │ (April 15, 2026).                                       │   │ |
| │ └────────────────────────────────────────────────────────┘   │ |
| │                                                                │ |
| │ If you have questions about this decision, please contact:    │ |
| │ vetting@witchcityrope.com                                     │ |
| │                                                                │ |
| └──────────────────────────────────────────────────────────────┘ |
+------------------------------------------------------------------+
```

**Visual Specifications**:
- **Border Left**: 6px solid `#DC143C` (error red)
- **Background**: `rgba(220, 20, 60, 0.05)` (light red tint)
- **Icon**: `IconX` from `@tabler/icons-react`, size 24, color #DC143C
- **Badge**: Mantine `Badge` variant="filled", color="red", size="lg"
- **Policy Box**: Mantine `Paper` with border, light red background tint

### 9. Mobile Responsive Layouts

#### Mobile Status Box (< 768px)

```
+---------------------------+
| ┌───────────────────────┐ |
| │ 📋 SUBMITTED          │ |
| ├───────────────────────┤ |
| │ App #: a1b2c3d4       │ |
| │ Submitted: Oct 4      │ |
| │                       │ |
| │ Your application is   │ |
| │ in queue for review.  │ |
| │                       │ |
| │ ┌─────────────────┐  │ |
| │ │ Next Steps:     │  │ |
| │ │ No action needed│  │ |
| │ └─────────────────┘  │ |
| │                       │ |
| │ ~14 days remaining    │ |
| └───────────────────────┘ |
+---------------------------+
```

**Mobile Modifications**:
- **Padding**: Reduced to `var(--space-md)` (24px)
- **Font Sizes**: Title 20px, body 14px
- **Group Layout**: Stack vertically instead of horizontal
- **Icon Size**: 20px instead of 24px
- **Badge**: Size "md" instead of "lg"

## Mantine Components Used

### Navigation Component

| Component | Purpose | Configuration |
|-----------|---------|--------------|
| **Group** | Container for nav items | `justify="space-between"`, `align="center"` |
| **Box** | Individual nav links | With Link wrapper, conditional rendering |
| **Drawer** | Mobile menu | `position="right"`, `size="xs"` |
| **Burger** | Mobile menu toggle | `opened` state controlled |
| **NavLink** | Mobile menu items | Icon support, active state |
| **Stack** | Menu item container | `gap="sm"` |

### Status Box Component

| Component | Purpose | Configuration |
|-----------|---------|--------------|
| **Paper** | Status box container | `shadow="md"`, `radius="md"`, `withBorder`, custom borderLeft |
| **Stack** | Vertical layout | `gap="md"` |
| **Group** | Horizontal layout | `justify="space-between"`, `gap="xl"` |
| **Title** | Status heading | `order={3}`, Montserrat font |
| **Badge** | Status indicator | `variant="filled"`, status-specific color, `size="lg"` |
| **Text** | Content text | `size="md"`, `c="dimmed"` for metadata |
| **Paper** (nested) | Next steps box | `p="md"`, `withBorder`, white background |

### HowToJoin Page Component

| Component | Purpose | Configuration |
|-----------|---------|--------------|
| **Container** | Page wrapper | `size="lg"`, max-width 1200px |
| **Title** | Page title | `order={1}`, Montserrat 800 |
| **LoadingOverlay** | Loading state | `visible` prop controlled |
| **Stack** | Page layout | `gap="xl"` |

## Interaction Patterns

### Menu Visibility Logic

```typescript
// Pseudocode - NOT implementation
const showHowToJoin = () => {
  if (!isAuthenticated) return true; // Always show for guests
  if (!vettingStatus?.hasApplication) return true; // Show for users without apps

  const hideStatuses = ['OnHold', 'Approved', 'Denied'];
  if (hideStatuses.includes(vettingStatus.application.status)) {
    return false; // Hide for these statuses
  }

  return true; // Show for all other statuses
};
```

**UX Flow**:
1. User navigates to site
2. Navigation component renders
3. `useMenuVisibility` hook determines visibility
4. Menu item conditionally rendered
5. Menu updates automatically when status changes

### Status Box Display Logic

```typescript
// Pseudocode - NOT implementation
const renderStatusBox = (status: VettingStatus) => {
  const statusConfigs = {
    Submitted: { color: '#4285F4', icon: IconClipboard, badge: 'blue' },
    UnderReview: { color: '#DAA520', icon: IconSearch, badge: 'yellow' },
    InterviewApproved: { color: '#228B22', icon: IconCheck, badge: 'green' },
    OnHold: { color: '#DC143C', icon: IconPause, badge: 'red' },
    Approved: { color: '#228B22', icon: IconCheck, badge: 'green' },
    Denied: { color: '#DC143C', icon: IconX, badge: 'red' }
  };

  return <VettingStatusBox config={statusConfigs[status]} />;
};
```

**UX Flow**:
1. User navigates to `/how-to-join`
2. Page loads vetting status from API
3. Status box renders with appropriate variant
4. Form shows or hides based on status
5. Next steps guide user appropriately

### Loading States

**Navigation Menu**:
- Show menu item while loading (fail-open approach)
- No loading spinner in navigation
- Prevents UI "jumping" during status check

**HowToJoin Page**:
- Mantine `LoadingOverlay` visible during API fetch
- Full-page overlay with spinner
- Prevents content flash before data loads

### Error Handling

**API Failure**:
- Menu item SHOWS on error (fail-open)
- Error logged to console silently
- User experience not blocked
- Page displays friendly message if error persists

**Network Timeout**:
- Menu item SHOWS (fail-open)
- Automatic retry in background
- User can still navigate to page

## Responsive Breakpoints

**Mobile (xs)**: 0px - 575px
- Single column layouts
- Stacked navigation items
- Reduced padding (20px)
- Smaller typography scale
- Full-width buttons

**Small (sm)**: 576px - 767px
- Tablet portrait
- Compact status boxes
- Touch-optimized spacing

**Medium (md)**: 768px - 991px
- Tablet landscape
- Desktop navigation appears
- Full status box layout

**Large (lg)**: 992px+
- Desktop full width
- Maximum readability
- Optimal spacing

## Accessibility Requirements

### Keyboard Navigation

- **Tab Order**: Logical flow through menu items
- **Focus Indicators**: Visible focus states on all interactive elements
- **Skip Links**: Skip to main content link for screen readers
- **Menu Toggle**: Enter/Space to open mobile menu

### Screen Reader Support

- **ARIA Labels**:
  - `aria-label="Main navigation"` on nav container
  - `aria-current="page"` for active menu items
  - `aria-expanded` state on mobile menu
  - `aria-label="Application status information"` on status boxes

- **Semantic HTML**:
  - `<nav>` for navigation container
  - `<article>` for status boxes
  - Proper heading hierarchy (h1 → h2 → h3)
  - `<strong>` for emphasis, not just styling

### Color Contrast

**WCAG AA Compliance (4.5:1)**:
- Text on cream background: #2B2B2B (charcoal) = 12.6:1 ✅
- Status titles on backgrounds: All >= 4.5:1 ✅
- Badge text contrast: All >= 4.5:1 ✅
- Next steps box: Black on white = 21:1 ✅

### Touch Targets

**Minimum Size**: 44px × 44px
- Navigation links: 48px height
- Mobile menu items: 48px height
- Buttons: 48px minimum height
- Status box clickable areas: 48px minimum

### Reduced Motion Support

```css
@media (prefers-reduced-motion: reduce) {
  * {
    animation-duration: 0.01ms !important;
    animation-iteration-count: 1 !important;
    transition-duration: 0.01ms !important;
  }
}
```

## Component Styling Specifications

### Status Box Base Styles

```typescript
// Base Paper styling - NOT implementation code
const statusBoxBaseStyle = {
  shadow: 'md',
  radius: 'md',
  withBorder: true,
  p: 'xl',
  mb: 'xl',
  style: {
    borderLeft: '6px solid [STATUS_COLOR]',
    backgroundColor: '[STATUS_BACKGROUND]'
  }
};
```

### Status-Specific Configurations

```typescript
// Status configurations - NOT implementation code
interface StatusConfig {
  color: string;           // Border and icon color
  backgroundColor: string; // Box background tint
  icon: TablerIcon;        // Icon component
  badgeColor: string;      // Mantine badge color
  title: string;           // Display title
}

const statusConfigs: Record<VettingStatus, StatusConfig> = {
  Submitted: {
    color: '#4285F4',
    backgroundColor: 'rgba(66, 133, 244, 0.05)',
    icon: IconClipboard,
    badgeColor: 'blue',
    title: 'Application Submitted'
  },
  UnderReview: {
    color: '#DAA520',
    backgroundColor: 'rgba(218, 165, 32, 0.05)',
    icon: IconSearch,
    badgeColor: 'yellow',
    title: 'Under Review'
  },
  InterviewApproved: {
    color: '#228B22',
    backgroundColor: 'rgba(34, 139, 34, 0.05)',
    icon: IconCheck,
    badgeColor: 'green',
    title: 'Interview Approved'
  },
  OnHold: {
    color: '#DC143C',
    backgroundColor: 'rgba(220, 20, 60, 0.05)',
    icon: IconPause,
    badgeColor: 'red',
    title: 'Application On Hold'
  },
  Approved: {
    color: '#228B22',
    backgroundColor: 'rgba(34, 139, 34, 0.05)',
    icon: IconCheck,
    badgeColor: 'green',
    title: 'Application Approved'
  },
  Denied: {
    color: '#DC143C',
    backgroundColor: 'rgba(220, 20, 60, 0.05)',
    icon: IconX,
    badgeColor: 'red',
    title: 'Application Not Approved'
  }
};
```

### Next Steps Box Styling

```typescript
// Next steps Paper - NOT implementation code
const nextStepsBoxStyle = {
  p: 'md',
  withBorder: true,
  style: {
    backgroundColor: 'white',
    borderColor: '#E0E0E0'
  }
};
```

### Typography Styling

```css
/* Status box title */
.status-box-title {
  font-family: 'Montserrat', sans-serif;
  font-weight: 700;
  font-size: 24px;
  text-transform: uppercase;
  letter-spacing: 1px;
  color: var(--color-charcoal);
}

/* Application details */
.status-box-details {
  font-family: 'Montserrat', sans-serif;
  font-weight: 600;
  font-size: 14px;
  text-transform: uppercase;
  color: var(--color-stone);
}

/* Status description */
.status-box-description {
  font-family: 'Source Sans 3', sans-serif;
  font-size: 16px;
  line-height: 1.7;
  color: var(--color-charcoal);
}

/* Next steps heading */
.next-steps-heading {
  font-family: 'Montserrat', sans-serif;
  font-weight: 600;
  font-size: 14px;
  color: var(--color-charcoal);
  margin-bottom: 8px;
}

/* Next steps content */
.next-steps-content {
  font-family: 'Source Sans 3', sans-serif;
  font-size: 14px;
  line-height: 1.7;
  color: var(--color-charcoal);
}
```

## Mobile-First Considerations

### Touch-Friendly Design

- **Navigation Links**: 48px height minimum for easy tapping
- **Mobile Menu**: Full-screen drawer for comfortable access
- **Status Boxes**: Adequate padding for readability
- **Buttons**: Full-width on mobile for easy tapping

### Thumb-Zone Optimization

- **Mobile Menu**: Drawer from right for thumb reach
- **CTA Buttons**: Positioned within thumb zone
- **Navigation Items**: Vertically stacked for easy access
- **Status Box Actions**: Full-width buttons at bottom

### Mobile Navigation Flow

```
┌──────────────────────┐
│ User opens mobile    │
│ menu (tap burger)    │
└──────┬───────────────┘
       │
       ▼
┌──────────────────────┐
│ Drawer slides from   │
│ right                │
└──────┬───────────────┘
       │
       ▼
┌──────────────────────┐
│ Menu items render    │
│ conditionally        │
└──────┬───────────────┘
       │
       ├─ Guest: Show "How to Join"
       ├─ New Member: Show "How to Join"
       ├─ Pending: Show "How to Join"
       ├─ Vetted: Hide "How to Join"
       ├─ OnHold: Hide "How to Join"
       └─ Denied: Hide "How to Join"
```

## Community-Specific Design Patterns

### Safety-Conscious Design

- **Clear Status Communication**: No ambiguous language
- **Contact Information Prominent**: Easy access to vetting team
- **Transparent Process**: Status descriptions explain what's happening
- **Respectful Tone**: Community-appropriate language throughout

### Privacy Protection

- **No Public Status Display**: Status only visible to applicant
- **Secure Navigation**: Menu visibility based on authentication
- **Minimal Data Exposure**: Only necessary status information shown
- **Safe Error Handling**: No sensitive data in error messages

### Inclusive Language

- **Welcoming Tone**: "Welcome to the community" vs "Congratulations"
- **Clear Expectations**: "Next steps" provide actionable guidance
- **Supportive Messaging**: "We're here to help" vs "Application rejected"
- **Respectful Denial**: "Not approved at this time" vs "Denied"

## Quality Checklist

### Design System Compliance

- [x] Uses Design System v7 colors exactly
- [x] Follows typography scale and font families
- [x] Uses spacing system variables
- [x] Maintains brand consistency
- [x] No custom colors outside palette

### Mantine Component Usage

- [x] All components from Mantine v7 library
- [x] Consistent component props and patterns
- [x] Proper TypeScript typing
- [x] Accessibility features enabled
- [x] Mobile-responsive components

### Accessibility Standards

- [x] WCAG 2.1 AA color contrast (4.5:1)
- [x] Keyboard navigation support
- [x] Screen reader compatibility
- [x] Touch target sizes (44px minimum)
- [x] Reduced motion support
- [x] Semantic HTML structure

### Mobile Responsiveness

- [x] Mobile-first design approach
- [x] Responsive breakpoints defined
- [x] Touch-optimized interactions
- [x] Thumb-zone considerations
- [x] Mobile navigation patterns

### User Experience

- [x] Clear visual hierarchy
- [x] Contextual navigation
- [x] Status-appropriate messaging
- [x] Actionable next steps
- [x] Error handling defined

### Community Values

- [x] Safety-conscious design
- [x] Privacy protection
- [x] Inclusive language
- [x] Transparent communication
- [x] Respectful tone throughout

## Implementation Notes

### Component File Structure

```
/apps/web/src/features/vetting/components/
├── VettingStatusBox.tsx              # Main status box component
├── StatusBoxVariants/
│   ├── SubmittedStatusBox.tsx
│   ├── UnderReviewStatusBox.tsx
│   ├── InterviewStatusBox.tsx
│   ├── OnHoldStatusBox.tsx
│   ├── ApprovedStatusBox.tsx
│   └── DeniedStatusBox.tsx
└── VettingApplicationForm.tsx        # Existing - no changes
```

### Navigation Component Integration

**File**: `/apps/web/src/components/layout/Navigation.tsx`

**Changes**:
- Import `useMenuVisibility` hook
- Conditionally render "How to Join" menu item
- Use existing CSS classes for styling
- Maintain Design System v7 animations

### HowToJoin Page Integration

**File**: `/apps/web/src/pages/HowToJoin.tsx`

**Changes**:
- Import `VettingStatusBox` component
- Display status box for users with applications
- Conditionally show application form
- Handle all status states appropriately

### CSS Class Usage

**Important**: Use existing CSS classes from `/apps/web/src/index.css`:
- `.btn .btn-primary` for CTA buttons
- `.btn .btn-secondary` for secondary buttons
- `.nav-underline-animation` for navigation links
- NO Mantine Button with inline styles
- NO custom button styling

## Design Deliverables

### Required Files

1. **ui-design.md** (this file) - Complete UI specifications ✅
2. **Component mockups** - Reference wireframes in this document ✅
3. **Mantine component mapping** - Documented in tables above ✅
4. **Responsive design specs** - Mobile layouts documented ✅
5. **Accessibility documentation** - WCAG compliance documented ✅

### Handoff Checklist

- [x] All status box variants designed
- [x] Mobile responsive layouts specified
- [x] Mantine components identified
- [x] Color scheme defined with status colors
- [x] Typography hierarchy specified
- [x] Accessibility requirements documented
- [x] Interaction patterns defined
- [x] Component styling specifications complete
- [x] Implementation notes provided
- [x] Quality checklist completed

## Next Steps

1. **React Developer Review**: Validate component specifications and Mantine usage
2. **Technical Implementation**: Build components according to specifications
3. **Accessibility Audit**: Validate WCAG compliance after implementation
4. **Mobile Testing**: Test responsive behavior on real devices
5. **Stakeholder Review**: Demonstrate status box designs for approval

---

**File Location**: `/docs/functional-areas/vetting-system/new-work/2025-10-04-how-to-join-conditional-visibility/design/ui-design.md`
**Created**: 2025-10-04
**Designer**: UI Designer Agent
**Status**: Ready for Implementation Review

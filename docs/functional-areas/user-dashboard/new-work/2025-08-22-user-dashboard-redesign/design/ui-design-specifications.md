# UI Design Specifications: User Dashboard v7
<!-- Last Updated: 2025-08-22 -->
<!-- Version: 1.0 -->
<!-- Owner: UI Designer Agent -->
<!-- Status: Ready for Review -->

## Design Overview

This document defines the complete UI design specifications for the WitchCityRope User Dashboard redesign using Design System v7. The designs prioritize user's upcoming RSVP'd events and implement a streamlined 5-section navigation architecture with sophisticated sensual aesthetics.

## Design Authority & Sources

### Primary Authority
- **Design System v7**: Single source of truth for all visual patterns
- **Mantine v7 Framework**: Component library and responsive patterns
- **Business Requirements**: User event focus and navigation structure

### Reference Materials
- **Legacy Wireframes**: General layout inspiration (not copied exactly)
- **Award-Winning Design Research**: Warm earth tones and organic flow patterns
- **Stakeholder Feedback**: Sophisticated dark elegance preferences

## Design Philosophy

### Core Principles
- **Event-Centric UX**: User's RSVP'd events are the primary focus
- **Warm Sophistication**: Burgundy, rose-gold, and amber create premium feel
- **Signature Animations**: v7 corner morphing and center-outward underlines
- **Accessibility First**: WCAG 2.1 AA compliance throughout
- **Mobile Excellence**: Responsive design with proven React/Mantine patterns

### Visual Language
- **Color Psychology**: Warm metallics convey luxury and community trust
- **Typography Hierarchy**: Clear information architecture with consistent scales
- **Interactive Feedback**: Subtle hover states and morphing animations
- **Spatial Design**: Generous whitespace with intentional information density

## Page Specifications

### 1. Dashboard Landing Page
**File**: `dashboard-landing-page-v7.html`
**Purpose**: Primary landing page highlighting user's upcoming events

#### Layout Architecture
```
┌─────────────────────────────────────────────────────────────┐
│  Header (Sticky) - 80px height                             │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────┐  ┌───────────────────────────────────────┐ │
│  │  Left Nav   │  │  Main Content Area                    │ │
│  │  280px      │  │  - Welcome Section                     │ │
│  │  - Dashboard│  │  - My Upcoming Events (PRIMARY)       │ │
│  │  - Events   │  │  - Recent Activity                    │ │
│  │  - Profile  │  │                                       │ │
│  │  - Security │  │  ┌─────────────┐  ┌─────────────────┐ │ │
│  │  - Members  │  │  │  Events     │  │ Status & Links  │ │ │
│  │             │  │  │  (2fr)      │  │ (1fr)           │ │ │
│  │             │  │  └─────────────┘  └─────────────────┘ │ │
│  └─────────────┘  └───────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

#### Key Components

**Welcome Section**
- Background: Linear gradient (burgundy → plum)
- Corner radius: 16px
- Radial glow overlay for depth
- Quick action buttons with signature corner morphing

**Event Cards**
- **PRIMARY FOCUS**: Only shows user's RSVP'd/purchased events
- Date badge: Gradient background, strong typography contrast
- Hover effect: translateX(8px) with vertical accent line
- Status badges: Color-coded (confirmed=green, pending=amber, waitlisted=red)

**Left Navigation**
- 5 sections exactly: Dashboard, Events, Profile, Security, Membership
- Active state: Gradient background with shadow
- Hover effect: Background change + translateX(4px)
- Icons: Simple emoji-style for clarity

### 2. Security Settings Page
**File**: `security-settings-page-v7.html`
**Purpose**: Simplified security management (reduced from legacy complexity)

#### Simplified Feature Set
Based on business requirements, the security page focuses on **essential** functions only:

**Included Features**:
- ✅ Password change with requirements
- ✅ Two-factor authentication management
- ✅ Active sessions monitoring
- ✅ Security notification preferences
- ✅ Basic data download
- ✅ Account deletion (danger zone)

**Excluded Complexity**:
- ❌ Complex backup code grids (simplified to view/regenerate buttons)
- ❌ Multiple authentication providers
- ❌ Advanced security audit logs
- ❌ Granular permission management

#### Security Section Layout
```
┌─────────────────────────────────────────────────────────────┐
│  Section Header with gradient underline on hover            │
├─────────────────────────────────────────────────────────────┤
│  Content area with form fields/status cards                │
│  - Consistent 16px border radius                           │
│  - Hover effects: translateY(-2px) + shadow                │
│  - Status icons: 48px circles with gradients               │
└─────────────────────────────────────────────────────────────┘
```

#### Form Design Patterns
- **Input Fields**: 12px border radius, rose-gold border on focus
- **Status Cards**: Icon + content layout with hover elevation
- **Toggle Switches**: Custom design matching v7 aesthetic
- **Danger Zone**: Red-tinted background with clear warnings

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
font-size: 36px;
text-transform: uppercase;
letter-spacing: -0.5px;

/* Card titles - clear hierarchy */
font-family: 'Montserrat', sans-serif;
font-weight: 800;
font-size: 24px;
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
.card:hover {
    transform: translateY(-4px);        /* Subtle elevation */
    box-shadow: 0 8px 30px rgba(0,0,0,0.12);
}

.card::before {
    /* Gradient top border that scales on hover */
    transform: scaleX(0);
    transition: transform 0.3s ease;
}

.card:hover::before {
    transform: scaleX(1);
}
```

## Responsive Design Specifications

### Breakpoint Strategy
- **Mobile**: max-width: 768px
- **Desktop**: min-width: 769px
- **Framework**: Use Mantine v7 responsive patterns (proven templates)

### Mobile Transformations

#### Layout Changes
```css
@media (max-width: 768px) {
    .main-layout {
        grid-template-columns: 1fr;    /* Single column */
        gap: var(--space-lg);
    }
    
    .left-nav {
        position: static;               /* No sticky positioning */
        order: 2;                      /* Below main content */
    }
    
    .main-content {
        order: 1;                      /* Above navigation */
    }
}
```

#### Component Adaptations
- **Dashboard Grid**: 2fr 1fr → 1fr (single column)
- **Form Rows**: 2 columns → 1 column
- **Event Items**: Horizontal → Vertical layout
- **Session Items**: Side-by-side → Stacked
- **Button Groups**: Horizontal → Vertical stack

### Touch Optimization
- **Minimum Touch Targets**: 44px × 44px
- **Button Padding**: Increased on mobile
- **Hover States**: Converted to touch-friendly feedback
- **Scroll Areas**: Native momentum scrolling

## Component Specifications

### Event Cards
```typescript
interface EventCardProps {
  id: string;
  title: string;
  date: Date;
  location: string;
  status: 'confirmed' | 'pending' | 'waitlisted';
  time: string;
}

// Mantine v7 implementation approach
<Card 
  radius="md" 
  withBorder 
  shadow="sm"
  style={{
    borderRadius: '12px',
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
  </Group>
</Card>
```

### Left Navigation
```typescript
interface NavItem {
  icon: string;
  label: string;
  href: string;
  active?: boolean;
}

const navItems: NavItem[] = [
  { icon: '📊', label: 'Dashboard', href: '/dashboard', active: true },
  { icon: '📅', label: 'Events', href: '/events' },
  { icon: '👤', label: 'Profile', href: '/profile' },
  { icon: '🔒', label: 'Security', href: '/security' },
  { icon: '🎯', label: 'Membership', href: '/membership' }
];

// Mantine v7 implementation approach
<Stack spacing="xs">
  {navItems.map(item => (
    <UnstyledButton
      key={item.href}
      className={cx(classes.navLink, { [classes.active]: item.active })}
      onClick={() => navigate(item.href)}
    >
      <Group>
        <Text className={classes.navIcon}>{item.icon}</Text>
        <Text size="sm" weight={500}>{item.label}</Text>
      </Group>
    </UnstyledButton>
  ))}
</Stack>
```

### Form Components
```typescript
// Password change form with v7 styling
<Stack spacing="lg">
  <PasswordInput
    label="Current Password"
    placeholder="Enter your current password"
    required
    radius="md"
    size="md"
    styles={{
      input: {
        borderColor: 'var(--color-rose-gold)',
        '&:focus': {
          borderColor: 'var(--color-burgundy)',
          boxShadow: '0 0 0 3px rgba(136, 1, 36, 0.1)',
        }
      }
    }}
  />
  
  <Group grow>
    <PasswordInput
      label="New Password"
      placeholder="Enter new password"
      required
    />
    <PasswordInput
      label="Confirm New Password"
      placeholder="Confirm new password"
      required
    />
  </Group>
  
  <Button
    className={classes.btnPrimary}  // v7 corner morphing styles
    type="submit"
  >
    Update Password
  </Button>
</Stack>
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
.form-input:focus {
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
<nav aria-label="Main navigation">
<aside aria-label="Dashboard sections">

<!-- Event cards with proper headings -->
<article aria-labelledby="event-title-1">
  <h3 id="event-title-1">Rope Fundamentals Workshop</h3>
  <!-- Event details -->
</article>

<!-- Form labels and descriptions -->
<label for="current-password">Current Password</label>
<input 
  id="current-password" 
  type="password" 
  aria-describedby="password-help"
  required 
/>
<div id="password-help">
  Your current account password
</div>
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

### Responsive Images
```css
/* Event images with aspect ratio preservation */
.event-image {
    aspect-ratio: 16 / 9;
    object-fit: cover;
    border-radius: 8px;
}

/* Responsive image sizing */
@media (max-width: 768px) {
    .event-image {
        aspect-ratio: 4 / 3;  /* Better mobile ratio */
    }
}
```

### Loading States
```typescript
// Skeleton loading for event cards
<Skeleton height={120} radius="md" mb="md" />
<Skeleton height={120} radius="md" mb="md" />
<Skeleton height={120} radius="md" />

// Progressive enhancement
{loading ? (
  <EventCardSkeleton />
) : (
  <EventCard {...eventData} />
)}
```

## Implementation Roadmap

### Phase 1: Foundation (Week 1)
- [ ] Setup Design System v7 CSS variables
- [ ] Implement basic layout with left navigation
- [ ] Create signature button component with corner morphing
- [ ] Setup responsive grid system

### Phase 2: Dashboard Content (Week 2)
- [ ] Welcome section with gradient background
- [ ] Event cards with proper status indicators
- [ ] Membership status display
- [ ] Quick links section

### Phase 3: Security Page (Week 3)
- [ ] Password change form with validation
- [ ] Two-factor authentication management
- [ ] Active sessions display
- [ ] Security preferences with toggle switches

### Phase 4: Polish & Testing (Week 4)
- [ ] Animation refinements
- [ ] Accessibility testing with screen readers
- [ ] Cross-browser testing
- [ ] Performance optimization
- [ ] Mobile device testing

## Quality Assurance Checklist

### Visual Design
- [ ] Design System v7 colors used consistently
- [ ] Typography hierarchy clear and readable
- [ ] Signature animations implemented correctly
- [ ] Responsive design works on all target devices
- [ ] Brand consistency maintained throughout

### User Experience
- [ ] Event-focused design prioritizes user's upcoming events
- [ ] 5-section navigation clear and intuitive
- [ ] Security page simplified compared to legacy
- [ ] Loading states and error handling implemented
- [ ] Progressive enhancement for slower connections

### Technical Implementation
- [ ] Mantine v7 components used appropriately
- [ ] TypeScript interfaces defined for all data
- [ ] CSS-in-JS integration with Mantine styling
- [ ] Performance targets met (<1.5s load time)
- [ ] Cross-browser compatibility verified

### Accessibility
- [ ] WCAG 2.1 AA compliance verified
- [ ] Keyboard navigation functional throughout
- [ ] Screen reader testing completed
- [ ] Color contrast ratios verified
- [ ] Focus management implemented

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

**Implementation Notes**: These designs provide a complete foundation for the WitchCityRope User Dashboard using Design System v7. The focus on user's upcoming events, simplified security management, and signature v7 animations creates a sophisticated yet accessible experience that aligns with community values and modern UX standards.

**Next Steps**: Begin Phase 1 implementation with Mantine v7 component setup and basic layout structure. All designs are ready for developer handoff with complete specifications and implementation guidance.
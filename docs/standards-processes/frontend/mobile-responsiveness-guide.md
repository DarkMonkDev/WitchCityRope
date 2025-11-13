# Mobile Responsiveness Guide for WitchCityRope Frontend Developers
<!-- Last Updated: 2025-11-12 -->
<!-- Version: 1.1 -->
<!-- Owner: Librarian Agent -->
<!-- Status: Active -->

## Executive Summary

**This guide is MANDATORY reading for all React developers before implementing ANY component.**

Mobile responsiveness is the #1 quality problem in our codebase. This guide consolidates best practices from extensive research and provides actionable patterns for building mobile-first React components.

### Quick Reference Checklist

Before committing any component:
- [ ] Typography scales fluidly using CSS clamp() (H1: 28px mobile → 48px desktop)
- [ ] Layouts adapt using Mantine Grid with responsive spans
- [ ] Touch targets meet 44x44px minimum (iOS) / 48x48px (Android)
- [ ] Mobile navigation works correctly (hamburger menu, body scroll lock)
- [ ] Tested on mobile viewports (320px, 375px, 768px minimum)
- [ ] No horizontal scrolling on mobile devices
- [ ] Fixed-width elements (380px) become fluid on mobile
- [ ] Global mobile CSS patterns applied (list styling, line-height optimization)

### Common Mobile Issues to Avoid

❌ **WRONG**: Fixed font sizes too large for mobile
```tsx
<Title style={{ fontSize: '48px' }}>Event Title</Title>
```

✅ **CORRECT**: Fluid typography with clamp()
```tsx
<Title style={{ fontSize: 'var(--font-size-h1)' }}>Event Title</Title>
// CSS: --font-size-h1: clamp(1.75rem, 1.11vw + 1.39rem, 3rem);
```

❌ **WRONG**: Desktop-only grid layout
```tsx
<div style={{ display: 'grid', gridTemplateColumns: '1fr 380px' }}>
```

✅ **CORRECT**: Responsive grid with Mantine
```tsx
<Grid gutter="xl">
  <Grid.Col span={{ base: 12, md: 8 }}>{/* Main */}</Grid.Col>
  <Grid.Col span={{ base: 12, md: 4 }}>{/* Sidebar */}</Grid.Col>
</Grid>
```

---

## Part 1: Typography Standards

### Why Typography Matters on Mobile

**Current Problem**: Event detail page H1 at 48px consumes excessive mobile viewport space, forcing unnecessary scrolling. Users on phones at community events struggle with readability.

**Solution**: Fluid typography that scales smoothly from mobile to desktop using CSS clamp().

### CSS Clamp() Implementation

**The Pattern**:
```css
/* Syntax: clamp(minimum, preferred, maximum) */
font-size: clamp(1.75rem, 1.11vw + 1.39rem, 3rem);
```

**Calculation Formula**:
```
Viewport coefficient (v) = 100 × (max-size - min-size) / (max-breakpoint - min-breakpoint)
Relative value (r) in rem = (min-bp × max-size - max-bp × min-size) / (min-bp - max-bp)
Result: clamp(min-rem, v + r rem, max-rem)
```

**Tools**: Use [Utopia Fluid Typography Calculator](https://utopia.fyi/type/calculator) for accurate clamp() generation.

### H1-H6 Mobile Size Recommendations

| Element | Mobile | Tablet | Desktop | CSS Clamp |
|---------|--------|--------|---------|-----------|
| H1 | 28px (1.75rem) | 36px (2.25rem) | 48px (3rem) | `clamp(1.75rem, 1.11vw + 1.39rem, 3rem)` |
| H2 | 24px (1.5rem) | 30px (1.875rem) | 36px (2.25rem) | `clamp(1.5rem, 1.11vw + 1.14rem, 2.25rem)` |
| H3 | 20px (1.25rem) | 24px (1.5rem) | 28px (1.75rem) | `clamp(1.25rem, 0.74vw + 1.02rem, 1.75rem)` |
| H4 | 18px (1.125rem) | 21px (1.31rem) | 24px (1.5rem) | `clamp(1.125rem, 0.56vw + 0.95rem, 1.5rem)` |
| H5 | 16px (1rem) | 18px (1.125rem) | 20px (1.25rem) | `clamp(1rem, 0.37vw + 0.88rem, 1.25rem)` |
| H6 | 16px (1rem) | 17px (1.06rem) | 18px (1.125rem) | `clamp(1rem, 0.19vw + 0.94rem, 1.125rem)` |
| Body | 16px (1rem) | 16px (1rem) | 17px (1.06rem) | `clamp(1rem, 0.19vw + 0.94rem, 1.06rem)` |

**Critical**: Never use body text smaller than 16px on mobile - prevents iOS Safari zoom on form focus.

### Line Height Standards

**Mobile Readability**:
```css
:root {
  --line-height-h1: clamp(1.2, 0.05vw + 1.18, 1.3);
  --line-height-h2: clamp(1.3, 0.05vw + 1.28, 1.4);
  --line-height-h3: 1.4;
  --line-height-body: 1.6;
}
```

**Rules**:
- H1-H2: Tighter line height (1.2-1.4) for large headings
- H3-H6: Medium line height (1.4-1.55) for readability
- Body text: Generous line height (1.6) for comfortable reading on mobile

### Implementation with Mantine UI

**Step 1: Define CSS Variables** (`/apps/web/src/index.css`):
```css
:root {
  /* Fluid Typography Scale (320px → 1400px viewports) */
  --font-size-h1: clamp(1.75rem, 1.11vw + 1.39rem, 3rem);      /* 28px → 48px */
  --font-size-h2: clamp(1.5rem, 1.11vw + 1.14rem, 2.25rem);    /* 24px → 36px */
  --font-size-h3: clamp(1.25rem, 0.74vw + 1.02rem, 1.75rem);   /* 20px → 28px */
  --font-size-h4: clamp(1.125rem, 0.56vw + 0.95rem, 1.5rem);   /* 18px → 24px */
  --font-size-h5: clamp(1rem, 0.37vw + 0.88rem, 1.25rem);      /* 16px → 20px */
  --font-size-h6: clamp(1rem, 0.19vw + 0.94rem, 1.125rem);     /* 16px → 18px */

  /* Line height scales */
  --line-height-h1: clamp(1.2, 0.05vw + 1.18, 1.3);
  --line-height-h2: clamp(1.3, 0.05vw + 1.28, 1.4);
  --line-height-body: 1.6;
}

/* Apply to HTML elements */
h1 {
  font-size: 2.25rem; /* Fallback: 36px average */
  font-size: var(--font-size-h1); /* Modern: fluid scaling */
  line-height: var(--line-height-h1);
}

h2 {
  font-size: 1.875rem; /* Fallback */
  font-size: var(--font-size-h2);
  line-height: var(--line-height-h2);
}

/* ... h3-h6 ... */
```

**Step 2: Integrate with Mantine Theme** (`/apps/web/src/theme/index.ts`):
```typescript
import { createTheme, rem } from '@mantine/core';

export const theme = createTheme({
  fontFamily: 'var(--font-body)',
  headings: {
    fontFamily: 'var(--font-heading)',
    fontWeight: '600',
    sizes: {
      h1: {
        fontSize: 'var(--font-size-h1)',
        lineHeight: 'var(--line-height-h1)',
      },
      h2: {
        fontSize: 'var(--font-size-h2)',
        lineHeight: 'var(--line-height-h2)',
      },
      h3: {
        fontSize: 'var(--font-size-h3)',
        lineHeight: '1.4',
      },
      h4: {
        fontSize: 'var(--font-size-h4)',
        lineHeight: '1.5',
      },
      h5: {
        fontSize: 'var(--font-size-h5)',
        lineHeight: '1.55',
      },
      h6: {
        fontSize: 'var(--font-size-h6)',
        lineHeight: '1.6',
      },
    }
  },
  fontSizes: {
    xs: rem(12),
    sm: rem(14),
    md: rem(16),  // Body text - maintains 16px minimum on mobile
    lg: rem(18),
    xl: rem(20),
  }
});
```

**Step 3: Use in Components**:
```tsx
import { Title, Text } from '@mantine/core';

function EventDetailPage() {
  return (
    <>
      {/* Automatically uses fluid typography from theme */}
      <Title order={1}>Salem's Rope Community</Title>

      {/* Custom override if needed */}
      <Title
        order={2}
        style={{ fontSize: 'var(--font-size-h2)' }}
      >
        Event Title
      </Title>

      {/* Body text */}
      <Text size="md">
        Event description with comfortable reading experience...
      </Text>
    </>
  );
}
```

### Accessibility Compliance

**WCAG 1.4.4 Resize Text (AA)**:
- ✅ Text must scale to 200% without horizontal scrolling
- ✅ Use `rem` units for min/max values (respects user preferences)
- ✅ Limit viewport unit contribution (<5vw) in preferred value

**Testing**:
```bash
# Browser zoom to 200%
1. Open event detail page
2. Press Cmd/Ctrl + "+" to zoom to 200%
3. Verify no horizontal scrolling
4. Verify text remains readable
```

---

## Part 2: Layout Standards

### Mantine Grid Responsive Props

**Mobile-First Pattern**:
```tsx
import { Grid } from '@mantine/core';

// Two-column to single-column
<Grid gutter={{ base: 'md', md: 'xl' }}>
  <Grid.Col span={{ base: 12, md: 8 }}>
    {/* Main content - full width on mobile, 8/12 on desktop */}
    <EventDetails />
  </Grid.Col>
  <Grid.Col span={{ base: 12, md: 4 }}>
    {/* Sidebar - full width on mobile, 4/12 on desktop */}
    <RSVPCard />
  </Grid.Col>
</Grid>
```

**Card Grid Pattern**:
```tsx
// 1 column mobile → 2 columns tablet → 3 columns desktop
<Grid gutter="md">
  {events.map(event => (
    <Grid.Col
      key={event.id}
      span={{
        base: 12,    // Mobile: 1 column (full width)
        sm: 6,       // Tablet: 2 columns
        md: 4,       // Desktop: 3 columns
      }}
    >
      <EventCard event={event} />
    </Grid.Col>
  ))}
</Grid>
```

### Two-Column to Single-Column Transitions

**Problem in EventDetailPage.tsx** (Lines 280-285):
```tsx
// ❌ WRONG - Desktop-only layout
<div style={{
  display: 'grid',
  gridTemplateColumns: '1fr 380px',
  gap: 'var(--space-xl)'
}}>
  <EventDetails />
  <RSVPCard />
</div>
```

**Solution**:
```tsx
// ✅ CORRECT - Responsive layout
<Box
  sx={{
    display: 'grid',
    gridTemplateColumns: {
      base: '1fr',           // Mobile: single column
      md: '1fr 380px',       // Desktop: main + fixed sidebar
    },
    gap: { base: 'md', md: 'xl' }
  }}
>
  <EventDetails />
  <RSVPCard />
</Box>

// OR using Mantine Grid (recommended)
<Grid gutter={{ base: 'md', md: 'xl' }}>
  <Grid.Col span={{ base: 12, md: 8 }}>
    <EventDetails />
  </Grid.Col>
  <Grid.Col span={{ base: 12, md: 4 }}>
    <RSVPCard />
  </Grid.Col>
</Grid>
```

### Container Sizing for Mobile

**Problem**: `Container size="xl"` may be too wide for mobile

**Solution**:
```tsx
import { Container } from '@mantine/core';

// Responsive container sizing
<Container
  size="xl"
  px={{ base: 'md', sm: 'lg', md: 'xl' }}
  py={{ base: 'lg', md: 'xl' }}
>
  {children}
</Container>
```

**Container Sizes**:
- `xs`: 540px
- `sm`: 720px
- `md`: 960px
- `lg`: 1140px
- `xl`: 1320px

**Mobile Recommendation**: Use `px` prop to add padding on mobile, allowing content to breathe within smaller viewports.

### Sticky Positioning Guidelines

**Problem in EventDetailPage.tsx** (Line 449):
```tsx
// ❌ WRONG - Sticky on mobile (problematic)
<Box pos="sticky" top={100}>
  <RSVPCard />
</Box>
```

**Solution**:
```tsx
// ✅ CORRECT - Sticky on desktop only
<Grid.Col span={{ base: 12, md: 4 }}>
  <Box
    pos={{ md: 'sticky' }}  // Only sticky on desktop
    top={{ md: 20 }}
    sx={(theme) => ({
      [theme.fn.largerThan('md')]: {
        maxHeight: 'calc(100vh - 40px)',  // Prevent taller than viewport
        overflowY: 'auto',  // Scrollable if content is long
      }
    })}
  >
    <RSVPCard />
  </Box>
</Grid.Col>
```

**Why**: On mobile, sticky sidebars take up viewport space, pushing content off-screen. Better to let content flow naturally.

**When to Use Sticky on Mobile**:
- ✅ Navigation headers/footers
- ✅ Fixed action buttons (bottom of screen)
- ❌ Sidebars
- ❌ Content cards

### Touch Target Minimums

**iOS Guidelines**: 44x44px minimum
**Android Guidelines**: 48x48px minimum
**WCAG 2.1 AAA**: 44x44px for all targets

**Implementation**:
```tsx
import { Button, ActionIcon } from '@mantine/core';

// ✅ CORRECT - Button with adequate touch target
<Button
  h={44}  // Height
  px="md" // Horizontal padding
  sx={{ minWidth: 44 }}  // Width
>
  RSVP
</Button>

// ✅ CORRECT - Using Mantine size props
<Button size="md">  {/* Default 'md' is 42px, acceptable */}
  RSVP
</Button>

// ✅ CORRECT - ActionIcon with proper sizing
<ActionIcon
  size={44}
  aria-label="Close"
>
  <IconX size={20} />
</ActionIcon>

// ❌ WRONG - Too small on mobile
<Button h={32} size="xs">
  RSVP  {/* Only 32px - hard to tap on mobile */}
</Button>
```

**Touch Target Spacing**:
```tsx
// ✅ CORRECT - 16px spacing between touch targets
<Group spacing="md">  {/* 16px spacing - safe */}
  <Button>Save</Button>
  <Button>Cancel</Button>
</Group>

// ❌ WRONG - Touch targets too close
<Group spacing={4}>  {/* Only 4px - accidental taps */}
  <Button>Save</Button>
  <Button>Cancel</Button>
</Group>
```

---

## Part 3: Navigation Standards

### Hamburger Menu Best Practices

**Current Problem in Navigation.tsx** (Lines 234-236):
```tsx
// ❌ ISSUE - Menu positioning off-screen
<div style={{
  position: 'fixed',
  right: isMobileMenuOpen ? 0 : '-100%',
  width: '80%',
  maxWidth: '320px'
}}>
```

**Issues**:
1. `right: '-100%'` may cause off-screen positioning
2. `width: '80%'` may be too wide on very small screens
3. Missing body scroll lock
4. Missing focus management

**Solution** - Use Mantine AppShell:
```tsx
import { AppShell, Burger, NavLink } from '@mantine/core';
import { useDisclosure } from '@mantine/hooks';

function Navigation() {
  const [opened, { toggle, close }] = useDisclosure();

  return (
    <AppShell
      header={{ height: 60 }}
      navbar={{
        width: 300,
        breakpoint: 'sm',
        collapsed: { mobile: !opened, desktop: true }
      }}
      padding="md"
    >
      <AppShell.Header>
        <Group h="100%" px="md" justify="space-between">
          <Logo />
          <Burger
            opened={opened}
            onClick={toggle}
            hiddenFrom="sm"
            size="md"
            aria-label="Toggle navigation menu"
            aria-expanded={opened}
            aria-controls="mobile-navigation"
          />
        </Group>
      </AppShell.Header>

      <AppShell.Navbar
        p="md"
        id="mobile-navigation"
        style={{
          WebkitOverflowScrolling: 'touch',
          overflowY: 'auto',
        }}
      >
        <NavLink
          href="/"
          label="Home"
          onClick={close}
        />
        <NavLink
          href="/events"
          label="Events"
          onClick={close}
        />
        {/* ... more links ... */}
      </AppShell.Navbar>

      <AppShell.Main>
        {children}
      </AppShell.Main>
    </AppShell>
  );
}
```

**Benefits**:
- ✅ Built-in body scroll lock
- ✅ Proper positioning (no off-screen issues)
- ✅ Accessibility (ARIA attributes required)
- ✅ iOS Safari compatibility
- ✅ Portal rendering (z-index management)

### Mobile Menu Positioning

**Critical iOS Safari Issue**:
> When hamburger menu positioned absolutely with many items, scrolling becomes unpleasant on iOS Safari - doesn't scroll smoothly and doesn't bounce in expected rubber-band way.

**Solution**:
```css
/* ❌ WRONG - Position menu absolutely */
.mobile-menu {
  position: absolute;
  top: 0;
  right: 0;
  height: 100vh;
  overflow-y: auto;
  /* Menu will have scrolling issues on iOS */
}

/* ✅ CORRECT - Position main content instead */
.main-content {
  position: fixed; /* Fix the content, not the menu */
  width: 100%;
  transition: transform 0.3s ease;
}

.main-content.menu-open {
  transform: translateX(-300px); /* Slide content over */
}

.mobile-menu {
  position: fixed;
  top: 0;
  right: 0;
  width: 300px;
  height: 100%; /* Natural height, not 100vh */
  overflow-y: auto;
  -webkit-overflow-scrolling: touch; /* iOS smooth scrolling */
}
```

**Note**: Mantine AppShell handles this automatically.

### Touch-Friendly Spacing

**Navigation Link Sizing**:
```tsx
import { NavLink } from '@mantine/core';

// ✅ CORRECT - Adequate touch targets
<NavLink
  label="Events"
  style={{
    minHeight: 48,
    padding: '12px 16px',
    fontSize: 16,  // Minimum for iOS without zoom
  }}
/>
```

**Edge Padding**:
```tsx
// ✅ CORRECT - 16px minimum from screen edges
<AppShell.Header>
  <Group h="100%" px="md" justify="space-between">
    {/* 16px padding from edges */}
  </Group>
</AppShell.Header>
```

### Body Scroll Locking Pattern

**Custom Hook** (if not using Mantine AppShell):
```typescript
import { useEffect, useRef } from 'react';

export function useLockBodyScroll(lock: boolean) {
  const scrollY = useRef(0);

  useEffect(() => {
    if (lock) {
      // Save current scroll position
      scrollY.current = window.scrollY;

      // Lock body scroll
      document.body.style.position = 'fixed';
      document.body.style.top = `-${scrollY.current}px`;
      document.body.style.width = '100%';
      document.body.style.overflowY = 'scroll'; // Prevent layout shift
      document.body.style.touchAction = 'none'; // Prevent iOS rubber band
    } else {
      // Restore body scroll
      document.body.style.position = '';
      document.body.style.top = '';
      document.body.style.width = '';
      document.body.style.overflowY = '';
      document.body.style.touchAction = '';

      // Restore scroll position
      window.scrollTo(0, scrollY.current);
    }

    return () => {
      // Cleanup on unmount
      document.body.style.position = '';
      document.body.style.top = '';
      document.body.style.width = '';
      document.body.style.overflowY = '';
      document.body.style.touchAction = '';
    };
  }, [lock]);
}

// Usage:
function MobileMenu({ isOpen }) {
  useLockBodyScroll(isOpen);
  // ... rest of component
}
```

---

## Part 4: Breakpoint Strategy

### Mantine v7 Breakpoint System

**Default Breakpoints**:
```typescript
const breakpoints = {
  xs: '36em',   // 576px  - Small phones (iPhone SE)
  sm: '48em',   // 768px  - Large phones / small tablets
  md: '62em',   // 992px  - Tablets
  lg: '75em',   // 1200px - Desktops
  xl: '88em',   // 1408px - Large desktops
};
```

**Usage in Components**:
```tsx
<Grid.Col span={{ base: 12, sm: 6, md: 4 }}>
  {/* base = mobile (<576px), sm = tablet (576px+), md = desktop (992px+) */}
</Grid.Col>
```

### Recommended Breakpoints for WitchCityRope

**Use 3-4 breakpoints for most layouts**:

1. **base** (mobile): < 576px - Single column layouts
2. **sm** (tablet): 576px+ - Two-column layouts
3. **md** (desktop): 992px+ - Multi-column layouts, sidebars appear
4. **lg** (large desktop): 1200px+ - Optional for wider layouts

**Example**:
```tsx
// ✅ OPTIMAL - 3 breakpoints
<Grid.Col span={{ base: 12, sm: 6, md: 4 }}>
  <Card />
</Grid.Col>

// ⚠️ OVERKILL - 5 breakpoints (unnecessary complexity)
<Grid.Col span={{ base: 12, xs: 12, sm: 6, md: 4, lg: 3, xl: 2 }}>
  <Card />
</Grid.Col>

// ❌ TOO FEW - 1 breakpoint (poor tablet experience)
<Grid.Col span={{ base: 12, md: 4 }}>
  <Card />  {/* Jumps from 1 column to 3 columns, no middle ground */}
</Grid.Col>
```

### Mobile-First Approach

**Always use mobile-first** - it's Mantine's default and prevents bugs:

```tsx
// ✅ MOBILE-FIRST (Recommended)
<Box
  p={{ base: 'md', sm: 'xl' }}  // Mobile default, then overrides
  fz={{ base: 14, sm: 16 }}
/>

// ❌ DESKTOP-FIRST (Avoid)
<Box
  sx={{
    padding: 'xl',           // Desktop default
    fontSize: '16px',
    '@media (max-width: 47.99em)': {  // Ugly breakpoint math
      padding: 'md',
      fontSize: '14px',
    }
  }}
/>
```

**Why Mobile-First?**:
- Progressive enhancement (works without CSS on old browsers)
- Easier to add features than remove them
- Aligns with Mantine's `min-width` media queries
- Better for performance (mobile loads fewer overrides)

### CSS Media Query Issues

**Problem in index.css** (Line 473):
```css
/* ❌ ISSUE - Single breakpoint + !important overrides */
@media (max-width: 768px) {
  .hero h1 {
    font-size: 40px !important;  /* Still too large, uses !important */
  }

  .section-title {
    font-size: 36px !important;  /* Could be smaller, uses !important */
  }
}
```

**Solution**:
```css
/* ✅ CORRECT - Use CSS clamp(), remove !important */
.hero h1 {
  font-size: clamp(1.75rem, 1.11vw + 1.39rem, 3rem);  /* 28px → 48px */
  /* No media queries needed! */
}

.section-title {
  font-size: clamp(1.5rem, 1.11vw + 1.14rem, 2.25rem);  /* 24px → 36px */
}
```

**Avoid `!important`**:
- ❌ Creates specificity wars
- ❌ Hard to override when needed
- ❌ Indicates design system problem
- ✅ Use proper CSS cascade instead

### When to Use CSS vs Mantine Responsive Props

**Mantine Responsive Props** (95% of cases):
```tsx
<Box
  w={{ base: 200, sm: 400, md: 600 }}
  p={{ base: 'md', md: 'xl' }}
  fz={{ base: 14, md: 16 }}
/>
```

**CSS Modules** (complex layouts, pseudo-selectors):
```tsx
import styles from './Event.module.css';

<div className={styles.eventGrid}>
  <Content />
</div>

// Event.module.css
.eventGrid {
  display: grid;
  grid-template-columns: 1fr;
  gap: var(--mantine-spacing-md);
}

@media (min-width: 62em) {
  .eventGrid {
    grid-template-columns: 1fr 380px;
    gap: var(--mantine-spacing-xl);
  }
}
```

**sx Prop** (quick prototypes, one-off components):
```tsx
<Box
  sx={(theme) => ({
    padding: theme.spacing.md,
    [theme.fn.largerThan('md')]: {
      padding: theme.spacing.xl,
    },
  })}
>
  <Content />
</Box>
```

---

## Part 5: Common Patterns & Solutions

### Event Detail Page Mobile Layout

**Current Issues**:
1. H1 at 48px (Line 317) - too large for mobile
2. Two-column grid (Lines 280-285) - no responsive breakpoints
3. Sticky positioning (Line 449) - problematic on mobile

**Complete Solution**:
```tsx
import { Grid, Box, Title, Text, Container } from '@mantine/core';

function EventDetailPage() {
  return (
    <Container size="xl" px={{ base: 'md', md: 'xl' }}>
      {/* Hero Section */}
      <Box py={{ base: 'lg', md: 'xl' }}>
        {/* ✅ Fluid typography from theme */}
        <Title order={1}>
          Event Title
        </Title>

        {/* ✅ Body text minimum 16px */}
        <Text size="md" mt="md">
          Event description
        </Text>
      </Box>

      {/* ✅ Responsive two-column layout */}
      <Grid gutter={{ base: 'md', md: 'xl' }}>
        {/* Main Content */}
        <Grid.Col span={{ base: 12, md: 8 }}>
          <Box>
            {/* ✅ Section titles use H2 (24px → 36px) */}
            <Title order={2} mb="md">
              Event Details
            </Title>
            <Text size="md">
              Content...
            </Text>
          </Box>
        </Grid.Col>

        {/* Sidebar */}
        <Grid.Col span={{ base: 12, md: 4 }}>
          {/* ✅ Sticky on desktop only */}
          <Box
            pos={{ md: 'sticky' }}
            top={{ md: 20 }}
            sx={(theme) => ({
              [theme.fn.largerThan('md')]: {
                maxHeight: 'calc(100vh - 40px)',
                overflowY: 'auto',
              }
            })}
          >
            <RSVPCard />
          </Box>
        </Grid.Col>
      </Grid>
    </Container>
  );
}
```

### Card Responsiveness

**Fluid Card Grid**:
```tsx
import { Grid, Card, Image, Text, Button } from '@mantine/core';

// 1 column mobile → 2 columns tablet → 3 columns desktop
<Grid gutter={{ base: 'md', md: 'lg' }}>
  {events.map(event => (
    <Grid.Col
      key={event.id}
      span={{ base: 12, sm: 6, lg: 4 }}
    >
      <Card
        shadow="sm"
        radius="md"
        withBorder
        h="100%"  // Equal height cards
      >
        <Card.Section>
          <Image src={event.image} height={200} alt={event.title} />
        </Card.Section>

        <Text fw={500} mt="md">{event.title}</Text>
        <Text size="sm" c="dimmed">{event.date}</Text>

        {/* ✅ Full-width button on mobile */}
        <Button fullWidth mt="md">
          View Details
        </Button>
      </Card>
    </Grid.Col>
  ))}
</Grid>
```

**Fixed-Width Sidebar Card**:
```tsx
// Use fixed width on desktop, fluid on mobile
<Box
  sx={{
    width: { base: '100%', md: 380 },
  }}
>
  <Card withBorder>
    <Text>Sidebar content</Text>
  </Card>
</Box>
```

### Hero Sections on Mobile

**Problem**: Large hero images/text consume too much mobile viewport

**Solution**:
```tsx
import { Box, Title, Text, BackgroundImage } from '@mantine/core';

<BackgroundImage
  src="/hero-image.jpg"
  h={{ base: 300, md: 500 }}  // Shorter on mobile
>
  <Box
    p={{ base: 'md', md: 'xl' }}
    sx={{
      display: 'flex',
      flexDirection: 'column',
      justifyContent: 'center',
      height: '100%',
    }}
  >
    {/* ✅ Fluid typography */}
    <Title
      order={1}
      c="white"
      style={{
        fontSize: 'var(--font-size-h1)',
        textShadow: '2px 2px 4px rgba(0,0,0,0.5)',
      }}
    >
      Salem's Rope Community
    </Title>

    {/* ✅ Hide subtitle on mobile if needed */}
    <Text
      size="lg"
      c="white"
      mt="md"
      visibleFrom="sm"
    >
      Learn, practice, connect
    </Text>
  </Box>
</BackgroundImage>
```

### Breadcrumb Navigation

**Mobile Guidelines** (Nielsen Norman Group):
- ✅ Truncate to show only last 1-2 levels
- ✅ Maintain 44x44px touch targets
- ✅ Use horizontal scrolling with visual affordances
- ❌ No multi-line wrapping (wastes space)
- ❌ No shrinking text/links (touch target issues)

**Implementation**:
```tsx
import { Breadcrumbs, Anchor, Text } from '@mantine/core';
import { useMediaQuery } from '@mantine/hooks';

function ResponsiveBreadcrumbs({ items }) {
  const isMobile = useMediaQuery('(max-width: 768px)');

  // Mobile: Show only last 2 levels
  const visibleItems = isMobile && items.length > 2
    ? [
        { label: '...', path: undefined },
        ...items.slice(-2)
      ]
    : items;

  return (
    <Breadcrumbs
      style={{
        overflowX: 'auto',
        WebkitOverflowScrolling: 'touch',
        padding: '12px 0',
        whiteSpace: 'nowrap',
      }}
    >
      {visibleItems.map((item, index) => {
        const isLast = index === visibleItems.length - 1;
        const isEllipsis = item.label === '...';

        if (isLast) {
          return (
            <Text
              key={index}
              fw={500}
              style={{
                padding: '8px 12px',
              }}
            >
              {item.label}
            </Text>
          );
        }

        if (isEllipsis) {
          return (
            <Text key={index} c="dimmed">
              {item.label}
            </Text>
          );
        }

        return (
          <Anchor
            key={index}
            href={item.path}
            style={{
              minHeight: 44,
              display: 'inline-flex',
              alignItems: 'center',
              padding: '8px 12px',
              maxWidth: isMobile ? 120 : undefined,
              overflow: 'hidden',
              textOverflow: 'ellipsis',
            }}
          >
            {item.label}
          </Anchor>
        );
      })}
    </Breadcrumbs>
  );
}
```

### Footer Layouts

**Responsive Footer**:
```tsx
import { Box, Grid, Text, Anchor, Stack } from '@mantine/core';

<Box
  component="footer"
  py={{ base: 'xl', md: '2xl' }}
  px={{ base: 'md', md: 'xl' }}
  bg="dark.9"
>
  <Grid gutter="xl">
    {/* 1 column mobile → 3 columns desktop */}
    <Grid.Col span={{ base: 12, sm: 4 }}>
      <Stack spacing="sm">
        <Text fw={700} c="white">About</Text>
        <Anchor href="/about" c="gray.5">About Us</Anchor>
        <Anchor href="/contact" c="gray.5">Contact</Anchor>
      </Stack>
    </Grid.Col>

    <Grid.Col span={{ base: 12, sm: 4 }}>
      <Stack spacing="sm">
        <Text fw={700} c="white">Events</Text>
        <Anchor href="/events" c="gray.5">Upcoming</Anchor>
        <Anchor href="/calendar" c="gray.5">Calendar</Anchor>
      </Stack>
    </Grid.Col>

    <Grid.Col span={{ base: 12, sm: 4 }}>
      <Stack spacing="sm">
        <Text fw={700} c="white">Legal</Text>
        <Anchor href="/privacy" c="gray.5">Privacy Policy</Anchor>
        <Anchor href="/terms" c="gray.5">Terms of Service</Anchor>
      </Stack>
    </Grid.Col>
  </Grid>
</Box>
```

---

## Part 6: Testing Requirements

### Required Test Devices/Sizes

**Minimum Viewports**:
- 320px (iPhone SE) - Smallest common phone
- 375px (iPhone 12/13) - Most common phone
- 414px (iPhone 14 Pro Max) - Largest phone
- 768px (iPad) - Tablet portrait
- 992px (iPad landscape) - Tablet landscape
- 1200px+ (Desktop) - Standard desktop

**Chrome DevTools Setup**:
```javascript
// Device presets to configure
const testDevices = [
  { name: 'iPhone SE', width: 375, height: 667 },
  { name: 'iPhone 12 Pro', width: 390, height: 844 },
  { name: 'iPhone 14 Pro Max', width: 430, height: 932 },
  { name: 'iPad', width: 768, height: 1024 },
  { name: 'Desktop', width: 1200, height: 800 },
];
```

**Playwright E2E Tests**:
```typescript
import { test, expect } from '@playwright/test';

test('Event detail page - mobile layout', async ({ page }) => {
  await page.setViewportSize({ width: 375, height: 667 }); // iPhone SE
  await page.goto('/events/test-event');

  // Verify single-column layout
  const grid = page.locator('[data-testid="event-grid"]');
  await expect(grid).toHaveCSS('grid-template-columns', '1fr');

  // Verify H1 size is mobile-appropriate (not 48px)
  const h1 = page.locator('h1');
  const fontSize = await h1.evaluate(el =>
    window.getComputedStyle(el).fontSize
  );
  expect(parseInt(fontSize)).toBeLessThanOrEqual(32); // Max 32px on mobile
});

test('Event detail page - desktop layout', async ({ page }) => {
  await page.setViewportSize({ width: 1200, height: 800 });
  await page.goto('/events/test-event');

  // Verify two-column layout
  const grid = page.locator('[data-testid="event-grid"]');
  await expect(grid).toHaveCSS('grid-template-columns', /1fr 380px/);
});
```

### Browser Testing Requirements

**Required Browsers**:
- ✅ Chrome Mobile (Android)
- ✅ Safari Mobile (iOS)
- ✅ Samsung Internet (Android)
- ✅ Firefox Mobile

**Testing Checklist**:
- [ ] Layouts adapt correctly at all breakpoints
- [ ] Typography scales fluidly (no text cutoff)
- [ ] Touch targets meet 44x44px minimum
- [ ] Hamburger menu opens/closes correctly
- [ ] Body scroll locked when menu open
- [ ] No horizontal scrolling on any page
- [ ] Sticky elements work correctly (desktop only)
- [ ] Forms are usable on mobile
- [ ] Buttons are tappable without zoom
- [ ] Images scale proportionally

### Touch Interaction Testing

**Manual Testing**:
```
1. Open page on real mobile device (not just emulator)
2. Attempt to tap all buttons/links
3. Verify no accidental taps (targets too close)
4. Verify no "fat finger" issues (targets too small)
5. Test swipe gestures (scrolling)
6. Test pinch-to-zoom (should work)
7. Test landscape orientation
```

**Automated Touch Target Test**:
```typescript
test('All touch targets meet minimum size', async ({ page }) => {
  await page.setViewportSize({ width: 375, height: 667 });
  await page.goto('/events');

  // Get all interactive elements
  const buttons = await page.locator('button, a[href]').all();

  for (const button of buttons) {
    const box = await button.boundingBox();
    if (box) {
      expect(box.width).toBeGreaterThanOrEqual(44);
      expect(box.height).toBeGreaterThanOrEqual(44);
    }
  }
});
```

### Accessibility Testing for Mobile

**Screen Reader Testing**:
- VoiceOver (iOS)
- TalkBack (Android)

**Checklist**:
- [ ] Hamburger menu announces "Toggle navigation menu"
- [ ] Menu state announced (expanded/collapsed)
- [ ] Focus trapped within menu when open
- [ ] Escape key closes menu
- [ ] Focus returns to burger button after close
- [ ] All links/buttons have accessible labels
- [ ] Form inputs have associated labels
- [ ] Error messages announced

**ARIA Attributes**:
```tsx
<Burger
  aria-label="Toggle navigation menu"
  aria-expanded={opened}
  aria-controls="mobile-nav"
/>

<nav
  id="mobile-nav"
  aria-label="Mobile navigation"
  role="navigation"
  aria-hidden={!opened}
>
  {/* Links */}
</nav>
```

---

## Part 7: Code Examples

### Before/After: EventDetailPage.tsx

**BEFORE** (Desktop-only):
```tsx
// ❌ Issues: Fixed font sizes, no responsive layout, sticky on mobile
function EventDetailPage() {
  return (
    <Container size="xl">
      {/* H1 too large for mobile */}
      <Title style={{ fontSize: '48px' }}>
        Event Title
      </Title>

      {/* Desktop-only grid */}
      <div style={{
        display: 'grid',
        gridTemplateColumns: '1fr 380px',
        gap: 'var(--space-xl)'
      }}>
        {/* Main content */}
        <div>
          <Title style={{ fontSize: '28px' }}>Details</Title>
          <Text style={{ fontSize: '17px' }}>Content</Text>
        </div>

        {/* Sidebar sticky on all devices */}
        <div style={{ position: 'sticky', top: '100px' }}>
          <RSVPCard />
        </div>
      </div>
    </Container>
  );
}
```

**AFTER** (Mobile-responsive):
```tsx
// ✅ Fluid typography, responsive layout, conditional sticky
function EventDetailPage() {
  return (
    <Container
      size="xl"
      px={{ base: 'md', md: 'xl' }}
      py={{ base: 'lg', md: 'xl' }}
    >
      {/* Fluid H1 from theme */}
      <Title order={1}>
        Event Title
      </Title>

      {/* Responsive grid */}
      <Grid gutter={{ base: 'md', md: 'xl' }} mt="xl">
        {/* Main content - full width mobile, 8/12 desktop */}
        <Grid.Col span={{ base: 12, md: 8 }}>
          <Stack spacing="lg">
            {/* Fluid H2 from theme */}
            <Title order={2}>Details</Title>

            {/* Body text minimum 16px */}
            <Text size="md">Content</Text>
          </Stack>
        </Grid.Col>

        {/* Sidebar - full width mobile, 4/12 desktop */}
        <Grid.Col span={{ base: 12, md: 4 }}>
          {/* Sticky on desktop only */}
          <Box
            pos={{ md: 'sticky' }}
            top={{ md: 20 }}
            sx={(theme) => ({
              [theme.fn.largerThan('md')]: {
                maxHeight: 'calc(100vh - 40px)',
                overflowY: 'auto',
              }
            })}
          >
            <RSVPCard />
          </Box>
        </Grid.Col>
      </Grid>
    </Container>
  );
}
```

### Complete Component Example: ResponsiveCard

```tsx
import { Card, Image, Text, Button, Badge, Group, Stack } from '@mantine/core';

interface ResponsiveCardProps {
  title: string;
  description: string;
  imageUrl: string;
  date: string;
  type: 'workshop' | 'social';
  onViewDetails: () => void;
}

export function ResponsiveCard({
  title,
  description,
  imageUrl,
  date,
  type,
  onViewDetails
}: ResponsiveCardProps) {
  return (
    <Card
      shadow="sm"
      radius="md"
      withBorder
      h="100%"
      style={{
        display: 'flex',
        flexDirection: 'column',
      }}
    >
      {/* Image section */}
      <Card.Section>
        <Image
          src={imageUrl}
          height={200}
          alt={title}
        />
      </Card.Section>

      {/* Content section - grows to fill space */}
      <Stack
        spacing="xs"
        mt="md"
        style={{ flex: 1 }}
      >
        {/* Badge */}
        <Group>
          <Badge
            color={type === 'workshop' ? 'blue' : 'green'}
            variant="light"
          >
            {type === 'workshop' ? 'Workshop' : 'Social Event'}
          </Badge>
        </Group>

        {/* Title - fluid typography from theme */}
        <Text fw={500} size="lg">
          {title}
        </Text>

        {/* Description - clamp to 3 lines */}
        <Text
          size="sm"
          c="dimmed"
          lineClamp={3}
        >
          {description}
        </Text>

        {/* Date */}
        <Text size="sm" c="dimmed">
          {date}
        </Text>
      </Stack>

      {/* Button section - stays at bottom */}
      <Button
        fullWidth
        mt="md"
        onClick={onViewDetails}
        h={44}  // Touch target minimum
      >
        View Details
      </Button>
    </Card>
  );
}

// Usage in grid
function EventsList({ events }) {
  return (
    <Grid gutter={{ base: 'md', md: 'lg' }}>
      {events.map(event => (
        <Grid.Col
          key={event.id}
          span={{ base: 12, sm: 6, lg: 4 }}
        >
          <ResponsiveCard
            title={event.title}
            description={event.description}
            imageUrl={event.imageUrl}
            date={event.date}
            type={event.type}
            onViewDetails={() => navigate(`/events/${event.id}`)}
          />
        </Grid.Col>
      ))}
    </Grid>
  );
}
```

### CSS Clamp() Calculations

**Manual Calculation** (for understanding):
```
Given:
- Min viewport: 320px (20rem)
- Max viewport: 1400px (87.5rem)
- Min font size: 28px (1.75rem)
- Max font size: 48px (3rem)

Calculate:
1. Size difference: 48 - 28 = 20px (1.25rem)
2. Viewport difference: 1400 - 320 = 1080px (67.5rem)
3. Viewport coefficient: 1.25rem / 67.5rem = 0.0185rem/rem = 1.85vw
4. Y-intercept: 1.75rem - (1.85vw × 20rem) = 1.75rem - 0.37rem = 1.38rem

Result: clamp(1.75rem, 1.85vw + 1.38rem, 3rem)

Simplified: clamp(1.75rem, 1.11vw + 1.39rem, 3rem)
```

**Using Utopia Calculator** (recommended):
1. Go to https://utopia.fyi/type/calculator
2. Enter min viewport: 320px
3. Enter max viewport: 1400px
4. Enter min size: 28px
5. Enter max size: 48px
6. Copy generated clamp() value

**All WitchCityRope Heading Clamps**:
```css
:root {
  --font-size-h1: clamp(1.75rem, 1.11vw + 1.39rem, 3rem);      /* 28px → 48px */
  --font-size-h2: clamp(1.5rem, 1.11vw + 1.14rem, 2.25rem);    /* 24px → 36px */
  --font-size-h3: clamp(1.25rem, 0.74vw + 1.02rem, 1.75rem);   /* 20px → 28px */
  --font-size-h4: clamp(1.125rem, 0.56vw + 0.95rem, 1.5rem);   /* 18px → 24px */
  --font-size-h5: clamp(1rem, 0.37vw + 0.88rem, 1.25rem);      /* 16px → 20px */
  --font-size-h6: clamp(1rem, 0.19vw + 0.94rem, 1.125rem);     /* 16px → 18px */
}
```

### Mantine Grid Patterns

**Pattern 1: Equal Columns → Single Column**:
```tsx
<Grid gutter="lg">
  <Grid.Col span={{ base: 12, md: 6 }}>
    <LeftContent />
  </Grid.Col>
  <Grid.Col span={{ base: 12, md: 6 }}>
    <RightContent />
  </Grid.Col>
</Grid>
```

**Pattern 2: Main + Sidebar (8/4) → Single Column**:
```tsx
<Grid gutter="xl">
  <Grid.Col span={{ base: 12, md: 8 }}>
    <MainContent />
  </Grid.Col>
  <Grid.Col span={{ base: 12, md: 4 }}>
    <Sidebar />
  </Grid.Col>
</Grid>
```

**Pattern 3: Fluid + Fixed Width → Single Column**:
```tsx
<Box
  sx={{
    display: 'grid',
    gridTemplateColumns: {
      base: '1fr',
      md: '1fr 380px',
    },
    gap: { base: 'md', md: 'xl' }
  }}
>
  <EventDetails />
  <Box sx={{ width: { base: '100%', md: 380 } }}>
    <RSVPCard />
  </Box>
</Box>
```

**Pattern 4: Card Grid (1 → 2 → 3 → 4 columns)**:
```tsx
<Grid gutter="md">
  {items.map(item => (
    <Grid.Col
      key={item.id}
      span={{
        base: 12,    // Mobile: 1 column
        sm: 6,       // Tablet: 2 columns
        md: 4,       // Desktop: 3 columns
        lg: 3        // Large: 4 columns
      }}
    >
      <Card />
    </Grid.Col>
  ))}
</Grid>
```

---

## Part 8: Quick Reference

### Typography Cheat Sheet

```typescript
// H1-H6 mobile → desktop sizes
H1: 28px → 48px
H2: 24px → 36px
H3: 20px → 28px
H4: 18px → 24px
H5: 16px → 20px
H6: 16px → 18px
Body: 16px (minimum, never smaller)

// Line heights
H1-H2: 1.2-1.4 (tight)
H3-H6: 1.4-1.55 (medium)
Body: 1.6 (generous)
```

### Breakpoint Cheat Sheet

```tsx
// Mantine breakpoint values
xs: 576px, sm: 768px, md: 992px, lg: 1200px, xl: 1408px

// Common responsive patterns
<Grid.Col span={{ base: 12, sm: 6, md: 4, lg: 3 }}>  // 1→2→3→4 columns
<Box p={{ base: 'md', md: 'xl' }}>                   // Mobile/desktop padding
<Button size={{ base: 'sm', md: 'md' }}>             // Smaller button on mobile
<Text fz={{ base: 14, md: 16 }}>                     // Smaller text on mobile
```

### Touch Target Cheat Sheet

```tsx
// Minimum sizes
Button: 44x44px (iOS), 48x48px (Android)
Spacing: 8-12px minimum between touch targets

// Implementation
<Button h={44} px="md" sx={{ minWidth: 44 }}>RSVP</Button>
<Group spacing="md">  {/* 16px spacing = safe */}
```

### Navigation Cheat Sheet

```tsx
// Hamburger menu pattern (Mantine AppShell)
const [opened, { toggle, close }] = useDisclosure();

<AppShell
  navbar={{
    width: 300,
    breakpoint: 'sm',
    collapsed: { mobile: !opened }
  }}
>
  <AppShell.Header>
    <Burger
      opened={opened}
      onClick={toggle}
      hiddenFrom="sm"
      aria-label="Toggle navigation menu"
    />
  </AppShell.Header>
  <AppShell.Navbar>{/* Links */}</AppShell.Navbar>
</AppShell>
```

### Grid Patterns Cheat Sheet

```tsx
// Single column → two columns
<Grid><Grid.Col span={{ base: 12, md: 6 }}>

// Single column → main + sidebar (8/4)
<Grid><Grid.Col span={{ base: 12, md: 8 }}>

// Fluid + fixed width
gridTemplateColumns: { base: '1fr', md: '1fr 380px' }

// Auto-responsive card grid (no media queries)
gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))'
```

### Testing Checklist

**Desktop Browser**:
- [ ] Open Chrome DevTools
- [ ] Toggle device toolbar (Cmd+Shift+M)
- [ ] Test viewports: 375px, 768px, 1200px
- [ ] Verify no horizontal scroll
- [ ] Verify typography scales
- [ ] Verify layouts adapt

**Real Devices**:
- [ ] iPhone (iOS Safari)
- [ ] Android phone (Chrome)
- [ ] iPad (Safari)
- [ ] Test touch interactions
- [ ] Test hamburger menu
- [ ] Test form inputs

---

## Part 9: Global Mobile CSS Patterns

These patterns have been tested and implemented globally to ensure consistent mobile UX across all pages. They are automatically applied through `index.css` and require no per-page configuration.

### 9.1 List Styling (UL Elements)

**Purpose**: Maximize horizontal space on mobile while maintaining text alignment.

**Implementation** (in `index.css`):
```css
@media (max-width: 768px) {
  ul {
    padding-left: 1.2em !important;
    margin-left: 0 !important;
    list-style-position: outside;
  }
}
```

**Effect**:
- Bullets align to left edge (no left margin)
- Wrapped text aligns with first line of text, not bullet
- 1.2em padding provides space for bullet + visual alignment

**Before vs After**:
```
❌ Before (inside bullets):
• This is a long line that wraps
back to the left edge

✅ After (outside bullets):
• This is a long line that wraps
  aligned with first line
```

**Why This Matters**:
- On mobile screens, every pixel counts
- Text wrapping aligned with first line (not bullet) creates cleaner visual hierarchy
- Reduces "jagged" appearance of wrapped list items
- Improves readability by maintaining consistent text alignment

### 9.2 Line-Height Optimization

**Purpose**: Show more content per screen on mobile without sacrificing readability.

**Implementation** (in `index.css`):
```css
@media (max-width: 768px) {
  body,
  p,
  li,
  .mantine-Text-root {
    line-height: 1.5 !important;
  }

  p {
    margin-bottom: 1rem !important;
  }
}
```

**Effect**:
- **Within paragraphs**: Tighter line spacing (1.5 vs desktop 1.8)
- **Between paragraphs**: Same spacing maintained (1rem margin-bottom)
- **Result**: More content visible per screen, visual hierarchy preserved

**Comparison**:

| Element | Desktop | Mobile | Savings |
|---------|---------|--------|---------|
| Line-height | 1.8 | 1.5 | ~16% more content per screen |
| Paragraph margin | 1rem | 1rem | No change (maintains hierarchy) |

**Why This Matters**:
- Mobile screens have limited vertical space
- Users shouldn't have to scroll excessively for the same content
- 1.5 line-height is still comfortable to read (WCAG compliant)
- Maintaining paragraph spacing preserves visual breaks between content blocks

### 9.3 Where These Rules Live

**Current Location**: `/home/chad/repos/witchcityrope/apps/web/src/index.css` (lines ~543-561)

**Status**: Implemented globally via CSS media query

**Future Work**: These patterns are already global (apply to all pages). No additional action needed unless we want to refine breakpoints or adjust values.

### 9.4 Applying to New Pages

✅ **Automatic**: These CSS rules automatically apply to all pages since they're in the global `index.css`.

✅ **No action needed**: Developers don't need to do anything special - lists and line-height are handled globally.

⚠️ **Override if needed**: If a specific component needs different behavior, use inline styles or component-specific CSS with higher specificity.

**Example Override** (if needed):
```tsx
// Component that needs desktop line-height on mobile
<Box
  sx={{
    '& p': {
      lineHeight: '1.8 !important', // Override mobile 1.5
    }
  }}
>
  <Text>Content that needs more breathing room</Text>
</Box>
```

### 9.5 Testing Checklist

When testing mobile layouts, verify:
- [ ] UL bullets align to left edge
- [ ] Wrapped list text aligns with first line
- [ ] Line spacing within paragraphs is tighter (1.5)
- [ ] Space between paragraphs is preserved
- [ ] Content doesn't feel cramped
- [ ] More content visible per screen than before
- [ ] No horizontal scrolling on lists
- [ ] Text remains readable at 1.5 line-height

### 9.6 Pattern Discovery Process

These patterns were discovered through iterative testing on the EventDetailPage:

1. **Problem Identified**: Lists were pushing bullets into text on mobile (poor horizontal space usage)
2. **Solution Tested**: `list-style-position: outside` with `padding-left: 1.2em`
3. **Problem Identified**: Too much vertical scrolling required on mobile
4. **Solution Tested**: Reduced line-height to 1.5 on mobile only
5. **Validation**: Both patterns improved UX without sacrificing readability
6. **Globalization**: Moved patterns to `index.css` for consistent application

**Key Insight**: Mobile typography should prioritize content density while maintaining readability. These patterns achieve both goals.

### 9.7 Future Enhancements

**Potential Refinements**:
- Test different padding-left values for lists (1.2em vs 1.5em)
- Consider breakpoint adjustments (768px vs 640px cutoff)
- Test line-height variations by content type (documentation vs marketing)
- Evaluate OL (ordered list) behavior with these patterns

**When to Update These Patterns**:
- User feedback indicates readability issues on mobile
- Analytics show high bounce rates on mobile content pages
- A/B testing shows different values perform better
- Accessibility audits recommend changes

---

## Conclusion

This guide consolidates best practices from 50,000+ lines of research into actionable patterns for WitchCityRope frontend development.

**Key Takeaways**:
1. **Typography**: Use CSS clamp() for fluid scaling (H1: 28px → 48px)
2. **Layouts**: Use Mantine Grid with responsive spans
3. **Touch Targets**: Minimum 44x44px for all interactive elements
4. **Navigation**: Use Mantine AppShell for mobile menu
5. **Testing**: Test on real devices at 320px, 375px, 768px, 1200px
6. **Mobile-First**: Always start with mobile, enhance for desktop
7. **Global Patterns**: List styling and line-height optimization apply automatically

**Remember**: Mobile responsiveness is NOT optional. This guide is mandatory reading before implementing ANY React component.

---

## Related Documentation

- [Mobile Typography Research](/docs/functional-areas/ui-ux/research/mobile-typography-best-practices.md)
- [Mobile Layout Patterns Research](/docs/functional-areas/ui-ux/research/2025-11-12-mobile-layout-patterns.md)
- [Mobile Navigation Patterns Research](/docs/functional-areas/ui-ux/research/2025-11-12-mobile-navigation-patterns.md)
- [React Patterns Guide](/docs/standards-processes/frontend/react-patterns.md)
- [Mantine v7 Documentation](https://mantine.dev/)

---

**Document Version**: 1.1
**Last Updated**: 2025-11-12
**Owner**: Librarian Agent
**Review Cycle**: Quarterly

**Feedback**: If you discover mobile responsiveness issues not covered in this guide, update this document and notify the librarian agent.

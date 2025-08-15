# Witch City Rope - Final Style Guide

## Overview

This is the definitive style guide for the Witch City Rope platform, consolidating all design decisions made during the design phase. This guide should be the single source of truth for all visual design and user experience decisions.

**Version:** 1.0  
**Last Updated:** January 26, 2025  
**Status:** Ready for Implementation

## Brand Identity

### Mission & Values
Witch City Rope is Salem's premier rope bondage education and practice community, focused on creating a supportive, consent-focused space that celebrates every journey.

### Brand Voice
- **Inclusive Education**: Welcoming to all experience levels
- **Respectful Community**: Emphasizing consent and safety
- **Playful Exploration**: Encouraging growth and discovery

### Key Messaging
- **Tagline**: "Where curiosity meets connection"
- **Value Proposition**: "Join 600+ members learning and growing together"
- **Call to Action**: Warm and inviting ("Start Your Journey", "Join Our Community")

## Visual Design System

### Color Palette

#### Primary Colors
```css
--color-burgundy: #880124;      /* Primary brand color */
--color-burgundy-dark: #660018; /* Hover states, emphasis */
--color-burgundy-light: #9F1D35; /* Lighter tints */
--color-plum: #614B79;          /* Secondary accent */
```

#### CTA & Accent Colors
```css
--color-amber: #FFBF00;         /* Primary CTA buttons */
--color-amber-dark: #E6AC00;    /* CTA hover states */
--color-electric: #9D4EDD;      /* Alternative CTA */
--color-electric-dark: #7B2CBF; /* Alternative hover */
```

#### Warm Metallics
```css
--color-rose-gold: #B76D75;     /* Decorative accents */
--color-copper: #B87333;        /* Secondary metallics */
--color-brass: #C9A961;         /* Special highlights */
```

#### Neutral Palette
```css
--color-midnight: #1A1A2E;      /* Dark backgrounds */
--color-charcoal: #2B2B2B;     /* Primary text */
--color-smoke: #4A4A4A;         /* Secondary text */
--color-stone: #8B8680;         /* Muted text */
--color-taupe: #B8B0A8;         /* Borders, dividers */
--color-ivory: #FFF8F0;         /* Light backgrounds */
--color-cream: #FAF6F2;         /* Alternate backgrounds */
```

#### Status Colors
```css
--color-success: #228B22;       /* Success states */
--color-warning: #DAA520;       /* Warning states */
--color-error: #DC143C;         /* Error states */
--color-info: #4169E1;          /* Information */
```

### Typography

#### Font Families
```css
--font-display: 'Bodoni Moda', serif;    /* Decorative headers */
--font-heading: 'Montserrat', sans-serif; /* All headings */
--font-body: 'Source Sans 3', sans-serif; /* Body text */
--font-accent: 'Satisfy', cursive;       /* Decorative accents */
```

#### Font Sizes
```css
--text-xs: 12px;
--text-sm: 14px;
--text-base: 16px;
--text-lg: 18px;
--text-xl: 20px;
--text-2xl: 24px;
--text-3xl: 28px;
--text-4xl: 32px;
--text-5xl: 40px;
--text-6xl: 48px;
```

#### Type Scale
- **Hero Headings**: 64px desktop / 40px mobile
- **Section Titles**: 48px desktop / 36px mobile
- **Card Titles**: 24px
- **Body Text**: 16px with 1.6 line height
- **Small Text**: 14px
- **Captions**: 12px

### Spacing System (4px Grid)
```css
--space-xs: 8px;
--space-sm: 16px;
--space-md: 24px;
--space-lg: 32px;
--space-xl: 40px;
--space-2xl: 48px;
--space-3xl: 64px;
```

### Component Specifications

#### Buttons

**Primary CTA Button**
- Background: Linear gradient `amber` to `amber-dark`
- Text: Uppercase, 14px, letter-spacing 1.5px
- Padding: 14px 32px
- Border radius: 4px
- Shadow: `0 4px 15px rgba(255, 191, 0, 0.4)`
- Hover: Lift 2px, increase shadow

**Secondary Button**
- Background: Transparent
- Border: 2px solid burgundy
- Text: Burgundy color
- Hover: Fill with burgundy, text becomes ivory

**Button States**
- Focus: 3px burgundy outline with 2px offset
- Disabled: 0.5 opacity, no pointer events
- Loading: Spinner animation overlay

#### Forms

**Input Fields**
- Background: Cream (#FAF6F2)
- Border: 2px solid taupe
- Padding: 16px
- Border radius: 8px
- Focus: Burgundy border with shadow
- Font size: 16px (prevents iOS zoom)

**Labels**
- Font: Montserrat 500
- Size: 14px
- Color: Charcoal
- Spacing: 8px below label

#### Cards
- Background: Ivory
- Border radius: 8px
- Padding: 24px
- Shadow: `0 4px 8px rgba(0,0,0,0.1)`
- Hover: Lift 4px, increase shadow

#### Navigation

**Header**
- Background: `rgba(255, 248, 240, 0.95)` with backdrop blur
- Sticky positioning with scroll shadow
- Logo: Montserrat 800, 30px
- Nav items: Montserrat 500, 15px uppercase

**Utility Bar**
- Background: Midnight (#1A1A2E)
- Text: 13px uppercase with 1px letter spacing
- Links: Taupe color, rose-gold hover

### Layout Principles

#### Grid System
- 12-column grid on desktop
- 8-column grid on tablet
- 4-column grid on mobile
- Gutters: 24px desktop, 16px mobile

#### Breakpoints
```css
--breakpoint-xs: 480px;  /* Large phones */
--breakpoint-sm: 768px;  /* Tablets */
--breakpoint-md: 1024px; /* Desktop */
--breakpoint-lg: 1440px; /* Large desktop */
```

#### Container Widths
- Max width: 1400px
- Padding: 40px desktop, 20px mobile

### Animation & Transitions

#### Standard Transitions
```css
--transition-fast: 150ms ease;
--transition-base: 300ms ease;
--transition-slow: 500ms ease;
```

#### Hover Effects
- Buttons: Transform and shadow changes
- Links: Underline animation
- Cards: Lift effect with shadow

#### Loading States
- Skeleton screens for content
- Spinning indicators for actions
- Progress bars for multi-step processes

### Accessibility

#### Focus States
- 3px outline with burgundy color
- 2px offset from element
- High contrast (4.5:1 minimum)

#### Touch Targets
- Minimum 44px height on mobile
- Adequate spacing between targets
- Clear active states

#### Screen Reader Support
- Semantic HTML structure
- ARIA labels where needed
- Skip navigation links

### Mobile Considerations

#### Navigation
- Hamburger menu at 768px breakpoint
- Slide-out drawer pattern
- Overlay with body scroll lock

#### Typography
- Responsive font sizes
- Minimum 16px for inputs (prevents zoom)
- Adjusted line heights for readability

#### Layout
- Single column on mobile
- Stacked forms and content
- Full-width buttons and inputs

## Component Library

### 1. Navigation Components
- Utility bar (top announcement bar)
- Main navigation header
- Mobile hamburger menu
- Breadcrumb navigation
- Sidebar navigation (admin/member areas)

### 2. Form Components
- Text inputs with floating labels
- Textareas with character count
- Select dropdowns
- Radio buttons and checkboxes
- Toggle switches
- Date/time pickers
- File upload areas
- Multi-step forms with progress

### 3. Display Components
- Event cards (grid and list views)
- Member cards
- Stats cards
- Progress bars
- Badges and tags
- Tables (responsive)
- Modals and dialogs
- Tooltips

### 4. Feedback Components
- Alert messages (success, warning, error, info)
- Toast notifications
- Loading spinners
- Skeleton screens
- Empty states

## Page Templates

### Public Pages
1. Landing page with hero section
2. Event listing with filters
3. Event detail with registration
4. Authentication pages (login, register, password reset)
5. Error pages (404, 403, 500)

### Member Pages
1. Dashboard with stats and quick actions
2. My Events/Tickets
3. Profile settings
4. Membership management

### Admin Pages
1. Admin dashboard
2. Event management
3. Vetting queue
4. Member management

## Implementation Notes

### CSS Architecture
- Use CSS custom properties for all values
- Mobile-first approach
- Component-based organization
- Utility classes for common patterns

### Performance
- Lazy load images
- Optimize font loading
- Minimize CSS bundle size
- Use CSS containment

### Browser Support
- Modern browsers (last 2 versions)
- Safari 14+
- No IE11 support

### Syncfusion Integration
See `syncfusion-component-mapping.md` for detailed component mappings and customization requirements.

## Design Tokens

All design decisions are available as JSON tokens in `design-tokens.json` for easy integration with design tools and build systems.

## Responsive Design

See `responsive-design-issues.md` for specific responsive implementation requirements and fixes.

## Version History

### v1.0 (January 26, 2025)
- Initial consolidated style guide
- Combined all design decisions from design phase
- Ready for implementation phase

---

This style guide represents the complete visual design system for Witch City Rope. All implementation should reference this document to ensure consistency across the platform.
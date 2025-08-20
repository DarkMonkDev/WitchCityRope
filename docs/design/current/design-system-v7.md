<!-- Last Updated: 2025-08-20 -->
<!-- Version: v7.0 -->
<!-- Owner: Design Team -->
<!-- Status: Active Authority -->

# Design System v7 - WitchCityRope

**Authority Status**: This is the AUTHORITATIVE design system for WitchCityRope v7
**Source Template**: [homepage-template-v7.html](./homepage-template-v7.html)

## Overview

This document defines the complete v7 design system extracted from the approved Final Design v7 template. All new development MUST use this design system as the single source of truth.

## Design Philosophy

- **Edgy Sophistication**: Modern, sensual aesthetic that respects the rope bondage community
- **Warm Luxury**: Rose gold, burgundy, and brass metallics create premium feel
- **Smooth Interactions**: Signature animations enhance without overwhelming
- **Accessibility First**: WCAG 2.1 AA compliance throughout
- **Mobile Excellence**: Mobile-first responsive design patterns

## Typography System

### Font Stack
```css
--font-display: 'Bodoni Moda', serif;     /* Headlines, dramatic text */
--font-heading: 'Montserrat', sans-serif;  /* Section titles, navigation */
--font-body: 'Source Sans 3', sans-serif;  /* Body text, descriptions */
--font-accent: 'Satisfy', cursive;         /* Taglines, decorative text */
```

### Typography Scale
- **Display (Hero)**: 64px / 40px mobile, Montserrat 800, uppercase, -1px letter-spacing
- **Section Titles**: 48px / 36px mobile, Montserrat 800, uppercase, 3px letter-spacing
- **Card Titles**: 24px, Montserrat 700, uppercase, 1px letter-spacing
- **Navigation**: 15px, Montserrat 500, uppercase, 1px letter-spacing
- **Body Text**: 16px, Source Sans 3 400, 1.7 line-height
- **Small Text**: 14px, Montserrat 600, uppercase
- **Taglines**: 28px / 22px mobile, Satisfy 400

### Typography Usage
- Display font for hero headlines only
- Heading font for all navigation, titles, labels
- Body font for all content text
- Accent font for taglines and special phrases

## Color Palette

### Primary Colors
```css
--color-burgundy: #880124;        /* Primary brand color */
--color-burgundy-dark: #660018;   /* Dark variant */
--color-burgundy-light: #9F1D35;  /* Light variant */
```

### Metallic Accents
```css
--color-rose-gold: #B76D75;  /* Primary accent, borders, highlights */
--color-copper: #B87333;     /* Secondary accent */
--color-brass: #C9A961;      /* Tertiary accent, warnings */
```

### CTA Colors
```css
--color-electric: #9D4EDD;      /* Primary CTA background */
--color-electric-dark: #7B2CBF; /* Primary CTA hover */
--color-amber: #FFBF00;          /* Secondary CTA background */
--color-amber-dark: #FF8C00;     /* Secondary CTA hover */
```

### Supporting Colors
```css
--color-dusty-rose: #D4A5A5;  /* Light accent */
--color-plum: #614B79;        /* Event card backgrounds */
--color-midnight: #1A1A2E;    /* Dark sections, footer */
```

### Neutral Colors
```css
--color-charcoal: #2B2B2B;  /* Primary text */
--color-smoke: #4A4A4A;     /* Secondary text */
--color-stone: #8B8680;     /* Tertiary text */
--color-taupe: #B8B0A8;     /* Light text on dark */
--color-ivory: #FFF8F0;     /* Light text, card text */
--color-cream: #FAF6F2;     /* Body background */
```

### Status Colors
```css
--color-success: #228B22;  /* Available, positive states */
--color-warning: #DAA520;  /* Limited spots, warnings */
--color-error: #DC143C;    /* Full, errors, alerts */
```

### Color Usage Guidelines
- Use burgundy for primary brand elements and main navigation
- Rose gold for accents, underlines, and interactive highlights
- Electric purple for primary action buttons
- Amber for secondary actions and login buttons
- Midnight for dark sections and backgrounds
- Cream for main page background

## Spacing System

```css
--space-xs: 8px;   /* Fine details, small gaps */
--space-sm: 16px;  /* Component internal spacing */
--space-md: 24px;  /* Related element spacing */
--space-lg: 32px;  /* Component spacing */
--space-xl: 40px;  /* Section spacing */
--space-2xl: 48px; /* Large section spacing */
--space-3xl: 64px; /* Hero and major sections */
```

### Spacing Usage
- Use consistent spacing scale throughout
- Always use CSS variables, never hardcoded values
- Mobile spacing should be one step smaller where appropriate

## Button System

### Button Types

#### Primary Button (Amber)
- **Use**: Main actions, login, join community
- **Background**: `linear-gradient(135deg, var(--color-amber) 0%, var(--color-amber-dark) 100%)`
- **Text**: `var(--color-midnight)`
- **Shadow**: `0 4px 15px rgba(255, 191, 0, 0.4)`

#### Primary Alt Button (Electric)
- **Use**: Secondary important actions
- **Background**: `linear-gradient(135deg, var(--color-electric) 0%, var(--color-electric-dark) 100%)`
- **Text**: `var(--color-ivory)`
- **Shadow**: `0 4px 15px rgba(157, 78, 221, 0.4)`

#### Secondary Button
- **Use**: Less important actions, navigation
- **Background**: `transparent`
- **Border**: `2px solid var(--color-burgundy)`
- **Text**: `var(--color-burgundy)`

### Button Styling
```css
.btn {
    padding: 14px 32px;
    border-radius: 12px 6px 12px 6px; /* Signature asymmetric corners */
    font-family: var(--font-heading);
    font-weight: 600;
    font-size: 14px;
    text-transform: uppercase;
    letter-spacing: 1.5px;
    transition: all 0.3s ease;
    cursor: pointer;
    border: none;
}

.btn-large {
    padding: 18px 40px;
    font-size: 16px;
}
```

### Signature Corner Animation
**CRITICAL**: All buttons use the signature corner morphing animation:
- **Default**: `border-radius: 12px 6px 12px 6px`
- **Hover**: `border-radius: 6px 12px 6px 12px`
- **Transition**: `all 0.3s ease`
- **NO vertical movement** (no translateY)

## Signature Animations

### 1. Navigation Underline (Center Outward)
**Implementation**:
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

**Use Cases**:
- All navigation links
- Logo hover effect
- Utility bar links
- Any text link that needs emphasis

### 2. Button Corner Morphing
**Implementation**:
```css
.btn {
    border-radius: 12px 6px 12px 6px;
    transition: all 0.3s ease;
}

.btn:hover {
    border-radius: 6px 12px 6px 12px;
    /* Enhanced shadows and gradient reversal */
}
```

**Critical Rules**:
- NEVER add translateY (no vertical movement)
- Always reverse gradients on hover
- Maintain 0.3s timing
- Apply to ALL button types

### 3. Feature Icon Shape-Shifting
**Implementation**:
```css
.feature-icon {
    border-radius: 50% 20% 50% 20%;
    transition: all 0.5s ease;
}

.feature:hover .feature-icon {
    border-radius: 20% 50% 20% 50%;
    transform: rotate(5deg) scale(1.1);
}
```

**Use Cases**:
- Feature icons in "What Makes Us Special" section
- Any icon that needs interactive feedback
- Profile avatars or decorative elements

### 4. Card Hover Effects
**Implementation**:
```css
.event-card {
    transition: all 0.3s ease;
}

.event-card:hover {
    transform: translateY(-4px);
    box-shadow: 0 8px 24px rgba(0,0,0,0.1);
}
```

**Use Cases**:
- Event cards
- Content cards
- Interactive panels

### 5. Header Scroll Effects
**Implementation**:
```css
.header {
    backdrop-filter: blur(10px);
    border-bottom: 3px solid rgba(183, 109, 117, 0.3);
    transition: all 0.3s ease;
}

.header.scrolled {
    box-shadow: 0 4px 30px rgba(0,0,0,0.12);
    border-bottom-color: rgba(183, 109, 117, 0.5);
}
```

## Layout System

### Container Patterns
```css
.section {
    padding: var(--space-2xl) 40px;
    max-width: 1200px;
    margin: 0 auto;
}

.classes-events {
    background: var(--color-ivory);
    border-radius: 16px;
    box-shadow: 0 4px 20px rgba(0,0,0,0.08);
}

.cta {
    border-radius: 16px;
    box-shadow: 0 20px 60px rgba(0,0,0,0.3);
}
```

### Grid Systems
```css
.events-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(350px, 1fr));
    gap: var(--space-lg);
}

.features {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
    gap: var(--space-xl);
}

.footer-content {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
    gap: var(--space-xl);
}
```

## Component Specifications

### Navigation Header
- Sticky positioning with backdrop blur
- 3px rose-gold bottom border
- Signature underline animations
- Mobile hamburger menu

### Event Cards
- 16px border radius
- Gradient header backgrounds
- Hover elevation (-4px translateY)
- Status color coding

### Feature Icons
- 100px × 100px size
- Asymmetric border radius
- Shape-shifting hover animation
- Gradient backgrounds

### Footer
- Midnight background
- Rose gold top border gradient
- Four-column grid layout
- Simple color-change link hovers

## Responsive Design

### Breakpoints
- **Mobile**: max-width: 768px
- **Desktop**: min-width: 769px

### Mobile Modifications
- Typography scales down one level
- Padding reduces to 20px
- Navigation collapses to hamburger
- Button stacking in hero section
- Single column grids

## Background Patterns

### Subtle Rope Pattern
```css
body::before {
    background-image: url("data:image/svg+xml,%3Csvg...");
    opacity: 0.03;
    background-size: 200px 200px;
}
```

### Hero Animated Patterns
- Floating rope curves with 20s animation
- Radial gradient overlays
- Rotating circle patterns in CTA sections

## Accessibility Requirements

- WCAG 2.1 AA color contrast ratios
- Focus indicators on all interactive elements
- Screen reader friendly animations
- Reduced motion support
- Keyboard navigation support
- Semantic HTML structure

## Implementation Notes

### CSS Variable Usage
- ALWAYS use CSS variables for colors and spacing
- NEVER hardcode color values
- Maintain consistent naming conventions

### Animation Performance
- Use hardware acceleration where appropriate
- Prefer transform and opacity changes
- Maintain 60fps performance targets

### Browser Support
- Modern browsers with CSS Grid support
- Graceful degradation for older browsers
- Progressive enhancement approach

## Version History

- **v7.0** (2025-08-20): Initial comprehensive design system
- Extracted from approved Final Design v7 template
- Established as single source of truth

---

**Next Steps**: Use this design system as the foundation for all v7-based development. Reference the working template for implementation details.
# Spacing Standards - Design System v7

**Authority**: Based on approved Final Design v7 template
**Status**: Complete spacing specification
**Last Updated**: 2025-08-20

## Spacing Philosophy

The v7 spacing system creates **harmonious rhythm** and **visual breathing room** through a consistent 8px base unit system. This approach ensures perfect alignment, predictable layouts, and scalable design patterns.

### Design Principles
- **8px Base Unit**: All spacing derived from 8px increments
- **Predictable Scale**: Logical progression for all spacing needs
- **Responsive Harmony**: Spacing scales appropriately across devices
- **Component Consistency**: Internal and external spacing follows patterns
- **Visual Hierarchy**: Spacing reinforces content relationships

## Spacing Scale

### Base Scale (8px System)
```css
:root {
  --space-xs: 8px;   /* Fine details, small gaps */
  --space-sm: 16px;  /* Component internal spacing */
  --space-md: 24px;  /* Related element spacing */
  --space-lg: 32px;  /* Component spacing */
  --space-xl: 40px;  /* Section spacing */
  --space-2xl: 48px; /* Large section spacing */
  --space-3xl: 64px; /* Hero and major sections */
}
```

### Usage Guidelines

#### Extra Small (8px) - `--space-xs`
**Use for**:
- Icon margins within buttons
- Fine detail spacing
- Border gaps
- List item internal padding
- Form field helper text margins

**Examples**:
```css
.event-date {
  margin-bottom: var(--space-xs); /* 8px */
}

.detail-item {
  gap: var(--space-xs); /* 8px */
}
```

#### Small (16px) - `--space-sm`
**Use for**:
- Component internal padding
- Card content padding
- Button internal spacing
- Form field spacing
- Navigation item spacing

**Examples**:
```css
.header {
  padding: var(--space-sm) 40px; /* 16px vertical */
}

.event-content {
  padding: var(--space-lg); /* But mobile becomes space-sm */
}

@media (max-width: 768px) {
  .event-content {
    padding: var(--space-sm); /* 16px on mobile */
  }
}
```

#### Medium (24px) - `--space-md`
**Use for**:
- Related element spacing
- Section internal margins
- Feature icon bottom margin
- Button groups gap
- Card stack spacing

**Examples**:
```css
.feature-icon {
  margin: 0 auto var(--space-md); /* 24px bottom */
}

.hero-buttons {
  gap: var(--space-md); /* 24px between buttons */
}
```

#### Large (32px) - `--space-lg`
**Use for**:
- Component spacing
- Navigation gaps
- Event grid gaps
- Section internal spacing
- Major element separation

**Examples**:
```css
.nav {
  gap: var(--space-lg); /* 32px between nav items */
}

.events-grid {
  gap: var(--space-lg); /* 32px grid gaps */
}

.utility-bar a {
  margin-left: var(--space-lg); /* 32px */
}
```

#### Extra Large (40px) - `--space-xl`
**Use for**:
- Section spacing
- Container padding
- Major layout divisions
- Hero content margins
- Feature grid gaps

**Examples**:
```css
.section {
  padding: var(--space-2xl) var(--space-xl); /* 40px horizontal */
}

.section-title {
  margin-bottom: var(--space-xl); /* 40px */
}

.features {
  gap: var(--space-xl); /* 40px */
}
```

#### 2X Large (48px) - `--space-2xl`
**Use for**:
- Large section spacing
- Major content divisions
- Hero section padding
- Container vertical spacing

**Examples**:
```css
.section {
  padding: var(--space-2xl) var(--space-xl); /* 48px vertical */
}

.classes-events {
  margin-top: var(--space-2xl); /* 48px */
}
```

#### 3X Large (64px) - `--space-3xl`
**Use for**:
- Hero section major spacing
- Page-level divisions
- Maximum visual separation
- Major layout breaks

**Examples**:
```css
.hero {
  padding: var(--space-3xl) 40px var(--space-2xl); /* 64px top */
}
```

## Responsive Spacing

### Mobile Adjustments
```css
/* Desktop spacing */
.section {
  padding: var(--space-2xl) 40px; /* 48px vertical, 40px horizontal */
}

/* Mobile adjustments */
@media (max-width: 768px) {
  .section {
    padding: var(--space-xl) 20px; /* 40px vertical, 20px horizontal */
  }
  
  .classes-events {
    padding: var(--space-xl) 20px; /* Reduce from 48px to 40px */
    margin-top: var(--space-xl); /* Reduce from 48px to 40px */
  }
}
```

### Responsive Scale Pattern
- **Desktop**: Full spacing scale
- **Mobile**: Generally one step smaller
- **Exception**: xs and sm remain consistent
- **Container padding**: Specific mobile values (40px → 20px)

## Component Spacing Patterns

### Navigation Header
```css
.header {
  padding: var(--space-sm) 40px; /* 16px vertical, 40px horizontal */
}

.header.scrolled {
  padding: 12px 40px; /* Slight reduction when scrolled */
}

.nav {
  gap: var(--space-lg); /* 32px between nav items */
}

@media (max-width: 768px) {
  .header {
    padding: var(--space-sm) 20px; /* 16px vertical, 20px horizontal */
  }
}
```

### Utility Bar
```css
.utility-bar {
  padding: 12px 40px; /* Custom value for compact feel */
}

.utility-bar a {
  margin-left: var(--space-lg); /* 32px between links */
}

@media (max-width: 768px) {
  .utility-bar {
    padding: 10px 20px; /* Slightly smaller on mobile */
  }
  
  .utility-bar a {
    margin-left: var(--space-md); /* 24px on mobile */
  }
}
```

### Event Cards
```css
.event-content {
  padding: var(--space-lg); /* 32px all sides */
}

.event-date {
  margin-bottom: var(--space-xs); /* 8px */
}

.event-description {
  margin-bottom: var(--space-md); /* 24px */
}

.event-details {
  margin-bottom: var(--space-md); /* 24px */
}

.event-footer {
  padding-top: var(--space-md); /* 24px */
}

@media (max-width: 768px) {
  .event-content {
    padding: var(--space-md); /* 24px on mobile */
  }
}
```

### Hero Section
```css
.hero {
  padding: var(--space-3xl) 40px var(--space-2xl); /* 64px top, 48px bottom */
}

.hero-tagline {
  margin-bottom: var(--space-sm); /* 16px */
}

.hero h1 {
  margin-bottom: var(--space-md); /* 24px */
}

.hero p {
  margin-bottom: var(--space-xl); /* 40px */
}

.hero-buttons {
  gap: var(--space-md); /* 24px */
}
```

### Feature Sections
```css
.features {
  gap: var(--space-xl); /* 40px */
  margin-bottom: var(--space-xl); /* 40px */
}

.feature-icon {
  margin: 0 auto var(--space-md); /* 24px bottom */
}

.feature h3 {
  margin-bottom: var(--space-sm); /* 16px */
}
```

### Section Titles
```css
.section-title {
  margin-bottom: var(--space-xl); /* 40px */
}

.section-title::after {
  margin: var(--space-sm) auto 0; /* 16px top */
}
```

## Layout Spacing

### Container System
```css
/* Main content containers */
.section {
  padding: var(--space-2xl) 40px; /* 48px vertical, 40px horizontal */
  max-width: 1200px;
  margin: 0 auto;
}

/* Special containers */
.classes-events {
  padding: var(--space-2xl) 40px; /* 48px vertical, 40px horizontal */
  margin-top: var(--space-2xl); /* 48px from previous section */
}

.hero {
  padding: var(--space-3xl) 40px var(--space-2xl); /* 64px top, 48px bottom */
}

.footer {
  padding: var(--space-2xl) 40px var(--space-xl); /* 48px top, 40px bottom */
  margin-top: var(--space-2xl); /* 48px from previous section */
}
```

### Grid Systems
```css
/* Event cards grid */
.events-grid {
  gap: var(--space-lg); /* 32px */
  margin-bottom: var(--space-xl); /* 40px */
}

/* Feature icons grid */
.features {
  gap: var(--space-xl); /* 40px */
  margin-bottom: var(--space-xl); /* 40px */
}

/* Footer content grid */
.footer-content {
  gap: var(--space-xl); /* 40px */
  margin-bottom: var(--space-xl); /* 40px */
}
```

## Special Spacing Cases

### Custom Values (When to Break the System)
Some elements require custom spacing for optical alignment:

```css
/* Header scroll state - custom value for visual perfection */
.header.scrolled {
  padding: 12px 40px; /* Custom 12px instead of 16px */
}

/* Utility bar - custom for compact feel */
.utility-bar {
  padding: 12px 40px; /* Custom for UI density */
}

/* Mobile utility bar */
@media (max-width: 768px) {
  .utility-bar {
    padding: 10px 20px; /* Custom for mobile density */
  }
}

/* Underline positioning */
.nav-item::after {
  bottom: -4px; /* Custom for visual alignment */
}

.utility-bar a::after {
  bottom: -2px; /* Custom for smaller scale */
}
```

### Optical Adjustments
Typography and visual elements sometimes need optical spacing adjustments:

```css
/* Logo positioning */
.logo::after {
  bottom: -4px; /* Visual alignment */
}

/* Section title underlines */
.section-title::after {
  margin: var(--space-sm) auto 0; /* 16px + auto centering */
}

/* Text shadow offset */
.event-image-title {
  text-shadow: 0 2px 4px rgba(0, 0, 0, 0.3); /* Custom shadow spacing */
}
```

## TypeScript Implementation

### Design Token Types
```typescript
export const spacing = {
  xs: '8px',
  sm: '16px',
  md: '24px',
  lg: '32px',
  xl: '40px',
  '2xl': '48px',
  '3xl': '64px',
} as const;

export type SpacingToken = keyof typeof spacing;
export type SpacingValue = typeof spacing[SpacingToken];
```

### React Component Props
```typescript
interface SpacingProps {
  p?: SpacingToken; // padding
  px?: SpacingToken; // padding horizontal
  py?: SpacingToken; // padding vertical
  pt?: SpacingToken; // padding top
  pr?: SpacingToken; // padding right
  pb?: SpacingToken; // padding bottom
  pl?: SpacingToken; // padding left
  m?: SpacingToken; // margin
  mx?: SpacingToken; // margin horizontal
  my?: SpacingToken; // margin vertical
  mt?: SpacingToken; // margin top
  mr?: SpacingToken; // margin right
  mb?: SpacingToken; // margin bottom
  ml?: SpacingToken; // margin left
  gap?: SpacingToken; // flex/grid gap
}
```

### Mantine Integration
```typescript
import { MantineProvider, createTheme } from '@mantine/core';

const theme = createTheme({
  spacing: {
    xs: '8px',
    sm: '16px',
    md: '24px',
    lg: '32px',
    xl: '40px',
    '2xl': '48px',
    '3xl': '64px',
  },
});

// Usage in components
const MyComponent = () => (
  <Box p="lg" mb="xl"> {/* 32px padding, 40px margin bottom */}
    Content
  </Box>
);
```

## Validation and Testing

### Spacing Consistency Checks
```javascript
// Validate spacing usage in components
const validateSpacing = (element) => {
  const computedStyles = getComputedStyle(element);
  const margins = [
    computedStyles.marginTop,
    computedStyles.marginRight,
    computedStyles.marginBottom,
    computedStyles.marginLeft
  ];
  const paddings = [
    computedStyles.paddingTop,
    computedStyles.paddingRight,
    computedStyles.paddingBottom,
    computedStyles.paddingLeft
  ];
  
  const validValues = ['8px', '16px', '24px', '32px', '40px', '48px', '64px', '0px', 'auto'];
  
  [...margins, ...paddings].forEach(value => {
    if (!validValues.includes(value) && value !== '0px') {
      console.warn(`Non-standard spacing value: ${value}`);
    }
  });
};
```

### Responsive Testing
```css
/* Debug responsive spacing */
@media (max-width: 768px) {
  * {
    outline: 1px solid red; /* Visualize spacing on mobile */
  }
}
```

## Common Patterns

### Stack Spacing (Vertical)
```css
.stack > * + * {
  margin-top: var(--space-md); /* 24px between stacked elements */
}
```

### Inline Spacing (Horizontal)
```css
.inline > * + * {
  margin-left: var(--space-sm); /* 16px between inline elements */
}
```

### Section Dividers
```css
.section + .section {
  margin-top: var(--space-2xl); /* 48px between sections */
}
```

## Do's and Don'ts

### ✅ DO
- Use CSS variables for all spacing values
- Follow the 8px base unit system
- Scale spacing down on mobile appropriately
- Maintain consistent component internal spacing
- Use spacing to create visual hierarchy
- Test spacing on different screen sizes

### ❌ DON'T
- Use arbitrary pixel values outside the system
- Mix spacing systems (rem, em, px randomly)
- Ignore responsive spacing adjustments
- Create visual imbalance with inconsistent spacing
- Use spacing smaller than 8px (except for borders)
- Forget to consider optical adjustments

## Accessibility Considerations

### Touch Targets
- Minimum 44px touch targets on mobile
- Adequate spacing between interactive elements
- Consider motor impairments in spacing decisions

### Visual Clarity
- Sufficient spacing for content scanning
- Clear visual grouping through spacing
- Avoid cramped layouts that strain vision

### Implementation
```css
/* Ensure minimum touch targets */
@media (max-width: 768px) {
  .btn {
    min-height: 44px; /* Accessibility requirement */
    padding: var(--space-sm) var(--space-md); /* 16px vertical, 24px horizontal */
  }
  
  .nav-item {
    padding: var(--space-sm) 0; /* 16px vertical for easier tapping */
  }
}
```

## Performance Impact

### CSS Custom Properties
- Minimal performance impact
- Enables dynamic spacing adjustments
- Supports theming and responsive design

### Layout Calculations
- Consistent spacing reduces layout thrashing
- Predictable values improve browser optimization
- Grid and flexbox work efficiently with consistent gaps

---

**Next Steps**: Implement the spacing system using CSS custom properties. Test responsive behavior across all breakpoints and validate consistency.

**Tools**: [Design Tokens v7](../current/design-tokens-v7.json) | [Working Template](../current/homepage-template-v7.html)
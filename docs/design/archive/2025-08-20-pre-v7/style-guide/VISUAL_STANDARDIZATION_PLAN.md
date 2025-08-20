# Visual Style Standardization Plan

## Executive Summary
Analysis of 22 wireframe files revealed:
- **45+ color values** that can be reduced to ~20
- **23 font sizes** that can be reduced to 9
- **Inconsistent spacing** despite a clear 4px base unit
- **15+ button variations** that need consolidation
- **Missing interactive states** for accessibility

## Phase 1: Create Core Design System (2-3 hours)

### 1.1 Consolidate Color Palette
**Current State**: 45+ unique colors with multiple grays, inconsistent formats
**Target State**: 20 CSS variables organized by purpose

```css
/* Primary Palette */
--color-primary: #8B4513;
--color-primary-hover: #6B3410;
--color-primary-light: #A0522D;

/* Neutral Scale (from 9 grays to 5) */
--color-gray-900: #1a1a1a;  /* Primary text */
--color-gray-700: #333;      /* Secondary text */
--color-gray-500: #666;      /* Tertiary text */
--color-gray-300: #ccc;      /* Borders light */
--color-gray-200: #e0e0e0;   /* Borders default */
--color-gray-100: #f5f5f5;   /* Backgrounds */

/* Status Colors (consolidate variations) */
--color-success: #2e7d32;    /* Was: #2e7d32, #4caf50, #28a745 */
--color-warning: #f57c00;    /* Was: #ffc107, #f57c00, #e65100, #ff9800 */
--color-error: #d32f2f;      /* Was: #d32f2f, #ff6b6b, #dc3545 */
--color-info: #1976d2;       /* Consistent */
```

### 1.2 Establish Typography Scale
**Current**: 23 different sizes
**Target**: 9-step scale based on frequency analysis

```css
/* Typography Scale */
--text-xs: 12px;    /* Small labels, help text */
--text-sm: 14px;    /* Secondary text, form labels */
--text-base: 16px;  /* Body text default */
--text-lg: 18px;    /* Emphasized body text */
--text-xl: 20px;    /* Section headings */
--text-2xl: 24px;   /* Page sections */
--text-3xl: 28px;   /* Page titles */
--text-4xl: 32px;   /* Hero headings */
--text-5xl: 48px;   /* Landing page only */
```

### 1.3 Codify Spacing System
**Base Unit**: 4px (confirmed by analysis)

```css
/* Spacing Scale */
--space-1: 4px;
--space-2: 8px;
--space-3: 12px;
--space-4: 16px;
--space-5: 20px;
--space-6: 24px;
--space-8: 32px;
--space-10: 40px;
--space-15: 60px;
```

## Phase 2: Component Standardization (3-4 hours)

### 2.1 Button System
**Current**: 15+ variations across files
**Target**: 4 types × 3 sizes × 4 states

```css
/* Button Base */
.btn {
  padding: var(--space-3) var(--space-5);
  border-radius: 6px;
  font-weight: 500;
  transition: all 200ms;
}

/* Variants: primary, secondary, warning, danger */
/* Sizes: sm, base, lg */
/* States: default, hover, active, disabled */
```

### 2.2 Form Controls
**Consolidate** all input, textarea, select styles:
- Consistent 2px border (not 1px)
- Uniform padding: 12px
- Standard focus state with primary color

### 2.3 Card Components
**Standardize** the 6 card variations:
- Event cards
- Member cards
- Ticket cards
- Info cards
- All use: padding 20px, border-radius 8px, consistent shadow

## Phase 3: File-by-File Updates (4-5 hours)

### High Priority Files (Most Inconsistent)
1. **landing.html** - Uses custom styles, needs major updates
2. **user-dashboard.html** - Mixed button styles, inconsistent spacing
3. **admin-events.html** - Different component patterns
4. **auth pages** - Missing consistent navigation

### Update Strategy
1. Add link to `design-system.css` in each file
2. Replace inline styles with CSS variables
3. Update class names to match new system
4. Test responsive behavior

### Specific Replacements

#### Colors
```html
<!-- Before -->
<div style="color: #333">
<button style="background: #8B4513">

<!-- After -->
<div style="color: var(--color-gray-700)">
<button class="btn btn-primary">
```

#### Spacing
```html
<!-- Before -->
<div style="padding: 20px; margin-bottom: 30px">

<!-- After -->
<div style="padding: var(--space-5); margin-bottom: var(--space-8)">
```

## Phase 4: Add Missing States (2 hours)

### Focus States (Accessibility)
```css
.btn:focus,
.form-input:focus {
  outline: 2px solid var(--color-primary);
  outline-offset: 2px;
}
```

### Disabled States
```css
.btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}
```

### Loading States
```css
.btn.loading {
  position: relative;
  color: transparent;
}
.btn.loading::after {
  /* Spinner styles */
}
```

## Phase 5: Documentation & Testing (2 hours)

### Create Style Guide Page
- Live component examples
- Copy/paste code snippets
- Do's and don'ts
- Accessibility notes

### Testing Checklist
- [ ] All colors use CSS variables
- [ ] Typography follows scale
- [ ] Spacing uses 4px base unit
- [ ] Components have all states
- [ ] Mobile responsive works
- [ ] Accessibility standards met

## Implementation Order

1. **Day 1**: Create `design-system.css` with all tokens and base components
2. **Day 2**: Update high-priority files (landing, dashboard, auth)
3. **Day 3**: Update remaining files systematically
4. **Day 4**: Add missing states, create documentation

## Success Metrics
- Reduce 45+ colors to 20 variables
- Reduce 23 font sizes to 9
- Standardize all buttons to 4 types
- 100% of interactive elements have focus states
- All spacing follows 4px grid

## Tools Needed
- Text editor with find/replace
- Browser dev tools for testing
- Accessibility checker (WAVE or axe)
- Color contrast analyzer

This plan provides a systematic approach to standardizing the visual design while maintaining the existing functionality and improving accessibility.
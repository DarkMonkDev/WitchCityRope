# Animation Standards - Design System v7

**Authority**: Based on approved Final Design v7 template
**Status**: Comprehensive implementation guide
**Last Updated**: 2025-08-20

## Overview

This document provides comprehensive specifications for all signature animations in the v7 design system. These animations are REQUIRED for v7 compliance and create the distinctive, sophisticated user experience.

## Animation Philosophy

- **Purposeful Motion**: Every animation serves a functional purpose
- **Smooth Sophistication**: Elegant, non-jarring transitions
- **Performance First**: Hardware-accelerated transforms
- **Accessibility Aware**: Respects `prefers-reduced-motion`
- **Consistent Timing**: Standardized easing and duration

## Core Animation Principles

### Timing Standards
```css
/* Standard timing functions */
--timing-fast: 0.3s ease;     /* UI interactions, hovers */
--timing-medium: 0.4s ease;   /* Secondary fills, complex changes */
--timing-slow: 0.5s ease;     /* Feature transforms, dramatic effects */
--timing-shimmer: 0.5s ease;  /* Shimmer and sweep effects */

/* Special animation durations */
--animation-float: 20s ease-in-out infinite;  /* Background patterns */
--animation-rotate: 30s linear infinite;      /* Rotating patterns */
```

### Performance Optimization
```css
/* Use hardware acceleration for transforms */
.animated-element {
  will-change: transform; /* Hint to browser for optimization */
  backface-visibility: hidden; /* Smooth animations on mobile */
}

/* Prefer transform and opacity over layout changes */
.good-animation {
  transform: translateY(-4px); /* ✓ Uses GPU */
}

.bad-animation {
  top: -4px; /* ✗ Causes layout recalculation */
}
```

## Signature Animation Catalog

### 1. Navigation Underline (Center-Outward Expansion)

**Description**: The signature underline animation that expands from center outward with gradient effect.

**Visual Effect**: 
- Underline starts at 0 width in center
- Expands outward symmetrically on hover
- Uses gradient for visual sophistication
- Creates premium, polished feel

**Implementation**:
```css
.nav-underline {
  position: relative;
  transition: color 0.3s ease;
}

.nav-underline::after {
  content: '';
  position: absolute;
  bottom: -4px;
  left: 50%;
  transform: translateX(-50%); /* Center positioning */
  width: 0;
  height: 2px;
  background: linear-gradient(90deg, transparent, var(--color-burgundy), transparent);
  transition: width 0.3s ease;
}

.nav-underline:hover::after {
  width: 100%; /* Expand to full width */
}
```

**Variants**:
```css
/* Logo underline variant */
.logo-underline::after {
  height: 2px;
  background: linear-gradient(90deg, transparent, var(--color-burgundy), transparent);
}

/* Utility bar underline */
.utility-underline::after {
  height: 1px;
  background: var(--color-rose-gold);
  bottom: -2px;
}

/* Footer link underline */
.footer-underline::after {
  height: 1px;
  background: var(--color-rose-gold);
  bottom: -1px;
}
```

**Use Cases**:
- Primary navigation links
- Logo hover effect
- Utility bar links
- Footer links
- Any text link requiring emphasis

**Accessibility**:
```css
@media (prefers-reduced-motion: reduce) {
  .nav-underline::after {
    transition: none;
  }
  
  .nav-underline:hover::after {
    width: 100%;
  }
}
```

### 2. Button Corner Morphing (Asymmetric Animation)

**Description**: The distinctive corner animation that morphs asymmetric border-radius on hover.

**Visual Effect**:
- Default: Corners pulled top-left and bottom-right
- Hover: Corners shift to top-right and bottom-left
- Creates dynamic, edgy aesthetic
- NO vertical movement (critical requirement)

**Implementation**:
```css
.btn {
  border-radius: 12px 6px 12px 6px; /* Default: TL BR pulled */
  transition: all 0.3s ease;
  position: relative;
  overflow: hidden;
  /* CRITICAL: No transform on default state */
}

.btn:hover {
  border-radius: 6px 12px 6px 12px; /* Hover: TR BL pulled */
  /* CRITICAL: NO translateY - no vertical movement */
  /* Only shadow and gradient changes allowed */
}
```

**Button Type Variants**:
```css
/* Primary Amber Button */
.btn-primary {
  background: linear-gradient(135deg, var(--color-amber) 0%, var(--color-amber-dark) 100%);
  box-shadow: 0 4px 15px rgba(255, 191, 0, 0.4);
}

.btn-primary:hover {
  background: linear-gradient(135deg, var(--color-amber-dark) 0%, var(--color-amber) 100%);
  box-shadow: 0 6px 25px rgba(255, 191, 0, 0.5);
  /* Corner morphing happens automatically from base .btn */
}

/* Electric Purple Button */
.btn-electric {
  background: linear-gradient(135deg, var(--color-electric) 0%, var(--color-electric-dark) 100%);
  box-shadow: 0 4px 15px rgba(157, 78, 221, 0.4);
}

.btn-electric:hover {
  background: linear-gradient(135deg, var(--color-electric-dark) 0%, var(--color-electric) 100%);
  box-shadow: 0 6px 25px rgba(157, 78, 221, 0.5);
}

/* Secondary Button with Fill Effect */
.btn-secondary {
  background: transparent;
  color: var(--color-burgundy);
  border: 2px solid var(--color-burgundy);
  position: relative;
  z-index: 1;
}

.btn-secondary::before {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  width: 0;
  height: 100%;
  background: var(--color-burgundy);
  transition: width 0.4s ease;
  z-index: -1;
}

.btn-secondary:hover {
  color: var(--color-ivory);
}

.btn-secondary:hover::before {
  width: 100%;
}
```

**Critical Rules**:
- ❌ NEVER use `translateY` on buttons
- ✅ ALWAYS reverse gradients on hover
- ✅ ALWAYS use 0.3s timing
- ✅ ALWAYS apply to ALL button types

**Use Cases**:
- All primary action buttons
- All secondary action buttons
- CTA buttons in hero and sections
- Form submit buttons

### 3. Feature Icon Shape-Shifting

**Description**: Organic border-radius transformation with rotation and scale.

**Visual Effect**:
- Icons have organic, asymmetric shape
- Shape morphs on hover with rotation
- Subtle scale increase adds emphasis
- Radial highlight effect appears

**Implementation**:
```css
.feature-icon {
  width: 100px;
  height: 100px;
  background: linear-gradient(135deg, var(--color-burgundy) 0%, var(--color-rose-gold) 100%);
  border-radius: 50% 20% 50% 20%; /* Organic asymmetric shape */
  display: flex;
  align-items: center;
  justify-content: center;
  color: white;
  font-size: 48px;
  box-shadow: 0 10px 30px rgba(136, 1, 36, 0.2);
  position: relative;
  overflow: hidden;
  transition: all 0.5s ease; /* Slower for dramatic effect */
}

/* Radial highlight overlay */
.feature-icon::before {
  content: '';
  position: absolute;
  top: -50%;
  left: -50%;
  width: 200%;
  height: 200%;
  background: radial-gradient(circle, rgba(255,255,255,0.3) 0%, transparent 70%);
  opacity: 0;
  transition: opacity 0.3s ease;
}

/* Hover state transformation */
.feature:hover .feature-icon {
  border-radius: 20% 50% 20% 50%; /* Shape morphs */
  transform: rotate(5deg) scale(1.1); /* Rotation + scale */
}

.feature:hover .feature-icon::before {
  opacity: 1; /* Highlight appears */
}
```

**Animation Breakdown**:
1. **Shape Change**: `border-radius` morphs organically
2. **Rotation**: 5-degree clockwise rotation
3. **Scale**: 1.1x size increase (10% larger)
4. **Highlight**: Radial overlay fades in
5. **Duration**: 0.5s for dramatic effect

**Use Cases**:
- Feature icons in "What Makes Us Special"
- Service highlight icons
- Profile avatars with interaction
- Decorative interactive elements

### 4. Card Hover Elevation

**Description**: Subtle lift effect for interactive cards.

**Visual Effect**:
- Cards lift slightly on hover
- Shadow increases for depth perception
- Smooth, professional interaction feedback

**Implementation**:
```css
.event-card {
  background: white;
  border-radius: 16px;
  box-shadow: 0 4px 12px rgba(0,0,0,0.05);
  transition: all 0.3s ease;
  cursor: pointer;
}

.event-card:hover {
  transform: translateY(-4px); /* 4px lift */
  box-shadow: 0 8px 24px rgba(0,0,0,0.1); /* Enhanced shadow */
}
```

**Shadow Progression**:
- **Default**: `0 4px 12px rgba(0,0,0,0.05)` - Subtle presence
- **Hover**: `0 8px 24px rgba(0,0,0,0.1)` - Clear elevation

**Use Cases**:
- Event cards
- Content cards
- Interactive panels
- Clickable sections

### 5. Header Scroll Effects

**Description**: Progressive enhancement as user scrolls.

**Visual Effect**:
- Header becomes more prominent on scroll
- Border intensifies
- Shadow increases
- Backdrop blur maintains

**Implementation**:
```css
.header {
  background: rgba(255, 248, 240, 0.95);
  backdrop-filter: blur(10px);
  box-shadow: 0 2px 20px rgba(0,0,0,0.08);
  padding: var(--space-sm) 40px;
  border-bottom: 3px solid rgba(183, 109, 117, 0.3);
  transition: all 0.3s ease;
  position: sticky;
  top: 0;
  z-index: 100;
}

.header.scrolled {
  padding: 12px 40px; /* Slightly more compact */
  box-shadow: 0 4px 30px rgba(0,0,0,0.12); /* More prominent shadow */
  border-bottom-color: rgba(183, 109, 117, 0.5); /* More visible border */
}
```

**JavaScript Implementation**:
```javascript
window.addEventListener('scroll', function() {
  const header = document.getElementById('header');
  if (window.scrollY > 100) {
    header.classList.add('scrolled');
  } else {
    header.classList.remove('scrolled');
  }
});
```

**Progressive Enhancement**:
- **0-100px scroll**: Default header state
- **100px+ scroll**: Enhanced scrolled state
- **Smooth transition**: 0.3s ease between states

### 6. Logo Hover Enhancement

**Description**: Subtle scale with underline animation.

**Implementation**:
```css
.logo {
  font-family: var(--font-heading);
  font-size: 30px;
  font-weight: 800;
  color: var(--color-burgundy);
  text-decoration: none;
  letter-spacing: -0.5px;
  transition: all 0.3s ease;
  position: relative;
}

.logo::after {
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

.logo:hover {
  color: var(--color-burgundy-light);
  transform: scale(1.02); /* Subtle 2% scale increase */
}

.logo:hover::after {
  width: 100%;
}
```

## Background Pattern Animations

### Floating Rope Patterns
```css
@keyframes float {
  0%, 100% { 
    transform: translate(0, 0) rotate(0deg); 
  }
  33% { 
    transform: translate(-30px, -20px) rotate(1deg); 
  }
  66% { 
    transform: translate(20px, -10px) rotate(-1deg); 
  }
}

.hero::before {
  animation: float 20s ease-in-out infinite;
}
```

### Rotating Circle Patterns
```css
@keyframes rotate {
  0% { transform: rotate(0deg); }
  100% { transform: rotate(360deg); }
}

.cta::before {
  animation: rotate 30s linear infinite;
}
```

## Special Effects

### Shimmer Effect for Primary Buttons
```css
.btn-primary::before {
  content: '';
  position: absolute;
  top: 0;
  left: -100%;
  width: 100%;
  height: 100%;
  background: linear-gradient(90deg, transparent, rgba(255,255,255,0.2), transparent);
  transition: left 0.5s ease;
}

.btn-primary:hover::before {
  left: 100%; /* Shimmer sweep effect */
}
```

## Accessibility Considerations

### Reduced Motion Support
```css
@media (prefers-reduced-motion: reduce) {
  * {
    animation-duration: 0.01ms !important;
    animation-iteration-count: 1 !important;
    transition-duration: 0.01ms !important;
  }
  
  /* Maintain hover states without animation */
  .nav-item:hover::after {
    width: 100%;
    transition: none;
  }
  
  .btn:hover {
    border-radius: 6px 12px 6px 12px;
    transition: none;
  }
}
```

### Screen Reader Compatibility
```css
/* Ensure animations don't interfere with screen readers */
.animated-element {
  speak: none; /* Prevent animation announcements */
}

/* Use aria-hidden for decorative animations */
.decorative-animation {
  aria-hidden: true;
}
```

## Performance Guidelines

### Hardware Acceleration
```css
/* Enable GPU acceleration for complex animations */
.feature-icon,
.event-card,
.btn {
  will-change: transform;
  transform: translateZ(0); /* Force hardware acceleration */
}
```

### Animation Cleanup
```css
/* Remove will-change after animation completes */
.feature-icon:not(:hover) {
  will-change: auto;
}
```

### Mobile Performance
```css
/* Simplified animations on mobile */
@media (max-width: 768px) {
  .feature-icon {
    transition: all 0.3s ease; /* Faster on mobile */
  }
  
  .feature:hover .feature-icon {
    transform: scale(1.05); /* Less rotation on mobile */
  }
}
```

## Implementation Checklist

### Required Animations
- [ ] Navigation underline (center-outward)
- [ ] Button corner morphing (NO vertical movement)
- [ ] Feature icon shape-shifting
- [ ] Card hover elevation
- [ ] Header scroll effects
- [ ] Logo hover enhancement

### Quality Assurance
- [ ] All animations use standardized timing
- [ ] Hardware acceleration enabled where needed
- [ ] Reduced motion preferences respected
- [ ] Mobile performance optimized
- [ ] No layout thrashing animations
- [ ] Smooth 60fps performance

### Browser Testing
- [ ] Chrome (latest)
- [ ] Firefox (latest)
- [ ] Safari (latest)
- [ ] Edge (latest)
- [ ] Mobile Safari
- [ ] Chrome Android

## Common Mistakes to Avoid

### ❌ DON'T
- Use `translateY` on buttons (breaks signature corner animation)
- Create animations that cause layout recalculation
- Use different timing than specified
- Skip reduced motion support
- Animate width/height directly (use transform instead)
- Create overly complex animation chains

### ✅ DO
- Use CSS variables for consistent timing
- Prefer transform and opacity changes
- Test on low-powered devices
- Include accessibility considerations
- Follow the established animation patterns
- Optimize for mobile performance

## Testing Guidelines

### Performance Testing
```javascript
// Monitor animation performance
const observer = new PerformanceObserver((list) => {
  list.getEntries().forEach((entry) => {
    if (entry.duration > 16.67) { // > 60fps
      console.warn('Animation frame drop:', entry);
    }
  });
});

observer.observe({entryTypes: ['measure']});
```

### Animation Validation
```javascript
// Verify required animations are present
function validateAnimations() {
  const requiredAnimations = [
    '.nav-item::after',
    '.btn:hover',
    '.feature-icon:hover',
    '.event-card:hover',
    '.logo:hover'
  ];
  
  requiredAnimations.forEach(selector => {
    const element = document.querySelector(selector);
    if (!element) {
      console.error(`Missing animation: ${selector}`);
    }
  });
}
```

---

**Next Steps**: Implement these animations in your components following the exact specifications. Test thoroughly for performance and accessibility compliance.

**Reference**: Study the working implementations in [homepage-template-v7.html](../current/homepage-template-v7.html) for complete examples.
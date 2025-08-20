# Typography Standards - Design System v7

**Authority**: Based on approved Final Design v7 template
**Status**: Complete typography specification
**Last Updated**: 2025-08-20

## Typography Philosophy

The v7 typography system balances **sophisticated elegance** with **modern readability**. Four carefully selected Google Fonts work together to create a cohesive hierarchy that supports the rope bondage community's need for both gravitas and approachability.

### Design Principles
- **Hierarchical Clarity**: Clear visual hierarchy guides user attention
- **Sophisticated Elegance**: Serif display adds gravitas and luxury
- **Modern Readability**: Sans-serif body text ensures accessibility
- **Brand Personality**: Cursive accents add warmth and community feel
- **Performance First**: Optimized font loading for web performance

## Font Stack

### Display Font - Bodoni Moda
```css
--font-display: 'Bodoni Moda', serif;
```

**Usage**: Hero headlines, dramatic statements only
**Character**: Classic, elegant, sophisticated, high-contrast serif
**Google Fonts**: `Bodoni+Moda:opsz,wght@6..96,400;6..96,700;6..96,900`
**Optical Sizing**: 6-96pt range for optimal rendering
**Weights**: 400 (Regular), 700 (Bold), 900 (Black)

**When to Use**:
- Hero section main headlines
- Major announcements
- Dramatic emphasis (sparingly)

**When NOT to Use**:
- Body text (readability issues)
- Navigation (too decorative)
- Small text (loses legibility)
- Long content blocks

### Heading Font - Montserrat
```css
--font-heading: 'Montserrat', sans-serif;
```

**Usage**: Section titles, navigation, labels, buttons
**Character**: Modern, geometric, clean, professional
**Google Fonts**: `Montserrat:wght@400;500;600;700;800`
**Weights**: 400 (Regular), 500 (Medium), 600 (Semi-bold), 700 (Bold), 800 (Extra-bold)

**When to Use**:
- Section headings (h2, h3)
- Navigation links
- Button text
- Form labels
- Card titles
- Any text requiring emphasis

### Body Font - Source Sans 3
```css
--font-body: 'Source Sans 3', sans-serif;
```

**Usage**: Body text, descriptions, content
**Character**: Highly readable, neutral, accessible
**Google Fonts**: `Source+Sans+3:wght@300;400;600`
**Weights**: 300 (Light), 400 (Regular), 600 (Semi-bold)

**When to Use**:
- All body text
- Descriptions
- Content blocks
- Captions
- Secondary information

### Accent Font - Satisfy
```css
--font-accent: 'Satisfy', cursive;
```

**Usage**: Taglines, decorative text only (use sparingly)
**Character**: Handwritten, warm, personal, friendly
**Google Fonts**: `Satisfy`
**Weight**: 400 (Regular only)

**When to Use**:
- Hero taglines ("Where curiosity meets connection")
- Decorative quotes
- Personal touches
- Warm, welcoming messages

**When NOT to Use**:
- Long text blocks (readability)
- Navigation or UI elements
- Technical information
- Anywhere legibility is critical

## Typography Scale

### Desktop Sizes
```css
/* Display/Hero Headlines */
.hero-title {
  font-family: var(--font-heading); /* Note: Uses heading font, not display */
  font-size: 64px;
  font-weight: 800;
  line-height: 1.1;
  letter-spacing: -1px;
  text-transform: uppercase;
}

/* Section Titles */
.section-title {
  font-family: var(--font-heading);
  font-size: 48px;
  font-weight: 800;
  line-height: 1.2;
  letter-spacing: 3px;
  text-transform: uppercase;
}

/* Taglines */
.tagline {
  font-family: var(--font-accent);
  font-size: 28px;
  font-weight: 400;
  line-height: 1.3;
  letter-spacing: normal;
}

/* Card Titles */
.card-title {
  font-family: var(--font-heading);
  font-size: 24px;
  font-weight: 700;
  line-height: 1.3;
  letter-spacing: 1px;
  text-transform: uppercase;
}

/* Hero Subtitles */
.hero-subtitle {
  font-family: var(--font-body);
  font-size: 22px;
  font-weight: 300;
  line-height: 1.6;
  letter-spacing: normal;
}

/* Body Text */
.body-text {
  font-family: var(--font-body);
  font-size: 16px;
  font-weight: 400;
  line-height: 1.7;
  letter-spacing: normal;
}

/* Navigation */
.nav-text {
  font-family: var(--font-heading);
  font-size: 15px;
  font-weight: 500;
  line-height: 1.4;
  letter-spacing: 1px;
  text-transform: uppercase;
}

/* Button Text */
.button-text {
  font-family: var(--font-heading);
  font-size: 14px;
  font-weight: 600;
  line-height: 1.2;
  letter-spacing: 1.5px;
  text-transform: uppercase;
}

.button-large-text {
  font-family: var(--font-heading);
  font-size: 16px;
  font-weight: 700;
  line-height: 1.2;
  letter-spacing: 1.5px;
  text-transform: uppercase;
}

/* Small Text */
.small-text {
  font-family: var(--font-heading);
  font-size: 14px;
  font-weight: 600;
  line-height: 1.4;
  letter-spacing: 0.5px;
  text-transform: uppercase;
}

/* Utility Bar */
.utility-text {
  font-family: var(--font-heading);
  font-size: 13px;
  font-weight: 400;
  line-height: 1.4;
  letter-spacing: 1px;
  text-transform: uppercase;
}
```

### Mobile Sizes
```css
@media (max-width: 768px) {
  .hero-title {
    font-size: 40px;
  }
  
  .section-title {
    font-size: 36px;
    letter-spacing: 2px;
  }
  
  .tagline {
    font-size: 22px;
  }
  
  .hero-subtitle {
    font-size: 18px;
  }
  
  .card-title {
    font-size: 20px;
  }
  
  .nav-text {
    font-size: 14px;
  }
  
  .button-text {
    font-size: 13px;
    letter-spacing: 1px;
  }
  
  .button-large-text {
    font-size: 15px;
    letter-spacing: 1px;
  }
  
  .utility-text {
    font-size: 11px;
  }
}
```

## Typography Hierarchy

### Level 1 - Hero/Display
**Purpose**: Maximum impact, main page headlines
**Implementation**:
```css
h1, .h1 {
  font-family: var(--font-heading); /* Not display font for web */
  font-size: 64px;
  font-weight: 800;
  line-height: 1.1;
  letter-spacing: -1px;
  text-transform: uppercase;
  color: var(--color-burgundy);
}
```

### Level 2 - Section Titles
**Purpose**: Major section divisions
**Implementation**:
```css
h2, .h2 {
  font-family: var(--font-heading);
  font-size: 48px;
  font-weight: 800;
  line-height: 1.2;
  letter-spacing: 3px;
  text-transform: uppercase;
  color: var(--color-burgundy);
  text-align: center;
}
```

### Level 3 - Subsection Titles
**Purpose**: Content group headings
**Implementation**:
```css
h3, .h3 {
  font-family: var(--font-heading);
  font-size: 24px;
  font-weight: 700;
  line-height: 1.3;
  letter-spacing: 1px;
  text-transform: uppercase;
  color: var(--color-burgundy);
}
```

### Level 4 - Small Headings
**Purpose**: Card titles, minor sections
**Implementation**:
```css
h4, .h4 {
  font-family: var(--font-heading);
  font-size: 18px;
  font-weight: 700;
  line-height: 1.3;
  letter-spacing: 0.5px;
  color: var(--color-charcoal);
}
```

### Body Text Hierarchy
**Large Body** (Hero subtitles):
```css
.body-large {
  font-family: var(--font-body);
  font-size: 22px;
  font-weight: 300;
  line-height: 1.6;
  color: var(--color-charcoal);
}
```

**Regular Body** (Main content):
```css
.body-regular {
  font-family: var(--font-body);
  font-size: 16px;
  font-weight: 400;
  line-height: 1.7;
  color: var(--color-charcoal);
}
```

**Small Body** (Captions, details):
```css
.body-small {
  font-family: var(--font-body);
  font-size: 15px;
  font-weight: 400;
  line-height: 1.6;
  color: var(--color-stone);
}
```

## Special Typography Treatments

### Gradient Text (Hero Accents)
```css
.text-gradient {
  background: linear-gradient(135deg, var(--color-rose-gold) 0%, var(--color-brass) 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  color: var(--color-rose-gold); /* Fallback */
}
```

### Text Shadows (Event Cards)
```css
.text-shadow {
  text-shadow: 0 2px 4px rgba(0, 0, 0, 0.3);
}
```

### Letter Spacing Scale
```css
:root {
  --letter-spacing-tight: -1px;    /* Hero headlines */
  --letter-spacing-normal: 0px;    /* Body text */
  --letter-spacing-wide: 1px;      /* Navigation, card titles */
  --letter-spacing-wider: 1.5px;   /* Button text */
  --letter-spacing-widest: 3px;    /* Section titles */
}
```

## Font Loading Optimization

### Google Fonts Implementation
```html
<!-- Optimized font loading -->
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link href="https://fonts.googleapis.com/css2?family=Bodoni+Moda:opsz,wght@6..96,400;6..96,700;6..96,900&family=Montserrat:wght@400;500;600;700;800&family=Source+Sans+3:wght@300;400;600&family=Satisfy&display=swap" rel="stylesheet">
```

### Font Display Strategy
```css
@font-face {
  font-family: 'Montserrat';
  font-display: swap; /* Show fallback until font loads */
  /* Other font-face properties */
}
```

### Fallback Stack
```css
:root {
  --font-display: 'Bodoni Moda', 'Times New Roman', serif;
  --font-heading: 'Montserrat', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
  --font-body: 'Source Sans 3', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
  --font-accent: 'Satisfy', 'Brush Script MT', cursive;
}
```

## Typography Components

### React Implementation
```typescript
import { Text, Title } from '@mantine/core';
import { styled } from '@emotion/styled';

// Hero Title Component
const HeroTitle = styled(Title)`
  font-family: var(--font-heading);
  font-size: 64px;
  font-weight: 800;
  line-height: 1.1;
  letter-spacing: -1px;
  text-transform: uppercase;
  color: var(--color-burgundy);
  
  @media (max-width: 768px) {
    font-size: 40px;
  }
`;

// Section Title Component
const SectionTitle = styled(Title)`
  font-family: var(--font-heading);
  font-size: 48px;
  font-weight: 800;
  line-height: 1.2;
  letter-spacing: 3px;
  text-transform: uppercase;
  color: var(--color-burgundy);
  text-align: center;
  position: relative;
  
  &::after {
    content: '';
    display: block;
    width: 100px;
    height: 2px;
    background: linear-gradient(90deg, transparent, var(--color-rose-gold), transparent);
    margin: var(--space-sm) auto 0;
  }
  
  @media (max-width: 768px) {
    font-size: 36px;
    letter-spacing: 2px;
  }
`;

// Tagline Component
const Tagline = styled(Text)`
  font-family: var(--font-accent);
  font-size: 28px;
  font-weight: 400;
  line-height: 1.3;
  color: var(--color-rose-gold);
  
  @media (max-width: 768px) {
    font-size: 22px;
  }
`;

// Body Text Component
const BodyText = styled(Text)`
  font-family: var(--font-body);
  font-size: 16px;
  font-weight: 400;
  line-height: 1.7;
  color: var(--color-charcoal);
`;
```

### TypeScript Type Definitions
```typescript
export type FontFamily = 'display' | 'heading' | 'body' | 'accent';
export type FontWeight = 300 | 400 | 500 | 600 | 700 | 800 | 900;
export type FontSize = 'xs' | 'sm' | 'md' | 'lg' | 'xl' | '2xl' | '3xl';
export type LetterSpacing = 'tight' | 'normal' | 'wide' | 'wider' | 'widest';

export interface TypographyProps {
  family?: FontFamily;
  weight?: FontWeight;
  size?: FontSize;
  spacing?: LetterSpacing;
  gradient?: boolean;
  shadow?: boolean;
  uppercase?: boolean;
}
```

## Usage Guidelines

### DO's
- ✅ Use the font hierarchy consistently
- ✅ Maintain proper line heights for readability
- ✅ Test typography on different devices
- ✅ Use appropriate font weights for emphasis
- ✅ Ensure sufficient contrast with backgrounds
- ✅ Optimize font loading performance

### DON'Ts
- ❌ Mix serif and sans-serif inappropriately
- ❌ Use accent font for long text blocks
- ❌ Ignore mobile typography scaling
- ❌ Overuse text transforms (ALL CAPS)
- ❌ Use too many font weights in one design
- ❌ Sacrifice readability for style

## Accessibility Considerations

### Readability Standards
- **Minimum font size**: 14px for body text
- **Line height**: Minimum 1.5 for body text
- **Paragraph width**: Maximum 80 characters
- **Contrast ratios**: Meet WCAG 2.1 AA standards

### Implementation
```css
/* Responsive typography for accessibility */
@media (prefers-reduced-motion: reduce) {
  * {
    font-variation-settings: normal;
  }
}

/* High contrast mode support */
@media (prefers-contrast: high) {
  .text-gradient {
    background: none;
    -webkit-text-fill-color: var(--color-burgundy);
    color: var(--color-burgundy);
  }
}

/* Large text preference */
@media (prefers-reduced-data: no-preference) {
  :root {
    font-size: 18px; /* Increase base font size */
  }
}
```

### Screen Reader Considerations
```html
<!-- Use semantic HTML for proper reading order -->
<h1>Main Page Title</h1>
<h2>Section Title</h2>
<h3>Subsection Title</h3>

<!-- Avoid decorative text in screen readers -->
<span aria-hidden="true" class="decorative-text">~~~</span>

<!-- Provide context for styled text -->
<span class="text-gradient" aria-label="Highlighted text">Featured Content</span>
```

## Performance Monitoring

### Font Loading Metrics
```javascript
// Monitor font loading performance
if ('fonts' in document) {
  document.fonts.ready.then(() => {
    console.log('All fonts loaded successfully');
    // Track loading time
    performance.mark('fonts-loaded');
  });
}

// Track font swap events
document.fonts.addEventListener('loadingdone', (event) => {
  console.log(`${event.fontface.family} loaded`);
});
```

### Core Web Vitals Impact
- Monitor Cumulative Layout Shift (CLS) from font swaps
- Optimize Largest Contentful Paint (LCP) with proper font loading
- Use `font-display: swap` to reduce Flash of Invisible Text (FOIT)

## Testing Checklist

### Typography Quality Assurance
- [ ] All fonts load properly across browsers
- [ ] Hierarchy is visually clear and consistent
- [ ] Mobile scaling works at all breakpoints
- [ ] Text remains readable on all background colors
- [ ] Letter spacing doesn't break word recognition
- [ ] Line heights provide comfortable reading experience
- [ ] Fallback fonts display acceptably
- [ ] Performance impact is minimal (<100KB total)

### Accessibility Testing
- [ ] All text meets WCAG 2.1 AA contrast ratios
- [ ] Typography scales properly with browser zoom (up to 200%)
- [ ] Text remains readable with user stylesheets
- [ ] Screen readers pronounce text naturally
- [ ] High contrast mode displays properly
- [ ] Text doesn't break with browser font substitutions

## Maintenance

### Version Control
- Track font version updates in design tokens
- Document typography changes with visual diffs
- Test performance impact of font updates
- Maintain fallback compatibility

### Future Considerations
- Variable fonts for better performance and flexibility
- System font alternatives for performance
- Custom font hosting for better control
- Advanced OpenType features utilization

---

**Next Steps**: Implement the typography system using CSS custom properties and React components. Test thoroughly across devices and accessibility tools.

**Tools**: [Design Tokens v7](../current/design-tokens-v7.json) | [Working Template](../current/homepage-template-v7.html)
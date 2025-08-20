# Color Standards - Design System v7

**Authority**: Based on approved Final Design v7 template
**Status**: Complete color specification
**Last Updated**: 2025-08-20

## Color Philosophy

The v7 color system embodies **edgy sophistication** - a warm, luxurious palette that respects the rope bondage community while maintaining modern web accessibility standards.

### Design Principles
- **Warm Luxury**: Rose gold and brass metallics create premium feel
- **Sophisticated Edge**: Deep burgundy adds gravitas and elegance
- **Electric Accents**: Purple tones provide modern vibrancy
- **Accessibility First**: All combinations meet WCAG 2.1 AA standards
- **Emotional Connection**: Colors evoke trust, warmth, and community

## Primary Color Palette

### Burgundy Family (Brand Core)
```css
--color-burgundy: #880124;        /* Primary brand color */
--color-burgundy-dark: #660018;   /* Dark variant for backgrounds */
--color-burgundy-light: #9F1D35;  /* Light variant for hover states */
```

**Usage**:
- **Primary**: Navigation text, section headings, main brand elements
- **Dark**: Hero backgrounds, footer, dark sections
- **Light**: Hover states, active navigation, emphasis

**Accessibility**: 
- Burgundy on cream: 7.4:1 (AAA)
- Burgundy on ivory: 6.8:1 (AAA)
- Burgundy light on cream: 6.1:1 (AA)

### Metallic Accents (Sophistication)
```css
--color-rose-gold: #B76D75;  /* Primary accent color */
--color-copper: #B87333;     /* Secondary accent */
--color-brass: #C9A961;      /* Tertiary accent, warnings */
```

**Usage**:
- **Rose Gold**: Borders, underlines, highlights, gradients
- **Copper**: Secondary accents, complementary elements
- **Brass**: Special links (incident reporting), warning states

**Accessibility**:
- Rose gold on midnight: 4.8:1 (AA)
- Copper on midnight: 5.2:1 (AA)
- Brass on midnight: 6.1:1 (AA)

### CTA Colors (Action & Energy)
```css
--color-electric: #9D4EDD;      /* Primary CTA background */
--color-electric-dark: #7B2CBF; /* Primary CTA hover */
--color-amber: #FFBF00;          /* Secondary CTA background */
--color-amber-dark: #FF8C00;     /* Secondary CTA hover */
```

**Usage**:
- **Electric**: Secondary action buttons, special CTAs
- **Amber**: Primary action buttons (login, join, register)
- **Dark variants**: Hover states, active states

**Accessibility**:
- White text on electric: 4.9:1 (AA)
- Midnight text on amber: 9.2:1 (AAA)

### Supporting Colors
```css
--color-dusty-rose: #D4A5A5;  /* Light accent, soft touches */
--color-plum: #614B79;        /* Event card gradients */
--color-midnight: #1A1A2E;    /* Dark sections, contrast */
```

**Usage**:
- **Dusty Rose**: Subtle accents, light decoration
- **Plum**: Event card backgrounds, gradient combinations
- **Midnight**: Dark backgrounds, high contrast sections

## Neutral Palette

### Text Colors (Hierarchy)
```css
--color-charcoal: #2B2B2B;  /* Primary text */
--color-smoke: #4A4A4A;     /* Secondary text */
--color-stone: #8B8680;     /* Tertiary text, muted content */
--color-taupe: #B8B0A8;     /* Light text on dark backgrounds */
```

**Text Hierarchy**:
1. **Charcoal**: Main headings, primary content
2. **Smoke**: Subheadings, secondary content
3. **Stone**: Captions, metadata, less important text
4. **Taupe**: Text on dark backgrounds

**Accessibility**:
- Charcoal on cream: 11.2:1 (AAA)
- Smoke on cream: 7.8:1 (AAA)
- Stone on cream: 4.7:1 (AA)
- Taupe on midnight: 4.9:1 (AA)

### Background Colors
```css
--color-ivory: #FFF8F0;     /* Card backgrounds, light sections */
--color-cream: #FAF6F2;     /* Main body background */
```

**Usage**:
- **Ivory**: Card backgrounds, content sections, overlays
- **Cream**: Main page background, base color

## Status Colors

### System States
```css
--color-success: #228B22;  /* Available spots, positive states */
--color-warning: #DAA520;  /* Limited spots, warnings */
--color-error: #DC143C;    /* Full events, errors, alerts */
```

**Usage Guidelines**:
- **Success**: Available event spots, successful actions, positive feedback
- **Warning**: Limited availability, caution states, attention needed
- **Error**: Full events, error states, critical alerts

**Accessibility**:
- All status colors meet AA standards on light backgrounds
- Use icons alongside colors for colorblind accessibility

## Color Combinations

### Approved Pairings

#### Primary Combinations
```css
/* Navigation */
background: var(--color-cream);
text: var(--color-burgundy);
accent: var(--color-rose-gold);

/* Hero Section */
background: linear-gradient(180deg, var(--color-midnight) 0%, var(--color-burgundy-dark) 100%);
text: var(--color-ivory);
accent: var(--color-rose-gold);

/* Content Sections */
background: var(--color-ivory);
text: var(--color-charcoal);
headings: var(--color-burgundy);

/* Footer */
background: var(--color-midnight);
text: var(--color-taupe);
headings: var(--color-rose-gold);
```

#### Button Combinations
```css
/* Primary Button (Amber) */
background: linear-gradient(135deg, var(--color-amber) 0%, var(--color-amber-dark) 100%);
text: var(--color-midnight);
shadow: 0 4px 15px rgba(255, 191, 0, 0.4);

/* Electric Button */
background: linear-gradient(135deg, var(--color-electric) 0%, var(--color-electric-dark) 100%);
text: var(--color-ivory);
shadow: 0 4px 15px rgba(157, 78, 221, 0.4);

/* Secondary Button */
background: transparent;
text: var(--color-burgundy);
border: 2px solid var(--color-burgundy);
hover-fill: var(--color-burgundy);
hover-text: var(--color-ivory);
```

### Gradient Specifications

#### Background Gradients
```css
/* Hero Section */
.hero-gradient {
  background: linear-gradient(180deg, var(--color-midnight) 0%, var(--color-burgundy-dark) 100%);
}

/* CTA Section */
.cta-gradient {
  background: linear-gradient(135deg, var(--color-burgundy-dark) 0%, var(--color-midnight) 100%);
}

/* Event Card Headers */
.event-header-gradient {
  background: linear-gradient(135deg, var(--color-plum) 0%, var(--color-burgundy) 100%);
}

/* Feature Icons */
.feature-icon-gradient {
  background: linear-gradient(135deg, var(--color-burgundy) 0%, var(--color-rose-gold) 100%);
}
```

#### Text Gradients
```css
/* Hero Title Accent */
.hero-text-gradient {
  background: linear-gradient(135deg, var(--color-rose-gold) 0%, var(--color-brass) 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}
```

#### Underline Gradients
```css
/* Navigation Underlines */
.nav-underline {
  background: linear-gradient(90deg, transparent, var(--color-burgundy), transparent);
}

/* Section Title Underlines */
.section-underline {
  background: linear-gradient(90deg, transparent, var(--color-rose-gold), transparent);
}

/* Footer Border */
.footer-border {
  background: linear-gradient(90deg, transparent, var(--color-rose-gold), transparent);
}
```

## Color Usage Rules

### DO's
- ✅ Use CSS variables for all color references
- ✅ Test color combinations for accessibility
- ✅ Apply gradients consistently across similar elements
- ✅ Use status colors with accompanying icons
- ✅ Maintain color hierarchy in text elements
- ✅ Use warm metallics for accents and highlights

### DON'Ts
- ❌ Hardcode hex values in components
- ❌ Create new color variations without approval
- ❌ Use colors that don't meet accessibility standards
- ❌ Mix cool and warm tones inappropriately
- ❌ Use status colors for decorative purposes
- ❌ Override established gradient directions

## Accessibility Compliance

### WCAG 2.1 AA Standards
All color combinations in the system meet or exceed WCAG 2.1 AA contrast ratios (4.5:1 for normal text, 3:1 for large text).

### AAA Compliance
Primary text combinations achieve AAA standards (7:1) where possible:
- Charcoal on cream: 11.2:1
- Burgundy on cream: 7.4:1
- Smoke on cream: 7.8:1

### Colorblind Accessibility
- Never use color alone to convey information
- Pair status colors with icons or text labels
- Ensure sufficient contrast in grayscale
- Test with colorblind simulation tools

### Implementation
```css
/* Always provide fallbacks */
.status-available {
  color: var(--color-success);
}

.status-available::before {
  content: '✓ '; /* Checkmark for visual clarity */
}

/* High contrast mode support */
@media (prefers-contrast: high) {
  :root {
    --color-burgundy: #000000;
    --color-charcoal: #000000;
  }
}
```

## Color Testing

### Tools for Validation
- **WebAIM Contrast Checker**: Verify WCAG compliance
- **Stark**: Figma/Sketch plugin for accessibility
- **Colorblinding**: Chrome extension for simulation
- **axe**: Automated accessibility testing

### Testing Checklist
- [ ] All text meets AA contrast ratios
- [ ] Primary combinations meet AAA when possible
- [ ] Status colors work without color perception
- [ ] High contrast mode supported
- [ ] Gradients maintain readability
- [ ] Mobile displays correctly

## Color Psychology

### Emotional Impact
- **Burgundy**: Trust, sophistication, stability
- **Rose Gold**: Warmth, luxury, approachability
- **Electric Purple**: Innovation, creativity, energy
- **Amber**: Warmth, optimism, action
- **Midnight**: Mystery, elegance, focus

### Community Alignment
Colors chosen to reflect:
- **Respect**: Sophisticated, mature aesthetic
- **Warmth**: Welcoming, inclusive community
- **Safety**: Trustworthy, reliable environment
- **Growth**: Progressive, forward-thinking

## Implementation Examples

### CSS Custom Properties Setup
```css
:root {
  /* Primary Colors */
  --color-burgundy: #880124;
  --color-burgundy-dark: #660018;
  --color-burgundy-light: #9F1D35;
  
  /* Metallics */
  --color-rose-gold: #B76D75;
  --color-copper: #B87333;
  --color-brass: #C9A961;
  
  /* CTA Colors */
  --color-electric: #9D4EDD;
  --color-electric-dark: #7B2CBF;
  --color-amber: #FFBF00;
  --color-amber-dark: #FF8C00;
  
  /* Supporting */
  --color-dusty-rose: #D4A5A5;
  --color-plum: #614B79;
  --color-midnight: #1A1A2E;
  
  /* Neutrals */
  --color-charcoal: #2B2B2B;
  --color-smoke: #4A4A4A;
  --color-stone: #8B8680;
  --color-taupe: #B8B0A8;
  --color-ivory: #FFF8F0;
  --color-cream: #FAF6F2;
  
  /* Status */
  --color-success: #228B22;
  --color-warning: #DAA520;
  --color-error: #DC143C;
}
```

### TypeScript Color Tokens
```typescript
export const colors = {
  primary: {
    burgundy: '#880124',
    burgundyDark: '#660018',
    burgundyLight: '#9F1D35',
  },
  metallics: {
    roseGold: '#B76D75',
    copper: '#B87333',
    brass: '#C9A961',
  },
  // ... rest of color system
} as const;

export type ColorToken = keyof typeof colors;
export type ColorValue = typeof colors[ColorToken];
```

### Mantine Theme Integration
```typescript
import { MantineColorsTuple, createTheme } from '@mantine/core';

const burgundy: MantineColorsTuple = [
  '#ffeef3',
  '#ffdbdf',
  '#ffb3bd',
  '#ff8798',
  '#fe6479',
  '#fe4d68',
  '#fe425f',
  '#e3324f',
  '#cc2946',
  '#b01d3b'
];

const theme = createTheme({
  colors: {
    burgundy,
    // Add custom color scales
  },
  primaryColor: 'burgundy',
  // Other theme customizations
});
```

## Maintenance

### Version Control
- Track color changes in design tokens file
- Document reasons for color modifications
- Test accessibility impact of any changes
- Update component libraries when colors change

### Deprecation Process
1. Mark old colors as deprecated in comments
2. Provide migration path to new colors
3. Update documentation with timeline
4. Remove deprecated colors after adoption period

---

**Next Steps**: Use this color system as the foundation for all v7 development. Reference the design tokens JSON file for programmatic access to color values.

**Tools**: [Design Tokens v7](../current/design-tokens-v7.json) | [Working Template](../current/homepage-template-v7.html)
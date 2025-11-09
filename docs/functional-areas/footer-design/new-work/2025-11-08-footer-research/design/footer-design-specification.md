<!-- Last Updated: 2025-11-08 -->
<!-- Version: 1.0 -->
<!-- Owner: UI Designer Agent -->
<!-- Status: Ready for Implementation -->

# Footer Design Specification - Accordion Mobile-First Footer

**Project**: WitchCityRope Footer Redesign
**Approved Mockup**: `/home/chad/repos/witchcityrope/docs/functional-areas/footer-design/new-work/2025-11-08-footer-research/mockups/footer-mockup-accordion.html`
**Design System**: Design System v7 (WitchCityRope)
**Target Browsers**: Modern browsers (Chrome/Edge 90+, Firefox 88+, Safari 14+)
**WCAG Compliance**: WCAG 2.1 AA

---

## 1. Design Overview

### Purpose
Replace existing footer with accordion-based mobile-first design that:
- Collapses footer sections on mobile (<768px) for vertical space efficiency
- Expands footer sections on desktop (≥768px) for full visibility
- Maintains Design System v7 visual language
- Provides accessibility-first keyboard navigation
- Reduces visual clutter while maintaining content discoverability

### Design Approach
**Accordion Mobile-First Footer**:
- **Mobile (<768px)**: Collapsible accordion sections, vertical stacking
- **Desktop (≥768px)**: Three-column grid, all sections expanded, hover underlines
- **Interaction**: Click to expand/collapse on mobile, hover underlines on desktop
- **Responsive**: Single breakpoint at 768px with viewport resize handling

### Key Design Decisions
1. **Accordion on Mobile**: Reduces initial footer height from ~600px to ~200px
2. **Center-Outward Underline**: Signature Design System v7 animation for titles
3. **Rose Gold Accents**: Maintains brand consistency (borders, underlines, icons)
4. **No Top Spacing on Footer Bottom**: Mobile has 0 margin/padding above privacy section
5. **Desktop Grid**: Three equal columns for About, Legal, Connect sections

---

## 2. Component Structure

### React Component Hierarchy

```tsx
<Footer>                              // Main footer container
  <div className="footer-content">   // Content wrapper (grid on desktop)

    {/* Section 1: About */}
    <FooterSection
      title="About"
      links={[
        { text: "About Us", href: "/about" },
        { text: "Code of Conduct", href: "/code-of-conduct" }
      ]}
    />

    {/* Section 2: Legal */}
    <FooterSection
      title="Legal"
      links={[
        { text: "Privacy Policy", href: "/privacy-policy" },
        { text: "Terms of Service", href: "/terms-of-service" },
        { text: "Refund Policy", href: "/refund-policy" }
      ]}
    />

    {/* Section 3: Connect */}
    <FooterSection
      title="Connect"
      links={[
        { text: "Contact Us", href: "/contact" }
      ]}
      socialLinks={[
        { platform: "FetLife", url: "https://fetlife.com/witchcityrope", icon: <FetLifeIcon /> },
        { platform: "Instagram", url: "https://instagram.com/witchcityrope", icon: <InstagramIcon /> }
      ]}
    />

    {/* Footer Bottom Section */}
    <FooterBottom
      privacyNotice="We protect the privacy of our members. Location details are shared only with vetted members and ticket holders."
      copyright="© 2025 WitchCityRope. All rights reserved."
      email="info@witchcityrope.com"
    />
  </div>
</Footer>
```

### Mantine v7 Components to Use

| Component | Purpose | Configuration |
|-----------|---------|--------------|
| **Accordion** | Accordion sections (mobile only) | `variant="separated"`, custom styling to match design |
| **Accordion.Item** | Individual footer section | Custom class `.footer-section` |
| **Accordion.Control** | Section header with title + icon | Custom class `.footer-section-header` |
| **Accordion.Panel** | Section body with links | Custom class `.footer-section-body` |
| **Group** | Horizontal layout for social links | `gap="md"` |
| **Stack** | Vertical layout for footer meta (mobile) | `gap="xs"` |
| **Anchor** | Footer links and email link | Built-in link component |
| **Text** | Privacy notice, copyright text | Size `"xs"` for small text |
| **Box** | Generic container for footer content | Responsive styling |

### Props Interface Definition

```tsx
// FooterSection component
interface FooterSectionProps {
  title: string;
  links: Array<{
    text: string;
    href: string;
  }>;
  socialLinks?: Array<{
    platform: string;
    url: string;
    icon: React.ReactNode;
  }>;
}

// FooterBottom component
interface FooterBottomProps {
  privacyNotice: string;
  copyright: string;
  email: string;
}

// Main Footer component
interface FooterProps {
  // No props needed - uses hardcoded content
}
```

### State Management Requirements

```tsx
// Accordion state (managed by Mantine Accordion component)
const [expandedSections, setExpandedSections] = useState<string[]>([]);

// Viewport detection for responsive behavior
const [isMobile, setIsMobile] = useState<boolean>(window.innerWidth < 768);

// Resize handler with debouncing
useEffect(() => {
  const handleResize = debounce(() => {
    setIsMobile(window.innerWidth < 768);
  }, 250);

  window.addEventListener('resize', handleResize);
  return () => window.removeEventListener('resize', handleResize);
}, []);

// Initialize accordion state based on viewport
useEffect(() => {
  if (!isMobile) {
    // Desktop: Expand all sections
    setExpandedSections(['about', 'legal', 'connect']);
  } else {
    // Mobile: Collapse all sections
    setExpandedSections([]);
  }
}, [isMobile]);
```

---

## 3. Visual Design Specifications

### Color Palette

```css
/* From Design System v7 */
--color-burgundy: #880124;        /* Primary brand color */
--color-rose-gold: #B76D75;       /* Accents, borders, underlines */
--color-midnight: #1A1A2E;        /* Footer background */
--color-taupe: #B8B0A8;           /* Light text, links */
--color-ivory: #FFF8F0;           /* Section titles, hover text */
--color-charcoal: #2B2B2B;        /* (Not used in footer) */
```

**Color Usage**:
- **Footer Background**: `--color-midnight` (#1A1A2E)
- **Top Border**: `--color-rose-gold` (#B76D75) 3px solid
- **Section Titles**: `--color-ivory` (#FFF8F0)
- **Links**: `--color-taupe` (#B8B0A8) default, `--color-rose-gold` (#B76D75) on hover
- **Underlines**: Rose gold gradient (center-outward animation)
- **Accordion Icon**: `--color-rose-gold` (#B76D75)
- **Section Borders (mobile)**: `rgba(183, 109, 117, 0.2)` (20% opacity rose gold)

### Typography

```css
/* Font Families */
--font-heading: 'Montserrat', sans-serif;  /* Section titles, links */
--font-body: 'Source Sans 3', sans-serif;  /* Privacy notice, copyright */

/* Section Titles */
.footer-section-title {
  font-family: var(--font-heading);
  font-size: 16px;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 1px;
  color: var(--color-ivory);
}

/* Footer Links */
.footer-links a {
  font-family: var(--font-body);
  font-size: 14px;
  font-weight: 400;
  color: var(--color-taupe);
}

/* Privacy Notice */
.privacy-notice {
  font-family: var(--font-body);
  font-size: 13px;
  font-weight: 400;
  font-style: italic;
  line-height: 1.6;
  color: var(--color-taupe);
}

/* Footer Meta (Copyright, Email) */
.footer-meta {
  font-family: var(--font-body);
  font-size: 13px;
  font-weight: 400;
  color: var(--color-taupe);
}
```

### Spacing System

```css
/* From Design System v7 */
--space-xs: 8px;
--space-sm: 16px;
--space-md: 24px;
--space-lg: 32px;
--space-xl: 40px;

/* Applied Spacing */
Footer padding (vertical): var(--space-xl) = 40px
Footer padding (horizontal - mobile): 20px
Footer padding (horizontal - desktop): 40px

Section header padding (mobile): var(--space-sm) 0 = 16px 0
Section header padding (desktop): 0 0 12px 0

Section bottom margin (mobile): var(--space-sm) = 16px
Section body padding (mobile - expanded): 0 0 var(--space-md) 0 = 0 0 24px 0

Grid column gap (desktop): var(--space-xl) = 40px

Social links gap: var(--space-md) = 24px
Privacy notice margin-bottom: var(--space-sm) = 16px
Footer meta gap (mobile): 12px
```

### Responsive Breakpoints

**Single Breakpoint**: 768px

**Mobile (<768px)**:
- Accordion sections collapsed by default
- Vertical stacking of all sections
- Full-width container with 20px padding
- Section borders between each accordion item
- NO top border on footer-bottom section
- NO spacing above footer-bottom (margin-top: 0, padding-top: 0)

**Desktop (≥768px)**:
- Three-column grid layout
- All sections expanded (max-height: none)
- Container padding: 0 40px
- Section borders removed (border-bottom: none)
- Column gap: 40px
- Footer-bottom spans all columns (grid-column: 1 / -1)
- Footer-bottom has top border (1px solid rgba rose gold)
- Privacy notice margin-top: 10px

---

## 4. Layout Specifications

### Mobile Layout (<768px)

```css
.footer {
  background-color: var(--color-midnight);
  border-top: 3px solid var(--color-rose-gold);
  padding: var(--space-xl) 0;
}

.footer-content {
  max-width: 1200px;
  margin: 0 auto;
  padding: 0 20px;
}

.footer-section {
  border-bottom: 1px solid rgba(183, 109, 117, 0.2);
  margin-bottom: var(--space-sm);
}

.footer-section:last-of-type {
  border-bottom: none; /* No border on last section before footer-bottom */
}

.footer-section-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: var(--space-sm) 0;
  cursor: pointer;
}

.footer-section-body {
  max-height: 0;
  overflow: hidden;
  transition: max-height 0.3s ease, padding 0.3s ease;
}

.footer-section-body.expanded {
  max-height: 500px;
  padding-bottom: var(--space-md);
}

.footer-bottom {
  margin-top: 0;   /* CRITICAL: NO spacing above footer-bottom on mobile */
  padding-top: 0;  /* CRITICAL: NO padding above footer-bottom on mobile */
  /* NO border-top on mobile */
}

.footer-meta {
  display: flex;
  flex-direction: column;
  gap: 12px;
}
```

**Mobile Accordion Behavior**:
- Sections collapsed by default (max-height: 0)
- Click section header to toggle expansion
- Accordion icon rotates 180deg when expanded
- Underline appears on section title when expanded
- Only one section can be expanded at a time (single-expand mode)

### Desktop Layout (≥768px)

```css
@media (min-width: 768px) {
  .footer-content {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    row-gap: 0;
    column-gap: var(--space-xl);
    padding: 0 40px;
  }

  .footer-section {
    border-bottom: none;
    margin-bottom: 0;
  }

  .footer-section-header {
    cursor: default; /* No pointer cursor - not clickable */
    padding: 0 0 12px 0;  /* Top: 0, Bottom: 12px */
  }

  .accordion-icon {
    display: none; /* Hide chevron icon on desktop */
  }

  .footer-section-body {
    max-height: none;  /* Always expanded */
    padding-bottom: 0;
  }

  .footer-bottom {
    grid-column: 1 / -1;  /* Spans all 3 columns */
    margin-top: 0;
    padding-top: 0;
    border-top: 1px solid rgba(183, 109, 117, 0.2);  /* Desktop: Add divider line */
  }

  .privacy-notice {
    margin-top: 10px;  /* Desktop: Spacing above privacy notice */
  }

  .footer-meta {
    flex-direction: row;
    justify-content: space-between;
    align-items: center;
  }
}
```

**Desktop Behavior**:
- All sections always expanded
- Hover on section title shows underline animation
- Three equal columns
- Footer-bottom spans full width below columns

### Grid System (Desktop)

```
┌─────────────────────────────────────────────────────────────┐
│  Footer (background: midnight, border-top: 3px rose gold)  │
│  ┌───────────────────────────────────────────────────────┐ │
│  │  Footer Content (max-width: 1200px, padding: 0 40px) │ │
│  │  ┌───────────┬──────────┬───────────────┐            │ │
│  │  │  About    │  Legal   │  Connect      │            │ │
│  │  │  - Link   │  - Link  │  - Link       │            │ │
│  │  │  - Link   │  - Link  │  - Socials    │            │ │
│  │  │           │  - Link  │               │            │ │
│  │  └───────────┴──────────┴───────────────┘            │ │
│  │  ┌──────────────────────────────────────┐            │ │
│  │  │  Footer Bottom (spans all columns)   │            │ │
│  │  │  - Privacy notice                    │            │ │
│  │  │  - Copyright | Email                 │            │ │
│  │  └──────────────────────────────────────┘            │ │
│  └───────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

**Column Structure**:
- **Column 1 (About)**: 2 links
- **Column 2 (Legal)**: 3 links
- **Column 3 (Connect)**: 1 link + 2 social media links

---

## 5. Interactive Behaviors

### Accordion Behavior (Mobile Only)

```tsx
// Mantine Accordion configuration
<Accordion
  variant="separated"
  onChange={(value) => setExpandedSections(value)}
  value={expandedSections}
>
  <Accordion.Item value="about">
    <Accordion.Control>
      <h3 className="footer-section-title">About</h3>
    </Accordion.Control>
    <Accordion.Panel>
      {/* Links */}
    </Accordion.Panel>
  </Accordion.Item>

  {/* Repeat for Legal, Connect sections */}
</Accordion>
```

**Behavior**:
1. **Initial State**: All sections collapsed (max-height: 0)
2. **Click Section Header**: Toggle expansion of clicked section
3. **Expanded State**: max-height: 500px, padding-bottom: 24px
4. **Icon Rotation**: Chevron rotates 180deg when expanded
5. **Underline Animation**: Title underline expands to 100% width when section is expanded
6. **Transition**: 0.3s ease for max-height, padding, icon rotation

**Desktop Override**:
- Accordion functionality disabled
- All sections always expanded (max-height: none)
- Click listeners removed (cursor: default)
- Icon hidden (display: none)

### Hover States

**Desktop - Section Title Hover**:
```css
.footer-section-header:hover .footer-section-title::after {
  width: 100%;  /* Underline expands from center */
}
```

**Footer Links Hover** (Mobile + Desktop):
```css
.footer-links a:hover {
  color: var(--color-rose-gold);  /* Text color changes to rose gold */
}
```

**Social Links Hover** (Mobile + Desktop):
```css
.social-link:hover {
  color: var(--color-rose-gold);  /* Icon and text change to rose gold */
}
```

**Email Link Hover** (Mobile + Desktop):
```css
.footer-meta a:hover {
  color: var(--color-rose-gold);  /* Email link changes to rose gold */
}
```

### Expanded States

**Mobile - Section Expanded**:
- Section title has underline at 100% width
- Accordion icon rotated 180deg
- Section body visible (max-height: 500px)
- Padding-bottom: 24px on section body

**Desktop - Always Expanded**:
- All sections always visible
- No accordion icons
- Underline only appears on hover

### Focus States (Keyboard Navigation)

```css
/* Focus on section headers (mobile) */
.footer-section-header:focus {
  outline: none;  /* Rely on underline animation for visual feedback */
}

/* Focus on links */
.footer-links a:focus,
.social-link:focus,
.footer-meta a:focus {
  outline: 2px solid var(--color-rose-gold);
  outline-offset: 2px;
}
```

**Keyboard Navigation**:
- **Tab**: Navigate between section headers (mobile) and all links
- **Enter/Space**: Toggle accordion section (mobile)
- **Tab through Links**: All footer links are keyboard accessible
- **Focus Ring**: 2px solid rose gold outline on all focusable elements

### Initialization (Page Load)

```tsx
// Initialize accordion state based on viewport width on page load
useEffect(() => {
  const isMobile = window.innerWidth < 768;

  if (isMobile) {
    // Mobile: All sections collapsed
    setExpandedSections([]);
  } else {
    // Desktop: All sections expanded
    setExpandedSections(['about', 'legal', 'connect']);
  }
}, []); // Run once on mount

// Handle viewport resize
useEffect(() => {
  const handleResize = debounce(() => {
    const isMobile = window.innerWidth < 768;

    if (isMobile) {
      setExpandedSections([]); // Collapse all on resize to mobile
    } else {
      setExpandedSections(['about', 'legal', 'connect']); // Expand all on resize to desktop
    }
  }, 250);

  window.addEventListener('resize', handleResize);
  return () => window.removeEventListener('resize', handleResize);
}, []);
```

---

## 6. Content Specifications

### Footer Links (Total: 6 Links)

**About Section**:
1. About Us - `/about` (may already exist)
2. Code of Conduct - `/code-of-conduct` (may already exist)

**Legal Section**:
3. Privacy Policy - `/privacy-policy` (needs to be created)
4. Terms of Service - `/terms-of-service` (needs to be created)
5. Refund Policy - `/refund-policy` (needs to be created)

**Connect Section**:
6. Contact Us - `/contact` (may already exist)

### Social Media Links

**FetLife**:
- Platform: FetLife
- URL: `https://fetlife.com/witchcityrope`
- Icon: SVG checkmark icon (see mockup line 376)
- Label: "FetLife"
- Attributes: `target="_blank"`, `rel="noopener noreferrer"`, `aria-label="Follow us on FetLife"`

**Instagram**:
- Platform: Instagram
- URL: `https://instagram.com/witchcityrope`
- Icon: SVG Instagram logo (see mockup line 382)
- Label: "Instagram"
- Attributes: `target="_blank"`, `rel="noopener noreferrer"`, `aria-label="Follow us on Instagram"`

### Contact Information

**Email**: info@witchcityrope.com
- Render as clickable mailto link: `<a href="mailto:info@witchcityrope.com">info@witchcityrope.com</a>`

### Privacy Statement

**Full Text**:
"We protect the privacy of our members. Location details are shared only with vetted members and ticket holders."

**Styling**:
- Font size: 13px
- Font style: italic
- Line height: 1.6
- Color: `--color-taupe`
- Margin-bottom: 16px (mobile), 0 (desktop with 10px margin-top)

### Copyright Notice

**Full Text**: "© 2025 WitchCityRope. All rights reserved."

**Styling**:
- Font size: 13px
- Color: `--color-taupe`
- Desktop: Left-aligned in flex row
- Mobile: Stacked above email

---

## 7. Technical Implementation Details

### CSS Variables to Define

```css
:root {
  /* Colors (from Design System v7) */
  --color-burgundy: #880124;
  --color-rose-gold: #B76D75;
  --color-midnight: #1A1A2E;
  --color-taupe: #B8B0A8;
  --color-ivory: #FFF8F0;

  /* Typography */
  --font-heading: 'Montserrat', sans-serif;
  --font-body: 'Source Sans 3', sans-serif;

  /* Spacing */
  --space-xs: 8px;
  --space-sm: 16px;
  --space-md: 24px;
  --space-lg: 32px;
  --space-xl: 40px;
}
```

**Note**: These variables should already exist in the global Design System v7 CSS file. Verify they are imported in the footer component.

### Mantine Configuration

**Theme Overrides**:
```tsx
// In MantineProvider theme configuration
const theme = createTheme({
  colors: {
    wcr: [
      '#f8f4e6', // ivory (lightest)
      '#e8ddd4',
      '#d4a5a5', // dustyRose
      '#c48b8b',
      '#b47171',
      '#a45757',
      '#9b4a75', // plum
      '#880124', // burgundy
      '#6b0119', // darker
      '#2c2c2c'  // charcoal (darkest)
    ]
  },
  primaryColor: 'wcr',
  fontFamily: 'Source Sans 3, sans-serif',
  headings: {
    fontFamily: 'Bodoni Moda, serif'
  }
});

// Footer-specific Mantine component styling
<Accordion
  styles={{
    control: {
      backgroundColor: 'transparent',
      border: 'none',
      padding: 'var(--space-sm) 0',
      '&:hover': {
        backgroundColor: 'transparent', // No background change
      },
    },
    item: {
      borderBottom: '1px solid rgba(183, 109, 117, 0.2)',
      borderRadius: 0,
      '&:last-of-type': {
        borderBottom: 'none',
      },
    },
    panel: {
      paddingBottom: 'var(--space-md)',
    },
  }}
>
  {/* Accordion items */}
</Accordion>
```

### Accessibility

**ARIA Attributes**:
```tsx
// Accordion controls (mobile)
<Accordion.Control aria-expanded={isExpanded} aria-controls="about-content">
  <h3 className="footer-section-title">About</h3>
</Accordion.Control>

<Accordion.Panel id="about-content">
  {/* Links */}
</Accordion.Panel>

// Social media links
<Anchor
  href="https://fetlife.com/witchcityrope"
  target="_blank"
  rel="noopener noreferrer"
  aria-label="Follow us on FetLife"
>
  <FetLifeIcon aria-hidden="true" />
  <span>FetLife</span>
</Anchor>

// Accordion icon
<span className="accordion-icon" aria-hidden="true">▼</span>
```

**Keyboard Navigation**:
- All interactive elements (accordion headers, links) are keyboard accessible
- Tab order: Section 1 header → Links → Section 2 header → Links → Section 3 header → Links → Email
- Enter/Space on accordion header toggles expansion (mobile)
- Focus rings visible on all focusable elements (2px solid rose gold)

**Screen Reader Support**:
- Section headers are `<h3>` semantic headings
- `aria-expanded` announces expansion state
- `aria-controls` associates header with content panel
- `aria-label` provides context for social media links
- `aria-hidden="true"` hides decorative icons from screen readers

### Animation Details

**Underline Animation (Center-Outward)**:
```css
.footer-section-title {
  position: relative;
  display: inline-block;
  padding-bottom: 4px;
}

.footer-section-title::after {
  content: '';
  position: absolute;
  bottom: 0;
  left: 50%;
  transform: translateX(-50%);
  width: 0;  /* Starts at 0 */
  height: 2px;
  background: linear-gradient(90deg, transparent, var(--color-rose-gold), transparent);
  transition: width 0.3s ease;
}

/* Mobile: Show underline when section is expanded */
@media (max-width: 767px) {
  .footer-section-header[aria-expanded="true"] .footer-section-title::after {
    width: 100%;
  }
}

/* Desktop: Show underline on hover */
@media (min-width: 768px) {
  .footer-section-header:hover .footer-section-title::after {
    width: 100%;
  }
}
```

**Accordion Expansion Animation**:
```css
.footer-section-body {
  max-height: 0;
  overflow: hidden;
  transition: max-height 0.3s ease, padding 0.3s ease;
}

.footer-section-body.expanded {
  max-height: 500px;
  padding-bottom: var(--space-md);
}
```

**Icon Rotation Animation**:
```css
.accordion-icon {
  font-size: 20px;
  color: var(--color-rose-gold);
  transition: transform 0.3s ease;
}

.accordion-icon.expanded {
  transform: rotate(180deg);
}
```

**Link Color Transitions**:
```css
.footer-links a,
.social-link,
.footer-meta a {
  transition: color 0.2s ease;
}
```

**Timing Functions**:
- Accordion expansion: 0.3s ease
- Underline animation: 0.3s ease
- Icon rotation: 0.3s ease
- Link color change: 0.2s ease

**Easing Functions**:
- All animations use standard `ease` easing
- No custom cubic-bezier curves required

### Responsive Logic

**Viewport Detection**:
```tsx
import { useMediaQuery } from '@mantine/hooks';

const Footer: React.FC = () => {
  const isMobile = useMediaQuery('(max-width: 767px)');
  const [expandedSections, setExpandedSections] = useState<string[]>([]);

  // Initialize accordion state based on viewport
  useEffect(() => {
    if (isMobile) {
      setExpandedSections([]); // Collapse all on mobile
    } else {
      setExpandedSections(['about', 'legal', 'connect']); // Expand all on desktop
    }
  }, [isMobile]);

  return (
    <footer className="footer">
      {/* Footer content */}
    </footer>
  );
};
```

**CSS Media Query**:
```css
/* Mobile-first base styles */
.footer-content {
  padding: 0 20px;
}

/* Desktop overrides */
@media (min-width: 768px) {
  .footer-content {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    column-gap: var(--space-xl);
    padding: 0 40px;
  }

  .footer-section-header {
    cursor: default;
    padding: 0 0 12px 0;
  }

  .accordion-icon {
    display: none;
  }

  .footer-section-body {
    max-height: none;
    padding-bottom: 0;
  }

  .footer-bottom {
    grid-column: 1 / -1;
    border-top: 1px solid rgba(183, 109, 117, 0.2);
  }

  .privacy-notice {
    margin-top: 10px;
  }

  .footer-meta {
    flex-direction: row;
    justify-content: space-between;
  }
}
```

**Debounced Resize Handler**:
```tsx
import { useDebouncedValue } from '@mantine/hooks';

// Use Mantine's debounced hook for resize handling
const [windowWidth, setWindowWidth] = useState(window.innerWidth);
const [debouncedWidth] = useDebouncedValue(windowWidth, 250);

useEffect(() => {
  const handleResize = () => setWindowWidth(window.innerWidth);
  window.addEventListener('resize', handleResize);
  return () => window.removeEventListener('resize', handleResize);
}, []);

useEffect(() => {
  const isMobile = debouncedWidth < 768;
  // Update accordion state based on debounced width
}, [debouncedWidth]);
```

---

## 8. Static Page Requirements

### Pages to Create (New)

1. **Privacy Policy** (`/privacy-policy`)
   - Legal document outlining data collection, usage, and protection
   - CMS-editable content
   - Route: `/privacy-policy`
   - Status: Needs to be created

2. **Terms of Service** (`/terms-of-service`)
   - Legal agreement between WitchCityRope and users
   - CMS-editable content
   - Route: `/terms-of-service`
   - Status: Needs to be created

3. **Refund Policy** (`/refund-policy`)
   - Policy for event ticket refunds and cancellations
   - CMS-editable content
   - Route: `/refund-policy`
   - Status: Needs to be created

### Pages to Verify (May Exist)

1. **About Us** (`/about`)
   - Community mission, history, leadership
   - Route: `/about`
   - Status: Verify if exists, update footer link

2. **Code of Conduct** (`/code-of-conduct`)
   - Community behavior expectations and safety guidelines
   - Route: `/code-of-conduct`
   - Status: Verify if exists, update footer link

3. **Contact Us** (`/contact`)
   - Contact form or email information
   - Route: `/contact`
   - Status: Verify if exists, update footer link

### CMS Integration Notes

- All static pages should use the existing CMS system for content editing
- Footer links should route to CMS-managed pages
- Admin users should be able to edit page content without code changes
- Use TipTap editor for rich text content (see CMS lessons learned)

---

## 9. Implementation Notes for React Developer

### File Location

**New Footer Component**:
- Create: `/apps/web/src/components/layout/Footer.tsx`
- CSS Module (optional): `/apps/web/src/components/layout/Footer.module.css`

**Component Structure**:
```
/apps/web/src/components/layout/
├── Footer.tsx              // Main footer component
├── FooterSection.tsx       // Reusable accordion section component
├── FooterBottom.tsx        // Footer bottom section component
└── Footer.module.css       // Optional: Component-specific styles
```

### Replace Existing Footer

**Steps**:
1. Locate current footer component (likely in `/apps/web/src/components/layout/` or `/apps/web/src/App.tsx`)
2. Import new `Footer` component
3. Replace old footer with `<Footer />` component
4. Remove old footer component and styles
5. Verify footer appears on all pages

**Example Integration**:
```tsx
// In App.tsx or Layout component
import { Footer } from '@/components/layout/Footer';

function App() {
  return (
    <div className="app">
      <Header />
      <main>
        <Outlet /> {/* Routes */}
      </main>
      <Footer /> {/* New footer */}
    </div>
  );
}
```

### Integration with Existing Layout/Routing

**React Router Integration**:
- Footer links use `react-router-dom` `Link` component for internal navigation
- External links (social media) use standard `<a>` tags with `target="_blank"`

**Example**:
```tsx
import { Link } from 'react-router-dom';

// Internal link
<Link to="/privacy-policy">Privacy Policy</Link>

// External link
<a href="https://fetlife.com/witchcityrope" target="_blank" rel="noopener noreferrer">
  FetLife
</a>
```

**Layout Considerations**:
- Footer should be placed outside of page-specific containers
- Footer spans full viewport width (no max-width on outer container)
- Footer content has max-width: 1200px constraint

### Testing Requirements

**Unit Tests** (`Footer.test.tsx`):
- [ ] Footer renders with correct sections
- [ ] All links render with correct hrefs
- [ ] Social media links have correct attributes (target, rel)
- [ ] Privacy notice and copyright text render correctly
- [ ] Email link has correct mailto href

**Integration Tests**:
- [ ] Footer appears on all pages
- [ ] Links navigate to correct routes
- [ ] External links open in new tab
- [ ] Accordion expands/collapses on mobile
- [ ] Sections always expanded on desktop

**Responsive Tests** (Playwright):
- [ ] Mobile (<768px): Accordion sections collapsed by default
- [ ] Mobile: Click accordion header expands section
- [ ] Mobile: Underline appears when section expanded
- [ ] Mobile: Footer bottom has no top spacing
- [ ] Desktop (≥768px): All sections expanded
- [ ] Desktop: Hover on section title shows underline
- [ ] Desktop: Three-column grid layout
- [ ] Desktop: Footer bottom has top border
- [ ] Resize from desktop to mobile: Sections collapse
- [ ] Resize from mobile to desktop: Sections expand

**Accessibility Tests**:
- [ ] Keyboard navigation works (Tab through all links)
- [ ] Enter/Space toggles accordion (mobile)
- [ ] Focus rings visible on all interactive elements
- [ ] Screen reader announces section expansion state
- [ ] Color contrast meets WCAG 2.1 AA (4.5:1 minimum)

**Visual Regression Tests**:
- [ ] Footer matches approved mockup on mobile
- [ ] Footer matches approved mockup on desktop
- [ ] Animations smooth (60fps target)
- [ ] No layout shift on page load

---

## 10. Code Examples

### Main Footer Component

```tsx
import React, { useState, useEffect } from 'react';
import { Accordion, Box, Group, Anchor, Text, Stack } from '@mantine/core';
import { useMediaQuery, useDebouncedValue } from '@mantine/hooks';
import { Link } from 'react-router-dom';
import './Footer.css'; // Or Footer.module.css

export const Footer: React.FC = () => {
  const isMobile = useMediaQuery('(max-width: 767px)');
  const [expandedSections, setExpandedSections] = useState<string[]>([]);

  // Initialize accordion state based on viewport
  useEffect(() => {
    if (isMobile) {
      setExpandedSections([]); // Collapse all on mobile
    } else {
      setExpandedSections(['about', 'legal', 'connect']); // Expand all on desktop
    }
  }, [isMobile]);

  return (
    <footer className="footer">
      <Box className="footer-content">
        {isMobile ? (
          // Mobile: Accordion layout
          <Accordion
            value={expandedSections}
            onChange={setExpandedSections}
            styles={{
              control: {
                backgroundColor: 'transparent',
                border: 'none',
                padding: 'var(--space-sm) 0',
              },
              item: {
                borderBottom: '1px solid rgba(183, 109, 117, 0.2)',
                borderRadius: 0,
              },
            }}
          >
            {/* About Section */}
            <Accordion.Item value="about" className="footer-section">
              <Accordion.Control>
                <h3 className="footer-section-title">About</h3>
              </Accordion.Control>
              <Accordion.Panel>
                <ul className="footer-links">
                  <li><Link to="/about">About Us</Link></li>
                  <li><Link to="/code-of-conduct">Code of Conduct</Link></li>
                </ul>
              </Accordion.Panel>
            </Accordion.Item>

            {/* Legal Section */}
            <Accordion.Item value="legal" className="footer-section">
              <Accordion.Control>
                <h3 className="footer-section-title">Legal</h3>
              </Accordion.Control>
              <Accordion.Panel>
                <ul className="footer-links">
                  <li><Link to="/privacy-policy">Privacy Policy</Link></li>
                  <li><Link to="/terms-of-service">Terms of Service</Link></li>
                  <li><Link to="/refund-policy">Refund Policy</Link></li>
                </ul>
              </Accordion.Panel>
            </Accordion.Item>

            {/* Connect Section */}
            <Accordion.Item value="connect" className="footer-section">
              <Accordion.Control>
                <h3 className="footer-section-title">Connect</h3>
              </Accordion.Control>
              <Accordion.Panel>
                <ul className="footer-links">
                  <li><Link to="/contact">Contact Us</Link></li>
                </ul>
                <Group gap="md" mt="sm">
                  <Anchor
                    href="https://fetlife.com/witchcityrope"
                    target="_blank"
                    rel="noopener noreferrer"
                    aria-label="Follow us on FetLife"
                    className="social-link"
                  >
                    <FetLifeIcon />
                    <span>FetLife</span>
                  </Anchor>
                  <Anchor
                    href="https://instagram.com/witchcityrope"
                    target="_blank"
                    rel="noopener noreferrer"
                    aria-label="Follow us on Instagram"
                    className="social-link"
                  >
                    <InstagramIcon />
                    <span>Instagram</span>
                  </Anchor>
                </Group>
              </Accordion.Panel>
            </Accordion.Item>
          </Accordion>
        ) : (
          // Desktop: Grid layout
          <Box style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 'var(--space-xl)' }}>
            {/* About Section */}
            <div className="footer-section">
              <h3 className="footer-section-title">About</h3>
              <ul className="footer-links">
                <li><Link to="/about">About Us</Link></li>
                <li><Link to="/code-of-conduct">Code of Conduct</Link></li>
              </ul>
            </div>

            {/* Legal Section */}
            <div className="footer-section">
              <h3 className="footer-section-title">Legal</h3>
              <ul className="footer-links">
                <li><Link to="/privacy-policy">Privacy Policy</Link></li>
                <li><Link to="/terms-of-service">Terms of Service</Link></li>
                <li><Link to="/refund-policy">Refund Policy</Link></li>
              </ul>
            </div>

            {/* Connect Section */}
            <div className="footer-section">
              <h3 className="footer-section-title">Connect</h3>
              <ul className="footer-links">
                <li><Link to="/contact">Contact Us</Link></li>
              </ul>
              <Group gap="md" mt="sm">
                <Anchor
                  href="https://fetlife.com/witchcityrope"
                  target="_blank"
                  rel="noopener noreferrer"
                  aria-label="Follow us on FetLife"
                  className="social-link"
                >
                  <FetLifeIcon />
                  <span>FetLife</span>
                </Anchor>
                <Anchor
                  href="https://instagram.com/witchcityrope"
                  target="_blank"
                  rel="noopener noreferrer"
                  aria-label="Follow us on Instagram"
                  className="social-link"
                >
                  <InstagramIcon />
                  <span>Instagram</span>
                </Anchor>
              </Group>
            </div>
          </Box>
        )}

        {/* Footer Bottom Section */}
        <div className="footer-bottom">
          <Text className="privacy-notice">
            We protect the privacy of our members. Location details are shared only with vetted members and ticket holders.
          </Text>
          <Group className="footer-meta" justify={isMobile ? 'flex-start' : 'space-between'}>
            <Text size="xs">&copy; 2025 WitchCityRope. All rights reserved.</Text>
            <Anchor href="mailto:info@witchcityrope.com" size="xs">
              info@witchcityrope.com
            </Anchor>
          </Group>
        </div>
      </Box>
    </footer>
  );
};
```

### Footer CSS (Simplified)

```css
/* Footer Container */
.footer {
  background-color: var(--color-midnight);
  color: var(--color-taupe);
  border-top: 3px solid var(--color-rose-gold);
  padding: var(--space-xl) 0;
}

.footer-content {
  max-width: 1200px;
  margin: 0 auto;
  padding: 0 20px;
}

/* Section Title */
.footer-section-title {
  font-family: var(--font-heading);
  font-size: 16px;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 1px;
  color: var(--color-ivory);
  position: relative;
  display: inline-block;
  padding-bottom: 4px;
}

/* Underline Animation */
.footer-section-title::after {
  content: '';
  position: absolute;
  bottom: 0;
  left: 50%;
  transform: translateX(-50%);
  width: 0;
  height: 2px;
  background: linear-gradient(90deg, transparent, var(--color-rose-gold), transparent);
  transition: width 0.3s ease;
}

/* Mobile: Show underline when expanded */
@media (max-width: 767px) {
  .footer-section-header[aria-expanded="true"] .footer-section-title::after {
    width: 100%;
  }
}

/* Desktop: Show underline on hover */
@media (min-width: 768px) {
  .footer-section:hover .footer-section-title::after {
    width: 100%;
  }

  .footer-content {
    padding: 0 40px;
  }

  .footer-bottom {
    grid-column: 1 / -1;
    border-top: 1px solid rgba(183, 109, 117, 0.2);
  }

  .privacy-notice {
    margin-top: 10px;
  }
}

/* Footer Links */
.footer-links {
  list-style: none;
  padding: 0;
  margin: 0;
}

.footer-links li {
  margin-bottom: 12px;
}

.footer-links a {
  color: var(--color-taupe);
  text-decoration: none;
  font-size: 14px;
  transition: color 0.2s ease;
}

.footer-links a:hover {
  color: var(--color-rose-gold);
}

/* Social Links */
.social-link {
  display: flex;
  align-items: center;
  gap: 8px;
  color: var(--color-taupe);
  text-decoration: none;
  transition: color 0.2s ease;
}

.social-link:hover {
  color: var(--color-rose-gold);
}

/* Privacy Notice */
.privacy-notice {
  font-size: 13px;
  line-height: 1.6;
  font-style: italic;
  color: var(--color-taupe);
  margin-bottom: var(--space-sm);
}

/* Footer Meta */
.footer-meta {
  font-size: 13px;
  color: var(--color-taupe);
}

.footer-meta a {
  color: var(--color-taupe);
  text-decoration: none;
  transition: color 0.2s ease;
}

.footer-meta a:hover {
  color: var(--color-rose-gold);
}

/* Footer Bottom */
.footer-bottom {
  margin-top: 0;
  padding-top: 0;
}

@media (min-width: 768px) {
  .footer-bottom {
    border-top: 1px solid rgba(183, 109, 117, 0.2);
  }

  .privacy-notice {
    margin-top: 10px;
  }
}

/* Reduced Motion */
@media (prefers-reduced-motion: reduce) {
  .footer-section-title::after,
  .footer-links a,
  .social-link,
  .footer-meta a {
    transition: none;
  }
}
```

---

## 11. Quality Checklist

Before considering implementation complete, verify:

### Design Accuracy
- [ ] Footer matches approved mockup pixel-perfectly on mobile
- [ ] Footer matches approved mockup pixel-perfectly on desktop
- [ ] All spacing values match specification (use browser DevTools to measure)
- [ ] All colors match Design System v7 palette (use color picker)
- [ ] Typography matches specification (font family, size, weight, letter-spacing)
- [ ] Underline animation is center-outward gradient
- [ ] Accordion icon rotates 180deg on expansion

### Responsive Behavior
- [ ] Mobile (<768px): Accordion sections collapsed by default
- [ ] Mobile: Sections expand on click
- [ ] Mobile: Only one section expanded at a time (or all can be expanded - clarify)
- [ ] Desktop (≥768px): All sections always expanded
- [ ] Desktop: Three-column grid layout
- [ ] Desktop: Accordion icons hidden
- [ ] Resize from desktop to mobile: Sections collapse
- [ ] Resize from mobile to desktop: Sections expand
- [ ] No layout shift or janky animations on resize

### Accessibility
- [ ] Keyboard navigation works (Tab through all links)
- [ ] Enter/Space toggles accordion section (mobile)
- [ ] Focus rings visible on all interactive elements (2px solid rose gold)
- [ ] Screen reader announces section expansion state (aria-expanded)
- [ ] All links have proper labels (aria-label for social media)
- [ ] Color contrast meets WCAG 2.1 AA (4.5:1 minimum)
- [ ] Reduced motion support (animations disabled if user prefers)

### Content
- [ ] All 6 links present and correct
- [ ] Social media links have correct URLs
- [ ] Email link is clickable mailto link
- [ ] Privacy notice text matches specification exactly
- [ ] Copyright year is 2025
- [ ] External links open in new tab (target="_blank")
- [ ] External links have security attributes (rel="noopener noreferrer")

### Performance
- [ ] Animations run at 60fps (use Chrome DevTools Performance tab)
- [ ] No console errors or warnings
- [ ] Footer loads quickly (no blocking resources)
- [ ] Resize handler is debounced (250ms delay)
- [ ] No memory leaks (event listeners cleaned up)

### Integration
- [ ] Footer appears on all pages
- [ ] Footer is placed correctly in layout (below main content)
- [ ] Footer doesn't overlap with other components
- [ ] React Router links navigate correctly
- [ ] Footer works with existing authentication/routing

### Browser Support
- [ ] Chrome/Edge 90+ (latest)
- [ ] Firefox 88+ (latest)
- [ ] Safari 14+ (latest)
- [ ] Mobile Safari iOS 14+
- [ ] Chrome Android

---

## 12. Next Steps After Implementation

### Immediate Tasks
1. **Create Static Pages**: Privacy Policy, Terms of Service, Refund Policy
2. **Verify Existing Pages**: About Us, Code of Conduct, Contact Us
3. **Update Links**: Ensure all footer links route to correct pages
4. **CMS Integration**: Connect static pages to CMS for content editing

### Future Enhancements
1. **Email Newsletter Signup**: Add newsletter form to Connect section
2. **Additional Social Media**: Add more platforms if needed (Twitter, Threads, etc.)
3. **Footer Analytics**: Track link clicks for usage insights
4. **Sitemap Link**: Add sitemap link to Legal section
5. **Language Selection**: Multi-language support if needed

### Testing Milestones
1. **Unit Tests**: Coverage >80% for Footer component
2. **Integration Tests**: Footer appears on all routes
3. **E2E Tests**: Playwright tests for responsive behavior
4. **Accessibility Audit**: WCAG 2.1 AA compliance verified
5. **Visual Regression**: Screenshots match approved mockup

---

## 13. Summary for Orchestrator

### Design Specification Complete

**Document Created**: `/home/chad/repos/witchcityrope/docs/functional-areas/footer-design/new-work/2025-11-08-footer-research/design/footer-design-specification.md`

### Key Specifications Documented

1. **Component Structure**: Mantine Accordion with FooterSection and FooterBottom components
2. **Color Palette**: Midnight background, rose gold accents, taupe text, ivory titles
3. **Typography**: Montserrat (headings), Source Sans 3 (body), sizes 13-16px
4. **Spacing**: Design System v7 spacing scale (8px-40px)
5. **Responsive**: Single breakpoint at 768px (mobile/desktop)
6. **Layout**: Mobile accordion, desktop three-column grid
7. **Animations**: Center-outward underline (0.3s), accordion expansion (0.3s), icon rotation (0.3s)
8. **Content**: 6 links, 2 social media, privacy notice, copyright, email
9. **Accessibility**: WCAG 2.1 AA, keyboard navigation, screen reader support
10. **Implementation**: React + TypeScript + Mantine v7 with detailed code examples

### Ready for Implementation

**Document Status**: ✅ Complete and ready for react-developer

**Questions/Clarifications**: None - All specifications extracted from approved mockup

**Next Phase**: Hand off to react-developer for implementation

---

**End of Design Specification Document**

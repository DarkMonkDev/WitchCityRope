# WitchCityRope UI Testing Checklist

**Created:** June 28, 2025  
**Purpose:** Comprehensive UI testing guide for all pages and components  
**Testing Framework:** Manual testing with MCP-style commands

## Table of Contents
1. [Testing Prerequisites](#testing-prerequisites)
2. [Home Page Testing](#1-home-page---httplocalhost5651)
3. [Events Page Testing](#2-events-page---httplocalhost5651events)
4. [Login Page Testing](#3-login-page---httplocalhost5651authlogin)
5. [Common UI Elements](#4-common-ui-elements)
6. [Responsive Design Testing](#5-responsive-design-testing)
7. [Performance Testing](#6-performance-testing)
8. [Accessibility Testing](#7-accessibility-testing)

---

## Testing Prerequisites

### Environment Setup
- [ ] Application running on http://localhost:5651/
- [ ] Browser DevTools accessible (F12)
- [ ] Screenshot tools ready (DevTools or extension)
- [ ] Multiple browsers available (Chrome, Edge, Firefox)
- [ ] Network throttling available for performance testing

### MCP Commands Reference
```bash
# Check if site is running
curl -I http://localhost:5651/

# Get page content
curl -s http://localhost:5651/ | grep -E "<title>|<h1>|class=\"hero\""

# Check specific endpoints
curl -I http://localhost:5651/events
curl -I http://localhost:5651/auth/login
```

---

## 1. Home Page - http://localhost:5651/

### Visual Elements to Verify

#### Hero Section
- [ ] **Tagline**: "Where curiosity meets connection" in cursive Satisfy font
- [ ] **Tagline Color**: Amber (#FFBF00)
- [ ] **Title Structure**: 3 lines with "Education & Practice" emphasized
- [ ] **Background**: Animated diagonal line pattern visible
- [ ] **Animation**: Lines moving subtly

#### Call-to-Action Buttons
- [ ] **Primary Button**: "Explore Classes" with amber gradient (#FFBF00 → #FF8C00)
- [ ] **Secondary Button**: "Join Community" with purple gradient (#9D4EDD → #7B2CBF)
- [ ] **Hover Effects**: Gradients reverse on hover
- [ ] **Button Spacing**: Proper gap between buttons

#### Rope Divider
- [ ] **SVG Rendering**: Burgundy rope SVG visible
- [ ] **Animation**: Gentle swaying motion (transform: rotate)
- [ ] **Knot Decorations**: Rotating animation on knots
- [ ] **Responsive**: Scales properly on different viewports

#### Features Section
- [ ] **Section Title**: "What Makes Our Community Special"
- [ ] **Card Count**: 4 feature cards displayed
- [ ] **Card Content**:
  - [ ] Expert Teaching (with icon)
  - [ ] Welcoming Community (with icon)
  - [ ] Safety First (with icon)
  - [ ] Everyone Belongs (with icon)
- [ ] **Card Styling**: Proper shadows and hover effects

### MCP Test Commands
```bash
# Verify hero content
curl -s http://localhost:5651/ | grep -i "where curiosity meets connection"

# Check for feature section
curl -s http://localhost:5651/ | grep -i "what makes our community special"

# Verify button presence
curl -s http://localhost:5651/ | grep -E "explore classes|join community"
```

### Expected Results
- Page loads within 2 seconds
- All animations start immediately
- No layout shifts after initial load
- Fonts load without FOUT (Flash of Unstyled Text)

### Common Issues
- [ ] Missing fonts: Check Google Fonts loading
- [ ] Broken animations: Verify CSS animations not disabled
- [ ] Layout shifts: Check image lazy loading

---

## 2. Events Page - http://localhost:5651/events

### Visual Elements to Verify

#### Page Header
- [ ] **Title**: "Explore Classes & Meetups"
- [ ] **Subtitle**: Descriptive text about events
- [ ] **Background**: Consistent with site theme

#### Search & Filter Bar
- [ ] **Search Box**: 
  - [ ] Magnifying glass icon inside field
  - [ ] Icon color: Stone (#8B8680)
  - [ ] Placeholder text: "Search events..."
- [ ] **Filter Buttons**:
  - [ ] "Show Past Classes" toggle
  - [ ] Sort dropdown (Date, Title, etc.)
- [ ] **Layout**: Proper spacing and alignment

#### Event Cards Display
- [ ] **Card Count**: Should show 6 test events (if mock data enabled)
- [ ] **Card Elements**:
  - [ ] Event image/placeholder
  - [ ] Title in card header
  - [ ] Date and time
  - [ ] Instructor name
  - [ ] Price or "Free" label
  - [ ] Status badge (Available/Limited/Sold Out)
- [ ] **Card States**:
  - [ ] Available (green indicator)
  - [ ] Limited Spots (orange indicator)
  - [ ] Sold Out (red indicator)
- [ ] **Card Types**:
  - [ ] Beginner (specific styling)
  - [ ] Intermediate (specific styling)
  - [ ] Advanced (specific styling)
  - [ ] Member Only (lock icon)

#### Empty State (if no events)
- [ ] **Icon**: Calendar icon displayed
- [ ] **Message**: "No events found"
- [ ] **Subtext**: "Try adjusting your filters..."
- [ ] **Reset Button**: Functional and styled

### MCP Test Commands
```bash
# Check events page structure
curl -s http://localhost:5651/events | grep -E "search-box|event-card|empty-state"

# Verify search functionality
curl -s http://localhost:5651/events | grep -i "search events"

# Check for filter controls
curl -s http://localhost:5651/events | grep -i "show past"
```

### Expected Results
- Search responds instantly to typing
- Filters update results without page reload
- Cards animate in smoothly
- Empty state appears when no results

### Common Issues
- [ ] Mock data not loading: Check data initialization
- [ ] Search not working: Verify Blazor components loaded
- [ ] Filter state not persisting: Check state management

---

## 3. Login Page - http://localhost:5651/auth/login

### Visual Elements to Verify

#### Page Layout
- [ ] **Header**: Gradient background (burgundy to plum)
- [ ] **Card**: White with rounded corners and shadow
- [ ] **Max Width**: 480px centered container

#### Authentication Header
- [ ] **Title**: "Welcome Back" (Sign In) / "Join Our Community" (Sign Up)
- [ ] **Age Notice**: "21+ COMMUNITY • AGE VERIFICATION REQUIRED"
- [ ] **Styling**: Proper font hierarchy

#### Tab Navigation
- [ ] **Tabs**: "Sign In" and "Create Account"
- [ ] **Active State**: Proper highlighting
- [ ] **Transition**: Smooth tab switching

#### Google OAuth Section
- [ ] **Button Text**: "Continue with Google"
- [ ] **Google Logo**: SVG with official colors
- [ ] **Button Style**: White background, proper shadow
- [ ] **Hover Effect**: Elevation change on hover
- [ ] **Position**: Above email/password fields

#### Divider
- [ ] **Text**: "or" with horizontal lines
- [ ] **Styling**: Centered with proper spacing

#### Email/Password Form
- [ ] **Email Field**: 
  - [ ] Label: "Email"
  - [ ] Type: email
  - [ ] Validation indicators
- [ ] **Password Field**:
  - [ ] Label: "Password"
  - [ ] Type: password
  - [ ] Show/hide toggle
- [ ] **Remember Me**: Checkbox with label
- [ ] **Submit Button**: Primary button styling

#### Footer Links
- [ ] **Forgot Password**: Link present and styled
- [ ] **Terms**: Link to terms of service

### MCP Test Commands
```bash
# Check login page structure
curl -s http://localhost:5651/auth/login | grep -E "sign-in|create-account|google"

# Verify OAuth endpoint
curl -I http://localhost:5651/api/auth/google-login

# Check form elements
curl -s http://localhost:5651/auth/login | grep -E "email|password|remember"
```

### Expected Results
- OAuth redirects to Google properly
- Form validation works client-side
- Tab switching is instant
- Error messages display clearly

### Common Issues
- [ ] OAuth button not visible: Ensure JavaScript enabled
- [ ] Form not submitting: Check Blazor SignalR connection
- [ ] Validation not working: Verify client-side scripts

---

## 4. Common UI Elements

### Navigation Bar
- [ ] **Logo**: Witch City Rope branding
- [ ] **Menu Items**: Proper highlighting for active page
- [ ] **User Menu**: Dropdown for authenticated users
- [ ] **Mobile Toggle**: Hamburger menu on small screens

### Footer
- [ ] **Sections**: 4 columns (Education, Community, Resources, Connect)
- [ ] **Newsletter Form**: 
  - [ ] Email input
  - [ ] Subscribe button
  - [ ] Success/error messages
- [ ] **Social Links**:
  - [ ] Discord icon and link
  - [ ] FetLife icon and link
  - [ ] Instagram icon and link
- [ ] **Copyright**: Current year and proper text

### Toast Notifications
- [ ] **Position**: Top-right corner
- [ ] **Animation**: Slide in/out
- [ ] **Types**: Success (green), Error (red), Info (blue)
- [ ] **Auto-dismiss**: After 5 seconds

### Color System Verification
- [ ] **Primary**: Burgundy (#8B0000)
- [ ] **Secondary**: Plum (#DDA0DD)
- [ ] **Accent**: Amber (#FFBF00)
- [ ] **Background**: Ivory (#FFFFF0)
- [ ] **Text**: Charcoal (#36454F)

---

## 5. Responsive Design Testing

### Breakpoints to Test
- [ ] **Mobile**: 375px (iPhone SE)
- [ ] **Large Mobile**: 414px (iPhone Plus)
- [ ] **Tablet**: 768px (iPad)
- [ ] **Desktop**: 1024px
- [ ] **Large Desktop**: 1920px

### Mobile Specific Checks
- [ ] Navigation collapses to hamburger
- [ ] Hero text scales appropriately
- [ ] Buttons stack vertically
- [ ] Cards become full width
- [ ] Footer stacks to single column
- [ ] Touch targets are 44px minimum

### Tablet Specific Checks
- [ ] 2-column layouts for cards
- [ ] Side-by-side buttons where appropriate
- [ ] Readable font sizes
- [ ] Proper padding adjustments

### MCP Commands for Viewport Testing
```bash
# Use browser DevTools device emulation
# Chrome: Ctrl+Shift+M in DevTools
# Test each breakpoint systematically
```

---

## 6. Performance Testing

### Load Time Metrics
- [ ] **First Contentful Paint**: < 1.5s
- [ ] **Largest Contentful Paint**: < 2.5s
- [ ] **Time to Interactive**: < 3.5s
- [ ] **Cumulative Layout Shift**: < 0.1

### Resource Loading
- [ ] **CSS Files**: 4 stylesheets maximum
- [ ] **JavaScript**: 3 core scripts
- [ ] **Fonts**: Preloaded critical fonts
- [ ] **Images**: Lazy loaded where appropriate

### Network Testing
- [ ] **3G Slow**: Page usable within 10s
- [ ] **Offline**: Proper error messages

### MCP Performance Commands
```bash
# Check resource sizes
curl -s http://localhost:5651/ | wc -c

# List all loaded resources
curl -s http://localhost:5651/ | grep -E "<link|<script|<img"

# Check response headers
curl -I http://localhost:5651/
```

---

## 7. Accessibility Testing

### Keyboard Navigation
- [ ] **Tab Order**: Logical flow through page
- [ ] **Focus Indicators**: Visible on all interactive elements
- [ ] **Skip Links**: "Skip to content" available
- [ ] **Trap Focus**: Modals properly trap focus

### Screen Reader Testing
- [ ] **Headings**: Proper hierarchy (h1 → h6)
- [ ] **Landmarks**: nav, main, footer roles
- [ ] **Alt Text**: All images have descriptions
- [ ] **ARIA Labels**: Buttons and links descriptive

### Color Contrast
- [ ] **Normal Text**: 4.5:1 ratio minimum
- [ ] **Large Text**: 3:1 ratio minimum
- [ ] **Focus States**: Sufficient contrast

### Form Accessibility
- [ ] **Labels**: Associated with inputs
- [ ] **Error Messages**: Announced to screen readers
- [ ] **Required Fields**: Marked with aria-required

### MCP Accessibility Commands
```bash
# Check for alt text
curl -s http://localhost:5651/ | grep -E "alt=|aria-label="

# Verify heading structure
curl -s http://localhost:5651/ | grep -E "<h[1-6]"

# Check for ARIA landmarks
curl -s http://localhost:5651/ | grep -E "role=|aria-"
```

---

## Test Execution Checklist

### Pre-Test Setup
1. [ ] Clear browser cache
2. [ ] Disable browser extensions
3. [ ] Set up screenshot naming convention
4. [ ] Open browser DevTools

### During Testing
1. [ ] Take screenshots at each breakpoint
2. [ ] Document any issues found
3. [ ] Test all interactive elements
4. [ ] Verify animations and transitions
5. [ ] Check console for errors

### Post-Test
1. [ ] Organize screenshots by date/page
2. [ ] Create issue tickets for bugs
3. [ ] Update test results documentation
4. [ ] Share findings with team

### Screenshot Naming Convention
```
wcr-[page]-[viewport]-[element]-YYYY-MM-DD.png

Examples:
- wcr-homepage-desktop-hero-2025-06-28.png
- wcr-events-mobile-cards-2025-06-28.png
- wcr-login-tablet-oauth-2025-06-28.png
```

---

## Known Issues & Workarounds

### Issue: Events not displaying
**Workaround**: Check if mock data service is initialized in Startup.cs

### Issue: OAuth button not visible
**Workaround**: Ensure JavaScript is enabled and Blazor has loaded

### Issue: Animations not working
**Workaround**: Check if prefers-reduced-motion is enabled

### Issue: Fonts not loading
**Workaround**: Verify Google Fonts CDN is accessible

---

## Testing Tools Reference

### Browser DevTools
- **Chrome/Edge**: F12 or Ctrl+Shift+I
- **Firefox**: F12 or Ctrl+Shift+I
- **Safari**: Cmd+Option+I

### Screenshot Extensions
- **GoFullPage**: Chrome/Edge full page capture
- **Fireshot**: Multi-browser screenshot tool
- **Awesome Screenshot**: Annotation features

### Performance Tools
- **Lighthouse**: Built into Chrome DevTools
- **WebPageTest**: Online performance testing
- **GTmetrix**: Performance and optimization

### Accessibility Tools
- **axe DevTools**: Comprehensive accessibility testing
- **WAVE**: WebAIM accessibility evaluation
- **NVDA/JAWS**: Screen reader testing

---

## Notes

- Always test in incognito/private mode for clean state
- Document browser version and OS for bug reports
- Compare against design mockups in `/docs/design/wireframes/`
- Use consistent test data for reproducibility
- Test both light and dark mode if implemented
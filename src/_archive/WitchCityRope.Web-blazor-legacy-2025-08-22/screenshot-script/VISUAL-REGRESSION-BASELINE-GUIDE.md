# Visual Regression Testing - Baseline Comparison Guide

## Overview

This guide provides a comprehensive framework for visual regression testing of the WitchCityRope web application. Visual regression testing ensures that UI changes don't introduce unintended visual bugs by comparing screenshots against baseline images.

## Test Suite Components

### 1. Visual Regression Test Script (`visual-regression-test.js`)
- Captures screenshots of all key pages
- Tests multiple viewport sizes (desktop, tablet, mobile)
- Captures interactive states (default, hover, focus, active)
- Performs visual quality checks

### 2. Visual Comparison Tool (`visual-regression-compare.js`)
- Compares current screenshots against baseline images
- Generates difference images highlighting changes
- Produces detailed comparison reports
- Manages baseline image updates

## Pages Tested

| Page | Path | Key Elements | Auth Required |
|------|------|--------------|---------------|
| Homepage | `/` | Hero section, tagline, rope divider, gradient buttons | No |
| Events List | `/events` | Event cards, filters, pagination | No |
| Event Detail | `/events/:id` | Event info, registration button, attendee list | No |
| Login | `/login` | Login form, OAuth buttons, password reset link | No |
| Dashboard | `/dashboard` | User stats, upcoming events, recent activity | Yes |
| Profile | `/profile` | User info, avatar, settings | Yes |
| Admin Events | `/admin/events` | Event management table, actions | Admin |
| Admin Vetting | `/admin/vetting` | Vetting queue, approval controls | Admin |

## Viewport Specifications

### Desktop
- **Width:** 1920px
- **Height:** 1080px
- **Device Scale Factor:** 1
- **Use Case:** Full desktop experience, all features visible

### Tablet
- **Width:** 768px
- **Height:** 1024px
- **Device Scale Factor:** 2
- **Use Case:** Touch-optimized layout, responsive navigation

### Mobile
- **Width:** 375px
- **Height:** 812px
- **Device Scale Factor:** 3
- **Use Case:** Mobile-first design, hamburger menu, stacked layout

## Interactive States

### 1. Default State
- Initial page load appearance
- No user interaction
- Base styling applied

### 2. Hover State
- Mouse hover effects on buttons and links
- Tooltip displays
- Color/shadow transitions

### 3. Focus State
- Keyboard navigation indicators
- Form field focus rings
- Accessibility compliance

### 4. Active State
- Button press animations
- Link click feedback
- Loading states

## Expected Visual Output

### Homepage Elements

#### Hero Section
- **Background:** Dark gradient with rope texture overlay
- **Title:** "WitchCityRope" in custom font
- **Tagline:** "Where curiosity meets connection" - centered, white text
- **CTA Buttons:** Purple/pink gradient with hover glow effect

#### Rope Divider SVG
- **Design:** Animated rope pattern
- **Color:** Gold/bronze gradient
- **Animation:** Subtle sway motion (2s duration)
- **Position:** Between hero and content sections

#### Navigation
- **Desktop:** Horizontal menu with gradient underline on active
- **Mobile:** Hamburger menu with slide-out drawer

### Event Pages

#### Event Cards
- **Layout:** Grid layout (3 columns desktop, 1 column mobile)
- **Shadow:** Elevated card effect with hover lift
- **Image:** 16:9 aspect ratio with gradient overlay
- **Text:** Title, date, location, description preview

#### Event Filters
- **Style:** Pill-shaped toggle buttons
- **States:** Active (gradient background), Inactive (outline only)
- **Categories:** All, Workshops, Social, Educational, Performance

### Form Elements

#### Input Fields
- **Border:** 1px solid #ddd, 2px solid purple on focus
- **Background:** White with slight inset shadow
- **Label:** Floating label animation on focus
- **Error State:** Red border with error message below

#### Buttons
- **Primary:** Purple/pink gradient, white text, shadow on hover
- **Secondary:** White background, purple text, purple border
- **Disabled:** Gray background, reduced opacity

### Color Palette

| Element | Color | Hex Code |
|---------|-------|----------|
| Primary Purple | Deep Purple | #6B46C1 |
| Secondary Pink | Hot Pink | #EC4899 |
| Background Dark | Near Black | #111827 |
| Background Light | Light Gray | #F9FAFB |
| Text Primary | Dark Gray | #1F2937 |
| Text Secondary | Medium Gray | #6B7280 |
| Success | Green | #10B981 |
| Warning | Yellow | #F59E0B |
| Error | Red | #EF4444 |

## Visual Quality Checks

### 1. Layout Integrity
- No horizontal scroll at any viewport
- Proper element spacing and alignment
- Responsive breakpoints working correctly

### 2. Image Quality
- All images loading properly
- Correct aspect ratios maintained
- No pixelation or stretching

### 3. Typography
- Font loading correctly
- Line heights and spacing consistent
- Text readable at all sizes

### 4. Interactive Feedback
- Hover states visible and smooth
- Focus indicators meet WCAG standards
- Loading states display properly

### 5. Cross-Browser Consistency
- Chrome/Edge rendering identical
- Firefox minor variations acceptable
- Safari gradient rendering differences noted

## Baseline Management

### Creating Initial Baselines
```bash
# Run visual regression tests
node visual-regression-test.js

# Create baselines from current screenshots
node visual-regression-compare.js --create-baseline
```

### Updating Baselines
1. Review current test failures
2. Verify changes are intentional
3. Update baseline images:
   ```bash
   node visual-regression-compare.js --create-baseline
   ```
4. Commit new baselines to version control

### Best Practices
- Always review diff images before updating baselines
- Document reason for baseline updates in commit messages
- Keep baseline images in version control
- Run tests on consistent environment (same OS, browser version)

## Interpreting Results

### Pass Criteria
- Pixel difference < 1% (configurable threshold)
- No missing elements
- No layout shifts
- All interactive states captured

### Common Acceptable Differences
- Anti-aliasing variations (< 0.1%)
- Font rendering differences between OS
- Animation frame timing
- Dynamic content (dates, user counts)

### Investigation Required
- Difference > 1%
- Missing UI elements
- Layout shifts or overflow
- Broken images or assets
- Console errors during capture

## Troubleshooting

### Screenshot Capture Issues
- **Problem:** Screenshots are blank
- **Solution:** Increase wait time for content loading

### Comparison Failures
- **Problem:** High difference percentage on identical pages
- **Solution:** Check for dynamic content, animations, or timestamps

### Performance Issues
- **Problem:** Tests take too long
- **Solution:** Run viewports in parallel, optimize wait times

## CI/CD Integration

### GitHub Actions Example
```yaml
name: Visual Regression Tests
on: [push, pull_request]

jobs:
  visual-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      - uses: actions/setup-node@v2
      - run: npm install
      - run: npm run test:visual
      - uses: actions/upload-artifact@v2
        if: failure()
        with:
          name: visual-regression-report
          path: screenshot-script/reports/
```

## Reporting

### Generated Reports
1. **JSON Report:** Detailed test results with metrics
2. **HTML Report:** Visual comparison with side-by-side images
3. **Markdown Summary:** Quick overview for PR comments
4. **Diff Images:** Highlighted pixel differences

### Report Locations
- Current screenshots: `screenshots/current/`
- Baseline images: `screenshots/baseline/`
- Difference images: `screenshots/diff/`
- Reports: `reports/`

## Maintenance Schedule

### Weekly
- Run full regression suite
- Review any failures
- Update baselines if needed

### Monthly
- Review and optimize test coverage
- Update viewport sizes if needed
- Clean up old screenshots

### Quarterly
- Audit color palette consistency
- Review animation timings
- Update this guide with new patterns
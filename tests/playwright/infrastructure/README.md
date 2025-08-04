# Infrastructure Tests

This directory contains tests for application infrastructure, including CSS loading, error handling, page structure, and layout systems.

## Test Files

### css-loading.spec.ts
- Monitors CSS file loading and responses
- Verifies CSS custom properties are defined
- Tests styled components and their computed styles
- Checks responsive CSS breakpoints
- Validates no CSS-related console errors

### error-timing.spec.ts
- Tracks error timing on page loads
- Monitors console errors, HTTP errors, and page errors
- Identifies JavaScript initialization errors
- Tracks network request failures
- Tests error patterns across different pages

### page-status.spec.ts
- Verifies admin pages load correctly
- Tests page structure and form elements
- Checks Blazor initialization status
- Monitors page load performance
- Validates critical routes are accessible

### layout-system.spec.ts
- Verifies layout system works for Razor pages
- Compares layout between Razor and Blazor pages
- Checks for layout components (header, footer, nav)
- Validates CSS and JavaScript loading in layout
- Tests meta tags and SEO elements

### styling.spec.ts
- Verifies CSS variables are properly loaded
- Tests component-specific styling (login card, buttons)
- Validates theme consistency across pages
- Tests responsive styling changes
- Checks for missing styles or broken CSS

## Key Infrastructure Checks

1. **Resource Loading**
   - CSS files return 200 status
   - JavaScript files load without errors
   - No 404s for critical resources

2. **Error Monitoring**
   - Console errors are tracked with timestamps
   - HTTP 500+ errors are captured
   - Page errors are logged

3. **Performance**
   - Page load times are measured
   - DOM Content Loaded timing
   - Time to Interactive metrics

4. **Layout Consistency**
   - All pages have consistent layout structure
   - CSS variables are properly defined
   - Responsive behavior works correctly

## Running Tests

```bash
# Run all infrastructure tests
npx playwright test infrastructure/

# Run specific category
npx playwright test infrastructure/css-loading.spec.ts

# Run with trace for debugging
npx playwright test infrastructure/ --trace on
```

## Common Issues

- **CSS not loading**: Check webpack/bundling configuration
- **Layout missing on Blazor pages**: Verify _Layout.cshtml usage
- **Slow page loads**: Check for blocking resources or large bundles
- **Errors on page load**: Look for missing dependencies or initialization order issues
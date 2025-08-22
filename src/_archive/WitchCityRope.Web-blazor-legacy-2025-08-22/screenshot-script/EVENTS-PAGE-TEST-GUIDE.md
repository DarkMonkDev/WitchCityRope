# Events Page Comprehensive Test Guide

## Test Overview
This guide provides step-by-step instructions for testing the events page at http://localhost:5651/events

## Prerequisites
- Local development server running at http://localhost:5651
- Modern web browser (Chrome, Edge, or Firefox recommended)
- Browser Developer Tools

## Test 1: Initial Page Load and Event Cards

### Steps:
1. Open browser and navigate to http://localhost:5651/events
2. Open Developer Tools (F12)
3. Go to Network tab and refresh the page
4. Note the page load time

### Verification:
- [ ] Page loads successfully (status 200)
- [ ] At least 4 event cards are visible
- [ ] Each event card displays:
  - [ ] Event title
  - [ ] Date/time
  - [ ] Location
  - [ ] Brief description
  - [ ] Image (if applicable)

### Screenshot:
- Take a full-page screenshot showing all event cards
- Name: `1-initial-load.png`

## Test 2: Search Functionality

### Steps:
1. Locate the search input field
2. Type "rope" in the search box
3. Wait for results to filter

### Verification:
- [ ] Search input is visible and functional
- [ ] Typing "rope" filters the events
- [ ] Only relevant events containing "rope" are shown
- [ ] No JavaScript errors in console

### Screenshots:
- Screenshot after typing "rope": `2-search-rope.png`
- Screenshot after clearing search: `3-search-cleared.png`

### Additional Tests:
1. Clear the search box
2. Verify all events reappear
3. Test edge cases:
   - Empty search
   - Special characters
   - Very long search terms

## Test 3: Performance Audit

### Using Chrome DevTools:
1. Open Performance tab in DevTools
2. Click "Record" and refresh the page
3. Stop recording after page loads

### Metrics to Check:
- [ ] First Contentful Paint (FCP) < 1.8s
- [ ] Largest Contentful Paint (LCP) < 2.5s
- [ ] Time to Interactive (TTI) < 3.8s
- [ ] Total Blocking Time (TBT) < 300ms

### Console Errors:
1. Open Console tab
2. Check for any errors or warnings
3. Document any issues found

### Network Analysis:
1. Open Network tab
2. Check for:
   - [ ] Failed requests (4xx, 5xx errors)
   - [ ] Large image files (> 500KB)
   - [ ] Slow API calls (> 1s)

## Test 4: Responsive Design - Mobile View

### Steps:
1. Open Device Toolbar in DevTools (Ctrl+Shift+M)
2. Set viewport to iPhone SE (375x667)
3. Refresh the page

### Verification:
- [ ] Event cards stack vertically
- [ ] Text is readable without horizontal scrolling
- [ ] Images resize appropriately
- [ ] Search functionality works on mobile
- [ ] Touch targets are at least 44x44 pixels

### Screenshot:
- Mobile view screenshot: `4-mobile-view.png`

### Additional Viewports to Test:
- iPad (768x1024)
- iPhone 14 Pro Max (430x932)
- Samsung Galaxy S20 (360x800)

## Test 5: Sorting and Filtering

### If Available:
1. Look for sort options (date, name, etc.)
2. Test each sort option
3. Verify events reorder correctly

### Verification:
- [ ] Sort controls are visible
- [ ] Sorting works in both directions
- [ ] Current sort is clearly indicated

## Test 6: Accessibility

### Using Chrome DevTools:
1. Run Lighthouse audit (Performance tab)
2. Focus on Accessibility score

### Manual Checks:
- [ ] All images have alt text
- [ ] Form inputs have labels
- [ ] Contrast ratio meets WCAG standards
- [ ] Keyboard navigation works
- [ ] Screen reader announces content properly

## Test 7: Cross-Browser Testing

Test the page in:
- [ ] Chrome/Edge (Chromium)
- [ ] Firefox
- [ ] Safari (if on Mac)

## Issues Documentation Template

For each issue found, document:

```markdown
### Issue: [Brief Description]
- **Severity**: High/Medium/Low
- **Steps to Reproduce**:
  1. Step 1
  2. Step 2
- **Expected Result**: What should happen
- **Actual Result**: What actually happens
- **Screenshot**: [filename]
- **Browser/Device**: Chrome 120 on Windows 11
```

## Performance Optimization Suggestions

Based on findings, consider:
1. Image optimization (WebP format, lazy loading)
2. Code splitting for JavaScript bundles
3. Caching strategies
4. Reducing API calls
5. Implementing virtual scrolling for large lists

## Summary Checklist

- [ ] All 4+ events display correctly
- [ ] Search functionality works
- [ ] No console errors
- [ ] Page loads in < 3 seconds
- [ ] Mobile responsive design works
- [ ] Accessibility score > 90
- [ ] Cross-browser compatibility verified

## Notes
- Test data may vary; ensure test environment has sufficient mock data
- Document any deviations from expected behavior
- Take screenshots of any issues for developer reference
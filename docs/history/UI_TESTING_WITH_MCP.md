# UI Testing with MCP Tools - Comprehensive Guide

## Overview

This guide provides comprehensive instructions for UI testing in the WitchCityRope project using Model Context Protocol (MCP) tools. We use two primary MCP servers for different testing needs:

- **Puppeteer MCP**: For capturing screenshots and browser automation
- **Browser-tools MCP**: For running audits (accessibility, performance, SEO) and debugging

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Tool Selection Guide](#tool-selection-guide)
3. [Common Testing Scenarios](#common-testing-scenarios)
4. [Step-by-Step Examples](#step-by-step-examples)
5. [Integration with Development Workflow](#integration-with-development-workflow)
6. [Troubleshooting](#troubleshooting)
7. [Best Practices](#best-practices)

## Prerequisites

### 1. MCP Configuration
Ensure your Claude Desktop config includes both MCP servers:

```json
{
  "mcpServers": {
    "puppeteer": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-puppeteer"]
    },
    "browser-tools": {
      "command": "npx",
      "args": ["browser-tools-mcp"]
    }
  }
}
```

### 2. Start the Application
```powershell
# Start UI monitoring (includes app startup)
.\tools\Start-UIMonitoring.ps1 -OpenBrowser

# Or manually start the app
cd src/WitchCityRope.Web
dotnet run
```

### 3. Verify MCP Tools Available
In Claude, the tools should appear as:
- `mcp_puppeteer.screenshot`
- `mcp_browser_tools.screenshot`
- `mcp_browser_tools.audit`

## Tool Selection Guide

### When to Use Puppeteer MCP

Use Puppeteer for:
- ✅ Basic screenshot capture
- ✅ Full-page screenshots
- ✅ Element-specific screenshots
- ✅ Testing with authentication
- ✅ Waiting for dynamic content
- ✅ Multiple viewport testing

### When to Use Browser-tools MCP

Use Browser-tools for:
- ✅ Accessibility audits
- ✅ Performance analysis
- ✅ SEO optimization checks
- ✅ Console error debugging
- ✅ Network request monitoring
- ✅ Best practices validation

## Common Testing Scenarios

### 1. New Feature UI Verification

```javascript
// Step 1: Capture baseline before changes
mcp_puppeteer.screenshot({
  url: "https://localhost:5652/feature-page",
  fullPage: true
})

// Step 2: Make your changes
// ... implement feature ...

// Step 3: Capture after changes
mcp_puppeteer.screenshot({
  url: "https://localhost:5652/feature-page",
  fullPage: true
})

// Step 4: Run accessibility audit
mcp_browser_tools.audit({
  url: "https://localhost:5652/feature-page",
  type: "accessibility"
})
```

### 2. Responsive Design Testing

```javascript
// Test mobile view
mcp_puppeteer.screenshot({
  url: "https://localhost:5652",
  viewport: { width: 375, height: 667 },
  fullPage: true
})

// Test tablet view
mcp_puppeteer.screenshot({
  url: "https://localhost:5652",
  viewport: { width: 768, height: 1024 },
  fullPage: true
})

// Test desktop view
mcp_puppeteer.screenshot({
  url: "https://localhost:5652",
  viewport: { width: 1920, height: 1080 },
  fullPage: true
})
```

### 3. Form Validation Testing

```javascript
// Capture form in error state
mcp_puppeteer.screenshot({
  url: "https://localhost:5652/auth/login",
  selector: "form",
  waitForSelector: ".validation-error"
})

// Debug JavaScript errors
mcp_browser_tools.screenshot({
  url: "https://localhost:5652/auth/login",
  includeLogs: true
})
```

### 4. Performance Impact Analysis

```javascript
// Before optimization
mcp_browser_tools.audit({
  url: "https://localhost:5652/events",
  type: "performance"
})

// After optimization
mcp_browser_tools.audit({
  url: "https://localhost:5652/events",
  type: "performance"
})
```

## Step-by-Step Examples

### Example 1: Testing Login Page OAuth Integration

```javascript
// 1. Capture login page
mcp_puppeteer.screenshot({
  url: "https://localhost:5652/auth/login",
  fullPage: true
})

// 2. Check for OAuth button visibility
mcp_puppeteer.screenshot({
  url: "https://localhost:5652/auth/login",
  selector: ".google-oauth-button",
  waitForSelector: ".google-oauth-button"
})

// 3. Test mobile responsiveness
mcp_puppeteer.screenshot({
  url: "https://localhost:5652/auth/login",
  viewport: { width: 375, height: 667 },
  fullPage: true
})

// 4. Run accessibility audit
mcp_browser_tools.audit({
  url: "https://localhost:5652/auth/login",
  type: "accessibility"
})
```

### Example 2: Testing Event List Page

```javascript
// 1. Capture event list
mcp_puppeteer.screenshot({
  url: "https://localhost:5652/events",
  fullPage: true,
  waitForSelector: ".event-card"
})

// 2. Test search functionality
mcp_puppeteer.screenshot({
  url: "https://localhost:5652/events?search=beginner",
  selector: ".event-list",
  waitForSelector: ".event-card"
})

// 3. Check performance with many events
mcp_browser_tools.audit({
  url: "https://localhost:5652/events",
  type: "performance"
})

// 4. Verify SEO metadata
mcp_browser_tools.audit({
  url: "https://localhost:5652/events",
  type: "seo"
})
```

### Example 3: Testing Admin Dashboard (Authenticated)

```javascript
// 1. Login and get auth token
// ... perform login ...

// 2. Capture admin dashboard
mcp_puppeteer.screenshot({
  url: "https://localhost:5652/admin",
  cookies: [{
    name: ".AspNetCore.Identity.Application",
    value: "your-auth-cookie-value",
    domain: "localhost"
  }],
  fullPage: true
})

// 3. Test data tables
mcp_puppeteer.screenshot({
  url: "https://localhost:5652/admin/events",
  selector: ".sf-grid",
  cookies: [/* auth cookie */]
})

// 4. Check for console errors
mcp_browser_tools.screenshot({
  url: "https://localhost:5652/admin",
  includeLogs: true,
  cookies: [/* auth cookie */]
})
```

## Integration with Development Workflow

### 1. Pre-Commit Testing

Add to your development routine:

```javascript
// Before committing UI changes
async function preCommitUITest() {
  // 1. Screenshot all changed pages
  const changedPages = ['/events', '/auth/login', '/'];
  
  for (const page of changedPages) {
    await mcp_puppeteer.screenshot({
      url: `https://localhost:5652${page}`,
      fullPage: true
    });
  }
  
  // 2. Run accessibility audits
  for (const page of changedPages) {
    await mcp_browser_tools.audit({
      url: `https://localhost:5652${page}`,
      type: "accessibility"
    });
  }
}
```

### 2. Feature Branch Testing

When working on a feature branch:

1. **Baseline Capture**: Screenshot relevant pages from main branch
2. **Development**: Make changes and test iteratively
3. **Final Verification**: Full audit before PR
4. **Documentation**: Include screenshots in PR description

### 3. CI/CD Integration

While MCP tools are for local development, you can create similar tests for CI:

```javascript
// Example Playwright test that mirrors MCP usage
test('homepage visual regression', async ({ page }) => {
  await page.goto('https://localhost:5652');
  await page.screenshot({ path: 'homepage.png', fullPage: true });
  // Compare with baseline
});
```

## Troubleshooting

### Common Issues and Solutions

#### 1. MCP Tools Not Available
**Problem**: `mcp_puppeteer` or `mcp_browser_tools` not found
**Solution**: 
- Restart Claude Desktop after config changes
- Verify Node.js is installed and in PATH
- Check MCP config file location

#### 2. SSL Certificate Errors
**Problem**: HTTPS errors when accessing localhost:5652
**Solution**:
```bash
# Regenerate dev certificates
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

#### 3. Blazor Pages Not Fully Loading
**Problem**: Screenshots show loading spinner or partial content
**Solution**:
```javascript
// Wait for Blazor to initialize
mcp_puppeteer.screenshot({
  url: "https://localhost:5652",
  waitForSelector: "[data-blazor-initialized]",
  fullPage: true
})
```

#### 4. Authentication Issues
**Problem**: Can't access protected pages
**Solution**:
```javascript
// Get auth cookie from browser DevTools
// Application tab > Cookies > Copy value
mcp_puppeteer.screenshot({
  url: "https://localhost:5652/admin",
  cookies: [{
    name: ".AspNetCore.Identity.Application",
    value: "paste-cookie-value-here",
    domain: "localhost",
    path: "/",
    httpOnly: true,
    secure: true
  }]
})
```

### Debug Commands

```javascript
// Check for JavaScript errors
mcp_browser_tools.screenshot({
  url: "https://localhost:5652/problematic-page",
  includeLogs: true
})

// Monitor network requests
mcp_browser_tools.screenshot({
  url: "https://localhost:5652/slow-page",
  includeNetwork: true
})

// Full diagnostic audit
mcp_browser_tools.audit({
  url: "https://localhost:5652",
  type: "all"
})
```

## Best Practices

### 1. Systematic Testing Approach

1. **Start with baseline**: Always capture current state first
2. **Test incrementally**: Verify each change as you make it
3. **Cover edge cases**: Test error states, empty states, loading states
4. **Document findings**: Save screenshots with descriptive names

### 2. Performance Considerations

- Run performance audits before deploying
- Target Core Web Vitals:
  - LCP (Largest Contentful Paint) < 2.5s
  - FID (First Input Delay) < 100ms
  - CLS (Cumulative Layout Shift) < 0.1

### 3. Accessibility Standards

- Ensure WCAG 2.1 AA compliance
- Test with keyboard navigation
- Verify screen reader compatibility
- Check color contrast ratios (4.5:1 for normal text, 3:1 for large text)

### 4. Mobile-First Testing

- Start testing with mobile viewport
- Progressively enhance for larger screens
- Test touch interactions and gestures
- Verify text remains readable without zooming

### 5. Cross-Browser Considerations

While MCP tools use Chromium, also manually test in:
- Firefox
- Safari (if on macOS)
- Edge (native, not Chromium)

### 6. Documentation Standards

For each UI change, document:
- Before/after screenshots
- Accessibility audit results
- Performance impact
- Any responsive design considerations
- Known limitations or browser-specific issues

## Example Test Report Template

```markdown
# UI Test Report - [Feature Name]

## Test Date: [Date]

### Visual Changes
- Before: [screenshot]
- After: [screenshot]

### Accessibility Results
- Score: [X/100]
- Issues found: [list]
- Fixes applied: [list]

### Performance Impact
- LCP: [before] -> [after]
- FID: [before] -> [after]
- CLS: [before] -> [after]

### Responsive Design
- Mobile: ✅ Tested
- Tablet: ✅ Tested
- Desktop: ✅ Tested

### Browser Compatibility
- Chrome: ✅ Tested
- Firefox: ⚠️ Minor issue with [X]
- Safari: ✅ Tested
- Edge: ✅ Tested

### Notes
[Any additional observations or recommendations]
```

## Conclusion

MCP tools provide powerful capabilities for UI testing during development. By following this guide and integrating these tools into your workflow, you can ensure high-quality, accessible, and performant user interfaces for the WitchCityRope platform.

Remember: The goal is not just to make things look good, but to ensure they work well for all users across all devices and browsers.
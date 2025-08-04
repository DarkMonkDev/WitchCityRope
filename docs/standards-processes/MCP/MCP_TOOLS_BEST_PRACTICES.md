# MCP Tools Best Practices for WitchCityRope

## Overview

This document outlines best practices for using MCP (Model Context Protocol) tools effectively for UI testing and verification in the WitchCityRope project.

## Tool Selection Guide

### Use Puppeteer MCP When:
- Taking screenshots of specific pages or elements
- Automating user interactions (clicks, form fills)
- Testing dynamic content that requires JavaScript
- Simulating user workflows
- Testing authentication flows

### Use Browser-tools MCP When:
- Running accessibility audits
- Checking performance metrics
- Analyzing SEO compliance
- Monitoring console errors
- Checking network requests

### Use Edge Headless Mode When:
- MCP tools aren't available
- Need quick screenshots in WSL environment
- Testing static content

## Effective Usage Patterns

### 1. Screenshot Testing Workflow

```javascript
// Puppeteer MCP - Full page screenshot with authentication
mcp_puppeteer.navigate({ url: "http://localhost:5651/auth/login" })
mcp_puppeteer.screenshot({ 
  fullPage: true, 
  path: "login-page.png" 
})
```

### 2. Comprehensive Page Audit

```javascript
// Browser-tools MCP - Full audit
mcp_browser_tools.runAudit({ 
  url: "http://localhost:5651/events",
  mode: "all"
})
```

### 3. Form Testing

```javascript
// Puppeteer MCP - Test login form
mcp_puppeteer.navigate({ url: "http://localhost:5651/auth/login" })
mcp_puppeteer.fill({ selector: "#email", value: "test@example.com" })
mcp_puppeteer.fill({ selector: "#password", value: "TestPass123!" })
mcp_puppeteer.click({ selector: "button[type='submit']" })
mcp_puppeteer.screenshot({ path: "after-login.png" })
```

## Common Challenges & Solutions

### 1. Blazor Server Dynamic Content

**Challenge**: Content renders after initial page load
**Solution**: Add wait times or check for specific elements

```javascript
// Wait for Blazor to initialize
mcp_puppeteer.navigate({ 
  url: "http://localhost:5651/events",
  waitUntil: "networkidle"
})
```

### 2. Authentication Testing

**Challenge**: OAuth redirects and session management
**Solution**: Use persistent browser contexts

```javascript
// Launch with persistent session
mcp_puppeteer.launch({ 
  headless: false,
  userDataDir: "./test-profile"
})
```

### 3. Responsive Design Testing

**Challenge**: Testing multiple viewports
**Solution**: Use viewport settings

```javascript
// Mobile viewport
mcp_puppeteer.launch({ 
  defaultViewport: { width: 375, height: 667 }
})
```

## Performance Testing Guidelines

### Target Metrics
- First Contentful Paint (FCP): < 1.8s
- Largest Contentful Paint (LCP): < 2.5s
- Time to Interactive (TTI): < 3.8s
- Cumulative Layout Shift (CLS): < 0.1

### Testing Command
```javascript
mcp_browser_tools.runAudit({ 
  url: "http://localhost:5651",
  mode: "performance"
})
```

## Accessibility Testing Standards

### WCAG 2.1 Compliance
- Level AA compliance minimum
- Test with screen readers
- Verify keyboard navigation
- Check color contrast ratios

### Testing Command
```javascript
mcp_browser_tools.runAudit({ 
  url: "http://localhost:5651",
  mode: "accessibility"
})
```

## Integration with Development Workflow

### 1. Pre-Commit Testing
```bash
# Run before committing UI changes
node test-ui-changes.js
```

### 2. Feature Branch Testing
```bash
# Test all pages after feature implementation
npm run test:ui:all
```

### 3. Pull Request Validation
- Include screenshots in PR description
- Run accessibility audit
- Check performance metrics
- Verify responsive design

## Documentation Standards

### Screenshot Naming Convention
```
[page]-[viewport]-[state]-[date].png

Examples:
- home-desktop-default-2025-06-27.png
- events-mobile-filtered-2025-06-27.png
- login-tablet-error-2025-06-27.png
```

### Test Report Format
```markdown
## UI Test Report - [Feature Name]
Date: [YYYY-MM-DD]
Tester: [Name/Claude]

### Screenshots
- [ ] Desktop (1920x1080)
- [ ] Tablet (768x1024)
- [ ] Mobile (375x667)

### Accessibility
- [ ] WCAG AA compliant
- [ ] Keyboard navigable
- [ ] Screen reader compatible

### Performance
- [ ] FCP < 1.8s
- [ ] LCP < 2.5s
- [ ] TTI < 3.8s

### Issues Found
1. [Issue description]
   - Severity: [High/Medium/Low]
   - Steps to reproduce
   - Expected vs actual behavior
```

## Troubleshooting

### MCP Connection Issues
1. Restart Claude Desktop
2. Check claude_desktop_config.json syntax
3. Verify npm packages are accessible
4. Check Windows Defender/firewall settings

### Screenshot Quality Issues
1. Use higher viewport dimensions
2. Add device scale factor for retina displays
3. Wait for fonts and images to load
4. Disable animations for consistent captures

### Performance Testing Variations
1. Test with cache disabled
2. Test with throttled network
3. Test with CPU throttling
4. Test after fresh deployment

## Key Takeaways

1. **Use the right tool for the job** - Puppeteer for interaction, Browser-tools for analysis
2. **Test systematically** - Follow the checklist for consistency
3. **Document findings** - Screenshots and reports for every significant change
4. **Automate when possible** - Create scripts for repetitive tests
5. **Consider the user** - Test with real-world conditions and constraints

## Resources

- [MCP Protocol Documentation](https://modelcontextprotocol.io)
- [Puppeteer Documentation](https://pptr.dev)
- [Web Vitals](https://web.dev/vitals/)
- [WCAG Guidelines](https://www.w3.org/WAI/WCAG21/quickref/)
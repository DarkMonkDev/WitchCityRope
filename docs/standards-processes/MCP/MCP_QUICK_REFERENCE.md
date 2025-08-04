# MCP Visual Verification Quick Reference

## Available MCP Commands

Once the MCP servers are configured, you can use these commands in Claude:

### puppeteer-mcp Commands (Primary Screenshot Tool)

```javascript
// Capture full page screenshot
mcp_puppeteer.screenshot({
  url: "https://localhost:5652",
  fullPage: true
})

// Capture specific element
mcp_puppeteer.screenshot({
  url: "https://localhost:5652/events",
  selector: ".event-list"
})

// Capture with authentication
mcp_puppeteer.screenshot({
  url: "https://localhost:5652/admin",
  cookies: [{
    name: "auth-token",
    value: "your-token-here",
    domain: "localhost"
  }]
})

// Wait for specific element before capturing
mcp_puppeteer.screenshot({
  url: "https://localhost:5652/events",
  waitForSelector: ".event-card",
  fullPage: true
})

// Capture with custom viewport
mcp_puppeteer.screenshot({
  url: "https://localhost:5652",
  viewport: { width: 375, height: 667 },
  fullPage: true
})
```

### browser-tools-mcp Commands (Audits & Debugging)

```javascript
// Screenshot with console logs
mcp_browser_tools.screenshot({
  url: "https://localhost:5652",
  includeLogs: true,
  includeNetwork: true
})

// Run accessibility audit
mcp_browser_tools.audit({
  url: "https://localhost:5652",
  type: "accessibility"
})

// Performance audit
mcp_browser_tools.audit({
  url: "https://localhost:5652/events",
  type: "performance"
})

// SEO audit
mcp_browser_tools.audit({
  url: "https://localhost:5652",
  type: "seo"
})

// Best practices audit
mcp_browser_tools.audit({
  url: "https://localhost:5652",
  type: "best-practices"
})

// All audits at once
mcp_browser_tools.audit({
  url: "https://localhost:5652",
  type: "all"
})
```

## Common Tasks

### 1. Verify Landing Page After CSS Changes

```javascript
// Capture before making changes
mcp_puppeteer.screenshot({
  url: "https://localhost:5652",
  fullPage: true
})

// Make your CSS changes...

// Capture after changes
mcp_puppeteer.screenshot({
  url: "https://localhost:5652",
  fullPage: true
})
```

### 2. Check Mobile Responsiveness

```javascript
// iPhone SE viewport
mcp_browser_tools.screenshot({
  url: "https://localhost:5652/events",
  viewport: { width: 375, height: 667 }
})

// iPad viewport
mcp_browser_tools.screenshot({
  url: "https://localhost:5652/events",
  viewport: { width: 768, height: 1024 }
})
```

### 3. Verify Form Validation

```javascript
// Capture form with errors
mcp_puppeteer.screenshot({
  url: "https://localhost:5652/login",
  selector: "form"
  // After triggering validation
})
```

### 4. Admin Page Verification

```javascript
// First login, then capture
mcp_puppeteer.screenshot({
  url: "https://localhost:5652/admin/events",
  cookies: [/* auth cookies */]
})
```

## Quick Start Commands

### Start Monitoring (PowerShell)
```powershell
# Start UI monitoring with browser
.\tools\Start-UIMonitoring.ps1 -OpenBrowser

# Skip build if already running
.\tools\Start-UIMonitoring.ps1 -SkipBuild

# Custom port
.\tools\Start-UIMonitoring.ps1 -Port 5000
```

### Start Monitoring (Bash)
```bash
# Start application and monitoring
cd /mnt/c/users/chad/source/repos/WitchCityRope
dotnet run --project src/WitchCityRope.Web &
node tools/ui-monitor.js
```

### Run Visual Tests
```bash
# Run all visual tests
cd visual-tests
npx playwright test

# Run specific test
npx playwright test witch-city-rope.spec.js

# Update snapshots
npx playwright test --update-snapshots

# Run in headed mode (see browser)
npx playwright test --headed
```

## Typical Workflow

1. **Start the application**
   ```powershell
   .\tools\Start-UIMonitoring.ps1
   ```

2. **Make code changes** - The monitor will detect changes automatically

3. **Use MCP to capture screenshots** in Claude:
   ```javascript
   mcp_puppeteer.screenshot({
     url: "https://localhost:5652/page-you-changed",
     fullPage: true
   })
   ```

4. **Run accessibility check**:
   ```javascript
   mcp_browser_tools.audit({
     url: "https://localhost:5652/page-you-changed",
     type: "accessibility"
   })
   ```

5. **Check performance impact**:
   ```javascript
   mcp_browser_tools.audit({
     url: "https://localhost:5652/page-you-changed",
     type: "performance"
   })
   ```

## Real-World Testing Examples

### Testing OAuth Integration
```javascript
// Verify Google OAuth button is visible
mcp_puppeteer.screenshot({
  url: "https://localhost:5652/auth/login",
  selector: ".google-oauth-button",
  waitForSelector: ".google-oauth-button"
})

// Check OAuth button across viewports
const viewports = [
  { width: 390, height: 844 },   // Mobile
  { width: 768, height: 1024 },  // Tablet
  { width: 1920, height: 1080 }  // Desktop
];

for (const viewport of viewports) {
  mcp_puppeteer.screenshot({
    url: "https://localhost:5652/auth/login",
    viewport: viewport,
    fullPage: true
  })
}
```

### Testing Dynamic Content (Events)
```javascript
// Wait for event cards to load
mcp_puppeteer.screenshot({
  url: "https://localhost:5652/events",
  waitForSelector: ".event-card",
  fullPage: true
})

// Test filtered view
mcp_puppeteer.screenshot({
  url: "https://localhost:5652/events?type=beginner",
  waitForSelector: ".event-card",
  selector: ".event-list"
})
```

### Testing UI Components
```javascript
// Hero section with animations
mcp_puppeteer.screenshot({
  url: "https://localhost:5652",
  selector: ".hero-section",
  waitForSelector: ".hero-tagline"
})

// Footer with newsletter signup
mcp_puppeteer.screenshot({
  url: "https://localhost:5652",
  selector: "footer",
  fullPage: false
})
```

## Debugging Tips

### View Console Errors
```javascript
mcp_browser_tools.screenshot({
  url: "https://localhost:5652/problematic-page",
  includeLogs: true
})
```

### Check Network Requests
```javascript
mcp_browser_tools.screenshot({
  url: "https://localhost:5652/slow-page",
  includeNetwork: true
})
```

### Capture Specific Error State
```javascript
// After triggering an error
mcp_puppeteer.screenshot({
  url: "https://localhost:5652/page-with-error",
  selector: ".error-message"
})
```

## Integration with Development

### VS Code Tasks
Add to `.vscode/tasks.json`:
```json
{
  "version": "2.0.0",
  "tasks": [
    {
      "label": "Start UI Monitoring",
      "type": "shell",
      "command": "powershell",
      "args": [
        "-File",
        "${workspaceFolder}/tools/Start-UIMonitoring.ps1"
      ],
      "group": "build",
      "presentation": {
        "reveal": "always",
        "panel": "new"
      }
    }
  ]
}
```

### Git Hooks
Add to `.git/hooks/pre-push`:
```bash
#!/bin/bash
cd visual-tests
npm test -- --reporter=dot
```

## Common Issues

### SSL Certificate Errors
- The tools are configured to ignore HTTPS errors for localhost
- If issues persist, regenerate dev certs:
  ```bash
  dotnet dev-certs https --clean
  dotnet dev-certs https --trust
  ```

### Port Already in Use
- The PowerShell script will detect and offer to kill existing processes
- Manual check: `Get-NetTCPConnection -LocalPort 5652`

### MCP Not Available in Claude
1. Restart Claude Desktop after config changes
2. Check config file location is correct
3. Verify Node.js is in PATH

## Best Practices

### 1. **Screenshot Strategy**
- Always capture a baseline before making changes
- Use Puppeteer MCP for visual verification
- Use browser-tools MCP for debugging and audits
- Capture both full page and specific components
- Test critical user flows (login, checkout, etc.)

### 2. **Responsive Testing**
- Test at minimum: Mobile (375px), Tablet (768px), Desktop (1920px)
- Check for text overflow and element overlap
- Verify touch targets are at least 44x44px on mobile
- Test landscape orientation for mobile devices

### 3. **Performance Monitoring**
- Run performance audits before and after major changes
- Watch for: Large DOM size, unused CSS/JS, unoptimized images
- Target metrics: LCP < 2.5s, FID < 100ms, CLS < 0.1
- Use browser-tools includeLogs to catch runtime errors

### 4. **Accessibility Compliance**
- Run accessibility audits on every new feature
- Common issues to check: Color contrast, ARIA labels, keyboard navigation
- Test with screen reader simulation
- Ensure all interactive elements are keyboard accessible

### 5. **Authentication Testing**
- Use cookies parameter for authenticated pages
- Test both logged-in and logged-out states
- Verify proper redirects for protected pages
- Check OAuth flows render properly

### 6. **Documentation**
- Save screenshots with descriptive names (page-feature-date)
- Document visual changes in commit messages
- Note any accessibility or performance issues found
- Create before/after comparisons for UI changes

### 7. **Troubleshooting**
- If MCP tools aren't available, restart Claude Desktop
- For Blazor apps, wait for full page load (networkidle2)
- Use includeLogs to debug JavaScript errors
- Check browser console for SignalR connection issues
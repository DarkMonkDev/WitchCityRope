# UI Testing with MCP Tools - Ubuntu Environment

## Overview

This guide covers UI testing for WitchCityRope using MCP tools in the Ubuntu native environment. Unlike WSL, all browser automation works directly without workarounds.

## Available MCP Tools

### 1. Browser-tools MCP Server
- **Purpose**: Direct browser control, screenshots, performance metrics
- **Best for**: Precise automation, debugging, performance testing
- **Location**: `/home/chad/mcp-servers/browser-tools-server/`

### 2. Stagehand MCP Server  
- **Purpose**: AI-powered natural language browser automation
- **Best for**: User flow testing, exploratory testing, accessibility checks
- **Location**: `/home/chad/mcp-servers/mcp-server-browserbase/stagehand/`

## Quick Start

### 1. Start the Application
```bash
cd /home/chad/repos/witchcityrope/WitchCityRope
~/.dotnet/dotnet run --project src/WitchCityRope.Web
# Application will be available at http://localhost:5000
```

### 2. Start Browser Automation

#### Option A: Browser-tools (Precise Control)
```bash
cd /home/chad/mcp-servers/browser-tools-server
./start-server.sh
# Choose option 2 (Headed + Safe) for visual debugging
```

#### Option B: Stagehand (Natural Language)
```bash
export OPENAI_API_KEY='your-api-key'
/home/chad/mcp-servers/mcp-server-browserbase/stagehand/quickstart.sh
```

## Common Testing Scenarios

### 1. Visual Regression Testing

```javascript
// Capture baseline screenshots with browser-tools
browser_navigate({ url: "http://localhost:5000" })
browser_screenshot({ path: "baseline/homepage.png", fullPage: true })

browser_navigate({ url: "http://localhost:5000/events" })
browser_screenshot({ path: "baseline/events.png", fullPage: true })

browser_navigate({ url: "http://localhost:5000/login" })
browser_screenshot({ path: "baseline/login.png", fullPage: true })

// After changes, capture new screenshots
browser_screenshot({ path: "current/homepage.png", fullPage: true })
// Compare manually or with image diff tools
```

### 2. User Flow Testing with Stagehand

```javascript
// Test complete login flow
stagehand_navigate({ url: "http://localhost:5000" })
stagehand_act({ action: "Click on the Login link in the navigation" })
stagehand_act({ action: "Enter 'admin@witchcityrope.com' in the email field" })
stagehand_act({ action: "Enter 'Test123!' in the password field" })
stagehand_act({ action: "Click the Login button" })

// Verify login success
stagehand_observe({ instruction: "Check if the user dashboard is displayed" })
screenshot({ path: "test-results/login-success.png" })
```

### 3. Responsive Design Testing

```javascript
// Test mobile viewport
browser_setViewport({ width: 375, height: 667 })
browser_navigate({ url: "http://localhost:5000" })
browser_screenshot({ path: "responsive/mobile-home.png" })

// Test tablet viewport
browser_setViewport({ width: 768, height: 1024 })
browser_screenshot({ path: "responsive/tablet-home.png" })

// Test desktop viewport
browser_setViewport({ width: 1920, height: 1080 })
browser_screenshot({ path: "responsive/desktop-home.png" })
```

### 4. Accessibility Testing

```javascript
// Use Stagehand for accessibility checks
stagehand_navigate({ url: "http://localhost:5000/events" })
stagehand_observe({ 
  instruction: "Check if all images have alt text"
})
stagehand_observe({ 
  instruction: "Verify all form inputs have labels"
})
stagehand_observe({ 
  instruction: "Check if the color contrast is sufficient for text"
})

// Capture annotated screenshot
screenshot({ path: "accessibility/events-audit.png" })
```

### 5. Performance Testing

```javascript
// Measure page load performance
browser_navigate({ url: "http://localhost:5000" })
browser_evaluate({ 
  script: `
    const timing = window.performance.timing;
    return {
      domContentLoaded: timing.domContentLoadedEventEnd - timing.navigationStart,
      loadComplete: timing.loadEventEnd - timing.navigationStart,
      firstPaint: performance.getEntriesByType('paint')[0]?.startTime || 0
    };
  `
})

// Check for memory leaks
browser_evaluate({ 
  script: "return performance.memory" 
})
```

### 6. Form Validation Testing

```javascript
// Test invalid inputs
stagehand_navigate({ url: "http://localhost:5000/register" })
stagehand_act({ action: "Click the submit button without filling any fields" })
stagehand_observe({ instruction: "List all validation error messages shown" })
screenshot({ path: "validation/empty-form-errors.png" })

// Test specific validation rules
stagehand_act({ action: "Enter 'invalid-email' in the email field" })
stagehand_act({ action: "Tab to the next field" })
stagehand_observe({ instruction: "Check if email validation error appears" })
```

## Testing Authenticated Areas

### Option 1: Login Through UI
```javascript
// Standard login flow
stagehand_navigate({ url: "http://localhost:5000/login" })
stagehand_act({ action: "Login with admin@witchcityrope.com / Test123!" })
// Continue testing authenticated pages
```

### Option 2: Direct Cookie Setting (Browser-tools)
```javascript
// Set authentication cookie directly
browser_setCookie({
  name: ".AspNetCore.Identity.Application",
  value: "your-auth-cookie-value",
  domain: "localhost",
  path: "/"
})
browser_navigate({ url: "http://localhost:5000/admin" })
```

## Debugging Failed Tests

### 1. Console Logs
```javascript
// Capture console output
browser_navigate({ url: "http://localhost:5000/problematic-page" })
browser_evaluate({ 
  script: "return Array.from(console.logs || [])" 
})
```

### 2. Network Requests
```javascript
// Monitor failed requests
browser_on({ 
  event: "response",
  handler: "console.log('Response:', response.url(), response.status())"
})
browser_navigate({ url: "http://localhost:5000/events" })
```

### 3. Element Inspection
```javascript
// Check if element exists
browser_evaluate({ 
  script: "return document.querySelector('.event-card') !== null" 
})

// Get element properties
browser_evaluate({ 
  script: `
    const elem = document.querySelector('.event-card');
    return {
      exists: elem !== null,
      visible: elem?.offsetParent !== null,
      text: elem?.textContent,
      classes: elem?.className
    };
  `
})
```

## Continuous Integration

### GitHub Actions Example
```yaml
name: UI Tests
on: [push, pull_request]

jobs:
  ui-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'
          
      - name: Start application
        run: |
          cd src/WitchCityRope.Web
          dotnet run &
          sleep 10
          
      - name: Run UI tests
        run: |
          cd /home/chad/repos/witchcityrope
          node test-browser-automation.js
```

## Best Practices

### 1. Test Organization
```
tests/
├── ui/
│   ├── baseline/          # Baseline screenshots
│   ├── current/           # Current test run
│   ├── diff/              # Visual differences
│   └── reports/           # Test reports
```

### 2. Naming Conventions
- Screenshots: `{page}-{viewport}-{date}.png`
- Test results: `{feature}-{scenario}-{result}.png`
- Reports: `{date}-{test-suite}-report.html`

### 3. Test Data Management
```javascript
// Use consistent test data
const testUsers = {
  admin: { email: "admin@witchcityrope.com", password: "Test123!" },
  member: { email: "member@witchcityrope.com", password: "Test123!" },
  teacher: { email: "teacher@witchcityrope.com", password: "Test123!" }
};
```

### 4. Error Handling
```javascript
try {
  browser_navigate({ url: "http://localhost:5000" })
  browser_screenshot({ path: "test.png" })
} catch (error) {
  console.error("Test failed:", error)
  browser_screenshot({ path: "error-state.png" })
}
```

## Troubleshooting

### Application Not Responding
```bash
# Check if app is running
curl http://localhost:5000/health

# Check dotnet processes
ps aux | grep dotnet

# Check logs
journalctl -u witchcityrope -f
```

### Browser Connection Issues
```bash
# Verify Chrome is running
ps aux | grep chrome | grep 9222

# Test DevTools connection
curl http://localhost:9222/json/version

# Restart browser
pkill -f chrome
google-chrome --remote-debugging-port=9222
```

### Screenshot Issues
```bash
# Check permissions
ls -la /home/chad/repos/witchcityrope/test-*.png

# Verify disk space
df -h

# Test with simple screenshot
cd /home/chad/mcp-servers/browser-tools-server
node test-puppeteer-direct.js
```

## Advanced Testing

### Custom Wait Conditions
```javascript
// Wait for specific element
browser_waitForSelector({ selector: ".event-card", timeout: 5000 })

// Wait for custom condition
browser_waitForFunction({ 
  fn: "document.querySelectorAll('.event-card').length > 0",
  timeout: 5000 
})
```

### Simulating User Interactions
```javascript
// Hover effects
browser_hover({ selector: ".nav-link" })
browser_screenshot({ path: "hover-state.png" })

// Keyboard navigation
browser_focus({ selector: "input[name='search']" })
browser_keyboard({ type: "Tab" })
browser_screenshot({ path: "focus-state.png" })
```

### Testing Animations
```javascript
// Disable animations for consistent screenshots
browser_addStyleTag({ 
  content: "*, *::before, *::after { animation-duration: 0s !important; }" 
})
```

## Summary

The Ubuntu environment provides direct, reliable browser automation without the complexity of WSL workarounds. Both MCP servers work seamlessly with the local Chrome installation, enabling comprehensive UI testing capabilities for the WitchCityRope project.
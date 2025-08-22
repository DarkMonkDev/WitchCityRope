# MCP Visual Testing Guide for Dashboard

## Overview

This guide provides step-by-step instructions for using MCP (Model Context Protocol) tools to visually test and verify the user dashboard implementation.

## Prerequisites

1. **MCP Tools Configured** in Claude Desktop:
   - puppeteer
   - browser-tools
   - commands (for PowerShell)

2. **Application Running**:
   ```bash
   # Start API
   cd src/WitchCityRope.Api
   dotnet run
   
   # Start Web (in another terminal)
   cd src/WitchCityRope.Web
   dotnet run
   ```

3. **Test User Credentials**:
   - Admin: admin@witchcityrope.com / Test123!
   - Member: member@witchcityrope.com / Test123!
   - Unvetted: guest@witchcityrope.com / Test123!

## Visual Testing Workflow

### Step 1: Launch Chrome with Debug Port

```javascript
// Using PowerShell MCP
await commands.run({
  command: "powershell",
  args: ["-Command", "Start-Process 'chrome.exe' '-ArgumentList \"--remote-debugging-port=9222\", \"--user-data-dir=C:\\temp\\chrome-debug\"'"]
});
```

### Step 2: Initial Dashboard Load Test

```javascript
// Capture dashboard in loading state
await mcp_puppeteer.screenshot({
  url: "https://localhost:8281/auth/login",
  waitForSelector: "#login-form",
  filename: "1-login-page.png"
});

// Login and capture dashboard
await mcp_browser_tools.screenshot({
  url: "https://localhost:8281/auth/login",
  beforeScreenshot: async (page) => {
    // Fill login form
    await page.fill('input[name="email"]', 'member@witchcityrope.com');
    await page.fill('input[name="password"]', 'Test123!');
    await page.click('button[type="submit"]');
    
    // Wait for dashboard to load
    await page.waitForSelector('.dashboard-container', { timeout: 10000 });
  },
  filename: "2-dashboard-loaded.png"
});
```

### Step 3: Component-Specific Screenshots

```javascript
// Welcome Section
await mcp_browser_tools.screenshot({
  url: "https://localhost:8281/member/dashboard",
  selector: ".dashboard-header",
  padding: 20,
  filename: "3-welcome-section.png"
});

// Upcoming Events
await mcp_browser_tools.screenshot({
  url: "https://localhost:8281/member/dashboard",
  selector: ".upcoming-events",
  filename: "4-upcoming-events.png"
});

// Membership Status Card
await mcp_browser_tools.screenshot({
  url: "https://localhost:8281/member/dashboard",
  selector: ".membership-status",
  filename: "5-membership-status.png"
});

// Quick Links
await mcp_browser_tools.screenshot({
  url: "https://localhost:8281/member/dashboard",
  selector: ".quick-links",
  filename: "6-quick-links.png"
});
```

### Step 4: Interactive State Testing

```javascript
// Hover States
await mcp_browser_tools.screenshot({
  url: "https://localhost:8281/member/dashboard",
  beforeScreenshot: async (page) => {
    await page.hover('.event-card:first-child');
    await page.waitForTimeout(500); // Wait for animation
  },
  filename: "7-event-hover.png"
});

// Dropdown Menus
await mcp_browser_tools.screenshot({
  url: "https://localhost:8281/member/dashboard",
  beforeScreenshot: async (page) => {
    await page.click('.user-menu-toggle');
    await page.waitForSelector('.user-dropdown', { state: 'visible' });
  },
  filename: "8-user-menu-open.png"
});

// Modal/Dialog States
await mcp_browser_tools.screenshot({
  url: "https://localhost:8281/member/dashboard",
  beforeScreenshot: async (page) => {
    await page.click('.view-ticket-btn');
    await page.waitForSelector('.modal', { state: 'visible' });
  },
  filename: "9-ticket-modal.png"
});
```

### Step 5: Responsive Design Testing

```javascript
// Mobile View (iPhone 12)
await mcp_puppeteer.screenshot({
  url: "https://localhost:8281/member/dashboard",
  viewport: { width: 390, height: 844 },
  fullPage: true,
  filename: "10-dashboard-mobile.png"
});

// Tablet View (iPad)
await mcp_puppeteer.screenshot({
  url: "https://localhost:8281/member/dashboard",
  viewport: { width: 820, height: 1180 },
  fullPage: true,
  filename: "11-dashboard-tablet.png"
});

// Desktop View (1920x1080)
await mcp_puppeteer.screenshot({
  url: "https://localhost:8281/member/dashboard",
  viewport: { width: 1920, height: 1080 },
  fullPage: true,
  filename: "12-dashboard-desktop.png"
});

// Ultra-wide (2560x1440)
await mcp_puppeteer.screenshot({
  url: "https://localhost:8281/member/dashboard",
  viewport: { width: 2560, height: 1440 },
  fullPage: true,
  filename: "13-dashboard-ultrawide.png"
});
```

### Step 6: Empty State Testing

```javascript
// No Upcoming Events
await mcp_browser_tools.screenshot({
  url: "https://localhost:8281/member/dashboard",
  beforeScreenshot: async (page) => {
    // Hide event cards to simulate empty state
    await page.evaluate(() => {
      document.querySelectorAll('.event-card').forEach(card => card.remove());
      const container = document.querySelector('.upcoming-events-content');
      container.innerHTML = `
        <div class="empty-state">
          <i class="fas fa-calendar-times fa-3x text-muted"></i>
          <p>No upcoming events</p>
          <a href="/events" class="btn btn-primary">Browse Events</a>
        </div>
      `;
    });
  },
  filename: "14-empty-events.png"
});
```

### Step 7: Error State Testing

```javascript
// API Error Simulation
await mcp_browser_tools.screenshot({
  url: "https://localhost:8281/member/dashboard",
  beforeScreenshot: async (page) => {
    // Intercept API calls and return error
    await page.route('**/api/dashboard/**', route => {
      route.fulfill({
        status: 500,
        body: 'Internal Server Error'
      });
    });
    
    // Reload to trigger error
    await page.reload();
    await page.waitForSelector('.error-message');
  },
  filename: "15-error-state.png"
});
```

### Step 8: Role-Based View Testing

```javascript
// Admin View
await mcp_browser_tools.screenshot({
  url: "https://localhost:8281/auth/login",
  beforeScreenshot: async (page) => {
    // Login as admin
    await page.fill('input[name="email"]', 'admin@witchcityrope.com');
    await page.fill('input[name="password"]', 'Test123!');
    await page.click('button[type="submit"]');
    await page.waitForNavigation();
    await page.waitForSelector('.admin-quick-access');
  },
  fullPage: true,
  filename: "16-dashboard-admin.png"
});

// Unvetted User View
await mcp_browser_tools.screenshot({
  url: "https://localhost:8281/auth/login",
  beforeScreenshot: async (page) => {
    // Login as unvetted user
    await page.fill('input[name="email"]', 'guest@witchcityrope.com');
    await page.fill('input[name="password"]', 'Test123!');
    await page.click('button[type="submit"]');
    await page.waitForNavigation();
    await page.waitForSelector('.vetting-prompt');
  },
  fullPage: true,
  filename: "17-dashboard-unvetted.png"
});
```

### Step 9: Accessibility Testing

```javascript
// Run comprehensive accessibility audit
const accessibilityResults = await mcp_browser_tools.audit({
  url: "https://localhost:8281/member/dashboard",
  type: "accessibility",
  options: {
    includeWarnings: true,
    wcagLevel: "AA"
  }
});

// Check color contrast
await mcp_browser_tools.screenshot({
  url: "https://localhost:8281/member/dashboard",
  beforeScreenshot: async (page) => {
    // Apply color blind simulation
    await page.evaluate(() => {
      const style = document.createElement('style');
      style.textContent = `
        body {
          filter: grayscale(100%);
        }
      `;
      document.head.appendChild(style);
    });
  },
  filename: "18-dashboard-grayscale.png"
});

// Focus indicators
await mcp_browser_tools.screenshot({
  url: "https://localhost:8281/member/dashboard",
  beforeScreenshot: async (page) => {
    // Tab through elements to show focus
    for (let i = 0; i < 5; i++) {
      await page.keyboard.press('Tab');
      await page.waitForTimeout(200);
    }
  },
  filename: "19-focus-indicators.png"
});
```

### Step 10: Performance Testing

```javascript
// Performance audit
const perfResults = await mcp_browser_tools.audit({
  url: "https://localhost:8281/member/dashboard",
  type: "performance",
  options: {
    throttling: "3G",
    clearCache: true
  }
});

// Network waterfall
await mcp_browser_tools.screenshot({
  url: "https://localhost:8281/member/dashboard",
  beforeScreenshot: async (page) => {
    // Open DevTools Network tab
    await page.evaluate(() => {
      console.log('Network requests:', performance.getEntriesByType('resource'));
    });
  },
  filename: "20-network-timing.png"
});
```

## Visual Regression Testing

### Creating Baseline Images

```javascript
// First run - create baselines
const pages = [
  'dashboard-full',
  'dashboard-mobile',
  'upcoming-events',
  'membership-status'
];

for (const page of pages) {
  await mcp_puppeteer.screenshot({
    url: "https://localhost:8281/member/dashboard",
    selector: `.${page}`,
    filename: `baseline/${page}.png`
  });
}
```

### Comparing Against Baseline

```javascript
// After changes - compare
await mcp_browser_tools.compareScreenshots({
  baseline: "baseline/dashboard-full.png",
  current: "current/dashboard-full.png",
  threshold: 0.1, // 10% difference threshold
  highlightDifferences: true,
  outputDiff: "diff/dashboard-full-diff.png"
});
```

## Automated Visual Test Suite

### Complete Test Script

```javascript
// dashboard-visual-test-suite.js
const runCompleteVisualTestSuite = async () => {
  const testResults = [];
  
  // Test configuration
  const viewports = [
    { name: 'mobile', width: 375, height: 667 },
    { name: 'tablet', width: 768, height: 1024 },
    { name: 'desktop', width: 1920, height: 1080 }
  ];
  
  const userTypes = [
    { email: 'admin@witchcityrope.com', role: 'admin' },
    { email: 'member@witchcityrope.com', role: 'member' },
    { email: 'guest@witchcityrope.com', role: 'unvetted' }
  ];
  
  const states = [
    { name: 'default', setup: null },
    { name: 'loading', setup: (page) => page.route('**/api/**', route => route.abort()) },
    { name: 'empty', setup: (page) => page.route('**/api/upcoming-events', route => route.fulfill({ body: '[]' })) }
  ];
  
  // Run tests
  for (const viewport of viewports) {
    for (const user of userTypes) {
      for (const state of states) {
        const testName = `dashboard-${viewport.name}-${user.role}-${state.name}`;
        
        try {
          await mcp_browser_tools.screenshot({
            url: "https://localhost:8281/member/dashboard",
            viewport: viewport,
            beforeScreenshot: async (page) => {
              // Login
              await page.goto('https://localhost:8281/auth/login');
              await page.fill('input[name="email"]', user.email);
              await page.fill('input[name="password"]', 'Test123!');
              await page.click('button[type="submit"]');
              await page.waitForNavigation();
              
              // Apply state
              if (state.setup) {
                await state.setup(page);
              }
              
              await page.waitForTimeout(1000);
            },
            filename: `test-results/${testName}.png`
          });
          
          testResults.push({ test: testName, status: 'PASSED' });
        } catch (error) {
          testResults.push({ test: testName, status: 'FAILED', error: error.message });
        }
      }
    }
  }
  
  // Generate report
  console.table(testResults);
  
  // Save results
  await mcp_browser_tools.evaluate({
    script: `
      const results = ${JSON.stringify(testResults, null, 2)};
      const blob = new Blob([JSON.stringify(results)], { type: 'application/json' });
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = 'visual-test-results.json';
      a.click();
    `
  });
};
```

## CI/CD Integration

### GitHub Actions Visual Testing

```yaml
name: Visual Regression Tests

on:
  pull_request:
    paths:
      - 'src/WitchCityRope.Web/Features/Members/**'
      - 'src/WitchCityRope.Web/wwwroot/css/**'

jobs:
  visual-tests:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup Chrome
      uses: browser-actions/setup-chrome@latest
    
    - name: Start Application
      run: |
        docker-compose up -d
        sleep 30 # Wait for startup
    
    - name: Run Visual Tests
      run: |
        npm install puppeteer
        node scripts/visual-tests/dashboard-tests.js
    
    - name: Upload Screenshots
      uses: actions/upload-artifact@v3
      if: always()
      with:
        name: visual-test-screenshots
        path: |
          test-results/*.png
          diff/*.png
    
    - name: Comment PR with Results
      uses: actions/github-script@v6
      if: failure()
      with:
        script: |
          github.rest.issues.createComment({
            issue_number: context.issue.number,
            owner: context.repo.owner,
            repo: context.repo.repo,
            body: '❌ Visual regression tests failed. Please review the screenshot artifacts.'
          });
```

## Best Practices

### 1. Screenshot Organization

```
/visual-tests/
├── baseline/           # Approved screenshots
├── current/           # Latest test run
├── diff/              # Differences highlighted
└── archive/           # Historical screenshots
    └── 2024-01-15/    # Date-based folders
```

### 2. Test Data Consistency

Always use the same test data for visual tests:
- Fixed user accounts
- Seeded events with consistent dates
- Predictable random data (use seeds)

### 3. Wait Strategies

```javascript
// Wait for specific elements
await page.waitForSelector('.dashboard-loaded');

// Wait for network idle
await page.waitForLoadState('networkidle');

// Wait for animations
await page.waitForTimeout(300);

// Custom wait function
await page.waitForFunction(() => {
  const elements = document.querySelectorAll('.skeleton-loader');
  return elements.length === 0;
});
```

### 4. Debugging Failed Tests

```javascript
// Enable verbose logging
await mcp_browser_tools.screenshot({
  url: "https://localhost:8281/member/dashboard",
  debug: true,
  slowMo: 500, // Slow down actions
  headless: false, // Show browser
  devtools: true, // Open DevTools
  filename: "debug-screenshot.png"
});
```

## Troubleshooting

### Common Issues

1. **"Timeout waiting for selector"**
   - Increase timeout: `{ timeout: 30000 }`
   - Check if element exists in DOM
   - Verify correct selector

2. **"Navigation timeout"**
   - Check if login is working
   - Verify API is running
   - Check for JavaScript errors

3. **"Screenshot differences detected"**
   - Check for dynamic content (dates, times)
   - Verify test data consistency
   - Consider increasing threshold

4. **"Cannot connect to Chrome"**
   - Ensure Chrome is running with debug port
   - Check firewall settings
   - Verify port 9222 is available

### Debug Commands

```javascript
// Get page HTML
const html = await mcp_browser_tools.evaluate({
  url: "https://localhost:8281/member/dashboard",
  script: "document.documentElement.outerHTML"
});

// Check for JavaScript errors
const errors = await mcp_browser_tools.evaluate({
  url: "https://localhost:8281/member/dashboard",
  script: `
    const errors = [];
    window.addEventListener('error', (e) => {
      errors.push(e.message);
    });
    // Wait and return errors
    await new Promise(resolve => setTimeout(resolve, 2000));
    return errors;
  `
});

// Network requests
const requests = await mcp_browser_tools.evaluate({
  url: "https://localhost:8281/member/dashboard",
  script: `
    const requests = [];
    const observer = new PerformanceObserver((list) => {
      for (const entry of list.getEntries()) {
        requests.push({
          name: entry.name,
          duration: entry.duration,
          size: entry.transferSize
        });
      }
    });
    observer.observe({ entryTypes: ['resource'] });
    await new Promise(resolve => setTimeout(resolve, 2000));
    return requests;
  `
});
```

## Visual Test Checklist

Before considering visual tests complete:

- [ ] All viewports tested (mobile, tablet, desktop)
- [ ] All user roles tested (admin, member, unvetted)
- [ ] All states tested (loading, empty, error)
- [ ] Interactive elements tested (hover, focus, active)
- [ ] Accessibility verified (contrast, focus indicators)
- [ ] Performance metrics captured
- [ ] Screenshots organized and labeled
- [ ] Baseline images approved
- [ ] CI/CD pipeline configured
- [ ] Documentation updated
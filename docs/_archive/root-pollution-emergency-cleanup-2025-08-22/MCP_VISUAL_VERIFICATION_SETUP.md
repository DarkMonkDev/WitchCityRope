# MCP Visual Verification Setup for WitchCityRope

This guide sets up automated visual verification using MCP (Model Context Protocol) servers to enable Claude to visually inspect and verify the WitchCityRope UI during development.

## Overview

We'll set up two MCP servers:
1. **puppeteer-mcp** - For capturing full page and element-specific screenshots
2. **browser-tools-mcp** - For comprehensive browser monitoring including console logs and performance audits

## Prerequisites

- Node.js 18+ installed
- Claude Desktop with MCP support
- WitchCityRope application running locally

## Installation Steps

### Step 1: Install puppeteer-mcp

```bash
# Navigate to your tools directory
cd ~/tools
mkdir mcp-servers
cd mcp-servers

# Clone and install puppeteer-mcp
git clone https://github.com/PellePedro/mcp-server-puppeteer.git
cd mcp-server-puppeteer
npm install
npm run build
```

### Step 2: Install browser-tools-mcp

```bash
# Install globally via npm
npm install -g browser-tools-mcp
```

### Step 3: Configure Claude Desktop

Add the following to your Claude Desktop MCP configuration file:

**Windows:** `%APPDATA%\Claude\claude_desktop_config.json`
**macOS:** `~/Library/Application Support/Claude/claude_desktop_config.json`
**Linux:** `~/.config/Claude/claude_desktop_config.json`

```json
{
  "mcpServers": {
    "puppeteer": {
      "command": "node",
      "args": ["C:/Users/chad/tools/mcp-servers/mcp-server-puppeteer/dist/index.js"]
    },
    "browser-tools": {
      "command": "npx",
      "args": ["browser-tools-mcp"]
    },
    "commands": {
      "command": "npx",
      "args": ["@modelcontextprotocol/server-commands"],
      "env": {
        "ALLOWED_COMMANDS": "curl,powershell"
      }
    }
  }
}
```

### Step 4: Verify Installation

Restart Claude Desktop and verify the MCP servers are available by checking for new tools in the tools menu.

## Usage Examples

### Capturing Full Page Screenshots

```typescript
// Example: Capture landing page
mcp_puppeteer.screenshot({
  url: "https://localhost:5652",
  fullPage: true,
  format: "png"
})

// Example: Capture specific element
mcp_puppeteer.screenshot({
  url: "https://localhost:5652/events",
  selector: ".event-list",
  format: "png"
})
```

### Browser Monitoring

```typescript
// Example: Capture with console logs
mcp_browser_tools.screenshot({
  url: "https://localhost:5652",
  includeLogs: true,
  includeNetwork: true
})

// Example: Run accessibility audit
mcp_browser_tools.audit({
  url: "https://localhost:5652",
  type: "accessibility"
})
```

## Automation Scripts

### UI Change Monitor

Create `tools/ui-monitor.js`:

```javascript
const chokidar = require('chokidar');
const { exec } = require('child_process');
const fs = require('fs').promises;
const path = require('path');

class WitchCityRopeUIMonitor {
  constructor() {
    this.baseUrl = 'https://localhost:5652';
    this.screenshotDir = './ui-snapshots';
    this.watchPaths = [
      'src/WitchCityRope.Web/Features',
      'src/WitchCityRope.Web/Pages',
      'src/WitchCityRope.Web/wwwroot/css'
    ];
  }

  async init() {
    await fs.mkdir(this.screenshotDir, { recursive: true });
    this.startWatching();
  }

  startWatching() {
    const watcher = chokidar.watch(this.watchPaths, {
      ignored: /(^|[\/\\])\../,
      persistent: true
    });

    watcher.on('change', async (filepath) => {
      console.log(`File changed: ${filepath}`);
      const page = this.determinePageFromFile(filepath);
      await this.captureSnapshot(page);
    });
  }

  determinePageFromFile(filepath) {
    if (filepath.includes('Auth')) return '/login';
    if (filepath.includes('Events')) return '/events';
    if (filepath.includes('Vetting')) return '/vetting/apply';
    if (filepath.includes('Admin')) return '/admin';
    return '/';
  }

  async captureSnapshot(page) {
    const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
    const filename = `${page.replace(/\//g, '-')}-${timestamp}.png`;
    
    console.log(`Capturing ${this.baseUrl}${page}`);
    
    // This would integrate with MCP tools in Claude
    await this.saveMetadata(filename, page);
  }

  async saveMetadata(filename, page) {
    const metadata = {
      url: `${this.baseUrl}${page}`,
      timestamp: new Date().toISOString(),
      filename: filename
    };
    
    await fs.writeFile(
      path.join(this.screenshotDir, `${filename}.json`),
      JSON.stringify(metadata, null, 2)
    );
  }
}

// Start monitoring
const monitor = new WitchCityRopeUIMonitor();
monitor.init();
```

### Test Scenarios

Create `tools/ui-test-scenarios.js`:

```javascript
const testScenarios = [
  {
    name: "Landing Page Visual Test",
    steps: [
      { url: "/", waitFor: ".hero-section", screenshot: true },
      { url: "/", selector: ".upcoming-events", screenshot: true }
    ]
  },
  {
    name: "Authentication Flow",
    steps: [
      { url: "/login", screenshot: true },
      { url: "/register", screenshot: true },
      { url: "/auth/2fa-setup", screenshot: true }
    ]
  },
  {
    name: "Event Management",
    steps: [
      { url: "/events", screenshot: true },
      { url: "/events/1", screenshot: true },
      { url: "/admin/events", screenshot: true }
    ]
  },
  {
    name: "Member Dashboard",
    steps: [
      { url: "/dashboard", screenshot: true },
      { url: "/my-events", screenshot: true },
      { url: "/profile", screenshot: true }
    ]
  },
  {
    name: "Mobile Responsiveness",
    viewports: [
      { width: 375, height: 667, name: "iPhone SE" },
      { width: 768, height: 1024, name: "iPad" },
      { width: 1920, height: 1080, name: "Desktop" }
    ],
    pages: ["/", "/events", "/dashboard"]
  }
];

module.exports = testScenarios;
```

## GitHub Actions Integration

Create `.github/workflows/visual-regression.yml`:

```yaml
name: Visual Regression Tests

on:
  pull_request:
    paths:
      - 'src/WitchCityRope.Web/**'
      - 'src/WitchCityRope.Web/wwwroot/**'

jobs:
  visual-tests:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '9.0.x'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore
    
    - name: Start application
      run: |
        cd src/WitchCityRope.Web
        dotnet run &
        sleep 30
    
    - name: Setup Node.js
      uses: actions/setup-node@v4
      with:
        node-version: '18'
    
    - name: Install Playwright
      run: |
        npm install -D @playwright/test
        npx playwright install chromium
    
    - name: Run visual tests
      run: |
        npx playwright test visual-tests/
    
    - name: Upload screenshots
      if: always()
      uses: actions/upload-artifact@v4
      with:
        name: visual-test-results
        path: test-results/
```

## Visual Test Suite

Create `visual-tests/witch-city-rope.spec.js`:

```javascript
const { test, expect } = require('@playwright/test');

const pages = [
  { name: 'Landing', url: '/' },
  { name: 'Events List', url: '/events' },
  { name: 'Login', url: '/login' },
  { name: 'Register', url: '/register' },
  { name: 'Dashboard', url: '/dashboard' },
  { name: 'Admin Dashboard', url: '/admin' }
];

test.describe('WitchCityRope Visual Tests', () => {
  pages.forEach(({ name, url }) => {
    test(`${name} page screenshot`, async ({ page }) => {
      await page.goto(`https://localhost:5652${url}`);
      await page.waitForLoadState('networkidle');
      await expect(page).toHaveScreenshot(`${name.toLowerCase().replace(' ', '-')}.png`);
    });
  });

  test('Mobile responsive views', async ({ page }) => {
    const viewports = [
      { width: 375, height: 667, name: 'iphone-se' },
      { width: 768, height: 1024, name: 'ipad' },
      { width: 1920, height: 1080, name: 'desktop' }
    ];

    for (const viewport of viewports) {
      await page.setViewportSize({ width: viewport.width, height: viewport.height });
      await page.goto('https://localhost:5652');
      await expect(page).toHaveScreenshot(`landing-${viewport.name}.png`);
    }
  });
});
```

## PowerShell Helper Scripts

Create `tools/Start-UIMonitoring.ps1`:

```powershell
# Start UI Monitoring for WitchCityRope
param(
    [string]$ProjectPath = "C:\Users\chad\source\repos\WitchCityRope",
    [int]$Port = 5652
)

Write-Host "Starting WitchCityRope UI Monitoring..." -ForegroundColor Green

# Start the application
$webProcess = Start-Process -FilePath "dotnet" -ArgumentList "run --project $ProjectPath\src\WitchCityRope.Web" -PassThru -NoNewWindow

# Wait for application to start
Write-Host "Waiting for application to start on port $Port..."
Start-Sleep -Seconds 10

# Verify application is running
$response = Invoke-WebRequest -Uri "https://localhost:$Port" -SkipCertificateCheck -ErrorAction SilentlyContinue
if ($response.StatusCode -eq 200) {
    Write-Host "Application started successfully!" -ForegroundColor Green
} else {
    Write-Host "Failed to start application" -ForegroundColor Red
    exit 1
}

# Start UI monitor
node "$ProjectPath\tools\ui-monitor.js"
```

## Best Practices

1. **Always run the application with HTTPS** for accurate testing:
   ```bash
   dotnet run --launch-profile https
   ```

2. **Use consistent test data** by seeding the database before visual tests

3. **Capture screenshots at key interaction points**:
   - After page load
   - After user interactions
   - After data changes
   - In different viewport sizes

4. **Store visual regression baselines** in version control

5. **Document visual changes** in pull requests

## Troubleshooting

### SSL Certificate Issues
If you encounter SSL certificate errors:
```bash
dotnet dev-certs https --trust
```

### Port Already in Use
Check and kill existing processes:
```powershell
Get-NetTCPConnection -LocalPort 5652 | Select-Object -Property OwningProcess | Get-Process | Stop-Process -Force
```

### MCP Server Not Responding
1. Check Claude Desktop logs
2. Verify Node.js version compatibility
3. Restart Claude Desktop after configuration changes

## Next Steps

1. Run initial baseline captures of all pages
2. Set up automated visual regression tests in CI/CD
3. Create visual test scenarios for critical user flows
4. Integrate with existing test suite
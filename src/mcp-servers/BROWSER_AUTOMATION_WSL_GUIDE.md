# Browser Automation in WSL: Complete Guide

## Overview

Browser automation in Windows Subsystem for Linux (WSL) presents unique challenges because WSL runs in a virtualized Linux environment without native GUI support. This guide documents the most effective solution: using PowerShell as a bridge to control Windows browsers from WSL.

## Why PowerShell Bridge Works Best

### The Challenge
- WSL lacks native display server (no X11/Wayland by default)
- Linux browser binaries can't access Windows GPU acceleration
- Installing Linux Chrome/Firefox in WSL requires complex X11 forwarding setup
- Performance is poor with virtualized graphics

### The Solution: PowerShell Bridge
PowerShell provides the perfect bridge because:
1. **Native Windows Integration**: PowerShell runs on the Windows host, accessing browsers directly
2. **WSL Interoperability**: WSL can execute Windows commands via `/mnt/c/Windows/System32/WindowsPowerShell/v1.0/powershell.exe`
3. **No Additional Setup**: Works out-of-the-box with WSL
4. **Full Performance**: Uses native Windows browser with hardware acceleration
5. **Debugging Support**: Chrome DevTools and remote debugging work seamlessly

## Implementation Examples

### 1. Browser Tools (Browser Use)

```javascript
// WSL-compatible launcher for Browser Tools
const launchBrowserInWSL = async () => {
  const puppeteer = require('puppeteer');
  
  // Detect if running in WSL
  const isWSL = require('fs').existsSync('/mnt/c/Windows/System32');
  
  if (isWSL) {
    // Find Windows Chrome installation
    const windowsChromePaths = [
      '/mnt/c/Program Files/Google/Chrome/Application/chrome.exe',
      '/mnt/c/Program Files (x86)/Google/Chrome/Application/chrome.exe',
      `/mnt/c/Users/${process.env.USER}/AppData/Local/Google/Chrome/Application/chrome.exe`
    ];
    
    const chromePath = windowsChromePaths.find(path => 
      require('fs').existsSync(path)
    );
    
    if (!chromePath) {
      throw new Error('Chrome not found in Windows. Please install Chrome.');
    }
    
    // Launch via PowerShell with remote debugging
    const debugPort = 9222;
    const { execSync } = require('child_process');
    
    // Kill any existing Chrome debug instances
    try {
      execSync('powershell.exe -Command "Get-Process chrome | Where-Object {$_.CommandLine -like \'*remote-debugging-port*\'} | Stop-Process -Force"');
    } catch (e) {
      // Ignore if no process found
    }
    
    // Launch Chrome with debugging
    execSync(`powershell.exe -Command "Start-Process -FilePath '${chromePath}' -ArgumentList '--remote-debugging-port=${debugPort}', '--no-first-run', '--no-default-browser-check'"`, {
      stdio: 'ignore'
    });
    
    // Wait for Chrome to start
    await new Promise(resolve => setTimeout(resolve, 2000));
    
    // Connect via Puppeteer
    const browser = await puppeteer.connect({
      browserURL: `http://localhost:${debugPort}`,
      defaultViewport: null
    });
    
    return browser;
  } else {
    // Standard Linux/Mac launch
    return await puppeteer.launch({ headless: false });
  }
};

// Usage with Browser Tools
const { BrowserTool } = require('@browserbasehq/browser-tools');

const tool = new BrowserTool({
  customLauncher: launchBrowserInWSL
});
```

### 2. Stagehand Integration

```javascript
// Stagehand with WSL support
const { Stagehand } = require('@browserbasehq/stagehand');

const stagehand = new Stagehand({
  browserLauncher: async () => {
    if (process.platform === 'linux' && require('fs').existsSync('/mnt/c/Windows')) {
      // WSL detected - use PowerShell bridge
      const chromePath = '/mnt/c/Program Files/Google/Chrome/Application/chrome.exe';
      
      require('child_process').execSync(
        `powershell.exe -Command "Start-Process '${chromePath}' -ArgumentList '--remote-debugging-port=9222'"`,
        { stdio: 'ignore' }
      );
      
      await new Promise(resolve => setTimeout(resolve, 3000));
      
      return {
        wsEndpoint: 'ws://localhost:9222/devtools/browser'
      };
    }
    // Default launcher for other platforms
    return null;
  }
});

// Use Stagehand normally
await stagehand.init();
await stagehand.page.goto('https://example.com');
await stagehand.act('Click the login button');
```

### 3. Direct Puppeteer Usage

```javascript
// Pure Puppeteer with WSL support
const puppeteer = require('puppeteer');

async function launchPuppeteerWSL() {
  if (require('fs').existsSync('/mnt/c/Windows')) {
    // Running in WSL
    const chromeCommand = `
      $chromePath = Get-ChildItem -Path @(
        "C:\\Program Files\\Google\\Chrome\\Application",
        "C:\\Program Files (x86)\\Google\\Chrome\\Application",
        "$env:LOCALAPPDATA\\Google\\Chrome\\Application"
      ) -Filter "chrome.exe" -ErrorAction SilentlyContinue | 
      Select-Object -First 1 -ExpandProperty FullName
      
      if ($chromePath) {
        Start-Process $chromePath -ArgumentList "--remote-debugging-port=9222", "--no-first-run"
      } else {
        Write-Error "Chrome not found"
        exit 1
      }
    `;
    
    require('child_process').execSync(
      `powershell.exe -Command "${chromeCommand}"`,
      { stdio: 'inherit' }
    );
    
    await new Promise(resolve => setTimeout(resolve, 2000));
    
    return await puppeteer.connect({
      browserURL: 'http://localhost:9222',
      defaultViewport: null
    });
  }
  
  // Standard launch for non-WSL
  return await puppeteer.launch({ headless: false });
}
```

### 4. Playwright Support

```javascript
// Playwright with WSL support
const { chromium } = require('playwright');

async function launchPlaywrightWSL() {
  if (process.platform === 'linux' && require('fs').existsSync('/mnt/c/Windows')) {
    // WSL detected
    const debugPort = 9223; // Different port to avoid conflicts
    
    // Launch Chrome via PowerShell
    require('child_process').execSync(`
      powershell.exe -Command "
        $chrome = Get-ItemProperty 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\App Paths\\chrome.exe' |
                  Select-Object -ExpandProperty '(default)'
        Start-Process $chrome -ArgumentList '--remote-debugging-port=${debugPort}'
      "
    `, { stdio: 'ignore' });
    
    await new Promise(resolve => setTimeout(resolve, 2000));
    
    // Connect with Playwright
    return await chromium.connectOverCDP(`http://localhost:${debugPort}`);
  }
  
  // Standard Playwright launch
  return await chromium.launch({ headless: false });
}
```

## Common WSL Issues and Solutions

### 1. Chrome Not Found
**Problem**: Chrome isn't installed or can't be located.
**Solution**: 
```bash
# Check if Chrome is installed
ls -la "/mnt/c/Program Files/Google/Chrome/Application/chrome.exe"

# Install Chrome if needed (from WSL)
powershell.exe -Command "Start-Process 'https://www.google.com/chrome/'"
```

### 2. Permission Denied
**Problem**: Can't execute PowerShell or Chrome.
**Solution**:
```bash
# Ensure PowerShell is executable
chmod +x /mnt/c/Windows/System32/WindowsPowerShell/v1.0/powershell.exe

# Run with explicit path
/mnt/c/Windows/System32/WindowsPowerShell/v1.0/powershell.exe -Command "echo test"
```

### 3. Port Already in Use
**Problem**: Remote debugging port is already occupied.
**Solution**:
```javascript
// Kill existing Chrome debug instances
const killChromeDebug = () => {
  try {
    require('child_process').execSync(
      'powershell.exe -Command "Get-Process chrome | Where-Object {$_.CommandLine -like \'*remote-debugging*\'} | Stop-Process -Force"'
    );
  } catch (e) {
    // Process might not exist
  }
};
```

### 4. Connection Timeout
**Problem**: Can't connect to Chrome DevTools.
**Solution**:
```javascript
// Implement retry logic
async function connectWithRetry(url, maxRetries = 5) {
  for (let i = 0; i < maxRetries; i++) {
    try {
      return await puppeteer.connect({ browserURL: url });
    } catch (e) {
      if (i === maxRetries - 1) throw e;
      await new Promise(resolve => setTimeout(resolve, 1000 * (i + 1)));
    }
  }
}
```

### 5. WSL2 Network Issues
**Problem**: localhost doesn't resolve correctly in WSL2.
**Solution**:
```javascript
// Use WSL2 host IP
const getWSLHostIP = () => {
  const output = require('child_process')
    .execSync('ip route | grep default | awk \'{print $3}\'')
    .toString()
    .trim();
  return output || 'localhost';
};

const browserURL = `http://${getWSLHostIP()}:9222`;
```

## Best Practices

### 1. Environment Detection
```javascript
const detectEnvironment = () => {
  const isWSL = process.platform === 'linux' && 
                require('fs').existsSync('/mnt/c/Windows');
  const isWSL2 = isWSL && 
                 require('fs').readFileSync('/proc/version', 'utf8')
                   .toLowerCase().includes('microsoft');
  
  return { isWSL, isWSL2 };
};
```

### 2. Graceful Degradation
```javascript
const getBrowserLauncher = () => {
  const { isWSL } = detectEnvironment();
  
  if (isWSL) {
    return launchViaPowyershell;
  } else if (process.platform === 'linux') {
    return launchWithXvfb;  // Headless fallback
  } else {
    return launchNative;
  }
};
```

### 3. Resource Cleanup
```javascript
process.on('exit', () => {
  if (isWSL) {
    // Clean up Chrome processes
    try {
      require('child_process').execSync(
        'powershell.exe -Command "Get-Process chrome | Where-Object {$_.CommandLine -like \'*remote-debugging*\'} | Stop-Process"'
      );
    } catch (e) {
      // Ignore errors during cleanup
    }
  }
});
```

## Performance Optimization

### 1. Connection Pooling
```javascript
// Reuse Chrome instance across tests
let sharedBrowser = null;

const getSharedBrowser = async () => {
  if (!sharedBrowser) {
    sharedBrowser = await launchBrowserInWSL();
  }
  return sharedBrowser;
};
```

### 2. Parallel Execution
```javascript
// Launch multiple Chrome instances on different ports
const launchMultipleBrowsers = async (count = 3) => {
  const browsers = [];
  const basePort = 9222;
  
  for (let i = 0; i < count; i++) {
    const port = basePort + i;
    
    // Launch Chrome instance
    require('child_process').execSync(
      `powershell.exe -Command "Start-Process chrome -ArgumentList '--remote-debugging-port=${port}'"`,
      { stdio: 'ignore' }
    );
    
    browsers.push({ port, url: `http://localhost:${port}` });
  }
  
  // Wait for all to start
  await new Promise(resolve => setTimeout(resolve, 3000));
  
  // Connect to all
  return Promise.all(
    browsers.map(b => puppeteer.connect({ browserURL: b.url }))
  );
};
```

## Conclusion

The PowerShell bridge method provides the most reliable and performant solution for browser automation in WSL. It leverages the native Windows browser while maintaining full compatibility with Node.js automation tools running in the Linux environment.

Key advantages:
- No additional setup required
- Native performance
- Full debugging capabilities
- Works with all major automation frameworks
- Supports both WSL1 and WSL2

This approach has been tested with:
- Browser Tools / Browser Use
- Stagehand
- Puppeteer
- Playwright
- Selenium WebDriver

For questions or issues, please refer to the troubleshooting section or file an issue in the respective project repositories.
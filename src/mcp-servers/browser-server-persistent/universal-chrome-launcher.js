#!/usr/bin/env node

/**
 * Universal Chrome Launcher - Always launches Chrome in incognito mode
 * This script ensures Chrome is always launched with --incognito flag for privacy
 */

const { spawn } = require('child_process');
const path = require('path');
const fs = require('fs');

// Chrome executable paths
const CHROME_PATHS = {
  windows: [
    'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe',
    'C:\\Program Files (x86)\\Google\\Chrome\\Application\\chrome.exe',
    process.env.LOCALAPPDATA + '\\Google\\Chrome\\Application\\chrome.exe'
  ],
  linux: [
    '/usr/bin/google-chrome',
    '/usr/bin/google-chrome-stable',
    '/usr/bin/chromium',
    '/usr/bin/chromium-browser'
  ],
  darwin: [
    '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome'
  ]
};

// Default Chrome arguments - ALWAYS includes incognito
const DEFAULT_ARGS = [
  '--incognito',  // Always open in incognito mode
  '--remote-debugging-port=9222',
  '--no-first-run',
  '--no-default-browser-check',
  '--disable-background-networking',
  '--disable-background-timer-throttling',
  '--disable-backgrounding-occluded-windows',
  '--disable-breakpad',
  '--disable-client-side-phishing-detection',
  '--disable-component-extensions-with-background-pages',
  '--disable-default-apps',
  '--disable-dev-shm-usage',
  '--disable-extensions',
  '--disable-features=Translate',
  '--disable-hang-monitor',
  '--disable-ipc-flooding-protection',
  '--disable-popup-blocking',
  '--disable-prompt-on-repost',
  '--disable-renderer-backgrounding',
  '--disable-sync',
  '--metrics-recording-only',
  '--no-sandbox',
  '--safebrowsing-disable-auto-update'
];

// Find Chrome executable
function findChrome() {
  const platform = process.platform;
  let paths = [];
  
  if (platform === 'win32') {
    paths = CHROME_PATHS.windows;
  } else if (platform === 'linux') {
    paths = CHROME_PATHS.linux;
  } else if (platform === 'darwin') {
    paths = CHROME_PATHS.darwin;
  }
  
  for (const chromePath of paths) {
    if (chromePath && fs.existsSync(chromePath)) {
      return chromePath;
    }
  }
  
  // If running in WSL, try to use PowerShell to launch Windows Chrome
  if (process.platform === 'linux' && fs.existsSync('/proc/version')) {
    const versionContent = fs.readFileSync('/proc/version', 'utf8');
    if (versionContent.includes('Microsoft') || versionContent.includes('WSL')) {
      return 'powershell.exe';
    }
  }
  
  throw new Error('Chrome not found. Please install Google Chrome.');
}

// Launch Chrome with incognito mode enforced
async function launchChrome(additionalArgs = []) {
  const chromePath = findChrome();
  const args = [...DEFAULT_ARGS];
  
  // Add any additional args, but ensure --incognito is always present
  for (const arg of additionalArgs) {
    if (!arg.startsWith('--incognito') && !args.includes(arg)) {
      args.push(arg);
    }
  }
  
  // If using PowerShell (WSL), wrap the command
  if (chromePath === 'powershell.exe') {
    const windowsChromePath = 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe';
    const powershellArgs = [
      '-Command',
      `& '${windowsChromePath}' ${args.join(' ')}`
    ];
    
    console.log('Launching Chrome in incognito mode via PowerShell...');
    console.log('Command:', chromePath, powershellArgs.join(' '));
    
    const chrome = spawn(chromePath, powershellArgs, {
      detached: true,
      stdio: 'ignore'
    });
    
    chrome.unref();
  } else {
    console.log('Launching Chrome in incognito mode...');
    console.log('Executable:', chromePath);
    console.log('Arguments:', args.join(' '));
    
    const chrome = spawn(chromePath, args, {
      detached: true,
      stdio: 'ignore'
    });
    
    chrome.unref();
  }
  
  // Wait a bit for Chrome to start
  await new Promise(resolve => setTimeout(resolve, 3000));
  
  // Verify Chrome is running with DevTools
  try {
    const response = await fetch('http://localhost:9222/json/version');
    const data = await response.json();
    console.log('Chrome launched successfully:', data.Browser);
    console.log('DevTools URL: http://localhost:9222');
    return true;
  } catch (error) {
    console.error('Failed to connect to Chrome DevTools. Chrome may not have started properly.');
    return false;
  }
}

// Main execution
if (require.main === module) {
  const args = process.argv.slice(2);
  
  launchChrome(args)
    .then(success => {
      if (success) {
        console.log('Chrome is running in incognito mode with remote debugging enabled.');
        console.log('You can now use browser automation tools.');
      } else {
        console.error('Failed to launch Chrome properly.');
        process.exit(1);
      }
    })
    .catch(error => {
      console.error('Error launching Chrome:', error.message);
      process.exit(1);
    });
}

module.exports = { launchChrome, findChrome };
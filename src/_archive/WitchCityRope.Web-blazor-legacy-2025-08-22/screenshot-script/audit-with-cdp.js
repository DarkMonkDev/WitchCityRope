const { exec } = require('child_process');
const fs = require('fs').promises;
const path = require('path');
const net = require('net');

// Configuration
const PAGES_TO_AUDIT = [
  { name: 'Home', url: 'http://localhost:5651/' },
  { name: 'Events', url: 'http://localhost:5651/events' },
  { name: 'Login', url: 'http://localhost:5651/auth/login' }
];

const EDGE_PATH = 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe';
const CHROME_PATH = 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe';

// Function to find an available port
function getAvailablePort() {
  return new Promise((resolve) => {
    const server = net.createServer();
    server.listen(0, () => {
      const port = server.address().port;
      server.close(() => resolve(port));
    });
  });
}

// Function to check if browser exists
async function getBrowserPath() {
  const paths = [EDGE_PATH, CHROME_PATH];
  for (const browserPath of paths) {
    try {
      await fs.access(browserPath.replace(/\\/g, '/'));
      return browserPath;
    } catch (e) {
      continue;
    }
  }
  throw new Error('No supported browser found (Edge or Chrome)');
}

// Function to launch browser with remote debugging
async function launchBrowser(port) {
  const browserPath = await getBrowserPath();
  const userDataDir = path.join(__dirname, 'temp-chrome-profile');
  
  // Create temp directory
  try {
    await fs.mkdir(userDataDir, { recursive: true });
  } catch (e) {}
  
  const args = [
    `--remote-debugging-port=${port}`,
    '--headless',
    '--disable-gpu',
    '--no-sandbox',
    '--disable-dev-shm-usage',
    `--user-data-dir="${userDataDir}"`
  ];
  
  const command = `"${browserPath}" ${args.join(' ')}`;
  
  return new Promise((resolve, reject) => {
    const browser = exec(command, (error) => {
      if (error && !error.killed) {
        reject(error);
      }
    });
    
    // Give browser time to start
    setTimeout(() => resolve(browser), 3000);
  });
}

// Manual audit function with instructions
async function performManualAudit() {
  console.log('\n=== COMPREHENSIVE WEBSITE AUDIT ===\n');
  console.log('Generated:', new Date().toLocaleString());
  console.log('\nSince automated browser control is limited in this environment,');
  console.log('here are detailed manual audit instructions:\n');
  
  const report = {
    generated: new Date().toISOString(),
    pages: []
  };
  
  for (const page of PAGES_TO_AUDIT) {
    console.log(`\n--- ${page.name} Page: ${page.url} ---\n`);
    
    console.log('STEP 1: Open the page in Chrome/Edge');
    console.log(`  URL: ${page.url}`);
    console.log('');
    
    console.log('STEP 2: Open Developer Tools (F12) and check:');
    console.log('');
    
    console.log('  A. CONSOLE TAB - Check for errors:');
    console.log('     - Red error messages (JavaScript errors)');
    console.log('     - Yellow warnings');
    console.log('     - Failed resource loads');
    console.log('     - Mixed content warnings');
    console.log('');
    
    console.log('  B. NETWORK TAB - Check for failed requests:');
    console.log('     - Refresh the page with Network tab open');
    console.log('     - Look for red status codes (404, 500, etc)');
    console.log('     - Check for slow requests (> 1s)');
    console.log('     - Note total page size and load time');
    console.log('');
    
    console.log('  C. LIGHTHOUSE TAB - Run full audit:');
    console.log('     - Click "Analyze page load"');
    console.log('     - Select all categories:');
    console.log('       ✓ Performance');
    console.log('       ✓ Accessibility');
    console.log('       ✓ Best Practices');
    console.log('       ✓ SEO');
    console.log('     - Click "Analyze"');
    console.log('     - Record scores and key issues');
    console.log('');
    
    console.log('  D. ELEMENTS TAB - Manual checks:');
    console.log('     - Right-click images → check for alt attributes');
    console.log('     - Check <html> tag for lang attribute');
    console.log('     - Check <head> for:');
    console.log('       - <title> tag');
    console.log('       - <meta name="description"');
    console.log('       - <meta name="viewport"');
    console.log('     - Search for form inputs and verify labels');
    console.log('');
    
    console.log('STEP 3: Test Interactions:');
    console.log('  - Try keyboard navigation (Tab key)');
    console.log('  - Test all buttons and links');
    console.log('  - Check responsive design (Ctrl+Shift+M)');
    console.log('');
    
    const pageReport = {
      name: page.name,
      url: page.url,
      checks: {
        console: { errors: 0, warnings: 0 },
        network: { failed: 0, slow: 0 },
        lighthouse: {
          performance: 0,
          accessibility: 0,
          bestPractices: 0,
          seo: 0
        },
        manual: {
          hasTitle: false,
          hasLang: false,
          hasViewport: false,
          hasMetaDescription: false,
          imagesWithAlt: 0,
          imagesWithoutAlt: 0,
          formsAccessible: false
        }
      }
    };
    
    report.pages.push(pageReport);
  }
  
  // Save report template
  const reportPath = path.join(__dirname, 'audit-report-template.json');
  await fs.writeFile(reportPath, JSON.stringify(report, null, 2));
  
  console.log('\n=== SEVERITY LEVELS ===\n');
  console.log('CRITICAL (Fix immediately):');
  console.log('  - JavaScript errors in console');
  console.log('  - Failed network requests (404, 500)');
  console.log('  - Accessibility score < 80');
  console.log('  - Missing alt text on images');
  console.log('');
  
  console.log('HIGH (Fix soon):');
  console.log('  - Performance score < 50');
  console.log('  - Form inputs without labels');
  console.log('  - Missing page title or lang attribute');
  console.log('  - Console warnings');
  console.log('');
  
  console.log('MEDIUM (Plan to fix):');
  console.log('  - SEO score < 80');
  console.log('  - Missing meta descriptions');
  console.log('  - Slow network requests (> 1s)');
  console.log('  - Best practices score < 90');
  console.log('');
  
  // Generate markdown report
  let mdReport = '# Comprehensive Website Audit Report\n\n';
  mdReport += `Generated: ${new Date().toLocaleString()}\n\n`;
  mdReport += '## Audit Checklist\n\n';
  
  for (const page of PAGES_TO_AUDIT) {
    mdReport += `### ${page.name} Page (${page.url})\n\n`;
    mdReport += '#### Console Errors\n';
    mdReport += '- [ ] No JavaScript errors\n';
    mdReport += '- [ ] No warnings\n';
    mdReport += '- [ ] No mixed content warnings\n\n';
    
    mdReport += '#### Network\n';
    mdReport += '- [ ] All requests successful (no 4xx or 5xx)\n';
    mdReport += '- [ ] Page loads in < 3 seconds\n';
    mdReport += '- [ ] No slow requests (> 1s)\n\n';
    
    mdReport += '#### Lighthouse Scores\n';
    mdReport += '- [ ] Performance: ___/100\n';
    mdReport += '- [ ] Accessibility: ___/100\n';
    mdReport += '- [ ] Best Practices: ___/100\n';
    mdReport += '- [ ] SEO: ___/100\n\n';
    
    mdReport += '#### Accessibility\n';
    mdReport += '- [ ] All images have alt text\n';
    mdReport += '- [ ] All form inputs have labels\n';
    mdReport += '- [ ] Page has one H1\n';
    mdReport += '- [ ] HTML has lang attribute\n';
    mdReport += '- [ ] Keyboard navigation works\n\n';
    
    mdReport += '#### SEO & Meta\n';
    mdReport += '- [ ] Page has title tag\n';
    mdReport += '- [ ] Page has meta description\n';
    mdReport += '- [ ] Page has viewport meta tag\n\n';
    
    mdReport += '#### Performance Metrics\n';
    mdReport += '- [ ] First Contentful Paint (FCP): ___ms\n';
    mdReport += '- [ ] Largest Contentful Paint (LCP): ___ms\n';
    mdReport += '- [ ] Time to Interactive (TTI): ___ms\n';
    mdReport += '- [ ] Cumulative Layout Shift (CLS): ___\n\n';
    
    mdReport += '---\n\n';
  }
  
  mdReport += '## Issues Found\n\n';
  mdReport += '### Critical Issues\n';
  mdReport += '1. \n\n';
  mdReport += '### High Priority Issues\n';
  mdReport += '1. \n\n';
  mdReport += '### Medium Priority Issues\n';
  mdReport += '1. \n\n';
  
  mdReport += '## Recommendations\n\n';
  mdReport += '1. \n';
  
  const mdReportPath = path.join(__dirname, 'comprehensive-audit-checklist.md');
  await fs.writeFile(mdReportPath, mdReport);
  
  console.log(`\nAudit checklist saved to: ${mdReportPath}`);
  console.log(`Report template saved to: ${reportPath}`);
  console.log('\nPlease complete the manual audit and fill in the checklist.');
}

// Quick automated checks using command line
async function runQuickChecks() {
  console.log('\n=== AUTOMATED QUICK CHECKS ===\n');
  
  for (const page of PAGES_TO_AUDIT) {
    console.log(`Checking ${page.name} page: ${page.url}`);
    
    // Try to fetch the page
    try {
      const response = await fetch(page.url);
      console.log(`  Status: ${response.status} ${response.status === 200 ? '✓' : '✗'}`);
      
      if (response.ok) {
        const html = await response.text();
        
        // Basic checks on HTML
        console.log(`  Has <title>: ${html.includes('<title>') ? '✓' : '✗'}`);
        console.log(`  Has viewport meta: ${html.includes('viewport') ? '✓' : '✗'}`);
        console.log(`  Has lang attribute: ${html.includes('lang=') ? '✓' : '✗'}`);
        console.log(`  Page size: ${(html.length / 1024).toFixed(1)} KB`);
      }
    } catch (error) {
      console.log(`  ERROR: ${error.message}`);
    }
    
    console.log('');
  }
}

// Main function
async function main() {
  console.log('Starting Comprehensive Website Audit...\n');
  
  try {
    // Try quick automated checks first
    await runQuickChecks();
  } catch (error) {
    console.log('Quick checks failed:', error.message);
  }
  
  // Provide manual audit instructions
  await performManualAudit();
  
  console.log('\n=== NEXT STEPS ===\n');
  console.log('1. Open each page in Chrome/Edge');
  console.log('2. Run Lighthouse audits');
  console.log('3. Check console for errors');
  console.log('4. Complete the checklist');
  console.log('5. Document all issues found');
  console.log('\nFor automated testing, consider using:');
  console.log('- Playwright (cross-platform)');
  console.log('- Selenium WebDriver');
  console.log('- Cypress (for E2E testing)');
}

// Run the audit
main().catch(console.error);
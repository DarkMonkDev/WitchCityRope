const puppeteer = require('puppeteer');
const path = require('path');
const fs = require('fs');

// Configuration for visual regression testing
const config = {
  baseUrl: 'http://localhost:5651',
  viewports: {
    desktop: { width: 1920, height: 1080, deviceScaleFactor: 1 },
    tablet: { width: 768, height: 1024, deviceScaleFactor: 2 },
    mobile: { width: 375, height: 812, deviceScaleFactor: 3 }
  },
  pages: [
    { name: 'homepage', path: '/', waitFor: '.hero-section' },
    { name: 'events', path: '/events', waitFor: '.event-list' },
    { name: 'login', path: '/login', waitFor: '.login-form' },
    { name: 'event-detail', path: '/events/1', waitFor: '.event-detail' },
    { name: 'dashboard', path: '/dashboard', waitFor: '.dashboard-content', requiresAuth: true },
    { name: 'profile', path: '/profile', waitFor: '.profile-section', requiresAuth: true },
    { name: 'admin-events', path: '/admin/events', waitFor: '.admin-panel', requiresAuth: true, requiresAdmin: true },
    { name: 'admin-vetting', path: '/admin/vetting', waitFor: '.vetting-queue', requiresAuth: true, requiresAdmin: true }
  ],
  interactiveStates: [
    'default',
    'hover',
    'focus',
    'active'
  ]
};

class VisualRegressionTester {
  constructor() {
    this.browser = null;
    this.results = {
      timestamp: new Date().toISOString(),
      tests: [],
      summary: {
        total: 0,
        passed: 0,
        failed: 0,
        warnings: 0
      }
    };
  }

  async initialize() {
    console.log('🚀 Initializing Visual Regression Test Suite...\n');
    
    // Set up directories
    this.setupDirectories();
    
    // Launch browser
    this.browser = await puppeteer.launch({
      headless: 'new',
      args: [
        '--no-sandbox',
        '--disable-setuid-sandbox',
        '--disable-dev-shm-usage',
        '--disable-gpu',
        '--font-render-hinting=none' // Consistent font rendering
      ]
    });
    
    console.log('✓ Browser launched successfully\n');
  }

  setupDirectories() {
    const dirs = [
      'screenshots/baseline',
      'screenshots/current',
      'screenshots/diff',
      'reports'
    ];
    
    dirs.forEach(dir => {
      const fullPath = path.join(__dirname, dir);
      if (!fs.existsSync(fullPath)) {
        fs.mkdirSync(fullPath, { recursive: true });
      }
    });
  }

  async captureScreenshot(page, name, viewport, state = 'default') {
    const filename = `${name}-${viewport}-${state}.png`;
    const filepath = path.join(__dirname, 'screenshots/current', filename);
    
    // Apply interactive state
    await this.applyInteractiveState(page, state);
    
    // Wait for animations to complete
    await page.waitForTimeout(500);
    
    // Capture screenshot
    await page.screenshot({
      path: filepath,
      fullPage: true
    });
    
    return filepath;
  }

  async applyInteractiveState(page, state) {
    switch(state) {
      case 'hover':
        // Hover over primary buttons and links
        await page.evaluate(() => {
          const elements = document.querySelectorAll('button, a, .interactive');
          if (elements.length > 0) {
            const event = new MouseEvent('mouseenter', {
              view: window,
              bubbles: true,
              cancelable: true
            });
            elements[0].dispatchEvent(event);
          }
        });
        break;
        
      case 'focus':
        // Focus on form inputs
        await page.evaluate(() => {
          const inputs = document.querySelectorAll('input:not([type="hidden"]), textarea, select');
          if (inputs.length > 0) {
            inputs[0].focus();
          }
        });
        break;
        
      case 'active':
        // Simulate active/pressed state
        await page.evaluate(() => {
          const buttons = document.querySelectorAll('button, a.btn');
          if (buttons.length > 0) {
            buttons[0].classList.add('active');
            const event = new MouseEvent('mousedown', {
              view: window,
              bubbles: true,
              cancelable: true
            });
            buttons[0].dispatchEvent(event);
          }
        });
        break;
    }
  }

  async testPage(pageConfig) {
    console.log(`\n📄 Testing: ${pageConfig.name}`);
    console.log(`   URL: ${config.baseUrl}${pageConfig.path}`);
    
    const pageResults = {
      name: pageConfig.name,
      path: pageConfig.path,
      viewports: {}
    };

    for (const [viewportName, viewport] of Object.entries(config.viewports)) {
      console.log(`\n   📱 ${viewportName} (${viewport.width}x${viewport.height})`);
      
      const viewportResults = {
        screenshots: {},
        errors: [],
        warnings: []
      };

      const page = await this.browser.newPage();
      await page.setViewport(viewport);

      try {
        // Navigate to page
        await page.goto(`${config.baseUrl}${pageConfig.path}`, {
          waitUntil: 'networkidle2',
          timeout: 30000
        });

        // Wait for specific element if configured
        if (pageConfig.waitFor) {
          try {
            await page.waitForSelector(pageConfig.waitFor, { timeout: 10000 });
          } catch (e) {
            viewportResults.warnings.push(`Element ${pageConfig.waitFor} not found`);
          }
        }

        // Capture screenshots for each interactive state
        for (const state of config.interactiveStates) {
          if (state !== 'default' && pageConfig.requiresAuth) {
            // Skip interactive states for auth-required pages in this test
            continue;
          }
          
          console.log(`      📸 Capturing ${state} state...`);
          
          try {
            const screenshotPath = await this.captureScreenshot(
              page,
              pageConfig.name,
              viewportName,
              state
            );
            
            viewportResults.screenshots[state] = {
              path: screenshotPath,
              captured: true
            };
            
            console.log(`      ✓ ${state} state captured`);
          } catch (error) {
            console.log(`      ✗ Failed to capture ${state} state: ${error.message}`);
            viewportResults.errors.push(`${state} state capture failed: ${error.message}`);
          }
        }

        // Run visual checks
        await this.runVisualChecks(page, viewportResults);

      } catch (error) {
        console.log(`   ✗ Page test failed: ${error.message}`);
        viewportResults.errors.push(`Navigation failed: ${error.message}`);
      } finally {
        await page.close();
      }

      pageResults.viewports[viewportName] = viewportResults;
    }

    this.results.tests.push(pageResults);
    return pageResults;
  }

  async runVisualChecks(page, results) {
    // Check for visual anomalies
    const checks = await page.evaluate(() => {
      const issues = [];
      
      // Check for overlapping elements
      const elements = document.querySelectorAll('*');
      const rects = new Map();
      
      elements.forEach(el => {
        if (el.offsetParent && window.getComputedStyle(el).visibility !== 'hidden') {
          const rect = el.getBoundingClientRect();
          if (rect.width > 0 && rect.height > 0) {
            // Check for text overflow
            if (el.scrollWidth > el.clientWidth || el.scrollHeight > el.clientHeight) {
              issues.push({
                type: 'overflow',
                element: el.tagName + (el.className ? '.' + el.className : ''),
                details: 'Content overflows container'
              });
            }
          }
        }
      });
      
      // Check for broken images
      const images = document.querySelectorAll('img');
      images.forEach(img => {
        if (!img.complete || img.naturalWidth === 0) {
          issues.push({
            type: 'broken-image',
            element: img.src || 'unknown',
            details: 'Image failed to load'
          });
        }
      });
      
      // Check contrast ratios for text
      const textElements = document.querySelectorAll('p, h1, h2, h3, h4, h5, h6, span, a, button');
      textElements.forEach(el => {
        const styles = window.getComputedStyle(el);
        const fontSize = parseInt(styles.fontSize);
        
        // Flag potentially small text
        if (fontSize < 12) {
          issues.push({
            type: 'small-text',
            element: el.tagName + (el.className ? '.' + el.className : ''),
            details: `Font size too small: ${fontSize}px`
          });
        }
      });
      
      return issues;
    });
    
    if (checks.length > 0) {
      results.warnings = results.warnings.concat(
        checks.map(c => `${c.type}: ${c.element} - ${c.details}`)
      );
    }
  }

  async runAllTests() {
    await this.initialize();
    
    console.log('🎯 Starting Visual Regression Tests\n');
    console.log(`Testing ${config.pages.length} pages across ${Object.keys(config.viewports).length} viewports`);
    console.log(`Capturing ${config.interactiveStates.length} interactive states per page\n`);
    
    // Test each page
    for (const pageConfig of config.pages) {
      if (pageConfig.requiresAuth && !pageConfig.requiresAdmin) {
        console.log(`\n⚠️  Skipping ${pageConfig.name} (requires authentication)`);
        continue;
      }
      
      if (pageConfig.requiresAdmin) {
        console.log(`\n⚠️  Skipping ${pageConfig.name} (requires admin access)`);
        continue;
      }
      
      await this.testPage(pageConfig);
    }
    
    await this.generateReport();
    await this.cleanup();
  }

  async generateReport() {
    console.log('\n\n📊 Generating Visual Regression Report...\n');
    
    // Calculate summary
    this.results.tests.forEach(test => {
      Object.values(test.viewports).forEach(viewport => {
        this.results.summary.total++;
        
        if (viewport.errors.length > 0) {
          this.results.summary.failed++;
        } else if (viewport.warnings.length > 0) {
          this.results.summary.warnings++;
        } else {
          this.results.summary.passed++;
        }
      });
    });
    
    // Generate JSON report
    const jsonPath = path.join(__dirname, 'reports', 'visual-regression-results.json');
    fs.writeFileSync(jsonPath, JSON.stringify(this.results, null, 2));
    console.log(`✓ JSON report saved to: ${jsonPath}`);
    
    // Generate HTML report
    const htmlReport = this.generateHTMLReport();
    const htmlPath = path.join(__dirname, 'reports', 'visual-regression-report.html');
    fs.writeFileSync(htmlPath, htmlReport);
    console.log(`✓ HTML report saved to: ${htmlPath}`);
    
    // Generate markdown summary
    const mdReport = this.generateMarkdownReport();
    const mdPath = path.join(__dirname, 'reports', 'visual-regression-summary.md');
    fs.writeFileSync(mdPath, mdReport);
    console.log(`✓ Markdown summary saved to: ${mdPath}`);
    
    // Print summary
    console.log('\n📈 Test Summary:');
    console.log(`   Total Tests: ${this.results.summary.total}`);
    console.log(`   ✅ Passed: ${this.results.summary.passed}`);
    console.log(`   ⚠️  Warnings: ${this.results.summary.warnings}`);
    console.log(`   ❌ Failed: ${this.results.summary.failed}`);
  }

  generateHTMLReport() {
    const html = `
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Visual Regression Test Report</title>
    <style>
        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            margin: 0;
            padding: 20px;
            background: #f5f5f5;
        }
        .container {
            max-width: 1200px;
            margin: 0 auto;
            background: white;
            padding: 30px;
            border-radius: 8px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }
        h1 {
            color: #333;
            margin-bottom: 10px;
        }
        .timestamp {
            color: #666;
            margin-bottom: 30px;
        }
        .summary {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
            gap: 20px;
            margin-bottom: 40px;
        }
        .stat {
            text-align: center;
            padding: 20px;
            border-radius: 8px;
            background: #f8f9fa;
        }
        .stat.passed { border-left: 4px solid #28a745; }
        .stat.warnings { border-left: 4px solid #ffc107; }
        .stat.failed { border-left: 4px solid #dc3545; }
        .stat .number {
            font-size: 36px;
            font-weight: bold;
            margin-bottom: 5px;
        }
        .page-test {
            margin-bottom: 40px;
            border: 1px solid #dee2e6;
            border-radius: 8px;
            overflow: hidden;
        }
        .page-header {
            background: #f8f9fa;
            padding: 15px 20px;
            font-weight: bold;
        }
        .viewport-results {
            padding: 20px;
            border-bottom: 1px solid #dee2e6;
        }
        .viewport-results:last-child {
            border-bottom: none;
        }
        .viewport-name {
            font-weight: bold;
            margin-bottom: 15px;
            color: #495057;
        }
        .screenshots {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 15px;
            margin-bottom: 20px;
        }
        .screenshot {
            border: 1px solid #dee2e6;
            border-radius: 4px;
            overflow: hidden;
            text-align: center;
        }
        .screenshot img {
            width: 100%;
            height: 150px;
            object-fit: cover;
        }
        .screenshot .label {
            padding: 8px;
            background: #f8f9fa;
            font-size: 14px;
        }
        .issues {
            margin-top: 15px;
        }
        .issue {
            padding: 8px 12px;
            margin-bottom: 5px;
            border-radius: 4px;
            font-size: 14px;
        }
        .error {
            background: #f8d7da;
            color: #721c24;
            border: 1px solid #f5c6cb;
        }
        .warning {
            background: #fff3cd;
            color: #856404;
            border: 1px solid #ffeeba;
        }
    </style>
</head>
<body>
    <div class="container">
        <h1>Visual Regression Test Report</h1>
        <div class="timestamp">Generated: ${new Date(this.results.timestamp).toLocaleString()}</div>
        
        <div class="summary">
            <div class="stat passed">
                <div class="number">${this.results.summary.passed}</div>
                <div>Passed</div>
            </div>
            <div class="stat warnings">
                <div class="number">${this.results.summary.warnings}</div>
                <div>Warnings</div>
            </div>
            <div class="stat failed">
                <div class="number">${this.results.summary.failed}</div>
                <div>Failed</div>
            </div>
        </div>
        
        ${this.results.tests.map(test => `
            <div class="page-test">
                <div class="page-header">${test.name} (${test.path})</div>
                ${Object.entries(test.viewports).map(([viewport, results]) => `
                    <div class="viewport-results">
                        <div class="viewport-name">${viewport}</div>
                        
                        <div class="screenshots">
                            ${Object.entries(results.screenshots).map(([state, screenshot]) => `
                                <div class="screenshot">
                                    <img src="../${screenshot.path.split(path.sep).slice(-3).join('/')}" 
                                         alt="${test.name} - ${viewport} - ${state}">
                                    <div class="label">${state}</div>
                                </div>
                            `).join('')}
                        </div>
                        
                        ${results.errors.length > 0 || results.warnings.length > 0 ? `
                            <div class="issues">
                                ${results.errors.map(error => `
                                    <div class="issue error">❌ ${error}</div>
                                `).join('')}
                                ${results.warnings.map(warning => `
                                    <div class="issue warning">⚠️ ${warning}</div>
                                `).join('')}
                            </div>
                        ` : ''}
                    </div>
                `).join('')}
            </div>
        `).join('')}
    </div>
</body>
</html>
    `;
    
    return html;
  }

  generateMarkdownReport() {
    const report = [];
    
    report.push('# Visual Regression Test Summary');
    report.push(`\nGenerated: ${new Date(this.results.timestamp).toLocaleString()}\n`);
    
    report.push('## Test Results Overview');
    report.push(`- **Total Tests:** ${this.results.summary.total}`);
    report.push(`- **Passed:** ${this.results.summary.passed} ✅`);
    report.push(`- **Warnings:** ${this.results.summary.warnings} ⚠️`);
    report.push(`- **Failed:** ${this.results.summary.failed} ❌\n`);
    
    report.push('## Page-by-Page Results\n');
    
    this.results.tests.forEach(test => {
      report.push(`### ${test.name} (${test.path})\n`);
      
      Object.entries(test.viewports).forEach(([viewport, results]) => {
        report.push(`#### ${viewport}`);
        
        // Screenshots captured
        const screenshotCount = Object.keys(results.screenshots).length;
        report.push(`- Screenshots captured: ${screenshotCount}/${config.interactiveStates.length}`);
        
        // Issues
        if (results.errors.length > 0) {
          report.push('\n**Errors:**');
          results.errors.forEach(error => {
            report.push(`- ❌ ${error}`);
          });
        }
        
        if (results.warnings.length > 0) {
          report.push('\n**Warnings:**');
          results.warnings.forEach(warning => {
            report.push(`- ⚠️ ${warning}`);
          });
        }
        
        if (results.errors.length === 0 && results.warnings.length === 0) {
          report.push('- ✅ All checks passed');
        }
        
        report.push('');
      });
    });
    
    report.push('## Next Steps\n');
    report.push('1. Review screenshots in `screenshots/current/` directory');
    report.push('2. Compare with baseline images (if available)');
    report.push('3. Address any errors or warnings identified');
    report.push('4. Run `npm run visual:approve` to update baseline images');
    
    return report.join('\n');
  }

  async cleanup() {
    if (this.browser) {
      await this.browser.close();
    }
  }
}

// Run tests if called directly
if (require.main === module) {
  const tester = new VisualRegressionTester();
  tester.runAllTests().catch(error => {
    console.error('\n❌ Test suite failed:', error);
    process.exit(1);
  });
}

module.exports = VisualRegressionTester;
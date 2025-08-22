const puppeteer = require('puppeteer');
const fs = require('fs').promises;
const path = require('path');

const PAGES_TO_AUDIT = [
  { name: 'Home', url: 'http://localhost:5651/' },
  { name: 'Events', url: 'http://localhost:5651/events' },
  { name: 'Login', url: 'http://localhost:5651/auth/login' }
];

async function analyzePage(pageInfo) {
  console.log(`\nAnalyzing ${pageInfo.name} page: ${pageInfo.url}`);
  
  const browser = await puppeteer.launch({
    headless: 'new',
    args: ['--no-sandbox', '--disable-setuid-sandbox']
  });
  
  const page = await browser.newPage();
  
  // Set viewport
  await page.setViewport({ width: 1920, height: 1080 });
  
  // Capture console messages and errors
  const consoleMessages = [];
  const networkErrors = [];
  const performanceMetrics = {};
  
  page.on('console', msg => {
    const msgType = msg.type();
    if (msgType === 'error' || msgType === 'warning' || msgType === 'warn') {
      consoleMessages.push({
        type: msgType,
        text: msg.text(),
        args: msg.args().map(arg => arg.toString()),
        location: msg.location()
      });
    }
  });

  page.on('pageerror', error => {
    consoleMessages.push({
      type: 'pageerror',
      text: error.toString(),
      stack: error.stack
    });
  });

  page.on('requestfailed', request => {
    networkErrors.push({
      url: request.url(),
      failure: request.failure(),
      resourceType: request.resourceType()
    });
  });
  
  try {
    // Navigate to page
    const startTime = Date.now();
    const response = await page.goto(pageInfo.url, { 
      waitUntil: 'networkidle2', 
      timeout: 30000 
    });
    const loadTime = Date.now() - startTime;
    
    // Check response status
    const status = response.status();
    
    // Wait a bit to capture any delayed console messages
    await new Promise(resolve => setTimeout(resolve, 3000));
    
    // Get performance metrics
    const perfData = await page.evaluate(() => {
      const perfData = {};
      
      // Navigation timing
      if (window.performance && window.performance.timing) {
        const timing = window.performance.timing;
        perfData.domContentLoaded = timing.domContentLoadedEventEnd - timing.navigationStart;
        perfData.pageLoad = timing.loadEventEnd - timing.navigationStart;
      }
      
      // Paint timing
      if (window.performance && window.performance.getEntriesByType) {
        const paintEntries = window.performance.getEntriesByType('paint');
        paintEntries.forEach(entry => {
          if (entry.name === 'first-contentful-paint') {
            perfData.firstContentfulPaint = Math.round(entry.startTime);
          }
        });
      }
      
      return perfData;
    });
    
    // Basic accessibility checks
    const accessibilityIssues = await page.evaluate(() => {
      const issues = [];
      
      // Check for images without alt text
      const imagesWithoutAlt = document.querySelectorAll('img:not([alt])');
      if (imagesWithoutAlt.length > 0) {
        issues.push({
          type: 'missing-alt-text',
          count: imagesWithoutAlt.length,
          severity: 'HIGH',
          description: `${imagesWithoutAlt.length} images found without alt text`
        });
      }
      
      // Check for missing page title
      if (!document.title || document.title.trim() === '') {
        issues.push({
          type: 'missing-title',
          severity: 'HIGH',
          description: 'Page is missing a title element'
        });
      }
      
      // Check for missing lang attribute
      if (!document.documentElement.lang) {
        issues.push({
          type: 'missing-lang',
          severity: 'HIGH',
          description: 'HTML element is missing lang attribute'
        });
      }
      
      // Check for heading structure
      const headings = document.querySelectorAll('h1, h2, h3, h4, h5, h6');
      const h1Count = document.querySelectorAll('h1').length;
      if (h1Count === 0) {
        issues.push({
          type: 'missing-h1',
          severity: 'MEDIUM',
          description: 'Page is missing an H1 heading'
        });
      } else if (h1Count > 1) {
        issues.push({
          type: 'multiple-h1',
          severity: 'MEDIUM',
          description: `Page has ${h1Count} H1 headings (should have only one)`
        });
      }
      
      // Check for form labels
      const inputsWithoutLabels = document.querySelectorAll('input:not([type="hidden"]):not([type="submit"]):not([aria-label]):not([aria-labelledby])');
      let unlabeledInputs = 0;
      inputsWithoutLabels.forEach(input => {
        const id = input.id;
        if (!id || !document.querySelector(`label[for="${id}"]`)) {
          unlabeledInputs++;
        }
      });
      if (unlabeledInputs > 0) {
        issues.push({
          type: 'missing-form-labels',
          severity: 'HIGH',
          count: unlabeledInputs,
          description: `${unlabeledInputs} form inputs found without associated labels`
        });
      }
      
      // Check for buttons without accessible text
      const buttons = document.querySelectorAll('button');
      let emptyButtons = 0;
      buttons.forEach(button => {
        const text = button.textContent.trim();
        const ariaLabel = button.getAttribute('aria-label');
        if (!text && !ariaLabel) {
          emptyButtons++;
        }
      });
      if (emptyButtons > 0) {
        issues.push({
          type: 'empty-buttons',
          severity: 'HIGH',
          count: emptyButtons,
          description: `${emptyButtons} buttons found without accessible text`
        });
      }
      
      return issues;
    });
    
    // SEO checks
    const seoData = await page.evaluate(() => {
      const issues = [];
      
      // Meta description
      const metaDesc = document.querySelector('meta[name="description"]');
      if (!metaDesc || !metaDesc.content) {
        issues.push({
          type: 'missing-meta-description',
          severity: 'MEDIUM',
          description: 'Page is missing meta description'
        });
      }
      
      // Check for viewport meta tag
      const viewport = document.querySelector('meta[name="viewport"]');
      if (!viewport) {
        issues.push({
          type: 'missing-viewport',
          severity: 'HIGH',
          description: 'Page is missing viewport meta tag (affects mobile responsiveness)'
        });
      }
      
      return issues;
    });
    
    // Check for mixed content
    const hasMixedContent = consoleMessages.some(msg => 
      msg.text && msg.text.toLowerCase().includes('mixed content')
    );
    
    if (hasMixedContent) {
      seoData.push({
        type: 'mixed-content',
        severity: 'HIGH',
        description: 'Page contains mixed content (HTTP resources on HTTPS page)'
      });
    }
    
    await browser.close();
    
    return {
      ...pageInfo,
      status,
      loadTime,
      performanceMetrics: {
        pageLoadTime: `${loadTime}ms`,
        domContentLoaded: perfData.domContentLoaded ? `${perfData.domContentLoaded}ms` : 'N/A',
        firstContentfulPaint: perfData.firstContentfulPaint ? `${perfData.firstContentfulPaint}ms` : 'N/A'
      },
      consoleMessages,
      networkErrors,
      accessibilityIssues,
      seoIssues: seoData,
      timestamp: new Date().toISOString()
    };
    
  } catch (error) {
    await browser.close();
    return {
      ...pageInfo,
      error: error.message,
      timestamp: new Date().toISOString()
    };
  }
}

function getSeverityLevel(issues) {
  if (issues.some(i => i.severity === 'HIGH')) return 'HIGH';
  if (issues.some(i => i.severity === 'MEDIUM')) return 'MEDIUM';
  return 'LOW';
}

async function generateReport(results) {
  let report = '# Comprehensive Website Audit Report\n\n';
  report += `Generated: ${new Date().toLocaleString()}\n\n`;
  
  // Executive Summary
  report += '## Executive Summary\n\n';
  let totalIssues = 0;
  let criticalIssues = 0;
  
  for (const result of results) {
    if (result.error) {
      criticalIssues++;
      continue;
    }
    
    const pageIssues = 
      result.consoleMessages.length +
      result.networkErrors.length +
      result.accessibilityIssues.length +
      result.seoIssues.length;
    
    totalIssues += pageIssues;
    
    // Count critical issues
    if (result.consoleMessages.filter(m => m.type === 'error' || m.type === 'pageerror').length > 0) criticalIssues++;
    if (result.networkErrors.length > 0) criticalIssues++;
    if (result.accessibilityIssues.filter(i => i.severity === 'HIGH').length > 0) criticalIssues++;
  }
  
  report += `- Total Pages Audited: ${results.length}\n`;
  report += `- Total Issues Found: ${totalIssues}\n`;
  report += `- Critical Issues: ${criticalIssues}\n\n`;
  
  // Issue Summary by Type
  report += '### Issues by Type\n\n';
  
  let totalConsoleErrors = 0;
  let totalNetworkErrors = 0;
  let totalAccessibilityIssues = 0;
  let totalSeoIssues = 0;
  
  for (const result of results) {
    if (!result.error) {
      totalConsoleErrors += result.consoleMessages.length;
      totalNetworkErrors += result.networkErrors.length;
      totalAccessibilityIssues += result.accessibilityIssues.length;
      totalSeoIssues += result.seoIssues.length;
    }
  }
  
  report += `- Console Errors/Warnings: ${totalConsoleErrors}\n`;
  report += `- Failed Network Requests: ${totalNetworkErrors}\n`;
  report += `- Accessibility Issues: ${totalAccessibilityIssues}\n`;
  report += `- SEO Issues: ${totalSeoIssues}\n\n`;
  
  // Page-by-page results
  report += '## Detailed Results by Page\n\n';
  
  for (const result of results) {
    report += `### ${result.name} Page\n`;
    report += `URL: ${result.url}\n\n`;
    
    if (result.error) {
      report += `#### ❌ ERROR: Failed to analyze page\n`;
      report += `**Severity: CRITICAL**\n\n`;
      report += `Error: ${result.error}\n\n`;
      report += '---\n\n';
      continue;
    }
    
    report += `#### Page Status: ${result.status === 200 ? '✅' : '❌'} ${result.status}\n\n`;
    
    // Performance Metrics
    report += '#### Performance Metrics\n';
    report += `- Page Load Time: ${result.performanceMetrics.pageLoadTime}\n`;
    report += `- DOM Content Loaded: ${result.performanceMetrics.domContentLoaded}\n`;
    report += `- First Contentful Paint: ${result.performanceMetrics.firstContentfulPaint}\n\n`;
    
    // Console Errors
    if (result.consoleMessages.length > 0) {
      report += '#### Console Messages\n';
      const errors = result.consoleMessages.filter(m => m.type === 'error' || m.type === 'pageerror');
      const warnings = result.consoleMessages.filter(m => m.type === 'warning' || m.type === 'warn');
      
      report += `**Severity: ${errors.length > 0 ? 'HIGH' : 'MEDIUM'}**\n\n`;
      
      if (errors.length > 0) {
        report += '**Errors:**\n';
        for (const msg of errors) {
          report += `- ${msg.text}\n`;
          if (msg.location && msg.location.url) {
            report += `  Location: ${msg.location.url}:${msg.location.lineNumber}\n`;
          }
        }
        report += '\n';
      }
      
      if (warnings.length > 0) {
        report += '**Warnings:**\n';
        for (const msg of warnings) {
          report += `- ${msg.text}\n`;
        }
        report += '\n';
      }
    }
    
    // Network Errors
    if (result.networkErrors.length > 0) {
      report += '#### Failed Network Requests\n';
      report += '**Severity: HIGH**\n\n';
      
      for (const error of result.networkErrors) {
        report += `- **${error.resourceType}**: ${error.url}\n`;
        if (error.failure) {
          report += `  Failure: ${error.failure.errorText}\n`;
        }
      }
      report += '\n';
    }
    
    // Accessibility Issues
    if (result.accessibilityIssues.length > 0) {
      report += '#### Accessibility Issues\n';
      report += `**Severity: ${getSeverityLevel(result.accessibilityIssues)}**\n\n`;
      
      for (const issue of result.accessibilityIssues) {
        report += `- **${issue.type}** (${issue.severity}): ${issue.description}\n`;
      }
      report += '\n';
    }
    
    // SEO Issues
    if (result.seoIssues.length > 0) {
      report += '#### SEO Issues\n';
      report += `**Severity: ${getSeverityLevel(result.seoIssues)}**\n\n`;
      
      for (const issue of result.seoIssues) {
        report += `- **${issue.type}** (${issue.severity}): ${issue.description}\n`;
      }
      report += '\n';
    }
    
    report += '---\n\n';
  }
  
  // Recommendations
  report += '## Recommendations by Priority\n\n';
  
  report += '### 🔴 Critical (Address Immediately)\n\n';
  
  let hasCritical = false;
  if (results.some(r => r.error)) {
    report += '1. **Fix page loading errors** - Some pages are completely inaccessible\n';
    hasCritical = true;
  }
  if (totalConsoleErrors > 0) {
    report += '2. **Fix JavaScript errors** - Console errors indicate broken functionality\n';
    hasCritical = true;
  }
  if (totalNetworkErrors > 0) {
    report += '3. **Fix failed network requests** - Missing resources affect user experience\n';
    hasCritical = true;
  }
  if (results.some(r => r.accessibilityIssues && r.accessibilityIssues.some(i => i.severity === 'HIGH'))) {
    report += '4. **Fix high-severity accessibility issues** - These prevent users with disabilities from using the site\n';
    hasCritical = true;
  }
  
  if (!hasCritical) {
    report += 'No critical issues found!\n';
  }
  
  report += '\n### 🟡 High Priority\n\n';
  report += '1. **Add missing alt text to images** - Improves accessibility and SEO\n';
  report += '2. **Ensure all form inputs have labels** - Critical for screen readers\n';
  report += '3. **Fix heading hierarchy** - Use one H1 per page and maintain proper nesting\n';
  report += '4. **Add viewport meta tag** - Essential for mobile responsiveness\n\n';
  
  report += '### 🟢 Medium Priority\n\n';
  report += '1. **Add meta descriptions** - Improves SEO and click-through rates\n';
  report += '2. **Optimize page load times** - Aim for under 3 seconds\n';
  report += '3. **Ensure buttons have accessible text** - Use aria-label for icon-only buttons\n';
  report += '4. **Add lang attribute to HTML** - Helps screen readers pronounce content correctly\n\n';
  
  // Summary of all issues
  report += '## Detailed Issue List\n\n';
  
  for (const result of results) {
    if (result.error || 
        (result.consoleMessages.length === 0 && 
         result.networkErrors.length === 0 && 
         result.accessibilityIssues.length === 0 && 
         result.seoIssues.length === 0)) {
      continue;
    }
    
    report += `### ${result.name} Page Issues\n\n`;
    
    let issueCount = 1;
    
    // List all issues with numbering
    if (result.consoleMessages.length > 0) {
      for (const msg of result.consoleMessages) {
        report += `${issueCount}. **Console ${msg.type}**: ${msg.text}\n`;
        issueCount++;
      }
    }
    
    if (result.networkErrors.length > 0) {
      for (const error of result.networkErrors) {
        report += `${issueCount}. **Network Error**: ${error.resourceType} - ${error.url}\n`;
        issueCount++;
      }
    }
    
    if (result.accessibilityIssues.length > 0) {
      for (const issue of result.accessibilityIssues) {
        report += `${issueCount}. **Accessibility**: ${issue.description}\n`;
        issueCount++;
      }
    }
    
    if (result.seoIssues.length > 0) {
      for (const issue of result.seoIssues) {
        report += `${issueCount}. **SEO**: ${issue.description}\n`;
        issueCount++;
      }
    }
    
    report += '\n';
  }
  
  return report;
}

async function main() {
  console.log('Starting comprehensive website audit...');
  console.log('This will analyze accessibility, performance, SEO, and monitor for errors.\n');
  
  const results = [];
  
  for (const pageInfo of PAGES_TO_AUDIT) {
    try {
      const result = await analyzePage(pageInfo);
      results.push(result);
    } catch (error) {
      console.error(`Failed to analyze ${pageInfo.name}:`, error.message);
      results.push({
        ...pageInfo,
        error: error.message,
        timestamp: new Date().toISOString()
      });
    }
  }
  
  // Generate report
  const report = await generateReport(results);
  
  // Save report
  const reportPath = path.join(__dirname, 'comprehensive-audit-report.md');
  await fs.writeFile(reportPath, report);
  console.log(`\nAudit complete! Report saved to: ${reportPath}`);
  
  // Also save raw JSON data
  const jsonPath = path.join(__dirname, 'audit-results.json');
  await fs.writeFile(jsonPath, JSON.stringify(results, null, 2));
  console.log(`Raw data saved to: ${jsonPath}`);
  
  // Print summary
  console.log('\n=== AUDIT SUMMARY ===');
  console.log(`Pages audited: ${results.length}`);
  
  let totalIssues = 0;
  for (const result of results) {
    if (!result.error) {
      const issues = result.consoleMessages.length + result.networkErrors.length + 
                    result.accessibilityIssues.length + result.seoIssues.length;
      totalIssues += issues;
      console.log(`- ${result.name}: ${issues} issues found`);
    } else {
      console.log(`- ${result.name}: ERROR - ${result.error}`);
    }
  }
  
  console.log(`\nTotal issues found: ${totalIssues}`);
  console.log('\nCheck the report for detailed findings and recommendations.');
}

main().catch(console.error);
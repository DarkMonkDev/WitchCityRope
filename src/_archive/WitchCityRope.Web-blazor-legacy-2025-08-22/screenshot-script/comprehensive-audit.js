const puppeteer = require('puppeteer');
const lighthouse = require('lighthouse');
const chromeLauncher = require('chrome-launcher');
const fs = require('fs').promises;
const path = require('path');

const PAGES_TO_AUDIT = [
  { name: 'Home', url: 'http://localhost:5651/' },
  { name: 'Events', url: 'http://localhost:5651/events' },
  { name: 'Login', url: 'http://localhost:5651/auth/login' }
];

const LIGHTHOUSE_CONFIG = {
  logLevel: 'error',
  output: 'json',
  onlyCategories: ['performance', 'accessibility', 'best-practices', 'seo'],
  formFactor: 'desktop',
  throttling: {
    rttMs: 40,
    throughputKbps: 10 * 1024,
    cpuSlowdownMultiplier: 1
  }
};

async function captureConsoleErrors(page) {
  const consoleMessages = [];
  const networkErrors = [];
  
  page.on('console', msg => {
    if (msg.type() === 'error' || msg.type() === 'warning') {
      consoleMessages.push({
        type: msg.type(),
        text: msg.text(),
        location: msg.location()
      });
    }
  });

  page.on('pageerror', error => {
    consoleMessages.push({
      type: 'pageerror',
      text: error.toString()
    });
  });

  page.on('requestfailed', request => {
    networkErrors.push({
      url: request.url(),
      failure: request.failure()
    });
  });

  return { consoleMessages, networkErrors };
}

async function runLighthouseAudit(url, chrome) {
  const options = {
    ...LIGHTHOUSE_CONFIG,
    port: chrome.port
  };

  const runnerResult = await lighthouse(url, options);
  return runnerResult.lhr;
}

async function analyzePage(pageInfo, browser, chrome) {
  console.log(`\nAnalyzing ${pageInfo.name} page: ${pageInfo.url}`);
  
  const page = await browser.newPage();
  const { consoleMessages, networkErrors } = captureConsoleErrors(page);
  
  try {
    // Navigate to page and wait for it to load
    await page.goto(pageInfo.url, { waitUntil: 'networkidle2', timeout: 30000 });
    
    // Wait a bit to capture any delayed console messages
    await new Promise(resolve => setTimeout(resolve, 2000));
    
    // Run Lighthouse audit
    const lighthouseResults = await runLighthouseAudit(pageInfo.url, chrome);
    
    // Extract key metrics
    const metrics = {
      scores: {
        performance: Math.round(lighthouseResults.categories.performance.score * 100),
        accessibility: Math.round(lighthouseResults.categories.accessibility.score * 100),
        bestPractices: Math.round(lighthouseResults.categories['best-practices'].score * 100),
        seo: Math.round(lighthouseResults.categories.seo.score * 100)
      },
      performanceMetrics: {
        FCP: lighthouseResults.audits['first-contentful-paint'].displayValue,
        LCP: lighthouseResults.audits['largest-contentful-paint'].displayValue,
        TTI: lighthouseResults.audits['interactive'].displayValue,
        CLS: lighthouseResults.audits['cumulative-layout-shift'].displayValue,
        TBT: lighthouseResults.audits['total-blocking-time'].displayValue
      },
      issues: {
        accessibility: [],
        performance: [],
        bestPractices: [],
        seo: []
      }
    };

    // Extract accessibility violations
    if (lighthouseResults.categories.accessibility.auditRefs) {
      for (const auditRef of lighthouseResults.categories.accessibility.auditRefs) {
        const audit = lighthouseResults.audits[auditRef.id];
        if (audit && audit.score !== null && audit.score < 1) {
          metrics.issues.accessibility.push({
            id: audit.id,
            title: audit.title,
            description: audit.description,
            score: audit.score,
            details: audit.details
          });
        }
      }
    }

    // Extract performance issues
    const performanceAudits = ['first-contentful-paint', 'largest-contentful-paint', 
                              'total-blocking-time', 'cumulative-layout-shift', 'speed-index'];
    for (const auditId of performanceAudits) {
      const audit = lighthouseResults.audits[auditId];
      if (audit && audit.score !== null && audit.score < 0.9) {
        metrics.issues.performance.push({
          id: audit.id,
          title: audit.title,
          value: audit.displayValue,
          score: audit.score
        });
      }
    }

    // Extract best practices issues
    if (lighthouseResults.categories['best-practices'].auditRefs) {
      for (const auditRef of lighthouseResults.categories['best-practices'].auditRefs) {
        const audit = lighthouseResults.audits[auditRef.id];
        if (audit && audit.score !== null && audit.score < 1) {
          metrics.issues.bestPractices.push({
            id: audit.id,
            title: audit.title,
            description: audit.description,
            score: audit.score
          });
        }
      }
    }

    // Extract SEO issues
    if (lighthouseResults.categories.seo.auditRefs) {
      for (const auditRef of lighthouseResults.categories.seo.auditRefs) {
        const audit = lighthouseResults.audits[auditRef.id];
        if (audit && audit.score !== null && audit.score < 1) {
          metrics.issues.seo.push({
            id: audit.id,
            title: audit.title,
            description: audit.description,
            score: audit.score
          });
        }
      }
    }

    await page.close();
    
    return {
      ...pageInfo,
      metrics,
      consoleMessages,
      networkErrors,
      timestamp: new Date().toISOString()
    };
    
  } catch (error) {
    await page.close();
    return {
      ...pageInfo,
      error: error.message,
      consoleMessages,
      networkErrors,
      timestamp: new Date().toISOString()
    };
  }
}

async function generateReport(results) {
  let report = '# Comprehensive Website Audit Report\n\n';
  report += `Generated: ${new Date().toLocaleString()}\n\n`;
  
  // Executive Summary
  report += '## Executive Summary\n\n';
  let totalIssues = 0;
  let criticalIssues = 0;
  
  for (const result of results) {
    if (result.error) continue;
    
    const pageIssues = 
      result.metrics.issues.accessibility.length +
      result.metrics.issues.performance.length +
      result.metrics.issues.bestPractices.length +
      result.metrics.issues.seo.length +
      result.consoleMessages.length +
      result.networkErrors.length;
    
    totalIssues += pageIssues;
    
    // Count critical issues (accessibility < 80, performance < 50, console errors)
    if (result.metrics.scores.accessibility < 80) criticalIssues++;
    if (result.metrics.scores.performance < 50) criticalIssues++;
    if (result.consoleMessages.filter(m => m.type === 'error').length > 0) criticalIssues++;
  }
  
  report += `- Total Pages Audited: ${results.length}\n`;
  report += `- Total Issues Found: ${totalIssues}\n`;
  report += `- Critical Issues: ${criticalIssues}\n\n`;
  
  // Page-by-page results
  for (const result of results) {
    report += `## ${result.name} Page\n`;
    report += `URL: ${result.url}\n\n`;
    
    if (result.error) {
      report += `### ERROR: Failed to analyze page\n`;
      report += `Error: ${result.error}\n\n`;
      continue;
    }
    
    // Scores
    report += '### Audit Scores\n';
    report += `- **Performance**: ${result.metrics.scores.performance}/100 ${getScoreEmoji(result.metrics.scores.performance)}\n`;
    report += `- **Accessibility**: ${result.metrics.scores.accessibility}/100 ${getScoreEmoji(result.metrics.scores.accessibility)}\n`;
    report += `- **Best Practices**: ${result.metrics.scores.bestPractices}/100 ${getScoreEmoji(result.metrics.scores.bestPractices)}\n`;
    report += `- **SEO**: ${result.metrics.scores.seo}/100 ${getScoreEmoji(result.metrics.scores.seo)}\n\n`;
    
    // Performance Metrics
    report += '### Performance Metrics\n';
    report += `- First Contentful Paint (FCP): ${result.metrics.performanceMetrics.FCP}\n`;
    report += `- Largest Contentful Paint (LCP): ${result.metrics.performanceMetrics.LCP}\n`;
    report += `- Time to Interactive (TTI): ${result.metrics.performanceMetrics.TTI}\n`;
    report += `- Cumulative Layout Shift (CLS): ${result.metrics.performanceMetrics.CLS}\n`;
    report += `- Total Blocking Time (TBT): ${result.metrics.performanceMetrics.TBT}\n\n`;
    
    // Console Errors
    if (result.consoleMessages.length > 0) {
      report += '### Console Messages\n';
      report += `**Severity: ${result.consoleMessages.filter(m => m.type === 'error').length > 0 ? 'HIGH' : 'MEDIUM'}**\n\n`;
      
      for (const msg of result.consoleMessages) {
        report += `- **${msg.type.toUpperCase()}**: ${msg.text}\n`;
        if (msg.location && msg.location.url) {
          report += `  Location: ${msg.location.url}:${msg.location.lineNumber}\n`;
        }
      }
      report += '\n';
    }
    
    // Network Errors
    if (result.networkErrors.length > 0) {
      report += '### Failed Network Requests\n';
      report += '**Severity: HIGH**\n\n';
      
      for (const error of result.networkErrors) {
        report += `- ${error.url}\n`;
        report += `  Failure: ${error.failure ? error.failure.errorText : 'Unknown'}\n`;
      }
      report += '\n';
    }
    
    // Accessibility Issues
    if (result.metrics.issues.accessibility.length > 0) {
      report += '### Accessibility Issues\n';
      report += '**Severity: HIGH**\n\n';
      
      for (const issue of result.metrics.issues.accessibility) {
        report += `- **${issue.title}**\n`;
        report += `  ${issue.description}\n`;
        report += `  Score: ${Math.round(issue.score * 100)}/100\n`;
      }
      report += '\n';
    }
    
    // Performance Issues
    if (result.metrics.issues.performance.length > 0) {
      report += '### Performance Issues\n';
      report += '**Severity: MEDIUM**\n\n';
      
      for (const issue of result.metrics.issues.performance) {
        report += `- **${issue.title}**: ${issue.value}\n`;
        report += `  Score: ${Math.round(issue.score * 100)}/100\n`;
      }
      report += '\n';
    }
    
    // Best Practices Issues
    if (result.metrics.issues.bestPractices.length > 0) {
      report += '### Best Practices Issues\n';
      report += '**Severity: MEDIUM**\n\n';
      
      for (const issue of result.metrics.issues.bestPractices) {
        report += `- **${issue.title}**\n`;
        report += `  ${issue.description}\n`;
      }
      report += '\n';
    }
    
    // SEO Issues
    if (result.metrics.issues.seo.length > 0) {
      report += '### SEO Issues\n';
      report += '**Severity: LOW**\n\n';
      
      for (const issue of result.metrics.issues.seo) {
        report += `- **${issue.title}**\n`;
        report += `  ${issue.description}\n`;
      }
      report += '\n';
    }
    
    report += '---\n\n';
  }
  
  // Recommendations
  report += '## Recommendations by Priority\n\n';
  report += '### Critical (Address Immediately)\n';
  report += '1. Fix all JavaScript errors in console\n';
  report += '2. Resolve all failed network requests\n';
  report += '3. Address accessibility violations scoring below 80\n\n';
  
  report += '### High Priority\n';
  report += '1. Improve performance metrics (FCP, LCP, TTI)\n';
  report += '2. Fix remaining accessibility issues\n';
  report += '3. Implement best practices violations\n\n';
  
  report += '### Medium Priority\n';
  report += '1. Optimize images and assets\n';
  report += '2. Improve SEO metadata\n';
  report += '3. Reduce JavaScript bundle size\n\n';
  
  return report;
}

function getScoreEmoji(score) {
  if (score >= 90) return '✅';
  if (score >= 50) return '⚠️';
  return '❌';
}

async function main() {
  console.log('Starting comprehensive website audit...');
  
  let browser;
  let chrome;
  
  try {
    // Launch Chrome with debugging port for Lighthouse
    chrome = await chromeLauncher.launch({
      chromeFlags: ['--headless', '--disable-gpu', '--no-sandbox']
    });
    
    // Connect Puppeteer to the same instance
    browser = await puppeteer.connect({
      browserURL: `http://localhost:${chrome.port}`,
      defaultViewport: { width: 1920, height: 1080 }
    });
    
    const results = [];
    
    for (const pageInfo of PAGES_TO_AUDIT) {
      const result = await analyzePage(pageInfo, browser, chrome);
      results.push(result);
    }
    
    // Generate report
    const report = await generateReport(results);
    
    // Save report
    const reportPath = path.join(__dirname, 'comprehensive-audit-report.md');
    await fs.writeFile(reportPath, report);
    console.log(`\nReport saved to: ${reportPath}`);
    
    // Also save raw JSON data
    const jsonPath = path.join(__dirname, 'audit-results.json');
    await fs.writeFile(jsonPath, JSON.stringify(results, null, 2));
    console.log(`Raw data saved to: ${jsonPath}`);
    
  } catch (error) {
    console.error('Audit failed:', error);
  } finally {
    if (browser) await browser.close();
    if (chrome) await chrome.kill();
  }
}

main().catch(console.error);
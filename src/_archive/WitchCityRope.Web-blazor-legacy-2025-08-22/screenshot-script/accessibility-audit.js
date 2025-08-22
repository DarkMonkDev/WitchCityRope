const puppeteer = require('puppeteer');
const lighthouse = require('lighthouse');
const chromeLauncher = require('chrome-launcher');
const fs = require('fs').promises;
const path = require('path');

// Configuration for accessibility testing
const PAGES_TO_AUDIT = [
  { name: 'Home', url: 'http://localhost:5651/', auth: false },
  { name: 'Events', url: 'http://localhost:5651/events', auth: false },
  { name: 'Login', url: 'http://localhost:5651/auth/login', auth: false },
  { name: 'Member Dashboard', url: 'http://localhost:5651/members/dashboard', auth: true },
  { name: 'Profile', url: 'http://localhost:5651/members/profile', auth: true },
  { name: 'Admin Event Management', url: 'http://localhost:5651/admin/events', auth: true, role: 'admin' },
  { name: 'Admin Vetting Queue', url: 'http://localhost:5651/admin/vetting', auth: true, role: 'admin' }
];

// WCAG 2.1 AA compliance configuration
const WCAG_CONFIG = {
  level: 'AA',
  principles: {
    perceivable: ['1.1', '1.2', '1.3', '1.4'],
    operable: ['2.1', '2.2', '2.3', '2.4', '2.5'],
    understandable: ['3.1', '3.2', '3.3'],
    robust: ['4.1']
  }
};

// Lighthouse configuration focused on accessibility
const LIGHTHOUSE_CONFIG = {
  logLevel: 'error',
  output: 'json',
  onlyCategories: ['accessibility'],
  formFactor: 'desktop',
  screenEmulation: {
    mobile: false,
    width: 1920,
    height: 1080,
    deviceScaleFactor: 1,
    disabled: false
  },
  throttling: {
    rttMs: 40,
    throughputKbps: 10 * 1024,
    cpuSlowdownMultiplier: 1
  }
};

// Color contrast requirements
const CONTRAST_REQUIREMENTS = {
  normal: {
    AA: 4.5,
    AAA: 7
  },
  large: {
    AA: 3,
    AAA: 4.5
  }
};

class AccessibilityAuditor {
  constructor() {
    this.results = [];
    this.browser = null;
    this.chrome = null;
  }

  async initialize() {
    // Launch Chrome with debugging port for Lighthouse
    this.chrome = await chromeLauncher.launch({
      chromeFlags: ['--headless', '--disable-gpu', '--no-sandbox', '--enable-accessibility-object-model']
    });
    
    // Connect Puppeteer to the same instance
    this.browser = await puppeteer.connect({
      browserURL: `http://localhost:${this.chrome.port}`,
      defaultViewport: { width: 1920, height: 1080 }
    });
  }

  async cleanup() {
    if (this.browser) await this.browser.close();
    if (this.chrome) await this.chrome.kill();
  }

  async runLighthouseAccessibilityAudit(url) {
    const options = {
      ...LIGHTHOUSE_CONFIG,
      port: this.chrome.port
    };

    const runnerResult = await lighthouse(url, options);
    return runnerResult.lhr;
  }

  async checkColorContrast(page) {
    return await page.evaluate((CONTRAST_REQUIREMENTS) => {
      const results = [];
      
      function getLuminance(rgb) {
        const [r, g, b] = rgb.match(/\d+/g).map(Number);
        const sRGB = [r, g, b].map(val => {
          val = val / 255;
          return val <= 0.03928 ? val / 12.92 : Math.pow((val + 0.055) / 1.055, 2.4);
        });
        return 0.2126 * sRGB[0] + 0.7152 * sRGB[1] + 0.0722 * sRGB[2];
      }

      function getContrastRatio(color1, color2) {
        const lum1 = getLuminance(color1);
        const lum2 = getLuminance(color2);
        const lighter = Math.max(lum1, lum2);
        const darker = Math.min(lum1, lum2);
        return (lighter + 0.05) / (darker + 0.05);
      }

      function isLargeText(element) {
        const fontSize = parseFloat(window.getComputedStyle(element).fontSize);
        const fontWeight = window.getComputedStyle(element).fontWeight;
        return fontSize >= 18 || (fontSize >= 14 && fontWeight >= 700);
      }

      const elements = document.querySelectorAll('*');
      elements.forEach(element => {
        const style = window.getComputedStyle(element);
        const color = style.color;
        const backgroundColor = style.backgroundColor;
        
        if (color !== 'rgba(0, 0, 0, 0)' && backgroundColor !== 'rgba(0, 0, 0, 0)') {
          const contrast = getContrastRatio(color, backgroundColor);
          const isLarge = isLargeText(element);
          const requirements = isLarge ? CONTRAST_REQUIREMENTS.large : CONTRAST_REQUIREMENTS.normal;
          
          if (contrast < requirements.AA) {
            results.push({
              element: element.tagName + (element.id ? '#' + element.id : '') + (element.className ? '.' + element.className.split(' ').join('.') : ''),
              color,
              backgroundColor,
              contrast: contrast.toFixed(2),
              required: requirements.AA,
              isLarge,
              text: element.textContent.trim().substring(0, 50),
              passed: false
            });
          }
        }
      });
      
      return results;
    }, CONTRAST_REQUIREMENTS);
  }

  async checkKeyboardNavigation(page) {
    return await page.evaluate(() => {
      const results = {
        focusableElements: [],
        tabOrder: [],
        missingTabIndex: [],
        skipLinks: [],
        keyboardTraps: []
      };

      // Get all focusable elements
      const focusableSelectors = 'a[href], button, input, select, textarea, [tabindex]:not([tabindex="-1"]), [contenteditable]';
      const focusableElements = document.querySelectorAll(focusableSelectors);
      
      focusableElements.forEach((element, index) => {
        const rect = element.getBoundingClientRect();
        const isVisible = rect.width > 0 && rect.height > 0;
        const tabIndex = element.getAttribute('tabindex');
        
        results.focusableElements.push({
          element: element.tagName + (element.id ? '#' + element.id : ''),
          tabIndex: tabIndex || '0',
          isVisible,
          hasLabel: !!element.getAttribute('aria-label') || !!element.textContent.trim(),
          type: element.type || element.tagName.toLowerCase()
        });

        if (tabIndex && parseInt(tabIndex) > 0) {
          results.tabOrder.push({
            element: element.tagName + (element.id ? '#' + element.id : ''),
            tabIndex: parseInt(tabIndex)
          });
        }
      });

      // Check for skip links
      const skipLinks = document.querySelectorAll('a[href^="#"]');
      skipLinks.forEach(link => {
        const targetId = link.getAttribute('href').substring(1);
        const target = document.getElementById(targetId);
        results.skipLinks.push({
          link: link.textContent.trim(),
          targetExists: !!target,
          targetId
        });
      });

      // Check for interactive elements without keyboard access
      const clickableElements = document.querySelectorAll('[onclick], [onmousedown], [onmouseup]');
      clickableElements.forEach(element => {
        if (!element.matches(focusableSelectors)) {
          results.missingTabIndex.push({
            element: element.tagName + (element.id ? '#' + element.id : ''),
            hasClickHandler: true,
            recommendation: 'Add tabindex="0" and keyboard event handlers'
          });
        }
      });

      return results;
    });
  }

  async checkAriaAttributes(page) {
    return await page.evaluate(() => {
      const results = {
        missingLabels: [],
        invalidAriaAttributes: [],
        missingRoles: [],
        landmarkRegions: [],
        headingStructure: []
      };

      // Check form elements for labels
      const formElements = document.querySelectorAll('input, select, textarea');
      formElements.forEach(element => {
        const hasLabel = element.labels && element.labels.length > 0;
        const hasAriaLabel = element.hasAttribute('aria-label');
        const hasAriaLabelledBy = element.hasAttribute('aria-labelledby');
        
        if (!hasLabel && !hasAriaLabel && !hasAriaLabelledBy && element.type !== 'hidden') {
          results.missingLabels.push({
            element: element.tagName + (element.id ? '#' + element.id : ''),
            type: element.type,
            name: element.name
          });
        }
      });

      // Check ARIA attributes validity
      const ariaElements = document.querySelectorAll('[aria-label], [aria-labelledby], [aria-describedby], [role]');
      ariaElements.forEach(element => {
        // Check if aria-labelledby references exist
        if (element.hasAttribute('aria-labelledby')) {
          const ids = element.getAttribute('aria-labelledby').split(' ');
          ids.forEach(id => {
            if (!document.getElementById(id)) {
              results.invalidAriaAttributes.push({
                element: element.tagName + (element.id ? '#' + element.id : ''),
                attribute: 'aria-labelledby',
                issue: `Referenced element with id "${id}" not found`
              });
            }
          });
        }

        // Check if aria-describedby references exist
        if (element.hasAttribute('aria-describedby')) {
          const ids = element.getAttribute('aria-describedby').split(' ');
          ids.forEach(id => {
            if (!document.getElementById(id)) {
              results.invalidAriaAttributes.push({
                element: element.tagName + (element.id ? '#' + element.id : ''),
                attribute: 'aria-describedby',
                issue: `Referenced element with id "${id}" not found`
              });
            }
          });
        }
      });

      // Check for landmark regions
      const landmarks = document.querySelectorAll('main, nav, header, footer, aside, [role="main"], [role="navigation"], [role="banner"], [role="contentinfo"], [role="complementary"]');
      landmarks.forEach(element => {
        results.landmarkRegions.push({
          element: element.tagName,
          role: element.getAttribute('role') || element.tagName.toLowerCase(),
          hasLabel: !!element.getAttribute('aria-label') || !!element.getAttribute('aria-labelledby')
        });
      });

      // Check heading structure
      const headings = document.querySelectorAll('h1, h2, h3, h4, h5, h6');
      let previousLevel = 0;
      headings.forEach((heading, index) => {
        const level = parseInt(heading.tagName.substring(1));
        const skip = level - previousLevel > 1 && previousLevel > 0;
        
        results.headingStructure.push({
          level,
          text: heading.textContent.trim(),
          skip,
          index
        });
        
        previousLevel = level;
      });

      return results;
    });
  }

  async checkScreenReaderCompatibility(page) {
    return await page.evaluate(() => {
      const results = {
        images: [],
        links: [],
        buttons: [],
        forms: [],
        tables: [],
        dynamicContent: []
      };

      // Check images for alt text
      const images = document.querySelectorAll('img');
      images.forEach(img => {
        const alt = img.getAttribute('alt');
        const isDecorative = img.getAttribute('role') === 'presentation' || alt === '';
        
        results.images.push({
          src: img.src,
          hasAlt: alt !== null,
          altText: alt,
          isDecorative,
          issue: alt === null ? 'Missing alt attribute' : (alt === '' && !isDecorative ? 'Empty alt text for non-decorative image' : null)
        });
      });

      // Check links for meaningful text
      const links = document.querySelectorAll('a');
      links.forEach(link => {
        const text = link.textContent.trim();
        const ariaLabel = link.getAttribute('aria-label');
        const hasImage = link.querySelector('img');
        
        results.links.push({
          href: link.href,
          text,
          ariaLabel,
          hasImage,
          issue: !text && !ariaLabel ? 'Link has no accessible text' : (text.toLowerCase() === 'click here' || text.toLowerCase() === 'read more' ? 'Generic link text' : null)
        });
      });

      // Check buttons for accessible names
      const buttons = document.querySelectorAll('button, [role="button"]');
      buttons.forEach(button => {
        const text = button.textContent.trim();
        const ariaLabel = button.getAttribute('aria-label');
        const hasImage = button.querySelector('img');
        
        results.buttons.push({
          text,
          ariaLabel,
          hasImage,
          type: button.type || 'button',
          issue: !text && !ariaLabel ? 'Button has no accessible name' : null
        });
      });

      // Check forms for fieldsets and legends
      const forms = document.querySelectorAll('form');
      forms.forEach(form => {
        const fieldsets = form.querySelectorAll('fieldset');
        const hasFieldsets = fieldsets.length > 0;
        const legends = form.querySelectorAll('legend');
        
        results.forms.push({
          name: form.name,
          hasFieldsets,
          hasLegends: legends.length > 0,
          fieldsetCount: fieldsets.length,
          legendCount: legends.length,
          issue: hasFieldsets && legends.length < fieldsets.length ? 'Fieldsets without legends' : null
        });
      });

      // Check tables for proper structure
      const tables = document.querySelectorAll('table');
      tables.forEach(table => {
        const caption = table.querySelector('caption');
        const headers = table.querySelectorAll('th');
        const scope = Array.from(headers).filter(h => h.hasAttribute('scope'));
        
        results.tables.push({
          hasCaption: !!caption,
          headerCount: headers.length,
          scopeCount: scope.length,
          issue: headers.length > 0 && scope.length === 0 ? 'Table headers without scope attributes' : null
        });
      });

      // Check for live regions and dynamic content
      const liveRegions = document.querySelectorAll('[aria-live], [role="alert"], [role="status"], [role="log"]');
      liveRegions.forEach(region => {
        results.dynamicContent.push({
          type: region.getAttribute('role') || 'live-region',
          ariaLive: region.getAttribute('aria-live'),
          ariaAtomic: region.getAttribute('aria-atomic'),
          ariaRelevant: region.getAttribute('aria-relevant')
        });
      });

      return results;
    });
  }

  async testFocusIndicators(page) {
    return await page.evaluate(() => {
      const results = [];
      const focusableSelectors = 'a[href], button, input, select, textarea, [tabindex]:not([tabindex="-1"])';
      const elements = document.querySelectorAll(focusableSelectors);
      
      elements.forEach(element => {
        const rect = element.getBoundingClientRect();
        if (rect.width === 0 || rect.height === 0) return;
        
        // Temporarily focus the element
        element.focus();
        const focusedStyle = window.getComputedStyle(element);
        const focusedOutline = focusedStyle.outline;
        const focusedBorder = focusedStyle.border;
        const focusedBoxShadow = focusedStyle.boxShadow;
        
        // Blur and get unfocused styles
        element.blur();
        const unfocusedStyle = window.getComputedStyle(element);
        const unfocusedOutline = unfocusedStyle.outline;
        const unfocusedBorder = unfocusedStyle.border;
        const unfocusedBoxShadow = unfocusedStyle.boxShadow;
        
        const hasVisibleFocusIndicator = 
          focusedOutline !== unfocusedOutline ||
          focusedBorder !== unfocusedBorder ||
          focusedBoxShadow !== unfocusedBoxShadow;
        
        results.push({
          element: element.tagName + (element.id ? '#' + element.id : ''),
          hasVisibleFocusIndicator,
          focusStyles: {
            outline: focusedOutline,
            border: focusedBorder,
            boxShadow: focusedBoxShadow
          }
        });
      });
      
      return results;
    });
  }

  async auditPage(pageInfo) {
    console.log(`\nAuditing ${pageInfo.name} page for accessibility...`);
    
    const page = await this.browser.newPage();
    const result = {
      ...pageInfo,
      timestamp: new Date().toISOString(),
      wcagViolations: [],
      colorContrastIssues: [],
      keyboardNavigationIssues: [],
      ariaIssues: [],
      screenReaderIssues: [],
      focusIndicatorIssues: []
    };
    
    try {
      // Navigate to the page
      await page.goto(pageInfo.url, { waitUntil: 'networkidle2', timeout: 30000 });
      
      // Run Lighthouse accessibility audit
      console.log('  Running Lighthouse accessibility audit...');
      const lighthouseResults = await this.runLighthouseAccessibilityAudit(pageInfo.url);
      result.lighthouseScore = Math.round(lighthouseResults.categories.accessibility.score * 100);
      
      // Extract WCAG violations from Lighthouse
      if (lighthouseResults.categories.accessibility.auditRefs) {
        for (const auditRef of lighthouseResults.categories.accessibility.auditRefs) {
          const audit = lighthouseResults.audits[auditRef.id];
          if (audit && audit.score !== null && audit.score < 1) {
            result.wcagViolations.push({
              id: audit.id,
              title: audit.title,
              description: audit.description,
              impact: auditRef.weight >= 10 ? 'critical' : auditRef.weight >= 3 ? 'serious' : 'minor',
              wcagCriteria: audit.id.includes('aria') ? '4.1.2' : 
                           audit.id.includes('color-contrast') ? '1.4.3' :
                           audit.id.includes('label') ? '1.3.1' : 'various',
              details: audit.details
            });
          }
        }
      }
      
      // Check color contrast
      console.log('  Checking color contrast...');
      const contrastIssues = await this.checkColorContrast(page);
      result.colorContrastIssues = contrastIssues;
      
      // Check keyboard navigation
      console.log('  Testing keyboard navigation...');
      const keyboardNav = await this.checkKeyboardNavigation(page);
      result.keyboardNavigationIssues = keyboardNav;
      
      // Check ARIA attributes
      console.log('  Checking ARIA attributes...');
      const ariaCheck = await this.checkAriaAttributes(page);
      result.ariaIssues = ariaCheck;
      
      // Check screen reader compatibility
      console.log('  Testing screen reader compatibility...');
      const screenReaderCheck = await this.checkScreenReaderCompatibility(page);
      result.screenReaderIssues = screenReaderCheck;
      
      // Test focus indicators
      console.log('  Testing focus indicators...');
      const focusCheck = await this.testFocusIndicators(page);
      result.focusIndicatorIssues = focusCheck.filter(item => !item.hasVisibleFocusIndicator);
      
      // Calculate overall compliance
      result.overallCompliance = this.calculateCompliance(result);
      
    } catch (error) {
      result.error = error.message;
      console.error(`  Error auditing ${pageInfo.name}:`, error.message);
    } finally {
      await page.close();
    }
    
    return result;
  }

  calculateCompliance(result) {
    let totalIssues = 0;
    let criticalIssues = 0;
    
    // Count WCAG violations
    result.wcagViolations.forEach(violation => {
      totalIssues++;
      if (violation.impact === 'critical' || violation.impact === 'serious') {
        criticalIssues++;
      }
    });
    
    // Count color contrast issues
    totalIssues += result.colorContrastIssues.length;
    criticalIssues += result.colorContrastIssues.length; // All contrast issues are critical
    
    // Count keyboard navigation issues
    totalIssues += result.keyboardNavigationIssues.missingTabIndex.length;
    totalIssues += result.keyboardNavigationIssues.keyboardTraps.length;
    criticalIssues += result.keyboardNavigationIssues.keyboardTraps.length;
    
    // Count ARIA issues
    totalIssues += result.ariaIssues.missingLabels.length;
    totalIssues += result.ariaIssues.invalidAriaAttributes.length;
    criticalIssues += result.ariaIssues.missingLabels.length;
    
    // Count screen reader issues
    const srIssues = result.screenReaderIssues;
    totalIssues += srIssues.images.filter(img => img.issue).length;
    totalIssues += srIssues.links.filter(link => link.issue).length;
    totalIssues += srIssues.buttons.filter(btn => btn.issue).length;
    criticalIssues += srIssues.images.filter(img => img.issue).length;
    criticalIssues += srIssues.buttons.filter(btn => btn.issue).length;
    
    // Count focus indicator issues
    totalIssues += result.focusIndicatorIssues.length;
    
    return {
      totalIssues,
      criticalIssues,
      lighthouseScore: result.lighthouseScore,
      wcagLevel: criticalIssues === 0 ? 'AA' : totalIssues < 5 ? 'A' : 'Non-compliant'
    };
  }

  async generateReport(results) {
    const report = {
      metadata: {
        reportTitle: 'Comprehensive Accessibility Audit Report',
        generatedAt: new Date().toISOString(),
        wcagVersion: '2.1',
        targetLevel: 'AA',
        totalPagesAudited: results.length
      },
      summary: {
        overallScore: 0,
        totalIssues: 0,
        criticalIssues: 0,
        pageScores: [],
        wcagCompliance: {
          level: 'Unknown',
          percentCompliant: 0
        }
      },
      detailedResults: results,
      recommendations: [],
      fixPriorities: []
    };
    
    // Calculate summary statistics
    let totalScore = 0;
    let totalIssues = 0;
    let criticalIssues = 0;
    
    results.forEach(result => {
      if (!result.error) {
        totalScore += result.lighthouseScore;
        totalIssues += result.overallCompliance.totalIssues;
        criticalIssues += result.overallCompliance.criticalIssues;
        
        report.summary.pageScores.push({
          page: result.name,
          score: result.lighthouseScore,
          issues: result.overallCompliance.totalIssues,
          critical: result.overallCompliance.criticalIssues
        });
      }
    });
    
    report.summary.overallScore = Math.round(totalScore / results.filter(r => !r.error).length);
    report.summary.totalIssues = totalIssues;
    report.summary.criticalIssues = criticalIssues;
    report.summary.wcagCompliance.level = criticalIssues === 0 ? 'AA' : 'Non-compliant';
    report.summary.wcagCompliance.percentCompliant = Math.round((1 - criticalIssues / Math.max(totalIssues, 1)) * 100);
    
    // Generate recommendations
    report.recommendations = this.generateRecommendations(results);
    report.fixPriorities = this.prioritizeFixes(results);
    
    return report;
  }

  generateRecommendations(results) {
    const recommendations = [];
    const issueTypes = new Map();
    
    // Aggregate issues by type
    results.forEach(result => {
      if (result.error) return;
      
      // Color contrast issues
      if (result.colorContrastIssues.length > 0) {
        if (!issueTypes.has('contrast')) issueTypes.set('contrast', 0);
        issueTypes.set('contrast', issueTypes.get('contrast') + result.colorContrastIssues.length);
      }
      
      // Missing labels
      if (result.ariaIssues.missingLabels.length > 0) {
        if (!issueTypes.has('labels')) issueTypes.set('labels', 0);
        issueTypes.set('labels', issueTypes.get('labels') + result.ariaIssues.missingLabels.length);
      }
      
      // Keyboard navigation
      if (result.keyboardNavigationIssues.missingTabIndex.length > 0) {
        if (!issueTypes.has('keyboard')) issueTypes.set('keyboard', 0);
        issueTypes.set('keyboard', issueTypes.get('keyboard') + result.keyboardNavigationIssues.missingTabIndex.length);
      }
      
      // Images without alt text
      const imgIssues = result.screenReaderIssues.images.filter(img => img.issue).length;
      if (imgIssues > 0) {
        if (!issueTypes.has('images')) issueTypes.set('images', 0);
        issueTypes.set('images', issueTypes.get('images') + imgIssues);
      }
    });
    
    // Generate recommendations based on issue patterns
    if (issueTypes.has('contrast')) {
      recommendations.push({
        category: 'Color Contrast',
        priority: 'Critical',
        recommendation: 'Review and update color schemes to meet WCAG 2.1 AA contrast ratios (4.5:1 for normal text, 3:1 for large text)',
        impact: `${issueTypes.get('contrast')} elements with insufficient contrast`,
        wcagCriteria: '1.4.3'
      });
    }
    
    if (issueTypes.has('labels')) {
      recommendations.push({
        category: 'Form Accessibility',
        priority: 'Critical',
        recommendation: 'Add proper labels to all form inputs using <label> elements or aria-label attributes',
        impact: `${issueTypes.get('labels')} form elements without accessible labels`,
        wcagCriteria: '1.3.1, 3.3.2'
      });
    }
    
    if (issueTypes.has('keyboard')) {
      recommendations.push({
        category: 'Keyboard Navigation',
        priority: 'High',
        recommendation: 'Ensure all interactive elements are keyboard accessible by adding appropriate tabindex and keyboard event handlers',
        impact: `${issueTypes.get('keyboard')} interactive elements not keyboard accessible`,
        wcagCriteria: '2.1.1'
      });
    }
    
    if (issueTypes.has('images')) {
      recommendations.push({
        category: 'Alternative Text',
        priority: 'High',
        recommendation: 'Provide meaningful alt text for all informative images, and empty alt="" for decorative images',
        impact: `${issueTypes.get('images')} images with missing or inadequate alt text`,
        wcagCriteria: '1.1.1'
      });
    }
    
    return recommendations;
  }

  prioritizeFixes(results) {
    const fixes = [];
    
    results.forEach(result => {
      if (result.error) return;
      
      // Critical: Color contrast issues
      result.colorContrastIssues.forEach(issue => {
        fixes.push({
          page: result.name,
          priority: 1,
          category: 'Color Contrast',
          element: issue.element,
          issue: `Contrast ratio ${issue.contrast} below required ${issue.required}`,
          fix: `Update colors - Current: ${issue.color} on ${issue.backgroundColor}`,
          wcagCriteria: '1.4.3'
        });
      });
      
      // Critical: Missing form labels
      result.ariaIssues.missingLabels.forEach(element => {
        fixes.push({
          page: result.name,
          priority: 1,
          category: 'Form Labels',
          element: element.element,
          issue: 'Form input without accessible label',
          fix: `Add <label> element or aria-label attribute for ${element.type} input`,
          wcagCriteria: '1.3.1'
        });
      });
      
      // High: Missing alt text
      result.screenReaderIssues.images.filter(img => img.issue).forEach(img => {
        fixes.push({
          page: result.name,
          priority: 2,
          category: 'Images',
          element: 'img',
          issue: img.issue,
          fix: img.isDecorative ? 'Add role="presentation" for decorative image' : 'Add descriptive alt text',
          wcagCriteria: '1.1.1'
        });
      });
      
      // Medium: Focus indicators
      result.focusIndicatorIssues.forEach(element => {
        fixes.push({
          page: result.name,
          priority: 3,
          category: 'Focus Indicators',
          element: element.element,
          issue: 'No visible focus indicator',
          fix: 'Add CSS outline or border for :focus state',
          wcagCriteria: '2.4.7'
        });
      });
    });
    
    // Sort by priority
    return fixes.sort((a, b) => a.priority - b.priority);
  }

  async run() {
    console.log('Starting Comprehensive Accessibility Audit...\n');
    console.log('Testing against WCAG 2.1 Level AA standards\n');
    
    try {
      await this.initialize();
      
      for (const pageInfo of PAGES_TO_AUDIT) {
        const result = await this.auditPage(pageInfo);
        this.results.push(result);
      }
      
      // Generate comprehensive report
      const report = await this.generateReport(this.results);
      
      // Save detailed JSON report
      const jsonPath = path.join(__dirname, 'accessibility-audit-results.json');
      await fs.writeFile(jsonPath, JSON.stringify(report, null, 2));
      console.log(`\nDetailed results saved to: ${jsonPath}`);
      
      // Generate and save markdown report
      const markdownReport = this.generateMarkdownReport(report);
      const mdPath = path.join(__dirname, 'accessibility-audit-report.md');
      await fs.writeFile(mdPath, markdownReport);
      console.log(`Markdown report saved to: ${mdPath}`);
      
      // Generate and save fixes script
      const fixesScript = this.generateFixesScript(report);
      const fixesPath = path.join(__dirname, 'accessibility-fixes.md');
      await fs.writeFile(fixesPath, fixesScript);
      console.log(`Fixes documentation saved to: ${fixesPath}`);
      
      // Display summary
      console.log('\n=== AUDIT SUMMARY ===');
      console.log(`Overall Accessibility Score: ${report.summary.overallScore}/100`);
      console.log(`WCAG 2.1 Compliance Level: ${report.summary.wcagCompliance.level}`);
      console.log(`Total Issues Found: ${report.summary.totalIssues}`);
      console.log(`Critical Issues: ${report.summary.criticalIssues}`);
      console.log('\nTop Recommendations:');
      report.recommendations.slice(0, 3).forEach((rec, i) => {
        console.log(`${i + 1}. ${rec.recommendation}`);
      });
      
    } catch (error) {
      console.error('Audit failed:', error);
    } finally {
      await this.cleanup();
    }
  }

  generateMarkdownReport(report) {
    let md = `# Comprehensive Accessibility Audit Report\n\n`;
    md += `Generated: ${new Date(report.metadata.generatedAt).toLocaleString()}\n`;
    md += `WCAG Version: ${report.metadata.wcagVersion} Level ${report.metadata.targetLevel}\n\n`;
    
    // Executive Summary
    md += `## Executive Summary\n\n`;
    md += `- **Overall Accessibility Score**: ${report.summary.overallScore}/100\n`;
    md += `- **WCAG Compliance Level**: ${report.summary.wcagCompliance.level}\n`;
    md += `- **Total Issues**: ${report.summary.totalIssues}\n`;
    md += `- **Critical Issues**: ${report.summary.criticalIssues}\n`;
    md += `- **Compliance Rate**: ${report.summary.wcagCompliance.percentCompliant}%\n\n`;
    
    // Page Scores
    md += `## Page-by-Page Scores\n\n`;
    md += `| Page | Score | Total Issues | Critical Issues |\n`;
    md += `|------|-------|--------------|----------------|\n`;
    report.summary.pageScores.forEach(page => {
      md += `| ${page.page} | ${page.score}/100 | ${page.issues} | ${page.critical} |\n`;
    });
    md += `\n`;
    
    // Detailed Results by Page
    md += `## Detailed Results\n\n`;
    report.detailedResults.forEach(result => {
      md += `### ${result.name} Page\n`;
      md += `URL: ${result.url}\n\n`;
      
      if (result.error) {
        md += `**Error**: ${result.error}\n\n`;
        return;
      }
      
      md += `**Lighthouse Accessibility Score**: ${result.lighthouseScore}/100\n\n`;
      
      // WCAG Violations
      if (result.wcagViolations.length > 0) {
        md += `#### WCAG Violations\n\n`;
        result.wcagViolations.forEach(violation => {
          md += `- **${violation.title}** (${violation.impact})\n`;
          md += `  - WCAG Criteria: ${violation.wcagCriteria}\n`;
          md += `  - ${violation.description}\n\n`;
        });
      }
      
      // Color Contrast Issues
      if (result.colorContrastIssues.length > 0) {
        md += `#### Color Contrast Issues\n\n`;
        md += `Found ${result.colorContrastIssues.length} elements with insufficient contrast:\n\n`;
        result.colorContrastIssues.slice(0, 5).forEach(issue => {
          md += `- **${issue.element}**: Contrast ${issue.contrast} (Required: ${issue.required})\n`;
          md += `  - Text: "${issue.text}"\n`;
          md += `  - Colors: ${issue.color} on ${issue.backgroundColor}\n\n`;
        });
        if (result.colorContrastIssues.length > 5) {
          md += `...and ${result.colorContrastIssues.length - 5} more\n\n`;
        }
      }
      
      // Keyboard Navigation Issues
      if (result.keyboardNavigationIssues.missingTabIndex.length > 0) {
        md += `#### Keyboard Navigation Issues\n\n`;
        md += `- ${result.keyboardNavigationIssues.missingTabIndex.length} interactive elements without keyboard access\n`;
        md += `- ${result.keyboardNavigationIssues.focusableElements.length} total focusable elements\n`;
        md += `- ${result.keyboardNavigationIssues.skipLinks.length} skip links found\n\n`;
      }
      
      // ARIA Issues
      if (result.ariaIssues.missingLabels.length > 0) {
        md += `#### ARIA and Labeling Issues\n\n`;
        md += `- ${result.ariaIssues.missingLabels.length} form elements without labels\n`;
        md += `- ${result.ariaIssues.invalidAriaAttributes.length} invalid ARIA references\n`;
        md += `- ${result.ariaIssues.landmarkRegions.length} landmark regions found\n\n`;
      }
      
      // Screen Reader Issues
      const srIssues = result.screenReaderIssues;
      const imageIssues = srIssues.images.filter(img => img.issue).length;
      const linkIssues = srIssues.links.filter(link => link.issue).length;
      const buttonIssues = srIssues.buttons.filter(btn => btn.issue).length;
      
      if (imageIssues + linkIssues + buttonIssues > 0) {
        md += `#### Screen Reader Compatibility Issues\n\n`;
        if (imageIssues > 0) md += `- ${imageIssues} images with missing or inadequate alt text\n`;
        if (linkIssues > 0) md += `- ${linkIssues} links with generic or missing text\n`;
        if (buttonIssues > 0) md += `- ${buttonIssues} buttons without accessible names\n`;
        md += `\n`;
      }
      
      md += `---\n\n`;
    });
    
    // Recommendations
    md += `## Recommendations\n\n`;
    report.recommendations.forEach((rec, i) => {
      md += `### ${i + 1}. ${rec.category} (${rec.priority} Priority)\n\n`;
      md += `**Recommendation**: ${rec.recommendation}\n\n`;
      md += `**Impact**: ${rec.impact}\n\n`;
      md += `**WCAG Criteria**: ${rec.wcagCriteria}\n\n`;
    });
    
    // Fix Priorities
    md += `## Top Priority Fixes\n\n`;
    const topFixes = report.fixPriorities.slice(0, 10);
    topFixes.forEach((fix, i) => {
      md += `${i + 1}. **${fix.page}** - ${fix.category}\n`;
      md += `   - Element: ${fix.element}\n`;
      md += `   - Issue: ${fix.issue}\n`;
      md += `   - Fix: ${fix.fix}\n\n`;
    });
    
    return md;
  }

  generateFixesScript(report) {
    let fixes = `# Accessibility Fixes Implementation Guide\n\n`;
    fixes += `Generated from audit on: ${new Date(report.metadata.generatedAt).toLocaleString()}\n\n`;
    
    fixes += `## Critical Fixes (Must be implemented for WCAG AA compliance)\n\n`;
    
    // Group fixes by category
    const fixesByCategory = new Map();
    report.fixPriorities.forEach(fix => {
      if (!fixesByCategory.has(fix.category)) {
        fixesByCategory.set(fix.category, []);
      }
      fixesByCategory.get(fix.category).push(fix);
    });
    
    // Color Contrast Fixes
    if (fixesByCategory.has('Color Contrast')) {
      fixes += `### 1. Color Contrast Fixes\n\n`;
      fixes += `Update the following CSS to meet WCAG contrast requirements:\n\n`;
      fixes += `\`\`\`css\n`;
      fixes += `/* Color Contrast Fixes for WCAG AA Compliance */\n\n`;
      
      const contrastFixes = fixesByCategory.get('Color Contrast').slice(0, 10);
      contrastFixes.forEach(fix => {
        fixes += `/* ${fix.page} - ${fix.element} */\n`;
        fixes += `/* Current contrast: ${fix.issue} */\n`;
        fixes += `/* TODO: ${fix.fix} */\n\n`;
      });
      
      fixes += `/* Example fix for improving contrast */\n`;
      fixes += `.low-contrast-text {\n`;
      fixes += `  color: #212529; /* Darker text color */\n`;
      fixes += `  background-color: #ffffff; /* White background */\n`;
      fixes += `  /* Contrast ratio: 16.1:1 (exceeds AA and AAA) */\n`;
      fixes += `}\n`;
      fixes += `\`\`\`\n\n`;
    }
    
    // Form Label Fixes
    if (fixesByCategory.has('Form Labels')) {
      fixes += `### 2. Form Label Fixes\n\n`;
      fixes += `Add proper labels to form elements:\n\n`;
      fixes += `\`\`\`html\n`;
      fixes += `<!-- Form Label Fixes -->\n\n`;
      
      fixesByCategory.get('Form Labels').slice(0, 5).forEach(fix => {
        fixes += `<!-- ${fix.page} - ${fix.element} -->\n`;
        fixes += `<!-- ${fix.fix} -->\n\n`;
      });
      
      fixes += `<!-- Example: Adding label to input -->\n`;
      fixes += `<label for="username">Username</label>\n`;
      fixes += `<input type="text" id="username" name="username" required>\n\n`;
      
      fixes += `<!-- Example: Using aria-label -->\n`;
      fixes += `<input type="search" aria-label="Search products" placeholder="Search...">\n`;
      fixes += `\`\`\`\n\n`;
    }
    
    // Image Alt Text Fixes
    if (fixesByCategory.has('Images')) {
      fixes += `### 3. Image Alt Text Fixes\n\n`;
      fixes += `Add appropriate alt text to images:\n\n`;
      fixes += `\`\`\`html\n`;
      fixes += `<!-- Image Alt Text Fixes -->\n\n`;
      
      fixes += `<!-- For informative images, provide descriptive alt text -->\n`;
      fixes += `<img src="logo.png" alt="Witch City Rope company logo">\n\n`;
      
      fixes += `<!-- For decorative images, use empty alt -->\n`;
      fixes += `<img src="decorative-border.png" alt="" role="presentation">\n\n`;
      
      fixes += `<!-- For complex images, use longer descriptions -->\n`;
      fixes += `<figure>\n`;
      fixes += `  <img src="event-schedule.png" alt="Event schedule chart" aria-describedby="schedule-desc">\n`;
      fixes += `  <figcaption id="schedule-desc">\n`;
      fixes += `    Detailed schedule showing workshop times from 9am to 5pm...\n`;
      fixes += `  </figcaption>\n`;
      fixes += `</figure>\n`;
      fixes += `\`\`\`\n\n`;
    }
    
    // Focus Indicator Fixes
    if (fixesByCategory.has('Focus Indicators')) {
      fixes += `### 4. Focus Indicator Fixes\n\n`;
      fixes += `Add visible focus indicators for keyboard navigation:\n\n`;
      fixes += `\`\`\`css\n`;
      fixes += `/* Focus Indicator Styles */\n\n`;
      
      fixes += `/* Basic focus outline for all interactive elements */\n`;
      fixes += `a:focus,\n`;
      fixes += `button:focus,\n`;
      fixes += `input:focus,\n`;
      fixes += `select:focus,\n`;
      fixes += `textarea:focus,\n`;
      fixes += `[tabindex]:focus {\n`;
      fixes += `  outline: 3px solid #0066cc;\n`;
      fixes += `  outline-offset: 2px;\n`;
      fixes += `}\n\n`;
      
      fixes += `/* High contrast focus indicator for better visibility */\n`;
      fixes += `.focus-visible:focus {\n`;
      fixes += `  outline: 3px solid #000000;\n`;
      fixes += `  box-shadow: 0 0 0 6px #ffffff, 0 0 0 9px #000000;\n`;
      fixes += `}\n`;
      fixes += `\`\`\`\n\n`;
    }
    
    fixes += `## Additional Resources\n\n`;
    fixes += `- [WCAG 2.1 Guidelines](https://www.w3.org/WAI/WCAG21/quickref/)\n`;
    fixes += `- [WebAIM Contrast Checker](https://webaim.org/resources/contrastchecker/)\n`;
    fixes += `- [ARIA Authoring Practices](https://www.w3.org/TR/wai-aria-practices-1.1/)\n`;
    fixes += `- [Accessible Forms Guide](https://webaim.org/articles/forms/)\n\n`;
    
    fixes += `## Testing Your Fixes\n\n`;
    fixes += `After implementing these fixes, re-run the accessibility audit to verify improvements:\n\n`;
    fixes += `\`\`\`bash\n`;
    fixes += `npm run audit:accessibility\n`;
    fixes += `\`\`\`\n\n`;
    
    fixes += `Also test manually with:\n`;
    fixes += `- Keyboard navigation (Tab, Shift+Tab, Enter, Space)\n`;
    fixes += `- Screen readers (NVDA, JAWS, VoiceOver)\n`;
    fixes += `- Browser zoom (200% minimum)\n`;
    fixes += `- High contrast mode\n`;
    
    return fixes;
  }
}

// Run the audit
const auditor = new AccessibilityAuditor();
auditor.run().catch(console.error);
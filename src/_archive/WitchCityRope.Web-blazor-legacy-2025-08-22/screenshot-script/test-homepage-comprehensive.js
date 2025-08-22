const puppeteer = require('puppeteer');
const path = require('path');
const fs = require('fs');

async function testHomepage() {
  console.log('Starting comprehensive homepage test...\n');
  
  const results = {
    screenshot: null,
    accessibility: { violations: [] },
    elementChecks: {},
    layoutIssues: [],
    timestamp: new Date().toISOString()
  };

  // Configure browser
  const possibleChromePaths = [
    '/mnt/c/Program Files/Google/Chrome/Application/chrome.exe',
    '/mnt/c/Program Files (x86)/Google/Chrome/Application/chrome.exe',
    '/mnt/c/Users/chad/AppData/Local/Google/Chrome/Application/chrome.exe'
  ];

  let chromePath = null;
  for (const path of possibleChromePaths) {
    if (fs.existsSync(path)) {
      chromePath = path;
      break;
    }
  }

  const launchOptions = {
    headless: 'new',
    args: [
      '--no-sandbox',
      '--disable-setuid-sandbox',
      '--disable-dev-shm-usage',
      '--disable-gpu'
    ]
  };

  // Don't use Windows Chrome in WSL - use the bundled Chromium
  console.log('Using bundled Chromium...');

  try {
    const browser = await puppeteer.launch(launchOptions);
    const page = await browser.newPage();
    
    // Set viewport
    await page.setViewport({ width: 1920, height: 1080 });
    
    // Navigate to homepage
    console.log('Navigating to http://localhost:5651/...\n');
    
    try {
      await page.goto('http://localhost:5651/', {
        waitUntil: 'networkidle2',
        timeout: 30000
      });
    } catch (navError) {
      console.error('Navigation error:', navError.message);
      results.layoutIssues.push(`Navigation failed: ${navError.message}`);
    }

    // Wait for content to load
    await page.waitForTimeout(2000);

    // 1. TAKE FULL PAGE SCREENSHOT
    console.log('1. Taking full page screenshot...');
    const screenshotsDir = path.join(__dirname, 'screenshots');
    if (!fs.existsSync(screenshotsDir)) {
      fs.mkdirSync(screenshotsDir);
    }
    
    const screenshotPath = path.join(screenshotsDir, 'home-fullpage.png');
    await page.screenshot({
      path: screenshotPath,
      fullPage: true
    });
    console.log(`   ✓ Screenshot saved to: ${screenshotPath}\n`);
    results.screenshot = screenshotPath;

    // 2. RUN ACCESSIBILITY AUDIT
    console.log('2. Running accessibility audit...');
    
    // Inject axe-core for accessibility testing
    await page.addScriptTag({
      url: 'https://cdnjs.cloudflare.com/ajax/libs/axe-core/4.7.2/axe.min.js'
    });
    
    // Wait for axe to load
    await page.waitForTimeout(1000);
    
    // Run accessibility audit
    const accessibilityResults = await page.evaluate(async () => {
      if (typeof axe !== 'undefined') {
        const results = await axe.run();
        return {
          violations: results.violations.map(v => ({
            id: v.id,
            impact: v.impact,
            description: v.description,
            help: v.help,
            nodes: v.nodes.length,
            tags: v.tags
          })),
          passes: results.passes.length,
          inapplicable: results.inapplicable.length
        };
      }
      return null;
    });

    if (accessibilityResults) {
      results.accessibility = accessibilityResults;
      console.log(`   ✓ Found ${accessibilityResults.violations.length} accessibility violations`);
      console.log(`   ✓ ${accessibilityResults.passes} rules passed`);
      
      if (accessibilityResults.violations.length > 0) {
        console.log('\n   Accessibility Violations:');
        accessibilityResults.violations.forEach(v => {
          console.log(`   - [${v.impact}] ${v.id}: ${v.help} (${v.nodes} instances)`);
        });
      }
    } else {
      console.log('   ⚠ Could not run accessibility audit (axe-core not loaded)');
    }
    console.log('');

    // 3. CHECK SPECIFIC ELEMENTS
    console.log('3. Checking specific elements...\n');

    // Check hero tagline
    console.log('   Checking hero tagline...');
    const heroTagline = await page.evaluate(() => {
      const taglines = Array.from(document.querySelectorAll('*')).filter(el => 
        el.textContent.includes('Where curiosity meets connection') &&
        !Array.from(el.children).some(child => child.textContent.includes('Where curiosity meets connection'))
      );
      
      if (taglines.length > 0) {
        const el = taglines[0];
        const rect = el.getBoundingClientRect();
        const styles = window.getComputedStyle(el);
        return {
          found: true,
          visible: rect.width > 0 && rect.height > 0 && styles.display !== 'none' && styles.visibility !== 'hidden',
          element: {
            tagName: el.tagName,
            className: el.className,
            position: { x: rect.x, y: rect.y, width: rect.width, height: rect.height },
            styles: {
              fontSize: styles.fontSize,
              color: styles.color,
              fontWeight: styles.fontWeight
            }
          }
        };
      }
      return { found: false };
    });
    
    results.elementChecks.heroTagline = heroTagline;
    if (heroTagline.found) {
      console.log(`   ✓ Hero tagline found and ${heroTagline.visible ? 'visible' : 'NOT visible'}`);
      if (heroTagline.visible) {
        console.log(`     - Element: ${heroTagline.element.tagName}${heroTagline.element.className ? '.' + heroTagline.element.className : ''}`);
        console.log(`     - Font size: ${heroTagline.element.styles.fontSize}`);
      }
    } else {
      console.log('   ✗ Hero tagline "Where curiosity meets connection" not found');
      results.layoutIssues.push('Hero tagline not found');
    }

    // Check rope divider SVG
    console.log('\n   Checking rope divider SVG...');
    const ropeDivider = await page.evaluate(() => {
      const svgs = document.querySelectorAll('svg');
      let ropeFound = false;
      let animationInfo = [];
      
      svgs.forEach(svg => {
        // Check if SVG might be a rope divider (look for path elements, specific classes, etc.)
        const paths = svg.querySelectorAll('path');
        const hasAnimation = svg.querySelector('animate, animateTransform, animateMotion') !== null;
        const hasCSS3Animation = window.getComputedStyle(svg).animation !== 'none 0s ease 0s 1 normal none running';
        
        if (paths.length > 0 || svg.classList.toString().toLowerCase().includes('rope') || 
            svg.id.toLowerCase().includes('rope')) {
          ropeFound = true;
          animationInfo.push({
            id: svg.id,
            classes: svg.className.baseVal || svg.className,
            animated: hasAnimation || hasCSS3Animation,
            animationType: hasAnimation ? 'SVG animation' : (hasCSS3Animation ? 'CSS animation' : 'none'),
            pathCount: paths.length
          });
        }
      });
      
      return { found: ropeFound, svgs: animationInfo };
    });
    
    results.elementChecks.ropeDivider = ropeDivider;
    if (ropeDivider.found) {
      console.log(`   ✓ Found ${ropeDivider.svgs.length} potential rope divider SVG(s)`);
      ropeDivider.svgs.forEach(svg => {
        console.log(`     - ${svg.id || svg.classes || 'unnamed'}: ${svg.animated ? 'animated (' + svg.animationType + ')' : 'not animated'}`);
      });
    } else {
      console.log('   ⚠ No rope divider SVG found');
      results.layoutIssues.push('Rope divider SVG not found');
    }

    // Check gradient buttons
    console.log('\n   Checking gradient buttons...');
    const gradientButtons = await page.evaluate(() => {
      const buttons = document.querySelectorAll('button, a.btn, .button, [role="button"]');
      const gradientButtons = [];
      
      buttons.forEach(btn => {
        const styles = window.getComputedStyle(btn);
        const hasGradient = styles.backgroundImage.includes('gradient') || 
                          styles.background.includes('gradient');
        
        if (hasGradient) {
          const rect = btn.getBoundingClientRect();
          gradientButtons.push({
            text: btn.textContent.trim(),
            type: btn.tagName,
            gradient: styles.backgroundImage || styles.background,
            visible: rect.width > 0 && rect.height > 0,
            clickable: !btn.disabled && styles.pointerEvents !== 'none'
          });
        }
      });
      
      return gradientButtons;
    });
    
    results.elementChecks.gradientButtons = gradientButtons;
    if (gradientButtons.length > 0) {
      console.log(`   ✓ Found ${gradientButtons.length} gradient button(s)`);
      gradientButtons.forEach(btn => {
        console.log(`     - "${btn.text}" (${btn.type}): ${btn.visible ? 'visible' : 'hidden'}, ${btn.clickable ? 'clickable' : 'not clickable'}`);
      });
    } else {
      console.log('   ⚠ No gradient buttons found');
      results.layoutIssues.push('No gradient buttons found');
    }

    // Check footer sections
    console.log('\n   Checking footer sections...');
    const footerSections = await page.evaluate(() => {
      const footer = document.querySelector('footer');
      if (!footer) return { found: false };
      
      // Look for sections within footer
      const sections = [];
      const sectionElements = footer.querySelectorAll('div[class*="col"], section, .footer-section, div > div');
      
      // Group by common patterns
      const potentialSections = new Set();
      sectionElements.forEach(el => {
        // Check if this element has meaningful content
        const hasLinks = el.querySelectorAll('a').length > 0;
        const hasHeading = el.querySelector('h1, h2, h3, h4, h5, h6') !== null;
        const hasText = el.textContent.trim().length > 10;
        
        if (hasLinks || hasHeading || hasText) {
          // Check if it's not a child of another section we've already counted
          let isChild = false;
          potentialSections.forEach(section => {
            if (section.contains(el)) isChild = true;
          });
          
          if (!isChild) {
            potentialSections.add(el);
          }
        }
      });
      
      potentialSections.forEach(section => {
        const heading = section.querySelector('h1, h2, h3, h4, h5, h6');
        const links = section.querySelectorAll('a');
        sections.push({
          heading: heading ? heading.textContent.trim() : 'No heading',
          linkCount: links.length,
          content: section.textContent.trim().substring(0, 50) + '...'
        });
      });
      
      return {
        found: true,
        sectionCount: sections.length,
        sections: sections
      };
    });
    
    results.elementChecks.footerSections = footerSections;
    if (footerSections.found) {
      console.log(`   ${footerSections.sectionCount === 4 ? '✓' : '⚠'} Footer has ${footerSections.sectionCount} section(s) (expected 4)`);
      if (footerSections.sectionCount !== 4) {
        results.layoutIssues.push(`Footer has ${footerSections.sectionCount} sections instead of 4`);
      }
      footerSections.sections.forEach((section, i) => {
        console.log(`     - Section ${i + 1}: ${section.heading} (${section.linkCount} links)`);
      });
    } else {
      console.log('   ✗ Footer not found');
      results.layoutIssues.push('Footer not found');
    }

    // 4. CHECK FOR ADDITIONAL LAYOUT ISSUES
    console.log('\n4. Checking for layout issues...\n');
    
    const layoutChecks = await page.evaluate(() => {
      const issues = [];
      
      // Check for overflow
      if (document.body.scrollWidth > window.innerWidth) {
        issues.push('Horizontal scroll detected - content overflows viewport');
      }
      
      // Check for broken images
      const images = document.querySelectorAll('img');
      images.forEach(img => {
        if (!img.complete || img.naturalWidth === 0) {
          issues.push(`Broken image: ${img.src || img.getAttribute('src')}`);
        }
      });
      
      // Check for console errors
      const errorElements = document.querySelectorAll('.error, .alert-danger, [class*="error"]:not([class*="error-boundary"])');
      errorElements.forEach(el => {
        if (el.offsetParent !== null && el.textContent.trim()) {
          issues.push(`Error element visible: "${el.textContent.trim().substring(0, 50)}..."`);
        }
      });
      
      // Check viewport meta tag
      const viewport = document.querySelector('meta[name="viewport"]');
      if (!viewport) {
        issues.push('Missing viewport meta tag - may cause mobile display issues');
      }
      
      return issues;
    });
    
    layoutChecks.forEach(issue => {
      results.layoutIssues.push(issue);
      console.log(`   ⚠ ${issue}`);
    });
    
    if (layoutChecks.length === 0) {
      console.log('   ✓ No additional layout issues detected');
    }

    // Save results to file
    const reportPath = path.join(__dirname, 'homepage-test-results.json');
    fs.writeFileSync(reportPath, JSON.stringify(results, null, 2));
    console.log(`\n✓ Test results saved to: ${reportPath}`);

    // Generate summary report
    const summaryPath = path.join(__dirname, 'homepage-test-summary.md');
    const summary = generateSummaryReport(results);
    fs.writeFileSync(summaryPath, summary);
    console.log(`✓ Summary report saved to: ${summaryPath}`);

    await browser.close();
    console.log('\nTest completed successfully!');
    
    return results;

  } catch (error) {
    console.error('Test failed:', error.message);
    return {
      success: false,
      error: error.message
    };
  }
}

function generateSummaryReport(results) {
  const report = [];
  
  report.push('# Homepage Test Results');
  report.push(`\nTest Date: ${results.timestamp}\n`);
  
  report.push('## 1. Screenshot');
  report.push(`- Full page screenshot saved to: ${results.screenshot || 'Failed to capture'}\n`);
  
  report.push('## 2. Accessibility Audit');
  if (results.accessibility.violations) {
    report.push(`- **Violations Found:** ${results.accessibility.violations.length}`);
    report.push(`- **Rules Passed:** ${results.accessibility.passes || 0}`);
    
    if (results.accessibility.violations.length > 0) {
      report.push('\n### Violations:');
      results.accessibility.violations.forEach(v => {
        report.push(`- **[${v.impact}]** ${v.id}: ${v.help} (${v.nodes} instances)`);
      });
    }
  } else {
    report.push('- Accessibility audit could not be performed');
  }
  report.push('');
  
  report.push('## 3. Element Checks');
  
  report.push('\n### Hero Tagline');
  if (results.elementChecks.heroTagline?.found) {
    report.push(`✓ Found and ${results.elementChecks.heroTagline.visible ? 'visible' : '**NOT visible**'}`);
  } else {
    report.push('✗ **NOT FOUND** - "Where curiosity meets connection" tagline missing');
  }
  
  report.push('\n### Rope Divider SVG');
  if (results.elementChecks.ropeDivider?.found) {
    const animated = results.elementChecks.ropeDivider.svgs.some(s => s.animated);
    report.push(`✓ Found ${results.elementChecks.ropeDivider.svgs.length} SVG(s) - ${animated ? 'animated' : '**not animated**'}`);
  } else {
    report.push('⚠ Not found');
  }
  
  report.push('\n### Gradient Buttons');
  if (results.elementChecks.gradientButtons?.length > 0) {
    report.push(`✓ Found ${results.elementChecks.gradientButtons.length} gradient button(s)`);
    const working = results.elementChecks.gradientButtons.filter(b => b.visible && b.clickable);
    report.push(`- ${working.length} are visible and clickable`);
  } else {
    report.push('⚠ No gradient buttons found');
  }
  
  report.push('\n### Footer Sections');
  if (results.elementChecks.footerSections?.found) {
    const count = results.elementChecks.footerSections.sectionCount;
    report.push(`${count === 4 ? '✓' : '⚠'} Footer has ${count} section(s) ${count !== 4 ? '(expected 4)' : ''}`);
  } else {
    report.push('✗ **Footer not found**');
  }
  
  report.push('\n## 4. Layout Issues');
  if (results.layoutIssues.length > 0) {
    report.push(`Found ${results.layoutIssues.length} issue(s):`);
    results.layoutIssues.forEach(issue => {
      report.push(`- ${issue}`);
    });
  } else {
    report.push('✓ No layout issues detected');
  }
  
  report.push('\n## Summary');
  const criticalIssues = results.layoutIssues.filter(i => 
    i.includes('not found') || i.includes('Navigation failed')
  ).length;
  
  if (criticalIssues > 0) {
    report.push(`⚠ **${criticalIssues} critical issue(s) require attention**`);
  } else if (results.layoutIssues.length > 0) {
    report.push(`⚠ ${results.layoutIssues.length} minor issue(s) found`);
  } else {
    report.push('✓ All checks passed successfully!');
  }
  
  return report.join('\n');
}

// Run the test
if (require.main === module) {
  testHomepage().catch(console.error);
}

module.exports = testHomepage;
const puppeteer = require('puppeteer');
const path = require('path');
const fs = require('fs');

async function testLoginPage() {
  console.log('Starting MCP-style login page test...');
  
  const launchOptions = {
    headless: 'new', // Use new headless mode
    args: [
      '--no-sandbox',
      '--disable-setuid-sandbox',
      '--disable-dev-shm-usage',
      '--disable-gpu'
    ]
  };

  // Create screenshots directory
  const screenshotsDir = path.join(__dirname, 'screenshots');
  if (!fs.existsSync(screenshotsDir)) {
    fs.mkdirSync(screenshotsDir);
  }

  const timestamp = new Date().toISOString().replace(/:/g, '-').split('.')[0];
  const testResults = {
    timestamp,
    tests: [],
    screenshots: []
  };

  try {
    console.log('Launching browser...');
    const browser = await puppeteer.launch(launchOptions);
    const page = await browser.newPage();
    
    // Set viewport to desktop size
    await page.setViewport({
      width: 1920,
      height: 1080
    });

    // Enable console logging
    page.on('console', msg => console.log('Browser console:', msg.text()));

    // Test 1: Navigate and screenshot login page
    console.log('\n=== Test 1: Navigate and Screenshot ===');
    try {
      console.log('Navigating to http://localhost:5651/auth/login...');
      const response = await page.goto('http://localhost:5651/auth/login', {
        waitUntil: 'networkidle2',
        timeout: 30000
      });

      console.log(`Page loaded with status: ${response.status()}`);
      
      // Wait for content to load
      await page.waitForTimeout(3000);

      // Take full page screenshot
      const screenshotPath = path.join(screenshotsDir, `login-fullpage-${timestamp}.png`);
      await page.screenshot({
        path: screenshotPath,
        fullPage: true
      });
      console.log(`Screenshot saved: ${screenshotPath}`);
      testResults.screenshots.push(screenshotPath);

      // Verify Google OAuth button
      console.log('Checking for Google OAuth button...');
      const googleOAuthCheck = await page.evaluate(() => {
        const results = {
          found: false,
          text: '',
          visible: false,
          position: null,
          allButtons: []
        };

        // Get all buttons and links
        const elements = document.querySelectorAll('button, a, div[role="button"], span[role="button"]');
        
        elements.forEach(elem => {
          const text = elem.textContent?.trim() || '';
          const ariaLabel = elem.getAttribute('aria-label') || '';
          const className = elem.className || '';
          const id = elem.id || '';
          
          // Store all buttons for debugging
          if (text) {
            results.allButtons.push({
              tag: elem.tagName,
              text: text.substring(0, 50),
              className: className.substring(0, 50),
              id: id
            });
          }
          
          // Check for Google OAuth
          if (text.toLowerCase().includes('google') || 
              ariaLabel.toLowerCase().includes('google') || 
              className.toLowerCase().includes('google') ||
              id.toLowerCase().includes('google')) {
            results.found = true;
            results.text = text;
            const rect = elem.getBoundingClientRect();
            results.visible = rect.width > 0 && rect.height > 0;
            results.position = { x: rect.x, y: rect.y, width: rect.width, height: rect.height };
          }
        });

        return results;
      });

      console.log('Google OAuth check results:');
      console.log('- Found:', googleOAuthCheck.found);
      console.log('- Visible:', googleOAuthCheck.visible);
      console.log('- Text:', googleOAuthCheck.text);
      console.log('- All buttons found:', googleOAuthCheck.allButtons.length);
      googleOAuthCheck.allButtons.forEach(btn => {
        console.log(`  - ${btn.tag}: "${btn.text}"`);
      });

      testResults.tests.push({
        name: 'Google OAuth Button Visibility',
        passed: googleOAuthCheck.found && googleOAuthCheck.visible,
        details: googleOAuthCheck
      });

    } catch (error) {
      console.error('Test 1 failed:', error.message);
      testResults.tests.push({
        name: 'Navigate and Screenshot',
        passed: false,
        error: error.message
      });
    }

    // Test 2: Test form interaction - Click Create Account tab
    console.log('\n=== Test 2: Form Interaction - Tab Switching ===');
    try {
      // First, analyze the page structure
      const pageStructure = await page.evaluate(() => {
        const structure = {
          forms: [],
          tabs: [],
          buttons: [],
          links: []
        };

        // Find all forms
        document.querySelectorAll('form').forEach(form => {
          structure.forms.push({
            id: form.id,
            className: form.className,
            action: form.action
          });
        });

        // Find potential tabs
        const tabSelectors = ['[role="tab"]', '.tab', 'button[data-tab]', 'a[data-tab]', '.nav-link', '.tab-link'];
        tabSelectors.forEach(selector => {
          document.querySelectorAll(selector).forEach(tab => {
            structure.tabs.push({
              text: tab.textContent?.trim(),
              role: tab.getAttribute('role'),
              className: tab.className,
              selector: selector
            });
          });
        });

        // Find all buttons
        document.querySelectorAll('button').forEach(btn => {
          structure.buttons.push({
            text: btn.textContent?.trim(),
            type: btn.type,
            className: btn.className
          });
        });

        // Find all links
        document.querySelectorAll('a').forEach(link => {
          if (link.textContent?.trim()) {
            structure.links.push({
              text: link.textContent.trim(),
              href: link.href,
              className: link.className
            });
          }
        });

        return structure;
      });

      console.log('Page structure analysis:');
      console.log('- Forms:', pageStructure.forms.length);
      console.log('- Potential tabs:', pageStructure.tabs.length);
      console.log('- Buttons:', pageStructure.buttons.length);
      console.log('- Links:', pageStructure.links.length);

      // Try to find and click Create Account
      const createAccountFound = await page.evaluate(() => {
        const patterns = ['Create Account', 'Sign Up', 'Register', 'New Account', 'Create an account'];
        
        // Check all clickable elements
        const allElements = document.querySelectorAll('button, a, span, div[role="button"], [role="tab"]');
        
        for (const elem of allElements) {
          const text = elem.textContent?.trim() || '';
          for (const pattern of patterns) {
            if (text.toLowerCase().includes(pattern.toLowerCase())) {
              elem.click();
              return { found: true, text: text, element: elem.tagName };
            }
          }
        }
        
        return { found: false };
      });

      if (createAccountFound.found) {
        console.log(`Clicked on "${createAccountFound.text}" (${createAccountFound.element})`);
        await page.waitForTimeout(2000);
        
        // Take screenshot after clicking
        const regScreenshotPath = path.join(screenshotsDir, `registration-form-${timestamp}.png`);
        await page.screenshot({
          path: regScreenshotPath,
          fullPage: true
        });
        console.log(`Registration form screenshot saved: ${regScreenshotPath}`);
        testResults.screenshots.push(regScreenshotPath);

        // Analyze the registration form
        const registrationAnalysis = await page.evaluate(() => {
          const analysis = {
            formFields: [],
            hasChanged: false
          };

          // Get all input fields
          document.querySelectorAll('input').forEach(input => {
            analysis.formFields.push({
              type: input.type,
              name: input.name,
              id: input.id,
              placeholder: input.placeholder,
              required: input.required,
              visible: window.getComputedStyle(input).display !== 'none'
            });
          });

          // Check if we have typical registration fields
          const hasNameField = analysis.formFields.some(f => 
            f.name?.includes('name') || f.id?.includes('name') || f.placeholder?.toLowerCase().includes('name')
          );
          const hasConfirmPassword = analysis.formFields.some(f => 
            f.name?.includes('confirm') || f.id?.includes('confirm') || f.placeholder?.toLowerCase().includes('confirm')
          );

          analysis.hasChanged = hasNameField || hasConfirmPassword;

          return analysis;
        });

        console.log('Registration form analysis:', registrationAnalysis);

        testResults.tests.push({
          name: 'Tab Switching to Registration',
          passed: createAccountFound.found && registrationAnalysis.hasChanged,
          details: { createAccountFound, registrationAnalysis }
        });

      } else {
        console.log('Create Account option not found');
        console.log('Available elements:', pageStructure);
        
        testResults.tests.push({
          name: 'Tab Switching',
          passed: false,
          details: { message: 'Create Account option not found', pageStructure }
        });
      }

    } catch (error) {
      console.error('Test 2 failed:', error.message);
      testResults.tests.push({
        name: 'Form Interaction',
        passed: false,
        error: error.message
      });
    }

    // Test 3: Accessibility Audit
    console.log('\n=== Test 3: Accessibility Audit ===');
    try {
      const accessibilityResults = await page.evaluate(() => {
        const results = {
          formLabels: { total: 0, labeled: 0, unlabeled: [] },
          ariaAttributes: { total: 0, elements: [] },
          keyboardNavigation: { focusableElements: 0, tabIndexIssues: [] },
          altTexts: { images: 0, withAlt: 0, withoutAlt: [] }
        };

        // Check form labels
        const inputs = document.querySelectorAll('input:not([type="hidden"]), textarea, select');
        results.formLabels.total = inputs.length;
        
        inputs.forEach(input => {
          const id = input.id;
          const hasLabel = id && document.querySelector(`label[for="${id}"]`);
          const hasAriaLabel = input.getAttribute('aria-label');
          const hasAriaLabelledBy = input.getAttribute('aria-labelledby');
          const hasPlaceholder = input.placeholder;
          
          if (hasLabel || hasAriaLabel || hasAriaLabelledBy) {
            results.formLabels.labeled++;
          } else {
            results.formLabels.unlabeled.push({
              type: input.type,
              name: input.name,
              id: input.id,
              hasPlaceholder: !!hasPlaceholder
            });
          }
        });

        // Check ARIA attributes
        const elementsWithAria = document.querySelectorAll('[aria-label], [aria-labelledby], [aria-describedby], [role]');
        results.ariaAttributes.total = elementsWithAria.length;
        
        // Sample first 10 ARIA elements
        Array.from(elementsWithAria).slice(0, 10).forEach(elem => {
          results.ariaAttributes.elements.push({
            tag: elem.tagName,
            role: elem.getAttribute('role'),
            ariaLabel: elem.getAttribute('aria-label')?.substring(0, 50)
          });
        });

        // Check keyboard navigation
        const focusableElements = document.querySelectorAll('a[href], button, input:not([type="hidden"]), textarea, select, [tabindex]');
        results.keyboardNavigation.focusableElements = focusableElements.length;
        
        focusableElements.forEach(elem => {
          const tabIndex = elem.getAttribute('tabindex');
          if (tabIndex && parseInt(tabIndex) < 0) {
            results.keyboardNavigation.tabIndexIssues.push({
              element: elem.tagName,
              tabIndex: tabIndex
            });
          }
        });

        // Check images
        const images = document.querySelectorAll('img');
        results.altTexts.images = images.length;
        
        images.forEach(img => {
          if (img.alt) {
            results.altTexts.withAlt++;
          } else {
            results.altTexts.withoutAlt.push({
              src: img.src?.substring(0, 50)
            });
          }
        });

        return results;
      });

      console.log('Accessibility Audit Results:');
      console.log(`- Form labels: ${accessibilityResults.formLabels.labeled}/${accessibilityResults.formLabels.total} labeled`);
      console.log(`- ARIA attributes: ${accessibilityResults.ariaAttributes.total} elements with ARIA`);
      console.log(`- Focusable elements: ${accessibilityResults.keyboardNavigation.focusableElements}`);
      console.log(`- Tab index issues: ${accessibilityResults.keyboardNavigation.tabIndexIssues.length}`);
      console.log(`- Images: ${accessibilityResults.altTexts.withAlt}/${accessibilityResults.altTexts.images} with alt text`);

      const accessibilityPassed = 
        accessibilityResults.formLabels.unlabeled.length === 0 && 
        accessibilityResults.keyboardNavigation.tabIndexIssues.length === 0;

      testResults.tests.push({
        name: 'Accessibility Audit',
        passed: accessibilityPassed,
        details: accessibilityResults
      });

    } catch (error) {
      console.error('Test 3 failed:', error.message);
      testResults.tests.push({
        name: 'Accessibility Audit',
        passed: false,
        error: error.message
      });
    }

    // Test 4: Error States
    console.log('\n=== Test 4: Error States Testing ===');
    try {
      // First check current form state
      const initialState = await page.evaluate(() => {
        const forms = document.querySelectorAll('form');
        const inputs = document.querySelectorAll('input:not([type="hidden"])');
        return {
          formCount: forms.length,
          inputCount: inputs.length,
          inputTypes: Array.from(inputs).map(i => ({ type: i.type, name: i.name, value: i.value }))
        };
      });
      console.log('Initial form state:', initialState);

      // Try to submit empty form
      const submitAttempt = await page.evaluate(() => {
        const results = {
          formFound: false,
          submitButtonFound: false,
          submitted: false
        };

        // Find form
        const form = document.querySelector('form');
        if (form) {
          results.formFound = true;
          
          // Find submit button
          const submitButton = form.querySelector('button[type="submit"]') || 
                             form.querySelector('button:not([type="button"])') ||
                             document.querySelector('button[type="submit"]');
          
          if (submitButton) {
            results.submitButtonFound = true;
            results.buttonText = submitButton.textContent?.trim();
            submitButton.click();
            results.submitted = true;
          }
        }

        return results;
      });

      console.log('Submit attempt:', submitAttempt);
      
      // Wait for validation
      await page.waitForTimeout(2000);

      // Check for validation messages
      const errorState = await page.evaluate(() => {
        const results = {
          browserValidation: [],
          customErrors: [],
          inputStates: []
        };

        // Check HTML5 validation
        document.querySelectorAll('input').forEach(input => {
          if (input.validity && !input.validity.valid) {
            results.browserValidation.push({
              field: input.name || input.id,
              validationMessage: input.validationMessage,
              validity: {
                valueMissing: input.validity.valueMissing,
                typeMismatch: input.validity.typeMismatch,
                patternMismatch: input.validity.patternMismatch
              }
            });
          }

          // Check input visual state
          const styles = window.getComputedStyle(input);
          const classes = Array.from(input.classList);
          results.inputStates.push({
            field: input.name || input.id,
            borderColor: styles.borderColor,
            hasErrorClass: classes.some(c => c.includes('error') || c.includes('invalid'))
          });
        });

        // Look for custom error messages
        const errorSelectors = ['.error', '.error-message', '.invalid-feedback', '[role="alert"]', '.text-danger'];
        errorSelectors.forEach(selector => {
          document.querySelectorAll(selector).forEach(elem => {
            if (elem.textContent?.trim() && window.getComputedStyle(elem).display !== 'none') {
              results.customErrors.push({
                text: elem.textContent.trim(),
                selector: selector
              });
            }
          });
        });

        return results;
      });

      console.log('Error state analysis:', errorState);

      // Take error state screenshot
      const errorScreenshotPath = path.join(screenshotsDir, `login-error-state-${timestamp}.png`);
      await page.screenshot({
        path: errorScreenshotPath,
        fullPage: true
      });
      console.log(`Error state screenshot saved: ${errorScreenshotPath}`);
      testResults.screenshots.push(errorScreenshotPath);

      const hasValidation = errorState.browserValidation.length > 0 || errorState.customErrors.length > 0;

      testResults.tests.push({
        name: 'Error States Testing',
        passed: hasValidation,
        details: errorState
      });

    } catch (error) {
      console.error('Test 4 failed:', error.message);
      testResults.tests.push({
        name: 'Error States Testing',
        passed: false,
        error: error.message
      });
    }

    // Test 5: UI/UX Analysis
    console.log('\n=== Test 5: UI/UX Analysis ===');
    try {
      const uiAnalysis = await page.evaluate(() => {
        const results = {
          layout: {},
          colorScheme: {},
          typography: {},
          spacing: {},
          issues: []
        };

        // Find main container
        const containers = ['[class*="auth"]', '[class*="login"]', 'form', '.card', '.container'];
        let mainContainer = null;
        for (const selector of containers) {
          const elem = document.querySelector(selector);
          if (elem) {
            mainContainer = elem;
            break;
          }
        }

        if (mainContainer) {
          const rect = mainContainer.getBoundingClientRect();
          const styles = window.getComputedStyle(mainContainer);
          
          results.layout = {
            width: rect.width,
            height: rect.height,
            centered: Math.abs(rect.left - (window.innerWidth - rect.right)) < 50,
            padding: styles.padding,
            margin: styles.margin
          };
        }

        // Analyze color scheme
        const bgColor = window.getComputedStyle(document.body).backgroundColor;
        results.colorScheme.background = bgColor;
        
        // Check buttons
        const buttons = document.querySelectorAll('button');
        if (buttons.length > 0) {
          const btnStyles = window.getComputedStyle(buttons[0]);
          results.colorScheme.primaryButton = {
            background: btnStyles.backgroundColor,
            color: btnStyles.color
          };
        }

        // Typography
        const headings = document.querySelectorAll('h1, h2, h3');
        results.typography.headings = Array.from(headings).map(h => ({
          tag: h.tagName,
          fontSize: window.getComputedStyle(h).fontSize,
          fontWeight: window.getComputedStyle(h).fontWeight
        }));

        // Check spacing consistency
        const formElements = document.querySelectorAll('input, button, .form-group, .mb-3, .mb-4');
        const margins = [];
        formElements.forEach(elem => {
          const margin = window.getComputedStyle(elem).marginBottom;
          if (margin && margin !== '0px') {
            margins.push(parseFloat(margin));
          }
        });

        if (margins.length > 0) {
          const uniqueMargins = [...new Set(margins)];
          results.spacing.uniqueMargins = uniqueMargins;
          results.spacing.consistent = uniqueMargins.length <= 3;
        }

        // Common UI issues
        // Check button sizes
        buttons.forEach(btn => {
          const height = parseFloat(window.getComputedStyle(btn).height);
          if (height < 44) {
            results.issues.push(`Button "${btn.textContent?.trim()}" is too small for touch targets (${height}px < 44px)`);
          }
        });

        // Check input sizes
        const inputs = document.querySelectorAll('input');
        inputs.forEach(input => {
          const height = parseFloat(window.getComputedStyle(input).height);
          if (height < 40) {
            results.issues.push(`Input field is too small (${height}px < 40px)`);
          }
        });

        return results;
      });

      console.log('UI/UX Analysis:', JSON.stringify(uiAnalysis, null, 2));

      testResults.tests.push({
        name: 'UI/UX Analysis',
        passed: uiAnalysis.issues.length === 0,
        details: uiAnalysis
      });

    } catch (error) {
      console.error('Test 5 failed:', error.message);
      testResults.tests.push({
        name: 'UI/UX Analysis',
        passed: false,
        error: error.message
      });
    }

    // Generate final report
    const reportPath = path.join(__dirname, `login-mcp-test-report-${timestamp}.md`);
    const report = generateDetailedReport(testResults);
    fs.writeFileSync(reportPath, report);
    console.log(`\nTest report saved: ${reportPath}`);

    await browser.close();
    console.log('\nAll tests completed!');

  } catch (error) {
    console.error('Fatal error:', error);
    console.error('Stack trace:', error.stack);
  }
}

function generateDetailedReport(testResults) {
  let report = `# Login Page Test Report\n\n`;
  report += `**Test Date:** ${new Date().toLocaleString()}\n`;
  report += `**Timestamp:** ${testResults.timestamp}\n\n`;
  
  // Summary
  report += `## Executive Summary\n\n`;
  const totalTests = testResults.tests.length;
  const passedTests = testResults.tests.filter(t => t.passed).length;
  const failedTests = totalTests - passedTests;
  
  report += `- **Total Tests:** ${totalTests}\n`;
  report += `- **Passed:** ${passedTests}\n`;
  report += `- **Failed:** ${failedTests}\n`;
  report += `- **Success Rate:** ${((passedTests / totalTests) * 100).toFixed(2)}%\n\n`;
  
  // Screenshots
  if (testResults.screenshots.length > 0) {
    report += `## Screenshots Captured\n\n`;
    testResults.screenshots.forEach(screenshot => {
      report += `- ${path.basename(screenshot)}\n`;
    });
    report += `\n`;
  }
  
  // Detailed Results
  report += `## Detailed Test Results\n\n`;
  
  testResults.tests.forEach((test, index) => {
    report += `### ${index + 1}. ${test.name}\n\n`;
    report += `**Status:** ${test.passed ? '✅ PASSED' : '❌ FAILED'}\n\n`;
    
    if (test.error) {
      report += `**Error:** ${test.error}\n\n`;
    }
    
    if (test.details) {
      report += `**Details:**\n`;
      report += `\`\`\`json\n${JSON.stringify(test.details, null, 2)}\n\`\`\`\n\n`;
    }
  });
  
  // Issues and Recommendations
  report += `## Issues Found and Recommendations\n\n`;
  
  let issueCount = 1;
  
  // Check each test for specific issues
  testResults.tests.forEach(test => {
    if (!test.passed) {
      report += `### Issue ${issueCount}: ${test.name} Failed\n\n`;
      
      if (test.name === 'Google OAuth Button Visibility') {
        report += `**Problem:** Google OAuth button was not found or not visible on the login page.\n\n`;
        report += `**Recommendation:** \n`;
        report += `- Ensure the Google OAuth integration is properly configured\n`;
        report += `- Verify the button is visible and properly styled\n`;
        report += `- Check if the button text includes "Google" for easy identification\n\n`;
      }
      
      if (test.name === 'Tab Switching') {
        report += `**Problem:** Could not find or switch to Create Account/Registration tab.\n\n`;
        report += `**Recommendation:** \n`;
        report += `- Implement clear tab navigation between Login and Registration\n`;
        report += `- Use proper ARIA roles for tab accessibility\n`;
        report += `- Ensure tabs are clearly labeled\n\n`;
      }
      
      if (test.name === 'Accessibility Audit' && test.details) {
        if (test.details.formLabels?.unlabeled?.length > 0) {
          report += `**Problem:** Form inputs without proper labels found.\n\n`;
          report += `**Recommendation:** \n`;
          report += `- Add \`<label>\` elements for all form inputs\n`;
          report += `- Use \`aria-label\` or \`aria-labelledby\` as alternatives\n`;
          report += `- Ensure all inputs are properly described for screen readers\n\n`;
        }
      }
      
      if (test.name === 'Error States Testing') {
        report += `**Problem:** Form validation not properly implemented.\n\n`;
        report += `**Recommendation:** \n`;
        report += `- Add HTML5 validation attributes (required, type="email", etc.)\n`;
        report += `- Implement custom validation messages\n`;
        report += `- Show clear error states on invalid inputs\n\n`;
      }
      
      if (test.name === 'UI/UX Analysis' && test.details?.issues?.length > 0) {
        report += `**Problems:** \n`;
        test.details.issues.forEach(issue => {
          report += `- ${issue}\n`;
        });
        report += `\n**Recommendations:** \n`;
        report += `- Ensure all interactive elements meet minimum size requirements (44px for touch)\n`;
        report += `- Maintain consistent spacing throughout the form\n`;
        report += `- Use a clear visual hierarchy\n\n`;
      }
      
      issueCount++;
    }
  });
  
  // Overall recommendations
  report += `## Overall Recommendations\n\n`;
  report += `1. **Accessibility**: Ensure all form elements have proper labels and ARIA attributes\n`;
  report += `2. **User Experience**: Implement clear visual feedback for all interactions\n`;
  report += `3. **Error Handling**: Add comprehensive form validation with helpful error messages\n`;
  report += `4. **Mobile Responsiveness**: Test and optimize for various screen sizes\n`;
  report += `5. **Performance**: Optimize page load time and reduce blocking resources\n`;
  
  return report;
}

// Run the test
testLoginPage().catch(console.error);
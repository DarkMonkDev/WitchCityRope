const puppeteer = require('puppeteer');
const path = require('path');
const fs = require('fs');

async function testLoginPageComprehensive() {
  console.log('Starting comprehensive login page test...');
  
  // Windows Chrome paths
  const possibleChromePaths = [
    '/mnt/c/Program Files/Google/Chrome/Application/chrome.exe',
    '/mnt/c/Program Files (x86)/Google/Chrome/Application/chrome.exe',
    '/mnt/c/Users/chad/AppData/Local/Google/Chrome/Application/chrome.exe'
  ];

  let chromePath = null;
  for (const p of possibleChromePaths) {
    if (fs.existsSync(p)) {
      chromePath = p;
      break;
    }
  }

  const launchOptions = {
    headless: true, // Run in headless mode
    args: [
      '--no-sandbox',
      '--disable-setuid-sandbox',
      '--disable-dev-shm-usage',
      '--disable-gpu'
    ]
  };

  if (chromePath) {
    console.log(`Found Chrome at: ${chromePath}`);
    launchOptions.executablePath = chromePath;
  }

  // Create screenshots directory
  const screenshotsDir = path.join(__dirname, 'screenshots');
  if (!fs.existsSync(screenshotsDir)) {
    fs.mkdirSync(screenshotsDir);
  }

  const timestamp = new Date().toISOString().replace(/:/g, '-').split('.')[0];
  const testResults = {
    timestamp,
    tests: []
  };

  try {
    const browser = await puppeteer.launch(launchOptions);
    const page = await browser.newPage();
    
    // Set viewport to desktop size
    await page.setViewport({
      width: 1920,
      height: 1080
    });

    // Test 1: Navigate and screenshot login page
    console.log('\n=== Test 1: Navigate and Screenshot ===');
    try {
      const response = await page.goto('http://localhost:5651/auth/login', {
        waitUntil: 'networkidle2',
        timeout: 30000
      });

      console.log(`Page loaded with status: ${response.status()}`);
      await page.waitForTimeout(2000);

      // Take full page screenshot
      const screenshotPath = path.join(screenshotsDir, `login-fullpage-${timestamp}.png`);
      await page.screenshot({
        path: screenshotPath,
        fullPage: true
      });
      console.log(`Screenshot saved: ${screenshotPath}`);

      // Verify Google OAuth button
      const googleOAuthCheck = await page.evaluate(() => {
        const results = {
          found: false,
          text: '',
          visible: false,
          position: null
        };

        const selectors = [
          'button:contains("Google")',
          'a:contains("Google")',
          '[aria-label*="Google"]',
          '.google-login',
          '#google-login',
          'button[class*="google"]',
          'a[class*="google"]'
        ];

        // Check all possible selectors
        for (const selector of selectors) {
          try {
            const elements = document.querySelectorAll('button, a, div[role="button"]');
            for (const elem of elements) {
              const text = elem.textContent?.trim() || '';
              const ariaLabel = elem.getAttribute('aria-label') || '';
              const className = elem.className || '';
              
              if (text.toLowerCase().includes('google') || 
                  ariaLabel.toLowerCase().includes('google') || 
                  className.toLowerCase().includes('google')) {
                results.found = true;
                results.text = text;
                const rect = elem.getBoundingClientRect();
                results.visible = rect.width > 0 && rect.height > 0;
                results.position = { x: rect.x, y: rect.y, width: rect.width, height: rect.height };
                break;
              }
            }
            if (results.found) break;
          } catch (e) {}
        }

        return results;
      });

      testResults.tests.push({
        name: 'Google OAuth Button Visibility',
        passed: googleOAuthCheck.found && googleOAuthCheck.visible,
        details: googleOAuthCheck
      });

      console.log('Google OAuth button found:', googleOAuthCheck.found);
      console.log('Google OAuth button visible:', googleOAuthCheck.visible);
      if (googleOAuthCheck.text) {
        console.log('Button text:', googleOAuthCheck.text);
      }

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
      // First check if tabs exist
      const tabsInfo = await page.evaluate(() => {
        const tabs = document.querySelectorAll('[role="tab"], .tab, button[data-tab], a[data-tab]');
        const tabInfo = [];
        tabs.forEach(tab => {
          tabInfo.push({
            text: tab.textContent?.trim(),
            role: tab.getAttribute('role'),
            ariaSelected: tab.getAttribute('aria-selected'),
            className: tab.className
          });
        });
        return tabInfo;
      });

      console.log('Found tabs:', tabsInfo.length);
      tabsInfo.forEach(tab => console.log('- Tab:', tab.text));

      // Try to find and click Create Account tab
      const createAccountClicked = await page.evaluate(() => {
        const patterns = ['Create Account', 'Sign Up', 'Register', 'New Account'];
        let clicked = false;
        
        const clickableElements = document.querySelectorAll('button, a, div[role="tab"], span');
        for (const elem of clickableElements) {
          const text = elem.textContent?.trim() || '';
          for (const pattern of patterns) {
            if (text.toLowerCase().includes(pattern.toLowerCase())) {
              elem.click();
              clicked = true;
              return { clicked: true, text: text };
            }
          }
        }
        
        return { clicked: false, text: '' };
      });

      if (createAccountClicked.clicked) {
        console.log(`Clicked on: ${createAccountClicked.text}`);
        await page.waitForTimeout(2000);
        
        // Take screenshot of registration form
        const regScreenshotPath = path.join(screenshotsDir, `registration-form-${timestamp}.png`);
        await page.screenshot({
          path: regScreenshotPath,
          fullPage: true
        });
        console.log(`Registration form screenshot saved: ${regScreenshotPath}`);

        // Check if form changed
        const registrationFormCheck = await page.evaluate(() => {
          const results = {
            hasNameField: false,
            hasEmailField: false,
            hasPasswordField: false,
            hasConfirmPasswordField: false,
            hasSubmitButton: false,
            formFields: []
          };

          // Check for common registration fields
          const inputs = document.querySelectorAll('input');
          inputs.forEach(input => {
            const type = input.type;
            const name = input.name?.toLowerCase() || '';
            const id = input.id?.toLowerCase() || '';
            const placeholder = input.placeholder?.toLowerCase() || '';
            
            results.formFields.push({
              type,
              name: input.name,
              id: input.id,
              placeholder: input.placeholder
            });

            if (name.includes('name') || id.includes('name') || placeholder.includes('name')) {
              results.hasNameField = true;
            }
            if (type === 'email' || name.includes('email') || id.includes('email')) {
              results.hasEmailField = true;
            }
            if (type === 'password' && !name.includes('confirm') && !id.includes('confirm')) {
              results.hasPasswordField = true;
            }
            if ((type === 'password' && (name.includes('confirm') || id.includes('confirm'))) ||
                placeholder.includes('confirm')) {
              results.hasConfirmPasswordField = true;
            }
          });

          // Check for submit button
          const submitButtons = document.querySelectorAll('button[type="submit"], button');
          submitButtons.forEach(btn => {
            const text = btn.textContent?.toLowerCase() || '';
            if (text.includes('sign up') || text.includes('register') || text.includes('create')) {
              results.hasSubmitButton = true;
            }
          });

          return results;
        });

        testResults.tests.push({
          name: 'Tab Switching to Registration',
          passed: createAccountClicked.clicked,
          details: registrationFormCheck
        });

        console.log('Registration form analysis:', registrationFormCheck);

        // Switch back to login tab
        await page.evaluate(() => {
          const patterns = ['Sign In', 'Log In', 'Login', 'Existing User'];
          const clickableElements = document.querySelectorAll('button, a, div[role="tab"], span');
          for (const elem of clickableElements) {
            const text = elem.textContent?.trim() || '';
            for (const pattern of patterns) {
              if (text.toLowerCase().includes(pattern.toLowerCase())) {
                elem.click();
                return;
              }
            }
          }
        });
        await page.waitForTimeout(1000);

      } else {
        console.log('Create Account tab not found');
        testResults.tests.push({
          name: 'Tab Switching',
          passed: false,
          details: { message: 'Create Account tab not found' }
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
          ariaAttributes: [],
          keyboardNavigation: { focusableElements: 0, tabIndexIssues: [] },
          colorContrast: { issues: [] },
          altTexts: { images: 0, withAlt: 0, withoutAlt: [] }
        };

        // Check form labels
        const inputs = document.querySelectorAll('input, textarea, select');
        results.formLabels.total = inputs.length;
        
        inputs.forEach(input => {
          const id = input.id;
          const hasLabel = id && document.querySelector(`label[for="${id}"]`);
          const hasAriaLabel = input.getAttribute('aria-label');
          const hasAriaLabelledBy = input.getAttribute('aria-labelledby');
          
          if (hasLabel || hasAriaLabel || hasAriaLabelledBy) {
            results.formLabels.labeled++;
          } else {
            results.formLabels.unlabeled.push({
              type: input.type,
              name: input.name,
              id: input.id
            });
          }
        });

        // Check ARIA attributes
        const elementsWithAria = document.querySelectorAll('[aria-label], [aria-labelledby], [aria-describedby], [role]');
        elementsWithAria.forEach(elem => {
          results.ariaAttributes.push({
            tag: elem.tagName,
            role: elem.getAttribute('role'),
            ariaLabel: elem.getAttribute('aria-label'),
            ariaLabelledBy: elem.getAttribute('aria-labelledby'),
            ariaDescribedBy: elem.getAttribute('aria-describedby')
          });
        });

        // Check keyboard navigation
        const focusableElements = document.querySelectorAll('a, button, input, textarea, select, [tabindex]');
        results.keyboardNavigation.focusableElements = focusableElements.length;
        
        focusableElements.forEach(elem => {
          const tabIndex = elem.getAttribute('tabindex');
          if (tabIndex && parseInt(tabIndex) < 0) {
            results.keyboardNavigation.tabIndexIssues.push({
              element: elem.tagName,
              tabIndex: tabIndex,
              text: elem.textContent?.trim().substring(0, 50)
            });
          }
        });

        // Basic color contrast check (simplified)
        const textElements = document.querySelectorAll('p, span, label, button, a, h1, h2, h3, h4, h5, h6');
        textElements.forEach(elem => {
          const styles = window.getComputedStyle(elem);
          const color = styles.color;
          const bgColor = styles.backgroundColor;
          
          // This is a simplified check - real contrast checking requires color parsing
          if (color && bgColor && bgColor !== 'rgba(0, 0, 0, 0)') {
            results.colorContrast.issues.push({
              element: elem.tagName,
              text: elem.textContent?.trim().substring(0, 30),
              color: color,
              backgroundColor: bgColor
            });
          }
        });

        // Check alt texts
        const images = document.querySelectorAll('img');
        results.altTexts.images = images.length;
        
        images.forEach(img => {
          if (img.alt) {
            results.altTexts.withAlt++;
          } else {
            results.altTexts.withoutAlt.push({
              src: img.src,
              className: img.className
            });
          }
        });

        return results;
      });

      console.log('Accessibility Audit Results:');
      console.log(`- Form labels: ${accessibilityResults.formLabels.labeled}/${accessibilityResults.formLabels.total} labeled`);
      if (accessibilityResults.formLabels.unlabeled.length > 0) {
        console.log('  Unlabeled inputs:', accessibilityResults.formLabels.unlabeled);
      }
      console.log(`- ARIA attributes found: ${accessibilityResults.ariaAttributes.length}`);
      console.log(`- Focusable elements: ${accessibilityResults.keyboardNavigation.focusableElements}`);
      if (accessibilityResults.keyboardNavigation.tabIndexIssues.length > 0) {
        console.log('  TabIndex issues:', accessibilityResults.keyboardNavigation.tabIndexIssues);
      }
      console.log(`- Images: ${accessibilityResults.altTexts.withAlt}/${accessibilityResults.altTexts.images} with alt text`);

      testResults.tests.push({
        name: 'Accessibility Audit',
        passed: accessibilityResults.formLabels.unlabeled.length === 0 && 
                accessibilityResults.keyboardNavigation.tabIndexIssues.length === 0,
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
      // Try to submit empty form
      const submitResult = await page.evaluate(() => {
        const results = {
          formFound: false,
          submitted: false,
          validationMessages: [],
          errorElements: []
        };

        // Find the login form
        const forms = document.querySelectorAll('form');
        if (forms.length > 0) {
          results.formFound = true;
          const form = forms[0];
          
          // Try to find and click submit button
          const submitButton = form.querySelector('button[type="submit"]') || 
                             form.querySelector('button') ||
                             document.querySelector('button[type="submit"]');
          
          if (submitButton) {
            submitButton.click();
            results.submitted = true;
          }
        }

        return results;
      });

      console.log('Form submission attempt:', submitResult);
      
      // Wait for validation messages to appear
      await page.waitForTimeout(2000);

      // Check for validation messages
      const validationCheck = await page.evaluate(() => {
        const results = {
          validationMessages: [],
          errorElements: [],
          errorStyles: []
        };

        // Check HTML5 validation messages
        const inputs = document.querySelectorAll('input');
        inputs.forEach(input => {
          if (input.validationMessage) {
            results.validationMessages.push({
              field: input.name || input.id,
              message: input.validationMessage,
              validity: input.validity
            });
          }
        });

        // Check for error message elements
        const errorSelectors = [
          '.error', '.error-message', '.validation-error', 
          '.invalid-feedback', '.help-block', '[role="alert"]',
          '.text-danger', '.text-red', 'span[class*="error"]'
        ];
        
        errorSelectors.forEach(selector => {
          const errors = document.querySelectorAll(selector);
          errors.forEach(error => {
            if (error.textContent?.trim()) {
              results.errorElements.push({
                selector: selector,
                text: error.textContent.trim(),
                visible: window.getComputedStyle(error).display !== 'none'
              });
            }
          });
        });

        // Check for error styling on inputs
        inputs.forEach(input => {
          const styles = window.getComputedStyle(input);
          const borderColor = styles.borderColor;
          const classList = Array.from(input.classList);
          
          if (classList.some(c => c.includes('error') || c.includes('invalid')) ||
              borderColor.includes('rgb(255') || borderColor.includes('red')) {
            results.errorStyles.push({
              field: input.name || input.id,
              borderColor: borderColor,
              classes: classList.filter(c => c.includes('error') || c.includes('invalid'))
            });
          }
        });

        return results;
      });

      console.log('Validation check results:', validationCheck);

      // Take screenshot of error state
      const errorScreenshotPath = path.join(screenshotsDir, `login-error-state-${timestamp}.png`);
      await page.screenshot({
        path: errorScreenshotPath,
        fullPage: true
      });
      console.log(`Error state screenshot saved: ${errorScreenshotPath}`);

      testResults.tests.push({
        name: 'Error States Testing',
        passed: validationCheck.validationMessages.length > 0 || 
                validationCheck.errorElements.length > 0,
        details: validationCheck
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
      const uiuxAnalysis = await page.evaluate(() => {
        const results = {
          layout: {},
          responsiveness: {},
          visualHierarchy: {},
          userFlow: {},
          issues: []
        };

        // Check layout
        const authCard = document.querySelector('.auth-card, .login-card, .card, form');
        if (authCard) {
          const rect = authCard.getBoundingClientRect();
          results.layout = {
            centered: Math.abs(rect.left - (window.innerWidth - rect.right)) < 50,
            width: rect.width,
            height: rect.height,
            aspectRatio: rect.width / rect.height
          };
        }

        // Check visual hierarchy
        const headings = document.querySelectorAll('h1, h2, h3');
        results.visualHierarchy.headings = Array.from(headings).map(h => ({
          tag: h.tagName,
          text: h.textContent?.trim(),
          fontSize: window.getComputedStyle(h).fontSize
        }));

        // Check button styling consistency
        const buttons = document.querySelectorAll('button');
        const buttonStyles = Array.from(buttons).map(btn => ({
          text: btn.textContent?.trim(),
          backgroundColor: window.getComputedStyle(btn).backgroundColor,
          color: window.getComputedStyle(btn).color,
          padding: window.getComputedStyle(btn).padding,
          borderRadius: window.getComputedStyle(btn).borderRadius
        }));
        results.visualHierarchy.buttons = buttonStyles;

        // Check user flow indicators
        results.userFlow = {
          hasPasswordToggle: !!document.querySelector('[type="password"] ~ button, .password-toggle'),
          hasRememberMe: !!document.querySelector('input[type="checkbox"][name*="remember"]'),
          hasForgotPassword: !!Array.from(document.querySelectorAll('a')).find(a => 
            a.textContent?.toLowerCase().includes('forgot')),
          hasSignUpLink: !!Array.from(document.querySelectorAll('a, button')).find(a => 
            a.textContent?.toLowerCase().includes('sign up') || 
            a.textContent?.toLowerCase().includes('create account'))
        };

        // Common UI/UX issues check
        // Check input field heights
        const inputs = document.querySelectorAll('input');
        const inputHeights = Array.from(inputs).map(i => parseFloat(window.getComputedStyle(i).height));
        const avgHeight = inputHeights.reduce((a, b) => a + b, 0) / inputHeights.length;
        inputHeights.forEach((height, index) => {
          if (Math.abs(height - avgHeight) > 5) {
            results.issues.push(`Input field ${index} has inconsistent height: ${height}px vs avg ${avgHeight}px`);
          }
        });

        // Check for proper spacing
        const formElements = document.querySelectorAll('input, button, .form-group');
        formElements.forEach((elem, index) => {
          if (index > 0) {
            const prevElem = formElements[index - 1];
            const spacing = elem.getBoundingClientRect().top - prevElem.getBoundingClientRect().bottom;
            if (spacing < 8) {
              results.issues.push(`Insufficient spacing between form elements: ${spacing}px`);
            }
          }
        });

        return results;
      });

      console.log('UI/UX Analysis Results:', JSON.stringify(uiuxAnalysis, null, 2));

      testResults.tests.push({
        name: 'UI/UX Analysis',
        passed: uiuxAnalysis.issues.length === 0,
        details: uiuxAnalysis
      });

    } catch (error) {
      console.error('Test 5 failed:', error.message);
      testResults.tests.push({
        name: 'UI/UX Analysis',
        passed: false,
        error: error.message
      });
    }

    // Generate comprehensive report
    const reportPath = path.join(__dirname, `login-test-report-${timestamp}.md`);
    const report = generateReport(testResults);
    fs.writeFileSync(reportPath, report);
    console.log(`\nComprehensive test report saved: ${reportPath}`);

    await browser.close();
    console.log('\nAll tests completed!');

  } catch (error) {
    console.error('Fatal error:', error.message);
  }
}

function generateReport(testResults) {
  let report = `# Login Page Comprehensive Test Report\n\n`;
  report += `**Test Date:** ${new Date().toLocaleString()}\n\n`;
  report += `## Test Summary\n\n`;
  
  const totalTests = testResults.tests.length;
  const passedTests = testResults.tests.filter(t => t.passed).length;
  const failedTests = totalTests - passedTests;
  
  report += `- **Total Tests:** ${totalTests}\n`;
  report += `- **Passed:** ${passedTests}\n`;
  report += `- **Failed:** ${failedTests}\n`;
  report += `- **Success Rate:** ${((passedTests / totalTests) * 100).toFixed(2)}%\n\n`;
  
  report += `## Detailed Test Results\n\n`;
  
  testResults.tests.forEach((test, index) => {
    report += `### ${index + 1}. ${test.name}\n\n`;
    report += `**Status:** ${test.passed ? '✅ PASSED' : '❌ FAILED'}\n\n`;
    
    if (test.error) {
      report += `**Error:** ${test.error}\n\n`;
    }
    
    if (test.details) {
      report += `**Details:**\n\`\`\`json\n${JSON.stringify(test.details, null, 2)}\n\`\`\`\n\n`;
    }
  });
  
  report += `## Recommendations\n\n`;
  
  // Add specific recommendations based on test results
  const accessibilityTest = testResults.tests.find(t => t.name === 'Accessibility Audit');
  if (accessibilityTest && !accessibilityTest.passed) {
    report += `### Accessibility Improvements\n`;
    if (accessibilityTest.details?.formLabels?.unlabeled?.length > 0) {
      report += `- Add labels to the following form inputs: ${accessibilityTest.details.formLabels.unlabeled.map(u => u.type).join(', ')}\n`;
    }
    if (accessibilityTest.details?.keyboardNavigation?.tabIndexIssues?.length > 0) {
      report += `- Fix negative tabindex values on interactive elements\n`;
    }
    report += `\n`;
  }
  
  const uiuxTest = testResults.tests.find(t => t.name === 'UI/UX Analysis');
  if (uiuxTest && uiuxTest.details?.issues?.length > 0) {
    report += `### UI/UX Improvements\n`;
    uiuxTest.details.issues.forEach(issue => {
      report += `- ${issue}\n`;
    });
    report += `\n`;
  }
  
  const errorTest = testResults.tests.find(t => t.name === 'Error States Testing');
  if (errorTest && !errorTest.passed) {
    report += `### Error Handling\n`;
    report += `- Implement proper form validation with clear error messages\n`;
    report += `- Add visual indicators for invalid input fields\n\n`;
  }
  
  return report;
}

testLoginPageComprehensive().catch(console.error);
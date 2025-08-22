const puppeteer = require('puppeteer');
const path = require('path');
const fs = require('fs');

async function testLoginPage() {
  console.log('Starting login page OAuth test...');
  
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
    headless: true, // Run headless for faster execution
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

  try {
    const browser = await puppeteer.launch(launchOptions);
    
    // Test different viewport sizes
    const viewports = [
      { name: 'desktop', width: 1920, height: 1080 },
      { name: 'tablet', width: 768, height: 1024 },
      { name: 'mobile', width: 390, height: 844 }
    ];

    // Create screenshots directory
    const screenshotsDir = path.join(__dirname, 'screenshots');
    if (!fs.existsSync(screenshotsDir)) {
      fs.mkdirSync(screenshotsDir);
    }

    const timestamp = new Date().toISOString().replace(/:/g, '-').split('.')[0];

    for (const viewport of viewports) {
      console.log(`\nTesting ${viewport.name} view (${viewport.width}x${viewport.height})...`);
      
      const page = await browser.newPage();
      await page.setViewport({
        width: viewport.width,
        height: viewport.height
      });

      console.log('Navigating to login page...');
      
      try {
        const response = await page.goto('http://localhost:5651/auth/login', {
          waitUntil: 'networkidle2',
          timeout: 30000
        });

        console.log(`Page loaded with status: ${response.status()}`);
        
        // Wait for any dynamic content to load
        await page.waitForTimeout(3000);

        // Take screenshot
        const screenshotPath = path.join(screenshotsDir, `login-${viewport.name}-${timestamp}.png`);
        await page.screenshot({
          path: screenshotPath,
          fullPage: true
        });
        console.log(`Screenshot saved: ${screenshotPath}`);

        // Check for OAuth elements
        const oauthAnalysis = await page.evaluate(() => {
          const results = {
            hasGoogleButton: false,
            googleButtonText: '',
            otherProviders: [],
            loginFormFound: false,
            formElements: [],
            layoutInfo: {}
          };

          // Check for Google OAuth button
          const googlePatterns = ['google', 'Google', 'Sign in with Google', 'Continue with Google'];
          const buttons = document.querySelectorAll('button, a, div[role="button"]');
          
          buttons.forEach(btn => {
            const text = btn.textContent?.trim() || '';
            const ariaLabel = btn.getAttribute('aria-label') || '';
            const className = btn.className || '';
            
            googlePatterns.forEach(pattern => {
              if (text.includes(pattern) || ariaLabel.includes(pattern) || className.toLowerCase().includes('google')) {
                results.hasGoogleButton = true;
                results.googleButtonText = text;
              }
            });
            
            // Check for other OAuth providers
            const providers = ['Facebook', 'Microsoft', 'GitHub', 'Twitter', 'LinkedIn'];
            providers.forEach(provider => {
              if (text.includes(provider) || ariaLabel.includes(provider)) {
                results.otherProviders.push(provider);
              }
            });
          });

          // Check for login form
          const forms = document.querySelectorAll('form');
          results.loginFormFound = forms.length > 0;
          
          // Find form inputs
          const inputs = document.querySelectorAll('input[type="email"], input[type="text"], input[type="password"], input[id*="email"], input[id*="login"], input[id*="password"]');
          inputs.forEach(input => {
            results.formElements.push({
              type: input.type,
              id: input.id,
              name: input.name,
              placeholder: input.placeholder
            });
          });

          // Get layout info
          const authCard = document.querySelector('.auth-card');
          if (authCard) {
            const rect = authCard.getBoundingClientRect();
            results.layoutInfo = {
              hasAuthCard: true,
              cardWidth: rect.width,
              cardHeight: rect.height,
              cardPosition: { top: rect.top, left: rect.left }
            };
          }

          // Check for divider or separator elements
          const dividers = document.querySelectorAll('.divider, .separator, .or-divider, hr');
          results.hasDivider = dividers.length > 0;

          return results;
        });

        console.log('\nOAuth Analysis Results:');
        console.log('- Google OAuth button found:', oauthAnalysis.hasGoogleButton);
        if (oauthAnalysis.hasGoogleButton) {
          console.log('  Button text:', oauthAnalysis.googleButtonText);
        }
        console.log('- Other OAuth providers:', oauthAnalysis.otherProviders.length > 0 ? oauthAnalysis.otherProviders.join(', ') : 'None');
        console.log('- Login form found:', oauthAnalysis.loginFormFound);
        console.log('- Form elements:', oauthAnalysis.formElements.length);
        oauthAnalysis.formElements.forEach(elem => {
          console.log(`  - ${elem.type} field: ${elem.id || elem.name || 'unnamed'}`);
        });
        console.log('- Layout info:', JSON.stringify(oauthAnalysis.layoutInfo, null, 2));

        // Save HTML for analysis
        const html = await page.content();
        const htmlPath = path.join(screenshotsDir, `login-${viewport.name}-${timestamp}.html`);
        fs.writeFileSync(htmlPath, html);
        console.log(`HTML saved: ${htmlPath}`);

      } catch (navError) {
        console.error('Navigation error:', navError.message);
      }

      await page.close();
    }

    await browser.close();
    console.log('\nTest completed successfully!');

  } catch (error) {
    console.error('Error:', error.message);
  }
}

testLoginPage().catch(console.error);
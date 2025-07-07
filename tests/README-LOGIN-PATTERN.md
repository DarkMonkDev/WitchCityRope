# Working Login Pattern for Puppeteer Tests

## IMPORTANT: Use This Pattern for All Login Tests

This login pattern has been proven to work reliably with the WitchCityRope Blazor application.

### The Working Pattern

```javascript
const puppeteer = require('puppeteer');

async function loginToWitchCityRope(page, email, password) {
  // Navigate to login page
  await page.goto('http://localhost:5651/auth/login', {
    waitUntil: 'networkidle2',
    timeout: 30000
  });

  // Fill email field - use multiple selectors for reliability
  await page.waitForSelector('input[type="email"], input[name="email"], input[id="email"], input[placeholder*="email"]', { timeout: 10000 });
  await page.type('input[type="email"], input[name="email"], input[id="email"], input[placeholder*="email"]', email);

  // Fill password field
  await page.waitForSelector('input[type="password"]', { timeout: 10000 });
  await page.type('input[type="password"]', password);

  // Submit form using evaluate to find button by text content
  const submitClicked = await page.evaluate(() => {
    const buttons = document.querySelectorAll('button');
    for (const button of buttons) {
      const text = button.textContent?.toLowerCase() || '';
      if (button.type === 'submit' || text.includes('sign in') || text.includes('login')) {
        button.click();
        return true;
      }
    }
    
    const form = document.querySelector('form');
    if (form) {
      form.submit();
      return true;
    }
    
    return false;
  });

  if (submitClicked) {
    try {
      await page.waitForNavigation({ waitUntil: 'networkidle2', timeout: 30000 });
    } catch (navError) {
      console.log('Navigation timeout - checking current state...');
    }
  }

  return page.url();
}

// Example usage:
async function testWithLogin() {
  const browser = await puppeteer.launch({
    headless: false,
    args: ['--no-sandbox', '--disable-setuid-sandbox']
  });

  const page = await browser.newPage();
  
  // Login as admin
  const urlAfterLogin = await loginToWitchCityRope(page, 'admin@witchcityrope.com', 'Test123!');
  console.log('Logged in, current URL:', urlAfterLogin);
  
  // Now continue with your test...
  
  await browser.close();
}
```

### Why This Pattern Works

1. **Multiple Selectors**: The email field selector uses multiple fallbacks: `type="email"`, `name="email"`, `id="email"`, and `placeholder*="email"`
2. **Text-Based Button Finding**: Instead of relying on specific IDs or classes, it finds buttons by their text content
3. **Form Fallback**: If no button is found, it tries to submit the form directly
4. **Graceful Navigation Handling**: Catches navigation timeouts which can occur with Blazor apps

### What NOT to Do

❌ **DON'T** use simple selectors like `#Input_Email`
❌ **DON'T** assume specific button IDs or classes
❌ **DON'T** expect immediate navigation after form submission
❌ **DON'T** use exact text matches for buttons

### Test Accounts

- **Admin**: admin@witchcityrope.com / Test123!
- **Teacher**: teacher@witchcityrope.com / Test123!
- **Vetted Member**: vetted@witchcityrope.com / Test123!
- **General Member**: member@witchcityrope.com / Test123!

### Reference Implementation

See the full working implementation in: `/src/WitchCityRope.Web/screenshot-script/test-member-dashboard.js`

### Troubleshooting

If login still fails:
1. Check if the application is running on http://localhost:5651
2. Verify the login page loads correctly
3. Check browser console for JavaScript errors
4. Ensure the test accounts exist in the database
5. Try running with `headless: false` to see what's happening
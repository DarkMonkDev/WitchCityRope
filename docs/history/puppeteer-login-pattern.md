# WitchCityRope - Working Puppeteer Login Pattern

This is a proven working pattern for logging into the WitchCityRope Blazor application during UI tests. This pattern should be used for ALL Puppeteer tests that require login.

## The Working Login Code:

```javascript
// Navigate to login page
await page.goto('http://localhost:5651/identity/account/login', {
  waitUntil: 'networkidle2',
  timeout: 30000
});

// Fill email - use multiple selectors for reliability
await page.waitForSelector('input[type="email"], input[name="email"], input[id="email"], input[placeholder*="email"]', { timeout: 10000 });
await page.type('input[type="email"], input[name="email"], input[id="email"], input[placeholder*="email"]', 'admin@witchcityrope.com');

// Fill password
await page.waitForSelector('input[type="password"]', { timeout: 10000 });
await page.type('input[type="password"]', 'Test123!');

// Submit form using evaluate to find button by text
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
```

## Key Points:
- DO NOT use simple selectors like #Input_Email - they fail
- DO use multiple selector fallbacks (type, name, id, placeholder)
- DO NOT rely on exact button text
- DO use page.evaluate() to find buttons by text content
- DO handle navigation timeouts gracefully

## Test Accounts:
- Admin: admin@witchcityrope.com / Test123!
- Member: member@witchcityrope.com / Test123!
- Teacher: teacher@witchcityrope.com / Test123!
- Vetted: vetted@witchcityrope.com / Test123!

## Reference:
This pattern is from /src/WitchCityRope.Web/screenshot-script/test-member-dashboard.js which is a proven working test.
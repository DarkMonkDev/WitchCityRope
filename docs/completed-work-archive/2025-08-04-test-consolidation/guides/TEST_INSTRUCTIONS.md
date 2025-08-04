# Login Navigation Test Instructions

This test script verifies that the navigation menu properly updates after successful authentication.

## Setup

1. Make sure your application is running on http://localhost:5651

2. Install Playwright (if not already installed):
   ```bash
   npm install playwright
   # or using the test package.json:
   npm install --prefix . --package-lock-only --package=test-package.json
   ```

3. Run the test:
   ```bash
   node test-login-navigation.js
   ```

## What the test does:

1. Opens http://localhost:5651/identity/account/login
2. Takes a screenshot (saved as `screenshots/before-login.png`)
3. Fills in the login form with:
   - Email: admin@witchcityrope.com
   - Password: Test123!
4. Clicks the Sign In button
5. Waits for navigation to complete
6. Takes another screenshot (saved as `screenshots/after-login.png`)
7. Checks if the navigation menu shows user information instead of the login button

## Results

The test will output:
- Whether user indicators were found in the navigation
- Whether the login button is still visible
- The actual navigation content for debugging

Screenshots will be saved in the `screenshots/` directory for visual verification.

## Troubleshooting

If the test fails to find elements, you may need to adjust the selectors in the script based on your actual HTML structure. The script tries to use common selectors, but your application might use different ones.
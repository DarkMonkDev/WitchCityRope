# User Dropdown Menu Tests

This directory contains Puppeteer tests specifically for testing the user dropdown menu functionality in the WitchCityRope application.

## Test Files

1. **test-user-dropdown-menu.js** - Basic test that covers the core functionality
2. **test-user-dropdown-menu-comprehensive.js** - Comprehensive test with detailed assertions and reporting

## Features Tested

### 1. Authentication States
- ✅ Unauthenticated state shows LOGIN button
- ✅ Authenticated state shows user menu with avatar
- ✅ Username is displayed correctly

### 2. Dropdown Menu Functionality
- ✅ Dropdown opens when clicking user avatar/button
- ✅ Dropdown closes when clicking outside
- ✅ Dropdown toggle works correctly

### 3. Menu Items Verification
- ✅ My Dashboard link
- ✅ Admin Dashboard link (for admin users)
- ✅ My Profile link
- ✅ Logout link
- ✅ All items have correct icons
- ✅ All items have correct href attributes

### 4. Admin-Specific Features
- ✅ Admin Dashboard menu item appears for admin users
- ✅ Regular users don't see admin-only options

### 5. Logout Functionality
- ✅ Clicking logout terminates the session
- ✅ User is redirected appropriately
- ✅ Login button reappears after logout
- ✅ User menu is removed from navigation

### 6. Responsive Behavior
- ✅ Mobile menu toggle appears on small screens
- ✅ Desktop menu behavior changes appropriately

## Prerequisites

1. Node.js and npm installed
2. Puppeteer installed:
   ```bash
   npm install puppeteer
   ```

3. Application running locally:
   ```bash
   cd src/WitchCityRope.Web
   dotnet run
   ```

## Running the Tests

### Basic Test
```bash
node test-user-dropdown-menu.js
```

### Comprehensive Test
```bash
node test-user-dropdown-menu-comprehensive.js
```

### With Custom URL
```bash
BASE_URL=https://localhost:7036 node test-user-dropdown-menu.js
```

### Headless Mode
```bash
HEADLESS=true node test-user-dropdown-menu-comprehensive.js
```

## Test Credentials

The tests use the following admin credentials:
- Email: `admin@witchcityrope.com`
- Password: `Test123!`

Make sure these credentials exist in your database or update them in the test files.

## Output

### Screenshots
Both tests save screenshots to their respective directories:
- Basic test: `user-dropdown-screenshots/`
- Comprehensive test: `user-dropdown-screenshots-comprehensive/`

Screenshots are taken at key points:
1. Homepage (unauthenticated)
2. Login page
3. Login form filled
4. After login
5. Dropdown menu open
6. After logout
7. Mobile view (comprehensive test only)

### Test Report
The comprehensive test generates a detailed JSON report at:
`user-dropdown-screenshots-comprehensive/test-report.json`

This report includes:
- Test results (pass/fail)
- Timestamps
- Error details
- Screenshot references
- Performance metrics

## Troubleshooting

### Common Issues

1. **Login fails**
   - Verify the admin credentials exist in the database
   - Check if the application is running on the expected port
   - Ensure the login form selectors match your implementation

2. **Elements not found**
   - The tests expect specific CSS classes (`.user-menu-btn`, `.user-dropdown`, etc.)
   - Update selectors if your implementation uses different classes
   - Check browser console for JavaScript errors

3. **Dropdown doesn't appear**
   - Verify the dropdown toggle JavaScript is working
   - Check for CSS animation delays
   - Increase wait times in the test if needed

4. **Screenshots are blank**
   - Ensure the application is fully loaded
   - Check for HTTPS certificate issues
   - Try running in non-headless mode to debug

### Debug Mode

To run tests with full browser visibility:
```bash
HEADLESS=false node test-user-dropdown-menu.js
```

This will open a browser window and you can watch the test execution.

## Extending the Tests

To add new test cases:

1. Add new menu items to verify:
   ```javascript
   const expectedMenuItems = [
       { text: 'New Item', href: '/path', icon: 'fa-icon' },
       // ... existing items
   ];
   ```

2. Test different user roles:
   ```javascript
   const REGULAR_USER = {
       email: 'user@example.com',
       password: 'UserPass123!'
   };
   ```

3. Add accessibility checks:
   ```javascript
   // Check ARIA attributes
   const ariaExpanded = await page.$eval('.user-menu-btn', 
       el => el.getAttribute('aria-expanded'));
   ```

## CI/CD Integration

To run these tests in a CI/CD pipeline:

```yaml
# Example GitHub Actions
- name: Run User Dropdown Tests
  run: |
    npm install puppeteer
    HEADLESS=true node test-user-dropdown-menu-comprehensive.js
  
- name: Upload Test Artifacts
  uses: actions/upload-artifact@v2
  if: always()
  with:
    name: dropdown-test-screenshots
    path: user-dropdown-screenshots-comprehensive/
```

## Related Documentation

- [User Menu Component Documentation](./docs/components/user-menu.md)
- [Authentication Flow](./docs/authentication.md)
- [UI Testing Strategy](./docs/testing/ui-tests.md)
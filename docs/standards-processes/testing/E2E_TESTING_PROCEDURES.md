# E2E Testing Procedures for WitchCityRope

## Overview

This document provides comprehensive procedures for running end-to-end tests in the WitchCityRope development Docker environment. It covers setup, execution, troubleshooting, and interpretation of test results.

## Prerequisites

### Environment Setup

1. **Docker Environment Running**
   ```bash
   # Start the development environment
   docker-compose up -d
   
   # Verify services are healthy
   docker ps
   ```

2. **Check Application Access**
   - Web Application: http://localhost:5651
   - API: http://localhost:5653
   - PostgreSQL: localhost:5433

3. **Install Puppeteer**
   ```bash
   cd tests/e2e
   npm install puppeteer
   ```

## Running Tests

### Correct Way to Run Tests in Docker Environment

**IMPORTANT**: The application runs on port 5651 in the Docker environment, not 8080 or other ports mentioned in older tests.

#### 1. Individual Test Execution

```bash
# Navigate to e2e test directory
cd tests/e2e

# Run a specific test
node test-complete-event-flow.js

# Run with visible browser (non-headless)
HEADLESS=false node test-events-reality-check.js
```

#### 2. Running Test Suites

```bash
# Run validation tests
./run-validation-tests.sh

# Run comprehensive validation tests
./run-comprehensive-validation-tests.sh

# Run admin events tests
node run-admin-events-tests.js
```

#### 3. Test Configuration

All tests should use these settings:
```javascript
const BASE_URL = 'http://localhost:5651';  // Docker port
const ADMIN_EMAIL = 'admin@witchcityrope.com';
const ADMIN_PASSWORD = 'Test123!';
```

## Common Issues and Solutions

### 1. Port Number Issues

**Problem**: Tests fail with connection refused or timeout errors.

**Solution**: 
- Always use port 5651 for Docker environment
- Update any test files using port 8080 or other ports
- Check if services are running: `docker ps`

### 2. Authentication Issues

**Problem**: Login fails with "Invalid login attempt" or authentication errors.

**Solution**:
- Use the correct test accounts (see Test Accounts section)
- Ensure you're using email OR scene name for login
- Check that the database is seeded: `docker exec witchcityrope-web-1 dotnet ef database update`

### 3. WebSocket Errors

**Problem**: Console shows WebSocket connection errors during tests.

**Solution**:
- These are usually harmless for E2E tests
- Add console error filtering if needed:
  ```javascript
  page.on('console', msg => {
    if (!msg.text().includes('WebSocket')) {
      console.log('Console:', msg.text());
    }
  });
  ```

### 4. Element Not Found

**Problem**: Tests fail with "waiting for selector" timeout.

**Solution**:
- Check if the page loaded correctly (take screenshots)
- Verify selectors haven't changed in the UI
- Add wait conditions:
  ```javascript
  await page.waitForSelector('selector', { 
    visible: true, 
    timeout: 10000 
  });
  ```

## Test Accounts

### Standard Test Users

| Email | Password | Role | Scene Name |
|-------|----------|------|------------|
| admin@witchcityrope.com | Test123! | Administrator | AdminUser |
| member@witchcityrope.com | Test123! | Member | MemberUser |
| teacher@witchcityrope.com | Test123! | Teacher | TeacherUser |
| vetted@witchcityrope.com | Test123! | Vetted Member | VettedUser |
| guest@witchcityrope.com | Test123! | Attendee | GuestUser |

### Using Scene Names

The login system accepts either email OR scene name:
```javascript
// Login with email
await page.type('input#Input_Email', 'admin@witchcityrope.com');

// OR login with scene name
await page.type('input#Input_Email', 'AdminUser');
```

## Test Scripts Reference

### Core Test Scripts

1. **test-complete-event-flow.js**
   - Comprehensive E2E test covering entire event lifecycle
   - Creates events, handles registrations, verifies dashboard
   - Best example of proper test structure

2. **run-validation-tests.sh**
   - Runs validation tests on all migrated forms
   - Checks for WCR component usage
   - Generates detailed reports

3. **test-validation-diagnostics.js**
   - Advanced diagnostic tool for forms
   - Captures detailed validation state
   - Useful for debugging form issues

### Admin Tests

1. **admin-events-management.test.js**
   - Tests all 4 tabs of event management
   - Covers CRUD operations
   - Validates form submissions

2. **run-admin-events-tests.js**
   - Simplified test runner for admin features
   - Includes helper functions
   - Good starting point for admin testing

### Diagnostic Tools

1. **diagnose-event-form.js**
   - Analyzes form structure
   - Identifies required fields
   - Reports validation errors

2. **check-console-errors.js**
   - Monitors browser console
   - Filters known issues
   - Helps identify JavaScript errors

## Interpreting Test Results

### Success Indicators

1. **Exit Code 0**: Test passed successfully
2. **Screenshots**: Check generated screenshots for visual confirmation
3. **Console Output**: Look for "✅ Test completed successfully"

### Failure Analysis

1. **Check Screenshots**
   - Located in test directory or screenshots/ folder
   - Named with descriptive error states
   - Compare with expected UI state

2. **Read Error Messages**
   - Timeout errors usually mean element not found
   - Network errors indicate API issues
   - JavaScript errors show in console output

3. **Review Test Reports**
   - JSON reports in test-results/ directory
   - HTML reports for validation tests
   - Detailed logs with timestamps

### Common Test Patterns

1. **Login Flow**
   ```javascript
   async function login(page) {
     await page.goto(`${BASE_URL}/Identity/Account/Login`);
     await page.waitForSelector('input#Input_Email', { visible: true });
     await page.type('input#Input_Email', ADMIN_EMAIL);
     await page.type('input#Input_Password', ADMIN_PASSWORD);
     await page.click('button[type="submit"]');
     await page.waitForNavigation({ waitUntil: 'networkidle2' });
   }
   ```

2. **Error Handling**
   ```javascript
   try {
     // Test code here
   } catch (error) {
     await page.screenshot({ path: 'error-state.png' });
     console.error('Test failed:', error.message);
     throw error;
   }
   ```

3. **Waiting for Elements**
   ```javascript
   // Wait for element to appear
   await page.waitForSelector('.element', { visible: true });
   
   // Wait for navigation
   await page.waitForNavigation({ waitUntil: 'networkidle2' });
   
   // Wait for specific time
   await new Promise(resolve => setTimeout(resolve, 1000));
   ```

## Best Practices

1. **Always Use Try-Catch**
   - Capture screenshots on failure
   - Log meaningful error messages
   - Clean up resources

2. **Use Descriptive Test Names**
   - Name files clearly: `test-[feature]-[action].js`
   - Add comments explaining test purpose
   - Document expected outcomes

3. **Handle Async Operations**
   - Use proper await statements
   - Set appropriate timeouts
   - Handle navigation properly

4. **Screenshot Key Points**
   - Before actions
   - After actions
   - On errors
   - Final state

## Troubleshooting Guide

### Docker Environment Issues

```bash
# Restart services
docker-compose restart

# View logs
docker-compose logs -f web
docker-compose logs -f api

# Rebuild if needed
docker-compose build --no-cache
docker-compose up -d
```

### Database Issues

```bash
# Check database connection
docker exec -it witchcityrope-db psql -U postgres -d witchcityrope_db

# Re-seed database
docker exec witchcityrope-web-1 dotnet run --project /app/scripts/seed-database.cs
```

### Test Environment Reset

```bash
# Full reset
docker-compose down -v
docker-compose up -d

# Wait for services to be ready
sleep 30

# Run tests
cd tests/e2e
node test-complete-event-flow.js
```

## Creating New Tests

### Test Template

```javascript
const puppeteer = require('puppeteer');

const BASE_URL = 'http://localhost:5651';
const ADMIN_EMAIL = 'admin@witchcityrope.com';
const ADMIN_PASSWORD = 'Test123!';

(async () => {
    const browser = await puppeteer.launch({
        headless: process.env.HEADLESS !== 'false',
        slowMo: 100,
        args: ['--no-sandbox', '--disable-setuid-sandbox']
    });
    
    const page = await browser.newPage();
    await page.setViewport({ width: 1440, height: 900 });
    
    try {
        console.log('Starting test...');
        
        // Test implementation here
        
        console.log('✅ Test completed successfully');
    } catch (error) {
        console.error('❌ Test failed:', error.message);
        await page.screenshot({ path: 'test-error.png' });
        process.exit(1);
    } finally {
        await browser.close();
    }
})();
```

## Maintenance

### Updating Tests

1. **When UI Changes**
   - Update selectors in affected tests
   - Re-run tests to verify fixes
   - Update screenshots if needed

2. **When API Changes**
   - Update request/response handling
   - Verify authentication still works
   - Check error handling

3. **Regular Maintenance**
   - Run full test suite weekly
   - Update test data as needed
   - Remove obsolete tests
   - Add tests for new features

## Contact and Support

For test-related issues:
1. Check this documentation first
2. Review test output and screenshots
3. Check Docker logs
4. Consult TEST_REGISTRY.md for specific test details

Remember: E2E tests are crucial for ensuring the application works correctly from a user's perspective. Keep them maintained and run them regularly!
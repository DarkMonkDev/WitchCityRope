# Login Monitoring and Testing Guide

This guide explains how to use the login monitoring tools to debug authentication issues in the WitchCityRope application.

## Tools Created

### 1. `test-login-with-monitoring.js`
A comprehensive Puppeteer script that:
- Monitors Docker container logs in real-time
- Performs automated login testing
- Captures all network requests and responses
- Takes screenshots at each step
- Saves detailed logs and analysis

### 2. `monitor-and-test-login.sh`
A bash script that provides:
- Background Docker log monitoring
- Options for automated or manual testing
- Log capture to timestamped files

### 3. `analyze-login-logs.js`
An analysis tool that:
- Parses captured logs
- Identifies patterns and issues
- Provides recommendations
- Generates a comprehensive report

## Prerequisites

1. Docker containers must be running:
   ```bash
   docker-compose up -d
   ```

2. Node.js and npm must be installed

3. Install Puppeteer (if not already installed):
   ```bash
   npm install puppeteer
   ```

## Usage

### Option 1: Full Automated Test with Monitoring

Run the comprehensive test that monitors logs and performs login:

```bash
node test-login-with-monitoring.js
```

This will:
- Start monitoring Docker logs for authentication-related messages
- Open a browser window with DevTools
- Navigate to the login page
- Fill in credentials (admin@witchcityrope.com / Test123!)
- Submit the form
- Capture all network traffic and responses
- Save screenshots and logs to `login-monitoring-results/`
- Keep the browser open for manual inspection

### Option 2: Interactive Testing with Monitoring

Use the bash script for more control:

```bash
./monitor-and-test-login.sh
```

Choose from:
1. **Automated test** - Runs the Puppeteer test
2. **Manual test** - Monitors logs while you test manually in your browser
3. **Exit**

### Option 3: Analyze Previous Test Results

After running tests, analyze the results:

```bash
node analyze-login-logs.js
```

This generates a detailed report including:
- Test results summary
- API calls made
- Docker log patterns
- Authentication data captured
- Critical issues found
- Recommendations

## What to Look For

### Success Indicators
- ✅ URL changes from `/identity/account/login` after submission
- ✅ `LoginAsync` method is called in the logs
- ✅ API calls are made to authentication endpoints
- ✅ JWT token or authentication cookies are set
- ✅ Logout button or user info appears
- ✅ No error messages in logs

### Failure Indicators
- ❌ URL remains on `/identity/account/login`
- ❌ No `LoginAsync` calls in Docker logs
- ❌ No API calls detected
- ❌ Error messages or exceptions in logs
- ❌ No authentication tokens stored
- ❌ Login form shows error messages

## Output Files

All results are saved in `login-monitoring-results/`:
- `test-summary-[timestamp].json` - Complete test results
- `1-login-page-[timestamp].png` - Initial login page
- `2-form-filled-[timestamp].png` - Form with credentials
- `3-final-state-[timestamp].png` - Page after login attempt
- `response-[timestamp].json` - API response details
- `error-[timestamp].json` - Any errors encountered

## Troubleshooting

### If login is not reaching AuthenticationService.LoginAsync:

1. **Check client-side issues:**
   - Open browser DevTools Network tab
   - Look for failed API requests
   - Check Console for JavaScript errors

2. **Verify API configuration:**
   - Check `appsettings.json` in Web project for correct API URL
   - Verify CORS settings in API project
   - Ensure authentication middleware is configured

3. **Check Docker networking:**
   - Verify containers can communicate
   - Check if API is accessible from Web container

### If seeing authentication errors:

1. **Verify database:**
   - Check if test user exists in database
   - Verify password hash is correct
   - Ensure user is active/enabled

2. **Check JWT configuration:**
   - Verify JWT settings match between Web and API
   - Check token expiration settings
   - Ensure signing keys are configured

## Manual Testing Steps

If automated tests aren't revealing the issue:

1. Start log monitoring:
   ```bash
   ./monitor-and-test-login.sh
   ```
   Choose option 2 (Manual test)

2. Open browser to http://localhost:5651/identity/account/login

3. Open browser DevTools (F12)
   - Go to Network tab
   - Clear any existing requests

4. Enter credentials:
   - Email: admin@witchcityrope.com
   - Password: Test123!

5. Click Sign In and observe:
   - Network requests in DevTools
   - Console errors
   - Docker logs in terminal

6. Check for:
   - POST request to login endpoint
   - Response status and body
   - Any redirect responses
   - Cookie/token storage

## Next Steps

Based on the analysis results:

1. If no API calls are made, focus on client-side debugging
2. If API calls fail, check server-side logs and configuration
3. If authentication succeeds but redirect fails, check navigation logic
4. If tokens aren't stored, verify client-side storage implementation

## Support

For detailed error analysis, share:
- The `test-summary-[timestamp].json` file
- Screenshots from `login-monitoring-results/`
- Any error messages from the console
- Docker logs captured during the test
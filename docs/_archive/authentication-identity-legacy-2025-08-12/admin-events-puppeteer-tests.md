# Admin Events Management - Puppeteer Tests

This file is WAY outdated and we no longer used puppeteer. However, this file could be used to generate and review the playwright tests that do the same purpose that the puppeteer tests were doing. This document describes the comprehensive Puppeteer tests created to verify the Admin Events Management implementation.

## Test Files Created

### 1. `test-admin-events-tabs.js`
**Purpose**: Verifies that all 4 tabs from the wireframes are correctly implemented

**What it tests**:
- Login flow using admin credentials
- Navigation to Admin -> Events Management
- Presence of all 4 tabs: Basic Info, Tickets/Orders, Emails, Volunteers/Staff
- Tab switching functionality
- Tab content visibility

**Expected Results**:
- All 4 tabs should be present with correct names
- Clicking each tab should show its content
- Tab switching should work smoothly

### 2. `test-admin-events-fields.js`
**Purpose**: Tests all form fields in each tab

**What it tests**:
- **Basic Info Tab**:
  - Event Name, Description, Image URL
  - Start/End DateTime, Registration open/close dates
  - Location, Max Attendees, Refund cutoff hours
  - Is Public checkbox
- **Tickets/Orders Tab**:
  - Ticket type selection (Individual, Couples, Both)
  - Individual and Couples pricing fields
  - Volunteer tickets field
- **Emails Tab**:
  - Email template selection interface
  - Custom email section
- **Volunteers/Staff Tab**:
  - Volunteer task management interface
  - Add task functionality

**Expected Results**:
- All fields should be present and accept input
- Form should save successfully
- Validation should work correctly

### 3. `test-admin-events-public-view.js`
**Purpose**: Verifies that changes made in admin appear on the public Events & Classes page

**What it tests**:
- Creates/edits an event with distinctive test data
- Saves the event in admin panel
- Navigates to public Events & Classes page
- Searches for the test event
- Verifies event details are displayed correctly

**Expected Results**:
- Admin changes should immediately appear on public page
- Event details (name, description, location, pricing) should match
- Event should be clickable for more details

### 4. `test-admin-events-all.js`
**Purpose**: Master test runner that executes all tests in sequence

**Features**:
- Runs all 3 tests automatically
- Provides summary of results
- Organizes screenshots into a directory
- Adds delays between tests for stability

## Running the Tests

### Prerequisites
1. Ensure the application is running:
   ```bash
   docker-compose up -d
   # OR
   dotnet run --project src/WitchCityRope.Web
   ```

2. Verify the application is accessible at http://localhost:5651

3. Ensure Puppeteer is installed:
   ```bash
   npm install puppeteer
   ```

### Running Individual Tests
```bash
# Test tabs only
node test-admin-events-tabs.js

# Test form fields
node test-admin-events-fields.js

# Test public view
node test-admin-events-public-view.js
```

### Running All Tests
```bash
# Run complete test suite
node test-admin-events-all.js
```

## Test Output

### Console Output
Each test provides detailed console output showing:
- Current step being executed
- Success/failure of each action
- Summary of results
- Any errors encountered

### Screenshots
Screenshots are automatically captured at key points:
- `admin-events-list.png` - Events list page
- `event-edit-initial.png` - Event edit page initial state
- `event-edit-basic-info.png` - Basic Info tab
- `event-edit-tickets-orders.png` - Tickets/Orders tab
- `event-edit-emails.png` - Emails tab
- `event-edit-volunteers-staff.png` - Volunteers/Staff tab
- `basic-info-filled.png` - Basic Info with test data
- `tickets-orders-filled.png` - Tickets tab with pricing
- `admin-event-before-save.png` - Complete form before saving
- `public-events-page.png` - Public Events & Classes page
- `public-event-details.png` - Event details on public page
- `error-screenshot.png` - Captured if any errors occur

When using `test-admin-events-all.js`, screenshots are organized into:
```
admin-events-test-screenshots/
├── admin-events-list.png
├── event-edit-initial.png
├── event-edit-basic-info.png
├── event-edit-tickets-orders.png
├── event-edit-emails.png
├── event-edit-volunteers-staff.png
└── ... (other screenshots)
```

## Interpreting Results

### Success Indicators
- ✅ All 4 tabs present with correct names
- ✅ All form fields accept input
- ✅ Event saves without validation errors
- ✅ Changes appear on public page
- ✅ No JavaScript errors in console

### Common Issues
1. **"Tab not found"** - Tab naming doesn't match wireframes
2. **"Field not found"** - Form field missing or has wrong name attribute
3. **"Save failed"** - Validation errors or backend issues
4. **"Not visible on public page"** - Event might not be marked as public
5. **"Navigation timeout"** - Application might be slow or not running

## Test Data

The tests use distinctive markers to identify test data:
- Event names include timestamp: `Test Event [PUBLIC VIEW TEST 1234567890]`
- Test prices: $123.45 (individual), $234.56 (couples)
- Test location: "Test Venue - Admin Panel Test"
- Max attendees: 99

This makes it easy to identify and clean up test data later.

## Maintenance

### Updating Tests
If the UI changes, update the selectors in the test files:
- Tab selectors: `.tab-button, .nav-link, [role="tab"]`
- Form fields: `input[name="model.FieldName"]`
- Save button: Look for text containing "save", "update", or "create"

### Adding New Tests
To test new features:
1. Copy an existing test file as a template
2. Update the test steps and assertions
3. Add to `test-admin-events-all.js` tests array
4. Document in this file

## New Tests Added (January 11, 2025)

### 5. `test-events-display-and-edit.js`
**Purpose**: Comprehensive test that verifies events actually display in the admin list and can be edited

**What it tests**:
- Events are fetched and displayed in the admin events list
- Edit buttons/links work correctly
- Multiple field changes can be made (title, description, location, capacity, pricing)
- Changes persist after saving
- Updated event details appear on the public Events & Classes page

**Key Features**:
- Dynamically searches for event elements using multiple selectors
- Generates unique timestamps for test data
- Captures screenshots at each major step
- Handles both successful and error scenarios

**Expected Results**:
- Events from database should display in admin list
- Edit form should load when clicking edit
- All field updates should save successfully
- Changes should immediately appear on public page

### 6. `test-events-api-vs-ui.js`
**Purpose**: Debug test that compares API responses with UI display to identify disconnects

**What it tests**:
- Direct API calls to `/api/events`
- Browser console errors during page load
- Network requests made by the UI
- Comparison of API data vs what's rendered

**Key Features**:
- Makes direct HTTP requests to API
- Monitors browser console for errors
- Captures network activity
- Provides detailed debugging output

**Expected Results**:
- API should return valid JSON with event data
- UI should make correct API calls
- No console errors should occur
- Event count in UI should match API response

### 7. `test-events-reality-check.js`
**Purpose**: Comprehensive "truth test" that shows exactly what users experience

**What it tests**:
- Actual page content without assumptions
- Whether created events persist
- Real user workflow from admin to public view
- Visual confirmation with screenshots

**Key Features**:
- Non-headless mode option for visual debugging
- Step-by-step output with analysis
- Captures page state at each step
- Summary report of what actually works vs what's broken

**Expected Results**:
- Admin page should show all events from database
- Event creation should persist data
- Events should appear on both admin and public pages
- No "no events found" messages when events exist

## Important Discoveries

### Issues Found by These Tests
1. **Database JSON Serialization Error**: Empty strings in `Tags` column couldn't be deserialized
   - **Fix**: `UPDATE "Events" SET "Tags" = '[]' WHERE "Tags" = '' OR "Tags" IS NULL`

2. **Wrong API Endpoint**: ApiClient was calling `/api/admin/events` instead of `/api/events`
   - **Fix**: Updated endpoint in `GetManagedEventsAsync` method

3. **Property Mapping Issues**: EventSummaryDto properties didn't match what code expected
   - **Fix**: Updated property mappings in ApiClient

### Test Execution Order
When debugging display issues, run tests in this order:
1. `test-events-api-vs-ui.js` - Identify API/UI disconnects
2. `test-events-reality-check.js` - See actual user experience
3. `test-events-display-and-edit.js` - Verify full CRUD functionality

## Troubleshooting

### Tests Won't Run
- Check if Puppeteer is installed: `npm list puppeteer`
- Verify application is running: `curl http://localhost:5651`
- Check for port conflicts
- Ensure you're in the correct directory (`tests/e2e`)

### Login Fails
- Verify credentials: admin@witchcityrope.com / Test123!
- Check if login page URL is correct: /Identity/Account/Login
- Ensure database has seed data
- Wait for container to be fully healthy

### Elements Not Found
- Use browser DevTools to inspect current selectors
- Check if page has fully loaded (increase waitForTimeout)
- Verify element names match model properties
- Use multiple selector strategies (id, class, name, xpath)

### Screenshots Not Saving
- Check write permissions in current directory
- Ensure enough disk space
- Try absolute paths for screenshot files
- Create screenshots directory if it doesn't exist

### "No Events Found" Despite Events in Database
- Check for JSON serialization errors in container logs
- Verify all JSON columns have valid data (not empty strings)
- Ensure API endpoint matches what UI expects
- Check browser console for API errors

# RSVP and Ticketing Tests

This directory contains Playwright tests for RSVP and ticketing functionality, converted from the original Puppeteer tests.

## Test Files

### 1. `rsvp-functionality.spec.ts`
**Original:** `test-rsvp-functionality.js`
- Tests RSVP functionality for social events
- Creates social events with RSVP capability
- Verifies RSVP tab visibility for social events
- Tests member RSVP creation
- Tests admin RSVP management
- Tests event check-in functionality

### 2. `member-rsvp-flow.spec.ts`
**Original:** `test-member-rsvp-flow.js`
- Tests complete RSVP flow from member perspective
- Admin creates social event
- Member RSVPs to event
- RSVP appears on member dashboard
- Admin can verify the RSVP

### 3. `ticket-functionality.spec.ts`
**Original:** `test-ticket-functionality.js`
- Tests ticket functionality for workshop events
- Creates workshop events with ticket sales
- Verifies NO RSVP tab for workshop events
- Tests ticket purchase flow
- Tests admin ticket management
- Tests check-in for ticketed events
- Verifies ticket terminology (TKT- prefix instead of REG-)

### 4. `complete-rsvp-dashboard-flow.spec.ts`
**Original:** `test-complete-rsvp-dashboard-flow.js`
- Tests complete RSVP to Dashboard flow
- Login as vetted member
- Navigate to events and RSVP to a social event
- Verify the RSVP appears on the dashboard
- Includes API tracking and debugging

## Page Objects

### `rsvp.page.ts`
Page object for RSVP functionality:
- RSVP creation from event pages
- Admin RSVP management
- Event check-in functionality
- RSVP statistics and counts

### `member-dashboard.page.ts`
Page object for member dashboard:
- View upcoming events (RSVPs and tickets)
- Manage RSVPs from dashboard
- View ticket information
- Dashboard statistics

## Running the Tests

Run all RSVP tests:
```bash
npx playwright test tests/playwright/rsvp/
```

Run specific test file:
```bash
npx playwright test tests/playwright/rsvp/rsvp-functionality.spec.ts
```

Run with UI mode for debugging:
```bash
npx playwright test tests/playwright/rsvp/ --ui
```

## Key Differences from Puppeteer Tests

1. **Better Selectors**: Uses Playwright's advanced selector engine with filters for more reliable element selection
2. **Page Objects**: All page interactions are encapsulated in page objects for better maintainability
3. **Improved Waiting**: Uses Playwright's built-in waiting mechanisms instead of manual timeouts
4. **Better Assertions**: Uses Playwright's expect API for more readable assertions
5. **Parallel Execution**: Tests can run in parallel when not using `.serial()`
6. **Better Error Messages**: Playwright provides more detailed error messages and screenshots
7. **API Tracking**: Enhanced API call tracking for debugging

## Test Data

Tests use the test accounts defined in `helpers/test.config.ts`:
- Admin: `admin@witchcityrope.com`
- Member: `member@witchcityrope.com`
- Vetted: `vetted@witchcityrope.com`

## Notes

- Social events (Rope Jams, Meetups) use RSVPs
- Workshop events use tickets
- RSVP tab only appears for social events
- Ticket confirmation codes use TKT- prefix
- Member dashboard shows both RSVPs and tickets
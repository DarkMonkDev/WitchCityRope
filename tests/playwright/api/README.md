# API Tests

This directory contains Playwright tests for testing the WitchCityRope API endpoints directly, as well as comparing API responses with UI behavior.

## Test Files

### `api-endpoints.spec.ts`
Tests core API endpoints for user data:
- User RSVPs endpoint (`/api/users/me/rsvps`)
- User tickets endpoint (`/api/users/me/tickets`)
- Specific event RSVP status (`/api/events/{id}/rsvps/me`)
- Authentication and authorization

### `events-api-vs-ui.spec.ts`
Compares API responses with UI display:
- Fetches events from API and compares with admin/public UI
- Monitors API calls made by the UI
- Uses request interception to log API interactions
- Takes screenshots for visual comparison

### `diagnose-event-api.spec.ts`
Comprehensive diagnostic tests for the events API:
- Health check endpoint
- Authentication flow
- Public and authenticated event listings
- Event creation and verification
- Admin-specific endpoints
- Error handling and rate limiting tests
- Performance measurements

### `member-rsvp-api.spec.ts`
End-to-end test flow for member RSVPs:
- Admin creates event via API
- Member RSVPs to the event
- Verification in member dashboard
- Tests both API and UI fallback approaches
- Handles concurrent RSVP attempts

## Running the Tests

```bash
# Run all API tests
npx playwright test tests/playwright/api/

# Run specific test file
npx playwright test tests/playwright/api/api-endpoints.spec.ts

# Run with UI mode for debugging
npx playwright test tests/playwright/api/ --ui

# Run with specific API URL
API_URL=http://localhost:5653 npx playwright test tests/playwright/api/
```

## Key Features

### API Request Context
All tests use Playwright's `APIRequestContext` for direct API testing:
```typescript
const apiContext = await playwright.request.newContext({
  baseURL: API_BASE_URL,
  extraHTTPHeaders: {
    'Accept': 'application/json',
    'Content-Type': 'application/json',
  },
});
```

### Authentication Handling
Tests handle both API token-based auth and cookie-based auth:
```typescript
// API token auth
headers: {
  'Authorization': `Bearer ${authToken}`
}

// Cookie auth from UI login
const authCookie = cookies.find(c => c.name === '.AspNetCore.Identity.Application');
```

### Request Interception
Some tests use request interception to monitor and log API calls:
```typescript
await page.route('**/api/**', async route => {
  const request = route.request();
  console.log(`API Request: ${request.method()} ${request.url()}`);
  // ... handle request
});
```

### Hybrid Testing Approach
Tests often try multiple approaches:
1. Direct API calls
2. UI-based actions
3. Page context fetch calls

This ensures tests are resilient to different authentication methods and API configurations.

## Environment Variables

- `API_URL`: Base URL for API (default: `http://localhost:5653`)
- `BASE_URL`: Base URL for UI (default: `http://localhost:5651`)

## Notes

- These tests were converted from Puppeteer to Playwright
- They maintain the original test logic while leveraging Playwright's superior API testing capabilities
- Tests include comprehensive logging for debugging
- Error handling includes fallback approaches when primary methods fail
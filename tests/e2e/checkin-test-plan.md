# Check-In System E2E Test Plan

## Test File Structure

### 1. checkin-staff-authentication.spec.ts → checkin-token-validation.spec.ts (7 tests)
**Updated 2025-11-03**: Migrated from CheckInStaff role authentication to session token validation
- ✅ Valid token allows access to check-in interface
- ✅ Invalid token shows error message
- ✅ Missing token shows error message
- ✅ Token for wrong event returns 403
- ✅ Revoked token cannot be used (401)
- ✅ No authentication required for valid token
- ✅ Expired token shows error message

### 2. checkin-attendee-workflow.spec.ts (4 tests)
**Updated 2025-11-03**: Uses session token kiosk mode (NO user authentication)
- Check in a registered attendee (token-based)
- Cannot check in same attendee twice
- Check-in modal displays attendee information
- Token validation fails for expired token during check-in (NEW)

### 3. checkin-walk-in-workflow.spec.ts (5 tests)
**Updated 2025-11-03**: Uses session token kiosk mode (NO user authentication)
- Add walk-in with new email (token-based)
- Add walk-in with existing user email
- Walk-in form validation - empty name
- Walk-in form validation - invalid email format
- Walk-in respects capacity limits (uses token-based API)

### 4. checkin-dashboard.spec.ts (5 tests)
**Updated 2025-11-03**: Uses session token kiosk mode (NO user authentication)
- Dashboard displays correct statistics (token-based navigation)
- Dashboard shows staff on duty (kiosk mode - no logged-in user)
- Recent check-ins feed updates
- Sync status displays
- Dashboard navigation from check-in interface

### 5. checkin-search-filter.spec.ts (6 tests)
**Updated 2025-11-03**: Uses session token kiosk mode (NO user authentication)
- Search attendees by name (partial match) (token-based)
- Search attendees by email
- Search shows no results for invalid query
- Clear search returns full list
- Filter by check-in status (conditional)
- Search is case-insensitive

## Authentication Pattern (Updated 2025-11-03)

**Session Token Kiosk Mode** (NO user login required):
1. Administrator generates session token via admin UI
2. Token embedded in check-in URL: `/events/{eventId}/checkin?token={token}&event={eventId}`
3. Token sent in `X-CheckIn-Token` header for API calls
4. NO authentication cookies - kiosk runs without logged-in user
5. Cookies cleared before each test to simulate kiosk mode

**Token Generation**:
- Admin: admin@witchcityrope.com / Test123! (generates tokens)
- Token lifespan: 1 hour default (configurable)
- Tokens can be revoked by Administrator

## Test Data
- **Administrator**: admin@witchcityrope.com / Test123! (for token generation only)
- **Member**: member@witchcityrope.com / Test123! (for testing unauthorized access)
- **Event**: Community Rope Jam (ID from API query)

## Routes
- Check-in interface: `/events/{eventId}/checkin?token={token}&event={eventId}` (token-based)
- Check-in dashboard: `/events/{eventId}/checkin/dashboard?token={token}&event={eventId}` (token-based)

## API Endpoints (All require X-CheckIn-Token header)
- `POST /api/checkin/session-tokens/generate` (admin auth required)
- `POST /api/checkin/session-tokens/revoke` (admin auth required)
- `GET /api/checkin/events/{eventId}/attendees` (X-CheckIn-Token header)
- `POST /api/checkin/events/{eventId}/checkin` (X-CheckIn-Token header)
- `POST /api/checkin/events/{eventId}/manual-entry` (X-CheckIn-Token header)
- `GET /api/checkin/events/{eventId}/dashboard` (X-CheckIn-Token header)
- `GET /api/checkin/sync/pending-count` (X-CheckIn-Token header)

## Migration Notes (2025-11-03)

**What Changed**:
- **Removed**: CheckInStaff role-based authentication
- **Added**: Session token kiosk mode (admin-generated tokens)
- **Impact**: Check-in kiosks no longer require user login

**Why Token-Based Approach**:
1. **Kiosk Mode**: Check-in stations don't need user accounts
2. **Security**: Tokens are time-limited and revocable
3. **Simplicity**: No password management for kiosk devices
4. **Audit**: Token usage tracked with event association

**Test Pattern**:
```typescript
test.beforeAll(async ({ browser }) => {
  // Admin generates token (separate context)
  const adminContext = await browser.newContext();
  const adminPage = await adminContext.newPage();
  await loginAsAdmin(adminPage);
  sessionToken = await generateSessionToken(adminPage, eventId);
  await adminContext.close();
});

test.beforeEach(async ({ page }) => {
  // NO login - clear cookies to simulate kiosk mode
  await page.context().clearCookies();
  await navigateToCheckIn(page, eventId, sessionToken);
});
```

## Helper Functions

**Token Helpers** (`/apps/web/tests/playwright/checkin/helpers/tokenHelpers.ts`):
- `loginAsAdmin()` - Login as admin to generate tokens
- `generateSessionToken()` - Generate check-in session token via API
- `revokeSessionToken()` - Revoke a session token
- `getTestEventId()` - Get test event ID from API
- `navigateToCheckIn()` - Navigate to check-in with token (NO login)
- `navigateToCheckInDashboard()` - Navigate to dashboard with token
- `getAttendees()` - Get attendees using X-CheckIn-Token header
- `getDashboardData()` - Get dashboard data using token

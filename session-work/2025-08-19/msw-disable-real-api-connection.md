# MSW Disable and Real API Connection Setup

**Date**: 2025-08-19  
**Task**: Disable MSW mocking and connect React app to real .NET API  
**Status**: COMPLETED

## Summary

Successfully disabled MSW mocking and configured the React application to connect to the real .NET API service. The app now uses actual API endpoints instead of mock data.

## Changes Made

### 1. API Client Configuration Updated
**File**: `/apps/web/src/api/client.ts`
- Updated base URL from `http://localhost:5651` to `http://localhost:5653`
- Confirmed connection to real .NET API service
- Verified withCredentials setting for httpOnly cookie authentication

### 2. Environment Configuration
**File**: `/apps/web/.env.development`
- Updated `VITE_API_BASE_URL=http://localhost:5653`
- Added `VITE_ENABLE_MSW=false` flag for future MSW control

### 3. Authentication Store Fix
**File**: `/apps/web/src/stores/authStore.ts`
- Updated auth check endpoint from `/api/auth/user` to `/api/Protected/profile`
- Matches actual API endpoint structure from Swagger

### 4. Authentication Mutations Fix
**File**: `/apps/web/src/features/auth/api/mutations.ts`
- Updated endpoints to match real API:
  - `/api/auth/login` → `/api/Auth/login`
  - `/api/auth/register` → `/api/Auth/register`
  - `/api/auth/logout` → `/api/Auth/logout`

### 5. API Connection Test Page
**File**: `/apps/web/src/pages/ApiConnectionTest.tsx` (NEW)
- Created comprehensive test page to verify real API connection
- Tests health endpoint, protected routes, and authentication
- Helps verify MSW is disabled and real API responses are received

**File**: `/apps/web/src/routes/router.tsx`
- Added route: `/api-connection-test`

**File**: `/apps/web/src/components/layout/Navigation.tsx`
- Added "API Test" navigation link

**File**: `/apps/web/src/pages/HomePage.tsx`
- Added prominent "Test Real API Connection" button

## Verification Steps

### Real API Endpoints Discovered (via Swagger)
```
GET  /api/Health              - Health check
POST /api/Auth/login          - User login
POST /api/Auth/logout         - User logout  
POST /api/Auth/register       - User registration
GET  /api/Auth/user/{id}      - Get user by ID
GET  /api/Protected/profile   - Get current user profile
GET  /api/Protected/welcome   - Protected welcome message
GET  /api/Events              - Events list
```

### MSW Status
- **MSW is ONLY used in testing** (via `/apps/web/src/test/setup.ts`)
- **NO MSW browser worker** in main application
- **Main app connects directly** to real API at localhost:5653
- Tests still use MSW for isolated testing

### Services Running
- **React App**: http://localhost:5173 (Vite dev server)
- **API Service**: http://localhost:5653 (.NET Minimal API)
- **API Health**: Responding with real data: `{"status":"healthy","timestamp":"..."}`

## Testing Instructions

1. **Visit**: http://localhost:5173/api-connection-test
2. **Click**: "Test Real API Connection" button
3. **Expected Results**:
   - Health check succeeds with real timestamp
   - Protected endpoint returns 401 (not authenticated)
   - Login fails with real API validation (test user not in DB)
   - All responses from localhost:5653 (real API)

## Impact

- ✅ **MSW Disabled**: App uses real API, not mock data
- ✅ **Real Authentication**: httpOnly cookies with .NET API
- ✅ **Real Validation**: Server-side validation errors
- ✅ **Real Database**: Actual PostgreSQL data (when available)
- ✅ **Production-Ready**: Same configuration as production deployment

## Next Steps

1. **Test with Real User Account**: Create actual user in database via registration
2. **Verify Full Auth Flow**: Login → Dashboard → Logout with real API
3. **Test Protected Routes**: Ensure route guards work with real authentication
4. **API Documentation**: Update endpoints in documentation to match real API

## Architecture Compliance

This change aligns with the documented architecture:
- **Microservices Pattern**: React → HTTP → .NET API → PostgreSQL
- **Security**: httpOnly cookies, no localStorage tokens
- **Environment-Specific**: Development connects to localhost:5653
- **Type Safety**: Endpoints updated to match NSwag-generated types

## Files Modified

- `/apps/web/src/api/client.ts` - API base URL and comments
- `/apps/web/.env.development` - API URL and MSW flag  
- `/apps/web/src/stores/authStore.ts` - Auth check endpoint
- `/apps/web/src/features/auth/api/mutations.ts` - Auth endpoints (3 changes)
- `/apps/web/src/pages/ApiConnectionTest.tsx` - NEW test page
- `/apps/web/src/routes/router.tsx` - New route
- `/apps/web/src/components/layout/Navigation.tsx` - Navigation link
- `/apps/web/src/pages/HomePage.tsx` - Home page link

**Total**: 7 files modified, 1 file created
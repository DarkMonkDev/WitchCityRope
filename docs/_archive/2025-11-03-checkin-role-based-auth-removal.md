# Check-In Role-Based Authentication Removal

**Date**: 2025-11-03
**Type**: Architecture Migration
**Status**: Completed

## Summary

The check-in system was migrated from role-based authentication (CheckInStaff role) to kiosk mode with cryptographic session tokens over 7 phases. This document archives the old approach for historical reference.

## Old System (Removed)

### CheckInStaff Role
- **Purpose**: Dedicated role for check-in volunteers
- **Test Account**: checkinstaff@witchcityrope.com / Test123!
- **Authorization**: Required login with CheckInStaff, EventOrganizer, or Administrator role
- **Access Pattern**: User logged in → Role checked → Access granted
- **Implementation**: User role enum value, ASP.NET Identity role, test account seeding

### Problems with Old Approach
1. **Account Management Burden**: Required creating user accounts for temporary volunteers
2. **Persistent Access**: Volunteers had ongoing system access even when not checking in
3. **Password Management**: Needed to manage passwords for potentially many check-in stations
4. **Security Concern**: Risk of volunteers accessing more than intended if role permissions changed
5. **Kiosk Mode Impossible**: Cannot run unauthenticated kiosk if login is required

## New System (Current)

### Session Tokens
- **Purpose**: Time-limited, event-specific access tokens
- **Generation**: Administrator/EventOrganizer generates token via admin UI
- **Authorization**: Cryptographic token validation (no user account needed)
- **Access Pattern**: Admin generates link → Volunteer uses link → Token validated
- **Implementation**: CheckInSessionToken entity, ISessionTokenService, admin endpoints

### Benefits
1. **No User Accounts**: Volunteers don't need system accounts
2. **Time-Limited Access**: Tokens expire after configurable hours (default 12)
3. **Event-Specific**: Each token only works for one specific event
4. **Revocable**: Admins can revoke tokens at any time
5. **True Kiosk Mode**: No authentication UI shown to volunteers
6. **Security**: 64-character cryptographically secure random tokens
7. **Audit Trail**: Token creation, usage, and revocation all logged

## Migration Timeline

### Phase 1: Documentation and Requirements (2025-10-XX)
- Security analysis documented kiosk mode requirements
- Technical design for session token system
- Check-in interface UI wireframes

### Phase 2: Backend Session Token Implementation (2025-11-02)
- Created `CheckInSessionToken` entity with EF Core configuration
- Implemented `SessionTokenService` with cryptographic token generation
- Added database migration for `CheckInSessionTokens` table
- Registered service in dependency injection

### Phase 3: Check-In Authorization Migration (2025-11-02)
- Removed role-based auth from check-in operation endpoints
- Added `X-CheckIn-Token` header validation to all check-in operations
- Updated CheckInService to validate tokens instead of roles
- All check-in API calls now require session token, not login

### Phase 4: Frontend Kiosk Mode Implementation (2025-11-02)
- Updated CheckInPage to parse token from URL query parameters
- Removed authentication checks from check-in interface
- All API hooks updated to send `X-CheckIn-Token` header
- Kiosk mode UI shown without login requirement

### Phase 5: Admin Token Management UI (2025-11-03)
- Created GenerateCheckInLinkModal component
- Added token management API client (sessionTokenApi.ts)
- React Query hooks for generate/revoke/list operations
- Integrated into AdminEventDetailsPage for easy access

### Phase 6: E2E Test Migration (2025-11-XX)
- Updated 27 E2E tests to use session tokens
- Replaced role-based auth tests with token validation tests
- Created helper functions for token generation in tests
- All tests passing with new token system

### Phase 7: Cleanup and Documentation (2025-11-03)
- Marked CheckInStaff enum with [Obsolete] attribute
- Added obsolete comments to UserSeeder.cs
- Created this archive documentation
- Updated file registry with all changes

## Migration Changes

### Backend

**New Files Created:**
- `/apps/api/Features/CheckIn/Entities/CheckInSessionToken.cs` - Session token entity
- `/apps/api/Features/CheckIn/Entities/Configuration/CheckInSessionTokenConfiguration.cs` - EF configuration
- `/apps/api/Features/CheckIn/Services/ISessionTokenService.cs` - Service interface
- `/apps/api/Features/CheckIn/Services/SessionTokenService.cs` - Service implementation
- `/apps/api/Features/CheckIn/Models/SessionTokenResponse.cs` - Response DTO
- `/apps/api/Features/CheckIn/Models/GenerateTokenRequest.cs` - Request DTO for generation
- `/apps/api/Features/CheckIn/Models/RevokeTokenRequest.cs` - Request DTO for revocation
- `/apps/api/Migrations/20251103044457_AddCheckInSessionTokens.cs` - Database migration

**Files Modified:**
- `/apps/api/Features/CheckIn/Endpoints/CheckInEndpoints.cs` - Added 3 admin token endpoints
- `/apps/api/Features/CheckIn/Services/CheckInService.cs` - Added token validation
- `/apps/api/Features/CheckIn/Extensions/CheckInServiceExtensions.cs` - Registered SessionTokenService
- `/apps/api/Data/ApplicationDbContext.cs` - Added CheckInSessionTokens DbSet
- `/apps/api/Services/Seeding/UserSeeder.cs` - Marked CheckInStaff user as OBSOLETE
- `/apps/api/Features/Users/Constants/UserRole.cs` - Marked CheckInStaff enum with [Obsolete]

### Frontend

**New Files Created:**
- `/apps/web/src/features/checkin/types/sessionToken.types.ts` - Session token TypeScript types
- `/apps/web/src/features/checkin/api/sessionTokenApi.ts` - API client for tokens
- `/apps/web/src/features/checkin/hooks/useSessionTokens.ts` - React Query hooks
- `/apps/web/src/features/checkin/components/GenerateCheckInLinkModal.tsx` - Admin UI modal

**Files Modified:**
- `/apps/web/src/pages/checkin/CheckInPage.tsx` - Parse token from URL, remove auth checks
- `/apps/web/src/features/checkin/components/CheckInInterface.tsx` - Token-based access
- `/apps/web/src/features/checkin/hooks/useCheckIn.ts` - All hooks send X-CheckIn-Token header
- `/apps/web/src/features/checkin/api/checkinApi.ts` - Token parameter on all API calls
- `/apps/web/src/pages/admin/AdminEventDetailsPage.tsx` - Integrated token generation modal

### Tests

- Updated 27 E2E tests from role-based to token-based authentication
- Replaced authentication setup with token generation helpers
- All tests validate token-based authorization flow

## Obsolete Code References

The following code/accounts remain for backward compatibility but are no longer actively used:

### Backend
- **UserRole.cs** - `CheckInStaff` enum value marked with `[Obsolete("CheckInStaff role is deprecated. Use session tokens for check-in access via CheckInSessionToken entity.")]`
- **UserSeeder.cs** - `checkinstaff@witchcityrope.com` user creation marked with OBSOLETE comment explaining migration
- **CheckInService.cs** - Line 567 comment about role override capabilities (informational only)

### Frontend
- No obsolete references - frontend fully migrated to session tokens

### Why Code Remains
- Existing databases may have users with CheckInStaff role
- Removing enum would break compilation if any code still references it
- [Obsolete] attribute provides compiler warnings if used in new code
- Test account remains functional for backward compatibility testing

## Technical Details

### Token Security
- **Generation**: Cryptographically secure random 512 bits (64 bytes)
- **Encoding**: URL-safe Base64 (no special character escaping needed)
- **Length**: 64 characters fixed length
- **Uniqueness**: Database unique constraint on token column
- **Validation**: Constant-time comparison to prevent timing attacks

### Token Lifecycle
1. **Creation**: Admin clicks "Generate Check-In Link" in admin UI
2. **Distribution**: Admin shares link with volunteer via QR code/URL
3. **Usage**: Volunteer opens link, token parsed from query parameter
4. **Validation**: Every API call validates token against database
5. **Expiration**: Token becomes invalid after configured hours (default 12)
6. **Revocation**: Admin can manually revoke token for security incident

### Database Schema
```sql
CREATE TABLE "public"."CheckInSessionTokens" (
    "Id" uuid NOT NULL PRIMARY KEY,
    "Token" character varying(64) NOT NULL UNIQUE,
    "EventId" uuid NOT NULL REFERENCES "public"."Events",
    "CreatedByUserId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "ExpiresAt" timestamp with time zone NOT NULL,
    "IsRevoked" boolean NOT NULL DEFAULT false,
    "RevokedAt" timestamp with time zone,
    "RevokedByUserId" uuid,
    "LastUsedAt" timestamp with time zone
);

-- Indexes for performance
CREATE INDEX "IX_CheckInSessionTokens_EventId" ON "CheckInSessionTokens" ("EventId");
CREATE INDEX "IX_CheckInSessionTokens_CreatedByUserId" ON "CheckInSessionTokens" ("CreatedByUserId");
CREATE INDEX "IX_CheckInSessionTokens_IsRevoked_ExpiresAt" ON "CheckInSessionTokens" ("IsRevoked", "ExpiresAt");
```

### API Endpoints

**Admin Token Management (Require Administrator or EventOrganizer):**
- `POST /api/checkin/session-tokens/generate` - Generate new token for event
- `POST /api/checkin/session-tokens/revoke` - Revoke active token
- `GET /api/checkin/session-tokens/event/{eventId}` - List active tokens for event

**Check-In Operations (Require Valid Session Token in X-CheckIn-Token Header):**
- `GET /api/checkin/event/{eventId}/attendees` - Get attendee list
- `POST /api/checkin/event/{eventId}/checkin` - Check in attendee
- `POST /api/checkin/event/{eventId}/manual-entry` - Create walk-in entry
- `GET /api/checkin/event/{eventId}/dashboard` - Real-time dashboard data
- `GET /api/checkin/event/{eventId}/capacity` - Capacity information
- `GET /api/checkin/event/{eventId}/export` - Export attendance data

## Performance Characteristics

### Token Validation
- **Cache**: In-memory cache with TTL matching token expiration
- **Fast Path**: Cache hit = O(1) validation (no database query)
- **Slow Path**: Cache miss = Single database query with composite index
- **Last Used Tracking**: Fire-and-forget update (doesn't block validation)

### Admin Operations
- **Generate**: Single database insert + cache update
- **Revoke**: Single database update + cache invalidation
- **List**: Single database query with event filter

## Security Improvements

### Attack Surface Reduction
1. **No Persistent Credentials**: Volunteers never receive passwords
2. **Time-Limited**: Cannot use old tokens after expiration
3. **Event-Scoped**: Token cannot be used for different events
4. **Revocable**: Emergency revocation doesn't require password changes
5. **No Role Confusion**: Cannot accidentally grant more permissions

### Audit Trail
- Token creation logged with admin user ID
- Every token usage updates LastUsedAt timestamp
- Revocation logged with revoking admin user ID
- Complete history for security investigations

## See Also

**Design Documents:**
- Original Security Analysis: `/docs/functional-areas/events/new-work/2025-08-24-events-management/requirements/check-in-security-analysis.md`
- Technical Design: `/docs/functional-areas/api-cleanup/new-work/2025-09-12-legacy-feature-extraction/design/checkin-system-technical-design.md`
- UI Design: Various wireframes in functional areas

**Implementation References:**
- Test Catalog: `/docs/standards-processes/testing/TEST_CATALOG.md`
- File Registry: `/docs/architecture/file-registry.md` (entries from 2025-11-02 to 2025-11-03)
- Session Work: `/session-work/2025-11-03/phase7-cleanup-summary.md`

## Lessons Learned

### What Worked Well
1. **Phased Approach**: 7 phases allowed systematic migration without breaking changes
2. **Backend First**: Implementing token system before removing role checks
3. **Comprehensive Testing**: Updating all E2E tests ensured no regressions
4. **Documentation**: Clear technical design prevented implementation confusion

### Challenges Encountered
1. **Test Migration**: 27 E2E tests required careful updates
2. **Cache Invalidation**: Needed proper cache management for revoked tokens
3. **Frontend State**: Token parsing from URL required careful React state management

### Future Considerations
1. **QR Code Generation**: Could add server-side QR code generation for token URLs
2. **Token Refresh**: Could implement token refresh for multi-day events
3. **Analytics**: Could track token usage patterns for security insights
4. **Mobile App**: Token system ready for native mobile check-in app

---

**Archive Date**: 2025-11-03
**Archived By**: Librarian Agent
**Reason**: Migration from role-based to token-based check-in access complete

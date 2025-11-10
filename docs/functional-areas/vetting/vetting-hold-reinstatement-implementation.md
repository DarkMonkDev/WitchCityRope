# Vetting Hold/Reinstatement Implementation

**Date**: 2025-11-09
**Status**: Complete
**Developer**: backend-developer agent

## Overview

Implemented backend API endpoints and business logic for membership hold/reinstatement functionality. This allows approved users to voluntarily place their membership on hold and later request reinstatement through a Final Review process.

## Implementation Summary

### Features Implemented

1. **PUT /api/users/{userId}/vetting/hold** - Place membership on hold
2. **PUT /api/users/{userId}/vetting/reinstate** - Request reinstatement
3. **GET /api/users/{userId}/vetting/hold-status** - Get current hold/reinstatement status

### Business Logic

#### Place Membership on Hold
- **Precondition**: User must be Approved (VettingStatus == 3)
- **Actions**:
  1. Changes `User.VettingStatus` to OnHold (5)
  2. Updates `VettingApplication.WorkflowStatus` to OnHold (5) if application exists
  3. Creates UserNote (NoteType: "StatusChange")
  4. Creates VettingAuditLog entry
  5. Cancels all future social event RSVPs (EventType == Social, StartDate > now)
  6. Sends email notification to admin (logged to console)
- **Response**: Returns new status, status name, and timestamp

#### Request Reinstatement
- **Precondition**: User must be OnHold (VettingStatus == 5)
- **Actions**:
  1. Changes `User.VettingStatus` to FinalReview (2)
  2. Updates `VettingApplication.WorkflowStatus` to FinalReview (2)
  3. Creates UserNote (NoteType: "StatusChange")
  4. Creates VettingAuditLog entry
  5. Sends email notification to admin (logged to console)
- **Response**: Returns new status, status name, and timestamp

#### Get Hold Status
- **Actions**:
  1. Returns current vetting status
  2. Indicates whether user can place on hold (if Approved)
  3. Indicates whether user can request reinstatement (if OnHold)
  4. Returns last status change date from UserNotes
- **Response**: Returns status information and available actions

### Security & Authorization

- All endpoints require authentication
- Users can only operate on their own profile (userId must match authenticated user)
- Returns 401 if not authenticated
- Returns 403 if trying to modify different user
- Returns 400 if user in wrong status for operation

### Database Operations

- Uses transactions for atomicity
- Updates multiple entities in single transaction:
  - User table (VettingStatus)
  - VettingApplication table (WorkflowStatus) if exists
  - UserNotes table (status change notes)
  - VettingAuditLog table (audit trail)
  - EventAttendances table (cancel future RSVPs)
- Proper error handling with rollback on failure

### Email Notifications

- Admin notifications sent to configured email address
- Currently logs to console (ready for email service integration)
- Non-blocking - failures logged but don't fail the operation
- Includes:
  - User information (SceneName, Email)
  - Reason provided
  - Timestamp
  - Additional context (e.g., RSVPs cancelled count)

## Files Created

### Feature Folder: `/apps/api/Features/VettingHold/`

1. **Models/VettingHoldModels.cs**
   - `PlaceMembershipOnHoldRequest` - Request DTO with reason
   - `RequestReinstatementRequest` - Request DTO with reason
   - `MembershipHoldResponse` - Response with new status and timestamp
   - `VettingHoldStatusResponse` - Response with status and available actions

2. **Services/IVettingHoldService.cs**
   - Service interface with XML documentation
   - Three methods: PlaceMembershipOnHoldAsync, RequestReinstatementAsync, GetHoldStatusAsync

3. **Services/VettingHoldService.cs**
   - Complete implementation with business logic
   - Transaction management
   - Error handling with Result pattern
   - Structured logging
   - Admin email notifications

4. **Endpoints/VettingHoldEndpoints.cs**
   - Three endpoints with proper OpenAPI documentation
   - Authentication and authorization checks
   - ApiResponse wrapper pattern
   - Proper HTTP status codes

## Files Modified

1. **`/apps/api/Features/Shared/Extensions/ServiceCollectionExtensions.cs`**
   - Added `using WitchCityRope.Api.Features.VettingHold.Services;`
   - Registered `IVettingHoldService` with DI container

2. **`/apps/api/Features/Shared/Extensions/WebApplicationExtensions.cs`**
   - Added `using WitchCityRope.Api.Features.VettingHold.Endpoints;`
   - Registered `MapVettingHoldEndpoints()` call

3. **OpenAPI Spec Regenerated**
   - `/apps/api/openapi.json` - Updated with 3 new endpoints
   - `/packages/shared-types/src/generated/api-types.ts` - TypeScript types regenerated
   - `/packages/shared-types/src/generated/api-client.ts` - API client updated
   - `/packages/shared-types/src/generated/api-helpers.ts` - Helper functions updated

## Technical Details

### VettingStatus Enum Values
```csharp
0 = NotStarted
1 = Submitted
2 = FinalReview
3 = Approved
4 = Denied
5 = OnHold
6 = Withdrawn
7 = InterviewScheduled
8 = InterviewCompleted
```

### Status Transitions
- **Place on Hold**: Approved (3) → OnHold (5)
- **Request Reinstatement**: OnHold (5) → FinalReview (2)

### RSVP Cancellation Logic
```csharp
// Cancels all future social event RSVPs
var futureRsvps = await _context.EventAttendances
    .Include(ea => ea.Event)
    .Where(ea => ea.UserId == userId &&
                ea.Status == AttendanceStatus.Active &&
                ea.AttendanceType == AttendanceType.RSVP &&
                ea.Event!.EventType == EventType.Social &&
                ea.Event.StartDate > now)
    .ToListAsync(cancellationToken);
```

### Error Handling
- Uses Result pattern for consistent error handling
- Transaction rollback on any failure
- Detailed error logging with context
- Proper HTTP status codes returned

### Logging
- Structured logging with context (userId, reason, status changes)
- Info level for successful operations
- Error level for failures
- Includes RSVP cancellation counts

## Testing Recommendations

### Unit Tests
1. Test status transitions (Approved → OnHold → FinalReview)
2. Test validation (wrong status, missing reason)
3. Test authorization (different user, not authenticated)
4. Test RSVP cancellation logic
5. Test transaction rollback on errors

### Integration Tests
1. End-to-end hold placement workflow
2. End-to-end reinstatement workflow
3. Verify database state after operations
4. Verify audit log entries created
5. Verify UserNotes created correctly

### Manual Testing
1. Use test account with Approved status
2. Place on hold, verify RSVPs cancelled
3. Request reinstatement, verify status change
4. Check admin email notifications (console logs)
5. Verify unauthorized access blocked

## API Documentation

The endpoints are automatically documented in the OpenAPI specification at:
- http://localhost:5655/swagger
- http://localhost:5655/openapi/v1.json

## Future Enhancements

1. **Email Service Integration**: Replace console logging with actual email service
2. **Admin Dashboard**: UI for viewing hold/reinstatement requests
3. **Notification System**: User notifications when status changes
4. **History Tracking**: UI to view status change history
5. **Bulk Operations**: Admin ability to place multiple users on hold

## Notes

- No database schema changes required - all tables and columns already exist
- Uses existing UserNote and VettingAuditLog tables
- Follows existing patterns from Vetting and Participation features
- Transaction management ensures data consistency
- Email notifications ready for production email service integration
- TypeScript types auto-generated for frontend consumption

## Related Documentation

- Database Schema Analysis: `/docs/functional-areas/vetting/vetting-hold-reinstatement-database-analysis.md`
- Vetting Feature: `/docs/functional-areas/vetting/`
- Participation Feature: `/docs/functional-areas/participation/`

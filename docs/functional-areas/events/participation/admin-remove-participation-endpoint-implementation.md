# Admin Remove Participation Endpoint Implementation

**Date**: 2025-11-09
**Developer**: backend-developer agent
**Feature**: Admin endpoint to remove user's RSVP/attendance from event
**Status**: ✅ COMPLETE - Implementation ready for testing

---

## Executive Summary

Implemented a new admin API endpoint that allows administrators to remove a user's attendance (either RSVP or ticket) from an event. This is a **simple removal endpoint** that marks attendance as cancelled without processing refunds or cascading to volunteer shifts.

**Key Distinction**: This endpoint is different from the existing `/remove` endpoint which has complex cascading logic for refunds and volunteer shift cancellation. This new endpoint is for simple attendance removal only.

---

## Implementation Details

### Endpoint Specification

**URL**: `DELETE /api/admin/events/{eventId}/participations/{userId}`

**Authorization**: Requires `Administrator` role

**Parameters**:
- `eventId` (Guid) - The event ID (from URL path)
- `userId` (Guid) - The user ID whose attendance to remove (from URL path)

**Response Codes**:
- `204 No Content` - Success, attendance removed
- `401 Unauthorized` - Admin authentication failed
- `403 Forbidden` - User lacks Administrator role
- `404 Not Found` - No active attendance found for this user and event
- `500 Internal Server Error` - Server error during processing

---

## Business Logic

### What It Does

1. **Validates Admin Authentication**
   - Extracts admin user ID from JWT token claims
   - Returns 401 if authentication fails

2. **Finds Active Attendance**
   - Queries `EventAttendances` table for any active attendance
   - Matches on `EventId`, `UserId`, and `Status == Active`
   - Works for both RSVP and Ticket attendance types

3. **Marks Attendance as Cancelled**
   - Sets `Status = Cancelled`
   - Sets `CancelledAt = DateTime.UtcNow`
   - Sets `CancellationReason = "Removed by admin {adminUserId}"`
   - Sets `UpdatedBy = adminUserId`
   - Sets `UpdatedAt = DateTime.UtcNow`

4. **Updates EventAttendee Status (if applicable)**
   - Checks if user has any remaining active attendances for this event
   - If no active attendances remain:
     - Finds `EventAttendee` record
     - Sets `RegistrationStatus = "cancelled"`
     - Sets `UpdatedAt = DateTime.UtcNow`

5. **Saves Changes and Returns Success**
   - Commits all changes to database
   - Logs success message
   - Returns 204 No Content

### What It Does NOT Do

❌ **Does NOT process refunds** - For paid tickets, use the separate refund endpoint
❌ **Does NOT cancel volunteer shifts** - Use the cascading removal endpoints if needed
❌ **Does NOT send email notifications** - Simple removal only
❌ **Does NOT validate event dates** - Allows removal from past events

---

## Code Location

**File**: `/home/chad/repos/witchcityrope/apps/api/Features/Participation/Endpoints/ParticipationEndpoints.cs`

**Lines**: 447-530

**Method**: Inline async lambda in `MapDelete()` call

---

## Related Endpoints

This endpoint is part of a family of admin participation management endpoints:

1. **NEW**: `DELETE /api/admin/events/{eventId}/participations/{userId}` - **Simple removal** (this endpoint)
2. **Existing**: `DELETE /api/admin/events/{eventId}/participations/{userId}/remove` - **RSVP removal with cascading** (refunds + volunteer shifts)
3. **Existing**: `POST /api/admin/events/{eventId}/tickets/{userId}/refund` - **Ticket refund with optional RSVP removal**

---

## Frontend Integration

### Expected Frontend Call Pattern

```typescript
// Example frontend implementation (to be created)
const removeParticipation = async (eventId: string, userId: string) => {
  const response = await fetch(
    `/api/admin/events/${eventId}/participations/${userId}`,
    {
      method: 'DELETE',
      credentials: 'include' // For cookie-based auth
    }
  );

  if (response.status === 204) {
    // Success - refresh participant list
    queryClient.invalidateQueries(['eventParticipations', eventId]);
    notifications.show({
      message: 'Attendance removed successfully',
      color: 'green'
    });
  } else if (response.status === 404) {
    notifications.show({
      message: 'No active attendance found for this user',
      color: 'red'
    });
  }
};
```

### Frontend Location (from EventForm.tsx comment)

**File**: `/home/chad/repos/witchcityrope/apps/web/src/components/events/EventForm.tsx`

**Line**: 882

**Current Code**:
```typescript
// TODO: Replace with actual admin API endpoint when backend is ready
// await fetch(`/api/admin/events/${eventId}/participations/${selectedParticipant.userId}/remove`, { method: 'DELETE' })
```

**Note**: The frontend currently has a TODO comment pointing to the old `/remove` endpoint. The frontend can now use either:
- The new simple endpoint: `/api/admin/events/${eventId}/participations/${userId}` (for simple removal)
- The existing cascading endpoint: `/api/admin/events/${eventId}/participations/${userId}/remove` (for removal with refunds/volunteer cancellation)

---

## Database Changes

**Tables Modified**:
1. `EventAttendances` - Status changed to Cancelled, timestamps updated
2. `EventAttendees` - RegistrationStatus changed to "cancelled" (if no active attendances remain)

**No Schema Changes Required** - All fields already exist

---

## Logging

Structured logging included:

**On Request**:
```
Admin {AdminUserId} removing attendance for user {UserId} from event {EventId}
```

**On Success**:
```
Admin {AdminUserId} successfully removed {AttendanceType} attendance for user {UserId} from event {EventId}
```

**Log Level**: Information

---

## Testing Recommendations

### Unit Tests (Not Created - Suggested)

```csharp
// Suggested test file location:
// /tests/unit/api/Features/Participation/AdminRemoveParticipationTests.cs

[Fact]
public async Task AdminRemoveParticipation_RemovesRSVP_ReturnsNoContent() { }

[Fact]
public async Task AdminRemoveParticipation_RemovesTicket_ReturnsNoContent() { }

[Fact]
public async Task AdminRemoveParticipation_NoActiveAttendance_Returns404() { }

[Fact]
public async Task AdminRemoveParticipation_NonAdmin_Returns403() { }

[Fact]
public async Task AdminRemoveParticipation_UpdatesEventAttendeeStatus() { }
```

### Integration Tests (Not Created - Suggested)

Test against real database with:
- Admin authentication
- Creating attendance records
- Verifying cancellation
- Checking EventAttendee status updates

### Manual Testing Checklist

- [ ] Test with RSVP attendance
- [ ] Test with ticket attendance
- [ ] Test with non-existent attendance (should return 404)
- [ ] Test without admin role (should return 403)
- [ ] Test without authentication (should return 401)
- [ ] Verify EventAttendee status updates correctly
- [ ] Verify database changes persist correctly
- [ ] Verify logging works correctly

---

## OpenAPI Documentation

**Endpoint Name**: `AdminRemoveParticipation`

**Summary**: "Remove user's attendance (admin only)"

**Description**: "Removes user's attendance (RSVP or ticket) from event. Does not process refunds - use refund endpoint separately for paid tickets. Admin role required."

**Tags**: `Admin`, `Participation`

**Note**: OpenAPI spec will be regenerated on next API start

---

## Compilation Status

✅ **API Compiles Successfully**

Verified compilation:
```bash
docker-compose exec -T api dotnet build
```

Result:
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

---

## Design Decisions

### Why a Separate Simple Endpoint?

1. **Separation of Concerns**: Simple removal vs. complex cascading logic
2. **Clear Intent**: Admin knows exactly what will happen (just remove attendance)
3. **Flexibility**: Admin can choose to handle refunds/volunteer shifts separately if needed
4. **Simpler Code**: Less complex than the existing `/remove` endpoint

### Why Not Use the Existing `/remove` Endpoint?

The existing `/remove` endpoint:
- Only handles RSVP removal specifically
- Automatically processes ticket refunds
- Automatically cancels volunteer shifts
- Has complex cascading logic

The new endpoint:
- Handles ANY attendance type (RSVP or Ticket)
- Does NOT process refunds
- Does NOT cancel volunteer shifts
- Simple, focused logic

---

## Known Limitations

1. **No Refund Processing**: For paid tickets, admin must separately process refunds
2. **No Volunteer Shift Cancellation**: Admin must separately cancel volunteer shifts if needed
3. **No Email Notifications**: Does not send email to user about cancellation
4. **No Audit Trail Details**: Basic logging only, no detailed audit records

**If these features are needed, use the existing cascading endpoints instead.**

---

## Future Enhancements (Suggested)

1. **Add Email Notifications**: Send cancellation email to affected user
2. **Add Detailed Audit Trail**: Create UserNote or attendance history record
3. **Add Reason Parameter**: Allow admin to specify cancellation reason via request body
4. **Add Bulk Removal**: Support removing multiple users at once
5. **Add Validation**: Prevent removal from past events if business rules require it

---

## Success Criteria

✅ **Implementation Complete**:
- [x] Endpoint created and compiles
- [x] Admin authorization enforced
- [x] Finds and cancels active attendance
- [x] Updates EventAttendee status when applicable
- [x] Structured logging implemented
- [x] Error handling for 401/404/500
- [x] OpenAPI documentation added
- [x] File registry updated

⏳ **Pending Testing**:
- [ ] Manual testing by admin user
- [ ] Frontend integration
- [ ] Unit tests created
- [ ] Integration tests created

---

## Related Documentation

- **Task Request**: User provided task description
- **Existing Endpoints**: Lines 533-801 in ParticipationEndpoints.cs
- **File Registry**: `/home/chad/repos/witchcityrope/docs/architecture/file-registry.md` (Line 95)
- **Entity Models**: `/home/chad/repos/witchcityrope/apps/api/Features/Participation/Entities/EventAttendance.cs`

---

## Conclusion

The admin remove participation endpoint is now implemented and ready for frontend integration and testing. It provides a simple, focused way for administrators to remove user attendance from events without the complexity of cascading refunds and volunteer shift cancellations.

**Next Steps**:
1. Frontend developer: Update EventForm.tsx to use the new endpoint
2. Test developer: Create unit and integration tests
3. Test executor: Run manual testing against Docker environment
4. Product owner: Verify behavior matches business requirements

**Implementation Time**: ~15 minutes
**Status**: ✅ Ready for testing and frontend integration

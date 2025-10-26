# Auto-RSVP Functionality and User Volunteer Shifts Implementation

**Date**: 2025-10-26
**Author**: Backend Developer Agent
**Status**: Implemented and Built Successfully

## Summary

Implemented auto-RSVP functionality for volunteer signups and created a new API endpoint to fetch user volunteer shifts for the dashboard.

## Requirements

### Auto-RSVP Logic

When a user signs up for a volunteer position on a **social event**:
1. Check if the event is a social event (EventType.Social)
2. Check if the user already has an active RSVP for this event
3. If NO RSVP exists, automatically create one for them

**Why**: Volunteers need to attend the event, so auto-RSVPing simplifies the process. Only applies to social events since class/workshop events require ticket purchases.

### User Volunteer Shifts Endpoint

Frontend needs an endpoint to fetch user's volunteer shifts for the dashboard:

**Endpoint**: `GET /api/user/volunteer-shifts`

**Response Format**:
```csharp
public class UserVolunteerShiftDto
{
    public Guid SignupId { get; set; }
    public string EventTitle { get; set; }
    public string EventLocation { get; set; }
    public DateTime EventDate { get; set; }
    public string PositionTitle { get; set; }
    public string? SessionName { get; set; }
    public DateTime? ShiftStartTime { get; set; }
    public DateTime? ShiftEndTime { get; set; }
}
```

**Filter**: Only returns upcoming volunteer shifts (events in the future)

## Implementation Details

### Files Created

1. **`/home/chad/repos/witchcityrope/apps/api/Features/Volunteers/Models/UserVolunteerShiftDto.cs`**
   - New DTO for volunteer shift information
   - Contains event details, position title, session name, and shift times

### Files Modified

1. **`/home/chad/repos/witchcityrope/apps/api/Features/Volunteers/Services/VolunteerService.cs`**

   **Updated `SignupForPositionAsync` method (lines 189-224)**:
   - Added conditional check for social events: `if (position.Event?.EventType == Enums.EventType.Social)`
   - Only auto-RSVPs for social events
   - Logs appropriate messages for different scenarios:
     - Auto-RSVP successful
     - User already has active participation (skip)
     - Event is not a social event (skip)

   **Added `GetUserVolunteerShiftsAsync` method (lines 256-312)**:
   - Queries VolunteerSignups with includes for Event and Session
   - Filters for confirmed signups only
   - Filters for upcoming events only (`StartDate >= DateTime.UtcNow`)
   - Orders by event start date
   - Maps to UserVolunteerShiftDto
   - Returns tuple with (success, shifts, error)

2. **`/home/chad/repos/witchcityrope/apps/api/Features/Volunteers/Endpoints/VolunteerEndpoints.cs`**

   **Updated signup endpoint description (line 130)**:
   - Changed from "Automatically RSVPs user to the event if not already registered"
   - To: "Automatically RSVPs user to social events if not already registered"

   **Added new endpoint `GET /api/user/volunteer-shifts` (lines 139-199)**:
   - Requires authentication
   - Extracts user ID from ClaimsPrincipal
   - Calls VolunteerService.GetUserVolunteerShiftsAsync
   - Returns ApiResponse<List<UserVolunteerShiftDto>> wrapper
   - Provides meaningful messages for empty results
   - Documented with OpenAPI metadata

## Business Rules Applied

### Auto-RSVP Logic

1. **Event Type Check**: Only auto-RSVPs for `EventType.Social` events
2. **Existing Participation Check**: Skips if user already has active participation
3. **Silent Failure**: Logs but doesn't block volunteer signup if RSVP creation fails
4. **Participation Type**: Creates RSVP (not ticket purchase)
5. **Participation Status**: Creates with `ParticipationStatus.Active`

### User Volunteer Shifts Query

1. **Authentication Required**: Endpoint requires authenticated user
2. **Own Data Only**: Returns only current user's volunteer shifts
3. **Confirmed Only**: Filters for `VolunteerSignupStatus.Confirmed`
4. **Upcoming Only**: Filters for events with `StartDate >= DateTime.UtcNow`
5. **Ordered**: Returns shifts ordered by event start date

## Technical Patterns Applied

### Result Pattern
Both service methods return tuple format: `(bool success, TData? data, string? error)`
- Consistent with existing codebase patterns
- Enables detailed error messages
- Supports null data responses

### API Response Wrapper
Endpoint returns `ApiResponse<List<UserVolunteerShiftDto>>` format:
```csharp
{
    "success": true,
    "data": [...],
    "message": "Found 3 upcoming volunteer shift(s)",
    "timestamp": "2025-10-26T..."
}
```

### Entity Framework Patterns
- Uses `.AsNoTracking()` for read-only queries
- Uses `.Include()` with `.ThenInclude()` for nested navigation properties
- Filters in database query, not in memory
- Projects to DTO immediately after query

### Logging
- Structured logging with context (userId, eventId, eventType)
- Informational logging for successful operations
- Error logging for exceptions with full context
- Debug-friendly messages for troubleshooting

## Build Status

✅ **Build Successful**
- 0 Compilation Errors
- 5 Warnings (pre-existing, not related to changes)
- OpenAPI spec regenerated with 92 endpoint paths

## Testing Recommendations

### Manual Testing

1. **Auto-RSVP for Social Event**:
   ```bash
   # Sign up for volunteer position on social event
   POST /api/volunteer-positions/{id}/signup

   # Verify auto-RSVP created
   GET /api/events/{eventId}/participation
   # Should show hasRSVP: true
   ```

2. **No Auto-RSVP for Class Event**:
   ```bash
   # Sign up for volunteer position on class event
   POST /api/volunteer-positions/{id}/signup

   # Verify no auto-RSVP created (class events require tickets)
   GET /api/events/{eventId}/participation
   # Should show hasRSVP: false, hasTicket: false
   ```

3. **Get User Volunteer Shifts**:
   ```bash
   # Fetch user's upcoming volunteer shifts
   GET /api/user/volunteer-shifts

   # Should return array of shifts with event details
   ```

### Integration Testing

Test scenarios to validate:
- Auto-RSVP creates participation record for social events
- Auto-RSVP skips for non-social events
- Auto-RSVP skips if user already has active participation
- Volunteer shifts endpoint returns only upcoming events
- Volunteer shifts endpoint returns only confirmed signups
- Volunteer shifts endpoint requires authentication
- Volunteer shifts endpoint returns empty array when no shifts

## Error Handling

### Auto-RSVP Errors
- If auto-RSVP fails, logs error but doesn't block volunteer signup
- Volunteer signup still succeeds
- User can manually RSVP if needed

### User Volunteer Shifts Errors
- Invalid user ID format: Returns error message
- Database query fails: Returns 500 with generic error message
- No authentication: Returns 401 Unauthorized
- Empty results: Returns 200 with empty array and informative message

## Security Considerations

1. **Authentication Required**: Volunteer shifts endpoint requires authenticated user
2. **Own Data Only**: User can only view their own volunteer shifts (user ID from claims)
3. **No Sensitive Data**: Response doesn't include internal IDs or sensitive information
4. **SQL Injection Prevention**: Uses parameterized queries via EF Core

## Performance Considerations

1. **Efficient Queries**:
   - Uses `.AsNoTracking()` for read-only operations
   - Filters in database, not in memory
   - Single query with includes instead of N+1 queries

2. **Minimal Data Transfer**:
   - Projects to DTO immediately
   - Only loads necessary fields
   - Filters for upcoming events at database level

3. **Indexing Recommendations**:
   - Consider index on `VolunteerSignups.UserId` + `Status`
   - Consider index on `Events.StartDate` for future date queries

## Documentation Updates

Updated OpenAPI documentation:
- Endpoint summary clearly states social event limitation
- New endpoint documented with tags, summary, description
- Response types documented with status codes

## Related Features

This implementation supports:
- **User Dashboard**: Shows upcoming volunteer commitments
- **Event Participation**: Auto-RSVP simplifies volunteer workflow
- **Event Details**: Volunteers can see they're registered

## Next Steps

Frontend integration:
1. Update dashboard to call `GET /api/user/volunteer-shifts`
2. Display volunteer shifts in dashboard UI
3. Update volunteer signup flow to show auto-RSVP notification for social events
4. Add filtering/sorting options for volunteer shifts if needed

## References

- Event Type Enum: `/home/chad/repos/witchcityrope/apps/api/Enums/EventType.cs`
- VolunteerSignup Entity: `/home/chad/repos/witchcityrope/apps/api/Models/VolunteerSignup.cs`
- ParticipationStatus Enum: Referenced in EventParticipation entity
- Backend Lessons Learned: `/home/chad/repos/witchcityrope/docs/lessons-learned/backend-developer-lessons-learned-3.md`

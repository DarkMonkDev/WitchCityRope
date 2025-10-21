# Volunteer Position Card Debugging - 2025-10-20

## Issues to Fix

### Issue 1: Start/Stop Times Not Displaying
**Problem**: Times are not showing up on the volunteer position cards even though the code includes the display logic.

**Investigation**:
1. Checked C# DTO (`apps/api/Features/Volunteers/Models/VolunteerModels.cs`):
   - DTO has `SessionStartTime` and `SessionEndTime` fields (DateTime?)
   - Service layer correctly maps these from `vp.Session?.StartTime` and `vp.Session?.EndTime`

2. Checked TypeScript interface (`apps/web/src/features/volunteers/types/volunteer.types.ts`):
   - Interface had the fields defined
   - Updated to add `| null` union type to match C# nullable DateTime

3. Checked component code (`apps/web/src/features/volunteers/components/VolunteerPositionCard.tsx`):
   - Component has correct conditional rendering: `{position.sessionStartTime && position.sessionEndTime && ...}`
   - Component has `formatTime()` function that handles ISO date strings
   - Added console.log to debug actual data being passed

**Status**: Types now match exactly. Need to test with browser to see actual API response.

### Issue 2: Button Layout with Description
**Problem**: Description and "Sign Up" button should be on the same line with text wrapping around the button.

**Solution**: Changed layout from separate elements to a single Group component:
- Used `Group` with `align="flex-start"` and `wrap="nowrap"`
- Description Text has `flex: 1` to take available space
- Button has `flexShrink: 0` to maintain its size
- Text will wrap naturally while button stays right-aligned

**Status**: ✅ FIXED - Code updated in component

## Files Modified

1. `/home/chad/repos/witchcityrope/apps/web/src/features/volunteers/components/VolunteerPositionCard.tsx`
   - Lines 133-162: Changed description and button layout to use Group with flex
   - Lines 21-29: Added console.log for debugging time fields

2. `/home/chad/repos/witchcityrope/apps/web/src/features/volunteers/types/volunteer.types.ts`
   - Lines 1-28: Updated interface to add `| null` union types for optional fields
   - Added comments documenting that types must match C# DTO exactly

## Next Steps

1. Test in browser to verify:
   - Times display correctly when data exists
   - Description and button are on same line
   - Text wraps properly on mobile/tablet/desktop
   - Console logs show correct data from API

2. Remove console.log after verification

3. Consider if volunteer positions need sessions or if they use event-level times

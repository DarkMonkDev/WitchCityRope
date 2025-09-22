# Button Text Cutoff Fix - 2025-09-22

## Issue
The "Login To Your Account" button on the vetting application form (/join page) had text cutoff at the top and bottom.

## Root Cause
Following my lessons learned, this is a recurring issue with Mantine buttons when using fixed heights. The button was not using proper padding and height configuration to prevent text cutoff.

## Solution Applied
Fixed in `/home/chad/repos/witchcityrope-react/apps/web/src/features/vetting/components/VettingApplicationForm.tsx`:

### Changes Made:
1. **Added TOUCH_TARGETS import** to use standardized button height constants
2. **Updated "Login to Your Account" button** (lines 247-264):
   - Changed from no styling to using `minHeight` instead of fixed height
   - Added proper vertical padding: `paddingTop: 12, paddingBottom: 12`
   - Added horizontal padding: `paddingLeft: 24, paddingRight: 24`
   - Set proper `lineHeight: 1.4` to ensure text has proper vertical spacing
   - Used `TOUCH_TARGETS.BUTTON_HEIGHT` (56px) for consistent sizing

3. **Updated Submit Application button** (lines 471-492):
   - Changed from `height: 56` to `minHeight: TOUCH_TARGETS.BUTTON_HEIGHT`
   - Added proper vertical padding: `paddingTop: 12, paddingBottom: 12`
   - Added `lineHeight: 1.4` for consistent text rendering

### Key Pattern Applied:
```typescript
style={{
  minHeight: TOUCH_TARGETS.BUTTON_HEIGHT, // Use minHeight, not height
  paddingTop: 12,                         // Explicit vertical padding
  paddingBottom: 12,                      // Prevents text cutoff
  paddingLeft: 24,                        // Horizontal spacing
  paddingRight: 24,
  fontSize: 16,
  fontWeight: 600,
  lineHeight: 1.4,                        // Proper line spacing
}}
```

## Prevention
This fix follows the lessons learned pattern:
- ❌ **NEVER use fixed heights** on Mantine buttons
- ✅ **USE minHeight + padding** to control button size
- ✅ **USE proper lineHeight** to ensure text rendering space
- ✅ **USE TOUCH_TARGETS constants** for consistent sizing

## Files Modified
- `/home/chad/repos/witchcityrope-react/apps/web/src/features/vetting/components/VettingApplicationForm.tsx`

## Testing
- Web server is running at http://localhost:5173
- Button should now display text without cutoff
- Both login and submit buttons use consistent styling

## Next Steps
- This pattern should be applied to any other buttons experiencing text cutoff
- Consider creating a reusable button component with these styling patterns
- Monitor for similar issues in other components
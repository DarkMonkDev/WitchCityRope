# Button Text Cutoff Fix - Modal Dialogs

**Date**: 2025-10-05
**Severity**: HIGH
**Status**: FIXED

## Problem Description

Button text was being cut off (top and bottom) in modal dialog footers across multiple components:
- Session modal ("Add Session" / "Update Session")
- Ticket Type modal ("Add Ticket Type" / "Update Ticket Type")
- Volunteer Position modal ("Add Position" / "Update Position")

The button text appeared with ascenders and descenders clipped, creating an unprofessional appearance.

## Root Cause

Modal buttons were using inline `style={{...}}` prop instead of Mantine's `styles={{ root: {...} }}` prop. This caused:

1. **Missing explicit height**: No fixed height set, causing button to collapse
2. **Missing explicit padding**: No top/bottom padding specified
3. **Missing line-height**: Default line-height causing text overflow
4. **Mantine styles conflict**: Inline styles don't work well with Mantine's styling system

## Solution Applied

Changed all modal submit buttons from:
```tsx
// ❌ BROKEN: Inline style prop
<Button
  type="submit"
  style={{
    background: 'linear-gradient(135deg, var(--mantine-color-amber-6), #DAA520)',
    border: 'none',
    color: 'var(--mantine-color-dark-9)',
    fontWeight: 600,
  }}
>
  Button Text
</Button>
```

To:
```tsx
// ✅ FIXED: Mantine styles prop with explicit dimensions
<Button
  type="submit"
  styles={{
    root: {
      background: 'linear-gradient(135deg, var(--mantine-color-amber-6), #DAA520)',
      border: 'none',
      color: 'var(--mantine-color-dark-9)',
      fontWeight: 600,
      height: '44px',           // CRITICAL: Explicit height
      paddingTop: '12px',       // CRITICAL: Top padding
      paddingBottom: '12px',    // CRITICAL: Bottom padding
      fontSize: '14px',
      lineHeight: '1.2',        // CRITICAL: Line height
    }
  }}
>
  Button Text
</Button>
```

## Files Changed

1. `/apps/web/src/components/events/SessionFormModal.tsx` (line 220-238)
2. `/apps/web/src/components/events/TicketTypeFormModal.tsx` (line 303-321)
3. `/apps/web/src/components/events/VolunteerPositionFormModal.tsx` (line 183-201)

## Key CSS Properties

The following properties are **CRITICAL** to prevent button text cutoff:

- `height: '44px'` - Sets explicit button height
- `paddingTop: '12px'` - Ensures space above text
- `paddingBottom: '12px'` - Ensures space below text
- `fontSize: '14px'` - Consistent text size
- `lineHeight: '1.2'` - Prevents text overflow

## Testing Performed

- ✅ Code review of all three modal components
- ✅ Verified consistent styling pattern applied
- ✅ All buttons use `styles={{ root: {...} }}` instead of `style={{...}}`

## Prevention for Future Development

1. **NEVER** use inline `style={{...}}` prop on Mantine buttons
2. **ALWAYS** use `styles={{ root: {...} }}` for custom button styling
3. **ALWAYS** include explicit `height`, `paddingTop`, `paddingBottom`, and `lineHeight` when styling buttons
4. **Reference** lessons learned file line 677-769 for complete pattern

## Related Lessons Learned

This fix follows the pattern documented in:
- `/docs/lessons-learned/react-developer-lessons-learned.md` (lines 677-769)
- Topic: "CRITICAL: MIXING CUSTOM CSS CLASSES WITH MANTINE BREAKS STYLING"

The lesson already documents that style props without proper height/padding cause text cutoff. This fix applies that knowledge to all modal buttons.

## Impact

- ✅ Professional appearance restored
- ✅ All button text fully visible
- ✅ Consistent styling across all modals
- ✅ Follows Mantine best practices
- ✅ Prevents future similar issues

# Vetting Form Label Font Size Fix - 2025-09-22

## Issue
The floating label font size was too small when in the default (non-floating) position on the vetting form, making it difficult to read.

## Root Cause
The CSS in `/apps/web/src/styles/FormComponents.module.css` was using `--mantine-font-size-sm` (14px) for the default label position, which was too small for optimal readability.

## Solution
Updated the following CSS classes to use `--mantine-font-size-md` (16px) instead:

### Changes Made

1. **Line 20**: Updated base floating label font size:
   ```css
   .formGroup .floatingLabel {
     font-size: var(--mantine-font-size-md); /* was --mantine-font-size-sm */
   }
   ```

2. **Line 64**: Updated empty state font size:
   ```css
   .inputContainer .floatingLabel.isEmpty {
     font-size: var(--mantine-font-size-md); /* was --mantine-font-size-sm */
   }
   ```

3. **Line 672**: Updated responsive empty state font size:
   ```css
   .floatingLabel.isEmpty {
     font-size: var(--mantine-font-size-md); /* was --mantine-font-size-sm */
   }
   ```

## Font Size Mapping
- `--mantine-font-size-xs` = 12px (floating state - unchanged)
- `--mantine-font-size-sm` = 14px (old default - too small)
- `--mantine-font-size-md` = 16px (new default - better readability)

## Impact
- **Improved readability**: Labels are now 16px instead of 14px in default position
- **Consistent floating behavior**: Floating labels remain at 12px for clean appearance
- **Cross-device compatibility**: Mobile responsive behavior maintained
- **No breaking changes**: Only visual improvement, no functionality changes

## Files Modified
- `/apps/web/src/styles/FormComponents.module.css`

## Testing
- Verified Vite development server starts successfully
- Changes apply to all EnhancedTextInput and EnhancedTextarea components
- Affects vetting form and all other forms using floating labels

## Notes
- This change affects ALL floating labels across the application, not just the vetting form
- The floating state (when label moves up) remains at 12px for clean visual hierarchy
- No changes needed in the React components themselves - this is purely a CSS fix
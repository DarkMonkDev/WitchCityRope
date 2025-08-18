# Placeholder Visibility and Label Positioning Fixes - 2025-08-18

## Issues Fixed

### 1. Placeholder Always Visible Issue
**Problem**: Placeholders were visible all the time, regardless of focus state.
**Root Cause**: The placeholder opacity logic `(focused && !hasValue) ? 1 : 0` was preventing placeholders from showing when focused.
**Solution**: Changed logic to `focused ? 1 : 0` so placeholders only show when input is focused.

### 2. Placeholder Control Toggle
**Problem**: No way to debug placeholder visibility in the test page.
**Solution**: Added a "Show Placeholders" toggle in the test controls that conditionally passes the placeholder prop to form components.

### 3. Label Position Consistency
**Problem**: Floating label positioning was inconsistent, especially with helper text.
**Solution**: 
- Fixed label positioning to use consistent transform values
- Updated CSS to use `scale()` for better visual consistency
- Ensured label position is based only on input state, not description/error text

## Files Modified

### `/apps/web/src/pages/MantineFormTest.tsx`
- Added `showPlaceholders` state variable
- Added "Show Placeholders" toggle switch in test controls
- Made all form component placeholder props conditional based on toggle state

### `/apps/web/src/components/forms/MantineFormInputs.tsx`
- Added explicit `placeholder` prop to all component destructuring
- Fixed placeholder opacity logic from `(focused && !hasValue) ? 1 : 0` to `focused ? 1 : 0`
- Ensured placeholder prop is passed to underlying Mantine components

### `/apps/web/src/styles/FormComponents.module.css`
- Added `transform-origin: left center` for better label scaling
- Updated floating label states to use `scale()` transforms
- Fixed label positioning to be consistent regardless of helper text

## Testing

The development environment is running at:
- React App: http://localhost:5173
- MantineFormTest page: http://localhost:5173/mantine-form-test

### Test Cases to Verify
1. **Placeholder Toggle**: Toggle "Show Placeholders" and verify placeholders appear/disappear
2. **Focus Behavior**: When placeholders are enabled, they should only show when input is focused
3. **Label Position**: Labels should float to the same position above inputs regardless of helper text presence
4. **Dark Mode**: All fixes should work in both light and dark modes

## Implementation Details

The key insight was that Mantine's placeholder behavior needed to be controlled at the prop level rather than just through CSS. The original logic was trying to show placeholders only when focused AND empty, but this prevented users from seeing placeholder text when they focused on an empty field.

The new approach:
1. Controls placeholder visibility through conditional prop passing
2. Shows placeholders only when focused (regardless of value)
3. Maintains floating label behavior as the primary labeling mechanism

This provides a better user experience where:
- Floating labels are the primary indication of field purpose
- Placeholders appear as supplementary guidance when user focuses on field
- Users can toggle placeholder visibility for debugging/preference
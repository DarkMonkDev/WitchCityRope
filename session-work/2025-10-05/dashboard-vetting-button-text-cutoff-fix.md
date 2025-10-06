# Dashboard Vetting Button Text Cutoff Fix

**Date**: 2025-10-05
**Issue**: Button text cut off at top and bottom on dashboard vetting application button
**Root Cause**: Same recurring issue - Mantine buttons without proper height/padding styling

## Problem

The "Submit Vetting Application" button on the user dashboard had text cut off at the top and bottom. This is the **SAME ISSUE** we've fixed before in modal buttons, demonstrating this is a **SYSTEMIC PROBLEM** that keeps recurring with every new button created.

### User Quote
> "this is an issue that seems to happen with every new button that's a minute and I really wish it would stop."

## Root Cause

Mantine Button components need explicit styling properties to prevent text cutoff:
- **Missing explicit height** - buttons default to height that clips text
- **Missing explicit padding** - top/bottom padding not properly set
- **Missing line height** - text rendering gets clipped
- **Using size prop alone** - `size="sm"` without explicit styles causes cutoff

## Files Fixed

### 1. UserDashboard.tsx (Line 214)
**Location**: `/home/chad/repos/witchcityrope/apps/web/src/features/dashboard/components/UserDashboard.tsx`

**Before**:
```tsx
<Button
  component="a"
  href="/join"
  color="blue"
  size="sm"
>
  Submit Vetting Application
</Button>
```

**After**:
```tsx
<Button
  component="a"
  href="/join"
  color="blue"
  styles={{
    root: {
      fontWeight: 600,
      height: '44px',
      paddingTop: '12px',
      paddingBottom: '12px',
      fontSize: '14px',
      lineHeight: '1.2'
    }
  }}
>
  Submit Vetting Application
</Button>
```

### 2. UpcomingEvents.tsx (Line 276)
**Location**: `/home/chad/repos/witchcityrope/apps/web/src/features/dashboard/components/UpcomingEvents.tsx`

**Before**:
```tsx
<Button
  variant="light"
  color="blue"
  size="sm"
  rightSection={<IconArrowRight size={16} />}
  onClick={onViewAllEvents}
>
  View All My Events
</Button>
```

**After**:
```tsx
<Button
  variant="light"
  color="blue"
  rightSection={<IconArrowRight size={16} />}
  onClick={onViewAllEvents}
  styles={{
    root: {
      fontWeight: 600,
      height: '44px',
      paddingTop: '12px',
      paddingBottom: '12px',
      fontSize: '14px',
      lineHeight: '1.2'
    }
  }}
>
  View All My Events
</Button>
```

## Prevention Strategy

Updated lessons learned file to emphasize this is a **CRITICAL RECURRING ISSUE** with mandatory prevention rules:

### The Five Required Properties
**EVERY Mantine Button MUST include these in `styles.root`:**
1. `height: '44px'` - explicit height
2. `paddingTop: '12px'` - explicit top padding
3. `paddingBottom: '12px'` - explicit bottom padding
4. `fontSize: '14px'` - consistent font size
5. `lineHeight: '1.2'` - prevent text cutoff

### Updated Documentation
- **File**: `/home/chad/repos/witchcityrope/docs/lessons-learned/react-developer-lessons-learned.md`
- **Lines Updated**: 680-816
- **Added**: Explicit "RECURRING ISSUE" warning
- **Added**: Dashboard button fix examples
- **Added**: Critical prevention checklist for new buttons

## Verification

Searched all dashboard components for buttons:
- ✅ UserDashboard.tsx - Fixed (1 button)
- ✅ UpcomingEvents.tsx - Fixed (1 button)
- ✅ MembershipStatistics.tsx - No buttons found
- ✅ No other Button components with size prop found in dashboard

## Key Takeaway

**THIS IS A SYSTEMIC PROBLEM** - every time a new Mantine Button is created without the proper styling pattern, text gets cut off. The lessons learned file now has:

1. **Explicit recurring issue warning**
2. **Mandatory five properties checklist**
3. **Dashboard-specific examples**
4. **Prevention rules for ALL new buttons**

**NEVER create a Mantine Button without copying the correct styling pattern from the lessons learned file.**

## Tags
#critical #recurring-issue #mantine #button-styling #text-cutoff #systemic-problem

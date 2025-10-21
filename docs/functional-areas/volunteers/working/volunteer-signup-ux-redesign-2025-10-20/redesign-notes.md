# Volunteer Signup UX Redesign - October 20, 2025

## Objective
Remove modal popup and move all volunteer signup functionality inline to the event detail page.

## Changes Required

### 1. VolunteerPositionCard.tsx
**Changes**:
- ✅ Remove Progress bar (lines 89-105)
- ✅ Update badge to show `(x / y spots filled)` format (lines 69-81)
- ✅ Add session start/end times display
- ✅ Remove "Experience required" section (lines 108-115)
- ✅ Update button text from "Learn More & Sign Up" to "Sign Up"
- ✅ Use secondary CTA styling (outline button)
- ✅ Make button small and NOT full width
- ✅ Add inline signup functionality (expand card or show form below)
- ✅ No notes field in signup
- ✅ Show success/error messages inline or via notifications

### 2. VolunteerPositionModal.tsx
**Action**: DELETE - No longer needed

### 3. EventDetailPage.tsx
**Changes**:
- ✅ Remove modal state management
- ✅ Remove VolunteerPositionModal component usage
- ✅ Ensure VolunteerPositionCard handles signup inline

### 4. volunteer.types.ts
**Changes**:
- ✅ Remove `notes` field from VolunteerSignupRequest
- ✅ Consider removing `requirements` field if not used

### 5. volunteerApi.ts
**Changes**:
- ✅ Update signupForVolunteerPosition to not send notes field

## Design Guidelines Applied
- Button styling: Mantine secondary CTA (variant="outline", color="burgundy")
- Inline form: Simple confirmation UI
- Time display: Format nicely (e.g., "2:00 PM - 4:00 PM")
- Badge format: Exactly `(3 / 5 spots filled)`
- Accessibility: Maintain proper ARIA labels

## Implementation Status
- [ ] Update VolunteerPositionCard.tsx
- [ ] Delete VolunteerPositionModal.tsx
- [ ] Update EventDetailPage.tsx
- [ ] Update volunteer.types.ts
- [ ] Update volunteerApi.ts

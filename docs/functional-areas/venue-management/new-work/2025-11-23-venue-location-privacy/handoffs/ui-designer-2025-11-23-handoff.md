# AGENT HANDOFF DOCUMENT

## Phase: UI Design → Implementation
## Date: 2025-11-23
## Feature: Venue Location Privacy

## 🎯 CRITICAL BUSINESS RULES (MUST IMPLEMENT)

1. **Location Field is Optional**: Admin venue form must NOT require Location field
   - ✅ Correct: Allow saving venue with empty Location field
   - ❌ Wrong: Requiring Location field with validation error

2. **Visibility Based on User Status**: Location display depends on vetting status AND participation status
   - ✅ Correct: Show Location to non-vetted non-participants, VenueName to vetted OR participants
   - ❌ Wrong: Using only vetting status, ignoring participation

3. **Conditional Section Rendering**: Event details page shows different sections based on access
   - ✅ Correct: Full venue details (name + directions + actions) for vetted/participants, limited location (city/state + info alert) for others
   - ❌ Wrong: Always showing same section with conditional text changes

4. **Location vs Venue Name Priority**: When user has access, ALWAYS show VenueName (not Location)
   - ✅ Correct: `hasAccess ? venue.name : venue.location`
   - ❌ Wrong: Showing both fields or showing Location when user has access

5. **Info Alert Messaging**: Non-vetted users must understand WHEN they get access
   - ✅ Correct: "Full venue address and directions will be provided after registration."
   - ❌ Wrong: "Full details not available" (unclear, no path forward)

## 📍 KEY DOCUMENTS TO READ

| Document | Path | Critical Sections |
|----------|------|-------------------|
| UI Design | `/docs/functional-areas/venue-management/new-work/2025-11-23-venue-location-privacy/design/ui-design.md` | All wireframes, Component Specifications, Interaction Patterns |
| Design System v7 | `/docs/design/current/design-system-v7.md` | Typography, Colors, Spacing sections |
| Button Style Guide | `/docs/design/current/button-style-guide.md` | Secondary button pattern for Copy/Maps actions |
| UI Designer Lessons | `/docs/lessons-learned/ui-designer-lessons-learned.md` | Admin Settings Card Pattern, Form patterns |

## 🚨 KNOWN PITFALLS

1. **Naive Conditional Rendering**: Using simple `user?.isVetted && <Component />` loses button during loading
   - **Why it happens**: API data is null/undefined during loading state
   - **How to avoid**: Use pattern from React Patterns doc - include `data === null || isLoading` in visibility condition

2. **Using Wrong DTO Field**: Backend returns `venue.location` (NEW) and `venue.name` (existing)
   - **Why it happens**: Developer uses cached understanding of venue data structure
   - **How to avoid**: Verify backend DTO includes new `location` field, use correct field names

3. **Hardcoded Mantine Styles**: Overriding Design System v7 colors with inline hex codes
   - **Why it happens**: Developer doesn't know CSS variables exist
   - **How to avoid**: ALWAYS use `var(--color-*)` variables from Design System v7

4. **Missing Participant Check**: Only checking `isVetted` for access, ignoring event participation
   - **Why it happens**: Requirements mention "vetted users" prominently, participation is secondary
   - **How to avoid**: Access = `user?.isVetted || isParticipant` (both conditions matter)

5. **Icon Accessibility**: Using emoji without proper ARIA labels
   - **Why it happens**: Emojis "just work" visually, accessibility is afterthought
   - **How to avoid**: Wrap emoji in `<span role="img" aria-label="...">` per wireframes

## ✅ VALIDATION CHECKLIST

Before proceeding to testing phase, verify:

- [ ] Admin form Location field is optional (no required validation)
- [ ] Admin form Location field has 100 char max length with counter
- [ ] Admin form Location field has proper helper text per wireframe
- [ ] Event card shows Location for non-vetted non-participants
- [ ] Event card shows VenueName for vetted users
- [ ] Event card shows VenueName for participants (post-RSVP)
- [ ] Event details shows limited section for non-vetted non-participants
- [ ] Event details shows full section for vetted users
- [ ] Event details shows full section for participants
- [ ] Info Alert displays with proper styling and messaging
- [ ] Copy Address button extracts first line of directions
- [ ] Open in Maps button constructs correct Google Maps URL
- [ ] All emojis have `role="img"` and `aria-label`
- [ ] All colors use CSS variables from Design System v7
- [ ] Component uses Mantine v7 components (not custom HTML)
- [ ] Mobile responsive behavior matches wireframes
- [ ] Keyboard navigation works for all interactive elements

## 🔄 DISCOVERED CONSTRAINTS

1. **Existing Venue Form Pattern**: Admin Settings page uses conditional form visibility pattern
   - **Impact**: New Location field must integrate into existing form structure
   - **Required Changes**: Add TextInput between VenueName and Directions fields, maintain existing layout

2. **User Context Structure**: Auth context provides `user.isVetted` boolean
   - **Impact**: Vetting status check is straightforward boolean check
   - **Required Changes**: None, use existing pattern

3. **Event Participation Data**: Event DTOs include user participation status
   - **Impact**: Need to check event-specific participation flag
   - **Required Changes**: Access event participation from DTO, not separate API call

4. **Mantine v7 is Active**: All components must use Mantine v7, NOT custom components
   - **Impact**: Use TextInput, Text, Stack, Alert, Group from @mantine/core
   - **Required Changes**: Import from @mantine/core, use component props not inline styles

## 📊 DATA MODEL DECISIONS

### Venue DTO (Backend)
```typescript
interface VenueDto {
  id: number;
  name: string;
  location: string; // NEW FIELD - City, state (max 100 chars, optional)
  directions: string;
  notes: string;
  isActive: boolean;
}
```

### Event DTO Venue Reference
```typescript
interface EventDto {
  // ... other fields
  venue: {
    id: number;
    name: string;
    location: string; // NEW - included in venue object
    directions: string;
    notes: string;
  };
  // User participation status
  userParticipation?: {
    isRegistered: boolean;
    isPaid: boolean;
    rsvpStatus: string;
  };
}
```

### Access Control Logic
```typescript
// In component or custom hook
const hasVenueAccess = user?.isVetted || event?.userParticipation?.isRegistered;
const shouldShowLocation = !hasVenueAccess;
const locationText = shouldShowLocation ? venue?.location : venue?.name;
```

## 🎯 SUCCESS CRITERIA

### Admin Form Test Cases

1. **Test Case**: Add Location to new venue
   - **Input**: Enter "Salem, MA" in Location field, fill other required fields
   - **Expected Output**: Venue saves successfully, Location stored in database

2. **Test Case**: Leave Location empty
   - **Input**: Leave Location field blank, fill other required fields
   - **Expected Output**: Venue saves successfully, no validation error

3. **Test Case**: Enter 100 character Location
   - **Input**: Type exactly 100 characters in Location field
   - **Expected Output**: Character counter shows "100/100", no validation error

4. **Test Case**: Try to enter 101 characters
   - **Input**: Type 101 characters in Location field
   - **Expected Output**: Field stops accepting input at 100 chars OR shows error

### Event Card Test Cases

1. **Test Case**: Non-vetted user views event card
   - **Input**: User with `isVetted: false`, event with `location: "Salem, MA"`
   - **Expected Output**: Event card displays "📍 Salem, MA"

2. **Test Case**: Vetted user views event card
   - **Input**: User with `isVetted: true`, event with `name: "Community Center"`
   - **Expected Output**: Event card displays "📍 Community Center"

3. **Test Case**: Participant views event card
   - **Input**: User with `isVetted: false`, `isRegistered: true`, event data
   - **Expected Output**: Event card displays "📍 Community Center" (venue name, not location)

### Event Details Test Cases

1. **Test Case**: Non-vetted user before RSVP
   - **Input**: User with `isVetted: false`, `isRegistered: false`
   - **Expected Output**: Shows "LOCATION" header, location text, info alert about access after registration

2. **Test Case**: User after RSVP
   - **Input**: User with `isVetted: false`, `isRegistered: true`
   - **Expected Output**: Shows "VENUE" header, venue name, "DIRECTIONS" header, full directions, Copy/Maps buttons

3. **Test Case**: Vetted user
   - **Input**: User with `isVetted: true`
   - **Expected Output**: Shows full venue details regardless of registration status

4. **Test Case**: Copy Address button click
   - **Input**: User clicks "Copy Address" button
   - **Expected Output**: First line of directions copied to clipboard, success notification shown

5. **Test Case**: Open in Maps button click
   - **Input**: User clicks "Open in Maps" button
   - **Expected Output**: New tab opens with Google Maps search for venue address

## ⚠️ DO NOT IMPLEMENT

- ❌ DO NOT require Location field in admin form validation
- ❌ DO NOT show Location field when user has venue access (show VenueName instead)
- ❌ DO NOT use inline hex colors - MUST use Design System v7 CSS variables
- ❌ DO NOT create custom components - use Mantine v7 components
- ❌ DO NOT forget participant check in access logic
- ❌ DO NOT use emojis without `role="img"` and `aria-label`
- ❌ DO NOT copy entire directions text - extract first line for address
- ❌ DO NOT assume API data is always available - handle loading/null states
- ❌ DO NOT forget character counter for Location field
- ❌ DO NOT show both Location and VenueName - show one based on access

## 📝 TERMINOLOGY DICTIONARY

| Term | Definition | Example |
|------|------------|---------|
| Location | Public city/state info for privacy | "Salem, MA" |
| VenueName | Full venue name (private until access granted) | "Salem Community Center" |
| Directions | Full address and navigation instructions | "123 Main St, Salem, MA 01970\nEnter side door" |
| Vetted User | User with trusted status (isVetted: true) | Teacher, Admin roles |
| Participant | User who registered/purchased for this event | isRegistered: true |
| Venue Access | Permission to see full venue details | isVetted OR isParticipant |
| Info Alert | Blue alert box explaining access restrictions | Mantine Alert component |

## 🔗 NEXT AGENT INSTRUCTIONS

### React Developer Implementation Steps

1. **FIRST**: Read all documents in Key Documents section
   - Start with UI Design wireframes - understand all 5 visual states
   - Review Design System v7 for color/typography variables
   - Check React Patterns doc for conditional rendering pattern

2. **SECOND**: Review existing admin settings code
   - Location: `/apps/web/src/pages/admin/AdminSettings.tsx`
   - Understand existing venue form structure
   - Note how form state is managed

3. **THIRD**: Implement in this order
   - Add Location field to admin venue form (TextInput component)
   - Update venue save/update logic to include location field
   - Add location display to event cards (conditional rendering)
   - Add location section to event details (two variants)
   - Implement Copy Address action
   - Implement Open in Maps action
   - Add all ARIA labels and accessibility features

4. **FOURTH**: Test against Success Criteria
   - Manual testing of all 11 test cases listed above
   - Verify responsive behavior on mobile
   - Check keyboard navigation
   - Validate accessibility with screen reader

5. **THEN**: Create handoff document for test-developer
   - Document component file paths
   - List all test scenarios
   - Note any edge cases discovered during implementation

## 🤝 HANDOFF CONFIRMATION

**Previous Agent**: UI Designer Agent
**Previous Phase Completed**: 2025-11-23
**Key Finding**: Location privacy requires TWO checks (vetting status AND participation status) - missing either breaks the feature

**Next Agent Should Be**: React Developer Agent
**Next Phase**: Implementation
**Estimated Effort**: 4-6 hours (2 hours for admin form, 2-3 hours for event card/details, 1 hour for actions/testing)

---

## Implementation Priority

1. **HIGH PRIORITY**: Admin form Location field (blocks all other work)
2. **HIGH PRIORITY**: Event card conditional display (user-facing visibility)
3. **MEDIUM PRIORITY**: Event details limited section (non-vetted experience)
4. **MEDIUM PRIORITY**: Event details full section (vetted/participant experience)
5. **LOW PRIORITY**: Copy Address and Open in Maps actions (nice-to-have UX)

## Design Decisions Rationale

### Why Location Field is Optional
- Not all venues need privacy protection
- Some events are fully remote
- Admins may choose to only show full venue name
- Empty state is valid business case

### Why Two Access Conditions
- Vetted users are trusted (permanent access to all venues)
- Participants committed to event (temporary access to specific venue)
- Both groups need full details to attend event
- Non-vetted non-participants only need general location for discovery

### Why Info Alert vs Inline Text
- Alert component creates clear visual distinction
- Icon draws attention to important information
- Light variant doesn't alarm users
- Clear call-to-action (register to get access)

### Why Copy Address Extracts First Line
- Full directions may include multi-line instructions
- First line is typically the street address
- Google Maps search works best with concise address
- User can still see full directions on page

---

**Questions or Clarifications?** Refer back to UI Design wireframes in `/docs/functional-areas/venue-management/new-work/2025-11-23-venue-location-privacy/design/ui-design.md`

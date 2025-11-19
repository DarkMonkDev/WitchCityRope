# UI Designer Handoff - Granular Event Timing Controls
<!-- Date: 2025-11-18 -->
<!-- From: Business Requirements -->
<!-- To: UI Designer Agent -->
<!-- Feature: Granular Event Timing Controls -->

## 🎯 DESIGN REQUEST OVERVIEW

Create wireframes for per-event timing control settings that allow event organizers to configure granular registration and cancellation windows for RSVP, Tickets, and Volunteer spots.

## 📍 DESIGN REQUIREMENTS

### 1. RSVP/Tickets Tab - Timing Settings Section

**Location**: Admin Event Create/Edit page, RSVP/Tickets tab
**Placement**: Top-right of tab title, inline with "RSVP/Tickets" heading

**Components Needed**:
- Toggle button for showing/hiding timing settings
- Collapsible settings panel
- 4 number inputs for timing configuration
- Clear labels and help text
- Visual indication of positive (before) vs negative (after) values

**Key Features**:
- Settings button uses subtle variant (not primary)
- Settings section collapsible (not always visible)
- Settings panel has border to separate from main content
- Number inputs support decimals (0.5 = 30 minutes)
- Inputs allow negative values (post-event timing)
- Empty inputs allowed (no restriction)

### 2. Volunteers Tab - Timing Settings Section

**Location**: Admin Event Create/Edit page, Volunteers tab
**Placement**: Top-right of tab title, inline with "Volunteers" heading

**Components Needed**:
- Toggle button for showing/hiding timing settings
- Collapsible settings panel
- 2 number inputs for volunteer timing
- Clear labels and help text
- Similar visual pattern to RSVP/Tickets settings

**Key Features**:
- Same pattern as RSVP/Tickets settings
- Fewer inputs (only 2 vs 4)
- Consistent visual hierarchy

### 3. User Volunteer Cancellation Button

**Location**: User's volunteer assignments page
**Placement**: Next to each volunteer assignment listing

**Components Needed**:
- Cancel button (subtle variant, red color)
- Confirmation dialog before cancel
- Success/error notifications
- Loading state during cancel

**Key Features**:
- Non-destructive visual (not primary delete style)
- Disabled state when outside cancellation window
- Clear feedback on success/failure

## 🎨 DESIGN SYSTEM PATTERNS TO FOLLOW

### Mantine v7 Component Library
**Use these components**:
- `<Button>` - For settings toggle and cancel actions
- `<Paper>` - For settings panel container
- `<Grid>` and `<Grid.Col>` - For input layout
- `<NumberInput>` - For timing hour inputs
- `<Title>` - For section headings
- `<Text>` - For descriptions and help text
- `<Group>` - For horizontal layout (title + button)

### Existing EventForm Pattern
**Reference**: `/apps/web/src/features/events/components/EventForm.tsx` (line 378-423)
- SegmentedControl pattern shows right-aligned controls
- Similar inline-with-title pattern needed for settings button
- Collapsible sections for advanced configuration

### Visual Hierarchy
- **Level 1**: Tab title (H3)
- **Level 2**: Settings section title (H5)
- **Level 3**: Input labels (default label size)
- **Level 4**: Help text (small, dimmed)

### Color Palette
- **Settings button**: Subtle variant (low emphasis)
- **Settings panel**: Default background with border
- **Cancel button**: Red color (destructive action)
- **Help text**: Dimmed color
- **Error messages**: Red (danger)
- **Success messages**: Green (success)

## 📐 WIREFRAME REQUESTS

### Wireframe 1: RSVP/Tickets Tab - Settings Collapsed

**Title**: `rsvp-tickets-tab-collapsed.png`

**Description**: Show RSVP/Tickets tab with timing settings button visible but settings panel hidden.

**Elements**:
```
┌─────────────────────────────────────────────────────────────┐
│ RSVP/Tickets                    [Show Timing Settings] ⚙️   │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│ [Existing RSVP/Tickets tab content...]                      │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

**Key Points**:
- Settings button on same line as tab title
- Settings button right-aligned
- Settings panel NOT visible
- Clear visual separation from content

### Wireframe 2: RSVP/Tickets Tab - Settings Expanded

**Title**: `rsvp-tickets-tab-expanded.png`

**Description**: Show RSVP/Tickets tab with timing settings panel visible and all 4 timing inputs.

**Elements**:
```
┌─────────────────────────────────────────────────────────────┐
│ RSVP/Tickets                    [Hide Timing Settings] ⚙️   │
├─────────────────────────────────────────────────────────────┤
│ ┌──────────────────────────────────────────────────────────┐│
│ │ Registration & Cancellation Windows                      ││
│ │                                                           ││
│ │ Configure when users can register for and cancel         ││
│ │ RSVPs/Tickets. Positive = before event start, negative   ││
│ │ = after event start. Leave empty for no restriction.     ││
│ │                                                           ││
│ │ ┌────────────────────┬────────────────────┐              ││
│ │ │ Registration Opens │ Registration Closes│              ││
│ │ │ Hours before event │ Hours before event │              ││
│ │ │ [  168.0  ]       │ [   1.0    ]       │              ││
│ │ │ e.g., 168 = 1 week│ e.g., 1 = 1 hour   │              ││
│ │ └────────────────────┴────────────────────┘              ││
│ │                                                           ││
│ │ ┌────────────────────┬────────────────────┐              ││
│ │ │ Cancellation Opens │ Cancellation Closes│              ││
│ │ │ Hours before event │ Hours before event │              ││
│ │ │ [  168.0  ]       │ [  -24.0   ]       │              ││
│ │ │ e.g., 168 = 1 week│ e.g., -24 = 24 hrs │              ││
│ │ │                    │      after event   │              ││
│ │ └────────────────────┴────────────────────┘              ││
│ └──────────────────────────────────────────────────────────┘│
│                                                              │
│ [Existing RSVP/Tickets tab content...]                      │
└─────────────────────────────────────────────────────────────┘
```

**Key Points**:
- Settings panel has border to separate from content
- 2x2 grid layout for 4 inputs
- Clear labels for each input
- Help text under each input explaining values
- Negative values visually indicated in placeholder
- Settings panel above main tab content

### Wireframe 3: Volunteers Tab - Settings Expanded

**Title**: `volunteers-tab-expanded.png`

**Description**: Show Volunteers tab with timing settings panel visible and 2 volunteer timing inputs.

**Elements**:
```
┌─────────────────────────────────────────────────────────────┐
│ Volunteers                      [Hide Timing Settings] ⚙️   │
├─────────────────────────────────────────────────────────────┤
│ ┌──────────────────────────────────────────────────────────┐│
│ │ Volunteer Timing Windows                                 ││
│ │                                                           ││
│ │ Configure when users can sign up for and cancel          ││
│ │ volunteer spots. Positive = before event start, negative ││
│ │ = after event start. Leave empty for no restriction.     ││
│ │                                                           ││
│ │ ┌────────────────────┬────────────────────┐              ││
│ │ │ Volunteer Signup   │ Volunteer Cancel   │              ││
│ │ │ Closes             │ Closes             │              ││
│ │ │ Hours before event │ Hours before event │              ││
│ │ │ [   24.0   ]      │ [   48.0   ]       │              ││
│ │ │ e.g., 24 = 1 day  │ e.g., 48 = 2 days  │              ││
│ │ └────────────────────┴────────────────────┘              ││
│ └──────────────────────────────────────────────────────────┘│
│                                                              │
│ [Existing Volunteers tab content...]                        │
└─────────────────────────────────────────────────────────────┘
```

**Key Points**:
- Same visual pattern as RSVP/Tickets settings
- Only 2 inputs (not 4)
- Consistent help text and labels
- Same bordered panel style

### Wireframe 4: User Volunteer Assignment with Cancel Button

**Title**: `user-volunteer-assignment-cancel.png`

**Description**: Show user's volunteer assignment listing with cancel button.

**Elements**:
```
┌─────────────────────────────────────────────────────────────┐
│ My Volunteer Assignments                                     │
├─────────────────────────────────────────────────────────────┤
│ ┌──────────────────────────────────────────────────────────┐│
│ │ Event: Rope Bondage Workshop                             ││
│ │                                                           ││
│ │ ┌─────────────────────────────────────────┬──────────┐   ││
│ │ │ Setup Crew                              │ [Cancel] │   ││
│ │ │ Help set up the venue before the event  │    🗙    │   ││
│ │ │ 5:00 PM - 6:00 PM                       │          │   ││
│ │ └─────────────────────────────────────────┴──────────┘   ││
│ │                                                           ││
│ │ ┌─────────────────────────────────────────┬──────────┐   ││
│ │ │ Safety Monitor                          │ [Cancel] │   ││
│ │ │ Monitor safety during the event         │    🗙    │   ││
│ │ │ 6:00 PM - 9:00 PM                       │          │   ││
│ │ └─────────────────────────────────────────┴──────────┘   ││
│ └──────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────┘
```

**Key Points**:
- Cancel button subtle variant, red color
- Cancel button right-aligned next to assignment details
- Small size button (not large/prominent)
- Clear X icon indicating cancel action
- Each assignment has own cancel button

### Wireframe 5: Cancel Confirmation Dialog

**Title**: `volunteer-cancel-confirmation.png`

**Description**: Show confirmation dialog when user clicks cancel button.

**Elements**:
```
┌─────────────────────────────────────────────────────────────┐
│                                                              │
│   ┌────────────────────────────────────────────────┐        │
│   │ Cancel Volunteer Assignment?                   │        │
│   ├────────────────────────────────────────────────┤        │
│   │                                                 │        │
│   │ Are you sure you want to cancel your           │        │
│   │ volunteer assignment for Setup Crew?           │        │
│   │                                                 │        │
│   │ This action cannot be undone.                  │        │
│   │                                                 │        │
│   │              [No, Keep It]  [Yes, Cancel]      │        │
│   └────────────────────────────────────────────────┘        │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

**Key Points**:
- Clear confirmation message
- Spot name mentioned in dialog
- Non-destructive action emphasized (Keep It)
- Destructive action secondary (Cancel)

### Wireframe 6: Error State - Cancellation Window Closed

**Title**: `volunteer-cancel-error.png`

**Description**: Show error notification when cancellation fails due to closed window.

**Elements**:
```
┌─────────────────────────────────────────────────────────────┐
│ ┌──────────────────────────────────────────────────────────┐│
│ │ ❌ Error                                            [X]  ││
│ │ Volunteer cancellation window has closed for this event  ││
│ └──────────────────────────────────────────────────────────┘│
│                                                              │
│ My Volunteer Assignments                                     │
│ ┌──────────────────────────────────────────────────────────┐│
│ │ ┌─────────────────────────────────────────┬──────────┐   ││
│ │ │ Setup Crew                              │ [Cancel] │   ││
│ │ │ Help set up the venue before the event  │ DISABLED │   ││
│ │ │ 5:00 PM - 6:00 PM                       │          │   ││
│ │ └─────────────────────────────────────────┴──────────┘   ││
│ └──────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────┘
```

**Key Points**:
- Red error notification at top
- Error message explains why cancel failed
- Cancel button disabled/unavailable
- Clear visual feedback

## ♿ ACCESSIBILITY REQUIREMENTS

### Keyboard Navigation
- All buttons keyboard accessible (Tab navigation)
- Settings can be toggled with Enter/Space
- Number inputs support arrow keys for increment/decrement
- Cancel confirmation supports Escape to dismiss

### Screen Readers
- Settings button has clear aria-label
- Timing inputs have descriptive labels
- Help text associated with inputs (aria-describedby)
- Cancel button announces action clearly

### Color Contrast
- Text meets WCAG AA standards (4.5:1 minimum)
- Error messages visible without relying on color alone
- Disabled states clearly indicated

### Focus States
- Visible focus indicators on all interactive elements
- Focus order logical (top to bottom, left to right)

## 📋 DESIGN DELIVERABLES

Please provide:
1. **6 wireframes** as described above (PNG format)
2. **Component specifications** document with:
   - Exact Mantine component props
   - Spacing and sizing values
   - Color values from Mantine theme
3. **Interaction specifications**:
   - Timing settings toggle behavior
   - Number input step increments (0.5)
   - Validation error display timing
   - Cancel button disabled states
4. **Accessibility checklist**:
   - ARIA attributes needed
   - Keyboard shortcuts
   - Screen reader announcements

## 🔗 REFERENCE MATERIALS

**Existing Patterns**:
- EventForm tabs: `/apps/web/src/features/events/components/EventForm.tsx`
- SegmentedControl inline pattern: EventForm.tsx line 378-423
- NumberInput usage: Mantine v7 documentation

**Design System**:
- Mantine v7 component library
- WitchCityRope color scheme
- Existing admin interface patterns

## 🤝 HANDOFF CONFIRMATION

**From**: Business Requirements Agent
**To**: UI Designer Agent
**Date**: 2025-11-18

**Next Steps After Design**:
1. React Developer reviews wireframes
2. React Developer implements components per design specs
3. Test Developer validates UI functionality

---

**Please create the 6 wireframes and component specifications document. Focus on consistency with existing EventForm patterns and clear visual hierarchy for timing configuration.**

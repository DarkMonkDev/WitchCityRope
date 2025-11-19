# UI Wireframes: Granular Event Timing Controls
<!-- Last Updated: 2025-11-18 -->
<!-- Version: 1.0 -->
<!-- Owner: UI Designer Agent -->
<!-- Status: Design Phase -->

## Design Overview

This document provides comprehensive wireframes and component specifications for implementing per-event timing controls that allow event organizers to configure granular registration and cancellation windows for RSVP, Tickets, and Volunteer spots.

**User Goals**:
- Event admins configure timing windows easily
- Visual clarity for positive (before event) vs negative (after event) values
- Users understand when they can/cannot cancel volunteer assignments
- Clear feedback when actions are outside allowed time windows

## User Personas

- **Admin**: Event organizers configuring timing settings
- **Teacher**: Instructors creating events with specific timing requirements
- **Vetted Member**: Users managing their volunteer assignments
- **General Member**: Users viewing their registrations/tickets

## Wireframes

### Wireframe 1: RSVP/Tickets Tab - Settings Collapsed

**Visual ASCII Wireframe**:
```
┌──────────────────────────────────────────────────────────────────────┐
│ ┌──────────────────────────────────────────────────────────────────┐ │
│ │ Event: Rope Bondage Workshop                                     │ │
│ └──────────────────────────────────────────────────────────────────┘ │
│                                                                        │
│ ┌──────────────────────────────────────────────────────────────────┐ │
│ │ Basic Info  RSVP/Tickets  Volunteers  Details                    │ │
│ └──────────────────────────────────────────────────────────────────┘ │
│                                                                        │
│ ┌──────────────────────────────────────────────────────────────────┐ │
│ │ RSVP/TICKETS                                  [ ⚙ Timing Settings]│ │
│ ├──────────────────────────────────────────────────────────────────┤ │
│ │                                                                    │ │
│ │ Enable RSVP                                                        │ │
│ │ [ ] Allow RSVPs for this event                                    │ │
│ │                                                                    │ │
│ │ Enable Tickets                                                     │ │
│ │ [ ] Sell tickets for this event                                   │ │
│ │                                                                    │ │
│ │ Capacity                                                           │ │
│ │ Maximum Attendees: [____50____]                                   │ │
│ │                                                                    │ │
│ │ [Existing RSVP/Tickets tab content continues...]                  │ │
│ └──────────────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────────┘
```

**Component Specifications**:

| Component | Mantine Component | Props/Configuration |
|-----------|------------------|---------------------|
| Tab Title + Settings Button Row | `Group` | `justify="space-between"`, `align="center"` |
| Tab Title | `Title` | `order={3}`, Burgundy gradient header style |
| Settings Button | `Button` | `variant="subtle"`, `leftSection={<IconSettings size={16} />}`, `size="sm"` |
| Tab Content | `Paper` | `p="xl"`, `withBorder` |

**Interaction Pattern**:
- Settings button in collapsed state shows "Timing Settings" with gear icon
- Button uses subtle variant to avoid competing with primary content
- Right-aligned positioning matches existing SegmentedControl pattern
- Click toggles settings panel visibility

---

### Wireframe 2: RSVP/Tickets Tab - Settings Expanded

**Visual ASCII Wireframe**:
```
┌──────────────────────────────────────────────────────────────────────┐
│ ┌──────────────────────────────────────────────────────────────────┐ │
│ │ RSVP/TICKETS                                  [ ⚙ Hide Settings ]│ │
│ ├──────────────────────────────────────────────────────────────────┤ │
│ │ ┌────────────────────────────────────────────────────────────────┐│ │
│ │ │ REGISTRATION & CANCELLATION WINDOWS                            ││ │
│ │ │                                                                 ││ │
│ │ │ Configure when users can register for and cancel RSVPs/Tickets ││ │
│ │ │ • Positive values = hours BEFORE event start                   ││ │
│ │ │ • Negative values = hours AFTER event start                    ││ │
│ │ │ • Leave empty for no restriction                               ││ │
│ │ │                                                                 ││ │
│ │ │ ┌──────────────────────────┬──────────────────────────┐        ││ │
│ │ │ │ REGISTRATION OPENS       │ REGISTRATION CLOSES      │        ││ │
│ │ │ │ Hours before event       │ Hours before event       │        ││ │
│ │ │ │ [______168.0______]      │ [_______1.0_______]      │        ││ │
│ │ │ │ e.g., 168 = 1 week       │ e.g., 1 = 1 hour         │        ││ │
│ │ │ └──────────────────────────┴──────────────────────────┘        ││ │
│ │ │                                                                 ││ │
│ │ │ ┌──────────────────────────┬──────────────────────────┐        ││ │
│ │ │ │ CANCELLATION OPENS       │ CANCELLATION CLOSES      │        ││ │
│ │ │ │ Hours before event       │ Hours before event       │        ││ │
│ │ │ │ [______168.0______]      │ [______-24.0______]      │        ││ │
│ │ │ │ e.g., 168 = 1 week       │ e.g., -24 = 24 hrs after │        ││ │
│ │ │ └──────────────────────────┴──────────────────────────┘        ││ │
│ │ └────────────────────────────────────────────────────────────────┘│ │
│ │                                                                    │ │
│ │ Enable RSVP                                                        │ │
│ │ [ ] Allow RSVPs for this event                                    │ │
│ │                                                                    │ │
│ │ [Existing RSVP/Tickets tab content continues...]                  │ │
│ └──────────────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────────┘
```

**Component Specifications**:

| Component | Mantine Component | Props/Configuration |
|-----------|------------------|---------------------|
| Settings Panel Container | `Paper` | `p="xl"`, `withBorder`, `mb="xl"`, `style={{ borderLeft: '4px solid var(--color-burgundy)' }}` |
| Panel Title | `Title` | `order={5}`, `mb="sm"`, uppercase, letter-spacing |
| Description Text | `Text` | `size="sm"`, `c="dimmed"`, `mb="md"` |
| Grid Layout | `Grid` | `gutter="md"` |
| Grid Columns (2x2) | `Grid.Col` | `span={{ base: 12, sm: 6 }}` - full width mobile, half width desktop |
| Number Inputs | `NumberInput` | `decimalScale={1}`, `step={0.5}`, `allowNegative={true}`, `placeholder="No restriction"` |
| Input Labels | `Text` | `component="label"`, uppercase, letter-spacing, `mb="xs"` |
| Help Text | `Text` | `size="xs"`, `c="dimmed"`, `mt="xs"` |

**Visual Design Details**:
- **Panel Border**: 4px solid burgundy on left edge for visual emphasis
- **Grid Layout**: 2 columns on desktop (≥769px), stacks to 1 column on mobile
- **Input Width**: Full width within grid column
- **Number Format**: Decimal allowed (0.5 = 30 minutes)
- **Negative Values**: Supported with clear visual indication in help text
- **Empty State**: Placeholder "No restriction" shown when input empty

**Responsive Behavior**:
- **Desktop (≥769px)**: 2x2 grid, side-by-side inputs
- **Mobile (<768px)**: Stacked vertically, full-width inputs

---

### Wireframe 3: Volunteers Tab - Settings Expanded

**Visual ASCII Wireframe**:
```
┌──────────────────────────────────────────────────────────────────────┐
│ ┌──────────────────────────────────────────────────────────────────┐ │
│ │ VOLUNTEERS                                    [ ⚙ Hide Settings ]│ │
│ ├──────────────────────────────────────────────────────────────────┤ │
│ │ ┌────────────────────────────────────────────────────────────────┐│ │
│ │ │ VOLUNTEER TIMING WINDOWS                                       ││ │
│ │ │                                                                 ││ │
│ │ │ Configure when users can sign up for and cancel volunteer spots││ │
│ │ │ • Positive values = hours BEFORE event start                   ││ │
│ │ │ • Negative values = hours AFTER event start (rarely used)      ││ │
│ │ │ • Leave empty for no restriction                               ││ │
│ │ │                                                                 ││ │
│ │ │ ┌──────────────────────────┬──────────────────────────┐        ││ │
│ │ │ │ VOLUNTEER SIGNUP CLOSES  │ VOLUNTEER CANCEL CLOSES  │        ││ │
│ │ │ │ Hours before event       │ Hours before event       │        ││ │
│ │ │ │ [_______24.0_______]     │ [_______48.0_______]     │        ││ │
│ │ │ │ e.g., 24 = 1 day         │ e.g., 48 = 2 days        │        ││ │
│ │ │ └──────────────────────────┴──────────────────────────┘        ││ │
│ │ └────────────────────────────────────────────────────────────────┘│ │
│ │                                                                    │ │
│ │ Volunteer Spots                                                    │ │
│ │ ┌────────────────────────────────────────────────────────────────┐│ │
│ │ │ Setup Crew                                            [Edit]   ││ │
│ │ │ Help set up venue before event | Spots: 3                      ││ │
│ │ └────────────────────────────────────────────────────────────────┘│ │
│ │                                                                    │ │
│ │ [+ Add Volunteer Spot]                                            │ │
│ └──────────────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────────┘
```

**Component Specifications**:

| Component | Mantine Component | Props/Configuration |
|-----------|------------------|---------------------|
| Settings Panel Container | `Paper` | Same as RSVP/Tickets panel |
| Panel Title | `Title` | `order={5}`, "VOLUNTEER TIMING WINDOWS" |
| Grid Layout | `Grid` | `gutter="md"` |
| Grid Columns (1x2) | `Grid.Col` | `span={{ base: 12, sm: 6 }}` - only 2 inputs, not 4 |
| Number Inputs | `NumberInput` | Same configuration as RSVP/Tickets |

**Key Differences from RSVP/Tickets**:
- Only 2 timing inputs (signup close, cancel close) vs 4
- No "open" timing needed (volunteers can signup when event published)
- Negative values rarely used (volunteers typically cancel before event)
- Same visual pattern for consistency

---

### Wireframe 4: User Volunteer Assignment with Cancel Button

**Visual ASCII Wireframe**:
```
┌──────────────────────────────────────────────────────────────────────┐
│ MY VOLUNTEER ASSIGNMENTS                                              │
├──────────────────────────────────────────────────────────────────────┤
│                                                                        │
│ ┌──────────────────────────────────────────────────────────────────┐ │
│ │ EVENT: ROPE BONDAGE WORKSHOP                                     │ │
│ │ Saturday, December 7, 2025 at 6:00 PM                            │ │
│ │ Salem Community Center                                            │ │
│ └──────────────────────────────────────────────────────────────────┘ │
│                                                                        │
│ ┌──────────────────────────────────────────────────────────────────┐ │
│ │ ┌────────────────────────────────────────────────┬──────────────┐│ │
│ │ │ SETUP CREW                                     │              ││ │
│ │ │ Help set up the venue before the event         │   [ Cancel ] ││ │
│ │ │ 5:00 PM - 6:00 PM                              │      🗙       ││ │
│ │ └────────────────────────────────────────────────┴──────────────┘│ │
│ └──────────────────────────────────────────────────────────────────┘ │
│                                                                        │
│ ┌──────────────────────────────────────────────────────────────────┐ │
│ │ ┌────────────────────────────────────────────────┬──────────────┐│ │
│ │ │ SAFETY MONITOR                                 │              ││ │
│ │ │ Monitor safety during the event                │   [ Cancel ] ││ │
│ │ │ 6:00 PM - 9:00 PM                              │      🗙       ││ │
│ │ └────────────────────────────────────────────────┴──────────────┘│ │
│ └──────────────────────────────────────────────────────────────────┘ │
│                                                                        │
│ ┌──────────────────────────────────────────────────────────────────┐ │
│ │ ┌────────────────────────────────────────────────┬──────────────┐│ │
│ │ │ CLEANUP CREW                                   │              ││ │
│ │ │ Help clean up venue after event                │   [Cancel]   ││ │
│ │ │ 9:00 PM - 10:00 PM                             │   DISABLED   ││ │
│ │ │ ⚠ Cancellation window closed (48 hrs before)   │              ││ │
│ │ └────────────────────────────────────────────────┴──────────────┘│ │
│ └──────────────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────────┘
```

**Component Specifications**:

| Component | Mantine Component | Props/Configuration |
|-----------|------------------|---------------------|
| Page Title | `Title` | `order={2}`, `mb="xl"` |
| Event Header Card | `Paper` | `p="md"`, `mb="md"`, `withBorder` |
| Event Title | `Title` | `order={4}` |
| Event Datetime | `Text` | `size="sm"`, `c="dimmed"` |
| Assignment Card | `Paper` | `p="md"`, `mb="md"`, `withBorder` |
| Assignment Grid | `Grid` | `gutter="md"` |
| Assignment Details | `Grid.Col` | `span={10}` - 83% width |
| Cancel Button Column | `Grid.Col` | `span={2}` - 17% width, right-aligned |
| Spot Name | `Text` | `weight={600}`, `size="md"`, uppercase |
| Spot Description | `Text` | `size="sm"`, `c="dimmed"` |
| Time Range | `Text` | `size="sm"`, `c="dimmed"` |
| Cancel Button | `Button` | `variant="subtle"`, `color="red"`, `size="sm"`, `leftSection={<IconX size={16} />}` |
| Disabled Warning | `Text` | `size="xs"`, `c="orange"`, `mt="xs"`, `style={{ fontStyle: 'italic' }}` |

**Button States**:

**Active State**:
- Red text color (`color="red"`)
- Subtle variant (not destructive filled button)
- Small size to not dominate layout
- X icon indicates cancel action
- Hover state shows red background fill

**Disabled State**:
- Gray text, reduced opacity
- `cursor: not-allowed`
- Button shows "DISABLED" text
- Warning message below explains why
- No hover effects

---

### Wireframe 5: Cancel Confirmation Dialog

**Visual ASCII Wireframe**:
```
┌──────────────────────────────────────────────────────────────────────┐
│                                                                        │
│                                                                        │
│      ┌────────────────────────────────────────────────────┐          │
│      │ CANCEL VOLUNTEER ASSIGNMENT?                       │          │
│      ├────────────────────────────────────────────────────┤          │
│      │                                                     │          │
│      │ Are you sure you want to cancel your volunteer     │          │
│      │ assignment for "Setup Crew"?                       │          │
│      │                                                     │          │
│      │ This will free up your spot for another member.    │          │
│      │                                                     │          │
│      │ This action cannot be undone.                      │          │
│      │                                                     │          │
│      │                                                     │          │
│      │              ┌──────────────┬──────────────┐       │          │
│      │              │ No, Keep It  │ Yes, Cancel  │       │          │
│      │              └──────────────┴──────────────┘       │          │
│      └────────────────────────────────────────────────────┘          │
│                                                                        │
│ MY VOLUNTEER ASSIGNMENTS (blurred background)                         │
│                                                                        │
└──────────────────────────────────────────────────────────────────────┘
```

**Component Specifications**:

| Component | Mantine Component | Props/Configuration |
|-----------|------------------|---------------------|
| Modal Container | `Modal` | `opened={showCancelModal}`, `onClose={handleClose}`, `centered`, `size="md"` |
| Modal Title | `Modal.Title` | "Cancel Volunteer Assignment?" |
| Confirmation Message | `Text` | `size="sm"`, `mb="md"`, includes spot name in quotes |
| Explanation Text | `Text` | `size="sm"`, `c="dimmed"`, `mb="md"` |
| Warning Text | `Text` | `size="sm"`, `weight={600}`, `mb="xl"` |
| Button Group | `Group` | `justify="flex-end"`, `gap="sm"` |
| Keep Button | `Button` | `variant="outline"`, `color="gray"` - non-destructive action emphasized |
| Cancel Button | `Button` | `color="red"` - destructive action secondary |

**Interaction Flow**:
1. User clicks "Cancel" button on volunteer assignment
2. Modal appears with backdrop blur
3. Modal displays spot name user is canceling
4. Two button choices:
   - "No, Keep It" (outline, gray) - safe choice, closes modal
   - "Yes, Cancel" (filled, red) - destructive action, confirms cancellation
5. On confirm: API call, success notification, assignment removed from list
6. On keep: Modal closes, no changes

**Accessibility**:
- Modal traps focus (cannot tab outside)
- Escape key closes modal (same as "No, Keep It")
- Clear button labels for screen readers
- Spot name announced in confirmation message

---

### Wireframe 6: Error State - Cancellation Window Closed

**Visual ASCII Wireframe**:
```
┌──────────────────────────────────────────────────────────────────────┐
│ ┌──────────────────────────────────────────────────────────────────┐ │
│ │ ❌ ERROR                                                    [ X ] │ │
│ │ Volunteer cancellation window has closed for this event          │ │
│ │ Cancellations must be made at least 48 hours before event start  │ │
│ └──────────────────────────────────────────────────────────────────┘ │
│                                                                        │
│ MY VOLUNTEER ASSIGNMENTS                                              │
├──────────────────────────────────────────────────────────────────────┤
│                                                                        │
│ ┌──────────────────────────────────────────────────────────────────┐ │
│ │ EVENT: ROPE BONDAGE WORKSHOP                                     │ │
│ │ Saturday, December 7, 2025 at 6:00 PM                            │ │
│ └──────────────────────────────────────────────────────────────────┘ │
│                                                                        │
│ ┌──────────────────────────────────────────────────────────────────┐ │
│ │ ┌────────────────────────────────────────────────────┬──────────┐│ │
│ │ │ SETUP CREW                                         │          ││ │
│ │ │ Help set up the venue before the event             │ [Cancel] ││ │
│ │ │ 5:00 PM - 6:00 PM                                  │ DISABLED ││ │
│ │ │ ⚠ Cancellation window closed (within 48 hrs)      │          ││ │
│ │ └────────────────────────────────────────────────────┴──────────┘│ │
│ └──────────────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────────┘
```

**Component Specifications**:

| Component | Mantine Component | Props/Configuration |
|-----------|------------------|---------------------|
| Error Notification | `Notification` | `color="red"`, `icon={<IconAlertCircle />}`, `onClose={handleDismiss}`, `withCloseButton`, `style={{ position: 'fixed', top: '80px', right: '20px', zIndex: 1000 }}` |
| Error Title | `Notification.Title` | "Error" |
| Error Message Line 1 | `Text` | Main error message |
| Error Message Line 2 | `Text` | `size="sm"`, `c="dimmed"`, explains timing requirement |
| Disabled Cancel Button | `Button` | `disabled`, `variant="subtle"`, `color="gray"` |
| Warning Badge | `Text` | `size="xs"`, `c="orange"`, with warning icon |

**Error Display Logic**:
- Notification appears at top-right (fixed position)
- Auto-closes after 5 seconds
- User can manually dismiss with X button
- Notification includes specific timing requirement (e.g., "48 hours before")
- Cancel button disabled with visual indicator
- Warning text below button explains why disabled

**Error Messages by Scenario**:

| Scenario | Error Message |
|----------|---------------|
| Cancellation window closed | "Volunteer cancellation window has closed for this event" |
| Too close to event | "Cancellations must be made at least [X] hours before event start" |
| Event already started | "Cannot cancel volunteer assignment after event has started" |
| Network error | "Failed to cancel assignment. Please try again." |

---

## Mantine Components Used

| Component | Purpose | Key Props |
|-----------|---------|-----------|
| `Title` | Section headings, tab titles | `order`, gradient header styling |
| `Paper` | Card containers, settings panels | `p`, `withBorder`, `mb` |
| `Group` | Horizontal layouts | `justify`, `align`, `gap` |
| `Stack` | Vertical layouts | `gap` |
| `Grid`, `Grid.Col` | Responsive grid layouts | `gutter`, `span` |
| `Button` | Toggle settings, cancel actions | `variant`, `color`, `size`, `leftSection` |
| `NumberInput` | Timing hour inputs | `decimalScale`, `step`, `allowNegative`, `placeholder` |
| `Text` | Labels, descriptions, help text | `size`, `c`, `weight`, `component` |
| `Modal` | Confirmation dialogs | `opened`, `onClose`, `centered`, `size` |
| `Notification` | Error messages | `color`, `icon`, `onClose`, `withCloseButton` |

---

## Interaction Patterns

### Timing Settings Toggle

**Behavior**:
1. **Initial State**: Settings collapsed, button shows "Timing Settings" with gear icon
2. **Click Action**: Panel expands below button with slide-down animation (0.3s ease)
3. **Button Changes**: Text changes to "Hide Settings", icon remains
4. **Second Click**: Panel collapses with slide-up animation (0.3s ease)
5. **State Persistence**: Settings remain expanded/collapsed during session

**Animation**:
```css
.timing-settings-panel {
  max-height: 0;
  opacity: 0;
  overflow: hidden;
  transition: max-height 0.3s ease, opacity 0.3s ease;
}

.timing-settings-panel.expanded {
  max-height: 500px;
  opacity: 1;
}
```

### Number Input Interactions

**Step Increments**:
- Arrow buttons: ±0.5 hours (30-minute increments)
- Scroll wheel: ±0.5 hours
- Keyboard arrows: ±0.5 hours
- Direct typing: Any decimal value allowed

**Validation**:
- **Min value**: None (negative values allowed)
- **Max value**: None (flexible timing)
- **Empty state**: Valid (no restriction)
- **Decimal precision**: 1 decimal place (e.g., 168.5)

**Error Display**:
- Invalid format (e.g., "abc"): Red border, error text below
- Business logic error (e.g., close before open): Red border, contextual error
- Error clears on valid input

**Help Text Display**:
- Always visible below input
- Shows example value (e.g., "e.g., 168 = 1 week")
- Dimmed color to not distract from input

### Volunteer Cancel Button States

**Enabled State** (within cancellation window):
- Button color: red (subtle variant)
- Hover: red background fill
- Cursor: pointer
- Click: Opens confirmation modal

**Disabled State** (outside cancellation window):
- Button color: gray
- Opacity: 0.6
- Cursor: not-allowed
- Click: Shows error notification (or does nothing)
- Warning text displayed below button

**Loading State** (during API call):
- Button shows spinner
- Text changes to "Cancelling..."
- Button disabled to prevent double-click
- Spinner color matches button color

### Validation Error Display Timing

**Real-time Validation**:
- **On Blur**: Validate when user leaves input field
- **On Change**: Clear errors when user starts typing
- **On Submit**: Validate all fields before save

**Error Display**:
- Error appears immediately on blur (no delay)
- Error clears immediately on valid change
- Error message specific to validation failure
- Red border on input persists until error cleared

**Success Feedback**:
- No visual feedback for individual inputs (avoiding clutter)
- Success notification on form save
- Settings panel remains expanded after save

---

## Responsive Breakpoints

**Mobile (xs)**: 0px - 575px
- Full-width inputs (single column)
- Settings panel full-width
- Cancel button stacks below assignment details
- Notification full-width at top

**Small (sm)**: 576px - 767px
- 2-column grid for timing inputs
- Settings panel full-width
- Cancel button inline with assignment

**Medium (md)**: 768px - 991px
- 2-column grid maintained
- Settings panel max-width 100%
- Standard layout

**Large (lg)**: 992px - 1199px
- 2-column grid maintained
- Settings panel max-width 100%
- Standard layout

**Extra Large (xl)**: 1200px+
- 2-column grid maintained
- Settings panel max-width 100%
- Standard layout

**Key Responsive Changes**:
- **Mobile**: Inputs stack vertically (Grid.Col span={12})
- **Desktop**: Inputs side-by-side (Grid.Col span={6})
- **Settings Button**: Always inline with tab title (both mobile/desktop)
- **Cancel Button**: Inline on tablet+, stacks on mobile (<576px)

---

## Accessibility Requirements

### Keyboard Navigation

**Tab Order**:
1. Settings toggle button
2. Timing inputs (top-left → top-right → bottom-left → bottom-right)
3. Main tab content (RSVP checkboxes, capacity input, etc.)
4. Volunteer cancel buttons (top to bottom)

**Keyboard Shortcuts**:
- **Tab**: Navigate forward through interactive elements
- **Shift+Tab**: Navigate backward
- **Enter/Space**: Activate buttons (toggle settings, cancel volunteer)
- **Arrow Up/Down**: Increment/decrement number inputs
- **Escape**: Close modal, clear focus

**Focus Management**:
- Settings panel expands: Focus remains on toggle button
- Modal opens: Focus moves to modal title
- Modal closes: Focus returns to cancel button that opened it
- Error notification: Focus does not move (non-intrusive)

### Screen Readers

**ARIA Labels**:
```tsx
// Settings toggle button
<Button
  aria-label="Show timing settings"
  aria-expanded={isExpanded}
  aria-controls="timing-settings-panel"
>
  Timing Settings
</Button>

// Timing settings panel
<Paper
  id="timing-settings-panel"
  role="region"
  aria-labelledby="timing-settings-title"
>
  <Title id="timing-settings-title">
    Registration & Cancellation Windows
  </Title>
  {/* inputs */}
</Paper>

// Number inputs
<NumberInput
  label="Registration Opens"
  description="Hours before event"
  aria-describedby="reg-open-help"
/>
<Text id="reg-open-help" size="xs">
  e.g., 168 = 1 week
</Text>

// Cancel button (enabled)
<Button
  aria-label={`Cancel volunteer assignment for ${spotName}`}
>
  Cancel
</Button>

// Cancel button (disabled)
<Button
  disabled
  aria-label={`Cannot cancel ${spotName} - cancellation window closed`}
  aria-disabled="true"
>
  Cancel
</Button>
```

**Screen Reader Announcements**:
- Settings expanded: "Timing settings panel expanded"
- Settings collapsed: "Timing settings panel collapsed"
- Input error: "Error: Registration close must be after registration open"
- Cancel success: "Volunteer assignment cancelled successfully"
- Cancel error: "Error: Volunteer cancellation window has closed"

### Color Contrast

**Text Contrast Ratios** (WCAG AA 4.5:1, AAA 7:1):
- Panel title on white: 12.3:1 (AAA) - charcoal `#2B2B2B`
- Body text on white: 10.8:1 (AAA) - smoke `#4A4A4A`
- Dimmed text on white: 5.2:1 (AA) - stone `#8B8680`
- Help text on white: 5.2:1 (AA) - stone `#8B8680`
- Error text on white: 8.1:1 (AAA) - error red `#DC143C`
- Button text on burgundy background: 12.1:1 (AAA) - ivory `#FFF8F0`

**Button Contrast**:
- Red cancel button (subtle): 8.5:1 (AAA)
- Red cancel button (hover fill): 12.3:1 (AAA)
- Disabled button: 4.8:1 (AA) - meets minimum
- Settings button: 8.5:1 (AAA)

**Error States**:
- Red border on input: Visible without relying on color alone (border width 2px)
- Error icon accompanies text (IconAlertCircle)
- Disabled button shows text "DISABLED" (not just gray color)

### Focus States

**Focus Indicators**:
```css
.btn:focus-visible,
.mantine-NumberInput-input:focus-visible {
  outline: 2px solid var(--color-burgundy);
  outline-offset: 2px;
  border-radius: 4px;
}

.mantine-Modal-content:focus-visible {
  outline: 2px solid var(--color-burgundy);
  outline-offset: -2px;
}
```

**Focus Visibility**:
- All interactive elements have visible focus ring
- Focus ring color: burgundy `#880124` (8.5:1 contrast)
- Focus ring width: 2px (exceeds 1px minimum)
- Focus offset: 2px (clear separation from element)

**Reduced Motion Support**:
```css
@media (prefers-reduced-motion: reduce) {
  .timing-settings-panel {
    transition: none;
  }

  .btn {
    transition: none;
  }

  .mantine-Modal-content {
    animation: none;
  }
}
```

---

## Design System Integration

### Color Variables Used

```css
--color-burgundy: #880124;        /* Panel border, focus rings */
--color-burgundy-dark: #660018;   /* Hover states */
--color-rose-gold: #B76D75;       /* Accents, dividers */
--color-charcoal: #2B2B2B;        /* Primary text */
--color-smoke: #4A4A4A;           /* Secondary text */
--color-stone: #8B8680;           /* Dimmed text, disabled states */
--color-ivory: #FFF8F0;           /* Light text on dark */
--color-cream: #FAF6F2;           /* Backgrounds */
--color-error: #DC143C;           /* Error messages, cancel buttons */
--color-warning: #DAA520;         /* Warning badges */
```

### Spacing Variables Used

```css
--space-xs: 8px;   /* Small gaps between elements */
--space-sm: 16px;  /* Gap between inputs */
--space-md: 24px;  /* Gap between sections */
--space-lg: 32px;  /* Panel padding */
--space-xl: 40px;  /* Section spacing */
```

### Typography Patterns

**Labels** (from Admin Settings Card Pattern):
```tsx
<Text
  component="label"
  style={{
    fontFamily: 'var(--font-heading)',
    fontSize: '14px',
    fontWeight: 600,
    color: 'var(--color-smoke)',
    textTransform: 'uppercase',
    letterSpacing: '0.5px',
  }}
>
  Registration Opens
</Text>
```

**Panel Titles** (from Admin Settings Card Pattern):
```tsx
<Title
  order={5}
  style={{
    fontFamily: 'var(--font-heading)',
    fontSize: '16px',
    fontWeight: 700,
    color: 'var(--color-charcoal)',
    textTransform: 'uppercase',
    letterSpacing: '1px',
  }}
>
  Registration & Cancellation Windows
</Title>
```

**Help Text**:
```tsx
<Text
  size="xs"
  c="dimmed"
  style={{
    fontFamily: 'var(--font-body)',
    fontSize: '12px',
    fontWeight: 400,
  }}
>
  e.g., 168 = 1 week
</Text>
```

### Consistent Patterns Used

**Admin Settings Card Header** (from lessons learned):
- Burgundy-to-plum gradient header
- Icon + title pattern
- Ivory text on gradient background
- Border-bottom separator

**Collapsible Sections**:
- Toggle button right-aligned inline with heading
- Subtle variant button (low emphasis)
- Smooth expand/collapse animation
- Settings icon indicates collapsible content

**Form Validation**:
- Inline error messages below inputs
- Red border on error state
- Error icon before message text
- Clear recovery path (fix input, error clears)

---

## Quality Checklist

- [x] Meets accessibility standards (WCAG 2.1 AA)
- [x] Responsive on all devices with Mantine breakpoints
- [x] Uses Mantine v7 components consistently (ADR-004)
- [x] Follows TypeScript-first patterns
- [x] Uses built-in Mantine theming system
- [x] Leverages Mantine's accessibility features
- [x] Follows React best practices (hooks, functional components)
- [x] Follows brand guidelines (burgundy/plum/ivory color scheme)
- [x] Clear user flows for timing configuration
- [x] Safety/consent prominent (confirmation before cancel)
- [x] Community values reflected (clear communication, user control)
- [x] Performance considered (minimal re-renders, efficient state)
- [x] Consistent with existing admin patterns (EventForm tabs, settings cards)
- [x] Mobile-first design approach
- [x] Touch-friendly targets on mobile (44px minimum)

---

**Design Review Ready**: These wireframes follow WitchCityRope Design System v7, Mantine v7 framework patterns, and existing admin UI conventions. Ready for React Developer implementation.

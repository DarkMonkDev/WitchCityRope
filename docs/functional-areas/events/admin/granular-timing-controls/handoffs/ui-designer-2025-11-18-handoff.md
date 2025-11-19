# UI Designer Handoff - Granular Event Timing Controls
<!-- Date: 2025-11-18 -->
<!-- From: UI Designer Agent -->
<!-- To: React Developer Agent -->
<!-- Feature: Granular Event Timing Controls -->

## 🎯 HANDOFF SUMMARY

UI design phase complete for granular event timing controls. All wireframes, component specifications, and interaction patterns documented and ready for implementation.

**Design Deliverables**:
- 6 complete wireframes (ASCII format in wireframes.md)
- Mantine v7 component specifications
- Interaction patterns and animations
- Accessibility requirements (WCAG 2.1 AA compliant)
- Responsive breakpoint specifications
- Error state designs

**Status**: ✅ Design Phase Complete, Ready for Implementation Phase

---

## 📍 DESIGN ARTIFACTS LOCATION

**Primary Document**: `/docs/functional-areas/events/admin/granular-timing-controls/design/wireframes.md`

**Contents**:
1. RSVP/Tickets tab - settings collapsed
2. RSVP/Tickets tab - settings expanded (4 timing inputs)
3. Volunteers tab - settings expanded (2 timing inputs)
4. User volunteer assignment with cancel button
5. Cancel confirmation dialog
6. Error state - cancellation window closed

---

## 🎨 KEY DESIGN DECISIONS

### Visual Pattern: Right-Aligned Inline Settings

**Decision**: Settings toggle button positioned inline with tab title, right-aligned
**Rationale**: Follows existing EventForm SegmentedControl pattern (established admin UI convention)
**Reference**: EventForm.tsx line 378-423 pattern

**Implementation**:
```tsx
<Group justify="space-between" align="center">
  <Title order={3}>RSVP/Tickets</Title>
  <Button
    variant="subtle"
    leftSection={<IconSettings size={16} />}
    onClick={toggleSettings}
  >
    {isExpanded ? 'Hide Settings' : 'Timing Settings'}
  </Button>
</Group>
```

### Collapsible Settings Panel

**Decision**: Settings panel hidden by default, expands with slide animation
**Rationale**: Progressive disclosure - don't overwhelm users with advanced options
**Animation**: 0.3s ease slide-down/up transition

**Benefits**:
- Cleaner initial UI
- Advanced users can access timing controls
- Doesn't clutter simple event creation workflow
- Maintains focus on primary content

### 2x2 Grid for RSVP/Tickets Timing Inputs

**Decision**: 4 timing inputs arranged in 2x2 grid layout
**Rationale**:
- Logical grouping: Registration (opens/closes) + Cancellation (opens/closes)
- Visual balance on desktop
- Stacks to single column on mobile
- Clear relationship between paired inputs

**Responsive**:
- Desktop (≥768px): 2 columns side-by-side
- Mobile (<768px): Stack vertically

### NumberInput with Decimal Support

**Decision**: Mantine NumberInput with `decimalScale={1}`, `step={0.5}`, `allowNegative={true}`
**Rationale**:
- 0.5 step allows 30-minute increments (common use case)
- Negative values support post-event timing (e.g., cancellation 24 hours after)
- Empty state represents "no restriction" (flexible)
- Clear help text explains positive/negative meaning

**Visual Indicators**:
- Positive values: "hours BEFORE event start"
- Negative values: "hours AFTER event start"
- Examples: "168 = 1 week", "-24 = 24 hrs after"

### Burgundy Border Panel for Timing Settings

**Decision**: Left border 4px solid burgundy on settings panel
**Rationale**:
- Visual emphasis on advanced settings
- Separates from main content
- Follows established admin card pattern (lessons learned)
- Burgundy brand color for consistency

### Subtle Red Cancel Button

**Decision**: Cancel button uses subtle variant with red color (not destructive filled)
**Rationale**:
- Non-alarming visual (cancel is reversible via re-signup)
- Red indicates action consequence
- Subtle variant doesn't dominate layout
- Consistent with other secondary actions

### Confirmation Modal Before Cancel

**Decision**: Modal dialog confirms volunteer cancellation
**Rationale**:
- Prevents accidental cancellations
- Shows spot name for clarity
- Explains consequence ("frees up spot for another member")
- "No, Keep It" as primary escape route (outline button)

---

## 🧩 MANTINE COMPONENTS SPECIFIED

### Primary Components

| Component | Usage | Key Props |
|-----------|-------|-----------|
| `Title` | Tab titles, section headings | `order={3}` for tabs, `order={5}` for panels |
| `Paper` | Settings panel container, assignment cards | `p="xl"`, `withBorder`, `style={{ borderLeft: '4px solid var(--color-burgundy)' }}` |
| `Group` | Horizontal layouts (title + button) | `justify="space-between"`, `align="center"` |
| `Grid`, `Grid.Col` | Responsive timing input layout | `gutter="md"`, `span={{ base: 12, sm: 6 }}` |
| `Button` | Settings toggle, cancel actions | `variant="subtle"`, `color="red"`, `leftSection={<Icon />}` |
| `NumberInput` | Timing hour inputs | `decimalScale={1}`, `step={0.5}`, `allowNegative={true}`, `placeholder="No restriction"` |
| `Text` | Labels, descriptions, help text | `component="label"`, `size="xs"`, `c="dimmed"` |
| `Modal` | Confirmation dialogs | `opened`, `onClose`, `centered`, `size="md"` |
| `Notification` | Error messages | `color="red"`, `icon={<IconAlertCircle />}`, `onClose` |

### Component Props Reference

**NumberInput Configuration**:
```tsx
<NumberInput
  label="Registration Opens"
  description="Hours before event"
  decimalScale={1}
  step={0.5}
  allowNegative={true}
  placeholder="No restriction"
  min={undefined}  // No minimum
  max={undefined}  // No maximum
  hideControls={false}  // Show arrow buttons
  aria-describedby="reg-open-help"
/>
<Text id="reg-open-help" size="xs" c="dimmed">
  e.g., 168 = 1 week
</Text>
```

**Button States**:
```tsx
// Settings toggle
<Button
  variant="subtle"
  size="sm"
  leftSection={<IconSettings size={16} />}
  aria-expanded={isExpanded}
  aria-controls="timing-settings-panel"
>
  {isExpanded ? 'Hide Settings' : 'Timing Settings'}
</Button>

// Cancel button (enabled)
<Button
  variant="subtle"
  color="red"
  size="sm"
  leftSection={<IconX size={16} />}
  onClick={handleCancelClick}
>
  Cancel
</Button>

// Cancel button (disabled)
<Button
  variant="subtle"
  color="gray"
  size="sm"
  disabled
  aria-disabled="true"
>
  Cancel
</Button>
```

**Modal Configuration**:
```tsx
<Modal
  opened={showCancelModal}
  onClose={() => setShowCancelModal(false)}
  title="Cancel Volunteer Assignment?"
  centered
  size="md"
>
  <Text size="sm" mb="md">
    Are you sure you want to cancel your volunteer assignment for "{spotName}"?
  </Text>
  <Text size="sm" c="dimmed" mb="md">
    This will free up your spot for another member.
  </Text>
  <Text size="sm" weight={600} mb="xl">
    This action cannot be undone.
  </Text>

  <Group justify="flex-end" gap="sm">
    <Button
      variant="outline"
      color="gray"
      onClick={() => setShowCancelModal(false)}
    >
      No, Keep It
    </Button>
    <Button
      color="red"
      onClick={handleConfirmCancel}
    >
      Yes, Cancel
    </Button>
  </Group>
</Modal>
```

---

## 🔄 INTERACTION PATTERNS

### Settings Panel Toggle

**State Management**:
```tsx
const [isSettingsExpanded, setIsSettingsExpanded] = useState(false);

const toggleSettings = () => {
  setIsSettingsExpanded((prev) => !prev);
};
```

**Animation**:
- Slide down: 0.3s ease when expanding
- Slide up: 0.3s ease when collapsing
- Use Mantine Collapse component for smooth animation

**Implementation**:
```tsx
import { Collapse } from '@mantine/core';

<Button onClick={toggleSettings}>
  {isSettingsExpanded ? 'Hide Settings' : 'Timing Settings'}
</Button>

<Collapse in={isSettingsExpanded}>
  <Paper p="xl" withBorder mt="md">
    {/* Timing inputs */}
  </Paper>
</Collapse>
```

### Number Input Validation

**Real-time Validation**:
- Validate on blur (when user leaves input)
- Clear errors on change (when user starts typing)
- Show specific error messages

**Business Logic Validation**:
- Registration close must be after registration open
- Cancellation close should be reasonable (warn if very long)
- Empty values are valid (no restriction)

**Error Display**:
```tsx
const [errors, setErrors] = useState({
  regOpen: '',
  regClose: '',
  cancelOpen: '',
  cancelClose: '',
});

<NumberInput
  label="Registration Opens"
  error={errors.regOpen}
  onBlur={() => validateRegistrationOpen()}
  onChange={() => setErrors((prev) => ({ ...prev, regOpen: '' }))}
/>
```

### Volunteer Cancellation Flow

**User Flow**:
1. User clicks "Cancel" button on volunteer assignment
2. Check if within cancellation window (client-side check for immediate feedback)
3. If outside window: Show error notification, disable button
4. If within window: Open confirmation modal
5. User confirms: API call to cancel assignment
6. Success: Show success notification, remove from list
7. Error: Show error notification, keep in list

**API Error Handling**:
```tsx
const handleConfirmCancel = async () => {
  try {
    await cancelVolunteerAssignment(eventId, spotId);

    notifications.show({
      color: 'green',
      title: 'Success',
      message: 'Volunteer assignment cancelled successfully',
      icon: <IconCheck />,
      autoClose: 3000,
    });

    // Remove from UI
    removeAssignmentFromList(spotId);
    setShowCancelModal(false);

  } catch (error) {
    notifications.show({
      color: 'red',
      title: 'Error',
      message: error.message || 'Failed to cancel assignment. Please try again.',
      icon: <IconAlertCircle />,
      autoClose: 5000,
    });
  }
};
```

### Disabled Button States

**Dynamic Disable Logic**:
```tsx
const isCancellationAllowed = useMemo(() => {
  if (!event.volunteerCancelCloseHours) return true; // No restriction

  const eventStart = parseISO(event.startDateTime);
  const now = new Date();
  const hoursUntilEvent = differenceInHours(eventStart, now);

  return hoursUntilEvent >= event.volunteerCancelCloseHours;
}, [event.startDateTime, event.volunteerCancelCloseHours]);

<Button
  variant="subtle"
  color={isCancellationAllowed ? 'red' : 'gray'}
  disabled={!isCancellationAllowed}
  onClick={handleCancelClick}
>
  Cancel
</Button>

{!isCancellationAllowed && (
  <Text size="xs" c="orange" mt="xs" style={{ fontStyle: 'italic' }}>
    ⚠ Cancellation window closed ({event.volunteerCancelCloseHours} hrs before)
  </Text>
)}
```

---

## ♿ ACCESSIBILITY IMPLEMENTATION

### ARIA Attributes Required

**Settings Toggle**:
```tsx
<Button
  aria-label="Show timing settings"
  aria-expanded={isSettingsExpanded}
  aria-controls="timing-settings-panel"
>
  Timing Settings
</Button>

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
```

**Number Inputs**:
```tsx
<NumberInput
  label="Registration Opens"
  description="Hours before event"
  aria-describedby="reg-open-help"
/>
<Text id="reg-open-help" size="xs">
  e.g., 168 = 1 week
</Text>
```

**Cancel Button**:
```tsx
// Enabled
<Button
  aria-label={`Cancel volunteer assignment for ${spotName}`}
>
  Cancel
</Button>

// Disabled
<Button
  disabled
  aria-label={`Cannot cancel ${spotName} - cancellation window closed`}
  aria-disabled="true"
>
  Cancel
</Button>
```

### Keyboard Navigation Order

**Tab Sequence**:
1. Settings toggle button
2. Registration opens input
3. Registration closes input
4. Cancellation opens input
5. Cancellation closes input
6. Main tab content (RSVP checkboxes, etc.)
7. Volunteer cancel buttons (top to bottom)

**Focus Management**:
- Settings expand: Focus stays on toggle button
- Modal opens: Focus moves to modal title
- Modal closes: Focus returns to cancel button
- Error notification: Focus does not move

### Screen Reader Announcements

**State Changes**:
```tsx
// Settings expanded
<Collapse
  in={isSettingsExpanded}
  onEntered={() => {
    // Announce to screen readers
    announceToScreenReader('Timing settings panel expanded');
  }}
  onExited={() => {
    announceToScreenReader('Timing settings panel collapsed');
  }}
>
  {/* Panel content */}
</Collapse>
```

**Error Announcements**:
```tsx
notifications.show({
  color: 'red',
  title: 'Error',
  message: 'Volunteer cancellation window has closed for this event',
  role: 'alert', // Screen reader announces immediately
  'aria-live': 'assertive',
});
```

### Color Contrast Compliance

**All text meets WCAG AA (4.5:1 minimum)**:
- Panel title: 12.3:1 (AAA) - charcoal on white
- Body text: 10.8:1 (AAA) - smoke on white
- Dimmed text: 5.2:1 (AA) - stone on white
- Error text: 8.1:1 (AAA) - error red on white
- Button text on burgundy: 12.1:1 (AAA) - ivory on burgundy

**Focus Indicators**:
```css
.btn:focus-visible,
.mantine-NumberInput-input:focus-visible {
  outline: 2px solid var(--color-burgundy);
  outline-offset: 2px;
}
```

---

## 📱 RESPONSIVE DESIGN SPECIFICATIONS

### Breakpoint Behavior

**Mobile (<768px)**:
- Timing inputs stack vertically (Grid.Col span={12})
- Settings panel full-width
- Cancel button stacks below assignment details
- Notification full-width at top

**Desktop (≥768px)**:
- Timing inputs side-by-side (Grid.Col span={6})
- Settings panel maintains max-width
- Cancel button inline with assignment
- Standard layout

### Component Responsive Props

```tsx
<Grid gutter="md">
  <Grid.Col span={{ base: 12, sm: 6 }}>
    <NumberInput label="Registration Opens" />
  </Grid.Col>
  <Grid.Col span={{ base: 12, sm: 6 }}>
    <NumberInput label="Registration Closes" />
  </Grid.Col>
</Grid>
```

**Button Groups**:
```tsx
// Desktop: Horizontal, right-aligned
// Mobile: Vertical stack, full-width
<Group
  justify={{ base: 'stretch', sm: 'flex-end' }}
  gap="sm"
  style={{
    flexDirection: { base: 'column', sm: 'row' }
  }}
>
  <Button>No, Keep It</Button>
  <Button>Yes, Cancel</Button>
</Group>
```

### Touch Targets (Mobile)

**Minimum sizes**:
- Buttons: 44px × 44px minimum (iOS guideline)
- Number input controls: 44px × 44px
- Clickable cards: 56px minimum height

**Implementation**:
```tsx
<Button
  size="sm"
  style={{
    minHeight: '44px',
    minWidth: '44px',
  }}
>
  Cancel
</Button>
```

---

## 🎨 DESIGN SYSTEM INTEGRATION

### Color Variables

**Use CSS variables from Design System v7**:
```tsx
<Paper
  style={{
    borderLeft: '4px solid var(--color-burgundy)',
    background: 'var(--color-cream)',
  }}
>
  {/* Content */}
</Paper>

<Text
  style={{
    color: 'var(--color-charcoal)',
  }}
>
  Label
</Text>
```

**Color Reference**:
- `--color-burgundy`: Panel borders, focus rings
- `--color-charcoal`: Primary text
- `--color-smoke`: Secondary text
- `--color-stone`: Dimmed text
- `--color-error`: Error messages, cancel buttons
- `--color-warning`: Warning badges
- `--color-ivory`: Light text on dark backgrounds
- `--color-cream`: Panel backgrounds

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

**Help Text**:
```tsx
<Text
  size="xs"
  c="dimmed"
  style={{
    fontFamily: 'var(--font-body)',
    fontSize: '12px',
  }}
>
  e.g., 168 = 1 week
</Text>
```

### Spacing Consistency

**Use spacing variables**:
```tsx
<Paper
  p="xl"          // 40px padding
  mb="md"         // 24px margin-bottom
>
  <Stack gap="lg"> {/* 32px gap */}
    {/* Content */}
  </Stack>
</Paper>
```

---

## 🔍 TESTING GUIDANCE

### Manual Testing Checklist

**Visual**:
- [ ] Settings button aligned right with tab title
- [ ] Settings panel has 4px burgundy left border
- [ ] Timing inputs display in 2x2 grid on desktop
- [ ] Inputs stack vertically on mobile (<768px)
- [ ] Cancel button red when enabled, gray when disabled
- [ ] Modal centers on screen with backdrop
- [ ] Error notification appears at top-right

**Interaction**:
- [ ] Settings toggle expands/collapses panel smoothly
- [ ] Number input arrows increment by 0.5
- [ ] Negative values allowed in number inputs
- [ ] Empty inputs show "No restriction" placeholder
- [ ] Cancel button opens confirmation modal
- [ ] "No, Keep It" closes modal without action
- [ ] "Yes, Cancel" triggers API call and shows notification
- [ ] Disabled cancel button shows warning text

**Accessibility**:
- [ ] All interactive elements keyboard accessible
- [ ] Tab order logical (top to bottom, left to right)
- [ ] Focus indicators visible on all elements
- [ ] Screen reader announces state changes
- [ ] ARIA attributes present on all interactive elements
- [ ] Color contrast meets WCAG AA standards
- [ ] Error messages announced to screen readers

**Responsive**:
- [ ] Layout works at 320px width (smallest mobile)
- [ ] Layout works at 768px (tablet breakpoint)
- [ ] Layout works at 1920px (large desktop)
- [ ] Touch targets ≥44px on mobile
- [ ] Text readable at all sizes
- [ ] No horizontal scrolling

**Error Handling**:
- [ ] Invalid number format shows error
- [ ] Registration close before open shows error
- [ ] API error shows notification
- [ ] Network failure shows retry option
- [ ] Cancellation outside window shows specific message

### Automated Testing Suggestions

**Component Tests** (Jest + React Testing Library):
```tsx
describe('TimingSettings', () => {
  it('toggles settings panel visibility', async () => {
    const { getByText, queryByText } = render(<TimingSettings />);

    // Panel hidden initially
    expect(queryByText('Registration Opens')).not.toBeInTheDocument();

    // Click toggle
    await userEvent.click(getByText('Timing Settings'));

    // Panel visible
    expect(getByText('Registration Opens')).toBeInTheDocument();
  });

  it('validates registration timing logic', async () => {
    const { getByLabelText, getByText } = render(<TimingSettings />);

    // Set close before open (invalid)
    await userEvent.type(getByLabelText('Registration Opens'), '1');
    await userEvent.type(getByLabelText('Registration Closes'), '168');

    // Error message appears
    expect(getByText(/close must be after open/i)).toBeInTheDocument();
  });
});
```

**E2E Tests** (Playwright):
```typescript
test('volunteer cancellation flow', async ({ page }) => {
  // Navigate to volunteer assignments
  await page.goto('/dashboard/volunteers');

  // Click cancel button
  await page.getByRole('button', { name: /cancel/i }).first().click();

  // Modal appears
  await expect(page.getByText(/cancel volunteer assignment/i)).toBeVisible();

  // Confirm cancellation
  await page.getByRole('button', { name: /yes, cancel/i }).click();

  // Success notification
  await expect(page.getByText(/cancelled successfully/i)).toBeVisible();

  // Assignment removed from list
  await expect(page.getByText(/setup crew/i)).not.toBeVisible();
});
```

---

## 📋 IMPLEMENTATION NOTES

### State Management

**Timing Settings State**:
```tsx
const [timingSettings, setTimingSettings] = useState({
  registrationOpenHours: null,
  registrationCloseHours: null,
  cancellationOpenHours: null,
  cancellationCloseHours: null,
  volunteerSignupCloseHours: null,
  volunteerCancelCloseHours: null,
});

const handleTimingChange = (field: string, value: number | null) => {
  setTimingSettings((prev) => ({
    ...prev,
    [field]: value,
  }));
};
```

**Validation State**:
```tsx
const [errors, setErrors] = useState<Record<string, string>>({});

const validateTimings = () => {
  const newErrors: Record<string, string> = {};

  // Registration close must be after open
  if (
    timingSettings.registrationOpenHours !== null &&
    timingSettings.registrationCloseHours !== null &&
    timingSettings.registrationCloseHours > timingSettings.registrationOpenHours
  ) {
    newErrors.registrationClose = 'Close time must be closer to event than open time';
  }

  setErrors(newErrors);
  return Object.keys(newErrors).length === 0;
};
```

### API Integration

**Event DTO Updates** (from backend):
```typescript
interface EventDto {
  // Existing fields...
  registrationOpenHours?: number | null;
  registrationCloseHours?: number | null;
  cancellationOpenHours?: number | null;
  cancellationCloseHours?: number | null;
  volunteerSignupCloseHours?: number | null;
  volunteerCancelCloseHours?: number | null;
}
```

**Save Timing Settings**:
```tsx
const handleSaveEvent = async () => {
  if (!validateTimings()) return;

  const eventData = {
    ...existingEventData,
    registrationOpenHours: timingSettings.registrationOpenHours,
    registrationCloseHours: timingSettings.registrationCloseHours,
    cancellationOpenHours: timingSettings.cancellationOpenHours,
    cancellationCloseHours: timingSettings.cancellationCloseHours,
    volunteerSignupCloseHours: timingSettings.volunteerSignupCloseHours,
    volunteerCancelCloseHours: timingSettings.volunteerCancelCloseHours,
  };

  await updateEvent(eventId, eventData);
};
```

**Cancel Volunteer Assignment**:
```tsx
const cancelVolunteerAssignment = async (eventId: number, spotId: number) => {
  const response = await fetch(`/api/events/${eventId}/volunteers/${spotId}`, {
    method: 'DELETE',
    credentials: 'include', // httpOnly cookie auth
  });

  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.message);
  }

  return response.json();
};
```

### Performance Considerations

**Debounce Input Validation**:
```tsx
import { useDebouncedCallback } from '@mantine/hooks';

const debouncedValidate = useDebouncedCallback(() => {
  validateTimings();
}, 500);

<NumberInput
  onChange={(value) => {
    handleTimingChange('registrationOpenHours', value);
    debouncedValidate();
  }}
/>
```

**Memoize Disabled State Calculation**:
```tsx
const isCancellationAllowed = useMemo(() => {
  // Calculation logic
}, [event.startDateTime, event.volunteerCancelCloseHours]);
```

---

## ✅ HANDOFF CHECKLIST

- [x] 6 wireframes created (ASCII format in wireframes.md)
- [x] Mantine v7 component specifications documented
- [x] Interaction patterns defined
- [x] Accessibility requirements specified (WCAG 2.1 AA)
- [x] Responsive breakpoints documented
- [x] Error state designs provided
- [x] Design system integration guidelines
- [x] Testing guidance included
- [x] Implementation notes for React Developer
- [x] API integration patterns specified
- [x] Performance considerations documented

---

## 🤝 NEXT STEPS

**React Developer Tasks**:
1. Review wireframes and component specifications
2. Implement timing settings panel in EventForm component
3. Add collapsible settings with Mantine Collapse component
4. Create volunteer assignment cancellation UI
5. Implement confirmation modal and error notifications
6. Add client-side validation for timing logic
7. Integrate with backend API endpoints
8. Implement accessibility features (ARIA, keyboard nav)
9. Test responsive behavior at all breakpoints
10. Create handoff document for Test Developer

**Questions?**
- Wireframes: `/docs/functional-areas/events/admin/granular-timing-controls/design/wireframes.md`
- Design System: `/docs/design/current/design-system-v7.md`
- Mantine Components: https://mantine.dev/core/number-input/

---

**Handoff Complete**: UI design ready for React Developer implementation.

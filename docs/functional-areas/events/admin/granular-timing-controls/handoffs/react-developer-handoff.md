# React Developer Handoff - Granular Event Timing Controls
<!-- Date: 2025-11-18 -->
<!-- From: Backend Developer -->
<!-- To: React Developer Agent -->
<!-- Feature: Granular Event Timing Controls -->

## 🎯 CRITICAL FRONTEND REQUIREMENTS (MUST IMPLEMENT)

### 1. Settings Sections Must Be Inline with Tab Titles
**Rule**: Timing settings button appears on same line as tab title, right-aligned.
- ✅ Correct: `<Group justify="space-between">` with Title + Settings Button
- ❌ Wrong: Settings button on separate line below title
- ❌ Wrong: Settings always visible without toggle

### 2. Use Mantine NumberInput with Decimal Support
**Rule**: Timing inputs must support 0.5 hour increments (30 minutes).
- ✅ Correct: `<NumberInput step={0.5} precision={1} />`
- ❌ Wrong: `<TextInput />` - no number validation
- ❌ Wrong: `step={1}` - doesn't support 30-minute increments

### 3. Negative Values Allowed (Post-Event Timing)
**Rule**: Users can configure timing up to -24 hours (24 hours AFTER event).
- ✅ Correct: `allowNegative min={-24} max={8760}`
- ❌ Wrong: `min={0}` - prevents post-event timing
- ❌ Wrong: No validation - allows < -24

### 4. NULL Means No Restriction (Empty Fields Allowed)
**Rule**: Empty fields = NULL in API = no restriction on timing.
- ✅ Correct: Allow empty NumberInput, send `null` to API
- ❌ Wrong: Default to 0 - changes event behavior
- ❌ Wrong: Require values - breaks backward compatibility

### 5. User Volunteer Cancel Button Required
**Rule**: Users must be able to cancel their own volunteer assignments.
- ✅ Correct: Cancel button on UserVolunteerShifts component
- ❌ Wrong: Admin-only cancel - users cannot self-cancel
- ❌ Wrong: Delete action - should be cancel (state change)

## 📍 KEY DOCUMENTS TO READ

| Document | Path | Critical Sections |
|----------|------|-------------------|
| Implementation Plan | `/home/chad/repos/witchcityrope/docs/functional-areas/events/admin/granular-timing-controls/implementation-plan.md` | UI Component Changes section |
| Backend Developer Handoff | `/home/chad/repos/witchcityrope/docs/functional-areas/events/admin/granular-timing-controls/handoffs/backend-developer-handoff.md` | EventDto specification |
| EventForm Current | `/apps/web/src/features/events/components/EventForm.tsx` | Existing tab structure |
| UserVolunteerShifts Current | `/apps/web/src/features/events/components/UserVolunteerShifts.tsx` | Existing volunteer UI |
| Mantine NumberInput Docs | Mantine v7 documentation | Component API |

## 🚨 KNOWN PITFALLS

### Pitfall 1: Placing Settings Below Tab Title
**Why it happens**: Natural to put settings in content area
**How to avoid**: Use `Group justify="space-between"` pattern - title left, button right, same line

### Pitfall 2: Not Supporting Decimal Values
**Why it happens**: Thinking hours are whole numbers
**How to avoid**: User requirement: 0.5 = 30 minutes, must use `step={0.5} precision={1}`

### Pitfall 3: Defaulting Empty Fields to Zero
**Why it happens**: Assuming all events need timing configured
**How to avoid**: Empty field = NULL in API = no restriction, don't default to 0

### Pitfall 4: Disabling Negative Values
**Why it happens**: Negative hours seem illogical
**How to avoid**: User requirement: -24 = 24 hours AFTER event, must allow negative with `allowNegative`

### Pitfall 5: Forgetting to Regenerate TypeScript Types
**Why it happens**: Assuming manual interfaces are fine
**How to avoid**: MUST run `npm run generate` in shared-types after backend DTO changes

## ✅ VALIDATION CHECKLIST

Before proceeding to testing, verify:

- [ ] TypeScript types regenerated from updated EventDto
- [ ] EventForm has RSVP/Tickets timing settings section
- [ ] EventForm has Volunteers timing settings section
- [ ] Settings sections collapsible (toggle button)
- [ ] Settings button inline with tab title (right-aligned)
- [ ] NumberInput components configured correctly:
  - [ ] `step={0.5}` for 30-minute increments
  - [ ] `precision={1}` for one decimal place
  - [ ] `min={-24}` for post-event maximum
  - [ ] `max={8760}` for reasonable upper limit (1 year)
  - [ ] `allowNegative` enabled
- [ ] Form validation rejects < -24 values
- [ ] Empty fields allowed (NULL in API)
- [ ] UserVolunteerShifts has cancel button
- [ ] Volunteer cancel button disabled when outside window
- [ ] volunteerApi.cancelVolunteerSignup method created
- [ ] Error handling for cancel failures
- [ ] All components type-safe with generated types
- [ ] No TypeScript compilation errors

## 🔄 DISCOVERED CONSTRAINTS

### Existing EventForm Tab Structure
**Location**: `/apps/web/src/features/events/components/EventForm.tsx`
**Current Pattern**: Tabs component with separate tab panels
**Impact**: Settings sections must fit into existing tab structure
**Required Changes**: Add settings toggle + collapsible section to RSVP/Tickets and Volunteers tabs

### Existing SegmentedControl Pattern
**Location**: EventForm.tsx, line 378-423
**Pattern**: Right-aligned controls on same line as section title
**Impact**: Use similar pattern for timing settings button
**Required Changes**: Replace SegmentedControl with Button for timing settings toggle

### Auto-Generated TypeScript Types
**Location**: `packages/shared-types/src/generated-api-types.ts`
**Current State**: Backend added 6 timing fields to EventDto
**Impact**: Must regenerate types before implementing UI
**Required Changes**: Run `npm run generate` in shared-types package

### User Volunteer Assignment Display
**Location**: `/apps/web/src/features/events/components/UserVolunteerShifts.tsx`
**Current State**: Shows user's volunteer assignments per event
**Impact**: Must add cancel button to each assignment
**Required Changes**: Add button + API call + error handling

## 📊 COMPONENT SPECIFICATIONS

### EventForm - RSVP/Tickets Tab Timing Settings

**Location**: `/apps/web/src/features/events/components/EventForm.tsx`

```tsx
import { useState } from 'react';
import { Group, Title, Button, Paper, Grid, NumberInput } from '@mantine/core';
import { IconSettings } from '@tabler/icons-react';

// Inside RSVP/Tickets tab panel:
const [showRsvpSettings, setShowRsvpSettings] = useState(false);

<Box>
  {/* Tab title with settings button */}
  <Group justify="space-between" mb="md">
    <Title order={3}>RSVP/Tickets</Title>
    <Button
      variant="subtle"
      leftSection={<IconSettings size={16} />}
      onClick={() => setShowRsvpSettings(!showRsvpSettings)}
      size="sm"
    >
      {showRsvpSettings ? 'Hide' : 'Show'} Timing Settings
    </Button>
  </Group>

  {/* Collapsible timing settings section */}
  {showRsvpSettings && (
    <Paper p="md" mb="md" withBorder>
      <Title order={5} mb="sm">Registration & Cancellation Windows</Title>
      <Text size="sm" c="dimmed" mb="md">
        Configure when users can register for and cancel RSVPs/Tickets.
        Positive hours = before event start, negative hours = after event start.
        Leave empty for no restriction.
      </Text>

      <Grid>
        <Grid.Col span={6}>
          <NumberInput
            label="Registration Opens"
            description="Hours before event when registration becomes available"
            placeholder="e.g., 168 = 1 week before"
            value={form.values.registrationOpenHours ?? undefined}
            onChange={(val) => form.setFieldValue('registrationOpenHours', val === '' ? null : val)}
            step={0.5}
            precision={1}
            min={-24}
            max={8760}
            allowNegative
            clearable
            {...form.getInputProps('registrationOpenHours')}
          />
        </Grid.Col>

        <Grid.Col span={6}>
          <NumberInput
            label="Registration Closes"
            description="Hours before event when registration ends"
            placeholder="e.g., 1 = 1 hour before"
            value={form.values.registrationCloseHours ?? undefined}
            onChange={(val) => form.setFieldValue('registrationCloseHours', val === '' ? null : val)}
            step={0.5}
            precision={1}
            min={-24}
            max={8760}
            allowNegative
            clearable
            {...form.getInputProps('registrationCloseHours')}
          />
        </Grid.Col>

        <Grid.Col span={6}>
          <NumberInput
            label="Cancellation Opens"
            description="Hours before event when cancellation becomes available"
            placeholder="e.g., 168 = 1 week before"
            value={form.values.cancellationOpenHours ?? undefined}
            onChange={(val) => form.setFieldValue('cancellationOpenHours', val === '' ? null : val)}
            step={0.5}
            precision={1}
            min={-24}
            max={8760}
            allowNegative
            clearable
            {...form.getInputProps('cancellationOpenHours')}
          />
        </Grid.Col>

        <Grid.Col span={6}>
          <NumberInput
            label="Cancellation Closes"
            description="Hours before event when cancellation ends"
            placeholder="e.g., -24 = 24 hours after event"
            value={form.values.cancellationCloseHours ?? undefined}
            onChange={(val) => form.setFieldValue('cancellationCloseHours', val === '' ? null : val)}
            step={0.5}
            precision={1}
            min={-24}
            max={8760}
            allowNegative
            clearable
            {...form.getInputProps('cancellationCloseHours')}
          />
        </Grid.Col>
      </Grid>
    </Paper>
  )}

  {/* Existing RSVP/Tickets tab content */}
</Box>
```

### EventForm - Volunteers Tab Timing Settings

```tsx
// Inside Volunteers tab panel:
const [showVolunteerSettings, setShowVolunteerSettings] = useState(false);

<Box>
  {/* Tab title with settings button */}
  <Group justify="space-between" mb="md">
    <Title order={3}>Volunteers</Title>
    <Button
      variant="subtle"
      leftSection={<IconSettings size={16} />}
      onClick={() => setShowVolunteerSettings(!showVolunteerSettings)}
      size="sm"
    >
      {showVolunteerSettings ? 'Hide' : 'Show'} Timing Settings
    </Button>
  </Group>

  {/* Collapsible timing settings section */}
  {showVolunteerSettings && (
    <Paper p="md" mb="md" withBorder>
      <Title order={5} mb="sm">Volunteer Timing Windows</Title>
      <Text size="sm" c="dimmed" mb="md">
        Configure when users can sign up for and cancel volunteer spots.
        Positive hours = before event start, negative hours = after event start.
        Leave empty for no restriction.
      </Text>

      <Grid>
        <Grid.Col span={6}>
          <NumberInput
            label="Volunteer Signup Closes"
            description="Hours before event when volunteer signup ends"
            placeholder="e.g., 24 = 1 day before"
            value={form.values.volunteerRegistrationCloseHours ?? undefined}
            onChange={(val) => form.setFieldValue('volunteerRegistrationCloseHours', val === '' ? null : val)}
            step={0.5}
            precision={1}
            min={-24}
            max={8760}
            allowNegative
            clearable
            {...form.getInputProps('volunteerRegistrationCloseHours')}
          />
        </Grid.Col>

        <Grid.Col span={6}>
          <NumberInput
            label="Volunteer Cancel Closes"
            description="Hours before event when volunteer cancellation ends"
            placeholder="e.g., 48 = 2 days before"
            value={form.values.volunteerCancellationCloseHours ?? undefined}
            onChange={(val) => form.setFieldValue('volunteerCancellationCloseHours', val === '' ? null : val)}
            step={0.5}
            precision={1}
            min={-24}
            max={8760}
            allowNegative
            clearable
            {...form.getInputProps('volunteerCancellationCloseHours')}
          />
        </Grid.Col>
      </Grid>
    </Paper>
  )}

  {/* Existing Volunteers tab content */}
</Box>
```

### Form Validation

**Location**: EventForm.tsx, inside `useForm()` hook

```tsx
import { useForm } from '@mantine/form';

const form = useForm({
  initialValues: {
    // ... existing fields ...
    registrationOpenHours: null,
    registrationCloseHours: null,
    cancellationOpenHours: null,
    cancellationCloseHours: null,
    volunteerRegistrationCloseHours: null,
    volunteerCancellationCloseHours: null,
  },

  validate: {
    // ... existing validations ...
    registrationOpenHours: (value) =>
      value !== null && value < -24
        ? 'Cannot be more than 24 hours after event start'
        : null,
    registrationCloseHours: (value) =>
      value !== null && value < -24
        ? 'Cannot be more than 24 hours after event start'
        : null,
    cancellationOpenHours: (value) =>
      value !== null && value < -24
        ? 'Cannot be more than 24 hours after event start'
        : null,
    cancellationCloseHours: (value) =>
      value !== null && value < -24
        ? 'Cannot be more than 24 hours after event start'
        : null,
    volunteerRegistrationCloseHours: (value) =>
      value !== null && value < -24
        ? 'Cannot be more than 24 hours after event start'
        : null,
    volunteerCancellationCloseHours: (value) =>
      value !== null && value < -24
        ? 'Cannot be more than 24 hours after event start'
        : null,
  },
});
```

### UserVolunteerShifts - Cancel Button

**Location**: `/apps/web/src/features/events/components/UserVolunteerShifts.tsx`

```tsx
import { useState } from 'react';
import { Button, Group, Text } from '@mantine/core';
import { IconX } from '@tabler/icons-react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { cancelVolunteerSignup } from '@/api/volunteerApi';
import { notifications } from '@mantine/notifications';

// Inside component, for each volunteer assignment:
const queryClient = useQueryClient();

const cancelMutation = useMutation({
  mutationFn: (signupId: string) => cancelVolunteerSignup(signupId),
  onSuccess: () => {
    notifications.show({
      title: 'Success',
      message: 'Volunteer assignment cancelled',
      color: 'green',
    });
    queryClient.invalidateQueries({ queryKey: ['volunteer-signups'] });
  },
  onError: (error: any) => {
    notifications.show({
      title: 'Error',
      message: error.message || 'Failed to cancel volunteer assignment',
      color: 'red',
    });
  },
});

const handleCancel = (signupId: string) => {
  if (window.confirm('Are you sure you want to cancel this volunteer assignment?')) {
    cancelMutation.mutate(signupId);
  }
};

// In JSX for each assignment:
<Group justify="space-between">
  <div>
    <Text fw={500}>{spot.name}</Text>
    <Text size="sm" c="dimmed">{spot.description}</Text>
  </div>
  <Button
    variant="subtle"
    color="red"
    size="xs"
    leftSection={<IconX size={14} />}
    onClick={() => handleCancel(signup.id)}
    loading={cancelMutation.isPending}
  >
    Cancel
  </Button>
</Group>
```

### Volunteer API Integration

**Location**: `/apps/web/src/api/volunteerApi.ts` (create or update)

```tsx
import { apiClient } from './apiClient';

export const cancelVolunteerSignup = async (signupId: string): Promise<void> => {
  const response = await apiClient.post(`/api/volunteer-signups/${signupId}/cancel`);

  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.error || 'Failed to cancel volunteer signup');
  }
};
```

### TypeScript Type Generation

**CRITICAL**: Run this BEFORE implementing UI:

```bash
cd packages/shared-types
npm run generate
```

**Verify types include timing fields**:
```typescript
import type { components } from '@witchcityrope/shared-types';

export type EventDto = components['schemas']['EventDto'];

// Should include:
// - registrationOpenHours?: number | null
// - registrationCloseHours?: number | null
// - cancellationOpenHours?: number | null
// - cancellationCloseHours?: number | null
// - volunteerRegistrationCloseHours?: number | null
// - volunteerCancellationCloseHours?: number | null
```

## 🎯 SUCCESS CRITERIA

### Component Tests

**EventForm.test.tsx** - Test timing settings:
```tsx
describe('EventForm - Timing Settings', () => {
  it('shows RSVP/Tickets timing settings when button clicked', () => {
    render(<EventForm />);
    const settingsButton = screen.getByText(/Show Timing Settings/i);
    fireEvent.click(settingsButton);
    expect(screen.getByText(/Registration Opens/i)).toBeInTheDocument();
  });

  it('accepts decimal values for timing hours', () => {
    render(<EventForm />);
    const input = screen.getByLabelText(/Registration Opens/i);
    fireEvent.change(input, { target: { value: '0.5' } });
    expect(input).toHaveValue(0.5);
  });

  it('accepts negative values for post-event timing', () => {
    render(<EventForm />);
    const input = screen.getByLabelText(/Cancellation Closes/i);
    fireEvent.change(input, { target: { value: '-24' } });
    expect(input).toHaveValue(-24);
  });

  it('rejects values less than -24', async () => {
    render(<EventForm />);
    const input = screen.getByLabelText(/Registration Opens/i);
    fireEvent.change(input, { target: { value: '-25' } });
    fireEvent.blur(input);
    await waitFor(() => {
      expect(screen.getByText(/Cannot be more than 24 hours after/i)).toBeInTheDocument();
    });
  });

  it('allows empty values (NULL in API)', () => {
    render(<EventForm />);
    const input = screen.getByLabelText(/Registration Opens/i);
    fireEvent.change(input, { target: { value: '' } });
    expect(input).toHaveValue(null);
  });
});
```

**UserVolunteerShifts.test.tsx** - Test cancel button:
```tsx
describe('UserVolunteerShifts - Cancel Button', () => {
  it('shows cancel button for user assignments', () => {
    render(<UserVolunteerShifts assignments={mockAssignments} />);
    const cancelButtons = screen.getAllByText(/Cancel/i);
    expect(cancelButtons).toHaveLength(mockAssignments.length);
  });

  it('calls cancel API when button clicked and confirmed', async () => {
    const mockCancel = vi.fn();
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    render(<UserVolunteerShifts assignments={mockAssignments} />);

    const cancelButton = screen.getAllByText(/Cancel/i)[0];
    fireEvent.click(cancelButton);

    await waitFor(() => {
      expect(mockCancel).toHaveBeenCalledWith(mockAssignments[0].id);
    });
  });

  it('does not call cancel API when confirmation denied', async () => {
    const mockCancel = vi.fn();
    vi.spyOn(window, 'confirm').mockReturnValue(false);
    render(<UserVolunteerShifts assignments={mockAssignments} />);

    const cancelButton = screen.getAllByText(/Cancel/i)[0];
    fireEvent.click(cancelButton);

    expect(mockCancel).not.toHaveBeenCalled();
  });

  it('shows error notification on cancel failure', async () => {
    const mockError = new Error('Cancellation window closed');
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    // Mock API to throw error

    render(<UserVolunteerShifts assignments={mockAssignments} />);
    const cancelButton = screen.getAllByText(/Cancel/i)[0];
    fireEvent.click(cancelButton);

    await waitFor(() => {
      expect(screen.getByText(/Cancellation window closed/i)).toBeInTheDocument();
    });
  });
});
```

## ⚠️ DO NOT IMPLEMENT

- ❌ DO NOT create manual TypeScript interfaces for EventDto (use generated types)
- ❌ DO NOT default empty fields to 0 (must be NULL)
- ❌ DO NOT disable negative values (post-event timing required)
- ❌ DO NOT use TextInput instead of NumberInput (need decimal support)
- ❌ DO NOT put settings in separate modal (must be inline, collapsible)
- ❌ DO NOT make timing settings always visible (must be toggle-able)
- ❌ DO NOT forget to regenerate TypeScript types before starting

## 📝 TERMINOLOGY DICTIONARY

| Term | Definition | Example |
|------|------------|---------|
| Timing Window | Period when specific action is allowed | Registration window = 7 days before to 1 hour before |
| Collapsible Section | UI element that can be shown/hidden via button | Settings Paper shown when toggle button clicked |
| NumberInput | Mantine component for numeric input with validation | Supports decimals, min/max, step increments |
| Inline Settings | Settings button on same line as section title | `Group justify="space-between"` pattern |
| Post-Event Timing | Negative hour values for timing after event starts | -24 = 24 hours after event |
| NULL Restriction | Empty field = no restriction on timing | Empty Registration Opens = can register any time before |

## 🔗 NEXT AGENT INSTRUCTIONS

### Test Developer Agent
**FIRST**: Read this handoff document completely
**SECOND**: Verify UI implementation deployed to staging:
```bash
# Check EventForm has timing settings
# Navigate to admin event creation/edit
# Click "Show Timing Settings" on RSVP/Tickets tab
# Verify NumberInput components appear with correct configuration
```
**THIRD**: Verify volunteer cancel button:
```bash
# Navigate to user volunteer assignments page
# Verify Cancel button appears for each assignment
# Test cancel flow with API integration
```
**FOURTH**: Read test developer handoff for E2E test creation
**THEN**: Begin E2E test suite for timing settings

## 🤝 HANDOFF CONFIRMATION

**Previous Agent**: Backend Developer Agent
**Previous Phase Completed**: 2025-11-18 (Backend API Implementation)
**Key Finding**: Backend API now supports per-event timing controls with EventDto exposing 6 timing fields and new volunteer cancel endpoint operational

**Next Agent Should Be**: Test Developer Agent
**Next Phase**: E2E Testing (Phase 4)
**Estimated Effort**: 2-3 days for UI components, form validation, API integration, and component testing

---

## Exact File Paths for Implementation

**Component Files** (update):
- `/apps/web/src/features/events/components/EventForm.tsx` - Add timing settings sections
- `/apps/web/src/features/events/components/UserVolunteerShifts.tsx` - Add cancel button

**API Files** (create or update):
- `/apps/web/src/api/volunteerApi.ts` - Add cancelVolunteerSignup method

**Type Files** (regenerate):
- `/packages/shared-types/src/generated-api-types.ts` - Run npm run generate

**Test Files** (create):
- `/apps/web/src/features/events/components/EventForm.test.tsx` - Test timing settings UI
- `/apps/web/src/features/events/components/UserVolunteerShifts.test.tsx` - Test cancel button

**Style Files** (if needed):
- `/apps/web/src/features/events/components/EventForm.module.css` - Custom styling for timing settings

---

**This handoff document contains all information needed for frontend UI implementation. Proceed with confidence!**

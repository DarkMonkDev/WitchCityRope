# UI Wireframes: Admin RSVP Removal & Ticket Refund Modals
<!-- Last Updated: 2025-11-09 -->
<!-- Version: 1.0 -->
<!-- Owner: UI Designer Agent -->
<!-- Status: Draft -->

## Design Overview

These modals provide confirmation and impact visibility for admin actions that remove RSVPs and refund tickets. Both actions are destructive and may have cascading effects (removing volunteer assignments), so the design emphasizes:

1. **Clear Impact Visibility**: Show ALL cascading effects before confirmation
2. **Appropriate Friction**: Prevent accidental clicks without being burdensome
3. **Safety-First Language**: Respectful, clear communication about participant impact
4. **Mantine UI Consistency**: Follow existing modal patterns (DenyApplicationModal)

## User Personas

- **Admin**: Event organizers managing event participation, removing RSVPs/tickets when needed
- **Use Cases**:
  - Participant requests removal/refund
  - Admin needs to free up capacity
  - Error correction (duplicate registration)
  - Policy violation requiring removal

## Wireframes

### 1. RSVP Removal Modal

**Trigger**: Admin clicks "Remove" link in RSVP Management table (Attendees tab)

**Desktop Layout (600px width)**:
```
┌─────────────────────────────────────────────────────┐
│  Remove RSVP?                                    [X]│  ← Title (burgundy #880124)
├─────────────────────────────────────────────────────┤
│                                                     │
│  You are about to remove the RSVP for:             │  ← Text (charcoal #2B2B2B)
│  • John Doe                                         │  ← Participant name (bold)
│  • Event: "Shibari Foundations Workshop"           │  ← Event name
│                                                     │
│  ┌─────────────────────────────────────────────┐   │
│  │ ⚠ This action will:                        │   │  ← Warning box (light yellow bg)
│  │ • Remove the RSVP for John Doe             │   │
│  │ • Refund their ticket: $35.00              │   │  ← Only if ticket exists
│  │ • Remove volunteer assignment:             │   │  ← Only if volunteer shift exists
│  │   Safety Monitor (Saturday 6-9 PM)         │   │
│  └─────────────────────────────────────────────┘   │
│                                                     │
│  This action cannot be undone.                     │  ← Warning text (dimmed)
│                                                     │
│  ┌─────────────────────────────────────────────┐   │
│  │ ☐ I understand this will remove the RSVP   │   │  ← Confirmation checkbox
│  │   and cannot be undone                      │   │
│  └─────────────────────────────────────────────┘   │
│                                                     │
│                    [Cancel]  [Remove RSVP]         │  ← Buttons (right-aligned)
│                     Secondary   Danger (red)        │
└─────────────────────────────────────────────────────┘
```

**Mobile Layout (< 768px)**:
```
┌───────────────────────────────┐
│ Remove RSVP?               [X]│
├───────────────────────────────┤
│                               │
│ You are about to remove the   │
│ RSVP for:                     │
│ • John Doe                    │
│ • Event: "Shibari             │
│   Foundations Workshop"       │
│                               │
│ ┌───────────────────────────┐ │
│ │ ⚠ This action will:      │ │
│ │ • Remove the RSVP        │ │
│ │ • Refund ticket: $35.00  │ │
│ │ • Remove volunteer       │ │
│ │   assignment: Safety     │ │
│ │   Monitor (Sat 6-9 PM)   │ │
│ └───────────────────────────┘ │
│                               │
│ This action cannot be undone. │
│                               │
│ ┌───────────────────────────┐ │
│ │ ☐ I understand this will │ │
│ │   remove the RSVP and    │ │
│ │   cannot be undone       │ │
│ └───────────────────────────┘ │
│                               │
│ [Remove RSVP]                 │  ← Primary action on top
│ (Full width, danger color)    │
│                               │
│ [Cancel]                      │  ← Secondary full-width
└───────────────────────────────┘
```

### 2. Ticket Refund Modal

**Trigger**: Admin clicks "Refund" link in Tickets Sold table (Attendees tab)

**Desktop Layout (600px width)**:
```
┌─────────────────────────────────────────────────────┐
│  Refund Ticket?                                  [X]│  ← Title (burgundy #880124)
├─────────────────────────────────────────────────────┤
│                                                     │
│  You are about to refund the ticket for:           │  ← Text (charcoal #2B2B2B)
│  • Jane Smith                                       │  ← Participant name (bold)
│  • Event: "Rope Bondage Performance Night"         │  ← Event name
│                                                     │
│  Refund Amount: $45.00                             │  ← Large, prominent amount
│                                                     │
│  ┌─────────────────────────────────────────────┐   │
│  │ ☑ Also remove RSVP if present              │   │  ← Checkbox (default: checked)
│  └─────────────────────────────────────────────┘   │
│                                                     │
│  ┌─────────────────────────────────────────────┐   │
│  │ ⚠ This action will:                        │   │  ← Warning box (light yellow bg)
│  │ • Refund $45.00 to Jane Smith              │   │
│  │ • Remove their RSVP                        │   │  ← Only if checkbox checked
│  │ • Remove volunteer assignment:             │   │  ← Only if volunteer shift exists
│  │   Setup Crew (Friday 5-7 PM)               │   │
│  └─────────────────────────────────────────────┘   │
│                                                     │
│  This action cannot be undone.                     │  ← Warning text (dimmed)
│                                                     │
│  ┌─────────────────────────────────────────────┐   │
│  │ ☐ I understand this will refund the ticket │   │  ← Confirmation checkbox
│  │   and cannot be undone                      │   │
│  └─────────────────────────────────────────────┘   │
│                                                     │
│                    [Cancel]  [Refund Ticket]       │  ← Buttons (right-aligned)
│                     Secondary   Danger (red)        │
└─────────────────────────────────────────────────────┘
```

**Mobile Layout (< 768px)**:
```
┌───────────────────────────────┐
│ Refund Ticket?             [X]│
├───────────────────────────────┤
│                               │
│ You are about to refund the   │
│ ticket for:                   │
│ • Jane Smith                  │
│ • Event: "Rope Bondage        │
│   Performance Night"          │
│                               │
│ Refund Amount: $45.00         │  ← Prominent amount
│                               │
│ ┌───────────────────────────┐ │
│ │ ☑ Also remove RSVP if    │ │  ← Default checked
│ │   present                 │ │
│ └───────────────────────────┘ │
│                               │
│ ┌───────────────────────────┐ │
│ │ ⚠ This action will:      │ │
│ │ • Refund $45.00          │ │
│ │ • Remove RSVP            │ │
│ │ • Remove volunteer       │ │
│ │   assignment: Setup      │ │
│ │   Crew (Fri 5-7 PM)      │ │
│ └───────────────────────────┘ │
│                               │
│ This action cannot be undone. │
│                               │
│ ┌───────────────────────────┐ │
│ │ ☐ I understand this will │ │
│ │   refund the ticket and  │ │
│ │   cannot be undone       │ │
│ └───────────────────────────┘ │
│                               │
│ [Refund Ticket]               │  ← Primary action on top
│ (Full width, danger color)    │
│                               │
│ [Cancel]                      │  ← Secondary full-width
└───────────────────────────────┘
```

## Component Specifications

### RSVP Removal Modal Props

```typescript
interface RemoveRsvpModalProps {
  opened: boolean;
  onClose: () => void;
  participantName: string;
  participantEmail: string;
  eventTitle: string;
  participationId: string;
  hasTicket: boolean;
  ticketAmount?: number;
  volunteerShifts?: Array<{
    shiftName: string;
    startTime: string;
    endTime: string;
  }>;
  onSuccess?: () => void;
}
```

### Ticket Refund Modal Props

```typescript
interface RefundTicketModalProps {
  opened: boolean;
  onClose: () => void;
  participantName: string;
  participantEmail: string;
  eventTitle: string;
  ticketId: string;
  refundAmount: number;
  hasRsvp: boolean;
  volunteerShifts?: Array<{
    shiftName: string;
    startTime: string;
    endTime: string;
  }>;
  onSuccess?: () => void;
}
```

## Mantine Components Used

| Component | Purpose | Configuration |
|-----------|---------|--------------|
| Modal | Container for confirmation dialogs | `centered`, `size="md"` (600px) |
| Stack | Vertical content layout | `gap="md"` (24px spacing) |
| Title | Modal title | `order={3}`, burgundy color |
| Text | Body text and labels | `size="sm"` for body, `fw={500}` for names |
| Checkbox | Confirmation and option selection | Standard Mantine checkbox |
| Alert | Warning box with impact summary | `color="yellow"`, `icon={<IconAlertTriangle />}` |
| Button | Cancel and confirm actions | Danger variant for destructive actions |
| Group | Button layout | `justify="flex-end"`, `gap="md"` |
| List | Impact items in warning box | Bulleted list with icons |

## Interaction Patterns

### State Management

```typescript
// RSVP Removal Modal
const [confirmed, setConfirmed] = useState(false);
const [isSubmitting, setIsSubmitting] = useState(false);

// Ticket Refund Modal
const [alsoRemoveRsvp, setAlsoRemoveRsvp] = useState(true); // Default: checked
const [confirmed, setConfirmed] = useState(false);
const [isSubmitting, setIsSubmitting] = useState(false);
```

### Button States

**Remove RSVP / Refund Ticket Button**:
- **Disabled** when:
  - Confirmation checkbox not checked (`!confirmed`)
  - Form is submitting (`isSubmitting`)
- **Loading** when: `isSubmitting === true`
- **Color**: Red (danger variant) - destructive action

**Cancel Button**:
- **Disabled** when: `isSubmitting === true` (prevent close during submission)
- **Color**: Light/outline variant

### Form Validation

- **Confirmation checkbox required**: Primary action button disabled until checked
- **No text input required**: Checkbox-based confirmation is sufficient for admin actions
- **Warning box dynamically updates**: Shows impacts based on data (ticket, volunteer shifts)

### API Interaction

```typescript
// RSVP Removal
const handleRemoveRsvp = async () => {
  setIsSubmitting(true);
  try {
    await participationApi.removeRsvp(participationId);

    notifications.show({
      title: 'RSVP Removed',
      message: `${participantName}'s RSVP has been removed`,
      color: 'green'
    });

    onClose();
    onSuccess?.();
  } catch (error: any) {
    notifications.show({
      title: 'Error',
      message: error?.detail || 'Failed to remove RSVP',
      color: 'red'
    });
  } finally {
    setIsSubmitting(false);
  }
};

// Ticket Refund
const handleRefundTicket = async () => {
  setIsSubmitting(true);
  try {
    await ticketApi.refundTicket(ticketId, { alsoRemoveRsvp });

    notifications.show({
      title: 'Ticket Refunded',
      message: `$${refundAmount.toFixed(2)} refunded to ${participantName}`,
      color: 'green'
    });

    onClose();
    onSuccess?.();
  } catch (error: any) {
    notifications.show({
      title: 'Error',
      message: error?.detail || 'Failed to refund ticket',
      color: 'red'
    });
  } finally {
    setIsSubmitting(false);
  }
};
```

## Responsive Breakpoints

- **Desktop (≥769px)**:
  - Modal width: 600px (size="md")
  - Buttons: Horizontal layout, right-aligned
  - Text: Standard sizing

- **Mobile (<768px)**:
  - Modal: Full-width with 20px padding
  - Buttons: Stacked vertically, full-width
  - Primary action button on top
  - Text: Slightly smaller, more line breaks

## Accessibility Requirements

### Keyboard Navigation
- **Tab order**: Title → Warning box → Checkbox options → Confirmation checkbox → Cancel → Confirm
- **Enter/Space**: Toggle checkboxes, activate buttons
- **Escape**: Close modal (only if not submitting)

### Screen Reader Support
```tsx
<Modal
  opened={opened}
  onClose={onClose}
  title={
    <Title order={3} style={{ color: '#880124' }}>
      Remove RSVP?
    </Title>
  }
  centered
  aria-labelledby="remove-rsvp-title"
  aria-describedby="remove-rsvp-description"
>
  <Text id="remove-rsvp-description" size="sm">
    You are about to remove the RSVP for {participantName}...
  </Text>
</Modal>
```

### Color Contrast
- **Warning box**: Yellow background (#FFF9E6) with dark text (#2B2B2B) = 12:1 (AAA)
- **Danger button**: Red (#DC143C) with white text = 5.2:1 (AA)
- **Title**: Burgundy (#880124) = 8.5:1 (AAA)

### Focus Management
- Modal automatically focuses on first interactive element (checkbox)
- Trap focus within modal while open
- Return focus to trigger button on close

## Visual Hierarchy

### Warning Box Design
```css
.warning-box {
  background: #FFF9E6; /* Light yellow */
  border-left: 4px solid #DAA520; /* Warning gold */
  padding: 16px;
  border-radius: 8px;
  margin: 16px 0;
}
```

### Impact Items
- Use bullet points (•) for clarity
- Bold participant names and amounts
- Indent volunteer shift details
- Icon: ⚠ (warning triangle) before "This action will:"

### Typography
- **Title**: Montserrat 700, 20px, burgundy
- **Participant name**: Source Sans 3 600, 16px, charcoal
- **Event name**: Source Sans 3 400, 14px, charcoal (in quotes)
- **Warning text**: Source Sans 3 400, 14px, smoke (#4A4A4A)
- **Amount**: Source Sans 3 700, 18px, charcoal

## Design System Integration

### Colors Used
- **Burgundy** (`#880124`): Modal title, confirmation focus
- **Charcoal** (`#2B2B2B`): Primary text
- **Smoke** (`#4A4A4A`): Secondary text, warnings
- **Warning Gold** (`#DAA520`): Warning box border
- **Error Red** (`#DC143C`): Danger button background
- **Light Yellow** (`#FFF9E6`): Warning box background
- **Ivory** (`#FFF8F0`): Button text on danger button

### Spacing
- **Modal padding**: `var(--space-lg)` (32px)
- **Stack gap**: `var(--space-md)` (24px)
- **Button gap**: `var(--space-md)` (24px)
- **Warning box margin**: `var(--space-md) 0`

### Buttons
- **Cancel**: Secondary button style (outline)
- **Remove RSVP / Refund Ticket**: Danger variant (red background, white text)
- **Size**: Standard (14px font, 14px/32px padding)
- **Corner radius**: 8px (standard Mantine, not asymmetric - modals use standard corners)

## Edge Cases & Conditional Display

### RSVP Removal Modal

**Scenario 1: RSVP Only (No Ticket)**
```
⚠ This action will:
• Remove the RSVP for John Doe
```

**Scenario 2: RSVP with Ticket**
```
⚠ This action will:
• Remove the RSVP for John Doe
• Refund their ticket: $35.00
```

**Scenario 3: RSVP with Ticket and Volunteer Shift**
```
⚠ This action will:
• Remove the RSVP for John Doe
• Refund their ticket: $35.00
• Remove volunteer assignment: Safety Monitor (Saturday 6-9 PM)
```

### Ticket Refund Modal

**Scenario 1: Ticket Only (No RSVP)**
- Checkbox "Also remove RSVP if present" is visible but disabled (grayed out)
- Warning shows: "Refund $45.00 to Jane Smith"

**Scenario 2: Ticket with RSVP**
- Checkbox "Also remove RSVP if present" is enabled and checked by default
- Warning shows: "Refund $45.00" + "Remove RSVP" if checkbox checked

**Scenario 3: Ticket with RSVP and Volunteer Shift**
- Same as Scenario 2, plus:
- Warning shows: "Remove volunteer assignment: ..."

## Quality Checklist
- [x] Meets accessibility standards (WCAG 2.1 AA)
- [x] Responsive on all devices with Mantine breakpoints
- [x] Uses Mantine v7 components consistently
- [x] Follows TypeScript-first patterns
- [x] Uses Mantine theming system
- [x] Follows React best practices (hooks, functional components)
- [x] Clear user flows with appropriate friction
- [x] Safety/consent prominent in language
- [x] Community values reflected (respectful language)
- [x] Performance considered (no unnecessary re-renders)
- [x] Consistent with existing DenyApplicationModal pattern

## Implementation Notes

1. **Reuse Patterns**: Follow `DenyApplicationModal.tsx` structure for consistency
2. **Dynamic Warning Box**: Calculate warning items based on props (hasTicket, volunteerShifts)
3. **Checkbox Confirmation**: Simpler than text input for admin workflows (low error risk)
4. **Notification Feedback**: Success shows participant name and action taken
5. **Error Handling**: Show API error details, allow retry without closing modal
6. **onSuccess Callback**: Refresh participations list in parent component

## Future Enhancements (Not MVP)

- **Partial Refund Amount**: Input field for custom refund amount (currently full amount only)
- **Refund Reason**: Optional textarea for admin notes on refund
- **Participant Notification**: Checkbox to send email to participant about removal/refund
- **Audit Trail Link**: Button to view participation history/audit log

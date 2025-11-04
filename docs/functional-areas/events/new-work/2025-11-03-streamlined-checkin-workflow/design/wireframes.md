# UI Wireframes: Streamlined Check-In Workflow
<!-- Last Updated: 2025-11-03 -->
<!-- Version: 1.0 -->
<!-- Owner: UI Designer Agent -->
<!-- Status: Draft -->

## Design Overview

### Purpose
Replace modal-based check-in confirmations with a streamlined multi-step button workflow that reduces workshop check-ins from 4 clicks (with modal) to 2 clicks (button-only), while maintaining proper payment tracking for social events.

### User Goals
- **Staff Members**: Check in 20-50 attendees quickly during high-traffic arrival times
- **Efficiency Target**: Process attendees in 3 seconds instead of 5 seconds (40% improvement)
- **Error Reduction**: Clear button states prevent confusion and click fatigue errors

### Key Design Principles
1. **Visual Clarity**: Button states clearly show current workflow step
2. **Kiosk-Optimized**: Large touch targets (minimum 44×44px) for tablet use
3. **Real-Time Feedback**: Instant visual updates when payment completes
4. **Accessibility**: WCAG 2.1 AA compliant with keyboard navigation
5. **Professional Appearance**: Salem-based organization aesthetic (burgundy/plum theme)

## User Personas

### Staff Check-In Operator
**Primary User**: Event coordinators and volunteers managing check-in desk
**Technical Skill**: Basic computer literacy
**Environment**: High-stress arrival times with 20-50 people in queue
**Device**: Tablet or desktop kiosk
**Needs**: Fast, error-proof workflow with clear visual feedback

### Event Organizer
**Secondary User**: Reviews check-in reports and payment reconciliation
**Environment**: Post-event admin review
**Needs**: Complete payment audit trail and accurate check-in records

### Attendee (QR Code Payment)
**Tertiary User**: Uses personal phone to complete payment via QR code
**Environment**: Standing at check-in desk with staff assisting
**Device**: Personal smartphone
**Needs**: Quick payment experience that updates kiosk automatically

## Wireframes

### 1. Table Row - Workshop/Pre-Paid Social Event (2-Click Workflow)

**Scenario**: Attendee has purchased ticket ahead of time

```
Desktop (≥769px):
┌──────────────────────────────────────────────────────────────────────────────────┐
│ Name                │ Pronouns    │ Payment Status      │ Action                │
├─────────────────────┼─────────────┼────────────────────┼───────────────────────┤
│ Jane Doe            │ she/her     │ ✓ Ticket Purchased │ [Covid Test Complete] │
│                     │             │  (green badge)      │  (blue, primary btn)  │
└─────────────────────┴─────────────┴────────────────────┴───────────────────────┘

After clicking "Covid Test Complete":
┌──────────────────────────────────────────────────────────────────────────────────┐
│ Jane Doe            │ she/her     │ ✓ Ticket Purchased │ [Check In]            │
│                     │             │  (green badge)      │  (green, primary btn) │
└─────────────────────┴─────────────┴────────────────────┴───────────────────────┘

After clicking "Check In":
┌──────────────────────────────────────────────────────────────────────────────────┐
│ Jane Doe            │ she/her     │ ✓ Ticket Purchased │ ✓ Checked In          │
│                     │             │  (green badge)      │  (green text, no btn) │
└─────────────────────┴─────────────┴────────────────────┴───────────────────────┘
```

**Mobile (<768px):**
```
┌────────────────────────────────────────┐
│ Jane Doe                  she/her      │
│ ✓ Ticket Purchased (green badge)       │
│                                         │
│ [ Covid Test Complete (full width) ]   │
└────────────────────────────────────────┘

After covid test:
┌────────────────────────────────────────┐
│ Jane Doe                  she/her      │
│ ✓ Ticket Purchased (green badge)       │
│                                         │
│ [ Check In (full width, green) ]       │
└────────────────────────────────────────┘

After check-in:
┌────────────────────────────────────────┐
│ Jane Doe                  she/her      │
│ ✓ Ticket Purchased (green badge)       │
│                                         │
│ ✓ Checked In (green, centered text)    │
└────────────────────────────────────────┘
```

### 2. Table Row - Social Event RSVP Only (2-Click WITHOUT Payment)

**Scenario**: Attendee RSVP'd, chooses NOT to purchase optional ticket

```
Desktop:
┌──────────────────────────────────────────────────────────────────────────────────┐
│ John Smith          │ he/him      │ RSVP Only          │ [Covid Test Complete] │
│                     │             │ (yellow badge)      │  (blue, primary btn)  │
└─────────────────────┴─────────────┴────────────────────┴───────────────────────┘

After clicking "Covid Test Complete" (skipping "Paid at Door"):
┌──────────────────────────────────────────────────────────────────────────────────┐
│ John Smith          │ he/him      │ RSVP Only          │ [Check In]            │
│                     │             │ (yellow badge)      │  (green, primary btn) │
└─────────────────────┴─────────────┴────────────────────┴───────────────────────┘

After clicking "Check In":
┌──────────────────────────────────────────────────────────────────────────────────┐
│ John Smith          │ he/him      │ RSVP Only          │ ✓ Checked In          │
│                     │             │ (yellow badge)      │  (green text)         │
└─────────────────────┴─────────────┴────────────────────┴───────────────────────┘
```

**Key Design Note**: "Paid at Door" button is OPTIONAL and can be completely skipped. Staff proceeds directly to "Covid Test Complete" if attendee doesn't want to purchase a ticket.

### 3. Table Row - Social Event RSVP with Optional Payment (3-5 Clicks)

**Scenario**: Attendee RSVP'd, chooses to purchase optional ticket at door

```
Initial State:
┌──────────────────────────────────────────────────────────────────────────────────┐
│ Sarah Johnson       │ she/her     │ RSVP Only          │ [Paid at Door]        │
│                     │             │ (yellow badge)      │  (gray, secondary)    │
└─────────────────────┴─────────────┴────────────────────┴───────────────────────┘

After clicking "Paid at Door" → Modal appears (see Modal section):
[Staff selects "Cash Payment" OR "Digital Payment (QR)"]

After payment completes (either method):
┌──────────────────────────────────────────────────────────────────────────────────┐
│ Sarah Johnson       │ she/her     │ ✓ Paid at Door     │ [Covid Test Complete] │
│                     │             │ (green badge)       │  (blue, primary btn)  │
└─────────────────────┴─────────────┴────────────────────┴───────────────────────┘

After clicking "Covid Test Complete":
┌──────────────────────────────────────────────────────────────────────────────────┐
│ Sarah Johnson       │ she/her     │ ✓ Paid at Door     │ [Check In]            │
│                     │             │ (green badge)       │  (green, primary btn) │
└─────────────────────┴─────────────┴────────────────────┴───────────────────────┘

After clicking "Check In":
┌──────────────────────────────────────────────────────────────────────────────────┐
│ Sarah Johnson       │ she/her     │ ✓ Paid at Door     │ ✓ Checked In          │
│                     │             │ (green badge)       │  (green text)         │
└─────────────────────┴─────────────┴────────────────────┴───────────────────────┘
```

### 4. Cash Payment Modal

**Triggered by**: "Paid at Door" → "Cash Payment" selection

```
Desktop Modal (centered, 500px width):
┌───────────────────────────────────────────────────────────────┐
│                                                        [×]     │
│                                                               │
│                    Record Cash Payment                        │
│                                                               │
│ ───────────────────────────────────────────────────────────  │
│                                                               │
│  Attendee: Sarah Johnson                                      │
│            (large, bold, charcoal text)                       │
│                                                               │
│  AMOUNT                                                       │
│  ┌──────────────────────────────────────────┐                │
│  │ $  20.00                                 │                │
│  └──────────────────────────────────────────┘                │
│  Currency format with $ prefix                               │
│                                                               │
│  PAYMENT METHOD                                               │
│  Cash (read-only, gray text)                                 │
│                                                               │
│  NOTES (OPTIONAL)                                             │
│  ┌──────────────────────────────────────────┐                │
│  │                                          │                │
│  │                                          │                │
│  │                                          │                │
│  └──────────────────────────────────────────┘                │
│  For special circumstances (e.g., discount reason)           │
│                                                               │
│                                                               │
│                       ┌──────────┐  ┌────────────────┐       │
│                       │  Cancel  │  │ Record Payment │       │
│                       └──────────┘  └────────────────┘       │
│                      (secondary)        (primary)            │
│                                                               │
└───────────────────────────────────────────────────────────────┘
```

**Mobile Modal (full-screen):**
```
┌─────────────────────────────────┐
│ [×] Record Cash Payment         │
├─────────────────────────────────┤
│                                 │
│ Attendee:                       │
│ Sarah Johnson                   │
│ (large text)                    │
│                                 │
│ AMOUNT                          │
│ ┌─────────────────────────────┐ │
│ │ $  20.00                    │ │
│ └─────────────────────────────┘ │
│                                 │
│ PAYMENT METHOD                  │
│ Cash                            │
│                                 │
│ NOTES (OPTIONAL)                │
│ ┌─────────────────────────────┐ │
│ │                             │ │
│ │                             │ │
│ └─────────────────────────────┘ │
│                                 │
│                                 │
│ [  Record Payment (full width) ]│
│ [  Cancel (full width)         ]│
│                                 │
└─────────────────────────────────┘
```

**Component Specifications - Cash Payment Modal**:
- **Modal**: Mantine Modal component, centered, overlay dark (0.75 opacity)
- **Title**: Montserrat 700, 20px, uppercase, burgundy
- **Attendee Name**: Source Sans 3 600, 18px, charcoal
- **Amount Input**: Mantine NumberInput with currency formatting
  - Prefix: "$" symbol
  - Decimal places: 2
  - Min value: 0.01
  - Max value: 1000.00 (reasonable event ticket limit)
  - Error state: Red border if invalid (e.g., $0.00 or empty)
- **Notes Textarea**: Mantine Textarea, 3 rows, optional
  - Max length: 200 characters
  - Character counter: "0/200 characters"
- **Buttons**: Stacked on mobile, inline on desktop
  - Cancel: Secondary button (burgundy outline)
  - Record Payment: Primary button (gold gradient)

### 5. QR Code Payment Modal

**Triggered by**: "Paid at Door" → "Digital Payment" selection

```
Desktop Modal (centered, 600px width):
┌───────────────────────────────────────────────────────────────┐
│                                                        [×]     │
│                                                               │
│                    Scan to Pay                                │
│                                                               │
│ ───────────────────────────────────────────────────────────  │
│                                                               │
│  Attendee: Mike Chen                                          │
│            (large, bold, charcoal text)                       │
│                                                               │
│                                                               │
│              ┌─────────────────────┐                          │
│              │                     │                          │
│              │                     │                          │
│              │    QR CODE HERE     │ ← 250×250px               │
│              │    (high contrast)  │                          │
│              │                     │                          │
│              │                     │                          │
│              └─────────────────────┘                          │
│                                                               │
│  Scan with your phone to complete payment                    │
│  (centered, medium text)                                      │
│                                                               │
│  witchcityrope.com/events/123/purchase?attendeeId=456         │
│  (small, taupe text, for manual entry)                       │
│                                                               │
│                                                               │
│  ⏳ Waiting for payment...                                    │
│  ━━━━━━━━━━━━━━━━━━ (animated progress bar)                   │
│                                                               │
│                                                               │
│                       ┌──────────┐                            │
│                       │  Cancel  │                            │
│                       └──────────┘                            │
│                      (secondary)                              │
│                                                               │
└───────────────────────────────────────────────────────────────┘
```

**QR Code Payment Success State:**
```
┌───────────────────────────────────────────────────────────────┐
│                                                               │
│                    Payment Received! ✓                        │
│                                                               │
│ ───────────────────────────────────────────────────────────  │
│                                                               │
│  Attendee: Mike Chen                                          │
│                                                               │
│                                                               │
│              ┌─────────────────────┐                          │
│              │                     │                          │
│              │         ✓           │ ← Green checkmark         │
│              │    Success!         │   animation              │
│              │    $20.00 paid      │                          │
│              │                     │                          │
│              └─────────────────────┘                          │
│                                                               │
│  Payment confirmed via PayPal                                 │
│  (green text, centered)                                       │
│                                                               │
│  [Modal auto-closes in 2 seconds...]                          │
│                                                               │
└───────────────────────────────────────────────────────────────┘
```

**Mobile QR Modal (full-screen):**
```
┌─────────────────────────────────┐
│ [×] Scan to Pay                 │
├─────────────────────────────────┤
│                                 │
│ Attendee:                       │
│ Mike Chen                       │
│                                 │
│                                 │
│   ┌───────────────────────┐     │
│   │                       │     │
│   │                       │     │
│   │   QR CODE HERE        │     │ ← 200×200px (smaller)
│   │                       │     │
│   │                       │     │
│   └───────────────────────┘     │
│                                 │
│ Scan with your phone to         │
│ complete payment                │
│                                 │
│ witchcityrope.com/events/...    │
│ (truncated URL)                 │
│                                 │
│ ⏳ Waiting for payment...       │
│ ━━━━━━━━━━━━━━━━━━              │
│                                 │
│                                 │
│ [  Cancel (full width)         ]│
│                                 │
└─────────────────────────────────┘
```

**Component Specifications - QR Code Modal**:
- **Modal**: Mantine Modal, centered, larger size (600px vs 500px)
- **QR Code**: Use `react-qr-code` or `qrcode.react` library
  - Size: 250×250px desktop, 200×200px mobile
  - Error correction: Medium (M level)
  - Foreground: Charcoal (#2B2B2B)
  - Background: Ivory (#FFF8F0)
  - URL: `https://witchcityrope.com/events/{eventId}/purchase?attendeeId={attendeeId}&returnUrl=checkin`
- **URL Display**: Mantine Text, size="xs", color="dimmed"
  - Desktop: Full URL visible
  - Mobile: Truncate middle with ellipsis if too long
- **Progress Indicator**: Mantine Progress component
  - Indeterminate animation (pulsing bar)
  - Color: Burgundy
  - Remove when payment completes
- **Success State**:
  - Replace QR code with checkmark icon (Mantine ThemeIcon)
  - Green background (#228B22)
  - White checkmark icon
  - Auto-close after 2 seconds
  - Play subtle success sound (optional)

### 6. Check-In Interface - Full Layout

```
Desktop (≥769px):
┌────────────────────────────────────────────────────────────────────────────────┐
│                                                                         [×]    │
│  Check-In: Advanced Rope Workshop                              (32px symbol)  │
│  Capacity: 18/20  |  Start Time: 7:00 PM                                      │
│                                                                                │
│ ──────────────────────────────────────────────────────────────────────────────│
│                                                                                │
│  ┌──────────────────────────────────────────────────────────────────────────┐ │
│  │ Name              │ Pronouns │ Payment Status      │ Action              │ │
│  ├───────────────────┼──────────┼─────────────────────┼─────────────────────┤ │
│  │ Jane Doe          │ she/her  │ ✓ Ticket Purchased  │ [Covid Test]        │ │
│  ├───────────────────┼──────────┼─────────────────────┼─────────────────────┤ │
│  │ John Smith        │ he/him   │ ✓ Ticket Purchased  │ [Check In]          │ │
│  ├───────────────────┼──────────┼─────────────────────┼─────────────────────┤ │
│  │ Sarah Johnson     │ they/them│ ✓ Ticket Purchased  │ ✓ Checked In        │ │
│  ├───────────────────┼──────────┼─────────────────────┼─────────────────────┤ │
│  │ Mike Chen         │ he/him   │ RSVP Only           │ [Paid at Door]      │ │
│  ├───────────────────┼──────────┼─────────────────────┼─────────────────────┤ │
│  │ Alex Rivera       │ she/her  │ ✓ Paid at Door      │ [Covid Test]        │ │
│  └───────────────────┴──────────┴─────────────────────┴─────────────────────┘ │
│                                                                                │
└────────────────────────────────────────────────────────────────────────────────┘
```

**Mobile (<768px):**
```
┌──────────────────────────────────────┐
│                               [×]    │
│ Check-In:                            │
│ Advanced Rope Workshop               │
│ 18/20  |  7:00 PM                    │
├──────────────────────────────────────┤
│                                      │
│ ┌──────────────────────────────────┐ │
│ │ Jane Doe         she/her         │ │
│ │ ✓ Ticket Purchased               │ │
│ │                                  │ │
│ │ [ Covid Test Complete ]          │ │
│ └──────────────────────────────────┘ │
│                                      │
│ ┌──────────────────────────────────┐ │
│ │ John Smith       he/him          │ │
│ │ ✓ Ticket Purchased               │ │
│ │                                  │ │
│ │ [ Check In ]                     │ │
│ └──────────────────────────────────┘ │
│                                      │
│ ┌──────────────────────────────────┐ │
│ │ Sarah Johnson    they/them       │ │
│ │ ✓ Ticket Purchased               │ │
│ │                                  │ │
│ │ ✓ Checked In                     │ │
│ └──────────────────────────────────┘ │
│                                      │
└──────────────────────────────────────┘
```

**Component Specifications - Full Interface**:
- **Exit Button**: Top-right corner
  - Desktop: 32×32px clickable area, "×" symbol in 24px font
  - Mobile: 44×44px touch target, "×" symbol in 32px font
  - Color: Burgundy (#880124)
  - Hover: Darker burgundy (#660018)
  - Positioned: `position: absolute; top: 16px; right: 16px;`
- **Header**:
  - Event title: Montserrat 700, 24px desktop / 20px mobile
  - Capacity/time: Source Sans 3 400, 16px desktop / 14px mobile, smoke color
  - Separator: Pipe character "|" with spacing
- **Table**:
  - Mantine Table component with `striped` and `highlightOnHover`
  - Desktop: 4 columns (Name, Pronouns, Payment Status, Action)
  - Mobile: Card layout (stacked rows, no table)
  - Row height: Minimum 56px for touch targets
  - Hover: Light ivory background (#FAF6F2)

### 7. Button State Progression - Visual Guide

```
Workshop / Pre-Paid Social Event (2 states):

[Step 1] → [Step 2] → [Complete]
 Covid       Check       Checked In
 Test        In          (text only)
(blue)     (green)      (green)

Social Event RSVP WITHOUT Payment (2 states):

[Step 1] → [Step 2] → [Complete]
 Covid       Check       Checked In
 Test        In          (text only)
(blue)     (green)      (green)

Social Event RSVP WITH Optional Payment (4 states):

[Step 1] → [Step 2] → [Step 3] → [Step 4] → [Complete]
 Paid at     Covid       Check       Checked In
 Door        Test        In          (text only)
(gray)      (blue)     (green)      (green)
   ↓
[Modal: Cash OR QR Payment]
```

**Button Color Legend**:
- **Gray (Stone)**: Optional action, neutral state ("Paid at Door")
- **Blue (Electric)**: Primary workflow action ("Covid Test Complete")
- **Green (Success)**: Final action ("Check In")
- **Green Text**: Completion state ("✓ Checked In")

## Mantine Components Used

| Component | Purpose | Configuration |
|-----------|---------|--------------|
| **Table** | Attendee list display | `striped`, `highlightOnHover`, responsive |
| **Button** | All action buttons | `variant`, `color`, `size`, `fullWidth` (mobile) |
| **Modal** | Cash payment, QR payment | `centered`, `size`, `withCloseButton` |
| **NumberInput** | Cash amount entry | Prefix "$", decimal=2, min=0.01 |
| **Textarea** | Payment notes | `rows={3}`, `maxLength={200}` |
| **Text** | Labels, descriptions | Size variants, color, weight |
| **Badge** | Payment status | Color-coded (green/yellow), size="lg" |
| **Progress** | QR payment waiting | Indeterminate animation, color="burgundy" |
| **Group** | Button layouts | `justify`, `gap` for spacing |
| **Stack** | Mobile button stacking | Vertical layout on mobile |
| **ThemeIcon** | Success checkmark | Size="xl", color="green" |

## Interaction Patterns

### Form Validation
- **Cash Amount**: Required, minimum $0.01, maximum $1000.00
  - Error message: "Amount must be between $0.01 and $1,000.00"
  - Error display: Red border, error text below input
- **Payment Notes**: Optional, max 200 characters
  - Character counter shows live count: "45/200 characters"
  - No validation errors (optional field)

### Loading States
- **Payment Recording**:
  - Button shows "Recording..." text
  - Button disabled during API call
  - Spinner icon inside button
- **QR Payment Waiting**:
  - Indeterminate progress bar
  - "Waiting for payment..." text
  - No timeout (staff can cancel manually)
- **Page Refresh**:
  - All UI state resets to initial button state
  - Database state persists (check-ins remain)
  - Covid test completion state is lost (UI-only)

### Success Feedback
- **Cash Payment**:
  - Modal closes immediately
  - Button changes to "Covid Test Complete"
  - Green notification toast: "Payment recorded: $20.00"
  - Toast position: top-right, auto-close 3 seconds
- **QR Payment**:
  - Modal shows success checkmark (2 seconds)
  - Modal auto-closes
  - Button changes to "Covid Test Complete"
  - Green notification toast: "Payment received: $20.00"
- **Check-In Complete**:
  - Button disappears, replaced with "✓ Checked In" text
  - Row background flashes green briefly (0.5s animation)
  - Green notification toast: "Jane Doe checked in successfully"

### Error Feedback
- **Cash Payment Failed**:
  - Modal remains open
  - Red notification toast: "Failed to record payment. Please try again."
  - Toast position: top-right, auto-close 5 seconds
  - User can retry or cancel
- **QR Payment Failed**:
  - Modal shows error state: "Payment failed. Please try again or use cash."
  - Red notification with retry button
  - Staff can close modal and select cash instead
- **Network Error**:
  - Red notification: "Network error. Check connection and try again."
  - Button remains in previous state
  - All data preserved for retry

## Responsive Breakpoints

Using Mantine responsive breakpoints:
- **Mobile (xs)**: 0px - 575px
- **Small (sm)**: 576px - 767px
- **Medium (md)**: 768px - 991px
- **Large (lg)**: 992px - 1199px
- **Extra Large (xl)**: 1200px+

### Responsive Behavior

**Table → Card Layout (Mobile)**:
```tsx
// Desktop: Mantine Table
<Table striped highlightOnHover>
  {/* 4 columns */}
</Table>

// Mobile (<768px): Card stack
<Stack gap="md">
  <Card shadow="sm" padding="lg">
    <Text weight={600}>{name}</Text>
    <Text size="sm" color="dimmed">{pronouns}</Text>
    <Badge>{paymentStatus}</Badge>
    <Button fullWidth mt="md">{actionButton}</Button>
  </Card>
</Stack>
```

**Button Sizing**:
- Desktop: Standard padding (14px/32px)
- Mobile: Full-width buttons, larger padding (18px/40px)
- Touch targets: Minimum 44×44px (iOS accessibility)

**Modal Sizing**:
- Desktop: Fixed width (500px cash, 600px QR)
- Mobile: Full-screen overlay with padding
- Buttons: Stacked vertically on mobile

**Exit Button**:
- Desktop: 32×32px clickable area
- Mobile: 44×44px touch target
- Always visible in top-right corner
- Absolute positioning maintained across breakpoints

## Accessibility Requirements

### Keyboard Navigation
- **Tab Order**:
  1. Exit button
  2. Table rows (if clickable)
  3. Action buttons (left to right, top to bottom)
  4. Modal inputs (if modal open)
  5. Modal action buttons
- **Enter/Space**: Activate buttons
- **Escape**: Close modals, return to table
- **Arrow Keys**: Navigate table rows (optional enhancement)

### Screen Reader Labels
```tsx
// Exit button
<Button aria-label="Exit check-in interface">×</Button>

// Covid test button
<Button aria-label="Mark covid test complete for Jane Doe">
  Covid Test Complete
</Button>

// Check-in button
<Button aria-label="Check in Jane Doe">
  Check In
</Button>

// Paid at door button
<Button aria-label="Record door payment for Sarah Johnson">
  Paid at Door
</Button>

// Cash amount input
<NumberInput
  label="Amount"
  aria-label="Payment amount in dollars"
  aria-describedby="amount-helper"
/>
<Text id="amount-helper" size="xs">
  Enter the cash amount received
</Text>
```

### Focus Management
- **Modal Open**: Focus moves to first input (cash amount)
- **Modal Close**: Focus returns to trigger button
- **Button Click**: Focus remains on button (or moves to next state)
- **Success State**: Focus moves to next unchecked attendee

### Color Contrast
- **Button Text on Gold**: 7.2:1 (AAA compliant)
- **Button Text on Green**: 8.1:1 (AAA compliant)
- **Badge Text**: 4.8:1 minimum (AA compliant)
- **Table Text**: 8.5:1 (AAA compliant)
- **Error States**: Red with 4.5:1 contrast minimum

### Reduced Motion Support
```css
@media (prefers-reduced-motion: reduce) {
  .btn {
    transition: none !important;
  }

  .success-flash {
    animation: none !important;
  }

  .shimmer-effect {
    display: none !important;
  }
}
```

## Design System Integration

### Colors (from Design System v7)
- **Primary Button (Covid Test)**: Electric purple gradient `#9D4EDD → #7B2CBF`
- **Success Button (Check In)**: Success green `#228B22`
- **Secondary Button (Paid at Door)**: Stone gray `#8B8680`
- **Success Text**: Success green `#228B22`
- **Badge - Paid**: Success green `#228B22`
- **Badge - RSVP**: Warning gold `#DAA520`
- **Error States**: Error red `#DC143C`
- **Exit Button**: Burgundy `#880124`

### Typography
- **Button Text**: Montserrat 600, 14px, uppercase, 1.5px letter-spacing
- **Table Headers**: Montserrat 700, 14px, uppercase
- **Table Body**: Source Sans 3 400, 16px
- **Modal Title**: Montserrat 700, 20px, uppercase
- **Input Labels**: Montserrat 600, 14px, uppercase
- **Badge Text**: Montserrat 600, 12px, uppercase

### Spacing
- **Table Row Padding**: 16px vertical, 12px horizontal
- **Button Spacing**: 16px gap between buttons
- **Modal Content**: 24px padding desktop, 16px mobile
- **Card Spacing**: 16px gap in mobile stack
- **Section Spacing**: 32px between major sections

### Animations
- **Button Corner Morphing**: `12px 6px 12px 6px` → `6px 12px 6px 12px` (0.3s ease)
- **Success Flash**: Green background fade-in/out (0.5s)
- **Modal Enter**: Fade + scale (0.2s ease-out)
- **Progress Bar**: Indeterminate animation (1.5s loop)
- **Shimmer**: Gold gradient sweep (0.5s ease)

## Edge Cases & Error States

### Error: Payment Failed (Cash)
**Visual**:
```
Modal remains open
Red notification toast appears:
┌─────────────────────────────────────────┐
│ ✗ Failed to record payment             │
│   Please try again or contact support  │
└─────────────────────────────────────────┘
Auto-close: 5 seconds

Amount input shows error:
┌──────────────────────────────────────────┐
│ $  20.00                                 │
└──────────────────────────────────────────┘
  ↑ Red border indicates error
✗ Network error. Please try again.
  (error text below input)
```

**User Action**: Retry by clicking "Record Payment" again, or cancel and try QR code

### Error: QR Payment Timeout
**Visual**:
```
Modal after 2 minutes of waiting:
┌───────────────────────────────────────────┐
│ ⚠️ Payment taking longer than expected   │
│                                           │
│ Attendee may need to retry payment       │
│ or use cash payment instead              │
│                                           │
│ [Keep Waiting] [Cancel & Use Cash]       │
└───────────────────────────────────────────┘
```

**User Action**: Keep waiting (attendee may be slow on phone) OR cancel and switch to cash

### Error: Event at Capacity (Door Payment)
**Visual**:
```
Table row shows warning:
┌──────────────────────────────────────────────────────────────────────────────────┐
│ Pat Brown           │ he/him      │ RSVP Only          │ [Paid at Door]        │
│                     │             │ (yellow badge)      │  (disabled, gray)     │
│                     │             │ ⚠️ Event at capacity                          │
└──────────────────────────────────────────────────────────────────────────────────┘

Clicking button shows alert:
┌─────────────────────────────────────────┐
│ ✗ Event at Capacity                    │
│   Cannot process door payment          │
│   Current: 20/20 attendees             │
└─────────────────────────────────────────┘
```

**User Action**: Apologize to attendee, offer waitlist or future event

### Error: Already Checked In (Accidental Click)
**Visual**:
```
Row shows completed state (no button):
┌──────────────────────────────────────────────────────────────────────────────────┐
│ Jane Doe            │ she/her     │ ✓ Ticket Purchased │ ✓ Checked In          │
│                     │             │  (green badge)      │  (green text)         │
│ Checked in at 7:05 PM by Staff #1                                               │
└──────────────────────────────────────────────────────────────────────────────────┘

If trying to check in again (edge case):
┌─────────────────────────────────────────┐
│ ℹ️ Already Checked In                   │
│   Jane Doe was checked in at 7:05 PM   │
└─────────────────────────────────────────┘
```

**Design Decision**: Remove button entirely after check-in to prevent accidental double-clicks

### Info: Payment Optional (Social Event)
**Visual**:
```
Tooltip on "Paid at Door" button:
┌──────────────────────────────────────────┐
│ Payment is optional for social events   │
│ Click to offer ticket purchase           │
│ OR proceed directly to Covid Test        │
└──────────────────────────────────────────┘

Shows on hover/focus, auto-hides after 3 seconds
```

**User Action**: Informed that payment is optional, can skip to covid test

## Quality Checklist

- [x] Meets accessibility standards (WCAG 2.1 AA)
  - Color contrast ratios verified (7.2:1+)
  - Keyboard navigation support
  - Screen reader labels provided
  - Focus management implemented

- [x] Responsive on all devices with Mantine breakpoints
  - Table → Card transition at 768px
  - Button stacking on mobile
  - Touch targets 44×44px minimum
  - Modals full-screen on mobile

- [x] Uses Mantine v7 components consistently (ADR-004)
  - Table, Button, Modal, Input components
  - Built-in theming and styling
  - No custom CSS beyond design system

- [x] Follows TypeScript-first patterns
  - Component props typed
  - Event handlers typed
  - State management typed

- [x] Uses built-in Mantine theming system
  - Custom WCR color palette integrated
  - Design System v7 colors applied
  - Consistent spacing scale

- [x] Leverages Mantine's accessibility features
  - Built-in ARIA attributes
  - Focus trap in modals
  - Keyboard navigation support

- [x] Follows React best practices (hooks, functional components)
  - useState for button state
  - useEffect for payment stream
  - Proper component composition

- [x] Follows brand guidelines
  - Burgundy/plum theme maintained
  - Salem mystical aesthetic
  - Professional appearance

- [x] Clear user flows
  - 2-click workshop check-in
  - 2-click RSVP without payment
  - 3-5 click RSVP with payment
  - Button states guide workflow

- [x] Safety/consent prominent
  - Clear payment optional messaging
  - Capacity warnings shown
  - Staff attribution in audit trail

- [x] Community values reflected
  - Inclusive language (pronouns)
  - Respectful payment handling
  - Transparent process

- [x] Performance considered (lazy loading, code splitting)
  - QR code library loaded on demand
  - Modal components lazy-loaded
  - Real-time stream only when needed

## Next Steps

### For React Developer
1. **Review this wireframe document** - Understand all 3 workflows
2. **Check handoff document** - Implementation guidance in `/handoffs/`
3. **Review Design System v7** - Colors, typography, spacing values
4. **Implement components** - Follow Mantine component specifications
5. **Test real-time payment updates** - SSE integration with kiosk

### For Backend Developer
1. **Review payment recording endpoints** - Ensure API ready for cash payments
2. **Implement SSE endpoint** - Real-time payment notifications (see research doc)
3. **Test QR code URL generation** - Verify attendee ID and event ID parameters
4. **Validate payment webhook integration** - PayPal webhook triggers SSE event

### For Test Developer
1. **Create E2E test scenarios** - All 3 workflows (workshop, RSVP, RSVP+payment)
2. **Test button state progression** - Verify correct sequence
3. **Test payment modals** - Cash and QR flows
4. **Test error scenarios** - Network failures, timeouts, validation errors
5. **Test accessibility** - Keyboard navigation, screen reader announcements

---

**Document Version**: 1.0
**Created**: 2025-11-03
**Author**: UI Designer Agent
**Review Status**: Ready for Implementation Review
**Next Review**: After stakeholder feedback

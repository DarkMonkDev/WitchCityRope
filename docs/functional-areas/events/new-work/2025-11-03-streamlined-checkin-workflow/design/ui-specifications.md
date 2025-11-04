# UI Specifications: Streamlined Check-In Workflow
<!-- Last Updated: 2025-11-03 -->
<!-- Version: 1.0 -->
<!-- Owner: UI Designer Agent -->
<!-- Status: Implementation Ready -->

## Overview

This document provides detailed technical specifications for implementing the streamlined check-in workflow. All specifications are based on Mantine v7 components and Design System v7.

## Component Architecture

### Component Hierarchy
```
<CheckInInterface>
  ├─ <CheckInHeader>
  │   ├─ Event title, capacity, time
  │   └─ Exit button (×)
  │
  ├─ <AttendeeTable> (desktop)
  │   ├─ <AttendeeRow>
  │   │   ├─ Name column
  │   │   ├─ Pronouns column
  │   │   ├─ PaymentStatusBadge
  │   │   └─ <CheckInButton> (state-driven)
  │   └─ [Multiple rows...]
  │
  ├─ <AttendeeCardStack> (mobile)
  │   ├─ <AttendeeCard>
  │   │   ├─ Name, pronouns
  │   │   ├─ PaymentStatusBadge
  │   │   └─ <CheckInButton>
  │   └─ [Multiple cards...]
  │
  ├─ <CashPaymentModal>
  │   ├─ Attendee name display
  │   ├─ Amount NumberInput
  │   ├─ Notes Textarea
  │   └─ Action buttons
  │
  └─ <QRPaymentModal>
      ├─ Attendee name display
      ├─ QR code display
      ├─ Payment URL
      ├─ Progress indicator
      └─ Cancel button
```

## Mantine Component Specifications

### 1. CheckInButton Component

**Purpose**: State-driven button that shows different text/color based on workflow step

**Props Interface**:
```typescript
interface CheckInButtonProps {
  attendee: {
    id: string;
    name: string;
    pronouns: string;
    paymentStatus: 'ticket' | 'rsvp' | 'paidAtDoor';
    isCheckedIn: boolean;
  };
  currentState: 'paidAtDoor' | 'covidTest' | 'checkIn' | 'complete';
  onStateChange: (newState: CheckInButtonState) => void;
  onCashPayment: () => void;
  onQRPayment: () => void;
  disabled?: boolean;
}

type CheckInButtonState = 'paidAtDoor' | 'covidTest' | 'checkIn' | 'complete';
```

**Implementation**:
```tsx
import { Button } from '@mantine/core';

export const CheckInButton: React.FC<CheckInButtonProps> = ({
  attendee,
  currentState,
  onStateChange,
  onCashPayment,
  onQRPayment,
  disabled = false
}) => {
  // Complete state - no button, just text
  if (currentState === 'complete') {
    return (
      <Text
        size="md"
        weight={600}
        color="green"
        style={{ display: 'flex', alignItems: 'center', gap: '8px' }}
      >
        <IconCheck size={20} />
        Checked In
      </Text>
    );
  }

  // Paid at Door button - opens payment modal
  if (currentState === 'paidAtDoor') {
    return (
      <Menu shadow="md" width={200}>
        <Menu.Target>
          <Button
            variant="outline"
            color="gray"
            size="md"
            className="btn btn-secondary"
            disabled={disabled}
            aria-label={`Record door payment for ${attendee.name}`}
          >
            Paid at Door
          </Button>
        </Menu.Target>

        <Menu.Dropdown>
          <Menu.Item
            icon={<IconCash size={16} />}
            onClick={onCashPayment}
          >
            Cash Payment
          </Menu.Item>
          <Menu.Item
            icon={<IconQrcode size={16} />}
            onClick={onQRPayment}
          >
            Digital Payment (QR)
          </Menu.Item>
        </Menu.Dropdown>
      </Menu>
    );
  }

  // Covid Test button
  if (currentState === 'covidTest') {
    return (
      <Button
        variant="gradient"
        gradient={{ from: '#9D4EDD', to: '#7B2CBF', deg: 135 }}
        size="md"
        className="btn btn-primary-alt"
        onClick={() => onStateChange('checkIn')}
        disabled={disabled}
        aria-label={`Mark covid test complete for ${attendee.name}`}
      >
        Covid Test Complete
      </Button>
    );
  }

  // Check In button
  if (currentState === 'checkIn') {
    return (
      <Button
        color="green"
        size="md"
        className="btn btn-success"
        onClick={() => onStateChange('complete')}
        disabled={disabled}
        aria-label={`Check in ${attendee.name}`}
      >
        Check In
      </Button>
    );
  }

  return null;
};
```

**Mantine Components Used**:
- `Button`: Core button component
- `Menu`: Dropdown for payment options
- `Text`: Completion state display
- `IconCheck`, `IconCash`, `IconQrcode`: Tabler icons

**Styling**:
- **Paid at Door**: Gray outline button (secondary)
- **Covid Test**: Electric purple gradient (primary alt)
- **Check In**: Green solid (success)
- **Complete**: Green text with checkmark icon

**State Management**:
```typescript
// React state in parent component
const [buttonState, setButtonState] = useState<CheckInButtonState>(() => {
  if (attendee.isCheckedIn) return 'complete';
  if (attendee.paymentStatus === 'rsvp') return 'paidAtDoor'; // Optional
  return 'covidTest'; // Default for paid attendees
});

// State transitions
const handleStateChange = (newState: CheckInButtonState) => {
  setButtonState(newState);

  // Persist to database only for final check-in
  if (newState === 'complete') {
    checkInAttendee(attendee.id);
  }
};
```

### 2. PaymentStatusBadge Component

**Purpose**: Color-coded badge showing payment status

**Props Interface**:
```typescript
interface PaymentStatusBadgeProps {
  status: 'ticket' | 'rsvp' | 'paidAtDoor';
  size?: 'sm' | 'md' | 'lg';
}
```

**Implementation**:
```tsx
import { Badge } from '@mantine/core';

export const PaymentStatusBadge: React.FC<PaymentStatusBadgeProps> = ({
  status,
  size = 'md'
}) => {
  const badgeConfig = {
    ticket: {
      label: '✓ Ticket Purchased',
      color: 'green',
    },
    rsvp: {
      label: 'RSVP Only',
      color: 'yellow',
    },
    paidAtDoor: {
      label: '✓ Paid at Door',
      color: 'green',
    },
  };

  const config = badgeConfig[status];

  return (
    <Badge
      color={config.color}
      size={size}
      variant="filled"
      style={{
        textTransform: 'uppercase',
        fontFamily: 'Montserrat',
        fontWeight: 600,
        letterSpacing: '0.5px',
      }}
    >
      {config.label}
    </Badge>
  );
};
```

**Mantine Components Used**:
- `Badge`: Mantine badge component

**Color Mapping**:
- **Green**: `#228B22` (success) - Ticket purchased or paid at door
- **Yellow**: `#DAA520` (warning) - RSVP only (payment optional)

### 3. CashPaymentModal Component

**Purpose**: Modal for recording cash payments at the door

**Props Interface**:
```typescript
interface CashPaymentModalProps {
  opened: boolean;
  onClose: () => void;
  attendee: {
    id: string;
    name: string;
  };
  onSubmit: (data: CashPaymentData) => Promise<void>;
}

interface CashPaymentData {
  amount: number;
  notes?: string;
}
```

**Implementation**:
```tsx
import { Modal, NumberInput, Textarea, Button, Text, Group, Stack } from '@mantine/core';
import { useForm } from '@mantine/form';

export const CashPaymentModal: React.FC<CashPaymentModalProps> = ({
  opened,
  onClose,
  attendee,
  onSubmit
}) => {
  const form = useForm<CashPaymentData>({
    initialValues: {
      amount: 0,
      notes: '',
    },
    validate: {
      amount: (value) => {
        if (value < 0.01) return 'Amount must be at least $0.01';
        if (value > 1000) return 'Amount cannot exceed $1,000.00';
        return null;
      },
    },
  });

  const handleSubmit = async (values: CashPaymentData) => {
    try {
      await onSubmit(values);
      form.reset();
      onClose();
      showNotification({
        title: 'Payment Recorded',
        message: `$${values.amount.toFixed(2)} cash payment recorded`,
        color: 'green',
        icon: <IconCheck />,
      });
    } catch (error) {
      showNotification({
        title: 'Payment Failed',
        message: 'Failed to record payment. Please try again.',
        color: 'red',
        icon: <IconX />,
        autoClose: 5000,
      });
    }
  };

  return (
    <Modal
      opened={opened}
      onClose={onClose}
      title="Record Cash Payment"
      centered
      size="md"
      styles={{
        title: {
          fontFamily: 'Montserrat',
          fontWeight: 700,
          fontSize: '20px',
          textTransform: 'uppercase',
          color: '#880124', // burgundy
        },
      }}
    >
      <form onSubmit={form.onSubmit(handleSubmit)}>
        <Stack spacing="md">
          {/* Attendee Name Display */}
          <Text
            size="lg"
            weight={600}
            color="charcoal"
            style={{ marginBottom: '16px' }}
          >
            Attendee: {attendee.name}
          </Text>

          {/* Amount Input */}
          <NumberInput
            label="Amount"
            placeholder="0.00"
            prefix="$"
            precision={2}
            min={0.01}
            max={1000}
            required
            hideControls
            size="md"
            styles={{
              label: {
                fontFamily: 'Montserrat',
                fontWeight: 600,
                fontSize: '14px',
                textTransform: 'uppercase',
                color: '#4A4A4A', // smoke
                marginBottom: '8px',
              },
            }}
            {...form.getInputProps('amount')}
            aria-label="Payment amount in dollars"
            aria-describedby="amount-helper"
          />
          <Text id="amount-helper" size="xs" color="dimmed">
            Enter the cash amount received
          </Text>

          {/* Payment Method (Read-Only) */}
          <Text size="sm" color="dimmed">
            <Text weight={600} component="span">Payment Method:</Text> Cash
          </Text>

          {/* Notes Textarea */}
          <Textarea
            label="Notes (Optional)"
            placeholder="For special circumstances (e.g., discount reason)"
            minRows={3}
            maxRows={5}
            maxLength={200}
            styles={{
              label: {
                fontFamily: 'Montserrat',
                fontWeight: 600,
                fontSize: '14px',
                textTransform: 'uppercase',
                color: '#4A4A4A',
                marginBottom: '8px',
              },
            }}
            {...form.getInputProps('notes')}
          />
          <Text size="xs" color="dimmed" align="right">
            {form.values.notes.length}/200 characters
          </Text>

          {/* Action Buttons */}
          <Group
            position="right"
            mt="md"
            spacing="sm"
            style={{
              flexDirection: 'row',
            }}
            breakpoint="sm"
          >
            <Button
              variant="outline"
              color="burgundy"
              className="btn btn-secondary"
              onClick={onClose}
            >
              Cancel
            </Button>
            <Button
              type="submit"
              variant="gradient"
              gradient={{ from: '#FFBF00', to: '#FF8C00', deg: 135 }}
              className="btn btn-primary"
            >
              Record Payment
            </Button>
          </Group>
        </Stack>
      </form>
    </Modal>
  );
};
```

**Mantine Components Used**:
- `Modal`: Container
- `NumberInput`: Cash amount with currency formatting
- `Textarea`: Optional notes
- `Button`: Cancel and submit actions
- `Text`: Labels and descriptions
- `Group`: Button layout (horizontal desktop)
- `Stack`: Vertical form layout

**Responsive Behavior**:
```css
/* Mobile (<768px) */
@media (max-width: 767px) {
  .cash-payment-modal .mantine-Group-root {
    flex-direction: column-reverse !important;
  }

  .cash-payment-modal .mantine-Button-root {
    width: 100% !important;
  }
}
```

### 4. QRPaymentModal Component

**Purpose**: Display QR code for digital payment and show real-time payment status

**Props Interface**:
```typescript
interface QRPaymentModalProps {
  opened: boolean;
  onClose: () => void;
  attendee: {
    id: string;
    name: string;
  };
  eventId: string;
  onPaymentComplete: () => void;
}
```

**Implementation**:
```tsx
import { Modal, Text, Stack, Button, Progress, ThemeIcon } from '@mantine/core';
import { IconCheck } from '@tabler/icons-react';
import { QRCodeSVG } from 'qrcode.react';
import { useKioskPaymentStream } from '@/hooks/useKioskPaymentStream';

export const QRPaymentModal: React.FC<QRPaymentModalProps> = ({
  opened,
  onClose,
  attendee,
  eventId,
  onPaymentComplete
}) => {
  const [paymentReceived, setPaymentReceived] = useState(false);

  // Real-time payment stream (SSE)
  const { lastPayment } = useKioskPaymentStream(kioskSessionId);

  // Watch for payment completion
  useEffect(() => {
    if (lastPayment?.attendeeId === attendee.id) {
      setPaymentReceived(true);
      setTimeout(() => {
        onPaymentComplete();
        onClose();
      }, 2000); // Show success for 2 seconds
    }
  }, [lastPayment]);

  // Generate QR code URL
  const paymentUrl = `https://witchcityrope.com/events/${eventId}/purchase?attendeeId=${attendee.id}&returnUrl=checkin`;

  return (
    <Modal
      opened={opened}
      onClose={onClose}
      title={paymentReceived ? 'Payment Received!' : 'Scan to Pay'}
      centered
      size="lg"
      withCloseButton={!paymentReceived}
      closeOnClickOutside={false}
      styles={{
        title: {
          fontFamily: 'Montserrat',
          fontWeight: 700,
          fontSize: '20px',
          textTransform: 'uppercase',
          color: paymentReceived ? '#228B22' : '#880124',
        },
      }}
    >
      <Stack spacing="lg" align="center">
        {/* Attendee Name */}
        <Text size="lg" weight={600} color="charcoal">
          Attendee: {attendee.name}
        </Text>

        {/* QR Code or Success Icon */}
        {paymentReceived ? (
          <ThemeIcon
            size={250}
            radius="50%"
            color="green"
            style={{ animation: 'checkmark-pop 0.5s ease-out' }}
          >
            <IconCheck size={150} stroke={3} />
          </ThemeIcon>
        ) : (
          <QRCodeSVG
            value={paymentUrl}
            size={250}
            bgColor="#FFF8F0" // ivory
            fgColor="#2B2B2B" // charcoal
            level="M" // Medium error correction
            includeMargin
            style={{
              padding: '16px',
              background: 'white',
              borderRadius: '8px',
              boxShadow: '0 4px 12px rgba(0,0,0,0.1)',
            }}
          />
        )}

        {/* Instructions or Success Message */}
        {paymentReceived ? (
          <Text size="md" color="green" weight={600} align="center">
            Payment confirmed via PayPal
          </Text>
        ) : (
          <>
            <Text size="md" align="center" color="charcoal">
              Scan with your phone to complete payment
            </Text>

            <Text size="xs" color="dimmed" align="center" style={{ wordBreak: 'break-all' }}>
              {paymentUrl}
            </Text>

            {/* Progress Indicator */}
            <Stack spacing="xs" style={{ width: '100%' }}>
              <Text size="sm" color="dimmed" align="center">
                ⏳ Waiting for payment...
              </Text>
              <Progress
                value={100}
                animate
                color="burgundy"
                size="sm"
                style={{ width: '100%' }}
              />
            </Stack>
          </>
        )}

        {/* Cancel Button (only when waiting) */}
        {!paymentReceived && (
          <Button
            variant="outline"
            color="burgundy"
            className="btn btn-secondary"
            onClick={onClose}
            mt="md"
          >
            Cancel
          </Button>
        )}
      </Stack>
    </Modal>
  );
};

// CSS for success animation
const styles = `
@keyframes checkmark-pop {
  0% {
    transform: scale(0);
    opacity: 0;
  }
  50% {
    transform: scale(1.2);
  }
  100% {
    transform: scale(1);
    opacity: 1;
  }
}
`;
```

**Mantine Components Used**:
- `Modal`: Container
- `QRCodeSVG`: Third-party QR code library (qrcode.react)
- `Progress`: Waiting indicator
- `ThemeIcon`: Success checkmark
- `Text`: Instructions and status
- `Button`: Cancel action
- `Stack`: Vertical layout

**QR Code Library**:
```bash
npm install qrcode.react
```

**Configuration**:
- Size: 250×250px (desktop and mobile)
- Background: Ivory (#FFF8F0)
- Foreground: Charcoal (#2B2B2B)
- Error correction: Medium (M)
- Include margin: Yes (better scanning)

### 5. AttendeeTable Component (Desktop)

**Purpose**: Desktop table view of all attendees

**Props Interface**:
```typescript
interface AttendeeTableProps {
  attendees: Attendee[];
  onCheckIn: (attendeeId: string) => Promise<void>;
}

interface Attendee {
  id: string;
  name: string;
  pronouns: string;
  paymentStatus: 'ticket' | 'rsvp' | 'paidAtDoor';
  isCheckedIn: boolean;
}
```

**Implementation**:
```tsx
import { Table } from '@mantine/core';

export const AttendeeTable: React.FC<AttendeeTableProps> = ({
  attendees,
  onCheckIn
}) => {
  return (
    <Table
      striped
      highlightOnHover
      verticalSpacing="md"
      styles={{
        root: {
          borderRadius: '8px',
          overflow: 'hidden',
        },
        thead: {
          backgroundColor: '#F5F5F5',
        },
        th: {
          fontFamily: 'Montserrat',
          fontWeight: 700,
          fontSize: '14px',
          textTransform: 'uppercase',
          color: '#2B2B2B',
          letterSpacing: '0.5px',
        },
        td: {
          fontFamily: 'Source Sans 3',
          fontSize: '16px',
          color: '#2B2B2B',
        },
      }}
    >
      <thead>
        <tr>
          <th>Name</th>
          <th>Pronouns</th>
          <th>Payment Status</th>
          <th>Action</th>
        </tr>
      </thead>
      <tbody>
        {attendees.map((attendee) => (
          <tr key={attendee.id}>
            <td>{attendee.name}</td>
            <td>{attendee.pronouns}</td>
            <td>
              <PaymentStatusBadge status={attendee.paymentStatus} />
            </td>
            <td>
              <CheckInButton
                attendee={attendee}
                onCheckIn={onCheckIn}
              />
            </td>
          </tr>
        ))}
      </tbody>
    </Table>
  );
};
```

**Mantine Components Used**:
- `Table`: Mantine table component
- `PaymentStatusBadge`: Custom badge component
- `CheckInButton`: Custom button component

**Responsive Hide**:
```css
@media (max-width: 767px) {
  .attendee-table {
    display: none;
  }
}
```

### 6. AttendeeCardStack Component (Mobile)

**Purpose**: Mobile card view of all attendees

**Implementation**:
```tsx
import { Stack, Card, Text, Group } from '@mantine/core';

export const AttendeeCardStack: React.FC<AttendeeTableProps> = ({
  attendees,
  onCheckIn
}) => {
  return (
    <Stack spacing="md">
      {attendees.map((attendee) => (
        <Card
          key={attendee.id}
          shadow="sm"
          padding="lg"
          radius="md"
          withBorder
          styles={{
            root: {
              borderColor: '#B8B0A8', // taupe
            },
          }}
        >
          <Stack spacing="sm">
            {/* Name and Pronouns */}
            <Group position="apart">
              <Text weight={600} size="lg">
                {attendee.name}
              </Text>
              <Text size="sm" color="dimmed">
                {attendee.pronouns}
              </Text>
            </Group>

            {/* Payment Status Badge */}
            <PaymentStatusBadge status={attendee.paymentStatus} size="lg" />

            {/* Check-In Button (Full Width) */}
            <CheckInButton
              attendee={attendee}
              onCheckIn={onCheckIn}
              fullWidth
            />
          </Stack>
        </Card>
      ))}
    </Stack>
  );
};
```

**Mantine Components Used**:
- `Stack`: Vertical card layout
- `Card`: Individual attendee card
- `Text`: Name and pronouns
- `Group`: Name/pronoun horizontal layout

**Responsive Show**:
```css
@media (min-width: 768px) {
  .attendee-card-stack {
    display: none;
  }
}
```

## State Management

### Component State (React)
```typescript
// Local UI state (not persisted)
const [covidTestComplete, setCovidTestComplete] = useState<Record<string, boolean>>({});

// Button state per attendee
const getButtonState = (attendee: Attendee): CheckInButtonState => {
  if (attendee.isCheckedIn) return 'complete';

  // Check local state for covid test completion
  if (covidTestComplete[attendee.id]) return 'checkIn';

  // Check if payment needed
  if (attendee.paymentStatus === 'rsvp') {
    // Optional: can skip to covidTest if payment not wanted
    return 'paidAtDoor'; // Show payment option first
  }

  return 'covidTest'; // Default for paid attendees
};

// Handle state transitions
const handleStateChange = (attendeeId: string, newState: CheckInButtonState) => {
  if (newState === 'checkIn') {
    setCovidTestComplete({ ...covidTestComplete, [attendeeId]: true });
  }

  if (newState === 'complete') {
    checkInAttendee(attendeeId);
  }
};
```

### API Integration
```typescript
// Check-in attendee (persists to database)
const checkInAttendee = async (attendeeId: string) => {
  try {
    await fetch(`/api/events/${eventId}/checkin`, {
      method: 'POST',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ attendeeId })
    });

    showNotification({
      title: 'Success',
      message: `${attendee.name} checked in successfully`,
      color: 'green',
      icon: <IconCheck />,
    });
  } catch (error) {
    showNotification({
      title: 'Error',
      message: 'Failed to check in. Please try again.',
      color: 'red',
      icon: <IconX />,
      autoClose: 5000,
    });
  }
};

// Record cash payment
const recordCashPayment = async (data: CashPaymentData) => {
  await fetch(`/api/events/${eventId}/payments/cash`, {
    method: 'POST',
    credentials: 'include',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      attendeeId,
      amount: data.amount,
      notes: data.notes,
      paymentMethod: 'Cash'
    })
  });
};
```

### Real-Time Updates (SSE)
```typescript
// Hook for real-time payment notifications
import { useEffect, useState } from 'react';

export const useKioskPaymentStream = (sessionId: string) => {
  const [lastPayment, setLastPayment] = useState<PaymentEvent | null>(null);
  const [status, setStatus] = useState<'connecting' | 'connected' | 'error'>('connecting');

  useEffect(() => {
    const eventSource = new EventSource(
      `/api/kiosk/payment-stream/${sessionId}`
    );

    eventSource.addEventListener('paymentComplete', (event) => {
      const payment = JSON.parse(event.data) as PaymentEvent;
      setLastPayment(payment);
      setStatus('connected');
    });

    eventSource.onerror = () => {
      setStatus('error');
    };

    eventSource.onopen = () => {
      setStatus('connected');
    };

    return () => eventSource.close();
  }, [sessionId]);

  return { lastPayment, status };
};

interface PaymentEvent {
  paymentId: string;
  attendeeId: string;
  amount: number;
  timestamp: string;
}
```

## Styling Specifications

### Design Tokens (CSS Variables)
```css
:root {
  /* Colors */
  --color-burgundy: #880124;
  --color-burgundy-dark: #660018;
  --color-electric: #9D4EDD;
  --color-electric-dark: #7B2CBF;
  --color-amber: #FFBF00;
  --color-amber-dark: #FF8C00;
  --color-success: #228B22;
  --color-warning: #DAA520;
  --color-error: #DC143C;
  --color-charcoal: #2B2B2B;
  --color-smoke: #4A4A4A;
  --color-stone: #8B8680;
  --color-taupe: #B8B0A8;
  --color-ivory: #FFF8F0;
  --color-cream: #FAF6F2;

  /* Typography */
  --font-heading: 'Montserrat', sans-serif;
  --font-body: 'Source Sans 3', sans-serif;

  /* Spacing */
  --space-xs: 8px;
  --space-sm: 16px;
  --space-md: 24px;
  --space-lg: 32px;
  --space-xl: 40px;

  /* Borders */
  --border-radius-sm: 6px;
  --border-radius-md: 12px;
  --border-radius-lg: 16px;

  /* Shadows */
  --shadow-sm: 0 2px 8px rgba(0,0,0,0.08);
  --shadow-md: 0 4px 12px rgba(0,0,0,0.1);
  --shadow-lg: 0 8px 24px rgba(0,0,0,0.12);
}
```

### Button Styles (CSS Classes)
```css
/* Base button - asymmetric corners */
.btn {
  border-radius: 12px 6px 12px 6px;
  font-family: var(--font-heading);
  font-weight: 600;
  font-size: 14px;
  text-transform: uppercase;
  letter-spacing: 1.5px;
  transition: all 0.3s ease;
  padding: 14px 32px;
}

.btn:hover {
  border-radius: 6px 12px 6px 12px; /* Corner morph */
}

/* Primary Alt (Electric Purple) - Covid Test button */
.btn-primary-alt {
  background: linear-gradient(135deg, #9D4EDD 0%, #7B2CBF 100%);
  color: #FFF8F0;
  box-shadow: 0 4px 15px rgba(157, 78, 221, 0.4);
}

.btn-primary-alt:hover {
  background: linear-gradient(135deg, #7B2CBF 0%, #9D4EDD 100%);
  box-shadow: 0 6px 20px rgba(157, 78, 221, 0.5);
}

/* Success (Green) - Check In button */
.btn-success {
  background: #228B22;
  color: #FFF8F0;
  box-shadow: 0 4px 15px rgba(34, 139, 34, 0.4);
}

.btn-success:hover {
  background: #1E7A1E;
  box-shadow: 0 6px 20px rgba(34, 139, 34, 0.5);
}

/* Secondary (Burgundy Outline) - Paid at Door, Cancel */
.btn-secondary {
  background: transparent;
  border: 2px solid var(--color-burgundy);
  color: var(--color-burgundy);
}

.btn-secondary:hover {
  background: var(--color-burgundy);
  color: var(--color-ivory);
}

/* Primary (Gold Gradient) - Record Payment */
.btn-primary {
  background: linear-gradient(135deg, #FFBF00 0%, #FF8C00 100%);
  color: #1A1A2E;
  box-shadow: 0 4px 15px rgba(255, 191, 0, 0.4);
}

.btn-primary:hover {
  background: linear-gradient(135deg, #FF8C00 0%, #FFBF00 100%);
  box-shadow: 0 6px 20px rgba(255, 191, 0, 0.5);
}
```

### Responsive Utilities
```css
/* Show/hide based on breakpoint */
@media (max-width: 767px) {
  .desktop-only {
    display: none !important;
  }

  .mobile-full-width {
    width: 100% !important;
  }

  .mobile-stack {
    flex-direction: column !important;
  }
}

@media (min-width: 768px) {
  .mobile-only {
    display: none !important;
  }
}
```

## Accessibility Implementation

### ARIA Labels
```tsx
// Button labels
<Button aria-label="Exit check-in interface">×</Button>
<Button aria-label={`Mark covid test complete for ${name}`}>Covid Test Complete</Button>
<Button aria-label={`Check in ${name}`}>Check In</Button>

// Input labels
<NumberInput
  label="Amount"
  aria-label="Payment amount in dollars"
  aria-describedby="amount-helper"
/>
<Text id="amount-helper" size="xs">Enter the cash amount received</Text>
```

### Keyboard Navigation
```tsx
// Focus management
const modalRef = useRef<HTMLDivElement>(null);

useEffect(() => {
  if (opened) {
    // Move focus to first input when modal opens
    const firstInput = modalRef.current?.querySelector('input');
    firstInput?.focus();
  }
}, [opened]);

// Return focus to trigger button on close
const handleClose = () => {
  onClose();
  triggerButtonRef.current?.focus();
};
```

### Focus Indicators
```css
/* Visible focus ring */
.btn:focus-visible {
  outline: 2px solid var(--color-burgundy);
  outline-offset: 2px;
}

/* Input focus */
.mantine-Input-input:focus {
  border-color: var(--color-burgundy);
  box-shadow: 0 0 0 2px rgba(136, 1, 36, 0.1);
}
```

## Performance Optimization

### Code Splitting
```tsx
// Lazy load modals
const CashPaymentModal = lazy(() => import('./CashPaymentModal'));
const QRPaymentModal = lazy(() => import('./QRPaymentModal'));

// Lazy load QR code library
const QRCode = lazy(() => import('qrcode.react').then(m => ({ default: m.QRCodeSVG })));
```

### Memoization
```tsx
// Memoize expensive computations
const buttonState = useMemo(
  () => getButtonState(attendee),
  [attendee.isCheckedIn, attendee.paymentStatus, covidTestComplete]
);

// Memoize callbacks
const handleCheckIn = useCallback(
  (attendeeId: string) => {
    checkInAttendee(attendeeId);
  },
  [eventId]
);
```

### Virtual Scrolling (Future Enhancement)
```tsx
// For events with 100+ attendees
import { VirtualList } from '@mantine/core';

<VirtualList
  height={600}
  itemHeight={72}
  itemCount={attendees.length}
  renderItem={({ index }) => (
    <AttendeeCard attendee={attendees[index]} />
  )}
/>
```

## Testing Specifications

### Component Tests
```tsx
// CheckInButton.test.tsx
describe('CheckInButton', () => {
  it('shows "Covid Test Complete" for paid attendees', () => {
    render(<CheckInButton currentState="covidTest" {...props} />);
    expect(screen.getByText('Covid Test Complete')).toBeInTheDocument();
  });

  it('transitions to "Check In" after covid test click', async () => {
    const onStateChange = jest.fn();
    render(<CheckInButton currentState="covidTest" onStateChange={onStateChange} {...props} />);

    await userEvent.click(screen.getByText('Covid Test Complete'));
    expect(onStateChange).toHaveBeenCalledWith('checkIn');
  });

  it('shows completion text after final check-in', () => {
    render(<CheckInButton currentState="complete" {...props} />);
    expect(screen.getByText('✓ Checked In')).toBeInTheDocument();
    expect(screen.queryByRole('button')).not.toBeInTheDocument();
  });
});
```

### Accessibility Tests
```tsx
// Accessibility.test.tsx
import { axe } from 'jest-axe';

it('has no accessibility violations', async () => {
  const { container } = render(<CheckInInterface {...props} />);
  const results = await axe(container);
  expect(results).toHaveNoViolations();
});

it('supports keyboard navigation', async () => {
  render(<CheckInInterface {...props} />);

  // Tab to first button
  await userEvent.tab();
  expect(screen.getByText('Covid Test Complete')).toHaveFocus();

  // Enter activates button
  await userEvent.keyboard('{Enter}');
  expect(screen.getByText('Check In')).toBeInTheDocument();
});
```

## File Structure
```
/apps/web/src/features/checkin/
├── components/
│   ├── CheckInInterface.tsx
│   ├── CheckInButton.tsx
│   ├── PaymentStatusBadge.tsx
│   ├── CashPaymentModal.tsx
│   ├── QRPaymentModal.tsx
│   ├── AttendeeTable.tsx
│   └── AttendeeCardStack.tsx
├── hooks/
│   ├── useKioskPaymentStream.ts
│   └── useCheckInState.ts
├── types/
│   └── checkin.types.ts
└── styles/
    └── checkin.module.css
```

## Next Steps for React Developer

1. **Setup**:
   - Install dependencies: `npm install qrcode.react @tabler/icons-react`
   - Create component folder structure
   - Import Mantine components

2. **Implementation Order**:
   - PaymentStatusBadge (simplest)
   - CheckInButton (core component)
   - CashPaymentModal
   - QRPaymentModal (requires SSE hook)
   - AttendeeTable/CardStack (composition)
   - CheckInInterface (orchestration)

3. **Integration**:
   - Connect to SSE endpoint (backend developer provides URL)
   - Wire up API calls for check-in and payments
   - Test real-time payment notifications

4. **Testing**:
   - Unit tests for each component
   - Integration tests for workflows
   - E2E tests with Playwright

---

**Document Version**: 1.0
**Created**: 2025-11-03
**Author**: UI Designer Agent
**Implementation Readiness**: ✅ Complete

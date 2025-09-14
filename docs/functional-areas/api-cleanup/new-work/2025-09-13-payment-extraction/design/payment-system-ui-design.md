# Payment System UI Design: Sliding Scale & Accessible Payment Flows
<!-- Last Updated: 2025-09-13 -->
<!-- Version: 1.0 -->
<!-- Owner: UI Designer Agent -->
<!-- Status: Draft -->

## Design Overview

The Payment System UI is designed to embody WitchCityRope's core values of economic inclusivity through dignified sliding scale pricing while maintaining security and accessibility. The design emphasizes privacy, respect, and community values throughout the payment experience.

## User Personas

- **Community Member (Any Role)**: Primary user paying for events using sliding scale
- **Returning Member**: Using saved payment methods for convenience
- **Admin**: Managing payments, processing refunds, viewing payment reports
- **Teacher/Organizer**: Viewing payment status for their events
- **Member with Financial Constraints**: Using sliding scale without shame or judgment

## Component Hierarchy & Architecture

### Core Payment Components

```typescript
// Payment flow components
PaymentFlowContainer
├── EventSummaryCard
├── SlidingScaleSelector
├── PaymentMethodForm
├── PaymentSummaryCard
├── ProcessingLoader
└── ConfirmationScreen

// Admin components  
AdminPaymentDashboard
├── PaymentStatsCards
├── PaymentTable
├── RefundModal
└── PaymentReportExport
```

## Wireframes & User Flows

### 1. Event Registration Payment Flow

#### Step 1: Event Summary & Sliding Scale Selection

```
+----------------------------------------------------------+
|  🏺 WitchCityRope Payment                               |
+----------------------------------------------------------+
|                                                          |
|  📅 Shibari Fundamentals Workshop                       |
|  🗓️ Saturday, October 15, 2025 • 2:00 PM - 6:00 PM    |
|  👨‍🏫 Instructor: Master Kenji                           |
|  📍 Salem Community Center                               |
|                                                          |
+----------------------------------------------------------+
|  💰 Pricing Options                                     |
|                                                          |
|  Standard Event Fee: $40.00                             |
|                                                          |
|  ┌─ Sliding Scale Options ─────────────────────────────┐ |
|  │                                                     │ |
|  │ ○ Full Price ($40.00)                              │ |
|  │   Support our community at standard rates          │ |
|  │                                                     │ |
|  │ ○ Sliding Scale                                     │ |
|  │   Pay what works within your means                  │ |
|  │   No verification or documentation required         │ |
|  │                                                     │ |
|  │   [Sliding Scale Amount Selector - Selected]       │ |
|  │   ├─────●──────────────────────────┤               │ |
|  │   $10      $25 (Selected)      $40                 │ |
|  │                                                     │ |
|  │   💜 "Our community thrives when everyone can       │ |
|  │      participate. Choose the amount that works      │ |
|  │      for your situation - no questions asked."      │ |
|  │                                                     │ |
|  └─────────────────────────────────────────────────────┘ |
|                                                          |
|  Selected Amount: $25.00                                |
|  Savings Applied: $15.00 (38% sliding scale)            |
|                                                          |
|                    [Continue to Payment]                |
|                                                          |
+----------------------------------------------------------+
```

#### Mobile View (375px)

```
+----------------------+
| 🏺 WCR Payment      |
+----------------------+
|                      |
| 📅 Shibari Fund.    |
| Oct 15 • 2-6PM       |
| Master Kenji         |
|                      |
+----------------------+
| 💰 Pricing          |
|                      |
| Standard: $40.00     |
|                      |
| ○ Full Price         |
|                      |
| ● Sliding Scale      |
| ┌──────────────────┐ |
| │  Pay Your Amount │ |
| │ ├───●────────────┤│ |
| │ $10  $25     $40 ││ |
| │                  │ |
| │ Selected: $25.00 │ |
| │ Savings: $15.00  │ |
| └──────────────────┘ |
|                      |
| "Choose what works   |
| for your situation   |
| - no questions."     |
|                      |
|    [Continue]        |
+----------------------+
```

### 2. Payment Method Selection & Processing

```
+----------------------------------------------------------+
|  🏺 WitchCityRope Payment - Step 2 of 3                |
+----------------------------------------------------------+
|                                                          |
|  💳 Payment Method                                       |
|                                                          |
|  ┌─ Credit or Debit Card ───────────────────────────────┐ |
|  │                                                     │ |
|  │  [Card Number Field with Floating Label]           │ |
|  │  ┌─────────────────────────────────────────┐       │ |
|  │  │ •••• •••• •••• ••••                     │       │ |
|  │  │ Card Number                              │       │ |
|  │  └─────────────────────────────────────────┘       │ |
|  │                                                     │ |
|  │  [Expiry]           [CVC]                           │ |
|  │  ┌─────────┐       ┌─────────┐                     │ |
|  │  │ MM/YY   │       │ •••     │                     │ |
|  │  └─────────┘       └─────────┘                     │ |
|  │                                                     │ |
|  │  ☐ Save payment method for future events           │ |
|  │     (Securely stored with our payment provider)    │ |
|  │                                                     │ |
|  └─────────────────────────────────────────────────────┘ |
|                                                          |
|  🔒 Your payment is secured by industry-leading         |
|      encryption. We never store your card information.  |
|                                                          |
|  ┌─ Order Summary ───────────────────────────────────────┐|
|  │ Shibari Fundamentals Workshop                       │ |
|  │ Sliding Scale Amount:                   $25.00     │ |
|  │ Processing Fee:                         $0.00      │ |
|  │ ────────────────────────────────────────            │ |
|  │ Total:                                  $25.00     │ |
|  └─────────────────────────────────────────────────────┘ |
|                                                          |
|              [← Back]    [Complete Payment]             |
|                                                          |
+----------------------------------------------------------+
```

### 3. Payment Processing State

```
+----------------------------------------------------------+
|  🏺 WitchCityRope Payment                               |
+----------------------------------------------------------+
|                                                          |
|                     Processing Payment...                |
|                                                          |
|                        ⭕ Loading                        |
|                                                          |
|                  Please do not close this page          |
|                                                          |
|              Your registration is being confirmed        |
|                                                          |
+----------------------------------------------------------+
```

### 4. Payment Success Confirmation

```
+----------------------------------------------------------+
|  🏺 WitchCityRope Payment                               |
+----------------------------------------------------------+
|                                                          |
|                         ✅                               |
|                Payment Successful!                       |
|                                                          |
|              Your registration is confirmed              |
|                                                          |
|  ┌─ Registration Details ───────────────────────────────┐ |
|  │                                                     │ |
|  │  📅 Shibari Fundamentals Workshop                  │ |
|  │  🗓️ Saturday, October 15, 2025 • 2:00 PM - 6:00 PM│ |
|  │  📍 Salem Community Center                         │ |
|  │                                                     │ |
|  │  🎫 Registration ID: #WCR-20251015-SF-001          │ |
|  │  💳 Payment: $25.00 (Sliding Scale Applied)        │ |
|  │  📧 Confirmation sent to: member@email.com          │ |
|  │                                                     │ |
|  └─────────────────────────────────────────────────────┘ |
|                                                          |
|  📝 What's Next:                                         |
|  • Check your email for event details and location      |
|  • Add this event to your calendar                      |
|  • Contact us if you have any questions                 |
|                                                          |
|            [View My Registrations] [Register for More] |
|                                                          |
+----------------------------------------------------------+
```

## Sliding Scale Interface Design Details

### Dignified Sliding Scale Approach

#### Core Design Principles
1. **Honor System Messaging**: Clear communication that it's based on trust
2. **No Verification Required**: Never ask for proof or documentation
3. **Positive Framing**: Focus on community support, not charity
4. **Privacy Assured**: Explicit messaging about confidentiality
5. **Range Clarity**: Clear indication of 0-75% discount range

#### Sliding Scale Component Specification

```jsx
// SlidingScaleSelector Component
const SlidingScaleSelector = ({ basePrice, onAmountChange }) => {
  const [selectedAmount, setSelectedAmount] = useState(basePrice);
  const [showSlider, setShowSlider] = useState(false);
  
  const discountPercentage = ((basePrice - selectedAmount) / basePrice) * 100;
  
  return (
    <Box className="sliding-scale-container">
      {/* Pricing Options */}
      <RadioGroup
        value={showSlider ? 'sliding' : 'full'}
        onChange={(value) => setShowSlider(value === 'sliding')}
        size="lg"
      >
        <Stack spacing="md">
          <Radio 
            value="full" 
            label="Full Price"
            description={`Support our community at standard rates ($${basePrice})`}
          />
          <Radio 
            value="sliding" 
            label="Sliding Scale"
            description="Pay what works within your means"
          />
        </Stack>
      </RadioGroup>

      {/* Sliding Scale Selector */}
      {showSlider && (
        <Box className="sliding-scale-selector">
          <Slider
            value={selectedAmount}
            onChange={setSelectedAmount}
            min={basePrice * 0.25} // 75% discount maximum
            max={basePrice}
            step={1}
            marks={[
              { value: basePrice * 0.25, label: `$${basePrice * 0.25}` },
              { value: basePrice * 0.5, label: `$${basePrice * 0.5}` },
              { value: basePrice, label: `$${basePrice}` }
            ]}
            size="lg"
            color="wcr"
          />
          
          {/* Selection Display */}
          <Group justify="space-between" mt="md">
            <Text size="lg" weight={600}>
              Selected Amount: ${selectedAmount.toFixed(2)}
            </Text>
            {discountPercentage > 0 && (
              <Text size="sm" c="wcr.6">
                Savings Applied: ${(basePrice - selectedAmount).toFixed(2)} 
                ({Math.round(discountPercentage)}% sliding scale)
              </Text>
            )}
          </Group>
          
          {/* Community Message */}
          <Alert 
            color="wcr" 
            variant="light" 
            icon={<IconHeart />}
            mt="md"
          >
            Our community thrives when everyone can participate. 
            Choose the amount that works for your situation - no questions asked.
          </Alert>
        </Box>
      )}
    </Box>
  );
};
```

#### Sliding Scale Styling

```css
.sliding-scale-container {
  background: var(--color-ivory);
  border: 2px solid var(--color-rose-gold);
  border-radius: 12px;
  padding: var(--space-lg);
  margin: var(--space-md) 0;
}

.sliding-scale-selector .mantine-Slider-root {
  padding: var(--space-md) 0;
}

.sliding-scale-selector .mantine-Slider-track {
  background: linear-gradient(
    to right, 
    var(--color-burgundy), 
    var(--color-rose-gold), 
    var(--color-amber)
  );
  height: 8px;
}

.sliding-scale-selector .mantine-Slider-thumb {
  background: var(--color-burgundy);
  border: 3px solid var(--color-cream);
  width: 24px;
  height: 24px;
  transition: all 0.3s ease;
}

.sliding-scale-selector .mantine-Slider-thumb:hover {
  transform: scale(1.2);
  box-shadow: 0 4px 12px rgba(136, 1, 36, 0.3);
}
```

### Payment Method Form Design

#### Mantine Components Used

| Component | Purpose | Configuration |
|-----------|---------|--------------|
| TextInput | Card number, name | Floating labels, validation |
| Group | Expiry/CVC layout | Two-column responsive |
| Checkbox | Save payment method | Privacy-conscious messaging |
| Button | Payment submission | Primary amber styling |
| Alert | Security messaging | Trust indicators |
| Paper | Form container | Ivory background, rose-gold border |

#### Form Validation Patterns

```jsx
const PaymentMethodForm = ({ onSubmit, amount, eventTitle }) => {
  const form = useForm({
    initialValues: {
      cardNumber: '',
      expiryDate: '',
      cvc: '',
      cardholderName: '',
      savePaymentMethod: false
    },
    validate: {
      cardNumber: (value) => 
        !/^\d{13,19}$/.test(value.replace(/\s/g, '')) ? 'Invalid card number' : null,
      expiryDate: (value) =>
        !/^(0[1-9]|1[0-2])\/\d{2}$/.test(value) ? 'Invalid expiry date' : null,
      cvc: (value) =>
        !/^\d{3,4}$/.test(value) ? 'Invalid CVC' : null,
      cardholderName: (value) =>
        !value.trim() ? 'Cardholder name is required' : null
    }
  });

  return (
    <Paper className="payment-form-container">
      <Title order={3} mb="md">Payment Method</Title>
      
      <Stack spacing="md">
        <TextInput
          {...form.getInputProps('cardNumber')}
          label="Card Number"
          placeholder="1234 5678 9012 3456"
          leftSection={<IconCreditCard />}
          styles={{
            input: { 
              paddingLeft: '50px',
              fontFamily: 'monospace'
            }
          }}
        />
        
        <Group grow>
          <TextInput
            {...form.getInputProps('expiryDate')}
            label="Expiry Date"
            placeholder="MM/YY"
          />
          <TextInput
            {...form.getInputProps('cvc')}
            label="CVC"
            placeholder="123"
            styles={{ input: { fontFamily: 'monospace' } }}
          />
        </Group>
        
        <TextInput
          {...form.getInputProps('cardholderName')}
          label="Cardholder Name"
          placeholder="Name as it appears on card"
        />
        
        <Checkbox
          {...form.getInputProps('savePaymentMethod', { type: 'checkbox' })}
          label="Save payment method for future events"
          description="Securely stored with our payment provider"
          color="wcr"
        />
        
        <Alert color="blue" variant="light" icon={<IconLock />}>
          Your payment is secured by industry-leading encryption. 
          We never store your card information.
        </Alert>
      </Stack>
    </Paper>
  );
};
```

## Admin Payment Management Interface

### Payment Dashboard Layout

```
+----------------------------------------------------------+
|  Admin Dashboard > Payment Management                    |
+----------------------------------------------------------+
|                                                          |
|  📊 Payment Statistics (Last 30 Days)                   |
|  ┌─────────────┬─────────────┬─────────────┬───────────┐ |
|  │💳 Total     │✅ Successful │❌ Failed    │💰 Revenue │ |
|  │Payments     │Payments     │Payments     │Generated  │ |
|  │             │             │             │           │ |
|  │    247      │    234      │    13       │ $8,640    │ |
|  │  (+12.3%)   │  (94.7%)    │  (5.3%)     │ (+15.2%)  │ |
|  └─────────────┴─────────────┴─────────────┴───────────┘ |
|                                                          |
|  🔍 Payment Search & Filter                              |
|  ┌──────────────────────────────────────────────────────┐ |
|  │ [Search by event, member, transaction ID...]        │ |
|  │                                                      │ |
|  │ Status: [All ▼] Event: [All ▼] Date: [Last 30 Days▼]│ |
|  └──────────────────────────────────────────────────────┘ |
|                                                          |
|  📋 Recent Payments                     [Export CSV]     |
|  ┌──────────────────────────────────────────────────────┐ |
|  │Date       │Member        │Event         │Amount │Status│ |
|  │           │              │              │       │      │ |
|  │Oct 12     │Sarah K.      │Shibari Fund. │$25.00 │✅ Paid│ |
|  │Oct 12     │Mike R.       │Advanced Rope │$40.00 │✅ Paid│ |
|  │Oct 11     │Jessica M.    │Beginner Suss.│$15.00 │✅ Paid│ |
|  │Oct 11     │David L.      │Community Eve.│$30.00 │❌Failed│ |
|  │Oct 10     │Emma T.       │Rope Circles  │$20.00 │💰Refund│ |
|  │           │              │              │       │      │ |
|  │ [Previous Page]               [Page 1 of 12] [Next] │ |
|  └──────────────────────────────────────────────────────┘ |
|                                                          |
+----------------------------------------------------------+
```

### Refund Processing Modal

```
+----------------------------------------------------------+
|  ⚡ Process Refund                               [X]     |
+----------------------------------------------------------+
|                                                          |
|  📋 Payment Details                                      |
|  ┌──────────────────────────────────────────────────────┐ |
|  │ Transaction ID: stripe_pi_1ABC123                    │ |
|  │ Member: Sarah K. (sarah.k@email.com)                │ |
|  │ Event: Shibari Fundamentals Workshop                │ |
|  │ Original Amount: $25.00                             │ |
|  │ Payment Date: October 12, 2025                      │ |
|  │ Payment Status: Completed                           │ |
|  └──────────────────────────────────────────────────────┘ |
|                                                          |
|  💰 Refund Amount                                        |
|  ┌──────────────────────────────────────────────────────┐ |
|  │ ○ Full Refund ($25.00)                              │ |
|  │ ○ Partial Refund                                    │ |
|  │   [Amount: $____] (Max: $25.00)                     │ |
|  └──────────────────────────────────────────────────────┘ |
|                                                          |
|  📝 Refund Reason                                        |
|  ┌──────────────────────────────────────────────────────┐ |
|  │ [Event Cancellation    ▼]                           │ |
|  │                                                      │ |
|  │ Additional Notes (Optional):                         │ |
|  │ ┌──────────────────────────────────────────────────┐ │ |
|  │ │ Event cancelled due to instructor illness.       │ │ |
|  │ │ Automatic full refund processed.                 │ │ |
|  │ └──────────────────────────────────────────────────┘ │ |
|  └──────────────────────────────────────────────────────┘ |
|                                                          |
|  ⚠️ Warning: This action cannot be undone. The refund     |
|     will be processed immediately to the original         |
|     payment method.                                       |
|                                                          |
|                  [Cancel]     [Process Refund]          |
|                                                          |
+----------------------------------------------------------+
```

## Mobile Responsive Design

### Payment Form - Mobile (375px)

```
+----------------------+
| 🏺 Payment Step 2/3 |
+----------------------+
|                      |
| 💳 Payment Method    |
|                      |
| [Card Number]        |
| ┌──────────────────┐ |
| │ 1234 5678 9012   │ |
| │ Card Number      │ |
| └──────────────────┘ |
|                      |
| [Expiry]  [CVC]      |
| ┌───────┐ ┌───────┐  |
| │ 12/27 │ │ 123   │  |
| └───────┘ └───────┘  |
|                      |
| [Name on Card]       |
| ┌──────────────────┐ |
| │ John Doe         │ |
| └──────────────────┘ |
|                      |
| ☐ Save payment      |
|   method            |
|                      |
| 🔒 Secure Checkout   |
|                      |
| ┌──────────────────┐ |
| │ Order Summary:   │ |
| │ Shibari Fund.    │ |
| │ Amount: $25.00   │ |
| │ Total:  $25.00   │ |
| └──────────────────┘ |
|                      |
| [← Back] [Pay $25]   |
+----------------------+
```

### Admin Dashboard - Mobile (375px)

```
+----------------------+
| Admin > Payments     |
+----------------------+
|                      |
| 📊 Stats (30 days)   |
| ┌──────────────────┐ |
| │ 💳 247 Payments  │ |
| │ ✅ 94.7% Success │ |
| │ 💰 $8,640 Revenue│ |
| └──────────────────┘ |
|                      |
| [Search Payments...] |
|                      |
| [All Status ▼]       |
| [All Events ▼]       |
| [Last 30 Days ▼]     |
|                      |
| 📋 Recent Payments   |
| [Export CSV]         |
|                      |
| ┌──────────────────┐ |
| │ Oct 12           │ |
| │ Sarah K.         │ |
| │ Shibari Fund.    │ |
| │ $25.00 ✅ Paid   │ |
| └──────────────────┘ |
|                      |
| ┌──────────────────┐ |
| │ Oct 12           │ |
| │ Mike R.          │ |
| │ Advanced Rope    │ |
| │ $40.00 ✅ Paid   │ |
| └──────────────────┘ |
|                      |
| [Previous] [1] [Next]|
+----------------------+
```

## Accessibility Features

### WCAG 2.1 AA Compliance

#### Visual Accessibility
- **Color Contrast**: 4.5:1 minimum ratio for all text
- **Focus Indicators**: Clear, high-contrast focus rings
- **Color Independence**: Status communicated through icons + color
- **Text Scaling**: Support up to 200% zoom without horizontal scrolling

#### Motor Accessibility
- **Touch Targets**: 44px minimum for mobile interfaces
- **Keyboard Navigation**: Full keyboard accessibility
- **Click Alternatives**: All mouse interactions have keyboard equivalents
- **Error Prevention**: Clear validation with inline feedback

#### Cognitive Accessibility
- **Simple Language**: Clear, jargon-free instructions
- **Progress Indicators**: Clear step tracking (Step 2 of 3)
- **Error Recovery**: Clear error messages with correction guidance
- **Consistent Navigation**: Predictable interface patterns

#### Screen Reader Optimization
```jsx
// Accessibility markup example
<Button
  aria-label={`Complete payment of $${amount} for ${eventTitle}`}
  aria-describedby="payment-security-notice"
  role="button"
  type="submit"
>
  Complete Payment
</Button>

<div id="payment-security-notice" className="sr-only">
  Your payment is secured by industry-leading encryption. 
  Processing typically takes 2-3 seconds.
</div>
```

## Error States & Edge Cases

### Payment Failure Scenarios

#### Card Declined
```
+----------------------------------------------------------+
|  ❌ Payment Failed                                        |
+----------------------------------------------------------+
|                                                          |
|  Your payment could not be processed.                   |
|                                                          |
|  🔍 Possible Reasons:                                    |
|  • Card was declined by your bank                       |
|  • Insufficient funds                                   |
|  • Expired card                                         |
|  • Incorrect card information                           |
|                                                          |
|  💡 What you can do:                                     |
|  • Check your card information and try again            |
|  • Contact your bank to authorize the payment           |
|  • Try a different payment method                       |
|                                                          |
|  Your registration is still being held for 15 minutes   |
|  while you resolve the payment issue.                   |
|                                                          |
|              [Try Different Card] [Retry Payment]       |
|                                                          |
+----------------------------------------------------------+
```

#### Network/Technical Error
```
+----------------------------------------------------------+
|  ⚠️ Payment Processing Issue                             |
+----------------------------------------------------------+
|                                                          |
|  We encountered a technical issue processing your       |
|  payment. Please don't worry - no charge was made.      |
|                                                          |
|  🔄 What's happening:                                    |
|  • Checking with our payment processor                  |
|  • Your registration is temporarily held                |
|  • No charge will appear on your statement              |
|                                                          |
|  💡 Next steps:                                          |
|  • Wait a moment and try again                          |
|  • Contact us if the problem persists                   |
|                                                          |
|  📞 Need help? Call (555) 123-4567                      |
|  📧 or email payments@witchcityrope.com                 |
|                                                          |
|                      [Try Again]                        |
|                                                          |
+----------------------------------------------------------+
```

### Loading States

#### Payment Processing
```jsx
const PaymentProcessingState = ({ eventTitle, amount }) => (
  <Stack align="center" spacing="xl" py="xl">
    <Loader size="xl" color="wcr" />
    <Stack align="center" spacing="sm">
      <Title order={2}>Processing Payment...</Title>
      <Text c="dimmed" ta="center">
        Please do not close this page while we process your ${amount} payment
        for {eventTitle}
      </Text>
      <Text size="sm" c="dimmed" ta="center">
        This typically takes 2-3 seconds
      </Text>
    </Stack>
    
    <Progress
      value={100}
      animated
      color="wcr"
      size="sm"
      style={{ width: '200px' }}
    />
  </Stack>
);
```

#### Skeleton Loading for Admin Dashboard
```jsx
const PaymentTableSkeleton = () => (
  <Table>
    <Table.Tbody>
      {Array(5).fill(0).map((_, i) => (
        <Table.Tr key={i}>
          <Table.Td><Skeleton height={20} /></Table.Td>
          <Table.Td><Skeleton height={20} /></Table.Td>
          <Table.Td><Skeleton height={20} /></Table.Td>
          <Table.Td><Skeleton height={20} width="60%" /></Table.Td>
          <Table.Td><Skeleton height={24} width="80px" /></Table.Td>
        </Table.Tr>
      ))}
    </Table.Tbody>
  </Table>
);
```

## Integration with Existing UI Patterns

### Design System Integration

#### Colors (Design System v7)
```css
:root {
  /* Payment-specific color applications */
  --payment-success: #228B22;
  --payment-error: #DC143C;
  --payment-warning: #DAA520;
  --payment-pending: var(--color-stone);
  --payment-background: var(--color-ivory);
  --payment-border: var(--color-rose-gold);
  --payment-accent: var(--color-burgundy);
  --slider-track: linear-gradient(to right, var(--color-burgundy), var(--color-rose-gold), var(--color-amber));
}
```

#### Typography
```css
/* Payment form headers */
.payment-header {
  font-family: 'Montserrat', sans-serif;
  font-weight: 600;
  color: var(--color-burgundy);
}

/* Amount displays */
.payment-amount {
  font-family: 'Montserrat', sans-serif;
  font-weight: 700;
  font-size: 1.25rem;
}

/* Form labels */
.payment-label {
  font-family: 'Source Sans 3', sans-serif;
  font-weight: 500;
}

/* Community messaging */
.community-message {
  font-family: 'Source Sans 3', sans-serif;
  font-style: italic;
  color: var(--color-burgundy);
}
```

#### Button Styling
```css
.payment-button-primary {
  background: linear-gradient(135deg, var(--color-amber), #DAA520);
  border: none;
  border-radius: 12px 6px 12px 6px;
  color: var(--color-charcoal);
  font-family: 'Montserrat', sans-serif;
  font-weight: 600;
  padding: var(--space-sm) var(--space-lg);
  transition: all 0.3s ease;
}

.payment-button-primary:hover {
  border-radius: 6px 12px 6px 12px;
  box-shadow: 0 4px 12px rgba(255, 191, 0, 0.3);
  transform: translateY(-1px);
}

.payment-button-secondary {
  background: transparent;
  border: 2px solid var(--color-burgundy);
  border-radius: 12px 6px 12px 6px;
  color: var(--color-burgundy);
}
```

### Navigation Integration

The payment system integrates with existing navigation patterns:

1. **Breadcrumb Navigation**: Events > Event Name > Payment
2. **Progress Indicators**: Step 1: Details, Step 2: Payment, Step 3: Confirmation
3. **Back Navigation**: Always provide way to return to previous step
4. **Session Management**: Maintain payment session across navigation

### Form Integration Patterns

#### Floating Label Implementation
```jsx
const FloatingTextInput = ({ label, ...props }) => (
  <div className="floating-input-container">
    <TextInput
      {...props}
      placeholder=" " // Required for floating label CSS
      classNames={{
        input: 'floating-input',
        label: 'floating-label'
      }}
    />
    <label className="floating-label">{label}</label>
  </div>
);
```

#### Validation Integration
```jsx
const usePaymentValidation = () => {
  return useForm({
    validate: zodResolver(z.object({
      cardNumber: z.string()
        .min(13, 'Card number must be at least 13 digits')
        .max(19, 'Card number cannot exceed 19 digits'),
      expiryDate: z.string()
        .regex(/^(0[1-9]|1[0-2])\/\d{2}$/, 'Invalid expiry date format'),
      cvc: z.string()
        .min(3, 'CVC must be 3-4 digits')
        .max(4, 'CVC must be 3-4 digits'),
      amount: z.number()
        .positive('Amount must be greater than 0')
        .max(1000, 'Amount cannot exceed $1000')
    }))
  });
};
```

## Community Values Implementation

### Dignity-Preserving Design

#### Language Choices
- **"Sliding Scale"** instead of "Discount" or "Assistance"
- **"Pay what works for you"** instead of "Reduced price"
- **"Community support"** framing instead of charity language
- **"No questions asked"** to emphasize trust and privacy

#### Visual Design Choices
- **No shame indicators**: Sliding scale looks identical to regular pricing
- **Positive messaging**: Community values and mutual support emphasis  
- **Privacy assurance**: Clear messaging about confidentiality
- **Equal treatment**: Same checkout process regardless of amount

#### Honor System Implementation
- **No verification fields** for sliding scale eligibility
- **No documentation required** to access reduced pricing
- **No means testing** or income requirements
- **Pure trust-based** system honoring member integrity

### Inclusive Payment Experience

#### Financial Accessibility
- **Wide price range**: 75% discount maximum ensures broad accessibility
- **Flexible amounts**: Slider allows any amount within range
- **No penalty messaging**: Full price and sliding scale presented equally
- **Future consideration**: Option to pay more when circumstances improve

#### Technical Accessibility
- **Screen reader compatible**: Full ARIA labeling and semantic HTML
- **Keyboard navigable**: Complete keyboard interaction support
- **High contrast**: Strong color contrast for visual accessibility
- **Mobile optimized**: Touch-friendly interface for all devices

## Quality Validation Checklist

### Design System Compliance
- [ ] Colors match Design System v7 exactly (#880124 burgundy, #FAF6F2 cream)
- [ ] Typography uses correct font families (Montserrat headers, Source Sans body)
- [ ] Button styling uses signature corner morphing (no vertical movement)
- [ ] Floating labels implemented on ALL form inputs
- [ ] Mobile responsive at 768px breakpoint
- [ ] Rose-gold borders and ivory backgrounds applied consistently

### Payment Flow Validation
- [ ] Sliding scale interface is dignified and shame-free
- [ ] Payment processing states provide clear feedback
- [ ] Error handling preserves user data and provides recovery options
- [ ] Success confirmation includes all necessary details
- [ ] Mobile experience maintains full functionality

### Accessibility Validation
- [ ] WCAG 2.1 AA compliance verified
- [ ] Screen reader testing completed
- [ ] Keyboard navigation fully functional
- [ ] Color contrast ratios meet 4.5:1 minimum
- [ ] Touch targets meet 44px minimum on mobile

### Community Values Validation
- [ ] Sliding scale messaging promotes dignity and inclusion
- [ ] No verification or documentation required for reduced pricing
- [ ] Privacy and confidentiality prominently assured
- [ ] Honor system clearly communicated and respected
- [ ] Equal user experience regardless of payment amount

### Admin Interface Validation
- [ ] Payment management interface maintains existing admin patterns
- [ ] Refund processing includes necessary business rule validation
- [ ] Reporting capabilities support administrative needs
- [ ] Mobile admin experience functional for basic tasks

This comprehensive UI design preserves WitchCityRope's community values while providing a secure, accessible, and dignified payment experience that removes financial barriers to participation.

## File Registry Update

| Date | File Path | Action | Purpose | Session/Task | Status |
|------|-----------|--------|---------|--------------|--------|
| 2025-09-13 | `/docs/functional-areas/api-cleanup/new-work/2025-09-13-payment-extraction/design/payment-system-ui-design.md` | CREATED | Comprehensive UI design for Payment system with sliding scale pricing and accessible payment flows | Payment System UI Design | ACTIVE |
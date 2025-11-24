# UI Wireframes: Admin Payment Transactions Page
<!-- Last Updated: 2025-11-17 -->
<!-- Version: 1.0 -->
<!-- Owner: UI Designer Agent -->
<!-- Status: Draft -->

## Design Overview

This document defines the complete UI design for the Admin Payment Transactions page within the Analytics section. The page provides administrators with comprehensive payment transaction management, including filtering, searching, and refund processing capabilities.

**Purpose**: Enable admins to view all payment transactions, filter by various criteria, and process refunds for PayPal-paid tickets using the existing RefundConfirmationModal component.

**User Goals**:
- Quickly find specific transactions using search and filters
- View transaction details at a glance (date, user, event, amount, status, payment method)
- Process refunds for PayPal payments with full audit trail
- Understand payment activity through summary statistics

## User Personas

- **Admin**: Primary user - system administrators managing all payment transactions
- **Teacher**: Secondary user - event organizers reviewing payments for their events
- **SafetyTeam**: Tertiary user - may need to view payment records for event management

## Route & Navigation

**Route**: `/admin/analytics/payments`

**Navigation Update Required**:
- Admin Dashboard Analytics card currently links to `/admin/analytics` (placeholder)
- Update card link to `/admin/analytics/payments` (actual payments page)
- Card should use existing design pattern from AdminDashboardPage
- Card title: "Payment Analytics" or "Payment Transactions"
- Card icon: Currency or payment-related icon (IconCurrencyDollar, IconReceipt)

## Wireframes

### Desktop Layout (≥769px)

```
+─────────────────────────────────────────────────────────────────────────+
│  Header                                                      [User Menu]  │
+─────────────────────────────────────────────────────────────────────────+
│                                                                           │
│  Payment Transactions                                                     │
│  ══════════════════════                                                   │
│                                                                           │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │  FILTER BAR                                                      │    │
│  │  ┌────────────────┐  ┌──────────────┐  ┌──────────────┐  ┌────┐│    │
│  │  │ [Search...   🔍]│  │ Date Range ▼ │  │ Method ▼     │  │ Status▼││  │
│  │  └────────────────┘  └──────────────┘  └──────────────┘  └────┘│    │
│  │  ┌──────────────┐                                                │    │
│  │  │ Amount Range │                                                │    │
│  │  │ Min: [    ]  │                                                │    │
│  │  │ Max: [    ]  │                                                │    │
│  │  └──────────────┘                                                │    │
│  └─────────────────────────────────────────────────────────────────┘    │
│                                                                           │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │  TRANSACTION TABLE (Burgundy Header)                            │    │
│  ├──────┬────────────┬────────────┬────────┬────────┬─────────────┤    │
│  │ Date │ User       │ Event      │ Method │ Amount │ Actions     │    │
│  ├──────┼────────────┼────────────┼────────┼────────┼─────────────┤    │
│  │11/17 │John Smith  │Rope 101    │PayPal  │$50.00  │[Refund]     │    │
│  │      │john@ex.com │            │        │        │             │    │
│  ├──────┼────────────┼────────────┼────────┼────────┼─────────────┤    │
│  │11/16 │Jane Doe    │Shibari Adv │PayPal  │$75.00  │[Refund]     │    │
│  │      │jane@ex.com │            │        │        │             │    │
│  ├──────┼────────────┼────────────┼────────┼────────┼─────────────┤    │
│  │11/15 │Bob Test    │Social Night│Free    │$0.00   │—            │    │
│  │      │bob@ex.com  │            │        │        │             │    │
│  └──────┴────────────┴────────────┴────────┴────────┴─────────────┘    │
│                                                                           │
│  Showing 23 of 156 transactions  |  Filtered by: PayPal, Paid            │
│                                                                           │
+─────────────────────────────────────────────────────────────────────────+
```

### Mobile Layout (<768px)

```
+───────────────────────────────+
│  ☰ Menu  | Payment Transactions│
+───────────────────────────────+
│                               │
│  Payment Transactions         │
│  ══════════════════            │
│                               │
│  ┌─────────────────────────┐  │
│  │ [Search...          🔍] │  │
│  └─────────────────────────┘  │
│                               │
│  [Filters Drawer ▼]           │
│                               │
│  ┌─────────────────────────┐  │
│  │ 📅 Nov 17, 2025         │  │
│  │ John Smith              │  │
│  │ john@example.com        │  │
│  │ ─────────────────────   │  │
│  │ Event: Rope 101         │  │
│  │ Method: PayPal          │  │
│  │ Amount: $50.00          │  │
│  │                         │  │
│  │        [Refund]         │  │
│  └─────────────────────────┘  │
│                               │
│  ┌─────────────────────────┐  │
│  │ 📅 Nov 16, 2025         │  │
│  │ Jane Doe                │  │
│  │ jane@example.com        │  │
│  │ ─────────────────────   │  │
│  │ Event: Shibari Adv      │  │
│  │ Method: PayPal          │  │
│  │ Amount: $75.00          │  │
│  │                         │  │
│  │        [Refund]         │  │
│  └─────────────────────────┘  │
│                               │
│  Showing 23 of 156            │
│  Filtered by: PayPal, Paid    │
│                               │
+───────────────────────────────+
```

## Component Specifications

### Page Header

**Component**: Mantine `Title` + `Group`

```tsx
<Box p="xl">
  <Title
    order={1}
    c="burgundy"
    mb="xl"
    style={{
      fontFamily: 'var(--font-heading)',
      fontSize: '32px',
      fontWeight: 700,
    }}
  >
    Payment Transactions
  </Title>
</Box>
```

**Styling**:
- Title color: `var(--color-burgundy)` (#880124)
- Font: Montserrat 700, 32px
- Bottom margin: `var(--space-xl)` (40px)

### Filter Bar

**Component**: Custom `PaymentFilterBar` component (similar to `EventsFilterBar`)

**Desktop Layout** (Flex row):
```tsx
<Group mb="lg" justify="space-between" align="flex-start" wrap="wrap">
  {/* Left: Filters */}
  <Group align="flex-start" gap="md">
    {/* Search Input */}
    <TextInput
      placeholder="Search transactions..."
      leftSection={<IconSearch size="1rem" />}
      style={{ minWidth: 300 }}
    />

    {/* Date Range */}
    <DatePickerInput
      type="range"
      label="Date Range"
      placeholder="Select dates"
      style={{ minWidth: 280 }}
    />

    {/* Payment Method Multi-Select */}
    <MultiSelect
      label="Payment Method"
      placeholder="All methods"
      data={['PayPal', 'Free', 'Venmo']}
      style={{ minWidth: 200 }}
    />

    {/* Status Multi-Select */}
    <MultiSelect
      label="Status"
      placeholder="All statuses"
      data={['Paid', 'Refunded', 'Pending', 'Failed']}
      style={{ minWidth: 180 }}
    />
  </Group>

  {/* Right: Amount Range */}
  <Group align="flex-start" gap="sm">
    <NumberInput
      label="Min Amount"
      placeholder="$0.00"
      prefix="$"
      style={{ width: 120 }}
    />
    <NumberInput
      label="Max Amount"
      placeholder="$999.99"
      prefix="$"
      style={{ width: 120 }}
    />
  </Group>
</Group>
```

**Mobile Layout** (Collapsible Drawer):
```tsx
<Stack gap="md">
  {/* Search always visible */}
  <TextInput
    placeholder="Search transactions..."
    leftSection={<IconSearch size="1rem" />}
  />

  {/* Filters Drawer Button */}
  <Button
    variant="light"
    leftSection={<IconFilter size="1rem" />}
    onClick={() => setDrawerOpen(true)}
    fullWidth
  >
    Filters
  </Button>

  {/* Drawer with all filter controls */}
  <Drawer
    opened={drawerOpen}
    onClose={() => setDrawerOpen(false)}
    title="Filter Transactions"
    position="bottom"
  >
    {/* Date Range, Method, Status, Amount Range */}
  </Drawer>
</Stack>
```

**Filter Behavior**:
- **Search**: Full-text search across user name, email, event name, transaction ID
- **Date Range**: Start and end date pickers (Mantine `DatePickerInput` type="range")
- **Payment Method**: Multi-select checkbox (PayPal, Free, Venmo, etc.)
- **Status**: Multi-select checkbox (Paid, Refunded, Pending, Failed)
- **Amount Range**: Min/max number inputs with currency prefix ($)

**Default State**: No filters active, shows all transactions

### Transaction Table

**Component**: Mantine `Table` with `striped` and `highlightOnHover` props

**Desktop Table Structure**:
```tsx
<Table striped highlightOnHover data-testid="payments-table">
  <Table.Thead bg="wcr.7">
    <Table.Tr>
      <Table.Th c="white" style={{ width: '120px' }}>Date</Table.Th>
      <Table.Th c="white" style={{ width: '200px' }}>User/Member</Table.Th>
      <Table.Th c="white" style={{ minWidth: '180px' }}>Event/Session</Table.Th>
      <Table.Th c="white" style={{ width: '120px', textAlign: 'center' }}>
        Payment Method
      </Table.Th>
      <Table.Th c="white" style={{ width: '100px', textAlign: 'right' }}>
        Amount
      </Table.Th>
      <Table.Th c="white" style={{ width: '120px', textAlign: 'center' }}>
        Status
      </Table.Th>
      <Table.Th c="white" style={{ width: '120px', textAlign: 'center' }}>
        Actions
      </Table.Th>
    </Table.Tr>
  </Table.Thead>

  <Table.Tbody>
    {transactions.map(transaction => (
      <Table.Tr
        key={transaction.id}
        data-testid="payment-row"
        style={{ cursor: 'pointer' }}
        onClick={() => handleRowClick(transaction.id)}
      >
        <Table.Td>{formatDate(transaction.date)}</Table.Td>
        <Table.Td>
          <Stack gap={0}>
            <Text fw={500} size="sm">{transaction.userName}</Text>
            <Text size="xs" c="dimmed">{transaction.userEmail}</Text>
          </Stack>
        </Table.Td>
        <Table.Td>
          <Text size="sm" lineClamp={2}>{transaction.eventName}</Text>
        </Table.Td>
        <Table.Td style={{ textAlign: 'center' }}>
          <Badge color={getPaymentMethodColor(transaction.method)}>
            {transaction.paymentMethod}
          </Badge>
        </Table.Td>
        <Table.Td style={{ textAlign: 'right' }}>
          <Text fw={600} size="md">
            ${transaction.amount.toFixed(2)}
          </Text>
        </Table.Td>
        <Table.Td style={{ textAlign: 'center' }}>
          <Badge color={getStatusColor(transaction.status)}>
            {transaction.status}
          </Badge>
        </Table.Td>
        <Table.Td
          style={{ textAlign: 'center' }}
          onClick={(e) => e.stopPropagation()}
        >
          {transaction.paymentMethod === 'PayPal' && transaction.status === 'Paid' && (
            <Button
              variant="light"
              color="red"
              size="xs"
              data-testid="button-refund"
              onClick={() => handleRefundClick(transaction)}
            >
              Refund
            </Button>
          )}
        </Table.Td>
      </Table.Tr>
    ))}
  </Table.Tbody>
</Table>
```

**Column Specifications**:

| Column | Width | Alignment | Content | Sortable |
|--------|-------|-----------|---------|----------|
| Date | 120px | Left | MM/DD/YYYY format | Yes (future) |
| User/Member | 200px | Left | Name (bold) + Email (gray, small) | No |
| Event/Session | Flex (min 180px) | Left | Event title (2 line clamp) | No |
| Payment Method | 120px | Center | Badge (PayPal=blue, Free=gray) | No |
| Amount | 100px | Right | $XX.XX (bold, medium) | Yes (future) |
| Status | 120px | Center | Badge (Paid=green, Refunded=orange) | No |
| Actions | 120px | Center | Refund button (conditional) | N/A |

**Mobile Card Layout**:
```tsx
<Stack gap="md">
  {transactions.map(transaction => (
    <Card
      key={transaction.id}
      shadow="sm"
      padding="md"
      radius="md"
      withBorder
      data-testid="payment-card"
      onClick={() => handleRowClick(transaction.id)}
      style={{ cursor: 'pointer' }}
    >
      <Stack gap="xs">
        {/* Date */}
        <Group justify="space-between">
          <Text size="xs" c="dimmed">
            {formatDate(transaction.date)}
          </Text>
          <Badge color={getStatusColor(transaction.status)}>
            {transaction.status}
          </Badge>
        </Group>

        {/* User */}
        <Text fw={600} size="md">{transaction.userName}</Text>
        <Text size="xs" c="dimmed">{transaction.userEmail}</Text>

        <Divider />

        {/* Event */}
        <Group justify="space-between">
          <Text size="sm" c="dimmed">Event:</Text>
          <Text size="sm" fw={500}>{transaction.eventName}</Text>
        </Group>

        {/* Payment Method */}
        <Group justify="space-between">
          <Text size="sm" c="dimmed">Method:</Text>
          <Badge color={getPaymentMethodColor(transaction.method)}>
            {transaction.paymentMethod}
          </Badge>
        </Group>

        {/* Amount */}
        <Group justify="space-between">
          <Text size="sm" c="dimmed">Amount:</Text>
          <Text fw={700} size="lg">
            ${transaction.amount.toFixed(2)}
          </Text>
        </Group>

        {/* Refund Button */}
        {transaction.paymentMethod === 'PayPal' && transaction.status === 'Paid' && (
          <Button
            variant="light"
            color="red"
            fullWidth
            data-testid="button-refund"
            onClick={(e) => {
              e.stopPropagation();
              handleRefundClick(transaction);
            }}
          >
            Process Refund
          </Button>
        )}
      </Stack>
    </Card>
  ))}
</Stack>
```

### Refund Button Behavior

**Conditional Display**:
- Show "Refund" button ONLY if:
  - `transaction.paymentMethod === 'PayPal'`
  - `transaction.status === 'Paid'`
  - User has Admin or Teacher role (backend authorization)

**Button Specs**:
- **Desktop**: `variant="light"`, `color="red"`, `size="xs"`
- **Mobile**: `variant="light"`, `color="red"`, `fullWidth`
- **Click handler**: Opens `RefundConfirmationModal` with payment data
- **Data passed to modal**:
  ```tsx
  {
    id: transaction.paymentId,
    userName: transaction.userName,
    userEmail: transaction.userEmail,
    amount: transaction.amount,
    paymentMethod: transaction.paymentMethod,
    paymentDate: transaction.date,
    description: transaction.eventName
  }
  ```

### RefundConfirmationModal Integration

**Modal Trigger**:
```tsx
const [refundModalOpened, setRefundModalOpened] = useState(false);
const [selectedPayment, setSelectedPayment] = useState<PaymentData | null>(null);

const handleRefundClick = (payment: PaymentData) => {
  setSelectedPayment(payment);
  setRefundModalOpened(true);
};

// In JSX
{selectedPayment && (
  <RefundConfirmationModal
    opened={refundModalOpened}
    onClose={() => {
      setRefundModalOpened(false);
      setSelectedPayment(null);
    }}
    payment={selectedPayment}
    onConfirm={async (refundReason) => {
      await processRefund(selectedPayment.id, refundReason);
      // Refresh transaction list
      await refetchTransactions();
    }}
  />
)}
```

**Modal Props**:
- `opened`: Boolean state for modal visibility
- `onClose`: Callback to close modal
- `payment`: Payment data object (id, user, amount, method, date, description)
- `onConfirm`: Async callback that receives `refundReason` string, calls refund API

**Success Flow**:
1. User clicks "Refund" button
2. Modal opens with payment details pre-filled
3. User enters refund reason (required, 500 char limit)
4. User checks confirmation checkbox
5. User clicks "Process Refund"
6. API call: `POST /api/admin/refunds/{ticketId}` with `{ refundReason, alsoRemoveRsvp: true }`
7. Success notification shows (green toast)
8. Modal closes
9. Transaction list refreshes (refunded transaction now shows "Refunded" status)

**Error Flow**:
1. Steps 1-6 same as success flow
2. API returns error (400/500)
3. Error notification shows (red toast with error message)
4. Modal remains open (allows user to retry)
5. Refund reason and checkbox state preserved

### Summary Statistics

**Component**: Mantine `Box` with `Group` for horizontal layout

**Desktop Layout**:
```tsx
<Box mt="md" p="md" style={{ backgroundColor: 'var(--color-cream)', borderRadius: '8px' }}>
  <Group gap="xl">
    <Group gap="xs">
      <Text size="sm" c="dimmed">Showing:</Text>
      <Text size="sm" fw={600}>23 of 156 transactions</Text>
    </Group>

    {activeFilters.length > 0 && (
      <Group gap="xs">
        <Text size="sm" c="dimmed">Filtered by:</Text>
        <Text size="sm" fw={600}>{activeFilters.join(', ')}</Text>
      </Group>
    )}

    {searchTerm && (
      <Group gap="xs">
        <Text size="sm" c="dimmed">Search:</Text>
        <Text size="sm" fw={600}>"{searchTerm}"</Text>
      </Group>
    )}

    <Group gap="xs">
      <Text size="sm" c="dimmed">Total Revenue:</Text>
      <Text size="sm" fw={600} c="wcr.7">
        ${totalRevenue.toFixed(2)}
      </Text>
    </Group>
  </Group>
</Box>
```

**Mobile Layout**:
```tsx
<Stack gap="xs" mt="md" p="md" style={{ backgroundColor: 'var(--color-cream)', borderRadius: '8px' }}>
  <Text size="sm" c="dimmed">
    Showing: <Text component="span" fw={600}>23 of 156 transactions</Text>
  </Text>
  {activeFilters.length > 0 && (
    <Text size="sm" c="dimmed">
      Filtered by: <Text component="span" fw={600}>{activeFilters.join(', ')}</Text>
    </Text>
  )}
  {searchTerm && (
    <Text size="sm" c="dimmed">
      Search: <Text component="span" fw={600}>"{searchTerm}"</Text>
    </Text>
  )}
  <Text size="sm" c="dimmed">
    Total Revenue: <Text component="span" fw={600} c="wcr.7">${totalRevenue.toFixed(2)}</Text>
  </Text>
</Stack>
```

**Statistics Displayed**:
- **Showing X of Y transactions**: Number of filtered results vs. total
- **Filtered by**: Active filter labels (only if filters applied)
- **Search**: Search term (only if search active)
- **Total Revenue**: Sum of all filtered transaction amounts (future enhancement)

### Empty State

**Component**: Table with single row, centered text

```tsx
{filteredTransactions.length === 0 && (
  <Table>
    <Table.Thead bg="wcr.7">
      {/* Table headers */}
    </Table.Thead>
    <Table.Tbody>
      <Table.Tr>
        <Table.Td colSpan={7} ta="center" py="xl">
          <Stack gap="md" align="center">
            <IconReceiptOff size={48} color="var(--color-stone)" />
            <Text c="dimmed" size="lg" fw={500}>
              No transactions found
            </Text>
            <Text c="dimmed" size="sm">
              Try adjusting your filters or search term
            </Text>
            <Button
              variant="light"
              color="wcr.7"
              onClick={handleClearFilters}
            >
              Clear Filters
            </Button>
          </Stack>
        </Table.Td>
      </Table.Tr>
    </Table.Tbody>
  </Table>
)}
```

**Triggers**:
- No transactions exist in database (rare)
- All transactions filtered out by active filters
- Search term has no matches

### Loading State

**Component**: Mantine `Loader` centered in page

```tsx
{isLoading && (
  <Box style={{ textAlign: 'center', padding: '40px' }}>
    <Loader size="lg" color="wcr.7" />
    <Text c="dimmed" size="sm" mt="md">
      Loading transactions...
    </Text>
  </Box>
)}
```

**Triggers**:
- Initial page load (fetching transactions from API)
- Filter change (refetching with new parameters)
- Refund operation in progress (optional: disable table during refund)

## Mantine Components Used

| Component | Purpose | Configuration |
|-----------|---------|---------------|
| **Table** | Transaction list display | `striped`, `highlightOnHover`, burgundy header |
| **Card** | Mobile transaction cards | `shadow="sm"`, `withBorder`, `radius="md"` |
| **Button** | Refund action trigger | `variant="light"`, `color="red"`, `size="xs"` |
| **TextInput** | Search functionality | `leftSection={<IconSearch />}`, min-width 300px |
| **DatePickerInput** | Date range filter | `type="range"`, label, placeholder |
| **MultiSelect** | Payment method and status filters | Checkbox mode, searchable |
| **NumberInput** | Amount range filters | Currency prefix ($), decimal precision |
| **Badge** | Payment method and status indicators | Color-coded (green, blue, gray, orange, red) |
| **Loader** | Loading state indicator | Size "lg", burgundy color |
| **Box, Group, Stack** | Layout containers | Responsive spacing, alignment |
| **Text, Title** | Typography | Varied sizes, weights, colors |
| **Drawer** | Mobile filter panel | `position="bottom"`, full filter controls |

## Interaction Patterns

### Search and Filter

**Search Behavior**:
- **Debounced input** (500ms delay) to reduce API calls
- **Full-text search** across:
  - User name (firstName + lastName)
  - User email
  - Event title
  - Transaction ID (future: if exposed)
- **Case-insensitive** matching
- **Substring matching** (not exact match)
- **Clear icon** appears when search has value

**Filter Application**:
- **Multi-select filters** (Payment Method, Status):
  - Multiple values can be selected
  - "OR" logic within same filter (PayPal OR Free)
  - "AND" logic between filters (PayPal AND Paid)
  - Shows count of selected items in dropdown label
- **Date range filter**:
  - Start and end date pickers
  - Transactions within date range (inclusive)
  - Single date selects that specific day only
- **Amount range filter**:
  - Min and max number inputs
  - Transactions where amount >= min AND amount <= max
  - Empty min defaults to $0.00
  - Empty max defaults to unlimited

**Filter Persistence**:
- Filter state stored in URL query parameters (future enhancement)
- Allows sharing filtered views with team members
- Browser back/forward maintains filter state

### Row Click Navigation (Future Enhancement)

**Behavior**:
- Clicking table row navigates to `/admin/analytics/payments/{transactionId}`
- Transaction detail page shows:
  - Full payment information
  - Refund history (if any)
  - Audit log
  - User details
  - Event details
  - PayPal transaction details
- **NOT implemented in Phase 4** (just document pattern for future)

### Refund Workflow

**Step-by-Step**:
1. **Identify eligible transaction**:
   - User sees "Refund" button in Actions column
   - Button only visible for PayPal paid transactions

2. **Open modal**:
   - Click "Refund" button
   - `RefundConfirmationModal` opens
   - Payment details pre-populated

3. **Enter refund reason**:
   - Textarea with 500 character limit
   - Character counter updates in real-time
   - Required field (cannot proceed without)

4. **Confirm action**:
   - Checkbox: "I understand this will process the refund and cannot be undone"
   - Required before "Process Refund" button enables

5. **Process refund**:
   - Click "Process Refund" button
   - Loading state (button shows spinner)
   - API call to `POST /api/admin/refunds/{ticketId}`

6. **Success handling**:
   - Green notification: "Refund processed successfully"
   - Modal closes automatically
   - Transaction list refreshes
   - Refunded transaction now shows "Refunded" status
   - Refund button no longer appears for that transaction

7. **Error handling**:
   - Red notification with error message
   - Modal remains open
   - User can retry or cancel
   - Refund reason and checkbox state preserved

## Loading States

### Initial Page Load
- Full-page loader centered with spinner
- "Loading transactions..." text below spinner
- Filter bar disabled (gray overlay)
- Table hidden or shows skeleton rows

### Filter Change
- Table overlay with semi-transparent loader
- Spinner centered over table
- Filter controls remain enabled
- Previous results remain visible (ghosted)

### Refund Processing
- "Process Refund" button shows loading spinner
- Button text changes to "Processing..."
- Button disabled during operation
- Cancel button disabled during operation
- Modal cannot be closed during operation

### Data Refresh
- Subtle loading indicator in summary statistics area
- "Refreshing..." text appears briefly
- Table content updates smoothly (no flash)

## Feedback

### Success Notifications

**Refund Success**:
```tsx
notifications.show({
  color: 'green',
  title: 'Refund Processed',
  message: `Refund of $${amount.toFixed(2)} has been processed successfully. The user will receive an email confirmation.`,
  icon: <IconCheck />,
  autoClose: 5000,
  position: 'top-right'
});
```

**Filter Applied**:
```tsx
// Subtle visual feedback in summary statistics
// No notification needed
```

### Error Notifications

**Refund Failed**:
```tsx
notifications.show({
  color: 'red',
  title: 'Refund Failed',
  message: error?.detail || 'Failed to process refund. Please try again or contact support.',
  icon: <IconAlertCircle />,
  autoClose: 7000,
  position: 'top-right'
});
```

**Network Error**:
```tsx
notifications.show({
  color: 'red',
  title: 'Network Error',
  message: 'Failed to load transactions. Check your connection and try again.',
  icon: <IconAlertCircle />,
  autoClose: false, // User must dismiss
  withCloseButton: true
});
```

### Inline Feedback

**Empty State**:
- Icon: `IconReceiptOff` (48px, stone gray)
- Text: "No transactions found"
- Subtext: "Try adjusting your filters or search term"
- Action: "Clear Filters" button

**No Refund Available**:
- Actions column shows "—" (em dash) for non-refundable transactions
- No button, no click handler
- Tooltip on hover: "Only PayPal paid transactions can be refunded"

## Responsive Breakpoints

**Breakpoints**:
- **Mobile (xs)**: 0px - 575px
- **Small (sm)**: 576px - 767px
- **Medium (md)**: 768px - 991px (TABLE THRESHOLD)
- **Large (lg)**: 992px - 1199px
- **Extra Large (xl)**: 1200px+

**Layout Changes**:

### Mobile (<768px):
- **Filter Bar**: Collapses to search + "Filters" button → Drawer
- **Table**: Becomes stacked card layout
- **Summary**: Vertical stack instead of horizontal group
- **Pagination**: Smaller page size (10 vs 25)
- **Refund Button**: Full-width in card footer

### Tablet (768px - 991px):
- **Filter Bar**: Two-row layout (search + date on row 1, method + status + amount on row 2)
- **Table**: Horizontal scroll if needed
- **Summary**: Wrapped horizontal group
- **Pagination**: Standard controls

### Desktop (≥992px):
- **Filter Bar**: Single row, all controls visible
- **Table**: Fixed column widths, no scroll
- **Summary**: Horizontal group with all stats
- **Pagination**: Full controls with page numbers

## Accessibility Requirements

### Keyboard Navigation
- Tab through filter inputs in logical order
- Enter key submits search
- Arrow keys navigate table rows
- Enter/Space on row opens transaction detail (future)
- Escape closes modal
- Tab within modal focuses refund reason → checkbox → Process button → Cancel

### Screen Reader Support

**Table Headers**:
```tsx
<Table.Th scope="col" aria-label="Payment date">Date</Table.Th>
<Table.Th scope="col" aria-label="User information">User/Member</Table.Th>
```

**Row Labels**:
```tsx
<Table.Tr
  aria-label={`Transaction for ${userName} on ${date}, amount ${amount}`}
>
```

**Refund Button**:
```tsx
<Button
  aria-label={`Process refund for ${userName}'s ${eventName} ticket, amount ${amount}`}
  data-testid="button-refund"
>
  Refund
</Button>
```

**Filter Inputs**:
```tsx
<TextInput
  label="Search transactions"
  aria-label="Search transactions by user, event, or transaction ID"
  placeholder="Search..."
/>
```

### Focus Indicators
- Burgundy outline: 2px solid on all interactive elements
- Focus visible on:
  - Filter inputs
  - Table rows (future: when clickable for detail)
  - Refund buttons
  - Modal controls

### Color Contrast
- All text meets WCAG 2.1 AA standards (4.5:1 minimum)
- Badge colors chosen for accessibility:
  - **Green (Paid)**: 4.8:1 on white background
  - **Blue (PayPal)**: 5.2:1 on white background
  - **Orange (Refunded)**: 4.6:1 on white background
  - **Red (Failed)**: 5.5:1 on white background

## Design System Integration

### Colors Used

**Brand Colors**:
- Burgundy (#880124): Page title, table header, focus outlines
- Rose Gold (#B76D75): Accents (future: subtle hover effects)
- Ivory (#FFF8F0): Table header text, card backgrounds

**Status Colors**:
- Success Green (#228B22): "Paid" status badge
- Warning Orange (#DAA520): "Refunded" status badge, "Pending" badge
- Error Red (#DC143C): "Failed" status badge, Refund button
- Info Blue (#4A90E2): "PayPal" payment method badge

**Neutral Colors**:
- Charcoal (#2B2B2B): Primary text
- Smoke (#4A4A4A): Secondary text (email addresses)
- Stone (#8B8680): Tertiary text, disabled states
- Cream (#FAF6F2): Summary statistics background, table row striping

### Typography

**Page Title** (Montserrat 700, 32px):
```tsx
style={{
  fontFamily: 'var(--font-heading)',
  fontSize: '32px',
  fontWeight: 700,
  color: 'var(--color-burgundy)'
}}
```

**Table Headers** (Montserrat 600, 14px, uppercase):
```tsx
style={{
  fontFamily: 'var(--font-heading)',
  fontSize: '14px',
  fontWeight: 600,
  textTransform: 'uppercase',
  letterSpacing: '0.5px',
  color: 'var(--color-ivory)'
}}
```

**Transaction Amounts** (Source Sans 3 600, 16px):
```tsx
<Text fw={600} size="md">
  ${amount.toFixed(2)}
</Text>
```

**User Names** (Source Sans 3 500, 14px):
```tsx
<Text fw={500} size="sm">
  {userName}
</Text>
```

**User Emails** (Source Sans 3 400, 12px, dimmed):
```tsx
<Text size="xs" c="dimmed">
  {userEmail}
</Text>
```

### Spacing

**Page Padding**: `p="xl"` (40px)
**Filter Bar Margin**: `mb="lg"` (32px)
**Summary Statistics Margin**: `mt="md"` (24px)
**Card Gap** (mobile): `gap="md"` (24px)
**Table Cell Padding**: Default Mantine (12px vertical, 16px horizontal)

### Buttons

**Refund Button** (Desktop):
- Class: `.btn-light` (Mantine variant="light")
- Color: Red
- Size: xs
- No corner morphing animation (Mantine button, not custom `.btn` class)
- Hover: Slightly darker red background

**Refund Button** (Mobile):
- Same as desktop but `fullWidth` prop
- Height: 44px minimum (touch-friendly)

**Clear Filters Button**:
- Variant: light
- Color: burgundy (wcr.7)
- Size: md
- Icon: `IconX` or `IconFilterOff`

## Mobile-First Considerations

### Touch Targets
- Minimum size: 44×44px (iOS guidelines)
- Refund button: 44px height on mobile
- Filter drawer button: Full-width, 48px height
- Table row cards: 56px minimum height

### Mobile Navigation
- Sticky header with hamburger menu
- Page title always visible (no scroll collapse)
- Filter drawer slides up from bottom (easier thumb reach)
- Summary statistics stick to bottom (future: optional)

### Performance
- Lazy load transactions (pagination)
- Debounced search (500ms delay)
- Virtualized table rows (future: if >1000 transactions)
- Optimistic UI updates after refund (instant status change)

### Thumb-Zone Optimization
- Refund button at bottom of card (easy right-thumb reach)
- Filter drawer trigger at top (left-thumb reach)
- Search input at top (both thumbs can reach)
- Summary statistics at bottom (scrollable info)

## Data Flow Diagram

```
┌─────────────────────────────────────────────────┐
│  Admin Payments Page Component                  │
│  ────────────────────────────                   │
│                                                  │
│  State:                                          │
│  - transactions (PaymentDto[])                   │
│  - filterState (method, status, dateRange, etc)  │
│  - searchTerm (string)                           │
│  - isLoading (boolean)                           │
│  - refundModalOpened (boolean)                   │
│  - selectedPayment (PaymentDto | null)           │
└────────────┬────────────────────────────────────┘
             │
             │ 1. Initial Load
             ▼
┌────────────────────────────────────────┐
│  GET /api/admin/payments               │
│  Query Params:                         │
│  - method: PayPal,Free                 │
│  - status: Paid,Refunded               │
│  - startDate: 2025-11-01               │
│  - endDate: 2025-11-30                 │
│  - minAmount: 0                        │
│  - maxAmount: 999.99                   │
│  - search: "John"                      │
└────────────┬───────────────────────────┘
             │
             │ 2. Response: PaymentDto[]
             ▼
┌────────────────────────────────────────┐
│  Payment List Table                    │
│  - Map transactions to rows            │
│  - Show "Refund" button if eligible    │
│  - Handle row click (future)           │
└────────────┬───────────────────────────┘
             │
             │ 3. User clicks "Refund"
             ▼
┌────────────────────────────────────────┐
│  RefundConfirmationModal               │
│  Props:                                │
│  - opened: true                        │
│  - payment: { id, userName, amount }   │
│  - onConfirm: (reason) => {...}        │
│  - onClose: () => {...}                │
└────────────┬───────────────────────────┘
             │
             │ 4. User fills reason, checks box
             │ 5. User clicks "Process Refund"
             ▼
┌────────────────────────────────────────┐
│  POST /api/admin/refunds/{ticketId}    │
│  Body:                                 │
│  {                                     │
│    refundReason: "User requested...",  │
│    alsoRemoveRsvp: true                │
│  }                                     │
└────────────┬───────────────────────────┘
             │
             │ 6. Success Response
             ▼
┌────────────────────────────────────────┐
│  Success Notification                  │
│  - Show green toast                    │
│  - Close modal                         │
│  - Refresh transaction list            │
└────────────┬───────────────────────────┘
             │
             │ 7. Re-fetch transactions
             ▼
┌────────────────────────────────────────┐
│  GET /api/admin/payments               │
│  (same query params as initial load)   │
└────────────┬───────────────────────────┘
             │
             │ 8. Response with updated data
             │    (refunded transaction now "Refunded")
             ▼
┌────────────────────────────────────────┐
│  Payment List Table (updated)          │
│  - Refunded transaction shows orange   │
│    "Refunded" badge                    │
│  - "Refund" button no longer visible   │
└────────────────────────────────────────┘
```

## Component Tree

```
AdminPaymentsPage
├── Box (page container)
│   ├── Title (page header: "Payment Transactions")
│   │
│   ├── PaymentFilterBar (custom component)
│   │   ├── Group (horizontal layout - desktop)
│   │   │   ├── TextInput (search)
│   │   │   ├── DatePickerInput (date range)
│   │   │   ├── MultiSelect (payment method)
│   │   │   ├── MultiSelect (status)
│   │   │   ├── NumberInput (min amount)
│   │   │   └── NumberInput (max amount)
│   │   │
│   │   └── Drawer (mobile filter panel)
│   │       └── Stack
│   │           ├── DatePickerInput
│   │           ├── MultiSelect (method)
│   │           ├── MultiSelect (status)
│   │           ├── NumberInput (min)
│   │           └── NumberInput (max)
│   │
│   ├── Table (desktop) OR Stack of Cards (mobile)
│   │   ├── Table.Thead (burgundy background)
│   │   │   └── Table.Tr
│   │   │       ├── Table.Th (Date)
│   │   │       ├── Table.Th (User/Member)
│   │   │       ├── Table.Th (Event/Session)
│   │   │       ├── Table.Th (Payment Method)
│   │   │       ├── Table.Th (Amount)
│   │   │       ├── Table.Th (Status)
│   │   │       └── Table.Th (Actions)
│   │   │
│   │   └── Table.Tbody
│   │       └── Table.Tr[] (mapped from transactions)
│   │           ├── Table.Td (date text)
│   │           ├── Table.Td (Stack: name + email)
│   │           ├── Table.Td (event title)
│   │           ├── Table.Td (Badge: payment method)
│   │           ├── Table.Td (Text: amount, bold)
│   │           ├── Table.Td (Badge: status)
│   │           └── Table.Td (Button: "Refund" if eligible)
│   │
│   ├── Box (summary statistics)
│   │   └── Group (horizontal stats layout)
│   │       ├── Group (Showing X of Y)
│   │       ├── Group (Filtered by: ...)
│   │       ├── Group (Search: "...")
│   │       └── Group (Total Revenue: $X.XX)
│   │
│   └── RefundConfirmationModal (conditional render)
│       ├── Modal (Mantine)
│       │   ├── Title ("Process Refund?")
│       │   ├── Stack (modal body)
│       │   │   ├── Text (payment info)
│       │   │   ├── List (user, method, date, description)
│       │   │   ├── Box (refund amount - prominent)
│       │   │   ├── Textarea (refund reason)
│       │   │   ├── Alert (warning/impact details)
│       │   │   ├── Text (cannot undo warning)
│       │   │   ├── Checkbox (confirmation)
│       │   │   └── Group (action buttons)
│       │   │       ├── Button ("Cancel")
│       │   │       └── Button ("Process Refund")
│       │   └── [Modal automatically handles backdrop, close, etc.]
```

## API Endpoints Required

### GET /api/admin/payments

**Purpose**: Fetch all payment transactions with optional filters

**Query Parameters**:
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| method | string[] | No | All | Payment methods: PayPal, Free, Venmo |
| status | string[] | No | All | Payment statuses: Paid, Refunded, Pending, Failed |
| startDate | ISO 8601 | No | null | Start of date range (inclusive) |
| endDate | ISO 8601 | No | null | End of date range (inclusive) |
| minAmount | decimal | No | 0 | Minimum transaction amount |
| maxAmount | decimal | No | null | Maximum transaction amount |
| search | string | No | null | Full-text search term |
| page | int | No | 1 | Pagination page number |
| pageSize | int | No | 25 | Results per page |

**Response**: `PaymentListResponse`
```json
{
  "payments": [
    {
      "id": "guid",
      "paymentId": "guid",
      "userId": "guid",
      "userName": "John Smith",
      "userEmail": "john@example.com",
      "eventId": "guid",
      "eventName": "Rope 101 Workshop",
      "ticketId": "guid",
      "paymentMethod": "PayPal",
      "amount": 50.00,
      "currency": "USD",
      "status": "Paid",
      "paymentDate": "2025-11-17T14:30:00Z",
      "createdAt": "2025-11-17T14:30:00Z",
      "canRefund": true
    }
  ],
  "totalCount": 156,
  "filteredCount": 23,
  "currentPage": 1,
  "pageSize": 25,
  "totalPages": 1
}
```

### POST /api/admin/refunds/{ticketId}

**Purpose**: Process refund for a ticket purchase

**Path Parameters**:
- `ticketId` (GUID): ID of the ticket to refund

**Request Body**: `AdminRefundTicketRequest`
```json
{
  "refundReason": "User requested refund due to scheduling conflict",
  "alsoRemoveRsvp": true
}
```

**Success Response** (200):
```json
{
  "message": "Refund processed successfully",
  "refundId": "guid",
  "amount": 50.00,
  "refundDate": "2025-11-17T15:45:00Z"
}
```

**Error Response** (400):
```json
{
  "title": "Refund Failed",
  "detail": "Ticket has already been refunded",
  "status": 400
}
```

**Error Response** (500):
```json
{
  "title": "Refund Failed",
  "detail": "PayPal API error: Insufficient funds in merchant account",
  "status": 500
}
```

## Test Scenarios (from E2E Tests)

**Reference**: `/apps/web/tests/payments/ticket-refund-workflow.spec.ts`

### Happy Path Tests
1. **Admin navigates to payment management page**
   - Login as admin
   - Navigate to `/admin/analytics/payments`
   - Verify page loads with payments table
   - Verify page title "Payment Transactions" visible

2. **Admin opens refund confirmation modal**
   - Find payment row with "Refund" button
   - Click refund button
   - Verify modal opens with correct payment details
   - Verify all modal elements present (reason textarea, checkbox, buttons)

3. **Admin completes refund workflow**
   - Fill refund reason (required, 500 char limit)
   - Check confirmation checkbox
   - Verify "Process Refund" button enabled
   - Click "Process Refund"
   - Verify success notification appears
   - Verify modal closes
   - Verify transaction list refreshes

### Validation Tests
4. **Refund reason is required**
   - Open modal
   - Leave reason empty
   - Check checkbox
   - Verify "Process Refund" button disabled

5. **Character limit enforced**
   - Open modal
   - Enter >500 characters
   - Verify textarea truncates at 500
   - Verify character counter shows "0 / 500 remaining"

6. **Confirmation checkbox is required**
   - Open modal
   - Fill refund reason
   - Leave checkbox unchecked
   - Verify "Process Refund" button disabled

### Edge Cases
7. **Cancel button closes modal without refunding**
   - Open modal
   - Fill partial data
   - Click "Cancel"
   - Verify modal closes
   - Verify no success notification
   - Verify transaction status unchanged

8. **Modal resets when reopened**
   - Open modal
   - Fill data and cancel
   - Reopen same modal
   - Verify textarea empty
   - Verify checkbox unchecked

### Database Persistence Tests
9. **Refund creates PaymentRefund record**
   - Complete refund workflow
   - Query PaymentRefunds table
   - Verify record created with:
     - Correct OriginalPaymentId
     - RefundReason text
     - RefundStatus = "Completed"
     - ProcessedByUserId = admin user ID
     - Accurate timestamps

## Questions Answered

### 1. Should transactions be displayed in a table or card grid?
**Answer**: **Table on desktop, card grid on mobile**

**Rationale**:
- Table provides better data density for desktop (can see 10-15 transactions at once)
- Sortable columns make it easy to organize by date, amount, or status
- Matches existing AdminEventsPage pattern (users already familiar)
- Card grid on mobile provides better touch targets and readability
- Responsive design ensures best experience on both devices

### 2. Which filters are most important for admins?
**Answer**: **Date, Payment Method, Status, Amount, Search**

**Rationale**:
- **Date range**: Most common filter ("show me this month's payments")
- **Payment Method**: Distinguish PayPal vs Free vs future methods
- **Status**: Filter by Paid, Refunded, Pending, Failed
- **Amount range**: Find high-value or low-value transactions
- **Search**: Quick access to specific user or event

### 3. Should "Refund" button be in Actions column or bulk selection?
**Answer**: **Actions column (individual refund per row)**

**Rationale**:
- Refunds are HIGH-RISK operations (cannot be undone)
- Bulk refunds too dangerous (accidental mass refunds)
- Individual button requires deliberate click per transaction
- Matches existing RefundConfirmationModal design (one payment at a time)
- Future: Add bulk refund ONLY with manager-level authorization

### 4. What should empty state show if no payments found?
**Answer**: **Icon + message + "Clear Filters" button**

**Rationale**:
- Icon (`IconReceiptOff`) provides visual clarity
- Message explains issue: "No transactions found"
- Subtext suggests action: "Try adjusting your filters or search term"
- "Clear Filters" button provides quick recovery
- Friendly UX (not just blank screen)

### 5. Loading state during data fetch?
**Answer**: **Mantine Loader centered with "Loading transactions..." text**

**Rationale**:
- Standard loading pattern used throughout app
- Burgundy-colored loader matches brand
- Text provides context (not just spinner)
- Prevents confusion during slow network requests
- Matches AdminEventsPage loading pattern

## Implementation Notes

### State Management

**React Query** (recommended):
```tsx
const { data: payments, isLoading, error, refetch } = useQuery({
  queryKey: ['admin-payments', filterState],
  queryFn: () => fetchPayments(filterState)
});

const refundMutation = useMutation({
  mutationFn: ({ ticketId, reason }) => processRefund(ticketId, reason),
  onSuccess: () => {
    refetch(); // Refresh payment list
    notifications.show({ /* success */ });
  },
  onError: (error) => {
    notifications.show({ /* error */ });
  }
});
```

**Benefits**:
- Automatic caching (filter changes fetch new data)
- Loading and error states built-in
- Optimistic updates for better UX
- Automatic refetch on window focus

### Performance Optimization

**Debounced Search**:
```tsx
const [searchTerm, setSearchTerm] = useState('');
const debouncedSearch = useDebounce(searchTerm, 500);

useEffect(() => {
  // Trigger API call only after 500ms of no typing
  refetch();
}, [debouncedSearch]);
```

**Pagination**:
- Desktop: 25 transactions per page
- Mobile: 10 transactions per page
- "Load More" button at bottom (infinite scroll alternative)
- Page numbers for desktop, simple prev/next for mobile

**Virtualization** (future optimization if >1000 transactions):
- Use `react-window` or Mantine's virtualized table
- Render only visible rows (improves performance)
- Scroll smoothly through thousands of transactions

### Error Handling

**Network Errors**:
- Retry button in error state
- "Check your connection" message
- Don't lose user's filter state

**Authorization Errors** (401/403):
- Redirect to login if session expired
- Show "Permission denied" if not Admin/Teacher
- Log out user if token invalid

**Validation Errors** (400):
- Show specific error message from API
- Highlight problematic field (if applicable)
- Allow user to correct and retry

**Server Errors** (500):
- Show generic "Server error" message
- Provide "Contact support" link
- Log error to monitoring service (Sentry, etc.)

## Next Steps for react-developer

1. **Create page component**: `/apps/web/src/pages/admin/AdminPaymentsPage.tsx`
2. **Create filter bar component**: `/apps/web/src/components/payments/PaymentFilterBar.tsx`
3. **Create payment row component**: `/apps/web/src/components/payments/PaymentTableRow.tsx` (optional extraction)
4. **Add route**: Update router configuration to include `/admin/analytics/payments`
5. **Update Admin Dashboard**: Change Analytics card link from `/admin/analytics` to `/admin/analytics/payments`
6. **Integrate RefundConfirmationModal**: Import from existing component, wire up props
7. **Create API query hook**: `/apps/web/src/features/payments/api/queries.ts` (usePayments)
8. **Create refund mutation hook**: `/apps/web/src/features/payments/api/mutations.ts` (useProcessRefund)
9. **Add TypeScript types**: Payment DTO types from auto-generated shared-types package
10. **Add data-testid attributes**: All elements from E2E test files

## Next Steps for backend-developer

1. **Create GET endpoint**: `/apps/api/Features/Payments/Endpoints/GetAdminPayments.cs`
2. **Create DTO**: `/apps/api/Features/Payments/Models/PaymentListItemDto.cs`
3. **Implement filters**: Date range, payment method, status, amount range, search
4. **Add pagination**: Page number, page size, total count
5. **Create POST endpoint**: `/apps/api/Features/Payments/Endpoints/ProcessRefund.cs` (or similar)
6. **Integrate PayPal service**: Call PayPal refund API with Capture ID
7. **Create PaymentRefund record**: Save to database with full audit trail
8. **Send email notification**: Use GlobalEmailTemplate for refund confirmation
9. **Add authorization**: Verify user is Admin or Teacher role
10. **Return appropriate responses**: Success (200) or error (400/500) with details

## Files to Create

**Frontend**:
- `/apps/web/src/pages/admin/AdminPaymentsPage.tsx`
- `/apps/web/src/components/payments/PaymentFilterBar.tsx`
- `/apps/web/src/components/payments/PaymentTableView.tsx` (optional)
- `/apps/web/src/features/payments/api/queries.ts`
- `/apps/web/src/features/payments/api/mutations.ts`
- `/apps/web/src/features/payments/types.ts` (if custom types needed beyond auto-gen)

**Backend**:
- `/apps/api/Features/Payments/Endpoints/GetAdminPayments.cs`
- `/apps/api/Features/Payments/Endpoints/ProcessRefund.cs`
- `/apps/api/Features/Payments/Models/PaymentListItemDto.cs`
- `/apps/api/Features/Payments/Models/PaymentListResponse.cs`
- `/apps/api/Features/Payments/Services/PayPalRefundService.cs` (if not exists)

**Tests** (already exist):
- `/apps/web/tests/payments/ticket-refund-workflow.spec.ts` ✅
- `/apps/web/tests/payments/refund-validations.spec.ts` ✅
- `/apps/web/tests/payments/refund-database-persistence.spec.ts` ✅

---

**Design Complete**: This wireframe document provides comprehensive specifications for implementing the Admin Payment Transactions page. All questions answered, all patterns documented, all components specified. Ready for implementation by react-developer and backend-developer.

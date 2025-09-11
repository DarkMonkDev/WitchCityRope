# Data Grid Analysis: Mantine v7 Options for Events Management

<!-- Last Updated: 2025-08-24 -->
<!-- Version: 1.0 -->
<!-- Owner: UI Designer Agent -->
<!-- Status: Ready for Development -->

## Overview

Analysis of data grid solutions for the Events Management system, focusing on the admin events list, check-in interface, and attendee management tables shown in the existing wireframes.

## Requirements from Wireframes

### Admin Events Management Table
- **Columns**: Event Name (with type badge), Date, Instructor, Capacity, Status, Actions
- **Features**: Sorting, filtering, search, pagination
- **Actions**: Edit, Check-in, More options (3-dot menu)
- **Status Indicators**: Published, Draft, Cancelled badges
- **Capacity Display**: "12/20" format with waitlist notation
- **Row Actions**: Multiple action buttons per row

### Check-in Interface Table
- **Columns**: Name (with pronouns & badges), Payment Status, Waiver Status, Check-in Status
- **Features**: Real-time updates, search, filtering, sorting
- **Interactive Elements**: Check-in buttons that update state
- **Complex Cell Content**: Multi-line name display, status badges
- **Dynamic Updates**: Stats update when check-ins occur

### Event Creation - Tickets/Orders Table
- **Columns**: Order #, Attendee, Ticket Type, Status, Date, Amount, Actions
- **Features**: Order management, status updates
- **Actions**: View order details

## Data Grid Options Analysis

### Option 1: Mantine DataTable (Recommended) ⭐

**Package**: `mantine-datatable`
**Pros**:
- ✅ Built specifically for Mantine ecosystem
- ✅ Excellent TypeScript support
- ✅ Built-in sorting, filtering, pagination
- ✅ Row selection and bulk operations
- ✅ Custom cell renderers
- ✅ Responsive design support
- ✅ Real-time data updates
- ✅ Loading states and empty states
- ✅ Keyboard navigation
- ✅ Accessibility compliant

**Cons**:
- ⚠️ Additional package dependency
- ⚠️ Learning curve for advanced features

**Code Example:**
```tsx
import { DataTable, DataTableSortStatus } from 'mantine-datatable';

const EventsDataTable = () => {
  const [sortStatus, setSortStatus] = useState<DataTableSortStatus>({
    columnAccessor: 'date',
    direction: 'asc',
  });

  return (
    <DataTable
      withTableBorder
      withColumnBorders
      striped
      highlightOnHover
      records={events}
      columns={[
        {
          accessor: 'title',
          title: 'Event Name',
          sortable: true,
          render: ({ title, type }) => (
            <Group gap="sm">
              <Text fw={500}>{title}</Text>
              <Badge size="xs" color={type === 'class' ? 'wcr.6' : 'blue'}>
                {type}
              </Badge>
            </Group>
          ),
        },
        {
          accessor: 'date',
          title: 'Date',
          sortable: true,
          render: ({ date }) => dayjs(date).format('MMM D, YYYY'),
        },
        {
          accessor: 'instructor',
          title: 'Instructor',
          sortable: true,
        },
        {
          accessor: 'capacity',
          title: 'Capacity',
          render: ({ registered, total, waitlist }) => (
            <Text>
              {registered}/{total}
              {waitlist > 0 && (
                <Text span c="orange" size="sm">
                  {' '}({waitlist} waitlist)
                </Text>
              )}
            </Text>
          ),
        },
        {
          accessor: 'status',
          title: 'Status',
          render: ({ status }) => (
            <Badge
              color={
                status === 'published' ? 'green' : 
                status === 'draft' ? 'yellow' : 'red'
              }
              size="sm"
            >
              {status}
            </Badge>
          ),
        },
        {
          accessor: 'actions',
          title: 'Actions',
          textAlign: 'center',
          render: (event) => (
            <Group gap="xs" justify="center">
              <ActionIcon 
                variant="light" 
                color="blue"
                onClick={() => handleEdit(event.id)}
              >
                <IconEdit size="1rem" />
              </ActionIcon>
              <ActionIcon 
                variant="light" 
                color="green"
                onClick={() => handleCheckin(event.id)}
              >
                <IconClipboard size="1rem" />
              </ActionIcon>
              <Menu>
                <Menu.Target>
                  <ActionIcon variant="light" color="gray">
                    <IconDots size="1rem" />
                  </ActionIcon>
                </Menu.Target>
                <Menu.Dropdown>
                  <Menu.Item leftSection={<IconCopy size="0.9rem" />}>
                    Duplicate
                  </Menu.Item>
                  <Menu.Item leftSection={<IconTrash size="0.9rem" />} color="red">
                    Delete
                  </Menu.Item>
                </Menu.Dropdown>
              </Menu>
            </Group>
          ),
        },
      ]}
      sortStatus={sortStatus}
      onSortStatusChange={setSortStatus}
      page={page}
      recordsPerPage={recordsPerPage}
      totalRecords={totalRecords}
      onPageChange={setPage}
      recordsPerPageOptions={[10, 20, 50]}
      onRecordsPerPageChange={setRecordsPerPage}
      noRecordsText="No events found"
      loadingText="Loading events..."
    />
  );
};
```

### Option 2: Mantine React Table

**Package**: `@tanstack/react-table` + Mantine components
**Pros**:
- ✅ Extremely powerful and flexible
- ✅ Excellent performance for large datasets
- ✅ Advanced filtering and sorting
- ✅ Column resizing, reordering
- ✅ Virtual scrolling support
- ✅ Grouping and aggregation

**Cons**:
- ❌ Much more complex setup
- ❌ Requires more boilerplate
- ❌ Steeper learning curve
- ❌ Not specifically designed for Mantine

**Code Example:**
```tsx
import { useReactTable, getCoreRowModel, getSortedRowModel } from '@tanstack/react-table';

const EventsReactTable = () => {
  const columns = useMemo(() => [
    {
      accessorKey: 'title',
      header: 'Event Name',
      cell: ({ row }) => (
        <Group gap="sm">
          <Text fw={500}>{row.original.title}</Text>
          <Badge size="xs" color={row.original.type === 'class' ? 'wcr.6' : 'blue'}>
            {row.original.type}
          </Badge>
        </Group>
      ),
    },
    // More columns...
  ], []);

  const table = useReactTable({
    data: events,
    columns,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
  });

  return (
    <Table>
      <Table.Thead>
        {table.getHeaderGroups().map(headerGroup => (
          <Table.Tr key={headerGroup.id}>
            {headerGroup.headers.map(header => (
              <Table.Th
                key={header.id}
                onClick={header.column.getToggleSortingHandler()}
                style={{ cursor: 'pointer' }}
              >
                {header.isPlaceholder ? null : flexRender(
                  header.column.columnDef.header,
                  header.getContext()
                )}
              </Table.Th>
            ))}
          </Table.Tr>
        ))}
      </Table.Thead>
      <Table.Tbody>
        {table.getRowModel().rows.map(row => (
          <Table.Tr key={row.id}>
            {row.getVisibleCells().map(cell => (
              <Table.Td key={cell.id}>
                {flexRender(cell.column.columnDef.cell, cell.getContext())}
              </Table.Td>
            ))}
          </Table.Tr>
        ))}
      </Table.Tbody>
    </Table>
  );
};
```

### Option 3: Native Mantine Table

**Package**: Built into `@mantine/core`
**Pros**:
- ✅ No additional dependencies
- ✅ Simple and lightweight
- ✅ Perfect Mantine integration
- ✅ Easy to customize

**Cons**:
- ❌ No built-in sorting/filtering
- ❌ No pagination built-in
- ❌ Manual implementation of advanced features
- ❌ Not suitable for complex data operations

**Code Example:**
```tsx
const SimpleEventsTable = () => (
  <Table striped highlightOnHover withTableBorder>
    <Table.Thead>
      <Table.Tr>
        <Table.Th>Event Name</Table.Th>
        <Table.Th>Date</Table.Th>
        <Table.Th>Instructor</Table.Th>
        <Table.Th>Capacity</Table.Th>
        <Table.Th>Status</Table.Th>
        <Table.Th>Actions</Table.Th>
      </Table.Tr>
    </Table.Thead>
    <Table.Tbody>
      {events.map((event) => (
        <Table.Tr key={event.id}>
          <Table.Td>
            <Group gap="sm">
              <Text fw={500}>{event.title}</Text>
              <Badge size="xs" color={event.type === 'class' ? 'wcr.6' : 'blue'}>
                {event.type}
              </Badge>
            </Group>
          </Table.Td>
          <Table.Td>{dayjs(event.date).format('MMM D, YYYY')}</Table.Td>
          <Table.Td>{event.instructor}</Table.Td>
          <Table.Td>{event.registered}/{event.total}</Table.Td>
          <Table.Td>
            <Badge color={getStatusColor(event.status)} size="sm">
              {event.status}
            </Badge>
          </Table.Td>
          <Table.Td>
            <Group gap="xs">
              <ActionIcon variant="light" color="blue">
                <IconEdit size="1rem" />
              </ActionIcon>
              <ActionIcon variant="light" color="green">
                <IconClipboard size="1rem" />
              </ActionIcon>
            </Group>
          </Table.Td>
        </Table.Tr>
      ))}
    </Table.Tbody>
  </Table>
);
```

## Specific Use Case Recommendations

### 1. Admin Events Management Table
**Recommendation**: Mantine DataTable
**Reason**: Perfect balance of features and complexity. Handles all requirements from wireframe.

**Key Features Needed**:
```tsx
const AdminEventsTable = () => (
  <DataTable
    // Essential features for admin management
    withTableBorder
    withColumnBorders
    striped
    highlightOnHover
    
    // Data and sorting
    records={events}
    sortStatus={sortStatus}
    onSortStatusChange={setSortStatus}
    
    // Pagination
    page={page}
    recordsPerPage={20}
    totalRecords={totalEvents}
    onPageChange={setPage}
    
    // Column definitions with complex renderers
    columns={adminEventsColumns}
    
    // Loading and empty states
    fetching={isLoading}
    noRecordsText="No events found"
    
    // Row selection for bulk operations
    selectedRecords={selectedEvents}
    onSelectedRecordsChange={setSelectedEvents}
  />
);
```

### 2. Check-in Interface Table
**Recommendation**: Mantine DataTable with Real-time Updates
**Reason**: Need real-time updates and interactive buttons that change state.

**Key Features Needed**:
```tsx
const CheckinTable = () => {
  const checkInMutation = useMutation({
    mutationFn: (attendeeId: string) => checkInAttendee(attendeeId),
    onSuccess: () => {
      queryClient.invalidateQueries(['event-attendees']);
      // Update stats in real-time
    },
  });

  return (
    <DataTable
      records={attendees}
      columns={[
        {
          accessor: 'name',
          title: 'Name',
          render: ({ name, pronouns, memberType }) => (
            <Stack gap="xs">
              <Group gap="sm">
                <Text fw={500}>{name}</Text>
                {memberType && (
                  <Badge size="xs" color="gold">
                    {memberType}
                  </Badge>
                )}
              </Group>
              <Text size="sm" c="dimmed" fs="italic">
                {pronouns}
              </Text>
            </Stack>
          ),
        },
        {
          accessor: 'status',
          title: 'Status',
          render: ({ id, checkedIn }) => (
            checkedIn ? (
              <Group gap="xs">
                <IconCheck size="1rem" color="green" />
                <Text c="green" fw={500}>Checked In</Text>
              </Group>
            ) : (
              <Button
                size="sm"
                loading={checkInMutation.isLoading && checkInMutation.variables === id}
                onClick={() => checkInMutation.mutate(id)}
                gradient={{ from: 'yellow', to: 'orange' }}
                variant="gradient"
              >
                Check In
              </Button>
            )
          ),
        },
      ]}
      // Real-time updates
      key={`checkin-${Date.now()}`} // Force re-render on data changes
    />
  );
};
```

### 3. Tickets/Orders Table
**Recommendation**: Mantine DataTable (Simple Configuration)
**Reason**: Standard table functionality, less complex than other tables.

```tsx
const TicketsOrdersTable = () => (
  <DataTable
    records={orders}
    columns={[
      { accessor: 'orderNumber', title: 'Order #' },
      { accessor: 'attendeeName', title: 'Attendee' },
      { 
        accessor: 'ticketType', 
        title: 'Ticket Type',
        render: ({ ticketType, price }) => (
          <Stack gap="xs">
            <Text>{ticketType}</Text>
            <Text size="sm" c="dimmed">${price}</Text>
          </Stack>
        )
      },
      {
        accessor: 'status',
        title: 'Status',
        render: ({ status }) => (
          <Badge color={getOrderStatusColor(status)}>{status}</Badge>
        )
      },
      { 
        accessor: 'date', 
        title: 'Date',
        render: ({ date }) => dayjs(date).format('MMM D, YYYY')
      },
      { accessor: 'amount', title: 'Amount' },
      {
        accessor: 'actions',
        title: 'Actions',
        render: ({ id }) => (
          <Button size="xs" variant="light" onClick={() => viewOrder(id)}>
            View
          </Button>
        )
      },
    ]}
    withTableBorder
    highlightOnHover
  />
);
```

## Installation and Setup

### Mantine DataTable (Recommended)
```bash
npm install mantine-datatable
```

```tsx
// In your app root or layout
import 'mantine-datatable/styles.css';

// Optional: Custom CSS overrides
const mantineDataTableStyles = {
  root: {
    '& .mantine-datatable-header': {
      backgroundColor: 'var(--mantine-color-wcr-0)',
    },
    '& .mantine-datatable-row:hover': {
      backgroundColor: 'var(--mantine-color-wcr-1)',
    },
  },
};
```

### TypeScript Interfaces
```tsx
// Define interfaces that match API DTOs
interface Event {
  id: string;
  title: string;
  type: 'class' | 'meetup';
  date: Date;
  instructor: string;
  registered: number;
  total: number;
  waitlist: number;
  status: 'published' | 'draft' | 'cancelled';
}

interface Attendee {
  id: string;
  name: string;
  pronouns: string;
  memberType?: 'VIP' | 'Member' | 'Teacher' | 'Staff';
  paymentStatus: 'paid' | 'unpaid' | 'comp';
  waiverSigned: boolean;
  checkedIn: boolean;
}

interface Order {
  id: string;
  orderNumber: string;
  attendeeName: string;
  ticketType: string;
  status: 'confirmed' | 'pending' | 'cancelled';
  date: Date;
  amount: number;
}
```

## Performance Considerations

### Large Datasets
- Use pagination instead of virtualization for most cases
- Implement server-side sorting and filtering for 1000+ records
- Use React Query for caching and background updates

### Real-time Updates
- WebSocket integration for check-in interface
- Optimistic updates for better UX
- Background sync with conflict resolution

### Mobile Performance
```tsx
// Responsive column hiding
const columns = [
  { accessor: 'title', title: 'Event', hidden: { base: false } },
  { accessor: 'date', title: 'Date', hidden: { base: false } },
  { accessor: 'instructor', title: 'Instructor', hidden: { base: true, sm: false } },
  { accessor: 'capacity', title: 'Capacity', hidden: { base: true, md: false } },
  { accessor: 'status', title: 'Status', hidden: { base: false } },
  { accessor: 'actions', title: 'Actions', hidden: { base: false } },
];
```

## Final Recommendation: Mantine DataTable

**Primary Choice**: `mantine-datatable` package
**Fallback**: Native Mantine Table for simple cases

**Justification**:
1. **Perfect Feature Match**: Handles all requirements from wireframes
2. **Mantine Integration**: Built specifically for Mantine ecosystem
3. **TypeScript Support**: Excellent type safety and IntelliSense
4. **Performance**: Good balance of features and performance
5. **Maintenance**: Actively maintained and well-documented
6. **Learning Curve**: Reasonable complexity for development team

**Implementation Plan**:
1. Start with Mantine DataTable for admin tables
2. Use same component for check-in interface with real-time features
3. Implement responsive design with column hiding
4. Add server-side pagination for large datasets
5. Integrate with React Query for data management

The existing wireframes provide excellent patterns that translate directly to DataTable configurations, making this the ideal choice for rapid development while maintaining the design integrity.
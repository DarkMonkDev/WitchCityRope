import React from 'react';
import { Table, ActionIcon, Button, Text, Group, Skeleton, Badge } from '@mantine/core';
import { IconCaretUp, IconCaretDown, IconSelector } from '@tabler/icons-react';
import { notifications } from '@mantine/notifications';
import { useNavigate } from 'react-router-dom';
import type { EventDto } from '@witchcityrope/shared-types';
import { CapacityDisplay } from './CapacityDisplay';
import type { AdminEventFiltersState } from '../../hooks/useAdminEventFilters';

interface EventsTableViewProps {
  events: EventDto[];
  sortState: Pick<AdminEventFiltersState, 'sortColumn' | 'sortDirection'>;
  onSort: (column: 'date' | 'title') => void;
  onCopyEvent?: (eventId: string) => void;
  isLoading?: boolean;
}

// Helper function to format event dates with robust field handling
const formatEventDate = (event: EventDto): string => {
  // Use the correct field name from generated EventDto type
  const dateString = event.startDate || '';
  
  if (!dateString) {
    console.warn('No startDate field found for event:', { 
      id: event.id, 
      title: event.title
    });
    return 'Date TBD';
  }
  
  const date = new Date(dateString);
  
  // Check if date is valid
  if (isNaN(date.getTime())) {
    console.error('Invalid date for event:', { eventId: event.id, dateString });
    return 'Invalid Date';
  }
  
  return date.toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric'
  });
};

// Helper function to format time range with robust field handling
const formatTimeRange = (event: EventDto): string => {
  // Use the correct field names from generated EventDto type
  const startDateString = event.startDate || '';
  const endDateString = event.endDate || '';
  
  if (!startDateString) return 'Time TBD';
  
  const start = new Date(startDateString);
  
  // Check if start date is valid
  if (isNaN(start.getTime())) {
    console.error('Invalid start date for event:', { eventId: event.id, startDateString });
    return 'Invalid Time';
  }
  
  const formatTime = (date: Date) => {
    return date.toLocaleTimeString('en-US', {
      hour: 'numeric',
      minute: '2-digit',
      hour12: true
    });
  };
  
  // If no end date, assume 2-hour duration or show start time only
  if (!endDateString) {
    const end = new Date(start.getTime() + 2 * 60 * 60 * 1000); // Add 2 hours
    return `${formatTime(start)} - ${formatTime(end)}`;
  }
  
  const end = new Date(endDateString);
  
  // Check if end date is valid
  if (isNaN(end.getTime())) {
    console.warn('Invalid end date for event, using start time only:', { eventId: event.id, endDateString });
    return formatTime(start); // Just show start time if end date is invalid
  }
  
  return `${formatTime(start)} - ${formatTime(end)}`;
};

// Helper function to get the correct current count based on event type
const getCorrectCurrentCount = (event: EventDto): number => {
  const isSocialEvent = event.eventType?.toLowerCase() === 'social';
  return isSocialEvent ? (event.currentRSVPs || 0) : (event.currentTickets || 0);
};

// Helper function to get event type badge color
const getEventTypeBadgeColor = (eventType?: string | null): string => {
  if (!eventType) return 'gray';

  switch (eventType.toLowerCase()) {
    case 'workshop':
      return 'blue';
    case 'social':
      return 'green';
    case 'performance':
      return 'purple';
    default:
      return 'gray';
  }
};

// Helper function to get sort icon
const getSortIcon = (column: 'date' | 'title', sortState: Pick<AdminEventFiltersState, 'sortColumn' | 'sortDirection'>) => {
  if (sortState.sortColumn !== column) {
    return <IconSelector size={14} />;
  }

  return sortState.sortDirection === 'asc'
    ? <IconCaretUp size={14} />
    : <IconCaretDown size={14} />;
};

// Loading skeleton component
const EventsTableSkeleton: React.FC = () => (
  <Table>
    <Table.Thead bg="wcr.7">
      <Table.Tr>
        <Table.Th c="white" style={{ width: '160px' }}>Date</Table.Th>
        <Table.Th c="white" style={{ width: '120px', textAlign: 'center' }}>Type</Table.Th>
        <Table.Th c="white" style={{ minWidth: '200px' }}>Event Title</Table.Th>
        <Table.Th c="white" style={{ width: '200px', maxWidth: '200px', textAlign: 'center' }}>Time</Table.Th>
        <Table.Th c="white" style={{ width: '160px', maxWidth: '160px', textAlign: 'center' }}>Tickets/Capacity</Table.Th>
        <Table.Th c="white" style={{ width: '150px', textAlign: 'center' }}>Actions</Table.Th>
      </Table.Tr>
    </Table.Thead>
    <Table.Tbody>
      {Array.from({ length: 5 }).map((_, index) => (
        <Table.Tr key={index}>
          <Table.Td style={{ width: '160px' }}><Skeleton height={20} width="80%" /></Table.Td>
          <Table.Td style={{ width: '120px', textAlign: 'center' }}><Skeleton height={20} width="70%" /></Table.Td>
          <Table.Td style={{ minWidth: '200px' }}><Skeleton height={20} width="90%" /></Table.Td>
          <Table.Td style={{ width: '200px', maxWidth: '200px', textAlign: 'center' }}><Skeleton height={20} width="70%" /></Table.Td>
          <Table.Td style={{ width: '160px', maxWidth: '160px' }}>
            <Skeleton height={16} width="60%" mb={4} />
            <Skeleton height={8} width="100%" />
          </Table.Td>
          <Table.Td style={{ width: '150px', textAlign: 'center' }}><Skeleton height={28} width="60%" /></Table.Td>
        </Table.Tr>
      ))}
    </Table.Tbody>
  </Table>
);

/**
 * EventsTableView - Displays events in a sortable table with row-click navigation
 *
 * IMPORTANT: This table uses ROW-CLICK for editing (NOT an "Edit" button)
 *
 * User Flow:
 * 1. User clicks anywhere on a table row (except action buttons)
 * 2. handleRowClick() navigates to /admin/events/:id
 * 3. AdminEventDetailsPage loads with EventForm populated with event data
 * 4. User can edit all fields and save changes
 *
 * Action Buttons:
 * - "Copy" button: Duplicates event and opens edit page for the copy
 * - NO "Edit" button: Row click serves this purpose
 * - NO "Delete" button: Not yet implemented
 *
 * Testing Note:
 * Tests should click on table rows (e.g., page.locator('tr[data-testid="admin-event"]'))
 * NOT look for a separate edit button
 */
export const EventsTableView: React.FC<EventsTableViewProps> = ({
  events,
  sortState,
  onSort,
  onCopyEvent,
  isLoading = false
}) => {
  const navigate = useNavigate();

  /**
   * handleRowClick - Primary interaction for viewing/editing events
   * Clicking any row navigates to the event detail/edit page
   * This is the ONLY way to edit events (no separate edit button)
   */
  const handleRowClick = (eventId: string) => {
    navigate(`/admin/events/${eventId}`);
  };

  const handleCopyEvent = (eventId: string, event: React.MouseEvent) => {
    // Stop propagation to prevent row click
    event.stopPropagation();
    
    if (onCopyEvent) {
      onCopyEvent(eventId);
    } else {
      // Fallback notification if copy handler not provided
      notifications.show({
        title: 'Copy Event',
        message: 'Copy functionality will be implemented soon.',
        color: 'blue'
      });
    }
  };

  if (isLoading) {
    return <EventsTableSkeleton />;
  }

  if (events.length === 0) {
    return (
      <Table>
        <Table.Thead bg="wcr.7">
          <Table.Tr>
            <Table.Th c="white" style={{ width: '160px' }}>Date</Table.Th>
            <Table.Th c="white" style={{ width: '120px', textAlign: 'center' }}>Type</Table.Th>
            <Table.Th c="white" style={{ minWidth: '200px' }}>Event Title</Table.Th>
            <Table.Th c="white" style={{ width: '200px', maxWidth: '200px', textAlign: 'center' }}>Time</Table.Th>
            <Table.Th c="white" style={{ width: '160px', maxWidth: '160px', textAlign: 'center' }}>Tickets/Capacity</Table.Th>
            <Table.Th c="white" style={{ width: '150px', textAlign: '-webkit-center' as any }}>Actions</Table.Th>
          </Table.Tr>
        </Table.Thead>
        <Table.Tbody>
          <Table.Tr>
            <Table.Td colSpan={6} ta="center" py="xl">
              <Text c="dimmed" size="lg">No events found matching your filters</Text>
            </Table.Td>
          </Table.Tr>
        </Table.Tbody>
      </Table>
    );
  }

  return (
    <Table striped highlightOnHover data-testid="events-table">
      {/* Table Header */}
      <Table.Thead bg="wcr.7">
        <Table.Tr>
          {/* Sortable Date Column */}
          <Table.Th
            style={{ cursor: 'pointer', width: '160px' }}
            onClick={() => onSort('date')}
            c="white"
          >
            <Group gap="xs" justify="space-between">
              Date
              <ActionIcon variant="transparent" size="sm" c="white">
                {getSortIcon('date', sortState)}
              </ActionIcon>
            </Group>
          </Table.Th>

          {/* Type Column */}
          <Table.Th c="white" style={{ width: '120px', textAlign: 'center' }}>Type</Table.Th>

          {/* Sortable Title Column - Takes most space */}
          <Table.Th
            style={{ cursor: 'pointer', minWidth: '200px' }}
            onClick={() => onSort('title')}
            c="white"
          >
            <Group gap="xs" justify="space-between">
              Event Title
              <ActionIcon variant="transparent" size="sm" c="white">
                {getSortIcon('title', sortState)}
              </ActionIcon>
            </Group>
          </Table.Th>

          <Table.Th c="white" style={{ width: '200px', maxWidth: '200px', textAlign: 'center' }}>Time</Table.Th>
          <Table.Th c="white" style={{ width: '160px', maxWidth: '160px', textAlign: 'center' }}>Tickets/Capacity</Table.Th>
          <Table.Th c="white" style={{ width: '150px', textAlign: '-webkit-center' as any }}>Actions</Table.Th>
        </Table.Tr>
      </Table.Thead>
      
      {/* Table Body */}
      <Table.Tbody>
        {events.map(event => (
          <Table.Tr
            key={event.id}
            data-testid="event-row"
            style={{ cursor: 'pointer' }}
            onClick={() => handleRowClick(event.id)}
            // Row hover handled by Mantine's highlightOnHover prop
          >
            {/* Date Column */}
            <Table.Td style={{ width: '160px' }}>
              <Text fw={500} size="md">
                {formatEventDate(event)}
              </Text>
            </Table.Td>

            {/* Type Column */}
            <Table.Td style={{ width: '120px', textAlign: 'center' }}>
              <Badge
                color={getEventTypeBadgeColor(event.eventType)}
                variant="filled"
                size="sm"
              >
                {event.eventType || 'Other'}
              </Badge>
            </Table.Td>

            {/* Title Column - Takes most space */}
            <Table.Td style={{ minWidth: '200px' }}>
              <Text fw={600} c="wcr.7" size="md" lineClamp={2}>
                {event.title}{!event.isPublished ? ' - DRAFT' : ''}
              </Text>
            </Table.Td>

            {/* Time Column */}
            <Table.Td style={{ width: '200px', maxWidth: '200px', textAlign: 'center' }}>
              <Text size="md" c="dimmed">
                {formatTimeRange(event)}
              </Text>
            </Table.Td>

            {/* Capacity Column - Narrow */}
            <Table.Td style={{ width: '160px', maxWidth: '160px' }}>
              <CapacityDisplay
                current={getCorrectCurrentCount(event)}
                max={event.capacity}
              />
            </Table.Td>

            {/* Actions Column - Narrow and centered */}
            <Table.Td style={{ width: '150px', textAlign: '-webkit-center' as any }} onClick={(e) => e.stopPropagation()}>
              <Button
                variant="filled"
                color="wcr.7"
                data-testid="button-copy-event"
                onClick={(e) => handleCopyEvent(event.id, e)}
                styles={{
                  root: {
                    minWidth: '55px',
                    fontWeight: 600,
                    fontSize: '14px',
                    paddingLeft: '10px',
                    paddingRight: '10px',
                    height: '44px',
                    paddingTop: '12px',
                    paddingBottom: '12px',
                    lineHeight: '1.2',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center'
                  }
                }}
              >
                Copy
              </Button>
            </Table.Td>
          </Table.Tr>
        ))}
      </Table.Tbody>
    </Table>
  );
};
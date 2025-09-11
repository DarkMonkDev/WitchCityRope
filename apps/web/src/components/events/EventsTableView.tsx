import React from 'react';
import { Table, ActionIcon, Button, Text, Group, Skeleton } from '@mantine/core';
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

// Helper function to format event dates
const formatEventDate = (dateString: string): string => {
  const date = new Date(dateString);
  return date.toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric'
  });
};

// Helper function to format time range
const formatTimeRange = (startDateTime: string, endDateTime: string): string => {
  const start = new Date(startDateTime);
  const end = new Date(endDateTime);
  
  const formatTime = (date: Date) => {
    return date.toLocaleTimeString('en-US', {
      hour: 'numeric',
      minute: '2-digit',
      hour12: true
    });
  };
  
  return `${formatTime(start)} - ${formatTime(end)}`;
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
        <Table.Th c="white">Date</Table.Th>
        <Table.Th c="white">Event Title</Table.Th>
        <Table.Th c="white">Time</Table.Th>
        <Table.Th c="white">Capacity/Tickets</Table.Th>
        <Table.Th c="white">Actions</Table.Th>
      </Table.Tr>
    </Table.Thead>
    <Table.Tbody>
      {Array.from({ length: 5 }).map((_, index) => (
        <Table.Tr key={index}>
          <Table.Td><Skeleton height={20} width="80%" /></Table.Td>
          <Table.Td><Skeleton height={20} width="90%" /></Table.Td>
          <Table.Td><Skeleton height={20} width="70%" /></Table.Td>
          <Table.Td>
            <Skeleton height={16} width="60%" mb={4} />
            <Skeleton height={8} width="100%" />
          </Table.Td>
          <Table.Td><Skeleton height={28} width="60%" /></Table.Td>
        </Table.Tr>
      ))}
    </Table.Tbody>
  </Table>
);

export const EventsTableView: React.FC<EventsTableViewProps> = ({
  events,
  sortState,
  onSort,
  onCopyEvent,
  isLoading = false
}) => {
  const navigate = useNavigate();

  const handleRowClick = (eventId: string) => {
    // Navigate to event edit page (full page, not modal)
    navigate(`/admin/events/edit/${eventId}`);
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
            <Table.Th c="white">Date</Table.Th>
            <Table.Th c="white">Event Title</Table.Th>
            <Table.Th c="white">Time</Table.Th>
            <Table.Th c="white">Capacity/Tickets</Table.Th>
            <Table.Th c="white">Actions</Table.Th>
          </Table.Tr>
        </Table.Thead>
        <Table.Tbody>
          <Table.Tr>
            <Table.Td colSpan={5} ta="center" py="xl">
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
            style={{ cursor: 'pointer' }}
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
          
          {/* Sortable Title Column */}
          <Table.Th 
            style={{ cursor: 'pointer' }}
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
          
          <Table.Th c="white">Time</Table.Th>
          <Table.Th c="white">Capacity/Tickets</Table.Th>
          <Table.Th c="white">Actions</Table.Th>
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
            <Table.Td>
              <Text fw={500} size="sm">
                {formatEventDate(event.startDateTime || '')}
              </Text>
            </Table.Td>
            
            {/* Title Column */}
            <Table.Td>
              <Text fw={600} c="wcr.7" size="sm" lineClamp={2}>
                {event.title}
              </Text>
            </Table.Td>
            
            {/* Time Column */}
            <Table.Td>
              <Text size="sm" c="dimmed">
                {formatTimeRange(event.startDateTime || '', event.endDateTime || event.startDateTime || '')}
              </Text>
            </Table.Td>
            
            {/* Capacity Column */}
            <Table.Td>
              <CapacityDisplay 
                current={event.currentAttendees || 0} 
                max={event.capacity || 0} 
              />
            </Table.Td>
            
            {/* Actions Column */}
            <Table.Td onClick={(e) => e.stopPropagation()}>
              <Button
                size="xs"
                variant="light"
                color="wcr"
                data-testid="button-copy-event"
                onClick={(e) => handleCopyEvent(event.id, e)}
                styles={{
                  root: {
                    fontWeight: 600
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
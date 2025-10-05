import React from 'react';
import { Box, Title, Button, Group, Loader, Alert } from '@mantine/core';
import { IconPlus } from '@tabler/icons-react';
import { useNavigate } from 'react-router-dom';
import { notifications } from '@mantine/notifications';
import { useEvents } from '../../features/events/api/queries';
import { useCopyEvent } from '../../features/events/api/mutations';
import { useAdminEventFilters } from '../../hooks/useAdminEventFilters';
import { EventsFilterBar } from '../../components/events/EventsFilterBar';
import { EventsTableView } from '../../components/events/EventsTableView';

/**
 * AdminEventsPage - Table-based administrative interface for managing events
 *
 * ARCHITECTURE OVERVIEW:
 * This page uses a DEDICATED PAGE NAVIGATION pattern (NOT modals) for all CRUD operations:
 *
 * CREATE FLOW:
 * - Click "Create Event" button → navigates to /admin/events/new
 * - NewEventPage renders with empty EventForm
 * - User fills form and saves → navigates to /admin/events/:id (detail/edit view)
 *
 * READ/EDIT FLOW:
 * - Click any TABLE ROW → navigates to /admin/events/:id
 * - AdminEventDetailsPage renders with populated EventForm
 * - User can edit all fields and save changes
 * - NO separate "Edit" button - row click is the primary interaction
 *
 * COPY FLOW:
 * - Click "Copy" button in Actions column → copies event and navigates to /admin/events/edit/:id
 *
 * DELETE FLOW:
 * - Not yet implemented (no delete button in table)
 *
 * KEY DESIGN DECISIONS:
 * 1. Row click for editing (not separate edit button) - cleaner UX
 * 2. Dedicated pages instead of modals - better for complex forms with tabs
 * 3. Consistent navigation pattern for create/edit
 *
 * Routes:
 * - /admin/events - This page (table view)
 * - /admin/events/new - NewEventPage (create)
 * - /admin/events/:id - AdminEventDetailsPage (view/edit)
 */
export const AdminEventsPage: React.FC = () => {
  const navigate = useNavigate();
  const { data: events, isLoading, error } = useEvents({ includeUnpublished: true });
  const copyEventMutation = useCopyEvent();
  
  // Filter and sort state management
  const {
    filterState,
    rawSearchTerm,
    updateFilter,
    handleSort,
    processEvents
  } = useAdminEventFilters();

  // Process events through filter pipeline
  const filteredAndSortedEvents = processEvents(events || []);

  const handleCreateEvent = () => {
    navigate('/admin/events/new');
  };

  const handleCopyEvent = async (eventId: string) => {
    try {
      const copiedEvent = await copyEventMutation.mutateAsync(eventId);
      
      notifications.show({
        title: 'Event Copied',
        message: 'Event copied successfully. Redirecting to edit page.',
        color: 'green'
      });
      
      // Navigate to edit the copied event - ensure copiedEvent has id property
      const copiedEventId = (copiedEvent as any)?.id || eventId;
      navigate(`/admin/events/edit/${copiedEventId}`);
    } catch (error) {
      notifications.show({
        title: 'Copy Failed',
        message: 'Unable to copy event. Please try again.',
        color: 'red'
      });
    }
  };

  if (error) {
    return (
      <Box p="xl">
        <Title
          order={1}
          mb="xl"
          c="wcr.7"
          ff="Bodoni Moda, serif"
          size="2.5rem"
        >
          Events Dashboard
        </Title>
        
        <Alert color="red" title="Error Loading Events">
          Failed to load events. Please try refreshing the page or contact support if the problem persists.
        </Alert>
      </Box>
    );
  }

  return (
    <Box p="xl">
      {/* Page Header with Title and Create Button */}
      <Group justify="space-between" align="center" mb="xl">
        <Title order={1} c="burgundy">
          Events Dashboard
        </Title>
        
        <Button
          leftSection={<IconPlus size={16} />}
          onClick={handleCreateEvent}
          variant="gradient"
          gradient={{ from: "wcr.6", to: "yellow.6" }}
          size="md"
          data-testid="button-create-event"
          styles={{
            root: {
              borderRadius: '12px 6px 12px 6px',
              fontWeight: 600,
              '&:hover': {
                borderRadius: '6px 12px 6px 12px',
                transition: 'border-radius 200ms ease'
              }
            }
          }}
        >
          Create Event
        </Button>
      </Group>

      {/* Filter Bar */}
      <EventsFilterBar
        filterState={filterState}
        rawSearchTerm={rawSearchTerm}
        onFilterChange={updateFilter}
      />

      {/* Events Table */}
      {isLoading ? (
        <Box style={{ textAlign: 'center', padding: '40px' }}>
          <Loader size="lg" color="wcr.7" />
        </Box>
      ) : (
        <EventsTableView
          events={filteredAndSortedEvents}
          sortState={{
            sortColumn: filterState.sortColumn,
            sortDirection: filterState.sortDirection
          }}
          onSort={handleSort}
          onCopyEvent={handleCopyEvent}
        />
      )}

      {/* Summary Statistics */}
      {!isLoading && events && (
        <Box mt="md">
          <Group gap="xl">
            <span style={{ fontSize: '14px', color: 'var(--mantine-color-gray-6)' }}>
              Showing {filteredAndSortedEvents.length} of {events.length} events
            </span>
            {filterState.activeTypes.length > 0 && (
              <span style={{ fontSize: '14px', color: 'var(--mantine-color-gray-6)' }}>
                Filtered by: {filterState.activeTypes.join(', ')}
              </span>
            )}
            {filterState.searchTerm && (
              <span style={{ fontSize: '14px', color: 'var(--mantine-color-gray-6)' }}>
                Search: "{filterState.searchTerm}"
              </span>
            )}
          </Group>
        </Box>
      )}
    </Box>
  );
};
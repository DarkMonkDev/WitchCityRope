import React, { useState } from 'react'
import { Table, ActionIcon, Button, Text, Group, Skeleton, Badge } from '@mantine/core'
import { IconCaretUp, IconCaretDown, IconSelector } from '@tabler/icons-react'
import { notifications } from '@mantine/notifications'
import { useNavigate } from 'react-router-dom'
import type { EventDto } from '@witchcityrope/shared-types'
import { CapacityDisplay } from './CapacityDisplay'
import type { AdminEventFiltersState } from '../../hooks/useAdminEventFilters'
import { useEventTimeZone } from '../../hooks/useEventTimeZone'
import { formatUtcToLocalTime, formatUtcTimeRange } from '../../utils/eventUtils'
import { GenerateCheckInLinkModal } from '../../features/checkin/components/GenerateCheckInLinkModal'

interface EventsTableViewProps {
  events: EventDto[]
  sortState: Pick<AdminEventFiltersState, 'sortColumn' | 'sortDirection'>
  onSort: (column: 'date' | 'title') => void
  onCopyEvent?: (eventId: string) => void
  isLoading?: boolean
}

// Helper function to get the next upcoming session or first session
const getDisplaySession = (event: EventDto) => {
  if (!event.sessions || event.sessions.length === 0) {
    return null;
  }

  const now = new Date();

  // Find next upcoming session (session with startTime >= now)
  const upcomingSessions = event.sessions
    .filter(session => session.startTime && new Date(session.startTime) >= now)
    .sort((a, b) => {
      const aTime = new Date(a.startTime || 0).getTime();
      const bTime = new Date(b.startTime || 0).getTime();
      return aTime - bTime;
    });

  // Return next upcoming session, or fall back to first session
  return upcomingSessions.length > 0 ? upcomingSessions[0] : event.sessions[0];
}

// Helper function to format event dates with robust field handling
const formatEventDate = (event: EventDto): string => {
  // Use next upcoming session's date instead of event.startDate
  const displaySession = getDisplaySession(event);

  if (!displaySession || !displaySession.startDate) {
    return 'Date TBD';
  }

  // session.startDate is a date-only field (local date extracted from UTC StartTime on backend)
  // Do NOT apply timezone conversion - just extract YYYY-MM-DD
  const datePart = displaySession.startDate.split('T')[0]; // Extract YYYY-MM-DD
  const [year, month, day] = datePart.split('-').map(Number);
  const date = new Date(year, month - 1, day);

  // Check if date is valid
  if (isNaN(date.getTime())) {
    console.error('Invalid date for event:', { eventId: event.id, sessionDate: displaySession.startDate })
    return 'Invalid Date'
  }

  return date.toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric'
    // NO timeZone parameter - we don't want conversion
  });
}

// Helper function to format time range with robust field handling
const formatTimeRange = (event: EventDto, timeZone: string): string => {
  // Use next upcoming session's time instead of event.startDate/endDate
  const displaySession = getDisplaySession(event);

  if (!displaySession || !displaySession.startTime) {
    return 'Time TBD';
  }

  const start = new Date(displaySession.startTime);

  // Check if start time is valid
  if (isNaN(start.getTime())) {
    console.error('Invalid start time for session:', { eventId: event.id, sessionStartTime: displaySession.startTime })
    return 'Invalid Time'
  }

  // If no end time, show start time only
  if (!displaySession.endTime) {
    return formatUtcToLocalTime(displaySession.startTime, timeZone);
  }

  const end = new Date(displaySession.endTime);

  // Check if end time is valid
  if (isNaN(end.getTime())) {
    console.warn('Invalid end time for session, using start time only:', {
      eventId: event.id,
      sessionEndTime: displaySession.endTime,
    })
    return formatUtcToLocalTime(displaySession.startTime, timeZone);
  }

  return formatUtcTimeRange(displaySession.startTime, displaySession.endTime, timeZone);
}

// Helper function to get the correct current count based on event flags
const getCorrectCurrentCount = (event: EventDto): number => {
  const allowRsvps = (event as any)?.allowRsvps ?? false
  const requireTicketPurchase = (event as any)?.requireTicketPurchase ?? true
  const isSocialEvent = allowRsvps && !requireTicketPurchase
  return isSocialEvent ? event.currentRSVPs || 0 : event.currentTickets || 0
}

// Helper function to get event type badge color
const getEventTypeBadgeColor = (eventType?: string | null): string => {
  if (!eventType) return 'gray'

  switch (eventType.toLowerCase()) {
    case 'workshop':
      return 'blue'
    case 'social':
      return 'green'
    case 'performance':
      return 'purple'
    default:
      return 'gray'
  }
}

// Helper function to check if event has any sessions within ±12 hours of current time
const isWithinCheckInWindow = (event: EventDto): boolean => {
  if (!event.sessions || event.sessions.length === 0) {
    return false
  }

  const now = new Date()
  const twelveHoursMs = 12 * 60 * 60 * 1000 // 12 hours in milliseconds
  const windowStart = new Date(now.getTime() - twelveHoursMs)
  const windowEnd = new Date(now.getTime() + twelveHoursMs)

  return event.sessions.some((session) => {
    // Use startTime (full timestamp) not date (date-only field)
    if (!session.startTime) return false

    const sessionStart = new Date(session.startTime)

    // Check if session start time is within ±12 hours of now
    return sessionStart >= windowStart && sessionStart <= windowEnd
  })
}

// Helper function to get sort icon
const getSortIcon = (
  column: 'date' | 'title',
  sortState: Pick<AdminEventFiltersState, 'sortColumn' | 'sortDirection'>
) => {
  if (sortState.sortColumn !== column) {
    return <IconSelector size={14} />
  }

  return sortState.sortDirection === 'asc' ? <IconCaretUp size={14} /> : <IconCaretDown size={14} />
}

// Loading skeleton component
const EventsTableSkeleton: React.FC = () => (
  <Table>
    <Table.Thead bg="wcr.7">
      <Table.Tr>
        <Table.Th c="white" style={{ width: '160px' }}>
          Date
        </Table.Th>
        <Table.Th c="white" style={{ width: '120px', textAlign: 'center' }}>
          Type
        </Table.Th>
        <Table.Th c="white" style={{ minWidth: '200px' }}>
          Event Title
        </Table.Th>
        <Table.Th c="white" style={{ width: '200px', maxWidth: '200px', textAlign: 'center' }}>
          Time
        </Table.Th>
        <Table.Th c="white" style={{ width: '160px', maxWidth: '160px', textAlign: 'center' }}>
          Tickets/Capacity
        </Table.Th>
        <Table.Th c="white" style={{ width: '100px', textAlign: 'center' }}>
          Actions
        </Table.Th>
      </Table.Tr>
    </Table.Thead>
    <Table.Tbody>
      {Array.from({ length: 5 }).map((_, index) => (
        <Table.Tr key={index}>
          <Table.Td style={{ width: '160px' }}>
            <Skeleton height={20} width="80%" />
          </Table.Td>
          <Table.Td style={{ width: '120px', textAlign: 'center' }}>
            <Skeleton height={20} width="70%" />
          </Table.Td>
          <Table.Td style={{ minWidth: '200px' }}>
            <Skeleton height={20} width="90%" />
          </Table.Td>
          <Table.Td style={{ width: '200px', maxWidth: '200px', textAlign: 'center' }}>
            <Skeleton height={20} width="70%" />
          </Table.Td>
          <Table.Td style={{ width: '160px', maxWidth: '160px' }}>
            <Skeleton height={16} width="60%" mb={4} />
            <Skeleton height={8} width="100%" />
          </Table.Td>
          <Table.Td style={{ width: '100px', textAlign: 'center' }}>
            <Skeleton height={28} width="60%" />
          </Table.Td>
        </Table.Tr>
      ))}
    </Table.Tbody>
  </Table>
)

/**
 * EventsTableView - Displays events in a sortable table with Copy action
 *
 * User Flow:
 * 1. User clicks on table row to edit event
 * 2. Navigation handler navigates to /admin/events/:id
 * 3. AdminEventDetailsPage loads with EventForm populated with event data
 * 4. User can edit all fields and save changes
 *
 * Action Buttons:
 * - "Copy" button: Duplicates event and opens edit page for the copy
 * - NO "Delete" button: Not yet implemented
 *
 * Testing Note:
 * E2E tests should click on the event row to navigate to event details
 */
export const EventsTableView: React.FC<EventsTableViewProps> = ({
  events,
  sortState,
  onSort,
  onCopyEvent,
  isLoading = false,
}) => {
  const navigate = useNavigate()
  const eventTimeZone = useEventTimeZone();

  // State for check-in modal
  const [checkInModalOpen, setCheckInModalOpen] = useState(false)
  const [selectedEventForCheckIn, setSelectedEventForCheckIn] = useState<{
    id: string
    title: string
  } | null>(null)

  /**
   * handleRowClick - Primary interaction for viewing/editing events
   * Clicking any row navigates to the event detail/edit page
   */
  const handleRowClick = (eventId: string) => {
    navigate(`/admin/events/${eventId}`)
  }

  const handleCopyEvent = (eventId: string, event: React.MouseEvent) => {
    // Stop propagation to prevent row click
    event.stopPropagation()

    if (onCopyEvent) {
      onCopyEvent(eventId)
    } else {
      // Fallback notification if copy handler not provided
      notifications.show({
        title: 'Copy Event',
        message: 'Copy functionality will be implemented soon.',
        color: 'blue',
      })
    }
  }

  if (isLoading) {
    return <EventsTableSkeleton />
  }

  if (events.length === 0) {
    return (
      <Table>
        <Table.Thead bg="wcr.7">
          <Table.Tr>
            <Table.Th c="white" style={{ width: '160px' }}>
              Date
            </Table.Th>
            <Table.Th c="white" style={{ width: '120px', textAlign: 'center' }}>
              Type
            </Table.Th>
            <Table.Th c="white" style={{ minWidth: '200px' }}>
              Event Title
            </Table.Th>
            <Table.Th c="white" style={{ width: '200px', maxWidth: '200px', textAlign: 'center' }}>
              Time
            </Table.Th>
            <Table.Th c="white" style={{ width: '160px', maxWidth: '160px', textAlign: 'center' }}>
              Tickets/Capacity
            </Table.Th>
            <Table.Th c="white" style={{ width: '100px', textAlign: 'center' }}>
              Actions
            </Table.Th>
          </Table.Tr>
        </Table.Thead>
        <Table.Tbody>
          <Table.Tr>
            <Table.Td colSpan={6} ta="center" py="xl">
              <Text c="dimmed" size="lg">
                No events found matching your filters
              </Text>
            </Table.Td>
          </Table.Tr>
        </Table.Tbody>
      </Table>
    )
  }

  return (
    <>
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
          <Table.Th c="white" style={{ width: '120px', textAlign: 'center' }}>
            Type
          </Table.Th>

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

          <Table.Th c="white" style={{ width: '200px', maxWidth: '200px', textAlign: 'center' }}>
            Time
          </Table.Th>
          <Table.Th c="white" style={{ width: '160px', maxWidth: '160px', textAlign: 'center' }}>
            Tickets/Capacity
          </Table.Th>
          <Table.Th c="white" style={{ width: '100px', textAlign: 'center' }}>
            Actions
          </Table.Th>
        </Table.Tr>
      </Table.Thead>

      {/* Table Body */}
      <Table.Tbody>
        {events.map((event) => (
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
              {(() => {
                const allowRsvps = (event as any)?.allowRsvps ?? false
                const requireTicketPurchase = (event as any)?.requireTicketPurchase ?? true
                const eventTypeDisplay = allowRsvps && !requireTicketPurchase ? 'Social' : 'Class'
                return (
                  <Badge color={getEventTypeBadgeColor(eventTypeDisplay)} variant="filled" size="sm">
                    {eventTypeDisplay}
                  </Badge>
                )
              })()}
            </Table.Td>

            {/* Title Column - Takes most space */}
            <Table.Td style={{ minWidth: '200px' }}>
              <Group gap="sm" align="center">
                <Text fw={600} c="wcr.7" size="md" lineClamp={2}>
                  {event.title}
                  {!event.isPublished ? ' - DRAFT' : ''}
                </Text>
                {isWithinCheckInWindow(event) && (
                  <Button
                    variant="filled"
                    color="blue"
                    size="compact-sm"
                    onClick={(e) => {
                      e.stopPropagation()
                      setSelectedEventForCheckIn({ id: event.id || '', title: event.title || '' })
                      setCheckInModalOpen(true)
                    }}
                    styles={{
                      root: {
                        fontWeight: 600,
                        height: '32px',
                        paddingTop: '6px',
                        paddingBottom: '6px',
                        paddingLeft: '12px',
                        paddingRight: '12px',
                        fontSize: '13px',
                        lineHeight: '1.4',
                      },
                    }}
                  >
                    Check In
                  </Button>
                )}
              </Group>
            </Table.Td>

            {/* Time Column */}
            <Table.Td style={{ width: '200px', maxWidth: '200px', textAlign: 'center' }}>
              <Text size="md" c="dimmed">
                {formatTimeRange(event, eventTimeZone)}
              </Text>
            </Table.Td>

            {/* Capacity Column - Narrow */}
            <Table.Td style={{ width: '160px', maxWidth: '160px' }}>
              <CapacityDisplay current={getCorrectCurrentCount(event)} max={event.capacity} />
            </Table.Td>

            {/* Actions Column - Contains Copy button */}
            <Table.Td style={{ width: '100px' }} onClick={(e) => e.stopPropagation()}>
              <Group gap="xs" justify="center">
                <Button
                  variant="subtle"
                  color="wcr.7"
                  data-testid="button-copy-event"
                  onClick={(e) => handleCopyEvent(event.id, e)}
                  styles={{
                    root: {
                      minHeight: 40,
                      height: 'auto',
                      fontWeight: 600,
                      fontSize: '14px',
                      paddingTop: '10px',
                      paddingBottom: '10px',
                      paddingLeft: '12px',
                      paddingRight: '12px',
                      lineHeight: '1.4',
                    },
                  }}
                >
                  Copy
                </Button>
              </Group>
            </Table.Td>
          </Table.Tr>
        ))}
      </Table.Tbody>
    </Table>

    {/* Check-In Modal */}
    <GenerateCheckInLinkModal
      opened={checkInModalOpen}
      onClose={() => {
        setCheckInModalOpen(false)
        setSelectedEventForCheckIn(null)
      }}
      eventId={selectedEventForCheckIn?.id || ''}
      eventTitle={selectedEventForCheckIn?.title || ''}
    />
  </>
  )
}

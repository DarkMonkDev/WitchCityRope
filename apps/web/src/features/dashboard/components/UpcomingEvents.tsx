// UpcomingEvents Component
// Displays user's upcoming events with registration status

import React from 'react';
import {
  Card,
  Group,
  Stack,
  Text,
  Badge,
  Button,
  Skeleton,
  Alert,
  ActionIcon,
  Tooltip,
  Box,
  Divider,
  Paper
} from '@mantine/core';
import {
  IconCalendarEvent,
  IconMapPin,
  IconUser,
  IconTicket,
  IconRefresh,
  IconAlertCircle,
  IconArrowRight,
  IconClock
} from '@tabler/icons-react';
import { useUserEvents, useDashboardError } from '../hooks/useDashboard';
import { DashboardUtils } from '../types/dashboard.types';
import type { DashboardEventDto } from '../types/dashboard.types';

/**
 * UpcomingEvents Component Props
 */
interface UpcomingEventsProps {
  /** Number of events to display (default: 3) */
  count?: number;
  /** Optional className for styling */
  className?: string;
  /** Callback when user wants to view all events */
  onViewAllEvents?: () => void;
}

/**
 * Event Card Component
 * Individual card for each upcoming event
 */
interface EventCardProps {
  event: DashboardEventDto;
}

const EventCard: React.FC<EventCardProps> = ({ event }) => {
  const statusDisplay = DashboardUtils.getRegistrationStatusDisplay(event.registrationStatus);
  const startDate = new Date(event.startDate);
  const endDate = new Date(event.endDate);
  
  // Format date display
  const eventDate = DashboardUtils.formatDate(event.startDate);
  const startTime = startDate.toLocaleTimeString('en-US', {
    hour: 'numeric',
    minute: '2-digit',
    hour12: true
  });
  const endTime = endDate.toLocaleTimeString('en-US', {
    hour: 'numeric',
    minute: '2-digit',
    hour12: true
  });
  
  // Check if event is today or soon
  const isToday = startDate.toDateString() === new Date().toDateString();
  const daysUntil = Math.ceil((startDate.getTime() - new Date().getTime()) / (1000 * 60 * 60 * 24));
  const isUpcoming = daysUntil <= 7 && daysUntil >= 0;

  return (
    <Paper
      p="md"
      withBorder
      style={{
        borderLeft: isToday ? '4px solid var(--mantine-color-green-5)' : 
                   isUpcoming ? '4px solid var(--mantine-color-orange-5)' : undefined,
        backgroundColor: isToday ? 'var(--mantine-color-green-0)' : undefined
      }}
    >
      <Stack gap="sm">
        {/* Event header */}
        <Group gap="sm" align="flex-start">
          <Stack gap="xs" style={{ flex: 1 }}>
            <Group gap="sm" align="center">
              <Text fw={600} size="md" lineClamp={1}>
                {event.title}
              </Text>
              
              {isToday && (
                <Badge color="green" size="xs" variant="filled">
                  Today!
                </Badge>
              )}
              
              {isUpcoming && !isToday && (
                <Badge color="orange" size="xs" variant="light">
                  {daysUntil === 1 ? 'Tomorrow' : `${daysUntil} days`}
                </Badge>
              )}
            </Group>
            
            <Badge
              color="blue"
              variant="light"
              size="sm"
              style={{ alignSelf: 'flex-start' }}
            >
              {event.eventType}
            </Badge>
          </Stack>
          
          <Badge
            color={statusDisplay.color}
            variant={statusDisplay.color === 'green' ? 'filled' : 'light'}
            size="sm"
          >
            {statusDisplay.label}
          </Badge>
        </Group>

        {/* Event details */}
        <Stack gap="xs">
          <Group gap="xs">
            <IconCalendarEvent size={16} color="#9b4a75" />
            <Text size="sm" c="dimmed">
              {eventDate} • {startTime} - {endTime}
            </Text>
          </Group>
          
          {event.location && (
            <Group gap="xs">
              <IconMapPin size={16} color="#9b4a75" />
              <Text size="sm" c="dimmed" lineClamp={1}>
                {event.location}
              </Text>
            </Group>
          )}
          
          {event.instructorName && (
            <Group gap="xs">
              <IconUser size={16} color="#9b4a75" />
              <Text size="sm" c="dimmed">
                {event.instructorName}
              </Text>
            </Group>
          )}
          
          {event.confirmationCode && (
            <Group gap="xs">
              <IconTicket size={16} color="#9b4a75" />
              <Text size="sm" c="dimmed" fw={500}>
                Confirmation: {event.confirmationCode}
              </Text>
            </Group>
          )}
        </Stack>
      </Stack>
    </Paper>
  );
};

/**
 * UpcomingEvents Component
 * Displays list of user's upcoming events
 */
export const UpcomingEvents: React.FC<UpcomingEventsProps> = ({ 
  count = 3, 
  className,
  onViewAllEvents 
}) => {
  const { data: eventsData, isLoading, error, refetch } = useUserEvents(count);
  const dashboardError = useDashboardError(error);

  // Loading state
  if (isLoading) {
    return (
      <Card shadow="sm" padding="lg" className={className}>
        <Stack gap="md">
          <Group gap="sm" align="center">
            <IconCalendarEvent size={20} color="#9b4a75" />
            <Text fw={600} size="lg">
              Upcoming Events
            </Text>
            <Skeleton height={20} width={20} circle />
          </Group>
          
          <Stack gap="sm">
            {Array.from({ length: count }).map((_, index) => (
              <Paper key={index} p="md" withBorder>
                <Stack gap="sm">
                  <Group gap="sm" align="center">
                    <Skeleton height={20} width="60%" />
                    <Skeleton height={24} width={80} />
                  </Group>
                  <Skeleton height={16} width="40%" />
                  <Skeleton height={16} width="50%" />
                </Stack>
              </Paper>
            ))}
          </Stack>
        </Stack>
      </Card>
    );
  }

  // Error state
  if (error) {
    return (
      <Card shadow="sm" padding="lg" className={className}>
        <Stack gap="md">
          <Group gap="sm" align="center">
            <IconCalendarEvent size={20} color="#9b4a75" />
            <Text fw={600} size="lg">
              Upcoming Events
            </Text>
          </Group>
          
          <Alert
            icon={<IconAlertCircle size={16} />}
            color="red"
            title="Unable to Load Events"
          >
            <Group gap="md" mt="sm">
              <Text size="sm">
                {dashboardError?.message || 'Failed to load your upcoming events.'}
              </Text>
              <ActionIcon
                variant="light"
                color="blue"
                onClick={() => refetch()}
              >
                <IconRefresh size={16} />
              </ActionIcon>
            </Group>
          </Alert>
        </Stack>
      </Card>
    );
  }

  const events = eventsData?.upcomingEvents || [];
  const hasEvents = events.length > 0;

  return (
    <Card shadow="sm" padding="lg" className={className}>
      <Stack gap="md">
        {/* Header */}
        <Group gap="sm" align="center">
          <IconCalendarEvent size={20} color="#9b4a75" />
          <Text fw={600} size="lg" style={{ flex: 1 }}>
            Upcoming Events
          </Text>
          
          <Tooltip label="Refresh events">
            <ActionIcon
              variant="subtle"
              size="sm"
              onClick={() => refetch()}
            >
              <IconRefresh size={14} />
            </ActionIcon>
          </Tooltip>
        </Group>

        {/* Events list or empty state */}
        {hasEvents ? (
          <Stack gap="sm">
            {events.map((event) => (
              <EventCard key={event.id} event={event} />
            ))}
            
            {/* View all events link */}
            {onViewAllEvents && (
              <>
                <Divider />
                <Group gap="sm" justify="center">
                  <Button
                    variant="light"
                    color="blue"
                    size="sm"
                    rightSection={<IconArrowRight size={16} />}
                    onClick={onViewAllEvents}
                  >
                    View All My Events
                  </Button>
                </Group>
              </>
            )}
          </Stack>
        ) : (
          <Alert
            color="blue"
            variant="light"
            icon={<IconClock size={16} />}
          >
            <Stack gap="sm">
              <Text size="sm">
                You don't have any upcoming events registered yet.
              </Text>
              <Text size="xs" c="dimmed">
                Check out our event calendar to find workshops, skill shares, and social events!
              </Text>
            </Stack>
          </Alert>
        )}
      </Stack>
    </Card>
  );
};

export default UpcomingEvents;
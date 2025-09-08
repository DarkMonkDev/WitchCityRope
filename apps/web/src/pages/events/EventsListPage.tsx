import React, { useMemo } from 'react';
import { 
  Container, Stack, Title, Text, Group, Alert, Button,
  Box, Skeleton, Center
} from '@mantine/core';
import { EventCard, EventFilters, type EventFiltersState } from '../../components/events/public';
import { useEventFilters } from '../../hooks/useEventFilters';
import { useEvents } from '../../lib/api/hooks/useEvents';
import { formatEventDate, formatEventTime, calculateEventDuration } from '../../utils/eventUtils';
import type { EventDto } from '../../lib/api/types/events.types';

// Extended type for public event card display
interface PublicEventDto extends Omit<EventDto, 'capacity'> {
  type: 'CLASS' | 'SOCIAL' | 'MEMBER';
  instructor?: string;
  startTime: string;
  endTime: string;
  price: {
    type: 'sliding' | 'fixed';
    min?: number;
    max?: number;
    amount?: number;
  };
  capacity: {
    total: number;
    taken: number;
    available: number;
  };
  isMemberOnly?: boolean;
  requiresVetting?: boolean;
}

// Mock function to get user role - replace with actual auth context
const useAuth = () => ({
  userRole: 'anonymous' as 'anonymous' | 'member' | 'vetted' | 'admin',
  isAuthenticated: false
});

// Transform EventDto to match EventCard props
const transformEventForCard = (event: EventDto): PublicEventDto => {
  const startDate = new Date(event.startDate);
  const endDate = event.endDate ? new Date(event.endDate) : null;
  const eventCapacity = typeof event.capacity === 'number' ? event.capacity : 12;
  const registrationCount = event.registrationCount || 0;
  
  return {
    ...event,
    type: 'CLASS' as const, // Default for now - will be enhanced with actual event types
    instructor: 'Instructor TBA', // Will be populated from backend
    startTime: formatEventTime(event.startDate),
    endTime: endDate ? formatEventTime(event.endDate) : formatEventTime(event.startDate),
    price: {
      type: 'sliding' as const,
      min: 35,
      max: 55
    },
    capacity: {
      total: eventCapacity,
      taken: registrationCount,
      available: eventCapacity - registrationCount
    },
    isMemberOnly: false, // Will be enhanced with actual field
    requiresVetting: false
  };
};

// Group events by date
const groupEventsByDate = (events: EventDto[]) => {
  const grouped: { [date: string]: EventDto[] } = {};
  
  events.forEach(event => {
    const dateKey = formatEventDate(event.startDate);
    if (!grouped[dateKey]) {
      grouped[dateKey] = [];
    }
    grouped[dateKey].push(event);
  });
  
  return grouped;
};

// Get unique instructors for filter
const getUniqueInstructors = (events: EventDto[]): string[] => {
  // For now, return mock instructors - will be populated from events data
  return ['Instructor A', 'Instructor B', 'Instructor C'];
};

export const EventsListPage: React.FC = () => {
  const { userRole } = useAuth();
  const { filters, updateFilters, clearFilters } = useEventFilters();
  
  // Convert filters to API format
  const apiFilters = useMemo(() => ({
    // Map UI filters to API filters when backend supports them
    search: filters.eventType !== 'all' ? filters.eventType : undefined,
  }), [filters]);
  
  const { 
    data: events, 
    isLoading, 
    error, 
    refetch 
  } = useEvents(apiFilters);
  
  const eventsArray: EventDto[] = (events as EventDto[]) || [];
  const instructors = useMemo(() => getUniqueInstructors(eventsArray), [eventsArray]);
  const groupedEvents = useMemo(() => groupEventsByDate(eventsArray), [eventsArray]);
  
  const handleRegister = (eventId: string) => {
    console.log('Register for event:', eventId);
    // Will implement with actual registration flow
  };
  
  const handleRSVP = (eventId: string) => {
    console.log('RSVP for event:', eventId);
    // Will implement with actual RSVP flow
  };

  if (error) {
    return (
      <Container size="xl" py="xl">
        <Alert color="red" title="Failed to Load Events">
          <Stack gap="sm">
            <Text size="sm">
              Unable to load events. Please check your connection and try again.
            </Text>
            <Button size="xs" variant="outline" onClick={() => refetch()}>
              Retry
            </Button>
          </Stack>
        </Alert>
      </Container>
    );
  }

  return (
    <Container size="xl" py="md">
      <Stack gap="xl">
        {/* Page Header */}
        <Stack gap="md" ta="center">
          <Title order={1} size="2.5rem" c="burgundy">
            Events & Classes
          </Title>
          <Text size="lg" c="dimmed" maw={600} mx="auto">
            Discover workshops, classes, and social events in Salem's rope bondage community
          </Text>
        </Stack>
        
        {/* Filters Section */}
        <EventFilters
          filters={filters}
          instructors={instructors}
          onFiltersChange={updateFilters}
          resultCount={eventsArray.length}
          isLoading={isLoading}
        />
        
        {/* Events Content */}
        {isLoading ? (
          <EventsListSkeleton />
        ) : eventsArray.length === 0 ? (
          <EmptyEventsState filters={filters} onClearFilters={clearFilters} />
        ) : (
          <Stack gap="xl">
            {Object.entries(groupedEvents).map(([date, dateEvents]) => (
              <Stack key={date} gap="md">
                {/* Date Header */}
                <Box>
                  <Title 
                    order={2} 
                    size="1.5rem" 
                    c="burgundy"
                    pb="xs"
                    style={{ 
                      borderBottom: '2px solid var(--mantine-color-burgundy-2)',
                      display: 'inline-block'
                    }}
                  >
                    {date}
                  </Title>
                </Box>
                
                {/* Event Cards for this date */}
                <Stack gap="md">
                  {dateEvents.map(event => (
                    <EventCard
                      key={event.id}
                      event={transformEventForCard(event)}
                      userRole={userRole}
                      onRegister={handleRegister}
                      onRSVP={handleRSVP}
                    />
                  ))}
                </Stack>
              </Stack>
            ))}
          </Stack>
        )}
      </Stack>
    </Container>
  );
};

// Loading skeleton component
const EventsListSkeleton: React.FC = () => (
  <Stack gap="xl">
    {Array.from({ length: 3 }, (_, index) => (
      <Stack key={index} gap="md">
        <Skeleton height={32} width={200} />
        <Stack gap="sm">
          <EventCardSkeleton />
          <EventCardSkeleton />
        </Stack>
      </Stack>
    ))}
  </Stack>
);

const EventCardSkeleton: React.FC = () => (
  <Box p="md" style={{ border: '1px solid #e0e0e0', borderRadius: '8px' }}>
    <Stack gap="sm">
      <Group justify="space-between">
        <Group gap="xs">
          <Skeleton height={20} width={60} radius="xl" />
          <Skeleton height={24} width={200} />
        </Group>
      </Group>
      
      <Group gap="md">
        <Skeleton height={16} width={120} />
        <Skeleton height={16} width={100} />
      </Group>
      
      <Skeleton height={40} />
      
      <Group justify="space-between">
        <Group gap="md">
          <Skeleton height={20} width={80} />
          <Skeleton height={16} width={120} />
        </Group>
        <Skeleton height={36} width={120} />
      </Group>
    </Stack>
  </Box>
);

// Empty state component
interface EmptyEventsStateProps {
  filters: EventFiltersState;
  onClearFilters: () => void;
}

const EmptyEventsState: React.FC<EmptyEventsStateProps> = ({ filters, onClearFilters }) => (
  <Center py="xl">
    <Stack align="center" gap="md">
      <Title order={3} c="dimmed">No Events Found</Title>
      <Text c="dimmed" ta="center">
        {filters.eventType === 'all' && !filters.instructor
          ? "There are no upcoming events scheduled at this time."
          : "No events found matching your current filters."
        }
      </Text>
      {(filters.eventType !== 'all' || filters.instructor) && (
        <Button variant="outline" color="burgundy" onClick={onClearFilters}>
          Clear Filters
        </Button>
      )}
    </Stack>
  </Center>
);
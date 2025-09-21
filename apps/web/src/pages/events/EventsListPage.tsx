import React, { useMemo, useState } from 'react';
import {
  Container, Stack, Title, Text, Group, Alert, Button,
  Box, Skeleton, Center, Paper, Badge, TextInput, Select, SegmentedControl,
  Table, ActionIcon, Switch
} from '@mantine/core';
import { IconSearch, IconCalendar, IconClock, IconUsers, IconArrowUp, IconArrowDown } from '@tabler/icons-react';
import { useEventFilters } from '../../hooks/useEventFilters';
import { useEvents } from '../../lib/api/hooks/useEvents';
import { formatEventDate, formatEventTime, calculateEventDuration } from '../../utils/eventUtils';
import type { EventDto } from '../../lib/api/types/events.types';
import { useNavigate } from 'react-router-dom';

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
  const navigate = useNavigate();
  const { userRole } = useAuth();
  const [viewMode, setViewMode] = useState<'cards' | 'list'>('cards');
  const [showPastClasses, setShowPastClasses] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  const [sortBy, setSortBy] = useState('date');
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
  

  // Use real API data only - ensure events is always an array
  const eventsArray: EventDto[] = Array.isArray(events) ? events : [];
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
        <Alert data-testid="events-error" color="red" title="Failed to Load Events">
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
    <Box data-testid="page-events" style={{ background: 'var(--color-cream)', minHeight: '100vh' }}>
      {/* Hero Section with burgundy gradient background */}
      <Box
        style={{
          background: 'linear-gradient(135deg, var(--color-burgundy) 0%, var(--color-plum) 100%)',
          position: 'relative',
          overflow: 'hidden'
        }}
      >
        {/* Subtle pattern overlay */}
        <Box
          style={{
            position: 'absolute',
            top: '-50%',
            left: '-50%',
            width: '200%',
            height: '200%',
            background: 'radial-gradient(circle, rgba(255,255,255,0.1) 0%, transparent 70%)',
            transform: 'rotate(45deg)'
          }}
        />
        <Container size="xl" style={{ position: 'relative', zIndex: 1 }}>
          <Stack gap="md" ta="center" py={64}>
            <Title 
              order={1} 
              size="3rem" 
              fw={800}
              style={{ 
                fontFamily: 'var(--font-heading)',
                color: 'var(--color-ivory)'
              }}
            >
              Explore Classes & Meetups
            </Title>
            <Text 
              size="xl" 
              style={{ 
                color: 'var(--color-dusty-rose)',
                fontSize: '20px'
              }}
            >
              Learn rope bondage in a safe, inclusive environment with experienced instructors
            </Text>
          </Stack>
        </Container>
      </Box>

      {/* Filter Bar */}
      <Box
        style={{
          background: 'var(--color-ivory)',
          borderBottom: '1px solid var(--color-taupe)',
          position: 'sticky',
          top: 0,
          zIndex: 90
        }}
      >
        <Container size="xl" py="md">
          <Group justify="space-between" align="center" wrap="wrap" gap="md">
            <Group gap="sm">
              <Switch
                label="Show Past Classes"
                checked={showPastClasses}
                onChange={(event) => setShowPastClasses(event.currentTarget.checked)}
                color="burgundy"
                size="sm"
                styles={{
                  label: {
                    fontFamily: 'var(--font-heading)',
                    fontWeight: 600,
                    fontSize: '14px'
                  }
                }}
              />
            </Group>
            
            <Group gap="md" align="center">
              <SegmentedControl
                data-testid="button-view-toggle"
                value={viewMode}
                onChange={(value) => setViewMode(value as 'cards' | 'list')}
                data={[
                  { label: 'Card View', value: 'cards' },
                  { label: 'List View', value: 'list' }
                ]}
                size="sm"
                color="burgundy"
                styles={{
                  root: {
                    background: 'var(--color-cream)',
                    borderRadius: '25px',
                    padding: '4px'
                  },
                  control: {
                    fontFamily: 'var(--font-heading)',
                    fontWeight: 600
                  }
                }}
              />
              
              <TextInput
                data-testid="input-search"
                placeholder="Search events..."
                value={searchQuery}
                onChange={(event) => setSearchQuery(event.currentTarget.value)}
                leftSection={<IconSearch size={16} color="var(--color-stone)" />}
                w={250}
                styles={{
                  input: {
                    border: '2px solid var(--color-taupe)',
                    borderRadius: '25px',
                    fontFamily: 'var(--font-body)',
                    fontSize: '14px',
                    '&:focus': {
                      borderColor: 'var(--color-burgundy)',
                      width: '300px',
                      transition: 'all 0.3s ease'
                    }
                  }
                }}
              />
              
              <Select
                data-testid="select-category"
                value={sortBy}
                onChange={(value) => setSortBy(value || 'date')}
                data={[
                  { value: 'date', label: 'Sort by Date' },
                  { value: 'price', label: 'Sort by Price' },
                  { value: 'availability', label: 'Sort by Availability' }
                ]}
                w={150}
                styles={{
                  input: {
                    border: '2px solid var(--color-taupe)',
                    borderRadius: '25px',
                    fontFamily: 'var(--font-body)',
                    fontSize: '14px',
                    background: 'var(--color-ivory)',
                    color: 'var(--color-charcoal)',
                    '&:hover': {
                      borderColor: 'var(--color-burgundy)'
                    }
                  }
                }}
              />
            </Group>
          </Group>
        </Container>
      </Box>

      {/* Main Content */}
      <Container size="xl" py="xl">
        {isLoading && eventsArray.length === 0 ? (
          <EventsListSkeleton />
        ) : eventsArray.length === 0 ? (
          <EmptyEventsState data-testid="events-empty" onClearFilters={clearFilters} />
        ) : viewMode === 'cards' ? (
          <EventCardGrid events={eventsArray} userRole={userRole} onRegister={handleRegister} onRSVP={handleRSVP} />
        ) : (
          <EventTableView events={eventsArray} onEventClick={(eventId) => navigate(`/events/${eventId}`)} />
        )}
      </Container>
    </Box>
  );
};

// Event Card Grid Component
interface EventCardGridProps {
  events: EventDto[];
  userRole: string;
  onRegister: (eventId: string) => void;
  onRSVP: (eventId: string) => void;
}

const EventCardGrid: React.FC<EventCardGridProps> = ({ events, userRole, onRegister, onRSVP }) => {
  const navigate = useNavigate();
  
  return (
    <Box
      data-testid="events-list"
      style={{
        display: 'grid',
        gridTemplateColumns: 'repeat(auto-fill, minmax(350px, 1fr))',
        gap: 'var(--space-lg)'
      }}
    >
      {events.map((event, index) => (
        <WireframeEventCard
          key={event.id}
          data-testid={`event-card-${index}`}
          event={event}
          userRole={userRole}
          onRegister={onRegister}
          onRSVP={onRSVP}
          onClick={() => navigate(`/events/${event.id}`)}
        />
      ))}
    </Box>
  );
};

// Wireframe-matching Event Card
interface WireframeEventCardProps {
  event: EventDto;
  userRole: string;
  onRegister: (eventId: string) => void;
  onRSVP: (eventId: string) => void;
  onClick: () => void;
  'data-testid'?: string;
}

const WireframeEventCard: React.FC<WireframeEventCardProps> = ({ event, userRole, onRegister, onRSVP, onClick, 'data-testid': testId }) => {
  const navigate = useNavigate();
  const capacityPercentage = ((event.registrationCount || 0) / (event.capacity || 20)) * 100;
  const availableSpots = (event.capacity || 20) - (event.registrationCount || 0);
  
  const getSpotColor = () => {
    if (availableSpots > 10) return 'var(--color-success)';
    if (availableSpots > 3) return 'var(--color-warning)';
    return 'var(--color-error)';
  };

  return (
    <Paper
      data-testid={testId || "event-card"}
      style={{
        background: 'var(--color-ivory)',
        borderRadius: '16px',
        overflow: 'hidden',
        boxShadow: '0 4px 12px rgba(0,0,0,0.05)',
        transition: 'all 0.3s ease',
        cursor: 'pointer',
        border: '1px solid rgba(183, 109, 117, 0.1)',
        position: 'relative',
        height: '100%',
        display: 'flex',
        flexDirection: 'column'
      }}
      onMouseEnter={(e) => {
        e.currentTarget.style.transform = 'translateY(-4px)';
        e.currentTarget.style.boxShadow = '0 8px 24px rgba(0,0,0,0.1)';
        e.currentTarget.style.borderColor = 'var(--color-rose-gold)';
      }}
      onMouseLeave={(e) => {
        e.currentTarget.style.transform = 'translateY(0)';
        e.currentTarget.style.boxShadow = '0 4px 12px rgba(0,0,0,0.05)';
        e.currentTarget.style.borderColor = 'rgba(183, 109, 117, 0.1)';
      }}
      onClick={onClick}
    >
      {/* Gradient Header with Event Title */}
      <Box
        style={{
          height: '100px',
          background: 'linear-gradient(135deg, var(--color-plum) 0%, var(--color-burgundy) 100%)',
          position: 'relative',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          padding: 'var(--space-md)'
        }}
      >
        <Title
          data-testid="event-title"
          order={3}
          size="lg"
          fw={700}
          ta="center"
          style={{
            fontFamily: 'var(--font-heading)',
            color: 'var(--color-ivory)',
            lineHeight: 1.3,
            textShadow: '0 2px 4px rgba(0, 0, 0, 0.3)',
            position: 'relative',
            zIndex: 1
          }}
        >
          {event.title}
        </Title>
      </Box>

      {/* Event Content */}
      <Box style={{ padding: 'var(--space-md) var(--space-lg)', flex: 1, display: 'flex', flexDirection: 'column' }}>
        {/* Date and Time */}
        <Text
          data-testid="event-date"
          size="sm"
          fw={700}
          mb="sm"
          style={{
            fontFamily: 'var(--font-heading)',
            color: 'var(--color-burgundy)',
            textTransform: 'uppercase',
            letterSpacing: '0.5px'
          }}
        >
          <span data-testid="event-time">{formatEventDate(event.startDate)} • {formatEventTime(event.startDate)}</span>
        </Text>

        {/* Description */}
        <Text
          size="sm"
          style={{
            color: 'var(--color-stone)',
            lineHeight: 1.6,
            marginBottom: 'var(--space-md)',
            flex: 1
          }}
        >
          {event.description}
        </Text>

        {/* Footer */}
        <Box
          style={{
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
            paddingTop: 'var(--space-sm)',
            marginTop: 'auto',
            borderTop: '1px solid var(--color-taupe)'
          }}
        >
          <Text
            fw={700}
            style={{
              fontFamily: 'var(--font-heading)',
              fontSize: '18px',
              color: 'var(--color-burgundy)'
            }}
          >
            {event.capacity && event.registrationCount ?
              `$${Math.round(((event.capacity - event.registrationCount) / event.capacity) * 50 + 25)}` :
              '$35-65'
            } sliding scale
          </Text>
          
          <Text
            fw={600}
            ta="center"
            style={{
              fontFamily: 'var(--font-heading)',
              fontSize: '14px',
              color: getSpotColor()
            }}
          >
            {event.registrationCount || 0}/{event.capacity || 20}
          </Text>
        </Box>

        {/* Action Button */}
        <Group justify="center" mt="md" pb="xs">
          <Button
            className="btn btn-primary"
            size="sm"
            onClick={(e) => {
              e.stopPropagation();
              navigate(`/events/${event.id}`);
            }}
            style={{
              background: 'linear-gradient(135deg, var(--color-amber) 0%, var(--color-amber-dark) 100%)',
              color: 'var(--color-midnight)',
              border: 'none',
              padding: '10px 24px',
              fontSize: '13px',
              letterSpacing: '1px',
              fontFamily: 'var(--font-heading)',
              fontWeight: 700,
              textTransform: 'uppercase'
            }}
          >
            Learn More
          </Button>
        </Group>
      </Box>
    </Paper>
  );
};

// Event Table View
interface EventTableViewProps {
  events: EventDto[];
  onEventClick: (eventId: string) => void;
}

const EventTableView: React.FC<EventTableViewProps> = ({ events, onEventClick }) => {
  const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>('asc');
  
  const handleSort = () => {
    setSortOrder(prev => prev === 'asc' ? 'desc' : 'asc');
  };

  return (
    <Paper
      style={{
        background: 'white',
        borderRadius: '12px',
        overflow: 'hidden',
        boxShadow: '0 4px 20px rgba(0,0,0,0.08)'
      }}
    >
      <Table highlightOnHover>
        <Table.Thead
          style={{
            background: 'var(--color-burgundy)'
          }}
        >
          <Table.Tr>
            <Table.Th
              style={{
                color: 'white',
                padding: 'var(--space-md)',
                fontFamily: 'var(--font-heading)',
                fontWeight: 600,
                fontSize: '1rem',
                textTransform: 'uppercase',
                letterSpacing: '1px',
                cursor: 'pointer',
                userSelect: 'none'
              }}
              onClick={handleSort}
            >
              <Group gap="xs">
                Date 
                <ActionIcon size="sm" variant="transparent">
                  {sortOrder === 'asc' ? <IconArrowUp size={16} color="white" /> : <IconArrowDown size={16} color="white" />}
                </ActionIcon>
              </Group>
            </Table.Th>
            <Table.Th style={{ color: 'white', padding: 'var(--space-md)', fontFamily: 'var(--font-heading)', fontWeight: 600, fontSize: '1rem', textTransform: 'uppercase', letterSpacing: '1px' }}>Time</Table.Th>
            <Table.Th style={{ color: 'white', padding: 'var(--space-md)', fontFamily: 'var(--font-heading)', fontWeight: 600, fontSize: '1rem', textTransform: 'uppercase', letterSpacing: '1px' }}>Event</Table.Th>
            <Table.Th style={{ color: 'white', padding: 'var(--space-md)', fontFamily: 'var(--font-heading)', fontWeight: 600, fontSize: '1rem', textTransform: 'uppercase', letterSpacing: '1px' }}>Price</Table.Th>
            <Table.Th style={{ color: 'white', padding: 'var(--space-md)', fontFamily: 'var(--font-heading)', fontWeight: 600, fontSize: '1rem', textTransform: 'uppercase', letterSpacing: '1px' }}>Spots</Table.Th>
            <Table.Th style={{ color: 'white', padding: 'var(--space-md)', fontFamily: 'var(--font-heading)', fontWeight: 600, fontSize: '1rem', textTransform: 'uppercase', letterSpacing: '1px' }}>Action</Table.Th>
          </Table.Tr>
        </Table.Thead>
        <Table.Tbody>
          {events.map((event, index) => {
            const availableSpots = (event.capacity || 20) - (event.registrationCount || 0);
            const getSpotColor = () => {
              if (availableSpots > 10) return 'var(--color-success)';
              if (availableSpots > 3) return 'var(--color-warning)';
              return 'var(--color-error)';
            };
            
            return (
              <Table.Tr
                key={event.id}
                style={{
                  cursor: 'pointer',
                  backgroundColor: index % 2 === 1 ? 'rgba(250, 246, 242, 0.8)' : 'transparent'
                }}
                onMouseEnter={(e) => {
                  e.currentTarget.style.backgroundColor = 'rgba(136, 1, 36, 0.08)';
                }}
                onMouseLeave={(e) => {
                  e.currentTarget.style.backgroundColor = index % 2 === 1 ? 'rgba(250, 246, 242, 0.8)' : 'transparent';
                }}
                onClick={() => onEventClick(event.id)}
              >
                <Table.Td style={{ padding: 'var(--space-md)', fontFamily: 'var(--font-body)', fontWeight: 600, color: 'var(--color-charcoal)' }}>
                  {formatEventDate(event.startDate).replace(/,.*/, '')}
                </Table.Td>
                <Table.Td style={{ padding: 'var(--space-md)', fontFamily: 'var(--font-body)', fontWeight: 500, color: 'var(--color-charcoal)' }}>
                  {formatEventTime(event.startDate)}
                </Table.Td>
                <Table.Td style={{ padding: 'var(--space-md)', fontFamily: 'var(--font-heading)', fontWeight: 600, color: 'var(--color-burgundy)', fontSize: '1.2rem' }}>
                  {event.title}
                </Table.Td>
                <Table.Td style={{ padding: 'var(--space-md)', fontFamily: 'var(--font-heading)', fontWeight: 700, color: 'var(--color-burgundy)', fontSize: '1.3rem', textAlign: 'center' }}>
                  $35-65
                </Table.Td>
                <Table.Td style={{ padding: 'var(--space-md)', fontFamily: 'var(--font-heading)', fontWeight: 600, fontSize: '1.1rem', textAlign: 'center', color: getSpotColor() }}>
                  {event.registrationCount || 0}/{event.capacity || 20}
                </Table.Td>
                <Table.Td style={{ padding: 'var(--space-md)', textAlign: 'center' }}>
                  <Button
                    size="xs"
                    onClick={(e) => {
                      e.stopPropagation();
                      onEventClick(event.id);
                    }}
                    style={{
                      background: 'var(--color-cream)',
                      border: '2px solid var(--color-rose-gold)',
                      color: 'var(--color-burgundy)',
                      padding: '6px 12px',
                      margin: '0 4px',
                      borderRadius: '8px 4px 8px 4px',
                      fontFamily: 'var(--font-heading)',
                      fontWeight: 500,
                      fontSize: '0.85rem',
                      transition: 'all 0.3s ease'
                    }}
                    onMouseEnter={(e) => {
                      e.currentTarget.style.borderRadius = '4px 8px 4px 8px';
                      e.currentTarget.style.background = 'var(--color-burgundy)';
                      e.currentTarget.style.color = 'white';
                      e.currentTarget.style.transform = 'scale(1.05)';
                    }}
                    onMouseLeave={(e) => {
                      e.currentTarget.style.borderRadius = '8px 4px 8px 4px';
                      e.currentTarget.style.background = 'var(--color-cream)';
                      e.currentTarget.style.color = 'var(--color-burgundy)';
                      e.currentTarget.style.transform = 'scale(1)';
                    }}
                  >
                    Learn More
                  </Button>
                </Table.Td>
              </Table.Tr>
            );
          })}
        </Table.Tbody>
      </Table>
    </Paper>
  );
};

// Loading skeleton component
const EventsListSkeleton: React.FC = () => (
  <Stack data-testid="events-loading" gap="xl">
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
  onClearFilters: () => void;
  'data-testid'?: string;
}

const EmptyEventsState: React.FC<EmptyEventsStateProps> = ({ onClearFilters, 'data-testid': testId }) => (
  <Center data-testid={testId} py="xl">
    <Stack align="center" gap="md">
      <Box
        style={{
          width: '120px',
          height: '120px',
          background: 'linear-gradient(135deg, var(--color-ivory) 0%, var(--color-cream) 100%)',
          border: '2px solid var(--color-rose-gold)',
          borderRadius: '50%',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          fontSize: '48px',
          color: 'var(--color-burgundy)',
          boxShadow: '0 4px 15px rgba(183, 109, 117, 0.2)',
          marginBottom: 'var(--space-lg)'
        }}
      >
        📅
      </Box>
      <Title 
        order={3} 
        style={{
          fontFamily: 'var(--font-heading)',
          fontSize: '28px',
          fontWeight: 700,
          color: 'var(--color-charcoal)',
          marginBottom: 'var(--space-sm)'
        }}
      >
        No Events Found
      </Title>
      <Text ta="center" style={{ fontSize: '18px', marginBottom: 'var(--space-xl)' }}>
        There are no upcoming events scheduled at this time.
      </Text>
      <Button 
        variant="outline" 
        color="burgundy" 
        onClick={onClearFilters}
        className="btn btn-secondary"
      >
        Refresh Events
      </Button>
    </Stack>
  </Center>
);
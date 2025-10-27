import React, { memo, useMemo } from 'react';
import {
  Paper, Badge, Title, Text, Group, Stack, Progress,
  Button, Anchor, Alert
} from '@mantine/core';
import { useNavigate } from 'react-router-dom';
import { formatPrice, getCapacityColor, formatEventDateTime, calculateEventPriceRange } from '../../../utils/eventUtils';

interface EventCardProps {
  event: {
    id: string;
    title: string;
    shortDescription?: string; // Brief summary for card displays
    description: string;
    startDate: string;
    endDate?: string;
    location: string;
    createdAt: string;
    updatedAt: string;
    type: 'CLASS' | 'SOCIAL' | 'MEMBER';
    instructor?: string;
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
    startTime: string;
    endTime: string;
    ticketTypes?: any[]; // Array of ticket types with pricing
  };
  userRole: 'anonymous' | 'member' | 'vetted' | 'admin';
  onRegister?: (eventId: string) => void;
  onRSVP?: (eventId: string) => void;
}

export const EventCard = memo<EventCardProps>(({
  event,
  userRole,
  onRegister = () => {},
  onRSVP = () => {}
}) => {
  const navigate = useNavigate();
  const canViewFullDetails = useMemo(() => {
    if (!event.isMemberOnly) return true;
    return userRole === 'vetted' || userRole === 'admin';
  }, [event.isMemberOnly, userRole]);

  const capacityPercentage = useMemo(() =>
    (event.capacity.taken / event.capacity.total) * 100,
    [event.capacity.taken, event.capacity.total]
  );

  // Calculate price display from ticket types
  const displayPrice = useMemo(() => {
    return calculateEventPriceRange(event.ticketTypes || []);
  }, [event.ticketTypes]);

  const renderActionButtons = () => {
    const stopPropagation = (e: React.MouseEvent) => {
      e.stopPropagation();
    };

    if (!canViewFullDetails) {
      return (
        <Button
          variant="outline"
          color="burgundy"
          onClick={stopPropagation}
          styles={{
            root: {
              height: '44px',
              paddingTop: '12px',
              paddingBottom: '12px',
              fontSize: '14px',
              lineHeight: '1.2'
            }
          }}
        >
          Login Required
        </Button>
      );
    }

    if (event.capacity.available === 0) {
      return (
        <Button
          variant="outline"
          color="red"
          onClick={stopPropagation}
          styles={{
            root: {
              height: '44px',
              paddingTop: '12px',
              paddingBottom: '12px',
              fontSize: '14px',
              lineHeight: '1.2'
            }
          }}
        >
          Join Waitlist
        </Button>
      );
    }

    if (event.type === 'SOCIAL') {
      return (
        <Group gap="xs">
          <Button
            color="green"
            onClick={(e) => {
              stopPropagation(e);
              onRSVP(event.id);
            }}
            styles={{
              root: {
                height: '44px',
                paddingTop: '12px',
                paddingBottom: '12px',
                fontSize: '14px',
                lineHeight: '1.2'
              }
            }}
          >
            RSVP Free
          </Button>
          <Button
            variant="outline"
            color="burgundy"
            onClick={stopPropagation}
            styles={{
              root: {
                height: '44px',
                paddingTop: '12px',
                paddingBottom: '12px',
                fontSize: '14px',
                lineHeight: '1.2'
              }
            }}
          >
            Support Ticket
          </Button>
        </Group>
      );
    }

    return (
      <Button
        color="burgundy"
        onClick={(e) => {
          stopPropagation(e);
          onRegister(event.id);
        }}
        styles={{
          root: {
            height: '44px',
            paddingTop: '12px',
            paddingBottom: '12px',
            fontSize: '14px',
            lineHeight: '1.2'
          }
        }}
      >
        Purchase Ticket
      </Button>
    );
  };

  return (
    <Paper
      withBorder
      shadow="sm"
      p="md"
      radius="md"
      data-testid="event-card"
      data-event-link={`event-link-${event.id}`}
      style={{
        transition: 'all 200ms ease',
        cursor: 'pointer',
        ...(event.isMemberOnly && {
          borderColor: 'var(--mantine-color-burgundy-5)',
          borderWidth: '2px'
        })
      }}
      onMouseEnter={(e) => {
        e.currentTarget.style.transform = 'translateY(-2px)';
        e.currentTarget.style.boxShadow = '0 4px 20px rgba(0,0,0,0.1)';
      }}
      onMouseLeave={(e) => {
        e.currentTarget.style.transform = 'translateY(0)';
        e.currentTarget.style.boxShadow = '';
      }}
      onClick={() => {
        // Use setTimeout to ensure navigation happens AFTER React finishes current render cycle
        // This allows Outlet to properly unmount old component and mount new one
        setTimeout(() => {
          navigate(`/events/${event.id}`)
        }, 0)
      }}
    >
      <Stack gap="sm">
        {/* Event Header */}
        <Group justify="space-between" align="flex-start">
          <Group gap="xs" align="center">
            <Badge
              variant="light"
              color={event.type === 'CLASS' ? 'green' : event.type === 'SOCIAL' ? 'orange' : 'grape'}
              size="sm"
              data-testid="event-type"
            >
              {event.type}
            </Badge>
            <Title order={3} size="lg" fw={600} data-testid="event-title">
              {event.title}
            </Title>
          </Group>
        </Group>

        {/* Event Meta - Split Date and Time */}
        <Group justify="space-between" gap="md">
          <Group gap={4}>
            <Text span>📅</Text>
            <Text size="sm" c="dimmed" data-testid="event-date">
              {(() => {
                if (!event.startDate) return 'TBD'
                const start = new Date(event.startDate)
                return start.toLocaleDateString('en-US', {
                  weekday: 'long',
                  month: 'short',
                  day: 'numeric'
                })
              })()}
            </Text>
          </Group>
          <Text size="sm" c="dimmed">
            {(() => {
              if (!event.startDate) return ''
              const start = new Date(event.startDate)
              const startTime = start.toLocaleTimeString('en-US', {
                hour: 'numeric',
                minute: '2-digit',
                hour12: true
              }).toLowerCase()

              if (!event.endDate) return startTime

              const end = new Date(event.endDate)
              const endTime = end.toLocaleTimeString('en-US', {
                hour: 'numeric',
                minute: '2-digit',
                hour12: true
              }).toLowerCase()

              return `${startTime} - ${endTime}`
            })()}
          </Text>
        </Group>
        {event.instructor && (
          <Group gap={4}>
            <Text span>👤</Text>
            <Text size="sm" c="dimmed">
              {event.instructor}
            </Text>
          </Group>
        )}

        {/* Description */}
        {canViewFullDetails ? (
          <Text size="sm" c="dimmed" lineClamp={2}>
            {event.shortDescription || ''}
          </Text>
        ) : (
          <Stack gap="sm">
            <Text size="sm" c="dimmed" lineClamp={1} style={{ opacity: 0.7 }}>
              {event.shortDescription ? event.shortDescription.substring(0, 50) + '...' : ''}
            </Text>
            <Alert color="orange" p="xs">
              <Text size="sm">
                <Anchor href="/login" c="burgundy">Login</Anchor> or{' '}
                <Anchor href="/register" c="burgundy">apply for membership</Anchor>{' '}
                to see full details and participate
              </Text>
            </Alert>
          </Stack>
        )}

        {/* Footer */}
        <Group justify="space-between" align="center" mt="xs">
          <Group gap="md">
            {/* Pricing */}
            <Text fw={600} c="green" size="md">
              {displayPrice}
            </Text>
            
            {/* Capacity */}
            <Group gap="xs" align="center">
              <Text 
                size="sm" 
                c={capacityPercentage >= 80 ? 'red' : 'dimmed'}
              >
                {capacityPercentage >= 80 && event.capacity.available > 0 
                  ? `Only ${event.capacity.available} left!`
                  : event.capacity.available === 0
                    ? 'Full - Join Waitlist'
                    : `${event.capacity.available} of ${event.capacity.total} available`
                }
              </Text>
              <Progress
                value={capacityPercentage}
                color={getCapacityColor(capacityPercentage)}
                size="sm"
                w={80}
                aria-label={`Event capacity: ${event.capacity.taken} of ${event.capacity.total} spots taken`}
              />
            </Group>
          </Group>

          {/* Actions */}
          <Group gap="xs">
            {renderActionButtons()}
          </Group>
        </Group>
      </Stack>
    </Paper>
  );
});

EventCard.displayName = 'EventCard';
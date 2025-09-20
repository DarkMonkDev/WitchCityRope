import React from 'react';
import { Box, Text, Group, Anchor } from '@mantine/core';
import { Event } from '../../types/Event';

interface EventCardProps {
  event: Event & {
    eventType?: string;
    currentRSVPs?: number;
    currentTickets?: number;
    currentAttendees?: number;
    capacity?: number;
  };
  /** Custom pricing display */
  price?: string;
  /** Availability status */
  status?: {
    type: 'available' | 'limited' | 'full';
    text: string;
  };
  /** Additional event details */
  details?: {
    duration?: string;
    level?: string;
    spots?: string;
  };
  /** Click handler for the card */
  onClick?: () => void;
}

export const EventCard: React.FC<EventCardProps> = ({
  event,
  price = '$35-55',
  status,
  details = {
    duration: '2.5 hours',
    level: 'Beginner',
    spots: 'Salem Studio'
  },
  onClick
}) => {
  // Calculate status dynamically based on event data
  const calculateStatus = () => {
    if (!status) {
      // Calculate based on event type and actual data
      const capacity = event.capacity || 0;
      const isSocialEvent = event.eventType?.toLowerCase() === 'social';
      const currentCount = isSocialEvent ? (event.currentRSVPs || 0) : (event.currentTickets || 0);
      const available = capacity - currentCount;

      // Determine status type
      let statusType: 'available' | 'limited' | 'full' = 'available';
      if (available <= 0) {
        statusType = 'full';
      } else if (available <= 3) {
        statusType = 'limited';
      }

      // Generate status text
      let statusText = '';
      if (isSocialEvent) {
        if (available <= 0) {
          statusText = 'RSVPs Full';
        } else if (available <= 3) {
          statusText = `Only ${available} RSVP${available !== 1 ? 's' : ''} left!`;
        } else {
          statusText = `${currentCount}/${capacity} RSVPs`;
        }
      } else {
        if (available <= 0) {
          statusText = 'Sold Out';
        } else if (available <= 3) {
          statusText = `Only ${available} ticket${available !== 1 ? 's' : ''} left!`;
        } else {
          statusText = `${available} of ${capacity} tickets`;
        }
      }

      return { type: statusType, text: statusText };
    }
    return status;
  };

  const finalStatus = calculateStatus();
  const formatDate = (isoString: string) => {
    const date = new Date(isoString);
    return date.toLocaleDateString('en-US', {
      weekday: 'long',
      month: 'long',
      day: 'numeric',
      hour: 'numeric',
      minute: '2-digit',
    });
  };

  const getStatusColor = (type: string) => {
    switch (type) {
      case 'available':
        return 'var(--color-success)';
      case 'limited':
        return 'var(--color-warning)';
      case 'full':
        return 'var(--color-error)';
      default:
        return 'var(--color-success)';
    }
  };

  return (
    <Anchor
      component="div"
      underline="never"
      className="event-card"
      style={{
        display: 'block',
        background: 'white',
        borderRadius: '16px',
        overflow: 'hidden',
        boxShadow: '0 4px 12px rgba(0,0,0,0.05)',
        transition: 'all 0.3s ease',
        textDecoration: 'none',
        cursor: onClick ? 'pointer' : 'default',
      }}
      onClick={onClick}
      data-testid="event-card"
    >
      {/* Event Image Header */}
      <Box
        style={{
          height: '100px',
          background: 'linear-gradient(135deg, var(--color-plum) 0%, var(--color-burgundy) 100%)',
          position: 'relative',
          overflow: 'hidden',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          padding: 'var(--space-md)',
        }}
      >
        {/* Pattern overlay */}
        <Box
          style={{
            position: 'absolute',
            top: 0,
            left: 0,
            right: 0,
            bottom: 0,
            backgroundImage: "url(\"data:image/svg+xml,%3Csvg width='100' height='100' viewBox='0 0 100 100' xmlns='http://www.w3.org/2000/svg'%3E%3Cpath d='M10,50 Q30,20 50,50 T90,50' stroke='%23FFFFFF' stroke-width='0.5' fill='none' opacity='0.2'/%3E%3C/svg%3E\")",
            backgroundSize: '200px 200px',
          }}
        />

        <Text
          style={{
            fontFamily: 'var(--font-heading)',
            fontSize: '22px',
            fontWeight: 700,
            color: 'var(--color-ivory)',
            lineHeight: 1.3,
            textAlign: 'center',
            position: 'relative',
            zIndex: 1,
            textShadow: '0 2px 4px rgba(0, 0, 0, 0.3)',
          }}
          data-testid="event-title"
        >
          {event.title}
        </Text>
      </Box>

      {/* Event Content */}
      <Box style={{ padding: 'var(--space-lg)' }}>
        <Text
          style={{
            fontFamily: 'var(--font-heading)',
            fontSize: '14px',
            fontWeight: 600,
            color: 'var(--color-burgundy)',
            textTransform: 'uppercase',
            letterSpacing: '0.5px',
            marginBottom: 'var(--space-xs)',
          }}
        >
          {formatDate(event.startDate)}
        </Text>

        <Text
          style={{
            color: 'var(--color-stone)',
            fontSize: '15px',
            lineHeight: 1.6,
            marginBottom: 'var(--space-md)',
          }}
          data-testid="event-description"
        >
          {event.description}
        </Text>

        <Group
          gap="md"
          style={{
            fontSize: '14px',
            color: 'var(--color-smoke)',
            marginBottom: 'var(--space-md)',
            flexWrap: 'wrap',
          }}
        >
          <Group gap="xs">
            <span>📍</span>
            <span>{event.location || details.spots}</span>
          </Group>
          {details.duration && (
            <Group gap="xs">
              <span>⏱️</span>
              <span>{details.duration}</span>
            </Group>
          )}
          {details.level && (
            <Group gap="xs">
              <span>📚</span>
              <span>{details.level}</span>
            </Group>
          )}
        </Group>

        {/* Event Footer */}
        <Group
          justify="space-between"
          align="center"
          style={{
            paddingTop: 'var(--space-md)',
            borderTop: '1px solid var(--color-taupe)',
          }}
        >
          <Text
            style={{
              fontFamily: 'var(--font-heading)',
              fontSize: '18px',
              fontWeight: 700,
              color: 'var(--color-burgundy)',
            }}
          >
            {price}
          </Text>

          <Text
            style={{
              fontSize: '14px',
              fontWeight: 600,
              color: getStatusColor(finalStatus.type),
            }}
          >
            {finalStatus.text}
          </Text>
        </Group>
      </Box>
    </Anchor>
  );
};
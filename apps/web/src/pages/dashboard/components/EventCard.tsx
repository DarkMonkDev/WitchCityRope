import React from 'react';
import { Card, Text, Badge, Button, Box, Stack } from '@mantine/core';
import { Link } from 'react-router-dom';
import type { UserEventDto } from '../../../types/dashboard.types';

interface EventCardProps {
  event: UserEventDto;
  className?: string;
}

/**
 * Event card for user dashboard
 *
 * CRITICAL DIFFERENCES from Public Event Card:
 * - NO pricing information
 * - NO capacity/availability
 * - NO "Learn More" button
 * - USES "View Details" button
 * - Shows registration status badge
 */
export const EventCard: React.FC<EventCardProps> = ({ event, className }) => {
  const statusColors: Record<string, string> = {
    'RSVP Confirmed': 'blue',
    'Ticket Purchased': 'green',
    'Attended': 'grape',
  };

  const formatEventDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      weekday: 'long',
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    });
  };

  const formatEventTime = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleTimeString('en-US', {
      hour: 'numeric',
      minute: '2-digit',
      hour12: true,
    });
  };

  return (
    <Card
      shadow="sm"
      padding="0"
      radius="md"
      withBorder
      className={className}
      style={{
        height: '100%',
        display: 'flex',
        flexDirection: 'column',
        background: 'var(--color-ivory)',
        borderColor: 'rgba(183, 109, 117, 0.1)',
        transition: 'all 0.3s ease',
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
    >
      {/* Gradient Header */}
      <Box
        h={100}
        style={{
          background: 'linear-gradient(135deg, var(--color-plum) 0%, var(--color-burgundy) 100%)',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          padding: 'var(--space-md)',
        }}
      >
        <Text
          c="white"
          fw={700}
          size="lg"
          ta="center"
          px="md"
          style={{
            fontFamily: 'var(--font-heading)',
            lineHeight: 1.3,
            textShadow: '0 2px 4px rgba(0, 0, 0, 0.3)',
          }}
        >
          {event.title}
        </Text>
      </Box>

      <Stack gap="sm" p="lg" style={{ flex: 1, display: 'flex', flexDirection: 'column' }}>
        {/* Date/Time */}
        <Text
          fw={700}
          c="burgundy"
          size="sm"
          tt="uppercase"
          style={{
            fontFamily: 'var(--font-heading)',
            letterSpacing: '0.5px',
          }}
        >
          {formatEventDate(event.startDate)} • {formatEventTime(event.startDate)}
        </Text>

        {/* Location */}
        <Text size="sm" c="dimmed">
          📍 {event.location}
        </Text>

        {/* Description */}
        {event.description && (
          <Text size="sm" c="dimmed" style={{ flex: 1 }}>
            {event.description}
          </Text>
        )}

        {/* Status Badge */}
        <Badge color={statusColors[event.registrationStatus] || 'gray'} variant="light">
          {event.registrationStatus}
        </Badge>

        {/* Action Button */}
        <Button
          component={Link}
          to={`/events/${event.id}`}
          variant="outline"
          color="rose-gold"
          fullWidth
          mt="auto"
          style={{
            borderRadius: '12px 6px 12px 6px',
            fontFamily: 'var(--font-heading)',
            fontWeight: 600,
            textTransform: 'uppercase',
            letterSpacing: '1px',
            fontSize: '13px',
            transition: 'all 0.3s ease',
          }}
          onMouseEnter={(e) => {
            e.currentTarget.style.borderRadius = '6px 12px 6px 12px';
            e.currentTarget.style.background = 'var(--color-burgundy)';
            e.currentTarget.style.color = 'white';
          }}
          onMouseLeave={(e) => {
            e.currentTarget.style.borderRadius = '12px 6px 12px 6px';
            e.currentTarget.style.background = 'transparent';
            e.currentTarget.style.color = 'var(--color-burgundy)';
          }}
        >
          View Details
        </Button>
      </Stack>
    </Card>
  );
};

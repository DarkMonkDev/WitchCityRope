// UserParticipations component for dashboard
import React from 'react';
import {
  Paper, Stack, Title, Text, Badge, Group, Box, Button,
  Skeleton, Alert
} from '@mantine/core';
import {
  IconCalendarEvent, IconMapPin, IconClock, IconTicket,
  IconCalendarCheck, IconExternalLink
} from '@tabler/icons-react';
import { Link } from 'react-router-dom';
import { useUserParticipations } from '../../hooks/useParticipation';
import { UserParticipationDto, ParticipationType, ParticipationStatus } from '../../types/participation.types';

interface UserParticipationsProps {
  limit?: number;
  showPastEvents?: boolean;
}

export const UserParticipations: React.FC<UserParticipationsProps> = ({
  limit = 5,
  showPastEvents = false
}) => {
  const { data: participations, isLoading, error } = useUserParticipations();


  const getStatusColor = (status: ParticipationStatus) => {
    switch (status) {
      case ParticipationStatus.Active:
        return 'green';
      case ParticipationStatus.Cancelled:
        return 'gray';
      case ParticipationStatus.Refunded:
        return 'orange';
      case ParticipationStatus.Waitlisted:
        return 'yellow';
      default:
        return 'blue';
    }
  };

  const getTypeIcon = (type: ParticipationType) => {
    return type === ParticipationType.Ticket ? <IconTicket size={16} /> : <IconCalendarCheck size={16} />;
  };

  const formatEventDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      weekday: 'short',
      month: 'short',
      day: 'numeric',
      year: 'numeric'
    });
  };

  const formatEventTime = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleTimeString('en-US', {
      hour: 'numeric',
      minute: '2-digit',
      hour12: true
    });
  };

  // Filter participations
  const now = new Date();
  const filteredParticipations = participations
    ?.filter(p => {
      const eventDate = new Date(p.eventStartDate);
      return showPastEvents ? eventDate < now : eventDate >= now;
    })
    .slice(0, limit) || [];

  if (isLoading) {
    return (
      <Paper
        p="lg"
        style={{
          background: 'var(--color-ivory)',
          borderRadius: '16px',
          border: '1px solid rgba(183, 109, 117, 0.1)'
        }}
      >
        <Stack gap="md">
          <Skeleton height={28} width="60%" />
          {Array.from({ length: 3 }).map((_, i) => (
            <Skeleton key={i} height={60} />
          ))}
        </Stack>
      </Paper>
    );
  }

  if (error) {
    return (
      <Paper
        p="lg"
        style={{
          background: 'var(--color-ivory)',
          borderRadius: '16px',
          border: '1px solid rgba(183, 109, 117, 0.1)'
        }}
      >
        <Alert color="red" title="Unable to Load Participations">
          <Text size="sm">
            We couldn't load your event participations. Please try refreshing the page.
          </Text>
        </Alert>
      </Paper>
    );
  }

  return (
    <Paper
      p="lg"
      style={{
        background: 'var(--color-ivory)',
        borderRadius: '16px',
        border: '1px solid rgba(183, 109, 117, 0.1)',
        boxShadow: '0 4px 12px rgba(0,0,0,0.05)'
      }}
    >
      <Stack gap="lg">
        {/* Header */}
        <Group justify="space-between" align="center">
          <Title
            order={3}
            style={{
              fontFamily: 'var(--font-heading)',
              fontSize: '20px',
              fontWeight: 700,
              color: 'var(--color-burgundy)'
            }}
          >
            {showPastEvents ? 'Past Events' : 'Your Upcoming Events'}
          </Title>
          {filteredParticipations.length > 0 && (
            <Button
              component={Link}
              to="/dashboard/events"
              variant="subtle"
              size="sm"
              rightSection={<IconExternalLink size={14} />}
            >
              View History
            </Button>
          )}
        </Group>

        {/* Content */}
        {filteredParticipations.length === 0 ? (
          <Box ta="center" py="xl">
            <IconCalendarEvent size={48} color="var(--color-stone)" style={{ marginBottom: '16px' }} />
            <Text size="lg" fw={600} c="var(--color-stone)" mb="sm">
              {showPastEvents ? 'No Past Events' : 'No Upcoming Events'}
            </Text>
            <Text size="sm" c="dimmed" mb="lg">
              {showPastEvents
                ? "You haven't attended any events yet."
                : "You don't have any upcoming events. Browse our events to find something interesting!"
              }
            </Text>
            {!showPastEvents && (
              <Button
                component={Link}
                to="/events"
                className="btn btn-primary"
                leftSection={<IconCalendarEvent size={16} />}
              >
                Browse Events
              </Button>
            )}
          </Box>
        ) : (
          <Stack gap="sm">
            {filteredParticipations.map((participation) => (
              <Box
                key={participation.id}
                component={Link}
                to={`/events/${participation.eventId}`}
                style={{
                  background: 'var(--color-cream)',
                  borderRadius: '12px',
                  padding: 'var(--space-md)',
                  border: '1px solid var(--color-taupe)',
                  transition: 'all 0.2s ease',
                  textDecoration: 'none',
                  color: 'inherit',
                  display: 'block',
                  cursor: 'pointer'
                }}
                onMouseEnter={(e) => {
                  e.currentTarget.style.borderColor = 'var(--color-burgundy)';
                  e.currentTarget.style.boxShadow = '0 2px 8px rgba(0,0,0,0.08)';
                  e.currentTarget.style.transform = 'translateY(-1px)';
                }}
                onMouseLeave={(e) => {
                  e.currentTarget.style.borderColor = 'var(--color-taupe)';
                  e.currentTarget.style.boxShadow = 'none';
                  e.currentTarget.style.transform = 'translateY(0)';
                }}
              >
                <Group justify="space-between" align="flex-start" wrap="nowrap">
                  {/* Event Info */}
                  <Box style={{ flex: 1, minWidth: 0 }}>
                    <Group gap="sm" mb="xs">
                      <Badge
                        color={getStatusColor(participation.status)}
                        variant="filled"
                        size="sm"
                        leftSection={getTypeIcon(participation.participationType)}
                        style={{ borderRadius: '12px 6px 12px 6px' }}
                      >
                        {participation.participationType}
                      </Badge>

                      <Badge
                        color={getStatusColor(participation.status)}
                        variant="light"
                        size="sm"
                      >
                        {participation.status}
                      </Badge>
                    </Group>

                    <Text
                      fw={600}
                      size="md"
                      c="var(--color-charcoal)"
                      mb="xs"
                      truncate
                    >
                      {participation.eventTitle}
                    </Text>

                    <Stack gap="xs">
                      <Group gap="sm" wrap="nowrap">
                        <IconClock size={14} color="var(--color-stone)" />
                        <Text size="sm" c="dimmed" truncate>
                          {formatEventDate(participation.eventStartDate)} at {formatEventTime(participation.eventStartDate)}
                        </Text>
                      </Group>

                      <Group gap="sm" wrap="nowrap">
                        <IconMapPin size={14} color="var(--color-stone)" />
                        <Text size="sm" c="dimmed" truncate>
                          {participation.eventLocation}
                        </Text>
                      </Group>
                    </Stack>
                  </Box>
                </Group>
              </Box>
            ))}
          </Stack>
        )}
      </Stack>
    </Paper>
  );
};
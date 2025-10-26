// UserVolunteerShifts component for dashboard
import React from 'react';
import {
  Paper, Stack, Title, Text, Badge, Group, Box, Button, Skeleton, Alert
} from '@mantine/core';
import {
  IconCalendarEvent, IconMapPin, IconClock, IconHeart, IconExternalLink
} from '@tabler/icons-react';
import { Link } from 'react-router-dom';

// Type for volunteer shift with event details
export interface VolunteerShiftWithEvent {
  id: string;
  eventId: string;
  eventTitle: string;
  eventStartDate: string;
  eventLocation: string;
  positionTitle: string;
  sessionName?: string;
  sessionStartTime?: string;
  sessionEndTime?: string;
}

interface UserVolunteerShiftsProps {
  shifts: VolunteerShiftWithEvent[];
  isLoading?: boolean;
  error?: Error | null;
}

/**
 * User Volunteer Shifts Component for Dashboard
 * Displays upcoming volunteer shifts the user has signed up for
 * Placement: Above "Your Upcoming Events" section on dashboard
 */
export const UserVolunteerShifts: React.FC<UserVolunteerShiftsProps> = ({
  shifts,
  isLoading = false,
  error = null
}) => {
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

  const formatShiftTime = (timeString?: string) => {
    if (!timeString) return '';
    try {
      const date = new Date(timeString);
      const formatted = date.toLocaleTimeString('en-US', {
        hour: 'numeric',
        minute: '2-digit',
        hour12: true
      });
      return formatted.replace(/AM|PM/g, match => match.toLowerCase());
    } catch {
      return timeString;
    }
  };

  // Filter to only show upcoming shifts
  const now = new Date();
  const upcomingShifts = shifts.filter(shift => {
    const eventDate = new Date(shift.eventStartDate);
    return eventDate >= now;
  });

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
          {Array.from({ length: 2 }).map((_, i) => (
            <Skeleton key={i} height={80} />
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
        <Alert color="red" title="Unable to Load Volunteer Shifts">
          <Text size="sm">
            We couldn't load your volunteer shifts. Please try refreshing the page.
          </Text>
        </Alert>
      </Paper>
    );
  }

  // Don't render the section if there are no upcoming shifts
  if (upcomingShifts.length === 0) {
    return null;
  }

  return (
    <Paper
      p="lg"
      style={{
        background: 'linear-gradient(135deg, rgba(155, 74, 117, 0.05) 0%, rgba(136, 1, 36, 0.05) 100%)',
        borderRadius: '16px',
        border: '1px solid rgba(183, 109, 117, 0.2)',
        boxShadow: '0 4px 12px rgba(0,0,0,0.05)'
      }}
    >
      <Stack gap="lg">
        {/* Header */}
        <Group justify="space-between" align="center">
          <Group gap="sm">
            <div
              style={{
                width: '36px',
                height: '36px',
                borderRadius: '50%',
                background: 'var(--color-plum)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center'
              }}
            >
              <IconHeart size={20} color="var(--color-ivory)" />
            </div>
            <Title
              order={3}
              style={{
                fontFamily: 'var(--font-heading)',
                fontSize: '20px',
                fontWeight: 700,
                color: 'var(--color-burgundy)'
              }}
            >
              Your Volunteer Shifts
            </Title>
          </Group>
        </Group>

        {/* Shifts List */}
        <Stack gap="sm">
          {upcomingShifts.map((shift) => (
            <Box
              key={shift.id}
              component={Link}
              to={`/events/${shift.eventId}`}
              style={{
                background: 'white',
                borderRadius: '12px',
                padding: 'var(--space-md)',
                border: '1px solid var(--color-stone-light)',
                transition: 'all 0.2s ease',
                textDecoration: 'none',
                color: 'inherit',
                display: 'block',
                cursor: 'pointer'
              }}
              onMouseEnter={(e) => {
                e.currentTarget.style.borderColor = 'var(--color-plum)';
                e.currentTarget.style.boxShadow = '0 2px 8px rgba(155, 74, 117, 0.15)';
                e.currentTarget.style.transform = 'translateY(-1px)';
              }}
              onMouseLeave={(e) => {
                e.currentTarget.style.borderColor = 'var(--color-stone-light)';
                e.currentTarget.style.boxShadow = 'none';
                e.currentTarget.style.transform = 'translateY(0)';
              }}
            >
              <Stack gap="xs">
                {/* Event Title and Position */}
                <Group justify="space-between" align="flex-start" wrap="nowrap">
                  <Box style={{ flex: 1, minWidth: 0 }}>
                    <Text
                      fw={600}
                      size="md"
                      c="var(--color-charcoal)"
                      mb={4}
                      truncate
                    >
                      {shift.eventTitle}
                    </Text>
                    <Group gap="xs" wrap="wrap">
                      <Badge
                        color="purple"
                        variant="filled"
                        size="sm"
                        style={{ borderRadius: '12px 6px 12px 6px' }}
                      >
                        {shift.positionTitle}
                      </Badge>
                      {shift.sessionName && (
                        <Badge size="sm" color="gray" variant="light">
                          {shift.sessionName}
                        </Badge>
                      )}
                    </Group>
                  </Box>
                </Group>

                {/* Event Date, Time, and Location */}
                <Stack gap="xs" mt="xs">
                  <Group gap="sm" wrap="nowrap">
                    <IconClock size={14} color="var(--color-stone)" />
                    <Text size="sm" c="dimmed" truncate>
                      {formatEventDate(shift.eventStartDate)} at {formatEventTime(shift.eventStartDate)}
                    </Text>
                  </Group>

                  {shift.sessionStartTime && shift.sessionEndTime && (
                    <Group gap="sm" wrap="nowrap">
                      <IconClock size={14} color="var(--color-plum)" />
                      <Text size="sm" c="dimmed" truncate>
                        Shift: {formatShiftTime(shift.sessionStartTime)} - {formatShiftTime(shift.sessionEndTime)}
                      </Text>
                    </Group>
                  )}

                  <Group gap="sm" wrap="nowrap">
                    <IconMapPin size={14} color="var(--color-stone)" />
                    <Text size="sm" c="dimmed" truncate>
                      {shift.eventLocation}
                    </Text>
                  </Group>
                </Stack>
              </Stack>
            </Box>
          ))}
        </Stack>

        {/* Thank you message */}
        <Box
          p="md"
          style={{
            background: 'rgba(255,255,255,0.6)',
            borderRadius: '8px',
            textAlign: 'center'
          }}
        >
          <Text size="sm" c="dimmed" fw={500}>
            Thank you for volunteering! Your help makes our community events possible. 💜
          </Text>
        </Box>
      </Stack>
    </Paper>
  );
};

import React from 'react';
import { Paper, Stack, Title, Text, Badge, Group } from '@mantine/core';
import { IconCheck, IconClock } from '@tabler/icons-react';
import type { VolunteerPosition } from '../../features/volunteers/types/volunteer.types';

interface UserVolunteerShiftsProps {
  positions: VolunteerPosition[];
}

/**
 * User Volunteer Shifts Component
 * Displays the shifts the user has signed up for on an event
 * Shows only when user has already volunteered for this event
 */
export const UserVolunteerShifts: React.FC<UserVolunteerShiftsProps> = ({ positions }) => {
  const formatTime = (timeString?: string) => {
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

  return (
    <Paper
      style={{
        background: 'var(--color-ivory)',
        borderRadius: '16px',
        padding: 'var(--space-lg)',
        boxShadow: '0 4px 12px rgba(0,0,0,0.05)',
        border: '1px solid rgba(34, 139, 34, 0.2)'
      }}
    >
      <Stack gap="md">
        {/* Header with success badge */}
        <Group gap="sm" align="center">
          <div
            style={{
              width: '36px',
              height: '36px',
              borderRadius: '50%',
              background: 'var(--color-success-light, rgba(34, 139, 34, 0.1))',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center'
            }}
          >
            <IconCheck size={20} color="var(--color-success, #228B22)" />
          </div>
          <Title
            order={3}
            style={{
              fontFamily: 'var(--font-heading)',
              fontSize: '18px',
              fontWeight: 700,
              color: 'var(--color-success, #228B22)'
            }}
          >
            You're Volunteering!
          </Title>
        </Group>

        {/* List of positions */}
        <Stack gap="xs">
          {positions.map((position) => (
            <div
              key={position.id}
              style={{
                background: 'white',
                borderRadius: '8px',
                padding: 'var(--space-sm)',
                border: '1px solid var(--color-stone-light)'
              }}
            >
              <Group justify="space-between" align="flex-start" wrap="nowrap">
                <Stack gap={4} style={{ flex: 1 }}>
                  <Group gap="xs" wrap="wrap">
                    <Text
                      size="sm"
                      fw={600}
                      style={{ color: 'var(--color-charcoal)' }}
                    >
                      {position.title}
                    </Text>
                    {position.sessionName && (
                      <Badge size="xs" color="gray" variant="light">
                        {position.sessionName}
                      </Badge>
                    )}
                  </Group>

                  {position.sessionStartTime && position.sessionEndTime && (
                    <Group gap="xs">
                      <IconClock size={12} color="var(--color-stone)" />
                      <Text size="xs" c="dimmed">
                        {formatTime(position.sessionStartTime)} - {formatTime(position.sessionEndTime)}
                      </Text>
                    </Group>
                  )}
                </Stack>
              </Group>
            </div>
          ))}
        </Stack>

        {/* Thank you message */}
        <Text
          size="sm"
          c="dimmed"
          ta="center"
          style={{ paddingTop: 'var(--space-xs)' }}
        >
          Thank you for helping make this event possible!
        </Text>
      </Stack>
    </Paper>
  );
};

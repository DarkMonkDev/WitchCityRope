import React from 'react';
import { Table, Text, Group } from '@mantine/core';
import { WCRButton } from '../ui';
import type { components } from '@witchcityrope/shared-types';
import { useEventTimeZone } from '../../hooks/useEventTimeZone';

// Use auto-generated SessionDto from backend instead of manual interface
export type EventSession = components['schemas']['SessionDto'];

interface EventSessionsGridProps {
  sessions: EventSession[];
  onEditSession: (sessionId: string) => void;
  onDeleteSession: (sessionId: string) => void;
  onAddSession: () => void;
}

export const EventSessionsGrid: React.FC<EventSessionsGridProps> = ({
  sessions,
  onEditSession,
  onDeleteSession,
  onAddSession,
}) => {
  // Get the configured event timezone from admin settings
  const eventTimeZone = useEventTimeZone();

  const formatDate = (dateString: string) => {
    // session.date is a date-only field (extracted from StartTime.Date on backend)
    // Do NOT apply timezone conversion - just extract YYYY-MM-DD
    const datePart = dateString.split('T')[0]; // Extract YYYY-MM-DD
    const [year, month, day] = datePart.split('-').map(Number);
    const date = new Date(year, month - 1, day);
    return date.toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric'
    });
  };

  const formatTime = (timeString: string) => {
    // Display in configured event timezone (not user's local timezone)
    const date = new Date(timeString);
    return date.toLocaleTimeString('en-US', {
      hour: 'numeric',
      minute: '2-digit',
      hour12: true,
      timeZone: eventTimeZone
    });
  };

  const getSoldDisplay = (sold?: number, capacity?: number) => {
    if (!sold && sold !== 0) return { text: '0', color: 'inherit' };
    if (!capacity) return { text: sold.toString(), color: 'inherit' };

    const percentage = (sold / capacity) * 100;

    if (percentage === 100) {
      return { text: `${sold} - Sold Out`, color: 'var(--mantine-color-red-6)' };
    }
    return { text: sold.toString(), color: 'inherit' };
  };

  return (
    <div>
      <Text size="sm" c="dimmed" mb="lg">
        Define the individual sessions for your event. Click on a row to edit session details.
        The system auto-detects single vs multi-day events based on session count.
      </Text>

      <Table
        striped
        highlightOnHover
        withTableBorder
        className="wcr-data-table"
        data-testid="grid-sessions"
        style={{
          backgroundColor: 'white',
          borderRadius: '12px',
          overflow: 'hidden',
          boxShadow: '0 4px 20px rgba(0,0,0,0.08)',
        }}
      >
        <Table.Thead style={{ backgroundColor: 'var(--mantine-color-burgundy-6)' }}>
          <Table.Tr>
            <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px' }}>
              S#
            </Table.Th>
            <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px' }}>
              Name
            </Table.Th>
            <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px' }}>
              Date
            </Table.Th>
            <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px' }}>
              Start Time
            </Table.Th>
            <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px' }}>
              End Time
            </Table.Th>
            <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px' }}>
              Capacity
            </Table.Th>
            <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px' }}>
              Sold
            </Table.Th>
          </Table.Tr>
        </Table.Thead>
        <Table.Tbody>
          {sessions.map((session) => (
            <Table.Tr
              key={session.id}
              data-testid="session-row"
              onClick={() => session.id && onEditSession(session.id)}
              style={{ cursor: 'pointer' }}
            >
              <Table.Td>
                <Text fw={700} size="sm" data-testid="session-id">
                  {session.sessionIdentifier || 'N/A'}
                </Text>
              </Table.Td>
              <Table.Td>
                <Text size="sm" data-testid="session-name">{session.name || 'N/A'}</Text>
              </Table.Td>
              <Table.Td>
                <Text size="sm">{session.date ? formatDate(session.date) : 'N/A'}</Text>
              </Table.Td>
              <Table.Td>
                <Text size="sm">{session.startTime ? formatTime(session.startTime) : 'N/A'}</Text>
              </Table.Td>
              <Table.Td>
                <Text size="sm">{session.endTime ? formatTime(session.endTime) : 'N/A'}</Text>
              </Table.Td>
              <Table.Td>
                <Text size="sm">{session.capacity ?? 0}</Text>
              </Table.Td>
              <Table.Td>
                <Text
                  fw={700}
                  size="sm"
                  style={{ color: getSoldDisplay(session.registrationCount, session.capacity).color }}
                >
                  {getSoldDisplay(session.registrationCount, session.capacity).text}
                </Text>
              </Table.Td>
            </Table.Tr>
          ))}
          {sessions.length === 0 && (
            <Table.Tr>
              <Table.Td colSpan={7}>
                <Text ta="center" c="dimmed" py="xl">
                  No sessions created yet. Click "Add Session" to get started.
                </Text>
              </Table.Td>
            </Table.Tr>
          )}
        </Table.Tbody>
      </Table>

      <Group mt="md">
        <WCRButton
          variant="secondary"
          size="lg"
          onClick={onAddSession}
          data-testid="button-add-session"
        >
          Add Session
        </WCRButton>
      </Group>
    </div>
  );
};
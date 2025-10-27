import React from 'react';
import { Table, Text, Group } from '@mantine/core';
import { WCRButton } from '../ui';

export interface EventSession {
  id: string;
  sessionIdentifier: string; // S1, S2, S3, etc.
  name: string;
  date: string;
  startTime: string;
  endTime: string;
  capacity: number;
  registeredCount: number;
}

export interface EventTicketTypeForSold {
  id: string;
  sessionIdentifiers: string[];
  quantityAvailable?: number;
}

interface EventSessionsGridProps {
  sessions: EventSession[];
  ticketTypes?: EventTicketTypeForSold[];
  onEditSession: (sessionId: string) => void;
  onDeleteSession: (sessionId: string) => void;
  onAddSession: () => void;
}

export const EventSessionsGrid: React.FC<EventSessionsGridProps> = ({
  sessions,
  ticketTypes = [],
  onEditSession,
  onDeleteSession,
  onAddSession,
}) => {
  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric'
    });
  };

  const formatTime = (timeString: string) => {
    // Assuming timeString is in HH:MM format
    const [hours, minutes] = timeString.split(':');
    const hour = parseInt(hours);
    const ampm = hour >= 12 ? 'PM' : 'AM';
    const displayHour = hour % 12 || 12;
    return `${displayHour}:${minutes} ${ampm}`;
  };

  // Calculate total tickets sold that include this session
  const getTicketsSoldForSession = (sessionIdentifier: string): number => {
    if (!ticketTypes || ticketTypes.length === 0) return 0;

    return ticketTypes
      .filter(tt => tt.sessionIdentifiers.includes(sessionIdentifier))
      .reduce((total, tt) => {
        // quantityAvailable is what's left - we need sold tickets
        // For now, return 0 as we don't have sold data in the form
        // This will need to be updated when we have actual ticket sales data
        return total + 0;
      }, 0);
  };

  const getSoldColor = (sold: number, capacity: number) => {
    const percentage = (sold / capacity) * 100;
    if (percentage >= 75) return 'var(--mantine-color-amber-6)'; // Warning color
    return 'inherit';
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
          {sessions.map((session) => {
            const ticketsSold = getTicketsSoldForSession(session.sessionIdentifier);
            return (
              <Table.Tr
                key={session.id}
                data-testid="session-row"
                onClick={() => onEditSession(session.id)}
                style={{ cursor: 'pointer' }}
              >
                <Table.Td>
                  <Text fw={700} size="sm" data-testid="session-id">
                    {session.sessionIdentifier}
                  </Text>
                </Table.Td>
                <Table.Td>
                  <Text size="sm" data-testid="session-name">{session.name}</Text>
                </Table.Td>
                <Table.Td>
                  <Text size="sm">{formatDate(session.date)}</Text>
                </Table.Td>
                <Table.Td>
                  <Text size="sm">{formatTime(session.startTime)}</Text>
                </Table.Td>
                <Table.Td>
                  <Text size="sm">{formatTime(session.endTime)}</Text>
                </Table.Td>
                <Table.Td>
                  <Text size="sm">{session.capacity}</Text>
                </Table.Td>
                <Table.Td>
                  <Text
                    fw={700}
                    size="sm"
                    style={{ color: getSoldColor(ticketsSold, session.capacity) }}
                  >
                    {ticketsSold}
                  </Text>
                </Table.Td>
              </Table.Tr>
            );
          })}
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

      <Group mt="md" justify="space-between" align="center">
        <WCRButton
          variant="secondary"
          size="lg"
          onClick={onAddSession}
          data-testid="button-add-session"
        >
          Add Session
        </WCRButton>

        <Text size="xs" c="dimmed" fs="italic">
          💡 Tip: Click on a row to edit session details in a modal dialog.
        </Text>
      </Group>
    </div>
  );
};
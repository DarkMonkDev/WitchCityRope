import React, { useMemo } from 'react';
import { Table, Text, Group, ActionIcon } from '@mantine/core';
import { IconTrash } from '@tabler/icons-react';
import { WCRButton } from '../ui';
import type { components } from '@witchcityrope/shared-types';
import { formatUtcToLocalTime } from '../../utils/eventUtils';

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
  // Sort sessions by session number (S1, S2, S3...) extracted from sessionIdentifier
  const sortedSessions = useMemo(() => {
    return [...sessions].sort((a, b) => {
      const numA = parseInt(a.sessionIdentifier?.replace(/\D/g, '') || '0', 10);
      const numB = parseInt(b.sessionIdentifier?.replace(/\D/g, '') || '0', 10);
      return numA - numB;
    });
  }, [sessions]);

  const formatDate = (dateString: string) => {
    // session.startDate is a date-only field (local date extracted from UTC StartTime on backend)
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

  const formatDateRange = (startDateString: string, endDateString?: string | null) => {
    // If no end date or end date equals start date, show single date
    if (!endDateString) {
      return formatDate(startDateString);
    }

    const startDatePart = startDateString.split('T')[0];
    const endDatePart = endDateString.split('T')[0];

    // If dates are the same, show single date
    if (startDatePart === endDatePart) {
      return formatDate(startDateString);
    }

    // Multi-day session: show date range
    const [startYear, startMonth, startDay] = startDatePart.split('-').map(Number);
    const [endYear, endMonth, endDay] = endDatePart.split('-').map(Number);

    const startDate = new Date(startYear, startMonth - 1, startDay);
    const endDate = new Date(endYear, endMonth - 1, endDay);

    // Check if same year
    if (startYear === endYear) {
      // Same year: "Dec 3 - 4, 2025" or "Nov 30 - Dec 1, 2025"
      const startFormatted = startDate.toLocaleDateString('en-US', {
        month: 'short',
        day: 'numeric'
      });
      const endFormatted = endDate.toLocaleDateString('en-US', {
        month: 'short',
        day: 'numeric',
        year: 'numeric'
      });
      return `${startFormatted} - ${endFormatted}`;
    } else {
      // Different years: "Dec 31, 2025 - Jan 1, 2026"
      const startFormatted = formatDate(startDateString);
      const endFormatted = formatDate(endDateString);
      return `${startFormatted} - ${endFormatted}`;
    }
  };

  const formatTime = (timeString: string) => {
    // NEW: Use true UTC to local time conversion
    // This converts from actual UTC (stored in database) to local time (America/New_York)
    return formatUtcToLocalTime(timeString);
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
            <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px', textAlign: 'center' }}>
              Actions
            </Table.Th>
          </Table.Tr>
        </Table.Thead>
        <Table.Tbody>
          {sortedSessions.map((session) => (
            <Table.Tr
              key={session.id}
              data-testid="session-row"
            >
              <Table.Td
                onClick={() => session.id && onEditSession(session.id)}
                style={{ cursor: 'pointer' }}
              >
                <Text fw={700} size="sm" data-testid="session-id">
                  {session.sessionIdentifier || 'N/A'}
                </Text>
              </Table.Td>
              <Table.Td
                onClick={() => session.id && onEditSession(session.id)}
                style={{ cursor: 'pointer' }}
              >
                <Text size="sm" data-testid="session-name">{session.name || 'N/A'}</Text>
              </Table.Td>
              <Table.Td
                onClick={() => session.id && onEditSession(session.id)}
                style={{ cursor: 'pointer' }}
              >
                <Text size="sm">{session.startDate ? formatDateRange(session.startDate, session.endDate) : 'N/A'}</Text>
              </Table.Td>
              <Table.Td
                onClick={() => session.id && onEditSession(session.id)}
                style={{ cursor: 'pointer' }}
              >
                <Text size="sm">{session.startTime ? formatTime(session.startTime) : 'N/A'}</Text>
              </Table.Td>
              <Table.Td
                onClick={() => session.id && onEditSession(session.id)}
                style={{ cursor: 'pointer' }}
              >
                <Text size="sm">{session.endTime ? formatTime(session.endTime) : 'N/A'}</Text>
              </Table.Td>
              <Table.Td
                onClick={() => session.id && onEditSession(session.id)}
                style={{ cursor: 'pointer' }}
              >
                <Text size="sm">{session.capacity ?? 0}</Text>
              </Table.Td>
              <Table.Td
                onClick={() => session.id && onEditSession(session.id)}
                style={{ cursor: 'pointer' }}
              >
                <Text
                  fw={700}
                  size="sm"
                  style={{ color: getSoldDisplay(session.registrationCount, session.capacity).color }}
                >
                  {getSoldDisplay(session.registrationCount, session.capacity).text}
                </Text>
              </Table.Td>
              <Table.Td style={{ textAlign: 'center' }}>
                <ActionIcon
                  data-testid="button-delete-session"
                  variant="subtle"
                  color="gray"
                  onClick={(e) => {
                    e.stopPropagation();
                    session.id && onDeleteSession(session.id);
                  }}
                  aria-label={`Delete ${session.name}`}
                  styles={{
                    root: {
                      '&:hover': { color: '#DC143C' }
                    }
                  }}
                >
                  <IconTrash size={20} />
                </ActionIcon>
              </Table.Td>
            </Table.Tr>
          ))}
          {sortedSessions.length === 0 && (
            <Table.Tr>
              <Table.Td colSpan={8}>
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
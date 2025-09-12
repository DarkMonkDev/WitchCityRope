import React from 'react';
import { Table, Text, Group, ActionIcon } from '@mantine/core';
import { IconEdit, IconTrash } from '@tabler/icons-react';
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
  const handleDeleteClick = (sessionId: string, sessionName: string) => {
    if (window.confirm(`Are you sure you want to delete session "${sessionName}"?`)) {
      onDeleteSession(sessionId);
    }
  };

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

  const getSoldColor = (sold: number, capacity: number) => {
    const percentage = (sold / capacity) * 100;
    if (percentage >= 75) return 'var(--mantine-color-amber-6)'; // Warning color
    return 'inherit';
  };

  return (
    <div>
      <Text size="sm" c="dimmed" mb="lg">
        Define the individual sessions for your event. Click Edit to modify session details. 
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
              Actions
            </Table.Th>
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
            <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px' }}>
              Delete
            </Table.Th>
          </Table.Tr>
        </Table.Thead>
        <Table.Tbody>
          {sessions.map((session) => (
            <Table.Tr key={session.id} data-testid="session-row">
              <Table.Td>
                <WCRButton
                  size="compact-xs"
                  variant="outline"
                  leftSection={<IconEdit size={14} />}
                  onClick={() => onEditSession(session.id)}
                  data-testid="button-edit-session"
                >
                  Edit
                </WCRButton>
              </Table.Td>
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
                  style={{ color: getSoldColor(session.registeredCount, session.capacity) }}
                >
                  {session.registeredCount}
                </Text>
              </Table.Td>
              <Table.Td>
                <ActionIcon
                  variant="filled"
                  color="red"
                  size="sm"
                  onClick={() => handleDeleteClick(session.id, session.name)}
                  data-testid="button-delete-session"
                >
                  <IconTrash size={14} />
                </ActionIcon>
              </Table.Td>
            </Table.Tr>
          ))}
          {sessions.length === 0 && (
            <Table.Tr>
              <Table.Td colSpan={9}>
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
          💡 Tip: Click Edit to modify session details in a modal dialog.
        </Text>
      </Group>
    </div>
  );
};
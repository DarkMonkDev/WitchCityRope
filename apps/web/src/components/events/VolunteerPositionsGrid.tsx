import React from 'react';
import { Table, Text, Group, ActionIcon, Badge } from '@mantine/core';
import { IconEdit, IconTrash } from '@tabler/icons-react';
import { WCRButton } from '../ui';
import type { VolunteerPosition } from './VolunteerPositionFormModal';

interface VolunteerPositionsGridProps {
  positions: VolunteerPosition[];
  onEditPosition: (positionId: string) => void;
  onDeletePosition: (positionId: string) => void;
  onAddPosition: () => void;
}

export const VolunteerPositionsGrid: React.FC<VolunteerPositionsGridProps> = ({
  positions,
  onEditPosition,
  onDeletePosition,
  onAddPosition,
}) => {
  const handleDeleteClick = (positionId: string, positionTitle: string) => {
    if (window.confirm(`Are you sure you want to delete the "${positionTitle}" position?`)) {
      onDeletePosition(positionId);
    }
  };

  const formatTime = (timeString: string) => {
    // Assuming timeString is in HH:MM format
    const [hours, minutes] = timeString.split(':');
    const hour = parseInt(hours);
    const ampm = hour >= 12 ? 'PM' : 'AM';
    const displayHour = hour % 12 || 12;
    return `${displayHour}:${minutes} ${ampm}`;
  };

  const getAssignmentStatus = (assigned: number, needed: number) => {
    if (assigned === 0) {
      return { text: `${needed} positions open`, color: 'red', variant: 'light' as const };
    } else if (assigned < needed) {
      return { text: `${needed - assigned} more needed`, color: 'yellow', variant: 'light' as const };
    } else {
      return { text: 'Fully staffed', color: 'green', variant: 'light' as const };
    }
  };

  return (
    <div>
      <Text size="sm" c="dimmed" mb="lg">
        Define volunteer positions needed for your event. Use the session filter to assign positions to specific sessions.
      </Text>

      <Table
        striped
        highlightOnHover
        withTableBorder
        data-testid="grid-volunteer-positions"
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
              Edit
            </Table.Th>
            <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px' }}>
              Position
            </Table.Th>
            <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px' }}>
              Sessions
            </Table.Th>
            <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px' }}>
              Time
            </Table.Th>
            <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px' }}>
              Description
            </Table.Th>
            <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px' }}>
              Needed
            </Table.Th>
            <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px' }}>
              Assigned
            </Table.Th>
            <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px' }}>
              Delete
            </Table.Th>
          </Table.Tr>
        </Table.Thead>
        <Table.Tbody>
          {positions.map((position) => {
            const status = getAssignmentStatus(position.slotsFilled, position.slotsNeeded);
            
            return (
              <Table.Tr key={position.id} data-testid="position-row">
                <Table.Td>
                  <WCRButton
                    size="compact-xs"
                    variant="outline"
                    leftSection={<IconEdit size={14} />}
                    onClick={() => onEditPosition(position.id)}
                    data-testid="button-edit-volunteer-position"
                  >
                    Edit
                  </WCRButton>
                </Table.Td>
                <Table.Td>
                  <Text fw={600} c="burgundy" data-testid="position-title">
                    {position.title}
                  </Text>
                </Table.Td>
                <Table.Td>
                  <Text fw={600} data-testid="position-sessions">
                    {position.sessions}
                  </Text>
                </Table.Td>
                <Table.Td>
                  <Text size="sm" c="stone">
                    {formatTime(position.startTime)} - {formatTime(position.endTime)}
                  </Text>
                </Table.Td>
                <Table.Td>
                  <Text size="sm" style={{ maxWidth: '200px', wordWrap: 'break-word' }}>
                    {position.description}
                  </Text>
                </Table.Td>
                <Table.Td style={{ textAlign: 'center', fontWeight: 600 }}>
                  <Text fw={600} data-testid="slots-needed">
                    {position.slotsNeeded}
                  </Text>
                </Table.Td>
                <Table.Td>
                  <Badge color={status.color} variant={status.variant}>
                    {status.text}
                  </Badge>
                </Table.Td>
                <Table.Td>
                  <ActionIcon
                    variant="filled"
                    color="red"
                    size="sm"
                    onClick={() => handleDeleteClick(position.id, position.title)}
                    data-testid="button-delete-volunteer-position"
                  >
                    <IconTrash size={14} />
                  </ActionIcon>
                </Table.Td>
              </Table.Tr>
            );
          })}
          {positions.length === 0 && (
            <Table.Tr>
              <Table.Td colSpan={8}>
                <Text ta="center" c="dimmed" py="xl">
                  No volunteer positions created yet. Click "Add New Position" to get started.
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
          onClick={onAddPosition}
          data-testid="button-add-volunteer-position"
        >
          Add New Position
        </WCRButton>
        
        <Text size="xs" c="dimmed" fs="italic">
          💡 Tip: Click Edit to modify position details in a modal dialog.
        </Text>
      </Group>
    </div>
  );
};
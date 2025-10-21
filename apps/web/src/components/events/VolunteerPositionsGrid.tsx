import React, { useState } from 'react';
import { Table, Text, Group, Badge, Collapse, Stack } from '@mantine/core';
import { WCRButton } from '../ui';
import { CapacityDisplay } from './CapacityDisplay';
import { VolunteerPositionInlineForm } from './VolunteerPositionInlineForm';
import { AssignedMembersSection } from './AssignedMembersSection';
import type { VolunteerPosition } from './VolunteerPositionFormModal';

interface VolunteerPositionsGridProps {
  positions: VolunteerPosition[];
  onPositionSubmit: (positionData: Omit<VolunteerPosition, 'id' | 'slotsFilled'>, positionId?: string) => void;
  onDeletePosition: (positionId: string) => void;
  availableSessions: Array<{ sessionIdentifier: string; name: string }>;
}

export const VolunteerPositionsGrid: React.FC<VolunteerPositionsGridProps> = ({
  positions,
  onPositionSubmit,
  onDeletePosition,
  availableSessions,
}) => {
  const [selectedPositionId, setSelectedPositionId] = useState<string | null>(null);
  const [isEditAreaOpen, setIsEditAreaOpen] = useState(false);
  const [editMode, setEditMode] = useState<'create' | 'edit'>('create');

  // Find the currently editing position
  const editingPosition = selectedPositionId
    ? positions.find(p => p.id === selectedPositionId)
    : null;

  const formatTime = (timeString: string) => {
    // Assuming timeString is in HH:MM format
    const [hours, minutes] = timeString.split(':');
    const hour = parseInt(hours);
    const ampm = hour >= 12 ? 'PM' : 'AM';
    const displayHour = hour % 12 || 12;
    return `${displayHour}:${minutes} ${ampm}`;
  };

  const handleRowClick = (positionId: string) => {
    // If clicking the same row that's already open, close it
    if (selectedPositionId === positionId && isEditAreaOpen) {
      setIsEditAreaOpen(false);
      setSelectedPositionId(null);
      setEditMode('create');
    } else {
      // Open edit area for this position
      setSelectedPositionId(positionId);
      setEditMode('edit');
      setIsEditAreaOpen(true);
    }
  };

  const handleAddNewClick = () => {
    setSelectedPositionId(null);
    setEditMode('create');
    setIsEditAreaOpen(true);
  };

  const handleFormSubmit = (positionData: Omit<VolunteerPosition, 'id' | 'slotsFilled'>) => {
    if (editMode === 'edit' && editingPosition) {
      // Update existing position - pass data and ID to parent
      onPositionSubmit(positionData, editingPosition.id);
    } else {
      // Create new position - pass data without ID
      onPositionSubmit(positionData);
    }

    // Close the edit area after submission
    setIsEditAreaOpen(false);
    setSelectedPositionId(null);
  };

  const handleFormCancel = () => {
    setIsEditAreaOpen(false);
    setSelectedPositionId(null);
    setEditMode('create');
  };

  const handleFormDelete = (positionId: string) => {
    onDeletePosition(positionId);
    setIsEditAreaOpen(false);
    setSelectedPositionId(null);
    setEditMode('create');
  };


  return (
    <div>
      <Text size="sm" c="dimmed" mb="lg">
        Define volunteer positions needed for your event. Click any row to edit details and manage assigned volunteers.
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
            <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px', textAlign: 'center' }}>
              Needed / Filled
            </Table.Th>
            <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px', textAlign: 'center' }}>
              Visibility
            </Table.Th>
          </Table.Tr>
        </Table.Thead>
        <Table.Tbody>
          {positions.map((position) => {
            const isSelected = selectedPositionId === position.id && isEditAreaOpen;

            return (
              <Table.Tr
                key={position.id}
                data-testid="position-row"
                onClick={() => handleRowClick(position.id)}
                style={{
                  cursor: 'pointer',
                  backgroundColor: isSelected ? '#f8f9fa' : undefined,
                  transition: 'background-color 0.2s',
                }}
              >
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
                <Table.Td>
                  <CapacityDisplay
                    current={position.slotsFilled}
                    max={position.slotsNeeded}
                  />
                </Table.Td>
                <Table.Td style={{ textAlign: 'center' }}>
                  <Badge
                    color={position.isPublicFacing ? 'green' : 'gray'}
                    variant="light"
                    data-testid="visibility-badge"
                  >
                    {position.isPublicFacing ? 'Public' : 'Private'}
                  </Badge>
                </Table.Td>
              </Table.Tr>
            );
          })}
          {positions.length === 0 && (
            <Table.Tr>
              <Table.Td colSpan={6}>
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
          onClick={handleAddNewClick}
          data-testid="button-add-volunteer-position"
        >
          Add New Position
        </WCRButton>

        <Text size="xs" c="dimmed" fs="italic">
          💡 Tip: Click any row to edit position details and manage volunteers
        </Text>
      </Group>

      {/* Inline Edit Area */}
      <Collapse in={isEditAreaOpen} transitionDuration={300}>
        <Stack gap="md" mt="xl">
          <VolunteerPositionInlineForm
            position={editingPosition}
            onSubmit={handleFormSubmit}
            onCancel={handleFormCancel}
            onDelete={handleFormDelete}
            availableSessions={availableSessions}
            mode={editMode}
          />

          {/* Member Assignment Section - Only show in edit mode */}
          {editMode === 'edit' && editingPosition && (
            <AssignedMembersSection
              positionId={editingPosition.id}
              positionTitle={editingPosition.title}
            />
          )}
        </Stack>
      </Collapse>
    </div>
  );
};

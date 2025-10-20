import React, { useState } from 'react';
import { Modal, Select, Button, Group, Text, Stack } from '@mantine/core';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { showNotification } from '@mantine/notifications';

interface CoordinatorAssignmentModalProps {
  opened: boolean;
  onClose: () => void;
  incidentId: string;
  currentCoordinatorId?: string;
}

export const CoordinatorAssignmentModal: React.FC<CoordinatorAssignmentModalProps> = ({
  opened,
  onClose,
  incidentId,
  currentCoordinatorId,
}) => {
  const [selectedUserId, setSelectedUserId] = useState<string>(currentCoordinatorId || '');
  const queryClient = useQueryClient();

  // Fetch available coordinators: GET /api/safety/admin/users/coordinators
  const { data: users } = useQuery<Array<{ id: string; realName: string; role: string; activeIncidentCount: number }>>({
    queryKey: ['safety', 'coordinators'],
    queryFn: async () => {
      const response = await fetch('/api/safety/admin/users/coordinators', {
        credentials: 'include'
      });
      if (!response.ok) throw new Error('Failed to fetch coordinators');
      return response.json();
    },
    enabled: opened,
  });

  // Assign coordinator mutation: POST /api/safety/admin/incidents/{id}/assign
  const assignMutation = useMutation({
    mutationFn: async (coordinatorId: string) => {
      const response = await fetch(`/api/safety/admin/incidents/${incidentId}/assign`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify({ coordinatorId }),
      });
      if (!response.ok) throw new Error('Failed to assign coordinator');
      return response.json();
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['safety', 'incident', incidentId] });
      queryClient.invalidateQueries({ queryKey: ['safety', 'incidents'] });
      showNotification({
        title: 'Success',
        message: 'Coordinator assigned successfully',
        color: 'green',
      });
      onClose();
    },
    onError: (error) => {
      showNotification({
        title: 'Error',
        message: error instanceof Error ? error.message : 'Failed to assign coordinator',
        color: 'red',
      });
    }
  });

  const handleAssign = () => {
    if (selectedUserId) {
      assignMutation.mutate(selectedUserId);
    }
  };

  return (
    <Modal opened={opened} onClose={onClose} title="Assign Coordinator" size="md">
      <Stack gap="md">
        <Text size="sm">Select a user to assign as the incident coordinator:</Text>

        <Select
          label="Coordinator"
          placeholder="Select user"
          data-testid="coordinator-search-input"
          data={users?.map((user: any) => ({
            value: user.id,
            label: `${user.realName} (${user.role}) - ${user.activeIncidentCount} active`,
          })) || []}
          value={selectedUserId}
          onChange={(value) => setSelectedUserId(value || '')}
          searchable
        />

        <Group justify="flex-end" gap="sm">
          <Button
            variant="subtle"
            onClick={onClose}
            data-testid="cancel-button"
            styles={{
              root: {
                fontWeight: 600,
                height: '44px',
                paddingTop: '12px',
                paddingBottom: '12px',
                fontSize: '14px',
                lineHeight: '1.2'
              }
            }}
          >
            Cancel
          </Button>
          <Button
            onClick={handleAssign}
            loading={assignMutation.isPending}
            disabled={!selectedUserId}
            data-testid="assign-button"
            styles={{
              root: {
                fontWeight: 600,
                height: '44px',
                paddingTop: '12px',
                paddingBottom: '12px',
                fontSize: '14px',
                lineHeight: '1.2'
              }
            }}
          >
            Assign
          </Button>
        </Group>
      </Stack>
    </Modal>
  );
};

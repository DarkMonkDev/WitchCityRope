import React, { useState, useEffect } from 'react';
import { Modal, Select, Textarea, Button, Group, Stack, Text, MultiSelect } from '@mantine/core';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { showNotification } from '@mantine/notifications';

interface EditPeopleModalProps {
  opened: boolean;
  onClose: () => void;
  incidentId: string;
  type: 'involvedParties' | 'witnesses';
  currentValue?: string;
}

interface UserOption {
  value: string;
  label: string;
}

export const EditPeopleModal: React.FC<EditPeopleModalProps> = ({
  opened,
  onClose,
  incidentId,
  type,
  currentValue,
}) => {
  const [selectedMembers, setSelectedMembers] = useState<string[]>([]);
  const [otherNames, setOtherNames] = useState('');
  const queryClient = useQueryClient();

  // Parse current value on open
  useEffect(() => {
    if (opened && currentValue) {
      // For now, put everything in otherNames since we need to implement parsing logic
      setOtherNames(currentValue);
      setSelectedMembers([]);
    } else if (opened && !currentValue) {
      setOtherNames('');
      setSelectedMembers([]);
    }
  }, [opened, currentValue]);

  // Fetch all users for member selection
  const { data: users } = useQuery<Array<{ id: string; sceneName: string; realName: string }>>({
    queryKey: ['safety', 'all-users'],
    queryFn: async () => {
      const response = await fetch('/api/safety/admin/users/coordinators', {
        credentials: 'include'
      });
      if (!response.ok) throw new Error('Failed to fetch users');
      return response.json();
    },
    enabled: opened,
  });

  // Update mutation: PUT /api/safety/admin/incidents/{id}/people
  const updateMutation = useMutation<unknown, Error, void>({
    mutationFn: async () => {
      // Combine selected members and other names
      const memberNames = selectedMembers
        .map(memberId => users?.find(u => u.id === memberId)?.sceneName)
        .filter(Boolean);

      const allNames = [...memberNames, otherNames.trim()]
        .filter(Boolean)
        .join('\n');

      const response = await fetch(`/api/safety/admin/incidents/${incidentId}/people`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify({
          [type]: allNames || null,
        }),
      });
      if (!response.ok) throw new Error(`Failed to update ${type}`);
      return response.json();
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['safety', 'incident', incidentId] });
      showNotification({
        title: 'Success',
        message: `${type === 'involvedParties' ? 'Involved parties' : 'Witnesses'} updated`,
        color: 'green',
      });
      onClose();
    },
    onError: (error) => {
      showNotification({
        title: 'Error',
        message: error instanceof Error ? error.message : `Failed to update ${type}`,
        color: 'red',
      });
    }
  });

  const userOptions: UserOption[] = users?.map(user => ({
    value: user.id,
    label: `${user.sceneName} (${user.realName})`,
  })) || [];

  const handleSave = () => {
    updateMutation.mutate();
  };

  const title = type === 'involvedParties' ? 'Edit Involved Parties' : 'Edit Witnesses';

  return (
    <Modal opened={opened} onClose={onClose} title={title} size="lg">
      <Stack gap="md">
        <Text size="sm">
          Select members from the list and/or add names of non-members below.
        </Text>

        <MultiSelect
          label="Select Members"
          placeholder="Search and select members"
          data={userOptions}
          value={selectedMembers}
          onChange={setSelectedMembers}
          searchable
          clearable
          data-testid="members-multiselect"
        />

        <Textarea
          label="Other Names (Non-Members)"
          placeholder="Enter names of non-members, one per line"
          value={otherNames}
          onChange={(e) => setOtherNames(e.currentTarget.value)}
          minRows={4}
          data-testid="other-names-textarea"
          description="Add names of people who are not members"
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
            onClick={handleSave}
            loading={updateMutation.isPending}
            data-testid="save-button"
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
            Save
          </Button>
        </Group>
      </Stack>
    </Modal>
  );
};

import React, { useState } from 'react';
import { Modal, Select, Textarea, Button, Group, Stack, Text, Checkbox } from '@mantine/core';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { showNotification } from '@mantine/notifications';

type IncidentStatus = 'ReportSubmitted' | 'InformationGathering' | 'ReviewingFinalReport' | 'OnHold' | 'Closed';

interface ChangeStatusModalProps {
  opened: boolean;
  onClose: () => void;
  incidentId: string;
  currentStatus: IncidentStatus;
}

const statusOptions = [
  { value: 'ReportSubmitted', label: 'Report Submitted' },
  { value: 'InformationGathering', label: 'Information Gathering' },
  { value: 'ReviewingFinalReport', label: 'Reviewing Final Report' },
  { value: 'OnHold', label: 'On Hold' },
  { value: 'Closed', label: 'Closed' },
];

const getGuidanceChecklist = (newStatus: string): string[] => {
  switch (newStatus) {
    case 'InformationGathering':
      return [
        'Coordinator has been assigned',
        'Initial contact made with reporter (if identified)',
        'Investigation plan created',
      ];
    case 'ReviewingFinalReport':
      return [
        'All evidence collected and documented',
        'Witness statements gathered',
        'Final report drafted',
      ];
    case 'OnHold':
      return [
        'Reason for hold documented',
        'Reporter notified (if identified)',
        'Resume criteria identified',
      ];
    case 'Closed':
      return [
        'Final report completed',
        'All parties notified',
        'Documentation archived',
      ];
    default:
      return [];
  }
};

export const ChangeStatusModal: React.FC<ChangeStatusModalProps> = ({
  opened,
  onClose,
  incidentId,
  currentStatus,
}) => {
  const [newStatus, setNewStatus] = useState<string>('');
  const [notes, setNotes] = useState('');
  const queryClient = useQueryClient();

  const checklist = newStatus ? getGuidanceChecklist(newStatus) : [];

  // Update status mutation: PUT /api/safety/admin/incidents/{id}/status
  const statusMutation = useMutation<unknown, Error, void>({
    mutationFn: async () => {
      const response = await fetch(`/api/safety/admin/incidents/${incidentId}/status`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify({ newStatus, notes }),
      });
      if (!response.ok) throw new Error('Failed to update status');
      return response.json();
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['safety', 'incident', incidentId] });
      queryClient.invalidateQueries({ queryKey: ['safety', 'incidents'] });
      showNotification({
        title: 'Success',
        message: 'Status updated successfully',
        color: 'green',
      });
      onClose();
      setNewStatus('');
      setNotes('');
    },
    onError: (error) => {
      showNotification({
        title: 'Error',
        message: error instanceof Error ? error.message : 'Failed to update status',
        color: 'red',
      });
    }
  });

  return (
    <Modal opened={opened} onClose={onClose} title="Change Incident Status" size="md">
      <Stack gap="md">
        <Select
          label="New Status"
          placeholder="Select new status"
          data={statusOptions}
          value={newStatus}
          onChange={(value) => setNewStatus(value || '')}
        />

        {checklist.length > 0 && (
          <Stack gap="xs">
            <Text size="sm" fw={600}>Guidance Checklist (informative only):</Text>
            {checklist.map((item, index) => (
              <Checkbox key={index} label={item} disabled />
            ))}
          </Stack>
        )}

        <Textarea
          label="Notes (optional)"
          placeholder="Add notes about this status change..."
          data-testid="status-update-notes"
          value={notes}
          onChange={(e) => setNotes(e.currentTarget.value)}
          minRows={3}
        />

        <Group justify="flex-end" gap="sm">
          <Button
            variant="subtle"
            onClick={onClose}
            data-testid="cancel-status-change"
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
            onClick={() => statusMutation.mutate()}
            loading={statusMutation.isPending}
            disabled={!newStatus}
            data-testid="confirm-status-change"
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
            Update Status
          </Button>
        </Group>
      </Stack>
    </Modal>
  );
};

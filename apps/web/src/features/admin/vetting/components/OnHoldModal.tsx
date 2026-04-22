import React, { useState } from 'react';
import {
  Modal,
  Stack,
  Text,
  Textarea,
  Button,
  Group,
  Title
} from '@mantine/core';
import { notifications } from '@mantine/notifications';
import { useQueryClient } from '@tanstack/react-query';
import { vettingAdminApi } from '../services/vettingAdminApi';
import { vettingKeys } from '../hooks/useVettingApplications';

interface OnHoldModalProps {
  opened: boolean;
  onClose: () => void;
  applicationId?: string;          // Single application (backwards compatibility)
  applicantName?: string;          // Single application (backwards compatibility)
  applicationIds?: string[];       // Bulk operations
  applicantNames?: string[];       // Bulk operations
  onSuccess?: () => void;
}

export const OnHoldModal: React.FC<OnHoldModalProps> = ({
  opened,
  onClose,
  applicationId,
  applicantName,
  applicationIds,
  applicantNames,
  onSuccess
}) => {
  const [reason, setReason] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const queryClient = useQueryClient();

  // Determine if this is a bulk operation
  const isBulkOperation = applicationIds && applicationIds.length > 0;
  const targetApplicationIds = isBulkOperation ? applicationIds : [applicationId!];
  const targetNames = isBulkOperation ? applicantNames! : [applicantName!];

  const handleSubmit = async () => {
    if (!reason.trim()) {
      notifications.show({
        title: 'Validation Error',
        message: 'Please provide a reason for putting the application(s) on hold',
        color: 'yellow'
      });
      return;
    }

    setIsSubmitting(true);
    try {
      // Handle bulk operations
      if (isBulkOperation) {
        // Process all applications in parallel
        await Promise.all(
          targetApplicationIds.map(id => vettingAdminApi.putApplicationOnHold(id, reason))
        );

        notifications.show({
          title: 'Applications On Hold',
          message: `${targetApplicationIds.length} application(s) have been put on hold`,
          color: 'blue'
        });
      } else {
        // Single application
        await vettingAdminApi.putApplicationOnHold(applicationId!, reason);

        notifications.show({
          title: 'Application On Hold',
          message: `${applicantName}'s application has been put on hold`,
          color: 'blue'
        });
      }

      // Refresh React Query caches so the admin grid and the affected
      // application detail pages reflect the new status without a manual
      // refresh. We invalidate both the list and any open detail queries
      // for affected IDs. Single-app callers (the detail page) also pass
      // their own onSuccess handler that calls refetch() — that's
      // redundant once invalidation is in place but harmless.
      queryClient.invalidateQueries({ queryKey: vettingKeys.applications() });
      for (const id of targetApplicationIds) {
        queryClient.invalidateQueries({ queryKey: vettingKeys.applicationDetail(id) });
      }

      setReason('');
      onClose();
      onSuccess?.();
    } catch (error: any) {
      notifications.show({
        title: 'Error',
        message: error?.detail || error?.message || 'Failed to put application(s) on hold',
        color: 'red'
      });
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleClose = () => {
    if (!isSubmitting) {
      setReason('');
      onClose();
    }
  };

  return (
    <Modal
      opened={opened}
      onClose={handleClose}
      title={
        <Title order={3} style={{ color: '#880124' }}>
          Put Application On Hold
        </Title>
      }
      centered
      size="md"
      data-testid="on-hold-modal"
    >
      <Stack gap="md">
        <Text size="sm">
          {isBulkOperation ? (
            <>
              You are about to put <strong>{targetApplicationIds.length} application(s)</strong> on hold:
              <br />
              <strong>{targetNames.join(', ')}</strong>
              <br />
              Please provide a reason that will be recorded in the application history.
            </>
          ) : (
            <>
              You are about to put <strong>{applicantName}'s</strong> application on hold.
              Please provide a reason that will be recorded in the application history.
            </>
          )}
        </Text>

        <Textarea
          label="Reason for putting on hold"
          placeholder="Enter the reason for putting this application on hold..."
          value={reason}
          onChange={(e) => setReason(e.currentTarget.value)}
          minRows={4}
          required
          data-testid="on-hold-reason-textarea"
          styles={{
            input: {
              borderRadius: '8px',
              border: '1px solid #E0E0E0',
            }
          }}
        />

        <Group justify="flex-end" gap="md" mt="md">
          <Button
            variant="light"
            onClick={handleClose}
            disabled={isSubmitting}
            data-testid="on-hold-cancel-button"
            style={{
              minHeight: 40,
              height: 'auto',
              padding: '10px 20px',
              lineHeight: 1.4
            }}
          >
            Cancel
          </Button>
          <Button
            color="orange"
            onClick={handleSubmit}
            loading={isSubmitting}
            disabled={!reason.trim()}
            data-testid="on-hold-submit-button"
            style={{
              minHeight: 40,
              height: 'auto',
              padding: '10px 20px',
              lineHeight: 1.4
            }}
          >
            Put On Hold
          </Button>
        </Group>
      </Stack>
    </Modal>
  );
};
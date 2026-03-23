import React from 'react';
import {
  Modal,
  Stack,
  Text,
  Button,
  Group,
  Title,
  Alert,
} from '@mantine/core';
import { IconAlertTriangle } from '@tabler/icons-react';
import { notifications } from '@mantine/notifications';
import { useDeleteApplication } from '../hooks/useDeleteApplication';

interface DeleteApplicationModalProps {
  opened: boolean;
  onClose: () => void;
  applicationId: string;
  applicantName: string;
  /** Called after successful deletion - typically navigates back to list */
  onSuccess?: () => void;
}

/**
 * Confirmation modal for soft-deleting a vetting application.
 * Follows the same modal pattern as DenyApplicationModal for consistency.
 *
 * Soft delete preserves the application data for audit purposes but:
 * - Hides it from the admin applications list
 * - Resets the user's VettingStatus so they can reapply
 * - Frees up the email for a new application submission
 */
export const DeleteApplicationModal: React.FC<DeleteApplicationModalProps> = ({
  opened,
  onClose,
  applicationId,
  applicantName,
  onSuccess,
}) => {
  const deleteApplication = useDeleteApplication(() => {
    // Show success notification after deletion completes
    notifications.show({
      title: 'Application Deleted',
      message: `${applicantName}'s application has been deleted`,
      color: 'red',
    });
    onClose();
    onSuccess?.();
  });

  const handleSubmit = async () => {
    deleteApplication.mutate(applicationId);
  };

  /** Prevent closing while delete is in progress */
  const handleClose = () => {
    if (!deleteApplication.isPending) {
      onClose();
    }
  };

  return (
    <Modal
      opened={opened}
      onClose={handleClose}
      title={
        <Title order={3} style={{ color: '#880124' }}>
          Delete Application
        </Title>
      }
      centered
      size="md"
      data-testid="delete-application-modal"
    >
      <Stack gap="md">
        {/* Warning alert explaining what happens on delete */}
        <Alert
          icon={<IconAlertTriangle size={16} />}
          color="red"
          variant="light"
        >
          This action will remove the application from all views. The applicant's
          account will be reset so they can reapply if needed.
        </Alert>

        <Text size="sm">
          Are you sure you want to delete <strong>{applicantName}'s</strong> application?
          This cannot be undone from the UI.
        </Text>

        {/* Action buttons - matches DenyApplicationModal layout and styling */}
        <Group justify="flex-end" gap="md" mt="md">
          <Button
            variant="light"
            onClick={handleClose}
            disabled={deleteApplication.isPending}
            data-testid="delete-cancel-button"
            style={{
              minHeight: 40,
              height: 'auto',
              padding: '10px 20px',
              lineHeight: 1.4,
            }}
          >
            Cancel
          </Button>
          <Button
            color="red"
            onClick={handleSubmit}
            loading={deleteApplication.isPending}
            data-testid="delete-submit-button"
            style={{
              minHeight: 40,
              height: 'auto',
              padding: '10px 20px',
              lineHeight: 1.4,
            }}
          >
            Delete Application
          </Button>
        </Group>
      </Stack>
    </Modal>
  );
};

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
import { vettingAdminApi } from '../services/vettingAdminApi';

interface DenyApplicationModalProps {
  opened: boolean;
  onClose: () => void;
  applicationId: string;
  applicantName: string;
  onSuccess?: () => void;
}

export const DenyApplicationModal: React.FC<DenyApplicationModalProps> = ({
  opened,
  onClose,
  applicationId,
  applicantName,
  onSuccess
}) => {
  const [reason, setReason] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async () => {
    if (!reason.trim()) {
      notifications.show({
        title: 'Validation Error',
        message: 'Please provide a reason for denying this application',
        color: 'yellow'
      });
      return;
    }

    setIsSubmitting(true);
    try {
      await vettingAdminApi.denyApplication(applicationId, reason);

      notifications.show({
        title: 'Application Denied',
        message: `${applicantName}'s application has been denied`,
        color: 'red'
      });

      setReason('');
      onClose();
      onSuccess?.();
    } catch (error: any) {
      notifications.show({
        title: 'Error',
        message: error?.detail || error?.message || 'Failed to deny application',
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
          Deny Application
        </Title>
      }
      centered
      size="md"
      data-testid="deny-application-modal"
    >
      <Stack gap="md">
        <Text size="sm">
          You are about to deny <strong>{applicantName}'s</strong> application.
          Please provide a reason that will be recorded in the application history.
        </Text>

        <Textarea
          label="Reason for denial"
          placeholder="Enter the reason for denying this application..."
          value={reason}
          onChange={(e) => setReason(e.currentTarget.value)}
          minRows={4}
          required
          data-testid="deny-reason-textarea"
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
            data-testid="deny-cancel-button"
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
            color="red"
            onClick={handleSubmit}
            loading={isSubmitting}
            disabled={!reason.trim()}
            data-testid="deny-submit-button"
            style={{
              minHeight: 40,
              height: 'auto',
              padding: '10px 20px',
              lineHeight: 1.4
            }}
          >
            Deny Application
          </Button>
        </Group>
      </Stack>
    </Modal>
  );
};
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

interface SendReminderModalProps {
  opened: boolean;
  onClose: () => void;
  applicationId?: string;          // Single application (backwards compatibility)
  applicantName?: string;          // Single application (backwards compatibility)
  applicationIds?: string[];       // Bulk operations
  applicantNames?: string[];       // Bulk operations
  onSuccess?: () => void;
}

export const SendReminderModal: React.FC<SendReminderModalProps> = ({
  opened,
  onClose,
  applicationId,
  applicantName,
  applicationIds,
  applicantNames,
  onSuccess
}) => {
  const [message, setMessage] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Determine if this is a bulk operation
  const isBulkOperation = applicationIds && applicationIds.length > 0;
  const targetApplicationIds = isBulkOperation ? applicationIds : [applicationId!];
  const targetNames = isBulkOperation ? applicantNames! : [applicantName!];

  const handleSubmit = async () => {
    if (!message.trim()) {
      notifications.show({
        title: 'Validation Error',
        message: 'Please provide a reminder message',
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
          targetApplicationIds.map(id => vettingAdminApi.sendApplicationReminder(id, message))
        );

        notifications.show({
          title: 'Reminders Sent',
          message: `Reminders have been sent for ${targetApplicationIds.length} application(s)`,
          color: 'green'
        });
      } else {
        // Single application
        await vettingAdminApi.sendApplicationReminder(applicationId!, message);

        notifications.show({
          title: 'Reminder Sent',
          message: `Reminder has been sent for ${applicantName}'s application`,
          color: 'green'
        });
      }

      setMessage('');
      onClose();
      onSuccess?.();
    } catch (error: any) {
      notifications.show({
        title: 'Error',
        message: error?.detail || error?.message || 'Failed to send reminder(s)',
        color: 'red'
      });
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleClose = () => {
    if (!isSubmitting) {
      setMessage('');
      onClose();
    }
  };

  // Pre-fill with a default message
  React.useEffect(() => {
    if (opened && !message) {
      if (isBulkOperation) {
        setMessage(
          `Hi,\n\nThis is a friendly reminder regarding the applications to join WitchCityRope for:\n` +
          `${targetNames.join(', ')}\n\n` +
          `We are waiting for additional information or references to complete the review process.\n\n` +
          `Please let us know if you have any questions or need assistance.\n\nThank you,\nWitchCityRope Vetting Team`
        );
      } else {
        setMessage(
          `Hi,\n\nThis is a friendly reminder regarding ${applicantName}'s application to join WitchCityRope. ` +
          `We are waiting for additional information or references to complete the review process.\n\n` +
          `Please let us know if you have any questions or need assistance.\n\nThank you,\nWitchCityRope Vetting Team`
        );
      }
    }
  }, [opened, applicantName, targetNames, isBulkOperation, message]);

  return (
    <Modal
      opened={opened}
      onClose={handleClose}
      title={
        <Title order={3} style={{ color: '#880124' }}>
          Send Reminder
        </Title>
      }
      centered
      size="md"
      data-testid="send-reminder-modal"
    >
      <Stack gap="md">
        <Text size="sm">
          {isBulkOperation ? (
            <>
              You are about to send reminders for <strong>{targetApplicationIds.length} application(s)</strong>:
              <br />
              <strong>{targetNames.join(', ')}</strong>
              <br />
              Please review and edit the message below as needed.
            </>
          ) : (
            <>
              You are about to send a reminder for <strong>{applicantName}'s</strong> application.
              Please review and edit the message below as needed.
            </>
          )}
        </Text>

        <Textarea
          label="Reminder message"
          placeholder="Enter the reminder message..."
          value={message}
          onChange={(e) => setMessage(e.currentTarget.value)}
          minRows={6}
          required
          data-testid="reminder-message-textarea"
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
            data-testid="reminder-cancel-button"
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
            disabled={!message.trim()}
            data-testid="reminder-submit-button"
            style={{
              minHeight: 40,
              height: 'auto',
              padding: '10px 20px',
              lineHeight: 1.4
            }}
          >
            Send Reminder
          </Button>
        </Group>
      </Stack>
    </Modal>
  );
};
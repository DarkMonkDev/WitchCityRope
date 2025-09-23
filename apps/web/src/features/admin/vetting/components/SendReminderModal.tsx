import React, { useState } from 'react';
import {
  Modal,
  Stack,
  Textarea,
  Button,
  Group,
  Title
} from '@mantine/core';
import { notifications } from '@mantine/notifications';
import { vettingAdminApi } from '../services/vettingAdminApi';
import { useVettingApplications } from '../hooks/useVettingApplications';
import type { ApplicationFilterRequest } from '../types/vetting.types';

interface SendReminderModalProps {
  opened: boolean;
  onClose: () => void;
  onSuccess?: () => void;
}

export const SendReminderModal: React.FC<SendReminderModalProps> = ({
  opened,
  onClose,
  onSuccess
}) => {
  const [message, setMessage] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Fetch all pending applications to send reminders to
  const filters: ApplicationFilterRequest = {
    page: 1,
    pageSize: 100, // Get all applications
    statusFilters: ['PendingInterview'], // Only pending interview applications
    priorityFilters: [],
    experienceLevelFilters: [],
    skillsFilters: [],
    searchQuery: '',
    sortBy: 'SubmittedAt',
    sortDirection: 'Desc'
  };

  const { data: applicationsData } = useVettingApplications(filters);
  const applications = applicationsData?.items || [];

  const handleSubmit = async () => {
    if (!message.trim()) {
      notifications.show({
        title: 'Validation Error',
        message: 'Please provide a reminder message',
        color: 'yellow'
      });
      return;
    }

    if (applications.length === 0) {
      notifications.show({
        title: 'No Applications',
        message: 'No pending applications found to send reminders to',
        color: 'yellow'
      });
      return;
    }

    setIsSubmitting(true);
    try {
      // Send reminders to all pending applications
      await Promise.all(
        applications.map(app => vettingAdminApi.sendApplicationReminder(app.id, message))
      );

      notifications.show({
        title: 'Reminders Sent',
        message: `Reminders have been sent to ${applications.length} pending application(s)`,
        color: 'green'
      });

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

  // Pre-fill with a default message template
  React.useEffect(() => {
    if (opened && !message) {
      setMessage(
        `Hi,\n\nThis is a friendly reminder regarding your vetting application to join WitchCityRope. ` +
        `We are waiting for additional information or references to complete the review process.\n\n` +
        `Please let us know if you have any questions or need assistance.\n\n` +
        `Thank you,\nWitchCityRope Vetting Team`
      );
    }
  }, [opened, message]);

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
            CANCEL
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
            SEND REMINDER
          </Button>
        </Group>
      </Stack>
    </Modal>
  );
};
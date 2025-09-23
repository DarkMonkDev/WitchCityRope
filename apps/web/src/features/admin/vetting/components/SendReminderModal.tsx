import React, { useState } from 'react';
import {
  Modal,
  Stack,
  Text,
  Textarea,
  Button,
  Group,
  Title,
  Checkbox,
  Paper,
  Divider
} from '@mantine/core';
import { notifications } from '@mantine/notifications';
import { vettingAdminApi } from '../services/vettingAdminApi';
import { useVettingApplications } from '../hooks/useVettingApplications';
import type { ApplicationFilterRequest } from '../types/vetting.types';

interface SendReminderModalProps {
  opened: boolean;
  onClose: () => void;
  applicationId?: string;          // Single application (backwards compatibility)
  applicantName?: string;          // Single application (backwards compatibility)
  applicationIds?: string[];       // Bulk operations (backwards compatibility)
  applicantNames?: string[];       // Bulk operations (backwards compatibility)
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
  const [selectedApplicationIds, setSelectedApplicationIds] = useState<Set<string>>(new Set());

  // Fetch all available applications for selection
  const filters: ApplicationFilterRequest = {
    page: 1,
    pageSize: 100, // Get all applications
    statusFilters: ['UnderReview', 'InterviewApproved', 'PendingInterview', 'OnHold'], // Statuses that might need reminders
    priorityFilters: [],
    experienceLevelFilters: [],
    skillsFilters: [],
    searchQuery: '',
    sortBy: 'SubmittedAt',
    sortDirection: 'Desc'
  };

  const { data: applicationsData } = useVettingApplications(filters);
  const applications = applicationsData?.items || [];

  // Handle backwards compatibility - if specific applications are passed, use those
  const isLegacyMode = (applicationId && applicantName) || (applicationIds && applicationIds.length > 0);
  const legacyApplicationIds = isLegacyMode ? (applicationIds || [applicationId!]) : [];
  const legacyNames = isLegacyMode ? (applicantNames || [applicantName!]) : [];

  // Initialize selection for legacy mode
  React.useEffect(() => {
    if (opened && isLegacyMode) {
      setSelectedApplicationIds(new Set(legacyApplicationIds));
    } else if (opened && !isLegacyMode) {
      setSelectedApplicationIds(new Set());
    }
  }, [opened, isLegacyMode, legacyApplicationIds]);

  const handleApplicationToggle = (applicationId: string, checked: boolean) => {
    const newSelected = new Set(selectedApplicationIds);
    if (checked) {
      newSelected.add(applicationId);
    } else {
      newSelected.delete(applicationId);
    }
    setSelectedApplicationIds(newSelected);
  };

  const handleSelectAll = (checked: boolean) => {
    if (checked) {
      setSelectedApplicationIds(new Set(applications.map(app => app.id)));
    } else {
      setSelectedApplicationIds(new Set());
    }
  };

  const handleSubmit = async () => {
    if (!message.trim()) {
      notifications.show({
        title: 'Validation Error',
        message: 'Please provide a reminder message',
        color: 'yellow'
      });
      return;
    }

    if (selectedApplicationIds.size === 0) {
      notifications.show({
        title: 'Validation Error',
        message: 'Please select at least one application to send reminders to',
        color: 'yellow'
      });
      return;
    }

    setIsSubmitting(true);
    try {
      // Process all selected applications in parallel
      await Promise.all(
        Array.from(selectedApplicationIds).map(id => vettingAdminApi.sendApplicationReminder(id, message))
      );

      notifications.show({
        title: 'Reminders Sent',
        message: `Reminders have been sent for ${selectedApplicationIds.size} application(s)`,
        color: 'green'
      });

      setMessage('');
      setSelectedApplicationIds(new Set());
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
      setMessage(
        `Hi,\n\nThis is a friendly reminder regarding the selected vetting applications to join WitchCityRope. ` +
        `We are waiting for additional information or references to complete the review process.\n\n` +
        `Please let us know if you have any questions or need assistance.\n\nThank you,\nWitchCityRope Vetting Team`
      );
    }
  }, [opened, message]);

  const selectedApplications = applications.filter(app => selectedApplicationIds.has(app.id));
  const hasSelections = selectedApplicationIds.size > 0;

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
        {/* Application Selection Section */}
        {!isLegacyMode && (
          <>
            <Text size="sm" fw={600}>
              Select applications to send reminders to:
            </Text>

            <Paper p="md" withBorder style={{ maxHeight: '300px', overflowY: 'auto' }}>
              <Stack gap="xs">
                {applications.length > 0 && (
                  <Checkbox
                    label={`Select All (${applications.length} applications)`}
                    checked={selectedApplicationIds.size === applications.length && applications.length > 0}
                    indeterminate={selectedApplicationIds.size > 0 && selectedApplicationIds.size < applications.length}
                    onChange={(event) => handleSelectAll(event.currentTarget.checked)}
                    styles={{ label: { fontWeight: 600 } }}
                  />
                )}

                <Divider />

                {applications.map((app) => (
                  <Checkbox
                    key={app.id}
                    label={
                      <Group gap="xs">
                        <Text size="sm">{app.sceneName}</Text>
                        <Text size="xs" c="dimmed">({app.status})</Text>
                        <Text size="xs" c="dimmed">- {app.applicationNumber}</Text>
                      </Group>
                    }
                    checked={selectedApplicationIds.has(app.id)}
                    onChange={(event) => handleApplicationToggle(app.id, event.currentTarget.checked)}
                  />
                ))}

                {applications.length === 0 && (
                  <Text size="sm" c="dimmed" ta="center">
                    No applications available for reminders
                  </Text>
                )}
              </Stack>
            </Paper>
          </>
        )}

        {/* Legacy mode or selection summary */}
        {(isLegacyMode || hasSelections) && (
          <Text size="sm">
            {isLegacyMode ? (
              <>
                You are about to send reminders for <strong>{legacyApplicationIds.length} application(s)</strong>:
                <br />
                <strong>{legacyNames.join(', ')}</strong>
              </>
            ) : (
              <>
                You are about to send reminders for <strong>{selectedApplicationIds.size} selected application(s)</strong>:
                <br />
                <strong>{selectedApplications.map(app => app.sceneName).join(', ')}</strong>
              </>
            )}
            <br />
            Please review and edit the message below as needed.
          </Text>
        )}

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
            disabled={!message.trim() || (!isLegacyMode && selectedApplicationIds.size === 0)}
            data-testid="reminder-submit-button"
            style={{
              minHeight: 40,
              height: 'auto',
              padding: '10px 20px',
              lineHeight: 1.4
            }}
          >
            Send {isLegacyMode ? legacyApplicationIds.length : selectedApplicationIds.size} Reminder{(isLegacyMode ? legacyApplicationIds.length : selectedApplicationIds.size) !== 1 ? 's' : ''}
          </Button>
        </Group>
      </Stack>
    </Modal>
  );
};
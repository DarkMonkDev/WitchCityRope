import React, { useState } from 'react';
import {
  Modal,
  Stack,
  Textarea,
  Button,
  Group,
  Title,
  Text,
  Alert,
  List
} from '@mantine/core';
import { IconMail, IconInfoCircle } from '@tabler/icons-react';
import { useQueryClient } from '@tanstack/react-query';
import { notifications } from '@mantine/notifications';
import { vettingAdminApi } from '../services/vettingAdminApi';
import { useSendReminder } from '../hooks/useSendReminder';
import { vettingKeys } from '../hooks/useVettingApplications';

interface SendReminderModalProps {
  opened: boolean;
  onClose: () => void;
  // Single-application mode (existing usage on the application detail page)
  applicationId?: string;
  applicantName?: string;
  // Bulk mode (used by the admin vetting list page when multiple rows are selected).
  // Mirrors the OnHoldModal API so the two batch actions look and feel identical.
  applicationIds?: string[];
  applicantNames?: string[];
  onSuccess?: () => void;
}

/**
 * Modal for sending an interview reminder email — supports both:
 *  - Single mode: pass `applicationId` + `applicantName` (used on the
 *    individual application detail page).
 *  - Bulk mode: pass `applicationIds` + `applicantNames` (used on the
 *    admin vetting list page after multi-select).
 *
 * In bulk mode the modal fans out one POST per application via Promise.all,
 * mirroring OnHoldModal's pattern. There is no dedicated bulk endpoint on the
 * backend; the per-application endpoint already does all the right things
 * (status check, increments RemindersSentCount, writes a VettingEmailLog
 * audit row) so we deliberately reuse it instead of building a parallel
 * code path. The caller is responsible for filtering the selection down to
 * applications in InterviewApproved status before opening the modal — the
 * backend will reject any other status with a clear error.
 *
 * The same custom message is sent to every recipient in bulk mode.
 */
export const SendReminderModal: React.FC<SendReminderModalProps> = ({
  opened,
  onClose,
  applicationId,
  applicantName,
  applicationIds,
  applicantNames,
  onSuccess
}) => {
  const [customMessage, setCustomMessage] = useState('');
  const [isBulkSubmitting, setIsBulkSubmitting] = useState(false);
  const queryClient = useQueryClient();

  // Bulk mode is active when we received a non-empty array of IDs.
  const isBulkOperation = !!(applicationIds && applicationIds.length > 0);
  const recipientNames = isBulkOperation
    ? (applicantNames ?? [])
    : applicantName
      ? [applicantName]
      : [];

  // Single-mode mutation hook — unchanged from before. We keep using it for
  // the single-app code path so existing tests and detail-page callers behave
  // identically. Bulk mode bypasses this hook because we need per-call
  // success/failure tracking, which is awkward to express through a single
  // useMutation invocation.
  const { mutate: sendReminder, isPending: isSinglePending } = useSendReminder(() => {
    setCustomMessage('');
    onClose();
    onSuccess?.();
  });

  const isSubmitting = isBulkOperation ? isBulkSubmitting : isSinglePending;

  const handleSingleSubmit = () => {
    if (!applicationId) return;
    sendReminder({
      applicationId,
      customMessage: customMessage.trim() || undefined
    });
  };

  const handleBulkSubmit = async () => {
    if (!applicationIds || applicationIds.length === 0) return;

    setIsBulkSubmitting(true);
    const trimmedMessage = customMessage.trim() || undefined;

    // Promise.allSettled (not Promise.all) so a single failed send does not
    // cancel the others. We then count successes/failures and report a
    // single summary toast — matches the user's expectation that a batch
    // send "either sends to everyone in the right status, or tells me
    // exactly what failed".
    const results = await Promise.allSettled(
      applicationIds.map(id =>
        vettingAdminApi.sendApplicationReminder(id, trimmedMessage)
      )
    );

    const successCount = results.filter(r => r.status === 'fulfilled').length;
    const failureCount = results.length - successCount;

    // Refresh the admin list so the Reminders column updates without a
    // manual refresh. We invalidate the broad applications() key (the list
    // query) — individual detail queries are not open in this flow.
    queryClient.invalidateQueries({ queryKey: vettingKeys.applications() });

    if (failureCount === 0) {
      notifications.show({
        title: 'Reminders Sent',
        message: `Sent interview reminder to ${successCount} applicant(s)`,
        color: 'green'
      });
    } else if (successCount === 0) {
      notifications.show({
        title: 'Reminder Send Failed',
        message: `Failed to send reminders to ${failureCount} applicant(s)`,
        color: 'red'
      });
    } else {
      notifications.show({
        title: 'Reminders Partially Sent',
        message: `Sent ${successCount}, failed ${failureCount}`,
        color: 'yellow'
      });
    }

    setIsBulkSubmitting(false);
    setCustomMessage('');
    onClose();
    onSuccess?.();
  };

  const handleSubmit = () => {
    if (isBulkOperation) {
      handleBulkSubmit();
    } else {
      handleSingleSubmit();
    }
  };

  const handleClose = () => {
    if (!isSubmitting) {
      setCustomMessage('');
      onClose();
    }
  };

  return (
    <Modal
      opened={opened}
      onClose={handleClose}
      title={
        <Title order={3} style={{ color: '#880124' }}>
          {isBulkOperation
            ? `Send Interview Reminder (${recipientNames.length})`
            : 'Send Interview Reminder'}
        </Title>
      }
      centered
      size="md"
      data-testid="send-reminder-modal"
    >
      <Stack gap="md">
        {isBulkOperation ? (
          <>
            <Text>
              Send the Interview Reminder template to <Text span fw={600}>{recipientNames.length}</Text>{' '}
              applicant(s):
            </Text>
            <List size="sm" spacing={4} data-testid="reminder-recipients-list">
              {recipientNames.map((name, idx) => (
                <List.Item key={idx}>{name}</List.Item>
              ))}
            </List>
          </>
        ) : (
          <Text>
            Send an interview reminder email to <Text span fw={600}>{applicantName}</Text> using
            the Interview Reminder template.
          </Text>
        )}

        <Alert
          icon={<IconInfoCircle size={16} />}
          color="blue"
          variant="light"
        >
          <Text size="sm">
            This will use the Interview Reminder email template configured in the
            Email Templates admin page. Template variables (scene name, application
            number, etc.) will be automatically populated.
            {isBulkOperation && ' The same custom message (if provided) will be included in every email.'}
          </Text>
        </Alert>

        <Textarea
          label="Custom message (optional)"
          description="This message will be included in the email template's {{custom_message}} section"
          placeholder="Add any additional context for the applicant..."
          value={customMessage}
          onChange={(e) => setCustomMessage(e.currentTarget.value)}
          minRows={4}
          data-testid="reminder-custom-message-textarea"
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
            leftSection={<IconMail size={16} />}
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

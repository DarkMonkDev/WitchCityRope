import React, { useState } from 'react';
import {
  Modal,
  Stack,
  Textarea,
  Button,
  Group,
  Title,
  Text,
  Alert
} from '@mantine/core';
import { IconMail, IconInfoCircle } from '@tabler/icons-react';
import { useSendReminder } from '../hooks/useSendReminder';

interface SendReminderModalProps {
  opened: boolean;
  onClose: () => void;
  applicationId: string;
  applicantName: string;
  onSuccess?: () => void;
}

/**
 * Modal for sending an interview reminder email to a specific applicant.
 * Uses the InterviewReminder email template from the vetting templates.
 * The custom message is optional and replaces the {{custom_message}} variable in the template.
 */
export const SendReminderModal: React.FC<SendReminderModalProps> = ({
  opened,
  onClose,
  applicationId,
  applicantName,
  onSuccess
}) => {
  const [customMessage, setCustomMessage] = useState('');

  const { mutate: sendReminder, isPending } = useSendReminder(() => {
    setCustomMessage('');
    onClose();
    onSuccess?.();
  });

  const handleSubmit = () => {
    sendReminder({
      applicationId,
      customMessage: customMessage.trim() || undefined
    });
  };

  const handleClose = () => {
    if (!isPending) {
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
          Send Interview Reminder
        </Title>
      }
      centered
      size="md"
      data-testid="send-reminder-modal"
    >
      <Stack gap="md">
        <Text>
          Send an interview reminder email to <Text span fw={600}>{applicantName}</Text> using
          the Interview Reminder template.
        </Text>

        <Alert
          icon={<IconInfoCircle size={16} />}
          color="blue"
          variant="light"
        >
          <Text size="sm">
            This will use the Interview Reminder email template configured in the
            Email Templates admin page. Template variables (scene name, application
            number, etc.) will be automatically populated.
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
            disabled={isPending}
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
            loading={isPending}
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

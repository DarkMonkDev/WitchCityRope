import React, { useState } from 'react';
import {
  Modal,
  Stack,
  Text,
  Button,
  Group,
  Title,
  Checkbox,
  Alert,
  List
} from '@mantine/core';
import { IconAlertTriangle } from '@tabler/icons-react';
import { notifications } from '@mantine/notifications';

interface RemoveRsvpModalProps {
  opened: boolean;
  onClose: () => void;
  participant: {
    userId: string;
    name: string;
    hasTicket: boolean;
    ticketAmount?: number;
    volunteerShifts?: string[];
  };
  eventName: string;
  onConfirm: () => Promise<void>;
}

export const RemoveRsvpModal: React.FC<RemoveRsvpModalProps> = ({
  opened,
  onClose,
  participant,
  eventName,
  onConfirm
}) => {
  const [confirmed, setConfirmed] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async () => {
    if (!confirmed) {
      notifications.show({
        title: 'Confirmation Required',
        message: 'Please confirm you understand this action cannot be undone',
        color: 'yellow'
      });
      return;
    }

    setIsSubmitting(true);
    try {
      await onConfirm();
      setConfirmed(false);
      onClose();
    } catch (error: any) {
      notifications.show({
        title: 'Error',
        message: error?.detail || error?.message || 'Failed to remove RSVP',
        color: 'red',
        autoClose: 5000
      });
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleClose = () => {
    if (!isSubmitting) {
      setConfirmed(false);
      onClose();
    }
  };

  // Build impact list based on available data
  const impactItems = [
    `Remove the RSVP for ${participant.name}`
  ];

  if (participant.hasTicket && participant.ticketAmount !== undefined) {
    impactItems.push(`Refund their ticket: $${participant.ticketAmount.toFixed(2)}`);
  }

  if (participant.volunteerShifts && participant.volunteerShifts.length > 0) {
    participant.volunteerShifts.forEach(shift => {
      impactItems.push(`Remove volunteer assignment: ${shift}`);
    });
  }

  return (
    <Modal
      opened={opened}
      onClose={handleClose}
      title={
        <Title order={3} style={{ color: '#880124' }}>
          Remove RSVP?
        </Title>
      }
      centered
      size="md"
      data-testid="remove-rsvp-modal"
    >
      <Stack gap="md">
        {/* Participant Info */}
        <Text size="sm">
          You are about to remove the RSVP for:
        </Text>
        <List size="sm" spacing="xs" withPadding>
          <List.Item>
            <Text component="span" fw={500}>{participant.name}</Text>
          </List.Item>
          <List.Item>
            Event: <Text component="span" fw={500}>"{eventName}"</Text>
          </List.Item>
        </List>

        {/* Warning Box with Impact Details */}
        <Alert
          icon={<IconAlertTriangle size={20} />}
          title="This action will:"
          color="yellow"
          variant="light"
          styles={{
            root: {
              borderLeft: '4px solid #DAA520'
            }
          }}
        >
          <List size="sm" spacing="xs" withPadding>
            {impactItems.map((item, index) => (
              <List.Item key={index}>
                <Text size="sm">{item}</Text>
              </List.Item>
            ))}
          </List>
        </Alert>

        {/* Cannot Undo Warning */}
        <Text size="sm" c="dimmed" ta="center">
          This action cannot be undone.
        </Text>

        {/* Confirmation Checkbox */}
        <Checkbox
          checked={confirmed}
          onChange={(event) => setConfirmed(event.currentTarget.checked)}
          label="I understand this will remove the RSVP and cannot be undone"
          data-testid="remove-rsvp-confirmation"
          styles={{
            label: {
              fontSize: '14px',
              color: '#2B2B2B'
            }
          }}
        />

        {/* Action Buttons */}
        <Group justify="flex-end" gap="md" mt="md">
          <Button
            variant="light"
            onClick={handleClose}
            disabled={isSubmitting}
            data-testid="remove-rsvp-cancel"
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
            color="red"
            onClick={handleSubmit}
            loading={isSubmitting}
            disabled={!confirmed}
            data-testid="remove-rsvp-submit"
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
            Remove RSVP
          </Button>
        </Group>
      </Stack>
    </Modal>
  );
};

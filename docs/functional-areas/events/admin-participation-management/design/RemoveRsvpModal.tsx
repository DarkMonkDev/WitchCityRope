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
  participantName: string;
  participantEmail: string;
  eventTitle: string;
  participationId: string;
  hasTicket: boolean;
  ticketAmount?: number;
  volunteerShifts?: Array<{
    shiftName: string;
    startTime: string;
    endTime: string;
  }>;
  onSuccess?: () => void;
}

export const RemoveRsvpModal: React.FC<RemoveRsvpModalProps> = ({
  opened,
  onClose,
  participantName,
  participantEmail,
  eventTitle,
  participationId,
  hasTicket,
  ticketAmount,
  volunteerShifts = [],
  onSuccess
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
      // TODO: Replace with actual API call
      // await participationApi.removeRsvp(participationId);

      notifications.show({
        title: 'RSVP Removed',
        message: `${participantName}'s RSVP has been removed`,
        color: 'green',
        autoClose: 3000
      });

      setConfirmed(false);
      onClose();
      onSuccess?.();
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
    `Remove the RSVP for ${participantName}`
  ];

  if (hasTicket && ticketAmount !== undefined) {
    impactItems.push(`Refund their ticket: $${ticketAmount.toFixed(2)}`);
  }

  if (volunteerShifts.length > 0) {
    volunteerShifts.forEach(shift => {
      impactItems.push(
        `Remove volunteer assignment: ${shift.shiftName} (${shift.startTime} - ${shift.endTime})`
      );
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
            <Text component="span" fw={500}>{participantName}</Text>
          </List.Item>
          <List.Item>
            Event: <Text component="span" fw={500}>"{eventTitle}"</Text>
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
            disabled={!confirmed}
            data-testid="remove-rsvp-submit"
            style={{
              minHeight: 40,
              height: 'auto',
              padding: '10px 20px',
              lineHeight: 1.4
            }}
          >
            Remove RSVP
          </Button>
        </Group>
      </Stack>
    </Modal>
  );
};

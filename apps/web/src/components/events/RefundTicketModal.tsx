import React, { useState, useEffect } from 'react';
import {
  Modal,
  Stack,
  Text,
  Button,
  Group,
  Title,
  Checkbox,
  Alert,
  List,
  Box
} from '@mantine/core';
import { IconAlertTriangle } from '@tabler/icons-react';
import { notifications } from '@mantine/notifications';

interface RefundTicketModalProps {
  opened: boolean;
  onClose: () => void;
  participant: {
    userId: string;
    name: string;
    ticketAmount: number;
    hasRsvp: boolean;
    volunteerShifts?: string[];
  };
  eventName: string;
  onConfirm: (alsoRemoveRsvp: boolean) => Promise<void>;
}

export const RefundTicketModal: React.FC<RefundTicketModalProps> = ({
  opened,
  onClose,
  participant,
  eventName,
  onConfirm
}) => {
  const [alsoRemoveRsvp, setAlsoRemoveRsvp] = useState(true); // Default: checked
  const [confirmed, setConfirmed] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Reset state when modal closes
  useEffect(() => {
    if (!opened) {
      setConfirmed(false);
      setAlsoRemoveRsvp(true); // Reset to default
    }
  }, [opened]);

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
      await onConfirm(alsoRemoveRsvp);
      setConfirmed(false);
      setAlsoRemoveRsvp(true);
      onClose();
    } catch (error: any) {
      notifications.show({
        title: 'Error',
        message: error?.detail || error?.message || 'Failed to refund ticket',
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
      setAlsoRemoveRsvp(true);
      onClose();
    }
  };

  // Build impact list based on selections
  const impactItems = [
    `Refund $${participant.ticketAmount.toFixed(2)} to ${participant.name}`
  ];

  if (participant.hasRsvp && alsoRemoveRsvp) {
    impactItems.push(`Remove their RSVP`);
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
          Refund Ticket?
        </Title>
      }
      centered
      size="md"
      data-testid="refund-ticket-modal"
    >
      <Stack gap="md">
        {/* Participant Info */}
        <Text size="sm">
          You are about to refund the ticket for:
        </Text>
        <List size="sm" spacing="xs" withPadding>
          <List.Item>
            <Text component="span" fw={500}>{participant.name}</Text>
          </List.Item>
          <List.Item>
            Event: <Text component="span" fw={500}>"{eventName}"</Text>
          </List.Item>
        </List>

        {/* Refund Amount - Prominent Display */}
        <Box
          style={{
            padding: '12px',
            backgroundColor: '#FAF6F2',
            borderRadius: '8px',
            textAlign: 'center'
          }}
        >
          <Text size="sm" c="dimmed" mb={4}>
            Refund Amount
          </Text>
          <Text size="xl" fw={700} c="charcoal">
            ${participant.ticketAmount.toFixed(2)}
          </Text>
        </Box>

        {/* Option: Also Remove RSVP */}
        {participant.hasRsvp && (
          <Checkbox
            checked={alsoRemoveRsvp}
            onChange={(event) => setAlsoRemoveRsvp(event.currentTarget.checked)}
            label="Also remove RSVP if present"
            data-testid="also-remove-rsvp-checkbox"
            styles={{
              root: {
                padding: '12px',
                backgroundColor: '#FFF8F0',
                borderRadius: '8px',
                border: '1px solid #B8B0A8'
              },
              label: {
                fontSize: '14px',
                fontWeight: 500,
                color: '#2B2B2B'
              }
            }}
          />
        )}

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
          label="I understand this will refund the ticket and cannot be undone"
          data-testid="refund-ticket-confirmation"
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
            data-testid="refund-ticket-cancel"
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
            data-testid="refund-ticket-submit"
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
            Refund Ticket
          </Button>
        </Group>
      </Stack>
    </Modal>
  );
};

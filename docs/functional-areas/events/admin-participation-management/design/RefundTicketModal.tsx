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
  List,
  Box
} from '@mantine/core';
import { IconAlertTriangle } from '@tabler/icons-react';
import { notifications } from '@mantine/notifications';

interface RefundTicketModalProps {
  opened: boolean;
  onClose: () => void;
  participantName: string;
  participantEmail: string;
  eventTitle: string;
  ticketId: string;
  refundAmount: number;
  hasRsvp: boolean;
  volunteerShifts?: Array<{
    shiftName: string;
    startTime: string;
    endTime: string;
  }>;
  onSuccess?: () => void;
}

export const RefundTicketModal: React.FC<RefundTicketModalProps> = ({
  opened,
  onClose,
  participantName,
  participantEmail,
  eventTitle,
  ticketId,
  refundAmount,
  hasRsvp,
  volunteerShifts = [],
  onSuccess
}) => {
  const [alsoRemoveRsvp, setAlsoRemoveRsvp] = useState(true); // Default: checked
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
      // await ticketApi.refundTicket(ticketId, { alsoRemoveRsvp });

      notifications.show({
        title: 'Ticket Refunded',
        message: `$${refundAmount.toFixed(2)} refunded to ${participantName}`,
        color: 'green',
        autoClose: 3000
      });

      setConfirmed(false);
      setAlsoRemoveRsvp(true);
      onClose();
      onSuccess?.();
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
    `Refund $${refundAmount.toFixed(2)} to ${participantName}`
  ];

  if (hasRsvp && alsoRemoveRsvp) {
    impactItems.push(`Remove their RSVP`);
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
            <Text component="span" fw={500}>{participantName}</Text>
          </List.Item>
          <List.Item>
            Event: <Text component="span" fw={500}>"{eventTitle}"</Text>
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
            ${refundAmount.toFixed(2)}
          </Text>
        </Box>

        {/* Option: Also Remove RSVP */}
        {hasRsvp && (
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
            data-testid="refund-ticket-submit"
            style={{
              minHeight: 40,
              height: 'auto',
              padding: '10px 20px',
              lineHeight: 1.4
            }}
          >
            Refund Ticket
          </Button>
        </Group>
      </Stack>
    </Modal>
  );
};

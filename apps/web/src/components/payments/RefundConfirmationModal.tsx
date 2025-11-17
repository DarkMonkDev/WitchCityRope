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
  Box,
  Textarea
} from '@mantine/core';
import { IconAlertTriangle } from '@tabler/icons-react';
import { notifications } from '@mantine/notifications';

interface RefundConfirmationModalProps {
  opened: boolean;
  onClose: () => void;
  payment: {
    id: string;
    userName: string;
    userEmail: string;
    amount: number;
    paymentMethod: string;
    paymentDate: string;
    description?: string;
  };
  onConfirm: (refundReason: string) => Promise<void>;
}

export const RefundConfirmationModal: React.FC<RefundConfirmationModalProps> = ({
  opened,
  onClose,
  payment,
  onConfirm
}) => {
  const [refundReason, setRefundReason] = useState('');
  const [confirmed, setConfirmed] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Reset state when modal closes
  useEffect(() => {
    if (!opened) {
      setConfirmed(false);
      setRefundReason('');
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

    if (!refundReason.trim()) {
      notifications.show({
        title: 'Refund Reason Required',
        message: 'Please provide a reason for the refund',
        color: 'yellow'
      });
      return;
    }

    setIsSubmitting(true);
    try {
      await onConfirm(refundReason);
      notifications.show({
        title: 'Refund Processed',
        message: `Refund of $${payment.amount.toFixed(2)} has been processed successfully. The user will receive an email confirmation.`,
        color: 'green',
        autoClose: 5000
      });
      setConfirmed(false);
      setRefundReason('');
      onClose();
    } catch (error: any) {
      notifications.show({
        title: 'Error',
        message: error?.detail || error?.message || 'Failed to process refund',
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
      setRefundReason('');
      onClose();
    }
  };

  const remainingChars = 500 - refundReason.length;

  return (
    <Modal
      opened={opened}
      onClose={handleClose}
      title={
        <Title order={3} style={{ color: '#880124' }}>
          Process Refund?
        </Title>
      }
      centered
      size="md"
      data-testid="refund-confirmation-modal"
    >
      <Stack gap="md">
        {/* Payment Info */}
        <Text size="sm">
          You are about to process a refund for:
        </Text>
        <List size="sm" spacing="xs" withPadding>
          <List.Item>
            <Text component="span" fw={500}>{payment.userName}</Text> ({payment.userEmail})
          </List.Item>
          <List.Item>
            Payment Method: <Text component="span" fw={500}>{payment.paymentMethod}</Text>
          </List.Item>
          <List.Item>
            Payment Date: <Text component="span" fw={500}>{new Date(payment.paymentDate).toLocaleDateString()}</Text>
          </List.Item>
          {payment.description && (
            <List.Item>
              Description: <Text component="span" fw={500}>{payment.description}</Text>
            </List.Item>
          )}
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
            ${payment.amount.toFixed(2)}
          </Text>
        </Box>

        {/* Refund Reason - Required Field */}
        <Box>
          <Textarea
            label="Refund Reason"
            placeholder="Please explain why this refund is being processed..."
            value={refundReason}
            onChange={(event) => setRefundReason(event.currentTarget.value)}
            maxLength={500}
            minRows={3}
            required
            data-testid="refund-reason-textarea"
            styles={{
              root: {
                marginBottom: '4px'
              },
              label: {
                fontSize: '14px',
                fontWeight: 500,
                marginBottom: '8px'
              }
            }}
          />
          <Text size="xs" c="dimmed" ta="right">
            {remainingChars} / 500 characters remaining
          </Text>
        </Box>

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
            <List.Item>
              <Text size="sm">Process a ${payment.amount.toFixed(2)} refund to {payment.userName}</Text>
            </List.Item>
            <List.Item>
              <Text size="sm">Send a refund confirmation email to {payment.userEmail}</Text>
            </List.Item>
            <List.Item>
              <Text size="sm">Create an audit record with the refund reason</Text>
            </List.Item>
          </List>
        </Alert>

        {/* Cannot Undo Warning */}
        <Text size="sm" c="dimmed" ta="center" fw={500}>
          This action cannot be undone.
        </Text>

        {/* Confirmation Checkbox */}
        <Checkbox
          checked={confirmed}
          onChange={(event) => setConfirmed(event.currentTarget.checked)}
          label="I understand this will process the refund and cannot be undone"
          data-testid="refund-confirmation-checkbox"
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
            data-testid="refund-cancel-button"
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
            disabled={!confirmed || !refundReason.trim()}
            data-testid="refund-confirm-button"
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
            Process Refund
          </Button>
        </Group>
      </Stack>
    </Modal>
  );
};

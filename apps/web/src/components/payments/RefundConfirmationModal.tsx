import React, { useState, useEffect } from 'react';
import {
  Modal,
  Stack,
  Text,
  Button,
  Group,
  Title,
  Checkbox,
  List,
  Box,
  Textarea,
  NumberInput,
  Alert,
  Divider
} from '@mantine/core';
import { IconAlertCircle } from '@tabler/icons-react';
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
    remainingRefundableAmount: number;
  };
  onConfirm: (refundAmount: number, refundReason: string) => Promise<void>;
}

export const RefundConfirmationModal: React.FC<RefundConfirmationModalProps> = ({
  opened,
  onClose,
  payment,
  onConfirm
}) => {
  const [refundAmount, setRefundAmount] = useState<number>(0);
  const [refundReason, setRefundReason] = useState('');
  const [confirmed, setConfirmed] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errors, setErrors] = useState<{ amount?: string; reason?: string }>({});

  // Reset state when modal closes
  useEffect(() => {
    if (!opened) {
      setConfirmed(false);
      setRefundAmount(0);
      setRefundReason('');
      setErrors({});
    }
  }, [opened]);

  const validate = () => {
    const newErrors: { amount?: string; reason?: string } = {};

    if (!refundAmount || refundAmount <= 0) {
      newErrors.amount = 'Refund amount must be greater than $0';
    }

    if (refundAmount > payment.remainingRefundableAmount) {
      newErrors.amount = `Amount exceeds remaining refundable amount of $${payment.remainingRefundableAmount.toFixed(2)}`;
    }

    if (!refundReason || refundReason.trim().length < 10) {
      newErrors.reason = 'Refund reason must be at least 10 characters';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async () => {
    if (!validate()) {
      return;
    }

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
      await onConfirm(refundAmount, refundReason.trim());
      setConfirmed(false);
      setRefundAmount(0);
      setRefundReason('');
      setErrors({});
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
      setRefundAmount(0);
      setRefundReason('');
      setErrors({});
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
          Process Variable Refund
        </Title>
      }
      centered
      size="md"
      data-testid="refund-confirmation-modal"
    >
      <Stack gap="md">
        {/* Payment Info */}
        <Text size="sm">
          You are about to process a variable refund for:
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

        {/* Transaction Summary */}
        <Box
          style={{
            padding: '12px',
            backgroundColor: '#FAF6F2',
            borderRadius: '8px'
          }}
        >
          <Group justify="space-between" mb={8}>
            <Text size="sm" c="dimmed">Transaction Amount:</Text>
            <Text size="sm" fw={600}>${payment.amount.toFixed(2)}</Text>
          </Group>
          <Group justify="space-between" mb={8}>
            <Text size="sm" c="dimmed">Already Refunded:</Text>
            <Text size="sm" fw={600}>${(payment.amount - payment.remainingRefundableAmount).toFixed(2)}</Text>
          </Group>
          <Divider my={8} />
          <Group justify="space-between">
            <Text size="sm" fw={600}>Remaining Refundable:</Text>
            <Text size="lg" fw={700} c="wcr.7">${payment.remainingRefundableAmount.toFixed(2)}</Text>
          </Group>
        </Box>

        {/* Refund Amount Input */}
        <NumberInput
          label="Refund Amount"
          placeholder="Enter amount to refund"
          value={refundAmount}
          onChange={(value) => {
            setRefundAmount(typeof value === 'number' ? value : 0);
            // Clear amount error when user changes value
            if (errors.amount) {
              setErrors({ ...errors, amount: undefined });
            }
          }}
          min={0}
          max={payment.remainingRefundableAmount}
          decimalScale={2}
          fixedDecimalScale
          prefix="$"
          error={errors.amount}
          required
          data-testid="refund-amount-input"
          styles={{
            label: {
              fontSize: '14px',
              fontWeight: 600,
              marginBottom: '8px'
            }
          }}
        />

        {/* Refund Reason - Required Field */}
        <Box>
          <Textarea
            label="Refund Reason"
            placeholder="Explain why this refund is being processed (minimum 10 characters)..."
            value={refundReason}
            onChange={(event) => {
              setRefundReason(event.currentTarget.value);
              // Clear reason error when user changes value
              if (errors.reason) {
                setErrors({ ...errors, reason: undefined });
              }
            }}
            maxLength={500}
            minRows={3}
            error={errors.reason}
            required
            data-testid="refund-reason-textarea"
            styles={{
              root: {
                marginBottom: '4px'
              },
              label: {
                fontSize: '14px',
                fontWeight: 600,
                marginBottom: '8px'
              }
            }}
          />
          <Text size="xs" c="dimmed" ta="right">
            {remainingChars} / 500 characters remaining
          </Text>
        </Box>

        {/* RSVP Warning Alert */}
        <Alert icon={<IconAlertCircle size={16} />} color="blue" variant="light">
          <Text size="sm" fw={500}>
            ⚠️ RSVP/Ticket will NOT be cancelled
          </Text>
          <Text size="xs" c="dimmed">
            This is a financial refund only. The member will retain their event access.
          </Text>
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
            disabled={!confirmed || !refundAmount || refundAmount <= 0 || !refundReason || refundReason.trim().length < 10}
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

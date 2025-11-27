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
  Alert
} from '@mantine/core';
import { IconAlertTriangle } from '@tabler/icons-react';
import { useEventTimeZone } from '../../../../hooks/useEventTimeZone';

interface AnalyticsRefundModalProps {
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
  onConfirm: (refundAmount: number, refundReason: string) => Promise<void>;
}

export const AnalyticsRefundModal: React.FC<AnalyticsRefundModalProps> = ({
  opened,
  onClose,
  payment,
  onConfirm
}) => {
  const [refundAmount, setRefundAmount] = useState<number | string>(payment.amount);
  const [refundReason, setRefundReason] = useState('');
  const [confirmed, setConfirmed] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [amountError, setAmountError] = useState<string | null>(null);
  const eventTimeZone = useEventTimeZone();

  // Reset state when modal opens/closes
  useEffect(() => {
    if (opened) {
      setRefundAmount(payment.amount);
      setRefundReason('');
      setConfirmed(false);
      setAmountError(null);
    }
  }, [opened, payment.amount]);

  // Validate refund amount
  useEffect(() => {
    const amount = typeof refundAmount === 'string' ? parseFloat(refundAmount) : refundAmount;

    if (isNaN(amount)) {
      setAmountError('Please enter a valid amount');
    } else if (amount <= 0) {
      setAmountError('Refund amount must be greater than $0.00');
    } else if (amount > payment.amount) {
      setAmountError(`Refund amount cannot exceed original amount of $${payment.amount.toFixed(2)}`);
    } else {
      setAmountError(null);
    }
  }, [refundAmount, payment.amount]);

  const handleSubmit = async () => {
    if (!confirmed) {
      return;
    }

    const amount = typeof refundAmount === 'string' ? parseFloat(refundAmount) : refundAmount;

    if (isNaN(amount) || amount <= 0 || amount > payment.amount) {
      return;
    }

    setIsSubmitting(true);
    try {
      await onConfirm(amount, refundReason.trim() || 'No reason provided');
      handleClose();
    } catch (error) {
      // Error notification is handled by the hook
      console.error('Refund error:', error);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleClose = () => {
    if (!isSubmitting) {
      setRefundAmount(payment.amount);
      setRefundReason('');
      setConfirmed(false);
      setAmountError(null);
      onClose();
    }
  };

  const remainingChars = 500 - refundReason.length;
  const isSubmitDisabled = !confirmed || !!amountError || isSubmitting;

  return (
    <Modal
      opened={opened}
      onClose={handleClose}
      title={
        <Title order={3} style={{ color: '#880124' }}>
          Process Refund
        </Title>
      }
      centered
      size="md"
      data-testid="analytics-refund-modal"
    >
      <Stack gap="md">
        {/* Payment Info */}
        <Text size="sm" fw={600}>
          Payment Information:
        </Text>
        <List size="sm" spacing="xs" withPadding>
          <List.Item>
            <Text component="span" fw={500}>{payment.userName}</Text> ({payment.userEmail})
          </List.Item>
          <List.Item>
            Payment Method: <Text component="span" fw={500}>{payment.paymentMethod}</Text>
          </List.Item>
          <List.Item>
            Payment Date: <Text component="span" fw={500}>{new Date(payment.paymentDate).toLocaleDateString('en-US', { timeZone: eventTimeZone })}</Text>
          </List.Item>
          {payment.description && (
            <List.Item>
              Event: <Text component="span" fw={500}>{payment.description}</Text>
            </List.Item>
          )}
        </List>

        {/* Original Amount - Prominent Display */}
        <Box
          style={{
            padding: '12px',
            backgroundColor: '#FAF6F2',
            borderRadius: '8px',
            textAlign: 'center'
          }}
        >
          <Text size="sm" c="dimmed" mb={4}>
            Original Amount
          </Text>
          <Text size="xl" fw={700} c="charcoal">
            ${payment.amount.toFixed(2)}
          </Text>
        </Box>

        {/* Refund Amount Input */}
        <Box>
          <NumberInput
            label="Refund Amount"
            description="Enter the amount to refund (partial or full)"
            placeholder="0.00"
            value={refundAmount}
            onChange={setRefundAmount}
            min={0.01}
            max={payment.amount}
            decimalScale={2}
            fixedDecimalScale
            prefix="$"
            required
            error={amountError}
            data-testid="refund-amount-input"
            styles={{
              label: {
                fontSize: '14px',
                fontWeight: 500,
                marginBottom: '4px'
              },
              description: {
                fontSize: '12px',
                marginTop: '4px'
              },
              input: {
                fontSize: '16px',
                fontWeight: 600
              }
            }}
          />
        </Box>

        {/* Refund Reason - Optional Field */}
        <Box>
          <Textarea
            label="Refund Reason (Optional)"
            placeholder="Optional: Explain why this refund is being processed..."
            value={refundReason}
            onChange={(event) => setRefundReason(event.currentTarget.value)}
            maxLength={500}
            minRows={3}
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

        {/* PROMINENT DISCLAIMER */}
        <Alert
          icon={<IconAlertTriangle size={24} />}
          color="orange"
          title="IMPORTANT NOTICE"
          styles={{
            root: {
              backgroundColor: '#FFF4E6',
              border: '2px solid #FA5252'
            },
            title: {
              fontSize: '14px',
              fontWeight: 700,
              color: '#2B2B2B'
            },
            message: {
              fontSize: '14px',
              color: '#2B2B2B'
            }
          }}
        >
          <Text fw={500}>
            This will process a financial refund only. It will NOT cancel the user's RSVP or ticket registration.
          </Text>
        </Alert>

        {/* Confirmation Checkbox */}
        <Checkbox
          checked={confirmed}
          onChange={(event) => setConfirmed(event.currentTarget.checked)}
          label="I understand this is a financial refund and will NOT cancel the RSVP/ticket"
          data-testid="refund-confirmation-checkbox"
          styles={{
            label: {
              fontSize: '14px',
              fontWeight: 500,
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
            disabled={isSubmitDisabled}
            data-testid="refund-submit-button"
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

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
  Divider,
  Collapse,
  Badge,
  Table
} from '@mantine/core';
import { IconAlertCircle, IconHistory, IconChevronDown, IconChevronRight } from '@tabler/icons-react';
import { notifications } from '@mantine/notifications';
import { useEventTimeZone } from '../../hooks/useEventTimeZone';

/** Individual refund record for display in refund history */
interface RefundHistoryItem {
  id: string;
  amount: number;
  reason: string;
  status: string;
  processedAt: string;
  processedByName: string;
}

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
    /** Refund history for this ticket — shown in modal so admin can see past refunds */
    refundHistory?: RefundHistoryItem[];
    /** Total already refunded (sum of completed refunds) */
    totalRefunded?: number;
  };
  /**
   * Whether this event uses RSVPs (AllowRsvps=true).
   * Controls visibility of "Also remove RSVP" checkbox and warning language.
   * Optional — when not provided (e.g., admin payments page), RSVP checkbox is hidden.
   */
  allowRsvps?: boolean;
  /**
   * Whether this user has an active RSVP for this event.
   * Controls visibility of "Also remove RSVP" checkbox.
   * Optional — when not provided, RSVP checkbox is hidden.
   */
  hasRsvp?: boolean;
  /**
   * Callback when admin confirms the refund/cancellation.
   * Receives all parameters needed for the unified endpoint.
   */
  onConfirm: (
    refundAmount: number,
    refundReason: string,
    cancelTicket: boolean,
    alsoRemoveRsvp: boolean
  ) => Promise<void>;
}

export const RefundConfirmationModal: React.FC<RefundConfirmationModalProps> = ({
  opened,
  onClose,
  payment,
  allowRsvps,
  hasRsvp,
  onConfirm
}) => {
  const [refundAmount, setRefundAmount] = useState<number>(0);
  const [refundReason, setRefundReason] = useState('');
  const [cancelTicket, setCancelTicket] = useState(false);
  const [alsoRemoveRsvp, setAlsoRemoveRsvp] = useState(false);
  const [confirmed, setConfirmed] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errors, setErrors] = useState<{ amount?: string; reason?: string }>({});
  const [historyExpanded, setHistoryExpanded] = useState(false);
  const eventTimeZone = useEventTimeZone();

  // $0 ticket: no money involved, cancel-only mode
  const isZeroAmountTicket = payment.remainingRefundableAmount === 0 && payment.amount === 0;
  // Fully refunded: all money returned but ticket may still be active (can still cancel)
  const isFullyRefunded = payment.remainingRefundableAmount === 0 && payment.amount > 0;
  // Use provided totalRefunded or calculate from amount vs remaining
  const alreadyRefunded = payment.totalRefunded ?? (payment.amount - payment.remainingRefundableAmount);
  // Has past refunds to show
  const hasRefundHistory = payment.refundHistory && payment.refundHistory.length > 0;
  // Show RSVP checkbox: only when event uses RSVPs, user has active RSVP, and cancel is checked
  const showRsvpCheckbox = allowRsvps === true && hasRsvp === true && cancelTicket;

  // Reset state when modal closes
  useEffect(() => {
    if (!opened) {
      setConfirmed(false);
      setRefundAmount(0);
      setRefundReason('');
      setCancelTicket(false);
      setAlsoRemoveRsvp(false);
      setErrors({});
      setHistoryExpanded(false);
    }
  }, [opened]);

  // When cancel is unchecked, also uncheck RSVP removal
  useEffect(() => {
    if (!cancelTicket) {
      setAlsoRemoveRsvp(false);
    }
  }, [cancelTicket]);

  const validate = () => {
    const newErrors: { amount?: string; reason?: string } = {};

    // For $0 tickets or fully refunded tickets, skip amount validation
    if (!isZeroAmountTicket && !isFullyRefunded) {
      // $0 refund is only valid when cancelling a ticket
      if (refundAmount === 0 && !cancelTicket) {
        newErrors.amount = 'Refund amount must be greater than $0 (or check "Cancel ticket" for a $0 cancellation)';
      }

      if (refundAmount > payment.remainingRefundableAmount) {
        newErrors.amount = `Amount exceeds remaining refundable amount of $${payment.remainingRefundableAmount.toFixed(2)}`;
      }

      if (refundAmount < 0) {
        newErrors.amount = 'Refund amount cannot be negative';
      }
    }

    if (!refundReason || refundReason.trim().length < 10) {
      newErrors.reason = 'Reason must be at least 10 characters';
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
      await onConfirm(refundAmount, refundReason.trim(), cancelTicket, alsoRemoveRsvp);
      setConfirmed(false);
      setRefundAmount(0);
      setRefundReason('');
      setCancelTicket(false);
      setAlsoRemoveRsvp(false);
      setErrors({});
      onClose();
    } catch (error: any) {
      notifications.show({
        title: 'Error',
        message: error?.detail || error?.message || 'Failed to process request',
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
      setCancelTicket(false);
      setAlsoRemoveRsvp(false);
      setErrors({});
      onClose();
    }
  };

  // Build the dynamic modal title based on what action is being taken
  const getModalTitle = () => {
    if (isZeroAmountTicket) return 'Cancel Ticket';
    if (isFullyRefunded) return 'Cancel Ticket (Fully Refunded)';
    if (cancelTicket) return 'Cancel Ticket & Process Refund';
    return 'Process Refund';
  };

  // Build the dynamic warning message based on cancel + RSVP selections
  const getWarningContent = () => {
    if (isZeroAmountTicket || (cancelTicket && isFullyRefunded)) {
      // Cancel only (no money involved)
      if (alsoRemoveRsvp) {
        return {
          color: 'red' as const,
          title: 'Ticket AND RSVP will be cancelled',
          detail: 'The member will lose ALL event access (ticket and RSVP).'
        };
      }
      if (allowRsvps && hasRsvp) {
        return {
          color: 'yellow' as const,
          title: 'Ticket will be cancelled',
          detail: 'The member will lose ticket access but retains their free RSVP.'
        };
      }
      return {
        color: 'yellow' as const,
        title: 'Ticket will be cancelled',
        detail: 'The member will lose access to this event.'
      };
    }

    if (cancelTicket) {
      // Cancel + refund
      if (alsoRemoveRsvp) {
        return {
          color: 'red' as const,
          title: 'Ticket AND RSVP will be cancelled',
          detail: 'The member will lose ALL event access and receive the specified refund.'
        };
      }
      if (allowRsvps && hasRsvp) {
        return {
          color: 'yellow' as const,
          title: 'Ticket will be cancelled',
          detail: 'The member will lose ticket access but retains their free RSVP. Refund will be processed.'
        };
      }
      return {
        color: 'yellow' as const,
        title: 'Ticket will be cancelled',
        detail: 'The member will lose event access. Refund will be processed.'
      };
    }

    // Refund only (no cancel)
    return {
      color: 'blue' as const,
      title: 'Financial refund only',
      detail: 'The member will retain their event access. This is a refund without cancellation.'
    };
  };

  // Determine if the submit button should be disabled
  const isSubmitDisabled = () => {
    if (!confirmed) return true;
    if (!refundReason || refundReason.trim().length < 10) return true;

    // For zero amount or fully refunded tickets, only cancel action is available
    if (isZeroAmountTicket || isFullyRefunded) {
      return !cancelTicket; // Must check cancel for these cases
    }

    // Must either refund money or cancel (or both)
    if (refundAmount <= 0 && !cancelTicket) return true;

    return false;
  };

  // Build confirmation label
  const getConfirmationLabel = () => {
    const parts: string[] = [];
    if (refundAmount > 0) parts.push(`refund $${refundAmount.toFixed(2)}`);
    if (cancelTicket) parts.push('cancel the ticket');
    if (alsoRemoveRsvp) parts.push('remove the RSVP');
    if (parts.length === 0) parts.push('process this action');
    return `I understand this will ${parts.join(' and ')} and cannot be undone`;
  };

  const remainingChars = 500 - refundReason.length;
  const warning = getWarningContent();

  return (
    <Modal
      opened={opened}
      onClose={handleClose}
      title={
        <Title order={3} style={{ color: '#880124' }}>
          {getModalTitle()}
        </Title>
      }
      centered
      size="lg"
      data-testid="refund-confirmation-modal"
    >
      <Stack gap="md">
        {/* Payment Info */}
        <Text size="sm">
          {cancelTicket || isZeroAmountTicket
            ? 'You are about to cancel the ticket for:'
            : 'You are about to process a refund for:'}
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
              Description: <Text component="span" fw={500}>{payment.description}</Text>
            </List.Item>
          )}
        </List>

        {/* Transaction Summary - Only show for paid tickets */}
        {!isZeroAmountTicket && (
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
              <Text size="sm" fw={600} c={alreadyRefunded > 0 ? 'orange' : undefined}>
                ${alreadyRefunded.toFixed(2)}
              </Text>
            </Group>
            <Divider my={8} />
            <Group justify="space-between">
              <Text size="sm" fw={600}>Remaining Refundable:</Text>
              <Text size="lg" fw={700} c={payment.remainingRefundableAmount > 0 ? 'wcr.7' : 'dimmed'}>
                ${payment.remainingRefundableAmount.toFixed(2)}
              </Text>
            </Group>
          </Box>
        )}

        {/* Zero Amount Info Box */}
        {isZeroAmountTicket && (
          <Box
            style={{
              padding: '12px',
              backgroundColor: '#FAF6F2',
              borderRadius: '8px',
              textAlign: 'center'
            }}
          >
            <Text size="sm" c="dimmed" mb={4}>Ticket Amount</Text>
            <Text size="lg" fw={700} c="charcoal">$0.00 (Free Ticket)</Text>
            <Text size="xs" c="dimmed" mt={4}>No refund required - this will cancel the ticket only</Text>
          </Box>
        )}

        {/* Refund History - Expandable section showing past refunds */}
        {hasRefundHistory && (
          <Box>
            <Group
              gap={4}
              style={{ cursor: 'pointer' }}
              onClick={() => setHistoryExpanded(!historyExpanded)}
              data-testid="refund-history-toggle"
            >
              {historyExpanded ? <IconChevronDown size={16} /> : <IconChevronRight size={16} />}
              <IconHistory size={16} />
              <Text size="sm" fw={600}>
                Refund History ({payment.refundHistory!.length} refund{payment.refundHistory!.length !== 1 ? 's' : ''})
              </Text>
            </Group>
            <Collapse in={historyExpanded}>
              <Box
                mt="xs"
                style={{
                  border: '1px solid #E0E0E0',
                  borderRadius: '8px',
                  overflow: 'hidden'
                }}
              >
                <Table striped withTableBorder>
                  <Table.Thead style={{ backgroundColor: '#F5F5F5' }}>
                    <Table.Tr>
                      <Table.Th>Date</Table.Th>
                      <Table.Th>Amount</Table.Th>
                      <Table.Th>Status</Table.Th>
                      <Table.Th>By</Table.Th>
                      <Table.Th>Reason</Table.Th>
                    </Table.Tr>
                  </Table.Thead>
                  <Table.Tbody>
                    {payment.refundHistory!.map((refund) => (
                      <Table.Tr key={refund.id}>
                        <Table.Td>
                          <Text size="xs">
                            {new Date(refund.processedAt).toLocaleDateString('en-US', { timeZone: eventTimeZone })}
                          </Text>
                        </Table.Td>
                        <Table.Td>
                          <Text size="xs" fw={500} c="red">
                            -${refund.amount.toFixed(2)}
                          </Text>
                        </Table.Td>
                        <Table.Td>
                          <Badge
                            size="xs"
                            color={refund.status === 'Completed' ? 'green' : refund.status === 'Failed' ? 'red' : 'yellow'}
                          >
                            {refund.status}
                          </Badge>
                        </Table.Td>
                        <Table.Td>
                          <Text size="xs">{refund.processedByName}</Text>
                        </Table.Td>
                        <Table.Td>
                          <Text size="xs" lineClamp={2} title={refund.reason}>
                            {refund.reason}
                          </Text>
                        </Table.Td>
                      </Table.Tr>
                    ))}
                  </Table.Tbody>
                </Table>
              </Box>
            </Collapse>
          </Box>
        )}

        {/* Refund Amount Input - Show for paid tickets with remaining balance */}
        {!isZeroAmountTicket && !isFullyRefunded && (
          <NumberInput
            label="Refund Amount"
            placeholder="Enter amount to refund"
            description={cancelTicket ? 'Enter $0 to cancel without a refund, or specify a refund amount' : undefined}
            value={refundAmount}
            onChange={(value) => {
              setRefundAmount(typeof value === 'number' ? value : 0);
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
            required={!cancelTicket}
            data-testid="refund-amount-input"
            styles={{
              label: {
                fontSize: '14px',
                fontWeight: 600,
                marginBottom: '8px'
              }
            }}
          />
        )}

        {/* Cancel Ticket Checkbox - Always available for non-zero tickets */}
        {!isZeroAmountTicket && (
          <Checkbox
            checked={cancelTicket}
            onChange={(event) => setCancelTicket(event.currentTarget.checked)}
            label="Cancel ticket (revoke event access)"
            description={cancelTicket
              ? 'The member will lose access to the event'
              : 'Leave unchecked for a financial refund only — member keeps access'}
            data-testid="cancel-ticket-checkbox"
            styles={{
              root: {
                padding: '12px',
                backgroundColor: cancelTicket ? '#FFF0F0' : '#F8F8F8',
                borderRadius: '8px',
                border: cancelTicket ? '1px solid #E57373' : '1px solid #E0E0E0',
                transition: 'all 0.2s ease'
              },
              label: {
                fontSize: '14px',
                fontWeight: 600,
                color: '#2B2B2B'
              },
              description: {
                fontSize: '12px'
              }
            }}
          />
        )}

        {/* Also Remove RSVP Checkbox - Only when event uses RSVPs AND cancel is checked */}
        {showRsvpCheckbox && (
          <Checkbox
            checked={alsoRemoveRsvp}
            onChange={(event) => setAlsoRemoveRsvp(event.currentTarget.checked)}
            label="Also remove RSVP"
            description={alsoRemoveRsvp
              ? 'The member will lose ALL event access (ticket + RSVP)'
              : 'Leave unchecked to let the member keep their free RSVP attendance'}
            data-testid="also-remove-rsvp-checkbox"
            styles={{
              root: {
                padding: '12px',
                marginLeft: '24px',
                backgroundColor: alsoRemoveRsvp ? '#FFF0F0' : '#FFF8F0',
                borderRadius: '8px',
                border: alsoRemoveRsvp ? '1px solid #E57373' : '1px solid #B8B0A8',
                transition: 'all 0.2s ease'
              },
              label: {
                fontSize: '14px',
                fontWeight: 500,
                color: '#2B2B2B'
              },
              description: {
                fontSize: '12px'
              }
            }}
          />
        )}

        {/* Reason - Required Field */}
        <Box>
          <Textarea
            label={cancelTicket || isZeroAmountTicket ? 'Cancellation Reason' : 'Refund Reason'}
            placeholder={cancelTicket || isZeroAmountTicket
              ? 'Explain why this ticket is being cancelled (minimum 10 characters)...'
              : 'Explain why this refund is being processed (minimum 10 characters)...'}
            value={refundReason}
            onChange={(event) => {
              setRefundReason(event.currentTarget.value);
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

        {/* Warning Alert - Dynamic based on cancel/RSVP selections */}
        <Alert icon={<IconAlertCircle size={16} />} color={warning.color} variant="light">
          <Text size="sm" fw={500}>
            {warning.title}
          </Text>
          <Text size="xs" c="dimmed">
            {warning.detail}
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
          label={getConfirmationLabel()}
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
            disabled={isSubmitDisabled()}
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
            {isZeroAmountTicket || (cancelTicket && refundAmount === 0)
              ? 'Cancel Ticket'
              : cancelTicket
                ? 'Cancel & Refund'
                : 'Process Refund'}
          </Button>
        </Group>
      </Stack>
    </Modal>
  );
};

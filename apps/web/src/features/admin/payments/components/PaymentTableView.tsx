import React, { useState } from 'react';
import {
  Table,
  Text,
  Badge,
  Button,
  Stack,
  Card,
  Group,
  Divider,
  Box,
  Loader,
  Alert
} from '@mantine/core';
import { IconReceiptOff, IconAlertCircle } from '@tabler/icons-react';
import { useMediaQuery } from '@mantine/hooks';
import { RefundConfirmationModal } from '../../../../components/payments/RefundConfirmationModal';
import { useRefundTicket } from '../hooks/useRefundTicket';
import type { PaymentTransactionDto } from '../hooks/usePayments';

interface PaymentTableViewProps {
  payments: PaymentTransactionDto[];
  isLoading: boolean;
  error: Error | null;
  onClearFilters: () => void;
}

const getPaymentMethodColor = (method: string): string => {
  switch (method) {
    case 'PayPal':
      return 'blue';
    case 'Free':
      return 'gray';
    case 'Venmo':
      return 'teal';
    default:
      return 'gray';
  }
};

const getStatusColor = (status: string): string => {
  switch (status) {
    case 'Paid':
      return 'green';
    case 'Refunded':
      return 'orange';
    case 'Pending':
      return 'yellow';
    case 'Failed':
      return 'red';
    default:
      return 'gray';
  }
};

const formatDate = (dateString: string): string => {
  const date = new Date(dateString);
  return date.toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  });
};

export const PaymentTableView: React.FC<PaymentTableViewProps> = ({
  payments,
  isLoading,
  error,
  onClearFilters
}) => {
  const isMobile = useMediaQuery('(max-width: 767px)');
  const [refundModalOpened, setRefundModalOpened] = useState(false);
  const [selectedPayment, setSelectedPayment] = useState<PaymentTransactionDto | null>(null);
  const refundMutation = useRefundTicket();

  const handleRefundClick = (payment: PaymentTransactionDto) => {
    setSelectedPayment(payment);
    setRefundModalOpened(true);
  };

  const handleRefundConfirm = async (refundReason: string) => {
    if (!selectedPayment?.ticketId) {
      throw new Error('No ticket ID available for refund');
    }

    await refundMutation.mutateAsync({
      ticketId: selectedPayment.ticketId,
      refundReason,
      alsoRemoveRsvp: true
    });
  };

  const handleModalClose = () => {
    setRefundModalOpened(false);
    setSelectedPayment(null);
  };

  // Loading State
  if (isLoading) {
    return (
      <Box style={{ textAlign: 'center', padding: '40px' }}>
        <Loader size="lg" color="wcr.7" />
        <Text c="dimmed" size="sm" mt="md">
          Loading transactions...
        </Text>
      </Box>
    );
  }

  // Error State
  if (error) {
    return (
      <Alert
        icon={<IconAlertCircle />}
        color="red"
        title="Error Loading Payments"
      >
        <Text>{error.message || 'Failed to load payment transactions'}</Text>
        <Text size="sm" c="dimmed" mt="sm">
          Please try again or contact support if the problem persists.
        </Text>
      </Alert>
    );
  }

  // Empty State
  if (payments.length === 0) {
    return (
      <Box p="xl" ta="center">
        <Stack gap="md" align="center">
          <IconReceiptOff size={48} color="var(--mantine-color-dimmed)" />
          <Text c="dimmed" size="lg" fw={500}>
            No transactions found
          </Text>
          <Text c="dimmed" size="sm">
            Try adjusting your filters or search term
          </Text>
          <Button
            variant="light"
            color="wcr.7"
            onClick={onClearFilters}
            data-testid="clear-filters-button"
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
            Clear Filters
          </Button>
        </Stack>
      </Box>
    );
  }

  // Mobile Card Layout
  if (isMobile) {
    return (
      <>
        <Stack gap="md">
          {payments.map((payment) => (
            <Card
              key={payment.id}
              shadow="sm"
              padding="md"
              radius="md"
              withBorder
              data-testid="payment-card"
              style={{ cursor: 'pointer' }}
            >
              <Stack gap="xs">
                {/* Date and Status */}
                <Group justify="space-between">
                  <Text size="xs" c="dimmed">
                    {formatDate(payment.paymentDate || '')}
                  </Text>
                  <Badge color={getStatusColor(payment.status || '')}>
                    {payment.status}
                  </Badge>
                </Group>

                {/* User */}
                <Text fw={600} size="md">
                  {payment.userName}
                </Text>
                <Text size="xs" c="dimmed">
                  {payment.userEmail}
                </Text>

                <Divider />

                {/* Event */}
                <Group justify="space-between">
                  <Text size="sm" c="dimmed">
                    Event:
                  </Text>
                  <Text size="sm" fw={500} style={{ textAlign: 'right', flex: 1 }}>
                    {payment.eventName}
                  </Text>
                </Group>

                {/* Payment Method */}
                <Group justify="space-between">
                  <Text size="sm" c="dimmed">
                    Method:
                  </Text>
                  <Badge color={getPaymentMethodColor(payment.paymentMethod || '')}>
                    {payment.paymentMethod}
                  </Badge>
                </Group>

                {/* Amount */}
                <Group justify="space-between">
                  <Text size="sm" c="dimmed">
                    Amount:
                  </Text>
                  <Text fw={700} size="lg">
                    ${payment.amount?.toFixed(2) || '0.00'}
                  </Text>
                </Group>

                {/* Refund Button */}
                {payment.isRefundable && (
                  <Button
                    variant="light"
                    color="red"
                    fullWidth
                    data-testid={`refund-button-${payment.ticketId}`}
                    onClick={(e) => {
                      e.stopPropagation();
                      handleRefundClick(payment);
                    }}
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
                )}
              </Stack>
            </Card>
          ))}
        </Stack>

        {/* Refund Modal */}
        {selectedPayment && (
          <RefundConfirmationModal
            opened={refundModalOpened}
            onClose={handleModalClose}
            payment={{
              id: selectedPayment.id || '',
              userName: selectedPayment.userName || '',
              userEmail: selectedPayment.userEmail || '',
              amount: selectedPayment.amount || 0,
              paymentMethod: selectedPayment.paymentMethod || '',
              paymentDate: selectedPayment.paymentDate || '',
              description: selectedPayment.eventName || ''
            }}
            onConfirm={handleRefundConfirm}
          />
        )}
      </>
    );
  }

  // Desktop Table Layout
  return (
    <>
      <Table striped highlightOnHover data-testid="payment-table">
        <Table.Thead style={{ backgroundColor: 'var(--mantine-color-wcr-7)' }}>
          <Table.Tr>
            <Table.Th c="white" style={{ width: '140px' }}>
              Date
            </Table.Th>
            <Table.Th c="white" style={{ width: '200px' }}>
              User/Member
            </Table.Th>
            <Table.Th c="white" style={{ minWidth: '180px' }}>
              Event/Session
            </Table.Th>
            <Table.Th c="white" style={{ width: '120px', textAlign: 'center' }}>
              Payment Method
            </Table.Th>
            <Table.Th c="white" style={{ width: '100px', textAlign: 'right' }}>
              Amount
            </Table.Th>
            <Table.Th c="white" style={{ width: '120px', textAlign: 'center' }}>
              Status
            </Table.Th>
            <Table.Th c="white" style={{ width: '120px', textAlign: 'center' }}>
              Actions
            </Table.Th>
          </Table.Tr>
        </Table.Thead>

        <Table.Tbody>
          {payments.map((payment) => (
            <Table.Tr key={payment.id} data-testid="payment-row">
              <Table.Td>
                <Text size="sm">{formatDate(payment.paymentDate || '')}</Text>
              </Table.Td>
              <Table.Td>
                <Stack gap={0}>
                  <Text fw={500} size="sm">
                    {payment.userName}
                  </Text>
                  <Text size="xs" c="dimmed">
                    {payment.userEmail}
                  </Text>
                </Stack>
              </Table.Td>
              <Table.Td>
                <Text size="sm" lineClamp={2}>
                  {payment.eventName}
                </Text>
              </Table.Td>
              <Table.Td style={{ textAlign: 'center' }}>
                <Badge color={getPaymentMethodColor(payment.paymentMethod || '')}>
                  {payment.paymentMethod}
                </Badge>
              </Table.Td>
              <Table.Td style={{ textAlign: 'right' }}>
                <Text fw={600} size="md">
                  ${payment.amount?.toFixed(2) || '0.00'}
                </Text>
              </Table.Td>
              <Table.Td style={{ textAlign: 'center' }}>
                <Badge color={getStatusColor(payment.status || '')}>
                  {payment.status}
                </Badge>
              </Table.Td>
              <Table.Td
                style={{ textAlign: 'center' }}
                onClick={(e) => e.stopPropagation()}
              >
                {payment.isRefundable ? (
                  <Button
                    variant="light"
                    color="red"
                    size="xs"
                    data-testid={`refund-button-${payment.ticketId}`}
                    onClick={() => handleRefundClick(payment)}
                    styles={{
                      root: {
                        fontWeight: 600,
                        height: '32px',
                        fontSize: '12px',
                        lineHeight: '1.2'
                      }
                    }}
                  >
                    Refund
                  </Button>
                ) : (
                  <Text size="sm" c="dimmed">
                    —
                  </Text>
                )}
              </Table.Td>
            </Table.Tr>
          ))}
        </Table.Tbody>
      </Table>

      {/* Refund Modal */}
      {selectedPayment && (
        <RefundConfirmationModal
          opened={refundModalOpened}
          onClose={handleModalClose}
          payment={{
            id: selectedPayment.id || '',
            userName: selectedPayment.userName || '',
            userEmail: selectedPayment.userEmail || '',
            amount: selectedPayment.amount || 0,
            paymentMethod: selectedPayment.paymentMethod || '',
            paymentDate: selectedPayment.paymentDate || '',
            description: selectedPayment.eventName || ''
          }}
          onConfirm={handleRefundConfirm}
        />
      )}
    </>
  );
};

import React from 'react';
import { Box, Title, Text, Group, Stack } from '@mantine/core';
import { usePaymentFilters } from '../../features/admin/payments/hooks/usePaymentFilters';
import { usePayments } from '../../features/admin/payments/hooks/usePayments';
import { PaymentFilterBar } from '../../features/admin/payments/components/PaymentFilterBar';
import { PaymentTableView } from '../../features/admin/payments/components/PaymentTableView';

/**
 * AdminPaymentsPage - Administrative interface for viewing and managing payment transactions
 *
 * Features:
 * - View all payment transactions with comprehensive filtering
 * - Filter by date range, payment method, status, amount, and search term
 * - Process refunds for PayPal paid tickets
 * - Summary statistics showing filtered results
 * - Integration with RefundConfirmationModal for safe refund operations
 *
 * Routes:
 * - /admin/analytics/payments - This page (payment management)
 */
export const AdminPaymentsPage: React.FC = () => {
  const {
    filterState,
    rawSearchTerm,
    updateFilter,
    clearFilters,
    activeFilters,
    apiFilters
  } = usePaymentFilters();

  const { data, isLoading, error } = usePayments(apiFilters);

  const payments = data?.transactions || [];
  const totalCount = data?.totalCount || 0;
  const filteredCount = payments.length; // Backend doesn't return filteredCount, use array length

  // Calculate total revenue from filtered payments
  const totalRevenue = payments.reduce((sum, payment) => sum + (payment.amount || 0), 0);

  return (
    <Box p="xl">
      {/* Page Header */}
      <Title
        order={1}
        mb="xl"
        c="wcr.7"
        ff="Bodoni Moda, serif"
        size="2.5rem"
      >
        Payment Transactions
      </Title>

      {/* Filter Bar */}
      <PaymentFilterBar
        searchTerm={rawSearchTerm}
        onSearchChange={(value) => updateFilter({ searchTerm: value })}
        startDate={filterState.startDate}
        endDate={filterState.endDate}
        onDateRangeChange={(dates) =>
          updateFilter({ startDate: dates[0], endDate: dates[1] })
        }
        paymentMethods={filterState.paymentMethods}
        onPaymentMethodsChange={(values) => updateFilter({ paymentMethods: values })}
        statuses={filterState.statuses}
        onStatusesChange={(values) => updateFilter({ statuses: values })}
        minAmount={filterState.minAmount}
        onMinAmountChange={(value) => updateFilter({ minAmount: value })}
        maxAmount={filterState.maxAmount}
        onMaxAmountChange={(value) => updateFilter({ maxAmount: value })}
        onClearFilters={clearFilters}
      />

      {/* Payment Table */}
      <PaymentTableView
        payments={payments}
        isLoading={isLoading}
        error={error}
        onClearFilters={clearFilters}
      />

      {/* Summary Statistics */}
      {!isLoading && !error && payments.length > 0 && (
        <Box
          mt="md"
          p="md"
          style={{
            backgroundColor: '#FAF6F2',
            borderRadius: '8px'
          }}
        >
          <Group gap="xl" wrap="wrap">
            {/* Showing X of Y */}
            <Group gap="xs">
              <Text size="sm" c="dimmed">
                Showing:
              </Text>
              <Text size="sm" fw={600}>
                {filteredCount} of {totalCount} transactions
              </Text>
            </Group>

            {/* Active Filters */}
            {activeFilters.length > 0 && (
              <Group gap="xs">
                <Text size="sm" c="dimmed">
                  Filtered by:
                </Text>
                <Text size="sm" fw={600}>
                  {activeFilters.join(', ')}
                </Text>
              </Group>
            )}

            {/* Search Term */}
            {filterState.searchTerm && (
              <Group gap="xs">
                <Text size="sm" c="dimmed">
                  Search:
                </Text>
                <Text size="sm" fw={600}>
                  "{filterState.searchTerm}"
                </Text>
              </Group>
            )}

            {/* Total Revenue */}
            <Group gap="xs">
              <Text size="sm" c="dimmed">
                Total Revenue:
              </Text>
              <Text size="sm" fw={600} c="wcr.7">
                ${totalRevenue.toFixed(2)}
              </Text>
            </Group>
          </Group>
        </Box>
      )}

      {/* Mobile Summary Statistics */}
      {!isLoading && !error && payments.length > 0 && (
        <Stack
          gap="xs"
          mt="md"
          p="md"
          hiddenFrom="sm"
          style={{
            backgroundColor: '#FAF6F2',
            borderRadius: '8px'
          }}
        >
          <Text size="sm" c="dimmed">
            Showing:{' '}
            <Text component="span" fw={600}>
              {filteredCount} of {totalCount} transactions
            </Text>
          </Text>

          {activeFilters.length > 0 && (
            <Text size="sm" c="dimmed">
              Filtered by:{' '}
              <Text component="span" fw={600}>
                {activeFilters.join(', ')}
              </Text>
            </Text>
          )}

          {filterState.searchTerm && (
            <Text size="sm" c="dimmed">
              Search:{' '}
              <Text component="span" fw={600}>
                "{filterState.searchTerm}"
              </Text>
            </Text>
          )}

          <Text size="sm" c="dimmed">
            Total Revenue:{' '}
            <Text component="span" fw={600} c="wcr.7">
              ${totalRevenue.toFixed(2)}
            </Text>
          </Text>
        </Stack>
      )}
    </Box>
  );
};

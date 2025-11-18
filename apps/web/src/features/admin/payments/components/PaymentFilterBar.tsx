import React, { useState } from 'react';
import {
  Group,
  TextInput,
  MultiSelect,
  NumberInput,
  Button,
  Stack,
  Drawer,
  Box
} from '@mantine/core';
import { DatePickerInput } from '@mantine/dates';
import { IconSearch, IconFilter, IconFilterOff } from '@tabler/icons-react';
import { useMediaQuery } from '@mantine/hooks';

interface PaymentFilterBarProps {
  searchTerm: string;
  onSearchChange: (value: string) => void;
  startDate: Date | null;
  endDate: Date | null;
  onDateRangeChange: (dates: [Date | null, Date | null]) => void;
  paymentMethods: string[];
  onPaymentMethodsChange: (values: string[]) => void;
  statuses: string[];
  onStatusesChange: (values: string[]) => void;
  minAmount: number | undefined;
  onMinAmountChange: (value: number | undefined) => void;
  maxAmount: number | undefined;
  onMaxAmountChange: (value: number | undefined) => void;
  onClearFilters: () => void;
}

const PAYMENT_METHOD_OPTIONS = [
  { value: 'PayPal', label: 'PayPal' },
  { value: 'Free', label: 'Free' },
  { value: 'Venmo', label: 'Venmo' }
];

const STATUS_OPTIONS = [
  { value: 'Paid', label: 'Paid' },
  { value: 'Refunded', label: 'Refunded' },
  { value: 'Pending', label: 'Pending' },
  { value: 'Failed', label: 'Failed' }
];

export const PaymentFilterBar: React.FC<PaymentFilterBarProps> = ({
  searchTerm,
  onSearchChange,
  startDate,
  endDate,
  onDateRangeChange,
  paymentMethods,
  onPaymentMethodsChange,
  statuses,
  onStatusesChange,
  minAmount,
  onMinAmountChange,
  maxAmount,
  onMaxAmountChange,
  onClearFilters
}) => {
  const [drawerOpen, setDrawerOpen] = useState(false);
  const isMobile = useMediaQuery('(max-width: 767px)');

  const hasActiveFilters =
    searchTerm ||
    startDate ||
    endDate ||
    paymentMethods.length > 0 ||
    statuses.length > 0 ||
    minAmount !== undefined ||
    maxAmount !== undefined;

  // Mobile Layout - Drawer with filters
  if (isMobile) {
    return (
      <Stack gap="md" mb="lg">
        {/* Search always visible */}
        <TextInput
          placeholder="Search transactions..."
          leftSection={<IconSearch size="1rem" />}
          value={searchTerm}
          onChange={(e) => onSearchChange(e.currentTarget.value)}
          data-testid="payment-search-input"
          styles={{
            root: {
              width: '100%'
            }
          }}
        />

        {/* Filters Drawer Button */}
        <Group gap="sm">
          <Button
            variant="light"
            leftSection={<IconFilter size="1rem" />}
            onClick={() => setDrawerOpen(true)}
            flex={1}
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
            Filters
            {hasActiveFilters && ` (${paymentMethods.length + statuses.length + (startDate ? 1 : 0) + (minAmount !== undefined || maxAmount !== undefined ? 1 : 0)})`}
          </Button>

          {hasActiveFilters && (
            <Button
              variant="light"
              color="red"
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
              <IconFilterOff size="1rem" />
            </Button>
          )}
        </Group>

        {/* Drawer with all filter controls */}
        <Drawer
          opened={drawerOpen}
          onClose={() => setDrawerOpen(false)}
          title="Filter Transactions"
          position="bottom"
          size="auto"
        >
          <Stack gap="md" p="md">
            {/* Date Range */}
            <DatePickerInput
              type="range"
              label="Date Range"
              placeholder="Select dates"
              value={[startDate, endDate]}
              onChange={onDateRangeChange}
              data-testid="payment-date-range"
              clearable
            />

            {/* Payment Method Multi-Select */}
            <MultiSelect
              label="Payment Method"
              placeholder="All methods"
              data={PAYMENT_METHOD_OPTIONS}
              value={paymentMethods}
              onChange={onPaymentMethodsChange}
              data-testid="payment-method-filter"
              clearable
              searchable
            />

            {/* Status Multi-Select */}
            <MultiSelect
              label="Status"
              placeholder="All statuses"
              data={STATUS_OPTIONS}
              value={statuses}
              onChange={onStatusesChange}
              data-testid="payment-status-filter"
              clearable
              searchable
            />

            {/* Amount Range */}
            <Group gap="sm">
              <NumberInput
                label="Min Amount"
                placeholder="$0.00"
                prefix="$"
                value={minAmount}
                onChange={(value) => onMinAmountChange(typeof value === 'number' ? value : undefined)}
                data-testid="payment-min-amount"
                min={0}
                decimalScale={2}
                flex={1}
              />
              <NumberInput
                label="Max Amount"
                placeholder="$999.99"
                prefix="$"
                value={maxAmount}
                onChange={(value) => onMaxAmountChange(typeof value === 'number' ? value : undefined)}
                data-testid="payment-max-amount"
                min={0}
                decimalScale={2}
                flex={1}
              />
            </Group>

            <Button
              variant="light"
              color="wcr.7"
              onClick={() => {
                onClearFilters();
                setDrawerOpen(false);
              }}
              fullWidth
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
              Clear All Filters
            </Button>
          </Stack>
        </Drawer>
      </Stack>
    );
  }

  // Desktop Layout - All filters visible
  return (
    <Box mb="lg">
      <Group justify="space-between" align="flex-start" wrap="wrap" gap="md">
        {/* Left: Main filters */}
        <Group align="flex-end" gap="md" flex={1}>
          {/* Search Input */}
          <TextInput
            placeholder="Search transactions..."
            leftSection={<IconSearch size="1rem" />}
            value={searchTerm}
            onChange={(e) => onSearchChange(e.currentTarget.value)}
            data-testid="payment-search-input"
            styles={{
              root: {
                minWidth: '300px'
              }
            }}
          />

          {/* Date Range */}
          <DatePickerInput
            type="range"
            label="Date Range"
            placeholder="Select dates"
            value={[startDate, endDate]}
            onChange={onDateRangeChange}
            data-testid="payment-date-range"
            clearable
            styles={{
              root: {
                minWidth: '280px'
              }
            }}
          />

          {/* Payment Method Multi-Select */}
          <MultiSelect
            label="Payment Method"
            placeholder="All methods"
            data={PAYMENT_METHOD_OPTIONS}
            value={paymentMethods}
            onChange={onPaymentMethodsChange}
            data-testid="payment-method-filter"
            clearable
            searchable
            styles={{
              root: {
                minWidth: '200px'
              }
            }}
          />

          {/* Status Multi-Select */}
          <MultiSelect
            label="Status"
            placeholder="All statuses"
            data={STATUS_OPTIONS}
            value={statuses}
            onChange={onStatusesChange}
            data-testid="payment-status-filter"
            clearable
            searchable
            styles={{
              root: {
                minWidth: '180px'
              }
            }}
          />
        </Group>

        {/* Right: Amount Range */}
        <Group align="flex-end" gap="sm">
          <NumberInput
            label="Min Amount"
            placeholder="$0.00"
            prefix="$"
            value={minAmount}
            onChange={(value) => onMinAmountChange(typeof value === 'number' ? value : undefined)}
            data-testid="payment-min-amount"
            min={0}
            decimalScale={2}
            styles={{
              root: {
                width: '120px'
              }
            }}
          />
          <NumberInput
            label="Max Amount"
            placeholder="$999.99"
            prefix="$"
            value={maxAmount}
            onChange={(value) => onMaxAmountChange(typeof value === 'number' ? value : undefined)}
            data-testid="payment-max-amount"
            min={0}
            decimalScale={2}
            styles={{
              root: {
                width: '120px'
              }
            }}
          />

          {hasActiveFilters && (
            <Button
              variant="light"
              color="wcr.7"
              leftSection={<IconFilterOff size="1rem" />}
              onClick={onClearFilters}
              data-testid="clear-filters-button"
              styles={{
                root: {
                  fontWeight: 600,
                  height: '36px',
                  fontSize: '14px',
                  lineHeight: '1.2'
                }
              }}
            >
              Clear Filters
            </Button>
          )}
        </Group>
      </Group>
    </Box>
  );
};

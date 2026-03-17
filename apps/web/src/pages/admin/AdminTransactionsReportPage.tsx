import React, { useState, useMemo } from 'react';
import {
  Container,
  Title,
  Text,
  Box,
  Paper,
  Group,
  Skeleton,
  Alert,
  Button,
  Stack,
} from '@mantine/core';
import { DatePickerInput } from '@mantine/dates';
import { BarChart } from '@mantine/charts';
import { useQuery } from '@tanstack/react-query';
import {
  IconAlertCircle,
  IconRefresh,
  IconChartBar,
} from '@tabler/icons-react';
import {
  useDailyTransactions,
  type DailyTransactionDay,
} from '../../features/admin/reports/hooks/useDailyTransactions';
import { apiClient } from '../../lib/api/client';
// Reuse existing payment table component for drill-down to avoid duplication
import { PaymentTableView } from '../../features/admin/payments/components/PaymentTableView';
import type { PaymentListResponse } from '../../features/admin/payments/hooks/usePayments';

/**
 * Helper: format a Date to "YYYY-MM-DD" for the API query parameter.
 * Uses local date parts to avoid timezone-shift issues.
 */
const toISODateString = (date: Date): string => {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
};

/**
 * Helper: format "2026-03-15" to a shorter "Mar 15" label for the chart X-axis.
 */
const toShortLabel = (isoDate: string): string => {
  // Parse as local date to avoid off-by-one timezone issues
  const [year, month, day] = isoDate.split('-').map(Number);
  const date = new Date(year, month - 1, day);
  return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
};

/**
 * AdminTransactionsReportPage
 *
 * Chart-over-table layout: a stacked bar chart showing daily transaction counts
 * with click-to-drill-down into a detail payment table for the selected day.
 * Reuses the existing PaymentTableView component for the detail view.
 */
export const AdminTransactionsReportPage: React.FC = () => {
  // Default to last 30 days
  const [dateRange, setDateRange] = useState<[Date | null, Date | null]>(() => {
    const today = new Date();
    const thirtyDaysAgo = new Date();
    thirtyDaysAgo.setDate(today.getDate() - 30);
    return [thirtyDaysAgo, today];
  });

  const [selectedDate, setSelectedDate] = useState<string | null>(null);

  // Convert Date objects to ISO strings for the API
  const dateFrom = dateRange[0] ? toISODateString(dateRange[0]) : '';
  const dateTo = dateRange[1] ? toISODateString(dateRange[1]) : '';

  const {
    data: transactionsData,
    isLoading: isChartLoading,
    error: chartError,
    refetch: refetchChart,
  } = useDailyTransactions(dateFrom, dateTo);

  // Transform API data into chart-friendly format with short date labels
  const chartData = useMemo(() => {
    if (!transactionsData?.days) return [];
    return transactionsData.days.map((day: DailyTransactionDay) => ({
      // Short label for the X-axis (e.g., "Mar 15")
      date: toShortLabel(day.date),
      // Keep the full ISO date for the click handler
      fullDate: day.date,
      completedCount: day.completedCount,
      failedCount: day.failedCount,
      incompleteCount: day.incompleteCount,
    }));
  }, [transactionsData]);

  // Find the selected day's data for the summary line
  const selectedDayData = useMemo(() => {
    if (!selectedDate || !transactionsData?.days) return null;
    return transactionsData.days.find((d) => d.date === selectedDate) ?? null;
  }, [selectedDate, transactionsData]);

  // Drill-down: fetch payment transactions for the selected date.
  // Uses useQuery directly (instead of usePayments hook) so we can set enabled: false
  // when no date is selected — avoids an unnecessary fetch of ALL payments on page load.
  const {
    data: paymentsData,
    isLoading: isPaymentsLoading,
    error: paymentsError,
  } = useQuery<PaymentListResponse>({
    queryKey: ['admin-payments', 'drill-down', selectedDate],
    queryFn: async () => {
      const params = new URLSearchParams();
      params.append('startDate', selectedDate!);
      params.append('endDate', selectedDate!);
      params.append('page', '1');
      params.append('pageSize', '50');
      params.append('sortBy', 'paymentDate');
      params.append('sortDirection', 'Desc');
      const response = await apiClient.get(`/api/admin/payments?${params}`);
      return response.data;
    },
    enabled: !!selectedDate,
    staleTime: 5 * 60 * 1000,
    refetchOnWindowFocus: false,
  });

  // Only show drill-down table when a date is selected
  const showDrillDown = selectedDate !== null;

  return (
    <Container size="xl" py="xl">
      {/* Page Title — matches AdminDashboardPage title styling */}
      <Box mb="xl">
        <Title
          order={1}
          style={{
            fontFamily: "'Montserrat', sans-serif",
            fontSize: '32px',
            fontWeight: 800,
            color: '#880124',
            textTransform: 'uppercase',
            letterSpacing: '-0.5px',
          }}
        >
          Transaction Overview
        </Title>
      </Box>

      {/* Date Range Picker — styled to match PaymentFilterBar */}
      <Group mb="lg" align="flex-end">
        <DatePickerInput
          type="range"
          label="Date Range"
          placeholder="Select dates"
          value={dateRange}
          onChange={(value) => {
            setDateRange(value);
            // Clear drill-down selection when date range changes
            setSelectedDate(null);
          }}
          clearable
          firstDayOfWeek={0}
          styles={{
            root: {
              minWidth: '280px',
            },
          }}
        />
      </Group>

      {/* Chart Section */}
      {chartError && (
        <Alert
          icon={<IconAlertCircle />}
          color="red"
          title="Error Loading Chart Data"
          mb="lg"
        >
          <Text>{chartError.message || 'Failed to load transaction chart data.'}</Text>
          <Button
            variant="light"
            color="red"
            size="xs"
            mt="sm"
            onClick={() => refetchChart()}
            leftSection={<IconRefresh size={14} />}
          >
            Retry
          </Button>
        </Alert>
      )}

      {isChartLoading ? (
        <Skeleton height={350} radius="md" mb="lg" />
      ) : (
        chartData.length > 0 && (
          <Paper
            shadow="sm"
            radius="md"
            p="lg"
            mb="lg"
            style={{
              background: '#FFF8F0',
              border: '1px solid rgba(136, 1, 36, 0.1)',
            }}
          >
            <BarChart
              h={350}
              data={chartData}
              dataKey="date"
              series={[
                { name: 'completedCount', color: 'green.6', label: 'Completed' },
                { name: 'failedCount', color: 'red.6', label: 'Failed' },
                { name: 'incompleteCount', color: 'yellow.6', label: 'Incomplete' },
              ]}
              tickLine="y"
              withLegend
              legendProps={{ verticalAlign: 'top' }}
              barProps={{
                // Recharts Bar onClick signature: (data, index, event)
                // data is the bar's render info { x, y, width, height, value, payload }
                // data.payload contains the original chart data row including fullDate
                onClick: (data: any) => {
                  const fullDate = data?.payload?.fullDate || data?.fullDate;
                  if (fullDate) {
                    setSelectedDate(fullDate);
                  }
                },
                style: { cursor: 'pointer' },
              }}
            />
          </Paper>
        )
      )}

      {/* Empty state when chart has no data and is not loading */}
      {!isChartLoading && !chartError && chartData.length === 0 && dateFrom && dateTo && (
        <Paper
          shadow="sm"
          radius="md"
          p="xl"
          mb="lg"
          style={{
            background: '#FFF8F0',
            border: '1px solid rgba(136, 1, 36, 0.1)',
            textAlign: 'center',
          }}
        >
          <IconChartBar size={48} color="#8B8680" style={{ marginBottom: '12px' }} />
          <Text c="dimmed" size="lg">
            No transactions found for the selected date range
          </Text>
        </Paper>
      )}

      {/* Summary Line — visible when a date is selected */}
      {showDrillDown && selectedDayData && (
        <Paper
          p="md"
          mb="lg"
          radius="md"
          style={{
            background: 'rgba(136, 1, 36, 0.05)',
            border: '1px solid rgba(136, 1, 36, 0.1)',
          }}
        >
          <Group gap="xs" wrap="wrap">
            <Text size="sm" fw={600}>
              {selectedDayData.completedCount} completed transaction{selectedDayData.completedCount !== 1 ? 's' : ''} on{' '}
              {toShortLabel(selectedDate!)} totaling ${selectedDayData.completedAmount.toFixed(2)}
            </Text>
            <Text size="sm" c="dimmed">|</Text>
            <Text size="sm" c="red">{selectedDayData.failedCount} failed</Text>
            <Text size="sm" c="dimmed">|</Text>
            <Text size="sm" c="yellow.8">{selectedDayData.incompleteCount} incomplete</Text>
          </Group>
        </Paper>
      )}

      {/* Drill-Down Detail Table — reuses existing PaymentTableView */}
      {showDrillDown ? (
        <Box>
          <Group justify="space-between" mb="md">
            <Text size="lg" fw={600} style={{ fontFamily: "'Montserrat', sans-serif" }}>
              Transactions for {toShortLabel(selectedDate!)}
            </Text>
            <Button
              variant="subtle"
              size="xs"
              onClick={() => setSelectedDate(null)}
            >
              Clear Selection
            </Button>
          </Group>
          <PaymentTableView
            payments={paymentsData?.transactions || []}
            isLoading={isPaymentsLoading}
            error={paymentsError}
          />
        </Box>
      ) : (
        /* No Selection Prompt — encourages chart interaction */
        !isChartLoading &&
        !chartError &&
        chartData.length > 0 && (
          <Stack align="center" py="xl" gap="md">
            <IconChartBar size={48} color="#8B8680" />
            <Text c="dimmed" size="lg" ta="center">
              Click a day on the chart to see transaction details
            </Text>
          </Stack>
        )
      )}
    </Container>
  );
};

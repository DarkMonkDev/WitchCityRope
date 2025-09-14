// Payment Summary Component
// Shows payment breakdown and order details

import React from 'react';
import {
  Box,
  Stack,
  Group,
  Text,
  Title,
  Paper,
  Badge,
  Divider
} from '@mantine/core';
import { IconCalendar, IconMapPin, IconUser, IconClock } from '@tabler/icons-react';
import type { PaymentEventInfo, SlidingScaleCalculation } from '../types/payment.types';

interface PaymentSummaryProps {
  /** Event information */
  eventInfo: PaymentEventInfo;
  /** Sliding scale calculation */
  calculation: SlidingScaleCalculation;
  /** Processing fees (if any) */
  processingFee?: number;
  /** Whether to show detailed breakdown */
  detailed?: boolean;
  /** Custom title */
  title?: string;
}

/**
 * Payment summary component showing order details and price breakdown
 */
export const PaymentSummary: React.FC<PaymentSummaryProps> = ({
  eventInfo,
  calculation,
  processingFee = 0,
  detailed = true,
  title = "Order Summary"
}) => {
  const totalAmount = calculation.finalAmount + processingFee;

  const formatDateTime = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      weekday: 'short',
      month: 'short',
      day: 'numeric',
      hour: 'numeric',
      minute: '2-digit'
    });
  };

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: eventInfo.currency || 'USD'
    }).format(amount);
  };

  return (
    <Paper 
      radius="md" 
      p="lg" 
      withBorder
      style={{ 
        backgroundColor: '#FAF6F2',
        border: '1px solid #D4A5A5'
      }}
    >
      <Stack gap="md">
        {/* Section Title */}
        <Title order={4} c="#880124">
          {title}
        </Title>

        {/* Event Details */}
        <Box>
          <Text fw={600} size="md" mb="xs">
            {eventInfo.title}
          </Text>
          
          <Stack gap="xs">
            <Group gap="xs" align="center">
              <IconCalendar size={16} color="#6B0119" />
              <Text size="sm" c="dimmed">
                {formatDateTime(eventInfo.startDateTime)}
              </Text>
            </Group>

            {eventInfo.endDateTime && (
              <Group gap="xs" align="center">
                <IconClock size={16} color="#6B0119" />
                <Text size="sm" c="dimmed">
                  Duration: {formatDateTime(eventInfo.startDateTime)} - {formatDateTime(eventInfo.endDateTime)}
                </Text>
              </Group>
            )}

            {eventInfo.instructorName && (
              <Group gap="xs" align="center">
                <IconUser size={16} color="#6B0119" />
                <Text size="sm" c="dimmed">
                  Instructor: {eventInfo.instructorName}
                </Text>
              </Group>
            )}

            {eventInfo.location && (
              <Group gap="xs" align="center">
                <IconMapPin size={16} color="#6B0119" />
                <Text size="sm" c="dimmed">
                  {eventInfo.location}
                </Text>
              </Group>
            )}
          </Stack>
        </Box>

        <Divider />

        {/* Price Breakdown */}
        <Stack gap="sm">
          {/* Base Price */}
          <Group justify="space-between" align="center">
            <Text size="sm">Event Fee:</Text>
            <Text size="sm">
              {calculation.display.original}
            </Text>
          </Group>

          {/* Sliding Scale Discount */}
          {calculation.discountAmount > 0 && (
            <>
              <Group justify="space-between" align="center">
                <Group gap="xs">
                  <Text size="sm" c="green.7">
                    Sliding Scale Discount:
                  </Text>
                  <Badge size="xs" color="green" variant="light">
                    {calculation.display.percentage}
                  </Badge>
                </Group>
                <Text size="sm" c="green.7">
                  -{calculation.display.discount}
                </Text>
              </Group>

              {detailed && (
                <Box>
                  <Text size="xs" c="dimmed" style={{ fontStyle: 'italic' }}>
                    Community discount applied • Honor-based system
                  </Text>
                </Box>
              )}
            </>
          )}

          {/* Processing Fee */}
          {processingFee > 0 && (
            <Group justify="space-between" align="center">
              <Text size="sm">Processing Fee:</Text>
              <Text size="sm">
                {formatCurrency(processingFee)}
              </Text>
            </Group>
          )}

          {/* Divider before total */}
          <Divider />

          {/* Total Amount */}
          <Group justify="space-between" align="center">
            <Text fw={700} size="lg" c="#880124">
              Total:
            </Text>
            <Text fw={700} size="xl" c="#880124">
              {formatCurrency(totalAmount)}
            </Text>
          </Group>
        </Stack>

        {/* Savings Display */}
        {calculation.discountAmount > 0 && detailed && (
          <Paper p="md" radius="sm" bg="rgba(34, 139, 34, 0.1)" withBorder>
            <Group justify="center" gap="xs">
              <Text size="sm" fw={500} c="green.8">
                You're saving {calculation.display.discount} with sliding scale pricing!
              </Text>
            </Group>
          </Paper>
        )}

        {/* Payment Notes */}
        {detailed && (
          <Box>
            <Stack gap="xs">
              <Text size="xs" c="dimmed" fw={500}>
                Payment Notes:
              </Text>
              <Text size="xs" c="dimmed">
                • Payment will be processed securely through Stripe
              </Text>
              <Text size="xs" c="dimmed">
                • You will receive an email receipt upon successful payment
              </Text>
              <Text size="xs" c="dimmed">
                • Registration is confirmed immediately after payment
              </Text>
              {calculation.discountAmount > 0 && (
                <Text size="xs" c="dimmed">
                  • Sliding scale usage is completely confidential
                </Text>
              )}
            </Stack>
          </Box>
        )}
      </Stack>
    </Paper>
  );
};

/**
 * Compact payment summary for mobile or small spaces
 */
export const CompactPaymentSummary: React.FC<PaymentSummaryProps> = ({
  eventInfo,
  calculation,
  processingFee = 0
}) => {
  const totalAmount = calculation.finalAmount + processingFee;

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: eventInfo.currency || 'USD'
    }).format(amount);
  };

  return (
    <Paper radius="md" p="md" withBorder>
      <Stack gap="sm">
        <Group justify="space-between" align="center">
          <Text fw={500} size="sm" truncate style={{ flex: 1 }}>
            {eventInfo.title}
          </Text>
          <Text fw={700} c="#880124">
            {formatCurrency(totalAmount)}
          </Text>
        </Group>

        {calculation.discountAmount > 0 && (
          <Group justify="space-between" align="center">
            <Text size="xs" c="green.7">
              Sliding Scale ({calculation.display.percentage}):
            </Text>
            <Text size="xs" c="green.7">
              -{calculation.display.discount}
            </Text>
          </Group>
        )}

        <Text size="xs" c="dimmed" ta="center">
          Registration confirmed upon payment
        </Text>
      </Stack>
    </Paper>
  );
};
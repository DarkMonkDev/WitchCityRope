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
import type { components } from '@witchcityrope/shared-types/generated/api-types';
import { useEventTimeZone } from '../../../hooks/useEventTimeZone';
import { formatAbbreviatedDate } from '../../../utils/eventUtils';

type TicketTypeDto = components["schemas"]["TicketTypeDto"];
type SessionDto = components["schemas"]["SessionDto"];

interface PaymentSummaryProps {
  /** Event information */
  eventInfo: PaymentEventInfo;
  /** Sliding scale calculation (legacy - optional) */
  calculation?: SlidingScaleCalculation;
  /** Selected tickets (new multi-ticket support) */
  selectedTickets?: TicketTypeDto[];
  /** Prices for each selected ticket (ticketId -> price) */
  ticketPrices?: Record<string, number>;
  /** Event sessions for showing dates */
  sessions?: SessionDto[];
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
  selectedTickets = [],
  ticketPrices = {},
  sessions = [],
  processingFee = 0,
  detailed = true,
  title = "Order Summary"
}) => {
  const eventTimeZone = useEventTimeZone();

  // Calculate total from individual ticket prices if available, otherwise use legacy calculation
  const ticketsTotal = Object.values(ticketPrices).reduce((sum, price) => sum + price, 0);
  const totalAmount = selectedTickets.length > 0
    ? ticketsTotal + processingFee
    : (calculation?.finalAmount || 0) + processingFee;

  const formatDateTime = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      weekday: 'short',
      month: 'short',
      day: 'numeric',
      timeZone: eventTimeZone
    });
  };

  // Format time from stored "naive UTC" - DO NOT use timezone conversion
  // User-entered event times are stored as UTC values that represent local time
  const formatTime = (dateString: string) => {
    const date = new Date(dateString);
    const hours = date.getUTCHours();
    const minutes = date.getUTCMinutes();
    const period = hours >= 12 ? 'PM' : 'AM';
    const hour12 = hours % 12 || 12;
    const minuteStr = minutes.toString().padStart(2, '0');
    return `${hour12}:${minuteStr} ${period}`;
  };

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: eventInfo.currency || 'USD'
    }).format(amount);
  };

  /**
   * Get formatted session dates for a ticket
   */
  const getTicketSessionDates = (ticket: TicketTypeDto): string => {
    if (!ticket.sessionIdentifiers || ticket.sessionIdentifiers.length === 0) {
      return '';
    }

    const matchingSessions = sessions.filter(session =>
      ticket.sessionIdentifiers?.includes(session.sessionIdentifier || '')
    );

    if (matchingSessions.length === 0) {
      return '';
    }

    const formattedDates = matchingSessions
      .filter(session => session.date)
      .map(session => formatAbbreviatedDate(session.date!, eventTimeZone))
      .join(', ');

    return formattedDates;
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
                  {formatTime(eventInfo.startDateTime)} - {formatTime(eventInfo.endDateTime)}
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
        {selectedTickets.length > 0 ? (
          <Stack gap="sm">
            {/* Individual Ticket Line Items */}
            {selectedTickets.map((ticket) => {
              const price = ticketPrices[ticket.id || ''] || 0;
              const sessionDates = getTicketSessionDates(ticket);
              return (
                <Box key={ticket.id}>
                  <Group justify="space-between" align="flex-start">
                    <Box style={{ flex: 1 }}>
                      <Text size="sm">{ticket.name}</Text>
                      {sessionDates && (
                        <Text size="xs" c="dimmed" mt={2}>
                          {sessionDates}
                        </Text>
                      )}
                    </Box>
                    <Text size="sm" style={{ whiteSpace: 'nowrap', marginLeft: '8px' }}>
                      {formatCurrency(price)}
                    </Text>
                  </Group>
                </Box>
              );
            })}

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
        ) : (
          <Box py="md">
            <Text size="sm" c="dimmed" ta="center">
              Select tickets to see pricing details
            </Text>
          </Box>
        )}

        {/* Payment Notes */}
        {detailed && (
          <Box>
            <Stack gap="xs">
              <Text size="xs" c="dimmed" fw={500}>
                Payment Notes:
              </Text>
              <Text size="xs" c="dimmed">
                • Payment will be processed securely through PayPal
              </Text>
              <Text size="xs" c="dimmed">
                • You will receive an email receipt upon successful payment
              </Text>
              <Text size="xs" c="dimmed">
                • Registration is confirmed immediately after payment
              </Text>
              {calculation && calculation.discountAmount > 0 && (
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
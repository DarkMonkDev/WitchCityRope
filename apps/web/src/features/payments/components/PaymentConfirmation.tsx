// Payment Confirmation Component
// Displays successful payment confirmation with registration details

import React from 'react';
import {
  Box,
  Stack,
  Group,
  Text,
  Title,
  Button,
  Paper,
  Badge,
  Alert,
  Divider,
  ThemeIcon,
  List
} from '@mantine/core';
import { useMediaQuery } from '@mantine/hooks';
import { IconCheck, IconMapPin, IconTicket, IconCreditCard, IconMail, IconDownload } from '@tabler/icons-react';
import type { PaymentResponse, PaymentEventInfo } from '../types/payment.types';
import { paymentUtils } from '../utils/paymentUtils';
import { useEventTimeZone } from '../../../hooks/useEventTimeZone';

interface PurchasedTicketSession {
  name: string;
  date: string;
  timeRange: string;
}

interface PurchasedTicket {
  id: string;
  name: string;
  sessions: PurchasedTicketSession[];
}

interface PaymentConfirmationProps {
  /** Payment details */
  payment: PaymentResponse;
  /** Event information */
  eventInfo: PaymentEventInfo;
  /** Purchased tickets (for multi-session events) */
  purchasedTickets?: PurchasedTicket[];
  /** Callback to view registrations */
  onViewRegistrations?: () => void;
  /** Callback to register for more events */
  onRegisterMore?: () => void;
  /** Callback to download receipt */
  onDownloadReceipt?: () => void;
}

/**
 * Payment success confirmation screen
 */
export const PaymentConfirmation: React.FC<PaymentConfirmationProps> = ({
  payment,
  eventInfo,
  purchasedTickets,
  onViewRegistrations,
  onRegisterMore,
  onDownloadReceipt
}) => {
  const eventTimeZone = useEventTimeZone();
  const isMobile = useMediaQuery('(max-width: 991px)');

  const formatDateTime = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      weekday: 'long',
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: 'numeric',
      minute: '2-digit',
      timeZone: eventTimeZone
    });
  };

  return (
    <Box maw={600} mx="auto">
      <Stack gap={isMobile ? 'md' : 'xl'}>
        {/* Success Header */}
        <Paper
          radius="md"
          p="xl"
          ta="center"
          bg="linear-gradient(135deg, rgba(34, 139, 34, 0.1), rgba(76, 175, 80, 0.05))"
          mb={isMobile ? 0 : undefined}
        >
          <Stack gap="md" align="center">
            <ThemeIcon
              size={80}
              radius="xl"
              color="green"
              variant="light"
            >
              <IconCheck size={40} />
            </ThemeIcon>
            
            <Title order={1} c="green.7">
              Payment Successful!
            </Title>
            
            <Text size="lg" c="green.6">
              Your registration is confirmed
            </Text>
          </Stack>
        </Paper>

        {/* Event Details */}
        <Paper radius="md" p="lg" withBorder mb={isMobile ? 0 : undefined}>
          <Stack gap="md">
            <Title order={3} c="#880124">
              Event Details
            </Title>

            {/* Event Name */}
            <Text fw={600} size="lg">{eventInfo.title}</Text>

            {/* Location (if available) */}
            {eventInfo.location && (
              <Group gap="sm">
                <IconMapPin size={18} color="#6B0119" />
                <Text>Location: {eventInfo.location}</Text>
              </Group>
            )}

            {/* Ticket Information - Show purchased tickets with session times */}
            {purchasedTickets && purchasedTickets.length > 0 && (
              <Stack gap="xs">
                <Text size="sm" fw={500} c="dimmed" tt="uppercase">Your Ticket(s)</Text>
                {purchasedTickets.map((ticket, index) => (
                  <Group key={ticket.id || index} gap="sm" align="flex-start">
                    <IconTicket size={18} color="#6B0119" style={{ marginTop: 4 }} />
                    <Box>
                      <Text fw={600}>{ticket.name}</Text>
                      {ticket.sessions && ticket.sessions.length > 0 && (
                        <Stack gap="xs" mt={4}>
                          {ticket.sessions.map((session, sessionIndex) => (
                            <Box key={sessionIndex}>
                              <Text size="sm" c="dimmed" fw={500}>
                                {session.name}
                              </Text>
                              <Text size="sm" c="dimmed">
                                {session.date}{session.timeRange && ` • ${session.timeRange}`}
                              </Text>
                            </Box>
                          ))}
                        </Stack>
                      )}
                    </Box>
                  </Group>
                ))}
              </Stack>
            )}

            <Divider />

            {/* Payment Information */}
            <Stack gap="sm">
              <Group gap="sm">
                <IconCreditCard size={18} color="#880124" />
                <Box>
                  <Group gap="xs">
                    <Text fw={500}>Payment:</Text>
                    <Text fw={600} size="lg" c="#880124">
                      ${payment.amount ? payment.amount.toFixed(2) : '0.00'}
                    </Text>
                  </Group>
                  
                  {payment.originalAmount && payment.originalAmount > payment.amount && (
                    <Group gap="xs">
                      <Text size="sm" c="dimmed">
                        Original Price: {paymentUtils.formatCurrency(payment.originalAmount)}
                      </Text>
                      <Badge size="sm" color="green" variant="light">
                        {Math.round(payment.slidingScalePercentage)}% Sliding Scale Applied
                      </Badge>
                    </Group>
                  )}
                </Box>
              </Group>

              <Group gap="sm">
                <IconMail size={18} color="#6B0119" />
                <Text size="sm" c="dimmed">
                  Confirmation email sent to your registered email address
                </Text>
              </Group>
            </Stack>
          </Stack>
        </Paper>

        {/* What's Next Section */}
        <Paper radius="md" p="lg" bg="#FAF6F2" withBorder>
          <Stack gap="md">
            <Title order={4} c="#880124">
              What's Next:
            </Title>

            <List
              size="sm"
              spacing="sm"
              styles={{
                root: { paddingLeft: 0 },
                item: { paddingLeft: '1.2em' }
              }}
            >
              <List.Item>
                <Text size="sm" c="dark">
                  Check your email for detailed event information and location details
                </Text>
              </List.Item>
              <List.Item>
                <Text size="sm" c="dark">
                  Contact us at info@witchcityrope.com if you have any questions
                </Text>
              </List.Item>
            </List>
          </Stack>
        </Paper>

        {/* Community Appreciation */}
        {payment.slidingScalePercentage > 0 && (
          <Alert
            color="purple"
            variant="light"
            icon={<IconCheck />}
          >
            <Text size="sm">
              Thank you for being part of our community! Your participation helps us maintain 
              accessible events for everyone. We're grateful to have you join us.
            </Text>
          </Alert>
        )}

        {/* Action Buttons */}
        <Group justify="center" gap="md">
          {onDownloadReceipt && (
            <Button
              variant="outline"
              color="wcr"
              leftSection={<IconDownload size={16} />}
              onClick={onDownloadReceipt}
            >
              Download Receipt
            </Button>
          )}

          {onViewRegistrations && (
            <Button
              variant="light"
              color="wcr"
              onClick={onViewRegistrations}
            >
              View My Events
            </Button>
          )}

          {onRegisterMore && (
            <Button
              color="#880124"
              onClick={onRegisterMore}
              styles={(_theme) => ({
                root: {
                  background: 'linear-gradient(135deg, #FFB800, #DAA520)',
                  border: 'none',
                  borderRadius: '12px 6px 12px 6px',
                  color: '#2C2C2C',
                  fontFamily: 'Montserrat, sans-serif',
                  fontWeight: 600,
                  transition: 'all 0.3s ease',
                  '&:hover': {
                    borderRadius: '6px 12px 6px 12px',
                    boxShadow: '0 4px 12px rgba(255, 191, 0, 0.3)',
                    transform: 'translateY(-1px)'
                  }
                }
              })}
            >
              Join More Events
            </Button>
          )}
        </Group>

        {/* Receipt Information */}
        <Paper radius="md" p="md" bg="gray.1">
          <Stack gap="xs">
            <Text size="xs" fw={500} c="dimmed">Receipt Information</Text>
            <Group justify="space-between">
              <Text size="xs" c="dimmed">Transaction ID:</Text>
              <Text size="xs" fw={500}>{payment.id.slice(-12).toUpperCase()}</Text>
            </Group>
            <Group justify="space-between">
              <Text size="xs" c="dimmed">Payment Date:</Text>
              <Text size="xs" fw={500}>
                {payment.processedAt ? 
                  formatDateTime(payment.processedAt) : 
                  formatDateTime(payment.createdAt)
                }
              </Text>
            </Group>
            <Group justify="space-between">
              <Text size="xs" c="dimmed">Payment Method:</Text>
              <Text size="xs" fw={500}>
                {payment.paymentMethodType === 0 ? 'Saved Card' : 'Credit Card'}
              </Text>
            </Group>
          </Stack>
        </Paper>
      </Stack>
    </Box>
  );
};
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
  ThemeIcon
} from '@mantine/core';
import { 
  IconCheck, 
  IconCalendar, 
  IconMapPin, 
  IconUser, 
  IconTicket,
  IconCreditCard,
  IconMail,
  IconDownload
} from '@tabler/icons-react';
import type { PaymentResponse, PaymentEventInfo } from '../types/payment.types';
import { paymentUtils } from '../api/paymentApi';

interface PaymentConfirmationProps {
  /** Payment details */
  payment: PaymentResponse;
  /** Event information */
  eventInfo: PaymentEventInfo;
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
  onViewRegistrations,
  onRegisterMore,
  onDownloadReceipt
}) => {
  const formatDateTime = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      weekday: 'long',
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: 'numeric',
      minute: '2-digit'
    });
  };

  const formatTime = (dateString: string) => {
    return new Date(dateString).toLocaleTimeString('en-US', {
      hour: 'numeric',
      minute: '2-digit'
    });
  };

  return (
    <Box maw={600} mx="auto">
      <Stack gap="xl">
        {/* Success Header */}
        <Paper 
          radius="md" 
          p="xl"
          ta="center"
          bg="linear-gradient(135deg, rgba(34, 139, 34, 0.1), rgba(76, 175, 80, 0.05))"
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

        {/* Registration Details */}
        <Paper radius="md" p="lg" withBorder>
          <Stack gap="md">
            <Title order={3} c="#880124">
              Registration Details
            </Title>

            {/* Event Information */}
            <Stack gap="sm">
              <Group gap="sm">
                <IconCalendar size={20} color="#880124" />
                <Box>
                  <Text fw={600} size="lg">{eventInfo.title}</Text>
                  <Text c="dimmed" size="sm">
                    {formatDateTime(eventInfo.startDateTime)} - {formatTime(eventInfo.endDateTime)}
                  </Text>
                </Box>
              </Group>

              {eventInfo.instructorName && (
                <Group gap="sm">
                  <IconUser size={18} color="#6B0119" />
                  <Text>Instructor: {eventInfo.instructorName}</Text>
                </Group>
              )}

              {eventInfo.location && (
                <Group gap="sm">
                  <IconMapPin size={18} color="#6B0119" />
                  <Text>{eventInfo.location}</Text>
                </Group>
              )}
            </Stack>

            <Divider />

            {/* Payment Information */}
            <Stack gap="sm">
              <Group gap="sm">
                <IconTicket size={18} color="#880124" />
                <Text fw={500}>Registration ID: </Text>
                <Badge variant="light" color="wcr">
                  #{payment.eventRegistrationId.slice(-8).toUpperCase()}
                </Badge>
              </Group>

              <Group gap="sm">
                <IconCreditCard size={18} color="#880124" />
                <Box>
                  <Group gap="xs">
                    <Text fw={500}>Payment:</Text>
                    <Text fw={600} size="lg" c="#880124">
                      {payment.displayAmount}
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

            <Stack gap="sm">
              <Group gap="sm" align="flex-start">
                <Text fw={500} size="sm" c="#880124">•</Text>
                <Text size="sm">
                  Check your email for detailed event information and location details
                </Text>
              </Group>
              
              <Group gap="sm" align="flex-start">
                <Text fw={500} size="sm" c="#880124">•</Text>
                <Text size="sm">
                  Add this event to your calendar using the link in your confirmation email
                </Text>
              </Group>
              
              <Group gap="sm" align="flex-start">
                <Text fw={500} size="sm" c="#880124">•</Text>
                <Text size="sm">
                  Join our community Discord for event updates and pre-event discussions
                </Text>
              </Group>
              
              <Group gap="sm" align="flex-start">
                <Text fw={500} size="sm" c="#880124">•</Text>
                <Text size="sm">
                  Contact us at events@witchcityrope.com if you have any questions
                </Text>
              </Group>
            </Stack>
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
              View My Registrations
            </Button>
          )}

          {onRegisterMore && (
            <Button
              color="#880124"
              onClick={onRegisterMore}
              styles={(theme) => ({
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
              Register for More Events
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
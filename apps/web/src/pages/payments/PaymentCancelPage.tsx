// Payment Cancel Page
// Displays when user cancels PayPal payment

import React from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import {
  Container,
  Stack,
  Title,
  Text,
  Alert,
  Button,
  Paper,
  Group,
  Divider
} from '@mantine/core';
import { 
  IconCircleX, 
  IconArrowLeft, 
  IconRefresh, 
  IconInfoCircle 
} from '@tabler/icons-react';

/**
 * Payment cancellation page component
 * Handles the return from PayPal when user cancels payment
 */
export const PaymentCancelPage: React.FC = () => {
  const [searchParams] = useSearchParams();
  
  // Get parameters from PayPal redirect
  const token = searchParams.get('token');
  const eventId = searchParams.get('eventId');
  const registrationId = searchParams.get('registrationId');

  return (
    <Container size="md" py="xl">
      <Stack gap="xl">
        {/* Cancel Header */}
        <Paper p="xl" radius="md" bg="orange.0" withBorder>
          <Stack gap="md" align="center">
            <IconCircleX size={64} style={{ color: '#fd7e14' }} />
            <Title order={2} c="orange.7" ta="center">
              Payment Cancelled
            </Title>
            <Text size="lg" ta="center" c="dimmed">
              Your PayPal payment was cancelled. No charges were made.
            </Text>
          </Stack>
        </Paper>

        {/* Information */}
        <Alert 
          icon={<IconInfoCircle size={16} />}
          color="blue"
          variant="light"
        >
          <Text size="sm">
            <strong>What happened?</strong> You cancelled the payment process on PayPal. 
            Your registration is not complete and no charges were made to your account.
          </Text>
        </Alert>

        {/* Options */}
        <Paper p="lg" radius="md" withBorder>
          <Title order={3} mb="md" c="#880124">
            What would you like to do?
          </Title>
          
          <Stack gap="md">
            <Text size="sm" c="dimmed">
              You have several options to complete your registration:
            </Text>
            
            <Stack gap="xs" ml="md">
              <Text size="sm">
                • <strong>Try Again:</strong> Return to the payment page to complete your registration
              </Text>
              <Text size="sm">
                • <strong>Browse Events:</strong> Look for other events that interest you
              </Text>
              <Text size="sm">
                • <strong>Contact Us:</strong> Get help with payment or registration questions
              </Text>
            </Stack>
          </Stack>
        </Paper>

        {/* Payment Issues Help */}
        <Paper p="lg" radius="md" bg="gray.0" withBorder>
          <Title order={4} mb="sm" c="gray.7">
            Having Payment Issues?
          </Title>
          
          <Text size="sm" c="dimmed" mb="md">
            If you experienced problems during payment, here are some common solutions:
          </Text>
          
          <Stack gap="xs" ml="md">
            <Text size="sm">
              • Ensure your PayPal account has sufficient funds or a valid payment method
            </Text>
            <Text size="sm">
              • Check that your browser allows pop-ups from our site
            </Text>
            <Text size="sm">
              • Try using a different browser or device
            </Text>
            <Text size="sm">
              • Clear your browser cache and cookies, then try again
            </Text>
          </Stack>
        </Paper>

        <Divider />

        {/* Action Buttons */}
        <Group justify="center" gap="md">
          {eventId && (
            <Button 
              component={Link} 
              to={`/events/${eventId}/register`}
              variant="filled"
              color="#880124"
              size="lg"
              leftSection={<IconRefresh size={20} />}
            >
              Try Payment Again
            </Button>
          )}
          
          <Button 
            component={Link} 
            to="/events"
            variant="outline"
            color="blue"
            size="lg"
            leftSection={<IconArrowLeft size={20} />}
          >
            Browse Events
          </Button>
        </Group>

        {/* Support Info */}
        <Alert color="gray" variant="light">
          <Text size="sm" ta="center">
            <strong>Need Help?</strong> Contact us at{' '}
            <Text component="a" href="mailto:support@witchcityrope.com" c="blue.6">
              support@witchcityrope.com
            </Text>{' '}
            {token && (
              <>
                or reference Token: <strong>{token}</strong>
              </>
            )}
          </Text>
        </Alert>

        {/* Sliding Scale Reminder */}
        <Paper p="md" radius="sm" bg="#FAF6F2" withBorder>
          <Text size="sm" ta="center" c="#6B0119">
            <strong>Remember:</strong> We offer sliding scale pricing to make events 
            accessible to everyone. Choose the amount that works for your situation - 
            no questions asked.
          </Text>
        </Paper>
      </Stack>
    </Container>
  );
};
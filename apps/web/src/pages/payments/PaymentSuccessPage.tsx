// Payment Success Page
// Displays success confirmation after PayPal payment completion

import React, { useEffect, useState } from 'react';
import { useSearchParams, useNavigate, Link } from 'react-router-dom';
import {
  Container,
  Stack,
  Title,
  Text,
  Alert,
  Button,
  Paper,
  Group,
  Divider,
  LoadingOverlay
} from '@mantine/core';
import { 
  IconCircleCheck, 
  IconCalendar, 
  IconMail, 
  IconArrowRight 
} from '@tabler/icons-react';
import { useQuery } from '@tanstack/react-query';
import { paymentApi } from '../../features/payments/api/paymentApi';
import type { PaymentResponse } from '../../features/payments/types/payment.types';

/**
 * Payment success page component
 * Handles the return from PayPal after successful payment
 */
export const PaymentSuccessPage: React.FC = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const [isLoading, setIsLoading] = useState(true);
  
  // Get parameters from PayPal redirect
  const paymentId = searchParams.get('paymentId');
  const token = searchParams.get('token');
  const payerID = searchParams.get('PayerID');

  // Fetch payment details
  const { data: paymentData, error, isLoading: isPaymentLoading } = useQuery<PaymentResponse>({
    queryKey: ['payment', paymentId],
    queryFn: () => paymentApi.getPayment(paymentId!),
    enabled: !!paymentId,
    retry: 3,
    refetchOnWindowFocus: false
  });

  useEffect(() => {
    // Simulate verification process
    const timer = setTimeout(() => {
      setIsLoading(false);
    }, 2000);

    return () => clearTimeout(timer);
  }, []);

  // If no payment ID, redirect to home
  useEffect(() => {
    if (!paymentId && !isLoading) {
      navigate('/', { replace: true });
    }
  }, [paymentId, isLoading, navigate]);

  if (isLoading || isPaymentLoading) {
    return (
      <Container size="md" py="xl">
        <Paper p="xl" radius="md" withBorder pos="relative">
          <LoadingOverlay visible={true} />
          <Stack gap="lg" align="center" py="xl">
            <Text size="lg">Verifying your payment...</Text>
            <Text size="sm" c="dimmed">
              Please wait while we confirm your PayPal payment
            </Text>
          </Stack>
        </Paper>
      </Container>
    );
  }

  if (error || !paymentData) {
    return (
      <Container size="md" py="xl">
        <Stack gap="lg">
          <Alert color="orange" icon={<IconMail size={16} />}>
            <Title order={4} mb="xs">
              Payment Verification in Progress
            </Title>
            <Text size="sm">
              We're still processing your payment. You'll receive a confirmation email 
              once everything is complete. If you have concerns, please contact support.
            </Text>
          </Alert>

          <Group justify="center">
            <Button 
              component={Link} 
              to="/"
              variant="filled"
              color="blue"
            >
              Return Home
            </Button>
          </Group>
        </Stack>
      </Container>
    );
  }

  return (
    <Container size="md" py="xl">
      <Stack gap="xl">
        {/* Success Header */}
        <Paper p="xl" radius="md" bg="green.0" withBorder>
          <Stack gap="md" align="center">
            <IconCircleCheck size={64} style={{ color: '#51cf66' }} />
            <Title order={2} c="green.7" ta="center">
              Payment Successful!
            </Title>
            <Text size="lg" ta="center" c="dimmed">
              Thank you for your payment. Your registration is confirmed.
            </Text>
          </Stack>
        </Paper>

        {/* Payment Details */}
        <Paper p="lg" radius="md" withBorder>
          <Title order={3} mb="md" c="#880124">
            Payment Details
          </Title>
          
          <Stack gap="sm">
            <Group justify="space-between">
              <Text size="sm" c="dimmed">Payment ID:</Text>
              <Text size="sm" fw={500} style={{ fontFamily: 'monospace' }}>
                {paymentData.id}
              </Text>
            </Group>
            
            <Group justify="space-between">
              <Text size="sm" c="dimmed">Amount Paid:</Text>
              <Text size="lg" fw={600} c="green.7">
                {paymentData.displayAmount}
              </Text>
            </Group>
            
            {paymentData.slidingScalePercentage > 0 && (
              <>
                <Group justify="space-between">
                  <Text size="sm" c="dimmed">Original Price:</Text>
                  <Text size="sm">
                    {paymentData.originalAmount ? `$${paymentData.originalAmount.toFixed(2)}` : 'N/A'}
                  </Text>
                </Group>
                
                <Group justify="space-between">
                  <Text size="sm" c="dimmed">Sliding Scale Discount:</Text>
                  <Text size="sm" c="green.6">
                    {paymentData.slidingScalePercentage}% off
                  </Text>
                </Group>
              </>
            )}
            
            <Group justify="space-between">
              <Text size="sm" c="dimmed">Payment Method:</Text>
              <Text size="sm">
                {paymentData.paymentMethodType === 3 ? 'PayPal' : 
                 paymentData.paymentMethodType === 4 ? 'Venmo' : 'PayPal'}
              </Text>
            </Group>
            
            <Group justify="space-between">
              <Text size="sm" c="dimmed">Status:</Text>
              <Text size="sm" c="green.6" fw={500}>
                {paymentData.statusDescription}
              </Text>
            </Group>
            
            {paymentData.processedAt && (
              <Group justify="space-between">
                <Text size="sm" c="dimmed">Processed:</Text>
                <Text size="sm">
                  {new Date(paymentData.processedAt).toLocaleString()}
                </Text>
              </Group>
            )}
          </Stack>
        </Paper>

        {/* Next Steps */}
        <Paper p="lg" radius="md" withBorder>
          <Title order={3} mb="md" c="#880124">
            What's Next?
          </Title>
          
          <Stack gap="md">
            <Group gap="sm">
              <IconMail size={20} color="#9b4a75" />
              <Text size="sm">
                <strong>Confirmation Email:</strong> You'll receive a detailed confirmation 
                email with your registration information within the next few minutes.
              </Text>
            </Group>
            
            <Group gap="sm">
              <IconCalendar size={20} color="#9b4a75" />
              <Text size="sm">
                <strong>Event Details:</strong> Check your email for event location, 
                time, and any special instructions from the organizer.
              </Text>
            </Group>
            
            <Group gap="sm">
              <IconArrowRight size={20} color="#9b4a75" />
              <Text size="sm">
                <strong>Questions?</strong> Contact the event organizer or our support 
                team if you need assistance.
              </Text>
            </Group>
          </Stack>
        </Paper>

        <Divider />

        {/* Action Buttons */}
        <Group justify="center" gap="md">
          <Button 
            component={Link} 
            to="/dashboard"
            variant="filled"
            color="#880124"
            size="lg"
            leftSection={<IconCalendar size={20} />}
          >
            View My Events
          </Button>
          
          <Button 
            component={Link} 
            to="/events"
            variant="outline"
            color="blue"
            size="lg"
          >
            Browse More Events
          </Button>
        </Group>

        {/* Support Info */}
        <Alert color="blue" variant="light">
          <Text size="sm" ta="center">
            <strong>Need Help?</strong> Contact us at{' '}
            <Text component="a" href="mailto:support@witchcityrope.com" c="blue.6">
              support@witchcityrope.com
            </Text>{' '}
            or reference Payment ID: <strong>{paymentData.id}</strong>
          </Text>
        </Alert>
      </Stack>
    </Container>
  );
};
// Payment Form Component
// Handles PayPal and Venmo payment integration with sliding scale

import React, { useState } from 'react';
import {
  Box,
  Stack,
  Group,
  Text,
  Title,
  Alert,
  Paper
} from '@mantine/core';
import { IconShieldCheck } from '@tabler/icons-react';
import { PayPalButton } from './PayPalButton';
import { useSlidingScale } from '../hooks/useSlidingScale';
import type { PaymentEventInfo } from '../types/payment.types';

interface PaymentFormProps {
  /** Event information for payment */
  eventInfo: PaymentEventInfo;
  /** Initial sliding scale percentage */
  initialSlidingScale?: number;
  /** Callback when payment is successful */
  onPaymentSuccess?: (paymentId: string) => void;
  /** Callback when payment fails */
  onPaymentError?: (error: string) => void;
  /** Whether form is disabled */
  disabled?: boolean;
}

/**
 * PayPal-based payment form component
 */
export const PaymentForm: React.FC<PaymentFormProps> = ({
  eventInfo,
  initialSlidingScale = 0,
  onPaymentSuccess,
  onPaymentError,
  disabled = false
}) => {
  const [paymentError, setPaymentError] = useState<string | null>(null);

  // Sliding scale logic
  const {
    finalAmount,
    discountPercentage,
    calculation,
  } = useSlidingScale(eventInfo.basePrice, initialSlidingScale);

  /**
   * Handle successful PayPal payment
   */
  const handlePaymentSuccess = (orderId: string) => {
    console.log('PayPal payment successful:', orderId);
    setPaymentError(null);
    onPaymentSuccess?.(orderId);
  };

  /**
   * Handle PayPal payment error
   */
  const handlePaymentError = (error: string) => {
    console.error('PayPal payment error:', error);
    setPaymentError(error);
    onPaymentError?.(error);
  };

  /**
   * Handle PayPal payment cancellation
   */
  const handlePaymentCancel = () => {
    console.log('PayPal payment cancelled by user');
    setPaymentError(null);
  };

  return (
    <Stack gap="lg">
      {/* Section Title */}
      <Title order={3} c="#880124">
        Payment Method
      </Title>

      {/* Payment Amount Summary */}
      <Paper p="md" radius="sm" bg="#FAF6F2" withBorder>
        <Stack gap="xs">
          <Group justify="space-between">
            <Text size="sm" c="dimmed">Base Price:</Text>
            <Text size="sm">{calculation.display.original}</Text>
          </Group>
          {calculation.discountAmount > 0 && (
            <Group justify="space-between">
              <Text size="sm" c="dimmed">
                Sliding Scale ({calculation.display.percentage}):
              </Text>
              <Text size="sm" c="green">-{calculation.display.discount}</Text>
            </Group>
          )}
          <Group justify="space-between">
            <Text fw={600} c="#880124">Total Amount:</Text>
            <Text fw={700} size="lg" c="#880124">
              {calculation.display.final}
            </Text>
          </Group>
        </Stack>
      </Paper>

      {/* PayPal Payment Button */}
      <PayPalButton
        eventInfo={eventInfo}
        amount={finalAmount}
        slidingScalePercentage={discountPercentage}
        onPaymentSuccess={handlePaymentSuccess}
        onPaymentError={handlePaymentError}
        onPaymentCancel={handlePaymentCancel}
        disabled={disabled}
      />

      {/* Error Display */}
      {paymentError && (
        <Alert color="red" variant="light">
          <Text size="sm">
            {paymentError}
          </Text>
        </Alert>
      )}

      {/* Security Notice */}
      <Alert 
        icon={<IconShieldCheck />}
        color="blue"
        variant="light"
      >
        <Text size="sm">
          <strong>Secure Payment:</strong> Your payment is processed securely by PayPal. 
          We never store your payment information.
        </Text>
      </Alert>

      {/* Payment Methods Info */}
      <Paper p="md" radius="sm" bg="#F8F9FA" withBorder>
        <Text size="sm" c="dimmed" ta="center">
          <strong>Accepted Payment Methods:</strong><br/>
          PayPal • Venmo (mobile) • Credit & Debit Cards via PayPal<br/>
          All payments are processed securely through PayPal's platform
        </Text>
      </Paper>
    </Stack>
  );
};
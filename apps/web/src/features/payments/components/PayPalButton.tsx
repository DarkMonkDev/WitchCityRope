// PayPal Button Component
// Handles PayPal and Venmo payments with sliding scale integration

import React from 'react';
import {
  Box,
  Paper,
  Text,
  Alert,
  LoadingOverlay
} from '@mantine/core';
import { 
  PayPalScriptProvider, 
  PayPalButtons, 
  usePayPalScriptReducer 
} from "@paypal/react-paypal-js";
import { IconInfoCircle } from '@tabler/icons-react';
import type { PaymentEventInfo } from '../types/payment.types';
import { PaymentMethodType } from '../types/payment.types';

interface PayPalButtonProps {
  /** Event information for payment */
  eventInfo: PaymentEventInfo;
  /** Final amount after sliding scale */
  amount: number;
  /** Sliding scale percentage applied */
  slidingScalePercentage: number;
  /** Callback when payment is successful */
  onPaymentSuccess?: (orderId: string) => void;
  /** Callback when payment fails */
  onPaymentError?: (error: string) => void;
  /** Callback when payment is cancelled */
  onPaymentCancel?: () => void;
  /** Whether button is disabled */
  disabled?: boolean;
}

/**
 * Internal PayPal button component (already wrapped with script provider)
 */
const PayPalButtonInternal: React.FC<PayPalButtonProps> = ({
  eventInfo,
  amount,
  slidingScalePercentage,
  onPaymentSuccess,
  onPaymentError,
  onPaymentCancel,
  disabled = false
}) => {
  const [{ isPending }] = usePayPalScriptReducer();

  /**
   * Create PayPal order through our backend
   */
  const createOrder = async (): Promise<string> => {
    try {
      const response = await fetch('/api/payments/process', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        credentials: 'include', // For httpOnly cookies
        body: JSON.stringify({
          eventRegistrationId: eventInfo.registrationId,
          originalAmount: eventInfo.basePrice,
          currency: eventInfo.currency,
          slidingScalePercentage,
          paymentMethodType: PaymentMethodType.PayPal,
          returnUrl: window.location.origin + '/payment/success',
          cancelUrl: window.location.origin + '/payment/cancel',
          savePaymentMethod: false,
          metadata: {
            eventId: eventInfo.id,
            eventTitle: eventInfo.title,
            finalAmount: amount
          }
        }),
      });

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.message || 'Failed to create PayPal order');
      }

      const orderData = await response.json();
      return orderData.orderId || orderData.id;
    } catch (error) {
      console.error('Error creating PayPal order:', error);
      const errorMessage = error instanceof Error ? error.message : 'Failed to create payment';
      onPaymentError?.(errorMessage);
      throw error;
    }
  };

  /**
   * Handle successful payment approval
   */
  const onApprove = async (data: any): Promise<void> => {
    try {
      // The payment has been approved by PayPal
      // Our backend should automatically capture it
      onPaymentSuccess?.(data.orderID);
    } catch (error) {
      console.error('Error handling payment approval:', error);
      const errorMessage = error instanceof Error ? error.message : 'Payment processing failed';
      onPaymentError?.(errorMessage);
    }
  };

  /**
   * Handle payment errors
   */
  const onError = (error: any) => {
    console.error('PayPal payment error:', error);
    const errorMessage = error?.message || 'PayPal payment failed. Please try again.';
    onPaymentError?.(errorMessage);
  };

  /**
   * Handle payment cancellation
   */
  const onCancel = () => {
    console.log('PayPal payment cancelled by user');
    onPaymentCancel?.();
  };

  if (isPending) {
    return (
      <Paper p="lg" withBorder radius="md" pos="relative">
        <LoadingOverlay visible={true} />
        <Box h={60} />
      </Paper>
    );
  }

  return (
    <Paper p="lg" withBorder radius="md">
      <Box mb="md">
        <Text size="lg" fw={600} c="#880124" mb="xs">
          Complete Payment
        </Text>
        <Text size="sm" c="dimmed">
          Pay securely with PayPal or Venmo • ${amount.toFixed(2)}
        </Text>
      </Box>

      <PayPalButtons
        disabled={disabled}
        createOrder={createOrder}
        onApprove={onApprove}
        onError={onError}
        onCancel={onCancel}
        style={{
          layout: 'vertical',
          color: 'gold',
          shape: 'rect',
          label: 'paypal',
          height: 48,
          tagline: false
        }}
        // Venmo button will automatically appear on mobile devices
      />

      <Alert 
        icon={<IconInfoCircle />}
        color="blue"
        variant="light"
        mt="md"
      >
        <Text size="sm">
          • PayPal and Venmo payments are processed securely by PayPal<br/>
          • Venmo option will appear automatically on mobile devices<br/>
          • You'll be redirected to PayPal to complete your payment
        </Text>
      </Alert>
    </Paper>
  );
};

/**
 * Main PayPal button component with script provider
 */
export const PayPalButton: React.FC<PayPalButtonProps> = (props) => {
  const paypalClientId = import.meta.env.VITE_PAYPAL_CLIENT_ID;

  if (!paypalClientId) {
    return (
      <Alert color="red">
        PayPal configuration is missing. Please contact support.
      </Alert>
    );
  }

  return (
    <PayPalScriptProvider 
      options={{
        clientId: paypalClientId, // Use camelCase for the property name
        currency: props.eventInfo.currency || "USD",
        intent: "capture",
        "enable-funding": "venmo", // Enable Venmo explicitly
        "disable-funding": "" // Don't disable any funding sources
      }}
    >
      <PayPalButtonInternal {...props} />
    </PayPalScriptProvider>
  );
};
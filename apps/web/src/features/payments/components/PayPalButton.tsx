import React, { useState } from 'react';
import { PayPalButtons, PayPalScriptProvider } from '@paypal/react-paypal-js';
import { Alert, Button, Box, Text, Loader, Stack } from '@mantine/core';
import { IconAlertCircle, IconCheck } from '@tabler/icons-react';
import type { PaymentEventInfo } from '../types/payment.types';

export interface PayPalButtonProps {
  eventInfo: PaymentEventInfo;
  amount: number;
  slidingScalePercentage?: number;
  onPaymentSuccess?: (details: any) => void;
  onPaymentError?: (error: string) => void;
  onPaymentCancel?: () => void;
  disabled?: boolean;
}

export const PayPalButton: React.FC<PayPalButtonProps> = ({
  eventInfo,
  amount,
  slidingScalePercentage = 0,
  onPaymentSuccess,
  onPaymentError,
  onPaymentCancel,
  disabled = false
}) => {
  const [isProcessing, setIsProcessing] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // PayPal configuration from environment
  const paypalClientId = import.meta.env.VITE_PAYPAL_CLIENT_ID;
  const paypalMode = import.meta.env.VITE_PAYPAL_MODE || 'sandbox';

  // Calculate final amount with sliding scale discount
  const discountAmount = amount * (slidingScalePercentage / 100);
  const finalAmount = amount - discountAmount;

  console.log('🔍 PayPal Button Configuration:');
  console.log('  - paypalClientId:', paypalClientId ? `${paypalClientId.slice(0, 10)}...` : 'NOT SET');
  console.log('  - paypalMode:', paypalMode);
  console.log('  - originalAmount:', amount);
  console.log('  - slidingScalePercentage:', slidingScalePercentage);
  console.log('  - finalAmount:', finalAmount);

  // Check if PayPal is properly configured
  if (!paypalClientId) {
    return (
      <Alert
        icon={<IconAlertCircle size={16} />}
        title="PayPal Configuration Error"
        color="red"
      >
        <Text size="sm">
          PayPal is not properly configured. Please check the environment configuration.
        </Text>
      </Alert>
    );
  }

  const createOrder = async () => {
    try {
      setIsProcessing(true);
      setError(null);

      console.log('🔍 Creating PayPal order:', {
        eventId: eventInfo.id,
        eventTitle: eventInfo.title,
        amount: finalAmount,
        currency: eventInfo.currency || 'USD'
      });

      // Return the order details for PayPal
      return {
        intent: 'CAPTURE',
        purchase_units: [{
          amount: {
            currency_code: eventInfo.currency || 'USD',
            value: finalAmount.toFixed(2)
          },
          description: `Ticket for ${eventInfo.title}`,
          custom_id: eventInfo.registrationId || eventInfo.id,
          soft_descriptor: 'WitchCityRope'
        }],
        application_context: {
          brand_name: 'WitchCityRope',
          landing_page: 'NO_PREFERENCE',
          shipping_preference: 'NO_SHIPPING',
          user_action: 'PAY_NOW'
        }
      };
    } catch (error) {
      console.error('❌ PayPal order creation failed:', error);
      const errorMessage = error instanceof Error ? error.message : 'Failed to create PayPal order';
      setError(errorMessage);
      onPaymentError?.(errorMessage);
      setIsProcessing(false);
      throw error;
    }
  };

  const onApprove = async (data: any, actions: any) => {
    try {
      setIsProcessing(true);
      console.log('🔍 PayPal payment approved:', data);

      // Capture the payment
      const details = await actions.order.capture();
      console.log('✅ PayPal payment captured:', details);

      // Call success callback with payment details
      onPaymentSuccess?.(details);

    } catch (error) {
      console.error('❌ PayPal payment capture failed:', error);
      const errorMessage = error instanceof Error ? error.message : 'Failed to capture PayPal payment';
      setError(errorMessage);
      onPaymentError?.(errorMessage);
    } finally {
      setIsProcessing(false);
    }
  };

  const onError = (error: any) => {
    console.error('❌ PayPal payment error:', error);
    const errorMessage = error?.message || 'PayPal payment failed';
    setError(errorMessage);
    onPaymentError?.(errorMessage);
    setIsProcessing(false);
  };

  const onCancel = () => {
    console.log('🔍 PayPal payment cancelled');
    onPaymentCancel?.();
    setIsProcessing(false);
  };

  return (
    <PayPalScriptProvider
      options={{
        "client-id": paypalClientId,
        currency: eventInfo.currency || 'USD',
        intent: 'capture',
        "enable-funding": "paylater,venmo",
        "disable-funding": "card"
      }}
    >
      <Box>
        {/* Payment Summary */}
        <Box mb="md" p="md" style={{
          background: 'var(--color-cream)',
          borderRadius: '8px',
          border: '1px solid rgba(136, 1, 36, 0.1)'
        }}>
          <Stack gap="xs">
            <Text fw={600} size="md">Payment Summary</Text>
            <Text size="sm" c="dimmed">{eventInfo.title}</Text>

            {slidingScalePercentage > 0 && (
              <>
                <Text size="sm">
                  Original Amount: <Text span td="line-through">${amount.toFixed(2)}</Text>
                </Text>
                <Text size="sm" c="green">
                  Sliding Scale Discount ({slidingScalePercentage}%): -${discountAmount.toFixed(2)}
                </Text>
              </>
            )}

            <Text fw={700} size="lg" style={{ color: 'var(--color-burgundy)' }}>
              Total: ${finalAmount.toFixed(2)}
            </Text>
          </Stack>
        </Box>

        {/* Error Display */}
        {error && (
          <Alert
            icon={<IconAlertCircle size={16} />}
            title="Payment Error"
            color="red"
            mb="md"
          >
            <Text size="sm">{error}</Text>
            <Button
              size="xs"
              variant="outline"
              color="red"
              mt="xs"
              onClick={() => setError(null)}
            >
              Try Again
            </Button>
          </Alert>
        )}

        {/* Processing State */}
        {isProcessing && (
          <Box mb="md" p="md" style={{
            background: 'var(--color-cream)',
            borderRadius: '8px',
            textAlign: 'center'
          }}>
            <Loader size="sm" color="blue" />
            <Text size="sm" mt="xs">Processing payment...</Text>
          </Box>
        )}

        {/* PayPal Buttons */}
        <PayPalButtons
          style={{
            layout: 'vertical',
            color: 'gold',
            shape: 'rect',
            label: 'paypal'
          }}
          disabled={disabled || isProcessing}
          createOrder={createOrder}
          onApprove={onApprove}
          onError={onError}
          onCancel={onCancel}
        />

        {/* Helper Text */}
        <Text size="xs" c="dimmed" ta="center" mt="sm">
          You'll be redirected to PayPal to complete your payment securely
        </Text>
      </Box>
    </PayPalScriptProvider>
  );
};
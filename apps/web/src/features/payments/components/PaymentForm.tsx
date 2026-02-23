// Payment Form Component
// Handles PayPal and credit card payment integration with sliding scale

import React, { useState } from 'react';
import {
  Box,
  Stack,
  Group,
  Text,
  Title,
  Alert,
  Checkbox
} from '@mantine/core';
import { IconShieldCheck } from '@tabler/icons-react';
import { PayPalButton } from './PayPalButton';
import { CreditCardForm, type NonceData } from './checkout/CreditCardForm';
import { useSlidingScale } from '../hooks/useSlidingScale';
import type { PaymentEventInfo } from '../types/payment.types';

interface PaymentFormProps {
  /** Event information for payment */
  eventInfo: PaymentEventInfo;
  /** Initial sliding scale percentage */
  initialSlidingScale?: number;
  /** Callback when card nonce is ready (tokenized) - parent handles checkout */
  onNonceReady?: (data: NonceData) => void;
  /** Callback when PayPal payment succeeds (legacy flow) */
  onPaymentSuccess?: (paymentId: string) => void;
  /** Callback when payment/tokenization fails */
  onPaymentError?: (error: string) => void;
  /** Whether form is disabled */
  disabled?: boolean;
  /** Whether checkout API call is in progress (from parent) */
  isCheckoutInProgress?: boolean;
}

/**
 * Payment form component with multiple payment methods
 */
export const PaymentForm: React.FC<PaymentFormProps> = ({
  eventInfo,
  initialSlidingScale = 0,
  onNonceReady,
  onPaymentSuccess,
  onPaymentError,
  disabled = false,
  isCheckoutInProgress = false
}) => {
  const [paymentError, setPaymentError] = useState<string | null>(null);
  const [termsAccepted, setTermsAccepted] = useState(false);

  // Sliding scale logic
  const {
    finalAmount,
    discountPercentage,
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

      {/* Terms and Conditions - applies to both payment methods */}
      <Group gap="sm" align="center">
        <Checkbox
          id="terms-checkbox"
          checked={termsAccepted}
          onChange={(event) => setTermsAccepted(event.currentTarget.checked)}
          disabled={disabled || isCheckoutInProgress}
          size="md"
          color="#880124"
          styles={{
            input: {
              cursor: disabled ? 'not-allowed' : 'pointer',
              border: '2px solid #880124',
              '&:checked': {
                backgroundColor: '#880124',
                borderColor: '#880124'
              }
            }
          }}
        />
        <Text
          component="label"
          htmlFor="terms-checkbox"
          size="sm"
          onClick={(e) => {
            if ((e.target as HTMLElement).tagName !== 'A') {
              setTermsAccepted(!termsAccepted);
            }
          }}
          style={{
            color: 'var(--color-stone)',
            lineHeight: 1.4,
            cursor: disabled ? 'not-allowed' : 'pointer',
            userSelect: 'none'
          }}
        >
          I agree to the{' '}
          <a
            href="/event-waiver"
            target="_blank"
            rel="noopener noreferrer"
            style={{
              color: 'var(--color-burgundy)',
              textDecoration: 'underline'
            }}
            onClick={(e) => e.stopPropagation()}
          >
            Event Waiver
          </a>
          {' '}and{' '}
          <a
            href="/refund-policy"
            target="_blank"
            rel="noopener noreferrer"
            style={{
              color: 'var(--color-burgundy)',
              textDecoration: 'underline'
            }}
            onClick={(e) => e.stopPropagation()}
          >
            Refund Policy
          </a>
        </Text>
      </Group>

      {/* Credit Card Form - tokenize only, parent handles checkout */}
      <Box>
        <CreditCardForm
          amount={finalAmount}
          onNonceReady={(nonceData) => {
            setPaymentError(null);
            onNonceReady?.(nonceData);
          }}
          onTokenizeError={(error) => {
            setPaymentError(error);
            onPaymentError?.(error);
          }}
          disabled={disabled || !termsAccepted}
          isCheckoutInProgress={isCheckoutInProgress}
        />
      </Box>

      {/* Simple "Or" divider - Visible on all screen sizes */}
      <Text
        ta="center"
        mt={0}
        mb={0}
        style={{
          fontFamily: 'var(--font-heading)',
          fontSize: '14px',
          fontWeight: 600,
          color: 'var(--color-smoke)',
          textTransform: 'uppercase',
          letterSpacing: '0.5px'
        }}
      >
        Or
      </Text>

      {/* PayPal Payment Buttons - Always Visible */}
      <Box
        style={{
          opacity: termsAccepted ? 1 : 0.4,
          pointerEvents: termsAccepted ? 'auto' : 'none',
          transition: 'opacity 0.3s ease'
        }}
      >
        <PayPalButton
          eventInfo={eventInfo}
          amount={finalAmount}
          slidingScalePercentage={discountPercentage}
          onPaymentSuccess={handlePaymentSuccess}
          onPaymentError={handlePaymentError}
          onPaymentCancel={handlePaymentCancel}
          disabled={disabled || !termsAccepted}
        />
      </Box>

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
          <strong>Secure Payment:</strong> Your payment is processed securely through our payment processor.
          We never store your payment information.
        </Text>
      </Alert>
    </Stack>
  );
};

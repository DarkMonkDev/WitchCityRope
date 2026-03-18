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
import { PayPalButton, type PayPalCheckoutResult } from './PayPalButton';
import { CreditCardForm, type NonceData } from './checkout/CreditCardForm';
import type { PaymentEventInfo } from '../types/payment.types';

/** A single ticket selection with quantity and optional assignees (multi-ticket support) */
interface TicketSelectionItem {
  ticketTypeId: string;
  quantity: number;
  assignees?: (string | null)[];
}

interface PaymentFormProps {
  /** Event information for payment */
  eventInfo: PaymentEventInfo;
  /** Ticket type IDs selected for purchase (needed for PayPal checkout flow) */
  ticketTypeIds: string[];
  /** Idempotency key to prevent duplicate purchases */
  idempotencyKey: string;
  /** Whether the event waiver has been accepted */
  eventWaiverAccepted?: boolean;
  /** Initial sliding scale percentage */
  initialSlidingScale?: number;
  /** Total amount to charge (after sliding scale, summing all selected tickets) */
  totalAmount: number;
  /** Multi-ticket selections for PayPal (optional) */
  ticketSelections?: TicketSelectionItem[];
  /** Callback when card nonce is ready (tokenized) - parent handles checkout */
  onNonceReady?: (data: NonceData) => void;
  /** Callback when PayPal payment succeeds */
  onPaymentSuccess?: (result: PayPalCheckoutResult) => void;
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
  ticketTypeIds,
  idempotencyKey,
  eventWaiverAccepted = false,
  initialSlidingScale = 0,
  totalAmount,
  ticketSelections,
  onNonceReady,
  onPaymentSuccess,
  onPaymentError,
  disabled = false,
  isCheckoutInProgress = false
}) => {
  const [paymentError, setPaymentError] = useState<string | null>(null);
  const [termsAccepted, setTermsAccepted] = useState(false);

  /**
   * Handle successful PayPal checkout (ticket created + payment captured)
   */
  const handlePaymentSuccess = (result: PayPalCheckoutResult) => {
    console.log('PayPal checkout successful:', result);
    setPaymentError(null);
    onPaymentSuccess?.(result);
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

      {/* Terms and Conditions - applies to both payment methods.
          wrap="nowrap" keeps checkbox and label text on the same row on mobile. */}
      <Group gap="sm" align="flex-start" wrap="nowrap">
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
          size="md"
          onClick={(e) => {
            if ((e.target as HTMLElement).tagName !== 'A') {
              setTermsAccepted(!termsAccepted);
            }
          }}
          style={{
            color: '#000000',
            fontWeight: 600,
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
          ,{' '}
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
          , and{' '}
          <a
            href="/terms-of-service"
            target="_blank"
            rel="noopener noreferrer"
            style={{
              color: 'var(--color-burgundy)',
              textDecoration: 'underline'
            }}
            onClick={(e) => e.stopPropagation()}
          >
            Terms of Service
          </a>
        </Text>
      </Group>

      {/* Credit Card Form - tokenize only, parent handles checkout.
         Uses totalAmount from parent (already includes sliding scale adjustments)
         to ensure the "Pay $X.XX" button matches the actual charge amount. */}
      <Box>
        <CreditCardForm
          amount={totalAmount}
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
          eventId={eventInfo.id}
          ticketTypeIds={ticketTypeIds}
          amount={totalAmount}
          slidingScalePercentage={initialSlidingScale}
          currency={eventInfo.currency}
          eventTitle={eventInfo.title}
          eventWaiverAccepted={eventWaiverAccepted}
          idempotencyKey={idempotencyKey}
          ticketSelections={ticketSelections}
          onPaymentSuccess={handlePaymentSuccess}
          onPaymentError={handlePaymentError}
          onPaymentCancel={handlePaymentCancel}
          disabled={disabled || !termsAccepted || isCheckoutInProgress}
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

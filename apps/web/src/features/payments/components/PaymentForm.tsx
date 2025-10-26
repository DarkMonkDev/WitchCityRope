// Payment Form Component
// Handles PayPal and credit card payment integration with sliding scale

import React, { useState, useEffect } from 'react';
import {
  Box,
  Stack,
  Group,
  Text,
  Title,
  Alert,
  Button,
  Checkbox
} from '@mantine/core';
import { IconShieldCheck } from '@tabler/icons-react';
import { PayPalButton } from './PayPalButton';
import { CreditCardForm } from './checkout/CreditCardForm';
import { useSlidingScale } from '../hooks/useSlidingScale';
import { isNonProduction, getPayPalTestCard } from '../../../lib/utils/environment';
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
 * Payment form component with multiple payment methods
 */
export const PaymentForm: React.FC<PaymentFormProps> = ({
  eventInfo,
  initialSlidingScale = 0,
  onPaymentSuccess,
  onPaymentError,
  disabled = false
}) => {
  const [paymentError, setPaymentError] = useState<string | null>(null);
  const [cardData, setCardData] = useState({
    cardNumber: '',
    cardholderName: '',
    expiryDate: '',
    cvv: '',
    billingZip: ''
  });
  const [termsAccepted, setTermsAccepted] = useState(false);
  const [isProcessing, setIsProcessing] = useState(false);

  // Sliding scale logic
  const {
    finalAmount,
    discountPercentage,
    calculation,
  } = useSlidingScale(eventInfo.basePrice, initialSlidingScale);

  // Auto-fill credit card in dev/staging environment
  useEffect(() => {
    if (isNonProduction()) {
      const testCard = getPayPalTestCard();
      setCardData({
        cardNumber: testCard.cardNumber.match(/.{1,4}/g)?.join(' ') || testCard.cardNumber,
        cardholderName: testCard.cardholderName,
        expiryDate: testCard.expiryDate,
        cvv: testCard.cvv,
        billingZip: testCard.billingZip
      });
      // Don't auto-accept terms - user must manually check
    }
  }, []); // Empty deps - run once on mount

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

  /**
   * Handle credit card payment submission
   */
  const handleCreditCardSubmit = async (data: typeof cardData) => {
    if (!termsAccepted) {
      setPaymentError('Please accept the terms and conditions to continue.');
      return;
    }

    setIsProcessing(true);
    setPaymentError(null);

    try {
      // Simulate credit card processing
      await new Promise(resolve => setTimeout(resolve, 2000));

      // Simulate successful payment
      const paymentId = `cc_${Date.now()}`;
      onPaymentSuccess?.(paymentId);
    } catch (error) {
      setPaymentError('Payment processing failed. Please try again.');
      onPaymentError?.('Payment processing failed');
    } finally {
      setIsProcessing(false);
    }
  };

  return (
    <Stack gap="lg">
      {/* Section Title */}
      <Title order={3} c="#880124">
        Payment Method
      </Title>

      {/* Credit Card Form */}
      <Box>
        <CreditCardForm
          cardData={cardData}
          onCardDataChange={setCardData}
          isProcessing={isProcessing}
          onSubmit={handleCreditCardSubmit}
        />

        {/* Terms and Button Row */}
        <Box mt="lg" mb="sm">
          <Group justify="space-between" align="center" wrap="nowrap" gap="md">
            <Group gap="sm" align="center" style={{ flex: 1 }}>
              <Checkbox
                id="terms-checkbox"
                checked={termsAccepted}
                onChange={(event) => setTermsAccepted(event.currentTarget.checked)}
                disabled={isProcessing}
                size="md"
                color="#880124"
                styles={{
                  input: {
                    cursor: isProcessing ? 'not-allowed' : 'pointer',
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
                style={{
                  color: 'var(--color-stone)',
                  lineHeight: 1.4,
                  cursor: isProcessing ? 'not-allowed' : 'pointer',
                  userSelect: 'none'
                }}
              >
                I agree to the{' '}
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

            <Box style={{ flexShrink: 0 }}>
              <Button
                onClick={() => handleCreditCardSubmit(cardData)}
                loading={isProcessing}
                disabled={!termsAccepted}
                size="lg"
                styles={{
                  root: {
                    background: termsAccepted
                      ? 'linear-gradient(135deg, #FFB800, #DAA520)'
                      : 'linear-gradient(135deg, #CCC, #AAA)',
                    color: termsAccepted ? '#2C2C2C' : '#666',
                    fontWeight: 600,
                    height: '44px',
                    fontSize: '14px',
                    lineHeight: '1.2',
                    whiteSpace: 'nowrap',
                    opacity: termsAccepted ? 1 : 0.4,
                    transition: 'all 0.3s ease',
                    cursor: termsAccepted ? 'pointer' : 'not-allowed',
                    '&:hover': {
                      boxShadow: termsAccepted ? '0 4px 12px rgba(255, 191, 0, 0.3)' : 'none'
                    }
                  }
                }}
              >
                {isProcessing ? 'Processing...' : 'Pay with Credit Card'}
              </Button>
            </Box>
          </Group>
        </Box>
      </Box>

      {/* Divider with "OR" text */}
      <Box style={{ position: 'relative', textAlign: 'center', margin: '20px 0' }}>
        <Box style={{
          position: 'absolute',
          top: '50%',
          left: 0,
          right: 0,
          height: '1px',
          background: 'var(--color-taupe)',
          zIndex: 0
        }} />
        <Text
          size="sm"
          fw={600}
          style={{
            display: 'inline-block',
            background: 'white',
            padding: '0 16px',
            position: 'relative',
            zIndex: 1,
            color: 'var(--color-stone)'
          }}
        >
          OR PAY WITH
        </Text>
      </Box>

      {/* Second Terms Checkbox for PayPal/Venmo */}
      <Box mb="md">
        <Group gap="sm" align="center">
          <Checkbox
            id="terms-checkbox-paypal"
            checked={termsAccepted}
            onChange={(event) => setTermsAccepted(event.currentTarget.checked)}
            disabled={isProcessing}
            size="md"
            color="#880124"
            styles={{
              input: {
                cursor: isProcessing ? 'not-allowed' : 'pointer',
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
            htmlFor="terms-checkbox-paypal"
            size="sm"
            style={{
              color: 'var(--color-stone)',
              lineHeight: 1.4,
              cursor: isProcessing ? 'not-allowed' : 'pointer',
              userSelect: 'none'
            }}
          >
            I agree to the{' '}
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
      </Box>

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

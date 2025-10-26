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
  Paper,
  Button
} from '@mantine/core';
import { IconShieldCheck } from '@tabler/icons-react';
import { PayPalButton } from './PayPalButton';
import { PaymentMethodSelector } from './checkout/PaymentMethodSelector';
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

// CSS animation for slide-down effect
const paymentFormStyles = `
  @keyframes slideDown {
    from {
      opacity: 0;
      max-height: 0;
      transform: translateY(-10px);
    }
    to {
      opacity: 1;
      max-height: 2000px;
      transform: translateY(0);
    }
  }
`;

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
  const [paymentMethod, setPaymentMethod] = useState<'card' | 'paypal'>('paypal');
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
    if (paymentMethod === 'card' && isNonProduction()) {
      const testCard = getPayPalTestCard();
      setCardData({
        cardNumber: testCard.cardNumber.match(/.{1,4}/g)?.join(' ') || testCard.cardNumber,
        cardholderName: testCard.cardholderName,
        expiryDate: testCard.expiryDate,
        cvv: testCard.cvv,
        billingZip: testCard.billingZip
      });
      // Auto-accept terms in dev
      setTermsAccepted(true);
    }
  }, [paymentMethod]);

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
    <>
      <style>{paymentFormStyles}</style>
      <Stack gap="lg">
        {/* Section Title */}
        <Title order={3} c="#880124">
          Payment Method
        </Title>

        {/* Payment Method Selector */}
        <PaymentMethodSelector
          selectedMethod={paymentMethod}
          onMethodChange={setPaymentMethod}
        />

        {/* Credit Card Form */}
        {paymentMethod === 'card' && (
          <Box
            style={{
              overflow: 'hidden',
              animation: 'slideDown 0.3s ease-out'
            }}
          >
            <CreditCardForm
              cardData={cardData}
              onCardDataChange={setCardData}
              isProcessing={isProcessing}
              onSubmit={handleCreditCardSubmit}
              termsAccepted={termsAccepted}
              onTermsChange={setTermsAccepted}
            />

            <Group justify="flex-end" mt="md">
              <Button
                onClick={() => handleCreditCardSubmit(cardData)}
                loading={isProcessing}
                disabled={!termsAccepted}
                size="lg"
                styles={{
                  root: {
                    background: 'linear-gradient(135deg, #FFB800, #DAA520)',
                    color: '#2C2C2C',
                    fontWeight: 600,
                    height: '44px',
                    paddingTop: '12px',
                    paddingBottom: '12px',
                    fontSize: '14px',
                    lineHeight: '1.2',
                    '&:hover': {
                      boxShadow: '0 4px 12px rgba(255, 191, 0, 0.3)'
                    }
                  }
                }}
              >
                {isProcessing ? 'Processing Payment...' : 'Complete Purchase'}
              </Button>
            </Group>
          </Box>
        )}

        {/* PayPal Payment Button */}
        {paymentMethod === 'paypal' && (
          <Box
            style={{
              overflow: 'hidden',
              animation: 'slideDown 0.3s ease-out'
            }}
          >
            <PayPalButton
              eventInfo={eventInfo}
              amount={finalAmount}
              slidingScalePercentage={discountPercentage}
              onPaymentSuccess={handlePaymentSuccess}
              onPaymentError={handlePaymentError}
              onPaymentCancel={handlePaymentCancel}
              disabled={disabled}
            />
          </Box>
        )}

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
            <strong>Secure Payment:</strong> Your payment is processed securely{' '}
            {paymentMethod === 'card' ? 'through our payment processor' : 'by PayPal'}.
            We never store your payment information.
          </Text>
        </Alert>
      </Stack>
    </>
  );
};

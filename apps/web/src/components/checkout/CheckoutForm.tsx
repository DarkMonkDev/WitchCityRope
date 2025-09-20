import React, { useState } from 'react';
import {
  Box,
  Text,
  Group,
  Stack,
  Paper,
  Button,
  Alert,
  Divider
} from '@mantine/core';
import { IconAlertCircle } from '@tabler/icons-react';
import { PaymentMethodSelector } from './PaymentMethodSelector';
import { CreditCardForm } from './CreditCardForm';
import { PayPalButton } from '../../features/payments/components/PayPalButton';
import type { PaymentEventInfo } from '../../features/payments/types/payment.types';

interface CheckoutFormProps {
  eventInfo: {
    id: string;
    title: string;
    startDateTime: string;
    endDateTime: string;
    price: number;
  };
  quantity: number;
  total: number;
  paymentMethod: 'card' | 'paypal' | 'venmo';
  onPaymentMethodChange: (method: 'card' | 'paypal' | 'venmo') => void;
  onPaymentSuccess: (paymentDetails: any) => void;
  onBack: () => void;
}

interface CreditCardData {
  cardNumber: string;
  cardholderName: string;
  expiryDate: string;
  cvv: string;
  billingZip: string;
}

export const CheckoutForm: React.FC<CheckoutFormProps> = ({
  eventInfo,
  quantity,
  total,
  paymentMethod,
  onPaymentMethodChange,
  onPaymentSuccess,
  onBack
}) => {
  const [isProcessing, setIsProcessing] = useState(false);
  const [paymentError, setPaymentError] = useState<string | null>(null);
  const [cardData, setCardData] = useState<CreditCardData>({
    cardNumber: '',
    cardholderName: '',
    expiryDate: '',
    cvv: '',
    billingZip: ''
  });
  const [termsAccepted, setTermsAccepted] = useState(false);

  const handleCreditCardSubmit = async (cardData: CreditCardData) => {
    if (!termsAccepted) {
      setPaymentError('Please accept the terms and conditions to continue.');
      return;
    }

    setIsProcessing(true);
    setPaymentError(null);

    try {
      // Simulate credit card processing
      // In a real implementation, this would call a payment processor like Stripe
      await new Promise(resolve => setTimeout(resolve, 2000));

      // Simulate successful payment
      const paymentDetails = {
        id: `txn_${Date.now()}`,
        method: 'credit_card',
        amount: total,
        currency: 'USD',
        cardLast4: cardData.cardNumber.slice(-4),
        status: 'completed',
        transactionId: `TXN-${Date.now()}`,
        confirmationNumber: `WCR-${new Date().getFullYear()}-${String(Date.now()).slice(-6)}`
      };

      onPaymentSuccess(paymentDetails);
    } catch (error) {
      setPaymentError('Payment processing failed. Please try again.');
    } finally {
      setIsProcessing(false);
    }
  };

  const handlePayPalSuccess = async (paymentDetails: any) => {
    const formattedDetails = {
      ...paymentDetails,
      method: 'paypal',
      confirmationNumber: `WCR-${new Date().getFullYear()}-${String(Date.now()).slice(-6)}`
    };
    onPaymentSuccess(formattedDetails);
  };

  const handlePayPalError = (error: string) => {
    setPaymentError(`PayPal payment failed: ${error}`);
  };

  const handleVenmoClick = () => {
    // For now, treat Venmo the same as PayPal since they're owned by the same company
    // In a real implementation, this would use Venmo's specific integration
    setPaymentError('Venmo payment is coming soon! Please use PayPal or credit card for now.');
  };

  // Create PayPal event info
  const paypalEventInfo: PaymentEventInfo = {
    id: eventInfo.id,
    title: eventInfo.title,
    startDateTime: eventInfo.startDateTime,
    endDateTime: eventInfo.endDateTime,
    currency: 'USD',
    basePrice: total,
    registrationId: eventInfo.id
  };

  return (
    <Stack gap="lg">
      {/* Payment Method Selection */}
      <Box>
        <Text
          style={{
            fontFamily: 'var(--font-heading)',
            fontSize: '20px',
            fontWeight: 700,
            color: 'var(--color-burgundy)',
            marginBottom: 'var(--space-md)',
            paddingBottom: 'var(--space-sm)',
            borderBottom: '2px solid var(--color-taupe)'
          }}
        >
          Payment Method
        </Text>

        <PaymentMethodSelector
          selectedMethod={paymentMethod}
          onMethodChange={onPaymentMethodChange}
        />
      </Box>

      {/* Payment Error Alert */}
      {paymentError && (
        <Alert
          icon={<IconAlertCircle size={16} />}
          title="Payment Error"
          color="red"
          variant="light"
        >
          {paymentError}
        </Alert>
      )}

      {/* Payment Form */}
      <Box>
        {paymentMethod === 'card' && (
          <CreditCardForm
            cardData={cardData}
            onCardDataChange={setCardData}
            isProcessing={isProcessing}
            onSubmit={handleCreditCardSubmit}
          />
        )}

        {paymentMethod === 'paypal' && (
          <Paper
            style={{
              background: 'var(--color-cream)',
              padding: 'var(--space-lg)',
              borderRadius: '12px',
              textAlign: 'center'
            }}
          >
            <Text
              size="lg"
              style={{
                color: 'var(--color-stone)',
                marginBottom: 'var(--space-md)'
              }}
            >
              You will be redirected to PayPal to complete your payment securely.
            </Text>

            <PayPalButton
              eventInfo={paypalEventInfo}
              amount={total}
              slidingScalePercentage={0}
              onPaymentSuccess={handlePayPalSuccess}
              onPaymentError={handlePayPalError}
              onPaymentCancel={() => {/* PayPal cancelled */}}
              disabled={isProcessing}
            />
          </Paper>
        )}

        {paymentMethod === 'venmo' && (
          <Paper
            style={{
              background: 'var(--color-cream)',
              padding: 'var(--space-lg)',
              borderRadius: '12px',
              textAlign: 'center'
            }}
          >
            <Text
              size="lg"
              style={{
                color: 'var(--color-stone)',
                marginBottom: 'var(--space-md)'
              }}
            >
              Venmo payment integration coming soon!
            </Text>

            <Button
              onClick={handleVenmoClick}
              style={{
                backgroundColor: '#3D95CE',
                color: 'white',
                width: '100%'
              }}
            >
              Pay with Venmo (Coming Soon)
            </Button>
          </Paper>
        )}
      </Box>

      {/* Total and Terms */}
      {paymentMethod === 'card' && (
        <Paper
          style={{
            background: 'var(--color-ivory)',
            borderRadius: '12px',
            padding: 'var(--space-lg)',
            textAlign: 'center',
            border: '2px solid var(--color-taupe)'
          }}
        >
          <Group justify="space-between" mb="md">
            <Text style={{ fontSize: '18px', color: 'var(--color-stone)' }}>
              Total Amount
            </Text>
            <Text
              style={{
                fontFamily: 'var(--font-display)',
                fontSize: '36px',
                fontWeight: 700,
                color: 'var(--color-burgundy)'
              }}
            >
              ${total.toFixed(2)}
            </Text>
          </Group>

          <Divider mb="md" color="var(--color-taupe)" />

          <Group gap="xs" mb="md" style={{ justifyContent: 'center' }}>
            <input
              type="checkbox"
              id="terms"
              checked={termsAccepted}
              onChange={(e) => setTermsAccepted(e.target.checked)}
              style={{
                width: '16px',
                height: '16px'
              }}
            />
            <Text size="sm" style={{ color: 'var(--color-stone)', lineHeight: 1.6 }}>
              I agree to the{' '}
              <a
                href="#"
                style={{
                  color: 'var(--color-burgundy)',
                  textDecoration: 'underline'
                }}
              >
                Terms of Service
              </a>
              {' '}and{' '}
              <a
                href="#"
                style={{
                  color: 'var(--color-burgundy)',
                  textDecoration: 'underline'
                }}
              >
                Refund Policy
              </a>
            </Text>
          </Group>
        </Paper>
      )}

      {/* Navigation Buttons */}
      <Group justify="space-between">
        <Button
          variant="outline"
          onClick={onBack}
          style={{
            borderColor: 'var(--color-burgundy)',
            color: 'var(--color-burgundy)'
          }}
        >
          Back to Review
        </Button>

        {paymentMethod === 'card' && (
          <Button
            onClick={() => handleCreditCardSubmit(cardData)}
            loading={isProcessing}
            disabled={!termsAccepted}
            className="btn btn-primary"
          >
            {isProcessing ? 'Processing Payment...' : 'Complete Purchase'}
          </Button>
        )}
      </Group>
    </Stack>
  );
};
// Credit Card Form Component using Accept.js
// Tokenize-only: card data goes directly to Authorize.net, returns nonce to parent.
// Parent handles the checkout API call.

import React, { useState, useCallback, useEffect } from 'react';
import { Stack, TextInput, Group, Box, Text, Button, Alert, Loader } from '@mantine/core';
import { IconAlertCircle, IconLock } from '@tabler/icons-react';
import { useMediaQuery } from '@mantine/hooks';
import { isNonProduction, getPayPalTestCard } from '../../../../lib/utils/environment';

export interface NonceData {
  nonce: string;
  dataDescriptor: string;
  lastFourDigits: string;
  cardType: string;
}

interface CreditCardFormProps {
  amount: number;
  onNonceReady: (data: NonceData) => void;
  onTokenizeError?: (error: string) => void;
  disabled?: boolean;
  /** Parent controls this when checkout API call is in progress */
  isCheckoutInProgress?: boolean;
}

interface CardData {
  cardNumber: string;
  cardholderName: string;
  expiryDate: string;
  cvv: string;
  billingZip: string;
}

// Accept.js response types
interface AcceptJsResponse {
  messages: {
    resultCode: 'Ok' | 'Error';
    message: Array<{ code: string; text: string }>;
  };
  opaqueData?: {
    dataDescriptor: string;
    dataValue: string;
  };
}

declare global {
  interface Window {
    Accept?: {
      dispatchData: (
        secureData: {
          authData: { clientKey: string; apiLoginID: string };
          cardData: {
            cardNumber: string;
            month: string;
            year: string;
            cardCode: string;
            zip?: string;
            fullName?: string;
          };
        },
        responseHandler: (response: AcceptJsResponse) => void
      ) => void;
    };
  }
}

export const CreditCardForm: React.FC<CreditCardFormProps> = ({
  amount,
  onNonceReady,
  onTokenizeError,
  disabled = false,
  isCheckoutInProgress = false
}) => {
  const isMobile = useMediaQuery('(max-width: 991px)');
  const [isTokenizing, setIsTokenizing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [cardData, setCardData] = useState<CardData>({
    cardNumber: '',
    cardholderName: '',
    expiryDate: '',
    cvv: '',
    billingZip: ''
  });

  // Auto-fill test card data in dev/staging environments
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
    }
  }, []);

  const loginId = import.meta.env.VITE_AUTHORIZENET_LOGIN_ID;
  const clientKey = import.meta.env.VITE_AUTHORIZENET_CLIENT_KEY;

  // Accept.js is loaded via script tag in index.html
  const isAcceptJsLoaded = typeof window !== 'undefined' && window.Accept;

  const isProcessing = isTokenizing || isCheckoutInProgress;

  const formatCardNumber = (value: string) => {
    const digitsOnly = value.replace(/\D/g, '');
    const formatted = digitsOnly.match(/.{1,4}/g)?.join(' ') || digitsOnly;
    return formatted.substring(0, 19);
  };

  const formatExpiryDate = (value: string) => {
    const digitsOnly = value.replace(/\D/g, '');
    if (digitsOnly.length >= 2) {
      return digitsOnly.slice(0, 2) + '/' + digitsOnly.slice(2, 4);
    }
    return digitsOnly;
  };

  const getCardType = (cardNumber: string) => {
    const digitsOnly = cardNumber.replace(/\D/g, '');
    if (digitsOnly.startsWith('4')) return 'Visa';
    if (digitsOnly.startsWith('5') || digitsOnly.startsWith('2')) return 'Mastercard';
    if (digitsOnly.startsWith('3')) return 'Amex';
    if (digitsOnly.startsWith('6')) return 'Discover';
    return '';
  };

  const getLastFour = (cardNumber: string) => {
    const digitsOnly = cardNumber.replace(/\D/g, '');
    return digitsOnly.slice(-4);
  };

  const isFormValid = () => {
    const digits = cardData.cardNumber.replace(/\D/g, '');
    const [month, year] = cardData.expiryDate.split('/');
    return (
      digits.length >= 13 &&
      digits.length <= 19 &&
      cardData.cardholderName.trim().length >= 2 &&
      month && year && parseInt(month) >= 1 && parseInt(month) <= 12 &&
      cardData.cvv.length >= 3 &&
      cardData.billingZip.length >= 5
    );
  };

  const handleSubmit = useCallback(async () => {
    if (!isAcceptJsLoaded) {
      setError('Payment system is loading. Please wait a moment and try again.');
      return;
    }

    if (!loginId || !clientKey) {
      setError('Credit card processing is not configured.');
      return;
    }

    setIsTokenizing(true);
    setError(null);

    const digits = cardData.cardNumber.replace(/\D/g, '');
    const [month, year] = cardData.expiryDate.split('/');
    const fullYear = year.length === 2 ? `20${year}` : year;

    // Send card data directly to Authorize.net via Accept.js for tokenization
    window.Accept!.dispatchData(
      {
        authData: {
          clientKey: clientKey,
          apiLoginID: loginId
        },
        cardData: {
          cardNumber: digits,
          month: month,
          year: fullYear,
          cardCode: cardData.cvv,
          zip: cardData.billingZip,
          fullName: cardData.cardholderName
        }
      },
      (acceptResponse: AcceptJsResponse) => {
        if (acceptResponse.messages.resultCode === 'Error') {
          const errorMsg = acceptResponse.messages.message
            .map(m => m.text)
            .join('; ');
          setError(errorMsg);
          onTokenizeError?.(errorMsg);
          setIsTokenizing(false);
          return;
        }

        if (!acceptResponse.opaqueData) {
          setError('Failed to tokenize card data.');
          setIsTokenizing(false);
          return;
        }

        // Pass the nonce to the parent - parent handles the checkout API call
        setIsTokenizing(false);
        onNonceReady({
          nonce: acceptResponse.opaqueData.dataValue,
          dataDescriptor: acceptResponse.opaqueData.dataDescriptor,
          lastFourDigits: getLastFour(cardData.cardNumber),
          cardType: getCardType(cardData.cardNumber)
        });
      }
    );
  }, [cardData, loginId, clientKey, isAcceptJsLoaded, onNonceReady, onTokenizeError]);

  if (!loginId || !clientKey) {
    return (
      <Alert icon={<IconAlertCircle size={16} />} title="Configuration Error" color="red">
        <Text size="sm">Credit card processing is not configured.</Text>
      </Alert>
    );
  }

  const labelStyles = {
    display: 'block',
    fontFamily: 'var(--font-heading)',
    fontSize: '14px',
    fontWeight: 600,
    color: 'var(--color-smoke)',
    marginBottom: '4px',
    textTransform: 'uppercase' as const,
    letterSpacing: '0.5px'
  };

  const inputStyles = {
    padding: 'var(--space-sm)',
    border: '2px solid var(--color-taupe)',
    borderRadius: '8px',
    fontSize: '16px',
    fontFamily: 'var(--font-body)',
    backgroundColor: 'var(--color-cream)',
    transition: 'all 0.3s ease',
    '&:focus': {
      borderColor: 'var(--color-burgundy)',
      backgroundColor: 'var(--color-ivory)',
      boxShadow: '0 0 0 3px rgba(136, 1, 36, 0.1)'
    }
  };

  const cardType = getCardType(cardData.cardNumber);

  const buttonLabel = isCheckoutInProgress
    ? 'Processing Payment...'
    : isTokenizing
      ? 'Securing Card...'
      : `Pay $${amount.toFixed(2)}`;

  return (
    <Stack gap="md">
      {error && (
        <Alert icon={<IconAlertCircle size={16} />} title="Card Error" color="red">
          <Text size="sm">{error}</Text>
        </Alert>
      )}

      {/* Card Number + Cardholder Name */}
      <Box hiddenFrom="md">
        <Box style={{ position: 'relative' }} mb="md">
          <Text component="label" style={labelStyles}>Card Number</Text>
          <TextInput
            value={cardData.cardNumber}
            onChange={(e) => setCardData(d => ({ ...d, cardNumber: formatCardNumber(e.target.value) }))}
            placeholder="1234 5678 9012 3456"
            disabled={isProcessing || disabled}
            styles={{ input: { ...inputStyles, paddingRight: '70px' } }}
          />
          {cardType && (
            <Box style={{ position: 'absolute', right: '12px', top: '55%', transform: 'translateY(-50%)', marginTop: '10px' }}>
              <Text size="xs" fw={700} c="dimmed">{cardType.toUpperCase()}</Text>
            </Box>
          )}
        </Box>
        <Box>
          <Text component="label" style={labelStyles}>Cardholder Name</Text>
          <TextInput
            value={cardData.cardholderName}
            onChange={(e) => setCardData(d => ({ ...d, cardholderName: e.target.value }))}
            placeholder="Name on card"
            disabled={isProcessing || disabled}
            styles={{ input: inputStyles }}
          />
        </Box>
      </Box>

      <Group grow align="flex-start" visibleFrom="md">
        <Box style={{ position: 'relative' }}>
          <Text component="label" style={labelStyles}>Card Number</Text>
          <TextInput
            value={cardData.cardNumber}
            onChange={(e) => setCardData(d => ({ ...d, cardNumber: formatCardNumber(e.target.value) }))}
            placeholder="1234 5678 9012 3456"
            disabled={isProcessing || disabled}
            styles={{ input: { ...inputStyles, paddingRight: '70px' } }}
          />
          {cardType && (
            <Box style={{ position: 'absolute', right: '12px', top: '55%', transform: 'translateY(-50%)', marginTop: '10px' }}>
              <Text size="xs" fw={700} c="dimmed">{cardType.toUpperCase()}</Text>
            </Box>
          )}
        </Box>
        <Box>
          <Text component="label" style={labelStyles}>Cardholder Name</Text>
          <TextInput
            value={cardData.cardholderName}
            onChange={(e) => setCardData(d => ({ ...d, cardholderName: e.target.value }))}
            placeholder="Name on card"
            disabled={isProcessing || disabled}
            styles={{ input: inputStyles }}
          />
        </Box>
      </Group>

      {/* Expiry, CVV, ZIP */}
      <Group grow>
        <Box>
          <Text component="label" style={labelStyles}>{isMobile ? 'Exp Date' : 'Expiry Date'}</Text>
          <TextInput
            value={cardData.expiryDate}
            onChange={(e) => setCardData(d => ({ ...d, expiryDate: formatExpiryDate(e.target.value) }))}
            placeholder="MM/YY"
            disabled={isProcessing || disabled}
            maxLength={5}
            styles={{ input: inputStyles }}
          />
        </Box>
        <Box>
          <Text component="label" style={labelStyles}>CVV</Text>
          <TextInput
            type="password"
            value={cardData.cvv}
            onChange={(e) => {
              const val = e.target.value.replace(/\D/g, '').substring(0, 4);
              setCardData(d => ({ ...d, cvv: val }));
            }}
            placeholder="123"
            disabled={isProcessing || disabled}
            maxLength={4}
            styles={{ input: inputStyles }}
          />
        </Box>
        <Box>
          <Text component="label" style={labelStyles}>{isMobile ? 'Zip' : 'Billing ZIP'}</Text>
          <TextInput
            value={cardData.billingZip}
            onChange={(e) => {
              const val = e.target.value.replace(/\D/g, '').substring(0, 5);
              setCardData(d => ({ ...d, billingZip: val }));
            }}
            placeholder="12345"
            disabled={isProcessing || disabled}
            maxLength={5}
            styles={{ input: inputStyles }}
          />
        </Box>
      </Group>

      {/* Submit Button */}
      <Button
        fullWidth
        size="lg"
        color="dark"
        onClick={handleSubmit}
        disabled={!isFormValid() || isProcessing || disabled}
        leftSection={isProcessing ? <Loader size="xs" color="white" /> : <IconLock size={18} />}
      >
        {buttonLabel}
      </Button>

      <Text size="xs" c="dimmed" ta="center">
        Your card information is sent directly to Authorize.net and never touches our servers.
      </Text>
    </Stack>
  );
};

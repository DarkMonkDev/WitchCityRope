import React from 'react';
import { Box, Text, TextInput, Group, Stack, Button } from '@mantine/core';
import { IconTestPipe } from '@tabler/icons-react';
import { isNonProduction, getPayPalTestCard } from '../../lib/utils/environment';

interface CreditCardData {
  cardNumber: string;
  cardholderName: string;
  expiryDate: string;
  cvv: string;
  billingZip: string;
}

interface CreditCardFormProps {
  cardData: CreditCardData;
  onCardDataChange: (data: CreditCardData) => void;
  isProcessing: boolean;
  onSubmit: (data: CreditCardData) => void;
  termsAccepted: boolean;
  onTermsChange: (accepted: boolean) => void;
}

export const CreditCardForm: React.FC<CreditCardFormProps> = ({
  cardData,
  onCardDataChange,
  isProcessing,
  termsAccepted,
  onTermsChange
}) => {
  const formatCardNumber = (value: string) => {
    // Remove all non-digits
    const digitsOnly = value.replace(/\D/g, '');
    // Add spaces every 4 digits
    const formatted = digitsOnly.match(/.{1,4}/g)?.join(' ') || digitsOnly;
    return formatted.substring(0, 19); // Max length for formatted card number
  };

  const formatExpiryDate = (value: string) => {
    // Remove all non-digits
    const digitsOnly = value.replace(/\D/g, '');
    // Add slash after MM
    if (digitsOnly.length >= 2) {
      return digitsOnly.slice(0, 2) + '/' + digitsOnly.slice(2, 4);
    }
    return digitsOnly;
  };

  const handleCardNumberChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const formatted = formatCardNumber(event.target.value);
    onCardDataChange({
      ...cardData,
      cardNumber: formatted
    });
  };

  const handleExpiryChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const formatted = formatExpiryDate(event.target.value);
    onCardDataChange({
      ...cardData,
      expiryDate: formatted
    });
  };

  const handleCvvChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const value = event.target.value.replace(/\D/g, '').substring(0, 4);
    onCardDataChange({
      ...cardData,
      cvv: value
    });
  };

  const handleZipChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const value = event.target.value.replace(/\D/g, '').substring(0, 5);
    onCardDataChange({
      ...cardData,
      billingZip: value
    });
  };

  const getCardType = (cardNumber: string) => {
    const digitsOnly = cardNumber.replace(/\D/g, '');
    if (digitsOnly.startsWith('4')) return 'visa';
    if (digitsOnly.startsWith('5') || digitsOnly.startsWith('2')) return 'mastercard';
    if (digitsOnly.startsWith('3')) return 'amex';
    if (digitsOnly.startsWith('6')) return 'discover';
    return 'unknown';
  };

  const cardType = getCardType(cardData.cardNumber);

  const handleFillTestCard = () => {
    const testCard = getPayPalTestCard();

    // Format the test card number with spaces
    const formattedCardNumber = formatCardNumber(testCard.cardNumber);

    onCardDataChange({
      cardNumber: formattedCardNumber,
      cardholderName: testCard.cardholderName,
      expiryDate: testCard.expiryDate,
      cvv: testCard.cvv,
      billingZip: testCard.billingZip
    });
  };

  return (
    <>
      {/* Test Card Auto-Fill Button - Dev/Staging Only */}
      {isNonProduction() && (
        <Button
          onClick={handleFillTestCard}
          variant="light"
          color="orange"
          size="sm"
          leftSection={<IconTestPipe size={16} />}
          mb="md"
          fullWidth
          styles={{
            root: {
              backgroundColor: 'rgba(253, 126, 20, 0.1)',
              color: '#FD7E14',
              border: '1px dashed #FD7E14',
              '&:hover': {
                backgroundColor: 'rgba(253, 126, 20, 0.2)',
              }
            }
          }}
        >
          Fill Test Card (Dev/Staging Only)
        </Button>
      )}

      <Stack gap="md">
        {/* Row 1: Card Number and Cardholder Name */}
        <Group grow align="flex-start">
          {/* Card Number */}
          <Box style={{ position: 'relative' }}>
            <Text
              component="label"
              style={{
                display: 'block',
                fontFamily: 'var(--font-heading)',
                fontSize: '14px',
                fontWeight: 600,
                color: 'var(--color-smoke)',
                marginBottom: '4px',
                textTransform: 'uppercase',
                letterSpacing: '0.5px'
              }}
            >
              Card Number
            </Text>
            <TextInput
              value={cardData.cardNumber}
              onChange={handleCardNumberChange}
              placeholder="1234 5678 9012 3456"
              disabled={isProcessing}
              styles={{
                input: {
                  padding: 'var(--space-sm)',
                  border: '2px solid var(--color-taupe)',
                  borderRadius: '8px',
                  fontSize: '16px',
                  fontFamily: 'var(--font-body)',
                  backgroundColor: 'var(--color-cream)',
                  transition: 'all 0.3s ease',
                  paddingRight: '80px', // Space for card icons
                  '&:focus': {
                    borderColor: 'var(--color-burgundy)',
                    backgroundColor: 'var(--color-ivory)',
                    boxShadow: '0 0 0 3px rgba(136, 1, 36, 0.1)'
                  }
                }
              }}
            />

            {/* Card Type Icons */}
            <Box
              style={{
                position: 'absolute',
                right: '12px',
                top: '50%',
                transform: 'translateY(-50%)',
                display: 'flex',
                gap: '4px',
                marginTop: '12px' // Adjust for label
              }}
            >
              <Box
                style={{
                  width: '32px',
                  height: '20px',
                  backgroundColor: cardType === 'visa' ? '#1A1F71' : 'var(--color-gray-light)',
                  borderRadius: '4px',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  fontSize: '10px',
                  color: 'white',
                  fontWeight: 'bold'
                }}
              >
                VISA
              </Box>
              <Box
                style={{
                  width: '32px',
                  height: '20px',
                  backgroundColor: cardType === 'mastercard' ? '#EB001B' : 'var(--color-gray-light)',
                  borderRadius: '4px',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  fontSize: '8px',
                  color: 'white',
                  fontWeight: 'bold'
                }}
              >
                MC
              </Box>
              <Box
                style={{
                  width: '32px',
                  height: '20px',
                  backgroundColor: cardType === 'amex' ? '#006FCF' : 'var(--color-gray-light)',
                  borderRadius: '4px',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  fontSize: '8px',
                  color: 'white',
                  fontWeight: 'bold'
                }}
              >
                AMEX
              </Box>
            </Box>
          </Box>

          {/* Cardholder Name */}
          <Box>
            <Text
              component="label"
              style={{
                display: 'block',
                fontFamily: 'var(--font-heading)',
                fontSize: '14px',
                fontWeight: 600,
                color: 'var(--color-smoke)',
                marginBottom: '4px',
                textTransform: 'uppercase',
                letterSpacing: '0.5px'
              }}
            >
              Cardholder Name
            </Text>
            <TextInput
              value={cardData.cardholderName}
              onChange={(event) =>
                onCardDataChange({
                  ...cardData,
                  cardholderName: event.target.value
                })
              }
              placeholder="Name on card"
              disabled={isProcessing}
              styles={{
                input: {
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
                }
              }}
            />
          </Box>
        </Group>

        {/* Row 2: Expiry Date, CVV, and Billing ZIP */}
        <Group grow>
          {/* Expiry Date */}
          <Box>
            <Text
              component="label"
              style={{
                display: 'block',
                fontFamily: 'var(--font-heading)',
                fontSize: '14px',
                fontWeight: 600,
                color: 'var(--color-smoke)',
                marginBottom: '4px',
                textTransform: 'uppercase',
                letterSpacing: '0.5px'
              }}
            >
              Expiry Date
            </Text>
            <TextInput
              value={cardData.expiryDate}
              onChange={handleExpiryChange}
              placeholder="MM/YY"
              disabled={isProcessing}
              maxLength={5}
              styles={{
                input: {
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
                }
              }}
            />
          </Box>

          {/* CVV */}
          <Box>
            <Text
              component="label"
              style={{
                display: 'block',
                fontFamily: 'var(--font-heading)',
                fontSize: '14px',
                fontWeight: 600,
                color: 'var(--color-smoke)',
                marginBottom: '4px',
                textTransform: 'uppercase',
                letterSpacing: '0.5px'
              }}
            >
              CVV
            </Text>
            <TextInput
              value={cardData.cvv}
              onChange={handleCvvChange}
              placeholder="123"
              disabled={isProcessing}
              maxLength={4}
              styles={{
                input: {
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
                }
              }}
            />
          </Box>

          {/* Billing ZIP */}
          <Box>
            <Text
              component="label"
              style={{
                display: 'block',
                fontFamily: 'var(--font-heading)',
                fontSize: '14px',
                fontWeight: 600,
                color: 'var(--color-smoke)',
                marginBottom: '4px',
                textTransform: 'uppercase',
                letterSpacing: '0.5px'
              }}
            >
              Billing ZIP
            </Text>
            <TextInput
              value={cardData.billingZip}
              onChange={handleZipChange}
              placeholder="12345"
              disabled={isProcessing}
              maxLength={5}
              styles={{
                input: {
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
                }
              }}
            />
          </Box>
        </Group>

        {/* Row 3: Terms & Conditions */}
        <Box>
          <Group gap="xs">
            <input
              type="checkbox"
              id="terms-checkbox"
              checked={termsAccepted}
              onChange={(e) => onTermsChange(e.target.checked)}
              disabled={isProcessing}
              style={{
                width: '20px',
                height: '20px',
                cursor: isProcessing ? 'not-allowed' : 'pointer',
                accentColor: 'var(--color-burgundy)',
                marginTop: '2px'
              }}
            />
            <Text
              component="label"
              htmlFor="terms-checkbox"
              size="sm"
              style={{
                color: 'var(--color-stone)',
                lineHeight: 1.6,
                cursor: isProcessing ? 'not-allowed' : 'pointer',
                userSelect: 'none',
                flex: 1
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
      </Stack>
    </>
  );
};
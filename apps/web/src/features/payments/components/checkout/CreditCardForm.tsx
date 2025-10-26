// Credit Card Form Component
// Handles credit card payment information input

import React from 'react';
import { Stack, TextInput, Group, Box, Text } from '@mantine/core';

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
}

/**
 * Credit card form component
 * Collects card details for payment processing
 */
export const CreditCardForm: React.FC<CreditCardFormProps> = ({
  cardData,
  onCardDataChange,
  isProcessing
}) => {
  /**
   * Format card number with spaces every 4 digits
   */
  const formatCardNumber = (value: string) => {
    // Remove all non-digits
    const digitsOnly = value.replace(/\D/g, '');
    // Add spaces every 4 digits
    const formatted = digitsOnly.match(/.{1,4}/g)?.join(' ') || digitsOnly;
    return formatted.substring(0, 19); // Max length for formatted card number
  };

  /**
   * Format expiry date as MM/YY
   */
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

  return (
    <Stack gap="md">
      {/* Row 1: Card Number and Cardholder Name */}
      <Group grow align="flex-start">
        {/* Card Number */}
        <Box style={{ position: 'relative' }}>
          <Text component="label" style={labelStyles}>
            Card Number
          </Text>
          <TextInput
            value={cardData.cardNumber}
            onChange={handleCardNumberChange}
            placeholder="1234 5678 9012 3456"
            disabled={isProcessing}
            styles={{
              input: {
                ...inputStyles,
                paddingRight: '80px' // Space for card icons
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
          <Text component="label" style={labelStyles}>
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
            styles={{ input: inputStyles }}
          />
        </Box>
      </Group>

      {/* Row 2: Expiry Date, CVV, and Billing ZIP */}
      <Group grow>
        {/* Expiry Date */}
        <Box>
          <Text component="label" style={labelStyles}>
            Expiry Date
          </Text>
          <TextInput
            value={cardData.expiryDate}
            onChange={handleExpiryChange}
            placeholder="MM/YY"
            disabled={isProcessing}
            maxLength={5}
            styles={{ input: inputStyles }}
          />
        </Box>

        {/* CVV */}
        <Box>
          <Text component="label" style={labelStyles}>
            CVV
          </Text>
          <TextInput
            type="password"
            value={cardData.cvv}
            onChange={handleCvvChange}
            placeholder="123"
            disabled={isProcessing}
            maxLength={4}
            styles={{ input: inputStyles }}
          />
        </Box>

        {/* Billing ZIP */}
        <Box>
          <Text component="label" style={labelStyles}>
            Billing ZIP
          </Text>
          <TextInput
            value={cardData.billingZip}
            onChange={handleZipChange}
            placeholder="12345"
            disabled={isProcessing}
            maxLength={5}
            styles={{ input: inputStyles }}
          />
        </Box>
      </Group>
    </Stack>
  );
};

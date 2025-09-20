import React from 'react';
import { Box, Group, Text } from '@mantine/core';

interface PaymentMethodSelectorProps {
  selectedMethod: 'card' | 'paypal' | 'venmo';
  onMethodChange: (method: 'card' | 'paypal' | 'venmo') => void;
}

export const PaymentMethodSelector: React.FC<PaymentMethodSelectorProps> = ({
  selectedMethod,
  onMethodChange
}) => {
  const paymentMethods = [
    {
      id: 'card' as const,
      label: 'Credit Card',
      icon: '💳'
    },
    {
      id: 'paypal' as const,
      label: 'PayPal',
      icon: '🟦'
    },
    {
      id: 'venmo' as const,
      label: 'Venmo',
      icon: '💙'
    }
  ];

  return (
    <Group gap="md" style={{ justifyContent: 'center', marginBottom: 'var(--space-md)' }}>
      {paymentMethods.map((method) => (
        <Box
          key={method.id}
          onClick={() => onMethodChange(method.id)}
          style={{
            padding: 'var(--space-sm) var(--space-lg)',
            border: `2px solid ${
              selectedMethod === method.id
                ? 'var(--color-burgundy)'
                : 'var(--color-taupe)'
            }`,
            borderRadius: '8px',
            cursor: 'pointer',
            transition: 'all 0.3s ease',
            background: selectedMethod === method.id
              ? 'linear-gradient(135deg, var(--color-burgundy) 0%, var(--color-plum) 100%)'
              : 'var(--color-cream)',
            color: selectedMethod === method.id
              ? 'var(--color-ivory)'
              : 'var(--color-charcoal)',
            fontFamily: 'var(--font-heading)',
            fontWeight: 600,
            fontSize: '16px',
            textTransform: 'uppercase',
            letterSpacing: '0.5px',
            minWidth: '140px',
            textAlign: 'center'
          }}
          className="payment-method-button"
        >
          <Group gap="xs" style={{ justifyContent: 'center' }}>
            <Text component="span" style={{ fontSize: '18px' }}>
              {method.icon}
            </Text>
            <Text
              component="span"
              style={{
                fontFamily: 'var(--font-heading)',
                fontWeight: 600,
                fontSize: '16px',
                textTransform: 'uppercase',
                letterSpacing: '0.5px'
              }}
            >
              {method.label}
            </Text>
          </Group>
        </Box>
      ))}
    </Group>
  );
};
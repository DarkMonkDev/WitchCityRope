// Payment Method Selector Component
// Allows users to choose between credit card and PayPal payment methods

import React from 'react';
import { Group, Paper, Stack, Text, UnstyledButton } from '@mantine/core';
import { IconCreditCard, IconBrandPaypal } from '@tabler/icons-react';

interface PaymentMethodSelectorProps {
  selectedMethod: 'card' | 'paypal';
  onMethodChange: (method: 'card' | 'paypal') => void;
}

/**
 * Payment method selector component
 * Shows card and PayPal options for user selection
 */
export const PaymentMethodSelector: React.FC<PaymentMethodSelectorProps> = ({
  selectedMethod,
  onMethodChange
}) => {
  return (
    <Stack gap="sm">
      <Text fw={600} c="#880124" size="sm">
        Select Payment Method
      </Text>

      <Group gap="md" grow>
        {/* Credit Card Option */}
        <UnstyledButton onClick={() => onMethodChange('card')}>
          <Paper
            p="md"
            radius="md"
            withBorder
            style={{
              borderColor: selectedMethod === 'card' ? '#880124' : '#dee2e6',
              borderWidth: selectedMethod === 'card' ? 2 : 1,
              backgroundColor: selectedMethod === 'card' ? '#FAF6F2' : '#ffffff',
              cursor: 'pointer',
              transition: 'all 0.2s ease'
            }}
          >
            <Stack gap="xs" align="center">
              <IconCreditCard
                size={32}
                color={selectedMethod === 'card' ? '#880124' : '#868e96'}
              />
              <Text
                fw={selectedMethod === 'card' ? 600 : 400}
                c={selectedMethod === 'card' ? '#880124' : 'dimmed'}
                size="sm"
              >
                Credit Card
              </Text>
            </Stack>
          </Paper>
        </UnstyledButton>

        {/* PayPal Option */}
        <UnstyledButton onClick={() => onMethodChange('paypal')}>
          <Paper
            p="md"
            radius="md"
            withBorder
            style={{
              borderColor: selectedMethod === 'paypal' ? '#880124' : '#dee2e6',
              borderWidth: selectedMethod === 'paypal' ? 2 : 1,
              backgroundColor: selectedMethod === 'paypal' ? '#FAF6F2' : '#ffffff',
              cursor: 'pointer',
              transition: 'all 0.2s ease'
            }}
          >
            <Stack gap="xs" align="center">
              <IconBrandPaypal
                size={32}
                color={selectedMethod === 'paypal' ? '#880124' : '#868e96'}
              />
              <Text
                fw={selectedMethod === 'paypal' ? 600 : 400}
                c={selectedMethod === 'paypal' ? '#880124' : 'dimmed'}
                size="sm"
              >
                PayPal
              </Text>
            </Stack>
          </Paper>
        </UnstyledButton>
      </Group>
    </Stack>
  );
};

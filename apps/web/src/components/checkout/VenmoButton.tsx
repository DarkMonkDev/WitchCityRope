import React, { useState } from 'react';
import { Button, Box, Text, Alert, Stack } from '@mantine/core';
import { IconAlertCircle } from '@tabler/icons-react';

export interface VenmoButtonProps {
  amount: number;
  eventTitle: string;
  onPaymentSuccess?: (details: any) => void;
  onPaymentError?: (error: string) => void;
  onPaymentCancel?: () => void;
  disabled?: boolean;
}

export const VenmoButton: React.FC<VenmoButtonProps> = ({
  amount,
  eventTitle,
  onPaymentSuccess,
  onPaymentError,
  onPaymentCancel,
  disabled = false
}) => {
  const [isProcessing, setIsProcessing] = useState(false);

  const handleVenmoClick = async () => {
    setIsProcessing(true);

    try {
      // For now, simulate Venmo payment processing
      // In a real implementation, this would integrate with Venmo's SDK
      await new Promise(resolve => setTimeout(resolve, 2000));

      // Simulate successful payment
      const paymentDetails = {
        id: `venmo_${Date.now()}`,
        method: 'venmo',
        amount: amount,
        currency: 'USD',
        status: 'completed',
        transactionId: `VENMO-${Date.now()}`,
        confirmationNumber: `WCR-${new Date().getFullYear()}-${String(Date.now()).slice(-6)}`
      };

      onPaymentSuccess?.(paymentDetails);
    } catch (error) {
      onPaymentError?.('Venmo payment failed. Please try again.');
    } finally {
      setIsProcessing(false);
    }
  };

  return (
    <Box>
      {/* Payment Summary */}
      <Box mb="md" p="md" style={{
        background: 'var(--color-cream)',
        borderRadius: '8px',
        border: '1px solid rgba(136, 1, 36, 0.1)'
      }}>
        <Stack gap="xs">
          <Text fw={600} size="md">Payment Summary</Text>
          <Text size="sm" c="dimmed">{eventTitle}</Text>
          <Text fw={700} size="lg" style={{ color: 'var(--color-burgundy)' }}>
            Total: ${amount.toFixed(2)}
          </Text>
        </Stack>
      </Box>

      {/* Coming Soon Notice */}
      <Alert
        icon={<IconAlertCircle size={16} />}
        title="Venmo Integration Coming Soon"
        color="blue"
        mb="md"
      >
        <Text size="sm">
          Venmo payment processing is currently being integrated. Please use PayPal or credit card for now.
        </Text>
      </Alert>

      {/* Branded Venmo Button */}
      <Button
        onClick={handleVenmoClick}
        loading={isProcessing}
        disabled={disabled || true} // Disabled until real integration
        size="lg"
        style={{
          width: '100%',
          height: '56px',
          backgroundColor: '#3D95CE',
          color: 'white',
          fontSize: '18px',
          fontWeight: 700,
          borderRadius: '8px',
          border: 'none',
          position: 'relative',
          overflow: 'hidden'
        }}
        styles={{
          root: {
            '&:hover': {
              backgroundColor: '#2E7BA6'
            },
            '&:disabled': {
              backgroundColor: '#B0BEC5',
              color: '#FFFFFF'
            }
          }
        }}
      >
        <Box style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
          {/* Venmo Logo Placeholder */}
          <Box
            style={{
              width: '28px',
              height: '28px',
              backgroundColor: 'white',
              borderRadius: '4px',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              fontWeight: 900,
              fontSize: '16px',
              color: '#3D95CE'
            }}
          >
            V
          </Box>
          <Text
            style={{
              fontFamily: 'var(--font-heading)',
              fontSize: '18px',
              fontWeight: 700,
              color: 'white'
            }}
          >
            Pay with Venmo
          </Text>
        </Box>
      </Button>

      {/* Helper Text */}
      <Text size="xs" c="dimmed" ta="center" mt="sm">
        Secure payment processing through Venmo
      </Text>
    </Box>
  );
};
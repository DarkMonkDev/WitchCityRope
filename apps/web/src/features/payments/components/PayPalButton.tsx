// TEMPORARY PLACEHOLDER - PayPal Button Component
// This file has been temporarily simplified to fix React mounting issues
// The PayPal dependency is causing module resolution failures

import React from 'react';
import { Alert, Button } from '@mantine/core';
import type { PaymentEventInfo } from '../types/payment.types';

export interface PayPalButtonProps {
  eventInfo: PaymentEventInfo;
  amount: number;
  slidingScalePercentage: number;
  onPaymentSuccess?: (orderId: string) => void;
  onPaymentError?: (error: string) => void;
  onPaymentCancel?: () => void;
  disabled?: boolean;
}

export const PayPalButton: React.FC<PayPalButtonProps> = (props) => {
  return (
    <Alert color="yellow" title="PayPal Integration Temporarily Disabled">
      PayPal payment processing is currently unavailable due to a dependency issue.
      Please contact support or try again later.
      <br />
      <br />
      <Button 
        variant="outline" 
        color="gray"
        disabled
        onClick={() => props.onPaymentError?.('PayPal temporarily unavailable')}
      >
        PayPal Payment (Disabled)
      </Button>
    </Alert>
  );
};
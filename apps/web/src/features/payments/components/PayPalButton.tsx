import React, { useState, useRef } from 'react';
import { PayPalButtons, PayPalScriptProvider } from '@paypal/react-paypal-js';
import { Alert, Button, Box, Text, Loader } from '@mantine/core';
import { IconAlertCircle } from '@tabler/icons-react';
import { debugLog } from '../../../utils/debug';
import { apiClient } from '../../../lib/api/client';

export interface PayPalButtonProps {
  /** Event ID for the ticket purchase */
  eventId: string;
  /** Ticket type IDs to purchase */
  ticketTypeIds: string[];
  /** Total payment amount (after sliding scale) */
  amount: number;
  /** Sliding scale discount percentage applied */
  slidingScalePercentage?: number;
  /** Currency code */
  currency?: string;
  /** Event title for PayPal order description */
  eventTitle?: string;
  /** Whether the event waiver has been accepted */
  eventWaiverAccepted?: boolean;
  /** Idempotency key to prevent duplicate purchases */
  idempotencyKey: string;
  /** Callback when PayPal payment completes successfully */
  onPaymentSuccess?: (details: PayPalCheckoutResult) => void;
  /** Callback when PayPal payment fails */
  onPaymentError?: (error: string) => void;
  /** Callback when user cancels PayPal popup */
  onPaymentCancel?: () => void;
  /** Whether the button is disabled */
  disabled?: boolean;
}

/** Result returned on successful PayPal checkout */
export interface PayPalCheckoutResult {
  captureId: string;
  status: string;
  amount: string;
  currency: string;
  payerId?: string;
  confirmationNumber?: string;
  ticketPurchaseIds: string[];
}

/**
 * PayPal checkout button using the unified checkout flow.
 * Creates pending ticket purchases before opening the PayPal popup,
 * then finalizes them after payment capture. Mirrors the credit card
 * checkout flow for consistent ticket + payment handling.
 */
export const PayPalButton: React.FC<PayPalButtonProps> = ({
  eventId,
  ticketTypeIds,
  amount,
  slidingScalePercentage = 0,
  currency = 'USD',
  eventTitle,
  eventWaiverAccepted = false,
  idempotencyKey,
  onPaymentSuccess,
  onPaymentError,
  onPaymentCancel,
  disabled = false
}) => {
  const [isProcessing, setIsProcessing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  // Track the PayPal order ID so onCancel can clean up pending tickets
  const currentOrderIdRef = useRef<string | null>(null);

  // PayPal configuration from environment
  const paypalClientId = import.meta.env.VITE_PAYPAL_CLIENT_ID;

  debugLog('PayPal Button Configuration:');
  debugLog('  - paypalClientId:', paypalClientId ? `${paypalClientId.slice(0, 10)}...` : 'NOT SET');
  debugLog('  - eventId:', eventId);
  debugLog('  - ticketTypeIds:', ticketTypeIds);
  debugLog('  - amount:', amount);
  debugLog('  - slidingScalePercentage:', slidingScalePercentage);

  // Check if PayPal is properly configured
  if (!paypalClientId) {
    return (
      <Alert
        icon={<IconAlertCircle size={16} />}
        title="PayPal Configuration Error"
        color="red"
      >
        <Text size="sm">
          PayPal is not properly configured. Please check the environment configuration.
        </Text>
      </Alert>
    );
  }

  /**
   * Called by PayPal JS SDK when user clicks the PayPal button.
   * Creates pending ticket purchases + PayPal order on the backend.
   * Returns the PayPal order ID for the JS SDK to open the popup.
   */
  const createOrder = async () => {
    try {
      setIsProcessing(true);
      setError(null);

      debugLog('Creating PayPal checkout order:', {
        eventId,
        ticketTypeIds,
        amount,
        slidingScalePercentage,
        idempotencyKey
      });

      // Call the unified PayPal checkout endpoint which:
      // 1. Creates pending ticket purchase(s)
      // 2. Creates a PayPal order linked to them
      // 3. Returns the PayPal order ID
      // Ensure slidingScalePercentage is an integer — the backend DTO expects Int32.
      // JavaScript sliders can produce floats (e.g., 33.33) which fail deserialization.
      const response = await apiClient.post('/api/checkout/paypal/create-order', {
        eventId,
        ticketTypeIds,
        amount,
        slidingScalePercentage: Math.round(slidingScalePercentage),
        currency,
        eventTitle,
        eventWaiverAccepted,
        idempotencyKey
      });

      const { orderId } = response.data;
      currentOrderIdRef.current = orderId;
      debugLog('PayPal checkout order created:', orderId);

      return orderId;
    } catch (err) {
      console.error('PayPal checkout order creation failed:', err);
      const errorMessage = err instanceof Error ? err.message : 'Failed to create PayPal order';
      setError(errorMessage);
      onPaymentError?.(errorMessage);
      setIsProcessing(false);
      throw err;
    }
  };

  /**
   * Called by PayPal JS SDK after user approves payment in the popup.
   * Captures the PayPal order and finalizes ticket purchases on the backend.
   */
  const onApprove = async (data: { orderID: string }) => {
    try {
      setIsProcessing(true);
      debugLog('PayPal payment approved, capturing:', data);

      // Call the unified capture endpoint which:
      // 1. Captures the PayPal payment
      // 2. Finalizes ticket purchase(s) (marks as Completed)
      // 3. Activates attendance records
      // 4. Sends confirmation emails
      const response = await apiClient.post('/api/checkout/paypal/capture-order', {
        orderId: data.orderID
      });

      const captureResult: PayPalCheckoutResult = response.data;
      debugLog('PayPal payment captured and tickets finalized:', captureResult);

      currentOrderIdRef.current = null;
      onPaymentSuccess?.(captureResult);

    } catch (err) {
      console.error('PayPal payment capture failed:', err);
      const errorMessage = err instanceof Error ? err.message : 'Failed to capture PayPal payment';
      setError(errorMessage);
      onPaymentError?.(errorMessage);
    } finally {
      setIsProcessing(false);
    }
  };

  /**
   * Called by PayPal JS SDK when payment encounters an error.
   */
  const onError = (err: Record<string, unknown>) => {
    console.error('PayPal SDK error:', err);
    const errorMessage = (err?.message as string) || 'PayPal payment failed';
    setError(errorMessage);
    onPaymentError?.(errorMessage);
    setIsProcessing(false);
  };

  /**
   * Called by PayPal JS SDK when user closes/cancels the popup.
   * Rolls back the pending ticket purchases created during createOrder.
   */
  const onCancel = async () => {
    debugLog('PayPal payment cancelled by user');

    // Clean up: roll back pending ticket purchases on the backend
    if (currentOrderIdRef.current) {
      try {
        await apiClient.post('/api/checkout/paypal/cancel-order', {
          orderId: currentOrderIdRef.current
        });
        debugLog('Pending ticket purchases rolled back after PayPal cancellation');
      } catch (err) {
        // Non-fatal: log but don't show error to user
        console.error('Failed to roll back pending tickets after cancel:', err);
      }
      currentOrderIdRef.current = null;
    }

    onPaymentCancel?.();
    setIsProcessing(false);
  };

  return (
    <PayPalScriptProvider
      options={{
        clientId: paypalClientId,
        currency,
        intent: 'capture',
        "enable-funding": "venmo",
        "disable-funding": "card,paylater"
      }}
    >
      <Box>
        {/* Error Display */}
        {error && (
          <Alert
            icon={<IconAlertCircle size={16} />}
            title="Payment Error"
            color="red"
            mb="md"
          >
            <Text size="sm">{error}</Text>
            <Button
              size="xs"
              variant="outline"
              color="red"
              mt="xs"
              onClick={() => setError(null)}
            >
              Try Again
            </Button>
          </Alert>
        )}

        {/* Processing State */}
        {isProcessing && (
          <Box mb="md" p="md" style={{
            background: 'var(--color-cream)',
            borderRadius: '8px',
            textAlign: 'center'
          }}>
            <Loader size="sm" color="blue" />
            <Text size="sm" mt="xs">Processing payment...</Text>
          </Box>
        )}

        {/* Mobile Layout - Vertical Buttons */}
        <Box hiddenFrom="md">
          <PayPalButtons
            style={{
              layout: 'vertical',
              color: 'gold',
              shape: 'rect',
              label: 'paypal',
              height: 55,
              tagline: false
            }}
            disabled={disabled || isProcessing}
            createOrder={createOrder}
            onApprove={onApprove}
            onError={onError}
            onCancel={onCancel}
          />
        </Box>

        {/* Desktop/Tablet Layout - Horizontal Buttons */}
        <Box visibleFrom="md">
          <PayPalButtons
            style={{
              layout: 'horizontal',
              color: 'gold',
              shape: 'rect',
              label: 'paypal',
              height: 55,
              tagline: false
            }}
            disabled={disabled || isProcessing}
            createOrder={createOrder}
            onApprove={onApprove}
            onError={onError}
            onCancel={onCancel}
          />
        </Box>

        {/* Helper Text */}
        <Text size="xs" c="dimmed" ta="center" mt="sm">
          You'll be redirected to PayPal to complete your payment securely
        </Text>
      </Box>
    </PayPalScriptProvider>
  );
};

// useKioskPaymentStream - SSE hook for real-time payment notifications
// Connects to backend SSE endpoint for payment completion events
// Source: /docs/functional-areas/events/new-work/2025-11-03-streamlined-checkin-workflow/design/ui-specifications.md

import { useEffect, useState, useRef } from 'react';

export interface PaymentEvent {
  paymentId: string;
  attendeeId: string;
  amount: number;
  timestamp: string;
}

export type ConnectionStatus = 'connecting' | 'connected' | 'error' | 'closed';

export interface UseKioskPaymentStreamReturn {
  lastPayment: PaymentEvent | null;
  status: ConnectionStatus;
  error: string | null;
}

/**
 * Custom hook for real-time payment notifications via SSE
 *
 * @param sessionToken - Kiosk session token for authentication
 * @returns Payment stream state with last payment and connection status
 *
 * @example
 * ```tsx
 * const { lastPayment, status } = useKioskPaymentStream(sessionToken);
 *
 * useEffect(() => {
 *   if (lastPayment?.attendeeId === currentAttendeeId) {
 *     // Handle payment completion
 *     onPaymentComplete();
 *   }
 * }, [lastPayment]);
 * ```
 */
export const useKioskPaymentStream = (
  sessionToken: string
): UseKioskPaymentStreamReturn => {
  const [lastPayment, setLastPayment] = useState<PaymentEvent | null>(null);
  const [status, setStatus] = useState<ConnectionStatus>('connecting');
  const [error, setError] = useState<string | null>(null);
  const eventSourceRef = useRef<EventSource | null>(null);
  const reconnectTimeoutRef = useRef<NodeJS.Timeout | null>(null);

  useEffect(() => {
    if (!sessionToken) {
      setError('Session token is required');
      setStatus('error');
      return;
    }

    // Construct SSE endpoint URL
    const apiBaseUrl = import.meta.env.VITE_API_URL || 'http://localhost:5655';
    const sseUrl = `${apiBaseUrl}/api/kiosk/payment-stream/${sessionToken}`;

    console.log('[SSE] Connecting to payment stream:', sseUrl);

    // Create EventSource connection
    const eventSource = new EventSource(sseUrl);
    eventSourceRef.current = eventSource;

    // Connection opened
    eventSource.onopen = () => {
      console.log('[SSE] Connection established');
      setStatus('connected');
      setError(null);
    };

    // Listen for 'connected' event (initial connection confirmation)
    eventSource.addEventListener('connected', (event) => {
      console.log('[SSE] Received connected event:', event.data);
      setStatus('connected');
    });

    // Listen for 'payment_complete' events
    eventSource.addEventListener('payment_complete', (event) => {
      try {
        const payment = JSON.parse(event.data) as PaymentEvent;
        console.log('[SSE] Payment complete:', payment);
        setLastPayment(payment);
      } catch (err) {
        console.error('[SSE] Failed to parse payment event:', err, event.data);
        setError('Failed to parse payment data');
      }
    });

    // Listen for 'heartbeat' events (keep-alive)
    eventSource.addEventListener('heartbeat', (event) => {
      console.log('[SSE] Heartbeat received:', event.data);
    });

    // Handle errors
    eventSource.onerror = (event) => {
      console.error('[SSE] Connection error:', event);
      setStatus('error');
      setError('Connection error - attempting reconnect...');

      // Attempt reconnection after 3 seconds
      if (reconnectTimeoutRef.current) {
        clearTimeout(reconnectTimeoutRef.current);
      }

      reconnectTimeoutRef.current = setTimeout(() => {
        console.log('[SSE] Attempting reconnection...');
        setStatus('connecting');
        // EventSource will automatically attempt to reconnect
      }, 3000);
    };

    // Cleanup on unmount
    return () => {
      console.log('[SSE] Closing connection');
      if (eventSourceRef.current) {
        eventSourceRef.current.close();
        eventSourceRef.current = null;
      }
      if (reconnectTimeoutRef.current) {
        clearTimeout(reconnectTimeoutRef.current);
        reconnectTimeoutRef.current = null;
      }
      setStatus('closed');
    };
  }, [sessionToken]);

  return {
    lastPayment,
    status,
    error
  };
};

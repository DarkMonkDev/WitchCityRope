// React Query hooks for payment operations
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { notifications } from '@mantine/notifications';
import { paymentsService, type CreateTicketPurchaseRequest, type CheckoutRequest, type CheckoutResponse, type TicketPurchaseResponse } from '../services/payments';

/**
 * Hook for purchasing tickets through the backend API
 */
export function usePurchaseTicket() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: CreateTicketPurchaseRequest) =>
      paymentsService.purchaseTicket(request),

    onSuccess: (data: TicketPurchaseResponse, variables: CreateTicketPurchaseRequest) => {
      // Invalidate participation data to refresh UI (event details page)
      queryClient.invalidateQueries({
        queryKey: ['participation', 'event', variables.eventId]
      });

      // Invalidate user participations list (dashboard)
      queryClient.invalidateQueries({
        queryKey: ['participation', 'user']
      });

      // Invalidate admin event participations table
      queryClient.invalidateQueries({
        queryKey: ['events', variables.eventId, 'participations']
      });

      // Invalidate check-in queries so kiosk shows updated ticket status
      queryClient.invalidateQueries({
        queryKey: ['checkin', 'attendees', variables.eventId]
      });
      queryClient.invalidateQueries({
        queryKey: ['checkin', 'dashboard', variables.eventId]
      });

      // Invalidate volunteer positions - ticket purchase affects canSignUp eligibility
      queryClient.invalidateQueries({
        queryKey: ['volunteerPositions', variables.eventId]
      });

      // Invalidate dashboard event cards so ticket info appears without manual refresh
      queryClient.invalidateQueries({
        queryKey: ['user-events']
      });

      // Show success notification
      notifications.show({
        title: 'Ticket Purchased Successfully!',
        message: 'Your ticket has been confirmed. Check your email for details.',
        color: 'green',
        autoClose: 5000
      });

      console.log('✅ Ticket purchase completed:', data);
    },

    onError: (error: any) => {
      // apiClient interceptor extracts RFC 9457 message to error.message
      const errorMessage = error instanceof Error ? error.message : 'Failed to purchase ticket';

      notifications.show({
        title: 'Purchase Failed',
        message: errorMessage,
        color: 'red',
        autoClose: 7000
      });

      console.error('❌ Ticket purchase failed:', error);
    }
  });
}

/**
 * Hook for unified checkout: create ticket + charge card atomically.
 * Returns structured error information for stage-aware error display.
 */
export function useCheckout() {
  const queryClient = useQueryClient();

  return useMutation<CheckoutResponse, Error, CheckoutRequest>({
    mutationFn: (request: CheckoutRequest) =>
      paymentsService.checkout(request),

    onSuccess: (_data: CheckoutResponse, variables: CheckoutRequest) => {
      // Invalidate participation data to refresh UI
      queryClient.invalidateQueries({
        queryKey: ['participation', 'event', variables.eventId]
      });
      queryClient.invalidateQueries({
        queryKey: ['participation', 'user']
      });
      queryClient.invalidateQueries({
        queryKey: ['events', variables.eventId, 'participations']
      });
      queryClient.invalidateQueries({
        queryKey: ['checkin', 'attendees', variables.eventId]
      });
      queryClient.invalidateQueries({
        queryKey: ['checkin', 'dashboard', variables.eventId]
      });
      queryClient.invalidateQueries({
        queryKey: ['volunteerPositions', variables.eventId]
      });

      // Invalidate dashboard event cards so ticket info appears without manual refresh
      queryClient.invalidateQueries({
        queryKey: ['user-events']
      });
    },

    onError: (error: any) => {
      // Don't show notification here - let the page component handle error display
      // with stage-aware messaging
      console.error('Checkout failed:', error);
    }
  });
}

/**
 * Hook for creating PayPal orders
 */
export function useCreatePayPalOrder() {
  return useMutation({
    mutationFn: paymentsService.createPayPalOrder,

    onError: (error: any) => {
      // apiClient interceptor extracts RFC 9457 message to error.message
      const errorMessage = error instanceof Error ? error.message : 'Failed to create PayPal order';

      notifications.show({
        title: 'PayPal Order Failed',
        message: errorMessage,
        color: 'red',
        autoClose: 7000
      });

      console.error('❌ PayPal order creation failed:', error);
    }
  });
}

/**
 * Hook for confirming PayPal payments and creating tickets
 */
export function useConfirmPayPalPayment() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ orderId, paymentDetails, ticketTypeIds }: { orderId: string; paymentDetails: any; ticketTypeIds: string[] }) =>
      paymentsService.confirmPayPalPayment(orderId, paymentDetails, ticketTypeIds),

    onSuccess: (data: TicketPurchaseResponse, variables: { orderId: string; paymentDetails: any; ticketTypeIds: string[] }) => {
      // Extract event ID from payment details
      const eventId = variables.paymentDetails?.purchase_units?.[0]?.custom_id || '';

      if (eventId) {
        // Invalidate participation data to refresh UI (event details page)
        queryClient.invalidateQueries({
          queryKey: ['participation', 'event', eventId]
        });

        // Invalidate admin event participations table
        queryClient.invalidateQueries({
          queryKey: ['events', eventId, 'participations']
        });

        // Invalidate check-in queries so kiosk shows updated ticket status
        queryClient.invalidateQueries({
          queryKey: ['checkin', 'attendees', eventId]
        });
        queryClient.invalidateQueries({
          queryKey: ['checkin', 'dashboard', eventId]
        });

        // Invalidate volunteer positions - ticket purchase affects canSignUp eligibility
        queryClient.invalidateQueries({
          queryKey: ['volunteerPositions', eventId]
        });
      }

      // Invalidate user participations list (dashboard)
      queryClient.invalidateQueries({
        queryKey: ['participation', 'user']
      });

      // Invalidate dashboard event cards so ticket info appears without manual refresh
      queryClient.invalidateQueries({
        queryKey: ['user-events']
      });

      // Show success notification
      notifications.show({
        title: 'Payment Successful!',
        message: 'Your PayPal payment has been processed and your ticket is confirmed.',
        color: 'green',
        autoClose: 5000
      });

      console.log('✅ PayPal payment confirmed:', data);
    },

    onError: (error: any) => {
      // apiClient interceptor extracts RFC 9457 message to error.message
      const errorMessage = error instanceof Error ? error.message : 'Failed to confirm PayPal payment';

      notifications.show({
        title: 'Payment Confirmation Failed',
        message: errorMessage,
        color: 'red',
        autoClose: 7000
      });

      console.error('❌ PayPal payment confirmation failed:', error);
    }
  });
}

export default {
  usePurchaseTicket,
  useCheckout,
  useCreatePayPalOrder,
  useConfirmPayPalPayment
};
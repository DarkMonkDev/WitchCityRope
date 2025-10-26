// React Query hooks for payment operations
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { notifications } from '@mantine/notifications';
import { paymentsService, type CreateTicketPurchaseRequest } from '../services/payments';

/**
 * Hook for purchasing tickets through the backend API
 */
export function usePurchaseTicket() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: CreateTicketPurchaseRequest) =>
      paymentsService.purchaseTicket(request),

    onSuccess: (data, variables) => {
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
      const errorMessage = error?.response?.data?.message || error?.message || 'Failed to purchase ticket';

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
 * Hook for creating PayPal orders
 */
export function useCreatePayPalOrder() {
  return useMutation({
    mutationFn: paymentsService.createPayPalOrder,

    onError: (error: any) => {
      const errorMessage = error?.response?.data?.message || error?.message || 'Failed to create PayPal order';

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
    mutationFn: ({ orderId, paymentDetails }: { orderId: string; paymentDetails: any }) =>
      paymentsService.confirmPayPalPayment(orderId, paymentDetails),

    onSuccess: (data, variables) => {
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
      }

      // Invalidate user participations list (dashboard)
      queryClient.invalidateQueries({
        queryKey: ['participation', 'user']
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
      const errorMessage = error?.response?.data?.message || error?.message || 'Failed to confirm PayPal payment';

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
  useCreatePayPalOrder,
  useConfirmPayPalPayment
};
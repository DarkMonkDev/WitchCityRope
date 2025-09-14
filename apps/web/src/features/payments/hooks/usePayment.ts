// Payment Processing Hook
// Handles payment processing logic with PayPal integration

import { useState, useCallback } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { paymentApi } from '../api/paymentApi';
import type {
  ProcessPaymentRequest,
  PaymentResponse,
  PaymentError,
  PaymentProcessingState,
  PaymentMethodType
} from '../types/payment.types';

/**
 * Hook for managing payment processing
 * @param eventRegistrationId Registration ID for the payment
 * @returns Payment processing state and methods
 */
export const usePayment = (eventRegistrationId?: string) => {
  const queryClient = useQueryClient();
  
  // Processing state
  const [processingState, setProcessingState] = useState<PaymentProcessingState>({
    currentStep: 'selection',
    isProcessing: false
  });

  /**
   * Process payment mutation
   */
  const processPaymentMutation = useMutation({
    mutationFn: async (request: ProcessPaymentRequest) => {
      setProcessingState(prev => ({
        ...prev,
        currentStep: 'processing',
        isProcessing: true,
        error: undefined
      }));

      try {
        const result = await paymentApi.processPayment(request);
        
        setProcessingState(prev => ({
          ...prev,
          currentStep: 'confirmation',
          isProcessing: false,
          paymentResult: result,
          successMessage: 'Payment completed successfully!'
        }));

        return result;
      } catch (error) {
        const paymentError = error as PaymentError;
        
        setProcessingState(prev => ({
          ...prev,
          currentStep: 'error',
          isProcessing: false,
          error: paymentError.message
        }));

        throw error;
      }
    },
    onSuccess: (data) => {
      // Invalidate relevant queries
      if (eventRegistrationId) {
        queryClient.invalidateQueries({ 
          queryKey: ['payment', eventRegistrationId] 
        });
      }
      queryClient.invalidateQueries({ 
        queryKey: ['user-payments'] 
      });
    }
  });

  /**
   * Get payment details query
   */
  const getPaymentQuery = useQuery({
    queryKey: ['payment', eventRegistrationId],
    queryFn: () => paymentApi.getPayment(eventRegistrationId!),
    enabled: !!eventRegistrationId,
    staleTime: 5 * 60 * 1000, // 5 minutes
    refetchOnWindowFocus: false
  });

  /**
   * Process payment with PayPal integration
   */
  const processPayment = useCallback(async (paymentData: {
    originalAmount: number;
    slidingScalePercentage: number;
    paymentMethodType: PaymentMethodType;
    savePaymentMethod: boolean;
    metadata?: Record<string, any>;
  }) => {
    if (!eventRegistrationId) {
      throw new Error('Event registration ID is required');
    }

    const request: ProcessPaymentRequest = {
      eventRegistrationId,
      originalAmount: paymentData.originalAmount,
      currency: 'USD',
      slidingScalePercentage: paymentData.slidingScalePercentage,
      paymentMethodType: paymentData.paymentMethodType,
      returnUrl: window.location.origin + '/payment/success',
      cancelUrl: window.location.origin + '/payment/cancel',
      savePaymentMethod: paymentData.savePaymentMethod,
      metadata: paymentData.metadata
    };

    return await processPaymentMutation.mutateAsync(request);
  }, [eventRegistrationId, processPaymentMutation]);

  /**
   * Create PayPal order (handled by backend)
   */
  const createPayPalOrder = useCallback(async (amount: number, eventInfo: any) => {
    try {
      // PayPal order creation is handled in the PayPalButton component
      // This method is kept for API compatibility but delegates to the component
      return { 
        orderId: 'will-be-created-by-paypal-button',
        amount,
        currency: 'USD'
      };
    } catch (error) {
      console.error('Error preparing PayPal order:', error);
      throw error;
    }
  }, []);

  /**
   * Reset processing state
   */
  const resetProcessingState = useCallback(() => {
    setProcessingState({
      currentStep: 'selection',
      isProcessing: false,
      error: undefined,
      successMessage: undefined,
      paymentResult: undefined
    });
  }, []);

  /**
   * Retry failed payment
   */
  const retryPayment = useCallback(() => {
    setProcessingState(prev => ({
      ...prev,
      currentStep: 'payment',
      error: undefined
    }));
  }, []);

  return {
    // State
    processingState,
    isProcessing: processPaymentMutation.isPending,
    error: (processPaymentMutation.error as unknown) as PaymentError | null,
    paymentData: getPaymentQuery.data,
    isLoadingPayment: getPaymentQuery.isLoading,

    // Actions
    processPayment,
    createPayPalOrder,
    resetProcessingState,
    retryPayment
  };
};

/**
 * Hook for managing user's payment history
 */
export const usePaymentHistory = () => {
  const [page, setPage] = useState(1);
  const pageSize = 10;

  const paymentHistoryQuery = useQuery({
    queryKey: ['user-payments', page, pageSize],
    queryFn: () => paymentApi.getUserPayments(page, pageSize),
    staleTime: 5 * 60 * 1000, // 5 minutes
    refetchOnWindowFocus: false
  });

  const nextPage = useCallback(() => {
    if (paymentHistoryQuery.data && page < (paymentHistoryQuery.data as any).totalPages) {
      setPage(prev => prev + 1);
    }
  }, [page, paymentHistoryQuery.data]);

  const previousPage = useCallback(() => {
    if (page > 1) {
      setPage(prev => prev - 1);
    }
  }, [page]);

  const goToPage = useCallback((newPage: number) => {
    if (newPage >= 1 && paymentHistoryQuery.data && newPage <= (paymentHistoryQuery.data as any).totalPages) {
      setPage(newPage);
    }
  }, [paymentHistoryQuery.data]);

  return {
    payments: (paymentHistoryQuery.data as any)?.payments || [],
    totalCount: (paymentHistoryQuery.data as any)?.totalCount || 0,
    currentPage: page,
    totalPages: (paymentHistoryQuery.data as any)?.totalPages || 1,
    pageSize,
    isLoading: paymentHistoryQuery.isLoading,
    error: paymentHistoryQuery.error,
    
    // Pagination actions
    nextPage,
    previousPage,
    goToPage
  };
};

/**
 * Hook for getting event payments (admin/organizer only)
 */
export const useEventPayments = (eventId?: string) => {
  return useQuery({
    queryKey: ['event-payments', eventId],
    queryFn: () => paymentApi.getEventPayments(eventId!),
    enabled: !!eventId,
    staleTime: 2 * 60 * 1000, // 2 minutes (more frequent for admin views)
    refetchOnWindowFocus: true
  });
};
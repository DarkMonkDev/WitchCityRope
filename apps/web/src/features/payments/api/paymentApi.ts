// Payment API Service
// Handles all communication with the payment endpoints

import { apiClient } from '../../../lib/api/client';
import type {
  ProcessPaymentRequest,
  ProcessRefundRequest,
  PaymentResponse,
  RefundResponse,
  PaymentStatusResponse,
  PaymentError
} from '../types/payment.types';

/**
 * Payment API service class
 * Provides methods for all payment-related API calls
 */
export class PaymentApiService {
  /**
   * Process a payment with sliding scale pricing
   * @param request Payment processing request
   * @returns Promise resolving to payment response
   */
  async processPayment(request: ProcessPaymentRequest): Promise<PaymentResponse> {
    try {
      const response = await apiClient.post<PaymentResponse>('/api/payments/process', request);
      return response.data;
    } catch (error) {
      throw this.handlePaymentError(error);
    }
  }

  /**
   * Get payment details by ID
   * @param paymentId Payment unique identifier
   * @returns Promise resolving to payment response
   */
  async getPayment(paymentId: string): Promise<PaymentResponse> {
    try {
      const response = await apiClient.get<PaymentResponse>(`/api/payments/${paymentId}`);
      return response.data;
    } catch (error) {
      throw this.handlePaymentError(error);
    }
  }

  /**
   * Get payment status for quick checks
   * @param paymentId Payment unique identifier
   * @returns Promise resolving to payment status
   */
  async getPaymentStatus(paymentId: string): Promise<PaymentStatusResponse> {
    try {
      const response = await apiClient.get<PaymentStatusResponse>(`/api/payments/${paymentId}/status`);
      return response.data;
    } catch (error) {
      throw this.handlePaymentError(error);
    }
  }

  /**
   * Process a refund for a payment
   * @param request Refund processing request
   * @returns Promise resolving to refund response
   */
  async processRefund(request: ProcessRefundRequest): Promise<RefundResponse> {
    try {
      const response = await apiClient.post<RefundResponse>(`/api/payments/${request.paymentId}/refund`, request);
      return response.data;
    } catch (error) {
      throw this.handlePaymentError(error);
    }
  }

  /**
   * Get all payments for a user (with pagination)
   * @param page Page number (default: 1)
   * @param pageSize Number of items per page (default: 10)
   * @returns Promise resolving to paginated payment list
   */
  async getUserPayments(page: number = 1, pageSize: number = 10): Promise<{
    payments: PaymentResponse[];
    totalCount: number;
    currentPage: number;
    pageSize: number;
    totalPages: number;
  }> {
    try {
      const response = await apiClient.get('/api/payments/user', {
        params: { page, pageSize }
      });
      return response.data;
    } catch (error) {
      throw this.handlePaymentError(error);
    }
  }

  /**
   * Get all payments for an event (admin/organizer only)
   * @param eventId Event unique identifier
   * @returns Promise resolving to event payment list
   */
  async getEventPayments(eventId: string): Promise<PaymentResponse[]> {
    try {
      const response = await apiClient.get<PaymentResponse[]>(`/api/payments/event/${eventId}`);
      return response.data;
    } catch (error) {
      throw this.handlePaymentError(error);
    }
  }

  /**
   * Handle and normalize payment API errors
   * @param error Raw API error
   * @returns Normalized PaymentError
   */
  private handlePaymentError(error: any): PaymentError {
    // Handle axios errors
    if (error.response?.data) {
      const apiError = error.response.data;
      
      return {
        code: apiError.code || 'PAYMENT_ERROR',
        message: apiError.message || 'An error occurred while processing your payment',
        type: apiError.type || 'api_error',
        retryable: apiError.retryable || false,
        suggestedAction: this.getSuggestedAction(apiError.code)
      };
    }

    // Handle network errors
    if (error.code === 'NETWORK_ERROR' || !error.response) {
      return {
        code: 'NETWORK_ERROR',
        message: 'Unable to connect to payment service. Please check your connection and try again.',
        type: 'network_error',
        retryable: true,
        suggestedAction: 'Check your internet connection and try again'
      };
    }

    // Handle unknown errors
    return {
      code: 'UNKNOWN_ERROR',
      message: 'An unexpected error occurred. Please try again or contact support.',
      type: 'unknown_error',
      retryable: true,
      suggestedAction: 'Try again or contact support if the problem persists'
    };
  }

  /**
   * Get suggested action for specific error codes
   * @param errorCode Error code from API
   * @returns Suggested action string
   */
  private getSuggestedAction(errorCode?: string): string {
    switch (errorCode) {
      case 'CARD_DECLINED':
        return 'Contact your bank or try a different payment method';
      case 'INSUFFICIENT_FUNDS':
        return 'Check your account balance or use a different payment method';
      case 'EXPIRED_CARD':
        return 'Use a different payment method or update your card information';
      case 'INVALID_CARD':
        return 'Check your card information and try again';
      case 'PROCESSING_ERROR':
        return 'Wait a moment and try again';
      case 'DUPLICATE_PAYMENT':
        return 'Check if your payment was already processed';
      default:
        return 'Try again or contact support if the problem persists';
    }
  }
}

/**
 * Singleton instance of the payment API service
 */
export const paymentApi = new PaymentApiService();

/**
 * Utility functions for payment calculations
 */
export const paymentUtils = {
  /**
   * Calculate sliding scale amount
   * @param originalAmount Original price
   * @param discountPercentage Discount percentage (0-75)
   * @returns Final amount and discount info
   */
  calculateSlidingScale(originalAmount: number, discountPercentage: number) {
    // Ensure discount percentage is within valid range
    const validPercentage = Math.max(0, Math.min(75, discountPercentage));
    
    const discountAmount = originalAmount * (validPercentage / 100);
    const finalAmount = originalAmount - discountAmount;

    return {
      originalAmount,
      discountPercentage: validPercentage,
      discountAmount,
      finalAmount,
      display: {
        original: this.formatCurrency(originalAmount),
        final: this.formatCurrency(finalAmount),
        discount: this.formatCurrency(discountAmount),
        percentage: `${Math.round(validPercentage)}%`
      }
    };
  },

  /**
   * Format amount as currency string
   * @param amount Amount to format
   * @param currency Currency code (default: USD)
   * @returns Formatted currency string
   */
  formatCurrency(amount: number, currency: string = 'USD'): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: currency,
      minimumFractionDigits: 2,
      maximumFractionDigits: 2
    }).format(amount);
  },

  /**
   * Validate payment amount for PayPal (must be between $0.01 and $10,000)
   * @param amount Amount in dollars
   * @returns Whether amount is valid for PayPal
   */
  isValidPayPalAmount(amount: number): boolean {
    return amount >= 0.01 && amount <= 10000;
  },

  /**
   * Round amount to PayPal-compatible precision (2 decimal places)
   * @param amount Amount to round
   * @returns Rounded amount
   */
  roundToPayPalPrecision(amount: number): number {
    return Math.round(amount * 100) / 100;
  },

  /**
   * Validate sliding scale percentage
   * @param percentage Percentage to validate
   * @returns Whether percentage is valid
   */
  isValidSlidingScalePercentage(percentage: number): boolean {
    return percentage >= 0 && percentage <= 75;
  },

  /**
   * Get payment status display properties
   * @param status Payment status
   * @returns Display properties for status
   */
  getStatusDisplay(status: string) {
    switch (status.toLowerCase()) {
      case 'pending':
        return {
          color: 'yellow',
          label: 'Processing',
          icon: 'clock'
        };
      case 'completed':
        return {
          color: 'green',
          label: 'Completed',
          icon: 'check'
        };
      case 'failed':
        return {
          color: 'red',
          label: 'Failed',
          icon: 'x'
        };
      case 'refunded':
        return {
          color: 'blue',
          label: 'Refunded',
          icon: 'arrow-back'
        };
      case 'partiallyrefunded':
        return {
          color: 'orange',
          label: 'Partially Refunded',
          icon: 'arrow-back'
        };
      default:
        return {
          color: 'gray',
          label: 'Unknown',
          icon: 'question'
        };
    }
  }
};
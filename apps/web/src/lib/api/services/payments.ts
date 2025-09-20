// Payment API Service
// Integrates with backend payment endpoints and PayPal webhook system

import { apiClient } from '../client';
import type { PaymentResponse, ProcessPaymentRequest } from '../../../features/payments/types/payment.types';

export interface CreateTicketPurchaseRequest {
  eventId: string;
  notes?: string;
  paymentMethodId?: string;
}

export interface TicketPurchaseResponse {
  id: string;
  eventId: string;
  userId: string;
  participationType: 'Ticket';
  status: 'Active' | 'Cancelled';
  createdAt: string;
  updatedAt: string;
  notes?: string;
  paymentMethodId?: string;
  paymentStatus?: 'Pending' | 'Completed' | 'Failed';
}

export interface PayPalOrderRequest {
  eventId: string;
  amount: number;
  currency?: string;
  slidingScalePercentage?: number;
  description?: string;
}

export interface PayPalOrderResponse {
  orderId: string;
  approvalUrl: string;
  amount: number;
  currency: string;
}

/**
 * Payment service for handling ticket purchases and PayPal integration
 */
export const paymentsService = {
  /**
   * Purchase a ticket for a class event
   * Uses the existing backend endpoint: POST /api/events/{eventId}/tickets
   */
  async purchaseTicket(request: CreateTicketPurchaseRequest): Promise<TicketPurchaseResponse> {
    console.log('🔍 Purchasing ticket:', request);

    const response = await apiClient.post<TicketPurchaseResponse>(
      `/api/events/${request.eventId}/tickets`,
      {
        eventId: request.eventId,
        notes: request.notes,
        paymentMethodId: request.paymentMethodId
      }
    );

    console.log('✅ Ticket purchase response:', response.data);
    return response.data;
  },

  /**
   * Create a PayPal order for ticket purchase
   * This would integrate with the backend PayPal service
   */
  async createPayPalOrder(request: PayPalOrderRequest): Promise<PayPalOrderResponse> {
    console.log('🔍 Creating PayPal order:', request);

    // TODO: Implement backend PayPal order creation endpoint
    // For now, return mock data to simulate the flow
    const mockOrderId = `ORDER_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;

    const mockResponse: PayPalOrderResponse = {
      orderId: mockOrderId,
      approvalUrl: `https://www.sandbox.paypal.com/checkoutnow?token=${mockOrderId}`,
      amount: request.amount,
      currency: request.currency || 'USD'
    };

    console.log('✅ Mock PayPal order created:', mockResponse);
    return mockResponse;
  },

  /**
   * Confirm PayPal payment after user approval
   * This would integrate with the backend to confirm payment and create ticket
   */
  async confirmPayPalPayment(orderId: string, paymentDetails: any): Promise<TicketPurchaseResponse> {
    console.log('🔍 Confirming PayPal payment:', { orderId, paymentDetails });

    // Extract event ID from payment details or store it during order creation
    const eventId = paymentDetails?.purchase_units?.[0]?.custom_id || '';

    // For now, call the regular ticket purchase endpoint with PayPal payment method
    const ticketResponse = await this.purchaseTicket({
      eventId,
      notes: `PayPal payment confirmed - Order ID: ${orderId}`,
      paymentMethodId: orderId
    });

    console.log('✅ PayPal payment confirmed and ticket created:', ticketResponse);
    return ticketResponse;
  },

  /**
   * Get payment status for an event registration
   */
  async getPaymentStatus(eventId: string): Promise<any> {
    console.log('🔍 Getting payment status for event:', eventId);

    try {
      const response = await apiClient.get(`/api/events/${eventId}/payment-status`);
      console.log('✅ Payment status:', response.data);
      return response.data;
    } catch (error) {
      console.log('ℹ️ No payment status found or endpoint not implemented:', error);
      return null;
    }
  }
};

export default paymentsService;
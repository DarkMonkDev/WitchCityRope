// Payment API Service
// Integrates with backend payment endpoints and PayPal webhook system

import { apiClient } from '../client';

import { debugLog } from '../../../utils/debug';

export interface CreateTicketPurchaseRequest {
  eventId: string;
  /** Ticket type IDs to purchase */
  ticketTypeIds: string[];
  notes?: string;
  paymentMethodId?: string;
  eventWaiverAccepted: boolean;
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

/** A single ticket selection with quantity and optional assignees (multi-ticket support) */
export interface TicketSelectionItem {
  ticketTypeId: string;
  quantity: number;
  assignees?: (string | null)[];
}

/** Result of a ticket assignment from checkout */
export interface TicketAssignmentResult {
  attendanceId: string;
  ticketPurchaseId: string;
  ticketTypeName: string;
  assignedToUserId?: string | null;
  assignedToSceneName?: string | null;
  status: string;
}

export interface CheckoutRequest {
  eventId: string;
  ticketTypeIds: string[];
  eventWaiverAccepted: boolean;
  nonce: string;
  dataDescriptor: string;
  amount: number;
  lastFourDigits?: string;
  cardType?: string;
  idempotencyKey: string;
  /** Multi-ticket selections (optional, takes precedence over ticketTypeIds when present) */
  ticketSelections?: TicketSelectionItem[];
  /** When true, all tickets are for assignees (purchaser already has a ticket) */
  buyForOthersOnly?: boolean;
}

export interface CheckoutResponse {
  transactionId: string;
  ticketPurchaseIds: string[];
  confirmationNumber: string;
  status: string;
  authCode?: string;
  amountCharged: number;
  /** Assignment results when multi-ticket checkout includes assignments */
  assignments?: TicketAssignmentResult[];
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
    debugLog('🔍 Purchasing ticket:', request);

    const requestBody = {
      eventId: request.eventId,
      ticketTypeIds: request.ticketTypeIds,
      notes: request.notes,
      paymentMethodId: request.paymentMethodId,
      eventWaiverAccepted: request.eventWaiverAccepted
    };

    debugLog('  - Request body:', requestBody);

    const response = await apiClient.post<TicketPurchaseResponse>(
      `/api/events/${request.eventId}/tickets`,
      requestBody
    );

    debugLog('✅ Ticket purchase response:', response.data);
    return response.data;
  },

  /**
   * Unified checkout: create ticket + charge card in a single atomic request.
   * Replaces the broken two-step flow (charge first, then create ticket).
   */
  async checkout(request: CheckoutRequest): Promise<CheckoutResponse> {
    debugLog('Checkout request:', request);

    const response = await apiClient.post<CheckoutResponse>(
      '/api/checkout/credit-card',
      request
    );

    debugLog('Checkout response:', response.data);
    return response.data;
  },

  /**
   * Get payment status for an event registration
   */
  async getPaymentStatus(eventId: string): Promise<any> {
    debugLog('🔍 Getting payment status for event:', eventId);

    try {
      const response = await apiClient.get(`/api/events/${eventId}/payment-status`);
      debugLog('✅ Payment status:', response.data);
      return response.data;
    } catch (error) {
      debugLog('ℹ️ No payment status found or endpoint not implemented:', error);
      return null;
    }
  }
};

export default paymentsService;
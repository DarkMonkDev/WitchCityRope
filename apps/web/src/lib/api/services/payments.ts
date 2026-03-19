// Payment API Service
// Integrates with backend payment endpoints and PayPal webhook system
//
// Type Strategy: Uses auto-generated types from @witchcityrope/shared-types for all
// types that have backend DTO equivalents. TicketPurchaseResponse remains manual
// because the backend does not expose a corresponding DTO in the OpenAPI schema.
// See: /docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md

import type { components } from '@witchcityrope/shared-types';
import { apiClient } from '../client';
import { debugLog } from '../../../utils/debug';

// =============================================================================
// Auto-generated type aliases from @witchcityrope/shared-types
// These MUST stay in sync with C# DTOs via NSwag generation.
// DO NOT manually edit — regenerate: cd packages/shared-types && npm run generate
// =============================================================================

/** Request payload for credit card checkout (atomic ticket + payment) */
export type CheckoutRequest = components['schemas']['CheckoutRequest'];

/** Response from credit card checkout */
export type CheckoutResponse = components['schemas']['CheckoutResponse'];

/** Request payload for ticket purchase (non-payment flow) */
export type CreateTicketPurchaseRequest = components['schemas']['CreateTicketPurchaseRequest'];

/** A single ticket selection with quantity and optional assignees (multi-ticket support) */
export type TicketSelectionItem = components['schemas']['TicketSelectionItem'];

/** Result of a ticket assignment from checkout */
export type TicketAssignmentResult = components['schemas']['TicketAssignmentResultDto'];

// =============================================================================
// Manual type — no backend DTO equivalent in OpenAPI schema
// This can be removed once the backend exposes a TicketPurchaseResponse DTO
// =============================================================================

// eslint-disable-next-line no-restricted-syntax
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

/**
 * Ticket Purchase Factory
 *
 * Create and delete test ticket purchases via API.
 * Uses existing /api/test-helpers/ticket-purchases endpoint.
 */

import type { APIRequestContext } from '@playwright/test';
import { DataFactoryApiClient } from '../api-client';
import type {
  CreateTicketPurchaseRequest,
  TicketPurchaseResponse,
  CleanupItem,
} from '../types';

export class TicketPurchaseFactory {
  private client: DataFactoryApiClient;
  private createdPurchases: string[] = [];

  constructor(request: APIRequestContext) {
    this.client = new DataFactoryApiClient(request);
  }

  /**
   * Create a test ticket purchase
   *
   * @example
   * const purchase = await purchaseFactory.create({
   *   userId: user.id,
   *   ticketTypeId: ticketType.id,
   *   quantity: 2
   * });
   */
  async create(options: CreateTicketPurchaseRequest): Promise<TicketPurchaseResponse> {
    const request = {
      userId: options.userId,
      ticketTypeId: options.ticketTypeId,
      quantity: options.quantity ?? 1,
    };

    const response = await this.client.post<typeof request, TicketPurchaseResponse>(
      '/ticket-purchases',
      request
    );

    this.createdPurchases.push(response.id);
    return response;
  }

  /**
   * Create a single ticket purchase (convenience method)
   */
  async createSingle(
    userId: string,
    ticketTypeId: string
  ): Promise<TicketPurchaseResponse> {
    return this.create({
      userId,
      ticketTypeId,
      quantity: 1,
    });
  }

  /**
   * Create multiple purchases for different users
   *
   * @param userIds - Array of user IDs
   * @param ticketTypeId - Ticket type to purchase
   */
  async createForUsers(
    userIds: string[],
    ticketTypeId: string
  ): Promise<TicketPurchaseResponse[]> {
    const purchases: TicketPurchaseResponse[] = [];

    for (const userId of userIds) {
      const purchase = await this.createSingle(userId, ticketTypeId);
      purchases.push(purchase);
    }

    return purchases;
  }

  /**
   * Delete a test ticket purchase
   */
  async delete(purchaseId: string): Promise<void> {
    await this.client.delete('/ticket-purchases', purchaseId);
    this.createdPurchases = this.createdPurchases.filter((id) => id !== purchaseId);
  }

  /**
   * Get cleanup items for all created purchases
   */
  getCleanupItems(): CleanupItem[] {
    return this.createdPurchases.map((id) => ({
      type: 'ticketPurchase' as const,
      id,
    }));
  }

  /**
   * Cleanup all created purchases
   */
  async cleanupAll(): Promise<void> {
    for (const id of [...this.createdPurchases]) {
      try {
        await this.delete(id);
      } catch (error) {
        console.warn(`Failed to cleanup ticket purchase ${id}:`, error);
      }
    }
  }

  /**
   * Get count of tracked purchases
   */
  get count(): number {
    return this.createdPurchases.length;
  }
}

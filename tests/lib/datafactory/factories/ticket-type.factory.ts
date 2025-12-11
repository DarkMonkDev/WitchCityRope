/**
 * Ticket Type Factory
 *
 * Create and delete test ticket types via API.
 * REQUIRES: Backend endpoint /api/test-helpers/ticket-types
 */

import type { APIRequestContext } from '@playwright/test';
import { DataFactoryApiClient } from '../api-client';
import type {
  CreateTicketTypeRequest,
  TicketTypeResponse,
  CleanupItem,
} from '../types';

export class TicketTypeFactory {
  private client: DataFactoryApiClient;
  private createdTicketTypes: string[] = [];

  constructor(request: APIRequestContext) {
    this.client = new DataFactoryApiClient(request);
  }

  /**
   * Create a test ticket type
   *
   * @example
   * // Single session ticket
   * const ticketType = await ticketTypeFactory.create({
   *   sessionId: session.id,
   *   eventId: event.id,
   *   name: 'General Admission',
   *   price: 25
   * });
   *
   * // Multi-session ticket
   * const multiTicket = await ticketTypeFactory.create({
   *   sessionIds: [session1.id, session2.id],
   *   eventId: event.id,
   *   name: 'Both Sessions',
   *   price: 40
   * });
   */
  async create(options: CreateTicketTypeRequest): Promise<TicketTypeResponse> {
    // Convert sessionId to sessionIds array if provided
    let sessionIds = options.sessionIds;
    if (options.sessionId && !sessionIds) {
      sessionIds = [options.sessionId];
    }

    // Backend uses camelCase JSON naming policy (configured in Program.cs)
    const request = {
      eventId: options.eventId,
      sessionIds: sessionIds,
      name: options.name,
      description: options.description ?? `Ticket: ${options.name}`,
      price: options.price,
      available: options.quantityAvailable ?? 100,
      pricingType: 0, // Fixed pricing
    };

    const response = await this.client.post<typeof request, TicketTypeResponse>(
      '/ticket-types',
      request
    );

    this.createdTicketTypes.push(response.id);
    return response;
  }

  /**
   * Create a ticket type with sensible defaults
   *
   * @param sessionId - Parent session ID
   * @param eventId - Parent event ID
   * @param name - Ticket name (optional)
   * @param price - Ticket price (optional, defaults to 20)
   */
  async createDefault(
    sessionId: string,
    eventId: string,
    name?: string,
    price?: number
  ): Promise<TicketTypeResponse> {
    return this.create({
      sessionId,
      eventId,
      name: name ?? 'General Admission',
      price: price ?? 20,
    });
  }

  /**
   * Create a free ticket type
   */
  async createFree(sessionId: string, eventId: string, name?: string): Promise<TicketTypeResponse> {
    return this.create({
      sessionId,
      eventId,
      name: name ?? 'Free Admission',
      price: 0,
    });
  }

  /**
   * Create a limited availability ticket
   */
  async createLimited(
    sessionId: string,
    eventId: string,
    quantity: number,
    name?: string
  ): Promise<TicketTypeResponse> {
    return this.create({
      sessionId,
      eventId,
      name: name ?? 'Limited Ticket',
      price: 30,
      quantityAvailable: quantity,
    });
  }

  /**
   * Delete a test ticket type
   */
  async delete(ticketTypeId: string): Promise<void> {
    await this.client.delete('/ticket-types', ticketTypeId);
    this.createdTicketTypes = this.createdTicketTypes.filter(
      (id) => id !== ticketTypeId
    );
  }

  /**
   * Get cleanup items for all created ticket types
   */
  getCleanupItems(): CleanupItem[] {
    return this.createdTicketTypes.map((id) => ({
      type: 'ticketType' as const,
      id,
    }));
  }

  /**
   * Cleanup all created ticket types
   */
  async cleanupAll(): Promise<void> {
    for (const id of [...this.createdTicketTypes]) {
      try {
        await this.delete(id);
      } catch (error) {
        console.warn(`Failed to cleanup ticket type ${id}:`, error);
      }
    }
  }

  /**
   * Get count of tracked ticket types
   */
  get count(): number {
    return this.createdTicketTypes.length;
  }
}

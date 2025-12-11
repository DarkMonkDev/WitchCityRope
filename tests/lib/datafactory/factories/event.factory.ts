/**
 * Event Factory
 *
 * Create and delete test events via API.
 * REQUIRES: Backend endpoint /api/test-helpers/events
 */

import type { APIRequestContext } from '@playwright/test';
import { DataFactoryApiClient } from '../api-client';
import type { CreateEventRequest, EventResponse, CleanupItem, EventType, EventStatus } from '../types';

/**
 * Convert EventType string to backend integer
 * Backend: Class = 1, Social = 2, Performance = 3, Workshop = 4
 */
function eventTypeToInt(eventType: EventType | undefined): number {
  const map: Record<EventType, number> = {
    'Class': 1,
    'Social': 2,
    'Performance': 3,
    'Workshop': 4,
  };
  return map[eventType ?? 'Class'];
}

/**
 * Convert EventStatus string to backend integer
 * Backend: Draft = 0, Published = 1, Cancelled = 2
 */
function eventStatusToInt(status: EventStatus | undefined): number {
  const map: Record<EventStatus, number> = {
    'Draft': 0,
    'Published': 1,
    'Cancelled': 2,
  };
  return map[status ?? 'Published'];
}

export class EventFactory {
  private client: DataFactoryApiClient;
  private createdEvents: string[] = [];

  constructor(request: APIRequestContext) {
    this.client = new DataFactoryApiClient(request);
  }

  /**
   * Create a test event
   *
   * @example
   * const event = await eventFactory.create({
   *   title: 'Test Workshop',
   *   startDate: new Date(),
   *   endDate: new Date(Date.now() + 3600000)
   * });
   */
  async create(options: CreateEventRequest): Promise<EventResponse> {
    const request = {
      title: options.title,
      shortDescription:
        options.shortDescription ?? `Test event: ${options.title}`,
      description: options.description ?? `Description for ${options.title}`,
      startDate: options.startDate.toISOString(),
      endDate: options.endDate.toISOString(),
      eventType: eventTypeToInt(options.eventType),
      status: eventStatusToInt(options.status),
      isPublished: options.isPublic ?? true,
      venueId: options.venueId ? parseInt(options.venueId, 10) : 1,
    };

    const response = await this.client.post<typeof request, EventResponse>(
      '/events',
      request
    );

    this.createdEvents.push(response.id);
    return response;
  }

  /**
   * Create an event with sensible defaults for testing
   *
   * Creates an event starting tomorrow, lasting 3 hours.
   *
   * @example
   * const event = await eventFactory.createDefault('My Test Event');
   */
  async createDefault(title?: string): Promise<EventResponse> {
    const now = new Date();
    const tomorrow = new Date(now.getTime() + 24 * 60 * 60 * 1000);

    return this.create({
      title: title ?? `Test Event ${Date.now()}`,
      startDate: tomorrow,
      endDate: new Date(tomorrow.getTime() + 3 * 60 * 60 * 1000), // 3 hours
    });
  }

  /**
   * Create a published event (ready for public viewing)
   */
  async createPublished(title: string): Promise<EventResponse> {
    const now = new Date();
    const tomorrow = new Date(now.getTime() + 24 * 60 * 60 * 1000);

    return this.create({
      title,
      startDate: tomorrow,
      endDate: new Date(tomorrow.getTime() + 3 * 60 * 60 * 1000),
      status: 'Published',
      isPublic: true,
    });
  }

  /**
   * Create a draft event (not visible to public)
   */
  async createDraft(title: string): Promise<EventResponse> {
    const now = new Date();
    const tomorrow = new Date(now.getTime() + 24 * 60 * 60 * 1000);

    return this.create({
      title,
      startDate: tomorrow,
      endDate: new Date(tomorrow.getTime() + 3 * 60 * 60 * 1000),
      status: 'Draft',
      isPublic: false,
    });
  }

  /**
   * Delete a test event (also deletes related sessions/tickets)
   */
  async delete(eventId: string): Promise<void> {
    await this.client.delete('/events', eventId);
    this.createdEvents = this.createdEvents.filter((id) => id !== eventId);
  }

  /**
   * Get cleanup items for all created events
   */
  getCleanupItems(): CleanupItem[] {
    return this.createdEvents.map((id) => ({ type: 'event' as const, id }));
  }

  /**
   * Cleanup all created events
   */
  async cleanupAll(): Promise<void> {
    for (const eventId of [...this.createdEvents]) {
      try {
        await this.delete(eventId);
      } catch (error) {
        console.warn(`Failed to cleanup event ${eventId}:`, error);
      }
    }
  }

  /**
   * Get count of tracked events
   */
  get count(): number {
    return this.createdEvents.length;
  }
}

/**
 * Convenience function for quick event creation
 */
export async function createTestEvent(
  request: APIRequestContext,
  options: CreateEventRequest
): Promise<EventResponse> {
  const factory = new EventFactory(request);
  return factory.create(options);
}

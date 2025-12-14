/**
 * Volunteer Position Factory
 *
 * Create and delete test volunteer positions via API.
 * REQUIRES: Backend endpoint /api/test-helpers/volunteer-positions
 */

import type { APIRequestContext } from '@playwright/test';
import { DataFactoryApiClient } from '../api-client';
import type {
  CreateVolunteerPositionRequest,
  VolunteerPositionResponse,
  CleanupItem,
} from '../types';

export class VolunteerFactory {
  private client: DataFactoryApiClient;
  private createdPositions: string[] = [];

  constructor(request: APIRequestContext) {
    this.client = new DataFactoryApiClient(request);
  }

  /**
   * Create a test volunteer position
   *
   * @example
   * const position = await volunteerFactory.create({
   *   eventId: event.id,
   *   title: 'Door Greeter',
   *   slotsAvailable: 2
   * });
   */
  async create(options: CreateVolunteerPositionRequest): Promise<VolunteerPositionResponse> {
    // Backend uses camelCase JSON naming policy (configured in Program.cs)
    // Map frontend field names to backend DTO field names:
    // - slotsAvailable (frontend) -> slotsNeeded (backend)
    const request = {
      eventId: options.eventId,
      title: options.title,
      description: options.description ?? `Volunteer role: ${options.title}`,
      slotsNeeded: options.slotsAvailable ?? 3,
      slotsFilled: 0,
      isPublicFacing: true,
      sessionId: options.sessionId ?? null,
      startTime: options.startTime ?? '18:00',
      endTime: options.endTime ?? '21:00',
    };

    const response = await this.client.post<
      typeof request,
      VolunteerPositionResponse
    >('/volunteer-positions', request);

    this.createdPositions.push(response.id);
    return response;
  }

  /**
   * Create a volunteer position with sensible defaults
   */
  async createDefault(
    eventId: string,
    title?: string
  ): Promise<VolunteerPositionResponse> {
    return this.create({
      eventId,
      title: title ?? 'Volunteer Position',
      slotsAvailable: 3,
    });
  }

  /**
   * Create multiple volunteer positions for an event
   */
  async createMultiple(
    eventId: string,
    positions: Array<{ title: string; slots?: number }>
  ): Promise<VolunteerPositionResponse[]> {
    const results: VolunteerPositionResponse[] = [];

    for (const pos of positions) {
      const position = await this.create({
        eventId,
        title: pos.title,
        slotsAvailable: pos.slots ?? 2,
      });
      results.push(position);
    }

    return results;
  }

  /**
   * Delete a test volunteer position
   */
  async delete(positionId: string): Promise<void> {
    await this.client.delete('/volunteer-positions', positionId);
    this.createdPositions = this.createdPositions.filter(
      (id) => id !== positionId
    );
  }

  /**
   * Get cleanup items for all created positions
   */
  getCleanupItems(): CleanupItem[] {
    return this.createdPositions.map((id) => ({
      type: 'volunteerPosition' as const,
      id,
    }));
  }

  /**
   * Cleanup all created positions
   */
  async cleanupAll(): Promise<void> {
    for (const id of [...this.createdPositions]) {
      try {
        await this.delete(id);
      } catch (error) {
        console.warn(`Failed to cleanup volunteer position ${id}:`, error);
      }
    }
  }

  /**
   * Get count of tracked positions
   */
  get count(): number {
    return this.createdPositions.length;
  }
}

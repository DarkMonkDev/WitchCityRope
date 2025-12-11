/**
 * Session Factory
 *
 * Create and delete test sessions via API.
 * REQUIRES: Backend endpoint /api/test-helpers/sessions
 */

import type { APIRequestContext } from '@playwright/test';
import { DataFactoryApiClient } from '../api-client';
import type { CreateSessionRequest, SessionResponse, CleanupItem } from '../types';

export class SessionFactory {
  private client: DataFactoryApiClient;
  private createdSessions: string[] = [];

  constructor(request: APIRequestContext) {
    this.client = new DataFactoryApiClient(request);
  }

  /**
   * Create a test session for an event
   *
   * @example
   * const session = await sessionFactory.create({
   *   eventId: event.id,
   *   title: 'Morning Session',
   *   startTime: new Date(),
   *   endTime: new Date(Date.now() + 3600000)
   * });
   */
  async create(options: CreateSessionRequest): Promise<SessionResponse> {
    // Map frontend field names to backend DTO field names
    // Backend: EventId (Guid), Name (string), SessionCode (optional), StartTime, EndTime, Capacity
    // CRITICAL: SessionCode has MaxLength(10) in backend - keep it short!
    const shortCode = Math.random().toString(36).substring(2, 6).toUpperCase();
    const request = {
      eventId: options.eventId,
      name: options.title, // Backend uses 'name', frontend uses 'title'
      sessionCode: `S${shortCode}`, // 5-char code like "S4A2B" - must be <= 10 chars
      startTime: options.startTime.toISOString(),
      endTime: options.endTime.toISOString(),
      capacity: options.maxCapacity ?? 20, // Backend uses 'capacity', frontend uses 'maxCapacity'
    };

    const response = await this.client.post<typeof request, SessionResponse>(
      '/sessions',
      request
    );

    this.createdSessions.push(response.id);
    return response;
  }

  /**
   * Create a session with sensible defaults
   *
   * @param eventId - Parent event ID
   * @param title - Session title (optional, will be generated)
   */
  async createDefault(eventId: string, title?: string): Promise<SessionResponse> {
    const now = new Date();
    const sessionStart = new Date(now.getTime() + 24 * 60 * 60 * 1000); // Tomorrow

    return this.create({
      eventId,
      title: title ?? `Test Session ${Date.now()}`,
      startTime: sessionStart,
      endTime: new Date(sessionStart.getTime() + 90 * 60 * 1000), // 90 minutes
    });
  }

  /**
   * Create multiple sessions for an event
   *
   * @param eventId - Parent event ID
   * @param count - Number of sessions to create
   * @param baseStartTime - Starting time for first session (defaults to tomorrow)
   */
  async createMultiple(
    eventId: string,
    count: number,
    baseStartTime?: Date
  ): Promise<SessionResponse[]> {
    const sessions: SessionResponse[] = [];
    const startBase = baseStartTime ?? new Date(Date.now() + 24 * 60 * 60 * 1000);

    for (let i = 0; i < count; i++) {
      const sessionStart = new Date(startBase.getTime() + i * 2 * 60 * 60 * 1000);
      const session = await this.create({
        eventId,
        title: `Session ${i + 1}`,
        startTime: sessionStart,
        endTime: new Date(sessionStart.getTime() + 90 * 60 * 1000),
      });
      sessions.push(session);
    }

    return sessions;
  }

  /**
   * Delete a test session
   */
  async delete(sessionId: string): Promise<void> {
    await this.client.delete('/sessions', sessionId);
    this.createdSessions = this.createdSessions.filter((id) => id !== sessionId);
  }

  /**
   * Get cleanup items for all created sessions
   */
  getCleanupItems(): CleanupItem[] {
    return this.createdSessions.map((id) => ({ type: 'session' as const, id }));
  }

  /**
   * Cleanup all created sessions
   */
  async cleanupAll(): Promise<void> {
    for (const sessionId of [...this.createdSessions]) {
      try {
        await this.delete(sessionId);
      } catch (error) {
        console.warn(`Failed to cleanup session ${sessionId}:`, error);
      }
    }
  }

  /**
   * Get count of tracked sessions
   */
  get count(): number {
    return this.createdSessions.length;
  }
}

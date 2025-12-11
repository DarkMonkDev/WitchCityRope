/**
 * DataFactory - Test Data Infrastructure for E2E Tests
 *
 * =====================================================
 * AI AGENTS: START HERE
 * =====================================================
 *
 * This is the SINGLE SOURCE OF TRUTH for test data creation.
 * Use these factories instead of:
 * - database-helpers.ts (deprecated)
 * - Direct database access
 * - Hardcoded seed data
 *
 * QUICK START:
 *
 * ```typescript
 * import { DataFactory } from '@tests/lib/datafactory';
 *
 * test('my test', async ({ request }) => {
 *   const df = new DataFactory(request);
 *
 *   // Create test data
 *   const user = await df.users.createVerified({ email: 'test@example.com' });
 *   const event = await df.events.createDefault('My Event');
 *
 *   // ... run test ...
 *
 *   // Cleanup (or use df.cleanupAll())
 *   await df.events.delete(event.id);
 *   await df.users.delete(user.id);
 * });
 * ```
 *
 * =====================================================
 */

import type { APIRequestContext } from '@playwright/test';

// Factory imports
import { UserFactory } from './factories/user.factory';
import { EventFactory } from './factories/event.factory';
import { SessionFactory } from './factories/session.factory';
import { TicketTypeFactory } from './factories/ticket-type.factory';
import { TicketPurchaseFactory } from './factories/ticket-purchase.factory';
import { VolunteerFactory } from './factories/volunteer.factory';
import { VettingFactory } from './factories/vetting.factory';

// API client
import { DataFactoryApiClient } from './api-client';

// Re-export types
export * from './types';

// Re-export factories
export * from './factories';

// Re-export scenarios
export * from './scenarios';

// Re-export API client utilities
export {
  DataFactoryApiClient,
  setGlobalClient,
  getGlobalClient,
  clearGlobalClient,
} from './api-client';

/**
 * Main DataFactory class
 *
 * Provides access to all entity factories and handles cleanup.
 *
 * @example
 * const df = new DataFactory(request);
 *
 * // Create data
 * const user = await df.users.createVerified({ email: 'test@example.com' });
 * const event = await df.events.createDefault('Test Event');
 *
 * // Cleanup all at once
 * await df.cleanupAll();
 */
export class DataFactory {
  public readonly users: UserFactory;
  public readonly events: EventFactory;
  public readonly sessions: SessionFactory;
  public readonly ticketTypes: TicketTypeFactory;
  public readonly ticketPurchases: TicketPurchaseFactory;
  public readonly volunteers: VolunteerFactory;
  public readonly vetting: VettingFactory;

  private readonly client: DataFactoryApiClient;

  constructor(request: APIRequestContext) {
    this.client = new DataFactoryApiClient(request);
    this.users = new UserFactory(request);
    this.events = new EventFactory(request);
    this.sessions = new SessionFactory(request);
    this.ticketTypes = new TicketTypeFactory(request);
    this.ticketPurchases = new TicketPurchaseFactory(request);
    this.volunteers = new VolunteerFactory(request);
    this.vetting = new VettingFactory(request);
  }

  /**
   * Check if test helper endpoints are available
   */
  async healthCheck(): Promise<boolean> {
    return this.client.healthCheck();
  }

  /**
   * Ensure test helpers are available (throws if not)
   */
  async ensureAvailable(): Promise<void> {
    return this.client.ensureAvailable();
  }

  /**
   * Cleanup all created test data
   *
   * Call in afterEach or afterAll hooks.
   * Order matters: deletes in reverse dependency order.
   *
   * @example
   * test.afterEach(async ({ request }) => {
   *   await df.cleanupAll();
   * });
   */
  async cleanupAll(): Promise<void> {
    // Order: most dependent first, least dependent last
    // This ensures we don't hit foreign key constraints
    await this.ticketPurchases.cleanupAll();
    await this.ticketTypes.cleanupAll();
    await this.sessions.cleanupAll();
    await this.volunteers.cleanupAll();
    await this.vetting.cleanupAll();
    await this.events.cleanupAll();
    await this.users.cleanupAll();
  }

  /**
   * Get summary of all tracked entities
   */
  getSummary(): Record<string, number> {
    return {
      users: this.users.count,
      events: this.events.count,
      sessions: this.sessions.count,
      ticketTypes: this.ticketTypes.count,
      ticketPurchases: this.ticketPurchases.count,
      volunteerPositions: this.volunteers.count,
      vettingApplications: this.vetting.count,
    };
  }
}

// Default export for convenience
export default DataFactory;

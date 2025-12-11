# DataFactory TypeScript Implementation Instructions

**Date**: 2025-12-10
**Priority**: HIGH - After backend endpoints complete

## Overview

Create a centralized DataFactory in `/tests/lib/datafactory/` that provides a single, discoverable location for all test data creation. This replaces the scattered and duplicated helpers currently in the codebase.

## Prerequisites

- Backend test helper endpoints MUST be implemented first
- See: `backend-developer-instructions.md`

## Directory Structure to Create

```
/tests/lib/
└── datafactory/
    ├── index.ts                    # Main export - AI agents look here first
    ├── types.ts                    # All TypeScript types
    ├── api-client.ts               # HTTP client for test helper API
    ├── factories/
    │   ├── index.ts                # Re-export all factories
    │   ├── user.factory.ts         # User operations
    │   ├── event.factory.ts        # Event operations
    │   ├── session.factory.ts      # Session operations
    │   ├── ticket-type.factory.ts  # Ticket type operations
    │   ├── ticket-purchase.factory.ts  # Ticket purchase operations
    │   ├── volunteer.factory.ts    # Volunteer position operations
    │   └── vetting.factory.ts      # Vetting application operations
    ├── scenarios/
    │   ├── index.ts                # Re-export all scenarios
    │   └── complete-event.scenario.ts  # Event with full setup
    └── README.md                   # Documentation for AI discoverability
```

## Implementation Files

### 1. `/tests/lib/datafactory/types.ts`

```typescript
/**
 * DataFactory Types
 *
 * All types for test data creation/deletion.
 * These types mirror the backend TestHelper DTOs.
 */

// ============================================
// USER TYPES
// ============================================

export interface CreateUserRequest {
  email: string;
  password?: string;  // Defaults to Test123!
  firstName?: string;
  lastName?: string;
  roles?: string[];   // e.g., ['Admin', 'VettedMember']
}

export interface UserResponse {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  roles: string[];
}

// ============================================
// EVENT TYPES
// ============================================

export interface CreateEventRequest {
  title: string;
  shortDescription?: string;
  description?: string;
  startDate: Date;
  endDate: Date;
  eventType?: 'Class' | 'Social' | 'Performance' | 'Workshop';
  status?: 'Draft' | 'Published' | 'Cancelled';
  isPublic?: boolean;
  venueId?: string;
}

export interface EventResponse {
  id: string;
  title: string;
  startDate: string;
  endDate: string;
  status: string;
}

// ============================================
// SESSION TYPES
// ============================================

export interface CreateSessionRequest {
  eventId: string;
  title: string;
  description?: string;
  startTime: Date;
  endTime: Date;
  maxCapacity?: number;
  requiresRegistration?: boolean;
}

export interface SessionResponse {
  id: string;
  eventId: string;
  title: string;
  startTime: string;
  endTime: string;
}

// ============================================
// TICKET TYPE TYPES
// ============================================

export interface CreateTicketTypeRequest {
  sessionId: string;
  name: string;
  description?: string;
  price: number;
  quantityAvailable?: number;
  isActive?: boolean;
  salesStartDate?: Date;
  salesEndDate?: Date;
}

export interface TicketTypeResponse {
  id: string;
  sessionId: string;
  name: string;
  price: number;
  quantityAvailable: number;
}

// ============================================
// TICKET PURCHASE TYPES
// ============================================

export interface CreateTicketPurchaseRequest {
  userId: string;
  ticketTypeId: string;
  quantity?: number;
}

export interface TicketPurchaseResponse {
  id: string;
  userId: string;
  ticketTypeId: string;
  quantity: number;
  purchaseDate: string;
}

// ============================================
// VOLUNTEER TYPES
// ============================================

export interface CreateVolunteerPositionRequest {
  eventId: string;
  title: string;
  description?: string;
  slotsAvailable?: number;
  startTime?: Date;
  endTime?: Date;
}

export interface VolunteerPositionResponse {
  id: string;
  eventId: string;
  title: string;
  slotsAvailable: number;
}

// ============================================
// VETTING TYPES
// ============================================

export interface CreateVettingApplicationRequest {
  userId: string;
  status?: 'Pending' | 'InReview' | 'Approved' | 'Rejected';
  notes?: string;
}

export interface VettingApplicationResponse {
  id: string;
  userId: string;
  status: string;
  createdAt: string;
}

// ============================================
// CLEANUP TRACKING
// ============================================

export interface CleanupItem {
  type: 'user' | 'event' | 'session' | 'ticketType' | 'ticketPurchase' | 'volunteerPosition' | 'vettingApplication';
  id: string;
}

export interface TestContext {
  cleanupItems: CleanupItem[];
  addCleanup: (item: CleanupItem) => void;
  cleanup: () => Promise<void>;
}
```

### 2. `/tests/lib/datafactory/api-client.ts`

```typescript
/**
 * DataFactory API Client
 *
 * HTTP client for test helper endpoints.
 * Handles all communication with /api/test-helpers/*
 */

import { APIRequestContext } from '@playwright/test';

const TEST_HELPERS_BASE = '/api/test-helpers';

export class DataFactoryApiClient {
  constructor(private request: APIRequestContext) {}

  // ============================================
  // GENERIC METHODS
  // ============================================

  async post<TRequest, TResponse>(
    endpoint: string,
    data: TRequest
  ): Promise<TResponse> {
    const response = await this.request.post(
      `${TEST_HELPERS_BASE}${endpoint}`,
      { data }
    );

    if (!response.ok()) {
      const error = await response.text();
      throw new Error(`DataFactory API error: ${endpoint} - ${error}`);
    }

    return response.json();
  }

  async delete(endpoint: string, id: string): Promise<void> {
    const response = await this.request.delete(
      `${TEST_HELPERS_BASE}${endpoint}/${id}`
    );

    if (!response.ok() && response.status() !== 404) {
      const error = await response.text();
      throw new Error(`DataFactory API error: DELETE ${endpoint}/${id} - ${error}`);
    }
  }

  // ============================================
  // HEALTH CHECK
  // ============================================

  async healthCheck(): Promise<boolean> {
    try {
      const response = await this.request.get(`${TEST_HELPERS_BASE}/health`);
      return response.ok();
    } catch {
      return false;
    }
  }
}

// Singleton instance for non-Playwright usage
let globalClient: DataFactoryApiClient | null = null;

export function setGlobalClient(request: APIRequestContext): void {
  globalClient = new DataFactoryApiClient(request);
}

export function getGlobalClient(): DataFactoryApiClient {
  if (!globalClient) {
    throw new Error('DataFactory API client not initialized. Call setGlobalClient first.');
  }
  return globalClient;
}
```

### 3. `/tests/lib/datafactory/factories/user.factory.ts`

```typescript
/**
 * User Factory
 *
 * Create and delete test users via API.
 * Uses existing /api/test-helpers/users endpoint.
 */

import { APIRequestContext } from '@playwright/test';
import { DataFactoryApiClient } from '../api-client';
import type { CreateUserRequest, UserResponse, CleanupItem } from '../types';

export class UserFactory {
  private client: DataFactoryApiClient;
  private createdUsers: string[] = [];

  constructor(request: APIRequestContext) {
    this.client = new DataFactoryApiClient(request);
  }

  /**
   * Create a test user
   *
   * @example
   * const user = await userFactory.create({
   *   email: 'test@example.com',
   *   roles: ['VettedMember']
   * });
   */
  async create(options: CreateUserRequest): Promise<UserResponse> {
    const request = {
      email: options.email,
      password: options.password ?? 'Test123!',
      firstName: options.firstName ?? 'Test',
      lastName: options.lastName ?? 'User',
      roles: options.roles ?? []
    };

    const response = await this.client.post<typeof request, UserResponse>(
      '/users',
      request
    );

    this.createdUsers.push(response.id);
    return response;
  }

  /**
   * Create a user with verified email
   */
  async createVerified(options: CreateUserRequest): Promise<UserResponse> {
    const user = await this.create(options);
    await this.verifyEmail(options.email);
    return user;
  }

  /**
   * Verify a user's email address
   */
  async verifyEmail(email: string): Promise<void> {
    await this.client.post('/verify-email', { email });
  }

  /**
   * Delete a test user
   */
  async delete(userId: string): Promise<void> {
    await this.client.delete('/users', userId);
    this.createdUsers = this.createdUsers.filter(id => id !== userId);
  }

  /**
   * Get cleanup items for all created users
   */
  getCleanupItems(): CleanupItem[] {
    return this.createdUsers.map(id => ({ type: 'user', id }));
  }

  /**
   * Cleanup all created users
   */
  async cleanupAll(): Promise<void> {
    for (const userId of [...this.createdUsers]) {
      try {
        await this.delete(userId);
      } catch (error) {
        console.warn(`Failed to cleanup user ${userId}:`, error);
      }
    }
  }
}

// Convenience function for quick user creation
export async function createTestUser(
  request: APIRequestContext,
  options: Partial<CreateUserRequest> & { email: string }
): Promise<UserResponse> {
  const factory = new UserFactory(request);
  return factory.create(options);
}
```

### 4. `/tests/lib/datafactory/factories/event.factory.ts`

```typescript
/**
 * Event Factory
 *
 * Create and delete test events via API.
 * REQUIRES: Backend endpoint implementation
 */

import { APIRequestContext } from '@playwright/test';
import { DataFactoryApiClient } from '../api-client';
import type { CreateEventRequest, EventResponse, CleanupItem } from '../types';

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
      shortDescription: options.shortDescription ?? `Test event: ${options.title}`,
      description: options.description ?? `Description for ${options.title}`,
      startDate: options.startDate.toISOString(),
      endDate: options.endDate.toISOString(),
      eventType: options.eventType ?? 'Class',
      status: options.status ?? 'Published',
      isPublic: options.isPublic ?? true,
      venueId: options.venueId
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
   */
  async createDefault(title?: string): Promise<EventResponse> {
    const now = new Date();
    const tomorrow = new Date(now.getTime() + 24 * 60 * 60 * 1000);

    return this.create({
      title: title ?? `Test Event ${Date.now()}`,
      startDate: tomorrow,
      endDate: new Date(tomorrow.getTime() + 3 * 60 * 60 * 1000) // 3 hours
    });
  }

  /**
   * Delete a test event (also deletes related sessions/tickets)
   */
  async delete(eventId: string): Promise<void> {
    await this.client.delete('/events', eventId);
    this.createdEvents = this.createdEvents.filter(id => id !== eventId);
  }

  /**
   * Get cleanup items for all created events
   */
  getCleanupItems(): CleanupItem[] {
    return this.createdEvents.map(id => ({ type: 'event', id }));
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
}
```

### 5. `/tests/lib/datafactory/factories/session.factory.ts`

```typescript
/**
 * Session Factory
 *
 * Create and delete test sessions via API.
 * REQUIRES: Backend endpoint implementation
 */

import { APIRequestContext } from '@playwright/test';
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
    const request = {
      eventId: options.eventId,
      title: options.title,
      description: options.description ?? `Session: ${options.title}`,
      startTime: options.startTime.toISOString(),
      endTime: options.endTime.toISOString(),
      maxCapacity: options.maxCapacity ?? 20,
      requiresRegistration: options.requiresRegistration ?? true
    };

    const response = await this.client.post<typeof request, SessionResponse>(
      '/sessions',
      request
    );

    this.createdSessions.push(response.id);
    return response;
  }

  /**
   * Delete a test session
   */
  async delete(sessionId: string): Promise<void> {
    await this.client.delete('/sessions', sessionId);
    this.createdSessions = this.createdSessions.filter(id => id !== sessionId);
  }

  /**
   * Get cleanup items for all created sessions
   */
  getCleanupItems(): CleanupItem[] {
    return this.createdSessions.map(id => ({ type: 'session', id }));
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
}
```

### 6. `/tests/lib/datafactory/factories/ticket-type.factory.ts`

```typescript
/**
 * Ticket Type Factory
 *
 * Create and delete test ticket types via API.
 */

import { APIRequestContext } from '@playwright/test';
import { DataFactoryApiClient } from '../api-client';
import type { CreateTicketTypeRequest, TicketTypeResponse, CleanupItem } from '../types';

export class TicketTypeFactory {
  private client: DataFactoryApiClient;
  private createdTicketTypes: string[] = [];

  constructor(request: APIRequestContext) {
    this.client = new DataFactoryApiClient(request);
  }

  async create(options: CreateTicketTypeRequest): Promise<TicketTypeResponse> {
    const request = {
      sessionId: options.sessionId,
      name: options.name,
      description: options.description ?? `Ticket: ${options.name}`,
      price: options.price,
      quantityAvailable: options.quantityAvailable ?? 100,
      isActive: options.isActive ?? true,
      salesStartDate: options.salesStartDate?.toISOString(),
      salesEndDate: options.salesEndDate?.toISOString()
    };

    const response = await this.client.post<typeof request, TicketTypeResponse>(
      '/ticket-types',
      request
    );

    this.createdTicketTypes.push(response.id);
    return response;
  }

  async delete(ticketTypeId: string): Promise<void> {
    await this.client.delete('/ticket-types', ticketTypeId);
    this.createdTicketTypes = this.createdTicketTypes.filter(id => id !== ticketTypeId);
  }

  getCleanupItems(): CleanupItem[] {
    return this.createdTicketTypes.map(id => ({ type: 'ticketType', id }));
  }

  async cleanupAll(): Promise<void> {
    for (const id of [...this.createdTicketTypes]) {
      try {
        await this.delete(id);
      } catch (error) {
        console.warn(`Failed to cleanup ticket type ${id}:`, error);
      }
    }
  }
}
```

### 7. `/tests/lib/datafactory/factories/index.ts`

```typescript
/**
 * Factory Exports
 *
 * Re-export all factories for convenient importing.
 */

export { UserFactory, createTestUser } from './user.factory';
export { EventFactory } from './event.factory';
export { SessionFactory } from './session.factory';
export { TicketTypeFactory } from './ticket-type.factory';
export { TicketPurchaseFactory } from './ticket-purchase.factory';
export { VolunteerFactory } from './volunteer.factory';
export { VettingFactory } from './vetting.factory';
```

### 8. `/tests/lib/datafactory/scenarios/complete-event.scenario.ts`

```typescript
/**
 * Complete Event Scenario
 *
 * Creates a fully-configured event with sessions, ticket types, etc.
 * Use for tests that need a complete event setup.
 */

import { APIRequestContext } from '@playwright/test';
import { EventFactory } from '../factories/event.factory';
import { SessionFactory } from '../factories/session.factory';
import { TicketTypeFactory } from '../factories/ticket-type.factory';
import type { EventResponse, SessionResponse, TicketTypeResponse } from '../types';

export interface CompleteEventData {
  event: EventResponse;
  sessions: SessionResponse[];
  ticketTypes: TicketTypeResponse[];
}

export interface CompleteEventOptions {
  title?: string;
  sessionCount?: number;
  ticketPrice?: number;
  ticketsPerSession?: number;
}

/**
 * Create a complete event with sessions and ticket types
 *
 * @example
 * const { event, sessions, ticketTypes } = await createCompleteEvent(request, {
 *   title: 'Workshop',
 *   sessionCount: 2,
 *   ticketPrice: 25
 * });
 */
export async function createCompleteEvent(
  request: APIRequestContext,
  options: CompleteEventOptions = {}
): Promise<CompleteEventData> {
  const eventFactory = new EventFactory(request);
  const sessionFactory = new SessionFactory(request);
  const ticketTypeFactory = new TicketTypeFactory(request);

  // Create event
  const now = new Date();
  const tomorrow = new Date(now.getTime() + 24 * 60 * 60 * 1000);

  const event = await eventFactory.create({
    title: options.title ?? `Complete Event ${Date.now()}`,
    startDate: tomorrow,
    endDate: new Date(tomorrow.getTime() + 8 * 60 * 60 * 1000) // 8 hours
  });

  // Create sessions
  const sessionCount = options.sessionCount ?? 1;
  const sessions: SessionResponse[] = [];

  for (let i = 0; i < sessionCount; i++) {
    const sessionStart = new Date(tomorrow.getTime() + i * 2 * 60 * 60 * 1000);
    const session = await sessionFactory.create({
      eventId: event.id,
      title: `Session ${i + 1}`,
      startTime: sessionStart,
      endTime: new Date(sessionStart.getTime() + 90 * 60 * 1000) // 90 minutes
    });
    sessions.push(session);
  }

  // Create ticket types for each session
  const ticketTypes: TicketTypeResponse[] = [];

  for (const session of sessions) {
    const ticketType = await ticketTypeFactory.create({
      sessionId: session.id,
      name: `General Admission - ${session.title}`,
      price: options.ticketPrice ?? 20,
      quantityAvailable: options.ticketsPerSession ?? 50
    });
    ticketTypes.push(ticketType);
  }

  return { event, sessions, ticketTypes };
}

/**
 * Cleanup a complete event and all related data
 */
export async function cleanupCompleteEvent(
  request: APIRequestContext,
  data: CompleteEventData
): Promise<void> {
  const eventFactory = new EventFactory(request);

  // Deleting the event should cascade delete sessions and ticket types
  await eventFactory.delete(data.event.id);
}
```

### 9. `/tests/lib/datafactory/index.ts`

```typescript
/**
 * DataFactory - Test Data Creation for E2E Tests
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
 *
 * =====================================================
 */

import { APIRequestContext } from '@playwright/test';

// Factory imports
import { UserFactory } from './factories/user.factory';
import { EventFactory } from './factories/event.factory';
import { SessionFactory } from './factories/session.factory';
import { TicketTypeFactory } from './factories/ticket-type.factory';
import { TicketPurchaseFactory } from './factories/ticket-purchase.factory';
import { VolunteerFactory } from './factories/volunteer.factory';
import { VettingFactory } from './factories/vetting.factory';

// Re-export types
export * from './types';

// Re-export factories
export * from './factories';

// Re-export scenarios
export * from './scenarios/complete-event.scenario';

/**
 * Main DataFactory class
 *
 * Provides access to all entity factories and handles cleanup.
 */
export class DataFactory {
  public readonly users: UserFactory;
  public readonly events: EventFactory;
  public readonly sessions: SessionFactory;
  public readonly ticketTypes: TicketTypeFactory;
  public readonly ticketPurchases: TicketPurchaseFactory;
  public readonly volunteers: VolunteerFactory;
  public readonly vetting: VettingFactory;

  constructor(request: APIRequestContext) {
    this.users = new UserFactory(request);
    this.events = new EventFactory(request);
    this.sessions = new SessionFactory(request);
    this.ticketTypes = new TicketTypeFactory(request);
    this.ticketPurchases = new TicketPurchaseFactory(request);
    this.volunteers = new VolunteerFactory(request);
    this.vetting = new VettingFactory(request);
  }

  /**
   * Cleanup all created test data
   *
   * Call in afterEach or afterAll hooks.
   * Order matters: deletes in reverse dependency order.
   */
  async cleanupAll(): Promise<void> {
    // Order: most dependent first, least dependent last
    await this.ticketPurchases.cleanupAll();
    await this.ticketTypes.cleanupAll();
    await this.sessions.cleanupAll();
    await this.volunteers.cleanupAll();
    await this.vetting.cleanupAll();
    await this.events.cleanupAll();
    await this.users.cleanupAll();
  }
}

// Default export for convenience
export default DataFactory;
```

### 10. `/tests/lib/datafactory/README.md`

```markdown
# DataFactory - Test Data Infrastructure

## Overview

DataFactory provides a centralized, type-safe way to create test data for E2E tests.

**AI AGENTS**: This is the SINGLE SOURCE OF TRUTH for test data creation.

## Quick Start

\`\`\`typescript
import { DataFactory } from '@tests/lib/datafactory';

test('example test', async ({ request }) => {
  const df = new DataFactory(request);

  // Create a verified user
  const user = await df.users.createVerified({
    email: 'test@example.com',
    roles: ['VettedMember']
  });

  // Create an event with sessions and tickets
  const event = await df.events.createDefault('My Event');

  // Cleanup when done
  await df.cleanupAll();
});
\`\`\`

## Available Factories

| Factory | Description |
|---------|-------------|
| `df.users` | Create/delete test users |
| `df.events` | Create/delete events |
| `df.sessions` | Create/delete sessions |
| `df.ticketTypes` | Create/delete ticket types |
| `df.ticketPurchases` | Create/delete ticket purchases |
| `df.volunteers` | Create/delete volunteer positions |
| `df.vetting` | Create/delete vetting applications |

## Scenarios

For complex test setups, use scenarios:

\`\`\`typescript
import { createCompleteEvent } from '@tests/lib/datafactory';

const { event, sessions, ticketTypes } = await createCompleteEvent(request, {
  title: 'Workshop',
  sessionCount: 2,
  ticketPrice: 25
});
\`\`\`

## Best Practices

1. **Always cleanup**: Use `df.cleanupAll()` in afterEach/afterAll
2. **Create fresh data**: Each test should create its own data
3. **Don't rely on seed data**: Use factories, not hardcoded IDs
4. **Use scenarios**: For complex setups, use pre-built scenarios

## API Reference

### UserFactory

\`\`\`typescript
// Create user
const user = await df.users.create({ email: 'test@example.com' });

// Create verified user
const user = await df.users.createVerified({ email: 'test@example.com' });

// Delete user
await df.users.delete(user.id);
\`\`\`

### EventFactory

\`\`\`typescript
// Create event with full options
const event = await df.events.create({
  title: 'My Event',
  startDate: new Date(),
  endDate: new Date(Date.now() + 3600000)
});

// Create event with defaults
const event = await df.events.createDefault('Quick Test Event');

// Delete event (cascades to sessions/tickets)
await df.events.delete(event.id);
\`\`\`

### SessionFactory

\`\`\`typescript
const session = await df.sessions.create({
  eventId: event.id,
  title: 'Morning Session',
  startTime: new Date(),
  endTime: new Date(Date.now() + 3600000)
});
\`\`\`

## Migration from database-helpers.ts

**OLD (deprecated)**:
\`\`\`typescript
import { createTestUser } from '../utils/database-helpers';
const user = await createTestUser(pool, { ... });
\`\`\`

**NEW**:
\`\`\`typescript
import { DataFactory } from '@tests/lib/datafactory';
const df = new DataFactory(request);
const user = await df.users.create({ ... });
\`\`\`
\`\`\`
```

## Implementation Steps

1. Create directory structure: `mkdir -p tests/lib/datafactory/factories tests/lib/datafactory/scenarios`
2. Create `types.ts` first (no dependencies)
3. Create `api-client.ts` (depends on types)
4. Create each factory file in `factories/`
5. Create `factories/index.ts` to re-export
6. Create scenario files in `scenarios/`
7. Create main `index.ts`
8. Create `README.md`

## Remaining Factory Files

Create these following the same pattern as user.factory.ts:

- `ticket-purchase.factory.ts` - Follows existing pattern
- `volunteer.factory.ts` - Follows existing pattern
- `vetting.factory.ts` - Follows existing pattern

## Path Aliases

Add to `tsconfig.json` (if not exists):

```json
{
  "compilerOptions": {
    "paths": {
      "@tests/*": ["./tests/*"]
    }
  }
}
```

## Verification Checklist

After implementation:

- [ ] All factory files compile without errors
- [ ] Types match backend DTOs
- [ ] API client connects to test helpers
- [ ] Each factory can create and delete entities
- [ ] cleanupAll() works correctly
- [ ] README is complete and accurate
- [ ] Path aliases work for imports

## Dependencies

This implementation depends on:
1. Backend test helper endpoints (see `backend-developer-instructions.md`)
2. Playwright test framework
3. TypeScript

## Files to Create

| File | Priority |
|------|----------|
| `types.ts` | First |
| `api-client.ts` | Second |
| `factories/user.factory.ts` | Third |
| `factories/event.factory.ts` | Third |
| `factories/session.factory.ts` | Third |
| `factories/ticket-type.factory.ts` | Third |
| `factories/ticket-purchase.factory.ts` | Third |
| `factories/volunteer.factory.ts` | Third |
| `factories/vetting.factory.ts` | Third |
| `factories/index.ts` | Fourth |
| `scenarios/complete-event.scenario.ts` | Fifth |
| `index.ts` | Sixth |
| `README.md` | Last |

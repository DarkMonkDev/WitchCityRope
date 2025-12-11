# DataFactory - Test Data Infrastructure

## Overview

DataFactory provides a centralized, type-safe way to create test data for E2E tests.

**AI AGENTS**: This is the SINGLE SOURCE OF TRUTH for test data creation.

## Quick Start

```typescript
import { DataFactory } from '@tests/lib/datafactory';

test('example test', async ({ request }) => {
  const df = new DataFactory(request);

  // Create a verified user
  const user = await df.users.createVerified({
    email: 'test@example.com',
    roles: ['VettedMember'],
  });

  // Create an event with sessions and tickets
  const event = await df.events.createDefault('My Event');

  // Cleanup when done
  await df.cleanupAll();
});
```

## Available Factories

| Factory              | Description                         |
| -------------------- | ----------------------------------- |
| `df.users`           | Create/delete test users            |
| `df.events`          | Create/delete events                |
| `df.sessions`        | Create/delete sessions              |
| `df.ticketTypes`     | Create/delete ticket types          |
| `df.ticketPurchases` | Create/delete ticket purchases      |
| `df.volunteers`      | Create/delete volunteer positions   |
| `df.vetting`         | Create/delete vetting applications  |

## Scenarios

For complex test setups, use scenarios:

```typescript
import { createCompleteEvent } from '@tests/lib/datafactory';

const { event, sessions, ticketTypes } = await createCompleteEvent(request, {
  title: 'Workshop',
  sessionCount: 2,
  ticketPrice: 25,
});
```

### Available Scenarios

- `createCompleteEvent` - Event with sessions and ticket types
- `createTicketedEvent` - Simple event ready for purchase testing
- `createWorkshopEvent` - Multi-session workshop event

## Best Practices

1. **Always cleanup**: Use `df.cleanupAll()` in afterEach/afterAll
2. **Create fresh data**: Each test should create its own data
3. **Don't rely on seed data**: Use factories, not hardcoded IDs
4. **Use scenarios**: For complex setups, use pre-built scenarios

## API Reference

### DataFactory

```typescript
const df = new DataFactory(request);

// Health check
const isAvailable = await df.healthCheck();

// Get summary of tracked entities
const summary = df.getSummary();
// { users: 2, events: 1, sessions: 3, ... }

// Cleanup all created data
await df.cleanupAll();
```

### UserFactory

```typescript
// Create user with options
const user = await df.users.create({
  email: 'test@example.com',
  password: 'Test123!', // optional, defaults to Test123!
  firstName: 'Test',    // optional
  lastName: 'User',     // optional
  roles: ['Admin'],     // optional
});

// Create verified user (email confirmed)
const user = await df.users.createVerified({
  email: 'verified@example.com',
});

// Create user with specific role
const admin = await df.users.createWithRole('admin@test.com', 'Admin');

// Verify email separately
await df.users.verifyEmail('test@example.com');

// Delete user
await df.users.delete(user.id);
```

### EventFactory

```typescript
// Create event with full options
const event = await df.events.create({
  title: 'My Event',
  startDate: new Date(),
  endDate: new Date(Date.now() + 3600000),
  eventType: 'Workshop',  // optional: 'Class' | 'Social' | 'Performance' | 'Workshop'
  status: 'Published',    // optional: 'Draft' | 'Published' | 'Cancelled'
  isPublic: true,         // optional
});

// Create event with defaults (starts tomorrow, 3 hours)
const event = await df.events.createDefault('Quick Test Event');

// Create published event
const event = await df.events.createPublished('Public Event');

// Create draft event
const event = await df.events.createDraft('Draft Event');

// Delete event (cascades to sessions/tickets)
await df.events.delete(event.id);
```

### SessionFactory

```typescript
// Create session
const session = await df.sessions.create({
  eventId: event.id,
  title: 'Morning Session',
  startTime: new Date(),
  endTime: new Date(Date.now() + 3600000),
  maxCapacity: 20,              // optional
  requiresRegistration: true,   // optional
});

// Create with defaults
const session = await df.sessions.createDefault(event.id, 'Session Title');

// Create multiple sessions
const sessions = await df.sessions.createMultiple(event.id, 3);
```

### TicketTypeFactory

```typescript
// Create ticket type
const ticketType = await df.ticketTypes.create({
  sessionId: session.id,
  name: 'General Admission',
  price: 25,
  quantityAvailable: 100,  // optional
  isActive: true,          // optional
});

// Create with defaults
const ticketType = await df.ticketTypes.createDefault(session.id);

// Create free ticket
const ticketType = await df.ticketTypes.createFree(session.id);

// Create limited availability ticket
const ticketType = await df.ticketTypes.createLimited(session.id, 10);
```

### TicketPurchaseFactory

```typescript
// Create purchase
const purchase = await df.ticketPurchases.create({
  userId: user.id,
  ticketTypeId: ticketType.id,
  quantity: 2,  // optional, defaults to 1
});

// Create single ticket purchase
const purchase = await df.ticketPurchases.createSingle(user.id, ticketType.id);

// Create purchases for multiple users
const purchases = await df.ticketPurchases.createForUsers(
  [user1.id, user2.id],
  ticketType.id
);
```

### VolunteerFactory

```typescript
// Create volunteer position
const position = await df.volunteers.create({
  eventId: event.id,
  title: 'Door Greeter',
  slotsAvailable: 2,  // optional
});

// Create with defaults
const position = await df.volunteers.createDefault(event.id);

// Create multiple positions
const positions = await df.volunteers.createMultiple(event.id, [
  { title: 'Door Greeter', slots: 2 },
  { title: 'Setup Helper', slots: 3 },
]);
```

### VettingFactory

```typescript
// Create vetting application
const application = await df.vetting.create({
  userId: user.id,
  status: 'Pending',  // optional: 'Pending' | 'InReview' | 'Approved' | 'Rejected'
});

// Create pending application
const application = await df.vetting.createPending(user.id);

// Create approved application
const application = await df.vetting.createApproved(user.id);

// Create with specific status
const application = await df.vetting.createWithStatus(user.id, 'InReview');
```

## Migration from database-helpers.ts

The old `database-helpers.ts` files are deprecated. Migrate to DataFactory:

**OLD (deprecated)**:

```typescript
import { createTestUser } from '../utils/database-helpers';
const user = await createTestUser(pool, { ... });
```

**NEW**:

```typescript
import { DataFactory } from '@tests/lib/datafactory';
const df = new DataFactory(request);
const user = await df.users.create({ ... });
```

## Troubleshooting

### "Test helper endpoints are not available"

Ensure:
1. API is running in Development or Test environment
2. Test containers are started: `docker compose up -d`
3. Health check passes: `curl http://localhost:5655/api/test-helpers/health`

### Cleanup failures

If cleanup fails, check:
1. Entity still exists (may have been deleted by cascade)
2. Foreign key constraints (cleanup order matters)
3. API is still running

The `cleanupAll()` method handles these gracefully with warnings.

## Architecture

```
/tests/lib/datafactory/
├── index.ts           # Main exports and DataFactory class
├── types.ts           # TypeScript types
├── api-client.ts      # HTTP client for test helper API
├── factories/
│   ├── index.ts       # Factory exports
│   ├── user.factory.ts
│   ├── event.factory.ts
│   ├── session.factory.ts
│   ├── ticket-type.factory.ts
│   ├── ticket-purchase.factory.ts
│   ├── volunteer.factory.ts
│   └── vetting.factory.ts
├── scenarios/
│   ├── index.ts       # Scenario exports
│   └── complete-event.scenario.ts
└── README.md          # This file
```

# TypeScript Patterns

**Purpose**: TypeScript type safety patterns, DTO usage, and type guard implementations for WitchCityRope.
**When to Read**: When working with TypeScript types, DTOs, or fixing type errors.
**Related**: [React Patterns](./react-patterns.md), [DTO Alignment Strategy](/docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md)

## 🚨 CRITICAL: DTO Alignment

**MANDATORY**: Read [DTO Alignment Strategy](/docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md) before working with API types.

**KEY RULES**:
- ✅ **ALWAYS** use auto-generated types from `@witchcityrope/shared-types`
- ❌ **NEVER** create manual interfaces for API data
- ❌ **NEVER** add field name mappings or convenience aliases
- ✅ **ALWAYS** import: `import type { components } from '@witchcityrope/shared-types'`

## Auto-Generated Type Usage

### Correct Pattern
```typescript
// ✅ CORRECT: Use auto-generated types
import type { components } from '@witchcityrope/shared-types';

export type EventDto = components['schemas']['EventDto'];
export type SessionDto = components['schemas']['SessionDto'];
export type RegistrationDto = components['schemas']['RegistrationDto'];

// Use in component
interface Props {
  event: EventDto;
  sessions: SessionDto[];
}
```

### Wrong Pattern
```typescript
// ❌ WRONG: Manual interface duplicates auto-generated type
interface Event {
  id: number;
  name: string;
  registeredCount: number;  // Backend actually uses registrationCount!
}

// This creates field name mismatches and bugs
```

## Type Guards

### Problem
Runtime type checking for API responses and user input.

### Pattern
```typescript
// ✅ CORRECT: Type guards for runtime validation
export function isEventDto(value: unknown): value is EventDto {
  return (
    typeof value === 'object' &&
    value !== null &&
    'id' in value &&
    'name' in value &&
    'startDateTime' in value
  );
}

export function isSessionDto(value: unknown): value is SessionDto {
  return (
    typeof value === 'object' &&
    value !== null &&
    'sessionIdentifier' in value &&
    'name' in value
  );
}

// Usage
const response = await fetch('/api/events/1');
const data = await response.json();

if (isEventDto(data)) {
  // TypeScript knows data is EventDto here
  console.log(data.name);
} else {
  console.error('Invalid event data received');
}
```

## Discriminated Unions

### Pattern
```typescript
// ✅ CORRECT: Discriminated unions for variant types
type LoadingState =
  | { status: 'idle' }
  | { status: 'loading' }
  | { status: 'success'; data: EventDto }
  | { status: 'error'; error: string };

function EventDisplay({ state }: { state: LoadingState }) {
  switch (state.status) {
    case 'idle':
      return <div>Ready to load</div>;
    case 'loading':
      return <Loader />;
    case 'success':
      return <EventCard event={state.data} />;
    case 'error':
      return <Alert color="red">{state.error}</Alert>;
  }
}
```

## Generic Components

### Pattern
```typescript
// ✅ CORRECT: Generic components with proper typing
interface DataTableProps<T> {
  data: T[];
  columns: Array<{
    key: keyof T;
    header: string;
    render?: (value: T[keyof T]) => React.ReactNode;
  }>;
}

export function DataTable<T>({ data, columns }: DataTableProps<T>) {
  return (
    <Table>
      <Table.Thead>
        <Table.Tr>
          {columns.map(col => (
            <Table.Th key={String(col.key)}>{col.header}</Table.Th>
          ))}
        </Table.Tr>
      </Table.Thead>
      <Table.Tbody>
        {data.map((row, idx) => (
          <Table.Tr key={idx}>
            {columns.map(col => (
              <Table.Td key={String(col.key)}>
                {col.render ? col.render(row[col.key]) : String(row[col.key])}
              </Table.Td>
            ))}
          </Table.Tr>
        ))}
      </Table.Tbody>
    </Table>
  );
}

// Usage with full type safety
<DataTable<EventDto>
  data={events}
  columns={[
    { key: 'name', header: 'Event Name' },
    { key: 'startDateTime', header: 'Start Date', render: (date) => formatDate(date) },
  ]}
/>
```

## Utility Types

### Common Patterns
```typescript
// ✅ Partial types for optional updates
type EventUpdate = Partial<EventDto>;

// ✅ Pick for selecting specific fields
type EventListItem = Pick<EventDto, 'id' | 'name' | 'startDateTime'>;

// ✅ Omit for excluding fields
type CreateEventRequest = Omit<EventDto, 'id' | 'createdAt' | 'updatedAt'>;

// ✅ Record for key-value mappings
type EventIdMap = Record<number, EventDto>;

// ✅ ReadonlyArray for immutable arrays
function displayEvents(events: ReadonlyArray<EventDto>) {
  // TypeScript prevents mutation
  // events.push(newEvent); // Error!
}
```

## Null Safety

### Pattern
```typescript
// ✅ CORRECT: Proper null/undefined handling
interface User {
  name: string;
  email?: string;  // Optional field
  roles: string[] | null;  // Nullable array
}

function UserDisplay({ user }: { user: User | null }) {
  // Handle null user
  if (!user) {
    return <div>No user data</div>;
  }

  // Handle optional email
  const emailDisplay = user.email ?? 'No email provided';

  // Handle nullable array with type narrowing
  const rolesList = user.roles?.join(', ') ?? 'No roles assigned';

  return (
    <div>
      <div>{user.name}</div>
      <div>{emailDisplay}</div>
      <div>{rolesList}</div>
    </div>
  );
}
```

## API Response Typing

### Pattern
```typescript
// ✅ CORRECT: Strongly typed API responses
interface ApiResponse<T> {
  data: T;
  success: boolean;
  errors?: string[];
}

async function fetchEvent(id: number): Promise<ApiResponse<EventDto>> {
  const response = await fetch(`/api/events/${id}`);

  if (!response.ok) {
    return {
      data: null as any,  // Will be checked via success flag
      success: false,
      errors: ['Failed to fetch event'],
    };
  }

  const data = await response.json();
  return {
    data,
    success: true,
  };
}

// Usage with type safety
const result = await fetchEvent(123);
if (result.success) {
  console.log(result.data.name);  // TypeScript knows data is EventDto
} else {
  console.error(result.errors);
}
```

## Enum Usage

### Pattern
```typescript
// ✅ CORRECT: Use const enums for compile-time constants
export const enum EventType {
  Class = 'Class',
  Meetup = 'Meetup',
  Performance = 'Performance',
  Social = 'Social',
}

export const enum VettingStatus {
  NotStarted = 'NotStarted',
  Pending = 'Pending',
  Approved = 'Approved',
  Rejected = 'Rejected',
}

// Usage
function isClassEvent(event: EventDto): boolean {
  return event.type === EventType.Class;
}
```

## Type Assertion Safety

### Bad Pattern
```typescript
// ❌ WRONG: Unsafe type assertion
const data = response.json() as EventDto;  // No runtime validation!
```

### Good Pattern
```typescript
// ✅ CORRECT: Validate before asserting
const data = await response.json();
if (isEventDto(data)) {
  // Now safe to use as EventDto
  processEvent(data);
} else {
  throw new Error('Invalid event data from API');
}
```

## Standards Maintenance

When you discover type-related bugs or patterns:
1. Add the pattern to this document
2. Include both incorrect and correct examples
3. Update DTO Alignment Strategy if DTOs are involved
4. Regenerate types if backend DTOs changed: `cd packages/shared-types && npm run generate`

---

*This document is maintained by the React Developer Agent and TypeScript specialists.*

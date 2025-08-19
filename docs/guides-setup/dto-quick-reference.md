# DTO Quick Reference Guide
<!-- Last Updated: 2025-08-19 -->
<!-- Version: 1.0 -->
<!-- Owner: Architecture Team -->
<!-- Status: Active -->

## 🚀 Quick Start

**Problem**: Need to create TypeScript interfaces for React components?
**Solution**: NEVER create them manually - use NSwag auto-generation!

**Golden Rule**: API DTOs are SOURCE OF TRUTH. NSwag generates TypeScript types automatically.

## 🚨 CRITICAL: Use Generated Types ONLY

```typescript
// CORRECT - Import generated types
import { User, Event, Registration } from '@witchcityrope/shared-types';

// WRONG - Never create manual interfaces
interface User {
  // This violates the architecture!
}
```

**NSwag Pipeline**: Run `npm run generate:types` when API changes.

## Common DTO Patterns

### User Authentication
```csharp
// C# DTO (SOURCE OF TRUTH)
public class UserDto
{
    public string SceneName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public List<string> Roles { get; set; }
    public bool IsVetted { get; set; }
    public string MembershipLevel { get; set; }
}
```

```typescript
// TypeScript Interface (MUST MATCH EXACTLY)
interface User {
  sceneName: string;
  createdAt: string; // ISO 8601 format
  lastLoginAt: string | null;
  roles: string[];
  isVetted: boolean;
  membershipLevel: string;
}
```

### Event Management
```csharp
// C# DTO (SOURCE OF TRUTH)
public class EventDto
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime EventDate { get; set; }
    public DateTime? RegistrationDeadline { get; set; }
    public int MaxAttendees { get; set; }
    public int CurrentAttendees { get; set; }
    public decimal Price { get; set; }
    public EventStatus Status { get; set; }
    public string VenueLocation { get; set; }
    public bool RequiresVetting { get; set; }
}
```

```typescript
// TypeScript Interface (MUST MATCH EXACTLY)
interface Event {
  id: string; // Guid serializes to string
  title: string;
  description: string;
  eventDate: string; // ISO 8601 format
  registrationDeadline: string | null;
  maxAttendees: number;
  currentAttendees: number;
  price: number;
  status: 'draft' | 'published' | 'full' | 'cancelled' | 'completed';
  venueLocation: string;
  requiresVetting: boolean;
}
```

### Registration System
```csharp
// C# DTO (SOURCE OF TRUTH)
public class RegistrationDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string AttendeeSceneName { get; set; }
    public RegistrationStatus Status { get; set; }
    public DateTime RegistrationDate { get; set; }
    public decimal? AmountPaid { get; set; }
    public string PaymentMethod { get; set; }
    public string SpecialRequests { get; set; }
    public bool HasWaiver { get; set; }
}
```

```typescript
// TypeScript Interface (MUST MATCH EXACTLY)
interface Registration {
  id: string;
  eventId: string;
  attendeeSceneName: string;
  status: 'pending' | 'confirmed' | 'waitlisted' | 'cancelled';
  registrationDate: string;
  amountPaid: number | null;
  paymentMethod: string;
  specialRequests: string;
  hasWaiver: boolean;
}
```

## Data Type Mapping

| C# Type | TypeScript Type | Notes |
|---------|-----------------|-------|
| `string` | `string` | Direct mapping |
| `int`, `decimal`, `double` | `number` | All numeric types to number |
| `bool` | `boolean` | Direct mapping |
| `DateTime` | `string` | ISO 8601 format (UTC) |
| `DateTime?` | `string \| null` | Nullable dates |
| `Guid` | `string` | Serializes to string |
| `List<T>` | `T[]` | Arrays in TypeScript |
| `enum` | `string literal union` | Use specific enum values |

## Enum Handling

### C# Enum
```csharp
public enum MembershipLevel
{
    Guest,
    General,
    Vetted,
    Teacher,
    Admin
}
```

### TypeScript Union Type
```typescript
type MembershipLevel = 'guest' | 'general' | 'vetted' | 'teacher' | 'admin';
```

## Null/Optional Property Guidelines

### Required Properties
```csharp
public string SceneName { get; set; } // Required
```
```typescript
sceneName: string; // Required
```

### Optional Properties
```csharp
public string? RealName { get; set; } // Optional
```
```typescript
realName?: string; // Optional (may not be present)
```

### Nullable Properties
```csharp
public DateTime? LastLoginAt { get; set; } // Can be null
```
```typescript
lastLoginAt: string | null; // Can be null
```

## API Response Patterns

### Single Entity Response
```csharp
// C# API Response
public class ApiResponse<T>
{
    public T Data { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; }
    public List<string> Errors { get; set; }
}
```

```typescript
// TypeScript Interface
interface ApiResponse<T> {
  data: T;
  success: boolean;
  message: string;
  errors: string[];
}
```

### Paginated Response
```csharp
// C# Paginated Response
public class PagedResponse<T>
{
    public List<T> Items { get; set; }
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}
```

```typescript
// TypeScript Interface
interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}
```

## Common Validation Patterns

### Form Validation with Zod
```typescript
import { z } from 'zod';

// Match the DTO validation exactly
const EventSchema = z.object({
  title: z.string().min(1).max(200),
  description: z.string().min(1),
  eventDate: z.string().refine(date => new Date(date) > new Date()),
  maxAttendees: z.number().min(1).max(100),
  price: z.number().min(0),
  requiresVetting: z.boolean()
});

type EventFormData = z.infer<typeof EventSchema>;
```

## Testing Patterns

### Mock API Responses
```typescript
// CORRECT - Mock actual API structure
const mockUser: User = {
  sceneName: "TestUser123",
  createdAt: "2023-08-19T10:30:00Z",
  lastLoginAt: "2023-08-19T08:15:00Z",
  roles: ["general"],
  isVetted: false,
  membershipLevel: "general"
};

// WRONG - Don't create ideal mock data
const mockUser = {
  firstName: "John", // API doesn't return this
  lastName: "Doe",   // API doesn't return this
  email: "john@example.com" // API doesn't return this
};
```

## Error Prevention Checklist

- [ ] Checked actual API response structure
- [ ] Matched property names exactly (case-sensitive)
- [ ] Handled nullable properties correctly
- [ ] Used proper TypeScript union types for enums
- [ ] Tested with real API data, not mocked ideal data
- [ ] Added proper type guards for runtime validation
- [ ] Documented any complex mappings needed

## Quick Commands

```bash
# Generate TypeScript types from OpenAPI (PRIMARY COMMAND)
npm run generate:types

# Test API integration with generated types
npm run test:integration

# Type check entire project
npm run type-check

# Start API for type generation
docker-compose up api
```

## NSwag Implementation Status

**Current State**: Manual interfaces created before NSwag pipeline implementation
**Target State**: All types generated via packages/shared-types/src/generated/
**Action Required**: Replace manual interfaces with generated types

## Emergency Contacts

- **DTO Mismatch Issues**: Architecture Review Board
- **TypeScript Compilation Errors**: Frontend Team Lead
- **API Contract Questions**: Backend Team Lead
- **Breaking Changes**: Project Manager

---

**Remember**: NEVER manually create DTO interfaces! Always use generated types from `@witchcityrope/shared-types`. The whole point of NSwag is to prevent manual interface creation and alignment issues.

**See**: `/docs/architecture/react-migration/domain-layer-architecture.md` for complete NSwag implementation details.

*Last updated: 2025-08-19 - Updated to emphasize NSwag auto-generation*
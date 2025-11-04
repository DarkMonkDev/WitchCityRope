# Microservices Patterns

**Purpose**: Web + API microservices architecture patterns, service communication, and boundaries.
**When to Read**: When understanding system architecture or implementing cross-service communication.
**Related**: [Docker Architecture](/docs/architecture/docker-architecture.md), [API Design Patterns](../backend/api-design-patterns.md)

## WitchCityRope Architecture

### Service Overview
```
┌──────────────────┐         ┌──────────────────┐
│   Web Service    │────────▶│   API Service    │
│  (React + Vite)  │  HTTP   │  (Minimal API)   │
│  Port: 5173      │         │  Port: 5655      │
└──────────────────┘         └─────────┬────────┘
                                       │
                                       ▼
                             ┌──────────────────┐
                             │    PostgreSQL    │
                             │   Port: 5434     │
                             └──────────────────┘
```

### Service Responsibilities

**Web Service** (React Frontend):
- User interface rendering
- Client-side routing
- Form validation (client-side)
- State management (Zustand)
- API consumption
- Authentication state (from cookies)

**API Service** (.NET Minimal API):
- Business logic enforcement
- Data validation (server-side)
- Database operations
- Authentication/authorization
- JWT token generation
- Cookie management

**Database** (PostgreSQL):
- Data persistence
- Referential integrity
- Transaction management
- Query optimization

## Service Communication

### HTTP-Only Communication
```typescript
// ✅ CORRECT: React → API via HTTP
export async function fetchEvents(): Promise<EventDto[]> {
  const response = await fetch('http://localhost:5655/api/events', {
    credentials: 'include',  // Send cookies
  });

  if (!response.ok) {
    throw new Error('Failed to fetch events');
  }

  return response.json();
}
```

### ❌ WRONG: Direct Database Access
```typescript
// ❌ NEVER do this in React
import { Pool } from 'pg';
const pool = new Pool({ connectionString: 'postgresql://...' });
```

## Authentication Flow

### Cookie-Based Auth Pattern
```
1. User Login Request
   React ──POST /api/auth/login──▶ API

2. API Validates Credentials
   API ──Query──▶ PostgreSQL

3. API Sets HttpOnly Cookie
   API ──Set-Cookie (HttpOnly)──▶ React

4. Subsequent Requests Include Cookie
   React ──GET /api/events + Cookie──▶ API

5. API Validates Cookie
   API ──Verify JWT──▶ Returns Data

6. User Logout
   React ──POST /api/auth/logout──▶ API
   API ──Delete Cookie──▶ React
```

## Service Boundaries

### What Belongs in Web Service
- ✅ UI components
- ✅ Client-side routing
- ✅ Client-side validation (for UX)
- ✅ State management
- ✅ API client functions

### What Belongs in API Service
- ✅ Business logic
- ✅ Server-side validation (mandatory)
- ✅ Database queries
- ✅ Authentication logic
- ✅ Authorization checks
- ✅ Data transformations

### What Belongs in Database
- ✅ Data storage
- ✅ Constraints (foreign keys, unique)
- ✅ Indexes
- ✅ Stored procedures (if needed)

## Cross-Service Concerns

### Shared Data Contracts (DTOs)
```typescript
// ✅ CORRECT: Single source of truth
import type { components } from '@witchcityrope/shared-types';

// Auto-generated from C# DTOs
export type EventDto = components['schemas']['EventDto'];
```

```csharp
// C# DTO (source of truth)
public record EventDto
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public DateTime StartDateTime { get; init; }
}
```

### Error Handling Across Services
```typescript
// React catches API errors
try {
  const event = await createEvent(data);
  notifications.show({ message: 'Event created!', color: 'green' });
} catch (error) {
  if (error instanceof ApiError) {
    notifications.show({ message: error.message, color: 'red' });
  }
}
```

```csharp
// API returns structured errors
if (!result.IsSuccess)
{
    return Results.BadRequest(new ErrorResponse
    {
        Message = "Validation failed",
        Errors = result.Errors.ToList()
    });
}
```

## Deployment Patterns

### Development (Docker Compose)
```yaml
services:
  web:
    build: ./packages/witchcityrope-web
    ports:
      - "5173:3000"
    environment:
      - VITE_API_URL=http://api:8080

  api:
    build: ./src/WitchCityRope.Api
    ports:
      - "5655:8080"
    environment:
      - ConnectionStrings__DefaultConnection=Host=postgres...

  postgres:
    image: postgres:16
    ports:
      - "5434:5432"
```

### Production (DigitalOcean)
- Web Service: Static hosting or container
- API Service: Container with health checks
- Database: Managed PostgreSQL

## Service Discovery

### Docker DNS Resolution
```typescript
// Development: Services use Docker service names
const API_URL = process.env.NODE_ENV === 'production'
  ? 'https://api.witchcityrope.com'
  : 'http://api:8080';  // Docker service name
```

## Health Checks

### API Health Endpoint
```csharp
app.MapGet("/health", async (ApplicationDbContext db) =>
{
    try
    {
        await db.Database.CanConnectAsync();
        return Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
    catch
    {
        return Results.Problem("Database connection failed");
    }
});
```

### Web Service Health
```typescript
export function HealthCheck() {
  const [status, setStatus] = useState<'checking' | 'healthy' | 'unhealthy'>('checking');

  useEffect(() => {
    fetch('/api/health')
      .then(res => setStatus(res.ok ? 'healthy' : 'unhealthy'))
      .catch(() => setStatus('unhealthy'));
  }, []);

  return <div>Service: {status}</div>;
}
```

## Service Isolation Benefits

1. **Independent Scaling**: Scale Web and API separately
2. **Technology Independence**: React and .NET can evolve independently
3. **Team Boundaries**: Frontend and backend teams work independently
4. **Deployment Flexibility**: Deploy services separately
5. **Fault Isolation**: Frontend works even if API is down (cached data)

## Standards Maintenance

When working across services:
1. Always communicate via HTTP (never direct database access)
2. Use auto-generated DTOs for type safety
3. Implement health checks for all services
4. Follow cookie-based authentication
5. Handle errors gracefully at service boundaries
6. Document service contracts (DTOs, endpoints)

---

*This document is maintained by the Backend Developer and React Developer agents.*

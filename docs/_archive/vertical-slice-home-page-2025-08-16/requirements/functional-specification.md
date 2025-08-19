# Functional Specification: Vertical Slice Home Page with Events Display
<!-- Last Updated: 2025-08-16 -->
<!-- Version: 2.0 -->
<!-- Owner: Functional Spec Agent -->
<!-- Status: Ready for Review -->

## Executive Summary

This functional specification defines a **minimal technical proof-of-concept** to validate the React + .NET API + PostgreSQL technology stack. This is **throwaway code** designed to test communication between layers and document lessons learned before implementing the actual production system.

## Architecture

### Microservices Architecture
**CRITICAL**: This is a Web+API microservices architecture:
- **Web Service** (React + Vite): UI at http://localhost:5173
- **API Service** (.NET Minimal API): Business logic at http://localhost:5653
- **Database** (PostgreSQL): localhost:5433
- **Pattern**: React → HTTP → API → Database (NEVER React → Database directly)

### Technology Stack
- **Frontend**: React 18.3.1 + TypeScript + Vite
- **HTTP Client**: Basic fetch API or Axios
- **Backend**: .NET 9 Minimal API
- **Database**: PostgreSQL with Entity Framework Core

### Component Structure (Simplified)
```
apps/web/src/
├── components/
│   ├── HomePage.tsx              # Main page component
│   ├── EventsList.tsx            # Events container with fetch logic
│   ├── EventCard.tsx             # Individual event display
│   └── LoadingSpinner.tsx        # Simple loading state
├── types/
│   └── Event.ts                  # Basic Event interface
└── utils/
    └── api.ts                    # Simple API fetch functions
```

### API Service Architecture (Minimal)
```
apps/api/
├── Controllers/
│   └── EventsController.cs      # Single GET endpoint
├── Models/
│   └── Event.cs                 # Basic Event entity
├── Services/
│   └── EventService.cs          # Simple data retrieval
└── Data/
    └── ApplicationDbContext.cs  # EF Core context
```

## Progressive Implementation

### Step 1: Hardcoded API Response
**Goal**: Prove React ↔ API communication works

- API returns hardcoded JSON array of 2-3 events
- No database involved
- Focus: HTTP communication, JSON serialization, basic error handling

### Step 2: Database Integration  
**Goal**: Prove API ↔ Database communication works

- Create simple Event table in PostgreSQL
- API queries database instead of returning hardcoded data
- **Note**: Database schema will likely be discarded after testing

## Data Models (Simplified)

### Database Schema (Step 2 Only)
```sql
-- Minimal Event table for testing
CREATE TABLE "Events" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "Title" VARCHAR(200) NOT NULL,
    "Description" TEXT NOT NULL,
    "StartDate" TIMESTAMPTZ NOT NULL,
    "Location" VARCHAR(200) NOT NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### TypeScript Interface (Simple)
```typescript
// apps/web/src/types/Event.ts
export interface Event {
  id: string;
  title: string;
  description: string;
  startDate: string;  // ISO 8601 string
  location: string;
}
```

### C# Model (Basic)
```csharp
// apps/api/Models/Event.cs
public class Event
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
```

## API Specification

### Single Endpoint
| Method | Path | Description | Response |
|--------|------|-------------|----------|
| GET | /api/events | Get events | Event[] |

**Step 1 Response (Hardcoded)**:
```json
[
  {
    "id": "1",
    "title": "Test Workshop",
    "description": "A test workshop for validating the tech stack",
    "startDate": "2025-08-25T14:00:00Z",
    "location": "Salem, MA"
  },
  {
    "id": "2", 
    "title": "Another Test Event",
    "description": "Another event to verify list display works",
    "startDate": "2025-08-30T19:00:00Z", 
    "location": "Salem, MA"
  }
]
```

**C# Controller (Simple)**:
```csharp
[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly EventService _eventService;

    public EventsController(EventService eventService)
    {
        _eventService = eventService;
    }

    [HttpGet]
    public async Task<ActionResult<Event[]>> GetEvents()
    {
        try
        {
            var events = await _eventService.GetEventsAsync();
            return Ok(events);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to retrieve events" });
        }
    }
}
```

## Component Specifications (Simplified)

### HomePage Component
```typescript
import React from 'react';
import { EventsList } from './EventsList';

export const HomePage: React.FC = () => {
  return (
    <div className="container mx-auto px-4 py-8">
      <h1 className="text-3xl font-bold text-center mb-8">
        WitchCity Rope - Tech Stack Test
      </h1>
      <EventsList />
    </div>
  );
};
```

### EventsList Component (Core Logic)
```typescript
import React, { useState, useEffect } from 'react';
import { Event } from '../types/Event';
import { EventCard } from './EventCard';
import { LoadingSpinner } from './LoadingSpinner';

export const EventsList: React.FC = () => {
  const [events, setEvents] = useState<Event[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchEvents = async () => {
      try {
        const response = await fetch('http://localhost:5653/api/events');
        if (!response.ok) {
          throw new Error('Failed to fetch events');
        }
        const data = await response.json();
        setEvents(data);
      } catch (err) {
        setError('Failed to load events');
        console.error('Error fetching events:', err);
      } finally {
        setLoading(false);
      }
    };

    fetchEvents();
  }, []);

  if (loading) return <LoadingSpinner />;
  if (error) return <div className="text-red-600">{error}</div>;
  if (events.length === 0) return <div>No events available</div>;

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
      {events.map((event) => (
        <EventCard key={event.id} event={event} />
      ))}
    </div>
  );
};
```

### EventCard Component (Basic)
```typescript
import React from 'react';
import { Event } from '../types/Event';

interface EventCardProps {
  event: Event;
}

export const EventCard: React.FC<EventCardProps> = ({ event }) => {
  const formatDate = (isoString: string) => {
    return new Date(isoString).toLocaleDateString('en-US', {
      weekday: 'long',
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: 'numeric',
      minute: '2-digit'
    });
  };

  return (
    <div className="border border-gray-300 rounded-lg p-4 shadow-md hover:shadow-lg transition-shadow">
      <h3 className="text-lg font-semibold mb-2">{event.title}</h3>
      <p className="text-gray-600 mb-4">{event.description}</p>
      <div className="text-sm text-gray-500">
        <p>📅 {formatDate(event.startDate)}</p>
        <p>📍 {event.location}</p>
      </div>
    </div>
  );
};
```

### LoadingSpinner Component
```typescript
import React from 'react';

export const LoadingSpinner: React.FC = () => {
  return (
    <div className="flex justify-center items-center py-8">
      <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
      <span className="ml-2">Loading events...</span>
    </div>
  );
};
```

## Testing Requirements (Simplified)

### Basic API Test
```csharp
[TestMethod]
public async Task GetEvents_ReturnsSuccessResult()
{
    // Arrange
    var controller = new EventsController(new EventService());

    // Act
    var result = await controller.GetEvents();

    // Assert
    Assert.IsInstanceOfType(result.Result, typeof(OkObjectResult));
    var okResult = result.Result as OkObjectResult;
    var events = okResult.Value as Event[];
    Assert.IsTrue(events.Length > 0);
}
```

### Basic Component Test
```typescript
import { render, screen } from '@testing-library/react';
import { EventCard } from '../EventCard';

const mockEvent = {
  id: '1',
  title: 'Test Event',
  description: 'Test description',
  startDate: '2025-08-23T14:00:00Z',
  location: 'Salem, MA'
};

test('displays event information', () => {
  render(<EventCard event={mockEvent} />);
  
  expect(screen.getByText('Test Event')).toBeInTheDocument();
  expect(screen.getByText('Test description')).toBeInTheDocument();
  expect(screen.getByText('Salem, MA')).toBeInTheDocument();
});
```

### Basic E2E Test
```typescript
import { test, expect } from '@playwright/test';

test('home page loads and shows events', async ({ page }) => {
  await page.goto('/');
  
  await expect(page.getByText('WitchCity Rope - Tech Stack Test')).toBeVisible();
  await expect(page.getByText('Loading events...')).toBeVisible();
  
  // Wait for events to load
  await expect(page.getByText('Test Workshop')).toBeVisible();
});
```

## Technical Validation Goals

### Primary Goals
- [ ] React app successfully calls API endpoint
- [ ] API returns JSON data without errors
- [ ] Events display on frontend
- [ ] Basic responsive grid layout works
- [ ] Loading state functions
- [ ] Basic error handling displays message

### Secondary Goals (Step 2)
- [ ] PostgreSQL database integration works
- [ ] API successfully queries database
- [ ] Data flows React → API → Database → API → React

## Success Criteria (Technical Proof)

### Communication Tests
- [ ] React fetch() call reaches API endpoint
- [ ] API returns proper JSON response
- [ ] Frontend displays data from API
- [ ] Console shows no critical errors

### Database Tests (Step 2)
- [ ] EF Core connects to PostgreSQL
- [ ] API queries return database data
- [ ] Events persist between API restarts

### Basic User Experience
- [ ] Page loads without crashing
- [ ] Events display in card format
- [ ] Layout is responsive (stacks on mobile)
- [ ] Loading spinner shows during fetch

## Environment Configuration

### Frontend (.env.development)
```
VITE_API_URL=http://localhost:5653
```

### API (appsettings.Development.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=witchcityrope_test;Username=postgres;Password=test123"
  },
  "AllowedOrigins": ["http://localhost:5173"]
}
```

## Dependencies

### React (Minimal)
- React 18.3.1 + TypeScript
- Tailwind CSS for basic styling
- No complex state management libraries

### .NET API (Basic)
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.0" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="9.0.0" />
```

## Quality Gates (Simplified for POC)

### Must Pass
- [ ] React app builds without errors
- [ ] API compiles and runs
- [ ] Basic functionality works (events display)
- [ ] No console errors during normal operation

### Should Pass
- [ ] Mobile responsive layout
- [ ] Loading states function
- [ ] Error handling displays messages
- [ ] Database connection works (Step 2)

---

**Key Changes Made:**
1. **Removed complex production concerns**: Authentication, SEO, accessibility compliance, performance optimization
2. **Simplified component structure**: Minimal components focused on proving communication
3. **Basic testing only**: Simple tests to verify functionality, no coverage requirements
4. **Progressive approach**: Step 1 (hardcoded) → Step 2 (database)
5. **Focus on technical proof**: Communication between layers, not features
6. **Throwaway code mindset**: Database entities will be discarded after testing

**Next Phase:** Design & Architecture (simple mockups for basic layout)
**Human Review Required:** Technical validation approach before proceeding
**Estimated Effort:** 1-2 development days for complete proof-of-concept
**Risk Level:** Very Low (minimal scope, focused on stack validation)
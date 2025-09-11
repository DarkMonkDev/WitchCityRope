# Functional Specification: Events Management System
<!-- Last Updated: 2025-08-25 -->
<!-- Version: 1.0 -->
<!-- Owner: Functional Spec Agent -->
<!-- Status: Draft -->

## Technical Overview

The Events Management System is a comprehensive solution for creating, managing, and checking into events at WitchCityRope. The system implements an **Event Session Matrix architecture** where Events contain multiple Sessions, and Tickets can include specific Sessions. This architecture supports both simple single-session events and complex multi-day events with various ticket combinations.

The system provides five main interfaces: Admin Dashboard, Event Form, Public Events List, Event Details, and Check-in Interface with specialized Kiosk Mode for secure on-site operations.

## Architecture

### Microservices Architecture
**CRITICAL**: This is a Web+API microservices architecture:
- **Web Service** (React + Vite): UI/Auth at http://localhost:5173
- **API Service** (Minimal API): Business logic at http://localhost:5653
- **Database** (PostgreSQL): localhost:5433
- **Pattern**: React → HTTP → API → Database (NEVER React → Database directly)

### Component Structure
```
/apps/web/src/pages/events/
├── AdminDashboard.tsx
├── EventForm.tsx
├── PublicEventsList.tsx
├── EventDetails.tsx
└── CheckInInterface.tsx

/apps/web/src/components/events/
├── EventCard.tsx
├── SessionEditor.tsx
├── TicketTypeEditor.tsx
├── VolunteerPositionEditor.tsx
├── EmailTemplateEditor.tsx
└── CheckInModal.tsx

/apps/api/Features/Events/
├── Services/
│   ├── IEventService.cs
│   ├── EventService.cs
│   ├── ISessionService.cs
│   ├── SessionService.cs
│   ├── ITicketService.cs
│   ├── TicketService.cs
│   ├── ICheckInService.cs
│   └── CheckInService.cs
├── Models/
│   ├── EventDto.cs
│   ├── SessionDto.cs
│   ├── TicketTypeDto.cs
│   ├── OrderDto.cs
│   ├── RSVPDto.cs
│   ├── VolunteerPositionDto.cs
│   ├── EmailTemplateDto.cs
│   └── CheckInDto.cs
└── Validators/
    ├── EventValidator.cs
    ├── SessionValidator.cs
    └── TicketValidator.cs
```

### Service Architecture
- **Web Service**: UI components make HTTP calls to API
- **API Service**: Business logic with EF Core database access
- **No Direct Database Access**: Web service NEVER directly accesses database

## Data Models

### Database Schema

#### Core Event Schema
```sql
CREATE TABLE Events (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    Title VARCHAR(200) NOT NULL,
    ShortDescription VARCHAR(160) NOT NULL,
    FullDescription TEXT NOT NULL,
    PoliciesAndProcedures TEXT,
    EventType VARCHAR(20) NOT NULL CHECK (EventType IN ('Class', 'Social')),
    VenueId UUID NOT NULL REFERENCES Venues(Id),
    IsPublished BOOLEAN NOT NULL DEFAULT FALSE,
    CreatedAt TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UpdatedAt TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CreatedBy UUID NOT NULL REFERENCES Users(Id)
);

CREATE TABLE EventSessions (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    EventId UUID NOT NULL REFERENCES Events(Id) ON DELETE CASCADE,
    SessionNumber INTEGER NOT NULL,
    Name VARCHAR(100) NOT NULL,
    StartDateTime TIMESTAMPTZ NOT NULL,
    EndDateTime TIMESTAMPTZ NOT NULL,
    Capacity INTEGER NOT NULL,
    SoldTickets INTEGER NOT NULL DEFAULT 0,
    CreatedAt TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE TicketTypes (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    EventId UUID NOT NULL REFERENCES Events(Id) ON DELETE CASCADE,
    Name VARCHAR(100) NOT NULL,
    Type VARCHAR(20) NOT NULL CHECK (Type IN ('Single', 'Couples')),
    MinPrice DECIMAL(10,2) NOT NULL,
    MaxPrice DECIMAL(10,2) NOT NULL,
    Quantity INTEGER NOT NULL,
    SalesEndDate DATE NOT NULL,
    IsActive BOOLEAN NOT NULL DEFAULT TRUE,
    CreatedAt TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE TicketSessionInclusions (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    TicketTypeId UUID NOT NULL REFERENCES TicketTypes(Id) ON DELETE CASCADE,
    SessionId UUID NOT NULL REFERENCES EventSessions(Id) ON DELETE CASCADE,
    UNIQUE(TicketTypeId, SessionId)
);

CREATE TABLE Orders (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    OrderNumber VARCHAR(20) UNIQUE NOT NULL,
    CustomerId UUID NOT NULL REFERENCES Users(Id),
    TicketTypeId UUID NOT NULL REFERENCES TicketTypes(Id),
    Status VARCHAR(20) NOT NULL CHECK (Status IN ('Confirmed', 'Pending', 'Cancelled')),
    Amount DECIMAL(10,2) NOT NULL,
    CreatedAt TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE RSVPs (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    EventId UUID NOT NULL REFERENCES Events(Id) ON DELETE CASCADE,
    UserId UUID NOT NULL REFERENCES Users(Id),
    RSVPDate TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    HasPurchasedTicket BOOLEAN NOT NULL DEFAULT FALSE,
    UNIQUE(EventId, UserId)
);
```

#### Supporting Schema
```sql
CREATE TABLE Venues (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    Name VARCHAR(100) NOT NULL,
    Address TEXT,
    Capacity INTEGER NOT NULL DEFAULT 20
);

CREATE TABLE EventTeachers (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    EventId UUID NOT NULL REFERENCES Events(Id) ON DELETE CASCADE,
    TeacherId UUID NOT NULL REFERENCES Users(Id),
    UNIQUE(EventId, TeacherId)
);

CREATE TABLE VolunteerPositions (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    EventId UUID NOT NULL REFERENCES Events(Id) ON DELETE CASCADE,
    PositionName VARCHAR(100) NOT NULL,
    SessionsIncluded VARCHAR(50) NOT NULL, -- 'S1,S2,S3' or 'All'
    StartTime TIME NOT NULL,
    EndTime TIME NOT NULL,
    Description TEXT,
    NumberNeeded INTEGER NOT NULL,
    NumberAssigned INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE EmailTemplates (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    EventId UUID NOT NULL REFERENCES Events(Id) ON DELETE CASCADE,
    TemplateName VARCHAR(50) NOT NULL,
    TriggerType VARCHAR(30) NOT NULL,
    TargetSessions VARCHAR(50), -- 'All' or 'S1,S2,S3'
    SubjectLine VARCHAR(200) NOT NULL,
    EmailContent TEXT NOT NULL,
    IsActive BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE CheckInRecords (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    OrderId UUID NOT NULL REFERENCES Orders(Id),
    SessionId UUID NOT NULL REFERENCES EventSessions(Id),
    CheckedInAt TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CheckedInBy UUID NOT NULL REFERENCES Users(Id),
    PaymentStatus VARCHAR(20) NOT NULL,
    WaiverSigned BOOLEAN NOT NULL,
    Notes TEXT,
    KioskStationId VARCHAR(50)
);
```

### DTOs and ViewModels

#### Core Event DTOs
```csharp
public class EventDto
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string ShortDescription { get; set; }
    public string FullDescription { get; set; }
    public string PoliciesAndProcedures { get; set; }
    public string EventType { get; set; } // 'Class' | 'Social'
    public Guid VenueId { get; set; }
    public string VenueName { get; set; }
    public bool IsPublished { get; set; }
    public List<SessionDto> Sessions { get; set; }
    public List<TicketTypeDto> TicketTypes { get; set; }
    public List<Guid> TeacherIds { get; set; }
    public List<VolunteerPositionDto> VolunteerPositions { get; set; }
    public List<EmailTemplateDto> EmailTemplates { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class SessionDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public int SessionNumber { get; set; }
    public string Name { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public int Capacity { get; set; }
    public int SoldTickets { get; set; }
    public int AvailableSpots => Capacity - SoldTickets;
}

public class TicketTypeDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string Name { get; set; }
    public string Type { get; set; } // 'Single' | 'Couples'
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public int Quantity { get; set; }
    public DateTime SalesEndDate { get; set; }
    public List<Guid> SessionIds { get; set; }
    public string SessionsIncluded { get; set; } // 'S1, S2, S3'
    public bool IsActive { get; set; }
    public int SoldCount { get; set; }
    public int Available => Quantity - SoldCount;
}
```

#### Check-In DTOs
```csharp
public class CheckInDto
{
    public Guid OrderId { get; set; }
    public Guid SessionId { get; set; }
    public string CustomerName { get; set; }
    public string Pronouns { get; set; }
    public string PaymentStatus { get; set; }
    public bool WaiverSigned { get; set; }
    public string CheckInStatus { get; set; } // 'NotArrived' | 'CheckedIn'
    public string Notes { get; set; }
    public DateTime? CheckedInAt { get; set; }
}

public class CheckInRequestDto
{
    public Guid OrderId { get; set; }
    public Guid SessionId { get; set; }
    public string PaymentStatus { get; set; }
    public bool WaiverSigned { get; set; }
    public string Notes { get; set; }
    public string KioskStationId { get; set; }
}
```

## API Specifications

### Events Endpoints
| Method | Path | Description | Request | Response |
|--------|------|-------------|---------|----------|
| GET | /api/events | List published events | Query params | List&lt;EventDto&gt; |
| GET | /api/events/{id} | Get event details | Event ID | EventDto |
| POST | /api/events | Create event | CreateEventDto | EventDto |
| PUT | /api/events/{id} | Update event | UpdateEventDto | EventDto |
| DELETE | /api/events/{id} | Delete event | Event ID | 204 No Content |
| POST | /api/events/{id}/publish | Publish event | Event ID | EventDto |

### Sessions Endpoints
| Method | Path | Description | Request | Response |
|--------|------|-------------|---------|----------|
| GET | /api/events/{eventId}/sessions | List event sessions | Event ID | List&lt;SessionDto&gt; |
| POST | /api/events/{eventId}/sessions | Create session | CreateSessionDto | SessionDto |
| PUT | /api/sessions/{id} | Update session | UpdateSessionDto | SessionDto |
| DELETE | /api/sessions/{id} | Delete session | Session ID | 204 No Content |

### Ticket Types Endpoints
| Method | Path | Description | Request | Response |
|--------|------|-------------|---------|----------|
| GET | /api/events/{eventId}/ticket-types | List ticket types | Event ID | List&lt;TicketTypeDto&gt; |
| POST | /api/events/{eventId}/ticket-types | Create ticket type | CreateTicketTypeDto | TicketTypeDto |
| PUT | /api/ticket-types/{id} | Update ticket type | UpdateTicketTypeDto | TicketTypeDto |
| DELETE | /api/ticket-types/{id} | Delete ticket type | Ticket Type ID | 204 No Content |

### Check-In Endpoints
| Method | Path | Description | Request | Response |
|--------|------|-------------|---------|----------|
| GET | /api/events/{eventId}/check-in | Get check-in data | Event ID | List&lt;CheckInDto&gt; |
| POST | /api/check-in | Check in attendee | CheckInRequestDto | CheckInDto |
| GET | /api/check-in/session/{sessionId}/stats | Get session stats | Session ID | CheckInStatsDto |
| POST | /api/check-in/kiosk-session | Start kiosk session | KioskSessionRequestDto | KioskSessionDto |

### Admin Endpoints
| Method | Path | Description | Request | Response |
|--------|------|-------------|---------|----------|
| GET | /api/admin/events | List all events (admin) | Query params | PaginatedResponse&lt;EventDto&gt; |
| GET | /api/admin/events/{id}/orders | List event orders | Event ID | List&lt;OrderDto&gt; |
| GET | /api/admin/events/{id}/rsvps | List event RSVPs | Event ID | List&lt;RSVPDto&gt; |

## Component Specifications

### 1. Admin Events Dashboard (`/admin/events`)
**Path**: `/admin/events`
**Authorization**: Admin role required
**Render Mode**: InteractiveServer
**Key Features**:
- Event listing with filters (All Event Types, Show Past Events checkbox)
- Sortable table with columns: Date, Time, Event Title, Capacity, Tickets, Actions
- Create Event button
- Copy and Edit actions per event

#### Fields and Behavior:
- **Date Column**: Display format "Sat, Mar 15" - sortable
- **Time Column**: Display format "2:00 PM - 5:00 PM"
- **Event Title**: Linked to event edit page
- **Capacity**: Total capacity number (center-aligned)
- **Tickets**: Sold tickets count (center-aligned)
- **Actions**: Copy and Edit buttons per row

#### State Management:
- Filter state for event type
- Sort state for Date and Title columns
- Past events visibility toggle

### 2. Event Creation/Edit Form (`/admin/events/create`, `/admin/events/{id}/edit`)
**Path**: `/admin/events/create` or `/admin/events/{id}/edit`
**Authorization**: Admin/Teacher role required
**Render Mode**: InteractiveServer

#### Tab Navigation:
1. **Basic Info Tab**
2. **Tickets/Orders Tab**
3. **Emails Tab**
4. **Volunteers/Staff Tab**

#### Basic Info Tab Fields:
- **Event Type Toggle**: Radio buttons for "Class" vs "Social Event"
- **Event Title**: Text input (required, 200 char max)
- **Short Description**: Text input (required, 160 char max) with character counter
- **Full Event Description**: TinyMCE rich text editor with toolbar
- **Policies & Procedures**: TinyMCE rich text editor (admin-only editing)
- **Venue Selection**: Dropdown with "Add Venue" button
- **Teachers/Instructors**: Card-based selection with remove buttons

#### Tickets/Orders Tab:
##### Event Sessions Table:
| Column | Data Type | Behavior |
|--------|-----------|----------|
| Actions | Button | Edit button opens session modal |
| S# | Text | Session number (S1, S2, S3...) |
| Name | Text | Session display name |
| Date | Date | Session date |
| Start Time | Time | Session start time |
| End Time | Time | Session end time |
| Capacity | Number | Max attendees per session |
| Sold | Number | Current tickets sold |
| Delete | Button | Delete button with confirmation |

##### Ticket Types Table:
| Column | Data Type | Behavior |
|--------|-----------|----------|
| Edit | Button | Edit button opens ticket modal |
| Ticket Name | Text | Ticket type name |
| Type | Text | 'Single' or 'Couples' |
| Sessions Included | Text | "S1, S2, S3" format |
| Price Range | Currency | "€120 - €150" format |
| Quantity | Number | Available quantity |
| Sales End | Date | Last day for sales |
| Delete | Button | Delete with confirmation |

##### Orders/RSVPs Tables:
**Tickets Sold Table** (for paid events):
| Column | Data Type | Purpose |
|--------|-----------|---------|
| Actions | Button | View order details |
| Order # | Text | Unique order number |
| Customer | Text | Customer name |
| Ticket Type | Text | Purchased ticket type |
| Sessions | Text | Included sessions |
| Status | Badge | Confirmed/Pending/Cancelled |
| Amount | Currency | Order total |

**RSVPs Table** (for social events):
| Column | Data Type | Purpose |
|--------|-----------|---------|
| Actions | Button | View RSVP details |
| Name | Text | RSVP person name |
| Email | Email | Contact email |
| RSVP Date | Date | When they RSVP'd |
| Ticket Purchased | Boolean | Yes/No indicator |

#### Emails Tab:
- **Template Cards**: Visual cards for each email type with remove buttons
- **Ad-hoc Email Card**: Always present for one-time emails
- **Template Editor**: Unified editor section
- **Template Selection**: Dropdown to add new templates
- **Target Sessions**: Multi-select for template targeting
- **Rich Text Editor**: TinyMCE with email-specific toolbar

#### Volunteers/Staff Tab:
##### Volunteer Positions Table:
| Column | Data Type | Behavior |
|--------|-----------|----------|
| Edit | Button | Edit position details |
| Position | Text | Position name |
| Sessions | Text | S1, S2, All |
| Time | Time Range | Start - End time |
| Description | Text | Position duties |
| Needed | Number | Required volunteers |
| Assigned | Status | Assignment status |
| Delete | Button | Remove position |

##### Add Position Form:
- Position Name (text input)
- Session Selection (dropdown)
- Start/End Time (time inputs)
- Description (textarea)
- Number Needed (number input)

### 3. Public Events List (`/events`)
**Path**: `/events`
**Authorization**: Public access
**Render Mode**: InteractiveServer

#### View Toggle:
- **Card View**: Grid layout with event cards
- **List View**: Table format matching admin dashboard

#### Filtering and Search:
- **Show Past Classes**: Checkbox toggle
- **Search Box**: Text search across event titles
- **View Toggle**: Card/List view buttons
- **Sort Dropdown**: Date, Price, Availability options

#### Event Card Components:
- **Event Image**: Gradient background with title overlay
- **Event Date**: Formatted date/time
- **Event Description**: Truncated description
- **Event Price**: Price range display
- **Availability**: Spots remaining with color coding
- **Action Buttons**: "Learn More", "Login", "Become Member" based on access

### 4. Event Details (`/events/{id}`)
**Path**: `/events/{id}`
**Authorization**: Public access (content varies by auth state)
**Render Mode**: InteractiveServer

#### Left Column Content:
- **Event Hero**: Large title banner with meta information
- **About This Series**: Rich text description
- **Daily Breakdown**: Structured session information
- **Prerequisites**: Requirements and what to bring
- **Instructor Information**: Teacher profiles
- **Important Policies**: Refund and conduct policies

#### Right Column (Registration Card):
- **Capacity Warning**: Alert for constrained sessions
- **Session Availability**: Individual session capacity display
- **Ticket Selection**: Interactive ticket type cards
- **Purchase Button**: Dynamic text based on selection
- **Event Details**: Location, duration, class size
- **Share Section**: Social sharing buttons

#### Ticket Selection Logic:
- **Available Tickets**: Selectable with pricing
- **Unavailable Tickets**: Grayed out with reason
- **Session Constraints**: Show limiting session for multi-session tickets
- **Dynamic Pricing**: Price ranges with sliding scale

### 5. Check-In Interface (`/events/{id}/check-in`)
**Path**: `/events/{id}/check-in`
**Authorization**: Admin/Staff role required
**Render Mode**: Kiosk Mode (specialized security mode)

#### Kiosk Mode Features:
- **Browser Lock**: Prevents navigation away from page
- **Security Headers**: Station identification and session timeout
- **Device Fingerprinting**: Tracks kiosk station identity
- **Automatic Expiration**: Session timeout with renewal prompts

#### Interface Components:
- **Session Timer**: Countdown to session expiration
- **Event Statistics**: Real-time capacity and arrival stats
- **Search/Filter Bar**: Name search and status filters
- **Attendee Table**: Check-in status for all attendees

#### Attendee Table Columns:
| Column | Data Type | Sortable | Purpose |
|--------|-----------|----------|---------|
| Name | Text + Pronouns | Yes | Attendee identification |
| Payment | Status Badge | Yes | Payment status |
| Waiver | Status Badge | Yes | Waiver signing status |
| Status | Action/Status | Yes | Check-in button or status |

#### Check-In Modal:
- **Attendee Header**: Name and pronouns display
- **Payment Status**: Dropdown (Paid/Unpaid/Cash/Comp)
- **Waiver Checkbox**: Signed/Not Signed toggle
- **Notes Field**: Optional check-in notes
- **Action Buttons**: Cancel/Confirm check-in

## Integration Points

### Authentication System
- JWT-based authentication for API endpoints
- Role-based authorization (Admin, Teacher, Member, Guest)
- Kiosk mode session management
- Cookie-based web authentication

### Email Notifications
- Template-based email system
- Trigger-based sending (confirmation, reminders, cancellations)
- Variable replacement system
- Session-specific targeting

### Payment Processing
- Integration with payment gateway (future)
- Order management and status tracking
- Sliding scale pricing support
- Refund policy enforcement

### Venue Management
- Venue selection from existing venues
- Capacity constraints enforcement
- Location display in public interfaces

## Security Requirements

### Kiosk Mode Security
- **Session Token**: Unique 4-hour session tokens
- **Device Fingerprinting**: Track and validate kiosk stations
- **Browser Lock**: Disable right-click, F12, and navigation
- **Automatic Expiration**: Force re-authentication after timeout
- **Station Identification**: Unique station IDs for audit trail

### Data Protection
- **Input Validation**: All form inputs validated server-side
- **XSS Prevention**: HTML encoding for all user content
- **CSRF Protection**: Anti-forgery tokens on all forms
- **Role-based Authorization**: Endpoint-level permission checks
- **Audit Trail**: Track all admin/staff actions

### Privacy Considerations
- **Attendee Data**: Limit access to necessary personnel
- **Payment Information**: Secure handling of payment details
- **Communication Preferences**: Respect email preferences
- **Data Retention**: Implement data cleanup policies

## Performance Requirements

### Response Time Targets
- **Page Load**: <2 seconds for all interfaces
- **API Endpoints**: <500ms for CRUD operations
- **Search/Filter**: <1 second for result display
- **Check-in Modal**: <200ms open/close time

### Concurrent Users
- **Admin Interface**: 5 concurrent admin users
- **Public Interface**: 100+ concurrent visitors
- **Check-in Interface**: 2-3 concurrent kiosk stations
- **Peak Load**: Handle event announcement traffic

### Data Optimization
- **Session Capacity**: Real-time capacity calculations
- **Pagination**: 20 items per page on list views
- **Caching Strategy**: Cache venue and teacher data
- **Image Optimization**: Compressed event images

## Testing Requirements

### Unit Test Coverage
- **Business Logic**: 90%+ coverage of service methods
- **Validation**: 100% coverage of validators
- **Calculations**: 100% coverage of capacity/pricing logic
- **Security**: Authentication and authorization logic

### Integration Tests
- **API Endpoints**: All CRUD operations
- **Email System**: Template rendering and sending
- **Check-in Flow**: Complete check-in process
- **Payment Integration**: Order creation and processing

### End-to-End Testing
- **Event Creation**: Complete admin workflow
- **Public Registration**: User ticket purchase flow
- **Check-in Process**: Kiosk mode functionality
- **Mobile Responsiveness**: All interfaces on mobile

### Performance Testing
- **Load Testing**: 100 concurrent users
- **Stress Testing**: Capacity calculation under load
- **Memory Usage**: Check for memory leaks
- **Database Performance**: Query optimization validation

## Migration Requirements

### Database Migrations
- **Events Table**: Add new fields maintaining backward compatibility
- **Sessions Architecture**: New session/ticket relationship tables
- **Check-in System**: New check-in tracking tables
- **Email Templates**: Template configuration tables

### Data Transformation
- **Legacy Events**: Migration script for existing event data
- **User Relationships**: Maintain existing teacher/member relationships
- **Historical Data**: Preserve past event and attendance records

### Backward Compatibility
- **API Versioning**: Maintain existing API endpoints during transition
- **Data Access**: Ensure existing reports continue working
- **User Experience**: Gradual rollout with feature flags

## Dependencies

### NuGet Packages (API)
- **Entity Framework Core**: Database ORM
- **FluentValidation**: Input validation
- **Swashbuckle**: OpenAPI documentation
- **Polly**: Retry policies for external services
- **MediatR**: Command/query patterns (if needed)

### NPM Packages (Web)
- **React**: ^18.0.0
- **React Router**: ^6.0.0
- **TypeScript**: ^5.0.0
- **TanStack Query**: ^4.0.0 (API state management)
- **React Hook Form**: Form management
- **Zod**: Client-side validation

### External Services
- **Email Service**: SendGrid or similar for notifications
- **Payment Gateway**: Stripe integration (future)
- **Image Storage**: Cloud storage for event images
- **Analytics**: Google Analytics integration

### Configuration Needs
- **Environment Variables**: API URLs, authentication keys
- **Feature Flags**: Progressive rollout configuration
- **Rate Limiting**: API endpoint protection
- **CORS Settings**: Cross-origin request configuration

## Acceptance Criteria

### Admin Interface Acceptance
- [ ] Admin can create events with multiple sessions
- [ ] Admin can configure multiple ticket types per event
- [ ] Admin can manage volunteer positions
- [ ] Admin can configure email templates
- [ ] Admin dashboard shows real-time event metrics
- [ ] All admin actions are audited and logged

### Public Interface Acceptance
- [ ] Visitors can browse published events
- [ ] Visitors can view event details and pricing
- [ ] Members can see member-only events
- [ ] Ticket selection reflects session constraints
- [ ] Availability updates in real-time
- [ ] Mobile interface works on all major devices

### Check-in Interface Acceptance
- [ ] Staff can start secure kiosk sessions
- [ ] Check-in modal captures all required information
- [ ] Real-time statistics update correctly
- [ ] Search and filtering work as expected
- [ ] Kiosk security measures prevent tampering
- [ ] Session timeout and renewal work properly

### Technical Acceptance
- [ ] All API endpoints return consistent response formats
- [ ] Database constraints prevent invalid data
- [ ] Email templates render correctly with variables
- [ ] Error handling provides meaningful feedback
- [ ] Performance targets are met under load
- [ ] Security tests pass for all interfaces

### Business Logic Acceptance
- [ ] Session capacity calculations are accurate
- [ ] Ticket availability reflects session constraints
- [ ] Pricing displays correctly for all ticket types
- [ ] Event publishing workflow functions properly
- [ ] RSVPs vs paid tickets tracked accurately
- [ ] Volunteer position management works completely

This comprehensive specification covers all aspects of the Events Management System based on the wireframes provided, ensuring complete functionality while maintaining the microservices architecture and design system standards.
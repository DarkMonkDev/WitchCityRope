# Admin Events Management Implementation - Detailed Documentation

## Work Completed (Session Date: January 10, 2025)

### 1. Backend Architecture Changes

#### 1.1 Entity Model Updates

**Event Entity** (`/src/WitchCityRope.Core/Entities/Event.cs`)
Added properties:
```csharp
public string? ImageUrl { get; private set; }
public int RefundCutoffHours { get; private set; } = 48;
public DateTime? RegistrationOpensAt { get; private set; }
public DateTime? RegistrationClosesAt { get; private set; }
public TicketType TicketTypes { get; private set; } = TicketType.Individual;
public decimal? IndividualPrice { get; private set; }
public decimal? CouplesPrice { get; private set; }
```

Added methods:
- `UpdateImageUrl(string? imageUrl)`
- `UpdateRefundCutoff(int hours)`
- `UpdateRegistrationDates(DateTime? opensAt, DateTime? closesAt)`
- `UpdateTicketing(TicketType ticketTypes, decimal? individualPrice, decimal? couplesPrice)`
- `GetDuration()`

**New Entities Created:**

1. **VolunteerTask** (`/src/WitchCityRope.Core/Entities/VolunteerTask.cs`)
   - Manages volunteer positions for events
   - Properties: Name, Description, StartTime, EndTime, RequiredVolunteers
   - Methods: UpdateDetails, AssignVolunteer, RemoveAssignment, GetConfirmedVolunteerCount

2. **VolunteerAssignment** (`/src/WitchCityRope.Core/Entities/VolunteerAssignment.cs`)
   - Tracks individual volunteer assignments
   - Properties: TaskId, UserId, Status, HasTicket, TicketPrice, BackgroundCheckVerified
   - Methods: Confirm, Cancel, UpdateTicketStatus, UpdateBackgroundCheckStatus

3. **EventEmailTemplate** (`/src/WitchCityRope.Core/Entities/EventEmailTemplate.cs`)
   - Email templates for event communications
   - Properties: Type, Subject, Body, IsActive
   - Methods: UpdateContent, Activate, Deactivate, RenderTemplate

**New Enums:**
- `TicketType` (Flags): Individual, Couples, Both
- `VolunteerStatus`: Pending, Confirmed, Cancelled
- `EmailTemplateType`: RegistrationConfirmation, EventReminder, WaitlistNotification, CancellationNotice, Custom

#### 1.2 Database Configuration

**Entity Configurations Created:**
- `/src/WitchCityRope.Infrastructure/Data/Configurations/VolunteerTaskConfiguration.cs`
- `/src/WitchCityRope.Infrastructure/Data/Configurations/VolunteerAssignmentConfiguration.cs`
- `/src/WitchCityRope.Infrastructure/Data/Configurations/EventEmailTemplateConfiguration.cs`

**Updated Files:**
- `EventConfiguration.cs` - Added new Event properties with proper column types
- `WitchCityRopeIdentityDbContext.cs` - Added DbSets for new entities

#### 1.3 DTOs and ViewModels

**New DTOs Created:**

1. **VolunteerTaskDtos.cs** (`/src/WitchCityRope.Core/DTOs/`)
   - `VolunteerTaskDto`
   - `VolunteerAssignmentDto`
   - `CreateVolunteerTaskRequest`
   - `UpdateVolunteerTaskRequest`
   - `AssignVolunteerRequest`
   - `UpdateVolunteerTicketRequest`
   - `VolunteerSummaryDto`

2. **EmailTemplateDtos.cs** (`/src/WitchCityRope.Core/DTOs/`)
   - `EventEmailTemplateDto`
   - `SaveEmailTemplateRequest`
   - `SendEventEmailRequest`
   - `SendEventEmailResponse`
   - `EmailTemplateVariablesDto`

3. **EventEditViewModel.cs** (`/src/WitchCityRope.Web/Models/`)
   - Comprehensive view model containing all fields for all 4 tabs
   - Helper classes: `EventOrderDto`, `VolunteerTicketDto`, `UserOptionDto`
   - Enum: `PricingType` (Fixed, SlidingScale, Free)

**Updated DTOs:**
- All Event-related DTOs updated to include new fields
- Files updated: CommonDtos.cs, CreateEventRequest.cs, EventManagementViewModel.cs, ListEventsRequest.cs, ApiClient.cs

### 2. UI Implementation

#### 2.1 EventEdit.razor Complete Rewrite

**File:** `/src/WitchCityRope.Web/Features/Admin/Pages/EventEdit.razor`

**Structure:**
- 1,391 lines of comprehensive Blazor component code
- 4 tabs matching wireframe specifications exactly
- Responsive design with proper styling

**Tab 1: Basic Info**
- Event type toggle (Class vs Meetup)
- Title input with validation
- Image upload placeholder
- Rich text description editor
- Lead teacher dropdown
- Date/time/duration grid
- Venue address field
- Capacity input
- Registration open/close datetime pickers

**Tab 2: Tickets/Orders**
- Pricing type radio buttons (Fixed/Sliding Scale/Free)
- Dynamic ticket type selection
- Conditional pricing fields
- Refund cutoff dropdown
- Orders table for existing events
- Volunteer ticket assignment cards

**Tab 3: Emails**
- Email template selector cards (4 types)
- Template editing with subject and body
- Rich text editor toolbar
- Variable support display
- Custom email section with recipient selection
- Save/test/send buttons

**Tab 4: Volunteers/Staff**
- Volunteer summary statistics
- Current volunteers table
- Task management section
- Add/edit/delete task functionality
- Inline task editing form
- Time slot management

**Styling:**
- Matches wireframe design system
- Brown primary color (#8B4513)
- Consistent spacing and typography
- Status badges with appropriate colors
- Loading states and animations

### 3. Testing Implementation

#### 3.1 E2E Test Files Created

1. **admin-events-management.test.js** (`/tests/e2e/`)
   - Tests all 4 tabs functionality
   - Field validation testing
   - Dynamic behavior verification
   - CRUD operations for volunteer tasks
   - Email template selection
   - Screenshot capture

2. **admin-to-public-events.test.js** (`/tests/e2e/`)
   - End-to-end integration testing
   - Admin creation to public display
   - Field mapping verification
   - Registration flow testing
   - Status display validation

### 4. Documentation

**Created Documentation:**
1. `/docs/enhancements/admin-events-tabs/implementation-plan.md` - Initial planning document
2. `/docs/enhancements/admin-events-tabs/completion-summary.md` - Summary of completed work
3. `/docs/enhancements/admin-events-tabs/implementation-details.md` - This detailed documentation

## Database Schema Changes

### New Tables
1. **VolunteerTasks**
   - Id (PK, Guid)
   - EventId (FK to Events)
   - Name (nvarchar(200))
   - Description (nvarchar(1000))
   - StartTime (time)
   - EndTime (time)
   - RequiredVolunteers (int)
   - CreatedAt (datetime2)
   - UpdatedAt (datetime2)

2. **VolunteerAssignments**
   - Id (PK, Guid)
   - TaskId (FK to VolunteerTasks)
   - UserId (FK to Users)
   - Status (nvarchar(50))
   - HasTicket (bit)
   - TicketPrice (decimal(18,2))
   - BackgroundCheckVerified (bit)
   - AssignedAt (datetime2)
   - UpdatedAt (datetime2)

3. **EventEmailTemplates**
   - Id (PK, Guid)
   - EventId (FK to Events)
   - Type (nvarchar(50))
   - Subject (nvarchar(500))
   - Body (nvarchar(max))
   - IsActive (bit)
   - CreatedAt (datetime2)
   - UpdatedAt (datetime2)

### Modified Tables
**Events** table - Added columns:
- ImageUrl (nvarchar(500), nullable)
- RefundCutoffHours (int, default: 48)
- RegistrationOpensAt (datetime2, nullable)
- RegistrationClosesAt (datetime2, nullable)
- TicketTypes (nvarchar(50), default: 'Individual')
- IndividualPrice (decimal(18,2), nullable)
- CouplesPrice (decimal(18,2), nullable)

## API Endpoints Required (Not Yet Implemented)

### Event Management
- `PUT /api/events/{id}/image` - Upload event image
- `PUT /api/events/{id}/registration-dates` - Update registration dates
- `PUT /api/events/{id}/ticketing` - Update ticket types and pricing

### Volunteer Management
- `GET /api/events/{id}/volunteer-tasks` - Get all tasks for event
- `POST /api/events/{id}/volunteer-tasks` - Create new task
- `PUT /api/events/{id}/volunteer-tasks/{taskId}` - Update task
- `DELETE /api/events/{id}/volunteer-tasks/{taskId}` - Delete task
- `POST /api/events/{id}/volunteer-tasks/{taskId}/assign` - Assign volunteer
- `DELETE /api/events/{id}/volunteer-tasks/{taskId}/assignments/{assignmentId}` - Remove assignment
- `POST /api/events/{id}/volunteer-tickets` - Create volunteer ticket

### Email Management
- `GET /api/events/{id}/email-templates` - Get all templates
- `POST /api/events/{id}/email-templates` - Save template
- `PUT /api/events/{id}/email-templates/{templateId}` - Update template
- `POST /api/events/{id}/send-email` - Send custom email
- `POST /api/events/{id}/send-test-email` - Send test email

## Next Steps Required

1. **Database Migration**
   - Generate EF Core migration for schema changes
   - Apply migration to development database

2. **API Implementation**
   - Implement all required endpoints
   - Create service interfaces and implementations
   - Add validation and error handling

3. **Integration Testing**
   - Connect UI to actual API endpoints
   - Test data persistence
   - Verify public page display

4. **Full Test Suite Execution**
   - Run existing unit tests
   - Execute new Puppeteer tests
   - Performance testing

5. **Production Preparation**
   - Security review
   - Performance optimization
   - Error logging setup
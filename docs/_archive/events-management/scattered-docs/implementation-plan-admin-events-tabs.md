# Admin Events Management Tabs Implementation Plan

## Overview
This plan details the implementation of the correct tabs and fields for the Admin Events Management section based on the wireframe specifications found in `/docs/design/wireframes/event-creation.html`.

## Current State vs. Required State

### Current Implementation (EventEdit.razor)
**Tabs:**
1. Basic Info
2. Details & Requirements  
3. Pricing & Tickets
4. Schedule & Location
5. Registrations (existing events only)

**Issues:**
- Tab names don't match wireframes
- Fields are in wrong tabs
- Missing critical features: Emails tab, Volunteers/Staff tab
- Missing many fields from wireframes

### Required Implementation (Per Wireframes)
**Tabs:**
1. **Basic Info** - Event type, title, image, description, teacher, schedule, venue, capacity
2. **Tickets/Orders** - Pricing options, ticket types, current orders, volunteer tickets
3. **Emails** - Email templates, custom email sending
4. **Volunteers/Staff** - Volunteer management, task assignments

## Implementation Steps

### Phase 1: Backend Model Updates

#### 1.1 Update Event Entity
Add missing properties to `/src/WitchCityRope.Core/Entities/Event.cs`:
- `ImageUrl` (string?) - Event image
- `RefundCutoffHours` (int) - Default 48
- `RegistrationOpensAt` (DateTime?) - When registration opens
- `RegistrationClosesAt` (DateTime?) - When registration closes
- `Duration` (TimeSpan) - Event duration
- `TicketTypes` (enum flags: Individual, Couples, Both)
- `IndividualPrice` (decimal?) 
- `CouplesPrice` (decimal?)
- `EmailTemplates` (List<EmailTemplate>) - Associated email templates

#### 1.2 Create New Entities
**VolunteerTask** entity:
- `Id` (Guid)
- `EventId` (Guid)
- `Name` (string)
- `Description` (string)
- `StartTime` (TimeOnly)
- `EndTime` (TimeOnly)
- `RequiredVolunteers` (int)
- `AssignedVolunteers` (List<VolunteerAssignment>)

**VolunteerAssignment** entity:
- `Id` (Guid)
- `TaskId` (Guid)
- `UserId` (Guid)
- `Status` (enum: Pending, Confirmed, Cancelled)
- `HasTicket` (bool)
- `TicketPrice` (decimal)
- `BackgroundCheckVerified` (bool)

**EmailTemplate** entity:
- `Id` (Guid)
- `EventId` (Guid)
- `Type` (enum: RegistrationConfirmation, EventReminder, WaitlistNotification, CancellationNotice)
- `Subject` (string)
- `Body` (string)
- `IsActive` (bool)

#### 1.3 Update DTOs and ViewModels
- Create `EventEditViewModel` with all fields organized by tab
- Create `VolunteerTaskDto`, `VolunteerAssignmentDto`, `EmailTemplateDto`
- Update `EventDetailDto` to include new fields

### Phase 2: Service Layer Updates

#### 2.1 Event Service Enhancements
- Add methods for managing volunteer tasks
- Add methods for email template management
- Add method to send custom emails to attendees
- Add method to create complimentary tickets for volunteers

#### 2.2 New Services
- `IVolunteerService` - Manage volunteer assignments and tasks
- `IEventEmailService` - Handle event-specific email operations

### Phase 3: UI Implementation

#### 3.1 Restructure EventEdit.razor
Complete rewrite to match wireframe structure:

**Tab 1: Basic Info**
- Event type toggle (Class/Meetup) with descriptions
- Title input
- Image upload component
- Rich text editor for description
- Teacher dropdown
- Date/time/duration fields
- Venue address
- Capacity settings
- Registration open/close datetime pickers

**Tab 2: Tickets/Orders**
- Pricing type radio buttons (Fixed/Sliding Scale/Free)
- Ticket type radio buttons (Individual/Couples/Both)
- Individual and couples price inputs
- Refund cutoff dropdown
- Orders table with status badges
- Volunteer ticket assignment cards

**Tab 3: Emails**
- Email template selector cards
- Subject line editor
- Rich text email body editor with variable support
- Save template button
- Custom email section with recipient selector
- Test email and send buttons

**Tab 4: Volunteers/Staff**
- Volunteer summary cards (Total/Confirmed/Pending)
- Volunteers table with task assignments
- Add new task button
- Task list with edit/delete actions
- Task edit form (inline editing)

#### 3.2 Create New Components
- `EventImageUpload.razor` - Handle image uploads
- `RichTextEditor.razor` - Wrapper for rich text editing
- `VolunteerTaskManager.razor` - Manage volunteer tasks
- `EmailTemplateEditor.razor` - Email template management
- `OrdersTable.razor` - Display current orders
- `VolunteerTicketCard.razor` - Volunteer ticket assignment

#### 3.3 Update Styling
- Match wireframe design system colors and spacing
- Implement status badges (confirmed, pending, cancelled)
- Add proper form validation indicators
- Ensure responsive design

### Phase 4: Integration

#### 4.1 API Endpoints
Add/update endpoints in EventsController:
- `POST /api/events/{id}/volunteer-tasks` - Create task
- `PUT /api/events/{id}/volunteer-tasks/{taskId}` - Update task
- `DELETE /api/events/{id}/volunteer-tasks/{taskId}` - Delete task
- `POST /api/events/{id}/volunteer-tasks/{taskId}/assign` - Assign volunteer
- `POST /api/events/{id}/email-templates` - Save email template
- `POST /api/events/{id}/send-email` - Send custom email
- `POST /api/events/{id}/volunteer-tickets` - Create volunteer ticket

#### 4.2 Database Migrations
- Add new tables for volunteer tasks, assignments, email templates
- Update Events table with new columns
- Add appropriate indexes and foreign keys

### Phase 5: Testing

#### 5.1 Unit Tests
- Test all new service methods
- Test validation rules for new fields
- Test email template variable replacement

#### 5.2 Integration Tests
- Test volunteer task CRUD operations
- Test email sending functionality
- Test volunteer ticket creation

#### 5.3 E2E Tests with Puppeteer
Comprehensive test suite covering:
- Creating event with all fields in all tabs
- Editing existing event
- Managing volunteer tasks
- Sending test emails
- Assigning volunteer tickets
- Verifying all data saves correctly
- Checking public event page shows correct information

## Success Criteria

1. All 4 tabs match wireframe specifications exactly
2. All fields from wireframes are implemented and functional
3. Data persists correctly to database
4. Changes in admin are reflected on public events page
5. Email functionality works with variable replacement
6. Volunteer management fully functional
7. All tests pass with >80% coverage
8. UI matches wireframe design system

## Timeline

- Phase 1 (Backend): 4 hours
- Phase 2 (Services): 2 hours  
- Phase 3 (UI): 6 hours
- Phase 4 (Integration): 2 hours
- Phase 5 (Testing): 4 hours

Total estimated time: 18 hours

## Next Steps

1. Start with backend model updates
2. Create database migrations
3. Implement service layer changes
4. Build UI components following wireframes
5. Write comprehensive tests
6. Verify with visual testing tools
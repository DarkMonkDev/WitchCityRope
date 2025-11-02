# Venue Management Feature
<!-- Last Updated: 2025-11-02 -->
<!-- Version: 1.0 -->
<!-- Owner: Admin Team -->
<!-- Status: In Development -->

## Overview
Admin venue management system for WitchCityRope. Enables administrators to manage event venues through a dedicated settings card with CRUD operations.

## Feature Description
The venue management feature provides administrators with the ability to create, edit, and manage event venues. This feature is integrated into the Admin Settings area and includes:

- Venue dropdown selection
- CRUD form for venue details
- Soft delete with IsActive flag
- Integration with event creation/editing forms

## Key Components

### Backend (API)
- **Database Table**: Venues (Name, Directions, Notes, IsActive)
- **API Endpoints**: Minimal API vertical slice pattern
- **Business Logic**: Soft delete via IsActive flag
- **Seed Data**: 3 default venues (Main Studio, Community Space, Outdoor Space)

### Frontend (React)
- **Admin Settings Card**: Dedicated venue management interface
- **Venue Dropdown**: Select existing venues for editing
- **CRUD Form**: Create/update venue information
- **Event Integration**: Venue selection in event creation/editing

## Related Features
- **Events Management**: Venues are selected during event creation and editing
- **Admin Settings**: Venue management lives in admin settings area alongside other admin tools

## Current Status
**Phase**: In Development
**Status**: Planning & Requirements

### Work Tracking
- **Requirements**: `/docs/functional-areas/venue-management/requirements/venue-management-requirements.md`
- **Handoffs**: `/docs/functional-areas/venue-management/handoffs/`

## Technical Architecture

### Database Design
- **Table Name**: Venues
- **Schema**: public (standard application schema)
- **Key Fields**:
  - Id (PK)
  - Name (string, required)
  - Directions (text, optional)
  - Notes (text, optional)
  - IsActive (boolean, default true)
  - CreatedAt (timestamp)
  - UpdatedAt (timestamp)

### Implementation Approach
- **Clean Implementation**: No migration cleanup needed
- **Soft Delete**: Use IsActive flag instead of hard deletes
- **Vertical Slice**: Follow established API architecture pattern
- **Seed Data**: 3 recommended default venues

## Success Criteria
- [ ] 3 seed venues available on fresh installation
- [ ] Admins can create new venues
- [ ] Admins can edit existing venues
- [ ] Admins can soft delete venues (IsActive = false)
- [ ] Event forms show venue dropdown with active venues only
- [ ] Deleted venues don't appear in event forms
- [ ] Event history preserves venue information (even if venue deleted)

## Development Phases

### Phase 1: Requirements & Design (Current)
- Business requirements documentation
- Database schema design
- API endpoint specification
- UI wireframes

### Phase 2: Backend Implementation
- Database entity creation
- Seed data implementation
- API endpoints (CRUD operations)
- Backend unit tests

### Phase 3: Frontend Implementation
- Admin settings venue card
- Venue management form
- Event form integration
- Frontend tests

### Phase 4: Testing & Integration
- End-to-end testing
- Integration with existing events
- Performance validation
- User acceptance testing

### Phase 5: Deployment
- Database migration execution
- Production deployment
- Documentation updates
- User training materials

## Documentation Index

### Requirements
- [Venue Management Requirements](./requirements/venue-management-requirements.md) - Complete business requirements

### Handoffs
All agent handoff documents are stored in `/docs/functional-areas/venue-management/handoffs/`:
1. `01-database-design.md` - Database design to backend API
2. `02-backend-api.md` - Backend API to UI design
3. `03-ui-design.md` - UI design to frontend implementation
4. `04-frontend-implementation.md` - Frontend to backend tests
5. `05-backend-tests.md` - Backend tests to frontend tests
6. `06-frontend-tests.md` - Frontend tests to test execution
7. `07-test-execution.md` - Test execution to deployment
8. `08-e2e-tests.md` - E2E testing (future phase)

## Related Documentation
- **Events Management**: `/docs/functional-areas/events/`
- **Admin Settings**: `/docs/functional-areas/admin-settings/` (if exists)
- **Database Standards**: `/docs/standards-processes/backend/database-migrations-guide.md`
- **Vertical Slice Guide**: `/docs/standards-processes/backend/vertical-slice-implementation-guide.md`

## Notes
- Implementation follows established vertical slice architecture
- Clean implementation without complex migration concerns
- Soft delete pattern preserves referential integrity with events
- Seed data provides immediate value on fresh installations

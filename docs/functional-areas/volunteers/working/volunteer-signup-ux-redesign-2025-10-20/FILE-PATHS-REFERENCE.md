# Volunteer Signup Feature - File Paths Reference
<!-- Created: 2025-10-20 -->
<!-- Purpose: Complete file path reference for volunteer signup UX redesign -->
<!-- Owner: Librarian Agent -->
<!-- Status: Active -->

## Overview
This document provides all file paths for the volunteer signup feature on the public events page for UX redesign work.

---

## 1. Public Events Page React Component

**Primary Component**:
```
/home/chad/repos/witchcityrope/apps/web/src/pages/events/EventDetailPage.tsx
```

**Description**: Main event detail page that displays volunteer positions section after event policies.

**Key Imports**:
- Line 18-21: Volunteer-related imports (hooks, components, types)
- Line 26: Selected volunteer position state
- Line 31: Volunteer positions data hook

---

## 2. Volunteer Signup Modal/Components

### 2.1 Position Card Component
```
/home/chad/repos/witchcityrope/apps/web/src/features/volunteers/components/VolunteerPositionCard.tsx
```

**Description**: Volunteer position summary card with:
- Progress bar showing filled vs total slots
- Badges: "Signed Up", "Full", "X left"
- Description preview
- Click to open modal

### 2.2 Position Detail Modal
```
/home/chad/repos/witchcityrope/apps/web/src/features/volunteers/components/VolunteerPositionModal.tsx
```

**Description**: Volunteer position detail modal with:
- Full position details
- Signup form with notes textarea
- Auto-RSVP notification for guests
- Mantine notifications for feedback
- Anonymous/authenticated user handling

### 2.3 React Hooks
```
/home/chad/repos/witchcityrope/apps/web/src/features/volunteers/hooks/useVolunteerPositions.ts
```

**Description**: React Query hook for:
- Fetching volunteer positions
- Caching volunteer data
- Automatic cache invalidation after signup

### 2.4 TypeScript Types
```
/home/chad/repos/witchcityrope/apps/web/src/features/volunteers/types/volunteer.types.ts
```

**Description**: TypeScript interfaces matching backend DTOs:
- `VolunteerPosition` - Position details with user signup status
- `VolunteerSignupRequest` - Signup payload with optional notes
- `VolunteerSignup` - Complete signup record

---

## 3. Volunteer Task API Endpoints

### 3.1 API Endpoints File
```
/home/chad/repos/witchcityrope/apps/api/Features/Volunteers/Endpoints/VolunteerEndpoints.cs
```

**Description**: Minimal API endpoints:
- `GET /api/volunteers/events/{eventId}` - Get volunteer positions for event with user signup status
- `POST /api/volunteers/signup` - Sign up for volunteer position with auto-RSVP logic

### 3.2 API Client (Frontend)
```
/home/chad/repos/witchcityrope/apps/web/src/features/volunteers/api/volunteerApi.ts
```

**Description**: Frontend API client functions:
- `getEventVolunteerPositions(eventId)` - Fetch positions
- `signupForVolunteerPosition(request)` - Submit signup

### 3.3 Service Layer
```
/home/chad/repos/witchcityrope/apps/api/Features/Volunteers/Services/VolunteerService.cs
```

**Description**: Business logic layer:
- `GetEventVolunteerPositionsAsync()` - Auth-aware filtering
- `SignupForPositionAsync()` - Auto-RSVP creation logic

---

## 4. Volunteer Task Database Models/DTOs

### 4.1 Entity Models

**VolunteerPosition Model**:
```
/home/chad/repos/witchcityrope/apps/api/Models/VolunteerPosition.cs
```

**Description**: Database entity for volunteer positions with:
- Event relationship
- Position details (Title, Description, SlotsNeeded)
- `IsPublicFacing` boolean for public vs admin-only positions
- One-to-many relationship with VolunteerSignups

**VolunteerSignup Model**:
```
/home/chad/repos/witchcityrope/apps/api/Models/VolunteerSignup.cs
```

**Description**: Database entity for volunteer signups:
- User and Position relationships
- Status (Confirmed/CheckedIn/Completed/Cancelled/NoShow)
- SignedUpAt timestamp
- Optional Notes field
- CheckIn/Completion tracking

### 4.2 API DTOs

**Volunteer DTOs**:
```
/home/chad/repos/witchcityrope/apps/api/Features/Volunteers/Models/VolunteerModels.cs
```

**Description**: API data transfer objects:
- `VolunteerPositionDto` - Position with user signup status and slots filled count
- `VolunteerSignupRequest` - Signup payload (PositionId, EventId, Notes)
- `VolunteerSignupDto` - Complete signup details for response

**Event DTOs** (Legacy - may be deprecated):
```
/home/chad/repos/witchcityrope/apps/api/Features/Events/Models/VolunteerPositionDto.cs
```

**Note**: This is an older DTO that may be superseded by the Volunteers feature DTOs.

---

## 5. Admin Components (Event Management Context)

### 5.1 Admin Event Form
```
/home/chad/repos/witchcityrope/apps/web/src/components/events/EventForm.tsx
```

**Description**: Admin event creation/edit form that manages volunteer positions (separate from public signup).

### 5.2 Volunteer Position Form Modal
```
/home/chad/repos/witchcityrope/apps/web/src/components/events/VolunteerPositionFormModal.tsx
```

**Description**: Admin modal for creating/editing volunteer positions.

### 5.3 Volunteer Positions Grid
```
/home/chad/repos/witchcityrope/apps/web/src/components/events/VolunteerPositionsGrid.tsx
```

**Description**: Admin grid for displaying volunteer positions in event management.

---

## 6. Database Migrations

**Migration File**:
```
/home/chad/repos/witchcityrope/apps/api/Migrations/20251020080341_AddVolunteerSignupsAndIsPublicFacing.cs
```

**Description**: Migration that added:
- `VolunteerSignups` table
- `IsPublicFacing` column to `VolunteerPositions` table

**Database Context**:
```
/home/chad/repos/witchcityrope/apps/api/Data/ApplicationDbContext.cs
```

**Description**: EF Core DbContext with `VolunteerSignups` DbSet.

---

## 7. Seed Data

**Seed Service**:
```
/home/chad/repos/witchcityrope/apps/api/Services/SeedDataService.cs
```

**Description**: Seeds test data including:
- 8 test volunteer signups across multiple events
- Volunteer positions with `IsPublicFacing` flags

---

## 8. Dependency Injection Configuration

**Service Registration**:
```
/home/chad/repos/witchcityrope/apps/api/Features/Shared/Extensions/ServiceCollectionExtensions.cs
```

**Description**: Registers `VolunteerService` in DI container.

**Endpoint Mapping**:
```
/home/chad/repos/witchcityrope/apps/api/Features/Shared/Extensions/WebApplicationExtensions.cs
```

**Description**: Maps volunteer endpoints with `MapVolunteerEndpoints()`.

---

## 9. Implementation Documentation

**Implementation Entry in File Registry**:
```
/home/chad/repos/witchcityrope/docs/architecture/file-registry.md
```

**Lines**: 10-30 (Volunteer Signup System entries from 2025-10-20)

**Description**: Complete file registry entries documenting all created/modified files for the volunteer signup feature.

---

## 10. Related Functional Areas

**Events Functional Area**:
```
/home/chad/repos/witchcityrope/docs/functional-areas/events/
```

**Description**: Events functional area contains volunteer-related documentation as volunteers are part of the events domain.

**Key Documents**:
- Business requirements: `/docs/functional-areas/events/new-work/2025-08-24-events-management/requirements/volunteers-staff-tab-analysis.md`
- Implementation planning: Various docs in events management folder

---

## Feature Context

### Implementation Status
- **Backend**: COMPLETE ✅
- **Frontend**: COMPLETE ✅
- **Database**: COMPLETE ✅
- **Integration**: COMPLETE ✅
- **Testing**: Manual testing complete (screenshots in /test-results/)

### Key Features Implemented
1. ✅ Public volunteer position listing on event details page
2. ✅ Anonymous and authenticated signup flows
3. ✅ Auto-RSVP creation for guests who sign up
4. ✅ Position capacity tracking (slots filled/total)
5. ✅ Optional notes field for signups
6. ✅ Real-time UI updates after signup
7. ✅ Mantine notification feedback
8. ✅ `IsPublicFacing` flag for admin-only positions

### UX Design Patterns
- **Card-based layout**: Similar to event card grid patterns
- **Modal workflow**: Detail view in modal (not inline)
- **Progress indicators**: Progress bars for position capacity
- **Status badges**: Visual feedback for signup states
- **Responsive design**: Works on mobile/tablet/desktop

### Business Logic
- **Auto-RSVP**: Guests who sign up for volunteer positions automatically get RSVP created
- **Capacity Management**: Prevents overfilling positions
- **Authentication-aware**: Different flows for guests vs logged-in users
- **Public/Admin Filtering**: Only public-facing positions shown to non-admins

---

## File Organization Strategy

This feature follows the **domain-based organization principle**:
- **Primary Domain**: Events (volunteers are event-specific)
- **Subfolder**: Could be organized as `/docs/functional-areas/events/volunteers/` per documentation standards
- **Current Structure**: `/docs/functional-areas/volunteers/` exists as standalone (acceptable for cross-cutting features)

### Recommendation
For future volunteer documentation, consider:
- `/docs/functional-areas/events/volunteers/` - For event-specific volunteer features
- `/docs/functional-areas/volunteers/` - For cross-event volunteer management (if/when implemented)

---

## Next Steps for UX Redesign

1. **Review Current Implementation**: Test the live feature at http://localhost:5173
2. **Identify Pain Points**: Document UX issues or improvement opportunities
3. **Design Iteration**: Create wireframes/mockups for improvements
4. **Implementation**: Update React components based on approved designs
5. **Testing**: Validate UX improvements with user feedback

---

## Questions or Issues?

- **File Registry**: `/home/chad/repos/witchcityrope/docs/architecture/file-registry.md`
- **Functional Area Index**: `/home/chad/repos/witchcityrope/docs/architecture/functional-area-master-index.md`
- **Librarian Agent**: Available for file discovery and documentation organization

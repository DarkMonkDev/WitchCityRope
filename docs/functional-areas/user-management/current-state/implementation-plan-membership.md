# Admin Members Management - Implementation Plan

## Overview
This document outlines the comprehensive plan for implementing the Admin Members Management feature for WitchCityRope. The feature provides administrators with a powerful interface to manage the entire member database, including viewing, searching, filtering, and editing member information.

## Phase 1: Planning & Design (Days 1-2)

### 1.1 Requirements Analysis
**Primary Requirements:**
- Paginated member list with 100 default items, sorted by scene name
- Table columns: Scene Name, Real Name, FetLife Name, Email, Date Joined, Events Attended, Membership Status
- Clickable rows leading to member detail page
- Pagination controls (10, 50, 100, 500 per page)
- Vetting status filter (dropdown with "All" option, default to vetted)
- Real-time search on scene name, real name, and FetLife name
- Sortable columns
- Alternating row backgrounds
- Consistent design with existing site

**Detail Page Requirements:**
- Edit member information
- View past events (sorted by date)
- Show ticket purchase/refund status
- Show RSVP vs actual attendance
- Change membership status
- Notes area (converted from vetting-specific to general notes)
- Linked incidents with report links

### 1.2 Technical Design Decisions
- **UI Framework**: Blazor Server with Syncfusion DataGrid
- **API Design**: RESTful endpoints with pagination, filtering, sorting
- **Database Changes**: Convert vetting notes to general notes table
- **Search Implementation**: Server-side filtering with debounced input
- **State Management**: Component state with API backing

### 1.3 Database Schema Changes
```sql
-- Rename VettingNotes to UserNotes
ALTER TABLE VettingNotes RENAME TO UserNotes;

-- Add columns for general note tracking
ALTER TABLE UserNotes 
ADD COLUMN NoteType VARCHAR(50) DEFAULT 'Vetting',
ADD COLUMN CreatedById UUID NOT NULL,
ADD COLUMN IsDeleted BOOLEAN DEFAULT FALSE,
ADD COLUMN DeletedAt TIMESTAMP,
ADD COLUMN DeletedById UUID;

-- Add indexes for performance
CREATE INDEX idx_usernotes_userid ON UserNotes(UserId);
CREATE INDEX idx_usernotes_notetype ON UserNotes(NoteType);
CREATE INDEX idx_usernotes_createdat ON UserNotes(CreatedAt);
```

### 1.4 API Endpoints Design
```
GET /api/admin/members
  ?page=1
  &pageSize=100
  &sortBy=sceneName
  &sortDirection=asc
  &search=searchTerm
  &vettingStatus=vetted|unvetted|all
  
GET /api/admin/members/{id}
GET /api/admin/members/{id}/events
GET /api/admin/members/{id}/notes
GET /api/admin/members/{id}/incidents

PUT /api/admin/members/{id}
POST /api/admin/members/{id}/notes
DELETE /api/admin/members/{id}/notes/{noteId}
```

## Phase 2: Core Implementation (Days 3-4)

### 2.1 Domain Model Updates
- Create `UserNote` entity to replace vetting-specific notes
- Update `WitchCityRopeUser` relationships
- Add DTOs for member list and detail views
- Create value objects for search and filter criteria

### 2.2 Repository Layer
- Implement `IMemberRepository` with pagination support
- Add complex query methods for filtering and searching
- Optimize queries with proper includes for performance
- Add event attendance aggregation queries

### 2.3 Service Layer
- Create `MemberManagementService` for business logic
- Implement search algorithm with multi-field matching
- Add note management functionality
- Create event history compilation service

### 2.4 API Controllers
- Implement `AdminMembersController` with all endpoints
- Add proper authorization checks
- Implement request validation
- Add response caching where appropriate

## Phase 3: Integration & UI (Days 5-6)

### 3.1 Blazor Components Structure
```
/Pages/Admin/Members/
  - Index.razor (Main list page)
  - Index.razor.cs (Code-behind)
  - Index.razor.css (Scoped styles)
  - MemberDetail.razor (Detail/edit page)
  - MemberDetail.razor.cs
  - MemberDetail.razor.css
  
/Components/Admin/Members/
  - MemberDataGrid.razor (Syncfusion grid wrapper)
  - MemberSearchBar.razor
  - MemberFilters.razor
  - MemberNotes.razor
  - MemberEventHistory.razor
  - MemberIncidents.razor
```

### 3.2 UI Implementation Details

#### Members List Page Features:
1. **Syncfusion DataGrid Configuration**
   - Enable sorting on all columns
   - Custom column templates for complex data
   - Row click event handling
   - Alternating row styles
   - Responsive design

2. **Search Implementation**
   - Debounced input (300ms delay)
   - Server-side search with highlighting
   - Loading indicators during search
   - Clear search functionality

3. **Pagination Controls**
   - Page size selector dropdown
   - Page navigation buttons
   - Current page indicator
   - Total records display

4. **Filter Controls**
   - Vetting status dropdown
   - Additional filters (future expansion)
   - Filter state persistence

#### Member Detail Page Features:
1. **Information Sections**
   - Basic Information (editable)
   - Account Status (role, vetting, active)
   - Contact Information
   - Privacy Settings

2. **Event History Table**
   - Sortable by date
   - Status indicators (attended, no-show, refunded)
   - Link to event details
   - Export functionality

3. **Notes Management**
   - Add new notes with rich text
   - Edit existing notes (with history)
   - Delete notes (soft delete)
   - Note type categorization

4. **Incident Reports**
   - List of linked incidents
   - Severity indicators
   - Direct links to full reports
   - Status tracking

### 3.3 State Management
- Use component state for UI interactions
- API calls for data persistence
- Loading states for all async operations
- Error handling with user feedback

## Phase 4: Testing & Refinement (Days 7-8)

### 4.1 Unit Tests
- Domain model tests
- Service layer tests
- API controller tests
- Repository tests with in-memory database

### 4.2 Integration Tests
- API endpoint integration tests
- Database migration tests
- Search and filter functionality tests
- Pagination edge cases

### 4.3 E2E Puppeteer Tests
```javascript
// Test scenarios to implement:
1. Load members list and verify default state
2. Search for members by different fields
3. Change pagination size and navigate pages
4. Sort by different columns
5. Filter by vetting status
6. Click through to member detail
7. Edit member information
8. Add and delete notes
9. View event history
10. Check responsive behavior
```

### 4.4 Performance Testing
- Load testing with 10,000+ member records
- Search performance optimization
- Query optimization verification
- UI responsiveness testing

## Success Criteria
1. ✅ Members list loads with proper pagination
2. ✅ Search filters results in real-time
3. ✅ All columns are sortable
4. ✅ Member details page shows complete information
5. ✅ Notes can be added, edited, and deleted
6. ✅ Event history displays accurately
7. ✅ All tests pass with 80%+ coverage
8. ✅ Performance meets requirements (<2s load time)
9. ✅ UI matches existing design system
10. ✅ Accessibility standards met (WCAG 2.1 AA)

## Risk Mitigation
1. **Performance Risk**: Large dataset queries
   - Mitigation: Implement database indexes and query optimization
   
2. **UI Complexity**: Syncfusion DataGrid configuration
   - Mitigation: Start with basic grid, add features incrementally
   
3. **Data Migration**: Converting notes table
   - Mitigation: Create reversible migration with backup

## Dependencies
- Syncfusion Blazor license (already available)
- PostgreSQL database access
- Existing authentication/authorization system
- Current admin layout and styling

## Timeline
- **Days 1-2**: Planning & Design (this document + UI mockups)
- **Days 3-4**: Core Implementation (backend + domain)
- **Days 5-6**: UI Implementation (Blazor components)
- **Days 7-8**: Testing & Refinement
- **Day 9**: Documentation & Deployment

Total estimated time: 9 days
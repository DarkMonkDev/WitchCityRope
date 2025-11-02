# Venue Management Requirements
<!-- Last Updated: 2025-11-02 -->
<!-- Version: 1.0 -->
<!-- Owner: Business Requirements Agent -->
<!-- Status: Draft -->

## Business Context
WitchCityRope hosts events at multiple venues. Currently, venue information is entered manually for each event. This feature provides centralized venue management to improve consistency, reduce data entry errors, and streamline event creation.

## Objectives
1. **Centralize Venue Data**: Single source of truth for venue information
2. **Reduce Data Entry**: Select from dropdown instead of typing venue details repeatedly
3. **Improve Accuracy**: Consistent directions and notes across all events at same venue
4. **Enable Management**: Admins can update venue information in one place
5. **Preserve History**: Maintain venue information even if venue becomes inactive

## User Stories

### As an Administrator
- **I want to** create new venues **so that** I can add them to the system when we start using new locations
- **I want to** edit venue information **so that** I can update directions or notes when details change
- **I want to** deactivate venues **so that** they don't appear in event forms but preserve historical data
- **I want to** see all venues (active and inactive) **so that** I can manage the complete venue list
- **I want to** reactivate venues **so that** I can restore venues that we start using again

### As an Event Organizer
- **I want to** select from existing venues **so that** I don't have to re-type venue information for every event
- **I want to** see venue directions and notes **so that** I can share accurate information with attendees
- **I want to** only see active venues in dropdown **so that** I'm not confused by old/inactive locations

## Functional Requirements

### FR-1: Venue Data Model
**Priority**: CRITICAL
**Description**: Define the venue data structure

#### Fields
| Field Name | Type | Required | Description | Default |
|------------|------|----------|-------------|---------|
| Id | Integer (PK) | Yes | Unique identifier | Auto-increment |
| Name | String(200) | Yes | Venue display name | N/A |
| Directions | Text | No | How to get to venue | Null |
| Notes | Text | No | Additional venue information | Null |
| IsActive | Boolean | Yes | Soft delete flag | true |
| CreatedAt | DateTime | Yes | Record creation timestamp | Current UTC |
| UpdatedAt | DateTime | Yes | Last update timestamp | Current UTC |

#### Business Rules
- **Name uniqueness**: Venue names must be unique (case-insensitive)
- **Soft delete only**: Never hard delete venues (preserve event history)
- **Required name**: Cannot create venue without a name
- **Optional details**: Directions and Notes are helpful but not required

### FR-2: Seed Data
**Priority**: HIGH
**Description**: Provide default venues on fresh installation

#### Default Venues
1. **Main Studio**
   - **Name**: "Main Studio"
   - **Directions**: "Enter through main entrance, studio is on second floor"
   - **Notes**: "Capacity: 30 people. Parking available in adjacent lot."
   - **IsActive**: true

2. **Community Space**
   - **Name**: "Community Space"
   - **Directions**: "Community center basement, enter through rear entrance"
   - **Notes**: "Capacity: 50 people. Street parking only."
   - **IsActive**: true

3. **Outdoor Space**
   - **Name**: "Outdoor Space"
   - **Directions**: "Park pavilion in southwest corner"
   - **Notes**: "Weather-dependent. Backup location: Main Studio"
   - **IsActive**: true

#### Seed Data Rules
- Run seed data on database initialization only (don't re-run on migrations)
- Check if venues exist before creating (avoid duplicates)
- Use explicit IDs or let database auto-generate

### FR-3: Admin Venue Management Interface
**Priority**: CRITICAL
**Description**: Admin settings card for venue CRUD operations

#### UI Components
1. **Venue Dropdown**
   - Shows all active venues alphabetically
   - Includes "Create New Venue" option
   - Includes "Show Inactive Venues" toggle

2. **Venue Form**
   - Name input (required, max 200 chars)
   - Directions textarea (optional)
   - Notes textarea (optional)
   - IsActive checkbox
   - Save button
   - Cancel button
   - Delete button (soft delete)

3. **Admin Settings Integration**
   - Lives in admin settings area alongside other admin tools
   - Only visible to Admin role
   - Follows existing admin UI patterns (Mantine v7 components)

#### User Workflows
**Create New Venue:**
1. Admin selects "Create New Venue" from dropdown
2. Form appears blank with all fields empty
3. Admin enters venue name (required)
4. Admin optionally enters directions and notes
5. Admin clicks Save
6. System validates name uniqueness
7. New venue appears in dropdown

**Edit Existing Venue:**
1. Admin selects venue from dropdown
2. Form populates with venue data
3. Admin modifies fields
4. Admin clicks Save
5. System updates venue
6. Success message displays

**Deactivate Venue:**
1. Admin selects venue from dropdown
2. Admin unchecks IsActive checkbox OR clicks Delete button
3. System confirms deactivation
4. Venue removed from active list
5. Venue preserved in database (soft delete)
6. Events using this venue retain venue information

### FR-4: Event Form Integration
**Priority**: CRITICAL
**Description**: Integrate venue selection into event creation/editing

#### Event Form Changes
- Replace manual venue input fields with venue dropdown
- Dropdown shows active venues only (IsActive = true)
- Dropdown sorted alphabetically
- Event saves VenueId (foreign key reference)
- Event displays full venue information (Name, Directions, Notes)

#### Business Rules
- Events can have zero or one venue (nullable foreign key)
- Cannot delete venue if events reference it (soft delete only)
- Event history shows venue information even if venue deactivated
- Venue changes don't affect existing events (denormalization acceptable for Name/Directions/Notes)

### FR-5: API Endpoints
**Priority**: CRITICAL
**Description**: RESTful API for venue management

#### Endpoints
| Method | Endpoint | Description | Access |
|--------|----------|-------------|--------|
| GET | `/api/venues` | Get all active venues | Authenticated |
| GET | `/api/venues/all` | Get all venues (including inactive) | Admin only |
| GET | `/api/venues/{id}` | Get single venue | Authenticated |
| POST | `/api/venues` | Create new venue | Admin only |
| PUT | `/api/venues/{id}` | Update venue | Admin only |
| DELETE | `/api/venues/{id}` | Soft delete venue (set IsActive=false) | Admin only |

#### Response DTOs
```csharp
// VenueDto
{
  "id": 1,
  "name": "Main Studio",
  "directions": "Enter through main entrance...",
  "notes": "Capacity: 30 people...",
  "isActive": true,
  "createdAt": "2025-11-02T10:00:00Z",
  "updatedAt": "2025-11-02T10:00:00Z"
}

// CreateVenueRequest
{
  "name": "Main Studio",
  "directions": "Enter through main entrance...",
  "notes": "Capacity: 30 people..."
}

// UpdateVenueRequest
{
  "name": "Main Studio",
  "directions": "Updated directions...",
  "notes": "Updated notes...",
  "isActive": true
}
```

#### Validation Rules
- **Name**: Required, max 200 characters, unique (case-insensitive)
- **Directions**: Optional, max 2000 characters
- **Notes**: Optional, max 2000 characters
- **IsActive**: Required boolean

#### Error Responses
- `400 Bad Request`: Validation errors (duplicate name, missing required fields)
- `401 Unauthorized`: Not authenticated
- `403 Forbidden`: Not admin role
- `404 Not Found`: Venue ID doesn't exist
- `500 Internal Server Error`: Database errors

## Non-Functional Requirements

### NFR-1: Performance
- Venue list API response: < 100ms
- Create/update operations: < 200ms
- Support up to 100 venues without performance degradation

### NFR-2: Security
- All venue management endpoints require Admin role
- Venue list (active only) available to authenticated users
- Input validation on all fields
- SQL injection protection (use parameterized queries)
- XSS protection (sanitize text inputs)

### NFR-3: Usability
- Venue dropdown searchable/filterable
- Form validation with clear error messages
- Confirmation dialog for soft delete
- Success notifications for all actions
- Responsive design (works on tablet/mobile)

### NFR-4: Data Integrity
- Foreign key constraints to Events table
- Cascade rules: SET NULL on venue soft delete (optional)
- Transaction support for multi-step operations
- Audit trail (CreatedAt, UpdatedAt timestamps)

### NFR-5: Maintainability
- Follow vertical slice architecture pattern
- Unit tests for all business logic
- Integration tests for API endpoints
- E2E tests for admin UI workflows
- Comprehensive code documentation

## Acceptance Criteria

### AC-1: Database Implementation
- [ ] Venues table created with all required fields
- [ ] Indexes created for performance (Name, IsActive)
- [ ] 3 seed venues created on fresh installation
- [ ] Foreign key relationship to Events table
- [ ] Migration successfully runs on test database

### AC-2: API Implementation
- [ ] All 6 API endpoints implemented
- [ ] Proper authorization on admin-only endpoints
- [ ] Validation errors return 400 with detailed messages
- [ ] Soft delete sets IsActive=false (not hard delete)
- [ ] API returns proper DTOs (not raw entities)

### AC-3: Admin UI Implementation
- [ ] Venue dropdown shows active venues alphabetically
- [ ] Create form validates required name field
- [ ] Edit form populates with existing venue data
- [ ] Delete button confirms before soft delete
- [ ] Success/error notifications display properly
- [ ] UI follows Mantine v7 design patterns

### AC-4: Event Integration
- [ ] Event creation form includes venue dropdown
- [ ] Event editing form includes venue dropdown
- [ ] Event details display venue information
- [ ] Deactivated venues don't appear in dropdown
- [ ] Existing events retain venue info after venue deactivation

### AC-5: Testing
- [ ] Unit tests for venue service (CRUD operations)
- [ ] Integration tests for API endpoints
- [ ] Frontend component tests for venue form
- [ ] E2E tests for complete admin workflow
- [ ] Test coverage > 80% for new code

## Technical Constraints
- **Architecture**: Follow vertical slice pattern
- **Database**: PostgreSQL (public schema)
- **ORM**: Entity Framework Core
- **API**: .NET Minimal API
- **Frontend**: React 18 + TypeScript + Mantine v7
- **State Management**: TanStack Query for API calls
- **Type Safety**: Use NSwag-generated TypeScript types

## Out of Scope (Future Enhancements)
- Venue categories/tags
- Venue capacity management (separate from notes)
- Venue photos/images
- Venue availability calendar
- Venue contact information
- GPS coordinates/maps integration
- Multi-venue events
- Venue booking/reservation system

## Dependencies
- **Events Management**: Event entity needs VenueId foreign key
- **Admin Settings**: UI pattern established for admin cards
- **Authentication**: Admin role authorization working
- **Type Generation**: NSwag pipeline operational

## Risk Assessment

### High Risk
- **Event Data Migration**: Existing events may have manual venue text that needs migration
  - **Mitigation**: VenueId is nullable, allow gradual migration

### Medium Risk
- **Name Uniqueness**: Case-insensitive uniqueness might miss edge cases
  - **Mitigation**: Database constraint + application-level validation

### Low Risk
- **Soft Delete Complexity**: IsActive flag adds query complexity
  - **Mitigation**: Include `.Where(v => v.IsActive)` in queries

## Success Metrics
- [ ] 3 seed venues available after fresh installation
- [ ] Admins can create/edit/delete venues in < 5 clicks
- [ ] Event creation time reduced by 30% (estimated)
- [ ] Zero data entry errors for venue information
- [ ] 100% of new events use venue dropdown (not manual entry)

## Assumptions
1. Admin users understand soft delete (venue still exists, just inactive)
2. Venue information changes infrequently (no complex history tracking needed)
3. Events reference a single venue (no multi-venue events)
4. Manual venue text in existing events is acceptable (no forced migration)
5. Directions and Notes are freeform text (no structured address fields)

## Questions for Stakeholders
1. Should we migrate existing event venue text to venue records?
2. Do we need venue capacity as a structured field (vs. notes)?
3. Should inactive venues be visible to non-admin users in event details?
4. What should happen if an event's venue is deleted? (Currently: preserve info)
5. Do we need audit history for venue changes? (Currently: only UpdatedAt)

## Glossary
- **Soft Delete**: Setting IsActive=false instead of removing record from database
- **Venue**: Physical location where WitchCityRope events are held
- **Seed Data**: Default data created during database initialization
- **Vertical Slice**: Architecture pattern organizing code by feature (not layer)
- **DTO**: Data Transfer Object - object for API requests/responses

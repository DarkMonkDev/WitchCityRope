# Functional Specification: Admin User Management System
<!-- Last Updated: 2025-08-12 -->
<!-- Version: 2.0 -->
<!-- Owner: Functional Spec Agent -->
<!-- Status: Updated after Stakeholder Feedback -->

## Technical Overview

A comprehensive admin user management system built in Blazor Server with vertical slice architecture. The system provides administrators with tools to view, filter, edit, and manage user accounts through a modern data grid interface with detailed user management capabilities.

**Key Technical Changes (v2.0):**
- Status field consolidation: Single enum replacing Status/Vetted dual fields
- Modern sidebar navigation layout for admin sections
- Enhanced user detail page with always-visible admin notes panel
- Streamlined component structure with reduced modal usage

## Architecture

### Component Structure
```
/Features/Admin/Users/
├── Pages/
│   ├── UserManagement.razor              # Main list page
│   └── UserDetail.razor                  # User detail page with tabs
├── Components/
│   ├── UserDataGrid.razor                # Main data grid component
│   ├── UserStatsCards.razor              # Statistics cards
│   ├── UserFilterPanel.razor             # Advanced filtering
│   ├── AdminNotesPanel.razor             # Always-visible notes (right panel)
│   ├── EditUserModal.razor               # User profile editing modal
│   └── Tabs/
│       ├── UserOverviewTab.razor         # User overview information
│       ├── UserEventsTab.razor           # Event participation history
│       ├── UserVettingTab.razor          # Vetting management
│       └── UserAuditTrailTab.razor       # Audit history
├── Layout/
│   └── AdminLayout.razor                 # Admin sidebar navigation layout
├── Services/
│   ├── IUserManagementService.cs
│   └── UserManagementService.cs
├── Models/
│   ├── UserManagementDtos.cs
│   ├── UserListItemDto.cs
│   └── UserDetailDto.cs
└── Validators/
    └── UserManagementValidator.cs
```

### Service Architecture
- **UserManagementService**: Central service for all user operations
- **Direct EF Core**: No repository pattern, direct DbContext usage
- **HTTP Client**: Web → API communication pattern
- **Result Pattern**: Consistent error handling and validation

## Data Models

### Database Schema Changes

#### Status Field Consolidation
```sql
-- Migration: Consolidate Status and IsVetted fields
ALTER TABLE auth.Users 
ADD COLUMN Status varchar(50) NOT NULL DEFAULT 'PendingReview';

-- Data migration
UPDATE auth.Users 
SET Status = CASE 
    WHEN IsVetted = true THEN 'Vetted'
    ELSE 'PendingReview'
END;

-- Drop old column after migration
ALTER TABLE auth.Users DROP COLUMN IsVetted;

-- Add index for efficient filtering
CREATE INDEX IX_Users_Status ON auth.Users(Status);
```

### Enums

#### UserStatus (Consolidated)
```csharp
public enum UserStatus
{
    PendingReview,    // New user, not yet reviewed
    Vetted,          // Approved and vetted member
    NoApplication,   // No vetting application submitted
    OnHold,          // Temporarily suspended/on hold
    Banned           // Permanently banned
}
```

### DTOs and ViewModels

#### UserListItemDto
```csharp
public class UserListItemDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string SceneName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public UserStatus Status { get; set; }  // Consolidated field
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public int EventsAttended { get; set; }
    public bool HasPendingNotes { get; set; }
}
```

#### UserDetailDto
```csharp
public class UserDetailDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string SceneName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PronouncedName { get; set; } = string.Empty;
    public string Pronouns { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public UserStatus Status { get; set; }  // Consolidated field
    public DateTime DateOfBirth { get; set; }
    public int Age => CalculateAge();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; }
    
    // Stats
    public int EventsAttended { get; set; }
    public int EventsCreated { get; set; }
    public int IncidentReports { get; set; }
    
    // Collections
    public List<UserEventDto> Events { get; set; } = new();
    public List<AdminNoteDto> AdminNotes { get; set; } = new();
    public List<VettingApplicationDto> VettingApplications { get; set; } = new();
    public List<UserAuditLogDto> AuditTrail { get; set; } = new();
}
```

#### UserStatsDto
```csharp
public class UserStatsDto
{
    public int TotalUsers { get; set; }
    public int PendingVetting { get; set; }    // Status = PendingReview
    public int OnHold { get; set; }            // Status = OnHold
}
```

## API Specifications

### Endpoints

| Method | Path | Description | Request | Response |
|--------|------|-------------|---------|----------|
| GET | /api/admin/users | Get paginated user list | UserListRequest | PagedResult<UserListItemDto> |
| GET | /api/admin/users/{id} | Get user details | - | UserDetailDto |
| PUT | /api/admin/users/{id} | Update user | UpdateUserRequest | UserDetailDto |
| PUT | /api/admin/users/{id}/status | Update user status | UpdateStatusRequest | ApiResponse |
| DELETE | /api/admin/users/{id} | Soft delete user | - | ApiResponse |
| GET | /api/admin/users/stats | Get user statistics | - | UserStatsDto |
| POST | /api/admin/users/{id}/notes | Add admin note | CreateNoteRequest | AdminNoteDto |
| GET | /api/admin/users/{id}/audit | Get audit trail | - | List<UserAuditLogDto> |

### Request/Response Models

#### UserListRequest
```csharp
public class UserListRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;  // Default to 50 records
    public string? SearchTerm { get; set; }
    public UserRole? RoleFilter { get; set; }
    public UserStatus? StatusFilter { get; set; }  // Consolidated status filter
    public bool? IsActiveFilter { get; set; }
    public string SortBy { get; set; } = "CreatedAt";
    public string SortDirection { get; set; } = "desc";
}
```

#### UpdateStatusRequest
```csharp
public class UpdateStatusRequest
{
    public UserStatus Status { get; set; }
    public string Reason { get; set; } = string.Empty;
}
```

## Component Specifications

### Main Components

#### UserManagement.razor
- **Path**: `/admin/users`
- **Authorization**: Administrator, Moderator
- **Layout**: AdminLayout with sidebar navigation
- **Render Mode**: InteractiveServer
- **Key Features**:
  - Statistics cards (3 cards: TotalUsers, PendingVetting, OnHold)
  - Advanced filtering panel
  - Syncfusion DataGrid with 50 records per page
  - Quick action buttons
  - Real-time updates

#### UserDetail.razor
- **Path**: `/admin/users/{id}`
- **Authorization**: Administrator, Moderator
- **Layout**: AdminLayout with sidebar navigation
- **Key Features**:
  - User overview (not in tabs, always visible)
  - AdminNotesPanel always visible on right side
  - Tabbed interface: Overview, Events, Vetting, Audit Trail
  - Edit user modal
  - Status change tracking

### Data Grid Configuration

#### UserDataGrid.razor
```csharp
// Columns Configuration
var columns = new[]
{
    new { Field = "FirstName", HeaderText = "First Name", Width = 120 },      // NEW COLUMN
    new { Field = "SceneName", HeaderText = "Scene Name", Width = 150 },
    new { Field = "Email", HeaderText = "Email", Width = 200 },
    new { Field = "Role", HeaderText = "Role", Width = 100 },
    new { Field = "Status", HeaderText = "Status", Width = 120 },             // Consolidated field
    new { Field = "CreatedAt", HeaderText = "Joined", Width = 120, Format = "d" },
    new { Field = "EventsAttended", HeaderText = "Events", Width = 80 },
    new { Field = "Actions", HeaderText = "", Width = 100 }
};

// Removed LastLogin column as requested
// Default page size: 50 records
```

### Layout Changes

#### AdminLayout.razor
```razor
<div class="admin-layout">
    <aside class="admin-sidebar">
        <nav class="admin-nav">
            <a href="/admin" class="nav-link">Dashboard</a>
            <a href="/admin/users" class="nav-link">User Management</a>
            <a href="/admin/events" class="nav-link">Events</a>
            <a href="/admin/incidents" class="nav-link">Incidents</a>
            <a href="/admin/reports" class="nav-link">Reports</a>
        </nav>
    </aside>
    <main class="admin-content">
        @Body
    </main>
</div>
```

### Component Restructuring

#### Removed Components (v2.0)
- ❌ UserNotesModal.razor (replaced with always-visible AdminNotesPanel)
- ❌ VettingStatusModal.razor (integrated into UserVettingTab)

#### New/Modified Components (v2.0)
- ✅ AdminNotesPanel.razor (always visible on right side)
- ✅ EditUserModal.razor (consolidated user editing)
- ✅ AdminLayout.razor (sidebar navigation)

## State Management

### Component State
- **Reactive State**: User list updates automatically on changes
- **Cascading Parameters**: Current user, permissions
- **Event Callbacks**: Status changes, note additions
- **Local State**: Filter settings, pagination, sort preferences

### Data Flow
```
UserManagement.razor
├── UserStatsCards (displays: TotalUsers, PendingVetting, OnHold)
├── UserFilterPanel (filters by consolidated Status enum)
└── UserDataGrid (shows FirstName, removes LastLogin)

UserDetail.razor
├── User Overview (always visible, not in tabs)
├── AdminNotesPanel (always visible on right)
└── Tabs Container
    ├── Overview Tab
    ├── Events Tab  
    ├── Vetting Tab (handles status changes)
    └── Audit Trail Tab
```

## Integration Points

### Authentication System
- ASP.NET Core Identity integration
- Role-based authorization (Administrator, Moderator)
- Audit trail for all user changes

### Email Notifications
- Status change notifications
- Account suspension alerts
- Vetting decision updates

### Event Management
- Event participation history
- Registration management
- Attendance tracking

### Incident Management
- User incident reports
- Safety violations
- Community guidelines enforcement

## Security Requirements

### Authorization Rules
```csharp
[Authorize(Roles = "Administrator,Moderator")]
public class UserManagementController : ControllerBase
{
    // Moderators can view and update status
    // Only Administrators can delete users
    
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> DeleteUser(Guid id) { }
}
```

### Data Validation
- **Input Sanitization**: All user inputs sanitized
- **Status Transitions**: Validated business rules for status changes
- **Age Verification**: 21+ requirement enforced
- **Email Validation**: RFC-compliant email validation

### Audit Requirements
- All user changes logged with timestamp and admin user
- Status change reasons required
- IP address and user agent tracking
- Data retention policies enforced

## Performance Requirements

### Response Time Targets
- User list loading: <2 seconds (50 records per page)
- User detail page: <1 second
- Status updates: <500ms
- Search/filter operations: <1 second

### Optimization Strategies
- **Pagination**: Default 50 records, configurable up to 200
- **Database Indexes**: Status, Role, CreatedAt, SceneName
- **Caching**: User statistics cached for 5 minutes
- **Lazy Loading**: Event history and audit trails loaded on-demand

### Scalability
- Concurrent admin users: 10+
- Total user records: 10,000+
- Real-time updates via SignalR
- Database query optimization

## Testing Requirements

### Unit Tests (80% Coverage)
```csharp
// Service Tests
UserManagementServiceTests
├── GetUsersAsync_WithStatusFilter_ReturnsFilteredResults()
├── UpdateUserStatusAsync_ValidTransition_UpdatesSuccessfully()
├── UpdateUserStatusAsync_InvalidTransition_ThrowsException()
└── GetUserStatsAsync_ReturnsCorrectCounts()

// Component Tests  
UserDataGridTests
├── Render_WithUsers_DisplaysCorrectColumns()
├── StatusFilter_WithConsolidatedEnum_FiltersCorrectly()
└── Pagination_DefaultPageSize_Shows50Records()
```

### Integration Tests
- API endpoint functionality
- Database operations
- Authentication/authorization
- Email notification triggers

### E2E Tests (Playwright)
- Admin user list navigation
- User detail page functionality  
- Status change workflows
- Filter and search operations
- Admin notes functionality

## Migration Requirements

### Database Migration
```sql
-- Phase 1: Add new Status column
ALTER TABLE auth.Users ADD COLUMN Status varchar(50);

-- Phase 2: Data migration script
UPDATE auth.Users SET Status = CASE 
    WHEN IsVetted = true AND IsActive = true THEN 'Vetted'
    WHEN IsVetted = false AND IsActive = true THEN 'PendingReview'  
    WHEN IsActive = false THEN 'OnHold'
    ELSE 'NoApplication'
END;

-- Phase 3: Make Status column required
ALTER TABLE auth.Users ALTER COLUMN Status SET NOT NULL;
ALTER TABLE auth.Users ALTER COLUMN Status SET DEFAULT 'PendingReview';

-- Phase 4: Add index and drop old column
CREATE INDEX IX_Users_Status ON auth.Users(Status);
ALTER TABLE auth.Users DROP COLUMN IsVetted;
```

### Code Migration
- Update all DTOs to use consolidated Status field
- Modify service layer methods
- Update component bindings
- Migrate existing filter logic

### Backward Compatibility
- API versioning for gradual migration
- Feature flags for progressive rollout
- Database rollback scripts prepared

## Dependencies

### NuGet Packages Required
```xml
<!-- Already included -->
<PackageReference Include="Syncfusion.Blazor.Grid" />
<PackageReference Include="Syncfusion.Blazor.Notifications" />
<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" />

<!-- Additional packages needed -->
<PackageReference Include="Microsoft.AspNetCore.SignalR" />
<PackageReference Include="FluentValidation.AspNetCore" />
```

### External Services
- PostgreSQL database
- SMTP email service
- SignalR for real-time updates

### Configuration
```json
{
  "UserManagement": {
    "DefaultPageSize": 50,
    "MaxPageSize": 200,
    "StatsCache": {
      "ExpirationMinutes": 5
    },
    "StatusTransitions": {
      "AllowedTransitions": {
        "PendingReview": ["Vetted", "OnHold", "Banned", "NoApplication"],
        "Vetted": ["OnHold", "Banned"],
        "OnHold": ["Vetted", "PendingReview", "Banned"],
        "NoApplication": ["PendingReview", "Vetted", "OnHold", "Banned"],
        "Banned": []
      }
    }
  }
}
```

## Acceptance Criteria

### Technical Criteria for Completion

#### Database & Backend
- [ ] Status field consolidation migration completed
- [ ] All API endpoints return consolidated Status field
- [ ] UserManagementService uses single Status field
- [ ] Database indexes created for performance
- [ ] Audit logging captures all status changes

#### User Interface  
- [ ] AdminLayout with sidebar navigation implemented
- [ ] UserDataGrid shows FirstName column, removes LastLogin
- [ ] Default pagination set to 50 records per page
- [ ] Statistics cards reduced to 3: TotalUsers, PendingVetting, OnHold
- [ ] AdminNotesPanel always visible (not modal)
- [ ] User detail page: overview not in tabs, tabs for Events/Vetting/Audit

#### Functionality
- [ ] Status filtering works with new enum values
- [ ] Status change validation enforces business rules
- [ ] Admin notes system functional with audit trail
- [ ] User search and filtering performance meets targets
- [ ] Real-time updates work for status changes

#### Testing & Quality
- [ ] Unit tests achieve 80% coverage
- [ ] Integration tests pass for all API endpoints  
- [ ] E2E tests cover major user workflows
- [ ] Performance benchmarks met (<2s page load, <1s searches)
- [ ] Security audit completed (authorization, input validation)

#### Documentation & Training
- [ ] Technical documentation updated for new Status field
- [ ] Admin user guide updated with new UI
- [ ] Migration runbook prepared and tested
- [ ] Rollback procedures documented

### Definition of Done
All acceptance criteria met, code reviewed, tests passing, deployed to staging environment, and stakeholder approval received for the consolidated status field approach and new admin UI layout.
# Dashboard Features Inventory - WitchCityRope Legacy API

**Date**: September 13, 2025  
**Purpose**: Complete inventory of all Dashboard features in legacy API for extraction decisions  
**Investigation Scope**: Legacy API (`/src/WitchCityRope.Api/`) and archived Blazor Web application

## Executive Summary

The legacy WitchCityRope API contains limited dashboard functionality focused on user-specific dashboards rather than administrative analytics/oversight tools. Most sophisticated dashboard features exist in the archived Blazor Web application and are not implemented in the current API.

## Complete Feature Inventory

### 1. CURRENT API DASHBOARD FEATURES

#### A. User Dashboard Controller (`/src/WitchCityRope.Api/Features/Dashboard/DashboardController.cs`)

**Found Features:**

##### 1.1 Basic User Dashboard (`GET /api/dashboard/{userId}`)
- **Description**: Returns basic user profile information
- **Data Sources**: `Users`, `VettingApplications`
- **User Roles**: User (own dashboard) or Administrator  
- **Complexity**: Low
- **Code Example**:
```csharp
public async Task<IActionResult> GetDashboard(Guid userId)
{
    var dashboard = new DashboardDto
    {
        SceneName = user.SceneName.Value,
        Role = user.Role,
        VettingStatus = vettingStatus
    };
    return Ok(dashboard);
}
```

##### 1.2 User Upcoming Events (`GET /api/dashboard/users/{userId}/upcoming-events`)
- **Description**: Shows user's next 3 upcoming events they're registered for
- **Data Sources**: `Registrations`, `Events`
- **User Roles**: User (own events) or Administrator
- **Complexity**: Low
- **Code Example**:
```csharp
var upcomingEvents = await _context.Registrations
    .Include(r => r.Event)
    .Where(r => r.UserId == userId && 
               r.Event.StartDate > DateTime.UtcNow &&
               r.Status != RegistrationStatus.Cancelled)
    .OrderBy(r => r.Event.StartDate)
    .Take(count)
```

##### 1.3 User Membership Statistics (`GET /api/dashboard/users/{userId}/stats`)
- **Description**: Shows user's membership activity statistics
- **Data Sources**: `Users`, `Registrations`, `VettingApplications`
- **User Roles**: User (own stats) or Administrator
- **Complexity**: Medium
- **Statistics Provided**:
  - Events attended (past events with Confirmed status)
  - Months as member (calculated from user creation date)
  - Consecutive events (events in last 6 months)
  - Join date
  - Vetting status and next interview date (placeholder)
- **Code Example**:
```csharp
var eventsAttended = await _context.Registrations
    .CountAsync(r => r.UserId == userId && 
                    r.Event.EndDate < DateTime.UtcNow &&
                    r.Status == RegistrationStatus.Confirmed);

var monthsAsMember = (int)Math.Ceiling((DateTime.UtcNow - user.CreatedAt).TotalDays / 30);

var consecutiveEvents = await _context.Registrations
    .CountAsync(r => r.UserId == userId && 
                    r.Event.StartDate > DateTime.UtcNow.AddMonths(-6) &&
                    r.Status == RegistrationStatus.Confirmed);
```

### 2. ADMINISTRATIVE OVERSIGHT TOOLS (Limited Implementation)

#### 2.1 Event Management (Admin/Organizer Only)
**Location**: `/src/WitchCityRope.Api/Features/Events/EventsController.cs`
- **Create Events**: `[Authorize(Roles = "Organizer,Admin")]`
- **Manage Sessions**: `[Authorize(Roles = "Organizer,Admin")]`
- **Manage Ticket Types**: `[Authorize(Roles = "Organizer,Admin")]`
- **Update/Delete Events**: Via EventsManagementService
- **Complexity**: High
- **Business Rules**: Extensive authorization and capacity validation

#### 2.2 User Management (Basic)
**Location**: `/src/WitchCityRope.Api/Services/UserService.cs`
- **Get User Profiles**: Basic profile retrieval
- **Update Profiles**: Scene name updates only
- **Password Management**: Not implemented (placeholder)
- **Two-Factor Auth**: Not implemented (placeholder)
- **Complexity**: Low (mostly placeholders)

### 3. ARCHIVED BLAZOR WEB DASHBOARD FEATURES

**NOTE**: These features exist in the archived Blazor application (`/src/_archive/WitchCityRope.Web-blazor-legacy-2025-08-22/`) but are NOT implemented in the current API.

#### 3.1 Admin Dashboard (`/admin/dashboard`) ❌ NOT IN API
- **Key Metrics Cards**: Total members, active events, monthly revenue, pending applications
- **Revenue Chart**: 7/30/90 day views with interactive Syncfusion charts
- **Activity Feed**: Real-time updates on user actions and system events
- **System Health Monitoring**: API, database, payment, and email service status
- **Quick Actions Grid**: 6 common admin tasks
- **Complexity**: High
- **Dependencies**: Syncfusion Charts (eliminated in React migration)

#### 3.2 Financial Reports (`/admin/financial-reports`) ❌ NOT IN API
- **Revenue Analysis**: Track income by period with visual charts
- **Payment History**: Detailed transaction log with filtering
- **Event Revenue Breakdown**: Chart/table toggle views
- **Sliding Scale Payment Analysis**: Doughnut chart visualization
- **Refund Tracking**: Reason analysis
- **Export Functionality**: CSV, PDF, Excel (placeholders)
- **Financial KPIs**: Summary cards
- **Complexity**: Very High

#### 3.3 User Management Dashboard (`/admin/user-management`) ❌ NOT IN API
- **Advanced Search/Filter**: Multi-criteria user filtering
- **Role Management**: Assign and manage user permissions
- **Bulk Operations**: Export user lists, bulk communications
- **User Activity Tracking**: Detailed user behavior analytics
- **Suspension/Activation**: User account management
- **Complexity**: High

#### 3.4 Incident Management System (`/admin/incidents`) ❌ NOT IN API
- **Incident Lifecycle Management**: Complete workflow
- **Timeline View**: Chronological incident tracking
- **Severity Classification**: Multiple incident levels
- **Resolution Tracking**: Status and outcome management
- **Reporting Dashboard**: Incident analytics
- **Complexity**: High

## Feature Analysis by Category

### KEEP: Event Attendance Analytics ✅
**Current API Implementation**: Limited but foundation exists
- **User upcoming events** ✅ Implemented
- **User attendance statistics** ✅ Basic implementation
- **Event capacity tracking** ✅ In EventsManagementService

**Extraction Value**: HIGH
- Core business functionality
- Already partially implemented
- Direct user value
- Relatively low complexity

### DON'T KEEP: Financial Summaries ❌
**Current API Implementation**: None
- **Revenue charts** ❌ Not implemented
- **Payment analytics** ❌ Not implemented
- **Financial KPIs** ❌ Not implemented

**Extraction Value**: LOW
- Complex implementation required
- High maintenance overhead
- Would require significant new development
- Better handled by dedicated accounting/payment systems

### DON'T KEEP: User Activity Statistics (on dashboard) ❌
**Current API Implementation**: None for admin view
- **System-wide user activity** ❌ Not implemented
- **User behavior analytics** ❌ Not implemented
- **Activity feed** ❌ Not implemented

**Extraction Value**: LOW
- High complexity, low immediate business value
- Privacy concerns
- Better suited for separate reporting/analytics system

### KEEP: Administrative Oversight Tools ✅ (Partially)
**Current API Implementation**: Basic event management only
- **Event management** ✅ Implemented with proper authorization
- **User management** ⚠️ Partial implementation (mostly placeholders)
- **System monitoring** ❌ Not implemented

**Extraction Value**: MEDIUM
- **Event management**: Keep (already implemented)
- **Basic user operations**: Keep (profile updates)
- **Advanced user management**: Don't Keep (complex, not implemented)
- **System monitoring**: Don't Keep (better handled by infrastructure tools)

### DON'T KEEP: Community Engagement Metrics ❌
**Current API Implementation**: None
- **Community activity tracking** ❌ Not implemented
- **Engagement analytics** ❌ Not implemented
- **Social features metrics** ❌ Not implemented

**Extraction Value**: LOW
- Not implemented
- Complex to build
- Questionable business value
- Privacy considerations

## Detailed Code Examples

### User Dashboard Implementation
```csharp
// Basic dashboard data - SIMPLE TO EXTRACT
public class DashboardDto
{
    public string SceneName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public VettingStatus VettingStatus { get; set; }
}

// Membership statistics - MEDIUM COMPLEXITY
public class MembershipStatsDto
{
    public bool IsVerified { get; set; }
    public int EventsAttended { get; set; }
    public int MonthsAsMember { get; set; }
    public int ConsecutiveEvents { get; set; }
    public DateTime JoinDate { get; set; }
    public VettingStatus VettingStatus { get; set; }
    public DateTime? NextInterviewDate { get; set; }
}
```

### Event Management (Admin Tools)
```csharp
// Event CRUD with proper authorization - KEEP
[Authorize(Roles = "Organizer,Admin")]
public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequest request)

// Business rules validation - COMPLEX BUT VALUABLE
public async Task<(bool Success, EventDetailsDto? Response, string Error)> UpdateEventAsync(
    Guid eventId,
    UpdateEventRequest request,
    Guid userId,
    CancellationToken cancellationToken = default)
{
    // Authorization check: user must be organizer or admin
    if (!eventEntity.Organizers.Any(o => o.Id == userId) && user.Role != UserRole.Administrator)
    {
        return (false, null, "Not authorized to update this event");
    }
    
    // Business rule: Cannot update past events
    if (eventEntity.StartDate < DateTime.UtcNow)
    {
        return (false, null, "Cannot update past events");
    }
    
    // Business rule: Cannot reduce capacity below current attendance
    var currentAttendance = eventEntity.GetCurrentAttendeeCount();
    if (request.Capacity.HasValue && request.Capacity.Value < currentAttendance)
    {
        return (false, null, $"Cannot reduce capacity to {request.Capacity}. Current attendance is {currentAttendance}");
    }
}
```

## Extraction Recommendations

### ✅ EXTRACT (High Value, Low-Medium Complexity)

#### 1. User Dashboard Features
- **Basic dashboard**: Simple profile + vetting status display
- **Upcoming events**: User's next registered events
- **Membership statistics**: User's attendance history and membership metrics
- **Complexity**: Low to Medium
- **Business Value**: High (core user experience)
- **Implementation Status**: Already exists in API

#### 2. Event Management (Admin)
- **Event CRUD operations**: Create, update, delete events with proper authorization
- **Session management**: Multi-session event support
- **Ticket type management**: Complex pricing and capacity management
- **Complexity**: High (but already implemented)
- **Business Value**: Critical (core administrative functionality)
- **Implementation Status**: Fully implemented in EventsManagementService

### ❌ DON'T EXTRACT (High Complexity, Low Value)

#### 1. Financial Dashboard Features
- **Revenue analytics**: Complex financial reporting
- **Payment history dashboards**: Detailed transaction analysis
- **Financial KPIs**: Automated financial metrics
- **Reason**: High complexity, better handled by dedicated financial tools

#### 2. Advanced User Activity Analytics
- **System-wide activity feeds**: Real-time activity tracking
- **User behavior analytics**: Complex user interaction analysis
- **Community engagement metrics**: Social interaction measurements
- **Reason**: High complexity, privacy concerns, questionable business value

#### 3. System Monitoring Dashboard
- **Health monitoring**: API/database/service status
- **Performance metrics**: System performance tracking
- **Reason**: Better handled by dedicated infrastructure monitoring tools (DataDog, New Relic, etc.)

### ⚠️ NEEDS CLARIFICATION

#### 1. Basic User Management
- **Current State**: Mostly placeholder implementations
- **Question**: Do you want basic admin user operations (view profiles, update basic info) or full user management?
- **Options**: 
  - Keep basic profile viewing/updating (Low complexity)
  - Skip advanced user management features (High complexity)

#### 2. Incident Management
- **Current State**: Basic SafetyService exists but no dashboard
- **Question**: Is incident tracking part of "administrative oversight"?
- **Options**:
  - Extract basic incident reporting (Medium complexity)
  - Skip incident dashboard/analytics (High complexity)

## Alternative Approaches for Rejected Features

### Financial Reporting
- **Use external tools**: QuickBooks, Wave Accounting
- **Export to Excel**: Simple CSV exports from payment data
- **Third-party analytics**: Stripe Dashboard, PayPal reporting

### User Activity Analytics
- **Google Analytics**: Web-based user behavior tracking
- **Simple logging**: Basic audit logs for administrative actions
- **Scheduled reports**: Weekly/monthly summary emails

### System Monitoring
- **Infrastructure tools**: DataDog, New Relic, Uptime Robot
- **Simple health checks**: Basic API endpoint status
- **Log aggregation**: ELK stack or similar for error tracking

## Questions for User

1. **Administrative Oversight Scope**: What specific admin functions are most important to you?
   - User profile management (view/edit basic info)?
   - Event management (already implemented)?
   - System oversight (monitoring, logs)?

2. **User Dashboard Priority**: Which user dashboard features are most valuable?
   - Personal attendance history? ✅ (recommend keep)
   - Upcoming events? ✅ (recommend keep) 
   - Vetting status tracking? ✅ (recommend keep)

3. **Event Analytics Granularity**: For event attendance analytics, what level of detail do you need?
   - Individual user statistics? ✅ (already implemented)
   - Event-wide attendance summaries? (would need development)
   - Historical trend analysis? (complex, recommend external tool)

4. **Incident Management**: Is safety incident tracking part of administrative oversight tools you want?
   - Basic incident reporting? (medium complexity)
   - Incident analytics dashboard? (high complexity, recommend skip)

5. **Migration Budget**: How much development time can you allocate to dashboard features?
   - Low: Extract only existing API features (1-2 days)
   - Medium: Add basic admin user management (3-5 days) 
   - High: Build custom analytics (2+ weeks)

## Recommended Extraction Plan

### Phase 1: Essential Features (1-2 days)
1. **User Dashboard API**: Extract existing dashboard endpoints
2. **Event Management**: Extract existing admin event management
3. **Basic Authentication**: Ensure proper role-based access

### Phase 2: Enhancement (Optional, 2-3 days)
1. **Basic User Management**: Add simple admin user viewing/editing
2. **Event Analytics**: Add basic event attendance summaries
3. **Simple Reporting**: Add CSV export capabilities

### Phase 3: Future Considerations (Don't Extract Now)
1. **Financial Reporting**: Use external tools (QuickBooks, Stripe Dashboard)
2. **Advanced Analytics**: Use Google Analytics + simple logging
3. **System Monitoring**: Use infrastructure monitoring tools

## File Registry Entry

| Date | File Path | Action | Purpose | Session/Task | Status |
|------|-----------|--------|---------|--------------|--------|
| 2025-09-13 | `/docs/functional-areas/api-cleanup/new-work/2025-09-13-dashboard-extraction/dashboard-features-inventory.md` | CREATED | Complete inventory of legacy API dashboard features for extraction decisions | Dashboard feature investigation | ACTIVE |
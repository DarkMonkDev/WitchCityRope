# UI Wireframes: User Management Admin Screen Redesign
<!-- Last Updated: 2025-08-12 -->
<!-- Version: 2.0 -->
<!-- Owner: UI Designer Agent -->
<!-- Status: Updated per Stakeholder Feedback -->

## Design Overview

The User Management Admin Screen Redesign provides a comprehensive interface for administrators to manage community members efficiently. The design prioritizes desktop workflows (1920x1080) with dense information display, streamlined filtering, and integrated administrative actions. **Updated to include admin navigation structure and simplified user flows based on critical stakeholder feedback.**

### User Goals
- Navigate efficiently through admin areas with consistent left navigation
- Quickly find and filter users with above-table search/filter controls
- Access detailed user information with integrated notes and actions
- Perform administrative actions without complex modal workflows
- Manage vetting processes with streamlined tab interface

## User Personas

- **Admin**: System administrators managing platform with full access to all user management functions
- **Event Organizer**: Limited admin access focused on event-related user management
- **Super Admin**: Enhanced permissions for sensitive operations like permanent deletions

## Admin Layout Structure

The admin area features a consistent left navigation menu with main sections:
- **Dashboard**: Admin overview and statistics
- **User Management**: Current user management interface
- **Event Management**: Event creation and oversight
- **Vetting Queue**: Dedicated vetting workflow area  
- **Reports**: Analytics and compliance reporting
- **Settings**: System configuration and admin preferences

## Wireframes

### 1. User Management List Page
**File**: `/user-list-page.html`

```
+------------------------------------------------------------------+
|  [☰] Admin Navigation          User Management                   |
+------------------------------------------------------------------+
| Dashboard            |  [247]   [12]    [3]                      |
| User Management  ▶   | Total   Pending  On Hold                  |
| Event Management     |  Users  Vetting                          |
| Vetting Queue        +-------------------------------------------+
| Reports              | Search: [________________] [🔍]           |
| Settings             | Role: [All Roles ▼] Status: [All ▼]      |
|                      | [Clear Filters]                           |
| [Collapse ‹]         +-------------------------------------------+
+----------------------| First Name | Email | Role | Status | Actions |
                       | Jamie      | jamie@ | Teacher | Vetted | [👁][✏️] |
                       | Alex       | alex@  | Admin  | Vetted | [👁][✏️] |
                       | Sarah      | sarah@ | Member | Pending| [👁][✏️] |
                       |                                           |
                       | [‹] [1][2][3]...[10] [›] (50 per page)   |
                       +-------------------------------------------+
```

**Key Changes from Stakeholder Feedback**:
- **Admin Navigation**: Left sidebar with collapsible admin menu structure
- **Simplified Stats**: Only Total Users, Pending Vetting, On Hold displayed
- **Above-Table Filters**: Search and filters moved above the data table
- **50 Records Default**: Pagination shows 50 records per page by default
- **Updated Columns**: Added First Name, removed User initials and Last Login
- **Single Status Field**: Combined Status/Vetted into one field with values:
  - Pending Review, Vetted, No Application, On Hold, Banned

### 2. User Detail Page  
**File**: `/user-detail-page.html`

```
+------------------------------------------------------------------+
| [☰] Admin › User Management › Jamie Wilson                       |
+------------------------------------------------------------------+
| Dashboard            | Jamie Wilson (rope_jamie)                 |
| User Management  ▶   | First Name: Jamie | Last Name: Wilson     |
| Event Management     | jamie@example.com | [Edit User] [Reset Pass] |
| Vetting Queue        | [Teacher] [Vetted] | [Deactivate] [Send Email] |
| Reports              | Pronouns: She/Her | Member Since: Mar 2023  |
| Settings             +-------------------------------------------+
|                      | [Overview & Notes] [Events] [Vetting] [Audit] |
| [Collapse ‹]         +-------------------------------------------+
+----------------------| Account Information    | Admin Notes Panel  |
                       | Scene Name: rope_jamie | [Add Note]         |
                       | Email: jamie@...       | +----------------+ |
                       | Pronouns: She/Her      | | admin_sarah    | |
                       | Pronounced: JAY-mee    | | Jan 10: Excell-| |
                       |                        | | ent instructor | |
                       | Account Status         | +----------------+ |
                       | Role: Teacher          | | admin_mike     | |
                       | Active: ✓ Yes          | | Nov 8: Upgraded| |
                       | Confirmed: ✓ Yes       | | to Teacher     | |
                       | Status: Vetted         | +----------------+ |
                       +------------------------+--------------------+
```

**Key Changes from Stakeholder Feedback**:
- **Admin Layout**: Consistent left navigation with breadcrumb
- **Name Fields**: Added separate First Name and Last Name fields
- **No Avatar**: Removed circular avatar with user initials
- **Merged Actions**: All quick actions in single top-right location
- **Removed Actions**: Export Data and Send Message buttons removed
- **Combined View**: Overview and Notes shown on same page/tab
- **Updated Tabs**: Overview & Notes, Events, Vetting, Audit Trail
- **Admin Notes Panel**: Integrated sidebar panel (not modal)
- **No Modals**: Removed separate Admin Notes and Vetting Status modals
- **Edit User Modal**: New modal for editing user information

### 3. Edit User Modal
**File**: `/edit-user-modal.html`

```
Modal Overlay (backdrop blur)
+--------------------------------------------------+
| Edit User - Jamie Wilson                    [×] |
+--------------------------------------------------+
| Personal Information                             |
|                                                  |
| First Name: * [Jamie            ]               |
| Last Name: *  [Wilson           ]               |
| Scene Name:   [rope_jamie       ]               |
| Email: *      [jamie@example.com]               |
| Pronouns:     [She/Her          ]               |
|                                                  |
| Account Settings                                 |
|                                                  |
| Role: [Teacher           ▼]                     |
| Status: [Vetted        ▼]                       |
|                                                  |
| ☐ Email Confirmed                                |
| ☐ Account Active                                 |
| ☐ Two-Factor Enabled                             |
|                                                  |
|                               [Cancel] [Save]   |
+--------------------------------------------------+
```

**Component Specifications**:
- **Modal Dialog**: SfDialog with form validation and required field indicators
- **Form Fields**: SfTextBox for text inputs with validation states
- **Dropdowns**: SfDropDownList for role and status selections
- **Checkboxes**: SfCheckBox for boolean account settings
- **Validation**: Real-time validation with error states and messages

## Syncfusion Components Used

| Component | Purpose | Configuration |
|-----------|---------|--------------|
| SfSidebar | Admin navigation | Collapsible, persistent state |
| SfGrid | User data display | 50 records paging, sorting, filtering |
| SfButton | All actions | Primary, secondary, icon styles |
| SfDialog | Edit user modal | Form validation, backdrop |
| SfTextBox | Search and inputs | Debouncing, validation, placeholders |
| SfDropDownList | Filters and selectors | Single select, validation |
| SfTab | Detail page sections | Integrated notes display |
| SfTextArea | Admin notes | Character limits, auto-resize |
| SfChip | Status indicators | Color-coded, consistent sizing |
| SfToast | Feedback | Success, error, info notifications |

## Updated Interaction Patterns

### Admin Navigation
- **Persistent Sidebar**: Collapsible left navigation maintains state
- **Breadcrumb Trail**: Clear path indication for nested admin areas  
- **Active States**: Visual indication of current admin section
- **Mobile Responsive**: Collapses to hamburger menu on smaller screens

### Streamlined Filtering
- **Above-Table Position**: Filters positioned above data for better workflow
- **Combined Controls**: Search and filter controls in single row
- **Clear All**: One-click filter reset functionality
- **Real-time Updates**: Immediate results as filters are applied

### Integrated User Management
- **Single-Page Notes**: Admin notes integrated into main detail view
- **Tab-Based Organization**: Logical grouping without modal complexity
- **Quick Edit**: Edit User modal for fast information updates
- **Unified Actions**: All administrative actions in consistent locations

## Updated Responsive Design

**Desktop-Optimized Layout**: 
- **Minimum Width**: 1200px for full admin functionality
- **Sidebar Width**: 280px with collapse to 60px icon-only mode
- **Content Area**: Flexible layout adapting to sidebar state
- **Filter Row**: Responsive filter controls that stack on narrow screens

## Enhanced Accessibility

### Navigation Structure
- **Landmark Roles**: Clear navigation landmarks for screen readers
- **Skip Links**: Quick navigation to main content and key sections
- **Focus Management**: Logical tab order through admin interface
- **Keyboard Shortcuts**: Alt+N for navigation, Alt+S for search

### Content Organization  
- **Heading Hierarchy**: Proper H1-H6 structure throughout interface
- **Status Indicators**: Text alternatives for color-coded status badges
- **Form Labels**: Descriptive labels and ARIA attributes
- **Error Handling**: Clear error states and recovery instructions

## Updated Quality Checklist

- [x] Admin navigation structure implemented consistently
- [x] Simplified statistics display (3 cards instead of 6)
- [x] Above-table search and filtering positioned correctly  
- [x] 50-record pagination as default display
- [x] First Name column added, initials column removed
- [x] Last Login column removed from display
- [x] Single Status field with specified values implemented
- [x] User detail page shows First Name and Last Name separately
- [x] Avatar circle with initials removed from interface
- [x] Quick actions consolidated to single top-right location
- [x] Export Data and Send Message actions removed
- [x] Overview and Notes combined in single tab/view
- [x] Tab structure updated: Overview & Notes, Events, Vetting, Audit Trail
- [x] Admin Notes integrated into sidebar panel (not modal)
- [x] Vetting status management integrated into Vetting tab
- [x] Edit User modal created for user information changes

## Implementation Notes

### Admin Layout Integration
- **Navigation State**: Sidebar state persists across admin sections
- **Route Structure**: URL patterns reflect admin navigation hierarchy
- **Permission Checks**: UI elements hidden/shown based on admin role
- **Responsive Behavior**: Navigation adapts to screen size gracefully

### Data Grid Updates  
- **Column Configuration**: Updated grid columns per feedback requirements
- **Pagination Settings**: Default 50 records with configurable options
- **Filter Implementation**: Server-side filtering for performance
- **Status Field Logic**: Single field combining previous Status/Vetted columns

### Integrated User Detail
- **Tab Navigation**: SfTab component with integrated content areas
- **Notes Panel**: Real-time notes display and editing capability
- **Action Consolidation**: Streamlined action buttons in consistent locations
- **Modal Simplification**: Reduced modal complexity per feedback

Remember: Design maintains safety, consent, and community focus while implementing the streamlined admin workflow requested by stakeholders.
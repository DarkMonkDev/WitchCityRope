# User Management System Documentation

## Overview

This documentation covers the complete user management system for WitchCityRope, including user accounts, membership management, and the vetting process for community access.

## Documentation Structure

### Current State Documentation
- **[Vetting Status Guide](./current-state/VETTING_STATUS_GUIDE.md)** - Technical implementation guide for vetting status
- **[Completion Summary](./current-state/completion-summary-membership.md)** - Implementation status of membership features
- **[Database Changes](./current-state/database-changes-membership.md)** - Database schema changes for membership
- **[Implementation Plan](./current-state/implementation-plan-membership.md)** - Original implementation plan
- **[Technical Design](./current-state/technical-design-membership.md)** - Technical architecture design
- **[UI Design](./current-state/ui-design-admin-members-management.md)** - Admin interface design

### Business Requirements
- **[User Management Requirements](./business-requirements/user-management-requirements.md)** - Comprehensive business requirements
- **[Functional Specifications](./business-requirements/functional-specifications.md)** - Detailed functional specifications

### Wireframes
All user management related wireframes have been consolidated here:
- **admin-vetting-queue.html** - Admin interface for reviewing vetting applications
- **admin-vetting-review.html** - Detailed vetting review interface
- **vetting-application.html** - User-facing vetting application form
- **member-profile-settings-visual.html** - Member profile management interface
- **member-membership-settings.html** - Member membership settings
- **user-dashboard-visual.html** - User dashboard overview

## Key Features

### User Account Management
- User registration and authentication
- Profile management (scene name, pronouns, bio, contact info)
- Emergency contact management
- Social media links (FetLife, Discord)

### Membership Management
- Membership status tracking (Guest, Member, Vetted Member, Teacher, Admin)
- Membership history and transitions
- Access control based on membership level

### Vetting Process
- Application submission workflow
- Admin review and approval process
- Interview scheduling and tracking
- Vetting status management (NotApplied, Pending, Approved, Rejected)
- Integration with event access permissions

## System Integration

### Authentication & Authorization
- Integrates with ASP.NET Core Identity system
- Role-based access control
- Claims-based vetting status (`IsVetted` claim)

### Event Management Integration
- Vetting status controls access to social events
- RSVP restrictions based on membership level
- Event check-in integration

### Database Architecture
- User entities with membership properties
- Vetting application tracking
- Audit trail for membership changes

## Technical Implementation Notes

### Current Architecture
- **Web Service**: Blazor Server UI components
- **API Service**: RESTful API for user management operations
- **Database**: PostgreSQL with Entity Framework Core
- **Pattern**: Web → API → Database (never direct database access from Web)

### Key Technologies
- ASP.NET Core Identity for authentication
- Entity Framework Core for data access
- Blazor Server for interactive UI components
- Syncfusion components for UI (NO MudBlazor or other frameworks)

## Getting Started

1. Review the [User Management Requirements](./business-requirements/user-management-requirements.md)
2. Examine the [Current State Documentation](./current-state/) to understand existing implementation
3. Study the wireframes to understand the user experience flow
4. Check the [Vetting Status Guide](./current-state/VETTING_STATUS_GUIDE.md) for technical implementation details

## Related Documentation

- [Authentication System](../authentication/)
- [Event Management](../events-management/)
- [Admin Dashboard](../admin-dashboard/)
- [Security Guidelines](../../standards-processes/SECURITY.md)

---

**Last Updated**: 2025-08-12
**Responsible Team**: Development Team
**Documentation Status**: Consolidated and Current
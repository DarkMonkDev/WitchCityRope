# User Management System - Functional Specifications

## Document Information
- **Document Type**: Functional Specification Document
- **Version**: 1.0
- **Date**: 2025-08-12
- **Status**: Draft - Awaiting Review
- **Related Documents**: [User Management Requirements](./user-management-requirements.md)

## Overview

This document provides detailed functional specifications for the User Management System, translating business requirements into specific functional behaviors and user interactions.

## System Architecture

### Component Overview
```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Blazor Web    │    │   API Service   │    │   Database      │
│   (UI Layer)    │───▶│  (Business      │───▶│   (PostgreSQL)  │
│                 │    │   Logic)        │    │                 │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                       │
         │                       │
         ▼                       ▼
┌─────────────────┐    ┌─────────────────┐
│  Identity       │    │  Email          │
│  Service        │    │  Service        │
└─────────────────┘    └─────────────────┘
```

### Data Models

#### User Entity
```csharp
public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; }
    public string SceneName { get; set; }
    public string? Pronouns { get; set; }
    public string? Bio { get; set; }
    public string? PhoneNumber { get; set; }
    public string? FetLifeHandle { get; set; }
    public string? DiscordHandle { get; set; }
    public bool IsVetted { get; set; }
    public MembershipLevel MembershipLevel { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastLoginAt { get; set; }
    public List<EmergencyContact> EmergencyContacts { get; set; }
    public List<VettingApplication> VettingApplications { get; set; }
}

public enum MembershipLevel
{
    Guest = 0,
    GeneralMember = 1,
    VettedMember = 2,
    Teacher = 3,
    Administrator = 4
}
```

#### Vetting Application Entity
```csharp
public class VettingApplication
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string WhyJoin { get; set; }
    public string HowHeardAboutUs { get; set; }
    public string? OtherNames { get; set; }
    public VettingStatus Status { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedBy { get; set; }
    public string? ReviewNotes { get; set; }
    public bool IsFlagged { get; set; }
    public string? FlagReason { get; set; }
}

public enum VettingStatus
{
    NotApplied = 0,
    Pending = 1,
    Interview = 2,
    Approved = 3,
    Rejected = 4
}
```

## Functional Specifications

### FS-001: User Registration and Account Creation

#### User Story
"As a prospective member, I want to create an account so that I can access community features and apply for membership."

#### Functional Behavior
1. **Registration Form Fields**:
   - Email address (required, unique)
   - Password (required, minimum 8 characters, complexity requirements)
   - Confirm password (required, must match)
   - Preferred scene name (required, 2-50 characters)
   - Pronouns (optional, dropdown selection)
   - Agreement to terms of service (required checkbox)
   - Age verification (must be 21+)

2. **Validation Rules**:
   - Email format validation and uniqueness check
   - Password strength requirements (uppercase, lowercase, number, special char)
   - Scene name availability check
   - Terms agreement must be checked
   - Age verification required

3. **Registration Process**:
   - User submits registration form
   - System validates all fields
   - Email verification sent to provided address
   - Account created with "Guest" membership level
   - User redirected to dashboard with onboarding flow

#### API Endpoints
- `POST /api/auth/register` - Create new user account
- `POST /api/auth/verify-email` - Verify email address
- `GET /api/auth/check-email/{email}` - Check email availability
- `GET /api/auth/check-scene-name/{sceneName}` - Check scene name availability

#### Success Criteria
- User can successfully register with valid information
- Invalid registration attempts show appropriate error messages
- Email verification works correctly
- New users start with Guest membership level

### FS-002: User Profile Management

#### User Story
"As a member, I want to manage my profile information so that other community members can connect with me appropriately."

#### Functional Behavior
1. **Profile Information Sections**:
   - **Basic Info**: Scene name, pronouns, bio (500 char max)
   - **Contact Info**: Email, phone number (optional)
   - **Social Links**: FetLife handle, Discord handle (optional)
   - **Emergency Contacts**: Name, relationship, phone, contact preferences
   - **Privacy Settings**: Profile visibility, contact information sharing

2. **Profile Photo Management**:
   - Upload profile photo (JPG, PNG, GIF, max 5MB)
   - Automatic resizing and optimization
   - Default avatar with initials if no photo

3. **Emergency Contacts**:
   - Add multiple emergency contacts (minimum 1 recommended)
   - Contact types: Partner, Friend, Family, Medical
   - Contact preferences: Any emergency, Medical only, Safety concerns only
   - Remove/edit existing contacts

#### UI Components
- Profile settings page with tabbed interface
- Avatar upload component with preview
- Dynamic emergency contact cards with add/remove functionality
- Privacy controls with clear explanations

#### API Endpoints
- `GET /api/users/{id}/profile` - Get user profile
- `PUT /api/users/{id}/profile` - Update user profile
- `POST /api/users/{id}/avatar` - Upload profile photo
- `POST /api/users/{id}/emergency-contacts` - Add emergency contact
- `DELETE /api/users/{id}/emergency-contacts/{contactId}` - Remove emergency contact

#### Success Criteria
- Users can update all profile fields successfully
- Profile photo upload works with appropriate file validation
- Emergency contacts can be added, edited, and removed
- Privacy settings are respected throughout the application

### FS-003: Vetting Application Process

#### User Story
"As a member seeking vetted status, I want to submit a vetting application so that I can access social events."

#### Functional Behavior
1. **Application Eligibility**:
   - User must have General Member status or higher
   - User cannot have pending or recent rejected application
   - System checks eligibility before showing application form

2. **Application Form**:
   - Why do you want to join WCR? (required, 500-2000 characters)
   - How did you hear about us? (required)
   - Other names/handles used in kink contexts (optional)
   - FetLife profile (optional but recommended)
   - Community references (optional)
   - Agreement to vetting process and community standards

3. **Application Submission**:
   - Form validation before submission
   - Draft save functionality
   - Confirmation email sent to user
   - Application enters "Pending" status
   - Admin notifications sent

4. **Application Status Tracking**:
   - User dashboard shows current application status
   - Status updates: Pending → Interview → Approved/Rejected
   - Email notifications for status changes
   - Timeline view of application progress

#### Progress Indicator
```
[ ✓ Create Account ] → [ ✓ Submit Application ] → [ ? Under Review ] → [ ? Decision ]
```

#### API Endpoints
- `GET /api/vetting/eligibility/{userId}` - Check vetting eligibility
- `POST /api/vetting/applications` - Submit vetting application
- `GET /api/vetting/applications/{userId}` - Get user's application status
- `PUT /api/vetting/applications/{id}/draft` - Save application draft

#### Success Criteria
- Eligible users can submit vetting applications
- Application form validates all required fields
- Users receive confirmation of submission
- Application status is trackable by user

### FS-004: Admin Vetting Queue Management

#### User Story
"As an admin, I want to efficiently review and process vetting applications so that I can maintain community safety standards."

#### Functional Behavior
1. **Vetting Queue Interface**:
   - Tabular view of all pending applications
   - Sort by: submission date, applicant name, status, flagged status
   - Filter by: date range, status, flagged applications
   - Search by applicant name or email
   - Batch actions for common operations

2. **Application Review**:
   - Detailed application view with all submitted information
   - Community connection checks (FetLife, mutual connections)
   - Flag applications for special review
   - Add internal review notes
   - Schedule interviews if needed

3. **Decision Making**:
   - Approve application (grants vetted status)
   - Request interview (changes status to "Interview")
   - Reject application (with optional feedback)
   - Request additional information
   - Bulk approval for qualified applications

4. **Communication Tools**:
   - Template emails for common responses
   - Interview scheduling integration
   - Automated status update notifications
   - Internal notes visible only to admins

#### Queue Management Features
- **Priority Indicators**: Flagged, long-pending, interview scheduled
- **Workload Distribution**: Assign applications to specific admins
- **Performance Metrics**: Average review time, approval rates
- **Audit Trail**: Complete history of all actions taken

#### API Endpoints
- `GET /api/admin/vetting/queue` - Get vetting queue with filters
- `GET /api/admin/vetting/applications/{id}` - Get application details
- `PUT /api/admin/vetting/applications/{id}/status` - Update application status
- `POST /api/admin/vetting/applications/{id}/notes` - Add review notes
- `POST /api/admin/vetting/applications/{id}/flag` - Flag/unflag application

#### Success Criteria
- Admins can efficiently process application queue
- All vetting decisions are properly recorded
- Users receive timely status updates
- System maintains complete audit trail

### FS-005: Membership Level Management

#### User Story
"As an admin, I want to manage user membership levels so that I can grant appropriate access and recognize community contributions."

#### Functional Behavior
1. **Membership Level Display**:
   - Current level prominently displayed in user profiles
   - Badge/indicator system for different levels
   - Level-based feature access throughout application
   - Member directory with level filtering

2. **Level Promotion/Demotion**:
   - Admin interface for changing membership levels
   - Reason required for all level changes
   - Automatic level changes (e.g., successful vetting → Vetted Member)
   - Bulk level changes for multiple users
   - Notification to user of level changes

3. **Access Control Integration**:
   - Event access based on membership level
   - Feature visibility based on level
   - Admin function access based on level
   - API endpoint protection by level

4. **Membership History**:
   - Complete history of level changes for each user
   - Audit trail with dates, reasons, and admin who made change
   - Reporting on membership distribution and trends

#### Level-Based Permissions
```
Guest: Workshop events, limited profile features
General Member: All workshops, full profile features, member directory
Vetted Member: + Social events, private areas
Teacher: + Create/manage workshops, mentor features
Admin: + User management, system administration
```

#### API Endpoints
- `GET /api/admin/members` - Get member list with levels
- `PUT /api/admin/members/{id}/level` - Change membership level
- `GET /api/admin/members/{id}/history` - Get membership history
- `GET /api/admin/members/statistics` - Get membership statistics

#### Success Criteria
- Membership levels control access appropriately
- Level changes are properly tracked and audited
- Users understand their current level and privileges
- Admins can efficiently manage member levels

### FS-006: User Search and Directory

#### User Story
"As a member, I want to find and connect with other community members while respecting privacy preferences."

#### Functional Behavior
1. **Member Directory**:
   - Searchable list of community members
   - Filter by membership level, pronouns, interests
   - Privacy controls respect user preferences
   - Contact options based on user settings

2. **Search Functionality**:
   - Search by scene name, bio keywords, interests
   - Advanced filters for membership level, location
   - Fuzzy matching for partial name searches
   - Recently active members highlighted

3. **Profile Privacy**:
   - Users control profile visibility (public, members only, private)
   - Contact information sharing preferences
   - Opt-out of directory listing entirely
   - Social media link visibility controls

4. **Connection Features**:
   - Send messages through platform (if enabled)
   - Request contact information (with approval)
   - Community interest matching
   - Event attendance correlation

#### Privacy Levels
- **Public**: Visible to all logged-in users
- **Members Only**: Visible to General Members and above
- **Vetted Only**: Visible to Vetted Members and above
- **Private**: Not visible in directory

#### API Endpoints
- `GET /api/members/directory` - Get member directory with filters
- `GET /api/members/search` - Search members by criteria
- `GET /api/members/{id}/profile` - Get public profile (respects privacy)
- `POST /api/members/{id}/contact-request` - Request contact information

#### Success Criteria
- Members can find others while respecting privacy
- Search performance is fast and accurate
- Privacy controls are consistently enforced
- Directory encourages community connections

## Integration Requirements

### Event Management Integration
- Vetting status controls social event access
- Membership level determines event visibility
- RSVP permissions based on member status
- Check-in integration with member lookup

### Authentication System Integration
- Single sign-on with existing Identity system
- Role-based claims for membership levels
- Vetting status stored as claims
- Session management integration

### Email Service Integration
- Account verification emails
- Vetting status update notifications
- Administrative notifications
- Template-based email system

### Audit and Logging Integration
- All user management actions logged
- Privacy-compliant audit trails
- Performance monitoring integration
- Security event logging

## Performance Requirements

### Response Time Targets
- User profile loading: < 2 seconds
- Member directory search: < 3 seconds
- Vetting queue loading: < 4 seconds
- Profile updates: < 1 second

### Scalability Targets
- Support 1000+ concurrent users
- Handle 10,000+ member profiles
- Process 100+ vetting applications per month
- Maintain performance with 5x growth

### Availability Requirements
- 99.5% uptime during peak hours
- Graceful degradation during high load
- Database backup and recovery procedures
- Disaster recovery planning

## Security Specifications

### Data Protection
- All personal data encrypted at rest
- HTTPS for all communications
- Secure session management
- PII access logging

### Authentication Security
- Multi-factor authentication for admins
- Strong password requirements
- Account lockout after failed attempts
- Session timeout policies

### Authorization Controls
- Role-based access control throughout system
- API endpoint protection
- Feature-level permissions
- Administrative action authorization

### Privacy Compliance
- GDPR data export capabilities
- Right to be forgotten implementation
- Consent management for data processing
- Privacy policy integration

## Testing Requirements

### Unit Testing
- 90%+ code coverage for business logic
- All API endpoints tested
- Data validation testing
- Edge case and error condition testing

### Integration Testing
- End-to-end user workflows
- Cross-system integration testing
- Performance testing under load
- Security penetration testing

### User Acceptance Testing
- Community member beta testing
- Admin workflow validation
- Accessibility compliance testing
- Mobile device compatibility testing

---

**Document Control**
- **Author**: Development Team
- **Technical Reviewers**: Backend Team, Frontend Team
- **Business Reviewers**: Community Leadership
- **Next Review Date**: 2025-08-26
- **Approval Status**: Pending Review
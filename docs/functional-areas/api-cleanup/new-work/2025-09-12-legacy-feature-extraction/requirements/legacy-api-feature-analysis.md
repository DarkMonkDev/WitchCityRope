# Legacy API Feature Analysis
<!-- Created: 2025-09-12 -->
<!-- Status: IN PROGRESS -->
<!-- Owner: Backend Developer -->

## Executive Summary

This document provides a comprehensive analysis of features in the legacy API at `/src/WitchCityRope.Api/` to determine extraction priorities and archival decisions for the critical API consolidation project.

## Architecture Context

### Legacy API (Source - READ ONLY)
- **Location**: `/src/WitchCityRope.Api/`
- **Status**: DORMANT - Contains valuable features but not currently serving traffic
- **Architecture**: Traditional layered architecture with repositories and services
- **Technology**: ASP.NET Core API with full infrastructure patterns

### Modern API (Target - ACTIVE)
- **Location**: `/apps/api/`
- **Port**: 5655
- **Status**: ACTIVE - Currently serving React frontend
- **Architecture**: Vertical slice architecture with minimal API patterns
- **Performance**: <50ms response times

## Feature Inventory

### Features in Legacy API (Source)
1. **Auth** - Authentication and authorization system
2. **CheckIn** - Event check-in system for attendees
3. **Dashboard** - Administrative dashboard features
4. **Events** - Comprehensive event management system
5. **Payments** - Payment processing and financial transactions
6. **Safety** - Community safety protocols and reporting
7. **Vetting** - Member vetting and approval workflows

### Features in Modern API (Target)
1. **Authentication** - JWT-based auth (cookie implementation)
2. **Events** - Basic event CRUD operations
3. **Health** - API health checks
4. **Shared** - Common utilities and extensions
5. **Users** - User management operations

## Detailed Feature Analysis

### 1. CheckIn System (HIGH PRIORITY)

**Location**: `/src/WitchCityRope.Api/Features/CheckIn/`
**Business Purpose**: Event check-in process for attendees at community events
**Current Status**: NOT IMPLEMENTED in modern API

#### Key Components:
- **CheckInAttendeeCommand**: Comprehensive check-in logic with multiple identifier support
- **Multi-Modal Check-In**: Supports confirmation codes, QR codes, and manual user lookup
- **Business Rules**: Event timing validation, waiver requirements, staff permissions
- **Attendee Details**: Dietary restrictions, accessibility needs, emergency contacts, pronouns

#### Data Models:
```csharp
public record CheckInAttendeeCommand(
    Guid EventId,
    string? ConfirmationCode,    // Ticket confirmation code
    string? QrCodeData,          // QR code scanning
    Guid? UserId,                // Manual lookup
    Guid CheckedInBy,            // Staff member performing check-in
    bool OverrideRestrictions    // Administrative override
);

public record AttendeeDetails(
    string? DietaryRestrictions,
    string? AccessibilityNeeds,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? PronouncedName,
    string? Pronouns,
    bool IsFirstTimeAttendee,
    bool HasCompletedWaiver
);
```

#### Key Business Logic:
- ✅ Multiple check-in methods (QR, confirmation code, manual)
- ✅ Role-based access control (CheckInStaff, Organizer, Admin)
- ✅ Event timing validation (30-minute early window)
- ✅ Waiver requirement validation
- ✅ First-time attendee detection and special notes
- ✅ Comprehensive attendee information display

#### Dependencies:
- Registration system (links to ticket/RSVP data)
- User management (staff permissions, attendee details)
- Waiver system (validates waiver completion)
- QR code parsing service

#### Integration Points:
- Registration entities for attendance tracking
- Event entities for timing validation
- User entities for permissions and attendee info
- External QR code generation/scanning

#### Complexity: **HIGH**
- Complex business rules with multiple validation paths
- Multi-modal input handling (QR, codes, manual)
- Staff permission system integration
- Real-time event validation logic

---

### 2. Safety System (CRITICAL PRIORITY)

**Location**: `/src/WitchCityRope.Api/Features/Safety/`
**Business Purpose**: Community safety incident reporting and management
**Current Status**: NOT IMPLEMENTED in modern API

#### Key Components:
- **Incident Reporting**: Anonymous and identified incident reports
- **Safety Team Notifications**: Automated alerts based on severity
- **Encryption Services**: Sensitive information protection
- **Reference Number System**: Trackable incident identification

#### Data Models:
```csharp
public record SubmitIncidentReportCommand(
    Guid? ReporterId,           // Optional for anonymous reports
    IncidentType Type,          // SafetyViolation, ConsentViolation, etc.
    IncidentSeverity Severity,  // Low, Medium, High, Critical
    DateTime IncidentDate,
    string Location,
    string Description,         // Encrypted in storage
    List<string> InvolvedParties, // Encrypted
    List<string> Witnesses,      // Encrypted
    bool RequestFollowUp,
    bool IsAnonymous
);

public enum IncidentType
{
    SafetyViolation, ConsentViolation, EquipmentFailure,
    Injury, HarassmentOrBullying, PolicyViolation, Other
}
```

#### Key Business Logic:
- ✅ Anonymous reporting support (protects reporter identity)
- ✅ Severity-based notification escalation
- ✅ Automatic reference number generation (e.g., "CON-20250912-1234")
- ✅ Sensitive data encryption (descriptions, involved parties)
- ✅ Audit trail with timestamped actions
- ✅ On-call safety team immediate alerts for critical incidents

#### Critical Features:
- **Encryption**: All sensitive data encrypted at rest
- **Notification Urgency**: 
  - Critical/High Consent Violations → Immediate alerts
  - High Injuries → Immediate alerts
  - All others → Standard team notification
- **Audit Trail**: Complete action history with timestamps
- **Anonymous Protection**: Optional reporter identification

#### Dependencies:
- User management (reporter and safety team identification)
- Notification service (email/SMS alerts)
- Encryption service (data protection)
- Audit logging system

#### Integration Points:
- Safety team member management
- Alert/notification systems (email, SMS, phone)
- Document storage for supporting evidence
- Incident tracking and follow-up workflow

#### Complexity: **CRITICAL**
- Legal compliance requirements (data protection)
- Real-time notification system
- Sensitive data encryption/decryption
- Multi-stakeholder alert coordination

---

### 3. Vetting System (HIGH PRIORITY)

**Location**: `/src/WitchCityRope.Api/Features/Vetting/`
**Business Purpose**: Member vetting and community approval workflow
**Current Status**: NOT IMPLEMENTED in modern API

#### Key Components:
- **Application Submission**: Comprehensive vetting application
- **Reference System**: External reference validation
- **Review Process**: Vetting team application review
- **Status Management**: Multi-stage approval workflow

#### Data Models:
```csharp
public record SubmitApplicationCommand(
    Guid UserId,
    string ExperienceLevel,
    string ExperienceDescription,
    List<string> SkillsAndInterests,
    string SafetyKnowledge,
    string ConsentUnderstanding,
    List<Reference> References,    // Minimum 2 required
    string WhyJoin,
    bool AgreesToCodeOfConduct,
    bool AgreesToSafetyGuidelines
);

public record ReviewApplicationCommand(
    Guid ApplicationId,
    Guid ReviewerId,
    ReviewDecision Decision,       // Approve, Reject, RequestMoreInfo, ScheduleInterview
    int? SafetyScore,             // 1-10
    int? CommunityFitScore,       // 1-10
    string[]? Concerns
);
```

#### Key Business Logic:
- ✅ Minimum 2 references required for all applications
- ✅ External reference validation via secure tokens
- ✅ Multi-stage review process with scoring
- ✅ Interview scheduling for borderline cases
- ✅ Automated status updates and notifications
- ✅ Vetting team member role-based access

#### Workflow States:
1. **Submitted** → Initial application received
2. **UnderReview** → Being evaluated by vetting team
3. **PendingReferences** → Waiting for reference responses
4. **PendingInterview** → Interview required before decision
5. **Approved** → Member granted vetted status
6. **Rejected** → Application denied with reason

#### Reference System:
- External reference requests via secure email tokens
- Reference validation with safety/community ratings
- Automated follow-up for incomplete references
- Reference status tracking (Pending, Completed, Declined)

#### Dependencies:
- User management (applicant and reviewer permissions)
- Email service (reference requests, notifications)
- Role management (vetted member group assignment)

#### Integration Points:
- User profile system (vetted status updates)
- Event access control (vetted-only events)
- Email notification system
- Interview scheduling (future enhancement)

#### Complexity: **HIGH**
- Multi-stage workflow management
- External reference coordination
- Scoring and evaluation system
- Automated status transitions

---

### 4. Payments System (MEDIUM PRIORITY)

**Location**: `/src/WitchCityRope.Api/Features/Payments/`
**Business Purpose**: Payment processing for events, memberships, donations
**Current Status**: BASIC IMPLEMENTATION exists in modern API

#### Key Components:
- **Stripe Integration**: Full payment provider integration
- **Payment Method Management**: Saved cards, new cards
- **Transaction Processing**: Comprehensive payment handling
- **Post-Payment Actions**: Registration confirmation, membership extension

#### Data Models:
```csharp
public record ProcessPaymentCommand(
    Guid UserId,
    PaymentType Type,           // EventRegistration, MembershipFee, Merchandise
    decimal Amount,
    PaymentMethod Method,       // SavedCard, NewCard, BankTransfer
    PaymentMetadata Metadata    // Event/registration linking
);

public enum PaymentType
{
    EventRegistration, MembershipFee, Merchandise, Donation, Other
}
```

#### Key Business Logic:
- ✅ Multiple payment types with different post-processing
- ✅ Stripe customer management (automatic customer creation)
- ✅ Payment method storage and reuse
- ✅ Idempotent payment processing (prevents duplicate charges)
- ✅ Processing fee calculation and tracking
- ✅ Receipt generation and email delivery

#### Advanced Features:
- **Stripe Integration**: Full payment method lifecycle
- **Customer Management**: Automatic Stripe customer creation/linking
- **Idempotency**: Prevents duplicate charges using payment ID
- **Metadata Tracking**: Links payments to events, registrations, orders
- **Post-Payment Actions**: Automatic status updates based on payment type

#### Comparison with Modern API:
**Modern API Payment Status**: Basic payment endpoints exist but lack:
- Stripe customer management
- Payment method storage
- Post-payment automation
- Comprehensive metadata tracking
- Advanced error handling

#### Dependencies:
- Stripe service (payment processing)
- User management (customer linking)
- Registration system (payment confirmation)
- Email service (receipt delivery)

#### Integration Points:
- Event registration system
- Membership management
- Merchandise orders
- Email notification system
- Accounting/reconciliation systems

#### Complexity: **MEDIUM-HIGH**
- External payment provider integration
- Financial transaction handling
- PCI compliance considerations
- Multi-step payment workflow

---

### 5. Dashboard System (LOW-MEDIUM PRIORITY)

**Location**: `/src/WitchCityRope.Api/Features/Dashboard/`
**Business Purpose**: Member dashboard with personalized information
**Current Status**: NOT IMPLEMENTED in modern API

#### Key Components:
- **User Dashboard**: Personal member information display
- **Upcoming Events**: User's registered events
- **Membership Statistics**: Engagement and participation metrics

#### Data Models:
```csharp
public class DashboardDto
{
    public string SceneName { get; set; }
    public UserRole Role { get; set; }
    public VettingStatus VettingStatus { get; set; }
}

public class MembershipStatsDto
{
    public bool IsVerified { get; set; }
    public int EventsAttended { get; set; }
    public int MonthsAsMember { get; set; }
    public int ConsecutiveEvents { get; set; }
    public VettingStatus VettingStatus { get; set; }
}
```

#### Key Business Logic:
- ✅ Role-based access control (users can only see own dashboard)
- ✅ Upcoming events with registration status
- ✅ Membership statistics calculation
- ✅ Vetting status display
- ✅ Event attendance history

#### Features:
- **Personal Information**: Scene name, role, vetting status
- **Event Management**: Upcoming events with registration details
- **Statistics**: Events attended, membership duration, engagement metrics
- **Privacy Protection**: Users only access their own data

#### Comparison with Modern API:
**Modern API Status**: No dashboard functionality - frontend likely builds this from individual API calls

#### Dependencies:
- User management (profile information)
- Event system (upcoming events, registration status)
- Registration tracking (attendance history)

#### Integration Points:
- User profile system
- Event management system
- Registration/RSVP system
- Statistical analysis

#### Complexity: **LOW-MEDIUM**
- Mostly data aggregation and display
- Basic authorization (user can see own data)
- Simple statistics calculations

---

### 6. Events System Comparison

**Legacy API Events**: `/src/WitchCityRope.Api/Features/Events/`
**Modern API Events**: `/apps/api/Features/Events/`

#### Legacy Features (Missing from Modern):
- **Event Sessions**: Complex multi-session event support
- **Ticket Types**: Multiple ticket types per event
- **Event Availability**: Real-time capacity tracking
- **RSVP System**: Free RSVP vs. paid ticket distinction
- **Event Management**: Advanced event lifecycle management

#### Modern API Features (Exists):
- ✅ Basic CRUD operations (Create, Read, Update, Delete)
- ✅ Event listing and filtering
- ✅ Authentication integration
- ✅ Simple capacity tracking

#### Key Gaps:
1. **Session Management**: Modern API lacks multi-session events
2. **Ticket Type Variety**: Only basic capacity, no ticket type variations
3. **Advanced Scheduling**: Limited time-based event features
4. **Registration Integration**: Basic implementation vs. comprehensive workflow

#### Complexity: **MEDIUM**
- Modern API has foundation, needs enhancement
- Legacy has comprehensive features requiring careful migration

---

### 7. Authentication Comparison

**Legacy API Auth**: `/src/WitchCityRope.Api/Features/Auth/`
**Modern API Auth**: `/apps/api/Features/Authentication/`

#### Legacy Features:
- Two-factor authentication support
- Email verification workflow  
- Password reset functionality
- Refresh token management
- Comprehensive user profile management

#### Modern API Features:
- ✅ Basic login/logout with JWT
- ✅ Cookie-based session management
- ✅ Role-based authorization
- ✅ User profile endpoints

#### Key Gaps:
1. **Two-Factor Authentication**: Modern API lacks 2FA
2. **Email Verification**: No email verification workflow
3. **Password Reset**: Missing password recovery
4. **Advanced Profile**: Limited user profile management

#### Status: **MEDIUM PRIORITY**
- Core authentication works
- Missing advanced security features
- Legacy has production-ready features

---

## Feature Priority Matrix

### EXTRACT (Business-Critical, High Value)

| Feature | Priority | Reason | Complexity | Timeline |
|---------|----------|---------|------------|----------|
| **Safety System** | CRITICAL | Legal compliance, community safety | HIGH | 2-3 weeks |
| **CheckIn System** | HIGH | Core event functionality | HIGH | 2-3 weeks |
| **Vetting System** | HIGH | Community management | HIGH | 2-3 weeks |
| **Payments** | MEDIUM | Revenue generation, enhanced features | MEDIUM | 1-2 weeks |

### ENHANCE (Improve Existing)

| Feature | Current Status | Enhancement Needed | Priority | Timeline |
|---------|---------------|-------------------|----------|----------|
| **Events** | Basic CRUD | Add sessions, ticket types, RSVP | MEDIUM | 1-2 weeks |
| **Authentication** | Core working | Add 2FA, email verification | MEDIUM | 1-2 weeks |
| **Dashboard** | Missing | User dashboard and statistics | LOW | 1 week |

### ARCHIVE (Historical Value, Not Currently Needed)

| Feature | Reason | Action |
|---------|---------|--------|
| **Advanced Auth Adapters** | Modern API uses simpler patterns | Document patterns, archive |
| **Complex Event DTOs** | Modern API uses simplified models | Extract valuable patterns only |
| **Legacy Controllers** | Replaced by minimal API endpoints | Archive with documentation |

## Extraction Recommendations

### Phase 1: Critical Safety Infrastructure (Week 1)
1. **Safety System**: Implement incident reporting (legal compliance)
2. **Basic Infrastructure**: Notification services, encryption services

### Phase 2: Core Event Features (Week 2)
1. **CheckIn System**: Event check-in functionality
2. **Enhanced Events**: Sessions and ticket types

### Phase 3: Community Management (Week 3)
1. **Vetting System**: Member vetting workflow
2. **Dashboard**: User dashboard and statistics

### Phase 4: Financial Features (Week 4)
1. **Enhanced Payments**: Stripe integration, payment methods
2. **Integration**: Connect payments with events and registrations

## Architecture Migration Strategy

### Vertical Slice Implementation
Each legacy feature should be migrated using the modern API's vertical slice pattern:

```
/apps/api/Features/[FeatureName]/
├── Services/[FeatureName]Service.cs      # Business logic
├── Endpoints/[FeatureName]Endpoints.cs   # API endpoints  
├── Models/[Request/Response]Dto.cs       # Data transfer objects
└── Validation/[Request]Validator.cs      # Input validation
```

### Key Migration Principles:
1. **Simplify Architecture**: Remove MediatR/CQRS complexity from legacy
2. **Direct Entity Framework**: Use DbContext directly in services
3. **Minimal API Patterns**: Follow modern API endpoint patterns
4. **Maintain Business Logic**: Preserve critical business rules
5. **Enhance Security**: Improve authentication and encryption patterns

## Success Criteria

### Technical Success Metrics:
- [ ] Zero duplicate API references in codebase
- [ ] All critical features migrated with business logic intact
- [ ] Performance maintained (<50ms response times)
- [ ] No frontend functionality regression
- [ ] Comprehensive test coverage for migrated features

### Business Success Metrics:
- [ ] Safety incident reporting functional
- [ ] Event check-in system operational
- [ ] Member vetting process working
- [ ] Payment processing enhanced
- [ ] Community platform fully functional

## Risk Assessment

### High Risk Areas:
1. **Safety System**: Legal compliance requirements, data encryption
2. **Payment Processing**: Financial data security, PCI compliance  
3. **Data Migration**: Preserving existing data during feature extraction

### Mitigation Strategies:
1. **Comprehensive Testing**: TestContainers for all database operations
2. **Incremental Migration**: One feature at a time with rollback capability
3. **Data Backup**: Complete backups before any schema changes
4. **Parallel Running**: Keep legacy accessible during migration period

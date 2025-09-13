# Safety System Functional Specification - WitchCityRope
<!-- Last Updated: 2025-09-12 -->
<!-- Version: 1.0 -->
<!-- Owner: Business Requirements Agent -->
<!-- Status: Draft - Ready for Implementation -->

## Executive Summary

This document defines the comprehensive functional specification for the Safety incident reporting system, identified as CRITICAL priority due to legal compliance requirements. The system enables anonymous incident reporting, administrative management, and user safety portals while maintaining the highest standards for sensitive data privacy and security.

**Key Simplifications Based on Stakeholder Feedback:**
- NO incident type categorization (only severity levels)
- Email notifications only (no SMS)
- Database records kept indefinitely for legal compliance
- Simplified incident reporting form

## Business Context

### Problem Statement
WitchCityRope currently lacks any safety incident reporting system, creating legal compliance risks and compromising community safety management. The legacy API contains a comprehensive safety system that must be extracted and modernized.

### Business Value
- **Legal Compliance**: Meets legal requirements for incident documentation
- **Community Safety**: Provides structured incident reporting and management
- **Privacy Protection**: Anonymous reporting protects community members
- **Audit Compliance**: Complete audit trails for legal review
- **Operational Efficiency**: Streamlined safety team workflows

### Success Metrics
- 100% incident reports captured with audit trails
- <30 second incident submission time
- 24/7 anonymous reporting availability
- Zero data breaches or privacy violations
- Safety team response time <2 hours for critical incidents

## Architecture Integration

### Vertical Slice Implementation Pattern
Following the established modern API architecture:

```
/apps/api/Features/Safety/
├── Services/SafetyService.cs           # Business logic
├── Endpoints/SafetyEndpoints.cs        # API endpoints  
├── Models/SafetyDtos.cs               # Data transfer objects
├── Validation/SafetyRequestValidator.cs # Input validation
└── Infrastructure/
    ├── EncryptionService.cs           # Data encryption
    ├── NotificationService.cs         # Email alerts
    └── AuditService.cs                # Audit logging
```

### DTO Alignment Strategy
**CRITICAL**: All DTOs follow NSwag auto-generation patterns per `/docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md`

## Data Structure Requirements

### Core Incident Report DTO
```csharp
public class IncidentReportDto
{
    public Guid Id { get; set; }
    public string ReferenceNumber { get; set; }  // Format: SAF-YYYYMMDD-1234
    public Guid? ReporterId { get; set; }        // Optional for anonymous reports
    public IncidentSeverity Severity { get; set; }
    public DateTime IncidentDate { get; set; }
    public DateTime ReportedAt { get; set; }
    public string Location { get; set; }
    public string Description { get; set; }      // Encrypted in database
    public List<string> InvolvedParties { get; set; } // Encrypted
    public List<string> Witnesses { get; set; }       // Encrypted
    public bool IsAnonymous { get; set; }
    public bool RequestFollowUp { get; set; }
    public IncidentStatus Status { get; set; }
    public Guid? AssignedTo { get; set; }
    public List<IncidentNoteDto> Notes { get; set; }
    public DateTime LastUpdated { get; set; }
}

public enum IncidentSeverity
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public enum IncidentStatus
{
    New = 1,
    InProgress = 2,
    Resolved = 3,
    Archived = 4
}
```

### Incident Submission Request
```csharp
public class SubmitIncidentReportRequest
{
    public Guid? ReporterId { get; set; }         // Optional for anonymous
    public IncidentSeverity Severity { get; set; }
    public DateTime IncidentDate { get; set; }
    public string Location { get; set; }
    public string Description { get; set; }
    public string? InvolvedParties { get; set; }  // Optional text area
    public string? Witnesses { get; set; }        // Optional text area
    public bool IsAnonymous { get; set; }
    public bool RequestFollowUp { get; set; }
    public string? ContactEmail { get; set; }     // Required if not anonymous
    public string? ContactPhone { get; set; }     // Optional
}
```

### Safety Team Response Models
```csharp
public class IncidentDetailResponse
{
    public IncidentReportDto Incident { get; set; }
    public List<AuditLogEntryDto> AuditTrail { get; set; }
    public bool CanUserEdit { get; set; }
    public bool CanUserAssign { get; set; }
}

public class SafetyDashboardResponse
{
    public SafetyStatisticsDto Statistics { get; set; }
    public List<IncidentSummaryDto> RecentIncidents { get; set; }
    public List<ActionItemDto> PendingActions { get; set; }
}
```

## User Workflows

### 1. Anonymous Incident Reporting Flow

**Trigger**: User visits safety page and clicks "Report Incident"

**Steps**:
1. Form loads with "Anonymous Report" pre-selected
2. User selects severity level (Low/Medium/High/Critical)
3. User enters incident date and time
4. User provides location information
5. User describes incident in detail
6. User optionally lists involved parties and witnesses
7. System validates all required fields
8. System generates tracking reference number (SAF-YYYYMMDD-XXXX)
9. System encrypts sensitive information
10. System stores report in database
11. System displays confirmation with tracking number
12. System sends email notification to safety team

**Business Rules**:
- No authentication required for anonymous reports
- Reference number must be unique and sequential
- All sensitive fields encrypted before database storage
- No IP address logging for anonymous reports
- Tracking page accessible via reference number only

**Success Criteria**:
- Report successfully submitted within 30 seconds
- Confirmation displayed with tracking number
- Safety team receives immediate email notification
- No personal identifying information stored

### 2. Identified Reporter Flow

**Trigger**: User selects "Include My Contact" option in reporting form

**Steps**:
1. Contact information fields become visible
2. User enters email address (required)
3. User optionally enters phone number
4. Form submission includes user identification
5. System links report to user account (if logged in)
6. Report appears in user's safety dashboard
7. System sends confirmation email to reporter
8. System sends notification to safety team

**Business Rules**:
- Email address required for identified reports
- Phone number optional but recommended
- Reports linked to user account if authenticated
- User receives email confirmation within 5 minutes
- User can track report status via dashboard

### 3. Safety Team Investigation Flow

**Trigger**: Safety team member receives incident notification email

**Steps**:
1. Team member clicks link to incident detail
2. System authenticates safety team role
3. System decrypts sensitive information for display
4. Team member reviews incident details
5. Team member assigns incident to specific investigator
6. Status updated to "In Progress"
7. Investigation notes added to incident record
8. Reporter contacted if not anonymous
9. Follow-up actions documented
10. Status updated to "Resolved" when complete

**Business Rules**:
- Only safety team roles can view full incident details
- All access attempts logged in audit trail
- Critical/High incidents auto-assigned to on-call team
- Status changes trigger notification emails
- Resolution requires documented action taken

## API Endpoint Specifications

### POST /api/safety/incidents
**Purpose**: Submit new incident report

**Authentication**: Optional (anonymous reporting supported)

**Request Body**: `SubmitIncidentReportRequest`

**Response**: 
```json
{
  "success": true,
  "data": {
    "referenceNumber": "SAF-20250912-1234",
    "trackingUrl": "/safety/track/SAF-20250912-1234"
  }
}
```

**Business Logic**:
1. Validate request data using FluentValidation
2. Generate unique reference number
3. Encrypt sensitive fields (description, involved parties, witnesses)
4. Store in database with audit log entry
5. Send notification email to safety team based on severity
6. Return tracking information

**Error Handling**:
- 400: Validation errors (field requirements, data format)
- 500: System errors (encryption failure, database unavailable)

### GET /api/safety/incidents/{referenceNumber}/status
**Purpose**: Get incident status for tracking (anonymous access)

**Authentication**: None required

**Response**:
```json
{
  "referenceNumber": "SAF-20250912-1234",
  "status": "InProgress",
  "lastUpdated": "2025-09-12T15:30:00Z",
  "canProvideMoreInfo": true
}
```

### GET /api/safety/admin/dashboard
**Purpose**: Safety team dashboard data

**Authentication**: Required (SafetyTeam role)

**Response**: `SafetyDashboardResponse`

**Business Logic**:
1. Aggregate statistics by severity and status
2. Return recent incidents (last 30 days)
3. List pending action items
4. Filter by user permissions

### GET /api/safety/admin/incidents/{id}
**Purpose**: Detailed incident view for safety team

**Authentication**: Required (SafetyTeam role)

**Response**: `IncidentDetailResponse`

**Business Logic**:
1. Verify user has safety team permissions
2. Decrypt sensitive information for display
3. Log access attempt in audit trail
4. Return complete incident details with audit history

### PATCH /api/safety/admin/incidents/{id}/status
**Purpose**: Update incident status and assignment

**Authentication**: Required (SafetyTeam role)

**Request Body**:
```json
{
  "status": "InProgress",
  "assignedTo": "guid-of-team-member",
  "notes": "Investigation started, reviewing evidence"
}
```

**Business Logic**:
1. Validate status transition is valid
2. Update incident record
3. Add note to incident history
4. Log status change in audit trail
5. Send notification emails if needed

## Security and Privacy Requirements

### Data Encryption
- **Fields to Encrypt**: description, involvedParties, witnesses, contactEmail, contactPhone
- **Encryption Method**: AES-256 with unique per-record keys
- **Key Management**: Azure Key Vault or equivalent secure storage
- **Decryption Access**: Only authorized safety team members

### Anonymous Protection
- **No IP Logging**: Anonymous reports don't store IP addresses
- **Session Isolation**: No session cookies for anonymous submissions
- **Reference-Only Tracking**: Tracking via reference number only
- **No User Linking**: Anonymous reports never linked to user accounts

### Audit Trail Requirements
- **All Access Logged**: Every view/edit attempt recorded with timestamp and user
- **Status Change Tracking**: All status updates logged with reason
- **Note History**: Complete history of all safety team notes and actions
- **Data Retention**: Audit logs kept indefinitely for legal compliance

### Role-Based Access Control
- **Anonymous Users**: Can submit reports and track via reference number
- **General Users**: Can submit identified reports, view own reports
- **Safety Team**: Can view all reports, update status, add notes
- **Safety Admin**: Full access plus user management and system configuration

## Notification Rules

### Email Notification Matrix

| Severity | Safety Team | On-Call Team | Admin |
|----------|-------------|--------------|-------|
| Critical | Immediate   | Immediate    | Immediate |
| High     | Immediate   | Immediate    | Next Day Summary |
| Medium   | Within 1 Hour | Daily Summary | Weekly Summary |
| Low      | Daily Summary | Weekly Summary | Weekly Summary |

### Notification Content
**Critical/High Severity**:
```
Subject: URGENT: Critical Safety Incident - SAF-20250912-1234

A critical safety incident has been reported and requires immediate attention.

Reference: SAF-20250912-1234
Severity: Critical
Location: [Location]
Reported: 2025-09-12 14:30

Access full details: [Secure Link]

This is an automated alert. Do not reply to this email.
```

**Medium/Low Severity**:
```
Subject: Safety Incident Report - SAF-20250912-1234

A new safety incident has been reported for your review.

Reference: SAF-20250912-1234
Severity: Medium
Location: [Location]
Reported: 2025-09-12 14:30

Review when convenient: [Secure Link]
```

## Business Rules

### Incident Lifecycle
1. **New** → **InProgress** → **Resolved** → **Archived**
2. Status can only move forward (no regression)
3. Critical incidents cannot be marked resolved without admin approval
4. All status changes require notes explaining action taken

### Escalation Rules
- **Critical Severity**: Auto-assign to on-call safety team member
- **High Severity + Consent Issues**: Immediate safety admin notification
- **No Response After 24 Hours**: Escalate to safety admin
- **Reporter Follow-up Requested**: Contact within 48 hours

### Data Validation Rules
- **Location**: Required, 5-200 characters
- **Description**: Required, 50-5000 characters
- **Incident Date**: Cannot be more than 30 days in the future
- **Contact Email**: Valid email format when provided
- **Severity Level**: Must be valid enum value

## Integration Points

### User Management System
- Link identified reports to user accounts
- Validate safety team role permissions
- Track user's incident history

### Email Service
- Send notifications based on severity rules
- Deliver confirmation emails to reporters
- Handle bounce-back and delivery failures

### Audit Logging System
- Log all system access attempts
- Track data changes with timestamps
- Maintain immutable audit records

## Performance Requirements

### Response Time Targets
- Incident submission: <2 seconds
- Dashboard load: <1 second
- Search/filter operations: <3 seconds
- Status updates: <1 second

### Scalability Requirements
- Support 100 concurrent incident submissions
- Handle 1000+ incidents in database
- Search and filter large incident datasets
- Export capabilities for legal review

## Testing Requirements

### Unit Tests
- Encryption/decryption services
- Reference number generation
- Notification rule logic
- Data validation rules

### Integration Tests
- End-to-end incident submission flow
- Email notification delivery
- Database encryption/decryption
- Role-based access control

### Security Tests
- Anonymous access protection
- Encryption key handling
- SQL injection prevention
- XSS protection in forms

## Compliance Requirements

### Legal Compliance
- **Data Retention**: Incident records kept indefinitely
- **Privacy Protection**: Anonymous reporting fully supported
- **Audit Trails**: Complete action history for legal review
- **Access Control**: Role-based permissions enforced

### Community Standards
- **Consent-First Approach**: Clear privacy notices throughout
- **Transparency**: Status tracking available to reporters
- **Confidentiality**: Sensitive information protected
- **Non-Retaliation**: Anonymous reporting prevents identification

## Quality Gate Checklist

### Functional Requirements
- [ ] Anonymous incident reporting fully functional
- [ ] Reference number tracking system working
- [ ] Safety team dashboard operational
- [ ] Email notifications sent per severity rules
- [ ] Status workflow enforced correctly
- [ ] Data encryption/decryption working
- [ ] Audit trail logging all actions
- [ ] Role-based access control implemented

### Security Requirements
- [ ] Sensitive data encrypted at rest
- [ ] Anonymous protection verified
- [ ] No IP address logging for anonymous reports
- [ ] Secure access to encrypted data
- [ ] SQL injection protection tested
- [ ] XSS protection implemented
- [ ] Input validation comprehensive

### Performance Requirements
- [ ] Incident submission <2 seconds
- [ ] Dashboard loads <1 second
- [ ] Email notifications <30 seconds
- [ ] Database queries optimized
- [ ] Concurrent user testing passed

### Compliance Requirements
- [ ] Legal data retention policies implemented
- [ ] Privacy protection mechanisms tested
- [ ] Audit trail completeness verified
- [ ] Access control authorization tested
- [ ] Community safety standards met

## Implementation Notes

### Development Priority
1. **Week 1**: Core incident submission and storage
2. **Week 2**: Safety team dashboard and management
3. **Week 3**: Notification system and email integration
4. **Week 4**: Security hardening and audit compliance

### Critical Dependencies
- Encryption service implementation
- Email service configuration
- Database migration for new tables
- Role permissions setup

### Risk Mitigation
- **Data Breach Risk**: Multi-layer encryption and access controls
- **System Downtime**: Database backup and recovery procedures
- **Legal Compliance**: Regular audit and compliance reviews
- **Performance Issues**: Database indexing and query optimization

This functional specification provides the complete technical and business requirements for implementing the Safety incident reporting system, ensuring legal compliance while maintaining the highest standards for community safety and privacy protection.
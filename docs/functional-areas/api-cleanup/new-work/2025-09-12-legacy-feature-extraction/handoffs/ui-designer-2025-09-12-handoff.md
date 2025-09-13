# AGENT HANDOFF DOCUMENT

## Phase: UI Design Complete
## Date: 2025-09-12
## Feature: Safety Incident Reporting System

## 🎯 CRITICAL BUSINESS RULES (MUST IMPLEMENT)

1. **Anonymous Reporting Must Be Fully Protected**: Anonymous reports MUST NOT log user IP, session data, or any identifying information
   - ✅ Correct: Radio button selection clears all contact fields, no session tracking
   - ❌ Wrong: Storing any user session data when anonymous is selected

2. **Severity-Based Alert Escalation**: Critical and High consent violations MUST trigger immediate safety team alerts
   - ✅ Correct: Real-time notifications to on-call safety team for severity ≥ HIGH
   - ❌ Wrong: All incidents getting same notification priority

3. **Sensitive Data Encryption**: All description text, involved parties, and witness information MUST be encrypted at rest
   - ✅ Correct: Encrypt before database storage, decrypt only for authorized safety team
   - ❌ Wrong: Plain text storage in database

4. **Audit Trail Requirements**: Every view, edit, and status change MUST be logged with timestamp and user
   - ✅ Correct: Complete action history with user ID, timestamp, and action type
   - ❌ Wrong: Basic logging without user attribution or action details

5. **Role-Based Access Control**: Only designated safety team members can view incident details
   - ✅ Correct: Safety team role verification before displaying encrypted content
   - ❌ Wrong: General admin access to sensitive incident data

## 📍 KEY DOCUMENTS TO READ

| Document | Path | Critical Sections |
|----------|------|-------------------|
| Legacy API Analysis | `/docs/functional-areas/api-cleanup/new-work/2025-09-12-legacy-feature-extraction/requirements/legacy-api-feature-analysis.md` | Lines 108-176: Safety System analysis |
| UI Design Document | `/docs/functional-areas/api-cleanup/new-work/2025-09-12-legacy-feature-extraction/design/safety-system-ui-design.md` | Component specifications and data models |
| UI Designer Lessons | `/docs/lessons-learned/ui-designer-lessons-learned.md` | Design System v7 requirements, Mantine patterns |

## 🚨 KNOWN PITFALLS

1. **Anonymous vs Identified Data Handling**: Mixing anonymous and identified report storage patterns
   - **Why it happens**: Legacy systems often assume user authentication for all forms
   - **How to avoid**: Create separate data paths for anonymous vs identified submissions

2. **Severity Color Coding Confusion**: Using generic status colors instead of safety-specific severity colors
   - **Why it happens**: Reusing standard success/warning/error colors
   - **How to avoid**: Use exact severity colors from design document (#228B22, #DAA520, #DC143C, #8B0000)

3. **Encryption Implementation Gaps**: Encrypting only some sensitive fields while leaving others plain text
   - **Why it happens**: Unclear understanding of what constitutes sensitive data
   - **How to avoid**: Encrypt ALL user-provided content (description, parties, witnesses, location details)

4. **Mobile Form Usability Issues**: Complex incident reporting form becoming unusable on mobile
   - **Why it happens**: Desktop-first design approach
   - **How to avoid**: Follow mobile wireframes exactly, test touch targets ≥44px

## ✅ VALIDATION CHECKLIST

Before proceeding to implementation phase, verify:

- [ ] Anonymous reporting path completely separate from identified reporting
- [ ] Severity levels mapped to exact colors and alert escalation rules
- [ ] All sensitive data fields marked for encryption in data model
- [ ] Role-based access control integrated with existing auth system
- [ ] Mobile responsive behavior tested for all form components
- [ ] Mantine v7 components selected match design specifications
- [ ] Floating label animations implemented for ALL text inputs
- [ ] Privacy notices prominent and legally compliant

## 🔄 DISCOVERED CONSTRAINTS

1. **Existing Auth System**: Modern API uses cookie-based authentication with role management
   - **Impact**: Must integrate safety team roles with existing user role system
   - **Required Changes**: Extend user roles to include "SafetyTeam" and "SafetyAdmin"

2. **Mantine v7 Framework**: UI must use Mantine components exclusively per ADR-004
   - **Impact**: All designs specified use Mantine component library
   - **Required Changes**: No custom UI components outside Mantine ecosystem

3. **Design System v7**: Strict color palette and animation requirements
   - **Impact**: Cannot deviate from specified colors (#880124, #B76D75, #FFBF00, etc.)
   - **Required Changes**: Use exact hex values, implement signature animations

## 📊 DATA MODEL DECISIONS

```
Entity: IncidentReport
- Id: Guid (Required) - Primary key, used in tracking numbers
- ReporterId: Guid? (Optional) - Null for anonymous reports
- Type: IncidentType enum (Required) - safety_violation, consent_violation, etc.
- Severity: IncidentSeverity enum (Required) - low, medium, high, critical
- IncidentDate: DateTime (Required) - When incident occurred
- Location: string (Required, Encrypted) - Where incident occurred
- Description: string (Required, Encrypted) - Detailed incident description
- InvolvedParties: string? (Optional, Encrypted) - Names/descriptions
- Witnesses: string? (Optional, Encrypted) - Witness information
- ReporterEmail: string? (Optional) - Contact if not anonymous
- ReporterPhone: string? (Optional) - Contact if not anonymous
- IsAnonymous: bool (Required) - Privacy flag
- Status: IncidentStatus enum (Required) - New, InProgress, Resolved
- AssignedTo: Guid? (Optional) - Safety team member ID
- CreatedAt: DateTime (Required) - Auto-generated
- UpdatedAt: DateTime (Required) - Auto-updated

Entity: IncidentAuditLog
- Id: Guid (Required) - Primary key
- IncidentId: Guid (Required) - Foreign key to IncidentReport
- UserId: Guid (Required) - Who performed the action
- Action: string (Required) - What action was performed
- Details: string? (Optional) - Additional action context
- Timestamp: DateTime (Required) - When action occurred

Business Logic:
- Reference numbers generated as: {TYPE}-{YYYYMMDD}-{Random4Digits}
- Critical/High severity incidents trigger immediate notifications
- Anonymous reports have NO user session tracking
- All encrypted fields use AES-256 encryption
```

## 🎯 SUCCESS CRITERIA

1. **Anonymous Submission Test**:
   - **Input**: Submit incident with "Anonymous Report" selected
   - **Expected Output**: No user identification stored, tracking number generated, safety team notified

2. **Critical Incident Alert Test**:
   - **Input**: Submit incident with severity="critical", type="consent_violation"
   - **Expected Output**: Immediate email/SMS to on-call safety team, admin dashboard shows alert

3. **Audit Trail Test**:
   - **Input**: Safety team member views, edits, and updates incident status
   - **Expected Output**: Complete audit log with user, timestamp, and action details

4. **Encryption Verification Test**:
   - **Input**: Submit incident with sensitive description and involved parties
   - **Expected Output**: Database shows encrypted values, UI displays decrypted for safety team only

## ⚠️ DO NOT IMPLEMENT

- ❌ DO NOT store any session data for anonymous reports
- ❌ DO NOT use generic notification patterns - severity requires specific escalation
- ❌ DO NOT implement plain text storage for ANY user-provided content
- ❌ DO NOT allow general admin access to incident details without safety team role
- ❌ DO NOT create custom UI components - use Mantine v7 exclusively
- ❌ DO NOT deviate from Design System v7 colors and animations
- ❌ DO NOT implement without mobile-first responsive design
- ❌ DO NOT skip floating label animations on form inputs

## 📝 TERMINOLOGY DICTIONARY

| Term | Definition | Example |
|------|------------|---------|
| Anonymous Report | Incident submission with no user identification | User selects radio button, no contact fields, no session tracking |
| Identified Report | Incident submission with reporter contact info | User provides email, links to user account, appears in user dashboard |
| Safety Team | Designated community members with incident management access | Users with "SafetyTeam" role can view, assign, update incidents |
| Incident Tracking Number | Unique reference for each report | "CON-20250912-1234" (Type-Date-Random) |
| Encryption at Rest | Sensitive data encrypted in database storage | Description, parties, witnesses encrypted with AES-256 |
| Severity Escalation | Alert priority based on incident severity level | Critical/High → immediate alerts, Medium/Low → standard notifications |

## 🔗 NEXT AGENT INSTRUCTIONS

1. **FIRST**: Read the legacy API analysis document (lines 108-176) to understand business requirements
2. **SECOND**: Review the complete UI design document for component specifications and data models
3. **THIRD**: Examine existing authentication and user role systems in modern API
4. **FOURTH**: Validate understanding of encryption requirements and audit logging needs
5. **THEN**: Begin implementation following vertical slice architecture pattern

## 🤝 HANDOFF CONFIRMATION

**Previous Agent**: UI Designer Agent
**Previous Phase Completed**: 2025-09-12
**Key Finding**: Safety system requires complex privacy protection with anonymous reporting capabilities and legal compliance through encryption and audit trails

**Next Agent Should Be**: React Developer Agent (for frontend components) + Backend Developer Agent (for API endpoints)
**Next Phase**: Implementation (Frontend + Backend parallel development)
**Estimated Effort**: 2-3 weeks for full implementation with testing

---

## Implementation Priority Notes

### Week 1 Focus: Core Infrastructure
- Implement data models with encryption
- Create basic incident submission endpoints
- Build anonymous vs identified submission paths
- Set up severity-based notification system

### Week 2 Focus: Admin Interface
- Implement safety team dashboard
- Create incident detail views with audit trails
- Build status management and assignment features
- Implement role-based access control

### Week 3 Focus: User Interface & Polish
- Complete user safety portal
- Implement comprehensive testing
- Add mobile responsive refinements
- Conduct security audit and penetration testing

This handoff ensures the next phase agents understand the critical legal compliance requirements and privacy protection needs while following established architectural patterns and design system requirements.
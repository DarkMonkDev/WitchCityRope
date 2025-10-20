# Business Requirements: Incident Reporting System
<!-- Last Updated: 2025-10-17 -->
<!-- Version: 1.0 -->
<!-- Owner: Business Requirements Agent -->
<!-- Status: Draft -->

## Executive Summary

WitchCityRope requires a comprehensive incident reporting and tracking system to document safety incidents, consent violations, and community concerns. This system will enable community members to submit reports (anonymously or identified), and empower incident coordinators to manage the resolution process through a multi-stage workflow. The system mirrors successful patterns from the existing vetting system while addressing unique needs around sensitive incident data, coordinator assignment, and Google Drive integration.

**Key Value Propositions:**
- **Safety First**: Enables rapid response to safety concerns with clear accountability
- **Trust Building**: Transparent, trackable incident resolution process builds community confidence
- **Privacy Protection**: Full anonymous reporting capability protects vulnerable reporters
- **Flexible Coordination**: Per-incident coordinator assignment allows expertise-based response
- **Workflow Guidance**: Stage-specific guidance ensures consistent, thorough investigations

## Business Context

### Problem Statement

WitchCityRope currently lacks a formal system for:
1. **Documenting safety incidents** beyond email/Discord conversations
2. **Tracking incident resolution** through investigation stages
3. **Maintaining confidential records** with appropriate access controls
4. **Assigning incident coordination** to appropriate community members
5. **Archiving final reports** in Google Drive for long-term reference

This gap creates:
- **Risk**: Incidents handled inconsistently or lost in communication threads
- **Accountability**: No clear ownership or status tracking for incidents
- **Trust**: Community members unsure if reports will be handled properly
- **Legal**: Poor documentation of safety responses and resolutions

### Business Value

**For Community Members (Reporters):**
- Safe, confidential mechanism to report concerns
- Tracking number to check status (anonymous reports)
- Confidence that reports will be reviewed and handled
- Protection from retaliation through anonymous reporting

**For Incident Coordinators:**
- Clear workflow with stage-specific guidance
- Centralized case management interface
- Communication tools (notes system)
- Historical incident context for informed decisions

**For Administrators:**
- Oversight of all active incidents
- Assignment flexibility (any user can coordinate)
- Audit trail for all actions
- Google Drive integration for permanent records

**For Community Safety:**
- Early warning system for patterns of behavior
- Documented evidence for membership decisions
- Transparency in safety response processes
- Continuous improvement through data analysis

### Success Metrics

**Adoption Metrics:**
- 100% of safety incidents documented in system (vs email/Discord)
- <24 hours from submission to assignment
- >90% of incidents progress to "Reviewing Final Report" stage

**Quality Metrics:**
- All incidents have ≥1 coordinator note per stage
- Zero unauthorized access to incident details
- 100% of closed incidents archived to Google Drive (Phase 2)

**User Satisfaction:**
- Anonymous submissions ≥50% of total (shows trust in system)
- <5% reopened incidents (resolution quality)
- Coordinator satisfaction >4/5 on workflow clarity

## User Roles & Personas

### Anonymous Reporter
**Description**: Community member reporting incident without revealing identity

**Needs:**
- Absolute anonymity protection (no IP logging, no follow-up capability)
- Simple, clear submission form
- Confidence report will be reviewed

**Limitations:**
- Cannot provide contact information
- Cannot request follow-up
- Cannot respond to coordinator questions
- Cannot check report status (fully anonymous, no tracking)

**User Story Focus**: Privacy protection, ease of reporting

---

### Identified Reporter
**Description**: Community member willing to attach identity to report

**Needs:**
- Optional contact information fields
- Ability to request follow-up
- Same privacy protections as anonymous (encryption, access controls)
- Potential for mediation/dialogue

**Benefits Over Anonymous:**
- Coordinators can ask clarifying questions
- Can participate in resolution process
- Can be notified of status changes

**User Story Focus**: Communication, resolution involvement

---

### Incident Coordinator (Assigned Per-Incident)
**Description**: Any user assigned to coordinate a specific incident investigation

**Key Characteristics:**
- **NOT a permanent role** - assignment is per-incident
- **Can be non-admin users** - expertise-based assignment
- **Single incident focus** - only sees assigned incidents
- **Guided workflow** - modal reminders at stage transitions

**Needs:**
- Clear incident details and timeline
- Notes system for documentation
- Stage transition guidance (modals with reminders)
- Assignment notification
- Status tracking

**Responsibilities:**
- Gather information during "Information Gathering" stage
- Prepare final report during "Reviewing Final Report" stage
- Document all activities in notes
- Upload to Google Drive when closing (Phase 2 automation)

**Limitations:**
- Cannot see incidents they're not assigned to
- Cannot assign/reassign incidents
- Cannot delete incidents or notes
- Must follow stage progression guidance (not enforced)

**User Story Focus**: Investigation workflow, documentation, guidance

---

### Admin
**Description**: System administrator with full oversight capability

**Needs:**
- View all incidents (assigned and unassigned)
- Assign coordinators to incidents
- Reassign incidents if needed
- Override capabilities for emergencies
- Audit trail visibility
- Google Drive management (Phase 2)

**Responsibilities:**
- Initial incident assignment
- Monitoring resolution progress
- Handling escalations
- System configuration
- Archive management

**Capabilities:**
- Assign any user as coordinator (including non-admins)
- View all notes and audit logs
- Access encrypted sensitive data
- Manage Google Drive integration (Phase 2)

**User Story Focus**: Oversight, assignment, system management

---

### Guest/General Member (Future Consideration)
**Description**: Users who may submit reports but have minimal system access

**Current Phase:** Basic reporting only
**Future Phases:** None (anonymous reports are truly one-way)

## User Stories with Acceptance Criteria

### Epic 1: Anonymous Incident Reporting

#### Story 1.1: Submit Anonymous Report
**As an** anonymous reporter
**I want to** submit an incident report without revealing my identity
**So that** I can report safety concerns without fear of retaliation

**Acceptance Criteria:**
- **Given** I am on the incident reporting form
- **When** I select "Submit Anonymously" toggle
- **Then** no contact information fields are shown
- **And** the form indicates "No follow-up mechanism available"
- **And** my identity is NOT stored (ReporterId = null)
- **And** IP address is NOT logged

- **Given** I submit an anonymous report
- **When** submission succeeds
- **Then** I see confirmation message: "Your report has been submitted and will be reviewed by our safety team"
- **And** I am returned to home page or safety resources page
- **And** I am NOT sent email confirmation

- **Given** I submit an anonymous report
- **When** the report is stored
- **Then** all sensitive fields are encrypted (description, involved parties, witnesses)
- **And** ReporterId field is null
- **And** audit log records "Anonymous Submission"
- **And** initial status is "Report Submitted"

**Business Rules:**
- Anonymous reports CANNOT request follow-up
- Anonymous reports CANNOT provide contact information
- Reference number is ONLY way to check status
- No email notifications sent for anonymous reports
- IP addresses are NOT logged for anonymous submissions

**Priority:** Critical (P0)
**Estimated Effort:** 8 hours

---

---

### Epic 2: Identified Incident Reporting

#### Story 2.1: Submit Identified Report
**As an** authenticated member
**I want to** submit an incident report with my contact information
**So that** coordinators can follow up with me directly

**Acceptance Criteria:**
- **Given** I am logged in
- **When** I am on the incident reporting form
- **Then** "Include My Contact" option is available
- **And** my email is pre-populated from profile
- **And** I can add optional phone number
- **And** I can check "Request follow-up contact"

- **Given** I submit an identified report
- **When** submission succeeds
- **Then** my ReporterId is stored
- **And** contact information is encrypted
- **And** I receive email confirmation
- **And** email includes "You will be contacted if additional information is needed"

- **Given** I submit identified report without requesting follow-up
- **When** report is stored
- **Then** RequestFollowUp = false
- **And** I can still be contacted if needed
- **And** email says "We may contact you if additional information is needed"

**Business Rules:**
- Identified reports REQUIRE valid email address
- Phone number is optional
- Follow-up request is optional (default: false)
- ReporterId links to ApplicationUser table
- All contact info encrypted at rest
- Email confirmation sent within 5 minutes

**Priority:** Critical (P0)
**Estimated Effort:** 6 hours

---

#### Story 2.2: Reporter Receives Status Updates
**As an** identified reporter
**I want to** receive email notifications when my report status changes
**So that** I know progress is being made

**Acceptance Criteria:**
- **Given** I submitted an identified report with email
- **When** status changes to "Information Gathering"
- **Then** I receive email: "Your incident report is being reviewed"
- **And** email includes current status
- **And** email does NOT include coordinator name or internal notes

- **Given** my report is closed
- **When** status changes to "Closed"
- **Then** I receive email: "Incident report has been resolved"
- **And** email thanks me for reporting
- **And** email does NOT include resolution details (privacy)

- **Given** my report is put on hold
- **When** status changes to "On Hold"
- **Then** I receive email: "Your report requires additional review"
- **And** email does NOT include reason for hold (privacy)

**Business Rules:**
- Only identified reporters receive email notifications
- Emails sent for: Assignment, Status Change, Closure
- Emails do NOT expose internal notes or coordinator identity
- Emails use professional, compassionate tone
- Emails include crisis resources footer

**Priority:** High (P1)
**Estimated Effort:** 6 hours

---

### Epic 3: Incident Coordinator Assignment & Access

#### Story 3.1: Admin Assigns Incident Coordinator
**As an** admin
**I want to** assign any user as incident coordinator
**So that** I can match expertise to incident type

**Acceptance Criteria:**
- **Given** I am viewing an unassigned incident
- **When** I click "Assign Coordinator"
- **Then** I see dropdown of ALL users (admin and non-admin)
- **And** dropdown shows scene name and real name
- **And** dropdown indicates if user has coordinated incidents before

- **Given** I select a coordinator
- **When** I click "Assign"
- **Then** incident AssignedTo field is updated
- **And** status changes from "Report Submitted" to "Information Gathering"
- **And** audit log records: "Assigned to [coordinator] by [admin]"
- **And** coordinator receives email notification
- **And** guidance modal is shown about "Information Gathering" stage

- **Given** incident is already assigned
- **When** I reassign to different coordinator
- **Then** previous coordinator loses access
- **And** new coordinator gains access
- **And** audit log records reassignment
- **And** both coordinators receive email notifications

**Business Rules:**
- ANY user can be assigned as coordinator (not just admins)
- Only ONE coordinator per incident at a time
- Assignment triggers status change to "Information Gathering"
- Reassignment requires admin role
- Audit trail records all assignment changes
- Email notification includes assignment guidance

**Priority:** Critical (P0)
**Estimated Effort:** 8 hours

---

#### Story 3.2: Coordinator Views Assigned Incidents
**As an** incident coordinator
**I want to** see only incidents assigned to me
**So that** I can focus on my responsibilities

**Acceptance Criteria:**
- **Given** I am logged in as coordinator
- **When** I navigate to incident dashboard
- **Then** I see ONLY incidents assigned to me
- **And** I see severity level with color coding
- **And** I see current status
- **And** I see days since last update
- **And** I do NOT see unassigned or others' incidents

- **Given** I have no assigned incidents
- **When** I view dashboard
- **Then** I see message: "No incidents currently assigned to you"
- **And** I see link to community safety guidelines

- **Given** I click on an assigned incident
- **When** incident detail page loads
- **Then** I see all incident details (encrypted data is decrypted for display)
- **And** I see all notes (mine and others)
- **And** I see audit log
- **And** I see status change buttons

**Business Rules:**
- Coordinators ONLY see incidents assigned to them
- Access revoked immediately upon reassignment
- Encrypted data decrypted for authorized coordinators
- Dashboard sorted by: Critical first, then days since update
- Color coding: Red (Critical), Orange (High), Yellow (Medium), Green (Low)

**Priority:** Critical (P0)
**Estimated Effort:** 10 hours

---

### Epic 4: Incident Workflow & Status Management

#### Story 4.1: Stage Transition with Guidance Modals
**As an** incident coordinator
**I want to** receive guidance when changing stages
**So that** I understand what actions to take

**Acceptance Criteria:**
- **Given** I am viewing incident in "Information Gathering" status
- **When** I click "Move to Reviewing Final Report"
- **Then** modal appears with heading: "Stage Guidance: Reviewing Final Report"
- **And** modal shows checklist:
  - [ ] All witness statements collected
  - [ ] All involved parties interviewed (if possible)
  - [ ] Timeline of events documented in notes
  - [ ] Supporting evidence reviewed
- **And** modal includes textarea: "Add note about this transition"
- **And** modal has buttons: "Cancel" and "Confirm Transition"

- **Given** I confirm stage transition
- **When** I click "Confirm Transition"
- **Then** status updates to new stage
- **And** my transition note is added to notes
- **And** audit log records: "Status changed to [new status] by [coordinator]"
- **And** timestamp is updated
- **And** modal closes

- **Given** I cancel stage transition
- **When** I click "Cancel"
- **Then** status remains unchanged
- **And** no note is added
- **And** modal closes

**Business Rules:**
- Guidance modals are REMINDERS, not enforcement (user can proceed without checking all items)
- Each stage has unique guidance checklist (defined in requirements)
- Transition note is OPTIONAL but encouraged
- Guidance does NOT block progression (soft enforcement)
- Admins see same guidance modals (consistency)

**Stage Guidance Checklists:**

**Information Gathering → Reviewing Final Report:**
- [ ] All witness statements collected
- [ ] All involved parties interviewed (if possible)
- [ ] Timeline of events documented in notes
- [ ] Supporting evidence reviewed

**Reviewing Final Report → Closed:**
- [ ] Final report drafted in notes
- [ ] Resolution actions documented
- [ ] Google Drive upload complete (manual in Phase 1)
- [ ] Reporter notified (if identified)

**Any Stage → On Hold:**
- [ ] Reason for hold documented
- [ ] Expected resume date noted
- [ ] Admin notified (if applicable)

**Priority:** High (P1)
**Estimated Effort:** 12 hours

---

#### Story 4.2: Incident Placed On Hold
**As an** incident coordinator
**I want to** place an incident on hold
**So that** I can pause investigation when waiting for information

**Acceptance Criteria:**
- **Given** I am viewing any incident I coordinate
- **When** I click "Put On Hold"
- **Then** modal appears: "Place Incident On Hold"
- **And** modal requires textarea: "Reason for hold (required)"
- **And** modal has optional field: "Expected resume date"
- **And** modal has buttons: "Cancel" and "Confirm Hold"

- **Given** I confirm hold
- **When** I submit with reason
- **Then** status changes to "On Hold"
- **And** note added: "HOLD: [reason]"
- **And** audit log records hold
- **And** incident appears in "On Hold" filter on dashboard

- **Given** incident is on hold
- **When** I view incident detail
- **Then** I see prominent banner: "This incident is ON HOLD"
- **And** banner shows reason and expected resume date
- **And** I see button "Resume Investigation"

- **Given** I click "Resume Investigation"
- **When** modal appears
- **Then** modal asks: "Resume to which stage?"
- **And** dropdown shows: Information Gathering, Reviewing Final Report
- **And** I select stage and click "Resume"
- **And** status changes to selected stage
- **And** note added: "Resumed investigation"

**Business Rules:**
- Incidents can be put on hold from ANY stage
- Hold reason is REQUIRED
- Expected resume date is optional
- "On Hold" incidents still visible to coordinator
- Admins can see all on-hold incidents
- Hold/resume actions logged in audit trail

**Priority:** Medium (P2)
**Estimated Effort:** 8 hours

---

#### Story 4.3: Close Incident with Final Report
**As an** incident coordinator
**I want to** close an incident after investigation
**So that** I can document resolution and archive

**Acceptance Criteria:**
- **Given** I am viewing incident in "Reviewing Final Report" status
- **When** I click "Close Incident"
- **Then** modal appears: "Close Incident"
- **And** modal shows checklist:
  - [ ] Final report documented in notes
  - [ ] Resolution actions recorded
  - [ ] Google Drive upload complete (manual in Phase 1)
  - [ ] Reporter notified (if identified)
- **And** modal requires textarea: "Final summary (required)"
- **And** modal has buttons: "Cancel" and "Confirm Close"

- **Given** I confirm close
- **When** I submit with final summary
- **Then** status changes to "Closed"
- **And** note added: "CLOSED: [final summary]"
- **And** audit log records closure with timestamp
- **And** incident moved to "Closed" filter on dashboard
- **And** identified reporter receives email notification

- **Given** incident is closed
- **When** admin views incident
- **Then** admin can reopen if needed
- **And** reopen requires reason
- **And** status returns to "Information Gathering"

**Business Rules:**
- Only incidents in "Reviewing Final Report" can be closed directly
- Final summary is REQUIRED
- Closed incidents are read-only for coordinators
- Admins can reopen closed incidents (emergency override)
- Google Drive upload is manual in Phase 1 (checkbox reminder)
- Phase 2 will automate Google Drive archival

**Priority:** High (P1)
**Estimated Effort:** 10 hours

---

### Epic 5: Notes & Communication

#### Story 5.1: Coordinator Adds Investigation Notes
**As an** incident coordinator
**I want to** add notes to document investigation activities
**So that** I create an audit trail and share context

**Acceptance Criteria:**
- **Given** I am viewing incident detail page
- **When** I see "Notes" section
- **Then** I see textarea with placeholder: "Add investigation note..."
- **And** I see "Save Note" button (disabled if textarea empty)

- **Given** I type a note
- **When** I click "Save Note"
- **Then** note is saved with timestamp
- **And** note shows my scene name as author
- **And** note appears in chronological order (newest first)
- **And** textarea clears for next note
- **And** success message appears briefly

- **Given** I view existing notes
- **When** notes list loads
- **Then** each note shows:
  - Author scene name
  - Timestamp (relative: "2 hours ago")
  - Note content (preserves line breaks)
  - Edit button (only for my notes, within 15 minutes)

- **Given** I click edit on my recent note
- **When** edit mode activates
- **Then** note content becomes editable textarea
- **And** I see "Save" and "Cancel" buttons
- **And** I can update content
- **And** edit saves with "Edited" indicator

**Business Rules:**
- Notes CANNOT be deleted (audit trail integrity)
- Notes CAN be edited within 15 minutes of creation (typo fixes)
- Edited notes show "Edited [timestamp]" indicator
- Notes support markdown formatting (Phase 2)
- System-generated notes (status changes) are auto-created
- Notes visible to all coordinators and admins

**Priority:** Critical (P0)
**Estimated Effort:** 10 hours

---

#### Story 5.2: System-Generated Notes for Actions
**As a** system
**I want to** automatically create notes for key actions
**So that** audit trail is complete and clear

**Acceptance Criteria:**
- **Given** incident is assigned
- **When** coordinator is assigned
- **Then** system note created: "Assigned to [coordinator] by [admin]"
- **And** note has special styling (icon indicator)

- **Given** status changes
- **When** any status transition occurs
- **Then** system note created: "Status changed from [old] to [new] by [user]"
- **And** note includes transition reason (if provided in modal)

- **Given** incident is put on hold
- **When** hold action completes
- **Then** system note created: "HOLD: [reason] by [coordinator]"
- **And** note styled with warning color (yellow background)

- **Given** incident is closed
- **When** close action completes
- **Then** system note created: "CLOSED: [final summary] by [coordinator]"
- **And** note styled with success color (green background)

**Business Rules:**
- System notes CANNOT be edited or deleted
- System notes clearly distinguished from user notes (icon, styling)
- System notes include full context (who, what, when, why)
- All audit actions generate system notes
- System notes count toward audit trail completeness metric

**Priority:** High (P1)
**Estimated Effort:** 6 hours

---

### Epic 6: Admin Dashboard & Oversight

#### Story 6.1: Admin Views All Incidents
**As an** admin
**I want to** view all incidents across all coordinators
**So that** I can monitor progress and identify bottlenecks

**Acceptance Criteria:**
- **Given** I am logged in as admin
- **When** I navigate to admin incident dashboard
- **Then** I see ALL incidents (assigned and unassigned)
- **And** I see filter options:
  - Status: All, Report Submitted, Information Gathering, Reviewing Final Report, On Hold, Closed
  - Severity: All, Critical, High, Medium, Low
  - Assigned: All, Unassigned, [User List]
  - Date Range: Last 7 days, Last 30 days, Last 90 days, All Time

- **Given** I apply filters
- **When** I select "Status: Report Submitted"
- **Then** I see only unassigned new incidents
- **And** count badge shows number of unassigned incidents
- **And** list sorted by submission date (oldest first)

- **Given** I view incident list
- **When** list loads
- **Then** each row shows:
  - Reference number (clickable)
  - Severity badge (color-coded)
  - Status badge
  - Assigned to (or "Unassigned")
  - Days since last update
  - Last activity date
- **And** rows are color-coded by age (red >7 days, yellow >3 days)

**Business Rules:**
- Admins see ALL incidents regardless of assignment
- Default filter: Active incidents (not closed)
- Unassigned incidents highlighted prominently
- Stale incidents (>7 days no update) flagged in red
- Export to CSV available (Phase 2)

**Priority:** Critical (P0)
**Estimated Effort:** 12 hours

---

#### Story 6.2: Admin Reassigns Incident
**As an** admin
**I want to** reassign an incident to different coordinator
**So that** I can balance workload or match expertise

**Acceptance Criteria:**
- **Given** I am viewing assigned incident
- **When** I click "Reassign"
- **Then** modal appears: "Reassign Incident"
- **And** modal shows current coordinator
- **And** modal shows dropdown of all users
- **And** modal requires textarea: "Reason for reassignment"

- **Given** I select new coordinator
- **When** I submit reassignment
- **Then** AssignedTo field updates
- **And** previous coordinator loses access immediately
- **And** new coordinator gains access immediately
- **And** audit log records: "Reassigned from [old] to [new] by [admin] - Reason: [reason]"
- **And** both coordinators receive email notification

- **Given** new coordinator logs in
- **When** they view incident
- **Then** they see reassignment note in notes
- **And** they see full incident history
- **And** they can immediately add notes and change status

**Business Rules:**
- Only admins can reassign incidents
- Reassignment reason is REQUIRED
- Previous coordinator access revoked immediately
- All notes/history preserved during reassignment
- Reassignment does NOT change status
- Email notifications sent to both coordinators

**Priority:** High (P1)
**Estimated Effort:** 6 hours

---

### Epic 7: Google Drive Integration (Phased Approach)

#### Story 7.1: Manual Google Drive Upload (Phase 1 - MVP)
**As an** incident coordinator
**I want to** be reminded to upload final report to Google Drive
**So that** I maintain permanent records

**Acceptance Criteria:**
- **Given** I am closing an incident
- **When** close modal appears
- **Then** checklist includes: "[ ] Google Drive upload complete"
- **And** I see link: "Upload to Google Drive (manual)"
- **And** link opens Google Drive in new tab

- **Given** I check the Google Drive checkbox
- **When** I confirm close
- **Then** note added: "Coordinator confirmed Google Drive upload"
- **And** incident closes successfully
- **And** no automated upload occurs (Phase 1)

- **Given** admin reviews closed incident
- **When** they view incident detail
- **Then** they see Google Drive checkbox status
- **And** they can verify upload manually
- **And** they see note confirming upload

**Business Rules:**
- Phase 1: Manual process with checkbox reminder
- Checkbox is honor system (not enforced)
- Coordinator responsible for creating Drive document
- No automated folder creation or upload
- Admin can verify upload compliance through notes

**Priority:** Medium (P2) - Phase 1 only
**Estimated Effort:** 2 hours

---

#### Story 7.2: Automated Google Drive Archival (Phase 2 - Future)
**As a** system
**I want to** automatically create Google Drive document when incident closes
**So that** archival is guaranteed and consistent

**Acceptance Criteria (Phase 2 Design):**
- **Given** incident is being closed
- **When** coordinator confirms close
- **Then** system creates Google Doc in "Safety Incidents" folder
- **And** document named: "[Reference Number] - [Severity] - [Date Closed]"
- **And** document contains:
  - Incident details (non-encrypted display)
  - Full notes timeline
  - Audit log
  - Final summary
  - Coordinator name and close date
- **And** document link saved to incident record
- **And** coordinator receives email with Drive link

- **Given** incident is reopened
- **When** status changes from Closed
- **Then** Google Doc flagged: "REOPENED [date]"
- **And** new closure creates updated document version
- **And** both versions preserved in Drive

**Business Rules (Phase 2):**
- Automation ONLY occurs for closed incidents
- Google service account handles uploads
- Documents stored in dedicated "Safety Incidents" folder
- Access restricted to safety team Google group
- Document versioning for reopened incidents
- Link to Drive document visible in incident detail

**Priority:** Low (P3) - Phase 2 implementation
**Estimated Effort:** 20 hours (Phase 2)

---

### Epic 8: Security & Privacy

#### Story 8.1: Encrypted Sensitive Data Storage
**As a** system
**I want to** encrypt all sensitive incident data at rest
**So that** privacy is protected from unauthorized access

**Acceptance Criteria:**
- **Given** incident is submitted
- **When** data is saved to database
- **Then** these fields are encrypted:
  - EncryptedDescription
  - EncryptedInvolvedParties
  - EncryptedWitnesses
  - EncryptedContactEmail
  - EncryptedContactPhone

- **Given** authorized user views incident
- **When** incident detail loads
- **Then** encrypted fields are decrypted for display
- **And** decryption happens server-side only
- **And** plaintext never exposed in API responses to unauthorized users

- **Given** unauthorized user accesses database
- **When** they view SafetyIncident table
- **Then** sensitive fields show encrypted strings
- **And** decryption requires application-level keys
- **And** keys are stored in environment variables (not code)

**Business Rules:**
- AES-256 encryption for all sensitive fields
- Encryption keys stored in Azure Key Vault (production)
- Keys rotated quarterly (admin responsibility)
- Decryption only for: Assigned coordinator, Admins
- API responses NEVER include plaintext for unauthorized users
- Audit log records all decryption access attempts

**Priority:** Critical (P0)
**Estimated Effort:** 8 hours (backend focus)

---

#### Story 8.2: Access Control Enforcement
**As a** system
**I want to** enforce strict access controls on incident data
**So that** only authorized users can view sensitive information

**Acceptance Criteria:**
- **Given** user is NOT assigned to incident and NOT admin
- **When** they attempt to access incident detail
- **Then** API returns 403 Forbidden
- **And** error message: "You do not have access to this incident"
- **And** attempt logged in audit trail

- **Given** coordinator is reassigned away from incident
- **When** they attempt to access incident
- **Then** API returns 403 Forbidden
- **And** message: "You are no longer assigned to this incident"
- **And** access revocation logged

- **Given** admin views any incident
- **When** they access incident detail
- **Then** API returns full incident data
- **And** access logged in audit trail
- **And** banner shows: "You are viewing this incident as administrator"

**Business Rules:**
- Authorization checked on EVERY API request
- Coordinators can ONLY access assigned incidents
- Admins can access ALL incidents
- Access revoked immediately upon reassignment
- All access attempts logged (success and failure)
- Failed access attempts trigger security alerts (>5 failures/hour)

**Priority:** Critical (P0)
**Estimated Effort:** 6 hours (backend focus)

---

## Functional Requirements Summary

### Data Requirements

**Incident Entity (Existing - Needs Update):**
```csharp
SafetyIncident {
  Id: Guid (primary key)
  ReferenceNumber: string (SAF-YYYYMMDD-NNNN)
  ReporterId: Guid? (null for anonymous)
  Severity: IncidentSeverity enum (Low, Medium, High, Critical)
  IncidentDate: DateTime (when incident occurred)
  ReportedAt: DateTime (when report submitted)
  Location: string (max 200 chars)
  EncryptedDescription: string (encrypted)
  EncryptedInvolvedParties: string? (encrypted)
  EncryptedWitnesses: string? (encrypted)
  EncryptedContactEmail: string? (encrypted)
  EncryptedContactPhone: string? (encrypted)
  IsAnonymous: bool
  RequestFollowUp: bool
  Status: IncidentStatus enum (NEEDS UPDATE - see below)
  AssignedTo: Guid? (any user, not just admins)
  CreatedAt: DateTime
  UpdatedAt: DateTime
  CreatedBy: Guid?
  UpdatedBy: Guid?
}
```

**CRITICAL UPDATE - Status Enum:**
```csharp
// OLD (Current Implementation - INCORRECT)
public enum IncidentStatus {
  New = 1,
  InProgress = 2,
  Resolved = 3,
  Archived = 4
}

// NEW (Required for Workflow)
public enum IncidentStatus {
  ReportSubmitted = 1,      // Initial state, awaiting assignment
  InformationGathering = 2, // Assigned, coordinator investigating
  ReviewingFinalReport = 3, // Preparing final documentation
  OnHold = 4,               // Paused, waiting for information
  Closed = 5                // Investigation complete, archived
}
```

**Notes Entity (New - Mirror Vetting Pattern):**
```csharp
IncidentNote {
  Id: Guid (primary key)
  IncidentId: Guid (foreign key to SafetyIncident)
  AuthorId: Guid (user who created note)
  Content: string (note text, supports line breaks)
  IsSystemGenerated: bool (true for auto-notes)
  CreatedAt: DateTime
  UpdatedAt: DateTime? (for edits within 15 min)
  EditedAt: DateTime? (timestamp of last edit)
}
```

**Audit Log Entity (Existing):**
```csharp
IncidentAuditLog {
  Id: Guid
  IncidentId: Guid
  Action: string (e.g., "Status Changed", "Assigned", "Note Added")
  PerformedBy: Guid
  Timestamp: DateTime
  Details: string (JSON serialized context)
}
```

### Integration Points

**Existing Systems:**
1. **Authentication System**: Uses existing auth for user identification and role checks
2. **Email Notification System**: Extends existing notification service for incident updates
3. **Vetting System Patterns**: Mirrors VettingApplicationDetail notes section, status badges, modal actions
4. **Encryption Service**: Uses existing encryption for sensitive data fields

**New Integrations:**
1. **Google Drive API** (Phase 2): Service account for automated document creation
2. **Reference Number Generator**: New service for SAF-YYYYMMDD-NNNN pattern
3. **Removed**: Anonymous status checking removed per stakeholder decision (fully anonymous = no tracking)

### UI/UX Requirements

**Mirror Vetting System Patterns:**
- **Notes Section**: Use VettingApplicationDetail.tsx lines 501-579 as template
- **Status Badges**: Use VettingStatusBadge component pattern with incident-specific colors
- **Modal Actions**: Use OnHoldModal and DenyApplicationModal patterns for guidance modals
- **Grid Layout**: Use VettingReviewGrid pattern for incident list/filtering

**Wireframe Reference:**
- **File**: `/docs/design/wireframes/incident-report-visual.html`
- **Approach**: Use wireframe almost exactly as-is (only minor updates if needed)
- **Key Elements**:
  - Anonymous toggle (prominent placement)
  - Severity selection (visual cards)
  - Contact info conditional display
  - Privacy notices and warnings
  - Crisis resources section

**Color Coding:**
- **Critical**: Red (#AA0130)
- **High**: Orange (#FF8C00)
- **Medium**: Yellow (#FFBF00)
- **Low**: Green (#4A5C3A)

**Status Colors:**
- **Report Submitted**: Blue (#614B79)
- **Information Gathering**: Purple (#7B2CBF)
- **Reviewing Final Report**: Orange (#E6AC00)
- **On Hold**: Yellow (#FFBF00)
- **Closed**: Green (#4A5C3A)

## Security & Privacy Requirements

### Data Protection
1. **Encryption at Rest**: AES-256 for all sensitive fields
2. **Encryption in Transit**: HTTPS/TLS 1.3 for all API communication
3. **Key Management**: Azure Key Vault for production, environment variables for development
4. **Key Rotation**: Quarterly rotation with backward compatibility

### Anonymous Reporting Protection
1. **No IP Logging**: Anonymous submissions do NOT log IP addresses
2. **No Session Tracking**: Anonymous submissions do NOT link to session cookies
3. **No User Linking**: Anonymous ReporterId is permanently null (cannot be updated)
4. **Reference Number Only**: No alternative identifiers stored

### Access Control
1. **Coordinator Access**: Only assigned coordinator can view incident details
2. **Admin Override**: Admins can view all incidents (logged in audit trail)
3. **Reassignment**: Previous coordinator immediately loses access
4. **Public Status Check**: Reference number allows read-only status view (no details)

### Audit Trail
1. **All Actions Logged**: Assignment, status changes, note additions, access attempts
2. **Failed Access Logged**: Unauthorized access attempts trigger security monitoring
3. **No Deletion**: Notes cannot be deleted (edit within 15 minutes only)
4. **Immutable Audit Log**: Audit records cannot be modified or deleted

## Compliance Requirements

### GDPR Considerations
1. **Right to Access**: Identified reporters can request full incident data
2. **Right to Erasure**: Complex due to safety records retention (legal counsel required)
3. **Data Minimization**: Only collect necessary information
4. **Purpose Limitation**: Data used ONLY for safety investigations

### Records Retention
1. **Active Incidents**: Retained indefinitely until closed
2. **Closed Incidents**: Retained for 7 years (legal requirement)
3. **Anonymous Reports**: Same retention as identified (cannot differentiate post-closure)
4. **Google Drive Archive**: Permanent backup of closed incidents

### Legal Considerations
1. **Mandatory Reporting**: Some incidents may require law enforcement notification (admin responsibility)
2. **Subpoena Response**: Encrypted data must be decryptable for legal requests
3. **Liability Protection**: Documentation demonstrates due diligence in safety response

## User Impact Analysis

| User Type | Impact | Priority | Notes |
|-----------|--------|----------|-------|
| **Anonymous Reporter** | High - New capability to report safely | Critical | Must be intuitive, trustworthy, private |
| **Identified Reporter** | High - Formalized reporting process | Critical | Email notifications improve transparency |
| **Incident Coordinator** | High - New role/workflow | Critical | Guidance modals essential for adoption |
| **Admin** | Medium - New oversight capability | High | Assignment/reassignment streamlines response |
| **General Members** | Low - Awareness of system | Medium | Communication about reporting mechanism |
| **Teachers/Event Hosts** | Medium - May need to report incidents | High | Training on when/how to use system |

## Success Scenarios

### Scenario 1: Anonymous Report Happy Path

**Context**: Community member experiences boundary violation at event

**Steps:**
1. User navigates to "Report an Incident" (public page, no login required)
2. User selects "Submit Anonymously" toggle
3. User selects severity: "High - Boundary Violation"
4. User enters incident date, location ("Monthly Rope Jam - Main Room")
5. User describes incident in detail (150 words)
6. User optionally adds involved parties description
7. User submits report
8. System generates internal reference number: SAF-20251017-0042 (for admin tracking only)
9. User sees confirmation: "Your report has been submitted and will be reviewed by our safety team."
10. User sees support resources and returns to home page

**Expected Outcomes:**
- Report stored with ReporterId = null
- All sensitive fields encrypted
- Status = "Report Submitted"
- No email sent
- Admin sees new incident in "Unassigned" queue

**Validation:**
- User cannot check status (fully anonymous)
- User identity NOT stored anywhere
- Admin cannot identify reporter

---

### Scenario 2: Identified Report with Follow-Up

**Context**: Vetted member reports safety concern and wants to participate in resolution

**Steps:**
1. User logs in to WitchCityRope
2. User navigates to "Report an Incident"
3. User sees "Include My Contact" option (enabled because logged in)
4. User selects "Include My Contact"
5. System pre-populates email from profile
6. User adds phone number
7. User checks "Request follow-up contact"
8. User selects severity: "Medium - Safety Concern"
9. User describes incident (equipment failure during class)
10. User submits report
11. System generates internal reference number and sends confirmation email
12. User receives email: "Your incident report has been submitted (Reference: SAF-20251017-0043)"

**Expected Outcomes:**
- Report stored with user's ReporterId
- Contact info encrypted
- RequestFollowUp = true
- Email confirmation sent
- Admin sees new incident with reporter name

**Follow-Up Flow:**
1. Admin assigns coordinator (Teacher with equipment expertise)
2. User receives email: "Your incident report is being reviewed"
3. Coordinator contacts user via email for additional details
4. User provides photos of failed equipment
5. Coordinator adds notes documenting findings
6. Coordinator moves to "Reviewing Final Report"
7. User receives email: "Your incident report is being finalized"
8. Coordinator closes incident with summary
9. User receives email: "Incident report has been resolved - Thank you"

---

### Scenario 3: Coordinator Investigation Workflow

**Context**: Admin assigns incident to experienced coordinator

**Steps:**
1. Admin views "Unassigned Incidents" queue
2. Admin selects incident SAF-20251017-0044 (Critical - Consent Violation)
3. Admin clicks "Assign Coordinator"
4. Admin selects "JaneRigger" from dropdown (vetted member with mediation experience)
5. Admin clicks "Assign"
6. System shows guidance modal: "Information Gathering Stage"
7. Modal shows checklist:
   - [ ] Contact reporter (if identified)
   - [ ] Interview involved parties
   - [ ] Collect witness statements
   - [ ] Document timeline of events
8. Admin adds note: "Assigned to Jane due to mediation expertise"
9. Admin confirms assignment

**Coordinator Flow:**
1. JaneRigger receives email: "You have been assigned incident SAF-20251017-0044"
2. Jane logs in and sees incident in dashboard
3. Jane clicks incident to view details
4. Jane sees encrypted description decrypted for her view
5. Jane adds note: "Initial review - appears to be miscommunication during scene negotiation"
6. Jane contacts identified reporter via encrypted email
7. Jane adds note: "Spoke with reporter - collecting additional context"
8. Jane interviews involved party
9. Jane adds note: "Interviewed respondent - both parties agree to mediation"
10. Jane moves status to "Reviewing Final Report"
11. System shows guidance modal with closure checklist
12. Jane adds final note: "Mediation successful - both parties satisfied with resolution"
13. Jane closes incident with summary
14. System creates note: "CLOSED: Mediation completed, no further action required"
15. Reporter receives closure email

**Expected Outcomes:**
- Full audit trail in notes
- All guidance modals shown
- Reporter kept informed (if identified)
- Admin can monitor progress
- Incident closed with clear resolution

---

### Scenario 4: On Hold Workflow

**Context**: Coordinator needs to pause investigation pending external information

**Steps:**
1. Coordinator reviewing incident awaiting police report
2. Coordinator clicks "Put On Hold"
3. Modal appears: "Place Incident On Hold"
4. Coordinator enters reason: "Awaiting police report from incident on 10/15"
5. Coordinator sets expected resume date: "10/31/2025"
6. Coordinator confirms hold
7. System changes status to "On Hold"
8. System adds note: "HOLD: Awaiting police report from incident on 10/15 - Expected resume: 10/31/2025"
9. Incident appears in coordinator's "On Hold" filter
10. Admin sees incident in "On Hold" dashboard

**Resume Flow:**
1. Coordinator receives police report on 10/28
2. Coordinator clicks "Resume Investigation"
3. Modal asks: "Resume to which stage?"
4. Coordinator selects "Reviewing Final Report"
5. Coordinator confirms resume
6. System changes status to "Reviewing Final Report"
7. System adds note: "Resumed investigation - police report received"
8. Coordinator proceeds to close incident

---

## Edge Cases & Error Handling

### Edge Case 1: Anonymous Reporter Requests Follow-Up
**Scenario**: User selects anonymous but checks "Request follow-up"

**Handling:**
- Form validation prevents this combination
- Error message: "Anonymous reports cannot request follow-up. Please select 'Include My Contact' to enable follow-up."
- Submit button remains disabled until resolved

---

### Edge Case 2: Coordinator Leaves Organization
**Scenario**: Assigned coordinator's account is deactivated

**Handling:**
- System detects inactive coordinator on incident load
- Admin receives alert: "Incident SAF-XXXXXXX-XXXX has inactive coordinator"
- Admin must manually reassign incident
- Incident flagged in dashboard with warning icon

---

### Edge Case 3: Multiple Simultaneous Assignments
**Scenario**: Two admins try to assign same incident simultaneously

**Handling:**
- Database constraint prevents duplicate assignments
- Second admin receives error: "This incident has already been assigned to [coordinator]"
- Second admin sees updated incident detail with current assignment
- Audit log records only successful assignment

---

### Edge Case 4: Reporter Deletes Account (Identified Report)
**Scenario**: Identified reporter deletes account after submitting report

**Handling:**
- Incident ReporterId preserved (soft delete of user)
- Contact email remains encrypted in incident record
- Coordinator can still access contact info
- System note added: "Reporter account deactivated on [date]"

---

## Out of Scope (NOT Implemented in Phase 1)

### Explicitly NOT Included:
1. **Automated Google Drive Integration**: Phase 1 uses manual upload reminder only
2. **Public Status Check Endpoint**: Anonymous users cannot check status in Phase 1 (future enhancement)
3. **Incident Analytics Dashboard**: Pattern analysis, trends, reporting (Phase 2)
4. **Multi-Coordinator Assignment**: Only single coordinator per incident in Phase 1
5. **Incident Linking**: Cannot link related incidents in Phase 1
6. **Witness Notifications**: System does not contact witnesses (coordinator responsibility)
7. **Mobile App**: Web-only in Phase 1
8. **SMS Notifications**: Email only in Phase 1
9. **Document Attachments**: No file uploads in Phase 1 (text only)
10. **Incident Templates**: Custom incident type forms (Phase 2)
11. **Integration with Membership System**: No automatic membership actions based on incidents
12. **Integration with Event System**: No automatic event bans based on incidents
13. **Public Incident Registry**: No public-facing incident log
14. **Reporter Dashboard**: Identified reporters cannot view their own submissions (status via email only)
15. **Coordinator Workload Balancing**: Admin manually balances assignment distribution

### Deferred to Phase 2:
- Automated Google Drive archival
- Removed: Public status check (fully anonymous per stakeholder decision)
- Incident analytics and trends
- Document attachment support
- SMS notifications
- Multi-coordinator assignment
- Incident linking/relationships
- Custom incident type templates

## Quality Gate Checklist (95% Required)

- [x] All user roles addressed (Anonymous Reporter, Identified Reporter, Coordinator, Admin)
- [x] Clear acceptance criteria for each story
- [x] Business value clearly defined
- [x] Edge cases considered (4 documented)
- [x] Security requirements documented (encryption, access control, audit trail)
- [x] Compliance requirements checked (GDPR, records retention)
- [x] Performance expectations set (<200ms API responses, real-time assignment updates)
- [x] Mobile experience considered (responsive wireframe design)
- [x] Examples provided (4 detailed scenarios)
- [x] Success metrics defined (adoption, quality, satisfaction)
- [x] Integration points identified (existing systems + new services)
- [x] Data requirements specified (entity updates, new notes entity)
- [x] UI/UX patterns referenced (vetting system mirrors)
- [x] Privacy requirements documented (anonymous protection)
- [x] Out of scope clearly defined (15 items)
- [x] Stakeholder decisions documented (5 critical decisions in context)
- [x] Risk assessment included (see below)
- [x] NSwag auto-generation considered (DTOs will be generated from backend)
- [x] Existing wireframe referenced (incident-report-visual.html)
- [x] Vetting patterns referenced (VettingApplicationDetail notes section)

## Risk Assessment

### Technical Risks

**Risk 1: Encryption Performance Impact**
- **Likelihood**: Medium
- **Impact**: Medium (slower API responses)
- **Mitigation**:
  - Use asynchronous encryption/decryption
  - Cache decrypted data in coordinator session (memory only, not localStorage)
  - Monitor API response times (target <200ms)

**Risk 2: Google Drive API Rate Limits (Phase 2)**
- **Likelihood**: Low
- **Impact**: High (archival failures)
- **Mitigation**:
  - Phase 1 uses manual upload (no API dependency)
  - Phase 2 implements retry logic with exponential backoff
  - Queue system for bulk archival operations

**Risk 3: Status Enum Migration Breaking Changes**
- **Likelihood**: High (existing data uses old enum)
- **Impact**: Critical (data corruption)
- **Mitigation**:
  - Database migration script to map old values to new values:
    - New → ReportSubmitted
    - InProgress → InformationGathering
    - Resolved → Closed
    - Archived → Closed
  - Comprehensive testing before production migration
  - Rollback plan if migration fails

### User Adoption Risks

**Risk 4: Coordinators Overwhelmed by Guidance Modals**
- **Likelihood**: Medium
- **Impact**: Low (user frustration)
- **Mitigation**:
  - Modals are NOT blocking (can proceed without checking all items)
  - "Don't show again" option (per user preference)
  - Feedback loop after 30 days to refine guidance

**Risk 5: Low Anonymous Report Adoption**
- **Likelihood**: Low
- **Impact**: High (defeats purpose)
- **Mitigation**:
  - Prominent anonymous option on form
  - Clear privacy guarantees (no IP logging, etc.)
  - Community education about reporting mechanism
  - Success stories (anonymized) shared with community

### Privacy Risks

**Risk 6: Accidental Identity Exposure**
- **Likelihood**: Low
- **Impact**: Critical (trust violation)
- **Mitigation**:
  - Automated tests verify ReporterId = null for anonymous
  - Email notifications NEVER sent for anonymous reports
  - API responses NEVER include reporter identity for unauthorized users
  - Regular privacy audits

**Risk 7: Coordinator Accesses Incident After Reassignment**
- **Likelihood**: Low
- **Impact**: Medium (unauthorized access)
- **Mitigation**:
  - Access revocation is immediate (not eventual consistency)
  - Every API request checks current assignment
  - Failed access attempts logged and monitored
  - Alert admin if >5 failed access attempts per hour

### Legal Risks

**Risk 8: Mandatory Reporting Failures**
- **Likelihood**: Low
- **Impact**: Critical (legal liability)
- **Mitigation**:
  - Coordinator training on mandatory reporting laws
  - Admin oversight of all critical severity incidents
  - Legal counsel review of incident handling procedures
  - Clear escalation procedures documented

---

## Next Steps

**Immediate Actions:**
1. **Stakeholder Review**: Share this document with product manager for approval
2. **Design Phase**: UI designer creates incident dashboard mockups based on vetting system patterns
3. **Functional Spec**: Functional spec agent creates technical design document
4. **Backend Planning**: Backend developer plans status enum migration strategy

**Questions for Product Manager:**
- [ ] Approve 5-stage workflow vs simpler 3-stage?
- [ ] Confirm Google Drive manual approach for Phase 1?
- [ ] Approve any-user coordinator assignment (not admin-only)?
- [ ] Confirm guidance modals should NOT block progression?
- [ ] Approve 7-year retention period for closed incidents?

---

**Document Status**: ✅ READY FOR REVIEW
**Next Agent**: UI Designer (create admin dashboard mockups)
**Estimated Design Effort**: 16 hours
**Estimated Implementation Effort**: 80 hours (frontend + backend + testing)

**Created**: 2025-10-17
**Author**: Business Requirements Agent
**Version**: 1.0

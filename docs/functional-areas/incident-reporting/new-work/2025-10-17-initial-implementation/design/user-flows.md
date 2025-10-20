# User Flows: Incident Reporting System
<!-- Last Updated: 2025-10-17 -->
<!-- Version: 1.0 -->
<!-- Owner: UI Designer Agent -->
<!-- Status: Draft - Awaiting Approval -->

## Overview

This document defines the complete user journey flows for the Incident Reporting System across all user personas. Each flow includes screen-by-screen navigation, decision points, system actions, and user feedback.

---

## Flow 1: Anonymous Report Submission

**Persona**: Anonymous Reporter
**Goal**: Submit an incident report without revealing identity
**Entry Point**: Public navigation "Report an Incident" link

### Flow Diagram

```
START
  ↓
[1] Public Incident Report Form
  ├─ Toggle: "Submit Anonymously" (ON by default)
  ├─ Select Severity: Low | Medium | High | Critical
  ├─ Enter Incident Details
  └─ Submit Form
  ↓
[2] Form Validation
  ├─ Valid → Continue
  └─ Invalid → Show errors, stay on form
  ↓
[3] System Actions
  ├─ Generate reference number (SAF-YYYYMMDD-NNNN)
  ├─ Encrypt sensitive data (description, parties, witnesses)
  ├─ Save to database (ReporterId = null)
  └─ Create system note: "Anonymous submission received"
  ↓
[4] Confirmation Screen
  ├─ "Your report has been submitted"
  ├─ Privacy notice (no status updates available)
  ├─ Support resources displayed
  └─ NO reference number shown
  ↓
[5] User Actions
  ├─ [Return Home] → Navigate to homepage
  └─ (Viewing support resources)
  ↓
END
```

### Screen Details

#### Screen 1: Public Incident Report Form

**URL**: `/report-incident`
**Authentication**: Not required

**Key Elements**:
- Anonymous toggle (default: ON)
- Severity selection (4 visual cards)
- Incident date/time picker
- Location field (text input or dropdown)
- Description textarea (minimum 20 characters)
- Involved parties textarea (optional)
- Witnesses textarea (optional)
- NO contact information fields (anonymous)
- Submit button (disabled until valid)

**Validation Rules**:
- Severity: Required
- Incident date: Required
- Location: Required (min 3 characters)
- Description: Required (min 20 characters)
- Involved parties: Optional
- Witnesses: Optional

**User Interactions**:
```
User clicks "Report an Incident" → Form loads
User sees "Submit Anonymously" toggle (checked) → Identity fields hidden
User selects severity card → Card highlights, validation updates
User fills incident details → Real-time validation
User clicks "Submit" → Form validates
  If valid → Processing spinner → Confirmation
  If invalid → Red borders on errors, focus first error
```

#### Screen 2: Anonymous Confirmation

**URL**: `/report-incident/confirmation`
**Authentication**: Not required

**Key Elements**:
- Success icon (green checkmark)
- Heading: "Report Submitted"
- Message: "Your report has been submitted and will be reviewed by our safety team."
- Privacy notice: "This report was submitted anonymously. We cannot provide status updates."
- Support resources box:
  - National Sexual Assault Hotline: 1-800-656-HOPE (4673)
  - LGBTQ National Hotline: 1-888-843-4564
  - Community Support: safety@witchcityrope.com
- "Return Home" button

**System Behavior**:
- No email sent (anonymous submission)
- User cannot navigate back to form (prevents duplicate submission)
- Breadcrumb: "Home > Report Incident > Confirmation"

---

## Flow 2: Identified Report Submission

**Persona**: Identified Reporter (Authenticated)
**Goal**: Submit incident report with contact information for follow-up
**Entry Point**: Authenticated navigation "Report an Incident"

### Flow Diagram

```
START
  ↓
[1] Login Check
  ├─ Logged in → Continue to form
  └─ Not logged in → Redirect to login → Return to form
  ↓
[2] Public Incident Report Form
  ├─ Toggle: "Submit Anonymously" (OFF)
  ├─ Contact fields visible (email pre-populated)
  ├─ "Request follow-up" checkbox
  ├─ Select Severity
  ├─ Enter Incident Details
  └─ Submit Form
  ↓
[3] Form Validation
  ├─ Valid → Continue
  └─ Invalid → Show errors, stay on form
  ↓
[4] System Actions
  ├─ Generate reference number (SAF-YYYYMMDD-NNNN)
  ├─ Encrypt sensitive data + contact info
  ├─ Save to database (ReporterId = currentUser.id)
  ├─ Create system note: "Report submitted by [Scene Name]"
  └─ Send confirmation email
  ↓
[5] Confirmation Screen
  ├─ "Your report has been submitted"
  ├─ "Check 'My Reports' for updates"
  ├─ Support resources
  └─ NO reference number shown (internal use only)
  ↓
[6] User Actions
  ├─ [View My Reports] → Navigate to /my-reports
  └─ [Return Home] → Navigate to homepage
  ↓
END
```

### Screen Details

#### Screen 1: Identified Incident Report Form

**URL**: `/report-incident`
**Authentication**: Required

**Key Elements** (Additional to anonymous):
- Toggle: "Submit Anonymously" (OFF)
- Contact section (visible):
  - Email: Pre-populated from user profile (read-only)
  - Phone: Optional text input
  - "Request follow-up contact" checkbox
- All other fields same as anonymous

**Validation Rules** (Additional):
- Email: Required, valid format, auto-filled
- Phone: Optional, valid format if provided

**User Interactions**:
```
User clicks "Report an Incident" → Check login
  If logged in → Form loads with email pre-filled
  If not logged in → Redirect to login → Return to form

User sees "Submit Anonymously" toggle (unchecked) → Contact fields visible
User can optionally check "Request follow-up contact"
User fills incident details → Real-time validation
User clicks "Submit" → Form validates
  If valid → Processing spinner → Confirmation + Email sent
  If invalid → Red borders on errors, focus first error
```

#### Screen 2: Identified Confirmation

**URL**: `/report-incident/confirmation`
**Authentication**: Required

**Key Elements**:
- Success icon (green checkmark)
- Heading: "Report Submitted"
- Message: "Your report has been received and you will be contacted if additional information is needed."
- Follow-up note: "You can view the status of your reports in My Reports section."
- Support resources box (same as anonymous)
- Action buttons:
  - "View My Reports" (primary)
  - "Return Home" (secondary)

**System Behavior**:
- Email sent to reporter:
  - Subject: "Incident Report Received"
  - Body: Confirmation message, support resources
  - NO reference number in email (internal use only)
- User can immediately view report in "My Reports"

---

## Flow 3: Admin Assigns Incident Coordinator

**Persona**: Admin
**Goal**: Assign unassigned incident to appropriate coordinator
**Entry Point**: Admin Incident Dashboard

### Flow Diagram

```
START
  ↓
[1] Admin Incident Dashboard
  ├─ Filter: Status = "Report Submitted" (unassigned)
  ├─ View unassigned queue alert (if any)
  └─ Click incident row OR "Assign" action
  ↓
[2] Coordinator Assignment Modal Opens
  ├─ User search dropdown (ALL users, not just admins)
  ├─ Search by scene name or real name
  └─ Select coordinator
  ↓
[3] Review Coordinator Details
  ├─ Scene name
  ├─ Real name
  ├─ Current active incident count
  ├─ Role
  └─ Confirm selection
  ↓
[4] Click "Assign Coordinator"
  ↓
[5] System Actions
  ├─ Update incident: AssignedTo = selectedUser.id
  ├─ Change status: ReportSubmitted → InformationGathering
  ├─ Create system note: "Assigned to [coordinator] by [admin]"
  ├─ Send email to coordinator
  └─ Show guidance modal: "Information Gathering" stage
  ↓
[6] Information Gathering Guidance Modal
  ├─ Checklist (not enforced)
  ├─ Optional Google Drive link field
  ├─ Optional note field
  └─ Click "Begin Information Gathering"
  ↓
[7] Confirmation
  ├─ Success notification
  ├─ Dashboard refreshes
  ├─ Incident removed from unassigned queue
  └─ Incident visible to assigned coordinator
  ↓
END
```

### Screen Details

#### Screen 1: Admin Incident Dashboard - Unassigned Filter

**URL**: `/admin/incidents?status=ReportSubmitted`
**Authentication**: Required (Admin role)

**Key Elements**:
- Unassigned queue alert (prominent, orange):
  ```
  ⚠ 3 incidents awaiting assignment [View Unassigned →]
  ```
- Filters:
  - Status: "Report Submitted" (default for unassigned)
  - Severity: All
  - Assigned: "Unassigned" (default)
- Incident table:
  - Reference number
  - Severity badge (Critical/High highlighted)
  - Status badge ("Report Submitted")
  - **Assigned To**: "Unassigned" (bold, dimmed text)
  - Last Updated: Relative time with aging color
  - Actions: "Assign Coordinator" (primary action)

**User Interactions**:
```
Admin navigates to dashboard → Sees unassigned count
Admin clicks "View Unassigned" alert → Filters applied automatically
Admin sees list sorted by: Critical first, then submission date
Admin clicks incident row → Detail page
  OR
Admin clicks actions menu (•••) → "Assign Coordinator"
  ↓ Opens assignment modal
```

#### Screen 2: Coordinator Assignment Modal

**Component**: CoordinatorAssignmentModal
**Trigger**: Click "Assign Coordinator"

**Key Elements**:
- Title: "Assign Incident Coordinator"
- Instruction text: "Select a user to coordinate this incident. Any user can be assigned..."
- Searchable dropdown:
  - All users listed (no role filtering)
  - Format: "SceneName (RealName)"
  - Description: "X active incidents"
  - Searchable by scene name or real name
- Selected user details card:
  - Scene name
  - Real name
  - Current active incident count
  - Role
- Guidance alert:
  - "This user will be responsible for managing this incident..."
  - "They will receive email notification upon assignment."
- Actions:
  - Cancel (closes modal)
  - Assign Coordinator (disabled until user selected, purple)

**User Interactions**:
```
Modal opens → Dropdown focuses
Admin types "Jane" → Dropdown filters users
Admin selects "JaneRigger (Jane Smith)" → Details card appears
Admin reviews:
  - Current workload: 2 active incidents
  - Role: Vetted Member (not admin - this is allowed!)
Admin clicks "Assign Coordinator" → Processing
  ↓ System updates incident
  ↓ Email sent to coordinator
  ↓ Guidance modal opens automatically
```

#### Screen 3: Information Gathering Guidance Modal

**Component**: StageGuidanceModal (variant: 'information-gathering')
**Trigger**: Automatic after assignment

**Key Elements**:
- Title: "Moving to Information Gathering"
- Guidance text: "The coordinator will now begin gathering additional information..."
- Checklist (NOT enforced - user can skip):
  - [ ] Coordinator has been assigned (already done)
  - [ ] Initial review of incident details complete
  - [ ] Google Drive folder created (manual link below)
- Optional Google Drive link field
- Optional note field: "Add a note about this transition..."
- Alert (blue): "These recommendations are guidance only. You may proceed without completing all items."
- Actions:
  - Cancel (closes modal, incident still assigned)
  - Begin Information Gathering (purple, proceeds regardless of checklist)

**User Interactions**:
```
Guidance modal opens automatically
Admin reviews checklist (informational only)
Admin optionally:
  - Pastes Google Drive link
  - Adds assignment note
Admin clicks "Begin Information Gathering"
  ↓ Status updates to InformationGathering
  ↓ Success notification
  ↓ Dashboard refreshes
  ↓ Modal closes
```

**Email to Coordinator** (JaneRigger):
```
Subject: Incident Assigned: SAF-20251017-0042

You have been assigned as the coordinator for incident SAF-20251017-0042.

Severity: Critical
Status: Information Gathering
Assigned by: AdminUser

You can view the incident details and begin your investigation here:
[View Incident Details →]

If you have questions, contact safety@witchcityrope.com

WitchCityRope Safety Team
```

---

## Flow 4: Coordinator Investigation Workflow

**Persona**: Incident Coordinator (Any User)
**Goal**: Investigate incident from assignment to resolution
**Entry Point**: Email notification OR Coordinator dashboard

### Flow Diagram

```
START (Coordinator receives assignment email)
  ↓
[1] Coordinator Dashboard
  ├─ View assigned incidents only
  ├─ Sort by: Critical first, days since update
  └─ Click incident row
  ↓
[2] Incident Detail Page
  ├─ Review incident details (decrypted)
  ├─ Review people involved (encrypted data visible)
  ├─ Add investigation notes (manual)
  └─ Current status: InformationGathering
  ↓
[3] Investigation Activities
  ├─ Contact reporter (if identified)
  ├─ Interview involved parties
  ├─ Collect witness statements
  ├─ Document in notes after each action
  └─ Upload documents to Google Drive (manual - Phase 1)
  ↓
[4] Investigation Complete
  ├─ All parties contacted
  ├─ Draft resolution prepared
  └─ Click "Change Status" → "Reviewing Final Report"
  ↓
[5] Reviewing Final Report Guidance Modal
  ├─ Checklist (not enforced)
  ├─ Optional Google Drive document link
  ├─ Optional transition note
  └─ Click "Move to Final Review"
  ↓
[6] Finalization Stage
  ├─ Status: ReviewingFinalReport
  ├─ Add final notes
  ├─ Prepare closure summary
  └─ Click "Change Status" → "Close Incident"
  ↓
[7] Close Incident Modal
  ├─ Checklist (not enforced)
  ├─ **REQUIRED** Final summary field
  ├─ Optional Google Drive final report link
  ├─ Checkbox: "Google Drive upload complete" (manual reminder)
  └─ Click "Close Incident"
  ↓
[8] System Actions
  ├─ Status: Closed
  ├─ Create system note: "CLOSED: [final summary]"
  ├─ Send email to reporter (if identified)
  └─ Incident archived (read-only for coordinator)
  ↓
[9] Confirmation
  ├─ Success notification
  ├─ Dashboard refreshes
  └─ Incident removed from active list
  ↓
END
```

### Screen Details

#### Screen 1: Coordinator Dashboard

**URL**: `/coordinator/incidents` OR `/my-incidents`
**Authentication**: Required (Assigned coordinator)

**Key Elements**:
- Page title: "My Assigned Incidents"
- Filters:
  - Status: All, InformationGathering, ReviewingFinalReport, OnHold (no access to unassigned or others' incidents)
  - Severity: All, Critical, High, Medium, Low
  - Date Range: Last 7/30/90 days, All
- Incident table (ONLY assigned incidents):
  - Reference number
  - Severity badge
  - Status badge
  - Last Updated (with aging colors)
  - Actions: "View Details"
- Sorting:
  - Critical incidents first
  - Then by days since update (oldest first)
  - "On Hold" incidents have yellow background

**Access Control**:
- Coordinator sees ONLY incidents assigned to them
- No visibility to unassigned or other coordinators' incidents
- If reassigned away, incident immediately disappears from view

**User Interactions**:
```
Coordinator logs in → Navigates to "My Incidents"
Coordinator sees 2 assigned incidents:
  - SAF-20251017-0042 (Critical, InformationGathering, 2 days old)
  - SAF-20251015-0038 (Medium, OnHold, 5 days old)

Coordinator clicks SAF-20251017-0042 row → Detail page loads
```

#### Screen 2: Incident Detail Page - Coordinator View

**URL**: `/coordinator/incidents/[id]`
**Authentication**: Required (Assigned coordinator OR Admin)

**Key Elements**:
- Header card:
  - Severity + Status badges (large)
  - Internal reference number
  - Assigned to: [Coordinator Scene Name] (self)
  - Last updated timestamp
  - Action buttons:
    - "Change Status" (primary)
    - "Put On Hold" (if needed)
    - Options menu (•••): Google Drive links, etc.

- Incident details card:
  - Date & time
  - Location
  - Severity
  - Reporter: Anonymous OR [Scene Name] + "Follow-up Requested" badge
  - **Description**: Decrypted sensitive data (coordinator has access)

- People involved card:
  - Involved parties: Decrypted
  - Witnesses: Decrypted
  - (Coordinator can see all encrypted data)

- **Notes section** (CRITICAL - mirrors vetting):
  - Add note textarea (4 rows)
  - "Save Note" button (disabled if empty)
  - Notes list:
    - System notes: Purple background, "SYSTEM" badge
    - Manual notes: Gray background, note icon
    - Chronological order (newest first)
    - Each shows: Author, timestamp, content

- Google Drive section (Phase 1 manual):
  - Checkbox: "Investigation documents uploaded"
  - "Open Google Drive" link (new tab)

**User Interactions**:
```
Coordinator views incident details
Coordinator reviews decrypted description:
  "During rope jam on 10/15, [person description] continued scene after
   safeword was called. Multiple witnesses present."

Coordinator adds note:
  "Initial contact made with reporter. Gathering witness statements.
   Contacted 2 of 3 witnesses so far."
Coordinator clicks "Save Note"
  → Note appears in list immediately
  → Note shows: "JaneRigger, 2 minutes ago"

Coordinator continues investigation over several days...
Coordinator adds more notes documenting activities...

Coordinator clicks "Change Status"
  → Dropdown appears: "Move to Reviewing Final Report"
Coordinator selects → Guidance modal opens
```

#### Screen 3: Reviewing Final Report Guidance Modal

**Component**: StageGuidanceModal (variant: 'final-review')
**Trigger**: Click "Change Status" → "Reviewing Final Report"

**Key Elements**:
- Title: "Moving to Reviewing Final Report"
- Guidance text: "The investigation is complete and the coordinator is preparing the final report..."
- Checklist (NOT enforced):
  - [ ] Investigation complete
  - [ ] All relevant parties contacted
  - [ ] Draft resolution prepared in Google Drive
- Optional Google Drive document link field
- Optional transition note
- Actions:
  - Cancel
  - Move to Final Review (purple)

**User Interactions**:
```
Modal opens
Coordinator reviews checklist (informational)
Coordinator pastes Google Drive link:
  "https://drive.google.com/document/d/..."
Coordinator adds note:
  "All witnesses interviewed. Draft resolution prepared.
   Recommending mediation between parties."
Coordinator clicks "Move to Final Review"
  ↓ Status: ReviewingFinalReport
  ↓ System note created with transition
  ↓ Modal closes
  ↓ Detail page refreshes
```

#### Screen 4: Close Incident Modal

**Component**: StageGuidanceModal (variant: 'close')
**Trigger**: Click "Change Status" → "Close Incident"

**Key Elements**:
- Title: "Closing Incident Report"
- Guidance text: "Once closed, this incident will move to archived status..."
- Checklist (NOT enforced):
  - [ ] Final report documented in notes
  - [ ] All relevant notes added
  - [ ] Reporter notified (if identified)
  - [ ] Google Drive upload complete (Phase 1 manual)
- **REQUIRED** Final summary textarea:
  - Placeholder: "Provide a final summary of the incident resolution..."
  - Minimum 20 characters
  - Validation: Required, cannot close without summary
- Optional Google Drive final report link
- Actions:
  - Cancel
  - Close Incident (green, disabled until summary entered)

**User Interactions**:
```
Modal opens
Coordinator reviews checklist
Coordinator enters final summary:
  "Mediation successfully completed between both parties. Both agreed
   to communication guidelines for future events. No further action
   required. Documentation uploaded to Google Drive."

Coordinator pastes Google Drive final report link
Coordinator clicks "Close Incident"
  → Validation checks final summary (required)
  → If valid, continues
  ↓ Status: Closed
  ↓ System note: "CLOSED: [final summary]"
  ↓ Email sent to reporter (if identified)
  ↓ Success notification
  ↓ Navigate back to coordinator dashboard
  ↓ Incident no longer in active list
```

**Email to Identified Reporter**:
```
Subject: Incident Report Resolved

Your incident report has been reviewed and resolved.

Thank you for reporting this to our safety team. Your report helps us
maintain a safe community for all members.

If you have additional questions, contact safety@witchcityrope.com

WitchCityRope Safety Team
```

---

## Flow 5: Put Incident On Hold (Mid-Investigation)

**Persona**: Coordinator OR Admin
**Goal**: Pause investigation pending external information
**Entry Point**: Incident detail page during investigation

### Flow Diagram

```
START (Coordinator reviewing incident)
  ↓
[1] Incident Detail Page
  ├─ Current status: InformationGathering OR ReviewingFinalReport
  ├─ Coordinator realizes external dependency
  └─ Click "Put On Hold" button
  ↓
[2] On Hold Modal Opens
  ├─ Optional reason field
  ├─ Checklist (not enforced)
  └─ Enter hold reason
  ↓
[3] Click "Put On Hold"
  ↓
[4] System Actions
  ├─ Status: OnHold
  ├─ Create system note: "HOLD: [reason]"
  ├─ Dashboard: Yellow background for on-hold incidents
  └─ Incident remains assigned to coordinator
  ↓
[5] Investigation Paused
  ├─ Coordinator waits for external info
  ├─ Can still view incident and add notes
  └─ "On Hold" banner visible on detail page
  ↓
[WAIT - Days/weeks pass]
  ↓
[6] External Info Received
  ├─ Coordinator navigates back to incident
  ├─ Sees "Resume Investigation" button
  └─ Click "Resume Investigation"
  ↓
[7] Resume Modal Opens
  ├─ Dropdown: "Resume to which stage?"
  ├─ Options: InformationGathering, ReviewingFinalReport
  ├─ Checklist (not enforced)
  └─ Select stage
  ↓
[8] Click "Resume Investigation"
  ↓
[9] System Actions
  ├─ Status: [Selected stage]
  ├─ Create system note: "Resumed investigation"
  ├─ Remove on-hold indicator
  └─ Investigation continues
  ↓
END
```

### Screen Details

#### Screen 1: On Hold Modal

**Component**: StageGuidanceModal (variant: 'on-hold')
**Trigger**: Click "Put On Hold"

**Key Elements**:
- Title: "Putting Report On Hold"
- Guidance text: "This incident will remain on hold until additional information is available..."
- Optional reason textarea:
  - Placeholder: "Enter the reason for putting this incident on hold..."
  - Not required (but recommended)
- Checklist (NOT enforced):
  - [ ] Document reason for hold in notes
  - [ ] Notify relevant parties if necessary
- Actions:
  - Cancel
  - Put On Hold (orange)

**User Interactions**:
```
Coordinator clicks "Put On Hold"
Modal opens
Coordinator enters reason:
  "Awaiting police report from incident on 10/15. Expected
   availability: 10/31/2025"

Coordinator clicks "Put On Hold"
  ↓ Status: OnHold
  ↓ System note: "HOLD: Awaiting police report from incident on 10/15.
                   Expected availability: 10/31/2025"
  ↓ Detail page shows on-hold banner
```

#### Screen 2: On Hold Banner

**Location**: Incident detail page header
**Visibility**: When status = OnHold

**Key Elements**:
```tsx
<Alert color="yellow" icon={<IconClock />} mb="md">
  <Group justify="space-between">
    <div>
      <Text fw={600}>This incident is ON HOLD</Text>
      <Text size="sm">
        Reason: Awaiting police report from incident on 10/15
      </Text>
      <Text size="sm" c="dimmed">
        Expected resume: October 31, 2025
      </Text>
    </div>
    <Button variant="white" size="sm" onClick={openResumeModal}>
      Resume Investigation
    </Button>
  </Group>
</Alert>
```

#### Screen 3: Resume Investigation Modal

**Component**: StageGuidanceModal (variant: 'resume')
**Trigger**: Click "Resume Investigation"

**Key Elements**:
- Title: "Resuming Investigation"
- Guidance text: "Investigation will resume. Review notes since hold..."
- **REQUIRED** Resume to stage dropdown:
  - Options: Information Gathering, Reviewing Final Report
  - Default: Previous stage before hold
- Checklist (NOT enforced):
  - [ ] Review notes since hold
  - [ ] Verify all blockers resolved
- Optional transition note
- Actions:
  - Cancel
  - Resume Investigation (purple, disabled until stage selected)

**User Interactions**:
```
Coordinator clicks "Resume Investigation"
Modal opens
Coordinator selects: "Reviewing Final Report"
Coordinator adds note:
  "Police report received. Proceeding to final review stage."

Coordinator clicks "Resume Investigation"
  ↓ Status: ReviewingFinalReport
  ↓ System note: "Resumed investigation"
  ↓ On-hold banner removed
  ↓ Investigation continues
```

---

## Flow 6: Identified User Views Their Reports

**Persona**: Identified Reporter (Authenticated)
**Goal**: Check status of submitted reports
**Entry Point**: User navigation "My Reports"

### Flow Diagram

```
START
  ↓
[1] My Reports Page
  ├─ List of user's submitted reports
  ├─ Sort by: Submission date (newest first)
  └─ Click report card
  ↓
[2] My Report Detail View (Limited)
  ├─ View severity and status
  ├─ View submission date
  ├─ View their own description (what they submitted)
  ├─ View generic status message
  └─ NO ACCESS to: Coordinator name, notes, internal actions
  ↓
[3] Return to My Reports
  ↓
END
```

### Screen Details

#### Screen 1: My Reports Page

**URL**: `/my-reports`
**Authentication**: Required (Identified users only)

**Key Elements**:
- Page title: "My Incident Reports"
- Subtitle: "View the status of incidents you've reported"
- Report cards (one per incident):
  - Severity badge
  - Status badge
  - Submission date
  - Last updated (relative time)
  - Generic status message
  - "View Limited Details" button

**Empty State**:
```tsx
<Box p="xl" ta="center">
  <Text size="lg" c="dimmed" mb="xs">
    📋 No Reports Submitted
  </Text>
  <Text size="sm" c="dimmed" mb="md">
    You haven't submitted any incident reports yet.
  </Text>
  <Button variant="light" component={Link} to="/report-incident">
    Report an Incident
  </Button>
</Box>
```

**Report Card**:
```tsx
<Paper p="lg" mb="md" style={{ border: '1px solid #E0E0E0' }}>
  <Group justify="space-between" mb="md">
    <Group gap="md">
      <SeverityBadge severity="High" />
      <IncidentStatusBadge status="InformationGathering" />
    </Group>
    <Text size="sm" c="dimmed">2 days ago</Text>
  </Group>

  <Stack gap="xs" mb="md">
    <Group gap="lg">
      <div>
        <Text size="xs" c="dimmed">Submitted</Text>
        <Text fw={600}>October 15, 2025</Text>
      </div>
    </Group>

    <Text size="sm" c="dimmed">
      Your report is being reviewed by our safety team
    </Text>
  </Stack>

  <Button variant="light" fullWidth rightSection={<IconArrowRight />}>
    View Limited Details
  </Button>
</Paper>
```

**Status Messages by Stage**:
| Status | Message |
|--------|---------|
| ReportSubmitted | "Your report is awaiting review" |
| InformationGathering | "Your report is being reviewed by our safety team" |
| ReviewingFinalReport | "Your report is being finalized" |
| OnHold | "Your report requires additional review" |
| Closed | "This report has been resolved" |

#### Screen 2: My Report Detail View (Limited)

**URL**: `/my-reports/[id]`
**Authentication**: Required (Reporter only)

**Key Elements**:
- Header:
  - Severity badge
  - Status badge
  - Submission date
  - Last updated
- Their submission:
  - Incident date/time
  - Location
  - Their description (what they wrote)
  - People involved (if they provided)
  - Witnesses (if they provided)
- Generic status messaging
- Support resources

**What They CANNOT See**:
- ❌ Coordinator name or identity
- ❌ Internal notes (system or manual)
- ❌ Other people's information
- ❌ Google Drive links
- ❌ Audit trail
- ❌ Administrative actions

**User Interactions**:
```
User clicks "View Limited Details"
  ↓ Limited view loads
User sees their original submission
User sees current status: "Information Gathering"
User sees message: "Your report is being reviewed by our safety team"

User CANNOT:
  - See who is investigating
  - See investigation notes
  - Change anything
  - Add comments (one-way after submission)

User clicks "Back to My Reports"
  ↓ Returns to list view
```

---

## Decision Points Summary

### Critical Decision Points Across All Flows

#### 1. Anonymous vs Identified (Flow 1 & 2)
**Decision**: Toggle state on report form
**Impact**:
- Anonymous: No contact fields, no follow-up, no status checking, NO reference number shown
- Identified: Contact fields, email notifications, "My Reports" access

#### 2. Coordinator Selection (Flow 3)
**Decision**: Admin selects ANY user (not just admins)
**Impact**:
- Selected user becomes coordinator
- Status changes: ReportSubmitted → InformationGathering
- Email sent to coordinator
- Guidance modal shown

#### 3. Stage Transitions (Flow 4)
**Decision**: Coordinator chooses next stage
**Impact**:
- InformationGathering → ReviewingFinalReport (when investigation complete)
- ReviewingFinalReport → Closed (when resolution documented)
- Any stage → OnHold (when external dependency)
- OnHold → [Selected Stage] (when ready to resume)

#### 4. Hold vs Continue (Flow 5)
**Decision**: Coordinator identifies external dependency
**Impact**:
- Hold: Status OnHold, investigation paused, can resume later
- Continue: Normal progression through stages

#### 5. Close Requirements (Flow 4)
**Decision**: Coordinator ready to close
**Validation**:
- Required: Final summary (minimum 20 characters)
- Optional: Google Drive link, checklist completion
**Impact**:
- Status: Closed (cannot reopen except by admin)
- Email sent to reporter (if identified)
- Read-only for coordinator

---

## Error Handling Flows

### Error 1: Form Validation Failure

```
User submits report with missing fields
  ↓
Form validation runs
  ↓
Invalid fields highlighted (red border)
  ↓
Error messages below each field
  ↓
Focus moves to first error
  ↓
Submit button remains disabled
  ↓
User corrects errors
  ↓
Real-time validation passes
  ↓
Submit button enables
  ↓
User resubmits → Success
```

### Error 2: Network Failure During Submission

```
User clicks "Submit"
  ↓
Processing spinner shows
  ↓
Network request fails
  ↓
Error notification appears:
  "Network error. Check your connection and try again."
  [Retry] button
  ↓
User clicks "Retry"
  ↓
Form resubmits (data preserved)
  ↓
Success OR repeat error handling
```

### Error 3: Unauthorized Access Attempt

```
User navigates to /coordinator/incidents/[id]
  ↓
Authorization check:
  - Is user assigned coordinator? NO
  - Is user admin? NO
  ↓
API returns 403 Forbidden
  ↓
Error page shows:
  "You do not have access to this incident"
  [Return to Dashboard] button
  ↓
Attempt logged in audit trail
  ↓
User redirected to their dashboard
```

### Error 4: Coordinator Reassigned Mid-View

```
Coordinator viewing incident detail
  ↓
Admin reassigns to different coordinator
  ↓
Coordinator tries to add note
  ↓
API returns 403 Forbidden
  ↓
Error notification:
  "You are no longer assigned to this incident"
  ↓
Page refreshes
  ↓
Incident no longer in coordinator's list
```

---

## Mobile-Specific Flow Variations

### Mobile: Anonymous Report Submission

**Differences from Desktop**:

```
Form Elements Stack Vertically:
  ┌─────────────────────┐
  │ Anonymous Toggle    │ (Full width, larger touch target)
  ├─────────────────────┤
  │ Severity Cards      │ (Stacked, full width each)
  │ [Low]               │
  │ [Medium]            │
  │ [High]              │
  │ [Critical]          │
  ├─────────────────────┤
  │ Date Picker         │ (Native mobile date/time picker)
  ├─────────────────────┤
  │ Location            │ (Full width)
  ├─────────────────────┤
  │ Description         │ (Auto-grow textarea)
  ├─────────────────────┤
  │ Submit Button       │ (Full width, 48px height, sticky bottom)
  └─────────────────────┘
```

**Mobile Optimizations**:
- Severity cards: Full width, 56px height (thumb-friendly)
- Submit button: Sticky to bottom of viewport (always visible)
- Date picker: Native mobile interface (not web datepicker)
- Textarea: Auto-grows as user types (min 4 rows, max 12 rows)
- Validation errors: Larger font size (16px minimum)

### Mobile: Incident Dashboard

**Differences from Desktop**:

```
Card Layout Instead of Table:
  ┌─────────────────────────────┐
  │ Filters (collapsed by default) │
  │ [Expand Filters ▼]          │
  ├─────────────────────────────┤
  │ Incident Card 1             │
  │ ┌─────────────────────────┐ │
  │ │ [CRIT] [SUBMIT]         │ │
  │ │ SAF-20251017-0042       │ │
  │ │ Unassigned • 3 days ago │ │
  │ │ [View Details →]        │ │
  │ └─────────────────────────┘ │
  ├─────────────────────────────┤
  │ Incident Card 2             │
  │ ┌─────────────────────────┐ │
  │ │ [HIGH] [GATHER]         │ │
  │ │ SAF-20251015-0038       │ │
  │ │ JaneRigger • 1 day ago  │ │
  │ │ [View Details →]        │ │
  │ └─────────────────────────┘ │
  └─────────────────────────────┘
```

**Mobile Optimizations**:
- Filters: Collapsed by default, expand to full-screen drawer
- Cards: Full width, 48px minimum touch target for buttons
- Badges: Slightly larger for readability (sm → md)
- Actions: Single "View Details" button (menu hidden, accessible on detail page)

---

## Flow Completion Summary

### Flows Documented
1. ✅ Anonymous Report Submission
2. ✅ Identified Report Submission
3. ✅ Admin Assigns Incident Coordinator
4. ✅ Coordinator Investigation Workflow
5. ✅ Put Incident On Hold (Mid-Investigation)
6. ✅ Identified User Views Their Reports

### Key UX Patterns
- **Soft Enforcement**: Guidance modals NOT blocking, checklists informational
- **Privacy First**: Anonymous = truly one-way, NO reference number shown to users
- **Clear Feedback**: Success notifications, error handling, status messaging
- **Progressive Disclosure**: Details revealed only when authorized
- **Mobile-Friendly**: Touch targets, responsive layouts, native controls

### Quality Checklist
- [x] All user personas covered
- [x] Entry points defined
- [x] Decision points documented
- [x] Error handling flows included
- [x] Mobile variations specified
- [x] System actions documented
- [x] Email notifications described
- [x] Access control enforced
- [x] Validation rules clear
- [x] Success criteria defined

---

**Created**: 2025-10-17
**Author**: UI Designer Agent
**Version**: 1.0
**Status**: Draft - Awaiting Approval
**Target Quality**: 90% (Phase 2 Quality Gate)

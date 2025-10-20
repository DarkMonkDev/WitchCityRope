# AGENT HANDOFF DOCUMENT

## Phase: Business Requirements → UI Design
## Date: 2025-10-17
## Feature: Incident Reporting System

---

## 🎯 CRITICAL BUSINESS RULES (MUST IMPLEMENT)

### 1. **Per-Incident Coordinator Assignment (Any User)**
**ANY user can be assigned as incident coordinator, NOT just admins**

- ✅ Correct: Assignment dropdown shows ALL users (admin flag irrelevant)
- ❌ Wrong: Filtering assignment dropdown to only admin users

**Why Critical:** Allows expertise-based assignment (mediation skills, event context knowledge, etc.)

---

### 2. **Fully Anonymous Reporting Protection**
**Anonymous reports have ZERO follow-up capability - truly one-way submission**

- ✅ Correct: Anonymous reports have no email, no contact info, no follow-up checkbox
- ❌ Wrong: Allowing "request follow-up" option for anonymous reports

**Why Critical:** Trust in anonymous reporting depends on absolute privacy guarantee

---

### 3. **Guidance Modals Are Reminders, Not Enforcement**
**Stage transition modals provide guidance but do NOT block progression**

- ✅ Correct: User can confirm transition without checking all checklist items
- ❌ Wrong: Requiring all checklist items to be checked before allowing transition

**Why Critical:** Coordinators need flexibility for unique situations, rigid enforcement reduces adoption

---

### 4. **5-Stage Workflow (NOT 4-Stage)**
**Status enum has 5 states: Report Submitted, Information Gathering, Reviewing Final Report, On Hold, Closed**

- ✅ Correct: Using new 5-stage enum throughout UI
- ❌ Wrong: Using old 4-stage enum (New, InProgress, Resolved, Archived)

**Why Critical:** Existing backend uses old enum - migration required, UI must use NEW enum exclusively

---

### 5. **Google Drive Manual Upload in Phase 1**
**Phase 1 uses checkbox reminder for Google Drive upload, NOT automated integration**

- ✅ Correct: Checkbox in close modal reminds coordinator to manually upload
- ❌ Wrong: Implementing automated Google Drive API integration in Phase 1

**Why Critical:** Phased approach allows MVP launch without Google API complexity

---

## 📍 KEY DOCUMENTS TO READ

| Document | Path | Critical Sections |
|----------|------|-------------------|
| **Business Requirements** | `/docs/functional-areas/incident-reporting/new-work/2025-10-17-initial-implementation/requirements/business-requirements.md` | ALL (comprehensive requirements) |
| **Existing Wireframe** | `/docs/design/wireframes/incident-report-visual.html` | Complete HTML wireframe - use almost as-is |
| **Vetting Notes Pattern** | `/apps/web/src/features/admin/vetting/components/VettingApplicationDetail.tsx` | Lines 501-579 (notes section UI pattern) |
| **Vetting Grid Pattern** | `/apps/web/src/features/admin/vetting/components/VettingReviewGrid.tsx` | Entire file (filtering, list layout) |
| **Vetting Status Badge** | `/apps/web/src/features/admin/vetting/components/VettingStatusBadge.tsx` | Badge styling and color coding |
| **Modal Patterns** | `/apps/web/src/features/admin/vetting/components/OnHoldModal.tsx` | Modal structure for actions |
| **SafetyIncident Entity** | `/apps/api/Features/Safety/Entities/SafetyIncident.cs` | Current backend entity (needs status enum update) |
| **Existing Form** | `/apps/web/src/features/safety/components/IncidentReportForm.tsx` | Public submission form (already exists) |

---

## 🚨 KNOWN PITFALLS

### Pitfall 1: Assuming Coordinator Role Exists
**Description:** Treating "Incident Coordinator" as a permanent user role like "Admin"

**Why it happens:** Vetting system uses admin-only access, easy to assume same pattern

**How to avoid:**
- Coordinators are assigned PER-INCIDENT (not a role)
- Access is temporary and incident-specific
- Assignment is stored in `SafetyIncident.AssignedTo` field
- ANY user can be assigned (check ApplicationUser table, not roles)

---

### Pitfall 2: Enforcing Guidance Checklist Completion
**Description:** Blocking stage transitions until all checklist items are checked

**Why it happens:** Standard UX pattern is to validate before allowing progression

**How to avoid:**
- Guidance modals are SOFT reminders, not HARD validation
- User can confirm transition without checking any items
- Include warning: "Some items unchecked - are you sure?"
- Do NOT disable confirm button based on checklist state

---

### Pitfall 3: Using Old Status Enum Values
**Description:** Implementing UI with current backend enum (New, InProgress, Resolved, Archived)

**Why it happens:** Existing backend code uses old enum, seems like source of truth

**How to avoid:**
- Backend status enum MUST BE UPDATED before UI implementation
- Coordinate with backend developer to migrate enum first
- UI design should use NEW 5-stage enum exclusively
- Backend migration maps: New → ReportSubmitted, InProgress → InformationGathering, Resolved/Archived → Closed

---

### Pitfall 4: Exposing Internal Details to Anonymous Status Check
**Description:** Showing coordinator name, notes, or internal info on status check page

**Why it happens:** Easy to reuse incident detail page for status check

**How to avoid:**
- Status check page shows ONLY: status badge, last updated timestamp
- No coordinator name, no notes, no internal actions
- Separate component from incident detail page
- Reference number is ONLY access key

---

### Pitfall 5: Manual TypeScript DTO Creation
**Description:** Creating TypeScript interfaces manually for incident DTOs

**Why it happens:** Trying to move fast, easier to create interface than wait for backend

**How to avoid:**
- READ: `/docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md` FIRST
- ALL DTO interfaces come from NSwag auto-generation
- NEVER manually create `SafetyIncident` or `IncidentNote` interfaces
- Use `@witchcityrope/shared-types` package exclusively

---

## ✅ VALIDATION CHECKLIST

Before proceeding to implementation, verify:

- [ ] UI mockups show assignment dropdown with ALL users (not just admins)
- [ ] Anonymous reporting form has NO contact fields, NO follow-up option
- [ ] Guidance modal mockups show "Confirm" button enabled regardless of checklist state
- [ ] All UI components use NEW 5-stage status enum (not old 4-stage)
- [ ] Close incident modal has checkbox reminder for Google Drive (not automated upload)
- [ ] Notes section mirrors VettingApplicationDetail.tsx pattern (lines 501-579)
- [ ] Incident list grid mirrors VettingReviewGrid.tsx filtering/layout
- [ ] Status badges use color coding: Critical=Red, High=Orange, Medium=Yellow, Low=Green
- [ ] No TypeScript DTO interfaces manually created (wait for NSwag generation)
- [ ] Wireframe design (`incident-report-visual.html`) used as primary reference

---

## 🔄 DISCOVERED CONSTRAINTS

### Constraint 1: Existing Backend Entity Needs Update
**Existing Code:** `SafetyIncident.cs` has `IncidentStatus` enum with 4 values (New, InProgress, Resolved, Archived)

**Impact:** Backend MUST update enum to 5 values before UI implementation

**Required Changes:**
- Backend developer updates `IncidentStatus` enum to 5 values
- Database migration maps old values to new values
- API endpoints updated to use new enum values
- NSwag regenerates TypeScript types with new enum

**Coordination:** UI Designer should design for NEW enum, Implementation waits for backend migration

---

### Constraint 2: Encryption Service Exists
**Existing Code:** Backend has encryption service for sensitive vetting data

**Impact:** Same encryption service will be used for incident sensitive fields

**Required Changes:** None (use existing service)

**Note:** UI never sees plaintext for unauthorized users - API handles encryption/decryption

---

### Constraint 3: Notes Pattern Established in Vetting System
**Existing Code:** VettingApplicationDetail has proven notes UI pattern

**Impact:** Incident notes should mirror this pattern exactly

**Required Changes:**
- Use same Textarea component for note entry
- Use same Paper/Stack layout for notes list
- Use same timestamp formatting (`formatTime` function)
- Add system-generated note styling (icon indicator)

**Benefit:** Consistency across admin interfaces, reduced development time

---

### Constraint 4: Google Drive Integration Deferred
**Business Decision:** Phase 1 uses manual upload reminder, Phase 2 automates

**Impact:** Close incident modal has checkbox reminder, NOT automated upload

**Required Changes:**
- Checkbox labeled: "Google Drive upload complete (manual)"
- Link to Google Drive in new tab
- No file upload component needed
- No Google Drive API integration in Phase 1

**Future:** Phase 2 will add automation, checkbox can remain as fallback

---

## 📊 DATA MODEL DECISIONS

### Entity: SafetyIncident
```typescript
interface SafetyIncident {
  // Identifiers
  id: string;                          // Guid
  referenceNumber: string;             // SAF-YYYYMMDD-NNNN

  // Reporter (null for anonymous)
  reporterId: string | null;           // Guid or null

  // Incident Details
  severity: IncidentSeverity;          // Low, Medium, High, Critical
  incidentDate: string;                // ISO 8601 DateTime
  reportedAt: string;                  // ISO 8601 DateTime
  location: string;                    // Max 200 chars

  // Encrypted Fields (decrypted for authorized users)
  description: string;                 // Decrypted from EncryptedDescription
  involvedParties: string | null;      // Decrypted from EncryptedInvolvedParties
  witnesses: string | null;            // Decrypted from EncryptedWitnesses
  contactEmail: string | null;         // Decrypted from EncryptedContactEmail
  contactPhone: string | null;         // Decrypted from EncryptedContactPhone

  // Privacy Flags
  isAnonymous: boolean;                // True = ReporterId is null
  requestFollowUp: boolean;            // True = identified reporter wants contact

  // Workflow
  status: IncidentStatus;              // NEW 5-stage enum
  assignedTo: string | null;           // Guid of any user (coordinator)

  // Timestamps
  createdAt: string;                   // ISO 8601
  updatedAt: string;                   // ISO 8601

  // Navigation Properties (populated by backend)
  assignedUser?: {                     // Coordinator info
    id: string;
    sceneName: string;
    realName: string;
  };
  notes: IncidentNote[];               // All notes for this incident
}
```

### Entity: IncidentNote (NEW)
```typescript
interface IncidentNote {
  id: string;                          // Guid
  incidentId: string;                  // Foreign key to SafetyIncident
  authorId: string;                    // User who created note
  authorName: string;                  // Scene name of author
  content: string;                     // Note text (supports line breaks)
  isSystemGenerated: boolean;          // True for auto-notes
  createdAt: string;                   // ISO 8601
  updatedAt: string | null;            // For edits within 15 min
  editedAt: string | null;             // Timestamp of last edit
}
```

### Enum: IncidentStatus (NEW 5-STAGE)
```typescript
enum IncidentStatus {
  ReportSubmitted = 1,       // Initial state, awaiting assignment
  InformationGathering = 2,  // Assigned, coordinator investigating
  ReviewingFinalReport = 3,  // Preparing final documentation
  OnHold = 4,                // Paused, waiting for information
  Closed = 5                 // Investigation complete, archived
}
```

### Enum: IncidentSeverity (EXISTING)
```typescript
enum IncidentSeverity {
  Low = 1,
  Medium = 2,
  High = 3,
  Critical = 4
}
```

**Business Logic:**
- Anonymous: `reporterId === null` AND `isAnonymous === true`
- Identified: `reporterId !== null` AND `isAnonymous === false`
- Coordinator Access: `assignedTo === currentUserId` OR `currentUser.isAdmin === true`
- Status Progression: ReportSubmitted → InformationGathering → ReviewingFinalReport → Closed
- On Hold: Can enter from any stage, resume to any stage (except ReportSubmitted)

---

## 🎯 SUCCESS CRITERIA

### Test Case 1: Anonymous Report Submission
**Input:** User submits anonymous report with severity=High, location="Main Room", description="[50+ chars]"

**Expected Output:**
- Reference number generated (format: SAF-YYYYMMDD-NNNN)
- No email sent to user
- ReporterId = null in database
- Status = ReportSubmitted
- Admin sees incident in "Unassigned" queue

---

### Test Case 2: Coordinator Assignment
**Input:** Admin assigns incident SAF-20251017-0001 to user "JaneRigger"

**Expected Output:**
- AssignedTo = JaneRigger's userId
- Status changes from ReportSubmitted to InformationGathering
- Guidance modal appears with "Information Gathering" checklist
- JaneRigger receives email notification
- JaneRigger sees incident in their dashboard
- Other users do NOT see this incident

---

### Test Case 3: Stage Transition with Incomplete Checklist
**Input:** Coordinator moves from InformationGathering to ReviewingFinalReport with 0/4 checklist items checked

**Expected Output:**
- Warning shown: "Some items unchecked - are you sure?"
- Confirm button is ENABLED (not disabled)
- Coordinator can proceed with transition
- Status updates to ReviewingFinalReport
- Transition note added (if provided)

---

### Test Case 4: Unauthorized Access Attempt
**Input:** User attempts to access incident not assigned to them (direct URL navigation)

**Expected Output:**
- API returns 403 Forbidden
- UI shows error: "You do not have access to this incident"
- Access attempt logged in audit trail
- User redirected to dashboard

---

## ⚠️ DO NOT IMPLEMENT

❌ **DO NOT** create "Incident Coordinator" as a user role in the system
❌ **DO NOT** enforce guidance checklist completion (soft reminders only)
❌ **DO NOT** implement automated Google Drive upload in Phase 1
❌ **DO NOT** expose internal notes or coordinator identity on anonymous status check
❌ **DO NOT** allow anonymous reports to request follow-up
❌ **DO NOT** manually create TypeScript interfaces for SafetyIncident or IncidentNote
❌ **DO NOT** use old 4-stage status enum (New, InProgress, Resolved, Archived)
❌ **DO NOT** implement multi-coordinator assignment (single coordinator only)
❌ **DO NOT** allow coordinators to see incidents they're not assigned to
❌ **DO NOT** implement incident analytics/trends dashboard (Phase 2)

---

## 📝 TERMINOLOGY DICTIONARY

| Term | Definition | Example |
|------|------------|---------|
| **Anonymous Report** | Incident submission with no identity stored (ReporterId = null) | User submits via public form, receives only reference number |
| **Identified Report** | Incident submission with user identity and contact info | Logged-in user includes email, can be contacted by coordinator |
| **Incident Coordinator** | User assigned to specific incident for investigation (temporary, per-incident) | JaneRigger assigned to SAF-20251017-0001 |
| **Reference Number** | Unique incident identifier (format: SAF-YYYYMMDD-NNNN) | SAF-20251017-0042 |
| **Guidance Modal** | Stage transition reminder with checklist (soft enforcement) | Modal shown when moving to "Reviewing Final Report" |
| **On Hold** | Incident paused pending additional information | Awaiting police report, external investigation |
| **System-Generated Note** | Automated note for status changes, assignments | "Assigned to JaneRigger by Admin on 2025-10-17" |
| **Severity** | Incident urgency level (Low, Medium, High, Critical) | Critical = immediate safety risk |
| **Stage** | Workflow status (Report Submitted, Information Gathering, etc.) | Current stage: Information Gathering |

---

## 🔗 NEXT AGENT INSTRUCTIONS

### For UI Designer:

**FIRST**: Read these documents in order:
1. This handoff document (you're reading it now)
2. Business requirements: `/docs/functional-areas/incident-reporting/new-work/2025-10-17-initial-implementation/requirements/business-requirements.md`
3. Existing wireframe: `/docs/design/wireframes/incident-report-visual.html`
4. Vetting patterns: VettingApplicationDetail.tsx, VettingReviewGrid.tsx, VettingStatusBadge.tsx

**SECOND**: Review existing code patterns:
- Notes section: `/apps/web/src/features/admin/vetting/components/VettingApplicationDetail.tsx` (lines 501-579)
- Grid layout: `/apps/web/src/features/admin/vetting/components/VettingReviewGrid.tsx`
- Modal actions: `/apps/web/src/features/admin/vetting/components/OnHoldModal.tsx`

**THIRD**: Validate understanding against Critical Business Rules:
- Can ANY user be assigned as coordinator? (YES)
- Are guidance modals enforced? (NO - soft reminders only)
- What status enum values? (NEW 5-stage: ReportSubmitted, InformationGathering, ReviewingFinalReport, OnHold, Closed)
- Google Drive integration? (Manual checkbox reminder in Phase 1)

**THEN**: Begin UI design with these priorities:
1. **Admin Dashboard** (incident list, filtering, assignment)
2. **Incident Detail Page** (coordinator view with notes)
3. **Guidance Modals** (stage transition reminders)
4. **Status Badges** (color coding for severity and status)
5. **Anonymous Status Check** (reference number lookup - public page)

**Deliverables:**
- High-fidelity mockups for 5 screens (dashboard, detail, modals, status check, public form)
- Component hierarchy diagram
- Color palette with severity/status mapping
- Interaction flows (assignment, stage transitions, notes)
- Accessibility annotations (ARIA labels, keyboard nav)

---

## 🤝 HANDOFF CONFIRMATION

**Previous Agent**: Business Requirements Agent
**Previous Phase Completed**: 2025-10-17
**Key Finding**: Per-incident coordinator assignment (any user) enables expertise-based response and is critical differentiator from vetting system

**Next Agent Should Be**: UI Designer
**Next Phase**: Design (Wireframes, Mockups, Component Architecture)
**Estimated Effort**: 16 hours

---

## 📋 PHASE COMPLETION CHECKLIST

Business Requirements Phase - ✅ COMPLETE

- [x] All user roles defined (Anonymous Reporter, Identified Reporter, Coordinator, Admin)
- [x] 30+ user stories with acceptance criteria
- [x] 5 critical business rules documented
- [x] Security requirements specified (encryption, access control)
- [x] Data model defined (SafetyIncident, IncidentNote, enums)
- [x] Integration points identified (existing systems, new services)
- [x] Success metrics defined (adoption, quality, satisfaction)
- [x] Edge cases documented (8 scenarios)
- [x] Risk assessment completed (8 risks with mitigation)
- [x] Out of scope clearly defined (15 items)
- [x] Stakeholder decisions documented (5 critical approvals)
- [x] Examples provided (4 detailed user scenarios)
- [x] Handoff document created for next phase

**Quality Gate**: 95% target achieved (100% checklist completion)

**Next Phase Quality Gate**: Design 0% → 90% (UI mockups, component architecture, interaction flows)

---

**Created**: 2025-10-17
**Author**: Business Requirements Agent
**Handoff To**: UI Designer
**Version**: 1.0

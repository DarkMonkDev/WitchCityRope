# AGENT HANDOFF DOCUMENT

## Phase: UI Design → Implementation
## Date: 2025-10-17
## Feature: Incident Reporting System
## From: UI Designer Agent
## To: React Developer & Functional Spec Agents

---

## 🎯 DESIGN DELIVERABLES COMPLETED

### 1. UI Design Document ✅
**Location**: `/docs/functional-areas/incident-reporting/new-work/2025-10-17-initial-implementation/design/ui-design.md`

**Contents**:
- 6 complete screen layouts
- Color palette specifications (severity + status colors)
- Typography specifications
- Responsive breakpoints (mobile/desktop)
- Accessibility requirements (WCAG 2.1 AA)
- Mobile-specific optimizations

### 2. Component Specifications ✅
**Location**: `/docs/functional-areas/incident-reporting/new-work/2025-10-17-initial-implementation/design/component-specifications.md`

**Contents**:
- 7 complete component specifications with TypeScript interfaces
- Mantine v7 component usage patterns
- Props interfaces for all components
- Styling configurations
- Behavior specifications
- Implementation code examples

### 3. User Flows ✅
**Location**: `/docs/functional-areas/incident-reporting/new-work/2025-10-17-initial-implementation/design/user-flows.md`

**Contents**:
- 6 complete user journey flows
- Decision points and branching
- Error handling flows
- Mobile variations
- System actions at each step
- Email notification content

---

## 🚨 CRITICAL DESIGN DECISIONS (MUST IMPLEMENT)

### 1. NO Reference Number Display to Submitters
**Stakeholder Decision**: Reference numbers are internal admin tracking ONLY

- ✅ Correct: Anonymous confirmation shows NO reference number
- ✅ Correct: Identified confirmation shows "Check My Reports for updates"
- ❌ Wrong: Showing "Your reference number is SAF-..." to ANY submitter

**Rationale**: Reference numbers are for internal coordination between admins/coordinators, not for public tracking.

---

### 2. Severity System - 4 Levels (NOT Type Categories)
**Update to Existing Wireframe**: Replace "Type of Incident" with "Severity Level"

**Color-Coded Severity Badges**:
- **Critical** (#AA0130 red): Immediate safety risk, consent violation
- **High** (#FF8C00 orange): Boundary violation, harassment
- **Medium** (#FFBF00 amber): Safety concern requiring follow-up
- **Low** (#4A5C3A green): Minor concern, documentation only

**Wireframe Changes**:
```html
<!-- OLD (existing wireframe) -->
<div class="severity-options">
  <div>Safety Concern</div>
  <div>Boundary Violation</div>
  <div>Harassment</div>
  <div>Other Concern</div>
</div>

<!-- NEW (required for implementation) -->
<div class="severity-options">
  <div class="severity-option severity-critical">
    <div class="severity-indicator"></div>
    <h4>Critical</h4>
    <p>Immediate safety risk, consent violation</p>
  </div>
  <!-- Repeat for High, Medium, Low -->
</div>
```

---

### 3. Status Badge Colors - 5-Stage Workflow
**NEW 5-Stage Enum** (backend migration required):

```typescript
enum IncidentStatus {
  ReportSubmitted = 1,      // #614B79 (plum)
  InformationGathering = 2, // #7B2CBF (electric purple)
  ReviewingFinalReport = 3, // #E6AC00 (dark amber)
  OnHold = 4,               // #FFBF00 (bright amber)
  Closed = 5                // #4A5C3A (forest green)
}
```

**Backend Coordination Required**:
- Current backend uses OLD 4-stage enum (New, InProgress, Resolved, Archived)
- Backend MUST migrate to NEW 5-stage enum BEFORE React implementation
- NSwag regeneration required after backend enum update

---

### 4. Guidance Modals - Soft Enforcement (NOT Blocking)
**Critical UX Decision**: Modals provide guidance but DO NOT enforce checklist completion

```tsx
// ✅ CORRECT - User can proceed without checking items
<Button
  onClick={handleConfirm}
  disabled={false}  // NOT disabled based on checklist
>
  Confirm Transition
</Button>

// ❌ WRONG - Do NOT disable based on checklist state
<Button
  onClick={handleConfirm}
  disabled={!allChecklistItemsChecked}
>
  Confirm Transition
</Button>
```

**Alert in Modal**:
```tsx
<Alert color="blue" icon={<IconInfoCircle />}>
  <Text size="xs">
    These recommendations are guidance only. You may proceed without
    completing all items.
  </Text>
</Alert>
```

---

### 5. Google Drive Integration - Manual Reminder (Phase 1)
**MVP Approach**: Checkbox reminder + manual link input (NO automated upload)

```tsx
// Phase 1 Implementation
<Checkbox
  label="Investigation documents uploaded to Google Drive"
  checked={googleDriveUploaded}
  onChange={setGoogleDriveUploaded}
/>
<Button component="a" href="[drive-link]" target="_blank">
  Open Google Drive
</Button>

// Phase 2 (Future) - Automated Integration
// NOT implemented in initial release
```

---

## 📋 COMPONENT MAPPING TO MANTINE v7

### Badge Components

| Component | Mantine Base | Custom Styling | Variants |
|-----------|-------------|----------------|----------|
| SeverityBadge | Mantine Badge | Custom colors, sizes | Critical, High, Medium, Low |
| IncidentStatusBadge | Mantine Badge | Custom colors, sizes | 5 status stages |

**Implementation Priority**: HIGH - Required for all screens

**Pattern Source**: VettingStatusBadge.tsx

---

### Dashboard Components

| Component | Mantine Base | Pattern Source | Complexity |
|-----------|-------------|----------------|------------|
| IncidentFilters | TextInput, Select | VettingReviewGrid filters | Medium |
| IncidentTable | Table, Menu, ActionIcon | VettingReviewGrid table | High |
| UnassignedQueueAlert | Alert | Custom | Low |
| IncidentPagination | Pagination | VettingReviewGrid | Low |

**Implementation Priority**: HIGH - Admin workflow depends on these

**Key Features**:
- Real-time search with debouncing
- Multi-filter support (status, severity, assigned)
- Active filter badges with remove capability
- Aging indicators (red >7 days, yellow >3 days)
- Actions menu per row

---

### Detail Page Components

| Component | Mantine Base | Pattern Source | Complexity |
|-----------|-------------|----------------|------------|
| IncidentDetailHeader | Card, Group, Badge | Custom | Medium |
| IncidentDetailsCard | Card, Stack, Text | Custom | Low |
| PeopleInvolvedCard | Card, Stack, Text | Custom | Low |
| **IncidentNotesList** | Card, Textarea, Paper | **VettingApplicationDetail 501-579** | **High** |
| GoogleDriveSection | Card, Checkbox, Button | Custom | Low |

**Implementation Priority**: CRITICAL for IncidentNotesList - mirrors vetting pattern exactly

**Notes Section Requirements**:
- System notes: Purple background, "SYSTEM" badge, 4px left border
- Manual notes: Gray background, note icon, editable within 15 minutes
- Add note textarea (4 rows minimum)
- Save button (disabled if empty)
- Chronological order (newest first)

---

### Modal Components

| Component | Mantine Base | Pattern Source | Complexity |
|-----------|-------------|----------------|------------|
| StageGuidanceModal | Modal, Textarea, Checkbox | OnHoldModal | High |
| CoordinatorAssignmentModal | Modal, Select, Paper | OnHoldModal + custom | Medium |
| OnHoldModal | Modal, Textarea | VettingApplicationDetail OnHoldModal | Low |

**Implementation Priority**: MEDIUM - Required for stage transitions

**Modal Variants** (5 total):
1. Information Gathering (assignment modal)
2. Reviewing Final Report
3. On Hold
4. Resume Investigation
5. Close Incident

**Common Pattern**:
```tsx
<Modal opened={opened} onClose={onClose} title={<Title />} centered>
  <Stack gap="md">
    <Text size="sm">{guidanceText}</Text>
    <Stack gap="xs">{checklist}</Stack>
    {optionalFields}
    <Group justify="flex-end">{actions}</Group>
  </Stack>
</Modal>
```

---

## 🎨 DESIGN SYSTEM INTEGRATION

### Color Palette (From Design System v7)

**Primary Colors**:
```css
--color-burgundy: #880124;       /* Headers, primary brand */
--color-rose-gold: #B76D75;      /* Accents, borders */
--color-midnight: #1A1A2E;       /* Dark sections */
```

**Severity Colors** (NEW - Specific to Incidents):
```css
--severity-critical: #AA0130;    /* Red */
--severity-high: #FF8C00;        /* Orange */
--severity-medium: #FFBF00;      /* Amber */
--severity-low: #4A5C3A;         /* Green */
```

**Status Colors** (NEW - Specific to Incidents):
```css
--status-submitted: #614B79;     /* Plum */
--status-gathering: #7B2CBF;     /* Electric purple */
--status-reviewing: #E6AC00;     /* Dark amber */
--status-on-hold: #FFBF00;       /* Bright amber */
--status-closed: #4A5C3A;        /* Forest green */
```

**Neutral Colors**:
```css
--color-charcoal: #2B2B2B;       /* Primary text */
--color-cream: #FAF6F2;          /* Page background */
--color-ivory: #FFF8F0;          /* Card backgrounds */
```

### Typography

```css
--font-heading: 'Montserrat', sans-serif;  /* All headings, labels */
--font-body: 'Source Sans 3', sans-serif;  /* Body text */
```

**Sizes**:
- Page titles: 32px / 24px mobile
- Section titles: 24px / 20px mobile
- Card headers: 18px
- Body text: 16px
- Small text: 14px

### Spacing

```css
--space-xs: 8px;
--space-sm: 16px;
--space-md: 24px;
--space-lg: 32px;
--space-xl: 40px;
```

**Usage**:
- Component internal: sm (16px)
- Related elements: md (24px)
- Component spacing: lg (32px)
- Section spacing: xl (40px)

---

## 📱 RESPONSIVE DESIGN SPECIFICATIONS

### Breakpoints (Mantine)

```typescript
const breakpoints = {
  xs: em(0),      // 0px - Mobile portrait
  sm: em(576),    // 576px - Mobile landscape
  md: em(768),    // 768px - Tablet
  lg: em(992),    // 992px - Desktop
  xl: em(1200),   // 1200px - Large desktop
};
```

### Mobile Modifications (<768px)

**Dashboard**:
- Filters: Stack vertically
- Table → Card layout:
  ```tsx
  <Paper p="md" mb="sm">
    <Group justify="space-between">
      <Badge>SAF-...</Badge>
      <SeverityBadge />
    </Group>
    <Group justify="space-between">
      <StatusBadge />
      <Text size="sm" c="dimmed">3 days ago</Text>
    </Group>
    <Text size="sm" fw={600}>Assigned: Unassigned</Text>
    <Button fullWidth mt="sm">View Details</Button>
  </Paper>
  ```

**Detail Page**:
- Header actions: Stack vertically
- Two-column layouts → Single column
- Buttons: Full width with spacing

**Forms**:
- Severity cards: Full width, stack vertically
- Submit button: Sticky to bottom, 48px height
- Textareas: Auto-grow (min 4 rows, max 12 rows)

**Touch Targets**:
- Primary actions: 48×48px minimum
- Secondary actions: 44×44px minimum
- Toolbar icons: 44×44px minimum

---

## ♿ ACCESSIBILITY REQUIREMENTS

### WCAG 2.1 AA Compliance

**Color Contrast**:
- All text: ≥4.5:1 contrast ratio
- Large text (18px+): ≥3:1 contrast ratio
- Severity badges: Pre-validated (Critical/High/Low pass, Medium needs dark text)

**Keyboard Navigation**:
```tsx
// All interactive elements
<Button aria-label="Close incident report">
  <IconX size={16} />
</Button>

// Form fields
<TextInput
  aria-describedby="description-help"
  aria-invalid={hasError ? 'true' : 'false'}
/>

// Live regions
<div role="status" aria-live="polite">
  {statusMessage}
</div>
```

**Focus Management**:
```tsx
// Return focus to trigger after modal close
const handleModalClose = () => {
  setModalOpened(false);
  triggerButtonRef.current?.focus();
};

// Focus first invalid field on submit
const handleSubmit = form.onSubmit((values) => {
  if (!form.isValid()) {
    const firstError = Object.keys(form.errors)[0];
    document.getElementById(firstError)?.focus();
  }
});
```

**Screen Reader Support**:
- Severity badges: `aria-label="Critical severity - immediate attention required"`
- Status badges: `aria-label="Information Gathering - active investigation in progress"`
- Notes: System notes have "System generated note" label
- Forms: Error messages linked via `aria-describedby`

---

## 🔗 EXISTING WIREFRAME INTEGRATION

### Public Incident Report Form

**Source**: `/docs/design/wireframes/incident-report-visual.html`

**Use As-Is**:
- Anonymous toggle placement and styling
- Identity fields conditional display
- Incident details section layout
- People involved section
- Support resources footer

**Updates Required**:
1. Replace "Type of Incident" with "Severity Level" (4 cards)
2. Remove reference number from confirmation screen
3. Update confirmation messaging:
   - Anonymous: "Your report has been submitted" (NO "save this number")
   - Identified: "Check My Reports for updates" (NO reference number)

**CSS Classes to Maintain**:
```css
.severity-option         /* Card styling */
.anonymous-toggle        /* Toggle container */
.form-section           /* Section containers */
.support-info           /* Resources footer */
.confirmation-message    /* Success screen */
```

---

## 🧪 TESTING CONSIDERATIONS

### Critical UI Test Scenarios

1. **Anonymous vs Identified Toggle**
   - Verify contact fields hide/show
   - Verify confirmation messaging differs
   - Verify NO reference number shown to either

2. **Severity Badge Color Coding**
   - Verify correct colors for all 4 levels
   - Verify contrast ratios pass WCAG
   - Verify responsive sizing

3. **Status Badge 5-Stage Workflow**
   - Verify all 5 statuses render correctly
   - Verify color differentiation
   - Verify short labels on xs/sm, full labels on md+

4. **Guidance Modal - Soft Enforcement**
   - Verify checklist items NOT required
   - Verify confirm button enabled regardless of checklist
   - Verify user can proceed without checking items

5. **Coordinator Assignment - Any User**
   - Verify dropdown shows ALL users (not filtered by role)
   - Verify non-admin users can be selected
   - Verify workload indicator displays

6. **Notes Section Pattern**
   - Verify system notes have purple background + badge
   - Verify manual notes have gray background + icon
   - Verify save button disabled when textarea empty
   - Verify chronological order (newest first)

7. **Responsive Behavior**
   - Verify mobile card layout (<768px)
   - Verify desktop table layout (≥768px)
   - Verify touch targets ≥44px on mobile
   - Verify sticky submit button on mobile forms

8. **Accessibility**
   - Verify keyboard navigation (Tab order)
   - Verify screen reader labels (ARIA)
   - Verify focus indicators visible
   - Verify error announcements

---

## 📊 IMPLEMENTATION PRIORITY

### Phase 2A: Core Components (Week 1)
**Priority**: CRITICAL

1. SeverityBadge component
2. IncidentStatusBadge component
3. Public Incident Report Form (updated wireframe)
4. Anonymous/Identified confirmation screens

**Rationale**: Enables end-to-end public reporting flow

---

### Phase 2B: Admin Dashboard (Week 2)
**Priority**: HIGH

1. IncidentFilters component
2. IncidentTable component (mirror VettingReviewGrid)
3. UnassignedQueueAlert component
4. IncidentPagination component

**Rationale**: Enables admin assignment and oversight

---

### Phase 2C: Detail Page & Notes (Week 3)
**Priority**: HIGH

1. IncidentDetailHeader component
2. IncidentDetailsCard component
3. PeopleInvolvedCard component
4. **IncidentNotesList component** (CRITICAL - mirror vetting)
5. GoogleDriveSection component

**Rationale**: Enables coordinator investigation workflow

---

### Phase 2D: Modals & Workflows (Week 4)
**Priority**: MEDIUM

1. CoordinatorAssignmentModal
2. StageGuidanceModal (5 variants)
3. OnHoldModal (adapted from vetting)
4. CloseIncidentModal

**Rationale**: Enables complete workflow transitions

---

### Phase 2E: My Reports (Week 5)
**Priority**: LOW

1. MyReportsPage component
2. MyReportCard component
3. MyReportDetailView (limited) component

**Rationale**: Nice-to-have for identified reporters, not blocking critical workflows

---

## ⚠️ KNOWN CHALLENGES & RECOMMENDATIONS

### Challenge 1: Backend Enum Migration
**Issue**: Current backend uses 4-stage enum, UI requires 5-stage enum

**Recommendation**:
- Backend developer MUST migrate enum FIRST
- Database migration script required
- NSwag regeneration after enum update
- UI implementation WAITS for backend completion

**Coordination**: Backend Developer handoff document should address this

---

### Challenge 2: Notes Section Complexity
**Issue**: Notes section mirrors vetting pattern with system/manual differentiation

**Recommendation**:
- Study VettingApplicationDetail.tsx lines 501-579 carefully
- Reuse existing utility functions (formatTime, isSystemGeneratedNote)
- Test system note creation in backend first
- Implement add note functionality incrementally

**Coordination**: Functional Spec should define system note trigger events

---

### Challenge 3: Coordinator Assignment Dropdown
**Issue**: Dropdown must show ALL users (thousands potentially), not filtered by role

**Recommendation**:
- Use Mantine Select with `searchable` prop
- Backend pagination for user search (limit: 50 results)
- Display format: "SceneName (RealName) - X active incidents"
- Cache user list on frontend (refresh every 5 minutes)

**Coordination**: Backend API endpoint needs search/filter capability

---

### Challenge 4: Google Drive Manual Process
**Issue**: Phase 1 relies on manual checkbox + link paste (no validation)

**Recommendation**:
- Clear messaging: "Phase 1: Manual upload reminder"
- Checkbox is honor system (not enforced)
- Link field: No validation (accept any URL)
- Admin can verify manually
- Phase 2 automation clearly documented as future enhancement

**Coordination**: Do NOT attempt automated integration in Phase 1

---

## 📝 NEXT AGENT INSTRUCTIONS

### For React Developer:

**FIRST**: Read these documents in order:
1. This handoff document (you're reading it now)
2. UI Design Document: `/docs/.../design/ui-design.md`
3. Component Specifications: `/docs/.../design/component-specifications.md`
4. User Flows: `/docs/.../design/user-flows.md`
5. Vetting System Reference Components:
   - VettingStatusBadge.tsx
   - VettingReviewGrid.tsx
   - VettingApplicationDetail.tsx (lines 501-579)
   - OnHoldModal.tsx

**SECOND**: Verify design understanding:
- [ ] Can ANY user be assigned as coordinator? (YES)
- [ ] Are guidance modals enforced? (NO - soft reminders only)
- [ ] What status enum values? (NEW 5-stage: ReportSubmitted, InformationGathering, ReviewingFinalReport, OnHold, Closed)
- [ ] Google Drive integration? (Manual checkbox reminder in Phase 1)
- [ ] Reference number shown to submitters? (NO - internal admin tracking only)

**THIRD**: Implementation sequence:
1. Create SeverityBadge and IncidentStatusBadge first
2. Update public incident report form (minor changes to existing wireframe)
3. Build IncidentTable mirroring VettingReviewGrid
4. Implement IncidentNotesList using VettingApplicationDetail pattern
5. Create modals using OnHoldModal pattern
6. Test responsive behavior at all breakpoints

**Deliverables**:
- Functional React components with TypeScript
- Mantine v7 integration
- Responsive behavior (mobile + desktop)
- Accessibility compliance (ARIA, keyboard nav)
- Unit tests for badge components
- Integration tests for form submission

---

### For Functional Spec Agent:

**FIRST**: Read these documents in order:
1. This handoff document
2. Business Requirements: `/docs/.../requirements/business-requirements.md`
3. UI Design Document: `/docs/.../design/ui-design.md`
4. User Flows: `/docs/.../design/user-flows.md`

**SECOND**: Map UI components to backend:
- [ ] Define SafetyIncident DTO (matches UI component props)
- [ ] Define IncidentNote DTO (system vs manual notes)
- [ ] Define API endpoints for each UI interaction
- [ ] Specify state management (React Query patterns)
- [ ] Document system note trigger events

**THIRD**: Critical API Requirements:
1. **Status Enum Migration**:
   - Backend MUST update IncidentStatus enum to 5 stages
   - Database migration script required
   - NSwag regeneration after enum update

2. **Coordinator Assignment**:
   - GET /api/users/for-assignment (ALL users, not filtered by role)
   - Response includes: sceneName, realName, role, activeIncidentCount

3. **Notes System**:
   - POST /api/incidents/{id}/notes (manual notes)
   - System notes auto-created on status changes
   - Notes include: isSystemGenerated flag

4. **Encryption/Decryption**:
   - Sensitive fields encrypted at rest
   - Decrypted for authorized users (coordinator + admin)
   - API responses never include plaintext for unauthorized users

**Deliverables**:
- Technical design document
- API endpoint specifications
- DTO definitions (matching UI props)
- State management architecture
- Integration points with existing systems

---

## 🤝 HANDOFF CONFIRMATION

**Previous Phase**: Business Requirements (Complete)
**Previous Agent**: Business Requirements Agent
**Previous Phase Completed**: 2025-10-17

**Current Phase**: UI Design (Complete)
**Current Agent**: UI Designer Agent
**Current Phase Completed**: 2025-10-17

**Next Phase**: Implementation Planning
**Next Agents**: React Developer + Functional Spec Agent (parallel work)
**Estimated Effort**:
- React Developer: 40 hours (2 weeks)
- Functional Spec: 16 hours (3-4 days)

---

## 📋 PHASE COMPLETION CHECKLIST

UI Design Phase - ✅ COMPLETE (90% Quality Target Achieved)

- [x] All screens designed (6 total)
- [x] All components specified (7 total)
- [x] User flows documented (6 complete flows)
- [x] Color palette defined (severity + status colors)
- [x] Typography specifications complete
- [x] Responsive behavior defined (mobile + desktop)
- [x] Accessibility requirements documented (WCAG 2.1 AA)
- [x] Mantine v7 component mapping complete
- [x] Vetting system patterns referenced
- [x] Wireframe integration instructions clear
- [x] Critical business rules addressed in design
- [x] Stakeholder decisions documented (NO reference number, etc.)
- [x] Implementation priority defined
- [x] Testing considerations documented
- [x] Known challenges identified
- [x] Handoff document created

**Quality Gate**: 90% target achieved (100% checklist completion)

**Next Phase Quality Gate**: Implementation 0% → 90% (functional components, API integration, testing)

---

**Created**: 2025-10-17
**Author**: UI Designer Agent
**Handoff To**: React Developer + Functional Spec Agent
**Version**: 1.0
**Status**: READY FOR HUMAN UI APPROVAL

---

## 🚨 MANDATORY HUMAN UI APPROVAL REQUIRED

**CRITICAL**: This handoff document and all UI design deliverables MUST receive human approval before Phase 2 implementation work begins.

**Required Approvals**:
- [ ] Product Manager: UI design approach
- [ ] UX Designer (if available): Accessibility and usability
- [ ] Tech Lead: Mantine v7 integration and implementation feasibility
- [ ] Security Lead: Privacy and access control design

**Once Approved**:
- React Developer can begin component implementation
- Functional Spec Agent can begin technical design
- Backend Developer can begin enum migration

**If Changes Required**:
- UI Designer Agent will revise designs
- New handoff document version created
- Re-submit for approval

---

**Waiting for Human UI Approval...**

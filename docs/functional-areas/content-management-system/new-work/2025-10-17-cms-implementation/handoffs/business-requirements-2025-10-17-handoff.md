# Business Requirements → Design Phase Handoff
<!-- Date: 2025-10-17 -->
<!-- From: Business Requirements Agent -->
<!-- To: UI Designer, Functional Spec Agent -->
<!-- Status: Complete - Ready for Human Review -->

## Handoff Summary

**Feature**: Content Management System (CMS) for static text pages
**Phase Complete**: Requirements Analysis (Phase 1)
**Next Phase**: Design & Functional Specifications (Phase 2)
**Quality Score**: 100% (10/10 checklist items complete)

**Key Decision**: Route-based CMS with backend HTML sanitization, TipTap in-place editing, revision history tracking

---

## Critical Business Rules (TOP 5 - MUST IMPLEMENT)

### 1. Admin-Only Edit Access
**Rule**: ONLY users with "Administrator" role can edit content
**Enforcement**:
- Frontend: Hide edit button from non-admins (UX)
- Backend: All write endpoints require "Administrator" role (security)
- Database: User attribution via foreign key to AspNetUsers

**Test Criteria**:
- Non-admin users cannot see edit button
- Direct API calls from non-admins return 401/403
- All edits attributed to valid Administrator user

---

### 2. Backend HTML Sanitization (XSS Prevention)
**Rule**: ALL user-submitted HTML MUST be sanitized on backend before database storage
**Whitelist Approach**:
- Allowed tags: `<p>`, `<br>`, `<strong>`, `<em>`, `<u>`, `<s>`, `<strike>`, `<h1>`-`<h6>`, `<ul>`, `<ol>`, `<li>`, `<blockquote>`, `<a>`, `<code>`, `<pre>`
- Allowed attributes: `href`, `title`, `class`
- Allowed CSS properties: `text-align`
- **Remove**: `<script>`, `<iframe>`, `onclick`, `onerror`, etc.

**Implementation**:
- Library: HtmlSanitizer.NET (OWASP-recommended)
- Location: Backend only (no frontend sanitization)
- Timing: Before database write

**Test Criteria**:
- Malicious `<script>` tags removed
- JavaScript event handlers stripped
- Safe HTML preserved
- No XSS vulnerabilities in security audit

---

### 3. Revision History with User Attribution
**Rule**: Create complete content snapshot BEFORE every save operation
**Data Captured**:
- Full HTML content (not diff, complete snapshot)
- Page title
- UTC timestamp
- User ID who made change
- Optional change description

**Lifecycle**:
- Create revision → Update current content (in transaction)
- Revisions never deleted (permanent audit trail)
- Cascade delete if page deleted
- Preserve if user deleted (audit integrity)

**Test Criteria**:
- Every save creates revision
- Revision shows correct user + timestamp
- History displayed newest-first
- Up to 50 most recent shown in UI

---

### 4. Optimistic UI Updates
**Rule**: UI must update immediately (<16ms perceived) while save happens in background
**Pattern**: TanStack Query optimistic update strategy
**Workflow**:
1. User clicks "Save"
2. Cache updated immediately (content appears saved)
3. API request sent in background
4. On success: Invalidate cache, show success toast
5. On error: Rollback cache, show error toast, enable retry

**Test Criteria**:
- UI updates within 16ms (instant feedback)
- Network failure triggers rollback
- Error notification visible
- Retry works after failure

---

### 5. Unsaved Changes Protection
**Rule**: Prevent data loss from accidental navigation with dirty state
**Implementation**:
- Track `isDirty` state (content changed but not saved)
- Browser `beforeunload` event listener
- Confirm dialog: "You have unsaved changes. Leave page?"
- Cancel button confirms: "Discard unsaved changes?"

**Test Criteria**:
- Navigation blocked if dirty
- Browser warning appears
- Cancel button requires confirmation
- Successful save clears dirty flag

---

## Complete User Stories

### Story 1: Admin Edits Static Page Content
- **Actor**: Administrator
- **Goal**: Edit page content in-place with rich text editor
- **Acceptance Criteria**: See business-requirements.md lines 81-118
- **Key UX**: "Edit Page" button → TipTap editor → "Save"/"Cancel" buttons

### Story 2: Admin Saves Content Changes
- **Actor**: Administrator
- **Goal**: Save changes and see them live immediately
- **Acceptance Criteria**: See business-requirements.md lines 122-150
- **Key UX**: Optimistic update (instant) + success toast

### Story 3: Admin Cancels Editing
- **Actor**: Administrator
- **Goal**: Discard changes and revert to original
- **Acceptance Criteria**: See business-requirements.md lines 154-178
- **Key UX**: Confirm dialog + rollback to original content

### Story 4: Admin Views Revision History
- **Actor**: Administrator
- **Goal**: See who changed content and when
- **Acceptance Criteria**: See business-requirements.md lines 182-214
- **Key UX**: "View History" → Modal with revision list (newest-first)

### Story 5: System Prevents XSS Attacks
- **Actor**: Platform Owner
- **Goal**: Block malicious HTML injection
- **Acceptance Criteria**: See business-requirements.md lines 218-251
- **Key UX**: Transparent (backend sanitization, no user interaction)

---

## Stakeholder Decisions Documented

### Decision 1: Route-Based Page Identification
**Decision**: Use URL slug for page identification, not database IDs
**Rationale**: Simpler for 3 pages, direct route → slug mapping
**Example**: `/resources` → fetch slug "resources" from database
**Impact on Design**: React routes defined in App.tsx, not dynamic

### Decision 2: Text-Only Content (No Images)
**Decision**: MVP does not include image upload capability
**Rationale**: Keep implementation simple, focus on text content
**Workaround**: Admins can link to externally-hosted images if needed
**Future**: Phase 2 can add image upload + media library

### Decision 3: Live Editing (No Draft State)
**Decision**: All saves publish immediately, no draft/published workflow
**Rationale**: Admins are trusted to publish immediately
**Impact on Design**: No draft UI, single "Save" button

### Decision 4: View-Only Revision History (MVP)
**Decision**: Revision history displays in MVP, rollback is Phase 2
**Rationale**: Audit trail is priority, rollback can be added later
**Impact on Design**: History list shows revisions but no "Restore" button

### Decision 5: Backend-Only Sanitization
**Decision**: HtmlSanitizer.NET on backend, no frontend DOMPurify
**Rationale**: Admin-only feature, backend is sufficient defense
**Impact on Design**: No frontend sanitization logic needed

---

## Technical Constraints for Design

### Technology Stack (Non-Negotiable)
**Frontend**:
- React 18 + TypeScript
- Mantine v7 components (Button, TextInput, Group, Stack, Box)
- TanStack Query v5 (optimistic updates, caching)
- Existing `MantineTiptapEditor.tsx` component (DO NOT create new editor)

**Backend**:
- .NET 9 Minimal API
- Vertical slice architecture (`/apps/api/Features/Cms/CmsEndpoints.cs`)
- Entity Framework Core + PostgreSQL
- HtmlSanitizer.NET NuGet package

**Type Generation**:
- NSwag auto-generation (C# DTOs → TypeScript)
- **CRITICAL**: DO NOT create manual TypeScript interfaces
- All types in `@witchcityrope/shared-types` package

### Performance Requirements
- Page load: <200ms (target), <500ms (max acceptable)
- Save response: <500ms server response
- UI update: <16ms perceived (optimistic update)
- Mobile 3G: Must work on slower connections

### Mobile-First Constraints
- Touch targets: Minimum 44×44 pixels
- TipTap editor: Must be touch-friendly
- Buttons: Large enough for thumb tapping
- Layout: Responsive on phones/tablets

---

## Design Phase Deliverables Required

### UI Wireframes Needed
1. **View Mode**: CMS page with "Edit Page" button (desktop + mobile)
2. **Edit Mode**: TipTap editor with title input, Save/Cancel buttons (desktop + mobile)
3. **Revision History Modal**: List of revisions with timestamps, usernames (desktop + mobile)
4. **Loading States**: Skeleton loader, saving spinner
5. **Error States**: Toast notifications, unsaved changes dialog

### Functional Specifications Needed
1. **API Endpoint Specifications**:
   - `GET /api/cms/pages/{slug}` - Fetch page by slug
   - `PUT /api/cms/pages/{id}` - Update page content
   - `GET /api/cms/pages/{id}/revisions` - Fetch revision history
2. **Database Schema**:
   - `cms.ContentPages` table
   - `cms.ContentRevisions` table
   - Indexes, foreign keys, constraints
3. **Component Specifications**:
   - `CmsPage.tsx` - Main page component
   - `useCmsPage.ts` - TanStack Query hook
   - `cmsApi.ts` - API client functions
4. **State Management**:
   - Edit mode toggle (`isEditing`)
   - Dirty state tracking (`isDirty`)
   - Optimistic update logic

### Component Hierarchy
```
CmsPage (container)
├── Edit Button (admin-only, conditional render)
├── View Mode (conditional)
│   └── dangerouslySetInnerHTML (sanitized content)
└── Edit Mode (conditional)
    ├── TextInput (page title)
    ├── MantineTiptapEditor (content)
    └── Button Group
        ├── Save Button (loading state)
        └── Cancel Button
```

---

## Success Criteria for Design Phase

### Wireframes Must Show
- ✅ Clear edit button placement (desktop + mobile)
- ✅ TipTap editor integrated seamlessly
- ✅ Save/Cancel button positions
- ✅ Loading states (skeleton, spinner)
- ✅ Error states (toast notifications)
- ✅ Revision history modal layout
- ✅ Mobile responsive behavior

### Functional Spec Must Define
- ✅ All 4 API endpoints with request/response schemas
- ✅ Database schema with indexes and constraints
- ✅ Component props and state management
- ✅ Error handling for all failure scenarios
- ✅ Optimistic update implementation details
- ✅ HTML sanitization configuration

### Design Review Criteria
- Aligns with existing WitchCityRope design language
- Mobile-friendly (touch targets, responsive)
- Accessibility (ARIA labels, keyboard nav)
- Performance (minimal re-renders, efficient caching)

---

## Open Questions for Design Phase

### Critical Questions (Must Answer)
- [ ] **Edit Button Placement**: Top-right, bottom-right, or floating action button?
- [ ] **Revision History UI**: Modal overlay, drawer, or separate page?
- [ ] **Cancel Confirmation**: Browser confirm dialog or custom Mantine modal?
- [ ] **Loading State**: Skeleton loader, spinner, or progress bar?
- [ ] **Mobile Editor Height**: Fixed rows or dynamic based on content?

### Nice-to-Know Questions
- [ ] **Color Scheme**: Use existing Mantine theme or custom CMS colors?
- [ ] **Typography**: Existing heading styles or CMS-specific?
- [ ] **Animation**: Fade transitions between view/edit modes?

---

## Dependencies and Blockers

### Ready to Proceed
- ✅ TipTap editor fully integrated (MantineTiptapEditor.tsx exists)
- ✅ Admin authentication operational (BFF pattern with httpOnly cookies)
- ✅ Minimal API architecture established (vertical slice pattern)
- ✅ PostgreSQL database available
- ✅ NSwag type generation pipeline operational

### No Blockers Identified
All prerequisites are in place for design phase to begin immediately.

---

## Risk Flagging for Design

### Medium Risk: Mobile Editor Usability
- **Issue**: TipTap formatting controls may be cramped on small screens
- **Recommendation**: Test editor on real mobile devices during design phase
- **Mitigation**: Consider simplified mobile toolbar or collapsible formatting panel

### Low Risk: Revision History Performance
- **Issue**: Loading 50+ revisions could be slow
- **Recommendation**: Paginate revision list if >50 entries
- **Mitigation**: Database index on `ContentPageId` + `CreatedAt DESC`

---

## Examples/Scenarios for Design Reference

### Example 1: Edit Workflow (Happy Path)
See business-requirements.md "Scenario 1" (lines 669-698) for complete step-by-step

**Key UX Moments**:
- Button appears → Click → Editor loads (<100ms)
- Content pre-populated in editor
- Save → Optimistic update → Success toast

---

### Example 2: Cancel Workflow
See business-requirements.md "Scenario 2" (lines 702-724) for complete step-by-step

**Key UX Moments**:
- Cancel → Confirm dialog → Restore original content
- No database write, instant rollback

---

### Example 3: Network Failure
See business-requirements.md "Scenario 3" (lines 728-755) for complete step-by-step

**Key UX Moments**:
- Save → Optimistic update → Error → Rollback → Retry
- User never sees broken state

---

## File Locations for Reference

### Business Requirements Document
`/docs/functional-areas/content-management-system/new-work/2025-10-17-cms-implementation/requirements/business-requirements.md`

### Technology Research Document
`/docs/functional-areas/content-management-system/new-work/2025-10-17-cms-implementation/research/cms-architecture-research.md`

### Existing TipTap Editor Component
`/apps/web/src/components/forms/MantineTiptapEditor.tsx`

### Future Implementation Location
- Backend: `/apps/api/Features/Cms/CmsEndpoints.cs`
- Frontend: `/apps/web/src/pages/cms/CmsPage.tsx`
- Hooks: `/apps/web/src/hooks/useCmsPage.ts`
- API Client: `/apps/web/src/api/cms.api.ts`

---

## Next Steps

### Immediate Actions
1. **Human Review**: Product owner reviews business requirements document
2. **Stakeholder Approval**: Confirm 3-page scope, admin-only access, text-only content
3. **Design Phase Start**: UI designer creates wireframes (desktop + mobile)
4. **Functional Spec**: Functional spec agent defines API contracts and database schema

### Design Phase Checklist
- [ ] Create wireframes for all 5 UI states (view, edit, history, loading, error)
- [ ] Define API endpoint specifications (request/response schemas)
- [ ] Design database schema (tables, indexes, foreign keys)
- [ ] Document component hierarchy and state management
- [ ] Create functional specification document
- [ ] Human review of design before implementation

---

## Quality Gate Status

**Business Requirements Phase**: ✅ COMPLETE
- Quality Score: 100% (10/10 checklist items)
- All user stories defined with acceptance criteria
- All business rules documented
- All constraints identified
- All risks assessed with mitigations

**Next Gate**: Design Phase (Target: 90% quality)

---

## Handoff Acknowledgment

**Receiving Agents**:
- [ ] UI Designer: Read and understood business requirements
- [ ] Functional Spec Agent: Read and understood technical constraints
- [ ] Database Designer: Read and understood schema requirements

**Questions or Clarifications**: Contact Business Requirements Agent

**Handoff Date**: 2025-10-17
**Next Review**: After design wireframes complete

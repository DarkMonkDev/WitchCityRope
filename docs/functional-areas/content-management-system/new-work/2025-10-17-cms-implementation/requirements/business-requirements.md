# Business Requirements: Content Management System (CMS)
<!-- Last Updated: 2025-10-17 -->
<!-- Version: 2.0 - APPROVED -->
<!-- Owner: Business Requirements Agent -->
<!-- Status: APPROVED 2025-10-17 - Ready for Phase 2 Design -->

## Executive Summary

WitchCityRope requires a simple, in-place Content Management System (CMS) to enable administrators to edit static text pages (Contact Us, Resources, Private Lessons) directly through the web interface without developer intervention. This admin-only feature will eliminate deployment bottlenecks for routine content updates, reduce developer workload, and empower subject matter experts to manage their own content while maintaining a complete audit trail of all changes.

**Business Value**:
- **Developer Time Savings**: ~4-8 hours per month eliminating deployment cycles for content updates
- **Faster Content Updates**: Changes go live immediately instead of waiting for next deployment
- **Content Ownership**: Administrators can manage content without technical dependencies
- **Audit Compliance**: Complete revision history with user attribution for accountability

**Success Metrics**:
- 100% of static page updates handled by admins without developer intervention
- Content update time reduced from 1-3 days (deploy cycle) to <5 minutes (immediate)
- Zero XSS vulnerabilities from user-submitted content
- <200ms page load times for all CMS-managed pages

---

## Stakeholder Approval

**APPROVED**: October 17, 2025 ✅

**Approval Document**: `/docs/functional-areas/content-management-system/new-work/2025-10-17-cms-implementation/reviews/phase1-requirements-review.md`

**Phase 2 Authorization**: Ready for Design & Architecture phase (UI Design FIRST)

---

## Business Context

### Current State (Problem)

**Pain Points**:
1. **Developer Bottleneck**: All static page content changes require developer intervention, code commits, and deployment cycles
2. **Slow Update Cycle**: Simple text changes take 1-3 days to go live (waiting for developer availability + deployment window)
3. **No Content Ownership**: Subject matter experts cannot update their own pages (e.g., Resources page for educational content)
4. **No Change Tracking**: No visibility into who changed what content or when
5. **Risk of Errors**: Manual file editing risks introducing HTML/formatting errors

**Current Workflow** (Manual, Slow):
```
Content change request
  ↓
Developer edits HTML in codebase
  ↓
Git commit + push
  ↓
CI/CD pipeline build
  ↓
Deploy to production (1-3 days total)
```

**Business Impact**:
- Delayed response to community needs (e.g., updating Resources page with new safety information)
- Developer time wasted on non-technical tasks
- Lost momentum on time-sensitive content updates (events, announcements)

---

### Desired State (Solution)

**Target Workflow** (Self-Service, Instant):
```
Admin clicks "Edit Page" button (always visible)
  ↓
TipTap editor appears in-place
  ↓
Admin edits content with rich text formatting
  ↓
Click "Save" → Content live immediately (<5 minutes total)
  ↓
Revision saved with user attribution + timestamp
```

**Business Outcomes**:
- **Admin Empowerment**: Administrators can update static pages on-demand
- **Instant Publishing**: Changes go live immediately with no deploy cycle
- **Audit Trail**: Complete history of who changed what and when
- **Developer Focus**: Developers freed from routine content tasks
- **Quality Control**: Revision history enables rollback if mistakes occur

---

### Why Now?

**Immediate Triggers**:
1. **Recent HTML Editor Migration**: TipTap editor now fully integrated (October 2025) - infrastructure ready
2. **Admin Authentication Complete**: BFF pattern with httpOnly cookies operational - security foundation exists
3. **Content Update Requests**: Active need to update Resources and Contact Us pages
4. **Developer Bottleneck**: Current backlog includes 3 pending content changes waiting for deployment

**Strategic Alignment**:
- **React Migration Progress**: Part of overall platform modernization
- **Self-Service Platform**: Aligns with goal of reducing operational overhead
- **Community Empowerment**: Supports community-driven content management

---

## User Stories

### Story 1: Admin Edits Static Page Content

**As an** Administrator
**I want to** click an "Edit" button on static pages and edit content in-place using a rich text editor
**So that** I can update page content immediately without developer intervention

**Acceptance Criteria**:
- **Given** I am logged in as an Administrator
- **When** I navigate to a CMS-managed page (Resources, Contact Us, Private Lessons)
- **Then** I see an "Edit Page" button **always visible** in the header (NOT hover-only)
- **And** the button is only visible to users with Administrator role
- **And** non-admin users do not see the edit button

**Edit Workflow**:
- **Given** I am an Administrator viewing a CMS page
- **When** I click the "Edit Page" button
- **Then** the page content is replaced with a TipTap rich text editor
- **And** the editor is pre-populated with the current page content
- **And** I see "Save" and "Cancel" buttons
- **And** the page title is editable in a text input field

**Mobile Considerations**:
- Editor must be fully functional on mobile devices (many admins use phones)
- Touch-friendly controls for text formatting
- Responsive layout for small screens

**STAKEHOLDER CLARIFICATION (2025-10-17)**:
- Edit button is **ALWAYS VISIBLE** to administrators for maximum discoverability
- Not hidden until hover or click
- Clear visual indicator that page is editable for admin users

---

### Story 2: Admin Saves Content Changes

**As an** Administrator
**I want to** save my content changes and see them reflected immediately
**So that** I can publish updates without waiting for deployment cycles

**Acceptance Criteria**:
- **Given** I have edited content in the TipTap editor
- **When** I click the "Save" button
- **Then** the content is saved to the database
- **And** the editor is replaced with the rendered HTML view
- **And** I see a success notification "Content saved successfully"
- **And** the page displays my updated content immediately
- **And** a revision is created in the database with my user ID and timestamp

**Optimistic Update Behavior**:
- **Given** I click "Save"
- **When** the save request is in-flight to the server
- **Then** the UI updates immediately (optimistic update)
- **And** if the save fails, the UI rolls back to previous state
- **And** I see an error notification "Failed to save content"

**Performance**:
- Save response time: <500ms server response
- UI update: <16ms (instant feedback via optimistic update)

---

### Story 3: Admin Cancels Editing

**As an** Administrator
**I want to** cancel my edits and discard changes
**So that** I can abandon changes if I make a mistake

**Acceptance Criteria**:
- **Given** I am editing content in the TipTap editor
- **When** I click the "Cancel" button
- **Then** I am prompted to confirm "Discard unsaved changes?"
- **And** if I confirm, the editor is replaced with the original content
- **And** my edits are discarded
- **And** if I decline, I remain in edit mode

**Unsaved Changes Warning**:
- **Given** I have unsaved changes in the editor
- **When** I attempt to navigate away from the page
- **Then** the browser warns "You have unsaved changes. Leave page?"
- **And** if I cancel navigation, I stay on the edit page
- **And** if I confirm, I navigate away and changes are lost

---

### Story 4: Admin Views Revision History

**As an** Administrator
**I want to** view a history of changes made to a page in the admin dashboard
**So that** I can see who changed what content and when, and potentially restore previous versions

**Acceptance Criteria**:
- **Given** I am an Administrator viewing the admin dashboard
- **When** I navigate to the CMS revision history page
- **Then** I see a list of all CMS pages with revision counts
- **And** clicking a page shows its full revision history
- **And** each revision shows:
  - Timestamp (e.g., "October 17, 2025 at 2:30 PM")
  - Username (e.g., "admin@witchcityrope.com")
  - Change description (e.g., "Updated via web interface")
  - Content preview (first 100 characters)
- **And** revisions are sorted newest-first
- **And** I see up to 50 most recent revisions per page

**Admin Dashboard Page Requirements**:
- Dedicated admin dashboard route: `/admin/cms/revisions`
- Lists all CMS pages with revision counts
- Click page name to view full revision history
- Each revision displays full content snapshot (not diff)
- UI shows who made the change and when

**Future Enhancement** (Post-MVP):
- Rollback/restore to previous revision capability
- Diff view between revisions
- Revision comparison side-by-side

**STAKEHOLDER CLARIFICATION (2025-10-17)**:
- This is a **new requirement** added based on stakeholder feedback
- Revision history must be **viewable in admin dashboard**, not just logged in database
- Build admin dashboard page for viewing history
- Restore/rollback capability is future enhancement (not MVP blocker)

---

### Story 5: System Prevents XSS Attacks

**As a** Platform Owner
**I want** all user-submitted HTML to be sanitized on the backend
**So that** malicious scripts cannot be injected into page content

**Acceptance Criteria**:
- **Given** an Administrator submits content with potentially malicious HTML
- **When** the content is saved to the database
- **Then** all `<script>` tags are removed
- **And** all `<iframe>` tags are removed
- **And** all JavaScript event handlers (onclick, onerror, etc.) are removed
- **And** only whitelisted HTML tags are preserved:
  - Text formatting: `<p>`, `<br>`, `<strong>`, `<em>`, `<u>`, `<s>`, `<strike>`
  - Headings: `<h1>` through `<h6>`
  - Lists: `<ul>`, `<ol>`, `<li>`
  - Other: `<blockquote>`, `<a>`, `<code>`, `<pre>`
- **And** only safe attributes are preserved: `href`, `title`, `class`
- **And** only safe CSS properties are preserved: `text-align`

**Security Requirements**:
- Backend-only sanitization (frontend bypassing won't work)
- HtmlSanitizer.NET library (OWASP-recommended)
- Admin users are trusted, but content is still sanitized (defense in depth)

---

## Functional Requirements

### FR-1: Page Identification and Routing

**Requirement**: CMS pages are identified by URL slug, with direct route-to-database mapping

**Business Rules**:
1. Each CMS page has a unique slug (e.g., "resources", "contact-us", "private-lessons")
2. Slug is part of the URL path: `/resources`, `/contact-us`, `/private-lessons`
3. React routes map directly to slugs (no database lookup for routing)
4. Adding a new CMS page requires:
   - Database entry with unique slug
   - React route definition in App.tsx
   - Developer intervention for route (acceptable for infrequent additions)

**Initial Pages** (MVP):
- `/resources` → "Resources" page
- `/contact-us` → "Contact Us" page
- `/private-lessons` → "Private Lessons" page

**Rationale**: Simplicity over flexibility - 3 pages don't require complex dynamic routing

**STAKEHOLDER CONFIRMATION (2025-10-17)**:
- **Approved**: Start with 3 pages only
- **Future scalability**: Architecture supports easy addition of new pages (just add route mapping)
- No additional pages for MVP

---

### FR-2: In-Place Content Editing

**Requirement**: Administrators can edit page content directly on the page using TipTap editor

**Edit Mode Workflow**:
1. Admin navigates to CMS page (e.g., `/resources`)
2. Admin sees "Edit Page" button **always visible** (top-right or bottom-right)
3. Admin clicks "Edit Page"
4. Page content is replaced with:
   - Text input for page title
   - TipTap rich text editor pre-populated with current HTML
   - "Save" and "Cancel" buttons
5. Admin edits content using TipTap formatting controls
6. Admin clicks "Save" or "Cancel"

**View Mode Workflow**:
1. Page displays rendered HTML content
2. No editing controls visible (except Edit button for admins)
3. Standard page layout with navigation header

**TipTap Editor Features** (Must Support):
- Text formatting: bold, italic, underline, strikethrough
- Headings: H1, H2, H3, H4, H5, H6
- Lists: unordered, ordered
- Links: insert/edit hyperlinks
- Blockquotes
- Code blocks
- Text alignment (left, center, right)
- Undo/redo

**Editor Configuration**:
- Minimum 10-15 rows visible (enough content visible to edit)
- Responsive on mobile (touch-friendly)
- Variable insertion NOT required for CMS pages (event-specific feature)

**STAKEHOLDER CLARIFICATION (2025-10-17)**:
- Edit button is **ALWAYS VISIBLE** to administrators (not hidden/hover-only)
- Clear visual indicator for admin users that page is editable

---

### FR-3: Content Persistence and Retrieval

**Requirement**: Page content is stored in PostgreSQL and retrieved via Minimal API

**Data Structure Requirements**:
- **Page Title**: String, required, 3-200 characters
- **Page Slug**: String, required, unique, 3-100 characters, lowercase with hyphens
- **Content HTML**: Text, required, no size limit (PostgreSQL TEXT column)
- **Created At**: DateTime, required, UTC timezone
- **Updated At**: DateTime, required, UTC timezone
- **Created By**: User ID (foreign key to AspNetUsers), required
- **Last Modified By**: User ID (foreign key to AspNetUsers), required

**API Endpoints Required**:
1. `GET /api/cms/pages/{slug}` - Public endpoint, fetch page by slug
2. `PUT /api/cms/pages/{id}` - Admin-only, update page content
3. `GET /api/cms/pages/{id}/revisions` - Admin-only, fetch revision history
4. `POST /api/cms/pages/{id}/rollback/{revisionId}` - Admin-only, rollback to revision (future)

**Caching Strategy**:
- TanStack Query cache on frontend (5-minute stale time)
- No server-side caching (content changes are infrequent)

---

### FR-4: Revision History Tracking

**Requirement**: All content changes are tracked with full audit trail and viewable in admin dashboard

**Revision Data Requirements**:
- **Content Snapshot**: Full HTML content at time of save (not diff, full copy)
- **Title Snapshot**: Page title at time of save
- **Timestamp**: UTC datetime of change
- **User Attribution**: User ID who made the change
- **Change Description**: Optional text field (e.g., "Updated safety information")

**Business Rules**:
1. Revision is created BEFORE updating current content (on every save)
2. Revisions are never deleted (permanent audit trail)
3. If page is deleted, all revisions are cascade-deleted (data cleanup)
4. If user is deleted, revisions are preserved with user ID (audit trail intact)
5. Revision history limited to 50 most recent entries in UI (database stores all)

**Admin Dashboard Requirements** (NEW):
- Dedicated route: `/admin/cms/revisions`
- Lists all CMS pages with revision counts
- Clicking page shows full revision history modal/page
- Each revision displays:
  - Timestamp in local timezone
  - Username (email or display name)
  - Change description
  - Content preview (first 100 characters)
  - Full content snapshot (expandable)

**Revision History Display**:
- Sorted newest-first
- Shows timestamp, username, change description
- Content preview: first 100 characters of HTML
- No diff view in MVP (full snapshot only)

**Future Features** (Out of Scope for MVP):
- Rollback to previous revision
- Compare two revisions (diff view)
- Export revision history

**STAKEHOLDER CLARIFICATION (2025-10-17)**:
- **NEW REQUIREMENT**: Build admin dashboard page to view revision history
- Not just database logging - must be viewable in UI
- Restore/rollback capability is future enhancement (not MVP)

---

### FR-5: Authorization and Security

**Requirement**: Only users with Administrator role can edit content

**Authorization Rules**:
1. **View Access**: All users (including guests) can view CMS pages
2. **Edit Access**: ONLY users with "Administrator" role can edit
3. **Edit Button Visibility**: Edit button only rendered for authenticated Administrators
4. **API Endpoint Protection**: All write endpoints require "Administrator" role
5. **Frontend Bypass Protection**: Backend validates role even if frontend bypassed

**Authentication Pattern**:
- HttpOnly cookies (existing BFF pattern)
- Role-based authorization using ASP.NET Core Identity roles
- No special CMS permissions (reuse existing Administrator role)

**Security Layers**:
1. **Frontend**: Edit button hidden for non-admins (UX)
2. **API Gateway**: Role check on all write endpoints (enforcement)
3. **HTML Sanitization**: Backend removes malicious HTML (XSS prevention)
4. **Database Constraints**: Foreign keys ensure valid user attribution

**STAKEHOLDER CONFIRMATION (2025-10-17)**:
- **Approved**: Administrator role only (no Teacher or other role access)

---

### FR-6: User Experience Requirements

**Requirement**: Editing workflow must be intuitive, fast, and mobile-friendly

**UX Principles**:
1. **Discoverability**: Edit button **always visible** to admins (not hidden/hover)
2. **Instant Feedback**: Optimistic updates for perceived <16ms save time
3. **Error Recovery**: Automatic rollback on save failure with clear error message
4. **Unsaved Changes Protection**: Browser warning before navigation with dirty state
5. **Mobile-First**: All editing controls work on phones/tablets

**Loading States**:
- Page load: Skeleton loader or "Loading..." text
- Saving: "Save" button shows spinner, disabled state
- Error: Toast notification with retry option

**Notifications**:
- Success: "Content saved successfully" (green toast, 3-second auto-dismiss)
- Error: "Failed to save content. Please try again." (red toast, manual dismiss)
- Unsaved Changes: "You have unsaved changes. Discard them?" (browser confirm dialog)

**Accessibility**:
- Edit button has ARIA label "Edit page content"
- TipTap editor is keyboard-navigable
- Save/Cancel buttons are keyboard-accessible
- Color contrast meets WCAG AA standards

**STAKEHOLDER CLARIFICATION (2025-10-17)**:
- Optimistic updates approved for UX pattern
- Edit button always visible (maximum discoverability)

---

## Non-Functional Requirements

### NFR-1: Performance

**Requirements**:
- **Page Load Time**: <200ms for CMS page initial render (measured from API response)
- **Save Response Time**: <500ms from click to server confirmation
- **UI Update Time**: <16ms perceived update (optimistic update before server response)
- **Editor Load Time**: <100ms for TipTap editor to render and become interactive

**Optimization Strategies**:
- TanStack Query caching (5-minute stale time, 10-minute cache time)
- PostgreSQL TEXT column (no JSON parsing overhead)
- Database indexes on slug and ContentPageId
- Optimistic updates for instant UI feedback

**Bundle Size Impact**:
- Frontend: +0KB (TipTap already integrated)
- Backend: +15KB (HtmlSanitizer.NET NuGet package)

---

### NFR-2: Security

**Requirements**:
- **XSS Prevention**: 100% of user-submitted HTML must be sanitized
- **Role-Based Access**: 100% of edit operations require Administrator role
- **Audit Trail**: 100% of content changes must be tracked with user attribution
- **Cookie Security**: Maintain existing httpOnly + SameSite=Strict cookies

**Threat Model**:
- **Malicious Admin**: Admin submits `<script>` tags → Sanitizer removes them
- **Frontend Bypass**: Direct API call without authentication → 401 Unauthorized
- **Role Escalation**: Non-admin modifies role claim → Backend validates against database
- **SQL Injection**: User input in content → Entity Framework parameterization prevents

**Compliance**:
- OWASP Top 10 XSS prevention guidelines
- GDPR audit trail requirements (user attribution)

---

### NFR-3: Usability

**Requirements**:
- **Learning Curve**: Administrators can edit pages with <5 minutes training
- **Mobile Usability**: 100% of editing features work on phones (iOS Safari, Android Chrome)
- **Error Recovery**: Users can recover from mistakes via Cancel button or revision history
- **Browser Compatibility**: Works on latest Chrome, Firefox, Safari, Edge

**Usability Testing Criteria**:
- Admin can navigate to page, click Edit, make change, save in <60 seconds
- No console errors during editing workflow
- All buttons clearly labeled and obvious function
- Tooltips or help text for non-obvious features

---

### NFR-4: Reliability

**Requirements**:
- **Data Persistence**: 100% of saved content must persist to database (no data loss)
- **Revision History Integrity**: 100% of revisions must be captured (no missed saves)
- **Error Handling**: Graceful degradation if save fails (rollback, user notification, retry option)
- **Concurrent Editing**: Last-write-wins strategy (no optimistic locking in MVP)

**Error Scenarios**:
- **Network Failure**: Optimistic update rolls back, toast notification, retry option
- **Database Failure**: 500 error logged, user sees "Server error, please try again later"
- **Validation Failure**: 400 error with specific message (e.g., "Title required")

**Data Backup**:
- Standard PostgreSQL backup procedures (no special CMS backup)
- Revision history acts as built-in versioning backup

---

### NFR-5: Maintainability

**Requirements**:
- **Adding New Pages**: Developer can add new CMS page in <30 minutes
- **Code Clarity**: Code follows existing Minimal API vertical slice pattern
- **Type Safety**: 100% TypeScript coverage with NSwag-generated types
- **Testing**: All API endpoints have unit tests, key workflows have E2E tests

**Developer Experience**:
- Clear separation: CMS feature in `/apps/api/Features/Cms/` folder
- Consistent naming: `CmsEndpoints.cs`, `CmsPage.tsx`, `useCmsPage.ts`
- Self-documenting code: Inline comments for non-obvious business rules
- OpenAPI documentation: Swagger UI for all CMS endpoints

---

## Technical Constraints

### Technology Stack (Must Use)

**Frontend**:
- React 18 with TypeScript
- Mantine v7 UI components
- TanStack Query v5 for data fetching
- React Router v7 for routing
- Existing `MantineTiptapEditor.tsx` component (already integrated)

**Backend**:
- .NET 9 Minimal API
- Vertical slice architecture pattern
- Entity Framework Core for database access
- HtmlSanitizer.NET for XSS prevention

**Database**:
- PostgreSQL
- Entity Framework Core migrations
- Schema: `cms` (separate from `auth`, `events`, etc.)

**Type Generation**:
- NSwag auto-generation from C# DTOs to TypeScript
- NO manual TypeScript interfaces for API data
- All types in `@witchcityrope/shared-types` package

---

### Architecture Constraints

**Must Follow**:
1. **Minimal API Vertical Slice Pattern**: All CMS logic in `/apps/api/Features/Cms/` folder
2. **BFF Authentication Pattern**: httpOnly cookies via existing `/auth/login` endpoint
3. **TanStack Query Patterns**: Optimistic updates, cache invalidation, error handling
4. **NSwag Type Generation**: C# DTOs → TypeScript interfaces (automated)

**Must NOT**:
1. **Manual TypeScript Interfaces**: Will conflict with NSwag generation
2. **localStorage for Auth**: Security risk, use cookies
3. **Direct Database Access**: Use Entity Framework Core only
4. **Inline Styles**: Use Mantine components and CSS modules

---

### Performance Constraints

**Hard Limits**:
- Page load: <200ms (target), <500ms (maximum acceptable)
- Save response: <500ms server response
- Database query: <100ms for page fetch by slug
- Editor load: <100ms to interactive

**Mobile Performance**:
- Works on 3G connection (slower network tolerance)
- Touch targets minimum 44×44 pixels (iOS accessibility guidelines)

---

## Future Considerations

### Scalability Planning

**More CMS Pages** (Future):
- Architecture supports easy addition of new pages
- Process: Add database entry + React route mapping
- Developer task: ~30 minutes per new page
- No architecture changes required

**Image Upload Capability** (Future):
- Phase 2 feature with DigitalOcean Spaces file storage
- File upload component
- Image optimization and CDN delivery
- Media library for image management

**File Storage Integration** (Future):
- DigitalOcean Spaces for image/file hosting
- S3-compatible API for file operations
- CDN integration for performance

**STAKEHOLDER CLARIFICATION (2025-10-17)**:
- **Scalability noted**: Architecture designed to support additional pages easily
- **Image uploads planned**: Future phase will integrate DigitalOcean Spaces
- **File storage strategy**: DigitalOcean Spaces for when images/files are needed

---

## Success Criteria

### Definition of "Done"

**MVP Feature Complete When**:
- ✅ Administrators can edit all 3 initial pages (Resources, Contact Us, Private Lessons)
- ✅ Content changes are saved to database with revision history
- ✅ Revision history is viewable in admin dashboard page
- ✅ XSS sanitization removes malicious HTML
- ✅ Only Administrators can see/use edit functionality
- ✅ All pages load in <200ms
- ✅ Mobile editing works on phones/tablets
- ✅ E2E tests verify full editing workflow
- ✅ Edit button is always visible to admins (not hidden/hover)

---

### User Acceptance Criteria

**Admin Perspective**:
- "I can update the Resources page in under 2 minutes without technical help"
- "I can see who last edited the Contact Us page and when in the admin dashboard"
- "If I make a mistake, I can click Cancel and undo my changes"
- "The editor works on my iPhone when I need to make urgent updates"
- "The Edit button is always visible so I know I can edit the page"

**Developer Perspective**:
- "I no longer get requests to update static page content"
- "Adding a new CMS page takes <30 minutes"
- "The code follows our established patterns and is easy to maintain"

**Security Perspective**:
- "Even if an admin tries to inject malicious HTML, it's sanitized on the backend"
- "There's a complete audit trail of all content changes"
- "Non-admin users cannot access edit functionality even if they bypass frontend"

---

### Performance Benchmarks

**Measured at**:
- Desktop Chrome (high-speed connection)
- Mobile Safari (3G connection)

**Targets**:
| Metric | Desktop Target | Mobile Target | Maximum Acceptable |
|--------|----------------|---------------|-------------------|
| Page Load | <100ms | <200ms | <500ms |
| Save Response | <200ms | <500ms | <1000ms |
| Editor Load | <50ms | <100ms | <200ms |
| Bundle Size Increase | N/A (TipTap exists) | N/A | +50KB max |

---

### Quality Standards

**Code Quality**:
- Zero TypeScript errors
- Zero console warnings in production
- 90%+ test coverage for CMS endpoints
- All TypeScript types from NSwag generation (no manual interfaces)

**Security Quality**:
- 100% of HTML sanitized on backend
- 100% of edit operations authorized
- Zero XSS vulnerabilities (verified by security audit)

**UX Quality**:
- No user-reported confusion about editing workflow
- Zero data loss incidents
- <1% error rate on saves (network/server issues only)

---

## Risks and Assumptions

### Technical Risks

**MEDIUM: Admin Accidentally Overwrites Content**
- **Likelihood**: Medium (human error)
- **Impact**: Medium (content lost if no revision)
- **Mitigation**:
  - Revision history captures all changes
  - Unsaved changes warning prevents accidental navigation
  - Cancel button allows aborting edits
  - Rollback feature (Phase 2) enables recovery
- **Monitoring**: Track number of revisions per page, alert if excessive

**LOW: XSS Vulnerability from Admin**
- **Likelihood**: Low (admins are trusted users)
- **Impact**: High (if vulnerability exists)
- **Mitigation**:
  - Backend sanitization with HtmlSanitizer.NET (OWASP-recommended)
  - Whitelist approach (only allowed tags preserved)
  - Regular security audits of sanitization config
- **Monitoring**: Log all sanitization actions, review for suspicious patterns

**LOW: Database Migration Failure**
- **Likelihood**: Low (standard Entity Framework migration)
- **Impact**: High (CMS feature unavailable)
- **Mitigation**:
  - Test migration in development environment first
  - Database backup before production migration
  - Rollback script prepared
- **Monitoring**: Migration logs, database health checks

**LOW: TipTap Editor Incompatibility**
- **Likelihood**: Very Low (TipTap already integrated)
- **Impact**: Medium (editing functionality broken)
- **Mitigation**:
  - TipTap version pinned in package.json
  - E2E tests verify editor functionality
  - Existing editor already in production use
- **Monitoring**: E2E test suite runs on every deployment

---

### Business Assumptions

**ASSUMPTION: 3 Pages Sufficient for MVP**
- **Rationale**: Contact Us, Resources, Private Lessons are the only static pages needing CMS
- **Validation**: CONFIRMED with stakeholder 2025-10-17 ✅
- **Risk if Wrong**: More pages needed = straightforward to add (30 min each)

**ASSUMPTION: Admin-Only Access Adequate**
- **Rationale**: Only Administrators need content editing capability
- **Validation**: CONFIRMED with stakeholder 2025-10-17 ✅
- **Risk if Wrong**: Role-based permissions can be expanded to include other roles

**ASSUMPTION: Text-Only Content Sufficient**
- **Rationale**: MVP does not require image upload capability
- **Validation**: CONFIRMED with stakeholder 2025-10-17 ✅
- **Risk if Wrong**: Image upload can be added in Phase 2 (backend + frontend work)

**ASSUMPTION: Live Editing Without Draft/Published States**
- **Rationale**: Simple immediate publishing meets current needs
- **Validation**: CONFIRMED with stakeholder 2025-10-17 ✅
- **Risk if Wrong**: Draft state can be added with database column + UI toggle

**ASSUMPTION: Revision History Sufficient Without Rollback**
- **Rationale**: Viewing history provides accountability, rollback is Phase 2 feature
- **Validation**: CONFIRMED with stakeholder 2025-10-17 ✅
- **Risk if Wrong**: Rollback can be added later (already designed in research)

---

### Mitigation Strategies

**Risk Mitigation Process**:
1. **Pre-Implementation Review**: Validate all assumptions with stakeholders ✅ COMPLETE
2. **Phased Rollout**: Deploy to staging first, test with real admins
3. **Training Session**: 30-minute admin training on editing workflow
4. **Monitoring Dashboard**: Track save success rate, edit frequency, error rate
5. **Feedback Loop**: Monthly check-in with admins on usability issues

**Contingency Plans**:
- **Data Loss**: Revision history enables manual content reconstruction
- **Performance Issues**: Database indexes + caching can be tuned
- **Usability Issues**: UI can be iterated based on admin feedback

---

## Out of Scope (Important!)

### Explicitly NOT Included in MVP

**SEO Metadata Fields**:
- No meta title, meta description, or Open Graph tags
- **Rationale**: Static pages don't require custom SEO (site-wide SEO sufficient)
- **Future**: Can add SEO fields in Phase 2 if needed

**Image Upload Capability**:
- No image upload or media library
- **Rationale**: Text-only content keeps MVP simple
- **Workaround**: Admins can link to externally-hosted images if needed
- **Future**: Phase 2 feature with image storage + CDN integration + DigitalOcean Spaces

**Draft/Published Workflow**:
- No draft state, all saves are immediate publish
- **Rationale**: Admins are trusted to publish immediately
- **Workaround**: Admins can edit, test, and save when ready
- **Future**: Draft state can be added with database column + UI toggle

**Content Approval Workflow**:
- No multi-user approval process (e.g., editor → reviewer → publisher)
- **Rationale**: Small team, admins are trusted
- **Future**: If needed, can add approval roles and workflow states

**Multi-Language Support**:
- No content translation or language switching
- **Rationale**: English-only platform for Salem community
- **Future**: If community expands, can add language field + content variants

**Advanced Text Formatting**:
- No tables, images, videos, embeds in MVP
- **Rationale**: TipTap supports these, but sanitizer would need configuration
- **Future**: Can enable additional tags in HtmlSanitizer config if needed

**Content Scheduling**:
- No scheduled publish dates
- **Rationale**: Immediate publishing meets current needs
- **Future**: Can add PublishedAt field + scheduler if needed

**Full-Text Search**:
- No search within CMS content
- **Rationale**: Only 3 pages, users can browse manually
- **Future**: PostgreSQL full-text search can be added if pages grow

---

## User Impact Analysis

| User Type | Impact | Priority | Benefit | Change Required |
|-----------|--------|----------|---------|-----------------|
| **Administrator** | High | High | Can edit static pages without developer help, changes live immediately | Learn new editing workflow (~5 min training) |
| **Teacher** | None | N/A | No change | None |
| **Vetted Member** | Low | Low | More up-to-date content as admins can update faster | None (transparent) |
| **General Member** | Low | Low | More up-to-date content | None (transparent) |
| **Guest** | Low | Low | More up-to-date content | None (transparent) |
| **Developer** | High | High | Freed from static content update requests (~4-8 hrs/month) | Implement CMS feature (~6.5 hrs) |

### Impact Details

**Administrators**:
- **Positive**: Empowered to manage content independently
- **Positive**: No waiting for developer availability
- **Positive**: Can see who made changes and when (accountability) in admin dashboard
- **Positive**: Edit button always visible (clear discoverability)
- **Neutral**: Must learn TipTap editor (familiar WYSIWYG interface)
- **Negative**: None identified

**End Users** (Members/Guests):
- **Positive**: Content updates happen faster (better experience)
- **Positive**: More accurate/current information
- **Neutral**: No visible change to page layout or functionality
- **Negative**: None identified

**Developers**:
- **Positive**: Eliminates routine content update tasks
- **Positive**: Reduces deployment frequency for content-only changes
- **Neutral**: Must implement and maintain CMS feature
- **Negative**: None identified (net positive)

---

## Examples/Scenarios

### Scenario 1: Admin Updates Resources Page (Happy Path)

**Context**: Administrator wants to add new safety guidelines to Resources page

**Steps**:
1. Admin logs in to WitchCityRope as admin@witchcityrope.com
2. Admin navigates to `/resources` page
3. Admin sees current content: "Community Resources - Safety Guidelines..."
4. Admin sees "Edit Page" button **always visible** in top-right corner
5. Admin clicks "Edit Page"
6. Page content is replaced with TipTap editor pre-populated with current HTML
7. Page title field shows "Resources"
8. Admin scrolls to Safety Guidelines section in editor
9. Admin adds new bullet point: "Always have safety scissors within arm's reach"
10. Admin clicks "Save" button
11. UI immediately shows updated content (optimistic update)
12. After 200ms, server confirms save
13. Success notification appears: "Content saved successfully"
14. Admin refreshes page to verify changes persisted
15. Changes are visible to all users immediately

**Expected Outcome**:
- ✅ New content visible immediately
- ✅ Revision created in database with admin's user ID and timestamp
- ✅ No deployment required
- ✅ Total time: <2 minutes

---

### Scenario 2: Admin Cancels Edit After Mistake (Cancel Workflow)

**Context**: Administrator starts editing but makes unwanted changes

**Steps**:
1. Admin clicks "Edit Page" on Contact Us page
2. Editor appears with current content
3. Admin accidentally deletes entire "Email Us" section
4. Admin realizes mistake
5. Admin clicks "Cancel" button
6. Browser confirms: "Discard unsaved changes?"
7. Admin clicks "OK"
8. Editor disappears, original content restored
9. Deleted section is back (no data loss)

**Expected Outcome**:
- ✅ Changes discarded
- ✅ Original content preserved
- ✅ No database write occurred
- ✅ User protected from accidental data loss

---

### Scenario 3: Network Failure During Save (Error Recovery)

**Context**: Administrator saves content but network fails mid-request

**Steps**:
1. Admin edits Private Lessons page
2. Admin clicks "Save"
3. Optimistic update shows new content immediately
4. Network request times out (server unreachable)
5. TanStack Query detects error
6. UI rolls back to previous content (optimistic update reversed)
7. Error notification appears: "Failed to save content. Please try again."
8. Admin clicks "Save" again
9. Network is restored
10. Save succeeds
11. Success notification: "Content saved successfully"

**Expected Outcome**:
- ✅ User aware of failure
- ✅ Content not lost (still in editor)
- ✅ Retry option available
- ✅ No partial/corrupt data saved

---

### Scenario 4: Admin Attempts XSS Injection (Security Validation)

**Context**: Admin (malicious or testing) tries to inject JavaScript

**Steps**:
1. Admin edits Resources page
2. Admin pastes malicious HTML into editor:
   ```html
   <h2>Test</h2>
   <script>alert('XSS')</script>
   <p onclick="alert('XSS')">Click me</p>
   ```
3. Admin clicks "Save"
4. Backend receives content
5. HtmlSanitizer.NET processes content:
   - Removes `<script>` tag entirely
   - Removes `onclick` attribute from `<p>` tag
   - Preserves `<h2>` and `<p>` text
6. Sanitized content saved to database:
   ```html
   <h2>Test</h2>
   <p>Click me</p>
   ```
7. Page displays sanitized content (no script execution)

**Expected Outcome**:
- ✅ Malicious scripts removed
- ✅ Safe content preserved
- ✅ No XSS vulnerability
- ✅ Backend protects even if admin bypasses frontend

---

### Scenario 5: Admin Views Revision History (Audit Trail)

**Context**: Administrator wants to see who last updated Contact Us page

**Steps**:
1. Admin navigates to `/admin/cms/revisions` admin dashboard page
2. Admin sees list of all CMS pages with revision counts:
   ```
   CMS Pages:
   - Contact Us (12 revisions)
   - Resources (8 revisions)
   - Private Lessons (5 revisions)
   ```
3. Admin clicks "Contact Us"
4. Modal/page displays revision list:
   ```
   Revision History: Contact Us

   1. October 17, 2025 at 3:45 PM - admin@witchcityrope.com
      "Updated phone number"
      Content preview: "<h1>Contact Us</h1><p>Reach us at 555-123-4567..."

   2. October 10, 2025 at 10:22 AM - admin@witchcityrope.com
      "Updated via web interface"
      Content preview: "<h1>Contact Us</h1><p>Reach us at 555-999-8888..."

   3. September 28, 2025 at 2:15 PM - system
      "Initial content"
      Content preview: "<h1>Contact Us</h1><p>Email us at info@witchc..."
   ```
5. Admin sees complete audit trail
6. Admin can identify when phone number was changed and by whom
7. Admin can expand any revision to see full content snapshot

**Expected Outcome** (MVP):
- ✅ Complete history visible in admin dashboard
- ✅ User attribution clear
- ✅ Timestamps in local timezone
- ✅ Full content snapshots available
- ⏸️ Rollback feature in Phase 2 (future enhancement)

---

## Questions for Product Manager

### Critical Questions (Must Answer Before Implementation)

**ALL ANSWERED 2025-10-17** ✅

- [x] **Revision History UI**: Should revision history be a modal overlay or separate page? **ANSWERED**: Admin dashboard page at `/admin/cms/revisions`
- [x] **Rollback Feature**: Is rollback capability required for MVP, or can it be Phase 2? **ANSWERED**: Phase 2 (future enhancement)
- [x] **Change Description**: Should admins enter a change description on every save, or optional? **ANSWERED**: Optional field
- [x] **Edit Button Visibility**: Should edit button be always visible or hover-only? **ANSWERED**: Always visible for discoverability
- [x] **Initial Page Count**: Are 3 pages sufficient for MVP? **ANSWERED**: Yes (Contact Us, Resources, Private Lessons)

### Nice-to-Know Questions (Can Defer)

- [x] **Additional Pages**: Are there other pages beyond the 3 initial pages that will need CMS? **ANSWERED**: No additional pages for MVP, architecture supports easy additions
- [ ] **Content Approval**: In future, will any content require approval before publishing? (Affects database design)
- [x] **Image Upload Timeline**: When is image upload capability needed? **ANSWERED**: Future phase with DigitalOcean Spaces integration
- [x] **Draft State**: Would draft/published states be valuable for any content? **ANSWERED**: Not for MVP, can add later if needed

---

## Quality Gate Checklist (100% Required for Phase 2 Approval)

- [x] All user roles addressed (Admin = edit, all others = view only)
- [x] Clear acceptance criteria for each story (Given/When/Then format)
- [x] Business value clearly defined (4-8 hrs/month developer savings, immediate content updates)
- [x] Edge cases considered (network failure, XSS injection, cancel workflow, unsaved changes)
- [x] Security requirements documented (XSS prevention, role-based authorization, audit trail)
- [x] Compliance requirements checked (OWASP XSS guidelines, GDPR audit trail)
- [x] Performance expectations set (<200ms load, <500ms save, optimistic updates)
- [x] Mobile experience considered (touch-friendly, responsive editor, 3G network tolerance)
- [x] Examples provided (5 complete scenarios with expected outcomes)
- [x] Success metrics defined (100% admin self-service, <5 min update time, zero XSS vulnerabilities)
- [x] Stakeholder approval received (October 17, 2025)
- [x] All critical questions answered
- [x] Future considerations documented (scalability, image uploads, file storage)

**Quality Score**: 13/13 (100%) ✅

**Phase 2 Authorization**: **APPROVED** ✅

---

## Appendix: Data Structure Requirements

### Content Page Data

**Business Data Needs** (NSwag will generate TypeScript from C# DTOs):

```csharp
// C# DTO - Source of Truth
public record ContentPageDto
{
    public int Id { get; init; }
    public string Slug { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public DateTime UpdatedAt { get; init; }
    public string LastModifiedBy { get; init; } = string.Empty;
}
```

**Business Rules**:
- `Id`: System-generated, unique identifier
- `Slug`: URL-safe string, lowercase with hyphens, 3-100 characters, must be unique
- `Title`: Plain text, 3-200 characters, required
- `Content`: HTML text, no size limit, sanitized on backend
- `UpdatedAt`: ISO 8601 format, UTC timezone, auto-updated on save
- `LastModifiedBy`: Username or email of last editor, required

### Revision Data

```csharp
// C# DTO - Source of Truth
public record RevisionDto
{
    public int Id { get; init; }
    public DateTime CreatedAt { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
    public string? ChangeDescription { get; init; }
    public string ContentPreview { get; init; } = string.Empty;
}
```

**Business Rules**:
- `CreatedAt`: ISO 8601 format, UTC timezone, immutable
- `CreatedBy`: Username or email of editor, immutable
- `ChangeDescription`: Optional text, 0-500 characters
- `ContentPreview`: First 100 characters of HTML, for display purposes only

### Update Request Data

```csharp
// C# DTO - Source of Truth
public record UpdateContentRequest
{
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string? ChangeDescription { get; init; }
}
```

**Business Rules**:
- All fields validated on backend
- `Title`: Required, 3-200 characters
- `Content`: Required, will be sanitized before storage
- `ChangeDescription`: Optional, defaults to "Content updated" if not provided

**CRITICAL**: Frontend will use NSwag-generated TypeScript interfaces from these DTOs. DO NOT create manual TypeScript interfaces.

---

## Document History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-10-17 | Business Requirements Agent | Initial draft for stakeholder review |
| 2.0 | 2025-10-17 | Business Requirements Agent | **APPROVED** - Added stakeholder feedback: Revision History UI requirement, always-visible edit button, future considerations for image uploads/file storage, confirmed all assumptions |

**Status**: **APPROVED FOR PHASE 2** ✅
**Next Review**: After UI Design completion (mandatory human review)
**Next Phase**: Phase 2 - Design & Architecture (UI Design FIRST per mandatory sequencing)
**Handoff Document**: `/docs/functional-areas/content-management-system/new-work/2025-10-17-cms-implementation/handoffs/business-requirements-2025-10-17-handoff.md`

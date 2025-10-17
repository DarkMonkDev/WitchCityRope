# Agent Handoff: Functional Specification → Implementation
<!-- Date: 2025-10-17 -->
<!-- From: Business Analyst Agent -->
<!-- To: Backend Developer, React Developer, Test Developer -->
<!-- Status: Ready for Implementation -->

## Handoff Overview

**Phase**: Phase 2 Design & Architecture → Phase 3 Implementation

**Deliverable**: Complete functional specification for Content Management System (CMS)

**Document**: `/docs/functional-areas/content-management-system/new-work/2025-10-17-cms-implementation/requirements/functional-specification.md`

**Quality Score**: 18/18 (100%) - Implementation-ready

**Next Agents**: Backend Developer, React Developer, Test Developer (parallel work)

---

## What Was Completed

### Functional Specification Document

**Scope**: Detailed technical specifications for CMS feature enabling administrators to edit static text pages (Contact Us, Resources, Private Lessons) with in-place editing, revision history, and XSS prevention.

**Key Deliverables**:
1. **4 API Endpoint Specifications** - Complete request/response with examples
2. **4 Data Transfer Objects (DTOs)** - With validation rules for NSwag generation
3. **6 React Component Specifications** - With props, state, responsibilities
4. **4 TanStack Query Hooks** - With cache strategies and optimistic updates
5. **Routing Configuration** - Public and admin routes with auth guards
6. **Content Sanitization Rules** - HtmlSanitizer.NET configuration
7. **Error Handling Specifications** - Network, validation, auth, server errors
8. **Performance Requirements** - Quantified targets (<200ms, <500ms, <16ms)
9. **Security Specifications** - Auth, authorization, XSS, SQL injection, CSRF
10. **Acceptance Criteria** - Mapped from approved business requirements
11. **Testing Requirements** - Unit, integration, E2E test scenarios
12. **Integration Points** - Auth system, admin dashboard, existing components

---

## Critical Business Rules (MUST Implement)

### 1. Admin-Only Editing (Security)

**Rule**: ONLY users with "Administrator" role can edit content.

**Backend Enforcement**:
```csharp
[Authorize(Roles = "Administrator")]
app.MapPut("/api/cms/pages/{id}", UpdatePage);
```

**Frontend UX** (NOT security):
```typescript
const isAdmin = hasRole('Administrator');
{isAdmin && <CmsEditButton ... />}
```

**WHY CRITICAL**: Non-admins MUST NOT be able to edit pages even if they bypass frontend.

---

### 2. Backend Sanitization (XSS Prevention)

**Rule**: All HTML content MUST be sanitized on backend BEFORE database write.

**Implementation**:
```csharp
var cleanHtml = sanitizer.Sanitize(request.Content);
page.Content = cleanHtml;  // Store sanitized HTML only
```

**Forbidden Tags**: `<script>`, `<iframe>`, `<object>`, `<embed>`, event handlers

**Allowed Tags**: `<p>`, `<strong>`, `<em>`, `<h1>`-`<h6>`, `<ul>`, `<ol>`, `<li>`, `<a>`, `<blockquote>`, `<code>`, `<pre>`

**WHY CRITICAL**: XSS vulnerability if unsanitized HTML reaches database or frontend.

---

### 3. Revision History (Audit Trail)

**Rule**: Create revision record BEFORE updating current content (on every save).

**Implementation**:
```csharp
// BEFORE update
db.ContentRevisions.Add(new ContentRevision {
    ContentPageId = page.Id,
    Content = page.Content,  // OLD content (snapshot)
    Title = page.Title,       // OLD title (snapshot)
    CreatedAt = DateTime.UtcNow,
    CreatedBy = userId
});

// THEN update
page.Content = cleanHtml;
page.UpdatedAt = DateTime.UtcNow;

await db.SaveChangesAsync();
```

**WHY CRITICAL**: Audit trail required for accountability and rollback capability (Phase 2).

---

### 4. Optimistic Updates (UX)

**Rule**: Update UI immediately (cache) BEFORE server confirmation, rollback on error.

**Implementation** (TanStack Query):
```typescript
onMutate: async (newData) => {
  queryClient.setQueryData(['cms-page', slug], {
    ...old,
    content: newData.content,
    title: newData.title
  });
  return { previousData: old };
},
onError: (err, vars, context) => {
  queryClient.setQueryData(['cms-page', slug], context.previousData);
}
```

**WHY CRITICAL**: <16ms perceived save time critical for mobile UX (vs 200-500ms network).

---

### 5. Route-Based Page Identification

**Rule**: CMS pages identified by URL slug, direct route-to-database mapping.

**Implementation**:
- **Route**: `/resources` → `<CmsPage slug="resources" />`
- **API Call**: `GET /api/cms/pages/resources` (slug in URL)
- **Database**: `SELECT * FROM cms.ContentPages WHERE Slug = 'resources'`

**Initial Pages**:
- `resources` → `/resources`
- `contact-us` → `/contact-us`
- `private-lessons` → `/private-lessons`

**WHY CRITICAL**: Simple, predictable, SEO-friendly, fast lookups.

---

## API Endpoint Summary (Backend)

### Endpoint 1: GET /api/cms/pages/{slug}

**Auth**: Public (no authentication)

**Purpose**: Fetch current page content by URL slug

**Response (200 OK)**:
```json
{
  "id": 1,
  "slug": "resources",
  "title": "Community Resources",
  "content": "<h1>Community Resources</h1><p>...</p>",
  "updatedAt": "2025-10-17T15:45:00Z",
  "lastModifiedBy": "admin@witchcityrope.com"
}
```

**Performance**: <200ms

**Database**: Single SELECT with unique index on Slug

---

### Endpoint 2: PUT /api/cms/pages/{id}

**Auth**: `[Authorize(Roles = "Administrator")]` - CRITICAL

**Purpose**: Update page content, create revision

**Request Body**:
```json
{
  "title": "Updated Title",
  "content": "<p>Updated content...</p>",
  "changeDescription": "Updated safety section"
}
```

**Backend Workflow**:
1. Validate authentication + role
2. Fetch page by ID
3. **CREATE REVISION** (before update)
4. **SANITIZE CONTENT** (HtmlSanitizer.NET)
5. Update page with sanitized content
6. Save to database
7. Return updated page

**Performance**: <500ms

---

### Endpoint 3: GET /api/cms/pages/{id}/revisions

**Auth**: `[Authorize(Roles = "Administrator")]`

**Purpose**: Fetch revision history for page

**Response (200 OK)**:
```json
[
  {
    "id": 45,
    "contentPageId": 1,
    "createdAt": "2025-10-17T15:45:00Z",
    "createdBy": "admin@witchcityrope.com",
    "changeDescription": "Updated phone number",
    "contentPreview": "<h1>Contact Us</h1><p>Reach us at..."
  }
]
```

**Query**: ORDER BY CreatedAt DESC, LIMIT 50

**Performance**: <200ms

---

### Endpoint 4: GET /api/cms/pages

**Auth**: `[Authorize(Roles = "Administrator")]`

**Purpose**: List all CMS pages with revision counts

**Response (200 OK)**:
```json
[
  {
    "id": 1,
    "slug": "contact-us",
    "title": "Contact Us",
    "revisionCount": 12,
    "updatedAt": "2025-10-17T15:45:00Z",
    "lastModifiedBy": "admin@witchcityrope.com"
  }
]
```

**Performance**: <100ms

---

## React Component Summary (Frontend)

### Component 1: CmsPage

**File**: `/apps/web/src/pages/cms/CmsPage.tsx`

**Purpose**: Main editing component (view/edit modes)

**Props**: `slug: string` (URL slug)

**State**: `isEditing`, `editableContent`, `editableTitle`, `isDirty`, `showCancelModal`

**Key Behaviors**:
- Fetch content via `useCmsPage(slug)`
- Show edit button to admins only
- Switch view/edit modes
- Optimistic save workflow
- Cancel with unsaved changes modal
- Browser `beforeunload` warning

---

### Component 2: CmsEditButton

**File**: `/apps/web/src/components/cms/CmsEditButton.tsx`

**Purpose**: Always-visible edit button (desktop sticky, mobile FAB)

**Responsive**:
- Desktop: Sticky top-right (`position: sticky`)
- Mobile: Floating Action Button bottom-right (`position: fixed`)

---

### Component 3: CmsCancelModal

**File**: `/apps/web/src/components/cms/CmsCancelModal.tsx`

**Purpose**: Mantine Modal for unsaved changes confirmation

**Buttons**: "Keep Editing" (outline), "Discard Changes" (danger red)

---

### Component 4: CmsRevisionListPage

**File**: `/apps/web/src/pages/admin/CmsRevisionListPage.tsx`

**Route**: `/admin/cms/revisions`

**Purpose**: Admin dashboard listing all CMS pages with revision counts

**Table**: Page Name, Total Revisions, Last Edited

---

### Component 5: CmsRevisionDetailPage

**File**: `/apps/web/src/pages/admin/CmsRevisionDetailPage.tsx`

**Route**: `/admin/cms/revisions/:pageId`

**Purpose**: Full revision history for single page (timeline view)

**Features**: Back button, breadcrumbs, expandable content, future restore button

---

### Component 6: CmsRevisionCard

**File**: `/apps/web/src/components/cms/CmsRevisionCard.tsx`

**Purpose**: Single revision display with metadata and expandable full content

**Display**: Date, username, change description, content preview, "View Full Content" button

---

## TanStack Query Hooks (Frontend)

### Hook 1: useCmsPage

**File**: `/apps/web/src/hooks/useCmsPage.ts`

**Purpose**: Fetch page + optimistic update mutation

**Query Key**: `['cms-page', slug]`

**Cache**: 5-minute stale time, 10-minute cache time

**Optimistic Update**: Update cache in `onMutate`, rollback in `onError`

---

### Hook 2: useCmsRevisions

**File**: `/apps/web/src/hooks/useCmsRevisions.ts`

**Purpose**: Fetch revision history for page

**Query Key**: `['cms-revisions', pageId]`

**Cache**: 2-minute stale time (revisions change less frequently)

---

### Hook 3: useCmsPageList

**File**: `/apps/web/src/hooks/useCmsPageList.ts`

**Purpose**: Fetch all CMS pages with revision counts

**Query Key**: `['cms-pages']`

**Cache**: 5-minute stale time

---

## DTOs (C# Source of Truth - NSwag Generation)

### ContentPageDto

```csharp
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

---

### UpdateContentRequest

```csharp
public record UpdateContentRequest
{
    [Required]
    [StringLength(200, MinimumLength = 3)]
    public string Title { get; init; } = string.Empty;

    [Required]
    public string Content { get; init; } = string.Empty;

    [StringLength(500)]
    public string? ChangeDescription { get; init; }
}
```

---

### RevisionDto

```csharp
public record RevisionDto
{
    public int Id { get; init; }
    public int ContentPageId { get; init; }
    public DateTime CreatedAt { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
    public string? ChangeDescription { get; init; }
    public string ContentPreview { get; init; } = string.Empty;
}
```

---

### CmsPageSummaryDto

```csharp
public record CmsPageSummaryDto
{
    public int Id { get; init; }
    public string Slug { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public int RevisionCount { get; init; }
    public DateTime UpdatedAt { get; init; }
    public string LastModifiedBy { get; init; } = string.Empty;
}
```

---

## Content Sanitization Configuration (Backend)

**Library**: HtmlSanitizer.NET (NuGet: `HtmlSanitizer`)

**Configuration** (Program.cs):
```csharp
builder.Services.AddSingleton<IHtmlSanitizer>(sp =>
{
    var sanitizer = new HtmlSanitizer();

    // Allow TipTap formatting tags
    sanitizer.AllowedTags.UnionWith(new[] {
        "p", "br", "strong", "em", "u", "s", "strike",
        "h1", "h2", "h3", "h4", "h5", "h6",
        "ul", "ol", "li", "blockquote",
        "a", "code", "pre"
    });

    // Allow safe attributes
    sanitizer.AllowedAttributes.UnionWith(new[] {
        "href", "title", "class"
    });

    // Allow safe CSS
    sanitizer.AllowedCssProperties.UnionWith(new[] {
        "text-align"
    });

    // Safe URL schemes only
    sanitizer.AllowedSchemes.Clear();
    sanitizer.AllowedSchemes.UnionWith(new[] {
        "http", "https", "mailto"
    });

    return sanitizer;
});
```

---

## Testing Requirements Summary

### Unit Tests

**Backend** (.NET):
- Sanitization: Verify `<script>` removal, allowed tags preserved
- Authorization: Verify 401/403 on unauthorized requests
- Validation: Verify 400 on missing/invalid fields
- Revision creation: Verify revision inserted before update

**Frontend** (Jest + React Testing Library):
- Component rendering: View mode, edit mode, admin-only edit button
- State management: `isDirty` tracking, optimistic update
- Modal behavior: Cancel confirmation

---

### Integration Tests

**Backend** (.NET):
- GET /api/cms/pages/{slug} - Returns page by slug
- PUT /api/cms/pages/{id} - Updates content + creates revision
- GET /api/cms/pages/{id}/revisions - Returns revision history
- Authorization: Admin role required for write operations

---

### E2E Tests

**Playwright**:
- **Happy Path**: Admin edits page → saves → content visible
- **Cancel Workflow**: Edit → cancel → modal → discard/keep editing
- **Error Recovery**: Save fails → rollback → retry
- **Revision History**: Navigate to list → detail → view revisions

---

## Performance Targets (Non-Functional)

| Operation | Target | Maximum Acceptable |
|-----------|--------|-------------------|
| Page Load (GET) | <200ms | <500ms |
| Save Response (PUT) | <500ms | <1000ms |
| UI Update (Optimistic) | <16ms | <50ms |
| Editor Load | <100ms | <200ms |
| Revision History (GET) | <200ms | <500ms |

**Optimization Strategies**:
- PostgreSQL indexes on Slug and ContentPageId
- TanStack Query caching (5-minute stale time)
- Optimistic updates (no network wait for UI)
- Backend sanitization only (no double sanitization)

---

## Security Requirements (Non-Negotiable)

### 1. Authentication

**Pattern**: HttpOnly Cookies via BFF

**Cookie Config**:
- `HttpOnly = true` (JavaScript cannot access)
- `SecurePolicy = Always` (HTTPS only)
- `SameSite = Strict` (CSRF protection)

---

### 2. Authorization

**Backend**: `[Authorize(Roles = "Administrator")]` on all write endpoints

**Frontend**: `hasRole('Administrator')` for UX only (NOT security)

---

### 3. XSS Prevention

**Backend**: HtmlSanitizer.NET on all content BEFORE database write

**Frontend**: `dangerouslySetInnerHTML` with sanitized HTML (no DOMPurify needed)

---

### 4. SQL Injection Prevention

**Entity Framework Core**: All queries parameterized automatically

**NO raw SQL** for CMS operations

---

### 5. CSRF Protection

**Cookie SameSite=Strict**: Cookie only sent to same-origin requests

---

## Integration Points (Existing Systems)

### Authentication System

**Existing**: Cookie-based BFF pattern with ASP.NET Core Identity

**Integration**:
- CMS endpoints read cookie automatically
- `ClaimsPrincipal` provides user ID and roles
- Frontend `useAuth` hook exposes `hasRole('Administrator')`

---

### Admin Dashboard

**Add Navigation Link**:
```typescript
<NavLink
  component={Link}
  to="/admin/cms/revisions"
  label="CMS Revision History"
  leftSection={<IconHistory />}
/>
```

---

### Existing Components

**MantineTiptapEditor** (already integrated):
- File: `/apps/web/src/components/forms/MantineTiptapEditor.tsx`
- Use WITHOUT variable insertion (CMS doesn't need event variables)

**Mantine Components**: Button, TextInput, Modal, Alert, Loader, Notifications, Table, etc.

---

## Edge Cases to Handle

### 1. Very Long Content (>10,000 characters)

**Backend**: PostgreSQL TEXT column supports up to 1GB (no size limit needed)

**Frontend**: TipTap handles long content efficiently, `max-height` with scroll

---

### 2. Network Failure During Save

**Behavior**:
- Optimistic update shows new content immediately
- After 30s timeout: Trigger `onError`
- Rollback cache, show error notification
- Preserve edits in editor, allow retry

---

### 3. Concurrent Editing (MVP Approach)

**Strategy**: Last-write-wins (no locking)

**Future**: Add "Last edited by [user] [time] ago" warning in Phase 2

---

### 4. Browser Back Button with Unsaved Changes

**Behavior**: `beforeunload` event shows native browser warning

**Future**: Custom modal with React Router `useBlocker` hook

---

## Known Limitations (Explicitly Out of Scope)

### NOT in MVP (Future Enhancements):

1. **Image Upload**: Text-only for MVP, Phase 2 with DigitalOcean Spaces
2. **Draft/Published Workflow**: All saves are immediate publish
3. **Rollback/Restore**: Revision history viewable, restore in Phase 2
4. **SEO Metadata**: No custom meta tags for MVP
5. **Content Scheduling**: No publish dates
6. **Multi-Language**: English only

---

## Implementation Priorities (Parallel Work)

### Phase 3A: Backend Implementation (Backend Developer)

**Priority 1** (Core Functionality):
1. Database schema: `cms.ContentPages` and `cms.ContentRevisions` tables
2. Entity models: `ContentPage`, `ContentRevision` classes
3. Minimal API endpoints: GET/PUT for pages, GET for revisions
4. HtmlSanitizer.NET configuration

**Priority 2** (Admin Features):
1. GET /api/cms/pages (list all pages)
2. GET /api/cms/pages/{id}/revisions (revision history)

**Priority 3** (Testing):
1. Integration tests for all endpoints
2. Unit tests for sanitization

---

### Phase 3B: Frontend Implementation (React Developer)

**Priority 1** (Core Editing):
1. `CmsPage` component (view/edit modes)
2. `useCmsPage` hook (fetch + optimistic update)
3. `CmsEditButton` component (responsive FAB)
4. Route definitions for 3 pages

**Priority 2** (UX Enhancements):
1. `CmsCancelModal` component
2. Browser `beforeunload` warning
3. Loading states, error handling

**Priority 3** (Admin Features):
1. `CmsRevisionListPage` component
2. `CmsRevisionDetailPage` component
3. `CmsRevisionCard` component
4. `useCmsRevisions` and `useCmsPageList` hooks

---

### Phase 3C: Testing (Test Developer)

**After Priority 1 Complete**:
1. E2E happy path: Edit → Save → View
2. Unit tests: Component rendering, state management

**After Priority 2 Complete**:
1. E2E cancel workflow
2. E2E error recovery
3. Integration tests: API endpoints

**After Priority 3 Complete**:
1. E2E revision history navigation
2. Mobile testing (FAB, touch controls)
3. Accessibility testing (keyboard, screen readers)

---

## Questions for Implementation Team

### Backend Developer:

- [ ] Confirm database schema location: `cms` schema or existing `public` schema?
- [ ] Entity Framework migration naming: `AddCmsContentManagement` acceptable?
- [ ] Vertical slice folder: `/apps/api/Features/Cms/` correct?
- [ ] HtmlSanitizer NuGet package version: Use latest stable (v9.0+)?

### React Developer:

- [ ] Confirm MantineTiptapEditor configuration for CMS (no variable insertion)?
- [ ] FAB z-index: `z-index: 100` avoids conflicts with existing UI?
- [ ] TanStack Query version: Confirm v5 patterns compatible?
- [ ] Breadcrumb component: Use existing or create new?

### Test Developer:

- [ ] Playwright test location: `/tests/playwright/cms/` acceptable?
- [ ] Test data: Use existing test accounts (admin@witchcityrope.com)?
- [ ] Mobile testing: Emulated devices or real devices?
- [ ] Performance testing: Separate performance test suite or integrated?

---

## Success Criteria (Definition of Done)

**MVP Feature Complete When**:
- ✅ Administrators can edit all 3 pages (Resources, Contact Us, Private Lessons)
- ✅ Content changes saved to database with revision history
- ✅ Revision history viewable in admin dashboard (list + detail pages)
- ✅ XSS sanitization removes malicious HTML
- ✅ Only Administrators can see/use edit functionality
- ✅ All pages load in <200ms
- ✅ Mobile editing works on phones/tablets (FAB, touch controls)
- ✅ E2E tests verify full editing workflow (happy path, cancel, error recovery)
- ✅ Edit button always visible to admins (not hidden/hover)

---

## Handoff Checklist

**Documents Created**:
- [x] Functional Specification: `/docs/functional-areas/content-management-system/new-work/2025-10-17-cms-implementation/requirements/functional-specification.md`
- [x] Handoff Document: This file

**Prerequisites Met**:
- [x] Business Requirements APPROVED (2025-10-17)
- [x] Technology Research Complete
- [x] UI Design APPROVED (2025-10-17)
- [x] All stakeholder decisions finalized

**Ready for Implementation**:
- [x] API endpoints fully specified
- [x] DTOs defined for NSwag generation
- [x] React components documented with props/state
- [x] TanStack Query hooks specified
- [x] Content sanitization configured
- [x] Error handling scenarios documented
- [x] Performance targets quantified
- [x] Security requirements defined
- [x] Testing requirements complete

**Next Steps**:
1. Backend Developer: Read functional spec → Implement API + database
2. React Developer: Read functional spec → Implement components + hooks
3. Test Developer: Read functional spec → Write tests (after Priority 1 complete)

---

## Contact

**Questions About This Handoff**:
- Functional Spec Questions: Review `/docs/functional-areas/content-management-system/new-work/2025-10-17-cms-implementation/requirements/functional-specification.md`
- Business Questions: Review `/docs/functional-areas/content-management-system/new-work/2025-10-17-cms-implementation/requirements/business-requirements.md`
- UI Questions: Review `/docs/functional-areas/content-management-system/new-work/2025-10-17-cms-implementation/design/ui-design.md`

**Critical Decisions Already Made** (DO NOT re-litigate):
- ✅ Mobile edit button: FAB bottom-right (APPROVED)
- ✅ Cancel confirmation: Mantine Modal (APPROVED)
- ✅ Revision history: Separate page architecture (APPROVED)
- ✅ Backend-only sanitization: HtmlSanitizer.NET (APPROVED)
- ✅ Optimistic updates: TanStack Query pattern (APPROVED)

---

**Handoff Date**: 2025-10-17

**Handoff Status**: ✅ COMPLETE - Ready for Phase 3 Implementation

**Estimated Implementation Time**: 6.5 hours (2 hours backend, 3 hours frontend, 1 hour testing, 0.5 hour integration)

**Go/No-Go**: 🟢 **GO** - All prerequisites met, all decisions finalized, all documentation complete

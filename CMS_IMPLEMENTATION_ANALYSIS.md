# CMS Implementation Analysis - WitchCityRope

**Date**: November 12, 2025  
**Analysis Purpose**: Understand current CMS architecture to plan dynamic routing implementation

---

## Executive Summary

The WitchCityRope CMS currently uses **static, hardcoded routes** for each CMS page. A generic `CmsPage` component handles content display and editing, but each page requires explicit route registration in the router and a dedicated wrapper component. The backend supports dynamic slug-based retrieval, and there is a fully functional admin interface for managing revisions.

**Key Finding**: The infrastructure for dynamic routing is already in place at the backend level. Frontend routing is the only thing preventing a dynamic catch-all implementation.

---

## 1. How CmsPage Component Works

### Component Location
`/home/chad/repos/witchcityrope/apps/web/src/features/cms/components/CmsPage.tsx`

### Data Fetching Pattern
- **Takes Input**: `slug` prop (string)
- **Fetching Method**: Uses `useCmsPage(slug)` hook
- **Hook Implementation**: React Query with React 18
  - Query key: `['cms-page', slug]`
  - Fetch function: `getCmsPageBySlug(slug)`
  - Stale time: 5 minutes
  - Retry policy: 2 retries with 1 second delay

### Component Features
1. **View Mode** (Default)
   - Displays title as H1
   - Renders HTML content via `dangerouslySetInnerHTML`
   - Shows edit button if user is Administrator

2. **Edit Mode** (Admin-only)
   - TextInput for page title
   - MantineTiptapEditor for rich HTML content
   - Dirty state tracking for unsaved changes
   - Browser `beforeunload` event listener to warn of unsaved changes
   - Save/Cancel buttons with disabled/loading states

3. **Admin Editing Flow**
   - Click "Edit" button
   - Modify title or content
   - Click "Save" to call `save()` mutation
   - Optimistic updates show immediately
   - Confirmation modal if canceling with unsaved changes

### Props Interface
```typescript
interface CmsPageProps {
  slug: string;                  // URL slug to fetch page
  defaultTitle?: string;         // Fallback title if page not found
  defaultContent?: string;       // Fallback content if page not found
}
```

### Example Usage Pattern
```typescript
export const AboutUsPage: React.FC = () => {
  return <CmsPage slug="about-us" defaultTitle="About WitchCityRope" />;
};
```

---

## 2. CMS API Endpoint Structure

### Base URL
`/api/cms`

### Endpoints

#### 1. GET `/api/cms/pages/{slug}` - Fetch Page by Slug
- **Access**: PUBLIC (no authentication required)
- **Purpose**: Retrieve published CMS page content by URL slug
- **Response**: `ContentPageDto`
```json
{
  "id": 1,
  "slug": "about-us",
  "title": "About WitchCityRope",
  "content": "<h2>Our Story</h2>...",
  "updatedAt": "2025-11-12T15:30:00Z",
  "lastModifiedBy": "admin@example.com",
  "isPublished": true
}
```
- **Error Responses**:
  - 404: Page not found or not published
  - No authentication/role restrictions

#### 2. PUT `/api/cms/pages/{id}` - Update Page Content
- **Access**: ADMIN ONLY (requires Administrator role)
- **Purpose**: Update page title/content and automatically create revision
- **Request Body**: `UpdateContentPageRequest`
```json
{
  "title": "Updated Title",
  "content": "<h2>New Content</h2>...",
  "changeDescription": "Updated sections"
}
```
- **Response**: `ContentPageDto` (updated page)
- **Error Responses**:
  - 400: Validation error (empty content after sanitization)
  - 401: Not authenticated
  - 403: Not administrator
  - 404: Page not found

#### 3. GET `/api/cms/pages/{id:int}/revisions` - Fetch Revision History
- **Access**: ADMIN ONLY (requires Administrator role)
- **Purpose**: Get all revisions for a page (up to 50 most recent)
- **Response**: List of `ContentRevisionDto`
```json
[
  {
    "id": 42,
    "contentPageId": 1,
    "title": "Old Title",
    "contentPreview": "First 200 chars of old content...",
    "fullContent": null,
    "createdAt": "2025-11-10T10:00:00Z",
    "createdBy": "admin@example.com",
    "changeDescription": "Updated sections"
  }
]
```
- **Error Responses**:
  - 401: Not authenticated
  - 403: Not administrator
  - 404: Page not found

#### 4. GET `/api/cms/pages` - List All Pages
- **Access**: ADMIN ONLY (requires Administrator role)
- **Purpose**: Get summary of all CMS pages for admin dashboard
- **Response**: List of `CmsPageSummaryDto`
```json
[
  {
    "id": 1,
    "slug": "about-us",
    "title": "About Us",
    "revisionCount": 5,
    "updatedAt": "2025-11-12T15:30:00Z",
    "lastModifiedBy": "admin@example.com",
    "isPublished": true
  }
]
```
- **Error Responses**:
  - 401: Not authenticated
  - 403: Not administrator

### API Implementation Details
**File**: `/home/chad/repos/witchcityrope/apps/api/Features/Cms/CmsEndpoints.cs`

- All endpoints use EntityFramework with efficient queries
- Content sanitization happens server-side before database writes (XSS prevention)
- Revisions are created automatically on update (domain method pattern)
- Slug-based lookup supports public page display (pages must be published)

---

## 3. Current Static Routes in Router

### File Location
`/home/chad/repos/witchcityrope/apps/web/src/routes/router.tsx`

### Hardcoded CMS Routes (Lines 120-165)
All these routes are explicitly registered and map to individual page components:

| Route Path | Component | Slug | Wrapper Method |
|-----------|-----------|------|-----------------|
| `/resources` | `ResourcesPage` | resources | Direct CmsPage call |
| `/contact-us` | `ContactUsPage` | contact-us | Direct CmsPage call |
| `/private-lessons` | `PrivateLessonsPage` | private-lessons | Direct CmsPage call |
| `/about-us` | `AboutUsPage` | about-us | Direct CmsPage call |
| `/code-of-conduct` | `CodeOfConductPage` | code-of-conduct | Direct CmsPage call |
| `/privacy-policy` | `PrivacyPolicyPage` | privacy-policy | Direct CmsPage call |
| `/terms-of-service` | `TermsOfServicePage` | terms-of-service | Direct CmsPage call |
| `/refund-policy` | `RefundPolicyPage` | refund-policy | Direct CmsPage call |
| `/faq` | `FaqPage` | faq | Direct CmsPage call |
| `/cms/getting-started` | `GettingStartedPage` | cms/getting-started | Direct CmsPage call |
| `/event-waiver` | `EventWaiverPage` | event-waiver | Direct CmsPage call |

**Total**: 11 CMS pages currently hardcoded

### Page Wrapper Pattern
Each page uses identical pattern:
```typescript
export const AboutUsPage: React.FC = () => {
  return <CmsPage slug="about-us" defaultTitle="About WitchCityRope" />;
};
```

---

## 4. Static Routes That Would Conflict with Catch-All

### Critical Conflicts
If a catch-all route like `/:slug` is added, these routes MUST be registered BEFORE the catch-all to work:

#### High Priority (Non-CMS routes that must be preserved)
- `/` (home)
- `/login`
- `/register`
- `/unauthorized`
- `/events` (events list)
- `/events/:id` (event detail)
- `/safety/report`
- `/safety/status`
- `/join`
- `/vetting/apply`
- `/checkout/:eventId`
- `/checkout/:eventId/:registrationId`
- `/events/:eventId/payment/:registrationId`
- `/payment/success`
- `/payment/cancel`
- `/dashboard` (protected)
- `/dashboard/profile-settings` (protected)
- `/my-reports` (protected)
- `/my-reports/:id` (protected)
- `/admin/*` (all admin routes - protected)
- `/form-test`, `/mantine-forms`, etc. (test routes)

#### Potential CMS Routes Needing Explicit Registration
If the catch-all is implemented, these CMS routes should ideally be removed (unless they need special handling):
- `/resources`
- `/contact-us`
- `/private-lessons`
- `/about-us`
- `/code-of-conduct`
- `/privacy-policy`
- `/terms-of-service`
- `/refund-policy`
- `/faq`
- `/cms/getting-started` (note: starts with `/cms/` - conflicts with `/cms/:slug` pattern)
- `/event-waiver`

### Recommendation
The safest approach is a **two-tier catch-all**:
1. Explicit routes for all non-CMS pages and admin routes (as currently configured)
2. Routes for nested CMS paths like `/cms/:slug` (for pages like "getting-started")
3. Final catch-all for top-level CMS pages like `/:slug`

This preserves all existing functionality while enabling dynamic routing.

---

## 5. Admin Interface for Creating CMS Pages

### Current Admin Interfaces

#### 1. Revision History Viewer
**Route**: `/admin/cms/revisions`  
**Component**: `CmsRevisionListPage`  
**Location**: `/home/chad/repos/witchcityrope/apps/web/src/features/cms/pages/CmsRevisionListPage.tsx`

**Features**:
- Lists all CMS pages in a table
- Shows: Title, Slug, Total Revisions, Last Edited Date, Last Editor
- Clickable rows navigate to detail page
- Admin-only (protected by `adminLoader`)

#### 2. Revision Detail Viewer
**Route**: `/admin/cms/revisions/:pageId`  
**Component**: `CmsRevisionDetailPage`  
**Location**: `/home/chad/repos/witchcityrope/apps/web/src/features/cms/pages/CmsRevisionDetailPage.tsx`

**Features**:
- Shows revision history for specific page
- Allows admin to view old versions
- Likely allows reverting to previous versions

#### 3. Inline Page Editing
**Available On**: Any CmsPage-based route (when logged in as admin)  
**Trigger**: "Edit" button appears in top-right of page

**Features**:
- Click "Edit" to enter edit mode
- Title field and rich HTML editor
- Save creates automatic revision with old content preserved
- Cancel with unsaved changes shows confirmation

### Missing Functionality
**No "Create New CMS Page" interface exists**

Current setup assumes:
1. New CMS pages are created via database seed data or migrations
2. Frontend developers manually add routes
3. Or they might be created through some undocumented admin flow

**Investigation Needed**: Check if there's an admin endpoint to create new pages or if pages are only created via migrations.

### Seed Data
**File**: `/home/chad/repos/witchcityrope/apps/api/Features/Cms/CmsSeedData.cs`  
(Content truncated due to file size, but indicates seed data is used to populate initial CMS pages)

---

## 6. Database Schema

### ContentPage Entity
**Location**: `/home/chad/repos/witchcityrope/apps/api/Features/Cms/Entities/ContentPage.cs`  
**Database Table**: `cms.ContentPages`

**Fields**:
```csharp
public int Id { get; set; }                           // PK
public string Slug { get; set; }                      // Max 100 chars, REQUIRED, Unique index likely
public string Title { get; set; }                     // Max 200 chars, REQUIRED
public string Content { get; set; }                   // HTML content, REQUIRED
public DateTime CreatedAt { get; set; }               // Audit
public DateTime UpdatedAt { get; set; }               // Audit
public Guid CreatedBy { get; set; }                   // FK to User
public Guid LastModifiedBy { get; set; }              // FK to User
public bool IsPublished { get; set; }                 // Default true
public ICollection<ContentRevision> Revisions { get; set; }
```

**Key Properties**:
- Slug is the unique identifier for public lookups
- IsPublished flag controls visibility (public API only returns published pages)
- All pages are audited with creator and modifier info

### ContentRevision Entity
**Location**: `/home/chad/repos/witchcityrope/apps/api/Features/Cms/Entities/ContentRevision.cs`  
**Database Table**: `cms.ContentRevisions`

**Purpose**: Stores historical versions of page content  
**Triggered**: Automatically on update via `ContentPage.UpdateContent()` domain method

---

## 7. Type Safety & Auto-Generated Types

### Type Generation Strategy
**Documentation**: `/docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md`  
**Package**: `@witchcityrope/shared-types`  
**Generator**: NSwag (OpenAPI spec → TypeScript)

### CMS DTOs Generated
```typescript
// All imported from auto-generated types:
export type ContentPageDto = components['schemas']['ContentPageDto'];
export type CmsPageSummaryDto = components['schemas']['CmsPageSummaryDto'];
export type UpdateContentPageRequest = components['schemas']['UpdateContentPageRequest'];
export type ContentRevisionDto = components['schemas']['ContentRevisionDto'];
```

**Location**: `/home/chad/repos/witchcityrope/apps/web/src/features/cms/types.ts`

**Critical Rule** (from DTO Alignment Strategy):
- API DTOs (C#) are the SOURCE OF TRUTH
- Never manually create TypeScript interfaces for API response data
- Type mismatches cause silent failures (e.g., `registeredCount` vs `registrationCount`)

---

## 8. Constraints & Dependencies

### Backend Constraints
1. **Slug as Unique Identifier**: Pages are looked up by slug, not ID in public API
2. **Published Flag Required**: Only `IsPublished = true` pages are returned in public queries
3. **Content Sanitization**: XSS prevention happens server-side before database write
4. **Revision Tracking**: Automatic on update, not reversible from API (no restore endpoint)

### Frontend Constraints
1. **React Query Cache**: Using slug as cache key - changing slug in URL changes cache key
2. **Optimistic Updates**: UI updates immediately, rolls back on error
3. **Editor Dependencies**: MantineTiptapEditor (rich HTML editing)
4. **Auth State**: useUser hook from authStore, role-based access

### Routing Constraints
1. **Parameterized Routes**: React Router v7 follows specific path matching order
2. **Route Specificity**: More specific routes must come before less specific ones
3. **No Current Catch-All**: No wildcard `/*` route exists currently
4. **Admin Loader**: Protected routes use `adminLoader` for authorization

---

## 9. Implementation Readiness Assessment

### What's Ready for Dynamic Routing
✅ Backend supports full dynamic slug-based retrieval  
✅ Public API endpoint is generic (handles any slug)  
✅ CmsPage component accepts slug as prop (reusable)  
✅ Hook pattern supports flexible data fetching  
✅ Type system is auto-generated (will auto-update)  
✅ Admin interface exists for managing revisions  

### What Needs Implementation
❌ Dynamic catch-all route in React Router  
❌ Dynamic page creation interface (currently missing)  
❌ Slug validation/sanitization on frontend  
❌ Navigation updates (menu/nav bar to link to CMS pages)  
❌ Tests for dynamic routing  
❌ Documentation of new route structure  

### Recommended Implementation Order
1. **Phase 1**: Add catch-all route to React Router (non-breaking)
2. **Phase 2**: Test with existing CMS pages (verify backward compatibility)
3. **Phase 3**: Create admin interface to create/delete CMS pages
4. **Phase 4**: Update navigation/menus to support dynamic pages
5. **Phase 5**: Remove hardcoded page wrappers (cleanup)
6. **Phase 6**: Add comprehensive tests

---

## Summary Table

| Aspect | Status | Location | Notes |
|--------|--------|----------|-------|
| **Generic CmsPage Component** | ✅ Exists | `/features/cms/components/CmsPage.tsx` | Accepts slug prop, handles all view/edit logic |
| **Slug-Based Data Fetching** | ✅ Implemented | `useCmsPage()` hook | Uses React Query, supports caching |
| **Backend API Endpoint** | ✅ Ready | `GET /api/cms/pages/{slug}` | Public, no auth required for read |
| **Admin Editing** | ✅ Implemented | Inline edit + revision history | Automatic revision creation |
| **Type Safety** | ✅ Auto-Generated | `@witchcityrope/shared-types` | NSwag-generated, DTO-aligned |
| **Dynamic Routes** | ❌ Not Implemented | Need `/:[slug]` route | Currently hardcoded routes |
| **Page Creation UI** | ❌ Not Implemented | Missing | No UI to create new pages |
| **Route Conflicts** | ⚠️ Analyze Required | `/events/:id`, `/my-reports/:id`, etc. | 11 static CMS routes + many system routes |
| **Catch-All Pattern** | ❌ Not Implemented | Router config | Need tiered approach to avoid conflicts |

---

## Recommended Path Forward

### Immediate Next Steps
1. **Route Design Decision**: Choose between:
   - Option A: Nested `/cms/:slug` (safer, separates CMS pages)
   - Option B: Root-level `/:slug` (cleaner URLs, requires careful ordering)
   - Option C: Hybrid (keep some hardcoded, add catch-all for overflow)

2. **Backward Compatibility Plan**: Decide if existing routes stay:
   - Keep all hardcoded routes while adding catch-all
   - Or remove hardcoded routes and add catch-all only

3. **Admin UI Requirements**: Design interface to:
   - Create new CMS page (with slug, title, initial content)
   - Delete CMS page
   - List all pages
   - Edit existing pages

4. **Testing Strategy**:
   - E2E tests for dynamic route navigation
   - Admin tests for create/delete workflows
   - Performance tests with many pages

---

## Files Not Shown (Truncated)

Due to token limits, these files exist but weren't fully examined:
- `/home/chad/repos/witchcityrope/apps/api/Features/Cms/CmsSeedData.cs` (27KB)
- `/home/chad/repos/witchcityrope/apps/web/src/features/cms/pages/CmsRevisionDetailPage.tsx`
- Various hook implementations and test files

Contact the analysis requester if these need deeper examination.

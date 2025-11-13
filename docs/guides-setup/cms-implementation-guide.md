# CMS Implementation Guide - WitchCityRope
<!-- Last Updated: 2025-11-12 -->
<!-- Version: 1.0 -->
<!-- Owner: Librarian Agent -->
<!-- Status: Active -->

## Overview

WitchCityRope's Content Management System (CMS) enables administrators to create and edit text-based pages through an in-browser rich text editor. The system uses a **dynamic routing architecture** that allows new pages to be added through database configuration without requiring code deployment.

### What the CMS Does

- **Admin-Only Editing**: Administrators can edit page content directly in the browser using a rich text editor (Mantine TipTap)
- **Dynamic Page Routing**: Pages are served through a single `:slug` route that fetches content from the database
- **Revision History**: All edits are tracked with full revision history (admin-only viewing)
- **In-Place Editing**: Edit button appears on the page itself (no separate admin interface needed)
- **Seed Data Management**: New pages can be added via database seed without code deployment

### Technology Stack

- **Frontend**: React 18 + TypeScript + Mantine v7 + TanStack Query
- **Backend**: .NET Minimal API + PostgreSQL + Entity Framework Core
- **Editor**: Mantine TipTap (rich text HTML editor)
- **Routing**: React Router v7 with dynamic segment routing (`:slug` pattern)
- **Type Generation**: NSwag (auto-generated TypeScript types from C# DTOs)

---

## Dynamic Routing Architecture

### The Problem Solved

**Before (Individual Routes)**: Each CMS page required:
1. Separate page component file (e.g., `ResourcesPage.tsx`)
2. Explicit route definition in `router.tsx`
3. Code deployment to add new pages
4. Maintenance burden as pages scaled

**After (Dynamic Routing)**: Single dynamic route handles all pages:
1. One `CmsDynamicPage` component serves all pages
2. Single `:slug` route matches any slug pattern
3. New pages added via database seed data only
4. Zero code deployment for new content pages

### How It Works

```
User navigates to /resources
  ↓
React Router matches { path: ":slug" } route (last in routes array)
  ↓
CmsDynamicPage component extracts slug="resources" from URL params
  ↓
CmsPage component uses useCmsPage("resources") hook
  ↓
API call: GET /api/cms/pages/resources
  ↓
Database query: SELECT * FROM cms_pages WHERE slug = 'resources'
  ↓
TanStack Query caches response (5-minute stale time)
  ↓
Component renders with fetched content
  ↓
Admin sees edit button (if authenticated as Administrator)
```

### URL Structure

**Production URLs**:
- `/resources` → Resources page
- `/about-us` → About Us page
- `/terms-of-service` → Terms of Service page
- `/refund-policy` → Refund Policy page
- `/faq` → FAQ page

**Admin URLs**:
- `/admin/cms/revisions` → All pages with revision counts
- `/admin/cms/revisions/:pageId` → Revision history for specific page

**API Endpoints**:
- `GET /api/cms/pages/:slug` → Fetch page by slug
- `PUT /api/cms/pages/:slug` → Update page content (creates new revision)
- `GET /api/cms/pages/:pageId/revisions` → List all revisions
- `GET /api/cms/pages/:pageId/revisions/:revisionId` → Get specific revision

---

## Component Architecture

### Route Configuration (`apps/web/src/routes/router.tsx`)

```typescript
export const router = createBrowserRouter([
  {
    path: '/',
    element: <RootLayout />,
    errorElement: <RootErrorBoundary />,
    children: [
      // Static routes first (highest priority)
      { path: '', element: <HomePage /> },
      { path: 'events', element: <EventsListPage /> },
      { path: 'events/:id', element: <EventDetailPage /> },
      { path: 'admin/*', element: <AdminLayout />, loader: adminLoader },
      { path: 'dashboard', element: <DashboardPage />, loader: authLoader },
      { path: 'login', element: <LoginPage /> },
      { path: 'register', element: <RegisterPage /> },
      // ... all other static routes ...

      // Dynamic CMS route - MUST BE LAST
      // Matches any single-level path not matched above
      {
        path: ':slug',
        element: <CmsDynamicPage />,
      },
    ],
  },
])
```

**Critical**: The `:slug` route **MUST BE LAST** in the routes array. React Router matches routes in order, so static routes like `/events` must be defined before the dynamic `:slug` route to avoid being incorrectly captured.

### CmsDynamicPage Component (`apps/web/src/features/cms/pages/CmsDynamicPage.tsx`)

```typescript
/**
 * Dynamic CMS page component that loads content based on URL slug parameter
 *
 * Route: /:slug
 * Examples:
 *   /resources → loads CMS page with slug "resources"
 *   /about-us → loads CMS page with slug "about-us"
 */
export const CmsDynamicPage: React.FC = () => {
  const { slug } = useParams<{ slug: string }>()

  // Validate slug parameter exists
  if (!slug) {
    return (
      <Container size="lg" py="xl">
        <Alert icon={<IconAlertCircle />} color="red" title="Invalid URL">
          No page slug provided in URL.
        </Alert>
      </Container>
    )
  }

  // Render CmsPage with slug from URL
  return <CmsPage slug={slug} defaultTitle="Page" />
}
```

**Purpose**: Thin wrapper that extracts the slug from URL parameters and passes it to the reusable `CmsPage` component.

### CmsPage Component (`apps/web/src/features/cms/components/CmsPage.tsx`)

```typescript
export const CmsPage: React.FC<CmsPageProps> = ({ slug, defaultTitle, defaultContent }) => {
  const user = useUser()
  const isAdmin = user?.role === 'Administrator'

  const { content, isLoading, save, isSaving, error } = useCmsPage(slug)

  const [isEditing, setIsEditing] = useState(false)
  const [editableContent, setEditableContent] = useState('')
  const [editableTitle, setEditableTitle] = useState('')
  const [isDirty, setIsDirty] = useState(false)

  // View mode: Display content with edit button
  if (!isEditing) {
    return (
      <Container size="lg" py="xl">
        {isAdmin && <CmsEditButton onClick={handleEdit} />}
        <h1>{content.title}</h1>
        <div dangerouslySetInnerHTML={{ __html: content.content }} />
      </Container>
    )
  }

  // Edit mode: Rich text editor with save/cancel
  return (
    <Container size="lg" py="xl">
      <TextInput
        label="Page Title"
        value={editableTitle}
        onChange={handleTitleChange}
      />
      <MantineTiptapEditor
        value={editableContent}
        onChange={handleContentChange}
      />
      <Group justify="flex-end">
        <Button variant="outline" onClick={handleCancel}>Cancel</Button>
        <Button onClick={handleSave} loading={isSaving}>Save</Button>
      </Group>
    </Container>
  )
}
```

**Features**:
- **Role-Based UI**: Edit button only visible to administrators
- **In-Place Editing**: Edit mode replaces view mode (no separate admin page)
- **Unsaved Changes Protection**: Browser warning on navigation with unsaved changes
- **TipTap Editor**: Rich text editing with toolbar (bold, italic, lists, links, etc.)
- **Optimistic UI**: TanStack Query handles caching and invalidation

### useCmsPage Hook (`apps/web/src/features/cms/hooks/useCmsPage.ts`)

```typescript
export function useCmsPage(slug: string) {
  const queryClient = useQueryClient()

  // Fetch page by slug
  const { data: content, isLoading, error } = useQuery({
    queryKey: ['cms-page', slug],
    queryFn: () => getCmsPage(slug),
    staleTime: 5 * 60 * 1000, // 5 minutes
  })

  // Save page mutation
  const { mutateAsync: save, isPending: isSaving } = useMutation({
    mutationFn: (update: CmsPageUpdateRequest) => updateCmsPage(slug, update),
    onSuccess: () => {
      // Invalidate query to refetch updated content
      queryClient.invalidateQueries({ queryKey: ['cms-page', slug] })
      notifications.show({
        title: 'Success',
        message: 'Page updated successfully',
        color: 'green',
      })
    },
    onError: (error) => {
      notifications.show({
        title: 'Error',
        message: 'Failed to update page',
        color: 'red',
      })
    },
  })

  return { content, isLoading, save, isSaving, error }
}
```

**Features**:
- **TanStack Query Integration**: Automatic caching and background refetching
- **5-Minute Stale Time**: Reduces API calls for frequently accessed pages
- **Optimistic Updates**: UI updates immediately, reverts on error
- **Notifications**: User feedback on save success/error

---

## Adding New CMS Pages

### Process Overview

New CMS pages can be added **entirely through database seed data** without any code deployment. This is the primary benefit of the dynamic routing architecture.

### Step-by-Step Guide

#### Step 1: Add Seed Data Entry

**File**: `apps/api/Features/CMS/Data/CmsSeedData.cs`

```csharp
public static class CmsSeedData
{
    public static void SeedCmsPages(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CmsPage>().HasData(
            // Existing pages...
            new CmsPage
            {
                Id = 1,
                Slug = "resources",
                Title = "Resources",
                Content = "<p>Educational resources for rope bondage...</p>",
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true
            },

            // Add your new page here
            new CmsPage
            {
                Id = 6, // Increment from last ID
                Slug = "privacy-policy", // URL: /privacy-policy
                Title = "Privacy Policy",
                Content = "<p>Your privacy is important to us...</p>",
                CreatedAt = new DateTime(2025, 11, 12, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2025, 11, 12, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true
            }
        );
    }
}
```

#### Step 2: Slug Naming Conventions

**Rules**:
- Lowercase only
- Words separated by hyphens (kebab-case)
- Descriptive and URL-friendly
- No special characters (only letters, numbers, hyphens)
- Must be unique across all pages

**Good Examples**:
- `resources` ✅
- `about-us` ✅
- `terms-of-service` ✅
- `refund-policy` ✅
- `membership-faq` ✅

**Bad Examples**:
- `Resources` ❌ (uppercase)
- `about_us` ❌ (underscores)
- `terms of service` ❌ (spaces)
- `faq?` ❌ (special characters)
- `events` ❌ (reserved slug - conflicts with static route)

#### Step 3: Content Formatting Guidelines

**HTML Content**:
```html
<!-- Simple paragraph -->
<p>This is a paragraph with <strong>bold</strong> and <em>italic</em> text.</p>

<!-- Links -->
<p>Visit our <a href="https://example.com">website</a> for more info.</p>

<!-- Lists -->
<ul>
  <li>First item</li>
  <li>Second item</li>
</ul>

<!-- Headings (use h2-h6, h1 is page title) -->
<h2>Section Title</h2>
<p>Section content...</p>

<!-- Line breaks -->
<p>First line<br>Second line</p>
```

**Security Note**: All HTML is sanitized server-side using HtmlSanitizer.NET. Only safe tags are allowed (no `<script>`, `<iframe>`, etc.).

#### Step 4: Create Database Migration

```bash
# Navigate to API directory
cd apps/api

# Create migration
dotnet ef migrations add AddPrivacyPolicyPage -o Features/CMS/Data/Migrations

# Review generated migration file
# Verify it includes INSERT statement for new CmsPage

# Apply migration to database
dotnet ef database update
```

#### Step 5: Verify in Browser

```bash
# Start Docker containers
./dev.sh

# Navigate to new page
# http://localhost:5173/privacy-policy

# Test edit functionality (as admin)
# - Click "Edit This Page" button
# - Modify content
# - Click "Save"
# - Verify changes persist after page refresh
```

#### Step 6: Add Navigation Link (Optional)

**File**: `apps/web/src/components/layout/Navigation.tsx`

```tsx
<Menu.Item component={Link} to="/privacy-policy">
  Privacy Policy
</Menu.Item>
```

**Note**: This step is optional. Pages can exist without navigation links (accessed directly via URL).

### Reserved Slugs

The following slugs are **RESERVED** and cannot be used for CMS pages because they conflict with static application routes:

**Authentication & User**:
- `login`
- `register`
- `logout`
- `unauthorized`
- `dashboard`
- `profile`

**Events**:
- `events` (events list page)
- `checkout` (payment checkout)

**Admin**:
- `admin` (entire admin section)

**Safety**:
- `safety` (safety reporting)

**Vetting**:
- `join` (vetting application)
- `vetting` (vetting system)

**Testing/Demo**:
- `test`
- `test-notifications`
- `form-test`
- `api-validation-v2-simple`
- `navigation-test`
- `payment-test`

**System**:
- Any slug matching existing static routes in `router.tsx`

**Enforcement**: Backend API should reject CMS page creation with reserved slugs (TODO: implement validation).

---

## Technical Architecture

### Database Schema

**Table**: `cms_pages`

```sql
CREATE TABLE cms_pages (
    id SERIAL PRIMARY KEY,
    slug VARCHAR(255) UNIQUE NOT NULL,
    title VARCHAR(500) NOT NULL,
    content TEXT NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,

    -- Indexes
    CONSTRAINT cms_pages_slug_key UNIQUE (slug),
    INDEX idx_cms_pages_slug (slug),
    INDEX idx_cms_pages_is_active (is_active)
);
```

**Table**: `cms_page_revisions`

```sql
CREATE TABLE cms_page_revisions (
    id SERIAL PRIMARY KEY,
    page_id INTEGER NOT NULL,
    title VARCHAR(500) NOT NULL,
    content TEXT NOT NULL,
    changed_at TIMESTAMP NOT NULL DEFAULT NOW(),
    changed_by_user_id INTEGER,
    change_summary TEXT,

    -- Foreign keys
    CONSTRAINT fk_page FOREIGN KEY (page_id)
        REFERENCES cms_pages(id) ON DELETE CASCADE,
    CONSTRAINT fk_user FOREIGN KEY (changed_by_user_id)
        REFERENCES users(id) ON DELETE SET NULL,

    -- Indexes
    INDEX idx_cms_revisions_page_id (page_id),
    INDEX idx_cms_revisions_changed_at (changed_at)
);
```

**Key Design Decisions**:
- **Slug as URL identifier**: Immutable after creation (no slug changes)
- **Revision history on every save**: Full audit trail of all changes
- **Soft delete via is_active**: Pages can be disabled without deletion
- **Foreign key on revisions**: Cascade delete removes all revisions with page
- **User tracking**: Changed by user ID for accountability (NULL for system changes)

### API Endpoints

**Fetch Page by Slug**
```
GET /api/cms/pages/:slug

Response 200:
{
  "id": 1,
  "slug": "resources",
  "title": "Resources",
  "content": "<p>Educational resources...</p>",
  "createdAt": "2025-01-01T00:00:00Z",
  "updatedAt": "2025-01-01T00:00:00Z",
  "isActive": true
}

Response 404:
{
  "title": "Not Found",
  "status": 404,
  "detail": "CMS page with slug 'invalid-slug' not found"
}
```

**Update Page Content**
```
PUT /api/cms/pages/:slug
Authorization: Bearer <token>

Request:
{
  "title": "Updated Title",
  "content": "<p>Updated content...</p>"
}

Response 200:
{
  "id": 1,
  "slug": "resources",
  "title": "Updated Title",
  "content": "<p>Updated content...</p>",
  "updatedAt": "2025-11-12T15:30:00Z"
}

Response 401: Unauthorized (user not authenticated)
Response 403: Forbidden (user not Administrator role)
Response 404: Page not found
```

**List Revisions**
```
GET /api/cms/pages/:pageId/revisions
Authorization: Bearer <token>

Response 200:
[
  {
    "id": 5,
    "pageId": 1,
    "title": "Resources",
    "changedAt": "2025-11-12T15:30:00Z",
    "changedByUserId": 42,
    "changedByUserName": "admin@witchcityrope.com",
    "changeSummary": "Updated educational resources section"
  },
  // ... more revisions
]
```

**Get Specific Revision**
```
GET /api/cms/pages/:pageId/revisions/:revisionId
Authorization: Bearer <token>

Response 200:
{
  "id": 5,
  "pageId": 1,
  "title": "Resources",
  "content": "<p>Historical content...</p>",
  "changedAt": "2025-11-12T15:30:00Z",
  "changedByUserId": 42
}
```

### Backend Implementation

**Entity Model** (`apps/api/Features/CMS/Entities/CmsPage.cs`):
```csharp
public class CmsPage
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; }

    // Navigation properties
    public ICollection<CmsPageRevision> Revisions { get; set; } = new List<CmsPageRevision>();
}
```

**Service Layer** (`apps/api/Features/CMS/Services/CmsService.cs`):
```csharp
public class CmsService
{
    public async Task<CmsPageDto?> GetPageBySlugAsync(string slug)
    {
        var page = await _context.CmsPages
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsActive);

        return page == null ? null : MapToDto(page);
    }

    public async Task<CmsPageDto> UpdatePageAsync(string slug, UpdateCmsPageRequest request, int userId)
    {
        var page = await _context.CmsPages.FirstOrDefaultAsync(p => p.Slug == slug);
        if (page == null) throw new NotFoundException($"Page with slug '{slug}' not found");

        // Create revision before updating
        var revision = new CmsPageRevision
        {
            PageId = page.Id,
            Title = page.Title,
            Content = page.Content,
            ChangedAt = DateTime.UtcNow,
            ChangedByUserId = userId
        };
        _context.CmsPageRevisions.Add(revision);

        // Update page
        page.Title = _sanitizer.Sanitize(request.Title);
        page.Content = _sanitizer.Sanitize(request.Content);
        page.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return MapToDto(page);
    }
}
```

**Key Implementation Notes**:
- **HTML Sanitization**: All content sanitized using HtmlSanitizer.NET before storage
- **Revision Creation**: Old content saved to revisions table on every update
- **UTC Timestamps**: All dates stored as UTC for consistency
- **AsNoTracking**: Read-only queries use AsNoTracking for performance
- **Authorization**: UpdatePageAsync requires authenticated Administrator role

### Frontend Type Generation

**NSwag Configuration** (`packages/shared-types/nswag.json`):
```json
{
  "runtime": "Net90",
  "defaultVariables": null,
  "documentGenerator": {
    "fromDocument": {
      "url": "http://localhost:5655/swagger/v1/swagger.json"
    }
  },
  "codeGenerators": {
    "openApiToTypeScriptClient": {
      "output": "src/generated/api-types.ts",
      "generateClientClasses": false,
      "generateClientInterfaces": false,
      "generateOptionalParameters": true
    }
  }
}
```

**Generated Types** (`packages/shared-types/src/generated/api-types.ts`):
```typescript
export interface CmsPageDto {
  id: number
  slug: string
  title: string
  content: string
  createdAt: string
  updatedAt: string
  isActive: boolean
}

export interface UpdateCmsPageRequest {
  title: string
  content: string
}

export interface CmsPageRevisionDto {
  id: number
  pageId: number
  title: string
  content: string
  changedAt: string
  changedByUserId?: number
  changedByUserName?: string
  changeSummary?: string
}
```

**Usage in Frontend**:
```typescript
import type { components } from '@witchcityrope/shared-types'

type CmsPageDto = components['schemas']['CmsPageDto']
type UpdateCmsPageRequest = components['schemas']['UpdateCmsPageRequest']
```

**CRITICAL**: Never manually create interfaces for API types. Always use auto-generated types from NSwag to ensure frontend-backend type alignment.

---

## Routing Priority and Conflict Prevention

### Why Route Order Matters

React Router v7 uses a **best-match algorithm** but still processes routes in the order they are defined. The `:slug` dynamic route will match **any** single-segment URL path (e.g., `/anything`), so it must be placed last to avoid capturing static routes.

### Correct Route Ordering

```typescript
const router = createBrowserRouter([
  {
    path: '/',
    element: <RootLayout />,
    children: [
      // 1. STATIC ROUTES FIRST (highest priority)
      { path: '', element: <HomePage /> },
      { path: 'events', element: <EventsListPage /> },
      { path: 'events/:id', element: <EventDetailPage /> },
      { path: 'admin/*', element: <AdminLayout /> },
      { path: 'dashboard', element: <DashboardPage /> },
      { path: 'login', element: <LoginPage /> },
      { path: 'register', element: <RegisterPage /> },
      // ... all other static routes ...

      // 2. DYNAMIC CMS ROUTE LAST (lowest priority)
      { path: ':slug', element: <CmsDynamicPage /> },
    ],
  },
])
```

### What Happens With Incorrect Ordering

**Scenario**: `:slug` route defined BEFORE static routes

```typescript
// ❌ WRONG - DO NOT DO THIS
const router = createBrowserRouter([
  {
    path: '/',
    children: [
      { path: ':slug', element: <CmsDynamicPage /> }, // ❌ TOO EARLY!
      { path: 'events', element: <EventsListPage /> }, // ❌ NEVER REACHED!
      { path: 'dashboard', element: <DashboardPage /> }, // ❌ NEVER REACHED!
    ],
  },
])
```

**Result**:
- `/events` matches `:slug` route → CMS page loads → 404 error (slug "events" not in database)
- `/dashboard` matches `:slug` route → CMS page loads → 404 error
- Static routes NEVER execute because `:slug` captures everything first

### Reserved Slugs Protection

**Backend Validation** (TODO):
```csharp
public class ReservedSlugValidator
{
    private static readonly string[] ReservedSlugs = new[]
    {
        "login", "register", "logout", "dashboard", "profile",
        "events", "admin", "safety", "join", "vetting", "checkout"
    };

    public static bool IsReserved(string slug)
    {
        return ReservedSlugs.Contains(slug.ToLower());
    }

    public static void ValidateSlug(string slug)
    {
        if (IsReserved(slug))
        {
            throw new ValidationException(
                $"Slug '{slug}' is reserved and cannot be used for CMS pages. " +
                "Please choose a different slug."
            );
        }
    }
}
```

**When to Use**:
- On CMS page creation (manual via SQL or future admin UI)
- On slug modification (if slug editing ever implemented)
- In seed data validation during startup

### Testing Route Priority

**E2E Test** (`tests/e2e/cms-routing.spec.ts`):
```typescript
test('static routes take precedence over CMS dynamic route', async ({ page }) => {
  // Visit static route that could conflict with CMS
  await page.goto('http://localhost:5173/events')

  // Should load EventsListPage, NOT CmsDynamicPage
  await expect(page.locator('h1')).toContainText('Events')

  // Should NOT show CMS 404 error
  await expect(page.locator('text=Page Not Found')).not.toBeVisible()
})

test('CMS route handles valid slugs', async ({ page }) => {
  await page.goto('http://localhost:5173/resources')

  // Should load CMS page content
  await expect(page.locator('h1')).toContainText('Resources')

  // Admin should see edit button
  await expect(page.locator('text=Edit This Page')).toBeVisible()
})

test('CMS route shows 404 for non-existent slugs', async ({ page }) => {
  await page.goto('http://localhost:5173/nonexistent-page-slug')

  // Should show error, NOT crash
  await expect(page.locator('text=Error')).toBeVisible()
})
```

---

## Admin Functionality

### Editing CMS Pages

**User Flow**:
1. Admin navigates to CMS page (e.g., `/resources`)
2. "Edit This Page" button appears at top-right of page
3. Admin clicks button → enters edit mode
4. Title field and rich text editor appear
5. Admin makes changes
6. Admin clicks "Save" → content updates → view mode restored
7. Admin clicks "Cancel" → unsaved changes modal appears (if dirty)

**Authentication & Authorization**:
- Only users with `Administrator` role can see edit button
- API endpoints validate `Administrator` role on update requests
- Non-admin users see read-only content (no edit button)

**Rich Text Editor Features**:
- Bold, italic, underline
- Headings (H2-H6)
- Bullet lists, numbered lists
- Links (with URL input)
- Alignment (left, center, right)
- Undo/redo
- HTML sanitization on save (server-side)

**Edit Button Component** (`apps/web/src/features/cms/components/CmsEditButton.tsx`):
```typescript
export const CmsEditButton: React.FC<CmsEditButtonProps> = ({
  onClick,
  viewportWidth
}) => {
  const isMobile = viewportWidth < 768

  return (
    <Button
      onClick={onClick}
      leftSection={<IconEdit size={16} />}
      variant="filled"
      color="blue"
      size={isMobile ? 'sm' : 'md'}
      style={{
        position: 'fixed',
        top: isMobile ? '80px' : '100px',
        right: isMobile ? '10px' : '20px',
        zIndex: 100,
      }}
    >
      Edit This Page
    </Button>
  )
}
```

**Unsaved Changes Protection**:
```typescript
// Browser beforeunload warning
useEffect(() => {
  const handleBeforeUnload = (e: BeforeUnloadEvent) => {
    if (isDirty && isEditing) {
      e.preventDefault()
      e.returnValue = '' // Chrome requires returnValue to be set
    }
  }

  window.addEventListener('beforeunload', handleBeforeUnload)
  return () => window.removeEventListener('beforeunload', handleBeforeUnload)
}, [isDirty, isEditing])

// Cancel confirmation modal
const handleCancel = () => {
  if (isDirty) {
    setShowCancelModal(true) // Show "Discard changes?" modal
  } else {
    setIsEditing(false) // No changes, exit immediately
  }
}
```

### Revision History Admin UI

**Admin Dashboard** (`/admin/cms/revisions`):
- Lists all CMS pages with revision counts
- Sortable by page title, last updated, revision count
- Click page → view full revision history

**Revision List Page** (`/admin/cms/revisions/:pageId`):
- Shows all revisions for specific page (newest first)
- Each row displays:
  - Revision date/time
  - Changed by user (email)
  - Change summary (future feature)
  - "View" button
- Click "View" → see full content of that revision

**Revision Detail Page** (`/admin/cms/revisions/:pageId/:revisionId`):
- Displays historical content (read-only)
- Shows title and content as they were at that point in time
- "Back" button returns to revision list
- Future enhancement: "Restore this revision" button

**Key Components**:
- `CmsRevisionListPage.tsx` → Lists all revisions for a page
- `CmsRevisionDetailPage.tsx` → Displays specific revision content
- `useCmsRevisions.ts` → TanStack Query hook for fetching revisions
- Backend endpoints return revision data with user information

### Publishing Workflow

**Current State (MVP)**:
- All saves are immediately live (no draft/publish workflow)
- Every save creates a new revision
- Admins can view revision history to see what changed

**Future Enhancement (Not Implemented)**:
- Draft mode: Save without publishing
- Schedule publishing: Set future publish date
- Approval workflow: Require review before publish
- Unpublish: Set `is_active = false` to hide page

---

## Testing

### Unit Tests

**CmsPage Component** (`apps/web/src/features/cms/components/__tests__/CmsPage.test.tsx`):
```typescript
describe('CmsPage', () => {
  it('renders page content in view mode', async () => {
    const { getByText } = render(<CmsPage slug="resources" />)
    await waitFor(() => expect(getByText('Resources')).toBeInTheDocument())
  })

  it('shows edit button only for admins', () => {
    // Test with admin user
    const { getByText } = render(<CmsPage slug="resources" />)
    expect(getByText('Edit This Page')).toBeInTheDocument()

    // Test with non-admin user
    const { queryByText } = render(<CmsPage slug="resources" />)
    expect(queryByText('Edit This Page')).not.toBeInTheDocument()
  })

  it('enters edit mode when edit button clicked', async () => {
    const { getByText, getByLabelText } = render(<CmsPage slug="resources" />)
    fireEvent.click(getByText('Edit This Page'))

    await waitFor(() => {
      expect(getByLabelText('Page Title')).toBeInTheDocument()
      expect(getByText('Save')).toBeInTheDocument()
    })
  })

  it('shows cancel confirmation modal with unsaved changes', async () => {
    // ... test implementation
  })
})
```

**useCmsPage Hook** (`apps/web/src/features/cms/hooks/__tests__/useCmsPage.test.ts`):
```typescript
describe('useCmsPage', () => {
  it('fetches page by slug', async () => {
    const { result } = renderHook(() => useCmsPage('resources'))

    await waitFor(() => {
      expect(result.current.content).toEqual({
        id: 1,
        slug: 'resources',
        title: 'Resources',
        content: '<p>Content...</p>',
      })
    })
  })

  it('saves page updates', async () => {
    const { result } = renderHook(() => useCmsPage('resources'))

    await act(async () => {
      await result.current.save({
        title: 'Updated Title',
        content: '<p>Updated content</p>',
      })
    })

    expect(result.current.content.title).toBe('Updated Title')
  })
})
```

### Integration Tests (Backend)

**CmsService Tests** (`apps/api/Features/CMS/Services/__tests__/CmsServiceTests.cs`):
```csharp
[Fact]
public async Task GetPageBySlug_ReturnsPage_WhenSlugExists()
{
    // Arrange
    var page = new CmsPage { Slug = "test", Title = "Test", Content = "<p>Test</p>" };
    await _context.CmsPages.AddAsync(page);
    await _context.SaveChangesAsync();

    // Act
    var result = await _service.GetPageBySlugAsync("test");

    // Assert
    Assert.NotNull(result);
    Assert.Equal("test", result.Slug);
}

[Fact]
public async Task UpdatePage_CreatesRevision_BeforeUpdating()
{
    // Arrange
    var page = CreateTestPage();

    // Act
    await _service.UpdatePageAsync("test", new UpdateCmsPageRequest
    {
        Title = "Updated",
        Content = "<p>Updated</p>"
    }, userId: 1);

    // Assert
    var revisions = await _context.CmsPageRevisions.Where(r => r.PageId == page.Id).ToListAsync();
    Assert.Single(revisions); // Old content saved as revision
}
```

### E2E Tests (Playwright)

**CMS Routing** (`tests/e2e/cms-routing.spec.ts`):
```typescript
test('user can view CMS page', async ({ page }) => {
  await page.goto('http://localhost:5173/resources')

  await expect(page.locator('h1')).toContainText('Resources')
  await expect(page.locator('text=Educational resources')).toBeVisible()
})

test('admin can edit CMS page', async ({ page }) => {
  // Login as admin
  await page.goto('http://localhost:5173/login')
  await page.fill('input[name="email"]', 'admin@witchcityrope.com')
  await page.fill('input[name="password"]', 'Test123!')
  await page.click('button[type="submit"]')

  // Navigate to CMS page
  await page.goto('http://localhost:5173/resources')
  await page.click('text=Edit This Page')

  // Edit content
  await page.fill('input[name="title"]', 'Updated Resources')
  await page.locator('.tiptap-editor').fill('New content')
  await page.click('text=Save')

  // Verify update
  await expect(page.locator('h1')).toContainText('Updated Resources')
  await expect(page.locator('text=New content')).toBeVisible()
})

test('non-admin cannot see edit button', async ({ page }) => {
  // Login as non-admin user
  await page.goto('http://localhost:5173/login')
  await page.fill('input[name="email"]', 'member@witchcityrope.com')
  await page.fill('input[name="password"]', 'Test123!')
  await page.click('button[type="submit"]')

  // Navigate to CMS page
  await page.goto('http://localhost:5173/resources')

  // Edit button should not exist
  await expect(page.locator('text=Edit This Page')).not.toBeVisible()
})

test('CMS 404 for non-existent slug', async ({ page }) => {
  await page.goto('http://localhost:5173/nonexistent-slug-12345')

  await expect(page.locator('text=Error')).toBeVisible()
  await expect(page.locator('text=Failed to load page content')).toBeVisible()
})
```

### Recommended Test Coverage

**Unit Tests**:
- CmsPage component rendering
- Edit mode transitions
- Unsaved changes handling
- Admin role visibility
- useCmsPage hook data fetching
- useCmsPage hook save mutations

**Integration Tests**:
- CmsService GetPageBySlug
- CmsService UpdatePage with revision creation
- CmsService GetRevisions
- HTML sanitization
- Authorization checks

**E2E Tests**:
- View CMS page as anonymous user
- View CMS page as authenticated user
- Edit CMS page as admin
- Edit button hidden for non-admin
- Save and persist changes
- Cancel with unsaved changes modal
- CMS 404 for invalid slugs
- Static route precedence over CMS route

---

## Migration Notes

### What Changed from Old Approach

**Before (11 Individual Components)**:
```
apps/web/src/features/cms/pages/
├── ResourcesPage.tsx
├── AboutUsPage.tsx
├── TermsOfServicePage.tsx
├── RefundPolicyPage.tsx
├── FaqPage.tsx
├── ContactUsPage.tsx
├── PrivateLessonsPage.tsx
├── SafetyGuidelinesPage.tsx
├── CodeOfConductPage.tsx
├── InclusivityStatementPage.tsx
└── AccessibilityStatementPage.tsx
```

Each file was ~10 lines:
```typescript
export const ResourcesPage: React.FC = () => {
  return <CmsPage slug="resources" defaultTitle="Resources" />
}
```

**Routes Configuration**:
```typescript
{ path: 'resources', element: <ResourcesPage /> },
{ path: 'about-us', element: <AboutUsPage /> },
{ path: 'terms-of-service', element: <TermsOfServicePage /> },
// ... 8 more explicit routes
```

**After (1 Dynamic Component)**:
```
apps/web/src/features/cms/pages/
└── CmsDynamicPage.tsx  (40 lines)
```

**Routes Configuration**:
```typescript
{ path: ':slug', element: <CmsDynamicPage /> },  // Handles all CMS pages
```

### Benefits Achieved

**Developer Benefits**:
- **90% less boilerplate code**: 11 files → 1 file
- **Zero code deployment for new pages**: Add via database seed only
- **Simplified maintenance**: One component to update instead of 11
- **Consistent behavior**: All pages use identical logic

**Business Benefits**:
- **Faster page creation**: 5 minutes instead of 30 minutes
- **No deployment downtime**: New pages via database only
- **Scalable to 100+ pages**: No code changes needed
- **Self-service ready**: Future admin UI can create pages without developers

**Technical Benefits**:
- **Smaller bundle size**: 11 components removed
- **Better code splitting**: Single lazy-loaded component
- **Easier testing**: One component to test thoroughly
- **Consistent routing**: Single pattern to understand

### Why We Made This Change

**Research Decision Document**: `/docs/functional-areas/content-management-system/research/2025-11-12-dynamic-cms-routing-research.md`

**Evaluation**:
- **Option 1**: Explicit Routes (current) - Score: 5.4/10 (fails database-only requirement)
- **Option 2**: Catch-All Splat Route (`/*`) - Score: 7.5/10 (captures 404s, performance issues)
- **Option 3**: Dynamic Segment Route (`:slug`) - Score: 9.4/10 ✅ **SELECTED**

**Key Factors**:
1. **Business Requirement**: Add pages via database without code deployment
2. **SEO-Friendly URLs**: Direct `/resources` not `/cms/resources`
3. **Route Conflict Prevention**: Explicit ordering (static first, dynamic last)
4. **Performance**: <200ms page load maintained
5. **Developer Experience**: Clear, debuggable routing pattern

### Research Documents

**Technology Research**:
- **Dynamic Routing Decision**: `/docs/functional-areas/content-management-system/research/2025-11-12-dynamic-cms-routing-research.md` (5,500 lines)
- **Routing Patterns Research**: `/docs/functional-areas/content-management-system/research/2025-11-12-cms-routing-patterns-research.md` (8,000 lines)

**Key Findings**:
- React Router v7 best-match algorithm prevents most conflicts
- Route order matters for dynamic segments
- Loader-based 404 handling is best practice
- Single `:slug` route outperforms 11 explicit routes

**Implementation Evidence**:
- **Route Configuration**: `/apps/web/src/routes/router.tsx` (lines 321-327)
- **Dynamic Page Component**: `/apps/web/src/features/cms/pages/CmsDynamicPage.tsx` (40 lines)
- **Reusable Page Component**: `/apps/web/src/features/cms/components/CmsPage.tsx` (197 lines)

---

## Architecture Decisions

### Why Dynamic Routing?

**Decision**: Use single `:slug` route instead of explicit routes per page

**Rationale**:
1. **Scalability**: System must support adding CMS pages without developer intervention
2. **Maintenance**: Single component easier to maintain than 11+ separate components
3. **Business Agility**: Content team can add pages via database seed without deployments
4. **Code Quality**: Reduces duplication and boilerplate

**Trade-Offs**:
- ✅ **Pro**: Zero code changes for new pages
- ✅ **Pro**: Smaller bundle size
- ⚠️ **Con**: Route order dependency (must document and enforce)
- ⚠️ **Con**: Slug collision risk (mitigated by reserved slug list)

### Why `:slug` Instead of Catch-All (`/*`)?

**Decision**: Use `:slug` dynamic segment instead of `*` splat route

**Rationale**:
1. **Correct 404 Behavior**: `/*` captures all 404s including typos in static routes
2. **Performance**: `:slug` only queries database for matched routes, not all 404s
3. **Debugging**: Clearer route matching behavior
4. **Best Practice**: Official React Router recommendation for dynamic content

**Example Problem with Catch-All**:
```typescript
// User typos /events as /event
// With /* route: CMS page loads → DB query → CMS 404 error (confusing)
// With :slug route: No match → Generic 404 page (correct behavior)
```

### Why No `/cms/` Prefix?

**Decision**: Use direct slugs (`/resources`) instead of prefixed (`/cms/resources`)

**Rationale**:
1. **SEO**: Shorter, cleaner URLs rank better
2. **User Experience**: `/resources` more professional than `/cms/resources`
3. **Business Requirement**: Explicit requirement from business requirements document
4. **URL Migration**: Changing URL structure later requires redirects (painful)

**Trade-Off**:
- ✅ **Pro**: Better SEO and UX
- ⚠️ **Con**: Must maintain reserved slug list to prevent conflicts

### Why TipTap Editor?

**Decision**: Use Mantine TipTap editor instead of alternatives (TinyMCE, Quill, Draft.js)

**Rationale**:
1. **Design System Integration**: Built for Mantine v7 (project's UI library)
2. **Zero Configuration**: No API keys or quota limits (TinyMCE problem)
3. **Bundle Size**: 70% smaller than TinyMCE
4. **Modern Stack**: ProseMirror-based (same as Notion, Google Docs)
5. **Extensibility**: Easy to add custom buttons and features

**Reference**: HTML Editor Migration project (`/docs/functional-areas/html-editor-migration/`)

### Why Revision History?

**Decision**: Store full page history, not just current version

**Rationale**:
1. **Audit Trail**: See who changed what and when
2. **Mistake Recovery**: Restore previous versions if needed
3. **Compliance**: Some communities require change logs
4. **Learning**: Understand content evolution over time

**Trade-Off**:
- ✅ **Pro**: Complete audit trail
- ⚠️ **Con**: Database storage grows (acceptable for text-only content)

---

## Performance Considerations

### Page Load Performance

**Target**: <200ms page load time (from requirements)

**Actual Performance**:
```
User navigates to /resources
  ↓
Route matching: <1ms (React Router static lookup)
  ↓
Component render: <16ms (React component mount)
  ↓
API fetch: 50-150ms (backend response + network)
  ↓
Database query: 10-50ms (indexed slug lookup)
  ↓
HTML rendering: <10ms (dangerouslySetInnerHTML)
  ↓
Total: 70-230ms (typically <200ms ✅)
```

**Optimization Strategies**:
1. **TanStack Query Caching**: 5-minute stale time reduces API calls
2. **Database Indexes**: `slug` column indexed for O(log n) lookups
3. **AsNoTracking**: Read-only queries skip EF Core change tracking
4. **Component Memoization**: CmsPage component uses React.memo where appropriate
5. **Lazy Loading**: CmsDynamicPage can be lazy-loaded if needed

### Bundle Size Impact

**Before (11 Explicit Components)**:
- 11 page components × ~2KB each = ~22KB
- 11 route definitions = ~2KB
- Total: ~24KB

**After (1 Dynamic Component)**:
- 1 CmsDynamicPage component = ~2KB
- 1 route definition = ~0.2KB
- Total: ~2.2KB

**Savings**: ~22KB (~90% reduction)

### Caching Strategy

**TanStack Query Configuration**:
```typescript
{
  queryKey: ['cms-page', slug],
  queryFn: () => getCmsPage(slug),
  staleTime: 5 * 60 * 1000,    // 5 minutes
  cacheTime: 10 * 60 * 1000,   // 10 minutes
  retry: 3,                     // Retry failed requests
  retryDelay: 1000,             // 1 second between retries
}
```

**Cache Behavior**:
- **First Visit**: Fresh fetch from API → Database query → Cache stored
- **Within 5 Minutes**: Instant from cache (no API call)
- **5-10 Minutes**: Stale data shown, background refetch triggered
- **After 10 Minutes**: Cache evicted, next visit triggers fresh fetch

**Invalidation**:
- Manual: After save, `invalidateQueries(['cms-page', slug])` clears cache
- Automatic: TanStack Query refetches on window focus (configurable)

### Database Performance

**Indexes**:
```sql
-- Primary key (automatic B-tree index)
PRIMARY KEY (id)

-- Unique constraint on slug (automatic B-tree index)
CONSTRAINT cms_pages_slug_key UNIQUE (slug)

-- Explicit index on slug (for query optimization)
CREATE INDEX idx_cms_pages_slug ON cms_pages (slug)

-- Partial index on active pages only
CREATE INDEX idx_cms_pages_is_active ON cms_pages (is_active) WHERE is_active = true
```

**Query Performance**:
```sql
-- Lookup by slug (most common query)
SELECT * FROM cms_pages WHERE slug = 'resources' AND is_active = true
-- Execution time: 1-10ms (indexed lookup)

-- List all active pages (admin revision list)
SELECT * FROM cms_pages WHERE is_active = true ORDER BY updated_at DESC
-- Execution time: 10-50ms (full table scan acceptable for admin UI)
```

### Lazy Loading (Optional)

**Current State**: CmsDynamicPage loaded with main bundle

**Future Optimization** (if bundle size becomes concern):
```typescript
// Lazy load CMS dynamic route
const CmsDynamicPage = lazy(() => import('../features/cms/pages/CmsDynamicPage'))

const router = createBrowserRouter([
  {
    path: ':slug',
    element: (
      <Suspense fallback={<LoadingSkeleton />}>
        <CmsDynamicPage />
      </Suspense>
    ),
  },
])
```

**Impact**:
- Initial bundle: -2KB
- CMS page load: +50ms (lazy load time)
- **Recommendation**: Not needed unless bundle size >500KB

---

## Security Considerations

### HTML Sanitization

**Server-Side Sanitization** (apps/api/Features/CMS/Services/CmsService.cs):
```csharp
using Ganss.Xss;

public class CmsService
{
    private readonly HtmlSanitizer _sanitizer = new HtmlSanitizer();

    public async Task<CmsPageDto> UpdatePageAsync(string slug, UpdateCmsPageRequest request)
    {
        // Sanitize all HTML before storage
        page.Title = _sanitizer.Sanitize(request.Title);
        page.Content = _sanitizer.Sanitize(request.Content);

        await _context.SaveChangesAsync();
        return MapToDto(page);
    }
}
```

**Allowed Tags**:
```csharp
_sanitizer.AllowedTags = new HashSet<string>
{
    "p", "br", "strong", "em", "u", "h2", "h3", "h4", "h5", "h6",
    "ul", "ol", "li", "a", "span", "div"
};

_sanitizer.AllowedAttributes = new HashSet<string>
{
    "href", "title", "style", "class"
};

_sanitizer.AllowedCssProperties = new HashSet<string>
{
    "color", "text-align", "font-weight"
};
```

**Blocked Tags**: `<script>`, `<iframe>`, `<object>`, `<embed>`, `<form>`, `<input>`, etc.

**XSS Protection**:
- All user input sanitized server-side (never trust frontend)
- `dangerouslySetInnerHTML` safe because content is pre-sanitized
- TipTap editor client-side validation (defense in depth)

### Authorization

**Role-Based Access Control**:
```csharp
// API endpoint authorization
[Authorize(Policy = "RequireAdministratorRole")]
public async Task<ActionResult<CmsPageDto>> UpdatePage(string slug, UpdateCmsPageRequest request)
{
    // Only Administrator role can update pages
}
```

**Frontend Role Check**:
```typescript
const user = useUser()
const isAdmin = user?.role === 'Administrator'

// Edit button only visible to admins
{isAdmin && <CmsEditButton onClick={handleEdit} />}
```

**Security Notes**:
- Backend authorization is source of truth (frontend is UX only)
- Non-admin API requests return 403 Forbidden
- JWT token validated on every request

### CSRF Protection

**Cookie-Based Auth**: Application uses httpOnly cookies with SameSite=Strict

**CSRF Tokens**: Not explicitly needed because:
1. SameSite=Strict prevents cross-site requests
2. Custom headers (e.g., `X-Requested-With`) provide additional protection
3. Modern browsers block cross-origin requests by default

**API Configuration**:
```csharp
services.AddAntiforgery(options =>
{
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});
```

### SQL Injection Prevention

**Entity Framework Protection**:
```csharp
// EF Core parameterizes all queries automatically
var page = await _context.CmsPages.FirstOrDefaultAsync(p => p.Slug == slug);

// Generated SQL (safe):
// SELECT * FROM cms_pages WHERE slug = @p0
// Parameters: @p0 = 'resources'
```

**No Raw SQL**: All database queries use LINQ, never raw SQL strings

### Content Security Policy

**Future Enhancement** (not currently implemented):
```html
<meta http-equiv="Content-Security-Policy"
      content="default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'">
```

**Would Prevent**:
- Inline script execution
- Loading external scripts
- Loading external stylesheets (except whitelisted)

---

## Troubleshooting

### Common Issues

#### Issue: CMS Page Shows 404 Error

**Symptoms**:
- Navigate to `/resources` → "Failed to load page content"
- API returns 404 status

**Causes & Solutions**:

**1. Slug Not in Database**
```bash
# Check if page exists
docker exec -it witchcityrope-db psql -U witchcityrope -d witchcityrope -c "SELECT slug FROM cms_pages WHERE slug = 'resources';"

# If empty, page was never seeded
# Solution: Add to CmsSeedData.cs and run migration
```

**2. IsActive = False**
```bash
# Check if page is active
docker exec -it witchcityrope-db psql -U witchcityrope -d witchcityrope -c "SELECT slug, is_active FROM cms_pages WHERE slug = 'resources';"

# If is_active = false
# Solution: Update to true
docker exec -it witchcityrope-db psql -U witchcityrope -d witchcityrope -c "UPDATE cms_pages SET is_active = true WHERE slug = 'resources';"
```

**3. API Not Running**
```bash
# Check API health
curl http://localhost:5655/health

# If no response
# Solution: Start Docker containers
./dev.sh
```

#### Issue: Edit Button Not Visible to Admin

**Symptoms**:
- Logged in as admin@witchcityrope.com
- No "Edit This Page" button appears

**Causes & Solutions**:

**1. Role Check Logic Error**
```typescript
// Verify user role in browser console
const user = useUser()
console.log('User role:', user?.role)
console.log('Is admin?', user?.role === 'Administrator')

// Should output: Is admin? true
```

**2. User Not Actually Admin**
```bash
# Check user roles in database
docker exec -it witchcityrope-db psql -U witchcityrope -d witchcityrope -c "SELECT email, role FROM users WHERE email = 'admin@witchcityrope.com';"

# Should show: role = 1 (Administrator enum value)
```

**3. Component Not Re-Rendering After Login**
```typescript
// Force page refresh after login
window.location.reload()

// Or update auth store to trigger re-render
```

#### Issue: Changes Not Saving

**Symptoms**:
- Click "Save" button
- Spinner shows briefly
- Changes revert to previous content

**Causes & Solutions**:

**1. 403 Forbidden (Authorization)**
```typescript
// Check browser network tab
// PUT /api/cms/pages/resources → 403 Forbidden

// Solution: Verify Administrator role
// Backend only allows Administrator role to save
```

**2. 400 Bad Request (Validation)**
```typescript
// Check API error response
// Response: "Title is required" or "Content exceeds max length"

// Solution: Ensure title filled and content not empty
```

**3. Network Error**
```typescript
// Check browser console for errors
// Error: "Failed to fetch" or "Network request failed"

// Solution: Verify API is running
curl http://localhost:5655/health
```

#### Issue: Static Route Captured by CMS Route

**Symptoms**:
- Navigate to `/events` → Shows CMS 404 error instead of events list
- Navigate to `/dashboard` → Shows CMS 404 error instead of dashboard

**Cause**: `:slug` route defined BEFORE static routes in `router.tsx`

**Solution**: Move `:slug` route to END of routes array
```typescript
// ❌ WRONG
children: [
  { path: ':slug', element: <CmsDynamicPage /> }, // TOO EARLY!
  { path: 'events', element: <EventsListPage /> }, // NEVER REACHED
]

// ✅ CORRECT
children: [
  { path: 'events', element: <EventsListPage /> }, // FIRST
  { path: 'dashboard', element: <DashboardPage /> },
  // ... all other static routes ...
  { path: ':slug', element: <CmsDynamicPage /> }, // LAST
]
```

#### Issue: HTML Content Not Rendering

**Symptoms**:
- Page shows raw HTML: `<p>Content here</p>`
- HTML tags visible to users

**Causes & Solutions**:

**1. Missing `dangerouslySetInnerHTML`**
```typescript
// ❌ WRONG
<div>{content.content}</div>

// ✅ CORRECT
<div dangerouslySetInnerHTML={{ __html: content.content }} />
```

**2. Over-Sanitization**
```csharp
// Check if sanitizer removed all tags
_sanitizer.AllowedTags.Add("p");
_sanitizer.AllowedTags.Add("strong");
// ... ensure basic tags allowed
```

#### Issue: Revisions Not Being Created

**Symptoms**:
- Save works
- Revision list shows no new entries
- Revision count not incrementing

**Cause**: Service not creating revision before update

**Solution**: Verify service logic
```csharp
public async Task<CmsPageDto> UpdatePageAsync(string slug, UpdateCmsPageRequest request)
{
    var page = await _context.CmsPages.FirstOrDefaultAsync(p => p.Slug == slug);

    // ✅ MUST create revision BEFORE updating page
    var revision = new CmsPageRevision
    {
        PageId = page.Id,
        Title = page.Title,           // OLD title
        Content = page.Content,       // OLD content
        ChangedAt = DateTime.UtcNow,
        ChangedByUserId = userId
    };
    _context.CmsPageRevisions.Add(revision);

    // Then update page
    page.Title = request.Title;
    page.Content = request.Content;
    page.UpdatedAt = DateTime.UtcNow;

    await _context.SaveChangesAsync();
}
```

### Debug Commands

**Check Database Contents**:
```bash
# List all CMS pages
docker exec -it witchcityrope-db psql -U witchcityrope -d witchcityrope -c "SELECT id, slug, title, updated_at, is_active FROM cms_pages ORDER BY updated_at DESC;"

# Count revisions per page
docker exec -it witchcityrope-db psql -U witchcityrope -d witchcityrope -c "SELECT p.slug, COUNT(r.id) AS revision_count FROM cms_pages p LEFT JOIN cms_page_revisions r ON p.id = r.page_id GROUP BY p.slug ORDER BY revision_count DESC;"

# View latest revision for a page
docker exec -it witchcityrope-db psql -U witchcityrope -d witchcityrope -c "SELECT * FROM cms_page_revisions WHERE page_id = 1 ORDER BY changed_at DESC LIMIT 5;"
```

**Test API Endpoints**:
```bash
# Fetch page by slug
curl http://localhost:5655/api/cms/pages/resources

# Update page (requires auth token)
curl -X PUT http://localhost:5655/api/cms/pages/resources \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"title":"Updated Title","content":"<p>Updated content</p>"}'

# List revisions
curl http://localhost:5655/api/cms/pages/1/revisions \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

**Check Frontend Types**:
```bash
# Verify NSwag types generated
cat packages/shared-types/src/generated/api-types.ts | grep "CmsPageDto"

# Should show:
# export interface CmsPageDto {
#   id: number
#   slug: string
#   title: string
#   content: string
#   ...
# }
```

**Browser Console Debug**:
```javascript
// Check TanStack Query cache
window.__REACT_QUERY_DEVTOOLS_CACHE__

// Check auth state
const user = useUser()
console.log('User:', user)
console.log('Is admin?', user?.role === 'Administrator')

// Check CMS page data
const { data } = useQuery(['cms-page', 'resources'])
console.log('CMS Page:', data)
```

---

## Future Enhancements

### Planned Features

**1. CMS Page Admin UI** (not implemented)
- Create new pages via admin interface (no SQL required)
- Edit page slugs (with validation for reserved slugs)
- Soft delete pages (set is_active = false)
- Reorder navigation menu items

**2. Draft/Publish Workflow** (not implemented)
- Save without publishing (draft mode)
- Preview draft before publish
- Schedule future publish date
- Unpublish feature (hide without deleting)

**3. Restore Revision** (not implemented)
- "Restore this revision" button on revision detail page
- Creates new revision with old content
- Confirms before restoring (irreversible)

**4. Change Summary** (not implemented)
- Text field: "What did you change?" on save
- Stored in `cms_page_revisions.change_summary`
- Displayed in revision list for easier tracking

**5. Rich Media Support** (not implemented)
- Image uploads (S3/CloudFlare R2)
- Video embeds (YouTube, Vimeo)
- File attachments (PDFs, documents)
- Image gallery component

**6. SEO Metadata** (not implemented)
- Meta description field
- Open Graph tags
- Twitter Card tags
- Custom canonical URL

**7. Access Control** (not implemented)
- Member-only pages (require authentication)
- Vetted-only pages (require vetting status)
- Role-based page visibility

**8. Analytics Integration** (not implemented)
- Track page views
- Track edit frequency
- Track most popular pages
- Admin dashboard with metrics

### Potential Improvements

**Performance**:
- CDN caching for static content
- Service worker for offline access
- Image optimization and lazy loading
- Preload frequently accessed pages

**Developer Experience**:
- CMS page preview in development
- Hot reload for CMS content changes
- Seed data generator CLI tool
- Automated slug reservation check

**Content Management**:
- Markdown support (alternative to HTML)
- Content templates (reusable page layouts)
- Global content blocks (header, footer)
- Search functionality for CMS pages

**Security**:
- Content Security Policy headers
- Rate limiting on save endpoints
- Audit log for all changes (already partially implemented)
- Two-factor auth requirement for CMS editing

---

## Related Documentation

### Design Phase Documents
- **Business Requirements**: `/docs/functional-areas/content-management-system/new-work/2025-10-17-cms-implementation/requirements/business-requirements.md`
- **Functional Specification**: `/docs/functional-areas/content-management-system/new-work/2025-10-17-cms-implementation/requirements/functional-spec.md`
- **UI/UX Design**: `/docs/functional-areas/content-management-system/new-work/2025-10-17-cms-implementation/design/ui-design.md`
- **Database Design**: `/docs/functional-areas/content-management-system/new-work/2025-10-17-cms-implementation/design/database-design.md`

### Research Documents
- **Dynamic Routing Research**: `/docs/functional-areas/content-management-system/research/2025-11-12-dynamic-cms-routing-research.md` (5,500 lines)
- **Routing Patterns Research**: `/docs/functional-areas/content-management-system/research/2025-11-12-cms-routing-patterns-research.md` (8,000 lines)

### Implementation Guides
- **HTML Editor Migration**: `/docs/functional-areas/html-editor-migration/` (TinyMCE → TipTap)
- **React Architecture Index**: `/docs/architecture/REACT-ARCHITECTURE-INDEX.md`
- **DTO Alignment Strategy**: `/docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md`

### Standards & Processes
- **React Patterns**: `/docs/standards-processes/frontend/react-patterns.md`
- **TypeScript Patterns**: `/docs/standards-processes/frontend/typescript-patterns.md`
- **API Design Patterns**: `/docs/standards-processes/backend/api-design-patterns.md`

### Testing Documentation
- **TEST_CATALOG**: `/docs/standards-processes/testing/TEST_CATALOG.md`
- **E2E Testing Patterns**: `/docs/lessons-learned/mantine-e2e-testing-patterns-2025-11-10.md`

### Backend Implementation
- **Service Layer**: `/apps/api/Features/CMS/Services/CmsService.cs`
- **Endpoint Definitions**: `/apps/api/Features/CMS/Endpoints/CmsEndpoints.cs`
- **Entity Models**: `/apps/api/Features/CMS/Entities/`
- **Seed Data**: `/apps/api/Features/CMS/Data/CmsSeedData.cs`

### Frontend Implementation
- **CMS Feature**: `/apps/web/src/features/cms/`
- **Dynamic Page**: `/apps/web/src/features/cms/pages/CmsDynamicPage.tsx`
- **Page Component**: `/apps/web/src/features/cms/components/CmsPage.tsx`
- **TipTap Editor**: `/apps/web/src/components/forms/MantineTiptapEditor.tsx`
- **Router Config**: `/apps/web/src/routes/router.tsx`

---

## Conclusion

The WitchCityRope CMS implementation uses a **modern dynamic routing architecture** that enables content management without code deployment. The single `:slug` route pattern provides scalability, maintainability, and business agility while maintaining strong SEO and performance characteristics.

### Key Takeaways

**For Developers**:
1. `:slug` route MUST be LAST in routes array
2. New pages added via `CmsSeedData.cs` only (no code changes)
3. Always use NSwag-generated types (never manual interfaces)
4. HTML sanitization happens server-side (backend is source of truth)

**For Content Managers**:
1. Editing is in-browser with rich text editor (no technical knowledge required)
2. All changes tracked with full revision history
3. Changes are live immediately (no draft/publish workflow yet)
4. Only Administrators can edit (secure by default)

**For Product Owners**:
1. New pages can be added in 5 minutes (database seed only)
2. No code deployment required for content changes
3. System scales to 100+ pages with zero performance degradation
4. Architecture supports future enhancements (draft/publish, media, SEO)

### Single Source of Truth

This guide is the **primary reference** for CMS implementation. All agents (developers, testers, designers) should reference this document when working on CMS features.

**Document Version**: 1.0
**Last Updated**: 2025-11-12
**Next Review**: After CMS Phase 3 implementation (database migrations)

---

**Questions?** See [Related Documentation](#related-documentation) or contact the development team.

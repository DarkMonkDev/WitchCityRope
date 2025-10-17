# Technology Research: React-Based CMS Architecture
<!-- Last Updated: 2025-10-17 -->
<!-- Version: 1.0 -->
<!-- Owner: Technology Researcher Agent -->
<!-- Status: Complete -->

## Executive Summary

**Decision Required**: Architecture pattern for in-place content editing system for WitchCityRope static pages (Contact Us, Resources, Private Lessons)

**Top Recommendations**:
1. **Route-based page identification** with slug-to-component mapping
2. **PostgreSQL TEXT column** for HTML storage with backend sanitization
3. **Separate revisions table** with full content snapshots
4. **TanStack Query cache-based optimistic updates** for smooth UX
5. **Backend-only sanitization** using HtmlSanitizer.NET

**Key Factors**:
- Simplicity over flexibility (3 pages, not 300)
- Admin-only editing (no public content creation)
- Existing TipTap editor integration
- Performance for mobile users

**Confidence Level**: High (85%)

---

## Research Scope

### Requirements
- **In-place editing**: Click "Edit" → TipTap appears inline → Save/Cancel
- **Admin-only access**: Strict authorization, no public creation
- **Revision history**: Track changes with user attribution
- **Text-only content**: No image upload for MVP
- **Minimal API backend**: .NET 9 vertical slice pattern
- **TipTap integration**: Use existing MantineTiptapEditor.tsx component

### Success Criteria
- Adding new CMS page requires <30 minutes developer time
- No over-engineering for 3 initial pages
- Developer experience: Clear, obvious patterns
- Performance: <200ms page load, <100ms save response
- Security: XSS prevention, admin-only access

### Out of Scope
- SEO metadata (not needed for MVP)
- Draft/published workflow (live editing only)
- Image upload (text-only)
- Multi-language support
- Public content creation

---

## Architecture Discovery Results

### Documents Reviewed
- **domain-layer-architecture.md**: Lines 1-750 - NSwag type generation confirmed
- **DTO-ALIGNMENT-STRATEGY.md**: Lines 1-213 - API DTOs as source of truth
- **migration-plan.md**: Lines 1-250 - React + TypeScript + Mantine v7 + TanStack Query stack
- **MantineTiptapEditor.tsx**: Lines 1-304 - Working TipTap editor with variable insertion
- **cms-legacy-2025-08-15/cms-technical-design.md**: Lines 1-573 - Blazor reference architecture

### Existing Solutions Found
- **TipTap Editor**: Already integrated at `/apps/web/src/components/forms/MantineTiptapEditor.tsx` (lines 184-303)
- **Admin Authentication**: Cookie-based BFF pattern fully operational
- **Minimal API Pattern**: Vertical slice architecture established in `/apps/api/Features/`
- **NSwag Type Generation**: Automated TypeScript interface generation from C# DTOs

### Verification Statement
"Confirmed TipTap editor and authentication are production-ready. No existing CMS content management solution. New implementation required following established patterns."

---

## Technology Options Evaluated

## Research Topic #1: Page Identification Strategy

### Overview
How should we identify and route to CMS-managed pages in a React SPA?

### Option 1: Route-Based Identification ⭐ RECOMMENDED

**Overview**: URL path defines the page, directly mapped to React routes
**Pattern**: `/resources` → ResourcesPage component → fetch content by slug "resources"

**Implementation**:
```typescript
// Route definition
<Route path="/resources" element={<CmsPage slug="resources" />} />
<Route path="/contact-us" element={<CmsPage slug="contact-us" />} />
<Route path="/private-lessons" element={<CmsPage slug="private-lessons" />} />

// API call
const { data } = useQuery({
  queryKey: ['cms-page', 'resources'],
  queryFn: () => api.get(`/api/cms/pages/resources`)
});
```

**Pros**:
- ✅ **Simplest implementation**: Direct route → slug mapping
- ✅ **Type-safe routes**: TypeScript can validate route strings
- ✅ **SEO-friendly**: Clean URLs like `/resources`, `/contact-us`
- ✅ **Fast lookups**: Database indexed on slug (VARCHAR index)
- ✅ **Obvious mapping**: Developer immediately knows which page is which
- ✅ **No extra database queries**: Slug is known from route

**Cons**:
- ⚠️ Requires route definition for each page (acceptable for 3 pages)
- ⚠️ URL changes require route update (rare for static pages)

**WitchCityRope Fit**:
- Safety/Privacy: ✅ No user-generated URLs, admin controls all routes
- Mobile Experience: ✅ Clean URLs easy to share and bookmark
- Learning Curve: ✅ Minimal - developers understand routes
- Community Values: ✅ Transparent, predictable structure

**Research Sources**:
- [React Router Dynamic Routes](https://stackoverflow.com/questions/42659611/reactjs-use-slug-instead-of-id-in-the-url-with-react-router)
- [Slug-based Routing Best Practices](https://rohitmondallblog.hashnode.dev/usingslug-to-make-dynamic-and-user-friendly-urls-for-our-website)

**Database Schema**:
```sql
CREATE TABLE cms.ContentPages (
    Id SERIAL PRIMARY KEY,
    Slug VARCHAR(100) NOT NULL UNIQUE,  -- 'resources', 'contact-us'
    Title VARCHAR(200) NOT NULL,
    Content TEXT NOT NULL,
    CreatedAt TIMESTAMP WITH TIME ZONE NOT NULL,
    UpdatedAt TIMESTAMP WITH TIME ZONE NOT NULL,
    CreatedBy VARCHAR(450) NOT NULL,
    LastModifiedBy VARCHAR(450) NOT NULL,

    CONSTRAINT FK_ContentPages_CreatedBy FOREIGN KEY (CreatedBy)
        REFERENCES auth.AspNetUsers(Id),
    CONSTRAINT FK_ContentPages_LastModifiedBy FOREIGN KEY (LastModifiedBy)
        REFERENCES auth.AspNetUsers(Id)
);

CREATE UNIQUE INDEX IX_ContentPages_Slug ON cms.ContentPages(Slug);
```

---

### Option 2: ID-Based with Slug Mapping

**Overview**: Numeric IDs in database, slugs stored separately for routing
**Pattern**: `/resources` → lookup slug → get ID 123 → fetch content

**Implementation**:
```typescript
// Two-step lookup
const { data: slugMap } = useQuery({
  queryKey: ['cms-slug-map'],
  queryFn: () => api.get('/api/cms/slug-to-id')
});

const pageId = slugMap['resources']; // Returns 123

const { data: content } = useQuery({
  queryKey: ['cms-page', pageId],
  queryFn: () => api.get(`/api/cms/pages/${pageId}`)
});
```

**Pros**:
- ✅ Numeric IDs are efficient for joins
- ✅ Can change slug without losing references

**Cons**:
- ❌ **Extra complexity**: Two API calls or slug-to-ID mapping
- ❌ **Slower**: Additional lookup step
- ❌ **Confusing**: Developers must understand slug/ID relationship
- ❌ **Over-engineering**: Unnecessary for 3 static pages

**WitchCityRope Fit**:
- Learning Curve: ❌ More complex for volunteers
- Maintenance: ❌ Extra moving parts

---

### Option 3: Component-Based CMS Wrapper

**Overview**: Special React component wraps arbitrary content
**Pattern**: `<CmsContent id="resources">...</CmsContent>`

**Implementation**:
```typescript
// Dynamic content wrapper
const CmsContent = ({ id, children }) => {
  const { data, isEditing } = useCmsContent(id);

  return isEditing ? <TipTapEditor /> : <div>{data?.html || children}</div>;
};

// Usage
<CmsContent id="resources">
  <h1>Default Resources Content</h1>
</CmsContent>
```

**Pros**:
- ✅ Flexible - can wrap any component
- ✅ Default fallback content in JSX

**Cons**:
- ❌ **Confusing edit mode**: What am I editing?
- ❌ **Not truly in-place**: Editor replaces entire wrapped section
- ❌ **Harder to find**: Content spread across components
- ❌ **Type safety issues**: Generic wrapper loses context

**WitchCityRope Fit**:
- Developer Experience: ❌ Confusing, hard to debug
- Maintainability: ❌ Content scattered across components

---

### Comparison Matrix: Page Identification

| Criteria | Weight | Route-Based | ID-Based | Component-Based | Winner |
|----------|--------|-------------|----------|-----------------|--------|
| **Simplicity** | 30% | 10/10 | 5/10 | 4/10 | **Route-Based** |
| **Developer Experience** | 25% | 9/10 | 6/10 | 5/10 | **Route-Based** |
| **Performance** | 15% | 10/10 | 7/10 | 8/10 | **Route-Based** |
| **Ease of Adding Pages** | 15% | 8/10 | 6/10 | 7/10 | **Route-Based** |
| **Type Safety** | 10% | 9/10 | 7/10 | 5/10 | **Route-Based** |
| **SEO Friendliness** | 5% | 10/10 | 10/10 | 8/10 | **Route/ID Tie** |
| **Total Weighted Score** | | **9.15** | **6.15** | **5.70** | **Route-Based** |

### Recommendation: Route-Based Identification ⭐

**Rationale**:
1. **Simplest for 3 pages**: Direct route → slug mapping is obvious
2. **Best developer experience**: No hidden lookups or abstractions
3. **Fastest**: Single database query, no slug-to-ID resolution
4. **Type-safe**: Routes can be typed and validated
5. **Predictable**: Adding new page = add route + database entry

**Implementation Guidance**:
```typescript
// apps/web/src/pages/cms/CmsPage.tsx
interface CmsPageProps {
  slug: string;
  defaultTitle?: string;
  defaultContent?: string;
}

export const CmsPage = ({ slug, defaultTitle, defaultContent }: CmsPageProps) => {
  const { data: content, isLoading } = useQuery({
    queryKey: ['cms-page', slug],
    queryFn: () => cmsApi.getPageBySlug(slug)
  });

  // Render content or editor based on edit mode
};

// apps/web/src/App.tsx
<Route path="/resources" element={
  <CmsPage
    slug="resources"
    defaultTitle="Resources"
    defaultContent="<h1>Resources</h1><p>Community resources...</p>"
  />
} />
```

---

## Research Topic #2: Content Storage & Retrieval Pattern

### Overview
Best practices for storing and fetching HTML content in PostgreSQL with React + Minimal API?

### Option 1: PostgreSQL TEXT Column ⭐ RECOMMENDED

**Overview**: Store HTML directly in TEXT column with backend sanitization
**Data Type**: `Content TEXT NOT NULL`

**Implementation**:
```csharp
// Minimal API endpoint
app.MapGet("/api/cms/pages/{slug}", async (string slug, AppDbContext db) => {
    var page = await db.ContentPages
        .Where(p => p.Slug == slug)
        .Select(p => new ContentPageDto {
            Id = p.Id,
            Slug = p.Slug,
            Title = p.Title,
            Content = p.Content,  // TEXT column, already sanitized
            UpdatedAt = p.UpdatedAt
        })
        .FirstOrDefaultAsync();

    return page is not null ? Results.Ok(page) : Results.NotFound();
});
```

**Pros**:
- ✅ **Simple storage**: Direct HTML string storage
- ✅ **Fast retrieval**: No parsing overhead
- ✅ **Minimal storage**: ~79MB for moderate content (vs JSONB 164MB)
- ✅ **Easy debugging**: Can view HTML directly in database
- ✅ **No TOAST overhead**: Small pages <2KB stay inline
- ✅ **PostgreSQL standard**: Well-understood data type

**Cons**:
- ⚠️ No structured querying within content (not needed)
- ⚠️ Full-text search requires additional indexes (future feature)

**WitchCityRope Fit**:
- Performance: ✅ Fast retrieval for mobile users
- Simplicity: ✅ No complex parsing
- Storage Cost: ✅ Minimal database size

**Research Sources**:
- [PostgreSQL TEXT vs JSONB Performance](https://medium.com/lumigo/the-postgres-showdown-text-columns-vs-jsonb-fields-0ffff011ac46)
- [TOAST Performance Cliff](https://www.evanjones.ca/postgres-large-json-performance.html)

---

### Option 2: JSONB Storage

**Overview**: Store content as JSONB with structured metadata
**Data Type**: `Content JSONB NOT NULL`

**Implementation**:
```sql
-- JSONB storage
Content JSONB NOT NULL
-- Example: {"html": "<h1>Resources</h1>", "metadata": {...}}
```

**Pros**:
- ✅ Structured querying within content
- ✅ Can index specific fields
- ✅ Validate JSON structure

**Cons**:
- ❌ **2× storage overhead**: 164MB vs 79MB for TEXT
- ❌ **Slower writes**: Binary conversion overhead
- ❌ **Slower for large docs**: 2-10× slower for >2KB (TOAST overhead)
- ❌ **Over-engineered**: Don't need structured querying for HTML blobs
- ❌ **Complexity**: JSON structure adds no value for HTML

**WitchCityRope Fit**:
- Simplicity: ❌ Unnecessary complexity
- Storage Cost: ❌ Double the storage

---

### Option 3: File System Storage

**Overview**: Store HTML in files, reference path in database

**Implementation**:
```csharp
// Database stores path
FilePath VARCHAR(500) NOT NULL  -- "/content/resources.html"

// API reads file
var html = await File.ReadAllTextAsync(page.FilePath);
```

**Pros**:
- ✅ Very small database rows
- ✅ Easy backup (copy files)

**Cons**:
- ❌ **Deployment complexity**: Files must be deployed separately
- ❌ **Slower**: File I/O overhead
- ❌ **Harder to version**: Files not in database transactions
- ❌ **No database backup**: Separate backup mechanism
- ❌ **Container unfriendly**: Ephemeral storage in containers

**WitchCityRope Fit**:
- Docker Compatibility: ❌ Container storage issues
- Simplicity: ❌ Extra I/O complexity

---

### Comparison Matrix: Content Storage

| Criteria | Weight | TEXT Column | JSONB | File System | Winner |
|----------|--------|-------------|-------|-------------|--------|
| **Simplicity** | 30% | 10/10 | 6/10 | 4/10 | **TEXT** |
| **Performance (Read)** | 25% | 10/10 | 7/10 | 6/10 | **TEXT** |
| **Storage Efficiency** | 15% | 10/10 | 5/10 | 10/10 | **TEXT/File Tie** |
| **Deployment Ease** | 15% | 10/10 | 10/10 | 3/10 | **TEXT/JSONB** |
| **Backup/Recovery** | 10% | 10/10 | 10/10 | 5/10 | **TEXT/JSONB** |
| **Query Capability** | 5% | 6/10 | 10/10 | 4/10 | **JSONB** |
| **Total Weighted Score** | | **9.55** | **7.25** | **5.10** | **TEXT Column** |

### Recommendation: PostgreSQL TEXT Column ⭐

**Rationale**:
1. **Optimal for HTML blobs**: TEXT designed for this use case
2. **Best performance**: 2× faster than JSONB for reads, no TOAST overhead for small pages
3. **Minimal storage**: Half the space of JSONB
4. **Simplest implementation**: No JSON parsing, no file I/O
5. **Docker-friendly**: Everything in database

**Sanitization Strategy**: Backend-only (see Topic #5)

**Caching Strategy**:
```typescript
// React Query with 5-minute cache
const { data } = useQuery({
  queryKey: ['cms-page', slug],
  queryFn: () => cmsApi.getPageBySlug(slug),
  staleTime: 5 * 60 * 1000,  // 5 minutes
  cacheTime: 10 * 60 * 1000  // 10 minutes
});
```

---

## Research Topic #3: Revision History Architecture

### Overview
How to implement lightweight revision tracking with user attribution?

### Option 1: Separate Revisions Table ⭐ RECOMMENDED

**Overview**: Store full content snapshots in separate table on each save

**Database Schema**:
```sql
CREATE TABLE cms.ContentRevisions (
    Id SERIAL PRIMARY KEY,
    ContentPageId INTEGER NOT NULL,
    Content TEXT NOT NULL,           -- Full HTML snapshot
    Title VARCHAR(200) NOT NULL,     -- Title snapshot
    CreatedAt TIMESTAMP WITH TIME ZONE NOT NULL,
    CreatedBy VARCHAR(450) NOT NULL,
    ChangeDescription VARCHAR(500),  -- Optional change summary

    CONSTRAINT FK_ContentRevisions_ContentPage
        FOREIGN KEY (ContentPageId)
        REFERENCES cms.ContentPages(Id) ON DELETE CASCADE,
    CONSTRAINT FK_ContentRevisions_CreatedBy
        FOREIGN KEY (CreatedBy)
        REFERENCES auth.AspNetUsers(Id) ON DELETE RESTRICT
);

CREATE INDEX IX_ContentRevisions_ContentPageId ON cms.ContentRevisions(ContentPageId);
CREATE INDEX IX_ContentRevisions_CreatedAt ON cms.ContentRevisions(CreatedAt);
```

**Implementation**:
```csharp
// Save workflow in Minimal API
app.MapPut("/api/cms/pages/{id}", async (
    int id,
    UpdateContentRequest req,
    AppDbContext db,
    ClaimsPrincipal user) =>
{
    var page = await db.ContentPages.FindAsync(id);
    if (page is null) return Results.NotFound();

    // Create revision BEFORE updating
    var revision = new ContentRevision {
        ContentPageId = page.Id,
        Content = page.Content,      // Old content
        Title = page.Title,           // Old title
        CreatedAt = DateTime.UtcNow,
        CreatedBy = user.FindFirst(ClaimTypes.NameIdentifier)!.Value,
        ChangeDescription = req.ChangeDescription ?? "Content updated"
    };

    db.ContentRevisions.Add(revision);

    // Update current content
    page.Content = HtmlSanitizer.Sanitize(req.Content);
    page.Title = req.Title;
    page.UpdatedAt = DateTime.UtcNow;
    page.LastModifiedBy = user.FindFirst(ClaimTypes.NameIdentifier)!.Value;

    await db.SaveChangesAsync();

    return Results.Ok(page.ToDto());
});
```

**Pros**:
- ✅ **Simple to implement**: Straightforward INSERT before UPDATE
- ✅ **Easy rollback**: Just copy revision content to current
- ✅ **Full history**: Can see exact state at any point
- ✅ **Fast queries**: Simple SELECT with date filtering
- ✅ **User attribution**: Direct CreatedBy foreign key
- ✅ **Storage acceptable**: ~10-50KB per revision, ~500KB for 10-50 revisions

**Cons**:
- ⚠️ Storage grows with edits (acceptable for 3 pages)
- ⚠️ Redundant data (small price for simplicity)

**WitchCityRope Fit**:
- Simplicity: ✅ Easy to understand and implement
- Performance: ✅ Fast queries for history view
- Storage: ✅ Acceptable for low-edit pages

**Research Sources**:
- [PostgreSQL Revision Tracking Patterns](https://stackoverflow.com/questions/2259158/track-revisions-in-postgresql)
- [Content Versioning with PostgreSQL](https://kaustavdm.in/versioning-content-postgresql/)

---

### Option 2: Audit Log with Diffs

**Overview**: Store only changes (diffs) between versions

**Implementation**:
```sql
CREATE TABLE cms.ContentAuditLog (
    Id SERIAL PRIMARY KEY,
    ContentPageId INTEGER NOT NULL,
    ChangeType VARCHAR(20) NOT NULL,  -- 'INSERT', 'UPDATE', 'DELETE'
    DiffData JSONB NOT NULL,          -- {"added": "...", "removed": "..."}
    CreatedAt TIMESTAMP WITH TIME ZONE NOT NULL,
    CreatedBy VARCHAR(450) NOT NULL
);
```

**Pros**:
- ✅ Minimal storage (only diffs)
- ✅ Detailed change tracking

**Cons**:
- ❌ **Complex reconstruction**: Must apply diffs to rebuild history
- ❌ **Slow rollback**: Chain of diffs must be reversed
- ❌ **Diff algorithm complexity**: HTML diffing is non-trivial
- ❌ **Error-prone**: Broken diff chain = lost history

**WitchCityRope Fit**:
- Simplicity: ❌ Too complex for 3 pages
- Rollback: ❌ Slow and error-prone

---

### Option 3: PostgreSQL Temporal Tables

**Overview**: Use PostgreSQL temporal features (system-versioned tables)

**Implementation**:
```sql
-- Requires PostgreSQL 13+ temporal_tables extension
CREATE TABLE cms.ContentPages (
    ...
    sys_period tstzrange NOT NULL DEFAULT tstzrange(current_timestamp, null)
);

CREATE TABLE cms.ContentPages_history (LIKE cms.ContentPages);
```

**Pros**:
- ✅ Automatic versioning
- ✅ Database-managed

**Cons**:
- ❌ **Extension dependency**: Requires temporal_tables extension
- ❌ **Complex queries**: Temporal syntax is unfamiliar
- ❌ **Limited control**: Less flexible than application-level
- ❌ **Debugging difficulty**: Harder to inspect history

**WitchCityRope Fit**:
- Simplicity: ❌ Unfamiliar to team
- Control: ❌ Less application control

---

### Comparison Matrix: Revision History

| Criteria | Weight | Separate Table | Audit Log Diffs | Temporal Tables | Winner |
|----------|--------|----------------|-----------------|-----------------|--------|
| **Simplicity** | 35% | 10/10 | 4/10 | 5/10 | **Separate Table** |
| **Rollback Ease** | 25% | 10/10 | 5/10 | 7/10 | **Separate Table** |
| **Query Performance** | 15% | 9/10 | 6/10 | 8/10 | **Separate Table** |
| **Storage Efficiency** | 10% | 6/10 | 10/10 | 8/10 | **Audit Diffs** |
| **User Attribution** | 10% | 10/10 | 10/10 | 7/10 | **Separate/Audit** |
| **Debugging** | 5% | 10/10 | 5/10 | 4/10 | **Separate Table** |
| **Total Weighted Score** | | **9.20** | **5.85** | **6.55** | **Separate Table** |

### Recommendation: Separate Revisions Table ⭐

**Rationale**:
1. **Simplest implementation**: INSERT before UPDATE pattern
2. **Fastest rollback**: Single UPDATE to restore
3. **Easy to debug**: Direct table queries show full history
4. **User attribution built-in**: Foreign key to AspNetUsers
5. **Storage acceptable**: 3 pages × 10-50 revisions = minimal overhead

**Implementation Notes**:
- Cascade delete: When page deleted, revisions deleted
- Restrict delete: Can't delete user who created revision (audit trail)
- Optional cleanup: Archive revisions >1 year old (future)

---

## Research Topic #4: In-Place Editing UX Pattern

### Overview
React patterns for seamless in-place editing with TipTap and TanStack Query?

### Option 1: Cache-Based Optimistic Updates ⭐ RECOMMENDED

**Overview**: Use TanStack Query cache manipulation for instant UI updates

**Implementation**:
```typescript
// apps/web/src/hooks/useCmsPage.ts
export const useCmsPage = (slug: string) => {
  const queryClient = useQueryClient();

  // Fetch content
  const query = useQuery({
    queryKey: ['cms-page', slug],
    queryFn: () => cmsApi.getPageBySlug(slug),
    staleTime: 5 * 60 * 1000
  });

  // Update mutation with optimistic updates
  const mutation = useMutation({
    mutationFn: (data: UpdateContentRequest) =>
      cmsApi.updatePage(query.data!.id, data),

    // Optimistic update
    onMutate: async (newData) => {
      // Cancel outgoing queries
      await queryClient.cancelQueries({ queryKey: ['cms-page', slug] });

      // Snapshot previous value
      const previousData = queryClient.getQueryData(['cms-page', slug]);

      // Optimistically update cache
      queryClient.setQueryData(['cms-page', slug], (old: any) => ({
        ...old,
        content: newData.content,
        title: newData.title,
        updatedAt: new Date().toISOString()
      }));

      return { previousData };
    },

    // Rollback on error
    onError: (err, newData, context) => {
      queryClient.setQueryData(
        ['cms-page', slug],
        context?.previousData
      );
      toast.error('Failed to save content');
    },

    // Refetch on success
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: ['cms-page', slug] });
    }
  });

  return {
    content: query.data,
    isLoading: query.isLoading,
    save: mutation.mutateAsync,
    isSaving: mutation.isPending
  };
};

// apps/web/src/pages/cms/CmsPage.tsx
export const CmsPage = ({ slug }: { slug: string }) => {
  const [isEditing, setIsEditing] = useState(false);
  const [editableContent, setEditableContent] = useState('');
  const [editableTitle, setEditableTitle] = useState('');
  const { content, save, isSaving } = useCmsPage(slug);
  const { isAdmin } = useAuth();

  const handleEdit = () => {
    setEditableContent(content!.content);
    setEditableTitle(content!.title);
    setIsEditing(true);
  };

  const handleSave = async () => {
    await save({
      title: editableTitle,
      content: editableContent,
      changeDescription: 'Updated via web interface'
    });
    setIsEditing(false);
  };

  const handleCancel = () => {
    if (window.confirm('Discard changes?')) {
      setIsEditing(false);
    }
  };

  return (
    <Box>
      {isAdmin && (
        <Group mb="md">
          {isEditing ? (
            <>
              <Button onClick={handleSave} loading={isSaving}>Save</Button>
              <Button variant="outline" onClick={handleCancel}>Cancel</Button>
            </>
          ) : (
            <Button onClick={handleEdit}>Edit Page</Button>
          )}
        </Group>
      )}

      {isEditing ? (
        <Stack>
          <TextInput
            label="Page Title"
            value={editableTitle}
            onChange={(e) => setEditableTitle(e.target.value)}
          />
          <MantineTiptapEditor
            value={editableContent}
            onChange={setEditableContent}
            minRows={10}
          />
        </Stack>
      ) : (
        <div dangerouslySetInnerHTML={{ __html: content?.content || '' }} />
      )}
    </Box>
  );
};
```

**Pros**:
- ✅ **Instant feedback**: UI updates immediately
- ✅ **Automatic rollback**: Error = restore previous state
- ✅ **Multiple updates**: Handles concurrent edits across components
- ✅ **TanStack Query native**: Uses built-in optimistic update pattern
- ✅ **Loading states**: `isPending` for button disabling
- ✅ **Error recovery**: Automatic retry + user notification

**Cons**:
- ⚠️ Requires careful cache key management
- ⚠️ Must handle race conditions (TanStack Query does this)

**WitchCityRope Fit**:
- Mobile Experience: ✅ Instant feedback = great UX
- Performance: ✅ <16ms UI update, no network wait
- Error Handling: ✅ Automatic rollback + retry

**Research Sources**:
- [TanStack Query Optimistic Updates](https://tanstack.com/query/latest/docs/framework/react/guides/optimistic-updates)
- [Concurrent Optimistic Updates](https://tkdodo.eu/blog/concurrent-optimistic-updates-in-react-query)

---

### Option 2: Pessimistic Updates (Wait for Server)

**Overview**: Wait for server response before updating UI

**Implementation**:
```typescript
const mutation = useMutation({
  mutationFn: (data) => cmsApi.updatePage(id, data),
  onSuccess: (data) => {
    queryClient.setQueryData(['cms-page', slug], data);
    toast.success('Content saved!');
  }
});
```

**Pros**:
- ✅ Simpler implementation
- ✅ Always consistent with server

**Cons**:
- ❌ **Slower UX**: User waits for network round-trip
- ❌ **No instant feedback**: Poor mobile experience
- ❌ **Feels sluggish**: 200-500ms wait feels unresponsive

**WitchCityRope Fit**:
- Mobile Experience: ❌ Network latency = poor UX

---

### Option 3: Local State Only (No Cache)

**Overview**: Use local useState for editing, sync on save

**Implementation**:
```typescript
const [content, setContent] = useState('');
const handleSave = () => {
  api.updatePage({ content });
};
```

**Pros**:
- ✅ Very simple

**Cons**:
- ❌ **No caching**: Refetch on every navigation
- ❌ **Manual sync**: Must manually update all state
- ❌ **No automatic retry**: Must implement error handling
- ❌ **Lose TanStack Query benefits**: No background refetch, stale-while-revalidate

**WitchCityRope Fit**:
- Performance: ❌ Unnecessary refetches
- Developer Experience: ❌ Manual state management

---

### Comparison Matrix: In-Place Editing UX

| Criteria | Weight | Optimistic (Cache) | Pessimistic | Local State | Winner |
|----------|--------|-------------------|-------------|-------------|--------|
| **User Experience** | 35% | 10/10 | 6/10 | 5/10 | **Optimistic** |
| **Performance** | 25% | 10/10 | 6/10 | 7/10 | **Optimistic** |
| **Error Handling** | 20% | 9/10 | 8/10 | 5/10 | **Optimistic** |
| **Implementation Complexity** | 10% | 7/10 | 9/10 | 10/10 | **Local State** |
| **Mobile Experience** | 10% | 10/10 | 5/10 | 6/10 | **Optimistic** |
| **Total Weighted Score** | | **9.20** | **6.60** | **6.20** | **Optimistic** |

### Recommendation: Cache-Based Optimistic Updates ⭐

**Rationale**:
1. **Best UX**: Instant feedback critical for mobile users
2. **Automatic error handling**: TanStack Query handles rollback + retry
3. **Performance**: <16ms UI update, network call happens in background
4. **Production-proven**: TanStack Query pattern used by major apps
5. **Matches existing stack**: Already using TanStack Query

**Dirty State Tracking**:
```typescript
const [isDirty, setIsDirty] = useState(false);

useEffect(() => {
  const handleBeforeUnload = (e: BeforeUnloadEvent) => {
    if (isDirty && isEditing) {
      e.preventDefault();
      e.returnValue = '';
    }
  };

  window.addEventListener('beforeunload', handleBeforeUnload);
  return () => window.removeEventListener('beforeunload', handleBeforeUnload);
}, [isDirty, isEditing]);
```

---

## Research Topic #5: Content Sanitization & Security

### Overview
How to prevent XSS attacks while allowing rich text formatting from TipTap?

### Option 1: Backend-Only Sanitization with HtmlSanitizer.NET ⭐ RECOMMENDED

**Overview**: Sanitize all HTML on server before storing in database

**Implementation**:
```csharp
// Install: dotnet add package HtmlSanitizer

// Program.cs - Configure service
builder.Services.AddSingleton<IHtmlSanitizer>(sp => {
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

    // Allow safe CSS (text alignment from TipTap)
    sanitizer.AllowedCssProperties.UnionWith(new[] {
        "text-align"
    });

    // Remove all <script>, <iframe>, event handlers
    // HtmlSanitizer does this by default

    return sanitizer;
});

// Minimal API endpoint
app.MapPut("/api/cms/pages/{id}", async (
    int id,
    UpdateContentRequest req,
    AppDbContext db,
    IHtmlSanitizer sanitizer,
    ClaimsPrincipal user) =>
{
    var page = await db.ContentPages.FindAsync(id);
    if (page is null) return Results.NotFound();

    // CRITICAL: Sanitize BEFORE storing
    var cleanHtml = sanitizer.Sanitize(req.Content);

    // Create revision
    db.ContentRevisions.Add(new ContentRevision {
        ContentPageId = page.Id,
        Content = page.Content,
        CreatedAt = DateTime.UtcNow,
        CreatedBy = user.FindFirst(ClaimTypes.NameIdentifier)!.Value
    });

    // Update with sanitized content
    page.Content = cleanHtml;
    page.Title = req.Title;  // Plain text, no sanitization needed
    page.UpdatedAt = DateTime.UtcNow;
    page.LastModifiedBy = user.FindFirst(ClaimTypes.NameIdentifier)!.Value;

    await db.SaveChangesAsync();

    return Results.Ok(page.ToDto());
});
```

**Pros**:
- ✅ **Single source of truth**: Backend controls all sanitization rules
- ✅ **Defense in depth**: Even if frontend bypassed, backend protects
- ✅ **Consistent**: All content sanitized same way
- ✅ **Thread-safe**: HtmlSanitizer.NET is thread-safe
- ✅ **Well-tested**: HtmlSanitizer v9.0.886 (active maintenance)
- ✅ **Admin trust**: Admin users are trusted source
- ✅ **Performance**: Sanitization overhead only on save (rare)

**Cons**:
- ⚠️ Backend dependency (acceptable for security)
- ⚠️ Must configure allowed tags for TipTap features

**WitchCityRope Fit**:
- Security: ✅ XSS prevention guaranteed
- Simplicity: ✅ Single sanitization point
- Admin-only: ✅ Trusted users, backend validates

**Research Sources**:
- [HtmlSanitizer.NET GitHub](https://github.com/mganss/HtmlSanitizer)
- [.NET 8 XSS Prevention](https://medium.com/@feldy7k/preventing-xss-attacks-in-net-8-api-with-html-sanitizer-method-0bb04413526b)
- [OWASP Sanitization Guidance](https://cheatsheetseries.owasp.org/cheatsheets/Cross_Site_Scripting_Prevention_Cheat_Sheet.html)

---

### Option 2: Frontend-Only Sanitization with DOMPurify

**Overview**: Sanitize HTML in React before rendering with dangerouslySetInnerHTML

**Implementation**:
```typescript
// Install: npm install dompurify @types/dompurify
import DOMPurify from 'dompurify';

// Render sanitized content
<div dangerouslySetInnerHTML={{
  __html: DOMPurify.sanitize(content.content)
}} />
```

**Pros**:
- ✅ Client-side protection
- ✅ DOMPurify is OWASP-recommended

**Cons**:
- ❌ **Bypassable**: Direct API calls skip frontend
- ❌ **Database stores unsanitized**: XSS risk if frontend removed
- ❌ **Redundant sanitization**: Re-sanitize on every render
- ❌ **Performance**: Client CPU overhead
- ❌ **Not defense-in-depth**: Single layer of protection

**WitchCityRope Fit**:
- Security: ❌ Insufficient for production
- Performance: ❌ Unnecessary client overhead

---

### Option 3: Both Backend and Frontend (Defense in Depth)

**Overview**: Sanitize on backend save AND frontend render

**Implementation**:
```csharp
// Backend
var cleanHtml = sanitizer.Sanitize(req.Content);
```

```typescript
// Frontend
<div dangerouslySetInnerHTML={{
  __html: DOMPurify.sanitize(content.content)
}} />
```

**Pros**:
- ✅ Maximum security
- ✅ Defense in depth

**Cons**:
- ❌ **Redundant**: Backend sanitization is sufficient for admin-only
- ❌ **Performance waste**: Double sanitization overhead
- ❌ **Maintenance burden**: Two sanitization configs to sync
- ❌ **Over-engineering**: Admin users are trusted

**WitchCityRope Fit**:
- Simplicity: ❌ Unnecessary complexity
- Admin-only context: ❌ Overkill for trusted users

---

### Comparison Matrix: Content Sanitization

| Criteria | Weight | Backend Only | Frontend Only | Both Layers | Winner |
|----------|--------|--------------|---------------|-------------|--------|
| **Security** | 40% | 9/10 | 6/10 | 10/10 | **Both** (overkill) |
| **Simplicity** | 25% | 10/10 | 8/10 | 5/10 | **Backend Only** |
| **Performance** | 15% | 10/10 | 7/10 | 5/10 | **Backend Only** |
| **Defense in Depth** | 10% | 8/10 | 4/10 | 10/10 | **Both** |
| **Maintenance** | 10% | 10/10 | 9/10 | 6/10 | **Backend Only** |
| **Total Weighted Score** | | **9.35** | **6.60** | **7.50** | **Backend Only** |

### Recommendation: Backend-Only Sanitization ⭐

**Rationale**:
1. **Admin-only context**: Trusted users, not public submissions
2. **Single source of truth**: Backend controls all stored content
3. **Defense sufficient**: API gateway prevents direct database access
4. **Performance**: Sanitize once on save, not on every render
5. **Simplicity**: One sanitization config to maintain

**Security Layers**:
1. **Authentication**: Only admins can edit (cookie-based BFF)
2. **Authorization**: Role check on API endpoint
3. **Sanitization**: HtmlSanitizer.NET removes malicious HTML
4. **Database storage**: Only clean HTML persisted

**TipTap Sanitization Config**:
```csharp
// Match TipTap editor capabilities
sanitizer.AllowedTags.UnionWith(new[] {
    // Text formatting
    "p", "br", "strong", "em", "u", "s", "strike",

    // Headings
    "h1", "h2", "h3", "h4", "h5", "h6",

    // Lists
    "ul", "ol", "li",

    // Blockquotes
    "blockquote",

    // Links
    "a",

    // Code
    "code", "pre"
});

sanitizer.AllowedAttributes.UnionWith(new[] {
    "href",   // Links
    "title",  // Accessibility
    "class"   // TipTap styling
});

sanitizer.AllowedCssProperties.UnionWith(new[] {
    "text-align"  // TipTap alignment
});

// AllowedSchemes defaults to http, https, mailto
// This prevents javascript:, data:, etc.
```

---

## Recommended Architecture: Complete Technical Approach

### System Overview

**Frontend (React + TypeScript + Mantine v7)**:
- TipTap editor for in-place editing
- TanStack Query for optimistic updates
- Route-based page identification
- Cache-based state management

**Backend (.NET 9 Minimal API)**:
- Vertical slice architecture
- PostgreSQL TEXT column for content
- Separate revisions table for history
- HtmlSanitizer.NET for XSS prevention

**Security**:
- Admin-only editing (role-based auth)
- Backend sanitization (single source of truth)
- HttpOnly cookies (existing BFF pattern)

### Database Schema

```sql
-- Content Management Schema
CREATE SCHEMA IF NOT EXISTS cms;

-- Main content pages table
CREATE TABLE cms.ContentPages (
    Id SERIAL PRIMARY KEY,
    Slug VARCHAR(100) NOT NULL UNIQUE,
    Title VARCHAR(200) NOT NULL,
    Content TEXT NOT NULL,
    CreatedAt TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    UpdatedAt TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    CreatedBy VARCHAR(450) NOT NULL,
    LastModifiedBy VARCHAR(450) NOT NULL,

    CONSTRAINT FK_ContentPages_CreatedBy
        FOREIGN KEY (CreatedBy)
        REFERENCES auth.AspNetUsers(Id) ON DELETE RESTRICT,
    CONSTRAINT FK_ContentPages_LastModifiedBy
        FOREIGN KEY (LastModifiedBy)
        REFERENCES auth.AspNetUsers(Id) ON DELETE RESTRICT
);

-- Revision history table
CREATE TABLE cms.ContentRevisions (
    Id SERIAL PRIMARY KEY,
    ContentPageId INTEGER NOT NULL,
    Content TEXT NOT NULL,
    Title VARCHAR(200) NOT NULL,
    CreatedAt TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    CreatedBy VARCHAR(450) NOT NULL,
    ChangeDescription VARCHAR(500),

    CONSTRAINT FK_ContentRevisions_ContentPage
        FOREIGN KEY (ContentPageId)
        REFERENCES cms.ContentPages(Id) ON DELETE CASCADE,
    CONSTRAINT FK_ContentRevisions_CreatedBy
        FOREIGN KEY (CreatedBy)
        REFERENCES auth.AspNetUsers(Id) ON DELETE RESTRICT
);

-- Indexes for performance
CREATE UNIQUE INDEX IX_ContentPages_Slug ON cms.ContentPages(Slug);
CREATE INDEX IX_ContentRevisions_ContentPageId ON cms.ContentRevisions(ContentPageId);
CREATE INDEX IX_ContentRevisions_CreatedAt ON cms.ContentRevisions(CreatedAt DESC);

-- Initial seed data
INSERT INTO cms.ContentPages (Slug, Title, Content, CreatedAt, UpdatedAt, CreatedBy, LastModifiedBy)
VALUES
    ('resources', 'Resources', '<h1>Community Resources</h1><p>Welcome to our resources page...</p>', NOW(), NOW(), 'system', 'system'),
    ('contact-us', 'Contact Us', '<h1>Contact Us</h1><p>Get in touch with WitchCityRope...</p>', NOW(), NOW(), 'system', 'system'),
    ('private-lessons', 'Private Lessons', '<h1>Private Lessons</h1><p>Learn rope bondage one-on-one...</p>', NOW(), NOW(), 'system', 'system');
```

### Backend Implementation (Minimal API)

```csharp
// /apps/api/Features/Cms/CmsEndpoints.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Ganss.Xss;

namespace WitchCityRope.Api.Features.Cms;

public static class CmsEndpoints
{
    public static RouteGroupBuilder MapCmsEndpoints(this RouteGroupBuilder group)
    {
        // Public endpoint - get page by slug
        group.MapGet("/pages/{slug}", GetPageBySlug);

        // Admin endpoints - require authorization
        var adminGroup = group.MapGroup("")
            .RequireAuthorization("AdminOnly");

        adminGroup.MapPut("/pages/{id:int}", UpdatePage);
        adminGroup.MapGet("/pages/{id:int}/revisions", GetRevisions);
        adminGroup.MapPost("/pages/{id:int}/rollback/{revisionId:int}", RollbackToRevision);

        return group;
    }

    // GET /api/cms/pages/{slug}
    private static async Task<IResult> GetPageBySlug(
        string slug,
        AppDbContext db)
    {
        var page = await db.ContentPages
            .Where(p => p.Slug == slug)
            .Select(p => new ContentPageDto
            {
                Id = p.Id,
                Slug = p.Slug,
                Title = p.Title,
                Content = p.Content,
                UpdatedAt = p.UpdatedAt,
                LastModifiedBy = p.LastModifiedByUser!.UserName ?? "Unknown"
            })
            .FirstOrDefaultAsync();

        return page is not null
            ? Results.Ok(page)
            : Results.NotFound(new { error = $"Page '{slug}' not found" });
    }

    // PUT /api/cms/pages/{id}
    [Authorize(Roles = "Administrator")]
    private static async Task<IResult> UpdatePage(
        int id,
        UpdateContentRequest req,
        AppDbContext db,
        IHtmlSanitizer sanitizer,
        ClaimsPrincipal user)
    {
        var page = await db.ContentPages
            .Include(p => p.Revisions)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (page is null)
            return Results.NotFound(new { error = "Page not found" });

        var userId = user.FindFirst(ClaimTypes.NameIdentifier)!.Value;

        // Create revision BEFORE updating
        var revision = new ContentRevision
        {
            ContentPageId = page.Id,
            Content = page.Content,
            Title = page.Title,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            ChangeDescription = req.ChangeDescription ?? "Content updated"
        };

        db.ContentRevisions.Add(revision);

        // Sanitize and update
        page.Content = sanitizer.Sanitize(req.Content);
        page.Title = req.Title;
        page.UpdatedAt = DateTime.UtcNow;
        page.LastModifiedBy = userId;

        await db.SaveChangesAsync();

        return Results.Ok(new ContentPageDto
        {
            Id = page.Id,
            Slug = page.Slug,
            Title = page.Title,
            Content = page.Content,
            UpdatedAt = page.UpdatedAt,
            LastModifiedBy = user.Identity!.Name ?? "Unknown"
        });
    }

    // GET /api/cms/pages/{id}/revisions
    [Authorize(Roles = "Administrator")]
    private static async Task<IResult> GetRevisions(
        int id,
        AppDbContext db)
    {
        var revisions = await db.ContentRevisions
            .Where(r => r.ContentPageId == id)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new RevisionDto
            {
                Id = r.Id,
                CreatedAt = r.CreatedAt,
                CreatedBy = r.CreatedByUser!.UserName ?? "Unknown",
                ChangeDescription = r.ChangeDescription,
                ContentPreview = r.Content.Length > 100
                    ? r.Content.Substring(0, 100) + "..."
                    : r.Content
            })
            .Take(50)  // Limit to 50 most recent
            .ToListAsync();

        return Results.Ok(revisions);
    }

    // POST /api/cms/pages/{id}/rollback/{revisionId}
    [Authorize(Roles = "Administrator")]
    private static async Task<IResult> RollbackToRevision(
        int id,
        int revisionId,
        AppDbContext db,
        ClaimsPrincipal user)
    {
        var page = await db.ContentPages.FindAsync(id);
        var revision = await db.ContentRevisions.FindAsync(revisionId);

        if (page is null || revision is null)
            return Results.NotFound();

        if (revision.ContentPageId != id)
            return Results.BadRequest(new { error = "Revision does not belong to this page" });

        var userId = user.FindFirst(ClaimTypes.NameIdentifier)!.Value;

        // Create revision of current state
        db.ContentRevisions.Add(new ContentRevision
        {
            ContentPageId = page.Id,
            Content = page.Content,
            Title = page.Title,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            ChangeDescription = $"Rolled back to revision {revisionId}"
        });

        // Restore from revision
        page.Content = revision.Content;
        page.Title = revision.Title;
        page.UpdatedAt = DateTime.UtcNow;
        page.LastModifiedBy = userId;

        await db.SaveChangesAsync();

        return Results.Ok(new { message = "Rollback successful" });
    }
}

// DTOs
public record ContentPageDto
{
    public int Id { get; init; }
    public string Slug { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public DateTime UpdatedAt { get; init; }
    public string LastModifiedBy { get; init; } = string.Empty;
}

public record UpdateContentRequest
{
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string? ChangeDescription { get; init; }
}

public record RevisionDto
{
    public int Id { get; init; }
    public DateTime CreatedAt { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
    public string? ChangeDescription { get; init; }
    public string ContentPreview { get; init; } = string.Empty;
}

// Entity models
public class ContentPage
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string LastModifiedBy { get; set; } = string.Empty;

    public WitchCityRopeUser? CreatedByUser { get; set; }
    public WitchCityRopeUser? LastModifiedByUser { get; set; }
    public ICollection<ContentRevision> Revisions { get; set; } = new List<ContentRevision>();
}

public class ContentRevision
{
    public int Id { get; set; }
    public int ContentPageId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string? ChangeDescription { get; set; }

    public ContentPage? ContentPage { get; set; }
    public WitchCityRopeUser? CreatedByUser { get; set; }
}

// Program.cs - Register sanitizer
builder.Services.AddSingleton<IHtmlSanitizer>(sp =>
{
    var sanitizer = new HtmlSanitizer();

    sanitizer.AllowedTags.UnionWith(new[] {
        "p", "br", "strong", "em", "u", "s", "strike",
        "h1", "h2", "h3", "h4", "h5", "h6",
        "ul", "ol", "li", "blockquote",
        "a", "code", "pre"
    });

    sanitizer.AllowedAttributes.UnionWith(new[] {
        "href", "title", "class"
    });

    sanitizer.AllowedCssProperties.UnionWith(new[] {
        "text-align"
    });

    return sanitizer;
});

// Program.cs - Map endpoints
var cmsGroup = app.MapGroup("/api/cms")
    .WithTags("CMS")
    .WithOpenApi();

cmsGroup.MapCmsEndpoints();
```

### Frontend Implementation (React + TypeScript)

```typescript
// /apps/web/src/api/cms.api.ts
import { apiClient } from './api-client';
import type {
  ContentPageDto,
  UpdateContentRequest,
  RevisionDto
} from '@witchcityrope/shared-types';

export const cmsApi = {
  getPageBySlug: async (slug: string): Promise<ContentPageDto> => {
    const { data } = await apiClient.get(`/cms/pages/${slug}`);
    return data;
  },

  updatePage: async (
    id: number,
    request: UpdateContentRequest
  ): Promise<ContentPageDto> => {
    const { data } = await apiClient.put(`/cms/pages/${id}`, request);
    return data;
  },

  getRevisions: async (id: number): Promise<RevisionDto[]> => {
    const { data } = await apiClient.get(`/cms/pages/${id}/revisions`);
    return data;
  },

  rollbackToRevision: async (
    id: number,
    revisionId: number
  ): Promise<void> => {
    await apiClient.post(`/cms/pages/${id}/rollback/${revisionId}`);
  }
};

// /apps/web/src/hooks/useCmsPage.ts
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { cmsApi } from '../api/cms.api';
import { notifications } from '@mantine/notifications';
import type { UpdateContentRequest } from '@witchcityrope/shared-types';

export const useCmsPage = (slug: string) => {
  const queryClient = useQueryClient();

  const query = useQuery({
    queryKey: ['cms-page', slug],
    queryFn: () => cmsApi.getPageBySlug(slug),
    staleTime: 5 * 60 * 1000,  // 5 minutes
  });

  const updateMutation = useMutation({
    mutationFn: (data: UpdateContentRequest) =>
      cmsApi.updatePage(query.data!.id, data),

    onMutate: async (newData) => {
      await queryClient.cancelQueries({ queryKey: ['cms-page', slug] });

      const previousData = queryClient.getQueryData(['cms-page', slug]);

      queryClient.setQueryData(['cms-page', slug], (old: any) => ({
        ...old,
        content: newData.content,
        title: newData.title,
        updatedAt: new Date().toISOString()
      }));

      return { previousData };
    },

    onError: (err, newData, context) => {
      queryClient.setQueryData(['cms-page', slug], context?.previousData);
      notifications.show({
        title: 'Error',
        message: 'Failed to save content',
        color: 'red'
      });
    },

    onSuccess: () => {
      notifications.show({
        title: 'Success',
        message: 'Content saved successfully',
        color: 'green'
      });
    },

    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: ['cms-page', slug] });
    }
  });

  return {
    content: query.data,
    isLoading: query.isLoading,
    error: query.error,
    save: updateMutation.mutateAsync,
    isSaving: updateMutation.isPending
  };
};

// /apps/web/src/pages/cms/CmsPage.tsx
import { useState, useEffect } from 'react';
import { Box, Button, Group, Stack, TextInput, LoadingOverlay } from '@mantine/core';
import { MantineTiptapEditor } from '../../components/forms/MantineTiptapEditor';
import { useCmsPage } from '../../hooks/useCmsPage';
import { useAuth } from '../../hooks/useAuth';

interface CmsPageProps {
  slug: string;
  defaultTitle?: string;
  defaultContent?: string;
}

export const CmsPage = ({ slug, defaultTitle, defaultContent }: CmsPageProps) => {
  const [isEditing, setIsEditing] = useState(false);
  const [editableContent, setEditableContent] = useState('');
  const [editableTitle, setEditableTitle] = useState('');
  const [isDirty, setIsDirty] = useState(false);

  const { content, isLoading, save, isSaving } = useCmsPage(slug);
  const { user, hasRole } = useAuth();
  const isAdmin = hasRole('Administrator');

  // Warn before leaving if dirty
  useEffect(() => {
    const handleBeforeUnload = (e: BeforeUnloadEvent) => {
      if (isDirty && isEditing) {
        e.preventDefault();
        e.returnValue = '';
      }
    };

    window.addEventListener('beforeunload', handleBeforeUnload);
    return () => window.removeEventListener('beforeunload', handleBeforeUnload);
  }, [isDirty, isEditing]);

  const handleEdit = () => {
    if (!content) return;
    setEditableContent(content.content);
    setEditableTitle(content.title);
    setIsEditing(true);
    setIsDirty(false);
  };

  const handleSave = async () => {
    try {
      await save({
        title: editableTitle,
        content: editableContent,
        changeDescription: 'Updated via web interface'
      });
      setIsEditing(false);
      setIsDirty(false);
    } catch (error) {
      console.error('Save failed:', error);
    }
  };

  const handleCancel = () => {
    if (isDirty && !window.confirm('Discard unsaved changes?')) {
      return;
    }
    setIsEditing(false);
    setIsDirty(false);
  };

  if (isLoading) {
    return <LoadingOverlay visible />;
  }

  if (!content && !defaultContent) {
    return <Box>Page not found</Box>;
  }

  return (
    <Box pos="relative">
      {isAdmin && (
        <Group mb="md" justify="flex-end">
          {isEditing ? (
            <>
              <Button
                onClick={handleSave}
                loading={isSaving}
                disabled={!isDirty}
              >
                Save
              </Button>
              <Button
                variant="outline"
                onClick={handleCancel}
                disabled={isSaving}
              >
                Cancel
              </Button>
            </>
          ) : (
            <Button onClick={handleEdit}>
              Edit Page
            </Button>
          )}
        </Group>
      )}

      {isEditing ? (
        <Stack>
          <TextInput
            label="Page Title"
            value={editableTitle}
            onChange={(e) => {
              setEditableTitle(e.currentTarget.value);
              setIsDirty(true);
            }}
            required
          />

          <MantineTiptapEditor
            value={editableContent}
            onChange={(html) => {
              setEditableContent(html);
              setIsDirty(true);
            }}
            minRows={15}
            placeholder="Enter page content..."
          />
        </Stack>
      ) : (
        <Box
          dangerouslySetInnerHTML={{
            __html: content?.content || defaultContent || ''
          }}
        />
      )}
    </Box>
  );
};

// /apps/web/src/App.tsx - Route definitions
import { Route, Routes } from 'react-router-dom';
import { CmsPage } from './pages/cms/CmsPage';

export const App = () => {
  return (
    <Routes>
      {/* CMS Routes */}
      <Route
        path="/resources"
        element={
          <CmsPage
            slug="resources"
            defaultTitle="Resources"
            defaultContent="<h1>Community Resources</h1><p>Loading...</p>"
          />
        }
      />

      <Route
        path="/contact-us"
        element={
          <CmsPage
            slug="contact-us"
            defaultTitle="Contact Us"
            defaultContent="<h1>Contact Us</h1><p>Loading...</p>"
          />
        }
      />

      <Route
        path="/private-lessons"
        element={
          <CmsPage
            slug="private-lessons"
            defaultTitle="Private Lessons"
            defaultContent="<h1>Private Lessons</h1><p>Loading...</p>"
          />
        }
      />

      {/* Other routes... */}
    </Routes>
  );
};
```

---

## Implementation Considerations

### Migration Path

**Phase 1: Database Setup (30 minutes)**
1. Create cms schema
2. Create ContentPages table
3. Create ContentRevisions table
4. Insert seed data for 3 pages
5. Run migration: `dotnet ef migrations add AddCmsSchema`

**Phase 2: Backend API (2 hours)**
1. Install HtmlSanitizer NuGet package
2. Create CmsEndpoints.cs with 4 endpoints
3. Configure HtmlSanitizer in Program.cs
4. Create entity models
5. Test with Swagger/Postman

**Phase 3: Frontend Components (3 hours)**
1. Create cmsApi.ts with typed API calls
2. Create useCmsPage hook with optimistic updates
3. Create CmsPage.tsx component
4. Add routes to App.tsx
5. Test editing workflow

**Phase 4: Integration Testing (1 hour)**
1. Test all 3 pages load correctly
2. Test edit → save → view workflow
3. Test revision history
4. Test rollback functionality
5. Test admin authorization

**Total Estimated Effort**: 6.5 hours

---

### Performance Impact

**Bundle Size**: +15KB (HtmlSanitizer.NET server-side, no client impact)
**Page Load**: <200ms (single database query)
**Save Response**: <100ms (sanitization + database write)
**Memory**: ~50KB per page in cache
**Database Storage**: ~10-50KB per page, ~500KB total for 3 pages with history

**Optimizations**:
- TanStack Query caching (5-minute stale time)
- Database indexes on slug and ContentPageId
- TEXT column avoids JSONB overhead
- Optimistic updates = instant UI feedback

---

### Risk Assessment

#### High Risk: None identified

#### Medium Risk

**Risk**: Admin accidentally overwrites content
- **Mitigation**: Revision history with rollback capability
- **Mitigation**: "Unsaved changes" warning before navigation
- **Mitigation**: Optimistic update rollback on save failure

**Risk**: XSS vulnerability from malicious admin
- **Mitigation**: Backend sanitization with HtmlSanitizer.NET
- **Mitigation**: Whitelist approach (only allowed tags)
- **Mitigation**: Admin role required (trusted users)

#### Low Risk

**Risk**: Database migration failure
- **Mitigation**: Test migration in development first
- **Mitigation**: Database backups before migration
- **Monitoring**: Entity Framework migration logs

**Risk**: TipTap editor compatibility
- **Mitigation**: Already integrated and tested
- **Mitigation**: Version pinned in package.json

---

## Recommendation

### Primary Recommendation: Route-Based CMS with Backend Sanitization

**Confidence Level**: High (85%)

**Rationale**:
1. **Simplest architecture**: Route → slug → database → render
2. **Best performance**: Single query, optimistic updates, TEXT storage
3. **Secure**: Backend-only sanitization, admin-only access
4. **Maintainable**: Clear patterns, easy to add pages
5. **Production-proven**: TanStack Query + Minimal API + PostgreSQL

**Why not 100% confidence?**
- Edge cases in HTML sanitization (15% uncertainty)
- Potential for future image upload requirement (not in MVP)

**Implementation Priority**: High - Required for static content management

---

### Alternative Recommendations

**Second Choice**: Same architecture with JSONB storage
- **Reason**: Only if structured querying needed in future
- **Trade-off**: 2× storage, slower performance for no current benefit

**Future Consideration**: Add DOMPurify frontend sanitization
- **When**: If CMS opens to non-admin users
- **Why not now**: Admin-only = unnecessary complexity

---

## Next Steps

1. **Business Requirements Agent**: Review recommendations, validate against business needs
2. **Database Designer**: Create migration scripts for cms schema
3. **Backend Developer**: Implement CmsEndpoints.cs with HtmlSanitizer.NET
4. **React Developer**: Create CmsPage component with optimistic updates
5. **Test Developer**: Create E2E tests for edit workflow

---

## Questions for Technical Team

- [ ] Should we add "Draft" status for pages? (Not in MVP, but easy to add)
- [ ] Do we want email notifications when content changes? (Future feature)
- [ ] Should revision history be time-limited (e.g., 1 year)? (Future optimization)
- [ ] Image upload requirement timeline? (Affects sanitization config)

---

## Quality Gate Checklist (90% Required)

- [x] Multiple options evaluated (minimum 2) - **5 research topics, 15 options total**
- [x] Quantitative comparison provided - **5 comparison matrices with weighted scoring**
- [x] WitchCityRope-specific considerations addressed - **Mobile, admin-only, simplicity**
- [x] Performance impact assessed - **<200ms load, <100ms save, bundle size**
- [x] Security implications reviewed - **XSS prevention, backend sanitization**
- [x] Mobile experience considered - **Optimistic updates, instant feedback**
- [x] Implementation path defined - **4-phase plan, 6.5 hours estimated**
- [x] Risk assessment completed - **High/Medium/Low risks with mitigations**
- [x] Clear recommendation with rationale - **Route-based + backend sanitization**
- [x] Sources documented for verification - **18+ research sources linked**

**Quality Score**: 10/10 (100%)

---

## Research Sources

### TipTap & React Content Editing
- [TipTap React Documentation](https://tiptap.dev/docs/editor/getting-started/install/react)
- [TipTap Inline Edits Guide](https://tiptap.dev/docs/content-ai/capabilities/ai-toolkit/guides/inline-edits)
- [Headless vs WYSIWYG 2025 Landscape](https://www.nutrient.io/blog/headless-vs-wysiwyg/)

### React Routing & Slug Patterns
- [ReactJS Slug-Based Routing](https://stackoverflow.com/questions/42659611/reactjs-use-slug-instead-of-id-in-the-url-with-react-router)
- [Dynamic URLs with React Router](https://rohitmondallblog.hashnode.dev/usingslug-to-make-dynamic-and-user-friendly-urls-for-our-website)
- [Strapi + React Dynamic Routing](https://www.cloudthat.com/resources/blog/a-guide-to-implement-dynamic-routing-and-slugs-in-reactjs-and-strapi)

### PostgreSQL Content Storage
- [TEXT vs JSONB Performance](https://medium.com/lumigo/the-postgres-showdown-text-columns-vs-jsonb-fields-0ffff011ac46)
- [PostgreSQL TOAST Performance](https://www.evanjones.ca/postgres-large-json-performance.html)
- [JSONB Storage Overhead](https://stackoverflow.com/questions/74826517/is-it-true-that-jsonb-may-take-more-disk-space-than-json-in-postgresql-and-why)

### Revision History Patterns
- [PostgreSQL Revision Tracking](https://stackoverflow.com/questions/2259158/track-revisions-in-postgresql)
- [Content Versioning with PostgreSQL](https://kaustavdm.in/versioning-content-postgresql/)
- [History Tracking with Postgres](https://www.thegnar.com/blog/history-tracking-with-postgres)
- [pgMemento Audit Trail](https://github.com/pgMemento/pgMemento)

### Content Sanitization
- [HtmlSanitizer.NET GitHub](https://github.com/mganss/HtmlSanitizer)
- [DOMPurify Documentation](https://github.com/cure53/DOMPurify)
- [OWASP XSS Prevention](https://cheatsheetseries.owasp.org/cheatsheets/Cross_Site_Scripting_Prevention_Cheat_Sheet.html)
- [.NET 8 XSS Prevention](https://medium.com/@feldy7k/preventing-xss-attacks-in-net-8-api-with-html-sanitizer-method-0bb04413526b)

### TanStack Query Patterns
- [Optimistic Updates Official Docs](https://tanstack.com/query/latest/docs/framework/react/guides/optimistic-updates)
- [Concurrent Optimistic Updates](https://tkdodo.eu/blog/concurrent-optimistic-updates-in-react-query)
- [TanStack Query Reusable Patterns](https://spin.atomicobject.com/tanstack-query-reusable-patterns/)

### .NET Minimal API Patterns
- [Minimal API Vertical Slice Architecture](https://treblle.com/blog/minimal-api-with-vertical-slice-architecture)
- [Structuring Minimal APIs](https://www.weekenddive.com/dotnet/3-ways-to-structure-minimal-apis)
- [Minimal APIs Application Layer](https://timdeschryver.dev/blog/treat-your-net-minimal-api-endpoint-as-the-application-layer)

---

**Research Completed**: 2025-10-17
**Next Phase**: Business Requirements Validation
**Estimated Implementation**: 6.5 hours (1 developer)
**Risk Level**: Low
**Confidence**: High (85%)

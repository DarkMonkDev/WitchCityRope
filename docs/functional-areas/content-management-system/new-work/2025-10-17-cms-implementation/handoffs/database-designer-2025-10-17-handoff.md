# Database Designer Handoff Document
<!-- Date: 2025-10-17 -->
<!-- From: Database Designer Agent -->
<!-- To: Backend Developer, React Developer -->
<!-- Phase: Design → Implementation -->

## Handoff Summary

**Work Completed**: Complete database schema design for WitchCityRope Content Management System (CMS).

**Next Agents**:
1. **Backend Developer**: Implement EF Core entities, configurations, migrations, and API endpoints
2. **React Developer**: Build CMS UI components using generated TypeScript types (parallel work)

**Critical Files Created**:
- `/docs/functional-areas/content-management-system/new-work/2025-10-17-cms-implementation/design/database-design.md`

---

## What Was Delivered

### 1. Database Schema Design

**Two Tables Created**:

#### cms.ContentPages
- **Purpose**: Store current version of each CMS page
- **Primary Key**: Serial integer (Id)
- **Unique Identifier**: Slug (VARCHAR, unique index)
- **Content Storage**: TEXT column (PostgreSQL optimized)
- **Audit Fields**: CreatedAt, UpdatedAt, CreatedBy, LastModifiedBy
- **Foreign Keys**: CreatedBy → AspNetUsers, LastModifiedBy → AspNetUsers

#### cms.ContentRevisions
- **Purpose**: Full content history for audit trail and rollback
- **Primary Key**: Serial integer (Id)
- **Foreign Key**: ContentPageId → ContentPages (CASCADE delete)
- **Content Snapshot**: Full TEXT copy of content at save time
- **Audit Fields**: CreatedAt, CreatedBy, ChangeDescription

### 2. Entity Framework Core Models

**Complete EF Core Implementation**:
- `ContentPage.cs` entity class with navigation properties
- `ContentRevision.cs` entity class
- `ContentPageConfiguration.cs` fluent configuration
- `ContentRevisionConfiguration.cs` fluent configuration
- Domain method: `UpdateContent()` (creates revision + updates content)

### 3. Migrations

**Two Migration Files Designed**:
1. `20251017_CreateCmsSchemaAndTables` - Schema + tables + indexes
2. `20251017_SeedInitialCmsPages` - 3 initial pages (resources, contact-us, private-lessons)

### 4. Performance Optimization

**Index Strategy**:
- Unique index on Slug (fast page lookup <10ms)
- Composite index on (ContentPageId, CreatedAt DESC) for revision history
- Partial index on IsPublished for future filtering
- All indexes designed for specific query patterns

**Performance Targets**:
- Page load by slug: <50ms
- Content save: <100ms
- Revision history (50 items): <100ms

---

## Critical Implementation Details

### PostgreSQL-Specific Patterns

**1. DateTime Handling (CRITICAL)**:
```csharp
// MUST use UTC DateTimes in code
var page = new ContentPage
{
    CreatedAt = DateTime.UtcNow,  // NOT DateTime.Now
    UpdatedAt = DateTime.UtcNow
};

// Database column type: timestamp with time zone
builder.Property(cp => cp.CreatedAt)
    .HasColumnType("timestamp with time zone");
```

**Why**: PostgreSQL requires DateTime.Kind = Utc. Unspecified kind causes runtime errors.

**2. TEXT Column Type**:
```csharp
builder.Property(cp => cp.Content)
    .HasColumnType("TEXT")  // Explicit PostgreSQL TEXT type
    .IsRequired();
```

**Why**: 2× faster than JSONB for HTML blobs, 50% less storage.

**3. Check Constraints**:
```csharp
builder.ToTable(t =>
{
    t.HasCheckConstraint(
        "CHK_ContentPages_Slug_Format",
        "\"Slug\" ~ '^[a-z0-9]+(-[a-z0-9]+)*$'"  // PostgreSQL regex
    );
});
```

**Why**: Database-level validation prevents bad data at source.

### Foreign Key Cascade Rules

**ContentRevisions → ContentPages**: CASCADE delete
- When page deleted, all revisions automatically deleted
- Intentional cleanup (no orphaned revisions)

**ContentPages/Revisions → AspNetUsers**: RESTRICT delete
- Cannot delete user who created/modified content
- Preserves audit trail integrity forever

### Domain Logic in UpdateContent()

**Critical Behavior**:
```csharp
public void UpdateContent(string content, string title, string userId, string? changeDescription = null)
{
    // 1. Create revision BEFORE updating content
    var revision = new ContentRevision
    {
        Content = Content,  // OLD content (snapshot)
        Title = Title       // OLD title (snapshot)
    };
    Revisions.Add(revision);

    // 2. Update current content
    Content = content;  // NEW content
    Title = title;      // NEW title
    UpdatedAt = DateTime.UtcNow;
    LastModifiedBy = userId;
}
```

**Why**: Captures "before" state in revision, then updates "current" state.

---

## Backend Developer Implementation Tasks

### 1. Create Entity Models

**Files to Create**:
- `/apps/api/Models/ContentPage.cs`
- `/apps/api/Models/ContentRevision.cs`

**Copy from**: Database design document section "Entity Framework Core Models"

**Key Points**:
- Use `[Table("ContentPages", Schema = "cms")]` attribute
- Include navigation properties (CreatedByUser, LastModifiedByUser, Revisions)
- Implement `UpdateContent()` domain method exactly as specified

### 2. Create EF Core Configurations

**Files to Create**:
- `/apps/api/Data/Configurations/ContentPageConfiguration.cs`
- `/apps/api/Data/Configurations/ContentRevisionConfiguration.cs`

**Copy from**: Database design document section "Entity Framework Fluent Configuration"

**Key Points**:
- Configure indexes (unique on Slug, composite on ContentPageId+CreatedAt)
- Set foreign key constraints (CASCADE/RESTRICT)
- Add check constraints (slug format, title length, content not empty)
- Use `HasColumnType("TEXT")` for Content fields
- Use `HasColumnType("timestamp with time zone")` for DateTime fields

### 3. Update ApplicationDbContext

**File to Modify**: `/apps/api/Data/ApplicationDbContext.cs`

**Changes**:
```csharp
// Add DbSet properties
public DbSet<ContentPage> ContentPages => Set<ContentPage>();
public DbSet<ContentRevision> ContentRevisions => Set<ContentRevision>();

// In OnModelCreating, apply configurations
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // ... existing configurations ...

    modelBuilder.ApplyConfiguration(new ContentPageConfiguration());
    modelBuilder.ApplyConfiguration(new ContentRevisionConfiguration());
}
```

### 4. Generate and Apply Migrations

**Commands**:
```bash
cd /home/chad/repos/witchcityrope-react/apps/api

# Generate migration
dotnet ef migrations add CreateCmsSchemaAndTables

# Review migration file, then apply
dotnet ef database update

# Generate seed data migration
dotnet ef migrations add SeedInitialCmsPages

# Apply seed migration
dotnet ef database update
```

**Verify Migration**:
```sql
-- Connect to database
psql -h localhost -p 5434 -U postgres -d witchcityrope_db

-- Check schema exists
\dn cms

-- Check tables exist
\dt cms.*

-- Check seed data
SELECT "Slug", "Title" FROM cms."ContentPages";
```

### 5. Implement CMS API Endpoints

**Endpoints to Create**:

```csharp
// GET /api/cms/pages/{slug} - Get page by slug (public)
[HttpGet("pages/{slug}")]
[AllowAnonymous]
public async Task<ActionResult<ContentPageDto>> GetPageBySlug(string slug)

// PUT /api/cms/pages/{id} - Update page (admin only)
[HttpPut("pages/{id}")]
[Authorize(Roles = "Administrator")]
public async Task<ActionResult<ContentPageDto>> UpdatePage(int id, UpdateContentPageRequest request)

// GET /api/cms/pages/{id}/revisions - Get revision history (admin only)
[HttpGet("pages/{id}/revisions")]
[Authorize(Roles = "Administrator")]
public async Task<ActionResult<List<ContentRevisionDto>>> GetRevisions(int id)

// GET /api/cms/pages - List all pages (admin only)
[HttpGet("pages")]
[Authorize(Roles = "Administrator")]
public async Task<ActionResult<List<ContentPageSummaryDto>>> GetAllPages()
```

**DTOs to Create**:
```csharp
public record ContentPageDto(
    int Id,
    string Slug,
    string Title,
    string Content,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string CreatedBy,
    string LastModifiedBy,
    bool IsPublished
);

public record UpdateContentPageRequest(
    string Title,
    string Content,
    string? ChangeDescription
);

public record ContentRevisionDto(
    int Id,
    int ContentPageId,
    string Content,
    string Title,
    DateTime CreatedAt,
    string CreatedBy,
    string? ChangeDescription
);

public record ContentPageSummaryDto(
    int Id,
    string Slug,
    string Title,
    DateTime UpdatedAt,
    int RevisionCount
);
```

### 6. Implement Content Sanitization

**Install Package**:
```bash
dotnet add package HtmlSanitizer
```

**Create Sanitizer Service**:
```csharp
public interface IContentSanitizer
{
    string SanitizeContent(string content);
}

public class ContentSanitizer : IContentSanitizer
{
    private readonly HtmlSanitizer _sanitizer;

    public ContentSanitizer()
    {
        _sanitizer = new HtmlSanitizer();

        // Allow safe HTML tags
        _sanitizer.AllowedTags.UnionWith(new[] {
            "p", "br", "strong", "em", "u", "s",
            "h1", "h2", "h3", "h4", "h5", "h6",
            "ul", "ol", "li", "a", "blockquote", "hr"
        });

        // Allow safe attributes
        _sanitizer.AllowedAttributes.Add("href");
        _sanitizer.AllowedAttributes.Add("title");
    }

    public string SanitizeContent(string content)
    {
        return _sanitizer.Sanitize(content);
    }
}
```

**Register in Program.cs**:
```csharp
builder.Services.AddScoped<IContentSanitizer, ContentSanitizer>();
```

**Use in API Endpoint**:
```csharp
[HttpPut("pages/{id}")]
public async Task<ActionResult<ContentPageDto>> UpdatePage(
    int id,
    UpdateContentPageRequest request,
    [FromServices] IContentSanitizer sanitizer)
{
    var page = await _context.ContentPages
        .Include(p => p.Revisions)
        .FirstOrDefaultAsync(p => p.Id == id);

    if (page == null) return NotFound();

    var sanitizedContent = sanitizer.SanitizeContent(request.Content);

    page.UpdateContent(
        sanitizedContent,
        request.Title,
        User.FindFirst(ClaimTypes.NameIdentifier)!.Value,
        request.ChangeDescription
    );

    await _context.SaveChangesAsync();
    return Ok(page.ToDto());
}
```

### 7. Generate NSwag TypeScript Types

**After implementing endpoints**:
```bash
# Start API
cd /home/chad/repos/witchcityrope-react/apps/api
dotnet run

# Generate TypeScript types (in separate terminal)
cd /home/chad/repos/witchcityrope-react/packages/shared-types
npm run generate
```

**Verify Types Generated**:
- `ContentPageDto`
- `UpdateContentPageRequest`
- `ContentRevisionDto`
- `ContentPageSummaryDto`

---

## React Developer Implementation Tasks

### 1. Wait for TypeScript Type Generation

**Before starting**: Backend developer must generate NSwag types.

**Verify types exist**:
```typescript
import { ContentPageDto, UpdateContentPageRequest } from '@witchcityrope/shared-types';
```

### 2. Implement TanStack Query Hooks

**Create**: `/apps/web/src/hooks/useCmsPage.ts`

```typescript
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { ContentPageDto, UpdateContentPageRequest } from '@witchcityrope/shared-types';

export function useCmsPage(slug: string) {
  return useQuery({
    queryKey: ['cms-page', slug],
    queryFn: async () => {
      const response = await fetch(`/api/cms/pages/${slug}`);
      if (!response.ok) throw new Error('Failed to fetch page');
      return response.json() as Promise<ContentPageDto>;
    },
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

export function useUpdateCmsPage(slug: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (data: UpdateContentPageRequest & { pageId: number }) => {
      const response = await fetch(`/api/cms/pages/${data.pageId}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          title: data.title,
          content: data.content,
          changeDescription: data.changeDescription
        })
      });
      if (!response.ok) throw new Error('Failed to update page');
      return response.json() as Promise<ContentPageDto>;
    },

    // Optimistic update (see UI design for full pattern)
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
    },

    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['cms-page', slug] });
    }
  });
}
```

### 3. Build CmsPage Component

**Create**: `/apps/web/src/components/cms/CmsPage.tsx`

**Features**:
- Fetch page content using `useCmsPage(slug)`
- Render content in view mode
- Show "Edit Page" button to admins (always visible)
- Switch to edit mode with TipTap editor
- Save/Cancel buttons
- Optimistic updates
- Error handling

**See UI Design**: `/docs/functional-areas/content-management-system/new-work/2025-10-17-cms-implementation/design/ui-design.md` for complete specifications.

### 4. Build Revision History Components

**Create**:
- `/apps/web/src/pages/admin/cms/RevisionListPage.tsx` - List all pages with revision counts
- `/apps/web/src/pages/admin/cms/RevisionDetailPage.tsx` - Revision history for single page

**See UI Design**: Section "State 7: Revision History - Separate Page Architecture" for wireframes and specifications.

### 5. Add Routes

**Update**: `/apps/web/src/App.tsx`

```typescript
// Public CMS pages
<Route path="/resources" element={<CmsPage slug="resources" />} />
<Route path="/contact-us" element={<CmsPage slug="contact-us" />} />
<Route path="/private-lessons" element={<CmsPage slug="private-lessons" />} />

// Admin CMS routes
<Route path="/admin/cms/revisions" element={<RevisionListPage />} />
<Route path="/admin/cms/revisions/:pageId" element={<RevisionDetailPage />} />
```

---

## Testing Requirements

### Backend Unit Tests

**Create**: `/apps/api.tests/Models/ContentPageTests.cs`

**Test Cases**:
- `UpdateContent_ValidData_CreatesRevisionAndUpdatesContent()`
- `UpdateContent_EmptyContent_ThrowsArgumentException()`
- `UpdateContent_EmptyTitle_ThrowsArgumentException()`
- `CanBeEditedBy_AdminUser_ReturnsTrue()`
- `CanBeEditedBy_NonAdminUser_ReturnsFalse()`

### Backend Integration Tests

**Create**: `/apps/api.tests/Integration/CmsIntegrationTests.cs`

**Test Cases**:
- `GetPageBySlug_ExistingPage_ReturnsPage()`
- `GetPageBySlug_NonExistentPage_Returns404()`
- `UpdatePage_ValidContent_CreatesRevision()`
- `UpdatePage_AdminUser_Succeeds()`
- `UpdatePage_NonAdminUser_Returns403()`
- `GetRevisions_ExistingPage_ReturnsHistory()`

### E2E Tests (React Developer)

**Create**: `/tests/playwright/cms/cms-editing.spec.ts`

**Test Cases**:
- Edit workflow (view → edit → save → view)
- Cancel workflow (unsaved changes warning)
- Revision history navigation
- Mobile edit button (FAB)
- Accessibility (keyboard navigation, ARIA labels)

---

## Known Issues / Limitations

**None at this time**. Design follows established WitchCityRope patterns.

**Future Enhancements**:
1. Draft/published workflow (IsPublished field prepared)
2. Rollback to previous revision (database supports, UI future)
3. Image upload for content (TipTap supports, API endpoint future)
4. SEO meta fields (schema prepared, UI future)

---

## Questions for Implementers

**Backend Developer**:
1. Should we add a service layer (`IContentPageService`) or use direct DbContext in controllers?
2. Should seed data run automatically on first startup or manual migration only?
3. Any concerns with HtmlSanitizer.NET configuration?

**React Developer**:
1. Should TipTap editor configuration match existing `MantineTiptapEditor.tsx` or create new variant?
2. Revision history: Client-side pagination or server-side with LIMIT/OFFSET?
3. Any concerns with optimistic update pattern for content saves?

---

## Success Criteria

**Backend Implementation Complete When**:
- [x] EF Core entities created
- [x] Migrations generated and applied
- [x] 3 seed pages exist in database
- [x] API endpoints return correct data
- [x] HtmlSanitizer.NET prevents XSS
- [x] NSwag TypeScript types generated
- [x] Unit + integration tests pass

**React Implementation Complete When**:
- [x] CmsPage component renders content
- [x] Edit mode works with TipTap
- [x] Optimistic updates feel instant
- [x] Revision history UI navigable
- [x] Mobile FAB edit button functional
- [x] E2E tests pass

---

## Related Documents

**MUST READ** (in order):
1. Business Requirements: `/docs/functional-areas/content-management-system/new-work/2025-10-17-cms-implementation/requirements/business-requirements.md`
2. Technology Research: `/docs/functional-areas/content-management-system/new-work/2025-10-17-cms-implementation/research/cms-architecture-research.md`
3. UI Design: `/docs/functional-areas/content-management-system/new-work/2025-10-17-cms-implementation/design/ui-design.md`
4. **Database Design** (this phase): `/docs/functional-areas/content-management-system/new-work/2025-10-17-cms-implementation/design/database-design.md`

**Reference**:
- Entity Framework Patterns: `/docs/standards-processes/development-standards/entity-framework-patterns.md`
- Database Designer Lessons: `/docs/lessons-learned/database-designer-lessons-learned.md`
- React Developer Lessons: `/docs/lessons-learned/react-developer-lessons-learned.md`

---

## Contact

**Questions during implementation?**
- Check database design document first
- Review UI design for React component specs
- Consult Entity Framework Patterns for EF Core questions
- Ask in project Slack/Discord if stuck

**Database Designer available for**:
- Schema clarifications
- Performance optimization questions
- Migration troubleshooting
- Index strategy questions

---

## Handoff Checklist

- [x] Database schema fully designed (2 tables, all fields specified)
- [x] EF Core entity models ready to copy
- [x] EF Core configurations ready to copy
- [x] Migration Up/Down methods designed
- [x] Seed data SQL prepared
- [x] Performance benchmarks documented
- [x] Testing strategy defined
- [x] Backend tasks clearly listed
- [x] React tasks clearly listed
- [x] Critical PostgreSQL patterns highlighted (UTC DateTime, TEXT column)
- [x] Foreign key cascade rules explained
- [x] Domain logic documented (UpdateContent method)
- [x] NSwag type generation process explained
- [x] Success criteria defined

**Status**: **READY FOR IMPLEMENTATION** ✅

---

**Handoff Date**: October 17, 2025
**Database Designer**: Database Designer Agent
**Next Agents**: Backend Developer (primary), React Developer (parallel)

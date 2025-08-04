# WitchCityRope CMS System - Technical Design

## Database Schema

### ContentPage Entity

```sql
CREATE TABLE cms.ContentPages (
    Id SERIAL PRIMARY KEY,
    Slug VARCHAR(100) NOT NULL UNIQUE,
    Title VARCHAR(200) NOT NULL,
    Content TEXT NOT NULL,
    CreatedAt TIMESTAMP WITH TIME ZONE NOT NULL,
    UpdatedAt TIMESTAMP WITH TIME ZONE NOT NULL,
    CreatedBy VARCHAR(450) NOT NULL,
    LastModifiedBy VARCHAR(450) NOT NULL,
    IsPublished BOOLEAN NOT NULL DEFAULT true,
    MetaDescription VARCHAR(500),
    MetaKeywords VARCHAR(500),
    
    CONSTRAINT FK_ContentPages_CreatedBy FOREIGN KEY (CreatedBy) 
        REFERENCES auth.AspNetUsers(Id) ON DELETE RESTRICT,
    CONSTRAINT FK_ContentPages_LastModifiedBy FOREIGN KEY (LastModifiedBy) 
        REFERENCES auth.AspNetUsers(Id) ON DELETE RESTRICT
);

CREATE INDEX IX_ContentPages_Slug ON cms.ContentPages(Slug);
CREATE INDEX IX_ContentPages_IsPublished ON cms.ContentPages(IsPublished);
```

### ContentRevision Entity

```sql
CREATE TABLE cms.ContentRevisions (
    Id SERIAL PRIMARY KEY,
    ContentPageId INTEGER NOT NULL,
    Content TEXT NOT NULL,
    CreatedAt TIMESTAMP WITH TIME ZONE NOT NULL,
    CreatedBy VARCHAR(450) NOT NULL,
    ChangeDescription VARCHAR(500),
    
    CONSTRAINT FK_ContentRevisions_ContentPage FOREIGN KEY (ContentPageId) 
        REFERENCES cms.ContentPages(Id) ON DELETE CASCADE,
    CONSTRAINT FK_ContentRevisions_CreatedBy FOREIGN KEY (CreatedBy) 
        REFERENCES auth.AspNetUsers(Id) ON DELETE RESTRICT
);

CREATE INDEX IX_ContentRevisions_ContentPageId ON cms.ContentRevisions(ContentPageId);
CREATE INDEX IX_ContentRevisions_CreatedAt ON cms.ContentRevisions(CreatedAt);
```

## Domain Models

### ContentPage.cs

```csharp
namespace WitchCityRope.Core.Entities
{
    public class ContentPage : BaseEntity
    {
        public string Slug { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string LastModifiedBy { get; set; } = string.Empty;
        public bool IsPublished { get; set; } = true;
        public string? MetaDescription { get; set; }
        public string? MetaKeywords { get; set; }
        
        // Navigation properties
        public WitchCityRopeUser? CreatedByUser { get; set; }
        public WitchCityRopeUser? LastModifiedByUser { get; set; }
        public ICollection<ContentRevision> Revisions { get; set; } = new List<ContentRevision>();
        
        // Domain methods
        public void UpdateContent(string content, string title, string userId, string? changeDescription = null)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Content cannot be empty", nameof(content));
                
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Title cannot be empty", nameof(title));
            
            // Create revision before changing
            var revision = new ContentRevision
            {
                ContentPageId = Id,
                Content = Content,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId,
                ChangeDescription = changeDescription ?? "Content updated"
            };
            
            Revisions.Add(revision);
            
            // Update content
            Content = content;
            Title = title;
            UpdatedAt = DateTime.UtcNow;
            LastModifiedBy = userId;
        }
        
        public bool CanBeEditedBy(string userId, IEnumerable<string> userRoles)
        {
            return userRoles.Contains("Administrator");
        }
    }
}
```

### ContentRevision.cs

```csharp
namespace WitchCityRope.Core.Entities
{
    public class ContentRevision : BaseEntity
    {
        public int ContentPageId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? ChangeDescription { get; set; }
        
        // Navigation properties
        public ContentPage? ContentPage { get; set; }
        public WitchCityRopeUser? CreatedByUser { get; set; }
    }
}
```

## Service Layer

### IContentPageService.cs

```csharp
namespace WitchCityRope.Core.Services
{
    public interface IContentPageService
    {
        Task<ContentPage?> GetBySlugAsync(string slug);
        Task<ContentPage?> GetByIdAsync(int id);
        Task<IEnumerable<ContentPage>> GetAllAsync();
        Task<ContentPage> CreateAsync(CreateContentPageRequest request, string userId);
        Task<ContentPage> UpdateAsync(int id, UpdateContentPageRequest request, string userId);
        Task<bool> DeleteAsync(int id, string userId);
        Task<IEnumerable<ContentRevision>> GetRevisionsAsync(int contentPageId);
        Task<bool> SlugExistsAsync(string slug, int? excludeId = null);
        Task<ContentPage> GetOrCreateBySlugAsync(string slug, string defaultTitle, string defaultContent, string userId);
    }
}
```

### ContentPageService.cs

```csharp
namespace WitchCityRope.Infrastructure.Services
{
    public class ContentPageService : IContentPageService
    {
        private readonly ApplicationDbContext _context;
        private readonly IContentSanitizer _sanitizer;
        private readonly ILogger<ContentPageService> _logger;
        
        public ContentPageService(
            ApplicationDbContext context,
            IContentSanitizer sanitizer,
            ILogger<ContentPageService> logger)
        {
            _context = context;
            _sanitizer = sanitizer;
            _logger = logger;
        }
        
        public async Task<ContentPage?> GetBySlugAsync(string slug)
        {
            return await _context.ContentPages
                .Include(x => x.CreatedByUser)
                .Include(x => x.LastModifiedByUser)
                .FirstOrDefaultAsync(x => x.Slug == slug && x.IsPublished);
        }
        
        public async Task<ContentPage> GetOrCreateBySlugAsync(string slug, string defaultTitle, string defaultContent, string userId)
        {
            var existing = await GetBySlugAsync(slug);
            if (existing != null)
                return existing;
                
            var contentPage = new ContentPage
            {
                Slug = slug,
                Title = defaultTitle,
                Content = _sanitizer.SanitizeContent(defaultContent),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = userId,
                LastModifiedBy = userId,
                IsPublished = true
            };
            
            _context.ContentPages.Add(contentPage);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Created new content page: {Slug}", slug);
            
            return contentPage;
        }
        
        public async Task<ContentPage> UpdateAsync(int id, UpdateContentPageRequest request, string userId)
        {
            var contentPage = await _context.ContentPages
                .Include(x => x.Revisions)
                .FirstOrDefaultAsync(x => x.Id == id);
                
            if (contentPage == null)
                throw new NotFoundException($"Content page with ID {id} not found");
            
            var sanitizedContent = _sanitizer.SanitizeContent(request.Content);
            
            contentPage.UpdateContent(sanitizedContent, request.Title, userId, request.ChangeDescription);
            
            if (!string.IsNullOrWhiteSpace(request.MetaDescription))
                contentPage.MetaDescription = request.MetaDescription;
                
            if (!string.IsNullOrWhiteSpace(request.MetaKeywords))
                contentPage.MetaKeywords = request.MetaKeywords;
            
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Updated content page: {Title} by user {UserId}", request.Title, userId);
            
            return contentPage;
        }
        
        // ... other implementations
    }
}
```

## Component Architecture

### BaseCmsPage.razor

```razor
@inherits ComponentBase
@inject IContentPageService ContentPageService
@inject AuthenticationStateProvider AuthenticationStateProvider
@inject IJSRuntime JSRuntime
@inject IToastService ToastService
@implements IDisposable

<div class="cms-page-wrapper">
    @if (IsAdmin)
    {
        <div class="cms-controls @(IsEditing ? "editing" : "")">
            @if (IsEditing)
            {
                <div class="cms-edit-toolbar">
                    <button class="btn btn-success" @onclick="SaveContentAsync" disabled="@IsSaving">
                        @if (IsSaving)
                        {
                            <span class="spinner-border spinner-border-sm me-2"></span>
                        }
                        <i class="fas fa-save me-1"></i> Save
                    </button>
                    <button class="btn btn-secondary" @onclick="CancelEditAsync">
                        <i class="fas fa-times me-1"></i> Cancel
                    </button>
                </div>
            }
            else
            {
                <div class="cms-view-controls">
                    <button class="btn btn-outline-primary btn-sm" @onclick="StartEditAsync">
                        <i class="fas fa-edit me-1"></i> Edit Page
                    </button>
                </div>
            }
        </div>
    }
    
    <div class="cms-content-container">
        @if (IsEditing)
        {
            <div class="cms-editor-container">
                <div class="mb-3">
                    <label class="form-label">Page Title</label>
                    <input type="text" class="form-control" @bind="EditableTitle" />
                </div>
                
                <div class="mb-3">
                    <label class="form-label">Content</label>
                    <SfRichTextEditor @bind-Value="EditableContent" 
                                      Height="600px"
                                      @ref="RichTextEditorRef"
                                      Placeholder="Enter your content here...">
                        <RichTextEditorToolbarSettings Items="@ToolbarItems" />
                        <RichTextEditorImageSettings SaveUrl="/api/cms/upload" />
                    </SfRichTextEditor>
                </div>
                
                <div class="row">
                    <div class="col-md-6">
                        <label class="form-label">Meta Description</label>
                        <textarea class="form-control" rows="3" @bind="EditableMetaDescription" 
                                  placeholder="Page description for SEO"></textarea>
                    </div>
                    <div class="col-md-6">
                        <label class="form-label">Meta Keywords</label>
                        <textarea class="form-control" rows="3" @bind="EditableMetaKeywords" 
                                  placeholder="Keywords separated by commas"></textarea>
                    </div>
                </div>
            </div>
        }
        else
        {
            <div class="cms-content-display">
                @if (ContentPage != null)
                {
                    @((MarkupString)ContentPage.Content)
                }
                else if (ChildContent != null)
                {
                    @ChildContent
                }
            </div>
        }
    </div>
</div>

@code {
    [Parameter] public string PageSlug { get; set; } = string.Empty;
    [Parameter] public string DefaultTitle { get; set; } = string.Empty;
    [Parameter] public string DefaultContent { get; set; } = string.Empty;
    [Parameter] public RenderFragment? ChildContent { get; set; }
    
    protected ContentPage? ContentPage { get; set; }
    protected bool IsAdmin { get; set; }
    protected bool IsEditing { get; set; }
    protected bool IsSaving { get; set; }
    protected string UserId { get; set; } = string.Empty;
    
    private SfRichTextEditor? RichTextEditorRef;
    private string EditableContent = string.Empty;
    private string EditableTitle = string.Empty;
    private string EditableMetaDescription = string.Empty;
    private string EditableMetaKeywords = string.Empty;
    
    private readonly List<ToolbarItemModel> ToolbarItems = new()
    {
        new() { Command = ToolbarCommand.Bold },
        new() { Command = ToolbarCommand.Italic },
        new() { Command = ToolbarCommand.Underline },
        new() { Command = ToolbarCommand.StrikeThrough },
        new() { Command = ToolbarCommand.Separator },
        new() { Command = ToolbarCommand.Formats },
        new() { Command = ToolbarCommand.Alignments },
        new() { Command = ToolbarCommand.Separator },
        new() { Command = ToolbarCommand.OrderedList },
        new() { Command = ToolbarCommand.UnorderedList },
        new() { Command = ToolbarCommand.Separator },
        new() { Command = ToolbarCommand.CreateLink },
        new() { Command = ToolbarCommand.Image },
        new() { Command = ToolbarCommand.Separator },
        new() { Command = ToolbarCommand.SourceCode },
        new() { Command = ToolbarCommand.Undo },
        new() { Command = ToolbarCommand.Redo }
    };
    
    protected override async Task OnInitializedAsync()
    {
        await LoadAuthenticationStateAsync();
        await LoadContentAsync();
    }
    
    private async Task LoadAuthenticationStateAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        IsAdmin = authState.User.IsInRole("Administrator");
        UserId = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    }
    
    private async Task LoadContentAsync()
    {
        if (string.IsNullOrWhiteSpace(PageSlug))
            return;
            
        ContentPage = await ContentPageService.GetBySlugAsync(PageSlug);
        
        if (ContentPage == null && IsAdmin && !string.IsNullOrWhiteSpace(DefaultContent))
        {
            ContentPage = await ContentPageService.GetOrCreateBySlugAsync(
                PageSlug, 
                DefaultTitle, 
                DefaultContent, 
                UserId);
        }
    }
    
    protected async Task StartEditAsync()
    {
        if (!IsAdmin || ContentPage == null) return;
        
        EditableContent = ContentPage.Content;
        EditableTitle = ContentPage.Title;
        EditableMetaDescription = ContentPage.MetaDescription ?? string.Empty;
        EditableMetaKeywords = ContentPage.MetaKeywords ?? string.Empty;
        
        IsEditing = true;
        StateHasChanged();
        
        await Task.Delay(100);
        await JSRuntime.InvokeVoidAsync("focusRichTextEditor");
    }
    
    protected async Task SaveContentAsync()
    {
        if (!IsAdmin || ContentPage == null || IsSaving) return;
        
        IsSaving = true;
        StateHasChanged();
        
        try
        {
            var request = new UpdateContentPageRequest
            {
                Title = EditableTitle,
                Content = EditableContent,
                MetaDescription = EditableMetaDescription,
                MetaKeywords = EditableMetaKeywords,
                ChangeDescription = "Content updated via web interface"
            };
            
            ContentPage = await ContentPageService.UpdateAsync(ContentPage.Id, request, UserId);
            
            IsEditing = false;
            ToastService.ShowSuccess("Content saved successfully!");
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Error saving content: {ex.Message}");
        }
        finally
        {
            IsSaving = false;
            StateHasChanged();
        }
    }
    
    protected async Task CancelEditAsync()
    {
        var confirmed = await JSRuntime.InvokeAsync<bool>("confirm", 
            "Are you sure you want to cancel? Any unsaved changes will be lost.");
            
        if (confirmed)
        {
            IsEditing = false;
            StateHasChanged();
        }
    }
    
    public void Dispose()
    {
        RichTextEditorRef?.Dispose();
    }
}
```

## API Endpoints

### CmsController.cs

```csharp
[ApiController]
[Route("api/cms")]
[Authorize(Roles = "Administrator")]
public class CmsController : ControllerBase
{
    private readonly IContentPageService _contentPageService;
    
    public CmsController(IContentPageService contentPageService)
    {
        _contentPageService = contentPageService;
    }
    
    [HttpGet("{slug}")]
    public async Task<ActionResult<ContentPageDto>> GetBySlug(string slug)
    {
        var contentPage = await _contentPageService.GetBySlugAsync(slug);
        return contentPage == null ? NotFound() : Ok(contentPage.ToDto());
    }
    
    [HttpPut("{id}")]
    public async Task<ActionResult<ContentPageDto>> UpdateContent(int id, UpdateContentPageRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();
            
        var contentPage = await _contentPageService.UpdateAsync(id, request, userId);
        return Ok(contentPage.ToDto());
    }
    
    [HttpPost("upload")]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        // Image upload implementation
        // Return JSON with image URL for RichTextEditor
    }
}
```

## Security Considerations

### Content Sanitization

```csharp
public class ContentSanitizer : IContentSanitizer
{
    private readonly HtmlSanitizer _sanitizer;
    
    public ContentSanitizer()
    {
        _sanitizer = new HtmlSanitizer();
        ConfigureAllowedElements();
    }
    
    private void ConfigureAllowedElements()
    {
        // Allow common formatting tags
        _sanitizer.AllowedTags.UnionWith(new[] { 
            "p", "br", "strong", "em", "u", "s", "h1", "h2", "h3", "h4", "h5", "h6",
            "ul", "ol", "li", "a", "img", "blockquote", "code", "pre"
        });
        
        // Allow specific attributes
        _sanitizer.AllowedAttributes.UnionWith(new[] {
            "href", "src", "alt", "title", "class", "style"
        });
        
        // Allow specific CSS properties
        _sanitizer.AllowedCssProperties.UnionWith(new[] {
            "color", "background-color", "font-size", "font-weight", "text-align"
        });
    }
    
    public string SanitizeContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return string.Empty;
            
        return _sanitizer.Sanitize(content);
    }
}
```

## Performance Optimizations

1. **Caching**: Implement memory caching for frequently accessed content
2. **Lazy Loading**: Load content only when needed
3. **Compression**: Enable response compression for content
4. **CDN**: Use CDN for uploaded images
5. **Database Indexing**: Proper indexing on frequently queried fields

## Testing Strategy

1. **Unit Tests**: Service layer and domain logic
2. **Integration Tests**: API endpoints and database operations
3. **Component Tests**: Blazor component functionality
4. **Security Tests**: Authorization and content sanitization
5. **Performance Tests**: Load testing for content delivery
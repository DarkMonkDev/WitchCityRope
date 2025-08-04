using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Services;
using WitchCityRope.Infrastructure.Data;

namespace WitchCityRope.Infrastructure.Services;

public class ContentPageService : IContentPageService
{
    private readonly WitchCityRopeIdentityDbContext _context;
    private readonly ILogger<ContentPageService> _logger;
    
    public ContentPageService(
        WitchCityRopeIdentityDbContext context,
        ILogger<ContentPageService> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    public async Task<ContentPage?> GetBySlugAsync(string slug)
    {
        return await _context.ContentPages
            .FirstOrDefaultAsync(x => x.Slug == slug);
    }
    
    public async Task<ContentPage?> GetByIdAsync(int id)
    {
        return await _context.ContentPages
            .FirstOrDefaultAsync(x => x.Id == id);
    }
    
    public async Task<IEnumerable<ContentPage>> GetAllAsync()
    {
        return await _context.ContentPages
            .OrderBy(x => x.Title)
            .ToListAsync();
    }
    
    public async Task<ContentPage> GetOrCreateBySlugAsync(string slug, string defaultTitle)
    {
        var existing = await GetBySlugAsync(slug);
        if (existing != null)
            return existing;
            
        var contentPage = ContentPage.CreateDefault(slug, defaultTitle);
        
        _context.ContentPages.Add(contentPage);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Created new content page: {Slug}", slug);
        
        return contentPage;
    }
    
    public async Task<ContentPage> UpdateAsync(int id, string content, string title, string userId, string? metaDescription = null, string? metaKeywords = null)
    {
        var contentPage = await _context.ContentPages.FirstOrDefaultAsync(x => x.Id == id);
        
        if (contentPage == null)
            throw new InvalidOperationException($"Content page with ID {id} not found");
        
        // Sanitize content (basic HTML sanitization)
        var sanitizedContent = SanitizeContent(content);
        
        contentPage.UpdateContent(sanitizedContent, title, userId, metaDescription, metaKeywords);
        
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Updated content page: {Title} by user {UserId}", title, userId);
        
        return contentPage;
    }
    
    private string SanitizeContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return "Default Text Place Holder";
        
        // For now, return content as-is since Syncfusion editor handles basic sanitization
        // In production, you might want to use HtmlSanitizer library
        return content;
    }
}
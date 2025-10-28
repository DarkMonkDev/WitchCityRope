using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Cms;

namespace WitchCityRope.Api.Services.Seeding;

/// <summary>
/// Handles seeding of CMS content (pages, blocks, menus, etc.).
/// Extracted from SeedDataService.cs for better maintainability.
/// Responsible for creating initial CMS pages and content.
/// </summary>
public class CmsSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CmsSeeder> _logger;

    public CmsSeeder(
        ApplicationDbContext context,
        ILogger<CmsSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Seeds CMS content (pages, blocks, navigation).
    /// Delegates to CmsSeedData.SeedInitialPagesAsync for actual page creation.
    /// Idempotent operation - CmsSeedData handles duplicate checking.
    /// </summary>
    public async Task SeedCmsContentAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting CMS content seeding");

        // Delegate to existing CMS seed data logic
        await CmsSeedData.SeedInitialPagesAsync(_context);

        _logger.LogInformation("CMS content seeding completed");
    }
}

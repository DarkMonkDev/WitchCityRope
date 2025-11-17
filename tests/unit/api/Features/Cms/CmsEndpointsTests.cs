using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Security.Claims;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Cms.Dtos;
using WitchCityRope.Api.Features.Cms.Entities;
using WitchCityRope.Api.Features.Cms.Services;
using WitchCityRope.Api.Models;
using Xunit;
using FluentAssertions;
using NSubstitute;

namespace WitchCityRope.UnitTests.Api.Features.Cms;

/// <summary>
/// Unit tests for CmsEndpoints (Pattern B - Minimal API)
/// Tests content page retrieval, updates with XSS prevention, revision management, and admin page listing
/// </summary>
public class CmsEndpointsTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IContentSanitizer _sanitizer;
    private readonly ILogger<Program> _mockLogger;
    private readonly CancellationToken _cancellationToken = CancellationToken.None;

    public CmsEndpointsTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"CmsEndpointsTests_{Guid.NewGuid()}")
            .Options;

        _dbContext = new ApplicationDbContext(options);

        // Use real ContentSanitizer (concrete class with parameterless constructor)
        _sanitizer = new ContentSanitizer();

        // Mock logger
        _mockLogger = Substitute.For<ILogger<Program>>();
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    #region GetPageBySlug Tests

    /// <summary>
    /// Test: GetPageBySlug with valid published page slug returns 200 OK with ContentPageDto
    /// </summary>
    [Fact]
    public async Task GetPageBySlug_WithValidPublishedPageSlug_Returns200OkWithContentPageDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new ApplicationUser
        {
            Id = userId,
            Email = "admin@witchcityrope.com",
            UserName = "admin@witchcityrope.com"
        };
        await _dbContext.Users.AddAsync(user);

        var page = new ContentPage
        {
            Slug = "test-page",
            Title = "Test Page",
            Content = "<p>This is test content</p>",
            IsPublished = true,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            LastModifiedBy = userId
        };
        await _dbContext.ContentPages.AddAsync(page);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await CallGetPageBySlug("test-page");

        // Assert
        result.Should().NotBeNull();
        result.GetType().Name.Should().Contain("Ok");

        // Extract value from Ok result
        dynamic dynamicResult = result!;
        object dtoObj = dynamicResult.Value;
        dtoObj.Should().BeOfType<ContentPageDto>();

        var dto = (ContentPageDto)dtoObj;
        dto.Id.Should().Be(page.Id);
        dto.Slug.Should().Be("test-page");
        dto.Title.Should().Be("Test Page");
        dto.Content.Should().Be("<p>This is test content</p>");
        dto.IsPublished.Should().BeTrue();
        dto.LastModifiedBy.Should().Be("admin@witchcityrope.com");
    }

    /// <summary>
    /// Test: GetPageBySlug with non-existent slug returns 404 NotFound
    /// </summary>
    [Fact]
    public async Task GetPageBySlug_WithNonExistentSlug_Returns404NotFound()
    {
        // Act
        var result = await CallGetPageBySlug("non-existent-page");

        // Assert
        result.Should().NotBeNull();
        result!.GetType().Name.Should().Contain("NotFound");
    }

    /// <summary>
    /// Test: GetPageBySlug with unpublished page returns 404 NotFound
    /// </summary>
    [Fact]
    public async Task GetPageBySlug_WithUnpublishedPage_Returns404NotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var page = new ContentPage
        {
            Slug = "unpublished-page",
            Title = "Unpublished Page",
            Content = "<p>This page is not published</p>",
            IsPublished = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            LastModifiedBy = userId
        };
        await _dbContext.ContentPages.AddAsync(page);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await CallGetPageBySlug("unpublished-page");

        // Assert
        result.Should().NotBeNull();
        result!.GetType().Name.Should().Contain("NotFound");
    }

    /// <summary>
    /// Test: GetPageBySlug with user having no email returns "Unknown" in LastModifiedBy field
    /// </summary>
    [Fact]
    public async Task GetPageBySlug_WithUserHavingNoEmail_ReturnsUnknownInLastModifiedBy()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userWithoutEmail = new ApplicationUser
        {
            Id = userId,
            UserName = "user-no-email",
            Email = null  // User with no email
        };
        await _dbContext.Users.AddAsync(userWithoutEmail);

        var page = new ContentPage
        {
            Slug = "orphan-page",
            Title = "Orphan Page",
            Content = "<p>User has no email</p>",
            IsPublished = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            LastModifiedBy = userId
        };
        await _dbContext.ContentPages.AddAsync(page);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await CallGetPageBySlug("orphan-page");

        // Assert
        result.Should().NotBeNull();
        dynamic dynamicResult = result!;
        object dtoObj = dynamicResult.Value;
        var dto = (ContentPageDto)dtoObj;
        dto.LastModifiedBy.Should().Be("Unknown");
    }

    #endregion

    #region UpdatePage Tests

    /// <summary>
    /// Test: UpdatePage with missing user ID claim returns 401 Unauthorized
    /// </summary>
    [Fact]
    public async Task UpdatePage_WithMissingUserIdClaim_Returns401Unauthorized()
    {
        // Arrange
        var request = new UpdateContentPageRequest
        {
            Title = "Updated Title",
            Content = "<p>Updated content</p>",
            ChangeDescription = "Updated via test"
        };

        var user = new ClaimsPrincipal(); // No claims

        // Act
        var result = await CallUpdatePage(1, request, user);

        // Assert
        result.Should().NotBeNull();
        result!.GetType().Name.Should().Contain("Unauthorized");
    }

    /// <summary>
    /// Test: UpdatePage with invalid user ID format returns 401 Unauthorized
    /// </summary>
    [Fact]
    public async Task UpdatePage_WithInvalidUserIdFormat_Returns401Unauthorized()
    {
        // Arrange
        var request = new UpdateContentPageRequest
        {
            Title = "Updated Title",
            Content = "<p>Updated content</p>"
        };

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "not-a-guid")
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims));

        // Act
        var result = await CallUpdatePage(1, request, user);

        // Assert
        result.Should().NotBeNull();
        result!.GetType().Name.Should().Contain("Unauthorized");
    }

    /// <summary>
    /// Test: UpdatePage with page not found returns 404 NotFound
    /// </summary>
    [Fact]
    public async Task UpdatePage_WithPageNotFound_Returns404NotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new UpdateContentPageRequest
        {
            Title = "Updated Title",
            Content = "<p>Updated content</p>"
        };

        var user = CreateAuthenticatedUser(userId);

        // Act
        var result = await CallUpdatePage(999, request, user);

        // Assert
        result.Should().NotBeNull();
        result!.GetType().Name.Should().Contain("NotFound");
    }

    /// <summary>
    /// Test: UpdatePage with empty content after sanitization returns 400 BadRequest
    /// </summary>
    [Fact]
    public async Task UpdatePage_WithEmptyContentAfterSanitization_Returns400BadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = await CreateUserInDatabase(userId, "admin@witchcityrope.com");

        var page = new ContentPage
        {
            Slug = "test-page",
            Title = "Test Page",
            Content = "<p>Original content</p>",
            IsPublished = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            LastModifiedBy = userId
        };
        await _dbContext.ContentPages.AddAsync(page);
        await _dbContext.SaveChangesAsync();

        var request = new UpdateContentPageRequest
        {
            Title = "Updated Title",
            Content = "   ", // Whitespace only - will be empty after sanitization
            ChangeDescription = "Test update"
        };

        var claimsPrincipal = CreateAuthenticatedUser(userId);

        // Act
        var result = await CallUpdatePage(page.Id, request, claimsPrincipal);

        // Assert
        result.Should().NotBeNull();
        result!.GetType().Name.Should().Contain("BadRequest");
    }

    /// <summary>
    /// Test: UpdatePage sanitizes content and removes XSS script tags
    /// </summary>
    [Fact]
    public async Task UpdatePage_SanitizesContentAndRemovesXSSScriptTags()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = await CreateUserInDatabase(userId, "admin@witchcityrope.com");

        var page = new ContentPage
        {
            Slug = "test-page",
            Title = "Test Page",
            Content = "<p>Original content</p>",
            IsPublished = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            LastModifiedBy = userId
        };
        await _dbContext.ContentPages.AddAsync(page);
        await _dbContext.SaveChangesAsync();

        var request = new UpdateContentPageRequest
        {
            Title = "Updated Title",
            Content = "<p>Safe content</p><script>alert('XSS')</script>", // XSS attempt
            ChangeDescription = "XSS test"
        };

        var claimsPrincipal = CreateAuthenticatedUser(userId);

        // Act
        var result = await CallUpdatePage(page.Id, request, claimsPrincipal);

        // Assert
        result.Should().NotBeNull();
        result!.GetType().Name.Should().Contain("Ok");

        // Verify content was sanitized (script tag removed)
        var updatedPage = await _dbContext.ContentPages.FindAsync(page.Id);
        updatedPage.Should().NotBeNull();
        updatedPage!.Content.Should().NotContain("script");
        updatedPage.Content.Should().Contain("Safe content");
    }

    /// <summary>
    /// Test: UpdatePage with valid request creates ContentRevision record
    /// </summary>
    [Fact]
    public async Task UpdatePage_WithValidRequest_CreatesContentRevisionRecord()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = await CreateUserInDatabase(userId, "admin@witchcityrope.com");

        var page = new ContentPage
        {
            Slug = "test-page",
            Title = "Original Title",
            Content = "<p>Original content</p>",
            IsPublished = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            LastModifiedBy = userId
        };
        await _dbContext.ContentPages.AddAsync(page);
        await _dbContext.SaveChangesAsync();

        var originalRevisionCount = await _dbContext.ContentRevisions.CountAsync();

        var request = new UpdateContentPageRequest
        {
            Title = "Updated Title",
            Content = "<p>Updated content</p>",
            ChangeDescription = "Test update"
        };

        var claimsPrincipal = CreateAuthenticatedUser(userId);

        // Act
        var result = await CallUpdatePage(page.Id, request, claimsPrincipal);

        // Assert
        result.Should().NotBeNull();
        result!.GetType().Name.Should().Contain("Ok");

        // Verify revision was created
        var revisionCount = await _dbContext.ContentRevisions.CountAsync();
        revisionCount.Should().Be(originalRevisionCount + 1);

        // Verify revision contains old content
        var revision = await _dbContext.ContentRevisions
            .Where(r => r.ContentPageId == page.Id)
            .OrderByDescending(r => r.CreatedAt)
            .FirstAsync();

        revision.Should().NotBeNull();
        revision.Content.Should().Be("<p>Original content</p>");
        revision.Title.Should().Be("Original Title");
        revision.ChangeDescription.Should().Be("Test update");
        revision.CreatedBy.Should().Be(userId);
    }

    /// <summary>
    /// Test: UpdatePage with valid request returns 200 OK with updated page DTO
    /// </summary>
    [Fact]
    public async Task UpdatePage_WithValidRequest_Returns200OkWithUpdatedPageDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = await CreateUserInDatabase(userId, "admin@witchcityrope.com");

        var page = new ContentPage
        {
            Slug = "test-page",
            Title = "Original Title",
            Content = "<p>Original content</p>",
            IsPublished = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            LastModifiedBy = userId
        };
        await _dbContext.ContentPages.AddAsync(page);
        await _dbContext.SaveChangesAsync();

        var request = new UpdateContentPageRequest
        {
            Title = "Updated Title",
            Content = "<p>Updated content</p>",
            ChangeDescription = "Test update"
        };

        var claimsPrincipal = CreateAuthenticatedUser(userId);

        // Act
        var result = await CallUpdatePage(page.Id, request, claimsPrincipal);

        // Assert
        result.Should().NotBeNull();
        result!.GetType().Name.Should().Contain("Ok");

        // Extract DTO from result
        dynamic dynamicResult = result;
        object dtoObj = dynamicResult.Value;
        dtoObj.Should().BeOfType<ContentPageDto>();

        var dto = (ContentPageDto)dtoObj;
        dto.Id.Should().Be(page.Id);
        dto.Title.Should().Be("Updated Title");
        dto.Content.Should().Be("<p>Updated content</p>");
        dto.LastModifiedBy.Should().Be("admin@witchcityrope.com");
    }

    /// <summary>
    /// Test: UpdatePage with ArgumentException from domain method returns 400 BadRequest
    /// </summary>
    [Fact]
    public async Task UpdatePage_WithArgumentExceptionFromDomainMethod_Returns400BadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = await CreateUserInDatabase(userId, "admin@witchcityrope.com");

        var page = new ContentPage
        {
            Slug = "test-page",
            Title = "Original Title",
            Content = "<p>Original content</p>",
            IsPublished = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            LastModifiedBy = userId
        };
        await _dbContext.ContentPages.AddAsync(page);
        await _dbContext.SaveChangesAsync();

        var request = new UpdateContentPageRequest
        {
            Title = "", // Empty title will trigger ArgumentException in domain method
            Content = "<p>Some content</p>",
            ChangeDescription = "Test update"
        };

        var claimsPrincipal = CreateAuthenticatedUser(userId);

        // Act
        var result = await CallUpdatePage(page.Id, request, claimsPrincipal);

        // Assert
        result.Should().NotBeNull();
        result!.GetType().Name.Should().Contain("BadRequest");
    }

    #endregion

    #region GetPageRevisions Tests

    /// <summary>
    /// Test: GetPageRevisions with valid page ID returns 200 OK with revisions list
    /// </summary>
    [Fact]
    public async Task GetPageRevisions_WithValidPageId_Returns200OkWithRevisionsList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = await CreateUserInDatabase(userId, "admin@witchcityrope.com");

        var page = new ContentPage
        {
            Slug = "test-page",
            Title = "Test Page",
            Content = "<p>Current content</p>",
            IsPublished = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            LastModifiedBy = userId
        };
        await _dbContext.ContentPages.AddAsync(page);
        await _dbContext.SaveChangesAsync();

        // Add revisions
        var revision1 = new ContentRevision
        {
            ContentPageId = page.Id,
            Content = "<p>Old content version 1</p>",
            Title = "Old Title 1",
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            CreatedBy = userId,
            ChangeDescription = "First update"
        };
        var revision2 = new ContentRevision
        {
            ContentPageId = page.Id,
            Content = "<p>Old content version 2</p>",
            Title = "Old Title 2",
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            CreatedBy = userId,
            ChangeDescription = "Second update"
        };
        await _dbContext.ContentRevisions.AddRangeAsync(revision1, revision2);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await CallGetPageRevisions(page.Id);

        // Assert
        result.Should().NotBeNull();
        result!.GetType().Name.Should().Contain("Ok");

        // Extract list from result
        dynamic dynamicResult = result;
        object listObj = dynamicResult.Value;
        listObj.Should().BeAssignableTo<List<ContentRevisionDto>>();

        var dtoList = (List<ContentRevisionDto>)listObj;
        dtoList.Should().HaveCount(2);

        // Verify ordering (most recent first)
        dtoList[0].ChangeDescription.Should().Be("Second update");
        dtoList[1].ChangeDescription.Should().Be("First update");

        // Verify content preview (first 200 characters)
        dtoList[0].ContentPreview.Should().Be("<p>Old content version 2</p>");
        dtoList[0].FullContent.Should().BeNull(); // Not included in list view
    }

    /// <summary>
    /// Test: GetPageRevisions with page not found returns 404 NotFound
    /// </summary>
    [Fact]
    public async Task GetPageRevisions_WithPageNotFound_Returns404NotFound()
    {
        // Act
        var result = await CallGetPageRevisions(999);

        // Assert
        result.Should().NotBeNull();
        result!.GetType().Name.Should().Contain("NotFound");
    }

    /// <summary>
    /// Test: GetPageRevisions with page exists but no revisions returns 200 OK with empty list
    /// </summary>
    [Fact]
    public async Task GetPageRevisions_WithPageExistsButNoRevisions_Returns200OkWithEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var page = new ContentPage
        {
            Slug = "new-page",
            Title = "New Page",
            Content = "<p>No revisions yet</p>",
            IsPublished = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            LastModifiedBy = userId
        };
        await _dbContext.ContentPages.AddAsync(page);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await CallGetPageRevisions(page.Id);

        // Assert
        result.Should().NotBeNull();
        result!.GetType().Name.Should().Contain("Ok");

        // Extract list from result
        dynamic dynamicResult = result;
        object listObj = dynamicResult.Value;
        listObj.Should().BeAssignableTo<List<ContentRevisionDto>>();

        var dtoList = (List<ContentRevisionDto>)listObj;
        dtoList.Should().BeEmpty();
    }

    #endregion

    #region GetAllPages Tests

    /// <summary>
    /// Test: GetAllPages returns 200 OK with all pages with summaries
    /// </summary>
    [Fact]
    public async Task GetAllPages_ReturnsAllPagesWithSummaries_Returns200Ok()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = await CreateUserInDatabase(userId, "admin@witchcityrope.com");

        var page1 = new ContentPage
        {
            Slug = "about",
            Title = "About Us",
            Content = "<p>About content</p>",
            IsPublished = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            LastModifiedBy = userId
        };
        var page2 = new ContentPage
        {
            Slug = "contact",
            Title = "Contact Us",
            Content = "<p>Contact content</p>",
            IsPublished = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            LastModifiedBy = userId
        };
        await _dbContext.ContentPages.AddRangeAsync(page1, page2);
        await _dbContext.SaveChangesAsync();

        // Add revisions to page1
        var revision = new ContentRevision
        {
            ContentPageId = page1.Id,
            Content = "<p>Old about content</p>",
            Title = "Old About Title",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId
        };
        await _dbContext.ContentRevisions.AddAsync(revision);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await CallGetAllPages();

        // Assert
        result.Should().NotBeNull();
        result!.GetType().Name.Should().Contain("Ok");

        // Extract list from result
        dynamic dynamicResult = result;
        object listObj = dynamicResult.Value;
        listObj.Should().BeAssignableTo<List<CmsPageSummaryDto>>();

        var dtoList = (List<CmsPageSummaryDto>)listObj;
        dtoList.Should().HaveCount(2);

        // Verify ordering by slug
        dtoList[0].Slug.Should().Be("about");
        dtoList[1].Slug.Should().Be("contact");

        // Verify revision count
        dtoList[0].RevisionCount.Should().Be(1);
        dtoList[1].RevisionCount.Should().Be(0);

        // Verify LastModifiedBy
        dtoList[0].LastModifiedBy.Should().Be("admin@witchcityrope.com");
    }

    /// <summary>
    /// Test: GetAllPages with empty database returns 200 OK with empty list
    /// </summary>
    [Fact]
    public async Task GetAllPages_WithEmptyDatabase_Returns200OkWithEmptyList()
    {
        // Act
        var result = await CallGetAllPages();

        // Assert
        result.Should().NotBeNull();
        result!.GetType().Name.Should().Contain("Ok");

        // Extract list from result
        dynamic dynamicResult = result;
        object listObj = dynamicResult.Value;
        listObj.Should().BeAssignableTo<List<CmsPageSummaryDto>>();

        var dtoList = (List<CmsPageSummaryDto>)listObj;
        dtoList.Should().BeEmpty();
    }

    /// <summary>
    /// Test: GetAllPages returns pages ordered by slug
    /// </summary>
    [Fact]
    public async Task GetAllPages_ReturnsPagesOrderedBySlug()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = await CreateUserInDatabase(userId, "test@witchcityrope.com");

        var page1 = new ContentPage
        {
            Slug = "zebra",
            Title = "Zebra Page",
            Content = "<p>Z</p>",
            IsPublished = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            LastModifiedBy = userId
        };
        var page2 = new ContentPage
        {
            Slug = "apple",
            Title = "Apple Page",
            Content = "<p>A</p>",
            IsPublished = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            LastModifiedBy = userId
        };
        var page3 = new ContentPage
        {
            Slug = "mango",
            Title = "Mango Page",
            Content = "<p>M</p>",
            IsPublished = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            LastModifiedBy = userId
        };
        await _dbContext.ContentPages.AddRangeAsync(page1, page2, page3);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await CallGetAllPages();

        // Assert
        result.Should().NotBeNull();
        dynamic dynamicResult = result!;
        object listObj = dynamicResult.Value;
        var dtoList = (List<CmsPageSummaryDto>)listObj;

        dtoList.Should().HaveCount(3);
        dtoList[0].Slug.Should().Be("apple");
        dtoList[1].Slug.Should().Be("mango");
        dtoList[2].Slug.Should().Be("zebra");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Helper: Invokes GetPageBySlug endpoint using reflection
    /// </summary>
    private async Task<IResult?> CallGetPageBySlug(string slug)
    {
        var endpointsType = Type.GetType("WitchCityRope.Api.Features.Cms.CmsEndpoints, WitchCityRope.Api");
        if (endpointsType == null) return null;

        var method = endpointsType.GetMethod("GetPageBySlug", BindingFlags.NonPublic | BindingFlags.Static);
        if (method == null) return null;

        var result = method.Invoke(null, new object?[] { slug, _dbContext, _cancellationToken });
        if (result is Task<IResult> task)
        {
            return await task;
        }

        return null;
    }

    /// <summary>
    /// Helper: Invokes UpdatePage endpoint using reflection
    /// </summary>
    private async Task<IResult?> CallUpdatePage(int id, UpdateContentPageRequest request, ClaimsPrincipal user)
    {
        var endpointsType = Type.GetType("WitchCityRope.Api.Features.Cms.CmsEndpoints, WitchCityRope.Api");
        if (endpointsType == null) return null;

        var method = endpointsType.GetMethod("UpdatePage", BindingFlags.NonPublic | BindingFlags.Static);
        if (method == null) return null;

        var result = method.Invoke(null, new object?[] { id, request, user, _dbContext, _sanitizer, _mockLogger, _cancellationToken });
        if (result is Task<IResult> task)
        {
            return await task;
        }

        return null;
    }

    /// <summary>
    /// Helper: Invokes GetPageRevisions endpoint using reflection
    /// </summary>
    private async Task<IResult?> CallGetPageRevisions(int id)
    {
        var endpointsType = Type.GetType("WitchCityRope.Api.Features.Cms.CmsEndpoints, WitchCityRope.Api");
        if (endpointsType == null) return null;

        var method = endpointsType.GetMethod("GetPageRevisions", BindingFlags.NonPublic | BindingFlags.Static);
        if (method == null) return null;

        var result = method.Invoke(null, new object?[] { id, _dbContext, _cancellationToken });
        if (result is Task<IResult> task)
        {
            return await task;
        }

        return null;
    }

    /// <summary>
    /// Helper: Invokes GetAllPages endpoint using reflection
    /// </summary>
    private async Task<IResult?> CallGetAllPages()
    {
        var endpointsType = Type.GetType("WitchCityRope.Api.Features.Cms.CmsEndpoints, WitchCityRope.Api");
        if (endpointsType == null) return null;

        var method = endpointsType.GetMethod("GetAllPages", BindingFlags.NonPublic | BindingFlags.Static);
        if (method == null) return null;

        var result = method.Invoke(null, new object?[] { _dbContext, _cancellationToken });
        if (result is Task<IResult> task)
        {
            return await task;
        }

        return null;
    }

    /// <summary>
    /// Helper: Creates authenticated user with ClaimsPrincipal
    /// </summary>
    private ClaimsPrincipal CreateAuthenticatedUser(Guid userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, "admin@witchcityrope.com"),
            new Claim(ClaimTypes.Role, "Administrator")
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    /// <summary>
    /// Helper: Creates user in database
    /// </summary>
    private async Task<ApplicationUser> CreateUserInDatabase(Guid userId, string email)
    {
        var user = new ApplicationUser
        {
            Id = userId,
            Email = email,
            UserName = email,
            EmailConfirmed = true
        };
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();
        return user;
    }

    #endregion
}

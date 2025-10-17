using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Cms.Dtos;
using WitchCityRope.Api.Features.Cms.Services;

namespace WitchCityRope.Api.Features.Cms
{
    public static class CmsEndpoints
    {
        public static void MapCmsEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/cms")
                .WithTags("CMS")
                .WithOpenApi();

            // GET /api/cms/pages/{slug} - Fetch page by slug (PUBLIC)
            group.MapGet("/pages/{slug}", GetPageBySlug)
                .WithName("GetCmsPageBySlug")
                .WithSummary("Get content page by slug")
                .WithDescription("Fetches a published content page by its URL slug. Public endpoint.")
                .Produces<ContentPageDto>(200)
                .Produces(404);

            // PUT /api/cms/pages/{id} - Update page content (ADMIN ONLY)
            group.MapPut("/pages/{id:int}", UpdatePage)
                .WithName("UpdateCmsPage")
                .WithSummary("Update content page")
                .WithDescription("Updates page content and creates a revision. Requires Administrator role.")
                .RequireAuthorization(policy => policy.RequireRole("Administrator"))
                .Produces<ContentPageDto>(200)
                .Produces(400)
                .Produces(401)
                .Produces(404);

            // GET /api/cms/pages/{id}/revisions - Fetch revision history (ADMIN ONLY)
            group.MapGet("/pages/{id:int}/revisions", GetPageRevisions)
                .WithName("GetCmsPageRevisions")
                .WithSummary("Get page revision history")
                .WithDescription("Fetches all revisions for a content page. Requires Administrator role.")
                .RequireAuthorization(policy => policy.RequireRole("Administrator"))
                .Produces<List<ContentRevisionDto>>(200)
                .Produces(401)
                .Produces(404);

            // GET /api/cms/pages - List all pages (ADMIN ONLY)
            group.MapGet("/pages", GetAllPages)
                .WithName("GetAllCmsPages")
                .WithSummary("List all content pages")
                .WithDescription("Lists all content pages with revision counts. Requires Administrator role.")
                .RequireAuthorization(policy => policy.RequireRole("Administrator"))
                .Produces<List<CmsPageSummaryDto>>(200)
                .Produces(401);
        }

        /// <summary>
        /// GET /api/cms/pages/{slug} - Fetch published page by slug
        /// </summary>
        private static async Task<IResult> GetPageBySlug(
            string slug,
            ApplicationDbContext db,
            CancellationToken ct)
        {
            var page = await db.ContentPages
                .Include(p => p.LastModifiedByUser)
                .Where(p => p.Slug == slug && p.IsPublished)
                .FirstOrDefaultAsync(ct);

            if (page == null)
            {
                return Results.NotFound(new { error = "Page not found or not published" });
            }

            var dto = new ContentPageDto
            {
                Id = page.Id,
                Slug = page.Slug,
                Title = page.Title,
                Content = page.Content,
                UpdatedAt = page.UpdatedAt,
                LastModifiedBy = page.LastModifiedByUser?.Email ?? "Unknown",
                IsPublished = page.IsPublished
            };

            return Results.Ok(dto);
        }

        /// <summary>
        /// PUT /api/cms/pages/{id} - Update page content and create revision
        /// </summary>
        private static async Task<IResult> UpdatePage(
            int id,
            [FromBody] UpdateContentPageRequest request,
            ClaimsPrincipal user,
            ApplicationDbContext db,
            ContentSanitizer sanitizer,
            ILogger<Program> logger,
            CancellationToken ct)
        {
            // Extract user ID from claims
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                logger.LogWarning("UpdatePage called without valid user ID claim");
                return Results.Unauthorized();
            }

            // Fetch page
            var page = await db.ContentPages
                .Include(p => p.Revisions)
                .Include(p => p.LastModifiedByUser)
                .FirstOrDefaultAsync(p => p.Id == id, ct);

            if (page == null)
            {
                return Results.NotFound(new { error = $"Page with ID {id} not found" });
            }

            // Sanitize content BEFORE database write (XSS prevention)
            var cleanContent = sanitizer.Sanitize(request.Content);

            if (string.IsNullOrWhiteSpace(cleanContent))
            {
                return Results.BadRequest(new { error = "Content is empty after sanitization" });
            }

            // Use domain method to update content (creates revision automatically)
            try
            {
                page.UpdateContent(
                    cleanContent,
                    request.Title,
                    userId,
                    request.ChangeDescription
                );

                await db.SaveChangesAsync(ct);

                logger.LogInformation(
                    "CMS page {PageId} ({Slug}) updated by user {UserId}. Revision created.",
                    page.Id,
                    page.Slug,
                    userId
                );

                // Return updated page
                var dto = new ContentPageDto
                {
                    Id = page.Id,
                    Slug = page.Slug,
                    Title = page.Title,
                    Content = page.Content,
                    UpdatedAt = page.UpdatedAt,
                    LastModifiedBy = page.LastModifiedByUser?.Email ?? "Unknown",
                    IsPublished = page.IsPublished
                };

                return Results.Ok(dto);
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Validation error updating page {PageId}", id);
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating page {PageId}", id);
                return Results.Problem("An error occurred while updating the page");
            }
        }

        /// <summary>
        /// GET /api/cms/pages/{id}/revisions - Fetch revision history
        /// </summary>
        private static async Task<IResult> GetPageRevisions(
            int id,
            ApplicationDbContext db,
            CancellationToken ct)
        {
            // Check if page exists
            var pageExists = await db.ContentPages.AnyAsync(p => p.Id == id, ct);
            if (!pageExists)
            {
                return Results.NotFound(new { error = $"Page with ID {id} not found" });
            }

            // Fetch revisions (most recent first, limit 50)
            var revisions = await db.ContentRevisions
                .Include(r => r.CreatedByUser)
                .Where(r => r.ContentPageId == id)
                .OrderByDescending(r => r.CreatedAt)
                .Take(50)
                .Select(r => new ContentRevisionDto
                {
                    Id = r.Id,
                    ContentPageId = r.ContentPageId,
                    CreatedAt = r.CreatedAt,
                    CreatedBy = r.CreatedByUser != null ? r.CreatedByUser.Email : "Unknown",
                    ChangeDescription = r.ChangeDescription,
                    ContentPreview = r.Content.Length > 200
                        ? r.Content.Substring(0, 200) + "..."
                        : r.Content,
                    Title = r.Title,
                    FullContent = null // Don't return full content in list view
                })
                .ToListAsync(ct);

            return Results.Ok(revisions);
        }

        /// <summary>
        /// GET /api/cms/pages - List all pages with revision counts
        /// </summary>
        private static async Task<IResult> GetAllPages(
            ApplicationDbContext db,
            CancellationToken ct)
        {
            var pages = await db.ContentPages
                .Include(p => p.LastModifiedByUser)
                .Include(p => p.Revisions)
                .OrderBy(p => p.Slug)
                .Select(p => new CmsPageSummaryDto
                {
                    Id = p.Id,
                    Slug = p.Slug,
                    Title = p.Title,
                    RevisionCount = p.Revisions.Count,
                    UpdatedAt = p.UpdatedAt,
                    LastModifiedBy = p.LastModifiedByUser != null ? p.LastModifiedByUser.Email : "Unknown",
                    IsPublished = p.IsPublished
                })
                .ToListAsync(ct);

            return Results.Ok(pages);
        }
    }
}

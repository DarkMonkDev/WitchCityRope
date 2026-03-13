using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Cms.Dtos;
using WitchCityRope.Api.Features.Cms.Entities;
using WitchCityRope.Api.Features.Cms.Services;
using WitchCityRope.Api.Features.Users.Constants;

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

            // POST /api/cms/pages - Create a new CMS page (ADMIN ONLY)
            group.MapPost("/pages", CreatePage)
                .WithName("CreateCmsPage")
                .WithSummary("Create content page")
                .WithDescription("Creates a new content page with the given title, slug, and content. Requires Administrator role.")
                .RequireAuthorization(policy => policy.RequireRole(UserRole.Administrator.ToRoleString()))
                .Produces<ContentPageDto>(201)
                .Produces(400)
                .Produces(401)
                .Produces(409);

            // PUT /api/cms/pages/{id} - Update page content (ADMIN ONLY)
            group.MapPut("/pages/{id:int}", UpdatePage)
                .WithName("UpdateCmsPage")
                .WithSummary("Update content page")
                .WithDescription("Updates page content and creates a revision. Requires Administrator role.")
                .RequireAuthorization(policy => policy.RequireRole(UserRole.Administrator.ToRoleString()))
                .Produces<ContentPageDto>(200)
                .Produces(400)
                .Produces(401)
                .Produces(404);

            // DELETE /api/cms/pages/{id} - Soft delete a CMS page (ADMIN ONLY)
            group.MapDelete("/pages/{id:int}", DeletePage)
                .WithName("DeleteCmsPage")
                .WithSummary("Soft delete content page")
                .WithDescription("Soft-deletes a content page. The page remains in the database but is excluded from all queries. Requires Administrator role.")
                .RequireAuthorization(policy => policy.RequireRole(UserRole.Administrator.ToRoleString()))
                .Produces(204)
                .Produces(401)
                .Produces(404);

            // GET /api/cms/pages/{id}/revisions - Fetch revision history (ADMIN ONLY)
            group.MapGet("/pages/{id:int}/revisions", GetPageRevisions)
                .WithName("GetCmsPageRevisions")
                .WithSummary("Get page revision history")
                .WithDescription("Fetches all revisions for a content page. Requires Administrator role.")
                .RequireAuthorization(policy => policy.RequireRole(UserRole.Administrator.ToRoleString()))
                .Produces<List<ContentRevisionDto>>(200)
                .Produces(401)
                .Produces(404);

            // GET /api/cms/pages - List all pages (ADMIN ONLY)
            group.MapGet("/pages", GetAllPages)
                .WithName("GetAllCmsPages")
                .WithSummary("List all content pages")
                .WithDescription("Lists all non-deleted content pages with revision counts. Requires Administrator role.")
                .RequireAuthorization(policy => policy.RequireRole(UserRole.Administrator.ToRoleString()))
                .Produces<List<CmsPageSummaryDto>>(200)
                .Produces(401);
        }

        /// <summary>
        /// GET /api/cms/pages/{slug} - Fetch published page by slug.
        /// Excludes soft-deleted pages so they return 404 to public visitors.
        /// </summary>
        private static async Task<IResult> GetPageBySlug(
            string slug,
            [FromServices] ApplicationDbContext db,
            CancellationToken ct)
        {
            var page = await db.ContentPages
                .Include(p => p.LastModifiedByUser)
                .Where(p => p.Slug == slug && p.IsPublished && !p.IsDeleted)
                .FirstOrDefaultAsync(ct);

            if (page == null)
            {
                return Results.Problem(
                    title: "Not Found",
                    detail: "Page not found or not published",
                    statusCode: 404);
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
        /// POST /api/cms/pages - Create a new CMS page.
        /// Validates slug uniqueness, sanitizes content, and defaults to published.
        /// </summary>
        private static async Task<IResult> CreatePage(
            HttpContext context,
            IAntiforgery antiforgery,
            [FromBody] CreateContentPageRequest request,
            ClaimsPrincipal user,
            [FromServices] ApplicationDbContext db,
            [FromServices] IContentSanitizer sanitizer,
            [FromServices] ILogger<Program> logger,
            CancellationToken ct)
        {
            // Validate CSRF token
            try
            {
                await antiforgery.ValidateRequestAsync(context);
            }
            catch (AntiforgeryValidationException)
            {
                return Results.Problem(
                    title: "CSRF Validation Failed",
                    detail: "Antiforgery token validation failed. Please refresh the page and try again.",
                    statusCode: 400);
            }

            // Extract user ID from claims
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                logger.LogWarning("CreatePage called without valid user ID claim");
                return Results.Problem(
                    title: "Unauthorized",
                    detail: "User authentication failed - missing or invalid user identifier",
                    statusCode: 401);
            }

            // Check slug uniqueness (globally unique, including soft-deleted pages)
            var slugExists = await db.ContentPages.AnyAsync(p => p.Slug == request.Slug, ct);
            if (slugExists)
            {
                return Results.Problem(
                    title: "Conflict",
                    detail: $"A page with slug '{request.Slug}' already exists. Please choose a different slug.",
                    statusCode: 409);
            }

            // Sanitize content before storing (XSS prevention)
            var cleanContent = sanitizer.Sanitize(request.Content);
            if (string.IsNullOrWhiteSpace(cleanContent))
            {
                return Results.Problem(
                    title: "Bad Request",
                    detail: "Content is empty after sanitization",
                    statusCode: 400);
            }

            var now = DateTime.UtcNow;
            var page = new ContentPage
            {
                Slug = request.Slug,
                Title = request.Title,
                Content = cleanContent,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = userId,
                LastModifiedBy = userId,
                IsPublished = true,
                IsDeleted = false
            };

            db.ContentPages.Add(page);
            await db.SaveChangesAsync(ct);

            logger.LogInformation(
                "CMS page {PageId} ({Slug}) created by user {UserId}.",
                page.Id,
                page.Slug,
                userId
            );

            var dto = new ContentPageDto
            {
                Id = page.Id,
                Slug = page.Slug,
                Title = page.Title,
                Content = page.Content,
                UpdatedAt = page.UpdatedAt,
                LastModifiedBy = user.FindFirst(ClaimTypes.Email)?.Value ?? "Unknown",
                IsPublished = page.IsPublished
            };

            return Results.Created($"/api/cms/pages/{page.Slug}", dto);
        }

        /// <summary>
        /// PUT /api/cms/pages/{id} - Update page content and create revision.
        /// Excludes soft-deleted pages (cannot update a deleted page).
        /// </summary>
        private static async Task<IResult> UpdatePage(
            HttpContext context,
            IAntiforgery antiforgery,
            int id,
            [FromBody] UpdateContentPageRequest request,
            ClaimsPrincipal user,
            [FromServices] ApplicationDbContext db,
            [FromServices] IContentSanitizer sanitizer,
            [FromServices] ILogger<Program> logger,
            CancellationToken ct)
        {
            // Validate CSRF token
            try
            {
                await antiforgery.ValidateRequestAsync(context);
            }
            catch (AntiforgeryValidationException)
            {
                return Results.Problem(
                    title: "CSRF Validation Failed",
                    detail: "Antiforgery token validation failed. Please refresh the page and try again.",
                    statusCode: 400);
            }

            // Extract user ID from claims
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                logger.LogWarning("UpdatePage called without valid user ID claim");
                return Results.Problem(
                    title: "Unauthorized",
                    detail: "User authentication failed - missing or invalid user identifier",
                    statusCode: 401);
            }

            // Fetch page - exclude soft-deleted pages
            var page = await db.ContentPages
                .Include(p => p.Revisions)
                .Include(p => p.LastModifiedByUser)
                .Where(p => !p.IsDeleted)
                .FirstOrDefaultAsync(p => p.Id == id, ct);

            if (page == null)
            {
                return Results.Problem(
                    title: "Not Found",
                    detail: $"Page with ID {id} not found",
                    statusCode: 404);
            }

            // Sanitize content BEFORE database write (XSS prevention)
            var cleanContent = sanitizer.Sanitize(request.Content);

            if (string.IsNullOrWhiteSpace(cleanContent))
            {
                return Results.Problem(
                    title: "Bad Request",
                    detail: "Content is empty after sanitization",
                    statusCode: 400);
            }

            // If slug is being changed, validate uniqueness (globally unique, including deleted pages)
            if (!string.IsNullOrWhiteSpace(request.Slug) && request.Slug != page.Slug)
            {
                var slugExists = await db.ContentPages.AnyAsync(p => p.Slug == request.Slug && p.Id != id, ct);
                if (slugExists)
                {
                    return Results.Problem(
                        title: "Conflict",
                        detail: $"A page with slug '{request.Slug}' already exists. Please choose a different slug.",
                        statusCode: 409);
                }
            }

            // Use domain method to update content (creates revision automatically)
            try
            {
                page.UpdateContent(
                    cleanContent,
                    request.Title,
                    userId,
                    request.ChangeDescription,
                    request.Slug
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
                return Results.Problem(
                    title: "Validation Error",
                    detail: ex.Message,
                    statusCode: 400);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "Invalid operation updating page {PageId}", id);
                return Results.Problem(
                    title: "Bad Request",
                    detail: ex.Message,
                    statusCode: 400);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating page {PageId}", id);
                return Results.Problem(
                    title: "Internal Server Error",
                    detail: "An error occurred while updating the page",
                    statusCode: 500);
            }
        }

        /// <summary>
        /// DELETE /api/cms/pages/{id} - Soft delete a CMS page.
        /// Sets IsDeleted=true, records who deleted it and when.
        /// The page and its revisions remain in the database for data integrity.
        /// </summary>
        private static async Task<IResult> DeletePage(
            HttpContext context,
            IAntiforgery antiforgery,
            int id,
            ClaimsPrincipal user,
            [FromServices] ApplicationDbContext db,
            [FromServices] ILogger<Program> logger,
            CancellationToken ct)
        {
            // Validate CSRF token
            try
            {
                await antiforgery.ValidateRequestAsync(context);
            }
            catch (AntiforgeryValidationException)
            {
                return Results.Problem(
                    title: "CSRF Validation Failed",
                    detail: "Antiforgery token validation failed. Please refresh the page and try again.",
                    statusCode: 400);
            }

            // Extract user ID from claims
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                logger.LogWarning("DeletePage called without valid user ID claim");
                return Results.Problem(
                    title: "Unauthorized",
                    detail: "User authentication failed - missing or invalid user identifier",
                    statusCode: 401);
            }

            // Fetch page - only allow deleting non-deleted pages
            var page = await db.ContentPages
                .Where(p => !p.IsDeleted)
                .FirstOrDefaultAsync(p => p.Id == id, ct);

            if (page == null)
            {
                return Results.Problem(
                    title: "Not Found",
                    detail: $"Page with ID {id} not found",
                    statusCode: 404);
            }

            // Use domain method for soft delete
            page.SoftDelete(userId);
            await db.SaveChangesAsync(ct);

            logger.LogInformation(
                "CMS page {PageId} ({Slug}) soft-deleted by user {UserId}.",
                page.Id,
                page.Slug,
                userId
            );

            return Results.NoContent();
        }

        /// <summary>
        /// GET /api/cms/pages/{id}/revisions - Fetch revision history.
        /// Excludes soft-deleted pages (no revision access for deleted pages).
        /// </summary>
        private static async Task<IResult> GetPageRevisions(
            int id,
            [FromServices] ApplicationDbContext db,
            CancellationToken ct)
        {
            // Check if page exists and is not soft-deleted
            var pageExists = await db.ContentPages.AnyAsync(p => p.Id == id && !p.IsDeleted, ct);
            if (!pageExists)
            {
                return Results.Problem(
                    title: "Not Found",
                    detail: $"Page with ID {id} not found",
                    statusCode: 404);
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
                    CreatedBySceneName = r.CreatedByUser != null && !string.IsNullOrWhiteSpace(r.CreatedByUser.SceneName)
                        ? r.CreatedByUser.SceneName
                        : r.CreatedByUser != null ? r.CreatedByUser.Email : "Unknown",
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
        /// GET /api/cms/pages - List all non-deleted pages with revision counts.
        /// Soft-deleted pages are excluded from this list.
        /// </summary>
        private static async Task<IResult> GetAllPages(
            [FromServices] ApplicationDbContext db,
            CancellationToken ct)
        {
            var pages = await db.ContentPages
                .Include(p => p.LastModifiedByUser)
                .Include(p => p.Revisions)
                .Where(p => !p.IsDeleted)
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

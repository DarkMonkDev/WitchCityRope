using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.Cms.Dtos
{
    /// <summary>
    /// Request DTO for creating a new CMS content page.
    /// Used for POST /api/cms/pages.
    /// Slug must be globally unique and match the format: lowercase alphanumeric with hyphens (e.g., "about-us").
    /// </summary>
    public record CreateContentPageRequest
    {
        /// <summary>
        /// Page title (3-200 characters)
        /// </summary>
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "Title must be between 3 and 200 characters")]
        public string Title { get; init; } = string.Empty;

        /// <summary>
        /// URL-friendly slug (e.g., "about-us"). Must be unique across all pages (including deleted).
        /// Format: lowercase letters, numbers, and hyphens only. No leading/trailing hyphens.
        /// </summary>
        [Required(ErrorMessage = "Slug is required")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Slug must be between 1 and 100 characters")]
        [RegularExpression(@"^[a-z0-9]+(-[a-z0-9]+)*$", ErrorMessage = "Slug must contain only lowercase letters, numbers, and hyphens (e.g., 'about-us')")]
        public string Slug { get; init; } = string.Empty;

        /// <summary>
        /// Initial HTML content for the page (will be sanitized server-side)
        /// </summary>
        [Required(ErrorMessage = "Content is required")]
        public string Content { get; init; } = string.Empty;
    }
}

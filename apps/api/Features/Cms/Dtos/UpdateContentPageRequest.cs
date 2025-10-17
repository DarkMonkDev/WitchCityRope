using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.Cms.Dtos
{
    /// <summary>
    /// Request DTO for updating content page
    /// Used for PUT /api/cms/pages/{id}
    /// </summary>
    public record UpdateContentPageRequest
    {
        /// <summary>
        /// Updated page title (3-200 characters)
        /// </summary>
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "Title must be between 3 and 200 characters")]
        public string Title { get; init; } = string.Empty;

        /// <summary>
        /// Updated HTML content (will be sanitized server-side)
        /// </summary>
        [Required(ErrorMessage = "Content is required")]
        public string Content { get; init; } = string.Empty;

        /// <summary>
        /// Optional description of changes for revision history
        /// </summary>
        [StringLength(500, ErrorMessage = "Change description cannot exceed 500 characters")]
        public string? ChangeDescription { get; init; }
    }
}

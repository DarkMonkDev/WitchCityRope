namespace WitchCityRope.Api.Features.Cms.Dtos
{
    /// <summary>
    /// DTO for content page responses
    /// Used for GET /api/cms/pages/{slug} and PUT /api/cms/pages/{id}
    /// </summary>
    public record ContentPageDto
    {
        /// <summary>
        /// Unique identifier for the content page
        /// </summary>
        public int Id { get; init; }

        /// <summary>
        /// URL-friendly slug for the page (e.g., "resources", "contact-us")
        /// </summary>
        public string Slug { get; init; } = string.Empty;

        /// <summary>
        /// Page title displayed in browser and UI
        /// </summary>
        public string Title { get; init; } = string.Empty;

        /// <summary>
        /// Sanitized HTML content of the page
        /// </summary>
        public string Content { get; init; } = string.Empty;

        /// <summary>
        /// Timestamp of last update (UTC)
        /// </summary>
        public DateTime UpdatedAt { get; init; }

        /// <summary>
        /// Email of user who last modified the page
        /// </summary>
        public string LastModifiedBy { get; init; } = string.Empty;

        /// <summary>
        /// Whether the page is published and visible to public
        /// </summary>
        public bool IsPublished { get; init; }
    }
}

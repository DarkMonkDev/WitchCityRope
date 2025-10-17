namespace WitchCityRope.Api.Features.Cms.Dtos
{
    /// <summary>
    /// DTO for content page list summaries
    /// Used for GET /api/cms/pages
    /// </summary>
    public record CmsPageSummaryDto
    {
        /// <summary>
        /// Unique identifier for the content page
        /// </summary>
        public int Id { get; init; }

        /// <summary>
        /// URL-friendly slug for the page
        /// </summary>
        public string Slug { get; init; } = string.Empty;

        /// <summary>
        /// Page title
        /// </summary>
        public string Title { get; init; } = string.Empty;

        /// <summary>
        /// Number of revisions for this page
        /// </summary>
        public int RevisionCount { get; init; }

        /// <summary>
        /// Timestamp of last update (UTC)
        /// </summary>
        public DateTime UpdatedAt { get; init; }

        /// <summary>
        /// Email of user who last modified the page
        /// </summary>
        public string LastModifiedBy { get; init; } = string.Empty;

        /// <summary>
        /// Whether the page is published
        /// </summary>
        public bool IsPublished { get; init; }
    }
}

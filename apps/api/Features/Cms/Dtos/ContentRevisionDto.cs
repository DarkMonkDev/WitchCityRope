namespace WitchCityRope.Api.Features.Cms.Dtos
{
    /// <summary>
    /// DTO for content revision responses
    /// Used for GET /api/cms/pages/{id}/revisions
    /// </summary>
    public record ContentRevisionDto
    {
        /// <summary>
        /// Unique identifier for the revision
        /// </summary>
        public int Id { get; init; }

        /// <summary>
        /// ID of the content page this revision belongs to
        /// </summary>
        public int ContentPageId { get; init; }

        /// <summary>
        /// Timestamp when this revision was created (UTC)
        /// </summary>
        public DateTime CreatedAt { get; init; }

        /// <summary>
        /// Email of user who created this revision
        /// </summary>
        public string CreatedBy { get; init; } = string.Empty;

        /// <summary>
        /// Optional description of what changed
        /// </summary>
        public string? ChangeDescription { get; init; }

        /// <summary>
        /// Preview of content (first 200 characters)
        /// </summary>
        public string ContentPreview { get; init; } = string.Empty;

        /// <summary>
        /// Title at the time of this revision
        /// </summary>
        public string Title { get; init; } = string.Empty;

        /// <summary>
        /// Full content of the revision (only included when explicitly requested)
        /// </summary>
        public string? FullContent { get; init; }
    }
}

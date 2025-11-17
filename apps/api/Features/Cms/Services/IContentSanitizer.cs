namespace WitchCityRope.Api.Features.Cms.Services
{
    /// <summary>
    /// Interface for sanitizing HTML content to prevent XSS attacks
    /// Configured to allow TipTap editor formatting tags only
    /// </summary>
    public interface IContentSanitizer
    {
        /// <summary>
        /// Sanitizes HTML content by removing all disallowed tags, attributes, and scripts
        /// </summary>
        /// <param name="html">Raw HTML content from user input</param>
        /// <returns>Sanitized HTML safe for database storage and display</returns>
        string Sanitize(string html);

        /// <summary>
        /// Validates whether HTML content contains any forbidden elements
        /// Useful for providing user feedback before save
        /// </summary>
        /// <param name="html">HTML content to validate</param>
        /// <returns>True if content is safe, false if it contains forbidden elements</returns>
        bool IsContentSafe(string html);
    }
}

using Ganss.Xss;

namespace WitchCityRope.Api.Features.Cms.Services
{
    /// <summary>
    /// Service for sanitizing HTML content to prevent XSS attacks
    /// Configured to allow TipTap editor formatting tags only
    /// </summary>
    public class ContentSanitizer
    {
        private readonly HtmlSanitizer _sanitizer;

        public ContentSanitizer()
        {
            _sanitizer = new HtmlSanitizer();

            // Clear default allowed tags
            _sanitizer.AllowedTags.Clear();

            // Allow TipTap formatting tags (safe subset of HTML)
            _sanitizer.AllowedTags.Add("p");
            _sanitizer.AllowedTags.Add("br");
            _sanitizer.AllowedTags.Add("strong");
            _sanitizer.AllowedTags.Add("em");
            _sanitizer.AllowedTags.Add("u");
            _sanitizer.AllowedTags.Add("s");
            _sanitizer.AllowedTags.Add("strike");
            _sanitizer.AllowedTags.Add("h1");
            _sanitizer.AllowedTags.Add("h2");
            _sanitizer.AllowedTags.Add("h3");
            _sanitizer.AllowedTags.Add("h4");
            _sanitizer.AllowedTags.Add("h5");
            _sanitizer.AllowedTags.Add("h6");
            _sanitizer.AllowedTags.Add("ul");
            _sanitizer.AllowedTags.Add("ol");
            _sanitizer.AllowedTags.Add("li");
            _sanitizer.AllowedTags.Add("a");
            _sanitizer.AllowedTags.Add("blockquote");
            _sanitizer.AllowedTags.Add("code");
            _sanitizer.AllowedTags.Add("pre");

            // Clear default allowed attributes
            _sanitizer.AllowedAttributes.Clear();

            // Allow specific safe attributes
            _sanitizer.AllowedAttributes.Add("href");     // For links
            _sanitizer.AllowedAttributes.Add("title");    // For tooltips
            _sanitizer.AllowedAttributes.Add("class");    // For styling

            // Clear default allowed CSS properties
            _sanitizer.AllowedCssProperties.Clear();

            // Allow safe CSS properties
            _sanitizer.AllowedCssProperties.Add("text-align");

            // Clear default allowed URI schemes
            _sanitizer.AllowedSchemes.Clear();

            // Allow only safe URL schemes
            _sanitizer.AllowedSchemes.Add("http");
            _sanitizer.AllowedSchemes.Add("https");
            _sanitizer.AllowedSchemes.Add("mailto");

            // Security settings
            _sanitizer.KeepChildNodes = true;    // Preserve text content when removing tags
            _sanitizer.AllowDataAttributes = false;  // Block data-* attributes (XSS vector)
        }

        /// <summary>
        /// Sanitizes HTML content by removing all disallowed tags, attributes, and scripts
        /// </summary>
        /// <param name="html">Raw HTML content from user input</param>
        /// <returns>Sanitized HTML safe for database storage and display</returns>
        public string Sanitize(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return string.Empty;
            }

            return _sanitizer.Sanitize(html);
        }

        /// <summary>
        /// Validates whether HTML content contains any forbidden elements
        /// Useful for providing user feedback before save
        /// </summary>
        /// <param name="html">HTML content to validate</param>
        /// <returns>True if content is safe, false if it contains forbidden elements</returns>
        public bool IsContentSafe(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return true;
            }

            var sanitized = Sanitize(html);
            return sanitized == html;
        }
    }
}

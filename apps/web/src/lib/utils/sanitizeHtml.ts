import DOMPurify from 'dompurify'

/**
 * Sanitize HTML content to prevent XSS attacks.
 * Allowed tags/attributes are configured to match the backend ContentSanitizer.cs whitelist.
 * IMPORTANT: Keep this in sync with apps/api/Features/Cms/Services/ContentSanitizer.cs
 */
export function sanitizeHtml(dirty: string): string {
  return DOMPurify.sanitize(dirty, {
    ALLOWED_TAGS: [
      // Text formatting (TipTap StarterKit + extensions)
      'p', 'br', 'strong', 'em', 'u', 's', 'b', 'i', 'strike',
      // Headings
      'h1', 'h2', 'h3', 'h4', 'h5', 'h6',
      // Lists
      'ul', 'ol', 'li',
      // Links
      'a',
      // Block elements
      'blockquote', 'pre', 'code', 'hr',
      // TipTap extensions: Superscript, Subscript, Highlight, Color/TextStyle
      'sup', 'sub', 'mark', 'span',
    ],
    ALLOWED_ATTR: [
      'href', 'target', 'rel', 'title',  // Links
      'class', 'style',                   // Styling
    ],
    ALLOW_DATA_ATTR: false,
  })
}

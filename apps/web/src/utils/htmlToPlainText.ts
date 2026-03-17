/**
 * Convert HTML content to plain text.
 *
 * Used by email template editors to generate the plainTextBody field
 * from the rich HTML editor content before saving to the backend.
 *
 * Handles common HTML patterns: line breaks, paragraphs, block elements,
 * and non-breaking spaces.
 */
export function htmlToPlainText(html: string): string {
  return html
    .replace(/<br\s*\/?>/gi, '\n')
    .replace(/<\/p>/gi, '\n\n')
    .replace(/<[^>]+>/g, '')
    .replace(/&nbsp;/g, ' ')
    .trim();
}

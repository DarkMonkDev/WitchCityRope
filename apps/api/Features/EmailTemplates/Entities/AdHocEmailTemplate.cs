using System.ComponentModel.DataAnnotations;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.EmailTemplates.Entities;

/// <summary>
/// Represents a saved ad-hoc email template for reuse
/// Only created via "Save as Template" feature on Ad Hoc tab
/// Can be deleted by users (unlike other template categories)
/// </summary>
public class AdHocEmailTemplate
{
    /// <summary>
    /// Primary key (EF Core manages - NO initializer)
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Template name (from subject or custom)
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string TemplateName { get; set; } = string.Empty;

    /// <summary>
    /// Email subject line
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// HTML email body with {{variable}} placeholders
    /// </summary>
    [Required]
    public string HtmlBody { get; set; } = string.Empty;

    /// <summary>
    /// Plain text email body
    /// </summary>
    [Required]
    public string PlainTextBody { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when template was created (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// User ID who created this template
    /// </summary>
    [Required]
    public Guid CreatedBy { get; set; }

    // Navigation property
    public ApplicationUser CreatedByUser { get; set; } = null!;
}

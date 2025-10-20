using System.ComponentModel.DataAnnotations;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Safety.Entities;

/// <summary>
/// Note/comment for incident coordination
/// Mirrors ApplicationNoteDto pattern from vetting system
/// </summary>
public class IncidentNote
{
    public IncidentNote()
    {
        Id = Guid.NewGuid();
        Content = string.Empty;
        Type = IncidentNoteType.Manual;
        IsPrivate = false;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Related incident ID
    /// </summary>
    [Required]
    public Guid IncidentId { get; set; }

    /// <summary>
    /// Note content (will be encrypted by service layer)
    /// </summary>
    [Required]
    public string Content { get; set; }

    /// <summary>
    /// Note type: Manual (user-created) or System (auto-generated)
    /// </summary>
    [Required]
    public IncidentNoteType Type { get; set; }

    /// <summary>
    /// Private notes visible only to coordinators/admins
    /// </summary>
    [Required]
    public bool IsPrivate { get; set; }

    /// <summary>
    /// Note author - NULL for system-generated notes
    /// </summary>
    public Guid? AuthorId { get; set; }

    /// <summary>
    /// Optional tags for categorization (comma-separated)
    /// </summary>
    [MaxLength(200)]
    public string? Tags { get; set; }

    /// <summary>
    /// When note was created (UTC)
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When note was last updated (UTC)
    /// NULL if never edited
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    /// <summary>
    /// Related incident
    /// </summary>
    public SafetyIncident Incident { get; set; } = null!;

    /// <summary>
    /// Note author (NULL for system notes)
    /// </summary>
    public ApplicationUser? Author { get; set; }
}

/// <summary>
/// Type of incident note
/// </summary>
public enum IncidentNoteType
{
    /// <summary>
    /// User-created note (coordinator or admin)
    /// </summary>
    Manual = 1,

    /// <summary>
    /// System-generated note (status changes, assignments, etc.)
    /// </summary>
    System = 2
}

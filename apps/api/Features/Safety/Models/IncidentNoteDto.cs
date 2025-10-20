using WitchCityRope.Api.Features.Safety.Entities;

namespace WitchCityRope.Api.Features.Safety.Models;

/// <summary>
/// Data transfer object for incident notes
/// </summary>
public class IncidentNoteDto
{
    /// <summary>
    /// Note ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Related incident ID
    /// </summary>
    public Guid IncidentId { get; set; }

    /// <summary>
    /// Note content (decrypted)
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Note type: Manual or System
    /// </summary>
    public IncidentNoteType Type { get; set; }

    /// <summary>
    /// Whether note is private (visible only to coordinators/admins)
    /// </summary>
    public bool IsPrivate { get; set; }

    /// <summary>
    /// Author user ID (NULL for system notes)
    /// </summary>
    public Guid? AuthorId { get; set; }

    /// <summary>
    /// Author name (NULL for system notes)
    /// </summary>
    public string? AuthorName { get; set; }

    /// <summary>
    /// Optional tags (comma-separated)
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// When note was created (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When note was last updated (UTC), NULL if never edited
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}

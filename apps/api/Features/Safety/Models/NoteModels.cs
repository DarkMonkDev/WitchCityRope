namespace WitchCityRope.Api.Features.Safety.Models;

/// <summary>
/// Request to add manual note to incident
/// </summary>
public class AddNoteRequest
{
    /// <summary>
    /// Note content (min 3 chars)
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Whether note is private (visible only to coordinators/admins)
    /// </summary>
    public bool IsPrivate { get; set; }

    /// <summary>
    /// Optional tags (comma-separated)
    /// </summary>
    public string? Tags { get; set; }
}

/// <summary>
/// Request to update existing manual note
/// </summary>
public class UpdateNoteRequest
{
    /// <summary>
    /// Updated content (min 3 chars)
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Whether note is private
    /// </summary>
    public bool IsPrivate { get; set; }

    /// <summary>
    /// Optional tags (comma-separated)
    /// </summary>
    public string? Tags { get; set; }
}

/// <summary>
/// Response containing list of notes
/// </summary>
public class NotesListResponse
{
    /// <summary>
    /// List of notes (ordered by CreatedAt DESC)
    /// </summary>
    public List<IncidentNoteDto> Notes { get; set; } = new();
}

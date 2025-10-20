using System.ComponentModel.DataAnnotations;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Data.Entities;

/// <summary>
/// User notes entity for storing ALL types of notes about users (vetting, general, administrative, status changes)
/// Unified note system with type differentiation via NoteType field
/// Replaces legacy AdminNotes text field approach with proper structured storage
/// </summary>
public class UserNote
{
    public UserNote()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    public UserNote(Guid userId, string content, string noteType, Guid? authorId = null)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Content = content;
        NoteType = noteType;
        AuthorId = authorId;
        CreatedAt = DateTime.UtcNow;
        IsArchived = false;
    }

    /// <summary>
    /// Primary key
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User this note is about (FK to ApplicationUser)
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Note content/text
    /// </summary>
    [Required]
    [MaxLength(5000)]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Type of note: "Vetting", "General", "Administrative", "StatusChange"
    /// Allows filtering and grouping by note purpose
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string NoteType { get; set; } = "General";

    /// <summary>
    /// User who authored this note (FK to ApplicationUser, nullable for system-generated notes)
    /// </summary>
    public Guid? AuthorId { get; set; }

    /// <summary>
    /// When the note was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Whether this note has been archived (soft delete)
    /// </summary>
    public bool IsArchived { get; set; } = false;

    // Navigation properties
    /// <summary>
    /// The user this note is about
    /// </summary>
    public ApplicationUser User { get; set; } = null!;

    /// <summary>
    /// The user who authored this note (nullable for system-generated notes)
    /// </summary>
    public ApplicationUser? Author { get; set; }
}

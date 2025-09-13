using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace WitchCityRope.Api.Features.Vetting.Entities;

/// <summary>
/// Internal notes and comments for reviewer collaboration
/// Supports private reviewer notes and public applicant communications
/// </summary>
public class VettingApplicationNote
{
    public VettingApplicationNote()
    {
        Id = Guid.NewGuid();
        IsPrivate = true;
        Type = NoteType.General;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        
        Attachments = new List<VettingNoteAttachment>();
    }

    // Primary Key
    public Guid Id { get; set; }

    // Application Reference
    public Guid ApplicationId { get; set; }
    public Guid ReviewerId { get; set; }

    // Note Content
    public string Content { get; set; } = string.Empty;
    public NoteType Type { get; set; } = NoteType.General;
    public bool IsPrivate { get; set; } // True = internal only, False = visible to applicant

    // Categorization
    public string TagsJson { get; set; } = "[]";
    [NotMapped]
    public List<string> Tags 
    { 
        get => JsonSerializer.Deserialize<List<string>>(TagsJson) ?? new List<string>();
        set => TagsJson = JsonSerializer.Serialize(value);
    }

    // Standard Audit Fields
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation Properties
    public VettingApplication Application { get; set; } = null!;
    public VettingReviewer Reviewer { get; set; } = null!;
    public ICollection<VettingNoteAttachment> Attachments { get; set; }
}

/// <summary>
/// Note types for categorization and workflow management
/// </summary>
public enum NoteType
{
    General = 1,
    Concern = 2,
    Clarification = 3,
    ReferenceNote = 4,
    InterviewNote = 5,
    DecisionRationale = 6
}
namespace WitchCityRope.Api.Features.Vetting.Entities;

/// <summary>
/// File attachments for reviewer notes
/// Supports encrypted file storage with integrity checking
/// </summary>
public class VettingNoteAttachment
{
    public VettingNoteAttachment()
    {
        Id = Guid.NewGuid();
        IsConfidential = true;
        CreatedAt = DateTime.UtcNow;
    }

    // Primary Key
    public Guid Id { get; set; }

    // Note Reference
    public Guid NoteId { get; set; }

    // File Information
    public string FileName { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string MimeType { get; set; } = string.Empty;

    // Storage Information
    public string StoragePath { get; set; } = string.Empty; // Path to encrypted file
    public string FileHash { get; set; } = string.Empty; // SHA-256 hash for integrity

    // Access Control
    public bool IsConfidential { get; set; } = true;

    // Timestamp
    public DateTime CreatedAt { get; set; }

    // Navigation Properties
    public VettingApplicationNote Note { get; set; } = null!;
}
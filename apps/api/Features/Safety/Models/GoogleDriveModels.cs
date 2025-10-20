namespace WitchCityRope.Api.Features.Safety.Models;

/// <summary>
/// Request to update Google Drive links
/// </summary>
public class UpdateGoogleDriveRequest
{
    /// <summary>
    /// Google Drive folder URL (NULL to clear)
    /// </summary>
    public string? GoogleDriveFolderUrl { get; set; }

    /// <summary>
    /// Google Drive final report URL (NULL to clear)
    /// </summary>
    public string? GoogleDriveFinalReportUrl { get; set; }
}

/// <summary>
/// Response after updating Google Drive links
/// </summary>
public class GoogleDriveUpdateResponse
{
    /// <summary>
    /// Incident ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Updated folder URL
    /// </summary>
    public string? GoogleDriveFolderUrl { get; set; }

    /// <summary>
    /// Updated final report URL
    /// </summary>
    public string? GoogleDriveFinalReportUrl { get; set; }

    /// <summary>
    /// Last updated timestamp
    /// </summary>
    public DateTime LastUpdatedAt { get; set; }

    /// <summary>
    /// Whether system note was created
    /// </summary>
    public bool SystemNoteCreated { get; set; }
}

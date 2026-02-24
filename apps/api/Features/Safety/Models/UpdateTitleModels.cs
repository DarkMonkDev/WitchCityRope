using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.Safety.Models;

/// <summary>
/// Request to update an incident's title
/// </summary>
public class UpdateTitleRequest
{
    /// <summary>
    /// New title for the incident (required, max 200 characters)
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
}

/// <summary>
/// Response after updating incident title
/// </summary>
public class UpdateTitleResponse
{
    /// <summary>
    /// Incident ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Updated title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Last updated timestamp
    /// </summary>
    public DateTime LastUpdatedAt { get; set; }
}

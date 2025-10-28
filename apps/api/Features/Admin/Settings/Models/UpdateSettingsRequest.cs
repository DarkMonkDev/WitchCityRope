using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.Admin.Settings.Models;

/// <summary>
/// Request model for updating multiple settings
/// </summary>
public class UpdateSettingsRequest
{
    /// <summary>
    /// Dictionary of setting key-value pairs to update
    /// </summary>
    [Required]
    public Dictionary<string, string> Settings { get; set; } = new();
}

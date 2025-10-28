using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Core.Entities;

/// <summary>
/// Application-wide settings stored in database
/// Key-value pairs for configuration that survives deployments
/// </summary>
public class Setting
{
    /// <summary>
    /// Primary key
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Setting key (unique identifier)
    /// Examples: "EventTimeZone", "PreStartBufferMinutes"
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Setting value (stored as string, parsed as needed)
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable description of what this setting controls
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// When setting was created
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When setting was last updated
    /// </summary>
    [Required]
    public DateTime UpdatedAt { get; set; }
}

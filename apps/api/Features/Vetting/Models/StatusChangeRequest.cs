using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.Vetting.Models;

/// <summary>
/// Request model for changing application status
/// </summary>
public class StatusChangeRequest
{
    [Required]
    [StringLength(100)]
    public string Status { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string Reasoning { get; set; } = string.Empty;
}

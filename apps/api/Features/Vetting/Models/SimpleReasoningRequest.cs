using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.Vetting.Models;

/// <summary>
/// Simple request model for operations requiring only reasoning
/// </summary>
public class SimpleReasoningRequest
{
    [Required]
    [StringLength(1000)]
    public string Reasoning { get; set; } = string.Empty;
}

using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.Vetting.Models;

/// <summary>
/// Simple request model for adding notes to applications
/// </summary>
public class SimpleNoteRequest
{
    [Required]
    [StringLength(2000)]
    public string Note { get; set; } = string.Empty;

    public bool? IsPrivate { get; set; }

    public List<string>? Tags { get; set; }
}

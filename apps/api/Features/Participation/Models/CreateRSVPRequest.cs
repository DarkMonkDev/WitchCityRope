using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.Participation.Models;

/// <summary>
/// Request model for creating an RSVP
/// </summary>
public class CreateRSVPRequest
{
    /// <summary>
    /// Event ID to RSVP for
    /// </summary>
    [Required]
    public Guid EventId { get; set; }

    /// <summary>
    /// Optional notes from the participant
    /// </summary>
    [MaxLength(2000)]
    public string? Notes { get; set; }
}
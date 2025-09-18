using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.Events.Models;

/// <summary>
/// Placeholder CreateEventRequest for test compilation
/// TODO: Implement full request structure when Events feature is developed
/// </summary>
public class CreateEventRequest
{
    [Required]
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public string Location { get; set; } = string.Empty;

    public string EventType { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Capacity { get; set; }

    public decimal[]? PricingTiers { get; set; }
}
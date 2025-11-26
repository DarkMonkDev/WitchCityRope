using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.Events.Models;

/// <summary>
/// Request model for copying an existing event with a new date and title
/// </summary>
public record CopyEventRequest
{
    /// <summary>
    /// New start date for the copied event (must be in the future)
    /// </summary>
    [Required(ErrorMessage = "New start date is required")]
    public required DateTimeOffset NewStartDate { get; init; }

    /// <summary>
    /// New title for the copied event
    /// </summary>
    [Required(ErrorMessage = "Title is required")]
    [MinLength(3, ErrorMessage = "Title must be at least 3 characters")]
    [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
    public required string NewTitle { get; init; }
}

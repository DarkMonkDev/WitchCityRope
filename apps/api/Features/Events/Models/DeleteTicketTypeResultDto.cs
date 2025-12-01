namespace WitchCityRope.Api.Features.Events.Models;

/// <summary>
/// DTO for the result of deleting a ticket type
/// </summary>
public class DeleteTicketTypeResultDto
{
    /// <summary>
    /// Whether the deletion was successful
    /// </summary>
    public bool Success { get; set; }
}

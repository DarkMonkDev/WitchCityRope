using System.Collections.Generic;

namespace WitchCityRope.Api.Features.Events.Models;

/// <summary>
/// DTO for the result of deleting a session
/// </summary>
public class DeleteSessionResultDto
{
    /// <summary>
    /// Whether the deletion was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Number of RSVPs that were cancelled
    /// </summary>
    public int RsvpsCancelled { get; set; }

    /// <summary>
    /// Number of volunteer signups that were cancelled
    /// </summary>
    public int VolunteerSignupsCancelled { get; set; }

    /// <summary>
    /// List of ticket type names that were deleted
    /// </summary>
    public List<string>? DeletedTicketTypes { get; set; }
}

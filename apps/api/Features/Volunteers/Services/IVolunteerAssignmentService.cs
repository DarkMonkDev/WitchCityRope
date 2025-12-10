using WitchCityRope.Api.Features.Volunteers.Models;

namespace WitchCityRope.Api.Features.Volunteers.Services;

/// <summary>
/// Interface for admin/safety team volunteer position assignment management
/// Handles assigning members to positions, viewing assignments, and removing assignments
/// </summary>
public interface IVolunteerAssignmentService
{
    /// <summary>
    /// Get all member assignments for a volunteer position
    /// Returns list of users currently assigned with their contact information
    /// </summary>
    /// <param name="positionId">Volunteer position ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success tuple with list of assignments or error message</returns>
    Task<(bool success, List<VolunteerAssignmentDto>? assignments, string? error)> GetPositionSignupsAsync(
        Guid positionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Assign a member to a volunteer position
    /// Checks for position capacity and existing participations before assignment
    /// </summary>
    /// <param name="positionId">Volunteer position ID</param>
    /// <param name="userId">User ID to assign</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success tuple with assignment details or error message</returns>
    Task<(bool success, VolunteerAssignmentDto? assignment, string? error)> AssignMemberToPositionAsync(
        Guid positionId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove a member assignment from a volunteer position
    /// Only allows removal if user has not checked in yet
    /// </summary>
    /// <param name="signupId">Volunteer signup ID to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success tuple with error message if failed</returns>
    Task<(bool success, string? error)> RemoveAssignmentAsync(
        Guid signupId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Search for active members by name, email, or Discord name
    /// Excludes inactive users and requires minimum 3 characters
    /// </summary>
    /// <param name="searchQuery">Search term (min 3 characters)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success tuple with list of matching users or error message</returns>
    Task<(bool success, List<UserSearchResultDto>? users, string? error)> SearchUsersAsync(
        string searchQuery,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel all volunteer signups for a user for a specific event
    /// Used when a ticket is refunded to automatically cancel volunteer commitments
    /// </summary>
    /// <param name="userId">User ID whose volunteer signups should be cancelled</param>
    /// <param name="eventId">Event ID to cancel volunteer signups for</param>
    /// <param name="cancellationReason">Reason for cancellation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success tuple with count of cancelled signups or error message</returns>
    Task<(bool success, int cancelledCount, string? error)> CancelAllVolunteerSignupsForUserEventAsync(
        Guid userId,
        Guid eventId,
        string cancellationReason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel volunteer signups for a user for specific sessions only
    /// Used when a ticket is cancelled to only cancel volunteer commitments for those sessions
    /// Preserves volunteer signups for sessions the user still has tickets for
    /// </summary>
    /// <param name="userId">User ID whose volunteer signups should be cancelled</param>
    /// <param name="eventId">Event ID to cancel volunteer signups for</param>
    /// <param name="sessionIds">Session IDs to cancel volunteer signups for</param>
    /// <param name="cancellationReason">Reason for cancellation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success tuple with count of cancelled signups or error message</returns>
    Task<(bool success, int cancelledCount, string? error)> CancelVolunteerSignupsForSessionsAsync(
        Guid userId,
        Guid eventId,
        List<Guid> sessionIds,
        string cancellationReason,
        CancellationToken cancellationToken = default);
}

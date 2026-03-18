using WitchCityRope.Api.Features.Shared.Models;
using WitchCityRope.Api.Features.TicketAssignment.Models;

namespace WitchCityRope.Api.Features.TicketAssignment.Services;

/// <summary>
/// Service for managing ticket assignment operations including assign, accept, decline,
/// and reassign workflows. Also provides dashboard queries for pending assignments
/// and assigned ticket status.
///
/// Business rules implemented:
/// - BR-020: Assignment requires authorized contact relationship
/// - BR-012: One ticket per assignee per event/session
/// - BR-022: Assigned tickets enter PendingAcceptance status
/// - BR-025: Declined tickets can be reassigned
/// - BR-030/BR-031/BR-032: Waiver and ToS acceptance requirements
/// - BR-035/BR-036: Vetting checks at assignment and acceptance time
/// - AD-003: Waiver must be personally accepted
/// - AD-015: No separate Declined status - reverts to Active with DeclinedAt set
/// </summary>
public interface ITicketAssignmentService
{
    /// <summary>
    /// Assigns an existing ticket to an authorized contact.
    /// The ticket transitions to PendingAcceptance status with the assignee as the new UserId.
    /// Business rules: BR-020 (authorization), BR-012 (one per person), BR-035 (vetting), AD-007 (post-purchase).
    /// </summary>
    /// <param name="attendanceId">The EventAttendance record to assign</param>
    /// <param name="callerUserId">The authenticated user (must own the ticket)</param>
    /// <param name="assignToUserId">The user to assign the ticket to (must have authorized the caller)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>TicketAssignmentDto on success, or error details</returns>
    Task<Result<TicketAssignmentDto>> AssignTicketAsync(
        Guid attendanceId, Guid callerUserId, Guid assignToUserId, CancellationToken ct);

    /// <summary>
    /// Accepts an assigned ticket or proxy RSVP by the assignee.
    /// Transitions PendingAcceptance to Active after waiver acceptance.
    /// Business rules: AD-003 (personal waiver), BR-031 (ToS), BR-032 (waiver activates),
    /// BR-036 (re-check vetting at acceptance).
    /// </summary>
    /// <param name="attendanceId">The EventAttendance record to accept</param>
    /// <param name="callerUserId">The authenticated user (must be the assigned user)</param>
    /// <param name="request">Waiver and ToS acceptance flags</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>TicketAssignmentDto on success, or error details</returns>
    Task<Result<TicketAssignmentDto>> AcceptAssignmentAsync(
        Guid attendanceId, Guid callerUserId, AcceptAssignmentRequest request, CancellationToken ct);

    /// <summary>
    /// Declines an assigned ticket or proxy RSVP by the assignee.
    /// The ticket reverts to Active status owned by the original purchaser (AD-015).
    /// DeclinedAt is set for audit trail and reassignment eligibility.
    /// Business rules: BR-025 (declined can be reassigned), AD-015 (no Declined status).
    /// </summary>
    /// <param name="attendanceId">The EventAttendance record to decline</param>
    /// <param name="callerUserId">The authenticated user (must be the assigned user)</param>
    /// <param name="reason">Optional reason for declining</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>TicketAssignmentDto on success, or error details</returns>
    Task<Result<TicketAssignmentDto>> DeclineAssignmentAsync(
        Guid attendanceId, Guid callerUserId, string? reason, CancellationToken ct);

    /// <summary>
    /// Reassigns a declined ticket to a new authorized contact.
    /// Only works on tickets that were declined (Status=Active AND DeclinedAt IS NOT NULL).
    /// Business rules: UC-008, BR-020 (authorization), BR-012 (one per person), BR-035 (vetting).
    /// </summary>
    /// <param name="attendanceId">The EventAttendance record to reassign</param>
    /// <param name="callerUserId">The authenticated user (must be the original purchaser)</param>
    /// <param name="newAssigneeUserId">The new user to assign the ticket to</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>TicketAssignmentDto on success, or error details</returns>
    Task<Result<TicketAssignmentDto>> ReassignTicketAsync(
        Guid attendanceId, Guid callerUserId, Guid newAssigneeUserId, CancellationToken ct);

    /// <summary>
    /// Gets all pending assignments for the current user's dashboard (EP-16).
    /// Returns tickets and RSVPs assigned to the user that are still in PendingAcceptance status.
    /// Ordered by event date ascending (most urgent first).
    /// </summary>
    /// <param name="userId">The authenticated user's ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of pending assignments needing acceptance</returns>
    Task<Result<List<PendingAssignmentDto>>> GetPendingAssignmentsAsync(
        Guid userId, CancellationToken ct);

    /// <summary>
    /// Gets assigned ticket status for the purchaser's dashboard (EP-17).
    /// Returns tickets the current user purchased that were assigned to others,
    /// including current status and whether reassignment is possible.
    /// </summary>
    /// <param name="userId">The authenticated user's ID (the purchaser)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of assigned ticket statuses</returns>
    Task<Result<List<AssignedTicketStatusDto>>> GetAssignedTicketsAsync(
        Guid userId, CancellationToken ct);
}

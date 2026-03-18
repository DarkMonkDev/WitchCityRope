using WitchCityRope.Api.Features.ProxyRsvp.Models;
using WitchCityRope.Api.Features.Shared.Models;

namespace WitchCityRope.Api.Features.ProxyRsvp.Services;

/// <summary>
/// Service for managing proxy RSVP operations where a delegate creates, and a principal
/// accepts or declines, an RSVP on the principal's behalf.
///
/// Business rules:
/// - BR-050: Same authorization system as ticket assignment (Authorized Contacts).
/// - BR-051: Proxy RSVPs enter PendingAcceptance state; principal must accept waiver.
/// - BR-053: Delegate cannot accept waiver on behalf of principal.
/// - BR-054: Proxy RSVPs count against event capacity (PendingAcceptance reserves a spot).
///
/// Use cases: UC-005 (Proxy RSVP creation), UC-006 (Accepting a proxy RSVP).
/// </summary>
public interface IProxyRsvpService
{
    /// <summary>
    /// Creates a proxy RSVP for a principal on behalf of a delegate.
    /// The RSVP enters PendingAcceptance status; the principal must accept the event waiver
    /// before the RSVP becomes Active.
    ///
    /// Validates:
    /// - Event exists and AllowRsvps is true
    /// - Delegate is authorized by the principal (BR-050)
    /// - Principal passes vetting check for VettedMembersOnly events (BR-035)
    /// - Event has remaining capacity (BR-054)
    /// - Principal does not already have an Active or PendingAcceptance RSVP (duplicate check)
    /// </summary>
    /// <param name="eventId">The event to RSVP for</param>
    /// <param name="delegateUserId">The delegate creating the proxy RSVP (caller)</param>
    /// <param name="principalUserId">The principal being RSVP'd for</param>
    /// <param name="notes">Optional notes from the delegate</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>ProxyRsvpDto on success, or error result</returns>
    Task<Result<ProxyRsvpDto>> CreateProxyRsvpAsync(
        Guid eventId,
        Guid delegateUserId,
        Guid principalUserId,
        string? notes,
        CancellationToken ct);

    /// <summary>
    /// Accepts a proxy RSVP on behalf of the principal.
    /// Transitions the RSVP from PendingAcceptance to Active.
    /// The caller must be the principal (the person the RSVP was created for).
    ///
    /// Validates:
    /// - Attendance record exists with PendingAcceptance status and RSVP type
    /// - Caller is the principal (UserId matches)
    /// - Event waiver is accepted (AD-003, BR-030, BR-053)
    /// - Vetting re-check for VettedMembersOnly events (BR-036)
    ///
    /// Side effects:
    /// - Updates EventAttendance status to Active with waiver timestamps
    /// - Updates ApplicationUser ToS fields if TermsOfServiceAccepted is true (BR-031)
    /// - Creates or updates EventAttendee record for check-in system
    /// - Creates AttendanceHistory audit record
    /// </summary>
    /// <param name="attendanceId">The EventAttendance record ID</param>
    /// <param name="callerUserId">The authenticated user accepting (must be the principal)</param>
    /// <param name="request">Acceptance details including waiver and ToS flags</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated ProxyRsvpDto on success, or error result</returns>
    Task<Result<ProxyRsvpDto>> AcceptProxyRsvpAsync(
        Guid attendanceId,
        Guid callerUserId,
        AcceptProxyRsvpRequest request,
        CancellationToken ct);

    /// <summary>
    /// Declines a proxy RSVP. Unlike ticket declines, declined RSVPs are simply cancelled
    /// (no reassignment flow). The delegate can create a new proxy RSVP if desired.
    ///
    /// Validates:
    /// - Attendance record exists with PendingAcceptance status and RSVP type
    /// - Caller is the principal (UserId matches)
    ///
    /// Side effects:
    /// - Updates EventAttendance status to Cancelled with decline timestamps
    /// - Creates AttendanceHistory audit record
    /// </summary>
    /// <param name="attendanceId">The EventAttendance record ID</param>
    /// <param name="callerUserId">The authenticated user declining (must be the principal)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Success or error result</returns>
    Task<Result> DeclineProxyRsvpAsync(
        Guid attendanceId,
        Guid callerUserId,
        CancellationToken ct);
}

namespace WitchCityRope.Api.Features.Participation.Entities;

/// <summary>
/// Status of event attendance
/// </summary>
public enum AttendanceStatus
{
    /// <summary>
    /// Active attendance
    /// </summary>
    Active = 1,

    /// <summary>
    /// Cancelled attendance
    /// </summary>
    Cancelled = 2,

    /// <summary>
    /// Refunded attendance (for tickets)
    /// </summary>
    Refunded = 3,

    /// <summary>
    /// Waitlisted attendance
    /// </summary>
    Waitlisted = 4,

    /// <summary>
    /// Pending payment - attendance created but payment not yet confirmed.
    /// Transitions to Active on payment completion, or Cancelled on payment failure/timeout.
    /// </summary>
    PendingPayment = 5,

    /// <summary>
    /// Ticket or RSVP has been assigned to a user but they have not yet
    /// accepted (signed waiver + ToS). Transitions to Active on acceptance
    /// or returns to assignable state on decline.
    /// Created by: Ticket assignment or proxy RSVP
    /// Transitions to: Active (on acceptance), Cancelled (on decline/expiry)
    /// </summary>
    PendingAcceptance = 6
}

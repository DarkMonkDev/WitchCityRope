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
    PendingPayment = 5
}

namespace WitchCityRope.Api.Features.Events;

/// <summary>
/// Types of actions users can perform on events that require timing validation
/// </summary>
public enum EventActionType
{
    /// <summary>RSVP creation for social events</summary>
    GetRsvp,

    /// <summary>RSVP cancellation</summary>
    CancelRsvp,

    /// <summary>Ticket purchase for classes/workshops</summary>
    GetTicket,

    /// <summary>Ticket cancellation/refund</summary>
    CancelTicket,

    /// <summary>Volunteer spot signup</summary>
    GetVolunteer,

    /// <summary>Volunteer assignment cancellation</summary>
    CancelVolunteer
}

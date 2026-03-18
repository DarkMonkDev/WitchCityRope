namespace WitchCityRope.Api.Features.EmailTemplates.Entities;

/// <summary>
/// Defines recipient groups for event-based email templates
/// Only used for Events category (NOT for Vetting/Admin/Incident)
/// </summary>
public enum EventRecipientGroup
{
    /// <summary>
    /// Users who actually attended (checked in to) the session
    /// Based on CheckIns table records
    /// </summary>
    SessionAttendees = 0,

    /// <summary>
    /// Users with RSVP (for socials) OR ticket purchases (for classes)
    /// Business logic: Event type determines which; deduplicate if user has both
    /// Based on TicketPurchases table
    /// </summary>
    RSVPTicketHolders = 1,

    /// <summary>
    /// Volunteers assigned to the specific session
    /// Based on VolunteerSignups table with SessionId match
    /// </summary>
    SessionVolunteers = 2,

    /// <summary>
    /// Teachers assigned to the session
    /// Based on Session entity teacher assignments
    /// </summary>
    Teachers = 3,

    /// <summary>
    /// Users with PendingAcceptance tickets or RSVPs for the session's event.
    /// These are users who were assigned a ticket or had a proxy RSVP created
    /// on their behalf but have not yet accepted (waiver + ToS).
    /// Used by TicketAcceptanceReminder and RsvpAcceptanceReminder templates.
    /// Based on EventAttendances where Status = PendingAcceptance.
    /// </summary>
    PendingAssignmentHolders = 4
}

namespace WitchCityRope.Core.Enums
{
    /// <summary>
    /// Status of an RSVP for a social event
    /// </summary>
    public enum RSVPStatus
    {
        /// <summary>
        /// RSVP is confirmed and the user has a guaranteed spot at the event
        /// </summary>
        Confirmed,

        /// <summary>
        /// Event is full, user is on the waitlist and will be confirmed if space becomes available
        /// </summary>
        Waitlisted,

        /// <summary>
        /// User has been checked in to the event
        /// </summary>
        CheckedIn,

        /// <summary>
        /// RSVP has been cancelled by the user or event organizers
        /// </summary>
        Cancelled
    }
}
namespace WitchCityRope.Core.Enums
{
    /// <summary>
    /// Status of a free RSVP for a social event
    /// </summary>
    public enum RSVPStatus
    {
        /// <summary>
        /// RSVP confirmed - no payment required
        /// </summary>
        Confirmed,

        /// <summary>
        /// RSVP cancelled by user or system
        /// </summary>
        Cancelled,

        /// <summary>
        /// User checked in at the event
        /// </summary>
        CheckedIn
    }
}
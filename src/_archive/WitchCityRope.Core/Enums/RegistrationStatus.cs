namespace WitchCityRope.Core.Enums
{
    /// <summary>
    /// Status of an event registration
    /// </summary>
    public enum RegistrationStatus
    {
        /// <summary>
        /// Registration created but payment not yet processed
        /// </summary>
        Pending,

        /// <summary>
        /// Payment received and registration confirmed
        /// </summary>
        Confirmed,

        /// <summary>
        /// Registration cancelled by user or system
        /// </summary>
        Cancelled,

        /// <summary>
        /// User checked in at the event
        /// </summary>
        CheckedIn,

        /// <summary>
        /// On waiting list due to capacity
        /// </summary>
        Waitlisted
    }
}
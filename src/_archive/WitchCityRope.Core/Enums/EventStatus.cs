namespace WitchCityRope.Core.Enums
{
    /// <summary>
    /// Status of an event
    /// </summary>
    public enum EventStatus
    {
        /// <summary>
        /// Event is in draft state, not yet published
        /// </summary>
        Draft,

        /// <summary>
        /// Event is published and open for registration
        /// </summary>
        Published,

        /// <summary>
        /// Event has been cancelled
        /// </summary>
        Cancelled,

        /// <summary>
        /// Event has been completed
        /// </summary>
        Completed
    }
}
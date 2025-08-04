namespace WitchCityRope.Core.Enums
{
    /// <summary>
    /// Status of an event ticket
    /// </summary>
    public enum TicketStatus
    {
        /// <summary>
        /// Ticket created but payment not yet processed
        /// </summary>
        Pending,

        /// <summary>
        /// Payment received and ticket confirmed
        /// </summary>
        Confirmed,

        /// <summary>
        /// Ticket cancelled by user or system
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
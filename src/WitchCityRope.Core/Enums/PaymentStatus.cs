namespace WitchCityRope.Core.Enums
{
    /// <summary>
    /// Status of a payment transaction
    /// </summary>
    public enum PaymentStatus
    {
        /// <summary>
        /// Payment initiated but not yet processed
        /// </summary>
        Pending,

        /// <summary>
        /// Payment successfully completed
        /// </summary>
        Completed,

        /// <summary>
        /// Payment failed or was declined
        /// </summary>
        Failed,

        /// <summary>
        /// Payment was refunded
        /// </summary>
        Refunded,

        /// <summary>
        /// Partial refund was issued
        /// </summary>
        PartiallyRefunded
    }
}
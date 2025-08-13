namespace WitchCityRope.Core.Enums
{
    /// <summary>
    /// Payment methods supported by the system
    /// </summary>
    public enum PaymentMethod
    {
        /// <summary>
        /// No payment method (for free events)
        /// </summary>
        None,

        /// <summary>
        /// Credit card payment
        /// </summary>
        CreditCard,

        /// <summary>
        /// Debit card payment
        /// </summary>
        DebitCard,

        /// <summary>
        /// PayPal payment
        /// </summary>
        PayPal,

        /// <summary>
        /// Venmo payment
        /// </summary>
        Venmo,

        /// <summary>
        /// Cash payment for in-person events
        /// </summary>
        Cash,

        /// <summary>
        /// Stripe payment processing
        /// </summary>
        Stripe
    }
}
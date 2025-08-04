namespace WitchCityRope.Core.Enums
{
    /// <summary>
    /// Pricing type for events
    /// </summary>
    public enum PricingType
    {
        /// <summary>
        /// Fixed price for all attendees
        /// </summary>
        Fixed,
        
        /// <summary>
        /// Sliding scale pricing with minimum, suggested, and maximum amounts
        /// </summary>
        SlidingScale,
        
        /// <summary>
        /// Free events with no charge
        /// </summary>
        Free
    }
}
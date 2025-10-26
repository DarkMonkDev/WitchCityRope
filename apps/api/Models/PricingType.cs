namespace WitchCityRope.Models;

/// <summary>
/// Pricing type for ticket types
/// </summary>
public enum PricingType
{
    /// <summary>
    /// Fixed price ticket - single set price
    /// </summary>
    Fixed,

    /// <summary>
    /// Sliding scale ticket - customer chooses price within min/max range
    /// </summary>
    SlidingScale
}

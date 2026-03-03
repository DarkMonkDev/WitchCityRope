namespace WitchCityRope.Api.Features.Payments;

/// <summary>
/// Constants for the payment system.
/// USD-only is a business decision: WitchCityRope operates exclusively in Salem, MA.
/// </summary>
public static class PaymentConstants
{
    /// <summary>
    /// The only supported currency. All payments are in US Dollars.
    /// </summary>
    public const string Currency = "USD";
}

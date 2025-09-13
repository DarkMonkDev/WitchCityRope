namespace WitchCityRope.Api.Features.Payments.Models;

/// <summary>
/// Payment method type enumeration
/// </summary>
public enum PaymentMethodType
{
    /// <summary>
    /// Using a previously saved payment method
    /// </summary>
    SavedCard = 0,
    
    /// <summary>
    /// Using a new credit/debit card
    /// </summary>
    NewCard = 1,
    
    /// <summary>
    /// Bank transfer (future enhancement)
    /// </summary>
    BankTransfer = 2,
    
    /// <summary>
    /// PayPal payment (future enhancement)
    /// </summary>
    PayPal = 3,
    
    /// <summary>
    /// Venmo payment (future enhancement)
    /// </summary>
    Venmo = 4
}
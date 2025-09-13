using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Payments.Entities;

/// <summary>
/// Saved payment method entity for user convenience and returning customers
/// PCI compliant - only stores display information and encrypted Stripe tokens
/// </summary>
public class PaymentMethod
{
    /// <summary>
    /// Payment method unique identifier
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Owner of the payment method
    /// </summary>
    public Guid UserId { get; set; }
    
    #region Encrypted Stripe Data (PCI Compliant)
    
    /// <summary>
    /// Encrypted Stripe Payment Method ID (never store raw card data)
    /// </summary>
    public string EncryptedStripePaymentMethodId { get; set; } = string.Empty;
    
    #endregion
    
    #region Display Information (Safe to Store)
    
    /// <summary>
    /// Last four digits of the card (safe for display)
    /// </summary>
    public string LastFourDigits { get; set; } = string.Empty;
    
    /// <summary>
    /// Card brand (Visa, MasterCard, etc.)
    /// </summary>
    public string CardBrand { get; set; } = string.Empty;
    
    /// <summary>
    /// Card expiry month
    /// </summary>
    public int ExpiryMonth { get; set; }
    
    /// <summary>
    /// Card expiry year
    /// </summary>
    public int ExpiryYear { get; set; }
    
    #endregion
    
    #region User Preferences
    
    /// <summary>
    /// Whether this is the user's default payment method
    /// </summary>
    public bool IsDefault { get; set; } = false;
    
    /// <summary>
    /// Whether this payment method is active (not deleted)
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    #endregion
    
    #region Timestamps
    
    /// <summary>
    /// When the payment method was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the payment method was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    #endregion
    
    #region Navigation Properties
    
    /// <summary>
    /// Navigation property to the user who owns this payment method
    /// </summary>
    public ApplicationUser? User { get; set; }
    
    #endregion
    
    #region Helper Methods
    
    /// <summary>
    /// Get display string for the payment method
    /// </summary>
    public string GetDisplayString()
    {
        return $"{CardBrand} ****{LastFourDigits} ({ExpiryMonth:00}/{ExpiryYear})";
    }
    
    /// <summary>
    /// Check if the payment method is expired
    /// </summary>
    public bool IsExpired()
    {
        var now = DateTime.UtcNow;
        var expiryDate = new DateTime(ExpiryYear, ExpiryMonth, DateTime.DaysInMonth(ExpiryYear, ExpiryMonth));
        return expiryDate < now;
    }
    
    /// <summary>
    /// Check if the payment method expires soon (within 3 months)
    /// </summary>
    public bool ExpiresSoon()
    {
        var now = DateTime.UtcNow;
        var threeMonthsFromNow = now.AddMonths(3);
        var expiryDate = new DateTime(ExpiryYear, ExpiryMonth, DateTime.DaysInMonth(ExpiryYear, ExpiryMonth));
        return expiryDate <= threeMonthsFromNow;
    }
    
    /// <summary>
    /// Soft delete the payment method
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        IsDefault = false; // Cannot be default if inactive
        UpdateTimestamp();
    }
    
    /// <summary>
    /// Update timestamps when entity is modified
    /// </summary>
    public void UpdateTimestamp()
    {
        UpdatedAt = DateTime.UtcNow;
    }
    
    #endregion
}
namespace WitchCityRope.Api.Features.Payments.Models.PayPal;

/// <summary>
/// PayPal capture response model
/// </summary>
public class PayPalCaptureResponse
{
    /// <summary>
    /// PayPal capture ID
    /// </summary>
    public string CaptureId { get; set; } = string.Empty;
    
    /// <summary>
    /// Capture status (COMPLETED, PENDING, DECLINED)
    /// </summary>
    public string Status { get; set; } = string.Empty;
    
    /// <summary>
    /// Amount captured
    /// </summary>
    public PayPalAmount Amount { get; set; } = new();
    
    /// <summary>
    /// Final capture flag
    /// </summary>
    public bool FinalCapture { get; set; }
    
    /// <summary>
    /// Capture creation timestamp
    /// </summary>
    public DateTime CreateTime { get; set; }
    
    /// <summary>
    /// Capture update timestamp
    /// </summary>
    public DateTime UpdateTime { get; set; }
    
    /// <summary>
    /// PayPal transaction ID
    /// </summary>
    public string? TransactionId { get; set; }
    
    /// <summary>
    /// Payer ID (customer identification)
    /// </summary>
    public string? PayerId { get; set; }
}

/// <summary>
/// PayPal amount object
/// </summary>
public class PayPalAmount
{
    /// <summary>
    /// Currency code (USD, EUR, etc.)
    /// </summary>
    public string CurrencyCode { get; set; } = "USD";
    
    /// <summary>
    /// Amount value as string
    /// </summary>
    public string Value { get; set; } = "0.00";
    
    /// <summary>
    /// Get decimal amount value
    /// </summary>
    public decimal GetDecimalValue()
    {
        return decimal.TryParse(Value, out var value) ? value : 0m;
    }
}
namespace WitchCityRope.Api.Features.Payments.Models.PayPal;

/// <summary>
/// PayPal order creation response model
/// </summary>
public class PayPalOrderResponse
{
    /// <summary>
    /// PayPal order ID
    /// </summary>
    public string OrderId { get; set; } = string.Empty;
    
    /// <summary>
    /// Order status (CREATED, APPROVED, COMPLETED, etc.)
    /// </summary>
    public string Status { get; set; } = string.Empty;
    
    /// <summary>
    /// Order creation timestamp
    /// </summary>
    public DateTime CreateTime { get; set; }
    
    /// <summary>
    /// Links for order processing (approval URL, capture URL, etc.)
    /// </summary>
    public List<PayPalLink> Links { get; set; } = new();
    
    /// <summary>
    /// Get the approval URL for redirect
    /// </summary>
    public string? GetApprovalUrl()
    {
        return Links.FirstOrDefault(l => l.Rel == "approve")?.Href;
    }
}

/// <summary>
/// PayPal link object
/// </summary>
public class PayPalLink
{
    /// <summary>
    /// Link URL
    /// </summary>
    public string Href { get; set; } = string.Empty;
    
    /// <summary>
    /// Link relationship (approve, capture, self)
    /// </summary>
    public string Rel { get; set; } = string.Empty;
    
    /// <summary>
    /// HTTP method for this link
    /// </summary>
    public string Method { get; set; } = "GET";
}
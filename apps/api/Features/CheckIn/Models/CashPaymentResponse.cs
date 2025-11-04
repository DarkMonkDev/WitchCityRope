namespace WitchCityRope.Api.Features.CheckIn.Models;

/// <summary>
/// Response model for successful cash payment recording.
/// Contains the created ticket purchase details.
/// </summary>
public class CashPaymentResponse
{
    /// <summary>
    /// ID of the created ticket purchase record
    /// </summary>
    public Guid TicketPurchaseId { get; set; }

    /// <summary>
    /// Whether the payment was successfully recorded
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Success or error message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Amount that was recorded
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// When the payment was recorded
    /// </summary>
    public DateTime RecordedAt { get; set; }
}

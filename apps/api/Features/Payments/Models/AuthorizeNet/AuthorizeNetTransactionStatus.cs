namespace WitchCityRope.Api.Features.Payments.Models.AuthorizeNet;

public class AuthorizeNetTransactionStatus
{
    public string TransactionId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal SettleAmount { get; set; }
    public string? CardNumber { get; set; }

    public bool IsSettled => Status.Equals("settledSuccessfully", StringComparison.OrdinalIgnoreCase)
                          || Status.Equals("refundSettledSuccessfully", StringComparison.OrdinalIgnoreCase);
}

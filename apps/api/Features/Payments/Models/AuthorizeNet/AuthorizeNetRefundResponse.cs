namespace WitchCityRope.Api.Features.Payments.Models.AuthorizeNet;

public class AuthorizeNetRefundResponse
{
    public bool Success { get; set; }
    public string? TransactionId { get; set; }
    public string? ResponseCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Message { get; set; }
    public bool WasVoided { get; set; }
}

namespace WitchCityRope.Api.Features.Payments.Models.AuthorizeNet;

public class AuthorizeNetPaymentResponse
{
    public bool Success { get; set; }
    public string? TransactionId { get; set; }
    public string? AuthCode { get; set; }
    public string? ResponseCode { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Message { get; set; }
    public string? AvsResultCode { get; set; }
    public string? CvvResultCode { get; set; }
}

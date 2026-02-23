namespace WitchCityRope.Api.Features.Payments.Models.AuthorizeNet;

public class AuthorizeNetOptions
{
    public string ApiLoginId { get; set; } = string.Empty;
    public string TransactionKey { get; set; } = string.Empty;
    public string ClientKey { get; set; } = string.Empty;
    public bool TestMode { get; set; } = true;
}

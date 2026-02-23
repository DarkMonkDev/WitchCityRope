using WitchCityRope.Api.Features.Payments.Models.AuthorizeNet;

namespace WitchCityRope.Api.Features.Payments.Services;

public interface IAuthorizeNetService
{
    /// <summary>
    /// Process a payment using an Accept.js nonce (opaque data).
    /// Card data never touches our server - Accept.js tokenizes it directly with Authorize.net.
    /// </summary>
    Task<AuthorizeNetPaymentResponse> ProcessPaymentWithNonceAsync(
        decimal amount,
        string nonce,
        string dataDescriptor,
        string description,
        string? invoiceId = null);

    /// <summary>
    /// Refund a transaction. Automatically handles void (unsettled) vs credit (settled).
    /// </summary>
    Task<AuthorizeNetRefundResponse> RefundAsync(
        string transactionId,
        decimal amount,
        string lastFourDigits,
        string? reason = null);

    /// <summary>
    /// Get transaction settlement status.
    /// </summary>
    Task<AuthorizeNetTransactionStatus?> GetTransactionStatusAsync(string transactionId);
}

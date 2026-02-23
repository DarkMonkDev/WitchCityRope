using AuthorizeNet.Api.Contracts.V1;
using AuthorizeNet.Api.Controllers;
using AuthorizeNet.Api.Controllers.Bases;
using WitchCityRope.Api.Features.Payments.Models.AuthorizeNet;
using Microsoft.Extensions.Options;

namespace WitchCityRope.Api.Features.Payments.Services;

/// <summary>
/// Authorize.NET payment processing service.
/// Uses Accept.js nonces (opaque data) so card data never touches our server.
/// Handles auth+capture, refunds (void vs credit based on settlement status).
/// </summary>
public class AuthorizeNetService : IAuthorizeNetService
{
    private readonly AuthorizeNetOptions _options;
    private readonly ILogger<AuthorizeNetService> _logger;

    public AuthorizeNetService(
        IOptions<AuthorizeNetOptions> options,
        ILogger<AuthorizeNetService> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (_options.TestMode)
        {
            ApiOperationBase<ANetApiRequest, ANetApiResponse>.RunEnvironment =
                AuthorizeNet.Environment.SANDBOX;
            _logger.LogInformation("AuthorizeNet configured for SANDBOX environment");
        }
        else
        {
            ApiOperationBase<ANetApiRequest, ANetApiResponse>.RunEnvironment =
                AuthorizeNet.Environment.PRODUCTION;
            _logger.LogInformation("AuthorizeNet configured for PRODUCTION environment");
        }
    }

    private merchantAuthenticationType GetMerchantAuthentication()
    {
        return new merchantAuthenticationType
        {
            name = _options.ApiLoginId,
            ItemElementName = ItemChoiceType.transactionKey,
            Item = _options.TransactionKey
        };
    }

    public Task<AuthorizeNetPaymentResponse> ProcessPaymentWithNonceAsync(
        decimal amount,
        string nonce,
        string dataDescriptor,
        string description,
        string? invoiceId = null)
    {
        _logger.LogInformation(
            "Processing Authorize.NET payment with Accept.js nonce for amount {Amount}, invoice: {InvoiceId}, " +
            "nonceLength={NonceLength}, descriptorLength={DescriptorLength}, descriptor={DataDescriptor}, " +
            "apiLoginId={ApiLoginIdPrefix}***, testMode={TestMode}",
            amount, invoiceId ?? "(none)", nonce?.Length ?? 0, dataDescriptor?.Length ?? 0, dataDescriptor,
            _options.ApiLoginId?[..Math.Min(4, _options.ApiLoginId?.Length ?? 0)] ?? "(null)", _options.TestMode);

        try
        {
            // Accept.js provides an opaque data token instead of raw card data
            var opaqueData = new opaqueDataType
            {
                dataDescriptor = dataDescriptor,
                dataValue = nonce
            };

            var paymentType = new paymentType { Item = opaqueData };

            var transactionRequest = new transactionRequestType
            {
                transactionType = transactionTypeEnum.authCaptureTransaction.ToString(),
                amount = amount,
                payment = paymentType
            };

            if (!string.IsNullOrEmpty(invoiceId) || !string.IsNullOrEmpty(description))
            {
                transactionRequest.order = new orderType
                {
                    invoiceNumber = invoiceId ?? "",
                    description = TruncateDescription(description)
                };
            }

            var request = new createTransactionRequest
            {
                merchantAuthentication = GetMerchantAuthentication(),
                transactionRequest = transactionRequest
            };

            var controller = new createTransactionController(request);
            controller.Execute();
            var response = controller.GetApiResponse();

            if (response == null)
            {
                // GetApiResponse() returns null when the HTTP call itself failed.
                // GetErrorResponse() contains the actual error details from the SDK.
                var errorResponse = controller.GetErrorResponse();
                if (errorResponse != null)
                {
                    var errCode = errorResponse.messages?.message?.FirstOrDefault()?.code ?? "unknown";
                    var errText = errorResponse.messages?.message?.FirstOrDefault()?.text ?? "unknown";
                    _logger.LogError(
                        "Authorize.NET returned null API response. ErrorResponse code={ErrorCode}, text={ErrorText}, resultCode={ResultCode}",
                        errCode, errText, errorResponse.messages?.resultCode);
                    return Task.FromResult(new AuthorizeNetPaymentResponse
                    {
                        Success = false,
                        ErrorCode = errCode,
                        ErrorMessage = $"Payment gateway error: {errText}"
                    });
                }

                _logger.LogError(
                    "Authorize.NET returned null response AND null error response for payment. " +
                    "This typically indicates a network connectivity issue between our server and the payment gateway.");
                return Task.FromResult(new AuthorizeNetPaymentResponse
                {
                    Success = false,
                    ErrorMessage = "Unable to reach payment gateway. Please try again in a moment."
                });
            }

            if (response.messages.resultCode == messageTypeEnum.Ok &&
                response.transactionResponse != null)
            {
                var txnResponse = response.transactionResponse;

                if (txnResponse.responseCode == "1")
                {
                    _logger.LogInformation(
                        "Authorize.NET payment APPROVED. TransactionId: {TransactionId}, AuthCode: {AuthCode}",
                        txnResponse.transId, txnResponse.authCode);

                    return Task.FromResult(new AuthorizeNetPaymentResponse
                    {
                        Success = true,
                        TransactionId = txnResponse.transId,
                        AuthCode = txnResponse.authCode,
                        ResponseCode = txnResponse.responseCode,
                        Message = "Transaction approved"
                    });
                }
                else
                {
                    var errorMessage = ExtractErrorMessage(txnResponse);

                    _logger.LogWarning(
                        "Authorize.NET payment DECLINED/ERROR. ResponseCode: {ResponseCode}, Message: {Message}",
                        txnResponse.responseCode, errorMessage);

                    return Task.FromResult(new AuthorizeNetPaymentResponse
                    {
                        Success = false,
                        ResponseCode = txnResponse.responseCode,
                        ErrorCode = txnResponse.errors?.FirstOrDefault()?.errorCode,
                        ErrorMessage = errorMessage
                    });
                }
            }
            else
            {
                var errorMessage = response.messages?.message?.FirstOrDefault()?.text
                    ?? "Unknown payment gateway error";
                var errorCode = response.messages?.message?.FirstOrDefault()?.code;

                _logger.LogError(
                    "Authorize.NET API error. Code: {ErrorCode}, Message: {Message}",
                    errorCode, errorMessage);

                return Task.FromResult(new AuthorizeNetPaymentResponse
                {
                    Success = false,
                    ErrorCode = errorCode,
                    ErrorMessage = errorMessage
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception processing Authorize.NET payment for amount {Amount}", amount);
            return Task.FromResult(new AuthorizeNetPaymentResponse
            {
                Success = false,
                ErrorMessage = $"Payment processing error: {ex.Message}"
            });
        }
    }

    public async Task<AuthorizeNetRefundResponse> RefundAsync(
        string transactionId,
        decimal amount,
        string lastFourDigits,
        string? reason = null)
    {
        _logger.LogInformation(
            "Processing Authorize.NET refund for transaction {TransactionId}, amount: {Amount}",
            transactionId, amount);

        try
        {
            var status = await GetTransactionStatusAsync(transactionId);
            if (status == null)
            {
                return new AuthorizeNetRefundResponse
                {
                    Success = false,
                    ErrorMessage = $"Could not retrieve transaction status for ID: {transactionId}."
                };
            }

            _logger.LogInformation(
                "Transaction {TransactionId} status: {Status}, SettleAmount: {SettleAmount}, IsSettled: {IsSettled}",
                transactionId, status.Status, status.SettleAmount, status.IsSettled);

            if (!status.IsSettled)
            {
                // Unsettled: must void (full amount only)
                if (amount < status.SettleAmount)
                {
                    return new AuthorizeNetRefundResponse
                    {
                        Success = false,
                        ErrorMessage = $"Cannot issue partial refund for unsettled transaction. " +
                                       $"The transaction amount is ${status.SettleAmount:F2}. " +
                                       "Either void the full amount now, or wait for settlement " +
                                       "(usually 24 hours) and then issue a partial refund."
                    };
                }

                return VoidTransaction(transactionId);
            }
            else
            {
                return CreditTransaction(transactionId, amount, lastFourDigits);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception processing Authorize.NET refund for {TransactionId}", transactionId);
            return new AuthorizeNetRefundResponse
            {
                Success = false,
                ErrorMessage = $"Refund processing error: {ex.Message}"
            };
        }
    }

    private AuthorizeNetRefundResponse VoidTransaction(string transactionId)
    {
        _logger.LogInformation("Voiding unsettled transaction {TransactionId}", transactionId);

        var transactionRequest = new transactionRequestType
        {
            transactionType = transactionTypeEnum.voidTransaction.ToString(),
            refTransId = transactionId
        };

        var request = new createTransactionRequest
        {
            merchantAuthentication = GetMerchantAuthentication(),
            transactionRequest = transactionRequest
        };

        var controller = new createTransactionController(request);
        controller.Execute();
        var response = controller.GetApiResponse();

        return ProcessRefundResponse(response, isVoid: true, transactionId);
    }

    private AuthorizeNetRefundResponse CreditTransaction(
        string transactionId,
        decimal amount,
        string lastFourDigits)
    {
        _logger.LogInformation(
            "Issuing credit for settled transaction {TransactionId}, amount: {Amount}",
            transactionId, amount);

        var creditCard = new creditCardType
        {
            cardNumber = lastFourDigits.PadLeft(4, '0'),
            expirationDate = "XXXX"
        };

        var paymentType = new paymentType { Item = creditCard };

        var transactionRequest = new transactionRequestType
        {
            transactionType = transactionTypeEnum.refundTransaction.ToString(),
            amount = amount,
            payment = paymentType,
            refTransId = transactionId
        };

        var request = new createTransactionRequest
        {
            merchantAuthentication = GetMerchantAuthentication(),
            transactionRequest = transactionRequest
        };

        var controller = new createTransactionController(request);
        controller.Execute();
        var response = controller.GetApiResponse();

        return ProcessRefundResponse(response, isVoid: false, transactionId);
    }

    private AuthorizeNetRefundResponse ProcessRefundResponse(
        createTransactionResponse? response,
        bool isVoid,
        string originalTransactionId)
    {
        var operation = isVoid ? "void" : "refund";

        if (response == null)
        {
            _logger.LogError(
                "Authorize.NET {Operation} returned null response for transaction {TransactionId}",
                operation, originalTransactionId);
            return new AuthorizeNetRefundResponse
            {
                Success = false,
                ErrorMessage = $"No response from payment gateway for {operation}. Please try again."
            };
        }

        if (response.messages.resultCode == messageTypeEnum.Ok &&
            response.transactionResponse != null)
        {
            var txnResponse = response.transactionResponse;

            if (txnResponse.responseCode == "1")
            {
                _logger.LogInformation(
                    "Authorize.NET {Operation} APPROVED for original transaction {OriginalId}. " +
                    "New TransactionId: {NewTransactionId}",
                    operation, originalTransactionId, txnResponse.transId);

                return new AuthorizeNetRefundResponse
                {
                    Success = true,
                    TransactionId = txnResponse.transId,
                    ResponseCode = txnResponse.responseCode,
                    WasVoided = isVoid,
                    Message = isVoid ? "Transaction voided successfully" : "Refund processed successfully"
                };
            }
            else
            {
                var errorMessage = ExtractErrorMessage(txnResponse);

                _logger.LogWarning(
                    "Authorize.NET {Operation} DECLINED/ERROR for transaction {TransactionId}. " +
                    "ResponseCode: {ResponseCode}, Message: {Message}",
                    operation, originalTransactionId, txnResponse.responseCode, errorMessage);

                return new AuthorizeNetRefundResponse
                {
                    Success = false,
                    ResponseCode = txnResponse.responseCode,
                    ErrorMessage = errorMessage
                };
            }
        }
        else
        {
            var errorMessage = response.messages?.message?.FirstOrDefault()?.text
                ?? $"Unknown {operation} error";

            _logger.LogError(
                "Authorize.NET {Operation} API error for transaction {TransactionId}: {Message}",
                operation, originalTransactionId, errorMessage);

            return new AuthorizeNetRefundResponse
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }

    public Task<AuthorizeNetTransactionStatus?> GetTransactionStatusAsync(string transactionId)
    {
        _logger.LogDebug("Getting transaction status for {TransactionId}", transactionId);

        try
        {
            var request = new getTransactionDetailsRequest
            {
                merchantAuthentication = GetMerchantAuthentication(),
                transId = transactionId
            };

            var controller = new getTransactionDetailsController(request);
            controller.Execute();
            var response = controller.GetApiResponse();

            if (response?.messages?.resultCode == messageTypeEnum.Ok &&
                response.transaction != null)
            {
                var txn = response.transaction;

                string? last4 = null;
                if (txn.payment?.Item is creditCardMaskedType maskedCard)
                {
                    last4 = maskedCard.cardNumber;
                }

                return Task.FromResult<AuthorizeNetTransactionStatus?>(new AuthorizeNetTransactionStatus
                {
                    TransactionId = txn.transId,
                    Status = txn.transactionStatus ?? "",
                    SettleAmount = txn.settleAmount,
                    CardNumber = last4
                });
            }

            var errorMsg = response?.messages?.message?.FirstOrDefault()?.text ?? "Unknown error";
            _logger.LogWarning(
                "Could not get transaction status for {TransactionId}: {Message}",
                transactionId, errorMsg);

            return Task.FromResult<AuthorizeNetTransactionStatus?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception getting transaction status for {TransactionId}", transactionId);
            return Task.FromResult<AuthorizeNetTransactionStatus?>(null);
        }
    }

    private static string ExtractErrorMessage(transactionResponse txnResponse)
    {
        var errorText = txnResponse.errors?.FirstOrDefault()?.errorText;
        if (!string.IsNullOrEmpty(errorText))
            return errorText;

        var messageText = txnResponse.messages?.FirstOrDefault()?.description;
        if (!string.IsNullOrEmpty(messageText))
            return messageText;

        return txnResponse.responseCode switch
        {
            "2" => "Transaction declined by issuing bank",
            "3" => "Transaction error - please try again",
            "4" => "Transaction held for review",
            _ => "Transaction failed - please try again"
        };
    }

    private static string TruncateDescription(string? description)
    {
        if (string.IsNullOrEmpty(description))
            return "";
        return description.Length > 255 ? description[..255] : description;
    }
}

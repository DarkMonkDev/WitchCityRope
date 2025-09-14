using System;
using System.Threading.Tasks;

namespace WitchCityRope.Api.Features.Payments;

/// <summary>
/// Command to process payments for events, memberships, or merchandise
/// Integrates with payment providers like Stripe
/// </summary>
public record ProcessPaymentCommand(
    Guid UserId,
    PaymentType Type,
    decimal Amount,
    string Currency,
    string Description,
    PaymentMethod Method,
    string? PaymentMethodId, // Stripe payment method ID
    string? CardToken, // For new cards
    bool SavePaymentMethod,
    PaymentMetadata Metadata
) : IRequest<ProcessPaymentResult>;

public enum PaymentType
{
    EventRegistration,
    MembershipFee,
    Merchandise,
    Donation,
    Other
}

public enum PaymentMethod
{
    SavedCard,
    NewCard,
    BankTransfer,
    PayPal,
    Venmo
}

public record PaymentMetadata(
    Guid? EventId,
    Guid? RegistrationId,
    Guid? OrderId,
    string? ItemDescription,
    Dictionary<string, string> CustomFields
);

public record ProcessPaymentResult(
    Guid PaymentId,
    string TransactionId,
    PaymentStatus Status,
    decimal AmountCharged,
    decimal ProcessingFee,
    DateTime ProcessedAt,
    string? ReceiptUrl,
    string? ErrorMessage
);

public enum PaymentStatus
{
    Succeeded,
    Processing,
    RequiresAction,
    Failed,
    Cancelled
}

/// <summary>
/// Handles payment processing
/// Integrates with payment providers and maintains transaction records
/// </summary>
public class ProcessPaymentCommandHandler
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IUserRepository _userRepository;
    private readonly IStripeService _stripeService;
    private readonly IPaymentValidationService _validationService;
    private readonly INotificationService _notificationService;

    public ProcessPaymentCommandHandler(
        IPaymentRepository paymentRepository,
        IUserRepository userRepository,
        IStripeService stripeService,
        IPaymentValidationService validationService,
        INotificationService notificationService)
    {
        _paymentRepository = paymentRepository;
        _userRepository = userRepository;
        _stripeService = stripeService;
        _validationService = validationService;
        _notificationService = notificationService;
    }

    public async Task<ProcessPaymentResult> Execute(ProcessPaymentCommand command)
    {
        // Validate amount
        if (command.Amount <= 0)
        {
            throw new ValidationException("Payment amount must be greater than zero");
        }

        // Get user
        var user = await _userRepository.GetByIdAsync(command.UserId);
        if (user == null)
        {
            throw new NotFoundException("User not found");
        }

        // Validate payment method
        string? stripePaymentMethodId = null;
        
        if (command.Method == PaymentMethod.SavedCard)
        {
            if (string.IsNullOrEmpty(command.PaymentMethodId))
            {
                throw new ValidationException("Payment method ID is required for saved cards");
            }
            stripePaymentMethodId = command.PaymentMethodId;
        }
        else if (command.Method == PaymentMethod.NewCard)
        {
            if (string.IsNullOrEmpty(command.CardToken))
            {
                throw new ValidationException("Card token is required for new cards");
            }
            
            // Create payment method from token
            stripePaymentMethodId = await _stripeService.CreatePaymentMethodAsync(command.CardToken);
        }
        else
        {
            throw new ValidationException($"Payment method {command.Method} is not currently supported");
        }

        // Ensure user has a Stripe customer ID
        if (string.IsNullOrEmpty(user.StripeCustomerId))
        {
            user.StripeCustomerId = await _stripeService.CreateCustomerAsync(
                user.Email,
                user.DisplayName
            );
            await _userRepository.UpdateStripeCustomerIdAsync(user.Id, user.StripeCustomerId);
        }

        // Attach payment method to customer if saving
        if (command.SavePaymentMethod && command.Method == PaymentMethod.NewCard)
        {
            await _stripeService.AttachPaymentMethodToCustomerAsync(
                stripePaymentMethodId!,
                user.StripeCustomerId
            );
        }

        // Validate payment based on type
        var validationResult = await _validationService.ValidatePaymentAsync(command);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.ErrorMessage ?? "Payment validation failed");
        }

        // Create payment record
        var paymentId = Guid.NewGuid();
        var payment = new Payment
        {
            Id = paymentId,
            UserId = command.UserId,
            Type = command.Type,
            Amount = command.Amount,
            Currency = command.Currency,
            Description = command.Description,
            Method = command.Method,
            Status = PaymentStatus.Processing,
            Metadata = command.Metadata,
            CreatedAt = DateTime.UtcNow
        };

        await _paymentRepository.CreateAsync(payment);

        try
        {
            // Process payment through Stripe
            var chargeResult = await _stripeService.CreateChargeAsync(new StripeChargeRequest
            {
                Amount = command.Amount,
                Currency = command.Currency,
                Description = command.Description,
                CustomerId = user.StripeCustomerId,
                PaymentMethodId = stripePaymentMethodId!,
                Metadata = ConvertMetadataForStripe(command.Metadata),
                IdempotencyKey = paymentId.ToString() // Prevent duplicate charges
            });

            // Update payment record with results
            payment.TransactionId = chargeResult.ChargeId;
            payment.Status = ConvertStripeStatus(chargeResult.Status);
            payment.ProcessingFee = chargeResult.ProcessingFee;
            payment.ProcessedAt = DateTime.UtcNow;
            payment.ReceiptUrl = chargeResult.ReceiptUrl;

            if (payment.Status == PaymentStatus.Failed)
            {
                payment.ErrorMessage = chargeResult.ErrorMessage;
            }

            await _paymentRepository.UpdateAsync(payment);

            // Send receipt email if successful
            if (payment.Status == PaymentStatus.Succeeded)
            {
                await _notificationService.SendPaymentReceiptAsync(
                    user.Email,
                    user.DisplayName,
                    payment.Amount,
                    payment.Description,
                    payment.ReceiptUrl!,
                    payment.TransactionId!
                );

                // Handle post-payment actions based on type
                await HandlePostPaymentActionsAsync(payment, command.Metadata);
            }

            return new ProcessPaymentResult(
                PaymentId: paymentId,
                TransactionId: payment.TransactionId ?? string.Empty,
                Status: payment.Status,
                AmountCharged: payment.Amount,
                ProcessingFee: payment.ProcessingFee ?? 0,
                ProcessedAt: payment.ProcessedAt ?? DateTime.UtcNow,
                ReceiptUrl: payment.ReceiptUrl,
                ErrorMessage: payment.ErrorMessage
            );
        }
        catch (Exception ex)
        {
            // Update payment record with failure
            payment.Status = PaymentStatus.Failed;
            payment.ErrorMessage = ex.Message;
            payment.ProcessedAt = DateTime.UtcNow;
            await _paymentRepository.UpdateAsync(payment);

            throw new PaymentException($"Payment processing failed: {ex.Message}");
        }
    }

    private Dictionary<string, string> ConvertMetadataForStripe(PaymentMetadata metadata)
    {
        var stripeMetadata = new Dictionary<string, string>();

        if (metadata.EventId.HasValue)
            stripeMetadata["event_id"] = metadata.EventId.Value.ToString();
        
        if (metadata.RegistrationId.HasValue)
            stripeMetadata["registration_id"] = metadata.RegistrationId.Value.ToString();
        
        if (metadata.OrderId.HasValue)
            stripeMetadata["order_id"] = metadata.OrderId.Value.ToString();
        
        if (!string.IsNullOrEmpty(metadata.ItemDescription))
            stripeMetadata["item_description"] = metadata.ItemDescription;

        foreach (var field in metadata.CustomFields)
        {
            stripeMetadata[$"custom_{field.Key}"] = field.Value;
        }

        return stripeMetadata;
    }

    private PaymentStatus ConvertStripeStatus(string stripeStatus)
    {
        return stripeStatus.ToLower() switch
        {
            "succeeded" => PaymentStatus.Succeeded,
            "processing" => PaymentStatus.Processing,
            "requires_action" => PaymentStatus.RequiresAction,
            "cancelled" => PaymentStatus.Cancelled,
            _ => PaymentStatus.Failed
        };
    }

    private async Task HandlePostPaymentActionsAsync(Payment payment, PaymentMetadata metadata)
    {
        switch (payment.Type)
        {
            case PaymentType.EventRegistration:
                if (metadata.RegistrationId.HasValue)
                {
                    // Update registration status to paid
                    await _paymentRepository.MarkRegistrationAsPaidAsync(metadata.RegistrationId.Value);
                }
                break;

            case PaymentType.MembershipFee:
                // Update membership expiration date
                await _paymentRepository.ExtendMembershipAsync(payment.UserId);
                break;

            case PaymentType.Merchandise:
                if (metadata.OrderId.HasValue)
                {
                    // Update order status
                    await _paymentRepository.MarkOrderAsPaidAsync(metadata.OrderId.Value);
                }
                break;
        }
    }
}

// Marker interface for future MediatR implementation
public interface IRequest<TResponse> { }

// Custom exceptions
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

public class PaymentException : Exception
{
    public PaymentException(string message) : base(message) { }
}

// Domain models
public class Payment
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public PaymentType Type { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Description { get; set; } = string.Empty;
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; }
    public string? TransactionId { get; set; }
    public decimal? ProcessingFee { get; set; }
    public string? ReceiptUrl { get; set; }
    public string? ErrorMessage { get; set; }
    public PaymentMetadata? Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? StripeCustomerId { get; set; }
}

// Service models
public class StripeChargeRequest
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Description { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string PaymentMethodId { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new();
    public string? IdempotencyKey { get; set; }
}

public class StripeChargeResult
{
    public string ChargeId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal ProcessingFee { get; set; }
    public string? ReceiptUrl { get; set; }
    public string? ErrorMessage { get; set; }
}

public class PaymentValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
}

// Repository and service interfaces
public interface IPaymentRepository
{
    Task CreateAsync(Payment payment);
    Task UpdateAsync(Payment payment);
    Task MarkRegistrationAsPaidAsync(Guid registrationId);
    Task ExtendMembershipAsync(Guid userId);
    Task MarkOrderAsPaidAsync(Guid orderId);
}

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid userId);
    Task UpdateStripeCustomerIdAsync(Guid userId, string stripeCustomerId);
}

public interface IStripeService
{
    Task<string> CreateCustomerAsync(string email, string name);
    Task<string> CreatePaymentMethodAsync(string cardToken);
    Task AttachPaymentMethodToCustomerAsync(string paymentMethodId, string customerId);
    Task<StripeChargeResult> CreateChargeAsync(StripeChargeRequest request);
}

public interface IPaymentValidationService
{
    Task<PaymentValidationResult> ValidatePaymentAsync(ProcessPaymentCommand command);
}

public interface INotificationService
{
    Task SendPaymentReceiptAsync(
        string email, 
        string name, 
        decimal amount, 
        string description, 
        string receiptUrl, 
        string transactionId);
}
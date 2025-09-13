using FluentValidation;
using WitchCityRope.Api.Features.Payments.Models.Requests;
using WitchCityRope.Api.Features.Payments.Models;

namespace WitchCityRope.Api.Features.Payments.Validators;

/// <summary>
/// Validator for ProcessPaymentApiRequest with comprehensive business rules
/// </summary>
public class ProcessPaymentApiRequestValidator : AbstractValidator<ProcessPaymentApiRequest>
{
    public ProcessPaymentApiRequestValidator()
    {
        RuleFor(x => x.EventRegistrationId)
            .NotEmpty()
            .WithMessage("Event registration ID is required");

        RuleFor(x => x.OriginalAmount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Original amount must be non-negative")
            .LessThanOrEqualTo(10000)
            .WithMessage("Original amount cannot exceed $10,000");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithMessage("Currency is required")
            .Must(BeValidCurrency)
            .WithMessage("Currency must be one of: USD, EUR, GBP, CAD")
            .Length(3)
            .WithMessage("Currency code must be exactly 3 characters");

        RuleFor(x => x.SlidingScalePercentage)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Sliding scale percentage cannot be negative")
            .LessThanOrEqualTo(75)
            .WithMessage("Sliding scale percentage cannot exceed 75% (community guideline)")
            .ScalePrecision(2, 5)
            .WithMessage("Sliding scale percentage can have at most 2 decimal places");

        RuleFor(x => x.PaymentMethodType)
            .IsInEnum()
            .WithMessage("Invalid payment method type");

        // Conditional validation based on payment method type
        RuleFor(x => x.SavedPaymentMethodId)
            .NotEmpty()
            .When(x => x.PaymentMethodType == PaymentMethodType.SavedCard)
            .WithMessage("Saved payment method ID is required when using saved card");

        RuleFor(x => x.StripePaymentMethodId)
            .NotEmpty()
            .When(x => x.PaymentMethodType == PaymentMethodType.NewCard)
            .WithMessage("Stripe payment method ID is required when using new card");

        // Ensure only one payment method ID is provided
        RuleFor(x => x)
            .Must(HaveOnlyOnePaymentMethodId)
            .WithMessage("Provide either saved payment method ID or Stripe payment method ID, not both")
            .WithName("PaymentMethodIds");
    }

    private static bool BeValidCurrency(string currency)
    {
        var validCurrencies = new[] { "USD", "EUR", "GBP", "CAD" };
        return validCurrencies.Contains(currency?.ToUpperInvariant());
    }

    private static bool HaveOnlyOnePaymentMethodId(ProcessPaymentApiRequest request)
    {
        var hasSavedId = !string.IsNullOrEmpty(request.SavedPaymentMethodId);
        var hasStripeId = !string.IsNullOrEmpty(request.StripePaymentMethodId);

        return request.PaymentMethodType switch
        {
            PaymentMethodType.SavedCard => hasSavedId && !hasStripeId,
            PaymentMethodType.NewCard => hasStripeId && !hasSavedId,
            _ => !hasSavedId && !hasStripeId // For future payment types
        };
    }
}

/// <summary>
/// Validator for ProcessRefundApiRequest with business rule validation
/// </summary>
public class ProcessRefundApiRequestValidator : AbstractValidator<ProcessRefundApiRequest>
{
    public ProcessRefundApiRequestValidator()
    {
        RuleFor(x => x.PaymentId)
            .NotEmpty()
            .WithMessage("Payment ID is required");

        RuleFor(x => x.RefundAmount)
            .GreaterThan(0)
            .WithMessage("Refund amount must be greater than zero")
            .LessThanOrEqualTo(10000)
            .WithMessage("Refund amount cannot exceed $10,000")
            .ScalePrecision(2, 10)
            .WithMessage("Refund amount can have at most 2 decimal places");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithMessage("Currency is required")
            .Must(BeValidCurrency)
            .WithMessage("Currency must be one of: USD, EUR, GBP, CAD")
            .Length(3)
            .WithMessage("Currency code must be exactly 3 characters");

        RuleFor(x => x.RefundReason)
            .NotEmpty()
            .WithMessage("Refund reason is required")
            .MinimumLength(10)
            .WithMessage("Refund reason must be at least 10 characters long")
            .MaximumLength(1000)
            .WithMessage("Refund reason cannot exceed 1000 characters");
    }

    private static bool BeValidCurrency(string currency)
    {
        var validCurrencies = new[] { "USD", "EUR", "GBP", "CAD" };
        return validCurrencies.Contains(currency?.ToUpperInvariant());
    }
}
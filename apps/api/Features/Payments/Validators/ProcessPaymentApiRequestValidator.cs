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
            .PrecisionScale(5, 2, true)
            .WithMessage("Sliding scale percentage can have at most 2 decimal places");

        RuleFor(x => x.PaymentMethodType)
            .IsInEnum()
            .WithMessage("Invalid payment method type");

        // PayPal URL validation
        RuleFor(x => x.ReturnUrl)
            .Must(BeValidUrl)
            .When(x => !string.IsNullOrEmpty(x.ReturnUrl))
            .WithMessage("Return URL must be a valid URL");

        RuleFor(x => x.CancelUrl)
            .Must(BeValidUrl)
            .When(x => !string.IsNullOrEmpty(x.CancelUrl))
            .WithMessage("Cancel URL must be a valid URL");
    }

    private static bool BeValidCurrency(string currency)
    {
        var validCurrencies = new[] { "USD", "EUR", "GBP", "CAD" };
        return validCurrencies.Contains(currency?.ToUpperInvariant());
    }

    private static bool BeValidUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return true; // Optional URLs are valid when empty

        return Uri.TryCreate(url, UriKind.Absolute, out var validUri)
               && (validUri.Scheme == Uri.UriSchemeHttp || validUri.Scheme == Uri.UriSchemeHttps);
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
            .PrecisionScale(10, 2, true)
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
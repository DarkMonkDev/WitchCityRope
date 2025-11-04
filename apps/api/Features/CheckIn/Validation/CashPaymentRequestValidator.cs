using FluentValidation;
using WitchCityRope.Api.Features.CheckIn.Models;

namespace WitchCityRope.Api.Features.CheckIn.Validation;

/// <summary>
/// Validator for cash payment requests
/// Ensures all required fields are present and valid
/// </summary>
public class CashPaymentRequestValidator : AbstractValidator<CashPaymentRequest>
{
    public CashPaymentRequestValidator()
    {
        RuleFor(x => x.AttendeeId)
            .NotEmpty()
            .WithMessage("Attendee ID is required");

        RuleFor(x => x.TicketTypeId)
            .NotEmpty()
            .WithMessage("Ticket type ID is required");

        RuleFor(x => x.Amount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Amount must be greater than or equal to $0.00")
            .LessThanOrEqualTo(10000)
            .WithMessage("Amount cannot exceed $10,000.00");

        RuleFor(x => x.RecordedByStaffId)
            .NotEmpty()
            .WithMessage("Staff ID is required");

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.Notes))
            .WithMessage("Notes cannot exceed 500 characters");
    }
}

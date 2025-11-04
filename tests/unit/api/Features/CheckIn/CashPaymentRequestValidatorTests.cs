using FluentValidation.TestHelper;
using WitchCityRope.Api.Features.CheckIn.Models;
using WitchCityRope.Api.Features.CheckIn.Validation;
using Xunit;

namespace WitchCityRope.UnitTests.Api.Features.CheckIn;

/// <summary>
/// Unit tests for CashPaymentRequestValidator
/// Tests validation rules for door cash payment requests
/// Part of Streamlined Check-In Workflow feature
/// Created: 2025-11-04
/// </summary>
public class CashPaymentRequestValidatorTests
{
    private readonly CashPaymentRequestValidator _validator;

    public CashPaymentRequestValidatorTests()
    {
        _validator = new CashPaymentRequestValidator();
    }

    [Fact]
    public void ValidRequest_PassesValidation()
    {
        // Arrange
        var request = new CashPaymentRequest
        {
            AttendeeId = Guid.NewGuid(),
            TicketTypeId = Guid.NewGuid(),
            Amount = 20.00m,
            RecordedByStaffId = Guid.NewGuid(),
            Notes = "Cash payment at door"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void NullAttendeeId_FailsValidation()
    {
        // Arrange
        var request = new CashPaymentRequest
        {
            AttendeeId = Guid.Empty, // Empty GUID
            TicketTypeId = Guid.NewGuid(),
            Amount = 20.00m,
            RecordedByStaffId = Guid.NewGuid()
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AttendeeId)
            .WithErrorMessage("Attendee ID is required");
    }

    [Fact]
    public void NullTicketTypeId_FailsValidation()
    {
        // Arrange
        var request = new CashPaymentRequest
        {
            AttendeeId = Guid.NewGuid(),
            TicketTypeId = Guid.Empty, // Empty GUID
            Amount = 20.00m,
            RecordedByStaffId = Guid.NewGuid()
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TicketTypeId)
            .WithErrorMessage("Ticket type ID is required");
    }

    [Fact]
    public void NegativeAmount_FailsValidation()
    {
        // Arrange
        var request = new CashPaymentRequest
        {
            AttendeeId = Guid.NewGuid(),
            TicketTypeId = Guid.NewGuid(),
            Amount = -10.00m, // Negative amount
            RecordedByStaffId = Guid.NewGuid()
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Amount)
            .WithErrorMessage("Amount must be greater than or equal to $0.00");
    }

    [Fact]
    public void AmountExceeds10000_FailsValidation()
    {
        // Arrange
        var request = new CashPaymentRequest
        {
            AttendeeId = Guid.NewGuid(),
            TicketTypeId = Guid.NewGuid(),
            Amount = 10001.00m, // Exceeds maximum
            RecordedByStaffId = Guid.NewGuid()
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Amount)
            .WithErrorMessage("Amount cannot exceed $10,000.00");
    }

    [Fact]
    public void NotesExceed500Chars_FailsValidation()
    {
        // Arrange
        var request = new CashPaymentRequest
        {
            AttendeeId = Guid.NewGuid(),
            TicketTypeId = Guid.NewGuid(),
            Amount = 20.00m,
            RecordedByStaffId = Guid.NewGuid(),
            Notes = new string('a', 501) // 501 characters
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Notes)
            .WithErrorMessage("Notes cannot exceed 500 characters");
    }

    [Fact]
    public void ZeroDollarAmount_PassesValidation()
    {
        // Arrange - Per spec: Allows $0.00 for free tickets
        var request = new CashPaymentRequest
        {
            AttendeeId = Guid.NewGuid(),
            TicketTypeId = Guid.NewGuid(),
            Amount = 0.00m, // Zero is valid
            RecordedByStaffId = Guid.NewGuid(),
            Notes = "Free ticket - sliding scale"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ValidRequestWithNotes_PassesValidation()
    {
        // Arrange
        var request = new CashPaymentRequest
        {
            AttendeeId = Guid.NewGuid(),
            TicketTypeId = Guid.NewGuid(),
            Amount = 15.00m,
            RecordedByStaffId = Guid.NewGuid(),
            Notes = "Sliding scale - minimum price applied"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ValidRequestWithoutNotes_PassesValidation()
    {
        // Arrange
        var request = new CashPaymentRequest
        {
            AttendeeId = Guid.NewGuid(),
            TicketTypeId = Guid.NewGuid(),
            Amount = 20.00m,
            RecordedByStaffId = Guid.NewGuid(),
            Notes = null // Notes are optional
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyRecordedByStaffId_FailsValidation()
    {
        // Arrange
        var request = new CashPaymentRequest
        {
            AttendeeId = Guid.NewGuid(),
            TicketTypeId = Guid.NewGuid(),
            Amount = 20.00m,
            RecordedByStaffId = Guid.Empty // Empty GUID
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RecordedByStaffId)
            .WithErrorMessage("Staff ID is required");
    }

    [Theory]
    [InlineData(0.00)]
    [InlineData(0.01)]
    [InlineData(10.50)]
    [InlineData(100.00)]
    [InlineData(999.99)]
    [InlineData(10000.00)]
    public void ValidAmountRange_PassesValidation(decimal amount)
    {
        // Arrange
        var request = new CashPaymentRequest
        {
            AttendeeId = Guid.NewGuid(),
            TicketTypeId = Guid.NewGuid(),
            Amount = amount,
            RecordedByStaffId = Guid.NewGuid()
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void NotesExactly500Chars_PassesValidation()
    {
        // Arrange - Boundary test: exactly 500 characters should pass
        var request = new CashPaymentRequest
        {
            AttendeeId = Guid.NewGuid(),
            TicketTypeId = Guid.NewGuid(),
            Amount = 20.00m,
            RecordedByStaffId = Guid.NewGuid(),
            Notes = new string('a', 500) // Exactly 500 characters
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Notes);
    }
}

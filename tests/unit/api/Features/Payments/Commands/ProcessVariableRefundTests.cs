using Xunit;
using FluentAssertions;
using NSubstitute;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Payments.Commands;
using WitchCityRope.Api.Features.Payments.Models.Requests;
using WitchCityRope.Api.Features.Payments.Services;
using WitchCityRope.Api.Features.Payments.Entities;
using WitchCityRope.Api.Features.Payments.ValueObjects;
using WitchCityRope.Api.Features.Payments.Models;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Tests.Fixtures;
using Microsoft.AspNetCore.Http.HttpResults;

namespace WitchCityRope.Api.Tests.Features.Payments.Commands;

/// <summary>
/// Unit tests for ProcessVariableRefund command
/// Tests authentication, authorization, validation, and business logic
/// CRITICAL: Variable refunds do NOT cancel RSVP/ticket (key differentiator)
/// </summary>
[Collection("Database")]
public class ProcessVariableRefundTests : IAsyncLifetime
{
    private readonly DatabaseTestFixture _fixture;
    private readonly IRefundService _mockRefundService;
    private readonly ILogger<ProcessVariableRefund> _mockLogger;
    private ApplicationDbContext _context = null!;
    private Guid _adminUserId;
    private Guid _teacherUserId;
    private Guid _memberUserId;
    private ClaimsPrincipal _adminUser = null!;
    private ClaimsPrincipal _teacherUser = null!;
    private ClaimsPrincipal _memberUser = null!;

    public ProcessVariableRefundTests(DatabaseTestFixture fixture)
    {
        _fixture = fixture;
        _mockRefundService = Substitute.For<IRefundService>();
        _mockLogger = Substitute.For<ILogger<ProcessVariableRefund>>();
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
        _context = _fixture.CreateDbContext();

        // Create test users
        _adminUserId = Guid.NewGuid();
        _teacherUserId = Guid.NewGuid();
        _memberUserId = Guid.NewGuid();

        var adminAppUser = new ApplicationUser
        {
            Id = _adminUserId,
            Email = $"admin-{Guid.NewGuid():N}@example.com",
            SceneName = "AdminUser",
            DateOfBirth = DateTime.UtcNow.AddYears(-30),
            Role = "Administrator",
            EmailVerificationToken = Guid.NewGuid().ToString()
        };

        var teacherAppUser = new ApplicationUser
        {
            Id = _teacherUserId,
            Email = $"teacher-{Guid.NewGuid():N}@example.com",
            SceneName = "TeacherUser",
            DateOfBirth = DateTime.UtcNow.AddYears(-28),
            Role = "Teacher",
            EmailVerificationToken = Guid.NewGuid().ToString()
        };

        var memberAppUser = new ApplicationUser
        {
            Id = _memberUserId,
            Email = $"member-{Guid.NewGuid():N}@example.com",
            SceneName = "MemberUser",
            DateOfBirth = DateTime.UtcNow.AddYears(-25),
            Role = "Member",
            EmailVerificationToken = Guid.NewGuid().ToString()
        };

        _context.Users.AddRange(adminAppUser, teacherAppUser, memberAppUser);
        await _context.SaveChangesAsync();

        // Create ClaimsPrincipals
        _adminUser = CreateClaimsPrincipal(_adminUserId.ToString(), "admin@example.com", "Administrator");
        _teacherUser = CreateClaimsPrincipal(_teacherUserId.ToString(), "teacher@example.com", "Teacher");
        _memberUser = CreateClaimsPrincipal(_memberUserId.ToString(), "member@example.com", "Member");
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    #region Authentication & Authorization Tests

    [Fact]
    public async Task Execute_WithMissingUserId_Returns401()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var request = CreateValidRequest();
        var user = new ClaimsPrincipal(); // No claims

        // Act
        var result = await ProcessVariableRefund.Execute(
            transactionId, request, _context, _mockRefundService, user, _mockLogger);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(401);
        problemResult.ProblemDetails.Title.Should().Be("Unauthorized");
    }

    [Fact]
    public async Task Execute_WithMemberRole_Returns403()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var request = CreateValidRequest();

        // Act
        var result = await ProcessVariableRefund.Execute(
            transactionId, request, _context, _mockRefundService, _memberUser, _mockLogger);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(403);
        problemResult.ProblemDetails.Title.Should().Be("Forbidden");
        problemResult.ProblemDetails.Detail.Should().Contain("Only admins and teachers");
    }

    [Fact]
    public async Task Execute_WithAdminRole_AllowsRefund()
    {
        // Arrange
        var ticketPurchase = await CreateCompletedTicketPurchase(100.00m);
        var request = CreateValidRequest(50.00m);

        SetupSuccessfulRefund(ticketPurchase.Id, 50.00m);

        // Act
        var result = await ProcessVariableRefund.Execute(
            ticketPurchase.Id, request, _context, _mockRefundService, _adminUser, _mockLogger);

        // Assert
        result.Should().BeOfType<Ok<ProcessVariableRefund.VariableRefundResponse>>();
    }

    [Fact]
    public async Task Execute_WithTeacherRole_AllowsRefund()
    {
        // Arrange
        var ticketPurchase = await CreateCompletedTicketPurchase(100.00m);
        var request = CreateValidRequest(50.00m);

        SetupSuccessfulRefund(ticketPurchase.Id, 50.00m);

        // Act
        var result = await ProcessVariableRefund.Execute(
            ticketPurchase.Id, request, _context, _mockRefundService, _teacherUser, _mockLogger);

        // Assert
        result.Should().BeOfType<Ok<ProcessVariableRefund.VariableRefundResponse>>();
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task Execute_WithZeroRefundAmount_Returns400()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var request = CreateValidRequest(0.00m);

        // Act
        var result = await ProcessVariableRefund.Execute(
            transactionId, request, _context, _mockRefundService, _adminUser, _mockLogger);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(400);
        problemResult.ProblemDetails.Detail.Should().Contain("must be greater than 0");
    }

    [Fact]
    public async Task Execute_WithNegativeRefundAmount_Returns400()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var request = CreateValidRequest(-10.00m);

        // Act
        var result = await ProcessVariableRefund.Execute(
            transactionId, request, _context, _mockRefundService, _adminUser, _mockLogger);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(400);
        problemResult.ProblemDetails.Detail.Should().Contain("must be greater than 0");
    }

    [Fact]
    public async Task Execute_WithEmptyRefundReason_Returns400()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var request = new VariableRefundRequest
        {
            RefundAmount = 50.00m,
            RefundReason = ""
        };

        // Act
        var result = await ProcessVariableRefund.Execute(
            transactionId, request, _context, _mockRefundService, _adminUser, _mockLogger);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(400);
        problemResult.ProblemDetails.Detail.Should().Contain("at least 10 characters");
    }

    [Fact]
    public async Task Execute_WithShortRefundReason_Returns400()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var request = new VariableRefundRequest
        {
            RefundAmount = 50.00m,
            RefundReason = "Too short" // Only 9 characters
        };

        // Act
        var result = await ProcessVariableRefund.Execute(
            transactionId, request, _context, _mockRefundService, _adminUser, _mockLogger);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(400);
        problemResult.ProblemDetails.Detail.Should().Contain("at least 10 characters");
    }

    #endregion

    #region Transaction Validation Tests

    [Fact]
    public async Task Execute_WithNonExistentTransaction_Returns404()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var request = CreateValidRequest();

        // Act
        var result = await ProcessVariableRefund.Execute(
            transactionId, request, _context, _mockRefundService, _adminUser, _mockLogger);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(404);
        problemResult.ProblemDetails.Title.Should().Be("Transaction Not Found");
    }

    [Fact]
    public async Task Execute_WithNonPayPalPayment_Returns400()
    {
        // Arrange
        var ticketPurchase = await CreateTicketPurchase(100.00m, "Cash");
        var request = CreateValidRequest(50.00m);

        // Act
        var result = await ProcessVariableRefund.Execute(
            ticketPurchase.Id, request, _context, _mockRefundService, _adminUser, _mockLogger);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(400);
        problemResult.ProblemDetails.Title.Should().Be("Invalid Payment Method");
        problemResult.ProblemDetails.Detail.Should().Contain("Only PayPal payments");
    }

    [Fact]
    public async Task Execute_WithNonCompletedPayment_Returns400()
    {
        // Arrange
        var ticketPurchase = await CreateTicketPurchase(100.00m, "PayPal", isCompleted: false);
        var request = CreateValidRequest(50.00m);

        // Act
        var result = await ProcessVariableRefund.Execute(
            ticketPurchase.Id, request, _context, _mockRefundService, _adminUser, _mockLogger);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(400);
        problemResult.ProblemDetails.Title.Should().Be("Payment Not Completed");
    }

    [Fact]
    public async Task Execute_WithFullyRefundedTransaction_Returns400()
    {
        // Arrange
        var ticketPurchase = await CreateCompletedTicketPurchase(100.00m);
        await CreateExistingRefund(ticketPurchase.Id, 100.00m); // Already fully refunded
        var request = CreateValidRequest(10.00m);

        // Act
        var result = await ProcessVariableRefund.Execute(
            ticketPurchase.Id, request, _context, _mockRefundService, _adminUser, _mockLogger);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(400);
        problemResult.ProblemDetails.Title.Should().Be("Refund Amount Exceeds Limit");
    }

    #endregion

    #region Partial Refund Logic Tests

    [Fact]
    public async Task Execute_WithPartialRefund_SetsStatusToPartiallyRefunded()
    {
        // Arrange
        var ticketPurchase = await CreateCompletedTicketPurchase(100.00m);
        var request = CreateValidRequest(50.00m); // Partial refund

        SetupSuccessfulRefund(ticketPurchase.Id, 50.00m);

        // Act
        var result = await ProcessVariableRefund.Execute(
            ticketPurchase.Id, request, _context, _mockRefundService, _adminUser, _mockLogger);

        // Assert
        result.Should().BeOfType<Ok<ProcessVariableRefund.VariableRefundResponse>>();
        var okResult = (Ok<ProcessVariableRefund.VariableRefundResponse>)result;
        okResult.Value!.PaymentStatus.Should().Be("PartiallyRefunded");
        okResult.Value.Message.Should().Contain("Partial refund");
    }

    [Fact]
    public async Task Execute_WithFullRefund_SetsStatusToRefunded()
    {
        // Arrange
        var ticketPurchase = await CreateCompletedTicketPurchase(100.00m);
        var request = CreateValidRequest(100.00m); // Full refund

        SetupSuccessfulRefund(ticketPurchase.Id, 100.00m);

        // Act
        var result = await ProcessVariableRefund.Execute(
            ticketPurchase.Id, request, _context, _mockRefundService, _adminUser, _mockLogger);

        // Assert
        result.Should().BeOfType<Ok<ProcessVariableRefund.VariableRefundResponse>>();
        var okResult = (Ok<ProcessVariableRefund.VariableRefundResponse>)result;
        okResult.Value!.PaymentStatus.Should().Be("Refunded");
        okResult.Value.Message.Should().Contain("Full refund");
    }

    [Fact]
    public async Task Execute_WithMultiplePartialRefunds_AccumulatesCorrectly()
    {
        // Arrange
        var ticketPurchase = await CreateCompletedTicketPurchase(100.00m);
        await CreateExistingRefund(ticketPurchase.Id, 25.00m); // First partial refund
        await CreateExistingRefund(ticketPurchase.Id, 25.00m); // Second partial refund
        var request = CreateValidRequest(25.00m); // Third partial refund (total: 75.00)

        SetupSuccessfulRefund(ticketPurchase.Id, 25.00m);

        // Act
        var result = await ProcessVariableRefund.Execute(
            ticketPurchase.Id, request, _context, _mockRefundService, _adminUser, _mockLogger);

        // Assert
        result.Should().BeOfType<Ok<ProcessVariableRefund.VariableRefundResponse>>();
        var okResult = (Ok<ProcessVariableRefund.VariableRefundResponse>)result;
        okResult.Value!.PaymentStatus.Should().Be("PartiallyRefunded"); // Still partial
        okResult.Value.RemainingRefundableAmount.Should().Be(25.00m); // 100 - 75 = 25 remaining
    }

    [Fact]
    public async Task Execute_WithMultiplePartialsEquatingToFull_SetsStatusToRefunded()
    {
        // Arrange
        var ticketPurchase = await CreateCompletedTicketPurchase(100.00m);
        await CreateExistingRefund(ticketPurchase.Id, 50.00m); // First partial refund
        var request = CreateValidRequest(50.00m); // Second partial refund (total: 100.00)

        SetupSuccessfulRefund(ticketPurchase.Id, 50.00m);

        // Act
        var result = await ProcessVariableRefund.Execute(
            ticketPurchase.Id, request, _context, _mockRefundService, _adminUser, _mockLogger);

        // Assert
        result.Should().BeOfType<Ok<ProcessVariableRefund.VariableRefundResponse>>();
        var okResult = (Ok<ProcessVariableRefund.VariableRefundResponse>)result;
        okResult.Value!.PaymentStatus.Should().Be("Refunded"); // Now fully refunded
        okResult.Value.RemainingRefundableAmount.Should().Be(0.00m);
    }

    [Fact]
    public async Task Execute_WithRefundExceedingRemaining_Returns400()
    {
        // Arrange
        var ticketPurchase = await CreateCompletedTicketPurchase(100.00m);
        await CreateExistingRefund(ticketPurchase.Id, 60.00m); // Already refunded 60
        var request = CreateValidRequest(50.00m); // Trying to refund 50 (total would be 110)

        // Act
        var result = await ProcessVariableRefund.Execute(
            ticketPurchase.Id, request, _context, _mockRefundService, _adminUser, _mockLogger);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(400);
        problemResult.ProblemDetails.Title.Should().Be("Refund Amount Exceeds Limit");
        problemResult.ProblemDetails.Detail.Should().Contain("remaining refundable amount $40.00");
    }

    #endregion

    #region Audit Trail Tests

    [Fact]
    public async Task Execute_UpdatesTicketPurchaseNotes_WithDisclaimerAboutNotCanceling()
    {
        // Arrange
        var ticketPurchase = await CreateCompletedTicketPurchase(100.00m);
        var request = CreateValidRequest(50.00m);

        SetupSuccessfulRefund(ticketPurchase.Id, 50.00m);

        // Act
        await ProcessVariableRefund.Execute(
            ticketPurchase.Id, request, _context, _mockRefundService, _adminUser, _mockLogger);

        // Assert
        var updated = await _context.TicketPurchases.FindAsync(ticketPurchase.Id);
        updated!.Notes.Should().Contain("FINANCIAL REFUND");
        updated.Notes.Should().Contain("RSVP/Ticket NOT cancelled");
        updated.Notes.Should().Contain("$50.00 refunded");
        updated.Notes.Should().Contain("Total refunded: $50.00 of $100.00");
        updated.Notes.Should().Contain(request.RefundReason);
    }

    #endregion

    #region Helper Methods

    private ClaimsPrincipal CreateClaimsPrincipal(string userId, string email, string role)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role)
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    private VariableRefundRequest CreateValidRequest(decimal amount = 50.00m)
    {
        return new VariableRefundRequest
        {
            RefundAmount = amount,
            RefundReason = "Valid refund reason with sufficient characters"
        };
    }

    private async Task<TicketPurchase> CreateCompletedTicketPurchase(decimal amount)
    {
        return await CreateTicketPurchase(amount, "PayPal", isCompleted: true);
    }

    private async Task<TicketPurchase> CreateTicketPurchase(decimal amount, string paymentMethod, bool isCompleted = true)
    {
        var ticketType = new TicketType
        {
            Id = Guid.NewGuid(),
            Name = "General Admission",
            Price = amount,
            IsActive = true
        };

        var ticketPurchase = new TicketPurchase
        {
            Id = Guid.NewGuid(),
            UserId = _memberUserId,
            TicketTypeId = ticketType.Id,
            Quantity = 1,
            TotalPrice = amount,
            PaymentMethod = paymentMethod,
            PaymentStatus = isCompleted ? "Completed" : "Pending",
            IsPaymentCompleted = isCompleted,
            PurchaseDate = DateTime.UtcNow,
            EncryptedPayPalCaptureId = paymentMethod == "PayPal" ? "encrypted-capture-id" : null,
            TicketType = ticketType
        };

        _context.TicketTypes.Add(ticketType);
        _context.TicketPurchases.Add(ticketPurchase);
        await _context.SaveChangesAsync();

        return ticketPurchase;
    }

    private async Task CreateExistingRefund(Guid ticketPurchaseId, decimal amount)
    {
        var refund = new PaymentRefund
        {
            Id = Guid.NewGuid(),
            TicketPurchaseId = ticketPurchaseId,
            RefundAmountValue = amount,
            RefundCurrency = "USD",
            RefundStatus = RefundStatus.Completed,
            RefundReason = "Previous refund",
            ProcessedByUserId = _adminUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.PaymentRefunds.Add(refund);
        await _context.SaveChangesAsync();
    }

    private void SetupSuccessfulRefund(Guid paymentId, decimal amount)
    {
        var refund = new PaymentRefund
        {
            Id = Guid.NewGuid(),
            TicketPurchaseId = paymentId,
            RefundAmountValue = amount,
            RefundCurrency = "USD",
            RefundStatus = RefundStatus.Completed,
            RefundReason = "Test refund",
            ProcessedByUserId = _adminUserId,
            CreatedAt = DateTime.UtcNow
        };

        _mockRefundService.ProcessRefundAsync(Arg.Any<ProcessRefundRequest>(), Arg.Any<CancellationToken>())
            .Returns(RefundResult.Success(refund));
    }

    #endregion
}

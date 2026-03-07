using WitchCityRope.Api.Features.Shared.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.EmailTemplates.Entities;
using WitchCityRope.Api.Features.Payments.Entities;
using WitchCityRope.Api.Features.Payments.Models;
using WitchCityRope.Api.Features.Payments.Models.PayPal;
using WitchCityRope.Api.Features.Payments.Services;
using WitchCityRope.Api.Features.Payments.ValueObjects;
using WitchCityRope.Api.Features.Safety.Services;
using SharedResult = WitchCityRope.Api.Features.Shared.Models.Result;
using WitchCityRope.Api.Features.Shared.Services;
using WitchCityRope.Api.Features.Volunteers.Services;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Tests.Fixtures;
using Xunit;

namespace WitchCityRope.UnitTests.Api.Features.Payments.Services;

/// <summary>
/// Unit tests for RefundService email notification functionality
/// Tests email sending for refund confirmations (Phase 3 of PayPal refund system)
/// Uses TestContainers PostgreSQL via DatabaseTestFixture (NOT InMemoryDatabase)
/// </summary>
[Collection("Database")]
public class RefundServiceEmailTests : IAsyncLifetime
{
    private readonly DatabaseTestFixture _fixture;
    private ApplicationDbContext _context = null!;
    private readonly IPayPalService _mockPayPalService;
    private readonly IEncryptionService _mockEncryptionService;
    private readonly IVolunteerAssignmentService _mockVolunteerAssignmentService;
    private readonly IEmailService _mockEmailService;
    private readonly ILogger<RefundService> _mockLogger;
    private RefundService _sut = null!;
    private Guid _testUserId;
    private readonly Guid _adminUserId = Guid.NewGuid();
    private Guid _ticketPurchaseId;

    public RefundServiceEmailTests(DatabaseTestFixture fixture)
    {
        _fixture = fixture;

        // Setup mocks
        _mockPayPalService = Substitute.For<IPayPalService>();
        _mockEncryptionService = Substitute.For<IEncryptionService>();
        _mockVolunteerAssignmentService = Substitute.For<IVolunteerAssignmentService>();
        _mockEmailService = Substitute.For<IEmailService>();
        _mockLogger = Substitute.For<ILogger<RefundService>>();

        // Setup default encryption behavior
        _mockEncryptionService.DecryptAsync(Arg.Any<string>())
            .Returns(Task.FromResult("decrypted_capture_id"));
        _mockEncryptionService.EncryptAsync(Arg.Any<string>())
            .Returns(Task.FromResult("encrypted_refund_id"));

        // Setup default volunteer cancellation behavior
        _mockVolunteerAssignmentService.CancelAllVolunteerSignupsForUserEventAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult((success: true, cancelledCount: 0, error: (string?)null)));
    }

    public async Task InitializeAsync()
    {
        _context = _fixture.CreateDbContext();
        _testUserId = Guid.NewGuid();
        _ticketPurchaseId = Guid.NewGuid();

        // Create system under test with real context
        _sut = new RefundService(
            _context,
            _mockPayPalService,
            _mockEncryptionService,
            _mockVolunteerAssignmentService,
            _mockEmailService,
            _mockLogger);

        // Seed test user (required for FK constraints)
        var testUser = new ApplicationUser
        {
            Id = _testUserId,
            UserName = "TestUser",
            Email = "test@witchcityrope.com",
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Users.Add(testUser);

        // Seed admin user (required for PaymentRefund.ProcessedByUserId FK)
        var adminUser = new ApplicationUser
        {
            Id = _adminUserId,
            UserName = "AdminUser",
            Email = "admin@witchcityrope.com",
            EmailConfirmed = true,
            Role = "Administrator",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Users.Add(adminUser);
        await _context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        _context?.Dispose();
        await _fixture.ResetDatabaseAsync();
    }

    #region Email Sending - Happy Path

    [Fact]
    public async Task ProcessRefundAsync_WithSuccessfulPayPalRefund_SendsRefundConfirmationEmail()
    {
        // Arrange
        var ticketPurchase = await CreateAndSaveTestTicketPurchaseAsync();

        var request = CreateRefundRequest(ticketPurchase.Id, Money.Create(25.00m, "USD"));

        SetupSuccessfulPayPalRefund();
        SetupSuccessfulEmail();

        // Act
        var result = await _sut.ProcessRefundAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _mockEmailService.Received(1).SendTemplatedEmailAsync(
            ticketPurchase.User!.Email!,
            Arg.Any<string>(),
            EmailCategory.Admin,
            "RefundConfirmation",
            Arg.Any<Dictionary<string, string>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessRefundAsync_WithSuccessfulRefund_SendsEmailToCorrectRecipient()
    {
        // Arrange
        var ticketPurchase = await CreateAndSaveTestTicketPurchaseAsync();

        var request = CreateRefundRequest(ticketPurchase.Id, Money.Create(25.00m, "USD"));

        SetupSuccessfulPayPalRefund();
        SetupSuccessfulEmail();

        // Act
        await _sut.ProcessRefundAsync(request);

        // Assert
        await _mockEmailService.Received(1).SendTemplatedEmailAsync(
            "test@witchcityrope.com",
            Arg.Any<string>(),
            Arg.Any<EmailCategory>(),
            Arg.Any<string>(),
            Arg.Any<Dictionary<string, string>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessRefundAsync_WithSuccessfulRefund_UsesCorrectEmailCategory()
    {
        // Arrange
        var ticketPurchase = await CreateAndSaveTestTicketPurchaseAsync();
        var request = CreateRefundRequest(ticketPurchase.Id, Money.Create(25.00m, "USD"));
        SetupSuccessfulPayPalRefund();
        SetupSuccessfulEmail();

        // Act
        await _sut.ProcessRefundAsync(request);

        // Assert
        await _mockEmailService.Received(1).SendTemplatedEmailAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            EmailCategory.Admin,
            Arg.Any<string>(),
            Arg.Any<Dictionary<string, string>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessRefundAsync_WithSuccessfulRefund_UsesCorrectTemplateType()
    {
        // Arrange
        var ticketPurchase = await CreateAndSaveTestTicketPurchaseAsync();
        var request = CreateRefundRequest(ticketPurchase.Id, Money.Create(25.00m, "USD"));
        SetupSuccessfulPayPalRefund();
        SetupSuccessfulEmail();

        // Act
        await _sut.ProcessRefundAsync(request);

        // Assert
        await _mockEmailService.Received(1).SendTemplatedEmailAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<EmailCategory>(),
            "RefundConfirmation",
            Arg.Any<Dictionary<string, string>>(),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Email Template Variables

    [Fact]
    public async Task ProcessRefundAsync_EmailTemplateVariables_ContainsUserName()
    {
        // Arrange
        var ticketPurchase = await CreateAndSaveTestTicketPurchaseAsync();
        // Update username via a fresh query to avoid tracking issues
        var user = await _context.Users.FindAsync(_testUserId);
        user!.UserName = "TestSceneName";
        await _context.SaveChangesAsync();

        var request = CreateRefundRequest(ticketPurchase.Id, Money.Create(25.00m, "USD"));
        SetupSuccessfulPayPalRefund();
        SetupSuccessfulEmail();

        // Act
        await _sut.ProcessRefundAsync(request);

        // Assert
        await _mockEmailService.Received(1).SendTemplatedEmailAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<EmailCategory>(),
            Arg.Any<string>(),
            Arg.Is<Dictionary<string, string>>(vars =>
                vars.ContainsKey("user_name") && vars["user_name"] == "TestSceneName"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessRefundAsync_EmailTemplateVariables_ContainsFormattedRefundAmount()
    {
        // Arrange
        var ticketPurchase = await CreateAndSaveTestTicketPurchaseAsync();
        var request = CreateRefundRequest(ticketPurchase.Id, Money.Create(25.00m, "USD"));
        SetupSuccessfulPayPalRefund();
        SetupSuccessfulEmail();

        // Act
        await _sut.ProcessRefundAsync(request);

        // Assert
        await _mockEmailService.Received(1).SendTemplatedEmailAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<EmailCategory>(),
            Arg.Any<string>(),
            Arg.Is<Dictionary<string, string>>(vars =>
                vars.ContainsKey("refund_amount") && vars["refund_amount"] == "$25.00"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessRefundAsync_EmailTemplateVariables_ContainsOriginalAmount()
    {
        // Arrange
        var ticketPurchase = await CreateAndSaveTestTicketPurchaseAsync();
        var request = CreateRefundRequest(ticketPurchase.Id, Money.Create(25.00m, "USD"));
        SetupSuccessfulPayPalRefund();
        SetupSuccessfulEmail();

        // Act
        await _sut.ProcessRefundAsync(request);

        // Assert
        await _mockEmailService.Received(1).SendTemplatedEmailAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<EmailCategory>(),
            Arg.Any<string>(),
            Arg.Is<Dictionary<string, string>>(vars =>
                vars.ContainsKey("original_amount") && vars["original_amount"] == "$50.00"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessRefundAsync_EmailTemplateVariables_ContainsRefundReason()
    {
        // Arrange
        var ticketPurchase = await CreateAndSaveTestTicketPurchaseAsync();
        var refundReason = "Customer requested cancellation due to scheduling conflict";
        var request = CreateRefundRequest(ticketPurchase.Id, Money.Create(25.00m, "USD"));
        request.RefundReason = refundReason;
        SetupSuccessfulPayPalRefund();
        SetupSuccessfulEmail();

        // Act
        await _sut.ProcessRefundAsync(request);

        // Assert
        await _mockEmailService.Received(1).SendTemplatedEmailAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<EmailCategory>(),
            Arg.Any<string>(),
            Arg.Is<Dictionary<string, string>>(vars =>
                vars.ContainsKey("refund_reason") && vars["refund_reason"] == refundReason),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessRefundAsync_EmailTemplateVariables_ContainsPaymentMethodAndTimingMessage()
    {
        // Arrange
        var ticketPurchase = await CreateAndSaveTestTicketPurchaseAsync("PayPal");
        var request = CreateRefundRequest(ticketPurchase.Id, Money.Create(25.00m, "USD"));
        SetupSuccessfulPayPalRefund();
        SetupSuccessfulEmail();

        // Act
        await _sut.ProcessRefundAsync(request);

        // Assert
        await _mockEmailService.Received(1).SendTemplatedEmailAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<EmailCategory>(),
            Arg.Any<string>(),
            Arg.Is<Dictionary<string, string>>(vars =>
                vars.ContainsKey("payment_method") &&
                vars["payment_method"] == "PayPal" &&
                vars.ContainsKey("timing_message") &&
                vars["timing_message"].Contains("immediately")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessRefundAsync_EmailTemplateVariables_ContainsRefundIdAndSupportEmail()
    {
        // Arrange
        var ticketPurchase = await CreateAndSaveTestTicketPurchaseAsync();
        var request = CreateRefundRequest(ticketPurchase.Id, Money.Create(25.00m, "USD"));
        SetupSuccessfulPayPalRefund();
        SetupSuccessfulEmail();

        // Act
        await _sut.ProcessRefundAsync(request);

        // Assert
        await _mockEmailService.Received(1).SendTemplatedEmailAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<EmailCategory>(),
            Arg.Any<string>(),
            Arg.Is<Dictionary<string, string>>(vars =>
                vars.ContainsKey("refund_id") &&
                !vars.ContainsKey("support_email")),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Error Handling

    [Fact]
    public async Task ProcessRefundAsync_WhenEmailFails_RefundStillCompletes()
    {
        // Arrange
        var ticketPurchase = await CreateAndSaveTestTicketPurchaseAsync();
        var request = CreateRefundRequest(ticketPurchase.Id, Money.Create(25.00m, "USD"));
        SetupSuccessfulPayPalRefund();
        _mockEmailService.SendTemplatedEmailAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<EmailCategory>(),
                Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(SharedResult.Failure("Email service unavailable"));

        // Act
        var result = await _sut.ProcessRefundAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue("refund should complete even if email fails");
        result.Value.Should().NotBeNull();
        result.Value!.RefundStatus.Should().Be(RefundStatus.Completed);
    }

    [Fact]
    public async Task ProcessRefundAsync_WhenEmailFails_PaymentRefundEntityIsStillCreated()
    {
        // Arrange
        var ticketPurchase = await CreateAndSaveTestTicketPurchaseAsync();
        var request = CreateRefundRequest(ticketPurchase.Id, Money.Create(25.00m, "USD"));
        SetupSuccessfulPayPalRefund();
        _mockEmailService.SendTemplatedEmailAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<EmailCategory>(),
                Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(SharedResult.Failure("Email service unavailable"));

        // Act
        var result = await _sut.ProcessRefundAsync(request);

        // Assert
        var savedRefund = await _context.PaymentRefunds.FirstOrDefaultAsync(r => r.TicketPurchaseId == ticketPurchase.Id);
        savedRefund.Should().NotBeNull("refund entity should be created despite email failure");
        savedRefund!.RefundStatus.Should().Be(RefundStatus.Completed);
    }

    [Fact]
    public async Task ProcessRefundAsync_WhenEmailFails_ErrorIsLogged()
    {
        // Arrange
        var ticketPurchase = await CreateAndSaveTestTicketPurchaseAsync();
        var request = CreateRefundRequest(ticketPurchase.Id, Money.Create(25.00m, "USD"));
        SetupSuccessfulPayPalRefund();
        _mockEmailService.SendTemplatedEmailAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<EmailCategory>(),
                Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(SharedResult.Failure("Email service unavailable"));

        // Act
        await _sut.ProcessRefundAsync(request);

        // Assert
        _mockLogger.Received().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Failed to send refund confirmation email")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task ProcessRefundAsync_WhenEmailThrowsException_RefundStillCompletes()
    {
        // Arrange
        var ticketPurchase = await CreateAndSaveTestTicketPurchaseAsync();
        var request = CreateRefundRequest(ticketPurchase.Id, Money.Create(25.00m, "USD"));
        SetupSuccessfulPayPalRefund();
        _mockEmailService.SendTemplatedEmailAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<EmailCategory>(),
                Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns<SharedResult>(_ => throw new Exception("SMTP connection failed"));

        // Act
        var result = await _sut.ProcessRefundAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue("refund should complete even if email throws exception");
        result.Value!.RefundStatus.Should().Be(RefundStatus.Completed);
    }

    [Fact]
    public async Task ProcessRefundAsync_WhenEmailThrowsException_ErrorIsLogged()
    {
        // Arrange
        var ticketPurchase = await CreateAndSaveTestTicketPurchaseAsync();
        var request = CreateRefundRequest(ticketPurchase.Id, Money.Create(25.00m, "USD"));
        SetupSuccessfulPayPalRefund();
        _mockEmailService.SendTemplatedEmailAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<EmailCategory>(),
                Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns<SharedResult>(_ => throw new Exception("SMTP connection failed"));

        // Act
        await _sut.ProcessRefundAsync(request);

        // Assert
        _mockLogger.Received().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Is<Exception>(ex => ex.Message.Contains("SMTP connection failed")),
            Arg.Any<Func<object, Exception?, string>>());
    }

    #endregion

    #region Integration with PaymentRefund Audit Trail

    [Fact]
    public async Task ProcessRefundAsync_RefundReason_IsStoredInPaymentRefundEntity()
    {
        // Arrange
        var ticketPurchase = await CreateAndSaveTestTicketPurchaseAsync();
        var refundReason = "Customer requested refund due to event cancellation";
        var request = CreateRefundRequest(ticketPurchase.Id, Money.Create(25.00m, "USD"));
        request.RefundReason = refundReason;
        SetupSuccessfulPayPalRefund();
        SetupSuccessfulEmail();

        // Act
        var result = await _sut.ProcessRefundAsync(request);

        // Assert
        result.Value!.RefundReason.Should().Be(refundReason);
        var savedRefund = await _context.PaymentRefunds.FirstOrDefaultAsync(r => r.Id == result.Value.Id);
        savedRefund.Should().NotBeNull();
        savedRefund!.RefundReason.Should().Be(refundReason);
    }

    [Fact]
    public async Task ProcessRefundAsync_ProcessedByUserId_IsCorrectlySet()
    {
        // Arrange
        var ticketPurchase = await CreateAndSaveTestTicketPurchaseAsync();
        var request = CreateRefundRequest(ticketPurchase.Id, Money.Create(25.00m, "USD"));
        request.ProcessedByUserId = _adminUserId;
        SetupSuccessfulPayPalRefund();
        SetupSuccessfulEmail();

        // Act
        var result = await _sut.ProcessRefundAsync(request);

        // Assert
        result.Value!.ProcessedByUserId.Should().Be(_adminUserId);
        var savedRefund = await _context.PaymentRefunds.FirstOrDefaultAsync(r => r.Id == result.Value.Id);
        savedRefund.Should().NotBeNull();
        savedRefund!.ProcessedByUserId.Should().Be(_adminUserId);
    }

    [Fact]
    public async Task ProcessRefundAsync_ProcessedAt_TimestampIsSet()
    {
        // Arrange
        var ticketPurchase = await CreateAndSaveTestTicketPurchaseAsync();
        var request = CreateRefundRequest(ticketPurchase.Id, Money.Create(25.00m, "USD"));
        var beforeRefund = DateTime.UtcNow;
        SetupSuccessfulPayPalRefund();
        SetupSuccessfulEmail();

        // Act
        var result = await _sut.ProcessRefundAsync(request);
        var afterRefund = DateTime.UtcNow;

        // Assert
        result.Value!.ProcessedAt.Should().BeOnOrAfter(beforeRefund);
        result.Value.ProcessedAt.Should().BeOnOrBefore(afterRefund);
        var savedRefund = await _context.PaymentRefunds.FirstOrDefaultAsync(r => r.Id == result.Value.Id);
        savedRefund.Should().NotBeNull();
        savedRefund!.ProcessedAt.Should().BeOnOrAfter(beforeRefund);
        savedRefund.ProcessedAt.Should().BeOnOrBefore(afterRefund);
    }

    #endregion

    #region Email Not Sent When Refund Fails

    [Fact]
    public async Task ProcessRefundAsync_WhenPayPalRefundFails_EmailIsNotSent()
    {
        // Arrange
        var ticketPurchase = await CreateAndSaveTestTicketPurchaseAsync();
        var request = CreateRefundRequest(ticketPurchase.Id, Money.Create(25.00m, "USD"));
        _mockPayPalService.RefundCaptureAsync(
                Arg.Any<string>(), Arg.Any<Money>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<PayPalRefundResponse>.Failure("Insufficient funds in PayPal account"));
        SetupSuccessfulEmail();

        // Act
        var result = await _sut.ProcessRefundAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue("service returns success but refund is marked as failed");
        result.Value!.RefundStatus.Should().Be(RefundStatus.Failed);
        await _mockEmailService.DidNotReceive().SendTemplatedEmailAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<EmailCategory>(),
            Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessRefundAsync_WhenRefundStatusIsProcessing_EmailIsNotSent()
    {
        // Arrange
        var ticketPurchase = await CreateAndSaveTestTicketPurchaseAsync();
        // Remove capture ID to force manual refund path
        ticketPurchase.EncryptedPayPalCaptureId = null;
        ticketPurchase.EncryptedPayPalOrderId = "ORDER_12345";
        _context.TicketPurchases.Update(ticketPurchase);
        await _context.SaveChangesAsync();

        var request = CreateRefundRequest(ticketPurchase.Id, Money.Create(25.00m, "USD"));
        SetupSuccessfulEmail();

        // Act
        var result = await _sut.ProcessRefundAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse("legacy payment requires manual refund");
        await _mockEmailService.DidNotReceive().SendTemplatedEmailAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<EmailCategory>(),
            Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessRefundAsync_ManualRefundWithNoPayPalId_SendsEmailWhenCompleted()
    {
        // Arrange
        var ticketPurchase = await CreateAndSaveTestTicketPurchaseAsync("Cash");
        ticketPurchase.EncryptedPayPalCaptureId = null;
        ticketPurchase.EncryptedPayPalOrderId = null;
        _context.TicketPurchases.Update(ticketPurchase);
        await _context.SaveChangesAsync();

        var request = CreateRefundRequest(ticketPurchase.Id, Money.Create(25.00m, "USD"));
        SetupSuccessfulEmail();

        // Act
        var result = await _sut.ProcessRefundAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.RefundStatus.Should().Be(RefundStatus.Completed);
        await _mockEmailService.Received(1).SendTemplatedEmailAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<EmailCategory>(),
            "RefundConfirmation", Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Helper Methods

    private async Task<TicketPurchase> CreateAndSaveTestTicketPurchaseAsync(string paymentMethod = "PayPal")
    {
        // Need a TicketType with a valid EventId for FK constraint
        // First ensure venue exists
        var venue = await _context.Venues.FindAsync(1);
        if (venue == null)
        {
            venue = new Venue
            {
                Id = 1,
                Name = "Test Venue",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Venues.Add(venue);
            await _context.SaveChangesAsync();
        }

        // Create event
        var eventId = Guid.NewGuid();
        var testEvent = new Event
        {
            Id = eventId,
            Title = "Test Event",
            Description = "Test",
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(2),
            Capacity = 20,
            VenueId = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Events.Add(testEvent);
        await _context.SaveChangesAsync();

        // Create ticket type
        var ticketTypeId = Guid.NewGuid();
        var ticketType = new TicketType
        {
            Id = ticketTypeId,
            EventId = eventId,
            Name = "Test Ticket",
            Price = 50.00m,
            Available = 20,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.TicketTypes.Add(ticketType);
        await _context.SaveChangesAsync();

        var ticketPurchase = new TicketPurchase
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            TicketTypeId = ticketTypeId,
            TotalPrice = 50.00m,
            PaymentStatus = TicketPurchasePaymentStatus.Completed,
            PaymentMethod = paymentMethod,
            EncryptedPayPalCaptureId = "encrypted_capture_id_12345",
            ProcessedAt = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        _context.TicketPurchases.Add(ticketPurchase);
        await _context.SaveChangesAsync();

        // Reload with navigation properties
        return await _context.TicketPurchases
            .Include(tp => tp.User)
            .FirstAsync(tp => tp.Id == ticketPurchase.Id);
    }

    private ProcessRefundRequest CreateRefundRequest(Guid ticketPurchaseId, Money refundAmount)
    {
        return new ProcessRefundRequest
        {
            TicketPurchaseId = ticketPurchaseId,
            RefundAmount = refundAmount,
            RefundReason = "Customer requested refund",
            ProcessedByUserId = _adminUserId,
            IpAddress = "127.0.0.1",
            UserAgent = "Test Agent",
            Metadata = new Dictionary<string, object>()
        };
    }

    private void SetupSuccessfulPayPalRefund()
    {
        var paypalRefundResult = new PayPalRefundResponse
        {
            RefundId = "REFUND_12345",
            Status = "COMPLETED",
            Amount = new PayPalAmount { Value = "25.00", CurrencyCode = "USD" },
            CreateTime = DateTime.UtcNow
        };
        _mockPayPalService.RefundCaptureAsync(
                Arg.Any<string>(), Arg.Any<Money>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<PayPalRefundResponse>.Success(paypalRefundResult));
    }

    private void SetupSuccessfulEmail()
    {
        _mockEmailService.SendTemplatedEmailAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<EmailCategory>(),
                Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(SharedResult.Success());
    }

    #endregion
}

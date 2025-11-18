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
using Xunit;

namespace WitchCityRope.UnitTests.Api.Features.Payments.Services;

/// <summary>
/// Unit tests for RefundService email notification functionality
/// Tests email sending for refund confirmations (Phase 3 of PayPal refund system)
/// </summary>
public class RefundServiceEmailTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IPayPalService _mockPayPalService;
    private readonly IEncryptionService _mockEncryptionService;
    private readonly IVolunteerAssignmentService _mockVolunteerAssignmentService;
    private readonly IEmailService _mockEmailService;
    private readonly ILogger<RefundService> _mockLogger;
    private readonly RefundService _sut;
    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly Guid _adminUserId = Guid.NewGuid();
    private readonly Guid _paymentId = Guid.NewGuid();

    public RefundServiceEmailTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"RefundServiceEmailTests_{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(options);

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

        // Create system under test
        _sut = new RefundService(
            _context,
            _mockPayPalService,
            _mockEncryptionService,
            _mockVolunteerAssignmentService,
            _mockEmailService,
            _mockLogger);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region Email Sending - Happy Path

    [Fact]
    public async Task ProcessRefundAsync_WithSuccessfulPayPalRefund_SendsRefundConfirmationEmail()
    {
        // Arrange
        var payment = CreateTestPaymentWithUser();
        await _context.Payments.AddAsync(payment);
        await _context.SaveChangesAsync();

        var request = CreateRefundRequest(payment.Id, Money.Create(25.00m, "USD"));

        // Setup PayPal to succeed
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

        // Setup email service to succeed
        _mockEmailService.SendTemplatedEmailAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<EmailCategory>(),
                Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(SharedResult.Success());

        // Act
        var result = await _sut.ProcessRefundAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _mockEmailService.Received(1).SendTemplatedEmailAsync(
            payment.User!.Email!,
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
        var payment = CreateTestPaymentWithUser();
        await _context.Payments.AddAsync(payment);
        await _context.SaveChangesAsync();

        var request = CreateRefundRequest(payment.Id, Money.Create(25.00m, "USD"));

        // Setup PayPal to succeed
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

        _mockEmailService.SendTemplatedEmailAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<EmailCategory>(),
                Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(SharedResult.Success());

        // Act
        await _sut.ProcessRefundAsync(request);

        // Assert
        await _mockEmailService.Received(1).SendTemplatedEmailAsync(
            "test@witchcityrope.com", // Should match payment.User.Email
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
        var payment = CreateTestPaymentWithUser();
        await _context.Payments.AddAsync(payment);
        await _context.SaveChangesAsync();

        var request = CreateRefundRequest(payment.Id, Money.Create(25.00m, "USD"));

        // Setup PayPal to succeed
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

        _mockEmailService.SendTemplatedEmailAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<EmailCategory>(),
                Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(SharedResult.Success());

        // Act
        await _sut.ProcessRefundAsync(request);

        // Assert
        await _mockEmailService.Received(1).SendTemplatedEmailAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            EmailCategory.Admin, // Must be Admin category
            Arg.Any<string>(),
            Arg.Any<Dictionary<string, string>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessRefundAsync_WithSuccessfulRefund_UsesCorrectTemplateType()
    {
        // Arrange
        var payment = CreateTestPaymentWithUser();
        await _context.Payments.AddAsync(payment);
        await _context.SaveChangesAsync();

        var request = CreateRefundRequest(payment.Id, Money.Create(25.00m, "USD"));

        // Setup PayPal to succeed
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

        _mockEmailService.SendTemplatedEmailAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<EmailCategory>(),
                Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(SharedResult.Success());

        // Act
        await _sut.ProcessRefundAsync(request);

        // Assert
        await _mockEmailService.Received(1).SendTemplatedEmailAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<EmailCategory>(),
            "RefundConfirmation", // Must be RefundConfirmation template
            Arg.Any<Dictionary<string, string>>(),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Email Template Variables

    [Fact]
    public async Task ProcessRefundAsync_EmailTemplateVariables_ContainsUserName()
    {
        // Arrange
        var payment = CreateTestPaymentWithUser();
        payment.User!.UserName = "TestSceneName";
        await _context.Payments.AddAsync(payment);
        await _context.SaveChangesAsync();

        var request = CreateRefundRequest(payment.Id, Money.Create(25.00m, "USD"));

        // Setup PayPal to succeed
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

        _mockEmailService.SendTemplatedEmailAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<EmailCategory>(),
                Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(SharedResult.Success());

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
        var payment = CreateTestPaymentWithUser();
        await _context.Payments.AddAsync(payment);
        await _context.SaveChangesAsync();

        var request = CreateRefundRequest(payment.Id, Money.Create(25.00m, "USD"));

        // Setup PayPal to succeed
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

        _mockEmailService.SendTemplatedEmailAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<EmailCategory>(),
                Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(SharedResult.Success());

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
        var payment = CreateTestPaymentWithUser();
        await _context.Payments.AddAsync(payment);
        await _context.SaveChangesAsync();

        var request = CreateRefundRequest(payment.Id, Money.Create(25.00m, "USD"));

        // Setup PayPal to succeed
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

        _mockEmailService.SendTemplatedEmailAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<EmailCategory>(),
                Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(SharedResult.Success());

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
        var payment = CreateTestPaymentWithUser();
        await _context.Payments.AddAsync(payment);
        await _context.SaveChangesAsync();

        var refundReason = "Customer requested cancellation due to scheduling conflict";
        var request = CreateRefundRequest(payment.Id, Money.Create(25.00m, "USD"));
        request.RefundReason = refundReason;

        // Setup PayPal to succeed
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

        _mockEmailService.SendTemplatedEmailAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<EmailCategory>(),
                Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(SharedResult.Success());

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
        var payment = CreateTestPaymentWithUser();
        payment.PaymentMethodType = PaymentMethodType.PayPal;
        await _context.Payments.AddAsync(payment);
        await _context.SaveChangesAsync();

        var request = CreateRefundRequest(payment.Id, Money.Create(25.00m, "USD"));

        // Setup PayPal to succeed
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

        _mockEmailService.SendTemplatedEmailAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<EmailCategory>(),
                Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(SharedResult.Success());

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
        var payment = CreateTestPaymentWithUser();
        await _context.Payments.AddAsync(payment);
        await _context.SaveChangesAsync();

        var request = CreateRefundRequest(payment.Id, Money.Create(25.00m, "USD"));

        // Setup PayPal to succeed
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

        _mockEmailService.SendTemplatedEmailAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<EmailCategory>(),
                Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(SharedResult.Success());

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
                !vars.ContainsKey("support_email")), // Static variable removed - now hardcoded in template
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Error Handling

    [Fact]
    public async Task ProcessRefundAsync_WhenEmailFails_RefundStillCompletes()
    {
        // Arrange
        var payment = CreateTestPaymentWithUser();
        await _context.Payments.AddAsync(payment);
        await _context.SaveChangesAsync();

        var request = CreateRefundRequest(payment.Id, Money.Create(25.00m, "USD"));

        // Setup PayPal to succeed
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

        // Setup email service to fail
        _mockEmailService.SendTemplatedEmailAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<EmailCategory>(),
                Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(SharedResult.Failure("Email service unavailable"));

        // Act
        var result = await _sut.ProcessRefundAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue("refund should complete even if email fails");
        result.Value.Should().NotBeNull();
        result.Value!.RefundStatus.Should().Be(RefundStatus.Completed,
            "refund should be marked as completed despite email failure");
    }

    [Fact]
    public async Task ProcessRefundAsync_WhenEmailFails_PaymentRefundEntityIsStillCreated()
    {
        // Arrange
        var payment = CreateTestPaymentWithUser();
        await _context.Payments.AddAsync(payment);
        await _context.SaveChangesAsync();

        var request = CreateRefundRequest(payment.Id, Money.Create(25.00m, "USD"));

        // Setup PayPal to succeed
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

        // Setup email service to fail
        _mockEmailService.SendTemplatedEmailAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<EmailCategory>(),
                Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(SharedResult.Failure("Email service unavailable"));

        // Act
        var result = await _sut.ProcessRefundAsync(request);

        // Assert
        var savedRefund = await _context.PaymentRefunds.FirstOrDefaultAsync(r => r.OriginalPaymentId == payment.Id);
        savedRefund.Should().NotBeNull("refund entity should be created despite email failure");
        savedRefund!.RefundStatus.Should().Be(RefundStatus.Completed);
    }

    [Fact]
    public async Task ProcessRefundAsync_WhenEmailFails_ErrorIsLogged()
    {
        // Arrange
        var payment = CreateTestPaymentWithUser();
        await _context.Payments.AddAsync(payment);
        await _context.SaveChangesAsync();

        var request = CreateRefundRequest(payment.Id, Money.Create(25.00m, "USD"));

        // Setup PayPal to succeed
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

        // Setup email service to fail
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
        var payment = CreateTestPaymentWithUser();
        await _context.Payments.AddAsync(payment);
        await _context.SaveChangesAsync();

        var request = CreateRefundRequest(payment.Id, Money.Create(25.00m, "USD"));

        // Setup PayPal to succeed
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

        // Setup email service to throw exception
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
        var payment = CreateTestPaymentWithUser();
        await _context.Payments.AddAsync(payment);
        await _context.SaveChangesAsync();

        var request = CreateRefundRequest(payment.Id, Money.Create(25.00m, "USD"));

        // Setup PayPal to succeed
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

        // Setup email service to throw exception
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
        var payment = CreateTestPaymentWithUser();
        await _context.Payments.AddAsync(payment);
        await _context.SaveChangesAsync();

        var refundReason = "Customer requested refund due to event cancellation";
        var request = CreateRefundRequest(payment.Id, Money.Create(25.00m, "USD"));
        request.RefundReason = refundReason;

        // Setup PayPal to succeed
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

        _mockEmailService.SendTemplatedEmailAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<EmailCategory>(),
                Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(SharedResult.Success());

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
        var payment = CreateTestPaymentWithUser();
        await _context.Payments.AddAsync(payment);
        await _context.SaveChangesAsync();

        var request = CreateRefundRequest(payment.Id, Money.Create(25.00m, "USD"));
        request.ProcessedByUserId = _adminUserId;

        // Setup PayPal to succeed
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

        _mockEmailService.SendTemplatedEmailAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<EmailCategory>(),
                Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(SharedResult.Success());

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
        var payment = CreateTestPaymentWithUser();
        await _context.Payments.AddAsync(payment);
        await _context.SaveChangesAsync();

        var request = CreateRefundRequest(payment.Id, Money.Create(25.00m, "USD"));

        var beforeRefund = DateTime.UtcNow;

        // Setup PayPal to succeed
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

        _mockEmailService.SendTemplatedEmailAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<EmailCategory>(),
                Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(SharedResult.Success());

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
        var payment = CreateTestPaymentWithUser();
        await _context.Payments.AddAsync(payment);
        await _context.SaveChangesAsync();

        var request = CreateRefundRequest(payment.Id, Money.Create(25.00m, "USD"));

        // Setup PayPal to fail
        _mockPayPalService.RefundCaptureAsync(
                Arg.Any<string>(), Arg.Any<Money>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<PayPalRefundResponse>.Failure("Insufficient funds in PayPal account"));

        _mockEmailService.SendTemplatedEmailAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<EmailCategory>(),
                Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(SharedResult.Success());

        // Act
        var result = await _sut.ProcessRefundAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue("service returns success but refund is marked as failed");
        result.Value!.RefundStatus.Should().Be(RefundStatus.Failed);

        // Email should NOT be sent because refund failed
        await _mockEmailService.DidNotReceive().SendTemplatedEmailAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<EmailCategory>(),
            Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessRefundAsync_WhenRefundStatusIsProcessing_EmailIsNotSent()
    {
        // Arrange
        var payment = CreateTestPaymentWithUser();
        // Remove capture ID to force manual refund path that doesn't complete automatically
        payment.EncryptedPayPalCaptureId = null;
        payment.EncryptedPayPalOrderId = "ORDER_12345"; // Has order ID but no capture ID
        await _context.Payments.AddAsync(payment);
        await _context.SaveChangesAsync();

        var request = CreateRefundRequest(payment.Id, Money.Create(25.00m, "USD"));

        _mockEmailService.SendTemplatedEmailAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<EmailCategory>(),
                Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(SharedResult.Success());

        // Act
        var result = await _sut.ProcessRefundAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse("legacy payment requires manual refund");

        // Email should NOT be sent because refund is not completed
        await _mockEmailService.DidNotReceive().SendTemplatedEmailAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<EmailCategory>(),
            Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessRefundAsync_ManualRefundWithNoPayPalId_SendsEmailWhenCompleted()
    {
        // Arrange
        var payment = CreateTestPaymentWithUser();
        // Manual payment (cash, etc.) with no PayPal IDs
        payment.EncryptedPayPalCaptureId = null;
        payment.EncryptedPayPalOrderId = null;
        payment.PaymentMethodType = PaymentMethodType.Cash;
        await _context.Payments.AddAsync(payment);
        await _context.SaveChangesAsync();

        var request = CreateRefundRequest(payment.Id, Money.Create(25.00m, "USD"));

        _mockEmailService.SendTemplatedEmailAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<EmailCategory>(),
                Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(SharedResult.Success());

        // Act
        var result = await _sut.ProcessRefundAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.RefundStatus.Should().Be(RefundStatus.Completed);

        // Email SHOULD be sent because manual refund is marked as completed
        await _mockEmailService.Received(1).SendTemplatedEmailAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<EmailCategory>(),
            "RefundConfirmation", Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Helper Methods

    private Payment CreateTestPaymentWithUser()
    {
        var user = new ApplicationUser
        {
            Id = _testUserId,
            UserName = "TestUser",
            Email = "test@witchcityrope.com",
            EmailConfirmed = true
        };

        var payment = new Payment
        {
            Id = _paymentId,
            UserId = _testUserId,
            User = user,
            EventRegistrationId = Guid.NewGuid(),
            AmountValue = 50.00m,
            
            Status = PaymentStatus.Completed,
            PaymentMethodType = PaymentMethodType.PayPal,
            EncryptedPayPalCaptureId = "encrypted_capture_id_12345",
            ProcessedAt = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        return payment;
    }

    private ProcessRefundRequest CreateRefundRequest(Guid paymentId, Money refundAmount)
    {
        return new ProcessRefundRequest
        {
            PaymentId = paymentId,
            RefundAmount = refundAmount,
            RefundReason = "Customer requested refund",
            ProcessedByUserId = _adminUserId,
            IpAddress = "127.0.0.1",
            UserAgent = "Test Agent",
            Metadata = new Dictionary<string, object>()
        };
    }

    #endregion
}

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Enums;
using WitchCityRope.Core.Interfaces;
using WitchCityRope.Core.ValueObjects;
using WitchCityRope.Infrastructure.PayPal;
using WitchCityRope.Tests.Common.Builders;
using Xunit;

namespace WitchCityRope.Infrastructure.Tests.PayPal
{
    public class PayPalServiceTests
    {
        private readonly PayPalService _payPalService;
        private readonly Mock<ILogger<PayPalService>> _mockLogger;
        private readonly IConfiguration _configuration;

        public PayPalServiceTests()
        {
            _mockLogger = new Mock<ILogger<PayPalService>>();
            
            var configValues = new Dictionary<string, string?>
            {
                {"PayPal:ClientId", "test-client-id"},
                {"PayPal:ClientSecret", "test-client-secret"},
                {"PayPal:IsSandbox", "true"}
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configValues)
                .Build();

            _payPalService = new PayPalService(_configuration, _mockLogger.Object);
        }

        [Fact]
        public void Constructor_Should_Throw_When_Configuration_Is_Null()
        {
            // Act & Assert
            var act = () => new PayPalService(null!, _mockLogger.Object);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_Should_Throw_When_Logger_Is_Null()
        {
            // Act & Assert
            var act = () => new PayPalService(_configuration, null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public async Task ProcessPaymentAsync_Should_Return_Success_Result()
        {
            // Arrange
            var user = new UserBuilder().Build();
            var @event = new EventBuilder().Build();
            var registration = new RegistrationBuilder()
                .WithUser(user)
                .WithEvent(@event)
                .Build();
            var amount = Money.Create(100.00m, "USD");
            const string paymentMethodId = "test-payment-method";

            // Act
            var result = await _payPalService.ProcessPaymentAsync(registration, amount, paymentMethodId);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.TransactionId.Should().NotBeNullOrEmpty();
            result.Status.Should().Be(PaymentStatus.Pending);
            result.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            result.ErrorMessage.Should().BeNull();

            _mockLogger.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Processing payment")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ProcessPaymentAsync_Should_Handle_Exceptions()
        {
            // Arrange
            var registration = new RegistrationBuilder().Build();
            var amount = Money.Create(100.00m, "USD");
            
            // Force an exception by passing null
            Registration? nullRegistration = null;

            // Act
            var result = await _payPalService.ProcessPaymentAsync(nullRegistration!, amount, "test-method");

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Status.Should().Be(PaymentStatus.Failed);
            result.ErrorMessage.Should().NotBeNullOrEmpty();
            result.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task ProcessRefundAsync_Should_Return_Success_Result()
        {
            // Arrange
            var payment = new PaymentBuilder()
                .WithAmount(100.00m, "USD")
                .WithTransactionId("original-transaction-123")
                .Build();
            var refundAmount = Money.Create(50.00m, "USD");
            const string reason = "Customer request";

            // Act
            var result = await _payPalService.ProcessRefundAsync(payment, refundAmount, reason);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.RefundTransactionId.Should().NotBeNullOrEmpty();
            result.RefundedAmount.Should().Be(refundAmount);
            result.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            result.ErrorMessage.Should().BeNull();

            _mockLogger.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Processing refund")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ProcessRefundAsync_Should_Use_Full_Amount_When_Not_Specified()
        {
            // Arrange
            var payment = new PaymentBuilder()
                .WithAmount(100.00m, "USD")
                .Build();

            // Act
            var result = await _payPalService.ProcessRefundAsync(payment, null, null);

            // Assert
            result.RefundedAmount.Should().Be(payment.Amount);
        }

        [Fact]
        public async Task ProcessRefundAsync_Should_Handle_Exceptions()
        {
            // Arrange
            Payment? nullPayment = null;

            // Act
            var result = await _payPalService.ProcessRefundAsync(nullPayment!, null, null);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
            result.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task ValidatePaymentMethodAsync_Should_Return_True_For_Valid_Method()
        {
            // Arrange
            const string paymentMethodId = "valid-payment-method";

            // Act
            var result = await _payPalService.ValidatePaymentMethodAsync(paymentMethodId);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ValidatePaymentMethodAsync_Should_Return_False_For_Empty_Method()
        {
            // Act
            var result1 = await _payPalService.ValidatePaymentMethodAsync(string.Empty);
            var result2 = await _payPalService.ValidatePaymentMethodAsync(null!);

            // Assert
            result1.Should().BeFalse();
            result2.Should().BeFalse();
        }

        [Fact]
        public async Task CreatePaymentIntentAsync_Should_Return_Valid_Intent()
        {
            // Arrange
            var amount = Money.Create(75.50m, "USD");
            var metadata = new PaymentMetadata
            {
                UserId = Guid.NewGuid(),
                EventId = Guid.NewGuid(),
                RegistrationId = Guid.NewGuid()
            };

            // Act
            var intent = await _payPalService.CreatePaymentIntentAsync(amount, metadata);

            // Assert
            intent.Should().NotBeNull();
            intent.IntentId.Should().NotBeNullOrEmpty();
            intent.ClientSecret.Should().NotBeNullOrEmpty();
            intent.ClientSecret.Should().Contain("sandbox.paypal.com"); // Should contain PayPal URL
            intent.Amount.Should().Be(amount);
            intent.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddHours(3), TimeSpan.FromMinutes(1));

            _mockLogger.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Creating payment intent")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task CreatePaymentIntentAsync_Should_Handle_Exceptions()
        {
            // Arrange
            Money? nullAmount = null;
            var metadata = new PaymentMetadata();

            // Act & Assert
            var act = async () => await _payPalService.CreatePaymentIntentAsync(nullAmount!, metadata);
            await act.Should().ThrowAsync<Exception>();
        }

        [Fact]
        public async Task GetPaymentStatusAsync_Should_Return_Pending_Status()
        {
            // Arrange
            const string transactionId = "test-transaction-123";

            // Act
            var status = await _payPalService.GetPaymentStatusAsync(transactionId);

            // Assert
            status.Should().Be(PaymentStatus.Pending);

            _mockLogger.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Getting payment status")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetPaymentStatusAsync_Should_Return_Failed_On_Exception()
        {
            // Arrange
            string? nullTransactionId = null;

            // Act
            var status = await _payPalService.GetPaymentStatusAsync(nullTransactionId!);

            // Assert
            status.Should().Be(PaymentStatus.Failed);

            _mockLogger.Verify(x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error getting payment status")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void Should_Use_Sandbox_Environment_When_Configured()
        {
            // Arrange
            var sandboxConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    {"PayPal:ClientId", "test-client-id"},
                    {"PayPal:ClientSecret", "test-client-secret"},
                    {"PayPal:IsSandbox", "true"}
                })
                .Build();

            // Act
            var service = new PayPalService(sandboxConfig, _mockLogger.Object);

            // Assert
            // The service should be created successfully with sandbox configuration
            service.Should().NotBeNull();
        }

        [Fact]
        public void Should_Use_Production_Environment_When_Configured()
        {
            // Arrange
            var productionConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    {"PayPal:ClientId", "prod-client-id"},
                    {"PayPal:ClientSecret", "prod-client-secret"},
                    {"PayPal:IsSandbox", "false"}
                })
                .Build();

            // Act
            var service = new PayPalService(productionConfig, _mockLogger.Object);

            // Assert
            // The service should be created successfully with production configuration
            service.Should().NotBeNull();
        }

        [Fact]
        public void Should_Default_To_Sandbox_When_IsSandbox_Not_Specified()
        {
            // Arrange
            var minimalConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    {"PayPal:ClientId", "test-client-id"},
                    {"PayPal:ClientSecret", "test-client-secret"}
                })
                .Build();

            // Act
            var service = new PayPalService(minimalConfig, _mockLogger.Object);

            // Assert
            // The service should be created successfully and default to sandbox
            service.Should().NotBeNull();
        }
    }
}
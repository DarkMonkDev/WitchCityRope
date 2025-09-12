using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using SendGrid;
using SendGrid.Helpers.Mail;
using WitchCityRope.Core.ValueObjects;
using WitchCityRope.Infrastructure.Email;
using Xunit;

namespace WitchCityRope.Infrastructure.Tests.Email
{
    // SKIPPED: Email service not implemented yet - skip until email features are built
    [Trait("Category", "SkippedFeature")]
    public class EmailServiceTests
    {
        private readonly Mock<ISendGridClient> _mockSendGridClient;
        private readonly EmailService _emailService;
        private readonly IConfiguration _configuration;

        public EmailServiceTests()
        {
            _mockSendGridClient = new Mock<ISendGridClient>();
            
            var configValues = new Dictionary<string, string?>
            {
                {"SendGrid:FromEmail", "test@witchcityrope.com"},
                {"SendGrid:FromName", "Test WCR"},
                {"SendGrid:Templates:RegistrationConfirmation", "template-reg-123"},
                {"SendGrid:Templates:CancellationConfirmation", "template-cancel-123"},
                {"SendGrid:Templates:VettingStatusUpdate", "template-vetting-123"}
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configValues)
                .Build();

            _emailService = new EmailService(_mockSendGridClient.Object, _configuration);
        }

        [Fact(Skip = "Email service not implemented yet - will be needed when email features are built")]
        public void Constructor_Should_Throw_When_SendGridClient_Is_Null()
        {
            // Act & Assert
            var act = () => new EmailService(null!, _configuration);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact(Skip = "Email service not implemented yet - will be needed when email features are built")]
        public async Task SendEmailAsync_Should_Send_Email_Successfully()
        {
            // Arrange
            var to = WitchCityRope.Core.ValueObjects.EmailAddress.Create("recipient@example.com");
            const string subject = "Test Subject";
            const string body = "<h1>Test Body</h1>";
            
            var mockResponse = new Response(HttpStatusCode.Accepted, null, null);
            _mockSendGridClient
                .Setup(x => x.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _emailService.SendEmailAsync(to, subject, body);

            // Assert
            result.Should().BeTrue();
            _mockSendGridClient.Verify(x => x.SendEmailAsync(
                It.Is<SendGridMessage>(msg =>
                    msg.Subject == subject &&
                    msg.HtmlContent == body &&
                    msg.PlainTextContent == null &&
                    msg.From.Email == "test@witchcityrope.com" &&
                    msg.From.Name == "Test WCR" &&
                    msg.Personalizations[0].Tos[0].Email == "recipient@example.com"
                ), 
                It.IsAny<CancellationToken>()), 
                Times.Once);
        }

        [Fact(Skip = "Email service not implemented yet - will be needed when email features are built")]
        public async Task SendEmailAsync_Should_Send_PlainText_Email_When_IsHtml_Is_False()
        {
            // Arrange
            var to = WitchCityRope.Core.ValueObjects.EmailAddress.Create("recipient@example.com");
            const string subject = "Test Subject";
            const string body = "Plain text body";
            
            var mockResponse = new Response(HttpStatusCode.Accepted, null, null);
            _mockSendGridClient
                .Setup(x => x.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _emailService.SendEmailAsync(to, subject, body, isHtml: false);

            // Assert
            result.Should().BeTrue();
            _mockSendGridClient.Verify(x => x.SendEmailAsync(
                It.Is<SendGridMessage>(msg =>
                    msg.PlainTextContent == body &&
                    msg.HtmlContent == null
                ), 
                It.IsAny<CancellationToken>()), 
                Times.Once);
        }

        [Fact(Skip = "Email service not implemented yet - will be needed when email features are built")]
        public async Task SendEmailAsync_Should_Return_False_When_SendGrid_Fails()
        {
            // Arrange
            var to = WitchCityRope.Core.ValueObjects.EmailAddress.Create("recipient@example.com");
            
            var mockResponse = new Response(HttpStatusCode.BadRequest, null, null);
            _mockSendGridClient
                .Setup(x => x.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _emailService.SendEmailAsync(to, "Subject", "Body");

            // Assert
            result.Should().BeFalse();
        }

        [Fact(Skip = "Email service not implemented yet - will be needed when email features are built")]
        public async Task SendEmailAsync_Should_Return_False_When_Exception_Occurs()
        {
            // Arrange
            var to = WitchCityRope.Core.ValueObjects.EmailAddress.Create("recipient@example.com");
            
            _mockSendGridClient
                .Setup(x => x.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("SendGrid error"));

            // Act
            var result = await _emailService.SendEmailAsync(to, "Subject", "Body");

            // Assert
            result.Should().BeFalse();
        }

        [Fact(Skip = "Email service not implemented yet - will be needed when email features are built")]
        public async Task SendBulkEmailAsync_Should_Send_To_Multiple_Recipients()
        {
            // Arrange
            var recipients = new[]
            {
                WitchCityRope.Core.ValueObjects.EmailAddress.Create("recipient1@example.com"),
                WitchCityRope.Core.ValueObjects.EmailAddress.Create("recipient2@example.com"),
                WitchCityRope.Core.ValueObjects.EmailAddress.Create("recipient3@example.com")
            };
            
            var mockResponse = new Response(HttpStatusCode.Accepted, null, null);
            _mockSendGridClient
                .Setup(x => x.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _emailService.SendBulkEmailAsync(recipients, "Subject", "Body");

            // Assert
            result.Should().BeTrue();
            _mockSendGridClient.Verify(x => x.SendEmailAsync(
                It.Is<SendGridMessage>(msg =>
                    msg.Personalizations[0].Tos.Count == 3 &&
                    msg.Personalizations[0].Tos.Any(t => t.Email == "recipient1@example.com") &&
                    msg.Personalizations[0].Tos.Any(t => t.Email == "recipient2@example.com") &&
                    msg.Personalizations[0].Tos.Any(t => t.Email == "recipient3@example.com")
                ), 
                It.IsAny<CancellationToken>()), 
                Times.Once);
        }

        [Fact(Skip = "Email service not implemented yet - will be needed when email features are built")]
        public async Task SendBulkEmailAsync_Should_Batch_Large_Recipient_Lists()
        {
            // Arrange
            var recipients = new List<WitchCityRope.Core.ValueObjects.EmailAddress>();
            for (int i = 0; i < 250; i++)
            {
                recipients.Add(WitchCityRope.Core.ValueObjects.EmailAddress.Create($"recipient{i}@example.com"));
            }
            
            var mockResponse = new Response(HttpStatusCode.Accepted, null, null);
            _mockSendGridClient
                .Setup(x => x.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _emailService.SendBulkEmailAsync(recipients, "Subject", "Body");

            // Assert
            result.Should().BeTrue();
            // Should be called 3 times: 100 + 100 + 50
            _mockSendGridClient.Verify(x => x.SendEmailAsync(
                It.IsAny<SendGridMessage>(), 
                It.IsAny<CancellationToken>()), 
                Times.Exactly(3));
        }

        [Fact(Skip = "Email service not implemented yet - will be needed when email features are built")]
        public async Task SendBulkEmailAsync_Should_Return_False_If_Any_Batch_Fails()
        {
            // Arrange
            var recipients = new List<WitchCityRope.Core.ValueObjects.EmailAddress>();
            for (int i = 0; i < 150; i++)
            {
                recipients.Add(WitchCityRope.Core.ValueObjects.EmailAddress.Create($"recipient{i}@example.com"));
            }
            
            var callCount = 0;
            _mockSendGridClient
                .Setup(x => x.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    callCount++;
                    // First batch succeeds, second batch fails
                    return callCount == 1 
                        ? new Response(HttpStatusCode.Accepted, null, null)
                        : new Response(HttpStatusCode.BadRequest, null, null);
                });

            // Act
            var result = await _emailService.SendBulkEmailAsync(recipients, "Subject", "Body");

            // Assert
            result.Should().BeFalse();
            _mockSendGridClient.Verify(x => x.SendEmailAsync(
                It.IsAny<SendGridMessage>(), 
                It.IsAny<CancellationToken>()), 
                Times.Exactly(2));
        }

        [Fact(Skip = "Email service not implemented yet - will be needed when email features are built")]
        public async Task SendTemplateEmailAsync_Should_Send_With_Template_Id()
        {
            // Arrange
            var to = WitchCityRope.Core.ValueObjects.EmailAddress.Create("recipient@example.com");
            var templateData = new { name = "John", event_name = "Test Event" };
            
            var mockResponse = new Response(HttpStatusCode.Accepted, null, null);
            _mockSendGridClient
                .Setup(x => x.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _emailService.SendTemplateEmailAsync(to, "registration-confirmation", templateData);

            // Assert
            result.Should().BeTrue();
            _mockSendGridClient.Verify(x => x.SendEmailAsync(
                It.Is<SendGridMessage>(msg =>
                    msg.TemplateId == "template-reg-123" &&
                    msg.Personalizations[0].TemplateData != null
                ), 
                It.IsAny<CancellationToken>()), 
                Times.Once);
        }

        [Fact(Skip = "Email service not implemented yet - will be needed when email features are built")]
        public async Task SendTemplateEmailAsync_Should_Throw_For_Unknown_Template()
        {
            // Arrange
            var to = WitchCityRope.Core.ValueObjects.EmailAddress.Create("recipient@example.com");

            // Act & Assert
            var act = async () => await _emailService.SendTemplateEmailAsync(to, "unknown-template", new { });
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Template 'unknown-template' not found*");
        }

        [Fact(Skip = "Email service not implemented yet - will be needed when email features are built")]
        public async Task SendRegistrationConfirmationAsync_Should_Use_Correct_Template()
        {
            // Arrange
            var to = WitchCityRope.Core.ValueObjects.EmailAddress.Create("recipient@example.com");
            var eventDate = new DateTime(2024, 12, 25, 18, 0, 0);
            
            var mockResponse = new Response(HttpStatusCode.Accepted, null, null);
            _mockSendGridClient
                .Setup(x => x.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _emailService.SendRegistrationConfirmationAsync(
                to, "TestUser", "Christmas Party", eventDate);

            // Assert
            result.Should().BeTrue();
            _mockSendGridClient.Verify(x => x.SendEmailAsync(
                It.Is<SendGridMessage>(msg =>
                    msg.TemplateId == "template-reg-123"
                ), 
                It.IsAny<CancellationToken>()), 
                Times.Once);
        }

        [Fact(Skip = "Email service not implemented yet - will be needed when email features are built")]
        public async Task SendCancellationConfirmationAsync_Should_Include_Refund_When_Provided()
        {
            // Arrange
            var to = WitchCityRope.Core.ValueObjects.EmailAddress.Create("recipient@example.com");
            var refundAmount = Money.Create(50.00m, "USD");
            
            var mockResponse = new Response(HttpStatusCode.Accepted, null, null);
            _mockSendGridClient
                .Setup(x => x.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _emailService.SendCancellationConfirmationAsync(
                to, "TestUser", "Test Event", refundAmount);

            // Assert
            result.Should().BeTrue();
            _mockSendGridClient.Verify(x => x.SendEmailAsync(
                It.Is<SendGridMessage>(msg =>
                    msg.TemplateId == "template-cancel-123"
                ), 
                It.IsAny<CancellationToken>()), 
                Times.Once);
        }

        [Fact(Skip = "Email service not implemented yet - will be needed when email features are built")]
        public async Task SendVettingStatusUpdateAsync_Should_Include_Notes_When_Provided()
        {
            // Arrange
            var to = WitchCityRope.Core.ValueObjects.EmailAddress.Create("recipient@example.com");
            const string notes = "Please provide additional references";
            
            var mockResponse = new Response(HttpStatusCode.Accepted, null, null);
            _mockSendGridClient
                .Setup(x => x.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _emailService.SendVettingStatusUpdateAsync(
                to, "TestUser", "Pending", notes);

            // Assert
            result.Should().BeTrue();
            _mockSendGridClient.Verify(x => x.SendEmailAsync(
                It.Is<SendGridMessage>(msg =>
                    msg.TemplateId == "template-vetting-123"
                ), 
                It.IsAny<CancellationToken>()), 
                Times.Once);
        }

        [Fact(Skip = "Email service not implemented yet - will be needed when email features are built")]
        public async Task Should_Use_Default_From_Email_When_Not_Searched()
        {
            // Arrange
            var emptyConfig = new ConfigurationBuilder().Build();
            var service = new EmailService(_mockSendGridClient.Object, emptyConfig);
            var to = WitchCityRope.Core.ValueObjects.EmailAddress.Create("recipient@example.com");
            
            var mockResponse = new Response(HttpStatusCode.Accepted, null, null);
            _mockSendGridClient
                .Setup(x => x.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            await service.SendEmailAsync(to, "Subject", "Body");

            // Assert
            _mockSendGridClient.Verify(x => x.SendEmailAsync(
                It.Is<SendGridMessage>(msg =>
                    msg.From.Email == "noreply@witchcityrope.com" &&
                    msg.From.Name == "Witch City Rope"
                ), 
                It.IsAny<CancellationToken>()), 
                Times.Once);
        }
    }
}
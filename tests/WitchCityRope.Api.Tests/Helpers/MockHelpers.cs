using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using WitchCityRope.Api.Features.Auth.Services;
using WitchCityRope.Api.Interfaces;
using WitchCityRope.Api.Services;
using WitchCityRope.Core.Entities;
using WitchCityRope.Infrastructure.Identity;
using CoreEnums = WitchCityRope.Core.Enums;
using ApiEnums = WitchCityRope.Api.Features.Events.Models;
using WitchCityRope.Core.Interfaces;
using WitchCityRope.Core.ValueObjects;
using WitchCityRope.Infrastructure.Data;
using WitchCityRope.Infrastructure.Services;

namespace WitchCityRope.Api.Tests.Helpers;

public static class MockHelpers
{
    public static WitchCityRopeIdentityDbContext CreateInMemoryDbContext(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<WitchCityRopeIdentityDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName ?? Guid.NewGuid().ToString())
            .Options;

        return new WitchCityRopeIdentityDbContext(options);
    }

    // Removed - use CreateInMemoryDbContext instead which returns WitchCityRopeIdentityDbContext

    public static Mock<ILogger<T>> CreateLoggerMock<T>()
    {
        return new Mock<ILogger<T>>();
    }

    public static Mock<WitchCityRope.Infrastructure.Services.IUserContext> CreateUserContextMock(WitchCityRopeUser? currentUser = null)
    {
        var mock = new Mock<WitchCityRope.Infrastructure.Services.IUserContext>();
        mock.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(currentUser);
        mock.Setup(x => x.GetCurrentUserId()).Returns(currentUser?.Id);
        return mock;
    }

    public static Mock<Core.Interfaces.IPasswordHasher> CreatePasswordHasherMock(
        bool verifyPasswordResult = true,
        string hashedPassword = "hashed-password")
    {
        var mock = new Mock<Core.Interfaces.IPasswordHasher>();
        mock.Setup(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(verifyPasswordResult);
        mock.Setup(x => x.HashPassword(It.IsAny<string>()))
            .Returns(hashedPassword);
        return mock;
    }

    public static Mock<Core.Interfaces.IEmailService> CreateEmailServiceMock(bool sendResult = true)
    {
        var mock = new Mock<Core.Interfaces.IEmailService>();
        // Comment out broken method - EmailService interface has changed
        // mock.Setup(x => x.SendAsync(It.IsAny<EmailMessage>()))
        //     .ReturnsAsync(sendResult);
        return mock;
    }

    public static Mock<Core.Interfaces.IPaymentService> CreatePaymentServiceMock(
        bool paymentSuccess = true,
        decimal amountCharged = 0,
        string? errorMessage = null)
    {
        var mock = new Mock<Core.Interfaces.IPaymentService>();
        // Comment out broken method - PaymentRequest and PaymentResult have changed
        // mock.Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
        //     .ReturnsAsync(new PaymentResult
        //     {
        //         Success = paymentSuccess,
        //         AmountCharged = amountCharged,
        //         ErrorMessage = errorMessage,
        //         TransactionId = paymentSuccess ? Guid.NewGuid().ToString() : null
        //     });
        return mock;
    }

    public static Mock<INotificationService> CreateNotificationServiceMock()
    {
        var mock = new Mock<INotificationService>();
        
        mock.Setup(x => x.SendEventRegistrationConfirmationAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);

        mock.Setup(x => x.SendEventCancellationNotificationAsync(
            It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);

        mock.Setup(x => x.SendEventReminderAsync(
            It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);

        mock.Setup(x => x.SendVettingStatusUpdateAsync(
            It.IsAny<Guid>(), It.IsAny<bool>()))
            .Returns(Task.CompletedTask);

        mock.Setup(x => x.SendPasswordResetAsync(
            It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        mock.Setup(x => x.SendEmailVerificationAsync(
            It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        return mock;
    }

    public static Mock<WitchCityRope.Infrastructure.Services.ISlugGenerator> CreateSlugGeneratorMock()
    {
        var mock = new Mock<WitchCityRope.Infrastructure.Services.ISlugGenerator>();
        mock.Setup(x => x.GenerateSlug(It.IsAny<string>()))
            .Returns<string>(title => title.ToLower().Replace(" ", "-"));
        return mock;
    }

    public static WitchCityRopeUser CreateTestUser(
        Guid? id = null,
        string email = "test@example.com",
        string sceneName = "TestUser",
        CoreEnums.UserRole role = CoreEnums.UserRole.Member,
        bool isActive = true,
        bool isVetted = false)
    {
        return WitchCityRope.Tests.Common.Factories.TestUserFactory.CreateTestUser(
            id: id,
            email: email,
            sceneName: sceneName,
            role: role,
            isActive: isActive,
            isVetted: isVetted,
            emailConfirmed: true);
    }

    public static Event CreateTestEvent(
        Guid? id = null,
        Guid? organizerId = null,
        string title = "Test Event",
        CoreEnums.EventType type = CoreEnums.EventType.Workshop,
        decimal price = 0,
        int maxAttendees = 20,
        bool requiresVetting = false)
    {
        var organizer = CreateTestUser(id: organizerId, role: CoreEnums.UserRole.Organizer);
        
        var eventBuilder = new WitchCityRope.Tests.Common.Builders.EventBuilder()
            .WithTitle(title)
            .WithEventType(type)
            .WithCapacity(maxAttendees)
            .WithPrimaryOrganizer(organizer);
            
        if (price > 0)
        {
            eventBuilder.WithSinglePrice(price);
        }
        else
        {
            eventBuilder.WithFreePricing();
        }
        
        var @event = eventBuilder.Build();
        
        // If a specific ID was requested, we need to use reflection to set it
        if (id.HasValue)
        {
            var eventType = typeof(Event);
            var idProperty = eventType.GetProperty("Id", System.Reflection.BindingFlags.Public | 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            idProperty?.SetValue(@event, id.Value);
        }
        
        return @event;
    }

    public static Registration CreateTestRegistration(
        Guid? id = null,
        Guid? eventId = null,
        Guid? userId = null,
        CoreEnums.RegistrationStatus status = CoreEnums.RegistrationStatus.Confirmed)
    {
        var user = userId.HasValue 
            ? CreateTestUser(id: userId.Value) 
            : CreateTestUser();
            
        var @event = eventId.HasValue
            ? CreateTestEvent(id: eventId.Value)
            : CreateTestEvent();
            
        var registration = new WitchCityRope.Tests.Common.Builders.RegistrationBuilder()
            .WithUser(user)
            .WithEvent(@event)
            .Build();
            
        // If a specific ID was requested, we need to use reflection to set it
        if (id.HasValue)
        {
            var regType = typeof(Registration);
            var idProperty = regType.GetProperty("Id", System.Reflection.BindingFlags.Public | 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            idProperty?.SetValue(registration, id.Value);
        }
        
        // Note: Status is not directly settable on Registration entity in the current model
        // The status would be managed through business logic methods
        
        return registration;
    }

    public static Payment CreateTestPayment(
        Guid? id = null,
        Guid? userId = null,
        decimal amount = 50.00m,
        CoreEnums.PaymentStatus status = CoreEnums.PaymentStatus.Completed)
    {
        // Create a ticket for the payment (Payment requires a Ticket)
        var user = userId.HasValue 
            ? CreateTestUser(id: userId.Value) 
            : CreateTestUser();
            
        var ticket = new WitchCityRope.Tests.Common.Builders.TicketBuilder()
            .WithUser(user.Id)
            .WithSelectedPrice(amount)
            .Build();
            
        var paymentBuilder = new WitchCityRope.Tests.Common.Builders.PaymentBuilder()
            .WithTicket(ticket)
            .WithAmount(amount);
            
        // Set the payment status
        switch (status)
        {
            case CoreEnums.PaymentStatus.Completed:
                paymentBuilder.AsCompleted();
                break;
            case CoreEnums.PaymentStatus.Failed:
                paymentBuilder.AsFailed();
                break;
            case CoreEnums.PaymentStatus.Refunded:
                paymentBuilder.AsRefunded();
                break;
        }
        
        var payment = paymentBuilder.Build();
        
        // If a specific ID was requested, we need to use reflection to set it
        if (id.HasValue)
        {
            var paymentType = typeof(Payment);
            var idProperty = paymentType.GetProperty("Id", System.Reflection.BindingFlags.Public | 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            idProperty?.SetValue(payment, id.Value);
        }
        
        return payment;
    }

    // VettingApplication creation removed - entity model has changed significantly
    // The VettingApplication now requires an IUser and uses domain-driven constructors
    // Tests should use the VettingApplicationBuilder from WitchCityRope.Tests.Common instead

    private static string GenerateConfirmationCode()
    {
        var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = new Random();
        var code = new char[8];

        for (int i = 0; i < code.Length; i++)
        {
            code[i] = chars[random.Next(chars.Length)];
        }

        return new string(code);
    }
}

// Additional test data builders
public class TestDataBuilder
{
    private readonly WitchCityRopeIdentityDbContext _context;

    public TestDataBuilder(WitchCityRopeIdentityDbContext context)
    {
        _context = context;
    }

    public async Task<WitchCityRopeUser> CreateUserAsync(
        string email = "test@example.com",
        string sceneName = "TestUser",
        CoreEnums.UserRole role = CoreEnums.UserRole.Member,
        bool isVetted = false)
    {
        var user = MockHelpers.CreateTestUser(
            email: email,
            sceneName: sceneName,
            role: role,
            isVetted: isVetted);

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<Event> CreateEventAsync(
        Guid organizerId,
        string title = "Test Event",
        CoreEnums.EventType type = CoreEnums.EventType.Workshop,
        bool requiresVetting = false)
    {
        var @event = MockHelpers.CreateTestEvent(
            organizerId: organizerId,
            title: title,
            type: type,
            requiresVetting: requiresVetting);

        await _context.Events.AddAsync(@event);
        await _context.SaveChangesAsync();
        return @event;
    }

    public async Task<Registration> CreateRegistrationAsync(
        Guid eventId,
        Guid userId,
        CoreEnums.RegistrationStatus status = CoreEnums.RegistrationStatus.Confirmed)
    {
        var registration = MockHelpers.CreateTestRegistration(
            eventId: eventId,
            userId: userId,
            status: status);

        await _context.Registrations.AddAsync(registration);
        await _context.SaveChangesAsync();
        return registration;
    }
}

// TestDataBuilderForDbContext removed - use TestDataBuilder with WitchCityRopeIdentityDbContext instead

// Note: Interface implementations have been moved to WitchCityRope.Tests.Common.Interfaces
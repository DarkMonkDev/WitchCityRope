using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using WitchCityRope.Api.Features.Auth.Services;
using WitchCityRope.Api.Interfaces;
using WitchCityRope.Api.Services;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Enums;
using WitchCityRope.Core.Interfaces;
using WitchCityRope.Core.ValueObjects;
using WitchCityRope.Infrastructure.Data;
using WitchCityRope.Infrastructure.Services;

namespace WitchCityRope.Api.Tests.Helpers;

public static class MockHelpers
{
    public static WitchCityRopeDbContext CreateInMemoryDbContext(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<WitchCityRopeDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName ?? Guid.NewGuid().ToString())
            .Options;

        return new WitchCityRopeDbContext(options);
    }

    public static Mock<ILogger<T>> CreateLoggerMock<T>()
    {
        return new Mock<ILogger<T>>();
    }

    public static Mock<WitchCityRope.Infrastructure.Services.IUserContext> CreateUserContextMock(User? currentUser = null)
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
        mock.Setup(x => x.SendAsync(It.IsAny<EmailMessage>()))
            .ReturnsAsync(sendResult);
        return mock;
    }

    public static Mock<Core.Interfaces.IPaymentService> CreatePaymentServiceMock(
        bool paymentSuccess = true,
        decimal amountCharged = 0,
        string? errorMessage = null)
    {
        var mock = new Mock<Core.Interfaces.IPaymentService>();
        mock.Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
            .ReturnsAsync(new PaymentResult
            {
                Success = paymentSuccess,
                AmountCharged = amountCharged,
                ErrorMessage = errorMessage,
                TransactionId = paymentSuccess ? Guid.NewGuid().ToString() : null
            });
        return mock;
    }

    public static Mock<INotificationService> CreateNotificationServiceMock()
    {
        var mock = new Mock<INotificationService>();
        
        mock.Setup(x => x.SendEventRegistrationConfirmationAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<RegistrationStatus>(),
            It.IsAny<int?>()))
            .Returns(Task.CompletedTask);

        mock.Setup(x => x.SendEventCancellationAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        mock.Setup(x => x.SendEventReminderAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<DateTime>(), It.IsAny<string>()))
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

    public static User CreateTestUser(
        Guid? id = null,
        string email = "test@example.com",
        string sceneName = "TestUser",
        UserRole role = UserRole.Member,
        bool isActive = true,
        bool isVetted = false)
    {
        return new User
        {
            Id = id ?? Guid.NewGuid(),
            EncryptedLegalName = "encrypted-legal-name",
            SceneName = new SceneName(sceneName),
            Email = new EmailAddress(email),
            DateOfBirth = DateTime.UtcNow.AddYears(-25),
            PasswordHash = "password-hash",
            Role = role,
            Roles = new[] { role },
            IsActive = isActive,
            IsVetted = isVetted,
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
    }

    public static Event CreateTestEvent(
        Guid? id = null,
        Guid? organizerId = null,
        string title = "Test Event",
        EventType type = EventType.Workshop,
        decimal price = 0,
        int maxAttendees = 20,
        bool requiresVetting = false)
    {
        return new Event
        {
            Id = id ?? Guid.NewGuid(),
            Title = title,
            Slug = title.ToLower().Replace(" ", "-"),
            Description = "Test event description",
            Type = type,
            StartDateTime = DateTime.UtcNow.AddDays(7),
            EndDateTime = DateTime.UtcNow.AddDays(7).AddHours(2),
            Location = "Test Location",
            MaxAttendees = maxAttendees,
            CurrentAttendees = 0,
            Price = price,
            RequiredSkillLevels = new List<string> { "All Levels" },
            Tags = new List<string> { "test", "workshop" },
            RequiresVetting = requiresVetting,
            OrganizerId = organizerId ?? Guid.NewGuid(),
            Status = EventStatus.Published,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static Registration CreateTestRegistration(
        Guid? id = null,
        Guid? eventId = null,
        Guid? userId = null,
        RegistrationStatus status = RegistrationStatus.Confirmed)
    {
        return new Registration
        {
            Id = id ?? Guid.NewGuid(),
            EventId = eventId ?? Guid.NewGuid(),
            UserId = userId ?? Guid.NewGuid(),
            Status = status,
            ConfirmationCode = GenerateConfirmationCode(),
            RegisteredAt = DateTime.UtcNow
        };
    }

    public static Payment CreateTestPayment(
        Guid? id = null,
        Guid? userId = null,
        decimal amount = 50.00m,
        PaymentStatus status = PaymentStatus.Completed)
    {
        return new Payment
        {
            Id = id ?? Guid.NewGuid(),
            UserId = userId ?? Guid.NewGuid(),
            Amount = new Money(amount, "USD"),
            Status = status,
            PaymentMethod = PaymentMethod.Stripe,
            TransactionId = Guid.NewGuid().ToString(),
            ProcessedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static VettingApplication CreateTestVettingApplication(
        Guid? id = null,
        Guid? userId = null,
        string status = "Pending")
    {
        return new VettingApplication
        {
            Id = id ?? Guid.NewGuid(),
            UserId = userId ?? Guid.NewGuid(),
            Experience = "3 years of rope experience",
            SafetyKnowledge = "Extensive safety knowledge",
            References = new List<VettingReference>
            {
                new VettingReference
                {
                    Name = "Reference One",
                    Email = "ref1@example.com",
                    Relationship = "Rope partner"
                },
                new VettingReference
                {
                    Name = "Reference Two",
                    Email = "ref2@example.com",
                    Relationship = "Event organizer"
                }
            },
            Status = status,
            SubmittedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }

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
    private readonly WitchCityRopeDbContext _context;

    public TestDataBuilder(WitchCityRopeDbContext context)
    {
        _context = context;
    }

    public async Task<User> CreateUserAsync(
        string email = "test@example.com",
        string sceneName = "TestUser",
        UserRole role = UserRole.Member,
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
        EventType type = EventType.Workshop,
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
        RegistrationStatus status = RegistrationStatus.Confirmed)
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

// Note: Interface implementations have been moved to WitchCityRope.Tests.Common.Interfaces
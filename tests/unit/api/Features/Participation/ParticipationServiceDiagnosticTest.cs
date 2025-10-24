using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using WitchCityRope.Api.Enums;
using WitchCityRope.Api.Features.Participation.Models;
using WitchCityRope.Api.Features.Participation.Services;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Tests.Fixtures;
using WitchCityRope.Api.Tests.TestBase;
using Xunit.Abstractions;

namespace WitchCityRope.Api.Tests.Features.Participation;

/// <summary>
/// Diagnostic test to understand why Participation tests are failing
/// </summary>
public class ParticipationServiceDiagnosticTest : DatabaseTestBase
{
    private ParticipationService _participationService = null!;
    private readonly Mock<ILogger<ParticipationService>> _mockLogger;
    private readonly ITestOutputHelper _output;

    public ParticipationServiceDiagnosticTest(DatabaseTestFixture databaseFixture, ITestOutputHelper output) : base(databaseFixture)
    {
        _output = output;
        _mockLogger = new Mock<ILogger<ParticipationService>>();
    }

    public override async Task InitializeAsync()
    {
        // Call base to initialize DbContext
        await base.InitializeAsync();

        // Create service AFTER DbContext is initialized
        _participationService = new ParticipationService(DbContext, _mockLogger.Object);
    }

    [Fact]
    public async Task Diagnostic_CreateRSVP_WithValidUser_ShowsActualError()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "diagnostic@test.com",
            UserName = "diagnostic@test.com",
            SceneName = "DiagnosticUser",
            VettingStatus = 3, // Approved (vetted)
            IsActive = true,
            Role = "Member",
            EncryptedLegalName = "Diagnostic User",
            DateOfBirth = DateTime.UtcNow.AddYears(-25),
            PronouncedName = "Diagnostic",
            Pronouns = "they/them",
            EmailVerificationToken = string.Empty,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var socialEvent = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Diagnostic Event",
            Description = "Test event",
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(2),
            Capacity = 10,
            EventType = EventType.Social,
            Location = "Test Location",
            IsPublished = true,
            PricingTiers = "{}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await DbContext.Users.AddAsync(user);
        await DbContext.Events.AddAsync(socialEvent);
        await DbContext.SaveChangesAsync();

        _output.WriteLine($"Created user: {user.Id}, IsVetted: {user.IsVetted}, VettingStatus: {user.VettingStatus}");
        _output.WriteLine($"Created event: {socialEvent.Id}, Type: {socialEvent.EventType}");

        var request = new CreateRSVPRequest
        {
            EventId = socialEvent.Id,
            Notes = "Diagnostic RSVP"
        };

        // Act
        Console.WriteLine($"About to call CreateRSVPAsync with request.EventId={request.EventId}, userId={user.Id}");
        var result = await _participationService.CreateRSVPAsync(request, user.Id);
        Console.WriteLine($"CreateRSVPAsync returned. IsSuccess={result.IsSuccess}");
        if (!result.IsSuccess && result.Details != null)
        {
            Console.WriteLine($"Full exception details: {result.Details}");
        }

        // Assert - Output all details
        _output.WriteLine($"Result IsSuccess: {result.IsSuccess}");
        _output.WriteLine($"Result Error: {result.Error}");
        _output.WriteLine($"Result Details: {result.Details}");

        if (result.Value != null)
        {
            _output.WriteLine($"Result Value: EventId={result.Value.EventId}, UserId={result.Value.UserId}");
        }

        if (!result.IsSuccess)
        {
            Console.WriteLine($"FAILURE ANALYSIS:");
            Console.WriteLine($"  Error: {result.Error}");
            Console.WriteLine($"  Details: {result.Details}");
            Console.WriteLine($"  User.IsVetted: {user.IsVetted}");
            Console.WriteLine($"  Event.EventType: {socialEvent.EventType}");
        }

        Assert.True(result.IsSuccess, $"Expected success but got failure. Error: {result.Error}, Details: {result.Details}");
    }
}

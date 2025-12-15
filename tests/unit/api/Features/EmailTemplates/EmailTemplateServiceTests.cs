using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.EmailTemplates.Entities;
using WitchCityRope.Api.Features.EmailTemplates.Models;
using WitchCityRope.Api.Features.EmailTemplates.Services;
using WitchCityRope.Api.Features.Shared.Services;
using WitchCityRope.Api.Models;
using Xunit;
using Result = WitchCityRope.Api.Features.Shared.Models.Result;

namespace WitchCityRope.Api.Tests.Services;

/// <summary>
/// Unit tests for EmailTemplateService focusing on post-import email workflow functionality
/// Tests NewImportedUsers segment, per-user variable replacement, and token generation
/// </summary>
public class EmailTemplateServiceTests : IAsyncLifetime
{
    private ApplicationDbContext _context = null!;
    private UserManager<ApplicationUser> _userManager = null!;
    private IEmailService _emailService = null!;
    private IConfiguration _configuration = null!;
    private ILogger<EmailTemplateService> _logger = null!;
    private EmailTemplateService _sut = null!;
    private string _connectionString = null!;

    public async Task InitializeAsync()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        await _context.Database.EnsureCreatedAsync();

        // Setup UserManager dependencies
        var userStore = Substitute.For<IUserStore<ApplicationUser>>();
        var passwordHasher = Substitute.For<IPasswordHasher<ApplicationUser>>();
        var userValidators = new List<IUserValidator<ApplicationUser>>();
        var passwordValidators = new List<IPasswordValidator<ApplicationUser>>();
        var keyNormalizer = Substitute.For<ILookupNormalizer>();
        var errors = Substitute.For<IdentityErrorDescriber>();
        var services = Substitute.For<IServiceProvider>();
        var userLogger = Substitute.For<ILogger<UserManager<ApplicationUser>>>();

        _userManager = Substitute.ForPartsOf<UserManager<ApplicationUser>>(
            userStore, null, passwordHasher, userValidators, passwordValidators,
            keyNormalizer, errors, services, userLogger);

        // Configure UserManager token generation methods to not call base implementation
        _userManager.When(x => x.GeneratePasswordResetTokenAsync(Arg.Any<ApplicationUser>()))
            .DoNotCallBase();
        _userManager.When(x => x.GenerateEmailConfirmationTokenAsync(Arg.Any<ApplicationUser>()))
            .DoNotCallBase();

        // Setup email service mock
        _emailService = Substitute.For<IEmailService>();
        _emailService.SendEmailAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        // Setup configuration with frontend URL
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Frontend:Url"] = "https://staging.witchcityrope.com"
            }!)
            .Build();

        // Setup logger
        _logger = Substitute.For<ILogger<EmailTemplateService>>();

        // Create service instance
        _sut = new EmailTemplateService(
            _context,
            _userManager,
            _emailService,
            _configuration,
            _logger);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        _userManager?.Dispose();
    }

    #region Helper Methods

    /// <summary>
    /// Creates a test user with unique identifiers to avoid conflicts
    /// </summary>
    private async Task<ApplicationUser> CreateTestUserAsync(
        string? email = null,
        string? sceneName = "DEFAULT_GENERATE",
        int vettingStatus = 0,
        bool emailConfirmed = false,
        bool isActive = true)
    {
        // Special handling: "DEFAULT_GENERATE" means generate a unique name
        // null means actually set SceneName to null (for fallback testing)
        var actualSceneName = sceneName == "DEFAULT_GENERATE"
            ? $"TestUser-{Guid.NewGuid():N}"
            : sceneName;

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email ?? $"test-{Guid.NewGuid():N}@example.com",
            UserName = email ?? $"test-{Guid.NewGuid():N}@example.com",
            SceneName = actualSceneName,
            VettingStatus = vettingStatus,
            EmailConfirmed = emailConfirmed,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            Role = "Member"
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return user;
    }

    #endregion

    #region BuildSegmentQuery - NewImportedUsers Tests

    [Fact]
    public async Task GetSegmentPreviewAsync_NewImportedUsers_ReturnsOnlyApprovedUnconfirmedActiveUsers()
    {
        // Arrange
        // Create users with different combinations of VettingStatus, EmailConfirmed, IsActive
        var approvedUnconfirmed = await CreateTestUserAsync(
            vettingStatus: 3, // Approved
            emailConfirmed: false,
            isActive: true);

        var approvedConfirmed = await CreateTestUserAsync(
            vettingStatus: 3, // Approved
            emailConfirmed: true, // Should NOT be included
            isActive: true);

        var underReviewUnconfirmed = await CreateTestUserAsync(
            vettingStatus: 0, // UnderReview - Should NOT be included
            emailConfirmed: false,
            isActive: true);

        var approvedUnconfirmedInactive = await CreateTestUserAsync(
            vettingStatus: 3, // Approved
            emailConfirmed: false,
            isActive: false); // Should NOT be included

        // Act
        var result = await _sut.GetSegmentPreviewAsync(
            UserSegment.NewImportedUsers,
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Count.Should().Be(1);
        result.Value.First().Email.Should().Be(approvedUnconfirmed.Email);
    }

    [Fact]
    public async Task GetSegmentPreviewAsync_NewImportedUsers_ExcludesConfirmedUsers()
    {
        // Arrange
        var user1 = await CreateTestUserAsync(
            vettingStatus: 3, // Approved
            emailConfirmed: true, // Confirmed - should be excluded
            isActive: true);

        var user2 = await CreateTestUserAsync(
            vettingStatus: 3, // Approved
            emailConfirmed: false, // Not confirmed - should be included
            isActive: true);

        // Act
        var result = await _sut.GetSegmentPreviewAsync(
            UserSegment.NewImportedUsers,
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Should().NotContain(u => u.Email == user1.Email);
        result.Value.Should().Contain(u => u.Email == user2.Email);
    }

    [Fact]
    public async Task GetSegmentPreviewAsync_NewImportedUsers_ExcludesNonApprovedVettingStatus()
    {
        // Arrange - Create users with different vetting statuses
        var underReview = await CreateTestUserAsync(
            vettingStatus: 0, // UnderReview
            emailConfirmed: false,
            isActive: true);

        var interviewApproved = await CreateTestUserAsync(
            vettingStatus: 1, // InterviewApproved
            emailConfirmed: false,
            isActive: true);

        var finalReview = await CreateTestUserAsync(
            vettingStatus: 2, // FinalReview
            emailConfirmed: false,
            isActive: true);

        var approved = await CreateTestUserAsync(
            vettingStatus: 3, // Approved - ONLY this should be included
            emailConfirmed: false,
            isActive: true);

        // Act
        var result = await _sut.GetSegmentPreviewAsync(
            UserSegment.NewImportedUsers,
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Count.Should().Be(1);
        result.Value.First().Email.Should().Be(approved.Email);
    }

    [Fact]
    public async Task GetSegmentPreviewAsync_NewImportedUsers_ExcludesInactiveUsers()
    {
        // Arrange
        var activeUser = await CreateTestUserAsync(
            vettingStatus: 3, // Approved
            emailConfirmed: false,
            isActive: true); // Active

        var inactiveUser = await CreateTestUserAsync(
            vettingStatus: 3, // Approved
            emailConfirmed: false,
            isActive: false); // Inactive - should be excluded

        // Act
        var result = await _sut.GetSegmentPreviewAsync(
            UserSegment.NewImportedUsers,
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Count.Should().Be(1);
        result.Value.First().Email.Should().Be(activeUser.Email);
    }

    #endregion

    #region ReplaceVariablesForUserAsync Tests

    [Fact]
    public async Task SendAdHocEmailAsync_WithUserNameVariable_ReplacesWithSceneName()
    {
        // Arrange
        var user = await CreateTestUserAsync(
            sceneName: "TestSceneName",
            vettingStatus: 3,
            emailConfirmed: false,
            isActive: true);

        var request = new SendAdHocEmailRequest
        {
            Subject = "Welcome",
            HtmlBody = "Hello {{user_name}}!",
            PlainTextBody = "Hello {{user_name}}!",
            Segment = UserSegment.NewImportedUsers,
            RecipientGroup = "NewImportedUsers"
        };

        // Act
        var result = await _sut.SendAdHocEmailAsync(
            request,
            Guid.NewGuid(),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify email was sent with scene name
        await _emailService.Received(1).SendEmailAsync(
            user.Email!,
            "Welcome",
            Arg.Is<string>(html => html.Contains("Hello TestSceneName!")),
            Arg.Is<string?>(text => text != null && text.Contains("Hello TestSceneName!")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAdHocEmailAsync_WithUserNameVariable_FallsBackToEmailWhenSceneNameEmpty()
    {
        // Arrange - SceneName is non-nullable string, so use empty string to test fallback
        var user = await CreateTestUserAsync(
            email: "test@example.com",
            sceneName: "", // Empty scene name - should fall back to email
            vettingStatus: 3,
            emailConfirmed: false,
            isActive: true);

        var request = new SendAdHocEmailRequest
        {
            Subject = "Welcome",
            HtmlBody = "Hello {{user_name}}!",
            PlainTextBody = "Hello {{user_name}}!",
            Segment = UserSegment.NewImportedUsers,
            RecipientGroup = "NewImportedUsers"
        };

        // Act
        var result = await _sut.SendAdHocEmailAsync(
            request,
            Guid.NewGuid(),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify email was sent with email fallback
        await _emailService.Received(1).SendEmailAsync(
            user.Email!,
            "Welcome",
            Arg.Is<string>(html => html.Contains("Hello test@example.com!")),
            Arg.Is<string?>(text => text != null && text.Contains("Hello test@example.com!")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAdHocEmailAsync_WithResetUrlVariable_GeneratesUniqueToken()
    {
        // Arrange
        var user = await CreateTestUserAsync(
            vettingStatus: 3,
            emailConfirmed: false,
            isActive: true);

        // Configure UserManager to return a unique token for any user
        // Note: Service queries DB and gets a different instance, so use Arg.Any
        var expectedToken = "unique-reset-token-123";
        _userManager.GeneratePasswordResetTokenAsync(Arg.Any<ApplicationUser>())
            .Returns(Task.FromResult(expectedToken));

        var request = new SendAdHocEmailRequest
        {
            Subject = "Reset Password",
            HtmlBody = "Reset link: {{reset_url}}",
            PlainTextBody = "Reset link: {{reset_url}}",
            Segment = UserSegment.NewImportedUsers,
            RecipientGroup = "NewImportedUsers"
        };

        // Act
        var result = await _sut.SendAdHocEmailAsync(
            request,
            Guid.NewGuid(),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify token generation was called for a user with matching email
        await _userManager.Received(1).GeneratePasswordResetTokenAsync(
            Arg.Is<ApplicationUser>(u => u.Email == user.Email));

        // Verify email contains correct URL structure
        await _emailService.Received(1).SendEmailAsync(
            user.Email!,
            "Reset Password",
            Arg.Is<string>(html =>
                html.Contains("https://staging.witchcityrope.com/reset-password?") &&
                html.Contains($"userId={user.Id}") &&
                html.Contains("token=")),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAdHocEmailAsync_WithResetUrlVariable_GeneratesUniqueTokensPerUser()
    {
        // Arrange
        var user1 = await CreateTestUserAsync(
            vettingStatus: 3,
            emailConfirmed: false,
            isActive: true);

        var user2 = await CreateTestUserAsync(
            vettingStatus: 3,
            emailConfirmed: false,
            isActive: true);

        // Configure token generation for any user
        // Note: Service queries DB and gets different instances, so use Arg.Any
        _userManager.GeneratePasswordResetTokenAsync(Arg.Any<ApplicationUser>())
            .Returns(Task.FromResult("unique-token"));

        var request = new SendAdHocEmailRequest
        {
            Subject = "Reset Password",
            HtmlBody = "Reset link: {{reset_url}}",
            PlainTextBody = "Reset link: {{reset_url}}",
            Segment = UserSegment.NewImportedUsers,
            RecipientGroup = "NewImportedUsers"
        };

        // Act
        var result = await _sut.SendAdHocEmailAsync(
            request,
            Guid.NewGuid(),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify each user got their own token generated (by email match)
        await _userManager.Received(1).GeneratePasswordResetTokenAsync(
            Arg.Is<ApplicationUser>(u => u.Email == user1.Email));
        await _userManager.Received(1).GeneratePasswordResetTokenAsync(
            Arg.Is<ApplicationUser>(u => u.Email == user2.Email));

        // Verify 2 separate emails were sent
        await _emailService.Received(2).SendEmailAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAdHocEmailAsync_WithVerificationUrlVariable_GeneratesEmailConfirmationToken()
    {
        // Arrange
        var user = await CreateTestUserAsync(
            vettingStatus: 3,
            emailConfirmed: false,
            isActive: true);

        // Configure UserManager to return a token for any user
        // Note: Service queries DB and gets a different instance, so use Arg.Any
        var expectedToken = "email-confirmation-token-456";
        _userManager.GenerateEmailConfirmationTokenAsync(Arg.Any<ApplicationUser>())
            .Returns(Task.FromResult(expectedToken));

        var request = new SendAdHocEmailRequest
        {
            Subject = "Verify Email",
            HtmlBody = "Verification link: {{verification_url}}",
            PlainTextBody = "Verification link: {{verification_url}}",
            Segment = UserSegment.NewImportedUsers,
            RecipientGroup = "NewImportedUsers"
        };

        // Act
        var result = await _sut.SendAdHocEmailAsync(
            request,
            Guid.NewGuid(),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify email confirmation token was generated for a user with matching email
        await _userManager.Received(1).GenerateEmailConfirmationTokenAsync(
            Arg.Is<ApplicationUser>(u => u.Email == user.Email));

        // Verify email contains verification URL
        await _emailService.Received(1).SendEmailAsync(
            user.Email!,
            "Verify Email",
            Arg.Is<string>(html =>
                html.Contains("https://staging.witchcityrope.com/verify-email?") &&
                html.Contains($"userId={user.Id}") &&
                html.Contains("token=")),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAdHocEmailAsync_WithSystemUrlVariable_ReplacesWithFrontendUrl()
    {
        // Arrange
        var user = await CreateTestUserAsync(
            vettingStatus: 3,
            emailConfirmed: false,
            isActive: true);

        var request = new SendAdHocEmailRequest
        {
            Subject = "Welcome",
            HtmlBody = "Visit us at {{system_url}}",
            PlainTextBody = "Visit us at {{system_url}}",
            Segment = UserSegment.NewImportedUsers,
            RecipientGroup = "NewImportedUsers"
        };

        // Act
        var result = await _sut.SendAdHocEmailAsync(
            request,
            Guid.NewGuid(),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        await _emailService.Received(1).SendEmailAsync(
            user.Email!,
            "Welcome",
            Arg.Is<string>(html => html.Contains("Visit us at https://staging.witchcityrope.com")),
            Arg.Is<string?>(text => text != null && text.Contains("Visit us at https://staging.witchcityrope.com")),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region SendAdHocEmailAsync - Per-User Variables Tests

    [Fact]
    public async Task SendAdHocEmailAsync_WithPerUserVariables_SendsIndividualEmails()
    {
        // Arrange
        var user1 = await CreateTestUserAsync(
            vettingStatus: 3,
            emailConfirmed: false,
            isActive: true);

        var user2 = await CreateTestUserAsync(
            vettingStatus: 3,
            emailConfirmed: false,
            isActive: true);

        // Configure tokens for each user
        _userManager.GeneratePasswordResetTokenAsync(user1)
            .Returns(Task.FromResult("token1"));
        _userManager.GeneratePasswordResetTokenAsync(user2)
            .Returns(Task.FromResult("token2"));

        var request = new SendAdHocEmailRequest
        {
            Subject = "Password Reset",
            HtmlBody = "Hello {{user_name}}, reset your password: {{reset_url}}",
            PlainTextBody = "Hello {{user_name}}, reset your password: {{reset_url}}",
            Segment = UserSegment.NewImportedUsers,
            RecipientGroup = "NewImportedUsers"
        };

        // Act
        var result = await _sut.SendAdHocEmailAsync(
            request,
            Guid.NewGuid(),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify individual emails were sent (not bulk)
        await _emailService.Received(2).SendEmailAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());

        // Verify each user received personalized content
        await _emailService.Received(1).SendEmailAsync(
            user1.Email!,
            Arg.Any<string>(),
            Arg.Is<string>(html => html.Contains(user1.SceneName)),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());

        await _emailService.Received(1).SendEmailAsync(
            user2.Email!,
            Arg.Any<string>(),
            Arg.Is<string>(html => html.Contains(user2.SceneName)),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAdHocEmailAsync_WithoutPerUserVariables_SendsBulkEmails()
    {
        // Arrange
        var user1 = await CreateTestUserAsync(
            vettingStatus: 3,
            emailConfirmed: false,
            isActive: true);

        var user2 = await CreateTestUserAsync(
            vettingStatus: 3,
            emailConfirmed: false,
            isActive: true);

        // Template has NO per-user variables (no {{reset_url}}, {{verification_url}}, or {{user_name}})
        var request = new SendAdHocEmailRequest
        {
            Subject = "System Announcement",
            HtmlBody = "This is a general announcement to all users.",
            PlainTextBody = "This is a general announcement to all users.",
            Segment = UserSegment.NewImportedUsers,
            RecipientGroup = "NewImportedUsers"
        };

        // Act
        var result = await _sut.SendAdHocEmailAsync(
            request,
            Guid.NewGuid(),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify bulk emails were sent (same content, different recipients)
        await _emailService.Received(2).SendEmailAsync(
            Arg.Any<string>(),
            "System Announcement",
            "This is a general announcement to all users.",
            "This is a general announcement to all users.",
            Arg.Any<CancellationToken>());

        // Verify NO token generation occurred
        await _userManager.DidNotReceive().GeneratePasswordResetTokenAsync(Arg.Any<ApplicationUser>());
        await _userManager.DidNotReceive().GenerateEmailConfirmationTokenAsync(Arg.Any<ApplicationUser>());
    }

    [Fact]
    public async Task SendAdHocEmailAsync_WithMultiplePerUserVariables_ReplacesAllVariables()
    {
        // Arrange
        var user = await CreateTestUserAsync(
            email: "test@example.com",
            sceneName: "TestUser",
            vettingStatus: 3,
            emailConfirmed: false,
            isActive: true);

        _userManager.GeneratePasswordResetTokenAsync(user)
            .Returns(Task.FromResult("reset-token"));
        _userManager.GenerateEmailConfirmationTokenAsync(user)
            .Returns(Task.FromResult("verify-token"));

        var request = new SendAdHocEmailRequest
        {
            Subject = "Complete Setup",
            HtmlBody = "Hello {{user_name}}, reset: {{reset_url}}, verify: {{verification_url}}, visit: {{system_url}}",
            PlainTextBody = "Hello {{user_name}}, reset: {{reset_url}}, verify: {{verification_url}}, visit: {{system_url}}",
            Segment = UserSegment.NewImportedUsers,
            RecipientGroup = "NewImportedUsers"
        };

        // Act
        var result = await _sut.SendAdHocEmailAsync(
            request,
            Guid.NewGuid(),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify all variables were replaced
        await _emailService.Received(1).SendEmailAsync(
            user.Email!,
            "Complete Setup",
            Arg.Is<string>(html =>
                html.Contains("Hello TestUser") &&
                html.Contains("reset: https://staging.witchcityrope.com/reset-password?") &&
                html.Contains("verify: https://staging.witchcityrope.com/verify-email?") &&
                html.Contains("visit: https://staging.witchcityrope.com")),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    #endregion
}

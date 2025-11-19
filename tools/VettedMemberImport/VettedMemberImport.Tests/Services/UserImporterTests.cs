using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using VettedMemberImport.Data;
using VettedMemberImport.Entities;
using VettedMemberImport.Models;
using VettedMemberImport.Services;

namespace VettedMemberImport.Tests.Services;

/// <summary>
/// Tests for UserImporter service - core import logic with database operations
/// </summary>
public class UserImporterTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<UserImporter>> _mockLogger;
    private readonly DateParser _dateParser;
    private readonly UserImporter _sut;

    public UserImporterTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _mockLogger = new Mock<ILogger<UserImporter>>();
        _dateParser = new DateParser();

        _sut = new UserImporter(_context, _mockLogger.Object, _dateParser);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region Success Cases

    [Fact]
    public async Task ImportUsersAsync_WithValidData_CreatesUserSuccessfully()
    {
        // Arrange
        var rows = new List<CsvRow>
        {
            new CsvRow
            {
                VetteesEmail = "test@example.com",
                VetteesNickname = "TestUser",
                VetteesPronouns = "they/them",
                FlHandles = "fetlife123",
                DescriptionOfTheAplicantAndMotivationToJoin = "Great applicant",
                AppSubmitted = "7/11/22",
                RelevantNotes = "Interview held & accepted 07/21/22"
            }
        };

        // Act
        var result = await _sut.ImportUsersAsync(rows, isDryRun: false);

        // Assert
        result.Should().NotBeNull();
        result.TotalRecords.Should().Be(1);
        result.SuccessCount.Should().Be(1);
        result.ErrorCount.Should().Be(0);
        result.SkippedCount.Should().Be(0);

        // Verify user was created
        var user = await _context.Users.FirstOrDefaultAsync();
        user.Should().NotBeNull();
        user!.Email.Should().Be("test@example.com");
        user.SceneName.Should().Be("TestUser");
        user.Pronouns.Should().Be("they/them");
        user.FetLifeName.Should().Be("fetlife123");
        user.VettingStatus.Should().Be(3); // Approved
        user.Role.Should().Be("VettedMember");
        user.EmailConfirmed.Should().BeFalse();
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ImportUsersAsync_WithValidData_CreatesVettingApplication()
    {
        // Arrange
        var rows = new List<CsvRow>
        {
            new CsvRow
            {
                VetteesEmail = "test@example.com",
                VetteesNickname = "TestUser",
                AppSubmitted = "7/11/22",
                RelevantNotes = "Approved"
            }
        };

        // Act
        await _sut.ImportUsersAsync(rows, isDryRun: false);

        // Assert
        var application = await _context.VettingApplications.FirstOrDefaultAsync();
        application.Should().NotBeNull();
        application!.Email.Should().Be("test@example.com");
        application.SceneName.Should().Be("TestUser");
        application.WorkflowStatus.Should().Be(3); // Approved
        application.SubmittedAt.Year.Should().Be(2022);
        application.SubmittedAt.Month.Should().Be(7);
        application.SubmittedAt.Day.Should().Be(11);
        application.ApplicationNumber.Should().StartWith("VET-");
    }

    [Fact]
    public async Task ImportUsersAsync_WithValidData_CreatesAuditLogs()
    {
        // Arrange
        var rows = new List<CsvRow>
        {
            new CsvRow
            {
                VetteesEmail = "test@example.com",
                VetteesNickname = "TestUser",
                AppSubmitted = "7/11/22",
                RelevantNotes = "Interview held & accepted 07/21/22"
            }
        };

        // Act
        await _sut.ImportUsersAsync(rows, isDryRun: false);

        // Assert
        var auditLogs = await _context.VettingAuditLogs.ToListAsync();
        auditLogs.Should().NotBeEmpty();
        auditLogs.Should().Contain(log => log.Action.Contains("Approved"));
    }

    #endregion

    #region Duplicate Detection

    [Fact]
    public async Task ImportUsersAsync_WithDuplicateEmail_SkipsUser()
    {
        // Arrange - Create existing user
        var existingUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            NormalizedEmail = "TEST@EXAMPLE.COM",
            UserName = "test@example.com",
            NormalizedUserName = "TEST@EXAMPLE.COM",
            SceneName = "ExistingUser",
            VettingStatus = 3,
            IsActive = true,
            EmailConfirmed = false,
            PasswordHash = "hash",
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            EncryptedLegalName = "",
            DateOfBirth = DateTime.UtcNow.AddYears(-30),
            PronouncedName = "",
            Pronouns = "",
            Role = "VettedMember",
            TermsOfServiceAccepted = false,
            HasVettingApplication = true
        };
        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        var rows = new List<CsvRow>
        {
            new CsvRow
            {
                VetteesEmail = "TEST@EXAMPLE.COM", // Same email, different case
                VetteesNickname = "NewUser",
                AppSubmitted = "7/11/22"
            }
        };

        // Act
        var result = await _sut.ImportUsersAsync(rows, isDryRun: false);

        // Assert
        result.TotalRecords.Should().Be(1);
        result.SkippedCount.Should().Be(1);
        result.SuccessCount.Should().Be(0);
        result.ErrorCount.Should().Be(0);
        result.Warnings.Should().ContainSingle();
        result.Warnings[0].Should().Contain("Duplicate");

        // Verify only one user exists
        var userCount = await _context.Users.CountAsync();
        userCount.Should().Be(1);
    }

    [Fact]
    public async Task ImportUsersAsync_WithDuplicateSceneName_SkipsUser()
    {
        // Arrange - Create existing user
        var existingUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "existing@example.com",
            NormalizedEmail = "EXISTING@EXAMPLE.COM",
            UserName = "existing@example.com",
            NormalizedUserName = "EXISTING@EXAMPLE.COM",
            SceneName = "TestUser",
            VettingStatus = 3,
            IsActive = true,
            EmailConfirmed = false,
            PasswordHash = "hash",
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            EncryptedLegalName = "",
            DateOfBirth = DateTime.UtcNow.AddYears(-30),
            PronouncedName = "",
            Pronouns = "",
            Role = "VettedMember",
            TermsOfServiceAccepted = false,
            HasVettingApplication = true
        };
        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        var rows = new List<CsvRow>
        {
            new CsvRow
            {
                VetteesEmail = "new@example.com",
                VetteesNickname = "testuser", // Same scene name, different case
                AppSubmitted = "7/11/22"
            }
        };

        // Act
        var result = await _sut.ImportUsersAsync(rows, isDryRun: false);

        // Assert
        result.TotalRecords.Should().Be(1);
        result.SkippedCount.Should().Be(1);
        result.SuccessCount.Should().Be(0);
        result.Warnings.Should().ContainSingle();
        result.Warnings[0].Should().Contain("Duplicate");

        // Verify only one user exists
        var userCount = await _context.Users.CountAsync();
        userCount.Should().Be(1);
    }

    #endregion

    #region Validation Errors

    [Fact]
    public async Task ImportUsersAsync_WithMissingEmail_AddsError()
    {
        // Arrange
        var rows = new List<CsvRow>
        {
            new CsvRow
            {
                VetteesEmail = "",
                VetteesNickname = "TestUser",
                AppSubmitted = "7/11/22"
            }
        };

        // Act
        var result = await _sut.ImportUsersAsync(rows, isDryRun: false);

        // Assert
        result.TotalRecords.Should().Be(1);
        result.ErrorCount.Should().Be(1);
        result.SuccessCount.Should().Be(0);
        result.Errors.Should().ContainSingle();
        result.Errors[0].Should().Contain("Missing email");
    }

    [Fact]
    public async Task ImportUsersAsync_WithMissingSceneName_AddsError()
    {
        // Arrange
        var rows = new List<CsvRow>
        {
            new CsvRow
            {
                VetteesEmail = "test@example.com",
                VetteesNickname = "",
                AppSubmitted = "7/11/22"
            }
        };

        // Act
        var result = await _sut.ImportUsersAsync(rows, isDryRun: false);

        // Assert
        result.TotalRecords.Should().Be(1);
        result.ErrorCount.Should().Be(1);
        result.SuccessCount.Should().Be(0);
        result.Errors.Should().ContainSingle();
        result.Errors[0].Should().Contain("Missing scene name");
    }

    [Fact]
    public async Task ImportUsersAsync_WithWhitespaceEmail_AddsError()
    {
        // Arrange
        var rows = new List<CsvRow>
        {
            new CsvRow
            {
                VetteesEmail = "   ",
                VetteesNickname = "TestUser",
                AppSubmitted = "7/11/22"
            }
        };

        // Act
        var result = await _sut.ImportUsersAsync(rows, isDryRun: false);

        // Assert
        result.ErrorCount.Should().Be(1);
        result.Errors[0].Should().Contain("Missing email");
    }

    #endregion

    #region Dry Run Mode

    [Fact]
    public async Task ImportUsersAsync_WithDryRun_DoesNotCreateUser()
    {
        // Arrange
        var rows = new List<CsvRow>
        {
            new CsvRow
            {
                VetteesEmail = "test@example.com",
                VetteesNickname = "TestUser",
                AppSubmitted = "7/11/22"
            }
        };

        // Act
        var result = await _sut.ImportUsersAsync(rows, isDryRun: true);

        // Assert
        result.SuccessCount.Should().Be(1);

        // Verify no user was created
        var userCount = await _context.Users.CountAsync();
        userCount.Should().Be(0);

        var applicationCount = await _context.VettingApplications.CountAsync();
        applicationCount.Should().Be(0);
    }

    [Fact]
    public async Task ImportUsersAsync_WithDryRun_StillLogsOperations()
    {
        // Arrange
        var rows = new List<CsvRow>
        {
            new CsvRow
            {
                VetteesEmail = "test@example.com",
                VetteesNickname = "TestUser",
                AppSubmitted = "7/11/22"
            }
        };

        // Act
        await _sut.ImportUsersAsync(rows, isDryRun: true);

        // Assert - Verify logging occurred
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("DRY RUN")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ImportUsersAsync_WithDryRun_StillValidatesData()
    {
        // Arrange
        var rows = new List<CsvRow>
        {
            new CsvRow
            {
                VetteesEmail = "",
                VetteesNickname = "TestUser",
                AppSubmitted = "7/11/22"
            }
        };

        // Act
        var result = await _sut.ImportUsersAsync(rows, isDryRun: true);

        // Assert
        result.ErrorCount.Should().Be(1);
        result.Errors[0].Should().Contain("Missing email");
    }

    #endregion

    #region Multiple Rows

    [Fact]
    public async Task ImportUsersAsync_WithMultipleValidRows_ImportsAll()
    {
        // Arrange
        var rows = new List<CsvRow>
        {
            new CsvRow
            {
                VetteesEmail = "user1@example.com",
                VetteesNickname = "User1",
                AppSubmitted = "7/11/22"
            },
            new CsvRow
            {
                VetteesEmail = "user2@example.com",
                VetteesNickname = "User2",
                AppSubmitted = "7/12/22"
            },
            new CsvRow
            {
                VetteesEmail = "user3@example.com",
                VetteesNickname = "User3",
                AppSubmitted = "7/13/22"
            }
        };

        // Act
        var result = await _sut.ImportUsersAsync(rows, isDryRun: false);

        // Assert
        result.TotalRecords.Should().Be(3);
        result.SuccessCount.Should().Be(3);
        result.ErrorCount.Should().Be(0);
        result.SkippedCount.Should().Be(0);

        var userCount = await _context.Users.CountAsync();
        userCount.Should().Be(3);
    }

    [Fact]
    public async Task ImportUsersAsync_WithMixedValidAndInvalid_ProcessesAllRows()
    {
        // Arrange
        var rows = new List<CsvRow>
        {
            new CsvRow
            {
                VetteesEmail = "user1@example.com",
                VetteesNickname = "User1",
                AppSubmitted = "7/11/22"
            },
            new CsvRow
            {
                VetteesEmail = "",
                VetteesNickname = "User2",
                AppSubmitted = "7/12/22"
            },
            new CsvRow
            {
                VetteesEmail = "user3@example.com",
                VetteesNickname = "User3",
                AppSubmitted = "7/13/22"
            }
        };

        // Act
        var result = await _sut.ImportUsersAsync(rows, isDryRun: false);

        // Assert
        result.TotalRecords.Should().Be(3);
        result.SuccessCount.Should().Be(2);
        result.ErrorCount.Should().Be(1);

        var userCount = await _context.Users.CountAsync();
        userCount.Should().Be(2);
    }

    #endregion

    #region Email Normalization

    [Fact]
    public async Task ImportUsersAsync_WithMixedCaseEmail_NormalizesCorrectly()
    {
        // Arrange
        var rows = new List<CsvRow>
        {
            new CsvRow
            {
                VetteesEmail = "TeSt@ExAmPlE.cOm",
                VetteesNickname = "TestUser",
                AppSubmitted = "7/11/22"
            }
        };

        // Act
        await _sut.ImportUsersAsync(rows, isDryRun: false);

        // Assert
        var user = await _context.Users.FirstOrDefaultAsync();
        user.Should().NotBeNull();
        user!.Email.Should().Be("test@example.com");
        user.NormalizedEmail.Should().Be("TEST@EXAMPLE.COM");
    }

    [Fact]
    public async Task ImportUsersAsync_WithEmailWhitespace_TrimsCorrectly()
    {
        // Arrange
        var rows = new List<CsvRow>
        {
            new CsvRow
            {
                VetteesEmail = "  test@example.com  ",
                VetteesNickname = "TestUser",
                AppSubmitted = "7/11/22"
            }
        };

        // Act
        await _sut.ImportUsersAsync(rows, isDryRun: false);

        // Assert
        var user = await _context.Users.FirstOrDefaultAsync();
        user.Should().NotBeNull();
        user!.Email.Should().Be("test@example.com");
    }

    #endregion

    #region Date Parsing

    [Fact]
    public async Task ImportUsersAsync_WithValidDate_ParsesCorrectly()
    {
        // Arrange
        var rows = new List<CsvRow>
        {
            new CsvRow
            {
                VetteesEmail = "test@example.com",
                VetteesNickname = "TestUser",
                AppSubmitted = "7/11/22"
            }
        };

        // Act
        await _sut.ImportUsersAsync(rows, isDryRun: false);

        // Assert
        var user = await _context.Users.FirstOrDefaultAsync();
        user.Should().NotBeNull();
        user!.CreatedAt.Year.Should().Be(2022);
        user.CreatedAt.Month.Should().Be(7);
        user.CreatedAt.Day.Should().Be(11);
    }

    [Fact]
    public async Task ImportUsersAsync_WithInvalidDate_UsesFallbackDate()
    {
        // Arrange
        var rows = new List<CsvRow>
        {
            new CsvRow
            {
                VetteesEmail = "test@example.com",
                VetteesNickname = "TestUser",
                AppSubmitted = "invalid date"
            }
        };

        // Act
        await _sut.ImportUsersAsync(rows, isDryRun: false);

        // Assert
        var user = await _context.Users.FirstOrDefaultAsync();
        user.Should().NotBeNull();
        // Should use fallback date (2 years ago)
        user!.CreatedAt.Should().BeCloseTo(DateTime.UtcNow.AddYears(-2), TimeSpan.FromDays(1));
    }

    #endregion

    #region Row Number Reporting

    [Fact]
    public async Task ImportUsersAsync_WithErrors_ReportsCorrectRowNumbers()
    {
        // Arrange
        var rows = new List<CsvRow>
        {
            new CsvRow
            {
                VetteesEmail = "user1@example.com",
                VetteesNickname = "User1",
                AppSubmitted = "7/11/22"
            },
            new CsvRow
            {
                VetteesEmail = "",
                VetteesNickname = "User2",
                AppSubmitted = "7/12/22"
            }
        };

        // Act
        var result = await _sut.ImportUsersAsync(rows, isDryRun: false);

        // Assert
        result.Errors[0].Should().Contain("Row 3"); // Row 2 is index 1, +2 = Row 3
    }

    #endregion
}

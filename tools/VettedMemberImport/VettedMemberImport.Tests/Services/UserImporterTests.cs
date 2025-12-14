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
                Email = "test@example.com",
                SceneName = "TestUser",
                Pronouns = "they/them",
                FetLifeHandle = "fetlife123",
                MotivationDescription = "Great applicant",
                ApplicationDate = "7/11/22",
                Notes = "Interview held & accepted 07/21/22"
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
                Email = "test@example.com",
                SceneName = "TestUser",
                ApplicationDate = "7/11/22",
                Notes = "Approved"
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
                Email = "test@example.com",
                SceneName = "TestUser",
                ApplicationDate = "7/11/22",
                Notes = "Interview held & accepted 07/21/22"
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
                Email = "TEST@EXAMPLE.COM", // Same email, different case
                SceneName = "NewUser",
                ApplicationDate = "7/11/22"
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
    public async Task ImportUsersAsync_WithDuplicateSceneName_AllowsImport()
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
                Email = "new@example.com",
                SceneName = "testuser", // Same scene name, different case - this is ALLOWED
                ApplicationDate = "7/11/22"
            }
        };

        // Act
        var result = await _sut.ImportUsersAsync(rows, isDryRun: false);

        // Assert - Duplicate scene names ARE allowed, only duplicate emails are blocked
        result.TotalRecords.Should().Be(1);
        result.SuccessCount.Should().Be(1);
        result.SkippedCount.Should().Be(0);
        result.ErrorCount.Should().Be(0);

        // Verify both users exist (duplicate scene names allowed)
        var userCount = await _context.Users.CountAsync();
        userCount.Should().Be(2);
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
                Email = "",
                SceneName = "TestUser",
                ApplicationDate = "7/11/22"
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
                Email = "test@example.com",
                SceneName = "",
                ApplicationDate = "7/11/22"
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
                Email = "   ",
                SceneName = "TestUser",
                ApplicationDate = "7/11/22"
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
                Email = "test@example.com",
                SceneName = "TestUser",
                ApplicationDate = "7/11/22"
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
                Email = "test@example.com",
                SceneName = "TestUser",
                ApplicationDate = "7/11/22"
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
                Email = "",
                SceneName = "TestUser",
                ApplicationDate = "7/11/22"
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
                Email = "user1@example.com",
                SceneName = "User1",
                ApplicationDate = "7/11/22"
            },
            new CsvRow
            {
                Email = "user2@example.com",
                SceneName = "User2",
                ApplicationDate = "7/12/22"
            },
            new CsvRow
            {
                Email = "user3@example.com",
                SceneName = "User3",
                ApplicationDate = "7/13/22"
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
                Email = "user1@example.com",
                SceneName = "User1",
                ApplicationDate = "7/11/22"
            },
            new CsvRow
            {
                Email = "",
                SceneName = "User2",
                ApplicationDate = "7/12/22"
            },
            new CsvRow
            {
                Email = "user3@example.com",
                SceneName = "User3",
                ApplicationDate = "7/13/22"
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
                Email = "TeSt@ExAmPlE.cOm",
                SceneName = "TestUser",
                ApplicationDate = "7/11/22"
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
                Email = "  test@example.com  ",
                SceneName = "TestUser",
                ApplicationDate = "7/11/22"
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
                Email = "test@example.com",
                SceneName = "TestUser",
                ApplicationDate = "7/11/22"
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
                Email = "test@example.com",
                SceneName = "TestUser",
                ApplicationDate = "invalid date"
            }
        };

        // Act
        await _sut.ImportUsersAsync(rows, isDryRun: false);

        // Assert
        var user = await _context.Users.FirstOrDefaultAsync();
        user.Should().NotBeNull();
        // Should use fallback date (current UTC time)
        user!.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
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
                Email = "user1@example.com",
                SceneName = "User1",
                ApplicationDate = "7/11/22"
            },
            new CsvRow
            {
                Email = "",
                SceneName = "User2",
                ApplicationDate = "7/12/22"
            }
        };

        // Act
        var result = await _sut.ImportUsersAsync(rows, isDryRun: false);

        // Assert
        result.Errors[0].Should().Contain("Row 3"); // Row 2 is index 1, +2 = Row 3
    }

    #endregion

    #region Status Parameter Tests

    [Fact]
    public async Task ImportUsersAsync_WithDefaultParameters_CreatesApprovedUser()
    {
        // Arrange
        var rows = new List<CsvRow>
        {
            new CsvRow
            {
                Email = "approved@example.com",
                SceneName = "ApprovedUser",
                ApplicationDate = "7/11/22"
            }
        };

        // Act - Call without status parameters (should use defaults: vettingStatus=3, workflowStatus=3)
        var result = await _sut.ImportUsersAsync(rows, isDryRun: false);

        // Assert
        result.Should().NotBeNull();
        result.SuccessCount.Should().Be(1);

        // Verify user has approved vetting status
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == "approved@example.com");
        user.Should().NotBeNull();
        user!.VettingStatus.Should().Be(3, "default status should be Approved (3)");
        user.SceneName.Should().Be("ApprovedUser");

        // Verify application has approved workflow status
        var application = await _context.VettingApplications.FirstOrDefaultAsync(a => a.Email == "approved@example.com");
        application.Should().NotBeNull();
        application!.WorkflowStatus.Should().Be(3, "default workflow should be Approved (3)");
    }

    [Fact]
    public async Task ImportUsersAsync_WithApprovedStatus_CreatesFullyVettedUser()
    {
        // Arrange
        var rows = new List<CsvRow>
        {
            new CsvRow
            {
                Email = "vetted@example.com",
                SceneName = "VettedUser",
                Pronouns = "they/them",
                ApplicationDate = "7/11/22"
            }
        };

        // Act - Explicitly set approved status
        var result = await _sut.ImportUsersAsync(rows, isDryRun: false, vettingStatus: 3, workflowStatus: 3);

        // Assert
        result.Should().NotBeNull();
        result.SuccessCount.Should().Be(1);
        result.ErrorCount.Should().Be(0);

        // Verify user has fully vetted status
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == "vetted@example.com");
        user.Should().NotBeNull();
        user!.VettingStatus.Should().Be(3, "user should be fully Approved (VettingStatus = 3)");
        user.SceneName.Should().Be("VettedUser");
        user.Pronouns.Should().Be("they/them");

        // Verify application has approved workflow status
        var application = await _context.VettingApplications.FirstOrDefaultAsync(a => a.Email == "vetted@example.com");
        application.Should().NotBeNull();
        application!.WorkflowStatus.Should().Be(3, "workflow should be Approved (3)");
        application.SceneName.Should().Be("VettedUser");
    }

    [Fact]
    public async Task ImportUsersAsync_WithInterviewApprovedStatus_CreatesAwaitingInterviewUser()
    {
        // Arrange
        var rows = new List<CsvRow>
        {
            new CsvRow
            {
                Email = "interview@example.com",
                SceneName = "InterviewUser",
                Pronouns = "she/her",
                FetLifeHandle = "fetlife456",
                ApplicationDate = "7/11/22"
            }
        };

        // Act - Set interview-approved status (vettingStatus=1, workflowStatus=1)
        var result = await _sut.ImportUsersAsync(rows, isDryRun: false, vettingStatus: 1, workflowStatus: 1);

        // Assert
        result.Should().NotBeNull();
        result.SuccessCount.Should().Be(1);
        result.ErrorCount.Should().Be(0);

        // Verify user has interview-approved status
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == "interview@example.com");
        user.Should().NotBeNull();
        user!.VettingStatus.Should().Be(1, "user should be InterviewApproved (VettingStatus = 1)");
        user.SceneName.Should().Be("InterviewUser");
        user.Pronouns.Should().Be("she/her");
        user.FetLifeName.Should().Be("fetlife456");

        // Verify application has interview-approved workflow status
        var application = await _context.VettingApplications.FirstOrDefaultAsync(a => a.Email == "interview@example.com");
        application.Should().NotBeNull();
        application!.WorkflowStatus.Should().Be(1, "workflow should be InterviewApproved (1)");
        application.SceneName.Should().Be("InterviewUser");
    }

    [Fact]
    public async Task ImportUsersAsync_WithInterviewApprovedStatus_MultipleUsers_AllHaveCorrectStatus()
    {
        // Arrange - Multiple users to import with interview-approved status
        var rows = new List<CsvRow>
        {
            new CsvRow
            {
                Email = "interview1@example.com",
                SceneName = "InterviewUser1",
                ApplicationDate = "7/11/22"
            },
            new CsvRow
            {
                Email = "interview2@example.com",
                SceneName = "InterviewUser2",
                ApplicationDate = "7/12/22"
            },
            new CsvRow
            {
                Email = "interview3@example.com",
                SceneName = "InterviewUser3",
                ApplicationDate = "7/13/22"
            }
        };

        // Act - Import all with interview-approved status
        var result = await _sut.ImportUsersAsync(rows, isDryRun: false, vettingStatus: 1, workflowStatus: 1);

        // Assert
        result.Should().NotBeNull();
        result.TotalRecords.Should().Be(3);
        result.SuccessCount.Should().Be(3);
        result.ErrorCount.Should().Be(0);

        // Verify all users have interview-approved status
        var users = await _context.Users.Where(u => u.Email.StartsWith("interview")).ToListAsync();
        users.Should().HaveCount(3);
        users.Should().OnlyContain(u => u.VettingStatus == 1, "all users should have InterviewApproved status");

        // Verify all applications have interview-approved workflow status
        var applications = await _context.VettingApplications.Where(a => a.Email.StartsWith("interview")).ToListAsync();
        applications.Should().HaveCount(3);
        applications.Should().OnlyContain(a => a.WorkflowStatus == 1, "all applications should have InterviewApproved workflow");
    }

    [Fact]
    public async Task ImportUsersAsync_WithInterviewApprovedStatus_DryRun_ValidatesWithoutCreating()
    {
        // Arrange
        var rows = new List<CsvRow>
        {
            new CsvRow
            {
                Email = "dryrun@example.com",
                SceneName = "DryRunUser",
                ApplicationDate = "7/11/22"
            }
        };

        // Act - Dry run with interview-approved status
        var result = await _sut.ImportUsersAsync(rows, isDryRun: true, vettingStatus: 1, workflowStatus: 1);

        // Assert
        result.Should().NotBeNull();
        result.SuccessCount.Should().Be(1);

        // Verify no user was created in database
        var userCount = await _context.Users.CountAsync();
        userCount.Should().Be(0, "dry run should not create users");

        var applicationCount = await _context.VettingApplications.CountAsync();
        applicationCount.Should().Be(0, "dry run should not create applications");
    }

    [Fact]
    public async Task ImportUsersAsync_WithDifferentStatuses_CreatesUsersWithCorrectStatus()
    {
        // Arrange - Test that we can import with different status values
        var approvedRows = new List<CsvRow>
        {
            new CsvRow
            {
                Email = "fully-approved@example.com",
                SceneName = "FullyApproved",
                ApplicationDate = "7/11/22"
            }
        };

        var interviewRows = new List<CsvRow>
        {
            new CsvRow
            {
                Email = "needs-interview@example.com",
                SceneName = "NeedsInterview",
                ApplicationDate = "7/12/22"
            }
        };

        // Act - Import with approved status first
        var approvedResult = await _sut.ImportUsersAsync(approvedRows, isDryRun: false, vettingStatus: 3, workflowStatus: 3);

        // Then import with interview-approved status
        var interviewResult = await _sut.ImportUsersAsync(interviewRows, isDryRun: false, vettingStatus: 1, workflowStatus: 1);

        // Assert
        approvedResult.SuccessCount.Should().Be(1);
        interviewResult.SuccessCount.Should().Be(1);

        // Verify approved user has status 3
        var approvedUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "fully-approved@example.com");
        approvedUser.Should().NotBeNull();
        approvedUser!.VettingStatus.Should().Be(3);

        var approvedApp = await _context.VettingApplications.FirstOrDefaultAsync(a => a.Email == "fully-approved@example.com");
        approvedApp.Should().NotBeNull();
        approvedApp!.WorkflowStatus.Should().Be(3);

        // Verify interview user has status 1
        var interviewUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "needs-interview@example.com");
        interviewUser.Should().NotBeNull();
        interviewUser!.VettingStatus.Should().Be(1);

        var interviewApp = await _context.VettingApplications.FirstOrDefaultAsync(a => a.Email == "needs-interview@example.com");
        interviewApp.Should().NotBeNull();
        interviewApp!.WorkflowStatus.Should().Be(1);
    }

    #endregion
}

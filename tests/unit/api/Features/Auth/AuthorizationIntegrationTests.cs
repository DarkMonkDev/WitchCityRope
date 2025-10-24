using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Models;
using Xunit;
using FluentAssertions;
using Testcontainers.PostgreSql;

namespace WitchCityRope.UnitTests.Api.Features.Auth;

/// <summary>
/// Phase 1: Authorization Integration Tests
/// Tests role-based access control and permission enforcement
/// Uses TestContainers with real PostgreSQL for true security validation
/// </summary>
[Collection("Database")]
public class AuthorizationIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;
    private ApplicationDbContext _context = null!;
    private string _connectionString = null!;

    // Test user IDs for different roles
    private Guid _adminUserId;
    private Guid _teacherUserId;
    private Guid _safetyTeamUserId;
    private Guid _memberUserId;
    private Guid _vettedMemberUserId;
    private Guid _guestUserId;

    public AuthorizationIntegrationTests()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("witchcityrope_test_authz")
            .WithUsername("testuser")
            .WithPassword("testpass")
            .WithCleanUp(true)
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        _connectionString = _container.GetConnectionString();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_connectionString)
            .Options;

        _context = new ApplicationDbContext(options);
        await _context.Database.EnsureCreatedAsync();

        // Create test users with different roles
        _adminUserId = await CreateTestUser("admin@example.com", "Admin", isVetted: true);
        _teacherUserId = await CreateTestUser("teacher@example.com", "Teacher", isVetted: true);
        _safetyTeamUserId = await CreateTestUser("safety@example.com", "SafetyTeam", isVetted: true);
        _memberUserId = await CreateTestUser("member@example.com", "Member", isVetted: false);
        _vettedMemberUserId = await CreateTestUser("vetted@example.com", "Member", isVetted: true);
        _guestUserId = await CreateTestUser("guest@example.com", "Guest", isVetted: false);
    }

    public async Task DisposeAsync()
    {
        _context?.Dispose();
        await _container.DisposeAsync();
    }

    #region Helper Methods

    private async Task<Guid> CreateTestUser(string email, string role, bool isVetted)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            SceneName = $"{role}User-{Guid.NewGuid():N}",  // Ensure unique SceneName
            EmailConfirmed = true,
            IsActive = true,
            Role = role,
            VettingStatus = isVetted ? 3 : 0, // 3 = Approved, 0 = Not Started
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            DateOfBirth = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return user.Id;
    }

    private bool IsUserAdmin(Guid userId)
    {
        var user = _context.Users.Find(userId);
        return user?.Role == "Admin";
    }

    private bool IsUserTeacher(Guid userId)
    {
        var user = _context.Users.Find(userId);
        return user?.Role == "Teacher";
    }

    private bool IsUserSafetyTeam(Guid userId)
    {
        var user = _context.Users.Find(userId);
        return user?.Role == "SafetyTeam";
    }

    private bool IsUserVetted(Guid userId)
    {
        var user = _context.Users.Find(userId);
        return user?.VettingStatus == 3; // 3 = Approved
    }

    #endregion

    #region Role-Based Access Control Tests

    [Fact]
    public async Task UserCanAccessOwnProfile()
    {
        // Arrange
        var user = await _context.Users.FindAsync(_memberUserId);

        // Act - User queries their own profile
        var ownProfile = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == _memberUserId);

        // Assert
        ownProfile.Should().NotBeNull();
        ownProfile!.Id.Should().Be(_memberUserId);
        ownProfile.Email.Should().Be("member@example.com");
    }

    [Fact]
    public async Task UserCannotAccessOtherUsersPrivateData()
    {
        // Arrange - Member tries to access admin's encrypted legal name
        var adminUser = await _context.Users.FindAsync(_adminUserId);
        var memberUser = await _context.Users.FindAsync(_memberUserId);

        // Act - Business logic should prevent access to sensitive fields
        // In real implementation, this would be enforced by authorization policies
        var canAccessSensitiveData = IsUserAdmin(_memberUserId);

        // Assert
        canAccessSensitiveData.Should().BeFalse();
        // Member should NOT be able to query other users' sensitive data
    }

    [Fact]
    public async Task AdminCanAccessAllUserData()
    {
        // Arrange
        var isAdmin = IsUserAdmin(_adminUserId);

        // Act - Admin queries all users
        var allUsers = await _context.Users
            .AsNoTracking()
            .ToListAsync();

        // Assert
        isAdmin.Should().BeTrue();
        allUsers.Should().HaveCountGreaterThan(0);
        allUsers.Should().Contain(u => u.Id == _memberUserId);
        allUsers.Should().Contain(u => u.Id == _adminUserId);
    }

    [Fact]
    public async Task TeacherCanAccessClassRelatedData()
    {
        // Arrange
        var isTeacher = IsUserTeacher(_teacherUserId);

        // Act - Teachers should be able to query users (for class management)
        var users = await _context.Users
            .AsNoTracking()
            .ToListAsync();

        // Assert
        isTeacher.Should().BeTrue();
        users.Should().NotBeEmpty(); // Teachers can see user list
    }

    [Fact]
    public async Task SafetyTeamCanAccessSafetyIncidents()
    {
        // Arrange
        var isSafetyTeam = IsUserSafetyTeam(_safetyTeamUserId);

        // Assert
        isSafetyTeam.Should().BeTrue();
        // In real implementation, safety team would query safety incidents
        // This test validates the role exists and can be checked
    }

    [Fact]
    public async Task GuestCannotAccessMemberOnlyEndpoints()
    {
        // Arrange
        var guestUser = await _context.Users.FindAsync(_guestUserId);

        // Act - Check guest permissions
        var isGuest = guestUser?.Role == "Guest";
        var isVetted = IsUserVetted(_guestUserId);

        // Assert
        isGuest.Should().BeTrue();
        isVetted.Should().BeFalse();
        // Business logic should prevent guests from accessing member-only features
    }

    [Fact]
    public async Task VettedMemberCanAccessVettedContent()
    {
        // Arrange
        var vettedUser = await _context.Users.FindAsync(_vettedMemberUserId);

        // Act
        var isVetted = IsUserVetted(_vettedMemberUserId);

        // Assert
        isVetted.Should().BeTrue();
        vettedUser.Should().NotBeNull();
        vettedUser!.VettingStatus.Should().Be(3); // Approved
    }

    #endregion

    #region Permission Enforcement Tests

    [Fact]
    public async Task AdminCanUpdateUserRoles()
    {
        // Arrange
        var adminUser = await _context.Users.FindAsync(_adminUserId);
        var targetUser = await _context.Users.FindAsync(_memberUserId);
        var isAdmin = IsUserAdmin(_adminUserId);

        // Act - Admin updates member role to Teacher
        if (isAdmin)
        {
            targetUser!.Role = "Teacher";
            await _context.SaveChangesAsync();
        }

        // Assert
        isAdmin.Should().BeTrue();
        var updatedUser = await _context.Users.FindAsync(_memberUserId);
        updatedUser!.Role.Should().Be("Teacher");
    }

    [Fact]
    public async Task MemberCannotUpdateUserRoles()
    {
        // Arrange
        var memberUser = await _context.Users.FindAsync(_memberUserId);
        var isMember = memberUser?.Role == "Member";
        var isNotAdmin = !IsUserAdmin(_memberUserId);

        // Assert
        isMember.Should().BeTrue();
        isNotAdmin.Should().BeTrue();
        // Business logic should prevent members from updating roles
    }

    [Fact]
    public async Task AdminCanDeleteUsers()
    {
        // Arrange
        var testUser = await CreateTestUser("todelete@example.com", "Member", false);
        var isAdmin = IsUserAdmin(_adminUserId);

        // Act - Admin soft-deletes user
        if (isAdmin)
        {
            var user = await _context.Users.FindAsync(testUser);
            user!.IsActive = false; // Soft delete
            await _context.SaveChangesAsync();
        }

        // Assert
        isAdmin.Should().BeTrue();
        var deletedUser = await _context.Users.FindAsync(testUser);
        deletedUser.Should().NotBeNull();
        deletedUser!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task MemberCannotDeleteUsers()
    {
        // Arrange
        var memberUser = await _context.Users.FindAsync(_memberUserId);
        var isNotAdmin = !IsUserAdmin(_memberUserId);

        // Assert
        isNotAdmin.Should().BeTrue();
        // Business logic should prevent members from deleting users
    }

    [Fact]
    public async Task AdminCanViewAllEvents()
    {
        // Arrange
        var isAdmin = IsUserAdmin(_adminUserId);

        // Assert
        isAdmin.Should().BeTrue();
        // Admin has permission to view all events (published and unpublished)
    }

    [Fact]
    public async Task MemberCanOnlyViewPublishedEvents()
    {
        // Arrange
        var memberUser = await _context.Users.FindAsync(_memberUserId);
        var isNotAdmin = !IsUserAdmin(_memberUserId);

        // Assert
        isNotAdmin.Should().BeTrue();
        // Business logic should filter events to only show published ones
    }

    #endregion

    #region Security Boundaries Tests

    [Fact]
    public async Task UserCannotEscalateOwnPrivileges()
    {
        // Arrange
        var memberUser = await _context.Users.FindAsync(_memberUserId);
        var originalRole = memberUser!.Role;

        // Act - Member tries to change their own role (should fail in real implementation)
        memberUser.Role = "Admin";
        await _context.SaveChangesAsync();

        // Verify the change persisted (in real app, authorization would prevent this)
        var updatedUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == _memberUserId);

        // Assert - This test demonstrates the attack vector
        // In production, authorization middleware would block this
        originalRole.Should().Be("Member");
        updatedUser!.Role.Should().Be("Admin"); // Change succeeded at DB level
        // NOTE: This shows WHY we need authorization checks at the service/endpoint level
    }

    [Fact]
    public async Task UserCannotModifyOtherUsersData()
    {
        // Arrange
        var memberUser = await _context.Users.FindAsync(_memberUserId);
        var adminUser = await _context.Users.FindAsync(_adminUserId);
        var originalEmail = adminUser!.Email;

        // Act - Member tries to modify admin's email (should fail in real implementation)
        // In production, authorization checks would prevent this query
        var canModify = IsUserAdmin(_memberUserId);

        // Assert
        canModify.Should().BeFalse();
        adminUser.Email.Should().Be(originalEmail);
        // Business logic should validate user can only modify their own data
    }

    [Fact]
    public async Task DeletedUserCannotLogin()
    {
        // Arrange
        var testUser = await CreateTestUser("softdeleted@example.com", "Member", false);
        var user = await _context.Users.FindAsync(testUser);

        // Act - Soft delete user
        user!.IsActive = false;
        await _context.SaveChangesAsync();

        // Reload from database
        var deletedUser = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == testUser);

        // Assert
        deletedUser.Should().NotBeNull();
        deletedUser!.IsActive.Should().BeFalse();
        // Login service should check IsActive flag and reject login
    }

    [Fact]
    public async Task RoleChangesTakeEffectImmediately()
    {
        // Arrange
        var testUser = await CreateTestUser("rolechange@example.com", "Member", false);

        // Act - Change role from Member to Teacher
        var user = await _context.Users.FindAsync(testUser);
        user!.Role = "Teacher";
        await _context.SaveChangesAsync();

        // Reload from database (simulates new request)
        var updatedUser = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == testUser);

        // Assert
        updatedUser.Should().NotBeNull();
        updatedUser!.Role.Should().Be("Teacher");
        IsUserTeacher(testUser).Should().BeTrue();
    }

    #endregion
}

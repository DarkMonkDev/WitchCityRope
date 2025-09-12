using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using WitchCityRope.Api.Services;
using WitchCityRope.Core;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Enums;
using WitchCityRope.Core.ValueObjects;
using WitchCityRope.Infrastructure.Data;
using WitchCityRope.Tests.Common.Builders;

namespace WitchCityRope.Api.Tests.Services;

public class UserServiceTests : IDisposable
{
    private readonly WitchCityRopeDbContext _dbContext;
    private readonly Mock<IUserService> _userServiceMock;

    public UserServiceTests()
    {
        var options = new DbContextOptionsBuilder<WitchCityRopeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new WitchCityRopeDbContext(options);
        _userServiceMock = new Mock<IUserService>();
    }

    #region User Management Tests

    [Fact]
    public async Task CreateUser_ValidData_ShouldSucceed()
    {
        // Arrange
        var user = new UserBuilder()
            .WithSceneName("NewUser")
            .WithEmail("newuser@example.com")
            .Build();

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var savedUser = await _dbContext.Users.FindAsync(user.Id);

        // Assert
        savedUser.Should().NotBeNull();
        savedUser!.SceneName.Value.Should().Be("NewUser");
        savedUser.Email.Value.Should().Be("newuser@example.com");
        savedUser.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetUserById_ExistingUser_ShouldReturnUser()
    {
        // Arrange
        var user = new UserBuilder().Build();
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _dbContext.Users.FindAsync(user.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task GetUserByEmail_ExistingUser_ShouldReturnUser()
    {
        // Arrange
        var email = "test@example.com";
        var user = new UserBuilder().WithEmail(email).Build();
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email.Value == email);

        // Assert
        result.Should().NotBeNull();
        result!.Email.Value.Should().Be(email);
    }

    #endregion

    #region User Update Tests

    [Fact]
    public async Task UpdateSceneName_ValidName_ShouldSucceed()
    {
        // Arrange
        var user = new UserBuilder().WithSceneName("OldName").Build();
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        var newSceneName = SceneName.Create("NewName");

        // Act
        user.UpdateSceneName(newSceneName);
        await _dbContext.SaveChangesAsync();

        // Assert
        var updatedUser = await _dbContext.Users.FindAsync(user.Id);
        updatedUser!.SceneName.Value.Should().Be("NewName");
        updatedUser.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void UpdateSceneName_NullName_ShouldThrowException()
    {
        // Arrange
        var user = new UserBuilder().Build();

        // Act
        var action = () => user.UpdateSceneName(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("newSceneName");
    }

    [Fact]
    public async Task UpdateEmail_ValidEmail_ShouldSucceed()
    {
        // Arrange
        var user = new UserBuilder().WithEmail("old@example.com").Build();
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        var newEmail = EmailAddress.Create("new@example.com");

        // Act
        user.UpdateEmail(newEmail);
        await _dbContext.SaveChangesAsync();

        // Assert
        var updatedUser = await _dbContext.Users.FindAsync(user.Id);
        updatedUser!.Email.Value.Should().Be("new@example.com");
    }

    #endregion

    #region User Status Tests

    [Fact]
    public async Task DeactivateUser_ActiveUser_ShouldSucceed()
    {
        // Arrange
        var user = new UserBuilder().Build();
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        user.Deactivate();
        await _dbContext.SaveChangesAsync();

        // Assert
        var updatedUser = await _dbContext.Users.FindAsync(user.Id);
        updatedUser!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task ReactivateUser_InactiveUser_ShouldSucceed()
    {
        // Arrange
        var user = new UserBuilder().Build();
        user.Deactivate();
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        user.Reactivate();
        await _dbContext.SaveChangesAsync();

        // Assert
        var updatedUser = await _dbContext.Users.FindAsync(user.Id);
        updatedUser!.IsActive.Should().BeTrue();
    }

    #endregion

    #region Role Management Tests

    [Fact]
    public void PromoteUser_ValidPromotion_ShouldSucceed()
    {
        // Arrange
        var user = new UserBuilder().WithRole(UserRole.Attendee).Build();

        // Act
        user.PromoteToRole(UserRole.Member);

        // Assert
        user.Role.Should().Be(UserRole.Member);
    }

    [Fact]
    public void PromoteUser_ToOrganizer_ShouldSucceed()
    {
        // Arrange
        var user = new UserBuilder().WithRole(UserRole.Member).Build();

        // Act
        user.PromoteToRole(UserRole.Organizer);

        // Assert
        user.Role.Should().Be(UserRole.Organizer);
    }

    [Fact]
    public void PromoteUser_InvalidDemotion_ShouldThrowException()
    {
        // Arrange
        var user = new UserBuilder().WithRole(UserRole.Organizer).Build();

        // Act
        var action = () => user.PromoteToRole(UserRole.Member);

        // Assert
        action.Should().Throw<DomainException>()
            .WithMessage("*Cannot demote user or assign same role*");
    }

    [Fact]
    public void PromoteUser_SameRole_ShouldThrowException()
    {
        // Arrange
        var user = new UserBuilder().WithRole(UserRole.Member).Build();

        // Act
        var action = () => user.PromoteToRole(UserRole.Member);

        // Assert
        action.Should().Throw<DomainException>()
            .WithMessage("*Cannot demote user or assign same role*");
    }

    #endregion

    #region Query Tests

    [Fact]
    public async Task QueryUsers_ByRole_ShouldReturnCorrectUsers()
    {
        // Arrange
        var organizer = new UserBuilder().WithRole(UserRole.Organizer).Build();
        var member1 = new UserBuilder().WithRole(UserRole.Member).Build();
        var member2 = new UserBuilder().WithRole(UserRole.Member).Build();
        var attendee = new UserBuilder().WithRole(UserRole.Attendee).Build();

        await _dbContext.Users.AddRangeAsync(organizer, member1, member2, attendee);
        await _dbContext.SaveChangesAsync();

        // Act
        var members = await _dbContext.Users
            .Where(u => u.Role == UserRole.Member)
            .ToListAsync();

        // Assert
        members.Should().HaveCount(2);
        members.Should().Contain(u => u.Id == member1.Id);
        members.Should().Contain(u => u.Id == member2.Id);
    }

    [Fact]
    public async Task QueryUsers_ActiveOnly_ShouldReturnOnlyActiveUsers()
    {
        // Arrange
        var activeUser = new UserBuilder().Build();
        var inactiveUser = new UserBuilder().Build();
        inactiveUser.Deactivate();

        await _dbContext.Users.AddRangeAsync(activeUser, inactiveUser);
        await _dbContext.SaveChangesAsync();

        // Act
        var activeUsers = await _dbContext.Users
            .Where(u => u.IsActive)
            .ToListAsync();

        // Assert
        activeUsers.Should().HaveCount(1);
        activeUsers.Should().Contain(u => u.Id == activeUser.Id);
    }

    #endregion

    public void Dispose()
    {
        _dbContext?.Dispose();
    }
}
using WitchCityRope.Api.Features.Authentication.Models;

namespace WitchCityRope.Tests.Common.Builders;

/// <summary>
/// Test data builder for UserDto following Vertical Slice Architecture patterns
/// Replaces UserBuilder from archived domain entities
/// Supports authentication and user management testing scenarios
/// </summary>
public class UserDtoBuilder
{
    private string _email = "testuser@witchcityrope.com";
    private string _sceneName = "TestUser";
    private string? _firstName = "Test";
    private string? _lastName = "User";
    private string[] _roles = { "Member" };
    private bool _isActive = true;
    private DateTime _createdAt = DateTime.UtcNow;
    private DateTime _updatedAt = DateTime.UtcNow;
    private DateTime? _lastLoginAt = null;

    public UserDtoBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public UserDtoBuilder WithSceneName(string sceneName)
    {
        _sceneName = sceneName;
        return this;
    }

    public UserDtoBuilder WithFirstName(string? firstName)
    {
        _firstName = firstName;
        return this;
    }

    public UserDtoBuilder WithLastName(string? lastName)
    {
        _lastName = lastName;
        return this;
    }

    public UserDtoBuilder WithRoles(string[] roles)
    {
        _roles = roles;
        return this;
    }

    public UserDtoBuilder AsAdmin()
    {
        _roles = new[] { "Admin" };
        return this;
    }

    public UserDtoBuilder AsTeacher()
    {
        _roles = new[] { "Teacher" };
        return this;
    }

    public UserDtoBuilder AsMember()
    {
        _roles = new[] { "Member" };
        return this;
    }

    public UserDtoBuilder AsGuest()
    {
        _roles = new[] { "Guest" };
        return this;
    }

    public UserDtoBuilder AsActive()
    {
        _isActive = true;
        return this;
    }

    public UserDtoBuilder AsInactive()
    {
        _isActive = false;
        return this;
    }

    public UserDtoBuilder WithCreatedAt(DateTime createdAt)
    {
        _createdAt = createdAt;
        return this;
    }

    public UserDtoBuilder WithLastLoginAt(DateTime? lastLoginAt)
    {
        _lastLoginAt = lastLoginAt;
        return this;
    }

    public UserDtoBuilder WithUniqueEmail()
    {
        _email = $"user-{Guid.NewGuid():N}@witchcityrope.com";
        return this;
    }

    public UserDtoBuilder WithUniqueSceneName()
    {
        _sceneName = $"User-{Guid.NewGuid():N}";
        return this;
    }

    /// <summary>
    /// Creates a UserDto for testing authentication scenarios
    /// </summary>
    public UserDto Build()
    {
        return new UserDto
        {
            Id = Guid.NewGuid().ToString(),
            Email = _email,
            SceneName = _sceneName,
            FirstName = _firstName,
            LastName = _lastName,
            Roles = _roles,
            IsActive = _isActive,
            CreatedAt = _createdAt.ToString("O"),
            UpdatedAt = _updatedAt.ToString("O"),
            LastLoginAt = _lastLoginAt?.ToString("O")
        };
    }

    /// <summary>
    /// Creates multiple UserDto instances for testing scenarios with multiple users
    /// </summary>
    public List<UserDto> Build(int count)
    {
        return Enumerable.Range(0, count)
            .Select(i => WithUniqueEmail().WithUniqueSceneName().Build())
            .ToList();
    }

    /// <summary>
    /// Creates a test admin user
    /// </summary>
    public static UserDto TestAdmin()
    {
        return new UserDtoBuilder()
            .WithEmail("admin@witchcityrope.com")
            .WithSceneName("AdminUser")
            .WithFirstName("Admin")
            .WithLastName("User")
            .AsAdmin()
            .Build();
    }

    /// <summary>
    /// Creates a test teacher user
    /// </summary>
    public static UserDto TestTeacher()
    {
        return new UserDtoBuilder()
            .WithEmail("teacher@witchcityrope.com")
            .WithSceneName("TeacherUser")
            .WithFirstName("Teacher")
            .WithLastName("User")
            .AsTeacher()
            .Build();
    }

    /// <summary>
    /// Creates a test vetted member user (NOTE: vetting status now tracked in ApplicationUser entity, not DTO)
    /// </summary>
    public static UserDto TestVettedMember()
    {
        return new UserDtoBuilder()
            .WithEmail("vetted@witchcityrope.com")
            .WithSceneName("VettedMember")
            .WithFirstName("Vetted")
            .WithLastName("Member")
            .AsMember()
            .Build();
    }

    /// <summary>
    /// Creates a test general member user (NOTE: vetting status now tracked in ApplicationUser entity, not DTO)
    /// </summary>
    public static UserDto TestMember()
    {
        return new UserDtoBuilder()
            .WithEmail("member@witchcityrope.com")
            .WithSceneName("GeneralMember")
            .WithFirstName("General")
            .WithLastName("Member")
            .AsMember()
            .Build();
    }

    /// <summary>
    /// Creates a test guest user
    /// </summary>
    public static UserDto TestGuest()
    {
        return new UserDtoBuilder()
            .WithEmail("guest@witchcityrope.com")
            .WithSceneName("GuestUser")
            .WithFirstName("Guest")
            .WithLastName("User")
            .AsGuest()
            .Build();
    }
}
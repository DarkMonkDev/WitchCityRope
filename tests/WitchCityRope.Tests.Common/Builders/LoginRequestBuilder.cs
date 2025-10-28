using WitchCityRope.Api.Features.Authentication.Models;

namespace WitchCityRope.Tests.Common.Builders;

/// <summary>
/// Test data builder for login requests
/// Supports authentication testing scenarios including credential validation
/// </summary>
public class LoginRequestBuilder
{
    private string _email = "testuser@witchcityrope.com";
    private string _password = "StrongPassword123!";

    public LoginRequestBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public LoginRequestBuilder WithPassword(string password)
    {
        _password = password;
        return this;
    }


    /// <summary>
    /// Sets credentials for the seeded admin user
    /// </summary>
    public LoginRequestBuilder AsAdmin()
    {
        _email = "admin@witchcityrope.com";
        _password = "Test123!";
        return this;
    }

    /// <summary>
    /// Sets credentials for the seeded teacher user
    /// </summary>
    public LoginRequestBuilder AsTeacher()
    {
        _email = "teacher@witchcityrope.com";
        _password = "Test123!";
        return this;
    }

    /// <summary>
    /// Sets credentials for the seeded vetted member user
    /// </summary>
    public LoginRequestBuilder AsVettedMember()
    {
        _email = "vetted@witchcityrope.com";
        _password = "Test123!";
        return this;
    }

    /// <summary>
    /// Sets credentials for the seeded general member user
    /// </summary>
    public LoginRequestBuilder AsMember()
    {
        _email = "member@witchcityrope.com";
        _password = "Test123!";
        return this;
    }

    /// <summary>
    /// Sets credentials for the seeded guest user
    /// </summary>
    public LoginRequestBuilder AsGuest()
    {
        _email = "guest@witchcityrope.com";
        _password = "Test123!";
        return this;
    }

    /// <summary>
    /// Sets empty email for validation testing
    /// </summary>
    public LoginRequestBuilder WithEmptyEmail()
    {
        _email = "";
        return this;
    }

    /// <summary>
    /// Sets invalid email format for validation testing
    /// </summary>
    public LoginRequestBuilder WithInvalidEmail()
    {
        _email = "not-an-email";
        return this;
    }

    /// <summary>
    /// Sets empty password for validation testing
    /// </summary>
    public LoginRequestBuilder WithEmptyPassword()
    {
        _password = "";
        return this;
    }

    /// <summary>
    /// Sets wrong password for authentication failure testing
    /// </summary>
    public LoginRequestBuilder WithWrongPassword()
    {
        _password = "WrongPassword123!";
        return this;
    }

    /// <summary>
    /// Sets non-existent user email for authentication failure testing
    /// </summary>
    public LoginRequestBuilder WithNonExistentUser()
    {
        _email = "nonexistent@witchcityrope.com";
        _password = "AnyPassword123!";
        return this;
    }

    /// <summary>
    /// Creates a LoginRequest for testing
    /// </summary>
    public LoginRequest Build()
    {
        return new LoginRequest
        {
            EmailOrSceneName = _email,
            Password = _password
        };
    }

    /// <summary>
    /// Creates a valid login request for successful authentication testing
    /// </summary>
    public static LoginRequest ValidRequest()
    {
        return new LoginRequestBuilder()
            .AsMember() // Use seeded member credentials
            .Build();
    }

    /// <summary>
    /// Creates an admin login request
    /// </summary>
    public static LoginRequest AdminRequest()
    {
        return new LoginRequestBuilder()
            .AsAdmin()
            .Build();
    }

    /// <summary>
    /// Creates a teacher login request
    /// </summary>
    public static LoginRequest TeacherRequest()
    {
        return new LoginRequestBuilder()
            .AsTeacher()
            .Build();
    }

    /// <summary>
    /// Creates a login request with invalid credentials
    /// </summary>
    public static LoginRequest InvalidRequest()
    {
        return new LoginRequestBuilder()
            .WithNonExistentUser()
            .Build();
    }

    /// <summary>
    /// Creates a login request with wrong password
    /// </summary>
    public static LoginRequest WrongPasswordRequest()
    {
        return new LoginRequestBuilder()
            .AsMember()
            .WithWrongPassword()
            .Build();
    }
}
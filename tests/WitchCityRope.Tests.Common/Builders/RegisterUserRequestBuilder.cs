using WitchCityRope.Api.Features.Authentication.Models;

namespace WitchCityRope.Tests.Common.Builders;

/// <summary>
/// Test data builder for user registration requests
/// Supports authentication testing scenarios including validation edge cases
/// </summary>
public class RegisterUserRequestBuilder
{
    private string _email = "testuser@witchcityrope.com";
    private string _password = "StrongPassword123!";
    private string _sceneName = "TestUser";

    public RegisterUserRequestBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public RegisterUserRequestBuilder WithPassword(string password)
    {
        _password = password;
        return this;
    }


    public RegisterUserRequestBuilder WithSceneName(string sceneName)
    {
        _sceneName = sceneName;
        return this;
    }


    public RegisterUserRequestBuilder WithUniqueEmail()
    {
        _email = $"user-{Guid.NewGuid():N}@witchcityrope.com";
        return this;
    }

    public RegisterUserRequestBuilder WithUniqueSceneName()
    {
        _sceneName = $"User-{Guid.NewGuid():N}";
        return this;
    }


    /// <summary>
    /// Sets weak password for validation testing
    /// </summary>
    public RegisterUserRequestBuilder WithWeakPassword(string weakPassword = "weak")
    {
        _password = weakPassword;
        return this;
    }

    /// <summary>
    /// Sets empty email for validation testing
    /// </summary>
    public RegisterUserRequestBuilder WithEmptyEmail()
    {
        _email = "";
        return this;
    }

    /// <summary>
    /// Sets invalid email format for validation testing
    /// </summary>
    public RegisterUserRequestBuilder WithInvalidEmail()
    {
        _email = "not-an-email";
        return this;
    }

    /// <summary>
    /// Sets empty scene name for validation testing
    /// </summary>
    public RegisterUserRequestBuilder WithEmptySceneName()
    {
        _sceneName = "";
        return this;
    }

    /// <summary>
    /// Creates a RegisterRequest for testing
    /// </summary>
    public RegisterRequest Build()
    {
        return new RegisterRequest
        {
            Email = _email,
            Password = _password,
            SceneName = _sceneName
        };
    }

    /// <summary>
    /// Creates a valid registration request for successful registration testing
    /// </summary>
    public static RegisterRequest ValidRequest()
    {
        return new RegisterUserRequestBuilder()
            .WithUniqueEmail()
            .WithUniqueSceneName()
            .Build();
    }

    /// <summary>
    /// Creates an admin registration request
    /// </summary>
    public static RegisterRequest AdminRequest()
    {
        return new RegisterUserRequestBuilder()
            .WithEmail("newadmin@witchcityrope.com")
            .WithSceneName("NewAdmin")
            .Build();
    }

    /// <summary>
    /// Creates a teacher registration request
    /// </summary>
    public static RegisterRequest TeacherRequest()
    {
        return new RegisterUserRequestBuilder()
            .WithEmail("newteacher@witchcityrope.com")
            .WithSceneName("NewTeacher")
            .Build();
    }

    /// <summary>
    /// Creates multiple unique registration requests for bulk testing
    /// </summary>
    public static List<RegisterRequest> Multiple(int count)
    {
        return Enumerable.Range(0, count)
            .Select(i => new RegisterUserRequestBuilder()
                .WithUniqueEmail()
                .WithUniqueSceneName()
                .Build())
            .ToList();
    }
}
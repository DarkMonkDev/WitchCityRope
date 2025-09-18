using Microsoft.Extensions.Logging;
using Moq;
using WitchCityRope.Tests.Common.Fixtures;

namespace WitchCityRope.Tests.Common.TestBase;

/// <summary>
/// Base class for testing individual feature services in Vertical Slice Architecture
/// Provides common service setup patterns and mocked dependencies
/// </summary>
public abstract class FeatureTestBase<TService> : DatabaseTestBase
    where TService : class
{
    protected Mock<ILogger<TService>> MockLogger { get; private set; }
    protected TService Service { get; private set; }

    protected FeatureTestBase(DatabaseTestFixture fixture) : base(fixture)
    {
        MockLogger = new Mock<ILogger<TService>>();
        Service = CreateService();
    }

    /// <summary>
    /// Create the service instance with required dependencies
    /// Override this method in derived classes to provide specific service construction
    /// </summary>
    protected abstract TService CreateService();

    /// <summary>
    /// Helper method for testing service methods that return Result pattern
    /// </summary>
    protected async Task<(bool Success, TResponse? Response, string Error)> TestServiceMethod<TRequest, TResponse>(
        Func<TService, TRequest, Task<(bool Success, TResponse? Response, string Error)>> serviceMethod,
        TRequest request)
    {
        // Act
        var result = await serviceMethod(Service, request);

        // Log result for debugging
        if (!result.Success)
        {
            MockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(result.Error)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        return result;
    }

    /// <summary>
    /// Helper method for testing service methods with standard service signature
    /// </summary>
    protected async Task<TResponse> TestServiceMethodWithResponse<TRequest, TResponse>(
        Func<TService, TRequest, Task<TResponse>> serviceMethod,
        TRequest request)
    {
        return await serviceMethod(Service, request);
    }

    /// <summary>
    /// Helper method for testing service methods without parameters
    /// </summary>
    protected async Task<TResponse> TestServiceMethodWithoutParams<TResponse>(
        Func<TService, Task<TResponse>> serviceMethod)
    {
        return await serviceMethod(Service);
    }

    /// <summary>
    /// Verify that error logging occurred
    /// </summary>
    protected void VerifyErrorLogged(string expectedErrorMessage)
    {
        MockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedErrorMessage)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    /// <summary>
    /// Verify that information logging occurred
    /// </summary>
    protected void VerifyInfoLogged(string expectedMessage)
    {
        MockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    /// <summary>
    /// Verify that warning logging occurred
    /// </summary>
    protected void VerifyWarningLogged(string expectedMessage)
    {
        MockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    /// <summary>
    /// Verify that no error logging occurred
    /// </summary>
    protected void VerifyNoErrorsLogged()
    {
        MockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    /// <summary>
    /// Reset all mocks for clean test state
    /// </summary>
    protected void ResetMocks()
    {
        MockLogger.Reset();
    }
}
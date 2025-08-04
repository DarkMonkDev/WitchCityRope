using Moq;
using System.Net;
using System.Net.Http;
using WitchCityRope.Core.Models;
using WitchCityRope.Web.Services;

namespace WitchCityRope.Web.Tests.New.Helpers;

using WitchCityRope.Web.Tests.New.TestData;

/// <summary>
/// Helper methods for creating common service mocks
/// </summary>
public static class ServiceMockHelper
{
    /// <summary>
    /// Creates a mock ApiClient with common setup
    /// </summary>
    public static Mock<ApiClient> CreateApiClientMock()
    {
        var mock = new Mock<ApiClient>();
        
        // Setup default behavior for common methods
        mock.Setup(x => x.GetAsync<It.IsAnyType>(It.IsAny<string>()))
            .ReturnsAsync((object?)null);
            
        mock.Setup(x => x.PostAsync<It.IsAnyType>(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync((object?)null);
            
        mock.Setup(x => x.PutAsync<It.IsAnyType>(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync((object?)null);
            
        mock.Setup(x => x.DeleteAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        return mock;
    }

    /// <summary>
    /// Creates a mock INotificationService
    /// </summary>
    public static Mock<INotificationService> CreateNotificationServiceMock()
    {
        var mock = new Mock<INotificationService>();
        
        mock.Setup(x => x.ShowSuccess(It.IsAny<string>()))
            .Verifiable();
            
        mock.Setup(x => x.ShowError(It.IsAny<string>()))
            .Verifiable();
            
        mock.Setup(x => x.ShowWarning(It.IsAny<string>()))
            .Verifiable();
            
        mock.Setup(x => x.ShowInfo(It.IsAny<string>()))
            .Verifiable();

        return mock;
    }


    /// <summary>
    /// Creates a mock HttpClient with response setup
    /// </summary>
    public static Mock<HttpClient> CreateHttpClientMock(
        HttpStatusCode statusCode = HttpStatusCode.OK,
        string? responseContent = null)
    {
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        var response = new HttpResponseMessage
        {
            StatusCode = statusCode,
            Content = responseContent != null 
                ? new StringContent(responseContent) 
                : new StringContent("{}")
        };

        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var httpClient = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://test.com/")
        };

        return new Mock<HttpClient>(() => httpClient);
    }

    /// <summary>
    /// Creates a paginated result for testing
    /// </summary>
    public static PagedResult<T> CreatePagedResult<T>(
        IEnumerable<T> items,
        int totalCount = -1,
        int pageNumber = 1,
        int pageSize = 10)
    {
        var itemsList = items.ToList();
        return new PagedResult<T>
        {
            Items = itemsList,
            TotalCount = totalCount >= 0 ? totalCount : itemsList.Count,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    // Note: ApiResponse type is not available in the Web project - it's in the API project

    /// <summary>
    /// Sets up a successful API call mock
    /// </summary>
    public static void SetupSuccessfulGet<T>(Mock<ApiClient> apiClientMock, 
        string endpoint, 
        T response)
    {
        apiClientMock
            .Setup(x => x.GetAsync<T>(endpoint))
            .ReturnsAsync(response);
    }

    /// <summary>
    /// Sets up a failed API call mock
    /// </summary>
    public static void SetupFailedApiCall(Mock<ApiClient> apiClientMock, 
        string endpoint,
        string errorMessage = "API call failed")
    {
        apiClientMock
            .Setup(x => x.GetAsync<It.IsAnyType>(endpoint))
            .ThrowsAsync(new HttpRequestException(errorMessage));
    }

    /// <summary>
    /// Verifies that a toast notification was shown
    /// </summary>
    public static void VerifyToastShown(Mock<IToastService> mock, 
        string message, 
        NotificationType type = NotificationType.Success)
    {
        switch (type)
        {
            case NotificationType.Success:
                mock.Verify(x => x.ShowSuccess(message), Times.Once);
                break;
            case NotificationType.Error:
                mock.Verify(x => x.ShowError(message), Times.Once);
                break;
            case NotificationType.Warning:
                mock.Verify(x => x.ShowWarning(message), Times.Once);
                break;
            case NotificationType.Info:
                mock.Verify(x => x.ShowInfo(message), Times.Once);
                break;
        }
    }
}
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WitchCityRope.Core.DTOs;
using WitchCityRope.Core.Models;
using WitchCityRope.Web.Models;
using WitchCityRope.Web.Services;
using Xunit;

namespace WitchCityRope.Web.Tests.New.Services;

/// <summary>
/// Tests for ApiClient service
/// </summary>
public class ApiClientTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<ApiClient>> _loggerMock;
    private readonly ApiClient _apiClient;

    public ApiClientTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("https://api.test.com/")
        };
        
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<ApiClient>>();
        
        _apiClient = new ApiClient(_httpClient, _configurationMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetAsync_WithSuccessfulResponse_ReturnsDeserializedObject()
    {
        // Arrange
        var expectedEvent = new EventDto
        {
            Id = Guid.NewGuid(),
            Name = "Test Event",
            Description = "Test Description",
            StartDateTime = DateTime.UtcNow.AddDays(7),
            EndDateTime = DateTime.UtcNow.AddDays(7).AddHours(2)
        };
        
        var jsonResponse = JsonSerializer.Serialize(expectedEvent);
        
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse)
            });

        // Act
        var result = await _apiClient.GetAsync<EventDto>("api/events/1");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(expectedEvent.Id);
        result.Name.Should().Be(expectedEvent.Name);
        result.Description.Should().Be(expectedEvent.Description);
    }

    [Fact]
    public async Task GetAsync_WithNotFoundResponse_ReturnsNull()
    {
        // Arrange
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound
            });

        // Act
        var result = await _apiClient.GetAsync<EventDto>("api/events/999");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_WithServerError_ThrowsHttpRequestException()
    {
        // Arrange
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                ReasonPhrase = "Internal Server Error"
            });

        // Act & Assert
        await _apiClient.Invoking(x => x.GetAsync<EventDto>("api/events/1"))
            .Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task PostAsync_WithValidData_ReturnsCreatedObject()
    {
        // Arrange
        var createRequest = new { Name = "New Event", Date = DateTime.UtcNow };
        var createdEvent = new EventDto
        {
            Id = Guid.NewGuid(),
            Name = "New Event"
        };
        
        var jsonResponse = JsonSerializer.Serialize(createdEvent);
        
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().EndsWith("api/events")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Created,
                Content = new StringContent(jsonResponse)
            });

        // Act
        var result = await _apiClient.PostAsync<EventDto>("api/events", createRequest);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(createdEvent.Id);
        result.Name.Should().Be(createdEvent.Name);
    }

    [Fact]
    public async Task PostAsync_WithBadRequest_ThrowsHttpRequestException()
    {
        // Arrange
        var invalidRequest = new { Name = "" }; // Invalid data
        
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("{\"errors\":{\"Name\":[\"Name is required\"]}}")
            });

        // Act & Assert
        await _apiClient.Invoking(x => x.PostAsync<EventDto>("api/events", invalidRequest))
            .Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task PutAsync_WithValidData_ReturnsUpdatedObject()
    {
        // Arrange
        var updateRequest = new { Name = "Updated Event" };
        var updatedEvent = new EventDto
        {
            Id = Guid.NewGuid(),
            Name = "Updated Event"
        };
        
        var jsonResponse = JsonSerializer.Serialize(updatedEvent);
        
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Put),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse)
            });

        // Act
        var result = await _apiClient.PutAsync<EventDto>("api/events/1", updateRequest);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated Event");
    }

    [Fact]
    public async Task DeleteAsync_WithExistingResource_ReturnsTrue()
    {
        // Arrange
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Delete),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NoContent
            });

        // Act
        var result = await _apiClient.DeleteAsync("api/events/1");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_WithNotFoundResource_ReturnsFalse()
    {
        // Arrange
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Delete),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound
            });

        // Act
        var result = await _apiClient.DeleteAsync("api/events/999");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetEventsAsync_WithPagination_ReturnsPagedResult()
    {
        // Arrange
        var events = new[]
        {
            new EventDto { Id = Guid.NewGuid(), Name = "Event 1" },
            new EventDto { Id = Guid.NewGuid(), Name = "Event 2" }
        };
        
        var pagedResult = new PagedResult<EventDto>
        {
            Items = events,
            TotalCount = 10,
            PageNumber = 1,
            PageSize = 2
        };
        
        var jsonResponse = JsonSerializer.Serialize(pagedResult);
        
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri!.ToString().Contains("pageNumber=1") &&
                    req.RequestUri.ToString().Contains("pageSize=2")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse)
            });

        // Act
        var result = await _apiClient.GetEventsAsync(pageNumber: 1, pageSize: 2);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(10);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(2);
    }

    [Fact]
    public async Task GetEventAsync_WithValidId_ReturnsEvent()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var eventDto = new EventDto
        {
            Id = eventId,
            Name = "Test Event"
        };
        
        var jsonResponse = JsonSerializer.Serialize(eventDto);
        
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri!.ToString().Contains($"events/{eventId}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse)
            });

        // Act
        var result = await _apiClient.GetEventAsync(eventId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(eventId);
        result.Name.Should().Be("Test Event");
    }

    [Fact]
    public async Task CreateEventAsync_WithValidRequest_ReturnsCreatedEvent()
    {
        // Arrange
        var request = new CreateEventRequest
        {
            Name = "New Event",
            Description = "Description",
            StartDateTime = DateTime.UtcNow.AddDays(7),
            EndDateTime = DateTime.UtcNow.AddDays(7).AddHours(2)
        };
        
        var createdEvent = new EventDto
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description
        };
        
        var jsonResponse = JsonSerializer.Serialize(createdEvent);
        
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Created,
                Content = new StringContent(jsonResponse)
            });

        // Act
        var result = await _apiClient.CreateEventAsync(request);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be(request.Name);
        result.Description.Should().Be(request.Description);
    }

    [Fact]
    public async Task GetCurrentUserAsync_WhenAuthenticated_ReturnsUser()
    {
        // Arrange
        var user = new UserDto
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            SceneName = "TestUser"
        };
        
        var jsonResponse = JsonSerializer.Serialize(user);
        
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri!.ToString().Contains("users/current")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse)
            });

        // Act
        var result = await _apiClient.GetCurrentUserAsync();

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("test@example.com");
        result.SceneName.Should().Be("TestUser");
    }

    [Fact]
    public async Task SetAuthToken_AddsAuthorizationHeader()
    {
        // Arrange
        var token = "test-jwt-token";
        HttpRequestMessage? capturedRequest = null;
        
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{}")
            });

        // Act
        _apiClient.SetAuthToken(token);
        await _apiClient.GetAsync<object>("api/test");

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Headers.Authorization.Should().NotBeNull();
        capturedRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
        capturedRequest.Headers.Authorization.Parameter.Should().Be(token);
    }

    [Fact]
    public async Task ClearAuthToken_RemovesAuthorizationHeader()
    {
        // Arrange
        _apiClient.SetAuthToken("test-token");
        HttpRequestMessage? capturedRequest = null;
        
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{}")
            });

        // Act
        _apiClient.ClearAuthToken();
        await _apiClient.GetAsync<object>("api/test");

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Headers.Authorization.Should().BeNull();
    }
}
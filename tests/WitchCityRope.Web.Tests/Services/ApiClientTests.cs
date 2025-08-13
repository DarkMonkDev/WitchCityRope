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
using WitchCityRope.Core.Enums;
using WitchCityRope.Web.Models;
using WitchCityRope.Web.Services;
using Xunit;

namespace WitchCityRope.Web.Tests.Services;

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
        
        _apiClient = new ApiClient(_httpClient, _loggerMock.Object);
    }

    [Fact]
    public async Task GetAsync_WithSuccessfulResponse_ReturnsDeserializedObject()
    {
        // Arrange
        var expectedEvent = new Core.DTOs.EventDto
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
        var result = await _apiClient.GetAsync<Core.DTOs.EventDto>("api/events/1");

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
        var result = await _apiClient.GetAsync<Core.DTOs.EventDto>("api/events/999");

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
        await _apiClient.Invoking(x => x.GetAsync<Core.DTOs.EventDto>("api/events/1"))
            .Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task PostAsync_WithValidData_ReturnsCreatedObject()
    {
        // Arrange
        var createRequest = new { Name = "New Event", Date = DateTime.UtcNow };
        var createdEvent = new Core.DTOs.EventDto
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
        var result = await _apiClient.PostAsync<object, Core.DTOs.EventDto>("api/events", createRequest);

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
        await _apiClient.Invoking(x => x.PostAsync<object, Core.DTOs.EventDto>("api/events", invalidRequest))
            .Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task PutAsync_WithValidData_ReturnsUpdatedObject()
    {
        // Arrange
        var updateRequest = new { Name = "Updated Event" };
        var updatedEvent = new Core.DTOs.EventDto
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
        var result = await _apiClient.PutAsync<object, Core.DTOs.EventDto>("api/events/1", updateRequest);

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
        var response = await _apiClient.DeleteAsync<object>("api/events/1");
        var result = response == null; // NoContent returns null

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
        await _apiClient.Invoking(x => x.DeleteAsync<object>("api/events/999"))
            .Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetEventsAsync_WithPagination_ReturnsEventList()
    {
        // Arrange
        var events = new[]
        {
            new EventSummaryDto { Id = Guid.NewGuid(), Title = "Event 1" },
            new EventSummaryDto { Id = Guid.NewGuid(), Title = "Event 2" }
        };
        
        var response = new ListEventsResponse
        {
            Events = events.ToList(),
            TotalCount = 10,
            Page = 1,
            PageSize = 2
        };
        
        var jsonResponse = JsonSerializer.Serialize(response);
        
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri!.ToString().Contains("page=1") &&
                    req.RequestUri.ToString().Contains("pageSize=2")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse)
            });

        // Act
        var result = await _apiClient.GetEventsAsync(page: 1, pageSize: 2);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetEventByIdAsync_WithValidId_ReturnsEvent()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var eventDto = new Core.DTOs.EventDto
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
        var result = await _apiClient.GetEventByIdAsync(eventId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(eventId);
        result.Name.Should().Be("Test Event");
    }

    [Fact]
    public async Task CreateEventAsync_WithValidRequest_ReturnsCreatedEvent()
    {
        // Arrange
        var request = new EventFormModel
        {
            Title = "New Event",
            Description = "Description",
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(2),
            Type = "Workshop",
            Location = "Test Location",
            Capacity = 50,
            Price = 25.00m
        };
        
        var createdEvent = new EventFormModel
        {
            Title = request.Title,
            Description = request.Description,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Type = request.Type,
            Location = request.Location,
            Capacity = request.Capacity,
            Price = request.Price
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
        result!.Title.Should().Be(request.Title);
        result.Description.Should().Be(request.Description);
    }

    #region User Management Tests

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetUsersAsync_WithSearchParameters_ReturnsPagedResult()
    {
        // Arrange
        var users = new List<AdminUserDto>
        {
            new AdminUserDto
            {
                Id = Guid.NewGuid(),
                SceneName = "TestUser1",
                Email = "test1@example.com",
                Role = Core.Enums.UserRole.Member,
                IsActive = true,
                IsVetted = true,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-6)
            },
            new AdminUserDto
            {
                Id = Guid.NewGuid(),
                SceneName = "TestUser2",
                Email = "test2@example.com",
                Role = Core.Enums.UserRole.Member,
                IsActive = true,
                IsVetted = false,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-3)
            }
        };
        
        var pagedResult = new PagedUserResult
        {
            Users = users,
            TotalCount = 2,
            CurrentPage = 1,
            PageSize = 50
        };
        
        var jsonResponse = JsonSerializer.Serialize(pagedResult);
        
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri!.ToString().Contains("api/admin/users") &&
                    req.RequestUri.ToString().Contains("searchTerm=test") &&
                    req.RequestUri.ToString().Contains("page=1")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse)
            });

        var searchRequest = new UserSearchRequest
        {
            SearchTerm = "test",
            Page = 1,
            PageSize = 50,
            SortBy = "sceneName",
            SortDirection = "asc"
        };

        // Act
        var result = await _apiClient.GetUsersAsync(searchRequest);

        // Assert
        result.Should().NotBeNull();
        result!.Users.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.CurrentPage.Should().Be(1);
        result.Users.First().SceneName.Should().Be("TestUser1");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetUsersAsync_WithRoleFilter_BuildsCorrectQuery()
    {
        // Arrange
        var pagedResult = new PagedUserResult
        {
            Users = new List<AdminUserDto>(),
            TotalCount = 0,
            CurrentPage = 1,
            PageSize = 50
        };
        
        var jsonResponse = JsonSerializer.Serialize(pagedResult);
        
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri!.ToString().Contains("api/admin/users") &&
                    req.RequestUri.ToString().Contains("role=2") && // Member = 2
                    req.RequestUri.ToString().Contains("isVetted=true")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse)
            });

        var searchRequest = new UserSearchRequest
        {
            Role = Core.Enums.UserRole.Member,
            IsVetted = true,
            Page = 1,
            PageSize = 50
        };

        // Act
        var result = await _apiClient.GetUsersAsync(searchRequest);

        // Assert
        result.Should().NotBeNull();
        
        // Verify the HTTP request was made with correct parameters
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.RequestUri!.ToString().Contains("role=2") &&
                req.RequestUri.ToString().Contains("isVetted=true")),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetUsersAsync_WithHttpError_ThrowsException()
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
                Content = new StringContent("Server error")
            });

        var searchRequest = new UserSearchRequest
        {
            Page = 1,
            PageSize = 50
        };

        // Act & Assert
        await _apiClient.Invoking(x => x.GetUsersAsync(searchRequest))
            .Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetUserByIdAsync_WithValidId_ReturnsUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new AdminUserDto
        {
            Id = userId,
            SceneName = "TestUser",
            Email = "test@example.com",
            Role = Core.Enums.UserRole.Member,
            IsActive = true,
            IsVetted = true,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-6),
            Pronouns = "they/them",
            PronouncedName = "Test User"
        };
        
        var jsonResponse = JsonSerializer.Serialize(user);
        
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri!.ToString().Contains($"api/admin/users/{userId}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse)
            });

        // Act
        var result = await _apiClient.GetUserByIdAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(userId);
        result.SceneName.Should().Be("TestUser");
        result.Email.Should().Be("test@example.com");
        result.Role.Should().Be(Core.Enums.UserRole.Member);
        result.Pronouns.Should().Be("they/them");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetUserByIdAsync_WithNotFoundId_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri!.ToString().Contains($"api/admin/users/{userId}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = new StringContent("User not found")
            });

        // Act & Assert
        await _apiClient.Invoking(x => x.GetUserByIdAsync(userId))
            .Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpdateUserAsync_WithValidData_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var updateDto = new UpdateUserDto
        {
            SceneName = "UpdatedUser",
            Role = Core.Enums.UserRole.Organizer,
            IsActive = true,
            IsVetted = true,
            Pronouns = "she/her",
            AdminNote = "Updated user role due to event organizer application"
        };
        
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Put &&
                    req.RequestUri!.ToString().Contains($"api/admin/users/{userId}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NoContent
            });

        // Act
        var result = await _apiClient.UpdateUserAsync(userId, updateDto);

        // Assert
        result.Should().BeTrue();
        
        // Verify the HTTP request was made
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Put &&
                req.RequestUri!.ToString().Contains($"api/admin/users/{userId}")),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpdateUserAsync_WithServerError_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var updateDto = new UpdateUserDto
        {
            SceneName = "UpdatedUser"
        };
        
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("Validation failed")
            });

        // Act
        var result = await _apiClient.UpdateUserAsync(userId, updateDto);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ResetUserPasswordAsync_WithValidData_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var resetDto = new ResetUserPasswordDto
        {
            NewPassword = "NewSecurePassword123!",
            RequirePasswordChangeOnLogin = true,
            AdminNote = "Password reset requested due to account compromise"
        };
        
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains($"api/admin/users/{userId}/reset-password")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NoContent
            });

        // Act
        var result = await _apiClient.ResetUserPasswordAsync(userId, resetDto);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ManageUserLockoutAsync_LockUser_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var lockoutDto = new UserLockoutDto
        {
            IsLocked = true,
            LockoutEnd = DateTime.UtcNow.AddDays(30),
            Reason = "Violation of community guidelines"
        };
        
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains($"api/admin/users/{userId}/lockout")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NoContent
            });

        // Act
        var result = await _apiClient.ManageUserLockoutAsync(userId, lockoutDto);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetUserStatsAsync_WithValidResponse_ReturnsStats()
    {
        // Arrange
        var stats = new UserStatsDto
        {
            TotalUsers = 150,
            PendingVetting = 12,
            OnHold = 3,
            CalculatedAt = DateTime.UtcNow,
            UsersByRole = new Dictionary<string, int>
            {
                { "Member", 120 },
                { "Organizer", 20 },
                { "Administrator", 5 },
                { "Attendee", 5 }
            }
        };
        
        var jsonResponse = JsonSerializer.Serialize(stats);
        
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri!.ToString().Contains("api/admin/users/stats")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse)
            });

        // Act
        var result = await _apiClient.GetUserStatsAsync();

        // Assert
        result.Should().NotBeNull();
        result!.TotalUsers.Should().Be(150);
        result.PendingVetting.Should().Be(12);
        result.OnHold.Should().Be(3);
        result.UsersByRole.Should().ContainKey("Member");
        result.UsersByRole["Member"].Should().Be(120);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetAvailableRolesAsync_WithValidResponse_ReturnsRoles()
    {
        // Arrange
        var roles = new List<RoleDto>
        {
            new RoleDto { Name = "Attendee", DisplayName = "Attendee", Description = "Standard event attendee", Priority = 1 },
            new RoleDto { Name = "Member", DisplayName = "Member", Description = "Verified community member", Priority = 2 },
            new RoleDto { Name = "Organizer", DisplayName = "Organizer", Description = "Event organizer", Priority = 3 },
            new RoleDto { Name = "Administrator", DisplayName = "Administrator", Description = "System administrator", Priority = 4 }
        };
        
        var jsonResponse = JsonSerializer.Serialize(roles);
        
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri!.ToString().Contains("api/admin/users/roles")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse)
            });

        // Act
        var result = await _apiClient.GetAvailableRolesAsync();

        // Assert
        result.Should().NotBeNull();
        result!.Should().HaveCount(4);
        result.Should().Contain(r => r.Name == "Member");
        result.Should().Contain(r => r.Name == "Administrator");
        result.First(r => r.Name == "Member").Description.Should().Be("Verified community member");
    }

    #endregion


}
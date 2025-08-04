using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Enums;
using WitchCityRope.Core.Models;
using WitchCityRope.Core.DTOs;
using WitchCityRope.Web.Models;

namespace WitchCityRope.Web.Tests.TestData;

/// <summary>
/// Builder class for creating test data
/// </summary>
public static class TestDataBuilder
{
    /// <summary>
    /// Creates a test EventDto
    /// </summary>
    public static Core.DTOs.EventDto CreateEvent(
        Guid? id = null,
        string? name = null,
        DateTime? startDateTime = null,
        DateTime? endDateTime = null,
        string? location = null,
        string eventType = "Workshop",
        string? status = null,
        int maxAttendees = 20,
        int currentAttendees = 0)
    {
        var eventId = id ?? Guid.NewGuid();
        var start = startDateTime ?? DateTime.Now.AddDays(7);
        var end = endDateTime ?? start.AddHours(2);
        
        return new Core.DTOs.EventDto
        {
            Id = eventId,
            Name = name ?? $"Test Event {eventId.ToString()[..8]}",
            StartDateTime = start,
            EndDateTime = end,
            Location = location ?? "Test Location",
            EventType = eventType,
            Status = status ?? "Open",
            MaxAttendees = maxAttendees,
            CurrentAttendees = currentAttendees,
            Price = 50.00m,
            Description = "Test event description",
            Tags = new List<string> { "beginner", "workshop" },
            RequiredSkillLevels = new List<string> { "beginner" },
            RequiresVetting = false
        };
    }

    /// <summary>
    /// Creates multiple test events
    /// </summary>
    public static List<Core.DTOs.EventDto> CreateEvents(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => CreateEvent(id: Guid.NewGuid(), name: $"Event {i}"))
            .ToList();
    }

    /// <summary>
    /// Creates a test UserDto
    /// </summary>
    public static UserDto CreateUser(
        Guid? id = null,
        string? email = null,
        string? displayName = null,
        string? sceneName = null,
        string[]? roles = null)
    {
        var userId = id ?? Guid.NewGuid();
        return new UserDto
        {
            Id = userId,
            Email = email ?? $"user{userId.ToString()[..8]}@example.com",
            DisplayName = displayName ?? "Test User",
            SceneName = sceneName ?? "TestScene",
            Roles = roles?.ToList() ?? new List<string> { "Member" },
            IsActive = true,
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a test member profile
    /// </summary>
    public static MemberProfileDto CreateMemberProfile(
        string? id = null,
        string? displayName = null,
        string? bio = null,
        string? experience = null,
        bool isPublic = true)
    {
        return new MemberProfileDto
        {
            Id = id ?? Guid.NewGuid().ToString(),
            DisplayName = displayName ?? "TestMember",
            Bio = bio ?? "Test bio",
            Experience = experience ?? "Beginner",
            IsPublic = isPublic,
            JoinedDate = DateTime.Now.AddMonths(-6),
            EventsAttended = Random.Shared.Next(0, 20)
        };
    }

    /// <summary>
    /// Creates an attendance record
    /// </summary>
    public static AttendanceDto CreateAttendance(
        int? eventId = null,
        Guid? userId = null,
        AttendanceStatus status = AttendanceStatus.Confirmed)
    {
        return new AttendanceDto
        {
            EventId = eventId ?? Random.Shared.Next(1, 100),
            UserId = userId?.ToString() ?? Guid.NewGuid().ToString(),
            Status = status,
            RegisteredAt = DateTime.Now
        };
    }

    /// <summary>
    /// Creates a notification
    /// </summary>
    public static NotificationDto CreateNotification(
        string? id = null,
        string? title = null,
        string? message = null,
        NotificationType type = NotificationType.Info,
        bool isRead = false)
    {
        return new NotificationDto
        {
            Id = id ?? Guid.NewGuid().ToString(),
            Title = title ?? "Test Notification",
            Message = message ?? "This is a test notification",
            Type = type,
            IsRead = isRead,
            CreatedAt = DateTime.Now
        };
    }

    // ApiResponse methods removed - type not available in current project structure

    /// <summary>
    /// Creates a paged result
    /// </summary>
    public static PagedResult<T> CreatePagedResult<T>(
        IEnumerable<T> items,
        int pageNumber = 1,
        int pageSize = 10,
        int? totalCount = null)
    {
        var itemsList = items.ToList();
        return new PagedResult<T>
        {
            Items = itemsList,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount ?? itemsList.Count,
            // TotalPages is a computed property, cannot be set directly
        };
    }

    /// <summary>
    /// Creates test authentication tokens
    /// </summary>
    public static AuthTokens CreateAuthTokens(
        string? accessToken = null,
        string? refreshToken = null,
        DateTime? expiresAt = null)
    {
        return new AuthTokens
        {
            AccessToken = accessToken ?? $"test_access_token_{Guid.NewGuid()}",
            RefreshToken = refreshToken ?? $"test_refresh_token_{Guid.NewGuid()}",
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddHours(1)
        };
    }

    /// <summary>
    /// Creates a login response
    /// </summary>
    public static LoginResponseDto CreateLoginResponse(
        UserDto? user = null,
        AuthTokens? tokens = null)
    {
        return new LoginResponseDto
        {
            User = user ?? CreateUser(),
            Tokens = tokens ?? CreateAuthTokens(),
            Success = true
        };
    }
}

// Placeholder DTOs and enums - these would come from your Shared project
public class MemberProfileDto
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string Experience { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
    public DateTime JoinedDate { get; set; }
    public int EventsAttended { get; set; }
}

public class AttendanceDto
{
    public int EventId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public AttendanceStatus Status { get; set; }
    public DateTime RegisteredAt { get; set; }
}

public enum AttendanceStatus
{
    Pending,
    Confirmed,
    Cancelled,
    Attended,
    NoShow
}

public class NotificationDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error
}

public class AuthTokens
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public class LoginResponseDto
{
    public UserDto User { get; set; } = new();
    public AuthTokens Tokens { get; set; } = new();
    public bool Success { get; set; }
}
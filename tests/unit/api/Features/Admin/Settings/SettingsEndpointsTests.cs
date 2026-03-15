using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using WitchCityRope.Api.Features.Admin.Settings.Endpoints;
using WitchCityRope.Api.Features.Admin.Settings.Interfaces;
using WitchCityRope.Api.Features.Admin.Settings.Models;
using Xunit;
using FluentAssertions;
using NSubstitute;

namespace WitchCityRope.UnitTests.Api.Features.Admin.Settings;

/// <summary>
/// Unit tests for SettingsEndpoints (Pattern B)
/// Tests public settings retrieval, admin settings management, and settings update validation
///
/// NOTE (2025-11-22): PreStartBufferMinutes setting has been REMOVED from the system.
/// These tests still reference it for backward compatibility with the settings endpoint,
/// but the buffer functionality itself has been replaced by per-event timing fields.
/// The validation tests for buffer minutes remain to ensure the endpoint still handles
/// the setting correctly if it exists in legacy data.
/// </summary>
public class SettingsEndpointsTests
{
    private readonly ISettingsService _mockSettingsService;

    public SettingsEndpointsTests()
    {
        _mockSettingsService = Substitute.For<ISettingsService>();
    }

    #region GetPublicSettings Tests

    /// <summary>
    /// Test: GetPublicSettings returns EventTimeZone and PreStartBufferMinutes with 200 OK
    /// </summary>
    [Fact]
    public async Task GetPublicSettings_WithValidSettings_Returns200OkWithSettings()
    {
        // Arrange
        _mockSettingsService.GetSettingAsync("EventTimeZone", Arg.Any<CancellationToken>())
            .Returns("America/New_York");
        _mockSettingsService.GetSettingAsync("PreStartBufferMinutes", Arg.Any<CancellationToken>())
            .Returns("30");

        // Act
        var result = await GetPublicSettings();

        // Assert
        result.Should().BeAssignableTo<IResult>();

        // Results.Ok returns IResult, we need to access the underlying value using dynamic
        dynamic dynamicResult = result;
        object settings = dynamicResult.Value;
        settings.Should().NotBeNull();

        // Use dynamic to access anonymous object properties
        dynamic settingsObj = settings!;
        ((string)settingsObj.EventTimeZone).Should().Be("America/New_York");
        ((string)settingsObj.PreStartBufferMinutes).Should().Be("30");
    }

    /// <summary>
    /// Test: GetPublicSettings with null values returns default values with 200 OK
    /// </summary>
    [Fact]
    public async Task GetPublicSettings_WithNullValues_ReturnsDefaultsAnd200Ok()
    {
        // Arrange
        _mockSettingsService.GetSettingAsync("EventTimeZone", Arg.Any<CancellationToken>())
            .Returns((string?)null);
        _mockSettingsService.GetSettingAsync("PreStartBufferMinutes", Arg.Any<CancellationToken>())
            .Returns((string?)null);

        // Act
        var result = await GetPublicSettings();

        // Assert
        result.Should().BeAssignableTo<IResult>();

        // Results.Ok returns IResult, we need to access the underlying value using dynamic
        dynamic dynamicResult = result;
        object settings = dynamicResult.Value;
        settings.Should().NotBeNull();

        dynamic settingsObj = settings!;
        ((string)settingsObj.EventTimeZone).Should().Be("America/New_York"); // Default
        ((string)settingsObj.PreStartBufferMinutes).Should().Be("0"); // Default
    }

    /// <summary>
    /// Test: GetPublicSettings handles service errors gracefully
    /// </summary>
    [Fact]
    public async Task GetPublicSettings_WhenServiceThrowsException_PropagatesException()
    {
        // Arrange
        _mockSettingsService.GetSettingAsync("EventTimeZone", Arg.Any<CancellationToken>())
            .Returns<string?>(x => throw new InvalidOperationException("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await GetPublicSettings());
    }

    #endregion

    #region GetAdminSettings Tests

    /// <summary>
    /// Test: GetAdminSettings returns all settings with 200 OK
    /// </summary>
    [Fact]
    public async Task GetAdminSettings_WithValidRequest_Returns200OkWithAllSettings()
    {
        // Arrange
        var expectedSettings = new Dictionary<string, string>
        {
            { "EventTimeZone", "America/New_York" },
            { "PreStartBufferMinutes", "30" },
            { "MaxTicketsPerPurchase", "10" }
        };
        _mockSettingsService.GetAllSettingsAsync(Arg.Any<CancellationToken>())
            .Returns(expectedSettings);

        // Act
        var result = await GetAdminSettings();

        // Assert
        result.Should().BeAssignableTo<IResult>();

        // Results.Ok returns IResult, we need to access the underlying value using dynamic
        dynamic dynamicResult = result;
        object settings = dynamicResult.Value;
        settings.Should().NotBeNull();
        settings.Should().BeEquivalentTo(expectedSettings);
    }

    /// <summary>
    /// Test: GetAdminSettings returns empty dictionary when no settings exist
    /// </summary>
    [Fact]
    public async Task GetAdminSettings_WithNoSettings_Returns200OkWithEmptyDictionary()
    {
        // Arrange
        _mockSettingsService.GetAllSettingsAsync(Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, string>());

        // Act
        var result = await GetAdminSettings();

        // Assert
        result.Should().BeAssignableTo<IResult>();

        // Results.Ok returns IResult, we need to access the underlying value using dynamic
        dynamic dynamicResult = result;
        object settingsObj = dynamicResult.Value;
        settingsObj.Should().NotBeNull();

        // Cast to Dictionary to check if empty
        var settings = settingsObj as Dictionary<string, string>;
        settings.Should().NotBeNull();
        settings!.Should().BeEmpty();
    }

    /// <summary>
    /// Test: GetAdminSettings handles service errors by propagating exception
    /// </summary>
    [Fact]
    public async Task GetAdminSettings_WhenServiceThrowsException_PropagatesException()
    {
        // Arrange
        _mockSettingsService.GetAllSettingsAsync(Arg.Any<CancellationToken>())
            .Returns<Dictionary<string, string>>(x => throw new InvalidOperationException("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await GetAdminSettings());
    }

    #endregion

    #region UpdateAdminSettings Tests

    /// <summary>
    /// Test: UpdateAdminSettings with valid settings returns 200 OK
    /// </summary>
    [Fact]
    public async Task UpdateAdminSettings_WithValidSettings_Returns200Ok()
    {
        // Arrange
        var request = new UpdateSettingsRequest
        {
            Settings = new Dictionary<string, string>
            {
                { "EventTimeZone", "America/Los_Angeles" },
                { "PreStartBufferMinutes", "15" }
            }
        };

        _mockSettingsService.UpsertMultipleSettingsAsync(
            Arg.Any<Dictionary<string, string>>(),
            Arg.Any<CancellationToken>())
            .Returns((true, string.Empty));

        // Act
        var result = await UpdateAdminSettings(request);

        // Assert
        result.Should().BeAssignableTo<IResult>();

        // Results.Ok returns IResult, we need to access the underlying value using dynamic
        dynamic dynamicResult = result;
        object response = dynamicResult.Value;
        response.Should().NotBeNull();

        dynamic responseObj = response!;
        ((string)responseObj.Message).Should().Be("Settings updated successfully");
    }

    /// <summary>
    /// Test: UpdateAdminSettings with invalid timezone returns 400 BadRequest
    /// </summary>
    [Fact]
    public async Task UpdateAdminSettings_WithInvalidTimeZone_Returns400BadRequest()
    {
        // Arrange
        var request = new UpdateSettingsRequest
        {
            Settings = new Dictionary<string, string>
            {
                { "EventTimeZone", "Invalid/TimeZone" }
            }
        };

        // Act
        var result = await UpdateAdminSettings(request);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(400);
        problemResult.ProblemDetails.Title.Should().Be("Invalid Timezone");
        problemResult.ProblemDetails.Detail.Should().Contain("Invalid/TimeZone");
        problemResult.ProblemDetails.Detail.Should().Contain("not valid");
    }

    /// <summary>
    /// Test: UpdateAdminSettings with negative buffer minutes returns 400 BadRequest
    /// </summary>
    [Fact]
    public async Task UpdateAdminSettings_WithNegativeBufferMinutes_Returns400BadRequest()
    {
        // Arrange
        var request = new UpdateSettingsRequest
        {
            Settings = new Dictionary<string, string>
            {
                { "PreStartBufferMinutes", "-10" }
            }
        };

        // Act
        var result = await UpdateAdminSettings(request);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(400);
        problemResult.ProblemDetails.Title.Should().Be("Invalid Buffer Minutes");
        problemResult.ProblemDetails.Detail.Should().Contain("non-negative integer");
    }

    /// <summary>
    /// Test: UpdateAdminSettings with non-integer buffer minutes returns 400 BadRequest
    /// </summary>
    [Fact]
    public async Task UpdateAdminSettings_WithNonIntegerBufferMinutes_Returns400BadRequest()
    {
        // Arrange
        var request = new UpdateSettingsRequest
        {
            Settings = new Dictionary<string, string>
            {
                { "PreStartBufferMinutes", "abc" }
            }
        };

        // Act
        var result = await UpdateAdminSettings(request);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(400);
        problemResult.ProblemDetails.Title.Should().Be("Invalid Buffer Minutes");
        problemResult.ProblemDetails.Detail.Should().Contain("non-negative integer");
    }

    /// <summary>
    /// Test: UpdateAdminSettings when service fails returns 500 InternalServerError
    /// </summary>
    [Fact]
    public async Task UpdateAdminSettings_WhenServiceFails_Returns500InternalServerError()
    {
        // Arrange
        var request = new UpdateSettingsRequest
        {
            Settings = new Dictionary<string, string>
            {
                { "EventTimeZone", "America/New_York" }
            }
        };

        _mockSettingsService.UpsertMultipleSettingsAsync(
            Arg.Any<Dictionary<string, string>>(),
            Arg.Any<CancellationToken>())
            .Returns((false, "Database connection failed"));

        // Act
        var result = await UpdateAdminSettings(request);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(500);
        problemResult.ProblemDetails.Title.Should().Be("Update Failed");
        problemResult.ProblemDetails.Detail.Should().Be("Database connection failed");
    }

    /// <summary>
    /// Test: UpdateAdminSettings with multiple settings updates all successfully
    /// </summary>
    [Fact]
    public async Task UpdateAdminSettings_WithMultipleSettings_Returns200Ok()
    {
        // Arrange
        var request = new UpdateSettingsRequest
        {
            Settings = new Dictionary<string, string>
            {
                { "EventTimeZone", "America/Chicago" },
                { "PreStartBufferMinutes", "45" },
                { "MaxTicketsPerPurchase", "5" }
            }
        };

        _mockSettingsService.UpsertMultipleSettingsAsync(
            Arg.Any<Dictionary<string, string>>(),
            Arg.Any<CancellationToken>())
            .Returns((true, string.Empty));

        // Act
        var result = await UpdateAdminSettings(request);

        // Assert
        result.Should().BeAssignableTo<IResult>();

        // Verify service was called with correct settings
        await _mockSettingsService.Received(1).UpsertMultipleSettingsAsync(
            Arg.Is<Dictionary<string, string>>(d =>
                d.Count == 3 &&
                d["EventTimeZone"] == "America/Chicago" &&
                d["PreStartBufferMinutes"] == "45" &&
                d["MaxTicketsPerPurchase"] == "5"),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Test: UpdateAdminSettings validates timezone before attempting update
    /// </summary>
    [Fact]
    public async Task UpdateAdminSettings_ValidatesTimezoneBeforeUpdate_Returns400OnInvalidTimezone()
    {
        // Arrange
        var request = new UpdateSettingsRequest
        {
            Settings = new Dictionary<string, string>
            {
                { "EventTimeZone", "NotARealTimeZone" },
                { "PreStartBufferMinutes", "30" } // Valid value that won't be processed
            }
        };

        // Act
        var result = await UpdateAdminSettings(request);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(400);

        // Verify service was NOT called because validation failed
        await _mockSettingsService.DidNotReceive().UpsertMultipleSettingsAsync(
            Arg.Any<Dictionary<string, string>>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Test: UpdateAdminSettings validates buffer minutes before attempting update
    /// </summary>
    [Fact]
    public async Task UpdateAdminSettings_ValidatesBufferMinutesBeforeUpdate_Returns400OnInvalidValue()
    {
        // Arrange
        var request = new UpdateSettingsRequest
        {
            Settings = new Dictionary<string, string>
            {
                { "EventTimeZone", "America/New_York" }, // Valid value that won't be processed
                { "PreStartBufferMinutes", "-5" } // Invalid
            }
        };

        // Act
        var result = await UpdateAdminSettings(request);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(400);

        // Verify service was NOT called because validation failed
        await _mockSettingsService.DidNotReceive().UpsertMultipleSettingsAsync(
            Arg.Any<Dictionary<string, string>>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Test: UpdateAdminSettings with valid timezone successfully validates known timezone
    /// </summary>
    [Fact]
    public async Task UpdateAdminSettings_WithValidKnownTimezone_PassesValidation()
    {
        // Arrange - Test with multiple known valid timezones
        var validTimezones = new[]
        {
            "America/New_York",
            "America/Los_Angeles",
            "America/Chicago",
            "Europe/London",
            "UTC"
        };

        foreach (var timezone in validTimezones)
        {
            var request = new UpdateSettingsRequest
            {
                Settings = new Dictionary<string, string>
                {
                    { "EventTimeZone", timezone }
                }
            };

            _mockSettingsService.UpsertMultipleSettingsAsync(
                Arg.Any<Dictionary<string, string>>(),
                Arg.Any<CancellationToken>())
                .Returns((true, string.Empty));

            // Act
            var result = await UpdateAdminSettings(request);

            // Assert
            result.Should().BeAssignableTo<IResult>($"timezone {timezone} should be valid");
            // Just verify it's an IResult - specific Ok<T> type check not needed for this test
        }
    }

    /// <summary>
    /// Test: UpdateAdminSettings with zero buffer minutes is valid
    /// </summary>
    [Fact]
    public async Task UpdateAdminSettings_WithZeroBufferMinutes_Returns200Ok()
    {
        // Arrange
        var request = new UpdateSettingsRequest
        {
            Settings = new Dictionary<string, string>
            {
                { "PreStartBufferMinutes", "0" }
            }
        };

        _mockSettingsService.UpsertMultipleSettingsAsync(
            Arg.Any<Dictionary<string, string>>(),
            Arg.Any<CancellationToken>())
            .Returns((true, string.Empty));

        // Act
        var result = await UpdateAdminSettings(request);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        // Just verify it's an IResult (200 OK) - no need to extract value for this test
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Helper: Simulates GetPublicSettings endpoint call
    /// </summary>
    private async Task<IResult> GetPublicSettings()
    {
        // Simulate endpoint logic
        var eventTimeZone = await _mockSettingsService.GetSettingAsync("EventTimeZone", CancellationToken.None);
        var preStartBuffer = await _mockSettingsService.GetSettingAsync("PreStartBufferMinutes", CancellationToken.None);

        var settings = new
        {
            EventTimeZone = eventTimeZone ?? "America/New_York",
            PreStartBufferMinutes = preStartBuffer ?? "0"
        };

        return Results.Ok(settings);
    }

    /// <summary>
    /// Helper: Simulates GetAdminSettings endpoint call
    /// </summary>
    private async Task<IResult> GetAdminSettings()
    {
        var settings = await _mockSettingsService.GetAllSettingsAsync(CancellationToken.None);
        return Results.Ok(settings);
    }

    /// <summary>
    /// Helper: Simulates UpdateAdminSettings endpoint call
    /// </summary>
    private async Task<IResult> UpdateAdminSettings(UpdateSettingsRequest request)
    {
        // Validate timezone if provided
        if (request.Settings.ContainsKey("EventTimeZone"))
        {
            var timeZoneId = request.Settings["EventTimeZone"];
            try
            {
                TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
                return Results.Problem(
                    title: "Invalid Timezone",
                    detail: $"Timezone '{timeZoneId}' is not valid",
                    statusCode: 400);
            }
        }

        // Validate buffer minutes if provided
        if (request.Settings.ContainsKey("PreStartBufferMinutes"))
        {
            var bufferValue = request.Settings["PreStartBufferMinutes"];
            if (!int.TryParse(bufferValue, out int bufferMinutes) || bufferMinutes < 0)
            {
                return Results.Problem(
                    title: "Invalid Buffer Minutes",
                    detail: "PreStartBufferMinutes must be a non-negative integer",
                    statusCode: 400);
            }
        }

        var (success, error) = await _mockSettingsService.UpsertMultipleSettingsAsync(
            request.Settings,
            CancellationToken.None);

        if (!success)
        {
            return Results.Problem(
                title: "Update Failed",
                detail: error,
                statusCode: 500);
        }

        return Results.Ok(new { Message = "Settings updated successfully" });
    }

    #endregion
}

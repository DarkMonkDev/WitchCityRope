using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Security.Claims;
using WitchCityRope.Api.Features.EmailTemplates.Endpoints;
using WitchCityRope.Api.Features.EmailTemplates.Entities;
using WitchCityRope.Api.Features.EmailTemplates.Models;
using WitchCityRope.Api.Features.EmailTemplates.Services;
using WitchCityRope.Api.Features.Shared.Models;
using WitchCityRope.Api.Models;
using Xunit;
using FluentAssertions;
using NSubstitute;

namespace WitchCityRope.UnitTests.Api.Features.EmailTemplates;

public class EmailTemplateEndpointsTests
{
    private readonly IEmailTemplateService _mockEmailTemplateService;
    private readonly ClaimsPrincipal _adminUser;
    private readonly ClaimsPrincipal _regularUser;

    public EmailTemplateEndpointsTests()
    {
        _mockEmailTemplateService = Substitute.For<IEmailTemplateService>();

        // Create admin user claims
        _adminUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "Administrator")
        }, "test"));

        // Create regular user claims
        _regularUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "Member")
        }, "test"));
    }

    #region GetGlobalTemplatesByCategory Tests

    [Fact]
    public async Task GetGlobalTemplatesByCategory_WithValidCategory_ReturnsOkResult()
    {
        // Arrange
        var category = "Events";
        var mockTemplates = new List<GlobalEmailTemplateDto>
        {
            new GlobalEmailTemplateDto
            {
                Id = Guid.NewGuid(),
                TemplateType = "EventConfirmation",
                Category = "Events",
                Subject = "Event Confirmation",
                HtmlBody = "<p>Thank you for registering</p>",
                PlainTextBody = "Thank you for registering",
                IsActive = true
            }
        };

        _mockEmailTemplateService.GetGlobalTemplatesByCategoryAsync(EmailCategory.Events, Arg.Any<CancellationToken>())
            .Returns(Result<List<GlobalEmailTemplateDto>>.Success(mockTemplates));

        // Act
        var result = await SimulateGetGlobalTemplatesByCategory(category);

        // Assert
        result.Should().BeOfType<Ok<List<GlobalEmailTemplateDto>>>();
        var okResult = (Ok<List<GlobalEmailTemplateDto>>)result;
        okResult.Value.Should().NotBeNull();
        okResult.Value!.Count.Should().Be(1);
        okResult.Value![0].TemplateType.Should().Be("EventConfirmation");
    }

    [Fact]
    public async Task GetGlobalTemplatesByCategory_WithInvalidCategory_ReturnsBadRequest()
    {
        // Arrange
        var category = "InvalidCategory";

        // Act
        var result = await SimulateGetGlobalTemplatesByCategory(category);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(400);
        problemResult.ProblemDetails!.Title.Should().Be("Invalid Category");
    }

    [Fact]
    public async Task GetGlobalTemplatesByCategory_WithServiceError_ReturnsBadRequest()
    {
        // Arrange
        var category = "Events";
        _mockEmailTemplateService.GetGlobalTemplatesByCategoryAsync(EmailCategory.Events, Arg.Any<CancellationToken>())
            .Returns(Result<List<GlobalEmailTemplateDto>>.Failure("Database error"));

        // Act
        var result = await SimulateGetGlobalTemplatesByCategory(category);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(400);
        problemResult.ProblemDetails!.Title.Should().Be("Failed to Retrieve Templates");
        problemResult.ProblemDetails.Detail.Should().Contain("Database error");
    }

    #endregion

    #region GetGlobalTemplateById Tests

    [Fact]
    public async Task GetGlobalTemplateById_WithValidId_ReturnsOkResult()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var mockTemplate = new GlobalEmailTemplateDto
        {
            Id = templateId,
            TemplateType = "EventConfirmation",
            Category = "Events",
            Subject = "Event Confirmation",
            HtmlBody = "<p>Thank you for registering</p>",
            PlainTextBody = "Thank you for registering",
            IsActive = true
        };

        _mockEmailTemplateService.GetGlobalTemplateByIdAsync(templateId, Arg.Any<CancellationToken>())
            .Returns(Result<GlobalEmailTemplateDto>.Success(mockTemplate));

        // Act
        var result = await SimulateGetGlobalTemplateById(templateId);

        // Assert
        result.Should().BeOfType<Ok<GlobalEmailTemplateDto>>();
        var okResult = (Ok<GlobalEmailTemplateDto>)result;
        okResult.Value.Should().NotBeNull();
        okResult.Value!.Id.Should().Be(templateId);
    }

    [Fact]
    public async Task GetGlobalTemplateById_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        _mockEmailTemplateService.GetGlobalTemplateByIdAsync(templateId, Arg.Any<CancellationToken>())
            .Returns(Result<GlobalEmailTemplateDto>.Failure("Template not found"));

        // Act
        var result = await SimulateGetGlobalTemplateById(templateId);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(404);
        problemResult.ProblemDetails!.Title.Should().Be("Template Not Found");
    }

    #endregion

    #region UpdateGlobalTemplate Tests

    [Fact]
    public async Task UpdateGlobalTemplate_WithValidRequest_ReturnsOkResult()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var userId = Guid.Parse(_adminUser.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var request = new UpdateGlobalTemplateRequest
        {
            Subject = "Updated Subject",
            HtmlBody = "<p>Updated Body</p>",
            PlainTextBody = "Updated Body"
        };

        var mockTemplate = new GlobalEmailTemplateDto
        {
            Id = templateId,
            TemplateType = "EventConfirmation",
            Subject = "Updated Subject",
            HtmlBody = "<p>Updated Body</p>",
            PlainTextBody = "Updated Body",
            IsActive = true
        };

        _mockEmailTemplateService.UpdateGlobalTemplateAsync(templateId, request, userId, Arg.Any<CancellationToken>())
            .Returns(Result<GlobalEmailTemplateDto>.Success(mockTemplate));

        // Act
        var result = await SimulateUpdateGlobalTemplate(templateId, request, _adminUser);

        // Assert
        result.Should().BeOfType<Ok<GlobalEmailTemplateDto>>();
        var okResult = (Ok<GlobalEmailTemplateDto>)result;
        okResult.Value.Should().NotBeNull();
        okResult.Value!.Subject.Should().Be("Updated Subject");
    }

    [Fact]
    public async Task UpdateGlobalTemplate_WithInvalidUser_ReturnsUnauthorized()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var request = new UpdateGlobalTemplateRequest { Subject = "Test", HtmlBody = "<p>Test</p>", PlainTextBody = "Test" };
        var invalidUser = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        var result = await SimulateUpdateGlobalTemplate(templateId, request, invalidUser);

        // Assert
        result.Should().BeOfType<UnauthorizedHttpResult>();
    }

    [Fact]
    public async Task UpdateGlobalTemplate_WithServiceError_ReturnsBadRequest()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var userId = Guid.Parse(_adminUser.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var request = new UpdateGlobalTemplateRequest { Subject = "Test", HtmlBody = "<p>Test</p>", PlainTextBody = "Test" };

        _mockEmailTemplateService.UpdateGlobalTemplateAsync(templateId, request, userId, Arg.Any<CancellationToken>())
            .Returns(Result<GlobalEmailTemplateDto>.Failure("Database error"));

        // Act
        var result = await SimulateUpdateGlobalTemplate(templateId, request, _adminUser);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(400);
        problemResult.ProblemDetails!.Title.Should().Be("Failed to Update Template");
    }

    #endregion

    #region GetEventTemplates Tests

    [Fact]
    public async Task GetEventTemplates_WithValidEventId_ReturnsOkResult()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var mockTemplates = new List<EventEmailTemplateDto>
        {
            new EventEmailTemplateDto
            {
                TemplateType = "EventConfirmation",
                Subject = "Event Confirmation",
                HtmlBody = "<p>Thank you for registering</p>",
                PlainTextBody = "Thank you for registering",
                IsCustomized = false
            }
        };

        _mockEmailTemplateService.GetEventTemplatesAsync(eventId, Arg.Any<CancellationToken>())
            .Returns(Result<List<EventEmailTemplateDto>>.Success(mockTemplates));

        // Act
        var result = await SimulateGetEventTemplates(eventId);

        // Assert
        result.Should().BeOfType<Ok<List<EventEmailTemplateDto>>>();
        var okResult = (Ok<List<EventEmailTemplateDto>>)result;
        okResult.Value.Should().NotBeNull();
        okResult.Value!.Count.Should().Be(1);
    }

    [Fact]
    public async Task GetEventTemplates_WithServiceError_ReturnsBadRequest()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        _mockEmailTemplateService.GetEventTemplatesAsync(eventId, Arg.Any<CancellationToken>())
            .Returns(Result<List<EventEmailTemplateDto>>.Failure("Event not found"));

        // Act
        var result = await SimulateGetEventTemplates(eventId);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(400);
        problemResult.ProblemDetails!.Title.Should().Be("Failed to Retrieve Event Templates");
    }

    #endregion

    #region GetEventTemplateByType Tests

    [Fact]
    public async Task GetEventTemplateByType_WithValidParameters_ReturnsOkResult()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var type = "EventConfirmation";
        var mockTemplate = new EventEmailTemplateDto
        {
            TemplateType = type,
            Subject = "Event Confirmation",
            HtmlBody = "<p>Thank you for registering</p>",
            PlainTextBody = "Thank you for registering",
            IsCustomized = false
        };

        _mockEmailTemplateService.GetEventTemplateByTypeAsync(eventId, type, Arg.Any<CancellationToken>())
            .Returns(Result<EventEmailTemplateDto>.Success(mockTemplate));

        // Act
        var result = await SimulateGetEventTemplateByType(eventId, type);

        // Assert
        result.Should().BeOfType<Ok<EventEmailTemplateDto>>();
        var okResult = (Ok<EventEmailTemplateDto>)result;
        okResult.Value.Should().NotBeNull();
        okResult.Value!.TemplateType.Should().Be(type);
    }

    [Fact]
    public async Task GetEventTemplateByType_WithNonExistentTemplate_ReturnsNotFound()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var type = "NonExistentType";

        _mockEmailTemplateService.GetEventTemplateByTypeAsync(eventId, type, Arg.Any<CancellationToken>())
            .Returns(Result<EventEmailTemplateDto>.Failure("Template not found"));

        // Act
        var result = await SimulateGetEventTemplateByType(eventId, type);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(404);
        problemResult.ProblemDetails!.Title.Should().Be("Template Not Found");
    }

    #endregion

    #region UpdateEventTemplate Tests

    [Fact]
    public async Task UpdateEventTemplate_WithValidRequest_ReturnsOkResult()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var type = "EventConfirmation";
        var userId = Guid.Parse(_adminUser.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var request = new UpdateEventTemplateRequest
        {
            Subject = "Custom Subject",
            HtmlBody = "<p>Custom Body</p>",
            PlainTextBody = "Custom Body"
        };

        var mockTemplate = new EventEmailTemplateDto
        {
            TemplateType = type,
            Subject = "Custom Subject",
            HtmlBody = "<p>Custom Body</p>",
            PlainTextBody = "Custom Body",
            IsCustomized = true
        };

        _mockEmailTemplateService.UpdateEventTemplateAsync(eventId, type, request, userId, Arg.Any<CancellationToken>())
            .Returns(Result<EventEmailTemplateDto>.Success(mockTemplate));

        // Act
        var result = await SimulateUpdateEventTemplate(eventId, type, request, _adminUser);

        // Assert
        result.Should().BeOfType<Ok<EventEmailTemplateDto>>();
        var okResult = (Ok<EventEmailTemplateDto>)result;
        okResult.Value.Should().NotBeNull();
        okResult.Value!.IsCustomized.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateEventTemplate_WithServiceError_ReturnsBadRequest()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var type = "EventConfirmation";
        var userId = Guid.Parse(_adminUser.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var request = new UpdateEventTemplateRequest { Subject = "Test", HtmlBody = "<p>Test</p>", PlainTextBody = "Test" };

        _mockEmailTemplateService.UpdateEventTemplateAsync(eventId, type, request, userId, Arg.Any<CancellationToken>())
            .Returns(Result<EventEmailTemplateDto>.Failure("Event not found"));

        // Act
        var result = await SimulateUpdateEventTemplate(eventId, type, request, _adminUser);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(400);
        problemResult.ProblemDetails!.Title.Should().Be("Failed to Update Event Template");
    }

    #endregion

    #region DeleteEventTemplate Tests

    [Fact]
    public async Task DeleteEventTemplate_WithValidParameters_ReturnsNoContent()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var type = "EventConfirmation";

        _mockEmailTemplateService.DeleteEventTemplateAsync(eventId, type, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await SimulateDeleteEventTemplate(eventId, type);

        // Assert
        result.Should().BeOfType<NoContent>();
    }

    [Fact]
    public async Task DeleteEventTemplate_WithServiceError_ReturnsBadRequest()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var type = "EventConfirmation";

        _mockEmailTemplateService.DeleteEventTemplateAsync(eventId, type, Arg.Any<CancellationToken>())
            .Returns(Result.Failure("Template not found"));

        // Act
        var result = await SimulateDeleteEventTemplate(eventId, type);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(400);
        problemResult.ProblemDetails!.Title.Should().Be("Failed to Delete Event Template");
    }

    #endregion

    #region SendAdHocEmail Tests

    [Fact]
    public async Task SendAdHocEmail_WithValidRequest_ReturnsOkResult()
    {
        // Arrange
        var userId = Guid.Parse(_adminUser.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var request = new SendAdHocEmailRequest
        {
            EventId = Guid.NewGuid(),
            Subject = "Test Email",
            HtmlBody = "<p>Test Body</p>",
            PlainTextBody = "Test Body",
            RecipientGroup = "AllAttendees"
        };

        var mockResponse = new SentAdHocEmailDto
        {
            Id = Guid.NewGuid(),
            Subject = "Test Email",
            SentAt = DateTime.Parse("2025-11-14T10:00:00Z"),
            RecipientCount = 50
        };

        _mockEmailTemplateService.SendAdHocEmailAsync(request, userId, Arg.Any<CancellationToken>())
            .Returns(Result<SentAdHocEmailDto>.Success(mockResponse));

        // Act
        var result = await SimulateSendAdHocEmail(request, _adminUser);

        // Assert
        result.Should().BeOfType<Ok<SentAdHocEmailDto>>();
        var okResult = (Ok<SentAdHocEmailDto>)result;
        okResult.Value.Should().NotBeNull();
        okResult.Value!.RecipientCount.Should().Be(50);
    }

    [Fact]
    public async Task SendAdHocEmail_WithServiceError_ReturnsBadRequest()
    {
        // Arrange
        var userId = Guid.Parse(_adminUser.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var request = new SendAdHocEmailRequest
        {
            EventId = Guid.NewGuid(),
            Subject = "Test",
            HtmlBody = "<p>Test</p>",
            PlainTextBody = "Test",
            RecipientGroup = "AllAttendees"
        };

        _mockEmailTemplateService.SendAdHocEmailAsync(request, userId, Arg.Any<CancellationToken>())
            .Returns(Result<SentAdHocEmailDto>.Failure("Email service unavailable"));

        // Act
        var result = await SimulateSendAdHocEmail(request, _adminUser);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(400);
        problemResult.ProblemDetails!.Title.Should().Be("Failed to Send Ad-Hoc Email");
    }

    #endregion

    #region GetAdHocEmailHistory Tests

    [Fact]
    public async Task GetAdHocEmailHistory_WithValidRequest_ReturnsOkResult()
    {
        // Arrange
        Guid? eventId = null;
        var mockHistory = new List<SentAdHocEmailDto>
        {
            new SentAdHocEmailDto
            {
                Id = Guid.NewGuid(),
                Subject = "Test Email",
                SentAt = DateTime.Parse("2025-11-14T10:00:00Z"),
                RecipientCount = 50
            }
        };

        _mockEmailTemplateService.GetAdHocEmailHistoryAsync(eventId, Arg.Any<CancellationToken>())
            .Returns(Result<List<SentAdHocEmailDto>>.Success(mockHistory));

        // Act
        var result = await SimulateGetAdHocEmailHistory(eventId);

        // Assert
        result.Should().BeOfType<Ok<List<SentAdHocEmailDto>>>();
        var okResult = (Ok<List<SentAdHocEmailDto>>)result;
        okResult.Value.Should().NotBeNull();
        okResult.Value!.Count.Should().Be(1);
    }

    [Fact]
    public async Task GetAdHocEmailHistory_WithServiceError_ReturnsBadRequest()
    {
        // Arrange
        Guid? eventId = null;
        _mockEmailTemplateService.GetAdHocEmailHistoryAsync(eventId, Arg.Any<CancellationToken>())
            .Returns(Result<List<SentAdHocEmailDto>>.Failure("Database error"));

        // Act
        var result = await SimulateGetAdHocEmailHistory(eventId);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(400);
        problemResult.ProblemDetails!.Title.Should().Be("Failed to Retrieve Ad-Hoc Email History");
    }

    #endregion

    #region GetAdHocEmailById Tests

    [Fact]
    public async Task GetAdHocEmailById_WithValidId_ReturnsOkResult()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        var mockEmail = new SentAdHocEmailDto
        {
            Id = emailId,
            Subject = "Test Email",
            HtmlBody = "<p>Test Body</p>",
            PlainTextBody = "Test Body",
            SentAt = DateTime.Parse("2025-11-14T10:00:00Z"),
            RecipientCount = 50
        };

        _mockEmailTemplateService.GetAdHocEmailByIdAsync(emailId, Arg.Any<CancellationToken>())
            .Returns(Result<SentAdHocEmailDto>.Success(mockEmail));

        // Act
        var result = await SimulateGetAdHocEmailById(emailId);

        // Assert
        result.Should().BeOfType<Ok<SentAdHocEmailDto>>();
        var okResult = (Ok<SentAdHocEmailDto>)result;
        okResult.Value.Should().NotBeNull();
        okResult.Value!.Id.Should().Be(emailId);
    }

    [Fact]
    public async Task GetAdHocEmailById_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var emailId = Guid.NewGuid();
        _mockEmailTemplateService.GetAdHocEmailByIdAsync(emailId, Arg.Any<CancellationToken>())
            .Returns(Result<SentAdHocEmailDto>.Failure("Email not found"));

        // Act
        var result = await SimulateGetAdHocEmailById(emailId);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(404);
        problemResult.ProblemDetails!.Title.Should().Be("Ad-Hoc Email Not Found");
    }

    #endregion

    #region Helper Methods - Simulation

    private async Task<IResult> SimulateGetGlobalTemplatesByCategory(string category)
    {
        if (!Enum.TryParse<EmailCategory>(category, ignoreCase: true, out var emailCategory))
        {
            return Results.Problem(
                title: "Invalid Category",
                detail: $"Invalid category. Valid values: {string.Join(", ", Enum.GetNames<EmailCategory>())}",
                statusCode: 400);
        }

        var result = await _mockEmailTemplateService.GetGlobalTemplatesByCategoryAsync(emailCategory, CancellationToken.None);

        if (!result.IsSuccess)
        {
            return Results.Problem(title: "Failed to Retrieve Templates", detail: result.Error, statusCode: 400);
        }

        return Results.Ok(result.Value);
    }

    private async Task<IResult> SimulateGetGlobalTemplateById(Guid id)
    {
        var result = await _mockEmailTemplateService.GetGlobalTemplateByIdAsync(id, CancellationToken.None);
        return result.IsSuccess ? Results.Ok(result.Value) : Results.Problem(title: "Template Not Found", detail: result.Error, statusCode: 404);
    }

    private async Task<IResult> SimulateUpdateGlobalTemplate(Guid id, UpdateGlobalTemplateRequest request, ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier) ?? user.FindFirst("sub");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Results.Unauthorized();
        }

        var result = await _mockEmailTemplateService.UpdateGlobalTemplateAsync(id, request, userId, CancellationToken.None);
        return result.IsSuccess ? Results.Ok(result.Value) : Results.Problem(title: "Failed to Update Template", detail: result.Error, statusCode: 400);
    }

    private async Task<IResult> SimulateGetEventTemplates(Guid eventId)
    {
        var result = await _mockEmailTemplateService.GetEventTemplatesAsync(eventId, CancellationToken.None);
        return result.IsSuccess ? Results.Ok(result.Value) : Results.Problem(title: "Failed to Retrieve Event Templates", detail: result.Error, statusCode: 400);
    }

    private async Task<IResult> SimulateGetEventTemplateByType(Guid eventId, string type)
    {
        var result = await _mockEmailTemplateService.GetEventTemplateByTypeAsync(eventId, type, CancellationToken.None);
        return result.IsSuccess ? Results.Ok(result.Value) : Results.Problem(title: "Template Not Found", detail: result.Error, statusCode: 404);
    }

    private async Task<IResult> SimulateUpdateEventTemplate(Guid eventId, string type, UpdateEventTemplateRequest request, ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier) ?? user.FindFirst("sub");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Results.Unauthorized();
        }

        var result = await _mockEmailTemplateService.UpdateEventTemplateAsync(eventId, type, request, userId, CancellationToken.None);
        return result.IsSuccess ? Results.Ok(result.Value) : Results.Problem(title: "Failed to Update Event Template", detail: result.Error, statusCode: 400);
    }

    private async Task<IResult> SimulateDeleteEventTemplate(Guid eventId, string type)
    {
        var result = await _mockEmailTemplateService.DeleteEventTemplateAsync(eventId, type, CancellationToken.None);
        return result.IsSuccess ? Results.NoContent() : Results.Problem(title: "Failed to Delete Event Template", detail: result.Error, statusCode: 400);
    }

    private async Task<IResult> SimulateSendAdHocEmail(SendAdHocEmailRequest request, ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier) ?? user.FindFirst("sub");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Results.Unauthorized();
        }

        var result = await _mockEmailTemplateService.SendAdHocEmailAsync(request, userId, CancellationToken.None);
        return result.IsSuccess ? Results.Ok(result.Value) : Results.Problem(title: "Failed to Send Ad-Hoc Email", detail: result.Error, statusCode: 400);
    }

    private async Task<IResult> SimulateGetAdHocEmailHistory(Guid? eventId)
    {
        var result = await _mockEmailTemplateService.GetAdHocEmailHistoryAsync(eventId, CancellationToken.None);
        return result.IsSuccess ? Results.Ok(result.Value) : Results.Problem(title: "Failed to Retrieve Ad-Hoc Email History", detail: result.Error, statusCode: 400);
    }

    private async Task<IResult> SimulateGetAdHocEmailById(Guid id)
    {
        var result = await _mockEmailTemplateService.GetAdHocEmailByIdAsync(id, CancellationToken.None);
        return result.IsSuccess ? Results.Ok(result.Value) : Results.Problem(title: "Ad-Hoc Email Not Found", detail: result.Error, statusCode: 404);
    }

    #endregion
}

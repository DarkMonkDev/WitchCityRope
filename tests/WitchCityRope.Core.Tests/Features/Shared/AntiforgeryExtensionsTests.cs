using FluentAssertions;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Moq;
using WitchCityRope.Api.Features.Shared.Extensions;
using WitchCityRope.Api.Features.Shared.Models;
using Xunit;

namespace WitchCityRope.Core.Tests.Features.Shared;

/// <summary>
/// Unit tests for <see cref="AntiforgeryExtensions.ValidateAsync"/> — the shared CSRF helper
/// that replaced 30+ copies of try/catch across endpoint files. Bug here = wide blast radius
/// (every state-changing endpoint), so the helper has to be unit-tested even though it is
/// only ~10 lines of logic.
/// </summary>
public class AntiforgeryExtensionsTests
{
    [Fact]
    public async Task ValidateAsync_WhenValid_ReturnsSuccess()
    {
        // Arrange
        var antiforgery = new Mock<IAntiforgery>();
        var context = new DefaultHttpContext();
        antiforgery
            .Setup(a => a.ValidateRequestAsync(context))
            .Returns(Task.CompletedTask); // Success path returns without throwing

        // Act
        var result = await antiforgery.Object.ValidateAsync(context);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeEmpty();
        result.ErrorKind.Should().Be(ResultErrorKind.None);
    }

    [Fact]
    public async Task ValidateAsync_WhenAntiforgeryThrows_ReturnsFailureWithStandardMessage()
    {
        // Arrange
        var antiforgery = new Mock<IAntiforgery>();
        var context = new DefaultHttpContext();
        antiforgery
            .Setup(a => a.ValidateRequestAsync(context))
            .ThrowsAsync(new AntiforgeryValidationException("token mismatch"));

        // Act
        var result = await antiforgery.Object.ValidateAsync(context);

        // Assert
        result.IsSuccess.Should().BeFalse();
        // ErrorKind defaults to BusinessRule → HTTP 400 via ToProblem, preserving the legacy
        // status that every prior call site hardcoded.
        result.ErrorKind.Should().Be(ResultErrorKind.BusinessRule);
        // Wire message must be the static user-facing string — never the exception's message
        // (per the Error Handling Standard).
        result.Error.Should().Be(
            "Antiforgery token validation failed. Please refresh the page and try again.");
        // Belt-and-suspenders: prove no exception text leaked through.
        result.Error.Should().NotContain("token mismatch");
    }
}

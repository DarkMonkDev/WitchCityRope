using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WitchCityRope.Api.Features.Auth;
using WitchCityRope.Api.Features.Auth.Models;
using WitchCityRope.Api.Features.Auth.Services;
using WitchCityRope.Api.Interfaces;
using Xunit;

namespace WitchCityRope.Api.Tests.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<IAuthService> _authServiceMock;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _authServiceMock = new Mock<IAuthService>();
            _controller = new AuthController(_authServiceMock.Object);
        }

        [Fact]
        public async Task Register_ValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new RegisterRequest 
            { 
                Email = "test@example.com",
                Password = "Test123!",
                ConfirmPassword = "Test123!",
                Username = "testuser",
                LegalName = "Test User",
                SceneName = "TestScene"
            };
            var response = new RegisterResponse 
            { 
                Token = "test-token",
                UserId = Guid.NewGuid(),
                Email = request.Email
            };
            _authServiceMock.Setup(x => x.RegisterAsync(It.IsAny<RegisterRequest>()))
                .ReturnsAsync(response);

            // Act
            var result = await _controller.Register(request);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);
            okResult.Value.Should().BeEquivalentTo(response);
        }

        [Fact]
        public async Task Register_DuplicateEmail_ReturnsBadRequest()
        {
            // Arrange
            var request = new RegisterRequest 
            { 
                Email = "duplicate@example.com",
                Password = "Test123!",
                ConfirmPassword = "Test123!",
                Username = "testuser",
                LegalName = "Test User",
                SceneName = "TestScene"
            };
            _authServiceMock.Setup(x => x.RegisterAsync(It.IsAny<RegisterRequest>()))
                .ThrowsAsync(new WitchCityRope.Api.Exceptions.ConflictException("Email already exists"));

            // Act
            var result = await _controller.Register(request);

            // Assert
            var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.StatusCode.Should().Be(400);
            badRequestResult.Value.Should().BeEquivalentTo(new { message = "Email already exists" });
        }

        [Fact]
        public async Task Register_ValidationError_ReturnsBadRequest()
        {
            // Arrange
            var request = new RegisterRequest 
            { 
                Email = "invalid-email",
                Password = "weak",
                ConfirmPassword = "weak",
                Username = "u",
                LegalName = "",
                SceneName = ""
            };
            _authServiceMock.Setup(x => x.RegisterAsync(It.IsAny<RegisterRequest>()))
                .ThrowsAsync(new WitchCityRope.Api.Exceptions.ValidationException("Invalid input"));

            // Act
            var result = await _controller.Register(request);

            // Assert
            var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.StatusCode.Should().Be(400);
            badRequestResult.Value.Should().BeEquivalentTo(new { message = "Invalid input" });
        }

        [Fact]
        public async Task Login_ValidCredentials_ReturnsOk()
        {
            // Arrange
            var request = new LoginRequest 
            { 
                Email = "test@example.com",
                Password = "Test123!"
            };
            var response = new LoginResponse 
            { 
                Token = "test-token",
                RefreshToken = "refresh-token",
                UserId = Guid.NewGuid()
            };
            _authServiceMock.Setup(x => x.LoginAsync(It.IsAny<LoginRequest>()))
                .ReturnsAsync(response);

            // Act
            var result = await _controller.Login(request);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);
            okResult.Value.Should().BeEquivalentTo(response);
        }

        [Fact]
        public async Task Login_InvalidCredentials_ReturnsUnauthorized()
        {
            // Arrange
            var request = new LoginRequest 
            { 
                Email = "test@example.com",
                Password = "WrongPassword"
            };
            _authServiceMock.Setup(x => x.LoginAsync(It.IsAny<LoginRequest>()))
                .ThrowsAsync(new WitchCityRope.Api.Features.Auth.Services.UnauthorizedException("Invalid credentials"));

            // Act
            var result = await _controller.Login(request);

            // Assert
            var unauthorizedResult = result.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            unauthorizedResult.StatusCode.Should().Be(401);
            unauthorizedResult.Value.Should().BeEquivalentTo(new { message = "Invalid credentials" });
        }

        [Fact]
        public async Task Logout_ReturnsOk()
        {
            // Act
            var result = await _controller.Logout();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);
            okResult.Value.Should().BeEquivalentTo(new { message = "Logged out successfully" });
        }

        [Fact]
        public async Task RefreshToken_ValidToken_ReturnsOk()
        {
            // Arrange
            var request = new RefreshTokenRequest { RefreshToken = "valid-refresh-token" };
            var response = new LoginResponse 
            { 
                Token = "new-token",
                RefreshToken = "new-refresh-token",
                UserId = Guid.NewGuid()
            };
            _authServiceMock.Setup(x => x.RefreshTokenAsync(It.IsAny<string>()))
                .ReturnsAsync(response);

            // Act
            var result = await _controller.RefreshToken(request);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);
            okResult.Value.Should().BeEquivalentTo(response);
        }

        [Fact]
        public async Task RefreshToken_InvalidToken_ReturnsUnauthorized()
        {
            // Arrange
            var request = new RefreshTokenRequest { RefreshToken = "invalid-token" };
            _authServiceMock.Setup(x => x.RefreshTokenAsync(It.IsAny<string>()))
                .ThrowsAsync(new WitchCityRope.Api.Features.Auth.Services.UnauthorizedException("Invalid refresh token"));

            // Act
            var result = await _controller.RefreshToken(request);

            // Assert
            var unauthorizedResult = result.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            unauthorizedResult.StatusCode.Should().Be(401);
            unauthorizedResult.Value.Should().BeEquivalentTo(new { message = "Invalid refresh token" });
        }

        [Fact]
        public async Task VerifyEmail_ValidToken_ReturnsOk()
        {
            // Arrange
            var request = new VerifyEmailRequest { Token = "valid-token" };
            _authServiceMock.Setup(x => x.VerifyEmailAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.VerifyEmail(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);
            okResult.Value.Should().BeEquivalentTo(new { message = "Email verified successfully. You can now log in." });
        }

        [Fact]
        public async Task VerifyEmail_InvalidToken_ReturnsBadRequest()
        {
            // Arrange
            var request = new VerifyEmailRequest { Token = "invalid-token" };
            _authServiceMock.Setup(x => x.VerifyEmailAsync(It.IsAny<string>()))
                .ThrowsAsync(new WitchCityRope.Api.Exceptions.ValidationException("Invalid or expired token"));

            // Act
            var result = await _controller.VerifyEmail(request);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.StatusCode.Should().Be(400);
            badRequestResult.Value.Should().BeEquivalentTo(new { message = "Invalid or expired token" });
        }
    }
}
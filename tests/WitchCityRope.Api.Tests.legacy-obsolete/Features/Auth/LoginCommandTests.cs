using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using WitchCityRope.Api.Features.Auth;
using WitchCityRope.Api.Features.Auth.Services;
using WitchCityRope.Infrastructure.Data;
using WitchCityRope.Core.Entities;
using WitchCityRope.Api.Services;
using Xunit;

namespace WitchCityRope.Api.Tests.Features.Auth
{
    public class LoginCommandTests : IDisposable
    {
        private readonly WitchCityRopeDbContext _context;
        private readonly WitchCityRope.Api.Features.Auth.Services.IPasswordHasher _passwordHasher;
        private readonly IJwtService _jwtService;

        public LoginCommandTests()
        {
            var options = new DbContextOptionsBuilder<WitchCityRopeDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;
            _context = new WitchCityRopeDbContext(options);
            _passwordHasher = new Mock<WitchCityRope.Api.Features.Auth.Services.IPasswordHasher>().Object;
            _jwtService = new Mock<IJwtService>().Object;
        }

        // TODO: These tests reference LoginCommand which doesn't exist in the current codebase
        // The current Auth implementation uses AuthService directly through the controller
        // These tests need to be rewritten to test AuthService instead of command handlers

        /*
        [Fact]
        public async Task Handle_ValidCredentials_ReturnsToken()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hashedpassword"
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var command = new LoginCommand.Command
            {
                Username = "testuser",
                Password = "password123"
            };
            var handler = new LoginCommand.Handler(_context, _passwordHasher, _jwtService);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Token.Should().NotBeNullOrEmpty();
            result.Username.Should().Be("testuser");
        }

        [Fact]
        public async Task Handle_InvalidUsername_ThrowsException()
        {
            // Arrange
            var command = new LoginCommand.Command
            {
                Username = "nonexistent",
                Password = "password123"
            };
            var handler = new LoginCommand.Handler(_context, _passwordHasher, _jwtService);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_InvalidPassword_ThrowsException()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hashedpassword"
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var command = new LoginCommand.Command
            {
                Username = "testuser",
                Password = "wrongpassword"
            };
            var handler = new LoginCommand.Handler(_context, _passwordHasher, _jwtService);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                handler.Handle(command, CancellationToken.None));
        }
        */

        public void Dispose()
        {
            _context?.Dispose();
        }
    }

    // Mock implementations for testing
    public class MockPasswordService : WitchCityRope.Api.Features.Auth.Services.IPasswordHasher
    {
        public string HashPassword(string password)
        {
            return "hashedpassword";
        }

        public bool VerifyPassword(string password, string hash)
        {
            return password == "password123" && hash == "hashedpassword";
        }
    }

    public class MockTokenService : WitchCityRope.Api.Features.Auth.Services.IJwtService
    {
        public JwtToken GenerateToken(UserWithAuth user)
        {
            return new JwtToken
            {
                Token = "mock-jwt-token",
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };
        }

        public string GenerateRefreshToken()
        {
            return "mock-refresh-token";
        }
    }
}
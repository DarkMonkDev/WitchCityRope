using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Features.Auth;
using WitchCityRope.Core.Data;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Services;
using Xunit;

namespace WitchCityRope.Api.Tests.Features.Auth
{
    public class LoginCommandTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordService _passwordService;
        private readonly ITokenService _tokenService;

        public LoginCommandTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _passwordService = new MockPasswordService();
            _tokenService = new MockTokenService();
        }

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
            var handler = new LoginCommand.Handler(_context, _passwordService, _tokenService);

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
            var handler = new LoginCommand.Handler(_context, _passwordService, _tokenService);

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
            var handler = new LoginCommand.Handler(_context, new MockPasswordService(false), _tokenService);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                handler.Handle(command, CancellationToken.None));
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }

    // Mock implementations for testing
    public class MockPasswordService : IPasswordService
    {
        private readonly bool _shouldVerify;

        public MockPasswordService(bool shouldVerify = true)
        {
            _shouldVerify = shouldVerify;
        }

        public string HashPassword(string password) => "hashedpassword";
        public bool VerifyPassword(string password, string hash) => _shouldVerify;
    }

    public class MockTokenService : ITokenService
    {
        public string GenerateToken(User user) => "mock-jwt-token";
    }
}
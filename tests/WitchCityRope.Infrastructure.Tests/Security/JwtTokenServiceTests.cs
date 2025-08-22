using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using WitchCityRope.Core.Enums;
using WitchCityRope.Infrastructure.Security;
using WitchCityRope.Tests.Common.Builders;
using Xunit;

namespace WitchCityRope.Infrastructure.Tests.Security
{
    public class JwtTokenServiceTests
    {
        private readonly JwtTokenService _jwtTokenService;
        private readonly IConfiguration _configuration;
        private readonly string _secretKey = "ThisIsAVerySecureSecretKeyForTesting12345678901234567890";

        public JwtTokenServiceTests()
        {
            var configValues = new Dictionary<string, string?>
            {
                {"Jwt:SecretKey", _secretKey},
                {"Jwt:Issuer", "TestIssuer"},
                {"Jwt:Audience", "TestAudience"},
                {"Jwt:ExpirationMinutes", "60"}
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configValues)
                .Build();

            _jwtTokenService = new JwtTokenService(_configuration);
        }

        [Fact]
        public void Constructor_Should_Throw_When_Configuration_Is_Null()
        {
            // Act & Assert
            var act = () => new JwtTokenService(null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_Should_Throw_When_SecretKey_Is_Missing()
        {
            // Arrange
            var configWithoutKey = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    {"Jwt:Issuer", "TestIssuer"},
                    {"Jwt:Audience", "TestAudience"}
                })
                .Build();

            // Act & Assert
            var act = () => new JwtTokenService(configWithoutKey);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("JWT secret key not configured");
        }

        [Fact]
        public void GenerateToken_Should_Create_Valid_JWT_Token()
        {
            // Arrange
            var user = new UserBuilder()
                .WithEmail("test@example.com")
                .WithSceneName("TestUser")
                .WithRole(UserRole.Member)
                .Build();

            // Act
            var token = _jwtTokenService.GenerateToken(user);

            // Assert
            token.Should().NotBeNullOrEmpty();
            token.Should().MatchRegex(@"^[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+$"); // JWT format
        }

        [Fact]
        public void GenerateToken_Should_Throw_For_Null_User()
        {
            // Act & Assert
            var act = () => _jwtTokenService.GenerateToken(null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void GenerateToken_Should_Include_All_Required_Claims()
        {
            // Arrange
            var user = new UserBuilder()
                .WithEmail("test@example.com")
                .WithSceneName("TestUser")
                .WithRole(UserRole.Administrator)
                .Build();

            // Act
            var token = _jwtTokenService.GenerateToken(user);
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(token);

            // Assert
            jsonToken.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id.ToString());
            jsonToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == user.Email.Value);
            jsonToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == user.SceneName.Value);
            jsonToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == UserRole.Administrator.ToString());
            jsonToken.Claims.Should().Contain(c => c.Type == "UserId" && c.Value == user.Id.ToString());
            jsonToken.Claims.Should().Contain(c => c.Type == "SceneName" && c.Value == user.SceneName.Value);
        }

        [Fact]
        public void GenerateToken_Should_Set_Correct_Expiration()
        {
            // Arrange
            var user = new UserBuilder().Build();
            var expectedExpiration = DateTime.UtcNow.AddMinutes(60);

            // Act
            var token = _jwtTokenService.GenerateToken(user);
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(token);

            // Assert
            jsonToken.ValidTo.Should().BeCloseTo(expectedExpiration, TimeSpan.FromMinutes(1));
        }

        [Fact]
        public void GenerateToken_Should_Set_Correct_Issuer_And_Audience()
        {
            // Arrange
            var user = new UserBuilder().Build();

            // Act
            var token = _jwtTokenService.GenerateToken(user);
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(token);

            // Assert
            jsonToken.Issuer.Should().Be("TestIssuer");
            jsonToken.Audiences.Should().Contain("TestAudience");
        }

        [Fact]
        public void ValidateToken_Should_Return_ClaimsPrincipal_For_Valid_Token()
        {
            // Arrange
            var user = new UserBuilder().Build();
            var token = _jwtTokenService.GenerateToken(user);

            // Act
            var principal = _jwtTokenService.ValidateToken(token);

            // Assert
            principal.Should().NotBeNull();
            principal!.Claims.Should().NotBeEmpty();
            principal.Identity!.IsAuthenticated.Should().BeTrue();
        }

        [Fact]
        public void ValidateToken_Should_Throw_For_Null_Token()
        {
            // Act & Assert
            var act = () => _jwtTokenService.ValidateToken(null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void ValidateToken_Should_Return_Null_For_Invalid_Token()
        {
            // Arrange
            const string invalidToken = "invalid.token.here";

            // Act
            var principal = _jwtTokenService.ValidateToken(invalidToken);

            // Assert
            principal.Should().BeNull();
        }

        [Fact]
        public void ValidateToken_Should_Return_Null_For_Expired_Token()
        {
            // Arrange
            var configWithShortExpiration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    {"Jwt:SecretKey", _secretKey},
                    {"Jwt:Issuer", "TestIssuer"},
                    {"Jwt:Audience", "TestAudience"},
                    {"Jwt:ExpirationMinutes", "-1"} // Already expired
                })
                .Build();

            var service = new JwtTokenService(configWithShortExpiration);
            var user = new UserBuilder().Build();
            var token = service.GenerateToken(user);

            // Act
            var principal = _jwtTokenService.ValidateToken(token);

            // Assert
            principal.Should().BeNull();
        }

        [Fact]
        public void ValidateToken_Should_Return_Null_For_Wrong_Issuer()
        {
            // Arrange
            var wrongIssuerConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    {"Jwt:SecretKey", _secretKey},
                    {"Jwt:Issuer", "WrongIssuer"},
                    {"Jwt:Audience", "TestAudience"},
                    {"Jwt:ExpirationMinutes", "60"}
                })
                .Build();

            var wrongService = new JwtTokenService(wrongIssuerConfig);
            var user = new UserBuilder().Build();
            var token = wrongService.GenerateToken(user);

            // Act
            var principal = _jwtTokenService.ValidateToken(token);

            // Assert
            principal.Should().BeNull();
        }

        [Fact]
        public void ValidateToken_Should_Return_Null_For_Wrong_Audience()
        {
            // Arrange
            var wrongAudienceConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    {"Jwt:SecretKey", _secretKey},
                    {"Jwt:Issuer", "TestIssuer"},
                    {"Jwt:Audience", "WrongAudience"},
                    {"Jwt:ExpirationMinutes", "60"}
                })
                .Build();

            var wrongService = new JwtTokenService(wrongAudienceConfig);
            var user = new UserBuilder().Build();
            var token = wrongService.GenerateToken(user);

            // Act
            var principal = _jwtTokenService.ValidateToken(token);

            // Assert
            principal.Should().BeNull();
        }

        [Fact]
        public void ValidateToken_Should_Return_Null_For_Wrong_Secret()
        {
            // Arrange
            var wrongSecretConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    {"Jwt:SecretKey", "WrongSecretKey12345678901234567890123456789012345678"},
                    {"Jwt:Issuer", "TestIssuer"},
                    {"Jwt:Audience", "TestAudience"},
                    {"Jwt:ExpirationMinutes", "60"}
                })
                .Build();

            var wrongService = new JwtTokenService(wrongSecretConfig);
            var user = new UserBuilder().Build();
            var token = wrongService.GenerateToken(user);

            // Act
            var principal = _jwtTokenService.ValidateToken(token);

            // Assert
            principal.Should().BeNull();
        }

        [Fact]
        public void GetUserIdFromToken_Should_Return_UserId_For_Valid_Token()
        {
            // Arrange
            var user = new UserBuilder().Build();
            var token = _jwtTokenService.GenerateToken(user);

            // Act
            var extractedUserId = _jwtTokenService.GetUserIdFromToken(token);

            // Assert
            extractedUserId.Should().Be(user.Id);
        }

        [Fact]
        public void GetUserIdFromToken_Should_Return_Null_For_Invalid_Token()
        {
            // Arrange
            const string invalidToken = "invalid.token.here";

            // Act
            var userId = _jwtTokenService.GetUserIdFromToken(invalidToken);

            // Assert
            userId.Should().BeNull();
        }

        [Fact]
        public void GenerateRefreshToken_Should_Create_Valid_Base64_String()
        {
            // Act
            var refreshToken = _jwtTokenService.GenerateRefreshToken();

            // Assert
            refreshToken.Should().NotBeNullOrEmpty();
            refreshToken.Should().MatchRegex("^[A-Za-z0-9+/=]+$"); // Base64 pattern
            
            // Should be decodable
            var bytes = Convert.FromBase64String(refreshToken);
            bytes.Length.Should().Be(32); // 32 bytes as specified in the implementation
        }

        [Fact]
        public void GenerateRefreshToken_Should_Create_Unique_Tokens()
        {
            // Act
            var token1 = _jwtTokenService.GenerateRefreshToken();
            var token2 = _jwtTokenService.GenerateRefreshToken();
            var token3 = _jwtTokenService.GenerateRefreshToken();

            // Assert
            token1.Should().NotBe(token2);
            token2.Should().NotBe(token3);
            token1.Should().NotBe(token3);
        }

        [Fact]
        public void Token_Should_Work_With_Default_Configuration()
        {
            // Arrange
            var minimalConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    {"Jwt:SecretKey", _secretKey}
                })
                .Build();

            var service = new JwtTokenService(minimalConfig);
            var user = new UserBuilder().Build();

            // Act
            var token = service.GenerateToken(user);
            var principal = service.ValidateToken(token);

            // Assert
            principal.Should().NotBeNull();
            
            // Check defaults
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(token);
            jsonToken.Issuer.Should().Be("WitchCityRope");
            jsonToken.Audiences.Should().Contain("WitchCityRope");
            jsonToken.ValidTo.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(60), TimeSpan.FromMinutes(1));
        }
    }
}
using Moq;
using WitchCityRope.Api.Features.Auth.Models;
using WitchCityRope.Api.Features.Auth.Services;

namespace WitchCityRope.Api.Tests.Mocks
{
    /// <summary>
    /// Factory for creating IJwtService mocks for API tests
    /// </summary>
    public static class JwtServiceMock
    {
        /// <summary>
        /// Creates a mock IJwtService with default behavior
        /// </summary>
        public static Mock<IJwtService> Create()
        {
            var mock = new Mock<IJwtService>();
            
            // Setup default token generation
            mock.Setup(x => x.GenerateToken(It.IsAny<UserWithAuth>()))
                .Returns((UserWithAuth user) => new JwtToken
                {
                    Token = $"test-token-for-{user.Id}",
                    ExpiresAt = DateTime.UtcNow.AddHours(1),
                    RefreshToken = $"test-refresh-token-for-{user.Id}"
                });
            
            // Setup refresh token generation
            mock.Setup(x => x.GenerateRefreshToken())
                .Returns(() => $"test-refresh-token-{Guid.NewGuid()}");
            
            return mock;
        }
        
        /// <summary>
        /// Creates a mock IJwtService that returns a specific token
        /// </summary>
        public static Mock<IJwtService> CreateWithToken(string token, string refreshToken)
        {
            var mock = new Mock<IJwtService>();
            
            mock.Setup(x => x.GenerateToken(It.IsAny<UserWithAuth>()))
                .Returns(new JwtToken
                {
                    Token = token,
                    ExpiresAt = DateTime.UtcNow.AddHours(1),
                    RefreshToken = refreshToken
                });
            
            mock.Setup(x => x.GenerateRefreshToken())
                .Returns(refreshToken);
            
            return mock;
        }
    }
}
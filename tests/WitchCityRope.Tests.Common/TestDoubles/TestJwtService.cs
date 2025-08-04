using System.Collections.Concurrent;
using System.Security.Claims;
using WitchCityRope.Core.Entities;
using WitchCityRope.Tests.Common.Interfaces;

namespace WitchCityRope.Tests.Common.TestDoubles
{
    /// <summary>
    /// Test implementation of IJwtService for unit testing
    /// </summary>
    public class TestJwtService : IJwtService
    {
        private readonly ConcurrentDictionary<string, TokenInfo> _tokens = new();
        private readonly ConcurrentDictionary<string, RefreshTokenInfo> _refreshTokens = new();
        
        public bool SimulateInvalidToken { get; set; }
        public bool SimulateExpiredToken { get; set; }
        public TimeSpan TokenLifetime { get; set; } = TimeSpan.FromHours(1);
        public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(7);

        public Task<JwtToken> GenerateTokenAsync(IUser user)
        {
            var token = $"test-jwt-{Guid.NewGuid()}";
            var refreshToken = $"test-refresh-{Guid.NewGuid()}";
            var expiresAt = DateTime.UtcNow.Add(TokenLifetime);

            _tokens[token] = new TokenInfo
            {
                UserId = user.Id,
                Email = user.EmailAddress.Value,
                SceneName = user.SceneName.Value,
                ExpiresAt = expiresAt,
                IsValid = true
            };

            _refreshTokens[refreshToken] = new RefreshTokenInfo
            {
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.Add(RefreshTokenLifetime),
                IsValid = true
            };

            return Task.FromResult(new JwtToken
            {
                Token = token,
                ExpiresAt = expiresAt,
                RefreshToken = refreshToken
            });
        }

        public Task<bool> ValidateTokenAsync(string token)
        {
            if (SimulateInvalidToken)
                return Task.FromResult(false);

            if (!_tokens.TryGetValue(token, out var tokenInfo))
                return Task.FromResult(false);

            if (SimulateExpiredToken || tokenInfo.ExpiresAt < DateTime.UtcNow)
                return Task.FromResult(false);

            return Task.FromResult(tokenInfo.IsValid);
        }

        public Task<Guid?> GetUserIdFromTokenAsync(string token)
        {
            if (_tokens.TryGetValue(token, out var tokenInfo) && tokenInfo.IsValid)
                return Task.FromResult<Guid?>(tokenInfo.UserId);

            return Task.FromResult<Guid?>(null);
        }

        public Task<string> GenerateRefreshTokenAsync()
        {
            return Task.FromResult($"test-refresh-{Guid.NewGuid()}");
        }

        public void InvalidateToken(string token)
        {
            if (_tokens.TryGetValue(token, out var tokenInfo))
                tokenInfo.IsValid = false;
        }

        public void InvalidateRefreshToken(string refreshToken)
        {
            if (_refreshTokens.TryGetValue(refreshToken, out var tokenInfo))
                tokenInfo.IsValid = false;
        }

        public bool IsRefreshTokenValid(string refreshToken)
        {
            return _refreshTokens.TryGetValue(refreshToken, out var tokenInfo) &&
                   tokenInfo.IsValid &&
                   tokenInfo.ExpiresAt > DateTime.UtcNow;
        }

        public Guid? GetUserIdFromRefreshToken(string refreshToken)
        {
            if (_refreshTokens.TryGetValue(refreshToken, out var tokenInfo) && tokenInfo.IsValid)
                return tokenInfo.UserId;
            
            return null;
        }

        public void Clear()
        {
            _tokens.Clear();
            _refreshTokens.Clear();
        }

        private class TokenInfo
        {
            public Guid UserId { get; set; }
            public string Email { get; set; } = string.Empty;
            public string SceneName { get; set; } = string.Empty;
            public DateTime ExpiresAt { get; set; }
            public bool IsValid { get; set; }
        }

        private class RefreshTokenInfo
        {
            public Guid UserId { get; set; }
            public DateTime ExpiresAt { get; set; }
            public bool IsValid { get; set; }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WitchCityRope.Api.Features.Auth.Models;
using WitchCityRope.Core.Entities;
using WitchCityRope.Infrastructure.Identity;

namespace WitchCityRope.Api.Features.Auth.Services
{
    /// <summary>
    /// Refresh token information
    /// </summary>
    public class RefreshTokenInfo
    {
        public Guid UserId { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsValid { get; set; }
    }

    /// <summary>
    /// User repository interface for Auth feature
    /// </summary>
    public interface IUserRepository
    {
        Task<UserWithAuth?> GetByEmailAsync(string email);
        Task<WitchCityRopeUser?> GetByIdAsync(Guid userId);
        Task<bool> IsSceneNameTakenAsync(string sceneName);
        Task CreateAsync(WitchCityRopeUser user, UserAuthentication userAuth);
        Task UpdateLastLoginAsync(Guid userId);
        Task StoreRefreshTokenAsync(Guid userId, string refreshToken, DateTime expiresAt);
        Task<RefreshTokenInfo?> GetRefreshTokenInfoAsync(string refreshToken);
        Task InvalidateRefreshTokenAsync(string refreshToken);
        Task<UserAuthentication?> GetByVerificationTokenAsync(string token);
        Task VerifyEmailAsync(Guid userId);
    }
}
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.ValueObjects;
using WitchCityRope.Infrastructure.Data;

namespace WitchCityRope.Api.Features.Auth.Services
{
    /// <summary>
    /// Repository implementation for authentication-related user operations
    /// </summary>
    public class AuthUserRepository : IUserRepository
    {
        private readonly WitchCityRopeDbContext _context;

        public AuthUserRepository(WitchCityRopeDbContext context)
        {
            _context = context;
        }

        public async Task<UserWithAuth?> GetByEmailAsync(string email)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.Value == email);

            if (user == null)
                return null;

            // Get the authentication record for this user
            var userAuth = await _context.UserAuthentications
                .FirstOrDefaultAsync(ua => ua.UserId == user.Id);

            if (userAuth == null)
                return null;

            return new UserWithAuth
            {
                User = user,
                PasswordHash = userAuth.PasswordHash,
                EmailVerified = true, // TODO: Add EmailVerifiedAt to UserAuthentication entity
                LastLoginAt = userAuth.UpdatedAt // Using UpdatedAt as a proxy for last login
            };
        }

        public async Task<User?> GetByIdAsync(Guid userId)
        {
            return await _context.Users.FindAsync(userId);
        }

        public async Task<bool> IsSceneNameTakenAsync(string sceneName)
        {
            return await _context.Users
                .AnyAsync(u => u.SceneName.Value == sceneName);
        }

        public async Task CreateAsync(User user, UserAuthentication userAuth)
        {
            // Add the user to the context
            _context.Users.Add(user);
            
            // In a real implementation, we would also save userAuth to a separate table
            // For now, we'll just save the user
            await _context.SaveChangesAsync();
        }

        public async Task UpdateLastLoginAsync(Guid userId)
        {
            // In a real implementation, this would update the last login timestamp
            // in the authentication table
            await Task.CompletedTask;
        }

        public async Task StoreRefreshTokenAsync(Guid userId, string refreshToken, DateTime expiresAt)
        {
            // In a real implementation, this would store the refresh token
            // in a tokens table
            await Task.CompletedTask;
        }

        public async Task<RefreshTokenInfo?> GetRefreshTokenInfoAsync(string refreshToken)
        {
            // In a real implementation, this would look up the refresh token
            // from a tokens table
            return await Task.FromResult<RefreshTokenInfo?>(null);
        }

        public async Task InvalidateRefreshTokenAsync(string refreshToken)
        {
            // In a real implementation, this would mark the refresh token as invalid
            await Task.CompletedTask;
        }

        public async Task<UserAuthentication?> GetByVerificationTokenAsync(string token)
        {
            // In a real implementation, this would look up the user by verification token
            // from the authentication table
            return await Task.FromResult<UserAuthentication?>(null);
        }

        public async Task VerifyEmailAsync(Guid userId)
        {
            // In a real implementation, this would mark the user's email as verified
            // in the authentication table
            await Task.CompletedTask;
        }
    }
}
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Features.Auth.Models;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.ValueObjects;
using WitchCityRope.Infrastructure.Data;
using WitchCityRope.Infrastructure.Identity;

namespace WitchCityRope.Api.Features.Auth.Services
{
    /// <summary>
    /// Repository implementation for authentication-related user operations using Identity
    /// </summary>
    public class AuthUserRepository : IUserRepository
    {
        private readonly WitchCityRopeIdentityDbContext _context;
        private readonly UserManager<WitchCityRopeUser> _userManager;

        public AuthUserRepository(WitchCityRopeIdentityDbContext context, UserManager<WitchCityRopeUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<UserWithAuth?> GetByEmailAsync(string email)
        {
            var identityUser = await _userManager.FindByEmailAsync(email);

            if (identityUser == null)
                return null;

            // Map WitchCityRopeUser to UserWithAuth for compatibility
            return new UserWithAuth
            {
                User = identityUser,
                PasswordHash = identityUser.PasswordHash ?? string.Empty,
                EmailVerified = identityUser.EmailConfirmed,
                PronouncedName = identityUser.PronouncedName,
                Pronouns = identityUser.Pronouns,
                LastLoginAt = identityUser.LastLoginAt
            };
        }

        public async Task<WitchCityRopeUser?> GetByIdAsync(Guid userId)
        {
            return await _userManager.FindByIdAsync(userId.ToString());
        }

        public async Task<bool> IsSceneNameTakenAsync(string sceneName)
        {
            return await _userManager.Users
                .AnyAsync(u => u.SceneNameValue == sceneName);
        }

        public async Task CreateAsync(WitchCityRopeUser identityUser, UserAuthentication userAuth)
        {
            // User is already a WitchCityRopeUser, no conversion needed

            // Create the user without password first
            var result = await _userManager.CreateAsync(identityUser);
            
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            // Set the password hash directly if provided
            if (!string.IsNullOrEmpty(userAuth.PasswordHash))
            {
                identityUser.PasswordHash = userAuth.PasswordHash;
                await _userManager.UpdateAsync(identityUser);
            }
        }

        public async Task UpdateLastLoginAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user != null)
            {
                user.LastLoginAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
            }
        }

        public async Task StoreRefreshTokenAsync(Guid userId, string refreshToken, DateTime expiresAt)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user != null)
            {
                var token = new RefreshToken
                {
                    UserId = userId,
                    Token = refreshToken,
                    ExpiresAt = expiresAt,
                    CreatedAt = DateTime.UtcNow,
                    IsRevoked = false
                };
                
                _context.RefreshTokens.Add(token);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<RefreshTokenInfo?> GetRefreshTokenInfoAsync(string refreshToken)
        {
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken && !rt.IsRevoked);

            if (token == null)
                return null;

            return new RefreshTokenInfo
            {
                UserId = token.UserId,
                ExpiresAt = token.ExpiresAt,
                IsValid = token.ExpiresAt > DateTime.UtcNow
            };
        }

        public async Task InvalidateRefreshTokenAsync(string refreshToken)
        {
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (token != null)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<UserAuthentication?> GetByVerificationTokenAsync(string token)
        {
            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.EmailVerificationToken == token);

            if (user == null)
                return null;

            // Return a UserAuthentication object for compatibility
            // Note: This is a compatibility shim and should be refactored
            return new UserAuthentication
            {
                Id = user.Id,
                UserId = user.Id,
                PasswordHash = user.PasswordHash ?? string.Empty,
                IsTwoFactorEnabled = user.TwoFactorEnabled,
                FailedLoginAttempts = user.FailedLoginAttempts,
                LockedOutUntil = user.LockedOutUntil,
                LastPasswordChangeAt = user.LastPasswordChangeAt,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
        }

        public async Task VerifyEmailAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user != null)
            {
                user.EmailConfirmed = true;
                user.EmailVerificationToken = string.Empty;
                user.EmailVerificationTokenCreatedAt = null;
                await _userManager.UpdateAsync(user);
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Infrastructure.Identity;
using WitchCityRope.Core.ValueObjects;
using WitchCityRope.Infrastructure.Data;

namespace WitchCityRope.Infrastructure.Identity
{
    /// <summary>
    /// Custom user store that extends the default Entity Framework user store
    /// to support scene name lookup and other custom functionality
    /// </summary>
    public class WitchCityRopeUserStore : UserStore<WitchCityRopeUser, WitchCityRopeRole, WitchCityRopeIdentityDbContext, Guid>
    {
        private readonly WitchCityRopeIdentityDbContext _context;

        public WitchCityRopeUserStore(
            WitchCityRopeIdentityDbContext context,
            IdentityErrorDescriber describer = null) 
            : base(context, describer)
        {
            _context = context;
        }

        /// <summary>
        /// Finds a user by scene name
        /// </summary>
        public async Task<WitchCityRopeUser> FindBySceneNameAsync(string sceneName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(sceneName))
                return null;

            // Scene names are case-insensitive
            var normalizedSceneName = sceneName.ToUpperInvariant();

            return await _context.Users
                .FirstOrDefaultAsync(u => u.SceneName.Value.ToUpper() == normalizedSceneName, cancellationToken);
        }

        /// <summary>
        /// Finds a user by email or scene name (for login support)
        /// </summary>
        public async Task<WitchCityRopeUser> FindByEmailOrSceneNameAsync(string emailOrSceneName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(emailOrSceneName))
                return null;

            // First try to find by email
            var userByEmail = await FindByEmailAsync(emailOrSceneName.ToUpperInvariant(), cancellationToken);
            if (userByEmail != null)
                return userByEmail;

            // If not found by email, try scene name
            return await FindBySceneNameAsync(emailOrSceneName, cancellationToken);
        }

        /// <summary>
        /// Gets all active users
        /// </summary>
        public async Task<List<WitchCityRopeUser>> GetActiveUsersAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return await _context.Users
                .Where(u => u.IsActive)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Gets all vetted users
        /// </summary>
        public async Task<List<WitchCityRopeUser>> GetVettedUsersAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return await _context.Users
                .Where(u => u.IsVetted && u.IsActive)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Checks if a scene name is already taken
        /// </summary>
        public async Task<bool> IsSceneNameTakenAsync(string sceneName, Guid? excludeUserId = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(sceneName))
                return false;

            var normalizedSceneName = sceneName.ToUpperInvariant();

            var query = _context.Users
                .Where(u => u.SceneNameValue.ToUpper() == normalizedSceneName);

            if (excludeUserId.HasValue)
            {
                query = query.Where(u => u.Id != excludeUserId.Value);
            }

            return await query.AnyAsync(cancellationToken);
        }

        /// <summary>
        /// Updates the last login timestamp
        /// </summary>
        public async Task UpdateLastLoginAsync(WitchCityRopeUser user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (user == null)
                throw new ArgumentNullException(nameof(user));

            user.LastLoginAt = DateTime.UtcNow;
            user.FailedLoginAttempts = 0; // Reset failed attempts on successful login

            _context.Update(user);
            await _context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Increments failed login attempts and handles lockout
        /// </summary>
        public async Task<bool> IncrementFailedLoginAttemptsAsync(WitchCityRopeUser user, int maxAttempts = 5, int lockoutMinutes = 30, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (user == null)
                throw new ArgumentNullException(nameof(user));

            user.FailedLoginAttempts++;

            if (user.FailedLoginAttempts >= maxAttempts)
            {
                user.LockedOutUntil = DateTime.UtcNow.AddMinutes(lockoutMinutes);
                user.LockoutEnd = user.LockedOutUntil; // Update Identity's lockout end
                user.LockoutEnabled = true;
            }

            _context.Update(user);
            await _context.SaveChangesAsync(cancellationToken);

            return user.FailedLoginAttempts >= maxAttempts;
        }

        /// <summary>
        /// Checks if the user is currently locked out
        /// </summary>
        public bool IsLockedOut(WitchCityRopeUser user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            return user.LockedOutUntil.HasValue && user.LockedOutUntil.Value > DateTime.UtcNow;
        }

        /// <summary>
        /// Override CreateAsync to ensure custom properties are properly set
        /// </summary>
        public override async Task<IdentityResult> CreateAsync(WitchCityRopeUser user, CancellationToken cancellationToken = default)
        {
            // Ensure the normalized fields are set
            if (string.IsNullOrEmpty(user.NormalizedEmail))
                user.NormalizedEmail = user.Email?.ToUpperInvariant();

            if (string.IsNullOrEmpty(user.NormalizedUserName))
                user.NormalizedUserName = user.UserName?.ToUpperInvariant();

            // Check if scene name is already taken
            if (await IsSceneNameTakenAsync(user.SceneName.Value, null, cancellationToken))
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "DuplicateSceneName",
                    Description = "This scene name is already taken."
                });
            }

            return await base.CreateAsync(user, cancellationToken);
        }

        /// <summary>
        /// Override UpdateAsync to ensure custom properties are properly validated
        /// </summary>
        public override async Task<IdentityResult> UpdateAsync(WitchCityRopeUser user, CancellationToken cancellationToken = default)
        {
            // Check if scene name is already taken (excluding current user)
            if (await IsSceneNameTakenAsync(user.SceneName.Value, user.Id, cancellationToken))
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "DuplicateSceneName",
                    Description = "This scene name is already taken."
                });
            }

            user.UpdatedAt = DateTime.UtcNow;

            return await base.UpdateAsync(user, cancellationToken);
        }
    }
}
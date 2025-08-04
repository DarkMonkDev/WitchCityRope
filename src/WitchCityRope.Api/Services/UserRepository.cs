using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
// Removed Core.Entities - using WitchCityRopeUser directly
using WitchCityRope.Core.ValueObjects;
using WitchCityRope.Infrastructure.Data;
using WitchCityRope.Infrastructure.Identity;

namespace WitchCityRope.Api.Services
{
    /// <summary>
    /// Temporary implementation of user repository for basic navigation
    /// TODO: Implement proper repository pattern with full functionality
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly WitchCityRopeIdentityDbContext _context;
        private readonly UserManager<WitchCityRopeUser> _userManager;

        public UserRepository(WitchCityRopeIdentityDbContext context, UserManager<WitchCityRopeUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<WitchCityRopeUser?> GetByIdAsync(Guid id)
        {
            return await _userManager.FindByIdAsync(id.ToString());
        }

        public async Task<WitchCityRopeUser?> GetByEmailAsync(string email)
        {
            return await _userManager.FindByEmailAsync(email);
        }

        public async Task<WitchCityRopeUser> CreateAsync(WitchCityRopeUser user)
        {
            var result = await _userManager.CreateAsync(user);
            
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
            
            return user;
        }

        public async Task UpdateAsync(WitchCityRopeUser user)
        {
            var result = await _userManager.UpdateAsync(user);
            
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Failed to update user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            return user != null;
        }

        public async Task<bool> SceneNameExistsAsync(string sceneName)
        {
            return await _context.Users.AnyAsync(u => u.SceneNameValue == sceneName);
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            return user != null;
        }

        public async Task AddAsync(WitchCityRopeUser user)
        {
            await CreateAsync(user);
        }

        public async Task DeleteAsync(WitchCityRopeUser user)
        {
            var result = await _userManager.DeleteAsync(user);
            
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Failed to delete user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
    }
}
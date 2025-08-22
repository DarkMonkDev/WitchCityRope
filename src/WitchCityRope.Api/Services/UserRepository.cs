using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Core.Entities;
using WitchCityRope.Infrastructure.Data;

namespace WitchCityRope.Api.Services
{
    /// <summary>
    /// Temporary implementation of user repository for basic navigation
    /// TODO: Implement proper repository pattern with full functionality
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly WitchCityRopeDbContext _context;

        public UserRepository(WitchCityRopeDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email.Value == email);
        }

        public async Task<User> CreateAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email.Value == email);
        }

        public async Task<bool> SceneNameExistsAsync(string sceneName)
        {
            return await _context.Users.AnyAsync(u => u.SceneName.Value == sceneName);
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.Users.AnyAsync(u => u.Id == id);
        }

        public async Task AddAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(User user)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }
    }
}
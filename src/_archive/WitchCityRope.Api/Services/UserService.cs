using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Models;
using WitchCityRope.Infrastructure.Data;
using WitchCityRope.Core.Exceptions;
using WitchCityRope.Core.ValueObjects;

namespace WitchCityRope.Api.Services;

public class UserService : IUserService
{
    private readonly WitchCityRopeDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(WitchCityRopeDbContext dbContext, IPasswordHasher passwordHasher)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
    }

    public async Task<UserProfileDto?> GetUserProfileAsync(Guid userId)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return null;

        return new UserProfileDto
        {
            Id = user.Id,
            Email = user.Email.Value,
            SceneName = user.SceneName.Value,
            Bio = "", // TODO: Add user profile extension
            Pronouns = "", // TODO: Add user profile extension
            PublicProfile = true, // TODO: Add user profile extension
            EmailVerified = false, // TODO: Add user authentication extension
            TwoFactorEnabled = false, // TODO: Add user authentication extension
            CreatedAt = user.CreatedAt,
            Roles = new List<string> { user.Role.ToString() }
        };
    }

    public async Task<UserProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null)
            throw new NotFoundException($"User with ID {userId} not found");

        // Update only provided fields
        if (request.SceneName != null)
            user.UpdateSceneName(Core.ValueObjects.SceneName.Create(request.SceneName));
        
        // TODO: Add support for Bio, Pronouns, PublicProfile
        // These properties need to be added to a UserProfile entity

        await _dbContext.SaveChangesAsync();

        return await GetUserProfileAsync(userId) ?? throw new InvalidOperationException("Failed to retrieve updated profile");
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null)
            return false;

        // TODO: Add password management to user authentication
        // This requires a separate UserAuthentication entity
        throw new NotImplementedException("Password management not yet implemented");

        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> EnableTwoFactorAsync(Guid userId, string code)
    {
        // TODO: Implement two-factor authentication enable
        await Task.CompletedTask;
        return false;
    }

    public async Task<bool> DisableTwoFactorAsync(Guid userId, string password)
    {
        // TODO: Implement two-factor authentication disable
        await Task.CompletedTask;
        return false;
    }
}
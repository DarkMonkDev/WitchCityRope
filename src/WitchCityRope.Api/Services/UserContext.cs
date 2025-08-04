using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Core.DTOs;
using WitchCityRope.Core.Entities;
using WitchCityRope.Infrastructure.Data;
using WitchCityRope.Infrastructure.Identity;
using WitchCityRope.Infrastructure.Services;

namespace WitchCityRope.Api.Services;

/// <summary>
/// Implementation of IUserContext that uses HttpContext to get current user information
/// </summary>
public class UserContext : IUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<WitchCityRopeUser> _userManager;
    private readonly WitchCityRopeIdentityDbContext _dbContext;

    public UserContext(
        IHttpContextAccessor httpContextAccessor,
        UserManager<WitchCityRopeUser> userManager,
        WitchCityRopeIdentityDbContext dbContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
        _dbContext = dbContext;
    }

    public Guid? GetCurrentUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return null;

        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
            return null;

        if (Guid.TryParse(userIdClaim, out var userId))
            return userId;

        return null;
    }

    public async Task<WitchCityRopeUser?> GetCurrentUserAsync()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return null;

        return await _dbContext.Users
            .Include(u => u.VettingApplications)
            .FirstOrDefaultAsync(u => u.Id == userId.Value);
    }
}
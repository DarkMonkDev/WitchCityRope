using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Users.Models;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Users.Services;

/// <summary>
/// User management service using direct Entity Framework access
/// Example of the simplified vertical slice architecture pattern - NO MediatR complexity
/// </summary>
public class UserManagementService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<UserManagementService> _logger;

    public UserManagementService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<UserManagementService> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Get user profile for current user - Simple Entity Framework service - NO MediatR complexity
    /// </summary>
    public async Task<(bool Success, UserDto? Response, string Error)> GetProfileAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Direct Entity Framework query for user lookup
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id.ToString() == userId, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("User not found for profile request: {UserId}", userId);
                return (false, null, "User not found");
            }

            var response = new UserDto(user);

            _logger.LogDebug("User profile retrieved successfully: {UserId} ({SceneName})", userId, user.SceneName);
            return (true, response, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user profile: {UserId}", userId);
            return (false, null, "Failed to retrieve user profile");
        }
    }

    /// <summary>
    /// Update user profile for current user - Direct Entity Framework access
    /// </summary>
    public async Task<(bool Success, UserDto? Response, string Error)> UpdateProfileAsync(
        string userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Find user using UserManager
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found for profile update: {UserId}", userId);
                return (false, null, "User not found");
            }

            // Check if scene name is already taken by another user
            if (!string.IsNullOrEmpty(request.SceneName) && request.SceneName != user.SceneName)
            {
                var existingSceneName = await _context.Users
                    .AsNoTracking()
                    .AnyAsync(u => u.SceneName == request.SceneName && u.Id != user.Id, cancellationToken);

                if (existingSceneName)
                {
                    return (false, null, "Scene name is already taken");
                }
            }

            // Update user properties
            if (!string.IsNullOrEmpty(request.SceneName))
            {
                user.SceneName = request.SceneName;
            }

            if (!string.IsNullOrEmpty(request.Pronouns))
            {
                user.Pronouns = request.Pronouns;
            }

            // Save changes using Entity Framework
            await _context.SaveChangesAsync(cancellationToken);

            var response = new UserDto(user);

            _logger.LogInformation("User profile updated successfully: {UserId} ({SceneName})", userId, user.SceneName);
            return (true, response, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user profile: {UserId}", userId);
            return (false, null, "Failed to update user profile");
        }
    }

    /// <summary>
    /// Get paginated list of users for admin - Direct Entity Framework access with filtering
    /// </summary>
    public async Task<(bool Success, UserListResponse? Response, string Error)> GetUsersAsync(
        UserSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Querying users from PostgreSQL database with filters");

            // Start with base query using direct Entity Framework
            var query = _context.Users.AsNoTracking();

            // Apply search term filter - search across email, scene name, discord name, and roles
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.ToLower();
                query = query.Where(u =>
                    (u.Email != null && u.Email.ToLower().Contains(searchTerm)) ||
                    (u.SceneName != null && u.SceneName.ToLower().Contains(searchTerm)) ||
                    (u.DiscordName != null && u.DiscordName.ToLower().Contains(searchTerm)) ||
                    (u.Role != null && u.Role.ToLower().Contains(searchTerm)));
            }

            // Apply role filter with OR logic for multiple roles
            if (request.RoleFilters != null && request.RoleFilters.Length > 0)
            {
                // User matches if their Role field contains ANY of the requested roles
                // This supports users with multiple comma-separated roles like "Teacher,SafetyTeam"
                query = query.Where(u =>
                    request.RoleFilters.Any(role =>
                        u.Role != null && u.Role.Contains(role)));
            }
            else if (!string.IsNullOrWhiteSpace(request.Role))
            {
                // Legacy single role filter (kept for backward compatibility)
                query = query.Where(u => u.Role == request.Role);
            }

            // Apply active status filter
            if (request.IsActive.HasValue)
            {
                query = query.Where(u => u.IsActive == request.IsActive.Value);
            }

            // Get total count for pagination
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply sorting
            query = request.SortBy.ToLower() switch
            {
                "scenename" => request.SortDescending
                    ? query.OrderByDescending(u => u.SceneName)
                    : query.OrderBy(u => u.SceneName),
                "email" => request.SortDescending
                    ? query.OrderByDescending(u => u.Email)
                    : query.OrderBy(u => u.Email),
                "role" => request.SortDescending
                    ? query.OrderByDescending(u => u.Role)
                    : query.OrderBy(u => u.Role),
                "discordname" => request.SortDescending
                    ? query.OrderByDescending(u => u.DiscordName)
                    : query.OrderBy(u => u.DiscordName),
                "vettingstatus" => request.SortDescending
                    ? query.OrderByDescending(u => u.VettingStatus)
                    : query.OrderBy(u => u.VettingStatus),
                "isactive" => request.SortDescending
                    ? query.OrderByDescending(u => u.IsActive)
                    : query.OrderBy(u => u.IsActive),
                "createdat" => request.SortDescending
                    ? query.OrderByDescending(u => u.CreatedAt)
                    : query.OrderBy(u => u.CreatedAt),
                "lastloginat" => request.SortDescending
                    ? query.OrderByDescending(u => u.LastLoginAt)
                    : query.OrderBy(u => u.LastLoginAt),
                _ => request.SortDescending
                    ? query.OrderByDescending(u => u.SceneName)
                    : query.OrderBy(u => u.SceneName)
            };

            // Apply pagination and project to DTO
            var users = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(u => new UserDto(u))
                .ToListAsync(cancellationToken);

            var response = new UserListResponse
            {
                Users = users,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };

            _logger.LogInformation("Retrieved {UserCount} users from database (page {Page} of {TotalPages})",
                users.Count, request.Page, response.TotalPages);

            return (true, response, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve users list");
            return (false, null, "Failed to retrieve users");
        }
    }

    /// <summary>
    /// Get single user by ID for admin - Direct Entity Framework access
    /// </summary>
    public async Task<(bool Success, UserDto? Response, string Error)> GetUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Guid.TryParse(userId, out var parsedId))
            {
                _logger.LogWarning("Invalid user ID format: {UserId}", userId);
                return (false, null, "Invalid user ID format");
            }

            // Direct Entity Framework query for single user
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == parsedId, cancellationToken);

            if (user == null)
            {
                _logger.LogInformation("User not found: {UserId}", userId);
                return (false, null, "User not found");
            }

            var userDto = new UserDto(user);

            _logger.LogDebug("User retrieved successfully: {UserId} ({SceneName})", userId, user.SceneName);
            return (true, userDto, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve user: {UserId}", userId);
            return (false, null, "Failed to retrieve user");
        }
    }

    /// <summary>
    /// Update user for admin - Direct Entity Framework access with role and status changes
    /// </summary>
    public async Task<(bool Success, UserDto? Response, string Error)> UpdateUserAsync(
        string userId,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Guid.TryParse(userId, out var parsedId))
            {
                _logger.LogWarning("Invalid user ID format for update: {UserId}", userId);
                return (false, null, "Invalid user ID format");
            }

            // Find user using UserManager for better Identity integration
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found for update: {UserId}", userId);
                return (false, null, "User not found");
            }

            // Check if scene name is already taken by another user
            if (!string.IsNullOrEmpty(request.SceneName) && request.SceneName != user.SceneName)
            {
                var existingSceneName = await _context.Users
                    .AsNoTracking()
                    .AnyAsync(u => u.SceneName == request.SceneName && u.Id != user.Id, cancellationToken);

                if (existingSceneName)
                {
                    return (false, null, "Scene name is already taken");
                }
            }

            // Update user properties
            if (!string.IsNullOrEmpty(request.SceneName))
            {
                user.SceneName = request.SceneName;
            }

            if (!string.IsNullOrEmpty(request.Role))
            {
                user.Role = request.Role;
            }

            if (!string.IsNullOrEmpty(request.Pronouns))
            {
                user.Pronouns = request.Pronouns;
            }

            if (request.IsActive.HasValue)
            {
                user.IsActive = request.IsActive.Value;
            }

            if (request.EmailConfirmed.HasValue)
            {
                user.EmailConfirmed = request.EmailConfirmed.Value;
            }

            if (request.VettingStatus.HasValue)
            {
                user.VettingStatus = request.VettingStatus.Value;
            }

            // Save changes using Entity Framework
            await _context.SaveChangesAsync(cancellationToken);

            var response = new UserDto(user);

            _logger.LogInformation("User updated successfully by admin: {UserId} ({SceneName})", userId, user.SceneName);
            return (true, response, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user: {UserId}", userId);
            return (false, null, "Failed to update user");
        }
    }

    /// <summary>
    /// Get users by role for dropdown options - Direct Entity Framework access
    /// </summary>
    public async Task<(bool Success, IEnumerable<UserOptionDto>? Response, string Error)> GetUsersByRoleAsync(
        string role,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting users by role: {Role}", role);

            // Query users with the specified role using Entity Framework
            var users = await _context.Users
                .AsNoTracking()
                .Where(u => u.Role == role && u.IsActive)
                .OrderBy(u => u.SceneName ?? u.Email)
                .Select(u => new UserOptionDto
                {
                    Id = u.Id.ToString(),
                    Name = u.SceneName ?? u.Email ?? "Unknown",
                    Email = u.Email ?? ""
                })
                .ToListAsync(cancellationToken);

            _logger.LogDebug("Found {Count} users with role: {Role}", users.Count, role);
            return (true, users, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get users by role: {Role}", role);
            return (false, null, "Failed to get users by role");
        }
    }
}
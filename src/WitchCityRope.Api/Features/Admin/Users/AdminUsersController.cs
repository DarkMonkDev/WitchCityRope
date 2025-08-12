using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WitchCityRope.Api.Extensions;
using WitchCityRope.Core.DTOs;
using WitchCityRope.Core.Enums;
using WitchCityRope.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Infrastructure.Data;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Repositories;

namespace WitchCityRope.Api.Features.Admin.Users
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Policy = "RequireAdmin")]
    public class AdminUsersController : ControllerBase
    {
        private readonly UserManager<WitchCityRopeUser> _userManager;
        private readonly RoleManager<WitchCityRopeRole> _roleManager;
        private readonly WitchCityRopeIdentityDbContext _context;
        private readonly IUserNoteRepository _userNoteRepository;
        private readonly ILogger<AdminUsersController> _logger;

        public AdminUsersController(
            UserManager<WitchCityRopeUser> userManager,
            RoleManager<WitchCityRopeRole> roleManager,
            WitchCityRopeIdentityDbContext context,
            IUserNoteRepository userNoteRepository,
            ILogger<AdminUsersController> logger)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userNoteRepository = userNoteRepository ?? throw new ArgumentNullException(nameof(userNoteRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets a paginated list of users with filtering and sorting
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PagedUserResult), 200)]
        public async Task<IActionResult> GetUsers([FromQuery] UserSearchRequest request)
        {
            try
            {
                var query = _userManager.Users.AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    var searchTerm = request.SearchTerm.ToLower();
                    query = query.Where(u => 
                        u.SceneNameValue.ToLower().Contains(searchTerm) ||
                        u.Email!.ToLower().Contains(searchTerm) ||
                        u.UserName!.ToLower().Contains(searchTerm));
                }

                if (request.Role.HasValue)
                {
                    query = query.Where(u => u.Role == request.Role.Value);
                }

                if (request.IsActive.HasValue)
                {
                    query = query.Where(u => u.IsActive == request.IsActive.Value);
                }

                if (request.IsVetted.HasValue)
                {
                    query = query.Where(u => u.IsVetted == request.IsVetted.Value);
                }

                if (request.EmailConfirmed.HasValue)
                {
                    query = query.Where(u => u.EmailConfirmed == request.EmailConfirmed.Value);
                }

                if (request.IsLockedOut.HasValue)
                {
                    if (request.IsLockedOut.Value)
                    {
                        query = query.Where(u => u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow);
                    }
                    else
                    {
                        query = query.Where(u => !u.LockoutEnd.HasValue || u.LockoutEnd <= DateTimeOffset.UtcNow);
                    }
                }

                // Apply sorting
                query = request.SortBy.ToLower() switch
                {
                    "email" => request.SortDirection == "desc" 
                        ? query.OrderByDescending(u => u.Email)
                        : query.OrderBy(u => u.Email),
                    "role" => request.SortDirection == "desc"
                        ? query.OrderByDescending(u => u.Role)
                        : query.OrderBy(u => u.Role),
                    "createdat" => request.SortDirection == "desc"
                        ? query.OrderByDescending(u => u.CreatedAt)
                        : query.OrderBy(u => u.CreatedAt),
                    "lastloginat" => request.SortDirection == "desc"
                        ? query.OrderByDescending(u => u.LastLoginAt)
                        : query.OrderBy(u => u.LastLoginAt),
                    _ => request.SortDirection == "desc"
                        ? query.OrderByDescending(u => u.SceneNameValue)
                        : query.OrderBy(u => u.SceneNameValue)
                };

                var totalCount = await query.CountAsync();
                var users = await query
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                var userDtos = users.Select(user => new AdminUserDto
                {
                    Id = user.Id,
                    SceneName = user.SceneNameValue,
                    Email = user.Email ?? string.Empty,
                    Role = user.Role,
                    IsActive = user.IsActive,
                    IsVetted = user.IsVetted,
                    EmailConfirmed = user.EmailConfirmed,
                    LockoutEnabled = user.LockoutEnabled,
                    LockoutEnd = user.LockoutEnd?.DateTime,
                    AccessFailedCount = user.AccessFailedCount,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt,
                    Pronouns = user.Pronouns,
                    PronouncedName = user.PronouncedName
                }).ToList();

                return Ok(new PagedUserResult
                {
                    Users = userDtos,
                    TotalCount = totalCount,
                    CurrentPage = request.Page,
                    PageSize = request.PageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users");
                return StatusCode(500, new { message = "An error occurred while retrieving users" });
            }
        }

        /// <summary>
        /// Gets detailed information about a specific user
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(AdminUserDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetUser(Guid id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id.ToString());
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                var userDto = new AdminUserDto
                {
                    Id = user.Id,
                    SceneName = user.SceneNameValue,
                    Email = user.Email ?? string.Empty,
                    Role = user.Role,
                    IsActive = user.IsActive,
                    IsVetted = user.IsVetted,
                    EmailConfirmed = user.EmailConfirmed,
                    LockoutEnabled = user.LockoutEnabled,
                    LockoutEnd = user.LockoutEnd?.DateTime,
                    AccessFailedCount = user.AccessFailedCount,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt,
                    Pronouns = user.Pronouns,
                    PronouncedName = user.PronouncedName
                };

                return Ok(userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user {UserId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving user details" });
            }
        }

        /// <summary>
        /// Updates user information
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserDto dto)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id.ToString());
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                var currentUserId = User.GetUserId();
                var changes = new List<string>();

                // Update scene name
                if (!string.IsNullOrEmpty(dto.SceneName) && dto.SceneName != user.SceneNameValue)
                {
                    var sceneName = WitchCityRope.Core.ValueObjects.SceneName.Create(dto.SceneName);
                    user.UpdateSceneName(sceneName);
                    changes.Add($"Scene name changed from '{user.SceneNameValue}' to '{dto.SceneName}'");
                }

                // Update role
                if (dto.Role.HasValue && dto.Role.Value != user.Role)
                {
                    var oldRole = user.Role;
                    user.PromoteToRole(dto.Role.Value);
                    changes.Add($"Role changed from '{oldRole}' to '{dto.Role.Value}'");
                }

                // Update active status
                if (dto.IsActive.HasValue && dto.IsActive.Value != user.IsActive)
                {
                    if (dto.IsActive.Value)
                    {
                        user.Reactivate();
                        changes.Add("Account reactivated");
                    }
                    else
                    {
                        user.Deactivate();
                        changes.Add("Account deactivated");
                    }
                }

                // Update vetted status
                if (dto.IsVetted.HasValue && dto.IsVetted.Value != user.IsVetted)
                {
                    if (dto.IsVetted.Value)
                    {
                        user.MarkAsVetted();
                        changes.Add("User marked as vetted");
                    }
                    else
                    {
                        // Note: We can't directly set IsVetted to false, need to use a method if available
                        // For now, log the attempt but don't make the change
                        changes.Add("Attempted to remove vetted status - requires manual review");
                    }
                }

                // Update email confirmed
                if (dto.EmailConfirmed.HasValue && dto.EmailConfirmed.Value != user.EmailConfirmed)
                {
                    user.EmailConfirmed = dto.EmailConfirmed.Value;
                    changes.Add($"Email confirmation status changed to '{dto.EmailConfirmed.Value}'");
                }

                // Update lockout enabled
                if (dto.LockoutEnabled.HasValue && dto.LockoutEnabled.Value != user.LockoutEnabled)
                {
                    user.LockoutEnabled = dto.LockoutEnabled.Value;
                    changes.Add($"Lockout enabled status changed to '{dto.LockoutEnabled.Value}'");
                }

                // Update pronouns
                if (dto.Pronouns != null && dto.Pronouns != user.Pronouns)
                {
                    user.UpdatePronouns(dto.Pronouns);
                    changes.Add($"Pronouns updated to '{dto.Pronouns}'");
                }

                // Update pronounced name
                if (dto.PronouncedName != null && dto.PronouncedName != user.PronouncedName)
                {
                    user.UpdatePronouncedName(dto.PronouncedName);
                    changes.Add($"Pronounced name updated to '{dto.PronouncedName}'");
                }

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return BadRequest(new { message = $"Failed to update user: {errors}" });
                }

                // Log the changes
                if (changes.Any() && !string.IsNullOrEmpty(dto.AdminNote))
                {
                    _logger.LogInformation("User {UserId} updated by admin {AdminId}: {Changes}. Note: {AdminNote}",
                        id, currentUserId, string.Join("; ", changes), dto.AdminNote);
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", id);
                return StatusCode(500, new { message = "An error occurred while updating the user" });
            }
        }

        /// <summary>
        /// Resets user password
        /// </summary>
        [HttpPost("{id}/reset-password")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetUserPasswordDto dto)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id.ToString());
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                var currentUserId = User.GetUserId();
                
                // Generate password reset token and reset password
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);
                
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return BadRequest(new { message = $"Failed to reset password: {errors}" });
                }

                // Mark that password change is required on next login if requested
                if (dto.RequirePasswordChangeOnLogin)
                {
                    user.LastPasswordChangeAt = null; // Force password change
                    await _userManager.UpdateAsync(user);
                }

                _logger.LogWarning("Password reset for user {UserId} by admin {AdminId}. Note: {AdminNote}",
                    id, currentUserId, dto.AdminNote ?? "No note provided");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for user {UserId}", id);
                return StatusCode(500, new { message = "An error occurred while resetting the password" });
            }
        }

        /// <summary>
        /// Manages user lockout status
        /// </summary>
        [HttpPost("{id}/lockout")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ManageLockout(Guid id, [FromBody] UserLockoutDto dto)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id.ToString());
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                var currentUserId = User.GetUserId();
                IdentityResult result;

                if (dto.IsLocked)
                {
                    var lockoutEnd = dto.LockoutEnd?.ToUniversalTime() ?? DateTime.UtcNow.AddYears(100); // Permanent if no end date
                    result = await _userManager.SetLockoutEndDateAsync(user, lockoutEnd);
                    
                    if (result.Succeeded)
                    {
                        _logger.LogWarning("User {UserId} locked out until {LockoutEnd} by admin {AdminId}. Reason: {Reason}",
                            id, lockoutEnd, currentUserId, dto.Reason ?? "No reason provided");
                    }
                }
                else
                {
                    result = await _userManager.SetLockoutEndDateAsync(user, null);
                    await _userManager.ResetAccessFailedCountAsync(user);
                    
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("User {UserId} lockout removed by admin {AdminId}. Reason: {Reason}",
                            id, currentUserId, dto.Reason ?? "No reason provided");
                    }
                }

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return BadRequest(new { message = $"Failed to update lockout status: {errors}" });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error managing lockout for user {UserId}", id);
                return StatusCode(500, new { message = "An error occurred while managing user lockout" });
            }
        }

        /// <summary>
        /// Gets user management statistics
        /// </summary>
        [HttpGet("stats")]
        [ProducesResponseType(typeof(UserStatsDto), 200)]
        [ResponseCache(Duration = 300)] // Cache for 5 minutes
        public async Task<IActionResult> GetUserStats()
        {
            try
            {
                var now = DateTimeOffset.UtcNow;
                var thisMonth = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
                var today = now.Date;

                var stats = new UserStatsDto
                {
                    TotalUsers = await _userManager.Users.CountAsync(),
                    PendingVetting = await _userManager.Users
                        .Include(u => u.UserExtended)
                        .CountAsync(u => u.UserExtended.VettingStatus == Core.Enums.VettingStatus.PendingVetting),
                    OnHold = await _userManager.Users
                        .Include(u => u.UserExtended)
                        .CountAsync(u => u.UserExtended.VettingStatus == Core.Enums.VettingStatus.OnHold),
                    CalculatedAt = now.DateTime
                };

                // Get role breakdown
                var roleGroups = await _userManager.Users
                    .GroupBy(u => u.Role)
                    .Select(g => new { Role = g.Key, Count = g.Count() })
                    .ToListAsync();

                stats.UsersByRole = roleGroups.ToDictionary(
                    g => g.Role.ToString(),
                    g => g.Count
                );

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user statistics");
                return StatusCode(500, new { message = "An error occurred while retrieving statistics" });
            }
        }

        /// <summary>
        /// Gets available roles for dropdown selection
        /// </summary>
        [HttpGet("roles")]
        [ProducesResponseType(typeof(List<RoleDto>), 200)]
        public IActionResult GetRoles()
        {
            try
            {
                var roles = Enum.GetValues<UserRole>()
                    .Select(role => new RoleDto
                    {
                        Name = role.ToString(),
                        DisplayName = role.ToString(),
                        Description = GetRoleDescription(role),
                        Priority = (int)role,
                        IsActive = true
                    })
                    .OrderBy(r => r.Priority)
                    .ToList();

                return Ok(roles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving roles");
                return StatusCode(500, new { message = "An error occurred while retrieving roles" });
            }
        }

        // Admin Notes Endpoints

        /// <summary>
        /// Gets admin notes for a specific user
        /// </summary>
        [HttpGet("{id}/notes")]
        [ProducesResponseType(typeof(List<UserNoteDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetUserNotes(Guid id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id.ToString());
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                var notes = await _userNoteRepository.GetUserNotesAsync(id);
                var noteDtos = notes.Select(note => new UserNoteDto
                {
                    Id = note.Id,
                    UserId = note.UserId,
                    NoteType = note.NoteType.ToString(),
                    Content = note.Content,
                    CreatedById = note.CreatedById,
                    CreatedAt = note.CreatedAt,
                    UpdatedAt = note.UpdatedAt,
                    IsDeleted = note.IsDeleted,
                    DeletedAt = note.DeletedAt,
                    DeletedById = note.DeletedById
                }).ToList();

                return Ok(noteDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notes for user {UserId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving user notes" });
            }
        }

        /// <summary>
        /// Creates a new admin note for a user
        /// </summary>
        [HttpPost("{id}/notes")]
        [ProducesResponseType(typeof(UserNoteDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> CreateUserNote(Guid id, [FromBody] CreateUserNoteDto dto)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id.ToString());
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                var currentUserId = User.GetUserId();
                if (currentUserId == Guid.Empty)
                {
                    return BadRequest(new { message = "Unable to identify current user" });
                }

                // Parse NoteType from string
                if (!Enum.TryParse<NoteType>(dto.NoteType, out var noteType))
                {
                    return BadRequest(new { message = "Invalid note type" });
                }

                var note = UserNote.Create(id, noteType, dto.Content, currentUserId);
                var createdNote = await _userNoteRepository.AddNoteAsync(note);

                var noteDto = new UserNoteDto
                {
                    Id = createdNote.Id,
                    UserId = createdNote.UserId,
                    NoteType = createdNote.NoteType.ToString(),
                    Content = createdNote.Content,
                    CreatedById = createdNote.CreatedById,
                    CreatedAt = createdNote.CreatedAt,
                    UpdatedAt = createdNote.UpdatedAt,
                    IsDeleted = createdNote.IsDeleted,
                    DeletedAt = createdNote.DeletedAt,
                    DeletedById = createdNote.DeletedById
                };

                _logger.LogInformation("Admin note created for user {UserId} by {AdminId}: {NoteType}",
                    id, currentUserId, noteType);

                return CreatedAtAction(nameof(GetUserNote), new { id = id, noteId = createdNote.Id }, noteDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating note for user {UserId}", id);
                return StatusCode(500, new { message = "An error occurred while creating the user note" });
            }
        }

        /// <summary>
        /// Gets a specific admin note
        /// </summary>
        [HttpGet("{id}/notes/{noteId}")]
        [ProducesResponseType(typeof(UserNoteDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetUserNote(Guid id, Guid noteId)
        {
            try
            {
                var note = await _userNoteRepository.GetNoteByIdAsync(noteId);
                if (note == null || note.UserId != id)
                {
                    return NotFound(new { message = "Note not found" });
                }

                var noteDto = new UserNoteDto
                {
                    Id = note.Id,
                    UserId = note.UserId,
                    NoteType = note.NoteType.ToString(),
                    Content = note.Content,
                    CreatedById = note.CreatedById,
                    CreatedAt = note.CreatedAt,
                    UpdatedAt = note.UpdatedAt,
                    IsDeleted = note.IsDeleted,
                    DeletedAt = note.DeletedAt,
                    DeletedById = note.DeletedById
                };

                return Ok(noteDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving note {NoteId} for user {UserId}", noteId, id);
                return StatusCode(500, new { message = "An error occurred while retrieving the note" });
            }
        }

        /// <summary>
        /// Updates an existing admin note
        /// </summary>
        [HttpPut("{id}/notes/{noteId}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateUserNote(Guid id, Guid noteId, [FromBody] UpdateUserNoteDto dto)
        {
            try
            {
                var note = await _userNoteRepository.GetNoteByIdAsync(noteId);
                if (note == null || note.UserId != id)
                {
                    return NotFound(new { message = "Note not found" });
                }

                var currentUserId = User.GetUserId();
                note.Update(dto.Content);
                await _userNoteRepository.UpdateNoteAsync(note);

                _logger.LogInformation("Admin note {NoteId} updated for user {UserId} by {AdminId}",
                    noteId, id, currentUserId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating note {NoteId} for user {UserId}", noteId, id);
                return StatusCode(500, new { message = "An error occurred while updating the note" });
            }
        }

        /// <summary>
        /// Soft deletes an admin note
        /// </summary>
        [HttpDelete("{id}/notes/{noteId}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteUserNote(Guid id, Guid noteId)
        {
            try
            {
                var currentUserId = User.GetUserId();
                await _userNoteRepository.DeleteNoteAsync(noteId, currentUserId);

                _logger.LogInformation("Admin note {NoteId} deleted for user {UserId} by {AdminId}",
                    noteId, id, currentUserId);

                return NoContent();
            }
            catch (InvalidOperationException)
            {
                return NotFound(new { message = "Note not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting note {NoteId} for user {UserId}", noteId, id);
                return StatusCode(500, new { message = "An error occurred while deleting the note" });
            }
        }

        /// <summary>
        /// Gets note statistics for admin dashboard
        /// </summary>
        [HttpGet("notes/stats")]
        [ProducesResponseType(typeof(UserNotesStatsDto), 200)]
        [ResponseCache(Duration = 300)] // Cache for 5 minutes
        public async Task<IActionResult> GetNotesStats()
        {
            try
            {
                var safetyNotesCount = await _userNoteRepository.GetNoteCountAsync(Guid.Empty, NoteType.Safety);
                var totalNotesCount = await _userNoteRepository.GetNoteCountAsync(Guid.Empty);
                var incidentNotesCount = await _userNoteRepository.GetNoteCountAsync(Guid.Empty, NoteType.Incident);
                var vettingNotesCount = await _userNoteRepository.GetNoteCountAsync(Guid.Empty, NoteType.Vetting);

                var stats = new UserNotesStatsDto
                {
                    TotalNotes = totalNotesCount,
                    SafetyNotes = safetyNotesCount,
                    IncidentNotes = incidentNotesCount,
                    VettingNotes = vettingNotesCount,
                    CalculatedAt = DateTime.UtcNow
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notes statistics");
                return StatusCode(500, new { message = "An error occurred while retrieving notes statistics" });
            }
        }

        private static string GetRoleDescription(UserRole role)
        {
            return role switch
            {
                UserRole.Attendee => "Standard event attendee",
                UserRole.Member => "Verified community member with additional privileges",
                UserRole.Organizer => "Event organizer who can create and manage events",
                UserRole.Moderator => "Community moderator who can review incidents and vetting",
                UserRole.Administrator => "System administrator with full access",
                _ => "Unknown role"
            };
        }
    }

    // DTO classes for admin notes functionality
    public class UserNoteDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string NoteType { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Guid CreatedById { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public Guid? DeletedById { get; set; }
    }

    public class CreateUserNoteDto
    {
        public string NoteType { get; set; } = "General";
        public string Content { get; set; } = string.Empty;
    }

    public class UpdateUserNoteDto
    {
        public string Content { get; set; } = string.Empty;
    }

    public class UserNotesStatsDto
    {
        public int TotalNotes { get; set; }
        public int SafetyNotes { get; set; }
        public int IncidentNotes { get; set; }
        public int VettingNotes { get; set; }
        public DateTime CalculatedAt { get; set; }
    }
}
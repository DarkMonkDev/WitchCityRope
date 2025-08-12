using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using WitchCityRope.Core.Enums;

namespace WitchCityRope.Core.DTOs
{
    /// <summary>
    /// Extended user information for admin user management
    /// </summary>
    public class AdminUserDto
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string SceneName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public bool IsActive { get; set; }
        public bool IsVetted { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool LockoutEnabled { get; set; }
        public DateTime? LockoutEnd { get; set; }
        public int AccessFailedCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public string? Pronouns { get; set; }
        public string? PronouncedName { get; set; }
        
        // Computed properties
        public bool IsLockedOut => LockoutEnd.HasValue && LockoutEnd > DateTime.UtcNow;
        public string RoleDisplayName => Role.ToString();
        public string StatusDisplay => GetStatusDisplay();
        
        private string GetStatusDisplay()
        {
            if (!IsActive) return "Inactive";
            if (IsLockedOut) return "Locked Out";
            if (!EmailConfirmed) return "Email Unconfirmed";
            if (IsVetted) return "Active (Vetted)";
            return "Active (Unvetted)";
        }
    }
    
    /// <summary>
    /// Request for searching users in admin interface
    /// </summary>
    public class UserSearchRequest
    {
        public string? SearchTerm { get; set; }
        public UserRole? Role { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsVetted { get; set; }
        public bool? EmailConfirmed { get; set; }
        public bool? IsLockedOut { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string SortBy { get; set; } = "sceneName";
        public string SortDirection { get; set; } = "asc";
    }
    
    /// <summary>
    /// Paginated result for user search
    /// </summary>
    public class PagedUserResult
    {
        public List<AdminUserDto> Users { get; set; } = new();
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
    }
    
    /// <summary>
    /// DTO for updating user information
    /// </summary>
    public class UpdateUserDto
    {
        public string? SceneName { get; set; }
        public UserRole? Role { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsVetted { get; set; }
        public bool? EmailConfirmed { get; set; }
        public bool? LockoutEnabled { get; set; }
        public string? Pronouns { get; set; }
        public string? PronouncedName { get; set; }
        
        [StringLength(1000)]
        public string? AdminNote { get; set; }
    }
    
    /// <summary>
    /// DTO for resetting user password
    /// </summary>
    public class ResetUserPasswordDto
    {
        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string NewPassword { get; set; } = string.Empty;
        
        [Required]
        public bool RequirePasswordChangeOnLogin { get; set; } = true;
        
        [StringLength(500)]
        public string? AdminNote { get; set; }
    }
    
    /// <summary>
    /// DTO for managing user lockout
    /// </summary>
    public class UserLockoutDto
    {
        public bool IsLocked { get; set; }
        public DateTime? LockoutEnd { get; set; }
        public int AccessFailedCount { get; set; }
        
        [StringLength(500)]
        public string? Reason { get; set; }
    }
    
    /// <summary>
    /// Statistics about users for admin dashboard
    /// </summary>
    public class UserStatsDto
    {
        public int TotalUsers { get; set; }
        public int PendingVetting { get; set; }
        public int OnHold { get; set; }
        public DateTime CalculatedAt { get; set; }
        
        // Role breakdown
        public Dictionary<string, int> UsersByRole { get; set; } = new();
    }
    
    /// <summary>
    /// Role information for dropdown/selection
    /// </summary>
    public class RoleDto
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Priority { get; set; }
        public bool IsActive { get; set; }
    }
    
    /// <summary>
    /// Activity log entry for user management actions
    /// </summary>
    public class UserActivityLogDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? Details { get; set; }
        public string PerformedByName { get; set; } = string.Empty;
        public DateTime PerformedAt { get; set; }
        public string Category { get; set; } = string.Empty; // Account, Role, Security, etc.
    }
    
    /// <summary>
    /// Request for user activity log search
    /// </summary>
    public class ActivityLogSearchRequest
    {
        public Guid? UserId { get; set; }
        public string? Action { get; set; }
        public string? Category { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
    
    /// <summary>
    /// Paginated activity log result
    /// </summary>
    public class PagedActivityLogResult
    {
        public List<UserActivityLogDto> Activities { get; set; } = new();
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
    }
}
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
        public UserStatus Status { get; set; } // Consolidated field replacing IsVetted
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
        
        // Backward compatibility property
        public bool IsVetted
        {
            get => Status == UserStatus.Vetted;
            set => Status = value ? UserStatus.Vetted : UserStatus.NoApplication;
        }
        
        private string GetStatusDisplay()
        {
            if (!IsActive) return "Inactive";
            if (IsLockedOut) return "Locked Out";
            if (!EmailConfirmed) return "Email Unconfirmed";
            return Status switch
            {
                UserStatus.Vetted => "Active (Vetted)",
                UserStatus.PendingReview => "Pending Review",
                UserStatus.NoApplication => "No Application",
                UserStatus.OnHold => "On Hold",
                UserStatus.Banned => "Banned",
                _ => "Unknown"
            };
        }
    }
    
    /// <summary>
    /// Detailed user information for user detail page
    /// </summary>
    public class UserDetailDto
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string SceneName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PronouncedName { get; set; }
        public string? Pronouns { get; set; }
        public UserRole Role { get; set; }
        public UserStatus Status { get; set; } // Consolidated field
        public DateTime DateOfBirth { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool LockoutEnabled { get; set; }
        public DateTime? LockoutEnd { get; set; }
        public int AccessFailedCount { get; set; }
        
        // Stats
        public int EventsAttended { get; set; }
        public int EventsCreated { get; set; }
        public int IncidentReports { get; set; }
        
        // Computed properties
        public int Age => CalculateAge();
        public bool IsLockedOut => LockoutEnd.HasValue && LockoutEnd > DateTime.UtcNow;
        
        // Collections (loaded separately for performance)
        public List<UserEventDto> Events { get; set; } = new();
        public List<UserNoteDto> AdminNotes { get; set; } = new();
        public List<VettingApplicationDto> VettingApplications { get; set; } = new();
        public List<UserAuditLogDto> AuditTrail { get; set; } = new();
        
        private int CalculateAge()
        {
            var today = DateTime.Today;
            var age = today.Year - DateOfBirth.Year;
            if (DateOfBirth.Date > today.AddYears(-age)) age--;
            return age;
        }
    }
    
    /// <summary>
    /// User event participation information
    /// </summary>
    public class UserEventDto
    {
        public Guid EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string EventType { get; set; } = string.Empty;
        public bool IsAttending { get; set; }
        public string RsvpStatus { get; set; } = string.Empty;
        public DateTime RsvpDate { get; set; }
        public bool IsOrganizer { get; set; }
        public string? Notes { get; set; }
    }
    
    /// <summary>
    /// User audit log entry
    /// </summary>
    public class UserAuditLogDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? Details { get; set; }
        public Guid PerformedById { get; set; }
        public string PerformedByName { get; set; } = string.Empty;
        public DateTime PerformedAt { get; set; }
        public string Category { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// Request for searching users in admin interface
    /// </summary>
    public class UserSearchRequest
    {
        public string? SearchTerm { get; set; }
        public UserRole? Role { get; set; }
        public bool? IsActive { get; set; }
        public UserStatus? Status { get; set; } // Updated to use new Status field
        public bool? EmailConfirmed { get; set; }
        public bool? IsLockedOut { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string SortBy { get; set; } = "sceneName";
        public string SortDirection { get; set; } = "asc";
        
        // Backward compatibility property
        public bool? IsVetted
        {
            get => Status == UserStatus.Vetted;
            set
            {
                if (value.HasValue)
                {
                    Status = value.Value ? UserStatus.Vetted : UserStatus.NoApplication;
                }
            }
        }
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
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? SceneName { get; set; }
        public string? Email { get; set; }
        public UserRole? Role { get; set; }
        public bool? IsActive { get; set; }
        public UserStatus? Status { get; set; } // Updated to use new Status field
        public bool? EmailConfirmed { get; set; }
        public bool? LockoutEnabled { get; set; }
        public string? Pronouns { get; set; }
        public string? PronouncedName { get; set; }
        
        [StringLength(1000)]
        public string? AdminNote { get; set; }
        
        // Backward compatibility property
        public bool? IsVetted
        {
            get => Status == UserStatus.Vetted;
            set
            {
                if (value.HasValue)
                {
                    Status = value.Value ? UserStatus.Vetted : UserStatus.NoApplication;
                }
            }
        }
    }
    
    /// <summary>
    /// DTO for updating user status with reason
    /// </summary>
    public class UpdateUserStatusDto
    {
        public UserStatus Status { get; set; }
        
        [Required]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;
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
    /// Statistics about users for admin dashboard with consolidated status
    /// </summary>
    public class UserStatsDto
    {
        public int TotalUsers { get; set; }
        public int PendingVetting { get; set; }    // Status = PendingReview
        public int OnHold { get; set; }            // Status = OnHold
        public DateTime CalculatedAt { get; set; }
        
        // Status breakdown
        public Dictionary<string, int> UsersByStatus { get; set; } = new();
        
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
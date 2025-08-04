using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Core.DTOs
{
    /// <summary>
    /// DTO for member list display in admin interface.
    /// </summary>
    public class MemberListDto
    {
        public Guid Id { get; set; }
        public string SceneName { get; set; } = string.Empty;
        public string RealName { get; set; } = string.Empty; // Decrypted for admin view
        public string? FetLifeName { get; set; }
        public string Email { get; set; } = string.Empty;
        public DateTime DateJoined { get; set; }
        public int EventsAttended { get; set; }
        public string MembershipStatus { get; set; } = string.Empty; // "Vetted", "Unvetted", "Pending"
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Request parameters for searching and filtering members.
    /// </summary>
    public class MemberSearchRequest
    {
        public string? SearchTerm { get; set; }
        public string VettingStatus { get; set; } = "vetted"; // all, vetted, unvetted
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 100;
        public string SortBy { get; set; } = "sceneName";
        public string SortDirection { get; set; } = "asc";
    }

    /// <summary>
    /// Paginated result of member search.
    /// </summary>
    public class PagedMemberResult
    {
        public List<MemberListDto> Members { get; set; } = new();
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
    }

    /// <summary>
    /// Detailed member information for admin view.
    /// </summary>
    public class MemberDetailDto
    {
        public Guid Id { get; set; }
        public string SceneName { get; set; } = string.Empty;
        public string RealName { get; set; } = string.Empty;
        public string? FetLifeName { get; set; }
        public string Email { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string? Pronouns { get; set; }
        public string? PronouncedName { get; set; }
        public string Role { get; set; } = string.Empty;
        public bool IsVetted { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public int TotalEventsAttended { get; set; }
        public decimal TotalSpent { get; set; }

        // Computed properties
        public int Age => DateTime.Today.Year - DateOfBirth.Year - 
            (DateTime.Today.DayOfYear < DateOfBirth.DayOfYear ? 1 : 0);
        
        public string Status => GetMembershipStatus();

        private string GetMembershipStatus()
        {
            if (!IsActive) return "Inactive";
            if (IsVetted) return "Vetted";
            return "Unvetted";
        }
    }

    /// <summary>
    /// Member's event attendance history.
    /// </summary>
    public class MemberEventHistoryDto
    {
        public Guid EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string RegistrationStatus { get; set; } = string.Empty;
        public decimal? PricePaid { get; set; }
        public bool DidAttend { get; set; }
        public DateTime? CheckedInAt { get; set; }
        public string? RefundReason { get; set; }
    }

    /// <summary>
    /// DTO for updating member information.
    /// </summary>
    public class UpdateMemberDto
    {
        public string? SceneName { get; set; }
        public string? FetLifeName { get; set; }
        public string? Pronouns { get; set; }
        public string? PronouncedName { get; set; }
        public string? Role { get; set; }
        public bool? IsVetted { get; set; }
        public bool? IsActive { get; set; }
    }

    /// <summary>
    /// Statistics about members for admin dashboard.
    /// </summary>
    public class MemberStatsDto
    {
        public int TotalMembers { get; set; }
        public int VettedMembers { get; set; }
        public int ActiveToday { get; set; }
        public int NewThisMonth { get; set; }
        public DateTime CalculatedAt { get; set; }
    }

    /// <summary>
    /// Complete member detail view model including related data.
    /// </summary>
    public class MemberDetailViewModel
    {
        public MemberDetailDto Member { get; set; } = new();
        public List<UserNoteDto> Notes { get; set; } = new();
        public List<MemberEventHistoryDto> EventHistory { get; set; } = new();
        public List<IncidentSummaryDto> Incidents { get; set; } = new();
    }

    /// <summary>
    /// DTO for displaying user notes.
    /// </summary>
    public class UserNoteDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string NoteType { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string CreatedByName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }

    /// <summary>
    /// DTO for creating a new user note.
    /// </summary>
    public class CreateUserNoteDto
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        public string NoteType { get; set; } = "General";

        [Required]
        [StringLength(5000, MinimumLength = 1)]
        public string Content { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for updating an existing user note.
    /// </summary>
    public class UpdateUserNoteDto
    {
        [Required]
        [StringLength(5000, MinimumLength = 1)]
        public string Content { get; set; } = string.Empty;
    }

    /// <summary>
    /// Summary of incidents for member detail view.
    /// </summary>
    public class IncidentSummaryDto
    {
        public Guid Id { get; set; }
        public DateTime IncidentDate { get; set; }
        public string IncidentType { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string ReferenceNumber { get; set; } = string.Empty;
    }
}
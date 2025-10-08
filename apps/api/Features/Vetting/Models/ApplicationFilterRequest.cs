namespace WitchCityRope.Api.Features.Vetting.Models;

/// <summary>
/// Filter and pagination request for reviewer dashboard
/// Supports all filtering options from the functional spec
/// </summary>
public class ApplicationFilterRequest
{
    // Pagination
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;

    // Status filtering
    public List<string> StatusFilters { get; set; } = new(); // "New", "InReview", "PendingReferences", etc.

    // Assignment filtering
    public bool? OnlyMyAssignments { get; set; }
    public bool? OnlyUnassigned { get; set; }
    public Guid? AssignedReviewerId { get; set; }

    // Priority filtering
    public List<int> PriorityFilters { get; set; } = new(); // 1=Standard, 2=High, 3=Urgent

    // Experience filtering
    public List<int> ExperienceLevelFilters { get; set; } = new(); // 1=Beginner, 2=Intermediate, etc.
    public int? MinYearsExperience { get; set; }
    public int? MaxYearsExperience { get; set; }

    // Skills/interests filtering
    public List<string> SkillsFilters { get; set; } = new();

    // Date range filtering
    public DateTime? SubmittedAfter { get; set; }
    public DateTime? SubmittedBefore { get; set; }
    public DateTime? LastActivityAfter { get; set; }
    public DateTime? LastActivityBefore { get; set; }

    // Search functionality
    public string? SearchQuery { get; set; } // Searches scene name, application number

    // Reference status filtering
    public bool? OnlyCompleteReferences { get; set; }
    public bool? OnlyPendingReferences { get; set; }

    // Sorting
    public string SortBy { get; set; } = "SubmittedAt"; // "SubmittedAt", "LastActivity", "Priority", "Status"
    public string SortDirection { get; set; } = "Desc"; // "Asc", "Desc"
}
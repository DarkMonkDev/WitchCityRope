using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Vetting.Entities;

/// <summary>
/// Manages vetting team members and workload distribution
/// Tracks reviewer performance and specializations
/// </summary>
public class VettingReviewer
{
    public VettingReviewer()
    {
        Id = Guid.NewGuid();
        IsActive = true;
        MaxWorkload = 10; // Default maximum concurrent reviews
        CurrentWorkload = 0;
        TotalReviewsCompleted = 0;
        AverageReviewTimeHours = 0;
        ApprovalRate = 0;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        IsAvailable = true;
        TimeZone = "UTC";
        
        AssignedApplications = new List<VettingApplication>();
        Decisions = new List<VettingDecision>();
        Notes = new List<VettingApplicationNote>();
    }

    // Primary Key
    public Guid Id { get; set; }

    // User Link
    public Guid UserId { get; set; }

    // Reviewer Settings
    public bool IsActive { get; set; }
    public int MaxWorkload { get; set; }
    public int CurrentWorkload { get; set; }

    // Specializations (JSON array)
    public string SpecializationsJson { get; set; } = "[]";
    [NotMapped]
    public List<string> Specializations 
    { 
        get => JsonSerializer.Deserialize<List<string>>(SpecializationsJson) ?? new List<string>();
        set => SpecializationsJson = JsonSerializer.Serialize(value);
    }

    // Performance Metrics
    public int TotalReviewsCompleted { get; set; }
    public decimal AverageReviewTimeHours { get; set; }
    public decimal ApprovalRate { get; set; } // Percentage
    public DateTime? LastActivityAt { get; set; }

    // Availability Settings
    public bool IsAvailable { get; set; } = true;
    public DateTime? UnavailableUntil { get; set; }
    public string TimeZone { get; set; } = "UTC";

    // Working Hours (JSON object)
    public string? WorkingHoursJson { get; set; }

    // Standard Audit Fields
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation Properties
    public ApplicationUser User { get; set; } = null!;
    public ICollection<VettingApplication> AssignedApplications { get; set; }
    public ICollection<VettingDecision> Decisions { get; set; }
    public ICollection<VettingApplicationNote> Notes { get; set; }
}
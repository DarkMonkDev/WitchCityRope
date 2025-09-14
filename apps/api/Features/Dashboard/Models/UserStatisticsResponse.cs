namespace WitchCityRope.Api.Features.Dashboard.Models;

/// <summary>
/// Response model for user membership statistics
/// Contains attendance history and membership metrics
/// </summary>
public class UserStatisticsResponse
{
    /// <summary>
    /// Whether the user is currently vetted
    /// </summary>
    public bool IsVerified { get; set; }

    /// <summary>
    /// Total number of events the user has attended (checked in)
    /// </summary>
    public int EventsAttended { get; set; }

    /// <summary>
    /// Number of months the user has been a member
    /// </summary>
    public int MonthsAsMember { get; set; }

    /// <summary>
    /// Number of events attended in the last 6 months (simplified consecutive metric)
    /// </summary>
    public int RecentEvents { get; set; }

    /// <summary>
    /// Date the user joined the community
    /// </summary>
    public DateTime JoinDate { get; set; }

    /// <summary>
    /// Current vetting status (0 = Submitted, 1 = InReview, 2 = Approved, 3 = Rejected)
    /// </summary>
    public int VettingStatus { get; set; }

    /// <summary>
    /// Next interview date (if applicable and scheduled)
    /// </summary>
    public DateTime? NextInterviewDate { get; set; }

    /// <summary>
    /// Total number of events the user is currently registered for (future events)
    /// </summary>
    public int UpcomingRegistrations { get; set; }

    /// <summary>
    /// Number of events the user has registered for but cancelled
    /// </summary>
    public int CancelledRegistrations { get; set; }
}
namespace WitchCityRope.Api.Features.EmailTemplates.Entities;

/// <summary>
/// Defines user segments for targeted email campaigns
/// Each segment represents a specific group of users based on their role, vetting status, or permissions
/// </summary>
public enum UserSegment
{
    /// <summary>
    /// All vetted members (VettingStatus == 3 - Approved)
    /// Full access to all member-only events and features
    /// </summary>
    AllVettedMembers,

    /// <summary>
    /// All pre-vetted members (VettingStatus NOT IN (4, 5) AND IsActive=true)
    /// Includes users with UnderReview, InterviewApproved, FinalReview, Approved, Withdrawn, and OnHold status
    /// Excludes Denied (4) and inactive users
    /// </summary>
    AllPreVettedMembers,

    /// <summary>
    /// All teachers (Role contains "Teacher")
    /// Users authorized to lead workshops and classes
    /// </summary>
    AllTeachers,

    /// <summary>
    /// All dungeon monitors (Role contains "DungeonMonitor")
    /// Users responsible for safety monitoring during events
    /// </summary>
    AllDMs,

    /// <summary>
    /// All safety team members (Role contains "SafetyTeam")
    /// Core safety team coordinating incident response and safety protocols
    /// </summary>
    AllSafetyTeam,

    /// <summary>
    /// All administrators (Role contains "Administrator")
    /// Full system access and administrative privileges
    /// </summary>
    AllAdmins,

    /// <summary>
    /// Users with unverified email addresses (EmailConfirmed == false)
    /// Accounts created but email verification not completed
    /// </summary>
    EmailNotVerified,

    /// <summary>
    /// Users with vetting applications under review (VettingStatus == 0 - UnderReview)
    /// Applications submitted and awaiting initial review
    /// </summary>
    VettingPending,

    /// <summary>
    /// Newly imported vetted users who have never logged in (VettingStatus == Approved AND EmailConfirmed == false)
    /// These are users imported via VettedMemberImport tool who need welcome emails with password reset links
    /// Uses EmailConfirmed as proxy for first login since LastLoginAt may not be available
    /// </summary>
    NewImportedUsers
}

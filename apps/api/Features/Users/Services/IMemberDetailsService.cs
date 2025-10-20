using WitchCityRope.Api.Features.Users.Models.MemberDetails;

namespace WitchCityRope.Api.Features.Users.Services;

/// <summary>
/// Service interface for member details admin operations
/// Provides comprehensive member information for admin member management
/// </summary>
public interface IMemberDetailsService
{
    /// <summary>
    /// Get comprehensive member details including participation summary
    /// </summary>
    Task<(bool Success, MemberDetailsResponse? Response, string Error)> GetMemberDetailsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get vetting details including questionnaire responses
    /// </summary>
    Task<(bool Success, VettingDetailsResponse? Response, string Error)> GetVettingDetailsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get paginated event history for a member
    /// </summary>
    Task<(bool Success, EventHistoryResponse? Response, string Error)> GetEventHistoryAsync(
        Guid userId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get safety incidents involving a member
    /// </summary>
    Task<(bool Success, MemberIncidentsResponse? Response, string Error)> GetMemberIncidentsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all notes for a member (unified notes system)
    /// </summary>
    Task<(bool Success, List<UserNoteResponse>? Response, string Error)> GetMemberNotesAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new note for a member
    /// </summary>
    Task<(bool Success, UserNoteResponse? Response, string Error)> CreateMemberNoteAsync(
        Guid userId,
        CreateUserNoteRequest request,
        Guid authorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update member status (active/inactive) and auto-create status change note
    /// </summary>
    Task<(bool Success, string Error)> UpdateMemberStatusAsync(
        Guid userId,
        UpdateMemberStatusRequest request,
        Guid performedByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update member role
    /// </summary>
    Task<(bool Success, string Error)> UpdateMemberRoleAsync(
        Guid userId,
        UpdateMemberRoleRequest request,
        Guid performedByUserId,
        CancellationToken cancellationToken = default);
}

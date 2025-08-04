using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WitchCityRope.Core.DTOs;

namespace WitchCityRope.Core.Repositories
{
    /// <summary>
    /// Repository interface for member management operations.
    /// </summary>
    public interface IMemberRepository
    {
        /// <summary>
        /// Searches and retrieves members based on the provided criteria.
        /// </summary>
        /// <param name="request">Search and pagination parameters.</param>
        /// <returns>Paginated list of members.</returns>
        Task<PagedMemberResult> GetMembersAsync(MemberSearchRequest request);

        /// <summary>
        /// Gets detailed information about a specific member.
        /// </summary>
        /// <param name="memberId">The member's user ID.</param>
        /// <returns>Detailed member information or null if not found.</returns>
        Task<MemberDetailDto?> GetMemberDetailAsync(Guid memberId);

        /// <summary>
        /// Gets a member's event attendance history.
        /// </summary>
        /// <param name="memberId">The member's user ID.</param>
        /// <returns>List of events the member has registered for or attended.</returns>
        Task<List<MemberEventHistoryDto>> GetMemberEventHistoryAsync(Guid memberId);

        /// <summary>
        /// Gets overall member statistics for the admin dashboard.
        /// </summary>
        /// <returns>Member statistics summary.</returns>
        Task<MemberStatsDto> GetMemberStatsAsync();

        /// <summary>
        /// Updates a member's information.
        /// </summary>
        /// <param name="memberId">The member's user ID.</param>
        /// <param name="dto">The update data.</param>
        Task UpdateMemberAsync(Guid memberId, UpdateMemberDto dto);

        /// <summary>
        /// Checks if a scene name is already in use by another member.
        /// </summary>
        /// <param name="sceneName">The scene name to check.</param>
        /// <param name="excludeUserId">Optional user ID to exclude from the check.</param>
        /// <returns>True if the scene name is taken, false otherwise.</returns>
        Task<bool> IsSceneNameTakenAsync(string sceneName, Guid? excludeUserId = null);

        /// <summary>
        /// Gets a list of members who have attended a specific event.
        /// </summary>
        /// <param name="eventId">The event ID.</param>
        /// <returns>List of member summaries who attended the event.</returns>
        Task<List<MemberListDto>> GetEventAttendeesAsync(Guid eventId);

        /// <summary>
        /// Gets incidents associated with a member.
        /// </summary>
        /// <param name="memberId">The member's user ID.</param>
        /// <returns>List of incident summaries.</returns>
        Task<List<IncidentSummaryDto>> GetMemberIncidentsAsync(Guid memberId);
    }
}
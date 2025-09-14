using System;
using System.Threading.Tasks;
using WitchCityRope.Core.DTOs;
using WitchCityRope.Core.Models;

namespace WitchCityRope.Api.Interfaces
{
    /// <summary>
    /// Interface for vetting service operations
    /// </summary>
    public interface IVettingService
    {
        /// <summary>
        /// Submits a new vetting application
        /// </summary>
        Task<VettingApplicationResponse> SubmitApplicationAsync(Core.DTOs.VettingApplicationRequest request, Guid userId);

        /// <summary>
        /// Retrieves vetting applications with pagination
        /// </summary>
        Task<Core.Models.PagedResult<VettingApplicationDto>> GetApplicationsAsync(string? status, int page, int pageSize);

        /// <summary>
        /// Reviews a vetting application
        /// </summary>
        Task<ReviewApplicationResponse> ReviewApplicationAsync(Guid applicationId, Core.DTOs.ReviewApplicationRequest request, Guid reviewerId);
    }
}
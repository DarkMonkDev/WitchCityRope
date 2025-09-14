using System;
using System.Threading.Tasks;
using WitchCityRope.Core.DTOs;
using WitchCityRope.Core.Models;

namespace WitchCityRope.Api.Interfaces
{
    /// <summary>
    /// Interface for safety service operations
    /// </summary>
    public interface ISafetyService
    {
        /// <summary>
        /// Submits an incident report
        /// </summary>
        Task<IncidentReportResponse> SubmitIncidentReportAsync(Core.DTOs.IncidentReportRequest request, Guid? reporterId);

        /// <summary>
        /// Retrieves incident reports with pagination
        /// </summary>
        Task<Core.Models.PagedResult<IncidentReportDto>> GetIncidentReportsAsync(string? status, int page, int pageSize);
    }
}
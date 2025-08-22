using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WitchCityRope.Web.Models;
using System.Net.Http;
using System.Net.Http.Json;
using WitchCityRope.Core.Enums;
// DTOs are defined in WitchCityRope.Web.Models

namespace WitchCityRope.Web.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly ILogger<DashboardService> _logger;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);
        
        public DashboardService(
            HttpClient httpClient, 
            IMemoryCache cache,
            ILogger<DashboardService> logger)
        {
            _httpClient = httpClient;
            _cache = cache;
            _logger = logger;
        }
        
        public async Task<DashboardViewModel> GetDashboardDataAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"dashboard_{userId}";
            
            if (_cache.TryGetValue<DashboardViewModel>(cacheKey, out var cachedData))
            {
                _logger.LogDebug("Returning cached dashboard data for user {UserId}", userId);
                return cachedData;
            }
            
            try
            {
                // Fetch all data in parallel for performance
                var upcomingEventsTask = GetUpcomingEventsAsync(userId, 3, cancellationToken);
                var statsTask = GetMembershipStatsAsync(userId, cancellationToken);
                var userTask = _httpClient.GetFromJsonAsync<DashboardDto>($"api/dashboard/{userId}", cancellationToken);
                
                await Task.WhenAll(upcomingEventsTask, statsTask, userTask);
                
                var dashboardData = new DashboardViewModel
                {
                    SceneName = userTask.Result?.SceneName ?? string.Empty,
                    Role = userTask.Result?.Role ?? WitchCityRope.Core.Enums.UserRole.Attendee,
                    VettingStatus = userTask.Result?.VettingStatus ?? WitchCityRope.Core.Entities.VettingStatus.Submitted,
                    UpcomingEvents = await upcomingEventsTask,
                    Stats = await statsTask
                };
                
                _cache.Set(cacheKey, dashboardData, _cacheExpiration);
                _logger.LogInformation("Dashboard data cached for user {UserId}", userId);
                
                return dashboardData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching dashboard data for user {UserId}", userId);
                
                // Return minimal dashboard data on error
                return new DashboardViewModel
                {
                    SceneName = "Guest",
                    Role = WitchCityRope.Core.Enums.UserRole.Attendee,
                    VettingStatus = WitchCityRope.Core.Entities.VettingStatus.Submitted,
                    UpcomingEvents = new List<Models.EventViewModel>(),
                    Stats = new MembershipStatsViewModel()
                };
            }
        }
        
        public async Task<List<Models.EventViewModel>> GetUpcomingEventsAsync(Guid userId, int count = 3, CancellationToken cancellationToken = default)
        {
            try
            {
                var eventsResponse = await _httpClient.GetFromJsonAsync<List<EventDto>>(
                    $"api/dashboard/users/{userId}/upcoming-events?count={count}", cancellationToken);
                
                // Map API response to view model
                if (eventsResponse != null)
                {
                    return eventsResponse.Select(e => new Models.EventViewModel
                    {
                        Id = e.Id,
                        Title = e.Title,
                        StartDate = e.StartDate,
                        EndDate = e.EndDate,
                        Location = e.Location,
                        EventType = e.EventType,
                        InstructorName = e.InstructorName,
                        RegistrationStatus = e.RegistrationStatus,
                        TicketId = e.TicketId
                    }).ToList();
                }
                
                return new List<Models.EventViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching upcoming events for user {UserId}", userId);
                // Return empty list on error to allow dashboard to still load
                return new List<Models.EventViewModel>();
            }
        }
        
        public async Task<MembershipStatsViewModel> GetMembershipStatsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var statsResponse = await _httpClient.GetFromJsonAsync<MembershipStatsDto>(
                    $"api/dashboard/users/{userId}/stats", cancellationToken);
                
                if (statsResponse != null)
                {
                    return new MembershipStatsViewModel
                    {
                        IsVerified = statsResponse.IsVerified,
                        EventsAttended = statsResponse.EventsAttended,
                        MonthsAsMember = statsResponse.MonthsAsMember,
                        ConsecutiveEvents = statsResponse.ConsecutiveEvents,
                        JoinDate = statsResponse.JoinDate,
                        VettingStatus = statsResponse.VettingStatus,
                        NextInterviewDate = statsResponse.NextInterviewDate
                    };
                }
                
                return new MembershipStatsViewModel
                {
                    IsVerified = false,
                    EventsAttended = 0,
                    MonthsAsMember = 0,
                    ConsecutiveEvents = 0,
                    JoinDate = DateTime.UtcNow,
                    VettingStatus = WitchCityRope.Core.Entities.VettingStatus.Submitted,
                    NextInterviewDate = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching membership stats for user {UserId}", userId);
                // Return default stats on error
                return new MembershipStatsViewModel
                {
                    IsVerified = false,
                    EventsAttended = 0,
                    MonthsAsMember = 0,
                    ConsecutiveEvents = 0,
                    JoinDate = DateTime.UtcNow,
                    VettingStatus = WitchCityRope.Core.Entities.VettingStatus.Submitted,
                    NextInterviewDate = null
                };
            }
        }
    }
}
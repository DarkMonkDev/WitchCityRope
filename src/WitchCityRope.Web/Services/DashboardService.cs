using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WitchCityRope.Web.Models;
using WitchCityRope.Core.Enums;
using WitchCityRope.Core.DTOs;

namespace WitchCityRope.Web.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly ApiClient _apiClient;
        private readonly IMemoryCache _cache;
        private readonly ILogger<DashboardService> _logger;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);
        
        public DashboardService(
            ApiClient apiClient,
            IMemoryCache cache,
            ILogger<DashboardService> logger)
        {
            _apiClient = apiClient;
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
                // Get basic dashboard data from API
                var dashboardDto = await _apiClient.GetAsync<Core.DTOs.DashboardDto>($"Dashboard/{userId}");
                
                if (dashboardDto == null)
                {
                    _logger.LogWarning("Dashboard data not found for user: {UserId}", userId);
                    return new DashboardViewModel
                    {
                        SceneName = "Guest",
                        Role = UserRole.Attendee,
                        VettingStatus = Core.Enums.VettingStatus.NotStarted,
                        UpcomingEvents = new List<Models.EventViewModel>(),
                        Stats = new MembershipStatsViewModel()
                    };
                }
                
                // Fetch all data in parallel for performance
                var upcomingEventsTask = GetUpcomingEventsAsync(userId, 3, cancellationToken);
                var statsTask = GetMembershipStatsAsync(userId, cancellationToken);
                
                await Task.WhenAll(upcomingEventsTask, statsTask);
                
                var dashboardData = new DashboardViewModel
                {
                    SceneName = dashboardDto.SceneName ?? "Guest",
                    Role = dashboardDto.Role,
                    VettingStatus = dashboardDto.VettingStatus,
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
                    Role = UserRole.Attendee,
                    VettingStatus = Core.Enums.VettingStatus.NotStarted,
                    UpcomingEvents = new List<Models.EventViewModel>(),
                    Stats = new MembershipStatsViewModel()
                };
            }
        }
        
        public async Task<List<Models.EventViewModel>> GetUpcomingEventsAsync(Guid userId, int count = 3, CancellationToken cancellationToken = default)
        {
            try
            {
                var events = await _apiClient.GetAsync<List<DashboardEventDto>>($"Dashboard/users/{userId}/upcoming-events?count={count}");
                
                if (events == null)
                {
                    return new List<Models.EventViewModel>();
                }
                
                // Convert API DTOs to view models
                return events.Select(e => new Models.EventViewModel
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
                var stats = await _apiClient.GetAsync<Core.DTOs.MembershipStatsDto>($"Dashboard/users/{userId}/stats");
                
                if (stats == null)
                {
                    return new MembershipStatsViewModel
                    {
                        IsVerified = false,
                        EventsAttended = 0,
                        MonthsAsMember = 0,
                        ConsecutiveEvents = 0,
                        JoinDate = DateTime.UtcNow,
                        VettingStatus = Core.Enums.VettingStatus.NotStarted,
                        NextInterviewDate = null
                    };
                }
                
                // Convert API DTO to view model
                return new MembershipStatsViewModel
                {
                    IsVerified = stats.IsVerified,
                    EventsAttended = stats.EventsAttended,
                    MonthsAsMember = stats.MonthsAsMember,
                    ConsecutiveEvents = stats.ConsecutiveEvents,
                    JoinDate = stats.JoinDate,
                    VettingStatus = stats.VettingStatus,
                    NextInterviewDate = stats.NextInterviewDate
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
                    VettingStatus = Core.Enums.VettingStatus.NotStarted,
                    NextInterviewDate = null
                };
            }
        }
    }
}
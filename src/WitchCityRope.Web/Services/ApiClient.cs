using System.Net.Http.Json;
using System.Text.Json;

namespace WitchCityRope.Web.Services;

/// <summary>
/// HTTP client service for making API calls to the backend
/// </summary>
public class ApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiClient> _logger;

    public ApiClient(HttpClient httpClient, ILogger<ApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }
    
    #region Generic Methods
    
    public async Task<TResponse> PostAsync<TRequest, TResponse>(string endpoint, TRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/v1/{endpoint}", request);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("API call failed: {StatusCode} - {Error}", response.StatusCode, error);
                throw new HttpRequestException($"API call failed: {response.StatusCode}");
            }
            
            var result = await response.Content.ReadFromJsonAsync<TResponse>();
            return result ?? throw new InvalidOperationException("Invalid response from API");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling API endpoint: {Endpoint}", endpoint);
            throw;
        }
    }
    
    #endregion

    #region Events

    public async Task<List<EventViewModel>> GetEventsAsync(int page = 1, int pageSize = 12)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<EventViewModel>>(
                $"api/events?page={page}&pageSize={pageSize}");
            return response ?? new List<EventViewModel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching events");
            throw;
        }
    }

    public async Task<EventDetailViewModel> GetEventDetailsAsync(int eventId)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<EventDetailViewModel>(
                $"api/events/{eventId}");
            return response ?? throw new Exception("Event not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching event details for ID: {EventId}", eventId);
            throw;
        }
    }

    public async Task<RegistrationResult> RegisterForEventAsync(int eventId)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/events/{eventId}/register", new { });
            response.EnsureSuccessStatusCode();
            
            return await response.Content.ReadFromJsonAsync<RegistrationResult>() 
                ?? new RegistrationResult { Success = false, Message = "Unknown error" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering for event ID: {EventId}", eventId);
            return new RegistrationResult { Success = false, Message = "Registration failed" };
        }
    }

    public async Task<List<EventViewModel>> GetRecommendedEventsAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<EventViewModel>>(
                "api/events/recommended");
            return response ?? new List<EventViewModel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching recommended events");
            return new List<EventViewModel>();
        }
    }

    #endregion

    #region Member Dashboard

    public async Task<MemberStats> GetMemberStatsAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<MemberStats>("api/members/stats");
            return response ?? new MemberStats();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching member stats");
            return new MemberStats();
        }
    }

    public async Task<List<UpcomingEventViewModel>> GetUpcomingEventsAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<UpcomingEventViewModel>>(
                "api/members/upcoming-events");
            return response ?? new List<UpcomingEventViewModel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching upcoming events");
            return new List<UpcomingEventViewModel>();
        }
    }

    public async Task<List<AnnouncementViewModel>> GetAnnouncementsAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<AnnouncementViewModel>>(
                "api/announcements");
            return response ?? new List<AnnouncementViewModel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching announcements");
            return new List<AnnouncementViewModel>();
        }
    }

    #endregion

    #region Tickets

    public async Task<List<TicketViewModel>> GetMyTicketsAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<TicketViewModel>>(
                "api/tickets/my-tickets");
            return response ?? new List<TicketViewModel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching tickets");
            return new List<TicketViewModel>();
        }
    }

    public async Task<TicketDetailViewModel?> GetTicketDetailAsync(int ticketId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<TicketDetailViewModel>(
                $"api/tickets/{ticketId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching ticket detail for ID: {TicketId}", ticketId);
            return null;
        }
    }

    #endregion

    #region Admin - Event Management

    public async Task<List<EventManagementViewModel>> GetManagedEventsAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<EventManagementViewModel>>(
                "api/admin/events");
            return response ?? new List<EventManagementViewModel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching managed events");
            return new List<EventManagementViewModel>();
        }
    }

    public async Task<EventFormModel> CreateEventAsync(EventFormModel model)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/admin/events", model);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<EventFormModel>() ?? model;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating event");
            throw;
        }
    }

    public async Task UpdateEventAsync(int eventId, EventFormModel model)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/admin/events/{eventId}", model);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating event ID: {EventId}", eventId);
            throw;
        }
    }

    #endregion

    #region Admin - Vetting

    public async Task<VettingStatsViewModel> GetVettingStatsAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<VettingStatsViewModel>(
                "api/admin/vetting/stats");
            return response ?? new VettingStatsViewModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching vetting stats");
            return new VettingStatsViewModel();
        }
    }

    public async Task<List<VettingApplicationViewModel>> GetVettingApplicationsAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<VettingApplicationViewModel>>(
                $"api/admin/vetting/applications?page={page}&pageSize={pageSize}");
            return response ?? new List<VettingApplicationViewModel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching vetting applications");
            return new List<VettingApplicationViewModel>();
        }
    }

    public async Task<VettingApplicationDetailViewModel?> GetVettingApplicationDetailAsync(int applicationId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<VettingApplicationDetailViewModel>(
                $"api/admin/vetting/applications/{applicationId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching vetting application detail for ID: {ApplicationId}", applicationId);
            return null;
        }
    }

    public async Task UpdateVettingStatusAsync(int applicationId, string status)
    {
        try
        {
            var response = await _httpClient.PatchAsJsonAsync(
                $"api/admin/vetting/applications/{applicationId}/status",
                new { status });
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating vetting status for ID: {ApplicationId}", applicationId);
            throw;
        }
    }

    public async Task UpdateVettingPriorityAsync(int applicationId, bool isPriority)
    {
        try
        {
            var response = await _httpClient.PatchAsJsonAsync(
                $"api/admin/vetting/applications/{applicationId}/priority",
                new { isPriority });
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating vetting priority for ID: {ApplicationId}", applicationId);
            throw;
        }
    }

    public async Task ApproveVettingApplicationAsync(int applicationId, string notes)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"api/admin/vetting/applications/{applicationId}/approve",
                new { notes });
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving vetting application ID: {ApplicationId}", applicationId);
            throw;
        }
    }

    public async Task RejectVettingApplicationAsync(int applicationId, string notes)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"api/admin/vetting/applications/{applicationId}/reject",
                new { notes });
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting vetting application ID: {ApplicationId}", applicationId);
            throw;
        }
    }

    #endregion
}

// View Models
public class EventViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int AvailableSpots { get; set; }
    public string? ImageUrl { get; set; }
}

public class EventDetailViewModel : EventViewModel
{
    public List<string>? Prerequisites { get; set; }
    public List<string>? WhatToBring { get; set; }
    public InstructorInfo? Instructor { get; set; }
    public string? EventType { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? ParkingInfo { get; set; }
    public List<string>? WhatToExpect { get; set; }
    public List<Guid> Organizers { get; set; } = new();
    public bool IsRegistered { get; set; }
}

public class InstructorInfo
{
    public string Name { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}

public class RegistrationResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}

public class MemberStats
{
    public int UpcomingEvents { get; set; }
    public int PastEvents { get; set; }
    public DateTime MemberSince { get; set; } = DateTime.Now;
    public string VettingStatus { get; set; } = "Pending";
}

public class UpcomingEventViewModel
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public string Location { get; set; } = string.Empty;
}

public class AnnouncementViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Type { get; set; } = "info";
}

public class TicketViewModel
{
    public int Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string EventTitle { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string Status { get; set; } = "Confirmed";
}

public class TicketDetailViewModel : TicketViewModel
{
    public DateTime EventEndDate { get; set; }
    public DateTime PurchaseDate { get; set; }
    public decimal AmountPaid { get; set; }
    public string? SpecialInstructions { get; set; }
}

public class EventManagementViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int RegisteredCount { get; set; }
    public decimal Price { get; set; }
    public decimal TotalRevenue { get; set; }
    public string? Description { get; set; }
}

public class EventFormModel
{
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = "draft";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public decimal Price { get; set; }
}

public class VettingStatsViewModel
{
    public int PendingCount { get; set; }
    public int InProgressCount { get; set; }
    public int CompletedToday { get; set; }
    public double AverageTime { get; set; }
}

public class VettingApplicationViewModel
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime SubmittedDate { get; set; }
    public string Status { get; set; } = "pending";
    public bool Priority { get; set; }
    public string ExperienceLevel { get; set; } = string.Empty;
    public int PreviousEvents { get; set; }
    public int ReferenceCount { get; set; }
    public string? AssignedTo { get; set; }
}

public class VettingApplicationDetailViewModel : VettingApplicationViewModel
{
    public string Phone { get; set; } = string.Empty;
    public string EmergencyContact { get; set; } = string.Empty;
    public string ExperienceDescription { get; set; } = string.Empty;
    public List<ReferenceViewModel> References { get; set; } = new();
    public string? ReviewNotes { get; set; }
}

public class ReferenceViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";
}
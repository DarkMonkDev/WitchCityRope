using System.Net.Http.Json;
using System.Text.Json;
using WitchCityRope.Core.DTOs;
using WitchCityRope.Core.Enums;

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

    public async Task<TResponse?> GetAsync<TResponse>(string endpoint)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/v1/{endpoint}");
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("API call failed: {StatusCode} - {Error}", response.StatusCode, error);
                throw new HttpRequestException($"API call failed: {response.StatusCode}");
            }
            
            var result = await response.Content.ReadFromJsonAsync<TResponse>();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling API endpoint: {Endpoint}", endpoint);
            throw;
        }
    }
    
    public async Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/v1/{endpoint}", request);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("API call failed: {StatusCode} - {Error}", response.StatusCode, error);
                throw new HttpRequestException($"API call failed: {response.StatusCode}");
            }
            
            var result = await response.Content.ReadFromJsonAsync<TResponse>();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling API endpoint: {Endpoint}", endpoint);
            throw;
        }
    }
    
    public async Task<TResponse?> DeleteAsync<TResponse>(string endpoint)
    {
        try
        {
            var response = await _httpClient.DeleteAsync(endpoint);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("API call failed: {StatusCode} - {Error}", response.StatusCode, error);
                throw new HttpRequestException($"API call failed: {response.StatusCode}");
            }
            
            if (response.Content.Headers.ContentLength == 0)
            {
                return default;
            }
            
            var result = await response.Content.ReadFromJsonAsync<TResponse>();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling API endpoint: {Endpoint}", endpoint);
            throw;
        }
    }
    
    #endregion

    #region Admin Methods (No v1 prefix)
    
    public async Task<TResponse?> GetAdminAsync<TResponse>(string endpoint)
    {
        try
        {
            var response = await _httpClient.GetAsync(endpoint);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Admin API call failed: {StatusCode} - {Error}", response.StatusCode, error);
                throw new HttpRequestException($"API call failed: {response.StatusCode}");
            }
            
            var result = await response.Content.ReadFromJsonAsync<TResponse>();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling admin API endpoint: {Endpoint}", endpoint);
            throw;
        }
    }
    
    public async Task<TResponse> PostAdminAsync<TRequest, TResponse>(string endpoint, TRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(endpoint, request);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Admin API call failed: {StatusCode} - {Error}", response.StatusCode, error);
                throw new HttpRequestException($"API call failed: {response.StatusCode}");
            }
            
            var result = await response.Content.ReadFromJsonAsync<TResponse>();
            return result ?? throw new InvalidOperationException("Invalid response from API");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling admin API endpoint: {Endpoint}", endpoint);
            throw;
        }
    }
    
    public async Task<TResponse?> PutAdminAsync<TRequest, TResponse>(string endpoint, TRequest request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync(endpoint, request);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Admin API call failed: {StatusCode} - {Error}", response.StatusCode, error);
                throw new HttpRequestException($"API call failed: {response.StatusCode}");
            }
            
            var result = await response.Content.ReadFromJsonAsync<TResponse>();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling admin API endpoint: {Endpoint}", endpoint);
            throw;
        }
    }
    
    public async Task PostAdminAsync<TRequest>(string endpoint, TRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(endpoint, request);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Admin API call failed: {StatusCode} - {Error}", response.StatusCode, error);
                throw new HttpRequestException($"API call failed: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling admin API endpoint: {Endpoint}", endpoint);
            throw;
        }
    }
    
    public async Task PutAdminAsync<TRequest>(string endpoint, TRequest request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync(endpoint, request);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Admin API call failed: {StatusCode} - {Error}", response.StatusCode, error);
                throw new HttpRequestException($"API call failed: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling admin API endpoint: {Endpoint}", endpoint);
            throw;
        }
    }
    
    #endregion

    #region Events

    public async Task<List<EventViewModel>> GetEventsAsync(int page = 1, int pageSize = 12)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ListEventsResponse>(
                $"api/events?page={page}&pageSize={pageSize}");
            
            // Map EventSummaryDto to EventViewModel
            return response?.Events.Select(e => new EventViewModel
            {
                Id = e.Id,
                Title = e.Title,
                Description = e.Description,
                StartDateTime = e.StartDateTime,
                EndDateTime = e.EndDateTime,
                Type = (int)e.Type,
                Location = e.Location,
                Price = e.Price,
                AvailableSpots = e.AvailableSpots
            }).ToList() ?? new List<EventViewModel>();
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
    
    public async Task<RegistrationResult> RegisterForEventAsync(Guid eventId)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/v1/events/{eventId}/register", new { });
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

    #region RSVPs

    public async Task<List<RsvpDto>> GetEventRsvpsAsync(Guid eventId)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<RsvpDto>>(
                $"api/v1/events/{eventId}/rsvps");
            return response ?? new List<RsvpDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting event RSVPs: {EventId}", eventId);
            throw;
        }
    }

    public async Task<bool> UpdateRsvpAsync(Guid rsvpId, UpdateRsvpRequest request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/v1/rsvps/{rsvpId}", request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating RSVP: {RsvpId}", rsvpId);
            throw;
        }
    }

    public async Task<bool> CancelRsvpAsync(Guid rsvpId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/v1/rsvps/{rsvpId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling RSVP: {RsvpId}", rsvpId);
            throw;
        }
    }

    #endregion

    #region Event Management
    
    public async Task<EventDto?> GetEventByIdAsync(Guid eventId)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<EventDto>(
                $"api/v1/events/{eventId}");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching event by ID: {EventId}", eventId);
            return null;
        }
    }
    
    public async Task<EventAttendeesResponse> GetEventAttendeesAsync(Guid eventId)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<EventAttendeesResponse>(
                $"api/v1/events/{eventId}/attendees");
            return response ?? new EventAttendeesResponse();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching event attendees: {EventId}", eventId);
            return new EventAttendeesResponse();
        }
    }
    
    public async Task<bool> CheckInRsvpAsync(Guid rsvpId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"api/v1/rsvps/{rsvpId}/checkin", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking in RSVP: {RsvpId}", rsvpId);
            return false;
        }
    }
    
    #endregion

    #region Check-In

    public async Task<CheckInResponse> CheckInAttendeeAsync(Guid eventId, CheckInRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/v1/events/{eventId}/checkin", request);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Check-in failed: {StatusCode} - {Error}", response.StatusCode, error);
                throw new HttpRequestException($"Check-in failed: {response.StatusCode}");
            }
            
            var result = await response.Content.ReadFromJsonAsync<CheckInResponse>();
            return result ?? throw new InvalidOperationException("Invalid response from API");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking in attendee for event: {EventId}", eventId);
            throw;
        }
    }

    #endregion
}

// View Models
public class EventsResponse
{
    public List<EventViewModel> Events { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class ListEventsResponse
{
    public List<EventSummaryDto> Events { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class EventSummaryDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public EventType Type { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public string Location { get; set; } = string.Empty;
    public int MaxAttendees { get; set; }
    public int CurrentAttendees { get; set; }
    public int AvailableSpots { get; set; }
    public decimal Price { get; set; }
    public List<string> Tags { get; set; } = new();
    public List<string> RequiredSkillLevels { get; set; } = new();
    public bool RequiresVetting { get; set; }
    public string OrganizerName { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
}

public enum EventType
{
    Workshop = 0,
    Social = 1,
    Performance = 2,
    Virtual = 3,
    Conference = 4,
    Private = 5,
    PlayParty = 6
}

public class EventViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDateTime { get; set; } // Changed to match API response
    public DateTime EndDateTime { get; set; } // Changed to match API response
    public int Type { get; set; } // Changed from string to int to match API response
    public string Location { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int AvailableSpots { get; set; }
    public string? ImageUrl { get; set; }
    
    // Add computed properties for compatibility
    public DateTime StartDate => StartDateTime;
    public DateTime EndDate => EndDateTime;
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
    public string? ConfirmationCode { get; set; }
    public string? Status { get; set; }
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

public class EventAttendeesResponse
{
    public List<RsvpDto> Rsvps { get; set; } = new();
    public List<TicketDto> Tickets { get; set; } = new();
    public int TotalCount { get; set; }
}
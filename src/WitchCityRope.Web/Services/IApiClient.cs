using WitchCityRope.Core.DTOs;
using WitchCityRope.Web.Models;

namespace WitchCityRope.Web.Services;

/// <summary>
/// Interface for HTTP client service for making API calls to the backend
/// </summary>
public interface IApiClient
{
    #region Generic Methods
    
    Task<TResponse> GetAsync<TResponse>(string endpoint);
    Task<TResponse> PostAsync<TRequest, TResponse>(string endpoint, TRequest request);
    Task<TResponse> PutAsync<TRequest, TResponse>(string endpoint, TRequest request);
    Task<TResponse> PatchAsync<TRequest, TResponse>(string endpoint, TRequest request);
    Task DeleteAsync(string endpoint);
    
    #endregion

    #region Events

    Task<List<EventViewModel>> GetEventsAsync(int page = 1, int pageSize = 12);
    Task<EventDetailViewModel> GetEventDetailsAsync(int eventId);
    Task<Core.DTOs.EventDto> GetEventByIdAsync(Guid eventId);
    Task<List<EventViewModel>> GetRecommendedEventsAsync();
    Task<CreateEventResponse> CreateEventAsync(CreateEventRequest request);
    Task UpdateEventAsync(Guid eventId, UpdateEventRequest request);
    Task<List<EventAttendeeDto>> GetEventAttendeesAsync(Guid eventId);

    #endregion

    #region Member Dashboard

    Task<MemberStats> GetMemberStatsAsync();
    Task<List<UpcomingEventViewModel>> GetUpcomingEventsAsync();
    Task<List<AnnouncementViewModel>> GetAnnouncementsAsync();

    #endregion

    #region Tickets

    Task<List<TicketViewModel>> GetMyTicketsAsync();
    Task<TicketDetailViewModel?> GetTicketDetailAsync(int ticketId);

    #endregion

    #region User Management
    
    Task<List<UserDto>> GetUsersAsync(string? roles = null);
    
    #endregion

    #region Admin - Event Management

    Task<List<EventManagementViewModel>> GetManagedEventsAsync();
    Task<EventFormModel> CreateEventAsync(EventFormModel model);
    Task UpdateEventAsync(int eventId, EventFormModel model);
    Task<EventManagementViewModel> GetEventByIdAsync(int eventId);

    #endregion

    #region Admin - Vetting

    Task<VettingStatsViewModel> GetVettingStatsAsync();
    Task<List<VettingApplicationViewModel>> GetVettingApplicationsAsync(int page = 1, int pageSize = 20);
    Task<VettingApplicationDetailViewModel?> GetVettingApplicationDetailAsync(int applicationId);
    Task UpdateVettingStatusAsync(int applicationId, string status);
    Task UpdateVettingPriorityAsync(int applicationId, bool isPriority);
    Task ApproveVettingApplicationAsync(int applicationId, string notes);
    Task RejectVettingApplicationAsync(int applicationId, string notes);

    #endregion

    #region Members - Event Registration

    Task<EventRegistrationResponse> RegisterForEventAsync(RegisterForEventRequest request);

    #endregion

    #region Volunteer Management

    Task<List<VolunteerTaskDto>> GetEventVolunteerTasksAsync(Guid eventId);
    Task<VolunteerTaskDto> GetVolunteerTaskAsync(Guid taskId);
    Task<VolunteerTaskDto> CreateVolunteerTaskAsync(Guid eventId, CreateVolunteerTaskRequest request);
    Task<VolunteerTaskDto> UpdateVolunteerTaskAsync(Guid taskId, UpdateVolunteerTaskRequest request);
    Task DeleteVolunteerTaskAsync(Guid taskId);
    Task<bool> AssignVolunteerToTaskAsync(Guid taskId, Guid userId);
    Task<bool> UnassignVolunteerFromTaskAsync(Guid taskId, Guid userId);
    Task<bool> MarkVolunteerTicketUsedAsync(Guid assignmentId);

    #endregion

    #region Event Email Management

    Task<List<EventEmailTemplateDto>> GetEventEmailTemplatesAsync(Guid eventId);
    Task<EventEmailTemplateDto> GetEmailTemplateAsync(Guid eventId, Guid templateId);
    Task<EventEmailTemplateDto> CreateEmailTemplateAsync(Guid eventId, SaveEmailTemplateRequest request);
    Task<EventEmailTemplateDto> UpdateEmailTemplateAsync(Guid eventId, Guid templateId, SaveEmailTemplateRequest request);
    Task DeleteEmailTemplateAsync(Guid eventId, Guid templateId);
    Task<bool> SendEventEmailAsync(Guid eventId, SendEventEmailRequest request);
    Task<bool> SendEmailFromTemplateAsync(Guid eventId, Guid templateId);
    Task<string> PreviewEmailTemplateAsync(Guid eventId, Guid templateId, Guid? sampleUserId = null);
    Task<bool> SendTestEmailAsync(Guid eventId, Guid templateId, string testEmail);
    Task<List<EventEmailTemplateDto>> GetSystemEmailTemplatesAsync();

    #endregion

    #region RSVP Management

    Task<List<RsvpDto>> GetEventRsvpsAsync(Guid eventId);
    Task<RsvpResponse> CreateRsvpAsync(Guid eventId, Core.DTOs.RsvpRequest request);
    Task<RsvpResponse> UpdateRsvpAsync(Guid eventId, Guid rsvpId, Core.DTOs.RsvpUpdateRequest request);
    Task CancelRsvpAsync(Guid eventId, Guid rsvpId);
    Task<RsvpDto> CheckInRsvpAsync(Guid eventId, Guid rsvpId);
    Task<CheckInResponse> CheckInAttendeeAsync(Guid eventId, CheckInRequest request);

    #endregion
}
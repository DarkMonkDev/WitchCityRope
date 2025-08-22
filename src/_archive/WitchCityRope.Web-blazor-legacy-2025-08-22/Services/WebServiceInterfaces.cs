using WitchCityRope.Web.Models;

namespace WitchCityRope.Web.Services;

// Service interfaces for Web project
public interface IDashboardService
{
    Task<DashboardViewModel> GetDashboardDataAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<Models.EventViewModel>> GetUpcomingEventsAsync(Guid userId, int count = 3, CancellationToken cancellationToken = default);
    Task<MembershipStatsViewModel> GetMembershipStatsAsync(Guid userId, CancellationToken cancellationToken = default);
}

public interface IEventService
{
    Task<List<EventListItem>> GetUpcomingEventsAsync();
    Task<EventDetail?> GetEventDetailAsync(Guid eventId);
    Task<bool> RegisterForEventAsync(Guid eventId);
}

public interface IUserService
{
    Task<UserProfile?> GetCurrentUserProfileAsync();
    Task UpdateProfileAsync(UserProfileUpdate update);
}

public interface IRegistrationService
{
    Task<List<UserRegistration>> GetMyRegistrationsAsync();
    Task<bool> CancelRegistrationAsync(Guid registrationId);
}

public interface IVettingService
{
    Task<VettingStatus?> GetMyVettingStatusAsync();
    Task<bool> SubmitVettingApplicationAsync(VettingApplicationSubmit application);
}

public interface IPaymentService
{
    Task<PaymentIntent> CreatePaymentIntentAsync(decimal amount, string description);
    Task<bool> ProcessPaymentAsync(string paymentIntentId);
}

public interface ISafetyService
{
    Task<bool> SubmitIncidentReportAsync(IncidentReportSubmit report);
    Task<List<SafetyResource>> GetSafetyResourcesAsync();
}

public interface INotificationService
{
    Task<List<UserNotification>> GetNotificationsAsync();
    Task MarkNotificationAsReadAsync(Guid notificationId);
}

public interface IToastService
{
    IReadOnlyList<ToastMessage> Messages { get; }
    event Action? OnChange;
    
    void ShowSuccess(string message);
    void ShowError(string message);
    void ShowWarning(string message);
    void ShowInfo(string message);
    void Remove(Guid id);
}

public interface INavigationService
{
    void NavigateTo(string url);
    void NavigateToLogin(string? returnUrl = null);
    void NavigateBack();
}

public interface IFileUploadService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName);
    Task<bool> DeleteFileAsync(string fileUrl);
}

// DTOs for Web services
public class EventListItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartDateTime { get; set; }
    public string Location { get; set; } = string.Empty;
    public int AvailableSpots { get; set; }
    public decimal Price { get; set; }
    
    // Computed property for compatibility
    public DateTime StartDate => StartDateTime;
}

public class EventDetail : EventListItem
{
    public string Description { get; set; } = string.Empty;
    public DateTime EndDateTime { get; set; }
    public List<string> Organizers { get; set; } = new();
    public bool IsRegistered { get; set; }
    
    // Computed property for compatibility
    public DateTime EndDate => EndDateTime;
}

public class UserProfile
{
    public string SceneName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? Pronouns { get; set; }
    public bool EmailVerified { get; set; }
    public DateTime MemberSince { get; set; }
}

public class UserProfileUpdate
{
    public string? SceneName { get; set; }
    public string? Bio { get; set; }
    public string? Pronouns { get; set; }
}

public class UserRegistration
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class VettingStatus
{
    public string Status { get; set; } = string.Empty;
    public DateTime? SubmittedAt { get; set; }
    public string? Notes { get; set; }
}

public class VettingApplicationSubmit
{
    public string LegalName { get; set; } = string.Empty;
    public string Experience { get; set; } = string.Empty;
    public string WhyJoin { get; set; } = string.Empty;
    public List<string> References { get; set; } = new();
}

public class PaymentIntent
{
    public string ClientSecret { get; set; } = string.Empty;
    public string PaymentIntentId { get; set; } = string.Empty;
}

public class IncidentReportSubmit
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime IncidentDate { get; set; }
    public bool IsAnonymous { get; set; }
}

public class SafetyResource
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

public class UserNotification
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
}
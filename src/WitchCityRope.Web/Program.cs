using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.ResponseCompression;
using WitchCityRope.Web.Services;
using WitchCityRope.Web.Models;
using Syncfusion.Blazor;
using SendGrid.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// Register Syncfusion license
var syncfusionLicense = builder.Configuration["Syncfusion:LicenseKey"];
if (!string.IsNullOrEmpty(syncfusionLicense))
{
    Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(syncfusionLicense);
}

// Add services to the container
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor(options =>
{
    // Configure Blazor Server circuit options for optimal performance
    options.DetailedErrors = builder.Environment.IsDevelopment();
    options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(3);
    options.DisconnectedCircuitMaxRetained = 100;
    options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(1);
    options.MaxBufferedUnacknowledgedRenderBatches = 10;
});

// Add Syncfusion Blazor service
builder.Services.AddSyncfusionBlazor();

// Add authentication services for Blazor Server
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
    options.DefaultChallengeScheme = "Cookies";
})
.AddCookie("Cookies", options =>
{
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
    options.AccessDeniedPath = "/access-denied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
});

// Add HttpContextAccessor for authentication service
builder.Services.AddHttpContextAccessor();

// Configure caching
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache(); // For session state

// Configure HttpClient for API calls
builder.Services.AddHttpClient<ApiClient>(client =>
{
    var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:5654/";
    client.BaseAddress = new Uri(apiBaseUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Add HttpClient for external services
builder.Services.AddHttpClient();

// Configure SendGrid for email
var sendGridApiKey = builder.Configuration["Email:SendGrid:ApiKey"];
if (!string.IsNullOrEmpty(sendGridApiKey))
{
    builder.Services.AddSendGrid(options =>
    {
        options.ApiKey = sendGridApiKey;
    });
}

// Add Authentication services
builder.Services.AddScoped<AuthenticationService>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider => 
    provider.GetRequiredService<AuthenticationService>());
builder.Services.AddScoped<ILocalStorageService, LocalStorageService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Add direct service implementations
builder.Services.AddScoped<WitchCityRope.Web.Services.IEventService, WitchCityRope.Web.Services.EventService>();
builder.Services.AddScoped<WitchCityRope.Web.Services.IUserService, WitchCityRope.Web.Services.UserService>();
builder.Services.AddScoped<WitchCityRope.Web.Services.IRegistrationService, WitchCityRope.Web.Services.RegistrationService>();
builder.Services.AddScoped<WitchCityRope.Web.Services.IVettingService, WitchCityRope.Web.Services.VettingService>();
builder.Services.AddScoped<WitchCityRope.Web.Services.IPaymentService, WitchCityRope.Web.Services.PaymentService>();
builder.Services.AddScoped<WitchCityRope.Web.Services.ISafetyService, WitchCityRope.Web.Services.SafetyService>();
builder.Services.AddScoped<WitchCityRope.Web.Services.INotificationService, WitchCityRope.Web.Services.NotificationService>();

// Add utility services
builder.Services.AddSingleton<WitchCityRope.Web.Services.IToastService, WitchCityRope.Web.Services.ToastService>(); // Singleton for state management
builder.Services.AddScoped<WitchCityRope.Web.Services.INavigationService, WitchCityRope.Web.Services.NavigationService>();
builder.Services.AddScoped<WitchCityRope.Web.Services.IFileUploadService, WitchCityRope.Web.Services.FileUploadService>();

// Add authorization
builder.Services.AddAuthorizationCore(options =>
{
    // Define authorization policies
    options.AddPolicy("RequireAuthenticated", policy => policy.RequireAuthenticatedUser());
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireEventOrganizer", policy => policy.RequireRole("Admin", "EventOrganizer"));
    options.AddPolicy("RequireVettingTeam", policy => policy.RequireRole("Admin", "VettingTeam"));
    options.AddPolicy("RequireSafetyTeam", policy => policy.RequireRole("Admin", "SafetyTeam"));
    options.AddPolicy("RequireVettedMember", policy => 
        policy.RequireRole("Member").RequireClaim("IsVetted", "true"));
});

// Configure logging
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add session support for server-side Blazor
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
});

// Add data protection for secure storage
builder.Services.AddDataProtection();

// Configure circuit handler for monitoring and logging
builder.Services.AddScoped<CircuitHandler, CircuitHandlerService>();

// Configure server-side Blazor performance monitoring
builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 30 * 1024 * 1024; // 30MB for file uploads
});

// Configure SignalR for real-time updates with performance optimizations
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.MaximumReceiveMessageSize = 32 * 1024; // 32KB
    
    // Performance optimizations
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    
    // Configure message buffer
    options.StreamBufferCapacity = 10;
})
.AddJsonProtocol(options =>
{
    // Use more compact JSON serialization
    options.PayloadSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.PayloadSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});

// Add response compression with Brotli and Gzip providers
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
});

// Configure Brotli compression
builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Optimal;
});

// Configure Gzip compression
builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Optimal;
});

// Add health checks
builder.Services.AddHealthChecks();

// Add response caching
builder.Services.AddResponseCaching();

var app = builder.Build();

// Initialize database with seed data
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<WitchCityRope.Infrastructure.Data.WitchCityRopeDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        await WitchCityRope.Infrastructure.Data.DbInitializer.InitializeAsync(context, logger);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Use response compression early in the pipeline
app.UseResponseCompression();

// Use response caching
app.UseResponseCaching();

// Configure static files with caching headers
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Set cache control header for static assets
        // Cache for 1 year (365 days)
        const int durationInSeconds = 365 * 24 * 60 * 60;
        ctx.Context.Response.Headers[HeaderNames.CacheControl] = 
            $"public,max-age={durationInSeconds}";
    }
});

app.UseRouting();

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

// Map endpoints
app.MapBlazorHub(options =>
{
    // Configure hub-specific options for performance
    options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets | 
                        Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;
    
    // Configure buffer sizes
    options.ApplicationMaxBufferSize = 64 * 1024; // 64KB
    options.TransportMaxBufferSize = 64 * 1024; // 64KB
    
    // Set WebSocket options
    options.WebSockets.CloseTimeout = TimeSpan.FromSeconds(5);
    
    // Long polling options as fallback
    options.LongPolling.PollTimeout = TimeSpan.FromSeconds(90);
});
app.MapFallbackToPage("/_Host");
app.MapRazorPages();

// Map health check endpoint
app.MapHealthChecks("/health");

app.Run();

// Service Interfaces (Program.cs versions for API compatibility)
public interface IEventService
{
    Task<PagedResult<EventDto>> GetEventsAsync(int page, int pageSize, string? search = null);
    Task<EventDetailsDto?> GetEventByIdAsync(Guid id);
    Task<EventDto> CreateEventAsync(CreateEventRequest request);
    Task UpdateEventAsync(Guid id, UpdateEventRequest request);
    Task DeleteEventAsync(Guid id);
}

public interface IUserService
{
    Task<UserProfileDto?> GetCurrentUserAsync();
    Task<UserProfileDto?> GetUserByIdAsync(Guid id);
    Task UpdateProfileAsync(UpdateProfileRequest request);
    Task ChangePasswordAsync(ChangePasswordRequest request);
    Task<PagedResult<UserDto>> GetUsersAsync(int page, int pageSize, string? search = null);
}

public interface IRegistrationService
{
    Task<RegistrationDto> RegisterForEventAsync(Guid eventId, RegistrationRequest request);
    Task<PagedResult<RegistrationDto>> GetMyRegistrationsAsync(int page, int pageSize);
    Task<PagedResult<RegistrationDto>> GetEventRegistrationsAsync(Guid eventId, int page, int pageSize);
    Task CancelRegistrationAsync(Guid registrationId);
}

public interface IVettingService
{
    Task<VettingApplicationDto> SubmitApplicationAsync(VettingApplicationRequest request);
    Task<PagedResult<VettingApplicationDto>> GetApplicationsAsync(int page, int pageSize, string? status = null);
    Task<VettingApplicationDto?> GetApplicationByIdAsync(Guid id);
    Task ReviewApplicationAsync(Guid id, ReviewApplicationRequest request);
}

public interface IPaymentService
{
    Task<PaymentIntentDto> CreatePaymentIntentAsync(CreatePaymentIntentRequest request);
    Task<PaymentDto> ProcessPaymentAsync(ProcessPaymentRequest request);
    Task<PagedResult<PaymentDto>> GetPaymentHistoryAsync(int page, int pageSize);
    Task<PaymentDto?> GetPaymentByIdAsync(Guid id);
}

public interface ISafetyService
{
    Task<IncidentReportDto> SubmitReportAsync(IncidentReportRequest request);
    Task<PagedResult<IncidentReportDto>> GetReportsAsync(int page, int pageSize, string? status = null);
    Task<IncidentReportDto?> GetReportByIdAsync(Guid id);
    Task UpdateReportStatusAsync(Guid id, UpdateReportStatusRequest request);
}

public interface INotificationService
{
    Task<PagedResult<NotificationDto>> GetNotificationsAsync(int page, int pageSize);
    Task<int> GetUnreadCountAsync();
    Task MarkAsReadAsync(Guid id);
    Task MarkAllAsReadAsync();
}

// Utility Service Interfaces
public interface IToastService
{
    void ShowSuccess(string message);
    void ShowError(string message);
    void ShowWarning(string message);
    void ShowInfo(string message);
}

public interface INavigationService
{
    void NavigateTo(string uri, bool forceLoad = false);
    void NavigateToLogin(string? returnUrl = null);
    string GetReturnUrl();
}

public interface IFileUploadService
{
    Task<UploadResult> UploadFileAsync(Stream fileStream, string fileName, string contentType);
    Task<bool> DeleteFileAsync(string fileUrl);
    bool ValidateFile(string fileName, long fileSize);
}

// DTOs and Request/Response Models
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public class EventDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Capacity { get; set; }
    public int RegisteredCount { get; set; }
}

public class EventDetailsDto : EventDto
{
    public string OrganizerName { get; set; } = string.Empty;
    public bool IsRegistered { get; set; }
    public List<string> Tags { get; set; } = new();
}

public class CreateEventRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Capacity { get; set; }
    public List<string> Tags { get; set; } = new();
}

public class UpdateEventRequest : CreateEventRequest { }


public class UpdateProfileRequest
{
    public string DisplayName { get; set; } = string.Empty;
    public string? Bio { get; set; }
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class RegistrationDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public DateTime RegisteredAt { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class RegistrationRequest
{
    public string? SpecialRequirements { get; set; }
}

public class VettingApplicationDto
{
    public Guid Id { get; set; }
    public string ApplicantName { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ReviewerNotes { get; set; }
}

public class VettingApplicationRequest
{
    public string Reference1Name { get; set; } = string.Empty;
    public string Reference1Contact { get; set; } = string.Empty;
    public string Reference2Name { get; set; } = string.Empty;
    public string Reference2Contact { get; set; } = string.Empty;
    public string Experience { get; set; } = string.Empty;
}

public class ReviewApplicationRequest
{
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public class PaymentIntentDto
{
    public string ClientSecret { get; set; } = string.Empty;
    public string PaymentIntentId { get; set; } = string.Empty;
}

public class CreatePaymentIntentRequest
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "usd";
    public string Description { get; set; } = string.Empty;
}

public class ProcessPaymentRequest
{
    public string PaymentIntentId { get; set; } = string.Empty;
}

public class PaymentDto
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class IncidentReportDto
{
    public Guid Id { get; set; }
    public DateTime ReportedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsAnonymous { get; set; }
}

public class IncidentReportRequest
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsAnonymous { get; set; }
    public DateTime? IncidentDate { get; set; }
}

public class UpdateReportStatusRequest
{
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public class NotificationDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
    public string Type { get; set; } = string.Empty;
}

public class UploadResult
{
    public bool Success { get; set; }
    public string? FileUrl { get; set; }
    public string? Error { get; set; }
}

// Implementation of LocalStorageService for server-side Blazor
public class LocalStorageService : ILocalStorageService
{
    private readonly ProtectedLocalStorage _protectedLocalStorage;
    private readonly ILogger<LocalStorageService> _logger;

    public LocalStorageService(ProtectedLocalStorage protectedLocalStorage, ILogger<LocalStorageService> logger)
    {
        _protectedLocalStorage = protectedLocalStorage;
        _logger = logger;
    }

    public async Task<T?> GetItemAsync<T>(string key)
    {
        try
        {
            var result = await _protectedLocalStorage.GetAsync<T>(key);
            return result.Success ? result.Value : default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving item from local storage with key: {Key}", key);
            return default;
        }
    }

    public async Task SetItemAsync<T>(string key, T value)
    {
        try
        {
            if (value != null)
            {
                await _protectedLocalStorage.SetAsync(key, value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting item in local storage with key: {Key}", key);
        }
    }

    public async Task RemoveItemAsync(string key)
    {
        try
        {
            await _protectedLocalStorage.DeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing item from local storage with key: {Key}", key);
        }
    }

    public async Task ClearAsync()
    {
        try
        {
            // Server-side Blazor doesn't have a direct way to clear all items
            // This would need to be implemented based on your specific needs
            _logger.LogWarning("ClearAsync called on server-side LocalStorageService - not implemented");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing local storage");
        }
    }
}

// Placeholder service implementations
public class EventService : IEventService
{
    public Task<PagedResult<EventDto>> GetEventsAsync(int page, int pageSize, string? search = null) => throw new NotImplementedException();
    public Task<EventDetailsDto?> GetEventByIdAsync(Guid id) => throw new NotImplementedException();
    public Task<EventDto> CreateEventAsync(CreateEventRequest request) => throw new NotImplementedException();
    public Task UpdateEventAsync(Guid id, UpdateEventRequest request) => throw new NotImplementedException();
    public Task DeleteEventAsync(Guid id) => throw new NotImplementedException();
}

public class UserService : IUserService
{
    public Task<UserProfileDto?> GetCurrentUserAsync() => throw new NotImplementedException();
    public Task<UserProfileDto?> GetUserByIdAsync(Guid id) => throw new NotImplementedException();
    public Task UpdateProfileAsync(UpdateProfileRequest request) => throw new NotImplementedException();
    public Task ChangePasswordAsync(ChangePasswordRequest request) => throw new NotImplementedException();
    public Task<PagedResult<UserDto>> GetUsersAsync(int page, int pageSize, string? search = null) => throw new NotImplementedException();
}

public class RegistrationService : IRegistrationService
{
    public Task<RegistrationDto> RegisterForEventAsync(Guid eventId, RegistrationRequest request) => throw new NotImplementedException();
    public Task<PagedResult<RegistrationDto>> GetMyRegistrationsAsync(int page, int pageSize) => throw new NotImplementedException();
    public Task<PagedResult<RegistrationDto>> GetEventRegistrationsAsync(Guid eventId, int page, int pageSize) => throw new NotImplementedException();
    public Task CancelRegistrationAsync(Guid registrationId) => throw new NotImplementedException();
}

public class VettingService : IVettingService
{
    public Task<VettingApplicationDto> SubmitApplicationAsync(VettingApplicationRequest request) => throw new NotImplementedException();
    public Task<PagedResult<VettingApplicationDto>> GetApplicationsAsync(int page, int pageSize, string? status = null) => throw new NotImplementedException();
    public Task<VettingApplicationDto?> GetApplicationByIdAsync(Guid id) => throw new NotImplementedException();
    public Task ReviewApplicationAsync(Guid id, ReviewApplicationRequest request) => throw new NotImplementedException();
}

public class PaymentService : IPaymentService
{
    public Task<PaymentIntentDto> CreatePaymentIntentAsync(CreatePaymentIntentRequest request) => throw new NotImplementedException();
    public Task<PaymentDto> ProcessPaymentAsync(ProcessPaymentRequest request) => throw new NotImplementedException();
    public Task<PagedResult<PaymentDto>> GetPaymentHistoryAsync(int page, int pageSize) => throw new NotImplementedException();
    public Task<PaymentDto?> GetPaymentByIdAsync(Guid id) => throw new NotImplementedException();
}

public class SafetyService : ISafetyService
{
    public Task<IncidentReportDto> SubmitReportAsync(IncidentReportRequest request) => throw new NotImplementedException();
    public Task<PagedResult<IncidentReportDto>> GetReportsAsync(int page, int pageSize, string? status = null) => throw new NotImplementedException();
    public Task<IncidentReportDto?> GetReportByIdAsync(Guid id) => throw new NotImplementedException();
    public Task UpdateReportStatusAsync(Guid id, UpdateReportStatusRequest request) => throw new NotImplementedException();
}

public class NotificationService : INotificationService
{
    public Task<PagedResult<NotificationDto>> GetNotificationsAsync(int page, int pageSize) => throw new NotImplementedException();
    public Task<int> GetUnreadCountAsync() => throw new NotImplementedException();
    public Task MarkAsReadAsync(Guid id) => throw new NotImplementedException();
    public Task MarkAllAsReadAsync() => throw new NotImplementedException();
}


public class NavigationService : INavigationService
{
    public void NavigateTo(string uri, bool forceLoad = false) { }
    public void NavigateToLogin(string? returnUrl = null) { }
    public string GetReturnUrl() => string.Empty;
}

public class FileUploadService : IFileUploadService
{
    public Task<UploadResult> UploadFileAsync(Stream fileStream, string fileName, string contentType) => throw new NotImplementedException();
    public Task<bool> DeleteFileAsync(string fileUrl) => throw new NotImplementedException();
    public bool ValidateFile(string fileName, long fileSize) => true;
}

// Circuit handler for monitoring Blazor Server connections
public class CircuitHandlerService : CircuitHandler
{
    private readonly ILogger<CircuitHandlerService> _logger;

    public CircuitHandlerService(ILogger<CircuitHandlerService> logger)
    {
        _logger = logger;
    }

    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Circuit opened: {CircuitId}", circuit.Id);
        return Task.CompletedTask;
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Circuit closed: {CircuitId}", circuit.Id);
        return Task.CompletedTask;
    }

    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _logger.LogWarning("Circuit connection down: {CircuitId}", circuit.Id);
        return Task.CompletedTask;
    }

    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Circuit connection restored: {CircuitId}", circuit.Id);
        return Task.CompletedTask;
    }
}
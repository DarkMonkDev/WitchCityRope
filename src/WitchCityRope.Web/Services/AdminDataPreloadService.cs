using Microsoft.Extensions.Caching.Memory;

namespace WitchCityRope.Web.Services;

/// <summary>
/// Service for preloading and caching admin data to improve page load performance
/// </summary>
public class AdminDataPreloadService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AdminDataPreloadService> _logger;
    private Timer? _timer;

    public AdminDataPreloadService(
        IServiceProvider serviceProvider,
        IMemoryCache cache,
        ILogger<AdminDataPreloadService> logger)
    {
        _serviceProvider = serviceProvider;
        _cache = cache;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Admin Data Preload Service is starting");

        // Preload data every 5 minutes
        _timer = new Timer(PreloadData, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Admin Data Preload Service is stopping");

        _timer?.Change(Timeout.Infinite, 0);
        _timer?.Dispose();

        return Task.CompletedTask;
    }

    private async void PreloadData(object? state)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var apiClient = scope.ServiceProvider.GetRequiredService<ApiClient>();

            // Preload commonly accessed admin data
            await PreloadDashboardMetrics(apiClient);
            await PreloadRecentActivity(apiClient);
            await PreloadSystemHealth(apiClient);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error preloading admin data");
        }
    }

    private async Task PreloadDashboardMetrics(ApiClient apiClient)
    {
        try
        {
            // Simulate loading metrics - replace with actual API calls
            var metrics = new DashboardMetrics
            {
                TotalRevenue = 15847.50m,
                RevenueGrowth = 12.5m,
                ActiveMembersCount = 287,
                NewMembersCount = 23,
                UpcomingEventsCount = 8,
                PendingVettingCount = 3
            };

            _cache.Set("admin_dashboard_metrics", metrics, TimeSpan.FromMinutes(5));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error preloading dashboard metrics");
        }
    }

    private async Task PreloadRecentActivity(ApiClient apiClient)
    {
        try
        {
            // Simulate loading activity - replace with actual API calls
            var activities = new List<ActivityItem>
            {
                new() { Type = "registration", Description = "Sarah K. registered for Advanced Rope Techniques", Timestamp = DateTime.Now.AddMinutes(-15) },
                new() { Type = "payment", Description = "Payment received: $45.00 from John D.", Timestamp = DateTime.Now.AddHours(-1) },
                new() { Type = "event", Description = "New event created: Introduction to Suspension", Timestamp = DateTime.Now.AddHours(-2) }
            };

            _cache.Set("admin_recent_activity", activities, TimeSpan.FromMinutes(2));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error preloading recent activity");
        }
    }

    private async Task PreloadSystemHealth(ApiClient apiClient)
    {
        try
        {
            // Simulate loading system health - replace with actual API calls
            var health = new SystemHealthStatus
            {
                ApiStatus = true,
                DatabaseStatus = true,
                PaymentStatus = true,
                EmailStatus = true,
                LastChecked = DateTime.Now
            };

            _cache.Set("admin_system_health", health, TimeSpan.FromMinutes(1));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error preloading system health");
        }
    }
}

// Cache models
public class DashboardMetrics
{
    public decimal TotalRevenue { get; set; }
    public decimal RevenueGrowth { get; set; }
    public int ActiveMembersCount { get; set; }
    public int NewMembersCount { get; set; }
    public int UpcomingEventsCount { get; set; }
    public int PendingVettingCount { get; set; }
}

public class ActivityItem
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class SystemHealthStatus
{
    public bool ApiStatus { get; set; }
    public bool DatabaseStatus { get; set; }
    public bool PaymentStatus { get; set; }
    public bool EmailStatus { get; set; }
    public DateTime LastChecked { get; set; }
}

/// <summary>
/// Extension methods for adding the preload service
/// </summary>
public static class AdminDataPreloadServiceExtensions
{
    public static IServiceCollection AddAdminDataPreloadService(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddHostedService<AdminDataPreloadService>();
        return services;
    }
}
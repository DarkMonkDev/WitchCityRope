using Microsoft.AspNetCore.Components.Server.Circuits;

namespace WitchCityRope.Web.Services;

/// <summary>
/// Service for managing API URLs based on the execution context (server-side vs client-side)
/// </summary>
public class ApiUrlService : IApiUrlService
{
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ApiUrlService> _logger;
    private readonly bool _isRunningInContainer;

    public ApiUrlService(
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ApiUrlService> logger)
    {
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        
        // Check if we're running in a Docker container
        _isRunningInContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
        _logger.LogInformation("ApiUrlService initialized. Running in container: {IsContainer}", _isRunningInContainer);
    }

    public string GetServerSideApiUrl()
    {
        // Check for InternalApiUrl first (used in Docker environments)
        var internalApiUrl = _configuration["InternalApiUrl"];
        
        // If we have an internal URL configured, use it (this is the Docker scenario)
        if (!string.IsNullOrEmpty(internalApiUrl))
        {
            _logger.LogInformation("Using internal API URL for server-side call: {Url} (Container: {IsContainer})", 
                internalApiUrl, _isRunningInContainer);
            return internalApiUrl;
        }

        // Fall back to regular ApiUrl
        var apiUrl = _configuration["ApiUrl"] ?? "http://localhost:8180";
        _logger.LogInformation("Using external API URL for server-side call: {Url} (Container: {IsContainer})", 
            apiUrl, _isRunningInContainer);
        return apiUrl;
    }

    public string GetClientSideApiUrl()
    {
        var apiUrl = _configuration["ApiUrl"] ?? "http://localhost:8180";
        _logger.LogDebug("Using client-side API URL: {Url}", apiUrl);
        return apiUrl;
    }

    public bool IsServerSideRequest()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        
        // If there's no HTTP context, we're likely in a background service or server-side operation
        if (httpContext == null)
        {
            _logger.LogDebug("No HTTP context available, treating as server-side request");
            return true;
        }

        // Check if this is a Blazor SignalR request
        var path = httpContext.Request.Path.Value?.ToLower() ?? "";
        if (path.Contains("/_blazor"))
        {
            _logger.LogDebug("Blazor SignalR request detected, treating as client-side");
            return false;
        }

        // Check if we're in prerendering phase
        if (!httpContext.Response.HasStarted)
        {
            _logger.LogDebug("Response has not started, likely in prerendering phase, treating as server-side");
            return true;
        }

        // Default to server-side for safety
        return true;
    }
}
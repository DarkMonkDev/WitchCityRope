namespace WitchCityRope.Web.Services;

/// <summary>
/// Service for managing API URLs based on the execution context (server-side vs client-side)
/// </summary>
public interface IApiUrlService
{
    /// <summary>
    /// Gets the API URL for server-side calls (internal Docker network)
    /// </summary>
    string GetServerSideApiUrl();
    
    /// <summary>
    /// Gets the API URL for client-side browser calls (external URL)
    /// </summary>
    string GetClientSideApiUrl();
    
    /// <summary>
    /// Determines if the current request is server-side or client-side
    /// </summary>
    bool IsServerSideRequest();
}
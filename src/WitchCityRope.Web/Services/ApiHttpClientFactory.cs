namespace WitchCityRope.Web.Services;

/// <summary>
/// Factory for creating HttpClient instances with the appropriate API URL
/// </summary>
public class ApiHttpClientFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IApiUrlService _apiUrlService;
    private readonly ILogger<ApiHttpClientFactory> _logger;

    public ApiHttpClientFactory(
        IHttpClientFactory httpClientFactory,
        IApiUrlService apiUrlService,
        ILogger<ApiHttpClientFactory> logger)
    {
        _httpClientFactory = httpClientFactory;
        _apiUrlService = apiUrlService;
        _logger = logger;
    }

    public HttpClient CreateClient(string name = "")
    {
        // Always use the ServerSideApi client which includes authentication handling
        // Ignore the name parameter to ensure authentication is always included
        var clientName = "ServerSideApi";
        var client = _httpClientFactory.CreateClient(clientName);
        
        // The named client already has the base URL configured, but we'll ensure it's set
        if (client.BaseAddress == null)
        {
            var baseUrl = _apiUrlService.GetServerSideApiUrl();
            client.BaseAddress = new Uri(baseUrl);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.Timeout = TimeSpan.FromSeconds(30);
        }
        
        _logger.LogInformation("Creating HttpClient '{ClientName}' with base URL: {BaseUrl}", 
            clientName, client.BaseAddress);
        
        return client;
    }
}
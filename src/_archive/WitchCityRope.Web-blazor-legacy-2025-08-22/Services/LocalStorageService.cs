using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace WitchCityRope.Web.Services;

/// <summary>
/// Implementation of LocalStorageService for server-side Blazor
/// </summary>
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
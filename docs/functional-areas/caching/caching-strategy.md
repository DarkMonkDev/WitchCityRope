# Caching and Email Strategy

## Caching Implementation

### Built-in .NET Memory Caching

We'll use .NET's built-in `IMemoryCache` for simplicity and performance. This is perfect for a single-server deployment and requires no additional infrastructure.

### NuGet Package
```xml
<!-- Already included in ASP.NET Core -->
<PackageReference Include="Microsoft.Extensions.Caching.Memory" Version="9.0.*" />
```

### Configuration
```csharp
// Program.cs
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1024; // Limit cache items
    options.CompactionPercentage = 0.25; // Compact 25% when limit reached
});
```

### Cache Service Implementation
```csharp
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
    Task RemoveAsync(string key);
    Task RemoveByPrefixAsync(string prefix);
}

public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<MemoryCacheService> _logger;
    
    // Track keys for prefix-based removal
    private readonly HashSet<string> _cacheKeys = new();
    private readonly SemaphoreSlim _keyLock = new(1, 1);

    public MemoryCacheService(IMemoryCache cache, ILogger<MemoryCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            if (_cache.TryGetValue(key, out T? value))
            {
                _logger.LogDebug("Cache hit for key: {Key}", key);
                return value;
            }
            
            _logger.LogDebug("Cache miss for key: {Key}", key);
            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving from cache for key: {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        try
        {
            var options = new MemoryCacheEntryOptions
            {
                Size = 1, // Each entry counts as 1 towards size limit
                SlidingExpiration = expiration ?? TimeSpan.FromMinutes(5)
            };

            _cache.Set(key, value, options);
            
            // Track key for prefix removal
            await _keyLock.WaitAsync();
            try
            {
                _cacheKeys.Add(key);
            }
            finally
            {
                _keyLock.Release();
            }
            
            _logger.LogDebug("Cached value for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        _cache.Remove(key);
        
        await _keyLock.WaitAsync();
        try
        {
            _cacheKeys.Remove(key);
        }
        finally
        {
            _keyLock.Release();
        }
        
        _logger.LogDebug("Removed cache key: {Key}", key);
    }

    public async Task RemoveByPrefixAsync(string prefix)
    {
        await _keyLock.WaitAsync();
        try
        {
            var keysToRemove = _cacheKeys.Where(k => k.StartsWith(prefix)).ToList();
            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
                _cacheKeys.Remove(key);
            }
            
            _logger.LogDebug("Removed {Count} cache keys with prefix: {Prefix}", 
                keysToRemove.Count, prefix);
        }
        finally
        {
            _keyLock.Release();
        }
    }
}
```

### Cache Keys Strategy
```csharp
public static class CacheKeys
{
    public const string EventPrefix = "event:";
    public const string UserPrefix = "user:";
    public const string MemberListPrefix = "members:";
    
    public static string Event(int id) => $"{EventPrefix}{id}";
    public static string EventList(string filter) => $"{EventPrefix}list:{filter}";
    public static string User(int id) => $"{UserPrefix}{id}";
    public static string UserByEmail(string email) => $"{UserPrefix}email:{email}";
    public static string MembersList(int page) => $"{MemberListPrefix}page:{page}";
}
```

### Using Cache in Services
```csharp
public class EventService : IEventService
{
    private readonly WitchCityRopeDbContext _db;
    private readonly ICacheService _cache;
    
    public EventService(WitchCityRopeDbContext db, ICacheService cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<Result<EventDto>> GetEventAsync(int id)
    {
        // Try cache first
        var cacheKey = CacheKeys.Event(id);
        var cached = await _cache.GetAsync<EventDto>(cacheKey);
        if (cached != null)
            return Result<EventDto>.Success(cached);
        
        // Load from database
        var eventEntity = await _db.Events
            .Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.Id == id);
            
        if (eventEntity == null)
            return Result<EventDto>.Failure("Event not found");
            
        var dto = new EventDto(eventEntity);
        
        // Cache for 10 minutes
        await _cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(10));
        
        return Result<EventDto>.Success(dto);
    }
    
    public async Task<Result> UpdateEventAsync(int id, UpdateEventRequest request)
    {
        // Update database...
        
        // Invalidate cache
        await _cache.RemoveAsync(CacheKeys.Event(id));
        await _cache.RemoveByPrefixAsync(CacheKeys.EventPrefix + "list:");
        
        return Result.Success();
    }
}
```

## Updated Program.cs
```csharp
// Add caching
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICacheService, MemoryCacheService>();

// Add background task queue
builder.Services.AddHostedService<QueuedHostedService>();
builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
```

## Benefits
### Caching Benefits
- **Performance**: Reduces database queries
- **Scalability**: Can handle more concurrent users
- **Simple**: Uses built-in .NET caching
- **No Infrastructure**: No Redis or external cache needed

### Future Considerations
- **Redis**: If scaling to multiple servers
- **Distributed Cache**: For load-balanced environments

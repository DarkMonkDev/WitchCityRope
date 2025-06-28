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

## Email Service with SendGrid

### NuGet Package
```xml
<PackageReference Include="SendGrid" Version="9.28.*" />
```

### Configuration
```json
// appsettings.json
{
  "SendGrid": {
    "ApiKey": "YOUR_API_KEY",
    "FromEmail": "noreply@witchcityrope.com",
    "FromName": "Witch City Rope"
  }
}
```

### Email Service Implementation
```csharp
public interface IEmailService
{
    Task<Result> SendSingleEmailAsync(EmailMessage message);
    Task<Result> SendBulkEmailAsync(BulkEmailMessage message);
    Task<Result> SendTemplateEmailAsync(string templateId, EmailRecipient recipient, object data);
}

public class SendGridEmailService : IEmailService
{
    private readonly ISendGridClient _client;
    private readonly SendGridSettings _settings;
    private readonly ILogger<SendGridEmailService> _logger;
    
    public SendGridEmailService(
        IOptions<SendGridSettings> settings,
        ILogger<SendGridEmailService> logger)
    {
        _settings = settings.Value;
        _client = new SendGridClient(_settings.ApiKey);
        _logger = logger;
    }
    
    public async Task<Result> SendSingleEmailAsync(EmailMessage message)
    {
        try
        {
            var msg = new SendGridMessage
            {
                From = new EmailAddress(_settings.FromEmail, _settings.FromName),
                Subject = message.Subject,
                PlainTextContent = message.PlainTextContent,
                HtmlContent = message.HtmlContent
            };
            
            msg.AddTo(new EmailAddress(message.ToEmail, message.ToName));
            
            var response = await _client.SendEmailAsync(msg);
            
            if (response.StatusCode == HttpStatusCode.Accepted)
            {
                _logger.LogInformation("Email sent to {Email}", message.ToEmail);
                return Result.Success();
            }
            
            var body = await response.Body.ReadAsStringAsync();
            _logger.LogError("SendGrid error: {StatusCode} - {Body}", 
                response.StatusCode, body);
            return Result.Failure("Failed to send email");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {Email}", message.ToEmail);
            return Result.Failure("Email service error");
        }
    }
    
    public async Task<Result> SendBulkEmailAsync(BulkEmailMessage message)
    {
        try
        {
            var msg = new SendGridMessage
            {
                From = new EmailAddress(_settings.FromEmail, _settings.FromName),
                Subject = message.Subject,
                PlainTextContent = message.PlainTextContent,
                HtmlContent = message.HtmlContent
            };
            
            // Add personalizations for bulk send
            var personalizations = message.Recipients
                .Select(r => new Personalization
                {
                    Tos = new List<EmailAddress> { new(r.Email, r.Name) },
                    Substitutions = r.Substitutions
                })
                .ToList();
                
            msg.Personalizations = personalizations;
            
            // SendGrid allows up to 1000 recipients per call
            var batches = personalizations.Chunk(1000);
            
            foreach (var batch in batches)
            {
                msg.Personalizations = batch.ToList();
                var response = await _client.SendEmailAsync(msg);
                
                if (response.StatusCode != HttpStatusCode.Accepted)
                {
                    var body = await response.Body.ReadAsStringAsync();
                    _logger.LogError("SendGrid bulk error: {StatusCode} - {Body}", 
                        response.StatusCode, body);
                    return Result.Failure("Failed to send bulk email");
                }
            }
            
            _logger.LogInformation("Bulk email sent to {Count} recipients", 
                message.Recipients.Count);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending bulk email");
            return Result.Failure("Bulk email service error");
        }
    }
    
    public async Task<Result> SendTemplateEmailAsync(
        string templateId, 
        EmailRecipient recipient, 
        object data)
    {
        try
        {
            var msg = new SendGridMessage
            {
                From = new EmailAddress(_settings.FromEmail, _settings.FromName),
                TemplateId = templateId
            };
            
            msg.AddTo(new EmailAddress(recipient.Email, recipient.Name));
            msg.SetTemplateData(data);
            
            var response = await _client.SendEmailAsync(msg);
            
            if (response.StatusCode == HttpStatusCode.Accepted)
            {
                _logger.LogInformation("Template email {TemplateId} sent to {Email}", 
                    templateId, recipient.Email);
                return Result.Success();
            }
            
            var body = await response.Body.ReadAsStringAsync();
            _logger.LogError("SendGrid template error: {StatusCode} - {Body}", 
                response.StatusCode, body);
            return Result.Failure("Failed to send template email");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending template email to {Email}", 
                recipient.Email);
            return Result.Failure("Template email service error");
        }
    }
}
```

### Email Templates
```csharp
public static class EmailTemplates
{
    // SendGrid Dynamic Template IDs
    public const string Welcome = "d-1234567890abcdef";
    public const string EventRegistration = "d-2345678901abcdef";
    public const string EventReminder = "d-3456789012abcdef";
    public const string VettingApproved = "d-4567890123abcdef";
    public const string VettingInterview = "d-5678901234abcdef";
    public const string PasswordReset = "d-6789012345abcdef";
    public const string EventCancellation = "d-7890123456abcdef";
}
```

### Email Queue for Background Processing
```csharp
public interface IEmailQueueService
{
    Task QueueEmailAsync(EmailMessage message);
    Task QueueBulkEmailAsync(BulkEmailMessage message);
}

public class EmailQueueService : IEmailQueueService
{
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly IServiceProvider _serviceProvider;
    
    public EmailQueueService(
        IBackgroundTaskQueue taskQueue,
        IServiceProvider serviceProvider)
    {
        _taskQueue = taskQueue;
        _serviceProvider = serviceProvider;
    }
    
    public async Task QueueEmailAsync(EmailMessage message)
    {
        await _taskQueue.QueueBackgroundWorkItemAsync(async token =>
        {
            using var scope = _serviceProvider.CreateScope();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            await emailService.SendSingleEmailAsync(message);
        });
    }
    
    public async Task QueueBulkEmailAsync(BulkEmailMessage message)
    {
        await _taskQueue.QueueBackgroundWorkItemAsync(async token =>
        {
            using var scope = _serviceProvider.CreateScope();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            await emailService.SendBulkEmailAsync(message);
        });
    }
}
```

### Usage Examples

#### Send Welcome Email
```csharp
public class AuthService : IAuthService
{
    private readonly IEmailService _emailService;
    
    public async Task<Result> RegisterAsync(RegisterRequest request)
    {
        // Create user...
        
        // Send welcome email
        await _emailService.SendTemplateEmailAsync(
            EmailTemplates.Welcome,
            new EmailRecipient(user.Email, user.SceneName),
            new 
            {
                name = user.SceneName,
                loginUrl = "https://witchcityrope.com/login"
            });
    }
}
```

#### Send Event Announcement
```csharp
public class EventAnnouncementService
{
    private readonly IEmailService _emailService;
    private readonly WitchCityRopeDbContext _db;
    
    public async Task SendEventAnnouncementAsync(int eventId)
    {
        var members = await _db.Users
            .Where(u => u.MembershipStatus == MembershipStatus.Active)
            .Select(u => new EmailRecipient(u.Email, u.SceneName))
            .ToListAsync();
            
        var eventDetails = await _db.Events.FindAsync(eventId);
        
        var message = new BulkEmailMessage
        {
            Subject = $"New Event: {eventDetails.Name}",
            HtmlContent = $@"
                <h2>New Event: {eventDetails.Name}</h2>
                <p>{eventDetails.Description}</p>
                <p>Date: {eventDetails.StartTime:MMMM d, yyyy h:mm tt}</p>
                <a href='https://witchcityrope.com/events/{eventId}'>Register Now</a>
            ",
            Recipients = members
        };
        
        await _emailService.SendBulkEmailAsync(message);
    }
}
```

## Updated Program.cs
```csharp
// Add caching
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICacheService, MemoryCacheService>();

// Add email
builder.Services.Configure<SendGridSettings>(
    builder.Configuration.GetSection("SendGrid"));
builder.Services.AddScoped<IEmailService, SendGridEmailService>();
builder.Services.AddScoped<IEmailQueueService, EmailQueueService>();

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

### Email Benefits
- **Reliable**: SendGrid has high deliverability
- **Scalable**: Can send thousands of emails
- **Templates**: Dynamic templates for consistency
- **Analytics**: Track opens, clicks, bounces
- **Compliance**: Built-in unsubscribe handling

### Future Considerations
- **Redis**: If scaling to multiple servers
- **Distributed Cache**: For load-balanced environments
- **Email Queue**: Could use Azure Service Bus or RabbitMQ later
- **Email Throttling**: SendGrid handles rate limiting
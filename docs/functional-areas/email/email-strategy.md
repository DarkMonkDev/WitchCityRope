# Email Strategy

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
### Email Benefits
- **Reliable**: SendGrid has high deliverability
- **Scalable**: Can send thousands of emails
- **Templates**: Dynamic templates for consistency
- **Analytics**: Track opens, clicks, bounces
- **Compliance**: Built-in unsubscribe handling

### Future Considerations
- **Email Queue**: Could use Azure Service Bus or RabbitMQ later
- **Email Throttling**: SendGrid handles rate limiting

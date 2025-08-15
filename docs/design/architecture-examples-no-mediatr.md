# Architecture Examples Without MediatR

This document demonstrates a simpler, more direct architecture approach without MediatR, showing concrete examples of how the system works with direct service injection and method calls.

## 1. Complete Vertical Slice for User Login

### Login Service (Direct Service Implementation)

```csharp
// File: /src/WitchCityRope.Core/Services/IAuthService.cs
namespace WitchCityRope.Core.Services;

public interface IAuthService
{
    Task<LoginResult> LoginAsync(string email, string password);
    Task<RegisterResult> RegisterAsync(RegisterRequest request);
    Task LogoutAsync();
    Task<User?> GetCurrentUserAsync();
}

public record LoginResult(
    bool Success,
    string? Token,
    User? User,
    string? ErrorMessage,
    bool RequiresTwoFactor = false
);

public record RegisterRequest(
    string Email,
    string Password,
    string SceneName,
    string? LegalName = null,
    string? Phone = null
);

public record RegisterResult(
    bool Success,
    User? User,
    string? ErrorMessage
);
```

```csharp
// File: /src/WitchCityRope.Infrastructure/Services/AuthService.cs
namespace WitchCityRope.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly WitchCityRopeDbContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IJwtTokenService _tokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        WitchCityRopeDbContext context,
        IPasswordHasher<User> passwordHasher,
        IJwtTokenService tokenService,
        ILogger<AuthService> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<LoginResult> LoginAsync(string email, string password)
    {
        try
        {
            // Find user by email
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                return new LoginResult(false, null, null, "Invalid email or password");
            }

            // Verify password
            var result = _passwordHasher.VerifyHashedPassword(
                user, user.PasswordHash, password);

            if (result == PasswordVerificationResult.Failed)
            {
                return new LoginResult(false, null, null, "Invalid email or password");
            }

            // Check if 2FA is required
            if (user.TwoFactorEnabled)
            {
                return new LoginResult(false, null, user, null, RequiresTwoFactor: true);
            }

            // Generate JWT token
            var token = _tokenService.GenerateToken(user);

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {Email} logged in successfully", email);

            return new LoginResult(true, token, user, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for {Email}", email);
            return new LoginResult(false, null, null, "An error occurred during login");
        }
    }

    public async Task<RegisterResult> RegisterAsync(RegisterRequest request)
    {
        try
        {
            // Check if email already exists
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return new RegisterResult(false, null, "Email already registered");
            }

            // Create new user
            var user = new User
            {
                Email = request.Email,
                SceneName = request.SceneName,
                LegalName = request.LegalName,
                Phone = request.Phone,
                CreatedAt = DateTime.UtcNow,
                Role = UserRole.Member
            };

            // Hash password
            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

            // Add to database
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("New user registered: {Email}", request.Email);

            return new RegisterResult(true, user, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for {Email}", request.Email);
            return new RegisterResult(false, null, "An error occurred during registration");
        }
    }

    public async Task LogoutAsync()
    {
        // In a JWT-based system, logout is typically handled client-side
        // This method could be used to blacklist tokens if needed
        await Task.CompletedTask;
    }

    public async Task<User?> GetCurrentUserAsync()
    {
        // This would typically get the user from the current HTTP context
        // Implementation depends on your authentication middleware
        return await Task.FromResult<User?>(null);
    }
}
```

### Login Validation with FluentValidation

```csharp
// File: /src/WitchCityRope.Core/Validators/LoginRequestValidator.cs
namespace WitchCityRope.Core.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters");
    }
}

public record LoginRequest(string Email, string Password);
```

### API Controller (Direct Service Usage)

```csharp
// File: /src/WitchCityRope.Api/Controllers/AuthController.cs
namespace WitchCityRope.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<RegisterRequest> _registerValidator;

    public AuthController(
        IAuthService authService,
        IValidator<LoginRequest> loginValidator,
        IValidator<RegisterRequest> registerValidator)
    {
        _authService = authService;
        _loginValidator = loginValidator;
        _registerValidator = registerValidator;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // Validate request
        var validationResult = await _loginValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                errors = validationResult.Errors.Select(e => new
                {
                    field = e.PropertyName,
                    message = e.ErrorMessage
                })
            });
        }

        // Call service directly
        var result = await _authService.LoginAsync(request.Email, request.Password);

        if (!result.Success)
        {
            return Unauthorized(new { message = result.ErrorMessage });
        }

        if (result.RequiresTwoFactor)
        {
            return Ok(new
            {
                requiresTwoFactor = true,
                userId = result.User?.Id
            });
        }

        return Ok(new
        {
            token = result.Token,
            user = new
            {
                id = result.User!.Id,
                email = result.User.Email,
                sceneName = result.User.SceneName,
                role = result.User.Role.ToString()
            }
        });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        // Validate request
        var validationResult = await _registerValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                errors = validationResult.Errors.Select(e => new
                {
                    field = e.PropertyName,
                    message = e.ErrorMessage
                })
            });
        }

        // Call service directly
        var result = await _authService.RegisterAsync(request);

        if (!result.Success)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(new
        {
            message = "Registration successful",
            user = new
            {
                id = result.User!.Id,
                email = result.User.Email,
                sceneName = result.User.SceneName
            }
        });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _authService.LogoutAsync();
        return Ok(new { message = "Logged out successfully" });
    }
}
```

## 2. Service Injection and Usage in Blazor Component

### Event Service

```csharp
// File: /src/WitchCityRope.Core/Services/IEventService.cs
namespace WitchCityRope.Core.Services;

public interface IEventService
{
    Task<IEnumerable<Event>> GetUpcomingEventsAsync();
    Task<Event?> GetEventByIdAsync(int id);
    Task<EventRegistrationResult> RegisterForEventAsync(int eventId, int userId);
    Task<IEnumerable<Event>> GetUserEventsAsync(int userId);
}

public record EventRegistrationResult(
    bool Success,
    Registration? Registration,
    string? ErrorMessage
);
```

```csharp
// File: /src/WitchCityRope.Infrastructure/Services/EventService.cs
namespace WitchCityRope.Infrastructure.Services;

public class EventService : IEventService
{
    private readonly WitchCityRopeDbContext _context;
    private readonly IPaymentService _paymentService;
    private readonly IEmailService _emailService;
    private readonly ILogger<EventService> _logger;

    public EventService(
        WitchCityRopeDbContext context,
        IPaymentService paymentService,
        IEmailService emailService,
        ILogger<EventService> logger)
    {
        _context = context;
        _paymentService = paymentService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<IEnumerable<Event>> GetUpcomingEventsAsync()
    {
        return await _context.Events
            .Include(e => e.Registrations)
            .Where(e => e.StartTime > DateTime.UtcNow)
            .Where(e => e.IsPublished)
            .OrderBy(e => e.StartTime)
            .ToListAsync();
    }

    public async Task<Event?> GetEventByIdAsync(int id)
    {
        return await _context.Events
            .Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<EventRegistrationResult> RegisterForEventAsync(int eventId, int userId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var evt = await GetEventByIdAsync(eventId);
            if (evt == null)
            {
                return new EventRegistrationResult(false, null, "Event not found");
            }

            // Check if already registered
            if (evt.Registrations.Any(r => r.UserId == userId))
            {
                return new EventRegistrationResult(false, null, "Already registered for this event");
            }

            // Check capacity
            if (evt.Registrations.Count >= evt.MaxAttendees)
            {
                return new EventRegistrationResult(false, null, "Event is full");
            }

            // Create registration
            var registration = new Registration
            {
                EventId = eventId,
                UserId = userId,
                Status = RegistrationStatus.Pending,
                RegisteredAt = DateTime.UtcNow
            };

            _context.Registrations.Add(registration);

            // Process payment if required
            if (evt.Price > 0)
            {
                var paymentResult = await _paymentService.ProcessPaymentAsync(
                    userId, evt.Price, $"Registration for {evt.Title}");

                if (!paymentResult.Success)
                {
                    await transaction.RollbackAsync();
                    return new EventRegistrationResult(false, null, "Payment failed");
                }

                registration.PaymentId = paymentResult.PaymentId;
                registration.Status = RegistrationStatus.Confirmed;
            }
            else
            {
                registration.Status = RegistrationStatus.Confirmed;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Send confirmation email
            await _emailService.SendEventRegistrationConfirmationAsync(userId, evt);

            _logger.LogInformation("User {UserId} registered for event {EventId}", userId, eventId);

            return new EventRegistrationResult(true, registration, null);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error registering user {UserId} for event {EventId}", userId, eventId);
            return new EventRegistrationResult(false, null, "An error occurred during registration");
        }
    }

    public async Task<IEnumerable<Event>> GetUserEventsAsync(int userId)
    {
        return await _context.Events
            .Include(e => e.Registrations)
            .Where(e => e.Registrations.Any(r => r.UserId == userId))
            .OrderBy(e => e.StartTime)
            .ToListAsync();
    }
}
```

### Blazor Component with Direct Service Injection

```razor
@* File: /src/WitchCityRope.Web/Features/Events/Pages/EventList.razor *@
@page "/events"
@inject IEventService EventService
@inject IAuthenticationService AuthService
@inject NavigationManager Navigation
@inject IToastService ToastService

<PageTitle>Upcoming Events - Witch City Rope</PageTitle>

<div class="container">
    <h1>Upcoming Events</h1>

    @if (_loading)
    {
        <LoadingSpinner />
    }
    else if (_events?.Any() == true)
    {
        <div class="event-grid">
            @foreach (var evt in _events)
            {
                <EventCard Event="evt" OnRegister="@(() => RegisterForEvent(evt.Id))" />
            }
        </div>
    }
    else
    {
        <div class="empty-state">
            <p>No upcoming events at this time. Check back soon!</p>
        </div>
    }
</div>

@code {
    private IEnumerable<Event>? _events;
    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            // Direct service call - no mediator needed
            _events = await EventService.GetUpcomingEventsAsync();
        }
        catch (Exception ex)
        {
            ToastService.ShowError("Failed to load events. Please try again.");
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task RegisterForEvent(int eventId)
    {
        var user = await AuthService.GetCurrentUserAsync();
        if (user == null)
        {
            Navigation.NavigateTo($"/login?returnUrl=/events/{eventId}");
            return;
        }

        try
        {
            // Direct service call for registration
            var result = await EventService.RegisterForEventAsync(eventId, user.Id);
            
            if (result.Success)
            {
                ToastService.ShowSuccess("Successfully registered for event!");
                Navigation.NavigateTo($"/member/tickets/{result.Registration!.Id}");
            }
            else
            {
                ToastService.ShowError(result.ErrorMessage ?? "Registration failed");
            }
        }
        catch (Exception ex)
        {
            ToastService.ShowError("An error occurred. Please try again.");
        }
    }
}
```

## 3. Entity Framework Direct Usage

### DbContext Configuration

```csharp
// File: /src/WitchCityRope.Infrastructure/Data/WitchCityRopeDbContext.cs
namespace WitchCityRope.Infrastructure.Data;

public class WitchCityRopeDbContext : DbContext
{
    public WitchCityRopeDbContext(DbContextOptions<WitchCityRopeDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Registration> Registrations => Set<Registration>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<VettingApplication> VettingApplications => Set<VettingApplication>();
    public DbSet<IncidentReport> IncidentReports => Set<IncidentReport>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply all configurations from the assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WitchCityRopeDbContext).Assembly);

        // Global query filters
        modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);
        modelBuilder.Entity<Event>().HasQueryFilter(e => !e.IsDeleted);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Audit fields
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is IAuditableEntity auditable)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        auditable.CreatedAt = DateTime.UtcNow;
                        break;
                    case EntityState.Modified:
                        auditable.UpdatedAt = DateTime.UtcNow;
                        break;
                }
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
```

### Repository Pattern (Optional but Clean)

```csharp
// File: /src/WitchCityRope.Core/Repositories/IEventRepository.cs
namespace WitchCityRope.Core.Repositories;

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(int id);
    Task<IEnumerable<Event>> GetUpcomingAsync();
    Task<IEnumerable<Event>> GetByDateRangeAsync(DateTime start, DateTime end);
    Task<Event> CreateAsync(Event evt);
    Task UpdateAsync(Event evt);
    Task<bool> ExistsAsync(int id);
}
```

```csharp
// File: /src/WitchCityRope.Infrastructure/Repositories/EventRepository.cs
namespace WitchCityRope.Infrastructure.Repositories;

public class EventRepository : IEventRepository
{
    private readonly WitchCityRopeDbContext _context;

    public EventRepository(WitchCityRopeDbContext context)
    {
        _context = context;
    }

    public async Task<Event?> GetByIdAsync(int id)
    {
        return await _context.Events
            .Include(e => e.Registrations)
                .ThenInclude(r => r.User)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<IEnumerable<Event>> GetUpcomingAsync()
    {
        return await _context.Events
            .Where(e => e.StartTime > DateTime.UtcNow)
            .Where(e => e.IsPublished)
            .OrderBy(e => e.StartTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Event>> GetByDateRangeAsync(DateTime start, DateTime end)
    {
        return await _context.Events
            .Where(e => e.StartTime >= start && e.StartTime <= end)
            .OrderBy(e => e.StartTime)
            .ToListAsync();
    }

    public async Task<Event> CreateAsync(Event evt)
    {
        _context.Events.Add(evt);
        await _context.SaveChangesAsync();
        return evt;
    }

    public async Task UpdateAsync(Event evt)
    {
        _context.Events.Update(evt);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Events.AnyAsync(e => e.Id == id);
    }
}
```

### Direct DbContext Usage in Service

```csharp
// File: /src/WitchCityRope.Infrastructure/Services/VettingService.cs
namespace WitchCityRope.Infrastructure.Services;

public class VettingService : IVettingService
{
    private readonly WitchCityRopeDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<VettingService> _logger;

    public VettingService(
        WitchCityRopeDbContext context,
        IEmailService emailService,
        ILogger<VettingService> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<VettingApplication> SubmitApplicationAsync(VettingApplicationDto dto, int userId)
    {
        // Direct EF Core usage - no repositories needed for simple operations
        var existingApplication = await _context.VettingApplications
            .FirstOrDefaultAsync(a => a.UserId == userId && a.Status == ApplicationStatus.Pending);

        if (existingApplication != null)
        {
            throw new DomainException("You already have a pending application");
        }

        var application = new VettingApplication
        {
            UserId = userId,
            SceneName = dto.SceneName,
            Experience = dto.Experience,
            References = dto.References,
            SafetyKnowledge = dto.SafetyKnowledge,
            Status = ApplicationStatus.Pending,
            SubmittedAt = DateTime.UtcNow
        };

        _context.VettingApplications.Add(application);
        await _context.SaveChangesAsync();

        // Send notification
        await _emailService.SendApplicationReceivedAsync(userId);

        _logger.LogInformation("Vetting application submitted for user {UserId}", userId);

        return application;
    }

    public async Task<IEnumerable<VettingApplication>> GetPendingApplicationsAsync()
    {
        // Direct query with includes
        return await _context.VettingApplications
            .Include(a => a.User)
            .Where(a => a.Status == ApplicationStatus.Pending)
            .OrderBy(a => a.SubmittedAt)
            .ToListAsync();
    }

    public async Task<VettingApplication> ReviewApplicationAsync(
        int applicationId, 
        bool approved, 
        string notes, 
        int reviewerId)
    {
        var application = await _context.VettingApplications
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == applicationId);

        if (application == null)
        {
            throw new DomainException("Application not found");
        }

        if (application.Status != ApplicationStatus.Pending)
        {
            throw new DomainException("Application has already been reviewed");
        }

        // Update application
        application.Status = approved ? ApplicationStatus.Approved : ApplicationStatus.Rejected;
        application.ReviewedAt = DateTime.UtcNow;
        application.ReviewerId = reviewerId;
        application.ReviewNotes = notes;

        // Update user role if approved
        if (approved)
        {
            application.User.IsVetted = true;
        }

        await _context.SaveChangesAsync();

        // Send notification
        await _emailService.SendApplicationReviewedAsync(
            application.UserId, 
            approved, 
            notes);

        _logger.LogInformation(
            "Application {ApplicationId} reviewed by {ReviewerId}: {Status}", 
            applicationId, 
            reviewerId, 
            application.Status);

        return application;
    }
}
```

## 4. Validation Pattern with FluentValidation

### Event Creation Validation

```csharp
// File: /src/WitchCityRope.Core/Validators/CreateEventValidator.cs
namespace WitchCityRope.Core.Validators;

public class CreateEventValidator : AbstractValidator<CreateEventDto>
{
    public CreateEventValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .Length(3, 100).WithMessage("Title must be between 3 and 100 characters");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required")
            .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters");

        RuleFor(x => x.StartTime)
            .GreaterThan(DateTime.UtcNow.AddHours(24))
            .WithMessage("Event must be scheduled at least 24 hours in advance");

        RuleFor(x => x.EndTime)
            .GreaterThan(x => x.StartTime)
            .WithMessage("End time must be after start time");

        RuleFor(x => x.MaxAttendees)
            .InclusiveBetween(1, 100)
            .WithMessage("Maximum attendees must be between 1 and 100");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Price cannot be negative");

        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Invalid event type");

        // Custom validation rule
        RuleFor(x => x)
            .Must(BeValidDuration)
            .WithMessage("Event duration must be between 30 minutes and 8 hours");

        // Conditional validation
        When(x => x.RequiresVetting, () =>
        {
            RuleFor(x => x.VettingRequirements)
                .NotEmpty()
                .WithMessage("Vetting requirements must be specified for vetted events");
        });
    }

    private bool BeValidDuration(CreateEventDto dto)
    {
        var duration = dto.EndTime - dto.StartTime;
        return duration >= TimeSpan.FromMinutes(30) && duration <= TimeSpan.FromHours(8);
    }
}

public record CreateEventDto(
    string Title,
    string Description,
    DateTime StartTime,
    DateTime EndTime,
    int MaxAttendees,
    decimal Price,
    EventType Type,
    bool RequiresVetting,
    string? VettingRequirements,
    string? Location
);
```

### Using Validation in Controller

```csharp
// File: /src/WitchCityRope.Api/Controllers/EventController.cs
namespace WitchCityRope.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EventController : ControllerBase
{
    private readonly IEventService _eventService;
    private readonly IValidator<CreateEventDto> _createValidator;
    private readonly IValidator<UpdateEventDto> _updateValidator;

    public EventController(
        IEventService eventService,
        IValidator<CreateEventDto> createValidator,
        IValidator<UpdateEventDto> updateValidator)
    {
        _eventService = eventService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Organizer")]
    public async Task<IActionResult> CreateEvent([FromBody] CreateEventDto dto)
    {
        // Validate request
        var validationResult = await _createValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            return BadRequest(ValidationProblemDetails(validationResult));
        }

        try
        {
            // Direct service call
            var evt = await _eventService.CreateEventAsync(dto, User.GetUserId());
            
            return CreatedAtAction(
                nameof(GetEvent), 
                new { id = evt.Id }, 
                new { id = evt.Id, title = evt.Title });
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetEvent(int id)
    {
        var evt = await _eventService.GetEventByIdAsync(id);
        
        if (evt == null)
        {
            return NotFound();
        }

        return Ok(MapToDto(evt));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Organizer")]
    public async Task<IActionResult> UpdateEvent(int id, [FromBody] UpdateEventDto dto)
    {
        // Validate request
        var validationResult = await _updateValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            return BadRequest(ValidationProblemDetails(validationResult));
        }

        try
        {
            await _eventService.UpdateEventAsync(id, dto, User.GetUserId());
            return NoContent();
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    private ValidationProblemDetails ValidationProblemDetails(ValidationResult result)
    {
        var problemDetails = new ValidationProblemDetails
        {
            Title = "Validation failed",
            Status = 400
        };

        foreach (var error in result.Errors)
        {
            if (!problemDetails.Errors.ContainsKey(error.PropertyName))
            {
                problemDetails.Errors[error.PropertyName] = new List<string>();
            }
            
            problemDetails.Errors[error.PropertyName].Add(error.ErrorMessage);
        }

        return problemDetails;
    }

    private EventDto MapToDto(Event evt)
    {
        return new EventDto
        {
            Id = evt.Id,
            Title = evt.Title,
            Description = evt.Description,
            StartTime = evt.StartTime,
            EndTime = evt.EndTime,
            MaxAttendees = evt.MaxAttendees,
            CurrentAttendees = evt.Registrations.Count(r => r.Status == RegistrationStatus.Confirmed),
            Price = evt.Price,
            Type = evt.Type,
            RequiresVetting = evt.RequiresVetting,
            IsPublished = evt.IsPublished
        };
    }
}
```

## 5. Benefits of the Direct Approach

### Simplified Dependency Injection Setup

```csharp
// File: /src/WitchCityRope.Api/Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add services directly - no MediatR configuration needed
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IVettingService, VettingService>();
builder.Services.AddScoped<IPaymentService, PayPalService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// Add repositories if using repository pattern
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Add validators
builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

// Add DbContext
builder.Services.AddDbContext<WitchCityRopeDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure pipeline...
```

### Testing Without MediatR

```csharp
// File: /tests/WitchCityRope.Api.Tests/Services/EventServiceTests.cs
namespace WitchCityRope.Api.Tests.Services;

public class EventServiceTests
{
    private readonly Mock<WitchCityRopeDbContext> _mockContext;
    private readonly Mock<IPaymentService> _mockPaymentService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<ILogger<EventService>> _mockLogger;
    private readonly EventService _service;

    public EventServiceTests()
    {
        _mockContext = new Mock<WitchCityRopeDbContext>();
        _mockPaymentService = new Mock<IPaymentService>();
        _mockEmailService = new Mock<IEmailService>();
        _mockLogger = new Mock<ILogger<EventService>>();

        _service = new EventService(
            _mockContext.Object,
            _mockPaymentService.Object,
            _mockEmailService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task RegisterForEventAsync_WhenEventNotFound_ReturnsError()
    {
        // Arrange
        var eventId = 1;
        var userId = 1;

        _mockContext.Setup(x => x.Events)
            .Returns(MockDbSet(new List<Event>()));

        // Act
        var result = await _service.RegisterForEventAsync(eventId, userId);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Event not found", result.ErrorMessage);
    }

    [Fact]
    public async Task RegisterForEventAsync_WhenSuccessful_SendsEmail()
    {
        // Arrange
        var evt = new Event
        {
            Id = 1,
            Title = "Test Event",
            Price = 0,
            MaxAttendees = 10,
            Registrations = new List<Registration>()
        };

        _mockContext.Setup(x => x.Events)
            .Returns(MockDbSet(new List<Event> { evt }));

        _mockEmailService.Setup(x => x.SendEventRegistrationConfirmationAsync(It.IsAny<int>(), It.IsAny<Event>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.RegisterForEventAsync(1, 1);

        // Assert
        Assert.True(result.Success);
        _mockEmailService.Verify(x => x.SendEventRegistrationConfirmationAsync(1, evt), Times.Once);
    }
}
```

## Key Benefits Summary

1. **Simplicity**: Direct method calls are easier to understand and debug
2. **Less Boilerplate**: No need for command/query classes and handlers
3. **Better IntelliSense**: IDEs can directly show available methods and parameters
4. **Easier Testing**: Mock services directly without MediatR pipeline complexity
5. **Performance**: Fewer allocations and no reflection-based handler resolution
6. **Maintainability**: Code flow is linear and easy to follow
7. **Flexibility**: Services can return rich result objects with multiple properties
8. **Type Safety**: Direct method signatures provide compile-time checking

## When This Approach Works Best

- Small to medium-sized applications
- Teams that value simplicity and readability
- Projects where the CQRS pattern isn't providing clear benefits
- When you want to minimize abstractions
- When debugging and maintenance simplicity is prioritized
- For applications where performance matters (fewer allocations)

The direct service approach provides a clean, maintainable architecture without the complexity of MediatR, making it easier for developers to understand and work with the codebase.
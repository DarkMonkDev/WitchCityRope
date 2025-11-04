# Service Layer Implementation Patterns

**Purpose**: Standardized patterns for implementing business logic services in WitchCityRope.
**When to Read**: When implementing services, repositories, or API endpoints.
**Related**: [Entity Framework Patterns](./entity-framework-patterns.md), [Error Handling Patterns](./error-handling-patterns.md)

## Service Pattern Requirements

All business logic services must follow the standardized implementation pattern:

### Service Interface Design
```csharp
public interface IUserManagementService
{
    Task<Result<PagedResult<UserDto>>> GetUsersAsync(UserFilterRequest filter, CancellationToken ct = default);
    Task<Result<UserDto>> GetUserAsync(Guid id, CancellationToken ct = default);
    Task<Result<UserDto>> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default);
    Task<Result<UserDto>> UpdateUserAsync(Guid id, UpdateUserRequest request, CancellationToken ct = default);
    Task<Result> DeleteUserAsync(Guid id, CancellationToken ct = default);
}
```

### Service Implementation Template
```csharp
public class UserManagementService : IUserManagementService
{
    private readonly WitchCityRopeDbContext _db;
    private readonly ILogger<UserManagementService> _logger;
    private readonly IValidator<CreateUserRequest> _createValidator;
    private readonly IValidator<UpdateUserRequest> _updateValidator;
    private readonly ICacheService _cache;
    private readonly IEmailService _email;

    public UserManagementService(
        WitchCityRopeDbContext db,
        ILogger<UserManagementService> logger,
        IValidator<CreateUserRequest> createValidator,
        IValidator<UpdateUserRequest> updateValidator,
        ICacheService cache,
        IEmailService email)
    {
        _db = db;
        _logger = logger;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _cache = cache;
        _email = email;
    }

    public async Task<Result<PagedResult<UserDto>>> GetUsersAsync(
        UserFilterRequest filter,
        CancellationToken ct = default)
    {
        try
        {
            var query = _db.Users
                .Include(u => u.UserExtended)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .AsNoTracking();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                query = query.Where(u =>
                    u.Email.Contains(filter.SearchTerm) ||
                    u.UserExtended.SceneName.Contains(filter.SearchTerm));
            }

            // Apply sorting
            query = filter.SortBy switch
            {
                "email" => query.OrderBy(u => u.Email),
                "joined" => query.OrderByDescending(u => u.UserExtended.CreatedAt),
                _ => query.OrderBy(u => u.UserExtended.SceneName ?? u.Email)
            };

            // Execute with pagination
            var totalCount = await query.CountAsync(ct);

            var users = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(u => new UserDto(u))
                .ToListAsync(ct);

            return Result<PagedResult<UserDto>>.Success(new PagedResult<UserDto>
            {
                Items = users,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching users");
            return Result<PagedResult<UserDto>>.Failure("Failed to fetch users");
        }
    }

    public async Task<Result<UserDto>> CreateUserAsync(
        CreateUserRequest request,
        CancellationToken ct = default)
    {
        // Validate input
        var validationResult = await _createValidator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return Result<UserDto>.Failure(validationResult.Errors.First().ErrorMessage);
        }

        using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            // Create user
            var user = new ApplicationUser
            {
                Email = request.Email,
                UserName = request.Email,
                EmailConfirmed = false
            };

            var createResult = await _userManager.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
            {
                return Result<UserDto>.Failure(createResult.Errors.First().Description);
            }

            // Create extended profile
            var userExtended = new UserExtended
            {
                UserId = user.Id,
                MembershipLevel = request.MembershipLevel,
                VettingStatus = VettingStatus.NotStarted,
                Pronouns = request.Pronouns,
                SceneName = request.SceneName
            };

            _db.UsersExtended.Add(userExtended);
            await _db.SaveChangesAsync(ct);

            // Send welcome email
            await _email.SendWelcomeEmailAsync(user.Email, ct);

            // Invalidate cache
            await _cache.RemoveAsync("users:*", ct);

            await transaction.CommitAsync(ct);

            return Result<UserDto>.Success(new UserDto(user, userExtended));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "Error creating user");
            return Result<UserDto>.Failure("Failed to create user");
        }
    }
}
```

### API Controller Pattern
```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly IUserManagementService _userService;

    public UsersController(IUserManagementService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<UserDto>), 200)]
    public async Task<IActionResult> GetUsers([FromQuery] UserFilterRequest filter)
    {
        var result = await _userService.GetUsersAsync(filter);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPost]
    [ProducesResponseType(typeof(UserDto), 201)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var result = await _userService.CreateUserAsync(request);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetUser), new { id = result.Value.Id }, result.Value)
            : BadRequest(result.Error);
    }
}
```

## Service Layer Requirements

1. **Result Pattern**: All operations return `Result<T>` for consistent error handling
2. **Validation**: Use FluentValidation for input validation (see [Validation Standards](/docs/standards-processes/validation-standardization/VALIDATION_STANDARDS.md))
3. **Logging**: Structured logging with appropriate log levels
4. **Transactions**: Database transactions for multi-operation business logic (see [Entity Framework Patterns](/docs/standards-processes/development-standards/entity-framework-patterns.md))
5. **Caching**: Cache invalidation strategies for data freshness
6. **Async/Await**: All operations must be asynchronous
7. **Cancellation Tokens**: Support cancellation for long-running operations

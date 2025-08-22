# Architecture Validator Rules - Simple Vertical Slice Enforcement
<!-- Last Updated: 2025-08-22 -->
<!-- Version: 1.0 -->
<!-- Owner: Librarian Agent -->
<!-- Status: Active -->

## Executive Summary

This document defines comprehensive validation rules for enforcing the **Simple Vertical Slice Architecture** in WitchCityRope API development. These rules prevent the introduction of MediatR/CQRS complexity and ensure consistent application of simple, maintainable patterns.

### Validation Purpose
- **Prevent Architectural Drift**: Stop complex patterns from being reintroduced
- **Enforce Consistency**: Ensure all features follow the same simple patterns
- **Catch Violations Early**: Detect problems before they reach production
- **Guide Developers**: Provide clear feedback on correct patterns

---

## 🚨 CRITICAL: Prohibited Patterns (IMMEDIATE VIOLATION)

### MediatR Dependencies (ZERO TOLERANCE)
```csharp
// ❌ IMMEDIATE VIOLATION - Any MediatR usage
using MediatR;
using MediatR.Extensions.Microsoft.DependencyInjection;

// ❌ Service registration violations
services.AddMediatR(typeof(Program));
services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

// ❌ Injection violations
private readonly IMediator _mediator;
public Controller(IMediator mediator) { ... }

// ❌ Usage violations
await _mediator.Send(new GetHealthQuery());
await _mediator.Publish(new EventCreated());
```

**Validation Rule**: Fail build if ANY MediatR references found in code

### CQRS Pattern Violations (ZERO TOLERANCE)
```csharp
// ❌ IMMEDIATE VIOLATION - Command/Query interfaces
public class GetHealthQuery : IRequest<HealthResponse> { }
public record CreateEventCommand(string Title) : IRequest<EventResponse>;

// ❌ Handler pattern violations  
public class GetHealthHandler : IRequestHandler<GetHealthQuery, HealthResponse>
{
    public async Task<HealthResponse> Handle(GetHealthQuery request, CancellationToken cancellationToken)
    { ... }
}

// ❌ Pipeline behavior violations
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{ ... }

// ❌ Notification pattern violations
public class EventCreatedNotification : INotification { }
public class EventCreatedHandler : INotificationHandler<EventCreatedNotification> { }
```

**Validation Rule**: Fail build if ANY IRequest, IRequestHandler, or IPipelineBehavior interfaces found

### Repository Pattern Violations (FORBIDDEN)
```csharp
// ❌ VIOLATION - Complex repository patterns
public interface IEventRepository
{
    Task<Event> GetByIdAsync(int id);
    Task<IEnumerable<Event>> GetAllAsync();
    Task AddAsync(Event entity);
    Task UpdateAsync(Event entity);
    Task DeleteAsync(int id);
}

public class EventRepository : IEventRepository { ... }

// ❌ Unit of Work pattern violations
public interface IUnitOfWork { ... }
public class UnitOfWork : IUnitOfWork { ... }

// ❌ Generic repository violations
public interface IRepository<T> where T : class { ... }
public class Repository<T> : IRepository<T> where T : class { ... }
```

**Validation Rule**: Fail build if ANY repository pattern interfaces found (allow only direct DbContext usage)

---

## ✅ Required Patterns (MUST VALIDATE)

### Folder Structure Requirements
```
✅ REQUIRED STRUCTURE:
Features/[FeatureName]/
├── Services/
│   └── [FeatureName]Service.cs     # Direct Entity Framework service
├── Endpoints/
│   └── [FeatureName]Endpoints.cs   # Minimal API endpoint registration
├── Models/
│   ├── [Name]Request.cs            # Request DTOs
│   └── [Name]Response.cs           # Response DTOs
└── Validation/                     # Optional
    └── [Name]Validator.cs          # FluentValidation (if needed)

❌ VIOLATIONS:
- Controllers/ folder (old pattern)
- Handlers/ folder (MediatR pattern)  
- Commands/ or Queries/ folders (CQRS pattern)
- Repositories/ folder (repository pattern)
```

**Validation Rules**:
- Each feature MUST have Services/ and Endpoints/ folders
- NO Controllers/, Handlers/, Commands/, Queries/, or Repositories/ folders allowed
- Models/ folder MUST contain only DTOs (no domain entities)

### Service Pattern Requirements
```csharp
// ✅ REQUIRED SERVICE PATTERN
public class [FeatureName]Service
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<[FeatureName]Service> _logger;

    public [FeatureName]Service(ApplicationDbContext context, ILogger<[FeatureName]Service> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ✅ REQUIRED: Direct Entity Framework queries
    public async Task<(bool Success, [Response]? Response, string Error)> [Method]Async(...)
    {
        try
        {
            var result = await _context.[Entity]
                .AsNoTracking()  // Required for read-only
                .Where(...)
                .ToListAsync(cancellationToken);
                
            return (true, response, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "...");
            return (false, null, "Error message");
        }
    }
}
```

**Validation Rules**:
- Service MUST inject ApplicationDbContext directly (no repository abstractions)
- Service MUST use tuple return pattern: (bool Success, T? Response, string Error)
- Service MUST have try-catch with structured logging
- Service MUST use AsNoTracking() for read-only queries

### Endpoint Pattern Requirements
```csharp
// ✅ REQUIRED ENDPOINT PATTERN
public static class [FeatureName]Endpoints
{
    public static void Map[FeatureName]Endpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/[feature]", async (
            [FeatureName]Service service,
            CancellationToken cancellationToken) =>
            {
                var (success, response, error) = await service.[Method]Async(cancellationToken);
                
                return success 
                    ? Results.Ok(response)
                    : Results.Problem(title: "...", detail: error, statusCode: 400);
            })
            .WithName("...")
            .WithSummary("...")
            .WithTags("[FeatureName]")
            .Produces<[Response]>(200);
    }
}
```

**Validation Rules**:
- Endpoints MUST use static class with Map[FeatureName]Endpoints method
- Endpoints MUST inject services directly (no controller base classes)
- Endpoints MUST use minimal API pattern (MapGet, MapPost, etc.)
- Endpoints MUST include OpenAPI documentation (WithName, WithSummary, WithTags)
- Endpoints MUST use Results.Ok() and Results.Problem() for responses

---

## Code Analysis Rules

### File Naming Conventions
```csharp
// ✅ CORRECT NAMING
Features/Health/Services/HealthService.cs
Features/Health/Endpoints/HealthEndpoints.cs
Features/Health/Models/HealthResponse.cs
Features/Authentication/Services/AuthenticationService.cs

// ❌ INCORRECT NAMING
Controllers/HealthController.cs           // No controllers allowed
Handlers/GetHealthHandler.cs             // No handlers allowed
Commands/CreateEventCommand.cs           // No commands allowed
Queries/GetEventsQuery.cs               // No queries allowed
Repositories/EventRepository.cs          // No repositories allowed
```

**Validation Rules**:
- Service files MUST end with "Service.cs"
- Endpoint files MUST end with "Endpoints.cs"
- Model files MUST end with "Request.cs" or "Response.cs"
- NO files ending with "Controller.cs", "Handler.cs", "Command.cs", "Query.cs", or "Repository.cs"

### Namespace Validation
```csharp
// ✅ CORRECT NAMESPACES
namespace WitchCityRope.Api.Features.Health.Services;
namespace WitchCityRope.Api.Features.Health.Endpoints;
namespace WitchCityRope.Api.Features.Health.Models;

// ❌ INCORRECT NAMESPACES
namespace WitchCityRope.Api.Controllers;        // No controllers
namespace WitchCityRope.Api.Handlers;          // No handlers  
namespace WitchCityRope.Api.Commands;          // No commands
namespace WitchCityRope.Api.Queries;           // No queries
namespace WitchCityRope.Api.Repositories;      // No repositories
```

**Validation Rules**:
- All feature code MUST be in Features.[FeatureName] namespace
- NO Controllers, Handlers, Commands, Queries, or Repositories namespaces allowed

### Dependency Injection Validation  
```csharp
// ✅ CORRECT DI REGISTRATION
services.AddScoped<HealthService>();
services.AddScoped<EventService>();
services.AddScoped<AuthenticationService>();

// ❌ INCORRECT DI REGISTRATION  
services.AddMediatR(typeof(Program));
services.AddScoped<IEventRepository, EventRepository>();
services.AddScoped<IUnitOfWork, UnitOfWork>();
services.AddTransient<GetHealthHandler>();
```

**Validation Rules**:
- Service registration MUST use concrete service classes only
- NO MediatR registration allowed
- NO repository interface registrations allowed
- NO handler registrations allowed

---

## Automated Validation Implementation

### Build-Time Validation Rules

**1. File Structure Validation**
```xml
<!-- In .csproj file -->
<Target Name="ValidateArchitecture" BeforeTargets="Build">
    <ItemGroup>
        <ProhibitedFiles Include="**/*Controller.cs" />
        <ProhibitedFiles Include="**/*Handler.cs" />
        <ProhibitedFiles Include="**/*Command.cs" />
        <ProhibitedFiles Include="**/*Query.cs" />
        <ProhibitedFiles Include="**/Repository.cs" />
    </ItemGroup>
    
    <Error Condition="'@(ProhibitedFiles)' != ''" 
           Text="Prohibited file patterns found: @(ProhibitedFiles). Use Features/[Name]/Services/[Name]Service.cs pattern instead." />
</Target>
```

**2. Code Pattern Validation**
```xml
<Target Name="ValidateCodePatterns" BeforeTargets="Build">
    <Exec Command="findstr /R /C:&quot;using MediatR&quot; /C:&quot;IRequest&lt;&quot; /C:&quot;IRequestHandler&lt;&quot; $(MSBuildProjectDirectory)\**\*.cs" 
          IgnoreExitCode="true" 
          ConsoleToMSBuild="true">
        <Output TaskParameter="ExitCode" PropertyName="MediatRFound" />
    </Exec>
    
    <Error Condition="$(MediatRFound) == 0" 
           Text="MediatR patterns found in code. Use direct Entity Framework services instead." />
</Target>
```

### Pre-Commit Hook Validation
```bash
#!/bin/sh
# .git/hooks/pre-commit

# Check for prohibited patterns in staged files
prohibited_patterns=(
    "using MediatR"
    "IRequest<"
    "IRequestHandler<"
    "IPipelineBehavior<"
    "INotification"
    "INotificationHandler<"
    "class.*Controller"
    "class.*Handler.*:.*Handler"
    "class.*Repository.*:.*Repository"
)

for pattern in "${prohibited_patterns[@]}"; do
    if git diff --cached --name-only | xargs grep -l "$pattern" 2>/dev/null; then
        echo "❌ ARCHITECTURE VIOLATION: Found prohibited pattern '$pattern'"
        echo "Use simple vertical slice patterns instead"
        exit 1
    fi
done

# Check folder structure
if find . -path "*/Controllers/*.cs" -o -path "*/Handlers/*.cs" -o -path "*/Commands/*.cs" -o -path "*/Queries/*.cs" -o -path "*/Repositories/*.cs" | grep -q .; then
    echo "❌ FOLDER STRUCTURE VIOLATION: Use Features/[Name]/ organization"
    exit 1
fi

echo "✅ Architecture validation passed"
```

---

## Manual Code Review Checklist

### ✅ Architecture Review Checklist

**Folder Structure Review**:
- [ ] Feature organized under Features/[Name]/ folder
- [ ] Contains Services/, Endpoints/, Models/ folders
- [ ] NO Controllers/, Handlers/, Commands/, Queries/, Repositories/ folders
- [ ] Service files named [Name]Service.cs
- [ ] Endpoint files named [Name]Endpoints.cs

**Service Pattern Review**:
- [ ] Service injects ApplicationDbContext directly
- [ ] Service uses tuple return pattern (bool Success, T? Response, string Error)
- [ ] Service has proper try-catch with structured logging
- [ ] Service uses AsNoTracking() for read-only queries
- [ ] NO repository pattern abstractions
- [ ] NO MediatR dependencies

**Endpoint Pattern Review**:
- [ ] Uses minimal API pattern (MapGet, MapPost, etc.)
- [ ] Static class with Map[Name]Endpoints method
- [ ] Direct service injection in endpoint
- [ ] Proper OpenAPI documentation (WithName, WithSummary, etc.)
- [ ] Uses Results.Ok() and Results.Problem()
- [ ] NO controller inheritance

**Dependency Injection Review**:
- [ ] Services registered with AddScoped<[Name]Service>()
- [ ] NO MediatR registration
- [ ] NO repository interface registrations
- [ ] NO handler registrations

**General Pattern Review**:
- [ ] NO using MediatR; statements
- [ ] NO IRequest, IRequestHandler interfaces
- [ ] NO Command/Query classes
- [ ] NO IPipelineBehavior implementations
- [ ] Direct Entity Framework usage throughout

---

## Violation Response Procedures

### Level 1: Automated Detection
**Action**: Build fails with clear error message
```
❌ ARCHITECTURE VIOLATION DETECTED
Pattern: using MediatR;
File: Features/Events/Services/EventService.cs:3
Solution: Remove MediatR dependency and use direct Entity Framework services
Reference: /docs/guides-setup/ai-agents/backend-developer-vertical-slice-guide.md
```

### Level 2: Code Review Detection  
**Action**: Pull request blocked with review comments
```
❌ Architecture violation in Features/Events/Services/EventService.cs

Problem: Found MediatR pattern usage
- Line 15: private readonly IMediator _mediator;
- Line 23: await _mediator.Send(new GetEventsQuery());

Required Fix:
1. Remove IMediator dependency
2. Inject ApplicationDbContext directly
3. Replace handler call with direct Entity Framework query

Reference Pattern: Features/Health/Services/HealthService.cs
```

### Level 3: Manual Intervention
**Action**: Architecture validator agent intervention
```
🤖 ARCHITECTURE VALIDATOR INTERVENTION

Multiple violations detected in PR #123:
1. MediatR dependencies in EventService.cs
2. Repository pattern in UserRepository.cs  
3. Controller pattern in EventsController.cs

Required Actions:
1. Refactor to Features/Events/Services/EventService.cs pattern
2. Remove all MediatR dependencies
3. Use direct Entity Framework queries
4. Follow Health feature example exactly

Blocking merge until violations resolved.
Status: REQUIRES_ARCHITECTURE_COMPLIANCE
```

---

## Success Metrics and Monitoring

### Quantitative Metrics
- **Violation Detection Rate**: Target 100% - catch all violations before merge
- **False Positive Rate**: Target <5% - avoid blocking valid code
- **Resolution Time**: Target <2 hours - quick feedback and fixes
- **Pattern Compliance**: Target 100% - all features follow same patterns

### Qualitative Indicators
- **Code Consistency**: All features use identical patterns
- **Developer Confidence**: Clear guidance prevents confusion
- **Maintenance Ease**: Simple patterns are easy to understand
- **Performance Stability**: No MediatR overhead maintains performance

### Monitoring Dashboard
```
Architecture Compliance Dashboard
================================

✅ Pattern Compliance: 100% (15/15 features)
✅ Build Violations: 0 in last 30 days  
✅ PR Violations: 2 caught and fixed this week
✅ Manual Reviews: 0 escalations needed

Recent Violations (Resolved):
- 2025-08-20: MediatR usage in EventService - Fixed in 1.2 hours
- 2025-08-19: Repository pattern in UserService - Fixed in 0.8 hours

Top Violation Types:
1. MediatR dependencies (60% of violations)
2. Repository patterns (25% of violations)  
3. Folder structure (15% of violations)
```

---

## Integration with Development Workflow

### IDE Integration
**Visual Studio / VS Code Extensions**:
- Real-time pattern validation while coding
- Syntax highlighting for prohibited patterns
- Quick fixes to convert to correct patterns
- IntelliSense warnings for architectural violations

### CI/CD Integration
```yaml
# Azure DevOps Pipeline / GitHub Actions
- name: Architecture Validation
  script: |
    # Run automated architecture validation
    dotnet build --verbosity normal /p:TreatWarningsAsErrors=true
    
    # Check for prohibited patterns
    if grep -r "using MediatR" src/; then
      echo "❌ MediatR patterns detected"
      exit 1
    fi
    
    # Validate folder structure
    if find src/ -path "*/Controllers/*" -o -path "*/Handlers/*"; then
      echo "❌ Prohibited folder structure"
      exit 1
    fi
    
    echo "✅ Architecture validation passed"
```

### Documentation Integration
- Link validation failures to specific guidance documents
- Provide exact code examples for fixes
- Reference Health feature as working example
- Include performance benefits of simple patterns

---

## Training and Onboarding

### Developer Onboarding Checklist
- [ ] Read Simple Vertical Slice Architecture guide
- [ ] Study Health feature example implementation
- [ ] Understand prohibited patterns and reasons
- [ ] Practice converting MediatR code to simple patterns
- [ ] Run architecture validator on sample violations

### AI Agent Training Integration
- [ ] Include validation rules in agent training data
- [ ] Test agents with violation scenarios
- [ ] Verify agents suggest correct patterns
- [ ] Monitor agent pattern compliance over time

---

Remember: **ARCHITECTURE VALIDATION IS NON-NEGOTIABLE**. The simple vertical slice patterns are designed to maintain performance, simplicity, and maintainability. Any violation undermines these benefits and must be prevented through rigorous automated and manual validation procedures.
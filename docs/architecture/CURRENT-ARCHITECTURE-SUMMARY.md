# Current Architecture Summary

**Last Updated**: January 26, 2025  
**Status**: Ready for Development

## Overview

This document provides a comprehensive summary of the current architecture and technology decisions for the Witch City Rope project. All documentation has been consolidated and aligned to present a single, coherent vision.

## Core Architecture Pattern

### Vertical Slice Architecture with Direct Services
- **No MediatR** - Removed for simplicity
- **Direct Service Injection** - Services injected directly into Blazor components
- **Feature-Based Organization** - Code organized by business capability
- **Single Project** - Monolithic Blazor Server application

### Benefits of This Approach
1. **Simplicity** - Easy to understand and navigate
2. **Performance** - No pipeline overhead
3. **Debuggability** - Direct execution path
4. **Maintainability** - Less boilerplate code

## Technology Stack

### Core Framework
- **ASP.NET Core 9.0** - Latest LTS version
- **Blazor Server** - Not WebAssembly (simpler deployment)
- **Entity Framework Core 9.0** - With SQLite provider

### UI & Frontend
- **Syncfusion Blazor Components** - Commercial license available
- **Custom CSS Design System** - Based on Witch City Rope branding
- **Minimal JavaScript** - Only for mobile menu and interactions

### Data & Storage
- **SQLite** - File-based database (not PostgreSQL)
- **IMemoryCache** - Built-in caching (not Redis)
- **Entity Framework** - Direct usage (no repository pattern)

### External Services
- **SendGrid** - Email delivery (transactional and bulk)
- **Google OAuth 2.0** - Social authentication
- **PayPal Checkout** - Payment processing
- **Google Fonts** - Typography CDN

### Development & Testing
- **xUnit** - Unit testing framework
- **Moq** - Mocking framework
- **FluentAssertions** - Assertion library
- **bUnit** - Blazor component testing
- **FluentValidation** - Input validation

### Infrastructure
- **Docker** - Single container deployment
- **GitHub Actions** - CI/CD pipeline
- **Low-cost VPS** - Production hosting
- **Nginx** - Reverse proxy
- **Let's Encrypt** - SSL certificates

## Folder Structure

```
WitchCityRope/
├── src/
│   └── WitchCityRope.Web/
│       ├── Features/
│       │   ├── Auth/
│       │   │   ├── Services/
│       │   │   ├── Models/
│       │   │   ├── Validators/
│       │   │   └── Pages/
│       │   ├── Events/
│       │   ├── Vetting/
│       │   ├── Payments/
│       │   ├── CheckIn/
│       │   └── Safety/
│       ├── Shared/
│       ├── Data/
│       ├── wwwroot/
│       └── Program.cs
├── tests/
├── docs/
└── docker-compose.yml
```

## Key Architectural Decisions (ADRs)

1. **ADR-001**: Blazor Server over WebAssembly - Better SEO, faster load
2. **ADR-002**: SQLite over PostgreSQL - Simpler deployment
3. **ADR-003**: Vertical Slice Architecture - Feature cohesion
4. **ADR-004**: Syncfusion UI Components - Professional UI
5. **ADR-005**: PayPal only payments - Simple integration
6. **ADR-011**: Direct Services over MediatR - Reduced complexity
7. **ADR-012**: SendGrid for Email - Reliable delivery
8. **ADR-013**: IMemoryCache for Caching - No Redis needed

## Service Pattern Example

```csharp
// Service Interface
public interface IEventService
{
    Task<Result<EventDto>> GetEventAsync(int id);
    Task<Result<EventDto>> CreateEventAsync(CreateEventRequest request);
}

// Service Implementation
public class EventService : IEventService
{
    private readonly WitchCityRopeDbContext _db;
    private readonly ICacheService _cache;
    private readonly IEmailService _email;
    
    // Direct dependency injection
    public EventService(
        WitchCityRopeDbContext db,
        ICacheService cache,
        IEmailService email)
    {
        _db = db;
        _cache = cache;
        _email = email;
    }
    
    public async Task<Result<EventDto>> GetEventAsync(int id)
    {
        // Try cache first
        var cached = await _cache.GetAsync<EventDto>($"event:{id}");
        if (cached != null)
            return Result<EventDto>.Success(cached);
            
        // Load from database
        var entity = await _db.Events.FindAsync(id);
        if (entity == null)
            return Result<EventDto>.Failure("Event not found");
            
        var dto = new EventDto(entity);
        await _cache.SetAsync($"event:{id}", dto);
        
        return Result<EventDto>.Success(dto);
    }
}

// Blazor Component Usage
@page "/events/{Id:int}"
@inject IEventService EventService

@code {
    [Parameter] public int Id { get; set; }
    
    protected override async Task OnInitializedAsync()
    {
        var result = await EventService.GetEventAsync(Id);
        // Handle result
    }
}
```

## Deployment Strategy

### Single Container Deployment
- One Docker container with everything
- SQLite database file mounted as volume
- Environment variables for configuration
- Nginx reverse proxy on host
- SSL via Let's Encrypt

### Production Configuration
- VPS hosting (DigitalOcean/Linode)
- 2GB RAM minimum
- Automated backups of SQLite file
- Health checks and monitoring
- Log aggregation with Serilog

## Cost Analysis

### Monthly Operational Costs
- **VPS**: $10-20
- **SendGrid**: $15 (after free tier)
- **Domain**: $1.25
- **SSL**: Free
- **Total**: ~$30-40/month

### One-Time/Annual Costs
- **Syncfusion License**: Already available
- **Domain Registration**: $15/year

## Development Workflow

1. **Feature Development**
   - Create feature folder
   - Implement service with business logic
   - Create Blazor pages/components
   - Add FluentValidation rules
   - Write unit tests

2. **Testing**
   - Unit tests for services
   - Component tests with bUnit
   - Integration tests with WebApplicationFactory
   - Manual testing in development

3. **Deployment**
   - Push to GitHub
   - GitHub Actions builds Docker image
   - Deploy to VPS
   - Run migrations if needed

## Next Steps

The architecture is fully documented and ready for implementation. The next phase is:

1. **Development Environment Setup**
   - Create .NET 9 project
   - Configure Syncfusion
   - Set up SQLite
   - Configure services

2. **MVP Implementation**
   - Sprint 1: Authentication
   - Sprint 2: Vetting System
   - Sprint 3: Event Management
   - Sprint 4: Payments & Check-in

## References

- **Technical Stack**: `/docs/architecture/technical-stack.md`
- **Caching & Email**: `/docs/architecture/CACHING-AND-EMAIL-STRATEGY.md`
- **Architecture Decisions**: `/docs/architecture/decisions.md`
- **Style Guide**: `/docs/design/FINAL-STYLE-GUIDE.md`
- **Progress Tracking**: `/docs/PROGRESS.md`

This architecture provides the optimal balance of simplicity, performance, and maintainability for a community platform serving 600 members.
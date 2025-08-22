# Claude Code Context - Witch City Rope

## Project Overview

Witch City Rope is a comprehensive membership and event management platform for a 600-member rope bondage educational community in Salem, MA. This is a safety-focused, consent-oriented educational organization with full-stack implementation complete.

## 🚀 Session Startup Checklist

**IMPORTANT: Complete these steps at the start of EVERY session:**

1. ✅ **Read Project Status**
   - Check `docs/PROGRESS.md` for latest updates
   - Review any recent changes or issues

2. ✅ **Start Visual Monitoring** (REQUIRED for UI work)
   ```powershell
   .\tools\Start-UIMonitoring.ps1 -OpenBrowser
   ```

3. ✅ **Verify MCP Tools Available**
   - Check that `puppeteer` and `mcp_browser_tools` are available
   - If not, restart Claude Desktop after checking MCP configuration

4. ✅ **Check Build Status**
   ```bash
   dotnet build
   ```

5. ✅ **Review Todo List**
   - Use TodoRead tool to check current tasks
   - Continue from where the last session ended

## Current Project Status

**Last Updated:** June 27, 2025  
**Development Phase:** COMPLETE - Full MVP implementation with comprehensive test coverage  
**Architecture:** Multi-project solution with Clean Architecture principles  
**Test Coverage:** >80% across all projects
**Visual Verification:** MCP tools configured and ready for UI testing

**ALWAYS CHECK `docs/PROGRESS.md` at session start for detailed status and history**

## Available Development Tools

### MCP Server Configuration (Claude Desktop)
When using Claude Desktop, you have access to powerful debugging and visual verification capabilities through the MCP (Model Context Protocol) server configuration:

```json
{
  "mcpServers": {
    "commands": {
      "command": "npx",
      "args": ["@modelcontextprotocol/server-commands"],
      "env": {
        "ALLOWED_COMMANDS": "curl,powershell"
      }
    },
    "puppeteer": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-puppeteer"]
    },
    "browser-tools": {
      "command": "npx",
      "args": ["browser-tools-mcp"]
    },
    "github": {
      "command": "npx",
      "args": ["@modelcontextprotocol/server-github"],
      "env": {
        "GITHUB_TOKEN": "your-github-token-here"
      }
    }
  }
}
```

#### 🎯 Visual Verification Tools (IMPORTANT - USE THESE!)

**ALWAYS use visual verification when making UI changes**. The project includes comprehensive MCP visual verification tools that should be used automatically:

1. **Start UI Monitoring** (Run at session start):
   ```powershell
   .\tools\Start-UIMonitoring.ps1 -OpenBrowser
   ```

2. **Capture Screenshots After Changes**:
   
   **Using Puppeteer MCP** (Primary tool for screenshots):
   ```javascript
   // Basic full page screenshot
   mcp_puppeteer.screenshot({
     url: "https://localhost:5652",
     fullPage: true
   })
   
   // Capture specific element
   mcp_puppeteer.screenshot({
     url: "https://localhost:5652/events",
     selector: ".event-list"
   })
   
   // With authentication
   mcp_puppeteer.screenshot({
     url: "https://localhost:5652/admin",
     cookies: [{
       name: "auth-token",
       value: "your-token-here",
       domain: "localhost"
     }]
   })
   ```

3. **Run Comprehensive Audits**:
   
   **Using browser-tools MCP** (For audits and debugging):
   ```javascript
   // Accessibility audit
   mcp_browser_tools.audit({
     url: "https://localhost:5652",
     type: "accessibility"
   })
   
   // Performance audit
   mcp_browser_tools.audit({
     url: "https://localhost:5652/events",
     type: "performance"
   })
   
   // SEO audit
   mcp_browser_tools.audit({
     url: "https://localhost:5652",
     type: "seo"
   })
   
   // Debug with console logs
   mcp_browser_tools.screenshot({
     url: "https://localhost:5652/problematic-page",
     includeLogs: true,
     includeNetwork: true
   })
   ```

4. **Test Responsive Design**:
   ```javascript
   // Mobile viewport
   mcp_browser_tools.screenshot({
     url: "https://localhost:5652",
     viewport: { width: 375, height: 667 }
   })
   
   // Tablet viewport
   mcp_browser_tools.screenshot({
     url: "https://localhost:5652",
     viewport: { width: 768, height: 1024 }
   })
   ```

**📚 Full Documentation**: See `/docs/MCP_VISUAL_VERIFICATION_SETUP.md`, `/docs/MCP_QUICK_REFERENCE.md`, and `/docs/UI_TESTING_WITH_MCP.md`

#### Command-Line Tools

**Available Commands:**
- `curl`: For testing API endpoints and external services
- `powershell`: For advanced Windows system operations and debugging

**Usage Examples:**
```bash
# Test API health endpoint
curl https://localhost:7001/api/v1/health

# Test authentication endpoint
curl -X POST https://localhost:7001/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test123!"}'

# Check running .NET processes
powershell -Command "Get-Process | Where-Object {$_.ProcessName -like '*dotnet*'}"

# Check if ports are in use
powershell -Command "Get-NetTCPConnection | Where-Object {$_.LocalPort -in @(5000,5001,7000,7001,5652)}"
```

**Common Debugging Scenarios:**
1. **Visual UI Verification**: Capture screenshots after any UI changes
2. **API Testing**: Use curl to test endpoints without needing Postman or browser
3. **Service Health Checks**: Verify API and Web services are responding
4. **Port Conflicts**: Check if required ports are available
5. **Process Management**: Monitor dotnet processes and resource usage
6. **External Service Testing**: Test SendGrid, PayPal endpoints

## Technology Stack

### Core Technologies
- **Frontend**: React 18 + TypeScript + Vite 6.0
- **Backend**: ASP.NET Core 9.0 Minimal API
- **Database**: PostgreSQL 17 with Entity Framework Core 9.0
- **UI Components**: Mantine v7 (open source)
- **Authentication**: JWT Bearer tokens + httpOnly cookies + Google OAuth 2.0
- **2FA**: TOTP-based with QR code generation
- **Payments**: PayPal Checkout SDK 1.1.0
- **Email**: SendGrid 9.29.3 for transactional and bulk emails
- **Caching**: Built-in IMemoryCache
- **Password Hashing**: BCrypt.Net-Next 4.0.3
- **Logging**: Serilog.AspNetCore 9.0.0
- **Testing**: xUnit 2.7.0, Moq 4.20.70, FluentAssertions 6.12.0, Bogus 35.4.0
- **Containerization**: Docker with Linux containers

### Project Structure
```
WitchCityRope/
├── apps/
│   ├── api/                         # ASP.NET Core Minimal API
│   └── web/                         # React + TypeScript frontend
├── packages/
│   └── shared-types/                # NSwag generated TypeScript types
├── src/
│   ├── WitchCityRope.Core/           # Domain entities, interfaces, DTOs
│   └── WitchCityRope.Infrastructure/ # Data access, external services
├── tests/
│   ├── WitchCityRope.Core.Tests/
│   ├── WitchCityRope.Infrastructure.Tests/
│   ├── WitchCityRope.Api.Tests/
│   ├── WitchCityRope.Web.Tests/
│   ├── WitchCityRope.IntegrationTests/
│   ├── WitchCityRope.E2E.Tests/
│   ├── WitchCityRope.PerformanceTests/
│   └── WitchCityRope.Tests.Common/
└── docs/
    ├── architecture/
    ├── design/
    └── testing/
```

## Architecture Overview

### Clean Architecture Layers
1. **Core Layer** (`WitchCityRope.Core`)
   - Domain entities and value objects
   - Business logic interfaces
   - DTOs and view models
   - Domain exceptions
   - No external dependencies

2. **Infrastructure Layer** (`WitchCityRope.Infrastructure`)
   - Entity Framework DbContext and configurations
   - Repository implementations
   - External service integrations (PayPal, SendGrid)
   - Authentication/Authorization implementation
   - Data migrations

3. **API Layer** (`WitchCityRope.Api`)
   - RESTful endpoints
   - API controllers
   - Request/Response models
   - API-specific middleware

4. **Web Layer** (`apps/web`)
   - React components with TypeScript
   - Vite build system
   - Mantine UI components
   - TanStack Query for data fetching
   - Zustand for state management
   - React Router v7 for navigation

### Key Design Patterns
- **Vertical Slice Architecture** for feature organization
- **Direct Service Pattern** (no MediatR)
- **Repository Pattern** for data access
- **Unit of Work** via EF Core DbContext
- **Dependency Injection** throughout
- **Options Pattern** for configuration

## Testing Infrastructure

### Test Categories
1. **Unit Tests** (300+)
   - Service layer logic
   - Domain entity behavior
   - Validation rules
   - Utility functions

2. **Integration Tests** (150+)
   - Database operations
   - External API calls
   - Email service
   - Authentication flows

3. **End-to-End Tests** (50+)
   - Complete user workflows
   - UI interaction tests
   - API endpoint tests
   - Security scenarios

4. **Performance Tests**
   - Load testing with k6
   - Database query optimization
   - Memory profiling
   - Concurrent user scenarios

### Testing Tools
- **Test Framework**: xUnit with parallel execution
- **Mocking**: Moq for dependency isolation
- **Assertions**: FluentAssertions for readable tests
- **Test Data**: Bogus for realistic fake data
- **Integration**: TestContainers for database tests
- **E2E**: WebApplicationFactory + Selenium
- **Coverage**: Coverlet with >80% threshold

## Important Files to Review

### Configuration Files
- `/src/WitchCityRope.Web/appsettings.json` - Application settings
- `/src/WitchCityRope.Web/Program.cs` - Service registration and middleware
- `/docker-compose.yml` - Container orchestration
- `/.github/workflows/test.yml` - CI/CD pipeline

### Core Domain
- `/src/WitchCityRope.Core/Entities/` - Domain models
- `/src/WitchCityRope.Core/Interfaces/` - Service contracts
- `/src/WitchCityRope.Core/DTOs/` - Data transfer objects
- `/src/WitchCityRope.Core/Enums/` - Domain enumerations

### Infrastructure
- `/src/WitchCityRope.Infrastructure/Data/WcrDbContext.cs` - EF Core context
- `/src/WitchCityRope.Infrastructure/Services/` - Service implementations
- `/src/WitchCityRope.Infrastructure/Migrations/` - Database migrations

### Features (Web)
- `/apps/web/src/pages/auth/` - Authentication pages
- `/apps/web/src/pages/events/` - Event management
- `/apps/web/src/pages/admin/` - Admin dashboard
- `/apps/api/Features/` - API endpoints organized by feature
- `/packages/shared-types/src/generated/` - Auto-generated TypeScript types

## Common Tasks

### Running the Application
```bash
# Start both API and Web with development containers
./dev.sh

# Or start services individually:
npm run dev          # Start React dev server (port 5173)
dotnet run --project apps/api  # Start API (port 5653)

# Or with Docker
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d
```

### Database Operations
```bash
# Create new migration
cd src/WitchCityRope.Infrastructure
dotnet ef migrations add MigrationName -s ../WitchCityRope.Web

# Update database
dotnet ef database update -s ../WitchCityRope.Web

# Remove last migration
dotnet ef migrations remove -s ../WitchCityRope.Web
```

### Running Tests
```bash
# All tests
dotnet test

# Specific project
dotnet test tests/WitchCityRope.Core.Tests

# With coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Specific category
dotnet test --filter "Category=Unit"
```

### Adding New Features
1. Create feature folder in appropriate project
2. Add domain entities to Core
3. Add repository interfaces to Core
4. Implement repositories in Infrastructure
5. Add service interfaces to Core
6. Implement services in Infrastructure or API
7. Add API endpoints in apps/api
8. Run NSwag generation for TypeScript types
9. Create React components using generated types
10. Write unit tests and E2E tests with Playwright
11. Update documentation

## Project Conventions

### Naming Conventions
- **Entities**: PascalCase singular (e.g., `Event`, `User`)
- **DTOs**: PascalCase with Dto suffix (e.g., `EventDto`)
- **Services**: Interface with I prefix (e.g., `IEventService`)
- **Repositories**: Interface with I prefix (e.g., `IEventRepository`)
- **Tables**: PascalCase plural (e.g., `Events`, `Users`)

### Code Organization
- One class per file
- Interfaces in Core project
- Implementations in Infrastructure/Web
- Tests mirror source structure
- Feature-based organization in Web project

### React Components
- Use functional components with TypeScript
- Use TanStack Query for data fetching
- Use Zustand for global state management
- Use Mantine v7 components for UI
- Handle errors with React Error Boundaries
- Use generated TypeScript types from @witchcityrope/shared-types

### Database Conventions
- Use Fluent API for configuration
- Soft deletes with `IsDeleted` flag
- Audit fields: `CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy`
- Indexes on foreign keys and commonly queried fields

## Core Business Rules

### Membership
- Minimum age: 21 years
- Vetting process required for member status
- Members can be suspended or banned
- No membership fees
- Scene names used publicly, legal names encrypted

### Events
- **Classes**: Public, $35-65, advance payment required
- **Meetups**: Members only, $0-35, pay at door allowed
- Capacity: Usually 60 people max
- Teachers don't count toward capacity
- Refunds allowed until 48-72 hours before event
- Waitlist management when at capacity

### Vetting Process
1. Application submission
2. Admin review
3. Interview scheduling (optional)
4. Approval/rejection decision
5. Email notification
6. Member badge issuance

### Payment Processing
- PayPal Checkout integration
- Sliding scale (honor system)
- Store transaction IDs
- IPN webhook handling
- Refund workflow support

## Security Considerations

### Authentication & Authorization
- JWT tokens with 24-hour expiration
- Google OAuth 2.0 integration
- Email/password with BCrypt hashing
- 2FA required for all accounts
- Role-based access (Admin, Staff, Member, Guest)

### Data Protection
- AES-256 encryption for legal names
- HTTPS enforced in production
- CORS configured for specific origins
- Input validation with FluentValidation
- SQL injection protection via EF Core
- XSS prevention in Blazor
- CSRF tokens for state-changing operations

### Privacy
- Legal names visible to admins only
- Anonymous incident reporting
- GDPR compliance considerations
- Data retention policies
- Right to deletion support

## Known Issues and Considerations

1. **Name-based check-in** - Manual search, no QR codes by design
2. **Honor system payments** - Trust-based sliding scale
3. **SQLite limitations** - Single-writer model, consider PostgreSQL for scale
4. **PostgreSQL setup** - Requires database server (auto-configured in development)
5. **Email throttling** - SendGrid rate limits apply
6. **File uploads** - Currently stored on disk, consider cloud storage
7. **No real-time updates** - Blazor Server but no SignalR hub yet

## Performance Optimizations

### Implemented
- Response caching for public pages
- Lazy loading for navigation properties
- Database query optimization with includes
- Minified CSS/JS assets
- Image lazy loading
- IMemoryCache for frequently accessed data

### Future Considerations
- Redis for distributed caching
- CDN for static assets
- Database read replicas
- Background job processing
- Event sourcing for audit trail

## Deployment Considerations

### Environment Variables
```bash
ConnectionStrings__DefaultConnection=Data Source=witchcityrope.db
JwtSettings__Secret=your-production-secret-min-32-chars
JwtSettings__Issuer=WitchCityRope
JwtSettings__Audience=WitchCityRopeUsers
SendGrid__ApiKey=your-sendgrid-api-key
PayPal__ClientId=your-paypal-client-id
PayPal__ClientSecret=your-paypal-client-secret
PayPal__Mode=live
# Syncfusion license no longer required - using Mantine v7 (open source)
ASPNETCORE_ENVIRONMENT=Production
```

### Production Checklist
- [ ] Update connection strings
- [ ] Set secure JWT secret
- [ ] Configure PayPal for production
- [ ] Set up SSL certificates
- [ ] Configure backup strategy
- [ ] Set up monitoring (e.g., Application Insights)
- [ ] Configure error tracking (e.g., Sentry)
- [ ] Review and update CORS policies
- [ ] Enable production logging
- [ ] Database migrations applied
- [ ] Health checks configured

## Next Steps (Phase 6-7)

### Pre-Production (Phase 6)
- Security audit and penetration testing
- Performance optimization
- User documentation
- Legal compliance (privacy policy, ToS)

### Production Deployment (Phase 7)
- VPS provisioning and setup
- Domain and SSL configuration
- Monitoring and alerting
- Beta testing
- Launch preparation

## Getting Help

1. **Architecture Questions**: Review `/docs/architecture/decisions/`
2. **Design/UX**: Check `/docs/design/wireframes/`
3. **Business Logic**: See requirements in `/docs/design/requirements.md`
4. **Test Examples**: Look in `/tests/` for patterns
5. **Configuration**: Check `appsettings.json` and environment variables

## Development Tips

1. **Always run tests before committing**
2. **Use feature branches for new work**
3. **Keep commits focused and atomic**
4. **Update tests when changing code**
5. **Document breaking changes**
6. **Use Mantine v7 components consistently**
7. **Follow existing patterns for consistency**
8. **Check logs for detailed error information**
9. **Use the debugger - Blazor Server supports it well**
10. **Review PROGRESS.md for context on recent changes**

---

**Remember**: This is a real project for a real community. Code quality, security, and user experience are paramount. When in doubt, ask for clarification rather than making assumptions.
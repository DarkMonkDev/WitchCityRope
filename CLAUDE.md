# Claude Code Project Configuration

## Project Overview
WitchCityRope is a Blazor Server application for a rope bondage community in Salem, offering workshops, performances, and social events.

## GitHub Repository
- **Repository**: https://github.com/DarkMonkDev/WitchCityRope.git
- **Authentication**: Personal Access Token (PAT) is configured in ~/.git-credentials
- **Git Config**: Credential helper is set to 'store' for persistent authentication

## Development Guidelines

### Build Commands
```bash
# Build the project
dotnet build

# Run tests
dotnet test

# Run the web application
dotnet run --project src/WitchCityRope.Web

# Check for linting/formatting issues
dotnet format --verify-no-changes
```

### Project Structure
- `/src/WitchCityRope.Core` - Core domain logic
- `/src/WitchCityRope.Infrastructure` - Data access and external services
- `/src/WitchCityRope.Api` - API endpoints
- `/src/WitchCityRope.Web` - Blazor Server UI
- `/tests` - Unit and integration tests

### Key Technologies
- ASP.NET Core 9.0
- Blazor Server
- Syncfusion Blazor Components
- Entity Framework Core
- SQL Server

### Important Notes
1. Always run `dotnet build` after making changes to check for compilation errors
2. The project uses nullable reference types - be mindful of null checks
3. Syncfusion components require proper namespace imports
4. CSS escape sequences in Razor files need @@ (e.g., @@keyframes, @@media)

### Performance Optimizations Implemented
- Response compression (Brotli + Gzip)
- Static file caching with 1-year cache headers
- CSS/JS minification
- Google Fonts optimization
- SignalR circuit optimization

### Admin Features
- Dashboard with metrics and charts
- User management interface
- Financial reports with export functionality
- Incident management system
- Event management
- Vetting queue for member approvals

## Session Context
This project has been actively developed with the following completed:
- Performance optimizations achieving 70% reduction
- Complete admin portal implementation
- UI testing and fixes against wireframes
- Compilation error fixes (resolved all 9 errors)

For any GitHub operations, the credentials are already configured and will work automatically.
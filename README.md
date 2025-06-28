# WitchCityRope

[![Test Pipeline](https://github.com/yourusername/WitchCityRope/actions/workflows/test.yml/badge.svg)](https://github.com/yourusername/WitchCityRope/actions/workflows/test.yml)
[![Build Status](https://dev.azure.com/yourusername/WitchCityRope/_apis/build/status/WitchCityRope?branchName=main)](https://dev.azure.com/yourusername/WitchCityRope/_build/latest?definitionId=1&branchName=main)
[![codecov](https://codecov.io/gh/yourusername/WitchCityRope/branch/main/graph/badge.svg?token=YOUR_TOKEN)](https://codecov.io/gh/yourusername/WitchCityRope)
[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=.net)](https://dotnet.microsoft.com/)
[![Blazor](https://img.shields.io/badge/Blazor-Server-512BD4?logo=blazor)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![pre-commit](https://img.shields.io/badge/pre--commit-enabled-brightgreen?logo=pre-commit)](https://github.com/pre-commit/pre-commit)

A comprehensive event management and community platform for the rope bondage community in Salem, MA. Built with ASP.NET Core 9.0 and Blazor Server, WitchCityRope provides a secure, user-friendly platform for managing events, member vetting, and community safety.

## Project Status

**Current Phase:** Pre-production  
**Development Status:** Feature Complete with Comprehensive Testing  
**Test Coverage:** >80%  
**Implementation:** 100% MVP features completed

## Key Features

- **Secure Authentication**: Google OAuth and email/password with 2FA support
- **Member Vetting**: Multi-step application process with admin review workflow  
- **Event Management**: Public classes and member-only meetups with capacity management
- **Payment Processing**: PayPal integration with sliding scale pricing
- **Mobile Check-in**: Staff-optimized interface for event day operations
- **Safety Tools**: Anonymous incident reporting and emergency contact management
- **Admin Dashboard**: Comprehensive analytics and management tools

## Quick Start

Get up and running with WitchCityRope in minutes. See our [Quick Start Guide](docs/QUICK_START.md) for detailed instructions.

```bash
# Clone the repository
git clone https://github.com/yourusername/WitchCityRope.git
cd WitchCityRope

# Set up configuration
cd src/WitchCityRope.Web
cp appsettings.json appsettings.Development.json

# Run the application
dotnet run

# Access at https://localhost:5652
```

## Technology Stack

### Core Technologies
- **Framework**: ASP.NET Core 9.0 with Blazor Server
- **Database**: SQLite with Entity Framework Core 9.0
- **UI Components**: Syncfusion Blazor Components (commercial license)

### Infrastructure & Services
- **Authentication**: JWT Bearer tokens with Google OAuth integration
- **Payments**: PayPal Checkout SDK with IPN handling
- **Email**: SendGrid for transactional emails and campaigns
- **Caching**: Built-in IMemoryCache for performance optimization
- **File Storage**: Local file system with secure access controls

### Development & Testing
- **Testing Frameworks**: xUnit, Moq, FluentAssertions, bUnit
- **Test Data**: Bogus for realistic test data generation
- **Integration Testing**: TestContainers, WebApplicationFactory
- **Performance Testing**: BenchmarkDotNet, k6 load testing
- **CI/CD**: GitHub Actions, Azure DevOps, GitLab CI
- **Containerization**: Docker with single-container deployment

## Architecture Overview

WitchCityRope follows a simplified vertical slice architecture optimized for rapid development and maintainability:

```
src/WitchCityRope.Web/
├── Features/           # Vertical slices by business capability
│   ├── Auth/          # Authentication & authorization
│   ├── Events/        # Event management
│   ├── Vetting/       # Member vetting workflow
│   ├── Payments/      # Payment processing
│   ├── CheckIn/       # Event check-in system
│   └── Safety/        # Incident reporting
├── Shared/            # Cross-cutting concerns
├── Data/              # Database context & migrations
└── wwwroot/           # Static assets & CSS
```

Each feature slice contains:
- **Services**: Business logic with direct service pattern
- **Models**: DTOs and request/response models
- **Validators**: FluentValidation rules
- **Pages**: Blazor components and pages

## Documentation

- **[Quick Start Guide](docs/QUICK_START.md)** - Get up and running quickly
- **[Development Roadmap](ROADMAP.md)** - Project phases and future plans
- **[Architecture Documentation](docs/architecture/)** - Technical decisions and patterns
- **[Design System](docs/design/FINAL-STYLE-GUIDE.md)** - UI/UX guidelines and components
- **[API Documentation](docs/api/)** - Service endpoints and integration guides
- **[Testing Guide](docs/testing/)** - Test strategy and coverage reports
- **[Claude AI Guide](docs/CLAUDE.md)** - AI assistant startup guide and conventions
- **[MCP Debugging Guide](docs/MCP_DEBUGGING_GUIDE.md)** - Debugging with Claude Desktop tools

## Screenshots

### Landing Page
The public-facing landing page showcases upcoming events and community information with a sophisticated dark theme featuring burgundy and plum accents.

### Member Dashboard
Members have access to a personalized dashboard showing their upcoming events, vetting status, and quick actions for common tasks.

### Event Management
Admin users can create and manage both public classes and member-only meetups with comprehensive scheduling and capacity controls.

### Mobile Check-in
The mobile-optimized check-in interface allows event staff to quickly process attendees with name-based search and waiver tracking.

## Security Features

- **Password Security**: BCrypt hashing with work factor 12
- **Multi-Factor Authentication**: TOTP-based 2FA with backup codes
- **Session Management**: Secure JWT tokens with 24-hour expiration
- **Data Encryption**: AES-256 encryption for sensitive personal data
- **Access Control**: Role-based permissions (Guest, Member, Staff, Admin)
- **HTTPS Enforcement**: Automatic redirect and HSTS in production
- **CSRF Protection**: Anti-forgery tokens on all forms
- **Rate Limiting**: DDoS protection on authentication endpoints
- **Input Validation**: FluentValidation with XSS prevention
- **SQL Injection Protection**: Parameterized queries via Entity Framework

## Performance

- **Page Load Times**: <2 seconds for all pages
- **Concurrent Users**: Tested with 500+ simultaneous users
- **Database Queries**: Optimized with proper indexing
- **Caching Strategy**: IMemoryCache for frequently accessed data
- **Static Assets**: Minified and bundled for production
- **Image Optimization**: Lazy loading with responsive sizing

## Getting Started

For detailed setup instructions, see our [Quick Start Guide](docs/QUICK_START.md).

### Prerequisites

- .NET 9.0 SDK or later
- Visual Studio 2022 or VS Code
- Syncfusion license key (free community license available)
- API keys for SendGrid and PayPal (sandbox for development)

### Quick Setup

```bash
# Clone and navigate to project
git clone https://github.com/yourusername/WitchCityRope.git
cd WitchCityRope

# Set up configuration
cd src/WitchCityRope.Web
cp appsettings.json appsettings.Development.json
# Edit appsettings.Development.json with your API keys

# Create database and run
dotnet ef database update
dotnet run
```

## Deployment Options

### Docker Deployment
```bash
docker build -t witchcityrope .
docker run -p 80:80 -p 443:443 witchcityrope
```

### VPS Deployment
The application is optimized for single-server deployment on a low-cost VPS. See our [deployment guide](docs/deployment/) for detailed instructions.

## Contributing

We welcome contributions! Please see our [Contributing Guidelines](CONTRIBUTING.md) for details.

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## Support

- **Documentation**: Check our [comprehensive docs](docs/)
- **Issues**: Report bugs via [GitHub Issues](https://github.com/yourusername/WitchCityRope/issues)
- **Discussions**: Join our [GitHub Discussions](https://github.com/yourusername/WitchCityRope/discussions)

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- The Salem rope bondage community for inspiration and requirements
- [Syncfusion](https://www.syncfusion.com/) for excellent Blazor components
- [SendGrid](https://sendgrid.com/) for reliable email delivery
- All contributors and open source projects that made this possible
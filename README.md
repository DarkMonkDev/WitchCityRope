# WitchCityRope

A comprehensive membership and event management platform for Salem's rope bondage educational community.

## 🚀 Project Status

**MVP Complete** - The application is feature-complete and production-ready with:
- Public event browsing and registration
- Member authentication and profiles  
- Staff vetting system
- Complete admin portal
- Optimized performance
- Professional UI/UX throughout

## 🛠️ Tech Stack

- **Framework**: ASP.NET Core 9.0 with Blazor Server
- **Database**: SQLite with Entity Framework Core
- **UI Components**: Syncfusion Blazor
- **Authentication**: JWT + Google OAuth 2.0
- **Payments**: PayPal Checkout SDK
- **Email**: SendGrid

## 📁 Project Structure

```
WitchCityRope/
├── src/
│   ├── WitchCityRope.Core/           # Domain entities, interfaces
│   ├── WitchCityRope.Infrastructure/ # Data access, external services
│   ├── WitchCityRope.Api/            # REST API endpoints
│   └── WitchCityRope.Web/            # Blazor Server UI
├── tests/                            # Comprehensive test suite
├── docs/                             # Documentation
└── deployment/                       # Deployment scripts
```

## 🚀 Getting Started

### Prerequisites
- .NET 9.0 SDK
- SQLite
- Syncfusion license key

### Quick Start
```bash
# Clone the repository
git clone https://github.com/DarkMonkDev/WitchCityRope.git
cd WitchCityRope

# Set up environment variables
cp .env.example .env
# Edit .env with your configuration

# Run the application
cd src/WitchCityRope.Web
dotnet run
```

Visit https://localhost:5652

## 📚 Documentation

- [Project Progress](/docs/PROGRESS.md) - Detailed development history
- [Architecture](/docs/architecture/) - Technical decisions and patterns
- [Admin Guide](/docs/admin-guide/) - Administration documentation
- [User Guide](/docs/user-guide/) - End-user documentation

## ✨ Features

### Public Features
- Event browsing and registration
- Google OAuth authentication
- Sliding scale payment options

### Member Features
- Personal dashboard
- Event ticket management
- Profile customization
- Two-factor authentication

### Admin Features
- Comprehensive dashboard with analytics
- User management system
- Financial reporting
- Incident management
- Event administration
- Vetting queue management

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true
```

## 🚀 Deployment

See [Deployment Guide](/deployment/README.md) for detailed instructions.

## 🔒 Security

- OWASP compliance
- HTTPS enforced
- CSRF protection
- XSS prevention
- SQL injection protection
- Rate limiting

## 📄 License

This project is private and proprietary.

## 👥 Contributing

This is a private project. Please contact the repository owner for contribution guidelines.

---

Built with ❤️ for the Salem rope bondage community
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

### Recent Updates
- Enhanced development environment with WSL2 support
- Integrated 8 MCP servers for AI-assisted development
- Added comprehensive status checking and debugging tools
- Improved documentation and developer experience

## 🛠️ Tech Stack

- **Framework**: ASP.NET Core 9.0 with Blazor Server
- **Database**: PostgreSQL with Entity Framework Core
- **UI Components**: Syncfusion Blazor
- **Authentication**: JWT + Google OAuth 2.0
- **Payments**: PayPal Checkout SDK
- **Email**: SendGrid
- **Development Tools**: MCP (Model Context Protocol) Servers
- **Environment**: WSL2 (Windows Subsystem for Linux)

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
- Docker Desktop (includes Docker Compose)
- Syncfusion license key
- Node.js 16+ (for MCP servers)
- WSL2 (recommended for Windows users)

### Quick Start

#### Option 1: Docker Compose (Recommended)
```bash
# Clone the repository
git clone https://github.com/DarkMonkDev/WitchCityRope.git
cd WitchCityRope

# Set up environment variables
cp .env.example .env
# Edit .env with your configuration

# Start all services with Docker
docker-compose up -d

# View logs
docker-compose logs -f
```

Visit https://localhost:5652

#### Option 2: Local Development
```bash
# Ensure PostgreSQL is running (Docker or local)
# Update connection string in appsettings.json if needed

# Run database migrations
cd src/WitchCityRope.Infrastructure
dotnet ef database update
cd ../..

# Run the application
cd src/WitchCityRope.Web
dotnet run
```

Visit https://localhost:8281

### Quick Developer Commands
```bash
# Run tests
dotnet test

# Build the solution
dotnet build

# Run with hot reload
dotnet watch run --project src/WitchCityRope.Web

# Check MCP server status
./check-mcp-status.sh

# Start browser automation server
cd src/mcp-servers/browser-server-persistent && ./browser-server-manager.sh start

# View logs
tail -f src/WitchCityRope.Web/logs/*.log
```

## 💻 Development Environment

### WSL2 Setup
This project is optimized for development in WSL2 (Windows Subsystem for Linux), providing:
- Native Linux performance on Windows
- Seamless file system integration
- Docker Desktop integration
- Access to both Windows and Linux tools

#### Setting up WSL2:
1. Enable WSL2 on Windows:
   ```powershell
   wsl --install
   ```
2. Install Ubuntu or preferred Linux distribution from Microsoft Store
3. Install Docker Desktop and enable WSL2 integration
4. Clone the repository inside WSL2 for best performance

### MCP Servers
The project includes 8 configured MCP (Model Context Protocol) servers for enhanced AI-assisted development:

1. **FileSystem MCP** - File operations across Windows drives
   - Access to repos, documents, downloads, and desktop
   - Seamless WSL2/Windows file integration

2. **Memory MCP** - Persistent context storage
   - Maintains conversation context across sessions
   - Stores in `%APPDATA%\Claude\memory-data\`

3. **Docker MCP** - Container management
   - Create, manage, and monitor Docker containers
   - Requires Docker Desktop running

4. **Commands MCP** - System command execution
   - Supports curl and PowerShell commands
   - Secure command execution environment

5. **GitHub MCP** - GitHub integration
   - Repository management
   - Issues and pull request operations
   - Requires GitHub CLI authentication

6. **PostgreSQL MCP** - Database operations
   - Direct database access and management
   - Configured for local development database

7. **Browser Tools MCP** - Web automation
   - Browser control and web scraping
   - Requires browser server running

8. **Stagehand MCP** - Advanced browser automation
   - AI-powered web interaction
   - Requires Chrome with debug port enabled

### Checking MCP Status
Run the status checker to verify all servers are working:
```bash
./check-mcp-status.sh
```

This script provides:
- Environment verification
- Individual server status
- Common issue detection
- Quick fix suggestions

## 📚 Documentation

- [Project Progress](/docs/PROGRESS.md) - Detailed development history
- [Architecture](/docs/architecture/) - Technical decisions and patterns
- [Admin Guide](/docs/admin-guide/) - Administration documentation
- [User Guide](/docs/user-guide/) - End-user documentation
- [MCP Servers Guide](/docs/MCP_SERVERS.md) - Detailed MCP configuration
- [MCP Debugging](/docs/MCP_DEBUGGING_GUIDE.md) - Troubleshooting MCP issues

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

# Run specific test project
dotnet test tests/WitchCityRope.Core.Tests

# Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run integration tests only
dotnet test --filter Category=Integration

# Run tests in watch mode
dotnet watch test --project tests/WitchCityRope.Core.Tests
```

## 🚀 Deployment

### Production Deployment
See [Deployment Guide](/deployment/README.md) for detailed instructions.

### Quick Deploy Commands
```bash
# Build and deploy with Docker Compose
docker-compose up -d --build

# Run database migrations
docker-compose exec api dotnet ef database update

# Check deployment health
curl https://localhost:5652/health
curl https://localhost:5654/health

# View logs
docker-compose logs -f
```

## 🔧 Troubleshooting

### Common Issues

1. **MCP Servers Not Working**
   ```bash
   # Run the status checker
   ./check-mcp-status.sh
   ```

2. **Database Connection Issues**
   ```bash
   # Check PostgreSQL container
   docker-compose ps postgres
   docker-compose logs postgres
   
   # Test connection
   docker-compose exec postgres pg_isready -U postgres
   
   # Run migrations
   docker-compose exec api dotnet ef database update
   ```

3. **Syncfusion License Issues**
   - Ensure `SYNCFUSION_LICENSE_KEY` is set in environment
   - Check license expiration date
   - Verify license is for correct version

4. **WSL2 Performance Issues**
   - Store code in WSL2 filesystem, not Windows drives
   - Use `\\wsl$\Ubuntu\home\user\` path from Windows
   - Disable Windows Defender real-time scanning for WSL directories

## 🔒 Security

- OWASP compliance
- HTTPS enforced
- CSRF protection
- XSS prevention
- SQL injection protection
- Rate limiting

## 📖 Resources

### Development Tools
- [Claude Desktop](https://claude.ai/download) - AI development assistant
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) - Container platform
- [WSL2 Documentation](https://docs.microsoft.com/en-us/windows/wsl/) - Windows Subsystem for Linux
- [Syncfusion Blazor](https://www.syncfusion.com/blazor-components) - UI component library

### Project Documentation
- [MCP Server Documentation](src/mcp-servers/README.md) - Detailed MCP setup
- [API Documentation](docs/api/) - REST API endpoints
- [Database Schema](docs/database/) - Entity relationships

### Useful Commands Reference
```bash
# Quick health check
./check-mcp-status.sh && dotnet test && echo "All systems operational!"

# Full rebuild
dotnet clean && dotnet restore && dotnet build

# Database reset (Docker)
docker-compose down -v && docker-compose up -d
docker-compose exec api dotnet ef database update
```

## 📄 License

This project is private and proprietary.

## 👥 Contributing

This is a private project. Please contact the repository owner for contribution guidelines.

---

Built with ❤️ for the Salem rope bondage community
# Claude Code Project Configuration

## Project Overview
WitchCityRope is a Blazor Server application for a rope bondage community in Salem, offering workshops, performances, and social events.

## GitHub Repository
- **Repository**: https://github.com/DarkMonkDev/WitchCityRope.git
- **Authentication**: Personal Access Token (PAT) is configured in ~/.git-credentials
- **Git Config**: Credential helper is set to 'store' for persistent authentication

## Database Configuration

### PostgreSQL Authentication
- **Default Password**: WitchCity2024!
- **Username**: postgres
- **Database Name**: witchcityrope_db

### Docker Compose PostgreSQL Configuration
When using Docker Compose:
- **Container Name**: `witchcityrope-db`
- **Port**: 5433
- **Volume**: `witchcityrope_postgres_dev_data`
- **Password**: WitchCity2024!
- **User**: postgres
- **Database**: witchcityrope_db

### Connection String Configuration
- **Docker Compose (Internal)**: `Host=postgres;Port=5432;Database=witchcityrope_db;Username=postgres;Password=WitchCity2024!`
- **Docker Compose (External)**: `Host=localhost;Port=5433;Database=witchcityrope_db;Username=postgres;Password=WitchCity2024!`
- **Local PostgreSQL**: `Host=localhost;Port=5432;Database=witchcityrope_db;Username=postgres;Password=WitchCity2024!`

## Development Guidelines

### Build Commands

#### Local Development (without Docker)
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

#### Docker Development
```bash
# Build and start all containers
docker-compose up -d --build

# Run tests in container
docker-compose exec web dotnet test

# Run database migrations
docker-compose exec web dotnet ef database update

# View application logs
docker-compose logs -f web

# Stop all containers
docker-compose down
```

#### Production Build
```bash
# Build production Docker image
docker build -t witchcityrope:latest -f Dockerfile --target final .

# Run production containers
docker-compose -f docker-compose.prod.yml up -d
```

### Important: Clean Up After Testing

**CRITICAL**: Any processes started during MCP testing MUST be properly shut down when you're done! Failure to do this will cause port conflicts when trying to run the application from Visual Studio.

#### Port Configuration

The application uses the following ports:

**Direct Launch (Visual Studio/dotnet run)**:
- API: 8180 (HTTP), 8181 (HTTPS) 
- Web: 8280 (HTTP), 8281 (HTTPS)

**Docker Compose**:
- Web: 5000 (HTTP), 5001 (HTTPS) - mapped from container's 8080/8443
- Database: 5433 - mapped from container's 5432

#### Common Processes to Clean Up

1. **dotnet run processes** (ports 5000, 5001, 7125, etc.):
   ```bash
   # Check for running dotnet processes
   ps aux | grep dotnet
   
   # Kill specific dotnet process by PID
   kill -9 <PID>
   
   # Kill all dotnet processes (use with caution)
   pkill -f dotnet
   ```

2. **Chrome DevTools** (port 9222):
   ```bash
   # From Windows PowerShell/CMD (NOT WSL)
   netstat -ano | findstr :9222
   taskkill /PID <PID> /F
   
   # Or close ALL Chrome instances
   taskkill /IM chrome.exe /F
   ```

3. **Docker containers**:
   ```bash
   # Stop all project containers
   docker-compose down
   
   # Check for any remaining containers
   docker ps
   
   # Force stop specific container
   docker stop <container_name>
   ```

4. **PostgreSQL** (port 5432):
   ```bash
   # Check if PostgreSQL is using the port
   netstat -an | grep 5432
   
   # Stop PostgreSQL service (if running locally)
   sudo service postgresql stop
   ```

#### Port Conflict Resolution

If Visual Studio reports "port already in use" errors:

1. **Find what's using the port**:
   ```bash
   # From WSL
   sudo lsof -i :5000    # Replace 5000 with the conflicted port
   
   # From Windows PowerShell
   netstat -ano | findstr :5000
   ```

2. **Kill the process**:
   ```bash
   # From WSL (using PID from lsof)
   kill -9 <PID>
   
   # From Windows (using PID from netstat)
   taskkill /PID <PID> /F
   ```

3. **Common ports used by the application**:
   - `5000` - HTTP development server
   - `5001` - HTTPS development server  
   - `7125` - Alternative HTTPS port
   - `9222` - Chrome DevTools debugging
   - `5432` - PostgreSQL database

#### Best Practices

1. **Always clean up after MCP testing sessions**
2. **Use `docker-compose down` instead of just `stop`** to fully clean up
3. **Close Chrome DevTools sessions** when done with browser testing
4. **Check for orphaned processes** before starting Visual Studio
5. **Document any persistent services** you're running for the project

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
- SQL Server (migrating to PostgreSQL)
- Docker & Docker Compose

## Docker Deployment Architecture

### Overview
WitchCityRope is containerized using Docker for both development and production environments. The application uses a multi-container architecture with separate containers for the web application and database, orchestrated using Docker Compose.

### Container Architecture

#### 1. Application Container (`witchcityrope-web`)
- **Base Image**: `mcr.microsoft.com/dotnet/aspnet:9.0` (runtime)
- **Build Image**: `mcr.microsoft.com/dotnet/sdk:9.0` (for multi-stage builds)
- **Exposed Ports**: 
  - `8080` (HTTP) - Internal container port
  - `8443` (HTTPS) - Internal container port for SSL
- **Health Check**: HTTP endpoint at `/health`
- **Working Directory**: `/app`

#### 2. Database Container (`witchcityrope-db`)
- **Image**: `postgres:16-alpine` (lightweight PostgreSQL)
- **Exposed Ports**: 
  - `5432` - PostgreSQL default port
- **Volumes**: 
  - `postgres_data:/var/lib/postgresql/data` - Data persistence
  - `./db/init:/docker-entrypoint-initdb.d` - Initialization scripts
- **Health Check**: `pg_isready` command

### Container Networking

#### Development Environment
- **Network**: `witchcityrope-network` (bridge network)
- **Service Discovery**: Containers communicate using service names
- **Connection String**: `Host=witchcityrope-db;Port=5432;Database=witchcityrope;Username=postgres;Password=${POSTGRES_PASSWORD}`

#### Production Environment
- **Network Isolation**: Each service in separate network segments
- **Reverse Proxy**: Nginx or Traefik for SSL termination and load balancing
- **External Access**: Only through reverse proxy on ports 80/443

### Volume Management

#### Persistent Volumes
1. **Database Data**: 
   - Volume: `postgres_data`
   - Mount: `/var/lib/postgresql/data`
   - Purpose: Persist database between container restarts

2. **Application Logs**:
   - Volume: `app_logs`
   - Mount: `/app/logs`
   - Purpose: Persist application logs for debugging

3. **Upload Storage**:
   - Volume: `uploads`
   - Mount: `/app/wwwroot/uploads`
   - Purpose: Persist user-uploaded files

#### Bind Mounts (Development Only)
- `./src:/src:cached` - Source code for hot reload
- `./appsettings.Development.json:/app/appsettings.Development.json` - Dev config

### Environment Configuration

#### Environment Variables
```yaml
# Application Container
- ASPNETCORE_ENVIRONMENT=Development|Staging|Production
- ASPNETCORE_URLS=http://+:8080;https://+:8443
- ConnectionStrings__DefaultConnection=${DB_CONNECTION_STRING}
- Syncfusion__LicenseKey=${SYNCFUSION_LICENSE}
- Email__SmtpServer=${SMTP_SERVER}
- Email__SmtpPort=${SMTP_PORT}
- Email__Username=${SMTP_USERNAME}
- Email__Password=${SMTP_PASSWORD}

# Database Container
- POSTGRES_USER=postgres
- POSTGRES_PASSWORD=${POSTGRES_PASSWORD}
- POSTGRES_DB=witchcityrope
- PGDATA=/var/lib/postgresql/data/pgdata
```

### Docker Compose Configuration

#### Development (`docker-compose.yml`)
```yaml
version: '3.8'

services:
  web:
    build:
      context: .
      dockerfile: Dockerfile
      target: development
    container_name: witchcityrope-web
    ports:
      - "5000:8080"
      - "5001:8443"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Host=db;Port=5432;Database=witchcityrope;Username=postgres;Password=${POSTGRES_PASSWORD}
    depends_on:
      db:
        condition: service_healthy
    volumes:
      - ./src:/src:cached
      - app_logs:/app/logs
    networks:
      - witchcityrope-network

  db:
    image: postgres:16-alpine
    container_name: witchcityrope-db
    ports:
      - "5432:5432"
    environment:
      - POSTGRES_PASSWORD=${POSTGRES_PASSWORD}
      - POSTGRES_DB=witchcityrope
    volumes:
      - postgres_data:/var/lib/postgresql/data
      - ./db/init:/docker-entrypoint-initdb.d
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 10s
      timeout: 5s
      retries: 5
    networks:
      - witchcityrope-network

volumes:
  postgres_data:
  app_logs:

networks:
  witchcityrope-network:
    driver: bridge
```

#### Production (`docker-compose.prod.yml`)
```yaml
version: '3.8'

services:
  web:
    image: witchcityrope:latest
    container_name: witchcityrope-web
    restart: unless-stopped
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=${DB_CONNECTION_STRING}
    depends_on:
      - db
    volumes:
      - app_logs:/app/logs
      - uploads:/app/wwwroot/uploads
    networks:
      - backend
      - frontend

  db:
    image: postgres:16-alpine
    container_name: witchcityrope-db
    restart: unless-stopped
    environment:
      - POSTGRES_PASSWORD=${POSTGRES_PASSWORD}
      - POSTGRES_DB=witchcityrope
    volumes:
      - postgres_data:/var/lib/postgresql/data
    networks:
      - backend

  nginx:
    image: nginx:alpine
    container_name: witchcityrope-proxy
    restart: unless-stopped
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx/nginx.conf:/etc/nginx/nginx.conf
      - ./nginx/ssl:/etc/nginx/ssl
    depends_on:
      - web
    networks:
      - frontend

volumes:
  postgres_data:
  app_logs:
  uploads:

networks:
  backend:
    internal: true
  frontend:
```

### Dockerfile (Multi-stage Build)
```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/WitchCityRope.Web/WitchCityRope.Web.csproj", "WitchCityRope.Web/"]
COPY ["src/WitchCityRope.Core/WitchCityRope.Core.csproj", "WitchCityRope.Core/"]
COPY ["src/WitchCityRope.Infrastructure/WitchCityRope.Infrastructure.csproj", "WitchCityRope.Infrastructure/"]
RUN dotnet restore "WitchCityRope.Web/WitchCityRope.Web.csproj"
COPY src/ .
WORKDIR "/src/WitchCityRope.Web"
RUN dotnet build "WitchCityRope.Web.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "WitchCityRope.Web.csproj" -c Release -o /app/publish

# Development stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS development
WORKDIR /app
EXPOSE 8080 8443
ENV ASPNETCORE_ENVIRONMENT=Development
ENV DOTNET_USE_POLLING_FILE_WATCHER=true
ENTRYPOINT ["dotnet", "watch", "run", "--project", "/src/WitchCityRope.Web/WitchCityRope.Web.csproj"]

# Final production stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080 8443
COPY --from=publish /app/publish .
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "WitchCityRope.Web.dll"]
```

### Development Workflow

#### Getting Started
```bash
# Clone the repository
git clone https://github.com/DarkMonkDev/WitchCityRope.git
cd WitchCityRope

# Create .env file for environment variables
cp .env.example .env

# Build and start containers
docker-compose up -d

# View logs
docker-compose logs -f

# Run database migrations
docker-compose exec web dotnet ef database update

# Stop containers
docker-compose down
```

#### Hot Reload Development
- Source code is mounted as a volume in development
- Changes to .cs files trigger automatic rebuild
- Changes to .razor files reflect immediately
- Database changes persist across container restarts

### Production Deployment

#### Build and Deploy
```bash
# Build production image
docker build -t witchcityrope:latest -f Dockerfile --target final .

# Tag for registry
docker tag witchcityrope:latest registry.example.com/witchcityrope:latest

# Push to registry
docker push registry.example.com/witchcityrope:latest

# Deploy using production compose
docker-compose -f docker-compose.prod.yml up -d
```

#### Database Migrations
```bash
# Run migrations in production
docker-compose -f docker-compose.prod.yml exec web dotnet ef database update

# Backup database
docker-compose -f docker-compose.prod.yml exec db pg_dump -U postgres witchcityrope > backup.sql
```

### Container Health Monitoring

#### Health Checks
- **Web App**: HTTP endpoint at `/health` returns 200 OK
- **Database**: PostgreSQL `pg_isready` command
- **Container Restart Policy**: `unless-stopped` in production

#### Logging
- Application logs persisted to volume
- Structured logging with Serilog
- Log aggregation with ELK stack (optional)

### Security Considerations

1. **Secrets Management**:
   - Never commit `.env` files
   - Use Docker secrets in Swarm mode
   - Consider HashiCorp Vault for production

2. **Network Security**:
   - Database not exposed externally in production
   - Internal networks for service communication
   - SSL termination at reverse proxy

3. **Image Security**:
   - Regular base image updates
   - Vulnerability scanning with Trivy
   - Non-root user in containers

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

## Quick Reference - Important Files

### Progress Tracking
- **PROGRESS.md** - Development progress and completed features
- **TODO.md** - Current task list and upcoming work

### Documentation
- **/docs/errors/** - Error documentation and troubleshooting guides
- **/docs/MCP_SERVERS.md** - MCP (Model Context Protocol) servers documentation

### Testing
- **/tests/WitchCityRope.IntegrationTests/** - Integration tests including navigation tests

### Configuration Files
- **appsettings.json** - Application settings (includes Syncfusion license key)
- **Program.cs** - Service configuration and middleware setup
- **/src/WitchCityRope.Infrastructure/Data/DbInitializer.cs** - Database seeding and initial data

### Test Accounts
The following test accounts are available (defined in DbInitializer.cs):
- **Admin**: admin@witchcityrope.com / Test123!
- **Teacher**: teacher@witchcityrope.com / Test123!
- **Vetted Member**: vetted@witchcityrope.com / Test123!
- **General Member**: member@witchcityrope.com / Test123!

## Session Context
This project has been actively developed with the following completed:
- Performance optimizations achieving 70% reduction
- Complete admin portal implementation
- UI testing and fixes against wireframes
- Compilation error fixes (resolved all 9 errors)
- Login functionality implementation with JWT authentication
- Comprehensive User Dashboard planning and documentation (see /docs/enhancements/UserDashboard/)

For any GitHub operations, the credentials are already configured and will work automatically.

## MCP Server Usage Patterns

### File Operations
**Use FileSystem MCP for:**
- Reading/writing project files in allowed directories
- Accessing files in: `C:\Users\chad\source\repos`, Documents, Downloads, Desktop
- Example: "Read the TODO.md file" or "Update the configuration in appsettings.json"

**DO NOT use FileSystem MCP for:**
- System files or directories outside allowed paths
- Use regular Read/Write tools for files in the current working directory

### Database Operations
**Use PostgreSQL MCP for:**
- Database queries once migrated from SQLite (currently placeholder)
- Read-only database operations for safety
- Schema inspection and data analysis
- Connection: `postgresql://postgres:your_password_here@localhost:5432/witchcityrope_db`

**Current Status:**
- Still using SQLite - PostgreSQL migration pending
- When ready, update password in connection string

### Browser Testing & Automation

**CRITICAL**: Chrome must ALWAYS be launched in **INCOGNITO MODE** for testing to ensure clean sessions without cached data or cookies!

#### PRIMARY METHOD: PowerShell Bridge with Universal Launcher (RECOMMENDED)

The PowerShell bridge method launches Chrome directly from WSL and is the **ONLY RELIABLE METHOD** that provides full access to both Browser Tools and Stagehand MCPs.

**Use the Universal Launcher Script:**
```bash
# ALWAYS use this script - it ensures incognito mode and proper settings
/mnt/c/users/chad/source/repos/WitchCityRope/src/mcp-servers/launch-chrome-universal.sh launch

# Check status
/mnt/c/users/chad/source/repos/WitchCityRope/src/mcp-servers/launch-chrome-universal.sh status

# Test connection
/mnt/c/users/chad/source/repos/WitchCityRope/src/mcp-servers/launch-chrome-universal.sh test
```

**PowerShell Bridge Benefits:**
- ✅ Launch Chrome directly from WSL - no pre-launch needed
- ✅ Full access to Browser Tools MCP
- ✅ Full access to Stagehand MCP  
- ✅ No SSH tunnels required
- ✅ Works immediately with both MCP servers
- ✅ Always launches in incognito mode for clean testing
- ✅ Simplest setup with most reliable results

**Direct PowerShell Command (if script unavailable):**
```bash
# IMPORTANT: Always include --incognito flag
powershell.exe -Command "& 'C:\Program Files\Google\Chrome\Application\chrome.exe' --remote-debugging-port=9222 --incognito"
```

#### Browser MCP Tools

**Use Stagehand MCP for:**
- Natural language UI testing ("Click the login button", "Fill in the form")
- Taking screenshots of specific elements
- Complex user flow testing
- AI-powered element selection and interaction

**Use Browser Tools MCP for:**
- Direct browser automation with specific selectors
- Performance testing and metrics
- Network request monitoring
- Console log capture
- Accessibility audits

#### Alternative Methods (NOT RECOMMENDED)

These methods have proven unreliable and should only be used if the PowerShell bridge method fails:

1. **Manual Windows Launch**: Requires SSH tunnel setup, prone to connection issues
2. **Persistent Server**: Complex setup, often fails to maintain stable connections
3. **Windows Batch File**: Limited functionality, doesn't work well with WSL environment

**Setup Documentation:**
- Universal launcher guide: `/src/mcp-servers/UNIVERSAL_LAUNCHER_GUIDE.md`
- Complete solution: `/src/mcp-servers/BROWSER_MCP_WORKING_SOLUTION.md`
- Chrome debug setup: `/src/mcp-servers/CHROME_DEBUG_SETUP.md`
- Browser automation guide: `/src/mcp-servers/BROWSER_AUTOMATION_GUIDE.md`

### Version Control
**Use GitHub MCP for:**
- Creating issues and pull requests
- Checking repository status
- Managing releases
- Viewing and creating commits
- Example: "Create an issue for the bug we just found"

**Use regular Git commands for:**
- Local git operations (add, commit, push)
- Branch management
- Merge operations

### Docker Operations
**Use Docker MCP for:**
- Managing containers (start, stop, restart, logs)
- Inspecting container status and health checks
- Executing commands in containers (e.g., database migrations)
- Building and managing Docker images
- Managing Docker networks and volumes
- Requires: Docker Desktop running

**Common Docker MCP Tasks:**
1. **Container Management:**
   - Start/stop the web and database containers
   - View real-time logs: `docker logs -f witchcityrope-web`
   - Check container health: `docker inspect witchcityrope-web --format='{{.State.Health.Status}}'`
   - Execute commands: `docker exec witchcityrope-web dotnet ef database update`

2. **Database Operations:**
   - Access PostgreSQL shell: `docker exec -it witchcityrope-db psql -U postgres`
   - Create database backup: `docker exec witchcityrope-db pg_dump -U postgres witchcityrope > backup.sql`
   - Restore database: `docker exec -i witchcityrope-db psql -U postgres witchcityrope < backup.sql`

3. **Development Workflow:**
   - Build development image: `docker-compose build`
   - Start all services: `docker-compose up -d`
   - View service status: `docker-compose ps`
   - Tail logs: `docker-compose logs -f web`
   - Stop services: `docker-compose down`

4. **Troubleshooting:**
   - Check container resource usage: `docker stats`
   - Inspect network connectivity: `docker network inspect witchcityrope-network`
   - Clean up unused resources: `docker system prune -a`
   - View volume contents: `docker run --rm -v witchcityrope_app_logs:/logs alpine ls -la /logs`

**Docker Compose Commands:**
```bash
# Development environment
docker-compose up -d                    # Start all services
docker-compose logs -f web              # Follow web container logs
docker-compose exec web bash            # Shell into web container
docker-compose down -v                  # Stop and remove volumes

# Production environment
docker-compose -f docker-compose.prod.yml up -d
docker-compose -f docker-compose.prod.yml exec web dotnet ef database update
```

### System Commands
**Use Commands MCP for:**
- PowerShell scripts
- Curl commands for API testing
- System automation tasks
- Limited to: curl, powershell commands only

### Memory & Context
**Use Memory MCP for:**
- Storing project-specific knowledge
- Remembering decisions and patterns
- Building a knowledge graph of the project
- Persistent information across sessions

**Good things to remember:**
- Architecture decisions
- Bug patterns and solutions
- User preferences
- Project-specific workflows

## MCP Server Prerequisites

Before starting Claude Desktop, ensure:

### 1. Browser MCP Setup (ALWAYS Use Incognito Mode!)

**CRITICAL**: Chrome must ALWAYS be launched in **INCOGNITO MODE** for testing!

#### PRIMARY METHOD: Universal Launcher Script (REQUIRED)
This is the ONLY reliable method that works consistently. Always use the universal launcher script:

```bash
# Launch Chrome with optimal settings (ALWAYS use this)
/mnt/c/users/chad/source/repos/WitchCityRope/src/mcp-servers/launch-chrome-universal.sh launch

# Check if Chrome is running
/mnt/c/users/chad/source/repos/WitchCityRope/src/mcp-servers/launch-chrome-universal.sh status

# Test the connection
/mnt/c/users/chad/source/repos/WitchCityRope/src/mcp-servers/launch-chrome-universal.sh test
```

**Why this is the ONLY recommended method**:
- ✅ Guarantees Chrome launches in incognito mode
- ✅ Launches directly from WSL environment
- ✅ Full access to both Browser Tools and Stagehand MCPs
- ✅ No SSH tunnels or complex networking required
- ✅ Works immediately every time
- ✅ Handles all edge cases and ensures proper cleanup

#### Direct PowerShell Command (Backup Only)
If the launcher script is unavailable, use this command:
```bash
# IMPORTANT: Always include --incognito flag
powershell.exe -Command "& 'C:\Program Files\Google\Chrome\Application\chrome.exe' --remote-debugging-port=9222 --incognito"
```

#### Alternative Methods (DEPRECATED - DO NOT USE)
The following methods have been tested and found unreliable:
- ❌ Manual Windows Launch - Requires SSH tunnels, connection issues
- ❌ Persistent Server - Complex setup, unstable connections
- ❌ Windows Batch Files - Don't work properly with WSL
- ❌ Pre-launching Chrome from Windows - Causes permission and access issues

**Browser MCP Tools Available**:
- **Stagehand**: Natural language browser control ("Click the login button")
- **Browser Tools**: Direct automation with selectors and performance testing

### 2. Docker Setup
Ensure Docker Desktop is running

### 3. PostgreSQL Setup
PostgreSQL will be needed after migration (currently using SQLite)

## Common MCP Usage Examples

### Example 1: UI Testing
"Use Stagehand to navigate to the login page and test the login flow with the admin@witchcityrope.com account"

### Example 2: File Analysis
"Use FileSystem MCP to read all .cs files in the Infrastructure folder and analyze the repository pattern"

### Example 3: Docker Logs
"Use Docker MCP to check the logs of the PostgreSQL container"

### Example 4: GitHub Integration
"Use GitHub MCP to create an issue for implementing lazy loading of Syncfusion components"

### Example 5: Memory Storage
"Use Memory MCP to remember that we decided to migrate from SQLite to PostgreSQL and the current status of that migration"

## Browser Automation Troubleshooting

### Chrome Incognito Mode Requirements

**CRITICAL**: All browser testing MUST be done in incognito mode to ensure:
- Clean sessions without cached data
- No interference from extensions
- Consistent test results
- No authentication persistence between tests

### Common Issues and Solutions

1. **"Cannot connect to Chrome DevTools"**
   - **PRIMARY SOLUTION**: Use the universal launcher script:
     ```bash
     /mnt/c/users/chad/source/repos/WitchCityRope/src/mcp-servers/launch-chrome-universal.sh launch
     ```
   - If script unavailable, use direct PowerShell with incognito:
     ```bash
     powershell.exe -Command "& 'C:\Program Files\Google\Chrome\Application\chrome.exe' --remote-debugging-port=9222 --incognito"
     ```
   - Check if port 9222 is in use: `netstat -an | findstr 9222` (in Windows)
   - Kill ALL Chrome instances and restart

2. **"Chrome not launching in incognito mode"**
   - ALWAYS use the universal launcher script - it enforces incognito mode
   - Never launch Chrome manually from Windows
   - If Chrome is already running, close ALL instances first
   - Verify incognito mode by checking for the incognito icon in Chrome

3. **"Tests failing due to cached data"**
   - This indicates Chrome is NOT in incognito mode
   - Close all Chrome instances
   - Re-launch using the universal launcher script
   - Never reuse existing Chrome sessions for testing

4. **"Connection refused from WSL"**
   - This usually means Chrome was launched incorrectly
   - Use ONLY the PowerShell bridge method (via launcher script)
   - Do NOT use SSH tunnels or manual Windows launch
   - The launcher script handles all connection setup automatically

5. **Testing if Chrome DevTools is accessible:**
   ```bash
   # Quick test (from WSL after launching with script)
   curl http://localhost:9222/json/version
   
   # Full connection test
   /mnt/c/users/chad/source/repos/WitchCityRope/src/mcp-servers/launch-chrome-universal.sh test
   ```

### Quick Reference Commands

```bash
# PRIMARY METHOD: Universal Launcher Script (ALWAYS USE THIS)
# This is the ONLY reliable method that guarantees incognito mode and proper setup

# Launch Chrome for testing (REQUIRED for all browser automation)
/mnt/c/users/chad/source/repos/WitchCityRope/src/mcp-servers/launch-chrome-universal.sh launch

# Check if Chrome is running with debug port
/mnt/c/users/chad/source/repos/WitchCityRope/src/mcp-servers/launch-chrome-universal.sh status

# Test the Chrome DevTools connection
/mnt/c/users/chad/source/repos/WitchCityRope/src/mcp-servers/launch-chrome-universal.sh test

# Direct PowerShell command (ONLY if launcher script is unavailable):
powershell.exe -Command "& 'C:\Program Files\Google\Chrome\Application\chrome.exe' --remote-debugging-port=9222 --incognito"

# Quick connection test (after launching):
curl http://localhost:9222/json/version

# DEPRECATED METHODS (DO NOT USE):
# The following methods have been tested and found unreliable:
# - Manual browser server start/stop scripts
# - Windows batch files
# - SSH tunnel methods
# - Pre-launching Chrome from Windows
```

### Browser Testing Best Practices

1. **ALWAYS use incognito mode** - The universal launcher enforces this
2. **Close all Chrome instances** before launching for tests
3. **Never reuse existing Chrome sessions** - Always start fresh
4. **Use the launcher script** - It handles all edge cases and setup
5. **Test the connection** before running automation scripts
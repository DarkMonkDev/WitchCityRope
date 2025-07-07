# Claude Code Project Configuration

## Project Overview
WitchCityRope is a Blazor Server application for a rope bondage community in Salem, offering workshops, performances, and social events.

## IMPORTANT: Working Login Test Pattern for UI Testing

When writing Puppeteer tests that require login, use this proven working pattern:

```javascript
// Step 1: Navigate to login page
await page.goto('http://localhost:5651/auth/login', {
  waitUntil: 'networkidle2',
  timeout: 30000
});

// Step 2: Fill in login credentials using multiple selector fallbacks
// Email field - use multiple selectors for reliability
await page.waitForSelector('input[type="email"], input[name="email"], input[id="email"], input[placeholder*="email"]', { timeout: 10000 });
await page.type('input[type="email"], input[name="email"], input[id="email"], input[placeholder*="email"]', 'admin@witchcityrope.com');

// Password field
await page.waitForSelector('input[type="password"]', { timeout: 10000 });
await page.type('input[type="password"]', 'Test123!');

// Step 3: Submit login form using evaluate to find button by text content
const submitClicked = await page.evaluate(() => {
  // Look for submit button by type or text content
  const buttons = document.querySelectorAll('button');
  for (const button of buttons) {
    const text = button.textContent?.toLowerCase() || '';
    if (button.type === 'submit' || text.includes('sign in') || text.includes('login')) {
      button.click();
      return true;
    }
  }
  
  // If no button found, try to submit the form directly
  const form = document.querySelector('form');
  if (form) {
    form.submit();
    return true;
  }
  
  return false;
});

// Step 4: Wait for navigation with error handling
if (submitClicked) {
  try {
    await page.waitForNavigation({ waitUntil: 'networkidle2', timeout: 30000 });
  } catch (navError) {
    console.log('Navigation timeout - checking current state...');
  }
}

// Step 5: Verify login success
const currentUrl = page.url();
console.log(`Current URL after login: ${currentUrl}`);
```

### Key Points:
1. **DO NOT** use simple selectors like `#Input_Email` - they often fail
2. **DO** use multiple selector fallbacks with type, name, id, and placeholder attributes
3. **DO NOT** rely on exact button text - use `includes()` for partial matches
4. **DO** use `page.evaluate()` to find and click buttons by their text content
5. **DO** handle navigation timeouts gracefully - sometimes the page updates without a full navigation

### Test Accounts Available:
- **Admin**: admin@witchcityrope.com / Test123!
- **Teacher**: teacher@witchcityrope.com / Test123!
- **Vetted Member**: vetted@witchcityrope.com / Test123!
- **General Member**: member@witchcityrope.com / Test123!

This pattern is proven to work in the test file: `/src/WitchCityRope.Web/screenshot-script/test-member-dashboard.js`

## Environment
- **Operating System**: Ubuntu 24.04 (Native Linux - NOT WSL)
- **Development Path**: `/home/chad/repos/witchcityrope/WitchCityRope`
- **MCP Servers Path**: `/home/chad/mcp-servers/`

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

## IMPORTANT: Docker-Only Development

**CRITICAL**: This project MUST be run using Docker containers only. Do not run the application directly on the host machine. All database connections, API services, and the web application should run through Docker Compose.

### Why Docker Only?
1. **Consistent Environment**: Ensures all developers have the same environment
2. **Database Isolation**: PostgreSQL runs in its own container with proper networking
3. **Service Communication**: Services communicate through Docker networks
4. **No Port Conflicts**: Avoids conflicts with local services
5. **Easy Cleanup**: Simple to stop and remove all services

## Development Guidelines

### Docker Development Commands (REQUIRED)

**Note**: Direct local development is NOT supported. Use Docker Compose for all development work.

```bash
# Start all services (database, API, and web)
docker-compose up -d

# View logs for all services
docker-compose logs -f

# View logs for specific service
docker-compose logs -f web
docker-compose logs -f api
docker-compose logs -f postgres

# Stop all services
docker-compose down

# Rebuild and restart services
docker-compose down && docker-compose up -d --build

# Access the application
# Web: http://localhost:5651
# API: http://localhost:5653
# Database: localhost:5433 (PostgreSQL)

# Run database migrations
docker-compose exec web dotnet ef database update

# Execute commands in containers
docker-compose exec web bash
docker-compose exec api bash
docker-compose exec postgres psql -U postgres -d witchcityrope_db
```

### Current Docker Configuration

**Services Running:**
- **PostgreSQL Database**: Port 5433 (internal: 5432)
- **API Service**: Port 5653 (HTTP only)
- **Web Application**: Port 5651 (HTTP only)

**Note**: HTTPS is disabled in Docker development. Use HTTP URLs:
- Web: http://localhost:5651
- API: http://localhost:5653

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
- Navigation architecture analysis and documentation (see /docs/design/user-flows/navigation-flows.md)

### Navigation Architecture
- **Current Implementation**: Navigation is integrated in MainLayout.razor (not separate component)
- **Desktop**: Horizontal nav with user dropdown menu
- **Mobile**: Slide-out menu with overlay
- **Documentation**: See /docs/design/user-flows/ for navigation patterns and site map
- **Note**: MainNav.razor component has been removed (was unused legacy code)

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

## Browser Testing & Automation

### 🚀 IMPORTANT: Testing Tool Priority Order

For browser automation and UI testing, use the following tools in order of preference:

1. **Direct Puppeteer (FIRST CHOICE)** - Write and run Puppeteer scripts directly
2. **Puppeteer MCP Server (SECOND CHOICE)** - Use the MCP server if available in Claude
3. **Stagehand MCP Server (OPTIONAL)** - For natural language automation only when needed

### UI Testing with Direct Puppeteer (Recommended)

**Why Direct Puppeteer First?**
- Full control over test scripts
- Better debugging capabilities
- No MCP server dependencies
- Faster execution
- Can be integrated into CI/CD pipelines

**Location**: Install Puppeteer directly in your project.

**What it's good for:**
- Direct browser automation with CSS/XPath selectors
- Performance testing and page load metrics
- Network request monitoring and interception
- Console log and error capture
- Accessibility audits (WCAG compliance)
- Generating PDFs from pages
- Testing responsive designs at different viewports

**How to use Direct Puppeteer:**
```bash
# Install Puppeteer in your project:
npm install puppeteer

# Create a test file (e.g., test-login.js):
const puppeteer = require('puppeteer');
// ... your test code ...

# Run the test directly:
node test-login.js

# Or integrate with testing frameworks:
npm test
```

### Puppeteer MCP Server (Second Choice)

If Direct Puppeteer is not suitable for your use case, you can use the Puppeteer MCP server in Claude:

**When to use:**
- Quick visual verification during development
- When you need Claude to capture screenshots
- For exploratory testing with Claude's assistance

**Available commands:**
```javascript
// In Claude, use these commands:
mcp_puppeteer.screenshot({
  url: "https://localhost:5652",
  fullPage: true
})

mcp_puppeteer.screenshot({
  url: "https://localhost:5652/events",
  selector: ".event-list"
})
```

**Common Puppeteer Tasks:**
- Navigate to URLs
- Take full page or element screenshots
- Click elements by selector
- Fill form fields
- Select dropdown options
- Execute JavaScript in page context
- Get page HTML content
- Wait for elements to appear

### Stagehand MCP Server (AI-powered) - Optional
**Location**: `/home/chad/mcp-servers/mcp-server-browserbase/stagehand/`

**What it's good for:**
- Natural language browser commands ("Click the login button")
- AI-powered element selection when selectors are complex
- Testing complex user flows with conversational instructions
- Taking screenshots of specific UI elements by description
- Handling dynamic content and AJAX-heavy applications
- Testing when exact selectors are unknown or change frequently

**How to use:**
```bash
# Set OpenAI API key (required):
export OPENAI_API_KEY='your-api-key-here'

# Run quickstart (includes Chrome launch):
/home/chad/mcp-servers/mcp-server-browserbase/stagehand/quickstart.sh

# Or start Chrome debug manually first:
google-chrome --remote-debugging-port=9222 --no-first-run --no-default-browser-check
```

**MCP Tools provided:**
- `stagehand_navigate` - Navigate using natural language
- `stagehand_action` - Perform actions using natural language
- `stagehand_extract` - Extract data using natural language
- `stagehand_screenshot` - Take screenshots by description
- `stagehand_observe` - Describe what's on the page

### When to Use Each Tool

#### Use Puppeteer when:
- You know exact CSS selectors or XPath
- You need performance metrics
- You want to monitor network requests
- You need to test specific viewport sizes
- You're automating repetitive tasks
- You need accessibility compliance testing

#### Use Stagehand MCP when:
- You want to describe actions in plain English
- UI elements are hard to select programmatically
- You're testing complex user journeys
- Selectors change frequently
- You need AI to understand page context
- You're exploring an unfamiliar UI

### Common Browser Testing Tasks

#### 1. Testing Login Flow
```javascript
// With Puppeteer:
const browser = await puppeteer.launch();
const page = await browser.newPage();
await page.goto('http://localhost:5000/login');
await page.type('#username', 'admin@witchcityrope.com');
await page.type('#password', 'Test123!');
await page.click('button[type="submit"]');
await page.waitForNavigation();
// Verify dashboard loads
const dashboardElement = await page.$('.dashboard-content');
expect(dashboardElement).toBeTruthy();
await browser.close();

// With Stagehand:
- "Go to the login page"
- "Enter admin@witchcityrope.com as username"
- "Enter the password Test123!"
- "Click the login button"
- "Verify I'm on the dashboard"
```

#### 2. Taking Screenshots
```javascript
// Full page screenshot with Puppeteer:
const browser = await puppeteer.launch();
const page = await browser.newPage();
await page.goto('http://localhost:5000');
await page.screenshot({ path: 'full-page.png', fullPage: true });
await browser.close();

// Element screenshot with Stagehand:
"Take a screenshot of the navigation menu"
```

#### 3. Form Testing
```javascript
// Puppeteer approach:
const browser = await puppeteer.launch();
const page = await browser.newPage();
await page.goto('http://localhost:5000/register');
// Fill form fields
await page.type('#email', 'test@example.com');
await page.type('#password', 'Test123!');
// Submit and check for errors
await page.click('button[type="submit"]');
const errorMessages = await page.$$('.validation-error');
expect(errorMessages.length).toBe(0);
await browser.close();

// Stagehand approach:
- "Fill out the registration form with test data"
- "Submit the form"
- "Check if there are any validation errors"
```

#### 4. Responsive Design Testing
```javascript
// Puppeteer (precise viewport control):
const browser = await puppeteer.launch();
const page = await browser.newPage();
await page.goto('http://localhost:5000');

// Test different viewports
await page.setViewport({ width: 375, height: 667 });  // iPhone SE
await page.screenshot({ path: 'mobile.png' });

await page.setViewport({ width: 768, height: 1024 }); // iPad
await page.screenshot({ path: 'tablet.png' });

await page.setViewport({ width: 1920, height: 1080 }); // Desktop
await page.screenshot({ path: 'desktop.png' });

await browser.close();

// Stagehand (descriptive):
"Show me how the page looks on mobile"
"Check if the menu is accessible on tablet"
```

### Starting the Test Environment

#### Quick Start Commands
```bash
# For Puppeteer tests:
npm test

# For Stagehand server (with API key):
export OPENAI_API_KEY='your-key-here'
/home/chad/mcp-servers/mcp-server-browserbase/stagehand/quickstart.sh

# Verify Chrome connection (for Stagehand):
curl http://localhost:9222/json/version
```

#### Testing Configuration
- Puppeteer manages its own Chrome/Chromium instance
- Stagehand uses local Chrome installation at `/usr/bin/google-chrome`
- Chrome DevTools port: 9222 (for Stagehand)
- No WSL workarounds needed on Ubuntu native

### Troubleshooting Browser Testing

#### Common Issues and Solutions

1. **"Cannot connect to Chrome DevTools"**
   ```bash
   # Check if Chrome is running:
   ps aux | grep chrome
   
   # Check if port 9222 is in use:
   lsof -i :9222
   
   # Kill existing Chrome processes:
   pkill -f "chrome.*remote-debugging"
   
   # Restart Chrome with debug port:
   google-chrome --remote-debugging-port=9222 --no-first-run
   ```

2. **"Sandbox errors" or "Permission denied"**
   - For Puppeteer, use: `puppeteer.launch({ args: ['--no-sandbox', '--disable-setuid-sandbox'] })`
   - Check available system resources: `free -h`
   - Ensure Chrome is executable: `chmod +x /usr/bin/google-chrome`

3. **"Element not found" errors**
   - Add explicit waits: `await page.waitForSelector('.element')`
   - Use Stagehand for dynamic content
   - Check if element is in iframe
   - Verify selector in browser DevTools first

4. **"Timeout" errors**
   - Increase timeout values in tool calls
   - Check if page is actually loading
   - Monitor network tab for hanging requests
   - Use headed mode to see what's happening

#### Testing the Setup
```bash
# Test Puppeteer directly:
node test-puppeteer.js

# Test Chrome connection (for Stagehand):
curl http://localhost:9222/json/version

# Check Chrome process:
ps aux | grep chrome | grep 9222

# Test with actual WitchCityRope site:
# 1. Start the application
cd /home/chad/repos/witchcityrope/WitchCityRope
~/.dotnet/dotnet run --project src/WitchCityRope.Web

# 2. Run your Puppeteer tests
npm test
```

### Best Practices for Browser Testing

1. **Always use automated testing** - Don't rely on manual testing alone
2. **Start with headed mode** when debugging new tests
3. **Use headless mode** for CI/CD and repeated runs
4. **Take screenshots** at key points for debugging
5. **Clean up resources** - Close browser sessions when done
6. **Use appropriate timeouts** - Don't make tests flaky with short timeouts
7. **Test at different viewports** for responsive design
8. **Capture console logs** to debug JavaScript errors
9. **Use AI (Stagehand) for complex interactions** that are hard to script
10. **Verify before automating** - Manually check the flow works first

### Example Test Scenarios

#### Complete Login Test with Puppeteer:
```javascript
const puppeteer = require('puppeteer');

async function testLogin() {
  const browser = await puppeteer.launch();
  const page = await browser.newPage();
  
  // Navigate to login
  await page.goto('http://localhost:5000/login');
  
  // Fill credentials
  await page.type('#username', 'admin@witchcityrope.com');
  await page.type('#password', 'Test123!');
  
  // Submit form
  await page.click('button[type="submit"]');
  
  // Wait for dashboard
  await page.waitForSelector('.dashboard-content', { timeout: 5000 });
  
  // Take screenshot for verification
  await page.screenshot({ path: 'login-success.png' });
  
  await browser.close();
}

testLogin();
```

#### Natural Language Test with Stagehand:
```
"Go to the WitchCityRope login page"
"Log in as the admin user with email admin@witchcityrope.com"
"Verify that I can see the admin dashboard"
"Take a screenshot of the dashboard"
"Navigate to the user management section"
"Check if I can see the list of users"
```

Remember: Automated browser testing with Puppeteer provides reliable, repeatable testing that manual testing cannot match.

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

### 1. Browser Testing Setup (Ubuntu Native)

**Puppeteer (Recommended)**:
- Install with: `npm install puppeteer`
- Manages its own Chromium instance
- No additional configuration needed
- Full documentation at: https://pptr.dev/

**Stagehand MCP Server (Optional)**:
- Already installed at: `/home/chad/mcp-servers/mcp-server-browserbase/stagehand/`
- Requires: `export OPENAI_API_KEY='your-key'`
- Uses local Chrome (not Browserbase cloud)
- AI-powered natural language browser control

### 2. Docker Setup
- Docker installed and running
- User added to docker group (requires logout/login to take effect)
- Use `docker` commands (docker-compose is integrated)

### 3. PostgreSQL Setup
- PostgreSQL 16 installed and running
- Service: `sudo systemctl status postgresql`
- Create database: `sudo -u postgres createdb witchcityrope_db`
- Default password from CLAUDE.md: WitchCity2024!

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

## Browser Automation Troubleshooting (Ubuntu)

### Common Issues and Solutions

1. **"Cannot connect to Chrome DevTools"**
   - Check if Chrome is running: `ps aux | grep chrome`
   - Ensure port 9222 is free: `lsof -i :9222`
   - Launch Chrome with debug port:
     ```bash
     google-chrome --remote-debugging-port=9222 --no-first-run --no-default-browser-check
     ```
   - Test connection: `curl http://localhost:9222/json/version`

2. **"Permission denied" errors**
   - Try safe mode config first: `mcp-config-safe.json`
   - If needed, use no-sandbox config: `mcp-config.json`
   - Ensure Chrome binary is executable: `ls -la /usr/bin/google-chrome`

3. **"Sandbox errors"**
   - Start with headed+safe mode (option 2 in start-server.sh)
   - Only use no-sandbox if absolutely necessary
   - Check system resources: `free -h` and `df -h`

4. **Testing Browser Automation:**
   ```bash
   # Test browser-tools directly:
   cd /home/chad/mcp-servers/browser-tools-server
   node test-puppeteer-direct.js
   
   # Test Chrome connection:
   curl http://localhost:9222/json/version
   
   # Check Chrome process:
   ps aux | grep chrome | grep 9222
   ```

### Quick Reference Commands

```bash
# Browser-tools MCP Server
cd /home/chad/mcp-servers/browser-tools-server
./start-server.sh  # Interactive menu with 4 modes

# Stagehand MCP Server  
export OPENAI_API_KEY='your-key'
/home/chad/mcp-servers/mcp-server-browserbase/stagehand/quickstart.sh

# Launch Chrome for debugging
google-chrome --remote-debugging-port=9222 --no-first-run --no-default-browser-check

# Test Chrome DevTools connection
curl http://localhost:9222/json/version

# Run browser automation test
cd /home/chad/repos/witchcityrope
node test-browser-automation.js

# Start WitchCityRope application
cd /home/chad/repos/witchcityrope/WitchCityRope
~/.dotnet/dotnet run --project src/WitchCityRope.Web
```

### Browser Testing Best Practices (Ubuntu)

1. **Clean browser state** - Each test gets a fresh browser instance
2. **Choose appropriate mode** - Headed for debugging, headless for CI
3. **Use Puppeteer's built-in features** - Screenshots, PDF generation, performance metrics
4. **Direct connection** - No proxies or tunnels needed on native Linux
5. **Test before automating** - Always verify the application is running first
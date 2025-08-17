# Docker Team Onboarding Guide - WitchCityRope
<!-- Last Updated: 2025-08-17 -->
<!-- Version: 1.0 -->
<!-- Owner: Librarian Agent -->
<!-- Status: Active -->

## Welcome to the Team! 🎉

This guide will get you up and running with the WitchCityRope Docker authentication project. By the end of this document, you'll have a fully working containerized authentication system running on your machine.

## 1. PROJECT OVERVIEW

### What is WitchCityRope?
WitchCityRope is a membership and event management platform for Salem's rope bondage community, offering workshops, performances, and social events. The application has been migrated from Blazor Server to a modern React + TypeScript frontend with a .NET Minimal API backend.

### Docker Architecture (4 Containers)
Our containerized system consists of:

- **React Container** (Port 5173): Vite-powered React + TypeScript frontend with hot module replacement
- **API Container** (Port 5655): .NET 9 Minimal API with ASP.NET Core Identity and hot reload
- **PostgreSQL Database** (Port 5433): PostgreSQL 16 Alpine with automated initialization
- **Test Container** (Port 8080): Nginx serving test pages for authentication validation

### Technology Stack Summary
- **Frontend**: React 18 + TypeScript + Vite (migrated from Blazor Server)
- **Backend**: .NET 9 Minimal API + ASP.NET Core Identity + Entity Framework Core
- **Database**: PostgreSQL 16 with automated schema migrations
- **Authentication**: Hybrid JWT + HttpOnly Cookies pattern (service-to-service + browser security)
- **Containerization**: Docker + Docker Compose with multi-environment support
- **Testing**: Playwright E2E + Vitest unit tests + .NET unit tests

## 2. GETTING STARTED

### Prerequisites Installation

Before starting, ensure you have these tools installed:

```bash
# Check if you have the required tools
docker --version          # Docker 20.10+
docker-compose --version  # Docker Compose 2.0+
node --version            # Node.js 18+
npm --version             # npm 9+
dotnet --version          # .NET 9.0+
git --version             # Git 2.30+
```

**Installation Commands (Ubuntu/Debian):**
```bash
# Update system
sudo apt update && sudo apt upgrade -y

# Install Docker and Docker Compose
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh
sudo usermod -aG docker $USER
sudo apt install docker-compose-plugin

# Install Node.js and npm
curl -fsSL https://deb.nodesource.com/setup_18.x | sudo -E bash -
sudo apt install -y nodejs

# Install .NET 9
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt update
sudo apt install -y dotnet-sdk-9.0

# Restart shell to apply Docker group changes
newgrp docker
```

### Repository Setup

```bash
# Clone the repository
git clone https://github.com/DarkMonkDev/WitchCityRope.git
cd WitchCityRope

# Switch to React branch (if not on master)
git checkout master

# Install frontend dependencies
cd apps/web
npm install
cd ../..

# Restore backend dependencies
cd apps/api
dotnet restore
cd ../..
```

### First-Time Setup Commands

```bash
# Make helper scripts executable
chmod +x dev.sh
chmod +x scripts/*.sh

# Start the Docker environment for the first time
./dev.sh

# Wait for all services to be healthy (30-60 seconds)
# You'll see output showing container startup progress
```

### Verify Everything Works

**1. Check Container Health:**
```bash
# Run comprehensive health check
./scripts/docker-health.sh

# You should see all services as "healthy"
```

**2. Test React Application:**
```bash
# Open in browser: http://localhost:5173
# You should see the WitchCityRope home page
```

**3. Test API Health:**
```bash
# Check API health endpoint
curl http://localhost:5655/health

# Should return: {"status":"Healthy","results":{...}}
```

**4. Test Authentication Flow:**
```bash
# Open test page: http://localhost:8080/test-auth.html
# Try registering a new user and logging in
# All authentication flows should work perfectly
```

## 3. DEVELOPMENT WORKFLOW

### Daily Development Process

**Start Your Day:**
```bash
# Start all containers with health checks
./dev.sh

# Watch logs (optional - opens in new terminal)
./scripts/docker-logs.sh
```

**During Development:**
```bash
# View specific service logs
docker-compose logs -f react    # React development logs
docker-compose logs -f api      # API development logs
docker-compose logs -f postgres # Database logs

# Rebuild a specific service after major changes
./scripts/docker-rebuild.sh api
./scripts/docker-rebuild.sh react
```

**End Your Day:**
```bash
# Stop all services
./scripts/docker-stop.sh

# Or stop and clean up
docker-compose down
```

### Using Helper Scripts

Our project includes several helper scripts for common operations:

```bash
# Development environment management
./dev.sh                     # Start full development environment
./scripts/docker-stop.sh     # Stop all containers
./scripts/docker-clean.sh    # Clean up containers, volumes, images

# Monitoring and debugging
./scripts/docker-health.sh   # Comprehensive health check
./scripts/docker-logs.sh     # View and filter service logs

# Database operations
./scripts/docker-migrate.sh  # Run database migrations
./scripts/docker-rebuild.sh  # Rebuild specific services
```

### Hot Reload Features

**React Hot Module Replacement (HMR):**
- Edit any React component in `/apps/web/src/`
- Changes appear instantly in browser (< 2 seconds)
- State preservation during component updates
- Full TypeScript error reporting

**API Hot Reload:**
- Edit any C# file in `/apps/api/`
- API rebuilds automatically (5-10 seconds)
- No container restart required
- Swagger UI updates automatically at http://localhost:5655/swagger

**Database Schema Changes:**
```bash
# Add new migration
cd apps/api
dotnet ef migrations add YourMigrationName

# Apply to running container
./scripts/docker-migrate.sh
```

### Testing Procedures

**Unit Tests:**
```bash
# React unit tests (Vitest)
cd apps/web
npm run test

# .NET unit tests
cd apps/api
dotnet test
```

**E2E Tests (Playwright):**
```bash
# Run E2E tests against containerized environment
cd apps/web
npm run test:e2e:playwright

# Run specific test file
npx playwright test auth.spec.ts
```

**Authentication Testing:**
```bash
# Open comprehensive test page
open http://localhost:8080/test-auth.html

# Or run automated authentication tests
curl -X POST http://localhost:5655/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test123!","firstName":"Test","lastName":"User"}'
```

## 4. KEY CONCEPTS

### JWT + HttpOnly Cookies Pattern

Our authentication system uses a hybrid approach that combines the benefits of JWT tokens for service-to-service communication with HttpOnly cookies for browser security:

**How it Works:**
1. User submits credentials to `/auth/login`
2. API validates credentials using ASP.NET Core Identity
3. API generates JWT token for service-to-service auth
4. API sets HttpOnly cookie for browser sessions
5. React app receives authentication state via `/auth/me`
6. Subsequent requests include both JWT (for API) and cookies (for browser)

**Security Benefits:**
- **XSS Protection**: HttpOnly cookies can't be accessed by JavaScript
- **CSRF Protection**: SameSite cookie configuration prevents cross-site attacks
- **Service Communication**: JWT tokens enable secure API-to-API calls
- **Session Management**: Sliding expiration with automatic renewal

### Service-to-Service Authentication

In our containerized environment, services communicate using internal Docker networking:

```bash
# Container-to-container communication
react-container -> api-container:5655 -> postgres-container:5432

# External access (development)
localhost:5173 -> localhost:5655 -> localhost:5433
```

**Authentication Flow in Containers:**
1. React sends auth request to `api:5655` (internal network)
2. API validates against PostgreSQL at `postgres:5432`
3. Response includes both JWT token and HttpOnly cookie
4. Browser receives cookie for subsequent requests
5. Internal API calls use JWT tokens

### Container Networking

Our Docker setup uses a custom bridge network (`witchcityrope-network`) with DNS resolution:

```yaml
# Service communication by container name
services:
  react:
    # Can reach: http://api:5655, http://postgres:5432
  api:
    # Can reach: http://postgres:5432
  postgres:
    # Database service - reached by other containers
```

**Network Configuration:**
- **Bridge Network**: Custom network with container name DNS
- **Port Mapping**: Host ports mapped to container ports
- **Security Groups**: Containers can only access necessary services
- **SSL/TLS**: HTTPS termination at reverse proxy level

### Environment Configurations

We support multiple environments with layered Docker Compose files:

```bash
# Development (default)
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up

# Testing
docker-compose -f docker-compose.yml -f docker-compose.test.yml up

# Production
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up
```

**Configuration Levels:**
- **Base** (`docker-compose.yml`): Common service definitions
- **Development** (`docker-compose.dev.yml`): Hot reload, debug ports, relaxed security
- **Testing** (`docker-compose.test.yml`): Isolated test data, Playwright integration
- **Production** (`docker-compose.prod.yml`): Hardened security, performance optimization

## 5. COMMON TASKS

### Starting/Stopping Containers

**Standard Startup:**
```bash
# Start with health checks and dependency order
./dev.sh

# Start specific environment
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d

# Start and follow logs
docker-compose up
```

**Graceful Shutdown:**
```bash
# Stop all services
./scripts/docker-stop.sh

# Stop with cleanup
docker-compose down

# Stop and remove volumes (CAUTION: deletes data)
docker-compose down -v
```

### Running Migrations

**Automatic Migration (Recommended):**
```bash
# Run all pending migrations
./scripts/docker-migrate.sh

# Check migration status
./scripts/docker-migrate.sh --status
```

**Manual Migration:**
```bash
# Create new migration
cd apps/api
dotnet ef migrations add YourMigrationName

# Apply to running container
docker-compose exec api dotnet ef database update
```

### Testing Authentication

**Complete Authentication Test:**
```bash
# Open comprehensive test page
open http://localhost:8080/test-auth.html
```

**API Testing with curl:**
```bash
# Register new user
curl -X POST http://localhost:5655/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email":"newuser@test.com",
    "password":"Test123!",
    "firstName":"New",
    "lastName":"User"
  }'

# Login user
curl -X POST http://localhost:5655/auth/login \
  -H "Content-Type: application/json" \
  -c cookies.txt \
  -d '{
    "email":"newuser@test.com",
    "password":"Test123!"
  }'

# Access protected endpoint
curl -X GET http://localhost:5655/protected \
  -b cookies.txt
```

**Test User Accounts:**
Pre-seeded test accounts available immediately:
- **Admin**: admin@witchcityrope.com / Test123!
- **Teacher**: teacher@witchcityrope.com / Test123!
- **Vetted Member**: vetted@witchcityrope.com / Test123!
- **General Member**: member@witchcityrope.com / Test123!
- **Guest/Attendee**: guest@witchcityrope.com / Test123!

### Debugging Issues

**Check Service Health:**
```bash
# Comprehensive health check
./scripts/docker-health.sh

# Check specific service
docker-compose ps
docker-compose logs api
```

**Common Debug Commands:**
```bash
# Inspect container
docker-compose exec api bash
docker-compose exec postgres psql -U witchcityrope -d witchcityrope

# Check resource usage
docker stats

# Inspect networks
docker network ls
docker network inspect witchcityrope-network
```

**Database Debugging:**
```bash
# Connect to PostgreSQL
docker-compose exec postgres psql -U witchcityrope -d witchcityrope

# Common queries
\dt                    # List tables
\d AspNetUsers        # Describe users table
SELECT COUNT(*) FROM AspNetUsers;  # Count users
```

## 6. TROUBLESHOOTING GUIDE

### Common Issues and Solutions

**Problem: Container won't start**
```bash
# Check Docker daemon
sudo systemctl status docker
sudo systemctl start docker

# Check port conflicts
netstat -tlnp | grep :5173
netstat -tlnp | grep :5655
netstat -tlnp | grep :5433

# Solution: Kill conflicting processes or change ports
```

**Problem: Database connection failed**
```bash
# Check PostgreSQL container status
docker-compose logs postgres

# Verify connection string format
# Container format: "Host=postgres;Port=5432;Database=witchcityrope;Username=witchcityrope;Password=..."
# Host format: "Host=localhost;Port=5433;Database=witchcityrope;Username=witchcityrope;Password=..."

# Test database connection
docker-compose exec postgres psql -U witchcityrope -d witchcityrope -c "SELECT 1;"
```

**Problem: Hot reload not working**
```bash
# React hot reload issues
cd apps/web
rm -rf node_modules/.vite
npm run dev

# API hot reload issues
./scripts/docker-rebuild.sh api

# Check file watching (Linux)
echo fs.inotify.max_user_watches=524288 | sudo tee -a /etc/sysctl.conf
sudo sysctl -p
```

**Problem: Authentication flow broken**
```bash
# Check API health
curl http://localhost:5655/health

# Verify JWT configuration
docker-compose exec api dotnet user-secrets list

# Test with known user
curl -X POST http://localhost:5655/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@witchcityrope.com","password":"Test123!"}'
```

### Where to Find Logs

**Service Logs:**
```bash
# All services
./scripts/docker-logs.sh

# Specific service
docker-compose logs -f react
docker-compose logs -f api
docker-compose logs -f postgres
```

**Application Logs:**
- **React**: Browser Developer Tools Console
- **API**: Docker logs + `/tmp/witchcityrope-api.log` (in container)
- **Database**: PostgreSQL logs in Docker logs

**Log Locations in Containers:**
```bash
# API container logs
docker-compose exec api ls /app/logs/

# PostgreSQL logs
docker-compose exec postgres ls /var/log/postgresql/
```

### Getting Help

**Documentation Resources:**
- **Docker Operations Guide**: `/docs/guides-setup/docker-operations-guide.md`
- **Production Deployment**: `/docs/guides-setup/docker-production-deployment.md`
- **Authentication Documentation**: `/docs/functional-areas/authentication/`
- **Project Architecture**: `/ARCHITECTURE.md`

**Quick Help Commands:**
```bash
# Script help
./scripts/docker-health.sh --help
./scripts/docker-logs.sh --help

# Docker Compose help
docker-compose --help
docker-compose logs --help
```

**Team Communication:**
- **Issues**: Create GitHub issues for bugs
- **Questions**: Check existing documentation first
- **Improvements**: Submit pull requests with documentation updates

## 7. RESOURCES

### Links to Key Documents

**Essential Documentation:**
- [Project Overview](/PROGRESS.md) - Current project status and milestones
- [Architecture Guide](/ARCHITECTURE.md) - System design and patterns
- [Docker Operations Guide](/docs/guides-setup/docker-operations-guide.md) - Comprehensive Docker procedures
- [Authentication Guide](/docs/functional-areas/authentication/) - Authentication implementation details

**Development Guides:**
- [Developer Quick Start](/docs/guides-setup/developer-quick-start.md) - Non-Docker development setup
- [Database Setup](/docs/guides-setup/database-setup.md) - Database configuration details
- [Playwright Setup](/docs/guides-setup/playwright-setup.md) - E2E testing configuration

**Standards and Processes:**
- [Documentation Standards](/docs/standards-processes/) - Project documentation standards
- [Session Handoffs](/docs/standards-processes/session-handoffs/) - Team communication patterns
- [Agent Boundaries](/docs/standards-processes/agent-boundaries.md) - AI workflow coordination

### Architecture Diagrams

**System Architecture:**
```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   React App     │    │   .NET API      │    │   PostgreSQL    │
│   (Port 5173)   │───▶│   (Port 5655)   │───▶│   (Port 5433)   │
│                 │    │                 │    │                 │
│ - TypeScript    │    │ - ASP.NET Core  │    │ - Identity      │
│ - Vite HMR      │    │ - Identity      │    │ - Sessions      │
│ - React Context │    │ - JWT + Cookies │    │ - User Data     │
└─────────────────┘    └─────────────────┘    └─────────────────┘
                                │
                                ▼
                       ┌─────────────────┐
                       │   Test Server   │
                       │   (Port 8080)   │
                       │                 │
                       │ - Auth Tests    │
                       │ - Validation    │
                       └─────────────────┘
```

**Container Network:**
```
Host Machine                    Docker Network (witchcityrope-network)
├── localhost:5173     ────▶    react:5173
├── localhost:5655     ────▶    api:5655
├── localhost:5433     ────▶    postgres:5432
└── localhost:8080     ────▶    test:80

Internal Communication:
react ──HTTP──▶ api ──TCP──▶ postgres
```

### API Documentation

**Authentication Endpoints:**
- `POST /auth/register` - Create new user account
- `POST /auth/login` - Authenticate user (returns JWT + sets HttpOnly cookie)
- `POST /auth/logout` - Invalidate session
- `GET /auth/me` - Get current user info
- `POST /auth/refresh` - Refresh authentication token

**Protected Endpoints:**
- `GET /protected` - Test protected resource access
- `GET /admin` - Admin-only test endpoint
- `GET /health` - API health check

**Base URLs:**
- **Development**: http://localhost:5655
- **Container Internal**: http://api:5655
- **Swagger UI**: http://localhost:5655/swagger

### Test Accounts Reference

**Pre-configured Test Users:**
| Role | Email | Password | Permissions |
|------|--------|----------|-------------|
| Admin | admin@witchcityrope.com | Test123! | Full system access |
| Teacher | teacher@witchcityrope.com | Test123! | Event management, student access |
| Vetted Member | vetted@witchcityrope.com | Test123! | Vetted event access |
| General Member | member@witchcityrope.com | Test123! | Standard member access |
| Guest/Attendee | guest@witchcityrope.com | Test123! | Public event access |

**Account Features:**
- All accounts are pre-verified (no email verification required)
- Passwords meet security requirements (8+ chars, mixed case, numbers, symbols)
- Roles assigned according to WitchCityRope membership tiers
- Test data includes realistic profile information

---

## Welcome to the Team!

You're now ready to contribute to the WitchCityRope Docker authentication project! The system you'll be working on has:

- ✅ **97% test success rate** in containerized environment
- ✅ **Production-ready authentication** with JWT + HttpOnly Cookies
- ✅ **Hot reload development** for both React and .NET API
- ✅ **Comprehensive test suite** with E2E and unit tests
- ✅ **Multi-environment support** (dev/test/prod)
- ✅ **Security validation** including XSS/CSRF protection

The authentication system is 100% complete and working. Your focus will be on containerization, performance optimization, and ensuring the same authentication flows work identically in Docker as they do in the localhost development environment.

**First Steps:**
1. Follow the setup instructions above
2. Verify all containers start and authenticate successfully
3. Explore the codebase in `/apps/web` (React) and `/apps/api` (.NET)
4. Run the test suite to familiarize yourself with the testing approach
5. Review the Docker operations guide for advanced procedures

**Questions?** Check the documentation links above or reach out to the team. Happy coding! 🚀
# Running WitchCityRope in Visual Studio Code

## Overview
This guide explains how to run the WitchCityRope application in Visual Studio Code. The application consists of two main components that need to run together:
- **API** (Port 8180) - REST API with authentication
- **Web** (Port 8280) - Blazor Server UI

## Quick Start - Just Press F5!

### Default Method: VS Code Debugging (F5)
1. Open VS Code in the project root
2. Press `F5` (or Run → Start Debugging)
3. The system will automatically:
   - Build the solution
   - Start the API on port 8180 in the background
   - Start the Web UI on port 8280 with debugging
   - Open your browser to http://localhost:8280

**That's it!** No configuration selection needed.

### Alternative Option 1: Using the Launch Script
```bash
cd /home/chad/repos/witchcityrope/WitchCityRope
./run-local.sh
```
This will:
1. Start PostgreSQL if needed
2. Kill any existing processes on ports 8180/8280
3. Start the API on http://localhost:8180
4. Start the Web UI on http://localhost:8280

### Alternative Option 2: Manual Terminal Commands
```bash
# Terminal 1 - Start API
cd src/WitchCityRope.Api
dotnet run --urls "http://localhost:8180"

# Terminal 2 - Start Web (after API is running)
cd src/WitchCityRope.Web
export ApiUrl="http://localhost:8180"
dotnet run --urls "http://localhost:8280"
```

## Port Configuration

| Service | Port | Purpose |
|---------|------|---------|
| API | 8180 | REST API endpoints |
| Web | 8280 | Blazor Server UI |
| PostgreSQL | 5432 | Database (local) |

## Troubleshooting

### Issue: "Port 8080 already in use"
This happens when VS Code is using Docker settings. Make sure you're using the correct launch configuration.

**Fix:**
1. Check that `.vscode/launch.json` exists with correct ports
2. Use the "Web + API" launch configuration, not a Docker configuration
3. Or use the `run-local.sh` script

### Issue: "Cannot connect to API"
The Web app needs the API running first.

**Fix:**
1. Ensure API is running on port 8180
2. Check the ApiUrl environment variable is set
3. Verify no firewall is blocking localhost connections

### Issue: "Database connection failed"
PostgreSQL needs to be running locally.

**Fix:**
```bash
# Check PostgreSQL status
sudo systemctl status postgresql

# Start if needed
sudo systemctl start postgresql

# Verify connection
psql -U postgres -d witchcityrope_db -c "SELECT 1"
```

## VS Code Configuration Files

### `.vscode/launch.json`
Defines debug configurations for:
- Launch Web (http) - Starts Web UI only
- Launch API - Starts API only
- Web + API - Starts both (recommended)

### `.vscode/tasks.json`
Defines build tasks:
- build - Builds the solution
- watch - Runs with hot reload

### `Properties/launchSettings.json`
Each project has its own launch settings:
- API: `src/WitchCityRope.Api/Properties/launchSettings.json`
- Web: `src/WitchCityRope.Web/Properties/launchSettings.json`

These define the ports and environment variables for each profile.

## Environment Variables

When running locally, these environment variables are important:

```bash
# For Web project
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://localhost:8280
ApiUrl=http://localhost:8180

# For API project
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://localhost:8180
```

## Docker vs Local Development

**Important:** The application can run in two ways:

1. **Docker** (uses port 8080 internally, mapped to 5651)
   ```bash
   docker-compose up
   ```

2. **Local** (uses ports 8180/8280)
   ```bash
   ./run-local.sh
   # or use VS Code debugging
   ```

Make sure you're using the right method for your needs. If you see port 8080, you're probably in Docker mode.

## Accessing the Application

Once running locally:
- **Web UI**: http://localhost:8280
- **API Swagger**: http://localhost:8180/swagger
- **Health Check**: http://localhost:8180/health

## Test Accounts
- admin@witchcityrope.com / Test123!
- member@witchcityrope.com / Test123!
- teacher@witchcityrope.com / Test123!
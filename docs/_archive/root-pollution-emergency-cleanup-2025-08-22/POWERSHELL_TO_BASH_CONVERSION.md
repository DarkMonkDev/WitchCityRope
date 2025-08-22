# PowerShell to Bash Script Conversion Summary

## Overview
Successfully converted PowerShell scripts to bash scripts for Ubuntu/Linux development environment.

## Converted Scripts

### 1. check-vulnerabilities.sh ✅
- **Location**: `/home/chad/repos/witchcityrope/WitchCityRope/check-vulnerabilities.sh`
- **Purpose**: Check for security vulnerabilities in .NET packages
- **Features**:
  - Checks for vulnerable packages using `dotnet list package --vulnerable`
  - Checks for outdated packages
  - Scans for legacy packages that need attention
  - Color-coded output for better readability
  - Platform-independent

### 2. start-docker.sh ✅
- **Location**: `/home/chad/repos/witchcityrope/WitchCityRope/start-docker.sh`
- **Purpose**: Start Docker Compose services
- **Features**:
  - Creates .env from .env.example if needed
  - Starts services with docker-compose
  - Displays service URLs
  - Simple and straightforward

### 3. start-chrome-debug.sh ✅
- **Location**: `/home/chad/repos/witchcityrope/WitchCityRope/start-chrome-debug.sh`
- **Purpose**: Start Chrome/Chromium in debug mode for Stagehand MCP
- **Features**:
  - Auto-detects Chrome/Chromium installation
  - Works on Linux and macOS
  - Creates temporary user data directory
  - Proper cleanup on exit
  - Remote debugging on port 9222

### 4. build-with-postgres.sh ✅
- **Location**: `/home/chad/repos/witchcityrope/WitchCityRope/scripts/build-with-postgres.sh`
- **Purpose**: Build solution after ensuring PostgreSQL is running
- **Features**:
  - Checks PostgreSQL status
  - Restores NuGet packages
  - Builds the solution
  - Optional test execution with `-t` flag
  - Detailed error handling
  - Color-coded output

### 5. performance-analysis.sh ✅
- **Location**: `/home/chad/repos/witchcityrope/WitchCityRope/src/WitchCityRope.Web/performance-analysis.sh`
- **Purpose**: Analyze web application performance
- **Features**:
  - Analyzes CSS and JavaScript bundle sizes
  - Checks minification status
  - Detects external resources
  - Checks for images
  - Generates detailed performance report
  - Provides optimization recommendations
  - Creates timestamped reports in `performance-reports/` directory

### 6. minify-assets.sh ✅
- **Location**: `/home/chad/repos/witchcityrope/WitchCityRope/src/WitchCityRope.Web/minify-assets.sh`
- **Purpose**: Minify CSS and JavaScript assets
- **Features**:
  - Checks for Node.js installation
  - Auto-installs terser and cssnano if needed
  - Minifies CSS files (app.css, pages.css, wcr-theme.css)
  - Minifies JavaScript files (app.js)
  - Shows size reduction statistics
  - Generates cache-busting version hash

### 7. start-ui-monitoring.sh ✅
- **Location**: `/home/chad/repos/witchcityrope/WitchCityRope/tools/start-ui-monitoring.sh`
- **Purpose**: Start Chrome with remote debugging for UI monitoring
- **Features**:
  - Checks Chrome installation
  - Kills existing debug instances
  - Configurable debug port
  - Optional browser opening
  - Sets environment variables for MCP
  - Proper process management

## Usage Examples

```bash
# Check for vulnerabilities
./check-vulnerabilities.sh

# Start Docker services
./start-docker.sh

# Start Chrome for debugging
./start-chrome-debug.sh

# Build with PostgreSQL
./scripts/build-with-postgres.sh
./scripts/build-with-postgres.sh --with-tests

# Analyze performance
cd src/WitchCityRope.Web
./performance-analysis.sh

# Minify assets
cd src/WitchCityRope.Web
./minify-assets.sh

# Start UI monitoring
./tools/start-ui-monitoring.sh
./tools/start-ui-monitoring.sh --open-browser --port 9223
```

## Benefits for Ubuntu Development

1. **Native Linux Support**: All scripts use bash and standard Linux tools
2. **No Windows Dependencies**: Removed PowerShell-specific cmdlets
3. **Better Performance**: Native bash execution is faster than PowerShell on Linux
4. **Standard Tools**: Uses common Linux utilities (grep, find, curl, etc.)
5. **Consistent Experience**: Same scripting language as system scripts

## Dependencies

- **bash**: Standard shell (pre-installed)
- **dotnet**: .NET SDK for building and vulnerability checks
- **docker & docker-compose**: For container management
- **node & npm**: For minification tools (minify-assets.sh)
- **google-chrome**: For UI monitoring and debugging
- **bc**: Basic calculator for floating-point math (usually pre-installed)
- **curl**: For HTTP requests (usually pre-installed)

## Notes

- All scripts have been made executable with `chmod +x`
- Scripts use color codes for better terminal output
- Error handling has been improved for Linux environments
- Scripts follow Linux/Unix conventions (exit codes, signal handling)
- Platform detection added where needed (Chrome/Chromium paths)
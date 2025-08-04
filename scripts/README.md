# WitchCityRope Scripts

This directory contains utility scripts for development, testing, and deployment of the WitchCityRope application.

> **📍 Script Inventory**: See [SCRIPT_INVENTORY.md](./SCRIPT_INVENTORY.md) for a complete listing of all scripts in the project, their locations, purposes, and usage instructions.

## Quick Access to Essential Scripts

The most frequently used development scripts remain in the root directory:
- `./dev.sh` - Main development menu system
- `./restart-web.sh` - Quick restart when hot reload fails
- `./check-dev-tools-status.sh` - Verify development environment

## Primary Development Launcher

### dev-start.sh / dev-start.ps1

**The comprehensive development launcher** that checks all prerequisites and provides multiple launch options.

**Features:**
- Checks for .NET SDK, PostgreSQL, and Docker
- Verifies environment configuration (.env file)
- Restores NuGet packages and builds the solution
- Checks for pending database migrations with option to apply
- Offers two launch options:
  1. **Direct launch** - Runs web app on ports 8280/8281
  2. **Docker Compose** - Full containerized environment
- Handles Ctrl+C gracefully with proper cleanup
- Color-coded status messages for clarity

**Usage:**
```bash
# From project root - Linux/WSL
./run.sh      # Quick launcher
# OR
./scripts/dev-start.sh

# From project root - Windows PowerShell
.\run.ps1     # Quick launcher
# OR
.\scripts\dev-start.ps1
```

**The script will:**
1. Verify you're in the correct directory
2. Check all prerequisites (SDK, PostgreSQL, Docker)
3. Create .env from template if needed
4. Build the entire solution
5. Check for and optionally apply database migrations
6. Present launch options with clear port information
7. Start the application with your chosen method

## Test Data Seeding Scripts

### seed-test-data.sh / seed-test-data.ps1

Seeds the PostgreSQL database with test data for development and testing purposes.

**Features:**
- Automatically checks if PostgreSQL is running
- Starts PostgreSQL container if needed
- Uses SQL script if psql is available, otherwise uses C# script
- Verifies data was inserted successfully
- Displays test account credentials

**Usage:**

Linux/WSL:
```bash
./seed-test-data.sh
```

Windows PowerShell:
```powershell
.\seed-test-data.ps1
```

**PowerShell Options:**
- `-SkipPostgresCheck`: Skip PostgreSQL container check (if managing PostgreSQL separately)
- `-Verbose`: Show detailed output

### seed-database.sql

Raw SQL script for seeding the PostgreSQL database. Can be run directly with psql:
```bash
PGPASSWORD="WitchCity2024!" psql -h localhost -p 5432 -U postgres -d witchcityrope_db -f seed-database.sql
```

### SeedDatabase.cs

C# script for seeding the database using dotnet-script. Useful when psql is not available:
```bash
# Install dotnet-script if needed
dotnet tool install -g dotnet-script

# Run the seeder
export DB_CONNECTION_STRING="Host=localhost;Port=5432;Database=witchcityrope_db;Username=postgres;Password=WitchCity2024!"
dotnet-script SeedDatabase.cs
```

### quick-start.sh / quick-start.ps1

**Quick development environment setup** - Simpler alternative to dev-start for rapid setup.

**What it does:**
1. Starts PostgreSQL in Docker
2. Seeds the database with test data
3. Provides instructions to run the application

**Usage:**

Linux/WSL:
```bash
./scripts/quick-start.sh
```

Windows PowerShell:
```powershell
.\scripts\quick-start.ps1
```

**Note:** For a more comprehensive setup with build checks and launch options, use `dev-start.sh/ps1` instead.

## Test Accounts Created

All test accounts use the password: **Test123!**

| Email | Role | Description |
|-------|------|-------------|
| admin@witchcityrope.com | Administrator | Full system access |
| teacher@witchcityrope.com | Teacher | Can organize workshops and events |
| vetted@witchcityrope.com | VettedMember | Trusted community member |
| member@witchcityrope.com | Member | Regular member with pending vetting |
| alice@example.com | VettedMember | Additional vetted member |
| bob@example.com | Member | Has pending vetting application |
| charlie@example.com | Attendee | Basic guest access |

## Seeded Data Overview

The seeding scripts create:
- **7 Users** with various roles (Admin, Teacher, VettedMember, Member, Attendee)
- **5 Events** including:
  - Introduction to Rope Bondage (Workshop)
  - Midnight Rope Performance (Performance)
  - Monthly Rope Social (Social)
  - Advanced Suspension Techniques (Workshop)
  - Halloween Rope Party (Past Event)
- **2 Vetting Applications** in pending status
- **Event Organizers** linking users to events they organize
- **User Authentications** with BCrypt hashed passwords

## Database Management Scripts

### ensure-postgres.sh / ensure-postgres.ps1

Automatically detects and starts the PostgreSQL container for WitchCityRope.

**Features:**
- Detects if PostgreSQL is already running (Docker Compose)
- Starts stopped containers automatically
- Creates new container if none exist
- Handles multiple deployment scenarios:
  - Docker Compose (port 5433)
  - Standalone Docker (port 5432)
- Waits for PostgreSQL to be ready before exiting
- Idempotent - safe to run multiple times

**Usage:**

Linux/WSL:
```bash
./ensure-postgres.sh
```

Windows PowerShell:
```powershell
.\ensure-postgres.ps1
```

**What it does:**
1. Checks if Docker is running
2. Looks for existing PostgreSQL containers (running or stopped)
3. Starts appropriate container based on environment
4. Displays connection information
5. Verifies PostgreSQL is ready to accept connections

**Connection Details:**
- Password: `WitchCity2024!`
- Username: `postgres`
- Database: `witchcityrope_db`
- Port varies by environment (shown in output)

## Build Scripts

### build-with-postgres.sh / build-with-postgres.ps1

Automatically ensures PostgreSQL is running before building the WitchCityRope solution.

**Features:**
- Automatically checks and starts PostgreSQL container
- Restores NuGet packages
- Builds the entire solution
- Optionally runs tests after building
- Provides clear error messages and build status
- Shows next steps after successful build

**Usage:**

Linux/WSL:
```bash
# Basic build
./build-with-postgres.sh

# Build and run tests
./build-with-postgres.sh --with-tests
# or
./build-with-postgres.sh -t

# Show help
./build-with-postgres.sh --help
```

Windows PowerShell:
```powershell
# Basic build
.\build-with-postgres.ps1

# Build and run tests
.\build-with-postgres.ps1 -WithTests
# or
.\build-with-postgres.ps1 -t

# Show help
.\build-with-postgres.ps1 -Help
```

**What it does:**
1. Verifies you're in the correct project directory
2. Runs ensure-postgres script to start PostgreSQL if needed
3. Restores all NuGet packages
4. Builds the entire solution
5. Optionally runs all tests
6. Displays build summary and next steps

**Error Handling:**
- Stops on first error for quick feedback
- Provides specific error messages
- Handles Ctrl+C gracefully
- Non-zero exit codes for CI/CD integration

## Other Scripts

### deploy-staging.sh
Deploys the application to the staging environment.

### init-staging-db.sh
Initializes the staging database with schema and initial data.


## Prerequisites

- Docker Desktop installed and running
- .NET SDK 9.0 or later
- PostgreSQL container or local installation
- Appropriate permissions to run scripts

## Troubleshooting

### PostgreSQL won't start
- Ensure Docker Desktop is running
- Check if port 5432 is already in use
- Review docker-compose.postgres.yml for configuration issues

### Seeding fails
- Check PostgreSQL connection string in appsettings.json
- Ensure migrations have been applied
- Check for existing data (seeder skips if data exists)

### Permission denied (Linux/WSL)
- Make scripts executable: `chmod +x *.sh`
- Run with appropriate permissions

## Notes

- The seeder will skip if data already exists in the database
- To reseed, you'll need to clear the existing data first
- Test data includes 10 sample events with various types and dates
- All user accounts are pre-verified for immediate testing
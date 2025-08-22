# WitchCityRope Test Data Seeding Script (PowerShell Version)
# This script ensures the database has proper test data for development and testing

param(
    [switch]$SkipPostgresCheck = $false,
    [switch]$Verbose = $false
)

# Script configuration
$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir

# Color functions for output
function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$ForegroundColor = "White"
    )
    Write-Host $Message -ForegroundColor $ForegroundColor
}

function Write-Success { Write-ColorOutput "✓ $args" "Green" }
function Write-Error { Write-ColorOutput "✗ $args" "Red" }
function Write-Info { Write-ColorOutput "$args" "Cyan" }
function Write-Warning { Write-ColorOutput "$args" "Yellow" }

Write-ColorOutput "=== WitchCityRope Test Data Seeder ===" "Blue"
Write-Host

# Function to check if PostgreSQL is running
function Test-PostgreSQL {
    Write-Info "Checking PostgreSQL status..."
    
    try {
        # Check if PostgreSQL container is running
        $postgresContainer = docker ps --format "table {{.Names}}" | Select-String "postgres"
        
        if ($postgresContainer) {
            Write-Success "PostgreSQL container is running"
            return $true
        } else {
            Write-Error "PostgreSQL container is not running"
            
            # Try to start PostgreSQL
            Write-Warning "Attempting to start PostgreSQL container..."
            
            $composeFile = Join-Path $ProjectRoot "docker-compose.postgres.yml"
            if (Test-Path $composeFile) {
                docker-compose -f $composeFile up -d
                
                # Wait for PostgreSQL to be ready
                Write-Info "Waiting for PostgreSQL to be ready..."
                Start-Sleep -Seconds 5
                
                # Check again
                $postgresContainer = docker ps --format "table {{.Names}}" | Select-String "postgres"
                if ($postgresContainer) {
                    Write-Success "PostgreSQL container started successfully"
                    return $true
                } else {
                    Write-Error "Failed to start PostgreSQL container"
                    return $false
                }
            } else {
                Write-Error "docker-compose.postgres.yml not found at: $composeFile"
                return $false
            }
        }
    } catch {
        Write-Error "Error checking PostgreSQL: $_"
        return $false
    }
}

# Function to run the database seeder
function Invoke-DatabaseSeeder {
    Write-Info "Running database seeder..."
    
    try {
        # Navigate to scripts directory
        Push-Location $ScriptDir
        
        # Check if psql is available
        try {
            $null = Get-Command psql -ErrorAction Stop
            Write-Info "Using SQL script method..."
            
            # Run the SQL seeding script
            $env:PGPASSWORD = "WitchCity2024!"
            psql -h localhost -p 5432 -U postgres -d witchcityrope_db -f seed-database.sql
            
            if ($LASTEXITCODE -eq 0) {
                Write-Success "Database seeded successfully!"
                return $true
            } else {
                Write-Error "Database seeding failed with exit code: $LASTEXITCODE"
                return $false
            }
        } catch {
            Write-Info "PostgreSQL client not found. Using C# script method..."
            
            # Check if dotnet-script is installed
            try {
                $null = Get-Command dotnet-script -ErrorAction Stop
            } catch {
                Write-Info "Installing dotnet-script tool..."
                dotnet tool install -g dotnet-script
                $env:PATH = "$env:PATH;$env:USERPROFILE\.dotnet\tools"
            }
            
            # Run the C# seeding script
            $env:DB_CONNECTION_STRING = "Host=localhost;Port=5432;Database=witchcityrope_db;Username=postgres;Password=WitchCity2024!"
            dotnet-script SeedDatabase.cs
            
            if ($LASTEXITCODE -eq 0) {
                Write-Success "Database seeded successfully!"
                return $true
            } else {
                Write-Error "Database seeding failed with exit code: $LASTEXITCODE"
                return $false
            }
        }
    } catch {
        Write-Error "Error running seeder: $_"
        return $false
    } finally {
        Pop-Location
    }
}

# Function to verify seeded data
function Test-SeededData {
    Write-Host
    Write-Info "Verifying seeded data..."
    
    try {
        # Query the database to verify data
        $userCount = docker exec postgres psql -U postgres -d witchcityrope_db -t -c 'SELECT COUNT(*) FROM "Users";' 2>$null
        $eventCount = docker exec postgres psql -U postgres -d witchcityrope_db -t -c 'SELECT COUNT(*) FROM "Events";' 2>$null
        
        if ($userCount -match '\d+' -and $eventCount -match '\d+') {
            Write-Success "Data verification passed"
            Write-Info "Users in database: $($userCount.Trim())"
            Write-Info "Events in database: $($eventCount.Trim())"
        }
    } catch {
        Write-Warning "Could not verify data directly, but seeding completed"
    }
    
    # Show summary
    Write-Host
    Write-ColorOutput "=== Seeded Test Accounts ===" "Blue"
    Write-Host "All accounts use password: Test123!"
    Write-Host
    Write-Host "• admin@witchcityrope.com (Administrator)"
    Write-Host "• teacher@witchcityrope.com (Teacher - can organize workshops)"
    Write-Host "• vetted@witchcityrope.com (Vetted Member - trusted community member)"
    Write-Host "• member@witchcityrope.com (Regular Member - has applied for vetting)"
    Write-Host
    Write-Host "Additional test users:"
    Write-Host "• alice@example.com (Vetted Member)"
    Write-Host "• bob@example.com (Member - has pending vetting application)"
    Write-Host "• charlie@example.com (Attendee - basic access)"
}

# Main execution
function Main {
    # Check prerequisites
    try {
        $null = Get-Command docker -ErrorAction Stop
    } catch {
        Write-Error "Docker is not installed or not in PATH"
        Write-Host "Please install Docker Desktop and try again."
        exit 1
    }
    
    try {
        $null = Get-Command dotnet -ErrorAction Stop
    } catch {
        Write-Error ".NET SDK is not installed or not in PATH"
        Write-Host "Please install .NET SDK and try again."
        exit 1
    }
    
    # Check and start PostgreSQL if needed
    if (-not $SkipPostgresCheck) {
        if (-not (Test-PostgreSQL)) {
            Write-Error "Failed to ensure PostgreSQL is running"
            Write-Host "Please start PostgreSQL manually or use -SkipPostgresCheck parameter"
            exit 1
        }
    }
    
    # Run the seeder
    if (-not (Invoke-DatabaseSeeder)) {
        Write-Error "Database seeding failed"
        exit 1
    }
    
    # Verify the data
    Test-SeededData
    
    Write-Host
    Write-ColorOutput "=== Test data seeding completed successfully! ===" "Green"
    Write-Host
    Write-Host "You can now run the application with:"
    Write-Host "  cd $ProjectRoot"
    Write-Host "  dotnet run --project src\WitchCityRope.Web"
    Write-Host
    Write-Host "Or run the API directly:"
    Write-Host "  dotnet run --project src\WitchCityRope.Api"
    
    # Open browser option
    Write-Host
    $openBrowser = Read-Host "Would you like to open the application in your browser? (Y/N)"
    if ($openBrowser -eq 'Y' -or $openBrowser -eq 'y') {
        Start-Process "https://localhost:5653"
    }
}

# Run main function
Main
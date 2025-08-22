# Development startup script for WitchCityRope
# This script ensures all prerequisites are met and starts the application

$ErrorActionPreference = "Stop"

# Colors for output
function Write-Status {
    param([string]$Message)
    Write-Host "[✓] " -ForegroundColor Green -NoNewline
    Write-Host $Message
}

function Write-Error {
    param([string]$Message)
    Write-Host "[✗] " -ForegroundColor Red -NoNewline
    Write-Host $Message
}

function Write-Warning {
    param([string]$Message)
    Write-Host "[!] " -ForegroundColor Yellow -NoNewline
    Write-Host $Message
}

function Write-Info {
    param([string]$Message)
    Write-Host "[i] " -ForegroundColor Blue -NoNewline
    Write-Host $Message
}

# Cleanup function
function Cleanup {
    Write-Host ""
    Write-Warning "Shutting down..."
    
    # Stop any dotnet processes we started
    if ($DotnetProcess) {
        Stop-Process -Id $DotnetProcess.Id -Force -ErrorAction SilentlyContinue
    }
    
    Write-Status "Cleanup complete"
    exit 0
}

# Register cleanup on Ctrl+C
$null = Register-EngineEvent -SourceIdentifier PowerShell.Exiting -Action { Cleanup }

Write-Host "============================================"
Write-Host "   WitchCityRope Development Launcher"
Write-Host "============================================"
Write-Host ""

# Check if we're in the right directory
if (-not (Test-Path "WitchCityRope.sln")) {
    Write-Error "Not in the WitchCityRope root directory!"
    Write-Info "Please run this script from the project root"
    exit 1
}

# Check for .NET SDK
Write-Info "Checking .NET SDK..."
try {
    $dotnetVersion = & dotnet --version 2>$null
    if ($LASTEXITCODE -ne 0) { throw }
    Write-Status ".NET SDK found: $dotnetVersion"
}
catch {
    Write-Error ".NET SDK not found!"
    Write-Info "Please install .NET 9.0 SDK from https://dotnet.microsoft.com/download"
    exit 1
}

# Check for PostgreSQL
Write-Info "Checking PostgreSQL..."
$postgresRunning = $false

# Check if PostgreSQL service is running
$pgService = Get-Service -Name "postgresql*" -ErrorAction SilentlyContinue
if ($pgService -and $pgService.Status -eq "Running") {
    Write-Status "PostgreSQL service is running"
    $postgresRunning = $true
}
else {
    # Check if PostgreSQL is accessible on localhost
    try {
        $tcpClient = New-Object System.Net.Sockets.TcpClient
        $tcpClient.Connect("localhost", 5432)
        $tcpClient.Close()
        Write-Status "PostgreSQL is running on port 5432"
        $postgresRunning = $true
    }
    catch {
        Write-Warning "PostgreSQL is not running on port 5432"
    }
}

# Check Docker containers if local PostgreSQL isn't running
if (-not $postgresRunning) {
    try {
        $dockerRunning = & docker ps 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Info "Checking Docker containers..."
            
            # Check for PostgreSQL containers
            if ($dockerRunning -match "postgres.*5432->5432") {
                Write-Status "PostgreSQL container running on port 5432"
                $postgresRunning = $true
            }
            elseif ($dockerRunning -match "postgres.*5433->5432") {
                Write-Status "PostgreSQL container running on port 5433"
                $postgresRunning = $true
            }
        }
    }
    catch {
        Write-Info "Docker not available"
    }
}

# If PostgreSQL still not found, offer options
if (-not $postgresRunning) {
    Write-Warning "PostgreSQL not detected. Options:"
    Write-Info "1. Start PostgreSQL service (if installed)"
    Write-Info "2. Start Docker containers: docker-compose up -d"
    Write-Host ""
    
    $continue = Read-Host "Continue anyway? (y/N)"
    if ($continue -ne 'y' -and $continue -ne 'Y') {
        exit 1
    }
}

# Check environment configuration
Write-Info "Checking environment configuration..."
if (Test-Path ".env") {
    Write-Status ".env file found"
}
else {
    if (Test-Path ".env.example") {
        Write-Warning ".env file not found. Creating from .env.example..."
        Copy-Item ".env.example" ".env"
        Write-Status ".env file created - please update with your settings"
    }
    else {
        Write-Warning "No .env file found"
    }
}

# Restore NuGet packages
Write-Info "Restoring NuGet packages..."
& dotnet restore
if ($LASTEXITCODE -eq 0) {
    Write-Status "NuGet packages restored"
}
else {
    Write-Error "Failed to restore NuGet packages"
    exit 1
}

# Build the solution
Write-Info "Building solution..."
& dotnet build --no-restore
if ($LASTEXITCODE -eq 0) {
    Write-Status "Solution built successfully"
}
else {
    Write-Error "Build failed!"
    exit 1
}

# Check for pending migrations
Write-Info "Checking database migrations..."
Push-Location "src\WitchCityRope.Infrastructure"

try {
    $migrations = & dotnet ef migrations list --no-build 2>$null
    if ($migrations -match "\(Pending\)") {
        Write-Warning "Pending database migrations detected"
        Write-Host ""
        
        $applyMigrations = Read-Host "Apply migrations now? (y/N)"
        if ($applyMigrations -eq 'y' -or $applyMigrations -eq 'Y') {
            Write-Info "Applying migrations..."
            & dotnet ef database update --no-build
            if ($LASTEXITCODE -eq 0) {
                Write-Status "Migrations applied successfully"
            }
            else {
                Write-Error "Failed to apply migrations"
                Write-Info "You may need to update the connection string in appsettings.json"
            }
        }
    }
    elseif ($LASTEXITCODE -eq 0) {
        Write-Status "Database migrations are up to date"
    }
}
catch {
    Write-Warning "Could not check migration status (database may be unavailable)"
}
finally {
    Pop-Location
}

# Display launch options
Write-Host ""
Write-Host "============================================"
Write-Host "   Launch Options"
Write-Host "============================================"
Write-Host ""
Write-Host "1. Direct launch (ports 8280/8281)"
Write-Host "2. Docker Compose"
Write-Host ""

$option = Read-Host "Select option (1-2)"

switch ($option) {
    "1" {
        # Direct launch
        Write-Host ""
        Write-Host "============================================"
        Write-Host "   Starting WitchCityRope Web Application"
        Write-Host "============================================"
        Write-Host ""
        Write-Info "Starting on:"
        Write-Info "  HTTP:  http://localhost:8280"
        Write-Info "  HTTPS: https://localhost:8281"
        Write-Info "Press Ctrl+C to stop"
        Write-Host ""
        
        Push-Location "src\WitchCityRope.Web"
        try {
            $global:DotnetProcess = Start-Process -FilePath "dotnet" -ArgumentList "run", "--no-build" -PassThru -NoNewWindow -Wait
        }
        finally {
            Pop-Location
        }
    }
    "2" {
        # Docker Compose
        Write-Host ""
        Write-Host "============================================"
        Write-Host "   Starting with Docker Compose"
        Write-Host "============================================"
        Write-Host ""
        
        try {
            $dockerComposeVersion = & docker-compose --version 2>$null
            if ($LASTEXITCODE -ne 0) { throw }
        }
        catch {
            Write-Error "docker-compose not found!"
            exit 1
        }
        
        Write-Info "Building and starting containers..."
        & docker-compose up --build
    }
    default {
        Write-Error "Invalid option"
        exit 1
    }
}
# Script to ensure PostgreSQL is running before building WitchCityRope
# This script will:
# 1. Check and start PostgreSQL if needed
# 2. Run dotnet restore
# 3. Run dotnet build
# 4. Handle errors appropriately

param(
    [switch]$WithTests,
    [alias("t")]
    [switch]$Tests,
    [switch]$Help,
    [alias("h")]
    [switch]$h
)

# Script configuration
$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir

# Colors for output
function Write-ColorOutput($ForegroundColor, $Message) {
    $fc = $host.UI.RawUI.ForegroundColor
    $host.UI.RawUI.ForegroundColor = $ForegroundColor
    Write-Output $Message
    $host.UI.RawUI.ForegroundColor = $fc
}

Write-ColorOutput Cyan "=== WitchCityRope Build with PostgreSQL ==="
Write-Output ""

# Function to display error and exit
function Handle-Error($Message) {
    Write-ColorOutput Red "Error: $Message"
    exit 1
}

# Function to check if we're in the correct directory
function Check-Directory {
    $solutionFile = Join-Path $ProjectRoot "WitchCityRope.sln"
    if (-not (Test-Path $solutionFile)) {
        Handle-Error "WitchCityRope.sln not found. Please run this script from the project root or scripts directory."
    }
}

# Function to ensure PostgreSQL is running
function Ensure-Postgres {
    Write-ColorOutput Yellow "Checking PostgreSQL status..."
    
    $ensurePostgresScript = Join-Path $ScriptDir "ensure-postgres.ps1"
    
    if (Test-Path $ensurePostgresScript) {
        try {
            & $ensurePostgresScript
            if ($LASTEXITCODE -ne 0) {
                Handle-Error "Failed to start PostgreSQL. Please check Docker Desktop is running."
            }
        }
        catch {
            Handle-Error "Failed to run ensure-postgres.ps1: $_"
        }
    }
    else {
        Handle-Error "ensure-postgres.ps1 not found in scripts directory."
    }
    
    Write-Output ""
}

# Function to restore NuGet packages
function Restore-Packages {
    Write-ColorOutput Yellow "Restoring NuGet packages..."
    
    Push-Location $ProjectRoot
    try {
        $result = dotnet restore
        if ($LASTEXITCODE -eq 0) {
            Write-ColorOutput Green "✓ Package restore completed successfully"
        }
        else {
            Handle-Error "Failed to restore NuGet packages"
        }
    }
    finally {
        Pop-Location
    }
    
    Write-Output ""
}

# Function to build the solution
function Build-Solution {
    Write-ColorOutput Yellow "Building solution..."
    
    Push-Location $ProjectRoot
    try {
        $result = dotnet build --no-restore
        if ($LASTEXITCODE -eq 0) {
            Write-ColorOutput Green "✓ Build completed successfully"
        }
        else {
            Handle-Error "Build failed. Please check the error messages above."
        }
    }
    finally {
        Pop-Location
    }
    
    Write-Output ""
}

# Function to run basic tests (optional)
function Run-Tests {
    if ($WithTests -or $Tests) {
        Write-ColorOutput Yellow "Running tests..."
        
        Push-Location $ProjectRoot
        try {
            $result = dotnet test --no-build --no-restore
            if ($LASTEXITCODE -eq 0) {
                Write-ColorOutput Green "✓ All tests passed"
            }
            else {
                Write-ColorOutput Red "⚠ Some tests failed"
                # Don't exit on test failure, just warn
            }
        }
        finally {
            Pop-Location
        }
        
        Write-Output ""
    }
}

# Function to display build summary
function Display-Summary {
    Write-ColorOutput Green "=== Build Summary ==="
    Write-Output "✓ PostgreSQL is running"
    Write-Output "✓ NuGet packages restored"
    Write-Output "✓ Solution built successfully"
    
    if ($WithTests -or $Tests) {
        Write-Output "✓ Tests executed"
    }
    
    Write-Output ""
    Write-ColorOutput Cyan "Next steps:"
    Write-Output "1. Run migrations: dotnet ef database update -s src/WitchCityRope.Api"
    Write-Output "2. Seed test data: .\scripts\seed-test-data.ps1"
    Write-Output "3. Run the application:"
    Write-Output "   - API: dotnet run --project src\WitchCityRope.Api"
    Write-Output "   - Web: dotnet run --project src\WitchCityRope.Web"
    Write-Output "   - Or use Aspire: dotnet run --project src\WitchCityRope.AppHost"
}

# Function to show help
function Show-Help {
    Write-Output "Usage: .\build-with-postgres.ps1 [options]"
    Write-Output ""
    Write-Output "Options:"
    Write-Output "  -Help, -h         Show this help message"
    Write-Output "  -WithTests, -t    Run tests after building"
    Write-Output ""
    Write-Output "This script ensures PostgreSQL is running before building the WitchCityRope solution."
    Write-Output ""
    Write-Output "Examples:"
    Write-Output "  .\build-with-postgres.ps1"
    Write-Output "  .\build-with-postgres.ps1 -WithTests"
    Write-Output "  .\build-with-postgres.ps1 -t"
}

# Main execution
function Main {
    # Check for help flag
    if ($Help -or $h) {
        Show-Help
        exit 0
    }
    
    try {
        # Execute build steps
        Check-Directory
        Ensure-Postgres
        Restore-Packages
        Build-Solution
        Run-Tests
        Display-Summary
    }
    catch {
        Write-ColorOutput Red "An unexpected error occurred: $_"
        exit 1
    }
}

# Handle Ctrl+C gracefully
try {
    Main
}
catch [System.Management.Automation.PipelineStoppedException] {
    Write-Output ""
    Write-ColorOutput Red "Build cancelled by user"
    exit 1
}
catch {
    Write-ColorOutput Red "An error occurred: $_"
    exit 1
}
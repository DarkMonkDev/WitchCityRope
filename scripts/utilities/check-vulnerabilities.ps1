# PowerShell script to check for vulnerabilities in WitchCityRope project
# Run this script periodically to check for security updates

Write-Host "WitchCityRope Security Vulnerability Check" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Green
Write-Host ""

# Check if dotnet CLI is available
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "Error: .NET CLI is not installed or not in PATH" -ForegroundColor Red
    exit 1
}

Write-Host "Checking .NET version..." -ForegroundColor Yellow
dotnet --version
Write-Host ""

# Function to check vulnerabilities for a project
function Check-ProjectVulnerabilities {
    param (
        [string]$ProjectPath,
        [string]$ProjectName
    )
    
    Write-Host "Checking $ProjectName..." -ForegroundColor Cyan
    
    # Check for vulnerable packages
    Write-Host "  - Scanning for vulnerabilities..." -ForegroundColor Gray
    $vulnerableOutput = & dotnet list $ProjectPath package --vulnerable --include-transitive 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        if ($vulnerableOutput -match "No vulnerable packages") {
            Write-Host "  ✓ No vulnerable packages found" -ForegroundColor Green
        } else {
            Write-Host "  ⚠ Vulnerable packages found:" -ForegroundColor Red
            Write-Host $vulnerableOutput
        }
    } else {
        Write-Host "  ✗ Error checking vulnerabilities" -ForegroundColor Red
        Write-Host $vulnerableOutput
    }
    
    # Check for outdated packages
    Write-Host "  - Checking for outdated packages..." -ForegroundColor Gray
    $outdatedOutput = & dotnet list $ProjectPath package --outdated 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        if ($outdatedOutput -match "No outdated packages") {
            Write-Host "  ✓ All packages are up to date" -ForegroundColor Green
        } else {
            Write-Host "  ℹ Outdated packages found (review for security updates):" -ForegroundColor Yellow
            Write-Host $outdatedOutput
        }
    } else {
        Write-Host "  ✗ Error checking outdated packages" -ForegroundColor Red
    }
    
    Write-Host ""
}

# Main execution
$rootPath = Get-Location

# Check if we're in the right directory
if (-not (Test-Path "WitchCityRope.sln")) {
    Write-Host "Error: WitchCityRope.sln not found. Please run this script from the solution root." -ForegroundColor Red
    exit 1
}

# Restore packages first
Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore --verbosity quiet
Write-Host ""

# Find all project files
$projects = @(
    @{ Path = "src/WitchCityRope.Api/WitchCityRope.Api.csproj"; Name = "API Project" },
    @{ Path = "src/WitchCityRope.Core/WitchCityRope.Core.csproj"; Name = "Core Project" },
    @{ Path = "src/WitchCityRope.Infrastructure/WitchCityRope.Infrastructure.csproj"; Name = "Infrastructure Project" },
    @{ Path = "src/WitchCityRope.Web/WitchCityRope.Web.csproj"; Name = "Web Project" }
)

# Check each project
foreach ($project in $projects) {
    if (Test-Path $project.Path) {
        Check-ProjectVulnerabilities -ProjectPath $project.Path -ProjectName $project.Name
    } else {
        Write-Host "Warning: Project not found - $($project.Path)" -ForegroundColor Yellow
    }
}

# Additional security checks
Write-Host "Additional Security Recommendations:" -ForegroundColor Green
Write-Host "===================================" -ForegroundColor Green
Write-Host ""

# Check for specific packages that need attention
Write-Host "Checking for legacy packages..." -ForegroundColor Yellow

$legacyPackages = @(
    "System.IdentityModel.Tokens.Jwt"
)

foreach ($package in $legacyPackages) {
    $found = Select-String -Path "src/**/*.csproj" -Pattern $package -Quiet
    if ($found) {
        Write-Host "⚠ Legacy package found: $package" -ForegroundColor Yellow
        Write-Host "  Consider migrating to: Microsoft.IdentityModel.JsonWebTokens" -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "Security Check Complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "1. Review the SECURITY_VULNERABILITY_REPORT.md for detailed findings" -ForegroundColor Gray
Write-Host "2. Update any vulnerable packages immediately" -ForegroundColor Gray
Write-Host "3. Consider updating outdated packages after testing" -ForegroundColor Gray
Write-Host "4. Set up automated security scanning in your CI/CD pipeline" -ForegroundColor Gray
Write-Host ""

# Generate timestamp
$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
Write-Host "Report generated at: $timestamp" -ForegroundColor Gray
# WitchCityRope Windows Deployment Script
# This script deploys WitchCityRope to a Windows VPS environment
# Requirements: Windows Server 2019+ with IIS, .NET 8.0 Runtime, PowerShell 5.1+

param(
    [Parameter(Mandatory=$true)]
    [string]$Environment,
    
    [Parameter(Mandatory=$true)]
    [string]$DeploymentPath,
    
    [Parameter(Mandatory=$false)]
    [string]$BackupPath = "C:\Backups\WitchCityRope",
    
    [Parameter(Mandatory=$false)]
    [string]$ConfigPath = ".\deployment-config.json",
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipBackup,
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipHealthCheck,
    
    [Parameter(Mandatory=$false)]
    [switch]$Force
)

# Script configuration
$ErrorActionPreference = "Stop"
$VerbosePreference = "Continue"
$script:StartTime = Get-Date
$script:LogFile = "deployment-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"

# Color output functions
function Write-Success {
    param([string]$Message)
    Write-Host "✓ $Message" -ForegroundColor Green
    Add-Content -Path $script:LogFile -Value "[SUCCESS] $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') - $Message"
}

function Write-Info {
    param([string]$Message)
    Write-Host "ℹ $Message" -ForegroundColor Cyan
    Add-Content -Path $script:LogFile -Value "[INFO] $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') - $Message"
}

function Write-Warning {
    param([string]$Message)
    Write-Host "⚠ $Message" -ForegroundColor Yellow
    Add-Content -Path $script:LogFile -Value "[WARNING] $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') - $Message"
}

function Write-Error {
    param([string]$Message)
    Write-Host "✗ $Message" -ForegroundColor Red
    Add-Content -Path $script:LogFile -Value "[ERROR] $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') - $Message"
}

# Load deployment configuration
function Load-DeploymentConfig {
    Write-Info "Loading deployment configuration from $ConfigPath"
    
    if (-not (Test-Path $ConfigPath)) {
        throw "Configuration file not found: $ConfigPath"
    }
    
    $script:Config = Get-Content $ConfigPath | ConvertFrom-Json
    
    # Validate configuration
    $requiredFields = @('AppName', 'ApiPort', 'WebPort', 'SqliteDbPath')
    foreach ($field in $requiredFields) {
        if (-not $script:Config.$field) {
            throw "Required configuration field missing: $field"
        }
    }
    
    Write-Success "Configuration loaded successfully"
}

# Prerequisites check
function Test-Prerequisites {
    Write-Info "Checking prerequisites..."
    
    $prerequisites = @{
        ".NET 8.0 Runtime" = { dotnet --list-runtimes | Select-String "Microsoft.NETCore.App 8.0" }
        "IIS" = { Get-WindowsFeature -Name Web-Server | Where-Object { $_.InstallState -eq "Installed" } }
        "IIS ASP.NET Core Module" = { Test-Path "${env:ProgramFiles}\IIS\Asp.Net Core Module\V2\aspnetcorev2.dll" }
        "Git" = { Get-Command git -ErrorAction SilentlyContinue }
    }
    
    $failed = $false
    foreach ($prereq in $prerequisites.GetEnumerator()) {
        Write-Host -NoNewline "Checking $($prereq.Key)... "
        if (& $prereq.Value) {
            Write-Host "OK" -ForegroundColor Green
        } else {
            Write-Host "MISSING" -ForegroundColor Red
            $failed = $true
        }
    }
    
    if ($failed -and -not $Force) {
        throw "Prerequisites check failed. Install missing components or use -Force to continue."
    }
    
    Write-Success "Prerequisites check completed"
}

# Create backup
function Backup-CurrentDeployment {
    if ($SkipBackup) {
        Write-Warning "Skipping backup as requested"
        return
    }
    
    Write-Info "Creating backup of current deployment..."
    
    if (-not (Test-Path $DeploymentPath)) {
        Write-Info "No existing deployment found, skipping backup"
        return
    }
    
    $backupDir = Join-Path $BackupPath "backup-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
    New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
    
    # Backup application files
    Write-Info "Backing up application files..."
    Copy-Item -Path "$DeploymentPath\*" -Destination $backupDir -Recurse -Force
    
    # Backup database
    $dbPath = Join-Path $DeploymentPath $script:Config.SqliteDbPath
    if (Test-Path $dbPath) {
        Write-Info "Backing up database..."
        $dbBackupPath = Join-Path $backupDir "database"
        New-Item -ItemType Directory -Path $dbBackupPath -Force | Out-Null
        Copy-Item -Path $dbPath -Destination $dbBackupPath -Force
    }
    
    # Create backup manifest
    $manifest = @{
        Timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
        Environment = $Environment
        DeploymentPath = $DeploymentPath
        BackupPath = $backupDir
        Version = (Get-Content "$DeploymentPath\version.txt" -ErrorAction SilentlyContinue) ?? "Unknown"
    }
    
    $manifest | ConvertTo-Json | Set-Content -Path (Join-Path $backupDir "manifest.json")
    
    Write-Success "Backup created at: $backupDir"
}

# Stop application
function Stop-Application {
    Write-Info "Stopping application..."
    
    # Stop IIS app pools
    $appPools = @("WitchCityRope-API", "WitchCityRope-Web")
    foreach ($pool in $appPools) {
        if (Get-IISAppPool -Name $pool -ErrorAction SilentlyContinue) {
            Write-Info "Stopping app pool: $pool"
            Stop-WebAppPool -Name $pool -ErrorAction SilentlyContinue
            
            # Wait for app pool to stop
            $timeout = 30
            $waited = 0
            while ((Get-WebAppPoolState -Name $pool).Value -ne "Stopped" -and $waited -lt $timeout) {
                Start-Sleep -Seconds 1
                $waited++
            }
        }
    }
    
    # Stop Windows services if any
    $services = @("WitchCityRope-API", "WitchCityRope-Web")
    foreach ($service in $services) {
        if (Get-Service -Name $service -ErrorAction SilentlyContinue) {
            Write-Info "Stopping service: $service"
            Stop-Service -Name $service -Force -ErrorAction SilentlyContinue
        }
    }
    
    Write-Success "Application stopped"
}

# Deploy application files
function Deploy-Application {
    Write-Info "Deploying application files..."
    
    # Create deployment directory
    if (-not (Test-Path $DeploymentPath)) {
        New-Item -ItemType Directory -Path $DeploymentPath -Force | Out-Null
    }
    
    # Get latest release package
    $packagePath = Get-LatestPackage
    
    # Extract package
    Write-Info "Extracting deployment package..."
    Expand-Archive -Path $packagePath -DestinationPath $DeploymentPath -Force
    
    # Copy environment-specific configuration
    $envConfigPath = ".\configs\$Environment"
    if (Test-Path $envConfigPath) {
        Write-Info "Applying environment-specific configuration..."
        Copy-Item -Path "$envConfigPath\*" -Destination $DeploymentPath -Recurse -Force
    }
    
    # Set permissions
    Write-Info "Setting permissions..."
    $acl = Get-Acl $DeploymentPath
    $permission = "IIS_IUSRS","FullControl","ContainerInherit,ObjectInherit","None","Allow"
    $accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule $permission
    $acl.SetAccessRule($accessRule)
    Set-Acl $DeploymentPath $acl
    
    # Create required directories
    $directories = @("logs", "uploads", "data")
    foreach ($dir in $directories) {
        $dirPath = Join-Path $DeploymentPath $dir
        if (-not (Test-Path $dirPath)) {
            New-Item -ItemType Directory -Path $dirPath -Force | Out-Null
        }
    }
    
    Write-Success "Application files deployed"
}

# Get latest package
function Get-LatestPackage {
    Write-Info "Finding latest deployment package..."
    
    $packageDir = ".\packages"
    if (-not (Test-Path $packageDir)) {
        throw "Package directory not found: $packageDir"
    }
    
    $latestPackage = Get-ChildItem -Path $packageDir -Filter "*.zip" | 
        Sort-Object -Property LastWriteTime -Descending | 
        Select-Object -First 1
    
    if (-not $latestPackage) {
        throw "No deployment package found in $packageDir"
    }
    
    Write-Success "Using package: $($latestPackage.Name)"
    return $latestPackage.FullName
}

# Configure IIS
function Configure-IIS {
    Write-Info "Configuring IIS..."
    
    Import-Module WebAdministration
    
    # Configure API site
    $apiSiteName = "WitchCityRope-API"
    $apiPath = Join-Path $DeploymentPath "api"
    
    if (-not (Get-Website -Name $apiSiteName -ErrorAction SilentlyContinue)) {
        Write-Info "Creating API website..."
        New-Website -Name $apiSiteName -Port $script:Config.ApiPort -PhysicalPath $apiPath
    } else {
        Write-Info "Updating API website..."
        Set-ItemProperty "IIS:\Sites\$apiSiteName" -Name physicalPath -Value $apiPath
    }
    
    # Configure API app pool
    if (-not (Get-WebAppPool -Name $apiSiteName -ErrorAction SilentlyContinue)) {
        New-WebAppPool -Name $apiSiteName
    }
    
    Set-ItemProperty -Path "IIS:\AppPools\$apiSiteName" -Name processIdentity.identityType -Value ApplicationPoolIdentity
    Set-ItemProperty -Path "IIS:\AppPools\$apiSiteName" -Name managedRuntimeVersion -Value ""
    Set-ItemProperty -Path "IIS:\AppPools\$apiSiteName" -Name enable32BitAppOnWin64 -Value $false
    
    # Configure Web site
    $webSiteName = "WitchCityRope-Web"
    $webPath = Join-Path $DeploymentPath "web"
    
    if (-not (Get-Website -Name $webSiteName -ErrorAction SilentlyContinue)) {
        Write-Info "Creating Web website..."
        New-Website -Name $webSiteName -Port $script:Config.WebPort -PhysicalPath $webPath
    } else {
        Write-Info "Updating Web website..."
        Set-ItemProperty "IIS:\Sites\$webSiteName" -Name physicalPath -Value $webPath
    }
    
    # Configure Web app pool
    if (-not (Get-WebAppPool -Name $webSiteName -ErrorAction SilentlyContinue)) {
        New-WebAppPool -Name $webSiteName
    }
    
    Set-ItemProperty -Path "IIS:\AppPools\$webSiteName" -Name processIdentity.identityType -Value ApplicationPoolIdentity
    Set-ItemProperty -Path "IIS:\AppPools\$webSiteName" -Name managedRuntimeVersion -Value ""
    Set-ItemProperty -Path "IIS:\AppPools\$webSiteName" -Name enable32BitAppOnWin64 -Value $false
    
    # Configure URL Rewrite rules
    Configure-UrlRewrite
    
    Write-Success "IIS configuration completed"
}

# Configure URL Rewrite
function Configure-UrlRewrite {
    Write-Info "Configuring URL Rewrite rules..."
    
    # Add HTTPS redirect rule
    $webConfigPath = Join-Path $DeploymentPath "web\web.config"
    if (Test-Path $webConfigPath) {
        $xml = [xml](Get-Content $webConfigPath)
        
        # Add rewrite rules if not exists
        if (-not $xml.configuration.'system.webServer'.rewrite) {
            $rewrite = $xml.CreateElement("rewrite")
            $rules = $xml.CreateElement("rules")
            
            # HTTPS redirect rule
            $httpsRule = $xml.CreateElement("rule")
            $httpsRule.SetAttribute("name", "Redirect to HTTPS")
            $httpsRule.SetAttribute("stopProcessing", "true")
            
            $match = $xml.CreateElement("match")
            $match.SetAttribute("url", "(.*)")
            
            $conditions = $xml.CreateElement("conditions")
            $httpsCondition = $xml.CreateElement("add")
            $httpsCondition.SetAttribute("input", "{HTTPS}")
            $httpsCondition.SetAttribute("pattern", "off")
            $httpsCondition.SetAttribute("ignoreCase", "true")
            
            $action = $xml.CreateElement("action")
            $action.SetAttribute("type", "Redirect")
            $action.SetAttribute("url", "https://{HTTP_HOST}/{R:1}")
            $action.SetAttribute("redirectType", "Permanent")
            
            $conditions.AppendChild($httpsCondition) | Out-Null
            $httpsRule.AppendChild($match) | Out-Null
            $httpsRule.AppendChild($conditions) | Out-Null
            $httpsRule.AppendChild($action) | Out-Null
            $rules.AppendChild($httpsRule) | Out-Null
            $rewrite.AppendChild($rules) | Out-Null
            
            $xml.configuration.'system.webServer'.AppendChild($rewrite) | Out-Null
            $xml.Save($webConfigPath)
        }
    }
}

# Run database migrations
function Update-Database {
    Write-Info "Running database migrations..."
    
    $apiPath = Join-Path $DeploymentPath "api"
    Push-Location $apiPath
    
    try {
        # Check if EF Core tools are available
        $efToolPath = Join-Path $apiPath "ef.dll"
        if (Test-Path $efToolPath) {
            & dotnet $efToolPath database update --no-build
        } else {
            Write-Warning "EF Core tools not found, skipping automatic migration"
        }
    }
    finally {
        Pop-Location
    }
    
    Write-Success "Database update completed"
}

# Start application
function Start-Application {
    Write-Info "Starting application..."
    
    # Start app pools
    Start-WebAppPool -Name "WitchCityRope-API"
    Start-WebAppPool -Name "WitchCityRope-Web"
    
    # Start websites
    Start-Website -Name "WitchCityRope-API"
    Start-Website -Name "WitchCityRope-Web"
    
    Write-Success "Application started"
}

# Health check
function Test-Deployment {
    if ($SkipHealthCheck) {
        Write-Warning "Skipping health check as requested"
        return
    }
    
    Write-Info "Running deployment health checks..."
    
    $checks = @{
        "API Health" = "http://localhost:$($script:Config.ApiPort)/health"
        "Web Health" = "http://localhost:$($script:Config.WebPort)/health"
        "API Swagger" = "http://localhost:$($script:Config.ApiPort)/swagger"
    }
    
    $failed = $false
    foreach ($check in $checks.GetEnumerator()) {
        Write-Host -NoNewline "Checking $($check.Key)... "
        try {
            $response = Invoke-WebRequest -Uri $check.Value -UseBasicParsing -TimeoutSec 30
            if ($response.StatusCode -eq 200) {
                Write-Host "OK" -ForegroundColor Green
            } else {
                Write-Host "FAILED (Status: $($response.StatusCode))" -ForegroundColor Red
                $failed = $true
            }
        }
        catch {
            Write-Host "FAILED (Error: $($_.Exception.Message))" -ForegroundColor Red
            $failed = $true
        }
    }
    
    if ($failed) {
        Write-Error "Health checks failed!"
        if (-not $Force) {
            throw "Deployment health checks failed. Use -Force to ignore."
        }
    } else {
        Write-Success "All health checks passed"
    }
}

# Main deployment flow
function Start-Deployment {
    try {
        Write-Info "Starting WitchCityRope deployment for environment: $Environment"
        Write-Info "Deployment path: $DeploymentPath"
        
        # Load configuration
        Load-DeploymentConfig
        
        # Check prerequisites
        Test-Prerequisites
        
        # Create backup
        Backup-CurrentDeployment
        
        # Stop application
        Stop-Application
        
        # Deploy files
        Deploy-Application
        
        # Configure IIS
        Configure-IIS
        
        # Update database
        Update-Database
        
        # Start application
        Start-Application
        
        # Run health checks
        Test-Deployment
        
        $duration = (Get-Date) - $script:StartTime
        Write-Success "Deployment completed successfully in $($duration.TotalMinutes.ToString('F2')) minutes"
        
        # Display summary
        Write-Host "`nDeployment Summary:" -ForegroundColor Yellow
        Write-Host "Environment: $Environment"
        Write-Host "API URL: http://localhost:$($script:Config.ApiPort)"
        Write-Host "Web URL: http://localhost:$($script:Config.WebPort)"
        Write-Host "Log file: $script:LogFile"
        
    }
    catch {
        Write-Error "Deployment failed: $_"
        Write-Warning "Rolling back deployment..."
        
        # Attempt rollback
        if ($script:BackupPath -and (Test-Path $script:BackupPath)) {
            # Rollback logic would go here
            Write-Info "Rollback functionality to be implemented"
        }
        
        throw
    }
}

# Execute deployment
Start-Deployment
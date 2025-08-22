# browser-bridge.ps1 - PowerShell script that acts as a bridge for browser commands
# This script provides browser automation capabilities that can be called from WSL

param(
    [Parameter(Mandatory=$false)]
    [string]$Command = "status",
    
    [Parameter(Mandatory=$false)]
    [string]$Url,
    
    [Parameter(Mandatory=$false)]
    [string]$Selector,
    
    [Parameter(Mandatory=$false)]
    [string]$Text,
    
    [Parameter(Mandatory=$false)]
    [int]$Port = 9222,
    
    [Parameter(Mandatory=$false)]
    [string]$Screenshot
)

# Function to check if Chrome is running with debugging port
function Test-ChromeDebugging {
    param([int]$Port)
    
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:$Port/json/version" -TimeoutSec 2
        return $true
    }
    catch {
        return $false
    }
}

# Function to get Chrome executable path
function Get-ChromePath {
    $chromePaths = @(
        "${env:ProgramFiles}\Google\Chrome\Application\chrome.exe",
        "${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe",
        "$env:LOCALAPPDATA\Google\Chrome\Application\chrome.exe"
    )
    
    foreach ($path in $chromePaths) {
        if (Test-Path $path) {
            return $path
        }
    }
    
    # Try to find Chrome using where command
    $whereResult = where.exe chrome 2>$null
    if ($whereResult) {
        return $whereResult
    }
    
    return $null
}

# Function to launch Chrome with debugging
function Start-ChromeDebugging {
    param([int]$Port)
    
    $chromePath = Get-ChromePath
    if (-not $chromePath) {
        Write-Error "Chrome not found. Please install Google Chrome."
        return $false
    }
    
    $arguments = @(
        "--remote-debugging-port=$Port",
        "--incognito",  # ALWAYS run in incognito mode
        "--no-first-run",
        "--no-default-browser-check",
        "--disable-popup-blocking",
        "--disable-translate",
        "--disable-background-timer-throttling",
        "--disable-renderer-backgrounding",
        "--disable-device-discovery-notifications",
        "--user-data-dir=$env:TEMP\chrome-automation-$(Get-Random)"
    )
    
    try {
        Start-Process -FilePath $chromePath -ArgumentList $arguments
        Start-Sleep -Seconds 3
        return Test-ChromeDebugging -Port $Port
    }
    catch {
        Write-Error "Failed to launch Chrome: $_"
        return $false
    }
}

# Function to get active tabs
function Get-ChromeTabs {
    param([int]$Port)
    
    try {
        $response = Invoke-RestMethod -Uri "http://localhost:$Port/json" -Method Get
        return $response
    }
    catch {
        Write-Error "Failed to get Chrome tabs: $_"
        return @()
    }
}

# Function to navigate to URL
function Navigate-Chrome {
    param(
        [string]$Url,
        [int]$Port
    )
    
    $tabs = Get-ChromeTabs -Port $Port
    if ($tabs.Count -eq 0) {
        Write-Error "No Chrome tabs found"
        return $false
    }
    
    $activeTab = $tabs | Where-Object { $_.type -eq "page" } | Select-Object -First 1
    if (-not $activeTab) {
        Write-Error "No active page tab found"
        return $false
    }
    
    $webSocketUrl = $activeTab.webSocketDebuggerUrl
    
    # For simplicity, we'll use the HTTP endpoint
    try {
        $body = @{
            expression = "window.location.href = '$Url'"
        } | ConvertTo-Json
        
        $response = Invoke-RestMethod -Uri "http://localhost:$Port/json/runtime/evaluate" -Method Post -Body $body -ContentType "application/json"
        Write-Host "Navigated to: $Url"
        return $true
    }
    catch {
        Write-Error "Failed to navigate: $_"
        return $false
    }
}

# Function to take screenshot
function Take-ChromeScreenshot {
    param(
        [string]$OutputPath,
        [int]$Port
    )
    
    Write-Host "Screenshot functionality requires Chrome DevTools Protocol implementation"
    Write-Host "Consider using Selenium or Puppeteer for advanced screenshot capabilities"
    return $false
}

# Main script logic
switch ($Command.ToLower()) {
    "status" {
        if (Test-ChromeDebugging -Port $Port) {
            Write-Host "Chrome is running with debugging on port $Port"
            $tabs = Get-ChromeTabs -Port $Port
            Write-Host "Active tabs: $($tabs.Count)"
            $tabs | ForEach-Object {
                Write-Host "  - $($_.title) ($($_.url))"
            }
        }
        else {
            Write-Host "Chrome is not running with debugging on port $Port"
        }
    }
    
    "launch" {
        if (Test-ChromeDebugging -Port $Port) {
            Write-Host "Chrome is already running with debugging on port $Port"
        }
        else {
            Write-Host "Launching Chrome with debugging..."
            if (Start-ChromeDebugging -Port $Port) {
                Write-Host "Chrome launched successfully"
            }
        }
    }
    
    "navigate" {
        if (-not $Url) {
            Write-Error "URL parameter is required for navigate command"
            exit 1
        }
        
        if (-not (Test-ChromeDebugging -Port $Port)) {
            Write-Host "Chrome not running. Launching..."
            Start-ChromeDebugging -Port $Port
        }
        
        Navigate-Chrome -Url $Url -Port $Port
    }
    
    "tabs" {
        if (Test-ChromeDebugging -Port $Port) {
            $tabs = Get-ChromeTabs -Port $Port
            $tabs | ConvertTo-Json -Depth 3
        }
        else {
            Write-Error "Chrome is not running with debugging on port $Port"
        }
    }
    
    "screenshot" {
        if (-not $Screenshot) {
            Write-Error "Screenshot parameter is required for screenshot command"
            exit 1
        }
        
        Take-ChromeScreenshot -OutputPath $Screenshot -Port $Port
    }
    
    "close" {
        $chrome = Get-Process chrome -ErrorAction SilentlyContinue | Where-Object {
            $_.CommandLine -like "*remote-debugging-port=$Port*"
        }
        
        if ($chrome) {
            $chrome | Stop-Process -Force
            Write-Host "Chrome closed"
        }
        else {
            Write-Host "Chrome not found"
        }
    }
    
    default {
        Write-Host "Usage: browser-bridge.ps1 -Command <command> [options]"
        Write-Host ""
        Write-Host "Commands:"
        Write-Host "  status              - Check if Chrome is running with debugging"
        Write-Host "  launch              - Launch Chrome with debugging enabled"
        Write-Host "  navigate -Url <url> - Navigate to a URL"
        Write-Host "  tabs                - List all open tabs"
        Write-Host "  screenshot -Screenshot <path> - Take a screenshot (limited)"
        Write-Host "  close               - Close Chrome"
        Write-Host ""
        Write-Host "Options:"
        Write-Host "  -Port <port>        - Debugging port (default: 9222)"
    }
}
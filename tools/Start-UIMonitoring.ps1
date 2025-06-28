# Start UI Monitoring for WitchCityRope
param(
    [string]$ProjectPath = "C:\Users\chad\source\repos\WitchCityRope",
    [int]$Port = 5652,
    [switch]$SkipBuild,
    [switch]$OpenBrowser
)

Write-Host "`n🎭 WitchCityRope UI Monitoring Setup" -ForegroundColor Magenta
Write-Host "====================================" -ForegroundColor Magenta

# Check if Node.js is installed
try {
    $nodeVersion = node --version
    Write-Host "✅ Node.js $nodeVersion detected" -ForegroundColor Green
} catch {
    Write-Host "❌ Node.js not found. Please install Node.js 18+" -ForegroundColor Red
    exit 1
}

# Navigate to project directory
Set-Location $ProjectPath

# Install npm dependencies for monitoring if needed
if (!(Test-Path "$ProjectPath\tools\node_modules")) {
    Write-Host "`n📦 Installing monitoring dependencies..." -ForegroundColor Yellow
    Set-Location "$ProjectPath\tools"
    npm init -y | Out-Null
    npm install chokidar --save | Out-Null
    Set-Location $ProjectPath
}

# Build the project if not skipped
if (!$SkipBuild) {
    Write-Host "`n🔨 Building WitchCityRope..." -ForegroundColor Yellow
    dotnet build --configuration Debug
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Build failed!" -ForegroundColor Red
        exit 1
    }
    Write-Host "✅ Build successful" -ForegroundColor Green
}

# Check if application is already running
$existingProcess = Get-NetTCPConnection -LocalPort $Port -ErrorAction SilentlyContinue
if ($existingProcess) {
    Write-Host "`n⚠️  Port $Port is already in use" -ForegroundColor Yellow
    $response = Read-Host "Kill existing process? (Y/N)"
    if ($response -eq 'Y') {
        $existingProcess | Select-Object -Property OwningProcess -Unique | ForEach-Object {
            Stop-Process -Id $_.OwningProcess -Force
        }
        Start-Sleep -Seconds 2
    } else {
        Write-Host "Using existing application instance" -ForegroundColor Cyan
    }
}

# Start the application if not running
if (!(Get-NetTCPConnection -LocalPort $Port -ErrorAction SilentlyContinue)) {
    Write-Host "`n🚀 Starting WitchCityRope application..." -ForegroundColor Yellow
    
    $env:ASPNETCORE_ENVIRONMENT = "Development"
    $env:ASPNETCORE_URLS = "https://localhost:$Port"
    
    $webProcess = Start-Process -FilePath "dotnet" `
        -ArgumentList "run --project `"$ProjectPath\src\WitchCityRope.Web`" --launch-profile https" `
        -PassThru -NoNewWindow
    
    # Wait for application to start
    Write-Host "⏳ Waiting for application to start on port $Port..." -ForegroundColor Cyan
    $attempts = 0
    $maxAttempts = 30
    
    while ($attempts -lt $maxAttempts) {
        Start-Sleep -Seconds 2
        try {
            $response = Invoke-WebRequest -Uri "https://localhost:$Port" -SkipCertificateCheck -ErrorAction SilentlyContinue
            if ($response.StatusCode -eq 200) {
                Write-Host "✅ Application started successfully!" -ForegroundColor Green
                break
            }
        } catch {
            $attempts++
            Write-Host "." -NoNewline
        }
    }
    
    if ($attempts -eq $maxAttempts) {
        Write-Host "`n❌ Failed to start application" -ForegroundColor Red
        exit 1
    }
}

# Open browser if requested
if ($OpenBrowser) {
    Write-Host "`n🌐 Opening browser..." -ForegroundColor Cyan
    Start-Process "https://localhost:$Port"
}

# Create snapshots directory
$snapshotDir = "$ProjectPath\ui-snapshots"
if (!(Test-Path $snapshotDir)) {
    New-Item -ItemType Directory -Path $snapshotDir | Out-Null
    Write-Host "📁 Created snapshot directory: $snapshotDir" -ForegroundColor Green
}

# Start UI monitor
Write-Host "`n👁️  Starting UI change monitor..." -ForegroundColor Yellow
Write-Host "📸 Screenshots will be saved to: $snapshotDir" -ForegroundColor Cyan
Write-Host "🔍 Monitoring for changes in:" -ForegroundColor Cyan
Write-Host "   - src\WitchCityRope.Web\Features" -ForegroundColor Gray
Write-Host "   - src\WitchCityRope.Web\Pages" -ForegroundColor Gray
Write-Host "   - src\WitchCityRope.Web\Components" -ForegroundColor Gray
Write-Host "   - src\WitchCityRope.Web\wwwroot\css" -ForegroundColor Gray
Write-Host "`nPress Ctrl+C to stop monitoring`n" -ForegroundColor Yellow

# Run the monitor
node "$ProjectPath\tools\ui-monitor.js"
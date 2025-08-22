# Quick launcher for WitchCityRope development
# Simply calls the main development startup script

$ScriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$DevStartScript = Join-Path $ScriptPath "scripts\dev-start.ps1"

# Check if the script exists
if (-not (Test-Path $DevStartScript)) {
    Write-Host "Error: Development startup script not found at: $DevStartScript" -ForegroundColor Red
    exit 1
}

# Run the development startup script
& $DevStartScript $args
# WitchCityRope Quick Start Script (PowerShell Version)
# This script starts PostgreSQL and seeds test data in one command

Write-Host "=== WitchCityRope Quick Start ===" -ForegroundColor Blue
Write-Host
Write-Host "This script will:" -ForegroundColor Yellow
Write-Host "1. Ensure PostgreSQL is running in Docker"
Write-Host "2. Seed the database with test data"
Write-Host "3. Provide instructions to run the application"
Write-Host

# Get script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# Ensure PostgreSQL is running first
Write-Host "Checking PostgreSQL status..." -ForegroundColor Yellow
& "$ScriptDir\ensure-postgres.ps1"
if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to start PostgreSQL" -ForegroundColor Red
    exit 1
}

# Run the seed script (which handles database seeding)
& "$ScriptDir\seed-test-data.ps1"

Write-Host
Write-Host "Quick start complete!" -ForegroundColor Green
Write-Host
Write-Host "To stop PostgreSQL when done:"
Write-Host "  docker-compose -f docker-compose.postgres.yml down"
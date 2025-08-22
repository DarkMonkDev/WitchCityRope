# Simple Docker Compose startup script for WitchCityRope

Write-Host "Starting WitchCityRope with Docker Compose..." -ForegroundColor Green
Write-Host

# Check if .env exists
if (-not (Test-Path .env)) {
    if (Test-Path .env.example) {
        Write-Host "Creating .env from .env.example..." -ForegroundColor Yellow
        Copy-Item .env.example .env
        Write-Host "Please update .env with your configuration values"
        Write-Host
    }
}

# Start services
docker-compose up -d --build

Write-Host
Write-Host "Services are starting..." -ForegroundColor Green
Write-Host
Write-Host "Web Application: https://localhost:5652"
Write-Host "API Service: https://localhost:5654"
Write-Host "PostgreSQL: localhost:5433"
Write-Host
Write-Host "To view logs: docker-compose logs -f"
Write-Host "To stop: docker-compose down"
Write-Host
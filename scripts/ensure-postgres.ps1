# Script to ensure PostgreSQL container is running for WitchCityRope
# Supports both Aspire orchestration and Docker Compose setups

param(
    [switch]$Force = $false
)

# Configuration
$PostgresPassword = "WitchCity2024!"
$PostgresUser = "postgres"
$PostgresDb = "witchcityrope_db"

# Colors for output
function Write-Success { Write-Host $args[0] -ForegroundColor Green }
function Write-Warning { Write-Host $args[0] -ForegroundColor Yellow }
function Write-Error { Write-Host $args[0] -ForegroundColor Red }

Write-Success "=== WitchCityRope PostgreSQL Container Manager ==="
Write-Host ""

# Function to check if Docker is running
function Test-Docker {
    try {
        $null = docker info 2>&1
        return $true
    }
    catch {
        return $false
    }
}

# Function to find PostgreSQL containers
function Find-PostgresContainers {
    $containers = @{
        AspireRunning = @()
        ComposeRunning = @()
        AspireStopped = @()
        ComposeStopped = @()
    }
    
    # Check for running containers
    $runningContainers = docker ps --format "{{.Names}}" 2>$null
    if ($runningContainers) {
        $containers.AspireRunning = $runningContainers | Where-Object { $_ -match "witchcityrope\.apphost-.*-postgres" }
        $containers.ComposeRunning = $runningContainers | Where-Object { $_ -eq "witchcityrope-db" -or $_ -eq "witchcityrope-postgres" }
    }
    
    # Check for stopped containers
    $allContainers = docker ps -a --format "{{.Names}}|{{.Status}}" 2>$null
    if ($allContainers) {
        foreach ($container in $allContainers) {
            $parts = $container -split '\|'
            $name = $parts[0]
            $status = $parts[1]
            
            if ($status -match "Exited") {
                if ($name -match "witchcityrope\.apphost-.*-postgres") {
                    $containers.AspireStopped += $name
                }
                elseif ($name -eq "witchcityrope-db" -or $name -eq "witchcityrope-postgres") {
                    $containers.ComposeStopped += $name
                }
            }
        }
    }
    
    return $containers
}

# Function to start a stopped container
function Start-Container {
    param([string]$ContainerName)
    
    Write-Warning "Starting stopped container: $ContainerName"
    
    try {
        docker start $ContainerName | Out-Null
        Write-Success "✓ Container $ContainerName started successfully"
        
        # Wait for PostgreSQL to be ready
        Write-Host "Waiting for PostgreSQL to be ready..."
        $maxAttempts = 30
        $attempt = 0
        
        while ($attempt -lt $maxAttempts) {
            $ready = docker exec $ContainerName pg_isready -U $PostgresUser 2>$null
            if ($LASTEXITCODE -eq 0) {
                Write-Success "✓ PostgreSQL is ready!"
                return $true
            }
            
            Write-Host "." -NoNewline
            Start-Sleep -Seconds 1
            $attempt++
        }
        
        Write-Host ""
        Write-Warning "Warning: PostgreSQL may not be fully ready yet"
        return $true
    }
    catch {
        Write-Error "Failed to start container $ContainerName"
        return $false
    }
}

# Function to check if Docker Compose file exists
function Test-ComposeSetup {
    return (Test-Path "docker-compose.yml") -or (Test-Path "docker-compose.postgres.yml")
}

# Function to start PostgreSQL using Docker Compose
function Start-WithCompose {
    Write-Warning "Starting PostgreSQL with Docker Compose..."
    
    # Check for postgres-specific compose file first
    if (Test-Path "docker-compose.postgres.yml") {
        Write-Host "Using docker-compose.postgres.yml"
        try {
            docker-compose -f docker-compose.postgres.yml up -d
            if ($LASTEXITCODE -eq 0) {
                Write-Success "✓ PostgreSQL started with Docker Compose (postgres config)"
                return $true
            }
        }
        catch {
            Write-Error "Failed to start with docker-compose.postgres.yml"
        }
    }
    elseif (Test-Path "docker-compose.yml") {
        Write-Host "Using docker-compose.yml"
        # Check if the compose file has a db service
        $composeContent = Get-Content "docker-compose.yml" -Raw
        if ($composeContent -match "^\s*db:" -or $composeContent -match "services:\s*\n\s*db:") {
            try {
                docker-compose up -d db
                if ($LASTEXITCODE -eq 0) {
                    Write-Success "✓ PostgreSQL started with Docker Compose"
                    return $true
                }
            }
            catch {
                Write-Error "Failed to start with docker-compose.yml"
            }
        }
        else {
            Write-Warning "No 'db' service found in docker-compose.yml"
        }
    }
    
    return $false
}

# Function to run PostgreSQL with direct Docker command
function Start-PostgresDirect {
    Write-Warning "Starting PostgreSQL with direct Docker run..."
    
    $containerName = "witchcityrope-postgres-standalone"
    
    # Remove any existing container with this name
    docker rm -f $containerName 2>$null | Out-Null
    
    # Run PostgreSQL container
    $dockerArgs = @(
        "run", "-d",
        "--name", $containerName,
        "-e", "POSTGRES_PASSWORD=$PostgresPassword",
        "-e", "POSTGRES_USER=$PostgresUser",
        "-e", "POSTGRES_DB=$PostgresDb",
        "-p", "5432:5432",
        "-v", "witchcityrope_postgres_data:/var/lib/postgresql/data",
        "postgres:16-alpine"
    )
    
    try {
        docker @dockerArgs | Out-Null
        if ($LASTEXITCODE -eq 0) {
            Write-Success "✓ PostgreSQL container started successfully"
            Write-Host "Container name: $containerName"
            Write-Host "Port: 5432"
            Write-Host "Database: $PostgresDb"
            Write-Host "User: $PostgresUser"
            
            # Wait for PostgreSQL to be ready
            Write-Host "Waiting for PostgreSQL to initialize..."
            Start-Sleep -Seconds 5
            
            $maxAttempts = 30
            $attempt = 0
            
            while ($attempt -lt $maxAttempts) {
                $ready = docker exec $containerName pg_isready -U $PostgresUser 2>$null
                if ($LASTEXITCODE -eq 0) {
                    Write-Success "✓ PostgreSQL is ready!"
                    return $true
                }
                
                Write-Host "." -NoNewline
                Start-Sleep -Seconds 1
                $attempt++
            }
            
            Write-Host ""
            Write-Warning "Warning: PostgreSQL may not be fully ready yet"
            return $true
        }
    }
    catch {
        Write-Error "Failed to start PostgreSQL container: $_"
    }
    
    return $false
}

# Function to display connection info
function Show-ConnectionInfo {
    param(
        [string]$Port,
        [string]$Type
    )
    
    Write-Host ""
    Write-Host "Connection info:"
    Write-Host "  Host: localhost"
    Write-Host "  Port: $Port ($Type)"
    Write-Host "  Database: $PostgresDb"
    Write-Host "  User: $PostgresUser"
    Write-Host "  Password: $PostgresPassword"
}

# Main logic
if (-not (Test-Docker)) {
    Write-Error "Error: Docker is not running or not accessible"
    Write-Host "Please ensure Docker Desktop is running and try again."
    exit 1
}

$containers = Find-PostgresContainers

# Check if any PostgreSQL container is already running
if ($containers.AspireRunning.Count -gt 0) {
    Write-Success "✓ Aspire-managed PostgreSQL is already running"
    Write-Host "Container: $($containers.AspireRunning[0])"
    Show-ConnectionInfo -Port "15432" -Type "Aspire default"
    exit 0
}
elseif ($containers.ComposeRunning.Count -gt 0) {
    Write-Success "✓ Docker Compose PostgreSQL is already running"
    Write-Host "Container: $($containers.ComposeRunning[0])"
    Show-ConnectionInfo -Port "5433" -Type "Docker Compose default"
    exit 0
}

# No running containers, check for stopped ones
if ($containers.AspireStopped.Count -gt 0) {
    Write-Warning "Found stopped Aspire PostgreSQL container"
    $containerToStart = $containers.AspireStopped[0]
    if (Start-Container -ContainerName $containerToStart) {
        Show-ConnectionInfo -Port "15432" -Type "Aspire default"
        exit 0
    }
}
elseif ($containers.ComposeStopped.Count -gt 0) {
    Write-Warning "Found stopped Docker Compose PostgreSQL container"
    if (Start-Container -ContainerName $containers.ComposeStopped[0]) {
        Show-ConnectionInfo -Port "5433" -Type "Docker Compose default"
        exit 0
    }
}

# No existing containers, need to create new one
Write-Warning "No existing PostgreSQL containers found"

# Try Docker Compose first if available
if (Test-ComposeSetup) {
    if (Start-WithCompose) {
        Show-ConnectionInfo -Port "5433" -Type "Docker Compose default"
        exit 0
    }
}

# Fall back to direct Docker run
Write-Host "Falling back to direct Docker run..."
if (Start-PostgresDirect) {
    Show-ConnectionInfo -Port "5432" -Type "default"
    exit 0
}
else {
    Write-Error "Failed to start PostgreSQL container"
    Write-Host "Please check Docker logs for more information."
    exit 1
}
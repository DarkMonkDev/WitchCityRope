#!/bin/bash

# Script to ensure PostgreSQL container is running for WitchCityRope
# Supports both Aspire orchestration and Docker Compose setups

set -e

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Configuration
POSTGRES_PASSWORD="WitchCity2024!"
POSTGRES_USER="postgres"
POSTGRES_DB="witchcityrope_db"

echo -e "${GREEN}=== WitchCityRope PostgreSQL Container Manager ===${NC}"
echo ""

# Function to check if Docker is running
check_docker() {
    if ! docker info >/dev/null 2>&1; then
        echo -e "${RED}Error: Docker is not running or not accessible${NC}"
        echo "Please ensure Docker Desktop is running and try again."
        exit 1
    fi
}

# Function to find running PostgreSQL containers
find_postgres_containers() {
    # Check for Aspire-managed PostgreSQL containers
    ASPIRE_CONTAINERS=$(docker ps --filter "name=witchcityrope.apphost-.*-postgres" --format "{{.Names}}" 2>/dev/null || true)
    
    # Check for Docker Compose PostgreSQL container (both possible names)
    COMPOSE_CONTAINER=$(docker ps --filter "name=witchcityrope-db" --format "{{.Names}}" 2>/dev/null || true)
    if [ -z "$COMPOSE_CONTAINER" ]; then
        COMPOSE_CONTAINER=$(docker ps --filter "name=witchcityrope-postgres" --format "{{.Names}}" 2>/dev/null || true)
    fi
    
    # Check for any stopped containers that match our patterns
    ASPIRE_STOPPED=$(docker ps -a --filter "name=witchcityrope.apphost-.*-postgres" --filter "status=exited" --format "{{.Names}}" 2>/dev/null || true)
    COMPOSE_STOPPED=$(docker ps -a --filter "name=witchcityrope-db" --filter "status=exited" --format "{{.Names}}" 2>/dev/null || true)
    if [ -z "$COMPOSE_STOPPED" ]; then
        COMPOSE_STOPPED=$(docker ps -a --filter "name=witchcityrope-postgres" --filter "status=exited" --format "{{.Names}}" 2>/dev/null || true)
    fi
}

# Function to start a stopped container
start_container() {
    local container_name=$1
    echo -e "${YELLOW}Starting stopped container: ${container_name}${NC}"
    
    if docker start "$container_name"; then
        echo -e "${GREEN}✓ Container ${container_name} started successfully${NC}"
        
        # Wait for PostgreSQL to be ready
        echo "Waiting for PostgreSQL to be ready..."
        local max_attempts=30
        local attempt=0
        
        while [ $attempt -lt $max_attempts ]; do
            if docker exec "$container_name" pg_isready -U "$POSTGRES_USER" >/dev/null 2>&1; then
                echo -e "${GREEN}✓ PostgreSQL is ready!${NC}"
                return 0
            fi
            
            echo -n "."
            sleep 1
            attempt=$((attempt + 1))
        done
        
        echo -e "\n${RED}Warning: PostgreSQL may not be fully ready yet${NC}"
    else
        echo -e "${RED}Failed to start container ${container_name}${NC}"
        return 1
    fi
}

# Function to check if Docker Compose file exists
check_compose_setup() {
    if [ -f "docker-compose.yml" ] || [ -f "docker-compose.postgres.yml" ]; then
        return 0
    fi
    return 1
}

# Function to start PostgreSQL using Docker Compose
start_with_compose() {
    echo -e "${YELLOW}Starting PostgreSQL with Docker Compose...${NC}"
    
    # Check for postgres-specific compose file first
    if [ -f "docker-compose.postgres.yml" ]; then
        echo "Using docker-compose.postgres.yml"
        if docker-compose -f docker-compose.postgres.yml up -d; then
            echo -e "${GREEN}✓ PostgreSQL started with Docker Compose (postgres config)${NC}"
            return 0
        fi
    elif [ -f "docker-compose.yml" ]; then
        echo "Using docker-compose.yml"
        # Check if the compose file has a db service
        if grep -q "^\s*db:" docker-compose.yml 2>/dev/null; then
            if docker-compose up -d db; then
                echo -e "${GREEN}✓ PostgreSQL started with Docker Compose${NC}"
                return 0
            fi
        else
            echo -e "${YELLOW}No 'db' service found in docker-compose.yml${NC}"
            return 1
        fi
    fi
    
    return 1
}

# Function to run PostgreSQL with direct Docker command
run_postgres_direct() {
    echo -e "${YELLOW}Starting PostgreSQL with direct Docker run...${NC}"
    
    local container_name="witchcityrope-postgres-standalone"
    
    # Remove any existing container with this name
    docker rm -f "$container_name" >/dev/null 2>&1 || true
    
    # Run PostgreSQL container
    if docker run -d \
        --name "$container_name" \
        -e POSTGRES_PASSWORD="$POSTGRES_PASSWORD" \
        -e POSTGRES_USER="$POSTGRES_USER" \
        -e POSTGRES_DB="$POSTGRES_DB" \
        -p 5432:5432 \
        -v witchcityrope_postgres_data:/var/lib/postgresql/data \
        postgres:16-alpine; then
        
        echo -e "${GREEN}✓ PostgreSQL container started successfully${NC}"
        echo "Container name: $container_name"
        echo "Port: 5432"
        echo "Database: $POSTGRES_DB"
        echo "User: $POSTGRES_USER"
        
        # Wait for PostgreSQL to be ready
        echo "Waiting for PostgreSQL to initialize..."
        sleep 5
        
        local max_attempts=30
        local attempt=0
        
        while [ $attempt -lt $max_attempts ]; do
            if docker exec "$container_name" pg_isready -U "$POSTGRES_USER" >/dev/null 2>&1; then
                echo -e "${GREEN}✓ PostgreSQL is ready!${NC}"
                return 0
            fi
            
            echo -n "."
            sleep 1
            attempt=$((attempt + 1))
        done
        
        echo -e "\n${RED}Warning: PostgreSQL may not be fully ready yet${NC}"
    else
        echo -e "${RED}Failed to start PostgreSQL container${NC}"
        return 1
    fi
}

# Main logic
check_docker
find_postgres_containers

# Check if any PostgreSQL container is already running
if [ -n "$ASPIRE_CONTAINERS" ]; then
    echo -e "${GREEN}✓ Aspire-managed PostgreSQL is already running${NC}"
    echo "Container: $ASPIRE_CONTAINERS"
    echo ""
    echo "Connection info:"
    echo "  Host: localhost"
    echo "  Port: 15432 (Aspire default)"
    echo "  Database: $POSTGRES_DB"
    echo "  User: $POSTGRES_USER"
    echo "  Password: $POSTGRES_PASSWORD"
    exit 0
elif [ -n "$COMPOSE_CONTAINER" ]; then
    echo -e "${GREEN}✓ Docker Compose PostgreSQL is already running${NC}"
    echo "Container: $COMPOSE_CONTAINER"
    echo ""
    echo "Connection info:"
    echo "  Host: localhost"
    echo "  Port: 5433 (Docker Compose default)"
    echo "  Database: $POSTGRES_DB"
    echo "  User: $POSTGRES_USER"
    echo "  Password: $POSTGRES_PASSWORD"
    exit 0
fi

# No running containers, check for stopped ones
if [ -n "$ASPIRE_STOPPED" ]; then
    echo -e "${YELLOW}Found stopped Aspire PostgreSQL container${NC}"
    # Get the first container name if multiple
    CONTAINER_TO_START=$(echo "$ASPIRE_STOPPED" | head -n 1)
    if start_container "$CONTAINER_TO_START"; then
        echo ""
        echo "Connection info:"
        echo "  Host: localhost"
        echo "  Port: 15432 (Aspire default)"
        echo "  Database: $POSTGRES_DB"
        echo "  User: $POSTGRES_USER"
        echo "  Password: $POSTGRES_PASSWORD"
        exit 0
    fi
elif [ -n "$COMPOSE_STOPPED" ]; then
    echo -e "${YELLOW}Found stopped Docker Compose PostgreSQL container${NC}"
    if start_container "$COMPOSE_STOPPED"; then
        echo ""
        echo "Connection info:"
        echo "  Host: localhost"
        echo "  Port: 5433 (Docker Compose default)"
        echo "  Database: $POSTGRES_DB"
        echo "  User: $POSTGRES_USER"
        echo "  Password: $POSTGRES_PASSWORD"
        exit 0
    fi
fi

# No existing containers, need to create new one
echo -e "${YELLOW}No existing PostgreSQL containers found${NC}"

# Try Docker Compose first if available
if check_compose_setup; then
    if start_with_compose; then
        echo ""
        echo "Connection info:"
        echo "  Host: localhost"
        echo "  Port: 5433 (Docker Compose default)"
        echo "  Database: $POSTGRES_DB"
        echo "  User: $POSTGRES_USER"
        echo "  Password: $POSTGRES_PASSWORD"
        exit 0
    fi
fi

# Fall back to direct Docker run
echo "Falling back to direct Docker run..."
if run_postgres_direct; then
    echo ""
    echo "Connection info:"
    echo "  Host: localhost"
    echo "  Port: 5432 (default)"
    echo "  Database: $POSTGRES_DB"
    echo "  User: $POSTGRES_USER"
    echo "  Password: $POSTGRES_PASSWORD"
    exit 0
else
    echo -e "${RED}Failed to start PostgreSQL container${NC}"
    echo "Please check Docker logs for more information."
    exit 1
fi
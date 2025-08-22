#!/bin/bash

# Development startup script for WitchCityRope
# This script ensures all prerequisites are met and starts the application

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${GREEN}[✓]${NC} $1"
}

print_error() {
    echo -e "${RED}[✗]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[!]${NC} $1"
}

print_info() {
    echo -e "${BLUE}[i]${NC} $1"
}

# Cleanup function
cleanup() {
    echo ""
    print_warning "Shutting down..."
    
    # Kill any dotnet processes we started
    if [ ! -z "$DOTNET_PID" ]; then
        kill $DOTNET_PID 2>/dev/null || true
    fi
    
    print_status "Cleanup complete"
    exit 0
}

# Set up trap for Ctrl+C
trap cleanup INT TERM

echo "============================================"
echo "   WitchCityRope Development Launcher"
echo "============================================"
echo ""

# Check if we're in the right directory
if [ ! -f "WitchCityRope.sln" ]; then
    print_error "Not in the WitchCityRope root directory!"
    print_info "Please run this script from the project root"
    exit 1
fi

# Check for .NET SDK
print_info "Checking .NET SDK..."
if ! command -v dotnet &> /dev/null; then
    print_error ".NET SDK not found!"
    print_info "Please install .NET 9.0 SDK from https://dotnet.microsoft.com/download"
    exit 1
fi

DOTNET_VERSION=$(dotnet --version)
print_status ".NET SDK found: $DOTNET_VERSION"

# Check for PostgreSQL
print_info "Checking PostgreSQL..."
POSTGRES_RUNNING=false

# First check if running locally
if command -v psql &> /dev/null; then
    if pg_isready -h localhost -p 5432 &> /dev/null; then
        print_status "PostgreSQL is running on port 5432"
        POSTGRES_RUNNING=true
    else
        print_warning "PostgreSQL is installed but not running on port 5432"
    fi
fi

# Check Docker containers if local PostgreSQL isn't running
if [ "$POSTGRES_RUNNING" = false ] && command -v docker &> /dev/null; then
    print_info "Checking Docker containers..."
    
    # Check if Docker daemon is running
    if docker ps &> /dev/null; then
        # Check for PostgreSQL on different ports
        if docker ps | grep -q "postgres.*5432->5432"; then
            print_status "PostgreSQL container running on port 5432"
            POSTGRES_RUNNING=true
        elif docker ps | grep -q "postgres.*5433->5432"; then
            print_status "PostgreSQL container running on port 5433"
            POSTGRES_RUNNING=true
        fi
    else
        print_warning "Docker is installed but daemon is not running"
    fi
fi

# If PostgreSQL still not found, offer to start it
if [ "$POSTGRES_RUNNING" = false ]; then
    print_warning "PostgreSQL not detected. Options:"
    print_info "1. Start PostgreSQL locally: sudo service postgresql start"
    print_info "2. Start Docker containers: docker-compose up -d"
    echo ""
    read -p "Continue anyway? (y/N) " -n 1 -r
    echo ""
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        exit 1
    fi
fi

# Check environment configuration
print_info "Checking environment configuration..."
if [ -f ".env" ]; then
    print_status ".env file found"
else
    if [ -f ".env.example" ]; then
        print_warning ".env file not found. Creating from .env.example..."
        cp .env.example .env
        print_status ".env file created - please update with your settings"
    else
        print_warning "No .env file found"
    fi
fi

# Restore NuGet packages
print_info "Restoring NuGet packages..."
if dotnet restore; then
    print_status "NuGet packages restored"
else
    print_error "Failed to restore NuGet packages"
    exit 1
fi

# Build the solution
print_info "Building solution..."
if dotnet build --no-restore; then
    print_status "Solution built successfully"
else
    print_error "Build failed!"
    exit 1
fi

# Check for pending migrations
print_info "Checking database migrations..."
cd src/WitchCityRope.Infrastructure

# Try to check migrations (may fail if database isn't accessible)
if dotnet ef migrations list --no-build 2>/dev/null | grep -q "(Pending)"; then
    print_warning "Pending database migrations detected"
    echo ""
    read -p "Apply migrations now? (y/N) " -n 1 -r
    echo ""
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        print_info "Applying migrations..."
        if dotnet ef database update --no-build; then
            print_status "Migrations applied successfully"
        else
            print_error "Failed to apply migrations"
            print_info "You may need to update the connection string in appsettings.json"
        fi
    fi
else
    # If the command succeeded, migrations are up to date
    if [ $? -eq 0 ]; then
        print_status "Database migrations are up to date"
    else
        print_warning "Could not check migration status (database may be unavailable)"
    fi
fi
cd ../..

# Display launch options
echo ""
echo "============================================"
echo "   Launch Options"
echo "============================================"
echo ""
echo "1. Direct launch (ports 8280/8281)"
echo "2. Docker Compose"
echo ""
read -p "Select option (1-2): " -n 1 -r
echo ""

case $REPLY in
    1)
        # Direct launch
        echo ""
        echo "============================================"
        echo "   Starting WitchCityRope Web Application"
        echo "============================================"
        echo ""
        print_info "Starting on:"
        print_info "  HTTP:  http://localhost:8280"
        print_info "  HTTPS: https://localhost:8281"
        print_info "Press Ctrl+C to stop"
        echo ""
        
        cd src/WitchCityRope.Web
        exec dotnet run --no-build
        ;;
    2)
        # Docker Compose
        echo ""
        echo "============================================"
        echo "   Starting with Docker Compose"
        echo "============================================"
        echo ""
        
        if ! command -v docker-compose &> /dev/null; then
            print_error "docker-compose not found!"
            exit 1
        fi
        
        print_info "Building and starting containers..."
        docker-compose up --build
        ;;
    *)
        print_error "Invalid option"
        exit 1
        ;;
esac
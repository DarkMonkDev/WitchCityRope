#!/bin/bash

# Script to ensure PostgreSQL is running before building WitchCityRope
# This script will:
# 1. Check and start PostgreSQL if needed
# 2. Run dotnet restore
# 3. Run dotnet build
# 4. Handle errors appropriately

set -e

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Script directory
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT_ROOT="$( cd "$SCRIPT_DIR/.." && pwd )"

echo -e "${BLUE}=== WitchCityRope Build with PostgreSQL ===${NC}"
echo ""

# Function to display error and exit
handle_error() {
    echo -e "${RED}Error: $1${NC}"
    exit 1
}

# Function to check if we're in the correct directory
check_directory() {
    if [ ! -f "$PROJECT_ROOT/WitchCityRope.sln" ]; then
        handle_error "WitchCityRope.sln not found. Please run this script from the project root or scripts directory."
    fi
}

# Function to ensure PostgreSQL is running
ensure_postgres() {
    echo -e "${YELLOW}Checking PostgreSQL status...${NC}"
    
    # Run the ensure-postgres.sh script
    if [ -f "$SCRIPT_DIR/ensure-postgres.sh" ]; then
        if ! "$SCRIPT_DIR/ensure-postgres.sh"; then
            handle_error "Failed to start PostgreSQL. Please check Docker Desktop is running."
        fi
    else
        handle_error "ensure-postgres.sh not found in scripts directory."
    fi
    
    echo ""
}

# Function to restore NuGet packages
restore_packages() {
    echo -e "${YELLOW}Restoring NuGet packages...${NC}"
    
    cd "$PROJECT_ROOT"
    if dotnet restore; then
        echo -e "${GREEN}✓ Package restore completed successfully${NC}"
    else
        handle_error "Failed to restore NuGet packages"
    fi
    
    echo ""
}

# Function to build the solution
build_solution() {
    echo -e "${YELLOW}Building solution...${NC}"
    
    cd "$PROJECT_ROOT"
    
    # Build with detailed output
    if dotnet build --no-restore; then
        echo -e "${GREEN}✓ Build completed successfully${NC}"
    else
        handle_error "Build failed. Please check the error messages above."
    fi
    
    echo ""
}

# Function to run basic tests (optional)
run_tests() {
    if [ "$1" == "--with-tests" ] || [ "$1" == "-t" ]; then
        echo -e "${YELLOW}Running tests...${NC}"
        
        cd "$PROJECT_ROOT"
        if dotnet test --no-build --no-restore; then
            echo -e "${GREEN}✓ All tests passed${NC}"
        else
            echo -e "${RED}⚠ Some tests failed${NC}"
            # Don't exit on test failure, just warn
        fi
        
        echo ""
    fi
}

# Function to display build summary
display_summary() {
    echo -e "${GREEN}=== Build Summary ===${NC}"
    echo "✓ PostgreSQL is running"
    echo "✓ NuGet packages restored"
    echo "✓ Solution built successfully"
    
    if [ "$1" == "--with-tests" ] || [ "$1" == "-t" ]; then
        echo "✓ Tests executed"
    fi
    
    echo ""
    echo -e "${BLUE}Next steps:${NC}"
    echo "1. Run migrations: dotnet ef database update -s src/WitchCityRope.Api"
    echo "2. Seed test data: ./scripts/seed-test-data.sh"
    echo "3. Run the application:"
    echo "   - API: dotnet run --project src/WitchCityRope.Api"
    echo "   - Web: dotnet run --project src/WitchCityRope.Web"
    echo "   - Or use Aspire: dotnet run --project src/WitchCityRope.AppHost"
}

# Main execution
main() {
    local run_tests_flag=""
    
    # Check for command line arguments
    if [ "$1" == "--help" ] || [ "$1" == "-h" ]; then
        echo "Usage: $0 [options]"
        echo ""
        echo "Options:"
        echo "  -h, --help       Show this help message"
        echo "  -t, --with-tests Run tests after building"
        echo ""
        echo "This script ensures PostgreSQL is running before building the WitchCityRope solution."
        exit 0
    fi
    
    if [ "$1" == "--with-tests" ] || [ "$1" == "-t" ]; then
        run_tests_flag="$1"
    fi
    
    # Execute build steps
    check_directory
    ensure_postgres
    restore_packages
    build_solution
    run_tests "$run_tests_flag"
    display_summary "$run_tests_flag"
}

# Handle Ctrl+C gracefully
trap 'echo -e "\n${RED}Build cancelled by user${NC}"; exit 1' INT

# Run main function with all arguments
main "$@"
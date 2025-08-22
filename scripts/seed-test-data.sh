#!/bin/bash

# WitchCityRope Test Data Seeding Script
# This script ensures the database has proper test data for development and testing

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Script directory
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

echo -e "${BLUE}=== WitchCityRope Test Data Seeder ===${NC}"
echo

# Function to check if PostgreSQL is running
check_postgres() {
    echo -e "${YELLOW}Checking PostgreSQL status...${NC}"
    
    # Check if PostgreSQL container is running
    if docker ps | grep -q "postgres"; then
        echo -e "${GREEN}✓ PostgreSQL container is running${NC}"
        return 0
    else
        echo -e "${RED}✗ PostgreSQL container is not running${NC}"
        echo -e "${YELLOW}Starting PostgreSQL container...${NC}"
        
        # Try to start PostgreSQL using docker-compose
        if [ -f "$PROJECT_ROOT/docker-compose.postgres.yml" ]; then
            docker-compose -f "$PROJECT_ROOT/docker-compose.postgres.yml" up -d
            
            # Wait for PostgreSQL to be ready
            echo -e "${YELLOW}Waiting for PostgreSQL to be ready...${NC}"
            sleep 5
            
            # Check again
            if docker ps | grep -q "postgres"; then
                echo -e "${GREEN}✓ PostgreSQL container started successfully${NC}"
                return 0
            else
                echo -e "${RED}✗ Failed to start PostgreSQL container${NC}"
                return 1
            fi
        else
            echo -e "${RED}✗ docker-compose.postgres.yml not found${NC}"
            return 1
        fi
    fi
}

# Function to run the database seeder
run_seeder() {
    echo -e "${YELLOW}Running database seeder...${NC}"
    
    # Navigate to scripts directory
    cd "$SCRIPT_DIR"
    
    # Check if psql is available
    if command -v psql &> /dev/null; then
        echo -e "${YELLOW}Using SQL script method...${NC}"
        
        # Run the SQL seeding script
        PGPASSWORD="WitchCity2024!" psql -h localhost -p 5432 -U postgres -d witchcityrope_db -f seed-database.sql
        SEEDER_EXIT_CODE=$?
    else
        echo -e "${YELLOW}Using C# script method...${NC}"
        
        # Check if dotnet-script is installed
        if ! command -v dotnet-script &> /dev/null; then
            echo -e "${YELLOW}Installing dotnet-script tool...${NC}"
            dotnet tool install -g dotnet-script
            export PATH="$PATH:$HOME/.dotnet/tools"
        fi
        
        # Run the C# seeding script
        export DB_CONNECTION_STRING="Host=localhost;Port=5432;Database=witchcityrope_db;Username=postgres;Password=WitchCity2024!"
        dotnet-script SeedDatabase.cs
        SEEDER_EXIT_CODE=$?
    fi
    
    if [ $SEEDER_EXIT_CODE -eq 0 ]; then
        echo -e "${GREEN}✓ Database seeded successfully!${NC}"
        return 0
    else
        echo -e "${RED}✗ Database seeding failed with exit code: $SEEDER_EXIT_CODE${NC}"
        return 1
    fi
}

# Function to verify seeded data
verify_data() {
    echo
    echo -e "${YELLOW}Verifying seeded data...${NC}"
    
    # Use psql to check the data
    docker exec postgres psql -U postgres -d witchcityrope_db -c "SELECT COUNT(*) as user_count FROM \"Users\";" 2>/dev/null | grep -q "[0-9]" && \
    docker exec postgres psql -U postgres -d witchcityrope_db -c "SELECT COUNT(*) as event_count FROM \"Events\";" 2>/dev/null | grep -q "[0-9]" && \
    echo -e "${GREEN}✓ Data verification passed${NC}"
    
    # Show summary
    echo
    echo -e "${BLUE}=== Seeded Test Accounts ===${NC}"
    echo "All accounts use password: Test123!"
    echo
    echo "• admin@witchcityrope.com (Administrator)"
    echo "• teacher@witchcityrope.com (Teacher - can organize workshops)"
    echo "• vetted@witchcityrope.com (Vetted Member - trusted community member)"
    echo "• member@witchcityrope.com (Regular Member - has applied for vetting)"
    echo
    echo "Additional test users:"
    echo "• alice@example.com (Vetted Member)"
    echo "• bob@example.com (Member - has pending vetting application)"
    echo "• charlie@example.com (Attendee - basic access)"
}

# Main execution
main() {
    # Check prerequisites
    if ! command -v docker &> /dev/null; then
        echo -e "${RED}✗ Docker is not installed or not in PATH${NC}"
        echo "Please install Docker and try again."
        exit 1
    fi
    
    if ! command -v dotnet &> /dev/null; then
        echo -e "${RED}✗ .NET SDK is not installed or not in PATH${NC}"
        echo "Please install .NET SDK and try again."
        exit 1
    fi
    
    # Check and start PostgreSQL if needed
    if ! check_postgres; then
        echo -e "${RED}Failed to ensure PostgreSQL is running${NC}"
        echo "Please start PostgreSQL manually and try again."
        exit 1
    fi
    
    # Run the seeder
    if ! run_seeder; then
        echo -e "${RED}Database seeding failed${NC}"
        exit 1
    fi
    
    # Verify the data
    verify_data
    
    echo
    echo -e "${GREEN}=== Test data seeding completed successfully! ===${NC}"
    echo
    echo "You can now run the application with:"
    echo "  cd $PROJECT_ROOT"
    echo "  dotnet run --project src/WitchCityRope.Web"
    echo
    echo "Or run the API directly:"
    echo "  dotnet run --project src/WitchCityRope.Api"
}

# Run main function
main
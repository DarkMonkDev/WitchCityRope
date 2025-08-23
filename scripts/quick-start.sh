#!/bin/bash

# WitchCityRope Quick Start Script
# This script starts PostgreSQL and seeds test data in one command

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

echo -e "${BLUE}=== WitchCityRope Quick Start ===${NC}"
echo
echo -e "${YELLOW}This script will:${NC}"
echo "1. Ensure PostgreSQL is running in Docker"
echo "2. Seed the database with test data"
echo "3. Provide instructions to run the application"
echo

# Ensure PostgreSQL is running first
echo -e "${YELLOW}Checking PostgreSQL status...${NC}"
"$SCRIPT_DIR/ensure-postgres.sh"
if [ $? -ne 0 ]; then
    echo -e "${RED}Failed to start PostgreSQL${NC}"
    exit 1
fi

# Run the seed script (which handles database seeding)
"$SCRIPT_DIR/seed-test-data.sh"

echo
echo -e "${GREEN}Quick start complete!${NC}"
echo
echo "To stop PostgreSQL when done:"
echo "  docker-compose -f docker-compose.postgres.yml down"
#!/bin/bash

# Simple Docker Compose startup script for WitchCityRope

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${GREEN}Starting WitchCityRope with Docker Compose...${NC}"
echo

# Check if .env exists
if [ ! -f .env ]; then
    if [ -f .env.example ]; then
        echo -e "${YELLOW}Creating .env from .env.example...${NC}"
        cp .env.example .env
        echo "Please update .env with your configuration values"
        echo
    fi
fi

# Start services
docker-compose up -d --build

echo
echo -e "${GREEN}Services are starting...${NC}"
echo
echo "Web Application: https://localhost:5652"
echo "API Service: https://localhost:5654"
echo "PostgreSQL: localhost:5433"
echo
echo "To view logs: docker-compose logs -f"
echo "To stop: docker-compose down"
echo
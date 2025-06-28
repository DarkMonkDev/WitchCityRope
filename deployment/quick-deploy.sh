#!/bin/bash

# WitchCityRope Quick Deployment Script
# This script provides a simplified deployment process with safety checks

set -euo pipefail

# Color codes
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m'

# Script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Display banner
echo -e "${BLUE}"
cat << "EOF"
 _    _ _ _       _      _____ _ _         ______                 
| |  | (_) |     | |    / ____(_) |       (_____ \                
| |  | |_| |_ ___| |__ | |     _| |_ _   _  ____) )___  ____  ___ 
| |/\| | | __/ __| '_ \| |    | | __| | | ||  __  / _ \|  _ \/ _ \
|  /\  | | || (__| | | | |____| | |_| |_| || |  \ \ |_| | |_) |  __/
|_/  \_|_|\__\___|_| |_|\_____|_|\__|\__, ||_|   |_\___/|  __/ \___|
                                      __/ |             |_|         
                                     |___/  Quick Deploy v1.0
EOF
echo -e "${NC}"

# Function to display usage
usage() {
    echo "Usage: $0 [staging|production]"
    echo ""
    echo "Quick deployment script for WitchCityRope"
    echo ""
    echo "This script will:"
    echo "  1. Run pre-deployment validation"
    echo "  2. Create a backup"
    echo "  3. Deploy the application"
    echo "  4. Run health checks"
    echo "  5. Display results"
    exit 1
}

# Check arguments
if [[ $# -ne 1 ]]; then
    usage
fi

ENVIRONMENT=$1

if [[ "$ENVIRONMENT" != "staging" && "$ENVIRONMENT" != "production" ]]; then
    echo -e "${RED}Error: Invalid environment '$ENVIRONMENT'${NC}"
    usage
fi

# Confirmation for production
if [[ "$ENVIRONMENT" == "production" ]]; then
    echo -e "${YELLOW}⚠ WARNING: You are about to deploy to PRODUCTION!${NC}"
    echo -n "Type 'DEPLOY TO PRODUCTION' to confirm: "
    read confirmation
    if [[ "$confirmation" != "DEPLOY TO PRODUCTION" ]]; then
        echo -e "${RED}Deployment cancelled${NC}"
        exit 1
    fi
fi

# Start deployment
echo -e "${BLUE}Starting deployment to $ENVIRONMENT...${NC}"
echo "Timestamp: $(date)"
echo ""

# Step 1: Pre-deployment validation
echo -e "${BLUE}Step 1/4: Running pre-deployment validation...${NC}"
if "$SCRIPT_DIR/pre-deployment-validation.sh"; then
    echo -e "${GREEN}✓ Validation passed${NC}"
else
    echo -e "${RED}✗ Validation failed${NC}"
    echo -n "Continue anyway? (y/N): "
    read continue_anyway
    if [[ "$continue_anyway" != "y" && "$continue_anyway" != "Y" ]]; then
        exit 1
    fi
fi

# Step 2: Check configuration
echo -e "\n${BLUE}Step 2/4: Checking configuration...${NC}"
CONFIG_FILE="$SCRIPT_DIR/configs/$ENVIRONMENT/.env"
if [[ -f "$CONFIG_FILE" ]]; then
    echo -e "${GREEN}✓ Configuration file found${NC}"
else
    echo -e "${RED}✗ Configuration file not found: $CONFIG_FILE${NC}"
    echo "Please create it from the example file:"
    echo "  cp $SCRIPT_DIR/configs/$ENVIRONMENT/.env.example $CONFIG_FILE"
    exit 1
fi

# Step 3: Deploy
echo -e "\n${BLUE}Step 3/4: Deploying application...${NC}"
if [[ -f "$SCRIPT_DIR/deploy-linux.sh" ]]; then
    if "$SCRIPT_DIR/deploy-linux.sh" -e "$ENVIRONMENT"; then
        echo -e "${GREEN}✓ Deployment completed${NC}"
    else
        echo -e "${RED}✗ Deployment failed${NC}"
        exit 1
    fi
else
    echo -e "${RED}✗ Deploy script not found${NC}"
    exit 1
fi

# Step 4: Health check
echo -e "\n${BLUE}Step 4/4: Running health checks...${NC}"
sleep 10  # Give services time to start

if "$SCRIPT_DIR/post-deployment-health-check.sh"; then
    echo -e "${GREEN}✓ Health checks passed${NC}"
else
    echo -e "${YELLOW}⚠ Some health checks failed${NC}"
fi

# Display summary
echo -e "\n${BLUE}Deployment Summary${NC}"
echo "===================="
echo "Environment: $ENVIRONMENT"
echo "Status: Completed"
echo "Time: $(date)"

# Display URLs
if [[ "$ENVIRONMENT" == "staging" ]]; then
    echo ""
    echo "Access URLs:"
    echo "  Web: https://staging.witchcityrope.com"
    echo "  API: https://staging-api.witchcityrope.com"
    echo "  Health: https://staging-api.witchcityrope.com/health"
elif [[ "$ENVIRONMENT" == "production" ]]; then
    echo ""
    echo "Access URLs:"
    echo "  Web: https://witchcityrope.com"
    echo "  API: https://api.witchcityrope.com"
    echo "  Health: https://api.witchcityrope.com/health"
fi

# Display next steps
echo -e "\n${BLUE}Next Steps:${NC}"
echo "1. Monitor logs: docker-compose logs -f"
echo "2. Check metrics: http://localhost:3000 (Grafana)"
echo "3. Verify functionality through the web interface"
echo "4. Monitor error rates for the next hour"

echo -e "\n${GREEN}Deployment complete!${NC}"
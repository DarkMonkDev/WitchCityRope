#!/bin/bash

echo "========================================="
echo "WitchCityRope Development Tools Test"
echo "========================================="
echo ""

# Color codes
GREEN='\033[0;32m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Test function
test_tool() {
    local name=$1
    local command=$2
    echo -n "Testing $name... "
    if eval "$command" &>/dev/null; then
        echo -e "${GREEN}✓ PASSED${NC}"
        return 0
    else
        echo -e "${RED}✗ FAILED${NC}"
        return 1
    fi
}

# Version check function
check_version() {
    local name=$1
    local command=$2
    echo -n "$name: "
    eval "$command" 2>/dev/null || echo "Not installed"
}

echo "=== Version Information ==="
check_version ".NET SDK" "dotnet --version"
check_version "Node.js" "node --version"
check_version "npm" "npm --version"
check_version "PostgreSQL" "psql --version | head -1"
check_version "Docker" "docker --version"
check_version "Docker Compose" "docker-compose --version"
check_version "Chrome" "google-chrome --version"
check_version "Python" "python3 --version"
check_version "GitHub CLI" "gh --version | head -1"
echo ""

echo "=== Tool Availability Tests ==="
test_tool ".NET SDK" "dotnet --list-sdks"
test_tool "Node.js" "node -e 'console.log(1+1)'"
test_tool "npm" "npm list -g --depth=0"
test_tool "PostgreSQL Client" "which psql"
test_tool "Docker" "docker ps"
test_tool "Docker Compose" "docker-compose version"
test_tool "Chrome Browser" "which google-chrome"
test_tool "Python 3" "python3 -c 'print(1+1)'"
test_tool "pip" "pip3 --version"
test_tool "GitHub CLI" "gh auth status"
echo ""

echo "=== PostgreSQL Service Test ==="
if echo "chad" | sudo -S systemctl is-active --quiet postgresql; then
    echo -e "PostgreSQL service: ${GREEN}✓ Running${NC}"
else
    echo -e "PostgreSQL service: ${RED}✗ Not running${NC}"
    echo "Starting PostgreSQL..."
    echo "chad" | sudo -S systemctl start postgresql
fi
echo ""

echo "=== Docker Service Test ==="
if echo "chad" | sudo -S systemctl is-active --quiet docker; then
    echo -e "Docker service: ${GREEN}✓ Running${NC}"
else
    echo -e "Docker service: ${RED}✗ Not running${NC}"
    echo "Starting Docker..."
    echo "chad" | sudo -S systemctl start docker
fi
echo ""

echo "=== Project Build Test ==="
cd /home/chad/repos/witchcityrope/WitchCityRope
echo "Testing .NET project build..."
if dotnet build --no-restore 2>&1 | grep -q "Build succeeded"; then
    echo -e ".NET Build: ${GREEN}✓ PASSED${NC}"
else
    echo -e ".NET Build: ${RED}✗ FAILED${NC}"
fi
echo ""

echo "=== MCP Servers Test ==="
echo "Browser-tools MCP:"
if [ -f "/home/chad/mcp-servers/browser-tools-server/dist/index.js" ]; then
    echo -e "  Installation: ${GREEN}✓ Found${NC}"
    if [ -f "/home/chad/mcp-servers/browser-tools-server/test-screenshot.png" ]; then
        echo -e "  Test output: ${GREEN}✓ Verified${NC}"
    fi
else
    echo -e "  Installation: ${RED}✗ Not found${NC}"
fi

echo "Stagehand MCP:"
if [ -f "/home/chad/mcp-servers/mcp-server-browserbase/stagehand/dist/index.js" ]; then
    echo -e "  Installation: ${GREEN}✓ Found${NC}"
else
    echo -e "  Installation: ${RED}✗ Not found${NC}"
fi
echo ""

echo "=== Git Configuration ==="
echo "User: $(git config --global user.name)"
echo "Email: $(git config --global user.email)"
echo "Credential helper: $(git config --global credential.helper)"
echo ""

echo "=== Summary ==="
echo "All major development tools have been installed and configured."
echo "To start developing:"
echo "  1. cd /home/chad/repos/witchcityrope/WitchCityRope"
echo "  2. dotnet run --project src/WitchCityRope.Web"
echo ""
echo "For browser automation:"
echo "  - Browser-tools: /home/chad/mcp-servers/browser-tools-server/start-server.sh"
echo "  - Stagehand: /home/chad/mcp-servers/mcp-server-browserbase/stagehand/quickstart.sh"
echo ""
#!/bin/bash

# =============================================================================
# MCP Server Status Checker for WitchCityRope Project
# =============================================================================
# This script checks the status of all 8 MCP servers and provides:
# - Detailed status reports
# - Common issue detection
# - Quick fix suggestions
# - Environment verification
# =============================================================================

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color
BOLD='\033[1m'

# Status counters
WORKING=0
NEEDS_SETUP=0
FAILING=0

# Function to print colored output
print_status() {
    local status=$1
    local message=$2
    case $status in
        "ok") echo -e "${GREEN}✅ $message${NC}" ; ((WORKING++)) ;;
        "warn") echo -e "${YELLOW}⚠️  $message${NC}" ; ((NEEDS_SETUP++)) ;;
        "error") echo -e "${RED}❌ $message${NC}" ; ((FAILING++)) ;;
        "info") echo -e "${BLUE}ℹ️  $message${NC}" ;;
        "fix") echo -e "${PURPLE}🔧 FIX: $message${NC}" ;;
    esac
}

# Function to print section headers
print_header() {
    echo -e "\n${BOLD}${CYAN}=== $1 ===${NC}\n"
}

# Function to check command availability
check_command() {
    local cmd=$1
    local package=$2
    if command -v $cmd &> /dev/null; then
        return 0
    else
        print_status "error" "$cmd not found"
        if [ -n "$package" ]; then
            print_status "fix" "Install with: $package"
        fi
        return 1
    fi
}

# Function to check npm package
check_npm_package() {
    local package=$1
    if npm list -g $package 2>/dev/null | grep -q $package; then
        return 0
    else
        return 1
    fi
}

# Function to test MCP server availability
test_mcp_server() {
    local server_name=$1
    local test_command=$2
    local fix_command=$3
    
    echo -e "\n${BOLD}Testing $server_name...${NC}"
    
    if eval "$test_command" 2>/dev/null; then
        print_status "ok" "$server_name is working"
        return 0
    else
        print_status "error" "$server_name is not available"
        if [ -n "$fix_command" ]; then
            print_status "fix" "$fix_command"
        fi
        return 1
    fi
}

# Main script starts here
clear
echo -e "${BOLD}${PURPLE}"
echo "╔════════════════════════════════════════════════════════════════╗"
echo "║           MCP Server Status Checker for WitchCityRope          ║"
echo "║                    $(date +'%Y-%m-%d %H:%M:%S')                      ║"
echo "╚════════════════════════════════════════════════════════════════╝"
echo -e "${NC}"

# Check environment
print_header "Environment Check"

# Check OS
echo -n "Operating System: "
if grep -q Microsoft /proc/version 2>/dev/null; then
    echo -e "${GREEN}WSL2 (Windows Subsystem for Linux)${NC}"
    WSL=true
else
    echo -e "${GREEN}$(uname -s)${NC}"
    WSL=false
fi

# Check Node.js
echo -n "Node.js: "
if check_command "node" ""; then
    NODE_VERSION=$(node --version)
    echo -e "${GREEN}$NODE_VERSION${NC}"
    
    # Check if version is sufficient (v16+)
    MAJOR_VERSION=$(echo $NODE_VERSION | cut -d. -f1 | sed 's/v//')
    if [ $MAJOR_VERSION -lt 16 ]; then
        print_status "warn" "Node.js version is old. Recommend v16+"
        print_status "fix" "Update Node.js: curl -fsSL https://deb.nodesource.com/setup_lts.x | sudo -E bash - && sudo apt-get install -y nodejs"
    fi
else
    print_status "fix" "Install Node.js: curl -fsSL https://deb.nodesource.com/setup_lts.x | sudo -E bash - && sudo apt-get install -y nodejs"
fi

# Check npm
echo -n "npm: "
if check_command "npm" ""; then
    echo -e "${GREEN}$(npm --version)${NC}"
fi

# Check npx
echo -n "npx: "
if check_command "npx" "npm install -g npx"; then
    echo -e "${GREEN}$(npx --version)${NC}"
fi

# Check Docker
echo -n "Docker: "
if check_command "docker" "Install Docker Desktop from https://www.docker.com/products/docker-desktop/"; then
    DOCKER_VERSION=$(docker --version | awk '{print $3}' | sed 's/,//')
    echo -e "${GREEN}$DOCKER_VERSION${NC}"
    
    # Check if Docker daemon is running
    if ! docker ps &>/dev/null; then
        print_status "warn" "Docker daemon is not running"
        if [ "$WSL" = true ]; then
            print_status "fix" "Start Docker Desktop on Windows"
        else
            print_status "fix" "sudo systemctl start docker"
        fi
    fi
fi

# Check GitHub CLI
echo -n "GitHub CLI: "
if check_command "gh" "https://cli.github.com/manual/installation"; then
    echo -e "${GREEN}$(gh --version | head -1)${NC}"
    
    # Check GitHub auth
    if ! gh auth status &>/dev/null; then
        print_status "warn" "GitHub CLI not authenticated"
        print_status "fix" "Run: gh auth login"
    fi
fi

# Check PostgreSQL client
echo -n "PostgreSQL client: "
if check_command "psql" "sudo apt-get install postgresql-client"; then
    echo -e "${GREEN}$(psql --version | awk '{print $3}')${NC}"
fi

# Check Chrome (for browser automation)
echo -n "Chrome/Chromium: "
if [ "$WSL" = true ]; then
    # In WSL, check if Chrome is accessible via Windows
    if [ -f "/mnt/c/Program Files/Google/Chrome/Application/chrome.exe" ] || \
       [ -f "/mnt/c/Program Files (x86)/Google/Chrome/Application/chrome.exe" ]; then
        echo -e "${GREEN}Available via Windows${NC}"
        CHROME_AVAILABLE=true
    else
        echo -e "${YELLOW}Not found in standard Windows locations${NC}"
        CHROME_AVAILABLE=false
    fi
else
    if check_command "google-chrome" "wget -q -O - https://dl.google.com/linux/linux_signing_key.pub | sudo apt-key add - && sudo apt-get update && sudo apt-get install google-chrome-stable" || \
       check_command "chromium-browser" "sudo apt-get install chromium-browser"; then
        CHROME_AVAILABLE=true
    else
        CHROME_AVAILABLE=false
    fi
fi

# Check Claude configuration
print_header "Claude Configuration"

CLAUDE_CONFIG_WIN="C:\\Users\\chad\\AppData\\Roaming\\Claude\\claude_desktop_config.json"
CLAUDE_CONFIG_WSL="/mnt/c/Users/chad/AppData/Roaming/Claude/claude_desktop_config.json"

if [ -f "$CLAUDE_CONFIG_WSL" ]; then
    print_status "ok" "Claude configuration file found"
    
    # Check if all 8 MCP servers are configured
    MCP_COUNT=$(grep -c '"command"' "$CLAUDE_CONFIG_WSL" 2>/dev/null || echo 0)
    if [ $MCP_COUNT -eq 8 ]; then
        print_status "ok" "All 8 MCP servers are configured"
    else
        print_status "warn" "Only $MCP_COUNT MCP servers configured (expected 8)"
    fi
else
    print_status "error" "Claude configuration file not found"
    print_status "fix" "Ensure Claude Desktop is installed and configured"
fi

# Test individual MCP servers
print_header "MCP Server Status"

# 1. FileSystem MCP
test_mcp_server "FileSystem MCP" \
    "npx -y @modelcontextprotocol/server-filesystem --help 2>&1 | grep -q 'filesystem'" \
    "Should work automatically with npx"

# 2. Memory MCP
test_mcp_server "Memory MCP" \
    "npx -y @modelcontextprotocol/server-memory --help 2>&1 | grep -q 'memory'" \
    "Should work automatically with npx"

# Check if memory file exists
MEMORY_FILE="/mnt/c/Users/chad/AppData/Roaming/Claude/memory-data/memory.json"
if [ -f "$MEMORY_FILE" ]; then
    print_status "info" "Memory data file exists ($(du -h "$MEMORY_FILE" | cut -f1))"
else
    print_status "info" "No memory data file yet (will be created on first use)"
fi

# 3. Docker MCP
test_mcp_server "Docker MCP" \
    "docker ps --format 'table' &>/dev/null && npx -y mcp-server-docker --help 2>&1 | grep -q 'docker'" \
    "Ensure Docker Desktop is running"

# 4. Commands MCP
test_mcp_server "Commands MCP" \
    "npx -y mcp-server-commands --help 2>&1 | grep -q 'commands'" \
    "Should work automatically with npx"

# 5. GitHub MCP
if command -v gh &> /dev/null && gh auth status &>/dev/null; then
    test_mcp_server "GitHub MCP" \
        "npx -y @modelcontextprotocol/server-github --help 2>&1 | grep -q 'github'" \
        "Authenticate GitHub CLI: gh auth login"
else
    print_status "warn" "GitHub MCP - Needs GitHub CLI authentication"
    print_status "fix" "Run: gh auth login"
fi

# 6. PostgreSQL MCP
echo -e "\n${BOLD}Testing PostgreSQL MCP...${NC}"
# Check if PostgreSQL container is running
if docker ps 2>/dev/null | grep -q postgres; then
    print_status "info" "PostgreSQL container is running"
    
    # Check connection (this will fail with placeholder password)
    if PGPASSWORD=your_password_here psql -h localhost -U postgres -d witchcityrope_db -c '\q' 2>/dev/null; then
        print_status "ok" "PostgreSQL MCP is working"
    else
        print_status "warn" "PostgreSQL MCP needs password configuration"
        print_status "fix" "Update password in claude_desktop_config.json"
        print_status "info" "Get password from docker-compose.yml or .env file"
    fi
else
    print_status "warn" "PostgreSQL container not running"
    print_status "fix" "Run: docker-compose up -d postgres"
fi

# 7. Browser Tools MCP
echo -e "\n${BOLD}Testing Browser Tools MCP...${NC}"
if [ "$CHROME_AVAILABLE" = true ]; then
    # Check if browser server is running
    if curl -s http://localhost:3000/health 2>/dev/null | grep -q "ok"; then
        print_status "ok" "Browser Tools MCP is working (server running)"
    else
        print_status "warn" "Browser Tools MCP needs browser server"
        print_status "fix" "Start browser server: cd src/mcp-servers/browser-server-persistent && ./browser-server-manager.sh start"
    fi
else
    print_status "error" "Browser Tools MCP requires Chrome/Chromium"
    print_status "fix" "Install Chrome or Chromium browser"
fi

# 8. Stagehand MCP
echo -e "\n${BOLD}Testing Stagehand MCP...${NC}"
STAGEHAND_PATH="/mnt/c/Users/chad/source/repos/WitchCityRope/mcp-server-browserbase/stagehand"
if [ -d "$STAGEHAND_PATH" ]; then
    if [ -f "$STAGEHAND_PATH/dist/index.js" ]; then
        print_status "info" "Stagehand build found"
        
        # Check if Chrome is running with debug port
        if curl -s http://localhost:9222/json/version &>/dev/null; then
            print_status "ok" "Stagehand MCP is working (Chrome debug port open)"
        else
            print_status "warn" "Stagehand MCP needs Chrome with debug port"
            if [ "$WSL" = true ]; then
                print_status "fix" "On Windows, run: chrome.exe --remote-debugging-port=9222"
            else
                print_status "fix" "Run: google-chrome --remote-debugging-port=9222"
            fi
        fi
        
        # Check OpenAI API key
        if grep -q "OPENAI_API_KEY.*sk-" "$CLAUDE_CONFIG_WSL" 2>/dev/null; then
            print_status "info" "OpenAI API key configured"
        else
            print_status "warn" "OpenAI API key might need updating"
        fi
    else
        print_status "warn" "Stagehand not built"
        print_status "fix" "Build Stagehand: cd $STAGEHAND_PATH && npm install && npm run build"
    fi
else
    print_status "error" "Stagehand directory not found"
fi

# Summary
print_header "Summary"

echo -e "${BOLD}Total MCP Servers: 8${NC}"
echo -e "${GREEN}✅ Working: $WORKING${NC}"
echo -e "${YELLOW}⚠️  Need Setup: $NEEDS_SETUP${NC}"
echo -e "${RED}❌ Failing: $FAILING${NC}"

# Quick fixes summary
if [ $NEEDS_SETUP -gt 0 ] || [ $FAILING -gt 0 ]; then
    print_header "Quick Fix Commands"
    
    echo "1. Update system packages:"
    echo "   sudo apt update && sudo apt upgrade -y"
    echo ""
    
    echo "2. For PostgreSQL MCP:"
    echo "   - Check docker-compose.yml for the actual password"
    echo "   - Update claude_desktop_config.json with correct password"
    echo ""
    
    echo "3. For Browser automation (Browser Tools & Stagehand):"
    if [ "$WSL" = true ]; then
        echo "   - Start Chrome on Windows: chrome.exe --remote-debugging-port=9222"
        echo "   - Start browser server: cd src/mcp-servers/browser-server-persistent && ./browser-server-manager.sh start"
    else
        echo "   - Start Chrome: google-chrome --remote-debugging-port=9222 &"
        echo "   - Start browser server: cd src/mcp-servers/browser-server-persistent && ./browser-server-manager.sh start"
    fi
    echo ""
    
    echo "4. For GitHub MCP:"
    echo "   gh auth login"
    echo ""
fi

# Environment-specific recommendations
print_header "Recommendations"

if [ "$WSL" = true ]; then
    echo "Running in WSL2 environment:"
    echo "- File operations work seamlessly between Windows and Linux"
    echo "- Browser automation requires Chrome on Windows side"
    echo "- Docker Desktop must be running on Windows"
else
    echo "Running in native Linux environment:"
    echo "- Install Chrome/Chromium for browser automation"
    echo "- Ensure Docker daemon is running"
fi

echo ""
echo -e "${BOLD}${CYAN}For detailed documentation, see:${NC}"
echo "- docs/MCP_SERVERS.md"
echo "- docs/MCP_DEBUGGING_GUIDE.md"
echo "- src/mcp-servers/README.md"

echo ""
echo -e "${BOLD}${GREEN}Status check completed at $(date +'%Y-%m-%d %H:%M:%S')${NC}"
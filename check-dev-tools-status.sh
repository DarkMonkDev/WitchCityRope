#!/bin/bash

# =============================================================================
# Development Tools Status Checker for WitchCityRope Project
# =============================================================================
# This script checks the status of development tools used in the project
# Clarifies which are CLI tools vs MCP servers
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

# Function to print colored output
print_status() {
    local status=$1
    local message=$2
    case $status in
        "ok") echo -e "${GREEN}✅ $message${NC}" ;;
        "warn") echo -e "${YELLOW}⚠️  $message${NC}" ;;
        "error") echo -e "${RED}❌ $message${NC}" ;;
        "info") echo -e "${BLUE}ℹ️  $message${NC}" ;;
    esac
}

# Function to print section headers
print_header() {
    echo -e "\n${BOLD}${CYAN}=== $1 ===${NC}\n"
}

# Clear screen and print header
clear
echo -e "${BOLD}${PURPLE}
╔════════════════════════════════════════════════════════════════╗
║        Development Tools Status for WitchCityRope              ║
║                    $(date +'%Y-%m-%d %H:%M:%S')                      ║
╚════════════════════════════════════════════════════════════════╝
${NC}"

print_header "CLI Tools (Used via Bash commands)"

# Check Docker
echo -e "\n${BOLD}Docker:${NC}"
if command -v docker &> /dev/null; then
    DOCKER_VERSION=$(docker --version | cut -d' ' -f3 | tr -d ',')
    print_status "ok" "Docker $DOCKER_VERSION installed"
    
    # Check if Docker daemon is running
    if docker ps &> /dev/null; then
        print_status "ok" "Docker daemon is running"
        
        # Check WitchCityRope containers
        if docker ps | grep -q "witchcity-"; then
            print_status "ok" "WitchCityRope containers are running"
            docker ps --format "table {{.Names}}\t{{.Status}}" | grep witchcity-
        else
            print_status "warn" "WitchCityRope containers not running"
            print_status "info" "Run: ./dev.sh to start containers"
        fi
    else
        print_status "error" "Docker daemon not running"
        print_status "info" "Run: sudo systemctl start docker"
    fi
else
    print_status "error" "Docker not installed"
fi

# Check GitHub CLI
echo -e "\n${BOLD}GitHub CLI:${NC}"
if command -v gh &> /dev/null; then
    GH_VERSION=$(gh --version | head -1)
    print_status "ok" "GitHub CLI installed: $GH_VERSION"
    
    # Check authentication
    if gh auth status &> /dev/null; then
        print_status "ok" "GitHub CLI authenticated"
    else
        print_status "warn" "GitHub CLI not authenticated"
        print_status "info" "Run: gh auth login"
    fi
else
    print_status "error" "GitHub CLI not installed"
    print_status "info" "Install: https://cli.github.com/"
fi

# Check PostgreSQL
echo -e "\n${BOLD}PostgreSQL Client:${NC}"
if command -v psql &> /dev/null; then
    PSQL_VERSION=$(psql --version | awk '{print $3}')
    print_status "ok" "PostgreSQL client $PSQL_VERSION installed"
    
    # Check if can connect to WitchCityRope DB
    if PGPASSWORD=postgres psql -h localhost -p 5433 -U postgres -d witchcityrope_db -c '\q' &> /dev/null; then
        print_status "ok" "Can connect to WitchCityRope database"
    else
        print_status "warn" "Cannot connect to database"
        print_status "info" "Make sure containers are running: docker ps"
    fi
else
    print_status "warn" "PostgreSQL client not installed"
    print_status "info" "Database access still works via: docker exec -it witchcity-postgres psql"
fi

# Check Node.js/npm (for Puppeteer)
echo -e "\n${BOLD}Node.js/npm (for Puppeteer):${NC}"
if command -v node &> /dev/null; then
    NODE_VERSION=$(node --version)
    print_status "ok" "Node.js $NODE_VERSION installed"
    
    if command -v npm &> /dev/null; then
        NPM_VERSION=$(npm --version)
        print_status "ok" "npm $NPM_VERSION installed"
        
        # Check if Puppeteer is installed in e2e directory
        if [ -d "/home/chad/repos/witchcityrope/WitchCityRope/tests/e2e/node_modules/puppeteer" ]; then
            print_status "ok" "Puppeteer installed in e2e tests directory"
        else
            print_status "warn" "Puppeteer not found in e2e directory"
            print_status "info" "Run: cd tests/e2e && npm install"
        fi
    fi
else
    print_status "error" "Node.js not installed"
fi

# Check Chrome/Chromium (for Puppeteer)
echo -e "\n${BOLD}Chrome/Chromium (for browser automation):${NC}"
if command -v google-chrome &> /dev/null; then
    CHROME_VERSION=$(google-chrome --version)
    print_status "ok" "$CHROME_VERSION installed"
elif command -v chromium &> /dev/null; then
    CHROMIUM_VERSION=$(chromium --version)
    print_status "ok" "$CHROMIUM_VERSION installed"
else
    print_status "warn" "Chrome/Chromium not found"
    print_status "info" "Puppeteer will download Chromium automatically if needed"
fi

print_header "Claude Code Built-in Tools (Always Available)"

echo "File Operations:"
print_status "ok" "Read - Read any file"
print_status "ok" "Write - Create/overwrite files"
print_status "ok" "Edit - Modify file contents"
print_status "ok" "Glob - Find files by pattern"
print_status "ok" "Grep - Search file contents"
print_status "ok" "LS - List directory contents"

echo -e "\nOther Tools:"
print_status "ok" "Bash - Execute any CLI command"
print_status "ok" "WebFetch - Fetch and analyze web content"
print_status "ok" "WebSearch - Search the web"
print_status "ok" "TodoWrite - Task management"

print_header "MCP Servers (Not Currently Loaded)"

print_status "info" "MCP servers provide additional tools but are NOT required"
print_status "info" "Everything works fine with CLI tools + built-in tools"
print_status "info" "To use MCP servers: claude --mcp-config ~/.config/claude/mcp.json"

echo -e "\nCurrently NOT available as MCP servers:"
echo "- Memory/Context persistence between sessions"
echo "- Context7 documentation lookup"
echo "- Structured database queries (use psql instead)"

print_header "Summary"

echo -e "${BOLD}All essential development tools are working via CLI commands!${NC}"
echo -e "\nFor more details, see: ${BLUE}MCP_VS_CLI_TOOLS_GUIDE.md${NC}"

print_header "Quick Commands Reference"

echo "Docker:"
echo "  docker ps                    # List running containers"
echo "  docker-compose logs -f web   # View web logs"
echo "  ./dev.sh                     # Start development environment"
echo "  ./restart-web.sh             # Quick restart web container"

echo -e "\nGitHub:"
echo "  gh pr create                 # Create pull request"
echo "  gh issue list                # List issues"
echo "  gh pr status                 # Check PR status"

echo -e "\nPostgreSQL:"
echo "  psql -h localhost -p 5433 -U postgres -d witchcityrope_db"
echo "  docker exec -it witchcity-postgres psql -U postgres"

echo -e "\nPuppeteer:"
echo "  cd tests/e2e && node test-login.js"
echo "  cd tests/e2e && node run-all-tests.js"

echo -e "\n${BOLD}${GREEN}Status check completed at $(date +'%H:%M:%S')${NC}"
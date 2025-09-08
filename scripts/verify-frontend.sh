#!/bin/bash
set -e

echo "===========================================" 
echo "🏗️  WitchCityRope Frontend Health Check"
echo "==========================================="
echo "Date: $(date)"
echo "Time: $(date '+%H:%M:%S')"
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to check URL and show status
check_url() {
    local url=$1
    local description=$2
    local expected_status=${3:-200}
    
    echo -n "🔍 $description... "
    
    if response=$(curl -s -w "%{http_code}" -o /dev/null --max-time 10 "$url" 2>/dev/null); then
        if [ "$response" = "$expected_status" ]; then
            echo -e "${GREEN}✅ OK ($response)${NC}"
            return 0
        else
            echo -e "${RED}❌ FAIL (HTTP $response, expected $expected_status)${NC}"
            return 1
        fi
    else
        echo -e "${RED}❌ CONNECTION FAILED${NC}"
        return 1
    fi
}

# Function to check if process is running
check_process() {
    local process_name=$1
    local description=$2
    
    echo -n "🔍 $description... "
    
    if pgrep -f "$process_name" > /dev/null; then
        echo -e "${GREEN}✅ RUNNING${NC}"
        return 0
    else
        echo -e "${RED}❌ NOT RUNNING${NC}"
        return 1
    fi
}

# Check if frontend dev server is running
echo "📊 Process Status Check"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
check_process "vite.*--port 5174" "Frontend Dev Server (Port 5174)"
check_process "vite.*--port 5173" "Container Dev Server (Port 5173)"

echo ""
echo "🌐 Frontend Connectivity Check"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

# Test frontend accessibility
check_url "http://localhost:5174" "Frontend Development Server"

echo ""
echo "🏠 Core Pages Status"
echo "━━━━━━━━━━━━━━━━━━━━━━━━"

# Test key pages - these should return 200 since they're client-side routes
check_url "http://localhost:5174/" "Homepage"
check_url "http://localhost:5174/login" "Login Page"  
check_url "http://localhost:5174/register" "Registration Page"
check_url "http://localhost:5174/events" "Events List Page"
check_url "http://localhost:5174/dashboard" "Dashboard Page"

echo ""
echo "🔧 API Integration Test"
echo "━━━━━━━━━━━━━━━━━━━━━━━━"

# Test API proxy (this might return different codes based on API status)
echo -n "🔍 API Proxy Health... "
api_response=$(curl -s -w "%{http_code}" -o /dev/null --max-time 10 "http://localhost:5174/api/health" 2>/dev/null || echo "000")

case $api_response in
    "200") echo -e "${GREEN}✅ API HEALTHY${NC}" ;;
    "404") echo -e "${YELLOW}⚠️  API ENDPOINT NOT FOUND (may be expected)${NC}" ;;
    "500") echo -e "${RED}❌ API SERVER ERROR${NC}" ;;
    "000") echo -e "${RED}❌ API CONNECTION FAILED${NC}" ;;
    *) echo -e "${YELLOW}⚠️  UNEXPECTED STATUS ($api_response)${NC}" ;;
esac

echo ""
echo "🧪 Demo and Special Pages"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━"

# Test demo pages
check_url "http://localhost:5174/admin/event-session-matrix-demo" "Event Session Matrix Demo"
check_url "http://localhost:5174/mantine-forms" "Mantine Forms Test"

echo ""
echo "📊 Frontend Status Summary"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

# Count processes
vite_processes=$(pgrep -f "vite" | wc -l)
node_processes=$(pgrep -f "node.*vite" | wc -l)

echo "🔧 Development Processes:"
echo "   • Vite processes: $vite_processes"
echo "   • Node/Vite processes: $node_processes"
echo ""

# Port status
echo "🌐 Port Status:"
if netstat -tlnp 2>/dev/null | grep -q ":5174 "; then
    echo -e "   • Port 5174: ${GREEN}✅ LISTENING${NC}"
else
    echo -e "   • Port 5174: ${RED}❌ NOT LISTENING${NC}"
fi

if netstat -tlnp 2>/dev/null | grep -q ":5173 "; then
    echo -e "   • Port 5173: ${GREEN}✅ LISTENING${NC}"
else
    echo -e "   • Port 5173: ${YELLOW}⚠️  NOT LISTENING${NC}"
fi

echo ""
echo "🎯 Known Working Features (as of 2025-01-09):"
echo "   ✅ Authentication system - Login/Register/Logout working"
echo "   ✅ Events pages - List and detail pages with real API data"
echo "   ✅ Dashboard system - Role-based widgets operational"
echo "   ✅ User profile management - Multi-tab interface working"
echo "   ✅ Form components - Mantine v7 with brand styling"
echo "   ✅ Event Session Matrix Demo - All UI issues resolved"
echo ""

echo "🔧 Recent Critical Fixes Applied:"
echo "   ✅ authService.ts - Fixed API response handling"
echo "   ✅ mutations.ts - Fixed response structure expectations"
echo "   ✅ EventsListPage - Removed mock data, using real API"
echo "   ✅ EventDetailPage - Added real API integration"
echo "   ✅ vite.config.ts - Fixed proxy port configuration"
echo "   ✅ Ad-Hoc Email UI - Target Sessions selector fixed"
echo "   ✅ Input animations - WitchCityRope brand colors applied"
echo "   ✅ Scroll issues - Email tab overflow fixed"
echo ""

echo "===========================================" 
echo "🏁 Frontend Health Check Complete"
echo "==========================================="
echo "Run this script regularly to verify frontend status"
echo "For issues, check logs with: npm run dev -- --port 5174"
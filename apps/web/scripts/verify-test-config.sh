#!/bin/bash

# Test configuration verification script
# Checks all memory management fixes are in place

set -e

echo "🔍 Verifying test runner memory management configuration..."
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to check config value
check_config() {
    local file="$1"
    local pattern="$2"
    local description="$3"
    
    if [ -f "$file" ]; then
        if grep -q "$pattern" "$file"; then
            echo -e "✅ ${GREEN}$description${NC}"
            return 0
        else
            echo -e "❌ ${RED}$description${NC}"
            return 1
        fi
    else
        echo -e "❌ ${RED}File not found: $file${NC}"
        return 1
    fi
}

# Function to check script exists and is executable
check_script() {
    local script="$1"
    local description="$2"
    
    if [ -f "$script" ] && [ -x "$script" ]; then
        echo -e "✅ ${GREEN}$description${NC}"
        return 0
    else
        echo -e "❌ ${RED}$description${NC}"
        return 1
    fi
}

echo "📋 Checking Playwright Configuration:"
echo "─────────────────────────────────────"
check_config "./playwright.config.ts" "workers.*process\.env\.CI.*2" "Worker limit set to 2 max"
check_config "./playwright.config.ts" "maxFailures: 2" "Max failures limit set"
check_config "./playwright.config.ts" "globalTeardown:" "Global teardown configured"
check_config "./playwright.config.ts" "max-old-space-size=1024" "Node.js memory limit in browser args"
check_config "./playwright.config.ts" "disable-dev-shm-usage" "Memory management browser args"

echo ""
echo "📋 Checking Vitest Configuration:"
echo "──────────────────────────────────"
check_config "./vitest.config.ts" "singleThread: true" "Single thread mode enabled"
check_config "./vitest.config.ts" "maxConcurrency: 1" "Max concurrency limited"
check_config "./vitest.config.ts" "teardownTimeout: 10000" "Teardown timeout set"

echo ""
echo "📋 Checking Package.json Scripts:"
echo "──────────────────────────────────"
check_config "./package.json" "NODE_OPTIONS='--max-old-space-size=2048' vitest" "Vitest memory limit"
check_config "./package.json" "NODE_OPTIONS='--max-old-space-size=2048' playwright test" "Playwright memory limit"
check_config "./package.json" "test:safe" "Safe test script available"
check_config "./package.json" "test:e2e:safe" "Safe E2E script available"
check_config "./package.json" "test:cleanup" "Cleanup script available"

echo ""
echo "📋 Checking Safety Scripts:"
echo "────────────────────────────"
check_script "./scripts/monitor-test-memory.sh" "Memory monitoring script exists and executable"
check_script "./scripts/cleanup-test-processes.sh" "Cleanup script exists and executable"
check_config "./tests/playwright/global-teardown.ts" "export default globalTeardown" "Global teardown script exists"

echo ""
echo "📊 Current System Status:"
echo "─────────────────────────"
echo "Memory usage:"
free -h | grep '^Mem:'

echo ""
echo "Running processes:"
node_count=$(pgrep -f node | wc -l)
chrome_count=$(pgrep -f chrome | wc -l)
echo "   Node.js processes: $node_count"
echo "   Chrome processes: $chrome_count"

echo ""
if [ $node_count -gt 20 ] || [ $chrome_count -gt 10 ]; then
    echo -e "⚠️  ${YELLOW}Warning: High process count detected. Consider running cleanup.${NC}"
    echo "   Run: npm run test:cleanup"
else
    echo -e "✅ ${GREEN}Process count looks normal${NC}"
fi

echo ""
echo "🎉 Configuration verification complete!"
echo ""
echo -e "${GREEN}Safe commands to use:${NC}"
echo "   npm run test:safe          # Vitest with monitoring"
echo "   npm run test:e2e:safe      # Playwright with monitoring"
echo "   npm run test:cleanup       # Emergency cleanup"
echo ""
echo -e "${RED}Avoid these in development:${NC}"
echo "   npm run test              # Can crash system"
echo "   npm run test:e2e          # Can crash system"
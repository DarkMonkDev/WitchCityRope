#!/bin/bash

# Emergency cleanup script for runaway test processes
# Usage: ./scripts/cleanup-test-processes.sh

set -e

echo "🧹 Emergency cleanup of test processes..."

# Function to safely kill processes
safe_kill() {
    local process_name="$1"
    local pids=$(pgrep -f "$process_name" 2>/dev/null || true)
    
    if [ -n "$pids" ]; then
        echo "🔍 Found $process_name processes: $pids"
        
        # First try graceful termination
        echo "⏹️  Attempting graceful termination..."
        kill $pids 2>/dev/null || true
        sleep 2
        
        # Check if processes are still running
        local remaining=$(pgrep -f "$process_name" 2>/dev/null || true)
        if [ -n "$remaining" ]; then
            echo "💀 Force killing stubborn processes: $remaining"
            kill -9 $remaining 2>/dev/null || true
        fi
        
        echo "✅ Cleaned up $process_name processes"
    else
        echo "✅ No $process_name processes found"
    fi
}

# Function to kill only test-related Chrome processes
kill_test_chrome() {
    echo "🔍 Looking for test-related Chrome processes..."
    
    # Find Chrome processes with test-specific flags (headless, remote-debugging-port, etc.)
    local test_chrome_pids=$(ps aux | grep -E "chrome.*(--headless|--remote-debugging-port|--disable-dev-shm-usage|playwright|test)" | grep -v grep | awk '{print $2}' | tr '\n' ' ')
    
    if [ -n "$test_chrome_pids" ]; then
        echo "🎯 Found test-related Chrome processes: $test_chrome_pids"
        echo "⏹️  Terminating test Chrome instances only..."
        kill $test_chrome_pids 2>/dev/null || true
        sleep 2
        
        # Check if any are still running
        local remaining=$(ps aux | grep -E "chrome.*(--headless|--remote-debugging-port|--disable-dev-shm-usage|playwright|test)" | grep -v grep | awk '{print $2}' | tr '\n' ' ')
        if [ -n "$remaining" ]; then
            echo "💀 Force killing stubborn test Chrome processes: $remaining"
            kill -9 $remaining 2>/dev/null || true
        fi
        echo "✅ Cleaned up test-related Chrome processes"
    else
        echo "✅ No test-related Chrome processes found"
    fi
    
    # Count regular Chrome processes (not killed)
    local regular_chrome=$(pgrep -f "chrome" | wc -l)
    if [ $regular_chrome -gt 0 ]; then
        echo "ℹ️  Regular Chrome instances preserved: $regular_chrome processes"
    fi
}

# Clean up different types of test processes
echo "🔍 Checking for runaway test processes..."

safe_kill "vitest"
safe_kill "playwright"
safe_kill "chromium"  # Chromium is typically only used for testing
kill_test_chrome      # Only kill test-related Chrome, not regular browser
safe_kill "node.*test"

# Clean up temporary files
echo "🗑️  Cleaning up temporary test files..."

# Remove playwright artifacts
if [ -d "./test-results" ]; then
    find ./test-results -name ".playwright-artifacts-*" -type d -exec rm -rf {} + 2>/dev/null || true
    echo "✅ Cleaned up Playwright artifacts"
fi

# Remove any lock files that might prevent tests from running
rm -f ./test-results/test-monitor.pid 2>/dev/null || true
rm -f ./.playwright-server.lock 2>/dev/null || true

# Check memory usage after cleanup
echo ""
echo "📊 System status after cleanup:"
echo "═══════════════════════════════"
free -h | grep '^Mem:'

# Count remaining processes
node_count=$(pgrep -f node | wc -l)
chrome_count=$(pgrep -f chrome | wc -l)

echo "📊 Remaining processes:"
echo "   Node.js processes: $node_count"
echo "   Chrome processes: $chrome_count"

if [ $node_count -gt 10 ] || [ $chrome_count -gt 0 ]; then
    echo "⚠️  Warning: Still many processes running. You may need to restart your system."
    echo "💡 Consider running 'sudo systemctl restart gdm' to reset the desktop session"
else
    echo "✅ System looks clean - safe to run tests again"
fi

echo ""
echo "🎉 Cleanup completed!"
echo "💡 You can now safely run tests again with:"
echo "   npm run test:safe"
echo "   npm run test:e2e:safe"
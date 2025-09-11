#!/bin/bash

# Memory monitoring script for test execution
# Usage: ./scripts/monitor-test-memory.sh [test-type]
# Test types: vitest, playwright, all

set -e

TEST_TYPE=${1:-"all"}
LOG_FILE="./test-results/memory-usage-$(date +%Y%m%d-%H%M%S).log"
PID_FILE="./test-results/test-monitor.pid"

# Create test-results directory if it doesn't exist
mkdir -p ./test-results

echo "🔍 Starting memory monitoring for $TEST_TYPE tests..."
echo "📊 Memory usage will be logged to: $LOG_FILE"

# Function to log memory usage
log_memory() {
    local timestamp=$(date '+%Y-%m-%d %H:%M:%S')
    local memory_info=$(free -h | grep '^Mem:')
    local node_processes=$(pgrep -f node | wc -l)
    local chrome_processes=$(pgrep -f chrome | wc -l)
    
    echo "[$timestamp] Memory: $memory_info | Node processes: $node_processes | Chrome processes: $chrome_processes" >> "$LOG_FILE"
}

# Function to monitor continuously
monitor_continuously() {
    echo $$ > "$PID_FILE"
    
    while true; do
        log_memory
        
        # Check for critical memory usage (>90%)
        memory_usage=$(free | grep '^Mem:' | awk '{print $3/$2 * 100.0}')
        if (( $(echo "$memory_usage > 90" | bc -l) )); then
            echo "🚨 CRITICAL: Memory usage is at ${memory_usage}% - killing test processes!" | tee -a "$LOG_FILE"
            pkill -f "vitest" || true
            pkill -f "playwright" || true
            pkill -f "chromium" || true
            # Only kill Chrome processes with test-specific flags
            ps aux | grep -E "chrome.*(--headless|--remote-debugging-port|--disable-dev-shm-usage|playwright|test)" | grep -v grep | awk '{print $2}' | xargs -r kill || true
            echo "✅ Killed runaway test processes (preserved regular Chrome)" | tee -a "$LOG_FILE"
        fi
        
        sleep 5
    done
}

# Function to stop monitoring
stop_monitoring() {
    if [ -f "$PID_FILE" ]; then
        local monitor_pid=$(cat "$PID_FILE")
        kill "$monitor_pid" 2>/dev/null || true
        rm -f "$PID_FILE"
        echo "⏹️  Stopped memory monitoring"
    fi
}

# Function to run tests with monitoring
run_with_monitoring() {
    local test_cmd="$1"
    
    # Start monitoring in background
    monitor_continuously &
    local monitor_pid=$!
    
    # Set up cleanup on script exit
    trap "kill $monitor_pid 2>/dev/null || true; stop_monitoring; exit" INT TERM EXIT
    
    echo "🚀 Running: $test_cmd"
    echo "📊 Memory monitoring active (PID: $monitor_pid)"
    
    # Run the test command
    if eval "$test_cmd"; then
        echo "✅ Tests completed successfully"
        exit_code=0
    else
        echo "❌ Tests failed"
        exit_code=1
    fi
    
    # Cleanup
    kill "$monitor_pid" 2>/dev/null || true
    stop_monitoring
    
    # Show memory summary
    echo ""
    echo "📊 Memory Usage Summary:"
    echo "════════════════════════"
    tail -10 "$LOG_FILE"
    
    exit $exit_code
}

# Handle different test types
case "$TEST_TYPE" in
    "vitest")
        run_with_monitoring "npm run test"
        ;;
    "playwright")
        run_with_monitoring "npm run test:e2e"
        ;;
    "all")
        echo "🧪 Running all tests with memory monitoring..."
        run_with_monitoring "npm run test && npm run test:e2e"
        ;;
    "stop")
        stop_monitoring
        ;;
    *)
        echo "❌ Unknown test type: $TEST_TYPE"
        echo "Usage: $0 [vitest|playwright|all|stop]"
        exit 1
        ;;
esac
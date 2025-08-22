#!/bin/bash
# launch-chrome-universal.sh - Universal Chrome launcher with incognito mode
# This is the standardized script that should be used everywhere
# It uses the PowerShell bridge method that has been proven to work

# Configuration
CHROME_PORT=${CHROME_PORT:-9222}
CHROME_PATH="C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe"
USER_DATA_DIR="/tmp/chrome-automation-$(date +%s)"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    local color=$1
    local message=$2
    echo -e "${color}${message}${NC}"
}

# Function to check if Chrome is already running with debug port
check_chrome_running() {
    if curl -s http://localhost:$CHROME_PORT/json/version > /dev/null 2>&1; then
        return 0
    else
        return 1
    fi
}

# Function to kill existing Chrome debug instances
kill_chrome_debug() {
    print_status "$YELLOW" "🔍 Checking for existing Chrome debug instances..."
    
    # Kill from Windows side using PowerShell
    powershell.exe -Command "
        \$chromeProcesses = Get-Process chrome -ErrorAction SilentlyContinue | 
            Where-Object { \$_.CommandLine -like '*remote-debugging-port=$CHROME_PORT*' }
        
        if (\$chromeProcesses) {
            Write-Host 'Found Chrome processes with debug port $CHROME_PORT'
            \$chromeProcesses | Stop-Process -Force
            Write-Host 'Chrome processes terminated'
        } else {
            Write-Host 'No Chrome debug processes found'
        }
    " 2>/dev/null
    
    # Also try to kill any Chrome processes from WSL perspective
    pkill -f "chrome.*remote-debugging-port=$CHROME_PORT" 2>/dev/null || true
    
    # Wait for processes to fully terminate
    sleep 2
    
    # Verify they're gone
    if check_chrome_running; then
        print_status "$RED" "⚠️  Warning: Chrome debug port still responding. Trying harder..."
        # Try to force kill by port
        powershell.exe -Command "
            \$connection = Get-NetTCPConnection -LocalPort $CHROME_PORT -ErrorAction SilentlyContinue
            if (\$connection) {
                \$processId = \$connection.OwningProcess
                Stop-Process -Id \$processId -Force -ErrorAction SilentlyContinue
            }
        " 2>/dev/null
        sleep 2
    fi
}

# Function to launch Chrome using PowerShell bridge (PROVEN METHOD)
launch_chrome() {
    local extra_args="$@"
    
    print_status "$GREEN" "🚀 Launching Chrome with remote debugging on port $CHROME_PORT..."
    print_status "$BLUE" "🔒 INCOGNITO MODE ENABLED for privacy"
    
    # Build the PowerShell command using the EXACT method that worked
    local ps_command="& '${CHROME_PATH}' --remote-debugging-port=$CHROME_PORT --incognito"
    
    # Add standard flags
    ps_command+=" --no-first-run"
    ps_command+=" --no-default-browser-check"
    ps_command+=" --disable-popup-blocking"
    ps_command+=" --disable-translate"
    ps_command+=" --disable-background-timer-throttling"
    ps_command+=" --disable-renderer-backgrounding"
    ps_command+=" --disable-device-discovery-notifications"
    
    # Add custom user data dir to avoid conflicts
    ps_command+=" --user-data-dir='$USER_DATA_DIR'"
    
    # Add any extra arguments
    if [ -n "$extra_args" ]; then
        ps_command+=" $extra_args"
    fi
    
    # Launch Chrome using PowerShell (the method that works!)
    print_status "$YELLOW" "📋 Command: powershell.exe -Command \"$ps_command\""
    powershell.exe -Command "$ps_command" &
    
    # Wait for Chrome to start and be ready
    print_status "$YELLOW" "⏳ Waiting for Chrome to start..."
    local count=0
    while ! check_chrome_running && [ $count -lt 30 ]; do
        sleep 1
        ((count++))
        # Show progress every 5 seconds
        if [ $((count % 5)) -eq 0 ]; then
            print_status "$YELLOW" "   Still waiting... ($count seconds)"
        fi
    done
    
    if check_chrome_running; then
        print_status "$GREEN" "✅ Chrome launched successfully!"
        print_status "$GREEN" "🌐 Remote debugging available at: http://localhost:$CHROME_PORT"
        
        # Show Chrome version info
        local version_info=$(curl -s http://localhost:$CHROME_PORT/json/version 2>/dev/null)
        local browser_version=$(echo "$version_info" | grep -o '"Browser":"[^"]*"' | cut -d'"' -f4)
        local protocol_version=$(echo "$version_info" | grep -o '"Protocol-Version":"[^"]*"' | cut -d'"' -f4)
        
        if [ -n "$browser_version" ]; then
            print_status "$BLUE" "📌 Chrome version: $browser_version"
            print_status "$BLUE" "📌 Protocol version: $protocol_version"
        fi
        
        # List initial tabs
        local tabs=$(curl -s http://localhost:$CHROME_PORT/json 2>/dev/null)
        local tab_count=$(echo "$tabs" | grep -c '"type":"page"' || echo "0")
        print_status "$BLUE" "📑 Open tabs: $tab_count"
        
        print_status "$GREEN" "🎉 Ready for automation!"
        return 0
    else
        print_status "$RED" "❌ Failed to launch Chrome or connect to debug port"
        print_status "$YELLOW" "💡 Troubleshooting tips:"
        print_status "$YELLOW" "   1. Check if Chrome is installed at: $CHROME_PATH"
        print_status "$YELLOW" "   2. Try running with a different port: CHROME_PORT=9223 $0"
        print_status "$YELLOW" "   3. Check Windows Firewall settings"
        return 1
    fi
}

# Function to show Chrome status
show_status() {
    print_status "$BLUE" "🔍 Checking Chrome status..."
    
    if check_chrome_running; then
        print_status "$GREEN" "✅ Chrome is running with debugging on port $CHROME_PORT"
        
        # Get version info
        local version_info=$(curl -s http://localhost:$CHROME_PORT/json/version 2>/dev/null)
        if [ -n "$version_info" ]; then
            local browser_version=$(echo "$version_info" | grep -o '"Browser":"[^"]*"' | cut -d'"' -f4)
            print_status "$BLUE" "📌 Version: $browser_version"
        fi
        
        # Get list of open tabs
        local tabs=$(curl -s http://localhost:$CHROME_PORT/json 2>/dev/null)
        if [ -n "$tabs" ]; then
            local tab_count=$(echo "$tabs" | grep -c '"type":"page"' || echo "0")
            print_status "$BLUE" "📑 Open tabs: $tab_count"
            
            # Show tab details
            echo "$tabs" | jq -r '.[] | select(.type=="page") | "   - \(.title) (\(.url))"' 2>/dev/null || true
        fi
    else
        print_status "$YELLOW" "⚠️  Chrome is not running with debugging on port $CHROME_PORT"
        print_status "$YELLOW" "💡 Run '$0 launch' to start Chrome"
    fi
}

# Function to test the connection
test_connection() {
    print_status "$BLUE" "🧪 Testing Chrome DevTools connection..."
    
    if check_chrome_running; then
        print_status "$GREEN" "✅ Connection successful!"
        
        # Show detailed connection info
        local version_info=$(curl -s http://localhost:$CHROME_PORT/json/version 2>/dev/null)
        echo ""
        echo "Connection Details:"
        echo "$version_info" | jq . 2>/dev/null || echo "$version_info"
        
        print_status "$GREEN" "✅ Both Browser Tools MCP and Stagehand MCP should work!"
    else
        print_status "$RED" "❌ Cannot connect to Chrome DevTools"
        print_status "$YELLOW" "💡 Run '$0 launch' to start Chrome first"
    fi
}

# Main script logic
case "${1:-help}" in
    launch|start)
        if check_chrome_running; then
            print_status "$YELLOW" "⚠️  Chrome is already running with debugging on port $CHROME_PORT"
            show_status
            print_status "$YELLOW" "💡 Use '$0 restart' to restart Chrome"
        else
            kill_chrome_debug
            launch_chrome "${@:2}"
        fi
        ;;
    kill|stop)
        kill_chrome_debug
        print_status "$GREEN" "✅ Chrome debug instances terminated"
        ;;
    status)
        show_status
        ;;
    restart)
        print_status "$BLUE" "🔄 Restarting Chrome..."
        kill_chrome_debug
        launch_chrome "${@:2}"
        ;;
    test)
        test_connection
        ;;
    *)
        echo "🚀 Chrome Universal Launcher - Always in Incognito Mode"
        echo "====================================================="
        echo ""
        echo "Usage: $0 [command] [options]"
        echo ""
        echo "Commands:"
        echo "  launch, start  - Launch Chrome with debugging in incognito mode"
        echo "  kill, stop     - Kill existing Chrome debug instances"
        echo "  status         - Check Chrome debug status"
        echo "  restart        - Kill and relaunch Chrome"
        echo "  test           - Test the DevTools connection"
        echo ""
        echo "Environment variables:"
        echo "  CHROME_PORT    - Debug port (default: 9222)"
        echo ""
        echo "Features:"
        echo "  ✅ Always launches in INCOGNITO mode for privacy"
        echo "  ✅ Uses proven PowerShell bridge method"
        echo "  ✅ Automatically kills existing debug instances"
        echo "  ✅ Works with both Browser Tools MCP and Stagehand MCP"
        echo ""
        echo "Example:"
        echo "  $0 launch                    # Start Chrome"
        echo "  CHROME_PORT=9223 $0 launch   # Use different port"
        exit 1
        ;;
esac

exit $?
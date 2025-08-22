#!/bin/bash

# Browser Server Manager Script
# Manages the persistent browser server for Browser Tools MCP

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SERVER_SCRIPT="$SCRIPT_DIR/browser-server.js"
PID_FILE="$SCRIPT_DIR/browser-server.pid"
LOG_FILE="$SCRIPT_DIR/browser-server.log"
ERROR_FILE="$SCRIPT_DIR/browser-server.error.log"
SERVICE_FILE="$SCRIPT_DIR/browser-server.service"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    local color=$1
    local message=$2
    echo -e "${color}${message}${NC}"
}

# Check if running with systemd
check_systemd() {
    if command -v systemctl &> /dev/null && systemctl is-system-running &> /dev/null; then
        return 0
    else
        return 1
    fi
}

# Get PID from file
get_pid() {
    if [ -f "$PID_FILE" ]; then
        cat "$PID_FILE"
    else
        echo ""
    fi
}

# Check if process is running
is_running() {
    local pid=$(get_pid)
    if [ -n "$pid" ] && kill -0 "$pid" 2>/dev/null; then
        return 0
    else
        return 1
    fi
}

# Start the server
start_server() {
    if is_running; then
        print_status "$YELLOW" "Browser server is already running (PID: $(get_pid))"
        return 0
    fi

    print_status "$GREEN" "Starting browser server..."
    
    # First, ensure Chrome is launched with debugging and incognito mode
    print_status "$GREEN" "Ensuring Chrome is running with debug port..."
    local launch_script="$(dirname "$SCRIPT_DIR")/launch-chrome-universal.sh"
    if [ -f "$launch_script" ]; then
        "$launch_script" launch
    else
        # Fallback to direct PowerShell command with incognito
        powershell.exe -Command "& 'C:\Program Files\Google\Chrome\Application\chrome.exe' --remote-debugging-port=9222 --incognito --no-first-run --no-default-browser-check" &
        sleep 3
    fi
    
    # Set environment variables
    export NODE_ENV=${NODE_ENV:-production}
    export CHROME_PATH=${CHROME_PATH:-"/mnt/c/Program Files/Google/Chrome/Application/chrome.exe"}
    export BROWSER_SERVER_PORT=${BROWSER_SERVER_PORT:-9222}
    export BROWSER_SERVER_HOST=${BROWSER_SERVER_HOST:-127.0.0.1}
    
    # Start the server in background
    nohup node "$SERVER_SCRIPT" >> "$LOG_FILE" 2>> "$ERROR_FILE" &
    local pid=$!
    
    # Save PID
    echo "$pid" > "$PID_FILE"
    
    # Wait a bit to see if it starts successfully
    sleep 2
    
    if kill -0 "$pid" 2>/dev/null; then
        print_status "$GREEN" "Browser server started successfully (PID: $pid)"
        return 0
    else
        print_status "$RED" "Failed to start browser server"
        return 1
    fi
}

# Stop the server
stop_server() {
    local pid=$(get_pid)
    
    if [ -z "$pid" ]; then
        print_status "$YELLOW" "Browser server is not running"
        return 0
    fi
    
    if ! is_running; then
        print_status "$YELLOW" "Browser server is not running (cleaning up PID file)"
        rm -f "$PID_FILE"
        return 0
    fi
    
    print_status "$GREEN" "Stopping browser server (PID: $pid)..."
    
    # Try graceful shutdown first
    kill -TERM "$pid" 2>/dev/null
    
    # Wait for process to stop
    local count=0
    while kill -0 "$pid" 2>/dev/null && [ $count -lt 30 ]; do
        sleep 1
        ((count++))
    done
    
    # Force kill if still running
    if kill -0 "$pid" 2>/dev/null; then
        print_status "$YELLOW" "Force stopping browser server..."
        kill -KILL "$pid" 2>/dev/null
    fi
    
    rm -f "$PID_FILE"
    print_status "$GREEN" "Browser server stopped"
}

# Restart the server
restart_server() {
    print_status "$GREEN" "Restarting browser server..."
    stop_server
    sleep 2
    start_server
}

# Show server status
show_status() {
    local pid=$(get_pid)
    
    echo "Browser Server Status:"
    echo "====================="
    
    if is_running; then
        print_status "$GREEN" "Status: Running"
        echo "PID: $pid"
        
        # Show process info
        if command -v ps &> /dev/null; then
            echo -e "\nProcess Info:"
            ps -p "$pid" -o pid,ppid,user,etime,cmd --no-headers
        fi
        
        # Show port info
        if command -v netstat &> /dev/null; then
            echo -e "\nListening Ports:"
            netstat -tlnp 2>/dev/null | grep "$pid" || echo "Unable to get port info (try with sudo)"
        fi
    else
        print_status "$RED" "Status: Not Running"
        
        if [ -f "$PID_FILE" ]; then
            echo "Stale PID file found: $pid"
        fi
    fi
    
    # Show log file info
    echo -e "\nLog Files:"
    if [ -f "$LOG_FILE" ]; then
        echo "Main log: $LOG_FILE ($(wc -l < "$LOG_FILE") lines)"
    fi
    if [ -f "$ERROR_FILE" ]; then
        echo "Error log: $ERROR_FILE ($(wc -l < "$ERROR_FILE") lines)"
    fi
}

# Show logs
show_logs() {
    local lines=${1:-20}
    
    if [ -f "$LOG_FILE" ]; then
        echo "=== Recent Log Entries ==="
        tail -n "$lines" "$LOG_FILE"
    fi
    
    if [ -f "$ERROR_FILE" ] && [ -s "$ERROR_FILE" ]; then
        echo -e "\n=== Recent Error Entries ==="
        tail -n "$lines" "$ERROR_FILE"
    fi
}

# Install as systemd service
install_systemd() {
    if ! check_systemd; then
        print_status "$RED" "Systemd is not available on this system"
        return 1
    fi
    
    print_status "$GREEN" "Installing systemd service..."
    
    # Update service file with current user
    sed "s/%USER%/$USER/g" "$SERVICE_FILE" > "/tmp/browser-server.service"
    
    # Copy service file
    sudo cp "/tmp/browser-server.service" /etc/systemd/system/browser-server.service
    
    # Reload systemd
    sudo systemctl daemon-reload
    
    # Enable service
    sudo systemctl enable browser-server.service
    
    print_status "$GREEN" "Systemd service installed successfully"
    echo "You can now use:"
    echo "  sudo systemctl start browser-server"
    echo "  sudo systemctl stop browser-server"
    echo "  sudo systemctl status browser-server"
}

# Uninstall systemd service
uninstall_systemd() {
    if ! check_systemd; then
        print_status "$RED" "Systemd is not available on this system"
        return 1
    fi
    
    print_status "$GREEN" "Uninstalling systemd service..."
    
    # Stop service if running
    sudo systemctl stop browser-server.service 2>/dev/null
    
    # Disable service
    sudo systemctl disable browser-server.service 2>/dev/null
    
    # Remove service file
    sudo rm -f /etc/systemd/system/browser-server.service
    
    # Reload systemd
    sudo systemctl daemon-reload
    
    print_status "$GREEN" "Systemd service uninstalled"
}

# Clean up logs
clean_logs() {
    print_status "$GREEN" "Cleaning up log files..."
    
    # Backup current logs
    if [ -f "$LOG_FILE" ]; then
        mv "$LOG_FILE" "$LOG_FILE.bak"
        touch "$LOG_FILE"
    fi
    
    if [ -f "$ERROR_FILE" ]; then
        mv "$ERROR_FILE" "$ERROR_FILE.bak"
        touch "$ERROR_FILE"
    fi
    
    print_status "$GREEN" "Log files cleaned (backups created)"
}

# Main script logic
case "$1" in
    start)
        start_server
        ;;
    stop)
        stop_server
        ;;
    restart)
        restart_server
        ;;
    status)
        show_status
        ;;
    logs)
        show_logs "${2:-20}"
        ;;
    install-systemd)
        install_systemd
        ;;
    uninstall-systemd)
        uninstall_systemd
        ;;
    clean-logs)
        clean_logs
        ;;
    *)
        echo "Browser Server Manager"
        echo "Usage: $0 {start|stop|restart|status|logs|install-systemd|uninstall-systemd|clean-logs}"
        echo ""
        echo "Commands:"
        echo "  start              Start the browser server"
        echo "  stop               Stop the browser server"
        echo "  restart            Restart the browser server"
        echo "  status             Show server status"
        echo "  logs [lines]       Show recent log entries (default: 20 lines)"
        echo "  install-systemd    Install as systemd service"
        echo "  uninstall-systemd  Uninstall systemd service"
        echo "  clean-logs         Clean up log files"
        exit 1
        ;;
esac

exit $?
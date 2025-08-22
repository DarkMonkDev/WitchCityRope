#!/bin/bash

# Auto-start script for Browser Server
# Add this to your .bashrc or .profile to start the server automatically

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MANAGER_SCRIPT="$SCRIPT_DIR/browser-server-manager.sh"

# Function to check and start browser server
check_and_start_browser_server() {
    # Check if the manager script exists
    if [ ! -f "$MANAGER_SCRIPT" ]; then
        echo "Browser server manager not found at: $MANAGER_SCRIPT"
        return 1
    fi

    # Check server status
    if ! "$MANAGER_SCRIPT" status | grep -q "Status: Running"; then
        echo "Starting browser server..."
        "$MANAGER_SCRIPT" start
    else
        echo "Browser server is already running"
    fi
}

# Only run if not in an SSH session (to avoid starting on every SSH connection)
if [ -z "$SSH_CLIENT" ] && [ -z "$SSH_TTY" ]; then
    check_and_start_browser_server
fi

# Alternative: Always run but with a lock file to prevent multiple starts
# LOCK_FILE="/tmp/browser-server-autostart.lock"
# if [ ! -f "$LOCK_FILE" ]; then
#     touch "$LOCK_FILE"
#     check_and_start_browser_server
# fi
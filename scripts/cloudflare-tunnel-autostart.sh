#!/bin/bash
# Auto-start Cloudflare tunnel for WitchCityRope development
# This script should be copied to ~/.witchcityrope-tunnel-autostart.sh
# and added to ~/.bashrc for automatic startup

TUNNEL_NAME="witchcityrope-dev"
LOCKFILE="/tmp/cloudflare-tunnel-$TUNNEL_NAME.lock"
PIDFILE="/tmp/cloudflare-tunnel-$TUNNEL_NAME.pid"

# Function to check if tunnel is running
is_tunnel_running() {
    if [ -f "$PIDFILE" ]; then
        PID=$(cat "$PIDFILE")
        if ps -p "$PID" > /dev/null 2>&1; then
            return 0
        fi
    fi
    return 1
}

# Start tunnel if not running
if ! is_tunnel_running; then
    echo "🚀 Starting Cloudflare tunnel for dev-api.chadfbennett.com..."
    
    # Use flock to prevent multiple simultaneous starts
    (
        flock -n 200 || exit 1
        
        # Double-check it's still not running
        if ! is_tunnel_running; then
            nohup cloudflared tunnel run witchcityrope-dev > ~/.cloudflare-tunnel.log 2>&1 &
            echo $! > "$PIDFILE"
            echo "✅ Tunnel started (PID: $!)"
            echo "📝 Logs: ~/.cloudflare-tunnel.log"
            echo "🌐 URL: https://dev-api.chadfbennett.com → localhost:5655"
        fi
    ) 200>"$LOCKFILE"
else
    PID=$(cat "$PIDFILE")
    echo "✅ Tunnel already running (PID: $PID)"
    echo "🌐 URL: https://dev-api.chadfbennett.com → localhost:5655"
fi
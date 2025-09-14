#!/bin/bash

# Auto-detect API port and start tunnel for webhook testing
# This script maintains consistent behavior across sessions

set -e

echo "🔍 Detecting API port..."

# Check common ports in order of preference
PORTS=(5655 5656 5653)
API_PORT=""

for PORT in "${PORTS[@]}"; do
    if lsof -Pi :$PORT -sTCP:LISTEN -t >/dev/null 2>&1; then
        API_PORT=$PORT
        echo "✅ Found API running on port $PORT"
        break
    fi
done

# If no API found, check docker containers
if [ -z "$API_PORT" ]; then
    echo "🐳 Checking Docker containers..."
    if docker ps --format "table {{.Names}}\t{{.Ports}}" 2>/dev/null | grep -q "api"; then
        API_PORT=$(docker ps --format "{{.Ports}}" --filter "name=api" | grep -oP '\d+(?=->5655)' | head -1)
        if [ -z "$API_PORT" ]; then
            API_PORT=5655  # Default docker port
        fi
        echo "✅ Found API in Docker on port $API_PORT"
    fi
fi

# If still no API found, try starting it
if [ -z "$API_PORT" ]; then
    echo "⚠️  No API detected. Starting API..."
    cd /home/chad/repos/witchcityrope-react/apps/api
    dotnet run --urls http://localhost:5655 &
    API_PORT=5655
    sleep 5  # Wait for API to start
fi

# Check if ngrok is installed
if ! command -v ngrok &> /dev/null && ! [ -f ~/bin/ngrok ]; then
    echo "📦 Installing ngrok..."
    /home/chad/repos/witchcityrope-react/scripts/install-ngrok.sh
fi

# Use ngrok from ~/bin if not in PATH
NGROK_CMD="ngrok"
if [ -f ~/bin/ngrok ]; then
    NGROK_CMD="$HOME/bin/ngrok"
fi

echo "🚀 Starting ngrok tunnel on port $API_PORT..."

# Kill any existing ngrok processes
pkill -f ngrok || true

# Start ngrok with consistent subdomain (requires paid plan)
# For free plan, we'll use a config file to maintain consistency
if [ -f ~/.ngrok2/ngrok.yml ]; then
    $NGROK_CMD http $API_PORT --config ~/.ngrok2/ngrok.yml
else
    # Create config for consistency
    mkdir -p ~/.ngrok2
    cat > ~/.ngrok2/ngrok.yml << EOF
version: "2"
authtoken: YOUR_NGROK_AUTH_TOKEN
tunnels:
  witchcityrope:
    proto: http
    addr: $API_PORT
    inspect: true
    bind_tls: true
EOF
    
    echo ""
    echo "⚠️  FIRST TIME SETUP REQUIRED:"
    echo "1. Sign up for free ngrok account: https://dashboard.ngrok.com/signup"
    echo "2. Get your authtoken: https://dashboard.ngrok.com/get-started/your-authtoken"
    echo "3. Run: $NGROK_CMD config add-authtoken YOUR_TOKEN"
    echo "4. For stable URLs, consider ngrok paid plan or Cloudflare Tunnel"
    echo ""
    echo "Starting ngrok without auth (limited functionality)..."
    $NGROK_CMD http $API_PORT
fi
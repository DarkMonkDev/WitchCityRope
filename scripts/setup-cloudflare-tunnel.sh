#!/bin/bash

# Setup Cloudflare Tunnel for WitchCityRope Development
# Using chadfbennett.com domain for dev tunneling

set -e

echo "🚀 Setting up Cloudflare Tunnel for local development"
echo "Domain: dev-api.chadfbennett.com"
echo ""

# Check if cloudflared is installed
if ! command -v cloudflared &> /dev/null; then
    echo "📦 Installing cloudflared..."
    wget -q https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-linux-amd64.deb
    sudo dpkg -i cloudflared-linux-amd64.deb
    rm cloudflared-linux-amd64.deb
    echo "✅ cloudflared installed"
else
    echo "✅ cloudflared already installed"
fi

# Check if already logged in
if [ ! -d ~/.cloudflared ]; then
    echo ""
    echo "🔐 Please login to Cloudflare..."
    echo "A browser window will open for authentication."
    cloudflared tunnel login
fi

# Check if tunnel already exists
if cloudflared tunnel list | grep -q "witchcityrope-dev"; then
    echo "✅ Tunnel 'witchcityrope-dev' already exists"
    TUNNEL_ID=$(cloudflared tunnel list | grep witchcityrope-dev | awk '{print $1}')
else
    echo "🔧 Creating tunnel 'witchcityrope-dev'..."
    cloudflared tunnel create witchcityrope-dev
    TUNNEL_ID=$(cloudflared tunnel list | grep witchcityrope-dev | awk '{print $1}')
fi

echo "📝 Tunnel ID: $TUNNEL_ID"

# Create config file
CONFIG_FILE=~/.cloudflared/config.yml
echo "📝 Creating config file at $CONFIG_FILE"

cat > $CONFIG_FILE << EOF
# Cloudflare Tunnel Configuration for WitchCityRope Dev
tunnel: $TUNNEL_ID
credentials-file: /home/$USER/.cloudflared/$TUNNEL_ID.json

# Ingress rules
ingress:
  # Main API endpoint
  - hostname: dev-api.chadfbennett.com
    service: http://localhost:5655
  # Catch-all
  - service: http_status:404
EOF

echo "✅ Config file created"

# Create DNS record
echo ""
echo "🌐 Creating DNS record for dev-api.chadfbennett.com..."
cloudflared tunnel route dns witchcityrope-dev dev-api.chadfbennett.com || echo "DNS record may already exist"

echo ""
echo "✅ Setup complete!"
echo ""
echo "📋 Next Steps:"
echo "1. The tunnel is configured to use: https://dev-api.chadfbennett.com"
echo "2. This URL will forward to localhost:5655"
echo "3. Run the tunnel with: cloudflared tunnel run witchcityrope-dev"
echo "4. Or use the auto-start script: ~/.witchcityrope-tunnel-autostart.sh"
echo ""
echo "🔧 Creating auto-start script..."

# Create auto-start script
cat > ~/.witchcityrope-tunnel-autostart.sh << 'EOF'
#!/bin/bash
# Auto-start Cloudflare tunnel if not running

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
EOF

chmod +x ~/.witchcityrope-tunnel-autostart.sh

echo "✅ Auto-start script created"
echo ""
echo "🔧 Adding to .bashrc for automatic startup..."

# Add to bashrc if not already there
if ! grep -q "witchcityrope-tunnel-autostart" ~/.bashrc; then
    cat >> ~/.bashrc << 'EOF'

# Auto-start WitchCityRope development tunnel
if [ -f ~/.witchcityrope-tunnel-autostart.sh ]; then
    ~/.witchcityrope-tunnel-autostart.sh
fi
EOF
    echo "✅ Added to .bashrc"
else
    echo "✅ Already in .bashrc"
fi

echo ""
echo "🎉 Setup Complete!"
echo ""
echo "The tunnel will now auto-start when you open a terminal."
echo ""
echo "📋 Summary:"
echo "- Domain: https://dev-api.chadfbennett.com"
echo "- Points to: localhost:5655"
echo "- Auto-starts on terminal open"
echo "- Logs: ~/.cloudflare-tunnel.log"
echo ""
echo "🔄 To start manually: cloudflared tunnel run witchcityrope-dev"
echo "🛑 To stop: pkill cloudflared"
echo "📊 To check status: ps aux | grep cloudflared"
echo ""
echo "⚠️  IMPORTANT: Restart your terminal or run: source ~/.bashrc"
#!/bin/bash

# Fix Cloudflare authentication issue
# The browser auth completed but cert.pem wasn't saved

echo "🔧 Fixing Cloudflare Tunnel authentication..."
echo ""

# Clean up old config
echo "🧹 Cleaning up old configuration..."
rm -rf ~/.cloudflared/*.json 2>/dev/null
rm -rf ~/.cloudflared/cert.pem 2>/dev/null

echo ""
echo "📋 Instructions:"
echo "1. Run this command in your terminal:"
echo ""
echo "   cloudflared tunnel login"
echo ""
echo "2. A browser window will open (or copy the URL if it doesn't)"
echo "3. Select 'chadfbennett.com' when prompted"
echo "4. Click 'Authorize'"
echo "5. WAIT for the terminal to show 'Certificate installed successfully'"
echo "6. If it times out, that's OK - just run this script again"
echo ""
echo "Press Enter to start the authentication process..."
read

# Run login with proper environment
BROWSER=firefox cloudflared tunnel login

# Check if cert was created
if [ -f ~/.cloudflared/cert.pem ]; then
    echo ""
    echo "✅ Authentication successful!"
    echo ""
    echo "🔧 Now creating tunnel..."
    
    # Create tunnel
    cloudflared tunnel create witchcityrope-dev
    
    # Get tunnel ID
    TUNNEL_ID=$(cloudflared tunnel list | grep witchcityrope-dev | awk '{print $1}')
    
    if [ -n "$TUNNEL_ID" ]; then
        echo "✅ Tunnel created with ID: $TUNNEL_ID"
        
        # Create config
        cat > ~/.cloudflared/config.yml << EOF
tunnel: $TUNNEL_ID
credentials-file: /home/$USER/.cloudflared/$TUNNEL_ID.json

ingress:
  - hostname: dev-api.chadfbennett.com
    service: http://localhost:5655
  - service: http_status:404
EOF
        
        echo "✅ Config created"
        
        # Create DNS route
        echo "🌐 Creating DNS route..."
        cloudflared tunnel route dns witchcityrope-dev dev-api.chadfbennett.com || echo "DNS route may already exist"
        
        echo ""
        echo "✅ Setup complete!"
        echo ""
        echo "🚀 To start the tunnel:"
        echo "   cloudflared tunnel run witchcityrope-dev"
        echo ""
        echo "🌐 Your URL: https://dev-api.chadfbennett.com → localhost:5655"
    else
        echo "❌ Failed to create tunnel. Please try again."
    fi
else
    echo ""
    echo "⚠️  Authentication may have timed out."
    echo "Please run this script again and complete the browser auth faster."
    echo ""
    echo "Alternative: Try this manual process:"
    echo "1. Open browser and go to: https://dash.cloudflare.com"
    echo "2. Go to Zero Trust → Access → Tunnels"
    echo "3. Create a tunnel manually named 'witchcityrope-dev'"
    echo "4. Download the certificate for the tunnel"
fi
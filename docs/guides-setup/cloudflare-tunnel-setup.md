# Cloudflare Tunnel Setup for Local Development

## Overview
This guide explains how to set up Cloudflare Tunnel for local development, providing a permanent HTTPS URL that tunnels to your local API. This eliminates the need to update PayPal webhook URLs every time you restart your development environment.

## Benefits
- **Permanent URL**: `https://dev-api.chadfbennett.com` never changes
- **Automatic startup**: Tunnel starts when you open a terminal
- **Free**: No cost for basic usage
- **Secure**: End-to-end encryption
- **No port forwarding**: Works behind firewalls and NAT

## Prerequisites
- Ubuntu/Linux environment
- Cloudflare account (free)
- Domain added to Cloudflare (chadfbennett.com in our case)

## Installation Steps

### Step 1: Install Cloudflared
```bash
# Download and install cloudflared
wget -q https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-linux-amd64.deb
sudo dpkg -i cloudflared-linux-amd64.deb
rm cloudflared-linux-amd64.deb
```

### Step 2: Authenticate with Cloudflare
```bash
# Login to Cloudflare (browser will open)
cloudflared tunnel login

# Select your domain when prompted
# Click "Authorize" to complete authentication
```

### Step 3: Create the Tunnel
```bash
# Create a named tunnel
cloudflared tunnel create witchcityrope-dev

# Note the tunnel ID that's displayed (looks like: 6ff42ae2-765d-4adf-8112-31c55c1551ef)
```

### Step 4: Configure the Tunnel
Create `~/.cloudflared/config.yml`:
```yaml
tunnel: YOUR-TUNNEL-ID
credentials-file: /home/$USER/.cloudflared/YOUR-TUNNEL-ID.json

ingress:
  - hostname: dev-api.chadfbennett.com
    service: http://localhost:5655
  - service: http_status:404
```

### Step 5: Create DNS Record
```bash
# Point subdomain to the tunnel
cloudflared tunnel route dns witchcityrope-dev dev-api.chadfbennett.com
```

### Step 6: Set Up Auto-Start Script
```bash
# Copy the auto-start script to your home directory
cp /home/chad/repos/witchcityrope-react/scripts/cloudflare-tunnel-autostart.sh ~/.witchcityrope-tunnel-autostart.sh
chmod +x ~/.witchcityrope-tunnel-autostart.sh

# Add to .bashrc for automatic startup
echo '
# Auto-start WitchCityRope development tunnel
if [ -f ~/.witchcityrope-tunnel-autostart.sh ]; then
    ~/.witchcityrope-tunnel-autostart.sh
fi' >> ~/.bashrc

# Reload bashrc
source ~/.bashrc
```

## How It Works

### Automatic Startup
When you open a new terminal:
1. `.bashrc` runs the auto-start script
2. Script checks if tunnel is already running
3. If not running, starts tunnel in background
4. Tunnel connects `https://dev-api.chadfbennett.com` to `localhost:5655`

### Manual Control
```bash
# Check if tunnel is running
ps aux | grep cloudflared

# View tunnel logs
tail -f ~/.cloudflare-tunnel.log

# Stop the tunnel
pkill cloudflared

# Start manually (if needed)
cloudflared tunnel run witchcityrope-dev
```

## Troubleshooting

### Tunnel Not Starting
```bash
# Check for errors in log
tail -n 50 ~/.cloudflare-tunnel.log

# Try starting manually to see errors
cloudflared tunnel run witchcityrope-dev
```

### Authentication Issues
```bash
# Re-authenticate
rm ~/.cloudflared/cert.pem
cloudflared tunnel login
```

### DNS Not Resolving
```bash
# Check DNS record
nslookup dev-api.chadfbennett.com

# Verify tunnel is registered
cloudflared tunnel list
```

### Port Conflicts
```bash
# Check what's using port 5655
lsof -i :5655

# Kill process if needed
kill -9 <PID>
```

## Configuration Files

### Environment Variables
The tunnel URL is configured in `.env.development`:
```env
WEBHOOK_BASE_URL=https://dev-api.chadfbennett.com
API_PORT=5655
```

### PayPal Webhook
Local development webhook URL:
```
https://dev-api.chadfbennett.com/api/webhooks/paypal
```

## Security Notes
- The tunnel is only for development, not production
- Anyone with the URL can access your local API
- Use authentication/authorization in your API
- Don't expose sensitive data through the tunnel

## Team Setup
For other team members to set up their environment:

1. Install cloudflared (Step 1)
2. Authenticate with their own Cloudflare account (Step 2)
3. Create their own tunnel with a unique subdomain
4. Update their `.env.development` with their tunnel URL
5. Use the auto-start script from `/scripts/cloudflare-tunnel-autostart.sh`

## Alternative: Using Different Subdomain
If team members need their own subdomains:
```bash
# Create tunnel with custom name
cloudflared tunnel create john-dev

# Use subdomain like: john-api.chadfbennett.com
# Update config.yml accordingly
```

## Resources
- [Cloudflare Tunnel Documentation](https://developers.cloudflare.com/cloudflare-one/connections/connect-apps/)
- [Cloudflared CLI Reference](https://developers.cloudflare.com/cloudflare-one/connections/connect-apps/install-and-setup/tunnel-useful-commands/)
- Auto-start script: `/scripts/cloudflare-tunnel-autostart.sh`
- Setup script: `/scripts/setup-cloudflare-tunnel.sh`
#!/bin/bash
# Install Docker, Nginx, and Required Dependencies
# Run this script as the witchcity user (not root)
# Usage: sudo -u witchcity ./02-install-dependencies.sh

set -euo pipefail

# Configuration
DOCKER_COMPOSE_VERSION="2.24.0"
NGINX_USER="www-data"

echo "🚀 Installing Docker, Nginx, and dependencies for WitchCityRope..."
echo "📅 Started at: $(date)"

# Check if running as correct user
if [ "$USER" = "root" ]; then
    echo "❌ This script should not be run as root"
    echo "   Please run: sudo -u witchcity ./02-install-dependencies.sh"
    exit 1
fi

# Install Docker
echo "🐳 Installing Docker..."
if ! command -v docker &> /dev/null; then
    # Add Docker's official GPG key
    sudo apt update
    sudo apt install -y ca-certificates curl gnupg

    sudo install -m 0755 -d /etc/apt/keyrings
    curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
    sudo chmod a+r /etc/apt/keyrings/docker.gpg

    # Add Docker repository
    echo \
        "deb [arch="$(dpkg --print-architecture)" signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu \
        "$(. /etc/os-release && echo "$VERSION_CODENAME")" stable" | \
        sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

    # Install Docker Engine
    sudo apt update
    sudo apt install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin

    # Add current user to docker group
    sudo usermod -aG docker "$USER"

    echo "✅ Docker installed successfully"
else
    echo "✅ Docker is already installed"
fi

# Install Docker Compose
echo "🔧 Installing Docker Compose v${DOCKER_COMPOSE_VERSION}..."
if ! command -v docker-compose &> /dev/null; then
    # Download Docker Compose
    sudo curl -L "https://github.com/docker/compose/releases/download/v${DOCKER_COMPOSE_VERSION}/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose

    # Make it executable
    sudo chmod +x /usr/local/bin/docker-compose

    # Create symbolic link for easy access
    sudo ln -sf /usr/local/bin/docker-compose /usr/bin/docker-compose

    echo "✅ Docker Compose installed successfully"
else
    echo "✅ Docker Compose is already installed"
fi

# Verify Docker installation
echo "🔍 Verifying Docker installation..."
docker --version
docker-compose --version

# Install Nginx
echo "🌐 Installing Nginx..."
if ! command -v nginx &> /dev/null; then
    sudo apt update
    sudo apt install -y nginx

    # Start and enable Nginx
    sudo systemctl start nginx
    sudo systemctl enable nginx

    echo "✅ Nginx installed and started"
else
    echo "✅ Nginx is already installed"
fi

# Install Certbot for SSL certificates
echo "🔒 Installing Certbot for SSL certificates..."
if ! command -v certbot &> /dev/null; then
    sudo apt install -y certbot python3-certbot-nginx
    echo "✅ Certbot installed successfully"
else
    echo "✅ Certbot is already installed"
fi

# Install Redis for caching
echo "📦 Installing Redis..."
if ! command -v redis-server &> /dev/null; then
    sudo apt install -y redis-server

    # Configure Redis for production use
    sudo sed -i 's/^# maxmemory <bytes>/maxmemory 512mb/' /etc/redis/redis.conf
    sudo sed -i 's/^# maxmemory-policy noeviction/maxmemory-policy allkeys-lru/' /etc/redis/redis.conf

    # Start and enable Redis
    sudo systemctl start redis-server
    sudo systemctl enable redis-server

    echo "✅ Redis installed and configured"
else
    echo "✅ Redis is already installed"
fi

# Install monitoring tools
echo "📊 Installing monitoring tools..."
sudo apt install -y \
    htop \
    iotop \
    nethogs \
    ncdu \
    tree \
    jsonlint

# Install Node.js (for build processes)
echo "📦 Installing Node.js 20..."
if ! command -v node &> /dev/null; then
    curl -fsSL https://deb.nodesource.com/setup_20.x | sudo -E bash -
    sudo apt install -y nodejs

    echo "✅ Node.js installed"
    node --version
    npm --version
else
    echo "✅ Node.js is already installed"
fi

# Create systemd service for Docker to start on boot
echo "⚙️  Configuring Docker service..."
sudo systemctl enable docker
sudo systemctl enable containerd

# Configure Docker daemon for better performance
echo "🔧 Configuring Docker daemon..."
sudo mkdir -p /etc/docker
sudo tee /etc/docker/daemon.json > /dev/null << 'EOF'
{
    "log-driver": "json-file",
    "log-opts": {
        "max-size": "10m",
        "max-file": "3"
    },
    "storage-driver": "overlay2",
    "dns": ["8.8.8.8", "1.1.1.1"],
    "experimental": false,
    "live-restore": true
}
EOF

sudo systemctl restart docker
echo "✅ Docker daemon configured"

# Create Nginx basic configuration structure
echo "🌐 Setting up Nginx configuration structure..."
sudo mkdir -p /etc/nginx/sites-available/witchcityrope
sudo mkdir -p /var/log/nginx/witchcityrope

# Create basic Nginx default site
sudo tee /etc/nginx/sites-available/default > /dev/null << 'EOF'
server {
    listen 80 default_server;
    listen [::]:80 default_server;

    root /var/www/html;
    index index.html index.htm index.nginx-debian.html;

    server_name _;

    location / {
        return 200 'WitchCityRope Server Ready - Nginx is working!';
        add_header Content-Type text/plain;
    }

    location /health {
        access_log off;
        return 200 "healthy\n";
        add_header Content-Type text/plain;
    }
}
EOF

# Test Nginx configuration
sudo nginx -t
sudo systemctl reload nginx
echo "✅ Nginx basic configuration created"

# Create Docker networks for the application
echo "🔗 Creating Docker networks..."
docker network create witchcity-network || echo "Network already exists"
docker network create witchcity-production || echo "Network already exists"
docker network create witchcity-staging || echo "Network already exists"

echo "✅ Docker networks created"

# Set up log directories with proper permissions
echo "📝 Setting up logging directories..."
sudo mkdir -p /var/log/witchcityrope/{production,staging}
sudo mkdir -p /var/log/nginx/witchcityrope
sudo chown -R "$USER:$USER" /var/log/witchcityrope
sudo chown -R "$NGINX_USER:$NGINX_USER" /var/log/nginx/witchcityrope

echo "✅ Logging directories configured"

# Create Docker Compose override files directory
echo "📁 Creating Docker Compose structure..."
mkdir -p ~/docker-compose-overrides
mkdir -p ~/environment-configs

# Create basic health check script
echo "🔍 Creating health check script..."
tee /opt/witchcityrope/health-check.sh > /dev/null << 'EOF'
#!/bin/bash
# WitchCityRope Health Check Script
# Checks all services and reports status

set -euo pipefail

echo "🔍 WitchCityRope Health Check - $(date)"
echo "=============================================="

# Check Docker
if systemctl is-active --quiet docker; then
    echo "✅ Docker service: Running"
else
    echo "❌ Docker service: Not running"
fi

# Check Nginx
if systemctl is-active --quiet nginx; then
    echo "✅ Nginx service: Running"
else
    echo "❌ Nginx service: Not running"
fi

# Check Redis
if systemctl is-active --quiet redis-server; then
    echo "✅ Redis service: Running"
else
    echo "❌ Redis service: Not running"
fi

# Check disk space
DISK_USAGE=$(df / | tail -1 | awk '{print $5}' | sed 's/%//')
if [ "$DISK_USAGE" -lt 80 ]; then
    echo "✅ Disk usage: ${DISK_USAGE}%"
else
    echo "⚠️  Disk usage: ${DISK_USAGE}% (Warning: > 80%)"
fi

# Check memory
MEMORY_USAGE=$(free | grep Mem | awk '{printf("%.0f", $3/$2 * 100.0)}')
if [ "$MEMORY_USAGE" -lt 85 ]; then
    echo "✅ Memory usage: ${MEMORY_USAGE}%"
else
    echo "⚠️  Memory usage: ${MEMORY_USAGE}% (Warning: > 85%)"
fi

# Check Docker containers if any are running
if docker ps -q | grep -q .; then
    echo ""
    echo "📦 Running Docker containers:"
    docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
else
    echo "📦 No Docker containers currently running"
fi

echo ""
echo "🔗 Network connectivity test:"
if curl -s --max-time 5 http://google.com > /dev/null; then
    echo "✅ Internet connectivity: OK"
else
    echo "❌ Internet connectivity: Failed"
fi

echo "=============================================="
echo "Health check completed at $(date)"
EOF

chmod +x /opt/witchcityrope/health-check.sh
echo "✅ Health check script created"

# Create daily maintenance script
echo "🔄 Creating daily maintenance script..."
tee /opt/witchcityrope/daily-maintenance.sh > /dev/null << 'EOF'
#!/bin/bash
# WitchCityRope Daily Maintenance Script
# Cleans up logs, unused Docker resources, and performs health checks

set -euo pipefail

LOG_FILE="/var/log/witchcityrope/daily-maintenance.log"
exec 1> >(tee -a "$LOG_FILE")
exec 2>&1

echo "🔄 Daily Maintenance Started - $(date)"
echo "======================================="

# Clean up Docker resources
echo "🐳 Cleaning up Docker resources..."
docker system prune -f --volumes
docker image prune -f

# Clean up old logs (older than 30 days)
echo "🗑️  Cleaning up old logs..."
find /var/log/witchcityrope -name "*.log*" -mtime +30 -delete

# Update package lists
echo "📦 Updating package lists..."
apt update

# Run health check
echo "🔍 Running health check..."
/opt/witchcityrope/health-check.sh

echo "======================================="
echo "Daily maintenance completed - $(date)"
echo ""
EOF

chmod +x /opt/witchcityrope/daily-maintenance.sh

# Add daily maintenance to cron
echo "⏰ Setting up daily maintenance cron job..."
(crontab -l 2>/dev/null; echo "0 2 * * * /opt/witchcityrope/daily-maintenance.sh") | crontab -

echo "✅ Daily maintenance configured"

# Test installations
echo "🧪 Testing installations..."

# Test Docker
echo "Testing Docker..."
if docker run --rm hello-world > /dev/null 2>&1; then
    echo "✅ Docker test: Passed"
else
    echo "❌ Docker test: Failed"
fi

# Test Nginx
echo "Testing Nginx..."
if curl -f http://localhost/health > /dev/null 2>&1; then
    echo "✅ Nginx test: Passed"
else
    echo "❌ Nginx test: Failed"
fi

# Test Redis
echo "Testing Redis..."
if redis-cli ping | grep -q PONG; then
    echo "✅ Redis test: Passed"
else
    echo "❌ Redis test: Failed"
fi

# Final summary
echo ""
echo "✅ Dependencies installation completed successfully!"
echo ""
echo "📋 Installation Summary:"
echo "   • Docker Engine: $(docker --version)"
echo "   • Docker Compose: $(docker-compose --version)"
echo "   • Nginx: $(nginx -v 2>&1 | cut -d: -f2 | xargs)"
echo "   • Redis: $(redis-server --version | cut -d' ' -f3)"
echo "   • Certbot: $(certbot --version | cut -d' ' -f2)"
echo "   • Node.js: $(node --version)"
echo "   • NPM: $(npm --version)"
echo ""
echo "🔧 Services Status:"
echo "   • Docker: $(systemctl is-active docker)"
echo "   • Nginx: $(systemctl is-active nginx)"
echo "   • Redis: $(systemctl is-active redis-server)"
echo ""
echo "📁 Directory Structure:"
echo "   • Application: /opt/witchcityrope/"
echo "   • Logs: /var/log/witchcityrope/"
echo "   • Nginx configs: /etc/nginx/sites-available/witchcityrope/"
echo ""
echo "🚨 IMPORTANT NEXT STEPS:"
echo "   1. Log out and back in to pick up Docker group membership"
echo "   2. Run health check: /opt/witchcityrope/health-check.sh"
echo "   3. Run next script: 03-database-setup.sh"
echo ""
echo "🔧 Useful Commands:"
echo "   • Health check: /opt/witchcityrope/health-check.sh"
echo "   • Daily maintenance: /opt/witchcityrope/daily-maintenance.sh"
echo "   • Docker status: docker ps"
echo "   • Nginx status: sudo systemctl status nginx"
echo "   • View logs: tail -f /var/log/witchcityrope/daily-maintenance.log"
echo ""
echo "📅 Completed at: $(date)"

echo ""
echo "⚠️  Please log out and back in to pick up Docker group membership!"
echo "   Then test Docker without sudo: docker ps"
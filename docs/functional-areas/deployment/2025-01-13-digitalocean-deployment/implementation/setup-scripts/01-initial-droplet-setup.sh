#!/bin/bash
# Initial DigitalOcean Droplet Setup Script
# Sets up the basic server configuration, security, and users
# Usage: Run this script on a fresh Ubuntu 24.04 droplet as root

set -euo pipefail

# Configuration
DEPLOY_USER="witchcity"
SSH_PUBLIC_KEY_PATH="/tmp/witchcity_rsa.pub"  # Upload your SSH public key here first
TIMEZONE="America/New_York"  # Salem, MA timezone

echo "🚀 Starting WitchCityRope droplet initial setup..."
echo "📅 Started at: $(date)"

# Update system packages
echo "📦 Updating system packages..."
apt update
apt upgrade -y

# Install essential packages
echo "🛠️  Installing essential packages..."
apt install -y \
    curl \
    wget \
    git \
    vim \
    htop \
    unzip \
    software-properties-common \
    apt-transport-https \
    ca-certificates \
    gnupg \
    lsb-release \
    ufw \
    fail2ban \
    logrotate \
    rsync \
    jq

# Set system timezone
echo "⏰ Setting timezone to ${TIMEZONE}..."
timedatectl set-timezone "${TIMEZONE}"

# Create deploy user
echo "👤 Creating deploy user '${DEPLOY_USER}'..."
if ! id "${DEPLOY_USER}" &>/dev/null; then
    useradd -m -s /bin/bash "${DEPLOY_USER}"
    usermod -aG sudo "${DEPLOY_USER}"

    # Set up SSH key for deploy user
    sudo -u "${DEPLOY_USER}" mkdir -p "/home/${DEPLOY_USER}/.ssh"

    if [ -f "${SSH_PUBLIC_KEY_PATH}" ]; then
        cat "${SSH_PUBLIC_KEY_PATH}" > "/home/${DEPLOY_USER}/.ssh/authorized_keys"
        chown "${DEPLOY_USER}:${DEPLOY_USER}" "/home/${DEPLOY_USER}/.ssh/authorized_keys"
        chmod 600 "/home/${DEPLOY_USER}/.ssh/authorized_keys"
        chmod 700 "/home/${DEPLOY_USER}/.ssh"
        echo "✅ SSH key configured for ${DEPLOY_USER}"
    else
        echo "⚠️  SSH public key not found at ${SSH_PUBLIC_KEY_PATH}"
        echo "   Please add SSH key manually after setup"
    fi
else
    echo "✅ User ${DEPLOY_USER} already exists"
fi

# Configure SSH security
echo "🔒 Configuring SSH security..."
SSH_CONFIG="/etc/ssh/sshd_config"

# Backup original SSH config
cp "${SSH_CONFIG}" "${SSH_CONFIG}.backup.$(date +%Y%m%d)"

# SSH security settings
cat >> "${SSH_CONFIG}" << 'EOF'

# WitchCityRope Security Configuration
PasswordAuthentication no
PermitRootLogin no
PubkeyAuthentication yes
AuthorizedKeysFile .ssh/authorized_keys
ChallengeResponseAuthentication no
UsePAM yes
X11Forwarding no
PrintMotd no
AcceptEnv LANG LC_*
Subsystem sftp /usr/lib/openssh/sftp-server
ClientAliveInterval 300
ClientAliveCountMax 2
MaxAuthTries 3
MaxSessions 2
EOF

# Restart SSH service
systemctl reload ssh
echo "✅ SSH security configured"

# Configure UFW firewall
echo "🔥 Configuring UFW firewall..."
ufw --force reset
ufw default deny incoming
ufw default allow outgoing

# Allow SSH
ufw allow ssh

# Allow HTTP/HTTPS
ufw allow 80/tcp
ufw allow 443/tcp

# Allow custom application ports
ufw allow 3001/tcp  # Production React
ufw allow 3002/tcp  # Staging React
ufw allow 5001/tcp  # Production API
ufw allow 5002/tcp  # Staging API

# Enable firewall
ufw --force enable
echo "✅ UFW firewall configured"

# Configure fail2ban
echo "🛡️  Configuring fail2ban..."
cat > /etc/fail2ban/jail.local << 'EOF'
[DEFAULT]
bantime = 3600
findtime = 600
maxretry = 5
backend = systemd

[sshd]
enabled = true
port = ssh
logpath = %(sshd_log)s
maxretry = 3
bantime = 86400

[nginx-http-auth]
enabled = true
port = http,https
logpath = /var/log/nginx/error.log

[nginx-limit-req]
enabled = true
port = http,https
logpath = /var/log/nginx/error.log
maxretry = 10
EOF

systemctl enable fail2ban
systemctl start fail2ban
echo "✅ fail2ban configured"

# Configure automatic security updates
echo "🔄 Configuring automatic security updates..."
cat > /etc/apt/apt.conf.d/50unattended-upgrades << 'EOF'
Unattended-Upgrade::Allowed-Origins {
    "${distro_id}:${distro_codename}-security";
    "${distro_id}ESM:${distro_codename}";
};
Unattended-Upgrade::AutoFixInterruptedDpkg "true";
Unattended-Upgrade::MinimalSteps "true";
Unattended-Upgrade::Remove-Unused-Dependencies "true";
Unattended-Upgrade::Automatic-Reboot "false";
EOF

cat > /etc/apt/apt.conf.d/20auto-upgrades << 'EOF'
APT::Periodic::Update-Package-Lists "1";
APT::Periodic::Unattended-Upgrade "1";
EOF

echo "✅ Automatic security updates configured"

# Create application directories
echo "📁 Creating application directories..."
mkdir -p /opt/witchcityrope/{production,staging}
mkdir -p /var/log/witchcityrope
mkdir -p /opt/backups/witchcityrope

# Set ownership for application directories
chown -R "${DEPLOY_USER}:${DEPLOY_USER}" /opt/witchcityrope
chown -R "${DEPLOY_USER}:${DEPLOY_USER}" /var/log/witchcityrope
chown -R "${DEPLOY_USER}:${DEPLOY_USER}" /opt/backups/witchcityrope

echo "✅ Application directories created"

# Configure log rotation
echo "🗞️  Configuring log rotation..."
cat > /etc/logrotate.d/witchcityrope << 'EOF'
/var/log/witchcityrope/*.log {
    daily
    rotate 30
    compress
    delaycompress
    missingok
    notifempty
    create 0644 witchcity witchcity
    postrotate
        systemctl reload docker || true
    endscript
}
EOF

echo "✅ Log rotation configured"

# Configure system limits
echo "⚡ Configuring system limits..."
cat >> /etc/security/limits.conf << 'EOF'

# WitchCityRope system limits
* soft nofile 65536
* hard nofile 65536
* soft nproc 32768
* hard nproc 32768
EOF

# Configure sysctl for better performance
cat > /etc/sysctl.d/99-witchcityrope.conf << 'EOF'
# WitchCityRope performance tuning
net.core.somaxconn = 65536
net.core.netdev_max_backlog = 5000
net.ipv4.tcp_max_syn_backlog = 65536
net.ipv4.tcp_fin_timeout = 10
net.ipv4.tcp_keepalive_time = 300
net.ipv4.tcp_keepalive_intvl = 30
net.ipv4.tcp_keepalive_probes = 3
vm.swappiness = 10
vm.dirty_ratio = 15
vm.dirty_background_ratio = 5
EOF

sysctl --system
echo "✅ System limits and performance tuning configured"

# Create motd with server info
cat > /etc/motd << 'EOF'
 __        ___ _       _     ____ _ _         ____
 \ \      / (_) |_ ___| |__ / ___(_) |_ _   _|  _ \ ___  _ __   ___
  \ \ /\ / /| | __/ __| '_ \| |   | | __| | | | |_) / _ \| '_ \ / _ \
   \ V  V / | | || (__| | | | |___| | |_| |_| |  _ < (_) | |_) |  __/
    \_/\_/  |_|\__\___|_| |_|\____|_|\__|\__, |_| \_\___/| .__/ \___|
                                        |___/            |_|

Welcome to the WitchCityRope Production Server
==============================================

System Information:
- Environment: Production/Staging Multi-Environment
- OS: Ubuntu 24.04 LTS
- Docker: Container-based deployment
- Database: Managed PostgreSQL
- Monitoring: System logs + health checks

Important Directories:
- Applications: /opt/witchcityrope/
- Logs: /var/log/witchcityrope/
- Backups: /opt/backups/witchcityrope/

Security Notice:
- This server is monitored and logged
- All access is logged and audited
- Only authorized personnel should access this system

EOF

# Display summary
echo ""
echo "✅ Initial droplet setup completed successfully!"
echo ""
echo "📋 Setup Summary:"
echo "   • System packages updated and essential tools installed"
echo "   • Deploy user '${DEPLOY_USER}' created with sudo privileges"
echo "   • SSH security hardened (password auth disabled, root login disabled)"
echo "   • UFW firewall configured with required ports"
echo "   • fail2ban configured for intrusion prevention"
echo "   • Automatic security updates enabled"
echo "   • Application directories created: /opt/witchcityrope/"
echo "   • Log rotation configured"
echo "   • System performance tuning applied"
echo ""
echo "🚨 IMPORTANT NEXT STEPS:"
echo "   1. Verify SSH key access works: ssh ${DEPLOY_USER}@$(curl -s ifconfig.me)"
echo "   2. Disable root SSH access test"
echo "   3. Run next script: 02-install-dependencies.sh"
echo ""
echo "🔐 Security Status:"
echo "   • Root login: DISABLED"
echo "   • Password authentication: DISABLED"
echo "   • Firewall: ENABLED"
echo "   • Intrusion prevention: ENABLED"
echo ""
echo "📅 Completed at: $(date)"
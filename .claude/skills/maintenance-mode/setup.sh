#!/bin/bash
# WitchCityRope Maintenance Mode - One-Time Server Setup
# SINGLE SOURCE OF TRUTH - DO NOT DUPLICATE
#
# This script deploys maintenance page files and updated nginx configs to the
# DigitalOcean droplet. Run this ONCE before using the maintenance-mode skill.
#
# What it does:
#   1. Creates /opt/witchcityrope/maintenance/ directory on server
#   2. Uploads maintenance.html, styles.css, 429.html
#   3. Backs up existing nginx configs
#   4. Uploads updated nginx configs with maintenance mode support
#   5. Tests and reloads nginx

set -e  # Exit on error

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
FILES_DIR="$SCRIPT_DIR/files"

# ============================================
# VAULT INTEGRATION
# ============================================

source "$SCRIPT_DIR/../_shared/vault-helpers.sh"

echo "WitchCityRope Maintenance Mode Setup"
echo "====================================="
echo ""
echo "This script will:"
echo "  1. Create maintenance page directory on the droplet"
echo "  2. Upload maintenance page files (HTML, CSS)"
echo "  3. Backup existing nginx configs"
echo "  4. Upload updated nginx configs with maintenance mode support"
echo "  5. Test and reload nginx"
echo ""
echo "This is a ONE-TIME setup. Only run once per droplet."
echo ""

# Quick bypass for non-interactive environments
if [ "$SKIP_CONFIRMATION" = "true" ]; then
    echo "Skipping confirmation (SKIP_CONFIRMATION=true)"
    echo ""
else
    read -p "Continue with setup? (y/N): " CONFIRM
    if [[ ! "$CONFIRM" =~ ^[Yy]$ ]]; then
        echo "Aborted."
        exit 0
    fi
    echo ""
fi

# ============================================
# VAULT - GET SSH CREDENTIALS
# ============================================

echo "Connecting to Vault..."
vault_init

SSH_KEY_FILENAME=$(vault_get_field "secret/shared/digitalocean" "SSH_KEY_FILENAME")
DROPLET_IP=$(vault_get_field "secret/shared/digitalocean" "DROPLET_IP")
DROPLET_USER=$(vault_get_field "secret/shared/digitalocean" "DROPLET_USER")

SSH_KEY="$HOME/.ssh/$SSH_KEY_FILENAME"
if [ ! -f "$SSH_KEY" ]; then
    echo "FAIL: SSH key not found: $SSH_KEY"
    exit 1
fi
echo "   SSH key: $SSH_KEY"
echo "   Server: $DROPLET_USER@$DROPLET_IP"
echo ""

MAINTENANCE_DIR="/opt/witchcityrope/maintenance"

# ============================================
# STEP 1: CREATE DIRECTORIES
# ============================================

echo "1. Creating directories on droplet..."

ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no -o ConnectTimeout=10 "$DROPLET_USER@$DROPLET_IP" << EOF
    mkdir -p $MAINTENANCE_DIR
    # Flag directories should already exist, but ensure they do
    mkdir -p /opt/witchcityrope/staging
    mkdir -p /opt/witchcityrope/production
    echo "   Directories ready"
EOF

echo ""

# ============================================
# STEP 2: UPLOAD MAINTENANCE PAGE FILES
# ============================================

echo "2. Uploading maintenance page files..."

# Verify local files exist
for file in maintenance.html styles.css 429.html; do
    if [ ! -f "$FILES_DIR/$file" ]; then
        echo "FAIL: $file not found in $FILES_DIR"
        exit 1
    fi
done

# Upload files
scp -i "$SSH_KEY" -o StrictHostKeyChecking=no \
    "$FILES_DIR/maintenance.html" \
    "$FILES_DIR/styles.css" \
    "$FILES_DIR/429.html" \
    "$DROPLET_USER@$DROPLET_IP:$MAINTENANCE_DIR/"

echo "   maintenance.html uploaded"
echo "   styles.css uploaded"
echo "   429.html uploaded"
echo ""

# ============================================
# STEP 3: BACKUP EXISTING NGINX CONFIGS
# ============================================

echo "3. Backing up existing nginx configs..."

ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << 'EOF'
    TIMESTAMP=$(date +%Y%m%d_%H%M%S)

    for config in notfai-staging witchcityrope-production notfai-production; do
        if [ -f "/etc/nginx/sites-available/$config" ]; then
            sudo cp "/etc/nginx/sites-available/$config" "/etc/nginx/sites-available/${config}.backup.${TIMESTAMP}"
            echo "   Backed up $config"
        else
            echo "   Skipped $config (not found)"
        fi
    done
EOF

echo ""

# ============================================
# STEP 4: UPLOAD NEW NGINX CONFIGS
# ============================================

echo "4. Uploading updated nginx configs..."

# Upload staging config
if [ -f "$FILES_DIR/nginx-notfai-staging.conf" ]; then
    scp -i "$SSH_KEY" -o StrictHostKeyChecking=no "$FILES_DIR/nginx-notfai-staging.conf" "$DROPLET_USER@$DROPLET_IP:/tmp/notfai-staging.conf"
    ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" "sudo cp /tmp/notfai-staging.conf /etc/nginx/sites-available/notfai-staging"
    echo "   Staging nginx config uploaded (notfai-staging)"
fi

# Upload production config
if [ -f "$FILES_DIR/nginx-witchcityrope-production.conf" ]; then
    scp -i "$SSH_KEY" -o StrictHostKeyChecking=no "$FILES_DIR/nginx-witchcityrope-production.conf" "$DROPLET_USER@$DROPLET_IP:/tmp/witchcityrope-production.conf"
    ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" "sudo cp /tmp/witchcityrope-production.conf /etc/nginx/sites-available/witchcityrope-production"
    echo "   Production nginx config uploaded (witchcityrope-production)"
fi

# Upload notfai production (backdoor) config
if [ -f "$FILES_DIR/nginx-notfai-production.conf" ]; then
    scp -i "$SSH_KEY" -o StrictHostKeyChecking=no "$FILES_DIR/nginx-notfai-production.conf" "$DROPLET_USER@$DROPLET_IP:/tmp/notfai-production.conf"
    ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" "sudo cp /tmp/notfai-production.conf /etc/nginx/sites-available/notfai-production"
    echo "   Backdoor nginx config uploaded (notfai-production)"
fi

echo ""

# ============================================
# STEP 5: TEST AND RELOAD NGINX
# ============================================

echo "5. Testing and reloading nginx..."

ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << 'EOF'
    echo "   Testing nginx configuration..."
    if sudo nginx -t 2>&1; then
        echo "   Nginx config test passed"
        sudo systemctl reload nginx
        echo "   Nginx reloaded"
    else
        echo "   Nginx config test FAILED"
        echo "   Restoring backups..."

        # Find most recent backups and restore
        for config in notfai-staging witchcityrope-production notfai-production; do
            BACKUP=$(ls -t /etc/nginx/sites-available/${config}.backup.* 2>/dev/null | head -1)
            if [ -n "$BACKUP" ]; then
                sudo cp "$BACKUP" "/etc/nginx/sites-available/$config"
                echo "   Restored $config from backup"
            fi
        done

        sudo nginx -t && sudo systemctl reload nginx
        echo "   Backups restored, nginx reloaded"
        exit 1
    fi
EOF

if [ $? -ne 0 ]; then
    echo ""
    echo "Setup failed - nginx config had errors. Backups were restored."
    exit 1
fi

echo ""

# ============================================
# STEP 6: VERIFY SETUP
# ============================================

echo "6. Verifying setup..."

ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << 'EOF'
    echo "   Checking directories..."
    [ -d "/opt/witchcityrope/maintenance" ] && echo "   /opt/witchcityrope/maintenance/ exists" || echo "   MISSING: /opt/witchcityrope/maintenance/"
    [ -d "/opt/witchcityrope/staging" ] && echo "   /opt/witchcityrope/staging/ exists" || echo "   MISSING: /opt/witchcityrope/staging/"
    [ -d "/opt/witchcityrope/production" ] && echo "   /opt/witchcityrope/production/ exists" || echo "   MISSING: /opt/witchcityrope/production/"

    echo "   Checking maintenance files..."
    [ -f "/opt/witchcityrope/maintenance/maintenance.html" ] && echo "   maintenance.html present" || echo "   MISSING: maintenance.html"
    [ -f "/opt/witchcityrope/maintenance/styles.css" ] && echo "   styles.css present" || echo "   MISSING: styles.css"
    [ -f "/opt/witchcityrope/maintenance/429.html" ] && echo "   429.html present" || echo "   MISSING: 429.html"

    echo "   Checking nginx configs..."
    [ -f "/etc/nginx/sites-available/notfai-staging" ] && echo "   notfai-staging config present" || echo "   MISSING: notfai-staging"
    [ -f "/etc/nginx/sites-available/witchcityrope-production" ] && echo "   witchcityrope-production config present" || echo "   MISSING: witchcityrope-production"
    [ -f "/etc/nginx/sites-available/notfai-production" ] && echo "   notfai-production config present" || echo "   MISSING: notfai-production"

    echo "   Checking maintenance mode status..."
    for env in staging production; do
        if [ -f "/opt/witchcityrope/$env/maintenance.flag" ]; then
            echo "   $env: MAINTENANCE MODE IS ENABLED (flag exists)"
        else
            echo "   $env: Normal operation (no flag)"
        fi
    done
EOF

echo ""
echo "Setup Complete!"
echo "==============="
echo ""
echo "Next steps:"
echo ""
echo "  1. Test maintenance mode on staging:"
echo "     bash .claude/skills/maintenance-mode/execute.sh --on --env staging"
echo "     # Check: https://staging.notfai.com (should show maintenance page)"
echo "     bash .claude/skills/maintenance-mode/execute.sh --off --env staging"
echo ""
echo "  2. Test maintenance mode on production:"
echo "     bash .claude/skills/maintenance-mode/execute.sh --on --env production"
echo "     # Check: https://prod.notfai.com (should show maintenance page)"
echo "     # Backdoor: https://notfai.com (should show real site)"
echo "     bash .claude/skills/maintenance-mode/execute.sh --off --env production"
echo ""
echo "  3. Check status anytime:"
echo "     bash .claude/skills/maintenance-mode/execute.sh --status"
echo ""

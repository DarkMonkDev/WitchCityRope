#!/bin/bash
# WitchCityRope Maintenance Mode - Toggle
# SINGLE SOURCE OF TRUTH - DO NOT DUPLICATE
#
# Enables/disables maintenance mode by toggling a flag file on the server.
# Nginx checks for this flag file and returns 503 (maintenance page) when present.
# When containers are down (502/504), the maintenance page shows automatically
# regardless of the flag file.

set -e  # Exit on error

# ============================================
# VAULT INTEGRATION
# ============================================

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
source "$SCRIPT_DIR/../_shared/vault-helpers.sh"

# ============================================
# PARSE ARGUMENTS
# ============================================

MODE=""
ENV="staging"  # Default to staging for safety

print_usage() {
    echo "Usage: $0 --on|--off|--status [--env staging|production]"
    echo ""
    echo "Options:"
    echo "  --on        Enable maintenance mode (show maintenance page)"
    echo "  --off       Disable maintenance mode (resume normal operation)"
    echo "  --status    Check current maintenance mode status for both environments"
    echo "  --env       Environment: staging (default) or production"
    echo ""
    echo "Examples:"
    echo "  $0 --on                    # Enable maintenance on staging"
    echo "  $0 --off --env staging     # Disable maintenance on staging"
    echo "  $0 --on --env production   # Enable maintenance on production"
    echo "  $0 --status                # Check status of both environments"
    echo ""
    echo "Backdoor URLs (exempt from maintenance mode):"
    echo "  Production: https://notfai.com (always works)"
    echo "  Staging:    No backdoor - both domains show maintenance"
}

while [[ $# -gt 0 ]]; do
    case $1 in
        --on)
            MODE="on"
            shift
            ;;
        --off)
            MODE="off"
            shift
            ;;
        --status)
            MODE="status"
            shift
            ;;
        --env)
            ENV="$2"
            shift 2
            ;;
        -h|--help)
            print_usage
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            print_usage
            exit 1
            ;;
    esac
done

if [ -z "$MODE" ]; then
    echo "Must specify --on, --off, or --status"
    echo ""
    print_usage
    exit 1
fi

if [[ "$ENV" != "staging" && "$ENV" != "production" ]]; then
    echo "Environment must be 'staging' or 'production'"
    print_usage
    exit 1
fi

FLAG_FILE="/opt/witchcityrope/$ENV/maintenance.flag"

# ============================================
# HEADER
# ============================================

echo "WitchCityRope Maintenance Mode"
echo "=============================="
echo ""
echo "   Mode: $MODE"
echo "   Environment: $ENV"
echo "   Flag file: $FLAG_FILE"
echo ""

# ============================================
# VAULT - GET SSH CREDENTIALS
# ============================================

echo "Connecting to Vault..."
vault_init

# Get SSH credentials from shared secrets
SSH_KEY_FILENAME=$(vault_get_field "secret/shared/digitalocean" "SSH_KEY_FILENAME")
DROPLET_IP=$(vault_get_field "secret/shared/digitalocean" "DROPLET_IP")
DROPLET_USER=$(vault_get_field "secret/shared/digitalocean" "DROPLET_USER")

SSH_KEY="$HOME/.ssh/$SSH_KEY_FILENAME"
if [ ! -f "$SSH_KEY" ]; then
    echo ""
    echo "FAIL: SSH key not found: $SSH_KEY"
    echo ""
    echo "=== SKILL_RESULT ==="
    echo "{\"skill\":\"maintenance-mode\",\"status\":\"failure\",\"error\":\"SSH key not found\"}"
    echo "=== END_SKILL_RESULT ==="
    exit 1
fi
echo "   SSH key: $SSH_KEY"
echo "   Server: $DROPLET_USER@$DROPLET_IP"
echo ""

# ============================================
# EXECUTE COMMAND
# ============================================

if [ "$MODE" = "status" ]; then
    echo "Checking maintenance mode status..."
    echo ""

    ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no -o ConnectTimeout=10 "$DROPLET_USER@$DROPLET_IP" << 'EOF'
echo "=== Maintenance Mode Status ==="
echo ""
for env in staging production; do
    FLAG="/opt/witchcityrope/${env}/maintenance.flag"
    if [ -f "$FLAG" ]; then
        echo "  $env: MAINTENANCE MODE ENABLED"
    else
        echo "  $env: Normal operation"
    fi
done
echo ""
echo "=== Nginx Status ==="
sudo systemctl status nginx --no-pager | head -3
echo ""
echo "=== Maintenance Files ==="
ls -la /opt/witchcityrope/maintenance/ 2>/dev/null || echo "  /opt/witchcityrope/maintenance/ does not exist - run setup.sh first"
EOF

    echo ""
    echo "=== SKILL_RESULT ==="
    echo "{\"skill\":\"maintenance-mode\",\"status\":\"success\",\"action\":\"status\"}"
    echo "=== END_SKILL_RESULT ==="
    exit 0
fi

if [ "$MODE" = "on" ]; then
    echo "Enabling maintenance mode on $ENV..."
    echo ""

    ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no -o ConnectTimeout=10 "$DROPLET_USER@$DROPLET_IP" << EOF
        # Ensure directory exists (should already from setup, but be safe)
        mkdir -p /opt/witchcityrope/$ENV

        # Create flag file to trigger maintenance mode
        touch $FLAG_FILE
        echo "   Created flag file: $FLAG_FILE"

        # Reload nginx to pick up the flag file check
        sudo systemctl reload nginx
        echo "   Nginx reloaded"

        # Verify
        if [ -f "$FLAG_FILE" ]; then
            echo "   Maintenance mode ENABLED"
        else
            echo "   Flag file not created"
            exit 1
        fi
EOF

    if [ $? -eq 0 ]; then
        echo ""
        echo "Maintenance mode ENABLED on $ENV"
        echo ""
        if [ "$ENV" = "production" ]; then
            echo "   Site is now showing maintenance page on prod.notfai.com / prod.witchcityrope.com"
            echo "   Backdoor (always works): https://notfai.com"
        else
            echo "   Site is now showing maintenance page on staging.notfai.com / staging.witchcityrope.com"
        fi
        echo ""
        echo "   To disable: $0 --off --env $ENV"
        echo ""
        echo "=== SKILL_RESULT ==="
        echo "{\"skill\":\"maintenance-mode\",\"status\":\"success\",\"action\":\"on\",\"environment\":\"$ENV\",\"maintenanceEnabled\":true}"
        echo "=== END_SKILL_RESULT ==="
    else
        echo ""
        echo "=== SKILL_RESULT ==="
        echo "{\"skill\":\"maintenance-mode\",\"status\":\"failure\",\"action\":\"on\",\"error\":\"SSH command failed\"}"
        echo "=== END_SKILL_RESULT ==="
        exit 1
    fi

elif [ "$MODE" = "off" ]; then
    echo "Disabling maintenance mode on $ENV..."
    echo ""

    ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no -o ConnectTimeout=10 "$DROPLET_USER@$DROPLET_IP" << EOF
        # Remove flag file to disable maintenance mode
        rm -f $FLAG_FILE
        echo "   Removed flag file: $FLAG_FILE"

        # Reload nginx
        sudo systemctl reload nginx
        echo "   Nginx reloaded"

        # Verify
        if [ ! -f "$FLAG_FILE" ]; then
            echo "   Maintenance mode DISABLED"
        else
            echo "   Flag file still exists"
            exit 1
        fi
EOF

    if [ $? -eq 0 ]; then
        echo ""
        echo "Maintenance mode DISABLED on $ENV"
        echo ""
        echo "   Site is now operational"
        echo ""
        echo "=== SKILL_RESULT ==="
        echo "{\"skill\":\"maintenance-mode\",\"status\":\"success\",\"action\":\"off\",\"environment\":\"$ENV\",\"maintenanceEnabled\":false}"
        echo "=== END_SKILL_RESULT ==="
    else
        echo ""
        echo "=== SKILL_RESULT ==="
        echo "{\"skill\":\"maintenance-mode\",\"status\":\"failure\",\"action\":\"off\",\"error\":\"SSH command failed\"}"
        echo "=== END_SKILL_RESULT ==="
        exit 1
    fi
fi

exit 0

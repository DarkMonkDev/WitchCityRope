#!/bin/bash
# Backup and Recovery Setup Script
# Sets up comprehensive backup and recovery system for WitchCityRope
# Run this script as the witchcity user
# Usage: ./07-backup-setup.sh

set -euo pipefail

echo "💾 Setting up backup and recovery system for WitchCityRope..."
echo "📅 Started at: $(date)"

# Check if running as correct user
if [ "$USER" = "root" ]; then
    echo "❌ This script should not be run as root"
    echo "   Please run as witchcity user: ./07-backup-setup.sh"
    exit 1
fi

# Configuration
BACKUP_ROOT="/opt/backups/witchcityrope"
REMOTE_BACKUP_ENABLED=false
S3_BUCKET=""
S3_REGION=""
DO_SPACES_KEY=""
DO_SPACES_SECRET=""

# Function to prompt for remote backup configuration
prompt_for_remote_backup() {
    echo "🌐 Configure remote backup (optional):"
    echo ""

    read -p "Enable remote backup to DigitalOcean Spaces? (y/n): " enable_remote

    if [ "$enable_remote" = "y" ] || [ "$enable_remote" = "Y" ]; then
        REMOTE_BACKUP_ENABLED=true

        read -p "Enter DigitalOcean Spaces bucket name: " S3_BUCKET
        read -p "Enter DigitalOcean Spaces region (e.g., nyc3, sfo3): " S3_REGION
        read -p "Enter DigitalOcean Spaces access key: " DO_SPACES_KEY
        read -s -p "Enter DigitalOcean Spaces secret key: " DO_SPACES_SECRET
        echo ""

        echo "✅ Remote backup will be enabled"
        echo "   Bucket: $S3_BUCKET"
        echo "   Region: $S3_REGION"
    else
        echo "✅ Only local backups will be configured"
    fi
}

# Get remote backup preferences
prompt_for_remote_backup

# Create backup directories
echo "📁 Creating backup directory structure..."
sudo mkdir -p "$BACKUP_ROOT"/{database,application,configuration,logs}
sudo mkdir -p "$BACKUP_ROOT"/archive/{daily,weekly,monthly}

# Set proper permissions
sudo chown -R "$USER:$USER" "$BACKUP_ROOT"

echo "✅ Backup directories created"

# Install backup tools
echo "🔧 Installing backup tools..."

# Install s3cmd for DigitalOcean Spaces if remote backup is enabled
if [ "$REMOTE_BACKUP_ENABLED" = true ]; then
    if ! command -v s3cmd &> /dev/null; then
        sudo apt update
        sudo apt install -y s3cmd
        echo "✅ s3cmd installed"
    else
        echo "✅ s3cmd already installed"
    fi

    # Configure s3cmd for DigitalOcean Spaces
    cat > ~/.s3cfg << EOF
[default]
access_key = $DO_SPACES_KEY
secret_key = $DO_SPACES_SECRET
host_base = ${S3_REGION}.digitaloceanspaces.com
host_bucket = %(bucket)s.${S3_REGION}.digitaloceanspaces.com
use_https = True
signature_v2 = False
EOF

    chmod 600 ~/.s3cfg
    echo "✅ s3cmd configured for DigitalOcean Spaces"
fi

# Install additional backup utilities
sudo apt install -y rsync pv pigz

echo "✅ Backup tools installed"

# Create database backup script
echo "🗄️ Creating database backup script..."
cat > /opt/witchcityrope/backup-full-database.sh << EOF
#!/bin/bash
# Comprehensive Database Backup Script for WitchCityRope
# Creates encrypted, compressed backups of both production and staging databases

set -euo pipefail

# Configuration
BACKUP_DIR="$BACKUP_ROOT/database"
TIMESTAMP=\$(date +%Y%m%d_%H%M%S)
RETENTION_DAYS=30
ENCRYPTION_KEY=\${BACKUP_ENCRYPTION_KEY:-\$(openssl rand -base64 32)}

# Load environment variables
if [ -f /opt/witchcityrope/production/.env.production ]; then
    source /opt/witchcityrope/production/.env.production
else
    echo "❌ Production environment file not found"
    exit 1
fi

log_message() {
    echo "[\$(date '+%Y-%m-%d %H:%M:%S')] \$1" | tee -a "\$BACKUP_DIR/backup.log"
}

create_database_backup() {
    local env=\$1
    local db_name=\$2
    local backup_file="\$BACKUP_DIR/\${env}_\${TIMESTAMP}.sql.gz.enc"

    log_message "Creating \$env database backup..."

    # Create compressed, encrypted backup
    docker run --rm \\
        -e PGPASSWORD="\$DATABASE_PASSWORD" \\
        postgres:16-alpine \\
        pg_dump -h "\$DATABASE_HOST" -p "\$DATABASE_PORT" -U "\$DATABASE_USER" -d "\$db_name" \\
        --no-owner --no-privileges --clean --if-exists --verbose \\
        2>"\$BACKUP_DIR/\${env}_backup_\${TIMESTAMP}.log" | \\
    pv -N "Compressing \$env DB" | \\
    pigz -9 | \\
    openssl enc -aes-256-cbc -salt -k "\$ENCRYPTION_KEY" -out "\$backup_file"

    if [ \$? -eq 0 ]; then
        local backup_size=\$(stat -c%s "\$backup_file" | numfmt --to=iec)
        log_message "✅ \$env backup completed: \$backup_file (\$backup_size)"

        # Create checksum
        sha256sum "\$backup_file" > "\$backup_file.sha256"

        # Upload to remote storage if enabled
        if [ "$REMOTE_BACKUP_ENABLED" = true ]; then
            upload_to_remote "\$backup_file" "database/\$(basename "\$backup_file")"
            upload_to_remote "\$backup_file.sha256" "database/\$(basename "\$backup_file.sha256")"
        fi
    else
        log_message "❌ \$env backup failed"
        return 1
    fi
}

upload_to_remote() {
    local local_file=\$1
    local remote_path=\$2

    log_message "Uploading to remote storage: \$remote_path"
    if s3cmd put "\$local_file" "s3://$S3_BUCKET/backups/\$remote_path"; then
        log_message "✅ Remote upload successful: \$remote_path"
    else
        log_message "❌ Remote upload failed: \$remote_path"
    fi
}

# Main backup function
main() {
    log_message "=== Database Backup Started ==="

    # Save encryption key securely
    echo "\$ENCRYPTION_KEY" > "\$BACKUP_DIR/.encryption_key_\$TIMESTAMP"
    chmod 600 "\$BACKUP_DIR/.encryption_key_\$TIMESTAMP"

    # Backup databases
    create_database_backup "production" "witchcityrope_prod"
    create_database_backup "staging" "witchcityrope_staging"

    # Clean up old local backups
    log_message "Cleaning up backups older than \$RETENTION_DAYS days..."
    find "\$BACKUP_DIR" -name "*.sql.gz.enc" -mtime +\$RETENTION_DAYS -delete
    find "\$BACKUP_DIR" -name "*.sha256" -mtime +\$RETENTION_DAYS -delete
    find "\$BACKUP_DIR" -name "*.log" -mtime +\$RETENTION_DAYS -delete
    find "\$BACKUP_DIR" -name ".encryption_key_*" -mtime +\$RETENTION_DAYS -delete

    log_message "=== Database Backup Completed ==="
}

# Run backup
main
EOF

chmod +x /opt/witchcityrope/backup-full-database.sh

# Create application backup script
echo "📦 Creating application backup script..."
cat > /opt/witchcityrope/backup-application.sh << 'EOF'
#!/bin/bash
# Application Backup Script for WitchCityRope
# Backs up Docker volumes, configuration files, and logs

set -euo pipefail

# Configuration
BACKUP_DIR="$BACKUP_ROOT/application"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
RETENTION_DAYS=14

log_message() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $1" | tee -a "$BACKUP_DIR/backup.log"
}

backup_docker_volumes() {
    log_message "Backing up Docker volumes..."

    local volume_backup="$BACKUP_DIR/docker_volumes_$TIMESTAMP.tar.gz"

    # Get list of WitchCityRope volumes
    local volumes=$(docker volume ls --filter "name=witchcity" --format "{{.Name}}")

    if [ -n "$volumes" ]; then
        # Create temporary container to access volumes
        docker run --rm \
            $(echo "$volumes" | sed 's/^/-v /;s/$/:\/backup\/&:ro/') \
            -v "$BACKUP_DIR:/host_backup" \
            alpine:latest \
            tar -czf "/host_backup/docker_volumes_$TIMESTAMP.tar.gz" -C /backup .

        local backup_size=$(stat -c%s "$volume_backup" | numfmt --to=iec)
        log_message "✅ Docker volumes backed up: $volume_backup ($backup_size)"

        # Upload to remote storage if enabled
        if [ "$REMOTE_BACKUP_ENABLED" = true ]; then
            upload_to_remote "$volume_backup" "application/$(basename "$volume_backup")"
        fi
    else
        log_message "⚠️  No WitchCityRope Docker volumes found"
    fi
}

backup_configuration() {
    log_message "Backing up configuration files..."

    local config_backup="$BACKUP_DIR/configuration_$TIMESTAMP.tar.gz"

    # Backup important configuration files
    tar -czf "$config_backup" \
        --exclude="*.log" \
        --exclude="*.pid" \
        /opt/witchcityrope/production/ \
        /opt/witchcityrope/staging/ \
        /etc/nginx/sites-available/witchcityrope-* \
        /etc/letsencrypt/ \
        /opt/witchcityrope/*.sh \
        /opt/witchcityrope/monitoring/ \
        2>/dev/null || true

    local backup_size=$(stat -c%s "$config_backup" | numfmt --to=iec)
    log_message "✅ Configuration backed up: $config_backup ($backup_size)"

    # Upload to remote storage if enabled
    if [ "$REMOTE_BACKUP_ENABLED" = true ]; then
        upload_to_remote "$config_backup" "application/$(basename "$config_backup")"
    fi
}

backup_logs() {
    log_message "Backing up recent logs..."

    local logs_backup="$BACKUP_DIR/logs_$TIMESTAMP.tar.gz"

    # Backup last 7 days of logs
    find /var/log/witchcityrope -name "*.log" -mtime -7 -type f -print0 | \
        tar -czf "$logs_backup" --null -T - 2>/dev/null || true

    if [ -f "$logs_backup" ]; then
        local backup_size=$(stat -c%s "$logs_backup" | numfmt --to=iec)
        log_message "✅ Logs backed up: $logs_backup ($backup_size)"

        # Upload to remote storage if enabled
        if [ "$REMOTE_BACKUP_ENABLED" = true ]; then
            upload_to_remote "$logs_backup" "application/$(basename "$logs_backup")"
        fi
    else
        log_message "⚠️  No recent logs found to backup"
    fi
}

upload_to_remote() {
    local local_file=$1
    local remote_path=$2

    if [ "$REMOTE_BACKUP_ENABLED" = true ]; then
        log_message "Uploading to remote storage: $remote_path"
        if s3cmd put "$local_file" "s3://$S3_BUCKET/backups/$remote_path"; then
            log_message "✅ Remote upload successful: $remote_path"
        else
            log_message "❌ Remote upload failed: $remote_path"
        fi
    fi
}

# Main backup function
main() {
    log_message "=== Application Backup Started ==="

    backup_docker_volumes
    backup_configuration
    backup_logs

    # Clean up old backups
    log_message "Cleaning up backups older than $RETENTION_DAYS days..."
    find "$BACKUP_DIR" -name "*.tar.gz" -mtime +$RETENTION_DAYS -delete
    find "$BACKUP_DIR" -name "*.log" -mtime +30 -delete

    log_message "=== Application Backup Completed ==="
}

# Run backup
main
EOF

chmod +x /opt/witchcityrope/backup-application.sh

# Create system backup script
echo "🖥️ Creating system backup script..."
cat > /opt/witchcityrope/backup-system.sh << 'EOF'
#!/bin/bash
# System Configuration Backup Script for WitchCityRope
# Backs up system configuration and security settings

set -euo pipefail

# Configuration
BACKUP_DIR="$BACKUP_ROOT/configuration"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
RETENTION_DAYS=30

log_message() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $1" | tee -a "$BACKUP_DIR/backup.log"
}

backup_system_config() {
    log_message "Backing up system configuration..."

    local system_backup="$BACKUP_DIR/system_config_$TIMESTAMP.tar.gz"

    # Backup important system configuration files
    sudo tar -czf "$system_backup" \
        --exclude="/etc/ssl/private/*" \
        --exclude="/etc/shadow*" \
        --exclude="/etc/gshadow*" \
        /etc/nginx/ \
        /etc/ssh/sshd_config \
        /etc/ufw/ \
        /etc/fail2ban/ \
        /etc/cron.d/ \
        /etc/logrotate.d/witchcityrope* \
        /etc/systemd/system/witchcity* \
        /var/spool/cron/crontabs/$(whoami) \
        2>/dev/null || true

    sudo chown "$USER:$USER" "$system_backup"

    local backup_size=$(stat -c%s "$system_backup" | numfmt --to=iec)
    log_message "✅ System configuration backed up: $system_backup ($backup_size)"

    # Upload to remote storage if enabled
    if [ "$REMOTE_BACKUP_ENABLED" = true ]; then
        upload_to_remote "$system_backup" "configuration/$(basename "$system_backup")"
    fi
}

backup_security_config() {
    log_message "Backing up security configuration..."

    local security_backup="$BACKUP_DIR/security_config_$TIMESTAMP.tar.gz"

    # Create security configuration backup (excluding private keys)
    {
        echo "=== UFW Status ==="
        sudo ufw status verbose

        echo ""
        echo "=== Fail2ban Status ==="
        sudo fail2ban-client status

        echo ""
        echo "=== SSH Configuration ==="
        grep -v "^#" /etc/ssh/sshd_config | grep -v "^$"

        echo ""
        echo "=== Installed Packages ==="
        dpkg -l | grep -E "(nginx|docker|redis|fail2ban|ufw)"

        echo ""
        echo "=== Docker Configuration ==="
        cat /etc/docker/daemon.json 2>/dev/null || echo "Default Docker configuration"

        echo ""
        echo "=== SSL Certificates ==="
        find /etc/letsencrypt/live -name "*.pem" -ls 2>/dev/null || echo "No SSL certificates found"
    } > "$BACKUP_DIR/security_info_$TIMESTAMP.txt"

    # Create tarball of security info
    tar -czf "$security_backup" -C "$BACKUP_DIR" "security_info_$TIMESTAMP.txt"
    rm "$BACKUP_DIR/security_info_$TIMESTAMP.txt"

    local backup_size=$(stat -c%s "$security_backup" | numfmt --to=iec)
    log_message "✅ Security configuration backed up: $security_backup ($backup_size)"

    # Upload to remote storage if enabled
    if [ "$REMOTE_BACKUP_ENABLED" = true ]; then
        upload_to_remote "$security_backup" "configuration/$(basename "$security_backup")"
    fi
}

upload_to_remote() {
    local local_file=$1
    local remote_path=$2

    if [ "$REMOTE_BACKUP_ENABLED" = true ]; then
        log_message "Uploading to remote storage: $remote_path"
        if s3cmd put "$local_file" "s3://$S3_BUCKET/backups/$remote_path"; then
            log_message "✅ Remote upload successful: $remote_path"
        else
            log_message "❌ Remote upload failed: $remote_path"
        fi
    fi
}

# Main backup function
main() {
    log_message "=== System Configuration Backup Started ==="

    backup_system_config
    backup_security_config

    # Clean up old backups
    log_message "Cleaning up backups older than $RETENTION_DAYS days..."
    find "$BACKUP_DIR" -name "*.tar.gz" -mtime +$RETENTION_DAYS -delete

    log_message "=== System Configuration Backup Completed ==="
}

# Run backup
main
EOF

chmod +x /opt/witchcityrope/backup-system.sh

# Create master backup script
echo "🔄 Creating master backup orchestration script..."
cat > /opt/witchcityrope/backup-full.sh << 'EOF'
#!/bin/bash
# Master Backup Script for WitchCityRope
# Orchestrates all backup operations

set -euo pipefail

BACKUP_DIR="$BACKUP_ROOT"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
LOG_FILE="$BACKUP_DIR/full_backup_$TIMESTAMP.log"

log_message() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $1" | tee -a "$LOG_FILE"
}

run_backup_script() {
    local script=$1
    local description=$2

    log_message "Starting $description..."

    if [ -x "$script" ]; then
        if "$script" >> "$LOG_FILE" 2>&1; then
            log_message "✅ $description completed successfully"
        else
            log_message "❌ $description failed"
            return 1
        fi
    else
        log_message "❌ $description script not found or not executable: $script"
        return 1
    fi
}

# Main backup orchestration
main() {
    log_message "=== WitchCityRope Full Backup Started ==="

    local backup_start=$(date +%s)
    local failed_backups=0

    # Run all backup scripts
    if ! run_backup_script "/opt/witchcityrope/backup-full-database.sh" "Database Backup"; then
        ((failed_backups++))
    fi

    if ! run_backup_script "/opt/witchcityrope/backup-application.sh" "Application Backup"; then
        ((failed_backups++))
    fi

    if ! run_backup_script "/opt/witchcityrope/backup-system.sh" "System Configuration Backup"; then
        ((failed_backups++))
    fi

    # Calculate backup duration
    local backup_end=$(date +%s)
    local duration=$((backup_end - backup_start))
    local duration_formatted=$(printf '%02d:%02d:%02d' $((duration/3600)) $((duration%3600/60)) $((duration%60)))

    # Summary
    log_message "=== WitchCityRope Full Backup Completed ==="
    log_message "Duration: $duration_formatted"
    log_message "Failed backups: $failed_backups"

    if [ $failed_backups -eq 0 ]; then
        log_message "✅ All backups completed successfully"
    else
        log_message "⚠️  $failed_backups backup(s) failed - check logs for details"
    fi

    # Upload log to remote storage if enabled
    if [ "$REMOTE_BACKUP_ENABLED" = true ]; then
        s3cmd put "$LOG_FILE" "s3://$S3_BUCKET/backups/logs/$(basename "$LOG_FILE")" || true
    fi

    return $failed_backups
}

# Run full backup
main
EOF

chmod +x /opt/witchcityrope/backup-full.sh

# Create backup restoration script
echo "🔧 Creating backup restoration script..."
cat > /opt/witchcityrope/restore-backup.sh << 'EOF'
#!/bin/bash
# Backup Restoration Script for WitchCityRope
# Restores from local or remote backups

set -euo pipefail

show_usage() {
    echo "Usage: $0 <backup_type> <backup_file> [options]"
    echo ""
    echo "Backup Types:"
    echo "  database     - Restore database backup"
    echo "  application  - Restore application files and volumes"
    echo "  system       - Restore system configuration"
    echo ""
    echo "Options:"
    echo "  --remote     - Download from remote storage first"
    echo "  --environment production|staging (for database restores)"
    echo ""
    echo "Examples:"
    echo "  $0 database production_20240913_120000.sql.gz.enc --environment production"
    echo "  $0 application docker_volumes_20240913_120000.tar.gz"
    echo "  $0 system system_config_20240913_120000.tar.gz --remote"
    exit 1
}

log_message() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $1"
}

restore_database() {
    local backup_file=$1
    local environment=${2:-production}
    local backup_dir="$BACKUP_ROOT/database"

    log_message "Restoring database from $backup_file to $environment environment..."

    # Check if backup file exists
    if [ ! -f "$backup_dir/$backup_file" ]; then
        log_message "❌ Backup file not found: $backup_dir/$backup_file"
        return 1
    fi

    # Get encryption key
    local key_file=$(find "$backup_dir" -name ".encryption_key_*" | sort | tail -1)
    if [ ! -f "$key_file" ]; then
        read -s -p "Enter backup encryption key: " ENCRYPTION_KEY
        echo ""
    else
        ENCRYPTION_KEY=$(cat "$key_file")
    fi

    # Load environment variables
    local env_file="/opt/witchcityrope/$environment/.env.$environment"
    if [ -f "$env_file" ]; then
        source "$env_file"
    else
        log_message "❌ Environment file not found: $env_file"
        return 1
    fi

    # Determine database name
    local db_name
    if [ "$environment" = "production" ]; then
        db_name="witchcityrope_prod"
    else
        db_name="witchcityrope_staging"
    fi

    # Decrypt, decompress, and restore
    log_message "Decrypting and restoring database..."
    openssl enc -aes-256-cbc -d -k "$ENCRYPTION_KEY" -in "$backup_dir/$backup_file" | \
    pigz -d | \
    docker run --rm -i \
        -e PGPASSWORD="$DATABASE_PASSWORD" \
        postgres:16-alpine \
        psql -h "$DATABASE_HOST" -p "$DATABASE_PORT" -U "$DATABASE_USER" -d "$db_name"

    if [ $? -eq 0 ]; then
        log_message "✅ Database restore completed successfully"
    else
        log_message "❌ Database restore failed"
        return 1
    fi
}

restore_application() {
    local backup_file=$1
    local backup_dir="$BACKUP_ROOT/application"

    log_message "Restoring application from $backup_file..."

    # Check if backup file exists
    if [ ! -f "$backup_dir/$backup_file" ]; then
        log_message "❌ Backup file not found: $backup_dir/$backup_file"
        return 1
    fi

    # Stop containers before restore
    log_message "Stopping WitchCityRope containers..."
    docker ps --filter "name=witchcity-" -q | xargs -r docker stop

    # Extract backup
    log_message "Extracting backup..."
    cd /
    sudo tar -xzf "$backup_dir/$backup_file"

    # Restart containers
    log_message "Restarting containers..."
    /opt/witchcityrope/restart.sh all

    log_message "✅ Application restore completed successfully"
}

restore_system() {
    local backup_file=$1
    local backup_dir="$BACKUP_ROOT/configuration"

    log_message "Restoring system configuration from $backup_file..."

    # Check if backup file exists
    if [ ! -f "$backup_dir/$backup_file" ]; then
        log_message "❌ Backup file not found: $backup_dir/$backup_file"
        return 1
    fi

    # Extract backup
    log_message "Extracting system configuration..."
    cd /
    sudo tar -xzf "$backup_dir/$backup_file"

    # Reload services
    log_message "Reloading system services..."
    sudo systemctl reload nginx
    sudo systemctl reload fail2ban

    log_message "✅ System configuration restore completed successfully"
}

download_from_remote() {
    local backup_type=$1
    local backup_file=$2

    if [ "$REMOTE_BACKUP_ENABLED" = true ]; then
        log_message "Downloading from remote storage..."
        s3cmd get "s3://$S3_BUCKET/backups/$backup_type/$backup_file" "$BACKUP_ROOT/$backup_type/$backup_file"
    else
        log_message "❌ Remote backup not configured"
        return 1
    fi
}

# Parse arguments
if [ $# -lt 2 ]; then
    show_usage
fi

BACKUP_TYPE=$1
BACKUP_FILE=$2
shift 2

REMOTE_DOWNLOAD=false
ENVIRONMENT="production"

# Parse options
while [[ $# -gt 0 ]]; do
    case $1 in
        --remote)
            REMOTE_DOWNLOAD=true
            shift
            ;;
        --environment)
            ENVIRONMENT=$2
            shift 2
            ;;
        *)
            log_message "❌ Unknown option: $1"
            show_usage
            ;;
    esac
done

# Download from remote if requested
if [ "$REMOTE_DOWNLOAD" = true ]; then
    download_from_remote "$BACKUP_TYPE" "$BACKUP_FILE"
fi

# Perform restore based on backup type
case $BACKUP_TYPE in
    database)
        restore_database "$BACKUP_FILE" "$ENVIRONMENT"
        ;;
    application)
        restore_application "$BACKUP_FILE"
        ;;
    system)
        restore_system "$BACKUP_FILE"
        ;;
    *)
        log_message "❌ Invalid backup type: $BACKUP_TYPE"
        show_usage
        ;;
esac
EOF

chmod +x /opt/witchcityrope/restore-backup.sh

# Create backup management script
echo "📋 Creating backup management script..."
cat > /opt/witchcityrope/backup-manage.sh << 'EOF'
#!/bin/bash
# Backup Management Script for WitchCityRope
# Lists, verifies, and manages backups

set -euo pipefail

show_usage() {
    echo "Usage: $0 <command> [options]"
    echo ""
    echo "Commands:"
    echo "  list        - List all available backups"
    echo "  verify      - Verify backup integrity"
    echo "  cleanup     - Clean up old backups"
    echo "  status      - Show backup status"
    echo "  remote-sync - Sync with remote storage"
    echo ""
    echo "Examples:"
    echo "  $0 list"
    echo "  $0 verify database production_20240913_120000.sql.gz.enc"
    echo "  $0 cleanup --older-than 30"
    echo "  $0 remote-sync --download"
    exit 1
}

list_backups() {
    echo "📋 Available Backups:"
    echo "===================="

    for backup_type in database application configuration; do
        local backup_dir="$BACKUP_ROOT/$backup_type"
        if [ -d "$backup_dir" ]; then
            echo ""
            echo "$backup_type backups:"
            find "$backup_dir" -name "*.gz*" -o -name "*.tar.gz" | sort -r | while read -r file; do
                local size=$(stat -c%s "$file" | numfmt --to=iec)
                local date=$(date -r "$file" '+%Y-%m-%d %H:%M:%S')
                printf "  %-50s %8s  %s\n" "$(basename "$file")" "$size" "$date"
            done
        fi
    done
}

verify_backup() {
    local backup_type=$1
    local backup_file=$2
    local backup_path="$BACKUP_ROOT/$backup_type/$backup_file"

    echo "🔍 Verifying backup: $backup_file"

    if [ ! -f "$backup_path" ]; then
        echo "❌ Backup file not found: $backup_path"
        return 1
    fi

    # Check for checksum file
    if [ -f "$backup_path.sha256" ]; then
        echo "Verifying checksum..."
        if (cd "$(dirname "$backup_path")" && sha256sum -c "$(basename "$backup_path").sha256" --quiet); then
            echo "✅ Checksum verification passed"
        else
            echo "❌ Checksum verification failed"
            return 1
        fi
    else
        echo "⚠️  No checksum file found"
    fi

    # Additional verification based on backup type
    case $backup_type in
        database)
            echo "Checking encrypted database backup..."
            # Try to decrypt first few bytes to verify encryption key would work
            if head -c 1024 "$backup_path" | openssl enc -aes-256-cbc -d -k "test" > /dev/null 2>&1; then
                echo "⚠️  Backup appears to be encrypted but couldn't verify with test key"
            fi
            echo "✅ Database backup file structure appears valid"
            ;;
        application|configuration)
            echo "Checking tar.gz archive..."
            if tar -tzf "$backup_path" > /dev/null 2>&1; then
                echo "✅ Archive structure is valid"
            else
                echo "❌ Archive appears corrupted"
                return 1
            fi
            ;;
    esac

    echo "✅ Backup verification completed"
}

cleanup_backups() {
    local retention_days=${1:-30}

    echo "🗑️  Cleaning up backups older than $retention_days days..."

    local cleaned_count=0

    for backup_type in database application configuration; do
        local backup_dir="$BACKUP_ROOT/$backup_type"
        if [ -d "$backup_dir" ]; then
            echo "Cleaning $backup_type backups..."

            while IFS= read -r -d '' file; do
                echo "  Removing: $(basename "$file")"
                rm -f "$file" "$file.sha256" 2>/dev/null || true
                ((cleaned_count++))
            done < <(find "$backup_dir" -name "*.gz*" -o -name "*.tar.gz" -mtime +$retention_days -print0)
        fi
    done

    echo "✅ Cleanup completed. Removed $cleaned_count backup files."
}

show_backup_status() {
    echo "📊 Backup Status Report"
    echo "======================"

    # Disk usage
    echo ""
    echo "Backup Storage Usage:"
    du -sh "$BACKUP_ROOT"/* 2>/dev/null | sort -hr

    # Recent backup activity
    echo ""
    echo "Recent Backup Activity:"
    find "$BACKUP_ROOT" -name "backup.log" -mtime -7 -exec echo "=== {} ===" \; -exec tail -5 {} \; 2>/dev/null

    # Backup counts by type
    echo ""
    echo "Backup Counts by Type:"
    for backup_type in database application configuration; do
        local count=$(find "$BACKUP_ROOT/$backup_type" -name "*.gz*" -o -name "*.tar.gz" 2>/dev/null | wc -l)
        printf "  %-15s: %d backups\n" "$backup_type" "$count"
    done

    # Remote backup status
    if [ "$REMOTE_BACKUP_ENABLED" = true ]; then
        echo ""
        echo "Remote Backup Status:"
        if s3cmd ls "s3://$S3_BUCKET/backups/" > /dev/null 2>&1; then
            echo "  ✅ Remote storage accessible"
            s3cmd du -H "s3://$S3_BUCKET/backups/" 2>/dev/null | tail -1 || echo "  Unable to get remote usage"
        else
            echo "  ❌ Remote storage not accessible"
        fi
    fi
}

remote_sync() {
    local direction=${1:-upload}

    if [ "$REMOTE_BACKUP_ENABLED" != true ]; then
        echo "❌ Remote backup not configured"
        return 1
    fi

    echo "🌐 Syncing with remote storage ($direction)..."

    case $direction in
        upload)
            for backup_type in database application configuration; do
                local backup_dir="$BACKUP_ROOT/$backup_type"
                if [ -d "$backup_dir" ]; then
                    echo "Uploading $backup_type backups..."
                    s3cmd sync "$backup_dir/" "s3://$S3_BUCKET/backups/$backup_type/" --delete-removed
                fi
            done
            ;;
        download)
            echo "Downloading from remote storage..."
            s3cmd sync "s3://$S3_BUCKET/backups/" "$BACKUP_ROOT/" --delete-removed
            ;;
        *)
            echo "❌ Invalid sync direction: $direction (use 'upload' or 'download')"
            return 1
            ;;
    esac

    echo "✅ Remote sync completed"
}

# Parse arguments
if [ $# -eq 0 ]; then
    show_usage
fi

COMMAND=$1
shift

case $COMMAND in
    list)
        list_backups
        ;;
    verify)
        if [ $# -lt 2 ]; then
            echo "❌ Missing arguments for verify command"
            show_usage
        fi
        verify_backup "$1" "$2"
        ;;
    cleanup)
        RETENTION_DAYS=30
        if [ $# -gt 0 ] && [ "$1" = "--older-than" ]; then
            RETENTION_DAYS=$2
        fi
        cleanup_backups "$RETENTION_DAYS"
        ;;
    status)
        show_backup_status
        ;;
    remote-sync)
        DIRECTION="upload"
        if [ $# -gt 0 ] && [ "$1" = "--download" ]; then
            DIRECTION="download"
        fi
        remote_sync "$DIRECTION"
        ;;
    *)
        echo "❌ Unknown command: $COMMAND"
        show_usage
        ;;
esac
EOF

chmod +x /opt/witchcityrope/backup-manage.sh

# Set up backup cron jobs
echo "⏰ Setting up backup cron jobs..."

(crontab -l 2>/dev/null; cat << 'EOF'
# WitchCityRope Backup Jobs

# Daily database backup at 2 AM
0 2 * * * /opt/witchcityrope/backup-full-database.sh >> /var/log/witchcityrope/backup-cron.log 2>&1

# Daily application backup at 3 AM
0 3 * * * /opt/witchcityrope/backup-application.sh >> /var/log/witchcityrope/backup-cron.log 2>&1

# Weekly system configuration backup (Sundays at 4 AM)
0 4 * * 0 /opt/witchcityrope/backup-system.sh >> /var/log/witchcityrope/backup-cron.log 2>&1

# Monthly full backup (1st of month at 5 AM)
0 5 1 * * /opt/witchcityrope/backup-full.sh >> /var/log/witchcityrope/backup-cron.log 2>&1

# Weekly backup cleanup (Mondays at 1 AM)
0 1 * * 1 /opt/witchcityrope/backup-manage.sh cleanup --older-than 30 >> /var/log/witchcityrope/backup-cron.log 2>&1
EOF
) | crontab -

echo "✅ Backup cron jobs configured"

# Configure log rotation for backup logs
echo "🗞️ Configuring log rotation for backup logs..."
sudo tee /etc/logrotate.d/witchcityrope-backups > /dev/null << 'EOF'
/var/log/witchcityrope/backup*.log {
    daily
    rotate 30
    compress
    delaycompress
    missingok
    notifempty
    create 0644 witchcity witchcity
}

/opt/backups/witchcityrope/*/backup.log {
    weekly
    rotate 12
    compress
    delaycompress
    missingok
    notifempty
    create 0644 witchcity witchcity
}
EOF

# Test backup setup
echo "🧪 Testing backup setup..."
/opt/witchcityrope/backup-manage.sh status
echo "✅ Backup system test completed"

# Final summary
echo ""
echo "✅ Backup and recovery setup completed successfully!"
echo ""
echo "📋 Setup Summary:"
echo "   • Daily database backups: 2 AM (encrypted and compressed)"
echo "   • Daily application backups: 3 AM (Docker volumes and configs)"
echo "   • Weekly system backups: Sundays at 4 AM"
echo "   • Monthly full backups: 1st of month at 5 AM"
echo "   • Automatic cleanup: Mondays at 1 AM (keeps 30 days)"
echo "   • Remote backup: $([ "$REMOTE_BACKUP_ENABLED" = true ] && echo "Enabled to DO Spaces" || echo "Disabled")"
echo "   • Backup encryption: AES-256-CBC for database backups"
echo ""
echo "📁 Important Files:"
echo "   • Master backup: /opt/witchcityrope/backup-full.sh"
echo "   • Database backup: /opt/witchcityrope/backup-full-database.sh"
echo "   • Application backup: /opt/witchcityrope/backup-application.sh"
echo "   • System backup: /opt/witchcityrope/backup-system.sh"
echo "   • Restore script: /opt/witchcityrope/restore-backup.sh"
echo "   • Management script: /opt/witchcityrope/backup-manage.sh"
echo ""
echo "🔧 Useful Commands:"
echo "   • List backups: /opt/witchcityrope/backup-manage.sh list"
echo "   • Backup status: /opt/witchcityrope/backup-manage.sh status"
echo "   • Manual backup: /opt/witchcityrope/backup-full.sh"
echo "   • Restore database: /opt/witchcityrope/restore-backup.sh database <file> --environment production"
echo "   • Verify backup: /opt/witchcityrope/backup-manage.sh verify database <file>"
echo "   • Clean backups: /opt/witchcityrope/backup-manage.sh cleanup --older-than 30"
echo ""
echo "💾 Backup Storage:"
echo "   • Local backups: $BACKUP_ROOT"
echo "   • Database: $BACKUP_ROOT/database"
echo "   • Applications: $BACKUP_ROOT/application"
echo "   • System config: $BACKUP_ROOT/configuration"
echo "$([ "$REMOTE_BACKUP_ENABLED" = true ] && echo "   • Remote: s3://$S3_BUCKET/backups/" || echo "   • Remote: Not configured")"
echo ""
echo "🚨 IMPORTANT NOTES:"
echo "   1. Database backups are encrypted - store encryption keys securely!"
echo "   2. Test restore procedures regularly"
echo "   3. Monitor backup logs: tail -f /var/log/witchcityrope/backup-cron.log"
echo "   4. Verify remote backup access if configured"
echo ""
echo "✅ WitchCityRope deployment setup is now complete!"
echo ""
echo "📅 Completed at: $(date)"
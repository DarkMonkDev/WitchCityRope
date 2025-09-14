# Backup Procedures and Disaster Recovery

## Overview

This document outlines comprehensive backup strategies and disaster recovery procedures for WitchCityRope to ensure business continuity and data protection.

## Backup Strategy Overview

### 3-2-1 Rule
- **3** copies of important data
- **2** different storage media types
- **1** offsite backup location

### Recovery Objectives
- **RPO (Recovery Point Objective)**: Maximum 4 hours of data loss
- **RTO (Recovery Time Objective)**: Maximum 2 hours downtime

## What to Backup

### Critical Data
1. **PostgreSQL Database**
   - User accounts and profiles
   - Event data
   - Content and posts
   - Transaction records
   - System configuration

2. **User-Generated Content**
   - Profile images
   - Event photos
   - Document uploads
   - Media files

3. **Application Configuration**
   - Environment files (.env)
   - SSL certificates
   - Nginx configurations
   - Docker configurations

4. **Application Code**
   - Custom modifications
   - Deployment scripts
   - Database migrations

## Backup Implementation

### Database Backup Script

```bash
#!/bin/bash
# /opt/witchcityrope/scripts/backup-database.sh

set -e

# Configuration
BACKUP_DIR="/backups/postgres"
S3_BUCKET="witchcityrope-backups"
RETENTION_DAYS=30
DB_CONTAINER="witchcityrope-db"
DB_NAME="WitchCityRope"
DB_USER="witchcity"

# Create backup directory
mkdir -p $BACKUP_DIR

# Generate timestamp
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="$BACKUP_DIR/db_backup_$TIMESTAMP.sql"

echo "Starting database backup at $(date)"

# Perform backup
docker exec -t $DB_CONTAINER pg_dump -U $DB_USER -d $DB_NAME > $BACKUP_FILE

# Compress backup
gzip -9 $BACKUP_FILE
BACKUP_FILE="${BACKUP_FILE}.gz"

# Calculate checksum
CHECKSUM=$(sha256sum $BACKUP_FILE | awk '{print $1}')
echo $CHECKSUM > "${BACKUP_FILE}.sha256"

# Upload to S3
aws s3 cp $BACKUP_FILE s3://$S3_BUCKET/database/
aws s3 cp "${BACKUP_FILE}.sha256" s3://$S3_BUCKET/database/

# Clean up old local backups
find $BACKUP_DIR -name "db_backup_*.gz" -mtime +7 -delete

# Clean up old S3 backups
aws s3 ls s3://$S3_BUCKET/database/ | while read -r line; do
    createDate=$(echo $line | awk '{print $1" "$2}')
    createDate=$(date -d "$createDate" +%s)
    olderThan=$(date -d "$RETENTION_DAYS days ago" +%s)
    if [[ $createDate -lt $olderThan ]]; then
        fileName=$(echo $line | awk '{print $4}')
        if [[ $fileName != "" ]]; then
            aws s3 rm s3://$S3_BUCKET/database/$fileName
        fi
    fi
done

echo "Database backup completed at $(date)"

# Send notification
curl -X POST https://hooks.slack.com/services/YOUR/WEBHOOK/URL \
     -H 'Content-type: application/json' \
     -d "{\"text\":\"Database backup completed successfully. Size: $(du -h $BACKUP_FILE | cut -f1)\"}"
```

### File Backup Script

```bash
#!/bin/bash
# /opt/witchcityrope/scripts/backup-files.sh

set -e

# Configuration
SOURCE_DIR="/opt/witchcityrope/wwwroot/uploads"
BACKUP_DIR="/backups/files"
S3_BUCKET="witchcityrope-backups"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)

echo "Starting file backup at $(date)"

# Create backup directory
mkdir -p $BACKUP_DIR

# Create tar archive
tar -czf "$BACKUP_DIR/files_backup_$TIMESTAMP.tar.gz" -C $SOURCE_DIR .

# Upload to S3 with server-side encryption
aws s3 cp "$BACKUP_DIR/files_backup_$TIMESTAMP.tar.gz" \
    s3://$S3_BUCKET/files/ \
    --storage-class GLACIER_IR \
    --server-side-encryption AES256

# Sync to S3 (incremental)
aws s3 sync $SOURCE_DIR s3://$S3_BUCKET/files/current/ \
    --delete \
    --storage-class STANDARD_IA

echo "File backup completed at $(date)"
```

### Configuration Backup Script

```bash
#!/bin/bash
# /opt/witchcityrope/scripts/backup-config.sh

set -e

# Configuration
CONFIG_DIR="/opt/witchcityrope"
BACKUP_DIR="/backups/config"
S3_BUCKET="witchcityrope-backups"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)

echo "Starting configuration backup at $(date)"

# Create backup directory
mkdir -p $BACKUP_DIR

# Files to backup
CONFIG_FILES=(
    ".env.production"
    "docker-compose.prod.yml"
    "nginx/conf.d/*"
    "scripts/*"
    "init-scripts/*"
)

# Create tar archive
tar -czf "$BACKUP_DIR/config_backup_$TIMESTAMP.tar.gz" \
    -C $CONFIG_DIR \
    --files-from <(printf '%s\n' "${CONFIG_FILES[@]}")

# Encrypt sensitive backup
gpg --symmetric --cipher-algo AES256 \
    --output "$BACKUP_DIR/config_backup_$TIMESTAMP.tar.gz.gpg" \
    "$BACKUP_DIR/config_backup_$TIMESTAMP.tar.gz"

# Upload encrypted backup
aws s3 cp "$BACKUP_DIR/config_backup_$TIMESTAMP.tar.gz.gpg" \
    s3://$S3_BUCKET/config/

# Clean up
rm "$BACKUP_DIR/config_backup_$TIMESTAMP.tar.gz"

echo "Configuration backup completed at $(date)"
```

### Automated Backup Schedule

```cron
# /etc/cron.d/witchcityrope-backups

# Database backups - every 4 hours
0 */4 * * * root /opt/witchcityrope/scripts/backup-database.sh >> /var/log/witchcityrope/backup.log 2>&1

# File backups - daily at 2 AM
0 2 * * * root /opt/witchcityrope/scripts/backup-files.sh >> /var/log/witchcityrope/backup.log 2>&1

# Configuration backups - weekly on Sunday at 3 AM
0 3 * * 0 root /opt/witchcityrope/scripts/backup-config.sh >> /var/log/witchcityrope/backup.log 2>&1

# Backup verification - daily at 6 AM
0 6 * * * root /opt/witchcityrope/scripts/verify-backups.sh >> /var/log/witchcityrope/backup.log 2>&1
```

## Backup Verification

### Verification Script

```bash
#!/bin/bash
# /opt/witchcityrope/scripts/verify-backups.sh

set -e

# Configuration
S3_BUCKET="witchcityrope-backups"
TEMP_DIR="/tmp/backup-verify"
ALERT_EMAIL="admin@witchcityrope.com"

echo "Starting backup verification at $(date)"

# Create temp directory
mkdir -p $TEMP_DIR

# Function to verify database backup
verify_database_backup() {
    echo "Verifying database backup..."
    
    # Get latest backup
    LATEST_BACKUP=$(aws s3 ls s3://$S3_BUCKET/database/ | sort | tail -n 2 | head -n 1 | awk '{print $4}')
    
    if [ -z "$LATEST_BACKUP" ]; then
        echo "ERROR: No database backup found"
        return 1
    fi
    
    # Download backup
    aws s3 cp s3://$S3_BUCKET/database/$LATEST_BACKUP $TEMP_DIR/
    aws s3 cp s3://$S3_BUCKET/database/${LATEST_BACKUP%.gz}.sha256 $TEMP_DIR/
    
    # Verify checksum
    EXPECTED_CHECKSUM=$(cat $TEMP_DIR/${LATEST_BACKUP%.gz}.sha256)
    ACTUAL_CHECKSUM=$(sha256sum $TEMP_DIR/$LATEST_BACKUP | awk '{print $1}')
    
    if [ "$EXPECTED_CHECKSUM" != "$ACTUAL_CHECKSUM" ]; then
        echo "ERROR: Checksum mismatch for $LATEST_BACKUP"
        return 1
    fi
    
    # Test restore (partial)
    gunzip -c $TEMP_DIR/$LATEST_BACKUP | head -n 1000 > /dev/null
    
    echo "Database backup verification successful"
    return 0
}

# Function to verify file backup
verify_file_backup() {
    echo "Verifying file backup..."
    
    # Get latest backup
    LATEST_BACKUP=$(aws s3 ls s3://$S3_BUCKET/files/ | grep files_backup | sort | tail -n 1 | awk '{print $4}')
    
    if [ -z "$LATEST_BACKUP" ]; then
        echo "ERROR: No file backup found"
        return 1
    fi
    
    # Download and test
    aws s3 cp s3://$S3_BUCKET/files/$LATEST_BACKUP $TEMP_DIR/ --range bytes=0-1048576
    tar -tzf $TEMP_DIR/$LATEST_BACKUP > /dev/null 2>&1 || true
    
    echo "File backup verification successful"
    return 0
}

# Run verifications
ERRORS=0

verify_database_backup || ((ERRORS++))
verify_file_backup || ((ERRORS++))

# Clean up
rm -rf $TEMP_DIR

# Report results
if [ $ERRORS -eq 0 ]; then
    echo "All backup verifications passed"
else
    echo "Backup verification failed with $ERRORS errors"
    # Send alert
    echo "Backup verification failed. Please check logs." | mail -s "WitchCityRope Backup Alert" $ALERT_EMAIL
    exit 1
fi
```

## Disaster Recovery Procedures

### Recovery Plan Overview

1. **Assessment** (15 minutes)
   - Identify failure scope
   - Determine recovery strategy
   - Notify stakeholders

2. **Infrastructure Recovery** (30 minutes)
   - Provision new VPS if needed
   - Install Docker and dependencies
   - Configure networking

3. **Application Recovery** (45 minutes)
   - Restore application code
   - Restore configuration
   - Restore database
   - Restore files

4. **Verification** (30 minutes)
   - Test all functionality
   - Verify data integrity
   - Monitor performance

### Full System Recovery

```bash
#!/bin/bash
# /opt/witchcityrope/scripts/disaster-recovery.sh

set -e

echo "Starting disaster recovery at $(date)"

# Configuration
S3_BUCKET="witchcityrope-backups"
RECOVERY_DIR="/opt/witchcityrope"
TEMP_DIR="/tmp/recovery"

# Create directories
mkdir -p $RECOVERY_DIR $TEMP_DIR

# Step 1: Install dependencies
echo "Installing dependencies..."
apt update
apt install -y docker.io docker-compose git nginx certbot python3-certbot-nginx

# Step 2: Restore application code
echo "Restoring application code..."
cd $RECOVERY_DIR
git clone https://github.com/yourusername/WitchCityRope.git .

# Step 3: Restore configuration
echo "Restoring configuration..."
LATEST_CONFIG=$(aws s3 ls s3://$S3_BUCKET/config/ | sort | tail -n 1 | awk '{print $4}')
aws s3 cp s3://$S3_BUCKET/config/$LATEST_CONFIG $TEMP_DIR/

# Decrypt configuration
gpg --decrypt $TEMP_DIR/$LATEST_CONFIG > $TEMP_DIR/config.tar.gz
tar -xzf $TEMP_DIR/config.tar.gz -C $RECOVERY_DIR

# Step 4: Start infrastructure
echo "Starting infrastructure services..."
docker-compose -f docker-compose.prod.yml up -d db redis

# Wait for database
sleep 30

# Step 5: Restore database
echo "Restoring database..."
LATEST_DB=$(aws s3 ls s3://$S3_BUCKET/database/ | grep -v sha256 | sort | tail -n 1 | awk '{print $4}')
aws s3 cp s3://$S3_BUCKET/database/$LATEST_DB $TEMP_DIR/

# Restore to database
gunzip -c $TEMP_DIR/$LATEST_DB | docker exec -i witchcityrope-db psql -U witchcity -d WitchCityRope

# Step 6: Restore files
echo "Restoring files..."
aws s3 sync s3://$S3_BUCKET/files/current/ $RECOVERY_DIR/wwwroot/uploads/

# Step 7: Start application
echo "Starting application..."
docker-compose -f docker-compose.prod.yml up -d

# Step 8: Restore SSL certificates
echo "Restoring SSL certificates..."
certbot --nginx -d yourdomain.com -d www.yourdomain.com --non-interactive --agree-tos --email admin@witchcityrope.com

# Step 9: Verify recovery
echo "Verifying recovery..."
sleep 30
curl -f http://localhost:5000/health || exit 1

echo "Disaster recovery completed at $(date)"
```

### Database-Only Recovery

```bash
#!/bin/bash
# /opt/witchcityrope/scripts/restore-database.sh

set -e

# Configuration
S3_BUCKET="witchcityrope-backups"
DB_CONTAINER="witchcityrope-db"
DB_NAME="WitchCityRope"
DB_USER="witchcity"

# Get backup file from argument or latest
if [ -z "$1" ]; then
    BACKUP_FILE=$(aws s3 ls s3://$S3_BUCKET/database/ | grep -v sha256 | sort | tail -n 1 | awk '{print $4}')
else
    BACKUP_FILE=$1
fi

echo "Restoring database from $BACKUP_FILE"

# Download backup
aws s3 cp s3://$S3_BUCKET/database/$BACKUP_FILE /tmp/

# Stop application
docker-compose -f docker-compose.prod.yml stop web

# Drop existing database
docker exec -i $DB_CONTAINER psql -U $DB_USER -c "DROP DATABASE IF EXISTS $DB_NAME;"
docker exec -i $DB_CONTAINER psql -U $DB_USER -c "CREATE DATABASE $DB_NAME;"

# Restore database
gunzip -c /tmp/$BACKUP_FILE | docker exec -i $DB_CONTAINER psql -U $DB_USER -d $DB_NAME

# Start application
docker-compose -f docker-compose.prod.yml start web

echo "Database restoration completed"
```

## Backup Monitoring and Alerts

### Monitoring Script

```bash
#!/bin/bash
# /opt/witchcityrope/scripts/monitor-backups.sh

# Configuration
S3_BUCKET="witchcityrope-backups"
ALERT_THRESHOLD=8  # hours
SLACK_WEBHOOK="https://hooks.slack.com/services/YOUR/WEBHOOK/URL"

# Check database backup age
LATEST_DB=$(aws s3 ls s3://$S3_BUCKET/database/ | grep -v sha256 | sort | tail -n 1)
DB_DATE=$(echo $LATEST_DB | awk '{print $1" "$2}')
DB_AGE=$(( ($(date +%s) - $(date -d "$DB_DATE" +%s)) / 3600 ))

if [ $DB_AGE -gt $ALERT_THRESHOLD ]; then
    curl -X POST $SLACK_WEBHOOK \
         -H 'Content-type: application/json' \
         -d "{\"text\":\"⚠️ Database backup is $DB_AGE hours old (threshold: $ALERT_THRESHOLD hours)\"}"
fi

# Check file backup age
LATEST_FILE=$(aws s3 ls s3://$S3_BUCKET/files/ | grep files_backup | sort | tail -n 1)
FILE_DATE=$(echo $LATEST_FILE | awk '{print $1" "$2}')
FILE_AGE=$(( ($(date +%s) - $(date -d "$FILE_DATE" +%s)) / 3600 ))

if [ $FILE_AGE -gt 24 ]; then
    curl -X POST $SLACK_WEBHOOK \
         -H 'Content-type: application/json' \
         -d "{\"text\":\"⚠️ File backup is $FILE_AGE hours old\"}"
fi
```

### Grafana Dashboard

```json
{
  "dashboard": {
    "title": "Backup Monitoring",
    "panels": [
      {
        "title": "Last Backup Age",
        "targets": [
          {
            "expr": "time() - backup_last_success_timestamp"
          }
        ]
      },
      {
        "title": "Backup Size Trend",
        "targets": [
          {
            "expr": "backup_size_bytes"
          }
        ]
      },
      {
        "title": "Backup Success Rate",
        "targets": [
          {
            "expr": "rate(backup_success_total[24h])"
          }
        ]
      }
    ]
  }
}
```

## Testing and Drills

### Monthly Recovery Test

```bash
#!/bin/bash
# /opt/witchcityrope/scripts/recovery-test.sh

# Test recovery to staging environment
echo "Starting monthly recovery test"

# Create test VPS
# Perform full recovery
# Run automated tests
# Generate report

echo "Recovery test completed. RTO: X minutes, RPO verified: Y hours"
```

### Recovery Checklist

- [ ] Identify incident type and scope
- [ ] Activate incident response team
- [ ] Assess data loss window
- [ ] Choose recovery strategy
- [ ] Provision infrastructure if needed
- [ ] Restore from backups
- [ ] Verify data integrity
- [ ] Test application functionality
- [ ] Update DNS if needed
- [ ] Notify users of recovery
- [ ] Document lessons learned

## Best Practices

1. **Regular Testing**
   - Test restore procedures monthly
   - Verify backup integrity daily
   - Document recovery times

2. **Security**
   - Encrypt sensitive backups
   - Use IAM roles for S3 access
   - Rotate encryption keys

3. **Documentation**
   - Keep recovery procedures updated
   - Document all passwords securely
   - Maintain contact lists

4. **Redundancy**
   - Multiple backup locations
   - Different storage classes
   - Cross-region replication

5. **Monitoring**
   - Alert on backup failures
   - Track backup metrics
   - Regular audit reports
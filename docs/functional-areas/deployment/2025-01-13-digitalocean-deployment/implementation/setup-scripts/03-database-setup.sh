#!/bin/bash
# Database Setup Script - Configure connection to DigitalOcean Managed PostgreSQL
# Run this script as the witchcity user
# Usage: ./03-database-setup.sh

set -euo pipefail

echo "🗄️  Setting up WitchCityRope database configuration..."
echo "📅 Started at: $(date)"

# Configuration - REPLACE THESE WITH YOUR ACTUAL VALUES
DB_HOST="your-postgres-cluster-do-user-XXXXX-0.db.ondigitalocean.com"
DB_PORT="25060"
DB_USER="doadmin"
DB_PASSWORD=""  # Will prompt for this
DB_NAME_PROD="witchcityrope_prod"
DB_NAME_STAGING="witchcityrope_staging"
SSL_MODE="require"

# Check if running as correct user
if [ "$USER" = "root" ]; then
    echo "❌ This script should not be run as root"
    echo "   Please run as witchcity user: ./03-database-setup.sh"
    exit 1
fi

# Function to prompt for database credentials
prompt_for_credentials() {
    echo "🔐 Please provide your DigitalOcean Managed PostgreSQL credentials:"
    echo ""

    # Prompt for database host
    read -p "Enter Database Host (e.g., your-cluster-do-user-XXXXX-0.db.ondigitalocean.com): " DB_HOST
    if [ -z "$DB_HOST" ]; then
        echo "❌ Database host is required"
        exit 1
    fi

    # Prompt for database port
    read -p "Enter Database Port [25060]: " port_input
    DB_PORT=${port_input:-25060}

    # Prompt for database user
    read -p "Enter Database User [doadmin]: " user_input
    DB_USER=${user_input:-doadmin}

    # Prompt for database password (hidden input)
    echo -n "Enter Database Password: "
    read -s DB_PASSWORD
    echo ""

    if [ -z "$DB_PASSWORD" ]; then
        echo "❌ Database password is required"
        exit 1
    fi

    echo "✅ Credentials collected"
}

# Function to test database connection
test_connection() {
    local db_name=$1
    local test_type=$2

    echo "🔍 Testing connection to ${test_type} database: ${db_name}"

    # Create temporary .pgpass file for authentication
    PGPASS_FILE="/tmp/.pgpass_test"
    echo "${DB_HOST}:${DB_PORT}:${db_name}:${DB_USER}:${DB_PASSWORD}" > "$PGPASS_FILE"
    chmod 600 "$PGPASS_FILE"

    # Test connection using docker with PostgreSQL client
    if docker run --rm \
        -e PGPASSFILE=/tmp/.pgpass \
        -v "$PGPASS_FILE:/tmp/.pgpass:ro" \
        postgres:16-alpine \
        psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$db_name" -c "SELECT version();" > /dev/null 2>&1; then
        echo "✅ ${test_type} database connection: SUCCESS"
        rm -f "$PGPASS_FILE"
        return 0
    else
        echo "❌ ${test_type} database connection: FAILED"
        rm -f "$PGPASS_FILE"
        return 1
    fi
}

# Function to create database if it doesn't exist
create_database() {
    local db_name=$1
    local test_type=$2

    echo "🏗️  Creating ${test_type} database: ${db_name}"

    # Create temporary .pgpass file
    PGPASS_FILE="/tmp/.pgpass_create"
    echo "${DB_HOST}:${DB_PORT}:postgres:${DB_USER}:${DB_PASSWORD}" > "$PGPASS_FILE"
    chmod 600 "$PGPASS_FILE"

    # Check if database exists and create if not
    DB_EXISTS=$(docker run --rm \
        -e PGPASSFILE=/tmp/.pgpass \
        -v "$PGPASS_FILE:/tmp/.pgpass:ro" \
        postgres:16-alpine \
        psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d postgres -tc "SELECT 1 FROM pg_database WHERE datname='$db_name'" | xargs)

    if [ "$DB_EXISTS" = "1" ]; then
        echo "✅ Database ${db_name} already exists"
    else
        echo "Creating database ${db_name}..."
        docker run --rm \
            -e PGPASSFILE=/tmp/.pgpass \
            -v "$PGPASS_FILE:/tmp/.pgpass:ro" \
            postgres:16-alpine \
            psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d postgres -c "CREATE DATABASE $db_name;"
        echo "✅ Database ${db_name} created successfully"
    fi

    rm -f "$PGPASS_FILE"
}

# Get credentials from user
prompt_for_credentials

# Test connection to default postgres database first
echo "🔍 Testing initial connection to PostgreSQL server..."
PGPASS_FILE="/tmp/.pgpass_initial"
echo "${DB_HOST}:${DB_PORT}:postgres:${DB_USER}:${DB_PASSWORD}" > "$PGPASS_FILE"
chmod 600 "$PGPASS_FILE"

if docker run --rm \
    -e PGPASSFILE=/tmp/.pgpass \
    -v "$PGPASS_FILE:/tmp/.pgpass:ro" \
    postgres:16-alpine \
    psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d postgres -c "SELECT version();" > /dev/null 2>&1; then
    echo "✅ PostgreSQL server connection: SUCCESS"
    rm -f "$PGPASS_FILE"
else
    echo "❌ PostgreSQL server connection: FAILED"
    echo "   Please check your credentials and network connectivity"
    rm -f "$PGPASS_FILE"
    exit 1
fi

# Create databases
create_database "$DB_NAME_PROD" "Production"
create_database "$DB_NAME_STAGING" "Staging"

# Test connections to both databases
test_connection "$DB_NAME_PROD" "Production"
test_connection "$DB_NAME_STAGING" "Staging"

# Create environment configuration files
echo "📝 Creating environment configuration files..."

# Create production environment file
cat > /opt/witchcityrope/production/.env.production << EOF
# WitchCityRope Production Environment Configuration
# Generated on $(date)

# Database Configuration
DATABASE_HOST=${DB_HOST}
DATABASE_PORT=${DB_PORT}
DATABASE_NAME=${DB_NAME_PROD}
DATABASE_USER=${DB_USER}
DATABASE_PASSWORD=${DB_PASSWORD}
DATABASE_SSL_MODE=${SSL_MODE}

# Connection String for .NET API
ConnectionStrings__DefaultConnection=Host=${DB_HOST};Port=${DB_PORT};Database=${DB_NAME_PROD};Username=${DB_USER};Password=${DB_PASSWORD};SSL Mode=${SSL_MODE};Trust Server Certificate=true

# Application Configuration
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:5001

# JWT Configuration (CHANGE THESE IN PRODUCTION!)
Authentication__JwtSecret=CHANGE_THIS_IN_PRODUCTION_SUPER_SECRET_KEY_MINIMUM_64_CHARS!
Authentication__Issuer=WitchCityRope
Authentication__Audience=WitchCityRopeUsers
Authentication__ExpiryMinutes=60
Authentication__RequireHttps=true

# CORS Configuration
CORS__AllowedOrigins=https://witchcityrope.com
CORS__AllowCredentials=true

# Logging Configuration
Logging__LogLevel__Default=Information
Logging__LogLevel__Microsoft=Warning
Logging__LogLevel__WitchCityRope=Information

# Redis Configuration
Redis__ConnectionString=localhost:6379
Redis__Database=0

# File Storage Configuration
Storage__Type=Local
Storage__LocalPath=/app/uploads
Storage__MaxFileSize=10485760

# Email Configuration (CONFIGURE THESE)
Email__SmtpHost=smtp.example.com
Email__SmtpPort=587
Email__SmtpUser=noreply@witchcityrope.com
Email__SmtpPassword=CHANGE_ME
Email__FromAddress=noreply@witchcityrope.com
Email__FromName=WitchCityRope

# Security Configuration
Security__AllowedHosts=witchcityrope.com
Security__RequireHttps=true
Security__HstsMaxAge=31536000
EOF

# Create staging environment file
cat > /opt/witchcityrope/staging/.env.staging << EOF
# WitchCityRope Staging Environment Configuration
# Generated on $(date)

# Database Configuration
DATABASE_HOST=${DB_HOST}
DATABASE_PORT=${DB_PORT}
DATABASE_NAME=${DB_NAME_STAGING}
DATABASE_USER=${DB_USER}
DATABASE_PASSWORD=${DB_PASSWORD}
DATABASE_SSL_MODE=${SSL_MODE}

# Connection String for .NET API
ConnectionStrings__DefaultConnection=Host=${DB_HOST};Port=${DB_PORT};Database=${DB_NAME_STAGING};Username=${DB_USER};Password=${DB_PASSWORD};SSL Mode=${SSL_MODE};Trust Server Certificate=true

# Application Configuration
ASPNETCORE_ENVIRONMENT=Staging
ASPNETCORE_URLS=http://+:5002

# JWT Configuration (DIFFERENT FROM PRODUCTION!)
Authentication__JwtSecret=STAGING_SECRET_KEY_DIFFERENT_FROM_PRODUCTION_MINIMUM_64_CHARS!
Authentication__Issuer=WitchCityRope-Staging
Authentication__Audience=WitchCityRopeUsers-Staging
Authentication__ExpiryMinutes=60
Authentication__RequireHttps=true

# CORS Configuration
CORS__AllowedOrigins=https://staging.witchcityrope.com
CORS__AllowCredentials=true

# Logging Configuration
Logging__LogLevel__Default=Debug
Logging__LogLevel__Microsoft=Information
Logging__LogLevel__WitchCityRope=Debug

# Redis Configuration
Redis__ConnectionString=localhost:6379
Redis__Database=1

# File Storage Configuration
Storage__Type=Local
Storage__LocalPath=/app/uploads
Storage__MaxFileSize=10485760

# Email Configuration (TEST SETTINGS)
Email__SmtpHost=smtp.example.com
Email__SmtpPort=587
Email__SmtpUser=staging@witchcityrope.com
Email__SmtpPassword=CHANGE_ME
Email__FromAddress=staging@witchcityrope.com
Email__FromName=WitchCityRope Staging

# Security Configuration
Security__AllowedHosts=staging.witchcityrope.com
Security__RequireHttps=true
Security__HstsMaxAge=31536000
EOF

# Set proper permissions for environment files
chmod 600 /opt/witchcityrope/production/.env.production
chmod 600 /opt/witchcityrope/staging/.env.staging

echo "✅ Environment configuration files created"

# Create database management scripts
echo "🔧 Creating database management scripts..."

# Create database backup script
cat > /opt/witchcityrope/backup-database.sh << 'EOF'
#!/bin/bash
# Database Backup Script for WitchCityRope
# Creates compressed backups of both production and staging databases

set -euo pipefail

# Load environment variables
if [ -f /opt/witchcityrope/production/.env.production ]; then
    source /opt/witchcityrope/production/.env.production
else
    echo "❌ Production environment file not found"
    exit 1
fi

BACKUP_DIR="/opt/backups/witchcityrope/database"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)

# Create backup directory
mkdir -p "$BACKUP_DIR"

echo "🗄️  Creating database backups at $(date)"

# Backup production database
echo "📦 Backing up production database..."
PROD_BACKUP="$BACKUP_DIR/production_${TIMESTAMP}.sql.gz"

docker run --rm \
    -e PGPASSWORD="$DATABASE_PASSWORD" \
    postgres:16-alpine \
    pg_dump -h "$DATABASE_HOST" -p "$DATABASE_PORT" -U "$DATABASE_USER" -d "witchcityrope_prod" \
    --no-owner --no-privileges --clean --if-exists | gzip > "$PROD_BACKUP"

echo "✅ Production backup: $PROD_BACKUP"

# Backup staging database
echo "📦 Backing up staging database..."
STAGING_BACKUP="$BACKUP_DIR/staging_${TIMESTAMP}.sql.gz"

docker run --rm \
    -e PGPASSWORD="$DATABASE_PASSWORD" \
    postgres:16-alpine \
    pg_dump -h "$DATABASE_HOST" -p "$DATABASE_PORT" -U "$DATABASE_USER" -d "witchcityrope_staging" \
    --no-owner --no-privileges --clean --if-exists | gzip > "$STAGING_BACKUP"

echo "✅ Staging backup: $STAGING_BACKUP"

# Clean up old backups (keep last 30 days)
find "$BACKUP_DIR" -name "*.sql.gz" -mtime +30 -delete

echo "✅ Database backup completed at $(date)"
echo "   Production: $PROD_BACKUP"
echo "   Staging: $STAGING_BACKUP"
EOF

chmod +x /opt/witchcityrope/backup-database.sh

# Create database restore script
cat > /opt/witchcityrope/restore-database.sh << 'EOF'
#!/bin/bash
# Database Restore Script for WitchCityRope
# Restores database from backup file

set -euo pipefail

# Check arguments
if [ $# -ne 2 ]; then
    echo "Usage: $0 <backup_file> <environment>"
    echo "Example: $0 /opt/backups/production_20250913_120000.sql.gz production"
    exit 1
fi

BACKUP_FILE="$1"
ENVIRONMENT="$2"

if [ ! -f "$BACKUP_FILE" ]; then
    echo "❌ Backup file not found: $BACKUP_FILE"
    exit 1
fi

# Load environment variables
ENV_FILE="/opt/witchcityrope/$ENVIRONMENT/.env.$ENVIRONMENT"
if [ -f "$ENV_FILE" ]; then
    source "$ENV_FILE"
else
    echo "❌ Environment file not found: $ENV_FILE"
    exit 1
fi

# Determine database name
if [ "$ENVIRONMENT" = "production" ]; then
    DB_NAME="witchcityrope_prod"
elif [ "$ENVIRONMENT" = "staging" ]; then
    DB_NAME="witchcityrope_staging"
else
    echo "❌ Invalid environment: $ENVIRONMENT (must be 'production' or 'staging')"
    exit 1
fi

echo "⚠️  WARNING: This will restore $DB_NAME from $BACKUP_FILE"
echo "   This will OVERWRITE the current database!"
read -p "Are you sure? (yes/no): " confirm

if [ "$confirm" != "yes" ]; then
    echo "Restore cancelled"
    exit 1
fi

echo "🗄️  Restoring database $DB_NAME from $BACKUP_FILE..."

# Restore database
zcat "$BACKUP_FILE" | docker run --rm -i \
    -e PGPASSWORD="$DATABASE_PASSWORD" \
    postgres:16-alpine \
    psql -h "$DATABASE_HOST" -p "$DATABASE_PORT" -U "$DATABASE_USER" -d "$DB_NAME"

echo "✅ Database restore completed at $(date)"
EOF

chmod +x /opt/witchcityrope/restore-database.sh

# Create database connection test script
cat > /opt/witchcityrope/test-database.sh << 'EOF'
#!/bin/bash
# Database Connection Test Script for WitchCityRope
# Tests connectivity to both production and staging databases

set -euo pipefail

echo "🔍 Testing database connections at $(date)"
echo "=============================================="

# Test production database
echo "Testing production database..."
if [ -f /opt/witchcityrope/production/.env.production ]; then
    source /opt/witchcityrope/production/.env.production

    if docker run --rm \
        -e PGPASSWORD="$DATABASE_PASSWORD" \
        postgres:16-alpine \
        psql -h "$DATABASE_HOST" -p "$DATABASE_PORT" -U "$DATABASE_USER" -d "witchcityrope_prod" \
        -c "SELECT 'Production DB Connected' as status;" > /dev/null 2>&1; then
        echo "✅ Production database: Connected"
    else
        echo "❌ Production database: Connection failed"
    fi
else
    echo "❌ Production environment file not found"
fi

# Test staging database
echo "Testing staging database..."
if [ -f /opt/witchcityrope/staging/.env.staging ]; then
    source /opt/witchcityrope/staging/.env.staging

    if docker run --rm \
        -e PGPASSWORD="$DATABASE_PASSWORD" \
        postgres:16-alpine \
        psql -h "$DATABASE_HOST" -p "$DATABASE_PORT" -U "$DATABASE_USER" -d "witchcityrope_staging" \
        -c "SELECT 'Staging DB Connected' as status;" > /dev/null 2>&1; then
        echo "✅ Staging database: Connected"
    else
        echo "❌ Staging database: Connection failed"
    fi
else
    echo "❌ Staging environment file not found"
fi

echo "=============================================="
echo "Database connection test completed at $(date)"
EOF

chmod +x /opt/witchcityrope/test-database.sh

# Add database backup to cron (daily at 3 AM)
echo "⏰ Setting up automated daily database backups..."
(crontab -l 2>/dev/null; echo "0 3 * * * /opt/witchcityrope/backup-database.sh >> /var/log/witchcityrope/backup.log 2>&1") | crontab -

echo "✅ Automated database backups configured"

# Create initial backup
echo "📦 Creating initial database backup..."
/opt/witchcityrope/backup-database.sh

# Test database connections
echo "🔍 Testing database connections..."
/opt/witchcityrope/test-database.sh

# Final summary
echo ""
echo "✅ Database setup completed successfully!"
echo ""
echo "📋 Setup Summary:"
echo "   • Production database: ${DB_NAME_PROD} (${DB_HOST}:${DB_PORT})"
echo "   • Staging database: ${DB_NAME_STAGING} (${DB_HOST}:${DB_PORT})"
echo "   • Environment files created with secure permissions"
echo "   • Database management scripts created"
echo "   • Automated daily backups configured (3 AM)"
echo "   • Initial backup created"
echo ""
echo "📁 Important Files:"
echo "   • Production config: /opt/witchcityrope/production/.env.production"
echo "   • Staging config: /opt/witchcityrope/staging/.env.staging"
echo "   • Backup script: /opt/witchcityrope/backup-database.sh"
echo "   • Restore script: /opt/witchcityrope/restore-database.sh"
echo "   • Test script: /opt/witchcityrope/test-database.sh"
echo ""
echo "🚨 IMPORTANT SECURITY NOTES:"
echo "   1. CHANGE the JWT secrets in production environment files!"
echo "   2. Environment files contain sensitive data - protect them!"
echo "   3. Configure email settings for production notifications"
echo "   4. Review and update CORS origins for your actual domains"
echo ""
echo "🚨 NEXT STEPS:"
echo "   1. Update JWT secrets in environment files"
echo "   2. Configure email settings"
echo "   3. Run next script: 04-ssl-setup.sh"
echo ""
echo "🔧 Useful Commands:"
echo "   • Test database: /opt/witchcityrope/test-database.sh"
echo "   • Backup database: /opt/witchcityrope/backup-database.sh"
echo "   • Restore database: /opt/witchcityrope/restore-database.sh <backup_file> <environment>"
echo ""
echo "📅 Completed at: $(date)"
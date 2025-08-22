#!/bin/bash
# Staging Database Initialization Script
# This script initializes the staging database with test data

set -e  # Exit on error

# Configuration
DB_PATH="${DB_PATH:-/app/data/witchcityrope_staging.db}"
BACKUP_DIR="${BACKUP_DIR:-/app/backups}"
MIGRATION_TIMEOUT="${MIGRATION_TIMEOUT:-300}"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Functions
log_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if running as staging environment
if [ "$ASPNETCORE_ENVIRONMENT" != "Staging" ]; then
    log_error "This script should only be run in the Staging environment"
    log_error "Current environment: $ASPNETCORE_ENVIRONMENT"
    exit 1
fi

# Create necessary directories
log_info "Creating necessary directories..."
mkdir -p "$(dirname "$DB_PATH")"
mkdir -p "$BACKUP_DIR"

# Backup existing database if it exists
if [ -f "$DB_PATH" ]; then
    BACKUP_FILE="$BACKUP_DIR/witchcityrope_staging_$(date +%Y%m%d_%H%M%S).db"
    log_warning "Existing database found. Creating backup at $BACKUP_FILE"
    cp "$DB_PATH" "$BACKUP_FILE"
    
    # Keep only last 5 backups
    cd "$BACKUP_DIR"
    ls -t witchcityrope_staging_*.db | tail -n +6 | xargs -r rm
    cd - > /dev/null
fi

# Run Entity Framework migrations
log_info "Running database migrations..."
cd /app

# Check if dotnet ef is available
if ! command -v dotnet &> /dev/null; then
    log_error "dotnet CLI not found. Please ensure .NET SDK is installed."
    exit 1
fi

# Update database
timeout $MIGRATION_TIMEOUT dotnet ef database update \
    --project src/WitchCityRope.Infrastructure \
    --startup-project src/WitchCityRope.Api \
    --context WitchCityRopeDbContext \
    --verbose

if [ $? -eq 0 ]; then
    log_info "Database migrations completed successfully"
else
    log_error "Database migration failed"
    exit 1
fi

# Run SQL initialization script
log_info "Initializing staging data..."
sqlite3 "$DB_PATH" << 'EOF'
-- Enable foreign keys
PRAGMA foreign_keys = ON;

-- Start transaction
BEGIN TRANSACTION;

-- Insert test admin user (password: StagingAdmin123!)
INSERT OR IGNORE INTO Users (
    Id, 
    Email, 
    PasswordHash, 
    SceneName, 
    LegalName,
    Role, 
    EmailVerified, 
    IsActive,
    CreatedAt,
    UpdatedAt
) VALUES (
    '00000000-0000-0000-0000-000000000001',
    'admin@staging.witchcityrope.com',
    '$2a$11$rBkZPQqRQCKh1HGYflJ8/uE6GcFiNR4c9hXKjH4p5tFXKgBqKClnO', -- StagingAdmin123!
    'Staging Admin',
    'Admin User',
    'Admin',
    1,
    1,
    datetime('now'),
    datetime('now')
);

-- Insert test moderator user (password: StagingMod123!)
INSERT OR IGNORE INTO Users (
    Id, 
    Email, 
    PasswordHash, 
    SceneName, 
    LegalName,
    Role, 
    EmailVerified, 
    IsActive,
    CreatedAt,
    UpdatedAt
) VALUES (
    '00000000-0000-0000-0000-000000000002',
    'moderator@staging.witchcityrope.com',
    '$2a$11$ZmBa.S.X5B3f1TQqLcpwSuPGZJQTPbnc4S.oGQBQJ8vdPl3bkr7oi', -- StagingMod123!
    'Staging Moderator',
    'Moderator User',
    'Moderator',
    1,
    1,
    datetime('now'),
    datetime('now')
);

-- Insert test regular user (password: StagingUser123!)
INSERT OR IGNORE INTO Users (
    Id, 
    Email, 
    PasswordHash, 
    SceneName, 
    LegalName,
    Role, 
    EmailVerified, 
    IsActive,
    CreatedAt,
    UpdatedAt
) VALUES (
    '00000000-0000-0000-0000-000000000003',
    'user@staging.witchcityrope.com',
    '$2a$11$0FHwLb3V3tJFjKRQ.qrWH.C9iF5W7w6IxOZjKxLvCIL5MzGzOJqOe', -- StagingUser123!
    'Staging User',
    'Regular User',
    'Member',
    1,
    1,
    datetime('now'),
    datetime('now')
);

-- Insert test events
INSERT OR IGNORE INTO Events (
    Id,
    Title,
    Description,
    StartDate,
    EndDate,
    Location,
    Capacity,
    Price,
    Status,
    Type,
    CreatedBy,
    CreatedAt,
    UpdatedAt
) VALUES 
(
    '00000000-0000-0000-0000-000000000101',
    'Staging Test Event - Rope Workshop',
    'A test rope workshop event for staging environment testing.',
    datetime('now', '+7 days'),
    datetime('now', '+7 days', '+3 hours'),
    'Staging Test Venue, Salem MA',
    20,
    25.00,
    'Published',
    'Workshop',
    '00000000-0000-0000-0000-000000000001',
    datetime('now'),
    datetime('now')
),
(
    '00000000-0000-0000-0000-000000000102',
    'Staging Test Event - Social Night',
    'A test social event for staging environment testing.',
    datetime('now', '+14 days'),
    datetime('now', '+14 days', '+4 hours'),
    'Staging Test Venue, Salem MA',
    50,
    15.00,
    'Published',
    'Social',
    '00000000-0000-0000-0000-000000000001',
    datetime('now'),
    datetime('now')
),
(
    '00000000-0000-0000-0000-000000000103',
    'Staging Test Event - Past Workshop',
    'A past event for testing historical data.',
    datetime('now', '-7 days'),
    datetime('now', '-7 days', '+3 hours'),
    'Staging Test Venue, Salem MA',
    15,
    30.00,
    'Completed',
    'Workshop',
    '00000000-0000-0000-0000-000000000001',
    datetime('now', '-7 days'),
    datetime('now', '-7 days')
);

-- Insert test registrations
INSERT OR IGNORE INTO Registrations (
    Id,
    EventId,
    UserId,
    Status,
    PaymentStatus,
    Amount,
    CreatedAt,
    UpdatedAt
) VALUES
(
    '00000000-0000-0000-0000-000000000201',
    '00000000-0000-0000-0000-000000000101',
    '00000000-0000-0000-0000-000000000003',
    'Confirmed',
    'Paid',
    25.00,
    datetime('now'),
    datetime('now')
),
(
    '00000000-0000-0000-0000-000000000202',
    '00000000-0000-0000-0000-000000000102',
    '00000000-0000-0000-0000-000000000003',
    'Pending',
    'Pending',
    15.00,
    datetime('now'),
    datetime('now')
);

-- Insert test vetting application
INSERT OR IGNORE INTO VettingApplications (
    Id,
    UserId,
    Status,
    SubmittedAt,
    ReviewedAt,
    ReviewedBy,
    ReviewNotes,
    EmergencyContactName,
    EmergencyContactPhone,
    Experience,
    References
) VALUES (
    '00000000-0000-0000-0000-000000000301',
    '00000000-0000-0000-0000-000000000003',
    'Pending',
    datetime('now', '-2 days'),
    NULL,
    NULL,
    NULL,
    'Test Emergency Contact',
    '555-0123',
    'Test experience for staging environment',
    'Test Reference 1, Test Reference 2'
);

-- Commit transaction
COMMIT;

-- Verify data
SELECT 'Users:' as table_name, COUNT(*) as count FROM Users
UNION ALL
SELECT 'Events:', COUNT(*) FROM Events
UNION ALL
SELECT 'Registrations:', COUNT(*) FROM Registrations
UNION ALL
SELECT 'VettingApplications:', COUNT(*) FROM VettingApplications;
EOF

if [ $? -eq 0 ]; then
    log_info "Staging data initialized successfully"
else
    log_error "Failed to initialize staging data"
    exit 1
fi

# Set proper permissions
chmod 666 "$DB_PATH"
log_info "Database permissions set"

# Create indexes for performance
log_info "Creating performance indexes..."
sqlite3 "$DB_PATH" << 'EOF'
-- Performance indexes (if not already created by migrations)
CREATE INDEX IF NOT EXISTS idx_users_email ON Users(Email);
CREATE INDEX IF NOT EXISTS idx_users_role ON Users(Role);
CREATE INDEX IF NOT EXISTS idx_events_startdate ON Events(StartDate);
CREATE INDEX IF NOT EXISTS idx_events_status ON Events(Status);
CREATE INDEX IF NOT EXISTS idx_registrations_eventid ON Registrations(EventId);
CREATE INDEX IF NOT EXISTS idx_registrations_userid ON Registrations(UserId);
CREATE INDEX IF NOT EXISTS idx_vetting_userid ON VettingApplications(UserId);
CREATE INDEX IF NOT EXISTS idx_vetting_status ON VettingApplications(Status);

-- Analyze database for query optimization
ANALYZE;
EOF

# Verify database integrity
log_info "Verifying database integrity..."
sqlite3 "$DB_PATH" "PRAGMA integrity_check;"

if [ $? -eq 0 ]; then
    log_info "Database integrity check passed"
else
    log_error "Database integrity check failed"
    exit 1
fi

# Display summary
log_info "Staging database initialization complete!"
log_info "Database location: $DB_PATH"
log_info "Database size: $(du -h "$DB_PATH" | cut -f1)"
log_info ""
log_info "Test accounts created:"
log_info "  Admin: admin@staging.witchcityrope.com (password: StagingAdmin123!)"
log_info "  Moderator: moderator@staging.witchcityrope.com (password: StagingMod123!)"
log_info "  User: user@staging.witchcityrope.com (password: StagingUser123!)"
log_info ""
log_info "Test data created:"
sqlite3 "$DB_PATH" << 'EOF'
SELECT '  - ' || COUNT(*) || ' test events' FROM Events;
SELECT '  - ' || COUNT(*) || ' test registrations' FROM Registrations;
SELECT '  - ' || COUNT(*) || ' test vetting applications' FROM VettingApplications;
EOF

exit 0
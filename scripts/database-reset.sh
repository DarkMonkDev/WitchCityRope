#!/bin/bash
# Database Reset Script for WitchCityRope Development
# This script completely resets the development database and applies all migrations with seeding

set -e

echo "🔄 Starting database reset for WitchCityRope development..."

# Configuration
DB_HOST="localhost"
DB_PORT="5433"
DB_USER="postgres"
DB_PASSWORD="devpass123"
DB_NAME="witchcityrope_dev"

# Export password for psql
export PGPASSWORD=$DB_PASSWORD

echo "📡 Terminating existing database connections..."
psql -h $DB_HOST -p $DB_PORT -U $DB_USER -c "SELECT pg_terminate_backend(pg_stat_activity.pid) FROM pg_stat_activity WHERE pg_stat_activity.datname = '$DB_NAME' AND pid <> pg_backend_pid();" 2>/dev/null || true

echo "🗑️  Dropping existing database..."
psql -h $DB_HOST -p $DB_PORT -U $DB_USER -c "DROP DATABASE IF EXISTS $DB_NAME;"

echo "🏗️  Creating fresh database..."
psql -h $DB_HOST -p $DB_PORT -U $DB_USER -c "CREATE DATABASE $DB_NAME;"

echo "🔧 Applying Entity Framework migrations..."
cd "$(dirname "$0")/../src/WitchCityRope.Infrastructure"
ASPNETCORE_ENVIRONMENT=Development dotnet ef database update

echo "✅ Database reset complete!"
echo ""
echo "📊 Database Status:"
echo "Database: $DB_NAME"
echo "Host: $DB_HOST:$DB_PORT"
echo "User: $DB_USER"
echo ""
echo "🚀 You can now start the API which will automatically seed data:"
echo "cd src/WitchCityRope.Api && ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=\"http://localhost:5653\" dotnet run --no-launch-profile"
#!/bin/bash

# Database connection details from appsettings.json
DB_HOST="localhost"
DB_PORT="5432"
DB_NAME="witchcityrope_db"
DB_USER="postgres"
DB_PASSWORD="your_secure_password_here"

# Export password for psql
export PGPASSWORD=$DB_PASSWORD

# Run the SQL script
psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -f fix-nullable-columns.sql

# Unset the password variable
unset PGPASSWORD

echo "Migration completed!"
#!/bin/bash

# Script to generate EF Core migrations
# Usage: ./scripts/generate-migration.sh MigrationName

set -e

if [ -z "$1" ]; then
    echo "Usage: $0 MigrationName"
    echo "Example: $0 AddUserProfile"
    exit 1
fi

MIGRATION_NAME=$1

echo "Generating migration: $MIGRATION_NAME"

# Run migration generation inside the web container
docker exec witchcity-web bash -c "
    cd /src && \
    export PATH=\"\$PATH:/root/.dotnet/tools\" && \
    dotnet tool install --global dotnet-ef --version 9.* || true && \
    dotnet ef migrations add $MIGRATION_NAME \
        --project WitchCityRope.Infrastructure \
        --startup-project WitchCityRope.Web \
        --output-dir Data/Migrations \
        --context WitchCityRopeIdentityDbContext \
        --verbose
"

echo "Migration generation complete!"
echo "To apply the migration, run: ./scripts/apply-migrations.sh"
#!/bin/bash
# Script to apply EF Core migrations to the database

echo "Applying migrations to the database..."

docker exec witchcity-web bash -c "
    cd /src && \
    export PATH=\"\$PATH:/root/.dotnet/tools\" && \
    dotnet tool install --global dotnet-ef --version 9.* || true && \
    dotnet ef database update \
        --project WitchCityRope.Infrastructure \
        --startup-project WitchCityRope.Web \
        --context WitchCityRopeIdentityDbContext \
        --verbose
"

if [ $? -eq 0 ]; then
    echo "Migrations applied successfully!"
else
    echo "Failed to apply migrations. Check the output above for errors."
    exit 1
fi
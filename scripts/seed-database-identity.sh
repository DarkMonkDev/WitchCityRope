#!/bin/bash

# Navigate to the DatabaseSeeder tool directory
cd "$(dirname "$0")/../tools/DatabaseSeeder" || exit 1

echo "========================================="
echo "WitchCityRope Database Seeder"
echo "========================================="
echo ""
echo "This will seed the database with test data including:"
echo "- 5 test users (with Identity authentication)"
echo "- 10 events (past and upcoming)"
echo "- Event registrations and payments"
echo "- Vetting applications"
echo "- Sample incident reports"
echo ""
echo "WARNING: This should only be run on a development database!"
echo ""
read -p "Do you want to continue? (y/N) " -n 1 -r
echo ""

if [[ ! $REPLY =~ ^[Yy]$ ]]
then
    echo "Seeding cancelled."
    exit 0
fi

echo ""
echo "Running database seeder..."
echo ""

# Run the seeder
dotnet run

echo ""
echo "Database seeding process completed!"
echo ""
echo "To verify the seed data, you can:"
echo "1. Start the application and log in with one of the test accounts"
echo "2. Use a database client to inspect the tables directly"
echo "3. Check the API endpoints to see the seeded data"
echo ""
#!/bin/bash

# List all users in the WitchCityRope database
# This is helpful for finding the exact email to use with the password reset tool

echo "Listing all users in the WitchCityRope database..."
echo ""

psql -h localhost -p 5433 -U postgres -d witchcityrope_db -c "
SELECT 
    \"Email\" as email,
    \"SceneName\" as scene_name,
    \"IsVetted\" as is_vetted,
    \"Role\" as role,
    \"IsActive\" as is_active,
    \"CreatedAt\" as created_at
FROM auth.\"Users\"
ORDER BY \"CreatedAt\" DESC;"
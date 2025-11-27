#!/bin/bash
# Script to create EF Core migration for removing FullName fields
# Date: 2025-11-27
# Purpose: Drop FullName columns from Users and VettingApplications tables

set -e  # Exit on error

echo "Creating migration to remove FullName fields..."
echo "Working directory: $(pwd)"

# Navigate to API project
cd /home/chad/repos/witchcityrope/apps/api

# Create migration
dotnet ef migrations add RemoveFullNameFields --context ApplicationDbContext

echo ""
echo "Migration created successfully!"
echo ""
echo "Migration files location: /home/chad/repos/witchcityrope/apps/api/Migrations/"
echo ""
echo "Next steps:"
echo "1. Review the generated migration file"
echo "2. Verify Up() method drops FullName from Users and VettingApplications tables"
echo "3. Verify Down() method adds them back for rollback capability"

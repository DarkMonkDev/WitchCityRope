#!/bin/bash

echo "=== Running Simple Diagnostic Test ==="
echo "This will show you exactly what HTTP responses your endpoints are returning"
echo "and help identify why your integration tests are failing."
echo

# Make sure we're in the right directory
cd /home/chad/repos/witchcityrope/WitchCityRope

echo "Building the diagnostic test..."
dotnet build SimpleDiagnostic.csproj --configuration Release

if [ $? -eq 0 ]; then
    echo "Build successful. Running diagnostic test..."
    echo
    dotnet run --project SimpleDiagnostic.csproj --configuration Release
else
    echo "Build failed. Please check the build errors above."
    exit 1
fi
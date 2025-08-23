#!/bin/bash

# Script directory
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"

echo "Starting WitchCityRope applications..."

# Ensure PostgreSQL is running before starting applications
echo "Checking PostgreSQL status..."
"$SCRIPT_DIR/ensure-postgres.sh"
if [ $? -ne 0 ]; then
    echo "Failed to start PostgreSQL. Applications require database access."
    exit 1
fi
echo ""

# Kill any existing processes
pkill -f "dotnet.*WitchCityRope" 2>/dev/null || true
sleep 2

# Start API
echo "Starting API..."
cd /mnt/c/users/chad/source/repos/WitchCityRope/src/WitchCityRope.Api
dotnet run > /mnt/c/users/chad/source/repos/WitchCityRope/api.log 2>&1 &
API_PID=$!

# Start Web
echo "Starting Web app..."
cd /mnt/c/users/chad/source/repos/WitchCityRope/src/WitchCityRope.Web
dotnet run > /mnt/c/users/chad/source/repos/WitchCityRope/web.log 2>&1 &
WEB_PID=$!

# Wait a bit for apps to start
echo "Waiting for applications to start..."
sleep 15

# Check if they're running
echo ""
echo "Checking application status..."

# Check API
if curl -s http://localhost:5653/health > /dev/null 2>&1; then
    echo "✓ API is running on http://localhost:5653"
else
    echo "✗ API failed to start. Check api.log"
fi

# Check Web
if curl -s http://localhost:5651 > /dev/null 2>&1; then
    echo "✓ Web app is running on http://localhost:5651"
else
    echo "✗ Web app failed to start. Check web.log"
fi

echo ""
echo "Applications started. Press Ctrl+C to stop."
trap "kill $API_PID $WEB_PID 2>/dev/null; exit" INT
wait
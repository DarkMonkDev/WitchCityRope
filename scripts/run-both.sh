#!/bin/bash

# Script directory
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"

echo "Starting WitchCityRope Web and API applications..."
echo ""

# Ensure PostgreSQL is running before starting applications
echo "Checking PostgreSQL status..."
"$SCRIPT_DIR/ensure-postgres.sh"
if [ $? -ne 0 ]; then
    echo "Failed to start PostgreSQL. Applications require database access."
    exit 1
fi
echo ""

# Kill any existing processes on the ports
echo "Cleaning up any existing processes..."
pkill -f "dotnet.*WitchCityRope.Web" || true
pkill -f "dotnet.*WitchCityRope.Api" || true
sleep 2

# Start the API in background and save log
echo "Starting API on https://localhost:5653..."
cd /mnt/c/users/chad/source/repos/WitchCityRope/src/WitchCityRope.Api
dotnet run > /mnt/c/users/chad/source/repos/WitchCityRope/api.log 2>&1 &
API_PID=$!

# Wait for API to start
echo "Waiting for API to start..."
for i in {1..30}; do
    if curl -k -s https://localhost:5653/health > /dev/null 2>&1; then
        echo "API is ready!"
        break
    fi
    echo -n "."
    sleep 1
done
echo ""

# Start the Web app in background and save log
echo "Starting Web app on https://localhost:5652..."
cd /mnt/c/users/chad/source/repos/WitchCityRope/src/WitchCityRope.Web
dotnet run > /mnt/c/users/chad/source/repos/WitchCityRope/web.log 2>&1 &
WEB_PID=$!

# Wait for Web app to start
echo "Waiting for Web app to start..."
for i in {1..30}; do
    if curl -k -s https://localhost:5652 > /dev/null 2>&1; then
        echo "Web app is ready!"
        break
    fi
    echo -n "."
    sleep 1
done
echo ""

echo ""
echo "Both applications are running!"
echo "Web app: https://localhost:5652"
echo "API: https://localhost:5653"
echo ""
echo "Logs are available at:"
echo "  Web: /mnt/c/users/chad/source/repos/WitchCityRope/web.log"
echo "  API: /mnt/c/users/chad/source/repos/WitchCityRope/api.log"
echo ""
echo "Press Ctrl+C to stop both applications"

# Wait for interrupt
trap "echo 'Stopping applications...'; kill $API_PID $WEB_PID 2>/dev/null; exit" INT
wait
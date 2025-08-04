#!/bin/bash

# Script to run WitchCityRope locally (not in Docker)

echo "Starting WitchCityRope locally..."
echo "================================"

# Check if PostgreSQL is running
if ! systemctl is-active --quiet postgresql; then
    echo "PostgreSQL is not running. Starting it..."
    sudo systemctl start postgresql
fi

# Kill any existing dotnet processes on our ports
echo "Checking for existing processes..."
lsof -ti:8180 | xargs -r kill -9 2>/dev/null
lsof -ti:8280 | xargs -r kill -9 2>/dev/null

# Start API in background
echo "Starting API on http://localhost:8180..."
cd src/WitchCityRope.Api
dotnet run --urls "http://localhost:8180" &
API_PID=$!

# Wait for API to start
echo "Waiting for API to start..."
sleep 5

# Start Web
echo "Starting Web on http://localhost:8280..."
cd ../WitchCityRope.Web
export ApiUrl="http://localhost:8180"
dotnet run --urls "http://localhost:8280"

# Kill API when Web exits
kill $API_PID 2>/dev/null
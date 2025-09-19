#!/bin/bash

# Kill Local Dev Servers Script
# Purpose: Kill any local Node/npm/Vite processes to prevent conflicts with Docker

set -e

echo "🔍 Searching for local Node/npm/Vite processes..."

# Find processes by name and pattern
LOCAL_PROCESSES=$(ps aux | grep -E "(npm run dev|vite.*--port.*517|node.*vite)" | grep -v grep | grep -v docker | awk '{print $2}' || true)

if [ -z "$LOCAL_PROCESSES" ]; then
    echo "✅ No local dev servers found running"
    exit 0
fi

echo "📋 Found local dev processes:"
ps aux | grep -E "(npm run dev|vite.*--port.*517|node.*vite)" | grep -v grep | grep -v docker

echo ""
echo "🛑 Killing local dev processes..."

for PID in $LOCAL_PROCESSES; do
    if kill -9 "$PID" 2>/dev/null; then
        echo "   ✅ Killed process $PID"
    else
        echo "   ⚠️  Process $PID already gone or permission denied"
    fi
done

echo ""
echo "🔍 Checking ports 5173, 5174, 5175..."

for PORT in 5173 5174 5175; do
    if lsof -i :$PORT >/dev/null 2>&1; then
        echo "⚠️  Port $PORT still in use:"
        lsof -i :$PORT

        # Try to kill processes on these ports
        PIDS=$(lsof -ti :$PORT 2>/dev/null || true)
        if [ -n "$PIDS" ]; then
            echo "   🛑 Killing processes on port $PORT..."
            echo "$PIDS" | xargs -r kill -9 2>/dev/null || true
        fi
    else
        echo "✅ Port $PORT is free"
    fi
done

echo ""
echo "🐳 Docker container status:"
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" | grep -E "(witchcity|NAMES)"

echo ""
echo "✅ Cleanup complete. Only Docker containers should be running."
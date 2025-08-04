#!/bin/bash
# Quick restart script for the web container when hot reload fails

echo "Restarting web container..."
docker-compose restart web
echo "Web container restarted. Waiting for it to be ready..."
sleep 5

# Check if it's healthy
if docker-compose ps web | grep -q "healthy\|running"; then
    echo "✅ Web container is running"
    echo "Access at: http://localhost:5651"
else
    echo "❌ Web container may have issues. Check logs with: docker-compose logs web"
fi
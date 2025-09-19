#!/bin/bash

echo "🚀 Starting WitchCityRope Development Environment..."
echo "================================================"

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo "❌ Docker is not running. Please start Docker and try again."
    exit 1
fi

echo "✅ Building and starting services with development configuration..."
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up --build -d

echo ""
echo "⏳ Waiting for services to be ready..."
sleep 5

echo ""
echo "🎉 Development environment is ready!"
echo ""
echo "📍 Service URLs:"
echo "  - React App:    http://localhost:5173"
echo "  - API:          http://localhost:5655"
echo "  - API Swagger:  http://localhost:5655/swagger"
echo "  - PostgreSQL:   localhost:5433 (user: postgres, pass: devpass123)"
echo ""
echo "📝 Useful commands:"
echo "  - View logs:    docker-compose -f docker-compose.yml -f docker-compose.dev.yml logs -f [service]"
echo "  - Stop all:     docker-compose -f docker-compose.yml -f docker-compose.dev.yml down"
echo "  - Restart:      docker-compose -f docker-compose.yml -f docker-compose.dev.yml restart [service]"
echo "  - Shell:        docker-compose -f docker-compose.yml -f docker-compose.dev.yml exec [service] sh"
echo ""
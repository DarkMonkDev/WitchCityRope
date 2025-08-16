#!/bin/bash

echo "🚀 Starting WitchCityRope Development Environment..."
echo "================================================"

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo "❌ Docker is not running. Please start Docker and try again."
    exit 1
fi

echo "✅ Building and starting services..."
docker-compose up --build -d

echo ""
echo "⏳ Waiting for services to be ready..."
sleep 5

echo ""
echo "🎉 Development environment is ready!"
echo ""
echo "📍 Service URLs:"
echo "  - React App:    http://localhost:5173"
echo "  - API:          http://localhost:5653"
echo "  - API Swagger:  http://localhost:5653/swagger"
echo "  - PostgreSQL:   localhost:5433 (user: witchcityrope, pass: witchcityrope)"
echo ""
echo "📝 Useful commands:"
echo "  - View logs:    docker-compose logs -f [service]"
echo "  - Stop all:     docker-compose down"
echo "  - Restart:      docker-compose restart [service]"
echo "  - Shell:        docker-compose exec [service] sh"
echo ""
#!/bin/bash

echo "🚀 Starting WitchCityRope Development Environment..."
echo "================================================"

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo "❌ Docker is not running. Please start Docker and try again."
    exit 1
fi

# Detect which docker compose command to use
if docker compose version > /dev/null 2>&1; then
    DOCKER_COMPOSE="docker compose"
    echo "✅ Using docker compose plugin (modern)"
else
    DOCKER_COMPOSE="docker-compose"
    echo "✅ Using docker-compose standalone"
fi

# Check if BuildKit/buildx is available and enable if possible
if docker buildx version > /dev/null 2>&1; then
    export DOCKER_BUILDKIT=1
    export COMPOSE_DOCKER_CLI_BUILD=1
    echo "✅ BuildKit enabled for improved build performance"
    BUILD_MSG="Using BuildKit for improved build performance..."
else
    # Don't enable BuildKit if buildx is not available
    unset DOCKER_BUILDKIT
    unset COMPOSE_DOCKER_CLI_BUILD
    echo "ℹ️  BuildKit not available - using standard build"
    echo "   To enable BuildKit, install docker-buildx-plugin:"
    echo "   sudo apt-get update && sudo apt-get install docker-buildx-plugin"
    BUILD_MSG="Using standard Docker build..."
fi

echo "✅ Building and starting services with development configuration..."
echo "   $BUILD_MSG"
$DOCKER_COMPOSE -f docker-compose.yml -f docker-compose.dev.yml up --build -d

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
echo "  - View logs:    $DOCKER_COMPOSE -f docker-compose.yml -f docker-compose.dev.yml logs -f [service]"
echo "  - Stop all:     $DOCKER_COMPOSE -f docker-compose.yml -f docker-compose.dev.yml down"
echo "  - Restart:      $DOCKER_COMPOSE -f docker-compose.yml -f docker-compose.dev.yml restart [service]"
echo "  - Shell:        $DOCKER_COMPOSE -f docker-compose.yml -f docker-compose.dev.yml exec [service] sh"
echo ""
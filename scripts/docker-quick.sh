#!/bin/bash
# Quick Docker commands for Witch City Rope development

case "$1" in
  up)
    echo "Starting development environment..."
    docker-compose up -d
    echo "Services started. Access at:"
    echo "  Web: http://localhost:5651"
    echo "  API: http://localhost:5653"
    ;;
  
  down)
    echo "Stopping all services..."
    docker-compose down
    ;;
  
  restart)
    echo "Restarting services..."
    docker-compose restart
    ;;
  
  logs)
    if [ -z "$2" ]; then
      docker-compose logs -f
    else
      docker-compose logs -f "$2"
    fi
    ;;
  
  build)
    echo "Rebuilding containers..."
    docker-compose build
    ;;
  
  clean)
    echo "WARNING: This will remove all containers and volumes!"
    read -p "Are you sure? (y/N): " confirm
    if [ "$confirm" = "y" ]; then
      docker-compose down -v
      rm -rf ./data ./logs ./uploads
      echo "Clean complete."
    fi
    ;;
  
  shell)
    service=${2:-api}
    echo "Accessing $service shell..."
    docker-compose exec "$service" /bin/bash
    ;;
  
  migrate)
    echo "Running database migrations..."
    docker-compose exec api dotnet ef database update \
      --project /src/WitchCityRope.Infrastructure \
      --startup-project /src/WitchCityRope.Api
    ;;
  
  test)
    echo "Running all tests..."
    docker-compose exec api dotnet test /src
    ;;
  
  *)
    echo "Usage: $0 {up|down|restart|logs|build|clean|shell|migrate|test}"
    echo ""
    echo "Commands:"
    echo "  up       - Start all services"
    echo "  down     - Stop all services"
    echo "  restart  - Restart all services"
    echo "  logs     - View logs (optionally specify service)"
    echo "  build    - Rebuild Docker images"
    echo "  clean    - Remove all containers and data (careful!)"
    echo "  shell    - Access container shell (default: api)"
    echo "  migrate  - Run database migrations"
    echo "  test     - Run all tests"
    echo ""
    echo "Examples:"
    echo "  $0 up"
    echo "  $0 logs api"
    echo "  $0 shell web"
    exit 1
    ;;
esac
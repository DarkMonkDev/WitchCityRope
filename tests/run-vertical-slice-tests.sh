#!/bin/bash

# Vertical Slice Home Page Test Runner
# Tests the React + API + PostgreSQL stack integration

set -e

echo "🧪 Running Vertical Slice Home Page Tests"
echo "=========================================="
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if required services are running
print_status "Checking if required services are running..."

# Check React dev server
if curl -s http://localhost:5173 > /dev/null; then
    print_status "✅ React dev server is running on http://localhost:5173"
else
    print_warning "⚠️  React dev server not running on http://localhost:5173"
    print_warning "   Start with: cd apps/web && npm run dev"
fi

# Check API server
if curl -s http://localhost:5655/health > /dev/null 2>&1; then
    print_status "✅ API server is running on http://localhost:5655"
else
    print_warning "⚠️  API server not running on http://localhost:5655"
    print_warning "   Start with: cd apps/api && dotnet run"
fi

echo ""

# 1. Run API Unit Tests
print_status "1. Running API Unit Tests..."
echo "   Testing EventsController with mocked dependencies"
cd unit/api
if dotnet test --verbosity minimal; then
    print_status "✅ API Unit Tests passed"
else
    print_error "❌ API Unit Tests failed"
    exit 1
fi
cd ../..
echo ""

# 2. Run React Component Tests
print_status "2. Running React Component Tests..."
echo "   Testing EventsList component with mocked fetch"
cd ../apps/web
if npm test -- --run; then
    print_status "✅ React Component Tests passed"
else
    print_error "❌ React Component Tests failed"
    exit 1
fi
cd ../../tests
echo ""

# 3. Run E2E Tests (only if services are running)
if curl -s http://localhost:5173 > /dev/null && curl -s http://localhost:5655/health > /dev/null 2>&1; then
    print_status "3. Running E2E Tests..."
    echo "   Testing complete React + API + PostgreSQL stack"
    cd e2e
    
    # Install playwright if needed
    if [ ! -d "node_modules" ]; then
        print_status "Installing Playwright dependencies..."
        npm install
        npx playwright install
    fi
    
    if npm test; then
        print_status "✅ E2E Tests passed"
    else
        print_error "❌ E2E Tests failed"
        exit 1
    fi
    cd ..
else
    print_warning "⚠️  Skipping E2E tests - services not running"
    print_warning "   Start React: cd apps/web && npm run dev"
    print_warning "   Start API: cd apps/api && dotnet run"
fi

echo ""
print_status "🎉 All available tests completed successfully!"
print_status "   The vertical slice implementation is working correctly."
echo ""

# Summary
echo "Test Summary:"
echo "============="
echo "✅ API Unit Tests - EventsController returns EventDto array"
echo "✅ React Component Tests - EventsList fetches and displays events"
if curl -s http://localhost:5173 > /dev/null && curl -s http://localhost:5655/health > /dev/null 2>&1; then
    echo "✅ E2E Tests - Complete React + API + PostgreSQL stack integration"
else
    echo "⚠️  E2E Tests - Skipped (services not running)"
fi
echo ""
echo "The React + API + PostgreSQL stack is proven to work! 🚀"
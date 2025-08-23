#!/bin/bash

echo "=== Testing Database Connection ==="

# Function to test PostgreSQL connection
test_postgres() {
    local host=$1
    local port=$2
    local name=$3
    
    echo -n "Testing $name (localhost:$port)... "
    
    if PGPASSWORD=WitchCity2024! psql -h localhost -p $port -U postgres -d witchcityrope_db -c "SELECT version();" >/dev/null 2>&1; then
        echo "✓ Connected!"
        return 0
    else
        echo "✗ Failed"
        return 1
    fi
}

# Check which PostgreSQL instances are available
echo ""
echo "Checking PostgreSQL instances:"
echo ""

# Check Docker Compose PostgreSQL (port 5433)
if docker ps | grep -q witchcity-postgres; then
    test_postgres localhost 5433 "Docker Compose PostgreSQL"
else
    echo "Docker Compose PostgreSQL (port 5433)... ✗ Not running"
fi

# Check Aspire PostgreSQL (port 15432)
if lsof -i :15432 >/dev/null 2>&1 || netstat -tuln 2>/dev/null | grep -q :15432; then
    test_postgres localhost 15432 "Aspire PostgreSQL"
else
    echo "Aspire PostgreSQL (port 15432)... ✗ Not running"
fi

# Check standard PostgreSQL (port 5432)
if lsof -i :5432 >/dev/null 2>&1 || netstat -tuln 2>/dev/null | grep -q :5432; then
    test_postgres localhost 5432 "Standard PostgreSQL"
else
    echo "Standard PostgreSQL (port 5432)... ✗ Not running"
fi

echo ""
echo "=== Testing from Application Perspective ==="
echo ""

# Test connection string that the app would use
if [ -n "$ConnectionStrings__witchcityrope_db" ]; then
    echo "Aspire connection string is set: $ConnectionStrings__witchcityrope_db"
elif [ -n "$ConnectionStrings__DefaultConnection" ]; then
    echo "Default connection string is set: $ConnectionStrings__DefaultConnection"
else
    echo "No connection string environment variables set"
    echo "Application will use fallback: Host=localhost;Database=witchcityrope_db;Username=postgres;Password=WitchCity2024!"
fi

echo ""
echo "=== Quick Test Commands ==="
echo ""
echo "To test Docker Compose PostgreSQL:"
echo "  docker exec witchcity-postgres psql -U postgres -d witchcityrope_db -c 'SELECT NOW();'"
echo ""
echo "To connect manually:"
echo "  PGPASSWORD=WitchCity2024! psql -h localhost -p 5433 -U postgres -d witchcityrope_db"
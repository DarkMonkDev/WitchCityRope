#!/bin/bash

# Performance Test Runner for Linux/Mac

echo "WitchCityRope Performance Test Runner"
echo "====================================="
echo

# Check if dotnet is installed
if ! command -v dotnet &> /dev/null; then
    echo "ERROR: .NET SDK is not installed or not in PATH"
    exit 1
fi

# Parse arguments
TEST_TYPE=${1:-load}
ENVIRONMENT=${2:-Development}

echo "Running $TEST_TYPE tests against $ENVIRONMENT environment..."
echo

# Set environment
export ASPNETCORE_ENVIRONMENT=$ENVIRONMENT

# Restore packages
echo "Restoring packages..."
dotnet restore tests/WitchCityRope.PerformanceTests/WitchCityRope.PerformanceTests.csproj
if [ $? -ne 0 ]; then
    echo "ERROR: Package restore failed"
    exit 1
fi

# Build the project
echo "Building performance tests..."
dotnet build tests/WitchCityRope.PerformanceTests/WitchCityRope.PerformanceTests.csproj --no-restore -c Release
if [ $? -ne 0 ]; then
    echo "ERROR: Build failed"
    exit 1
fi

# Create results directory with timestamp
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
RESULTS_DIR="./test-results/$TIMESTAMP"
mkdir -p "$RESULTS_DIR"

# Run tests based on type
case $TEST_TYPE in
    load)
        echo "Running load tests..."
        dotnet test tests/WitchCityRope.PerformanceTests/WitchCityRope.PerformanceTests.csproj \
            --no-build -c Release \
            --filter "FullyQualifiedName~LoadTest" \
            --logger "console;verbosity=normal" \
            --results-directory "$RESULTS_DIR"
        ;;
    stress)
        echo "Running stress tests..."
        dotnet test tests/WitchCityRope.PerformanceTests/WitchCityRope.PerformanceTests.csproj \
            --no-build -c Release \
            --filter "FullyQualifiedName~StressTest" \
            --logger "console;verbosity=normal" \
            --results-directory "$RESULTS_DIR"
        ;;
    all)
        echo "Running all performance tests..."
        dotnet test tests/WitchCityRope.PerformanceTests/WitchCityRope.PerformanceTests.csproj \
            --no-build -c Release \
            --logger "console;verbosity=normal" \
            --results-directory "$RESULTS_DIR"
        ;;
    *)
        echo "ERROR: Unknown test type: $TEST_TYPE"
        echo "Valid options: load, stress, all"
        exit 1
        ;;
esac

if [ $? -ne 0 ]; then
    echo
    echo "Performance tests failed!"
    exit 1
else
    echo
    echo "Performance tests completed successfully!"
    echo "Results saved to: $RESULTS_DIR"
    echo "Check the reports folder for detailed results."
fi

echo
echo "Done."
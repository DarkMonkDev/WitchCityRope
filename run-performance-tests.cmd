@echo off
REM Performance Test Runner for Windows

echo WitchCityRope Performance Test Runner
echo =====================================
echo.

REM Check if dotnet is installed
where dotnet >nul 2>nul
if %errorlevel% neq 0 (
    echo ERROR: .NET SDK is not installed or not in PATH
    exit /b 1
)

REM Parse arguments
set TEST_TYPE=%1
set ENVIRONMENT=%2

if "%TEST_TYPE%"=="" set TEST_TYPE=load
if "%ENVIRONMENT%"=="" set ENVIRONMENT=Development

echo Running %TEST_TYPE% tests against %ENVIRONMENT% environment...
echo.

REM Set environment
set ASPNETCORE_ENVIRONMENT=%ENVIRONMENT%

REM Restore packages
echo Restoring packages...
dotnet restore tests\WitchCityRope.PerformanceTests\WitchCityRope.PerformanceTests.csproj
if %errorlevel% neq 0 (
    echo ERROR: Package restore failed
    exit /b 1
)

REM Build the project
echo Building performance tests...
dotnet build tests\WitchCityRope.PerformanceTests\WitchCityRope.PerformanceTests.csproj --no-restore -c Release
if %errorlevel% neq 0 (
    echo ERROR: Build failed
    exit /b 1
)

REM Run tests based on type
if "%TEST_TYPE%"=="load" (
    echo Running load tests...
    dotnet test tests\WitchCityRope.PerformanceTests\WitchCityRope.PerformanceTests.csproj --no-build -c Release --filter "FullyQualifiedName~LoadTest" --logger "console;verbosity=normal" --results-directory .\test-results\%date:~-4,4%%date:~-10,2%%date:~-7,2%_%time:~0,2%%time:~3,2%
) else if "%TEST_TYPE%"=="stress" (
    echo Running stress tests...
    dotnet test tests\WitchCityRope.PerformanceTests\WitchCityRope.PerformanceTests.csproj --no-build -c Release --filter "FullyQualifiedName~StressTest" --logger "console;verbosity=normal" --results-directory .\test-results\%date:~-4,4%%date:~-10,2%%date:~-7,2%_%time:~0,2%%time:~3,2%
) else if "%TEST_TYPE%"=="all" (
    echo Running all performance tests...
    dotnet test tests\WitchCityRope.PerformanceTests\WitchCityRope.PerformanceTests.csproj --no-build -c Release --logger "console;verbosity=normal" --results-directory .\test-results\%date:~-4,4%%date:~-10,2%%date:~-7,2%_%time:~0,2%%time:~3,2%
) else (
    echo ERROR: Unknown test type: %TEST_TYPE%
    echo Valid options: load, stress, all
    exit /b 1
)

if %errorlevel% neq 0 (
    echo.
    echo Performance tests failed!
    exit /b 1
) else (
    echo.
    echo Performance tests completed successfully!
    echo Check the reports folder for detailed results.
)

echo.
echo Done.
pause
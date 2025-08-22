@echo off
REM WitchCityRope Test Runner Script for Windows
REM This script runs all tests and generates coverage reports

echo ===========================
echo WitchCityRope Test Runner
echo ===========================

REM Clean previous coverage reports
echo Cleaning previous coverage reports...
if exist coverage rmdir /s /q coverage
mkdir coverage

REM Build the solution first
echo.
echo Building solution...
dotnet build --configuration Release
if errorlevel 1 (
    echo Build failed! Please fix build errors before running tests.
    exit /b 1
)

REM Run tests for each project
set all_passed=1

echo.
echo Running Core Tests...
dotnet test "tests\WitchCityRope.Core.Tests" ^
    --logger "console;verbosity=minimal" ^
    --collect:"XPlat Code Coverage" ^
    /p:CoverletOutputFormat=opencover ^
    /p:CoverletOutput=..\..\coverage\
if errorlevel 1 set all_passed=0

echo.
echo Running API Tests...
dotnet test "tests\WitchCityRope.Api.Tests" ^
    --logger "console;verbosity=minimal" ^
    --collect:"XPlat Code Coverage" ^
    /p:CoverletOutputFormat=opencover ^
    /p:CoverletOutput=..\..\coverage\ ^
    /p:MergeWith=..\..\coverage\coverage.json
if errorlevel 1 set all_passed=0

echo.
echo Running Infrastructure Tests...
dotnet test "tests\WitchCityRope.Infrastructure.Tests" ^
    --logger "console;verbosity=minimal" ^
    --collect:"XPlat Code Coverage" ^
    /p:CoverletOutputFormat=opencover ^
    /p:CoverletOutput=..\..\coverage\ ^
    /p:MergeWith=..\..\coverage\coverage.json
if errorlevel 1 set all_passed=0

echo.
echo Running Web Tests...
dotnet test "tests\WitchCityRope.Web.Tests" ^
    --logger "console;verbosity=minimal" ^
    --collect:"XPlat Code Coverage" ^
    /p:CoverletOutputFormat=opencover ^
    /p:CoverletOutput=..\..\coverage\ ^
    /p:MergeWith=..\..\coverage\coverage.json
if errorlevel 1 set all_passed=0

REM Generate coverage report
echo.
echo Generating coverage report...
where reportgenerator >nul 2>&1
if %errorlevel%==0 (
    reportgenerator ^
        -reports:"coverage\**\coverage.opencover.xml" ^
        -targetdir:"coverage\report" ^
        -reporttypes:"HtmlInline_AzurePipelines;Cobertura"
    echo Coverage report generated in coverage\report\index.html
) else (
    echo ReportGenerator not found. Install it with:
    echo dotnet tool install -g dotnet-reportgenerator-globaltool
)

REM Summary
echo.
echo ============================
if %all_passed%==1 (
    echo All tests passed!
    exit /b 0
) else (
    echo Some tests failed!
    exit /b 1
)
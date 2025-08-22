@echo off
REM WitchCityRope Test Runner with Coverage
REM This script runs all tests and generates a coverage report

echo ================================================
echo WitchCityRope Test Runner with Coverage
echo ================================================

REM Check if dotnet is installed
where dotnet >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo Error: dotnet CLI is not installed
    exit /b 1
)

REM Check .NET version
for /f "tokens=*" %%i in ('dotnet --version') do set DOTNET_VERSION=%%i
echo Using .NET SDK: %DOTNET_VERSION%

REM Clean previous test results
echo.
echo Cleaning previous test results...
if exist TestResults rd /s /q TestResults
if exist CoverageReport rd /s /q CoverageReport

REM Restore packages
echo.
echo Restoring packages...
dotnet restore
if %ERRORLEVEL% NEQ 0 (
    echo Failed to restore packages
    exit /b 1
)

REM Build solution
echo.
echo Building solution...
dotnet build --no-restore --configuration Release
if %ERRORLEVEL% NEQ 0 (
    echo Build failed
    exit /b 1
)

REM Run tests with coverage
echo.
echo Running tests with coverage...
dotnet test --no-build --configuration Release ^
    --logger "trx;LogFileName=test-results.trx" ^
    --logger "console;verbosity=normal" ^
    --collect:"XPlat Code Coverage" ^
    --results-directory ./TestResults ^
    -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo Some tests failed!
    exit /b 1
) else (
    echo.
    echo All tests passed!
)

REM Check if reportgenerator is installed
where reportgenerator >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo Installing ReportGenerator tool...
    dotnet tool install -g dotnet-reportgenerator-globaltool
)

REM Generate coverage report
echo.
echo Generating coverage report...
reportgenerator ^
    -reports:"TestResults/**/coverage.opencover.xml" ^
    -targetdir:"CoverageReport" ^
    -reporttypes:"Html;Cobertura;TextSummary;Badges" ^
    -verbosity:"Info" ^
    -title:"WitchCityRope Coverage Report" ^
    -tag:"%date:~10,4%%date:~4,2%%date:~7,2%_%time:~0,2%%time:~3,2%%time:~6,2%"

REM Display coverage summary
echo.
echo Coverage Summary:
echo ================
type CoverageReport\Summary.txt

REM Parse coverage values
for /f "tokens=3" %%a in ('findstr /C:"Line coverage:" CoverageReport\Summary.txt') do set LINE_COVERAGE=%%a
for /f "tokens=3" %%a in ('findstr /C:"Branch coverage:" CoverageReport\Summary.txt') do set BRANCH_COVERAGE=%%a

REM Remove % sign
set LINE_COVERAGE=%LINE_COVERAGE:~0,-1%
set BRANCH_COVERAGE=%BRANCH_COVERAGE:~0,-1%

REM Check thresholds (60%)
set THRESHOLD=60
if %LINE_COVERAGE% LSS %THRESHOLD% (
    echo.
    echo Warning: Line coverage ^(%LINE_COVERAGE%%%^) is below threshold ^(%THRESHOLD%%%^)
)

if %BRANCH_COVERAGE% LSS %THRESHOLD% (
    echo.
    echo Warning: Branch coverage ^(%BRANCH_COVERAGE%%%^) is below threshold ^(%THRESHOLD%%%^)
)

REM Open report in browser
echo.
echo Opening coverage report in browser...
start CoverageReport\index.html

echo.
echo Test run completed successfully!
echo Coverage report: CoverageReport\index.html
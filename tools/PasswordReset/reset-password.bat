@echo off
REM Password Reset Tool for WitchCityRope
REM Usage: reset-password.bat <email> <new-password>

if "%~2"=="" (
    echo Usage: %0 ^<email^> ^<new-password^>
    echo Example: %0 admin@witchcityrope.com NewPassword123!
    exit /b 1
)

set EMAIL=%1
set PASSWORD=%2

echo Resetting password for user: %EMAIL%
dotnet run -- %EMAIL% %PASSWORD%
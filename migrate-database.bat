@echo off
REM Database Migration Script for AmplePack (Windows)
REM This script handles database setup and migrations

echo ??? AmplePack Database Migration Script
echo ======================================

REM Configuration
set DB_CONNECTION=%1
if "%DB_CONNECTION%"=="" set DB_CONNECTION=Data Source=AmplePack.sqlite

set ENVIRONMENT=%2
if "%ENVIRONMENT%"=="" set ENVIRONMENT=Development

echo Environment: %ENVIRONMENT%
echo Database: %DB_CONNECTION%

REM Check if dotnet ef tools are installed
echo Checking Entity Framework Core tools...
dotnet tool list -g | findstr "dotnet-ef" >nul
if %errorlevel% neq 0 (
    echo ?? Installing Entity Framework Core tools...
    dotnet tool install --global dotnet-ef
    if %errorlevel% neq 0 (
        echo ? Failed to install EF Core tools
        exit /b 1
    )
) else (
    echo ? Entity Framework Core tools found
)

REM Navigate to project directory
cd AmplePack

REM Check if migrations exist
if not exist "Migrations" (
    echo ?? Creating initial migration...
    dotnet ef migrations add InitialCreate
    
    if %errorlevel% neq 0 (
        echo ? Failed to create migration
        exit /b 1
    )
    
    echo ? Initial migration created
)

REM Apply migrations
echo ?? Applying database migrations...
dotnet ef database update

if %errorlevel% equ 0 (
    echo ? Database migrations applied successfully
    
    REM Seed initial data if in development
    if "%ENVIRONMENT%"=="Development" (
        echo ?? Database will be seeded with initial data on first run
    )
    
    echo.
    echo ?? Database setup complete!
    echo.
    echo Database Connection: %DB_CONNECTION%
    echo Environment: %ENVIRONMENT%
    
) else (
    echo ? Migration failed
    exit /b 1
)

cd ..
pause
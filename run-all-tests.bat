@echo off
REM Test Runner Script - Runs all tests locally (Windows)
REM Usage: run-all-tests.bat [--quick] [--help]

setlocal enabledelayedexpansion

REM Default values
set QUICK_MODE=false
set BASE_URL=https://localhost:5001
for /f "tokens=2 delims==" %%a in ('wmic OS Get localdatetime /value') do set "dt=%%a"
set OUTPUT_DIR=test-results-%dt:~0,4%%dt:~4,2%%dt:~6,2%_%dt:~8,2%%dt:~10,2%%dt:~12,2%

REM Parse arguments
:parse_args
if "%~1"=="--quick" (
    set QUICK_MODE=true
    shift
    goto parse_args
)
if "%~1"=="--url" (
    set BASE_URL=%~2
    shift
    shift
    goto parse_args
)
if "%~1"=="--help" (
    echo Usage: %0 [--quick] [--url BASE_URL]
    echo   --quick   Run only unit and integration tests (skip UI, performance, security)
    echo   --url     Base URL for application (default: https://localhost:5001)
    exit /b 0
)
if not "%~1"=="" (
    echo Unknown option: %1
    exit /b 1
)

REM Create output directory
if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"

echo ========================================
echo AmplePack Comprehensive Test Suite
echo ========================================
if "%QUICK_MODE%"=="true" (
    echo Mode: Quick
) else (
    echo Mode: Full
)
echo Base URL: %BASE_URL%
echo Output Directory: %OUTPUT_DIR%
echo ========================================

REM Check prerequisites
echo.
echo Checking prerequisites...

REM Check .NET
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ? .NET is not installed
    exit /b 1
) else (
    for /f %%i in ('dotnet --version') do set DOTNET_VERSION=%%i
    echo ? .NET !DOTNET_VERSION! found
)

REM Check if solution builds
echo.
echo Building solution...
dotnet build --configuration Release
if errorlevel 1 (
    echo ? Solution build failed
    exit /b 1
) else (
    echo ? Solution builds successfully
)

REM Start application in background
echo.
echo Starting application for testing...
cd AmplePack
start /b dotnet run --configuration Release --urls "%BASE_URL%"
cd ..

REM Wait for application to start
echo Waiting for application to start...
set timeout=60
:wait_loop
curl -s -k "%BASE_URL%" >nul 2>&1
if not errorlevel 1 goto app_started
timeout /t 2 /nobreak >nul
set /a timeout-=2
if %timeout% gtr 0 goto wait_loop

echo ? Application failed to start within 60 seconds
exit /b 1

:app_started
echo ? Application started successfully

REM Initialize failed tests counter
set FAILED_COUNT=0

REM 1. Unit Tests
echo.
echo Running Unit Tests...
echo ----------------------------------------
dotnet test AmplePack.Tests/AmplePack.Tests.csproj --configuration Release --logger "trx;LogFileName=unit-tests.trx" --results-directory "%OUTPUT_DIR%" --collect:"XPlat Code Coverage"
if errorlevel 1 (
    echo ? Unit Tests failed
    set /a FAILED_COUNT+=1
) else (
    echo ? Unit Tests completed successfully
)

REM 2. Integration Tests
echo.
echo Running Integration Tests...
echo ----------------------------------------
dotnet test AmplePack.Tests/AmplePack.Tests.csproj --configuration Release --filter Category=Integration --logger "trx;LogFileName=integration-tests.trx" --results-directory "%OUTPUT_DIR%"
if errorlevel 1 (
    echo ? Integration Tests failed
    set /a FAILED_COUNT+=1
) else (
    echo ? Integration Tests completed successfully
)

REM Skip remaining tests in quick mode
if "%QUICK_MODE%"=="true" goto summary

REM 3. UI Tests (if Playwright is available)
echo.
echo Running UI Tests...
echo ----------------------------------------
if exist "AmplePack.UITests\bin\Debug\net9.0\playwright.ps1" (
    cd AmplePack.UITests
    dotnet test --configuration Release --logger "trx;LogFileName=ui-tests.trx" --results-directory "..\%OUTPUT_DIR%"
    if errorlevel 1 (
        echo ? UI Tests failed
        set /a FAILED_COUNT+=1
    ) else (
        echo ? UI Tests completed successfully
    )
    cd ..
) else (
    echo ?? Playwright not found, skipping UI tests
)

REM 4. Performance Tests (if k6 is available)
echo.
echo Running Performance Tests...
echo ----------------------------------------
k6 version >nul 2>&1
if not errorlevel 1 (
    cd PerformanceTests
    call run-performance-tests.bat --url "%BASE_URL%" --output "..\%OUTPUT_DIR%"
    if errorlevel 1 (
        echo ? Performance Tests failed
        set /a FAILED_COUNT+=1
    ) else (
        echo ? Performance Tests completed successfully
    )
    cd ..
) else (
    echo ?? k6 not found, skipping performance tests
)

REM 5. Security Tests (if available)
echo.
echo Running Security Tests...
echo ----------------------------------------
docker version >nul 2>&1
if not errorlevel 1 (
    echo ?? Security tests require manual setup, skipping for now
) else (
    echo ?? Docker not found, skipping security tests
)

:summary
REM Kill the application process
taskkill /f /im dotnet.exe >nul 2>&1

echo.
echo ========================================
echo Test Execution Summary
echo ========================================
echo Output Directory: %OUTPUT_DIR%
echo.

if %FAILED_COUNT% equ 0 (
    echo ? All tests completed successfully!
    echo.
    echo Generated artifacts:
    dir "%OUTPUT_DIR%" /b
    exit /b 0
) else (
    echo ? %FAILED_COUNT% test suite(s) failed
    echo.
    echo Check the output directory for detailed results: %OUTPUT_DIR%
    exit /b 1
)
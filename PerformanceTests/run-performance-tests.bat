@echo off
REM Performance Testing Script for AmplePack (Windows)
REM Requires k6 to be installed: https://k6.io/docs/getting-started/installation/

setlocal enabledelayedexpansion

REM Default values
set BASE_URL=https://localhost:5001
set OUTPUT_DIR=test-results
for /f "tokens=2 delims==" %%a in ('wmic OS Get localdatetime /value') do set "dt=%%a"
set TIMESTAMP=%dt:~0,4%%dt:~4,2%%dt:~6,2%_%dt:~8,2%%dt:~10,2%%dt:~12,2%

REM Parse command line arguments
:parse_args
if "%~1"=="--url" (
    set BASE_URL=%~2
    shift
    shift
    goto parse_args
)
if "%~1"=="--output" (
    set OUTPUT_DIR=%~2
    shift
    shift
    goto parse_args
)
if "%~1"=="--help" (
    echo Usage: %0 [--url BASE_URL] [--output OUTPUT_DIR]
    echo   --url     Base URL of the application (default: https://localhost:5001)
    echo   --output  Output directory for test results (default: test-results)
    exit /b 0
)
if not "%~1"=="" (
    echo Unknown option: %1
    exit /b 1
)

REM Create output directory
if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"

echo ========================================
echo AmplePack Performance Testing Suite
echo ========================================
echo Base URL: %BASE_URL%
echo Output Directory: %OUTPUT_DIR%
echo Timestamp: %TIMESTAMP%
echo ========================================

REM Check if k6 is installed
k6 version >nul 2>&1
if errorlevel 1 (
    echo ? k6 is not installed. Please install k6 from https://k6.io/docs/getting-started/installation/
    exit /b 1
)

REM Check if application is accessible
echo Checking if application is accessible at %BASE_URL%...
curl -s -o nul -w "%%{http_code}" "%BASE_URL%" | findstr "200 302 301" >nul
if errorlevel 1 (
    echo ? Application is not accessible at %BASE_URL%
    echo Please ensure the application is running and the URL is correct
    exit /b 1
) else (
    echo ? Application is accessible
)

REM Function to run k6 test
call :run_k6_test "load_test" "load-test.js"
call :run_k6_test "spike_test" "spike-test.js"

echo.
echo ========================================
echo Performance Testing Completed
echo ========================================
echo Results saved to: %OUTPUT_DIR%
echo.
echo Summary files:
dir "%OUTPUT_DIR%\*%TIMESTAMP%*" /b

echo.
echo To view detailed results:
echo   - JSON files can be analyzed with jq or imported into analysis tools
echo   - CSV files can be opened in Excel or imported into monitoring systems

goto :eof

:run_k6_test
set test_name=%~1
set test_file=%~2
set output_file=%OUTPUT_DIR%\%test_name%_%TIMESTAMP%

echo.
echo Running %test_name%...
echo ----------------------------------------

k6 run --env BASE_URL="%BASE_URL%" --out json="%output_file%.json" --out csv="%output_file%.csv" "%test_file%"

if errorlevel 1 (
    echo ? %test_name% failed
) else (
    echo ? %test_name% completed successfully
)

goto :eof
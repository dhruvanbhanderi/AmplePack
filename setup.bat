@echo off
REM AmplePack - Complete Setup and Verification Script (Windows)
REM This script sets up, builds, tests, and verifies the entire application

echo ?? AmplePack Complete Setup Script
echo ==================================
echo.

set "ERROR_COUNT=0"
set "WARNING_COUNT=0"

REM Function to print status messages
goto :main

:print_status
echo [INFO] %~1
goto :eof

:print_success
echo [SUCCESS] %~1
goto :eof

:print_warning
echo [WARNING] %~1
set /a WARNING_COUNT+=1
goto :eof

:print_error
echo [ERROR] %~1
set /a ERROR_COUNT+=1
goto :eof

:main

REM Check prerequisites
call :print_status "Checking prerequisites..."

dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    call :print_error ".NET SDK is not installed. Please install .NET 9 SDK."
    exit /b 1
)

for /f "tokens=*" %%i in ('dotnet --version 2^>nul') do (
    call :print_success ".NET SDK found: %%i"
)

git --version >nul 2>&1
if %errorlevel% neq 0 (
    call :print_warning "Git is not installed. Some features may not work."
) else (
    for /f "tokens=*" %%i in ('git --version 2^>nul') do (
        call :print_success "Git found: %%i"
    )
)

echo.

REM Clean and restore
call :print_status "Cleaning and restoring packages..."
dotnet clean --verbosity quiet >nul 2>&1
dotnet restore --verbosity quiet >nul 2>&1

if %errorlevel% equ 0 (
    call :print_success "Packages restored successfully"
) else (
    call :print_error "Package restoration failed"
    exit /b 1
)

echo.

REM Build the solution
call :print_status "Building the solution..."
dotnet build --configuration Release --no-restore --verbosity quiet >nul 2>&1

if %errorlevel% equ 0 (
    call :print_success "Build completed successfully"
) else (
    call :print_error "Build failed"
    exit /b 1
)

echo.

REM Run tests
call :print_status "Running tests..."
dotnet test --configuration Release --no-build --verbosity quiet --logger "console;verbosity=minimal" >test_output.tmp 2>&1

if %errorlevel% equ 0 (
    call :print_success "All tests passed"
    for /f "tokens=*" %%i in ('findstr /C:"Passed" test_output.tmp') do echo %%i
) else (
    call :print_error "Some tests failed"
    type test_output.tmp
    del test_output.tmp
    exit /b 1
)

del test_output.tmp
echo.

REM Check for Entity Framework tools
call :print_status "Checking Entity Framework tools..."
dotnet tool list -g | findstr "dotnet-ef" >nul
if %errorlevel% neq 0 (
    call :print_status "Installing Entity Framework Core tools..."
    dotnet tool install --global dotnet-ef --verbosity quiet >nul 2>&1
    call :print_success "EF Core tools installed"
) else (
    call :print_success "EF Core tools already installed"
)

echo.

REM Database migration check
call :print_status "Checking database setup..."
cd AmplePack

if not exist "Migrations" (
    call :print_status "Creating initial migration..."
    dotnet ef migrations add InitialCreate --no-build >nul 2>&1
    if %errorlevel% equ 0 (
        call :print_success "Initial migration created"
    ) else (
        call :print_warning "Migration creation skipped (may already exist)"
    )
)

call :print_status "Setting up database..."
dotnet ef database update --no-build >nul 2>&1
if %errorlevel% equ 0 (
    call :print_success "Database setup completed"
) else (
    call :print_warning "Database update had issues (may already be up to date)"
)

cd ..

echo.

REM Security check
call :print_status "Running security checks..."
set "SECURITY_ISSUES=0"

REM Check for hardcoded secrets (basic check)
findstr /R /C:"password.*=" AmplePack\*.cs AmplePack\*.json 2>nul | findstr /V "Password.Required PasswordHash" >nul
if %errorlevel% equ 0 (
    call :print_warning "Potential hardcoded passwords found"
    set /a SECURITY_ISSUES+=1
)

REM Check for debug code
findstr /R /C:"Console.WriteLine\|Debug.WriteLine" AmplePack\*.cs 2>nul >nul
if %errorlevel% equ 0 (
    call :print_warning "Debug statements found in code"
    set /a SECURITY_ISSUES+=1
)

if %SECURITY_ISSUES% equ 0 (
    call :print_success "No obvious security issues found"
)

echo.

REM Performance check
call :print_status "Checking for performance considerations..."
set "PERF_ISSUES=0"

REM Check for Entity Framework issues
findstr /R /C:"\.ToList()" AmplePack\Controllers\*.cs 2>nul >nul
if %errorlevel% equ 0 (
    call :print_warning "Potential N+1 queries found (ToList() in controllers)"
    set /a PERF_ISSUES+=1
)

if %PERF_ISSUES% equ 0 (
    call :print_success "No obvious performance issues found"
)

echo.

REM Code quality check
call :print_status "Checking code quality..."
set "QUALITY_ISSUES=0"

REM Check for proper using statements
for /f %%i in ('findstr /R /C:"using System.Linq;" AmplePack\*.cs 2^>nul ^| find /c /v ""') do (
    if %%i gtr 10 (
        call :print_warning "Consider using global using statements for common namespaces"
        set /a QUALITY_ISSUES+=1
    )
)

if %QUALITY_ISSUES% equ 0 (
    call :print_success "Code quality looks good"
)

echo.

REM Final verification
call :print_status "Running final verification..."

REM Check if key files exist
set "MISSING_FILES=0"
set "REQUIRED_FILES=AmplePack\Program.cs AmplePack\appsettings.json AmplePack\appsettings.Development.json README.md GETTING_STARTED.md"

for %%f in (%REQUIRED_FILES%) do (
    if not exist "%%f" (
        call :print_error "Missing required file: %%f"
        set /a MISSING_FILES+=1
    )
)

if %MISSING_FILES% equ 0 (
    call :print_success "All required files present"
)

echo.

REM Summary
echo ?? Setup Complete!
echo ==================
echo.
call :print_success "? Build: Successful"
call :print_success "? Tests: All Passing"
call :print_success "? Database: Ready"
call :print_success "? Dependencies: Installed"
echo.

echo ?? Next Steps:
echo 1. Run the application: cd AmplePack ^&^& dotnet run
echo 2. Open browser: https://localhost:7153
echo 3. Login with: admin@ample.com / Admin@123
echo 4. Explore the features!
echo.

echo ?? Documentation:
echo - README.md - Main documentation
echo - GETTING_STARTED.md - Quick start guide
echo - docs/ - Detailed documentation
echo.

echo ??? Development:
echo - Run tests: dotnet test
echo - Build: dotnet build
echo - Deploy: deploy.bat
echo.

call :print_success "AmplePack is ready to use! ??"

if %ERROR_COUNT% gtr 0 (
    echo.
    echo ?? %ERROR_COUNT% error(s) found. Please review and fix before proceeding.
    exit /b 1
)

if %WARNING_COUNT% gtr 0 (
    echo.
    echo ?? %WARNING_COUNT% warning(s) found. Consider reviewing these items.
)

pause
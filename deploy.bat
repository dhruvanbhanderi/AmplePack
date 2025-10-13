@echo off
REM AmplePack Deployment Script for Windows
REM This script prepares the application for production deployment

echo ?? Starting AmplePack deployment preparation...

REM Check if dotnet is available
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ? .NET SDK is not installed. Please install .NET 9 SDK.
    exit /b 1
)

echo ? .NET SDK found

REM Clean previous builds
echo ?? Cleaning previous builds...
dotnet clean
if exist "AmplePack\bin" rmdir /s /q "AmplePack\bin"
if exist "AmplePack\obj" rmdir /s /q "AmplePack\obj"
if exist "AmplePack.Tests\bin" rmdir /s /q "AmplePack.Tests\bin"
if exist "AmplePack.Tests\obj" rmdir /s /q "AmplePack.Tests\obj"

REM Restore packages
echo ?? Restoring NuGet packages...
dotnet restore
if %errorlevel% neq 0 (
    echo ? Package restore failed.
    exit /b 1
)

REM Build the solution
echo ?? Building the solution...
dotnet build --configuration Release --no-restore
if %errorlevel% neq 0 (
    echo ? Build failed. Please fix compilation errors.
    exit /b 1
)

echo ? Build successful

REM Run tests
echo ?? Running tests...
dotnet test --configuration Release --no-build --verbosity minimal
if %errorlevel% neq 0 (
    echo ? Tests failed. Please fix failing tests.
    exit /b 1
)

echo ? All tests passed

REM Publish for deployment
echo ?? Publishing application...
dotnet publish AmplePack\AmplePack.csproj --configuration Release --output .\publish --self-contained true --runtime win-x64
if %errorlevel% neq 0 (
    echo ? Publish failed.
    exit /b 1
)

echo ? Application published successfully

REM Create deployment package
echo ?? Creating deployment package...
if exist "AmplePack-Deployment.zip" del "AmplePack-Deployment.zip"
powershell Compress-Archive -Path ".\publish\*" -DestinationPath ".\AmplePack-Deployment.zip"

echo ?? Deployment preparation complete!
echo.
echo ?? Published files are in: .\publish\
echo ?? Deployment package: .\AmplePack-Deployment.zip
echo.
echo ?? Next steps:
echo 1. Copy the published files to your production server
echo 2. Configure the production database connection string in appsettings.Production.json
echo 3. Set up IIS or configure Kestrel for production
echo 4. Configure SSL certificates
echo 5. Run database migrations on production database
echo.
echo ?? For detailed deployment instructions, see the README.md file

pause
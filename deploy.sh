#!/bin/bash

# AmplePack Deployment Script
# This script prepares the application for production deployment

echo "?? Starting AmplePack deployment preparation..."

# Function to check if a command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Check prerequisites
echo "?? Checking prerequisites..."

if ! command_exists dotnet; then
    echo "? .NET SDK is not installed. Please install .NET 9 SDK."
    exit 1
fi

echo "? .NET SDK found"

# Clean previous builds
echo "?? Cleaning previous builds..."
dotnet clean
rm -rf AmplePack/bin AmplePack/obj AmplePack.Tests/bin AmplePack.Tests/obj

# Restore packages
echo "?? Restoring NuGet packages..."
dotnet restore

# Build the solution
echo "?? Building the solution..."
dotnet build --configuration Release --no-restore

if [ $? -ne 0 ]; then
    echo "? Build failed. Please fix compilation errors."
    exit 1
fi

echo "? Build successful"

# Run tests
echo "?? Running tests..."
dotnet test --configuration Release --no-build --verbosity minimal

if [ $? -ne 0 ]; then
    echo "? Tests failed. Please fix failing tests."
    exit 1
fi

echo "? All tests passed"

# Publish for deployment
echo "?? Publishing application..."
dotnet publish AmplePack/AmplePack.csproj --configuration Release --output ./publish --self-contained true --runtime win-x64

if [ $? -ne 0 ]; then
    echo "? Publish failed."
    exit 1
fi

echo "? Application published successfully"

# Create deployment package
echo "?? Creating deployment package..."
cd publish
tar -czf ../AmplePack-Deployment.tar.gz .
cd ..

echo "?? Deployment preparation complete!"
echo ""
echo "?? Published files are in: ./publish/"
echo "?? Deployment package: ./AmplePack-Deployment.tar.gz"
echo ""
echo "?? Next steps:"
echo "1. Copy the published files to your production server"
echo "2. Configure the production database connection string in appsettings.Production.json"
echo "3. Set up IIS or configure Kestrel for production"
echo "4. Configure SSL certificates"
echo "5. Run database migrations on production database"
echo ""
echo "?? For detailed deployment instructions, see the README.md file"
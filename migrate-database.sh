#!/bin/bash

# Database Migration Script for AmplePack
# This script handles database setup and migrations

echo "??? AmplePack Database Migration Script"
echo "======================================"

# Configuration
DB_CONNECTION=${1:-"Data Source=AmplePack.sqlite"}
ENVIRONMENT=${2:-"Development"}

echo "Environment: $ENVIRONMENT"
echo "Database: $DB_CONNECTION"

# Function to check if dotnet ef tools are installed
check_ef_tools() {
    if ! dotnet tool list -g | grep -q "dotnet-ef"; then
        echo "?? Installing Entity Framework Core tools..."
        dotnet tool install --global dotnet-ef
    else
        echo "? Entity Framework Core tools found"
    fi
}

# Check EF tools
check_ef_tools

# Navigate to project directory
cd AmplePack

# Check if migrations exist
if [ ! -d "Migrations" ]; then
    echo "?? Creating initial migration..."
    dotnet ef migrations add InitialCreate
    
    if [ $? -ne 0 ]; then
        echo "? Failed to create migration"
        exit 1
    fi
    
    echo "? Initial migration created"
fi

# Apply migrations
echo "?? Applying database migrations..."
dotnet ef database update

if [ $? -eq 0 ]; then
    echo "? Database migrations applied successfully"
    
    # Seed initial data if in development
    if [ "$ENVIRONMENT" = "Development" ]; then
        echo "?? Database will be seeded with initial data on first run"
    fi
    
    echo ""
    echo "?? Database setup complete!"
    echo ""
    echo "Database Connection: $DB_CONNECTION"
    echo "Environment: $ENVIRONMENT"
    
else
    echo "? Migration failed"
    exit 1
fi

cd ..
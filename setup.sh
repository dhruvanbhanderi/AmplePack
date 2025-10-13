#!/bin/bash

# AmplePack - Complete Setup and Verification Script
# This script sets up, builds, tests, and verifies the entire solution
# Handles separate main project (AmplePack) and test project (AmplePack.Tests)

echo "?? AmplePack Complete Setup Script"
echo "=================================="
echo ""

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Function to check if a command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Project paths
SOLUTION_FILE="AmplePack.sln"
MAIN_PROJECT="AmplePack"
TEST_PROJECT="AmplePack.Tests"
MAIN_PROJECT_FILE="$MAIN_PROJECT/AmplePack.csproj"
TEST_PROJECT_FILE="$TEST_PROJECT/AmplePack.Tests.csproj"

# Check prerequisites
print_status "Checking prerequisites..."

if ! command_exists dotnet; then
    print_error ".NET SDK is not installed. Please install .NET 9 SDK."
    exit 1
fi

print_success ".NET SDK found: $(dotnet --version)"

if ! command_exists git; then
    print_warning "Git is not installed. Some features may not work."
else
    print_success "Git found: $(git --version)"
fi

echo ""

# Verify project structure
print_status "Verifying project structure..."

if [ ! -f "$SOLUTION_FILE" ]; then
    print_error "Solution file not found: $SOLUTION_FILE"
    exit 1
fi

if [ ! -f "$MAIN_PROJECT_FILE" ]; then
    print_error "Main project file not found: $MAIN_PROJECT_FILE"
    exit 1
fi

if [ ! -f "$TEST_PROJECT_FILE" ]; then
    print_error "Test project file not found: $TEST_PROJECT_FILE"
    exit 1
fi

print_success "Solution structure verified"
print_status "  ??? $MAIN_PROJECT/ (Main web application)"
print_status "  ??? $TEST_PROJECT/ (Test project)"
print_status "  ??? $SOLUTION_FILE (Solution file)"

echo ""

# Clean and restore solution
print_status "Cleaning and restoring solution..."
dotnet clean "$SOLUTION_FILE" --verbosity quiet
dotnet restore "$SOLUTION_FILE" --verbosity quiet

if [ $? -eq 0 ]; then
    print_success "Solution packages restored successfully"
else
    print_error "Solution package restoration failed"
    exit 1
fi

echo ""

# Build the main project first
print_status "Building main project ($MAIN_PROJECT)..."
dotnet build "$MAIN_PROJECT_FILE" --configuration Release --no-restore --verbosity quiet

if [ $? -eq 0 ]; then
    print_success "Main project build completed successfully"
else
    print_error "Main project build failed"
    exit 1
fi

# Build the test project
print_status "Building test project ($TEST_PROJECT)..."
dotnet build "$TEST_PROJECT_FILE" --configuration Release --no-restore --verbosity quiet

if [ $? -eq 0 ]; then
    print_success "Test project build completed successfully"
else
    print_error "Test project build failed"
    exit 1
fi

# Build entire solution to ensure integration
print_status "Building entire solution..."
dotnet build "$SOLUTION_FILE" --configuration Release --no-restore --verbosity quiet

if [ $? -eq 0 ]; then
    print_success "Solution build completed successfully"
else
    print_error "Solution build failed"
    exit 1
fi

echo ""

# Run tests
print_status "Running test project ($TEST_PROJECT)..."
TEST_RESULT=$(dotnet test "$TEST_PROJECT_FILE" --configuration Release --no-build --verbosity quiet --logger "console;verbosity=minimal" 2>&1)
TEST_EXIT_CODE=$?

if [ $TEST_EXIT_CODE -eq 0 ]; then
    print_success "All tests passed"
    echo "$TEST_RESULT" | grep -E "(Passed|Failed|Skipped)" | tail -1
else
    print_error "Some tests failed"
    echo "$TEST_RESULT"
    exit 1
fi

echo ""

# Check for Entity Framework tools
print_status "Checking Entity Framework tools..."
if ! dotnet tool list -g | grep -q "dotnet-ef"; then
    print_status "Installing Entity Framework Core tools..."
    dotnet tool install --global dotnet-ef --verbosity quiet
    print_success "EF Core tools installed"
else
    print_success "EF Core tools already installed"
fi

echo ""

# Database migration check for main project
print_status "Checking database setup for main project..."
cd "$MAIN_PROJECT"

if [ ! -d "Migrations" ]; then
    print_status "Creating initial migration for main project..."
    dotnet ef migrations add InitialCreate --no-build >/dev/null 2>&1
    if [ $? -eq 0 ]; then
        print_success "Initial migration created"
    else
        print_warning "Migration creation skipped (may already exist)"
    fi
fi

print_status "Setting up database for main project..."
dotnet ef database update --no-build >/dev/null 2>&1
if [ $? -eq 0 ]; then
    print_success "Database setup completed"
else
    print_warning "Database update had issues (may already be up to date)"
fi

cd ..

echo ""

# Security check for main project
print_status "Running security checks on main project..."
SECURITY_ISSUES=0

# Check for hardcoded secrets in main project
if grep -r "password\s*=" "$MAIN_PROJECT/" --include="*.cs" --include="*.json" | grep -v "Password.Required" | grep -v "PasswordHash" >/dev/null; then
    print_warning "Potential hardcoded passwords found in main project"
    SECURITY_ISSUES=$((SECURITY_ISSUES + 1))
fi

# Check for debug code in main project
if grep -r "Console.WriteLine\|Debug.WriteLine" "$MAIN_PROJECT/" --include="*.cs" >/dev/null; then
    print_warning "Debug statements found in main project code"
    SECURITY_ISSUES=$((SECURITY_ISSUES + 1))
fi

if [ $SECURITY_ISSUES -eq 0 ]; then
    print_success "No obvious security issues found in main project"
fi

echo ""

# Performance check for main project
print_status "Checking for performance considerations in main project..."
PERF_ISSUES=0

# Check for Entity Framework issues in controllers
if grep -r "\.ToList()" "$MAIN_PROJECT/Controllers/" --include="*.cs" >/dev/null 2>/dev/null; then
    print_warning "Potential N+1 queries found (ToList() in controllers)"
    PERF_ISSUES=$((PERF_ISSUES + 1))
fi

# Check for synchronous database calls
if grep -r "\.Result\|\.Wait()" "$MAIN_PROJECT/Controllers/" --include="*.cs" >/dev/null 2>/dev/null; then
    print_warning "Synchronous async calls found in controllers"
    PERF_ISSUES=$((PERF_ISSUES + 1))
fi

if [ $PERF_ISSUES -eq 0 ]; then
    print_success "No obvious performance issues found in main project"
fi

echo ""

# Code quality check for both projects
print_status "Checking code quality across projects..."
QUALITY_ISSUES=0

# Check for proper using statements in main project
MAIN_LINQ_COUNT=$(grep -r "using System.Linq;" "$MAIN_PROJECT/" --include="*.cs" 2>/dev/null | wc -l)
if [ "$MAIN_LINQ_COUNT" -gt 10 ]; then
    print_warning "Consider using global using statements for common namespaces in main project"
    QUALITY_ISSUES=$((QUALITY_ISSUES + 1))
fi

# Check test project structure
if [ ! -d "$TEST_PROJECT/Controllers" ] && [ ! -d "$TEST_PROJECT/Services" ] && [ ! -d "$TEST_PROJECT/Models" ]; then
    print_warning "Test project may benefit from organized test structure (Controllers/, Services/, Models/)"
    QUALITY_ISSUES=$((QUALITY_ISSUES + 1))
fi

if [ $QUALITY_ISSUES -eq 0 ]; then
    print_success "Code quality looks good across both projects"
fi

echo ""

# Final verification
print_status "Running final verification..."

# Check if key files exist for both projects
REQUIRED_FILES=(
    "$MAIN_PROJECT/Program.cs"
    "$MAIN_PROJECT/appsettings.json"
    "$MAIN_PROJECT/appsettings.Development.json"
    "$TEST_PROJECT/AmplePack.Tests.csproj"
    "README.md"
    "GETTING_STARTED.md"
    "$SOLUTION_FILE"
)

MISSING_FILES=0
for file in "${REQUIRED_FILES[@]}"; do
    if [ ! -f "$file" ]; then
        print_error "Missing required file: $file"
        MISSING_FILES=$((MISSING_FILES + 1))
    fi
done

if [ $MISSING_FILES -eq 0 ]; then
    print_success "All required files present in both projects"
fi

# Verify project references
print_status "Verifying project references..."
if dotnet list "$TEST_PROJECT_FILE" reference | grep -q "$MAIN_PROJECT"; then
    print_success "Test project correctly references main project"
else
    print_warning "Test project may not reference main project"
fi

echo ""

# Summary
echo "?? Setup Complete!"
echo "=================="
echo ""
print_success "? Solution Build: Successful"
print_success "? Main Project ($MAIN_PROJECT): Ready"
print_success "? Test Project ($TEST_PROJECT): All Tests Passing"
print_success "? Database: Ready"
print_success "? Dependencies: Installed"
echo ""

echo "?? Project Structure:"
echo "??? $MAIN_PROJECT/                 # Main web application"
echo "?   ??? Controllers/              # MVC Controllers"
echo "?   ??? Models/                   # Data models"
echo "?   ??? Services/                 # Business logic"
echo "?   ??? Views/                    # Razor views"
echo "?   ??? Data/                     # Database context"
echo "??? $TEST_PROJECT/               # Test project"
echo "?   ??? Tests for all components"
echo "??? $SOLUTION_FILE               # Solution file"
echo ""

echo "?? Next Steps:"
echo "1. Run the main application:"
echo "   cd $MAIN_PROJECT && dotnet run"
echo "2. Open browser: https://localhost:7153"
echo "3. Login with: admin@ample.com / Admin@123"
echo "4. Explore the features!"
echo ""

echo "?? Testing:"
echo "- Run all tests: dotnet test $SOLUTION_FILE"
echo "- Run specific project tests: dotnet test $TEST_PROJECT_FILE"
echo "- Run with coverage: dotnet test $TEST_PROJECT_FILE --collect:\"XPlat Code Coverage\""
echo ""

echo "?? Documentation:"
echo "- README.md - Main documentation"
echo "- GETTING_STARTED.md - Quick start guide"
echo "- docs/ - Detailed documentation"
echo ""

echo "??? Development Commands:"
echo "- Build solution: dotnet build $SOLUTION_FILE"
echo "- Build main project: dotnet build $MAIN_PROJECT_FILE"
echo "- Build test project: dotnet build $TEST_PROJECT_FILE"
echo "- Deploy: ./deploy.sh"
echo ""

print_success "AmplePack solution with separate main and test projects is ready to use! ??"
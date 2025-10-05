#!/bin/bash

# Test Runner Script - Runs all tests locally
# Usage: ./run-all-tests.sh [--quick] [--help]

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Default values
QUICK_MODE=false
BASE_URL="https://localhost:5001"
OUTPUT_DIR="test-results-$(date +%Y%m%d_%H%M%S)"

# Parse arguments
while [[ $# -gt 0 ]]; do
  case $1 in
    --quick)
      QUICK_MODE=true
      shift
      ;;
    --url)
      BASE_URL="$2"
      shift 2
      ;;
    --help)
      echo "Usage: $0 [--quick] [--url BASE_URL]"
      echo "  --quick   Run only unit and integration tests (skip UI, performance, security)"
      echo "  --url     Base URL for application (default: https://localhost:5001)"
      exit 0
      ;;
    *)
      echo "Unknown option: $1"
      exit 1
      ;;
  esac
done

# Create output directory
mkdir -p "$OUTPUT_DIR"

echo -e "${BLUE}========================================"
echo "AmplePack Comprehensive Test Suite"
echo "========================================"
echo -e "Mode: $([ "$QUICK_MODE" = true ] && echo "Quick" || echo "Full")"
echo "Base URL: $BASE_URL"
echo "Output Directory: $OUTPUT_DIR"
echo -e "========================================${NC}"

# Function to print status
print_status() {
  local status=$1
  local message=$2
  case $status in
    "SUCCESS")
      echo -e "${GREEN}? $message${NC}"
      ;;
    "FAILURE")
      echo -e "${RED}? $message${NC}"
      ;;
    "WARNING")
      echo -e "${YELLOW}?? $message${NC}"
      ;;
    "INFO")
      echo -e "${BLUE}?? $message${NC}"
      ;;
  esac
}

# Function to run a test step
run_test_step() {
  local step_name=$1
  local command=$2
  local required=${3:-true}
  
  echo ""
  print_status "INFO" "Running $step_name..."
  echo "----------------------------------------"
  
  if eval "$command"; then
    print_status "SUCCESS" "$step_name completed successfully"
    return 0
  else
    if [ "$required" = true ]; then
      print_status "FAILURE" "$step_name failed"
      return 1
    else
      print_status "WARNING" "$step_name failed (optional)"
      return 0
    fi
  fi
}

# Check prerequisites
echo ""
print_status "INFO" "Checking prerequisites..."

# Check .NET
if command -v dotnet &> /dev/null; then
  DOTNET_VERSION=$(dotnet --version)
  print_status "SUCCESS" ".NET $DOTNET_VERSION found"
else
  print_status "FAILURE" ".NET is not installed"
  exit 1
fi

# Check if solution builds
print_status "INFO" "Building solution..."
if dotnet build --configuration Release; then
  print_status "SUCCESS" "Solution builds successfully"
else
  print_status "FAILURE" "Solution build failed"
  exit 1
fi

# Start application in background for testing
print_status "INFO" "Starting application for testing..."
cd AmplePack
dotnet run --configuration Release --urls "$BASE_URL" &
APP_PID=$!
cd ..

# Wait for application to start
print_status "INFO" "Waiting for application to start..."
timeout=60
while ! curl -s -k "$BASE_URL" > /dev/null; do
  sleep 2
  timeout=$((timeout - 2))
  if [ $timeout -le 0 ]; then
    print_status "FAILURE" "Application failed to start within 60 seconds"
    kill $APP_PID 2>/dev/null || true
    exit 1
  fi
done
print_status "SUCCESS" "Application started successfully"

# Trap to kill application on exit
trap 'kill $APP_PID 2>/dev/null || true' EXIT

# Run tests
FAILED_TESTS=()

# 1. Unit Tests
if ! run_test_step "Unit Tests" "dotnet test AmplePack.Tests/AmplePack.Tests.csproj --configuration Release --logger 'trx;LogFileName=unit-tests.trx' --results-directory '$OUTPUT_DIR' --collect:'XPlat Code Coverage'"; then
  FAILED_TESTS+=("Unit Tests")
fi

# 2. Integration Tests
if ! run_test_step "Integration Tests" "dotnet test AmplePack.Tests/AmplePack.Tests.csproj --configuration Release --filter Category=Integration --logger 'trx;LogFileName=integration-tests.trx' --results-directory '$OUTPUT_DIR'"; then
  FAILED_TESTS+=("Integration Tests")
fi

# Skip remaining tests in quick mode
if [ "$QUICK_MODE" = false ]; then
  
  # 3. UI Tests (if Playwright is available)
  if command -v playwright &> /dev/null || [ -f "AmplePack.UITests/bin/Debug/net9.0/playwright.ps1" ]; then
    if ! run_test_step "UI Tests" "cd AmplePack.UITests && dotnet test --configuration Release --logger 'trx;LogFileName=ui-tests.trx' --results-directory '../$OUTPUT_DIR'" false; then
      FAILED_TESTS+=("UI Tests")
    fi
  else
    print_status "WARNING" "Playwright not found, skipping UI tests"
  fi
  
  # 4. Performance Tests (if k6 is available)
  if command -v k6 &> /dev/null; then
    if ! run_test_step "Performance Tests" "cd PerformanceTests && ./run-performance-tests.sh --url '$BASE_URL' --output '../$OUTPUT_DIR'" false; then
      FAILED_TESTS+=("Performance Tests")
    fi
  else
    print_status "WARNING" "k6 not found, skipping performance tests"
  fi
  
  # 5. Security Tests (if available)
  if command -v zap-baseline.py &> /dev/null || command -v docker &> /dev/null; then
    if ! run_test_step "Security Tests" "cd SecurityTests && ./run-security-tests.sh --url '$BASE_URL' --output '../$OUTPUT_DIR'" false; then
      FAILED_TESTS+=("Security Tests")
    fi
  else
    print_status "WARNING" "OWASP ZAP or Docker not found, skipping security tests"
  fi
fi

# Generate summary report
echo ""
echo -e "${BLUE}========================================"
echo "Test Execution Summary"
echo -e "========================================${NC}"
echo "Output Directory: $OUTPUT_DIR"
echo ""

if [ ${#FAILED_TESTS[@]} -eq 0 ]; then
  print_status "SUCCESS" "All tests completed successfully!"
  echo ""
  echo "Generated artifacts:"
  ls -la "$OUTPUT_DIR"
  exit 0
else
  print_status "FAILURE" "Some tests failed:"
  for test in "${FAILED_TESTS[@]}"; do
    echo "  - $test"
  done
  echo ""
  echo "Check the output directory for detailed results: $OUTPUT_DIR"
  exit 1
fi
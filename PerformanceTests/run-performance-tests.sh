#!/bin/bash

# Performance Testing Script for AmplePack
# Requires k6 to be installed: https://k6.io/docs/getting-started/installation/

# Default values
BASE_URL="https://localhost:5001"
OUTPUT_DIR="test-results"
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")

# Parse command line arguments
while [[ $# -gt 0 ]]; do
  case $1 in
    --url)
      BASE_URL="$2"
      shift 2
      ;;
    --output)
      OUTPUT_DIR="$2"
      shift 2
      ;;
    --help)
      echo "Usage: $0 [--url BASE_URL] [--output OUTPUT_DIR]"
      echo "  --url     Base URL of the application (default: https://localhost:5001)"
      echo "  --output  Output directory for test results (default: test-results)"
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

echo "========================================"
echo "AmplePack Performance Testing Suite"
echo "========================================"
echo "Base URL: $BASE_URL"
echo "Output Directory: $OUTPUT_DIR"
echo "Timestamp: $TIMESTAMP"
echo "========================================"

# Function to run a k6 test
run_k6_test() {
  local test_name=$1
  local test_file=$2
  local output_file="$OUTPUT_DIR/${test_name}_${TIMESTAMP}"
  
  echo ""
  echo "Running $test_name..."
  echo "----------------------------------------"
  
  k6 run \
    --env BASE_URL="$BASE_URL" \
    --out json="$output_file.json" \
    --out csv="$output_file.csv" \
    "$test_file"
  
  if [ $? -eq 0 ]; then
    echo "? $test_name completed successfully"
  else
    echo "? $test_name failed"
  fi
}

# Check if k6 is installed
if ! command -v k6 &> /dev/null; then
  echo "? k6 is not installed. Please install k6 from https://k6.io/docs/getting-started/installation/"
  exit 1
fi

# Check if application is accessible
echo "Checking if application is accessible at $BASE_URL..."
if curl -s -o /dev/null -w "%{http_code}" "$BASE_URL" | grep -q "200\|302\|301"; then
  echo "? Application is accessible"
else
  echo "? Application is not accessible at $BASE_URL"
  echo "Please ensure the application is running and the URL is correct"
  exit 1
fi

# Run tests
run_k6_test "load_test" "load-test.js"
run_k6_test "spike_test" "spike-test.js"

echo ""
echo "========================================"
echo "Performance Testing Completed"
echo "========================================"
echo "Results saved to: $OUTPUT_DIR"
echo ""
echo "Summary files:"
ls -la "$OUTPUT_DIR"/*"$TIMESTAMP"*

echo ""
echo "To view detailed results:"
echo "  - JSON files can be analyzed with jq or imported into analysis tools"
echo "  - CSV files can be opened in Excel or imported into monitoring systems"
echo ""
echo "Example analysis commands:"
echo "  jq '.metrics.http_req_duration.values.avg' $OUTPUT_DIR/load_test_${TIMESTAMP}.json"
echo "  jq '.metrics.http_req_failed.values.rate' $OUTPUT_DIR/load_test_${TIMESTAMP}.json"
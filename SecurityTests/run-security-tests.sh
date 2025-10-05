#!/bin/bash

# OWASP ZAP Security Testing Script for AmplePack
# Requires OWASP ZAP to be installed: https://www.zaproxy.org/download/

# Default values
BASE_URL="https://localhost:5001"
OUTPUT_DIR="security-test-results"
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
ZAP_PORT=8090

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
    --zap-port)
      ZAP_PORT="$2"
      shift 2
      ;;
    --help)
      echo "Usage: $0 [--url BASE_URL] [--output OUTPUT_DIR] [--zap-port PORT]"
      echo "  --url      Base URL of the application (default: https://localhost:5001)"
      echo "  --output   Output directory for test results (default: security-test-results)"
      echo "  --zap-port Port for ZAP proxy (default: 8090)"
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
echo "AmplePack Security Testing Suite"
echo "========================================"
echo "Base URL: $BASE_URL"
echo "Output Directory: $OUTPUT_DIR"
echo "ZAP Port: $ZAP_PORT"
echo "Timestamp: $TIMESTAMP"
echo "========================================"

# Check if ZAP is installed
if ! command -v zap.sh &> /dev/null && ! command -v zap-baseline.py &> /dev/null; then
  echo "? OWASP ZAP is not installed or not in PATH"
  echo "Please install ZAP from https://www.zaproxy.org/download/"
  echo "Or ensure zap.sh or zap-baseline.py is in your PATH"
  exit 1
fi

# Function to run ZAP baseline scan
run_zap_baseline() {
  echo ""
  echo "Running ZAP Baseline Scan..."
  echo "----------------------------------------"
  
  local report_file="$OUTPUT_DIR/zap_baseline_report_$TIMESTAMP.html"
  local json_file="$OUTPUT_DIR/zap_baseline_report_$TIMESTAMP.json"
  
  if command -v zap-baseline.py &> /dev/null; then
    zap-baseline.py \
      -t "$BASE_URL" \
      -r "$report_file" \
      -J "$json_file" \
      -w baseline_report.md \
      -z "-configfile /dev/null"
  elif command -v docker &> /dev/null; then
    # Use Docker if ZAP is not installed locally
    echo "Using Docker to run ZAP..."
    docker run -v $(pwd):/zap/wrk/:rw \
      -t owasp/zap2docker-stable \
      zap-baseline.py \
      -t "$BASE_URL" \
      -r "$report_file" \
      -J "$json_file"
  else
    echo "? Neither ZAP nor Docker is available"
    return 1
  fi
  
  if [ $? -eq 0 ] || [ $? -eq 2 ]; then  # ZAP returns 2 for warnings
    echo "? ZAP Baseline scan completed"
  else
    echo "? ZAP Baseline scan failed"
  fi
}

# Function to run ZAP full scan
run_zap_full_scan() {
  echo ""
  echo "Running ZAP Full Scan..."
  echo "----------------------------------------"
  
  local report_file="$OUTPUT_DIR/zap_full_report_$TIMESTAMP.html"
  local json_file="$OUTPUT_DIR/zap_full_report_$TIMESTAMP.json"
  
  if command -v zap-full-scan.py &> /dev/null; then
    zap-full-scan.py \
      -t "$BASE_URL" \
      -r "$report_file" \
      -J "$json_file" \
      -z "-configfile /dev/null"
  elif command -v docker &> /dev/null; then
    # Use Docker if ZAP is not installed locally
    echo "Using Docker to run ZAP..."
    docker run -v $(pwd):/zap/wrk/:rw \
      -t owasp/zap2docker-stable \
      zap-full-scan.py \
      -t "$BASE_URL" \
      -r "$report_file" \
      -J "$json_file"
  else
    echo "? Neither ZAP nor Docker is available"
    return 1
  fi
  
  if [ $? -eq 0 ] || [ $? -eq 2 ]; then  # ZAP returns 2 for warnings
    echo "? ZAP Full scan completed"
  else
    echo "? ZAP Full scan failed"
  fi
}

# Function to run API security tests
run_api_security_tests() {
  echo ""
  echo "Running API Security Tests..."
  echo "----------------------------------------"
  
  # Test for common API vulnerabilities
  local api_endpoints=(
    "/BoxCalculator/Calculate"
    "/BoxCalculator/GetLayoutVisualization"
    "/Api/GetCustomers"
    "/Api/GetOrders"
  )
  
  for endpoint in "${api_endpoints[@]}"; do
    echo "Testing endpoint: $endpoint"
    
    # Test for SQL injection
    curl -s -o /dev/null -w "%{http_code}" \
      -X POST \
      -H "Content-Type: application/json" \
      -d '{"id": "1 OR 1=1"}' \
      "$BASE_URL$endpoint" | grep -q "500"
    
    if [ $? -eq 0 ]; then
      echo "??  Potential SQL injection vulnerability in $endpoint"
    fi
    
    # Test for XSS
    curl -s -o /dev/null -w "%{http_code}" \
      -X POST \
      -H "Content-Type: application/json" \
      -d '{"name": "<script>alert(1)</script>"}' \
      "$BASE_URL$endpoint" | grep -q "200"
    
    if [ $? -eq 0 ]; then
      echo "??  Potential XSS vulnerability in $endpoint"
    fi
  done
}

# Function to check security headers
check_security_headers() {
  echo ""
  echo "Checking Security Headers..."
  echo "----------------------------------------"
  
  local headers_file="$OUTPUT_DIR/security_headers_$TIMESTAMP.txt"
  
  curl -s -I "$BASE_URL" > "$headers_file"
  
  # Check for important security headers
  local required_headers=(
    "X-Content-Type-Options"
    "X-Frame-Options"
    "X-XSS-Protection"
    "Strict-Transport-Security"
    "Content-Security-Policy"
  )
  
  for header in "${required_headers[@]}"; do
    if grep -qi "$header" "$headers_file"; then
      echo "? $header header is present"
    else
      echo "? $header header is missing"
    fi
  done
  
  echo "Full headers saved to: $headers_file"
}

# Function to test for common vulnerabilities
test_common_vulnerabilities() {
  echo ""
  echo "Testing Common Vulnerabilities..."
  echo "----------------------------------------"
  
  # Test for directory traversal
  echo "Testing directory traversal..."
  curl -s "$BASE_URL/../../../etc/passwd" | grep -q "root:" && echo "??  Directory traversal vulnerability detected"
  
  # Test for information disclosure
  echo "Testing information disclosure..."
  curl -s "$BASE_URL/web.config" | grep -q "configuration" && echo "??  Configuration file accessible"
  curl -s "$BASE_URL/appsettings.json" | grep -q "{" && echo "??  App settings file accessible"
  
  # Test for backup files
  echo "Testing for backup files..."
  for ext in bak old backup; do
    curl -s "$BASE_URL/web.config.$ext" | grep -q "configuration" && echo "??  Backup file detected: web.config.$ext"
  done
}

# Check if application is accessible
echo "Checking if application is accessible at $BASE_URL..."
if curl -s -o /dev/null -w "%{http_code}" "$BASE_URL" | grep -q "200\|302\|301"; then
  echo "? Application is accessible"
else
  echo "? Application is not accessible at $BASE_URL"
  echo "Please ensure the application is running and the URL is correct"
  exit 1
fi

# Run security tests
check_security_headers
test_common_vulnerabilities
run_api_security_tests
run_zap_baseline

# Optionally run full scan (commented out by default as it takes longer)
# echo "Do you want to run a full ZAP scan? This may take 10-30 minutes. (y/N)"
# read -r response
# if [[ "$response" =~ ^[Yy]$ ]]; then
#   run_zap_full_scan
# fi

echo ""
echo "========================================"
echo "Security Testing Completed"
echo "========================================"
echo "Results saved to: $OUTPUT_DIR"
echo ""
echo "Summary files:"
ls -la "$OUTPUT_DIR"/*"$TIMESTAMP"*

echo ""
echo "Review the security reports and address any HIGH or MEDIUM severity findings."
echo "Pay special attention to:"
echo "  - Authentication and authorization flaws"
echo "  - Input validation vulnerabilities"
echo "  - Security misconfigurations"
echo "  - Sensitive data exposure"
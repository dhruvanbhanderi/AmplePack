#!/bin/bash

# Setup script for AmplePack testing environment
# This script installs all necessary tools for running the complete test suite

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

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

echo -e "${BLUE}========================================"
echo "AmplePack Testing Environment Setup"
echo -e "========================================${NC}"

# Check OS
if [[ "$OSTYPE" == "linux-gnu"* ]]; then
  OS="linux"
  print_status "INFO" "Detected Linux system"
elif [[ "$OSTYPE" == "darwin"* ]]; then
  OS="mac"
  print_status "INFO" "Detected macOS system"
else
  print_status "FAILURE" "Unsupported operating system: $OSTYPE"
  exit 1
fi

# Check if running as root
if [ "$EUID" -eq 0 ]; then
  print_status "WARNING" "Running as root. Some installations might not work correctly."
fi

# 1. Check and install .NET 9
print_status "INFO" "Checking .NET installation..."
if command -v dotnet &> /dev/null; then
  DOTNET_VERSION=$(dotnet --version)
  if [[ $DOTNET_VERSION == 9.* ]]; then
    print_status "SUCCESS" ".NET 9 already installed ($DOTNET_VERSION)"
  else
    print_status "WARNING" ".NET version $DOTNET_VERSION found, but .NET 9 is recommended"
  fi
else
  print_status "INFO" "Installing .NET 9..."
  if [ "$OS" = "linux" ]; then
    # Install .NET on Linux
    wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
    sudo dpkg -i packages-microsoft-prod.deb
    rm packages-microsoft-prod.deb
    sudo apt-get update
    sudo apt-get install -y dotnet-sdk-9.0
  elif [ "$OS" = "mac" ]; then
    # Install .NET on macOS
    if command -v brew &> /dev/null; then
      brew install dotnet
    else
      print_status "WARNING" "Homebrew not found. Please install .NET 9 manually from https://dotnet.microsoft.com/download"
    fi
  fi
  
  if command -v dotnet &> /dev/null; then
    print_status "SUCCESS" ".NET installed successfully"
  else
    print_status "FAILURE" "Failed to install .NET"
    exit 1
  fi
fi

# 2. Install Node.js (for Playwright)
print_status "INFO" "Checking Node.js installation..."
if command -v node &> /dev/null; then
  NODE_VERSION=$(node --version)
  print_status "SUCCESS" "Node.js already installed ($NODE_VERSION)"
else
  print_status "INFO" "Installing Node.js..."
  if [ "$OS" = "linux" ]; then
    curl -fsSL https://deb.nodesource.com/setup_18.x | sudo -E bash -
    sudo apt-get install -y nodejs
  elif [ "$OS" = "mac" ]; then
    if command -v brew &> /dev/null; then
      brew install node
    else
      print_status "WARNING" "Homebrew not found. Please install Node.js manually"
    fi
  fi
  
  if command -v node &> /dev/null; then
    print_status "SUCCESS" "Node.js installed successfully"
  else
    print_status "WARNING" "Failed to install Node.js automatically"
  fi
fi

# 3. Install k6 (for performance testing)
print_status "INFO" "Checking k6 installation..."
if command -v k6 &> /dev/null; then
  K6_VERSION=$(k6 version)
  print_status "SUCCESS" "k6 already installed ($K6_VERSION)"
else
  print_status "INFO" "Installing k6..."
  if [ "$OS" = "linux" ]; then
    sudo gpg -k
    sudo gpg --no-default-keyring --keyring /usr/share/keyrings/k6-archive-keyring.gpg --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D69
    echo "deb [signed-by=/usr/share/keyrings/k6-archive-keyring.gpg] https://dl.k6.io/deb stable main" | sudo tee /etc/apt/sources.list.d/k6.list
    sudo apt-get update
    sudo apt-get install k6
  elif [ "$OS" = "mac" ]; then
    if command -v brew &> /dev/null; then
      brew install k6
    else
      print_status "WARNING" "Homebrew not found. Please install k6 manually from https://k6.io/docs/getting-started/installation/"
    fi
  fi
  
  if command -v k6 &> /dev/null; then
    print_status "SUCCESS" "k6 installed successfully"
  else
    print_status "WARNING" "Failed to install k6 automatically"
  fi
fi

# 4. Install Docker (for security testing)
print_status "INFO" "Checking Docker installation..."
if command -v docker &> /dev/null; then
  print_status "SUCCESS" "Docker already installed"
else
  print_status "INFO" "Installing Docker..."
  if [ "$OS" = "linux" ]; then
    # Install Docker on Linux
    curl -fsSL https://get.docker.com -o get-docker.sh
    sudo sh get-docker.sh
    sudo usermod -aG docker $USER
    rm get-docker.sh
    print_status "WARNING" "Please log out and log back in for Docker group permissions to take effect"
  elif [ "$OS" = "mac" ]; then
    if command -v brew &> /dev/null; then
      brew install --cask docker
      print_status "WARNING" "Please start Docker Desktop manually"
    else
      print_status "WARNING" "Please install Docker Desktop manually from https://www.docker.com/products/docker-desktop"
    fi
  fi
  
  if command -v docker &> /dev/null; then
    print_status "SUCCESS" "Docker installed successfully"
  else
    print_status "WARNING" "Failed to install Docker automatically"
  fi
fi

# 5. Install additional tools
print_status "INFO" "Installing additional tools..."

# Install curl if not present
if ! command -v curl &> /dev/null; then
  if [ "$OS" = "linux" ]; then
    sudo apt-get install -y curl
  elif [ "$OS" = "mac" ]; then
    # curl comes with macOS
    :
  fi
fi

# Install jq for JSON processing
if ! command -v jq &> /dev/null; then
  if [ "$OS" = "linux" ]; then
    sudo apt-get install -y jq
  elif [ "$OS" = "mac" ]; then
    if command -v brew &> /dev/null; then
      brew install jq
    fi
  fi
fi

# 6. Setup project dependencies
print_status "INFO" "Setting up project dependencies..."

# Restore NuGet packages
if [ -f "AmplePack.sln" ]; then
  dotnet restore
  print_status "SUCCESS" "NuGet packages restored"
else
  print_status "WARNING" "Solution file not found in current directory"
fi

# Install Playwright browsers
if [ -d "AmplePack.UITests" ]; then
  cd AmplePack.UITests
  dotnet restore
  
  # Try to install Playwright browsers
  if [ -f "bin/Debug/net9.0/playwright.ps1" ]; then
    pwsh bin/Debug/net9.0/playwright.ps1 install --with-deps
    print_status "SUCCESS" "Playwright browsers installed"
  else
    print_status "WARNING" "Playwright installation script not found. Run 'dotnet build' first"
  fi
  cd ..
fi

# 7. Create necessary directories
print_status "INFO" "Creating test output directories..."
mkdir -p test-results
mkdir -p performance-results
mkdir -p security-results
mkdir -p ui-test-results

# Make scripts executable
chmod +x run-all-tests.sh 2>/dev/null || true
chmod +x PerformanceTests/run-performance-tests.sh 2>/dev/null || true
chmod +x SecurityTests/run-security-tests.sh 2>/dev/null || true

echo ""
echo -e "${BLUE}========================================"
echo "Setup Summary"
echo -e "========================================${NC}"

# Check what's installed
echo "Installed tools:"
command -v dotnet &> /dev/null && echo "  ? .NET $(dotnet --version)"
command -v node &> /dev/null && echo "  ? Node.js $(node --version)"
command -v k6 &> /dev/null && echo "  ? k6 $(k6 version | head -1)"
command -v docker &> /dev/null && echo "  ? Docker $(docker --version | cut -d' ' -f3 | cut -d',' -f1)"
command -v curl &> /dev/null && echo "  ? curl"
command -v jq &> /dev/null && echo "  ? jq"

echo ""
echo "Next steps:"
echo "1. Run './run-all-tests.sh --quick' for unit and integration tests"
echo "2. Run './run-all-tests.sh' for the complete test suite"
echo "3. Check individual test directories for specific testing tools"

echo ""
print_status "SUCCESS" "Setup completed! You can now run the comprehensive test suite."